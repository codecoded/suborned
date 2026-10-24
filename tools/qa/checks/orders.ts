// Brief 060 §2 (Suture): the order checks.
//   Q-OFFER (sim proxy)  an order a seed gave is taken: a route's orders are its plan's steps, and every order in a saved session came from the
//                        host's ring, panel or keys (Script.Write keeps refused ones: they are the player's). A refusal there is "offered,
//                        then refused" (the host half, Q-OFFER on the real offer, waits for Latch's -bot row). Mutants are exempt: they are
//                        meant to be refused; their codes go to the tables. mission-over is not counted (an order after the end).
//   Q-DROP               an order taken and later dropped on arrival (order.dropped, rulebook cell.approach): the price was shown, then the
//                        act never happened. Seeds only, for the same reason.
//   Q-LEARN (sim proxy)  a completed Watch or Listen of the same person by the same agent lands twice with the same words: the repeat was
//                        taken and "learned" again (Sky #1). The trace's knowledge diff (Keel) will make this exact.
import type { Finding, Item, Play } from '../types';
import { clock } from './warn';

export function qOffer(p: Play, it: Item): Finding[] {
  if (p.error || it.mutations.length) return [];
  const faithful = it.seed.kind === 'route' || p.qa?.record === '';
  const planned = it.seed.kind === 'route' && (it.seed.routeKind === 'careless' || it.seed.routeKind === 'fail'); // a careless or fail route's refusal is its point
  const out: Finding[] = [];
  for (const o of p.orders) {
    if (o.ok || o.code === 'mission-over' || o.order.do === 'drive') continue;
    out.push({
      check: 'Q-OFFER', severity: faithful && !planned ? 'S2' : 'info', key: `Q-OFFER:${it.seed.kind}:${o.order.do}:${o.code}${faithful ? '' : ':replay-differs'}${planned ? ':planned' : ''}`,
      title: `${it.seed.kind === 'route' ? 'A route step' : 'An order Sky gave'} was refused: ${o.order.do} (${o.code})${planned ? ', as the route plans' : faithful ? '' : ', on a replay that differs from his'}`,
      expected: it.seed.kind === 'route' ? 'every step of the plan is taken' : 'what the host offers, the engine takes (or the pill showed the refusal first)',
      observed: `${o.order.op ?? 'cell'} ${o.order.do}${o.order.target ? ' ' + o.order.target : ''} at ${clock(o.order.tick)}: ${o.code}: ${(o.why ?? '').slice(0, 120)}${faithful ? '' : ' (replay differs from what Sky saw)'}`,
      tick: o.order.tick, owner: it.seed.kind === 'route' ? 'Waymark' : 'Latch',
    });
  }
  return out;
}

export function qDrop(p: Play, it: Item): Finding[] {
  if (p.error || it.mutations.length) return [];
  return p.log.filter(l => l.code === 'order.dropped').map(l => {
    const code = (l.text.split(': ')[1] ?? '').trim() || 'dropped';
    return { check: 'Q-DROP', severity: 'S2' as const, key: `Q-DROP:${code}`, title: `A taken order was dropped on arrival (${code})`, expected: 'an order taken is carried out, or refused when given', observed: `${l.text.slice(0, 160)} at ${clock(l.tick)}`, tick: l.tick, owner: 'Keel' };
  });
}

const LOOK = /^(scout|fixer|tech|enforcer)\.(watch|listen)\./;
export function qLearn(p: Play): Finding[] {
  if (p.error) return [];
  const seen = new Map<string, number>(), out: Finding[] = [];
  for (const l of p.log) {
    if (!LOOK.test(l.code) || l.code.endsWith('.container')) continue;
    const k = `${l.code}|${l.text}`, first = seen.get(k);
    if (first === undefined) { seen.set(k, l.tick); continue; }
    out.push({ check: 'Q-LEARN', severity: 'S2', key: `Q-LEARN:${l.code.split('.')[1]}`, title: `A repeat ${l.code.split('.')[1] === 'watch' ? 'Watch' : 'Listen'} is taken and lands again with the same result`, expected: 'a repeat Watch or Listen teaches something new, or is refused (already-watched, already-heard)', observed: `"${l.text.slice(0, 100)}" at ${clock(first)} and again at ${clock(l.tick)}`, tick: l.tick, owner: 'Keel' });
  }
  return out;
}
