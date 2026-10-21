// Builds index.html and shot-N.svg for brief 024 "Revision: fewer keys, easy locks".
//   bun briefs/024/fewer-keys/build.ts ; PNG check: rsvg-convert shot-N.svg -o shot-N.png
// Bases are the build's own shots (0.2.32): base-tp.png = briefs/024/build/tp-firing.png, base-cmd.png = commander-firing.png.
// (librsvg will not follow "../", so the bases are copied beside this file.) Everything drawn on top is mockup.
import { readFileSync, writeFileSync } from "fs";
const here = import.meta.dir;
const radial = readFileSync(here + "/../../030/radial.html", "utf8").split("\n");
const SYMS =
  radial.slice(23, 45).join("\n") +
  `
<symbol id="i-burst" viewBox="0 0 20 20"><path d="M1 6h11l2 2v3h-4l-1 4H5l1-4H1z" fill="currentColor"/><path d="M16 5l2.500-1.500M17 9h2.500M16 12.500l2.500 1.500" stroke="currentColor" stroke-width="1.800" stroke-linecap="round"/></symbol>
<symbol id="i-mouse" viewBox="0 0 20 20"><rect x="4.500" y="1.500" width="11" height="17" rx="5.500" fill="none" stroke="currentColor" stroke-width="1.600"/><path d="M10 1.500V9H4.500V7a5.500 5.500 0 015.500-5.500z" fill="currentColor"/></symbol>
<symbol id="i-lock" viewBox="0 0 20 20"><path d="M2 7V2h5M13 2h5v5M18 13v5h-5M7 18H2v-5" fill="none" stroke="currentColor" stroke-width="2.200" stroke-linecap="round" stroke-linejoin="round"/><circle cx="10" cy="10" r="2.400" fill="currentColor"/></symbol>`;
const C = { teal: "#0f4a45", teal2: "#16665e", cream: "#f4ecd8", cell: "#4fe3e8", brass: "#c9a35a", ink: "#12302d", uni: "#2f8a58" };
type A = Record<string, string | number>;
const at = (a: A) => Object.entries(a).map(([k, v]) => `${k}="${v}"`).join(" ");
const h = (t: string, a: A, inner = "") => (inner === "" && t !== "text" ? `<${t} ${at(a)}/>` : `<${t} ${at(a)}>${inner}</${t}>`);
const g = (inner: string, a: A = {}) => h("g", a, inner);
const rect = (x: number, y: number, w: number, hh: number, fill: string, a: A = {}) => h("rect", { x, y, width: w, height: hh, fill, ...a });
const circ = (cx: number, cy: number, r: number, fill: string, a: A = {}) => h("circle", { cx, cy, r, fill, ...a });
const ell = (cx: number, cy: number, rx: number, ry: number, fill: string, a: A = {}) => h("ellipse", { cx, cy, rx, ry, fill, ...a });
const txt = (x: number, y: number, s: string, size: number, fill: string, a: A = {}) => h("text", { x, y, "font-size": size, fill, ...a }, s);
const path = (d: string, a: A) => h("path", { d, ...a });
const ic = (id: string, x: number, y: number, s: number, col = C.cream) => h("use", { href: "#i-" + id, x: x - s / 2, y: y - s / 2, width: s, height: s, style: `color:${col}` });
const W = (s: string, size: number, bold = false) => s.length * size * (bold ? 0.6 : 0.54);
const cap = (x: number, y: number, label: string, size = 15) => {
  const w = Math.max(26, label.length * size * 0.62 + 14);
  return { w, s: g(rect(x, y - 13, w, 26, C.cream, { rx: 5 }) + txt(x + w / 2, y + 5.500, label, size, C.teal, { "text-anchor": "middle", "font-weight": 700 })) };
};

// the build's roster portraits (crops of the base shot) and where they sit
const PORT = [[57, 66], [57, 141], [57, 218], [57, 293]];
const PSRC = [[26, 35], [26, 110], [26, 187], [26, 262]];
let uid = 0;
function face(base: string, n: number, cx: number, cy: number, r: number, a: A = {}): string {
  const id = "cf" + ++uid, [sx, sy] = PSRC[n - 1];
  return `<clipPath id="${id}"><circle cx="${cx}" cy="${cy}" r="${r}"/></clipPath>` +
    g(h("svg", { x: cx - r, y: cy - r, width: 2 * r, height: 2 * r, viewBox: `${sx} ${sy} 62 62` }, h("image", { href: base, x: 0, y: 0, width: 1920, height: 1080 })), { "clip-path": `url(#${id})`, ...a }) +
    circ(cx, cy, r, "none", { stroke: C.cell, "stroke-width": 3 });
}

// ---------------------------------------------------------------- pieces
type Part = { i?: string; t?: string; b?: boolean; col?: string; k?: string; face?: [string, number] };
function pill(x: number, y: number, parts: Part[], minW = 0): string {
  let w = 22;
  for (const p of parts) w += p.i ? 28 : p.face ? 34 : p.k ? cap(0, 0, p.k, 15).w + 8 : W(p.t!, p.b ? 17 : 15, !!p.b) + 12;
  w = Math.max(w, minW);
  const hh = 34;
  let s = rect(x, y, w, hh, C.teal, { rx: 8, stroke: C.brass, "stroke-width": 1.500 });
  let tx = x + 12;
  for (const p of parts) {
    if (p.i) { s += ic(p.i, tx + 11, y + 17, 22, p.col); tx += 28; }
    else if (p.face) { s += face(p.face[0], p.face[1], tx + 13, y + 17, 13); tx += 34; }
    else if (p.k) { const c = cap(tx, y + 17, p.k, 15); s += c.s; tx += c.w + 8; }
    else { s += txt(tx, y + 22.500, p.t!, p.b ? 17 : 15, p.col ?? C.cream, p.b ? { "font-weight": 700 } : {}); tx += W(p.t!, p.b ? 17 : 15, !!p.b) + 12; }
  }
  return s;
}
// four corner ticks round a person, with the numbers of the agents on him (one mark, so the two-icon limit holds)
function bracket(cx: number, top: number, bot: number, w: number, nums: number[], hot: boolean, ghost = false): string {
  const x0 = cx - w / 2, x1 = cx + w / 2, L = 15;
  const d = `M${x0} ${top + L}V${top}H${x0 + L}M${x1 - L} ${top}H${x1}V${top + L}M${x0} ${bot - L}V${bot}H${x0 + L}M${x1 - L} ${bot}H${x1}V${bot - L}`;
  let o = path(d, { stroke: "#fff", "stroke-width": hot ? 9 : 7, fill: "none", "stroke-linecap": "round", "stroke-linejoin": "round", opacity: 0.8 });
  o += path(d, { stroke: C.cell, "stroke-width": hot ? 5 : 3.500, fill: "none", "stroke-linecap": "round", "stroke-linejoin": "round", ...(hot ? { class: "pulse" } : {}) });
  if (ghost) return o;
  const n = nums.length, cw = 24, x = cx - (n * cw) / 2;
  nums.forEach((k, i) => (o += circ(x + i * cw + cw / 2, top - 16, 11, C.cream, { stroke: C.teal, "stroke-width": 2 }) + txt(x + i * cw + cw / 2, top - 11.500, String(k), 14, C.teal, { "text-anchor": "middle", "font-weight": 700 })));
  return o;
}
const tether = (x0: number, y0: number, x1: number, y1: number, firing: boolean) =>
  firing
    ? path(`M${x0} ${y0}L${x1} ${y1}`, { stroke: "#fff", "stroke-width": 7, opacity: 0.6, fill: "none", "stroke-linecap": "round" }) + path(`M${x0} ${y0}L${x1} ${y1}`, { stroke: C.cell, "stroke-width": 3, "stroke-dasharray": "26 12", fill: "none", class: "tr" })
    : path(`M${x0} ${y0}L${x1} ${y1}`, { stroke: "#fff", "stroke-width": 5, opacity: 0.55, fill: "none", "stroke-linecap": "round" }) + path(`M${x0} ${y0}L${x1} ${y1}`, { stroke: C.cell, "stroke-width": 2.500, "stroke-dasharray": "1 8", fill: "none", "stroke-linecap": "round" });
const cursor = (x: number, y: number) => path(`M${x} ${y}v26l7-6 5 11 5-2-5-11h9z`, { fill: "#fff", stroke: C.ink, "stroke-width": 1.800, "stroke-linejoin": "round" });
// the slow-down while a drag is held: a quarter-speed chip on the clock and a turquoise rim (the freeze look)
const slow = () =>
  rect(5, 5, 1910, 1070, "none", { stroke: C.cell, "stroke-width": 10, opacity: 0.45, rx: 6 }) +
  g(rect(1531, 22, 40, 28, C.teal, { rx: 7, stroke: C.cell, "stroke-width": 2 }) + txt(1551, 43, "¼", 19, C.cell, { "text-anchor": "middle", "font-weight": 700 }));
// a person in the officers' green (stand-in: the build has one officer at the car; the second is drawn)
function officer(x: number, foot: number, hgt: number): string {
  const u = hgt / 100;
  return ell(x, foot + 2, 20 * u, 6 * u, "#00000030") +
    rect(x - 9 * u, foot - 46 * u, 7.500 * u, 46 * u, "#22563a", { rx: 2 }) + rect(x + 1.500 * u, foot - 46 * u, 7.500 * u, 46 * u, "#22563a", { rx: 2 }) +
    path(`M${x - 15 * u} ${foot - 44 * u}L${x - 11 * u} ${foot - 84 * u}Q${x} ${foot - 90 * u} ${x + 11 * u} ${foot - 84 * u}L${x + 15 * u} ${foot - 44 * u}Z`, { fill: C.uni }) +
    circ(x, foot - 94 * u, 8 * u, "#d9b08c") + path(`M${x - 9 * u} ${foot - 97 * u}h${18 * u}v-4h-${18 * u}z`, { fill: "#22563a" });
}

// ---------------------------------------------------------------- the four states
// Third person: the base shot with the key bar painted out (the paving beside it, feathered) and the built goal line put back.
function tpBase(): string {
  const mask = `<defs><filter id="bl" x="-20%" y="-50%" width="140%" height="200%"><feGaussianBlur stdDeviation="6"/></filter><mask id="pm" maskUnits="userSpaceOnUse" x="600" y="1000" width="700" height="100"><rect x="672" y="1030" width="576" height="70" fill="#fff" filter="url(#bl)"/></mask></defs>`;
  const patch = g(h("svg", { x: 640, y: 1024, width: 640, height: 56, viewBox: "1250 1024 640 56" }, h("image", { href: "base-tp.png", x: 0, y: 0, width: 1920, height: 1080 })), { mask: "url(#pm)" });
  return mask + h("image", { href: "base-tp.png", x: 0, y: 0, width: 1920, height: 1080 }) + patch + patch +
    g(rect(845, 1030, 231, 34, C.teal, { rx: 8, stroke: C.brass, "stroke-width": 1.500, opacity: 0.95 }) + txt(960, 1052, "Watch the café terrace.", 14, C.cream, { "text-anchor": "middle" }));
}
const stateBefore = () => h("image", { href: "base-tp.png", x: 0, y: 0, width: 1920, height: 1080 }) + g(rect(691, 1028, 538, 42, "none", { stroke: "#e0604a", "stroke-width": 4, rx: 8, "stroke-dasharray": "10 6" }));
const stateTp = () => tpBase();

// Commander: the build's shot, the old "Firing at Vale's car" line covered by the state's own line.
const cmdBase = () => h("image", { href: "base-cmd.png", x: 0, y: 0, width: 1920, height: 1080 });
const O1 = { x: 1172, foot: 538 }, O2 = { x: 1262, foot: 566 };
const agents = [{ n: 1, x: 613, y: 508 }, { n: 2, x: 655, y: 533 }, { n: 3, x: 751, y: 514 }];
const hostileMark = (x: number, y: number) => circ(x, y, 14, C.teal, { stroke: C.cream, "stroke-width": 2 }) + ic("gun", x, y, 18, C.cream);

function stateAppear(): string {
  let o = cmdBase();
  o += pill(1226, 394, [{ i: "horn", col: C.cream }, { t: "Police coming in", b: true }, { t: "2 officers" }], 354);
  o += officer(O2.x, O2.foot, 66);
  o += hostileMark(O1.x, O1.foot - 80) + hostileMark(O2.x, O2.foot - 84);
  // the first-time hint: a ghost of the gesture, no words but the one cap
  const sx = PORT[1][0] + 34, sy = PORT[1][1], ex = O2.x - 24, ey = O2.foot - 40;
  o += path(`M${sx} ${sy}L${ex} ${ey}`, { stroke: "#fff", "stroke-width": 9, opacity: 0.5, fill: "none", "stroke-linecap": "round" });
  o += path(`M${sx} ${sy}L${ex} ${ey}`, { stroke: C.cream, "stroke-width": 3.500, "stroke-dasharray": "14 12", fill: "none", class: "tr", opacity: 0.95 });
  const gx = sx + (ex - sx) * 0.36, gy = sy + (ey - sy) * 0.36;
  o += face("base-cmd.png", 2, gx, gy, 22, { opacity: 0.7 });
  const c = cap(gx + 30, gy - 30, "drag", 16); o += c.s;
  o += bracket(O2.x, O2.foot - 100, O2.foot + 4, 46, [], false, true);
  return o;
}
function stateLocks(): string {
  let o = cmdBase();
  o += officer(O2.x, O2.foot, 66);
  // Scout and Tech are already on the first officer (dotted until the gun speaks, then the tracer), the Fixer's drag is in flight to the second
  o += tether(agents[0].x + 12, agents[0].y - 20, O1.x - 4, O1.foot - 36, true);
  o += tether(agents[2].x + 12, agents[2].y - 20, O1.x - 4, O1.foot - 30, true);
  o += bracket(O1.x, O1.foot - 100, O1.foot + 4, 46, [1, 3], false);
  // the fire line, now on the officer
  o += pill(1226, 394, [{ i: "burst", col: C.cell }, { t: "Firing at the officer", b: true }, { t: "down in 4 s" }, { k: "X" }, { t: "stop" }], 354);
  // portraits 1 and 3 wear the lock; portrait 2 is lifted
  for (const n of [1, 3]) { const [px, py] = PORT[n - 1]; o += circ(px + 25, py + 24, 13, C.cell, { stroke: C.cream, "stroke-width": 2.500 }) + ic("lock", px + 25, py + 24, 17, C.teal); }
  const [fx, fy] = PORT[1];
  o += circ(fx, fy, 34, "none", { stroke: C.cell, "stroke-width": 4, class: "pulse" });
  // the drag: from the Fixer's portrait to the second officer
  const cx = O2.x - 4, cy = O2.foot - 50;
  o += path(`M${fx + 30} ${fy}L${cx} ${cy}`, { stroke: "#fff", "stroke-width": 11, opacity: 0.7, fill: "none", "stroke-linecap": "round" });
  o += path(`M${fx + 30} ${fy}L${cx} ${cy}`, { stroke: C.cell, "stroke-width": 4.500, "stroke-dasharray": "16 10", fill: "none", class: "tr" });
  o += bracket(O2.x, O2.foot - 100, O2.foot + 4, 46, [], true);
  o += face("base-cmd.png", 2, cx - 30, cy - 6, 22);
  o += cursor(cx + 6, cy - 4);
  o += pill(cx + 30, cy + 34, [{ face: ["base-cmd.png", 2] }, { t: "down in 6 s" }]);
  o += slow();
  return o;
}

const svg = (inner: string, defs: boolean) =>
  `<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 1920 1080" width="1920" height="1080" font-family="Helvetica Neue, Arial, sans-serif">${defs ? `<defs>${SYMS}</defs>` : ""}${inner}</svg>`;
const states = [
  { id: 0, name: "0.2.32 (before)", fn: stateBefore, cap: "Before, build 0.2.32: the bottom bar (outlined) lists seven key caps on the same screen as the fight. Sky: 'if it needs that much explaining something's wrong.'" },
  { id: 1, name: "Third person, firing", fn: stateTp, cap: "The same moment without the bar. The line at the target already says what matters now: who, how long, the mouse held. Nothing else is taught here, because the player has just used LMB. The bar's other keys come back one at a time, on the thing, the first few times they matter, and then never again." },
  { id: 2, name: "Commander, officers appear", fn: stateAppear, cap: "Vale's doors open, two officers come in (the beat line). The first time a hostile appears with agents idle, a ghost of the gesture runs from a portrait to the officer: one cap, 'drag'. No sentence. It plays three times in the game, never again once the player has done it." },
  { id: 3, name: "Commander, locks set", fn: stateLocks, cap: "Scout and Tech are on the first officer (tracers, a bracket with their numbers, the lock mark on their portraits). The Fixer's portrait is being dragged onto the second: the rubber line, the target pulsing, one price line at the cursor ('down in 6 s'). While the drag is held the world runs at a quarter speed (¼ on the clock, turquoise rim, the freeze look) and for half a second after it. Drop: Fixer is locked and the clock returns." },
];
for (const s of states) writeFileSync(`${here}/shot-${s.id}.svg`, svg(s.fn(), true));
const html = `<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Brief 024 · fewer keys, easy locks</title>
<style>body{margin:0;background:#efe7d3;color:#1d2b29;font:15px/1.5 "Helvetica Neue",Arial,sans-serif}
header{padding:14px 20px;display:flex;gap:14px;align-items:center;flex-wrap:wrap}h1{font-size:20px;margin:0}
button{font:inherit;font-weight:700;border:2px solid #0f4a45;background:#fbf7ec;color:#0f4a45;border-radius:8px;padding:4px 14px;cursor:pointer}button[aria-pressed=true]{background:#0f4a45;color:#f4ecd8}
.cap{padding:0 20px 12px;max-width:1500px}#wrap{transform-origin:0 0}@keyframes tr{to{stroke-dashoffset:-38}}.tr{animation:tr .3s linear infinite}@keyframes pl{50%{opacity:.4}}.pulse{animation:pl 1s ease-in-out infinite}svg.stage{display:none;width:1920px;height:1080px}svg.stage.on{display:block}</style></head><body>
<svg width="0" height="0" style="position:absolute"><defs>${SYMS}</defs></svg>
<header><h1>Fewer keys, easy locks · 1920 × 1080</h1>${states.map((s) => `<button data-s="${s.id}" aria-pressed="${s.id === 1}">${s.id} · ${s.name}</button>`).join("")}</header>
<div class="cap" id="cap"></div>
<div id="wrap">${states.map((s) => svg(s.fn(), false).replace("<svg ", `<svg class="stage${s.id === 1 ? " on" : ""}" id="st${s.id}" `)).join("")}</div>
<script>
const caps=${JSON.stringify(states.map((s) => s.cap))};
function show(n){document.querySelectorAll('svg.stage').forEach(e=>e.classList.toggle('on',e.id==='st'+n));document.querySelectorAll('button').forEach(b=>b.setAttribute('aria-pressed',b.dataset.s==n));document.getElementById('cap').textContent=caps[n]}
document.querySelectorAll('button').forEach(b=>b.onclick=()=>show(b.dataset.s));addEventListener('keydown',e=>{if('0123'.includes(e.key))show(+e.key)});
function fit(){const k=Math.min(1,innerWidth/1920);document.getElementById('wrap').style.transform='scale('+k+')';document.getElementById('wrap').style.height=(1080*k)+'px'}addEventListener('resize',fit);fit();show(1);
</script></body></html>`;
writeFileSync(here + "/index.html", html);
console.log("ok", html.length);
