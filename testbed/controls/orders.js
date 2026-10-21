// Fake acts: walk there, then a timer, then the result. Prices are the Handover's.
import * as THREE from './vendor/three.module.min.js';
import { G, say } from './state.js';
import { ACTS, WALK, RUN, ROLES, priceLine } from './data.js';

const HARM_ORDER = ['knock', 'kill', 'fire'];
export const agentOf = () => G.world.agents[G.sel]; // the lead: first selected, the one driven in third person
export const selected = () => G.world.agents.filter((_, i) => G.group[i]);
export const crew = () => G.carry ?? selected(); // who an order is for (while a portrait is dragged: who it carries)
export const names = (l) => l.map((a) => a.name).join(', ');
export const reachOf = (e, act, agent) => (act.noWalk ? Infinity : act.reachBy && agent ? act.reachBy[agent.role] : e.reach);

// The ways the selected agent has on a thing: calm ways first, the lightest harm last.
export function waysFor(e, agent) {
  if (!e || !e.acts?.length || ['out', 'dead', 'down'].includes(e.state)) return [];
  const who = agent ? [agent] : crew(); // no agent given: any of the crew can give the way
  const ids = e.acts.filter((id) => who.some((a) => ACTS[id].by.includes(a.role)) && (ACTS[id].again || !e.used.has(id)) && !(id === 'fire' && !e.hostile));
  const calm = ids.filter((id) => ACTS[id].kind !== 'harm'), harm = ids.filter((id) => ACTS[id].kind === 'harm').sort((a, b) => HARM_ORDER.indexOf(a) - HARM_ORDER.indexOf(b));
  return [...calm, ...harm].map((id) => ({ id, ...ACTS[id] }));
}
// Harm is never the main action, except firing at someone who is shooting at you.
export function defaultFor(e, agent) {
  const ways = waysFor(e, agent);
  return (e.hostile && ways.find((w) => w.id === 'fire')) || ways.find((w) => w.kind !== 'harm') || null;
}

// Who does a way, and for how long. Config G.squadAct: 'best' = the best-placed agent who can, the rest hold
// (a hostile is the exception: everyone who can fire locks it); 'all' = everyone who can does it.
export function planFor(e, w, agents = crew(), run = false) {
  const able = agents.filter((a) => w.by.includes(a.role));
  if (!able.length) return { agents: [], walkS: 0, lead: null, running: false, holds: agents };
  const rf = () => run; // the order's gait: run only if it was given as a run (Shift or a double click)
  let who = able;
  if (G.squadAct === 'best' && able.length > 1 && true) who = [able.reduce((b, a) => (walkSeconds(a, e, w, rf(a)) < walkSeconds(b, e, w, rf(b)) ? a : b))];
  const walks = who.map((a) => walkSeconds(a, e, w, rf(a))).filter(isFinite);
  const said = agents.length > 1 ? (who.length === agents.length ? `all ${who.length}` : names(who)) : '';
  return { agents: who, walkS: walks.length ? Math.max(...walks) : Infinity, lead: who[0], running: rf(who[0]), holds: agents.filter((a) => !who.includes(a)), who: said };
}

const cache = new Map(), pathCache = new Map();
// Walk time to be in reach, one number with the act. Cached by where the agent stands.
export function walkSeconds(agent, e, act, run = false) {
  const p = agent.g.position, key = `${agent.id}|${e.id}|${act.id}|${Math.round(p.x * 2)}|${Math.round(p.z * 2)}`;
  let l = cache.get(key);
  if (l === undefined) { const path = G.world.nav.path(p.x, p.z, e.g.position.x, e.g.position.z, reachOf(e, act, agent)); l = path ? G.world.nav.length(p.x, p.z, path) : Infinity; cache.set(key, l); pathCache.set(key, path); if (cache.size > 400) cache.clear(); }
  return l / (run ? RUN : WALK);
}

// The walk as ground points, for the dashed line (valid after walkSeconds for the same agent and target).
export function walkPath(agent, e, act) {
  const p = agent.g.position, key = `${agent.id}|${e.id}|${act.id}|${Math.round(p.x * 2)}|${Math.round(p.z * 2)}`;
  const path = pathCache.get(key); return path?.length ? [[p.x, p.z], ...path] : null;
}

export function cancel(agent, quiet) {
  if (G.plan.delete(agent.id)) return true; // a queued (ghost) order
  const o = agent.order; if (!o) return false;
  if (o.kind === 'act') G.stoppedInfo = { ent: o.ent, act: o.act, until: G.clock + 1 };
  if (!quiet && o.kind === 'act' && o.phase === 'act' && o.t > 0) say(`${o.act.verb} stopped · nothing done${o.act.cost ? ' · no cash spent' : ''}`, 'warn'); // an act under way: what it cost
  if (o.ent) o.ent.frozen = false;
  agent.order = null; agent.speed = 0; return true;
}

// Paused with "start together": an order is kept as a ghost (path, spot, label) and every ghost starts when play resumes.
const queueing = () => G.paused && G.planMode === 'sync' && !G.flushing;
export function flushPlan() {
  const items = [...G.plan.values()]; G.plan.clear(); G.flushing = true;
  for (const it of items) it.exec(); G.flushing = false;
  if (items.length) say(`Go · ${items.length} order${items.length > 1 ? 's' : ''} start together`, 'ok');
}

export function walkTo(agent, x, z, run = false, follow = false) {
  const path = G.world.nav.path(agent.g.position.x, agent.g.position.z, x, z, 0.4);
  if (!path) { say('No way there', 'warn'); return null; }
  const len = G.world.nav.length(agent.g.position.x, agent.g.position.z, path);
  if (queueing()) { G.plan.set(agent.id, { agent, kind: 'walk', path, end: [x, z], label: `${run ? 'Run' : 'Walk'} · ${Math.round(len / (run ? RUN : WALK))} s`, icon: run ? 'run' : 'walk', exec: () => walkTo(agent, x, z, run, follow) }); return len / (run ? RUN : WALK); }
  cancel(agent);
  agent.order = { kind: 'walk', path, run, follow, dest: new THREE.Vector3(x, 0, z), len0: len };
  return len / (run ? RUN : WALK);
}

// A group walk: each agent keeps its place relative to the others when they are already spaced out,
// otherwise they spread on a small ring. Every spot is moved to the nearest walkable one.
export function groupWalk(agents, p, run = false) {
  const nav = G.world.nav, n = agents.length;
  const cx = agents.reduce((s, a) => s + a.g.position.x, 0) / n, cz = agents.reduce((s, a) => s + a.g.position.z, 0) / n;
  const offs = agents.map((a) => [a.g.position.x - cx, a.g.position.z - cz]);
  const spaced = n > 1 && offs.every((o, i) => offs.every((q, j) => i === j || Math.hypot(o[0] - q[0], o[1] - q[1]) >= 1.1)) && Math.max(...offs.map((o) => Math.hypot(o[0], o[1]))) <= 4;
  const slots = n === 1 ? [[0, 0]] : spaced ? offs : agents.map((_, i) => [Math.cos(i / n * 6.283 + 0.8) * 1.2, Math.sin(i / n * 6.283 + 0.8) * 1.2]);
  return agents.map((a, i) => { const t = nav.nearestFree(p.x + slots[i][0], p.z + slots[i][1]) ?? [p.x, p.z]; return walkTo(a, t[0], t[1], run); });
}

export function giveAct(agent, e, act, run = false) {
  if (act.cost && G.cash < act.cost) { say(`Not enough cash for ${act.verb.toLowerCase()} (needs $${act.cost.toLocaleString('en-US')})`, 'warn'); return false; }
  const reach = reachOf(e, act, agent), p = agent.g.position;
  const path = G.world.nav.path(p.x, p.z, e.g.position.x, e.g.position.z, reach);
  if (!path) { say(`No way to ${e.name}`, 'warn'); return false; }
  const len = G.world.nav.length(p.x, p.z, path);
  if (queueing()) { G.plan.set(agent.id, { agent, kind: 'act', ent: e, act, path, end: path.length ? path[path.length - 1] : [p.x, p.z], label: priceLine(act, e, len / (run ? RUN : WALK), true, '', run).slice(0, 2).join(' · '), icon: act.icon, exec: () => giveAct(agent, e, act, run) }); return true; }
  cancel(agent);
  e.frozen = true;
  agent.order = { kind: 'act', ent: e, act, path, run, t: 0, phase: path.length ? 'walk' : 'act', len0: len };
  return true;
}

const face = (a, x, z) => { const dx = x - a.g.position.x, dz = z - a.g.position.z; if (dx || dz) a.g.rotation.y = Math.atan2(dx, dz); };
export function step(a, dt, tracer) {
  const o = a.order; if (!o) { a.speed = 0; return; }
  if (o.phase === 'walk' || o.kind === 'walk') {
    const wp = o.path[0];
    if (!wp) { if (o.kind === 'walk') { a.order = null; a.speed = 0; return; } o.phase = 'act'; }
    else {
      const p = a.g.position, dx = wp[0] - p.x, dz = wp[1] - p.z, d = Math.hypot(dx, dz), sp = o.run ? RUN : WALK, m = Math.min(d, sp * dt);
      p.x += dx / d * m; p.z += dz / d * m; face(a, wp[0], wp[1]); a.speed = sp; a.vx = dx / d * sp; a.vz = dz / d * sp;
      if (d - m < 0.05) o.path.shift();
      if (o.kind === 'act' && Math.hypot(o.ent.g.position.x - p.x, o.ent.g.position.z - p.z) <= reachOf(o.ent, o.act, a) * 0.85) { o.path.length = 0; o.phase = 'act'; }
      return;
    }
  }
  a.speed = 0; a.vx = a.vz = 0;
  const e = o.ent, p = a.g.position;
  if (Math.hypot(e.g.position.x - p.x, e.g.position.z - p.z) > reachOf(e, o.act, a) + 0.6) { // they moved away: go again
    const path = G.world.nav.path(p.x, p.z, e.g.position.x, e.g.position.z, reachOf(e, o.act, a)); if (!path) return cancel(a);
    o.path = path; o.phase = 'walk'; return;
  }
  face(a, e.g.position.x, e.g.position.z);
  const dur = o.act.t / G.actScale; o.t += dt; o.dur = dur;
  if (o.act.fire && Math.floor(o.t * 3) !== Math.floor((o.t - dt) * 3)) tracer(a, e);
  if (o.t >= dur) finish(a, o);
}

function finish(a, o) {
  const e = o.ent, act = o.act, w = G.world, tint = (c) => w.tint(e, c);
  cancel(a, true); G.stoppedInfo = null; G.doneInfo = { ent: e, act, agents: [a], until: G.clock + 2.5 }; e.used.add(act.id);
  if (act.cost) G.cash -= act.cost;
  if (act.trail) G.trail += act.trail;
  if (act.alert) G.alert = Math.min(3, Math.max(G.alert + (act.fire ? 0 : 1), act.fire ? 2 : 0));
  const done = { watch: () => tint(0x1b4a80), pay: () => tint(0x1d5a28), call: () => tint(0x1d5a28), press: () => tint(0x1d5a28), take: () => tint(0x6b6450), loop: () => tint(0x0d5c50), open: () => { e.open = true; },
    knock: () => { e.state = 'out'; w.raise(e, true); }, kill: () => { e.state = 'dead'; w.raise(e, true); tint(0x000000); }, fire: () => { e.state = 'down'; w.raise(e, true); } };
  done[act.id]?.();
  say(`Done · ${act.fire ? 'Down' : act.verb}${act.bare ? '' : ' ' + e.name}`, 'ok');
}
