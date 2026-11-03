// Reference sampler for suborned.presentation/1 (contracts/world/presentation.schema.ts): where everything is at time t. Latch's Unity mover
// should give the same answer; the tests use this to check the file's behaviour (on the ground, on the lanes, seamless at the period).
import type { Presentation } from '../../contracts/world/presentation.schema';

export type Pose = { id: string; model: string; x: number; y: number; z: number; heading: number; moving: boolean };
const mod = (a: number, m: number) => ((a % m) + m) % m;

/** cumulative lengths of a closed xyz loop (index i = start of point i; last entry = total incl. the closing segment) */
const loopCum = new WeakMap<object, number[]>();
function cumOf(l: { xyz: number[] }) {
  let c = loopCum.get(l); if (c) return c;
  c = [0]; const n = l.xyz.length / 3;
  for (let i = 1; i <= n; i++) { const a = (i - 1) * 3, b = (i % n) * 3; c.push(c[i - 1] + Math.hypot(l.xyz[b] - l.xyz[a], l.xyz[b + 1] - l.xyz[a + 1])); }
  loopCum.set(l, c); return c;
}
export function onLoop(l: { xyz: number[] }, s: number) {
  const c = cumOf(l), n = l.xyz.length / 3, L = c[n]; s = mod(s, L);
  let lo = 0, hi = n; while (hi - lo > 1) { const m = (lo + hi) >> 1; if (c[m] <= s) lo = m; else hi = m; }
  const a = lo * 3, b = ((lo + 1) % n) * 3, f = (s - c[lo]) / ((c[lo + 1] - c[lo]) || 1), dx = l.xyz[b] - l.xyz[a], dy = l.xyz[b + 1] - l.xyz[a + 1];
  return { x: l.xyz[a] + dx * f, y: l.xyz[a + 1] + dy * f, z: l.xyz[a + 2] + (l.xyz[b + 2] - l.xyz[a + 2]) * f, heading: Math.atan2(dy, dx) };
}
/** a timed route at d seconds since its start: linear between points; heading from the segment, held through a dwell */
export function onRoute(r: { xy: number[]; t: number[] }, d: number) {
  const t = r.t, n = t.length; if (d < 0 || d > t[n - 1]) return null;
  let lo = 0, hi = n - 1; while (hi - lo > 1) { const m = (lo + hi) >> 1; if (t[m] <= d) lo = m; else hi = m; }
  const f = (d - t[lo]) / ((t[hi] - t[lo]) || 1), ax = r.xy[2 * lo], ay = r.xy[2 * lo + 1], bx = r.xy[2 * hi], by = r.xy[2 * hi + 1];
  let i = lo, dx = bx - ax, dy = by - ay; // a dwell (same point twice): look back for the last real segment
  while (Math.hypot(dx, dy) < 1e-6 && i > 0) { dx = r.xy[2 * i] - r.xy[2 * i - 2]; dy = r.xy[2 * i + 1] - r.xy[2 * i - 1]; i--; }
  return { x: ax + (bx - ax) * f, y: ay + (by - ay) * f, heading: Math.atan2(dy, dx), moving: Math.hypot(bx - ax, by - ay) > 1e-6 };
}
export function sample(p: Presentation, t: number): Pose[] {
  const P = p.clock.periodSec, tau = mod(t, P), out: Pose[] = [];
  for (const m of p.timed) {
    const r = p.routes[m.route], q = onRoute(r, mod(tau - m.t0, P)); if (!q) continue;
    out.push({ id: m.id, model: m.model, x: q.x, y: q.y, z: r.z, heading: q.heading, moving: q.moving });
  }
  for (const m of p.looping) {
    const l = p.loops[m.loop], q = onLoop(l, m.s0 + m.v * tau), h = q.heading + (m.v < 0 ? Math.PI : 0);
    out.push({ id: m.id, model: m.model, x: q.x - Math.sin(h) * m.lat, y: q.y + Math.cos(h) * m.lat, z: q.z, heading: h, moving: true });
  }
  for (const s of p.still) out.push({ id: s.id, model: s.model, x: s.at[0], y: s.at[1], z: s.z, heading: s.heading, moving: false });
  return out;
}
