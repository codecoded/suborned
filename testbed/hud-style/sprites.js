// The kit's sprites for the lab: the real @2x files from /kit/, and body-recoloured copies for the cream / seafoam / ink presets.
// Recolour = keep the chrome rim, screws, seafoam inner line and stripes; replace only the deep-teal body pixels (dark, teal hue)
// by the token colour, keeping the sprite's own shading (so the plate still looks like enamel, not a flat fill).
// If a recoloured body is picked, Glyph repaints it into the kit properly; the export carries the PNG this makes as the target.
const cache = new Map();
const imgs = new Map();
import { COL, TALL, DIVIDERS, divGeom, COLUMNS, columnById } from "./tokens.js";
export const kitUrl = (p) => p.startsWith("/") ? p : `/kit/${p}@2x.png`;
function load(url) {
  if (!imgs.has(url)) imgs.set(url, new Promise((res, rej) => { const i = new Image(); i.onload = () => res(i); i.onerror = () => rej(new Error("no " + url)); i.src = url; }));
  return imgs.get(url);
}
const hex2rgb = (h) => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];
function hsl(r, g, b) {
  r /= 255; g /= 255; b /= 255; const mx = Math.max(r, g, b), mn = Math.min(r, g, b), l = (mx + mn) / 2; let h = 0, s = 0;
  if (mx !== mn) { const d = mx - mn; s = l > 0.5 ? d / (2 - mx - mn) : d / (mx + mn); h = mx === r ? (g - b) / d + (g < b ? 6 : 0) : mx === g ? (b - r) / d + 2 : (r - g) / d + 4; h *= 60; }
  return [h, s, l];
}
const LREF = 0.12;
export async function recolor(path, hex, gain = 1.6) {
  const key = `${path}|${hex}|${gain}`;
  if (cache.has(key)) return cache.get(key);
  const p = (async () => {
    const img = await load(kitUrl(path));
    const c = document.createElement("canvas"); c.width = img.naturalWidth; c.height = img.naturalHeight;
    const x = c.getContext("2d", { willReadFrequently: true }); x.drawImage(img, 0, 0);
    const d = x.getImageData(0, 0, c.width, c.height), a = d.data, T = hex2rgb(hex);
    for (let i = 0; i < a.length; i += 4) {
      if (a[i + 3] < 8) continue;
      const [h, s, l] = hsl(a[i], a[i + 1], a[i + 2]);
      if (h < 130 || h > 210 || l > 0.46 || s < 0.09) continue;
      let w;
      if (l <= 0.33) w = Math.min(1, (0.33 - l) / 0.05) * Math.min(1, (s - 0.09) / 0.05) + (l > 0.28 && s < 0.3 ? 0.5 : 0); // the body
      else if (s < 0.3) w = Math.min(1, (0.46 - l) / 0.05) * Math.min(1, (s - 0.09) / 0.04); // the soft gloss band of the small plates (grey-teal, not the saturated inner line)
      else continue;
      w = Math.min(1, w);
      const f = 1 + gain * (l - LREF);
      for (let k = 0; k < 3; k++) a[i + k] = Math.max(0, Math.min(255, a[i + k] * (1 - w) + T[k] * f * w));
    }
    x.putImageData(d, 0, 0);
    const png = c.toDataURL("image/png");
    const blob = await new Promise((r) => c.toBlob(r, "image/png"));
    return { url: URL.createObjectURL(blob), b64: png.split(",")[1] };
  })();
  cache.set(key, p);
  return p;
}
// the sprite variables the parts use: --spr-<set>-<name> (set: hud = the HUD frames, ov = the overlays)
// the taller column: the kit sprite cut at COL.cuts with one body row repeated d2x times in each gap (nothing redrawn)
const tallCache = new Map();
async function tallColumn(srcUrl, d2x) {
  const key = srcUrl + "|" + d2x; if (tallCache.has(key)) return tallCache.get(key);
  const p = (async () => {
  const img = await load(srcUrl), c = document.createElement("canvas"); c.width = COL.w; c.height = COL.h + COL.cuts.length * d2x;
  const x = c.getContext("2d"); x.imageSmoothingEnabled = false;
  // recipe tallColumn/2 (round 6): the block that fills each gap is no longer the one row 222 copied d2x times (it showed as a lighter band under the rivets and at the foot, because the rows next to
  // each ring are shaded by it). It is the plain row 222 (sideways smoothed inside the body so no grain streaks), tone-matched to the gap's own edge columns, eased into the row just above the cut
  // over the first EASE rows and into the row at the cut over the last EASE rows. Outside the body (rim, stripes, ring flanks) each row runs from the row above to the row below.
  const sc = document.createElement("canvas"); sc.width = COL.w; sc.height = COL.h; const sx = sc.getContext("2d", { willReadFrequently: true }); sx.drawImage(img, 0, 0);
  const sd = sx.getImageData(0, 0, COL.w, COL.h).data, W4 = COL.w * 4, EASE = 6, EDGE = [[17, 30], [187, 200]];
  const rowOf = (y) => Float32Array.from(sd.subarray(y * W4, (y + 1) * W4));
  const smooth = (r) => { const o = Float32Array.from(r); for (let px = COL.body[0]; px <= COL.body[1]; px++) for (let k = 0; k < 4; k++) { let sum = 0, n = 0; for (let j = -5; j <= 5; j++) { const q = px + j; if (q < COL.body[0] || q > COL.body[1]) continue; sum += r[q * 4 + k]; n++; } o[px * 4 + k] = sum / n; } return o; };
  const edgeMean = (r, k) => { let sum = 0, n = 0; for (const [a, b] of EDGE) for (let px = a; px <= b; px++) { sum += r[px * 4 + k]; n++; } return sum / n; };
  const plain0 = smooth(rowOf(COL.row)), ss = (t) => t * t * (3 - 2 * t);
  let y0 = 0, dy = 0;
  for (const cut of COL.cuts) {
    x.drawImage(img, 0, y0, COL.w, cut - y0, 0, dy, COL.w, cut - y0); dy += cut - y0;
    const A = smooth(rowOf(cut - 1)), B = smooth(rowOf(cut)), gain = [0, 1, 2].map((k) => Math.max(0.6, Math.min(1.4, (edgeMean(A, k) + edgeMean(B, k)) / 2 / Math.max(1, edgeMean(plain0, k)))));
    const blk = x.createImageData(COL.w, d2x), o = blk.data;
    for (let r = 0; r < d2x; r++) {
      const t = (r + 0.5) / d2x, wa = ss(Math.max(0, 1 - (r + 0.5) / EASE)), wb = ss(Math.max(0, 1 - (d2x - r - 0.5) / EASE));
      for (let px = 0; px < COL.w; px++) for (let k = 0; k < 4; k++) {
        const i = px * 4 + k, inBody = px >= COL.body[0] && px <= COL.body[1];
        let v;
        if (k === 3) v = 255;
        else if (inBody) { const P = plain0[i] * gain[k]; v = P + (A[i] - P) * wa + (B[i] - P) * wb; }
        else v = A[i] * (1 - t) + B[i] * t;
        o[r * W4 + i] = v;
      }
    }
    x.putImageData(blk, 0, dy); dy += d2x; y0 = cut;
  }
  x.drawImage(img, 0, y0, COL.w, COL.h - y0, 0, dy, COL.w, COL.h - y0);
  const blob = await new Promise((r) => c.toBlob(r, "image/png")); return { url: URL.createObjectURL(blob), b64: c.toDataURL("image/png").split(",")[1] };
  })();
  tallCache.set(key, p); return p;
}
const FRAMES = { pill_status: "panels/pill_status", panel_dark: "panels/panel_dark", plate_dark: "markers/plate_dark", panel_strip: "panels/panel_strip", panel_slot: "panels/panel_slot" };
export async function applySprites(root, tk) {
  const set = (name, url) => root.style.setProperty(name, `url(${url})`);
  const out = {}; // exported recoloured sprites: name -> base64
  for (const [set_, color] of [["hud", tk.body.hud], ["ov", tk.body.overlay]]) {
    const all = FRAMES; // the roster column is handled below (it has variants)
    for (const [n, p] of Object.entries(all)) {
      if (!color) set(`--spr-${set_}-${n}`, kitUrl(p));
      else { const r = await recolor(p, color, tk.body.gain); set(`--spr-${set_}-${n}`, r.url); out[`${set_}-${n}@2x.png`] = r.b64; }
    }
  }
  // the roster column, taller (under the portrait / hanging plate), from the same (possibly recoloured) column sprite. Every column variant (the kit's and the repainted ones) is built for every divider,
  // so the boards can show them side by side: --spr-hud-roster_column-under-<divider>-<column>. The picked column also takes the plain names. Only the picked one is exported.
  const pick = columnById(tk.roster.column).id, srcOf = async (c) => { const path = c.url || "roster/roster_column"; return tk.body.hud ? await recolor(path, tk.body.hud, tk.body.gain) : { url: kitUrl(path), b64: null }; };
  for (const c of COLUMNS) {
    const sc = await srcOf(c), srcCol = sc.url, sel = c.id === pick;
    set(`--spr-hud-roster_column-${c.id}`, srcCol);
    for (const [lay, T] of Object.entries(TALL)) { const t = await tallColumn(srcCol, T.d2x); set(`--spr-hud-roster_column-${lay}-${c.id}`, t.url); if (sel) { set(`--spr-hud-roster_column-${lay}`, t.url); out[`hud-roster_column-${lay}@2x.png`] = t.b64; } }
    for (const D of DIVIDERS) { const g = divGeom(D.id), t = await tallColumn(srcCol, g.d2x); set(`--spr-hud-roster_column-under-${D.id}-${c.id}`, t.url); if (sel) { set(`--spr-hud-roster_column-under-${D.id}`, t.url); out[`hud-roster_column-under-${D.id}@2x.png`] = t.b64; } }
    if (sel) { set("--spr-hud-roster_column", srcCol); if (sc.b64) out["hud-roster_column@2x.png"] = sc.b64; }
  }
  // discs: dark (kit or recoloured) or the kit's light disc
  const dd = tk.disc.family === "dark" && tk.disc.color ? await recolor("markers/disc_dark_24", tk.disc.color, tk.body.gain) : null;
  const dd30 = tk.disc.family === "dark" && tk.disc.color ? await recolor("markers/disc_dark_30", tk.disc.color, tk.body.gain) : null;
  set("--spr-disc", tk.disc.family === "light" ? kitUrl("markers/disc_light_24") : dd ? dd.url : kitUrl("markers/disc_dark_24"));
  set("--spr-disc30", dd30 ? dd30.url : kitUrl("markers/disc_dark_30"));
  set("--spr-disc-light", kitUrl("markers/disc_light_24"));
  set("--spr-disc-light-sel", kitUrl("markers/disc_light_24_selected")); // the weapon disc that carries a "drawn" cause
  if (dd) { out["disc_dark_24@2x.png"] = dd.b64; out["disc_dark_30@2x.png"] = dd30.b64; }
  return out;
}
