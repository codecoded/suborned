// Brief 060 tier 2 (Suture, 2026-10-25): the checks the indecisive `human` bot exists to feed. Each reads the engine's own play (log, orders with
// their refusals, ledger), so it runs on every seed of tier 1 (Sky's sessions, the routes) and on a bot row's session replayed on the build engine.
// On a human row it also reads what the bot meant (BotReportRow.human.events, BotRun.Human): a stop the bot meant (its cancel, re-order,
// abandon, a switch to another agent, a misclick, a wrong wedge) is the player's doing; one it did not mean is the game's.
//
//   Q-CANCEL      a new order quietly stopped what an agent was doing (Sky 2026-10-22: "cancelling a task I had ordered"; Sky's #14: third person
//                 stopping actions). Three ways: the host cancels a running act to give the new order (a `cancel` and another order to the same
//                 agent within 0.3 s); a new order ends the walk to an act (order.walk-in-ends: the act never happens); a third-person drive
//                 stops an act or the loop (cell.drive.onBusy).
//   Q-LOOP-LEFT   the Tech left the camera loop because he was given a move, a drive or another act (brief 058 §5: "the Tech walks off the cabinet
//                 and the loop ends at once with no warning"). A route that ends its loop at the Enter means to.
//   Q-INTENT      on a human row: the order the game gave is not the one the bot meant (Sky's #3, next-batch 31: "I've asked tec to move but he's
//                 dragging the downed scout"), from the row's `given` events against the session's orders at that tick.
//   Q-PILE        orders piled up on a busy agent: three or more orders to one agent within 10 s that the engine refused as busy, or that each
//                 stopped the act before them. The HUD did not make it plain the agent was busy.
//   Q-STALE-RING  an act ordered on someone who had already gone down, been carried off or gone inside: the open ring kept offering it (the target
//                 side of Q-GIVER). Within 0.1 s of the change it is a race (info).
//   Q-LEAK        state left over from a plan the player dropped: a cancelled act that still lands later (S1), or still costs Trail or Alert (S2),
//                 with no new order of it in between.
// Mutants are exempt from Q-CANCEL, Q-LOOP-LEFT and Q-PILE (a mutation makes them on purpose); Q-STALE-RING needs a ring, so seeds and bot rows
// only; Q-LEAK holds for every play.
import { bySky, type Finding, type Item, type Play, type Severity } from '../types';
import { clock } from './warn';

export const HUMAN_CHECKS = ['Q-CANCEL', 'Q-LOOP-LEFT', 'Q-INTENT', 'Q-PILE', 'Q-STALE-RING', 'Q-LEAK'] as const;
export const HUMAN_RULES = { gestureTicks: 3, meantS: 5, pileN: 3, pileS: 10, staleS: 10, raceTicks: 1, loopCauseTicks: 30 } as const;

/** One indecision a human row acted out (bot.ts HumanEvent; the kinds BotRun.Human writes). `given` is a step's input, with the act it meant. */
export type HumanEventLike = { tick: number; kind: string; op: string; act: string; target: string; step?: number; route?: string; detail?: string };
/** The kinds of event that mean the player himself stopped or changed an order. */
export const MEANT = new Set(['cancel', 'reorder', 'abandon', 'wander', 'switch', 'misclick', 'wrong', 'wrong-wedge', 'peek', 'third-person']);
const kindOf = (k: string) => k.toLowerCase().replace(/_/g, '-');

type Src = { seed: 'route' | 'session' | 'bot'; mutated: boolean; events?: HumanEventLike[]; /** a session Sky played himself (seeds.ts `by`) */ sky?: boolean };
const OPS = new Set(['scout', 'fixer', 'tech', 'enforcer']);
const ACT = new Set(['watch', 'listen', 'turn', 'loop', 'reroute', 'mislead', 'call', 'enter', 'takedown', 'take', 'carry', 'restrain', 'stash', 'board']);

/** What a cancel line says: `cancel.<verb>` "<op> stops: <verb> <target> (...)", or the loop's end. */
function stopped(l: { code: string; text: string }): { op: string; verb: string; target: string } | undefined {
  if (l.code === 'loop.ends') return { op: 'tech', verb: 'loop', target: '' };
  const m = l.code.match(/^cancel\.([a-z-]+)$/); if (!m || !ACT.has(m[1]!)) return undefined;
  const t = l.text.match(/^(scout|fixer|tech|enforcer) stops: ([a-z]+) ?([^ (]*)/);
  return t ? { op: t[1]!, verb: t[2]!, target: t[3] ?? '' } : { op: '', verb: m[1]!, target: '' };
}

function meantBy(src: Src, op: string, tick: number): HumanEventLike | undefined {
  return src.events?.filter(e => MEANT.has(kindOf(e.kind)) && (e.op === op || kindOf(e.kind) === 'switch') && e.tick <= tick + HUMAN_RULES.gestureTicks && tick - e.tick <= HUMAN_RULES.meantS * 10).pop();
}
const sevFor = (src: Src, meant: HumanEventLike | undefined, seedSev: Severity): Severity => src.seed === 'bot' ? (meant ? 'info' : 'S2') : seedSev;
const who = (src: Src) => src.seed === 'route' ? 'a route step' : src.seed === 'bot' ? (src.events ? 'the human bot' : 'the bot') : src.sky ? 'Sky' : 'a saved proof session';

export function qCancel(p: Play, src: Src): Finding[] {
  if (p.error || src.mutated) return [];
  const out: Finding[] = [], orders = p.orders.map(o => o.order);
  for (const l of p.log) {
    // a new order ends the walk to an act
    const w = l.code === 'order.walk-in-ends' && l.text.match(/^(scout|fixer|tech|enforcer): the walk to ([^ ]+) ends \(([a-z]+)\)/);
    if (w) {
      const m = meantBy(src, w[1]!, l.tick);
      out.push({ check: 'Q-CANCEL', severity: sevFor(src, m, 'S3'), key: `Q-CANCEL:walk-in:${m ? 'meant' : 'unmeant'}`, owner: 'Latch (the pill says "Cancels ..." before the new order lands)',
        title: m ? 'The player\'s new order ended a walk to an act (meant)' : 'A new order quietly ended an agent\'s walk to an act: the act never happened',
        expected: 'a new order that drops a walk-in says so before it lands ("Cancels Listen") and on the plate after', observed: `${w[1]} walking to ${w[2]}: ended by a ${w[3]} at ${clock(l.tick)} (${who(src)}${m ? `; meant: ${m.kind}` : ''})`, tick: l.tick });
      continue;
    }
    const s = stopped(l); if (!s || !s.op) continue;
    const at = orders.filter(o => o.op === s.op && Math.abs(o.tick - l.tick) <= HUMAN_RULES.gestureTicks);
    const lone = at.length > 0 && at.every(o => o.do === 'cancel');
    if (lone) continue; // the player's own cancel
    const drive = at.find(o => o.do === 'drive'), other = at.find(o => o.do !== 'cancel' && o.do !== 'drive');
    if (!drive && !(other && at.some(o => o.do === 'cancel'))) continue;
    const m = meantBy(src, s.op, l.tick), cost = l.text.match(/\(([a-z-]+):/)?.[1] ?? '';
    const how = drive ? 'drive' : 'replace';
    out.push({ check: 'Q-CANCEL', severity: drive ? 'S2' : sevFor(src, m, 'S3'), key: `Q-CANCEL:${how}:${s.verb}${m ? ':meant' : ''}`, owner: drive ? 'Latch (Sky 2026-10-25: going third person never stops the current act)' : 'Latch (the pill says "Cancels ..." before the new order lands)',
      title: drive ? `Taking an agent in third person stopped their ${s.verb}` : `A new order stopped the ${s.verb} the agent was doing${m ? ' (meant)' : ''}`,
      expected: drive ? 'going third person never stops the current act (Sky 2026-10-25): the act runs on while you drive' : 'a new order on a busy agent never cancels its act unseen: the pill says "Cancels ..." before you let go, the plate and a pop after (next-batch 10-11)',
      observed: `${s.op} ${s.verb}${s.target ? ' ' + s.target : ''} stopped at ${clock(l.tick)} by ${drive ? 'a third-person drive' : `a ${other!.do}`}${cost ? ` (${cost}: the time is spent)` : ''} (${who(src)}${m ? `; meant: ${m.kind}` : ''})`, tick: l.tick });
  }
  return out;
}

export function qLoopLeft(p: Play, src: Src): Finding[] {
  if (p.error || src.mutated) return [];
  const out: Finding[] = [];
  for (const l of p.log) {
    if (l.code !== 'loop.ends' && l.code !== 'cancel.loop') continue;
    const before = p.orders.map(o => o.order).filter(o => o.op === 'tech' && o.tick <= l.tick && l.tick - o.tick <= HUMAN_RULES.loopCauseTicks);
    const cause = before.filter(o => o.do !== 'cancel').pop();
    if (!cause || before.some(o => o.do === 'cancel' && o.tick >= cause.tick)) continue; // a cancel the player gave, or no order of his
    if (cause.do === 'drive') continue; // Q-CANCEL:drive:loop says it
    const planned = src.seed === 'route' || (src.seed === 'bot' && !meantBy(src, 'tech', cause.tick) && !!src.events); // the plan's own step walks him off
    const m = meantBy(src, 'tech', cause.tick);
    out.push({ check: 'Q-LOOP-LEFT', severity: planned ? 'info' : 'S3', key: `Q-LOOP-LEFT:${cause.do}${planned ? ':planned' : m ? `:${kindOf(m.kind)}` : ''}`, owner: 'Latch (brief 058 §5 stopgap: a squad move leaves a looping Tech; a move off the cabinet takes the 0.5 s hold) / Glyph (the plate line)',
      title: planned ? 'The plan walks the Tech off the loop (planned)' : `The Tech left the camera loop for a ${cause.do}`,
      expected: 'a looping Tech leaves the cabinet only on an order meant for him, held 0.5 s, with "Camera comes back" shown', observed: `${cause.do}${cause.to ? ' to ' + JSON.stringify(cause.to) : ''}${cause.target ? ' ' + cause.target : ''} at ${clock(cause.tick)}, loop ended ${clock(l.tick)} (${who(src)}${m ? `; after a ${m.kind}` : ''})`, tick: l.tick });
  }
  return out;
}

/** Human rows: every step the bot gave (a `given` event) arrives as the act it meant. A move that becomes a carry, a click that orders someone else. */
export function qIntent(p: Play, src: Src): Finding[] {
  if (p.error || src.seed !== 'bot' || !src.events) return [];
  const out: Finding[] = [];
  for (const e of src.events.filter(x => kindOf(x.kind) === 'given' || kindOf(x.kind) === 'step')) {
    if (!OPS.has(e.op)) continue;
    const got = p.orders.map(o => o.order).filter(o => o.op === e.op && o.tick >= e.tick && o.tick - e.tick <= 20 && o.do !== 'drive' && o.do !== 'cancel');
    if (!got.length) continue; // nothing reached the engine (a shut ring, a missed click): Q-OFFER's and the row's business
    const g = got[0]!, okAct = g.do === e.act || (e.act === 'move' && g.do === 'move') || (e.act === 'turn' && g.do === 'turn');
    const okTarget = !e.target || !g.target || g.target === e.target;
    if (okAct && okTarget) continue;
    out.push({ check: 'Q-INTENT', severity: 'S2', key: `Q-INTENT:${e.act}->${g.do}`, owner: 'Latch (HostContext: what a click under the cursor gives)',
      title: `The bot meant ${e.act}${e.target ? ' on ' + e.target : ''}, the game gave ${g.do}${g.target ? ' on ' + g.target : ''}`,
      expected: 'a ground click gives a move or nothing; a click on a thing opens its ring and gives no order until a wedge is chosen (next-batch 31: a move never becomes a carry)',
      observed: `${e.op} at ${clock(e.tick)}: meant ${e.act}${e.target ? ' ' + e.target : ''}, got ${g.do}${g.target ? ' ' + g.target : ''}${e.detail ? ` (${e.detail})` : ''}`, tick: g.tick });
  }
  return out;
}

export function qPile(p: Play, src: Src): Finding[] {
  if (p.error || src.mutated) return [];
  const out: Finding[] = [], win = HUMAN_RULES.pileS * 10;
  for (const op of OPS) {
    const busy = p.orders.filter(o => o.order.op === op && !o.ok && /agent-busy|busy until/.test(`${o.code ?? ''} ${o.why ?? ''}`)).map(o => o.order.tick);
    const stops = p.log.filter(l => (l.code === 'order.walk-in-ends' && l.text.startsWith(op)) || (stopped(l)?.op === op && p.orders.some(o => o.order.op === op && o.order.do !== 'cancel' && Math.abs(o.order.tick - l.tick) <= HUMAN_RULES.gestureTicks))).map(l => l.tick);
    for (const [what, ticks] of [['refused as busy', busy], ['each stopping the one before', stops]] as const) {
      for (let i = 0, j = 0; i < ticks.length; i++) {
        while (ticks[i]! - ticks[j]! > win) j++;
        if (i - j + 1 < HUMAN_RULES.pileN) continue;
        out.push({ check: 'Q-PILE', severity: 'S3', key: `Q-PILE:${what === 'refused as busy' ? 'refused' : 'stopped'}`, owner: 'Latch / Glyph (the plate shows busy and until when)',
          title: `Orders piled up on a busy ${op}: ${what}`, expected: `an agent who is busy looks busy (the plate, the ring's lock) so a player does not order them ${HUMAN_RULES.pileN} times in ${HUMAN_RULES.pileS} s`,
          observed: `${i - j + 1} orders to the ${op} between ${clock(ticks[j]!)} and ${clock(ticks[i]!)}, ${what} (${who(src)})`, tick: ticks[i] });
        j = i + 1; // one finding a pile
      }
    }
  }
  return out;
}

/** The tick a target went down, out, dead, carried off or inside, from the log (lines that name it). */
export function goneAt(p: Play, target: string, before: number): number | undefined {
  const t = target.replace(/^camera:/, ''), word = new RegExp(`(^|[^a-z0-9-])${t.replace(/[-]/g, '\\-')}([^a-z0-9-]|$)`, 'i');
  let at: number | undefined;
  for (const l of p.log) {
    if (l.tick > before) break;
    const gone = /(^|\.)(down|dead|out|officer-down|lifted|in)$/.test(l.code) && !/^order\.|^cancel\./.test(l.code);
    if (gone && (l.code.startsWith(t + '.') || word.test(l.text))) at = l.tick;
  }
  return at;
}
const TARGET_GONE = /no-target|target-down|already-down|target-gone|gone|not-there|is down|is out|downed-target|target-inside/;

export function qStaleRing(p: Play, src: Src): Finding[] {
  if (p.error || src.mutated) return [];
  const out: Finding[] = [];
  for (const o of p.orders) {
    if (o.ok || !o.order.target || o.order.do === 'drive') continue;
    if (!TARGET_GONE.test(`${o.code ?? ''} ${o.why ?? ''}`)) continue;
    const at = goneAt(p, o.order.target, o.order.tick); if (at === undefined || o.order.tick - at > HUMAN_RULES.staleS * 10) continue;
    const race = o.order.tick - at <= HUMAN_RULES.raceTicks;
    out.push({ check: 'Q-STALE-RING', severity: race ? 'info' : 'S2', key: `Q-STALE-RING:${race ? 'race' : o.order.do}`, owner: 'Latch (the open ring re-checks its target every tick, as its givers)',
      title: race ? 'A target went down in the same tick the order landed (a race)' : `The ring still offered ${o.order.do} on ${o.order.target}, who was already gone`,
      expected: 'a wedge on someone who has gone down, been carried off or gone inside shuts (or the ring closes) within 0.2 s', observed: `${o.order.op} ${o.order.do} ${o.order.target} at ${clock(o.order.tick)}: ${o.code}; gone at ${clock(at)}, ${((o.order.tick - at) / 10).toFixed(1)} s before (${who(src)})`, tick: o.order.tick });
  }
  return out;
}

/** The log line that says an act landed (for its verb), and whether it names the target. */
function landed(verb: string, target: string, l: { code: string; text: string }, op = ''): boolean {
  const named = !target || l.text.includes(`(${target})`) || l.text.includes(target);
  switch (verb) {
    case 'watch': return new RegExp(`^${op || '(scout|fixer|tech|enforcer)'}\\.watch\\.(?!container)`).test(l.code) && named;
    case 'listen': return new RegExp(`^${op || '(scout|fixer|tech|enforcer)'}\\.listen\\.`).test(l.code) && named;
    case 'turn': return /^turn\.[a-z]+\.(turned|refused)$/.test(l.code) && named;
    case 'loop': return l.code === 'loop.on';
    case 'reroute': return l.code === 'patrol.rerouted';
    case 'mislead': return l.code === 'rivals.misled';
    case 'call': return /^call\./.test(l.code);
    case 'takedown': return /^takedown\.(out|dead)$/.test(l.code) && named;
    case 'enter': return l.code === 'enter.in';
    default: return false;
  }
}

export function qLeak(p: Play, src: Src): Finding[] {
  if (p.error) return [];
  const out: Finding[] = [];
  const walk = (l: { code: string; text: string }) => { const m = l.code === 'order.walk-in-ends' && l.text.match(/^(scout|fixer|tech|enforcer): the walk to ([^ ]+) ends/); return m ? { op: m[1]!, target: m[2]! } : undefined; };
  for (const l of p.log) {
    const s = stopped(l), w = walk(l);
    const drops: { op: string; verb: string; target: string }[] = [];
    if (s && s.verb !== 'loop' && s.verb !== 'carry' && l.text.includes('nothing')) drops.push(s); // "lost: ... nothing lands / is learned"
    if (w) { const o = p.orders.map(x => x.order).filter(x => x.op === w.op && x.target === w.target && x.tick <= l.tick && ACT.has(x.do)).pop(); if (o) drops.push({ op: w.op, verb: o.do, target: w.target }); }
    for (const d of drops) {
      const again = (t: number) => p.orders.some(x => x.ok && x.order.do === d.verb && (!d.target || x.order.target === d.target) && x.order.tick > l.tick && x.order.tick <= t);
      // another agent with the same act on the same target going (taken before the stop) may land it: not this one's
      const elsewhere = p.orders.some(x => x.ok && x.order.op !== d.op && x.order.do === d.verb && (!d.target || x.order.target === d.target) && x.order.tick <= l.tick);
      const late = p.log.find(x => x.tick > l.tick && landed(d.verb, d.target, x, d.op) && !again(x.tick) && (/^(watch|listen)$/.test(d.verb) || !elsewhere));
      if (late) out.push({ check: 'Q-LEAK', severity: 'S1', key: `Q-LEAK:lands:${d.verb}`, owner: 'Keel', title: `A cancelled ${d.verb} still happened`,
        expected: 'an act stopped before it lands never lands, unless it is ordered again', observed: `${d.op} ${d.verb}${d.target ? ' ' + d.target : ''} stopped at ${clock(l.tick)}; "${late.text.slice(0, 90)}" at ${clock(late.tick)} (${who(src)})`, tick: late.tick });
      const cost = elsewhere ? undefined : (p.ledger ?? []).find(x => x.tick > l.tick && (x.what === 'trail' || x.what === 'alert') && x.cause.split(/[.:]/)[0] === d.verb && (!d.target || x.cause.includes(d.target) || !x.cause.includes(':')) && !again(x.tick));
      if (cost) out.push({ check: 'Q-LEAK', severity: 'S2', key: `Q-LEAK:cost:${d.verb}`, owner: 'Keel', title: `A cancelled ${d.verb} still cost ${cost.what === 'trail' ? 'Trail' : 'Alert'}`,
        expected: 'what a stopped act costs is the rulebook cancel line, shown before you cancel; nothing more lands later', observed: `${d.op} ${d.verb}${d.target ? ' ' + d.target : ''} stopped at ${clock(l.tick)}; ${cost.what} +${cost.delta} (${cost.cause}) at ${clock(cost.tick)} (${who(src)})`, tick: cost.tick });
    }
  }
  return out;
}

/** Every human-bot check for one play. `src` says where the play came from and, on a human row, what the bot meant. */
export function humanChecks(p: Play, src: Src): Finding[] {
  return [...qCancel(p, src), ...qLoopLeft(p, src), ...qIntent(p, src), ...qPile(p, src), ...qStaleRing(p, src), ...qLeak(p, src)];
}
/** The same for a tier-1 item (a seed or its mutant). */
export function qHuman(p: Play, it: Item): Finding[] {
  const sky = bySky(it.seed), f = humanChecks(p, { seed: it.seed.kind, mutated: it.mutations.length > 0, sky });
  // a saved session the engine no longer replays as Sky saw it (as Q-OFFER): what the game did differs, so only the engine's own invariant stands
  if (it.seed.kind !== 'session' || p.qa?.record === '') return f;
  return f.map(x => x.check === 'Q-LEAK' ? x : { ...x, severity: 'info', key: `${x.key}:replay-differs`, observed: `${x.observed} (replay differs from ${sky ? 'what Sky saw' : 'the recording'})` });
}
