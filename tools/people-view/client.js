// The viewer's behaviour: tabs, the roster's filters and sort, the day scrubber on the map, the tie graph's filters and path finding, the character sheet.
// It reads the same data the page was built from (the `data` element) and draws nothing the data does not say. No network, no clock: the day runs on the scrubber.
(function () {
'use strict';
const D = JSON.parse(document.getElementById('data').textContent);
const hide = (el, on) => el.toggleAttribute('hidden', !!on);
const $ = (s, r) => (r || document).querySelector(s), $$ = (s, r) => [...(r || document).querySelectorAll(s)];
const esc = x => String(x).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
const cap = s => s.charAt(0).toUpperCase() + s.slice(1);
const pad = n => String(n).padStart(2, '0');
const hm = m => pad(Math.floor(m / 60) % 24) + ':' + pad(Math.round(m) % 60);
const hms = s => { s = ((Math.round(s) % 86400) + 86400) % 86400; return pad(Math.floor(s / 3600)) + ':' + pad(Math.floor(s / 60) % 60) + ':' + pad(s % 60); };
const usd = n => '$' + Number(n).toLocaleString('en-US');
const P = new Map(D.persons.map(p => [p.id, p]));
const nm = id => (P.get(id) ? P.get(id).name : id);
const link = id => '<button class="lnk" data-open="' + esc(id) + '">' + esc(nm(id)) + '</button>';
const placeName = id => (D.places[id] ? D.places[id].name : id);
const uName = u => (u === 'resident' ? 'Residents' : ((D.units.find(x => x.id === u) || {}).name || u));
const tName = t => (D.rules.temperaments[t] ? D.rules.temperaments[t].name : t);
const TABS = $$('.tab-btn').map(b => b.dataset.go);
const st = { tab: 'overview', sel: null, hist: [], T: parseHMS(D.mission.startsAt), playing: false, speed: 60, names: false };

function parseHMS(s) { const a = s.split(':').map(Number); return a[0] * 3600 + a[1] * 60 + (a[2] || 0); }

// ------------------------------------------------------------------ hash
function readHash() {
  const h = location.hash.replace(/^#/, ''), [tab, q] = h.split('?'), sp = new URLSearchParams(q || '');
  return { tab: TABS.includes(tab) ? tab : null, p: sp.get('p'), t: sp.get('t'), a: sp.get('a'), b: sp.get('b'), any: sp.get('any'), q: sp.get('q') };
}
function writeHash() {
  const sp = new URLSearchParams();
  if (st.sel) sp.set('p', st.sel);
  if (st.tab === 'map') sp.set('t', String(Math.round(st.T)));
  if (st.tab === 'graph') { if ($('#pa').value) sp.set('a', $('#pa').value); if ($('#pb').value) sp.set('b', $('#pb').value); if ($('#pany').checked) sp.set('any', '1'); }
  const q = $('#q').value; if (q) sp.set('q', q);
  const s = '#' + st.tab + (sp.toString() ? '?' + sp.toString() : '');
  if (s !== location.hash) history.replaceState(null, '', s);
}

// ------------------------------------------------------------------ tabs
function go(tab) {
  if (!TABS.includes(tab)) return;
  st.tab = tab;
  $$('.tab').forEach(s => (s.hidden = s.dataset.tab !== tab));
  $$('.tab-btn').forEach(b => b.setAttribute('aria-selected', String(b.dataset.go === tab)));
  if (tab === 'map') drawMap();
  if (tab !== 'map') setPlaying(false);
  writeHash();
}
document.addEventListener('click', e => {
  const t = e.target.closest('[data-go]'); if (t) { go(t.dataset.go); return; }
  const o = e.target.closest('[data-open]'); if (o) { openSheet(o.dataset.open); return; }
  const f = e.target.closest('[data-filter]'); if (f) { applyFilterFromBar(f.dataset.filter); }
});

// ------------------------------------------------------------------ roster
const R = { sets: {}, sort: { col: null, dir: 1 } };
const FKEY = { unit: 'unit', tem: 'tem', need: 'need', pos: 'pos', shift: 'shift', role: 'role', talker: 'talker', diff: 'diff' };
function filterRows() {
  const q = $('#q').value.trim().toLowerCase(), toks = q ? q.split(/\s+/) : [];
  let n = 0;
  $$('#roster tbody tr').forEach(tr => {
    let ok = toks.every(t => tr.dataset.q.includes(t));
    for (const k in R.sets) {
      const set = R.sets[k]; if (!set || !set.size) continue;
      const v = k === 'role' ? tr.dataset.role : tr.dataset[FKEY[k]];
      if (k === 'role') { if (!(set.has('any') ? tr.dataset.role : false) && !set.has(v)) ok = false; } else if (!set.has(v)) ok = false;
    }
    tr.hidden = !ok; if (ok) n++;
  });
  $('#rcount').textContent = n + ' of ' + D.persons.length;
}
function setChip(el, on) { el.setAttribute('aria-pressed', String(on)); }
function toggleChip(el) {
  const [k, v] = el.dataset.f.split(':'); (R.sets[k] = R.sets[k] || new Set());
  const on = el.getAttribute('aria-pressed') !== 'true';
  if (on) R.sets[k].add(v); else R.sets[k].delete(v);
  setChip(el, on); filterRows();
}
function clearFilters() { R.sets = {}; $$('#tab-roster [data-f]').forEach(c => setChip(c, false)); $('#q').value = ''; filterRows(); writeHash(); }
function applyFilterFromBar(spec) {
  const [dim, val] = spec.split(':');
  clearFilters();
  const map = { unit: 'unit', temperament: 'tem', need: 'need', position: 'pos', shift: 'shift' };
  if (map[dim]) { const c = $('#tab-roster [data-f="' + map[dim] + ':' + CSS.escape(val) + '"]') || $$('#tab-roster [data-f]').find(x => x.dataset.f === map[dim] + ':' + val); if (c) toggleChip(c); } else { $('#q').value = val; filterRows(); }
  go('roster');
}
$$('#tab-roster [data-f]').forEach(c => { c.addEventListener('click', () => toggleChip(c)); c.addEventListener('keydown', e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); toggleChip(c); } }); });
$('#rclear').addEventListener('click', clearFilters);
$('#q').addEventListener('input', () => { if (st.tab !== 'roster' && $('#q').value) go('roster'); filterRows(); writeHash(); });
$$('#roster th').forEach((th, i) => th.addEventListener('click', () => {
  const dir = R.sort.col === i ? -R.sort.dir : 1; R.sort = { col: i, dir };
  $$('#roster th').forEach(x => x.removeAttribute('aria-sort')); th.setAttribute('aria-sort', dir > 0 ? 'ascending' : 'descending');
  const tb = $('#roster tbody'), rows = [...tb.rows].map((r, k) => ({ r, k }));
  const key = r => { const c = r.cells[i]; return c.dataset.n !== undefined ? Number(c.dataset.n) : (c.dataset.s !== undefined ? c.dataset.s : c.textContent); };
  rows.sort((a, b) => { const x = key(a.r), y = key(b.r); return (typeof x === 'number' ? x - y : String(x).localeCompare(String(y), 'en', { numeric: true })) * dir || a.k - b.k; });
  rows.forEach(x => tb.appendChild(x.r));
}));
$('#roster tbody').addEventListener('click', e => { const tr = e.target.closest('tr'); if (tr && !e.target.closest('button')) openSheet(tr.dataset.p); });
$('#roster tbody').addEventListener('keydown', e => {
  const tr = e.target.closest('tr'); if (!tr) return;
  if (e.key === 'Enter') { openSheet(tr.dataset.p); e.preventDefault(); }
  if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
    let n = tr; do { n = e.key === 'ArrowDown' ? n.nextElementSibling : n.previousElementSibling; } while (n && n.hidden);
    if (n) { n.focus(); e.preventDefault(); }
  }
});

// ------------------------------------------------------------------ the day, on the map
const B = D.map.bounds, MAP = $('#map');
const sOf = pl => (D.places[pl] && D.places[pl].s) || null;
const inBounds = s => s[0] >= B[0] - 1 && s[0] <= B[2] + 1 && s[1] >= B[1] - 1 && s[1] <= B[3] + 1;
function posAt(p, t) {
  const stops = p.stops; let prev = null, next = null;
  const here = s => {
    const place = s[0], doing = s[1];
    if (doing === 'home') return { k: 'home', doing, place };
    if (place === 'beyond') return { k: 'beyond', doing, place };
    if (p.post === 'inside' && place === p.workPlace && (doing === 'work' || doing === 'meal')) return { k: 'inside', doing, place };
    const xy = sOf(place); if (!xy || !inBounds(xy)) return { k: 'off', doing, place };
    return { k: 'map', xy, doing, place };
  };
  for (const s of stops) { if (t >= s[2] && t < s[3]) return here(s); if (s[3] <= t) prev = s; else if (!next) next = s; }
  if (prev && next) {
    const a = sOf(prev[0]), b = sOf(next[0]);
    if (!a || !b || prev[0] === 'beyond' || next[0] === 'beyond') return { k: 'off', doing: 'walking', place: next[0] };
    const f = (t - prev[3]) / (next[2] - prev[3]), xy = [a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f];
    return inBounds(xy) ? { k: 'map', xy, doing: 'walking', place: next[0], walking: true } : { k: 'off', doing: 'walking', place: next[0] };
  }
  return here(prev || next);
}
const dots = new Map($$('#L-dots .dot').map(d => [d.dataset.p, d]));
const lbls = new Map($$('#L-dots [data-lbl]').map(d => [d.dataset.lbl, d]));
const nameLayer = $('#L-labels');
function drawMap() {
  if (st.tab !== 'map' && !drawMap.force) return;
  const t = st.T / 60, buckets = new Map(), live = { map: 0, inside: 0, home: 0, off: 0, beyond: 0, walking: 0, seats: 0 };
  const cur = {};
  for (const p of D.persons) {
    const s = posAt(p, t); cur[p.id] = s;
    if (s.k === 'map') { const key = Math.round(s.xy[0] * 2) + ',' + Math.round(s.xy[1] * 2); (buckets.get(key) || buckets.set(key, []).get(key)).push(p.id); live.map++; if (s.walking) live.walking++; const pk = D.places[s.place]; if (pk && (pk.kind === 'seat' || pk.kind === 'stand')) live.seats++; } else live[s.k]++;
  }
  for (const [id, d] of dots) { const s = cur[id], off = s.k !== 'map'; hide(d, off); if (!off) { d.__xy = s.xy; d.__s = s; } const l = lbls.get(id); if (l) hide(l, off); }
  for (const ids of buckets.values()) {
    ids.sort(); // by id: the same dot takes the same slot
    ids.forEach((id, i) => { const d = dots.get(id), r = i ? 1.15 * Math.sqrt(i) : 0, a = i * 2.39996323; d.setAttribute('cx', (d.__xy[0] + r * Math.cos(a)).toFixed(2)); d.setAttribute('cy', (d.__xy[1] + r * Math.sin(a)).toFixed(2)); d.__tip = cur[id]; });
  }
  for (const [id, l] of lbls) { const d = dots.get(id); if (!d.hasAttribute('hidden')) { l.setAttribute('x', d.getAttribute('cx')); l.setAttribute('y', (parseFloat(d.getAttribute('cy')) - 1.3).toFixed(2)); } }
  if (st.names) {
    nameLayer.innerHTML = '';
    for (const [id, d] of dots) if (!d.hasAttribute('hidden') && !lbls.has(id)) { const tx = document.createElementNS('http://www.w3.org/2000/svg', 'text'); tx.setAttribute('class', 'nm'); tx.setAttribute('x', d.getAttribute('cx')); tx.setAttribute('y', (parseFloat(d.getAttribute('cy')) - 1.1).toFixed(2)); tx.textContent = nm(id).split(' ')[0]; nameLayer.appendChild(tx); }
  }
  $('#clk').textContent = hms(st.T);
  const m = Math.round(st.T - D.mission.startMin * 60), neg = m < 0, a = Math.abs(m);
  $('#mclk').textContent = 'mission ' + (neg ? '−' : '') + Math.floor(a / 60) + ':' + pad(a % 60) + '  (0:00 = ' + D.mission.startsAt + ')';
  $('#scrub').value = String(Math.round(st.T));
  $('#live').innerHTML = [['Drawn on the block', live.map], ['  of them walking', live.walking], ['  at a café chair or standing point', live.seats], ['Inside a building (at their desk)', live.inside], ['At home', live.home], ['Beyond the sheet or off the map', live.off + live.beyond]].map(x => '<span>' + esc(x[0]) + '</span><b>' + x[1] + '</b>').join('');
  drawSelPath();
  $$('#L-dots .dot').forEach(d => d.classList.toggle('sel', d.dataset.p === st.sel));
}
function drawSelPath() {
  const pl = $('#selpath'); if (!st.sel) { pl.setAttribute('points', ''); return; }
  const p = P.get(st.sel), pts = []; let last = '';
  for (const s of p.stops) { const xy = sOf(s[0]); if (!xy || s[0] === last || s[1] === 'home' || !inBounds(xy)) continue; last = s[0]; pts.push(xy[0] + ',' + xy[1]); }
  pl.setAttribute('points', pts.join(' '));
}
function setTime(sec) { st.T = ((sec % 86400) + 86400) % 86400; drawMap(); writeHash(); }
let raf = 0, lastTs = 0;
function frame(ts) { if (!st.playing) return; const dt = Math.min(0.1, (ts - lastTs) / 1000); lastTs = ts; st.T = (st.T + dt * st.speed) % 86400; drawMap(); raf = requestAnimationFrame(frame); }
function setPlaying(on) { if (on === st.playing) return; st.playing = on; const b = $('#play'); b.textContent = on ? '❚❚ Pause' : '▶ Play'; if (on) { lastTs = performance.now(); raf = requestAnimationFrame(frame); } else { cancelAnimationFrame(raf); writeHash(); } }
$('#play').addEventListener('click', () => setPlaying(!st.playing));
$('#speed').addEventListener('change', e => (st.speed = Number(e.target.value)));
$('#scrub').addEventListener('input', e => setTime(Number(e.target.value)));
$$('[data-jump]').forEach(b => b.addEventListener('click', () => { setPlaying(false); setTime(parseHMS(b.dataset.jump)); }));
$$('[data-layer]').forEach(c => c.addEventListener('change', () => { const g = $('#L-' + c.dataset.layer); if (g) hide(g, !c.checked); }));
$('#names').addEventListener('change', e => { st.names = e.target.checked; if (!st.names) nameLayer.innerHTML = ''; drawMap(); });
$$('input[name=colour]').forEach(r => r.addEventListener('change', () => { const tem = r.value === 'tem' && r.checked; if (r.checked) { MAP.classList.toggle('tem', tem); hide($('#legend-unit'), tem); hide($('#legend-tem'), !tem); } }));
const mc = $('#mark-chg'); if (mc) mc.addEventListener('change', () => MAP.classList.toggle('nochg', !mc.checked));
const tip = $('#tip');
function showTip(html, x, y) { tip.innerHTML = html; tip.hidden = false; const w = tip.offsetWidth; tip.style.left = Math.min(x + 14, innerWidth - w - 8) + 'px'; tip.style.top = (y + 16) + 'px'; }
MAP.addEventListener('mousemove', e => {
  const d = e.target.closest('.dot'); if (!d) { tip.hidden = true; return; }
  const p = P.get(d.dataset.p), s = d.__tip || d.__s || {};
  showTip('<b>' + esc(p.name) + '</b><br><small>' + esc(p.position) + ' · ' + esc(uName(p.unit)) + '<br>' + esc(s.doing || '') + (s.place ? ' at ' + esc(placeName(s.place)) : '') + '</small>', e.clientX, e.clientY);
});
MAP.addEventListener('mouseleave', () => (tip.hidden = true));
MAP.addEventListener('click', e => { const d = e.target.closest('.dot'); if (d && !MAP.__dragged) openSheet(d.dataset.p); });

// ------------------------------------------------------------------ pan and zoom (map, graph)
function panzoom(svg) {
  const base = svg.dataset.vb.split(' ').map(Number); let vb = base.slice(), drag = null;
  const apply = () => { svg.setAttribute('viewBox', vb.map(n => +n.toFixed(2)).join(' ')); };
  svg.addEventListener('wheel', e => {
    e.preventDefault();
    const r = svg.getBoundingClientRect(), k = Math.exp(e.deltaY * 0.0015), mx = vb[0] + ((e.clientX - r.left) / r.width) * vb[2], my = vb[1] + ((e.clientY - r.top) / r.height) * vb[3];
    const nw = Math.min(base[2] * 1.5, Math.max(base[2] / 12, vb[2] * k)), f = nw / vb[2];
    vb = [mx - (mx - vb[0]) * f, my - (my - vb[1]) * f, nw, vb[3] * f]; apply();
  }, { passive: false });
  svg.addEventListener('pointerdown', e => { if (e.button !== 0) return; drag = { x: e.clientX, y: e.clientY, vb: vb.slice(), moved: false }; });
  window.addEventListener('pointermove', e => {
    if (!drag) return; const r = svg.getBoundingClientRect(), dx = e.clientX - drag.x, dy = e.clientY - drag.y;
    if (!drag.moved && Math.hypot(dx, dy) < 4) return; drag.moved = true; svg.classList.add('drag'); svg.__dragged = true;
    const s = Math.max(drag.vb[2] / r.width, drag.vb[3] / r.height); vb = [drag.vb[0] - dx * s, drag.vb[1] - dy * s, drag.vb[2], drag.vb[3]]; apply();
  });
  window.addEventListener('pointerup', () => { if (drag && drag.moved) setTimeout(() => (svg.__dragged = false), 0); drag = null; svg.classList.remove('drag'); });
  svg.addEventListener('dblclick', () => { vb = base.slice(); apply(); });
}
panzoom(MAP); panzoom($('#graph'));

// ------------------------------------------------------------------ the tie graph
const G = $('#graph'), gnodes = new Map($$('#nodes .n').map(n => [n.dataset.p, n])), gedges = $$('#edges .e');
const gu = new Set(D.ui.groups), gk = new Set(Object.keys(D.rules.ties));
function applyGraphFilter() {
  for (const [id, n] of gnodes) n.style.display = gu.has(P.get(id).unit) ? '' : 'none';
  for (const e of gedges) e.style.display = gk.has(e.dataset.k) && gu.has(P.get(e.dataset.a).unit) && gu.has(P.get(e.dataset.b).unit) ? '' : 'none';
}
$$('[data-gu]').forEach(c => { const flip = () => { const on = !gu.has(c.dataset.gu); if (on) gu.add(c.dataset.gu); else gu.delete(c.dataset.gu); setChip(c, on); applyGraphFilter(); }; c.addEventListener('click', flip); c.addEventListener('keydown', e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); flip(); } }); });
$$('[data-gk]').forEach(c => c.addEventListener('change', () => { if (c.checked) gk.add(c.dataset.gk); else gk.delete(c.dataset.gk); applyGraphFilter(); }));
$('#g-labels').addEventListener('change', e => $$('#nodes .n text').forEach(t => hide(t, !e.target.checked && !t.parentNode.classList.contains('role'))));
$('#nodes').addEventListener('click', e => { const n = e.target.closest('.n'); if (!n || G.__dragged) return; if (e.shiftKey) { setEnd(n.dataset.p); return; } openSheet(n.dataset.p); });
const tips = id => { const p = P.get(id); return '<b>' + esc(p.name) + '</b><br><small>' + esc(p.position) + ' · ' + esc(uName(p.unit)) + ' · ' + p.ties.length + ' ties held</small>'; };
$('#nodes').addEventListener('mousemove', e => { const n = e.target.closest('.n'); if (!n) { tip.hidden = true; return; } showTip(tips(n.dataset.p), e.clientX, e.clientY); });
$('#nodes').addEventListener('mouseleave', () => (tip.hidden = true));
// who can introduce whom: a path along ties that introduce, each from the person who holds it; or any tie, either way
const adj = { intro: new Map(), any: new Map() };
for (const e of D.graph.edges) {
  const add = (m, a, b) => (m.get(a) || m.set(a, []).get(a)).push({ to: b, kind: e.kind, at: e.at });
  const introduces = D.rules.ties[e.kind].introduces;
  if (introduces) { add(adj.intro, e.a, e.b); if (!e.dir) add(adj.intro, e.b, e.a); }
  add(adj.any, e.a, e.b); add(adj.any, e.b, e.a);
}
function bfs(a, b, any) {
  const g = any ? adj.any : adj.intro, prev = new Map([[a, null]]), q = [a];
  while (q.length) { const x = q.shift(); if (x === b) break; for (const y of (g.get(x) || [])) if (!prev.has(y.to)) { prev.set(y.to, { from: x, kind: y.kind, at: y.at }); q.push(y.to); } }
  if (!prev.has(b)) return null;
  const path = []; for (let x = b; prev.get(x); x = prev.get(x).from) path.unshift({ from: prev.get(x).from, to: x, kind: prev.get(x).kind, at: prev.get(x).at });
  return path;
}
const idOf = v => { v = v.trim(); if (P.has(v)) return v; const m = D.persons.filter(p => p.name.toLowerCase() === v.toLowerCase()); return m.length === 1 ? m[0].id : null; };
function clearPath() { G.classList.remove('hl'); $$('#graph .on').forEach(x => x.classList.remove('on')); $('#pathout').hidden = true; }
function drawPath() {
  clearPath(); const a = idOf($('#pa').value), b = idOf($('#pb').value), out = $('#pathout');
  if (!a || !b) { if ($('#pa').value || $('#pb').value) { out.hidden = false; out.textContent = !a && $('#pa').value ? 'No one is called that (use a name or an id).' : !b && $('#pb').value ? 'No one is called that (use a name or an id).' : 'Choose the other end.'; } writeHash(); return; }
  const any = $('#pany').checked; let path = bfs(a, b, any), dir = 'forward';
  out.hidden = false;
  if (a === b) { out.textContent = 'The same person.'; return; }
  if (!path) {
    const back = bfs(b, a, any);
    out.innerHTML = '<b>No path from ' + esc(nm(a)) + ' to ' + esc(nm(b)) + '</b> along ' + (any ? 'any tie' : 'ties that introduce') + '.' + (back ? ' There is one the other way (swap).' : (any ? '' : ' Try “any tie”.'));
    writeHash(); return;
  }
  G.classList.add('hl');
  gnodes.get(a).classList.add('on'); path.forEach(s => { gnodes.get(s.to).classList.add('on'); const e = gedges.find(x => x.dataset.k === s.kind && ((x.dataset.a === s.from && x.dataset.b === s.to) || (x.dataset.b === s.from && x.dataset.a === s.to))); if (e) e.classList.add('on'); });
  out.innerHTML = '<b>' + path.length + ' ' + (path.length === 1 ? 'introduction' : 'introductions') + '</b> · ' + link(a) + path.map(s => ' <small>—' + esc(s.kind) + (s.at ? ' (' + esc(s.at) + ')' : '') + '→</small> ' + link(s.to)).join('');
  writeHash();
}
function setEnd(id) { if (!$('#pa').value || ($('#pa').value && $('#pb').value)) { $('#pa').value = id; $('#pb').value = ''; } else $('#pb').value = id; drawPath(); }
['pa', 'pb'].forEach(i => $('#' + i).addEventListener('change', drawPath)); $('#pany').addEventListener('change', drawPath);
$('#pswap').addEventListener('click', () => { const t = $('#pa').value; $('#pa').value = $('#pb').value; $('#pb').value = t; drawPath(); });
$('#pclear').addEventListener('click', () => { $('#pa').value = ''; $('#pb').value = ''; clearPath(); writeHash(); });

// ------------------------------------------------------------------ the character sheet
const DOING = { home: '#e1d9c6', commute: '#9fb8c7', work: '#14817f', meal: '#67a85f', 'pre-shift': '#3b6fb6', evening: '#c07a4d', errand: '#7a8a99' };
function timeline(p) {
  const pc = m => (100 * m / 1440).toFixed(3) + '%', wd = (a, b) => (100 * Math.max(0.25, b - a) / 1440).toFixed(3) + '%';
  let html = '';
  const stops = p.stops.slice().sort((a, b) => a[2] - b[2]); let end = 0;
  for (const s of stops) {
    if (s[2] > end) html += '<i class="walk" style="left:' + pc(end) + ';width:' + wd(end, s[2]) + '" title="walking ' + hm(end) + '–' + hm(s[2]) + '"></i>';
    const inside = p.post === 'inside' && s[0] === p.workPlace && (s[1] === 'work' || s[1] === 'meal'), seat = D.places[s[0]] && (D.places[s[0]].kind === 'seat' || D.places[s[0]].kind === 'stand');
    html += '<i class="' + (seat ? 'seat' : '') + '" style="left:' + pc(s[2]) + ';width:' + wd(s[2], s[3]) + ';background:' + (DOING[s[1]] || '#999') + '" title="' + hm(s[2]) + '–' + hm(s[3]) + ' ' + esc(s[1]) + ' at ' + esc(placeName(s[0])) + (inside ? ' (inside)' : '') + '"></i>';
    end = Math.max(end, s[3]);
  }
  const mb = [[D.mission.startMin, D.mission.end, 'the Handover, ' + D.mission.startsAt, '']]; if (p.role) { const r = D.roles.find(x => x.id === p.role); if (r) mb.push([r.window.dayFrom, r.window.dayTo, 'the ' + p.role + "'s window", ' role']); }
  mb.forEach(b => (html += '<i class="band' + b[3] + '" style="left:' + pc(b[0] % 1440) + ';width:' + wd(b[0], b[1] + 4) + '" title="' + esc(b[2]) + '"></i>'));
  let ax = ''; for (let h = 0; h <= 24; h += 3) ax += '<span style="left:' + (h / 24 * 100) + '%">' + pad(h % 24) + '</span>';
  return '<div class="tlw">' + html + '</div><div class="tlax">' + ax + '</div>';
}
function scaleRow(p, s) {
  const steps = D.rules.steps[s] || [], max = steps.length - 1, v = p.start[s];
  return '<div class="scale"><span>' + cap(s) + '</span><span class="pips">' + steps.map((_, i) => '<i class="' + (i <= v ? 'f' : '') + '"></i>').join('') + '</span><span><b>' + esc(p.steps[s]) + '</b> <small>' + v + '/' + max + '</small></span></div>';
}
function approachTable(p) {
  const rows = D.fits[p.fit], role = p.role ? D.roles.find(r => r.id === p.role) : null;
  return rows.map(x => {
    const cls = !x.works ? (x.fit === 'refused' ? 'refused' : 'cannot') : x.fit === 'suits' ? 'suits' : 'worse';
    const fit = x.fit === 'suits' ? 'suits them' : x.fit === 'worse' ? 'one step worse' : 'refused';
    const std = x.standing.map(s => esc(cap(s.band)) + ': ' + esc(s.fit)).join(' · ');
    return '<div class="apc ' + cls + '"><div class="hd"><span><b>' + esc(cap(x.a)) + '</b> <span class="chip ' + (x.fit === 'suits' ? 'suits' : x.fit === 'worse' ? 'worse' : 'refused') + '">' + esc(fit) + '</span>' + (role && role.approach === x.a ? ' <span class="chip">the Handover’s plan</span>' : '') + '</span><span>' + esc(usd(x.total.usd)) + ' · ' + x.total.timeS + ' s · +' + x.total.trail + ' T</span></div><div>' + esc(x.say) + (x.reports ? ' <b>Reports the approach.</b>' : '') + '</div><div class="muted" style="font-size:11.5px">Needs known first: ' + esc(x.requires === 'nothing' ? 'nothing' : x.requires === 'routine' ? 'routine (Watch)' : 'need (Listen)') + '. ' + esc(x.fitWhy) + '.' + (x.lines.length > 1 ? ' ' + x.lines.map(esc).join(' · ') + '.' : '') + (std ? ' District standing: ' + std + '.' : '') + '</div></div>';
  }).join('');
}
function sheetHtml(p) {
  const T = D.rules.temperaments[p.tem], role = p.role ? D.roles.find(r => r.id === p.role) : null, u = D.units.find(x => x.id === p.unit);
  const kindTag = k => '<span class="kind ' + esc(k) + '">' + esc(k) + '</span>';
  const tieRow = t => { const q = P.get(t.with), dir = D.rules.ties[t.kind].direction !== 'both'; return '<div class="tie">' + kindTag(t.kind) + (dir ? '<small>→</small>' : '') + link(t.with) + '<small>' + esc(q ? q.position + ', ' + uName(q.unit) : '') + '</small>' + (D.rules.ties[t.kind].secret ? '<span class="secretchip" title="nobody in the city is told it; the cell learns it with the secret">secret</span>' : '') + (t.at ? '<small>· ' + (t.by === 'them' ? 'they stop by at their ' : 'at their ') + esc(t.at) + '</small>' : '') + '</div>'; };
  const secretHtml = () => { const S = D.rules.secrets && D.rules.secrets[p.secret]; if (!D.rules.secrets) return ''; const lov = p.ties.find(t => D.rules.ties[t.kind].secret); return '<h4>Secret <small>(Pressure’s leverage once known)</small></h4>' + (p.secret ? '<div><span class="secretchip">' + esc(p.secret) + '</span> ' + (p.secret === 'affair' && lov ? '<small>with ' + link(lov.with) + ' (the lover tie); learning it on one lover teaches it on both</small>' : '<small>a past on file</small>') + (S ? '<br><small>Learned by ' + S.learnedBy.map(esc).join(', ') + '; never told along ties, never in a keeper’s tip.</small>' : '') + '</div>' : '<small>none</small>'); };
  const wts = Object.entries(p.needWeights).sort((a, b) => b[1] - a[1]);
  const wmax = Math.max(1, ...wts.map(x => x[1]));
  const bits = [];
  bits.push('<h4>Where and when</h4><dl class="kv"><dt>Unit</dt><dd>' + esc(uName(p.unit)) + ' <small class="mono">' + esc(p.unit) + '</small></dd><dt>Position</dt><dd>' + esc(p.position) + ' <small>(' + esc(p.trade) + (p.post ? ', post: ' + esc(p.post) : '') + ')</small></dd>' + (p.workPlace ? '<dt>Stands at</dt><dd>' + esc(placeName(p.workPlace)) + '</dd>' : '') + '<dt>Shift</dt><dd>' + (p.shift ? hm(p.shift[0]) + '–' + hm(p.shift[1]) + (p.shift[1] < p.shift[0] ? ' <small>(past midnight)</small>' : '') + (u ? ' <small>of ' + u.shifts.map(s => hm(s[0]) + '–' + hm(s[1])).join(', ') + (u.watch.length ? ' · watch ' + u.watch.map(s => hm(s[0]) + '–' + hm(s[1])).join(', ') : '') + '</small>' : '') : 'none (a resident)') + '</dd><dt>Home</dt><dd>' + (p.home.plot === 'beyond' ? 'beyond the sheet (comes in by tram or car)' : esc(placeName(p.home.place))) + '</dd><dt>Day</dt><dd><span class="mono">' + esc(p.variant) + '</span></dd></dl>');
  if (role) bits.push('<div class="rolebox" style="margin-top:8px"><b>The Handover’s ' + esc(role.id) + '</b> <small>· ' + esc(role.employer) + ' ' + esc(role.position) + '</small><br><small>' + esc(role.what) + '</small><br><small>Window ' + esc(role.window.missionFrom) + '–' + esc(role.window.missionTo) + ' mission (' + hm(role.window.dayFrom) + '–' + hm(role.window.dayTo) + ')' + (role.window.place ? ' at ' + esc(role.window.place) : '') + (role.approach ? ' · plan: ' + esc(role.approach) : '') + '</small>' + role.tied.map(t => '<br><small>' + (t.ok ? '✓' : '✗') + ' ' + esc(t.text) + '</small>').join('') + '</div>');
  bits.push('<h4>Temperament, and what the cell knows</h4><div class="two"><div class="know cell"><h5>The cell knows (at the start)</h5>Temperament: <b>?</b><br>Need: <b>?</b><br>Routine: <b>?</b><br><small>Watch (20 s) shows the routine; Listen (42 s) shows need and temperament.' + (p.knownBy.length ? ' A keeper’s tip: ' + p.knownBy.map(k => esc(nm(k))).join(', ') + ' know their need.' : '') + '</small></div><div class="know truth"><h5>The truth</h5>Temperament: <b>' + esc(T.name) + '</b><br>Need: <b>' + esc(p.need) + '</b><br>Routine: <b>' + p.stops.length + ' stops</b><br><small>responds to ' + esc(T.respondsTo.join('/')) + (T.refuses.length ? '; refuses ' + esc(T.refuses.join('/')) : '') + (T.reportsApproach ? '; reports approaches' : '') + (p.inst ? '; loyal to ' + esc(p.inst) : '') + '</small></div></div>');
  bits.push('<p style="margin:8px 0 0"><span class="chip ' + (p.talker ? 'warn' : 'ok') + '">' + (p.talker ? 'A Talker at the start' : 'Not a Talker at the start') + '</span> <small>fear + resentment = ' + p.talkerSums[0] + (p.talker ? ' &gt; ' : ' ≤ ') + 'bravery + loyalty = ' + p.talkerSums[1] + (p.talker ? ': questioned, gives up what they know' : '') + '</small></p><small class="muted">What the rules say of them by district: ' + esc(T.hero) + ' (Hero); ' + esc(T.menace) + ' (Menace).</small>');
  bits.push('<h4>Need <small>(per mille of the cast row’s weights)</small></h4><div class="wt">' + wts.map(([k, v]) => '<span class="' + (k === p.need ? 'me' : '') + '">' + esc(k) + '</span><span class="bar ' + (k === p.need ? 'me' : '') + '"><b style="width:' + (100 * v / wmax).toFixed(0) + '%"></b></span><span class="' + (k === p.need ? 'me' : '') + '">' + v + '</span>').join('') + '</div>');
  bits.push(secretHtml());
  bits.push('<h4>Scales</h4>' + ['bravery', 'resentment', 'fear', 'loyalty'].map(s => scaleRow(p, s)).join(''));
  bits.push('<h4>Ties <small>(' + p.ties.length + ' held' + (p.tiedBy.length ? ', ' + p.tiedBy.length + ' received' : '') + ')</small></h4>' + p.ties.map(tieRow).join('') + (p.tiedBy.length ? '<div style="margin-top:4px"><small>Held on them by:</small></div>' + p.tiedBy.map(t => '<div class="tie">' + kindTag(t.kind) + '<small>←</small>' + link(t.from) + '<small>' + esc(P.get(t.from).position) + '</small></div>').join('') : ''));
  bits.push('<h4>Knowledge</h4><div><small>Knows the need of:</small> ' + (p.knows.length ? '<details style="display:inline"><summary style="display:inline">' + p.knows.length + ' people (a keeper’s tip)</summary>' + p.knows.map(id => '<div>' + link(id) + ' <small>' + esc(P.get(id).need) + '</small></div>').join('') + '</details>' : '<small>nobody (not a keeper)</small>') + '</div><div><small>Their need is known to:</small> ' + (p.knownBy.length ? p.knownBy.map(link).join(', ') : '<small>no keeper</small>') + '</div><div><small>Memory: empty. The cell is known to them by face only.</small></div>');
  bits.push('<h4>The day <small>(24 h; hatched = walking; outlined = the Handover)</small></h4>' + timeline(p) + '<div class="row" style="gap:8px;font-size:11.5px">' + Object.keys(DOING).map(k => '<span><i class="sw sq" style="background:' + DOING[k] + '"></i> ' + k + '</span>').join('') + '<span><i class="sw sq" style="background:#fff;border:2px solid #22313a"></i> at a chair or standing point</span></div><div class="stops">' + p.stops.map(s => '<div><span class="mono">' + hm(s[2]) + '–' + hm(s[3]) + '</span><span>' + esc(s[1]) + '</span><span>' + esc(placeName(s[0])) + '</span></div>').join('') + '</div>');
  bits.push('<h4>The approaches on them <small>(read from the rules)</small></h4>' + approachTable(p));
  if (p.notes.length) bits.push('<h4>Notes raised for them</h4>' + p.notes.map(n => '<div class="note">' + esc(n) + '</div>').join(''));
  if (p.diff && p.diff.status !== 'same') bits.push('<h4>Against ' + esc(D.against.id) + '</h4>' + (p.diff.status === 'added' ? '<div class="note" style="background:var(--teal-l);border-color:#a8d3cf">Added: no one held this id in ' + esc(D.against.id) + '.</div>' : '<table class="diff"><tr><th>Field</th><th>' + esc(D.against.id) + '</th><th>now</th></tr>' + p.diff.fields.map(f => '<tr><td>' + esc(f[0]) + '</td><td class="old">' + esc(f[1]) + '</td><td class="new">' + esc(f[2]) + '</td></tr>').join('') + '</table>'));
  else if (p.diff) bits.push('<h4>Against ' + esc(D.against.id) + '</h4><small>Unchanged.</small>');
  return bits.join('');
}
function openSheet(id, push) {
  const p = P.get(id); if (!p) return;
  if (push !== false && st.sel && st.sel !== id) st.hist.push(st.sel);
  st.sel = id; const sh = $('#sheet'); sh.hidden = false; document.body.classList.add('sheet-open');
  sh.innerHTML = '<header><div class="nm"><h2 style="margin:0">' + esc(p.name) + '</h2><div><small class="mono">' + esc(p.id) + '</small> · ' + esc(p.position) + ' · ' + esc(uName(p.unit)) + (p.role ? ' <span class="badge-role">Handover ' + esc(p.role) + '</span>' : '') + '</div><div class="btns" style="margin-top:6px">' + (st.hist.length ? '<button class="btn" id="sback">‹ Back</button>' : '') + '<button class="btn" id="smap">On the map</button><button class="btn" id="sfrom">Path from here</button><button class="btn" id="sto">Path to here</button></div></div><button class="x" id="sx" aria-label="close (Esc)" title="Esc">×</button></header><div class="body">' + sheetHtml(p) + '</div>';
  $('#sx').onclick = closeSheet;
  const b = $('#sback'); if (b) b.onclick = () => { const prev = st.hist.pop(); if (prev) openSheet(prev, false); };
  $('#smap').onclick = () => { go('map'); drawMap(); };
  $('#sfrom').onclick = () => { $('#pa').value = id; go('graph'); drawPath(); };
  $('#sto').onclick = () => { $('#pb').value = id; go('graph'); drawPath(); };
  $$('#roster tbody tr').forEach(tr => tr.classList.toggle('cur', tr.dataset.p === id));
  $$('#graph .n').forEach(n => n.classList.toggle('sel', n.dataset.p === id));
  $('#sheet .body').scrollTop = 0;
  if (st.tab === 'map') drawMap();
  writeHash();
}
function closeSheet() { $('#sheet').hidden = true; document.body.classList.remove('sheet-open'); st.sel = null; st.hist = []; $$('#roster tbody tr.cur').forEach(t => t.classList.remove('cur')); $$('#graph .n.sel').forEach(n => n.classList.remove('sel')); if (st.tab === 'map') drawMap(); writeHash(); }

// ------------------------------------------------------------------ keys
document.addEventListener('keydown', e => {
  const t = e.target, typing = t && (t.tagName === 'INPUT' && t.type !== 'range' && t.type !== 'checkbox' && t.type !== 'radio' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT');
  if (e.key === 'Escape') {
    if (typing && t.value) { t.value = ''; t.dispatchEvent(new Event('input')); t.dispatchEvent(new Event('change')); return; }
    if (typing) { t.blur(); return; }
    if (!$('#sheet').hidden) { closeSheet(); return; }
    if (!$('#pathout').hidden) { $('#pclear').click(); return; }
    return;
  }
  if (typing || e.ctrlKey || e.metaKey || e.altKey) return;
  if (e.key === '/') { e.preventDefault(); $('#q').focus(); $('#q').select(); return; }
  if (/^[1-9]$/.test(e.key) && TABS[Number(e.key) - 1]) { go(TABS[Number(e.key) - 1]); return; }
  if (st.tab === 'map') {
    if (e.key === ' ' && (t === document.body || t.id === 'scrub')) { e.preventDefault(); setPlaying(!st.playing); return; }
    if (e.key === 'ArrowLeft' || e.key === 'ArrowRight') { e.preventDefault(); setTime(st.T + (e.key === 'ArrowRight' ? 1 : -1) * (e.shiftKey ? 600 : 60)); return; }
  }
  if (e.key === 'ArrowLeft' && e.altKey) return;
});

// ------------------------------------------------------------------ start
applyGraphFilter();
const h = readHash();
if (h.q) { $('#q').value = h.q; }
if (h.any) $('#pany').checked = true;
if (h.a) $('#pa').value = h.a; if (h.b) $('#pb').value = h.b;
if (h.t && !isNaN(Number(h.t))) st.T = Number(h.t);
filterRows();
drawMap.force = true; drawMap(); drawMap.force = false;
if (h.p && P.get(h.p)) openSheet(h.p);
go(h.tab || (h.q ? 'roster' : 'overview'));
if (h.a || h.b) drawPath();
document.documentElement.dataset.ready = '1';
})();
