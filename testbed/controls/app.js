// The controls playground: wires the scene, the camera, the presets and the pointer together.
import * as THREE from './vendor/three.module.min.js';
import { G, say } from './state.js';
import { PRESETS, ACTS, WALK, RUN, ROLES, priceLine } from './data.js';
import { buildWorld } from './world.js';
import { Rig } from './camera.js';
import { Marker, groundRing } from './markers.js';
import { initRing, openRing, closeRing, ringPointer, placeRing, draw as drawRing } from './ring.js';
import { agentOf, crew, selected, names, waysFor, defaultFor, planFor, walkPath, walkTo, groupWalk, giveAct, cancel, step } from './orders.js';
import { buildChrome, refresh, setHint, setFps } from './ui.js';

const $ = (id) => document.getElementById(id);
window.addEventListener('error', (e) => { $('errors').textContent += `${e.message} (${e.filename?.split('/').pop()}:${e.lineno})\n`; });
window.addEventListener('unhandledrejection', (e) => { $('errors').textContent += `${e.reason}\n`; });

const canvas = $('c'), renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
const scene = new THREE.Scene(); scene.background = new THREE.Color(0xcfe8ef);
scene.add(new THREE.HemisphereLight(0xffffff, 0xb8a98f, 1.5)); const sun = new THREE.DirectionalLight(0xfff1d6, 2.2); sun.position.set(-10, 24, 8); scene.add(sun);
const camera = new THREE.PerspectiveCamera(30, 1, 0.1, 300), rig = new Rig(camera); G.rig = rig;
const marker = new Marker(scene, $('marker')), moveRings = [0, 1, 2, 3].map(() => groundRing(scene)), selRings = [0, 1, 2, 3].map(() => groundRing(scene));
moveRings.forEach((m, i) => m.material.color.set(ROLES[i].color)); selRings.forEach((m, i) => m.material.color.set(ROLES[i].color));
const V = new THREE.Vector3(), ray = new THREE.Raycaster(), tracers = [];
const preset = () => PRESETS[G.preset];

function newWorld() { G.world?.dispose(); G.world = buildWorld(scene); G.trail = 0; G.alert = 0; G.cash = 100000; G.selTarget = null; G.group = [true, false, false, false]; G.sel = 0; G.carry = null; G.world.picks = G.world.ents.flatMap((e) => e.pick.map((m) => (m.userData.ent = e, m))); closeRing(); }
newWorld();
function resize() { renderer.setSize(innerWidth, innerHeight); camera.aspect = innerWidth / innerHeight; camera.updateProjectionMatrix(); }
addEventListener('resize', resize); resize();

const project = (x, y, z) => { V.set(x, y, z).project(camera); return V.z > 1 ? null : { x: (V.x * 0.5 + 0.5) * innerWidth, y: (-V.y * 0.5 + 0.5) * innerHeight }; };
function pick(cx, cy) {
  ray.setFromCamera({ x: cx / innerWidth * 2 - 1, y: -(cy / innerHeight) * 2 + 1 }, camera);
  const hit = ray.intersectObjects(G.world.picks, false)[0], g = new THREE.Vector3();
  ray.ray.intersectPlane(new THREE.Plane(new THREE.Vector3(0, 1, 0), 0), g);
  return { ent: hit?.object.userData.ent ?? null, point: g };
}
const actionable = (e) => !!e && !e.agent && !!defaultFor(e);

// ---- what a click does ------------------------------------------------------------------
function doAct(e, w, how, run = false, agents = crew()) {
  const plan = planFor(e, w, agents);
  if (!plan.agents.length) return say(`${how} · nobody can ${w.verb.toLowerCase()} ${e.name}`, 'warn');
  if (!isFinite(plan.walkS)) return say(`${how} · no way to ${e.name}`, 'warn');
  const doing = plan.agents.filter((a) => giveAct(a, e, w, run));
  if (doing.length) say(`${how} · ${names(doing)}: ${priceLine(w, e, plan.walkS).join(' · ')}${plan.holds.length ? ` · ${names(plan.holds)} hold` : ''}`, 'act');
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
  if (twice && G.view === 'cmd') { rig.focus.copy(G.world.agents[i].g.position); say(`Centred on the ${G.world.agents[i].name}`); } // a double tap centres, as in the game
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
  if (e?.agent) { G.selTarget = null; return selectAgent(G.world.agents.indexOf(e), shift); }
  if (e) {
    const w = defaultFor(e);
    if (!w) return waysFor(e).length ? openRing(e, waysFor(e), P.ring) : say(`${how} · ${names(crew())} can't do anything to ${e.name}`, 'warn');
    const mode = dbl ? P.dbl.thing : P.left.thing;
    if (mode === 'act') { G.selTarget = null; return doAct(e, w, how); }
    if (!dbl) { G.selTarget = e; return say(`${how} · selected ${e.name}`); }
    return;
  }
  G.selTarget = null;
  if (dbl && P.dbl.ground === 'run') return groundGo(hit.point, how, true);
  if (!dbl) groundGo(hit.point, how, shift);
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
// Shift on a wedge flips the squad setting for that one order: everyone does it, or only the best placed.
function takeWay(w, flip = false) { const e = G.ring.ent, was = G.squadAct; if (flip) G.squadAct = was === 'best' ? 'all' : 'best'; closeRing(); doAct(e, w, 'Ring'); G.squadAct = was; }
initRing($('ring'), takeWay);

// ---- the pointer --------------------------------------------------------------------------
let down = null, lastClick = { t: 0, x: 0, y: 0, ent: null };
canvas.addEventListener('contextmenu', (e) => e.preventDefault());
canvas.addEventListener('pointerdown', (e) => {
  canvas.setPointerCapture(e.pointerId); G.ptr.x = e.clientX; G.ptr.y = e.clientY;
  down = { b: e.button, x: e.clientX, y: e.clientY, t: performance.now(), hit: pick(e.clientX, e.clientY), drag: false, ring: false };
  if (e.button === 2 && preset().right === 'press') { // 0.2.34: the ring opens on press
    const h = down.hit; down.ring = true;
    if (G.ring) rightTap(h); else if (h.ent && !h.ent.agent && waysFor(h.ent).length) openRing(h.ent, waysFor(h.ent), preset().ring); else rightTap(h);
  }
});
canvas.addEventListener('pointermove', (e) => {
  G.ptr.x = e.clientX; G.ptr.y = e.clientY; if (!down) return;
  if (!down.drag && Math.hypot(e.clientX - down.x, e.clientY - down.y) > 6) down.drag = true;
  if (down.drag && (down.b === 0 || down.b === 1) && G.view === 'tp') rig.orbit(e.movementX, e.movementY);
  if (down.drag && down.b === 0 && G.view === 'cmd') { // a drag box on the ground picks agents
    const r = { x0: Math.min(down.x, e.clientX), x1: Math.max(down.x, e.clientX), y0: Math.min(down.y, e.clientY), y1: Math.max(down.y, e.clientY) }; G.box = r;
    Object.assign($('box').style, { display: 'block', left: r.x0 + 'px', top: r.y0 + 'px', width: r.x1 - r.x0 + 'px', height: r.y1 - r.y0 + 'px' });
  }
});
canvas.addEventListener('pointerup', (e) => {
  const d = down; down = null; if (!d) return;
  const now = performance.now(), hold = (now - d.t) / 1000;
  if (d.drag && d.b === 0 && G.box) { boxSelect(G.box, e.shiftKey || e.ctrlKey || e.metaKey); G.box = null; $('box').style.display = 'none'; }
  if (d.drag || d.ring) return;
  const hit = pick(e.clientX, e.clientY);
  if (d.b === 0) {
    const dbl = now - lastClick.t < 350 && (hit.ent ? lastClick.ent === hit.ent : !lastClick.ent && Math.hypot(e.clientX - lastClick.x, e.clientY - lastClick.y) < 24);
    lastClick = dbl ? { t: 0, x: 0, y: 0, ent: null } : { t: now, x: e.clientX, y: e.clientY, ent: hit.ent };
    leftClick(hit, dbl, e.shiftKey);
  } else if (d.b === 2) {
    if (hold >= 0.22) say('Right hold · nothing happens', 'warn'); else rightTap(hit);
  }
});
addEventListener('pointermove', (e) => { if (G.ring) ringPointer(e, false); });
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
  G.ptr.x = e.clientX; G.ptr.y = e.clientY; G.ptr.over = document.elementFromPoint(e.clientX, e.clientY) === canvas;
  const r = document.querySelector(`[data-agent="${pd.i}"]`).getBoundingClientRect(), ls = $('rubber').querySelectorAll('line');
  ls.forEach((l) => { l.setAttribute('x1', r.x + r.width / 2); l.setAttribute('y1', r.y); l.setAttribute('x2', e.clientX); l.setAttribute('y2', e.clientY); });
  Object.assign($('ghost').style, { left: e.clientX + 'px', top: e.clientY + 'px' }); $('ghost').textContent = names(G.carry);
});
addEventListener('pointerup', (e) => {
  const d = pd; pd = null; if (!d?.drag) return;
  const carried = G.carry; G.carry = null; $('rubber').style.display = $('ghost').style.display = 'none'; dragEnded = true; setTimeout(() => (dragEnded = false), 0);
  if (document.elementFromPoint(e.clientX, e.clientY) !== canvas) return say('Portrait drop · cancelled');
  const hit = pick(e.clientX, e.clientY), who = names(carried), x = hit.ent;
  if (!x) return groundGo(hit.point, 'Portrait drop', false, carried);
  if (x.agent) return say('Portrait drop · cancelled');
  G.carry = carried; const w = defaultFor(x), any = waysFor(x).length; G.carry = null;
  if (w) return doAct(x, w, 'Portrait drop', false, carried);
  if (any) { setGroup(G.world.agents.map((a) => carried.includes(a)), false); openRing(x, waysFor(x), preset().ring); return say(`Portrait drop · ring for ${who}`); }
  say(`Portrait drop · ${who} can't do anything to ${x.name}`, 'warn');
});

// ---- keys ---------------------------------------------------------------------------------
let eDown = null;
function promptTarget() { return G.hover ?? G.prompt; }
function eAct() { const e = promptTarget(); if (!e) return; const w = defaultFor(e); if (w) doAct(e, w, 'E'); }
function setView(v) {
  if (v === G.view) return; closeRing(); rig.setMode(v, agentOf()); G.view = v; G.tpAt = G.clock; G.selTarget = null;
  say(v === 'tp' ? `Third person · the ${agentOf().name}${selected().length > 1 ? ' leads' : ''}` : 'Commander view'); drawChrome?.();
}
addEventListener('keydown', (e) => {
  if (e.repeat) return; const mod = e.ctrlKey || e.metaKey;
  if (mod && e.code === 'KeyA') { e.preventDefault(); return selectAll(); }
  if (!mod) G.keys.add(e.code);
  const n = ['Digit1', 'Digit2', 'Digit3', 'Digit4'].indexOf(e.code); if (n >= 0) return selectAgent(n, e.shiftKey || mod);
  if (e.code === 'Digit5') return selectAll();
  if (mod) return;
  if (e.code === 'KeyT') return setView(G.view === 'cmd' ? 'tp' : 'cmd');
  if (e.code === 'Escape') { closeRing(); G.selTarget = null; return; }
  if (G.ring && (e.code === 'KeyE' || e.code === 'Enter')) return takeWay(G.ring.shown[G.ring.lit]);
  if (G.view === 'cmd') { if (e.code === 'KeyQ') rig.turn(-1); if (e.code === 'KeyE') rig.turn(1); }
  else if (e.code === 'KeyE') { if (preset().eHold) eDown = { t: performance.now(), ring: false }; else eAct(); }
});
addEventListener('keyup', (e) => {
  G.keys.delete(e.code);
  if (e.code === 'KeyE' && eDown) { if (!eDown.ring) eAct(); eDown = null; }
});
addEventListener('blur', () => G.keys.clear());

// ---- hooks for the chrome -----------------------------------------------------------------
let drawChrome;
drawChrome = buildChrome({
  preset(k) { G.preset = k; G.marker = PRESETS[k].marker; G.selTarget = null; closeRing(); say(`Preset · ${PRESETS[k].label}`); },
  option(key, v) { if (key === 'view') return setView(v); G[key] = key === 'actScale' ? +v : v; },
  reset() { newWorld(); G.view === 'tp' && setView('cmd'); say('Scene reset'); },
  select: selectAgent, squad: selectAll,
});

// ---- the frame ----------------------------------------------------------------------------
function tracer(a, e) {
  const g = new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(a.g.position.x, 1.2, a.g.position.z), new THREE.Vector3(e.g.position.x, 1.1, e.g.position.z)]);
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
let last = performance.now(), frames = 0, fpsAt = last, followT = 0;
function frame(now) {
  requestAnimationFrame(frame);
  const dt = Math.min(0.05, (now - last) / 1000); last = now; G.clock += dt;
  const w = G.world, P = preset(), me = agentOf();
  w.update(dt, G.clock);
  w.agents.forEach((a) => { const own = G.view === 'tp' && a === me; a.vx = a.vz = 0; if (!(own && driveAgent(a, dt))) step(a, dt, tracer); });
  rig.update(dt, me, { x: me.vx || 0, z: me.vz || 0 });
  // hover and prompt
  G.hover = null; G.prompt = null;
  if (G.ptr.over && !G.ring && !(down?.drag)) { const h = pick(G.ptr.x, G.ptr.y); if (actionable(h.ent)) G.hover = h.ent; canvas.style.cursor = G.hover ? 'pointer' : 'default'; }
  if (G.view === 'tp' && !G.hover) { let best = 8; for (const e of w.ents) { if (e.agent || !actionable(e)) continue; const dx = e.g.position.x - me.g.position.x, dz = e.g.position.z - me.g.position.z, d = Math.hypot(dx, dz); if (d < best && Math.abs(((Math.atan2(dx, dz) - rig.f + 3 * Math.PI) % (2 * Math.PI)) - Math.PI) < 1.2) { best = d; G.prompt = e; } } }
  // in third person the rest of the squad follows the lead, a little behind and to the sides
  followT -= dt;
  if (G.view === 'tp' && G.tpSquad === 'follow' && followT <= 0) {
    followT = 0.4; const h = me.g.rotation.y, fx = Math.sin(h), fz = Math.cos(h); let k = 0;
    for (const a of selected()) {
      if (a === me) continue; k++;
      if (a.order && !a.order.follow) continue;
      const side = (k % 2 ? 1 : -1) * Math.ceil(k / 2) * 1.1, tx = me.g.position.x - fx * (1.6 + 0.3 * k) - fz * side, tz = me.g.position.z - fz * (1.6 + 0.3 * k) + fx * side;
      if (Math.hypot(a.g.position.x - tx, a.g.position.z - tz) > 1.8) { const t = w.nav.nearestFree(tx, tz); if (t) walkTo(a, t[0], t[1], Math.hypot(a.g.position.x - tx, a.g.position.z - tz) > 5, true); }
    }
  }
  // hold-to-ring (0.2.34): a held left click or a held E
  const nowMs = performance.now();
  if (down && down.b === 0 && !down.drag && !down.ring && P.leftHold && (P.leftHold === true || G.view === 'tp') && nowMs - down.t >= 220 && actionable(down.hit.ent)) { down.ring = true; openRing(down.hit.ent, waysFor(down.hit.ent), P.ring); }
  if (eDown && !eDown.ring && nowMs - eDown.t >= 220) { const e = promptTarget(); eDown.ring = true; if (e) openRing(e, waysFor(e), P.ring); }
  if (G.ring && !waysFor(G.ring.ent).length) closeRing();
  // the marker: the main action on one thing
  const tgt = G.ring ? null : G.hover ?? G.selTarget ?? G.prompt, act = tgt && defaultFor(tgt);
  const plan = act ? planFor(tgt, act) : null, ws = plan?.walkS ?? 0;
  marker.update(dt, tgt, act, isFinite(ws) ? ws : 0, project, plan?.lead && !act.noWalk ? walkPath(plan.lead, tgt, act) : null, plan?.who);
  w.agents.forEach((a, i) => { const m = selRings[i]; m.visible = G.group[i]; if (m.visible) { m.position.set(a.g.position.x, 0.03, a.g.position.z); m.scale.setScalar(i === G.sel && selected().length > 1 ? 1.9 : 1.5); } });
  w.agents.forEach((a, i) => { const m = moveRings[i], o = a.order; m.visible = !!o && o.kind === 'walk'; if (m.visible) { m.position.set(o.dest.x, 0.04, o.dest.z); m.scale.setScalar(0.9); } });
  for (let i = tracers.length - 1; i >= 0; i--) if (G.clock - tracers[i].at > 0.12) { scene.remove(tracers[i].l); tracers.splice(i, 1); }
  placeRing(project);
  const hint = G.ring ? (crew().length > 1 ? 'Shift + click does it the other way: everyone or the best placed' : P.right === 'press' ? 'Right click again takes the lit way' : 'Click away to close') : G.view === 'tp' && G.prompt ? 'E does it' : G.view === 'tp' && G.clock - (G.tpAt ?? -99) < 5 ? 'T lets go of the agent' : P.dbl.thing && (G.hover || G.selTarget) ? 'Double click to do it' : '';
  setHint(hint); refresh(project);
  renderer.render(scene, camera);
  frames++; if (now - fpsAt > 1000) { setFps(Math.round(frames * 1000 / (now - fpsAt))); frames = 0; fpsAt = now; }
}
requestAnimationFrame(frame);

// for the headless check
window.__ctl = { select: (i) => setGroup(G.group.map((_, k) => k === i), false), squad: selectAll, G, screenOf: (id) => { const e = G.world.byId[id]; return project(e.g.position.x, e.h * 0.5, e.g.position.z); }, setView, preset: (k) => { G.preset = k; G.marker = PRESETS[k].marker; closeRing(); G.selTarget = null; drawChrome(); } };
