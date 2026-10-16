// Cover points of a block (brief 011, Pulse; briefs/011/pulse-fight-ai.md §5). From the civic block (its objects, structures and dressing as the world draws them:
// tools/world/build.ts civicMasses, with the kit's own heights), the terrace wall's solids, the cars the mission parks, and the engine's site (the walk grid, the fight points),
// it works out where a body can stand beside something that shields it from fire: a position on free ground, the direction it shields against, how wide that is, how good it is.
// Deterministic: the same inputs give the same bytes. Nothing is authored; a block with more furniture has more cover and a bare one has none, and the checker (check.ts) says
// where a fight has none. Block frame, integer cm, as sim/data/handover.site.json.
//   bun run cover        writes contracts/world/civic-block.cover.json and prints where the fights lack cover
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Cover, type CoverPoint, type FightArea } from '../../contracts/world/cover.schema';
import { civic, toPlanFn } from '../plan/civic';
import { type V, inPoly } from '../plan/geom';
import { civicMasses } from '../world/build';
import { hashes } from './inputs';
import { COVER, protects, dist as dcm } from './select';

const root = join(import.meta.dir, '..', '..');
export const COVER_FILE = 'contracts/world/civic-block.cover.json';
const PLAN = 'contracts/world/harbour-district.plan.json', SITE = 'sim/data/handover.site.json', KIT = 'contracts/world/kit.json';

/** The constants, each with its reason. [S] starting values: tune by playtest, Motive owns the modifiers they feed. */
export const RULES = {
  /** rulebook eye-cm: an object this high also blocks sight; below it a body can see over it */
  eyeCm: 160,
  /** Motive §2.7: "an occluder 0.9–1.6 m high": under 0.9 m a person is not hidden by it even crouched (the block's 0.52 m planter beds, the tables) */
  minHeightCm: 90,
  /** a body is about 0.5 m across: a face narrower than 0.6 m (a bollard, a lamp's foot) hides nobody */
  minWidthCm: 60,
  /** a body stands 0.85 m off the face: the walk grid keeps 0.65 m (plan standards.walking.body.r) off every ground solid and a grid cell is 0.5 m */
  standoffCm: 85,
  qFull: COVER.qFull, qPartial: COVER.qPartial, snapCm: COVER.snapCm, minRangeCm: COVER.minRangeCm,
  /** a hostile post counts as having cover if a shielding point is within 6 m of it: a run of 2 s at the rivals' 1.6 m/s with the first second spent aiming (§2.2 aim-new-s) */
  hostileReachCm: 600,
  /** the cell's side has cover if it is within 8 m of the car it fights over: the support operatives stand within 4 m of it (Motive's calculator) and the Enforcer fights from 20 m out */
  cellReachCm: 800,
} as const;

const CAR = { lengthM: 4.6, widthM: 1.9, heightM: 1.45 }; // a parked car; [S]

type V2 = [number, number];
type Obj = { id: string; kind: CoverPoint['kind']; poly: V2[]; h: number; present?: [number, number] };

const sub = (a: V2, b: V2): V2 => [a[0] - b[0], a[1] - b[1]];
const len = (a: V2) => Math.hypot(a[0], a[1]);
const unit = (a: V2): V2 => { const l = len(a) || 1; return [a[0] / l, a[1] / l]; };
const dotp = (a: V2, b: V2) => a[0] * b[0] + a[1] * b[1];
const nearestOn = (p: V2, a: V2, b: V2): V2 => { const ab = sub(b, a), t = Math.max(0, Math.min(1, dotp(sub(p, a), ab) / (dotp(ab, ab) || 1))); return [a[0] + ab[0] * t, a[1] + ab[1] * t]; };
const rectAt = (c: V2, along: 'x' | 'y', l: number, w: number): V2[] => { const [hx, hy] = along === 'x' ? [l / 2, w / 2] : [w / 2, l / 2]; return [[c[0] - hx, c[1] - hy], [c[0] + hx, c[1] - hy], [c[0] + hx, c[1] + hy], [c[0] - hx, c[1] + hy]]; };

/** The cover-capable objects of the civic block, block frame, metres. */
export function objectsOf(plan: any, block: any, site: any): Obj[] {
  const civ = civic(plan.civic, block), toPlan = toPlanFn(plan.civic);
  const o: V = plan.civic.blockToPlan.origin, ax: V = plan.civic.blockToPlan.x, ay: V = plan.civic.blockToPlan.y, det = ax[0] * ay[1] - ax[1] * ay[0];
  const toBlock = (p: V): V2 => { const dx = p[0] - o[0], dy = p[1] - o[1]; return [(dx * ay[1] - dy * ay[0]) / det, (ax[0] * dy - ax[1] * dx) / det]; };
  const out: Obj[] = [];
  const KIND: [RegExp, CoverPoint['kind']][] = [[/^civic\/square-pavilion-support-\d+$/, 'pier'], [/^civic\/civic-(control|service)-cabinet(-\d+)?$/, 'cabinet'], [/^civic\/foreground-civic-robot$/, 'robot'], [/^civic\/atom-fountain$/, 'fountain'], [/^civic\/run-[a-z-]+\/\d+$/, 'planter-run']];
  for (const m of civicMasses(civ, toPlan)) {
    const k = KIND.find(([re]) => re.test(m.id)); if (!k) continue;
    out.push({ id: m.id, kind: k[1], poly: m.ring.map(toBlock), h: m.height });
  }
  // the café terrace's wall (its own solids: the mass of the terrace is its box, open ground inside the wall)
  for (const p of block.dressing?.pieces ?? []) if (p.piece === 'CafeBay') for (const s of p.solids ?? []) if (/^wall-/.test(s.id)) out.push({ id: `civic/cafe/${s.id}`, kind: 'wall', poly: s.polygon as V2[], h: s.height });
  // the cars: parked along their bay (the getaway car along the square's bay, x; the others along their kerb, y), present while the mission has them there
  const at = (id: string): V2 => { const p = site.places[id]; return [p.at[0] / 100, p.at[1] / 100]; }, sch = site.schedule;
  out.push({ id: 'car/getaway', kind: 'car', poly: rectAt(at(site.getaway.car), 'x', CAR.lengthM, CAR.widthM), h: CAR.heightM, present: [0, sch.over] });
  out.push({ id: 'car/official', kind: 'car', poly: rectAt(at(site.car.pickup), 'y', CAR.lengthM, CAR.widthM), h: CAR.heightM, present: [0, sch.valeAtCar] });
  out.push({ id: 'car/rivals', kind: 'car', poly: rectAt(at(site.rivals.park), 'y', CAR.lengthM, CAR.widthM), h: CAR.heightM, present: [sch.rivalsArrive, sch.over] });
  return out.sort((a, b) => (a.id < b.id ? -1 : 1));
}

/** The site's walk grid: a cell is free when its centre is clear of every blocker and a body's width off every ground solid. */
export function gridOf(site: any) {
  const g = site.grid as { x0: number; y0: number; cell: number; nx: number; ny: number; rows: string[] };
  return { cell: g.cell, centre: (i: number, j: number): V2 => [(g.x0 + (i + 0.5) * g.cell) / 100, (g.y0 + (j + 0.5) * g.cell) / 100], nx: g.nx, ny: g.ny, x0: g.x0, y0: g.y0, free: (i: number, j: number) => i >= 0 && j >= 0 && i < g.nx && j < g.ny && g.rows[i]![j] === '.' };
}

const inRing = (p: V2, ring: V2[]) => inPoly(p as V, ring as V[]);
const sedge = (p: V2, ring: V2[]) => { let d = Infinity; for (let i = 0; i < ring.length; i++) { const a = ring[i]!, b = ring[(i + 1) % ring.length]!, q = nearestOn(p, a, b); d = Math.min(d, len(sub(p, q))); } return d; };

/** Runs of a polygon's edges that are one face: consecutive edges while the turn at each vertex is under 40° and the run has turned under 30° in all and is under 6 m long. */
export function chains(poly: V2[]): V2[][] {
  const n = poly.length, E: { a: V2; b: V2; l: number; dir: V2 }[] = [];
  for (let i = 0; i < n; i++) { const a = poly[i]!, b = poly[(i + 1) % n]!, l = len(sub(b, a)); if (l > 0.02) E.push({ a, b, l, dir: unit(sub(b, a)) }); }
  const m = E.length, turn = (p: number, q: number) => Math.abs(Math.atan2(E[p]!.dir[0] * E[q]!.dir[1] - E[p]!.dir[1] * E[q]!.dir[0], dotp(E[p]!.dir, E[q]!.dir)));
  let k = 0; for (let i = 0; i < m; i++) if (turn((i + m - 1) % m, i) >= (40 * Math.PI) / 180) { k = i; break; }
  const out: V2[][] = []; let cur: V2[] = [], cum = 0, cl = 0;
  for (let s = 0; s < m; s++) {
    const i = (k + s) % m, e = E[i]!;
    if (cur.length) {
      const t = turn((i + m - 1) % m, i);
      if (t >= (40 * Math.PI) / 180 || cum + t > (30 * Math.PI) / 180 || cl + e.l > 6) { out.push(cur); cur = []; cum = 0; cl = 0; }
      else cum += t;
    }
    if (!cur.length) cur = [e.a];
    cur.push(e.b); cl += e.l;
  }
  if (cur.length) out.push(cur);
  return out;
}

const polyLen = (c: V2[]) => c.slice(1).reduce((s, p, i) => s + len(sub(p, c[i]!)), 0);
const polyAt = (c: V2[], t: number): { p: V2; dir: V2 } => {
  let left = t;
  for (let i = 1; i < c.length; i++) { const l = len(sub(c[i]!, c[i - 1]!)); if (left <= l || i === c.length - 1) { const d = unit(sub(c[i]!, c[i - 1]!)); return { p: [c[i - 1]![0] + d[0] * Math.min(left, l), c[i - 1]![1] + d[1] * Math.min(left, l)], dir: d }; } left -= l; }
  return { p: c[0]!, dir: [1, 0] };
};
const nearestOnChain = (p: V2, c: V2[]): V2 => { let best = c[0]!, bd = Infinity; for (let i = 1; i < c.length; i++) { const q = nearestOn(p, c[i - 1]!, c[i]!), d = len(sub(p, q)); if (d < bd) { bd = d; best = q; } } return best; };

/** Quality per mille: the height class times the arc. Height 0.9 m up: 800; 1.2 m up: 900; eye height (1.6 m) up: 1000. Arc: 250 at none, 1000 from 60°. Floor. A waist-high run seen across a wide arc is full cover (≥ 600); a narrow or low one is partial. */
export const quality = (hCm: number, arcDeg: number): number => {
  const h0 = hCm >= RULES.eyeCm ? 1000 : hCm >= 120 ? 900 : 800;
  return Math.floor((h0 * (250 + Math.floor((750 * Math.min(arcDeg, 60)) / 60))) / 1000);
};

export type Candidate = Omit<CoverPoint, 'id'>;

/** Every cover point beside the objects, deduplicated, sorted: [x, y, of]. */
export function pointsOf(objs: Obj[], grid: ReturnType<typeof gridOf>): Candidate[] {
  const out: Candidate[] = [];
  const stand = RULES.standoffCm / 100;
  for (const ob of objs) {
    if (ob.h * 100 < RULES.minHeightCm) continue;
    const ccw = ob.poly.reduce((a, p, i) => a + (p[0] * ob.poly[(i + 1) % ob.poly.length]![1] - ob.poly[(i + 1) % ob.poly.length]![0] * p[1]), 0) >= 0;
    void ccw;
    for (const ch of chains(ob.poly)) {
      const L = polyLen(ch), chord = len(sub(ch[ch.length - 1]!, ch[0]!));
      if (Math.max(L, chord) * 100 < RULES.minWidthCm) continue;
      const m = Math.max(1, Math.ceil(L / 4.6)); // one point at the middle of a face up to 4.6 m (a planter run, a car's side): the middle is where the arc is widest; longer faces, one each 4.6 m
      for (let k = 0; k < m; k++) {
        const { p: S, dir } = polyAt(ch, ((k + 0.5) * L) / m);
        let n: V2 = [dir[1], -dir[0]]; if (inRing([S[0] + n[0] * 0.05, S[1] + n[1] * 0.05], ob.poly)) n = [-n[0], -n[1]];
        const ideal: V2 = [S[0] + n[0] * stand, S[1] + n[1] * stand];
        // the free grid cell centre nearest the ideal standing point, 0.65–1.35 m off the face, on its outward side
        const ci = Math.floor((ideal[0] * 100 - grid.x0) / grid.cell), cj = Math.floor((ideal[1] * 100 - grid.y0) / grid.cell);
        let best: V2 | undefined, bd = Infinity;
        for (let i = ci - 1; i <= ci + 1; i++) for (let j = cj - 1; j <= cj + 1; j++) {
          if (!grid.free(i, j)) continue;
          const c = grid.centre(i, j), q = nearestOnChain(c, ch), dq = len(sub(c, q));
          if (dq < 0.65 || dq > 1.35 || dotp(sub(c, S), n) < 0.3 || inRing(c, ob.poly)) continue;
          if (objs.some(o2 => o2 !== ob && (inRing(c, o2.poly) || sedge(c, o2.poly) < 0.65))) continue;
          const d = len(sub(c, ideal)); if (d < bd - 1e-9 || (Math.abs(d - bd) < 1e-9 && best && (c[0] < best[0] || (c[0] === best[0] && c[1] < best[1])))) { bd = d; best = c; }
        }
        if (!best) continue;
        const Q = nearestOnChain(best, ch), f = unit(sub(Q, best));
        const ang = (e: V2) => (Math.acos(Math.max(-1, Math.min(1, dotp(unit(sub(e, best!)), f)))) * 180) / Math.PI;
        const arc = Math.min(89, Math.floor(0.85 * Math.min(ang(ch[0]!), ang(ch[ch.length - 1]!))));
        if (arc < 5) continue;
        const hCm = Math.round(ob.h * 100), q = quality(hCm, arc);
        if (q < RULES.qPartial) continue;
        out.push({
          at: [Math.round(best[0] * 100), Math.round(best[1] * 100)], faces: [Math.round(f[0] * 1000), Math.round(f[1] * 1000)], arc, cos: Math.floor(1000 * Math.cos((arc * Math.PI) / 180)),
          h: hCm, against: hCm >= RULES.eyeCm ? 'fire+sight' : 'fire', q, kind: ob.kind, of: ob.id, ...(ob.present ? { present: ob.present } : {}),
        });
      }
    }
  }
  // two points within 0.9 m shielding the same way are one: keep the better
  const key = (c: Candidate) => [c.of, c.at[0], c.at[1]] as const, lt = (a: Candidate, b: Candidate) => { const x = key(a), y = key(b); return x[0] !== y[0] ? x[0] < y[0] : x[1] !== y[1] ? x[1] < y[1] : x[2] < y[2]; };
  const sorted = [...out].sort((a, b) => b.q - a.q || (lt(a, b) ? -1 : 1)), kept: Candidate[] = [];
  for (const c of sorted) if (!kept.some(k => dcm(k.at, c.at) < 90 && k.faces[0] * c.faces[0] + k.faces[1] * c.faces[1] > 906_000 && JSON.stringify(k.present) === JSON.stringify(c.present))) kept.push(c);
  return kept.sort((a, b) => (lt(a, b) ? -1 : 1));
}

export type Report = { areas: { id: string; hostile: { post: string; at: [number, number]; points: string[] }[]; cell: string[] }[]; sources: string[] };

export function buildCover(): { cover: Cover; json: string } {
  const read = (f: string) => readFileSync(join(root, f), 'utf8');
  const planText = read(PLAN), plan = JSON.parse(planText), blockText = read(plan.civic.block), block = JSON.parse(blockText), siteText = read(SITE), site = JSON.parse(siteText), kitText = read(KIT);
  const objs = objectsOf(plan, block, site), grid = gridOf(site);
  const points: CoverPoint[] = pointsOf(objs, grid).map((c, i) => ({ id: `cv-${String(i + 1).padStart(3, '0')}`, ...c }));
  const at = (id: string): [number, number] => { const p = site.places[id]; if (!p) throw new Error(`the site has no place ${id}`); const a = p.stand ?? p.at; return [a[0], a[1]]; };
  const crew = Object.keys(site.places).filter(k => k.startsWith('crew:')).sort();
  const anchor = site.getaway.car as string, a = at(anchor), carOf = 'car/getaway';
  const area = (id: string, hostile: string[], allies: string[]): FightArea => {
    const hostileCover = hostile.map(post => ({ post, points: points.filter(c => c.of !== carOf && dcm(c.at, at(post)) <= RULES.hostileReachCm && c.q >= RULES.qPartial && protects(c, a)).map(c => c.id) }));
    // the cell's side, the same question the other way: the points within cellReach of the car that shield against a hostile post, and the posts they answer
    const near = points.filter(c => dcm(c.at, a) <= RULES.cellReachCm && c.q >= RULES.qPartial);
    const answers = hostile.map(h => ({ post: h, points: near.filter(c => protects(c, at(h))).map(c => c.id) }));
    return { id, anchor, hostile, cell: crew, allies, hostileCover, cellCover: answers };
  };
  const cover: Cover = {
    schema: 'suborned.cover/1', mission: 'handover',
    frame: 'civic-block.json block frame in integer centimetres: x south, y east (as sim/data/handover.site.json). Mission clock in seconds from 0:00.',
    from: hashes({ block: blockText, site: siteText, kit: kitText, plan: planText }),
    rules: { eyeCm: RULES.eyeCm, minHeightCm: RULES.minHeightCm, minWidthCm: RULES.minWidthCm, standoffCm: RULES.standoffCm, qFull: RULES.qFull, qPartial: RULES.qPartial, snapCm: RULES.snapCm, minRangeCm: RULES.minRangeCm, hostileReachCm: RULES.hostileReachCm, cellReachCm: RULES.cellReachCm },
    points,
    blockers: objs.filter(o => o.kind !== 'car' && o.h * 100 >= RULES.eyeCm).map(o => ({ id: o.id, z: 0, h: Math.round(o.h * 100), poly: o.poly.map(p => [Math.round(p[0] * 100), Math.round(p[1] * 100)] as [number, number]) })),
    refuge: [
      ...block.buildings.flatMap((b: any) => (b.entrances ?? []).filter((e: any) => e.kind !== 'service').map((e: any) => // a service door is no refuge for the crowd (brief 013)
         ({ id: `door:${e.id}`, at: [Math.round(e.at[0] * 100), Math.round(e.at[1] * 100)], kind: 'door' as const, src: `civic-block.json buildings.${b.id}.entrances.${e.id}` }))),
      ...['security-entry', 'rival-entry'].map(id => ({ id: `mouth:${id}`, at: at(id), kind: 'mouth' as const, src: `handover.site.json places.${id}: where a street meets the block's window` })),
    ].sort((x: { id: string }, y: { id: string }) => (x.id < y.id ? -1 : 1)),
    areas: [area('getaway-safe', site.fight.ambush, [site.fight.guard]), area('getaway-loud', site.fight.police, [])],
  };
  Cover.parse(cover);
  return { cover, json: JSON.stringify(cover, null, 1) + '\n' };
}
