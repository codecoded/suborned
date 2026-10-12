// bun run block: write contracts/world/civic-block.json (suborned.block/3), the civic block in BLOCK frame
// (the Unity block at yaw 90: x runs south, y east; z up). The ONLY place that reads the old repo, and only here.
// It reads the Oct 9 city-scale sources, applies contracts/world/civic-block.adjust.json (Vantage's explicit edits,
// each with a reason), and records every source path + sha256 and every applied adjustment in the output.
// usage (repo root): bun tools/plan/block.ts [--src <TheSuborned asset root>]
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { homedir } from 'node:os';
import { resolve } from 'node:path';

const SRC_LABEL = '~/Projects/the-suborned/game/Assets/TheSuborned';
const root = resolve(import.meta.dir, '../..'), args = process.argv.slice(2);
const srcRoot = (args.includes('--src') ? args[args.indexOf('--src') + 1] : undefined) ?? SRC_LABEL.replace('~', homedir());
const OUT = resolve(root, 'contracts/world/civic-block.json');
const ADJUST = resolve(root, 'contracts/world/civic-block.adjust.json');

type P = [number, number];
type Item = { id: string; name?: string; kind?: string; category?: string; polygon: P[]; z: number; height: number; [k: string]: unknown };

// ---- constants transcribed from the Oct 9 editor code (not data files; the numbers are not in the nav json) ----
// HandoverCafe.cs:18  X = 11, Front = -1.3, Back = 10   corner | bay | corner = 22 m wide, 11.3 m deep
// HandoverCafe.cs:19  Arc = 5.3                        corner radius
// HandoverCafe.cs:20  Floor = .45, Ceiling = 4.6, Height = 34.8
const CAFE = { width: 22, depth: 11.3, height: 34.8, cornerArc: 5.3, terrace: 0.45, ceiling: 4.6 };
// HandoverKit.cs:20,24,25,99  "footprints are the nav's; heights are the city's": the Records Office is the Hall set at
// x0.75 in plan (HallScale) and x1.4 in height (HallRise); entrance towers x2 (TowerRise); giant order 12 m (ColumnRise).
// The Hall prefab's own height is not in these files: wings 15.5, crown 38, order 12 are Brief 001 A0's figures (Aster).
const OFFICE = { wings: 15.5, crown: 38, giantOrder: 12, hallScale: 0.75, hallRise: 1.4, towerRise: 2 };

const sources: { path: string; sha256: string }[] = [];
const read = (rel: string) => {
  const text = readFileSync(resolve(srcRoot, rel));
  sources.push({ path: rel, sha256: createHash('sha256').update(text).digest('hex') });
  return text.toString('utf8');
};
const nav = JSON.parse(read('Data/World/handover-navigation.json'));
const doors = JSON.parse(read('Data/World/handover-destinations.json')).destinations; // only the two door positions are read: doors are world
// The source names two bays and their pavilions after the old mission; the world names them by place.
const BY_PLACE: [RegExp, string][] = [[/^getaway-pavilion-support-/, 'square-pavilion-support-'], [/^boarding-pavilion-support-/, 'office-pavilion-support-']];
for (const s of nav.solids) for (const [re, to] of BY_PLACE) s.id = s.id.replace(re, to);
read('Editor/World/HandoverCafe.cs'); read('Editor/World/HandoverKit.cs'); // hashed: the constants above come from them

const r3 = (x: number) => { const v = Math.round(x * 1000) / 1000; return v === 0 ? 0 : v; };
const pt = (p: { x: number; y: number }): P => [r3(p.x), r3(p.y)];
const circle = (c: P, r: number, n: number): P[] => Array.from({ length: n }, (_, i) => [r3(c[0] + r * Math.cos((2 * Math.PI * i) / n)), r3(c[1] + r * Math.sin((2 * Math.PI * i) / n))]);
const rect = (x: number[], y: number[]): P[] => [[x[0], y[0]], [x[1], y[0]], [x[1], y[1]], [x[0], y[1]]].map(p => [r3(p[0]), r3(p[1])] as P);
const solid = (id: string) => { const s = nav.solids.find((x: any) => x.id === id); if (!s) throw new Error(`source has no solid ${id}`); return s; };
const poly = (s: any): P[] => s.type === 'polygon' ? s.vertices.map(pt) : s.type === 'cylinder' ? circle([s.center?.x ?? (s.x[0] + s.x[1]) / 2, s.center?.y ?? (s.y[0] + s.y[1]) / 2], s.radius, s.radius > 2 ? 32 : 16) : rect(s.x, s.y);
const from = (s: any, extra: object = {}): Item => ({ id: s.id, ...extra, polygon: poly(s), z: r3(s.z[0]), height: r3(s.z[1] - s.z[0]) } as Item);

// ---- buildings ----
const cafeSolid = solid('corner-cafe'), officeSolid = solid('civic-office-site');
const bbox = (p: P[]) => [Math.min(...p.map(v => v[0])), Math.max(...p.map(v => v[0])), Math.min(...p.map(v => v[1])), Math.max(...p.map(v => v[1]))];
const [cx0, cx1, cy0, cy1] = bbox(poly(cafeSolid));
if (Math.abs(cx1 - cx0 - CAFE.depth) > 1e-6 || Math.abs(cy1 - cy0 - CAFE.width) > 1e-6 || r3(cafeSolid.z[1]) !== CAFE.height)
  throw new Error(`corner-cafe in the nav is ${r3(cy1 - cy0)} x ${r3(cx1 - cx0)} x ${cafeSolid.z[1]}, HandoverCafe.cs says ${CAFE.width} x ${CAFE.depth} x ${CAFE.height}`);
const officePoly = poly(officeSolid), [ox0, ox1, oy0, oy1] = bbox(officePoly), faceY = r3((oy0 + oy1) / 2);
// ids stay 'cafe' / 'civic-office' (the checker and Latch's builder key on them); `source` is the nav id.
const door = (name: string): { at: P } => { const d = doors.find((x: any) => x.name === name); if (!d) throw new Error(`source has no door ${name}`); return { at: [r3(d.x), r3(d.y)] }; };
const buildings = [
  { id: 'cafe', name: 'Corner café', source: 'corner-cafe', polygon: poly(cafeSolid), z: 0, height: CAFE.height, interior: { floor: CAFE.terrace, ceiling: CAFE.ceiling, cornerArc: CAFE.cornerArc }, entrances: [{ id: 'cafe-entrance', name: 'Café entrance', ...door('Café door') }] },
  {
    id: 'civic-office', name: 'Records Office', source: 'civic-office-site', polygon: officePoly, z: 0, height: OFFICE.wings,
    entrances: [{ id: 'office-entrance', name: 'Office entrance', ...door('Office entrance') }],
    heights: { wings: OFFICE.wings, crown: OFFICE.crown, giantOrder: OFFICE.giantOrder },
    // entrance towers sit on the two long faces (public west, official east); the sources give no tower footprint
    towers: [{ id: 'public', at: [r3(ox0), faceY], height: OFFICE.crown }, { id: 'official', at: [r3(ox1), faceY], height: OFFICE.crown }],
  },
] as any[];

// ---- structures and objects ----
const structures: Item[] = [
  ...nav.solids.filter((s: any) => s.kind === 'pavilion-support').map((s: any) => from(s, { name: s.id, category: 'pavilion-support' })),
  ...nav.solids.filter((s: any) => s.kind === 'service-cabinet').map((s: any) => from(s, { name: s.id, category: 'service-cabinet' })),
  from(solid('foreground-civic-robot'), { name: 'Civic steward monument', category: 'civic-monument' }),
];
const objects: Item[] = nav.solids
  .filter((s: any) => ['fountain', 'planter', 'table'].includes(s.kind))
  .map((s: any) => from(s, { kind: s.kind }));


// ---- adjustments ----
type Adj = { op: 'addBay' | 'addStructure' | 'edit'; id?: string; reason: string; polygon?: P[]; item?: Item; move?: P; z?: number; height?: number };
const adjFile = readFileSync(ADJUST, 'utf8');
const adj: { schema: string; owner: string; adjustments: Adj[] } = JSON.parse(adjFile);
if (adj.schema !== 'suborned.block-adjust/1') throw new Error('adjust file schema must be suborned.block-adjust/1');
const bays: { id: string; polygon: P[] }[] = [];
const all = new Map<string, Item>([...structures, ...objects].map(i => [i.id, i]));
for (const a of adj.adjustments) {
  if (!a.reason?.trim()) throw new Error(`adjustment without a reason: ${JSON.stringify(a)}`);
  if (a.op === 'addBay') bays.push({ id: a.id!, polygon: a.polygon! });
  else if (a.op === 'addStructure') { structures.push(a.item!); all.set(a.item!.id, a.item!); }
  else if (a.op === 'edit') {
    const it = all.get(a.id!); if (!it) throw new Error(`adjustment ${a.id}: no such item in the source (stale adjustment?)`);
    if (a.move) it.polygon = it.polygon.map(([x, y]) => [r3(x + a.move![0]), r3(y + a.move![1])] as P);
    if (a.z !== undefined) it.z = a.z;
    if (a.height !== undefined) it.height = a.height;
  } else throw new Error(`unknown adjustment op ${(a as any).op}`);
}

const out = {
  schema: 'suborned.block/3', id: 'civic',
  frame: {
    name: 'block', units: 'm', x: 'south (the Unity block at yaw 90)', y: 'east', z: 'up',
    z0: 'height above the local ground: pavement top (plan 0.18), or the lay-by surface inside a bay (plan 0)',
    toPlan: 'plan = origin + x * ax + y * ay, with origin (-130, -230), ax (0, -1), ay (1, 0): plan x = -130 + y, plan y = -230 - x',
  },
  sourceRoot: SRC_LABEL,
  sources,
  adjustments: { file: 'contracts/world/civic-block.adjust.json', sha256: createHash('sha256').update(adjFile).digest('hex'), owner: adj.owner, applied: adj.adjustments },
  buildings, structures, objects, bays,
};

// numbers-only arrays on one line, everything else indented
const text = JSON.stringify(out, null, 1).replace(/\[\s+(-?[\d.e+-]+(?:,\s+-?[\d.e+-]+)*)\s+\]/g, (_, n) => `[${n.replace(/\s+/g, ' ')}]`) + '\n';
writeFileSync(OUT, text);
const kinds = (a: Item[]) => Object.entries(a.reduce<Record<string, number>>((m, i) => ((m[i.kind ?? i.category ?? '?'] = (m[i.kind ?? i.category ?? '?'] ?? 0) + 1), m), {})).map(([k, n]) => `${k} ${n}`).join(', ');
console.log(`block → ${OUT} (${Buffer.byteLength(text)} bytes)`);
console.log(`sources: ${sources.map(s => `${s.path} ${s.sha256.slice(0, 12)}`).join('; ')}`);
console.log(`buildings ${buildings.length} (cafe ${cx1 - cx0} x ${r3(cy1 - cy0)} x ${CAFE.height}; office ${r3(ox1 - ox0)} x ${r3(oy1 - oy0)}, wings ${OFFICE.wings}, crown ${OFFICE.crown}), structures ${structures.length} [${kinds(structures)}], objects ${objects.length} [${kinds(objects)}], bays ${bays.length}, entrances ${buildings.flatMap(b => b.entrances).length}`);
console.log(`omitted from ${nav.solids.length} nav solids: cars, sentries, globes, lamps, chairs, parasols, street props, access ramps (not exported before; add when a consumer needs them)`);
console.log(`adjustments applied: ${adj.adjustments.length} (${adj.owner})`);
