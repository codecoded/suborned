// A small z-buffer renderer for the visibility study (brief 011, Vantage): extruded rings and flat ground seen by an orthographic
// camera at the play camera's angles (GDD §10: yaw 30, pitch 38; any zoom), flat-shaded under the sun of light.json (yaw 330),
// outlined where faces meet, written as PNG. Same projection as tools/missions/sight.ts, so a marker drawn with `project` lands on
// the pixel the renderer drew. Study pictures only: the look is Unity's (Latch's plates).
import { deflateSync } from 'node:zlib';
import type { V } from '../../../tools/plan/geom';
import { type View, depth, project } from '../../../tools/missions/sight';

export type RGB = [number, number, number];
export type Solid = { id: string; ring: V[]; z0: number; z1: number; color: RGB; ghost?: boolean };
export type Flat = { ring: V[]; z: number; color: RGB };

const SUN = (() => { const y = (330 * Math.PI) / 180, e = (52 * Math.PI) / 180; return [Math.sin(y) * Math.cos(e), Math.cos(y) * Math.cos(e), Math.sin(e)] as const; })();

/** Ear clipping (rings are simple polygons). */
function triangulate(ring: V[]): [number, number, number][] {
  const n = ring.length, area = ring.reduce((s, p, i) => s + p[0] * ring[(i + 1) % n]![1] - ring[(i + 1) % n]![0] * p[1], 0), ccw = area > 0;
  const idx = Array.from({ length: n }, (_, i) => i), out: [number, number, number][] = [];
  const cross = (a: V, b: V, c: V) => (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]);
  const inTri = (p: V, a: V, b: V, c: V) => { const d1 = cross(a, b, p), d2 = cross(b, c, p), d3 = cross(c, a, p); return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0)); };
  let guard = 0;
  while (idx.length > 3 && guard++ < 10000) {
    let cut = false;
    for (let i = 0; i < idx.length; i++) {
      const a = ring[idx[(i + idx.length - 1) % idx.length]!]!, b = ring[idx[i]!]!, c = ring[idx[(i + 1) % idx.length]!]!, k = cross(a, b, c);
      if ((ccw ? k : -k) <= 1e-9) continue;
      if (idx.some(j => { const p = ring[j]!; return p !== a && p !== b && p !== c && inTri(p, a, b, c); })) continue;
      out.push([idx[(i + idx.length - 1) % idx.length]!, idx[i]!, idx[(i + 1) % idx.length]!]); idx.splice(i, 1); cut = true; break;
    }
    if (!cut) break;
  }
  if (idx.length === 3) out.push([idx[0]!, idx[1]!, idx[2]!]);
  return out;
}

export type Frame = { focus: V; view: View; size: V; ss?: number };

/** Render `solids` and `flats` into RGBA bytes at size × ss, then box-filter to size. */
export function render(f: Frame, flats: Flat[], solids: Solid[], sky: RGB = [236, 231, 222]): Uint8Array {
  const ss = f.ss ?? 2, W = f.size[0] * ss, H = f.size[1] * ss, view = f.view;
  const P = (p: V, z: number) => { const s = project(p, z, f.focus, view, [W, H]); return [s[0], s[1], depth(p, z, view)] as const; };
  const zb = new Float64Array(W * H).fill(Infinity), col = new Uint8Array(W * H * 3), face = new Int32Array(W * H).fill(-1);
  for (let i = 0; i < W * H; i++) { col[i * 3] = sky[0]; col[i * 3 + 1] = sky[1]; col[i * 3 + 2] = sky[2]; }
  let faceId = 0;
  const tri = (a: readonly [number, number, number], b: readonly [number, number, number], c: readonly [number, number, number], rgb: RGB, id: number, ghost = false) => {
    const x0 = Math.max(0, Math.floor(Math.min(a[0], b[0], c[0]))), x1 = Math.min(W - 1, Math.ceil(Math.max(a[0], b[0], c[0])));
    const y0 = Math.max(0, Math.floor(Math.min(a[1], b[1], c[1]))), y1 = Math.min(H - 1, Math.ceil(Math.max(a[1], b[1], c[1])));
    const den = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1]);
    if (Math.abs(den) < 1e-9) return;
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
      const px = x + 0.5, py = y + 0.5;
      const w1 = ((b[1] - c[1]) * (px - c[0]) + (c[0] - b[0]) * (py - c[1])) / den, w2 = ((c[1] - a[1]) * (px - c[0]) + (a[0] - c[0]) * (py - c[1])) / den, w3 = 1 - w1 - w2;
      if (w1 < -1e-6 || w2 < -1e-6 || w3 < -1e-6) continue;
      if (ghost && ((x >> 1) + (y >> 1)) % 3 !== 0) continue; // a screen-door ghost, as the fade draws it
      const z = w1 * a[2] + w2 * b[2] + w3 * c[2], k = y * W + x;
      if (z >= zb[k]! - 1e-4) continue;
      zb[k] = z; face[k] = id; col[k * 3] = rgb[0]; col[k * 3 + 1] = rgb[1]; col[k * 3 + 2] = rgb[2];
    }
  };
  const shade = (rgb: RGB, n: readonly [number, number, number]): RGB => { const l = 0.62 + 0.38 * Math.max(0, n[0] * SUN[0] + n[1] * SUN[1] + n[2] * SUN[2]); return [Math.min(255, rgb[0] * l), Math.min(255, rgb[1] * l), Math.min(255, rgb[2] * l)]; };
  for (const g of flats) {
    const T = triangulate(g.ring), id = faceId++, rgb = shade(g.color, [0, 0, 1]);
    for (const [i, j, k] of T) tri(P(g.ring[i]!, g.z), P(g.ring[j]!, g.z), P(g.ring[k]!, g.z), rgb, id);
  }
  const toCam: V = [-Math.sin((view.yaw * Math.PI) / 180), -Math.cos((view.yaw * Math.PI) / 180)];
  for (const s of solids) {
    const r = s.ring, n = r.length, ccw = r.reduce((a, p, i) => a + p[0] * r[(i + 1) % n]![1] - r[(i + 1) % n]![0] * p[1], 0) > 0;
    for (let i = 0; i < n; i++) {
      const a = r[i]!, b = r[(i + 1) % n]!, dx = b[0] - a[0], dy = b[1] - a[1], L = Math.hypot(dx, dy);
      if (L < 1e-6) continue;
      const nrm: V = ccw ? [dy / L, -dx / L] : [-dy / L, dx / L];
      if (nrm[0] * toCam[0] + nrm[1] * toCam[1] <= 0) continue; // back face
      const rgb = shade(s.color, [nrm[0], nrm[1], 0]), id = faceId++;
      const A = P(a, s.z0), B = P(b, s.z0), C = P(b, s.z1), D = P(a, s.z1);
      tri(A, B, C, rgb, id, s.ghost); tri(A, C, D, rgb, id, s.ghost);
    }
    const T = triangulate(r), id = faceId++, rgb = shade(s.color, [0, 0, 1]);
    for (const [i, j, k] of T) tri(P(r[i]!, s.z1), P(r[j]!, s.z1), P(r[k]!, s.z1), rgb, id, s.ghost);
  }
  // outline where faces meet (darken), then downsample
  for (let y = 0; y < H - 1; y++) for (let x = 0; x < W - 1; x++) {
    const k = y * W + x;
    if (face[k] !== face[k + 1] || face[k] !== face[k + W]) { const near = Math.min(zb[k]!, zb[k + 1]!, zb[k + W]!), m = near === zb[k] ? k : near === zb[k + 1] ? k + 1 : k + W; col[m * 3]! *= 0.55; col[m * 3 + 1]! *= 0.55; col[m * 3 + 2]! *= 0.55; }
  }
  const w = f.size[0], h = f.size[1], out = new Uint8Array(w * h * 4);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    let r = 0, g = 0, b = 0;
    for (let v = 0; v < ss; v++) for (let u = 0; u < ss; u++) { const k = ((y * ss + v) * W + x * ss + u) * 3; r += col[k]!; g += col[k + 1]!; b += col[k + 2]!; }
    const o = (y * w + x) * 4, q = ss * ss; out[o] = r / q; out[o + 1] = g / q; out[o + 2] = b / q; out[o + 3] = 255;
  }
  return out;
}

const CRC = (() => { const t = new Uint32Array(256); for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; t[n] = c >>> 0; } return t; })();
const crc32 = (b: Uint8Array) => { let c = 0xffffffff; for (const x of b) c = CRC[(c ^ x) & 0xff]! ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
/** RGBA bytes → PNG file bytes. */
export function png(rgba: Uint8Array, w: number, h: number): Uint8Array {
  const raw = new Uint8Array((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) { raw[y * (w * 4 + 1)] = 0; raw.set(rgba.subarray(y * w * 4, (y + 1) * w * 4), y * (w * 4 + 1) + 1); }
  const chunk = (type: string, data: Uint8Array) => { const b = new Uint8Array(12 + data.length), dv = new DataView(b.buffer); dv.setUint32(0, data.length); b.set(new TextEncoder().encode(type), 4); b.set(data, 8); dv.setUint32(8 + data.length, crc32(b.subarray(4, 8 + data.length))); return b; };
  const ihdr = new Uint8Array(13), dv = new DataView(ihdr.buffer); dv.setUint32(0, w); dv.setUint32(4, h); ihdr[8] = 8; ihdr[9] = 6;
  const parts = [new Uint8Array([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', new Uint8Array(deflateSync(raw, { level: 6 }))), chunk('IEND', new Uint8Array())];
  const out = new Uint8Array(parts.reduce((s, p) => s + p.length, 0)); let o = 0; for (const p of parts) { out.set(p, o); o += p.length; }
  return out;
}
