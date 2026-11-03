// NP-1, no placeholders (brief 075, Sky 2026-10-28: the gate towers' bright-blue procedural cylinders, landmarks whose kit piece did not fit, were "ugly blue
// buildings"). A structure the plan names a kit piece for stands as that piece or the world fails: a missing, unadopted or unfitting piece is never drawn as a
// procedural stand-in. Read from the export and the plan:
//   - landmarks: source kit with a piece (or pads); the gateway arch the plan names is placed;
//   - signatures, tower sites, sky bridges, stations and the Parade gate block: each the plan names is in the export as source kit (a station or the gate the
//     generator skipped for want of its piece is a fault, not silence). A signature standing as its base piece while its regenerated variant is pending is kit, and allowed;
//   - megastructures: a kind whose generator is a stand-in for a kit piece (STAND_IN_KINDS: the pier tower) fails unless an adopted piece is tagged landmark:<id>
//     (Unity then places that piece instead of the parts: MegaKit, StandIns);
//   - every kit ref anywhere in the export names an adopted piece (no candidate, rejected or unknown id reaches Unity);
//   - facades: every building wall face is dressed by kit (faces-cover.ts, the world:faces rule, brief 072-C).
// Allowed procedural geometry (not a placeholder): plain building masses, roofs and podiums under their kit dressing; the megastructure kinds built of decks,
// columns and masses (terraces, arcades, harbour decks); a caisson's drum; the ground, kerbs and roads; the sky track's beam ribbon; fascia signs; street items
// built from their kit.json parts (CityLayers). Tests: placeholders.test.ts, a failing example each.
import type { KitAsset } from '../../../contracts/world/kit.schema';
import type { World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import type { Plan } from '../../plan/plan';
import { type WCtx, rule } from '../../plan/world-ctx';
import { adopted, catalogued } from '../gen/kitrefs';
import { faceCoverage } from './faces-cover';

/** Megastructure kinds whose generator draws a procedural stand-in for a kit piece (mega/pier.ts: "a procedural stand-in for the regenerated kit piece"). */
export const STAND_IN_KINDS = ['pier tower'];

/** Every kit ref in the export: an object with a string `id` and a 3-number `scale`, with the path it was found at. */
export function kitRefs(w: unknown): { path: string; id: string }[] {
  const out: { path: string; id: string }[] = [];
  const walk = (x: unknown, path: string) => {
    if (Array.isArray(x)) { x.forEach((v, i) => walk(v, `${path}[${i}]`)); return; }
    if (!x || typeof x !== 'object') return;
    const o = x as Record<string, unknown>;
    if (typeof o.id === 'string' && Array.isArray(o.scale) && o.scale.length === 3) out.push({ path, id: o.id });
    for (const [k, v] of Object.entries(o)) if (v && typeof v === 'object') walk(v, `${path}.${k}`);
  };
  walk(w, 'world');
  return out;
}

export type PlaceholderReport = { faults: string[]; checked: number; pending: string[] };

/** The placeholders in a world against its plan; `kit` = the adopted catalogue, `catalogue` = every piece by id (for the face rule's sizes). */
export function placeholderFaults(plan: Plan, w: World, kit: KitAsset[] = adopted(), catalogue: (id: string) => KitAsset | undefined = catalogued): PlaceholderReport {
  const faults: string[] = [], pending: string[] = [], ok = new Set(kit.map(a => a.id));
  let checked = 0;
  const need = (cond: boolean, fault: string) => { checked++; if (!cond) faults.push(fault); };
  // landmarks
  for (const l of plan.landmarks) {
    const m = w.landmarks.find(x => x.id === `landmark/${l.id}`);
    need(!!m && m.source === 'kit' && (!!m.kit || !!m.kits?.length), m ? `landmark/${l.id}: ${m.source ?? 'no source'} stand-in for ${l.kit ?? 'its piece'} (${m.why ?? 'no kit ref'})` : `landmark/${l.id}: not in the export`);
    const gw = (l as { gateway?: { kit?: string } }).gateway;
    if (gw?.kit) need(!!m?.gateway, `landmark/${l.id}: its gateway ${gw.kit} is not placed (not adopted)`);
  }
  // signatures and named structures
  const sig = new Map(w.signatures.map(s => [s.id, s]));
  const named = (id: string, piece: string | undefined) => {
    const s = sig.get(id);
    need(!!s && s.source === 'kit' && !!s.kit, s ? `${id}: ${s.source} stand-in for ${s.pending || piece || 'its piece'} (${s.why ?? 'no adopted piece'})` : `${id}: no piece placed for ${piece ?? 'its kit'} (not adopted)`);
    if (s?.pending && s.source === 'kit') pending.push(`${id} stands as ${s.kit?.id} while ${s.pending} is pending`);
  };
  for (const s of plan.signatures ?? []) named(`signature/${s.id}`, s.kit);
  const B = plan.standards.buildings as { towers?: { sites?: { id: string; regen?: { from?: string } }[]; skyBridges?: { id: string; kit: string }[] } };
  for (const s of B.towers?.sites ?? []) named(`tower/${s.id}`, s.regen?.from ? `${s.regen.from}@${s.id}` : undefined);
  for (const b of B.towers?.skyBridges ?? []) named(`skybridge/${b.id}`, b.kit);
  for (const l of plan.skyrail) for (const st of (l.stations ?? []) as { id: string; kit?: string }[]) if (st.kit) named(`station/${l.id}/${st.id}`, st.kit);
  if ((plan.megastructures ?? []).some(m => ((m as { portals?: { id: string }[] }).portals ?? []).some(p => p.id === 'parade-gate'))) named('portal/parade-gate', 'ParadeGate_Block');
  for (const s of w.signatures) if (!sig.has(s.id) || s.source !== 'kit') { if (![...faults].some(f => f.startsWith(`${s.id}:`))) need(false, `${s.id}: ${s.source} stand-in (${s.why ?? s.pending ?? 'no adopted piece'})`); }
  // megastructures drawn as a stand-in for their piece
  // (Unity draws the adopted piece tagged landmark:<id> in its place, MegaKit StandIns, as the livery reads it: then it is kit)
  for (const m of plan.megastructures ?? []) need(!STAND_IN_KINDS.includes(m.kind) || kit.some(a => a.tags?.use?.includes(`landmark:${m.id}`)), `mega/${m.id}: a ${m.kind} with no adopted piece tagged landmark:${m.id} is drawn as a procedural stand-in (mega/pier.ts)`);
  // every kit ref is an adopted piece
  const refs = kitRefs(w), bad = refs.filter(r => !ok.has(r.id));
  checked += refs.length;
  const byId = new Map<string, string[]>(); for (const r of bad) (byId.get(r.id) ?? byId.set(r.id, []).get(r.id)!).push(r.path);
  for (const [id, paths] of byId) faults.push(`${id}: ${paths.length} ref${paths.length > 1 ? 's' : ''} to a piece that is not adopted (${catalogued(id)?.status ?? 'not in the catalogue'}), e.g. ${paths[0]}`);
  // facades: every wall face dressed
  const cov = faceCoverage(w, catalogue, () => '', () => false);
  checked += cov.faces;
  for (const g of cov.undressed) faults.push(`${g.face}: ${g.length} m of wall undressed (${g.from}–${g.to} m) at ${g.at.join(', ')}`);
  return { faults, checked, pending };
}

const SOURCE = 'brief 075 (Sky 2026-10-28: the gate towers\' procedural stand-ins were "ugly blue buildings"; no placeholder ships) · tools/world/rules/placeholders.ts · faces-cover.ts (brief 072-C) · '
  + 'failing examples (placeholders.test.ts): a landmark whose piece does not fit drawn as a cylinder; a station whose piece is not adopted; a pier tower stand-in; a facade naming a candidate piece; an undressed wall';

/** NP-1: no procedural stand-in for a kit piece. */
export function placeholderRules(c: WCtx): Check[] {
  const r = placeholderFaults(c.plan, c.w);
  const k = rule('NP-1', 'Kit', 'No placeholders: every landmark, signature, tower, sky bridge, station, gate and megastructure the plan gives a kit piece stands as that adopted piece, every kit ref is adopted and every wall face is dressed', r.checked, r.faults, '0 stand-ins', SOURCE);
  return [r.pending.length && k.ok ? { ...k, detail: `allowed (kit base piece, variant pending): ${r.pending.join('; ')}` } : k];
}
