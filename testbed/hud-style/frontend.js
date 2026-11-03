// Front end, direction A "light line": mockup page (brief 051). #s=menu|options|controls|brief|pause|states|timing|word-demo & w,h (device px), static=1, bare=1.
// Everything numeric comes from frontend.tokens.js, so the mockup and the spec cannot drift. Live (no static=1): hover, arrow keys and clicks play the transitions.
import { A, hexA } from "./frontend.tokens.js";
const q = new URLSearchParams(location.hash.slice(1));
const W = +(q.get("w") || 1920), H = +(q.get("h") || 1080), k = H / 1080, SW = W / k;
const S = q.get("s") || "menu";
const stage = document.getElementById("stage");
stage.style.zoom = k; stage.style.width = SW + "px";
if (q.get("static")) stage.classList.add("static");
if (q.get("bare")) stage.classList.add("bare");
if (q.get("sa")) A.scrims.menu[0].alpha = +q.get("sa");
const T = A.type, al = A.alpha;
stage.style.setProperty("--idle", al.idleWord);
const f = (n) => +n.toFixed(2);
const tyle = (t, extra = "") => `font-family:${t.font.includes("Tab") ? "JostT" : "Jost"};font-size:${t.sizeRef}px;letter-spacing:${t.trackingEm || 0}em;${t.upper ? "text-transform:uppercase;" : ""}${extra}`;

// ---- picture, scrims, title -------------------------------------------------
const bg = (url, fx = 0.5) => `<div class="bg" style="background-image:url(${url});background-position:${fx * 100}% 50%"></div>`;
const smooth = (x) => x * x * (3 - 2 * x);
function scrim(s) {
  const stops = []; for (let i = 0; i <= 12; i++) { const p = i / 12, hd = s.holdRef ? Math.min(0.95, s.holdRef / (s.frac * SW)) : s.hold, a = p <= hd ? s.alpha : s.alpha * (1 - smooth((p - hd) / (1 - hd))); stops.push(`rgba(11,22,21,${f(a)}) ${f(p * 100)}%`); }
  const dir = { left: "to right", right: "to left", top: "to bottom", bottom: "to top" }[s.edge], horiz = s.edge === "left" || s.edge === "right";
  const pos = { left: "left:0;top:0;bottom:0;", right: "right:0;top:0;bottom:0;", top: "left:0;right:0;top:0;", bottom: "left:0;right:0;bottom:0;" }[s.edge];
  const size = horiz ? `width:${f(s.frac * SW)}px;` : `height:${f(s.frac * 1080)}px;`;
  return `<div class="scrim" style="${pos}${size}background:linear-gradient(${dir},${stops.join(",")})"></div>`;
}
const veil = (a) => `<div class="abs" style="inset:0;background:rgba(11,22,21,${a})"></div>`;
function coverRect(fx = 0.5) { const sc = Math.max(SW / 2560, 1080 / 1440), dw = 2560 * sc, dh = 1440 * sc; return { x0: (SW - dw) * fx, y0: (1080 - dh) * 0.5, dw, dh }; }
function title() {
  const c = coverRect(), x = c.x0 + 0.57 * c.dw, y = c.y0 + 0.04 * c.dh;
  return `<div class="lock" style="left:${f(x)}px;top:${f(y)}px"><div class="a t">THE</div><div class="b t">SUBORNED</div><div class="r"></div></div>`;
}
const build = () => `<div class="build t">Build 0.2.87 · c031 · sim 0.26.0 · 04a4fb75+</div>`;

// ---- words: text items with a marker and a line ---------------------------------
// o: {x,y (container top-left) | bottom, items:[{t,st}], t:type token, itemH, gap, inset, gutter, lineW, lineY, slide, w}
function words(o) {
  const { itemH, gap = 0 } = o, tt = o.t, ms = A.marker.sizeRef, W_ = o.w;
  const n = o.items.length, hgt = n * itemH + (n - 1) * gap;
  let h = "", mkTop = null, extra = 0, tops = [];
  o.items.forEach((it, i) => {
    extra += it.gapBefore || 0; const top = i * (itemH + gap) + extra; tops.push(top);
    if ((it.st || "").includes("f") || (it.st || "").includes("p")) mkTop = top + itemH / 2;
    h += `<div class="w ${it.st || ""}" data-i="${i}" style="top:${top}px;height:${itemH}px;width:${W_}px;--lw:${o.lineW}px">` +
      `<div class="lab t shadow ${o.noShadow ? "" : ""}" style="left:${o.inset}px;right:0;${tyle(tt)}">${it.t}</div>` +
      `<div class="ul" style="left:${o.inset}px;top:${o.lineY}px"></div>` +
      (o.slide ? "" : `<div class="mk ${/f|p/.test(it.st || "") ? "" : "hide"} ${/p/.test(it.st || "") ? "p" : ""}" style="--ms:${ms}px;left:${o.inset - o.gutter}px;top:${itemH / 2}px"></div>`) + `</div>`;
  });
  const total = tops.length ? tops[tops.length - 1] + itemH : hgt;
  if (o.slide) h += `<div class="mk ${mkTop == null ? "hide" : ""} ${o.items.some((i) => /p/.test(i.st || "")) ? "p" : ""}" style="--ms:${ms}px;left:${o.inset - o.gutter}px;top:${mkTop ?? 0}px"></div>`;
  const pos = o.bottom != null ? `bottom:${o.bottom}px;` : `top:${o.y}px;`;
  return `<div class="wl ${o.slide ? "slide" : ""}" style="left:${o.x}px;${pos}width:${W_}px;height:${total}px" data-itemh="${itemH}">${h}</div>`;
}
const M = A.menu;
function menuWords(items, o = {}) {
  return words({ x: M.leftRef, bottom: M.bottomRef, w: M.itemW, itemH: M.itemH, gap: M.gapRef, inset: M.textInsetRef, gutter: A.marker.gutterRef, lineW: M.lineWRef, lineY: M.lineYRef, t: T.word, slide: true, items, ...o });
}
const state = (i, fi, st = "f") => (i === fi ? st : "");

// ---- sheet pieces ---------------------------------------------------------------
const SH = A.sheet, CT = A.controls;
function chev(side, p) { return `<span class="chev c${side} ${p ? "p" : ""}"></span>`; }
const ctlChoice = (v, pr) => `<div class="ch t" style="${tyle(T.rowValue)}">${chev("l", pr === "l")}<span class="v">${v}</span>${chev("r", pr === "r")}</div>`;
const ctlSlider = (u, text, drag) => `<div class="sl ${drag ? "drag" : ""}" style="${tyle(T.rowValue)}"><div class="rail"><div class="fill" style="width:${u * 100}%"></div><div class="kn" style="left:${u * 100}%"></div></div><span class="val t">${text}</span></div>`;
const ctlSwitch = (on, p) => `<div class="sw ${on ? "on" : "off"} ${p ? "p" : ""}" style="${tyle(T.rowValue)}"><span class="box"><i></i></span><span class="word t">${on ? "On" : "Off"}</span></div>`;
function cap(txt, kind = "", extra = "") { return `<span class="cap ${kind}">${extra}<span class="t">${txt}</span></span>`; }

function rows(o) { // o: {x,y,w,items:[{k:'row'|'group',label,ctl,st}], rowH,groupH}
  let y = 0, h = "", tickTop = null, rowH = o.rowH || SH.rowH;
  o.items.forEach((it) => {
    if (it.k === "group") { h += `<div class="gh" style="top:${y}px;height:${SH.groupH}px"><span class="t" style="${tyle(T.group)}">${it.label}</span><i></i></div>`; y += SH.groupH; return; }
    const st = it.st || ""; if (/f|p/.test(st)) tickTop = y + A.line.tickInsetRef;
    h += `<div class="r ${st}" style="top:${y}px;height:${rowH}px;${it.w ? `width:${it.w}px;right:auto;` : ""}"><div class="rlab t" style="${tyle(T.rowLabel)}">${it.label}</div><div class="rc">${it.ctl || ""}</div><div class="ul"></div></div>`;
    y += rowH;
  });
  h += `<div class="tick" style="top:${tickTop ?? 0}px;height:${rowH - 2 * A.line.tickInsetRef}px;${tickTop == null ? "visibility:hidden;" : ""}"></div>`;
  return `<div class="rows" data-rowh="${rowH}" style="top:${o.y}px;height:${y}px;${o.left != null ? `left:${o.left}px;right:auto;width:${o.w}px;` : ""}">${h}</div>`;
}
function sheet(o) { // o: {w,h,titleText,tabs,active,tabState,body,foot:{left:[],right:[]},cx,top,fill}
  const w = o.w || SH.wRef, h = o.h || SH.hRef, x = (SW - w) / 2, y = o.top ?? (1080 - h) / 2;
  const fill = hexA(A.colors.ink, o.fillAlpha ?? SH.fillAlpha), bd = hexA(A.colors.chrome, SH.borderAlpha);
  let tabs = "", tabX = SH.padX, ul = "";
  if (o.tabs) {
    const tt = T.tab; const meas = document.createElement("canvas").getContext("2d"); meas.font = `${tt.sizeRef}px Jost`;
    o.tabs.forEach((name, i) => {
      const tw = textW(name, tt) + 2 * SH.tabPadX, on = i === o.active, hv = i === o.hoverTab;
      tabs += `<div class="tab ${on ? "on" : ""} ${hv ? "hov" : ""}" style="left:${tabX - SH.padX}px;width:${tw}px"><span class="t up" style="${tyle(tt)}">${name}</span></div>`;
      if (on) ul = `<div class="tabul ${o.tabFocus ? "big" : ""}" style="left:${tabX - SH.padX}px;width:${tw}px"></div>${o.tabFocus ? `<div class="mk" style="--ms:${A.marker.sizeRef}px;left:${tabX - SH.padX - 2}px;top:${SH.tabH / 2}px"></div>` : ""}`;
      if (hv && !on) ul += `<div class="tabhov" style="left:${tabX - SH.padX}px;width:${tw}px"></div>`;
      tabX += tw + SH.tabGap;
    });
    tabs = `<div class="tabs" style="top:${SH.tabsTop}px">${tabs}${ul}</div>`;
  }
  const footY = h - SH.footerRuleFromBottom;
  const foot = `<div class="rule" style="left:${SH.padX}px;right:${SH.padX}px;top:${footY}px"></div>` + (o.foot ? o.foot : "");
  const glass = o.glass === false ? "" : `<div class="glass"><i style="left:${-f(x) - 1}px;top:${-f(y) - 1}px;width:${SW}px;background-image:url(${o.glassUrl || "/bg/frontend-menu.jpg"})"></i><b style="background:${hexA(A.colors.inkDeep, o.veil ?? A.scrims.panelVeil.alpha)}"></b><b style="background:${hexA(A.colors.ink, SH.glassTint)}"></b></div>`;
  return `<div class="sheet" style="left:${f(x)}px;top:${f(y)}px;width:${w}px;height:${h}px;--sheetfill:${o.glass === false ? fill : "transparent"};--sheetborder:${bd}">` + glass +
    `<div class="title t up" style="top:${SH.titleTop}px;${tyle(T.title)}">${o.titleText}</div>${tabs}${o.body}${foot}</div>`;
}
const _c = document.createElement("canvas").getContext("2d");
function textW(s, tt) { _c.font = `${tt.sizeRef}px Jost`; const w = _c.measureText(tt.upper ? s.toUpperCase() : s).width; return w + (tt.trackingEm || 0) * tt.sizeRef * s.length; }
function footerItems(left, right, st = {}) {
  const itemW = (s) => Math.ceil(textW(s, T.action)) + 30 + 6;
  const mk = (t, x, side) => words({ x, y: 0, w: itemW(t), items: [{ t, st: st[t] || "" }], itemH: SH.footerItemH, inset: 30, gutter: 20, lineW: Math.ceil(textW(t, T.action)) + 14, lineY: 29, t: T.action });
  let h = "";
  const fy = SH.hRef - SH.footerRuleFromBottom, top = fy + (SH.footerRuleFromBottom - SH.footerItemH) / 2 - 2;
  left.forEach((t) => h += `<div class="abs" style="left:${SH.padX - 30 + 4}px;top:${top}px">${mk(t)}</div>`);
  right.forEach((t) => h += `<div class="abs" style="right:${SH.padX - 8}px;top:${top}px;width:${itemW(t)}px">${mk(t)}</div>`);
  return h;
}

// ---- screens -------------------------------------------------------------------
function menuScreen(save, fi) {
  const labels = save ? ["New game", "Continue", "Options", "Controls", "Quit"] : ["New game", "Options", "Controls", "Quit"];
  const st = q.get("st") || "f";
  return bg("/bg/frontend-menu.jpg") + A.scrims.menu.map(scrim).join("") + title() + build() + menuWords(labels.map((t, i) => ({ t, st: state(i, fi, st) })));
}
const DISPLAY = (fi, dis = true) => [
  { k: "row", label: "Window mode", ctl: ctlChoice("Borderless") }, { k: "row", label: "Resolution", ctl: ctlChoice("Native") },
  { k: "row", label: "Render scale", ctl: ctlSlider(1, "100%") }, { k: "row", label: "Quality", ctl: ctlChoice("Balanced") },
  { k: "row", label: "V-sync", ctl: ctlSwitch(true) }, { k: "row", label: "Frame cap", ctl: ctlChoice("60"), st: "d" },
  { k: "row", label: "HUD scale", ctl: ctlSlider(0.4, "100%") },
];
const PAGES = {
  Display: DISPLAY,
  Audio: () => [["Master", 0.8], ["Music", 0.6], ["Effects", 0.8], ["Voices", 0.9]].map(([l, u]) => ({ k: "row", label: l, ctl: ctlSlider(u, Math.round(u * 100) + "") })),
  Camera: () => [
    { k: "group", label: "Commander view" }, { k: "row", label: "Pan speed", ctl: ctlSlider(0.4, "18 m/s") }, { k: "row", label: "Edge pan", ctl: ctlSwitch(false) },
    { k: "row", label: "Orbit sensitivity", ctl: ctlSlider(0.43, "1.0") }, { k: "row", label: "Invert orbit", ctl: ctlSwitch(false) }, { k: "row", label: "Trackpad drag orbit", ctl: ctlSwitch(false) },
    { k: "group", label: "Third person" }, { k: "row", label: "Camera distance", ctl: ctlChoice("Default") }, { k: "row", label: "Camera settle", ctl: ctlSwitch(true) }],
  Gameplay: () => [
    { k: "group", label: "Orders" }, { k: "row", label: "Click scheme", ctl: ctlChoice("Click walks, hold acts") }, { k: "row", label: "Orders while paused", ctl: ctlChoice("Start together") },
    { k: "row", label: "Hints", ctl: ctlSwitch(true) }, { k: "row", label: "Default game speed", ctl: ctlChoice("x1") }, { k: "row", label: "Conversations", ctl: ctlChoice("First time") },
    { k: "group", label: "Fights" }, { k: "row", label: "Agents near you", ctl: ctlChoice("Fire with you") },
    { k: "group", label: "Playground" }, { k: "row", label: "Third person: right button", ctl: ctlChoice("Right hold fires") }, { k: "row", label: "Third person: aim", ctl: ctlChoice("Hold") }],
};
function pageRows(tab, fi) {
  const items = PAGES[tab](fi); let ri = -1;
  items.forEach((it) => { if (it.k === "row") { ri++; if (ri === fi) it.st = (it.st || "") + "f"; } });
  return items;
}
function optionsScreen(tab, fi, pause) {
  const tabs = ["Display", "Audio", "Camera", "Gameplay"];
  const body = rows({ y: SH.bodyTop, items: pageRows(tab, fi) });
  const ff = q.get("foot") || ""; // "Reset page" or "Back" focused
  return (pause ? bg("/bg/frontend-game.jpg") : bg("/bg/frontend-menu.jpg") + "") + veil(pause ? 0.4 : A.scrims.panelVeil.alpha) + (pause ? "" : A.scrims.panel.map(scrim).join("") + build()) +
    sheet({ veil: pause ? 0.4 : undefined, glassUrl: pause ? "/bg/frontend-game.jpg" : null, titleText: "Options", tabs, active: tabs.indexOf(tab), hoverTab: +(q.get("hov") ?? -1), tabFocus: !!q.get("tabf"), body, foot: footerItems(["Reset page"], ["Back"], { "Reset page": ff === "reset" ? "f" : "", Back: ff === "back" ? "f" : "" }) });
}
const KEYS = {
  left: [["Select agent 1", "1", ["F1", "Num 1"]], ["Select agent 2", "2", ["F2", "Num 2"]], ["Select agent 3", "3", ["F3", "Num 3"]], ["Select agent 4", "4", ["F4", "Num 4"]], ["Select all", "5", ["Num 5"]], ["Next agent", "Tab", []],
    ["Add to group", "Shift", ["R Shift"]], ["Pick action 1", "Shift 1", ["Num 1"]], ["Pick action 2", "Shift 2", ["Num 2"]], ["Pick action 3", "Shift 3", ["Num 3"]], ["Pick action 4", "Shift 4", ["Num 4"]], ["Cancel", "N", []], ["Walk or run", "G", []]],
  right: [["Pause", "Space", []], ["Faster", "=", ["Num +"]], ["Slower", "–", ["Num –"]], ["Next event", "N", []], ["Briefing", "B", []], ["History", "L", []], ["Intel", "I", []], ["Response list", "R", []], ["Hints", "H", []], ["Acknowledge", "K", []],
    ["Detail (hold)", "Alt", ["R Alt"]], ["All strips", "`", []], ["Play again", "Return", ["Num Enter"]]],
};
function controlsScreen(mode, fi) {
  const colW = (SH.wRef - 2 * SH.padX - SH.colGap) / 2;
  const mkCol = (list, off, fiCol) => rows({ left: SH.padX + off, w: colW, y: SH.bodyTop, rowH: SH.controlsRowH, items: list.map(([l, p, alts], i) => {
    const gi = off ? i + 13 : i; let st = gi === fi ? "f" : "";
    let ctl = alts.map((a) => cap(a, "alt")).join("");
    let kind = "";
    if (mode === "listen" && gi === fi) kind = "listen";
    if (mode === "clash" && (l === "Cancel" || l === "Next event")) kind = "clash";
    const p2 = kind === "listen" ? "Press a key" : p;
    ctl += cap(p2, kind, kind === "clash" ? `<i class="bd2"></i>` : "");
    return { k: "row", label: l, ctl, st };
  }) });
  const tabs = ["Command", "Camera", "Fights", "Third person"];
  let strip = "";
  if (mode === "clash") {
    const sy = SH.bodyTop + 13 * SH.controlsRowH + 8;
    const sw = words({ x: 0, y: 0, w: 120, items: [{ t: "Swap", st: "" }], itemH: 40, inset: 30, gutter: 20, lineW: 74, lineY: 29, t: T.action });
    const cn = words({ x: 0, y: 0, w: 130, items: [{ t: "Cancel", st: "f" }], itemH: 40, inset: 30, gutter: 20, lineW: 84, lineY: 29, t: T.action });
    strip = `<div class="strip" style="top:${sy}px"><div class="bdg gl"></div><div class="msg t" style="${tyle({ ...T.rowLabel, sizeRef: 16.5 })}">N already: Next event</div>` +
      `<div class="abs" style="right:150px;top:8px;width:120px;height:40px">${sw}</div><div class="abs" style="right:12px;top:8px;width:130px;height:40px">${cn}</div></div>`;
  }
  const body = mkCol(KEYS.left, 0) + mkCol(KEYS.right, colW + SH.colGap) + strip;
  return bg("/bg/frontend-menu.jpg") + veil(A.scrims.panelVeil.alpha) + A.scrims.panel.map(scrim).join("") + build() + sheet({ titleText: "Controls", tabs, active: 0, body, foot: footerItems(["Reset all"], ["Back"]) });
}
function briefScreen() {
  const B = A.brief, fx = B.imageFocus[0];
  const col = `<div class="brief t0" style="right:${B.colRightRef}px;top:${B.colTopRef}px;width:${B.colWRef}px">` +
    `<div class="h t">The Handover</div><div class="hr"></div>` +
    `<div class="hook t">Vale has a file on you. Tonight he carries it to Security.</div>` +
    ["The cameras caught your cell. Councillor Vale put it on paper.", "No networks here. The file lives in a folder and in his head.", "Reach him first. Then we learn who else has read it."].map((p) => `<p class="t">${p}</p>`).join("") +
    `<div class="sub t" style="margin-top:20px"> </div>` +
    [["Target", "Councillor Vale. Holds the file on you."], ["Location", "Civic Records Office, the civic block."], ["Window", "Shift change, 5:30 pm. He leaves soon after."], ["Objective", "Take him alive. Deliver him to the getaway car."]].map(([a, b]) => `<div class="fact"><b class="t">${a}</b><span class="t">${b}</span></div>`).join("") +
    `<div class="sub t">Intel</div><ul>` + ["Inez, café waitress. She knows the door guard.", "Vale's driver needs cash.", "A rival crew wants him too.", "Cameras are watched by people. People can be turned."].map((l) => `<li class="t">${l}</li>`).join("") + `</ul><div class="go t">Quiet hands. Go.</div></div>`;
  const cont = words({ x: SW - B.colRightRef - B.continueW, bottom: B.continueBottomRef, w: B.continueW, itemH: 48, inset: 40, gutter: 24, lineW: B.continueW - 40, lineY: 37, t: T.wordSmall, items: [{ t: "Continue", st: q.get("st") || "f" }] });
  return bg("/bg/frontend-menu.jpg", fx) + A.scrims.brief.map(scrim).join("") + col + cont;
}
function pauseScreen() {
  const P = A.pause, labels = ["Resume", "Options", "Controls", "Restart mission", "Fight range", "Quit to menu", "Quit"], fi = +(q.get("f") ?? 0);
  const items = labels.map((t, i) => ({ t, st: state(i, fi, q.get("st") || "f"), gapBefore: i === 5 ? P.endGapRef : 0 }));
  const listH = labels.length * P.itemH + P.endGapRef, titleH = 52, buildH = 34;
  const cardH = P.padTop + titleH + listH + buildH + P.padBottom, x = (SW - P.wRef) / 2, y = 1080 * P.topFrac;
  const list = words({ x: 0, y: P.padTop + titleH, w: P.wRef - 2 * P.padX, items, itemH: P.itemH, inset: P.gutterRef + 4, gutter: P.gutterRef, lineW: P.lineWRef, lineY: 33, t: T.wordSmall, slide: true });
  const card = `<div class="sheet" style="left:${f(x)}px;top:${f(y)}px;width:${P.wRef}px;height:${cardH}px;--sheetfill:${hexA(A.colors.ink, P.fillAlpha)};--sheetborder:${hexA(A.colors.chrome, SH.borderAlpha)}">` +
    `<div class="title t up" style="left:${P.padX - 2}px;top:${P.padTop}px;${tyle({ ...T.title, sizeRef: P.titleRef })}">Paused</div>` +
    `<div class="abs" style="left:${P.padX}px;top:0">${list}</div>` +
    `<div class="build t" style="position:absolute;left:${P.padX - 2}px;right:auto;bottom:${P.padBottom}px;font-size:11.5px">Build 0.2.87 · c031 · sim 0.26.0 · 04a4fb75+</div></div>`;
  return bg("/bg/frontend-game.jpg") + card;
}

// ---- boards ------------------------------------------------------------------
const bcap = (t, x, y, kk) => `<div class="cap2 ${kk || ""}" style="left:${x}px;top:${y}px">${t}</div>`;
const bnote = (t, x, y, w) => `<div class="note" style="left:${x}px;top:${y}px;width:${w}px">${t}</div>`;
const boardBg = `<div class="abs" style="inset:0;background:#0b1615"></div>`;
const picCell = (x, y, w, h, inner, shiftX = 0) => `<div class="pnl" style="left:${x}px;top:${y}px;width:${w}px;height:${h}px"><div class="abs" style="left:${-shiftX}px;top:-620px;width:${SW}px;height:1080px"><div class="bg" style="background-image:url(/bg/frontend-menu.jpg);background-position:50% 50%"></div>${scrim({ edge: "left", frac: 0.6, hold: 0.75, alpha: A.scrims.menu[0].alpha })}</div>${inner}</div>`;
const glassCell = (x, y, w, h, inner) => `<div class="pnl" style="left:${x}px;top:${y}px;width:${w}px;height:${h}px;background:${hexA(A.colors.ink, 0.86)}">${inner}</div>`;
function wordsBoard() {
  let h = boardBg + bcap("Words: main menu, pause menu, Continue, footer actions. Over the real picture and its left scrim.", 48, 28, "k");
  const sts = [["Idle", "", "cream 74 %"], ["Hover = focus (pointer, keys or pad)", "f", "cream 100 %, marker, line draws in"], ["Pressed (80 ms)", "p", "line turns cream, marker 70 %"], ["Disabled", "d", "cream 40 %, no marker, no line, skipped"]];
  const cw = 440, ch = 130;
  sts.forEach(([n, st, d], i) => {
    const x = 48 + (i % 4) * (cw + 12), y = 70;
    h += picCell(x, y, cw, ch, words({ x: 40, y: 41, w: M.itemW, itemH: M.itemH, inset: M.textInsetRef, gutter: A.marker.gutterRef, lineW: M.lineWRef, lineY: M.lineYRef, t: T.word, items: [{ t: "Options", st }] }), 0);
    h += bcap(n, x, y + ch + 10, "k") + bnote(d, x, y + ch + 32, cw);
  });
  // the stack as it sits: marker slides
  h += bcap("The marker is one square that slides between items; each item draws its own line.", 48, 290, "k");
  [0, 1, 2, 3].forEach((fi, i) => {
    const x = 48 + i * (cw + 12);
    h += picCell(x, 320, cw, 330, words({ x: 40, y: 20, w: M.itemW, itemH: M.itemH, gap: M.gapRef, inset: M.textInsetRef, gutter: A.marker.gutterRef, lineW: M.lineWRef, lineY: M.lineYRef, t: T.word, slide: true, items: ["New game", "Continue", "Options", "Controls", "Quit"].map((t, j) => ({ t, st: j === fi ? "f" : "" })) }), 0);
  });
  h += bnote("Words never change weight, size or position when focused (nothing reflows); only opacity, the marker and the line move.", 48, 668, 1700);
  // footer actions, brief Continue, pause item
  h += bcap("Footer actions and Continue use the same words at a smaller size", 48, 716, "k");
  const act = (t, st, x) => words({ x, y: 10, w: Math.ceil(textW(t, T.action)) + 36, items: [{ t, st }], itemH: SH.footerItemH, inset: 30, gutter: 20, lineW: Math.ceil(textW(t, T.action)) + 14, lineY: 29, t: T.action });
  [["Idle", "", "Reset page"], ["Focus", "f", "Back"], ["Pressed", "p", "Back"], ["Disabled", "d", "Reset page"]].forEach(([n, st, t], i) => { const x = 48 + i * (cw + 12); h += glassCell(x, 746, cw, 60, act(t, st, 40)) + bcap(n, x, 816, "k"); });
  const cb = (st, x) => words({ x, y: 4, w: 232, items: [{ t: "Continue", st }], itemH: 48, inset: 40, gutter: 24, lineW: 192, lineY: 37, t: T.wordSmall });
  h += bcap("Continue (mission brief, 18 ref) and pause items use the same parts", 48, 850, "k");
  h += picCell(48, 880, cw, 60, cb("f", 40), 0) + picCell(500, 880, cw, 60, cb("", 40), 0);
  return h;
}
function rowsBoard() {
  let h = boardBg + bcap("Rows: Options and Controls on the smoked-glass sheet (tick + line on the focused row)", 48, 28, "k");
  const cw = 440, rh = 52, cols = [["Idle", ""], ["Focus", "f"], ["Pressed / dragging", "fp"], ["Disabled", "d"]];
  const types = [["Choice", (st) => ctlChoice("Borderless", /p/.test(st) ? "r" : "")], ["Slider", (st) => ctlSlider(0.62, "62%", /p/.test(st))], ["Switch on", (st) => ctlSwitch(true, /p/.test(st))], ["Switch off", (st) => ctlSwitch(false, /p/.test(st))],
    ["Key", (st) => cap("Shift 1", /p/.test(st) ? "p" : "")], ["Key (listening)", (st) => /d/.test(st) ? cap("Shift 1") : cap(/f/.test(st) ? "Press a key" : "Shift 1", /f/.test(st) ? "listen" : "")]];
  cols.forEach(([n], c) => h += bcap(n, 48 + c * (cw + 12), 64, "k"));
  types.forEach(([name, fn], r) => cols.forEach(([n, st], c) => {
    const x = 48 + c * (cw + 12), y = 90 + r * (rh + 8);
    h += glassCell(x, y, cw, rh, rows({ y: 4, left: 0, w: cw, rowH: 44, items: [{ k: "row", label: name, ctl: fn(st), st }] }));
  }));
  h += bcap("Group head", 48, 460, "k");
  h += glassCell(48, 484, cw, 90, rows({ y: 0, left: 0, w: cw, rowH: 44, items: [{ k: "group", label: "Orders" }, { k: "row", label: "Hints", ctl: ctlSwitch(true) }] }));
  h += bcap("Tabs: idle · pointer over · active · active, focus on the strip", 500, 460, "k");
  const tabCell = (x, y, o) => glassCell(x, y, cw, 62, `<div class="tabs" style="top:10px;left:20px;right:auto;width:${cw - 40}px">${tabsHtml(["Display", "Audio", "Camera"], o)}</div>`);
  h += tabCell(500, 484, { active: 0, hover: 1 }) + tabCell(952, 484, { active: 0, focus: true });
  h += bcap("Clash actions, key states", 48, 600, "k");
  h += glassCell(48, 624, cw + 452, 62, `<div class="abs" style="left:14px;top:10px"><span class="cap alt">R Shift</span>${cap("Shift")}${cap("Shift 1", "clash", `<i class="bd2"></i>`)}${cap("Press a key", "listen")}</div>`);
  h += bnote("Key states: alias (dim) · primary · clash (cream 2 px + half badge) · listening (live ring, blinking 1 s).", 48, 700, 900);
  h += bcap("Slider and switch use the marker's square: the knob is the marker, the switch's mark is the marker.", 48, 736, "k");
  return h;
}
function tabsHtml(names, o) {
  const tt = T.tab; let x = 0, hh = "", ul = "";
  names.forEach((name, i) => {
    const tw = textW(name, tt) + 2 * SH.tabPadX, on = i === o.active, hv = i === o.hover;
    hh += `<div class="tab ${on ? "on" : ""} ${hv ? "hov" : ""}" style="left:${x}px;width:${tw}px"><span class="t up" style="${tyle(tt)}">${name}</span></div>`;
    if (on) ul += `<div class="tabul ${o.focus ? "big" : ""}" style="left:${x}px;width:${tw}px"></div>${o.focus ? `<div class="mk" style="--ms:${A.marker.sizeRef}px;left:${x - 2}px;top:${SH.tabH / 2}px"></div>` : ""}`;
    if (hv && !on) ul += `<div class="tabhov" style="left:${x}px;width:${tw}px"></div>`;
    x += tw + SH.tabGap;
  });
  return hh + ul;
}
// cubic-bezier solver for the timing frames
const bez = (x1, y1, x2, y2) => (t) => { if (t <= 0) return 0; if (t >= 1) return 1; let lo = 0, hi = 1, u = t; for (let i = 0; i < 40; i++) { const x = 3 * (1 - u) * (1 - u) * u * x1 + 3 * (1 - u) * u * u * x2 + u ** 3; if (x < t) lo = u; else hi = u; u = (lo + hi) / 2; } return 3 * (1 - u) * (1 - u) * u * y1 + 3 * (1 - u) * u * u * y2 + u ** 3; };
const easeOut = bez(0.215, 0.61, 0.355, 1), easeIn = bez(0.55, 0.055, 0.675, 0.19);
function timingBoard() {
  const TM = A.timing; let h = boardBg + bcap("Timing: focus moves from Options to Controls (frames at ms after the key press)", 48, 28, "k");
  const frames = [0, 40, 80, 120, 180, 260], cw = 290;
  const lw = M.lineWRef;
  frames.forEach((ms, i) => {
    const t = ms / 1000, x = 48 + i * (cw + 10), y = 64;
    const mkY = 24 + 60 * easeOut(t / TM.markerSlide.s), oldLine = lw * (1 - easeIn(t / TM.lineRetract.s)), newLine = lw * easeOut((t - TM.lineDraw.delay) / TM.lineDraw.s);
    const a1 = 1 - (1 - al.idleWord) * Math.min(1, t / TM.labelFade.s), a0 = al.idleWord + (1 - al.idleWord) * Math.min(1, t / TM.labelFade.s);
    const item = (i2, label, a, line, top) => `<div class="w" style="top:${top}px;height:48px;width:290px;--lw:${lw}px"><div class="lab t shadow" style="left:${M.textInsetRef}px;right:0;opacity:${f(a)};${tyle(T.word)}">${label}</div><div class="ul" style="left:${M.textInsetRef}px;top:${M.lineYRef}px;width:${f(line)}px;transition:none"></div></div>`;
    const list = `<div class="wl" style="left:0;top:20px;width:290px;height:130px">${item(0, "Options", a1, oldLine, 0)}${item(1, "Controls", a0, newLine, 60)}<div class="mk" style="--ms:${A.marker.sizeRef}px;left:${M.textInsetRef - A.marker.gutterRef}px;top:${f(mkY)}px;transition:none"></div></div>`;
    h += picCell(x, y, cw, 150, list, 0) + bcap(ms + " ms", x, y + 160, "k");
  });
  // row version: tick slides, line draws
  h += bcap("Rows: focus moves from Quality to V-sync", 48, 540, "k");
  frames.forEach((ms, i) => {
    const t = ms / 1000, x = 48 + i * (cw + 10), y = 566, rw = cw - 20;
    const tickY = 7 + 44 * easeOut(t / TM.tickSlide.s), oldL = rw * (1 - easeIn(t / TM.lineRetract.s)), newL = rw * easeOut((t - TM.lineDraw.delay) / TM.lineDraw.s);
    const a1 = 1 - (1 - al.idleRow) * Math.min(1, t / TM.labelFade.s) * 0 , mix = Math.min(1, t / TM.labelFade.s);
    const row = (label, top, line, lab) => `<div class="r" style="top:${top}px;height:44px;left:0;right:0"><div class="rlab t" style="left:14px;opacity:${f(lab)};${tyle(T.rowLabel)}">${label}</div><div class="ul" style="width:${f(line)}px;transition:none"></div></div>`;
    h += glassCell(x, y, cw, 110, `<div class="rows" style="left:10px;right:auto;width:${rw}px;top:8px;height:90px">${row("Quality", 0, oldL, 1 - (1 - al.idleRow) * mix)}${row("V-sync", 44, newL, al.idleRow + (1 - al.idleRow) * mix)}<div class="tick" style="top:${f(tickY)}px;height:30px;transition:none"></div></div>`) + bcap(ms + " ms", x, y + 120, "k");
  });
  // curve
  const gx = 48, gy = 280, gw = 360, gh = 200;
  let path = ""; for (let i = 0; i <= 60; i++) { const t = i / 60; path += `${i ? "L" : "M"}${f(gx + t * gw)},${f(gy + gh - easeOut(t) * gh)} `; }
  let pathIn = ""; for (let i = 0; i <= 60; i++) { const t = i / 60; pathIn += `${i ? "L" : "M"}${f(gx + t * gw)},${f(gy + gh - easeIn(t) * gh)} `; }
  h += `<svg class="abs" style="left:0;top:0" width="${SW}" height="1080"><rect x="${gx}" y="${gy}" width="${gw}" height="${gh}" fill="none" stroke="rgba(203,198,195,.3)"/><path d="${path}" stroke="#60e0ea" stroke-width="2" fill="none"/><path d="${pathIn}" stroke="#f7f4ec" stroke-width="1.5" fill="none" stroke-dasharray="4 3"/></svg>`;
  h += bcap("Ease-out-cubic (line) · ease-in-cubic (dashed)", gx, gy + gh + 10, "k");
  const rowsT = [["Marker slide", `${TM.markerSlide.s * 1000} ms`, "ease-out-cubic", "top"], ["Line draw-in", `${TM.lineDraw.s * 1000} ms after ${TM.lineDraw.delay * 1000} ms`, "ease-out-cubic", "width 0 to full, from the label's left edge"], ["Line retract (leaving item)", `${TM.lineRetract.s * 1000} ms`, "ease-in-cubic", "width back to 0"], ["Row tick slide", `${TM.tickSlide.s * 1000} ms`, "ease-out-cubic", "top"], ["Tab line slide", `${TM.tabSlide.s * 1000} ms`, "ease-out-cubic", "left and width"], ["Label opacity", `${TM.labelFade.s * 1000} ms`, "ease-out", "74 % to 100 %"], ["Press", `${TM.pressIn * 1000} ms in, ${TM.pressOut * 1000} ms out`, "ease-out", "line cream, marker 70 %"], ["Switch flip", `${TM.switchFlip * 1000} ms`, "ease-out-cubic", "marker scale 0 to 1"], ["Screen fade", `${TM.screenFade * 1000} ms`, "ease-out", "as built"], ["Menu items in", `${TM.itemFade * 1000} ms, ${TM.itemStagger * 1000} ms apart, rise ${TM.itemRise} ref`, "ease-out", `marker appears after ${TM.markerAppearDelay * 1000} ms, line draws once`]];
  rowsT.forEach((r, i) => { h += `<div class="cap2 k" style="left:460px;top:${280 + i * 24}px;width:230px">${r[0]}</div><div class="note" style="left:700px;top:${278 + i * 24}px;width:600px">${r[1]} · ${r[2]} · ${r[3]}</div>`; });
  return h;
}
const statesSimple = wordsBoard;

// ---- interactivity (live only) ---------------------------------------------------
function wire() {
  if (q.get("static")) return;
  document.querySelectorAll(".wl.slide").forEach((wl) => {
    const items = [...wl.querySelectorAll(".w:not(.d)")], mk = wl.querySelector(".mk"), ih = +wl.dataset.itemh;
    let cur = items.findIndex((e) => /\bf\b/.test(e.className));
    const set = (i) => { items.forEach((e, j) => e.classList.toggle("f", j === i)); cur = i; mk.classList.remove("hide"); mk.style.top = (parseFloat(items[i].style.top) + ih / 2) + "px"; };
    items.forEach((e, i) => { e.addEventListener("pointerenter", () => set(i)); e.addEventListener("pointerdown", () => { e.classList.add("p"); mk.classList.add("p"); }); e.addEventListener("pointerup", () => { e.classList.remove("p"); mk.classList.remove("p"); }); });
    addEventListener("keydown", (ev) => { if (ev.key === "ArrowDown") set((cur + 1) % items.length); if (ev.key === "ArrowUp") set((cur - 1 + items.length) % items.length); if (ev.key === "Enter") { items[cur].classList.add("p"); mk.classList.add("p"); setTimeout(() => { items[cur].classList.remove("p"); mk.classList.remove("p"); }, 120); } });
  });
  document.querySelectorAll(".rows").forEach((rs) => {
    const rw = [...rs.querySelectorAll(".r:not(.d)")], tick = rs.querySelector(".tick"), rh = +rs.dataset.rowh;
    let cur = rw.findIndex((e) => /\bf\b/.test(e.className));
    const set = (i) => { rw.forEach((e, j) => e.classList.toggle("f", j === i)); cur = i; tick.style.visibility = "visible"; tick.style.top = (parseFloat(rw[i].style.top) + A.line.tickInsetRef) + "px"; };
    rw.forEach((e, i) => e.addEventListener("pointerenter", () => set(i)));
    if (rs === document.querySelector(".rows")) addEventListener("keydown", (ev) => { if (ev.key === "j") set((cur + 1) % rw.length); if (ev.key === "k") set((cur - 1 + rw.length) % rw.length); });
  });
  document.querySelectorAll(".tab").forEach((t) => t.addEventListener("pointerenter", () => t.classList.add("hov")));
}

// ---- main ---------------------------------------------------------------------
const SCREENS = {
  menu: () => menuScreen(true, +(q.get("f") ?? 2)),
  "menu-nosave": () => menuScreen(false, +(q.get("f") ?? 0)),
  options: () => optionsScreen(q.get("tab") || "Display", +(q.get("f") ?? 3)),
  "options-pause": () => optionsScreen(q.get("tab") || "Display", +(q.get("f") ?? 3), true),
  controls: () => controlsScreen(q.get("mode") || "browse", +(q.get("f") ?? 11)),
  brief: briefScreen, pause: pauseScreen, words: wordsBoard, rowsboard: rowsBoard, timing: timingBoard,
};
stage.innerHTML = SCREENS[S]();
await document.fonts.ready; await Promise.all([...document.fonts].map((f_) => f_.load().catch(() => {})));
stage.innerHTML = SCREENS[S](); wire();
// measuring: every .t's rect (device px) and its effective colour/alpha, for the contrast check
window.__rects = () => [...stage.querySelectorAll(".t")].map((e) => {
  const r = e.getBoundingClientRect(), cs = getComputedStyle(e); let op = 1; for (let p = e; p && p !== document.body; p = p.parentElement) op *= +getComputedStyle(p).opacity;
  const m = cs.color.match(/[\d.]+/g).map(Number); const a = (m[3] ?? 1) * op;
  return { text: e.textContent.slice(0, 40), x: r.x, y: r.y, w: r.width, h: r.height, rgb: m.slice(0, 3), a, size: parseFloat(cs.fontSize) * k };
});
window.__ready = true;
