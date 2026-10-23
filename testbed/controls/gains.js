// Brief 043: the done moment. When an act lands, show at a glance that it finished and what it gave; when a new order drops an act, say so, and warn before.
// Everything here is a view on top of the fake acts: the gains are the Handover's real ones (briefs/043-done-moment.md, "what each act gains"),
// the words are the game's own fragments (text.json less.intel) or plain labels marked for Fable. G.done: 'built' (0.2.48 as it plays) | 'gain' | 'gated' (also hides acts until the Handover's gates open).
import { G } from './state.js';
import { ACTS, KINDS, ROLES } from './data.js';
import { icon } from './icons.js';
import { facesHtml } from './markers.js';

const real = () => performance.now() / 1000;
export const FRESH_S = 90, CHIP_S = 4.2, PLATE_S = 2.2, TICK_S = 1.2, POP_S = 1.8, ROWS = 12;
export const on = () => G.done !== 'built';
const mmss = (t) => `${Math.floor(t / 60)}:${String(Math.floor(t % 60)).padStart(2, '0')}`;
const cap = (s) => s[0].toUpperCase() + s.slice(1);

// What Listen teaches about each person: the need record's label, the temperament's fragment, the secret's fragment (text.json less.intel; contracts/people/harbour-district.needs.json).
const FACTS = {
  inez: { need: 'Bureau loan', temper: 'Idealist', secret: 'Loves the guard' },
  guard: { need: 'Promotion', temper: 'Loyal', secret: 'Loves Inez' },
  driver: { need: 'Lean month', temper: 'For hire', secret: '' },
};
// What the approaches come to on each person, one after the other (stand-ins for the engine's turn.<approach>.<outcome>): Inez needs two Helps (route A), Pay works on the driver.
const TURNS = {
  inez: { help: ['notyet', 'turned'], pay: ['refused'], press: ['suspicious'] },
  driver: { pay: ['turned'], help: ['turned'], press: ['suspicious'] },
  guard: { help: ['notyet', 'notyet', 'turned'], press: ['suspicious'], pay: ['refused'] },
};
// The Handover's gates (text.json locks: needs-routine, needs-need, needs-temperament, not-a-contact, the door's three conditions). Always computed; hidden from the ring only in 'gated'.
const GATES = {
  listen: (e) => !e.person || !!e.know?.routine,
  pay: (e) => !e.person || (!!e.know?.routine && !!e.know?.temper && !e.turned), // already-turned: Call them, or Help to keep them on side
  help: (e) => !e.person || !!e.know?.need,
  press: (e) => !e.person || (!!e.know?.need && !e.turned),
  call: (e) => !e.person || !!e.turned,
  board: () => !!G.valeTaken,
  open: (e) => e.kind !== 'door' || !!(G.world?.byId.camera?.dark && G.world?.byId.guard?.dark),
};
export const gatePass = (e, id) => !GATES[id] || GATES[id](e);
export const gateOpen = (e, id) => G.done !== 'gated' || (gatePass(e, id) && !e.closed?.has(id));
export const isFresh = (e, id) => on() && (e.fresh?.[id]?.until ?? 0) > real();
export const freshSeq = (e, id) => e.fresh?.[id]?.seq ?? 0;
// Among the acts that just opened, the one that fits the person leads (the engine's context rank; here: the approach whose script turns them soonest).
export const fitRank = (e, id) => { const l = TURNS[e.kind]?.[id]; const i = l ? l.indexOf('turned') : -1; return i < 0 ? 9 : i; };
let seq = 0;
const fresh = (e, id, n) => { (e.fresh ??= {})[id] = { until: real() + FRESH_S, seq: n }; }; // n: which landing opened it, so the newest news leads

// The gain of a landed act: lines (icon + word), who it changes (anchor), the plate word, the tone ('ok' filled, 'soft' not yet, 'bad' refused).
function gainOf(e, act, agent) {
  const w = G.world, id = act.id, out = { lines: [], tone: 'ok', anchor: e, plate: 'done' };
  const line = (ic, t) => out.lines.push({ ic, t });
  const know = (e.know ??= {});
  if (id === 'watch') {
    if (e.kind === 'camera') { line('eye', 'Cone'); know.sight = true; e.pulse = real() + 2.4; }
    else if (e.person) { line('clock', 'Rounds'); know.routine = true; if (e.kind === 'guard') { line('eye', 'Cone'); know.sight = true; e.pulse = real() + 2.4; } }
    else line('clock', 'Rounds');
  } else if (id === 'listen') {
    const f = FACTS[e.kind]; know.need = know.temper = true; if (f) { line('coin', f.need); line('person', f.temper); if (f.secret) { line('heart', f.secret); know.secret = true; } } else line('person', 'Nothing new');
  } else if (['pay', 'help', 'press'].includes(id) && TURNS[e.kind]?.[id]) {
    const tries = (e.tries ??= {}), list = TURNS[e.kind][id], res = list[Math.min(tries[id] ?? 0, list.length - 1)]; tries[id] = (tries[id] ?? 0) + 1;
    if (res === 'turned') { line('person', 'Contact'); e.turned = true; out.plate = 'turned'; }
    else if (res === 'notyet') { line('half', 'Not yet'); out.tone = 'soft'; out.plate = 'not yet'; e.used.delete(id); }
    else if (res === 'refused') { line('cross', 'Refused'); out.tone = 'bad'; out.plate = 'refused'; (e.closed ??= new Set()).add(id); out.closed = id; }
    else { line('siren', 'Reported'); out.tone = 'bad'; out.plate = 'reported'; G.alert = Math.max(G.alert, 1); }
  } else if (id === 'call') {
    const cam = w.byId.camera, gd = w.byId.guard;
    if (e.kind === 'inez') { out.anchor = gd; line('eyeoff', 'Guard away'); gd.dark = true; gd.window = { icon: 'eyeoff', until: G.clock + 60 }; }
    else { out.anchor = cam; line('eyeoff', 'Camera blind'); cam.dark = true; cam.window = { icon: 'eyeoff', until: G.clock + 45 }; }
  } else if (id === 'loop') {
    const cam = w.byId.camera; out.anchor = cam; line('eyeoff', 'Camera dark'); cam.dark = true; cam.window = { icon: 'eyeoff', holder: agent, home: { x: agent.g.position.x, z: agent.g.position.z }, face: agent };
  } else if (id === 'open') { line('door', 'Door open'); }
  else if (id === 'take') { if (e.kind === 'vale') { line('person', 'Vale'); G.valeTaken = true; } else line('hand', 'Taken'); }
  else if (id === 'board') { line('car', 'Aboard'); }
  else if (id === 'knock') { line('body', 'Out'); line('clock', 'Wakes'); }
  else if (id === 'kill') { line('body', 'Dead'); }
  if (!out.lines.length) out.lines.push({ ic: 'check', t: cap(act.doneText || 'Done') });
  return out;
}

// Called by orders.finish() once the act's effects are applied: the chip, the plate word, the tick on the agent, the history row, the newly open acts.
export function landed(a, e, act) {
  if (!on() || act.fire) return;
  const before = new Map(); for (const x of G.world.ents) for (const id of Object.keys(ACTS)) before.set(x.id + id, gatePass(x, id));
  const g = gainOf(e, act, a);
  const n = ++seq, unlock = []; // the acts that were shut and are open now, on any thing (the chip names at most three)
  for (const x of G.world.ents) for (const id of Object.keys(ACTS)) if (!before.get(x.id + id) && gatePass(x, id) && x.acts?.includes(id) && ACTS[id].by.length && !x.closed?.has(id)) { fresh(x, id, n); if (!unlock.some((u) => u.id === id)) unlock.push({ id, ent: x }); }
  const born = real(), key = `${a.id}-${born}`;
  (G.chips ??= []).push({ key, ent: g.anchor, act, agent: a, lines: g.lines, tone: g.tone, unlock: unlock.slice(0, 3), closed: g.closed, born });
  while (G.chips.length > 4) G.chips.shift();
  a.doneUntil = born + PLATE_S; a.doneWord = g.plate; a.doneKind = g.tone; a.tickAt = born;
  G.hist ??= []; G.hist.unshift({ at: G.clock, agent: a, act, target: e, bits: g.lines.map((l) => l.t), tone: g.tone, unread: true }); G.hist.length = Math.min(G.hist.length, ROWS);
}

// A window that closes: the loop ends when the Tech leaves the cabinet; a call's window runs out.
export function windowsStep() {
  if (!on() || !G.world) return;
  for (const e of G.world.ents) {
    const w = e.window; if (!w) continue;
    const gone = w.holder ? (w.holder.order && w.holder.order.kind !== 'act' || w.holder.order?.kind === 'act' && w.holder.order.act.id !== 'loop' || Math.hypot(w.holder.g.position.x - w.home.x, w.holder.g.position.z - w.home.z) > 2.4) : G.clock >= w.until;
    if (gone) { e.window = null; e.dark = false; (G.pops ??= []).push({ ent: e, at: 'ent', html: `${icon('eye', 16)}<span>${e.kind === 'camera' ? 'Camera on' : 'Guard back'}</span>`, cls: 'back', born: real() }); }
  }
}

// What a new order would drop: every given agent on an act, walking to it or doing it.
export function cancelsOf(agents) {
  const out = [];
  for (const a of agents) {
    const o = a.order; if (!o || o.kind !== 'act' || o.act.fire) continue;
    const spent = o.phase === 'act' ? Math.round(o.t * G.actScale) : 0, turn = ['pay', 'help', 'press'].includes(o.act.id);
    out.push({ agent: a, verb: o.act.verb, cost: o.phase !== 'act' ? '' : turn ? 'remembered' : o.act.kind === 'harm' ? 'they report' : spent >= 1 ? `${spent} s lost` : '' });
  }
  return out;
}
export const cancelHtml = (list) => (list.length ? `<span class="cx">${icon('cross', 15)}<b>Cancels ${[...new Set(list.map((c) => c.verb))].join(' and ')}</b>${list[0].cost ? `<i>${list[0].cost}</i>` : ''}</span>` : '');

// A new order, X or a stop dropped an act in progress: a pop on the agent, the plate word, a history row.
export function dropped(a, o) {
  if (!on() || o.kind !== 'act' || o.act.fire) return;
  const c = cancelsOf([a])[0] ?? { verb: o.act.verb, cost: '' }, t = real();
  (G.pops ??= []).push({ agent: a, at: 'agent', html: `${icon('cross', 16)}<span>${c.verb} cancelled</span>${c.cost ? `<i>${c.cost}</i>` : ''}`, cls: 'cancel', born: t });
  a.doneUntil = t + PLATE_S * 0.7; a.doneWord = 'cancelled'; a.doneKind = 'cancel';
  G.hist ??= []; G.hist.unshift({ at: G.clock, agent: a, act: o.act, target: o.ent, bits: [`${c.verb} cancelled`, ...(c.cost ? [c.cost] : [])], tone: 'cancel', cancelled: true, unread: true }); G.hist.length = Math.min(G.hist.length, ROWS);
}

// ---- drawing
const $ = (id) => document.getElementById(id);
function sync(box, items, make) { // items: [{key, html, cls, x, y}] keep their elements between frames
  const have = new Map([...box.children].map((c) => [c.dataset.k, c]));
  items.forEach((it) => {
    let d = have.get(it.key); have.delete(it.key);
    if (!d) { d = document.createElement('div'); d.dataset.k = it.key; box.appendChild(d); }
    if (d.dataset.h !== it.html || d.className !== it.cls) { d.dataset.h = it.html; d.className = it.cls; d.innerHTML = it.html; }
    d.style.transform = `translate(${Math.round(it.x)}px,${Math.round(it.y)}px)`; d.style.opacity = it.o ?? 1;
  });
  have.forEach((d) => d.remove());
}
const ease = (t) => 1 - (1 - t) * (1 - t);

export function gainsFrame(project) {
  const t = real(), items = [];
  if (!on() || !G.world) { for (const id of ['gains', 'wins', 'pops']) $(id).replaceChildren(); $('hist').style.display = $('histkey').style.display = 'none'; return; }
  windowsStep();
  // the gain chips, on the thing that changed
  G.chips = (G.chips ?? []).filter((c) => t - c.born < CHIP_S);
  const per = new Map();
  for (const c of G.chips) {
    const age = t - c.born, p = project(c.ent.g.position.x, c.ent.h + 0.35, c.ent.g.position.z); if (!p) continue;
    const n = per.get(c.ent) ?? 0; per.set(c.ent, n + 1);
    const lift = (G.hover === c.ent ? 56 : 0) + (c.ent.window ? 36 : 0) + n * 8 + (1 - ease(Math.min(1, age / 0.25))) * 10;
    const lines = c.lines.map((l) => `<span class="gl">${icon(l.ic, 17)}<b>${l.t}</b></span>`).join('');
    const unl = c.unlock.length || c.closed ? `<span class="gu">${icon('arrow', 14)}${c.unlock.map((u) => `<span class="gv">${icon(ACTS[u.id].icon, 15)}${ACTS[u.id].verb}</span>`).join('')}${c.closed ? `<span class="gv shut">${icon(ACTS[c.closed].icon, 15)}${ACTS[c.closed].verb}</span>` : ''}</span>` : '';
    const mark = c.tone === 'ok' ? 'check' : c.tone === 'soft' ? 'half' : 'cross';
    items.push({ key: c.key, html: `<span class="gt">${facesHtml([c.agent])}<em>${icon(mark, 18)}</em></span><span class="gb">${lines}${unl}</span>`, cls: `gn ${c.tone}`, x: p.x, y: p.y - 10 - lift, o: Math.min(1, age / 0.12, (CHIP_S - age) / 0.5) });
  }
  sync($('gains'), items);
  [...$('gains').children].forEach((d) => { const c = G.chips.find((x) => x.key === d.dataset.k); if (c) d.style.setProperty('--c', KINDS[c.act.kind].color); });
  // the agent's tick and the pops (cancelled, window closed)
  const pops = []; G.pops = (G.pops ?? []).filter((q) => t - q.born < POP_S);
  for (const q of G.pops) {
    const age = t - q.born, base = q.at === 'agent' ? q.agent.g.position : q.ent.g.position, p = project(base.x, q.at === 'agent' ? 3.1 : q.ent.h + 0.6, base.z); if (!p) continue;
    pops.push({ key: 'p' + q.born + q.cls, html: q.html, cls: `pop ${q.cls}`, x: p.x, y: p.y - ease(Math.min(1, age / 0.3)) * 8, o: Math.min(1, age / 0.1, (POP_S - age) / 0.4) });
  }
  for (const a of G.world.agents) {
    const age = t - (a.tickAt ?? -9); if (age > TICK_S) continue; const p = project(a.g.position.x, 2.75, a.g.position.z); if (!p) continue;
    pops.push({ key: 'tick' + a.id + a.tickAt, html: icon(a.doneKind === 'ok' ? 'check' : a.doneKind === 'soft' ? 'half' : 'cross', 18), cls: `tick ${a.doneKind}`, x: p.x, y: p.y - ease(Math.min(1, age / 0.25)) * 6, o: Math.min(1, (TICK_S - age) / 0.35) });
  }
  sync($('pops'), pops);
  // the windows that are open: what is switched off, for how long (a number, or the Tech's face while he holds the loop)
  const wins = [];
  for (const e of G.world.ents) {
    const w = e.window; if (!w) continue; const p = project(e.g.position.x, e.h + 0.6, e.g.position.z); if (!p) continue;
    const left = w.until ? Math.max(0, Math.ceil(w.until - G.clock)) : null;
    wins.push({ key: 'w' + e.id, html: `${icon(w.icon, 16)}${w.face ? facesHtml([w.face]) : `<b>${left}</b>`}`, cls: `win${w.until && left <= 10 ? ' low' : ''}`, x: p.x, y: p.y - 6 });
  }
  sync($('wins'), wins);
  // history
  const h = G.hist ?? [], unread = h.some((r) => r.unread);
  $('histkey').style.display = h.length ? 'grid' : 'none'; $('histkey').classList.toggle('unread', unread);
  $('hist').style.display = G.histOpen && h.length ? 'block' : 'none';
  if (G.histOpen) { h.forEach((r) => (r.unread = false)); const html = h.map((r) => `<div class="hr ${r.tone}"><span class="ht">${mmss(r.at)}</span>${facesHtml([r.agent])}<span class="hi">${icon(r.cancelled ? 'cross' : r.act.icon, 16)}</span><span class="hw">${r.target ? cap(r.target.name) : ''}</span><span class="hg">${r.bits.join(' · ')}</span></div>`).join(''); if ($('hist').dataset.h !== html) { $('hist').dataset.h = html; $('hist').innerHTML = html; } }
}

// The word on the portrait's plate while the agent has no order: done / turned / refused / not yet / reported / cancelled.
export function donePlate(a) { return on() && real() < (a.doneUntil ?? 0) ? { word: a.doneWord, kind: a.doneKind } : null; }

export function resetGains() { G.chips = []; G.pops = []; G.hist = []; G.valeTaken = false; }
