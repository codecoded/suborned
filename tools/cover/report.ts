// Where the fights lack cover, and what would give it (brief 011, Pulse). The checker (check.ts) says that a fight area fails; this says which posts, the nearest cover that already
// shields each from the car (a mission question: move the post), and where a planter run would, clear of the dressing's keep-clear zones and the site's blocked ground (a world
// question: Vantage's furniture). A proposal is checked by making the file again with the run in it: the post then has a shielding point within reach. Not part of the contract file.
import type { Cover } from '../../contracts/world/cover.schema';
import { inPoly, lineDist, type V } from '../plan/geom';
import { toPlanFn } from '../plan/civic';
import { derive } from '../plan/plan';
import { gridOf, objectsOf, pointsOf, RULES } from './gen';
import { protects } from './select';

type V2 = [number, number];
export type Finding = {
  area: string; side: 'hostile' | 'cell'; post: string; at: V2;
  /** what the plan has under the post: a plot (the square: the block's furniture, Vantage) or a road (the lane: Datum's street, and the post is the mission's to move) */
  on: string;
  nearest?: { id: string; of: string; distM: number };
  proposal?: { piece: string; at: V2; yawDeg: number; reason: string; thenShielded: boolean };
};

const RUN = { lengthM: 4.3, widthM: 0.68, heightM: 1.1 }; // PlanterRun_4_*: kit sizes 4.30-4.37 x 0.68, 1.06-1.18 m high

/** Findings for every post of every area that has no cover, metres in block frame. */
export function findings(cover: Cover, plan: any, block: any, site: any): Finding[] {
  const out: Finding[] = [], objs = objectsOf(plan, block, site), grid = gridOf(site), d = derive(plan), toPlan = toPlanFn(plan.civic);
  const onWhat = (p: V2): string => { const q = toPlan(p as V), plot = d.plots.find(x => inPoly(q, x.polyV)); if (plot) return `plot ${plot.id} (${plot.use})`; const r = d.roads.find(x => lineDist(q, x.line) <= x.w / 2); return r ? `road ${r.id}` : 'open ground outside the plan\'s plots and roads'; };
  const at = (id: string): V2 => { const p = site.places[id], a = p.stand ?? p.at; return [a[0] / 100, a[1] / 100]; };
  // the dressing's keep-clear zones that bar planters: a polygon, or a ring (centre, r0..r1) about a point
  const zones = ((block.dressing?.keepClear ?? []) as { id: string; polygon?: V2[]; ring?: { centre: V2; r0: number; r1: number }; bars: string[] }[]).filter(z => z.bars.includes('planter'))
    .map(z => (p: V2) => (z.polygon ? inPoly(p as V, z.polygon as V[]) : Math.hypot(p[0] - z.ring!.centre[0], p[1] - z.ring!.centre[1]) >= z.ring!.r0 && Math.hypot(p[0] - z.ring!.centre[0], p[1] - z.ring!.centre[1]) <= z.ring!.r1));
  const blockedCm = (x: number, y: number) => !grid.free(Math.floor((x * 100 - grid.x0) / grid.cell), Math.floor((y * 100 - grid.y0) / grid.cell));
  for (const a of cover.areas) {
    const anchor = at(a.anchor);
    for (const h of a.hostileCover) {
      if (h.points.length) continue;
      const post = at(h.post), f: Finding = { area: a.id, side: 'hostile', post: h.post, at: post, on: onWhat(post) };
      // the nearest existing point (any distance) that would shield a body from the car
      let best: Finding['nearest'];
      for (const c of cover.points) if (c.of !== 'car/getaway' && c.q >= RULES.qPartial && protects(c, [anchor[0] * 100, anchor[1] * 100])) {
        const d = Math.hypot(c.at[0] / 100 - post[0], c.at[1] / 100 - post[1]);
        if (!best || d < best.distM) best = { id: c.id, of: c.of, distM: Math.round(d * 10) / 10 };
      }
      f.nearest = best;
      // a planter run broadside to the car, 1.6 m to 4 m from the post toward it, on free ground and clear of the zones that bar planters
      const u = ((): V2 => { const d = Math.hypot(anchor[0] - post[0], anchor[1] - post[1]) || 1; return [(anchor[0] - post[0]) / d, (anchor[1] - post[1]) / d]; })(), along: V2 = [-u[1], u[0]];
      for (const dM of [1.6, 2.4, 3.2, 4.0]) {
        const c: V2 = [post[0] + u[0] * dM, post[1] + u[1] * dM];
        const ring: V2[] = [[-1, -1], [1, -1], [1, 1], [-1, 1]].map(([sx, sy]) => [c[0] + along[0] * sx! * (RUN.lengthM / 2) + u[0] * sy! * (RUN.widthM / 2), c[1] + along[1] * sx! * (RUN.lengthM / 2) + u[1] * sy! * (RUN.widthM / 2)] as V2);
        const samples: V2[] = [];
        for (let i = 0; i <= 8; i++) for (const sy of [-1, 0, 1]) samples.push([c[0] + along[0] * (i / 4 - 1) * (RUN.lengthM / 2) + u[0] * sy * (RUN.widthM / 2), c[1] + along[1] * (i / 4 - 1) * (RUN.lengthM / 2) + u[1] * sy * (RUN.widthM / 2)]);
        if (samples.some(p => blockedCm(p[0], p[1]) || zones.some(z => z(p)) || objs.some(o => inPoly(p as V, o.poly as V[])))) continue;
        const yaw = Math.round((Math.atan2(along[1], along[0]) * 180) / Math.PI);
        const trial = pointsOf([...objs, { id: 'proposal/run', kind: 'planter-run', poly: ring, h: RUN.heightM }], grid);
        const ok = trial.some(t => t.of === 'proposal/run' && Math.hypot(t.at[0] / 100 - post[0], t.at[1] / 100 - post[1]) <= RULES.hostileReachCm / 100 && protects({ ...t, present: undefined }, [anchor[0] * 100, anchor[1] * 100]));
        f.proposal = { piece: 'PlanterRun_4_A', at: [Math.round(c[0] * 100) / 100, Math.round(c[1] * 100) / 100], yawDeg: yaw, reason: `${dM} m from ${h.post} toward the car, broadside to it, outside every zone that bars planters`, thenShielded: ok };
        if (ok) break;
      }
      out.push(f);
    }
  }
  return out;
}
