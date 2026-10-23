// The scenes: placements of every overlay over the 0.2.56 backdrops (anchors read off the shots, brief 044), and the board.
import { ARROWS, FONTS, DIVIDERS, divGeom, COLUMNS } from "./tokens.js";
import { rosterCost, colH, RS } from "./parts.js";
import { rangeItems, rangeStates, rangeRed, rangeArc } from "./ranges.js";
import { pill, chip, marker, tag, pop, rings, whyHead, groundTile, whyBoard, roster, rosterStates, doneStrip, squadPlate, clockBar, miniPlate, minimap, feedCard, intel, goalLine } from "./parts.js";

const at = (html, x, y, cls = "") => `<div class="at ${cls}" style="left:${x}px;top:${y}px">${html}</div>`;
const pillAt = (p, tk, x, tipY) => at(pill(p, tk), x, tipY - 10);
const cap = (t, x, y) => `<span class="cap" style="left:${x}px;top:${y}px">${t}</span>`;

const listen = { face: "scout", mark: "ok", kind: "look", lines: [["debt", "Bureau loan"], ["fearful", "Fearful"], ["secret", "Loves the guard"]], unl: [{ icon: "help", word: "Help" }, { icon: "pressure", word: "Pressure" }] };
const helpOffer = { head: "Hold", text: "Help Inez · $40k · 64 s · +1 Trail", kind: "talk", icon: "help", faces: ["fixer"], st: "offer", live: true };

export const SCENES = [
  { id: "board", name: "Board: every piece", bg: "help-pip", board: true },
  { id: "pill-states", name: "Pill states", bg: "guard-back", status: "done", items: (tk) => [
    pillAt({ head: "Hold", text: "Watch the guard · 26 s", kind: "look", icon: "watch", faces: ["scout"], st: "offer", seen: true, live: true, cmp: "pill-watched" }, tk, 960, 482),
    pillAt({ head: "Hold", text: "Call the driver · $0 · 40 s", kind: "talk", icon: "call", faces: ["fixer"], st: "hold", p: 0.55, cmp: "pill-hold" }, tk, 560, 420),
    pillAt({ head: "Let go", text: "Loop the camera · 12 s", kind: "tech", icon: "loop", faces: ["tech"], st: "ready", p: 1, cmp: "pill-ready" }, tk, 905, 215),
    pillAt({ text: "Firing at an officer · down in 2 s", kind: "harm", icon: "fire", faces: ["enforcer"], st: "fire", cmp: "pill-fire" }, tk, 1485, 690)] },
  { id: "help-pip", name: "Chip + Help pill (hold me)", bg: "help-pip", status: "done", items: (tk) => [at(chip(listen), 961, 392), pillAt(helpOffer, tk, 960, 456)] },
  { id: "listen-chip", name: "Listen chip", bg: "listen-chip", status: "done", items: () => [at(chip(listen), 961, 347)] },
  { id: "walkin", name: "Walk-in plate", bg: "walkin", status: "→ Inez", items: (tk) => [
    at(chip({ face: "scout", mark: "ok", kind: "look", lines: [["routine", "Rounds"]], unl: [{ icon: "listen", word: "Listen" }, { icon: "pay", word: "Pay" }] }), 961, 392),
    pillAt({ text: "to Inez · Listen · 14 s", kind: "look", icon: "listen", faces: ["scout"], st: "order" }, tk, 960, 456)] },
  { id: "call-window", name: "Window marker", bg: "call-window", status: "done", items: () => [
    at(chip({ face: "fixer", mark: "ok", kind: "talk", lines: [["eye_shut", "Guard away"]], cmp: "chip-window" }), 960, 417),
    at(marker({ icon: "eye_shut", num: "174", left: 174, total: 177 }), 960, 450)] },
  { id: "cancel-warn", name: "Cancel tag", bg: "cancel-warn", status: "16 s", items: (tk) => [at(tag("Cancels Watch · 4 s lost"), 958, 407), pillAt({ head: "Hold", text: "Watch the patrol · 20 s", kind: "look", icon: "watch", faces: ["scout"], st: "offer", live: true }, tk, 958, 456)] },
  { id: "cancel-pop", name: "Closing pop", bg: "cancel-pop", status: "cancelled", items: () => [at(pop({ icon: "close_x", text: "Watch cancelled · 5 s lost" }), 960, 472)] },
  { id: "guard-back", name: "Guard back pop", bg: "guard-back", status: "done", items: () => [at(pop({ icon: "eye_open", text: "Guard back" }), 960, 478)] },
  { id: "tp-fire", name: "Third person fire", bg: "tp-fire", status: "drawing", items: (tk) => [pillAt({ text: "Drawing", kind: "harm", icon: "fire", faces: ["scout"], st: "fire" }, tk, 970, 505)] },
  { id: "range-before", name: "Range cues 049: before (0.2.65)", bg: "help-pip", status: "done", items: (tk) => [rangeItems("before", tk)] },
  { id: "range-a", name: "Target rings 049: after A (red when they aim at us, ticks in an exchange), with the under-fire arc", bg: "help-pip", status: "done", items: (tk) => [rangeItems("a", tk)] },
  { id: "range-noarc", name: "Target rings 049: after A, red ring only (no arc on the agents)", bg: "help-pip", status: "done", items: (tk) => [rangeItems("noarc", tk)] },
  { id: "range-arc", name: "Target rings 049: the arc at the real ring size", bg: "help-pip", blurBg: true, noHud: true, items: (tk) => [rangeArc(tk)] },
  { id: "range-states", name: "Target rings 049: plain rings and the ones we aim at", bg: "help-pip", blurBg: true, noHud: true, items: () => [rangeStates()] },
  { id: "range-red", name: "Target rings 049: red rings (they aim or fire at us), frozen frames, and a cone", bg: "help-pip", blurBg: true, noHud: true, items: () => [rangeRed()] },
  { id: "roster", name: "Roster slots", bg: "help-pip", board: true, noRoster: true, blurBg: true, status: "done", roster: true },
  { id: "roster-nonum", name: "Roster: no numbers", bg: "help-pip", board: true, noRoster: true, blurBg: true, status: "done", roster: true, nonum: true },
  { id: "fonts", name: "Type: six fonts on the same HUD", bg: "help-pip", board: true, blurBg: true, noRoster: true, fonts: true, status: "done" },
  { id: "roster-under", name: "Roster: icons under the portrait", bg: "help-pip", board: true, noRoster: true, blurBg: true, status: "done", roster: true, under: true },
  { id: "roster-div", name: "Roster: dividers between characters (2x)", bg: "help-pip", board: true, noRoster: true, blurBg: true, status: "done", div2x: true, noHud: true },
  { id: "roster-divstates", name: "Roster: dividers, every state (1x)", bg: "help-pip", board: true, noRoster: true, blurBg: true, status: "done", divstates: true, noHud: true },
  { id: "roster-clean", name: "Roster column: cleaned art, as built beside three (2x)", bg: "help-pip", board: true, noRoster: true, blurBg: true, status: "done", cleanBoard: true, noHud: true },
  { id: "roster-cleanstates", name: "Roster column: cleaned art, every state (1x)", bg: "help-pip", board: true, noRoster: true, blurBg: true, status: "done", cleanStates: true, noHud: true },
  { id: "under-fire", name: "Under fire: arrow and why", bg: "help-pip", blurBg: true, status: "hurt", fire: true, items: (tk, cause, v) => {
    const o = { cause, theta: v.theta, ring: v.ring, occ: v.occ };
    const ARW = Object.fromEntries(ARROWS);
    return [cap(`${ARW[tk.why.arrow.style] || tk.why.arrow.style}, shooter at ${v.theta} deg (ground plane), cause: ${cause}`, 330, 150),
      at(groundTile(tk, { ...o, ground: "pave", z: 2.6, cmp: "tile-big-pave" }), 330, 172, "tl"), at(groundTile(tk, { ...o, ground: "asph", z: 2.6, cmp: "tile-big-asph" }), 790, 172, "tl"),
      cap("1:1, the person is about 65 px (play zoom)", 1280, 150), at(groundTile(tk, { ...o, ground: "pave", z: 1, cmp: "tile-play-pave" }), 1280, 172, "tl"), at(groundTile(tk, { ...o, ground: "asph", z: 1, cmp: "tile-play-asph" }), 1460, 172, "tl"),
      cap("Same cue, no ring (agent not selected)", 1280, 360), at(groundTile(tk, { ...o, ground: "pave", z: 1, ring: "none", cmp: "tile-play-noring" }), 1280, 382, "tl")];
  } },
  { id: "arrows", name: "Arrows: every style, three grounds", bg: "help-pip", blurBg: true, noHud: true, items: (tk, cause, v) => {
    const cols = ["asis", "arc", "notch", "streak", "ripple"], names = { asis: "As built (the problem)", arc: "A  Arc on the ring", notch: "B  Notch on the ring", streak: "C  Streak in", ripple: "D  Ripple" };
    const rows = [["pave", 40, "selected", "pavement, selected, shooter behind-right"], ["asph", 215, "selected", "asphalt, selected, shooter in front-left"], ["pave", 305, "none", "pavement, not selected, shooter in front-right"]];
    const z = 1.8, tw = 300 * 0.55 * z, th = 290 * 0.55 * z, gap = 36, x0 = (1920 - (5 * tw + 4 * gap)) / 2;
    const out = [cap("Under fire: the ground cue toward the shooter. Cream with an ink-deep edge, ground-plane decals, hidden behind the agent. The cause disc is shown over the head.", 40, 18)];
    rows.forEach(([g, th_, ring, label], r) => cols.forEach((st, c) => {
      const x = x0 + c * (tw + gap), y = 66 + r * (th + 30);
      out.push(cap(r === 0 ? names[st] : label, x, y - 17) + at(groundTile({ ...tk, why: { ...tk.why, arrow: { ...tk.why.arrow, style: st } } }, { ground: g, theta: th_, ring, z, cause, cmp: "arrow-" + st + "-" + r }), x, y, "tl"));
    }));
    out.push(cap("Rows: " + rows.map((r) => r[3]).join("  |  "), 40, 1058));
    return out;
  } },
  { id: "ground", name: "Ground markers: behind the agent", bg: "help-pip", blurBg: true, noHud: true, items: (tk, cause, v) => {
    const z = 2.6, tw = 300 * 0.55 * z, gap = 30, x0 = (1920 - (4 * tw + 3 * gap)) / 2, y = 190, out = [cap("Ground markers are decals in the ground plane: the agent stands in front of the back half of the ring and of the cue (left of each pair: how 0.2.56 drew it, over his legs).", 40, 40)];
    [["pave", false], ["pave", true], ["asph", false], ["asph", true]].forEach(([g, occ], c) => {
      const x = x0 + c * (tw + gap); out.push(cap(occ ? "Decal behind the agent (what Latch builds)" : "Drawn over the agent (0.2.56)", x, y - 17) + at(groundTile(tk, { ground: g, theta: 55, ring: "selected", occ, z, cause: null, cmp: "tile-occ-" + g + (occ ? "-on" : "-off") }), x, y, "tl"));
    });
    return out;
  } },
];

export const GROUNDS = [["help-pip", "Cafe pavement"], ["cancel-pop", "Asphalt"], ["tp-fire", "Teal wall and steps"], ["guard-back", "Records front"]];

function board(tk, cause) {
  const L = (h, x, y, label) => cap(label, x, y - 17) + at(h, x, y, "tl");
  const P = (p, x, y, label) => cap(label, x, y - 17) + at(pill(p, tk), x, y, "tl");
  return [
    P({ head: "Hold", text: "Help Inez · $40k · 64 s · +1 Trail", kind: "talk", icon: "help", faces: ["fixer"], st: "offer", live: true, cmp: "pill-offer" }, 190, 120, "Offer: press and hold the pill"),
    P({ head: "Hold", text: "Call the driver · $0 · 40 s", kind: "talk", icon: "call", faces: ["fixer"], st: "hold", p: 0.55, cmp: "pill-hold" }, 800, 120, "Hold at 55 %"),
    P({ head: "Let go", text: "Loop the camera · 12 s", kind: "tech", icon: "loop", faces: ["tech"], st: "ready", p: 1, cmp: "pill-ready" }, 190, 214, "Ready"),
    P({ text: "Firing at an officer · down in 2 s", kind: "harm", icon: "fire", faces: ["enforcer"], st: "fire", cmp: "pill-fire" }, 800, 214, "Fire"),
    P({ head: "Hold", text: "Watch the guard · 26 s", kind: "look", icon: "watch", faces: ["scout"], st: "offer", seen: true, live: true, cmp: "pill-watched" }, 190, 308, "Offer with Watched spot"),
    P({ text: "to Inez · Listen · 14 s", kind: "look", icon: "listen", faces: ["scout"], st: "order", cmp: "pill-walkin" }, 800, 308, "Walk-in plate"),
    L(chip(listen), 190, 420, "Done chip: facts and unlock pills"),
    L(chip({ face: "fixer", mark: "ok", kind: "talk", lines: [["eye_shut", "Guard away"]], cmp: "chip-window" }), 700, 420, "Window chip") +
    at(marker({ icon: "eye_shut", num: "174", left: 174, total: 177, cmp: "marker" }), 700, 478, "tl"),
    L(marker({ icon: "eye_shut", num: "9", left: 9, total: 177, low: true, cmp: "marker-low" }), 700, 530, "Window, last 10 s (amber blink)"),
    L(marker({ icon: "loop", face: "tech", left: 120, total: 177, cmp: "marker-tech" }), 700, 590, "Window held by the Tech"),
    L(chip({ face: "scout", mark: "soft", kind: "look", lines: [["routine", "Rounds"]], unl: [{ icon: "listen", word: "Listen" }, { icon: "pay", word: "Pay", shut: true }], cmp: "chip-soft" }), 960, 420, "Half answer, a shut way"),
    L(tag("Cancels Watch · 4 s lost"), 190, 640, "Cancel tag"),
    L(pop({ icon: "close_x", text: "Watch cancelled · 5 s lost" }), 190, 700, "Pop") + at(pop({ icon: "eye_open", text: "Guard back" }), 520, 700, "tl"),
    L(rings(0.6), 190, 790, "Rings: ground, hint, activity, aim"),
    ...(tk.why.place === "none" ? [] : [L(whyBoard(tk), 700, 690, "Why under fire: one icon, the strongest cause"), L(whyHead(cause, tk), 700, 790, "Over the agent"), L(groundTile(tk, { ground: "pave", theta: 40, z: 1.1, cmp: "tile-board" }), 880, 760, "Ground: toward the shooter")]),
  ].join("");
}

function rosterBoard(tk, cause) {
  const NAMES = { asbuilt: "As built 0.2.56", inside: "Inside the face", rail: "Rail, numbers", railLite: "Rail, no numbers", under: "Under the portrait", hang: "On a hanging plate" };
  const X = { asbuilt: 24, inside: 160, rail: 296, railLite: 462, under: 628, hang: 760 };
  const cols = ["asbuilt", "inside", "rail", "railLite", "under", "hang"].map((L) => cap(NAMES[L], X[L], 40) + at(roster({ ...tk, roster: { ...tk.roster, layout: L } }, { fire: { i: 3, cause } }), X[L], 60, "tl"));
  return cols.join("") + cap(`Every state, layout: ${tk.roster.layout}`, 1010, 60) + at(rosterStates(tk, tk.roster.layout), 1010, 84, "tl scale12");
}
// the no-numbers rail on its own: the column at 2x, the same column with numbers beside it, every state, and the strip that moves
function rosterNoNum(tk, cause) {
  const lite = { ...tk, roster: { layout: "railLite" } }, rail = { ...tk, roster: { layout: "rail" } };
  return cap("Rail, no numbers (44 px tab)", 40, 40) + at(roster(lite, { fire: { i: 3, cause } }), 40, 60, "tl scale2") +
    cap("Rail with numbers (54 px tab)", 420, 40) + at(roster(rail, { fire: { i: 3, cause } }), 420, 60, "tl scale2") +
    cap("Every state without numbers (selected, busy, seen, watched, drawn, holstered, under fire, hurt, down)", 800, 40) + at(rosterStates(lite, "railLite"), 800, 64, "tl scale12") +
    cap("The same states with numbers", 800, 448) + at(rosterStates(rail, "rail"), 800, 472, "tl scale12");
}
// icons under the portrait (brief 046 round 3): the side rail without numbers, the row under the portrait (column taller) and the row on a hanging plate, at 2x, every state, 1:1, and what each costs on screen
function rosterUnder(tk, cause) {
  const dv = tk.roster.divider, L = (l) => ({ ...tk, roster: { ...tk.roster, layout: l } }), fire = { i: 3, cause }, C = Object.fromEntries(["rail", "railLite", "under", "hang"].map((l) => [l, rosterCost(l, dv)]));
  const col = (l, name, x) => cap(name, x, 40) + at(roster(L(l), { fire }), x, 62, "tl scale2") + `<div class="costcap at tl" style="left:${x}px;top:${62 + colH(l, dv) * 2 + 8}px">${C[l].columnH} px tall · ${C[l].pctHeight} % of the screen height<br>${C[l].footprintW} px wide · ${C[l].pctArea} % of the screen</div>`;
  const row = (l, y, name) => cap(name, 800, y - 20) + at(rosterStates(L(l), l), 800, y, "tl scale12");
  const tr = (l, name) => `<tr><td>${name}</td><td>${C[l].columnH}</td><td>${C[l].pctHeight} %</td><td>${C[l].footprintW}</td><td>${C[l].pctArea} %</td><td>${C[l].pitch}</td><td>${C[l].statusX}</td></tr>`;
  const table = `<table class="cost at tl" style="left:840px;top:760px"><tr><th>At 1080p</th><th>Column px</th><th>% height</th><th>Dock width px</th><th>% screen</th><th>Portrait pitch px</th><th>Status strip x</th></tr>${tr("rail", "Rail, numbers")}${tr("railLite", "Rail, no numbers")}${tr("under", "Under the portrait")}${tr("hang", "Hanging plate")}</table>`;
  return col("railLite", "Rail, no numbers", 40) + col("under", "Icons under the portrait", 340) + col("hang", "Icons on a hanging plate", 590) +
    row("under", 84, "Under the portrait: every state") + row("hang", 424, "Hanging plate: every state") + table +
    cap("Actual size, 1080p", 1560, 60) + at(roster(L("railLite"), { fire }), 1560, 84, "tl") + at(roster(L("under"), { fire }), 1712, 84, "tl") + at(roster(L("hang"), { fire }), 1812, 84, "tl");
}
// the type board (brief 046 round 3): the same HUD pieces, once per font option, each cell carrying its own --ff / --fw / --fws / --fs
// dividers (brief 046 round 5): the same four portraits and states under every divider, at 2x, with what each costs in height
const DIV_STATES = [{ sel: true, pips: 4, weapon: "drawn" }, { busy: "s_watching", pips: 3, weapon: "holstered" }, { watched: true, weapon: "drawn", pips: 2 }, { seen: true, weapon: "holstered", pips: 3 }];
function rosterDividers(tk, cause) {
  const T = (dv) => ({ ...tk, roster: { ...tk.roster, layout: "under", divider: dv } }), fire = { i: 3, cause: "fired" }, x0 = 24, pitch = 196;
  const cols = DIVIDERS.map((D, k) => { const x = x0 + k * pitch, g = divGeom(D.id), H = colH("under", D.id), pct = ((H / 1080) * 100).toFixed(1);
    return cap(`${k + 1}  ${D.name}`, x, 40) + at(roster(T(D.id), { states: DIV_STATES, fire }), x, 62, "tl scale2") + `<div class="costcap at tl" style="left:${x}px;top:${62 + 30 + H * 2 + 8}px;width:${pitch - 20}px">${H.toFixed(1)} px · ${pct} %<br>air ${g.airAbove} above / ${g.airBelow} below</div>`; }).join("");
  const rows = DIVIDERS.map((D, k) => `<tr><td>${k + 1}  ${D.name}</td><td>${colH("under", D.id).toFixed(1)}</td><td>${((colH("under", D.id) / 1080) * 100).toFixed(1)} %</td></tr>`).join("");
  return cols + `<table class="cost at tl" style="left:1420px;top:96px"><tr><th>Divider</th><th>Column px</th><th>% height</th></tr>${rows}</table>` +
    `<div class="costcap at tl" style="left:1420px;top:350px;width:470px;white-space:normal;font-weight:500">Same four portraits and the same icons in every column. Only the mark between the characters changes, and with it the gap (the column is taller by the mark's height). Every mark is drawn from kit pixels or kit.uss colours; none is a new colour.<br><br>${DIVIDERS.map((D, k) => `${k + 1} ${D.note} <i style="opacity:.7">(${D.src})</i>`).join("<br>")}</div>`;
}
function rosterDivStates(tk, cause) {
  const T = (dv) => ({ ...tk, roster: { ...tk.roster, layout: "under", divider: dv } }), x0 = 150, out = [];
  let y = 62;
  DIVIDERS.forEach((D, r) => {
    const t = T(D.id), cellH = Math.round(42 + 35.15 + divGeom(D.id).gap + 12) + (r === 0 ? 21 : 0);
    out.push(cap(`${r + 1}  ${D.name}`, 24, y + 40) + at(rosterStates(t, "under", { keep: ["normal", "selected", "busy", "seen", "watched", "drawn", "fired on", "hurt", "down"], caps: r === 0 }), x0, y, "tl"));
    y += cellH + 8;
  });
  const fire = { i: 3, cause: "fired" };
  const cols = DIVIDERS.map((D, k) => cap(String(k + 1), 1130 + k * 94 + 40, 62 + 15 + colH("under", D.id) + 6) + at(roster(T(D.id), { states: DIV_STATES, fire }), 1130 + k * 94, 62, "tl")).join("");
  return out.join("") + cap("Actual size, 1080p", 1130, 20) + cols;
}
// the roster column art (brief 046 round 6): the kit's column as built beside the three repainted ones, the same four portraits, icons under the portrait, the picked divider; at 2x, and every state at 1x
const CLEAN_SHORT = { kit: "As built", inlay: "A  Inlay", bare: "B  Bare", rivetless: "C  No rivets" };
const CLEAN_NAMES = { kit: "As built", inlay: "A  Inlay under the caps", bare: "B  Bare body", rivetless: "C  Inlay, no rivets" };
function rosterClean(tk) {
  const T = (c) => ({ ...tk, roster: { ...tk.roster, layout: "under", column: c } }), fire = { i: 3, cause: "fired" }, x0 = 24, pitch = 196, dv = tk.roster.divider, H = colH("under", dv);
  const cols = COLUMNS.map((c, k) => { const x = x0 + k * pitch;
    return cap(CLEAN_NAMES[c.id], x, 40) + at(roster(T(c.id), { states: DIV_STATES, fire }), x, 62, "tl scale2") + `<div class="costcap at tl" style="left:${x}px;top:${62 + H * 2 + 22}px;width:${pitch - 12}px">${c.rivets} rivets<br>${c.id === "kit" ? "stripes float" : c.id === "bare" ? "no stripes" : "stripes tuck under the caps"}</div>`; }).join("");
  const actual = COLUMNS.map((c, k) => cap(CLEAN_SHORT[c.id].split("  ")[0] === "As built" ? "As built" : CLEAN_SHORT[c.id][0], 1000 + k * 100 + 30, 62 + 15 + H + 6) + at(roster(T(c.id), { states: DIV_STATES, fire }), 1000 + k * 100, 62, "tl")).join("");
  const rows = COLUMNS.map((c) => `<tr><td>${CLEAN_NAMES[c.id]}</td><td>${c.rivets}</td><td>${c.rivets}</td></tr>`).join("");
  return cols + cap("Actual size, 1080p", 1000, 20) + actual + `<table class="cost at tl" style="left:1430px;top:96px"><tr><th>Column</th><th>Rivets at 2x</th><th>at 1x</th></tr>${rows}</table>` +
    `<div class="costcap at tl" style="left:1430px;top:260px;width:460px;white-space:normal;font-weight:500">Every column is the kit sprite (same size, same ring centres, same cut rows). Divider: ${dividerNameOf(dv)}. The lime ring at the window edge and the magenta fringe on the outline are gone in A, B and C.<br><br>${COLUMNS.map((c) => `<b>${CLEAN_NAMES[c.id]}</b>: ${c.note}`).join("<br><br>")}</div>`;
}
const dividerNameOf = (id) => (DIVIDERS.find((d) => d.id === id) || {}).name || id;
function rosterCleanStates(tk) {
  const T = (c) => ({ ...tk, roster: { ...tk.roster, layout: "under", column: c } }), x0 = 150, out = [], keep = ["normal", "selected", "busy", "seen", "watched", "drawn", "fired on", "hurt", "down"];
  let y = 62; const dv = tk.roster.divider;
  COLUMNS.forEach((c, r) => { const cellH = Math.round(42 + 35.15 + divGeom(dv).gap + 12) + (r === 0 ? 21 : 0);
    out.push(cap(CLEAN_SHORT[c.id], 24, y + 40) + at(rosterStates(T(c.id), "under", { keep, caps: r === 0 }), x0, y, "tl")); y += cellH + 8; });
  return out.join("");
}
function fontsBoard(tk) {
  const row = (...h) => `<div class="frow">${h.join("")}</div>`;
  const offer = { head: "Hold", text: "Help Inez · $40k · 64 s · +1 Trail", kind: "talk", icon: "help", faces: ["fixer"], st: "offer", live: true, cmp: "pill-offer" };
  const fire = { text: "Firing at an officer · down in 2 s", kind: "harm", icon: "fire", faces: ["enforcer"], st: "fire", cmp: "pill-fire" };
  const cell = (f, x, y) => `<div class="fcell hudwrap" data-font="${f.id}" style="left:${x}px;top:${y}px;--ff:${f.css};--fw:${f.body};--fws:${f.strong};--fs:${f.scale}">
    <b class="rcap">${f.name} · ${f.body} / ${f.strong} · x${f.scale}</b>
    ${row(clockBar("1:38", "3:10", "$100,000", "0", 1))}
    ${row(pill(offer, tk))}
    ${row(pill(fire, tk))}
    ${row(chip(listen), `<span class="fcol">${marker({ icon: "eye_shut", num: "174", left: 174, total: 177 })}${marker({ icon: "eye_shut", num: "9", left: 9, total: 177, low: true })}</span>`)}
    ${row(feedCard(), goalLine())}
    ${row(pop({ icon: "p_time", text: "0123456789 · 12 s · $40k" }), doneStrip("done"), squadPlate())}</div>`;
  return FONTS.map((f, k) => cell(f, 24 + (k % 3) * 636, 20 + Math.floor(k / 3) * 536)).join("");
}
export function sceneHTML(sc, tk, ground, cause = "drawn", v = {}) {
  const bg = sc.board ? ground : sc.bg;
  const hud = sc.fonts ? `<div class="hudlayer"></div>` : sc.noHud ? `<div class="hudlayer">${clockBar("1:38", "3:10", "$100,000", "0", 0)}</div>` : `<div class="hudlayer">${sc.noRoster ? "" : roster(tk, { fire: sc.fire || sc.board ? { i: 0, cause } : null })}${sc.noRoster ? "" : doneStrip(sc.status || "done")}${sc.noRoster ? "" : squadPlate()}${clockBar(sc.id === "call-window" ? "4:20" : "1:38", "3:10", "$100,000", "0", 0)}${miniPlate()}${minimap()}${feedCard()}${goalLine()}${(sc.board && !sc.roster) || sc.id === "listen-chip" ? intel() : ""}</div>`;
  return `<img class="bg${sc.board || sc.blurBg ? " blur" : ""}" src="/bg/${bg}.jpg">${hud}${sc.fonts ? fontsBoard(tk) : sc.cleanBoard ? rosterClean(tk) : sc.cleanStates ? rosterCleanStates(tk) : sc.div2x ? rosterDividers(tk, cause) : sc.divstates ? rosterDivStates(tk, cause) : sc.under ? rosterUnder(tk, cause) : sc.nonum ? rosterNoNum(tk, cause) : sc.roster ? rosterBoard(tk, cause) : sc.board ? board(tk, cause) : sc.items(tk, cause, v).join("")}`;
}
