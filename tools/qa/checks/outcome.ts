// Brief 060 §2 (Suture): Q-ERR, Q-DET, Q-FAIL and Q-DEBRIEF over one play.
import type { Ctx, Finding, Play } from '../types';

const has = (p: Play, code: string, re?: RegExp) => p.log.some(l => l.code === code && (!re || re.test(l.text)));

/** An engine error's message with its ids taken out, so one fault is one issue ("no person in the role '*'"). */
export const errorClass = (why: string) => why.replace(/^engine error: /, '').replace(/'[^']*'/g, "'*'").replace(/\(.*\)/, '').replace(/\d+/g, 'n').trim().slice(0, 90);

/** Q-ERR: no engine exception, no engine-error refusal or fail, no debrief that throws (the host would show no end card). */
export function qErr(p: Play): Finding[] {
  const out: Finding[] = [];
  if (p.error) out.push({ check: 'Q-ERR', severity: 'S1', key: 'Q-ERR:exception:' + p.error.split(':')[0], title: 'The engine threw while playing', expected: 'every script plays to its end', observed: p.error.slice(0, 200), owner: 'Keel' });
  if (p.error) return out;
  for (const o of p.orders) if (o.code === 'engine-error') out.push({ check: 'Q-ERR', severity: 'S1', key: 'Q-ERR:refusal:' + errorClass(o.why ?? ''), title: `An order gets engine-error instead of a refusal: ${errorClass(o.why ?? '')}`, expected: 'a refusal names a rule', observed: (o.why ?? '').slice(0, 200), tick: o.order.tick, owner: 'Keel' });
  const fe = p.log.find(l => l.code === 'fail.engine-error');
  if (fe) out.push({ check: 'Q-ERR', severity: 'S1', key: 'Q-ERR:fail', title: 'The run failed with engine-error', expected: 'no engine error', observed: fe.text.slice(0, 200), tick: fe.tick, owner: 'Keel' });
  if (p.qa?.debriefError) out.push({ check: 'Q-ERR', severity: 'S1', key: 'Q-ERR:debrief', title: 'Debrief.Of throws at this ending (the host shows no debrief)', expected: 'a debrief on every ending', observed: p.qa.debriefError.slice(0, 200), tick: p.tick, owner: 'Keel' });
  return out;
}

/** Q-DET: the engine's own replay of the play (its orders, from its start state) matches it checkpoint for checkpoint. */
export function qDet(p: Play): Finding[] {
  if (p.error || p.replayIdentical !== false) return [];
  return [{ check: 'Q-DET', severity: 'S1', key: 'Q-DET:replay', title: 'A replay of the same orders differs', expected: 'replayIdentical true', observed: 'replayIdentical false', tick: p.tick, owner: 'Keel' }];
}

/** The fail camera's caption key for a cause (text.json less.failcam rule: for sealed, the origin's reporter desk or heard, else origin.because). */
export function captionKey(p: Play): string {
  const c = p.cause!;
  if (c.fail !== 'sealed') return c.why;
  const rep = c.origin?.reporter?.id;
  return rep === 'desk' || rep === 'heard' ? rep : c.origin?.because ?? '';
}

/** Q-FAIL: a fail carries its code, a FailCause with a culprit (or an origin when no single culprit decided it), the log line, and card text. */
export function qFail(p: Play, ctx: Ctx): Finding[] {
  if (p.error) return [];
  const out: Finding[] = [], f = (key: string, title: string, expected: string, observed: string, severity: Finding['severity'] = 'S2', owner = 'Keel') =>
    out.push({ check: 'Q-FAIL', severity, key: 'Q-FAIL:' + key, title, expected, observed, tick: p.tick, owner });
  if (p.outcome === 'success') {
    if (!has(p, 'success')) f('success-no-log', 'A success with no success line in the log', 'log has success', 'none');
    if (p.cause) f('success-cause', 'A success carries a fail cause', 'no cause on a success', p.cause.fail);
    return out;
  }
  if (p.outcome === 'withdrawn') {
    if (!p.cause || p.cause.fail !== 'withdraw') f('withdraw-cause', 'A withdrawal without its cause', 'cause.fail = withdraw', p.cause?.fail ?? 'no cause');
    return out;
  }
  if (p.outcome !== 'failure') return out;
  if (!p.fail) { f('no-code', 'A failure with no fail code', 'a fail code', '""', 'S1'); return out; }
  if (!has(p, 'fail.' + p.fail)) f('no-log', `Fail ${p.fail} has no fail line in the log`, `log fail.${p.fail}`, 'none');
  if (!p.cause) f('no-cause', `Fail ${p.fail} has no FailCause (the fail camera has nothing to show)`, 'a cause', 'none', 'S2');
  else {
    if (p.cause.fail !== p.fail) f('cause-code', `Fail ${p.fail}: its cause names ${p.cause.fail}`, p.fail, p.cause.fail, 'S1');
    if (p.cause.single && !p.cause.culprits.length) f('no-culprit', `Fail ${p.fail} is single but names no culprit`, 'a culprit', 'none');
    if (!p.cause.single && !p.cause.origin && p.fail === 'sealed') f('no-origin', 'A seal with no origin (the fail camera cannot say what started it)', 'cause.origin', 'none', 'S3');
    const ck = captionKey(p);
    if (!ctx.text.failcam[p.fail]?.[ck]) f(`no-caption:${p.fail}:${ck}`, `The fail camera has no caption for ${p.fail} / ${ck || '(empty)'}`, 'text.json less.failcam.caption entry', 'missing', 'S3', 'Fable');
  }
  if (!ctx.text.fails.has(p.fail)) f(`no-card:${p.fail}`, `Fail ${p.fail} has no card line (briefing.fails)`, 'card text', 'missing', 'S2', 'Fable');
  if (!ctx.text.endFails.has(p.fail)) f(`no-end:${p.fail}`, `Fail ${p.fail} has no end-card line (less.end.fail)`, 'end text', 'missing', 'S2', 'Fable');
  return out;
}

/** board.in with the councillor aboard: the Enforcer's board names him ("… with Popovic."); one carrying an agent says "with the scout". */
const VALE_ABOARD = /^The enforcer is in the car with (?!the )\S/;
/** The log facts each debrief route id claims (sim Debrief.cs route ids → the engine log lines that back them). */
export const CLAIMS: Record<string, { why: string; backed: (p: Play) => boolean }> = {
  'vale-reaches-getaway-car': { why: 'board.in, the Enforcer with the councillor', backed: p => has(p, 'board.in', VALE_ABOARD) }, // by role, not his name (brief 089: the engine names him from the cast)
  'vale-taken-through-office': { why: 'enter.out (always with the councillor)', backed: p => has(p, 'enter.out') },
  'vale-intercepted-outside': { why: 'take.done', backed: p => has(p, 'take.done') },
  'waitress-pulls-guard-away': { why: 'guard.at-cafe', backed: p => has(p, 'guard.at-cafe') },
  'guard-goes-to-waitress': { why: 'call.guard, then guard.at-cafe', backed: p => has(p, 'call.guard') && has(p, 'guard.at-cafe') }, // engine 0.51.0 (brief 102, option 1): the turned guard's own call
  'entrance-camera-looped': { why: 'loop.on', backed: p => has(p, 'loop.on') },
  'patrol-diverted': { why: 'patrol.rerouted', backed: p => has(p, 'patrol.rerouted') },
  'entry-timed-between-sweeps': { why: 'enter.in', backed: p => has(p, 'enter.in') },
  'guard-shot-beside-waitress': { why: 'guard.down', backed: p => has(p, 'guard.down') },
  'guard-shot-away-from-waitress': { why: 'guard.down', backed: p => has(p, 'guard.down') },
  'guard-knocked-out': { why: 'takedown.out (guard)', backed: p => has(p, 'takedown.out', /guard/i) },
  'guard-killed': { why: 'takedown.dead (guard) or guard.down', backed: p => has(p, 'takedown.dead', /guard/i) || has(p, 'guard.down') },
  'driver-moves-vale-to-service-lane': { why: 'vale.exits to the service lane', backed: p => has(p, 'vale.exits', /service lane/) },
  'rivals-driven-back': { why: 'rivals.retreat', backed: p => has(p, 'rivals.retreat') },
  'rivals-follow-false-tip': { why: 'rivals.misled', backed: p => has(p, 'rivals.misled') },
};
/** Route ids that only a won run may claim. */
const WIN_ONLY = new Set(['vale-reaches-getaway-car']);
/** Route ids the engine retired, with the version that retired them: no debrief of that engine or later gives them, so one that does is a
 *  regression (engine 0.33.0, brief 059: "pickup-camera-blocked" went with car.blocks and the driver's Call; nothing blocks the pickup camera). */
export const RETIRED: Record<string, string> = { 'pickup-camera-blocked': 'engine 0.33.0 (brief 059: car.blocks and the driver\'s Call retired)' };

/** Q-DEBRIEF: every route claim of the debrief (built on every ending, as HandoverHost does) is backed by the log; a run that did not win
 *  makes no claim only a win can make; the debrief's own loud flag agrees with the gunfire count. */
export function qDebrief(p: Play): Finding[] {
  if (p.error || p.outcome === 'none') return []; // a script that stops before the mission ends (Sky left mid-run) has no end card
  const d = p.qa?.debrief ?? p.debrief; if (!d) return [];
  const out: Finding[] = [];
  for (const id of d.route ?? []) {
    if (RETIRED[id]) { out.push({ check: 'Q-DEBRIEF', severity: 'S2', key: 'Q-DEBRIEF:retired:' + id, title: `The debrief still says ${id}, retired in ${RETIRED[id]}`, expected: 'no retired route id', observed: id, tick: p.tick, owner: 'Keel' }); continue; }
    const c = CLAIMS[id];
    if (!c) { out.push({ check: 'Q-DEBRIEF', severity: 'info', key: 'Q-DEBRIEF:unknown:' + id, title: `Debrief route id ${id} has no evidence rule yet`, expected: 'a rule in tools/qa/checks/outcome.ts CLAIMS', observed: id, owner: 'Suture' }); continue; }
    if (c.backed(p)) continue;
    const lost = p.outcome !== 'success';
    out.push({
      check: 'Q-DEBRIEF', severity: lost && WIN_ONLY.has(id) ? 'S1' : 'S2', key: `Q-DEBRIEF:${id}:${lost ? 'not-won' : 'won'}`,
      title: lost && WIN_ONLY.has(id) ? `A run that did not win says "${id}" in its debrief` : `Debrief says ${id} with nothing in the log behind it`,
      expected: `the log shows ${c.why}`, observed: `${p.outcome}${p.fail ? ' (' + p.fail + ')' : ''}; no ${c.why} in the log`, tick: p.tick, owner: 'Keel',
    });
  }
  if (typeof d.loud === 'boolean' && d.loud !== p.gunfire > 0) out.push({ check: 'Q-DEBRIEF', severity: 'S2', key: 'Q-DEBRIEF:loud', title: 'The debrief\'s loud flag disagrees with the shots fired', expected: `loud ${p.gunfire > 0}`, observed: `loud ${d.loud}`, tick: p.tick, owner: 'Keel' });
  return out;
}
