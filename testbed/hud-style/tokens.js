// HUD style tokens and presets (brief 046). Every colour here has a source in kit.uss, the kit sprites or the style contract:
// the `src` map names it. The export is this object (schema suborned.hud.tokens/1); Latch loads it.
export const SCHEMA = "suborned.hud.tokens/1";

// the kit's own colours (kit.uss :root, contracts/style palette) -- the only swatches the lab offers
export const KIT = {
  ink: ["#17302f", "kit.uss --ink"], ink2: ["#244442", "kit.uss --ink-2"], inkDeep: ["#0b1615", "kit.uss --ink-deep"],
  cream: ["#f7f4ec", "kit.uss --cream"], ivory: ["#eee9da", "kit.uss --ivory"], deepTeal: ["#1f6c67", "kit.uss --deep-teal"],
  seafoam: ["#72b5a7", "kit.uss --seafoam"], paleSeafoam: ["#9bcfc2", "kit.uss --pale-seafoam"], footer: ["#bae2d6", "kit.uss --footer-result"],
  chrome: ["#cbc6c3", "kit.uss --chrome"], brass: ["#99784d", "kit.uss --brass"], turquoise: ["#1cb9c6", "kit.uss --turquoise"],
  turquoiseLight: ["#60e0ea", "kit.uss --turquoise-light"], dim: ["#b0cdc7", "kit.uss --text-dim"], warmInk: ["#46524e", "kit.uss --text-warm-ink"],
  amber: ["#f0a020", "style contract signal_colours.guard"], bodyTeal: ["#0c3434", "pill_status body (sampled)"], white: ["#ffffff", "kit.uss --white"], pipEmpty: ["#142c2c", "kit.uss --pip-empty"],
};
export const kc = (k) => KIT[k][0];

// the 0.2.56 control-lab colours (history, "As built" only; never offered as swatches)
export const LAB_KIND = { look: "#4aa8ff", talk: "#4cc35f", tech: "#1fd1b5", take: "#f1ead8", harm: "#ff4d7d" };

// HUD type (Sky, 2026-10-22: "I don't like our font - it's too bold, needs to be a lot cleaner"). Six options, same HUD, same sizes. Static instances of OFL fonts with the digits already tabular
// (Unity's text engine has no tnum switch): fonts/<File>.ttf, built by fonts/build-instances.py.txt from the Google Fonts variable files. `scale` matches the x-height of Jost so a size means the same on every option.
export const FONTS = [
  { id: "jostBold", name: "Jost Bold (as built)", family: "Jost", css: "Jost", body: 700, strong: 700, scale: 1, files: { body: "Jost-Bold.ttf", strong: "Jost-Bold.ttf" }, licence: "SIL OFL 1.1 (Jost, Indestructible Type 2020)", source: "the kit's own fonts/Jost-Bold.ttf", tabular: "proportional digits (tabular-nums only where the lab asked)", note: "What Sky played." },
  { id: "jostMedium", name: "Jost Medium", family: "Jost", css: "JostT", body: 500, strong: 600, scale: 1, files: { body: "Jost-Medium-Tab.ttf", strong: "Jost-SemiBold-Tab.ttf" }, licence: "SIL OFL 1.1 (Jost, Indestructible Type 2020; no reserved name)", source: "https://github.com/google/fonts/tree/main/ofl/jost", tabular: "tabular digits baked in", note: "Same family, two weights lighter: nothing else changes in the look." },
  { id: "jostRegular", name: "Jost Regular", family: "Jost", css: "JostT", body: 400, strong: 500, scale: 1, files: { body: "Jost-Regular-Tab.ttf", strong: "Jost-Medium-Tab.ttf" }, licence: "SIL OFL 1.1 (Jost, Indestructible Type 2020; no reserved name)", source: "https://github.com/google/fonts/tree/main/ofl/jost", tabular: "tabular digits baked in", note: "The lightest: reads clean on cream, thin on a dark frame at 12 px." },
  { id: "outfit", name: "Outfit Medium", family: "Outfit", css: "OutfitT", body: 500, strong: 600, scale: 0.96, files: { body: "Outfit-Medium-Tab.ttf", strong: "Outfit-SemiBold-Tab.ttf" }, licence: "SIL OFL 1.1 (Outfit Project Authors 2021; no reserved name)", source: "https://github.com/google/fonts/tree/main/ofl/outfit", tabular: "tabular digits baked in", note: "Geometric, a little rounder than Jost: the closest cousin." },
  { id: "figtree", name: "Figtree Medium", family: "Figtree", css: "FigtreeT", body: 500, strong: 600, scale: 0.93, files: { body: "Figtree-Medium-Tab.ttf", strong: "Figtree-SemiBold-Tab.ttf" }, licence: "SIL OFL 1.1 (Figtree Project Authors 2022; no reserved name)", source: "https://github.com/google/fonts/tree/main/ofl/figtree", tabular: "tabular digits baked in", note: "Clean friendly grotesk with a tall x-height: the most legible small." },
  { id: "manrope", name: "Manrope Medium", family: "Manrope", css: "ManropeT", body: 500, strong: 600, scale: 0.87, files: { body: "Manrope-Medium-Tab.ttf", strong: "Manrope-SemiBold-Tab.ttf" }, licence: "SIL OFL 1.1 (Manrope Project Authors 2018; no reserved name)", source: "https://github.com/google/fonts/tree/main/ofl/manrope", tabular: "tabular digits baked in", note: "Modern grotesk, wide and even; less retro." },
];
export const fontById = (id) => FONTS.find((f) => f.id === id) || FONTS[1];
// the HUD's text roles and their nominal size in px at 1080p (the sizes the CSS and the kit's .uss use); the effective size on an option is nominal x scale, rounded to 0.5
export const FONT_ROLES = { pill: 15.5, chip: 15, counter: 14.5, clock: 20, tag: 13.5, pop: 13.5, feedHead: 13.5, goal: 13, unlock: 12.5, status: 12.5, squad: 12, intelHead: 11.5, number: 10.5 };
const clone = (o) => JSON.parse(JSON.stringify(o));
const BASE = {
  schema: SCHEMA, id: "", name: "", note: "",
  frame: "kit", // "kit" = nine-slice kit sprites; "flat" = the 0.2.56 control-lab overlays
  body: { hud: null, overlay: null, gain: 1.6 }, // body colour of the frame sprites; null = the kit's own deep teal
  type: { main: kc("cream"), dim: kc("dim"), head: kc("dim"), card: kc("ink") },
  accent: { live: kc("turquoiseLight"), warn: kc("amber"), hair: kc("brass"), tail: kc("bodyTeal") },
  disc: { family: "dark", color: null, icon: kc("cream"), size: 26, iconSize: 15, ring: true },
  face: { size: 24 },
  rim: { shadow: 1 }, // drop shadow strength under frames, 0-2
  // "why am I being shot at": one icon for the strongest cause (on the portrait with its pulse ring, and over the agent), and a direction cue on the ground.
  // The ground cue is a decal in the ground plane, hidden behind the agent (brief 046, Sky 2026-10-22: the three stacked chevrons were "horrible", "a lot more subtle").
  why: { place: "both", disc: "light", size: 26, iconRatio: 0.6, ring: kc("cream"), pulseMs: 900,
    arrow: { style: "arc", color: kc("cream"), edge: kc("inkDeep"), alpha: 0.85, weight: 1, cue: "head" }, // style: none | asis | arc | notch | streak | ripple; cue = where the cause disc sits: head | arc | portrait
    icons: { drawn: "fire", fired: "bang", carrying: "s_carrying", violence: "takedown", known: "papers", seen: "sawyou" } },
  // under (icons in a slim row under each portrait, column taller) | hang (the row on a small plate hanging under the ring) | railLite (side rail, no numbers) | rail | inside | asbuilt: where the badges sit (brief 046, Roster slots)
  // weapon: ONE pictogram (the kit's `fire` pistol) in ONE slot with two states, drawn (light disc, ink gun) and holstered (dark disc, the same gun at 55 %); nothing else on a portrait is a gun.
  font: { id: "jostMedium" }, // see FONTS: family, weights (body, strong) and a size scale; the export carries the resolved family, files and sizes
  roster: { layout: "under", divider: "steel", column: "rivetless", weapon: { icon: "fire", drawn: { disc: "light", ink: true, alpha: 1 }, holstered: { disc: "dark", ink: false, alpha: 0.55 }, causeDrawn: "when the strongest cause is 'drawn' the weapon disc itself carries it (the kit's selected light disc, turquoise ring); no second gun" } },
  hold: { anim: "fill", ease: "linear", ms: 1200, release: "snap", cancel: "drain", fill: kc("turquoiseLight"), alpha: 0.4, segments: 10 },
};
const P = (id, name, note, patch) => { const o = clone(BASE); o.id = id; o.name = name; o.note = note; for (const k of Object.keys(patch)) o[k] = typeof patch[k] === "object" && !Array.isArray(patch[k]) && o[k] && typeof o[k] === "object" ? { ...o[k], ...patch[k] } : patch[k]; return o; };

const creamType = { main: kc("ink"), dim: kc("warmInk"), head: kc("warmInk"), card: kc("ink") };
export const PRESETS = [
  P("built", "As built 0.2.56", "What Sky played: kit HUD in deep teal; the pill, chip, marker and tag are the control lab's flat web overlays (bright kind colours, white 2 px border); the hold is a white 55 % bar.", {
    frame: "flat", font: { id: "jostBold" }, roster: { layout: "asbuilt", column: "kit" }, why: { place: "none" }, hold: { anim: "fill", ease: "linear", ms: 1200, release: "snap", cancel: "snap", fill: "#ffffff", alpha: 0.55, segments: 10 }, disc: { family: "dark", icon: kc("cream"), size: 26, iconSize: 15, ring: true } }),
  P("revisedA", "Revised A", "Brief 044 revised A: kit sprites, chrome rim, deep-teal body, cream type, turquoise-light for the live thing, discs the size of the cameo. No kind colours. Jost Regular (Sky, round 4).", { font: { id: "jostRegular" } }),
  P("creamPlate", "Cream plate", "Cream body on every frame (the kit's panel_card / plate_light colour), chrome rim kept, ink type, deep teal for the live thing; the kind disc is the kit's light disc with an ink icon. Hold lights the rim.", {
    body: { hud: kc("cream"), overlay: kc("cream") }, type: creamType,
    accent: { live: kc("deepTeal"), warn: kc("amber"), hair: kc("brass"), tail: kc("cream") },
    disc: { family: "light", color: null, icon: kc("ink"), size: 28, iconSize: 17, ring: true },
    why: { disc: "light", ring: kc("deepTeal") },
    hold: { anim: "rim", ease: "ease-in-out", ms: 1100, release: "ripple", cancel: "drain", fill: kc("deepTeal"), alpha: 0.9, segments: 10 } }),
  P("seafoamEnamel", "Seafoam enamel", "The civic block's seafoam band as the body (footer-result #bae2d6), ink type, deep-teal discs with cream icons. Hold fills in ticks, like the pips.", {
    body: { hud: kc("footer"), overlay: kc("footer") }, type: creamType,
    accent: { live: kc("deepTeal"), warn: kc("amber"), hair: kc("brass"), tail: kc("footer") },
    disc: { family: "dark", color: kc("deepTeal"), icon: kc("cream"), size: 28, iconSize: 17, ring: true },
    why: { disc: "dark", ring: kc("deepTeal") },
    hold: { anim: "ticks", ease: "ease-out", ms: 1100, release: "snap", cancel: "drain", fill: kc("deepTeal"), alpha: 0.85, segments: 12 } }),
  P("creamTealDiscs", "Cream with teal discs", "Cream plates (as Cream plate) but the discs and the dark HUD frames stay the kit's deep teal: the clean look of the signage ends with the strength of the teal. Hold runs round the disc.", {
    body: { hud: null, overlay: kc("cream") }, type: { main: kc("ink"), dim: kc("warmInk"), head: kc("warmInk"), card: kc("ink") },
    accent: { live: kc("turquoise"), warn: kc("amber"), hair: kc("brass"), tail: kc("cream") },
    disc: { family: "dark", color: null, icon: kc("cream"), size: 30, iconSize: 18, ring: true },
    why: { disc: "dark", ring: kc("ivory") },
    hold: { anim: "disc", ease: "ease-in-out", ms: 1100, release: "ripple", cancel: "drain", fill: kc("turquoise"), alpha: 0.9, segments: 10 } }),
  P("inkIvory", "Ink and ivory", "Safe dark fallback with no cyan: the kit's ink (greener than the deep teal) as body, ivory type and ivory live accent, brass hairline. Hold wipes the words ivory to white.", {
    body: { hud: kc("ink"), overlay: kc("ink") }, type: { main: kc("cream"), dim: kc("dim"), head: kc("dim"), card: kc("ink") },
    accent: { live: kc("ivory"), warn: kc("amber"), hair: kc("brass"), tail: kc("ink") },
    disc: { family: "dark", color: kc("ink2"), icon: kc("cream"), size: 28, iconSize: 17, ring: true },
    why: { disc: "light", ring: kc("ivory") },
    hold: { anim: "wipe", ease: "ease-in", ms: 1000, release: "flash", cancel: "snap", fill: kc("ivory"), alpha: 0.9, segments: 10 } }),
];
export const WHY = { drawn: "Weapon drawn in sight", fired: "Fired on them", carrying: "Carrying Vale", violence: "Violence seen", known: "Known face", seen: "Seen (rivals)" };
export const presetById = (id) => clone(PRESETS.find((p) => p.id === id));
export const ANIMS = [
  ["fill", "Fill sweep (inside the window)"], ["rim", "Rim fill (the edge lights up)"], ["ticks", "Segmented ticks"], ["disc", "Radial on the disc"],
  ["line", "Underline"], ["wipe", "Word wipe"], ["pulse", "Charge glow"],
];
export const EASES = ["linear", "ease-in", "ease-out", "ease-in-out", "snap-end"];
export const RELEASES = [["snap", "Snap (scale + ring)"], ["ripple", "Ripple ring"], ["flash", "Flash wash"], ["none", "None"]];
export const CANCELS = [["drain", "Drain back"], ["snap", "Snap to zero"]];
export const BODIES = [["Kit teal", null], ["Cream", kc("cream")], ["Ivory", kc("ivory")], ["Seafoam", kc("footer")], ["Pale seafoam", kc("paleSeafoam")], ["Ink", kc("ink")], ["Deep teal", kc("deepTeal")]];
export const ease = (name, t) => {
  t = Math.max(0, Math.min(1, t));
  if (name === "ease-in") return t * t * t;
  if (name === "ease-out") return 1 - Math.pow(1 - t, 3);
  if (name === "ease-in-out") return t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
  if (name === "snap-end") return t < 0.8 ? t * 0.7 / 0.8 : 0.7 + (t - 0.8) * 1.5; // slow, then the last fifth rushes
  return t;
};

// The ground cue toward the shooter (brief 046). Drawn as a decal in the ground plane (flattened by sin of the camera pitch, 38 deg = 0.616),
// hidden behind anything standing in front of it. Units: the agent's ground ring has radius 100; angles are ground-plane degrees toward the shooter
// (0 = screen right, 90 = away from the camera). All of it is cream with an ink-deep edge (kit colours; no red or amber), alpha 0.85 at most, and never more than one mark.
export const FLATTEN = 0.616;
export const ARROWS = [
  ["none", "None"],
  ["asis", "As built: three stacked chevrons (the problem)"],
  ["arc", "A  Arc on the ring"],
  ["notch", "B  Notch on the ring"],
  ["streak", "C  Streak in"],
  ["ripple", "D  Ripple"],
];
export const ARROW_SPEC = {
  ring: { radius: 100, note: "the selected ring's outer white line" },
  arc: { desc: "A thin arc just outside the ring, centred on the shooter; breathes between 55 and 100 % opacity.", r: 118, spanDeg: 70, width: 10, edgeWidth: 17, edgeAlpha: 0.5, alphaLow: 0.6, alphaHigh: 1 },
  notch: { desc: "One small wedge on the ring, tip toward the shooter; drifts 8 units outward and back with the pulse.", rBase: 108, rTip: 140, halfBase: 17, drift: 8, edgeWidth: 7, alphaLow: 0.65, alphaHigh: 1 },
  streak: { desc: "A short faint streak that travels in along the line to the shooter and fades before the ring; a quiet tracer.", rFrom: 190, rTo: 132, length: 60, width: 11, edgeWidth: 18, edgeAlpha: 0.45, gapMs: 400, alpha: 0.95 },
  ripple: { desc: "A brief arc pulse that leaves the ring toward the shooter and fades; nothing stays on the ground between pulses.", r0: 112, r1: 160, spanDeg: 50, width: 11, edgeWidth: 18, edgeAlpha: 0.45, ms: 700, gapMs: 600 },
  asis: { desc: "0.2.56: three cream chevrons, 24 x 30 px each, stacked in a row, flattened (for comparison only)." },
};

// Target rings (brief 049; Sky, 2026-10-22: "rings should be red when they're firing at you, and with crosshairs also when involved at being aimed or shooting").
// Ground decals on the signal layer (unlit, emissive, outside the post grade), drawn under the figure; widths in metres on the ground, flattened like the cell's own ring.
// Data: the hostile's state comes from the under-fire read (Game.FireWhyOf: who aims or fires at whom), ours from the cell's aim / fire orders; the in / long / out state from FireLine.Band.
export const TARGET_RINGS = {
  schema: "suborned.hud.targetrings/1",
  note: "Colour says whose (side colour) or that it is a threat to us (red); shape says reach (solid / thin / dashed); the four ticks say an aimed shot is in the exchange. Nothing is drawn for a civilian, car or camera until the cursor is on it (hover), or the cell aims at it.",
  metresPerPx: 1 / 38.6, flatten: FLATTEN,
  colours: { cell: [kc("turquoiseLight"), "HostViews.CellRings (turquoise-light)"], guard: [kc("amber"), "style contract signal_colours.guard"], rivals: ["#8a4fd0", "style contract signal_colours.rivals"], civilian: [kc("cream"), "kit.uss --cream"], threat: ["#e03a2f", "style contract signal_colours.cones (free to use; told apart from a cone by shape and motion, below)"], tick: [kc("cream"), "kit.uss --cream"], edge: [kc("inkDeep"), "kit.uss --ink-deep"] },
  states: {
    in:    { ring: { radiusM: 0.54, lineM: 0.10, colour: "side", alpha: 1 }, ticks: false, when: "an armed, selected agent has a line and the target is inside the effective ring (FireLine.Band point / eff); combatants only" },
    long:  { ring: { radiusM: 0.54, lineM: 0.075, colour: "side", alpha: 1 }, ticks: false, when: "inside the max ring only" },
    out:   { ring: { radiusM: 0.54, lineM: 0.065, colour: "side", alpha: 0.9, dashed: { segments: 22 } }, ticks: false, when: "beyond the max ring" },
    hover: { ring: { radiusM: 0.68, lineM: 0.12, colour: "side", alpha: 1 }, ticks: false, when: "the cursor is on it (the pill prices it); the only way a civilian, car or camera gets a ring" },
    ours:  { ring: "as in / long / out / hover, side colour kept", ticks: { still: true, colour: "tick" }, when: "one of the cell is aiming or firing at it (the order is in its aim or fire phase)" },
    aim:   { ring: { radiusM: 0.54, lineM: 0.12, colour: "threat", alpha: 1, dashed: false }, ticks: { colour: "tick", motion: "closing" }, when: "it is aiming at one of the cell (FireWhyOf names a target and it has not shot yet); any reach state, a dashed ring turns solid" },
    fire:  { ring: { radiusM: 0.54, lineM: 0.12, colour: "threat", alpha: 1, dashed: false }, ticks: { colour: "tick", motion: "kick" }, pulse: { colour: "threat", lineM: 0.07, scaleTo: 1.8, ms: 300, alpha: [0.9, 0] }, when: "it is firing at one of the cell: a shot in the last 1 s" },
  },
  ticks: { count: 4, atDeg: [0, 90, 180, 270], inner: "ring + 0.07 m", outer: "ring + 0.22 m", lineM: 0.075, edgeM: 0.13, edgeAlpha: 0.6, note: "square to the screen in the ground plane; the same crosshair band ticks as the fights' (GDD §10)" },
  motion: {
    aim: { loopMs: 1000, ticks: "start 0.30 m further out at alpha 0.5, close to rest by 78 % with ease-in, hold; loops while it aims (the aim windup is 1 s: the ticks lock when it is ready)" },
    fire: { loopMs: 1000, ticks: "kick 0.07 m in at the shot and ease back out over 30 %", pulse: "one thin ring leaves the ring at the shot: scale 1 to 1.8 over 300 ms, alpha 0.9 to 0; no pulse between shots" },
  },
  priority: "fire > aim > ours > hover > in > long > out (one state at a time; a duel is red: the threat wins and the cell's own aim is still the pill and the fire line)",
  notACone: { cone: "a flat translucent wedge (fill 38 %, rim 0.085 m) metres long, only the learned cone or Alt, no ticks, sweeps slowly", ring: "a closed outline 1.1 m across at the feet with four ticks, never filled, closes and kicks (above)", note: "same hue as the cones, told apart by shape and motion (GDD §10); red is free (Sky, 2026-10-16)" },
  underFireArc: { decision: "keep the arc (why.arrow, style arc, weight 1) as the default", why: "The red ring names who is shooting, not whom: with two or more agents selected the arc on the agent, the portrait pulse ring and the cause disc are the pairing. At the cell's real ring (r 0.42 m, 16 px) the arc is a 1.6 px hairline that hides behind the legs on a diagonal and reads weakly on pale paving: it is a quiet second cue, never the first. Weight 1.6 to 2 is a one-number change if Sky wants it louder (`why.arrow.weight`).", pictures: "briefs/049/arc.png" },
  occludedByFigure: true,
};

// The taller roster column (brief 046, round 3). Nothing is redrawn: the kit's own roster_column sprite (218 x 833 @2x, drawn at 0.395) is cut at the clean body row in each gap
// (rows 223, 412, 601, 789: no chrome, no screw, in the dark body between two rings) and the one body row at that gap (222) is repeated d2x times, four times (three gaps and the foot).
// Nine-slice by rows: the rings, rims, screws, caps and seafoam stripes are the sprite's own pixels; only the plain body between two rings is repeated.
export const COL = { cuts: [223, 412, 601, 789], row: 222, body: [36, 182], w: 218, h: 833, drawnW: 86.11, drawnH: 328.64 }; // body: the x range of the plain body between the seafoam stripes (smoothed sideways in the repeated row)
export const TALL = { hang: { d2x: 41 } }; // @2x px added per gap (the `under` column's gap depends on its divider: see DIVIDERS)
const K2X = COL.drawnH / COL.h; // 1080p px per @2x px of the repeated row
for (const k of Object.keys(TALL)) TALL[k].d = TALL[k].d2x * K2X; // 1080p px added per gap: hang 16.2

// Dividers between the characters of the "under" column (brief 046 round 5; Sky: "try with a divider between characters perhaps?").
// Every divider lives in the gap between one portrait's icon row and the next portrait, from kit sources only. The gap = 3.2 px (the sprite's own) + d (the repeated body rows).
// Geometry in px at 1080p, relative to the ring centre: the gap starts GAP_TOP below it; a1 = air above the icon row (the row hugs its own portrait: proximity says whose it is),
// a2 = air between the row and the mark, h = the mark's height, a3 = air between the mark and the next portrait (derived from d2x, the whole @2x pixel count of the repeated row).
export const GAP0 = 3.2, GAP_TOP = 34.3, ROW_H = 16, BODY = [6.7, 79.4]; // the sprite's own gap: ring bottom 34.3 below one centre, the next ring's top 36.7 above its centre, pitch 74.2: 3.2 px
export const DIVIDERS = [
  { id: "none", name: "None (as now)", note: "No mark; the row is centred in the gap (what Sky played).", centred: true, d2x: 53, a1: 3.35, a2: 0, h: 0, w: 0, src: "the column sprite only" },
  { id: "space", name: "Spacing only", note: "No mark; the row hugs its portrait and the air under it is the divider.", a1: 2.5, a2: 0, h: 0, w: 0, a3: 7, src: "the column sprite only" },
  { id: "hair", name: "Brass hairline", note: "A 1 px brass line, 58 px wide, fading at both ends.", a1: 2.5, a2: 2.5, h: 1, w: 58, a3: 3, src: "kit.uss --brass #99784d (a hairline only, as the HUD already uses it)" },
  { id: "band", name: "Seafoam band", note: "A 3 px seafoam bar rung from stripe to stripe, the column's own side stripes carried across.", a1: 2.5, a2: 2.5, h: 3, w: 72, a3: 3, src: "kit.uss --seafoam #72b5a7" },
  { id: "steel", name: "Steel line", note: "Sky's pick (divider 5 without its screws): a 1 px chrome line at 45 %, 54 px wide, centred; no rivets.", a1: 2.5, a2: 2.5, h: 1, w: 54, a3: 3, src: "kit.uss --chrome #cbc6c3 at 45 % (the line the screw strip drew)" },
  { id: "notch", name: "Chrome rule, notch", note: "A 2 px chrome rule with a small chrome tail (the chip's tail shape, 9 x 4.5) hanging at the centre.", a1: 2.5, a2: 2.5, h: 6.5, w: 62, a3: 2, src: "kit.uss --chrome #cbc6c3, tail body #0c3434 as on the pills" },
  { id: "well", name: "Recessed well", note: "The icon row sits in a darker well (the pips' track colour) the width of the body; the air under the well is the divider.", a1: 4.5, a2: 0, h: 21, w: 70, a3: 4, wellTop: 2, src: "kit.uss --pip-empty #142c2c" },
];
// The roster column art (brief 046 round 6). `kit` = the kit's own sprite as built (16 rivets, stripes that end in the body); the others are repainted from it
// by prep/roster_column.py (same size and ring centres, same clean cut rows, so every number above holds). Rivets = per column at 2x (it is the same count at 1x).
export const COLUMNS = [
  { id: "kit", name: "As built (kit sprite)", url: null, rivets: 16, note: "16 rivets (8 on the bezels, 2 each in gaps 1 and 2, none in gap 3, 4 at the outer corners); side stripes start and end inside the body, not joined to the caps; lime and magenta key spill on the window edges." },
  { id: "inlay", name: "Inlay under the caps", url: "/sprites/roster_column_inlay@2x.png", rivets: 4, note: "4 rivets (the outer corners); the side stripes run the full height and tuck under both caps." },
  { id: "bare", name: "Bare body", url: "/sprites/roster_column_bare@2x.png", rivets: 4, note: "4 rivets (the outer corners); no side stripes: the cap tabs are the only seafoam." },
  { id: "rivetless", name: "Inlay, no rivets", url: "/sprites/roster_column_rivetless@2x.png", rivets: 0, note: "No rivets at all; the side stripes run the full height and tuck under both caps." },
];
export const columnById = (id) => COLUMNS.find((c) => c.id === id) || COLUMNS[0];
export const dividerById = (id) => DIVIDERS.find((x) => x.id === id) || DIVIDERS[0];
// the numbers a divider implies: the whole @2x pixel count of the repeated row (so the sprite is cut on a pixel), the added height per gap, where the icon row and the mark sit
export function divGeom(id) {
  const D = dividerById(id), want = D.centred ? D.d2x : Math.round(((D.id === "well" ? D.wellTop + D.h + D.a3 : D.a1 + ROW_H + D.a2 + D.h + D.a3) - GAP0) / K2X), d2x = want, d = d2x * K2X, gap = GAP0 + d;
  const rowDy = D.centred ? GAP_TOP + (gap - ROW_H) / 2 : GAP_TOP + D.a1, markDy = D.id === "well" ? GAP_TOP + D.wellTop : GAP_TOP + D.a1 + ROW_H + D.a2;
  const a3 = D.centred ? 0 : gap - (markDy - GAP_TOP) - D.h;
  return { id: D.id, d2x, d: +d.toFixed(3), gap: +gap.toFixed(2), rowDy: +rowDy.toFixed(2), mark: D.h ? { dy: +markDy.toFixed(2), h: D.h, w: D.w, x: +(42.98 - D.w / 2).toFixed(2) } : null, airAbove: +(rowDy - GAP_TOP).toFixed(2), airBelow: +(D.id === "well" ? gap - D.wellTop - D.h : D.centred ? (gap - ROW_H) / 2 : gap - D.a1 - ROW_H).toFixed(2), a3: +a3.toFixed(2) };
}
// the tall column's cut for a layout: under follows its divider, hang is fixed
export const tallFor = (lay, dv) => lay === "under" ? { d2x: divGeom(dv).d2x, d: divGeom(dv).d } : TALL[lay];
