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
  console.log(`pylons ${w.transit.skyRail.reduce((n, s) => n + s.pylons.length, 0)} (checker ${d.pylons.length}), stops ${w.transit.stops.length / 2}, stations ${w.transit.stations.length / 2}`);
  coverage(w);
}

/** Kit vs procedural per layer. Layers the kit has not reached (wave 2: facades, towers, roofs) are all procedural stand-ins. */
export function coverage(w: World): void {
  const row = (name: string, xs: { source?: string }[] | readonly object[], kit = (x: { source?: string }) => x.source === 'kit') => ({ name, kit: (xs as { source?: string }[]).filter(kit).length, total: xs.length });
  const rows = [
    row('street', w.street), row('frontages (sign kit)', w.frontages), row('landmarks', w.landmarks),
    ...(['buildings', 'roofs', 'civic', 'gates'] as const).map(k => row(`${k} (wave 2)`, w[k], () => false)),
    { name: 'megastructures (wave 2)', kit: 0, total: w.megastructures.length },
  ];
  console.log('coverage (kit / procedural): ' + rows.map(r => `${r.name} ${r.kit}/${r.total - r.kit}`).join('; '));
  const by: Record<string, [number, number]> = {};
  for (const s of w.street) { const e = (by[s.kind] ??= [0, 0]); e[s.source === 'kit' ? 0 : 1]++; }
  console.log('street by kind (kit/procedural): ' + Object.entries(by).sort((a, b) => b[1][0] + b[1][1] - a[1][0] - a[1][1]).map(([k, [a, b]]) => `${k} ${a}/${b}`).join(', '));
  const used: Record<string, number> = {};
  for (const x of [...w.street.map(s => s.kit?.id), ...w.frontages.map(f => f.signKit?.id), ...w.landmarks.flatMap(l => [l.kit?.id, ...(l.kits ?? []).map(k => k.id)]), ...w.roofs.map(r => r.kit?.id), ...w.megastructures.map(m => m.gateway?.id)]) if (x) used[x] = (used[x] ?? 0) + 1;
  console.log('kit pieces used: ' + Object.entries(used).sort().map(([k, v]) => `${k} ${v}`).join(', '));
  for (const l of w.landmarks) console.log(`landmark ${l.id}: ${l.source} ${l.kit ? `${l.kit.id} x ${l.kit.scale[0]}` : l.kits ? `${l.kits.length} x ${l.kits[0]!.id} x ${l.kits[0]!.scale[0]}` : ''}`);
  for (const m of w.megastructures) if (m.gateway) console.log(`gateway ${m.id}: ${m.gateway.id} x ${m.gateway.scale[0]} at ${m.gateway.at}`);
  console.log(`roof kit pads: ${w.roofs.filter(r => r.kit).length}`);
  for (const l of w.landmarks) if (l.why) console.log(`landmark ${l.id} procedural: ${l.why}`);
}
