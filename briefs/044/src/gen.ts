// Brief 044: overlay mockups. Three directions (A chrome plate, B station sign, C orb and label) drawn over the cleaned 0.2.56 shots.
// Words are exactly as built; only the look changes. Run: bun briefs/044/src/gen.ts [dir ...]  (renders PNGs with headless Chrome)
import { mkdirSync, writeFileSync, existsSync } from "node:fs";
import { spawnSync } from "node:child_process";

const ROOT = "/Users/sky/Projects/suborned";
const KIT = `file://${ROOT}/unity/Assets/Suborned/UI/Resources/Kit`;
const SRC = `${ROOT}/briefs/044/src`;
const OUT = `${ROOT}/briefs/044`;
const CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";

// ---- the kit's colours (kit.uss :root) and the enamel accents of the city
const C = {
  ink: "#17302f", ink2: "#244442", deep: "#0b1615", cream: "#f7f4ec", ivory: "#eee9da", deepTeal: "#1f6c67", seafoam: "#72b5a7",
  pale: "#9bcfc2", chrome: "#cbc6c3", brass: "#99784d", turq: "#1cb9c6", turqL: "#60e0ea", dim: "#b0cdc7", line: "#d2e6e2",
};
// kind enamel: [fill, icon ink]
const KIND: Record<string, [string, string]> = {
  look: ["#2f6fd6", C.cream], talk: ["#4fc58c", C.deep], tech: ["#1cb9c6", C.deep], take: [C.ivory, C.deep], harm: ["#e4533d", C.cream],
};
const ROLE: Record<string, string> = { scout: "#8fd3c8", fixer: "#f2b48a", tech: "#9db7ff", enforcer: "#e6dfcc" };

const ic = (name: string, px: number, col: string, extra = "") =>
  `<i class="ic" style="width:${px}px;height:${px}px;background:${col};-webkit-mask-image:url(${KIT}/icons/singles/${name}@2x.png);${extra}"></i>`;
const cameo = (role: string, px: number, ring = 2, extra = "") =>
  `<span class="cam" style="width:${px}px;height:${px}px;background-image:url(${KIT}/portraits/cameo_${role}_normal@2x.png);box-shadow:0 0 0 ${ring}px ${ROLE[role]},0 0 0 ${ring + 1}px ${C.deep};${extra}"></span>`;
// nine-slice of a kit sprite: slice in @2x px [l,t,r,b], drawn at scale s (the game's -unity-slice-scale)
function nine(file: string, sl: [number, number, number, number], s: number, extra = "") {
  const [l, t, r, b] = sl;
  return `<div class="fr" style="border-width:${t * s}px ${r * s}px ${b * s}px ${l * s}px;border-image:url(${KIT}/${file}@2x.png) ${t} ${r} ${b} ${l} fill / ${t * s}px ${r * s}px ${b * s}px ${l * s}px stretch;${extra}"></div>`;
}

const CSS = `
@font-face{font-family:Jost;font-weight:700;src:url(${KIT}/fonts/Jost-Bold.ttf)}
@font-face{font-family:Jost;font-weight:600;src:url(${KIT}/fonts/Jost-SemiBold.ttf)}
@font-face{font-family:Barlow;font-weight:500;src:url(${KIT}/fonts/Barlow-Medium.ttf)}
*{box-sizing:border-box;margin:0;padding:0}
html,body{width:1920px;height:1080px;overflow:hidden;background:#000}
.bg{position:absolute;left:0;top:0;width:1920px;height:1080px}
.ic{display:inline-block;flex:none;-webkit-mask-size:contain;-webkit-mask-repeat:no-repeat;-webkit-mask-position:center}
.cam{display:inline-block;flex:none;border-radius:50%;background-size:cover;background-position:center;background-color:#244442}
.fr{position:absolute;inset:0;border-style:solid;pointer-events:none}
.abs{position:relative;display:inline-flex;align-items:center;font-family:Jost,sans-serif;font-weight:700;color:${C.cream};white-space:nowrap;line-height:1}
.in{position:relative;display:flex;align-items:center;white-space:nowrap}
.dim{color:${C.dim}}
`;

// ---------------------------------------------------------------- the data every direction draws (the words as built)
type PillSt = "offer" | "hold" | "ready" | "order" | "fire";
interface Pill { head?: string; text: string; kind: keyof typeof KIND; icon: string; faces: string[]; st: PillSt; fill?: number; seen?: boolean }
interface Chip { face: string; mark: "ok" | "soft" | "no"; kind: keyof typeof KIND; lines: [string, string][]; unl?: { icon: string; word: string; shut?: boolean }[] }
interface Marker { icon: string; num?: string; face?: string; left?: number; total?: number; low?: boolean; kind: keyof typeof KIND }
interface Pop { icon: string; text: string; kind?: "ok" | "warn" }

interface Dir {
  id: string; name: string; mh: number; // mh: the marker's height, so the chip above it clears it

  pill(p: Pill): { html: string; w: number; h: number; tipDy: number }; // box html positioned by the caller (left/top of the box); tipDy: from the box bottom to the target
  chip(c: Chip): string;
  marker(m: Marker): string;
  tag(text: string): string;
  pop(p: Pop): string;
}

// ================================================================= A · chrome plate, revised 2026-10-22 (no kind colours: every colour is a kit sprite or kit.uss / contract HUD colour, see the audit in briefs/044-overlay-style.md)
// the kit's own pieces: disc_dark_24 / disc_light_24 (the kind disc), stripes_warn (the tile), mark_ok / mark_half / mark_no (the outcome badge)
const tile = (w: number, h: number) => `<span style="flex:none;width:${w}px;height:${h}px;background:url(${KIT}/overlays/stripes_warn@2x.png) 0 0/22px 15px repeat;border-radius:2px"></span>`;
const kdisc = (icon: string, light: boolean) => `<span style="position:relative;width:26px;height:26px;flex:none;background:url(${KIT}/markers/${light ? "disc_light_24" : "disc_dark_24"}@2x.png) center/26px 26px no-repeat;display:flex;align-items:center;justify-content:center">${ic(icon, 15, light ? C.ink : C.cream)}</span>`;
const cameoK = (role: string, px: number, extra = "") => `<span class="cam" style="width:${px}px;height:${px}px;background-image:url(${KIT}/portraits/cameo_${role}_normal@2x.png);box-shadow:0 0 0 2px ${C.chrome},0 0 0 3px ${C.deep};${extra}"></span>`;
const markK = (m: string) => `<img src="${KIT}/badges/${m === "ok" ? "mark_ok" : m === "soft" ? "mark_half" : "mark_no"}@2x.png" style="position:absolute;right:-7px;bottom:-6px;width:19px;height:19px">`;
const A: Dir = {
  id: "a", name: "A · Chrome plate (revised)", mh: 26,
  pill(p) {
    const H = 38, s = H / 88, fire = p.kind === "harm" || p.st === "fire";
    const frame = fire ? nine("panels/panel_strip", [25, 25, 25, 25], 0.3) : nine("panels/pill_status", [93, 0, 93, 0], s);
    const headCol = p.st === "ready" ? C.turqL : C.dim;
    const txt = p.head ? `<span style="color:${headCol}">${p.head}</span>&nbsp;<span class="dim" style="opacity:.7">·</span>&nbsp;${p.text}` : p.text;
    const disc = kdisc(p.icon, fire);
    const faces = p.faces.length ? `<span class="in" style="margin-left:10px">${p.faces.map((f, i) => cameoK(f, 24, i ? "margin-left:-7px" : "")).join("")}</span>` : "";
    const seen = p.seen ? `<span class="in" style="margin-left:9px;height:22px;padding:0 9px 0 0;border-radius:5px;background:${C.deep};box-shadow:0 0 0 1.5px ${C.line}aa;font-size:12.5px;gap:5px;overflow:hidden">${tile(7, 22)}${ic("sawyou", 13, C.cream)}Watched spot</span>` : "";
    const fill = p.fill != null ? `<div style="position:absolute;left:${fire ? 8 : 15}px;right:${fire ? 8 : 15}px;top:6px;bottom:6px;border-radius:${fire ? 3 : 13}px;overflow:hidden"><div style="width:${p.fill * 100}%;height:100%;background:${C.turqL}66"></div></div>` : "";
    const glow = p.st === "ready" ? `box-shadow:0 0 0 2px ${C.turqL},0 0 16px ${C.turqL}99;border-radius:${fire ? 6 : 19}px;` : "";
    const tail = `<span style="position:absolute;left:50%;top:${H - 2}px;margin-left:-9px;width:0;height:0;border-left:9px solid transparent;border-right:9px solid transparent;border-top:11px solid ${C.chrome}"></span><span style="position:absolute;left:50%;top:${H - 4}px;margin-left:-6px;width:0;height:0;border-left:6px solid transparent;border-right:6px solid transparent;border-top:7.5px solid #0c3434"></span>`;
    const pad = fire ? "0 15px 0 14px" : `0 ${Math.round(39 * s) - 2}px 0 ${Math.round(39 * s) - 5}px`;
    const html = `<div class="abs" style="height:${H}px;padding:${pad};font-size:15.5px;gap:9px;${glow}${p.st === "ready" ? "transform:scale(1.05);transform-origin:50% 100%;" : ""}">${frame}${fill}<span class="in" style="gap:9px;position:relative">${fire ? tile(10, 24) : ""}${disc}<span>${txt}</span>${faces}${seen}${fire ? tile(10, 24) : ""}</span>${tail}</div>`;
    return { html, w: 0, h: H, tipDy: 12 };
  },
  chip(c) {
    const lines = c.lines.map(([i, w]) => `<div class="in" style="height:22px;gap:8px;font-size:15px">${ic(i, 16, C.cream)}<span>${w}</span></div>`).join("");
    const unl = c.unl?.length ? `<div class="in" style="margin-top:4px;padding-top:6px;border-top:1px solid ${C.brass};gap:5px">${ic("chev_up", 12, C.turqL, "transform:rotate(90deg)")}${c.unl.map((u) => `<span class="in" style="height:22px;padding:0 9px 0 8px;gap:5px;font-size:12.5px;opacity:${u.shut ? 0.65 : 1};text-decoration:${u.shut ? "line-through" : "none"}">${nine("markers/plate_dark", [24, 24, 24, 24], 0.37)}<span class="in" style="gap:5px">${ic(u.icon, 12, C.cream)}${u.word}</span></span>`).join("")}</div>` : "";
    const face = `<span style="position:relative;flex:none;width:32px;height:32px;margin-top:1px">${cameoK(c.face, 32)}${markK(c.mark)}</span>`;
    return `<div class="abs" style="padding:13px 17px 12px 17px;align-items:flex-start;gap:11px;font-size:15px">${nine("panels/panel_dark", [35, 35, 35, 35], 0.5)}${face}<span class="in" style="flex-direction:column;align-items:flex-start;position:relative;padding-right:2px">${lines}${unl}</span></div>`;
  },
  marker(m) {
    const H = 26, s = H / 60;
    const inner = m.face ? cameoK(m.face, 18).replace("0 0 0 2px", "0 0 0 1.5px") : `<span style="font-size:14.5px;letter-spacing:.2px;font-variant-numeric:tabular-nums">${m.num}</span>`;
    const ring = m.left != null && m.total ? `<span style="position:absolute;left:10px;right:10px;bottom:3.5px;height:2.5px;border-radius:2px;background:${C.deep}99;overflow:hidden"><span style="display:block;width:${(m.left / m.total) * 100}%;height:100%;background:${m.low ? "#f0a020" : C.turqL}"></span></span>` : "";
    return `<div class="abs" style="height:${H}px;padding:0 11px 0 8px;gap:6px;font-size:14px;${m.low ? "animation:blink .5s infinite alternate;" : ""}">${nine("markers/plate_dark", [24, 24, 24, 24], s)}<span class="in" style="gap:6px">${ic(m.icon, 15, C.cream)}${inner}</span>${ring}</div>`;
  },
  tag(t) {
    return `<div class="abs" style="height:26px;padding:0 11px 0 0;gap:7px;font-size:13.5px;background:${C.deep}f0;border-radius:6px;box-shadow:0 2px 4px #0006;overflow:hidden"><span class="in" style="height:26px;align-self:stretch">${tile(9, 26)}</span><span class="in" style="gap:7px;position:relative;border:0">${ic("close_x", 12, C.cream)}${t}</span><span style="position:absolute;inset:0;border:1.5px dashed ${C.cream}99;border-radius:6px;pointer-events:none"></span></div>`;
  },
  pop(p) {
    const H = 26;
    return `<div class="abs" style="height:${H}px;padding:0 11px 0 9px;gap:7px;font-size:13.5px">${nine("markers/plate_dark", [24, 24, 24, 24], H / 60)}<span class="in" style="gap:7px">${ic(p.icon, 14, C.cream)}${p.text}</span></div>`;
  },
};

// ================================================================= B · station sign (flat enamel, cream keyline, line-colour tab, hanging stem)
const signBase = (fill: string, extra = "") => `background:${fill};border-radius:7px;box-shadow:0 0 0 2.5px ${C.deep},0 3px 6px #0005;${extra}`;
const keyline = (col = C.cream, dash = false, r = 4.5, inset = 3) => `<span style="position:absolute;inset:${inset}px;border:1.5px ${dash ? "dashed" : "solid"} ${col};border-radius:${r}px;pointer-events:none;opacity:.9"></span>`;
const rivets = (col = C.chrome) => [[5, 5], [5, -5], [-5, 5], [-5, -5]].map(([x, y]) => `<i style="position:absolute;${x > 0 ? "left" : "right"}:${Math.abs(x) + 1}px;${y > 0 ? "top" : "bottom"}:${Math.abs(y) + 1}px;width:3.5px;height:3.5px;border-radius:50%;background:${col};box-shadow:0 0 0 .8px #0008"></i>`).join("");
const B: Dir = {
  id: "b", name: "B · Station sign", mh: 46,
  pill(p) {
    const H = 34, fire = p.kind === "harm" || p.st === "fire"; const [fc, fi] = KIND[fire ? "harm" : p.kind];
    const face = fire ? "#1d2a2b" : "#16534f";
    const headCol = p.st === "ready" ? C.turqL : C.dim;
    const txt = p.head ? `<span style="color:${headCol}">${p.head}</span>&nbsp;<span style="opacity:.6">·</span>&nbsp;${p.text}` : p.text;
    const faces = p.faces.length ? `<span class="in" style="margin-left:9px">${p.faces.map((f, i) => cameo(f, 22, 2, i ? "margin-left:-6px" : "")).join("")}</span>` : "";
    const seen = p.seen ? `<span class="in" style="margin-left:9px;height:20px;padding:0 8px 0 6px;border-radius:4px;background:${C.deep};font-size:12px;gap:5px;box-shadow:0 0 0 1.2px ${C.cream}88">${stripes(6, 20)}${ic("sawyou", 12, C.cream)}Watched spot</span>` : "";
    const fill = p.fill != null ? `<div style="position:absolute;left:36px;right:3px;top:3px;bottom:3px;border-radius:0 4px 4px 0;overflow:hidden"><div style="width:${p.fill * 100}%;height:100%;background:${C.seafoam}77"></div></div>` : "";
    const ready = p.st === "ready";
    const clip = fire ? "clip-path:polygon(7px 0,calc(100% - 7px) 0,100% 7px,100% calc(100% - 7px),calc(100% - 7px) 100%,7px 100%,0 calc(100% - 7px),0 7px);" : "";
    const body = `<div class="abs" style="height:${H}px;padding:0 14px 0 0;font-size:15px;${signBase(face, fire ? "border-radius:0;box-shadow:0 0 0 2.5px #0b1615;" : ready ? `box-shadow:0 0 0 2.5px ${C.deep},0 0 0 4.5px ${C.turqL},0 3px 8px #0006;` : "")}${clip}">${fill}<span class="in" style="align-self:stretch;width:34px;justify-content:center;background:${fire ? "repeating-linear-gradient(135deg,#e4533d 0 6px,#2a1a18 6px 12px)" : fc};border-radius:${fire ? 0 : "5px 0 0 5px"};margin-right:10px;flex:none">${fire ? `<span style="width:22px;height:22px;border-radius:50%;background:#e4533d;display:flex;align-items:center;justify-content:center;box-shadow:0 0 0 2px #1d2a2b">${ic("fire", 14, C.cream)}</span>` : ic(p.icon, 18, fi)}</span>${keyline(ready ? C.turqL : C.cream, false, fire ? 2 : 4.5, 3)}<span class="in" style="gap:0;position:relative">${txt}${faces}${seen}</span></div>`;
    const stem = `<div style="position:absolute;left:50%;top:100%;width:2px;height:9px;margin-left:-1px;background:${C.chrome};box-shadow:0 0 0 .6px #0008"></div><div style="position:absolute;left:50%;top:calc(100% + 7px);width:7px;height:7px;margin-left:-3.5px;border-radius:50%;background:radial-gradient(circle at 35% 30%,#fff,${C.chrome});box-shadow:0 0 0 .8px #0009"></div>`;
    return { html: `<div style="position:relative;display:inline-block;${ready ? "transform:scale(1.04);transform-origin:50% 100%" : ""}">${body}${stem}</div>`, w: 0, h: H, tipDy: 16 };
  },
  chip(c) {
    const [fc] = KIND[c.kind];
    const lines = c.lines.map(([i, w]) => `<div class="in" style="height:22px;gap:8px;font-size:15px">${ic(i, 16, C.cream)}<span>${w}</span></div>`).join("");
    const unl = c.unl?.length ? `<div class="in" style="margin-top:5px;gap:5px">${ic("chev_up", 12, C.cream, "transform:rotate(90deg)")}${c.unl.map((u) => `<span class="in" style="height:21px;padding:0 9px 0 7px;gap:5px;font-size:12.5px;border-radius:4px;background:${C.deep};opacity:${u.shut ? 0.65 : 1};text-decoration:${u.shut ? "line-through" : "none"};box-shadow:0 0 0 1px ${C.cream}55">${ic(u.icon, 12, C.cream)}${u.word}</span>`).join("")}</div>` : "";
    const face = `<span style="position:relative;flex:none;width:30px;height:30px;margin-top:1px">${cameo(c.face, 30, 2)}<span style="position:absolute;right:-6px;bottom:-5px;width:16px;height:16px;border-radius:50%;background:${markBg(c.mark)};box-shadow:0 0 0 1.5px ${C.cream},0 0 0 2.5px ${C.deep};display:flex;align-items:center;justify-content:center">${ic(markIc(c.mark), 9.5, C.cream)}</span></span>`;
    return `<div class="abs" style="padding:15px 16px 11px 14px;align-items:flex-start;gap:10px;font-size:15px;${signBase("#16534f")}"><span style="position:absolute;left:0;right:0;top:0;height:7px;background:${fc};border-radius:7px 7px 0 0"></span>${keyline(C.cream, false, 4.5, 3.5).replace(`inset:3.5px`, "inset:9px 3.5px 3.5px 3.5px")}${rivets()}${face}<span class="in" style="flex-direction:column;align-items:flex-start;position:relative">${lines}${unl}</span></div>`;
  },
  marker(m) {
    const [fc, fi] = KIND[m.kind]; const D = 46;
    const inner = m.face ? cameo(m.face, 20, 1.5) : `<span style="font-size:16px;font-variant-numeric:tabular-nums">${m.num}</span>`;
    return `<div class="abs" style="width:${D}px;height:${D}px;border-radius:50%;background:${C.seafoam};box-shadow:0 0 0 2px ${C.cream},0 0 0 4px ${C.deep},0 3px 6px #0005;justify-content:center;flex-direction:column;${m.low ? "animation:blink .5s infinite alternate;" : ""}"><span style="position:absolute;left:-5px;right:-5px;top:17px;height:22px;background:#16534f;border-radius:4px;box-shadow:0 0 0 1.5px ${C.cream},0 0 0 3px ${C.deep}"></span><span class="in" style="position:absolute;top:4px;left:0;right:0;justify-content:center">${ic(m.icon, 13, C.deep)}</span><span class="in" style="position:absolute;top:17px;left:-5px;right:-5px;height:22px;justify-content:center">${inner}</span></div>`;
  },
  tag(t) {
    return `<div class="abs" style="height:26px;padding:0 12px 0 10px;gap:7px;font-size:13.5px;${signBase(C.deep + "f2", "border-radius:6px;box-shadow:0 0 0 2px #0b1615,0 2px 5px #0006;")}">${keyline(C.cream, true, 3.5, 2.5)}<span class="in" style="gap:7px;position:relative">${ic("close_x", 12, C.cream)}${t}</span></div>`;
  },
  pop(p) {
    return `<div class="abs" style="height:26px;padding:0 12px 0 10px;gap:7px;font-size:13.5px;${signBase("#16534f", "border-radius:6px;box-shadow:0 0 0 2px #0b1615,0 2px 5px #0006;")}">${keyline(C.cream, false, 3.5, 2.5)}<span class="in" style="gap:7px;position:relative">${ic(p.icon, 14, C.cream)}${p.text}</span></div>`;
  },
};

// ================================================================= C · orb and label (icon first, a quiet label, rings in the world)
const glass = `background:${C.deep}e6;box-shadow:0 0 0 1px ${C.cream}40,0 3px 7px #0005;`;
const orb = (fc: string, fi: string, icon: string, D: number, ringExtra = "", sq = false) =>
  `<span style="position:relative;flex:none;width:${D}px;height:${D}px;border-radius:${sq ? "28%" : "50%"};background:radial-gradient(circle at 35% 28%,${lighten(fc)} 0,${fc} 58%);box-shadow:0 0 0 2.5px ${C.chrome},0 0 0 3.8px ${C.deep},0 3px 6px #0006,inset 0 -3px 4px #0003;display:flex;align-items:center;justify-content:center;${ringExtra}">${ic(icon, Math.round(D * 0.55), fi)}</span>`;
const ringSvg = (D: number, frac: number, col: string, w = 3.2) => {
  const R = D / 2 + 6.5, S = (R + w) * 2, circ = 2 * Math.PI * R;
  return `<svg style="position:absolute;left:${D / 2 - S / 2}px;top:${D / 2 - S / 2}px;width:${S}px;height:${S}px;transform:rotate(-90deg);pointer-events:none" viewBox="0 0 ${S} ${S}"><circle cx="${S / 2}" cy="${S / 2}" r="${R}" fill="none" stroke="${C.deep}" stroke-opacity=".7" stroke-width="${w + 2}"/><circle cx="${S / 2}" cy="${S / 2}" r="${R}" fill="none" stroke="${C.cream}" stroke-opacity=".35" stroke-width="${w}"/><circle cx="${S / 2}" cy="${S / 2}" r="${R}" fill="none" stroke="${col}" stroke-width="${w}" stroke-linecap="round" stroke-dasharray="${circ * frac} ${circ}"/></svg>`;
};
const Cc: Dir = {
  id: "c", name: "C · Orb and label", mh: 30,
  pill(p) {
    const D = 34, fire = p.kind === "harm" || p.st === "fire"; const [fc, fi] = KIND[fire ? "harm" : p.kind];
    const headCol = p.st === "ready" ? C.turqL : C.dim;
    const txt = p.head ? `<span style="color:${headCol}">${p.head}</span>&nbsp;<span style="opacity:.6">·</span>&nbsp;${p.text}` : p.text;
    const faces = p.faces.length ? `<span class="in" style="margin-left:9px">${p.faces.map((f, i) => cameo(f, 22, 2, i ? "margin-left:-6px" : "")).join("")}</span>` : "";
    const seen = p.seen ? `<span class="in" style="margin-left:9px;height:20px;padding:0 8px 0 6px;border-radius:10px;background:#000a;font-size:12px;gap:5px;box-shadow:0 0 0 1px ${C.cream}77;border:1px dashed #0000">${ic("sawyou", 12, C.cream)}Watched spot</span>` : "";
    const frac = p.fill ?? (p.st === "ready" ? 1 : 0);
    const rg = p.fill != null || p.st === "ready" ? ringSvg(D, frac, p.st === "ready" ? C.turqL : C.cream) : "";
    // the orb sits on the target's line; the label runs right from it
    const html = `<div style="position:relative;display:inline-block;${p.st === "ready" ? "transform:scale(1.06);transform-origin:17px 100%" : ""}"><div class="abs" style="height:${D}px;font-size:15px;padding:0 15px 0 ${D + 6}px;border-radius:17px;${glass}margin-left:0">${txt}${faces}${seen}</div><div style="position:absolute;left:0;top:0">${orb(fc, fi, p.icon, D, "", fire)}${rg}</div><div style="position:absolute;left:${D / 2 - 0.75}px;top:100%;width:1.5px;height:10px;background:${C.cream};opacity:.85"></div></div>`;
    return { html, w: 0, h: D, tipDy: 10, anchorLeft: D / 2 } as any;
  },
  chip(c) {
    const [fc] = KIND[c.kind];
    const lines = c.lines.map(([i, w]) => `<div class="in" style="height:22px;gap:8px;font-size:15px">${ic(i, 16, C.cream)}<span>${w}</span></div>`).join("");
    const unl = c.unl?.length ? `<div class="in" style="margin-top:5px;gap:5px">${ic("chev_up", 12, C.dim, "transform:rotate(90deg)")}${c.unl.map((u) => `<span class="in" style="height:21px;padding:0 9px 0 6px;gap:5px;font-size:12.5px;border-radius:11px;background:#ffffff1f;opacity:${u.shut ? 0.65 : 1};text-decoration:${u.shut ? "line-through" : "none"}">${ic(u.icon, 12, C.cream)}${u.word}</span>`).join("")}</div>` : "";
    return `<div class="abs" style="padding:9px 15px 9px 48px;align-items:flex-start;font-size:15px;border-radius:17px;${glass}min-height:38px"><span style="position:absolute;left:-2px;top:-8px">${orbFace(c)}</span><span class="in" style="flex-direction:column;align-items:flex-start;margin-top:1px">${lines}${unl}</span></div>`;
  },
  marker(m) {
    const [fc, fi] = KIND[m.kind]; const D = 30; const frac = m.left != null && m.total ? m.left / m.total : 0;
    const inner = m.face ? cameo(m.face, 20, 1.5) : `<span style="font-size:15px;font-variant-numeric:tabular-nums">${m.num}</span>`;
    return `<div class="abs" style="height:${D}px;font-size:14px;padding:0 12px 0 ${D + 10}px;border-radius:15px;${glass}${m.low ? "animation:blink .5s infinite alternate;" : ""}">${inner}<div style="position:absolute;left:0;top:0">${orb(fc, fi, m.icon, D)}${m.num ? ringSvg(D, frac, m.low ? "#f0a020" : C.turqL, 2.8) : ""}</div></div>`;
  },
  tag(t) {
    return `<div class="abs" style="height:26px;font-size:13.5px;padding:0 12px 0 28px;border-radius:13px;background:${C.deep}ee;border:1.5px dashed ${C.cream}aa">${`<span style="position:absolute;left:3px;top:50%;margin-top:-10px;width:20px;height:20px;border-radius:50%;background:${C.ink2};box-shadow:0 0 0 1px ${C.cream}88;display:flex;align-items:center;justify-content:center">${ic("close_x", 11, C.cream)}</span>`}${t}</div>`;
  },
  pop(p) {
    return `<div class="abs" style="height:26px;font-size:13.5px;padding:0 12px 0 28px;border-radius:13px;${glass}"><span style="position:absolute;left:3px;top:50%;margin-top:-10px;width:20px;height:20px;border-radius:50%;background:${C.ink2};box-shadow:0 0 0 1px ${C.cream}88;display:flex;align-items:center;justify-content:center">${ic(p.icon, 12, C.cream)}</span>${p.text}</div>`;
  },
};
function orbFace(c: Chip) {
  return `<span style="position:relative;display:block;width:36px;height:36px">${cameo(c.face, 36, 2.5, `box-shadow:0 0 0 2.5px ${ROLE[c.face]},0 0 0 4px ${C.deep},0 3px 6px #0006`)}<span style="position:absolute;right:-5px;bottom:-4px;width:17px;height:17px;border-radius:50%;background:${markBg(c.mark)};box-shadow:0 0 0 1.5px ${C.cream},0 0 0 2.5px ${C.deep};display:flex;align-items:center;justify-content:center">${ic(markIc(c.mark), 10, C.cream)}</span></span>`;
}

// ---- small helpers
function lighten(hex: string) { const n = parseInt(hex.slice(1), 16); const f = (v: number) => Math.min(255, Math.round(v + (255 - v) * 0.32)); return `rgb(${f(n >> 16)},${f((n >> 8) & 255)},${f(n & 255)})`; }
function stripes(w: number, h: number) { // the kit's warn stripes (stripes_warn): cream and ink, 45 degrees
  if (!w) return "";
  return `<span style="flex:none;width:${w}px;height:${h || "100%"}px;background:repeating-linear-gradient(135deg,${C.cream} 0 3px,${C.ink} 3px 6px);${h ? "" : ""}border-radius:2px"></span>`;
}
const markBg = (m: string) => (m === "ok" ? "#2c9a68" : m === "soft" ? "#6d7b79" : "#8d3a30");
const markIc = (m: string) => (m === "ok" ? "check" : m === "soft" ? "asked" : "close_x");

// ---------------------------------------------------------------- the scenes (anchors read off the 0.2.56 shots)
interface Place { at: [number, number]; mode: "bc" | "tl" | "tc" | "tip"; node: (d: Dir) => string; lift?: boolean }
const listen: Chip = { face: "scout", mark: "ok", kind: "look", lines: [["debt", "Bureau loan"], ["fearful", "Fearful"], ["secret", "Loves the guard"]], unl: [{ icon: "help", word: "Help" }, { icon: "pressure", word: "Pressure" }] };
const helpOffer: Pill = { head: "Hold", text: "Help Inez · $40k · 64 s · +1 Trail", kind: "talk", icon: "help", faces: ["fixer"], st: "offer" };

const SCENES: { id: string; bg: string; place: (d: Dir) => string[]; crop: [number, number, number, number] }[] = [
  { id: "pill-states", bg: "guard-back", crop: [340, 120, 1760, 820], place: (d) => [
    pillAt(d, { head: "Hold", text: "Watch the guard · 26 s", kind: "look", icon: "watch", faces: ["scout"], st: "offer", seen: true }, 960, 482),
    pillAt(d, { head: "Hold", text: "Call the driver · $0 · 40 s", kind: "talk", icon: "call", faces: ["fixer"], st: "hold", fill: 0.55 }, 560, 420),
    pillAt(d, { head: "Let go", text: "Loop the camera · 12 s", kind: "tech", icon: "loop", faces: ["tech"], st: "ready", fill: 1 }, 905, 215),
    pillAt(d, { text: "Firing at an officer · down in 2 s", kind: "harm", icon: "fire", faces: ["enforcer"], st: "fire" }, 1485, 690),
  ] },
  { id: "help-pip", bg: "help-pip", crop: [700, 270, 1220, 480], place: (d) => [chipAt(d, listen, 961, 392), pillAt(d, helpOffer, 960, 456)] },
  { id: "listen-chip", bg: "listen-chip", crop: [780, 220, 1140, 370], place: (d) => [chipAt(d, listen, 961, 347)] },
  { id: "walkin", bg: "walkin", crop: [760, 320, 1160, 480], place: (d) => [
    chipAt(d, { face: "scout", mark: "ok", kind: "look", lines: [["routine", "Rounds"]], unl: [{ icon: "listen", word: "Listen" }, { icon: "pay", word: "Pay" }] }, 961, 392),
    pillAt(d, { text: "to Inez · Listen · 14 s", kind: "look", icon: "listen", faces: ["scout"], st: "order" }, 960, 456),
  ] },
  { id: "call-window", bg: "call-window", crop: [800, 340, 1120, 480], place: (d) => [
    chipAt(d, { face: "fixer", mark: "ok", kind: "talk", lines: [["eye_shut", "Guard away"]] }, 960, 450 - d.mh - 7),
    markerAt(d, { icon: "eye_shut", num: "174", left: 174, total: 177, kind: "talk" }, 960, 450),
  ] },
  { id: "cancel-warn", bg: "cancel-warn", crop: [740, 350, 1180, 480], place: (d) => [
    tagAt(d, "Cancels Watch · 4 s lost", 958, 411),
    pillAt(d, { head: "Hold", text: "Watch the patrol · 20 s", kind: "look", icon: "watch", faces: ["scout"], st: "offer" }, 958, 456),
  ] },
  { id: "cancel-pop", bg: "cancel-pop", crop: [780, 400, 1140, 520], place: (d) => [popAt(d, { icon: "close_x", text: "Watch cancelled · 5 s lost" }, 960, 472)] },
  { id: "guard-back", bg: "guard-back", crop: [820, 400, 1100, 520], place: (d) => [popAt(d, { icon: "eye_open", text: "Guard back" }, 960, 478)] },
  { id: "tp-fire", bg: "tp-fire", crop: [800, 420, 1140, 540], place: (d) => [pillAt(d, { text: "Drawing", kind: "harm", icon: "fire", faces: ["scout"], st: "fire" }, 970, 505)] },
];

// placing: tipY is the target point; the pill's tip hangs 'tipDy' below its box
function pillAt(d: Dir, p: Pill, x: number, tipY: number) {
  const r: any = d.pill(p); const ax = r.anchorLeft;
  return `<div style="position:absolute;left:${x}px;top:${tipY - r.tipDy}px;transform:translate(${ax != null ? -ax + "px" : "-50%"},-100%);display:flex;justify-content:center">${r.html}</div>`;
}
function chipAt(d: Dir, c: Chip, x: number, bottomY: number, mode?: string) { return `<div style="position:absolute;left:${x}px;top:${bottomY - (mode === "lift" ? 56 : 0)}px;transform:translate(-50%,-100%);">${d.chip(c)}</div>`; }
function markerAt(d: Dir, m: Marker, x: number, bottomY: number) { return `<div style="position:absolute;left:${x}px;top:${bottomY}px;transform:translate(-50%,-100%)">${d.marker(m)}</div>`; }
function tagAt(d: Dir, t: string, x: number, bottomY: number) { return `<div style="position:absolute;left:${x}px;top:${bottomY - 4}px;transform:translate(-50%,-100%)">${d.tag(t)}</div>`; }
function popAt(d: Dir, p: Pop, x: number, bottomY: number) { return `<div style="position:absolute;left:${x}px;top:${bottomY}px;transform:translate(-50%,-100%)">${d.pop(p)}</div>`; }

const QA = `<pre id="qa" style="position:fixed;left:0;top:0;opacity:0"></pre><script>addEventListener("load",()=>setTimeout(()=>{
const roots=[...document.querySelectorAll(".abs")].map(e=>e.getBoundingClientRect()), bad=[];
const R=(r)=>[r.left|0,r.top|0,r.right|0,r.bottom|0].join(",");
document.querySelectorAll(".abs").forEach((root,i)=>{const rr=root.getBoundingClientRect();
 if(rr.left<0||rr.top<0||rr.right>1920||rr.bottom>1080) bad.push("offscreen "+R(rr));
 root.querySelectorAll("*").forEach(el=>{ if(el.classList.contains("fr")||el.tagName=="svg"||el.tagName=="circle") return; const st=getComputedStyle(el); if(st.position=="absolute"&&!el.textContent.trim()&&!el.classList.contains("ic")&&!el.classList.contains("cam")) return;
  const r=el.getBoundingClientRect(); if(r.width==0) return;
  const tol=7; if(r.left<rr.left-tol||r.right>rr.right+tol||r.top<rr.top-tol-3||r.bottom>rr.bottom+tol+3) bad.push("outside "+(el.textContent.trim().slice(0,14)||el.className)+" "+R(r)+" in "+R(rr)); });
 root.querySelectorAll(".in").forEach(el=>{const t=el.scrollWidth-el.clientWidth; if(t>1) bad.push("overflow-x "+t+" "+el.textContent.trim().slice(0,14));});
 roots.forEach((o,j)=>{ if(j>i){const a=rr,b=o; if(a.left<b.right-1&&b.left<a.right-1&&a.top<b.bottom-1&&b.top<a.bottom-1) bad.push("roots overlap "+R(a)+" x "+R(b));}});});
document.getElementById("qa").textContent="QA:"+JSON.stringify(bad)+":END";},400));</script>`;
function page(bg: string, parts: string[], qa = false) {
  return `<!doctype html><meta charset=utf-8><style>${CSS}@keyframes blink{from{opacity:1}to{opacity:.55}}</style><body><img class="bg" src="file://${SRC}/clean/${bg}.png">${parts.join("")}${qa ? QA : ""}</body>`;
}

function render(htmlPath: string, png: string, scale = 1) {
  const r = spawnSync(CHROME, ["--headless=new", "--disable-gpu", "--hide-scrollbars", "--allow-file-access-from-files", `--force-device-scale-factor=${scale}`, "--window-size=1920,1080", "--virtual-time-budget=2500", `--screenshot=${png}`, `file://${htmlPath}`], { encoding: "utf8" });
  if (!existsSync(png)) { console.error(r.stderr); throw new Error("no render " + png); }
}

const only = process.argv.slice(2);
const dirs = [A, B, Cc].filter((d) => !only.length || only.includes(d.id));
mkdirSync(`${SRC}/html`, { recursive: true });
for (const d of dirs) {
  let n = 1;
  for (const s of SCENES) {
    const hp = `${SRC}/html/${d.id}-${s.id}.html`; writeFileSync(hp, page(s.bg, s.place(d)));
    const png = `${OUT}/${d.id}-${String(n).padStart(2, "0")}-${s.id}.png`;
    render(hp, png); // 1080p composite
    render(hp, `${SRC}/x2-${d.id}-${s.id}.png`, 2); // for the 2x crops
    const qp = `${SRC}/html/qa-${d.id}-${s.id}.html`; writeFileSync(qp, page(s.bg, s.place(d), true));
    const q = spawnSync(CHROME, ["--headless=new", "--disable-gpu", "--allow-file-access-from-files", "--window-size=1920,1080", "--virtual-time-budget=2500", "--dump-dom", `file://${qp}`], { encoding: "utf8" });
    const m = /QA:(.*?):END/s.exec(q.stdout || ""); console.log(`QA ${d.id}-${s.id}: ${m ? (m[1] === "[]" ? "ok" : m[1]) : "no result"}`);
    n++;
  }
}
console.log("done", dirs.map((d) => d.id).join(","));
