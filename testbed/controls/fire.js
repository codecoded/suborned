// Hold the left button to fire. A press on a hostile charges the pill (0.4 s), then the shot goes;
// it keeps firing every 1.5 s while the button is held. Releasing early cancels. Hostiles take hits and go down; they don't shoot back.
import { G, say } from './state.js';
import { ACTS, PRESETS, RIGHT_TAP_S } from './data.js';
import { crew, agentOf, names, squadAll, cancel } from './orders.js';

export const FOLLOW_M = 8, HITS = { hostile: 3, other: 2 };
// Syndicate fights (brief 041, Sky 2026-10-21): the first shot needs the hold; once a fight is on a right click focuses fire, drawn agents engage on their own
// (weapons free), and hostiles shoot back for show. Feel numbers only; Motive checks balance before anything reaches the engine.
// A fight lasts while a hostile is engaging (alive, alerted, with contact: someone in range and in sight) or while shots are still going; it ends SETTLE_S after the last of that.
// Contact breaks by killing them all or by moving out of their sight or range (G.fire.range).
export const SETTLE_S = 2, NOTICE_M = 9, RETURN_EVERY = 2.2, NEIGHBOUR_M = 8;
export const syndicate = () => G.fights === 'syndicate';
export const fightOn = () => syndicate() && G.clock < G.fightUntil;
// Under fire: a hostile has shot at this agent in the last few seconds (a hit or a miss) and is still up.
export const underFire = (a) => syndicate() && G.clock < (a.underFireUntil ?? 0) && !!a.fireFrom && !['out', 'dead', 'down'].includes(a.fireFrom.state);
export const isDrawn = (a) => G.clock < (a.drawnUntil ?? 0);
export const firingNow = (a) => !!a.firePhase || G.clock - (a.shotAt ?? -99) < 0.7;
const dist = (a, b) => Math.hypot(a.g.position.x - b.g.position.x, a.g.position.z - b.g.position.z);
const down = (e) => ['out', 'dead', 'down'].includes(e.state);
const pinned = (a) => G.sessions.some((s) => s.pin && s.agents.includes(a));
export const hpOf = (e) => ({ hp: e.hp ?? e.hpMax ?? (e.hostile ? HITS.hostile : HITS.other), max: e.hpMax ?? e.hp ?? (e.hostile ? HITS.hostile : HITS.other) });
const initHp = (e) => { e.hpMax ??= e.hp ?? (e.hostile ? HITS.hostile : HITS.other); e.hp ??= e.hpMax; };
const stripAgents = (list, keepPinned = true) => { for (const s of [...G.sessions]) { if (keepPinned && s.pin) continue; s.agents = s.agents.filter((a) => !list.includes(a)); if (!s.agents.length) G.sessions.splice(G.sessions.indexOf(s), 1); } };

// A hostile who is shot at, or sees a drawn agent close by, opens up; the fight clock runs from every shot fired or taken.
export function alertHostile(e) {
  if (!e.hostile || down(e)) return;
  if (!e.alerted) { e.alerted = true; e.nextShot = G.clock + 0.9 + (e.id.length % 3) * 0.3; }
  G.fightUntil = Math.max(G.fightUntil, G.clock + SETTLE_S);
}

// What a hold on this thing fires: any person who is not in the cell (a hostile or a bystander). null = nothing to fire at.
export function holdFor(ent) {
  if (!ent || ent.agent || !ent.person || down(ent) || !ent.acts?.length) return null;
  return { id: 'fire', ...ACTS.fire };
}
// How full the pill is: a right-hold charge starts filling after the tap window, a left-hold one from the press.
export const chargeFrac = (c) => Math.max(0, Math.min(1, c.right ? (c.t - RIGHT_TAP_S) / (G.chargeS - RIGHT_TAP_S) : c.t / G.chargeS));
// A calm left-hold act waits for the button to come up (hold preset, 'commit on release'); fire never does.
// Fire from a right hold does too (default preset): full pill, let go, and the agent fires on its own until the target is down, X or ×, or a new order.
export const commitsOnRelease = (c) => G.actCommit === 'release' && !c.ring && !!PRESETS[G.preset]?.holdActs && (c.way.id === 'fire' ? !!c.right && !!PRESETS[G.preset]?.rightFires && G.tpAim !== 'aim' : !c.right);
export function startCharge(ent, way, ring = false, run = false) { G.charge = { ent, way, t: 0, done: false, ring, run, label: way.id === 'fire' ? 'fire' : way.verb.toLowerCase(), bystander: !ent.hostile && way.id === 'fire' }; }

// Who shoots: in third person the driven agent (the others join by the follow setting); on the map the squad rule: best placed or everyone.
export function shooters(ent, force = false) {
  if (G.view === 'tp') return [agentOf()];
  const able = crew().filter((a) => ACTS.fire.by.includes(a.role));
  if (force) { const drawn = able.filter(isDrawn); return drawn.length ? drawn : able; } // right hold: every selected drawn agent
  return squadAll() || able.length < 2 ? able : [able.reduce((b, a) => (dist(a, ent) < dist(b, ent) ? a : b))];
}

// A fire hold goes through the same steps for each shooter: draw (if the gun is holstered), aim at a new target, then shoot every cadence.
// G.fire holds the times. Out of range or behind a wall, a shooter waits and says so.
export function beginFire(ent, agents, { lock = false, held = false, follow = false, pin = false, focus = false } = {}) {
  initHp(ent);
  if (lock) for (const a of agents) if (a.order) cancel(a, true, true); // a fire order is a new order: it replaces what the agent was doing
  if (pin || focus) stripAgents(agents, !pin); // a portrait lock takes the agent from anything else; a focus click leaves portrait locks alone
  const s = { ent, agents: [...agents], who: [...agents], lock, held, follow, pin, focus, next: {}, ph: {}, shots: 0, last: '' };
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
export function stopAll() { G.fightUntil = 0; for (const e of G.world?.ents ?? []) e.alerted = false; const n = G.sessions.length; if (n) G.stopped = { ent: G.sessions[0].ent, until: G.clock + 1 }; G.sessions = []; G.charge = null; if (n) say('Cease fire · X'); }

const clearShot = (a, e) => {
  const n = G.world.nav, ax = a.g.position.x, az = a.g.position.z, ex = e.g.position.x, ez = e.g.position.z, d = Math.hypot(ex - ax, ez - az);
  for (let t = 0.5; t < d - 1.8; t += 0.5) if (n.solid(ax + (ex - ax) * t / d, az + (ez - az) * t / d)) return false;
  return true;
};
const blockedWhy = (a, e) => (dist(a, e) > G.fire.range ? 'Too far' : clearShot(a, e) ? null : 'No clear line');

// Syndicate: a right click on a hostile in a fight. Every selected drawn agent (portrait locks stay) fires at it at once, until it is down.
export function focusFire(ent, agents) {
  const list = agents.filter((a) => !pinned(a)); if (!list.length) return null;
  return beginFire(ent, list, { lock: true, focus: true });
}
// Weapons free: in a fight, a drawn agent with no order and no target of its own picks one. Nearest threat first, whoever is shooting at it before that.
let autoAt = 0;
function joinAuto(ent, a) {
  initHp(ent);
  let s = G.sessions.find((x) => x.auto && x.ent === ent);
  if (!s) { s = { ent, agents: [], who: [], lock: true, held: false, follow: false, auto: true, next: {}, ph: {}, shots: 0, last: '' }; G.sessions.push(s); }
  s.agents.push(a);
}
function autoEngage() {
  if (!(fightOn() && G.free)) { if (G.sessions.some((s) => s.auto)) G.sessions = G.sessions.filter((s) => !s.auto); return; }
  if (G.clock < autoAt) return; autoAt = G.clock + 0.25;
  const w = G.world, hostiles = w.ents.filter((e) => e.hostile && !down(e));
  for (const a of w.agents) {
    if (!isDrawn(a) || a.order || pinned(a)) continue;
    const cur = G.sessions.find((s) => s.agents.includes(a));
    if (cur && !cur.auto) continue; // it has a target of its own (a focus click, a hold)
    const cands = hostiles.filter((e) => dist(a, e) <= G.fire.range && clearShot(a, e)), score = (e) => dist(a, e) - (e.targeting === a ? 100 : 0);
    const best = cands.length ? cands.reduce((b, e) => (score(e) < score(b) ? e : b)) : null;
    if (cur) {
      if (cands.includes(cur.ent) && !(best && best !== cur.ent && best.targeting === a && cur.ent.targeting !== a)) continue;
      stripAgents([a], false);
    }
    if (best) joinAuto(best, a);
  }
}
// The fakes shoot back, for show: cosmetic only, nobody is hurt. A hostile opens up when shot at or when a drawn agent is close; it aims at whoever is shooting at it, else the nearest.
function stepHostiles(hooks) {
  const w = G.world, now = G.clock;
  for (const e of w.ents) {
    if (!e.hostile || down(e)) continue;
    if (!e.alerted) { if (w.agents.some((a) => isDrawn(a) && dist(a, e) <= NOTICE_M && clearShot(a, e))) alertHostile(e); continue; }
    const cands = w.agents.filter((a) => dist(a, e) <= G.fire.range && clearShot(a, e));
    if (cands.length) G.fightUntil = Math.max(G.fightUntil, now + SETTLE_S); // contact: the fight stays on
    else if (now > G.fightUntil) { e.alerted = false; e.targeting = null; continue; } // contact broken and the settle is over: they stand down
    if (!cands.length) { e.targeting = null; continue; }
    const mine = cands.filter((a) => G.sessions.some((s) => s.ent === e && s.agents.includes(a))), pool = mine.length ? mine : cands;
    const t = e.targeting = pool.reduce((b, a) => (dist(a, e) < dist(b, e) ? a : b));
    if (now >= (e.nextShot ?? 0)) {
      e.volley = (e.volley ?? 0) + 1; e.nextShot = now + RETURN_EVERY + (e.volley % 2) * 0.4;
      e.g.rotation.y = Math.atan2(t.g.position.x - e.g.position.x, t.g.position.z - e.g.position.z);
      t.fireFrom = e; t.underFireUntil = now + RETURN_EVERY + 1; t.incomingAt = now;
      hooks.hostileShot(e, t, e.volley % 3 === 0); G.alert = Math.max(G.alert, 2); G.fightUntil = Math.max(G.fightUntil, now + SETTLE_S);
    }
  }
}

export function stepFire(dt, hooks) {
  if (syndicate()) { stepHostiles(hooks); autoEngage(); }
  const c = G.charge;
  if (c && !c.done) { c.t += dt; if (c.t >= G.chargeS) { if (commitsOnRelease(c)) c.ready = true; else { c.done = true; hooks.charged(c); } } }
  const w = G.world, F = G.fire, now = G.clock, live = new Set();
  for (const s of [...G.sessions]) {
    if (down(s.ent) || (!s.lock && !s.held)) { G.sessions.splice(G.sessions.indexOf(s), 1); continue; }
    // A lasting fire order goes on through walking and running; it ends when the target is down, on X or a new act, or when the agent is out of weapon range (it says "Too far" for a moment).
    if (s.lock && !s.held && !s.auto) {
      const far = s.agents.filter((a) => dist(a, s.ent) > F.range);
      if (far.length) { for (const a of far) a.tooFarUntil = now + 1.5; G.tooFar = { ent: s.ent, until: now + 1.5 }; s.agents = s.agents.filter((a) => !far.includes(a)); say(`Too far · ${names(far)} stop firing at ${s.ent.name}`, 'warn'); if (!s.agents.length) { G.sessions.splice(G.sessions.indexOf(s), 1); continue; } }
    }
    const who = new Set(s.agents), me = agentOf();
    if (s.follow && s.held && G.followFire === 'with') for (const a of w.agents) if (a !== me && isDrawn(a) && dist(a, me) <= FOLLOW_M) who.add(a);
    s.who = [...who];
    for (const a of who) {
      live.add(a);
      let p = s.ph[a.id];
      if (!p) { const d = isDrawn(a) ? 0 : F.drawS; p = s.ph[a.id] = d > 0 ? { phase: 'draw', until: now + d, total: d } : { phase: 'aim', until: now + F.aimS, total: F.aimS }; }
      a.firePhase = p.phase; a.phaseInfo = p;
      if (a.order && p.phase !== 'draw' && !p.blocked) a.g.rotation.y = Math.atan2(s.ent.g.position.x - a.g.position.x, s.ent.g.position.z - a.g.position.z); // fire on the move: it keeps aiming at the target while it walks
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
  a.g.rotation.y = Math.atan2(e.g.position.x - a.g.position.x, e.g.position.z - a.g.position.z);
  hooks.tracer(a, e, miss);
  G.fightUntil = Math.max(G.fightUntil, G.clock + SETTLE_S); G.alert = Math.max(G.alert, 2);
  if (syndicate()) { alertHostile(e); for (const o of G.world.ents) if (o !== e && o.hostile && dist(o, e) <= NEIGHBOUR_M) alertHostile(o); }
  if (miss) return;
  e.hp -= 1; e.hitAt = G.clock; e.flashUntil = G.clock + 0.14; e.body?.material.emissive.setHex(0xcc4444);
  if (!e.hit) { e.hit = true; G.trail += 1; } // Trail per person hit, not per shot
  if (e.hp <= 0) { e.state = 'down'; G.world.raise(e, true); say(`Down · ${e.name}`, 'ok'); G.beat = { ent: e, until: G.clock + 1 }; G.sessions = G.sessions.filter((x) => x.ent !== e); }
}

// The pill, one line, live: where the fire hold is now.
const nm = (e) => { const n = e.name.replace(/^the /, ''); return n[0].toUpperCase() + n.slice(1); };
export function fireStatus() {
  const now = G.clock, b = G.beat;
  if (b && now < b.until) return { ent: b.ent, text: `${nm(b.ent)} down`, agents: [] };
  const autos = G.sessions.filter((x) => x.auto && x.agents.length).sort((p, q) => q.agents.length - p.agents.length);
  const s = G.sessions.find((x) => x.held) ?? G.sessions.find((x) => x.lock && !x.auto && x.agents.length) ?? autos[0];
  if (s) {
    const ph = s.who.map((a) => ({ a, p: s.ph[a.id] })).filter((x) => x.p), ok = ph.filter((x) => !x.p.blocked), e = s.ent;
    if (!ph.length) return { ent: e, text: 'Drawing…', agents: s.who };
    if (!ok.length) return { ent: e, text: ph[0].p.blocked, agents: s.who };
    if (fightOn() || ok.some((x) => x.p.phase === 'fire')) { // the pill is one quiet line: who they are firing at and how long it takes; hits and misses show on the tracer and the bar
      const wait = Math.min(...ok.map((x) => (x.p.phase === 'fire' ? Math.max(0, (s.next[x.a.id] ?? 0) - now) : Math.max(0, x.p.until - now) + (x.p.phase === 'draw' ? G.fire.aimS : 0))));
      return { ent: e, text: `Firing at ${e.name} · down in ${Math.ceil(Math.max(0, e.hp - 1) * G.fire.cadence / ok.length + wait)} s`, agents: ok.map((x) => x.a) };
    }
    const fire = ok.filter((x) => x.p.phase === 'fire');
    if (fire.length) {
      const eta = Math.ceil(Math.max(0, e.hp - 1) * G.fire.cadence / fire.length + Math.min(...fire.map((x) => Math.max(0, (s.next[x.a.id] ?? 0) - now))));
      return { ent: e, text: `Firing at ${e.name}${s.last ? ' · ' + s.last : ''} · ${e.hpMax - e.hp} of ${e.hpMax} · down in ${eta} s`, agents: fire.map((x) => x.a) };
    }
    const aim = ok.filter((x) => x.p.phase === 'aim');
    return { ent: e, text: aim.length ? `Aiming · ${Math.max(1, Math.ceil(Math.min(...aim.map((x) => x.p.until - now))))} s` : 'Drawing…', agents: (aim.length ? aim : ok).map((x) => x.a) };
  }
  if (G.tooFar && now < G.tooFar.until) return { ent: G.tooFar.ent, text: 'Too far', agents: [] };
  if (G.stopped && now < G.stopped.until) return { ent: G.stopped.ent, text: 'Stopped', agents: [] };
  return null;
}
// How far along the plate's bar is: the draw, the aim, or the wait for the next shot.
export function fireProgress(a) {
  const p = a.phaseInfo;
  if (a.firePhase === 'draw' || a.firePhase === 'aim') return { word: a.firePhase === 'draw' ? 'drawing' : 'aiming', frac: p ? 1 - (p.until - G.clock) / p.total : 0 };
  return { word: 'firing', frac: Math.min(1, (G.clock - (a.shotAt ?? -9)) / G.fire.cadence) };
}
