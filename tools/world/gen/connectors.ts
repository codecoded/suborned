// Connectors: stairs and lifts for the cores the megastructure builders sited, and the station banks (stair tower, two
// escalator runs round a mezzanine, a lift) on public ground at every sky rail platform. Then the check the brief asks for:
// every deck piece reaches the street by >= 2 connectors, >= 1 of them step-free.
import type { Connector, Mass } from '../../../contracts/world/world.schema';
import { type V, dist, lineDist } from '../../plan/geom';
import { modeOf, placements } from '../../plan/transit';
import { type Ctx, r3, rv } from './common';
import { fillTravel, reach } from './travel';
import { samples9, walkBlock } from '../../plan/street-zones';
import { type Rect, bbox, growRect, inRect, pointInRing, rectCentre, rectPts, rectsTouch } from './geo';
import { deckRec, zOf } from './mega/kit';
import type { Core, DeckRec, MegaOut } from './types';

const rectGap = (a: Rect, b: Rect) => Math.hypot(Math.max(0, a[0] - b[1], b[0] - a[1]), Math.max(0, a[2] - b[3], b[2] - a[3]));
const ringRect = (r: Rect): V[] => rectPts(r).map(rv);

/** The plan's station bank on a forecourt plot (`plots[].bank` with `serves`), if one is laid for this station side. */
type ForecourtBank = { serves: { line: string; station: string; side: string }; span: [number, number]; axisClear: [number, number]; keepClear: Rect[]; bridge: { w: number; from: number; to: number } };
/** True if the point stands on a plan signature's ground (SpaceHQ's hall). */
export const inSignature = (ctx: Ctx, p: V): boolean => (ctx.plan.signatures ?? []).some(s => inRect(p, s.ground as Rect));

export function forecourtBank(ctx: Ctx, line: string, station: string, side: string): ForecourtBank | null {
  for (const p of ctx.plan.plots as { bank?: unknown }[]) {
    const b = p.bank as ForecourtBank | string | undefined;
    if (b && typeof b === 'object' && b.serves.line === line && b.serves.station === station && b.serves.side === side) return b;
  }
  return null;
}

/** Rects the plan keeps clear of the bank's ground contact and furniture: the forecourt's `axisClear` across the forecourt's depth (Garden Walk). */
export function axisClears(ctx: Ctx): Rect[] {
  return (ctx.d.plots as unknown as { bank?: ForecourtBank | string; polyV: V[] }[]).flatMap(p => {
    const b = p.bank, bb = bbox(p.polyV);
    return b && typeof b === 'object' ? [[b.axisClear[0], b.axisClear[1], bb[2], bb[3]] as Rect] : [];
  });
}

export type DeckPiece = { id: string; level: string; rect: Rect };

/** Union of deck pieces that touch (<= 0.6 m) at one level: a walkable piece. Returns id -> group id (the smallest id). */
export function deckGroups(pieces: DeckPiece[]): Map<string, string> {
  const parent = new Map(pieces.map(p => [p.id, p.id] as const));
  const find = (x: string): string => (parent.get(x) === x ? x : (parent.set(x, find(parent.get(x)!)), parent.get(x)!));
  for (let i = 0; i < pieces.length; i++) for (let j = i + 1; j < pieces.length; j++) {
    const a = pieces[i]!, b = pieces[j]!;
    if (a.level === b.level && rectsTouch(a.rect, b.rect, 0.6)) { const ra = find(a.id), rb = find(b.id); if (ra !== rb) parent.set(ra < rb ? rb : ra, ra < rb ? ra : rb); }
  }
  return new Map(pieces.map(p => [p.id, find(p.id)] as const));
}

/** Walking reach of one deck group: the farthest point from a way down, against the level's maxTravel. */
export type Travel = { group: string; level: string; limit: number; max: number; worst: V; pieces: number };
export type Out = { connectors: Connector[]; decks: DeckRec[]; pieces: DeckPiece[]; issues: string[]; travel: Travel[] };
/** Ways down for maxTravel: stairs, ramps, escalators and stair towers; at the concourse lifts count too (a deck needs a lift only within liftEvery). */
/** Where a way down lands: the head end of a stair, ramp or escalator (3 m of it), the whole footprint of a core, tower or lift. */
export function wayRect(c: Connector): Rect {
  const b = bbox(c.ring), [hx, hy] = c.head, d = 3;
  if (Math.hypot(c.head[0] - c.foot[0], c.head[1] - c.foot[1]) < 1) return b;
  if (Math.abs(hy - b[2]) < 0.6) return [b[0], b[1], b[2], Math.min(b[3], b[2] + d)];
  if (Math.abs(hy - b[3]) < 0.6) return [b[0], b[1], Math.max(b[2], b[3] - d), b[3]];
  if (Math.abs(hx - b[0]) < 0.6) return [b[0], Math.min(b[1], b[0] + d), b[2], b[3]];
  return [Math.max(b[0], b[1] - d), b[1], b[2], b[3]];
}
export const isWayDown = (kind: Connector['kind'], level: string) => kind !== 'goodsLift' && (kind !== 'lift' || level === 'concourse');

/**
 * A core's lift car: at the core's south-west corner, or, where that corner is further than `joint` from every surface
 * the lift lands on, against the core's edge that faces the nearest of them, centred on the overlap (L-8: a lift
 * landing 13.6 m from its deck).
 */
export function carIn(core: Rect, car: [number, number], landings: Rect[], joint: number): Rect {
  const corner: Rect = [core[0], core[0] + car[0], core[2], core[2] + car[1]];
  if (!landings.length || landings.some(r => rectGap(corner, r) <= joint)) return corner;
  const to = landings.reduce((a, b) => (rectGap(core, b) < rectGap(core, a) ? b : a));
  // along one axis: flush to the core's edge that faces the landing, else centred on where they overlap
  const place = (lo: number, hi: number, size: number, t0: number, t1: number): number => {
    if (t1 <= lo) return lo;
    if (t0 >= hi) return hi - size;
    return Math.max(lo, Math.min(hi - size, (Math.max(lo, t0) + Math.min(hi, t1) - size) / 2));
  };
  const x = place(core[0], core[1], car[0], to[0], to[1]), y = place(core[2], core[3], car[1], to[2], to[3]);
  return [x, x + car[0], y, y + car[1]];
}

export function generateConnectors(ctx: Ctx, megas: MegaOut[], stations: Mass[], solids: V[][]): Out {
  const L = ctx.L, zs = L.list as { id: string; z: number }[], connectors: Connector[] = [], extra: DeckRec[] = [];
  // two goings: the most a lift car may stand off the deck it lands on (L-8)
  const joint = 2 * (L.connectors.stair.going as number);
  const decks = megas.flatMap(m => m.decks);
  const platforms: DeckPiece[] = stations.map(s => ({ id: s.id, level: 'concourse', rect: bbox(s.ring) }));
  const zOfLv = (lv: string, host: string) => {
    const m = (ctx.plan.megastructures ?? []).find(x => `mega/${x.id}` === host);
    return zOf(ctx, lv, m);
  };
  const lvOfZ = (z: number) => zs.find(l => Math.abs(l.z - z) < 0.01)?.id;
  const servesOf = (rect: Rect, zFrom: number, zTo: number, pool: DeckPiece[], tol = 9) =>
    pool.filter(d => { const z = zs.find(l => l.id === d.level)?.z ?? -1; return z > zFrom + 0.01 && z <= zTo + 0.01 && rectGap(rect, d.rect) <= tol; }).map(d => d.id);
  const pool: DeckPiece[] = [...decks.map(d => ({ id: d.id, level: d.level, rect: d.rect })), ...platforms];

  // 1. cores -> a stair and, where the core carries one, a lift
  for (const c of megas.flatMap(m => m.cores) as Core[]) {
    const z0 = c.from === 'street' ? ctx.pav : zOfLv(c.from, c.host), z1 = zOfLv(c.to, c.host), ctr = rectCentre(c.rect);
    const serves = servesOf(c.rect, z0, z1, pool);
    if (c.stair) connectors.push({ id: `${c.id}/stair`, kind: 'stair', host: c.host, serves, from: c.from, to: c.to, foot: rv(ctr), head: rv(ctr), bottom: r3(z0), top: r3(z1), width: L.minWidth.stair, stepFree: false, ring: ringRect(c.rect), yaw: 0 });
    if (c.lift) {
      const car = L.connectors.lift.car as [number, number];
      const landings = pool.filter(d => serves.includes(d.id) && d.level === c.to).map(d => d.rect);
      const lr = carIn(c.rect, car, landings, joint);
      connectors.push({ id: `${c.id}/lift`, kind: 'lift', host: c.host, serves, from: c.from, to: c.to, foot: rv(rectCentre(lr)), head: rv(rectCentre(lr)), bottom: r3(z0), top: r3(z1), width: car[0], stepFree: true, ring: ringRect(lr), yaw: 0 });
    }
  }
  // fixed connectors from the plan (stairs, cart ramp, goods lifts)
  for (const f of megas.flatMap(m => m.connectors)) {
    const b = bbox(f.ring);
    connectors.push({ ...f, serves: servesOf(b, f.bottom, f.top, pool, 3) });
  }

  // 2. station banks
  const issues: string[] = [];
  const std = ctx.S.transit.stationEntrance, env = ctx.S.skyrailEnvelope.half as number;
  const tw = std.stairTower.size as [number, number, number], runA = std.escalators.runA, runB = std.escalators.runB, mz = std.escalators.mezzanine as [number, number];
  const tower = { along: tw[1], across: tw[0] }, liftSz = ctx.L.connectors.lift.car as [number, number], pairW = 4;
  const publicOk = (p: V, occ: Rect[]) => {
    const land = ctx.d.plots.some(pl => ['park', 'open', 'square'].includes(pl.use) && pointInRing(p, pl.polyV)) || ctx.plan.foot.some(f => lineDist(p, f.pts) <= f.w / 2) || ctx.plan.quay.some(q => pointInRing(p, q));
    return land && !solids.some(s => pointInRing(p, s)) && !occ.some(o => inRect(p, o)) && !ctx.d.pylons.some(py => dist(py.p, p) < 3 + 0.8);
  };
  const occupied: Rect[] = [], beam: V[] = ctx.d.sky.flatMap(p => p.samples.filter((_, i) => i % 2 === 0).map(x => x.p));
  for (const line of ctx.plan.skyrail) for (const s of placements(ctx.plan, ctx.d, line, 'sky').items) {
    for (const side of [1, -1] as const) {
      const pid = `station/${line.id}/${s.id}/${side > 0 ? 'l' : 'r'}`, plat = platforms.find(p => p.id === pid);
      if (!plat) continue;
      const smp = s.path.samples.reduce((b, x) => (dist(x.p, s.at) < dist(b.p, s.at) ? x : b));
      if (Math.abs(smp.dir[1]) > 1e-3 && Math.abs(smp.dir[0]) > 1e-3) throw new Error(`station ${pid}: line not axis-aligned (not in the rulebook)`);
      const horiz = Math.abs(smp.dir[0]) > 0.5;
      if (!horiz) throw new Error(`station ${pid}: banks are laid for an east-west line`);
      const platOut = side > 0 ? plat.rect[3] : -plat.rect[2], px0 = plat.rect[0], px1 = plat.rect[1];
      let placed = false;
      type Lay = { t: number; d: 1 | -1; towerR: Rect; liftR: Rect; aR: Rect; mzR: Rect; bR: Rect; bounds: Rect; bridge?: { x: [number, number]; y: [number, number] } };
      const spec = forecourtBank(ctx, line.id, s.id, side > 0 ? 'l' : 'r');
      function* attempts(): Generator<Lay> {
        if (spec) { // the plan's forecourt: tower and lift east of the clear axis, run A west of it, run B and the mezzanine over it, the bridge from `bridge.from` to `bridge.to`
          const t = Math.abs(spec.bridge.from), [s0] = spec.span, ac = spec.axisClear, ys = (c0: number, c1: number) => [side * (t + c0), side * (t + c1)].sort((a, b) => a - b) as [number, number];
          const at = (x0: number, x1: number, c0: number, c1: number): Rect => [x0, x1, ys(c0, c1)[0], ys(c0, c1)[1]], tx = Math.max(s0 + runB.size[0], ac[1]);
          const towerR = at(tx, tx + tower.along, 0, tower.across), aR = at(ac[0] - (runA.size[0] - 8), ac[0], 0, pairW / 2 + 0.2);
          const bR = at(tx - runB.size[0], tx, 4.4, 4.4 + pairW / 2 + 0.2);
          const mzR = at(Math.min(aR[0] - mz[0], bR[0]), aR[0], 0, mz[1] + 2.4); // on to the foot of run B (L-8)
          const liftR = at(tx, tx + liftSz[0], 7.2, 7.2 + liftSz[1]), cx = (towerR[0] + towerR[1]) / 2;
          const bounds: Rect = [Math.min(mzR[0], bR[0]), towerR[1], Math.min(towerR[2], mzR[2], liftR[2]), Math.max(towerR[3], mzR[3], liftR[3])];
          yield { t, d: -1, towerR, liftR, aR, mzR, bR, bounds, bridge: { x: [cx - spec.bridge.w / 2, cx + spec.bridge.w / 2], y: [Math.min(spec.bridge.from, spec.bridge.to), Math.max(spec.bridge.from, spec.bridge.to)] } };
          return;
        }
        for (let t = Math.max(env + 1, (std.platformClear as number | undefined) ?? 0); t <= 40; t += 0.75) // t: the tower and lift stand from platformClear (outside the platform's outer edge), never under it (brief 007)
           for (const sx of [0, 3, -3, 6, -6, 9, -9, 12, -12, 15, -15, 18, -18]) for (const d of [1, -1] as const) {
          const xa = (px0 + px1) / 2 + sx, tx = [xa, xa + d * tower.along].sort((a, b) => a - b) as [number, number];
          if (tx[0] < px0 + 0.5 || tx[1] > px1 - 0.5) continue;
          const cl = (l0: number, l1: number, c0: number, c1: number): Rect => { const xs = [xa + d * l0, xa + d * l1].sort((a, b) => a - b), ys = [side * (t + c0), side * (t + c1)].sort((a, b) => a - b); return [xs[0]!, xs[1]!, ys[0]!, ys[1]!]; };
          const lay = { t, d, towerR: cl(0, tower.along, 0, tower.across), aR: cl(13, 13 + runA.size[0], 0, pairW / 2 + 0.2), mzR: cl(34, 34 + mz[0], 0, mz[1] + 2.4), bR: cl(tower.along, tower.along + runB.size[0], 4.4, 4.4 + pairW / 2 + 0.2), bounds: cl(0, 42, 0, 10.2) };
          yield { ...lay, liftR: cl(0, liftSz[0], 7.2, 7.2 + liftSz[1]) }; // the lift beyond the tower, away from the line
          yield { ...lay, liftR: cl(tower.along + 0.4, tower.along + 0.4 + liftSz[0], 0, liftSz[1]) }; // or beside it, in the tower's own band (where the walk zone is beyond)
        }
      }
      search: for (const { t, d, towerR, liftR, aR, mzR, bR, bounds, bridge } of attempts()) {
        let ok = true;
        for (let x = bounds[0]; x <= bounds[1] + 0.01 && ok; x += 3) for (let y = bounds[2]; y <= bounds[3] + 0.01 && ok; y += 2.55) if (!publicOk([x, y], occupied)) ok = false;
        if (!ok) continue;
        // the tower, run A and lift stand outside the beam envelope + 1 m of every line (SF-13), not only the trunk the bank is laid against
        if ([towerR, aR, liftR].some(r => beam.some(q => rectGap([q[0], q[0], q[1], q[1]], r) < env + 1 - 0.01))) continue;
        // and out of every walk zone (SF-13) — except inside a signature structure's ground (SpaceHQ's ground hall: brief 007, the hall's own lanes are the walk zones; Datum to confirm)
        if ([towerR, aR, liftR].some(r => !inSignature(ctx, rectCentre(r)) && walkBlock(ctx, samples9(ringRect(r)), ''))) continue;
        // lay the bank
        const base = `bank/${line.id}/${s.id}/${side > 0 ? 'l' : 'r'}`;
        const bridgeNeeded = t > platOut + 0.2 || !!bridge; // the bank stands clear of the platform: a concourse bridge joins them
        const bridgeId = `${base}/bridge`, mezzId = `${base}/mezzanine`, zc = zOfLv('concourse', base), zd = zOfLv('deck', base);
        let platServe = pid;
        if (bridgeNeeded) {
          const g = platOut, bx = bridge?.x ?? [towerR[0], towerR[1]], ys = bridge?.y ?? ([side * g, side * t].sort((a, b) => a - b) as [number, number]);
          const br: Rect = [bx[0]!, bx[1]!, ys[0], ys[1]];
          extra.push(deckRec(ctx, base, bridgeId, 'concourse', br, [], L.structureDepth.bridge)); pool.push({ id: bridgeId, level: 'concourse', rect: br }); platServe = bridgeId;
        }
        extra.push(deckRec(ctx, base, mezzId, 'deck', mzR, [], L.structureDepth.deck)); pool.push({ id: mezzId, level: 'deck', rect: mzR });
        const mk = (k: Connector['kind'], n: string, r: Rect, from: string, to: string, serves: string[], free: boolean, w: number, top: number, foot?: V, head?: V): Connector => ({ id: `${base}/${n}`, kind: k, host: base, serves, from, to, foot: rv(foot ?? rectCentre(r)), head: rv(head ?? rectCentre(r)), bottom: from === 'street' ? ctx.pav : zd, top: r3(top), width: w, stepFree: free, ring: ringRect(r), yaw: horiz ? 0 : Math.PI / 2 });
        const along = (r: Rect, near: boolean): [V, V] => { const y = (r[2] + r[3]) / 2, a: V = [r[0], y], b: V = [r[1], y]; return (d > 0) === near ? [a, b] : [b, a]; };
        const [af, ah] = along(aR, true), [bf, bh] = along(bR, false);
        connectors.push(
          mk('stairTower', 'tower', towerR, 'street', 'concourse', [mezzId, platServe], false, ctx.L.minWidth.stair, std.stairTower.size[2]),
          mk('escalator', 'run-a', aR, 'street', 'deck', [mezzId], false, pairW, zd, af, ah), mk('escalator', 'run-b', bR, 'deck', 'concourse', [mezzId, platServe], false, pairW, zc, bf, bh),
          mk('lift', 'lift', liftR, 'street', 'concourse', [mezzId, platServe], true, liftSz[0], zc));
        for (const r of [bounds]) occupied.push(r);
        placed = true; break search;
      }
      if (!placed) issues.push(`${pid}: no public ground for a station bank`);
    }
  }

  // 3. every deck piece reaches the street: >= 2 connectors, >= 1 step-free
  const all: DeckPiece[] = [...decks.map(d => ({ id: d.id, level: d.level, rect: d.rect })), ...extra.map(d => ({ id: d.id, level: d.level, rect: d.rect })), ...platforms];
  const groups = deckGroups(all);
  const members = new Map<string, string[]>();
  for (const [id, g] of groups) members.set(g, [...(members.get(g) ?? []), id]);
  for (const [g, ids] of members) {
    const cs = connectors.filter(c => c.serves.some(s => ids.includes(s)));
    if (cs.length < 2) issues.push(`deck group ${g} (${ids.length} pieces): ${cs.length} connectors, needs >= 2`);
    else if (!cs.some(c => c.stepFree)) issues.push(`deck group ${g}: no step-free connector`);
  }
  for (const d of [...decks, ...extra]) d.group = groups.get(d.id)!;
  // 4. maxTravel: every point of a deck group is within the level's limit, walked, of a way down that lands on it. A group the
  // builders' own cores leave short (a bridge's dead end) gets stairs on public ground beside it.
  const travel: Travel[] = [], blocks = megas.flatMap(m => m.blocks ?? []), CORE = L.cores.stairLift as [number, number];
  for (const [g, ids] of members) {
    const pcs = all.filter(p => ids.includes(p.id)), level = pcs[0]!.level, limit = L.spacing[level]?.maxTravel as number | undefined;
    if (!limit) continue;
    const waysOf = () => connectors.filter(c => isWayDown(c.kind, level)).map(c => ({ rect: wayRect(c), serves: c.serves }));
    let r = reach(pcs, waysOf(), blocks, limit);
    if (r.max > limit) {
      const near = (rc: Rect) => pcs.filter(p => rectGap(rc, p.rect) <= 3).map(p => p.id);
      const fill = fillTravel({
        pieces: pcs, ways: waysOf(), obstacles: blocks, limit, serves: near,
        candidates: (c, taken, tier) => {
          const cs: { rc: Rect; d: number }[] = [];
          if (tier) return [];
          for (const [w, h] of [[CORE[0], CORE[1]], [CORE[1], CORE[0]]] as const) for (let px = Math.floor(c[0]) - 30; px <= c[0] + 30; px++) for (let py = Math.floor(c[1]) - 30; py <= c[1] + 30; py++) {
            const rc: Rect = [px, px + w, py, py + h];
            if (!near(rc).length || taken.some(t => rectGap(rc, t) < 1) || ![[rc[0], rc[2]], [rc[1], rc[3]], rectCentre(rc)].every(q => ctx.d.roads.every(rd => rd.gyratory || lineDist(q as V, rd.line) >= rd.w / 2)) || ![[rc[0], rc[2]], [rc[1], rc[2]], [rc[0], rc[3]], [rc[1], rc[3]], rectCentre(rc)].every(q => publicOk(q as V, [...occupied, ...taken]))) continue;
            cs.push({ rc, d: Math.hypot((rc[0] + rc[1]) / 2 - c[0], (rc[2] + rc[3]) / 2 - c[1]) });
          }
          return cs.sort((a, b) => a.d - b.d).map(q => q.rc);
        },
      });
      fill.added.forEach((rc, k) => {
        const z1 = zs.find(l => l.id === level)!.z, ctr = rectCentre(rc);
        connectors.push({ id: `${g}/way-${k}`, kind: 'stair', host: g, serves: near(rc), from: 'street', to: level, foot: rv(ctr), head: rv(ctr), bottom: ctx.pav, top: r3(z1), width: L.minWidth.stair, stepFree: false, ring: ringRect(rc), yaw: 0 });
        occupied.push(rc);
      });
      r = reach(pcs, waysOf(), blocks, limit);
    }
    travel.push({ group: g, level, limit, max: Math.round(r.max * 10) / 10, worst: rv(r.worst), pieces: pcs.length });
    if (r.max > limit) issues.push(`${level} group ${g}: ${Number.isFinite(r.max) ? r.max.toFixed(1) : 'unreachable'} m walked from a way down at (${r.worst[0].toFixed(1)}, ${r.worst[1].toFixed(1)}), maxTravel ${limit}`);
  }
  void lvOfZ; void growRect;
  return { connectors, decks: extra, pieces: all, issues, travel };
}
