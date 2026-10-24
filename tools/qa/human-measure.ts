// Brief 060 tier 2 (Suture, 2026-10-25; Sky: "I think the biggest influence would be a bot that's indecisive. Humans aren't consistent with their
// actions."): measure how Sky actually plays from his saved sessions, and write the profile the `human` bot policy reads (tools/qa/human-profile.json,
// Latch's tools/qa/human.ts readProfile: probabilities as bare numbers, times as {p50, p90}, under `values`; each value's provenance under `source`).
//
// Whose sessions. The play folder holds ~5,900 saved sessions; almost all are automation: proofs, the late-fight and tp-long rows, shots, the QA host.
// A person plays at real time: the wall clock from the file's name (its first save) to its last write is at least 0.9 × the mission time it holds
// (pausing only makes it longer). Automation fast-forwards (ratio far below 1), or plays one order sequence in every batch (a real-time proof:
// the same op/verb/target sequence in two real-time files). A session whose first orders copy an earlier file's (a Continue, a replay) counts only
// its new orders. tools/qa/seeds.ts's "over 8 KB" rule does not separate them: of its 176 seeds, 121 are one 17-second tp-long proof shape and
// ~40 are late-row replays of Sky's 2026-10-22 session.
//
// What a session can show: every order the host sent the engine (taken or refused, Script.Write) at its tick, third-person drive samples, and the
// end. What it cannot: selection (F1-F4) without an order, ring opens and closes, hovers, misclicks that opened nothing, camera turns (Q/E), pans,
// zoom, pauses (only their total, from the wall-clock ratio). Those values are estimated, and say so.
//
// Refusal codes need the engine Sky played. `--replay` plays each session on the committed engine of its recorded version (the nearest commit
// that reproduces its recorded checkpoints) and keeps the play under out/qa/human/plays/; only plays whose checkpoints all match are read for
// refusals ("faithful"). Engines that were never committed (0.8.1, 0.10.0, 0.14.1, 0.20.0, 0.24.0, 0.25.0) leave their sessions unjudged.
//
// usage: bun run guard bun tools/qa/human-measure.ts [--replay] [--write] [--dirs a,b]
//   --replay  replay Sky's sessions on their engines first (a few minutes: one engine build per version)
//   --write   write tools/qa/human-profile.json and briefs/060/qa/human-measures.json (committed evidence)
import { existsSync, mkdirSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT, engineAt, play } from '../missions/check/engine';
import { SESSIONS_DIR } from './seeds';

export const EDITOR_SESSIONS_DIR = join(ROOT, 'out/play');
export const PLAYS_DIR = join(ROOT, 'out/qa/human/plays');
export const PROFILE_OUT = join(ROOT, 'tools/qa/human-profile.json');
export const MEASURES_OUT = join(ROOT, 'briefs/060/qa/human-measures.json');

/** The rules of the measure, [S]: tuned when more sessions arrive. */
export const RULES = {
  realTimeRatio: 0.9,  // wall / mission at least this: a person's play
  minTicks: 50,        // a session shorter than 5 s of mission is a mis-start
  copyMin: 3,          // a shared prefix of this many orders is a copy (Continue or replay)
  maxSamePerTick: 4,   // the same order (all fields) more than this often in one tick: a host repeat, not a hand (a paused game gives many different orders one tick)
  gestureTicks: 3,     // orders within 0.3 s of the one before are one decision (a squad order, a double click, cancel + move)
  driveBoutGapTicks: 50, // drive samples on one agent more than 5 s apart start a new third-person stretch (WASD is held, tapped, let go)
  idleS: 15,           // a gap between decisions longer than this is an idle, not a hesitation
  walkCmS: 140, runCmS: 280, // rulebook op-speed-cm-s, op-run-speed-cm-s
  redirectM: 3,        // a move replaced by a move more than this far from its point and off its heading is a change of mind (else a nudge)
  redirectDeg: 30,     // ... off its heading by more than this
  detourS: 6,          // A, then B, then A again within this: a switch that did not stick
  lineTailS: 60,       // a line (a Watch or Listen on a target) is judged only when the session runs on this long after it
  quickRestartS: 120,  // the next session started within this of the last save: a restart
} as const;

/** Act lengths (s) when the log does not say (rulebook values; turn and call from the engine's own log lines). */
export const ACT_S: Record<string, number> = { watch: 20, listen: 42, turn: 50, loop: 62, reroute: 45, mislead: 44, enter: 38, takedown: 6, carry: 3, board: 1, call: 10, take: 38, draw: 1 };
/** Verbs that commit to a line on a target (the end of the approach a Watch or Listen began). */
const COMMIT = new Set(['turn', 'call', 'takedown', 'carry', 'loop', 'reroute', 'mislead', 'take', 'restrain', 'stash', 'enter', 'fire']);
const LEARN = new Set(['watch', 'listen']);
const OPS = ['scout', 'fixer', 'tech', 'enforcer'];

// ---------------------------------------------------------------- reading

export type O = { tick: number; op?: string; do: string; to?: string | number[]; target?: string; approach?: string; run?: boolean; release?: boolean; [k: string]: unknown };
export type Sess = {
  file: string; id: string; start: number; wall: number; until: number; ratio: number; seq: string; orders: O[]; engine: string; outcome: string; checkpoints: [number, string][];
  end?: string; mode: 'handover' | 'range'; copied: number; copiedFrom?: string;
};
const stamp = (f: string) => { const m = f.match(/(\d{4})(\d\d)(\d\d)-(\d\d)(\d\d)(\d\d)/); return m ? new Date(+m[1]!, +m[2]! - 1, +m[3]!, +m[4]!, +m[5]!, +m[6]!).getTime() / 1000 : 0; };
const nonDrive = (o: O[]) => o.filter(a => a.do !== 'drive');

export function readSessions(dirs: string[]): Sess[] {
  const all: Sess[] = [];
  for (const dir of dirs) {
    if (!existsSync(dir)) continue;
    for (const f of readdirSync(dir).filter(f => /^handover-.*\.script\.json$/.test(f))) {
      let d: any; try { d = JSON.parse(readFileSync(join(dir, f), 'utf8')); } catch { continue; }
      if (d?.schema !== 'suborned.sim.script/1') continue;
      const o = (d.orders ?? []) as O[], nd = nonDrive(o), until = Number(d.until ?? 0), start = stamp(f), wall = statSync(join(dir, f)).mtimeMs / 1000 - start;
      const endF = join(dir, f.replace(/\.json$/, '.end.txt'));
      all.push({
        file: join(dir, f), id: f.replace(/^handover-/, '').replace(/\.script\.json$/, ''), start, wall, until, ratio: until > 0 ? wall / (until / 10) : 0,
        seq: nd.map(a => `${a.op}:${a.do}:${a.target ?? ''}`).join('|'), orders: o, engine: String(d.expect?.engine ?? ''), outcome: String(d.expect?.outcome ?? 'none'),
        checkpoints: (d.expect?.checkpoints ?? []) as [number, string][], end: existsSync(endF) ? readFileSync(endF, 'utf8').split('\n')[0] : undefined,
        mode: nd.some(a => a.do === 'spawn') ? 'range' : 'handover', copied: 0,
      });
    }
  }
  return all.sort((a, b) => a.start - b.start || a.id.localeCompare(b.id));
}

const okey = (a: O) => `${a.tick}:${a.op}:${a.do}:${a.target ?? ''}:${JSON.stringify(a.to ?? '')}`;
export type Classified = { sky: Sess[]; dropped: Record<string, number> };
/** Sky's own sessions among everything saved (see the head of this file). */
export function skySessions(all: Sess[], r = RULES): Classified {
  const dropped: Record<string, number> = { 'fast (automation)': 0, 'repeated real-time sequence (proof)': 0, 'a copy of an earlier session': 0, 'too short or no orders': 0, 'a burst no hand makes': 0 };
  const rt = all.filter(s => {
    if (s.until < r.minTicks || nonDrive(s.orders).length === 0) { dropped['too short or no orders']!++; return false; }
    if (s.ratio < r.realTimeRatio) { dropped['fast (automation)']!++; return false; }
    const per = new Map<string, number>(); for (const o of nonDrive(s.orders)) { const k = JSON.stringify(o); per.set(k, (per.get(k) ?? 0) + 1); }
    if (Math.max(...per.values()) > r.maxSamePerTick) { dropped['a burst no hand makes']!++; return false; }
    return true;
  });
  const keys = new Map(all.map(s => [s, nonDrive(s.orders).map(okey)]));
  // a real-time proof plays one sequence again in every batch, each time a little differently timed; a copy (a Continue left at once) is the same orders exactly
  const seqN = new Map<string, Set<string>>(); for (const s of rt) seqN.set(s.seq, (seqN.get(s.seq) ?? new Set()).add(keys.get(s)!.join('|')));
  const sky: Sess[] = [];
  for (const s of rt) {
    if (seqN.get(s.seq)!.size > 1) { dropped['repeated real-time sequence (proof)']!++; continue; }
    const mine = keys.get(s)!; let best = 0, from = '';
    for (const p of all) {
      if (p.start >= s.start) break;
      const theirs = keys.get(p)!; let k = 0; while (k < mine.length && k < theirs.length && mine[k] === theirs[k]) k++;
      if (k > best) { best = k; from = p.id; }
    }
    if (best >= r.copyMin) { if (best >= mine.length) { dropped['a copy of an earlier session']!++; continue; } s.copied = best; s.copiedFrom = from; }
    sky.push(s);
  }
  return { sky, dropped };
}

// ---------------------------------------------------------------- decisions and bouts

/** One decision: the orders of one gesture (a squad order, a double click, cancel then move), the agents it touched. */
export type Decision = { tick: number; ops: string[]; orders: O[]; main: O };
export function decisions(orders: O[], r = RULES): Decision[] {
  const nd = nonDrive(orders).slice().sort((a, b) => a.tick - b.tick), out: Decision[] = [];
  for (const o of nd) {
    const last = out[out.length - 1], lastTick = last?.orders[last.orders.length - 1]!.tick;
    if (last && o.tick - lastTick! <= r.gestureTicks) last.orders.push(o); else out.push({ tick: o.tick, ops: [], orders: [o], main: o });
  }
  for (const d of out) { d.ops = [...new Set(d.orders.map(o => o.op ?? 'cell'))].sort(); d.main = d.orders.find(o => o.do !== 'cancel' && !(o.do === 'move' && o.run)) ?? d.orders[0]!; }
  return out;
}
/** Third-person bouts: runs of drive samples on one agent (WASD in the follow view). */
export type Bout = { op: string; from: number; to: number };
export function bouts(orders: O[], r = RULES): Bout[] {
  const out: Bout[] = [];
  for (const o of orders.filter(a => a.do === 'drive').sort((a, b) => a.tick - b.tick)) {
    const last = out[out.length - 1];
    if (last && last.op === o.op && o.tick - last.to <= r.driveBoutGapTicks) last.to = o.tick; else out.push({ op: o.op ?? 'cell', from: o.tick, to: o.tick });
  }
  return out;
}

// ---------------------------------------------------------------- statistics

export type Q = { n: number; p10: number; p25: number; p50: number; p75: number; p90: number; mean: number };
export function quant(xs: number[]): Q {
  const s = xs.filter(Number.isFinite).slice().sort((a, b) => a - b), n = s.length;
  const at = (q: number) => { if (!n) return 0; const i = (n - 1) * q, lo = Math.floor(i), hi = Math.ceil(i); return r2(s[lo]! + (s[hi]! - s[lo]!) * (i - lo)); };
  return { n, p10: at(0.1), p25: at(0.25), p50: at(0.5), p75: at(0.75), p90: at(0.9), mean: n ? r2(s.reduce((a, b) => a + b, 0) / n) : 0 };
}
const r2 = (v: number) => Math.round(v * 100) / 100, r3 = (v: number) => Math.round(v * 1000) / 1000;
const share = (k: number, n: number) => ({ k, n, p: n ? r3(k / n) : 0 });
const dist = (a: number[], b: number[]) => Math.hypot(a[0]! - b[0]!, a[1]! - b[1]!);
const xy = (o: O): number[] | undefined => Array.isArray(o.to) && o.to.length >= 2 ? [Number(o.to[0]), Number(o.to[1])] : undefined;

// ---------------------------------------------------------------- the engine's word (faithful replays)

export type Verdict = { faithful: boolean; orders: { order: O; ok: boolean; code?: string; why?: string }[]; log: { tick: number; code: string; text: string }[] };
export function loadVerdicts(dir = PLAYS_DIR): Map<string, Verdict> {
  const out = new Map<string, Verdict>(); if (!existsSync(dir)) return out;
  for (const f of readdirSync(dir).filter(f => f.endsWith('.json'))) {
    try { const d = JSON.parse(readFileSync(join(dir, f), 'utf8')); out.set(d.id, { faithful: !!d.all, orders: d.play.orders, log: d.play.log }); } catch { /* skip */ }
  }
  return out;
}
/** A refusal's family, in plain words. */
export function refusalFamily(code: string, why = ''): string {
  const c = `${code} ${why}`;
  if (/can't be walked|unwalkable|no-path|not-walkable/.test(c)) return 'ground that cannot be walked';
  if (/agent-busy|is busy until/.test(c)) return 'agent busy';
  if (/agent-down|downed|carried|enforcer-inside|in-car/.test(c)) return 'agent down, carried or away';
  if (/needs-|stand within|too-far|out-of-reach|reach|knowledge/.test(c)) return 'not ready (knowledge or reach)';
  if (/^fire|fire-|no-target|out-of-range|already-drawn|panic/.test(c)) return 'fight order not possible';
  return 'other';
}

// ---------------------------------------------------------------- the measures

export type Measures = ReturnType<typeof measure>;
export function measure(all: Sess[], cls: Classified, verdicts: Map<string, Verdict> = new Map(), r = RULES) {
  const sky = cls.sky, hand = sky.filter(s => s.mode === 'handover'), range = sky.filter(s => s.mode === 'range');
  const gaps: number[] = [], gapsRange: number[] = [], first: number[] = [];
  let switches = 0, switchN = 0, squad = 0, decN = 0, detours = 0, detourN = 0, wrongAgent = 0;
  const cancelDelays: number[] = [], actReplaceDelays: number[] = [], moveRedirectDelays: number[] = [];
  let acts = 0, actsReplaced = 0, actsCancelled = 0, moves = 0, movesJudged = 0, movesRedirected = 0, movesStopped = 0, movesNudged = 0, movesToAct = 0, cancels = 0;
  let faithfulMoves = 0, unwalkable = 0, reclickN = 0, reclicked = 0;
  let actDecisions = 0, repeatSame = 0, otherWay = 0, wrongVerb = 0, retryN = 0, retried = 0;
  let lines = 0, abandoned = 0; const abandonedOn: Record<string, number> = {};
  let runMoves = 0;
  const boutS: number[] = []; let tpSessions = 0, driveTicks = 0, missionTicks = 0, boutsN = 0;
  const ends: Record<string, number> = {}; const quitAt: number[] = [], endAt: number[] = []; let restarts = 0;
  const refusals: Record<string, number> = {}; let judgedOrders = 0, refused = 0, judgedDecisions = 0, busyOrders = 0;
  const ratios: number[] = [];

  for (const s of [...hand, ...range]) {
    const own = s.copied ? (() => { const nd = nonDrive(s.orders); const cut = nd[s.copied - 1]!.tick; return s.orders.filter(o => o.tick > cut || (o.do === 'drive' && o.tick > cut)); })() : s.orders;
    const D = decisions(own, r), B = bouts(own, r);
    // gaps between inputs: a decision, or the start of a third-person bout
    const inputs = [...D.map(d => d.tick), ...B.map(b => b.from)].sort((a, b) => a - b);
    for (let i = 1; i < inputs.length; i++) (s.mode === 'range' ? gapsRange : gaps).push((inputs[i]! - inputs[i - 1]!) / 10);
    if (s.mode === 'range') continue;
    ratios.push(s.ratio);
    if (!s.copied && inputs.length) first.push(inputs[0]! / 10);

    // switching
    for (let i = 0; i < D.length; i++) {
      decN++; if (D[i]!.ops.length > 1) squad++;
      if (i > 0) { switchN++; if (D[i]!.ops.join() !== D[i - 1]!.ops.join()) switches++; }
      if (i > 0 && i + 1 < D.length && D[i - 1]!.ops.length === 1 && D[i]!.ops.length === 1 && D[i + 1]!.ops.length === 1 && D[i]!.ops[0] !== D[i - 1]!.ops[0]) {
        detourN++; if (D[i + 1]!.ops[0] === D[i - 1]!.ops[0] && (D[i + 1]!.tick - D[i - 1]!.tick) / 10 <= r.detourS) detours++;
      }
      // the wrong agent: an order to A, undone within 5 s (A's next input), and the same order given to B within those 5 s
      const d = D[i]!; if (d.ops.length !== 1 || d.main.do === 'cancel') continue;
      const a = d.ops[0]!, o = d.main, soon = D.slice(i + 1).filter(x => (x.tick - d.tick) / 10 <= 5);
      const undone = soon.some(x => x.ops.includes(a)), same = (x: O) => x.do === o.do && (o.target ? x.target === o.target : !!(xy(x) && xy(o) && dist(xy(x)!, xy(o)!) / 100 <= RULES.redirectM));
      if (undone && soon.some(x => x.orders.some(y => y.op !== a && same(y)))) wrongAgent++;
    }

    // per agent: cancels, replaced acts, redirected moves (a move's landing from where the agent stood: its last point or its drive)
    const v = verdicts.get(s.id), faithful = !!v?.faithful;
    const okOf = new Map<O, boolean>(); if (faithful) v!.orders.forEach((o, i) => { const mine = s.orders[i]; if (mine) okOf.set(mine, o.ok); });
    const pos = new Map<string, number[]>();
    for (const op of OPS) {
      const seq: { t: number; o?: O; drive?: Bout }[] = [];
      for (const d of D) for (const o of d.orders) if (o.op === op) seq.push({ t: o.tick, o });
      for (const b of B) if (b.op === op) seq.push({ t: b.from, drive: b });
      seq.sort((a, b) => a.t - b.t);
      for (let i = 0; i < seq.length; i++) {
        const cur = seq[i]!;
        if (cur.drive) { const last = own.filter(o => o.do === 'drive' && o.op === op && o.tick <= cur.drive!.to).pop(); const p = last && xy(last); if (p) pos.set(op, p); continue; }
        const o = cur.o!; if (okOf.get(o) === false) continue; // refused: it never started
        // the next input on this agent that is not the same gesture (the double click's run, the cancel's own move)
        let j = i + 1; while (j < seq.length && seq[j]!.o && seq[j]!.t - o.tick <= r.gestureTicks && (seq[j]!.o!.do === o.do || seq[j]!.o!.do === 'cancel' || o.do === 'cancel')) j++;
        const nx = seq[j]; const dt = nx ? (nx.t - o.tick) / 10 : Infinity;
        if (o.do === 'cancel') {
          cancels++;
          const prev = seq.slice(0, i).reverse().find(x => x.o && x.o.do !== 'cancel')?.o;
          if (prev) cancelDelays.push((o.tick - prev.tick) / 10);
          continue;
        }
        if (o.do === 'move') {
          moves++; if (o.run) runMoves++;
          const to = xy(o), from = pos.get(op);
          if (to && from) {
            movesJudged++;
            const len = dist(from, to), land = len / (o.run ? r.runCmS : r.walkCmS);
            if (nx && dt < land) {
              const no = nx.o, nt = no && xy(no);
              const here = [from[0]! + (to[0]! - from[0]!) * Math.min(1, dt / land), from[1]! + (to[1]! - from[1]!) * Math.min(1, dt / land)];
              const turn = nt ? Math.abs(((Math.atan2(nt[1]! - here[1]!, nt[0]! - here[0]!) - Math.atan2(to[1]! - from[1]!, to[0]! - from[0]!)) * 180 / Math.PI + 540) % 360 - 180) : 180;
              if (no && no.do === 'move' && nt && (dist(nt, to) / 100 <= r.redirectM || turn <= r.redirectDeg)) movesNudged++;
              else if (no && no.do === 'cancel') { movesStopped++; moveRedirectDelays.push(r2(dt)); }
              else if (no && no.do !== 'move') movesToAct++;
              else { movesRedirected++; moveRedirectDelays.push(r2(dt)); }
              pos.set(op, [from[0]! + (to[0]! - from[0]!) * Math.min(1, dt / land), from[1]! + (to[1]! - from[1]!) * Math.min(1, dt / land)]);
            } else pos.set(op, to);
          } else if (to) pos.set(op, to);
          continue;
        }
        const len = ACT_S[o.do];
        if (len !== undefined && o.do !== 'draw') {
          acts++;
          // the act's start: the engine's own log line in a faithful replay (a walk-in delays it), else the order's tick
          const started = faithful ? v!.log.find(l => l.tick >= o.tick && l.code === `order.${o.do}` && l.text.startsWith(op)) : undefined;
          const secs = started ? Number(started.text.match(/\((\d+(?:\.\d+)?) s\)/)?.[1] ?? len) : len, t0 = started ? started.tick : o.tick;
          if (nx && nx.t < t0 + secs * 10) { if (nx.o?.do === 'cancel') actsCancelled++; else actsReplaced++; actReplaceDelays.push(r2(dt)); busyOrders++; }
        }
      }
    }

    // re-orders, a different way on the same target, the wrong verb put right, a retry after a refusal
    const seenSame = new Set<string>(), seenWay = new Map<string, Set<string>>();
    for (let i = 0; i < D.length; i++) for (const o of D[i]!.orders) {
      if (!o.target || o.do === 'move' || o.do === 'cancel' || o.do === 'fire' || o.do === 'follow') continue;
      actDecisions++;
      const same = `${o.op}|${o.do}|${o.target}`, way = `${o.do}${o.approach ? ':' + o.approach : ''}`, ways = seenWay.get(o.target) ?? new Set();
      if (seenSame.has(same)) repeatSame++;
      else if (ways.size && !ways.has(way) && [...ways].some(w => w.split(':')[0] === o.do)) otherWay++; // another approach of the same verb (turn ask, then turn pay)
      seenSame.add(same); ways.add(way); seenWay.set(o.target, ways);
      // the wrong verb put right: within 5 s the same agent gives another verb on the same target, the first one undone
      const nx = D.slice(i + 1).find(d => d.orders.some(x => x.op === o.op));
      const nxo = nx?.orders.find(x => x.op === o.op && x.do !== 'cancel' && x.target === o.target);
      if (nx && nxo && nxo.do !== o.do && !LEARN.has(o.do) && (nx.tick - o.tick) / 10 <= 5) wrongVerb++;
    }
    if (faithful) {
      for (let i = 0; i < v!.orders.length; i++) {
        const x = v!.orders[i]!; if (x.order.do === 'drive') continue;
        judgedOrders++;
        if (x.order.do === 'move') faithfulMoves++;
        if (!x.ok && x.code !== 'mission-over') {
          refused++; const fam = refusalFamily(x.code ?? '', x.why ?? ''); refusals[fam] = (refusals[fam] ?? 0) + 1;
          if (fam === 'agent busy') busyOrders++;
          if (fam === 'ground that cannot be walked') unwalkable++;
          const later = v!.orders.slice(i + 1).filter(y => y.order.op === x.order.op && y.order.tick > x.order.tick + r.gestureTicks);
          if (x.order.target) { retryN++; if (later.some(y => y.order.do === x.order.do && y.order.target === x.order.target && y.order.tick - x.order.tick <= 300)) retried++; } // the same act on the same target within 30 s
          else if (x.order.do === 'move') { reclickN++; if (later.some(y => y.order.do === 'move' && y.order.tick - x.order.tick <= 30)) reclicked++; } // another ground click within 3 s
        }
      }
      judgedDecisions += D.length;
    }

    // lines: a Watch or Listen on a target, then a commit on it, before the end
    const learned = new Map<string, number>();
    for (const o of nonDrive(own)) if (LEARN.has(o.do) && o.target && !learned.has(o.target)) learned.set(o.target, o.tick);
    for (const [t, at] of learned) {
      if ((s.until - at) / 10 < r.lineTailS) continue;
      lines++;
      const done = nonDrive(own).some(o => o.tick > at && COMMIT.has(o.do) && (o.target === t || (o.do === 'call' && t === 'guard') || (o.do === 'enter' && t.startsWith('camera')) || (t === 'guard' && o.do === 'takedown')));
      if (!done) { abandoned++; const k = t.replace(/[-:].*$/, ''); abandonedOn[k] = (abandonedOn[k] ?? 0) + 1; }
    }

    // third person
    if (B.length) tpSessions++;
    boutsN += B.length; for (const b of B) { boutS.push((b.to - b.from + 1) / 10); driveTicks += b.to - b.from + 1; }
    missionTicks += s.until;

    // the end
    const ended = s.end ? (s.end.startsWith('Success') ? 'success' : `failure (${s.end.match(/\(([^)]+)\)/)?.[1] ?? '?'})`) : s.outcome !== 'none' ? s.outcome : 'quit (no ending)';
    ends[ended] = (ends[ended] ?? 0) + 1;
    (ended.startsWith('quit') ? quitAt : endAt).push(s.until / 10);
    const next = hand.find(x => x.start > s.start); if (next && next.start - (s.start + s.wall) <= r.quickRestartS) restarts++;
  }

  const gapAll = quant(gaps), gapShort = quant(gaps.filter(g => g <= r.idleS)), idle = gaps.filter(g => g > r.idleS);
  return {
    sessions: {
      saved: all.length, sky: sky.length, handover: hand.length, fightLab: range.length, dropped: cls.dropped,
      faithfulReplays: hand.filter(s => verdicts.get(s.id)?.faithful).length,
      engines: [...new Set(hand.map(s => s.engine.replace('suborned.sim/', '')))].join(', '),
      dates: hand.length ? `${hand[0]!.id.slice(0, 8)}..${hand[hand.length - 1]!.id.slice(0, 8)}` : '',
      missionMinutes: r2(missionTicks / 600), decisions: decN,
      wallToMission: quant(ratios),
    },
    gaps: { all: gapAll, hesitation: gapShort, idle: { ...share(idle.length, gaps.length), s: quant(idle) }, firstOrderS: quant(first), fightLab: quant(gapsRange) },
    switching: { agentChanges: share(switches, switchN), squadOrders: share(squad, decN), detours: share(detours, detourN), wrongAgent: share(wrongAgent, decN) },
    cancel: {
      explicitCancels: share(cancels, decN), cancelDelayS: quant(cancelDelays),
      actsReplacedBeforeLanding: share(actsReplaced, acts), actsCancelledBeforeLanding: share(actsCancelled, acts), actReplaceDelayS: quant(actReplaceDelays),
      movesRedirected: share(movesRedirected, movesJudged), movesStopped: share(movesStopped, movesJudged), moveRedirectDelayS: quant(moveRedirectDelays), movesNudged: share(movesNudged, movesJudged), movesCutByAnAct: share(movesToAct, movesJudged),
      moves, runMoves: share(runMoves, moves),
    },
    reorders: { sameAgainSameTarget: share(repeatSame, actDecisions), otherApproachSameTarget: share(otherWay, actDecisions), wrongVerbPutRight: share(wrongVerb, actDecisions), retryAfterRefusal: share(retried, retryN), reclickAfterUnwalkable: share(reclicked, reclickN) },
    abandon: { lines: share(abandoned, lines), on: abandonedOn, perDecision: share(abandoned, decN) },
    thirdPerson: { sessions: share(tpSessions, hand.length), bouts: boutsN, boutsPerDecision: share(boutsN, decN), boutS: quant(boutS), shareOfMission: r3(missionTicks ? driveTicks / missionTicks : 0) },
    mistakes: { judgedOrders, unwalkablePerMove: share(unwalkable, faithfulMoves), refused: share(refused, judgedOrders), byFamily: refusals, perDecision: share(refused, judgedDecisions), ordersOnBusyAgent: share(busyOrders, judgedOrders + acts) },
    ends: { by: ends, quit: share(ends['quit (no ending)'] ?? 0, hand.length), quitAtS: quant(quitAt), endedAtS: quant(endAt), restartsWithin2Min: share(restarts, hand.length) },
  };
}

// ---------------------------------------------------------------- the profile (Latch's reader: tools/qa/human.ts readProfile)

/** What the sessions cannot show, estimated: the value and the reasoning. */
export const ESTIMATES = {
  reorderProb: { v: 0.15, why: 'estimated: a step taken out of the plan\'s order needs the plan, and Sky\'s sessions have none. The bounds: 0.29 of his decisions go to another agent than the one before (an upper bound: most of it is the plan\'s own turn-taking), and 0.007 are switches that did not stick (a floor). Halfway, rounded down: 0.15, Latch\'s starting value.' },
  misclickProb: { v: 0.06, why: 'estimated: misses that open nothing are not recorded. Two anchors: (1) moves Sky clicked onto ground that cannot be walked (a roof, a wall, a solid) are near-misses the engine logs, measured below; (2) pointing tasks are run at about 4% errors (Soukoreff and MacKenzie 2004, the ISO 9241-9 convention), and the Handover\'s people are moving targets 40-60 px tall, which misses more often than still ones. Ground clicks miss more (8.7% of Sky\'s moves land on ground that cannot be walked, half re-clicked within 3 s), people less (about 4%); about half a route\'s steps are moves. So 6% a ring or move step.' },
  ringOpenCloseProb: { v: 0.12, why: 'estimated: ring opens are not recorded. Every order shows its price before you commit (GDD §01), so looking and backing out is designed play. In the faithful replays Sky gave orders the game then refused for knowledge or reach (needs-routine, stand within) on about 1 in 30 ring orders; a shut wedge he saw first was one he closed instead, which would be several times that. Novices browse radial menus before they commit (Kurtenbach and Buxton 1993). So about 1 ring in 8 is opened and closed without a choice.' },
  cameraTurnProb: { v: 0.05, why: 'estimated: Q/E turns are not recorded (the camera is not in the engine). The Records Office front is played at yaw 210 and the run opens at 30 (GDD §10), so a run needs at least two quarter steps; Sky gives a median of about 50 decisions a run. A 5% chance a step gives 2-3 turns a run.' },
  wrongVerbProb: { v: 0.03, why: 'partly measured: a different verb on the same target by the same agent within 5 s of the first (the first undone) is the trace a wrong wedge leaves; see reorders.wrongVerbPutRight. The wrong wedge that was noticed before release leaves no trace, and 8-way radial menus run at about 2-5% selection errors (Kurtenbach and Buxton 1993). So 3%.' },
} as const;

export function toProfile(m: Measures, build = '') {
  const n = `${m.sessions.handover} Handover sessions of Sky's (${m.sessions.dates}, engines ${m.sessions.engines}; ${m.sessions.decisions} decisions over ${m.sessions.missionMinutes} min of mission)`;
  const fN = `${m.sessions.faithfulReplays} of them replayed exactly on their own engine`;
  const c = m.cancel, g = m.gaps;
  // one step of the plan is one decision: the per-step chances are per decision
  const undone = c.actsReplacedBeforeLanding.k + c.actsCancelledBeforeLanding.k + c.movesRedirected.k + c.movesStopped.k, judged = c.actsReplacedBeforeLanding.n + c.movesRedirected.n;
  const cancelP = r3(undone / Math.max(1, judged));
  const delays = [...[c.actReplaceDelayS], ...[c.cancelDelayS], ...[c.moveRedirectDelayS]].filter(q => q.n);
  const pool = (k: 'p50' | 'p90') => r2(delays.reduce((a, q) => a + q[k] * q.n, 0) / Math.max(1, delays.reduce((a, q) => a + q.n, 0)));
  const d = (q: Q, fb: { p50: number; p90: number }) => q.n >= 5 && q.p50 > 0 ? { p50: q.p50, p90: Math.max(q.p90, q.p50) } : fb; // fewer than 5 samples: Latch's starting value
  const values = {
    cancelProb: cancelP,
    cancelDelayS: pool('p50') > 0 ? { p50: pool('p50'), p90: Math.max(pool('p90'), pool('p50')) } : { p50: 1.2, p90: 4 },
    reorderProb: ESTIMATES.reorderProb.v,
    repeatProb: r3(m.reorders.sameAgainSameTarget.p + m.reorders.otherApproachSameTarget.p),
    switchProb: r3((m.switching.detours.k + m.switching.wrongAgent.k) / Math.max(1, m.sessions.decisions)),
    gapS: { ...d(g.hesitation, { p50: 1.5, p90: 5 }), p10: g.hesitation.p10 },
    idleProb: g.idle.p,
    idleS: d(g.idle.s, { p50: 5, p90: 15 }),
    abandonProb: m.abandon.perDecision.p,
    misclickProb: ESTIMATES.misclickProb.v,
    ringOpenCloseProb: ESTIMATES.ringOpenCloseProb.v,
    cameraTurnProb: ESTIMATES.cameraTurnProb.v,
    wrongVerbProb: Math.max(ESTIMATES.wrongVerbProb.v, m.reorders.wrongVerbPutRight.p),
    thirdPersonProb: m.thirdPerson.boutsPerDecision.p,
    thirdPersonS: d(m.thirdPerson.boutS, { p50: 3, p90: 8 }),
  };
  const source: Record<keyof typeof values, string> = {
    cancelProb: `measured from ${n}: per order, undone before it landed: an act given another order (${c.actsReplacedBeforeLanding.k}) or cancelled (${c.actsCancelledBeforeLanding.k}) before its time was up, of ${c.actsReplacedBeforeLanding.n} acts (${r3((c.actsReplacedBeforeLanding.k + c.actsCancelledBeforeLanding.k) / Math.max(1, c.actsReplacedBeforeLanding.n))}); a move sent more than ${RULES.redirectM} m off and ${RULES.redirectDeg} degrees off its heading (${c.movesRedirected.k}) or stopped (${c.movesStopped.k}) before it arrived, of ${c.movesRedirected.n} judged moves (${r3((c.movesRedirected.k + c.movesStopped.k) / Math.max(1, c.movesRedirected.n))}). Steering along the way (${c.movesNudged.k}) and a walk cut short by the act it led to (${c.movesCutByAnAct.k}) are not counted. Arrival is a straight walk from the last known point at 1.4 m/s (2.8 running), so the move share is a floor`,
    cancelDelayS: `measured: time from the order to its cancel or replacement, pooled over acts (median ${c.actReplaceDelayS.p50} s), explicit cancels (${c.cancelDelayS.p50} s) and redirected moves (${c.moveRedirectDelayS.p50} s)`,
    reorderProb: `${ESTIMATES.reorderProb.why} Measured bound: the agent changes between Sky's decisions ${m.switching.agentChanges.p}`,
    repeatProb: `measured from ${n}: per act step on a target (moves excluded), the same agent giving the same act on the same target again (${m.reorders.sameAgainSameTarget.k} of ${m.reorders.sameAgainSameTarget.n}) or another approach of the same verb on it (${m.reorders.otherApproachSameTarget.k}): "the repeat Watch". ${Math.round(m.reorders.retryAfterRefusal.p * 100)}% of refused acts on a target were given again within 30 s`,
    switchProb: `measured from ${n}, a floor: per decision, a switch that did not stick: an order given to the wrong agent, undone within 5 s and given to another (${m.switching.wrongAgent.k}), or a decision on another agent and straight back (A, B, A within ${RULES.detourS} s; ${m.switching.detours.k} of ${m.switching.detours.n} such triples). Selecting an agent (F1-F4, a portrait) without ordering is not recorded, so glances that gave no order are missing. The plain agent change between decisions is ${m.switching.agentChanges.p}, mostly the plan's own turn-taking; squad orders are ${m.switching.squadOrders.p} of decisions`,
    gapS: `measured from ${n}: time between inputs (a decision, or taking an agent in third person), gaps up to ${RULES.idleS} s, in mission time (pauses excluded); all gaps: median ${g.all.p50} s, p90 ${g.all.p90} s`,
    idleProb: `measured: share of gaps between inputs over ${RULES.idleS} s (${g.idle.k} of ${g.idle.n})`,
    idleS: `measured: those idle gaps (n ${g.idle.s.n})`,
    abandonProb: `measured from ${n}: per decision, a line given up: a Watch or Listen on a target with no turn, call, loop, reroute, mislead, takedown or fire on it before the run ended (${m.abandon.lines.k} of ${m.abandon.lines.n} lines, ${m.abandon.lines.p} a line; only lines with ${RULES.lineTailS} s of run after them)`,
    misclickProb: `${ESTIMATES.misclickProb.why} Measured anchor: ${m.mistakes.byFamily['ground that cannot be walked'] ?? 0} unwalkable moves among ${m.mistakes.judgedOrders} orders in the ${fN.replace(/^\d+ of them /, 'sessions ')}`,
    ringOpenCloseProb: ESTIMATES.ringOpenCloseProb.why,
    cameraTurnProb: ESTIMATES.cameraTurnProb.why,
    wrongVerbProb: `${ESTIMATES.wrongVerbProb.why} Measured trace: ${m.reorders.wrongVerbPutRight.k} of ${m.reorders.wrongVerbPutRight.n} acts`,
    thirdPersonProb: `measured from ${n}: third-person bouts (drive samples) per decision, ${m.thirdPerson.bouts} bouts; ${m.thirdPerson.sessions.k} of ${m.thirdPerson.sessions.n} sessions used it, ${Math.round(m.thirdPerson.shareOfMission * 100)}% of mission time`,
    thirdPersonS: `measured: length of a third-person bout (n ${m.thirdPerson.boutS.n})`,
  };
  return {
    schema: 'suborned.bot.human-profile/1',
    note: 'Brief 060 tier 2, Suture: how Sky plays, measured from his saved sessions (tools/qa/human-measure.ts). The human bot policy (Latch, tools/qa/human.ts readProfile) reads `values`: each probability is a chance per plan step (one step = one decision), each time is seconds as {p50, p90} (lognormal). Every draw comes from a stream seeded by the row seed and the value\'s name (human.ts stream), so a row replays exactly. `source` says, per value, measured from what or estimated and why; `measured` carries the calibration targets the checks compare a human row against.',
    by: `Suture ${new Date().toISOString().slice(0, 10)}${build ? `; build ${build}` : ''}`,
    sessions: { sky: m.sessions.handover, fightLab: m.sessions.fightLab, faithfulReplays: m.sessions.faithfulReplays, saved: m.sessions.saved, dates: m.sessions.dates },
    values, source,
    measured: {
      refusedPerOrder: m.mistakes.refused.p, refusedByFamily: m.mistakes.byFamily, retryAfterRefusal: m.reorders.retryAfterRefusal.p,
      runShareOfMoves: m.cancel.runMoves.p, squadShareOfDecisions: m.switching.squadOrders.p, agentChangeBetweenDecisions: m.switching.agentChanges.p,
      firstOrderS: { p50: g.firstOrderS.p50, p90: g.firstOrderS.p90 }, quitWithoutEnding: m.ends.quit.p, quitAtS: { p50: m.ends.quitAtS.p50, p90: m.ends.quitAtS.p90 },
      wallToMission: { p50: m.sessions.wallToMission.p50, p90: m.sessions.wallToMission.p90 },
    },
  };
}

// ---------------------------------------------------------------- the replays (refusal codes from the engine Sky played)

/** Committed engines by version, newest first: git log over the engine source, each commit's version string. */
export function engineCommits(): { c: string; at: string; v: string }[] {
  const g = (...a: string[]) => Bun.spawnSync(['git', ...a], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' }).stdout.toString().trim();
  return g('log', '--format=%h %ad', '--date=format:%Y%m%d-%H%M%S', '--', 'sim/Suborned.Sim').split('\n').filter(Boolean).map(l => {
    const [c, at] = l.split(' '); const v = g('grep', '-h', '-o', '"suborned\\.sim/[0-9.]*"', c!, '--', 'sim/Suborned.Sim/*.cs').split('\n')[0]?.replace(/"/g, '') ?? '';
    return { c: c!, at: at!, v };
  }).filter(x => x.v);
}

export async function replaySessions(sky: Sess[], say = (m: string) => console.error(m)): Promise<void> {
  mkdirSync(PLAYS_DIR, { recursive: true });
  const commits = engineCommits();
  for (const s of sky.filter(x => x.mode === 'handover')) {
    const out = join(PLAYS_DIR, `${s.id}.json`); if (existsSync(out)) continue;
    const same = commits.filter(c => c.v === s.engine), before = same.filter(c => c.at <= s.id), firstC = before[0] ?? same[same.length - 1];
    if (!firstC) { say(`human: ${s.id}: engine ${s.engine} was never committed; no verdicts`); continue; }
    const raw = JSON.parse(readFileSync(s.file, 'utf8')), script = { schema: raw.schema, mission: raw.mission, start: raw.start, orders: raw.orders, until: raw.until };
    let best: any, k = -1;
    for (const c of [firstC, ...same.filter(x => x !== firstC)]) {
      let p: any; try { p = await play(script, `human-${s.id}`, engineAt(c.c), { replay: false }); } catch { continue; }
      let j = 0; while (j < s.checkpoints.length && j < p.checkpoints.length && s.checkpoints[j]![0] === p.checkpoints[j].tick && s.checkpoints[j]![1] === p.checkpoints[j].hash) j++;
      if (j > k) { k = j; best = { id: s.id, commit: c.c, matched: j, of: s.checkpoints.length, all: j === s.checkpoints.length && j > 0, play: { orders: p.orders, log: p.log, outcome: p.outcome, fail: p.fail } }; }
      if (k === s.checkpoints.length) break;
    }
    if (best) { writeFileSync(out, JSON.stringify(best)); say(`human: ${s.id} on ${best.commit}: ${best.matched}/${best.of} checkpoints`); }
  }
}

if (import.meta.main) {
  const a = process.argv.slice(2), val = (k: string) => { const i = a.indexOf(k); return i >= 0 ? a[i + 1] : undefined; };
  const dirs = val('--dirs')?.split(',') ?? [SESSIONS_DIR, EDITOR_SESSIONS_DIR];
  const all = readSessions(dirs), cls = skySessions(all);
  if (a.includes('--replay')) await replaySessions(cls.sky);
  const m = measure(all, cls, loadVerdicts());
  const build = (() => { try { return String(JSON.parse(readFileSync(join(ROOT, 'out/build/play/build.json'), 'utf8')).version); } catch { return ''; } })();
  const prof = toProfile(m, build);
  console.log(JSON.stringify({ measures: m, values: prof.values }, null, 1));
  if (a.includes('--write')) {
    writeFileSync(PROFILE_OUT, JSON.stringify(prof, null, 1) + '\n');
    mkdirSync(join(MEASURES_OUT, '..'), { recursive: true });
    writeFileSync(MEASURES_OUT, JSON.stringify({ schema: 'suborned.bot.human-measures/1', by: prof.by, rules: RULES, sessions: cls.sky.map(s => ({ id: s.id, mode: s.mode, engine: s.engine, until: s.until, wallToMission: r2(s.ratio), copied: s.copied || undefined })), measures: m }, null, 1) + '\n');
    console.error(`human: wrote ${PROFILE_OUT} and ${MEASURES_OUT}`);
  }
  process.exit(0);
}
