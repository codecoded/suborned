// The self-contained HTML plan sheet: the district drawing with layer toggles, zoom and a title block, the Records
// Precinct inset, the systems schedule, the checks and the design basis.
import type { Plan } from './plan';
import type { Report } from './checks';

const esc = (s: string) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;');
const m = (x: number) => `${Math.round(x)} m`;
const table = (head: string[], rows: (string | number)[][], cls = '') =>
  `<table class="${cls}"><thead><tr>${head.map(h => `<th>${h}</th>`).join('')}</tr></thead><tbody>${rows.map(r => `<tr>${r.map(c => `<td>${c}</td>`).join('')}</tr>`).join('')}</tbody></table>`;

const LAYERS = [['base', 'Water + quay'], ['plots', 'Plots'], ['areas', 'Environmental areas'], ['roads', 'Streets'], ['centrelines', 'Centrelines'], ['tram', 'Tram T1'],
  ['sky', 'Sky rail S1'], ['walks', 'Walks'], ['gates', 'Gates'], ['landmarks', 'Landmarks'], ['signatures', 'Signatures'], ['civic', 'Civic block'], ['labels', 'Labels'], ['grid', 'Grid'], ['proposal', 'Crossing proposal (009)', 'off']];
const LEGEND: [string, string][] = [
  ['sw-primary', 'Primary distributor'], ['sw-district', 'District distributor'], ['sw-access', 'Access road'], ['sw-mall', 'Mall / walk'],
  ['sw-tram', 'Tram track · ◆ stop'], ['sw-sky', 'Sky rail beams · pylon'], ['sw-station', 'Sky rail station ▲'], ['sw-gate', 'Gate (seal point)'],
  ['sw-walk', 'Walk on foot (crosses anywhere)'], ['sw-sig', 'Signature: ground ┄, footprint, front →'], ['sw-pr-kept', 'Proposal layer: crossing kept'], ['sw-pr-add', 'proposed (X1–X4)'], ['sw-pr-gone', 'dropped (R1–R2)'], ['sw-civic', 'Civic block'], ['sw-park', 'Park / green'], ['sw-res', 'Residential'], ['sw-mixed', 'Mixed use'], ['sw-com', 'Commercial'],
  ['sw-port', 'Port / industrial'], ['sw-water', 'Water'], ['sw-beyond', 'Beyond the plan'],
];

/** The sheet's styles; also embedded in the stand-alone SVGs rendered to PNG (briefs/009/crossings-proposal/make.ts). */
export const CSS = `
:root{color-scheme:light;--paper:#f4ecda;--sheet:#f8f2e4;--ink:#2b2723;--ink2:#6e655a;--line:#cfc3a8;--tram:#dd5b42;--sky:#13838a;--gate:#d0391c;--ok:#2f7d4f;--bad:#c0392b}
*{box-sizing:border-box}html,body{margin:0;background:#e9e1cd;color:var(--ink);font:14px/1.5 ui-sans-serif,-apple-system,"Helvetica Neue",Arial,sans-serif}
.wrap{max-width:1500px;margin:0 auto;padding:20px 16px 60px}
header{display:flex;flex-wrap:wrap;gap:12px 24px;align-items:baseline;border-bottom:2px solid var(--ink);padding-bottom:10px;margin-bottom:16px}
header h1{font:600 22px/1.2 ui-monospace,"SF Mono",Menlo,monospace;letter-spacing:.06em;margin:0}header .meta{font:12px ui-monospace,Menlo,monospace;color:var(--ink2)}
.chip{font:600 12px ui-monospace,Menlo,monospace;padding:3px 9px;border:1.5px solid currentColor;border-radius:3px}.chip.ok{color:var(--ok)}.chip.bad{color:var(--bad)}
h2{font:600 13px ui-monospace,Menlo,monospace;letter-spacing:.14em;text-transform:uppercase;margin:28px 0 10px;border-bottom:1px solid var(--line);padding-bottom:4px}
.card{background:var(--sheet);border:1.5px solid var(--ink);box-shadow:0 1px 0 #0001}
.toggles{display:flex;flex-wrap:wrap;gap:4px 14px;padding:8px 12px;border-bottom:1px solid var(--line);font:12px ui-monospace,Menlo,monospace}
.toggles label{cursor:pointer;user-select:none}.toggles .hint{margin-left:auto;color:var(--ink2)}
.view{position:relative;overflow:hidden;cursor:grab;touch-action:none}.view:active{cursor:grabbing}.view svg{display:block;width:100%;height:auto;background:var(--paper)}
.tb{display:grid;grid-template-columns:2.2fr 2fr 1fr 1fr 1fr .7fr 1.2fr;border-top:1.5px solid var(--ink);font:11px ui-monospace,Menlo,monospace}
.tb div{padding:6px 10px;border-right:1px solid var(--ink)}.tb div:last-child{border-right:0}.tb b{display:block;font-size:9px;letter-spacing:.12em;color:var(--ink2);font-weight:600}
@media(max-width:800px){.tb{grid-template-columns:1fr 1fr}.tb div{border-bottom:1px solid var(--ink)}}
.legend{display:flex;flex-wrap:wrap;gap:6px 16px;padding:10px 12px;font:12px ui-monospace,Menlo,monospace;border-top:1px solid var(--line)}
.legend span{display:inline-flex;align-items:center;gap:6px}.legend i{width:22px;height:12px;display:inline-block;border:1px solid var(--ink)}
.cols{display:grid;grid-template-columns:repeat(auto-fit,minmax(340px,1fr));gap:16px}
.panel{background:var(--sheet);border:1px solid var(--line);padding:12px 14px}.panel h3{margin:0 0 8px;font:600 12px ui-monospace,Menlo,monospace;letter-spacing:.1em;text-transform:uppercase}
.panel ul,.panel ol{margin:0;padding-left:18px}.panel li{margin:4px 0}
table{border-collapse:collapse;width:100%;font:12px ui-monospace,Menlo,monospace}th,td{text-align:left;padding:4px 8px;border-bottom:1px solid var(--line);vertical-align:top}
th{font-size:10px;letter-spacing:.1em;text-transform:uppercase;color:var(--ink2)}td.ok{color:var(--ok);font-weight:700}td.bad{color:var(--bad);font-weight:700}
.checks td:nth-child(3){font-family:ui-sans-serif,-apple-system,Arial,sans-serif}.detail{color:var(--ink2);font-size:11px}
.inset{display:grid;grid-template-columns:minmax(0,1.6fr) minmax(240px,1fr);gap:0}.inset ol{columns:2;font:11.5px ui-monospace,Menlo,monospace;margin:0;padding:12px 12px 12px 30px}
@media(max-width:900px){.inset{grid-template-columns:1fr}}
footer{margin-top:30px;font:12px ui-monospace,Menlo,monospace;color:var(--ink2)}code{font:12px ui-monospace,Menlo,monospace;background:#0000000a;padding:1px 4px}
/* drawing */
.water{fill:#a9cfd0;stroke:#5f8f91;stroke-width:1;vector-effect:non-scaling-stroke}.quay{fill:#d8cdb6;stroke:var(--ink);stroke-width:1;vector-effect:non-scaling-stroke}
.beyond{fill:url(#hatch);stroke:none}
.plot{stroke:var(--ink);stroke-width:.8;vector-effect:non-scaling-stroke}
.plot.square{fill:#f1ddc1}.plot.civic{fill:#e9a68b}.plot.mixed{fill:#e7dbc0}.plot.residential{fill:#e3dfc6}.plot.commercial{fill:#ead1ab}
.plot.industrial{fill:#d8d3c6}.plot.port{fill:#c9d6d3}.plot.park{fill:#c2d39c}.plot.open{fill:#e6dfc4}
.civic-hi{fill:none;stroke:var(--gate);stroke-width:2.2;vector-effect:non-scaling-stroke}
.area{fill:none;stroke:#7d5a2c;stroke-width:1.2;stroke-dasharray:10 4 2 4;vector-effect:non-scaling-stroke}
.area-lab{fill:#7d5a2c;font-weight:700;font-family:ui-monospace,Menlo,monospace;letter-spacing:.25em;text-anchor:middle;paint-order:stroke;stroke:var(--paper);stroke-width:3px;opacity:.85}
.kerb,.foot-k{fill:none;stroke:#4a423a;stroke-linecap:butt;stroke-linejoin:round}.kerb-c{fill:#4a423a}
.road{fill:none;stroke-linecap:butt;stroke-linejoin:round}.road.primary{stroke:#d0bc92}.road.district{stroke:#dfcfaa}.road.access{stroke:#efe6cd}
.foot{fill:none;stroke-linecap:butt}.foot.mall{stroke:#f5eedd}.foot.walk{stroke:#f2ead6}
.reservation{fill:none;stroke:#c9d6ad;stroke-linecap:butt}.median{fill:#c9d6ad;stroke:none}
.island{fill:#c2d39c;stroke:var(--ink);stroke-width:.8;vector-effect:non-scaling-stroke}.garden{fill:#b3c98a;stroke:none}
.cl{fill:none;stroke:#7a6f60;stroke-width:.7;stroke-dasharray:14 4 2 4;vector-effect:non-scaling-stroke}
.tram{fill:none;stroke:var(--tram);stroke-width:1.3;stroke-linecap:round}.platform{fill:#f3b7a8;stroke:var(--tram);stroke-width:.8;vector-effect:non-scaling-stroke}
.sky{fill:none;stroke:var(--sky);stroke-width:1.5;stroke-linecap:round}.pylon{fill:#fff;stroke:var(--sky);stroke-width:1;vector-effect:non-scaling-stroke}
.station{fill:#d6efeecc;stroke:var(--sky);stroke-width:1.4;vector-effect:non-scaling-stroke}
.gate{fill:#f6c9bc;stroke:var(--gate);stroke-width:1.2;vector-effect:non-scaling-stroke}.gate.seal{fill:var(--gate)}
.sig{fill:#efe2c4cc;stroke:#7a5c2e;stroke-width:1.4;vector-effect:non-scaling-stroke}.sig-ground{fill:none;stroke:#7a5c2e;stroke-width:1;stroke-dasharray:6 3;vector-effect:non-scaling-stroke}
.sig-leg{fill:#7a5c2e40;stroke:#7a5c2e;stroke-width:.8;vector-effect:non-scaling-stroke}.sig-front{fill:none;stroke:#7a5c2e;stroke-width:1.6;vector-effect:non-scaling-stroke}.sig-head{fill:#7a5c2e}.sig-lab{fill:#7a5c2e;font-weight:700}
.pr-walk.hot{stroke-opacity:.32;stroke-width:1.6}.pr-walk{fill:none;stroke:#4a3d78;stroke-opacity:.12;stroke-width:1.3;stroke-linejoin:round;stroke-linecap:round;vector-effect:non-scaling-stroke}
.pr-kept{fill:none;stroke:#1f7a6a;stroke-width:4;vector-effect:non-scaling-stroke}.pr-add{fill:none;stroke:#c0157a;stroke-width:6;stroke-linecap:round;vector-effect:non-scaling-stroke}
.pr-gone{fill:none;stroke:#a39a90;stroke-width:4;vector-effect:non-scaling-stroke}.pr-strike{fill:none;stroke:#c0392b;stroke-width:2.5;stroke-linecap:round;vector-effect:non-scaling-stroke}
.pr-call{fill:none;stroke:#c0157a;stroke-width:2;stroke-dasharray:8 4;vector-effect:non-scaling-stroke}
.mark{fill:none;stroke:var(--ink);stroke-width:1;stroke-dasharray:3 3;vector-effect:non-scaling-stroke}.mark.monument{fill:var(--ink);stroke-dasharray:none}.mark.skyport{fill:#f5f0e4;stroke-dasharray:none}
.lab{fill:var(--ink);font-family:ui-monospace,"SF Mono",Menlo,monospace;text-anchor:middle;paint-order:stroke;stroke:var(--paper);stroke-width:2.5px;stroke-linejoin:round}
.road-lab{dominant-baseline:central;font-weight:600;letter-spacing:.06em}.road-lab.access{font-weight:500}
.pr-lab{fill:#c0157a;font-weight:700}.pr-lab.gone{fill:#c0392b}.pr-call-lab{fill:#c0157a;font-weight:700;text-anchor:start}
.tram-lab{fill:var(--tram);font-weight:700;text-anchor:end}.sky-lab{fill:var(--sky);font-weight:700}.gate-lab{fill:var(--gate);font-weight:700;text-anchor:start}
.mark-lab,.plot-lab{fill:#4d463d;font-style:italic}.circus-lab{font-weight:700;letter-spacing:.2em}.link-lab{fill:#7d5a2c;font-weight:700;text-anchor:start}.foot-lab{letter-spacing:.2em}
.grid{fill:none;stroke:#2b272314;stroke-width:1;vector-effect:non-scaling-stroke}.gref{fill:var(--ink2);font:600 11px ui-monospace,Menlo,monospace;text-anchor:middle}
.border{fill:none;stroke:var(--ink);stroke-width:2;vector-effect:non-scaling-stroke}
.n-arrow{fill:var(--ink)}.n-lab{fill:var(--ink);font-weight:700;font-family:ui-monospace,Menlo,monospace;text-anchor:middle}.sb-b{fill:var(--ink)}.sb-w{fill:#fff;stroke:var(--ink);stroke-width:1;vector-effect:non-scaling-stroke}
.sb-lab{fill:var(--ink);font-family:ui-monospace,Menlo,monospace;text-anchor:middle}
.pavement{fill:#efe7d3;stroke:var(--ink);stroke-width:.8;vector-effect:non-scaling-stroke}.core{fill:#f3dcc0;stroke:#8b7d6b;stroke-width:.6;stroke-dasharray:3 2;vector-effect:non-scaling-stroke}.core.office{fill:#ecd2c3}
.bldg{fill:#3a332c;stroke:var(--ink)}.bldg.cafe{fill:#c4553e}.bldg-lab{fill:#fff;font:700 3px ui-monospace,Menlo,monospace;text-anchor:middle}
.struct{fill:#8a9aa0;stroke:var(--ink);stroke-width:.6;vector-effect:non-scaling-stroke}.fountain{fill:#a9cfd0;stroke:var(--ink);stroke-width:.8;vector-effect:non-scaling-stroke}
.obj{fill:#b3c98a;stroke:var(--ink);stroke-width:.4;vector-effect:non-scaling-stroke}.obj.table{fill:#fff}
.bay{fill:none;stroke:var(--gate);stroke-width:1.2;stroke-dasharray:4 2;vector-effect:non-scaling-stroke}.xlight{fill:none;stroke:#2f9e8f;stroke-width:1.1;stroke-dasharray:1 3;stroke-linecap:round;vector-effect:non-scaling-stroke}
.walk-route{fill:none;stroke:#1d4fb8;stroke-width:1.6;stroke-dasharray:4 3;stroke-linecap:round;vector-effect:non-scaling-stroke}.walk-lab{fill:#1d4fb8;font-weight:700}
.pt{stroke:#1d4fb8;stroke-width:1.4;vector-effect:non-scaling-stroke}.pt-lab{fill:#1d4fb8;font:700 2px ui-monospace,Menlo,monospace;text-anchor:start;paint-order:stroke;stroke:#fff;stroke-width:.5px}
.dim{fill:none;stroke:#4b6a8a;stroke-width:.8;vector-effect:non-scaling-stroke}.dimt{fill:#4b6a8a;font-family:ui-monospace,Menlo,monospace;text-anchor:middle}
#inset .lab{stroke-width:.8px}#inset .area-lab{stroke-width:1px}#inset .gref{display:none}#inset .grid{stroke:#2b27230c}
.sw-primary{background:#d0bc92}.sw-district{background:#dfcfaa}.sw-access{background:#efe6cd}.sw-mall{background:#f5eedd}
.sw-tram{background:linear-gradient(#0000 40%,var(--tram) 40% 60%,#0000 60%);border:0!important}.sw-sky{background:linear-gradient(#0000 40%,var(--sky) 40% 60%,#0000 60%);border:0!important}
.sw-sig{background:#efe2c4;border:1.5px dashed #7a5c2e!important}.sw-pr-kept{background:#1f7a6a}.sw-pr-add{background:#c0157a}.sw-pr-gone{background:linear-gradient(45deg,#a39a90 42%,#c0392b 42% 58%,#a39a90 58%)}.sw-walk{background:linear-gradient(90deg,#1d4fb8 50%,#0000 50%) 0 50%/6px 2px repeat-x;border:0!important}.sw-station{background:#d6efee;border-color:var(--sky)!important}.sw-gate{background:var(--gate);transform:rotate(45deg) scale(.6)}.sw-civic{background:#e9a68b}.sw-park{background:#c2d39c}
.sw-res{background:#e3dfc6}.sw-mixed{background:#e7dbc0}.sw-com{background:#ead1ab}.sw-port{background:#c9d6d3}.sw-water{background:#a9cfd0}
.sw-beyond{background:repeating-linear-gradient(45deg,#0000 0 3px,#2b272355 3px 4px)}
`;

const JS = `
for(const cb of document.querySelectorAll('[data-toggle]')){const show=()=>{
  for(const g of document.querySelectorAll('#district [data-layer="'+cb.dataset.toggle+'"]'))g.style.display=cb.checked?'':'none'};cb.addEventListener('change',show);show()}
for(const view of document.querySelectorAll('.view')){
  const svg=view.querySelector('svg'),[x0,y0,w0,h0]=svg.getAttribute('viewBox').split(' ').map(Number);let vb=[x0,y0,w0,h0],drag=null;
  const set=()=>svg.setAttribute('viewBox',vb.map(v=>v.toFixed(2)).join(' '));
  const at=e=>{const r=svg.getBoundingClientRect();return[vb[0]+(e.clientX-r.left)/r.width*vb[2],vb[1]+(e.clientY-r.top)/r.height*vb[3]]};
  view.addEventListener('wheel',e=>{e.preventDefault();const k=Math.exp(e.deltaY*.0015),p=at(e),w=Math.min(w0,Math.max(w0/40,vb[2]*k)),s=w/vb[2];
    vb=[p[0]-(p[0]-vb[0])*s,p[1]-(p[1]-vb[1])*s,w,vb[3]*s];set()},{passive:false});
  view.addEventListener('pointerdown',e=>{drag=[e.clientX,e.clientY,vb[0],vb[1]];view.setPointerCapture(e.pointerId)});
  view.addEventListener('pointermove',e=>{if(!drag)return;const r=svg.getBoundingClientRect();vb[0]=drag[2]-(e.clientX-drag[0])/r.width*vb[2];vb[1]=drag[3]-(e.clientY-drag[1])/r.height*vb[3];set()});
  view.addEventListener('pointerup',()=>drag=null);view.addEventListener('dblclick',()=>{vb=[x0,y0,w0,h0];set()});
}`;

export function sheet(plan: Plan, R: Report, svgs: { district: string; inset: string; legend: string }, source: string) {
  const pass = R.checks.filter(c => c.ok).length, all = R.checks.length, s = plan.sheet;
  const chip = `<span class="chip ${pass === all ? 'ok' : 'bad'}">${pass}/${all} checks pass</span>`;
  const tb = (drawing: string, title: string, scale: string) => `<div class="tb"><div><b>PROJECT</b>THE SUBORNED · ${esc(plan.name.toUpperCase())}</div><div><b>DRAWING</b>${esc(title)}</div>
    <div><b>NO.</b>${drawing}</div><div><b>SCALE</b>${scale}</div><div><b>DATE</b>${plan.date}</div><div><b>REV</b>v${plan.version}</div><div><b>CHECKED</b>${pass}/${all} rules · ${esc(plan.status)}</div></div>`;
  const defs = `<svg width="0" height="0" style="position:absolute" aria-hidden="true"><defs>
    <pattern id="hatch" width="8" height="8" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><rect width="8" height="8" fill="#ebe3cf"/><line x1="0" y1="0" x2="0" y2="8" stroke="#2b272340" stroke-width="1.2"/></pattern></defs></svg>`;
  const stops = table(['Stop', 'Chainage', 'To next'], R.stops.map(x => [`◆ ${esc(x.name)}`, m(x.chainage), x.next ? m(x.next) : '—']));
  const stations = table(['Station', 'Chainage', 'To next'], R.stations.map(x => [`▲ ${esc(x.name)}`, m(x.chainage), x.next ? m(x.next) : '—']));
  const byArea = plan.areas.map(a => [esc(a.name), plan.gates.filter(g => g.area === a.id).map(g => esc(g.name)).join(', ')]);
  const routes = R.routes.map(r => `<li><b>${esc(r.name)}</b>: ${r.roads.map(esc).join(' → ')} → ${esc(r.to)}. ${m(r.length)}${r.gates.length ? `, through ${r.gates.map(esc).join(', ')}` : ''}.</li>`).join('');
  const walks = R.walks.map(w => `<li>${esc(w.from)} → ${esc(w.to)}: ${m(w.m)} on foot (${m(w.straight)} as the crow flies)</li>`).join('');
  const crossings = [...new Map(R.crossings.map(c => [`${c.road}|${Math.round(c.p[0])}|${Math.round(c.p[1])}`, c])).values()];
  const crossTxt = [...new Set(crossings.map(c => c.road))].map(r => `${esc(r)} ×${crossings.filter(c => c.road === r).length}`).join(', ');
  const checks = table(['Group', 'Rule', '', 'Value', 'Limit', ''], R.checks.map(c => [c.group, c.id, esc(c.rule) + (c.detail ? `<div class="detail">${esc(c.detail)}</div>` : ''), esc(c.value), esc(c.limit), `<span class="${c.ok ? 'ok' : 'bad'}">${c.ok ? 'PASS' : 'FAIL'}</span>`]), 'checks')
    .replace(/<td><span class="ok">/g, '<td class="ok"><span>').replace(/<td><span class="bad">/g, '<td class="bad"><span>');
  const std = plan.standards;
  const basis = table(['System', 'Parameter', 'Value'], [
    ['Tram', 'Minimum radius, running / terminal loop', `${std.tram.radiusRunning} m / ${std.tram.radiusLoop} m`],
    ['Tram', 'Stop spacing · platform', `${std.tram.stopSpacing.join('–')} m · ${std.tram.platform} m on straight`],
    ['Tram', 'Track centres', `${std.tram.trackCentres} m`],
    ['Sky rail', 'Minimum radius, line / terminal loop', `${std.skyrail.radiusLine} m / ${std.skyrail.radiusLoop} m`],
    ['Sky rail', 'Pylon span: straight / R ≥ 180 m / R < 180 m', `${std.skyrail.spanStraight} / ${std.skyrail.spanCurve} / ${std.skyrail.spanTight} m (R 91–183 m taken at the tighter span)`],
    ['Sky rail', 'Beam height · beam centres', `${std.skyrail.height} m · ${std.skyrail.beamCentres} m`],
    ['Sky rail', 'Pylons only in', std.skyrail.pylonZones.join(', ')],
    ['Sky rail', 'Interchange walk to tram', `≤ ${std.skyrail.interchange} m`],
    ['Roads', 'Junction spacing, primary / district', `≥ ${std.roads.junctionSpacing.primary} m / ≥ ${std.roads.junctionSpacing.district} m (plan rule at game scale)`],
    ['Walking', 'Crossing roads', 'anywhere: kerbs, medians, tram reservation and circus are walkable; buildings, built plots, landmarks and water block'],
    ['Walking', 'Escape walk directness', `walk ÷ straight line ≤ ${std.walking.directness}`],
    ...(std.walking.body ? [['Walking', 'Body · clear width (W-5)', `a walker keeps ${std.walking.body.r} m from a ground solid (${+(2 * std.walking.body.r).toFixed(2)} m wide) · walks keep ${std.walking.body.clearWidth} m clear (${std.walking.body.constrained.w} m for ≤ ${std.walking.body.constrained.run} m; seating aisles ${std.walking.body.aisle} m)`]] : []),
    ['Roads', 'Widths kerb to kerb', 'primary 24 (median 4) · Boulevard 28 (tram 10) · district 18 (median 6) · access 12–13 · Parade 40'],
  ]);
  const toggles = LAYERS.map(([id, label, off]) => `<label><input type="checkbox" data-toggle="${id}"${off ? '' : ' checked'}> ${label}</label>`).join('') + '<span class="hint">scroll to zoom · drag to pan · double-click to reset</span>';
  const legend = LEGEND.map(([c, t]) => `<span><i class="${c}"></i>${t}</span>`).join('');

  return `<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Harbour District Plan</title><style>${CSS}</style></head><body>${defs}<div class="wrap">
<header><h1>${esc(plan.name.toUpperCase())} · MASTER PLAN</h1><span class="meta">${s.drawing} · v${plan.version} · ${plan.date} · ${esc(plan.status)}</span>${chip}</header>

<h2>Sheet 1 · District plan</h2>
<div class="card"><div class="toggles">${toggles}</div><div class="view">${svgs.district}</div>${tb(s.drawing, 'District master plan: roads, tram, sky rail', s.scale)}<div class="legend">${legend}</div></div>

<h2>Concept</h2>
<div class="cols"><div class="panel"><h3>The plan in seven moves</h3><ol>${plan.notes.concept.map(c => `<li>${esc(c)}</li>`).join('')}</ol></div>
<div class="panel"><h3>Why re-plan: the current world</h3><ul>${plan.notes.why.map(c => `<li>${esc(c)}</li>`).join('')}</ul></div></div>

<h2>Systems schedule</h2>
<div class="cols"><div class="panel"><h3>Tram T1 · ${esc(plan.tram[0].name)}</h3>${stops}<p class="detail">Beyond: ${esc(plan.tram[0].beyond)}. Tram crosses traffic (signals for cars and trams) at: ${crossTxt}. People cross anywhere.</p></div>
<div class="panel"><h3>Sky rail S1 · ${esc(plan.skyrail[0].name)}</h3>${stations}<p class="detail">Beyond: ${esc(plan.skyrail[0].beyond)}. Beam height ${std.skyrail.height} m; trains turn on the loop round the Skyport.</p></div>
<div class="panel"><h3>Gates by environmental area</h3>${table(['Area', 'Gates'], byArea)}</div>
<div class="panel">${routes ? `<h3>Routes</h3><ul>${routes}</ul>` : ''}<h3 style="margin-top:12px">Nearest transit on foot (cross anywhere)</h3><ul>${walks}</ul></div></div>

<h2>Checks</h2>
<div class="panel">${checks}</div>

<h2>Sheet 2 · ${esc(s.inset.name)}</h2>
<div class="card"><div class="inset"><div class="view">${svgs.inset}</div><div><ol>${svgs.legend}</ol></div></div>${tb(s.inset.drawing, `${s.inset.name}: the civic block at exact geometry`, s.inset.scale)}</div>

<h2>Design basis</h2>
<div class="cols"><div class="panel">${basis}</div><div class="panel"><h3>Sources</h3><ul>${plan.notes.sources.map(x => `<li>${esc(x)}</li>`).join('')}</ul></div></div>

<h2>Open questions</h2>
<div class="panel"><ol>${plan.notes.questions.map(q => `<li>${esc(q)}</li>`).join('')}</ol></div>

<footer>Source of truth: <code>${esc(source)}</code> (suborned.plan/1). This sheet is generated: <code>bun tools/plan/cli.ts</code>. Frame: x east, y north, metres from the Crown Circus centre; Unity (x, height, y).</footer>
</div><script>${JS}</script></body></html>`;
}
