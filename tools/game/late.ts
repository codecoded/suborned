// suborned.late/1 (brief 045, Splice): the late-fight frame-time check of `bun run batch`. Today's checks miss the cost of a late save in a fight: the proof samples
// 10 s at the start, and the shot runner's perf window gives no orders and reports a mean and a p99. This plays a whole script in the built player, to the start of
// its last `seconds` (default 60) of mission, then ON at real time with the script's orders given at their ticks (as play gives them), and reads every frame.
//
//   row     name (kebab-case) · script (a route script by name or a path) · seconds (the timed window, default 60) · tail (seconds timed after the mission ends, default 2) · size [w, h] (default 1920 x 1080)
//   the end of a script's mission is what the engine says (`sim play`: the tick and outcome the player must reach too); the window is [end - seconds*10, end] in ticks.
//
// The player's side is `-late <resolved.json> <outDir>` (the contract is in briefs/045-late-lag.md): it writes <outDir>/late.json (suborned.late.report/1) and a frames file
// per row; judgeLate reads them. Fails: any frame over 50 ms (window or tail), p99 over 16.7 ms (window), a frame over 100 ms in the tail (the end card, FailCam), a run
// that did not reach the end tick and outcome the engine gave (the drive diverged, so the numbers are not the script's), or a row the player did not report.
import { existsSync, readFileSync } from 'node:fs';
import { isAbsolute, join, resolve } from 'node:path';
import { z } from 'zod';
import { ROOT, ROUTES } from './shots';

export const LIMITS = { maxFrameMs: 50, p99Ms: 16.7, endFrameMs: 100 } as const;
export const TICKS_PER_SECOND = 10;
/** Ticks run before the window with the clock going, so the timed window opens on a warm, running game (not on the hitch of a fast-forward); frames before the window are not timed. */
export const WARM_TICKS = 30;

export const LateRow = z.object({
  name: z.string().regex(/^[a-z0-9][a-z0-9-]*$/, 'kebab-case: lower case letters, digits and hyphens'),
  note: z.string().optional(), script: z.string(),
  seconds: z.number().min(5).max(300).optional(), tail: z.number().min(0).max(10).optional(),
  size: z.tuple([z.number().int().min(320).max(7680), z.number().int().min(180).max(4320)]).optional(),
}).strict();
export const LateFile = z.object({ schema: z.literal('suborned.late/1'), note: z.string().optional(), rows: z.array(LateRow).min(1) }).strict();
export type LateFile = z.infer<typeof LateFile>;

export type End = { tick: number; outcome: string; fail: string };
export type LateResolved = {
  name: string; scriptPath: string;
  /** the clock runs from warmTick (orders at their ticks), frames are timed from startTick to endTick, then tailSeconds more of real time */
  warmTick: number; startTick: number; endTick: number; outcome: string; failCode: string; tailSeconds: number; w: number; h: number;
};
export type LateResolvedFile = { schema: 'suborned.late.resolved/1'; outDir: string; rows: LateResolved[] };

export const scriptPath = (ref: string) => isAbsolute(ref) ? ref : existsSync(resolve(ROOT, ref)) && ref.endsWith('.json') ? resolve(ROOT, ref) : join(ROOT, ROUTES, ref.endsWith('.json') ? ref : `${ref}.json`);

/** The file the player reads. `endOf` plays a script headless and says where the mission ends (the engine's word, not the script's `until`). */
export function resolveLate(raw: unknown, outDir: string, endOf: (path: string) => End): LateResolvedFile {
  const p = LateFile.safeParse(raw);
  if (!p.success) throw new Error(`late.json has ${p.error.issues.length} problem(s):\n  ${p.error.issues.map(i => `${i.path.join('.') || 'late.json'}: ${i.message}`).join('\n  ')}`);
  const seen = new Set<string>(), rows: LateResolved[] = [], problems: string[] = [];
  for (const r of p.data.rows) {
    if (seen.has(r.name)) problems.push(`${r.name}: the name is used twice`); seen.add(r.name);
    const path = scriptPath(r.script);
    let e: End;
    try { e = endOf(path); } catch (x) { problems.push(`${r.name}: ${(x as Error).message}`); continue; }
    const span = Math.round((r.seconds ?? 60) * TICKS_PER_SECOND);
    if (e.tick < span) { problems.push(`${r.name}: the mission ends at tick ${e.tick}, before a ${r.seconds ?? 60} s window`); continue; }
    rows.push({ name: r.name, scriptPath: path, startTick: e.tick - span, warmTick: Math.max(0, e.tick - span - WARM_TICKS), endTick: e.tick, outcome: e.outcome, failCode: e.fail, tailSeconds: r.tail ?? 2, w: r.size?.[0] ?? 1920, h: r.size?.[1] ?? 1080 });
  }
  if (problems.length) throw new Error(`late.json: ${problems.join('; ')}`);
  return { schema: 'suborned.late.resolved/1', outDir, rows };
}

/** What the player reports per row (suborned.late.report/1, <outDir>/late.json). Frame times in ms, one per rendered frame (unscaled delta). */
export type LateReportRow = {
  name: string; status: 'ok' | 'failed'; error?: string;
  /** where the run really ended: must be the resolved endTick and outcome */
  endTick: number; outcome: string; failCode?: string;
  ordersGiven: number; ordersRefused?: number;
  window: { count: number; meanMs: number; p99Ms: number; maxMs: number; over50: number };
  tail: { count: number; maxMs: number; over50: number; over100: number };
  /** the worst frames of window and tail, worst first (at least 5): ms, game tick and what the host did that frame if it said (save, order, end) */
  worst?: { ms: number; tick: number; what?: string }[];
  /** the file with every timed frame: "ms,tick,phase" per line (phase w or t), next to late.json */
  frames?: string;
};
export type LateReport = { schema: 'suborned.late.report/1'; status: string; screen: string; build?: string; rows: LateReportRow[] };

const f1 = (n: number) => (Math.round(n * 10) / 10).toString();

// ---------------------------------------------------------------- findings (one run), then the verdict (one or two runs)
//
// The first version failed on any frame over 50 ms. On the shared Studio that failed a good build twice: stray 51-97 ms frames that moved between runs and
// coincided with no save, order or end. A frame is now sorted by what the host says it did that frame (`what` in the report: save, order, end, or nothing):
//   event       a frame over 50 ms in a frame that held a save, an order or the end, or a tail frame over 100 ms (the end card, End(), FailCam). The old per-save
//               replay (68-273 ms on every order and every 5 s) and the 1.9 s end freeze are this.
//   freeze      a frame of FREEZE_MS or more, whatever it held (an unattributed stall that big is not a stray)
//   stray-many  STRAY_MANY or more frames over 50 ms with no event in them (a hitch that repeats is not load)
//   p99         the window's p99 over 16.7 ms (p99 and stray-many are only advisory when the machine was noisy during that run: load does that to any frame loop)
//   structural  the drive did not end where the engine's replay does, too few frames, a row not run, a report that did not come
// A lone stray over 50 ms is noted, never failed. A row with findings is run once more, and it FAILS only when a finding of the same class comes back:
// a real regression recurs on every run (it is on every save), load does not. Structural problems found on the second run always fail (it cannot be told
// from a broken drive). The machine's load is recorded with each run and flags "noisy machine" when high.
export type FindingClass = 'event' | 'freeze' | 'stray-many' | 'p99' | 'structural';
/** frames over 50 ms with no save, order or end in them: how many, and the worst ones the player listed */
export type Stray = { count: number; ms: number[] };
export type Finding = { row: string; klass: FindingClass; msg: string };
export const FREEZE_MS = 250;
export const STRAY_MANY = 3;
/** What the host did in a frame, from the report's `what` ("order+save", "end", "" or "tail" for a tail frame with nothing). */
export function eventOf(what: string | undefined): '' | 'save' | 'order' | 'end' {
  const w = what ?? '';
  return /end/.test(w) ? 'end' : /save/.test(w) ? 'save' : /order/.test(w) ? 'order' : '';
}

/** The findings of one report against the resolved rows: per row the line and the findings. `rows` limits the rows judged (a rerun holds only the rows that needed it). */
export function findLate(resolved: LateResolvedFile, rep: LateReport | undefined): { findings: Finding[]; lines: string[]; strays: Record<string, Stray> } {
  const findings: Finding[] = [], lines: string[] = [], strays: Record<string, Stray> = {};
  if (!rep || rep.schema !== 'suborned.late.report/1') return { findings: [{ row: '', klass: 'structural', msg: 'the player wrote no suborned.late.report/1' }], lines, strays };
  const add = (row: string, klass: FindingClass, msg: string) => findings.push({ row, klass, msg: `${row}: ${msg}` });
  if (/^(unreadable|no host)/.test(rep.status)) add('player', 'structural', `player: ${rep.status}`);
  for (const r of resolved.rows) {
    const x = rep.rows.find(y => y.name === r.name);
    if (!x) { add(r.name, 'structural', 'not run (the player stopped before it)'); continue; }
    if (x.status !== 'ok') { add(r.name, 'structural', x.error ?? 'failed'); continue; }
    const w = x.window, t = x.tail;
    if (x.endTick !== r.endTick || x.outcome !== r.outcome) add(r.name, 'structural', `ended at tick ${x.endTick} (${x.outcome}), the engine's replay ends at ${r.endTick} (${r.outcome}): the drive diverged`);
    if (w.count < 100) add(r.name, 'structural', `only ${w.count} frames timed in the window`);
    const worst = x.worst ?? [], over = w.over50 + t.over50, slow = worst.filter(f => f.ms > LIMITS.maxFrameMs);
    const tied = slow.filter(f => eventOf(f.what));
    const tailMax = t.maxMs, tailOver100 = t.over100 > 0 || tailMax > LIMITS.endFrameMs;
    if (tied.length) add(r.name, 'event', `${tied.length > 1 ? `${tied.length} frames` : 'a frame'} over ${LIMITS.maxFrameMs} ms held a save, order or the end: ${tied.slice(0, 3).map(f => `${f1(f.ms)} ms at tick ${f.tick} (${f.what})`).join(', ')}${over > tied.length ? `; ${over} over ${LIMITS.maxFrameMs} ms in all` : ''}`);
    if (tailOver100) add(r.name, 'event', `${f1(tailMax)} ms at the end of the mission (limit ${LIMITS.endFrameMs})`);
    const maxAll = Math.max(w.maxMs, t.maxMs);
    if (maxAll >= FREEZE_MS) add(r.name, 'freeze', `a frame took ${f1(maxAll)} ms (a freeze is ${FREEZE_MS} ms or more, whatever it held)`);
    const strayCount = Math.max(0, over - tied.length);
    if (strayCount > 0) strays[r.name] = { count: strayCount, ms: slow.filter(f => !eventOf(f.what)).map(f => f.ms) };
    if (strayCount >= STRAY_MANY) add(r.name, 'stray-many', `${strayCount} frames over ${LIMITS.maxFrameMs} ms with no save, order or end in them`);
    if (w.p99Ms > LIMITS.p99Ms) add(r.name, 'p99', `p99 ${f1(w.p99Ms)} ms (limit ${LIMITS.p99Ms})`);
    const wf = worst[0];
    lines.push(`${r.name}: worst ${f1(maxAll)} ms${wf ? ` (tick ${wf.tick}${wf.what ? ', ' + wf.what : ''})` : ''}, p99 ${f1(w.p99Ms)} ms, mean ${f1(w.meanMs)} ms over ${w.count} frames, end ${f1(t.maxMs)} ms; ${x.ordersGiven} orders${strayCount > 0 ? `; ${strayCount} stray frame(s) over ${LIMITS.maxFrameMs} ms (no event)` : ''}`);
  }
  return { findings, lines, strays };
}

/** One run, strictly: every finding is a problem (a lone stray too). Kept for a single reading of a report; the batch row uses runLate. */
export function judgeLate(resolved: LateResolvedFile, rep: LateReport | undefined): { ok: boolean; problems: string[]; lines: string[] } {
  const f = findLate(resolved, rep), problems = f.findings.map(x => x.msg);
  for (const [row, st] of Object.entries(f.strays)) problems.push(`${row}: ${st.count} stray frame(s) over ${LIMITS.maxFrameMs} ms with no event${st.ms.length ? ` (${st.ms.map(f1).join(', ')} ms)` : ''}`);
  return { ok: problems.length === 0, problems, lines: f.lines };
}

/** Machine load around one run of the player (os.loadavg, 1 minute, sampled while it ran) and what was using the CPU. */
export type LoadSample = { ncpu: number; start: number; peak: number; end: number; top: string[] };
/** "noisy machine": the 1-minute load average reached half the cores while the row ran. (The player itself is two or three cores; the Studio idles at 3 to 8 of 18.) */
export const NOISY_LOAD_PER_CORE = 0.6;
export const noisy = (l: LoadSample | undefined): boolean => !!l && l.peak >= NOISY_LOAD_PER_CORE * l.ncpu;
export const loadText = (l: LoadSample | undefined): string => l ? `load ${f1(l.start)} -> peak ${f1(l.peak)} -> ${f1(l.end)} on ${l.ncpu} cores${noisy(l) ? ' · NOISY MACHINE' : ''}${l.top.length ? ` · busiest: ${l.top.join(', ')}` : ''}` : 'load not recorded';

export type LateAttempt = { n: number; rows: string[]; advisory: Finding[]; report?: LateReport; load?: LoadSample; findings: Finding[]; lines: string[]; strays: Record<string, Stray> };
export type LateVerdict = { ok: boolean; problems: string[]; lines: string[]; notes: string[]; noisy: boolean; attempts: LateAttempt[]; report?: LateReport };
/** Play the rows (`play` gets the rows to run and the attempt number); a row with findings goes once more, and fails only if a finding of the same class returns. */
export async function runLate(resolved: LateResolvedFile, play: (rows: LateResolvedFile, n: number) => Promise<{ report?: LateReport; load?: LoadSample }>): Promise<LateVerdict> {
  const attempts: LateAttempt[] = [];
  const once = async (rows: LateResolvedFile, n: number): Promise<LateAttempt> => {
    const p = await play(rows, n), f = findLate(rows, p.report);
    // on a noisy machine a p99 or a crowd of strays is what load does to any frame loop: noted, not judged (a save, an order, the end or a freeze still are)
    const soft = noisy(p.load) ? f.findings.filter(x => x.klass === 'p99' || x.klass === 'stray-many') : [];
    if (soft.length) f.findings = f.findings.filter(x => !soft.includes(x));
    const a: LateAttempt = { n, rows: rows.rows.map(r => r.name), report: p.report, load: p.load, ...f, advisory: soft };
    attempts.push(a); return a;
  };
  const first = await once(resolved, 1);
  const rowsOf = (a: LateAttempt, name: string) => a.findings.filter(f => f.row === name || f.row === '' || f.row === 'player');
  const again = resolved.rows.filter(r => rowsOf(first, r.name).length > 0);
  const problems: string[] = [], notes: string[] = [];
  let second: LateAttempt | undefined;
  if (again.length) second = await once({ ...resolved, rows: again }, 2);
  const merged: LateReportRow[] = [];
  for (const r of resolved.rows) {
    const f1s = rowsOf(first, r.name), f2s = second && again.includes(r) ? rowsOf(second, r.name) : [];
    const row = (second && again.includes(r) ? second.report : first.report)?.rows.find(x => x.name === r.name) ?? first.report?.rows.find(x => x.name === r.name);
    if (row) merged.push(row);
    if (!f1s.length) continue;
    const classes = new Set(f1s.map(f => f.klass));
    const repeated = f2s.filter(f => classes.has(f.klass) || f.klass === 'structural');
    if (repeated.length) { for (const f of repeated) problems.push(`${f.msg} (seen on both runs${f.klass === 'structural' ? '' : `: first run ${f1s.filter(x => x.klass === f.klass).map(x => x.msg.slice(r.name.length + 2)).join('; ')}`})`); }
    else notes.push(`${r.name}: first run saw ${f1s.map(f => f.klass).join(', ')} (${f1s.map(f => f.msg.slice(r.name.length + 2)).join('; ')}); the rerun did not repeat it: not counted${noisy(first.load) ? ', and the machine was noisy' : ''}`);
  }
  for (const a of attempts) for (const f of a.advisory) notes.push(`${f.msg} (run ${a.n}): NOISY MACHINE, not judged; rerun on a quiet machine to judge it`);
  for (const a of attempts) for (const [row, st] of Object.entries(a.strays)) notes.push(`${row}: run ${a.n} had ${st.count} stray frame(s) over ${LIMITS.maxFrameMs} ms with no save, order or end${st.ms.length ? ` (${st.ms.map(f1).join(', ')} ms)` : ''}: not a failure alone`);
  const unrow = first.findings.filter(f => f.row === '' || f.row === 'player').map(f => f.msg);
  if (unrow.length && second?.findings.some(f => f.row === '' || f.row === 'player')) problems.push(...unrow);
  const isNoisy = attempts.some(a => noisy(a.load));
  const lineOf = (a: LateAttempt | undefined, name: string) => a?.lines.find(l => l.startsWith(name + ':'));
  const lines = resolved.rows.flatMap(r => { const l = (second && again.includes(r) ? lineOf(second, r.name) : undefined) ?? lineOf(first, r.name); return l ? [l] : []; });
  const report = first.report ? { ...first.report, rows: merged } : second?.report;
  const loads = attempts.map(a => `run ${a.n} (${a.rows.join(', ')}): ${loadText(a.load)}`);
  return { ok: problems.length === 0, problems, lines, notes: [...notes, ...loads], noisy: isNoisy, attempts, report };
}

export const readJsonFile = (p: string): unknown => JSON.parse(readFileSync(p, 'utf8'));
