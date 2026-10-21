// Builds index.html (+ shot-*.svg for checking) for brief 024 "Revision: action-game controls".
// bun briefs/024/action-controls/build.ts ; shots: rsvg-convert shot-1.svg -o shot-1.png
import { readFileSync, writeFileSync } from "fs";
const here = import.meta.dir;
const radial = readFileSync(here + "/../../030/radial.html", "utf8").split("\n");
const SYMS =
  radial.slice(23, 45).join("\n") +
  `
<symbol id="i-eyes" viewBox="0 0 20 20"><path d="M1.5 9C4 14 16 14 18.5 9M4 12l-2 3M10 13.500V17M16 12l2 3" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></symbol>
<symbol id="i-burst" viewBox="0 0 20 20"><path d="M1 6h11l2 2v3h-4l-1 4H5l1-4H1z" fill="currentColor"/><path d="M16 5l2.500-1.500M17 9h2.500M16 12.500l2.500 1.500" stroke="currentColor" stroke-width="1.800" stroke-linecap="round"/></symbol>
<symbol id="i-ret" viewBox="0 0 20 20"><path d="M2 2.500h11l2 2v3h-4l-1 3.500H6l1-3.500H2z" fill="currentColor"/><path d="M18 16H9M12 12.500L8.500 16l3.500 3.500" fill="none" stroke="currentColor" stroke-width="1.800" stroke-linecap="round" stroke-linejoin="round"/></symbol>
<symbol id="i-mouse" viewBox="0 0 20 20"><rect x="4.500" y="1.500" width="11" height="17" rx="5.500" fill="none" stroke="currentColor" stroke-width="1.600"/><path d="M10 1.500V9H4.500V7a5.500 5.500 0 015.500-5.500z" fill="currentColor"/></symbol>
<symbol id="i-mouser" viewBox="0 0 20 20"><rect x="4.500" y="1.500" width="11" height="17" rx="5.500" fill="none" stroke="currentColor" stroke-width="1.600"/><path d="M10 1.500V9h5.500V7A5.500 5.500 0 0010 1.500z" fill="currentColor"/></symbol>`;
const C = { teal: "#0f4a45", teal2: "#16665e", cream: "#f4ecd8", cell: "#4fe3e8", guard: "#f0a020", brass: "#c9a35a", ink: "#12302d" };
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
const cap = (x: number, y: number, label: string, size = 15, dim = false) => {
  const w = Math.max(26, label.length * size * 0.62 + 14);
  return { w, s: g(rect(x, y - 13, w, 26, C.cream, { rx: 5, opacity: dim ? 0.55 : 1 }) + txt(x + w / 2, y + 5.5, label, size, C.teal, { "text-anchor": "middle", "font-weight": 700 })) };
};
const W = (s: string, size: number, bold = false) => s.length * size * (bold ? 0.6 : 0.54);

// ---------------------------------------------------------------- scene
function scene(): string {
  let o = "";
  o += rect(0, 0, 1920, 1080, "#cfe6ea");
  // skyline
  for (const [x, w, y] of [[560, 90, 40], [1560, 110, 20], [1700, 80, 90], [1810, 120, 50]]) o += rect(x, y, w, 400, "#e3ece6", { rx: 30 });
  // left building
  o += rect(0, 90, 650, 540, "#e8bd96");
  for (let r = 0; r < 3; r++) for (let c = 0; c < 4; c++) o += circ(70 + c * 150, 170 + r * 100, 30, "#0f5a54", { stroke: "#16665e", "stroke-width": 8 });
  // cafe tower
  o += rect(650, 0, 880, 640, "#f2ead3");
  o += rect(650, 0, 44, 640, "#1c7a6e") + rect(1486, 0, 44, 640, "#1c7a6e");
  for (let c = 0; c < 4; c++) for (let r = 0; r < 2; r++) o += rect(730 + c * 190, 40 + r * 150, 150, 120, "#16665e", { stroke: "#c9a35a", "stroke-width": 4 });
  o += rect(650, 335, 880, 26, "#8fd0c0");
  o += rect(700, 440, 780, 170, "#bfe3dc");
  o += rect(700, 520, 780, 90, "#e7cf9c", { opacity: 0.55 });
  for (let i = 0; i < 6; i++) o += rect(760 + i * 130, 440, 8, 170, "#d9d0b4");
  // awning
  for (let i = 0; i < 26; i++) o += rect(690 + i * 31, 396, 31, 46, i % 2 ? "#f7eedb" : "#f08a60");
  // sign
  o += rect(1420, 110, 62, 210, "#0f4a45", { stroke: C.brass, "stroke-width": 3, rx: 6 });
  "CAFE".split("").forEach((l, i) => (o += txt(1451, 160 + i * 46, l, 40, C.cream, { "text-anchor": "middle", "font-weight": 700 })));
  // right building
  o += rect(1530, 90, 390, 540, "#e0b48c");
  for (let r = 0; r < 3; r++) for (let c = 0; c < 2; c++) o += rect(1590 + c * 160, 150 + r * 110, 100, 70, "#16665e", { rx: 8 });
  // ground
  o += rect(0, 610, 1920, 470, "#efe3c6");
  for (let x = -1400; x <= 3300; x += 220) o += h("line", { x1: 960 + (x - 960) * 0.18, y1: 610, x2: x, y2: 1080, stroke: "#dccfac", "stroke-width": 3 });
  for (const y of [640, 685, 745, 835, 960, 1100]) o += h("line", { x1: 0, y1: y, x2: 1920, y2: y, stroke: "#dccfac", "stroke-width": 3 });
  // planter and hedge
  o += rect(690, 594, 800, 30, "#e8dcc0", { stroke: "#cdbf9d", "stroke-width": 2 });
  for (let i = 0; i < 17; i++) o += circ(715 + i * 47, 590, 22, i % 4 === 2 ? "#6fb36a" : "#4aa05a");
  // umbrellas
  for (const x of [880, 1300]) o += path(`M${x - 90} 505 L${x} 440 L${x + 90} 505 Z`, { fill: "#f08a60" }) + rect(x - 3, 505, 6, 90, "#d9d0b4");
  // lamp post
  o += rect(886, 330, 12, 290, "#c8d2d4") + circ(892, 316, 24, "#dfe8ea", { stroke: "#9fb0b3", "stroke-width": 3 }) + rect(874, 590, 36, 40, C.teal);
  return o;
}

function person(x: number, foot: number, hgt: number, body: string, o: { hair?: string; apron?: boolean; back?: boolean; skin?: string; trim?: string } = {}): string {
  const u = hgt / 100, skin = o.skin ?? "#e2b995", hair = o.hair ?? "#2a2220";
  let s = ell(x, foot, 17 * u, 5 * u, "#0003");
  s += rect(x - 8 * u, foot - 40 * u, 7 * u, 40 * u, "#2a3a42") + rect(x + 1 * u, foot - 40 * u, 7 * u, 40 * u, "#2a3a42");
  s += rect(x - 12 * u, foot - 78 * u, 24 * u, 42 * u, body, { rx: 8 * u });
  if (o.trim) s += rect(x - 12 * u, foot - 50 * u, 24 * u, 5 * u, o.trim);
  if (o.apron) s += rect(x - 9 * u, foot - 62 * u, 18 * u, 26 * u, "#fbf6e8", { rx: 3 * u });
  s += rect(x - 17 * u, foot - 74 * u, 6 * u, 30 * u, skin, { rx: 3 * u }) + rect(x + 11 * u, foot - 74 * u, 6 * u, 30 * u, skin, { rx: 3 * u });
  s += circ(x, foot - 88 * u, 10 * u, o.back ? hair : skin);
  if (!o.back) s += path(`M${x - 10 * u} ${foot - 90 * u} a${10 * u} ${10 * u} 0 0 1 ${20 * u} 0 q${-10 * u} ${-6 * u} ${-20 * u} 0`, { fill: hair });
  if (o.back) s += path(`M${x} ${foot - 82 * u} q${5 * u} ${22 * u} 0 ${40 * u} q${-6 * u} ${-20 * u} 0 ${-40 * u}`, { fill: hair });
  return s;
}
const ground = (x: number, y: number, rx: number, col: string, sw = 5) =>
  ell(x, y, rx, rx * 0.3, "none", { stroke: "#fff", "stroke-width": sw + 3 }) + ell(x, y, rx, rx * 0.3, "none", { stroke: col, "stroke-width": sw });

// ---------------------------------------------------------------- HUD (shared)
function hud(held: number, drawn: boolean, mk: { firing?: number; ret?: number } = {}): string {
  let o = "";
  // roster
  o += rect(16, 24, 104, 430, C.teal, { rx: 22, stroke: C.brass, "stroke-width": 2 });
  const cols = ["#6aa0c8", "#c0587a", "#c9a35a", "#2c8e93"];
  for (let k = 0; k < 4; k++) {
    const cy = 82 + k * 98, sel = k === held;
    o += circ(68, cy, 38, cols[k], { stroke: sel ? C.cell : C.cream, "stroke-width": sel ? 5 : 2 });
    o += circ(68, cy - 6, 12, "#e2b995") + path(`M44 ${cy + 28} q24 -34 48 0`, { fill: "#2a3a42" });
    o += cap(26, cy - 26, String(k + 1), 15).s;
    if (sel) {
      o += circ(98, cy + 28, 15, drawn ? C.teal2 : C.teal, { stroke: C.cream, "stroke-width": 2 }) + ic(drawn ? "gun" : "gunh", 98, cy + 28, 18, drawn ? C.cell : C.cream);
    }
    if (mk.firing === k) o += circ(98, cy + 28, 21, "none", { stroke: C.cell, "stroke-width": 3, class: "pulse" }) + circ(98, cy + 28, 15, C.cell, { stroke: C.cream, "stroke-width": 2 }) + ic("burst", 98, cy + 28, 20, C.teal);
    if (mk.ret === k) o += circ(98, cy + 28, 15, C.teal2, { stroke: C.cream, "stroke-width": 2, "stroke-dasharray": "3 3" }) + ic("ret", 98, cy + 28, 19, C.cream);
  }
  // status pill
  o += rect(1400, 22, 500, 58, C.teal, { rx: 29, stroke: C.brass, "stroke-width": 2 });
  o += txt(1430, 62, "0:29", 34, C.cream, { "font-weight": 700 });
  o += txt(1540, 60, "$100,000", 24, C.cream);
  for (let i = 0; i < 3; i++) o += circ(1730 + i * 30, 51, 9, "none", { stroke: C.cream, "stroke-width": 2.5 });
  o += rect(1856, 38, 9, 26, C.cream) + rect(1872, 38, 9, 26, C.cream);
  // scope
  o += rect(24, 836, 240, 168, C.teal, { rx: 14, stroke: C.brass, "stroke-width": 2 });
  for (let i = 1; i < 6; i++) o += h("line", { x1: 24 + i * 40, y1: 846, x2: 24 + i * 40, y2: 994, stroke: "#2a7d73", "stroke-width": 1 });
  for (let i = 1; i < 4; i++) o += h("line", { x1: 34, y1: 836 + i * 42, x2: 254, y2: 836 + i * 42, stroke: "#2a7d73", "stroke-width": 1 });
  o += path("M150 930 L196 870 L220 884 Z", { fill: "#e8bd96", opacity: 0.5 }) + circ(150, 930, 8, C.cell) + path("M150 916 l8 12 h-16 z", { fill: C.cream });
  o += rect(276, 960, 118, 34, C.teal, { rx: 8, stroke: C.brass }) + txt(290, 983, "Map", 18, C.cream) + cap(342, 977, "M", 15).s;
  return o;
}
function strip(): string {
  const items: [string, string][] = [["WASD", "move"], ["Shift", "run"], ["E", "act"], ["hold E", "options"], ["RMB", "aim"], ["LMB", "fire"], ["F", "draw"]];
  let x = 0;
  const parts: { x: number; s: string }[] = [];
  for (const [k, l] of items) {
    const c = cap(0, 0, k, 15); const w = c.w + 10 + W(l, 17) + 22;
    parts.push({ x, s: "" }); x += w + 8;
  }
  const total = x - 8, x0 = 960 - total / 2 + 60;
  let o = rect(x0 - 14, 1010, total + 28, 44, C.teal, { rx: 10, stroke: C.brass, opacity: 0.96 });
  let cx = x0;
  for (const [k, l] of items) {
    const c = cap(cx, 1032, k, 15);
    o += c.s + txt(cx + c.w + 8, 1038, l, 17, C.cream);
    cx += c.w + 10 + W(l, 17) + 30;
  }
  return o;
}

// ---------------------------------------------------------------- prompt pill (world marker)
type Part = { i?: string; t?: string; b?: boolean; col?: string; k?: string };
function pill(cx: number, y: number, parts: Part[], o: { keys: boolean; dots?: boolean; hot?: boolean }): string {
  let w = 14;
  if (o.keys) w += 34 + 10 + 22 + 10;
  for (const p of parts) w += p.i ? 28 : p.k ? cap(0, 0, p.k, 17).w + 8 : W(p.t!, 17, !!p.b) + 12;
  if (o.dots) w += 40;
  const x = cx - w / 2, hh = 38;
  let s = rect(x, y, w, hh, C.teal, { rx: 8, stroke: C.brass, "stroke-width": 1.5 });
  let tx = x + 10;
  if (o.keys) { s += cap(tx, y + 19, "E", 17).s; tx += 38 + 4; s += ic("mouse", tx + 10, y + 19, 22, C.cream); tx += 32 }
  for (const p of parts) { if (p.i) { s += ic(p.i, tx + 11, y + 19, 22, p.col); tx += 28 } else if (p.k) { const c = cap(tx, y + 19, p.k, 17); s += c.s; tx += c.w + 8 } else { s += txt(tx, y + 25, p.t!, 17, p.col ?? C.cream, { "font-weight": p.b ? 700 : 400 }); tx += W(p.t!, 17, !!p.b) + 12 } }
  if (o.dots) { s += h("line", { x1: x + w - 38, y1: y + 8, x2: x + w - 38, y2: y + hh - 8, stroke: C.brass, "stroke-width": 1 }) + ic("more", x + w - 20, y + 19, 20, C.cell) }
  return s;
}

// ---------------------------------------------------------------- ring (030 spec: outer 64, inner 26)
type Seg = { i: string; w: string; p: Part[]; hot?: boolean; dash?: boolean };
function ring(cx: number, cy: number, segs: Seg[], hold: number): string {
  const R = 64, r = 26, n = segs.length, step = (2 * Math.PI) / n;
  let o = circ(cx, cy, R + 8, "#0f4a4544");
  segs.forEach((s, k) => {
    const am = -Math.PI / 2 + k * step, a0 = am - step / 2 + 0.03, a1 = am + step / 2 - 0.03;
    const P = (rad: number, a: number): [number, number] => [cx + rad * Math.cos(a), cy + rad * Math.sin(a)];
    const [x0, y0] = P(R, a0), [x1, y1] = P(R, a1), [x2, y2] = P(r, a1), [x3, y3] = P(r, a0);
    o += path(`M${x0} ${y0}A${R} ${R} 0 0 1 ${x1} ${y1}L${x2} ${y2}A${r} ${r} 0 0 0 ${x3} ${y3}Z`, { fill: s.hot ? C.teal2 : C.teal, stroke: s.hot ? C.cell : C.brass, "stroke-width": s.hot ? 3 : 1.5, ...(s.dash ? { "stroke-dasharray": "6 4" } : {}) });
    const [ix, iy] = P((R + r) / 2, am);
    o += ic(s.i, ix, iy, 22);
    const dx = Math.cos(am), dy = Math.sin(am), right = dx > 0.2, left = dx < -0.2;
    let pw = 40 + W(s.w, 15, true);
    for (const p of s.p) pw += p.i ? 20 : W(p.t!, 14) + 8;
    const ph = 32; let px: number, py: number;
    if (right) { px = cx + R * dx + 18; py = cy + R * dy * 0.9 - ph / 2 } else if (left) { px = cx + R * dx - 18 - pw; py = cy + R * dy * 0.9 - ph / 2 } else { px = cx - pw / 2; py = dy < 0 ? cy - R - 10 - ph : cy + R + 10 }
    o += rect(px, py, pw, ph, s.hot ? C.teal2 : C.teal, { rx: 6, stroke: s.hot ? C.cell : C.brass, "stroke-width": s.hot ? 2 : 1, ...(s.dash ? { "stroke-dasharray": "6 4" } : {}) });
    let tx = px + 10;
    o += ic(s.i, tx + 8, py + 16, 17) ; tx += 26;
    o += txt(tx, py + 21, s.w, 15, C.cream, { "font-weight": 700 }); tx += W(s.w, 15, true) + 8;
    for (const p of s.p) { if (p.i) { o += ic(p.i, tx + 7, py + 16, 15, p.col); tx += 20 } else { o += txt(tx, py + 21, p.t!, 14, p.col ?? "#cfe3de"); tx += W(p.t!, 14) + 8 } }
  });
  // centre: the E cap with its hold ring
  o += circ(cx, cy, 20, C.teal, { stroke: C.cream, "stroke-width": 1.5, opacity: 0.95 });
  const ang = hold * 2 * Math.PI, ex = cx + 20 * Math.sin(ang), ey = cy - 20 * Math.cos(ang);
  o += hold >= 1 ? circ(cx, cy, 20, "none", { stroke: C.cell, "stroke-width": 4 }) : path(`M${cx} ${cy - 20}A20 20 0 ${ang > Math.PI ? 1 : 0} 1 ${ex} ${ey}`, { fill: "none", stroke: C.cell, "stroke-width": 4 });
  o += txt(cx, cy + 6, "E", 17, C.cream, { "text-anchor": "middle", "font-weight": 700 });
  return o;
}

// ---------------------------------------------------------------- reticle
function reticle(cx: number, cy: number, fill: number, wide: boolean): string {
  let o = "";
  const ring_ = (col: string, sw: number) => circ(cx, cy, 24, "none", { stroke: col, "stroke-width": sw });
  const ticks = (col: string, sw: number) => path(`M${cx} ${cy - 40}v14M${cx} ${cy + 26}v14M${cx - 40} ${cy}h14M${cx + 26} ${cy}h14`, { stroke: col, "stroke-width": sw, fill: "none", "stroke-linecap": "round" });
  const band = wide ? 52 : 38;
  const bandTicks = (col: string, sw: number) => path(`M${cx - band} ${cy - 8}v16M${cx + band} ${cy - 8}v16`, { stroke: col, "stroke-width": sw, fill: "none", "stroke-linecap": "round" });
  o += ring_("#fff", 6) + ticks("#fff", 7) + bandTicks("#fff", 7);
  o += ring_(C.teal, 2.5) + ticks(C.teal, 3.5) + bandTicks(C.teal, 3.5) + circ(cx, cy, 2.5, C.teal);
  if (fill > 0) {
    const R = 33, ang = fill * 2 * Math.PI, ex = cx + R * Math.sin(ang), ey = cy - R * Math.cos(ang);
    o += circ(cx, cy, R, "none", { stroke: "#fff", "stroke-width": 9, opacity: 0.8 }) + path(`M${cx} ${cy - R}A${R} ${R} 0 ${ang > Math.PI ? 1 : 0} 1 ${ex} ${ey}`, { fill: "none", stroke: C.teal, "stroke-width": 5 });
  }
  return o;
}

// ---------------------------------------------------------------- the three states
const inez = { x: 1000, foot: 706, h: 150 }, guard = { x: 1610, foot: 700, h: 156 };
function cast(opts: { inezRing?: boolean; guardRing?: boolean }): string {
  let o = "";
  o += person(guard.x, guard.foot, guard.h, "#4d5c4a", { hair: "#2a2220", trim: "#c9a35a" });
  o += person(inez.x, inez.foot, inez.h, "#e0604a", { apron: true, hair: "#4a2c22" });
  if (opts.guardRing) o += ground(guard.x, guard.foot + 4, 40, C.guard, 5);
  if (opts.inezRing) o += ground(inez.x, inez.foot + 4, 44, C.cell, 5);
  return o;
}
const stem = (x: number, y0: number, y1: number) => path(`M${x} ${y0}V${y1 - 8}`, { stroke: C.brass, "stroke-width": 2 }) + path(`M${x - 8} ${y1 - 12}L${x} ${y1}L${x + 8} ${y1 - 12}`, { fill: C.cell, stroke: "none" });

function state1(): string {
  let o = scene() + cast({ inezRing: true, guardRing: true });
  // agent (Scout, from behind)
  o += person(720, 980, 320, "#1f2a30", { back: true, hair: "#1c1514", trim: "#f4ecd8" });
  o += ground(720, 984, 70, C.cell, 6);
  o += pill(inez.x, 456, [{ i: "eyes", col: C.cream }, { t: "Watch", b: true }, { t: "4 s" }, { t: "$0" }, { t: "T 0" }, { i: "eye" }], { keys: true, dots: true });
  o += stem(inez.x, 494, 548);
  o += hud(0, false) + strip();
  return o;
}
function state2(): string {
  let o = scene() + cast({ inezRing: true, guardRing: true });
  o += person(720, 980, 320, "#7a3a58", { back: true, hair: "#1c1514", trim: "#f4ecd8" });
  o += ground(720, 984, 70, C.cell, 6);
  const T = (t: string, col?: string): Part => ({ t, col });
  o += ring(inez.x, 600, [
    { i: "ask", w: "Ask", hot: true, p: [T("10 s"), T("$0"), T("T +1"), T("?")] },
    { i: "pay", w: "Pay", p: [T("14 s"), T("$40"), T("T +1"), T("?")] },
    { i: "help", w: "Help", p: [T("18 s"), T("$0"), T("T +1"), T("?")] },
    { i: "press", w: "Pressure", dash: true, p: [T("8 s"), T("T +2"), T("A ?")] },
    { i: "more", w: "All", p: [] },
  ], 1);
  o += hud(1, false) + strip();
  return o;
}
function aimBase(firing: boolean, stopped = false): string {
  let o = scene() + cast({ guardRing: true });
  const ax = 720, foot = 980, mx = ax + 172, my = 756, rx = guard.x, ry = guard.foot - 96;
  o += person(ax, foot, 320, "#1f2a30", { back: true, hair: "#1c1514", trim: "#4fe3e8" });
  o += ground(ax, foot + 4, 70, C.cell, 6);
  o += path(`M${ax + 112} 760 l56 -10 l4 14 l-24 6 l-4 18 l-18 2 z`, { fill: "#111c1e", stroke: "#fff", "stroke-width": 1.5 });
  if (firing) {
    // a second agent, not driven, returning fire (thin tracer, return mark)
    o += person(1330, 880, 170, "#7a3a58", { back: true, hair: "#1c1514" });
    o += path(`M1362 786 L${rx - 14} ${ry + 20}`, { stroke: "#fff", "stroke-width": 5, opacity: 0.6, fill: "none" }) + path(`M1362 786 L${rx - 14} ${ry + 20}`, { stroke: C.cell, "stroke-width": 2, "stroke-dasharray": "10 16", fill: "none", class: "tr" });
    o += circ(1330, 702, 17, C.teal2, { stroke: C.cream, "stroke-width": 2, "stroke-dasharray": "3 3" }) + ic("ret", 1330, 702, 20, C.cream);
    // the driven agent's tracer, muzzle flash, hit spark, firing badge
    o += path(`M${mx} ${my} L${rx} ${ry}`, { stroke: "#fff", "stroke-width": 9, opacity: 0.65, fill: "none", "stroke-linecap": "round" });
    o += path(`M${mx} ${my} L${rx} ${ry}`, { stroke: C.cell, "stroke-width": 3.5, "stroke-dasharray": "30 14", fill: "none", class: "tr" });
    o += path(`M${mx - 4} ${my} l14 -16 l4 12 l16 -2 l-12 12 l8 12 l-14 -6 l-6 12 l-4 -14 l-14 -4 z`, { fill: C.cream, stroke: C.cell, "stroke-width": 2 });
    o += path(`M${rx - 22} ${ry - 4} l-16 -8M${rx - 22} ${ry + 10} l-18 4M${rx - 10} ${ry - 20} l-4 -14`, { stroke: C.cream, "stroke-width": 3, "stroke-linecap": "round", fill: "none" });
    o += circ(ax, 634, 21, C.cell, { stroke: C.cream, "stroke-width": 3 }) + ic("burst", ax, 634, 28, C.teal);
  }
  o += reticle(rx, ry, firing ? 1 : stopped ? 0 : 0.62, firing);
  return o;
}
function state3(): string {
  const rx = guard.x, ry = guard.foot - 96;
  return aimBase(false) + pill(rx - 70, ry - 112, [{ i: "fire", col: C.cream }, { i: "horn" }, { t: "Fire", b: true }, { t: "down 4 s" }, { t: "T +2" }, { t: "A 2" }, { i: "mouse" }, { t: "hold" }], { keys: false }) + hud(3, true) + strip();
}
function state4(): string {
  const rx = guard.x, ry = guard.foot - 96;
  return aimBase(true) + pill(rx - 70, ry - 112, [{ i: "burst", col: C.cell }, { t: "Firing at the guard", b: true }, { t: "down in 3 s" }, { i: "mouse" }, { t: "held" }, { k: "X" }, { t: "stop" }], { keys: false }) + hud(3, true, { firing: 3, ret: 1 }) + strip();
}
function state5(): string {
  const rx = guard.x, ry = guard.foot - 96;
  return aimBase(false, true) + pill(rx - 70, ry - 112, [{ i: "kill", col: C.cream }, { t: "Stopped", b: true }, { t: "gun still drawn" }], { keys: false }) + hud(3, true, { ret: 1 }) + strip();
}

const svg = (inner: string, defs: boolean) =>
  `<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 1920 1080" width="1920" height="1080" font-family="Helvetica Neue, Arial, sans-serif">${defs ? `<defs>${SYMS}</defs>` : ""}${inner}</svg>`;
const states = [
  { id: 1, name: "Prompt", fn: state1, cap: "Facing Inez: the prompt names the act and its price in one line. E acts, so does a left click on her (setting). Hold E (the dots) opens the options." },
  { id: 2, name: "Hold E", fn: state2, cap: "Holding E 0.22 s opens the ring on her. Release over a wedge takes it. The temperament is unknown (?), so E alone would have opened this. Pressure is dashed: never lit." },
  { id: 3, name: "Aim", fn: state3, cap: "Right button held: reticle at the cursor, gun drawn. Left button fires. The guard is not hostile, so Fire is a hold (the ring fills) and the pill carries the loud mark." },
  { id: 4, name: "Firing", fn: state4, cap: "Left button held: a tracer runs from the gun to the guard, the muzzle flashes, the agent and its portrait wear the firing mark, and one line says who it fires at and how long. The Fixer (not driven) returns fire: thin tracer, return mark on her and her portrait. Release, or X, stops it." },
  { id: 5, name: "Stopped", fn: state5, cap: "Release: the tracer cuts at once, the marks go, and the line reads Stopped for 1 s. The gun stays drawn (portrait mark filled) until F or auto-holster." },
];
for (const s of states) writeFileSync(`${here}/shot-${s.id}.svg`, svg(s.fn(), true));
const html = `<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Brief 024 · action-game controls (third person)</title>
<style>body{margin:0;background:#efe7d3;color:#1d2b29;font:15px/1.5 "Helvetica Neue",Arial,sans-serif}
header{padding:14px 20px;display:flex;gap:14px;align-items:center;flex-wrap:wrap}h1{font-size:20px;margin:0}
button{font:inherit;font-weight:700;border:2px solid #0f4a45;background:#fbf7ec;color:#0f4a45;border-radius:8px;padding:4px 14px;cursor:pointer}button[aria-pressed=true]{background:#0f4a45;color:#f4ecd8}
.cap{padding:0 20px 12px;max-width:1500px}#wrap{transform-origin:0 0}@keyframes tr{to{stroke-dashoffset:-44}}.tr{animation:tr .25s linear infinite}@keyframes pl{50%{opacity:.35}}.pulse{animation:pl 1s ease-in-out infinite}svg.stage{display:none;width:1920px;height:1080px}svg.stage.on{display:block}</style></head><body>
<svg width="0" height="0" style="position:absolute"><defs>${SYMS}</defs></svg>
<header><h1>Third person: act with E, aim, fire · 1920 × 1080</h1>${states.map((s) => `<button data-s="${s.id}" aria-pressed="${s.id === 1}">${s.id} · ${s.name}</button>`).join("")}</header>
<div class="cap" id="cap"></div>
<div id="wrap">${states.map((s) => svg(s.fn(), false).replace("<svg ", `<svg class="stage${s.id === 1 ? " on" : ""}" id="st${s.id}" `)).join("")}</div>
<script>
const caps=${JSON.stringify(states.map((s) => s.cap))};
function show(n){document.querySelectorAll('svg.stage').forEach(e=>e.classList.toggle('on',e.id==='st'+n));document.querySelectorAll('button').forEach(b=>b.setAttribute('aria-pressed',b.dataset.s==n));document.getElementById('cap').textContent=caps[n-1]}
document.querySelectorAll('button').forEach(b=>b.onclick=()=>show(b.dataset.s));addEventListener('keydown',e=>{if('12345'.includes(e.key))show(+e.key)});
function fit(){const k=Math.min(1,innerWidth/1920);document.getElementById('wrap').style.transform='scale('+k+')';document.getElementById('wrap').style.height=(1080*k)+'px'}addEventListener('resize',fit);fit();show(1);
</script></body></html>`;
writeFileSync(here + "/index.html", html);
console.log("ok", html.length);
