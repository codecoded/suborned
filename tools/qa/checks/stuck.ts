// Brief 060 §2 #10, #11 (Suture): Q-STUCK and Q-SPAWN (sim half), from the QA host's samples (one a second of game time; a stopgap until
// Keel's batch --trace).
//   Q-STUCK  Vale at his car (vale.at-car, state Pickup) is taken, boards or changes state within valeStuckS [S 10 s]; an operative with an
//            open order (a walk, a walk-in act, an act past its landing) does not stand still for longer than stuckS [S 10 s].
//   Q-SPAWN  nobody is on the street before the schedule brings them: a rival before the rivals arrive (rivals.arrive or rivals.drawn), a
//            response unit before a dispatch (alarm.dispatches, security.called, response.reinforces).
import type { Ctx, Finding, Play } from '../types';
import { clock } from './warn';

export function qStuck(p: Play, ctx: Ctx): Finding[] {
  if (p.error || !p.qa) return [];
  const out: Finding[] = [], vale = p.qa.vale;
  for (let i = 0; i < vale.length; i++) {
    if (vale[i]!.state !== 'Pickup') continue;
    const from = vale[i]!.tick, to = i + 1 < vale.length ? vale[i + 1]!.tick : p.tick, s = (to - from) / 10;
    if (s <= ctx.rules.valeStuckS) continue;
    const next = i + 1 < vale.length ? `then ${vale[i + 1]!.state}` : p.outcome === 'none' ? 'still there when the script ended' : `still there at the end (${p.fail || p.outcome})`;
    out.push({
      check: 'Q-STUCK', severity: 'S1', key: 'Q-STUCK:vale-at-car', title: 'Vale stands at his car and never gets in',
      expected: `within ${ctx.rules.valeStuckS} s of vale.at-car Vale boards, is taken, or moves on`, observed: `at the car ${clock(from)} → ${clock(to)} (${s.toFixed(0)} s), ${next}`, tick: from, owner: 'Keel',
    });
  }
  for (const st of p.qa.stuck) {
    const s = (st.to - st.from) / 10; if (s <= ctx.rules.stuckS) continue;
    const verb = st.order.split(':')[0]!;
    out.push({
      check: 'Q-STUCK', severity: 'S1', key: `Q-STUCK:agent:${st.kind}:${verb}`, title: `An agent stands still on an open order (${st.kind}, ${verb})`,
      expected: `an agent with an open order moves or lands it within ${ctx.rules.stuckS} s`, observed: `the ${st.op} on ${st.order} at (${st.x}, ${st.y}) from ${clock(st.from)} to ${clock(st.to)} (${s.toFixed(0)} s)`, tick: st.from, owner: 'Keel',
    });
  }
  return out;
}

const firstTick = (p: Play, codes: string[]) => { const l = p.log.find(x => codes.includes(x.code)); return l ? l.tick : Infinity; };
export function qSpawn(p: Play): Finding[] {
  if (p.error || !p.qa) return [];
  const out: Finding[] = [];
  const rivals = firstTick(p, ['rivals.arrive', 'rivals.drawn']), units = firstTick(p, ['alarm.dispatches', 'security.called', 'response.reinforces']);
  for (const s of p.qa.seen) {
    const rival = /^rival/.test(s.id), unit = /^(security|police|robot|officer)-/.test(s.id);
    const due = rival ? rivals : unit ? units : -Infinity;
    if (s.tick >= due) continue;
    out.push({
      check: 'Q-SPAWN', severity: 'S3', key: `Q-SPAWN:${rival ? 'rival' : 'unit'}`, title: `${rival ? 'A rival' : 'A response unit'} is on the street before the schedule brings them`,
      expected: `absent until ${rival ? 'rivals.arrive or rivals.drawn' : 'a dispatch'}${Number.isFinite(due) ? ' at ' + clock(due) : ' (none in this run)'}`, observed: `${s.id} present at ${clock(s.tick)} at (${s.x}, ${s.y})`, tick: s.tick, owner: 'Keel',
    });
  }
  return out;
}
