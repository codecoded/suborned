// Engine 0.28 / 0.29 HUD pieces (brief 054 §3.1, §4.3, §7, §9.1; host-interface 0.28.0 + 0.29.0), drawn over the real play-zoom backdrops.
// World marks are ground decals in the flattened ground plane (like ranges.js); widths are metres. Colours are all sourced: cream and ink-deep (kit.uss),
// the cones' red (style contract signal_colours.cones), cyan only where the cell's own ring already is. Nothing here brings a palette.
// The shadows are computed from the engine's own rule (brief 055 §7, perception.mount) so the notch the lab draws is the notch Fan.Shadows holds.
import { FLATTEN as FL } from "./tokens.js";
import { PX, ring } from "./ranges.js";
import { nine, ic, pill } from "./parts.js";

const CREAM = "#f7f4ec", INK = "#0b1615", RED = "#e03a2f", CELL = "#60e0ea";
const M = (m) => m * PX, rad = (d) => (d * Math.PI) / 180, f1 = (n) => n.toFixed(1);
const GY = PX * FL;                                          // ground metres -> screen px down the page
let uid = 0;
const at = (html, x, y, cls = "") => `<div class="at ${cls}" style="left:${x}px;top:${y}px">${html}</div>`;
const pillAt = (p, tk, x, tipY) => at(pill(p, tk), x, tipY - 10);
const cap = (t, x, y) => `<span class="cap" style="left:${x}px;top:${y}px">${t}</span>`;

// ---------------------------------------------------------------- who stands where (feet, play px on the 1080p frame) in each backdrop
export const CAFE = { A: { x: 385, y: 706 }, C: { x: 413, y: 632 }, B: { x: 530, y: 658 }, T: { x: 198, y: 937 }, E: { x: 288, y: 962 } };  // help-pip: customer, Inez, the off-duty guard at table 1, a passer-by, the Enforcer
export const OFFICE = { G: { x: 960, y: 514 } };                                                                                       // guard-back: the door guard on the landing
const FIG = 56;                                                                                                                        // a figure is about 56 px tall at play zoom

// ---------------------------------------------------------------- geometry (ground metres relative to the watcher's feet; +x right, +y down the page; heading 0 = right, 90 = down)
const pt = (r, a) => [r * Math.cos(rad(a)), r * Math.sin(rad(a))];
const wedgeD = (R, heading, half) => { const [x0, y0] = pt(R, heading - half), [x1, y1] = pt(R, heading + half); return `M0,0 L${f1(x0)},${f1(y0)} A${f1(R)},${f1(R)} 0 0 1 ${f1(x1)},${f1(y1)} Z`; };
const polyD = (pts) => "M" + pts.map(([x, y]) => `${f1(M(x))},${f1(M(y))}`).join(" L") + " Z";
export const relM = (o, px, py) => [(px - o.x) / PX, (py - o.y) / GY];                      // a backdrop pixel -> metres from the watcher
export const cutPx = (o, pts) => pts.map(([px, py]) => relM(o, px, py));                    // a solid drawn on the backdrop (its footprint in px) -> a polygon in metres

// One occluder's shadow, the engine's rule (brief 055 §7.5): an eye at L cm above its floor, a solid of height h cm and footprint radius r m at distance d,
// targets seen by head (160) or chest (120): hidden where neither line clears. Returns a convex quad in metres, or null when nothing is hidden within reach.
export function shadowOf(occ, L, reachM, heads = [160, 120]) {
  const d = Math.hypot(occ.x, occ.y), a = Math.atan2(occ.y, occ.x), half = Math.atan2(occ.r, d), h = occ.h;
  let s0 = d + occ.r, s1 = reachM;
  if (L >= h) { const clear = heads.map((T) => (T >= L ? 0 : (d * (L - T)) / (L - h))); s1 = Math.min(reachM, Math.min(...clear)); }   // seen over it from far enough behind: hidden only close behind
  else { const ok = heads.filter((T) => T > L).map((T) => (d * (T - L)) / (h - L)); if (ok.length) s0 = Math.max(s0, Math.max(...ok)); }  // under it (low eye): hidden from some way behind it on
  if (s1 <= s0) return null;
  const P = (s, da) => [s * Math.cos(a + da), s * Math.sin(a + da)];
  return [P(s0, -half), P(s1, -half), P(s1, half), P(s0, half)];
}

// a decal is under what stands in front of it: occl = screen-space ellipses {cx,cy,rx,ry} (a tall tree, a lamp) cut out of the whole layer, as the depth test does in the game
const layer = (inner, occl = []) => { const id = "oc" + ++uid; return `<svg class="rsvg w29" width="1920" height="1080" viewBox="0 0 1920 1080" style="z-index:0">${occl.length ? `<defs><mask id="${id}" maskUnits="userSpaceOnUse" x="0" y="0" width="1920" height="1080"><rect width="1920" height="1080" fill="#fff"/>${occl.map((e) => `<ellipse cx="${e.cx}" cy="${e.cy}" rx="${e.rx}" ry="${e.ry}" fill="#000"/>`).join("")}</mask></defs><g mask="url(#${id})">${inner}</g>` : inner}</svg>`; };
const G = (o, inner) => `<g transform="translate(${o.x} ${o.y}) scale(1 ${FL})">${inner}</g>`;
const maskOf = (id, cuts) => `<mask id="${id}" maskUnits="userSpaceOnUse" x="-1400" y="-1400" width="2800" height="2800"><rect x="-1400" y="-1400" width="2800" height="2800" fill="#fff"/>${cuts.map((c) => `<path d="${polyD(c)}" fill="#000"/>`).join("")}</mask>`;

// ---------------------------------------------------------------- 1. a civilian's sight: a rim-only wedge, +-60 deg, 13 m, no near ring, cream (a civilian is not a red signal)
export function civCone(o, { heading, half = 60, reachM = 13, cuts = [], shadows = [], alpha = 1, occl = [] }) {
  const id = "cv" + ++uid, R = M(reachM), all = [...cuts, ...shadows], d = wedgeD(R, heading, half);
  const rim = (w, col, a) => `<path d="${d}" fill="none" stroke="${col}" stroke-opacity="${a}" stroke-width="${w}" stroke-linejoin="round"/>`;
  const edge = all.map((c) => `<path d="${polyD(c)}" fill="none" stroke="${INK}" stroke-opacity=".55" stroke-width="${M(0.12)}" stroke-linejoin="round"/><path d="${polyD(c)}" fill="none" stroke="${CREAM}" stroke-width="${M(0.05)}" stroke-linejoin="round"/>`).join("");
  const dead = shadows.map((c) => `<path d="${polyD(c)}" fill="url(#${id}h)"/>`).join("");
  return layer(`<defs>${maskOf(id, all)}<clipPath id="${id}c"><path d="${d}"/></clipPath><pattern id="${id}h" width="${M(0.34)}" height="${M(0.34)}" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><line x1="0" y1="0" x2="0" y2="${M(0.34)}" stroke="${CREAM}" stroke-opacity=".7" stroke-width="${M(0.045)}"/></pattern></defs>` + G(o, `<g opacity="${alpha}"><g mask="url(#${id})">${rim(M(0.17), INK, 0.6)}${rim(M(0.075), CREAM, 1)}</g><g clip-path="url(#${id}c)">${dead}${edge}</g></g>`), occl);
}
// the same fan for a security watcher TODAY (flat unlit fill 38 %, crisp rim 0.085 m, red) and as 0.28 draws it (the shadows cut out of the fill, a thin rim on each cut)
export function secCone(o, { heading, half, reachM, shadows = [], cuts = [], cut = true, fill = 0.38, occl = [] }) {
  const id = "sc" + ++uid, R = M(reachM), d = wedgeD(R, heading, half), all = cut ? [...cuts, ...shadows] : [];
  const edge = all.map((c) => `<path d="${polyD(c)}" fill="none" stroke="${INK}" stroke-opacity=".45" stroke-width="${M(0.08)}"/><path d="${polyD(c)}" fill="none" stroke="${RED}" stroke-opacity=".9" stroke-width="${M(0.045)}" stroke-linejoin="round"/>`).join("");
  return layer(`<defs>${maskOf(id, all)}<clipPath id="${id}c"><path d="${d}"/></clipPath></defs>` + G(o,
    `<g mask="url(#${id})"><path d="${d}" fill="${RED}" fill-opacity="${fill}"/><path d="${d}" fill="none" stroke="${RED}" stroke-width="${M(0.085)}" stroke-linejoin="round"/></g><g clip-path="url(#${id}c)">${edge}</g>`), occl);
}
// security see all round within 13 m: a thin red ring rim, clipped by walls and solids (never filled)
export function secRing(o, { reachM = 13, cuts = [], shadows = [], alpha = 1, occl = [] }) {
  const id = "sr" + ++uid, R = M(reachM), all = [...cuts, ...shadows];
  const cir = (w, col, a) => `<ellipse rx="${R}" ry="${R}" fill="none" stroke="${col}" stroke-opacity="${a}" stroke-width="${w}"/>`;
  return layer(`<defs>${maskOf(id, all)}</defs>` + G(o, `<g opacity="${alpha}" mask="url(#${id})">${cir(M(0.14), INK, 0.5)}${cir(M(0.07), RED, 1)}</g>`), occl);
}
// 2. the near-body ring: 1.5 m at a civilian's feet, dashed (it is a reach for bodies only; a cone is a wedge, a ring is a place), cream
export function nearRing(o, { cm = 150 }) {
  const R = M(cm / 100), n = 36, seg = (2 * Math.PI * R) / n;
  const dash = `stroke-dasharray="${f1(seg * 0.56)} ${f1(seg * 0.44)}"`;
  return layer(G(o, `<ellipse rx="${R}" ry="${R}" fill="none" stroke="${INK}" stroke-opacity=".6" stroke-width="${M(0.14)}" ${dash}/><ellipse rx="${R}" ry="${R}" fill="none" stroke="${CREAM}" stroke-width="${M(0.07)}" ${dash}/>`));
}
const hoverRing = (o) => layer(G(o, ring("hover", CREAM)));

// ---------------------------------------------------------------- 3. mood on the person: one kit disc, one face, no words
const moodIc = (m, px = 17) => `<i class="ic" style="width:${px}px;height:${px}px;background:#f7f4ec;-webkit-mask-image:url(/world29/mood_${m}@2x.png)"></i>`;
export const moodDisc = (m, sz = 32) => `<span class="rd mood" data-cmp="mood-${m}" data-mood="${m}" style="width:${sz}px;height:${sz}px">${moodIc(m, Math.round(sz * 0.53))}</span>`;
const overHead = (feet, html, k = 1) => at(`<span class="ohead" data-cmp="over-head">${html}</span>`, feet.x, feet.y - FIG + 2);
// the other thing a person may carry over the head (the roster's own status disc, light) so the "at most two icons" case is drawn
const statusDisc = (icon, sz = 32) => `<span class="rd light" style="width:${sz}px;height:${sz}px">${ic(icon, Math.round(sz * 0.6) + "px", "var(--ink-icon)")}</span>`;
// the prediction marks: a dashed ring means "the Scout's read, not a result". Exact (after Listen) uses the kit's own solid marks.
export const dashed = (kind, px = 16) => {
  const r = px / 2 - 1, half = kind === "half" ? `<path d="M${px / 2} ${px / 2 - r + 2} A${r - 2} ${r - 2} 0 0 0 ${px / 2} ${px / 2 + r - 2} Z" fill="${CREAM}"/>` : "";
  return `<i class="dm" data-cmp="mark-${kind}-dashed" style="width:${px}px;height:${px}px"><svg width="${px}" height="${px}"><circle cx="${px / 2}" cy="${px / 2}" r="${r}" fill="none" stroke="${CREAM}" stroke-width="1.5" stroke-dasharray="3.1 2.3"/>${half}</svg>${kind === "ok" ? ic("check", Math.round(px * 0.55)) : ""}</i>`;
};
const exact = (kind, px = 18) => `<img class="mk0" data-cmp="mark-${kind}-exact" src="/kit/badges/mark_${kind === "ok" ? "ok" : "half"}@2x.png" style="width:${px}px;height:${px}px">`;
const likelyTag = (kind, word = "likely") => `<span class="uplate lk" data-cmp="likely">${nine("ov", "plate_dark", 0.37)}<span>${dashed(kind, 15)}${word}</span></span>`;
const hurtTag = () => `<span class="uplate lk" data-cmp="hurt">${nine("ov", "plate_dark", 0.37)}<span>${dashed("half", 15)}</span></span>`;
const exactTag = (kind) => `<span class="uplate lk" data-cmp="exact">${nine("ov", "plate_dark", 0.37)}<span>${exact(kind, 15)}</span></span>`;

// ---------------------------------------------------------------- the verb plate (kit plate_verbs, 4 knobs) with the rows that are offered closed up
function verbPlate(rows, cmp = "verbs") {
  const knob = (r, i) => { const x = 42.3 - 20 + i * 47.6;
    if (!r) return `<img class="vk" src="/kit/buttons/btn_verb_disabled@2x.png" style="left:${x}px">`;
    return `<img class="vk" src="/kit/buttons/btn_verb_${r.sel ? "selected" : "normal"}@2x.png" style="left:${x}px">${`<span class="vi" style="left:${x}px">${ic(r.icon, 19)}</span>`}${r.mark ? `<span class="vm" style="left:${x + 27}px">${r.mark}</span>` : ""}`; };
  return `<div class="vplate" data-cmp="${cmp}"><div class="fr" style="border-width:26.5px 27px;border-style:solid;border-image-source:url(/kit/panels/plate_verbs@2x.png);border-image-slice:53 54 53 54 fill;border-image-width:26.5px 27px"></div>${[0, 1, 2, 3].map((i) => knob(rows[i], i)).join("")}</div>`;
}

// ---------------------------------------------------------------- the scenes
const takePill = { text: "Take down the guard · 6 s · +1 Trail", kind: "harm", icon: "takedown", faces: ["enforcer"], st: "fire", cmp: "pill-takedown" };
const pressure = (after = "", cmp = "pill-pressure") => ({ head: "Hold", text: "Pressure Inez · 40 s · +1 Trail", kind: "talk", icon: "pressure", faces: ["fixer"], st: "offer", live: true, after, cmp });
const help = (after = "", cmp = "pill-help") => ({ head: "Hold", text: "Help Inez · $40k · 64 s · +1 Trail", kind: "talk", icon: "help", faces: ["fixer"], st: "offer", live: true, after, cmp });
const { A, B, C, T, E } = CAFE;
// the café facade is a wall the wedge cannot cross: a cut in px, turned into metres from each watcher
const WALL = [[0, 0], [1000, 0], [900, 250], [750, 375], [550, 500], [500, 535], [350, 565], [120, 510], [0, 470]];     // the café facade and its awning edge (a solid to the sky)
const LOFT = [[0, 905], [150, 868], [300, 975], [330, 1080], [0, 1080]];                     // the corner building's roof at the lower left
const TOPIARY = { x: 478, y: 758, r: 0.45, h: 200 };                                           // the 2 m topiary beside the terrace

const TOP_OCC = [{ cx: 478, cy: 742, rx: 13, ry: 24 }];                                          // the topiary's own picture stands over the decal
const OFFICE_OCC = [{ cx: 1060, cy: 598, rx: 20, ry: 98 }, { cx: 999, cy: 586, rx: 12, ry: 30 }];  // the 6.2 m cypress, the small topiary, a lamp post
const cafeCuts = (o) => [cutPx(o, WALL)];
export const W29_SCENES = [
  // 1) hover a customer with a Pressure armed on Inez at Alert 0-1: they do not care, no cone (name/mood only)
  { id: "w29-quiet", name: "0.29 · hover a customer, Pressure armed, Alert 0: no cone", bg: "help-pip", status: "A 0", items: (tk) => [
    hoverRing(A), overHead(C, moodDisc("uneasy")), pillAt(pressure(likelyTag("ok")), tk, C.x, C.y - FIG - 46)] },
  // 2) the same at Alert 2: she would see it, her cone shows (cut by the café wall)
  { id: "w29-cone-a2", name: "0.29 · hover a customer, Pressure armed, Alert 2: her cone", bg: "help-pip", status: "A 2", items: (tk) => [
    civCone(A, { heading: -78, cuts: cafeCuts(A) }), hoverRing(A), overHead(C, moodDisc("uneasy")), pillAt(pressure(likelyTag("ok")), tk, C.x, C.y - FIG - 46)] },
  // 3) a takedown armed on a passer-by: the customer who would see it shows her cone and the 1.5 m ring for a body
  { id: "w29-takedown", name: "0.29 · hover a customer, takedown armed: cone + near-body ring", bg: "help-pip", status: "A 0", items: (tk) => [
    civCone(A, { heading: 75, shadows: [shadowOf({ ...TOPIARY, ...relXY(A, TOPIARY) }, 160, 13)].filter(Boolean), cuts: [], occl: TOP_OCC }), nearRing(A, {}), hoverRing(A),
    pillAt(takePill, tk, T.x, T.y - FIG - 8)] },
  // 4) an off-duty guard at table 1 hovered with the same Pressure: security see all round, a thin 13 m ring, clipped
  { id: "w29-guard-ring", name: "0.29 · hover the off-duty guard, Pressure armed: 13 m ring", bg: "help-pip", status: "A 0", items: (tk) => [
    secRing(B, { cuts: [cutPx(B, WALL), cutPx(B, LOFT)] }), hoverRing(B), overHead(C, moodDisc("uneasy")), pillAt(pressure(likelyTag("ok")), tk, C.x, C.y - FIG - 46)] },
  // 5) Alt: everyone on the armed order's eye (cap 6): her cone, his ring, one rim each, slightly quieter than the hovered one
  { id: "w29-alt", name: "0.29 · Alt: the armed order's people (cap 6), Alert 2", bg: "help-pip", status: "A 2", items: (tk) => [
    secRing(B, { cuts: [cutPx(B, WALL), cutPx(B, LOFT)], alpha: 0.85 }), civCone(A, { heading: -78, cuts: cafeCuts(A), alpha: 0.9 }), overHead(C, moodDisc("uneasy")),
    pillAt(pressure(likelyTag("ok")), tk, C.x, C.y - FIG - 46)] },
  // 6) the security fan today and with shadows: a door guard on his landing, a 6.2 m cypress
  { id: "w29-fan-today", name: "0.28 · security cone today (no shadow)", bg: "guard-back", status: "today", items: () => [fan(false)] },
  { id: "w29-fan-cut", name: "0.28 · security cone with Fan.Shadows cut out", bg: "guard-back", status: "0.28", items: () => [fan(true)] },
  // 7) mood on the person: friendly / uneasy / scared, one icon, and the two-icon case
  { id: "w29-mood", name: "0.29 · mood on the person: one icon, no words", bg: "help-pip", status: "mood", items: () => [
    overHead(A, moodDisc("friendly")), overHead(C, `${moodDisc("uneasy")}${statusDisc("s_listening")}`), overHead(B, moodDisc("scared")), overHead(T, moodDisc("uneasy"))] },
  // 8) the price line: before a Watch, after a Watch (likely / worse, dashed), after Listen (exact)
  { id: "w29-price", name: "0.29 · price line: before Watch, after Watch (dashed), after Listen (exact)", bg: "help-pip", status: "price", items: (tk) => [
    cap("Before a Watch", 120, 150), pillAt(pressure("", "p-before"), tk, 330, 214),
    cap("After a Watch: the favoured approach wears likely, the hurt one a dashed half", 120, 250), pillAt(pressure(likelyTag("ok"), "p-likely"), tk, 330, 314), pillAt(help(hurtTag(), "p-worse"), tk, 800, 314),
    cap("After Listen: everything exact (the kit's own marks)", 120, 350), pillAt(pressure(exactTag("ok"), "p-exact-ok"), tk, 330, 414), pillAt(help(exactTag("half"), "p-exact-half"), tk, 800, 414)] },
  // 9) the verb plate: Watch only when offered; the rest close up
  { id: "w29-watch-on", name: "0.29 · verb plate, Watch offered (Inez, uneasy)", bg: "help-pip", status: "Inez", items: () => [
    hoverRing(C), overHead(C, moodDisc("uneasy")), at(verbPlate([{ icon: "watch" }, { icon: "listen" }, { icon: "pressure", mark: dashed("ok", 15) }, { icon: "help", mark: dashed("half", 15) }], "verbs-watch"), 960, 1010)] },
  { id: "w29-watch-off", name: "0.29 · verb plate, no Watch to give (the driver): rows close up", bg: "help-pip", status: "driver", items: () => [
    hoverRing(T), at(verbPlate([{ icon: "listen" }, { icon: "pay" }, { icon: "help" }, { icon: "pressure" }], "verbs-nowatch"), 960, 1010)] },
  // 10) the marks side by side on one piece of ground (the wedge and the rings cut to 5 m / 4 m so they fit a tile; the lines are the lines)
  { id: "w29-told-apart", name: "0.29 · told apart: civilian cone, security cone, security ring, near-body ring, hover ring", bg: "help-pip", blurBg: true, noHud: true, items: () => toldApart() },
];
function relXY(o, p) { const [x, y] = relM(o, p.x, p.y); return { x, y }; }
// the door guard's fan: heading 70 deg, +-45 deg, 12 m (stand-ins for the site's numbers), the cypress that stands on his line
function fan(cut) {
  const G0 = OFFICE.G, cyp = { x: 1060, y: 688, r: 0.55, h: 620 }, rel = { ...cyp, ...relXY(G0, cyp) };
  const sh = [shadowOf(rel, 235, 12)].filter(Boolean);
  return secCone(G0, { heading: 70, half: 45, reachM: 12, shadows: sh, cut, occl: OFFICE_OCC });
}

// the marks told apart: five stand-ins on the same pavement (shortened reach so each fits; widths are the real ones)
function toldApart() {
  const y = 760, xs = [250, 560, 870, 1180, 1490], P = (x) => ({ x, y });
  const fig = (x) => `<img class="gagent" src="/bg/ground-agent.png" style="position:absolute;left:${x - 17}px;top:${y - 63}px;width:34px;height:66px;z-index:1">`;
  const lab = ["Civilian's sight: rim, no fill, cream", "Security's sight: filled, red", "Security see all round: thin red ring", "Near-body ring: dashed, cream, 1.5 m", "Hover ring (as built)"];
  return [
    cap("Told apart on one pavement. The cone and the ring are cut to 5 m and 4 m to fit; the line weights are the real ones.", 40, 40),
    civCone(P(xs[0]), { heading: 0, half: 60, reachM: 5 }), secCone(P(xs[1]), { heading: 0, half: 40, reachM: 5 }), secRing(P(xs[2]), { reachM: 4 }), nearRing(P(xs[3]), {}), layer(G(P(xs[4]), ring("hover", CREAM))),
    ...xs.map(fig), ...lab.map((t, i) => cap(t, xs[i] - 110, y + 120))];
}
