// Hold the left button to fire. A press on a hostile charges the pill (0.4 s), then the shot goes;
// it keeps firing every 1.5 s while the button is held. Releasing early cancels. Hostiles take hits and go down; they don't shoot back.
import { G, say } from './state.js';
import { ACTS } from './data.js';
import { crew, agentOf, names } from './orders.js';

export const FOLLOW_M = 8, HITS = { hostile: 3, other: 2 };
export const isDrawn = (a) => G.clock < (a.drawnUntil ?? 0);
export const firingNow = (a) => !!a.firePhase || G.clock - (a.shotAt ?? -99) < 0.7;
const dist = (a, b) => Math.hypot(a.g.position.x - b.g.position.x, a.g.position.z - b.g.position.z);
const down = (e) => ['out', 'dead', 'down'].includes(e.state);

// What a hold on this thing fires: any person who is not in the cell (a hostile or a bystander). null = nothing to fire at.
export function holdFor(ent) {
  if (!ent || ent.agent || !ent.person || down(ent) || !ent.acts?.length) return null;
  return { id: 'fire', ...ACTS.fire };
}
export function startCharge(ent, way, ring = false, run = false) { G.charge = { ent, way, t: 0, done: false, ring, run, label: way.id === 'fire' ? 'fire' : way.verb.toLowerCase(), bystander: !ent.hostile && way.id === 'fire' }; }

// Who shoots: in third person the driven agent (the others join by the follow setting); on the map the squad rule: best placed or everyone.
export function shooters(ent, force = false) {
  if (G.view === 'tp') return [agentOf()];
  const able = crew().filter((a) => ACTS.fire.by.includes(a.role));
  if (force) { const drawn = able.filter(isDrawn); return drawn.length ? drawn : able; } // right hold: every selected drawn agent
  return G.squadAct === 'all' || able.length < 2 ? able : [able.reduce((b, a) => (dist(a, ent) < dist(b, ent) ? a : b))];
}

// A fire hold goes through the same steps for each shooter: draw (if the gun is holstered), aim at a new target, then shoot every cadence.
// G.fire holds the times. Out of range or behind a wall, a shooter waits and says so.
export function beginFire(ent, agents, { lock = false, held = false, follow = false } = {}) {
  ent.hpMax ??= ent.hp ?? (ent.hostile ? HITS.hostile : HITS.other); ent.hp ??= ent.hpMax;
  const s = { ent, agents: [...agents], who: [...agents], lock, held, follow, next: {}, ph: {}, shots: 0, last: '' };
  G.sessions.push(s); G.alert = Math.max(G.alert, 2);
  say(`${ent.hostile ? 'Fire' : 'Firing at a bystander'} · ${names(agents)}${lock ? ' lock' + (agents.length > 1 ? '' : 's') + ' on ' + ent.name : ' at ' + ent.name}${follow && G.followFire === 'with' ? ' · drawn agents within 8 m join' : ''}`, ent.hostile ? 'act' : 'warn');
  return s;
}

// The button came up: a charge that is not full is cancelled; a held fire stops and the pill says Stopped for a second (a lock goes on).
export function releaseHold() {
  const c = G.charge; G.charge = null;
  if (c && !c.done) return say(`${c.way.id === 'fire' ? 'Fire' : c.way.verb} cancelled`, 'warn');
  let n = 0; for (const s of G.sessions) if (s.held) { s.held = false; n++; G.stopped = { ent: s.ent, until: G.clock + 1 }; }
  if (n) say('Cease fire');
}
export function stopAll() { const n = G.sessions.length; if (n) G.stopped = { ent: G.sessions[0].ent, until: G.clock + 1 }; G.sessions = []; G.charge = null; if (n) say('Cease fire · X'); }

const blockedWhy = (a, e) => {
  if (dist(a, e) > G.fire.range) return 'Out of range';
  const n = G.world.nav, ax = a.g.position.x, az = a.g.position.z, ex = e.g.position.x, ez = e.g.position.z, d = Math.hypot(ex - ax, ez - az);
  for (let t = 0.5; t < d - 1.8; t += 0.5) if (n.solid(ax + (ex - ax) * t / d, az + (ez - az) * t / d)) return 'No clear shot';
  return null;
};

export function stepFire(dt, hooks) {
  const c = G.charge;
  if (c && !c.done) { c.t += dt; if (c.t >= G.chargeS) { c.done = true; hooks.charged(c); } }
  const w = G.world, F = G.fire, now = G.clock, live = new Set();
  for (const s of [...G.sessions]) {
    if (down(s.ent) || (!s.lock && !s.held)) { G.sessions.splice(G.sessions.indexOf(s), 1); continue; }
    const who = new Set(s.agents), me = agentOf();
    if (s.follow && s.held && G.followFire === 'with') for (const a of w.agents) if (a !== me && isDrawn(a) && dist(a, me) <= FOLLOW_M) who.add(a);
    s.who = [...who];
    for (const a of who) {
      live.add(a);
      let p = s.ph[a.id];
      if (!p) { const d = isDrawn(a) ? 0 : F.drawS; p = s.ph[a.id] = d > 0 ? { phase: 'draw', until: now + d, total: d } : { phase: 'aim', until: now + F.aimS, total: F.aimS }; }
      a.firePhase = p.phase; a.phaseInfo = p;
      if (p.phase === 'draw') { if (now >= p.until) { a.drawnUntil = now + 8; s.ph[a.id] = { phase: 'aim', until: now + F.aimS, total: F.aimS }; } continue; }
      p.blocked = blockedWhy(a, s.ent);
      if (p.blocked) { p.until += dt; if (s.next[a.id] !== undefined) s.next[a.id] += dt; continue; } // it waits, nothing is lost
      if (p.phase === 'aim') { if (now >= p.until) { p.phase = 'fire'; s.next[a.id] = now; } else continue; }
      if (now < s.next[a.id]) continue;
      s.next[a.id] = now + F.cadence; shoot(a, s, hooks);
      if (down(s.ent)) break;
    }
  }
  for (const a of w.agents) if (!live.has(a)) a.firePhase = null;
  for (const e of w.ents) if (e.flashUntil && G.clock > e.flashUntil) { e.flashUntil = 0; e.body?.material.emissive.setHex(0); }
}

function shoot(a, s, hooks) {
  const e = s.ent, miss = s.shots++ % 4 === 3; // every fourth shot misses: the same every time
  a.shotAt = G.clock; a.drawnUntil = G.clock + 8; s.last = miss ? 'miss' : 'hit';
  if (!a.order) a.g.rotation.y = Math.atan2(e.g.position.x - a.g.position.x, e.g.position.z - a.g.position.z);
  hooks.tracer(a, e, miss);
  if (miss) return;
  e.hp -= 1; e.flashUntil = G.clock + 0.12; e.body?.material.emissive.setHex(0x993333);
  if (!e.hit) { e.hit = true; G.trail += 1; } // Trail per person hit, not per shot
  if (e.hp <= 0) { e.state = 'down'; G.world.raise(e, true); say(`Down · ${e.name}`, 'ok'); G.beat = { ent: e, until: G.clock + 1 }; G.sessions = G.sessions.filter((x) => x.ent !== e); }
}

// The pill, one line, live: where the fire hold is now.
const nm = (e) => { const n = e.name.replace(/^the /, ''); return n[0].toUpperCase() + n.slice(1); };
export function fireStatus() {
  const now = G.clock, b = G.beat;
  if (b && now < b.until) return { ent: b.ent, text: `${nm(b.ent)} down`, agents: [] };
  const s = G.sessions.find((x) => x.held) ?? G.sessions.find((x) => x.lock);
  if (s) {
    const ph = s.who.map((a) => ({ a, p: s.ph[a.id] })).filter((x) => x.p), ok = ph.filter((x) => !x.p.blocked), e = s.ent;
    if (!ph.length) return { ent: e, text: 'Drawing…', agents: s.who };
    if (!ok.length) return { ent: e, text: ph[0].p.blocked, agents: s.who };
    const fire = ok.filter((x) => x.p.phase === 'fire');
    if (fire.length) {
      const eta = Math.ceil(Math.max(0, e.hp - 1) * G.fire.cadence / fire.length + Math.min(...fire.map((x) => Math.max(0, (s.next[x.a.id] ?? 0) - now))));
      return { ent: e, text: `Firing at ${e.name}${s.last ? ' · ' + s.last : ''} · ${e.hpMax - e.hp} of ${e.hpMax} · down in ${eta} s`, agents: fire.map((x) => x.a) };
    }
    const aim = ok.filter((x) => x.p.phase === 'aim');
    return { ent: e, text: aim.length ? `Aiming · ${Math.max(1, Math.ceil(Math.min(...aim.map((x) => x.p.until - now))))} s` : 'Drawing…', agents: (aim.length ? aim : ok).map((x) => x.a) };
  }
  if (G.stopped && now < G.stopped.until) return { ent: G.stopped.ent, text: 'Stopped', agents: [] };
  return null;
}
// How far along the plate's bar is: the draw, the aim, or the wait for the next shot.
export function fireProgress(a) {
  const p = a.phaseInfo;
  if (a.firePhase === 'draw' || a.firePhase === 'aim') return { word: a.firePhase === 'draw' ? 'drawing' : 'aiming', frac: p ? 1 - (p.until - G.clock) / p.total : 0 };
  return { word: 'firing', frac: Math.min(1, (G.clock - (a.shotAt ?? -9)) / G.fire.cadence) };
}
