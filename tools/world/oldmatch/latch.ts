// Latch's swap export (`latch.oldbuilt/1`, briefs/075/oldbuild/swap/built.json, unity/Assets/Suborned/Editor/OldTown/Swap/SwapExport.cs) read as
// suborned.oldbuilt/1 (contracts/world/oldbuilt.schema.ts) for the plan-match check (brief 075 remap step 2, Splice; agreed in briefs/075/notes.md "Plan match").
// Latch's file is kept as the Unity side writes it; this reader does the geometry the old builders' pieces imply:
//   - points are already in the plan frame (SwapExport.Pl: plan (x, y) = old Unity (x, z - 206)), so offset [0, 0];
//   - buildings: oriented boxes with base/top; kind mass → domestic, frontage → commercial, building → flagship, landmark → landmark; origin old;
//   - frontages (the old builders model no doors): each ground-storey bay front becomes an entrance of kind `bay`, its facade point, outward normal and width:
//     a stand-in, matched as "a door of ours lies on this bay" (match.ts), never door to door;
//   - kerbs: one stone per 2 m module at the kerb line (DecoKitStreetLayout: pivot on the kerb line, its forward towards the carriageway, the stone along
//     the perpendicular); corners are R 2 m quarter arcs about the cell's walk-side corner; noses half-rounds R 1; returned as kerb lines (no road centrelines:
//     the asphalt meshes are decks laid under the pavements, not carriageway extents, so they are not used);
//   - kerb stones joined end to end into kerb lines (ends within 2 cm), so a mismatch is a run along a kerb, not a stone;
//   - pavements: 2 m flag modules centred on `at`, turned by their Unity `yaw` (swap2 on), folded to a quarter turn; a file without yaw (the first swap) has
//     each module's turn read from its neighbours on the same grid (a module 2 m away: the direction to it, folded to a quarter turn), axis-aligned when none;
//   - levels: old/road and old/walk are the street level's z and walk; old/promenade is kept as a level (plan-match reads it as the plan ground at its walk height, standards.levels.grounds); old/water is no walk level;
//     the export's ringed levels (our decks and station platforms, kept by the swap) are decks;
//   - coverage: the old builders lay kerb stones and flags only in the core (the boulevard, Southside, the side street, the circus); the district beyond is
//     massing on a flat ground. Ground comparisons (kerbs, pavement edges) run inside the coverage (the pieces' 2 m cells grown by COVER m), never beyond it;
//   - the Handover zone is not in the file ("compare the zone to the export itself"): zone 'ours', the check takes it from our export.
import type { OldBuilt } from '../../../contracts/world/oldbuilt.schema';

type V = [number, number];
export type LatchBuilt = {
  schema: 'latch.oldbuilt/1'; status?: string; brief?: string; frame?: string; scene: string; heights?: string; spacehq?: string;
  export?: { source: string; sha256: string }; handoverZone?: [number, number, number, number]; inZone?: string;
  buildings: { piece: string; kind: 'mass' | 'frontage' | 'building' | 'landmark' | string; ring: V[]; base: number; top: number; building: string; front?: V; at?: V }[];
  frontages: { building: string; piece: string; at: V; normal: V; w: number; kind?: string }[];
  kerbs: { piece: string; at: V; yaw: number; top: number }[];
  pavements: { piece: string; at: V; size: number; top: number; yaw?: number }[];
  roads: { name: string; top: number; triangles: number[] }[];
  levels: { id: string; height: number; ring?: V[]; what?: string }[];
  counts?: Record<string, number>;
};

/** m: the ground coverage grows each kerb or flag cell by this (a plan kerb this close to the old ground is compared). */
export const COVER = 6;
const r3 = (x: number) => Math.round(x * 1000) / 1000;
const KIND: Record<string, OldBuilt['buildings'][number]['kind']> = { mass: 'domestic', frontage: 'commercial', building: 'flagship', landmark: 'landmark' };

/** Unity yaw (degrees, clockwise from +Z seen from above) applied to a local (x, z) offset, in plan (x, y). */
const rot = (yaw: number, l: V): V => { const a = (yaw * Math.PI) / 180, c = Math.cos(a), s = Math.sin(a); return [l[0] * c + l[1] * s, -l[0] * s + l[1] * c]; };
const add = (a: V, b: V): V => [r3(a[0] + b[0]), r3(a[1] + b[1])];

/** The kerb line a stone lays, in plan points. Local frame: +z = the stone's forward (towards the carriageway), the stone along local x. */
export function kerbLine(k: LatchBuilt['kerbs'][number]): V[] {
  const at = k.at, arc = (c: V, r: number, a0: number, a1: number, n: number) => Array.from({ length: n + 1 }, (_, i) => { const t = ((a0 + ((a1 - a0) * i) / n) * Math.PI) / 180; return add(at, rot(k.yaw, [c[0] + r * Math.cos(t), c[1] + r * Math.sin(t)])); });
  if (k.piece.startsWith('Kerb_GraniteCorner')) return arc([1, -1], 2, 90, 180, 8); // the cell's walk-side corner is local (1, -1): DecoKitStreetLayout.SideStreet
  if (k.piece.startsWith('Kerb_GraniteNose')) return arc([0, 0], 1, 0, 180, 8); // a half-round island end, bulging forward
  return [add(at, rot(k.yaw, [-1, 0])), add(at, rot(k.yaw, [1, 0]))];
}

export function fromLatch(b: LatchBuilt): OldBuilt {
  if (b.schema !== 'latch.oldbuilt/1') throw new Error(`built.json: schema ${b.schema}, want latch.oldbuilt/1`);
  const lv = (id: string) => b.levels.find(l => l.id === id)?.height;
  const street = { id: 'street', z: lv('old/road') ?? 0, walk: lv('old/walk') };
  const levelName = (l: { height: number; what?: string }) => (/concourse|platform/.test(l.what ?? '') || Math.abs(l.height - 24) < 0.01 ? 'concourse' : 'deck');
  // the raised levels are ours, kept by the swap: one level per name at the height its ringed levels stand (the first; a second height under one name is a
  // deck on no level, reported by the deck check)
  const raised = new Map<string, number>();
  for (const l of b.levels) if (l.ring && l.ring.length >= 3 && !raised.has(levelName(l))) raised.set(levelName(l), l.height);
  const levels: OldBuilt['levels'] = [street, ...[...raised].map(([id, z]) => ({ id, z, walk: z })), ...b.levels.filter(l => l.id === 'old/promenade').map(l => ({ id: l.id, z: l.height }))];
  const turns = pavementTurns(b.pavements);
  const tile = (p: LatchBuilt['pavements'][number], i: number): V[] => { const h = p.size / 2, a = turns[i]!, c = Math.cos(a) * h, s = Math.sin(a) * h, [x, y] = p.at; return [[x - c + s, y - s - c], [x + c + s, y + s - c], [x + c - s, y + s + c], [x - c - s, y - s + c]].map(q => [r3(q[0]!), r3(q[1]!)] as V); };
  const grow = (pts: V[], g: number): V[] => { const xs = pts.map(p => p[0]), ys = pts.map(p => p[1]), x0 = Math.min(...xs) - g, x1 = Math.max(...xs) + g, y0 = Math.min(...ys) - g, y1 = Math.max(...ys) + g; return [[x0, y0], [x1, y0], [x1, y1], [x0, y1]]; };
  const kerbs = joinLines(b.kerbs.map((k, i) => ({ id: `kerb/${k.piece}#${i}`, line: kerbLine(k), top: k.top })));
  return {
    schema: 'suborned.oldbuilt/1',
    source: { scene: b.scene, builder: 'Suborned.OldTown.Editor.Swap.SwapExport (latch.oldbuilt/1, read by tools/world/oldmatch/latch.ts)', heights: b.heights, ...(b.export ? { export: `${b.export.source} sha256 ${b.export.sha256}` } : {}) } as OldBuilt['source'],
    frame: { units: 'm', offset: [0, 0] },
    levels,
    buildings: b.buildings.map((x, i) => ({ id: `${x.piece}#${i}`, building: x.building, ring: x.ring, base: x.base, height: r3(x.top - x.base), kind: KIND[x.kind] ?? 'other', origin: 'old' as const, piece: x.piece })),
    entrances: b.frontages.map((f, i) => ({ id: `bay/${f.building}#${i}`, building: f.building, at: f.at, z: street.walk ?? street.z, normal: f.normal, w: f.w, kind: 'bay' as const })),
    roads: [],
    kerbs,
    pavements: b.pavements.map((p, i) => ({ id: `pave/${p.piece}#${i}`, ring: tile(p, i), top: p.top })),
    decks: b.levels.filter(l => l.ring && l.ring.length >= 3).map(l => ({ id: l.id, ring: l.ring!, z: l.height, level: levelName(l) })),
    coverage: [...kerbs.map(k => ({ id: k.id, ring: grow(k.line, COVER) })), ...b.pavements.map((p, i) => ({ id: `pave#${i}`, ring: grow(tile(p, i), COVER) }))],
    zone: 'ours',
    notes: [
      'roads: the asphalt meshes are decks under the pavements, not carriageway extents; kerbs come from the kerb stones',
      ...b.levels.filter(l => l.id === 'old/water').map(l => `level ${l.id} (${l.height}) is water, not a walk level: not compared`),
      ...(b.handoverZone ? [`handover zone ${b.handoverZone.join(',')}: not in the file, taken from our export`] : []),
    ],
  };
}

/** Each module's turn (radians in [0, π/2)): from its Unity yaw (clockwise from +Z seen from above, so a plan turn of -yaw) when the file has one, else from
 *  its neighbours (tileTurns). */
export function pavementTurns(ps: { at: V; size: number; yaw?: number }[]): number[] {
  const q = Math.PI / 2, near = ps.some(p => typeof p.yaw !== 'number') ? tileTurns(ps) : [];
  return ps.map((p, i) => { if (typeof p.yaw !== 'number') return near[i]!; const t = (((-p.yaw * Math.PI) / 180) % q + q) % q; return t > q - 1e-3 ? 0 : t; });
}

/** Each module's turn (radians in [0, π/2)): the direction to a neighbour 2 m away (±5 cm), folded to a quarter turn; 0 when it has none. */
export function tileTurns(ps: { at: V; size: number }[]): number[] {
  const cell = 4, g = new Map<string, number[]>(), key = (x: number, y: number) => `${Math.floor(x / cell)},${Math.floor(y / cell)}`;
  ps.forEach((p, i) => (g.get(key(p.at[0], p.at[1])) ?? g.set(key(p.at[0], p.at[1]), []).get(key(p.at[0], p.at[1]))!).push(i));
  const q = Math.PI / 2;
  return ps.map((p, i) => {
    const cx = Math.floor(p.at[0] / cell), cy = Math.floor(p.at[1] / cell);
    for (let a = cx - 1; a <= cx + 1; a++) for (let c = cy - 1; c <= cy + 1; c++) for (const j of g.get(`${a},${c}`) ?? []) {
      if (j === i) continue;
      const dx = ps[j]!.at[0] - p.at[0], dy = ps[j]!.at[1] - p.at[1];
      if (Math.abs(Math.hypot(dx, dy) - p.size) <= 0.05) { const t = ((Math.atan2(dy, dx) % q) + q) % q; return t > q - 1e-3 ? 0 : t; }
    }
    return 0;
  });
}

/** Kerb pieces joined end to end into lines: an end within 2 cm of another piece's end continues it (only where exactly two ends meet). */
export function joinLines<T extends { id: string; line: V[]; top: number }>(xs: T[]): T[] {
  const k = (p: V) => `${Math.round(p[0] * 50)},${Math.round(p[1] * 50)}`, ends = new Map<string, number[]>();
  xs.forEach((x, i) => { for (const p of [x.line[0]!, x.line[x.line.length - 1]!]) (ends.get(k(p)) ?? ends.set(k(p), []).get(k(p))!).push(i); });
  const other = (i: number, p: V) => { const e = ends.get(k(p)); return e && e.length === 2 ? e.find(j => j !== i) : undefined; };
  const orient = (line: V[], from: V) => (k(line[0]!) === k(from) ? line : [...line].reverse());
  const used = new Set<number>(), out: T[] = [];
  for (let s = 0; s < xs.length; s++) {
    if (used.has(s)) continue;
    // walk back from piece s to a free end (or round a closed loop to s again), then lay the line forward from there
    let i = s, back = xs[s]!.line[0]!;
    for (let n = 0; n < xs.length; n++) { const j = other(i, back); if (j === undefined || j === s || used.has(j)) break; const l = orient(xs[j]!.line, back); back = l[l.length - 1]!; i = j; }
    const first = orient(xs[i]!.line, back), chain: V[] = [...first];
    used.add(i);
    let tail = chain[chain.length - 1]!, cur = i;
    for (;;) { const j = other(cur, tail); if (j === undefined || used.has(j)) break; const l = orient(xs[j]!.line, tail); chain.push(...l.slice(1)); tail = l[l.length - 1]!; used.add(j); cur = j; }
    out.push({ ...xs[i]!, line: chain });
  }
  return out;
}

/** Either format, as suborned.oldbuilt/1. */
export const readBuilt = (raw: { schema: string }): OldBuilt => (raw.schema === 'latch.oldbuilt/1' ? fromLatch(raw as LatchBuilt) : (raw as OldBuilt));
