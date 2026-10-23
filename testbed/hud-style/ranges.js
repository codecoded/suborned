// Brief 049 (visual cues): range and fire lines at play zoom over real ground. Added beside scenes.js, nothing of the lab rewritten.
// Everything here is drawn in play px (1080p, zoom 14: 38.6 px per metre; the ground plane is flattened by FLATTEN), so a ring is a world
// ring whose line widths are metres (what Latch sets on a LineRenderer), not HUD art. Colours are the game's own and all sourced:
// cell cyan #60e0ea (HostViews.CellRings), guard / police amber #f0a020 and rivals violet #8a4fd0 (style contract signal_colours), cream and ink-deep (kit.uss).
// Figures are stand-ins: cut-outs of the city guard and a patrol officer from the 0.2.56 frames (prep/ground.py), mirrored or tinted.
import { pill, groundArrow, groundTile } from "./parts.js";
import { FLATTEN } from "./tokens.js";

export const PX = 38.6, REACH_M = 15, MAX_M = 16;   // the pistol: effective ring 15 m, max ring 16 m (rulebook weapons.pistol; FireLine.Band point / eff / long / out)
const FL = FLATTEN, CELL = "#60e0ea", AMBER = "#f0a020", VIOLET = "#8a4fd0", CREAM = "#f7f4ec", INK = "#0b1615";
export const RED = "#e03a2f";                                // the cones' red (style contract signal_colours.cones); free to use, told apart from a cone by shape and motion (see TARGET_RINGS in tokens.js)
const M = (m) => m * PX;                                  // metres -> play px
const dist = (a, b) => Math.hypot((b.x - a.x) / PX, (b.y - a.y) / (PX * FL));
const figSize = { guard: { w: 62, h: 118, fx: 28, fy: 111 }, patrol: { w: 62, h: 120, fx: 31, fy: 116 } }, FK = 0.55;

// ---------------------------------------------------------------- the world rings (a group around its own feet, play px, flattened)
// state: sel | idle (the cell) · reach | long | out | hover (a target) ; col: the side colour
// o.mode (Sky, 2026-10-22: "red when they're firing at you, and with crosshairs also when involved at being aimed or shooting"):
//   null  : the plain ring (reach / long / out / hover), no ticks
//   "ours": one of the cell is aiming or firing at it. Side colour kept, four cream crosshair ticks (still)
//   "aim" : it is aiming at one of the cell (under-fire data). Red ring, four cream ticks that close in on the ring while it aims
//   "fire": it is firing at one of the cell. Red ring, ticks tight, a thin pulse leaves the ring at each shot
// o.ph: null = live (CSS loop); a number = that many ms into the loop, frozen (for boards)
export function ring(state, col, o = {}) {
  if (state === "none") return "";
  const mode = o.mode || null, hot = mode === "aim" || mode === "fire", ticked = !!mode;
  const cell = state === "sel" || state === "idle";
  const c = hot ? RED : col;
  const R = M(state === "hover" ? 0.68 : cell ? 0.42 : 0.54);
  let w = state === "sel" ? M(0.11) : state === "idle" ? M(0.045) : state === "hover" ? M(0.12) : state === "reach" ? M(0.1) : state === "long" ? M(0.075) : M(0.065);
  if (hot) w = M(0.12);                                       // a threat is a touch heavier than a plain ring
  const e = w + M(0.045);                                     // the ink-deep edge, so a ring reads on asphalt and on pale paving
  const dash = state === "out" && !hot ? `stroke-dasharray="${(2 * Math.PI * R) / 22} ${(2 * Math.PI * R) / 22}"` : "";
  const a = cell ? (state === "sel" ? 1 : 0.7) : state === "out" && !hot ? 0.9 : 1;
  const frz = o.ph == null ? "" : `animation-delay:-${o.ph}ms;animation-play-state:paused;`;
  let g = `<ellipse rx="${R}" ry="${R}" fill="none" stroke="${INK}" stroke-opacity="${state === "out" && !hot ? 0.42 : 0.5}" stroke-width="${e}" ${dash}/>`;
  g += `<ellipse rx="${R}" ry="${R}" fill="none" stroke="${c}" stroke-opacity="${a}" stroke-width="${w}" ${dash}/>`;
  if (mode === "fire") g += `<ellipse class="rpulse" rx="${R}" ry="${R}" fill="none" stroke="${RED}" stroke-width="${M(0.07)}" style="${frz}"/>`;
  if (ticked) {                                               // the crosshair band ticks: the shape that says "an aimed shot is in this exchange"
    const t0 = R + M(0.07), t1 = R + M(0.22), tc = CREAM;
    [[1, 0], [0, 1], [-1, 0], [0, -1]].forEach(([x, y]) => {
      const cls = mode === "aim" ? "rtk rtk-aim" : mode === "fire" ? "rtk rtk-fire" : "";
      const v = `--tx:${x * M(0.3)}px;--ty:${y * M(0.3) * FL}px;--kx:${-x * M(0.07)}px;--ky:${-y * M(0.07) * FL}px;`;
      g += `<g class="${cls}" style="${v}${frz}"><line x1="${x * t0}" y1="${y * t0}" x2="${x * t1}" y2="${y * t1}" stroke="${INK}" stroke-opacity=".6" stroke-width="${M(0.13)}" stroke-linecap="round"/><line x1="${x * t0}" y1="${y * t0}" x2="${x * t1}" y2="${y * t1}" stroke="${tc}" stroke-width="${M(0.075)}" stroke-linecap="round"/></g>`;
    });
  }
  return `<g transform="scale(1 ${FL})">${g}</g>`;
}
const at = (html, x, y) => `<g transform="translate(${x} ${y})">${html}</g>`;
const shadow = (x, y) => `<ellipse cx="${x}" cy="${y}" rx="15" ry="5.5" fill="#1a1410" fill-opacity=".26"/>`;

// ---------------------------------------------------------------- the street: who stands where (play px on the 1080p frame; the same for every picture)
const CAST = {
  agents: [{ id: "enforcer", x: 1310, y: 790, fig: "guard", sel: true, hue: "saturate(.12) brightness(.6) contrast(1.15)" }, { id: "fixer", x: 1190, y: 850, fig: "guard", flip: true, sel: true, hue: "saturate(.12) brightness(.72) contrast(1.15)" }],
  targets: [
    { id: "t1", label: "an officer", x: 1405, y: 548, fig: "patrol", side: AMBER, hover: true, mode: "ours" },
    { id: "t2", label: "the guard", x: 1040, y: 650, fig: "guard", flip: true, side: AMBER, mode: "aim", at: "fixer" },
    { id: "t3", label: "an officer", x: 1565, y: 262, fig: "patrol", flip: true, side: AMBER },
    { id: "t5", label: "an officer", x: 1600, y: 600, fig: "patrol", flip: true, side: AMBER, mode: "fire", at: "enforcer" },
    { id: "t4", label: "a rival", x: 1340, y: 318, fig: "guard", side: VIOLET, hue: "saturate(.7) brightness(.55)" },
  ],
  bystanders: [{ id: "b1", label: "a passer-by", x: 1475, y: 705, fig: "guard", hue: "hue-rotate(200deg) saturate(1.6) brightness(1.1)" }],
};
const shooters = () => CAST.agents.filter((a) => a.sel);
const reachOf = (t) => shooters().filter((a) => dist(a, t) <= REACH_M);
const fig = (f, x, y, extra = "") => { const s = figSize[f.fig], k = FK; return `<img src="${f.fig === "patrol" ? "/bg/figure-patrol.png" : "/bg/ground-agent.png"}" style="position:absolute;left:${x - s.fx * k}px;top:${y - s.fy * k}px;width:${s.w * k}px;height:${s.h * k}px;${f.flip ? "transform:scaleX(-1);" : ""}${f.hue ? `filter:${f.hue};` : ""}${extra}">`; };
const head = (f) => f.y - figSize[f.fig].fy * FK + 2;
const layer = (z, html) => `<div class="rzl" style="z-index:${z}">${html}</div>`;
const svg = (inner) => `<svg class="rsvg" width="1920" height="1080" viewBox="0 0 1920 1080">${inner}</svg>`;
const everyone = () => [...CAST.agents, ...CAST.targets, ...CAST.bystanders];
const figures = () => layer(2, everyone().map((f) => fig(f, f.x, f.y)).join(""));
const shadows = () => layer(1, svg(everyone().map((f) => shadow(f.x + 3, f.y + 1)).join("")));
const hostilePill = (tk, t) => { const p = { text: "Right hold · Fire at " + t.label + " · +2/hit Trail · Alert ≥2", kind: "harm", icon: "fire", faces: ["enforcer", "fixer"], st: "fire" }; return `<div class="at" style="left:${t.x}px;top:${head(t) - 12}px;z-index:6">${pill(p, tk)}</div>`; };

// ---------------------------------------------------------------- BEFORE: what 0.2.65 draws (the circle in the Handover; the lines and labels are the fight range's panel, shown here as Sky saw them)
function labelText(a, t, bystander) {
  const d = Math.round(dist(a, t));
  if (d > REACH_M && d <= MAX_M) return `30% · ${d} m long shot · hits in 2 shots · new target: aims first`;
  if (d > REACH_M) return `out of range · ${d} m · ${t.id === "t3" ? "half cover · " : ""}holstered · untrained`;
  return `75% · ${d} m${bystander ? " · bystander" : ""} · hits next shot · new target: aims first${bystander ? " · target running" : ""}`;
}
function before(tk) {
  const a = CAST.agents[0], rows = [];
  const targets = [...CAST.targets, ...CAST.bystanders];
  let lines = "", labels = "";
  const placed = [];
  targets.forEach((t) => {
    const out = dist(a, t) > REACH_M, col = CELL;
    const A = { x: a.x, y: a.y - 48 }, B = { x: t.x, y: t.y - 40 };
    lines += `<line x1="${A.x}" y1="${A.y}" x2="${B.x}" y2="${B.y}" stroke="${col}" stroke-opacity="${out ? 0.4 : 0.95}" stroke-width="${out ? 1.2 : 2.4}"/>`;
    const txt = labelText(a, t, CAST.bystanders.includes(t)), w = txt.length * 6.1 + 10;
    let lx = A.x + (B.x - A.x) * 0.38 - w / 2, ly = A.y + (B.y - A.y) * 0.38 - 10;
    for (let s = 0; s < 8; s++) { const hit = placed.some((p) => lx < p.x + p.w && lx + w > p.x && ly < p.y + 19 && ly + 19 > p.y); if (!hit) break; ly += s % 2 ? -22 * (s + 1) / 2 : 22 * (s + 2) / 2; }
    placed.push({ x: lx, y: ly, w });
    labels += `<div class="oldlbl" style="left:${lx}px;top:${ly}px">${txt}</div>`;
  });
  const circ = CAST.agents.filter((x) => x.sel).map((x) => `<ellipse cx="${x.x}" cy="${x.y}" rx="${M(REACH_M)}" ry="${M(REACH_M) * FL}" fill="none" stroke="${CELL}" stroke-width="2"/>`).join("");
  const cellRings = CAST.agents.map((x) => at(ring("sel", CELL), x.x, x.y)).join("");
  return layer(1, svg(circ + shadows0() + cellRings)) + figures() + layer(3, svg(lines)) + layer(5, labels) + hostilePill(tk, CAST.targets[0]);
}
const shadows0 = () => everyone().map((f) => shadow(f.x + 3, f.y + 1)).join("");

// ---------------------------------------------------------------- AFTER: the ring on each one a selected, drawn agent could fire on; no circle, no pre-fire lines, no words
// A (the pick): plain rings have no ticks. Ticks mean "in a shot exchange": the cell is aiming or firing at it (cream ticks, its side colour kept),
// or it is aiming / firing at the cell (red ring, cream ticks). o.arc: the under-fire arc on the agent that is shot at (brief 046); o.ph: freeze the loop at that ms.
const thetaOf = (a, t) => (Math.atan2(-(t.y - a.y) / (PX * FL), (t.x - a.x) / PX) * 180) / Math.PI;
function after(tk, o = {}) {
  const rings = [], arcs = [], tk2 = o.arcW ? { ...tk, why: { ...tk.why, arrow: { ...tk.why.arrow, weight: o.arcW } } } : tk;
  const by = (id) => CAST.agents.find((x) => x.id === id);
  CAST.agents.forEach((x) => rings.push(at(ring(x.sel ? "sel" : "idle", CELL), x.x, x.y)));
  CAST.targets.forEach((t) => {
    const n = reachOf(t).length, long = !n && shooters().some((a) => dist(a, t) <= MAX_M), state = t.hover && n ? "hover" : n ? "reach" : long ? "long" : "out";
    const ag = t.at ? by(t.at) : by("enforcer");
    rings.push(at(ring(state, t.side, { mode: t.mode, ph: o.ph }), t.x, t.y));
    if (o.arc && t.at) arcs.push(groundArrow(tk.why.arrow.style, thetaOf(ag, t), M(0.42), tk2, ag.x, ag.y));
  });
  // a civilian wears no ring until the cursor is on them (not in this frame)
  const e = CAST.agents[0], t1 = CAST.targets[0], t5 = CAST.targets.find((t) => t.id === "t5");
  const A = { x: e.x + 9, y: e.y - 50 }, B = { x: t1.x, y: t1.y - 40 };
  const L = (c, w, op, d) => `<line x1="${A.x}" y1="${A.y}" x2="${B.x}" y2="${B.y}" stroke="${c}" stroke-opacity="${op}" stroke-width="${w}" ${d ? `stroke-dasharray="${d}"` : ""} stroke-linecap="butt"/>`;
  const fire = L(INK, 3.4, 0.35) + `<line class="march" x1="${A.x}" y1="${A.y}" x2="${B.x}" y2="${B.y}" stroke="${CREAM}" stroke-opacity=".9" stroke-width="1.5" stroke-dasharray="9 5"/>`;
  // hostile fire keeps its short solid streak (one 34 px dash, 60 % of the way from the shooter)
  const H0 = { x: t5.x, y: t5.y - 38 }, H1 = { x: e.x, y: e.y - 50 }, u = Math.hypot(H1.x - H0.x, H1.y - H0.y), ux = (H1.x - H0.x) / u, uy = (H1.y - H0.y) / u, f = 0.6 * u;
  const sx = H0.x + ux * f, sy = H0.y + uy * f;
  const streak = `<line x1="${sx}" y1="${sy}" x2="${sx + ux * 34}" y2="${sy + uy * 34}" stroke="${INK}" stroke-opacity=".5" stroke-width="3.6" stroke-linecap="round"/><line x1="${sx}" y1="${sy}" x2="${sx + ux * 34}" y2="${sy + uy * 34}" stroke="${CREAM}" stroke-width="1.6" stroke-linecap="round"/>`;
  return layer(1, svg(shadows0() + rings.join("")) + arcs.join("")) + figures() + layer(3, svg(fire + streak)) + hostilePill(tk, t1);
}

export function rangeItems(kind, tk, o = {}) { return kind === "before" ? before(tk) : after(tk, { arc: kind === "a" || kind === "arc", ...o }); }

// ---------------------------------------------------------------- the ring states on their own: real ground, 2.6x, the figure over the ring
const TILE = { w: 300, h: 290, feet: [150, 180], agent: { x: 122, y: 69, w: 62, h: 118 } };
// cone: the game's own look (HostViews: flat fill at 38 %, crisp rim 0.085 m); only for the "told apart" tile
const coneWedge = (col) => { const L = M(3.6), half = (28 * Math.PI) / 180, a0 = -half - 0.5, a1 = half - 0.5, p = (a) => `${(L * Math.cos(a)).toFixed(1)},${(L * Math.sin(a)).toFixed(1)}`;
  return `<g transform="scale(1 ${FL})"><path d="M0,0 L${p(a0)} A${L},${L} 0 0 1 ${p(a1)} Z" fill="${col}" fill-opacity=".38" stroke="${col}" stroke-width="${M(0.085)}" stroke-linejoin="round"/></g>`; };
function stateTile(ground, state, side, label, hue = "", o = {}, z = 1.2, extra = "") {
  const K = 0.55, s = K * z, [fx, fy] = TILE.feet;
  const g = ring(state, side, o);
  return `<div class="stile"><div class="gtile" style="width:${TILE.w * s}px;height:${TILE.h * s}px"><div class="gscale" style="width:${TILE.w}px;height:${TILE.h}px;transform:scale(${s})">
    <img class="gbg" src="/bg/ground-${ground}.jpg" width="${TILE.w}" height="${TILE.h}">
    <svg style="position:absolute;left:0;top:0;z-index:1" width="${TILE.w}" height="${TILE.h}"><g transform="translate(${fx} ${fy}) scale(${1 / K})">${extra}${g}</g></svg>
    <img class="gagent" src="/bg/ground-agent.png" style="left:${TILE.agent.x}px;top:${TILE.agent.y}px;width:${TILE.agent.w}px;height:${TILE.agent.h}px;${hue ? `filter:${hue}` : ""}">
    </div></div><b class="rcap2">${label}</b></div>`;
}
const HUE = { [VIOLET]: "saturate(.7) brightness(.55)", [CREAM]: "hue-rotate(200deg) saturate(1.6) brightness(1.1)", [CELL]: "saturate(.12) brightness(.62) contrast(1.15)", [AMBER]: "", [RED]: "" };
const cap = (t, x, y) => `<span class="cap" style="left:${x}px;top:${y}px">${t}</span>`;
// 1) the plain rings and the ones the cell is aiming at (shape says reach and aim; colour says whose)
export function rangeStates() {
  const row = (ground, y) => {
    const cells = [
      ["sel", CELL, "Cell, selected"], ["idle", CELL, "Cell, not selected"],
      ["reach", AMBER, "In reach"], ["long", AMBER, "Long (can hit, poorly)"], ["out", AMBER, "Out of reach"], ["hover", AMBER, "Cursor on it"],
      ["reach", AMBER, "We aim: ticks", { mode: "ours" }], ["reach", VIOLET, "Rival, plain"], ["reach", VIOLET, "Rival, we aim", { mode: "ours" }], ["hover", CREAM, "Passer-by, cursor on"],
    ];
    return `<div class="srow" style="top:${y}px;gap:4px">${cells.map(([st, c, l, o]) => stateTile(ground, st, c, l, HUE[c], o || {}, 1.12)).join("")}</div>`;
  };
  return cap("Plain rings carry no ticks. The four cream ticks (the crosshair band) appear only when the cell is aiming or firing at it; the ring keeps its side colour. Pavement (top), asphalt (bottom), 1.12x.", 40, 30) + row("pave", 70) + row("asph", 400);
}
// 2) the red ones: it is aiming / firing at the cell. Frozen frames of the loop, and a cone beside to show they are not the same thing
export function rangeRed() {
  const P = (ground, y) => {
    const cells = [
      ["reach", AMBER, "Officer aims at us, 0 ms", { mode: "aim", ph: 0 }, ""],
      ["reach", AMBER, "400 ms: ticks closing", { mode: "aim", ph: 400 }, ""],
      ["reach", AMBER, "900 ms: locked", { mode: "aim", ph: 900 }, ""],
      ["reach", AMBER, "Fires: ticks kick in, pulse out (0 ms)", { mode: "fire", ph: 20 }, ""],
      ["reach", AMBER, "Fires, 150 ms", { mode: "fire", ph: 150 }, ""],
      ["reach", VIOLET, "Rival aims at us", { mode: "aim", ph: 900 }, ""],
      ["out", AMBER, "Out of reach but aiming at us", { mode: "aim", ph: 900 }, ""],
      ["none", AMBER, "A cone, for comparison (unlit fill + rim, no ring, no ticks)", {}, coneWedge(RED)],
    ];
    return `<div class="srow" style="top:${y}px;gap:6px">${cells.map(([st, c, l, o, x]) => stateTile(ground, st, c, l, HUE[c], x ? {} : o, 1.2, x)).join("")}</div>`;
  };
  return cap("It is aiming or firing at the cell: the ring turns red (the cones' red), the cream ticks close in on it while it aims; at each shot they kick in and one thin ring leaves. A cone is a flat wedge, 38 % fill, no ring.", 40, 30) + P("pave", 70) + P("asph", 400);
}

// 3) the arc on the agent at the cell's real ring (0.42 m = 16 px at play zoom): is it still a cue, now that the shooter wears a red ring?
export function rangeArc(tk) {
  const z = 2.4, tw = 300 * 0.55 * z, gap = 24, x0 = (1920 - (4 * tw + 3 * gap)) / 2;
  const T = (g, w, th) => groundTile({ ...tk, why: { ...tk.why, arrow: { ...tk.why.arrow, weight: w } } }, { ground: g, theta: th, ring: "selected", R: M(0.42), z, cause: null, occ: true, cmp: `arc-${g}-${w}-${th}` });
  const cells = [["pave", 1, 40, "046 weight 1.0 (1.6 px), pavement"], ["pave", 2, 40, "weight 2.0 (3 px)"], ["asph", 1, 40, "weight 1.0, asphalt"], ["asph", 2, 40, "weight 2.0"]];
  return cap("The under-fire arc on a cell agent at its real ring (r 0.42 m = 16 px at play zoom), 2.4x. Shooter up and to the right.", 40, 120) +
    cells.map(([g, w, th, l], i) => { const x = x0 + i * (tw + gap); return cap(l, x, 190) + `<div class="at tl" style="left:${x}px;top:212px">${T(g, w, th)}</div>`; }).join("");
}
