// Counts per layer, tenancies by trade, and the figures that must match the checker.
import type { World } from '../../contracts/world/world.schema';
import type { Derived } from '../plan/plan';
import type { Generated } from './gen';

export function report(w: World, gen: Generated, d: Derived): void {
  console.log(`counts ${Object.entries(w.counts).map(([k, v]) => `${k} ${v}`).join(', ')}`);
  const by = (xs: string[]) => { const m: Record<string, number> = {}; for (const x of xs) m[x] = (m[x] ?? 0) + 1; return Object.entries(m).sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0])).map(([k, v]) => `${k} ${v}`).join(', '); };
  console.log(`buildings ${gen.blds.length} (${by(gen.blds.map(b => b.kind))}); plots ${w.ground.plots.length}; roofs by programme: ${by(w.roofs.map(r => r.programme))}`);
  console.log(`tenancies ${w.frontages.length} by trade: ${by(w.frontages.map(f => f.trade))}`);
  console.log(`tenancies by level: ${by(w.frontages.map(f => f.level))}`);
  console.log(`connectors ${w.connectors.length}: ${by(w.connectors.map(c => c.kind))}; decks ${gen.decks.length}, columns ${gen.decks.reduce((n, x) => n + x.columns.length, 0)}`);
  console.log(`street items ${w.street.length}: ${by(w.street.map(s => s.kind))}`);
  console.log(`megastructures ${w.megastructures.map(m => `${m.id} ${m.parts.length} parts + ${m.prims.length} prims`).join('; ')}`);
  const cp = w.megastructures.flatMap(m => m.prims).filter(p => /\/gallery-canopy\/glass$/.test(p.id));
  console.log(`gallery canopies ${cp.length} (glazed barrels: ${cp.map(p => `${p.id} len ${p.size[0]} span ${p.size[1]} rise ${p.size[2]} springing ${p.base}`).join('; ')}); ribs ${w.megastructures.flatMap(m => m.prims).filter(p => /\/gallery-canopy\/rib-/.test(p.id)).length}, piers ${w.megastructures.flatMap(m => m.prims).filter(p => /\/gallery-canopy\/pier-/.test(p.id)).length}`);
  console.log(`pylons ${w.transit.skyRail.reduce((n, s) => n + s.pylons.length, 0)} (checker ${d.pylons.length}), stops ${w.transit.stops.length / 2}, stations ${w.transit.stations.length / 2}`);
  console.log(`horizon: ${w.horizon.pieces.length} pieces; sea sectors ${w.horizon.sea.sectors.length} (${w.horizon.sea.sectors.map(f => f.id).join(', ')}); land rings ${w.horizon.land.length} (${w.horizon.land.map(f => f.id.replace('horizon/land/', '')).join(', ')}); banks ${w.horizon.banks.length}; no square sea plane`);
  coverage(w, gen);
}

/** Kit vs procedural per layer. Layers the kit has not reached (wave 2: facades, towers, roofs) are all procedural stand-ins. */
export function coverage(w: World, gen?: Generated): void {
  const row = (name: string, xs: { source?: string }[] | readonly object[], kit = (x: { source?: string }) => x.source === 'kit') => ({ name, kit: (xs as { source?: string }[]).filter(kit).length, total: xs.length });
  const rows = [
    row('street', w.street), row('frontages (sign kit)', w.frontages), row('landmarks', w.landmarks),
    ...(['roofs', 'civic', 'gates'] as const).map(k => row(`${k} (wave 2)`, w[k], () => false)),
    { name: 'megastructures (wave 2)', kit: 0, total: w.megastructures.length },
  ];
  if (gen) faces(w, gen);
  console.log('coverage (kit / procedural): ' + rows.map(r => `${r.name} ${r.kit}/${r.total - r.kit}`).join('; '));
  const by: Record<string, [number, number]> = {};
  for (const s of w.street) { const e = (by[s.kind] ??= [0, 0]); e[s.source === 'kit' ? 0 : 1]++; }
  console.log('street by kind (kit/procedural): ' + Object.entries(by).sort((a, b) => b[1][0] + b[1][1] - a[1][0] - a[1][1]).map(([k, [a, b]]) => `${k} ${a}/${b}`).join(', '));
  const used: Record<string, number> = {};
  for (const f of w.facades) used[f.kit.id] = (used[f.kit.id] ?? 0) + 1;
  for (const x of [...w.street.map(s => s.kit?.id), ...w.frontages.map(f => f.signKit?.id), ...w.landmarks.flatMap(l => [l.kit?.id, ...(l.kits ?? []).map(k => k.id)]), ...w.roofs.map(r => r.kit?.id), ...w.megastructures.map(m => m.gateway?.id)]) if (x) used[x] = (used[x] ?? 0) + 1;
  console.log('kit pieces used: ' + Object.entries(used).sort().map(([k, v]) => `${k} ${v}`).join(', '));
  for (const l of w.landmarks) console.log(`landmark ${l.id}: ${l.source} ${l.kit ? `${l.kit.id} x ${l.kit.scale[0]}` : l.kits ? `${l.kits.length} x ${l.kits[0]!.id} x ${l.kits[0]!.scale[0]}` : ''}`);
  for (const m of w.megastructures) if (m.gateway) console.log(`gateway ${m.id}: ${m.gateway.id} x ${m.gateway.scale[0]} at ${m.gateway.at}`);
  console.log(`roof kit pads: ${w.roofs.filter(r => r.kit).length}`);
  for (const l of w.landmarks) if (l.why) console.log(`landmark ${l.id} procedural: ${l.why}`);
}

/** Building faces = street-facing wall metres of the generated buildings. Before wave 2 every one was procedural. */
function faces(w: World, gen: Generated): void {
  let street = 0, kit = 0, kitB = 0, B = 0;
  const by: Record<string, [number, number]> = {};
  for (const b of gen.blds) {
    const m = gen.faceMetres.get(b.id); if (!m) continue;
    B++; street += m.street; kit += m.kit; if (m.kit > 0) kitB++;
    const e = (by[b.use] ??= [0, 0]); e[0] += m.kit; e[1] += m.street - m.kit;
  }
  const pct = (a: number, t: number) => (t ? ((100 * a) / t).toFixed(1) : '0') + '%';
  console.log(`building faces: ${B} plot buildings, ${street.toFixed(0)} m of street face; kit ${kit.toFixed(0)} m (${pct(kit, street)}), procedural ${(street - kit).toFixed(0)} m (${pct(street - kit, street)}); before wave 2 procedural 100%`);
  console.log(`buildings with a kit facade: ${kitB}/${B}; facade pieces ${w.facades.length}: ` + ['bay', 'end', 'corner'].map(f => `${f} ${w.facades.filter(x => x.form === f).length}`).join(', '));
  console.log('faces by use (kit / procedural m): ' + Object.entries(by).map(([k, [a, b]]) => `${k} ${a.toFixed(0)}/${b.toFixed(0)}`).join(', '));
  console.log(`buildings (wave 2): ${gen.blds.length} bodies, kit facade ${kitB}, procedural grammar ${gen.blds.length - kitB}`);
  const sf = w.skins.flatMap(k => k.faces), fam: Record<string, number> = {}, acc: Record<string, number> = {};
  for (const k of w.skins) { fam[k.family] = (fam[k.family] ?? 0) + 1; acc[k.accent] = (acc[k.accent] ?? 0) + 1; }
  const glazed = sf.filter(f => /[RPB]/.test(f.bays));
  console.log(`skins ${w.skins.length} (procedural walls by the grammar, no curtain wall): families ${Object.entries(fam).map(([k, v]) => `${k} ${v}`).join(', ')}; accents ${Object.entries(acc).map(([k, v]) => `${k} ${v}`).join(', ')}; faces ${sf.length} (${glazed.length} glazed, ${sf.length - glazed.length} stone with porthole columns ${sf.reduce((n, f) => n + f.portholes.length, 0)}); glass share max ${Math.max(...sf.map(f => f.glass)).toFixed(2)}, mean ${(glazed.reduce((a, f) => a + f.glass, 0) / Math.max(1, glazed.length)).toFixed(2)} (limit 0.55)`);
  console.log(`street corners: ${w.facades.filter(f => f.form === 'corner').length} kit corners; the rest of the procedural bodies' street corners are rounded arcs (grammar §1)`);
}
