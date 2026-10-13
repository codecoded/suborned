// Counts per layer, tenancies by trade, and the figures that must match the checker.
import type { World } from '../../contracts/world/world.schema';
import type { Derived } from '../plan/plan';
import type { Generated } from './gen';
import { civicClearance } from './build';

export function report(w: World, gen: Generated, d: Derived): void {
  console.log(`counts ${Object.entries(w.counts).map(([k, v]) => `${k} ${v}`).join(', ')}`);
  const by = (xs: string[]) => { const m: Record<string, number> = {}; for (const x of xs) m[x] = (m[x] ?? 0) + 1; return Object.entries(m).sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0])).map(([k, v]) => `${k} ${v}`).join(', '); };
  console.log(`buildings ${gen.blds.length} (${by(gen.blds.map(b => b.kind))}); plots ${w.ground.plots.length}; roofs by programme: ${by(w.roofs.map(r => r.programme))}`);
  console.log(`tenancies ${w.frontages.length} by trade: ${by(w.frontages.map(f => f.trade))}`);
  console.log(`tenancies by level: ${by(w.frontages.map(f => f.level))}`);
  console.log(`connectors ${w.connectors.length}: ${by(w.connectors.map(c => c.kind))}; decks ${gen.decks.length}, columns ${gen.decks.reduce((n, x) => n + x.columns.length, 0)}`);
  console.log(`street items ${w.street.length}: ${by(w.street.map(s => s.kind))}`);
  if (w.crossings && gen.crossingLog) {
    const k = (kind: string) => w.crossings!.filter(c => c.kind === kind), g = gen.crossingLog;
    console.log(`crossings ${w.crossings.length}: kerb ${k('kerb').length} (${g.landings} landings found, ${g.paired} pairs standing, ${g.added} partners added, ${g.dropped.length} landings dropped), stop ${k('stop').length}, bank ${k('bank').length}; refuges ${w.crossings.reduce((n, c) => n + c.refuges.length, 0)}; zebra ${w.crossings.filter(c => c.control === 'zebra').length}`);
    for (const x of g.dropped) console.log(`  dropped ${x}`);
  }
  console.log(`megastructures ${w.megastructures.map(m => `${m.id} ${m.parts.length} parts + ${m.prims.length} prims`).join('; ')}`);
  const cp = w.megastructures.flatMap(m => m.prims).filter(p => /\/gallery-canopy\/glass$/.test(p.id));
  console.log(`gallery canopies ${cp.length} (glazed barrels: ${cp.map(p => `${p.id} len ${p.size[0]} span ${p.size[1]} rise ${p.size[2]} springing ${p.base}`).join('; ')}); ribs ${w.megastructures.flatMap(m => m.prims).filter(p => /\/gallery-canopy\/rib-/.test(p.id)).length}, piers ${w.megastructures.flatMap(m => m.prims).filter(p => /\/gallery-canopy\/pier-/.test(p.id)).length}`);
  console.log(`pylons ${w.transit.skyRail.reduce((n, s) => n + s.pylons.length, 0)} (checker ${d.pylons.length}), stops ${w.transit.stops.length / 2}, stations ${w.transit.stations.length / 2}`);
  console.log(`horizon: ${w.horizon.pieces.length} pieces; sea sectors ${w.horizon.sea.sectors.length} (${w.horizon.sea.sectors.map(f => f.id).join(', ')}); land rings ${w.horizon.land.length} (${w.horizon.land.map(f => f.id.replace('horizon/land/', '')).join(', ')}); banks ${w.horizon.banks.length}; no square sea plane`);
  { const h = w.horizon, coast = h.sea.sectors[0]!.ring.filter(p => Math.hypot(p[0] - h.centre[0], p[1] - h.centre[1]) < 4999), n = (re: RegExp) => h.patches.filter(f => re.test(f.id)).length;
    const hills = new Set(h.hills.map(m => m.id.replace(/\/tier-\d+$/, ''))).size, onHill = h.pieces.filter(p => !['planet', 'moon', 'rocket-ascent'].includes(p.ring) && p.base > 0).length;
    console.log(`horizon land: coastline vertices ${coast.length} (north ${coast.filter(p => p[1] > h.centre[1]).length}, south ${coast.filter(p => p[1] <= h.centre[1]).length}, incl. the mole); patches ${h.patches.length} (parks ${n(/^horizon\/park\//)}, tree clumps ${n(/^horizon\/trees\//)}, promenade ${n(/^horizon\/promenade\//)}); hills ${hills} (${h.hills.length} terraces); pieces on a terrace ${onHill}`); }
  const ck = w.civic.filter(m => m.kit), cpro = w.civic.filter(m => !m.kit && m.id.split('/').length === 2 && m.surface !== 'limestone');
  console.log(`civic kit pieces ${ck.length}: ${Object.entries(ck.reduce((o, m) => ({ ...o, [m.kit!.id]: (o[m.kit!.id] ?? 0) + 1 }), {} as Record<string, number>)).map(([k, v]) => `${k} ${v} x${[...new Set(ck.filter(m => m.kit!.id === k).map(m => m.kit!.scale[0]))].join('/')}`).join(', ')}`);
  console.log(`civic objects with no kit piece ${cpro.length}: ${cpro.map(m => m.id.replace('civic/', '')).join(', ')}`);
  const clr = civicClearance(w.civic);
  console.log(`civic person-scale clearance: ${clr.length ? `${clr.length} overlaps\n  ${clr.join('\n  ')}` : 'clear'}`);
  colour(w);
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

/** Grammar §2a: the accent share of every procedural street facade (lower storeys and whole), the elements, and the fabric counts. */
export function colour(w: World): void {
  const sk = w.skins.filter(k => k.share.lower !== null), xs = sk.map(k => k.share.lower!).sort((a, b) => a - b), q = (f: number) => xs[Math.min(xs.length - 1, Math.floor(f * xs.length))]!;
  const whole = sk.map(k => k.share.whole!).sort((a, b) => a - b), tg = sk.filter(k => k.share.target), inBand = tg.filter(k => k.share.lower! >= 0.2 - 1e-9 && k.share.lower! <= 0.3 + 1e-9);
  const tl = tg.map(k => k.share.lower!).sort((a, b) => a - b), tw = tg.map(k => k.share.whole!).sort((a, b) => a - b);
  console.log(`accent share (grammar §2a, lower = ground + 2 upper storeys of the procedural street walls): ${sk.length} facades measured (${w.skins.length - sk.length} all kit-faced); all: lower min ${q(0).toFixed(3)} median ${q(0.5).toFixed(3)} max ${q(1).toFixed(3)}, whole median ${whole[Math.floor(whole.length / 2)]!.toFixed(3)}`);
  console.log(`accent share, held to 20-30% (>= 12 m procedural street face, not a shed): ${tg.length} facades, in band ${inBand.length}, under ${tg.filter(k => k.share.lower! < 0.2).length}, over ${tg.filter(k => k.share.lower! > 0.3).length}; lower min ${tl[0]!.toFixed(3)} median ${tl[Math.floor(tl.length / 2)]!.toFixed(3)} max ${tl[tl.length - 1]!.toFixed(3)}; whole min ${tw[0]!.toFixed(3)} median ${tw[Math.floor(tw.length / 2)]!.toFixed(3)} max ${tw[tw.length - 1]!.toFixed(3)} (10-15%)`);
  const ids = (xs: typeof tg) => xs.map(k => `${k.building} ${k.share.lower!.toFixed(3)}`).join(', ') || 'none';
  const pf = sk.filter(k => k.family === 'porthole' || k.faces.some(f => f.bays.includes('P'))), pfT = pf.filter(k => k.share.target);
  console.log(`accent band (grammar §2a, 3a porthole panels ${w.skins[0]?.porthole.panel.w ?? 0} x ${w.skins[0]?.porthole.panel.h ?? 0} m behind every porthole; sheds exempt): street facades under 20%: ${tg.filter(k => k.share.lower! < 0.2).length} (${ids(tg.filter(k => k.share.lower! < 0.2))}); over 30%: ${tg.filter(k => k.share.lower! > 0.3).length} (${ids(tg.filter(k => k.share.lower! > 0.3))}); porthole fronts held ${pfT.length}, under 20% ${pfT.filter(k => k.share.lower! < 0.2).length}`);
  console.log('accent share per facade (building lower/whole, * = held to the band): ' + sk.map(k => `${k.building.replace(/^bld\//, '')} ${k.share.lower!.toFixed(2)}/${k.share.whole!.toFixed(2)}${k.share.target ? '*' : ''}`).join(', '));
  const el: Record<string, number> = {}; for (const k of w.skins) for (const e of k.share.elements) el[e] = (el[e] ?? 0) + 1;
  console.log(`colour elements per building: min ${Math.min(...w.skins.map(k => k.share.elements.length))}; ${Object.entries(el).map(([k, v]) => `${k} ${v}`).join(', ')}; spandrels ${w.skins.filter(k => k.spandrel).length}, accent piers ${w.skins.filter(k => k.faience.piers).length}; fins ${w.skins.reduce((n, k) => n + k.fins.length, 0)} (corner ${w.skins.reduce((n, k) => n + k.fins.filter(f => f.kind === 'corner').length, 0)}, entrance ${w.skins.reduce((n, k) => n + k.fins.filter(f => f.kind === 'entrance').length, 0)})`);
  const by = <T,>(xs: T[], f: (x: T) => string) => Object.entries(xs.reduce((o, x) => ({ ...o, [f(x)]: (o[f(x)] ?? 0) + 1 }), {} as Record<string, number>)).map(([k, v]) => `${k} ${v}`).join(', ');
  const F = w.fabric;
  console.log(`fabric: awnings ${F.awnings.length} on ${new Set(F.awnings.map(a => a.building)).size} buildings (${by(F.awnings, a => a.stripes[0])}); banners ${F.banners.length} (${by(F.banners, b => b.kind)}; ${by(F.banners, b => b.surface)}); flags ${F.flags.length} (${by(F.flags, f => f.kind)}; ${by(F.flags, f => f.surface)})`);
}
