// Brief 013 C: the quiet places, the report points and the response origins of the civic block, placed by rule on the street layer (never for one mission).
//   stash      a Dumpster against the wall beside each service door (civic-block.json `kind: service`), on the side away from the play camera;
//   origins    police posts, until every walkable sample of the block's window is within `reach.origin` m (walk) of a robot dock or a post (the Records Office's doors are posts);
//   report     call boxes (phone-booth items), until every sample is within `reach.report` m of a door of a staffed unit, a phone booth or a call box.
// The ground is the engine's own walk grid (tools/plan/walk-grid.ts), so a box covers what a walker in the sim walks. Candidates are the kerb zones of the roads in the
// window (where every other phone booth stands: plan streetscape.catalogue.phone-booth.zone), tried through Layer.fits, the same tests every street item passes (SF-1, SF-2,
// SF-3, SF-14). Deterministic: same plan, same block, same kit, same items. A block with no service door has none of this (a recorded run's pinned block).
import type { StreetItem, Takedown } from '../../../../contracts/world/world.schema';
import { toPlanFn } from '../../../plan/civic';
import { type V, dist, inPoly, lineDist } from '../../../plan/geom';
import { boxRing, samples9 } from '../../../plan/street-zones';
import { reachField, toBlockFn, walkGrid } from '../../../plan/walk-grid';
import { blockersOf } from '../../../plan/walk';
import { handoverPoints } from '../../civic/handover';
import { QUIET, type Cand, cameraDir, cover, lattice, measure, stashSpot } from '../../civic/quiet';
import { dressingOf } from '../../civic/dress';
import { SITING, cornerOf, junctionCorners } from '../../civic/siting';
import { type Ctx, r3, r4, rv } from '../common';
import { cataloguedFor } from '../kitrefs';
import type { Layer } from './kit';

const STEP = 2; // m between kerb candidates along a road
const CLEAR_POINT = 1.2; // m: nothing on the ground within this of a Handover point (CD-1)

export function dressQuiet(ctx: Ctx, L: Layer): Takedown | undefined {
  const { plan, d, civ } = ctx;
  if (!civ.serviceDoors?.length) return undefined;
  const toBlock = toBlockFn(plan.civic), toPlan = toPlanFn(plan.civic), z = ctx.pav;
  const grid = walkGrid(plan, d, civ), w = grid.win;
  const inWin = (p: V) => { const q = toBlock(p); return q[0] >= w.x0 && q[0] <= w.x1 && q[1] >= w.y0 && q[1] <= w.y1; };
  const { polys } = blockersOf(plan, d, civ);
  const blockers: V[][] = [...polys, ...(civ.solids ?? [])];
  const dr = dressingOf(civ);
  const zones = [...(dr?.keepClear ?? []).filter(k => k.id !== 'service-lane' && k.bars.includes('bollard') && 'polygon' in k).map(k => ({ id: k.id, ring: ('polygon' in k ? k.polygon : []).map(toPlan) })), ...civ.drives.map(x => ({ id: `drive ${x.id}`, ring: x.poly }))]; // a drive bars what its keep-clear zone barred (brief 023)
  const points = handoverPoints(toPlan);
  /** The free cell a walker stands on for a point (plan frame), or the point itself where the plan has built over it (R-REPORT / R-STASH then fail on it). */
  const standOf = (p: V): V => rv(toPlan(grid.tryNearestFree(toBlock(p)) ?? toBlock(p)));
  const sizeOf = (kind: string): [number, number, number] => kind === 'police-post' ? QUIET.size['police-post'] : (ctx.S.catalogue[kind].size as [number, number, number]);

  // ---- stash: a container beside each service door
  // the piece is whichever catalogue piece is used as a `dumpster` and tagged stash (a candidate too: tagging is the world's, adopting is Vela's); a kit with none (a run's older pin) stands no container
  const dump = cataloguedFor('dumpster'), holds = dump?.tags.stash?.holds ?? 0;
  const dsize: [number, number, number] = dump ? [dump.size[0], dump.size[2], dump.size[1]] : [2.1, 1.19, 1.51]; // kit axes (across, up, depth) → item (across, depth, height)
  const stashes: Takedown['stashes'] = [], doorsOut: Takedown['entrances'] = [];
  const all = [...civ.entrances, ...civ.serviceDoors];
  for (const e of all) doorsOut.push({ id: e.id, building: e.building, name: e.name, kind: e.kind, at: rv(e.p), z: e.z, stand: standOf(e.p) });
  for (const e of civ.serviceDoors) {
    const b = civ.buildings.find(x => x.id === e.building)!, doorStash = { id: `stash/${e.id}/door`, kind: 'door' as const, at: rv(e.p), stand: standOf(e.p), holds: QUIET.doorHolds, of: e.id };
    const spot = holds < 1 ? undefined : stashSpot({ door: e, building: b.poly, size: dsize, cam: cameraDir(QUIET.cameraYaw), blockers, zones, points, pointClear: CLEAR_POINT, grid, toBlock, roads: d.roads.filter(r => !r.gyratory), kerbReach: QUIET.kerbReach, fits: (at, yaw) => L.fits('dumpster', at, yaw, z, `civic/${e.building}`, { size: dsize, blocks: true }) });
    const id = `street/civic/stash/${e.id}`;
    if (!spot || !L.add('dumpster', spot.at, spot.yaw, z, `civic/${e.building}`, { size: dsize, id, uncounted: true, blocks: true })) {
      stashes.push({ ...doorStash, bare: holds < 1 ? 'no container: the kit has no piece tagged stash' : `no container: neither against the wall beside ${e.name} nor in a kerb zone within ${QUIET.kerbReach} m of it does a ${dump!.id} (${r3(dsize[0])} x ${r3(dsize[1])} m) pass the street rules (SF-1 clear walking zone, SF-2 crossings and doors, SF-14 quay)` });
      continue;
    }
    const it = L.items[L.items.length - 1]!;
    it.sight = 'blocks'; it.stash = { holds };
    stashes.push(doorStash, { id: `stash/${e.id}/container`, kind: 'container', at: it.at, stand: rv(toPlan(spot.stand)), holds: it.stash.holds, of: e.id, item: id, ring: spot.ring.map(rv) });
  }

  // ---- the ground to cover: every free lattice sample a walker can reach from a door
  const pub = civ.entrances;
  const doorStands = pub.map(e => toBlock(e.p));
  const samples = lattice(grid, doorStands);
  const near = (it: StreetItem) => inWin(it.at);
  const existing = (kind: string) => L.items.filter(i => i.kind === kind && near(i));
  const booths = existing('phone-booth'), docks = existing('civic-robot-dock');

  // ---- kerb candidates: along each road in the window, both sides, in the kerb zone
  const corners = junctionCorners(d.roads, plan.nodes);
  const cands = (kind: string, tag: string): Cand[] => {
    const size = sizeOf(kind), out: Cand[] = [];
    for (const r of d.roads) {
      if (r.gyratory) continue;
      const sec = ctx.S.sections[r.class] as { kerb: number }, off = r.w / 2 + 0.45 + size[1] / 2 + 0.05;
      if (size[1] + 0.5 > sec.kerb) continue; // the kerb zone must hold it (the plan: items ≥ 0.45 m from the kerb face, inside the zone)
      let run = 0;
      for (let i = 1; i < r.line.length; i++) {
        const a = r.line[i - 1]!, b = r.line[i]!, seg = dist(a, b); if (seg < 1e-6) continue;
        const t: V = [(b[0] - a[0]) / seg, (b[1] - a[1]) / seg], nl: V = [-t[1], t[0]];
        for (let s = (STEP - (run % STEP)) % STEP; s < seg; s += STEP) {
          const p: V = [a[0] + t[0] * s, a[1] + t[1] * s];
          for (const side of [1, -1] as const) {
            const at: V = [p[0] + nl[0] * side * off, p[1] + nl[1] * side * off];
            if (!inWin(at)) continue;
            const yaw = Math.atan2(t[1], t[0]) + (side > 0 ? 0 : Math.PI), ring = boxRing(at, size[0], size[1], yaw); // the piece's +Z front faces yaw + 90°: here the kerb's pavement side, so a box opens on the footway, never on the carriageway (it faced the road until 2026-10-19)
            if (!samples9(ring).every(q => grid.free(toBlock(q)))) continue;
            if (d.roads.some(o => o !== r && !o.gyratory && samples9(ring).some(q => lineDist(q, o.line) < o.w / 2 + SITING.mouthKerb))) continue; // a side road's mouth: this road's kerb line runs across its carriageway there, and nothing stands on a road (a post blocks sight, a box is glazed: neither stands on one)
            const corner = cornerOf(r, at, corners), atCorner = !!corner && corner.along >= SITING.setback && corner.along <= SITING.zone;
            if (kind === 'phone-booth' && !atCorner) continue; // a call box stands on a corner's pavement, set back from the corner and in sight of it (tools/world/civic/siting.ts)
            if (civ.bays.some(bay => inPoly(at, bay.poly) || bay.poly.some(q => dist(q, at) < 1.5))) continue;
            if (zones.some(zn => inPoly(at, zn.ring))) continue;
            if (points.some(pt => dist(pt.p, at) < CLEAR_POINT + size[0] / 2)) continue;
            if (!L.fits(kind, at, yaw, z, `civic/${r.id}`, { size, blocks: kind === 'police-post' })) continue;
            out.push({ id: `${tag}@${r3(at[0])},${r3(at[1])}`, at, yaw: r4(Math.atan2(Math.sin(yaw), Math.cos(yaw))), stand: grid.tryNearestFree(toBlock(at)) ?? toBlock(at), edge: `civic/${r.id}`, ...(kind === 'phone-booth' ? { tier: SITING.rank[r.class] ?? 2, near: corner!.along } : atCorner ? { tier: SITING.rank[r.class] ?? 2, near: corner!.along } : { tier: 3 }) }); // a police post is a corner's too (the same pavement, the same set-back); where no corner's ground brings more ground within reach, it stands anywhere in a kerb zone
          }
        }
        run += seg;
      }
    }
    return out;
  };
  const standing: { at: V; kind: string }[] = [...docks, ...booths].map(i => ({ at: i.at, kind: i.kind })); // what already stands for the city's phones and docks: a new one keeps `apart` m off every one of them, of its kind or not
  const made = new Map<Cand, StreetItem>(), count: Record<string, number> = {};
  const put = (kind: string, name: string) => (c: Cand): boolean => {
    if (standing.some(p => dist(p.at, c.at) < QUIET.apart)) return false;
    const id = `street/civic/${name}-${count[name] ?? 0}`;
    if (!L.add(kind, c.at, c.yaw, z, c.edge, { size: sizeOf(kind), id, uncounted: true, blocks: kind === 'police-post' })) return false;
    count[name] = (count[name] ?? 0) + 1;
    const it = L.items[L.items.length - 1]!;
    standing.push({ at: it.at, kind }); made.set(c, it);
    return true;
  };

  // ---- report points: call boxes until every sample is within reach of a door of a staffed unit, a phone booth or a box (a police post is a response origin, not a report point)
  const reportSrc: V[] = [...doorStands, ...booths.map(i => toBlock(i.at))];
    const boxes = cover({ grid, samples, sources: reportSrc, reachM: QUIET.reach.report, cands: cands('phone-booth', 'box'), place: put('phone-booth', 'call-box') });
  // ---- origins: police posts until every sample is within reach of a dock or a post (the Records Office's doors are posts)
  const officeDoors = pub.filter(e => e.building === 'civic-office');
  const originSrc: V[] = [...docks.map(i => toBlock(i.at)), ...officeDoors.map(e => toBlock(e.p))];
    const posts = cover({ grid, samples, sources: originSrc, reachM: QUIET.reach.origin, cands: cands('police-post', 'post'), place: put('police-post', 'police-post') });

  // ---- the section
  const report: Takedown['report'] = [
    ...pub.map(e => ({ id: `door:${e.id}`, kind: 'door' as const, at: rv(e.p), stand: standOf(e.p), door: e.id })),
    ...booths.map(i => ({ id: i.id, kind: 'call-box' as const, at: i.at, stand: standOf(i.at), item: i.id })),
    ...boxes.placed.map(c => ({ id: made.get(c)!.id, kind: 'call-box' as const, at: made.get(c)!.at, stand: rv(toPlan(c.stand)), item: made.get(c)!.id })),
  ];
  const origins: Takedown['origins'] = [
    ...docks.map(i => ({ id: i.id, kind: 'dock' as const, at: i.at, stand: standOf(i.at), item: i.id })),
    ...officeDoors.map(e => ({ id: `door:${e.id}`, kind: 'post' as const, at: rv(e.p), stand: standOf(e.p), door: e.id })),
    ...posts.placed.map(c => ({ id: made.get(c)!.id, kind: 'post' as const, at: made.get(c)!.at, stand: rv(toPlan(c.stand)), item: made.get(c)!.id })),
  ];
  const base = (src: V[]) => reachField(grid, src), rb = base(doorStands), ob = base(originSrc);
  const rm = measure(boxes.field, samples, QUIET.reach.report), om = measure(posts.field, samples, QUIET.reach.origin), rbm = measure(rb, samples, QUIET.reach.report), obm = measure(ob, samples, QUIET.reach.origin);
    const wsec = ctx.S.watchers.types;
  const watchers: Takedown['watchers'] = L.items.filter(i => ['mast-camera', 'pole-camera', 'civic-robot-dock'].includes(i.kind) && near(i)).map(i => {
    const t = wsec[i.kind === 'civic-robot-dock' ? 'civic-robot' : i.kind] as { range: number; cone: number; heads?: number; headStep?: number };
    const heads = t.heads ?? 1, step = ((t.headStep ?? 0) * Math.PI) / 180;
    return { id: i.id, kind: i.kind, at: i.at, facings: Array.from({ length: heads }, (_, k) => r4(i.yaw + k * step)), range: t.range, coneDeg: t.cone, item: i.id };
  });
  return {
    grid: { x0: w.x0, x1: w.x1, y0: w.y0, y1: w.y1, cell: grid.cell },
    reach: { ...QUIET.reach, lattice: QUIET.lattice },
    entrances: doorsOut, stashes, report, origins,
    coverage: {
      report: { samples: samples.length, worstM: rm.worstM, medianM: rm.medianM, within: rm.within, baselineWithin: rbm.within, baselineWorstM: rbm.worstM },
      origin: { samples: samples.length, worstM: om.worstM, medianM: om.medianM, within: om.within, baselineWithin: obm.within, baselineWorstM: obm.worstM },
    },
    watchers,
  };
}
