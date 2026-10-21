// The controls playground: wires the scene, the camera, the presets and the pointer together.
import * as THREE from './vendor/three.module.min.js';
import { G, say } from './state.js';
import { PRESETS, ACTS, WALK, RUN, ROLES, RIGHT_TAP_S, priceLine, phrase } from './data.js';
import { buildWorld } from './world.js';
import { Rig } from './camera.js';
import { Marker, groundRing, facesHtml } from './markers.js';
import { initRing, openRing, closeRing, ringPointer, placeRing, ringCharge, draw as drawRing } from './ring.js';
import { agentOf, crew, selected, names, waysFor, defaultFor, planFor, walkPath, walkTo, groupWalk, giveAct, cancel, step, reachOf, flushPlan } from './orders.js';
import { buildChrome, refresh, setHint, setFps } from './ui.js';
import { makeCones } from './cones.js';
import { holdFor, startCharge, beginFire, releaseHold, stopAll, stepFire, shooters, isDrawn, FOLLOW_M } from './fire.js';
import { liveStatus } from './status.js';

const $ = (id) => document.getElementById(id);
window.addEventListener('error', (e) => { $('errors').textContent += `${e.message} (${e.filename?.split('/').pop()}:${e.lineno})\n`; });
window.addEventListener('unhandledrejection', (e) => { $('errors').textContent += `${e.reason}\n`; });

const canvas = $('c'), renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
const scene = new THREE.Scene(); scene.background = new THREE.Color(0xcfe8ef);
scene.add(new THREE.HemisphereLight(0xffffff, 0xb8a98f, 1.5)); const sun = new THREE.DirectionalLight(0xfff1d6, 2.2); sun.position.set(-10, 24, 8); scene.add(sun);
const camera = new THREE.PerspectiveCamera(30, 1, 0.1, 300), rig = new Rig(camera); G.rig = rig;
const marker = new Marker(scene, $('marker')), moveRings = [0, 1, 2, 3].map(() => groundRing(scene)), selRings = [0, 1, 2, 3].map(() => groundRing(scene)), runRings = [0, 1, 2, 3].map(() => groundRing(scene, true)), runCores = [0, 1, 2, 3].map(() => groundRing(scene));
runRings.forEach((m, i) => m.material.color.set(ROLES[i].color)); runCores.forEach((m, i) => m.material.color.set(ROLES[i].color));
moveRings.forEach((m, i) => m.material.color.set(ROLES[i].color)); selRings.forEach((m, i) => m.material.color.set(ROLES[i].color));
const breakLines = [0, 1, 2, 3].map(() => { const l = new THREE.Line(new THREE.BufferGeometry(), new THREE.LineDashedMaterial({ dashSize: 0.5, gapSize: 0.25, depthTest: false, transparent: true })); l.renderOrder = 11; l.frustumCulled = false; l.visible = false; scene.add(l); return l; });
const V = new THREE.Vector3(), ray = new THREE.Raycaster(), tracers = [];
const preset = () => PRESETS[G.preset];

function newWorld() { G.world?.dispose(); G.world = buildWorld(scene); G.trail = 0; G.alert = 0; G.cash = 100000; G.selTarget = null; G.group = [true, false, false, false]; G.sel = 0; G.carry = null; G.world.picks = G.world.ents.flatMap((e) => e.pick.map((m) => (m.userData.ent = e, m))); G.world.cones = makeCones(G.world); setCrowd(G.crowd); if (rig.mode === 'cmd') rig.focus.set(2, 0, 4.5); G.plan.clear(); G.paused = false; G.vale = null; G.sessions = []; G.charge = null; G.beat = G.stopped = G.ordered = G.doneInfo = G.stoppedInfo = null; closeRing(); }
newWorld();
function resize() { renderer.setSize(innerWidth, innerHeight); camera.aspect = innerWidth / innerHeight; camera.updateProjectionMatrix(); }
addEventListener('resize', resize); resize();

const project = (x, y, z) => { V.set(x, y, z).project(camera); return V.z > 1 ? null : { x: (V.x * 0.5 + 0.5) * innerWidth, y: (-V.y * 0.5 + 0.5) * innerHeight }; };
// Everyone under the cursor, nearest first. The one you are on stays (a little hysteresis), Tab steps through the overlap.
function entsAt(cx, cy) {
  ray.setFromCamera({ x: cx / innerWidth * 2 - 1, y: -(cy / innerHeight) * 2 + 1 }, camera);
  const out = []; for (const h of ray.intersectObjects(G.world.picks, false)) { const e = h.object.userData.ent; if (!out.includes(e)) out.push(e); } return out;
}
function pick(cx, cy) {
  const hits = entsAt(cx, cy), g = new THREE.Vector3();
  ray.ray.intersectPlane(new THREE.Plane(new THREE.Vector3(0, 1, 0), 0), g);
  const keep = [G.tabEnt, G.stick].find((e) => e && hits.includes(e));
  return { ent: keep ?? hits[0] ?? null, point: g, hits };
}
// "Click walks, hold acts": the left button never harms, so a hostile's main act is its first calm one (or none).
const mainFor = (e) => (preset().holdActs ? waysFor(e).find((w) => w.kind !== 'harm') ?? null : defaultFor(e));
const actionable = (e) => !!e && !e.agent && !!mainFor(e);
// Right hold is force: a person or a car. Third person without a target in the cursor takes the nearest hostile ahead.
const forceWay = (e) => (e && !e.agent && (e.person || e.kind === 'car') && !['out', 'dead', 'down'].includes(e.state) ? { id: 'fire', ...ACTS.fire } : null);
function hostileAhead() {
  const w = G.world, me = agentOf(); let best = 25, hit = null;
  for (const e of w.ents) { if (!e.hostile || ['out', 'dead', 'down'].includes(e.state)) continue; const dx = e.g.position.x - me.g.position.x, dz = e.g.position.z - me.g.position.z, d = Math.hypot(dx, dz); if (d < best && Math.abs(((Math.atan2(dx, dz) - rig.f + 3 * Math.PI) % (2 * Math.PI)) - Math.PI) < 0.9) { best = d; hit = e; } }
  return hit;
}
// where an agent stands to do a thing: the end of its walk to the act's reach
function standSpot(e) {
  const w = mainFor(e), plan = w && planFor(e, w); if (!plan?.lead) return null;
  const a = plan.lead, path = G.world.nav.path(a.g.position.x, a.g.position.z, e.g.position.x, e.g.position.z, reachOf(e, w, a));
  return path?.length ? path[path.length - 1] : null;
}

// ---- what a click does ------------------------------------------------------------------
const runHeld = () => G.keys.has('ShiftLeft') || G.keys.has('ShiftRight');
function doAct(e, w, how, run = false, agents = crew()) {
  const plan = planFor(e, w, agents, run);
  if (!plan.agents.length) return say(`${how} · nobody can ${w.verb.toLowerCase()} ${e.name}`, 'warn');
  if (!isFinite(plan.walkS)) return say(`${how} · no way to ${e.name}`, 'warn');
  const doing = plan.agents.filter((a) => giveAct(a, e, w, run));
  if (!doing.length) return;
  const squad = agents.length > 1, all = doing.length === agents.length, many = doing.length > 1, subj = squad && all ? 'The squad' : names(doing), rn = plan.running, runs = rn && plan.walkS > 0.5;
  const line = priceLine(w, e, plan.walkS, true, '', rn).slice(1).join(' · ') + (runs ? ' · people notice' : '') + (w.fire && (run || doing.some((a) => a.speed > 2)) ? ' · running: less accurate' : '');
  G.ordered = { ent: e, until: G.clock + 0.8 };
  if (squad) G.peel = { until: G.clock + 1.2, lead: doing, holds: plan.holds };
  const head = runs ? `${subj} ${many || (squad && all) ? 'run' : 'runs'} to ${phrase(w, e)}` : squad ? `${all ? 'The squad' : subj + (many ? ' break' : ' breaks') + ' away to'} ${phrase(w, e)}` : `${subj}: ${priceLine(w, e, plan.walkS, true, '', rn)[0]}`;
  const q = G.paused && G.planMode === 'sync' ? ' · waits for Space' : '';
  say(`${how} · ${head} · ${line}${plan.holds.length ? ` · ${names(plan.holds)} hold` : ''}${q}`, 'act');
}
// A second double click on what an agent is already walking to turns the walk into a run (the act is not restarted).
function upgradeRun(e, point, how) {
  let n = 0, did = [];
  for (const a of crew()) {
    const o = a.order; if (!o || o.run) continue;
    if (e ? o.kind === 'act' && o.ent === e : o.kind === 'walk' && Math.hypot(o.dest.x - point.x, o.dest.z - point.z) < 2.5) { n++; if (o.phase !== 'act') { o.run = true; did.push(a); } }
  }
  if (did.length) say(`${how} · ${names(did)} ${did.length > 1 ? 'run' : 'runs'} to ${e ? phrase(did[0].order.act, e) : 'the spot'} · people notice`, 'act');
  return n > 0;
}
// ---- selection: 1 to 4 pick one, Shift or Ctrl adds or removes, 5 / Ctrl+A / Squad / a drag box pick everyone
let lastKey = { i: -1, t: 0 }, dragEnded = false;
function setGroup(flags, say_) {
  G.group = flags; const p = flags.indexOf(true), changed = p !== G.sel; G.sel = p; closeRing();
  if (changed && G.view === 'tp') rig.refocus(agentOf());
  if (say_) say(selected().length > 1 ? `Squad: ${names(selected())}` : `Selected the ${agentOf().name}`);
}
function selectAgent(i, add = false) {
  if (dragEnded) return;
  const now = performance.now(), twice = lastKey.i === i && now - lastKey.t < 350 && !add; lastKey = { i, t: now };
  const f = add ? G.group.map((v, k) => (k === i ? !v : v)) : G.group.map((_, k) => k === i);
  setGroup(f.some(Boolean) ? f : G.group.map((_, k) => k === i), true);
  if (twice && G.view === 'cmd') { rig.centreOn(G.world.agents[i].g.position); say(`Centred on the ${G.world.agents[i].name}`); } // a double tap centres, as in the game
}
const selectAll = () => setGroup([true, true, true, true], true);
function boxSelect(r, add) { // agents whose feet are inside the dragged box
  const hit = G.world.agents.map((a) => { const p = project(a.g.position.x, 0, a.g.position.z); return !!p && p.x >= r.x0 && p.x <= r.x1 && p.y >= r.y0 && p.y <= r.y1; });
  if (!hit.some(Boolean)) return add ? 0 : say('Box · nobody in it', 'warn');
  setGroup(add ? G.group.map((v, k) => v || hit[k]) : hit, false); say(`Box · ${selected().length > 1 ? 'Squad: ' + names(selected()) : 'Selected the ' + agentOf().name}`);
}
function nearMiss(p) { for (const e of G.world.ents) if (!e.agent && waysFor(e).length && Math.hypot(e.g.position.x - p.x, e.g.position.z - p.z) - e.r < 1.1) return ` (just beside ${e.name})`; return ''; }
function groundGo(p, how, run, agents = crew()) {
  const secs = groupWalk(agents, p, run).filter((s) => s !== null);
  if (secs.length) say(`${how} · ${names(agents)} ${run ? 'run' : 'walk'} · ${Math.round(Math.max(...secs))} s${nearMiss(p)}`, nearMiss(p) ? 'warn' : '');
}
function leftClick(hit, dbl, shift) {
  const P = preset(), e = hit.ent;
  const how = dbl ? 'Double click' : 'Left click';
  if (G.ring) { closeRing(); return say(`${how} · closed the ring`); }
  if (dbl && !e?.agent && upgradeRun(e, hit.point, how)) return;
  if (e && !e.agent && P.thingWalk) { // click walks to where the act would be done, and does nothing; double click runs
    G.selTarget = null; const spot = standSpot(e);
    if (!spot) return say(`${how} · already in reach of ${e.name}`);
    const pt = { x: spot[0], z: spot[1] }; if (dbl && upgradeRun(null, pt, how)) return;
    return groundGo(pt, how, dbl || shift);
  }
  if (e?.agent) { if (P.agentClick === 'none') return say(`${how} · pick agents on the portraits or with 1 to 4`); G.selTarget = null; return selectAgent(G.world.agents.indexOf(e), shift); }
  if (e) {
    const w = defaultFor(e);
    if (w?.id === 'fire') return say(`${how} · hold the left button to fire`, 'warn');
    if (!w) return waysFor(e).length ? openRing(e, waysFor(e), P.ring) : say(`${how} · ${names(crew())} can't do anything to ${e.name}`, 'warn');
    const mode = dbl ? P.dbl.thing : P.left.thing;
    if (mode === 'act' || mode === 'run') { G.selTarget = null; return doAct(e, w, how, shift || mode === 'run'); }
    if (!dbl) { G.selTarget = e; return say(`${how} · selected ${e.name}`); }
    return;
  }
  G.selTarget = null;
  if (dbl) return G.runMode === 'dbl' || P.dbl.ground === 'run' ? groundGo(hit.point, how, true) : undefined; // a double click on open ground runs there (or Shift + click does, by the setting)
  groundGo(hit.point, how, shift && (G.runMode === 'shift' || P.shiftRuns));
}
function rightTap(hit) {
  const P = preset(), e = hit.ent;
  if (G.ring) { // a second right click
    if (P.right === 'press' && !(e && e !== G.ring.ent)) return takeWay(G.ring.shown[G.ring.lit]);
    closeRing(); if (!e) return say('Right click · closed the ring');
  }
  if (e && !e.agent && waysFor(e).length) return openRing(e, waysFor(e), P.ring);
  if (crew().map((a) => cancel(a)).some(Boolean)) say('Right click · stopped'); else say('Right click · nothing there');
}
// Shift on a wedge runs there; Alt flips the squad setting for that one order (not in the hold preset: there is no such setting).
function takeWay(w, m = {}) {
  if (w.kind === 'harm' && (w.id === 'fire' || G.harmHold === 'all') && !m.held) { startCharge(G.ring.ent, w, true); return; } // a harmful wedge is held, not clicked (the ring stays open while it fills)
  const e = G.ring.ent, was = G.squadAct, run = !!(m.shift || runHeld()); if (m.alt && !preset().holdActs) G.squadAct = was === 'best' ? 'all' : 'best'; closeRing(); doAct(e, w, 'Ring', run); G.squadAct = was; }
initRing($('ring'), takeWay);

// ---- the pointer --------------------------------------------------------------------------
let down = null, lastClick = { t: 0, x: 0, y: 0, ent: null };
canvas.addEventListener('contextmenu', (e) => e.preventDefault());
canvas.addEventListener('pointerdown', (e) => {
  if (e.button === 1) e.preventDefault(); canvas.setPointerCapture(e.pointerId); G.ptr.x = e.clientX; G.ptr.y = e.clientY;
  down = { b: e.button, x: e.clientX, y: e.clientY, t: performance.now(), hit: pick(e.clientX, e.clientY), drag: false, ring: false };
  if (e.button === 0 && !G.ring) { // hold the left button on a person to fire; a quick click is still a click
    const P = preset(), t = P.holdActs ? down.hit.ent : holdFor(down.hit.ent) ? down.hit.ent : G.aim ? G.aimT : null, way = P.holdActs ? (t && !t.agent ? mainFor(t) : null) : holdFor(t); // "hold acts": the hold is the main calm act
    if (way) { down.charge = true; startCharge(t, way, false, e.shiftKey); }
  }
  if (e.button === 2 && preset().rightFires && !G.ring && !(G.view === 'tp' && G.tpAim === 'aim')) { // right hold fires: same charge, the pill says so
    const t = forceWay(down.hit.ent) ? down.hit.ent : G.view === 'tp' ? hostileAhead() : null, way = forceWay(t);
    if (way) { down.rcharge = true; startCharge(t, way); G.charge.right = true; G.charge.extra = `down in ${ACTS.fire.t} s`; }
  }
  if (e.button === 2 && preset().right === 'press') { // 0.2.34: the ring opens on press
    const h = down.hit; down.ring = true;
    if (G.ring) rightTap(h); else if (h.ent && !h.ent.agent && waysFor(h.ent).length) openRing(h.ent, waysFor(h.ent), preset().ring); else rightTap(h);
  }
});
// Aim (right button held, third person) then the left button: the browser sends the second button as a mouse event, not a pointer event.
let chord = false;
canvas.addEventListener('mousedown', (e) => { if (e.button === 0 && G.aim && !G.charge && !(preset().rightFires && G.tpAim === 'fire')) { const way = holdFor(G.aimT); if (way) { startCharge(G.aimT, way); chord = true; } } });
canvas.addEventListener('mouseup', (e) => { if (e.button === 0 && chord) { chord = false; releaseHold(); } });
canvas.addEventListener('pointermove', (e) => {
  G.ptr.x = e.clientX; G.ptr.y = e.clientY; if (!down) return;
  if (!down.drag && Math.hypot(e.clientX - down.x, e.clientY - down.y) > 6) down.drag = true;
  if (down.drag && (down.b === 0 || down.b === 1) && G.view === 'tp') rig.orbit(e.movementX, e.movementY);
  if (down.b === 1 && G.view === 'cmd') rig.pan(e.movementX, e.movementY);
  if (down.drag && down.b === 0 && G.view === 'cmd') { // a drag box on the ground picks agents
    const r = { x0: Math.min(down.x, e.clientX), x1: Math.max(down.x, e.clientX), y0: Math.min(down.y, e.clientY), y1: Math.max(down.y, e.clientY) }; G.box = r;
    Object.assign($('box').style, { display: 'block', left: r.x0 + 'px', top: r.y0 + 'px', width: r.x1 - r.x0 + 'px', height: r.y1 - r.y0 + 'px' });
  }
});
canvas.addEventListener('pointerup', (e) => {
  const d = down; down = null; G.holdEnt = null; if (!d) return;
  const now = performance.now(), hold = (now - d.t) / 1000;
  if (d.rcharge) { const c = G.charge; if (c && !c.done && (performance.now() - d.t) / 1000 < RIGHT_TAP_S) G.charge = null; /* a tap: the ring, silently */ else return releaseHold(); }
  if (d.charge) { const c = G.charge; if (c && !c.done && c.t < 0.2 && !c.ent.hostile) G.charge = null; /* a quick click on a bystander is its normal click */ else return releaseHold(); }
  if (G.aim && d.b === 2) { G.aim = false; return; }
  if (d.drag && d.b === 0 && G.box) { boxSelect(G.box, e.shiftKey || e.ctrlKey || e.metaKey); G.box = null; $('box').style.display = 'none'; }
  if (d.drag || d.ring) return;
  const hit = pick(e.clientX, e.clientY);
  if (d.b === 0) {
    const dbl = now - lastClick.t < 350 && (hit.ent ? lastClick.ent === hit.ent : !lastClick.ent && Math.hypot(e.clientX - lastClick.x, e.clientY - lastClick.y) < 24);
    lastClick = dbl ? { t: 0, x: 0, y: 0, ent: null } : { t: now, x: e.clientX, y: e.clientY, ent: hit.ent };
    leftClick(hit, dbl, e.shiftKey);
  } else if (d.b === 2) {
    if (hold >= RIGHT_TAP_S && G.view === 'tp') return; else if (hold >= RIGHT_TAP_S) say('Right hold · nothing happens', 'warn'); else rightTap(hit);
  }
});
addEventListener('pointermove', (e) => { if (G.ring) ringPointer(e, false); });
addEventListener('pointerup', () => { G.holdEnt = null; if (G.charge?.ring) releaseHold(); });
$('ring').addEventListener('pointerdown', (e) => { e.preventDefault(); ringPointer(e, true); });
$('ring').addEventListener('contextmenu', (e) => e.preventDefault());
canvas.addEventListener('pointerenter', () => (G.ptr.over = true)); canvas.addEventListener('pointerleave', () => (G.ptr.over = false));
canvas.addEventListener('wheel', (e) => { e.preventDefault(); rig.zoom(e.deltaY); }, { passive: false });

// ---- drag a portrait onto a target: that agent (or the whole selection, if it is selected) gets the order
let pd = null;
document.addEventListener('pointerdown', (e) => { const b = e.target.closest?.('[data-agent]'); if (b && e.button === 0) pd = { i: +b.dataset.agent, x: e.clientX, y: e.clientY, drag: false }; });
addEventListener('pointermove', (e) => {
  if (!pd) return;
  if (!pd.drag && Math.hypot(e.clientX - pd.x, e.clientY - pd.y) > 6) { pd.drag = true; G.carry = G.group[pd.i] ? selected() : [G.world.agents[pd.i]]; $('rubber').style.display = 'block'; $('ghost').style.display = 'block'; }
  if (!pd.drag) return;
  G.slowUntil = performance.now() + 500; G.ptr.x = e.clientX; G.ptr.y = e.clientY; G.ptr.over = document.elementFromPoint(e.clientX, e.clientY) === canvas;
  const r = document.querySelector(`[data-agent="${pd.i}"]`).getBoundingClientRect(), ls = $('rubber').querySelectorAll('line');
  ls.forEach((l) => { l.setAttribute('x1', r.x + r.width / 2); l.setAttribute('y1', r.y); l.setAttribute('x2', e.clientX); l.setAttribute('y2', e.clientY); });
  Object.assign($('ghost').style, { left: e.clientX + 'px', top: e.clientY + 'px' }); $('ghost').textContent = names(G.carry);
});
addEventListener('pointerup', (e) => {
  const d = pd; pd = null; if (!d?.drag) return;
  const carried = G.carry; G.carry = null; $('rubber').style.display = $('ghost').style.display = 'none'; dragEnded = true; setTimeout(() => (dragEnded = false), 0); G.slowUntil = performance.now() + 500;
  if (document.elementFromPoint(e.clientX, e.clientY) !== canvas) return say('Portrait drop · cancelled');
  const hit = pick(e.clientX, e.clientY), who = names(carried), x = hit.ent;
  if (!x) return groundGo(hit.point, 'Portrait drop', e.shiftKey, carried);
  if (x.agent) return say('Portrait drop · cancelled');
  if (x.hostile) return beginFire(x, carried, { lock: true });
  G.carry = carried; const w = defaultFor(x), any = waysFor(x).length; G.carry = null;
  if (w) return doAct(x, w, 'Portrait drop', e.shiftKey, carried);
  if (any) { setGroup(G.world.agents.map((a) => carried.includes(a)), false); openRing(x, waysFor(x), preset().ring); return say(`Portrait drop · ring for ${who}`); }
  say(`Portrait drop · ${who} can't do anything to ${x.name}`, 'warn');
});

// ---- keys ---------------------------------------------------------------------------------
let eDown = null;
function promptTarget() { return G.hover ?? G.prompt; }
function eAct() { const e = promptTarget(); if (!e) return; const w = defaultFor(e); if (w?.id === 'fire') return say('E · hold the left button to fire', 'warn'); if (w) doAct(e, w, 'E', runHeld()); }
function setView(v) {
  if (v === G.view) return; closeRing(); rig.setMode(v, agentOf()); G.view = v; G.tpAt = G.clock; G.selTarget = null;
  say(v === 'tp' ? `Third person · the ${agentOf().name}${selected().length > 1 ? ' leads' : ''}` : 'Commander view'); drawChrome?.();
}
addEventListener('keydown', (e) => {
  if (e.code.startsWith('Shift') && G.ring) { G.keys.add(e.code); drawRing(); return; }
  if (e.code === 'Space') { e.preventDefault(); if (!e.repeat) togglePause(); return; }
  if (e.repeat) return; const mod = e.ctrlKey || e.metaKey;
  if (mod && e.code === 'KeyA') { e.preventDefault(); return selectAll(); }
  if (!mod) G.keys.add(e.code);
  const n = ['Digit1', 'Digit2', 'Digit3', 'Digit4'].indexOf(e.code); if (n >= 0) return selectAgent(n, e.shiftKey || mod);
  if (e.code === 'Digit5') return selectAll();
  if (mod) return;
  if (e.code === 'KeyT') return G.view === 'cmd' ? setView('tp') : selectAgent((G.sel + 1) % 4); // third person: T takes the next agent
  if (e.code === 'KeyC') { G.camFollow = !G.camFollow; rig.focusGoal = null; return say(G.camFollow ? 'Camera follows the lead' : 'Follow off'); }
  if (e.code === 'Tab') { // step through the people under the cursor
    e.preventDefault(); const hits = entsAt(G.ptr.x, G.ptr.y).filter(actionable); if (hits.length > 1) { const i = hits.indexOf(G.stick); G.tabEnt = hits[(i + 1) % hits.length]; G.tabAt = { x: G.ptr.x, y: G.ptr.y }; say(`Target · ${G.tabEnt.name}`); } return;
  }
  if (e.code === 'KeyF') { const on = !selected().every(isDrawn); selected().forEach((a) => { a.drawnUntil = on ? G.clock + 1e9 : 0; }); return say(`${names(selected())} ${on ? 'draw' : 'holster'}`); }
  if (e.code === 'KeyX') { const s = crew().filter((a) => cancel(a)); stopAll(); if (s.length) say(`X · stopped ${names(s)}`); return; }
  if (e.code === 'Escape') { if (G.ring) closeRing(); else if (G.view === 'tp') setView('cmd'); G.selTarget = null; return; }
  if (G.ring && (e.code === 'KeyE' || e.code === 'Enter')) return takeWay(G.ring.shown[G.ring.lit]);
  if (G.view === 'cmd') { if (e.code === 'KeyQ') rig.turn(-1); if (e.code === 'KeyE') rig.turn(1); }
  else if (e.code === 'KeyE') { if (preset().eHold) eDown = { t: performance.now(), ring: false }; else eAct(); }
});
addEventListener('keyup', (e) => {
  G.keys.delete(e.code); if (e.code.startsWith('Shift') && G.ring) drawRing();
  if (e.code === 'KeyE' && eDown) { if (!eDown.ring) eAct(); eDown = null; }
});
addEventListener('blur', () => G.keys.clear());

// ---- hooks for the chrome -----------------------------------------------------------------
let drawChrome;
drawChrome = buildChrome({
  preset(k) { G.preset = k; G.marker = PRESETS[k].marker; if (PRESETS[k].runMode) G.runMode = PRESETS[k].runMode; G.selTarget = null; closeRing(); say(`Preset · ${PRESETS[k].label}`); },
  option(key, v) { if (key === 'view') return setView(v); if (key === 'crowd') return setCrowd(v); if (key === 'camFollow') { G.camFollow = v === 'true'; rig.focusGoal = null; return; } G[key] = key === 'actScale' ? +v : v; },
  reset() { newWorld(); G.view === 'tp' && setView('cmd'); say('Scene reset'); },
  select: selectAgent, squad: selectAll, pause: togglePause,
  // Vale comes out of the door with two officers: three agents, two officers, half a second of game time
  vale() {
    const w = G.world; if (w.byId.vale) return say('Vale is already out', 'warn');
    for (const [id, kind, x, z, c] of [['vale', 'vale', 12, 6.3, 0xf0e6a0], ['officerA', 'police', 10.4, 6.2, 0x34508f], ['officerB', 'police', 13.6, 6.2, 0x34508f]]) { const e = w.addPerson(id, kind, x, z, c); e.pick.forEach((m) => { m.userData.ent = e; w.picks.push(m); }); }
    w.byId.door.open = true; G.vale = { t0r: performance.now(), t0s: G.clock, done: false, n: 0, sim: 0, real: 0 };
    say('Vale comes out with two officers · lock three agents on them', 'warn');
  },
  // the plate's gait icon: walk or run for this agent's current order only
  gait(i) {
    const a = G.world.agents[i], o = a.order; if (!o) return; const run = !o.run; o.run = run;
    say(`${a.name} ${run ? 'runs · people notice' : 'walks'}`, run ? 'warn' : '');
  },
  stop(i) { const a = G.world.agents[i]; if (cancel(a)) say(`${a.name} stopped`); },
});

// ---- the frame ----------------------------------------------------------------------------
function tracer(a, e, miss = false) { // a miss ends a little beside the target
  const dx = e.g.position.x - a.g.position.x, dz = e.g.position.z - a.g.position.z, len = Math.hypot(dx, dz) || 1, o = miss ? 1.3 : 0;
  const g = new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(a.g.position.x, 1.2, a.g.position.z), new THREE.Vector3(e.g.position.x - dz / len * o, 1.1, e.g.position.z + dx / len * o)]);
  const l = new THREE.Line(g, new THREE.LineBasicMaterial({ color: 0xfff6d8 })); scene.add(l); tracers.push({ l, at: G.clock });
}
function driveAgent(a, dt) { // third person: the keys walk the agent
  const k = G.keys, fa = k.has('KeyW') - k.has('KeyS'), sa = k.has('KeyD') - k.has('KeyA');
  if (!fa && !sa) { rig.moveYaw(false); return false; }
  cancel(a);
  const y = rig.moveYaw(true); let dx = Math.sin(y) * fa - Math.cos(y) * sa, dz = Math.cos(y) * fa + Math.sin(y) * sa; const l = Math.hypot(dx, dz); dx /= l; dz /= l;
  const sp = k.has('ShiftLeft') || k.has('ShiftRight') ? RUN : WALK, p = a.g.position, nav = G.world.nav;
  if (nav.free(p.x + dx * sp * dt, p.z + dz * sp * dt)) { p.x += dx * sp * dt; p.z += dz * sp * dt; } else if (nav.free(p.x + dx * sp * dt, p.z)) p.x += dx * sp * dt; else if (nav.free(p.x, p.z + dz * sp * dt)) p.z += dz * sp * dt;
  a.g.rotation.y = Math.atan2(dx, dz); a.vx = dx * sp; a.vz = dz * sp; a.speed = sp; return true;
}
// ---- a busy street: walkers who keep moving through it (none, some or busy)
function setCrowd(level) {
  const w = G.world; G.crowd = level;
  for (const e of w.ents.filter((x) => x.crowd)) { w.group.remove(e.g); delete w.byId[e.id]; w.ents.splice(w.ents.indexOf(e), 1); w.picks = w.picks.filter((m) => m.userData.ent !== e); }
  let r = 4242; const rnd = () => (r = (r * 1103515245 + 12345) & 0x7fffffff) / 0x7fffffff, tones = [0xc9c2b0, 0xb8a9c9, 0xa9c4b0, 0xc9b79c, 0xd2b3a6, 0xa9bdd2];
  for (let i = 0; i < { none: 0, some: 6, busy: 16 }[level]; i++) {
    const cross = i % 4 === 3, lane = cross ? -16 + rnd() * 34 : 0.8 + rnd() * 5.2, a = cross ? -7.5 : -18 + rnd() * 6, b = cross ? 6.3 : 14 + rnd() * 8, t = rnd(), pos = a + (b - a) * t;
    const e = w.addPerson(`crowd${i}`, 'walker', cross ? lane : pos, cross ? pos : lane, tones[i % tones.length], { crowd: true, walker: { a, b, z: lane, axis: cross ? 'z' : 'x', dir: rnd() < 0.5 ? 1 : -1, wait: 0, speed: 0.9 + rnd() * 0.7, crowd: true } });
    e.pick.forEach((m) => { m.userData.ent = e; w.picks.push(m); });
  }
}
// ---- third person: buildings that hide the agent fade out (the camera inside one counts)
const fadeRay = new THREE.Ray();
function fadeBuildings(real) {
  const me = agentOf(), cam = camera.position, tgt = new THREE.Vector3(me.g.position.x, 1.2, me.g.position.z), d = cam.distanceTo(tgt), p = new THREE.Vector3();
  fadeRay.set(cam, tgt.clone().sub(cam).normalize());
  for (const b of G.world.buildings) {
    const hit = G.view === 'tp' && (b.box.containsPoint(cam) || (fadeRay.intersectBox(b.box, p) && cam.distanceTo(p) < d));
    b.mesh.material.opacity += ((hit ? 0.16 : 1) - b.mesh.material.opacity) * Math.min(1, real * 8);
  }
}
// ---- pause and plan
function togglePause() {
  G.paused = !G.paused;
  if (!G.paused) flushPlan(); else say(G.planMode === 'sync' ? 'Paused · orders wait and start together on Space' : 'Paused · Space to go', 'warn');
}
const roleColor = (a) => ROLES.find((r) => r.key === a.role).color;
const planLines = [0, 1, 2, 3].map(() => { const l = new THREE.Line(new THREE.BufferGeometry(), new THREE.LineDashedMaterial({ dashSize: 0.35, gapSize: 0.3, depthTest: false, transparent: true, opacity: 0.8 })); l.renderOrder = 9; l.frustumCulled = false; l.visible = false; scene.add(l); return l; });
const planRings = [0, 1, 2, 3].map(() => { const r = groundRing(scene, true); r.material.opacity = 0.75; return r; });
function drawGhosts() { // queued orders: a dashed path, a ring where it ends, a pill saying what it is
  const items = [...G.plan.values()], box = $('ghosts');
  planLines.forEach((l, i) => {
    const it = items[i], r = planRings[i], d = box.children[i]; l.visible = r.visible = !!it; d.style.display = it ? 'flex' : 'none'; if (!it) return;
    const p = it.agent.g.position, pts = [new THREE.Vector3(p.x, 0.1, p.z), ...it.path.map(([x, z]) => new THREE.Vector3(x, 0.1, z))]; if (pts.length < 2) pts.push(new THREE.Vector3(p.x + 0.01, 0.1, p.z));
    l.geometry.dispose(); l.geometry = new THREE.BufferGeometry().setFromPoints(pts); l.computeLineDistances(); l.material.color.set(roleColor(it.agent));
    r.position.set(it.end[0], 0.06, it.end[1]); r.scale.setScalar(1.1); r.material.color.set(roleColor(it.agent));
    const s = project(it.end[0], 1.2, it.end[1]); if (!s) { d.style.display = 'none'; return; }
    d.style.transform = `translate(${Math.round(s.x)}px,${Math.round(s.y)}px)`; const html = `${facesHtml([it.agent])}<span>${it.label}</span>`; if (d.dataset.h !== html) { d.dataset.h = html; d.innerHTML = html; }
  });
}
// ---- standing in a camera's or a guard's cone: noticed at 8 s, wary at 20 s, report at 45 s (Heat 0, brief 031); any act pauses the clock
function attention(dt) {
  for (const a of G.world.agents) {
    const seen = G.world.cones.seenAt(a.g.position.x, a.g.position.z), still = !a.speed, acting = a.order?.kind === 'act' && a.order.phase === 'act';
    if (a.seenT === undefined) a.seenT = 0;
    if (seen && still && !acting) a.seenT += dt * G.attScale; else if (!acting) a.seenT = Math.max(0, a.seenT - 2 * dt * G.attScale);
    if (a.seenT >= 45 && !a.reported) { a.reported = true; say(`${a.name} was reported: seen standing at the ${seen}`, 'warn'); } else if (a.seenT < 8) a.reported = false;
  }
}
let last = performance.now(), frames = 0, fpsAt = last, followT = 0;
function frame(now) {
  requestAnimationFrame(frame);
  const real = Math.min(0.05, (now - last) / 1000); last = now;
  G.timeScale = G.paused ? 0 : pd?.drag || now < G.slowUntil ? 0.25 : 1; // paused, or a quarter speed while a portrait is dragged and for 0.5 s after
  const dt = real * G.timeScale; G.clock += dt;
  const w = G.world, P = preset(), me = agentOf();
  if (dt > 0) { w.update(dt, G.clock); w.cones.update(G.clock);
    w.agents.forEach((a) => { const own = G.view === 'tp' && a === me; a.vx = a.vz = 0; if (!(own && driveAgent(a, dt))) step(a, dt, tracer); });
    attention(dt); }
  rig.update(real, me, { x: me.vx || 0, z: me.vz || 0 });
  // hover and prompt
  G.hover = null; G.prompt = null;
  if (G.ptr.over && !G.ring && !(down?.drag)) {
    const h = pick(G.ptr.x, G.ptr.y); let e = h.ent;
    if (G.tabEnt && !h.hits.includes(G.tabEnt) || G.tabAt && Math.hypot(G.ptr.x - G.tabAt.x, G.ptr.y - G.tabAt.y) > 18) G.tabEnt = null;
    if (e !== G.stick && G.stick && !h.hits.includes(G.stick) && performance.now() - (G.stickLost ??= performance.now()) < 150) e = G.stick; // someone walks across: keep the one you were on a moment
    else G.stickLost = null;
    G.stick = e && actionable(e) ? e : null; if (G.stick) G.hover = G.stick;
    canvas.style.cursor = G.hover ? 'pointer' : 'default';
  } else G.stick = null;
  if (G.view === 'tp' && !G.hover) { let best = 8; for (const e of w.ents) { if (e.agent || !actionable(e)) continue; const dx = e.g.position.x - me.g.position.x, dz = e.g.position.z - me.g.position.z, d = Math.hypot(dx, dz); if (d < best && Math.abs(((Math.atan2(dx, dz) - rig.f + 3 * Math.PI) % (2 * Math.PI)) - Math.PI) < 1.2) { best = d; G.prompt = e; } } }
  // in third person the rest of the squad follows the lead, a little behind and to the sides
  followT -= real;
  if (G.view === 'tp' && G.tpSquad === 'follow' && followT <= 0) {
    followT = 0.4; const h = me.g.rotation.y, fx = Math.sin(h), fz = Math.cos(h); let k = 0;
    for (const a of selected()) {
      if (a === me) continue; k++;
      if (a.order && !a.order.follow) continue;
      const side = (k % 2 ? 1 : -1) * Math.ceil(k / 2) * 1.1, tx = me.g.position.x - fx * (1.6 + 0.3 * k) - fz * side, tz = me.g.position.z - fz * (1.6 + 0.3 * k) + fx * side;
      if (Math.hypot(a.g.position.x - tx, a.g.position.z - tz) > 1.8) { const t = w.nav.nearestFree(tx, tz); if (t) walkTo(a, t[0], t[1], Math.hypot(a.g.position.x - tx, a.g.position.z - tz) > 5, true); }
    }
  }
  // third person: the right button held aims, and the nearest hostile ahead is the aim target
  G.aim = G.view === 'tp' && !G.ring && down?.b === 2 && performance.now() - down.t >= RIGHT_TAP_S * 1000; G.aimT = null;
  if (G.aim) { G.aimT = hostileAhead(); if (!G.hover) G.prompt = G.aimT; if (preset().rightFires && G.tpAim === 'fire') me.drawnUntil = G.clock + 1; } // the gun is up while right is held
  $('reticle').style.display = G.aim ? 'block' : 'none'; if (G.aim) $('reticle').style.transform = `translate(${G.ptr.x}px,${G.ptr.y}px)`;
  if (dt > 0) stepFire(dt, { tracer, charged: (c) => {
    if (c.ring) closeRing();
    if (c.way.id === 'fire') { beginFire(c.ent, shooters(c.ent, !!c.right), { held: true, follow: G.view === 'tp' });
      if (G.view === 'tp' && G.followFire === 'lock') { const near = w.agents.filter((a) => a !== me && isDrawn(a) && Math.hypot(a.g.position.x - me.g.position.x, a.g.position.z - me.g.position.z) <= FOLLOW_M); if (near.length) beginFire(c.ent, near, { lock: true }); } }
    else { G.charge = null; G.holdEnt = c.ent; doAct(c.ent, c.way, 'Hold', c.run || runHeld()); }
  } });
  // hold-to-ring (0.2.34): a held left click or a held E
  const nowMs = performance.now();
  if (down && down.b === 0 && !down.drag && !down.ring && P.leftHold && (P.leftHold === true || G.view === 'tp') && nowMs - down.t >= 220 && actionable(down.hit.ent)) { down.ring = true; openRing(down.hit.ent, waysFor(down.hit.ent), P.ring); }
  if (eDown && !eDown.ring && nowMs - eDown.t >= 220) { const e = promptTarget(); eDown.ring = true; if (e) openRing(e, waysFor(e), P.ring); }
  if (G.ring && !waysFor(G.ring.ent).length) closeRing();
  // the marker: the main action on one thing
  const ch = G.charge && !G.charge.done && !G.charge.ring && (G.charge.right ? G.charge.t > RIGHT_TAP_S : G.charge.ent.hostile && !PRESETS[G.preset].holdActs || G.charge.t > 0.2) ? G.charge : null;
  let tgt = G.ring ? null : ch?.ent ?? G.holdEnt ?? G.hover ?? G.selTarget ?? G.prompt;
  const st = ch || G.ring ? null : liveStatus(tgt); if (st) tgt = st.ent; // the live words win over the first offer
  const act = ch ? ch.way : st ? (st.fire ? { id: 'fire', ...ACTS.fire } : st.act) : tgt && mainFor(tgt);
  const plan = act ? planFor(tgt, act, crew(), runHeld()) : null, ws = plan?.walkS ?? 0;
  let extra = null; // where the agent would stand: is it in a cone, and (for Watch) how far it can see from
  if (plan?.lead && !act.noWalk) { const pth = walkPath(plan.lead, tgt, act), sp = pth?.length ? pth[pth.length - 1] : [plan.lead.g.position.x, plan.lead.g.position.z]; extra = { seen: !!w.cones.seenAt(sp[0], sp[1]), reach: act.id === 'watch' && G.view === 'cmd' ? reachOf(tgt, act, plan.lead) : 0 }; }
  marker.update(real, tgt, act, isFinite(ws) ? ws : 0, project, plan?.lead && !act.noWalk ? walkPath(plan.lead, tgt, act) : null, plan?.agents, plan?.running, ch, extra, st);
  if (G.charge?.ring && !G.charge.done) ringCharge(G.charge);
  // breakaway: while hovering, light the agent who would go, dim the ones who hold; for a moment after the order too
  const live = tgt && plan?.agents.length && crew().length > 1 ? { lead: plan.agents, holds: plan.holds } : null, pv = live ?? (G.clock < (G.peel?.until ?? 0) ? G.peel : null);
  G.pv = pv ? { lead: pv.lead.map((a) => w.agents.indexOf(a)), holds: pv.holds.map((a) => w.agents.indexOf(a)) } : null;
  w.agents.forEach((a, i) => {
    const lead = G.pv?.lead.includes(i), hold = G.pv?.holds.includes(i), m = a.body.material; m.transparent = true;
    m.opacity += ((hold ? 0.4 : 1) - m.opacity) * 0.3; m.emissive.setHex(lead ? 0x777777 : 0); const running = a.speed > 2; a.g.pose.rotation.x += ((running ? 0.28 : 0) - a.g.pose.rotation.x) * 0.3;
    if (running && !a.wasRun && G.clock - (G.noticeAt ?? -99) > 6) { say('Running: people notice', 'warn'); G.noticeAt = G.clock; } a.wasRun = running;
  });
  breakLines.forEach((l, i) => { // a path from each breaking-away agent to the target (not while it is already walking)
    const a = w.agents[live ? w.agents.indexOf(plan.agents[i]) : -1];
    l.visible = !!a; if (!a) return;
    const pts = (walkPath(a, tgt, act) ?? [[a.g.position.x, a.g.position.z], [tgt.g.position.x, tgt.g.position.z]]).map(([x, z]) => new THREE.Vector3(x, 0.12, z));
    l.geometry.dispose(); l.geometry = new THREE.BufferGeometry().setFromPoints(pts); l.computeLineDistances(); l.material.color.set(ROLES.find((r) => r.key === a.role).color);
  });
  w.agents.forEach((a, i) => { const m = selRings[i]; m.visible = G.group[i]; if (m.visible) { m.position.set(a.g.position.x, 0.03, a.g.position.z); m.scale.setScalar(G.pv?.lead.includes(i) ? 2.1 : i === G.sel && selected().length > 1 ? 1.9 : 1.5); } });
  w.agents.forEach((a, i) => { // the destination: a ring for a walk; a doubled, dashed ring for a run
    const m = moveRings[i], d = runRings[i], c = runCores[i], o = a.order, on = !!o && o.kind === 'walk';
    m.visible = on && !o.run; d.visible = c.visible = on && !!o.run;
    if (on) for (const r of o.run ? [d, c] : [m]) { r.position.set(o.dest.x, 0.04, o.dest.z); r.scale.setScalar(r === c ? 0.5 : r === d ? 1.15 : 0.9); }
  });
  for (let i = tracers.length - 1; i >= 0; i--) if (G.clock - tracers[i].at > 0.35) { scene.remove(tracers[i].l); tracers.splice(i, 1); }
  fadeBuildings(real); drawGhosts(); document.body.classList.toggle('paused', G.paused);
  if (G.vale && !G.vale.done) { const v = G.vale, locked = new Set(G.sessions.filter((s) => s.lock && s.ent.id.startsWith('officer')).flatMap((s) => s.agents.map((a) => a.id))); v.n = locked.size; v.sim = G.clock - v.t0s; v.real = (performance.now() - v.t0r) / 1000; if (v.n >= 3) v.done = true; }
  placeRing(project);
  const hint = G.paused ? (G.planMode === 'sync' ? 'Paused · Space starts every order together' : 'Paused · Space to go') : G.ring ? (crew().length > 1 ? preset().holdActs ? 'Shift runs there' : 'Shift runs there · Alt: everyone or the best placed' : P.right === 'press' ? 'Right click again takes the lit way' : 'Click away to close') : G.view === 'tp' && G.prompt ? 'E does it' : G.view === 'tp' && G.clock - (G.tpAt ?? -99) < 5 ? 'T lets go of the agent' : preset().holdActs && G.hover ? 'Hold the left button to do it · right hold fires' : G.hover?.hostile ? 'Hold the left button to fire' : P.dbl.thing && (G.hover || G.selTarget) ? (P.dbl.thing === 'run' ? 'Double click runs there' : 'Double click to do it') : '';
  setHint(hint); refresh(project);
  renderer.render(scene, camera);
  frames++; if (now - fpsAt > 1000) { setFps(Math.round(frames * 1000 / (now - fpsAt))); frames = 0; fpsAt = now; }
}
requestAnimationFrame(frame);

// for the headless check
window.__ctl = { setCrowd, entsAt, marker, defaultsOk: () => G.world.ents.every((e) => G.world.agents.every((a) => !['knock', 'kill'].includes(defaultFor(e, a)?.id))), select: (i) => setGroup(G.group.map((_, k) => k === i), false), squad: selectAll, G, screenOf: (id) => { const e = G.world.byId[id]; return project(e.g.position.x, e.h * 0.5, e.g.position.z); }, setView, preset: (k) => { G.preset = k; G.marker = PRESETS[k].marker; if (PRESETS[k].runMode) G.runMode = PRESETS[k].runMode; closeRing(); G.selTarget = null; drawChrome(); } };
