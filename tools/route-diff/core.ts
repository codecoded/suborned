// route-diff core (brief 034 item 1): pure functions. A play (the engine's `play <script> --json` report) is boiled down to a Row; two
// Rows for the same route are compared into a Moved record (which columns moved, the first log line where the runs part). No I/O here.
import type { Play } from '../missions/check/engine';

/** m:ss.d, as the engine's Report.Clock prints it (a tenth of a second is a tick). */
export const clockOf = (tick: number) => { const s = Math.floor(tick / 10); return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}.${tick % 10}`; };

export type Health = { lost: number; total: number; pct: number };
/** What the table reads of one play, and what the divergence search needs; small enough to cache. */
export type Row = {
  id: string; outcome: string; fail: string; tick: number; trail: number; alert: number; gunfire: number; standing: number;
  health: Health; startHash: string; finalHash: string; scriptSha: string;
  /** "tick<TAB>code<TAB>text" per event-log line */
  log: string[]; checkpoints: [number, string][];
  /** the engine refused the script, or died: the first line of what it said */
  error?: string;
};

export const OPS = ['scout', 'fixer', 'tech', 'enforcer'] as const;
/** Rulebook values the health sum needs (weapons.health: health-op, health-enforcer). */
export type Healths = { op: number; enforcer: number; /** lab-health-scale: what a lab start multiplies every health by */ lab?: number };

/** Health the cell lost, from the event log: the play report carries no health, but every hit on an operative logs "<op>.hit.* … N health left."
 *  and every drop "<op>.down". An op's loss is its maximum minus the last "health left" (all of it once down). A lab start (health × lab-health-scale)
 *  is recognised by a reading above a plain maximum. */
export function healthLost(log: { code: string; text: string }[], max: Healths): Health {
  const left: Record<string, number> = {}, down = new Set<string>();
  let scale = 1;
  for (const l of log) {
    const op = OPS.find(o => l.code.startsWith(`${o}.hit.`));
    if (op) {
      const n = Number(/(\d+) health left/.exec(l.text)?.[1]);
      if (Number.isFinite(n)) { left[op] = n; if (n > (op === 'enforcer' ? max.enforcer : max.op)) scale = max.lab ?? 10; }
    } else {
      const d = OPS.find(o => l.code === `${o}.down`);
      if (d) down.add(d);
    }
  }
  let lost = 0, total = 0;
  for (const op of OPS) {
    const m = (op === 'enforcer' ? max.enforcer : max.op) * scale;
    total += m;
    lost += down.has(op) ? m : left[op] === undefined ? 0 : Math.max(0, m - left[op]!);
  }
  return { lost, total, pct: total ? Math.floor(lost * 100 / total) : 0 };
}

export function rowOf(id: string, p: Play, scriptSha: string, max: Healths): Row {
  return {
    id, outcome: p.outcome, fail: p.fail, tick: p.tick, trail: p.trail, alert: p.alert, gunfire: p.gunfire, standing: p.standing,
    health: healthLost(p.log, max), startHash: p.startHash, finalHash: p.finalHash, scriptSha,
    log: p.log.map(l => `${l.tick}\t${l.code}\t${l.text}`), checkpoints: p.checkpoints.map(c => [c.tick, c.hash]),
  };
}
export const errorRow = (id: string, scriptSha: string, error: string): Row => ({ id, outcome: 'error', fail: '', tick: 0, trail: 0, alert: 0, gunfire: 0, standing: 0, health: { lost: 0, total: 0, pct: 0 }, startHash: '', finalHash: '', scriptSha, log: [], checkpoints: [], error });

export type Divergence = {
  /** index of the first event-log line that differs (or where one log ends); -1 when the logs are the same and only a checkpoint hash differs */
  at: number; a?: string; b?: string;
  /** every line up to and including `at` has the same tick and code in both runs: only the words differ (a reworded log line is not a move) */
  wordingOnly?: boolean;
  /** the first checkpoint tick whose state hash differs, if any */
  checkpointTick?: number;
};

const sig = (l: string | undefined) => l === undefined ? undefined : l.split('\t').slice(0, 2).join('\t');

/** The first log line where two runs differ, in tick and code (the behaviour); wording-only when nothing but text differs. undefined when the logs are identical. */
export function firstDivergence(a: Row, b: Row): Divergence | undefined {
  const n = Math.max(a.log.length, b.log.length);
  let at = -1, text = -1;
  for (let i = 0; i < n; i++) {
    if (text < 0 && a.log[i] !== b.log[i]) text = i;
    if (sig(a.log[i]) !== sig(b.log[i])) { at = i; break; }
  }
  let checkpointTick: number | undefined;
  const m = Math.min(a.checkpoints.length, b.checkpoints.length);
  for (let i = 0; i < m; i++) if (a.checkpoints[i]![1] !== b.checkpoints[i]![1]) { checkpointTick = Math.min(a.checkpoints[i]![0], b.checkpoints[i]![0]); break; }
  if (at >= 0) return { at, a: a.log[at], b: b.log[at], checkpointTick };
  if (text >= 0) return { at: text, a: a.log[text], b: b.log[text], wordingOnly: true, checkpointTick };
  if ((checkpointTick === undefined || a.startHash !== b.startHash) && a.checkpoints.length === b.checkpoints.length) return undefined; // different starts: the hashes all differ and say nothing
  return { at: -1, checkpointTick: checkpointTick ?? Math.min(a.checkpoints.at(-1)?.[0] ?? 0, b.checkpoints.at(-1)?.[0] ?? 0) };
}

export const COLUMNS = ['outcome', 'fail', 'end', 'trail', 'alert', 'health'] as const;
export type Column = typeof COLUMNS[number];
export type Moved = {
  id: string; status: 'same' | 'moved' | 'added' | 'removed';
  a?: Row; b?: Row;
  /** columns whose value differs */
  cols: Column[];
  /** the same route's script text differs between the two sides (so part of the move is the script's, not the engine's) */
  scriptChanged: boolean;
  divergence?: Divergence;
};

/** Value of a column as the table prints it. */
export function cell(r: Row, c: Column): string {
  switch (c) {
    case 'outcome': return r.outcome;
    case 'fail': return r.fail || '-';
    case 'end': return clockOf(r.tick);
    case 'trail': return String(r.trail);
    case 'alert': return String(r.alert);
    case 'health': return `${r.health.pct} %`;
  }
}

export function compare(id: string, a: Row | undefined, b: Row | undefined): Moved {
  if (!a || !b) return { id, status: a ? 'removed' : 'added', a, b, cols: [], scriptChanged: false };
  const cols = COLUMNS.filter(c => cell(a, c) !== cell(b, c));
  const scriptChanged = a.scriptSha !== b.scriptSha, divergence = firstDivergence(a, b);
  // The start state hashes the inputs, so when any input differs every hash does and says nothing; only when the two runs began from the
  // same state is a final hash that differs, with every column and every log line the same, a move in state alone.
  const sameStart = a.startHash === b.startHash && !a.error && !b.error;
  const stateOnly = sameStart && a.finalHash !== b.finalHash && !cols.length && divergence?.at === -1;
  const moved = cols.length > 0 || (!!divergence && divergence.at >= 0 && !divergence.wordingOnly) || stateOnly || a.error !== b.error;
  return { id, status: moved ? 'moved' : 'same', a, b, cols, scriptChanged, divergence };
}

/** Every route on either side, in name order. */
export function compareAll(a: Map<string, Row>, b: Map<string, Row>): Moved[] {
  return [...new Set([...a.keys(), ...b.keys()])].sort().map(id => compare(id, a.get(id), b.get(id)));
}

const pad = (s: string, n: number) => s.length >= n ? s : s + ' '.repeat(n - s.length);
const trunc = (s: string, n: number) => s.length > n ? s.slice(0, n - 1) + '…' : s;
const logLine = (l: string | undefined) => { if (l === undefined) return '(log ends)'; const [t, code, ...text] = l.split('\t'); return `${clockOf(Number(t))}  ${code}  ${text.join('\t')}`; };

/** One table: only routes that moved (all of them with `all`), then one divergence block under each. */
export function renderTable(moved: Moved[], o: { all?: boolean; labelA: string; labelB: string; width?: number }): string {
  const shown = moved.filter(m => o.all || m.status !== 'same');
  const out: string[] = [];
  const head = ['route', 'outcome', 'fail', 'end', 'Trail', 'Alert', 'health lost'];
  const rows = shown.map(m => {
    if (m.status === 'added' || m.status === 'removed') { // the route exists on one side only: its values, tagged
      const r = (m.a ?? m.b)!, tag = m.status === 'added' ? 'B only' : 'A only';
      return [`${m.id} [${tag}]`, ...COLUMNS.map(c => cell(r, c))];
    }
    const f = (c: Column) => m.cols.includes(c) ? `${cell(m.a!, c)} → ${cell(m.b!, c)}` : m.status === 'same' ? cell(m.a!, c) : '=';
    return [m.id, f('outcome'), f('fail'), f('end'), f('trail'), f('alert'), f('health')];
  });
  const w = head.map((h, i) => Math.max(h.length, ...rows.map(r => r[i]!.length)));
  out.push(`A = ${o.labelA}    B = ${o.labelB}    (= means the value did not move)`);
  out.push(head.map((h, i) => pad(h, w[i]!)).join('  ').trimEnd());
  shown.forEach((m, k) => {
    out.push(rows[k]!.map((c, i) => pad(c, w[i]!)).join('  ').trimEnd());
    const tags: string[] = [];
    if (m.status === 'moved' && m.scriptChanged) tags.push('the route script differs between A and B');
    if (m.a?.error || m.b?.error) tags.push(`engine said: A ${m.a?.error ? trunc(m.a.error, 90) : 'ok'}; B ${m.b?.error ? trunc(m.b.error, 90) : 'ok'}`);
    for (const t of tags) out.push(`    note  ${t}`);
    const d = m.divergence;
    if (m.status === 'same' && d?.wordingOnly) out.push(`    note  no behaviour moved; log wording differs from line ${d.at + 1}`);
    if (m.status === 'moved' && d && !m.a?.error && !m.b?.error) {
      const n = o.width ?? 150;
      if (d.at < 0) out.push(`    parts at checkpoint ${clockOf(d.checkpointTick ?? 0)}: the logs are the same line for line, the state hash is not`);
      else {
        out.push(`    first log line apart (line ${d.at + 1}):`);
        out.push(`      A  ${trunc(logLine(d.a), n)}`);
        out.push(`      B  ${trunc(logLine(d.b), n)}`);
      }
    } else if (m.status === 'moved' && !d && !m.cols.length && !tags.length) out.push('    state hash differs, nothing in the log or the columns');
  });
  const c = (s: Moved['status']) => moved.filter(m => m.status === s).length, words = moved.filter(m => m.status === 'same' && m.divergence?.wordingOnly).length;
  out.push('');
  out.push(`${moved.length} routes: ${c('moved')} moved, ${c('same')} unchanged${words ? ` (${words} of them with reworded log lines; --all lists them)` : ''}${c('added') ? `, ${c('added')} only on B` : ''}${c('removed') ? `, ${c('removed')} only on A` : ''}`);
  return out.join('\n');
}

// ---------------------------------------------------------------- the matrix (fight-lab's prepared rows)

/** One printed row of `suborned-sim fight-lab`: preset, run and what the table carries. */
export type LabRow = { key: string; result: string; end: string; trail: string; alert: string; standing: string; down: string; capt: string; health: string; verdict: string };

/** Parse fight-lab's text table (sim/cli/FightLab.cs prints it; the fixed columns are preset, run, result, end, T(m/tot), A, St, pub, inv, cases, down, capt, hp-lost %, …, verdict). */
export function parseLab(text: string): LabRow[] {
  const re = /^(\S+)\s+((?:P|L)-?[A-Z0-9]+|L\d)\s+(\S+(?: [a-z][\w-]*)?)\s+(\d+:\d\d(?:\.\d)?)\s+(\d+)\/(\d+)\s+(\d+)\s+(-?\d+)→(-?\d+)\s+\S+\s+\S+\s+\d+\s+(\d+)\s+(\d+)\s+(\d+) %\s.*?\s(pass|FAIL)(?: — (.*?))?(?:\s+REPLAY DIFFERS)?\s*$/;
  const rows: LabRow[] = [];
  for (const line of text.split('\n')) {
    const m = re.exec(line);
    if (!m) continue;
    rows.push({ key: `${m[1]} ${m[2]}`, result: m[3]!, end: m[4]!, trail: `${m[5]}/${m[6]}`, alert: m[7]!, standing: `${m[8]}→${m[9]}`, down: m[10]!, capt: m[11]!, health: `${m[12]} %`, verdict: m[13] === 'pass' ? 'pass' : `FAIL${m[14] ? ' — ' + m[14] : ''}` });
  }
  return rows;
}

const LAB_COLS: [keyof LabRow, string][] = [['result', 'result'], ['end', 'end'], ['trail', 'Trail mission/total'], ['alert', 'Alert'], ['standing', 'Standing'], ['down', 'ops down'], ['capt', 'captured'], ['health', 'health lost'], ['verdict', 'verdict']];
export type LabMoved = { key: string; status: 'same' | 'moved' | 'added' | 'removed'; a?: LabRow; b?: LabRow; cols: (keyof LabRow)[] };
export function compareLab(a: LabRow[], b: LabRow[]): LabMoved[] {
  const A = new Map(a.map(r => [r.key, r])), B = new Map(b.map(r => [r.key, r]));
  return [...new Set([...A.keys(), ...B.keys()])].map(key => {
    const x = A.get(key), y = B.get(key);
    if (!x || !y) return { key, status: x ? 'removed' as const : 'added' as const, a: x, b: y, cols: [] };
    const cols = LAB_COLS.map(c => c[0]).filter(c => x[c] !== y[c]);
    return { key, status: cols.length ? 'moved' as const : 'same' as const, a: x, b: y, cols };
  });
}
export function renderLab(moved: LabMoved[], o: { all?: boolean }): string {
  const shown = moved.filter(m => o.all || m.status !== 'same');
  const head = ['matrix row', ...LAB_COLS.map(c => c[1])];
  const rows = shown.map(m => [m.key, ...LAB_COLS.map(([c]) => m.status === 'added' || m.status === 'removed' ? (c === 'result' ? `${m.status} (${(m.a ?? m.b)![c]})` : '') : m.cols.includes(c) ? `${m.a![c]} → ${m.b![c]}` : m.status === 'same' ? m.a![c] : '=')]);
  const w = head.map((h, i) => Math.max(h.length, ...rows.map(r => r[i]!.length)));
  const c = (s: LabMoved['status']) => moved.filter(m => m.status === s).length;
  return [head.map((h, i) => pad(h, w[i]!)).join('  ').trimEnd(), ...rows.map(r => r.map((x, i) => pad(x, w[i]!)).join('  ').trimEnd()), '', `${moved.length} matrix rows: ${c('moved')} moved, ${c('same')} unchanged${c('added') ? `, ${c('added')} only on B` : ''}${c('removed') ? `, ${c('removed')} only on A` : ''}`].join('\n');
}
