// usage: bun run batch [--force] [--wait-timeout <s>] [--dry-run] [--from <step>] [--only a,b] [--skip a,b] [--editor | --batchmode] [--shots <file>] [--shots-out <dir>] [--no-shots] [--rebuild]
// The one command Latch runs per build (brief 034 item 3). In order, stopping at the first failure:
//   sim          bun tools/sim/unity.ts --tree           the engine, the inputs and the DLL into the Unity project (headless scenarios first)
//   compile      tools/game/runtime-compile.ts           the player's scripts compile (dotnet, no Editor); the shot runner is installed first when it is not
//   kit          bun run kit                             the DecoKit catalogue and the hash-checked copy of adopted pieces
//   world        bun run world                           the plan → world export
//   unity-world  Unity reimport + HarbourWorldBuilder rebuild from the export      (Editor open: its menu, through the Unity CLI; closed: batchmode)
//   unity-match  Unity's scenarios against headless (Suborned/Sim/Run Scenarios)   (same two ways)
//   game-build   bun tools/game/cli.ts --build           the player (it proves the first frame draws); SKIPPED, no build number spent, when nothing under unity/Assets, ProjectSettings or Packages is newer than the app (--rebuild forces it)
//   game-check   bun tools/game/cli.ts --check           scenarios, the proof with frame times, the driven session replayed headless
//   shots        the built player with -shots            tools/game/shots.json: each shot, its HUD layer and manifest, clips (ShotRunner.cs)
//   budget       tools/hud-budget + the layout audit     every HUD shot within budget; every audit clean
//   late-fight   the built player with -late             tools/game/late.json: loud-fight and Sky's lagging session played to the last 60 s, then on at real time with the script's orders, 1080p; a frame over 50 ms tied to a save, order or the end, p99 over 16.7 ms, over 100 ms at the end: a row with findings is run once more and fails only if the same kind returns; the machine's load is recorded (brief 045; tools/game/late.ts)
//   tp-long      the built player with -tplong          tools/game/tplong.json: the two third-person views (Sky's street, the worst heading) and the play view at 2880x1864, ~40 s: triangles, SetPass, shadow casters, renderers drawn and far, mean and p99 against the gates in tplong.ts (brief 048); SKIPPED, with the reason, until the built player has the hook
// Sky is playing (a player of his, no automation flag; playing.ts): before every step the batch waits ("Sky is playing; waiting") up to --wait-timeout seconds (default 1200), then stops; --force goes on.
// then prints one screen: pass/fail per step, seconds, the build, frame times, where the shots are. Logs of every step: out/batch/logs/<step>.log.
// --dry-run prints what each step would run and touches nothing (no Unity, no build). Exit 0 only if every step that ran passed.
import { copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync, renameSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { cpus, homedir, loadavg } from 'node:os';
import { join, resolve } from 'node:path';
import { compile as runtimeCompile } from './runtime-compile';
import { guardConfig, takeGuardFlags, timeoutText, waitForSky } from './playing';
import { GATES, MARKER as TPLONG_MARKER, judgeTpLong, resolveTpLong, type TpLongReport } from './tplong';
import { LIMITS, loadText, resolveLate, runLate, type End, type LateReport, type LateResolvedFile, type LoadSample } from './late';
import { build as buildEngine, engineAt } from '../missions/check/engine';
import { DEFAULT_FILE, ROOT, expectedFiles, fileResolver, pngFileSize, resolveShots, type ResolvedFile } from './shots';

const UNITY = join(ROOT, 'unity'), OUT = join(ROOT, 'out/batch'), APP = join(ROOT, 'out/build/play/The Suborned.app'), PROOF = join(ROOT, 'out/build/play/proof/proof.json');
const SCENE = join(UNITY, 'Assets/Scenes/HarbourDistrict.unity'), MATCH = join(ROOT, 'out/sim-unity/unity-editor.json');
const FILES = ['ShotRunner.cs', 'CaptureSize.cs'].map(f => [join(ROOT, 'tools/game/unity', f), join(UNITY, 'Assets/Suborned/Runtime/Play', f)] as const);
const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };

export type StepResult = { ok: boolean; detail: string; /** the step has nothing to run yet: shown as skipped, with the reason, and the batch goes on */ skipped?: boolean };
export type Ctx = {
  dry: boolean; mode: 'editor' | 'batchmode'; shotsFile: string; shotsOut: string;
  /** build the player even when nothing it is made from is newer than the app */
  rebuild?: boolean;
  /** what the steps hand each other */
  facts: Record<string, unknown>;
  say(m: string): void;
  /** called before each step runs; a text means Sky kept playing past the timeout: the step fails with it (playing.ts) */
  guard?(stepId: string): Promise<string | undefined>;
};
export type Step = { id: string; title: string; plan(c: Ctx): string[]; run(c: Ctx): Promise<StepResult> };
export type Row = { id: string; title: string; status: 'ok' | 'FAILED' | 'skipped' | 'not run'; seconds: number; detail: string };

const sleep = (ms: number) => new Promise(r => setTimeout(r, ms));
const tail = (s: string, n = 12) => s.trim().split('\n').slice(-n).join('\n');

// ---------------------------------------------------------------- running things

type Ran = { code: number; out: string };
/** Run a command, its output to out/batch/logs/<id>.log (and returned). */
async function sh(id: string, cmd: string[], o: { cwd?: string; timeoutMs?: number; append?: boolean } = {}): Promise<Ran> {
  mkdirSync(join(OUT, 'logs'), { recursive: true });
  const p = Bun.spawn(cmd, { cwd: o.cwd ?? ROOT, env, stdout: 'pipe', stderr: 'pipe' });
  const timer = o.timeoutMs ? setTimeout(() => p.kill(), o.timeoutMs) : undefined;
  const [out, err, code] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]);
  if (timer) clearTimeout(timer);
  const text = out + (err ? `\n--- stderr ---\n${err}` : '');
  writeFileSync(join(OUT, 'logs', `${id}.log`), (o.append && existsSync(join(OUT, 'logs', `${id}.log`)) ? readFileSync(join(OUT, 'logs', `${id}.log`), 'utf8') : '') + `$ ${cmd.join(' ')}\n${text}\n(exit ${code})\n`);
  return { code, out: text };
}
const fail = (what: string, r?: Ran): StepResult => ({ ok: false, detail: r ? `${what}\n${tail(r.out)}` : what });

/** The Editor with this project open, if any (pgrep; the Unity CLI answers for the rest). */
export function editorOpen(): boolean {
  const ps = Bun.spawnSync(['pgrep', '-fil', 'Unity.app/Contents/MacOS/Unity'], { stdout: 'pipe', stderr: 'pipe' }).stdout.toString();
  return ps.split('\n').some(l => l.toLowerCase().includes(UNITY.toLowerCase()) && !l.includes('-batchmode'));
}
async function editorStatus(): Promise<{ playMode?: string; compiling?: boolean } | null> {
  const r = await sh('unity-cli', ['unity', 'cmd', 'editor_status']);
  const line = r.out.split('\n').find(l => l.includes('"playMode"'));
  if (!line) return null;
  try { return JSON.parse(line.slice(line.indexOf('{'))); } catch { return null; }
}
async function menu(path: string): Promise<boolean> { return (await sh('unity-cli', ['unity', 'cmd', 'menu', '--path', path], { append: true })).code === 0; }
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
    id: 'world', title: 'plan to world export', plan: () => ['bun run world'],
    async run() { const r = await sh('world', ['bun', 'run', 'world']); return r.code === 0 ? { ok: true, detail: tail(r.out, 1).slice(0, 110) } : fail('bun run world failed', r); },
  },
  {
    id: 'unity-world', title: 'Unity reimport + world rebuild',
    plan: c => c.mode === 'editor'
      ? ['unity cmd editor_status  (must be stopped, not compiling)', 'unity cmd menu --path "Assets/Refresh"', 'unity cmd menu --path "Suborned/World/Build Harbour District (Export)"  (waits for unity/Assets/Scenes/HarbourDistrict.unity to be written)']
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
    plan: c => c.mode === 'editor' ? ['unity cmd menu --path "Suborned/Sim/Run Scenarios (Unity vs headless)"  (waits for out/sim-unity/unity-editor.json)'] : [`${unityExe()} -batchmode -quit -projectPath unity -executeMethod Suborned.World.Play.SimScenarios.RunBatch -logFile out/batch/logs/unity-match.unity.log`],
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
    id: 'game-build', title: 'player build', plan: c => [c.rebuild ? 'bun tools/game/cli.ts --build  (--rebuild)' : 'skipped when nothing under unity/Assets, ProjectSettings or Packages is newer than the app; else bun tools/game/cli.ts --build'],
    async run(c) {
      const cur = c.rebuild ? undefined : appCurrent();
      if (cur) { c.facts.build = readJson(join(ROOT, 'out/build/play/build.json')); c.facts.rebuildSkipped = cur; return { ok: true, detail: `NOT REBUILT, no build number spent: ${cur}` }; }
      const t0 = Date.now(), r = await sh('game-build', ['bun', 'tools/game/cli.ts', '--build'], { timeoutMs: 3600000 });
      const b = readJson(join(ROOT, 'out/build/play/build.json'));
      if (r.code !== 0 || !b || b.result !== 'Succeeded' || Date.parse(b.at) < t0 - 5000) return fail('game build failed', r);
      c.facts.build = b;
      return { ok: true, detail: `Build ${b.version} (${b.recording}, ${b.sha}${b.dirty ? '+' : ''}) in ${b.seconds} s, ${b.errors} errors` };
    },
  },
  {
    id: 'game-check', title: 'player proof', plan: () => ['bun tools/game/cli.ts --check --reuse-proof  (the proof the build just took is reused)'],
    async run(c) {
      const r = await sh('game-check', ['bun', 'tools/game/cli.ts', '--check', '--reuse-proof'], { timeoutMs: 1800000 });
      const scen = r.out.match(/scenarios (\d+)\/(\d+) match headless[^\n]*/)?.[0], drive = /driven session replays headless/.test(r.out);
      c.facts.proof = readJson(PROOF);
      return r.code === 0 ? { ok: true, detail: `${scen ?? 'scenarios ok'}; driven session ${drive ? 'replays' : '?'}` } : fail('game:check failed', r);
    },
  },
  {
    id: 'shots', title: 'shots in the player', plan: c => [`resolve ${rel(c.shotsFile)} → out/batch/shots.resolved.json`, `"${join(APP, 'Contents/MacOS/The Suborned')}" -shots out/batch/shots.resolved.json ${rel(c.shotsOut)} -novsync -logFile ${rel(c.shotsOut)}/player.log`],
    async run(c) {
      let resolved: ResolvedFile;
      try { resolved = prepare(c); } catch (e) { return fail((e as Error).message); }
      const exe = join(APP, 'Contents/MacOS/The Suborned');
      if (!existsSync(exe)) return fail(`no built player at ${rel(APP)}; run the game-build step`);
      const t0 = Date.now();
      const r = await sh('shots', [exe, '-shots', join(OUT, 'shots.resolved.json'), c.shotsOut, '-novsync', '-logFile', join(c.shotsOut, 'player.log')], { timeoutMs: 1800000 });
      const rep = readJson(join(c.shotsOut, 'shots.json')) as Report | undefined;
      if (!rep) return fail(`the player wrote no shots.json (exit ${r.code}); see ${rel(c.shotsOut)}/player.log`);
      c.facts.shots = rep; c.facts.resolved = resolved;
      return judgeShots(resolved, rep, c.shotsOut, (Date.now() - t0) / 1000);
    },
  },
  {
    id: 'budget', title: 'HUD budget + layout audit', plan: c => [`bun tools/hud-budget/cli.ts ${rel(c.shotsOut)} --skip-unmasked`, 'the layout audit recorded with each HUD shot (HudToolkit.AuditText)'],
    async run(c) {
      const rep = c.facts.shots as Report | undefined;
      if (!rep) return fail('no shots report (the shots step did not run in this batch)');
      const r = await sh('budget', ['bun', 'tools/hud-budget/cli.ts', c.shotsOut, '--skip-unmasked']);
      const lines = r.out.split('\n'), pass = lines.filter(l => l.startsWith('PASS')).length, bad = lines.filter(l => l.startsWith('FAIL'));
      const audited = rep.shots.filter(s => s.audit), dirty = audited.filter(s => !s.audit!.clean);
      const problems = [...bad.map(l => l.slice(0, 160)), ...dirty.flatMap(s => s.audit!.issues.slice(0, 3).map(i => `audit ${s.name}: ${i}`))];
      return problems.length ? fail(`budget ${pass} pass, ${bad.length} over; audit ${audited.length - dirty.length}/${audited.length} clean\n${problems.slice(0, 10).join('\n')}`) : { ok: r.code === 0, detail: `budget ${pass} shots within budget; layout audit ${audited.length}/${audited.length} clean` };
    },
  },
  {
    id: 'late-fight', title: 'late-fight frame times', plan: () => [`resolve tools/game/late.json → out/batch/late.resolved.json (each script's end from the engine)`, `"${join(APP, 'Contents/MacOS/The Suborned')}" -late out/batch/late.resolved.json out/batch/late -novsync -logFile out/batch/late/player.log`, 'fail: a frame > 50 ms tied to a save, order or the end, p99 > 16.7 ms, > 100 ms in the 2 s after the mission ends; each only if it comes back on a second run (brief 045)'],
    async run(c) {
      const exe = join(APP, 'Contents/MacOS/The Suborned');
      if (!existsSync(exe)) return fail(`no built player at ${rel(APP)}; run the game-build step`);
      if (!playerHasLate()) return fail(`the built player has no -late mode yet (its code lacks "${LATE_MARKER}"): Latch adds the hook, contract in briefs/045-late-lag.md ("The player's hook"); --skip late-fight until then`);
      const dir = join(OUT, 'late'); rmSync(dir, { recursive: true, force: true }); mkdirSync(dir, { recursive: true });
      let resolved: LateResolvedFile;
      try { resolved = resolveLate(JSON.parse(readFileSync(LATE_FILE, 'utf8')), dir, engineEnd); } catch (e) { return fail((e as Error).message); }
      writeFileSync(join(OUT, 'late.resolved.json'), JSON.stringify(resolved, null, 1) + '\n');
      // each run is a fresh player over the rows it is given; a row with findings goes once more and fails only if the same kind of finding returns (late.ts: runLate)
      const v = await runLate(resolved, async (rows, n) => {
        const d = n === 1 ? dir : join(OUT, `late-${n}`), rf = join(OUT, n === 1 ? 'late.resolved.json' : `late.resolved-${n}.json`);
        rmSync(d, { recursive: true, force: true }); mkdirSync(d, { recursive: true });
        writeFileSync(rf, JSON.stringify({ ...rows, outDir: d }, null, 1) + '\n');
        if (n > 1) c.say(`late-fight: run ${n - 1} had findings in ${rows.rows.map(r => r.name).join(', ')}; running them again`);
        const watch = watchLoad();
        const r = await sh('late-fight', [exe, '-late', rf, d, '-novsync', '-logFile', join(d, 'player.log')], { timeoutMs: 480000, append: n > 1 });
        const load = await watch.stop();
        const report = readJson(join(d, 'late.json')) as LateReport | undefined;
        if (!report) c.say(`late-fight: the player wrote no late.json (exit ${r.code}); see ${rel(d)}/player.log`);
        return { report, load };
      });
      c.facts.late = v.report; c.facts.lateNotes = v.notes;
      writeFileSync(join(OUT, 'late.verdict.json'), JSON.stringify({ ok: v.ok, problems: v.problems, notes: v.notes, noisy: v.noisy, limits: LIMITS, attempts: v.attempts.map(a => ({ n: a.n, rows: a.rows, load: a.load, findings: a.findings, strays: a.strays, lines: a.lines, report: a.report })) }, null, 1) + '\n');
      const ran = v.attempts.length > 1 ? ` (${v.attempts.length} runs)` : '';
      const tailNotes = [...v.notes.filter(n => !/^run \d/.test(n)), `${v.attempts.map(a => `run ${a.n}: ${loadText(a.load)}`).join(' | ')}`];
      return v.ok ? { ok: true, detail: `${[...v.lines, ...tailNotes].join('\n')}${ran ? '\n' + ran.trim() : ''}` } : fail(`${v.problems.length} problem${v.problems.length === 1 ? '' : 's'}${ran}:\n${[...v.problems, ...v.lines, ...tailNotes].join('\n')}`);
    },
  },
  {
    id: 'tp-long', title: 'draw distance (third person)',
    plan: () => [`resolve tools/game/tplong.json → out/batch/tplong.resolved.json`, `"${join(APP, 'Contents/MacOS/The Suborned')}" -tplong out/batch/tplong.resolved.json out/batch/tplong -novsync -logFile out/batch/tplong/player.log`,
      `fail: third person over ${GATES.third.triangles / 1e6} M triangles, SetPass ${GATES.third.setPass}, shadow casters ${GATES.third.shadowCasters}, renderers drawn ${GATES.third.drawn} (${GATES.third.beyond150} beyond 150 m), mean ${GATES.third.meanMs} ms, p99 ${GATES.third.p99Ms} ms; play view over ${GATES.play.triangles / 1e6} M triangles or ${GATES.play.meanMs} ms (brief 048)`],
    async run(c) {
      const exe = join(APP, 'Contents/MacOS/The Suborned');
      if (!existsSync(exe)) return fail(`no built player at ${rel(APP)}; run the game-build step`);
      if (!playerHasTpLong()) return { ok: true, skipped: true, detail: `the built player has no -tplong mode yet (its code lacks "${TPLONG_MARKER}"): Latch adds the hook with the draw-distance work, contract in briefs/048-draw-distance.md ("The player's hook"); this row runs once it is there` };
      const dir = join(OUT, 'tplong'); rmSync(dir, { recursive: true, force: true }); mkdirSync(dir, { recursive: true });
      let resolved;
      try { resolved = resolveTpLong(JSON.parse(readFileSync(TPLONG_FILE, 'utf8')), dir); } catch (e) { return fail((e as Error).message); }
      const rf = join(OUT, 'tplong.resolved.json'); writeFileSync(rf, JSON.stringify(resolved, null, 1) + '\n');
      const watch = watchLoad();
      const r = await sh('tp-long', [exe, '-tplong', rf, dir, '-novsync', '-logFile', join(dir, 'player.log')], { timeoutMs: 300000 });
      const load = await watch.stop();
      const report = readJson(join(dir, 'tplong.json')) as TpLongReport | undefined;
      if (!report) c.say(`tp-long: the player wrote no tplong.json (exit ${r.code}); see ${rel(dir)}/player.log`);
      const v = judgeTpLong(resolved, report, load);
      c.facts.tplong = v.lines;
      writeFileSync(join(OUT, 'tplong.verdict.json'), JSON.stringify({ ok: v.ok, problems: v.problems, notes: v.notes, gates: GATES, load, lines: v.lines, report }, null, 1) + '\n');
      const tail = [...v.notes, loadText(load)];
      return v.ok ? { ok: true, detail: `${[...v.lines, ...tail].join('\n')}` } : fail(`${v.problems.length} problem${v.problems.length === 1 ? '' : 's'}:\n${[...v.problems, ...v.lines, ...tail].join('\n')}`);
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

const LATE_FILE = join(ROOT, 'tools/game/late.json'), LATE_MARKER = 'suborned.late.report/1';
/** Does the built player carry the -late mode? Its report's schema literal is in its code only when Latch's hook is. (Without the hook the player would just open the game and sit; this stops that after a second.) */
export function playerHasLate(managed = join(APP, 'Contents/Resources/Data/Managed/Suborned.Runtime.dll')): boolean {
  if (!existsSync(managed)) return false;
  return readFileSync(managed).includes(Buffer.from(LATE_MARKER, 'utf16le'));
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
    try { r = c.dry ? { ok: true, detail: s.plan(c).join('\n') } : await s.run(c); } catch (e) { r = { ok: false, detail: `threw: ${(e as Error).message}` }; }
    rows.push({ id: s.id, title: s.title, status: r.ok ? (r.skipped ? 'skipped' : 'ok') : 'FAILED', seconds: (performance.now() - t0) / 1000, detail: r.skipped ? `skipped: ${r.detail}` : r.detail });
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

export function screen(rows: Row[], o: { when: string; mode: string; build?: any; proof?: any; shots?: Report; shotsOut: string; skipped?: string; late?: LateReport; lateNotes?: string[]; tplong?: string[] }): string {
  const w = Math.max(...rows.map(r => r.id.length));
  const out: string[] = [`bun run batch · ${o.when} · Unity ${o.mode === 'editor' ? 'Editor open (menus through the Unity CLI)' : 'batchmode'}`, ''];
  for (const r of rows) {
    const first = r.detail.split('\n')[0] ?? '';
    out.push(`  ${r.id.padEnd(w)}  ${r.status.padEnd(8)} ${(r.status === 'ok' || r.status === 'FAILED' ? `${r.seconds.toFixed(0)} s` : '').padStart(5)}  ${first}`);
    if (r.status === 'FAILED') for (const l of r.detail.split('\n').slice(1)) out.push(`  ${' '.repeat(w)}  ${' '.repeat(8)} ${' '.repeat(5)}  ${l}`);
  }
  out.push('');
  const bad = rows.find(r => r.status === 'FAILED');
  if (o.skipped) out.push(`build   NOT REBUILT: ${o.skipped}`);
  if (o.build) out.push(`build   ${rel(APP)}  ${o.build.version} (${o.build.recording}, ${o.build.sha}${o.build.dirty ? '+' : ''})`);
  for (const l of frameLines(o.proof, o.shots)) out.push(`frames  ${l}`);
  for (const r of o.late?.rows ?? []) if (r.status === 'ok') out.push(`late    ${r.name}: worst ${f1(Math.max(r.window.maxMs, r.tail.maxMs))} ms, p99 ${f1(r.window.p99Ms)} ms, end ${f1(r.tail.maxMs)} ms (last 60 s, ${o.late!.screen})`);
  for (const n of o.lateNotes ?? []) if (/NOISY|not counted|stray/.test(n)) out.push(`late    ${n}`);
  for (const l of o.tplong ?? []) out.push(`tp-long  ${l}`);
  if (o.shots) out.push(`shots   ${rel(o.shotsOut)}/  (${o.shots.shots.length}; shots.json is the report)`);
  out.push(bad ? `STOPPED at ${bad.id}: ${bad.title}. Log: out/batch/logs/${bad.id}.log` : `BATCH OK: ${rows.filter(r => r.status === 'ok').length} steps in ${rows.reduce((a, r) => a + r.seconds, 0).toFixed(0)} s`);
  return out.join('\n');
}

if (import.meta.main) {
  const args = process.argv.slice(2), flag = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args.splice(i, 2)[1] : undefined; }, bool = (k: string) => { const i = args.indexOf(k); if (i >= 0) args.splice(i, 1); return i >= 0; };
  try { takeGuardFlags(args); } catch (e) { console.error((e as Error).message); process.exit(2); }
  const from = flag('--from'), only = flag('--only')?.split(','), skipArg = flag('--skip')?.split(','), shotsFile = resolve(flag('--shots') ?? DEFAULT_FILE), shotsOut = resolve(flag('--shots-out') ?? join(OUT, 'shots'));
  const dry = bool('--dry-run'), noShots = bool('--no-shots'), forceEditor = bool('--editor'), forceBatch = bool('--batchmode'), rebuild = bool('--rebuild');
  if (args.length || (forceEditor && forceBatch)) { console.error(`unknown or conflicting argument ${args.join(' ') || '--editor --batchmode'}; see the usage line at the top of tools/game/batch.ts`); process.exit(2); }
  const skip = [...(skipArg ?? []), ...(noShots ? ['shots', 'budget'] : [])];
  const { run, unknown } = pick(STEPS.map(s => s.id), { from, only, skip });
  if (unknown.length) { console.error(`unknown step ${unknown.join(', ')}; steps: ${STEPS.map(s => s.id).join(' ')}`); process.exit(2); }
  const mode: Ctx['mode'] = forceEditor ? 'editor' : forceBatch ? 'batchmode' : editorOpen() ? 'editor' : 'batchmode';
  const c: Ctx = {
    dry, mode, rebuild, shotsFile, shotsOut, facts: {}, say: m => console.error(m),
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
  if (dry) { console.log(`bun run batch --dry-run · Unity ${mode === 'editor' ? 'Editor open' : 'batchmode'}\n`); for (const r of rows) console.log(r.status === 'ok' ? `${r.id}  ${r.title}\n${r.detail.split('\n').map(l => '    ' + l).join('\n')}` : `${r.id}  (${r.status})`); process.exit(rows.some(r => r.status === 'FAILED') ? 1 : 0); }
  console.log(screen(rows, { when: new Date().toISOString().slice(0, 16).replace('T', ' '), mode, build: c.facts.build ?? readJson(join(ROOT, 'out/build/play/build.json')), proof: c.facts.proof, shots: c.facts.shots as Report | undefined, shotsOut, skipped: c.facts.rebuildSkipped as string | undefined, late: c.facts.late as LateReport | undefined, lateNotes: c.facts.lateNotes as string[] | undefined, tplong: c.facts.tplong as string[] | undefined }));
  process.exit(rows.some(r => r.status === 'FAILED') ? 1 : 0);
}
