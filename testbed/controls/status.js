// The live words for an act: what the pill and the portrait plate say while an order is walking, running, acting, done or stopped.
import { G } from './state.js';
import { WALK, RUN } from './data.js';
import { fireStatus } from './fire.js';

const secs = (a) => { const o = a.order; return Math.max(0, Math.round(G.world.nav.length(a.g.position.x, a.g.position.z, o.path) / (o.run ? RUN : WALK))); };
const left = (o) => Math.max(0, Math.round(((o.dur ?? o.act.t / G.actScale) - o.t) * G.actScale));
const moving = (o) => o.kind === 'act' && o.phase === 'walk';
// short: for a plate or for one face among several
export function orderWords(a) {
  const o = a.order; if (!o) return '';
  if (o.kind === 'walk' || moving(o)) return `${o.run ? 'running' : 'walking'} ${secs(a)} s`;
  return o.act.fire ? 'firing' : `${o.act.ing.toLowerCase()} ${left(o)} s`;
}
const long = (a) => { const o = a.order; return moving(o) ? `${o.run ? 'Running' : 'Walking'} there · ${secs(a)} s` : `${o.act.ing} · ${left(o)} s left`; };
const head = (act, e) => (act.bare ? act.verb : `${act.verb} ${e.name}`);

export function orderStatus(e) {
  const now = G.clock, ags = G.world.agents.filter((a) => a.order?.kind === 'act' && a.order.ent === e);
  if (ags.length) {
    const act = ags[0].order.act;
    if (G.ordered?.ent === e && now < G.ordered.until) return { ent: e, act, text: `${head(act, e)} · ordered`, agents: ags };
    if (ags.length === 1) return { ent: e, act, text: long(ags[0]), agents: ags };
    return { ent: e, act, text: '', items: ags.map((a) => ({ a, text: orderWords(a) })), agents: ags };
  }
  const d = G.doneInfo; if (G.done === 'built' && d?.ent === e && now < d.until) // brief 043: the gain chip replaces this line unless Done moment is 'As built'
    return { ent: e, act: d.act, text: `Done · ${d.act.doneText ?? 'done'}`, agents: d.agents };
  const s = G.stoppedInfo; if (s?.ent === e && now < s.until) return { ent: e, act: s.act, text: 'Stopped', agents: [] };
  return null;
}
// One status for the pill: a fire hold first, then the orders on the thing under the pointer.
export function liveStatus(tgt) { const f = fireStatus(); return f ? { ...f, fire: true } : tgt ? orderStatus(tgt) : null; }
