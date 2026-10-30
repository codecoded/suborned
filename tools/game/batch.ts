// usage: bun run batch [--force] [--wait-timeout <s>] [--dry-run] [--from <step>] [--restart] [--full | --submission] [--only a,b] [--skip a,b] [--editor | --batchmode] [--shots <file>] [--shots-out <dir>] [--no-shots] [--rebuild] [--perf064] [--rows a,b] [--behind]
// The one command Latch runs per build (brief 034 item 3). In order, stopping at the first failure:
//   sim          bun tools/sim/unity.ts --tree           the engine, the inputs and the DLL into the Unity project (headless scenarios first)
//   compile      tools/game/runtime-compile.ts           the player's scripts compile (dotnet, no Editor); the shot runner is installed first when it is not
//   kit          bun run kit                             the DecoKit catalogue and the hash-checked copy of adopted pieces
//   world        bun run world                           the plan → world export
//   unity-world  Unity reimport + HarbourWorldBuilder rebuild from the export      (Editor open: its menu, through the Unity CLI; closed: batchmode)
//   unity-match  Unity's scenarios against headless (Suborned/Sim/Run Scenarios)   (same two ways)
//   game-build   bun tools/game/cli.ts --build           the player and its proof, by default a delta (below; it proves the first frame draws); SKIPPED, no build number spent, when nothing under unity/Assets, ProjectSettings or Packages is newer than the app (--rebuild forces it)
//   game-check   bun tools/game/cli.ts --check           scenarios, the proof with frame times (every gated section ok: tools/game/proofgates.ts, r059 and r067 among them), the driven session replayed headless, the app carrying cues.json
//   shots        the built player with -shots            tools/game/shots.json: each shot, its HUD layer and manifest, clips (ShotRunner.cs)
//   budget       tools/hud-budget + the layout audit     every HUD shot within budget; every audit clean; and the proof's pictures of each set in tools/game/proofset.json (r059, r067, r123) the proof has, at 1920x1080
//   late-fight   the built player with -late             tools/game/late.json: loud-fight and Sky's lagging session played to the last 60 s, then on at real time with the script's orders, 1080p; a frame over 50 ms tied to a save, order or the end, p99 over 16.7 ms, over 100 ms at the end: a row with findings is run once more and fails only if the same kind returns; the machine's load is recorded (brief 045; tools/game/late.ts)
//   tp-long      the built player with -tplong          tools/game/tplong.json: the two third-person views (Sky's street, the worst heading) and the play view at 2880x1864, ~40 s: triangles, SetPass, shadow casters, renderers drawn and far, mean and p99 against the gates in tplong.ts (brief 048); SKIPPED, with the reason, until the built player has the hook
//   perf064      the built player with -perf064         (opt-in: --perf064 or --only perf064, ~3 minutes) Redline's scene at 2880x1864, four 20 s windows, before/after: frame ms, the host's cone cost, the CPU/GPU split and each window's load when the build writes them (brief 064 R7; tools/game/perfrow.ts). Counts are the gate; ms only on a quiet machine
//   Every step that reports frame times (game-check, shots, late-fight, tp-long, perf064) records the load average (1/5/15) at its start and end and prints it under its row, NOISY MACHINE included.
// Delta proofs (brief 078; tools/game/proofdelta.ts, proofmap.json): the proof runs only the sections a change since each one's last recorded pass can affect. A path the map cannot place, or any change to the engine, its data, the world export or the kit, runs the whole proof; so do --full and --submission. A section not run shows as "not run, unchanged since build N (commit)", never as passed.
// After a failure the screen prints the command that goes on (next line); a bare `bun run batch` does the same while the sources are unchanged: it starts at the failed step on the same player (--restart: from the top). No step before it runs, so nothing rewrites the scene and forces a rebuild. A failed proof section runs alone first, then, if it passes, after what ran before it; only that second run counts.
// Sky is playing (a player of his, no automation flag; playing.ts): before every step the batch waits ("Sky is playing; waiting") up to --wait-timeout seconds (default 1200), then stops; --force goes on.
// Every player step starts the player ONCE with its whole list (-shots, -late, -tplong take a file of rows; -perf064 is one scene): launchPlayer below. The window opens
// windowed at the step's size from its first frame (-screen-fullscreen 0 -screen-width/-height; without them a launch opens at the size the last run left in the
// player's prefs, then jumps), and each step's log and row say how many launches it made (late-fight: a second only when a row with findings is run again).
//   --rows a,b   late-fight and tp-long play only these rows of late.json / tplong.json (a short test of one route; the verdict files are the same format)
//   --behind     start the player with `open -g` (it never takes the keyboard and opens under the window in front); off by default until a quiet-machine
//                comparison shows the frame times the same within 2 % (briefs/042/splice-one-launch.md)
// then prints one screen: pass/fail per step, seconds, the build, frame times, where the shots are. Logs of every step: out/batch/logs/<step>.log.
// --dry-run prints what each step would run and touches nothing (no Unity, no build, nothing written under out/): only the steps' plan() texts run, plus read-only pgrep to see whether the Editor is open (tests: batch.test.ts). Exit 0 only if every step that ran passed.
import { copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync, renameSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { cpus, homedir, loadavg } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { compile as runtimeCompile } from './runtime-compile';
import { guardConfig, takeGuardFlags, timeoutText, waitForSky } from './playing';
import { GATES, MARKER as TPLONG_MARKER, judgeTpLong, resolveTpLong, type TpLongReport } from './tplong';
import { LIMITS, loadText, noisy as loadNoisy, resolveLate, runLate, type End, type LateReport, type LateResolvedFile, type LoadSample } from './late';
import { perf064Lines, spanEnd, spanStart, spanText, type LoadSpan, type Perf064Report } from './perfrow';
import { build as buildEngine, engineAt } from '../missions/check/engine';
import { DEFAULT_FILE, ROOT, expectedFiles, fileResolver, pngFileSize, resolveShots, type ResolvedFile } from './shots';
import { judgeProofBudget, loadProofSet, type ProofBudget } from './proofset';
import { walkFileState } from './walkfile';
import { gateFile, ledgerFile, loadMap, owedCompany, planLines, planProof, readLedger, snapshot, writePlan, planFile, type Plan } from './proofdelta';

const UNITY = join(ROOT, 'unity'), OUT = process.env.SUBORNED_BATCH_OUT ? resolve(process.env.SUBORNED_BATCH_OUT) : join(ROOT, 'out/batch'), /* SUBORNED_BATCH_OUT: the tests' scratch folder */ APP = join(ROOT, 'out/build/play/The Suborned.app'), PROOF = join(ROOT, 'out/build/play/proof/proof.json');
const BUILD_DIR = dirname(dirname(PROOF));
const SCENE = join(UNITY, 'Assets/Scenes/HarbourDistrict.unity'), MATCH = join(ROOT, 'out/sim-unity/unity-editor.json');
const FILES = ['ShotRunner.cs', 'CaptureSize.cs'].map(f => [join(ROOT, 'tools/game/unity', f), join(UNITY, 'Assets/Suborned/Runtime/Play', f)] as const);
const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };

export type StepResult = { ok: boolean; detail: string; /** the step has nothing to run yet: shown as skipped, with the reason, and the batch goes on */ skipped?: boolean };
export type Ctx = {
  dry: boolean; mode: 'editor' | 'batchmode'; shotsFile: string; shotsOut: string;
  /** build the player even when nothing it is made from is newer than the app */
  rebuild?: boolean;
  /** run the perf probe step (opt-in: about three minutes of the player; --perf064 or --only perf064) */
  perf064?: boolean;
  /** late-fight and tp-long: only these rows (--rows) */
  rows?: string[];
  /** start players with `open -g` (--behind) */
  behind?: boolean;
  /** a reason to run the whole proof: --full, --submission */
  fullProof?: string;
  /** what the steps hand each other */
  facts: Record<string, unknown>;
  say(m: string): void;
  /** called before each step runs; a text means Sky kept playing past the timeout: the step fails with it (playing.ts) */
  guard?(stepId: string): Promise<string | undefined>;
};
/** perf: the step reports frame times, so the screen records the machine's load around it (perfrow.ts, Redline R7) */
export type Step = { id: string; title: string; perf?: boolean; plan(c: Ctx): string[]; run(c: Ctx): Promise<StepResult> };
export type Row = { id: string; title: string; status: 'ok' | 'FAILED' | 'skipped' | 'not run'; seconds: number; detail: string; /** the machine's load average (1/5/15) at the start and end of a perf step */ load?: LoadSpan };

const sleep = (ms: number) => new Promise(r => setTimeout(r, ms));
const tail = (s: string, n = 12) => s.trim().split('\n').slice(-n).join('\n');

// ---------------------------------------------------------------- running things

type Ran = { code: number; out: string };
/** Run a command, its output to out/batch/logs/<id>.log (and returned). */
async function sh(id: string, cmd: string[], o: { cwd?: string; timeoutMs?: number; append?: boolean; header?: string } = {}): Promise<Ran> {
  mkdirSync(join(OUT, 'logs'), { recursive: true });
  const p = Bun.spawn(cmd, { cwd: o.cwd ?? ROOT, env, stdout: 'pipe', stderr: 'pipe' });
  const timer = o.timeoutMs ? setTimeout(() => p.kill(), o.timeoutMs) : undefined;
  const [out, err, code] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]);
  if (timer) clearTimeout(timer);
  const text = out + (err ? `\n--- stderr ---\n${err}` : '');
  writeFileSync(join(OUT, 'logs', `${id}.log`), (o.append && existsSync(join(OUT, 'logs', `${id}.log`)) ? readFileSync(join(OUT, 'logs', `${id}.log`), 'utf8') : '') + `${o.header ? `# ${o.header}\n` : ''}$ ${cmd.join(' ')}\n${text}\n(exit ${code})\n`);
  return { code, out: text };
}
const fail = (what: string, r?: Ran): StepResult => ({ ok: false, detail: r ? `${what}\n${tail(r.out)}` : what });

/** The player's command line for one launch: windowed at w x h pixels from the first frame, then the mode's own arguments. `behind`: through `open -g -n -W`
 *  (LaunchServices starts it without taking the keyboard, its window under the one in front; -W waits for it to quit). */
export function playerCommand(mode: string[], size: [number, number], behind = false, app = APP): string[] {
  const args = ['-screen-fullscreen', '0', '-screen-width', String(size[0]), '-screen-height', String(size[1]), ...mode];
  return behind ? ['open', '-g', '-n', '-W', app, '--args', ...args] : [join(app, 'Contents/MacOS/The Suborned'), ...args];
}
/** Launches per step in this batch (the step's log and row show the count). */
export const launches: Record<string, number> = {};
export const launchText = (id: string) => `${launches[id] ?? 0} player launch${launches[id] === 1 ? '' : 'es'}`;
/** One player launch for step `id`, counted and logged. Behind, the process is LaunchServices' child, not ours: a timeout kills the player by PID, found by its
 *  -logFile path (unique to the step), never by pattern kill. */
async function launchPlayer(c: Ctx, id: string, mode: string[], size: [number, number], o: { timeoutMs: number; append?: boolean }): Promise<Ran> {
  launches[id] = (launches[id] ?? 0) + 1;
  c.say(`${id}: player launch ${launches[id]} (${c.behind ? 'open -g, behind' : 'direct'}, ${size[0]}x${size[1]} windowed)`);
  const cmd = playerCommand(mode, size, c.behind);
  const logFile = mode[mode.indexOf('-logFile') + 1];
  const timer = c.behind && logFile ? setTimeout(() => {
    const pids = Bun.spawnSync(['pgrep', '-f', logFile], { stdout: 'pipe' }).stdout.toString().trim().split('\n').filter(Boolean).map(Number).filter(p => p !== process.pid);
    for (const pid of pids) try { process.kill(pid); } catch { /* gone */ }
  }, o.timeoutMs) : undefined;
  const r = await sh(id, cmd, { timeoutMs: c.behind ? o.timeoutMs + 10000 : o.timeoutMs, append: o.append || launches[id] > 1, header: `player launch ${launches[id]} of step ${id}` });
  if (timer) clearTimeout(timer);
  return r;
}

/** The Editor with this project open, if any (pgrep; the Unity CLI answers for the rest). */
export function editorOpen(): boolean {
  const ps = Bun.spawnSync(['pgrep', '-fil', 'Unity.app/Contents/MacOS/Unity'], { stdout: 'pipe', stderr: 'pipe' }).stdout.toString();
  return ps.split('\n').some(l => l.toLowerCase().includes(UNITY.toLowerCase()) && !l.includes('-batchmode'));
}
// --project-path: with a second Editor open on this Mac (another project) the CLI refuses to guess (exit 6), so every call names ours
async function editorStatus(): Promise<{ playMode?: string; compiling?: boolean } | null> {
  const r = await sh('unity-cli', ['unity', 'cmd', 'editor', 'status', '--project-path', UNITY]);
  const line = r.out.split('\n').find(l => l.includes('"playMode"'));
  if (!line) return null;
  try { return JSON.parse(line.slice(line.indexOf('{'))); } catch { return null; }
}
async function menu(path: string): Promise<boolean> { return (await sh('unity-cli', ['unity', 'cmd', 'editor', 'invoke_menu', '--path', path, '--project-path', UNITY], { append: true })).code === 0; }
async function idle(timeoutMs = 600000): Promise<boolean> {
  const t0 = Date.now();
  while (Date.now() - t0 < timeoutMs) { const s = await editorStatus(); if (s && !s.compiling) return true; await sleep(2000); }
  return false;
}
const editorLog = join(homedir(), 'Library/Logs/Unity/Editor.log');
const logSize = () => (existsSync(editorLog) ? statSync(editorLog).size : 0);
/** What the Editor wrote since `from`: the lines that say a build or a script went wrong. */
export function logProblems(text: string): string[] {
  return [...new Set(text.split('\n').filter(l => /error CS\d+|Exception:|\bWorld: .*(FAIL|error)|Build Finished, Result: Failure|Compilation failed/i.test(l)).map(l => l.trim().slice(0, 200)))].slice(0, 8);
}
const logSince = (from: number) => (existsSync(editorLog) && logSize() > from ? readFileSync(editorLog).subarray(from).toString('utf8') : '');

async function waitFile(file: string, since: number, timeoutMs: number, settleMs = 4000): Promise<boolean> {
  const t0 = Date.now();
  while (Date.now() - t0 < timeoutMs) {
    if (existsSync(file) && statSync(file).mtimeMs > since) { await sleep(settleMs); const m = statSync(file).mtimeMs; await sleep(1000); if (statSync(file).mtimeMs === m) return true; }
    await sleep(2000);
  }
  return false;
}

// ---------------------------------------------------------------- the steps

/** The shot runner is in the Unity project: staged in tools/game/unity until the first batch moves it into Assets (Unity makes its .meta). */
export function installRunner(staged = FILES[0]![0], installed = FILES[0]![1]): string {
  if (existsSync(installed)) return 'installed';
  if (!existsSync(staged)) throw new Error(`ShotRunner.cs is in neither ${staged} nor ${installed}`);
  copyFileSync(staged, installed); rmSync(staged);
  return 'installed now (moved from tools/game/unity/ShotRunner.cs; Unity will import it)';
}

export const STEPS: Step[] = [
  {
    id: 'sim', title: 'engine into Unity', plan: () => ['bun tools/sim/unity.ts --tree'],
    async run() {
      const r = await sh('sim', ['bun', 'tools/sim/unity.ts', '--tree']);
      const head = r.out.match(/headless: (\d+)\/(\d+) scenarios pass[^\n]*/)?.[0], dll = r.out.match(/Suborned\.Sim\.dll sha256 (\w{12})/)?.[1];
      return r.code === 0 ? { ok: true, detail: `${head ?? 'engine built'}${dll ? `; DLL ${dll}` : ''}` } : fail('tools/sim/unity.ts failed', r);
    },
  },
  {
    id: 'compile', title: 'player scripts compile', plan: () => ['install tools/game/unity/ShotRunner.cs and CaptureSize.cs into unity/Assets/Suborned/Runtime/Play when they are not there', 'bun tools/game/runtime-compile.ts'],
    async run(c) {
      let how: string;
      try { const hows = FILES.map(([a, b]) => installRunner(a, b)); how = hows.find(h => h !== 'installed') ?? 'installed'; } catch (e) { return fail((e as Error).message); }
      if (how !== 'installed') c.say(`shot runner ${how}`);
      const r = runtimeCompile();
      if (r.missing) return fail(r.missing);
      return r.ok ? { ok: true, detail: `Suborned.Runtime compiles, ${r.warnings} warnings (shot runner ${how.split(' ')[0]})` } : fail(`${r.errors.length} compile error${r.errors.length === 1 ? '' : 's'}:\n${r.errors.slice(0, 8).join('\n')}`);
    },
  },
  {
    id: 'kit', title: 'kit catalogue and import', plan: () => ['bun run kit'],
    async run() { const r = await sh('kit', ['bun', 'run', 'kit']); return r.code === 0 ? { ok: true, detail: tail(r.out, 1).slice(0, 110) } : fail('bun run kit failed', r); },
  },
  {
    id: 'world', title: 'plan to world export', plan: () => ['bun run world', 'then, only when out/world/harbour-district.walk.json is not made from that export (the Unity step refuses a stale one): bun tools/geometry/cli.ts --no-shots'],
    async run() {
      const r = await sh('world', ['bun', 'run', 'world']);
      if (r.code !== 0) return fail('bun run world failed', r);
      let walk = walkFileState(ROOT), mended = '';
      if (!walk.fresh) { // a world run that moved the export leaves the walk file behind; geometry writes it again (2 s with the kit cached)
        const was = walk.why, g = await sh('world', ['bun', 'tools/geometry/cli.ts', '--no-shots'], { append: true });
        walk = walkFileState(ROOT);
        if (g.code !== 0 || !walk.fresh) return fail(`the walk file was stale (${was}) and bun tools/geometry/cli.ts --no-shots did not mend it (${walk.why})`, g);
        mended = ` (stale: ${was}; geometry wrote it again)`;
      }
      return { ok: true, detail: `${tail(r.out, 1).slice(0, 80)}; walk file ${walk.why}${mended}` };
    },
  },
  {
    id: 'unity-world', title: 'Unity reimport + world rebuild',
    plan: c => c.mode === 'editor'
      ? ['unity cmd editor status  (must be stopped, not compiling)', 'unity cmd editor invoke_menu --path "Assets/Refresh"', 'unity cmd editor invoke_menu --path "Suborned/World/Build Harbour District (Export)"  (waits for unity/Assets/Scenes/HarbourDistrict.unity to be written)']
      : [`${unityExe()} -batchmode -quit -projectPath unity -executeMethod Suborned.World.HarbourWorldBuilder.Build -world out/world/harbour-district.world.json -logFile out/batch/logs/unity-world.unity.log`],
    async run(c) {
      const t0 = Date.now();
      if (c.mode === 'editor') {
        const st = await editorStatus();
        if (!st) return fail('Unity has the project open but does not answer the Unity CLI; close it, or run with --batchmode after closing');
        if (st.playMode && st.playMode !== 'stopped') return fail(`Unity is in Play (${st.playMode}); stop Play, a rebuild would end the session`);
        const from = logSize();
        if (!(await menu('Assets/Refresh')) || !(await idle(900000))) return fail('Assets/Refresh did not finish (the Editor is still compiling after 15 min, or the CLI refused)');
        await sleep(1500);
        if (!(await menu('Suborned/World/Build Harbour District (Export)'))) return fail('the Unity CLI could not run the menu Suborned/World/Build Harbour District (Export)');
        if (!(await waitFile(SCENE, t0, 1800000))) return fail('the scene was not written within 30 min');
        const bad = logProblems(logSince(from));
        return bad.length ? fail(`the Editor logged problems during the rebuild:\n${bad.join('\n')}`) : { ok: true, detail: `scene rebuilt from the export in ${Math.round((Date.now() - t0) / 1000)} s (Editor)` };
      }
      const r = await sh('unity-world', [unityExe(), '-batchmode', '-quit', '-projectPath', UNITY, '-executeMethod', 'Suborned.World.HarbourWorldBuilder.Build', '-world', 'out/world/harbour-district.world.json', '-logFile', join(OUT, 'logs/unity-world.unity.log')], { timeoutMs: 3600000 });
      const log = existsSync(join(OUT, 'logs/unity-world.unity.log')) ? readFileSync(join(OUT, 'logs/unity-world.unity.log'), 'utf8') : '';
      const fp = log.match(/fingerprint (\w+)/)?.[1];
      return r.code === 0 && existsSync(SCENE) && statSync(SCENE).mtimeMs > t0 ? { ok: true, detail: `scene rebuilt from the export in ${Math.round((Date.now() - t0) / 1000)} s (batchmode)${fp ? `, fingerprint ${fp}` : ''}` } : fail(`Unity batchmode exit ${r.code}${logProblems(log).length ? '\n' + logProblems(log).join('\n') : ''}\n(log out/batch/logs/unity-world.unity.log)`);
    },
  },
  {
    id: 'unity-match', title: 'Unity scenarios match headless',
    plan: c => c.mode === 'editor' ? ['unity cmd editor invoke_menu --path "Suborned/Sim/Run Scenarios (Unity vs headless)"  (waits for out/sim-unity/unity-editor.json)'] : [`${unityExe()} -batchmode -quit -projectPath unity -executeMethod Suborned.World.Play.SimScenarios.RunBatch -logFile out/batch/logs/unity-match.unity.log`],
    async run(c) {
      const t0 = Date.now();
      if (c.mode === 'editor') {
        if (!(await menu('Suborned/Sim/Run Scenarios (Unity vs headless)'))) return fail('the Unity CLI could not run Suborned/Sim/Run Scenarios');
        if (!(await waitFile(MATCH, t0, 1200000, 1500))) return fail('out/sim-unity/unity-editor.json was not written within 20 min');
      } else {
        const r = await sh('unity-match', [unityExe(), '-batchmode', '-quit', '-projectPath', UNITY, '-executeMethod', 'Suborned.World.Play.SimScenarios.RunBatch', '-logFile', join(OUT, 'logs/unity-match.unity.log')], { timeoutMs: 1800000 });
        if (r.code !== 0 && !existsSync(MATCH)) return fail(`Unity batchmode exit ${r.code} (log out/batch/logs/unity-match.unity.log)`);
      }
      return matchResult(MATCH, t0);
    },
  },
  {
    id: 'game-build', title: 'player build', plan: c => [c.rebuild ? 'bun tools/game/cli.ts --build --proof-plan out/build/play/proofplan.json  (--rebuild)' : 'skipped when nothing under unity/Assets, ProjectSettings or Packages is newer than the app; else bun tools/game/cli.ts --build --proof-plan out/build/play/proofplan.json'],
    async run(c) {
      const cur = c.rebuild ? undefined : appCurrent();
      if (cur) { c.facts.build = readJson(join(ROOT, 'out/build/play/build.json')); c.facts.rebuildSkipped = cur; return { ok: true, detail: `NOT REBUILT, no build number spent: ${cur}` }; }
      const plan = makePlan(c); c.facts.plan = plan; for (const l of planLines(plan)) c.say(l); // the snapshot is taken here, on what this build reads: after the world steps, before Unity
      const t0 = Date.now(), r = await sh('game-build', ['bun', 'tools/game/cli.ts', '--build', '--proof-plan', planFile(BUILD_DIR)], { timeoutMs: 3600000 });
      const b = readJson(join(ROOT, 'out/build/play/build.json'));
      if (r.code !== 0 || !b || b.result !== 'Succeeded' || Date.parse(b.at) < t0 - 5000) return fail('game build failed', r);
      c.facts.build = b;
      return { ok: true, detail: `Build ${b.version} (${b.recording}, ${b.sha}${b.dirty ? '+' : ''}) in ${b.seconds} s, ${b.errors} errors; ${planLines(plan)[0]}` };
    },
  },
  {
    id: 'game-check', perf: true, title: 'player proof', plan: () => ['bun tools/game/cli.ts --check --reuse-proof --proof-plan out/build/play/proofplan.json  (the proof the build just took is reused; with no build in this batch, a new proof of what the plan runs)', 'a section that passes alone after failing in company runs once more after what ran before it'],
    async run(c) {
      // the build took the proof; with no build in this batch (--from game-check, or the app is current) a proof is taken only if the plan has anything to run
      let plan = (c.facts.plan as Plan | undefined) ?? makePlan(c), reuse = !!c.facts.plan || (plan.mode === 'delta' && !plan.only.length), r: Ran, round = 0, owed: string[] = [];
      for (;;) {
        r = await sh('game-check', ['bun', 'tools/game/cli.ts', '--check', ...(reuse ? ['--reuse-proof'] : []), '--proof-plan', planFile(BUILD_DIR)], { timeoutMs: 1800000, append: round > 0 });
        owed = owedCompany(readLedger(ledgerFile(BUILD_DIR)));
        if (r.code !== 0 || !owed.length || ++round > 1) break;
        c.say(`game-check: ${owed.join(', ')} passed alone; now after what ran before ${owed.length === 1 ? 'it' : 'them'}`);
        plan = makePlan(c); reuse = false;
      }
      const scen = r.out.match(/scenarios (\d+)\/(\d+) match headless[^\n]*/)?.[0];
      c.facts.proof = readJson(PROOF);
      const gate = readJson(gateFile(BUILD_DIR)) as ProofGate | undefined; c.facts.gate = gate;
      return r.code === 0 ? { ok: true, detail: `${scen ?? 'scenarios ok'}; ${sessionWords(r.out)}${gate ? `; ${gateWords(gate)}` : ''}` } : fail(`game:check failed${gate?.failed.length ? ` (${gate.failed.join(', ')})` : ''}`, r);
    },
  },
  {
    id: 'shots', perf: true, title: 'shots in the player', plan: c => [`resolve ${rel(c.shotsFile)} → out/batch/shots.resolved.json`, `"${join(APP, 'Contents/MacOS/The Suborned')}" -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -shots out/batch/shots.resolved.json ${rel(c.shotsOut)} -novsync -logFile ${rel(c.shotsOut)}/player.log`],
    async run(c) {
      let resolved: ResolvedFile;
      try { resolved = prepare(c); } catch (e) { return fail((e as Error).message); }
      const exe = join(APP, 'Contents/MacOS/The Suborned');
      if (!existsSync(exe)) return fail(`no built player at ${rel(APP)}; run the game-build step`);
      const t0 = Date.now();
      const r = await launchPlayer(c, 'shots', ['-shots', join(OUT, 'shots.resolved.json'), c.shotsOut, '-novsync', '-logFile', join(c.shotsOut, 'player.log')], [1920, 1080], { timeoutMs: 1800000 });
      const rep = readJson(join(c.shotsOut, 'shots.json')) as Report | undefined;
      if (!rep) return fail(`the player wrote no shots.json (exit ${r.code}); see ${rel(c.shotsOut)}/player.log`);
      c.facts.shots = rep; c.facts.resolved = resolved;
      const j = judgeShots(resolved, rep, c.shotsOut, (Date.now() - t0) / 1000);
      return { ...j, detail: `${j.detail}\n${launchText('shots')}` };
    },
  },
  {
    id: 'budget', title: 'HUD budget + layout audit', plan: c => [`bun tools/hud-budget/cli.ts ${rel(c.shotsOut)} --skip-unmasked`, 'the layout audit recorded with each HUD shot (HudToolkit.AuditText)', `the proof's pictures of each set in tools/game/proofset.json whose section the proof has (${Object.keys(loadProofSet().sets).join(', ')}), from ${rel(dirname(PROOF))} at 1920x1080: bun tools/game/proofset.ts budget`],
    async run(c) {
      // `--only budget` / `--from budget` re-judge the last shots without retaking them (Splice, 2026-10-26): the report the shots step left in the shots folder
      const rep = (c.facts.shots ?? readJson(join(c.shotsOut, 'shots.json'))) as Report | undefined;
      if (!rep) return fail(`no shots report (the shots step did not run in this batch, and ${rel(c.shotsOut)} has no shots.json)`);
      const r = await sh('budget', ['bun', 'tools/hud-budget/cli.ts', c.shotsOut, '--skip-unmasked']);
      const lines = r.out.split('\n'), pass = lines.filter(l => l.startsWith('PASS')).length, bad = lines.filter(l => l.startsWith('FAIL'));
      const audited = rep.shots.filter(s => s.audit), dirty = audited.filter(s => !s.audit!.clean);
      const sets = judgeProofSets(readJson(PROOF) ?? c.facts.proof, undefined, undefined, undefined, (readJson(gateFile(BUILD_DIR)) as ProofGate | undefined)?.skips);
      const problems = [...bad.map(l => l.slice(0, 160)), ...dirty.flatMap(s => s.audit!.issues.slice(0, 3).map(i => `audit ${s.name}: ${i}`)), ...sets.flatMap(x => x.problems.map(p => `${x.name}: ${p}`))];
      const proofText = sets.map(x => x.detail).join('; ');
      return problems.length ? fail(`budget ${pass} pass, ${bad.length} over; audit ${audited.length - dirty.length}/${audited.length} clean${proofText ? `; ${proofText}` : ''}\n${problems.slice(0, 10).join('\n')}`) : { ok: r.code === 0, detail: `budget ${pass} shots within budget; layout audit ${audited.length}/${audited.length} clean${proofText ? `; ${proofText}` : ''}` };
    },
  },
  {
    id: 'late-fight', perf: true, title: 'late-fight frame times', plan: () => [`resolve tools/game/late.json → out/batch/late.resolved.json (each script's end from the engine)`, `"${join(APP, 'Contents/MacOS/The Suborned')}" -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -late out/batch/late.resolved.json out/batch/late -novsync -logFile out/batch/late/player.log  (one launch, every row)`, 'fail: a frame > 50 ms tied to a save, order or the end, p99 > 16.7 ms, > 100 ms in the 2 s after the mission ends; each only if it comes back on a second run (brief 045)'],
    async run(c) {
      const exe = join(APP, 'Contents/MacOS/The Suborned');
      if (!existsSync(exe)) return fail(`no built player at ${rel(APP)}; run the game-build step`);
      if (!playerHasLate()) return fail(`the built player has no -late mode yet (its code lacks "${LATE_MARKER}"): Latch adds the hook, contract in briefs/045-late-lag.md ("The player's hook"); --skip late-fight until then`);
      const dir = join(OUT, 'late'); rmSync(dir, { recursive: true, force: true }); mkdirSync(dir, { recursive: true });
      let resolved: LateResolvedFile;
      try { resolved = resolveLate(onlyRows(JSON.parse(readFileSync(LATE_FILE, 'utf8')), c.rows), dir, engineEnd); } catch (e) { return fail((e as Error).message); }
      writeFileSync(join(OUT, 'late.resolved.json'), JSON.stringify(resolved, null, 1) + '\n');
      // each run is a fresh player over the rows it is given; a row with findings goes once more and fails only if the same kind of finding returns (late.ts: runLate)
      const v = await runLate(resolved, async (rows, n) => {
        const d = n === 1 ? dir : join(OUT, `late-${n}`), rf = join(OUT, n === 1 ? 'late.resolved.json' : `late.resolved-${n}.json`);
        rmSync(d, { recursive: true, force: true }); mkdirSync(d, { recursive: true });
        writeFileSync(rf, JSON.stringify({ ...rows, outDir: d }, null, 1) + '\n');
        if (n > 1) c.say(`late-fight: run ${n - 1} had findings in ${rows.rows.map(r => r.name).join(', ')}; running them again`);
        const watch = watchLoad();
        const r = await launchPlayer(c, 'late-fight', ['-late', rf, d, '-novsync', '-logFile', join(d, 'player.log')], sizeOf(rows.rows), { timeoutMs: 480000, append: n > 1 }); // every row in one launch
        const load = await watch.stop();
        const report = readJson(join(d, 'late.json')) as LateReport | undefined;
        if (!report) c.say(`late-fight: the player wrote no late.json (exit ${r.code}); see ${rel(d)}/player.log`);
        return { report, load };
      });
      c.facts.late = v.report; c.facts.lateNotes = v.notes;
      writeFileSync(join(OUT, 'late.verdict.json'), JSON.stringify({ ok: v.ok, problems: v.problems, notes: v.notes, noisy: v.noisy, limits: LIMITS, attempts: v.attempts.map(a => ({ n: a.n, rows: a.rows, load: a.load, findings: a.findings, strays: a.strays, lines: a.lines, report: a.report })) }, null, 1) + '\n');
      const ran = v.attempts.length > 1 ? ` (${v.attempts.length} runs)` : '';
      const tailNotes = [...v.notes.filter(n => !/^run \d/.test(n)), `${v.attempts.map(a => `run ${a.n}: ${loadText(a.load)}`).join(' | ')}`, `${launchText('late-fight')} (${resolved.rows.length} row${resolved.rows.length === 1 ? '' : 's'})`];
      return v.ok ? { ok: true, detail: `${[...v.lines, ...tailNotes].join('\n')}${ran ? '\n' + ran.trim() : ''}` } : fail(`${v.problems.length} problem${v.problems.length === 1 ? '' : 's'}${ran}:\n${[...v.problems, ...v.lines, ...tailNotes].join('\n')}`);
    },
  },
  {
    id: 'tp-long', perf: true, title: 'draw distance (third person)',
    plan: () => [`resolve tools/game/tplong.json → out/batch/tplong.resolved.json`, `"${join(APP, 'Contents/MacOS/The Suborned')}" -screen-fullscreen 0 -screen-width 2880 -screen-height 1864 -tplong out/batch/tplong.resolved.json out/batch/tplong -novsync -logFile out/batch/tplong/player.log  (one launch, every row)`,
      `fail: third person over ${GATES.third.triangles / 1e6} M triangles, SetPass ${GATES.third.setPass}, shadow casters ${GATES.third.shadowCasters}, renderers drawn ${GATES.third.drawn} (${GATES.third.beyond150} beyond 150 m), mean ${GATES.third.meanMs} ms, p99 ${GATES.third.p99Ms} ms; play view over ${GATES.play.triangles / 1e6} M triangles or ${GATES.play.meanMs} ms (brief 048)`],
    async run(c) {
      const exe = join(APP, 'Contents/MacOS/The Suborned');
      if (!existsSync(exe)) return fail(`no built player at ${rel(APP)}; run the game-build step`);
      if (!playerHasTpLong()) return { ok: true, skipped: true, detail: `the built player has no -tplong mode yet (its code lacks "${TPLONG_MARKER}"): Latch adds the hook with the draw-distance work, contract in briefs/048-draw-distance.md ("The player's hook"); this row runs once it is there` };
      const dir = join(OUT, 'tplong'); rmSync(dir, { recursive: true, force: true }); mkdirSync(dir, { recursive: true });
      let resolved;
      try { resolved = resolveTpLong(onlyRows(JSON.parse(readFileSync(TPLONG_FILE, 'utf8')), c.rows), dir); } catch (e) { return fail((e as Error).message); }
      const rf = join(OUT, 'tplong.resolved.json'); writeFileSync(rf, JSON.stringify(resolved, null, 1) + '\n');
      const watch = watchLoad();
      const r = await launchPlayer(c, 'tp-long', ['-tplong', rf, dir, '-novsync', '-logFile', join(dir, 'player.log')], sizeOf(resolved.rows), { timeoutMs: 300000 }); // every row in one launch
      const load = await watch.stop();
      const report = readJson(join(dir, 'tplong.json')) as TpLongReport | undefined;
      if (!report) c.say(`tp-long: the player wrote no tplong.json (exit ${r.code}); see ${rel(dir)}/player.log`);
      const v = judgeTpLong(resolved, report, load);
      c.facts.tplong = v.lines;
      writeFileSync(join(OUT, 'tplong.verdict.json'), JSON.stringify({ ok: v.ok, problems: v.problems, notes: v.notes, gates: GATES, load, lines: v.lines, report }, null, 1) + '\n');
      const tail = [...v.notes, loadText(load), `${launchText('tp-long')} (${resolved.rows.length} row${resolved.rows.length === 1 ? '' : 's'})`];
      return v.ok ? { ok: true, detail: `${[...v.lines, ...tail].join('\n')}` } : fail(`${v.problems.length} problem${v.problems.length === 1 ? '' : 's'}:\n${[...v.problems, ...v.lines, ...tail].join('\n')}`);
    },
  },
  {
    id: 'perf064', perf: true, title: 'perf probe (Redline 064)',
    plan: c => [c.perf064 ? 'asked for: runs' : 'SKIPPED unless --perf064 or --only perf064', `"${join(APP, 'Contents/MacOS/The Suborned')}" -screen-fullscreen 0 -screen-width 2880 -screen-height 1864 -perf064 out/batch/perf064 -capture-size 2880x1864 -novsync -perf-warm 30 -perf-seconds 20 -logFile out/batch/perf064/player.log`, 'measures only: no gate on ms; the load around the probe and per window is printed with it, NOISY MACHINE when the 1-minute load reached 0.6 of the cores (brief 064 R7)'],
    async run(c) {
      if (!c.perf064) return { ok: true, skipped: true, detail: 'opt-in, about three minutes of the player: --perf064 or --only perf064' };
      const exe = join(APP, 'Contents/MacOS/The Suborned');
      if (!existsSync(exe)) return fail(`no built player at ${rel(APP)}; run the game-build step`);
      if (!playerHasPerf064()) return { ok: true, skipped: true, detail: `the built player has no -perf064 mode (its code lacks "${PERF064_MARKER}")` };
      const dir = join(OUT, 'perf064'); rmSync(dir, { recursive: true, force: true }); mkdirSync(dir, { recursive: true });
      const watch = watchLoad();
      const r = await launchPlayer(c, 'perf064', ['-perf064', dir, '-capture-size', '2880x1864', '-novsync', '-perf-warm', '30', '-perf-seconds', '20', '-logFile', join(dir, 'player.log')], [2880, 1864], { timeoutMs: 420000 });
      const load = await watch.stop();
      const rep = readJson(join(dir, 'perf064.json')) as Perf064Report | undefined;
      if (!rep?.windows?.length) return fail(`the player wrote no perf064.json windows (exit ${r.code}); see ${rel(dir)}/player.log`);
      const v = perf064Lines(rep, loadNoisy(load));
      c.facts.perf064 = [...v.lines, ...v.notes, loadText(load)];
      writeFileSync(join(OUT, 'perf064.verdict.json'), JSON.stringify({ load, noisy: v.noisy, notes: v.notes, lines: v.lines, report: rep }, null, 1) + '\n');
      return { ok: true, detail: `${rep.windows.length} windows at ${rep.screen ?? '?'}${v.noisy ? ' · NOISY MACHINE, ms not judged' : ''}; ${launchText('perf064')}` };
    },
  },
];

/** The newest file under the folders a player is built from, and its time. */
export function newestInput(roots: string[]): { at: number; what: string } {
  let at = 0, what = '';
  const walk = (d: string) => { for (const e of readdirSync(d, { withFileTypes: true })) { const p = join(d, e.name); if (e.isDirectory()) walk(p); else { const m = statSync(p).mtimeMs; if (m > at) { at = m; what = p; } } } };
  for (const r of roots) if (existsSync(r)) walk(r);
  return { at, what };
}
/** Why the built app is current (nothing under unity/Assets, ProjectSettings or Packages is newer than its build), or undefined when it needs building. */
export function appCurrent(o: { app?: string; stamp?: string; roots?: string[] } = {}): string | undefined {
  const app = o.app ?? APP, stamp = o.stamp ?? join(ROOT, 'out/build/play/build.json');
  if (!existsSync(app) || !existsSync(stamp)) return undefined;
  const b = readJson(stamp); if (b?.result !== 'Succeeded') return undefined;
  const at = statSync(stamp).mtimeMs, n = newestInput(o.roots ?? ['Assets', 'ProjectSettings', 'Packages'].map(d => join(UNITY, d)));
  return n.at > at ? undefined : `${b.version ?? 'the app'} is newer than every file under unity/Assets, ProjectSettings and Packages (latest ${rel(n.what) || 'none'}); --rebuild to build anyway`;
}

function unityExe(): string {
  const ver = readFileSync(join(UNITY, 'ProjectSettings/ProjectVersion.txt'), 'utf8').match(/m_EditorVersion: (\S+)/)?.[1];
  return `/Applications/Unity/Hub/Editor/${ver}/Unity.app/Contents/MacOS/Unity`;
}
const rel = (p: string) => p.startsWith(ROOT + '/') ? p.slice(ROOT.length + 1) : p;
const readJson = (f: string): any => { try { return JSON.parse(readFileSync(f, 'utf8')); } catch { return undefined; } };

/** The proof sets (tools/game/proofset.json) the proof has a section for, judged at 1x by the HUD budget; a set whose section the proof lacks is an older build's, said and not failed.
 *  `proof` is proof.json; `dir` the folder its pictures are in. */
export function judgeProofSets(proof: any, dir = dirname(PROOF), stage = join(OUT, 'proofset'), judge: typeof judgeProofBudget = judgeProofBudget, skips: Record<string, string> = {}): (ProofBudget & { name: string })[] {
  return Object.entries(loadProofSet().sets).map(([name, s]) => proof?.frames?.[s.section] === undefined
    ? { name, ok: true, detail: `proof set ${name}: not in this proof (an older build)`, problems: [], judged: 0, pass: 0 }
    : proof.frames[s.section] === null && skips[s.section] // a delta proof did not run it: its pictures in the folder are an earlier build's, so they are not judged as this one's
      ? { name, ok: true, detail: `proof set ${name}: ${skips[s.section]}`, problems: [], judged: 0, pass: 0 }
      : { name, ...judge(dir, name, join(stage, name)) });
}

// ---------------------------------------------------------------- delta proofs and going on after a failure (brief 078)

/** What cli.ts leaves after a check with a plan (proofgate.json). */
export type ProofGate = { mode: 'full' | 'delta'; why: string; build?: string; ran: string[]; skips: Record<string, string>; failed: string[]; notReached: string[]; owed: string[] };
const makePlan = (c: Ctx): Plan => { const m = loadMap(), s = snapshot(ROOT, m), p = planProof(m, s, readLedger(ledgerFile(BUILD_DIR)), { force: c.fullProof }); writePlan(BUILD_DIR, p, s); return p; };
/** The game-check row's words for the two replays, from the check's log. */
export function sessionWords(out: string): string {
  const said = (k: string) => new RegExp(`game: ${k} replays headless`).test(out) ? 'replays' : new RegExp(`game: ${k} DOES NOT replay`).test(out) ? 'DOES NOT replay' : new RegExp(`game: ${k}: not run`).test(out) ? 'not run' : '?';
  return `driven session ${said('driveSession')}, fire session ${said('fireSession')}`;
}
export function gateWords(g: ProofGate): string {
  const n = Object.keys(g.skips).length, by = Object.values(g.skips)[0];
  return g.mode === 'full' ? `full proof (${g.why})` : `delta proof: ${g.ran.length} section${g.ran.length === 1 ? '' : 's'} ran${n ? `, ${n} ${by}` : ''}`;
}

/** The batch's last failure, kept to go on from: out/batch/last.json. */
export type LastRun = { failedStep: string; failedSections: string[]; build?: string; snapshot: string; at: string };
/** The step a bare `bun run batch` starts at: the last failed step, while the tree and the player are the ones it failed on; else the top. */
export function resumeFrom(last: LastRun | undefined, snapshotNow: string, buildNow: string | undefined): string | undefined {
  return last && last.snapshot === snapshotNow && last.build === buildNow ? last.failedStep : undefined;
}
/** The screen's last line after a failure: the command that goes on from it. */
export function nextCommand(step: string, sections: string[] = []): string {
  return `next    bun run batch --from ${step}  (same player while the sources are unchanged${sections.length ? `; re-runs ${sections.join(', ')} alone first` : ''}; a bare bun run batch does the same, --restart runs from the top)`;
}

/** Unity's scenarios against headless, from the file the match wrote after `since`. */
export function matchResult(file: string, since: number): StepResult {
  const j = readJson(file);
  if (!j || (existsSync(file) && statSync(file).mtimeMs < since)) return fail(`${rel(file)} is missing or older than this step`);
  if (j.error) return fail(`Unity could not play the scenarios: ${j.error}`);
  const bad = (j.rows ?? []).filter((r: any) => !r.matchesHeadless || !r.replayIdentical || r.why).map((r: any) => `${r.id}: ${r.why || (r.matchesHeadless ? 'replay differs' : 'differs from headless')}`);
  return j.passed ? { ok: true, detail: `${j.matching}/${j.scenarios} scenarios match headless (${j.runtime ?? 'Editor'}, DLL ${String(j.dllSha256).slice(0, 12)})` }
    : fail(`${j.matching}/${j.scenarios} match headless${bad.length ? `; ${bad.slice(0, 6).join('; ')}` : ''}`);
}

// ---------------------------------------------------------------- shots

export type Report = { status: string; screen: string; shots: { name: string; status: string; error: string; files: string[]; tick: number; audit?: { clean: boolean; issues: string[] }; frames?: { count: number; meanMs: number; fps: number; p99Ms: number; gpuMs: number }; clipFrames?: number }[] };

/** The batch's shots live in one rotating place (brief 050): out/batch/shots is the newest run, shots-prev1 and shots-prev2 the two before it, the one before that is deleted.
 *  A run written elsewhere (--shots-out) is cleared as before; the folder is the caller's. */
export function rotateShots(dir: string, keep = 2, root = OUT): void {
  const abs = resolve(dir);
  if (!existsSync(abs)) return;
  if (!abs.startsWith(root + '/')) { rmSync(abs, { recursive: true, force: true }); return; }
  rmSync(`${abs}-prev${keep}`, { recursive: true, force: true });
  for (let i = keep - 1; i >= 1; i--) if (existsSync(`${abs}-prev${i}`)) renameSync(`${abs}-prev${i}`, `${abs}-prev${i + 1}`);
  if (readdirSync(abs).length) renameSync(abs, `${abs}-prev1`); else rmSync(abs, { recursive: true, force: true });
}
function prepare(c: Ctx): ResolvedFile {
  const raw = JSON.parse(readFileSync(c.shotsFile, 'utf8'));
  const sc = scenarioExporter();
  const resolved = resolveShots(raw, c.shotsOut, fileResolver(sc));
  rotateShots(c.shotsOut); mkdirSync(c.shotsOut, { recursive: true });
  mkdirSync(OUT, { recursive: true }); writeFileSync(join(OUT, 'shots.resolved.json'), JSON.stringify(resolved, null, 1) + '\n');
  return resolved;
}
/** A scenario id as a script on disk, from the headless engine (`sim script <id>`); only called for a shot that names one. */
function scenarioExporter() {
  return (id: string) => {
    const dir = join(OUT, 'scripts'); mkdirSync(dir, { recursive: true });
    const dll = join(ROOT, 'out/missions/engine/tree/bin/suborned-sim.dll');
    if (!existsSync(dll)) return undefined;
    const r = Bun.spawnSync(['dotnet', dll, 'script', id], { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
    if (r.exitCode !== 0) return undefined;
    const path = join(dir, `${id}.script.json`); writeFileSync(path, r.stdout.toString());
    const j = JSON.parse(r.stdout.toString());
    return { path, info: { start: j.start ?? {}, until: j.until ?? 0 } };
  };
}

/** Did the player take every shot the file asked for, and leave its files? */
export function judgeShots(resolved: ResolvedFile, rep: Report, dir: string, seconds: number): StepResult {
  const problems: string[] = [];
  for (const s of resolved.shots) {
    const r = rep.shots.find(x => x.name === s.name);
    if (!r) { problems.push(`${s.name}: not taken (the player stopped before it)`); continue; }
    if (r.status === 'failed') { problems.push(`${s.name}: ${r.error}`); continue; }
    for (const f of expectedFiles(s)) {
      const p = join(dir, f);
      if (!existsSync(p)) { problems.push(`${s.name}: ${f} is missing`); continue; }
      const size = f.endsWith('.png') ? pngFileSize(p) : undefined; // a size is 1920 x 1080 unless the row says: the budget and the audit are defined there
      if (size && (size[0] !== s.w || size[1] !== s.h)) problems.push(`${s.name}: ${f} is ${size[0]}x${size[1]}, the row says ${s.w}x${s.h}`);
    }
  }
  const clips = resolved.shots.filter(s => s.clipSeconds > 0).length, huds = resolved.shots.filter(s => s.hud && !s.clipSeconds).length;
  if (rep.status === 'unreadable' || /^no host/.test(rep.status)) problems.push(`player: ${rep.status}`);
  return problems.length ? fail(`${problems.length} of ${resolved.shots.length} shots went wrong:\n${problems.slice(0, 10).join('\n')}`)
    : { ok: true, detail: `${resolved.shots.length} shots (${huds} with HUD layers, ${clips} clip${clips === 1 ? '' : 's'}) in ${seconds.toFixed(0)} s, ${rep.screen}` };
}

// ---------------------------------------------------------------- late-fight (brief 045)

/** `--rows a,b`: the file's rows cut to those names, in the file's order; a name the file lacks is an error (no silent empty run). */
export function onlyRows<T extends { rows: { name: string }[] }>(raw: T, names?: string[]): T {
  if (!names?.length) return raw;
  const miss = names.filter(n => !raw.rows.some(r => r.name === n));
  if (miss.length) throw new Error(`--rows: no row ${miss.join(', ')} (rows: ${raw.rows.map(r => r.name).join(', ')})`);
  return { ...raw, rows: raw.rows.filter(r => names.includes(r.name)) };
}
/** The window a list opens at: its first row's size (CaptureSize then sets each row's own). */
const sizeOf = (rows: { w: number; h: number }[]): [number, number] => [rows[0]?.w ?? 1920, rows[0]?.h ?? 1080];

const LATE_FILE = join(ROOT, 'tools/game/late.json'), LATE_MARKER = 'suborned.late.report/1';
/** Does the built player carry the -late mode? Its report's schema literal is in its code only when Latch's hook is. (Without the hook the player would just open the game and sit; this stops that after a second.) */
export function playerHasLate(managed = join(APP, 'Contents/Resources/Data/Managed/Suborned.Runtime.dll')): boolean {
  if (!existsSync(managed)) return false;
  return readFileSync(managed).includes(Buffer.from(LATE_MARKER, 'utf16le'));
}
const PERF064_MARKER = 'suborned.perf064/1';
/** Does the built player carry the -perf064 probe? Its report's schema literal is in its code only when the probe is. */
export function playerHasPerf064(managed = join(APP, 'Contents/Resources/Data/Managed/Suborned.Runtime.dll')): boolean {
  if (!existsSync(managed)) return false;
  return readFileSync(managed).includes(Buffer.from(PERF064_MARKER, 'utf16le'));
}
const TPLONG_FILE = join(ROOT, 'tools/game/tplong.json');
/** Does the built player carry the -tplong mode? Its report's schema literal is in its code only when Latch's hook is. */
export function playerHasTpLong(managed = join(APP, 'Contents/Resources/Data/Managed/Suborned.Runtime.dll')): boolean {
  if (!existsSync(managed)) return false;
  return readFileSync(managed).includes(Buffer.from(TPLONG_MARKER, 'utf16le'));
}
/** Where a script's mission ends, from the working tree's engine (`play --json`: the tick and outcome the player's drive must reach too). */
function engineEnd(path: string): End {
  const dll = buildEngine(engineAt('tree'));
  const r = Bun.spawnSync(['dotnet', dll, 'play', path, '--json'], { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
  const line = r.stdout.toString().split('\n').find(l => l.startsWith('{'));
  if (!line) throw new Error(`the engine could not play ${rel(path)}: ${r.stderr.toString().trim().slice(0, 200)}`);
  const j = JSON.parse(line);
  return { tick: j.tick, outcome: j.outcome, fail: j.fail ?? '' };
}

/** Machine load while a player runs: the 1-minute load average at the start, its peak while sampled (every 2 s), at the end; and the busiest other processes at the peak. */
function watchLoad(): { stop(): Promise<LoadSample> } {
  const ncpu = cpus().length, start = loadavg()[0]!; let peak = start, top: string[] = [], busy = false;
  const sample = async () => {
    if (busy) return; busy = true;
    try {
      const l = loadavg()[0]!;
      if (l >= peak) {
        peak = l;
        const p = Bun.spawn(['ps', '-Ao', 'pcpu=,pid=,comm=', '-r'], { stdout: 'pipe', stderr: 'ignore' });
        const text = await new Response(p.stdout).text();
        top = text.split('\n').slice(0, 8).map(x => x.trim().match(/^([\d.]+)\s+(\d+)\s+(.*)$/)).filter((m): m is RegExpMatchArray => !!m && Number(m[1]) >= 25 && !/The Suborned|\/ps$/.test(m[3]!))
          .slice(0, 3).map(m => `${m[3]!.split('/').pop()} ${Math.round(Number(m[1]))}%`);
      }
    } finally { busy = false; }
  };
  const timer = setInterval(sample, 2000);
  return { async stop() { clearInterval(timer); await sample(); return { ncpu, start: Math.round(start * 10) / 10, peak: Math.round(Math.max(peak, loadavg()[0]!) * 10) / 10, end: Math.round(loadavg()[0]! * 10) / 10, top }; } };
}

// ---------------------------------------------------------------- the run and the screen

export function pick(ids: string[], o: { from?: string; only?: string[]; skip?: string[] }): { run: string[]; unknown: string[] } {
  const unknown = [...(o.only ?? []), ...(o.skip ?? []), ...(o.from ? [o.from] : [])].filter(i => !ids.includes(i));
  let run = ids.slice();
  if (o.from) run = run.slice(ids.indexOf(o.from));
  if (o.only?.length) run = run.filter(i => o.only!.includes(i));
  if (o.skip?.length) run = run.filter(i => !o.skip!.includes(i));
  return { run, unknown };
}

/** Run the steps in order and stop at the first that fails. Pure of Unity: the steps come in, so a test passes stubs. */
export async function runSteps(steps: Step[], c: Ctx, run: string[]): Promise<Row[]> {
  const rows: Row[] = [];
  let stopped = false;
  for (const s of steps) {
    if (!run.includes(s.id)) { rows.push({ id: s.id, title: s.title, status: 'skipped', seconds: 0, detail: '' }); continue; }
    if (stopped) { rows.push({ id: s.id, title: s.title, status: 'not run', seconds: 0, detail: 'stopped at the first failure' }); continue; }
    c.say(`▶ ${s.id}: ${s.title}`);
    const t0 = performance.now();
    if (!c.dry && c.guard) { const g = await c.guard(s.id); if (g) { rows.push({ id: s.id, title: s.title, status: 'FAILED', seconds: 0, detail: g }); stopped = true; continue; } }
    let r: StepResult;
    const span0 = s.perf && !c.dry ? spanStart() : undefined; // the load average when a perf step starts and ends (Redline R7)
    try { r = c.dry ? { ok: true, detail: s.plan(c).join('\n') } : await s.run(c); } catch (e) { r = { ok: false, detail: `threw: ${(e as Error).message}` }; }
    rows.push({ id: s.id, title: s.title, status: r.ok ? (r.skipped ? 'skipped' : 'ok') : 'FAILED', seconds: (performance.now() - t0) / 1000, detail: r.skipped ? `skipped: ${r.detail}` : r.detail, ...(span0 && !r.skipped ? { load: spanEnd(span0) } : {}) });
    if (!r.ok) stopped = true;
  }
  return rows;
}

const f1 = (n: number) => (Math.round(n * 10) / 10).toString();
/** The frame times worth one line: play at zoom, the worst yaw, the worst spot, from the proof and the shots. */
export function frameLines(proof: any, rep: Report | undefined): string[] {
  const out: string[] = [], f = proof?.frames;
  if (f) {
    out.push(`play zoom ${f1(f.fps)} fps mean (p99 ${f1(f.p99Ms)} ms) ${proof.framesNote ?? (proof.screen ? `at ${proof.screen}` : '')}`.trim());
    const rows: { name: string; fps: number; p99: number; gpu?: number }[] = [...(f.byYaw ?? []).map((y: any) => ({ name: `yaw ${y.yaw}`, fps: y.fps, p99: y.p99Ms })), ...(f.follow ?? []).map((y: any) => ({ name: y.spot, fps: y.fps, p99: y.p99Ms, gpu: y.gpuMs })), ...(f.people ?? []).map((y: any) => ({ name: y.spot, fps: y.fps, p99: y.p99Ms, gpu: y.gpuMs })), ...(f.drive ?? []).map((y: any) => ({ name: y.spot, fps: y.fps, p99: y.p99Ms, gpu: y.gpuMs }))];
    if (rows.length) { const w = rows.reduce((a, b) => (b.fps < a.fps ? b : a)); out.push(`slowest of ${rows.length} spots: ${w.name} ${f1(w.fps)} fps (p99 ${f1(w.p99)} ms${w.gpu !== undefined && w.gpu >= 0 ? `, gpu ${f1(w.gpu)} ms` : ''}); 60 fps needs p99 under 16.7 ms`); }
  }
  for (const s of rep?.shots ?? []) if (s.frames) out.push(`shot ${s.name}: ${f1(s.frames.fps)} fps (p99 ${f1(s.frames.p99Ms)} ms${s.frames.gpuMs >= 0 ? `, gpu ${f1(s.frames.gpuMs)} ms` : ''})`);
  return out;
}

export function screen(rows: Row[], o: { when: string; mode: string; build?: any; proof?: any; shots?: Report; shotsOut: string; skipped?: string; late?: LateReport; lateNotes?: string[]; tplong?: string[]; perf064?: string[]; launches?: Record<string, number>; gate?: ProofGate }): string {
  const w = Math.max(...rows.map(r => r.id.length));
  const out: string[] = [`bun run batch · ${o.when} · Unity ${o.mode === 'editor' ? 'Editor open (menus through the Unity CLI)' : 'batchmode'}`, ''];
  for (const r of rows) {
    const first = r.detail.split('\n')[0] ?? '';
    out.push(`  ${r.id.padEnd(w)}  ${r.status.padEnd(8)} ${(r.status === 'ok' || r.status === 'FAILED' ? `${r.seconds.toFixed(0)} s` : '').padStart(5)}  ${first}`);
    if (r.status === 'FAILED') for (const l of r.detail.split('\n').slice(1)) out.push(`  ${' '.repeat(w)}  ${' '.repeat(8)} ${' '.repeat(5)}  ${l}`);
    if (r.load) out.push(`  ${' '.repeat(w)}  ${' '.repeat(8)} ${' '.repeat(5)}  ${spanText(r.load)}`); // beside the numbers: a noisy machine shows in the row
  }
  out.push('');
  const bad = rows.find(r => r.status === 'FAILED');
  if (o.skipped) out.push(`build   NOT REBUILT: ${o.skipped}`);
  if (o.build) out.push(`build   ${rel(APP)}  ${o.build.version} (${o.build.recording}, ${o.build.sha}${o.build.dirty ? '+' : ''})`);
  const ran = Object.entries(o.launches ?? launches); // one launch per player step, its whole list in it (late-fight: a second only for a re-run of a row with findings)
  if (ran.length) out.push(`players ${ran.map(([id, n]) => `${id} ${n} launch${n === 1 ? '' : 'es'}`).join(' · ')}`);
  if (o.gate) out.push(`proof   ${gateWords(o.gate)}`);
  if (o.gate?.mode === 'delta') { const by = new Map<string, string[]>(); for (const [s, t] of Object.entries(o.gate.skips)) by.set(t, [...(by.get(t) ?? []), s]); for (const [t, ss] of by) out.push(`proof   ${t}: ${ss.join(', ')}`); }
  for (const l of frameLines(o.proof, o.shots)) out.push(`frames  ${l}`);
  for (const r of o.late?.rows ?? []) if (r.status === 'ok') out.push(`late    ${r.name}: worst ${f1(Math.max(r.window.maxMs, r.tail.maxMs))} ms, p99 ${f1(r.window.p99Ms)} ms, end ${f1(r.tail.maxMs)} ms (last 60 s, ${o.late!.screen})`);
  for (const n of o.lateNotes ?? []) if (/NOISY|not counted|stray/.test(n)) out.push(`late    ${n}`);
  for (const l of o.tplong ?? []) out.push(`tp-long  ${l}`);
  for (const l of o.perf064 ?? []) out.push(`perf064  ${l}`);
  if (o.shots) out.push(`shots   ${rel(o.shotsOut)}/  (${o.shots.shots.length}; shots.json is the report)`);
  if (bad) out.push(nextCommand(bad.id, bad.id === 'game-check' ? o.gate?.failed : []));
  out.push(bad ? `STOPPED at ${bad.id}: ${bad.title}. Log: out/batch/logs/${bad.id}.log` : `BATCH OK: ${rows.filter(r => r.status === 'ok').length} steps in ${rows.reduce((a, r) => a + r.seconds, 0).toFixed(0)} s`);
  return out.join('\n');
}

if (import.meta.main) {
  const args = process.argv.slice(2), flag = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args.splice(i, 2)[1] : undefined; }, bool = (k: string) => { const i = args.indexOf(k); if (i >= 0) args.splice(i, 1); return i >= 0; };
  if (args.includes('--help') || args.includes('-h')) { // the usage comment at the top of this file
    const top: string[] = []; for (const l of readFileSync(import.meta.path, 'utf8').split('\n')) { if (!l.startsWith('//')) break; top.push(l.replace(/^\/\/ ?/, '')); }
    console.log(top.join('\n')); process.exit(0);
  }
  try { takeGuardFlags(args); } catch (e) { console.error((e as Error).message); process.exit(2); }
  const from = flag('--from'), only = flag('--only')?.split(','), skipArg = flag('--skip')?.split(','), shotsFile = resolve(flag('--shots') ?? DEFAULT_FILE), shotsOut = resolve(flag('--shots-out') ?? join(OUT, 'shots'));
  const dry = bool('--dry-run'), noShots = bool('--no-shots'), forceEditor = bool('--editor'), forceBatch = bool('--batchmode'), rebuild = bool('--rebuild'), perf064 = bool('--perf064') || !!only?.includes('perf064'), rowsArg = flag('--rows')?.split(','), behind = bool('--behind'), full = bool('--full'), submission = bool('--submission'), restart = bool('--restart');
  if (args.length || (forceEditor && forceBatch)) { console.error(`unknown or conflicting argument ${args.join(' ') || '--editor --batchmode'}; see the usage line at the top of tools/game/batch.ts`); process.exit(2); }
  const skip = [...(skipArg ?? []), ...(noShots ? ['shots', 'budget'] : [])];
  // a bare run after a failure goes on from the failed step on the same player, while the sources are the ones it failed on (brief 078)
  const lastFile = join(OUT, 'last.json'), proofMap = loadMap(), tree = () => snapshot(ROOT, proofMap).id;
  const bare = !from && !only && !skipArg && !noShots && !rebuild && !full && !submission && !restart;
  const last = readJson(lastFile) as LastRun | undefined, resumed = bare && last ? resumeFrom(last, tree(), readJson(join(BUILD_DIR, 'build.json'))?.version) : undefined;
  if (resumed) console.error(`going on from ${resumed} on the same player: nothing changed since it failed (${last!.at.slice(0, 16)}); --restart runs from the top`);
  const { run, unknown } = pick(STEPS.map(s => s.id), { from: from ?? resumed, only, skip });
  if (unknown.length) { console.error(`unknown step ${unknown.join(', ')}; steps: ${STEPS.map(s => s.id).join(' ')}`); process.exit(2); }
  const mode: Ctx['mode'] = forceEditor ? 'editor' : forceBatch ? 'batchmode' : editorOpen() ? 'editor' : 'batchmode';
  const c: Ctx = {
    dry, mode, rebuild, perf064, rows: rowsArg, behind, shotsFile, shotsOut, facts: {}, say: m => console.error(m), fullProof: submission ? 'a submission build always runs the whole proof' : full ? '--full' : undefined,
    // nothing heavy runs while Sky plays: wait before each step (a look at the process list when nobody does), stop after the timeout (playing.ts)
    guard: async id => { const w = await waitForSky(`batch ${id}`, { cfg: guardConfig }); return w.timedOut ? timeoutText(w) : undefined; },
  };
  if (!dry && run.includes('shots')) { // a bad row stops the batch now, not after the build
    try { resolveShots(JSON.parse(readFileSync(shotsFile, 'utf8')), shotsOut, fileResolver(() => undefined)); } catch (e) { const m = (e as Error).message; if (!/no engine scenario/.test(m)) { console.error(m); process.exit(2); } }
  }
  if (dry && run.includes('shots')) { // the file is checked even when nothing runs
    try { const r = resolveShots(JSON.parse(readFileSync(shotsFile, 'utf8')), shotsOut, fileResolver(() => undefined)); console.error(`shots: ${rel(shotsFile)} is valid, ${r.shots.length} shots`); } catch (e) { const m = (e as Error).message; if (!/no engine scenario/.test(m)) { console.error(m); process.exit(1); } }
  }
  const rows = await runSteps(STEPS, c, run);
  if (dry) { console.log(`bun run batch --dry-run · Unity ${mode === 'editor' ? 'Editor open' : 'batchmode'}\n`); for (const r of rows) console.log(r.status === 'ok' ? `${r.id}  ${r.title}\n${r.detail.split('\n').map(l => '    ' + l).join('\n')}` : `${r.id}  (${r.status})`); if (run.includes('game-build') || run.includes('game-check')) for (const l of planLines(planProof(proofMap, snapshot(ROOT, proofMap), readLedger(ledgerFile(BUILD_DIR)), { force: c.fullProof }))) console.log(l); process.exit(rows.some(r => r.status === 'FAILED') ? 1 : 0); }
  const failed = rows.find(r => r.status === 'FAILED'), gate = c.facts.gate as ProofGate | undefined;
  mkdirSync(OUT, { recursive: true });
  if (failed) writeFileSync(lastFile, JSON.stringify({ failedStep: failed.id, failedSections: failed.id === 'game-check' ? gate?.failed ?? [] : [], build: readJson(join(BUILD_DIR, 'build.json'))?.version, snapshot: tree(), at: new Date().toISOString() } satisfies LastRun, null, 1) + '\n');
  else rmSync(lastFile, { force: true });
  console.log(screen(rows, { gate, when: new Date().toISOString().slice(0, 16).replace('T', ' '), mode, build: c.facts.build ?? readJson(join(ROOT, 'out/build/play/build.json')), proof: c.facts.proof, shots: c.facts.shots as Report | undefined, shotsOut, skipped: c.facts.rebuildSkipped as string | undefined, late: c.facts.late as LateReport | undefined, lateNotes: c.facts.lateNotes as string[] | undefined, tplong: c.facts.tplong as string[] | undefined, perf064: c.facts.perf064 as string[] | undefined }));
  process.exit(rows.some(r => r.status === 'FAILED') ? 1 : 0);
}
