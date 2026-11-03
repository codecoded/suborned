// Plan signatures (brief 007 A): SpaceHQ stands on `signatures[].ground`; no plot building may (SG-1). The piece is the regenerated variant when adopted, else the
// base kit piece with the variant listed as pending. The kit's front (+Z) faces the plan's stair side: yaw π/2 turns +X to +Y so the front faces −x.
import type { SignatureOut } from '../../../contracts/world/world.schema';
import type { Rect } from './geo';
import { type Ctx, r3, r4 } from './common';
import { adoptedBy } from './kitrefs';

/** Rects no plot building may stand on: every plan signature's ground. */
export const signatureGrounds = (ctx: Ctx): Rect[] => (ctx.plan.signatures ?? []).map(s => s.ground as Rect);

/** Where a signature stands and which way its piece faces, until the plan carries numbers (TODO Datum: signatures[].base / yawDeg numeric). base: the pavement, or the
 *  water surface for a moored ship (pivot at the waterline). yaw: the kit's front (+Z) faces yaw + 90°; SpaceHQ's front (its grand stair) faces −x (yaw π/2);
 *  the OceanLiner's bow is its front (Blender −Y = kit +Z, tools/decokit/liner.py), and the plan turns it to +y: yaw 0, so its port side faces −x, the berth. */
const STAND: Record<string, { base: 'pavement' | 'water'; yaw: number }> = { OceanLiner: { base: 'water', yaw: 0 } };
export const WATER_LEVEL = -1; // build.ts PROV.water (TODO standards.harbour)

export function generateSignatures(ctx: Ctx): SignatureOut[] {
  return (ctx.plan.signatures ?? []).map((s): SignatureOut => {
    const variant = adoptedBy(s.regen?.variant), a = variant ?? adoptedBy(s.kit), st = STAND[s.kit ?? ''] ?? { base: 'pavement', yaw: Math.PI / 2 };
    const base = st.base === 'water' ? WATER_LEVEL : ctx.pav, ground = s.ground as Rect;
    if (!a) return { id: `signature/${s.id}`, kind: 'signature', source: 'procedural', ground, base, tip: base + (s.height ?? 0), why: `no adopted kit piece ${s.kit ?? ''}` };
    // Brief 075 remap step 2 (the SpaceHQ switch): `scale` (uniform, the old builders' Put scale: SpaceHQ × 1.35) and, for the base piece the old builders place, `oldYaw`
    // (their Unity yaw in degrees; KitPlacer turns Unity by −yaw, so the kit yaw is −oldYaw). The old SpaceHQ's grand stair is on its kit −Z, so the plan's yawDeg (the stair's
    // heading) is not the kit yaw for it; a regenerated variant keeps STAND's yaw (its front +Z is its stair).
    const sc = (s as { scale?: number }).scale ?? 1, oldYaw = (s as { oldYaw?: number }).oldYaw, yaw = !variant && typeof oldYaw === 'number' ? -(oldYaw * Math.PI) / 180 : st.yaw;
    return {
      id: `signature/${s.id}`, kind: 'signature', source: 'kit', ground, base, tip: r3(base + a.size[1] * sc), kit: { id: a.id, scale: [sc, sc, sc], yaw: r4(yaw) + 0, at: [r3(s.c[0]), r3(s.c[1])] },
      ...(variant || !s.regen?.variant ? {} : { pending: s.regen?.variant ?? '', why: `${s.regen?.variant ?? 'variant'} not in the kit yet: the base piece stands (footprint ${a.size[0]} × ${a.size[2]} m vs ground ${r3(ground[1] - ground[0])} × ${r3(ground[3] - ground[2])} m)` }),
    };
  });
}

/** The regenerated kit piece that dresses a tower site (`<regen.from>@<site>`, adopted), or undefined: the site then keeps its drawn stand-in tower. masses.ts (rule T-K1:
 *  a site this returns a piece for exports its stand-in tower masses `hidden`, data only, and the every-face pass dresses none of their faces) and generateStructures read the same. */
export const towerKit = (s: { id: string; regen?: { from?: string } }) => adoptedBy(s.regen?.from ? `${s.regen.from}@${s.id}` : undefined);

/** Kit pieces the plan names for its tall and threaded structures (brief 007): tower sites (the regenerated `<from>@<site>` piece, pivot on the podium deck), the SkyBridge between the
 *  avenue gate towers, the SkyRail stations that name a kit piece (pivot on the line at the station; the pieces run along local Z, so yaw π/2 for an east-west trunk) and the Parade gate block.
 *  A piece the kit has not adopted leaves the procedural mass and is listed `pending`. */
export function generateStructures(ctx: Ctx): SignatureOut[] {
  const out: SignatureOut[] = [], B = ctx.B;
  const podTop = r3(ctx.pav + B.plinth + B.groundStorey + (B.towers.podium - 1) * B.upperStorey), at = (r: Rect): [number, number] => [r3((r[0] + r[1]) / 2), r3((r[2] + r[3]) / 2)];
  for (const s of B.towers.sites as { id: string; rect: Rect; regen?: { from?: string } }[]) {
    const id = s.regen?.from ? `${s.regen.from}@${s.id}` : '', a = towerKit(s);
    if (!a) { out.push({ id: `tower/${s.id}`, kind: 'tower', source: 'procedural', base: podTop, tip: 0, pending: id, why: `no adopted piece ${id}` }); continue; }
    out.push({ id: `tower/${s.id}`, kind: 'tower', source: 'kit', base: podTop, tip: r3(podTop + a.size[1]), kit: { id: a.id, scale: [1, 1, 1], yaw: 0, at: at(s.rect) } });
  }
  for (const b of (B.towers.skyBridges ?? []) as { id: string; kit: string; axis: string; floor: number }[]) {
    const a = adoptedBy(b.kit), c = /\(([-\d.]+),\s*([-\d.]+)\)/.exec(b.axis); // "x, centre (0, 190) over …"
    out.push(a && c ? { id: `skybridge/${b.id}`, kind: 'skybridge', source: 'kit', base: b.floor, tip: r3(b.floor + a.size[1]), kit: { id: a.id, scale: [1, 1, 1], yaw: 0, at: [Number(c[1]), Number(c[2])] } } : { id: `skybridge/${b.id}`, kind: 'skybridge', source: 'procedural', base: b.floor, tip: b.floor, pending: b.kit });
  }
  for (const l of ctx.plan.skyrail) for (const st of (l.stations ?? []) as { id: string; path?: string; at: [number, number]; kit?: string; in?: string }[]) {
    if (!st.kit) continue;
    const decks = !!st.in && st.in.startsWith('signatures/') || st.id === 'skyport', a = adoptedBy(decks ? 'SkyRailStation_Decks' : st.kit) ?? adoptedBy(st.kit);
    if (!a) continue;
    out.push({ id: `station/${l.id}/${st.id}`, kind: 'station', source: 'kit', base: 0, tip: r3(a.size[1]), kit: { id: a.id, scale: [1, 1, 1], yaw: stationYaw(ctx, l.id, st), at: [r3(st.at[0]), r3(st.at[1])] } });
  }
  const gate = (ctx.plan.megastructures ?? []).flatMap(m => (m as { portals?: { id: string; rect: Rect }[] }).portals ?? []).find(p => p.id === 'parade-gate'), g = adoptedBy('ParadeGate_Block');
  if (gate && g) out.push({ id: 'portal/parade-gate', kind: 'portal', source: 'kit', base: 0, tip: r3(g.size[1]), kit: { id: g.id, scale: [1, 1, 1], yaw: 0, at: [276, 0] } });
  return out;
}

/** A station piece's yaw (brief 075 remap step 2, Datum's find: every station stood at y 0, east–west): the piece runs along its kit Z, so kit +Z lies on the line's
 *  direction at the station (the nearest sample of its path, else of any path of its line), folded into [0, π) since the platform reads the same both ways:
 *  an east–west trunk gives π/2 (as before), a north–south leg 0. */
export function stationYaw(ctx: Pick<Ctx, 'd'>, line: string, st: { path?: string; at: [number, number] }): number {
  const paths = ctx.d.sky.filter(p => p.line === line), own = paths.filter(p => p.id === st.path), pool = (own.length ? own : paths).flatMap(p => p.samples);
  let best = Infinity, dir: [number, number] = [1, 0];
  for (const q of pool) { const dd = (q.p[0] - st.at[0]) ** 2 + (q.p[1] - st.at[1]) ** 2; if (dd < best) { best = dd; dir = q.dir as [number, number]; } }
  let y = Math.atan2(dir[1], dir[0]) - Math.PI / 2;
  y = ((y % Math.PI) + Math.PI) % Math.PI;
  if (Math.PI - y < 1e-6) y = 0;
  return r4(y) + 0;
}
