// bun run batch --sky [--then-full] (Splice, 2026-11-11; Sky: "this took forever :( … Don't understand why it cant use maths to determine game state", then "yes" to
// Aster's plan; CLAUDE.md "Delta only"): the fast path to a build for Sky, about 10 minutes. Rules and game state are judged by maths (bun run sim:test, the seed
// sweep), so a build for Sky gets a short smoke, not the proof:
//   steps    sim (only when the engine or its inputs changed: its gate), words, compile, hostlogic, kit and world (only when the kit or world changed), unity-world
//            (only when what it reads changed: a candidate added to the kit is not that, proofmap.json kit.json#rest), then game-build with the smoke. Never unity-match,
//            game-check, shots, budget, late-fight, tp-long or perf064: the full batch has them.
//   smoke    the proof's own base section (-gameProof -proofOnly base -fastExit, GameBoot.GameProof; nothing new in the player): the player started behind (open -g),
//            OldTown with the Handover host, the briefing up, Begin, about 4 s of play to a tick past 0, the play shot with its HUD layer and the pause menu, then
//            quit. It fails on a blank frame, play that did not start, a HUD with no element, any Exception in the player's log, or a build that does not carry
//            OldTown.unity. With a rebuild the build takes it (cli.ts --build with a base-only plan, its blank-frame rebuild included); without one it is
//            launched on the app as it stands, into out/build/play/smoke.
//   records  the build in tools/game/version.json (the build does) and runs/builds/builds.json (an entry with sent [] for Latch's or Aster's send), prints the app.
//   behind   --then-full starts `bun run batch --full` behind, detached: its own player folder (out/build/full, a scratch build: no build number, never Sky's
//            app), its own batch folder (out/batch/full-behind: logs, batch.log, last.json), --force past the Sky-is-playing wait (the rule is that it runs while
//            Sky plays), --behind players, and --verdict out/batch/full-behind/verdict.json: a frame-time step that fails on a NOISY machine is said and does
//            not stop it; Aster reads the verdict's findings. Unity has one project lock, so any later batch on Sky's player first asks a running behind batch to
//            stop (out/batch/full-behind/stop, read before each of its steps) and waits for it to exit.
import { spawn } from 'node:child_process';
import { existsSync, mkdirSync, openSync, readFileSync, renameSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { sectionIds, type Plan, type ProofMap } from './proofdelta';
import { spanNoisy, spanText, type LoadSpan } from './perfrow';
import { ROOT } from './shots';
import { OUT, readJson, rel } from './batch-paths';

/** The steps a --sky batch may run, in the batch's order; each still runs only when its gate says (sim, kit, world, unity-world). */
export const SKY_STEPS = ['sim', 'words', 'compile', 'hostlogic', 'kit', 'world', 'unity-world', 'game-build'];
/** A --sky over this warns (Sky: "a change reaches Sky in about 10 minutes"). */
export const SKY_LIMIT_S = 600;
/** The steps a --sky leaves out: what pick() skips. */
export const skySkips = (ids: string[]) => ids.filter(i => !SKY_STEPS.includes(i));

/** The game build's proof plan for --sky: the proof's base alone (`-proofOnly base`), every section said as left to the full batch. */
export function smokePlan(m: ProofMap, snapshot: string): Plan {
  const why = 'not run: a --sky build takes the smoke (the base); the full batch proves it';
  return { schema: 'suborned.proofplan/1', mode: 'delta', why: '--sky: the smoke, the proof\'s base alone', only: [], skips: Object.fromEntries(sectionIds(m).map(s => [s, why])), reasons: {}, seconds: m.baseSeconds, snapshot } as Plan;
}

/** What the smoke left: proof.json, the player's log, the build's contents, and the play shot's files with their times. */
export type SmokeInput = { proof: any; log: string; contents: any; since: number; files: Record<string, number | undefined>; manifest: any; seconds: number };
export const SMOKE_FILES = ['play.png', 'play.hud.png', 'play.manifest.json'];
/** The smoke judged: the frame drew, play started, the HUD drew, no Exception in the log, the build carries the played scene. */
export function judgeSmoke(s: SmokeInput): { ok: boolean; detail: string; problems: string[] } {
  const problems: string[] = [], lines: string[] = Array.isArray(s.proof?.log) ? s.proof.log : [];
  if (!s.proof) problems.push('the player wrote no proof.json (it did not start, or found no Handover host in 60 s)');
  else if (s.proof.status !== 'ok') problems.push(`the frame is ${s.proof.status}${s.proof.status === 'blank' ? ' (one flat colour: the pipeline did not start)' : ''}`);
  if (s.proof && !lines.some(l => /^briefing up: True/.test(l))) problems.push('the briefing did not come up');
  const play = lines.map(l => l.match(/^play: modal (\S+), paused (\w+), tick (\d+)/)).find(Boolean), tick = play ? Number(play[3]) : 0;
  if (s.proof && (!play || play[2] !== 'False' || tick <= 0)) problems.push(`play did not start (${play ? `paused ${play[2]}, tick ${tick}` : 'no play line'})`);
  for (const f of SMOKE_FILES) if (!(s.files[f]! >= s.since)) problems.push(`${f} was not written`);
  const hud = (s.manifest?.elements ?? []).length; // every element of a shot's manifest is the HUD's (play: image, bar, next, marker; the briefing: paper, hud)
  if (s.files['play.manifest.json']! >= s.since && !hud) problems.push('the HUD drew no element in the play shot');
  const errors = s.log.split('\n').filter(l => /Exception/.test(l)).map(l => l.trim().slice(0, 160));
  if (errors.length) problems.push(`${errors.length} Exception line${errors.length === 1 ? '' : 's'} in the player's log: ${[...new Set(errors)].slice(0, 3).join(' | ')}`);
  if (!s.contents || !('Assets/Scenes/OldTown.unity' in s.contents)) problems.push('the build does not carry Assets/Scenes/OldTown.unity (build-contents.json)');
  return problems.length ? { ok: false, detail: `smoke FAILED: ${problems.join('; ')}`, problems }
    : { ok: true, problems, detail: `smoke ok in ${s.seconds.toFixed(0)} s: OldTown with the Handover host, the briefing up, played to tick ${tick}, the HUD drew ${hud} elements, the pause menu, no Exception in the log` };
}
/** The smoke's input from a proof folder written after `since`. */
export function readSmoke(dir: string, buildDir: string, since: number, seconds: number): SmokeInput {
  const at = (f: string) => (existsSync(join(dir, f)) ? statSync(join(dir, f)).mtimeMs : undefined);
  return { proof: readJson(join(dir, 'proof.json')), log: existsSync(join(dir, 'player.log')) ? readFileSync(join(dir, 'player.log'), 'utf8') : '', contents: readJson(join(buildDir, 'build-contents.json')),
    since, files: Object.fromEntries(SMOKE_FILES.map(f => [f, at(f)])), manifest: readJson(join(dir, 'play.manifest.json')), seconds };
}
/** The smoke's own launch, on an app not rebuilt: the base proof behind, into `dir`. */
export const smokeArgs = (dir: string) => ['-gameProof', dir, '-proofOnly', 'base', '-fastExit', '-novsync', '-logFile', join(dir, 'player.log')];

/** runs/builds/builds.json: an entry for a build made for Sky (sent [] until it is sent; whatsNew for Aster), once per build number. Returns what it did. */
export function logBuilt(stamp: any, file = join(ROOT, 'runs/builds/builds.json')): string {
  if (stamp?.result !== 'Succeeded' || typeof stamp.build !== 'number' || /-scratch/.test(stamp.version ?? '')) return 'not logged: the stamp has no build number (a scratch build)';
  const log = existsSync(file) ? JSON.parse(readFileSync(file, 'utf8')) : { schema: 'suborned.builds/1', note: 'One entry per game build sent (tools/game/cli.ts --send). whatsNew is one line, filled by Aster.', builds: [] };
  if (log.builds.some((b: any) => b.build === stamp.build)) return `${stamp.version} already in ${rel(file)}`;
  log.builds.push({ build: stamp.build, version: stamp.version ?? null, date: new Date().toISOString(), builtAt: stamp.at ?? null, sha: stamp.sha ?? null, dirty: stamp.dirty ?? null, engine: stamp.sim ?? null,
    engineSource: stamp.engine ?? null, dllSha256: stamp.dllSha256 ?? null, recording: stamp.recording ?? null, zip: null, sent: [], whatsNew: '' });
  mkdirSync(join(file, '..'), { recursive: true }); writeFileSync(file, JSON.stringify(log, null, 1) + '\n');
  return `${stamp.version} logged in ${rel(file)}`;
}

/** A --sky's time against its limit, with the slowest step. */
export function skyLines(rows: { id: string; status: string; seconds: number }[]): string[] {
  const ran = rows.filter(r => r.status === 'ok' || r.status === 'FAILED'), total = ran.reduce((a, r) => a + r.seconds, 0);
  if (total <= SKY_LIMIT_S || !ran.length) return [];
  const w = ran.reduce((a, b) => (b.seconds > a.seconds ? b : a));
  return [`WARNING this --sky took ${total.toFixed(0)} s, over the ${SKY_LIMIT_S} s target; slowest: ${w.id} ${w.seconds.toFixed(0)} s`];
}

// ---------------------------------------------------------------- the full batch behind (--then-full)

export const BEHIND_OUT = join(ROOT, 'out/batch/full-behind'), BEHIND_PLAYER = 'out/build/full';
const RUNNING = join(BEHIND_OUT, 'running.json'), STOP = join(BEHIND_OUT, 'stop');
export const behindCommand = (verdict = join(BEHIND_OUT, 'verdict.json')) => ['bun', 'tools/game/batch.ts', '--full', '--force', '--behind', '--batchmode', '--verdict', verdict];
export const behindEnv = () => ({ ...process.env, SUBORNED_PLAYER_DIR: BEHIND_PLAYER, SUBORNED_BATCH_OUT: BEHIND_OUT });
const alive = (pid: number) => { try { process.kill(pid, 0); return true; } catch { return false; } };
/** The behind batch, when one is running (its pid alive). */
export function behindRunning(file = RUNNING): { pid: number; at: string; for?: string } | undefined { const r = readJson(file); return r && typeof r.pid === 'number' && alive(r.pid) ? r : undefined; }
/** Start the full batch behind, detached (it outlives this one), its output in out/batch/full-behind/batch.log. */
export function startBehind(forBuild: string | undefined): { pid: number; log: string; verdict: string } {
  mkdirSync(BEHIND_OUT, { recursive: true }); rmSync(STOP, { force: true });
  const verdict = join(BEHIND_OUT, 'verdict.json'), log = join(BEHIND_OUT, 'batch.log');
  if (existsSync(verdict)) renameSync(verdict, join(BEHIND_OUT, 'verdict.prev.json'));
  const fd = openSync(log, 'w'), cmd = behindCommand(verdict);
  const p = spawn(cmd[0]!, cmd.slice(1), { cwd: ROOT, env: behindEnv(), detached: true, stdio: ['ignore', fd, fd] }); p.unref();
  writeFileSync(RUNNING, JSON.stringify({ pid: p.pid, at: new Date().toISOString(), for: forBuild, command: cmd.join(' '), player: BEHIND_PLAYER }, null, 1) + '\n');
  return { pid: p.pid!, log, verdict };
}
/** Before a batch on Sky's player: a running behind batch holds Unity's project lock, so it is asked to stop at its next step and waited for. Seconds waited. */
export async function stopBehind(say: (m: string) => void, o: { running?: typeof behindRunning; sleep?: (ms: number) => Promise<void>; timeoutS?: number; stop?: string } = {}): Promise<number> {
  const running = o.running ?? behindRunning, sleep = o.sleep ?? ((ms: number) => new Promise(r => setTimeout(r, ms))), t0 = Date.now(), r = running();
  if (!r) return 0;
  const stop = o.stop ?? STOP; mkdirSync(join(stop, '..'), { recursive: true }); writeFileSync(stop, `${new Date().toISOString()} the next build for Sky\n`);
  say(`a full batch is running behind (pid ${r.pid}, since ${r.at.slice(11, 16)}): asked to stop at its next step (it holds Unity's project lock); waiting`);
  while (running() && (Date.now() - t0) / 1000 < (o.timeoutS ?? 3600)) await sleep(5000);
  if (running()) throw new Error(`the full batch behind (pid ${r.pid}) did not stop within ${o.timeoutS ?? 3600} s; kill ${r.pid} (by PID) and run again`);
  return (Date.now() - t0) / 1000;
}
/** Read by the behind batch before each step: asked to stop for the next build. */
export const stopAsked = (out = OUT): string | undefined => (out === BEHIND_OUT && existsSync(STOP) ? `stopped for the next build for Sky (${readFileSync(STOP, 'utf8').trim()}); the next --then-full starts it again` : undefined);

/** runSteps' rule with --verdict: a failed frame-time step whose machine was NOISY around it goes on to the next step (the verdict says it); anything else stops. */
export const goesOn = (goOnNoisy: boolean | undefined, perf: boolean | undefined, load: LoadSpan | undefined): boolean => !!goOnNoisy && !!perf && spanNoisy(load);
/** The behind batch's verdict (--verdict): a failed frame-time step on a NOISY machine is noisy, not a finding; anything else that failed is a finding. */
export type VerdictRow = { id: string; status: string; seconds: number; detail: string; perf?: boolean; load?: LoadSpan; lateNoisy?: boolean };
export function verdict(rows: VerdictRow[], o: { build?: any; at?: string; stopped?: string } = {}) {
  const failed = rows.filter(r => r.status === 'FAILED'), noisyRow = (r: VerdictRow) => !!r.perf && (spanNoisy(r.load) || !!r.lateNoisy);
  const findings = failed.filter(r => !noisyRow(r) && !(o.stopped && r.detail.startsWith('stopped for the next build'))).map(r => ({ step: r.id, detail: r.detail.split('\n').slice(0, 6).join('\n') }));
  return { schema: 'suborned.batch-verdict/1', at: o.at ?? new Date().toISOString(), build: o.build ? `${o.build.version} (${o.build.sha}${o.build.dirty ? '+' : ''})` : undefined, ok: !findings.length && !o.stopped,
    stopped: o.stopped, findings, noisy: failed.filter(noisyRow).map(r => ({ step: r.id, load: spanText(r.load), detail: r.detail.split('\n')[0] })),
    rows: rows.map(r => ({ id: r.id, status: r.status, seconds: Math.round(r.seconds), detail: r.detail.split('\n')[0] })) };
}
