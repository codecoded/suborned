// HUD style lab (brief 046): presets as tabs, tokens as controls, every HUD piece live over the real backdrops, hold animations playable.
import { TARGET_RINGS, FONTS, fontById, FONT_ROLES, PRESETS, presetById, KIT, kc, ANIMS, EASES, RELEASES, CANCELS, BODIES, ease, SCHEMA } from "./tokens.js";
import { applySprites } from "./sprites.js";
import { SCENES, GROUNDS, sceneHTML } from "./scenes.js";
import { WHY, ARROWS, ARROW_SPEC, FLATTEN } from "./tokens.js";
import { RS, SLOTS, rosterCost, delta, colH, isTall, tabFor } from "./parts.js";
import { COL, TALL, DIVIDERS, divGeom, dividerById, GAP_TOP, COLUMNS, columnById } from "./tokens.js";
import { intel as intelHTML } from "./parts.js";

const $ = (s, r = document) => r.querySelector(s);
const stage = $("#stage");
const clone = (o) => JSON.parse(JSON.stringify(o));
const hexRgb = (h) => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];
const lum = (h) => { const [r, g, b] = hexRgb(h); return (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255; };
const isLight = (h) => lum(h) > 0.55;
const kitHexes = new Map(Object.values(KIT).map(([h, s]) => [h.toLowerCase(), s]));

let tk = presetById("revisedA"), baseId = "revisedA", lastId = "built", dirty = false;
let view = { scene: "help-pip", zoom: "fit", ground: "help-pip", intel: false, cause: "drawn", guides: false, theta: 40, ring: "selected", occ: true };
let exportSprites = {}, seq = 0;

// ---------------------------------------------------------------- hash (so a state can be linked and shot headless)
function readHash() {
  const q = new URLSearchParams(location.hash.slice(1));
  if (q.get("p") && PRESETS.some((p) => p.id === q.get("p"))) { baseId = q.get("p"); tk = presetById(baseId); }
  if (q.get("bare") === "1") document.body.classList.add("bare");
  if (q.get("s")) view.scene = q.get("s"); if (q.get("z")) view.zoom = q.get("z"); if (q.get("g")) view.ground = q.get("g"); if (q.get("c")) view.cause = q.get("c"); if (q.get("i")) view.intel = q.get("i") === "1";
  if (q.get("rl")) tk.roster.layout = q.get("rl"); if (q.get("dv")) tk.roster.divider = q.get("dv"); if (q.get("rc")) tk.roster.column = q.get("rc");
  if (q.get("f")) tk.font.id = q.get("f");
  if (q.get("a")) view.theta = Number(q.get("a")); if (q.get("rg")) view.ring = q.get("rg"); if (q.get("o")) view.occ = q.get("o") === "1";
  if (q.get("as")) { tk.why.arrow.style = q.get("as"); } if (q.get("cue")) { tk.why.arrow.cue = q.get("cue"); }
}
function writeHash() { const q = new URLSearchParams({ p: baseId, s: view.scene, z: view.zoom, g: view.ground, c: view.cause, i: view.intel ? "1" : "0", a: view.theta, rg: view.ring, o: view.occ ? "1" : "0" }); if (document.body.classList.contains("bare")) q.set("bare", "1"); history.replaceState(null, "", "#" + q); }

// ---------------------------------------------------------------- tokens -> CSS variables
function setVars() {
  const v = (k, x) => stage.style.setProperty(k, x);
  const t = tk.type, a = tk.accent, d = tk.disc, h = tk.hold;
  const rgb = (hex) => hexRgb(hex).join(",");
  v("--main", t.main); v("--main-rgb", rgb(t.main)); v("--dim", t.dim); v("--head", t.head);
  v("--live", a.live); v("--live-rgb", rgb(a.live)); v("--live-ic", a.live); v("--warn", a.warn); v("--hair", a.hair); v("--tail", a.tail);
  v("--disc", d.size + "px"); v("--disc-icon-size", d.iconSize + "px"); v("--disc-icon", d.icon); v("--face", tk.face.size + "px");
  v("--fillc", `rgba(${rgb(h.fill)},${h.alpha})`); v("--wipe", h.fill); v("--shadow", String(tk.rim.shadow));
  v("--rdy", COL.cuts.length * delta(tk.roster.layout, tk.roster.divider) + "px"); v("--sea", kc("seafoam")); v("--chromec", kc("chrome")); v("--pipe", kc("pipEmpty")); v("--tailbody", kc("bodyTeal")); stage.dataset.anim = h.anim; stage.dataset.roster = tk.roster.layout; stage.classList.toggle("guides", !!view.guides); stage.dataset.frame = tk.frame;
  // the HUD frames have their own body: text and live colour follow it
  v("--why", tk.why.size + "px"); v("--why-ratio", String(tk.why.iconRatio)); v("--why-ring", tk.why.ring); v("--pulse", tk.why.pulseMs + "ms");
  const F = fontById(tk.font.id); v("--ff", F.css); v("--fw", String(F.body)); v("--fws", String(F.strong)); v("--fs", String(F.scale));
  const hls = [...stage.querySelectorAll(".hudlayer,.hudwrap")]; if (!hls.length) return;
  const light = isLight(tk.body.hud || KIT.bodyTeal[0]);
  const hm = light ? kc("ink") : kc("cream"), hd = light ? kc("warmInk") : kc("dim"), hv = light ? (lum(tk.accent.live) < 0.45 ? tk.accent.live : kc("deepTeal")) : (lum(tk.accent.live) > 0.5 ? tk.accent.live : kc("turquoiseLight"));
  hls.forEach((hl) => { hl.style.setProperty("--main", hm); hl.style.setProperty("--main-rgb", rgb(hm)); hl.style.setProperty("--dim", hd); hl.style.setProperty("--head", hd);
    hl.style.setProperty("--live", hv); hl.style.setProperty("--live-ic", hv); hl.style.setProperty("--live-rgb", rgb(hv)); });
}

// ---------------------------------------------------------------- render
async function render() {
  const my = ++seq;
  exportSprites = await applySprites(stage, tk);
  if (my !== seq) return;
  const sc = SCENES.find((s) => s.id === view.scene) || SCENES[0];
  stage.innerHTML = sceneHTML(sc, tk, view.ground, view.cause, view);
  if (view.intel && !sc.board && sc.id !== "listen-chip") stage.querySelector(".hudlayer").insertAdjacentHTML("beforeend", intelHTML());
  setVars(); fitRims(); applyZoom(); refreshJump(); writeHash();
  $("#groundL").style.display = sc.board ? "" : "none";
  $("#status").textContent = `${tk.name}${dirty ? " (edited)" : ""} · ${sc.name}`;
  buildTabs();
}
function fitRims() {
  stage.querySelectorAll(".pill").forEach((el) => {
    const r = el.querySelector(".rim rect"); if (!r) return; const w = el.offsetWidth, h = el.offsetHeight, fire = el.classList.contains("fire");
    r.setAttribute("x", 1.5); r.setAttribute("y", 1.5); r.setAttribute("width", w - 3); r.setAttribute("height", h - 3); r.setAttribute("rx", fire ? 7 : h / 2 - 1.5);
    const f = el.querySelector(".pflash"); if (f) f.style.borderRadius = fire ? "4px" : "14px";
  });
}

// ---------------------------------------------------------------- zoom and jump
function applyZoom() {
  const box = $("#scroll");
  const s = view.zoom === "fit" ? Math.min(box.clientWidth / 1920, box.clientHeight / 1080) : Number(view.zoom);
  stage.style.transform = `scale(${s})`; $("#sizer").style.width = 1920 * s + "px"; $("#sizer").style.height = 1080 * s + "px";
  stage.dataset.scale = s;
  document.querySelectorAll("#zoom button").forEach((b) => b.classList.toggle("on", b.dataset.z === String(view.zoom)));
}
function refreshJump() {
  const j = $("#jump"), names = [...new Set([...stage.querySelectorAll("[data-cmp]")].map((e) => e.dataset.cmp))];
  j.innerHTML = `<option value="">piece…</option>` + names.map((n) => `<option>${n}</option>`).join("");
}
function jump(name) {
  const el = stage.querySelector(`[data-cmp="${name}"]`); if (!el) return;
  if (view.zoom === "fit") { view.zoom = "2"; applyZoom(); }
  const s = Number(stage.dataset.scale), sr = stage.getBoundingClientRect(), r = el.getBoundingClientRect(), box = $("#scroll");
  box.scrollTo({ left: (r.left - sr.left) + r.width / 2 - box.clientWidth / 2, top: (r.top - sr.top) + r.height / 2 - box.clientHeight / 2 });
}

// ---------------------------------------------------------------- hold (press and hold any live pill)
const holds = new Map();
let raf = 0;
function loop(now) {
  let any = false;
  for (const [el, h] of holds) {
    any = true;
    if (h.state === "hold") {
      const t = (now - h.t0) / tk.hold.ms; setP(el, ease(tk.hold.ease, t));
      if (t >= 1) { h.state = "ready"; setP(el, 1); el.dataset.state = "ready"; el.querySelector(".phead").textContent = "Let go"; }
    } else if (h.state === "drain") {
      const t = (now - h.t0) / 260; setP(el, h.from * (1 - ease("ease-out", t)));
      if (t >= 1) end(el);
    } else if (h.state === "fired") {
      const t = (now - h.t0) / 420; setP(el, 1 - ease("ease-out", t)); if (t >= 1) end(el);
    }
  }
  raf = any ? requestAnimationFrame(loop) : 0;
}
function setP(el, p) {
  el.style.setProperty("--p", p.toFixed(4));
  const N = tk.hold.segments, on = Math.floor(p * N + 1e-6);
  el.querySelectorAll(".hl-ticks i").forEach((i, k) => i.classList.toggle("on", k < on));
}
function end(el) { holds.delete(el); el.dataset.state = "offer"; setP(el, 0); el.querySelector(".phead").textContent = el.dataset.head; el.classList.remove("canceling"); }
stage.addEventListener("pointerdown", (e) => {
  const el = e.target.closest(".pill[data-live]"); if (!el || holds.has(el) || e.button !== 0) return;
  el.classList.remove("rel-snap", "rel-ripple", "rel-flash", "canceling");
  holds.set(el, { state: "hold", t0: performance.now() }); el.dataset.state = "hold"; el.setPointerCapture?.(e.pointerId);
  if (!raf) raf = requestAnimationFrame(loop);
});
function release(e) {
  for (const [el, h] of holds) {
    if (h.state === "ready") {
      h.state = "fired"; h.t0 = performance.now(); el.dataset.state = "hold";
      void el.offsetWidth; if (tk.hold.release !== "none") el.classList.add("rel-" + tk.hold.release);
      el.querySelector(".phead").textContent = el.dataset.head; setTimeout(() => el.classList.remove("rel-snap", "rel-ripple", "rel-flash"), 700);
    } else if (h.state === "hold") {
      const from = Number(el.style.getPropertyValue("--p")) || 0;
      if (tk.hold.cancel === "drain") { h.state = "drain"; h.t0 = performance.now(); h.from = from; el.dataset.state = "hold"; }
      else { void el.offsetWidth; el.classList.add("canceling"); setTimeout(() => end(el), 0); }
    }
  }
}
addEventListener("pointerup", release); addEventListener("pointercancel", release);

// ---------------------------------------------------------------- the panel (token controls)
function getPath(o, p) { return p.split(".").reduce((a, k) => a[k], o); }
function setPath(p, v) { const ks = p.split("."), last = ks.pop(); ks.reduce((a, k) => a[k], tk)[last] = v; dirty = true; render(); buildPanel(); }
function colorRow(label, path, { none } = {}) {
  const cur = getPath(tk, path), seen = new Set();
  const sw = Object.entries(KIT).filter(([, [h]]) => !seen.has(h) && seen.add(h)).map(([n, [h, s]]) => `<i style="background:${h}" title="${n} ${h} · ${s}" data-v="${h}" class="${cur === h ? "on" : ""}"></i>`).join("");
  const nosrc = cur && !kitHexes.has(cur.toLowerCase()) ? `<span class="nosrc" title="no source in the kit or the style contract">no source</span>` : "";
  return `<div class="row" data-path="${path}"><label>${label}</label><div class="sw">${none ? `<i class="none ${cur === null ? "on" : ""}" title="the kit's own teal" data-v=""></i>` : ""}${sw}<input type="color" value="${cur || "#0c3434"}" title="custom colour (lab only until it has a source)">${nosrc}</div></div>`;
}
const rangeRow = (label, path, min, max, step, unit = "") => { const v = getPath(tk, path); return `<div class="row"><label>${label}</label><input type="range" min="${min}" max="${max}" step="${step}" value="${v}" data-path="${path}"><output>${v}${unit}</output></div>`; };
const selectRow = (label, path, opts) => { const v = getPath(tk, path); return `<div class="row"><label>${label}</label><select data-path="${path}">${opts.map((o) => { const [k, n] = Array.isArray(o) ? o : [o, o]; return `<option value="${k}" ${k === v ? "selected" : ""}>${n}</option>`; }).join("")}</select></div>`; };
function buildPanel() {
  const p = $("#panel");
  p.innerHTML = `
   <div class="note">${tk.note}</div>
   <h3>Frames</h3>
   ${selectRow("Overlay style", "frame", [["kit", "Kit sprites (nine-slice)"], ["flat", "Flat web overlays (0.2.56)"]])}
   ${colorRow("HUD body", "body.hud", { none: true })}
   ${colorRow("Overlay body", "body.overlay", { none: true })}
   ${rangeRow("Texture", "body.gain", 0, 3, 0.1)}
   ${rangeRow("Shadow", "rim.shadow", 0, 2, 0.1)}
   <h3>Type (overlays)</h3>
   ${colorRow("Words", "type.main")}${colorRow("Head word", "type.head")}${colorRow("Dim", "type.dim")}
   <h3>Accent</h3>
   ${colorRow("Live thing", "accent.live")}${colorRow("Running out", "accent.warn")}${colorRow("Hairline", "accent.hair")}${colorRow("Tail body", "accent.tail")}
   <h3>Kind disc and faces</h3>
   ${selectRow("Disc", "disc.family", [["dark", "Dark disc"], ["light", "Light disc"]])}
   ${tk.disc.family === "dark" ? colorRow("Disc body", "disc.color", { none: true }) : ""}
   ${colorRow("Icon", "disc.icon")}
   ${rangeRow("Disc size", "disc.size", 20, 44, 1, " px")}${rangeRow("Icon size", "disc.iconSize", 8, 30, 1, " px")}${rangeRow("Face size", "face.size", 16, 40, 1, " px")}
   <h3>Under fire: why</h3>
   ${selectRow("Show", "why.place", [["both", "Portrait and over the agent"], ["portrait", "Portrait only"], ["head", "Over the agent only"], ["none", "Off"]])}
   ${selectRow("Disc", "why.disc", [["light", "Light disc"], ["dark", "Dark disc"]])}
   ${rangeRow("Size", "why.size", 20, 44, 1, " px")}${rangeRow("Icon", "why.iconRatio", 0.4, 0.8, 0.02)}${rangeRow("Pulse", "why.pulseMs", 400, 2000, 50, " ms")}
   ${colorRow("Pulse ring", "why.ring")}
   <h3>Under fire: ground cue</h3>
   ${selectRow("Style", "why.arrow.style", ARROWS)}${selectRow("Cause disc", "why.arrow.cue", [["head", "Over the head"], ["arc", "Riding on the cue"], ["portrait", "Portrait only"]])}
   ${rangeRow("Weight", "why.arrow.weight", 0.5, 2, 0.1)}${rangeRow("Strength", "why.arrow.alpha", 0.2, 1, 0.05)}${colorRow("Colour", "why.arrow.color")}${colorRow("Edge", "why.arrow.edge")}
   <h3>Type</h3>
   ${selectRow("Font", "font.id", FONTS.map((f) => [f.id, f.name + " (" + f.body + " / " + f.strong + ")"]))}
   <div class="note">${fontById(tk.font.id).note} ${fontById(tk.font.id).licence}.</div>
   <h3>Roster</h3>
   ${selectRow("Badges", "roster.layout", [["under", "Icons under the portrait (column taller)"], ["hang", "Icons on a plate hanging under the ring"], ["railLite", "Rail, no numbers (weapon, status, dots)"], ["rail", "Rail with numbers (strict slots)"], ["inside", "Inside the face (kit band)"], ["asbuilt", "As built 0.2.56 (the problems)"]])}
   ${selectRow("Divider", "roster.divider", DIVIDERS.map((D) => [D.id, D.name]))}
   ${selectRow("Column art", "roster.column", COLUMNS.map((c) => [c.id, c.name + " · " + c.rivets + " rivets"]))}
   <div class="note">${columnById(tk.roster.column).note}</div>
   <h3>Hold</h3>
   ${selectRow("Progress", "hold.anim", ANIMS)}${selectRow("Easing", "hold.ease", EASES)}${rangeRow("Hold time", "hold.ms", 300, 2500, 50, " ms")}
   ${selectRow("Release", "hold.release", RELEASES)}${selectRow("Let go early", "hold.cancel", CANCELS)}
   ${colorRow("Progress", "hold.fill")}${rangeRow("Strength", "hold.alpha", 0.15, 1, 0.05)}${rangeRow("Ticks", "hold.segments", 4, 20, 1)}
   <div class="btns"><button class="btn" id="reset">Reset to preset</button><button class="btn" id="copy">Copy JSON</button><button class="btn" id="dl">Download JSON</button><button class="btn primary" id="export">Export as picked</button></div>
   <div id="exportMsg" class="note toast"></div>`;
  p.querySelectorAll("[data-path]").forEach((el) => {
    const path = el.dataset.path;
    if (el.classList.contains("row")) {
      el.querySelectorAll(".sw i").forEach((i) => i.addEventListener("click", () => setPath(path, i.dataset.v === "" ? null : i.dataset.v)));
      el.querySelector("input[type=color]").addEventListener("input", (e) => { setPathQuiet(path, e.target.value); });
      el.querySelector("input[type=color]").addEventListener("change", () => buildPanel());
    } else if (el.tagName === "INPUT") el.addEventListener("input", () => { el.nextElementSibling.textContent = el.value; setPathQuiet(path, Number(el.value)); });
    else el.addEventListener("change", () => setPath(path, el.value));
  });
  $("#reset").onclick = () => { tk = presetById(baseId); dirty = false; render(); buildPanel(); };
  $("#copy").onclick = async () => { await navigator.clipboard?.writeText(JSON.stringify(exported(), null, 2)); msg("Copied the tokens JSON."); };
  $("#dl").onclick = () => download(`${tk.id || "custom"}.tokens.json`, JSON.stringify(exported(), null, 2));
  $("#export").onclick = doExport;
}
function setPathQuiet(path, v) { const ks = path.split("."), last = ks.pop(); ks.reduce((a, k) => a[k], tk)[last] = v; dirty = true; render(); }
const msg = (t) => { const m = $("#exportMsg"); if (m) m.textContent = t; };
function download(name, text) { const a = document.createElement("a"); a.href = URL.createObjectURL(new Blob([text], { type: "application/json" })); a.download = name; a.click(); }

// ---------------------------------------------------------------- export (what Latch loads)
function exported() {
  const out = clone(tk); out.id = dirty ? `${baseId}-edited` : baseId; out.derivedFrom = baseId; out.edited = dirty;
  const light = isLight(tk.body.hud || KIT.bodyTeal[0]);
  out.resolved = { // the HUD frames' own text colours follow their body; overlays use `type`
    hudText: light ? kc("ink") : kc("cream"), hudDim: light ? kc("warmInk") : kc("dim"), hudLive: light ? (lum(tk.accent.live) < 0.45 ? tk.accent.live : kc("deepTeal")) : (lum(tk.accent.live) > 0.5 ? tk.accent.live : kc("turquoiseLight")),
    holdFill: `rgba(${hexRgb(tk.hold.fill).join(",")},${tk.hold.alpha})`, easingCurve: tk.hold.ease };
  out.sources = {}; const add = (h) => { if (h) out.sources[h] = kitHexes.get(h.toLowerCase()) || "NO SOURCE (lab-only)"; };
  [tk.body.hud, tk.body.overlay, tk.disc.color, tk.disc.icon, ...Object.values(tk.type), ...Object.values(tk.accent), tk.hold.fill, tk.why.ring, tk.why.arrow.color, tk.why.arrow.edge].forEach(add);
  out.recolor = { algorithm: "body-recolor/1", note: "Frame sprites keep chrome rim, screws, inner line and stripes; only deep-teal body pixels (hue 130-210, L < 0.31, S > 0.1) take the body colour, shaded by the sprite's own lightness: out = target * (1 + gain * (L - 0.12)). The exported PNGs are the target art; Glyph repaints them into the kit if picked.", gain: tk.body.gain,
    sprites: Object.keys(exportSprites) };
  const costs = Object.fromEntries(["asbuilt", "inside", "rail", "railLite", "under", "hang"].map((l) => [l, rosterCost(l, tk.roster.divider)]));
  out.rosterSpec = { note: "px at 1080p; column drawn at 0.79 of its 109 x 416 art. Slot rects are [x, y, w, h] relative to the rail tab origin (rail), the row (under), the plate (hang) or the face window top-left (inside). tab = the anchor: { dx, dy (from the ring centre), w, h }.", geometry: RS, slots: SLOTS, layout: tk.roster.layout, divider: tk.roster.divider, columnArt: { picked: tk.roster.column, note: "The repainted roster column sprites (same 218 x 833 @2x, same ring centres and cut rows as the kit sprite; the stretch recipe below applies to each). Repainted from the kit sprite by testbed/hud-style/prep/roster_column.py; provenance in testbed/hud-style/sprites/PROVENANCE.json.", options: Object.fromEntries(COLUMNS.map((c) => [c.id, { name: c.name, rivets: c.rivets, sprite: c.url ? `export/sprites/roster_column_${c.id}@2x.png` : "kit roster/roster_column@2x.png", note: c.note }])) }, cost: costs,
    weapon: { ...tk.roster.weapon, sizes: { under: 16, hang: 15, railLite: 20, rail: 20, inside: 17 }, rule: "one weapon pictogram per portrait, in the weapon slot only; no cause, status or badge repeats the gun" },
    column: { recipe: "tallColumn/2", note: "The picked roster column sprite (columnArt; 218 x 833 @2x) is cut at the clean body rows in each gap and a block of `d2x` rows is inserted in each of the four gaps (three between the rings, one before the foot): rings, rims, caps and stripes are the sprite's own pixels. Block row r = the plain row `row` (smoothed sideways inside `body`), tone-matched per channel to the gap (ratio of the edge columns of rows cut-1 and cut to those of `row`), eased into row cut-1 over the first `ease` rows and into row cut over the last `ease` rows (smoothstep); outside `body` a linear blend from row cut-1 to row cut.", ease: 6, edgeColumns: [[17, 30], [187, 200]], ...COL, layouts: Object.fromEntries(Object.entries(TALL).map(([l, t]) => [l, { d2x: t.d2x, d: +t.d.toFixed(2), height: +colH(l).toFixed(2), ringCentres: RS.YC.map((_, k) => +(RS.YC[k] + k * t.d).toFixed(2)), bandOffset: 36.05, bandCentre: "ring centre + 36.05 + d / 2" }])),
      // the `under` column: one cut per divider (its gap), listed with the geometry the divider needs. The picked one is roster.divider.
      under: { picked: tk.roster.divider, gapTop: GAP_TOP, spriteGap: 3.2, note: "px at 1080p from the ring centre of the portrait above. rowDy = top of the 16 px icon row; mark = the divider element (dy from the ring centre, h, w, x from the column's left edge); airAbove / airBelow = space above and below the icon row to the portrait above and the next one; a3 = air between the mark and the next portrait. No mark under the last portrait.",
        dividers: Object.fromEntries(DIVIDERS.map((D) => { const g = divGeom(D.id); return [D.id, { name: D.name, source: D.src, note: D.note, ...g, columnHeight: +colH("under", D.id).toFixed(2), ringCentres: RS.YC.map((_, k) => +(RS.YC[k] + k * g.d).toFixed(2)) }]; })) } } };
  out.ground = { note: "Ground markers are decals in the ground plane, drawn under anything standing in front of them (Latch: depth or stencil so the agent hides the back half of the ring and of the cue). Units for the cue: the selected ring's outer radius = 100; angle = ground-plane degrees toward the shooter.", flatten: FLATTEN, cueSpec: ARROW_SPEC, ringPlayPx: 19, occludedByAgent: true };
  out.targetRings = TARGET_RINGS;
  const F = fontById(tk.font.id), r5 = (x) => Math.round(x * 2) / 2;
  out.fontSpec = { note: "HUD type: pills, chips, plates, clock bar, feed card head, Intel head. Barlow stays for speech and Intel lines. Sizes are px at 1080p: effective = nominal x scale (matches the x-height of Jost) rounded to 0.5. Static TTFs live in testbed/hud-style/fonts/; the digits are already tabular (Unity has no tnum switch).",
    id: F.id, family: F.family, body: { weight: F.body, file: F.files.body }, strong: { weight: F.strong, file: F.files.strong }, scale: F.scale, licence: F.licence, source: F.source, tabular: F.tabular,
    sizes: Object.fromEntries(Object.entries(FONT_ROLES).map(([k, n]) => [k, { nominal: n, effective: r5(n * F.scale) }])),
    strongRoles: ["clock", "head word (Hold / Let go)", "counter", "Intel head", "feed head", "goal name", "status word", "clock bar values"],
    options: FONTS.map((f) => ({ id: f.id, name: f.name, family: f.family, body: f.body, strong: f.strong, scale: f.scale, licence: f.licence, files: f.files })) };
  out.exportedAt = new Date().toISOString();
  return out;
}
async function doExport() {
  const body = { name: baseId + (dirty ? "-edited" : ""), tokens: exported(), sprites: exportSprites };
  try { const r = await (await fetch("/api/export", { method: "POST", body: JSON.stringify(body) })).json(); msg("Wrote " + r.wrote.join(", ")); }
  catch { download(`${tk.id}.tokens.json`, JSON.stringify(body.tokens, null, 2)); msg("No server to write to; downloaded the JSON."); }
}

// ---------------------------------------------------------------- tabs, scene bar, keys
function buildTabs() {
  const t = $("#tabs");
  t.innerHTML = PRESETS.map((p) => `<button data-id="${p.id}" class="${p.id === baseId && !dirty ? "on" : ""}" title="${p.note.replace(/"/g, "'")}">${p.name}</button>`).join("") + (dirty ? `<button class="on custom">Edited: ${tk.name}</button>` : "");
  t.querySelectorAll("button[data-id]").forEach((b) => (b.onclick = () => pick(b.dataset.id)));
}
function pick(id) { if (id !== baseId) lastId = baseId; baseId = id; tk = presetById(id); dirty = false; render(); buildPanel(); }
$("#scene").innerHTML = SCENES.map((s) => `<option value="${s.id}">${s.name}</option>`).join("");
$("#ground").innerHTML = GROUNDS.map(([k, n]) => `<option value="${k}">${n}</option>`).join("");
$("#scene").onchange = (e) => { view.scene = e.target.value; render(); };
$("#ground").onchange = (e) => { view.ground = e.target.value; render(); };
$("#cause").innerHTML = Object.entries(WHY).map(([k, n]) => `<option value="${k}">${k}: ${n}</option>`).join("");
$("#cause").onchange = (e) => { view.cause = e.target.value; render(); };
$("#guidesOn").onchange = (e) => { view.guides = e.target.checked; stage.classList.toggle("guides", view.guides); };
$("#theta").value = view.theta; $("#thetaV").textContent = view.theta + "°"; $("#ring").value = view.ring; $("#occ").checked = view.occ;
$("#theta").oninput = (e) => { view.theta = Number(e.target.value); $("#thetaV").textContent = view.theta + "°"; render(); };
$("#ring").onchange = (e) => { view.ring = e.target.value; render(); };
$("#occ").onchange = (e) => { view.occ = e.target.checked; render(); };
$("#intelOn").onchange = (e) => { view.intel = e.target.checked; render(); };
$("#jump").onchange = (e) => { if (e.target.value) jump(e.target.value); e.target.value = ""; };
document.querySelectorAll("#zoom button").forEach((b) => (b.onclick = () => { view.zoom = b.dataset.z; applyZoom(); writeHash(); }));
addEventListener("resize", applyZoom);
addEventListener("keydown", (e) => { if (e.key === "\\") pick(lastId); });
addEventListener("error", (e) => { $("#errors").textContent += e.message + "\n"; });
addEventListener("unhandledrejection", (e) => { $("#errors").textContent += String(e.reason) + "\n"; });

readHash();
$("#scene").value = view.scene; $("#cause").value = view.cause; $("#ground").value = view.ground; $("#intelOn").checked = view.intel;
await render(); buildPanel();
const holdAt = (p, sel = ".pill[data-live]") => { const el = stage.querySelector(sel); holds.delete(el); el.dataset.state = p >= 1 ? "ready" : p > 0 ? "hold" : "offer"; setP(el, p); el.querySelector(".phead").textContent = p >= 1 ? "Let go" : el.dataset.head; };
window.__lab = { exportNow: doExport, exported, holdAt, async set(path, v) { setPathQuiet(path, v); await new Promise((r) => setTimeout(r, 60)); await render(); }, pick, view, get tk() { return tk; }, async scene(id, zoom) { view.scene = id; if (zoom) view.zoom = zoom; $("#scene").value = id; await render(); }, async zoom(z) { view.zoom = z; applyZoom(); }, render, jump,
  ready: true };
