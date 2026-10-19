// Brief 011 · Vantage · the Records Office public door, re-opened (2026-10-18). Sky: "but the west door is the service entrance."
// Re-checks the four options of study.ts against brief 013's Office service door (civic-block.adjust.json addEntrance office-service-door,
// block (63, -41.1), west wing face onto the Service lane) and the quiet-place rule (briefs/013/vantage-quiet-places.md §2); measures every
// Handover point at the four headings the view can now take (brief 016: Q/E, yaw 30 / 120 / 210 / 300); scores new options.
// Run (repo root): bun briefs/011/vantage/door-recheck.ts → briefs/011/vantage/door-recheck.json and the recheck-*, rot-*, ground-*, e-*, f-*, g-*,
// h-* pictures. Needs rsvg-convert. Nothing here changes the plan, the block, the adjust file or the mission: options are transforms in memory.
// Measures with rule V-1's code (tools/missions/sight.ts, tools/missions/handover/visibility.ts), extended to any heading; the service door's
// watchers are the engine's cones (rulebook door-reach-m 26 / door-half-m 19, the entrance camera 22 / 9, patrol-depth-m 24, half 2 + 0.65 d,
// witness-range-m 13) with a 2D line past the civic buildings' footprints: a static survey, as in brief 013, not a sim run.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import { derive } from '../../../tools/plan/plan';
import { civic, toPlanFn, type Civic } from '../../../tools/plan/civic';
import { generated } from '../../../tools/plan/world-ctx';
import { walkBetween } from '../../../tools/plan/walk-fine';
import { type V, inPoly } from '../../../tools/plan/geom';
import { type Blocker, type View, PLAY, blockersOf as sightBlockers, blocks, groundSeen, project, seen } from '../../../tools/missions/sight';
import { entranceShapes, V1_LIMIT } from '../../../tools/missions/handover/visibility';
import { render, png, type Solid, type Flat, type RGB } from './oblique';

const ROOT = resolve(import.meta.dir, '../../..'), OUT = resolve(import.meta.dir);
const J = (p: string) => JSON.parse(readFileSync(resolve(ROOT, p), 'utf8'));
const plan = J('contracts/world/harbour-district.plan.json'), block = J(plan.civic.block), mission = J('contracts/missions/handover/mission.json');
const rules = J('contracts/rules/rulebook.json');
const d = derive(plan), civ = civic(plan.civic, block), { w } = generated(plan, d, civ), toPlan = toPlanFn(plan.civic);
const RV = (k: string) => rules.values[k].v as number;
const DOOR_REACH = RV('door-reach-m'), DOOR_HALF = RV('door-half-m'), WITNESS = RV('witness-range-m'), P_DEPTH = RV('patrol-depth-m'), P_BASE = RV('patrol-half-base-cm') / 100, P_PER = RV('patrol-half-per-mille') / 1000;
const YAWS = [30, 120, 210, 300] as const;

// Blockers for every heading: 130 m round the civic buildings (rays climb 0.78 m a metre; nothing near is taller than ~100 m).
const fx = civ.buildings.flatMap(b => b.poly.map(p => p[0])), fy = civ.buildings.flatMap(b => b.poly.map(p => p[1]));
const B0 = sightBlockers(w, [Math.min(...fx) - 130, Math.max(...fx) + 130, Math.min(...fy) - 130, Math.max(...fy) + 130], entranceShapes(w, civ, toPlan));

// ---- frames: block (x south, y east) <-> plan (x east, y north) ----
const P2B = (p: V): V => [-230 - p[1], p[0] + 130];
const cen = (r: V[]): V => [r.reduce((s, p) => s + p[0], 0) / r.length, r.reduce((s, p) => s + p[1], 0) / r.length];
type XF = (p: V) => V;
const inB = (p: V, x0: number, x1: number, y0: number, y1: number) => { const b = P2B(p); return b[0] >= x0 && b[0] <= x1 && b[1] >= y0 && b[1] <= y1; };
const viaBlock = (f: (b: V) => V): XF => p => toPlan(f(P2B(p)));
const ringXf = (r: V[], f: XF) => r.map(f);
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
type Mission = typeof mission;
const mapPts = (m: Mission, f: (name: string, at: V) => V | undefined): Mission => {
  const n = clone(m);
  for (const x of n.destinations) x.at = f(x.name, x.at) ?? x.at;
  for (const x of n.crew) x.at = f(`crew ${x.name}`, x.at) ?? x.at;
  for (const x of n.points) if (x.at) x.at = f(x.id, x.at) ?? x.at;
  for (const x of n.cameras) x.at = f(`camera ${x.id}`, x.at) ?? x.at;
  return n;
};
const bayRings = civ.bays.map(b => b.poly);
const rot = (deg: number, piv0: V, piv1: V) => { const a = (deg * Math.PI) / 180, c = Math.cos(a), s = Math.sin(a); return (b: V): V => [piv1[0] + c * (b[0] - piv0[0]) - s * (b[1] - piv0[1]), piv1[1] + s * (b[0] - piv0[0]) + c * (b[1] - piv0[1])]; };

// The service side as brief 013 built it (block frame): the door and the Service lane's keep-clear (civic-block.adjust.json `service-lane`).
const adjust = J('contracts/world/civic-block.adjust.json');
const SVC_DOOR = block.buildings.find((b: any) => b.id === 'civic-office').entrances.find((e: any) => e.id === 'office-service-door').at as V;
const LANE: V[] = (JSON.stringify(adjust).match(/"id":"service-lane","polygon":(\[\[.*?\]\])/)?.[1] ? JSON.parse(JSON.stringify(adjust).match(/"id":"service-lane","polygon":(\[\[.*?\]\])/)![1]!) : [[48, -46], [77, -46], [77, -40.5], [48, -40.5]]) as V[];

// ---- the options ----
type Option = {
  id: string; title: string; short: string; civicXf: (id: string, ring: V[]) => V[] | null; mission: Mission; bays: V[][];
  roads?: (id: string, line: V[]) => V[]; plots?: (id: string, ring: V[]) => V[];
  office: (b: V) => V; guardFacing: V; patrolFacing: V; tops?: Record<string, number>; isNew?: boolean;
};
const ID: (b: V) => V = b => b;
const NOW: Option = { id: 'now', title: 'now (as built, 2026-10-18)', short: 'now', civicXf: (_i, r) => r, mission, bays: bayRings, office: ID, guardFacing: [-1, 0], patrolFacing: [1, 0] };
// (a) mirror the Office's contents N-S in its plot (study.ts)
const mirA = viaBlock(([x, y]) => [124 - x, y]);
const optA: Option = {
  id: 'a', title: '(a) mirror the Office in its plot', short: '(a) mirror', civicXf: (_id, r) => (P2B(cen(r))[0] > 31 ? ringXf(r, mirA) : r),
  mission: mapPts(mission, (_n, at) => (at[0] > 31 ? [124 - at[0], at[1]] : undefined)), bays: [bayRings[1]!, ringXf(bayRings[0]!, mirA)],
  office: ([x, y]) => [124 - x, y], guardFacing: [1, 0], patrolFacing: [-1, 0],
};
// (b) the public door and entrance onto the west face (study.ts)
const PIV0: V = [52.5, -30];
const rotB = rot(90, PIV0, [64, -40.5]); // study.ts: (x, y) -> (64 - (y + 30), -40.5 + (x - 52.5))
const inForecourt = (r: V[]) => inB(cen(r), 44, 52.55, -40.2, -19.8);
const B_SITES: Record<string, V> = { 'patrol-a': [68, -44.8], 'patrol-b': [52, -44.8], 'camera entrance': [53, -44.5], 'Service lane': [70.5, -43.2] };
const optB: Option = {
  id: 'b', title: '(b) front door and entrance onto the west face', short: '(b) west face',
  civicXf: (id, r) => (/^civic\/(civic-office|cafe)/.test(id) ? r : inForecourt(r) ? ringXf(r, viaBlock(rotB)) : r),
  mission: mapPts(mission, (n, at) => B_SITES[n] ?? (n === 'Office entrance' || n === 'Office approach' ? rotB(at) : undefined)), bays: bayRings,
  office: ID, guardFacing: [0, -1], patrolFacing: [0, 1],
};
// (c) the whole civic site mirrored N-S in the plan (study.ts)
const mirC = viaBlock(([x, y]) => [54 - x, y]);
const optC: Option = {
  id: 'c', title: '(c) turn the civic site round in the plan', short: '(c) site turned',
  civicXf: (_id, r) => ringXf(r, mirC), mission: mapPts(mission, (_n, at) => [54 - at[0], at[1]]), bays: bayRings.map(b => ringXf(b, mirC)),
  roads: (id, line) => (id === 'road/orbit-walk' ? line.map(([x, y]) => [x, -514 - y] as V) : line),
  plots: (id, ring) => (id === 'plot/records-square' || id === 'plot/records-office' ? ring.map(mirC) : ring),
  office: ([x, y]) => [54 - x, y], guardFacing: [1, 0], patrolFacing: [-1, 0],
};
// (d) the paintings' layout (study.ts)
const OFF_D = (p: V): V => [p[0] - 6.5, -294 * 2 - p[1] + 74.5];
const CAFE_C: V = [-159, -213.65], CAFE_N: V = [-146.65, -247];
const CAFE_D = (p: V): V => [CAFE_N[0] + (p[1] - CAFE_C[1]), CAFE_N[1] - (p[0] - CAFE_C[0])];
const FOUNT_D = (p: V): V => [p[0] - 7, p[1] - 9.5];
const isOffice = (id: string) => /^civic\/(civic-office|office-public-entrance|office-public-landing|office-public-steps|cyp-office|top-office)/.test(id);
const isCafe = (r: V[]) => { const c = P2B(cen(r)); return c[0] < -3.2 && c[1] > -45 && c[1] < -12; };
const isFountain = (r: V[]) => { const c = cen(r); return Math.hypot(c[0] + 160, c[1] + 236.5) < 10.5; };
const D_PTS: Record<string, V> = {
  'Office entrance': [-166.5, -231.75], 'Office approach': [-166.5, -234.9], 'Official entrance': [-166.5, -231.75], 'Meet Vale': [-166.5, -231.75],
  'Official drop-off': [-169, -259.4], 'Meet driver': [-172, -259.4], 'Boarding pavilion': [-169, -258.4], 'Service lane': [-178.6, -222], 'South walkway': [-146.6, -261],
  'patrol-a': [-170.5, -242.75], 'patrol-b': [-152.5, -242.75], 'camera entrance': [-177, -238], 'camera pickup': [-178.5, -253], cabinet: [-176.5, -257.5],
  'rival-park': [-176, -265.5], 'patrol-reroute': [-158, -259], 'block-spot': [-175.5, -255.5],
};
const optD: Option = {
  id: 'd', title: '(d) the paintings\' layout: Office and café at the back of the square', short: '(d) paintings',
  civicXf: (id, r) => (isOffice(id) ? ringXf(r, OFF_D) : /^civic\/(official|office-official|top-official|cyp-facade|civic-office)/.test(id) ? ringXf(r, OFF_D) : isCafe(r) ? ringXf(r, CAFE_D) : isFountain(r) ? ringXf(r, FOUNT_D) : inB(cen(r), 40, 90, -60, 10) ? null : r),
  mission: mapPts(mission, (n, at) => { if (D_PTS[n]) return P2B(D_PTS[n]!); const p = toPlan(at); if (at[0] < -3.2 && at[0] > -30 && at[1] > -45 && at[1] < -12) return P2B(CAFE_D(p)); if (Math.hypot(p[0] + 160, p[1] + 236.5) < 10.5) return P2B(FOUNT_D(p)); return undefined; }),
  bays: [bayRings[1]!, [[-177, -261], [-161, -261], [-161, -258], [-177, -258]]],
  office: b => P2B(OFF_D(toPlan(b))), guardFacing: [1, 0], patrolFacing: [-1, 0],
};

// ---- new options ----
// The public entrance's own pieces (the RecordsEntrance kit, its landing and steps, the two topiaries in its pockets): what moves with the door.
const isDoorKit = (id: string) => /^civic\/(office-public-entrance|office-public-landing|office-public-steps|top-office-[we])/.test(id);
const doorKitSolid = (r: V[]) => inB(cen(r), 48.9, 52.55, -34, -26);
// (e) another face: the public door on the south face, east of the official tower (block (72.5, -23.25), front south onto Apron Road), turned 180°
const rotE = rot(180, PIV0, [72.5, -23.25]);
const E_GONE = /^civic\/(solid\/)?(civic-office-north|top-official-e)$/; // the bed and the pot in front of that bit of face give way to the door
const optE: Option = {
  id: 'e', title: '(e) public door on the south face, beside the official door', short: '(e) south face', isNew: true,
  civicXf: (id, r) => (E_GONE.test(id) ? null : isDoorKit(id) || (id.startsWith('civic/solid/') && doorKitSolid(r)) ? ringXf(r, viaBlock(rotE)) : r),
  mission: mapPts(mission, (n, at) => (n === 'Office entrance' || n === 'Office approach' ? rotE(at) : n === 'camera entrance' ? [80.6, -15.5] : undefined)), bays: bayRings,
  office: ID, guardFacing: [1, 0], patrolFacing: [1, 0],
};
// (e2) the east face (block (65.5, -19.5), front east): points only, to show the face is behind the building at yaw 30
const rotE2 = rot(-90, PIV0, [65.5, -19.5]);
const optE2: Option = {
  id: 'e2', title: '(e2) public door on the east face', short: '(e2) east face', isNew: true,
  civicXf: (id, r) => (/^civic\/(solid\/)?civic-office-west$/.test(id) ? null : isDoorKit(id) || (id.startsWith('civic/solid/') && doorKitSolid(r)) ? ringXf(r, viaBlock(rotE2)) : r), // the east bed gives way
  mission: mapPts(mission, (n, at) => (n === 'Office entrance' || n === 'Office approach' ? rotE2(at) : n === 'camera entrance' ? [56, -12] : undefined)), bays: bayRings,
  office: ID, guardFacing: [0, 1], patrolFacing: [1, 0],
};
// (f) lower the public tower to the wings (15.5 m): the door stays; what the 38 m crown hides behind it comes back. Against C-0's Oct 9 crown.
const TOWER_PUBLIC = 'civic/civic-office/tower-public';
const optF: Option = {
  id: 'f', title: '(f) tower-public lowered to the wings (15.5 m), door as built', short: '(f) tower lowered', isNew: true,
  civicXf: (_i, r) => r, mission, bays: bayRings, office: ID, guardFacing: [-1, 0], patrolFacing: [1, 0], tops: { [TOWER_PUBLIC]: 15.5 },
};
const optF2: Option = { ...optF, id: 'f2', title: '(f2) both towers lowered to the wings (15.5 m), door as built', short: '(f2) both towers lowered', tops: { [TOWER_PUBLIC]: 15.5, 'civic/civic-office/tower-official': 15.5 } };
// (g) forecourt shift to the north-west corner: the entrance turned 40° onto the NW chamfer (55.5,-36.5)-(58.9,-40.5), front north-west, the
// forecourt at the Service lane's north mouth; the camera stays at its pole (49.5, -41.5), turned to the new door.
const CH0: V = [55.5, -36.5], CH1: V = [58.9, -40.5], CHM: V = [(CH0[0] + CH1[0]) / 2, (CH0[1] + CH1[1]) / 2];
const CHN: V = (() => { const dx = CH1[0] - CH0[0], dy = CH1[1] - CH0[1], L = Math.hypot(dx, dy); return [dy / L, -dx / L] as V; })(); // outward (north-west)
const rotG = rot((Math.atan2(CHN[1], CHN[0]) - Math.PI) * 180 / Math.PI, PIV0, CHM);
const optG: Option = {
  id: 'g', title: '(g) forecourt shift: the public door on the north-west corner', short: '(g) NW corner', isNew: true,
  civicXf: (id, r) => (/^civic\/(solid\/)?(civic-facade-southeast|cyp-facade-w)$/.test(id) ? null : isDoorKit(id) || (id.startsWith('civic/solid/') && doorKitSolid(r)) ? ringXf(r, viaBlock(rotG)) : r), // the corner bed and its cypress give way
  mission: mapPts(mission, (n, at) => (n === 'Office entrance' || n === 'Office approach' ? rotG(at) : undefined)), bays: bayRings,
  office: ID, guardFacing: CHN, patrolFacing: [1, 0],
};
// (h) a heading for the Office door: the layout as built; the Office front's beat is read at yaw 210 (the door's own front), the rest at 30.
const OFFICE_FRONT = new Set(['Office entrance', 'Office approach', 'camera entrance cone', 'South walkway', 'cabinet', 'patrol-a', 'patrol-b', 'patrol sweep']);
const OPTIONS = [NOW, optA, optB, optC, optD, optE, optE2, optF, optF2, optG];

// ---- measure ----
const WING_BASE = B0.find(b => b.id === 'civic/civic-office')!.z1 - 15.5; // the wings' base in the export (their top less the 15.5 m wing)
const blockersOfOpt = (o: Option): Blocker[] => B0.flatMap(b => {
  if (!b.id.startsWith('civic/')) return [b];
  const r = o.civicXf(b.id.split('#')[0]!, b.ring); if (!r) return [];
  const top = o.tops?.[b.id]; return [{ ...b, ring: r, box: undefined, ...(top !== undefined ? { z1: WING_BASE + top } : {}) }];
});
const civOf = (o: Option): Civic => {
  const xf = (id: string, r: V[]) => o.civicXf(`civic/${id}`, r) ?? r;
  return { ...civ, buildings: civ.buildings.map(b => ({ ...b, poly: xf(b.id, b.poly), towers: b.towers?.map(t => ({ ...t, poly: t.poly && xf(`${b.id}/tower-${t.id}`, t.poly) })) })), bays: o.bays.map((poly, i) => ({ id: civ.bays[i]?.id ?? `bay${i}`, poly })), solids: (civ.solids ?? []).flatMap((s, i) => { const r = o.civicXf(`civic/solid/${civ.solidIds?.[i] ?? i}`, s); return r === null ? (o.id === 'd' ? [] : []) : [r]; }).filter(s => !(o.id === 'd' && inB(cen(s), 40, 90, -60, 10))) };
};
const PAVE = 0.18, LANDING = 0.75;
type Row = { what: string; kind: 'person' | 'cone' | 'sweep'; p: V; z: number; seen: number; onlyFade: number; open?: number; by: string[] };
/** V-1's measure (tools/missions/handover/visibility.ts) at any heading; a cone also gets its share over open ground (outside building footprints). */
function rowsAt(m: Mission, c2: Civic, B: Blocker[], v: View): Row[] {
  const rows: Row[] = [], foot = c2.buildings.flatMap(b => [b.poly, ...(b.towers ?? []).flatMap(t => (t.poly ? [t.poly] : []))]);
  const zAt = (p: V, surface?: string) => (surface === 'office-public' ? PAVE + LANDING : c2.bays.some(b => inPoly(p, b.poly)) ? 0 : PAVE);
  const person = (what: string, at: V, surface?: string) => { const p = toPlan(at), z = zAt(p, surface), s = seen(p, z, B, v); rows.push({ what, kind: 'person', p, z, seen: s.pastArchitecture, onlyFade: s.onlyFade, by: s.by }); };
  for (const x of m.destinations) person(x.name, x.at, x.surface);
  for (const c of m.crew) person(`crew ${c.name}`, c.at);
  for (const pt of m.points) if (pt.at) person(pt.id, pt.at);
  for (const c of m.cameras) {
    const a = toPlan(c.at), t = m.destinations.find((x: any) => x.name === c.looksAt); if (!t) continue;
    const b = toPlan(t.at), L = Math.hypot(b[0] - a[0], b[1] - a[1]), u: V = [(b[0] - a[0]) / L, (b[1] - a[1]) / L], n: V = [-u[1], u[0]], pts: V[] = [];
    for (let i = 1; i <= 12; i++) for (let j = -4; j <= 4; j++) { const s = (c.reachM * i) / 12, ww = (c.halfM * s / c.reachM) * (j / 4); pts.push([a[0] + u[0] * s + n[0] * ww, a[1] + u[1] * s + n[1] * ww]); }
    const g = groundSeen(pts, PAVE + 0.05, B, v), open = pts.filter(p => !foot.some(f => inPoly(p, f)));
    rows.push({ what: `camera ${c.id} cone`, kind: 'cone', p: a, z: PAVE, seen: g, onlyFade: 0, open: groundSeen(open, PAVE + 0.05, B, v), by: [] });
  }
  const pa = m.points.find((p: any) => p.id === 'patrol-a')?.at, pb = m.points.find((p: any) => p.id === 'patrol-b')?.at;
  if (pa && pb) {
    const a = toPlan(pa), b = toPlan(pb), n = Math.max(2, Math.round(Math.hypot(b[0] - a[0], b[1] - a[1]))), S = Array.from({ length: n + 1 }, (_, i) => seen([a[0] + ((b[0] - a[0]) * i) / n, a[1] + ((b[1] - a[1]) * i) / n], PAVE, B, v));
    rows.push({ what: 'patrol sweep', kind: 'sweep', p: a, z: PAVE, seen: S.reduce((s, x) => s + x.pastArchitecture, 0) / S.length, onlyFade: S.reduce((s, x) => s + x.onlyFade, 0) / S.length, by: [...new Set(S.flatMap(x => x.by))].sort() });
  }
  return rows;
}
const ok = (r: Row) => r.seen >= V1_LIMIT - 1e-9;
const pc = (x: number) => `${Math.round(x * 100)} %`;
const arch = (r: Row) => r.by.filter(b => !/tree|parasol|cyp|palm/i.test(b)).map(b => b.replace(/^civic\//, '').replace(/#\d+$/, '')).filter((x, i, a) => a.indexOf(x) === i).slice(0, 2).join(' + ');

// The service door and the lane against the watchers, in the block frame (2D line past the civic footprints, as Site.Sight at eye height).
const inCone = (o: V, f: V, reach: number, half: number, p: V) => { const L = Math.hypot(f[0], f[1]), u: V = [f[0] / L, f[1] / L], rx = p[0] - o[0], ry = p[1] - o[1], dot = rx * u[0] + ry * u[1]; if (dot < 0 || dot > reach) return false; return Math.abs(rx * u[1] - ry * u[0]) <= (dot * half) / reach; };
const inSweep = (o: V, f: V, p: V) => { const L = Math.hypot(f[0], f[1]), u: V = [f[0] / L, f[1] / L], rx = p[0] - o[0], ry = p[1] - o[1], dep = rx * u[0] + ry * u[1]; if (dep < 0 || dep > P_DEPTH) return false; return Math.abs(rx * u[1] - ry * u[0]) <= P_BASE + dep * P_PER; };
function svcCheck(o: Option) {
  const c2 = civOf(o), m = o.mission, B = (p: V) => P2B(p);
  const occ = c2.buildings.flatMap(b => [b.poly, ...(b.towers ?? []).flatMap(t => (t.poly ? [t.poly] : []))]).map(r => r.map(B));
  const clearLine = (a: V, b: V) => { const L = Math.hypot(b[0] - a[0], b[1] - a[1]), n = Math.ceil(L / 0.2); for (let i = 1; i < n; i++) { const p: V = [a[0] + ((b[0] - a[0]) * i) / n, a[1] + ((b[1] - a[1]) * i) / n]; if (occ.some(r => inPoly(p, r))) return false; } return true; };
  const at = (name: string): V => m.destinations.find((x: any) => x.name === name)!.at as V, pt = (id: string): V => m.points.find((x: any) => x.id === id)!.at as V;
  const door = at('Office entrance'), approach = at('Office approach'), official = at('Official entrance'), svc = o.office(SVC_DOOR), lane = LANE.map(o.office);
  const cam = m.cameras.find((c: any) => c.id === 'entrance')!, camF: V = [door[0] - cam.at[0], door[1] - cam.at[1]];
  const pa = pt('patrol-a'), pb = pt('patrol-b'), sweepAt = Array.from({ length: 41 }, (_, i) => [pa[0] + ((pb[0] - pa[0]) * i) / 40, pa[1] + ((pb[1] - pa[1]) * i) / 40] as V);
  const crowdPts = [door, approach], offPts = [official];
  const watchers = (p: V) => ({
    guard: inCone(door, o.guardFacing, DOOR_REACH, DOOR_HALF, p) && clearLine(door, p),
    camera: inCone(cam.at, camF, cam.reachM, cam.halfM, p) && clearLine(cam.at, p),
    sweep: sweepAt.filter(s => inSweep(s, o.patrolFacing, p) && clearLine(s, p)).length / sweepAt.length,
    publicCrowd: crowdPts.some(q => Math.hypot(q[0] - p[0], q[1] - p[1]) <= WITNESS && clearLine(q, p)),
    officialCrowd: offPts.some(q => Math.hypot(q[0] - p[0], q[1] - p[1]) <= WITNESS && clearLine(q, p)),
  });
  // the lane's open ground, 1 m cells
  const xs = lane.map(p => p[0]), ys = lane.map(p => p[1]), cells: V[] = [];
  for (let x = Math.min(...xs) + 0.5; x < Math.max(...xs); x += 1) for (let y = Math.min(...ys) + 0.5; y < Math.max(...ys); y += 1) if (inPoly([x, y], lane) && !occ.some(r => inPoly([x, y], r))) cells.push([x, y]);
  const W = cells.map(watchers), share = (f: (x: ReturnType<typeof watchers>) => boolean) => Math.round((W.filter(f).length / Math.max(1, W.length)) * 100) / 100;
  const sv = watchers(svc), dist = Math.hypot(door[0] - svc[0], door[1] - svc[1]);
  const segD = (p: V, a: V, b: V) => { const dx = b[0] - a[0], dy = b[1] - a[1], t = Math.max(0, Math.min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / (dx * dx + dy * dy))); return Math.hypot(p[0] - a[0] - t * dx, p[1] - a[1] - t * dy); };
  const toLane = (p: V) => (inPoly(p, lane) ? 0 : Math.min(...lane.map((a, i) => segD(p, a, lane[(i + 1) % lane.length]!))));
  const onLane = [door, approach].some(p => toLane(p) < 1.5);
  const drv = at('Meet driver'), car = at('Official drop-off'), guardSeesPickup = [drv, car].some(p => inCone(door, o.guardFacing, DOOR_REACH, DOOR_HALF, p) && clearLine(door, p));
  const doorsApart = Math.round(Math.hypot(door[0] - official[0], door[1] - official[1]) * 10) / 10;
  // where the lane lands: the share of its ground on a civic plot (not a road or another plot)
  const civPlots = w.ground.plots.filter(q => /records-(square|office)$/.test(q.id)).map(q => (o.plots ? o.plots(q.id, q.ring) : q.ring).map(P2B));
  const laneOnCivic = Math.round((cells.filter(c => civPlots.some(r => inPoly(c, r))).length / Math.max(1, cells.length)) * 100) / 100;
  const holds = dist >= WITNESS && !onLane && !sv.guard && !sv.camera && !sv.publicCrowd;
  return { door, approach, svc, lane, official, cam: cam.at as V, camF, pa, pb, dist: Math.round(dist * 10) / 10, onLane, guardSeesPickup, doorsApart, laneOnCivic, svcWatchers: { ...sv, sweep: Math.round(sv.sweep * 100) / 100 },
    lane: { cells: cells.length, guard: share(x => x.guard), camera: share(x => x.camera), sweep: share(x => x.sweep > 0), publicCrowd: share(x => x.publicCrowd), anyPublicSide: share(x => x.guard || x.camera || x.publicCrowd) }, holds, laneRing: lane };
}

const pt = (m: Mission, n: string) => toPlan(m.destinations.find((x: any) => x.name === n)!.at as V);
const results = OPTIONS.map(o => {
  const B = blockersOfOpt(o), c2 = civOf(o), byYaw = Object.fromEntries(YAWS.map(y => [y, rowsAt(o.mission, c2, B, { ...PLAY, yaw: y })])) as Record<number, Row[]>;
  const walk = (a: V, b: V) => Math.round(walkBetween(plan, d, c2, a, b, 0.25) * 10) / 10;
  const sv = svcCheck(o), svcPlan = toPlan(sv.svc);
  const walks = {
    'guard: Office entrance → Café door': walk(pt(o.mission, 'Office entrance'), pt(o.mission, 'Café door')),
    'escort: Service lane → Getaway bay': walk(pt(o.mission, 'Service lane'), pt(o.mission, 'Getaway bay')),
    'Vale: Official entrance → Meet driver': walk(pt(o.mission, 'Official entrance'), pt(o.mission, 'Meet driver')),
    'carry: guard post → service door': walk(pt(o.mission, 'Office entrance'), svcPlan),
    'carry: Meet driver → service door': walk(pt(o.mission, 'Meet driver'), svcPlan),
  };
  return { o, B, c2, byYaw, walks, sv };
});

// (h): the as-built layout read per beat: the Office front at 210, everything else at 30
const nowR = results[0]!, hRows = nowR.byYaw[30]!.map((r, i) => (OFFICE_FRONT.has(r.what) ? { ...nowR.byYaw[210]![i]!, heading: 210 } : { ...r, heading: 30 }));

// ---- report ----
const v1 = (rows: Row[]) => `${rows.filter(ok).length}/${rows.length}`;
for (const r of results) {
  const h30 = r.byYaw[30]!.filter(x => !ok(x));
  console.log(`${r.o.id.padEnd(4)} V-1 yaw30 ${v1(r.byYaw[30]!).padEnd(6)} (120 ${v1(r.byYaw[120]!)}, 210 ${v1(r.byYaw[210]!)}, 300 ${v1(r.byYaw[300]!)}) · service door ${r.sv.dist} m from the public door, holds ${r.sv.holds} · walks ${Object.values(r.walks).join(' / ')} m`);
  console.log(`     hidden at 30: ${h30.map(x => `${x.what} ${pc(x.seen)}`).join(', ')}`);
  console.log(`     service door watchers ${JSON.stringify(r.sv.svcWatchers)} · lane ${JSON.stringify(r.sv.lane)} · onLane ${r.sv.onLane} · lane on civic plot ${r.sv.laneOnCivic} · doors apart ${r.sv.doorsApart} m · guard sees pickup ${r.sv.guardSeesPickup}`);
}
console.log(`h    V-1 per beat ${v1(hRows)} · hidden: ${hRows.filter(x => !ok(x)).map(x => `${x.what} ${pc(x.seen)} @${x.heading}`).join(', ')}`);
const rotation = nowR.byYaw[30]!.map((r, i) => ({ what: r.what, kind: r.kind, ...Object.fromEntries(YAWS.map(y => [`yaw${y}`, { seen: Math.round(nowR.byYaw[y]![i]!.seen * 100) / 100, onlyFade: Math.round(nowR.byYaw[y]![i]!.onlyFade * 100) / 100, ...(nowR.byYaw[y]![i]!.open !== undefined ? { openGround: Math.round(nowR.byYaw[y]![i]!.open! * 100) / 100 } : {}), by: ok(nowR.byYaw[y]![i]!) ? undefined : arch(nowR.byYaw[y]![i]!) }])) }));
const PIN = ['Behind café', 'South walkway', 'Office approach', 'Office entrance', 'patrol-b', 'patrol-reroute', 'camera entrance cone', 'patrol sweep'];
console.log('\nrotation, the eight pinned points (share of the figure seen past architecture; cones: all / open ground):');
for (const n of PIN) { const i = nowR.byYaw[30]!.findIndex(r => r.what === n); console.log(`  ${n.padEnd(22)} ${YAWS.map(y => { const r = nowR.byYaw[y]![i]!; return `${y}: ${pc(r.seen).padStart(5)}${r.open !== undefined ? ` (${pc(r.open)} open)` : ''}${ok(r) ? '' : ` [${arch(r)}]`}`; }).join('  ')}`); }
for (const y of YAWS) console.log(`  yaw ${y}: ${nowR.byYaw[y]!.filter(x => !ok(x)).length} hidden`);

writeFileSync(resolve(OUT, 'door-recheck.json'), JSON.stringify({
  made: 'bun briefs/011/vantage/door-recheck.ts', date: '2026-10-18', limit: V1_LIMIT, yaws: YAWS,
  sources: { plan: plan.version, block: block.adjustments?.sha256, mission: 'contracts/missions/handover/mission.json', serviceDoor: SVC_DOOR, lane: LANE, rules: { DOOR_REACH, DOOR_HALF, WITNESS, P_DEPTH, P_BASE, P_PER } },
  rotation, officeFrontAt210: [...OFFICE_FRONT], h: { v1: v1(hRows), rows: hRows.map(r => ({ what: r.what, heading: r.heading, seen: Math.round(r.seen * 100) / 100 })) },
  options: results.map(r => ({ id: r.o.id, title: r.o.title, isNew: !!r.o.isNew, v1: Object.fromEntries(YAWS.map(y => [y, v1(r.byYaw[y]!)])), hiddenAt30: r.byYaw[30]!.filter(x => !ok(x)).map(x => ({ what: x.what, seen: Math.round(x.seen * 100) / 100, by: arch(x) })), walks: r.walks,
    service: { publicDoor: r.sv.door, serviceDoor: r.sv.svc, metres: r.sv.dist, publicOnLane: r.sv.onLane, laneOnCivicPlot: r.sv.laneOnCivic, publicToOfficialDoor: r.sv.doorsApart, guardSeesPickup: r.sv.guardSeesPickup, watchersOfServiceDoor: r.sv.svcWatchers, laneShareWatched: r.sv.lane, holds: r.sv.holds } })),
}, null, 1));

// ---- pictures ----
const C: Record<string, RGB> = { plot: [229, 219, 203], road: [168, 163, 176], bay: [190, 184, 198], garden: [196, 214, 172], bld: [233, 226, 214], office: [243, 236, 220], tower: [236, 226, 206], cafe: [214, 234, 228], entrance: [118, 182, 168], green: [92, 140, 96], parasol: [236, 206, 196], water: [150, 202, 214], low: [204, 196, 182], bed: [150, 182, 128], lowered: [250, 214, 170] };
const ribbon = (line: V[], half: number): V[][] => { const q: V[][] = []; for (let i = 0; i + 1 < line.length; i++) { const a = line[i]!, b = line[i + 1]!, dx = b[0] - a[0], dy = b[1] - a[1], n = Math.hypot(dx, dy) || 1, nx = (-dy / n) * half, ny = (dx / n) * half, ex = (dx / n) * 0.05, ey = (dy / n) * 0.05; q.push([[a[0] + nx - ex, a[1] + ny - ey], [b[0] + nx + ex, b[1] + ny + ey], [b[0] - nx + ex, b[1] - ny + ey], [a[0] - nx - ex, a[1] - ny - ey]]); } return q; };
function scene(o: Option, f: V, rad: number): { flats: Flat[]; solids: Solid[] } {
  const flats: Flat[] = [], solids: Solid[] = [];
  const inView = (r: V[]) => { const xs = r.map(p => p[0]), ys = r.map(p => p[1]); return Math.max(...xs) > f[0] - rad * 2.5 && Math.min(...xs) < f[0] + rad * 2.5 && Math.max(...ys) > f[1] - rad * 2.5 && Math.min(...ys) < f[1] + rad * 2.5; };
  for (const p of w.ground.plots) { const ring = o.plots ? o.plots(p.id, p.ring) : p.ring; if (inView(ring)) flats.push({ ring, z: 0.18, color: o.id === 'd' && p.id === 'plot/records-office' ? C.garden! : C.plot! }); }
  for (const r of w.ground.roads) { const line = o.roads ? o.roads(r.id, r.line) : r.line; for (const q of ribbon(line, r.width / 2)) if (inView(q)) flats.push({ ring: q, z: 0.02, color: C.road! }); }
  for (const b of o.bays) flats.push({ ring: b, z: 0.05, color: C.bay! });
  for (const b of blockersOfOpt(o)) {
    if (!inView(b.ring)) continue;
    const color = b.kind === 'dressing' ? (/parasol/.test(b.id) ? C.parasol! : /fountain/.test(b.id) ? C.water! : C.green!) : o.tops?.[b.id] !== undefined ? C.lowered! : /cafe/.test(b.id) ? C.cafe! : /tower/.test(b.id) ? C.tower! : /entrance/.test(b.id) ? C.entrance! : /^civic/.test(b.id) ? C.office! : C.bld!;
    solids.push({ id: b.id, ring: b.ring, z0: b.z0, z1: b.z1, color });
  }
  for (const m of w.civic) {
    if (m.height >= 2.5 || !m.ring.length || /Inlay|BedTuft|Bollard|Topiary/.test(m.kit?.id ?? '')) continue;
    if (!m.ring.some(p => Math.hypot(p[0] - f[0], p[1] - f[1]) < rad * 2)) continue;
    const r = o.civicXf(m.id, m.ring); if (!r) continue;
    solids.push({ id: m.id, ring: r, z0: m.base, z1: /CafeTerrace/.test(m.kit?.id ?? '') ? m.base + 0.05 : m.base + m.height, color: /bed|Planter|Palm/.test(`${m.kit?.id ?? ''}${m.surface}`) ? C.bed! : C.low! });
  }
  return { flats, solids };
}
const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
const KEY = ['Café door', 'Office entrance', 'Office approach', 'Service lane', 'Official entrance', 'Meet driver', 'Getaway bay', 'South walkway', 'Behind café', 'cabinet', 'beside-inez', 'patrol-a', 'patrol-b', 'patrol-reroute', 'rival-park', 'crew Enforcer', 'block-spot'];
const colourOf = (r: Row) => (ok(r) ? '#1f8f5f' : r.onlyFade > 0 ? '#e0761b' : '#c62828');
function markers(rows: Row[], focus: V, view: View, size: V, extra: { p: V; label: string; colour: string }[] = []): string {
  const out: string[] = [];
  for (const r of rows) {
    if (r.kind !== 'person') continue;
    const feet = project(r.p, r.z, focus, view, size), head = project(r.p, r.z + 1.75, focus, view, size);
    if (feet[0] < -40 || feet[0] > size[0] + 40 || feet[1] < -40 || feet[1] > size[1] + 40) continue;
    const c = colourOf(r), hidden = !ok(r), show = KEY.includes(r.what);
    out.push(`<line x1="${feet[0]}" y1="${feet[1]}" x2="${head[0]}" y2="${head[1]}" stroke="${c}" stroke-width="5" ${hidden ? 'stroke-dasharray="5 4"' : ''} stroke-linecap="round"/>`);
    out.push(`<circle cx="${feet[0]}" cy="${feet[1]}" r="${show ? 8 : 5}" fill="${hidden ? 'white' : c}" stroke="${c}" stroke-width="3"/>`);
    if (show) out.push(`<text x="${feet[0] + 11}" y="${feet[1] - 6}" font-family="Helvetica, Arial" font-size="17" font-weight="600" fill="#1d2433" stroke="white" stroke-width="4" paint-order="stroke">${esc(r.what)} ${pc(r.seen)}</text>`);
  }
  for (const e of extra) { const s = project(e.p, 0.2, focus, view, size); if (s[0] < 0 || s[0] > size[0] || s[1] < 0 || s[1] > size[1]) continue; out.push(`<rect x="${s[0] - 8}" y="${s[1] - 8}" width="16" height="16" fill="${e.colour}" stroke="white" stroke-width="2"/><text x="${s[0] + 12}" y="${s[1] + 18}" font-family="Helvetica, Arial" font-size="16" font-weight="700" fill="${e.colour}" stroke="white" stroke-width="4" paint-order="stroke">${esc(e.label)}</text>`); }
  return out.join('\n');
}
const LEGEND = `<circle cx="10" cy="10" r="7" fill="#1f8f5f"/><text x="24" y="15">seen (≥ 50 % of a figure)</text>
<circle cx="230" cy="10" r="7" fill="white" stroke="#e0761b" stroke-width="3"/><text x="244" y="15">only through the fade</text>
<circle cx="10" cy="40" r="7" fill="white" stroke="#c62828" stroke-width="3"/><text x="24" y="45">hidden</text>
<rect x="223" y="33" width="14" height="14" fill="#333"/><text x="244" y="45">Office service door (brief 013)</text>`;
const svgWrap = (size: V, body: string) => `<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="${size[0]}" height="${size[1]}" viewBox="0 0 ${size[0]} ${size[1]}">${body}</svg>`;
const img = (bytes: Uint8Array, x: number, y: number, ww: number, hh: number) => `<image x="${x}" y="${y}" width="${ww}" height="${hh}" xlink:href="data:image/png;base64,${Buffer.from(bytes).toString('base64')}"/>`;
function write(file: string, svg: string, outW?: number) { const tmp = resolve(ROOT, `out/vantage/${file}.svg`); mkdirSync(resolve(ROOT, 'out/vantage'), { recursive: true }); writeFileSync(tmp, svg); execFileSync('rsvg-convert', [...(outW ? ['-w', String(outW)] : []), '-o', resolve(OUT, `${file}.png`), tmp]); }
function caption(size: V, l1: string, l2: string) {
  return `<rect x="0" y="${size[1] - 74}" width="${size[0]}" height="74" fill="white" fill-opacity="0.88"/>
<text x="18" y="${size[1] - 44}" font-family="Helvetica, Arial" font-size="22" font-weight="700" fill="#1d2433">${esc(l1)}</text>
<text x="18" y="${size[1] - 16}" font-family="Helvetica, Arial" font-size="17" fill="#333">${esc(l2)}</text>
<g transform="translate(${size[0] - 560}, ${size[1] - 66})" font-family="Helvetica, Arial" font-size="16" fill="#1d2433">${LEGEND}</g>`;
}
const SIZE: V = [1600, 900];
/** One frame: option o at heading yaw, centred on focus, zoom; returns the composed SVG body. */
function frame(r: (typeof results)[number], yaw: number, focus: V, zoom: number, l1: string, l2: string): string {
  const view: View = { ...PLAY, yaw, zoom }, S = scene(r.o, focus, zoom * 2.2);
  const base = png(render({ focus, view, size: SIZE, ss: 2 }, S.flats, S.solids), SIZE[0], SIZE[1]);
  return img(base, 0, 0, SIZE[0], SIZE[1]) + markers(r.byYaw[yaw]!, focus, view, SIZE, [{ p: toPlan(r.sv.svc), label: 'service door', colour: '#333' }]) + caption(SIZE, l1, l2);
}
const doorOf = (r: (typeof results)[number]) => pt(r.o.mission, 'Office entrance');
const overviewFocus = (o: Option): V => (o.id === 'd' ? [-158, -240] : [-157, -262]);
// 1 · rotation: the as-built door at play zoom and the overview, at the four headings (2 x 2)
for (const [file, zoom, focusOf] of [['rot-door-four', 14, doorOf], ['rot-overview-four', 34, (r: (typeof results)[number]) => overviewFocus(r.o)]] as const) {
  const tiles = YAWS.map(y => { const rows = nowR.byYaw[y]!, door = rows.find(x => x.what === 'Office entrance')!; return frame(nowR, y, focusOf(nowR), zoom, `as built, yaw ${y}${y === 30 ? ' (opening view)' : ''} · ${v1(rows)} points seen`, `Office entrance ${pc(door.seen)} · ${file === 'rot-door-four' ? 'play zoom 14' : 'zoomed out to 34'} · Q/E turns the view a quarter`); });
  write(file, svgWrap([3200, 1800], tiles.map((t, i) => `<svg x="${(i % 2) * 1600}" y="${Math.floor(i / 2) * 900}" width="1600" height="900">${t}</svg>`).join('')), 2000);
}
write('h-door-210', svgWrap(SIZE, frame(nowR, 210, doorOf(nowR), 14, '(h) as built, the Office door at yaw 210 (two quarter turns)', `Office front: ${[...OFFICE_FRONT].map(n => { const x = nowR.byYaw[210]!.find(r => r.what === n)!; return `${n} ${pc(x.seen)}`; }).slice(0, 4).join(', ')}`)));
// 2 · new options at the play camera (yaw 30)
for (const r of results.filter(x => x.o.isNew && x.o.id !== 'e2' && x.o.id !== 'f2')) {
  write(`${r.o.id}-overview`, svgWrap(SIZE, frame(r, 30, overviewFocus(r.o), 34, `${r.o.title} · V-1 ${v1(r.byYaw[30]!)} at yaw 30`, `zoomed out to 34 · guard walk ${r.walks['guard: Office entrance → Café door']} m · service door ${r.sv.dist} m from the public door · lane rule ${r.sv.holds ? 'holds' : 'fails'}`)));
  const dr = r.byYaw[30]!.find(x => x.what === 'Office entrance')!;
  write(`${r.o.id}-door`, svgWrap(SIZE, frame(r, 30, doorOf(r), 14, `${r.o.short}: the public door at play zoom 14, yaw 30`, `Office entrance ${pc(dr.seen)} seen past architecture (${pc(dr.onlyFade)} only through the fade)`)));
}
// 3 · the plan sheet: each option top-down (block frame, north up), the service side and the watchers
const PX = 9, PW = 560, PH = 520;
function panel(r: (typeof results)[number], ox: number, oy: number): string {
  const s = r.sv, c2 = r.c2, o = r.o;
  const offC = cen(c2.buildings.find(b => b.id === 'civic-office')!.poly.map(P2B));
  const cx = offC[0] - 6, cy = offC[1] - 4; // view centre (block)
  const S = (b: V): [number, number] => [ox + PW / 2 + (b[1] - cy) * PX, oy + PH / 2 + (b[0] - cx) * PX];
  const poly = (ring: V[], attrs: string) => `<polygon points="${ring.map(p => S(p).join(',')).join(' ')}" ${attrs}/>`;
  const out: string[] = [`<svg x="${ox}" y="${oy}" width="${PW}" height="${PH}" overflow="hidden"><rect x="0" y="0" width="${PW}" height="${PH}" fill="#f4efe6"/></svg>`];
  const clip = `clip-path="url(#c${o.id})"`;
  out.push(`<defs><clipPath id="c${o.id}"><rect x="${ox}" y="${oy}" width="${PW}" height="${PH}"/></clipPath></defs><g ${clip}>`);
  for (const p of w.ground.plots) { const ring = (o.plots ? o.plots(p.id, p.ring) : p.ring).map(P2B); out.push(poly(ring, 'fill="#e5dbcb" stroke="#cfc3b0"')); }
  out.push(poly(s.laneRing, 'fill="#f3d36b" fill-opacity="0.55" stroke="#b8901c" stroke-dasharray="6 4"'));
  // the watchers: guard cone, camera cone, sweep band, crowd ring
  const cone = (o0: V, f: V, reach: number, half: number) => { const L = Math.hypot(f[0], f[1]), u: V = [f[0] / L, f[1] / L], n: V = [-u[1], u[0]]; return [o0, [o0[0] + u[0] * reach + n[0] * half, o0[1] + u[1] * reach + n[1] * half], [o0[0] + u[0] * reach - n[0] * half, o0[1] + u[1] * reach - n[1] * half]] as V[]; };
  const sweepPoly = (): V[] => { const f = o.patrolFacing, L = Math.hypot(f[0], f[1]), u: V = [f[0] / L, f[1] / L], n: V = [-u[1], u[0]], a = s.pa, b = s.pb, far = (p: V, k: number): V => [p[0] + u[0] * P_DEPTH + n[0] * k * (P_BASE + P_DEPTH * P_PER), p[1] + u[1] * P_DEPTH + n[1] * k * (P_BASE + P_DEPTH * P_PER)]; const near = (p: V, k: number): V => [p[0] + n[0] * k * P_BASE, p[1] + n[1] * k * P_BASE]; const side = (b[0] - a[0]) * n[0] + (b[1] - a[1]) * n[1] >= 0 ? 1 : -1; return [near(a, -side), near(b, side), far(b, side), far(a, -side)]; };
  out.push(poly(sweepPoly(), 'fill="#e48aa8" fill-opacity="0.18" stroke="#c2577b" stroke-opacity="0.5"'));
  out.push(poly(cone(s.door, o.guardFacing, DOOR_REACH, DOOR_HALF), 'fill="#f0a020" fill-opacity="0.2" stroke="#c07a00"'));
  out.push(poly(cone(s.cam, s.camF, 22, 9), 'fill="#d23c3c" fill-opacity="0.22" stroke="#a02020"'));
  out.push(`<circle cx="${S(s.door)[0]}" cy="${S(s.door)[1]}" r="${WITNESS * PX}" fill="none" stroke="#2a6fb0" stroke-width="2" stroke-dasharray="7 5"/>`);
  for (const b of c2.buildings) { out.push(poly(b.poly.map(P2B), `fill="${b.id === 'cafe' ? '#cfe6de' : '#fbf7ee'}" stroke="#555" stroke-width="1.5"`)); for (const t of b.towers ?? []) if (t.poly) out.push(poly(t.poly.map(P2B), `fill="${o.tops?.[`civic/${b.id}/tower-${t.id}`] !== undefined ? '#fad6aa' : '#e7dcc6'}" stroke="#555"`)); }
  const dot = (p: V, col: string, label: string, sq = false) => { const q = S(p); return (sq ? `<rect x="${q[0] - 7}" y="${q[1] - 7}" width="14" height="14" fill="${col}"/>` : `<circle cx="${q[0]}" cy="${q[1]}" r="7" fill="${col}" stroke="white" stroke-width="2"/>`) + `<text x="${q[0] + 10}" y="${q[1] + 5}" font-family="Helvetica, Arial" font-size="14" font-weight="700" fill="${col}" stroke="white" stroke-width="3.5" paint-order="stroke">${esc(label)}</text>`; };
  out.push(dot(s.door, '#2a6fb0', 'public door'), dot(s.official, '#7a4bb0', 'official'), dot(s.svc, '#222', 'service door', true));
  const ar = (from: V, to: V) => { const a = S(from), b = S(to); return `<line x1="${a[0]}" y1="${a[1]}" x2="${b[0]}" y2="${b[1]}" stroke="#2a6fb0" stroke-width="3" marker-end="url(#arr)"/>`; };
  out.push(ar(s.door, [s.door[0] + o.guardFacing[0] * 5, s.door[1] + o.guardFacing[1] * 5]));
  out.push('</g>');
  const verdict = s.holds ? 'holds' : 'fails', vc = s.holds ? '#1f8f5f' : '#c62828';
  out.push(`<text x="${ox + 12}" y="${oy + 26}" font-family="Helvetica, Arial" font-size="19" font-weight="700" fill="#1d2433">${esc(o.short)} · V-1 ${v1(r.byYaw[30]!)}</text>`);
  out.push(`<text x="${ox + 12}" y="${oy + 50}" font-family="Helvetica, Arial" font-size="15" fill="${vc}" font-weight="700">service door: ${verdict} · ${s.dist} m from the public door${s.onLane ? ' · public door on the lane' : ''}</text>`);
  const wl = Object.entries(s.svcWatchers).filter(([k, v]) => (typeof v === 'number' ? v > 0 : v)).map(([k, v]) => (typeof v === 'number' ? `${k} ${pc(v)}` : k));
  out.push(`<text x="${ox + 12}" y="${oy + 70}" font-family="Helvetica, Arial" font-size="14" fill="#333">watching the service door: ${esc(wl.join(', ') || 'nobody')}</text>`);
  out.push(`<rect x="${ox}" y="${oy}" width="${PW}" height="${PH}" fill="none" stroke="#999"/>`);
  return out.join('\n');
}
{
  const show = results.filter(r => !['e2', 'f', 'f2'].includes(r.o.id)), cols = 4, rowsN = Math.ceil(show.length / cols), W2 = cols * PW + (cols + 1) * 16, H2 = rowsN * PH + (rowsN + 1) * 16 + 110;
  const body = `<rect width="${W2}" height="${H2}" fill="white"/><defs><marker id="arr" markerWidth="8" markerHeight="8" refX="4" refY="4" orient="auto"><path d="M0,0 L8,4 L0,8 z" fill="#2a6fb0"/></marker></defs>` + show.map((r, i) => panel(r, 16 + (i % cols) * (PW + 16), 16 + Math.floor(i / cols) * (PH + 16))).join('\n') +
    `<g transform="translate(16, ${H2 - 96})" font-family="Helvetica, Arial" font-size="17" fill="#1d2433"><text x="0" y="20" font-weight="700">Brief 011 door options re-checked against brief 013's Office service door (black square) and the Service lane (yellow, dashed), block frame, north up, 9 px/m.</text>
<text x="0" y="46">Amber: the guard's door cone (26 m, 19 m half). Red: the entrance camera (22 m, 9 m half). Rose: the patrol sweep (24 m deep). Blue dashed ring: 13 m witness range round the public door. Arrow: the way the public door faces.</text>
<text x="0" y="72">"Holds" = the public door is 13 m or more from the service door, its door and approach are off the lane, and neither the guard, the entrance camera nor the public door's crowd sees the service door (2D line past the civic footprints).</text></g>`;
  write('recheck-plan', svgWrap([W2, H2], body), 2200);
}
// 4 · what the camera sees of the ground round the Office at each heading (1 m cells, a figure's middle 0.9 m up, past architecture)
{
  const pw = 620, ph = 560, k = 9, cx0 = 58, cy0 = -30, B = blockersOfOpt(NOW), parts: string[] = [`<rect width="${4 * pw + 80}" height="${ph + 150}" fill="white"/>`];
  YAWS.forEach((yaw, i) => {
    const ox = 16 + i * (pw + 16), oy = 16, S = (b: V): [number, number] => [ox + pw / 2 + (b[1] - cy0) * k, oy + ph / 2 + (b[0] - cx0) * k], v: View = { ...PLAY, yaw };
    parts.push(`<rect x="${ox}" y="${oy}" width="${pw}" height="${ph}" fill="#f4efe6" stroke="#999"/>`);
    const foot = civ.buildings.flatMap(b => [b.poly, ...(b.towers ?? []).flatMap(t => (t.poly ? [t.poly] : []))]);
    for (let x = cx0 - 30; x < cx0 + 30; x += 1) for (let y = cy0 - 34; y < cy0 + 34; y += 1) {
      const p = toPlan([x + 0.5, y + 0.5]); if (foot.some(f => inPoly(p, f))) continue;
      const vis = !B.some(b => b.kind === 'architecture' && blocks(p, 1.08, b, v)); const q = S([x, y]);
      parts.push(`<rect x="${q[0]}" y="${q[1]}" width="${k}" height="${k}" fill="${vis ? '#9fd3b0' : '#e7a3a3'}"/>`);
    }
    for (const b of civ.buildings) { parts.push(`<polygon points="${b.poly.map(P2B).map(p => S(p).join(',')).join(' ')}" fill="#fbf7ee" stroke="#555"/>`); for (const t of b.towers ?? []) if (t.poly) parts.push(`<polygon points="${t.poly.map(P2B).map(p => S(p).join(',')).join(' ')}" fill="#e7dcc6" stroke="#555"/>`); }
    parts.push(`<polygon points="${LANE.map(p => S(p).join(',')).join(' ')}" fill="none" stroke="#b8901c" stroke-width="2" stroke-dasharray="6 4"/>`);
    const lab = (p: V, t: string, col: string, sq = false) => { const q = S(p); return (sq ? `<rect x="${q[0] - 6}" y="${q[1] - 6}" width="12" height="12" fill="${col}"/>` : `<circle cx="${q[0]}" cy="${q[1]}" r="6" fill="${col}" stroke="white" stroke-width="2"/>`) + `<text x="${q[0] + 9}" y="${q[1] + 5}" font-family="Helvetica, Arial" font-size="13" font-weight="700" fill="${col}" stroke="white" stroke-width="3" paint-order="stroke">${t}</text>`; };
    parts.push(lab([51.75, -30], 'public', '#2a6fb0'), lab([76.25, -30], 'official', '#7a4bb0'), lab(SVC_DOOR, 'service', '#222', true));
    const tc: V = [Math.cos((yaw * Math.PI) / 180), -Math.sin((yaw * Math.PI) / 180)]; // toward camera, block frame: plan (-sin, -cos) -> block (cos, -sin)
    const c0 = S([cx0 + 24, cy0 - 26]); parts.push(`<line x1="${c0[0]}" y1="${c0[1]}" x2="${c0[0] + tc[1] * 40}" y2="${c0[1] + tc[0] * 40}" stroke="#1d2433" stroke-width="3" marker-end="url(#arr2)"/><text x="${c0[0] - 30}" y="${c0[1] + 60}" font-family="Helvetica, Arial" font-size="13" fill="#1d2433">to the camera</text>`);
    parts.push(`<text x="${ox + 10}" y="${oy + 24}" font-family="Helvetica, Arial" font-size="20" font-weight="700" fill="#1d2433">yaw ${yaw}</text>`);
  });
  parts.push(`<defs><marker id="arr2" markerWidth="8" markerHeight="8" refX="4" refY="4" orient="auto"><path d="M0,0 L8,4 L0,8 z" fill="#1d2433"/></marker></defs>`);
  parts.push(`<g transform="translate(16, ${ph + 50})" font-family="Helvetica, Arial" font-size="17" fill="#1d2433"><text x="0" y="20" font-weight="700">The ground round the Records Office as built: green = a figure's middle (0.9 m) is seen past architecture, red = hidden. Block frame, north up, 9 px/m; yellow dashed = the Service lane.</text>
<text x="0" y="46">At the opening view (yaw 30) only the ground off the south and west faces is seen: the public door's north side is behind 15.5 m wings and the 38 m tower. Each heading shows two faces.</text><text x="0" y="72">The public door's front (north) is seen at 120 and 210; the café's front (south, off this sheet) at 30 and 300.</text></g>`);
  write('ground-four', svgWrap([4 * pw + 80, ph + 150], parts.join('\n')), 2400);
}
