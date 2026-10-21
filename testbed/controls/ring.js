// The ring of other ways. Calm: icons only, the main way on top with a pointer tab, one plate for all.
// Busy (0.2.34): a plate beside every wedge.
import { G, say } from './state.js';
import { KINDS, priceLine } from './data.js';
import { icon } from './icons.js';
import { planFor, crew } from './orders.js';
import { facesHtml } from './markers.js';

const R1 = 34, R2 = 78, SZ = 330, C = SZ / 2;
const pt = (r, a) => [C + r * Math.sin(a * Math.PI / 180), C - r * Math.cos(a * Math.PI / 180)];
const arc = (r1, r2, a0, a1) => { const [x0, y0] = pt(r2, a0), [x1, y1] = pt(r2, a1), [x2, y2] = pt(r1, a1), [x3, y3] = pt(r1, a0); return `M${x0} ${y0}A${r2} ${r2} 0 0 1 ${x1} ${y1}L${x2} ${y2}A${r1} ${r1} 0 0 0 ${x3} ${y3}Z`; };

// Split the ways: at most three on the ring (one harmful at most); the rest sit behind the dots.
function split(ways, style) {
  if (style === 'busy') return { shown: ways.slice(0, 5), rest: ways.slice(5) };
  const shown = [], rest = [];
  for (const w of ways) (shown.length < 3 && !(w.kind === 'harm' && shown.some((s) => s.kind === 'harm')) ? shown : rest).push(w);
  return { shown, rest };
}

let el, onTake;
const runNow = () => G.keys.has('ShiftLeft') || G.keys.has('ShiftRight');
export const initRing = (node, take) => { el = node; onTake = take; };

export function openRing(ent, ways, style) {
  const { shown, rest } = split(ways, style);
  G.ring = { ent, ways, shown, rest, style, hover: -1, dots: false, lit: 0 };
  draw(); say(`Ring · ${ent.name}`);
}
export function closeRing() { G.ring = null; if (!el) return; el.style.display = 'none'; el.innerHTML = ''; }

function wedgeHtml(w, i, n, style, lit, hover) {
  const step = style === 'busy' ? 72 : 90, a0 = i * step - step / 2 + 2, a1 = i * step + step / 2 - 2, isDef = i === 0 && style === 'calm';
  const k = KINDS[w.kind], mid = i * step, [ix, iy] = pt((R1 + R2) / 2 + (isDef ? 6 : 0), mid);
  const fill = isDef ? k.color : hover || (lit && style === 'busy') ? '#34504f' : 'rgba(20,34,36,.92)';
  const col = isDef ? '#10201f' : k.color;
  const d = arc(R1, R2 + (isDef ? 12 : 0), a0, a1);
  const [tx, ty] = pt(R2 + 12, mid), tab = isDef ? `<path d="M${tx - 14} ${ty + 4}L${tx} ${ty - 18}L${tx + 14} ${ty + 4}Z" fill="${k.color}" stroke="#10201f" stroke-width="3" stroke-linejoin="round"/>` : '';
  return `<g data-w="${i}" class="wedge${hover ? ' on' : ''}"><path d="${d}" fill="${fill}" stroke="${hover ? '#fff' : k.color}" stroke-width="${hover ? 3 : 2}" ${w.kind === 'harm' ? 'stroke-dasharray="7 5"' : ''}/>${tab}
    <foreignObject x="${ix - 16}" y="${iy - 16}" width="32" height="32" style="color:${col};pointer-events:none"><div class="wi">${icon(w.icon, isDef ? 30 : 24)}</div></foreignObject></g>`;
}

export function draw() {
  const r = G.ring; if (!r) return;
  const { shown, rest, style } = r;
  let g = shown.map((w, i) => wedgeHtml(w, i, shown.length, style, i === r.lit, r.hover === i)).join('');
  if (rest.length) { // the dots wedge
    const i = shown.length, a = i * 90, [ix, iy] = pt(56, a);
    g += `<g data-w="dots" class="wedge"><path d="${arc(R1, R2, a - 43, a + 43)}" fill="${r.dots ? '#34504f' : 'rgba(20,34,36,.92)'}" stroke="#f1ead8" stroke-width="2"/><foreignObject x="${ix - 16}" y="${iy - 16}" width="32" height="32" style="color:#f1ead8;pointer-events:none"><div class="wi">${icon('dots', 24)}</div></foreignObject></g>`;
    if (r.dots) rest.forEach((w, j) => { const aa = a + (j - (rest.length - 1) / 2) * 34, k = KINDS[w.kind], [x, y] = pt(104, aa);
      g += `<g data-w="r${j}" class="wedge"><path d="${arc(86, 120, aa - 16, aa + 16)}" fill="${r.hover === 'r' + j ? '#34504f' : 'rgba(20,34,36,.92)'}" stroke="${r.hover === 'r' + j ? '#fff' : k.color}" stroke-width="2" ${w.kind === 'harm' ? 'stroke-dasharray="7 5"' : ''}/><foreignObject x="${x - 13}" y="${y - 13}" width="26" height="26" style="color:${k.color};pointer-events:none"><div class="wi">${icon(w.icon, 22)}</div></foreignObject></g>`; });
  }
  const plates = style === 'busy' ? shown.map((w, i) => { const [x, y] = pt(104, i * 72), pl = planFor(r.ent, w, crew(), runNow()), p = priceLine(w, r.ent, pl.walkS, true, '', pl.running);
    return `<div class="bp${i === r.lit ? ' lit' : ''}" style="left:${x}px;top:${y}px;${Math.sin(i * 72 * Math.PI / 180) < -0.2 ? 'transform:translate(-100%,-50%)' : 'transform:translate(0,-50%)'}">${icon(w.icon, 16)}${p.join(' · ')}<b>${icon('finger', 12)}${w.trail || 0}</b><b>${icon('siren', 12)}${w.alert ? 2 : 0}</b>${facesHtml(pl.agents)}</div>`; }).join('') : '';
  el.innerHTML = `<svg width="${SZ}" height="${SZ}" viewBox="0 0 ${SZ} ${SZ}">${g}<circle cx="${C}" cy="${C}" r="4" fill="#fff"/></svg>${plates}<div class="plate1"></div>`;
  el.style.display = 'block'; plate();
}

// One plate, always in the same place; the text follows the way under the pointer (or the main way).
function plate() {
  const r = G.ring, p = el.querySelector('.plate1'); if (!p || r.style === 'busy') { if (p) p.remove(); return; }
  const h = r.hover, w = h === 'dots' ? null : typeof h === 'string' && h[0] === 'r' ? r.rest[+h.slice(1)] : r.shown[h >= 0 ? h : 0];
  if (!w) { p.innerHTML = 'More ways'; p.style.setProperty('--c', '#f1ead8'); return; }
  p.style.setProperty('--c', KINDS[w.kind].color); p.classList.toggle('harm', w.kind === 'harm');
  p.innerHTML = icon(w.icon, 18) + (() => { const pl = planFor(r.ent, w, crew(), runNow()); return priceLine(w, r.ent, pl.walkS, true, '', pl.running).join('<i>·</i>') + facesHtml(pl.agents); })();
}

export function ringPointer(e, down) {
  const r = G.ring; if (!r) return false;
  const t = e.target.closest?.('[data-w]'), v = t ? t.dataset.w : null;
  if (!down) { const nv = v === null ? -1 : v === 'dots' ? 'dots' : v[0] === 'r' ? v : +v; if (nv !== r.hover) { r.hover = nv; draw(); } return true; }
  if (v === 'dots') { r.dots = !r.dots; draw(); return true; }
  if (v !== null) { onTake(v[0] === 'r' ? r.rest[+v.slice(1)] : r.shown[+v], { shift: e.shiftKey, alt: e.altKey }); return true; }
  if (e.button === 2 && r.style === 'busy') { onTake(r.shown[r.lit]); return true; }
  return false;
}
export function placeRing(project) {
  const r = G.ring; if (!r) return;
  const p = project(r.ent.g.position.x, r.ent.h * 0.55, r.ent.g.position.z); if (!p) return;
  const x = Math.max(160, Math.min(innerWidth - 160, p.x)), y = Math.max(160, Math.min(innerHeight - 160, p.y));
  el.style.transform = `translate(${Math.round(x - C)}px,${Math.round(y - C)}px)`;
  const pl = el.querySelector('.plate1'); if (pl) pl.classList.toggle('up', y > innerHeight - 260);
}

// While a harmful wedge is held, its plate fills (the ring stays open).
export function ringCharge(c) {
  const p = el?.querySelector('.plate1'); if (!p) return;
  const f = Math.min(1, c.t / G.chargeS) * 100;
  p.textContent = `Hold to ${c.way.id === 'fire' ? 'fire' : c.way.verb.toLowerCase()}`; p.style.background = `linear-gradient(90deg, #fff ${f}%, ${KINDS[c.way.kind].color} ${f}%)`;
}
