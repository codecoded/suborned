// Brief 060 tier 2 (Latch; Sky approved 2026-10-24): the `bot` lane of a QA pass. The built player plays itself through real input (`-bot`,
// unity/Assets/Suborned/Runtime/Play/BotRun.cs): each row is one whole Handover at real time (~10 minutes with boot), chosen by a seeded policy
// (random, greedy, cautious) from the game's own ring offers, or played toward a recorded winning route with a person's indecision on top
// (human: tools/qa/human.ts draws the route and every noise from the row's seed; the row's tape rides in the plan file). The player checks every frame (Q-GIVER, Q-OFFER, Q-WARN-HUD, Q-CARD, Q-FEET,
// Q-SOLID, Q-ERR, Q-FRAME, Q-MEM, and Q-STUCK when a row never ends) and keeps a JPEG shot, its HUD manifest and the replay at each anomaly.
// This file plans the rows (tools/qa/bot.json; the seeds move with the night, so each night plays new games and every row says its seed),
// runs one player per row through the guard (it rechecks before each row: a game Sky opens stops the next row from starting), and turns the
// player's report (suborned.bot.report/1) into QA findings. Thresholds are Suture's (brief 060 §2, [S]); the player reports the ones it used.
//
// Storage: everything goes under out/qa/<run>/bot/<row>/ and rotates with the pass (tools/qa/rotate.ts). A row keeps at most 60 shots as
// JPEG (~150-300 KB each) with their manifests, frames.csv, feet.csv (~5 MB), mem.csv, hud.jsonl, card.json and the replay scripts.
//
// usage (normally through `bun run qa --nightly` or `bun run qa --lanes bot`):
//   bun tools/qa/bot.ts --plan [--night YYYYMMDD]       print the rows a night would play
//   bun tools/qa/bot.ts --run <outDir> [--rows n] [--minutes m] [--force]   play them now (waits while Sky plays)
import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { z } from 'zod';
import { ROOT } from '../missions/check/engine';
import { whoIsPlaying } from '../game/playing';
import { BUILD_APP, lane, playStream } from './sim';
import { feetShare, gradeMem, gradeSolid, isRace, loadChairs, loadWalk, offerOf, plainRefusal, readFeet, readMem, SEATS_FILES, GRADE, type Grade } from './grade';
import type { Finding, Play, Severity } from './types';
import { humanTape, loadProfile, winningRoutes, type HumanProfile, type HumanTape, type ProfileSource, type RouteScript } from './human';

export const POLICIES = ['random', 'greedy', 'cautious', 'human'] as const;
export const BotRow = z.object({
  name: z.string().regex(/^[a-z0-9][a-z0-9-]*$/), policy: z.enum(POLICIES), seed: z.number().int().min(0).optional(),
  minutes: z.number().min(1).max(30).optional(), decideS: z.number().min(0.5).max(30).optional(), note: z.string().optional(),
  /** human only: pin the row's route to one winning script id (else the seed picks it) */ route: z.string().optional(),
}).strict();
export const BotFile = z.object({ schema: z.literal('suborned.bot/1'), note: z.string().optional(), rows: z.array(BotRow).min(1) }).strict();
export type BotPlanRow = { name: string; policy: string; seed: number; seconds: number; decideS: number; w: number; h: number; /** the human policy's plan and noise (tools/qa/human.ts), drawn from the row's seed */ human?: HumanTape };
export type BotPlan = { schema: 'suborned.bot.plan/1'; outDir: string; rows: BotPlanRow[] };

/** A night's seed for a row: the row's own seed (or its index) mixed with the date, so each night plays new games, reproducibly. */
export function nightSeed(night: string, base: number): number {
  let h = 2166136261 >>> 0; for (const ch of `${night}:${base}`) { h ^= ch.charCodeAt(0); h = Math.imul(h, 16777619) >>> 0; }
  return h % 1_000_000;
}

/** The rows of a night: tools/qa/bot.json, the seeds moved by the date unless `fixed`; `rows` cuts the list, `minutes` overrides each row's cap. */
export function planRows(raw: unknown, night: string, o: { rows?: number; minutes?: number; fixed?: boolean; routes?: RouteScript[]; profile?: { profile: HumanProfile; source: ProfileSource } } = {}): BotPlanRow[] {
  const f = BotFile.parse(raw);
  const rows = f.rows.slice(0, o.rows ?? f.rows.length);
  let routes = o.routes, profile = o.profile; // read once, only when a human row plays
  return rows.map((r, i) => {
    const seed = o.fixed ? r.seed ?? i + 1 : nightSeed(night, r.seed ?? i + 1);
    const row: BotPlanRow = { name: r.name, policy: r.policy, seed, seconds: Math.round((o.minutes ?? r.minutes ?? 11) * 60), decideS: r.decideS ?? 0, w: 1920, h: 1080 };
    if (r.policy === 'human') { routes ??= winningRoutes(); profile ??= loadProfile(); row.human = humanTape(seed, routes, profile, r.route); }
    else if (r.route) throw new Error(`bot row ${r.name}: route is the human policy's (policy ${r.policy})`);
    return row;
  });
}

// ---------------------------------------------------------------- the player's report

export type BotAnomaly = { check: string; key: string; title: string; expected: string; observed: string; tick: number; clock: string; at: [number, number]; shot: string; script: string; /** Suture's offline regrade (grade.ts): its cause, severity and plain words win over the player's */ grade?: Grade;
  /** human rows: the indecision just before it (the latest cancel, abandon, misclick, re-order, wrong wedge ... within HUMAN_NOISE_S), e.g. "misclick 2.1 s before: step 7 scout listen waitress" */ noise?: string };
/** One indecision the human row acted out (BotRun.Human), in the row's report: when, which step of which route, what. */
export type HumanEvent = { t: number; tick: number; clock: string; kind: string; route: string; step: number; op: string; act: string; target: string; detail: string;
  /** kind "given" (Suture's Q-INTENT): what the game gave for the try whose step is op/act/target (the bot's intent); null when nothing came of it.
   *  `to` is a move's point "x,y" (engine cm) or its named place */ gave?: { op: string; act: string; target: string; to: string; ok: boolean; code: string } | null };
export type HumanReport = { route: string; group: string; /** the row uses third person now and then (the profile's row model) */ thirdPerson?: boolean; routes: string[]; profile: string; steps: number; done: number; skipped: number; counts: Record<string, number>; events: HumanEvent[];
  /** each step as it ended: d done (given), s skipped (refused or blocked too long), - never reached; with its tries */ trace: string };
export type BotReportRow = {
  name: string; policy: string; seed: number; status: string; error?: string; preset: string; seconds: number; endTick: number; outcome: string; failCode: string; cardHead: string;
  input: { decisions: number; clicks: number; rings: number; ringMisses: number; taken: number; moves: number; runs: number; pans: number; turns: number };
  orders: { given: number; refused: number; codes: Record<string, number> };
  frames: { count: number; meanMs: number; p99Ms: number; maxMs: number; over50: number };
  memory: { startMB: number; endMB: number; slopeMBPerMin: number };
  consoleErrors: number; feetSamples: number; figures: number; shots: number; counts: Record<string, number>; anomalies: BotAnomaly[];
  /** the human policy's plan, progress and indecision (absent on the other policies) */ human?: HumanReport;
};
export type BotReport = { schema: 'suborned.bot.report/1'; status: string; screen: string; build: string; thresholds: Record<string, number>; rows: BotReportRow[] };

/** Severity and owner (§14) by check: the host's checks are the player's, the look and copy go to their owners through Aster. */
export const CHECK_META: Record<string, { severity: Severity; owner: string }> = {
  'Q-GIVER': { severity: 'S2', owner: 'Latch' }, 'Q-OFFER': { severity: 'S2', owner: 'Latch (host offer) / Keel (engine refusal)' },
  'Q-WARN-HUD': { severity: 'S2', owner: 'Latch / Glyph' }, 'Q-CARD': { severity: 'S1', owner: 'Latch / Fable' },
  'Q-FEET': { severity: 'S3', owner: 'Latch / Splice (walk file)' }, 'Q-SOLID': { severity: 'S3', owner: 'Latch / Pulse (paths)' },
  'Q-ERR': { severity: 'S1', owner: 'Latch' }, 'Q-FRAME': { severity: 'S4', owner: 'Redline' }, 'Q-MEM': { severity: 'S4', owner: 'Latch (Redline profiles)' },
  'Q-STUCK': { severity: 'S1', owner: 'Latch / Keel' }, 'Q-BOT': { severity: 'S1', owner: 'Latch' },
  'Q-TP-KEEP': { severity: 'S2', owner: 'Latch' }, // human rows: T stopped an act the bot never touched (Sky 2026-10-22)
};

/** The findings of one row: one per anomaly the player kept (its replay and shot ride along), and a row that broke or never ran. */
export function findingsOf(row: BotReportRow | undefined, name: string, base: string): (Finding & { shot?: string; script?: string })[] {
  if (!row) return [{ check: 'Q-BOT', severity: 'S1', owner: 'Latch', key: 'Q-BOT:not-run', title: 'A bot row did not run', expected: 'every row plays to its end', observed: `${name}: the player wrote no row (it crashed, was stopped or Sky opened the game)` }];
  const out: (Finding & { shot?: string; script?: string })[] = [];
  if (row.status === 'failed') out.push({ check: 'Q-BOT', severity: 'S1', owner: 'Latch', key: 'Q-BOT:failed', title: 'A bot row broke', expected: 'the row plays to its end', observed: `${name}: ${row.error ?? 'failed'}` });
  for (const a of row.anomalies) {
    const m = CHECK_META[a.check] ?? { severity: 'S3' as Severity, owner: 'Suture' }, g = a.grade;
    out.push({
      check: a.check, severity: g?.severity ?? m.severity, owner: g?.owner ?? m.owner, key: g ? `${a.check}:${g.cause}` : `${a.check}:${a.key.replace(/[0-9]+/g, '#').slice(0, 80)}`, title: g?.title ?? a.title,
      expected: g?.expected ?? a.expected, observed: `${a.observed}${g?.note ? `; ${g.note}` : ''} (${row.policy} seed ${row.seed}${row.human ? ` on ${row.human.routes.join(' > ')}` : ''}, ${a.clock}${a.noise ? `; human: ${a.noise}` : ''})`, tick: a.tick,
      shot: a.shot ? join(base, a.shot) : undefined, script: a.script ? join(base, a.script) : undefined,
    });
  }
  return out;
}

// ---------------------------------------------------------------- the offline regrade (Suture, grade.ts)

export type GradeOpts = { walkFile?: string; seatsFile?: string; app?: string; say?: (m: string) => void;
  /** replays anomaly scripts on the built player's engine and gives each play's log (tests stand in for the engine) */ replay?: (scripts: string[]) => Promise<{ tick: number; code: string }[][]> };

/** Replays scripts on the build lane (the built player's own engine and inputs), in order. */
async function replayOnBuild(scripts: string[], dir: string): Promise<{ tick: number; code: string }[][]> {
  const l = lane('build'), list = join(dir, 'replay-list.txt'), logs: { tick: number; code: string }[][] = [];
  writeFileSync(list, scripts.join('\n') + '\n');
  await playStream(l, list, (p: Play, i: number) => { logs[i] = p.log ?? []; }, { replay: false });
  rmSync(list, { force: true });
  return logs;
}

/** Regrades each row's anomalies in place from the files the row wrote (grade.ts): Q-SOLID by cause against the drawn ground, Q-OFFER refusals for
 *  a doer who went down in the order's own tick as races, Q-MEM by held growth instead of one slope, and the Q-FEET share. A row without its files keeps
 *  the player's word. */
export async function gradeRows(rows: BotReportRow[], outDir: string, o: GradeOpts = {}): Promise<void> {
  const say = o.say ?? (() => {}), app = o.app ?? BUILD_APP;
  const walk = loadWalk(o.walkFile ?? join(ROOT, 'out/world/harbour-district.walk.json'));
  const chairs = loadChairs(o.seatsFile ?? SEATS_FILES(app, ROOT).find(f => existsSync(f)) ?? '');
  if (!walk) say('qa bot: no walk file (bun run geometry writes out/world/harbour-district.walk.json): Q-SOLID keeps the player\'s grid word');
  const races: { a: BotAnomaly; op: string; script: string }[] = [];
  for (const row of rows) {
    const dir = join(outDir, row.name), feet = readFeet(join(dir, 'feet.csv')), mem = readMem(join(dir, 'mem.csv'));
    for (const a of row.anomalies) {
      if (a.check === 'Q-SOLID' && feet.length) a.grade = gradeSolid(a, feet, walk, chairs);
      if (a.check === 'Q-OFFER') {
        const of = offerOf(a.observed); if (!of) continue;
        a.grade = { cause: `${of.verb}:${of.code}`, severity: 'S2', title: `The ring offers ${of.verb[0]!.toUpperCase() + of.verb.slice(1)}, then the game refuses it: ${plainRefusal(of.code)}`, owner: CHECK_META['Q-OFFER']!.owner, note: '' };
        if ((GRADE.offer.doerCodes as readonly string[]).includes(of.code) && a.script) races.push({ a, op: of.op, script: join(outDir, a.script) });
      }
    }
    if (mem.length) {
      const v = gradeMem(mem), had = row.anomalies.find(a => a.check === 'Q-MEM');
      row.anomalies = row.anomalies.filter(a => a.check !== 'Q-MEM');
      const base: BotAnomaly = { check: 'Q-MEM', key: 'held', title: 'Memory keeps growing through the run', expected: `no held growth over ${GRADE.mem.totalMBMin} MB a minute (total) or ${GRADE.mem.managedMBMin} MB a minute (managed) after a ${GRADE.mem.warmS} s warm-up`, observed: v.note, tick: had?.tick ?? row.endTick, clock: had?.clock ?? '', at: [0, 0], shot: '', script: had?.script ?? '' };
      if (v.judged && v.leak) row.anomalies.push({ ...base, grade: { cause: 'held-growth', severity: 'S4', title: base.title, owner: CHECK_META['Q-MEM']!.owner, note: '' } });
      else if (had) row.anomalies.push({ ...base, observed: `${had.observed}; regraded: ${v.note}`, grade: { cause: v.judged ? 'not-a-leak' : 'not-judged', severity: 'info', title: v.judged ? 'Memory rose, then held: not a leak' : 'Memory: the row was too short to judge', owner: CHECK_META['Q-MEM']!.owner, note: '' } });
    }
    if (feet.length) {
      const f = feetShare(feet);
      if (!f.ok) row.anomalies.push({ check: 'Q-FEET', key: 'share', title: 'Feet off the drawn floor too often', expected: `feet within ${GRADE.feet.tolM * 100} cm of the drawn floor on ${GRADE.feet.share * 100}% of samples`, observed: `${f.within} of ${f.n} samples (${(f.share * 100).toFixed(2)}%)`, tick: row.endTick, clock: '', at: [0, 0], shot: '', script: '', grade: { cause: 'share', severity: 'S3', title: 'Feet off the drawn floor too often', owner: CHECK_META['Q-FEET']!.owner, note: '' } });
    }
  }
  if (!races.length) return;
  let logs: { tick: number; code: string }[][];
  try { logs = await (o.replay ?? (s => replayOnBuild(s, outDir)))(races.map(r => r.script)); } catch (e) { say(`qa bot: the Q-OFFER replays did not run (${(e as Error).message.split('\n')[0]}): those refusals stay findings`); return; }
  races.forEach((r, i) => {
    const log = logs[i]; if (!log) return;
    if (isRace(log, r.op, r.a.tick)) r.a.grade = { cause: 'race', severity: 'info', title: 'An agent went down in the same tick the order landed (a race, not a lie)', owner: CHECK_META['Q-OFFER']!.owner, note: `the ${r.op} went down within ${GRADE.offer.raceTicks} tick of the order: nothing on screen could have shown it` };
    else r.a.grade = { cause: 'stale-ring', severity: 'S2', title: 'The ring kept offering an agent who was already down', owner: 'Latch (the open ring must re-check its givers)', note: `the ${r.op} was down before the order landed` };
  });
}

// ---------------------------------------------------------------- running the player

export type RunOpts = { rows?: number; minutes?: number; night?: string; fixed?: boolean; say?: (m: string) => void; playing?: () => unknown[]; app?: string; /** for tests: whether the player knows -bot */ hasBot?: (app: string) => boolean };
export type BotLaneResult = { report?: BotReport; rows: BotPlanRow[]; ran: string[]; stoppedForSky: boolean; seconds: number; build: string; /** the lane did not run, and why (no player, or a player built before -bot): a note, not a finding */ skipped?: string };

const exe = (app: string) => join(app, 'Contents/MacOS/The Suborned');
/** The built player knows `-bot`: its runtime assembly carries BotRun's report schema (a string literal, UTF-16 in the dll's user strings).
 *  A player built before tier 2 (0.2.112 and earlier) does not, and would ignore the flag and open the game for nobody. */
export function playerHasBot(app: string): boolean {
  const dll = join(app, 'Contents/Resources/Data/Managed/Suborned.Runtime.dll');
  if (!existsSync(dll)) return false;
  return readFileSync(dll).includes(Buffer.from('suborned.bot.report/1', 'utf16le'));
}

const today = () => new Date().toISOString().slice(0, 10).replace(/-/g, '');

/** One player per row (a crash costs one row; the guard is asked before each). Reports merge into <outDir>/bot.json. */
export async function runBotLane(outDir: string, o: RunOpts = {}): Promise<BotLaneResult> {
  const say = o.say ?? ((m: string) => console.error(m)), app = o.app ?? BUILD_APP, playing = o.playing ?? whoIsPlaying, t0 = performance.now();
  const build = existsSync(join(app, '..', 'build.json')) ? String(JSON.parse(readFileSync(join(app, '..', 'build.json'), 'utf8')).version) : '?';
  const rows = planRows(JSON.parse(readFileSync(join(ROOT, 'tools/qa/bot.json'), 'utf8')), o.night ?? today(), o);
  const skip = (why: string): BotLaneResult => { say(`qa bot: skipped: ${why}`); return { rows, ran: [], stoppedForSky: false, seconds: 0, build, skipped: why }; };
  if (!existsSync(exe(app))) return skip(`no built player at ${app} (bun run batch builds it)`);
  if (!(o.hasBot ?? playerHasBot)(app)) return skip(`the built player ${build} has no -bot (built before brief 060 tier 2); the next build plays the rows`);
  mkdirSync(outDir, { recursive: true });
  const merged: BotReportRow[] = []; let last: BotReport | undefined; const ran: string[] = []; let stoppedForSky = false;
  for (const row of rows) {
    if (playing().length) { say(`qa bot: Sky is playing; the rows from ${row.name} on are not started`); stoppedForSky = true; break; }
    const dir = join(outDir, row.name); mkdirSync(dir, { recursive: true });
    const planFile = join(outDir, `${row.name}.plan.json`), plan: BotPlan = { schema: 'suborned.bot.plan/1', outDir, rows: [row] }; // the player writes <outDir>/bot.json and <outDir>/<row>/..., so its paths are relative to the lane folder
    const repFile = join(outDir, 'bot.json'); rmSync(repFile, { force: true });
    writeFileSync(planFile, JSON.stringify(plan, null, 1));
    say(`qa bot: ${row.name} (${row.policy}, seed ${row.seed}${row.human ? `, route ${row.human.route}` : ''}, up to ${Math.round(row.seconds / 60)} min) on ${build}`);
    const proc = Bun.spawn([exe(app), '-bot', planFile, outDir, '-capture-size', `${row.w}x${row.h}`, '-novsync', '-logFile', join(dir, 'player.log')], { stdout: 'ignore', stderr: 'ignore' });
    const limit = (row.seconds + 150) * 1000; let killed = false;
    const timer = setTimeout(() => { killed = true; try { process.kill(proc.pid); } catch { /* gone */ } }, limit); // by PID only
    await proc.exited; clearTimeout(timer);
    ran.push(row.name);
    if (existsSync(repFile)) { try { last = JSON.parse(readFileSync(repFile, 'utf8')) as BotReport; merged.push(...last.rows); } catch (e) { say(`qa bot: ${row.name}: bot.json unreadable: ${(e as Error).message}`); } }
    if (killed) say(`qa bot: ${row.name}: stopped after ${Math.round(limit / 1000)} s`);
  }
  const report: BotReport | undefined = last ? { ...last, rows: merged } : undefined;
  if (report) writeFileSync(join(outDir, 'bot.json'), JSON.stringify(report, null, 1));
  return { report, rows, ran, stoppedForSky, seconds: (performance.now() - t0) / 1000, build };
}

if (import.meta.main) {
  const a = process.argv.slice(2), val = (k: string) => { const i = a.indexOf(k); return i >= 0 ? a[i + 1] : undefined; };
  const night = val('--night') ?? today();
  if (a.includes('--plan')) { console.log(JSON.stringify(planRows(JSON.parse(readFileSync(join(ROOT, 'tools/qa/bot.json'), 'utf8')), night, { rows: val('--rows') ? Number(val('--rows')) : undefined }), null, 1)); process.exit(0); }
  const out = val('--run'); if (!out) { console.error('usage: bun tools/qa/bot.ts --plan | --run <outDir> [--rows n] [--minutes m] [--force]'); process.exit(2); }
  if (!a.includes('--force') && whoIsPlaying().length) { console.error('qa bot: Sky is playing; run through `bun run guard` (it waits)'); process.exit(3); }
  const r = await runBotLane(out, { night, rows: val('--rows') ? Number(val('--rows')) : undefined, minutes: val('--minutes') ? Number(val('--minutes')) : undefined, playing: a.includes('--force') ? () => [] : undefined });
  for (const row of r.report?.rows ?? []) console.log(`${row.name}: ${row.status} ${row.outcome}${row.failCode ? ` (${row.failCode})` : ''} at tick ${row.endTick}; ${row.input.taken} ways taken, ${row.orders.refused}/${row.orders.given} refused; ${Object.entries(row.counts).map(([k, v]) => `${k} ${v}`).join(', ') || 'no anomalies'}`);
  if (r.skipped) process.exit(0);
  process.exit(r.ran.length === r.rows.length ? 0 : 1);
}
