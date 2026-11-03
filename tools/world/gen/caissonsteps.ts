// Brief 075 remap (Datum's Skyport and reach note): a caisson's deck stands over the quay. The Skyport apron runs 8 m onto the Skyport Tower's drum and the Market Pier 6 m
// onto the liner berth's, so where each pier's walk (plan.foot) enters a drum the walker met a lip from the quay top (pavement 0.18) to the caisson deck (1.2): a 1.02 m
// wall, not a way in. A flight of steps now climbs it on the walk's own axis, the walk's full width: risers of at most RISER m (the rise split evenly, as the old
// harbour's 0.2 m steps), treads of TREAD m, in limestone (the old harbour's steps, OG-H2). The top step runs on under the drum's curved edge (3 mm under its deck, so
// they never z-fight) so no gap opens between the straight flight and the round drum. Drawn as ground solids (ground.quays), read by no rule: the reach rule already
// joins pier and drum (RCH-1); this makes the joint a stair a person can climb. Steps, not a ramp: 1:12 would take 12 m of the apron.
import type { Mass } from '../../../contracts/world/world.schema';
import type { V } from '../../plan/geom';
import type { Plan } from '../../plan/plan';
import { r3, rv } from './common';

export const RISER = 0.17; // m, the most a riser rises (Approved Document K: 0.15-0.17 for a public stair)
export const TREAD = 0.35; // m, a public stair's going (ADK: 0.28 minimum; the old harbour's were 1.2 m treads on a terrace)

export type StepLog = { flights: string[] };

export function caissonSteps(plan: Plan, pav: number): { masses: Mass[]; log: StepLog } {
  const masses: Mass[] = [], log: StepLog = { flights: [] };
  for (const l of plan.landmarks) {
    if (!l.caisson) continue;
    const c = l.c, R = l.caisson.r, rise = l.caisson.deck - pav;
    if (rise <= 0.01) continue;
    const n = Math.ceil(rise / RISER - 1e-9), riser = rise / n;
    for (const f of plan.foot as { id: string; w: number; pts: number[][] }[]) {
      const pts = f.pts as V[], inside = (p: V) => Math.hypot(p[0] - c[0], p[1] - c[1]) < R;
      for (let i = 1; i < pts.length; i++) {
        const a = pts[i - 1]!, b = pts[i]!;
        if (inside(a) === inside(b)) continue;
        const [o, e] = inside(a) ? [b, a] : [a, b]; // o outside the drum, e inside
        const L = Math.hypot(e[0] - o[0], e[1] - o[1]), u: V = [(e[0] - o[0]) / L, (e[1] - o[1]) / L], nrm: V = [-u[1], u[0]], h = f.w / 2;
        // s along u (from o) where a line at lateral offset q meets the circle, the near side
        const meet = (q: number) => { const px = o[0] + nrm[0] * q - c[0], py = o[1] + nrm[1] * q - c[1], B = px * u[0] + py * u[1], C = px * px + py * py - R * R, D = B * B - C; return D < 0 ? undefined : -B - Math.sqrt(D); };
        const sc = Math.min(...[-h, 0, h].map(meet).filter((x): x is number => x !== undefined)), sw = Math.max(...[-h, 0, h].map(meet).filter((x): x is number => x !== undefined));
        if (!Number.isFinite(sc)) continue;
        const P = (s: number, q: number): V => rv([o[0] + u[0] * s + nrm[0] * q, o[1] + u[1] * s + nrm[1] * q]);
        const box = (s0: number, s1: number): V[] => [P(s0, -h), P(s1, -h), P(s1, h), P(s0, h)];
        const base = r3(pav - 0.02);
        for (let k = 1; k <= n; k++) {
          const s0 = sc - (n - k) * TREAD, s1 = k < n ? s0 + TREAD : sw + 0.5, top = k < n ? pav + k * riser : l.caisson.deck - 0.003;
          masses.push({ id: `steps/${l.id}/${f.id}/${k}`, surface: 'limestone', ring: box(s0, s1), base, height: r3(top - base) });
        }
        log.flights.push(`${f.id} → ${l.id}: ${n} risers of ${riser.toFixed(3)} m (${pav} → ${l.caisson.deck}), treads ${TREAD} m, ${f.w} m wide, foot at ${P(sc - (n - 1) * TREAD, 0).join(', ')}`);
      }
    }
  }
  return { masses, log };
}
