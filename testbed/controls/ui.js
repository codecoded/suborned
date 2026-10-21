// The page chrome: preset tabs, toggles, roster, stats, the click log, order timers.
import { G } from './state.js';
import { PRESETS, ROLES, KINDS } from './data.js';
import { icon } from './icons.js';

const $ = (id) => document.getElementById(id);
const OPTS = [
  ['Marker', 'marker', [['ring', 'Ring and badge'], ['pill', 'Verb pill'], ['badge', 'Badge, then verb'], ['built', 'As built']]],
  ['Camera', 'camMode', [['snap', 'Snap (0.2.34)'], ['leash', 'Leash']]],
  ['View', 'view', [['cmd', 'Commander'], ['tp', 'Third person']]],
  ['Time', 'timeMode', [['total', 'Total'], ['split', 'Walk + act'], ['line', 'Walk on ground']]],
  ['Squad acts', 'squadAct', [['best', 'Best placed'], ['all', 'Everyone']]],
  ['Third person', 'tpSquad', [['follow', 'Squad follows'], ['hold', 'Squad holds']]],
  ['Acts', 'actScale', [[1, 'Real time'], [4, '4× faster']]],
];

export function buildChrome(hooks) {
  const draw = () => {
    $('tabs').innerHTML = Object.entries(PRESETS).map(([k, p]) => `<button data-p="${k}" class="${G.preset === k ? 'on' : ''}">${p.label}</button>`).join('');
    $('opts').innerHTML = OPTS.map(([label, key, list]) => `<span class="grp"><em>${label}</em>${list.map(([v, t]) => `<button data-o="${key}" data-v="${v}" class="${String(G[key]) === String(v) ? 'on' : ''}">${t}</button>`).join('')}</span>`).join('') + '<button data-reset class="reset">Reset scene</button>';
    $('about').textContent = PRESETS[G.preset].about;
  };
  document.addEventListener('click', (e) => {
    const b = e.target.closest('button'); if (!b) return; b.blur();
    if (b.dataset.p) hooks.preset(b.dataset.p);
    else if (b.dataset.o) hooks.option(b.dataset.o, b.dataset.v);
    else if (b.hasAttribute('data-reset')) hooks.reset();
    else if (b.dataset.agent) hooks.select(+b.dataset.agent, e.shiftKey || e.ctrlKey || e.metaKey);
    else if (b.hasAttribute('data-squad')) hooks.squad();
    draw();
  });
  $('roster').innerHTML = '<button data-squad class="squad">Squad</button>' + ROLES.map((r, i) => `<button data-agent="${i}" style="--c:${r.color}"><b>${i + 1}</b><span>${r.name}</span><small></small></button>`).join('');
  $('orders').innerHTML = ROLES.map(() => '<div class="ord"><i></i><span></span></div>').join('');
  draw(); return draw;
}

export function refresh(project) {
  const w = G.world;
  $('stats').innerHTML = `Trail <b>${G.trail}</b> · Alert <b>${G.alert}</b> · $<b>${G.cash.toLocaleString('en-US')}</b> · <u>${Math.floor(G.clock / 60)}:${String(Math.floor(G.clock % 60)).padStart(2, '0')}</u> · <span id="fps"></span>`;
  [...$('roster').querySelectorAll('[data-agent]')].forEach((b, i) => {
    const a = w.agents[i], o = a.order; b.classList.toggle('sel', G.group[i]); b.classList.toggle('lead', G.group[i] && G.group.filter(Boolean).length > 1 && i === G.sel);
    b.querySelector('small').textContent = !o ? '' : o.kind === 'walk' ? 'walking' : o.phase === 'walk' ? 'walking' : o.act.fire ? 'firing' : `${Math.max(0, Math.round((o.dur - o.t) * G.actScale))} s`;
  });
  [...$('orders').children].forEach((d, i) => {
    const a = w.agents[i], o = a.order, show = o && o.kind === 'act';
    d.style.display = show ? 'flex' : 'none'; if (!show) return;
    const p = project(a.g.position.x, 2.3, a.g.position.z); if (!p) { d.style.display = 'none'; return; }
    const k = KINDS[o.act.kind], f = o.phase === 'act' ? Math.min(1, o.t / o.dur) : 0;
    d.style.transform = `translate(${Math.round(p.x)}px,${Math.round(p.y)}px)`; d.style.setProperty('--c', k.color);
    d.firstChild.style.width = `${f * 100}%`;
    d.lastChild.innerHTML = `${icon(o.act.icon, 16)} ${o.act.bare ? o.act.verb : o.act.verb} · ${o.phase === 'walk' ? 'walking' : Math.max(0, Math.round((o.dur - o.t) * G.actScale)) + ' s'}`;
  });
  const lines = G.log.slice(-8), now = performance.now();
  $('log').innerHTML = lines.map((l) => `<div class="${l.tone}" style="opacity:${Math.max(0.35, 1 - (now - l.at) / 20000)}">${l.text}</div>`).join('');
}
export const setHint = (t) => { const h = $('hint'); if (h.textContent !== t) h.textContent = t; h.style.opacity = t ? 1 : 0; };
export const setFps = (f) => { const e = $('fps'); if (e) e.textContent = `${f} fps`; };
