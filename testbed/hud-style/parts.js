// Every HUD component as the game draws it (briefs 011 v10 signage, 044 revised A). Pure HTML builders; the look comes from CSS variables
// that tokens.js sets (--main, --live, --disc ... and the sprite urls --spr-*), so a token change restyles without rebuilding.
// Sizes are px at the 1920 x 1080 reference. Words are exactly the game's.
import { ARROW_SPEC as AS, FLATTEN, COL, TALL, tallFor, divGeom, GAP_TOP, DIVIDERS } from "./tokens.js";
const K = "/kit/";
const SL = { pill_status: [93, 0, 93, 0], panel_dark: [35, 35, 35, 35], plate_dark: [24, 24, 24, 24], panel_strip: [25, 25, 25, 25], panel_slot: [26, 26, 26, 26] };
export const nine = (set, n, s, extra = "") => {
  const [l, t, r, b] = SL[n];
  return `<div class="fr" style="border-width:${t * s}px ${r * s}px ${b * s}px ${l * s}px;border-image-source:var(--spr-${set}-${n});border-image-slice:${t} ${r} ${b} ${l} fill;border-image-width:${t * s}px ${r * s}px ${b * s}px ${l * s}px;border-image-repeat:stretch;${extra}"></div>`;
};
export const ic = (name, px, col = "var(--main)", extra = "") => { const u = typeof px === "number" ? px + "px" : px; return `<i class="ic" style="width:${u};height:${u};background:${col};-webkit-mask-image:url(${K}icons/singles/${name}@2x.png);${extra}"></i>`; };
export const cameo = (role, px = "var(--face)", extra = "") => `<span class="cam" style="width:${px}${typeof px === "number" ? "px" : ""};height:${px}${typeof px === "number" ? "px" : ""};background-image:url(${K}portraits/cameo_${role}_normal@2x.png);${extra}"></span>`;
const tile = (w, h) => `<span class="stripes" style="width:${w}px;height:${h}px"></span>`;
const disc = (icon, light, ring = "") => `<span class="disc${light ? " light" : ""}"><i class="dring"></i>${ic(icon, "var(--disc-icon-size)", light ? "var(--ink-icon)" : "var(--disc-icon)")}</span>`;
const mark = (m, px = 19) => `<img class="mk" src="${K}badges/${m === "ok" ? "mark_ok" : m === "soft" ? "mark_half" : "mark_no"}@2x.png" style="width:${px}px;height:${px}px">`;

// ---------------------------------------------------------------- the verb pill (offer, hold, ready, order, fire)
// p: { id, head, text, kind, icon, faces[], st: offer|hold|ready|order|fire, p (progress 0..1), seen, live }
export function pill(p, tk) {
  const fire = p.st === "fire" || p.kind === "harm";
  const frame = fire ? nine("ov", "panel_strip", 0.3) : nine("ov", "pill_status", 38 / 88);
  const txt = p.head ? `<span class="phead">${p.head}</span><span class="pdot">·</span>${p.text}` : p.text;
  const faces = (p.faces || []).length ? `<span class="faces">${p.faces.map((f) => cameo(f)).join("")}</span>` : "";
  const seen = p.seen ? `<span class="seen">${tile(7, 22)}${ic("sawyou", 13)}Watched spot</span>` : "";
  const N = tk.hold.segments, ticks = Array.from({ length: N }, (_, i) => `<i data-i="${i}"></i>`).join("");
  const hold = fire ? "" : `<span class="hl"><i class="hl-fill"></i><span class="hl-ticks">${ticks}</span><i class="hl-line"></i></span>`;
  return `<div class="pill${fire ? " fire" : ""}" data-cmp="${p.cmp || "pill"}" data-state="${p.st}" data-kind="${p.kind}" ${p.live ? 'data-live="1"' : ""} data-head="${p.head || ""}" style="--p:${p.p ?? (p.st === "ready" ? 1 : 0)}">
    ${frame}<i class="pglow"></i>${hold}<svg class="rim"><rect pathLength="1"/></svg><i class="pflash"></i>
    <span class="pin">${fire ? tile(10, 24) : ""}${disc(p.icon, fire)}<span class="ptxt">${txt}</span>${faces}${seen}${fire ? tile(10, 24) : ""}</span>
    <span class="tail"><i></i><i></i></span></div>`;
}

// ---------------------------------------------------------------- done chip (facts + unlock pills)
export function chip(c) {
  const lines = c.lines.map(([i, w]) => `<div class="cl">${ic(i, 16)}<span>${w}</span></div>`).join("");
  const unl = c.unl?.length ? `<div class="unl">${ic("chev_up", 12, "var(--live-ic)", "transform:rotate(90deg)")}${c.unl.map((u) => `<span class="uplate${u.shut ? " shut" : ""}">${nine("ov", "plate_dark", 0.37)}<span>${ic(u.icon, 12)}${u.word}</span></span>`).join("")}</div>` : "";
  return `<div class="chip" data-cmp="${c.cmp || "chip"}" data-kind="${c.kind || "look"}">${nine("ov", "panel_dark", 0.5)}<span class="cface">${cameo(c.face, 32)}${mark(c.mark)}</span><span class="clines">${lines}${unl}</span></div>`;
}
// window marker (the counter under a chip) and its draining underline
export function marker(m) {
  const inner = m.face ? cameo(m.face, 18) : `<span class="num">${m.num}</span>`;
  const line = m.left != null && m.total ? `<span class="wline"><i style="width:${(m.left / m.total) * 100}%"></i></span>` : "";
  return `<div class="marker${m.low ? " low" : ""}" data-cmp="${m.cmp || "marker"}">${nine("ov", "plate_dark", 26 / 60)}<span class="mi">${ic(m.icon, 15)}${inner}</span>${line}</div>`;
}
export const tag = (t) => `<div class="ctag" data-cmp="tag"><span class="stripes" style="width:9px;height:26px"></span><span class="ti">${ic("close_x", 12)}${t}</span></div>`;
export const pop = (p) => `<div class="pop" data-cmp="pop">${nine("ov", "plate_dark", 26 / 60)}<span class="pi">${ic(p.icon, 14)}${p.text}</span></div>`;
// the rings on the ground and the activity ring on a badge (kit overlays)
export function rings(prog = 0.6) {
  return `<div class="rings" data-cmp="ring" style="--p:${prog}">
    <span class="rg"><img src="${K}overlays/ring_ground_selected@2x.png" width="60" height="36"><b>selected</b></span>
    <span class="rg"><img src="${K}overlays/ring_ground_hint@2x.png" width="60" height="36"><b>hint</b></span>
    <span class="rg act"><span class="badge30"><i></i>${ic("s_watching", 17, "var(--disc-icon)")}</span><img class="rtrack" src="${K}overlays/ring_track@2x.png"><span class="rfill"></span><b>activity</b></span>
    <span class="rg"><img src="${K}overlays/crosshair@2x.png" width="40" height="40"><b>aim</b></span></div>`;
}

// ---------------------------------------------------------------- "why am I under fire": one icon for the strongest cause
export const whyDisc = (cause, tk, extra = "") => `<span class="why${tk.why.disc === "light" ? " light" : ""}" data-cause="${cause}" style="${extra}">${ic(tk.why.icons[cause], "calc(var(--why) * var(--why-ratio))", tk.why.disc === "light" ? "var(--ink-icon)" : "var(--disc-icon)")}</span>`;
// over the agent: the agent's number plate and the cause disc (at most two icons over a person)
export const whyHead = (cause, tk, n = 1) => `<div class="whyhead" data-cmp="why-head">${tk.roster.layout === "railLite" ? "" : `<span class="numplate" style="background-image:url(${K}markers/plate_light@2x.png)">${n}</span>`}${whyDisc(cause, tk)}</div>`;
// ---------------------------------------------------------------- the ground cue toward the shooter (brief 046): a decal in the ground plane, never a billboard
// theta = ground-plane degrees toward the shooter (0 = screen right, 90 = away from the camera). Authored in ring units (ring radius 100) and drawn
// flattened by FLATTEN; thickness and size scale with the ring because it is a world decal. The scene hides it behind whatever stands in front of it.
let gid = 0;
const P2 = (r, deg) => [r * Math.cos((deg * Math.PI) / 180), r * Math.sin((deg * Math.PI) / 180)];
const arcD = (r, span) => { const [x1, y1] = P2(r, -span / 2), [x2, y2] = P2(r, span / 2); return `M${x1.toFixed(2)} ${y1.toFixed(2)}A${r} ${r} 0 0 1 ${x2.toFixed(2)} ${y2.toFixed(2)}`; };
export function groundArrow(style, theta, R, tk, cx = 0, cy = 0) {
  if (!style || style === "none") return "";
  const A = tk.why.arrow, w = A.weight ?? 1, ms = tk.why.pulseMs, id = "g" + ++gid;
  const vars = `--core:${A.color};--edge:${A.edge};--ga:${A.alpha};--pms:${ms}ms;left:${cx}px;top:${cy}px;width:${R * 3.6}px;height:${R * 3.6}px;margin:${-R * 1.8}px 0 0 ${-R * 1.8}px`;
  let g = "";
  if (style === "arc") { const S = AS.arc, d = arcD(S.r, S.spanDeg);
    g = `<g class="an a-arc" style="--lo:${S.alphaLow};--hi:${S.alphaHigh}"><path class="e" d="${d}" style="stroke-width:${S.edgeWidth * w};opacity:${S.edgeAlpha}"/><path class="c" d="${d}" style="stroke-width:${S.width * w}"/></g>`; }
  else if (style === "notch") { const S = AS.notch, pts = `${S.rBase},${-S.halfBase * w} ${S.rTip},0 ${S.rBase},${S.halfBase * w}`;
    g = `<g class="an a-notch" style="--lo:${S.alphaLow};--hi:${S.alphaHigh};--dx:${S.drift}px"><polygon class="e" points="${pts}" style="stroke-width:${S.edgeWidth}"/><polygon class="c" points="${pts}"/></g>`; }
  else if (style === "streak") { const S = AS.streak, wd = S.width * w, ew = S.edgeWidth * w;
    const grad = (n, col) => `<linearGradient id="${id}${n}"><stop offset="0" style="stop-color:var(${col});stop-opacity:0"/><stop offset="1" style="stop-color:var(${col});stop-opacity:1"/></linearGradient>`;
    g = `<defs>${grad("e", "--edge")}${grad("c", "--core")}</defs><g class="an a-streak" style="--dx:${S.rFrom - S.rTo}px;--sa:${S.alpha};--cyc:${ms + S.gapMs}ms"><rect x="${S.rFrom}" y="${-ew / 2}" width="${S.length}" height="${ew}" rx="${ew / 2}" fill="url(#${id}e)" style="opacity:${S.edgeAlpha}"/><rect x="${S.rFrom}" y="${-wd / 2}" width="${S.length}" height="${wd}" rx="${wd / 2}" fill="url(#${id}c)"/></g>`; }
  else if (style === "ripple") { const S = AS.ripple, d = arcD(S.r0, S.spanDeg);
    g = `<g class="an a-ripple" style="--sc:${S.r1 / S.r0};--rms:${S.ms}ms;--cyc:${S.ms + S.gapMs}ms"><path class="e" d="${d}" style="stroke-width:${S.edgeWidth * w};opacity:${S.edgeAlpha}"/><path class="c" d="${d}" style="stroke-width:${S.width * w}"/></g>`; }
  else if (style === "asis") { // 0.2.56: three stacked cream chevrons, 24 x 30 px at the build's ring, flattened
    const poly = "0,0 50,0 100,50 50,100 0,100 50,50";
    g = [0, 1, 2].map((i) => `<g transform="translate(${118 + i * 36},-30) scale(.5,.6)"><polygon class="a-asis" style="--i:${i}" points="${poly}" fill="var(--core)" stroke="var(--edge)" stroke-width="7" stroke-linejoin="round"/></g>`).join(""); }
  return `<div class="gdecal" data-cmp="why-arrow" data-style="${style}" data-theta="${theta}" style="${vars}"><svg viewBox="-180 -180 360 360" width="${R * 3.6}" height="${R * 3.6}"><g transform="scale(1 ${FLATTEN})"><g transform="rotate(${-theta})">${g}</g></g></svg></div>`;
}
// the agent's ground ring (kit sprite, ring_ground_selected / hint), flattened like the decal
export const groundRing = (kind, R, cx, cy) => `<img class="grnd" data-cmp="ground-ring" src="${K}overlays/ring_ground_${kind}@2x.png" style="left:${cx - R}px;top:${cy - R * FLATTEN}px;width:${R * 2}px;height:${R * 2 * FLATTEN}px">`;

// ---------------------------------------------------------------- ground tiles: a real play-zoom ground with the agent cut out, so ground markers can sit UNDER him
// Backdrops are 300 x 290 native px windows (prep/ground.py); K = 0.55 puts the agent at about 65 px (play zoom). Everything inside is authored in native px.
export const GT = { w: 300, h: 290, K: 0.55, feet: [150, 180], agent: { x: 120, y: 68, w: 62, h: 118 } };
// o: { ground: pave|asph, theta, style, ring: selected|hint|none, occ, z (zoom on top of K), cue: head|arc|portrait, cause, label }
export function groundTile(tk, o) {
  const z = o.z ?? 1, s = GT.K * z, [fx, fy] = GT.feet, R = (o.R ?? 19) / GT.K; // R: ring radius in play px (19 = a ring about 38 px wide next to a 65 px person)
  const style = o.style ?? tk.why.arrow.style, cue = o.cue ?? tk.why.arrow.cue, theta = o.theta ?? 40;
  const ring = o.ring === "none" ? "" : groundRing(o.ring || "selected", R, fx, fy);
  const decal = groundArrow(style, theta, R, tk, fx, fy);
  // the cause disc (a HUD element, not a decal): above the head, or riding on the arc
  const dsz = (cue === "arc" ? 20 : tk.why.size) / GT.K;
  let disc = "";
  if (o.cause && tk.why.place !== "none" && tk.why.place !== "portrait") {
    let dx = fx, dy = GT.agent.y - dsz / 2 - 6 / GT.K;
    if (cue === "arc") { const r = R * 1.13 + dsz / 2 + 3 / GT.K; dx = fx + r * Math.cos((theta * Math.PI) / 180); dy = fy - r * Math.sin((theta * Math.PI) / 180) * FLATTEN; }
    if (cue !== "portrait") disc = `<div class="gdisc" data-cmp="why-disc" style="left:${dx - dsz / 2}px;top:${dy - dsz / 2}px;width:${dsz}px;height:${dsz}px;--why:${dsz}px">${whyDisc(o.cause, tk, "")}</div>`;
  }
  const agent = `<img class="gagent${o.occ === false ? " under" : ""}" src="/bg/ground-agent.png" style="left:${GT.agent.x}px;top:${GT.agent.y}px;width:${GT.agent.w}px;height:${GT.agent.h}px">`;
  return `<div class="gtile" data-cmp="${o.cmp || "ground-tile"}" data-ground="${o.ground}" data-occ="${o.occ === false ? 0 : 1}" style="width:${GT.w * s}px;height:${GT.h * s}px"><div class="gscale" style="width:${GT.w}px;height:${GT.h}px;transform:scale(${s})"><img class="gbg" src="/bg/ground-${o.ground === "asph" ? "asph" : "pave"}.jpg" width="${GT.w}" height="${GT.h}">${ring}${decal}${agent}${disc}</div></div>`;
}
export function whyBoard(tk) {
  return `<div class="whyrow" data-cmp="why-causes">${Object.keys(tk.why.icons).map((c) => `<span class="wc">${whyDisc(c, tk)}<b>${c}</b></span>`).join("")}</div>`;
}

// ---------------------------------------------------------------- the HUD proper
const ROLES = ["scout", "fixer", "tech", "enforcer"];
// ---------------------------------------------------------------- the roster: a fixed badge-slot layout (brief 046 section "Roster slots")
// The column sprite is drawn at 0.79 of its 109 x 416 art: ring centres x 54.4, y 65.6 / 159.5 / 254.9 / 349.0 (nat), face window 66 (r 33),
// ivory mat to r 37.5, chrome rim to r 46 (rings are 2.3 nat px apart: there is NO free margin inside the column, so a badge is either on the face,
// on the rim, or outside the column). Everything below is px at 1080p (nat x 0.79).
export const RS = { s: 0.79, W: 86.11, H: 328.64, XC: 42.98, YC: [51.82, 126.0, 201.37, 275.71], WIN: 52.14, MAT: 29.6, RING: 36.34, PITCH: 74.5 };
// the taller column (brief 046 round 3): the kit sprite with a repeated body row in each gap, so a row of icons fits under each portrait (see COL in tokens.js)
export const delta = (lay, dv) => tallFor(lay, dv)?.d ?? 0;
export const colH = (lay, dv) => RS.H + COL.cuts.length * delta(lay, dv);
export const ringY = (lay, k, dv) => RS.YC[k] + k * delta(lay, dv);
export const tabFor = (lay, dv) => lay === "under" ? { dx: RS.XC - 33.5, dy: divGeom(dv).rowDy, w: 67, h: 16 } : SLOTS[lay]?.tab;
export const BAND = 36.05; // ring centre to the cut row (the middle of the gap, from the sprite's own pixels: ring 1 bottom 34.3, ring 2 top 36.7); the row centre is BAND + d / 2
export const RING_TOP = 36.7;
// slot rects (the under and hang rows: weapon 16 / 15 px discs, status the same, four 5 px pips on a 28 px track), relative to their anchor: rail tab origin (rail), the row (under) or the plate (hang), or the face window's top-left (inside)
export const SLOTS = {
  rail: { tab: { dx: RS.W - 2, dy: -33, w: 54, h: 66 }, num: [4, 7, 24, 16], status: [30, 5, 20, 20], weapon: [6, 29, 20, 20], alert: [30, 29, 20, 20], dots: [8, 53, 38, 8] },
  // no numbers (Sky 2026-10-22): three slots, narrower tab; the alert (under fire) takes the status slot, hurt shows on the portrait art
  railLite: { tab: { dx: RS.W - 2, dy: -33, w: 44, h: 66 }, status: [12, 6, 20, 20], weapon: [12, 29, 20, 20], dots: [4, 53, 36, 8] },
  // icons under the portrait (Sky 2026-10-22: "add icons under the character"): one slim row, weapon / status / dots, centred under the ring, in the gap the taller column makes.
  // tab = the row's anchor (no frame): 67 x 16 px, x from the column's centre, y centred on the gap
  under: { tab: { dx: RS.XC - 33.5, dy: divGeom("none").rowDy, w: 67, h: 16 }, // dy shown for the "none" divider; the others move it (tabFor)
    weapon: [0, 0, 16, 16], status: [19, 0, 16, 16], dots: [39, 4, 28, 8] },
  // the same row on a small plate (kit plate_dark) hanging just under the ring and tucked over the gap's edges: 72 x 22
  hang: { tab: { dx: RS.XC - 36, dy: BAND + TALL.hang.d / 2 - 11, w: 72, h: 22 }, weapon: [3.5, 3.5, 15, 15], status: [21.5, 3.5, 15, 15], dots: [40, 7, 28, 8] },
  inside: { num: [3, 3, 22, 15], status: [32, 3, 17, 17], weapon: [2, 24, 17, 17], alert: [33, 24, 17, 17], dots: [13, 42, 26, 6] },
};
export const isTall = (lay) => lay === "under" || lay === "hang";
// what a layout costs on screen at 1080p: the column's height, the whole dock's footprint (column plus rail tabs), as shares of the 1920 x 1080 screen; the status strip's x
export function rosterCost(lay, dv) {
  const H = colH(lay, dv), t = SLOTS[lay]?.tab, rail = lay === "rail" || lay === "railLite";
  const w = rail ? t.dx + t.w : RS.W, area = RS.W * H + (rail ? 4 * (t.w - 2) * t.h : 0);
  return { layout: lay, columnH: +H.toFixed(1), footprintW: +w.toFixed(1), area: Math.round(area), pctHeight: +((H / 1080) * 100).toFixed(1), pctArea: +((area / (1920 * 1080)) * 100).toFixed(2), pitch: +(RS.PITCH + delta(lay, dv)).toFixed(1), statusX: lay === "rail" ? 162 : lay === "railLite" ? 152 : 110, dockBottom: +(15 + H).toFixed(1) };
}
export const DEFAULT_STATES = [
  { pips: 3, weapon: "holstered" },
  { sel: true, busy: "s_watching", weapon: "holstered", pips: 4 },
  { pips: 3, weapon: "drawn", watched: true },
  { pips: 2, weapon: "holstered", seen: true },
];
// ONE weapon pictogram (the kit's `fire` pistol) in ONE slot, two states: drawn = light disc, ink gun; holstered = dark disc, the same gun at 55 %. `cause` = the disc carries a "drawn" cause: the kit's selected light disc.
const disc20 = (icon, light, sz, dim, cls = "") => `<span class="rd${light ? " light" : ""}${cls}" style="width:${sz}px;height:${sz}px">${ic(icon, Math.round(sz * 0.6) + "px", light ? "var(--ink-icon)" : "var(--disc-icon)", dim ? "opacity:.55" : "")}</span>`;
const weaponDisc = (state, sz, cause) => state === "drawn" ? (cause ? disc20("fire", true, sz, false, ` sel why-d" data-cause="drawn`) : disc20("fire", true, sz)) : state === "holstered" ? disc20("fire", false, sz, true) : "";
// the divider mark under portrait i (not under the last one): a kit-sourced element in the gap, centred on the column, at the geometry divGeom() gives
export function dividerHTML(dv, i, yc) {
  const g = divGeom(dv); if (!g.mark) return "";
  const m = g.mark, st = `left:${m.x}px;top:${yc + m.dy}px;width:${m.w}px;height:${m.h}px`;
  const inner = dv === "notch" ? `<s class="rule"></s><b class="tail"><b></b></b>` : "";
  return `<i class="rdiv" data-div="${dv}" data-i="${i}" data-cmp="divider" style="${st}">${inner}</i>`;
}
// one portrait with its badges. yc = ring centre (relative to the roster origin), st = state, lay = rail | inside | asbuilt
export function slotHTML(i, st, lay, tk, yc, fire, cell) {
  const dv = tk?.roster?.divider || "none", role = ROLES[i], x0 = RS.XC - RS.WIN / 2, y0 = yc - RS.WIN / 2;
  const art = st.down ? "down" : st.hurt ? "hurt" : "normal";
  const underFire = fire && fire.i === i && tk && tk.why.place !== "none" && tk.why.place !== "head";
  const win = `<div class="rw" data-i="${i}" style="left:${x0}px;top:${y0}px;width:${RS.WIN}px;height:${RS.WIN}px"><img src="${K}portraits/portrait_${role}_${art}@2x.png"><img class="shade" src="${K}portraits/portrait_shade@2x.png"></div>`;
  const rings = `${st.sel ? `<img class="rsel" src="${K}portraits/portrait_ring_selected@2x.png" style="left:${RS.XC - 34.4}px;top:${yc - 34.4}px">` : ""}${st.busy ? `<img class="rbusy on" src="${K}portraits/portrait_ring_busy@2x.png" style="left:${RS.XC - 34.4}px;top:${yc - 34.4}px">` : ""}${underFire ? `<i class="why-ring" style="left:${x0 - 3}px;top:${y0 - 3}px;width:${RS.WIN + 6}px;height:${RS.WIN + 6}px"></i>` : ""}`;
  const lite = lay === "railLite", tall = isTall(lay), hit = fire && fire.i === i && tk && tk.why.place !== "none" && tk.why.place !== "head";
  const gunCause = !!hit && fire.cause === "drawn"; // the weapon disc already says "drawn": no second gun anywhere on this portrait
  const status = (lite || tall) && !st.down && hit && !gunCause ? ["why", false] : st.down ? ["s_downed", false] : st.seen ? ["sawyou", false] : st.watched ? ["cone", false] : st.busy ? [st.busy, false] : null;
  const wState = gunCause ? "drawn" : st.weapon;
  const wsz = lay === "hang" ? 15 : lay === "under" ? 16 : lay === "inside" ? 17 : 20;
  const weapon = weaponDisc(wState, wsz, gunCause);
  let alert = "";
  if (fire && fire.i === i && tk && tk.why.place !== "none" && tk.why.place !== "head") { if (!gunCause) alert = `<span class="rd light why-d" data-cause="${fire.cause}" style="width:20px;height:20px">${ic(tk.why.icons[fire.cause], "12px", "var(--ink-icon)")}</span>`; }
  else if (st.hurt) alert = disc20("s_hurt", false, 20);
  const pips = Array.from({ length: 4 }, (_, k) => `<img src="${K}markers/pip_${k < (st.pips ?? 0) ? "full" : "empty"}@2x.png">`).join("");
  const dsz = tall ? wsz : 20;
  const statusHTML = !status ? "" : status[0] === "why" ? `<span class="rd light why-d" data-cause="${fire.cause}" style="width:${dsz}px;height:${dsz}px">${ic(tk.why.icons[fire.cause], Math.round(dsz * 0.6) + "px", "var(--ink-icon)")}</span>` : disc20(status[0], false, dsz);
  const bd = (slot, rect, html, a) => `<span class="bd" data-slot="${slot}" data-i="${i}" style="left:${a[0] + rect[0]}px;top:${a[1] + rect[1]}px;width:${rect[2]}px;height:${rect[3]}px">${html}</span>`;
  const box = (slot, rect, a) => `<i class="slotbox" data-slot="${slot}" data-i="${i}" style="left:${a[0] + rect[0]}px;top:${a[1] + rect[1]}px;width:${rect[2]}px;height:${rect[3]}px"></i>`;
  const guides = `<i class="gface" data-i="${i}" style="left:${x0}px;top:${y0}px;width:${RS.WIN}px;height:${RS.WIN}px"></i><i class="gring" data-i="${i}" style="left:${RS.XC - RS.RING}px;top:${yc - RS.RING}px;width:${RS.RING * 2}px;height:${RS.RING * 2}px"></i>`;
  const num = `<span class="numplate" style="background-image:url(${K}markers/plate_light@2x.png)">${i + 1}</span>`;
  let out = win + rings + guides;
  if (tall) {
    const S = SLOTS[lay], t = tabFor(lay, dv), a = [t.dx, yc + t.dy];
    out += `<div class="rtab ${lay}" data-i="${i}" style="left:${a[0]}px;top:${a[1]}px;width:${t.w}px;height:${t.h}px">${lay === "hang" ? nine("hud", "plate_dark", 22 / 60) : ""}</div>`;
    if (lay === "under" && (cell || i < 3)) out += dividerHTML(dv, i, yc);
    out += ["status", "weapon", "dots"].map((k) => box(k, S[k], a)).join("");
    out += (status ? bd("status", S.status, statusHTML, a) : "") + (weapon ? bd("weapon", S.weapon, weapon, a) : "") + bd("dots", S.dots, `<span class="pips row">${pips}</span>`, a);
  } else if (lite) {
    const S = SLOTS.railLite, t = S.tab, a = [t.dx, yc + t.dy];
    out += `<div class="rtab" data-i="${i}" style="left:${a[0]}px;top:${a[1]}px;width:${t.w}px;height:${t.h}px">${nine("hud", "plate_dark", 0.4)}</div>`;
    out += ["status", "weapon", "dots"].map((k) => box(k, S[k], a)).join("");
    out += (status ? bd("status", S.status, statusHTML, a) : "") + (weapon ? bd("weapon", S.weapon, weapon, a) : "") + bd("dots", S.dots, `<span class="pips lite">${pips}</span>`, a);
  } else if (lay === "rail") {
    const S = SLOTS.rail, t = S.tab, a = [t.dx, yc + t.dy];
    out += `<div class="rtab" data-i="${i}" style="left:${a[0]}px;top:${a[1]}px;width:${t.w}px;height:${t.h}px">${nine("hud", "plate_dark", 0.4)}</div>`;
    out += ["num", "status", "weapon", "alert", "dots"].map((k) => box(k, S[k], a)).join("");
    out += bd("num", S.num, num, a) + (status ? bd("status", S.status, statusHTML, a) : "") + (weapon ? bd("weapon", S.weapon, weapon, a) : "") + (alert ? bd("alert", S.alert, alert, a) : "") + bd("dots", S.dots, `<span class="pips">${pips}</span>`, a);
  } else if (lay === "inside") {
    const S = SLOTS.inside, a = [x0, y0];
    out += ["num", "status", "weapon", "alert", "dots"].map((k) => box(k, S[k], a)).join("");
    const d16 = (icon, light, dim) => disc20(icon, light, 17, dim);
    out += bd("num", S.num, num.replace("numplate", "numplate sm"), a) + (status ? bd("status", S.status, d16(status[0], false), a) : "") + (weapon ? bd("weapon", S.weapon, weapon, a) : "") + (alert ? bd("alert", S.alert, alert.replace(/20px/g, "17px"), a) : "") + bd("dots", S.dots, `<span class="pips sm">${pips}</span>`, a);
  } else { // asbuilt: what the 0.2.56 crop shows (offsets differ per slot, badges cross the rim and sit on the faces, an extra eye on the 4th, the Tech's gun is a grey ring)
    const J = [[-9, -4], [-10, -3], [-8, -5], [-10, -3]][i], bx = x0 + J[0] - 8, by = y0 + J[1] - 10;
    out += `<span class="bd ab" style="left:${x0 + 2}px;top:${y0 + 0}px">${num}</span>`;
    if (i !== 2) out += `<span class="bd ab eye" style="left:${bx}px;top:${by}px;width:24px;height:24px"><i class="orange"></i>${disc20("eye_open", false, 22)}</span>`;
    out += `<span class="bd ab" style="left:${x0 + 1}px;top:${y0 + RS.WIN - 20 + (i === 1 ? 2 : 0)}px">${i === 2 ? `<span class="rd grey" style="width:18px;height:18px">${ic("fire", 11, "var(--disc-icon)")}</span>` : `<span class="rd cyan" style="width:20px;height:20px">${ic("fire", 12, "#17302f")}</span>`}</span>`;
    out += `<span class="bd ab" style="left:${x0 + 17}px;top:${y0 + RS.WIN - 11}px"><span class="pips">${pips}</span></span>`;
    if (i === 3) out += `<span class="bd ab" style="left:${x0 + 40}px;top:${y0 - 14}px"><span class="xeye">${ic("eye_open", 14)}</span></span>`;
  }
  return out;
}
export const colSprite = (lay, dv, col) => lay === "under" ? `var(--spr-hud-roster_column-under-${dv}${col ? "-" + col : ""})` : isTall(lay) ? `var(--spr-hud-roster_column-${lay}${col ? "-" + col : ""})` : `var(--spr-hud-roster_column${col ? "-" + col : ""})`;
export function roster(tk, o = {}) {
  const lay = tk?.roster?.layout || "rail", dv = tk?.roster?.divider || "none", states = o.states || DEFAULT_STATES, fire = o.fire || null, H = colH(lay, dv), W = RS.W;
  const frame = colSprite(lay, dv, tk?.roster?.column);
  return `<div class="roster lay-${lay}" data-cmp="roster" data-layout="${lay}" data-column="${tk?.roster?.column || ""}" data-divider="${lay === "under" ? dv : ""}" style="width:${W}px;height:${H}px"><div class="rframe" style="height:${H}px;background:${frame} 0 0/${W}px ${H}px no-repeat"></div>${RS.YC.map((_, i) => slotHTML(i, states[i], lay, tk, ringY(lay, i, dv), fire)).join("")}</div>`;
}
// one cell (a ring cut from the column sprite plus its badges) for the states board
export function rosterCell(i, st, lay, tk, label, fire) {
  const dv = tk?.roster?.divider || "none", tall = isTall(lay), yc = tall ? 42 : RS.PITCH / 2, H = colH(lay, dv), h = tall ? (lay === "under" ? Math.round(yc + GAP_TOP + divGeom(dv).gap + 12) : 100) : RS.PITCH;
  const frame = tall ? `${colSprite(lay, dv, tk?.roster?.column)} 0 ${-(ringY(lay, 1, dv) - yc)}px/${RS.W}px ${H}px no-repeat` : `${colSprite("rail", dv, tk?.roster?.column)} 0 ${-(RS.YC[1] - yc)}px/${RS.W}px ${RS.H}px no-repeat`;
  return `<div class="rcellw"><div class="roster rcell lay-${lay}" data-cmp="roster-cell" data-layout="${lay}" data-divider="${lay === "under" ? dv : ""}" style="width:${RS.W}px;height:${h}px"><div class="rframe" style="background:${frame};height:${h}px"></div>${slotHTML(i, st, lay, tk, yc, fire, true)}</div><b class="rcap">${label}</b></div>`;
}
export function rosterStates(tk, lay, o = {}) {
  const S = [["normal", 0, { pips: 3, weapon: "holstered" }], ["selected", 1, { sel: true, pips: 3, weapon: "holstered" }], ["busy", 1, { busy: "s_watching", pips: 3, weapon: "holstered" }], ["seen", 2, { seen: true, pips: 3, weapon: "holstered" }],
    ["watched", 2, { watched: true, pips: 3, weapon: "holstered" }], ["drawn", 3, { weapon: "drawn", pips: 4 }], ["holstered", 3, { weapon: "holstered", pips: 4 }], ["fired on", 0, { weapon: "drawn", pips: 2 }, "fired"], ["drawn in sight", 3, { weapon: "holstered", pips: 3 }, "drawn"], ["hurt", 1, { hurt: true, pips: 1, weapon: "holstered" }], ["down", 2, { down: true, pips: 0 }]].filter(([l]) => !o.keep || o.keep.includes(l));
  const cols = o.keep ? `grid-template-columns:repeat(${o.keep.length},98px);gap:12px 8px;` : "";
  return `<div class="rstates${lay === "railLite" ? " lite" : isTall(lay) ? " tall" : ""}${o.caps === false ? " nocap" : ""}" data-cmp="roster-states" style="${cols}">${S.map(([l, i, st, f]) => rosterCell(i, st, lay, tk, l, f ? { i, cause: f } : null)).join("")}</div>`;
}
export const doneStrip = (word = "done") => `<div class="strip status" data-cmp="status">${nine("hud", "panel_strip", 0.3)}<span class="si">${ic("check", 12)}<b>${word}</b></span></div>`;
export const squadPlate = () => `<div class="plate squad" data-cmp="squad">${nine("hud", "plate_dark", 0.4)}<span>Squad</span></div><div class="plate gun" data-cmp="gunkey">${nine("hud", "plate_dark", 0.4)}${ic("fire", 14)}</div>`;
export function clockBar(t = "1:38", next = "3:10", cash = "$100,000", trail = "0", lamps = 0) {
  return `<div class="clockbar" data-cmp="clock">${nine("hud", "pill_status", 44 / 88)}<span class="ci"><b class="clk">${t}</b><u></u><span class="ff">${ic("nextevent", 18, "var(--live-ic)")}</span>${ic("p_unknown", 17)}<b>${next}</b><u></u>${ic("p_cash", 18)}<b>${cash}</b><u></u>${ic("p_trail", 18)}<b>${trail}</b><u></u><span class="lamps">${[0, 1, 2].map((i) => `<i class="${i < lamps ? "on" : ""}"></i>`).join("")}</span>${ic("pause", 16)}</span></div>`;
}
export const miniPlate = () => `<div class="mini" data-cmp="miniplate">${nine("hud", "panel_dark", 0.3)}<span>${["fire", "dot", "listen", "dot"].map((n) => ic(n, 12)).join("")}</span></div>`;
export const minimap = () => `<div class="minimap" data-cmp="minimap">${nine("hud", "panel_dark", 0.5)}<span class="mm"></span></div>`;
export function feedCard(l1 = "Listen", pts = "+4", clk = "0:55", l2 = "Inez: Yes. Yes. And thank you.") {
  return `<div class="feed" data-cmp="feed">${nine("hud", "panel_strip", 0.5)}<span class="fbar"></span><span class="fin"><span class="fh">${ic("listen", 16)}<b>${l1}</b><span class="fr2"><i class="fdot"></i><b>${pts}</b><b>${clk}</b></span></span><span class="fl">${l2}</span></span></div><div class="plate feedbtn" data-cmp="feedkey">${nine("hud", "plate_dark", 0.4)}${ic("papers", 14)}<i class="fdot"></i></div>`;
}
export function intel() {
  const row = (n, j, a, b, m, y) => `<div class="irow" style="top:${y}px"><span class="in1"><b>${n}</b><em>${j}</em></span><span class="dsc" style="left:${150}px">${disc(a, false)}</span><span class="dsc" style="left:${186}px">${disc(b, false)}</span><span class="imk">${mark(m, 22)}</span></div>`;
  return `<div class="intel" data-cmp="intel">${nine("hud", "panel_dark", 0.5)}<span class="ihead"><b>INTEL</b><em>what the cell knows</em>${ic("chev_up", 14)}</span>${row("Inez", "Waitress", "debt", "fearful", "ok", 36)}${row("Door guard", "Records Office", "secret", "loyal", "soft", 98)}</div>`;
}
export const goalLine = () => `<div class="plate goal" data-cmp="goal">${nine("hud", "plate_dark", 0.43)}<span>Take ${ic("chev_down", 10, "var(--live-ic)")} <b>Vale</b> alive.</span></div>`;
