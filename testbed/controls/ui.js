// The page chrome: preset tabs, toggles, roster, stats, the click log, order timers.
import { G } from './state.js';
import { PRESETS, ROLES, KINDS } from './data.js';
import { icon } from './icons.js';
import { firingNow, fireProgress, syndicate, hpOf, underFire } from './fire.js';
import { orderWords } from './status.js';
import { donePlate } from './gains.js';

const $ = (id) => document.getElementById(id);
const OPTS = [
  ['Marker', 'marker', [['ring', 'Ring and badge'], ['pill', 'Verb pill'], ['badge', 'Badge, then verb'], ['built', 'As built']]],
  ['Camera', 'camMode', [['snap', 'Snap (0.2.34)'], ['leash', 'Leash']]],
  ['View', 'view', [['cmd', 'Commander'], ['tp', 'Third person']]],
  ['Crowd', 'crowd', [['none', 'None'], ['some', 'Some'], ['busy', 'Busy']]],
  ['Commander camera', 'edgePan', [['off', 'Edge pan off'], ['on', 'Edge pan on']]],
  ['Follow lead (C)', 'camFollow', [[false, 'Off'], [true, 'On']]],
  ['Time', 'timeMode', [['total', 'Total'], ['split', 'Walk + act'], ['line', 'Walk on ground']]],
  ['Fights', 'fights', [['syndicate', 'Syndicate'], ['current', 'Current']]],
  ['Third person fire', 'tpAim', [['fire', 'Right hold fires'], ['aim', 'Right aims, left fires']]],
  ['Harmful acts', 'harmHold', [['fire', 'Only Fire holds'], ['all', 'Kill and Knock out hold too']]],
  ['Follow fire', 'followFire', [['with', 'Fire with you'], ['lock', 'Lock your target']]],
  ['Run on the map', 'runMode', [['dbl', 'Double click'], ['shift', 'Shift + click']]],
  ['Acts', 'actCommit', [['release', 'Commit on release'], ['full', 'Commit when full']]],
  ['Squad acts', 'squadAct', [['best', 'Best placed'], ['all', 'Everyone']]],
  ['Third person', 'tpSquad', [['follow', 'Squad follows'], ['hold', 'Squad holds']]],
  ['Orders while paused', 'planMode', [['sync', 'Start together'], ['plain', 'Start as given']]],
  ['Done moment', 'done', [['gain', 'Gains'], ['gated', 'Gains + Handover order'], ['built', 'As built']]],
  ['Act speed', 'actScale', [[1, 'Real time'], [4, '4× faster']]],
];

export function buildChrome(hooks) {
  const draw = () => {
    $('tabs').innerHTML = Object.entries(PRESETS).map(([k, p]) => `<button data-p="${k}" class="${G.preset === k ? 'on' : ''}">${p.label}</button>`).join('') + `<button data-settings class="gear">Settings ${document.body.classList.contains('opts-open') ? '▴' : '▾'}</button>`;
    $('opts').innerHTML = OPTS.filter((o) => !(o[1] === 'fights' && !PRESETS[G.preset].rightFires) && !(o[1] === 'squadAct' && PRESETS[G.preset].holdActs) && !(o[1] === 'actCommit' && !PRESETS[G.preset].holdActs)).map(([label, key, list]) => `<span class="grp"><em>${label}</em>${list.map(([v, t]) => `<button data-o="${key}" data-v="${v}" class="${String(G[key]) === String(v) ? 'on' : ''}">${t}</button>`).join('')}</span>`).join('') + '<button data-pause class="reset">Pause / play</button><button data-vale class="reset">Vale comes out</button><button data-reset class="reset">Reset scene</button>';
    $('about').textContent = PRESETS[G.preset].about;
  };
  document.addEventListener('click', (e) => {
    const b = e.target.closest('button'); if (!b) return; b.blur();
    if (e.target.closest('[data-free]')) { hooks.free(); return draw(); }
    if (b.dataset.p) hooks.preset(b.dataset.p);
    else if (b.dataset.o) hooks.option(b.dataset.o, b.dataset.v);
    else if (b.hasAttribute('data-settings')) document.body.classList.toggle('opts-open');
    else if (b.hasAttribute('data-reset')) hooks.reset();
    else if (b.hasAttribute('data-pause')) hooks.pause();
    else if (b.hasAttribute('data-vale')) hooks.vale();
    else if (b.dataset.agent) hooks.select(+b.dataset.agent, e.shiftKey || e.ctrlKey || e.metaKey);
    else if (b.hasAttribute('data-squad')) hooks.squad();
    else if (b.dataset.gait) hooks.gait(+b.dataset.gait);
    else if (b.dataset.stop) hooks.stop(+b.dataset.stop);
    draw();
  });
  // each portrait has the game's action plate beside it: gait icon (footprints or runner; a click flips the current order only), progress bar, time left, a stop on hover
  $('roster').innerHTML = `<button data-squad class="squad">Squad<span data-free class="free" title="Weapons free (G)">${icon('gun', 14)}</span></button>` + ROLES.map((r, i) => `<div class="agentrow"><button data-agent="${i}" style="--c:${r.color}"><b>${i + 1}</b><span>${r.name}</span><small></small><span class="ufa"><i></i><i></i><i></i></span><img class="pf" src="faces/${r.name.toLowerCase()}_under_fire.png" alt=""></button><div class="aplate" data-plate="${i}"><button data-gait="${i}" class="gi"></button><div class="bar"><i></i></div><span class="val"></span><span class="eye">${icon('eye', 14)}</span><button data-stop="${i}" class="x">×</button></div></div>`).join('');
  $('firemarks').innerHTML = ROLES.map(() => `<div class="fm">${icon('gun', 16)}</div>`).join('');
  $('hpbars').innerHTML = Array.from({ length: 8 }, () => '<div class="hb"><i></i></div>').join('');
  $('ghosts').innerHTML = ROLES.map(() => '<div class="gh"></div>').join(''); $('att').innerHTML = ROLES.map(() => '<div class="at"></div>').join('');
  $('runmarks').innerHTML = ROLES.map(() => `<div class="rm">${icon('run', 18)}</div>`).join('');
  $('orders').innerHTML = ROLES.map(() => '<div class="ord"><i></i><span></span></div>').join('');
  draw(); return draw;
}

export function refresh(project) {
  const w = G.world;
  $('stats').innerHTML = `Trail <b>${G.trail}</b> · Alert <b>${G.alert}</b> · $<b>${G.cash.toLocaleString('en-US')}</b> · <u>${G.paused ? 'paused ' : G.timeScale < 1 ? '¼ speed ' : ''}${Math.floor(G.clock / 60)}:${String(Math.floor(G.clock % 60)).padStart(2, '0')}</u> · <span id="fps"></span>`;
  [...$('roster').querySelectorAll('[data-agent]')].forEach((b, i) => {
    const a = w.agents[i], o = a.order; b.classList.toggle('sel', G.group[i]); b.classList.toggle('lead', G.group[i] && G.group.filter(Boolean).length > 1 && i === G.sel);
    b.classList.toggle('ready', !!G.readyIdx?.includes(i)); b.classList.toggle('break', !!G.pv?.lead.includes(i)); b.classList.toggle('hold', !!G.pv?.holds.includes(i));
    b.querySelector('small').textContent = ''; b.classList.toggle('busy', !G.group[i] && o?.kind === 'act'); // on an act and out of the squad
    // the action plate: shown only while the agent has an order (as the game's chip is)
    const pl = $('roster').querySelector(`[data-plate="${i}"]`), moving = o && (o.kind === 'walk' || o.phase === 'walk'), run = !!o?.run;
    const q = G.plan.get(a.id);
    const dp = !o ? donePlate(a) : null; pl.dataset.dn = dp ? dp.kind : ''; b.classList.toggle('doneflash', !!dp && dp.kind !== 'cancel'); // brief 043: the plate says done / turned / refused / cancelled for a moment
    if (q && !a.order) { const p2 = $('roster').querySelector(`[data-plate="${i}"]`); p2.style.display = 'flex'; p2.classList.remove('run', 'fire'); const gi = p2.querySelector('.gi'); gi.innerHTML = icon(q.icon, 18); gi.dataset.ic = q.icon; p2.querySelector('.bar i').style.width = '0%'; p2.querySelector('.val').textContent = 'queued'; p2.querySelector('.eye').style.display = 'none'; return; }
    // under fire: the portrait pulses and shows its hurt face with ticks coming in from the shooter's side; the plate says so
    const uf = underFire(a); b.classList.toggle('underfire', uf); pl.classList.toggle('uf', uf);
    if (uf) { const A = project(a.g.position.x, 0, a.g.position.z), S = project(a.fireFrom.g.position.x, 0, a.fireFrom.g.position.z); if (A && S) b.style.setProperty('--ang', `${Math.round(Math.atan2(S.y - A.y, S.x - A.x) * 180 / Math.PI)}deg`); }
    const fr = firingNow(a); b.classList.toggle('firing', fr); pl.classList.toggle('fire', fr && !o);
    const tf = G.clock < (a.tooFarUntil ?? 0); pl.style.display = o || fr || uf || tf || dp ? 'flex' : 'none';
    if (tf && !uf) { pl.classList.remove('run'); pl.querySelector('.gi').innerHTML = icon('gun', 18); pl.querySelector('.gi').dataset.ic = 'gun'; pl.querySelector('.bar i').style.width = '100%'; pl.querySelector('.val').textContent = 'too far'; pl.querySelector('.eye').style.display = 'none'; return; }
    if (uf && !o) { pl.classList.remove('run'); pl.querySelector('.gi').innerHTML = icon('siren', 18); pl.querySelector('.gi').dataset.ic = 'siren'; pl.querySelector('.bar i').style.width = '100%'; pl.querySelector('.val').textContent = 'Under fire'; pl.querySelector('.eye').style.display = 'none'; return; }
    if (!o && fr) { pl.classList.remove('run'); pl.querySelector('.gi').innerHTML = icon('gun', 18); pl.querySelector('.gi').dataset.ic = 'gun'; const fp = fireProgress(a); pl.querySelector('.bar i').style.width = `${Math.max(0, Math.min(1, fp.frac)) * 100}%`; pl.querySelector('.val').textContent = fp.word; pl.querySelector('.eye').style.display = 'none'; return; }
    if (dp) { pl.classList.remove('run', 'fire'); const gi = pl.querySelector('.gi'), n = dp.kind === 'ok' ? 'check' : dp.kind === 'soft' ? 'half' : 'cross'; if (gi.dataset.ic !== n) { gi.dataset.ic = n; gi.innerHTML = icon(n, 18); } pl.querySelector('.bar i').style.width = dp.kind === 'cancel' ? '0%' : '100%'; pl.querySelector('.val').textContent = dp.word; pl.querySelector('.eye').style.display = 'none'; return; }
    if (!o) return;
    const rem = moving ? w.nav.length(a.g.position.x, a.g.position.z, o.path) : 0, left = moving ? rem / (run ? 2.8 : 1.4) : o.dur ? (o.dur - o.t) * G.actScale : 0;
    const frac = moving ? (o.len0 > 0 ? 1 - rem / o.len0 : 0) : o.dur ? o.t / o.dur : 1;
    pl.classList.toggle('run', run); { const gi = pl.querySelector('.gi'), n = moving ? (run ? 'run' : 'walk') : o.act.icon; if (gi.dataset.ic !== n) { gi.dataset.ic = n; gi.innerHTML = icon(n, 18); } } // only when it changes, so a click lands on the same element
    pl.querySelector('.bar i').style.width = `${Math.max(0, Math.min(1, frac)) * 100}%`;
    pl.querySelector('.val').textContent = uf ? 'Under fire' : orderWords(a); pl.querySelector('.eye').style.display = run ? 'inline-grid' : 'none';
  });
  [...$('runmarks').children].forEach((d, i) => { // the run mark over a running agent
    const a = w.agents[i], p = a.speed > 2 ? project(a.g.position.x, 2.5, a.g.position.z) : null;
    d.style.display = p ? 'grid' : 'none'; if (p) d.style.transform = `translate(${Math.round(p.x)}px,${Math.round(p.y)}px)`;
  });
  [...$('firemarks').children].forEach((d, i) => { const a = w.agents[i], p = firingNow(a) ? project(a.g.position.x, 2.5, a.g.position.z) : null; d.style.display = p ? 'grid' : 'none'; if (p) d.style.transform = `translate(${Math.round(p.x)}px,${Math.round(p.y)}px)`; });
  const fr = $('roster').querySelector('.free'); fr.style.display = syndicate() ? 'grid' : 'none'; fr.classList.toggle('on', !!G.free);
  { const bars = [...$('hpbars').children], list = w.ents.filter((e) => e.hostile ? !['out', 'dead', 'down'].includes(e.state) && (G.clock < G.fightUntil || e.hit) : e.hit && !['out', 'dead', 'down'].includes(e.state)).slice(0, bars.length);
    bars.forEach((d, i) => { const e = list[i], p = e && syndicate() ? project(e.g.position.x, e.h + 0.45, e.g.position.z) : null; d.style.display = p ? 'block' : 'none'; if (!p) return;
      const { hp, max } = hpOf(e); d.style.transform = `translate(${Math.round(p.x)}px,${Math.round(p.y)}px)`; d.style.setProperty('--n', max); d.firstChild.style.width = `${Math.max(0, hp) / max * 100}%`; d.classList.toggle('hit', G.clock - (e.hitAt ?? -9) < 0.3); d.classList.toggle('shooting', !!e.alerted); }); }
  [...$('att').children].forEach((d, i) => { // the attention clock over an agent standing in a cone
    const a = w.agents[i], p = a.seenT > 0.5 ? project(a.g.position.x, 2.7, a.g.position.z) : null; d.style.display = p ? 'flex' : 'none'; if (!p) return;
    const t = a.seenT, word = t >= 45 ? 'report' : t >= 20 ? 'wary' : t >= 8 ? 'noticed' : 'seen';
    d.style.transform = `translate(${Math.round(p.x)}px,${Math.round(p.y)}px)`; d.style.setProperty('--f', `${Math.min(1, t / 45) * 360}deg`); d.dataset.w = word; d.textContent = word;
  });
  const v = G.vale, tb = $('test'); tb.style.display = v ? 'block' : 'none';
  if (v) tb.textContent = v.done ? (v.sim <= 0.5 ? `All three locked in ${v.sim.toFixed(2)} s of game time · it works` : `All three locked in ${v.sim.toFixed(2)} s of game time · too slow, half a second is the aim`) + ` (${v.real.toFixed(1)} s on the clock)` : `Vale is out · ${v.n} of 3 agents locked · ${v.sim.toFixed(2)} s of game time`;
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
