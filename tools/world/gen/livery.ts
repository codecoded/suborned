// Landmark livery (brief 075-C step 2; the old reel's DecoKitColourways "livery" layer, ~/Projects/the-suborned/game/Assets/TheSuborned/Editor/DecoKit/DecoKitColourways.cs).
// The kit's landmark prefabs stay neutral (ivory lattices, ivory hulls, crown gold); the export names, per placed landmark piece, which material slots take which
// colour, as material keys `<slot>__<colour>` in the style of 074's colourways (`DK_Limestone_Tex__cream`), so Latch swaps them per instance and Vela makes one
// tinted material per key. The rule, from the old reel:
//   - every placed piece of a livery category (Landmarks, Skyport, Sky: the old LiveryCats) paints its lattice slot DK_Steel_Ivory (painted lattice steel,
//     tools/decokit/landmarks.py and skyport_parts.py) vermilion: the Skyport tower's bands and lattices, the gantries, the sphere's legs and ring girder;
//   - signature pieces add their own paint over that: SphereStation's hull DK_Hull_Ivory brick red (old: "deep brick red against the teal sky"), SpaceHQ_Portal's
//     ribs, beads, orbits and crown DK_Crown_Gold the showpiece gold (old: SpaceHQ DK_Gold #F2C14E, metallic 1, smoothness 0.45; spacehq.py draws pal.Gold as
//     DK_Crown_Gold). A regenerated piece (`<base>@<site>`) takes its base piece's paint.
// Deterministic: the catalogue and the export decide it, no seed. Sizes and plan sites are untouched. LV-1 (rules/livery.ts) checks the export against this rule.
import type { KitAsset } from '../../../contracts/world/kit.schema';
import type { Liveries, LiveryPlaced, World } from '../../../contracts/world/world.schema';

export const LIVERY = {
  categories: ['Landmarks', 'Skyport', 'Sky'],
  lattice: { slot: 'DK_Steel_Ivory', colour: 'vermilion' },
  own: {
    SphereStation: [{ slot: 'DK_Hull_Ivory', colour: 'brick' }],
    SpaceHQ_Portal: [{ slot: 'DK_Crown_Gold', colour: 'gold' }],
  } as Record<string, { slot: string; colour: string }[]>,
  /** The old reel's colours. No metallic / smoothness = the slot keeps its own finish (enamel); gold is the old showpiece finish. */
  colours: {
    brick: { hex: '#9C2E2B' },
    vermilion: { hex: '#B8432E' },
    gold: { hex: '#F2C14E', metallic: 1, smoothness: 0.45 },
  } as Liveries['colours'],
};

/** The material key a slot takes in a colour (074's colourway form). */
export const liveryKey = (slot: string, colour: string) => `${slot}__${colour}`;
const baseOf = (id: string) => id.split('@')[0]!;

/** A piece's livery: slot -> material key, only slots the piece has; null if it takes none. */
export function liveryOf(a: Pick<KitAsset, 'id' | 'category' | 'materials'>): Record<string, string> | null {
  const out: Record<string, string> = {};
  if (LIVERY.categories.includes(a.category) && (a.materials ?? []).includes(LIVERY.lattice.slot)) out[LIVERY.lattice.slot] = liveryKey(LIVERY.lattice.slot, LIVERY.lattice.colour);
  for (const o of LIVERY.own[baseOf(a.id)] ?? []) if ((a.materials ?? []).includes(o.slot)) out[o.slot] = liveryKey(o.slot, o.colour);
  return Object.keys(out).length ? out : null;
}

/** Every kit ref in the export (an object with a string `id` naming a catalogued piece and a `scale`): host = the nearest enclosing export object's id, ref = the key path
 *  from it ('kit', 'kits[2]', 'gateway'). Plus the pieces Unity places for a megastructure by its `landmark:<name>` tag (ref 'use'). Array order: deterministic. */
export function liveriedRefs(w: Omit<World, 'counts' | 'liveries'>, catalogue: (id: string) => KitAsset | undefined, adopted: KitAsset[]): LiveryPlaced[] {
  const out: LiveryPlaced[] = [];
  const visit = (o: unknown, host: string, ref: string) => {
    if (Array.isArray(o)) { o.forEach((v, i) => visit(v, host, `${ref}[${i}]`)); return; }
    if (!o || typeof o !== 'object') return;
    const r = o as Record<string, unknown>, id = typeof r.id === 'string' ? r.id : undefined, a = id && 'scale' in r ? catalogue(id) : undefined;
    if (a) { const slots = liveryOf(a); if (slots) out.push({ host, ref, piece: a.id, slots }); return; }
    const h = id ?? host, base = id ? '' : ref;
    for (const [k, v] of Object.entries(r)) if (v && typeof v === 'object') visit(v, h, base ? `${base}.${k}` : k);
  };
  for (const [k, v] of Object.entries(w)) if (v && typeof v === 'object') visit(v, k, '');
  for (const m of w.megastructures) for (const a of adopted) if (a.tags.use.includes(`landmark:${m.id.slice('mega/'.length)}`)) { const slots = liveryOf(a); if (slots) out.push({ host: m.id, ref: 'use', piece: a.id, slots }); }
  return out;
}

/** The export's livery block: the colours and every placed landmark piece's slots. */
export function liveriesOf(w: Omit<World, 'counts' | 'liveries'>, catalogue: (id: string) => KitAsset | undefined, adopted: KitAsset[]): Liveries {
  return { colours: LIVERY.colours, placed: liveriedRefs(w, catalogue, adopted) };
}
