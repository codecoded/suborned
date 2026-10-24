// Brief 060 tier 2 (Latch; Sky approved 2026-10-24): the `bot` lane of a QA pass. The built player plays itself through real input (`-bot`,
// unity/Assets/Suborned/Runtime/Play/BotRun.cs): each row is one whole Handover at real time (~10 minutes with boot), chosen by a seeded policy
// (random, greedy, cautious) from the game's own ring offers. The player checks every frame (Q-GIVER, Q-OFFER, Q-WARN-HUD, Q-CARD, Q-FEET,
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
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { z } from 'zod';
import { ROOT } from '../missions/check/engine';
import { whoIsPlaying } from '../game/playing';
import { BUILD_APP } from './sim';
import type { Finding, Severity } from './types';

export const POLICIES = ['random', 'greedy', 'cautious'] as const;
export const BotRow = z.object({
  name: z.string().regex(/^[a-z0-9][a-z0-9-]*$/), policy: z.enum(POLICIES), seed: z.number().int().min(0).optional(),
  minutes: z.number().min(1).max(30).optional(), decideS: z.number().min(0.5).max(30).optional(), note: z.string().optional(),
}).strict();
export const BotFile = z.object({ schema: z.literal('suborned.bot/1'), note: z.string().optional(), rows: z.array(BotRow).min(1) }).strict();
export type BotPlanRow = { name: string; policy: string; seed: number; seconds: number; decideS: number; w: number; h: number };
export type BotPlan = { schema: 'suborned.bot.plan/1'; outDir: string; rows: BotPlanRow[] };

/** A night's seed for a row: the row's own seed (or its index) mixed with the date, so each night plays new games, reproducibly. */
export function nightSeed(night: string, base: number): number {
  let h = 2166136261 >>> 0; for (const ch of `${night}:${base}`) { h ^= ch.charCodeAt(0); h = Math.imul(h, 16777619) >>> 0; }
  return h % 1_000_000;
}

/** The rows of a night: tools/qa/bot.json, the seeds moved by the date unless `fixed`; `rows` cuts the list, `minutes` overrides each row's cap. */
export function planRows(raw: unknown, night: string, o: { rows?: number; minutes?: number; fixed?: boolean } = {}): BotPlanRow[] {
  const f = BotFile.parse(raw);
  const rows = f.rows.slice(0, o.rows ?? f.rows.length);
  return rows.map((r, i) => ({
    name: r.name, policy: r.policy, seed: o.fixed ? r.seed ?? i + 1 : nightSeed(night, r.seed ?? i + 1),
    seconds: Math.round((o.minutes ?? r.minutes ?? 11) * 60), decideS: r.decideS ?? 0, w: 1920, h: 1080,
  }));
}

// ---------------------------------------------------------------- the player's report

export type BotAnomaly = { check: string; key: string; title: string; expected: string; observed: string; tick: number; clock: string; at: [number, number]; shot: string; script: string };
export type BotReportRow = {
  name: string; policy: string; seed: number; status: string; error?: string; preset: string; seconds: number; endTick: number; outcome: string; failCode: string; cardHead: string;
  input: { decisions: number; clicks: number; rings: number; ringMisses: number; taken: number; moves: number; runs: number; pans: number; turns: number };
  orders: { given: number; refused: number; codes: Record<string, number> };
  frames: { count: number; meanMs: number; p99Ms: number; maxMs: number; over50: number };
  memory: { startMB: number; endMB: number; slopeMBPerMin: number };
  consoleErrors: number; feetSamples: number; figures: number; shots: number; counts: Record<string, number>; anomalies: BotAnomaly[];
};
export type BotReport = { schema: 'suborned.bot.report/1'; status: string; screen: string; build: string; thresholds: Record<string, number>; rows: BotReportRow[] };

/** Severity and owner (§14) by check: the host's checks are the player's, the look and copy go to their owners through Aster. */
export const CHECK_META: Record<string, { severity: Severity; owner: string }> = {
  'Q-GIVER': { severity: 'S2', owner: 'Latch' }, 'Q-OFFER': { severity: 'S2', owner: 'Latch (host offer) / Keel (engine refusal)' },
  'Q-WARN-HUD': { severity: 'S2', owner: 'Latch / Glyph' }, 'Q-CARD': { severity: 'S1', owner: 'Latch / Fable' },
  'Q-FEET': { severity: 'S3', owner: 'Latch / Splice (walk file)' }, 'Q-SOLID': { severity: 'S3', owner: 'Latch / Pulse (paths)' },
  'Q-ERR': { severity: 'S1', owner: 'Latch' }, 'Q-FRAME': { severity: 'S4', owner: 'Redline' }, 'Q-MEM': { severity: 'S4', owner: 'Redline' },
  'Q-STUCK': { severity: 'S1', owner: 'Latch / Keel' }, 'Q-BOT': { severity: 'S1', owner: 'Latch' },
};

/** The findings of one row: one per anomaly the player kept (its replay and shot ride along), and a row that broke or never ran. */
export function findingsOf(row: BotReportRow | undefined, name: string, base: string): (Finding & { shot?: string; script?: string })[] {
  if (!row) return [{ check: 'Q-BOT', severity: 'S1', owner: 'Latch', key: 'Q-BOT:not-run', title: 'A bot row did not run', expected: 'every row plays to its end', observed: `${name}: the player wrote no row (it crashed, was stopped or Sky opened the game)` }];
  const out: (Finding & { shot?: string; script?: string })[] = [];
  if (row.status === 'failed') out.push({ check: 'Q-BOT', severity: 'S1', owner: 'Latch', key: 'Q-BOT:failed', title: 'A bot row broke', expected: 'the row plays to its end', observed: `${name}: ${row.error ?? 'failed'}` });
  for (const a of row.anomalies) {
    const m = CHECK_META[a.check] ?? { severity: 'S3' as Severity, owner: 'Suture' };
    out.push({
      check: a.check, severity: m.severity, owner: m.owner, key: `${a.check}:${a.key.replace(/[0-9]+/g, '#').slice(0, 80)}`, title: a.title,
      expected: a.expected, observed: `${a.observed} (${row.policy} seed ${row.seed}, ${a.clock})`, tick: a.tick,
      shot: a.shot ? join(base, a.shot) : undefined, script: a.script ? join(base, a.script) : undefined,
    });
  }
  return out;
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
    const planFile = join(outDir, `${row.name}.plan.json`), plan: BotPlan = { schema: 'suborned.bot.plan/1', outDir: dir, rows: [row] };
    writeFileSync(planFile, JSON.stringify(plan, null, 1));
    say(`qa bot: ${row.name} (${row.policy}, seed ${row.seed}, up to ${Math.round(row.seconds / 60)} min) on ${build}`);
    const proc = Bun.spawn([exe(app), '-bot', planFile, dir, '-capture-size', `${row.w}x${row.h}`, '-novsync', '-logFile', join(dir, 'player.log')], { stdout: 'ignore', stderr: 'ignore' });
    const limit = (row.seconds + 150) * 1000; let killed = false;
    const timer = setTimeout(() => { killed = true; try { process.kill(proc.pid); } catch { /* gone */ } }, limit); // by PID only
    await proc.exited; clearTimeout(timer);
    ran.push(row.name);
    const repFile = join(dir, 'bot.json');
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
