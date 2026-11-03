// LV-1, landmark livery (brief 075-C step 2; the old reel's DecoKitColourways livery). The export's `liveries` is what tools/world/gen/livery.ts's rule makes from the
// export's own kit refs and the catalogue: every placed piece of a livery category paints DK_Steel_Ivory vermilion, SphereStation's hull brick red, SpaceHQ_Portal's
// DK_Crown_Gold gold; no other piece or slot is painted; every key is `<slot>__<colour>` with the colour in the table, and the table is the rule's.
import type { World } from '../../../contracts/world/world.schema';
import type { KitAsset } from '../../../contracts/world/kit.schema';
import type { Check } from '../../plan/checks';
import { type WCtx, rule } from '../../plan/world-ctx';
import { adopted, catalogued } from '../gen/kitrefs';
import { LIVERY, liveriedRefs } from '../gen/livery';

const keyOf = (p: { host: string; ref: string; piece: string }) => `${p.host} ${p.ref} ${p.piece}`;

/** The faults of a world's livery against the rule. */
export function liveryFaults(w: World, catalogue: (id: string) => KitAsset | undefined = catalogued, kit: KitAsset[] = adopted()): { faults: string[]; checked: number; placed: number } {
  const faults: string[] = [], L = w.liveries;
  if (!L) return { faults: ['the export has no liveries block'], checked: 1, placed: 0 };
  for (const [c, v] of Object.entries(LIVERY.colours)) { const h = L.colours[c]; if (!h || h.hex.toUpperCase() !== v.hex.toUpperCase() || h.metallic !== v.metallic || h.smoothness !== v.smoothness) faults.push(`colour ${c}: ${JSON.stringify(h ?? null)} is not the rule's ${JSON.stringify(v)}`); }
  for (const c of Object.keys(L.colours)) if (!(c in LIVERY.colours)) faults.push(`colour ${c} is not in the rule`);
  for (const p of L.placed) for (const [slot, k] of Object.entries(p.slots)) { const m = /^(.+)__([a-z]+)$/.exec(k); if (!m || m[1] !== slot || !L.colours[m[2]!]) faults.push(`${keyOf(p)}: ${slot} -> ${k} is not <slot>__<colour in the table>`); }
  const { liveries: _l, counts: _c, ...rest } = w, want = new Map(liveriedRefs(rest, catalogue, kit).map(p => [keyOf(p), p])), have = new Map(L.placed.map(p => [keyOf(p), p]));
  for (const [k, p] of want) { const h = have.get(k); if (!h) faults.push(`${k}: placed with no livery (wants ${JSON.stringify(p.slots)})`); else if (JSON.stringify(h.slots) !== JSON.stringify(p.slots)) faults.push(`${k}: ${JSON.stringify(h.slots)}, the rule gives ${JSON.stringify(p.slots)}`); }
  for (const k of have.keys()) if (!want.has(k)) faults.push(`${k}: livery on no placed piece the rule paints`);
  return { faults, checked: want.size + Object.keys(LIVERY.colours).length, placed: L.placed.length };
}

const SOURCE = 'brief 075-C step 2 (Aster: the old reel\'s landmark colours as colourway-style keys) · tools/world/gen/livery.ts (LIVERY) · ~/Projects/the-suborned/…/Editor/DecoKit/DecoKitColourways.cs (LiveryCats, Lattice, Vermilion, Signature) · '
  + 'failing examples (livery.test.ts): SphereStation\'s hull left unpainted; a station\'s lattice given gold; a livery on the OceanLiner';

/** LV-1: the landmark livery is the rule's, on every placed landmark piece. */
export function liveryRules(c: WCtx): Check[] {
  const r = liveryFaults(c.w);
  return [rule('LV-1', 'Colour', `Every placed landmark piece paints by the livery rule (lattice DK_Steel_Ivory vermilion on Landmarks, Skyport and Sky pieces; SphereStation hull brick; SpaceHQ_Portal and DiscTower@crown-vista crown gold); ${r.placed} pieces`, r.checked, r.faults, '0 faults', SOURCE)];
}
