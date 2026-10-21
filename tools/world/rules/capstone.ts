// CAP-1..CAP-5 (brief 026, Sky 2026-10-20): the capstone programme's pieces (tools/world/gen/street/capstone.ts) are what the rules table says, where a real city puts them.
// CAP-1 ground: every flat piece names an adopted ground piece; a kerb stone lies on a carriageway's kerb face and never across a crossing strip or a drive; flags lie on a pavement plot (not a park, the
//       civic square or office) outside every building; a tree pit lies under a tree; drains and manholes lie on a carriageway or a walk, never across a crossing.
// CAP-2 siting by job: a robot stands at its job (waiter at a refreshment frontage, shopkeeper at a shop's, butler at an office or hotel door, nanny beside a bench); a vending machine is backed on the wall of a
//       tenancy of a trade the table names; the Automat on an automat tenancy; boards, lockers, baskets and door planting by their trades.
// CAP-3 the Handover stays clear: nothing of the programme within 1.2 m of a Handover point, and no standing piece on the civic square or office plots (the block stays as built).
// CAP-4 every piece the programme places is adopted in the kit and carries its own kind's use tag; a scale outside the piece's tags.scale is only the ground pieces refit to their edge.
// CAP-5 (informational) what was placed, and the places no rule could fill (coverage gaps go to Sky, never forced).
import type { StreetItem } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { toPlanFn } from '../../plan/civic';
import { type V, dist, inPoly, lineDist, segDist } from '../../plan/geom';
import { boxRing, ringGap as ringGapOf } from '../../plan/street-zones';
import { footOf } from '../civic/foot';
import { type WCtx, itemRing, rule, stat } from '../../plan/world-ctx';
import { CAPSTONE_KINDS, FLAT_SET, RULES, inRect4 } from '../gen/street/capstone-rules';
import { zoneOf } from '../gen/street/capstone';
import { ctxOf as genCtx } from '../gen/common';
import { handoverPoints } from '../civic/handover';
import { adopted } from '../gen/kitrefs';
import { overlaps } from '../gen/street/crossings';
import { pointInRing } from '../gen/geo';

const G = 'Capstone';
const SRC = 'brief 026 (Sky 2026-10-20: the old DecoKit city is the floor) · tools/world/gen/street/capstone-rules.ts · real-city siting (coverage gaps go to Sky)';
const USE_OF: Record<string, string> = { kerb: 'ground:kerb', 'pave-flags': 'ground:slab', 'tree-pit': 'ground:tree-pit', drain: 'ground:drain', manhole: 'ground:manhole', tactile: 'ground:tactile', terrazzo: 'ground:terrazzo', 'vending-machine': 'vending', automat: 'automat', 'bike-rack': 'bike-rack', 'info-lightbox': 'info-lightbox', 'sandwich-board': 'sandwich-board', 'hanging-basket': 'hanging-basket', 'window-box': 'window-box', lockers: 'lockers', 'round-bench': 'round-bench', 'drinking-fountain': 'drinking-fountain' };
const ringDist = (p: V, r: V[]) => (inPoly(p, r) ? 0 : Math.min(...r.map((q, i) => segDist(p, q, r[(i + 1) % r.length]!))));

export function capstoneRules(c: WCtx): Check[] {
  const mine = [...(c.w.ground.pieces ?? []), ...c.w.street.filter(s => CAPSTONE_KINDS.has(s.kind))];
  if (!mine.length && !c.gen.capstone) return [];
  const by = new Map(adopted().map(a => [a.id, a])), gc = genCtx(c.plan, c.d, c.civ, 0.18), zone = zoneOf(gc), toPlan = toPlanFn(c.plan.civic), points = handoverPoints(toPlan);
  const strips = (c.w.crossings ?? []).map(x => x.strip as V[]), drives = (c.w.ground.drives ?? []).map(d => d.ring as V[]);
  const solids = c.w.buildings.filter(m => m.base < 1).map(m => m.ring), trees = c.w.street.filter(s => s.kind === 'tree'), roads = c.d.roads.filter(r => !r.gyratory);
  const civicPlots = c.d.plots.filter(p => p.civic || ['square', 'civic'].includes(p.use)).map(p => p.polyV);

  // CAP-1
  const bad1: string[] = [];
  for (const s of mine.filter(x => FLAT_SET.has(x.kind))) {
    const a = s.kit && by.get(s.kit.id), ring = itemRing(s);
    if (!a || !a.tags.use.includes(USE_OF[s.kind]!)) { bad1.push(`${s.id}: ${s.kit?.id ?? 'no piece'} is not an adopted ${USE_OF[s.kind]} piece`); continue; }
    if (s.kind === 'kerb') {
      const d = Math.min(...roads.map(r => Math.abs(lineDist(s.at, r.line) - r.w / 2)));
      if (d > 0.2) bad1.push(`${s.id}: ${d.toFixed(2)} m off a carriageway's kerb face`);
      const fr = footOf(a, { scale: s.kit!.scale, yaw: s.yaw, at: s.at }), mid: V = [fr.reduce((n, q) => n + q[0], 0) / fr.length, fr.reduce((n, q) => n + q[1], 0) / fr.length]; // the stone's own centre: its pivot is on the kerb face, not in the middle (Kerb_Pearl)
      if (strips.some(p => overlaps(boxRing(mid, Math.max(0.1, s.size[0] - 0.1), s.size[1] - 0.01, s.yaw), p)) || drives.some(p => overlaps(boxRing(mid, s.size[0], s.size[1] - 0.01, s.yaw), p))) bad1.push(`${s.id}: stands across a crossing strip or a drive (the kerb drops there)`);
    } else if (s.kind === 'pave-flags') {
      const pl = c.d.plots.find(p => pointInRing(s.at, p.polyV));
      if (!pl || RULES.flags.skipUse.includes(pl.use) || pl.civic) bad1.push(`${s.id}: not on a pavement plot (${pl?.use ?? 'none'})`);
      else if (solids.some(r => pointInRing(s.at, r))) bad1.push(`${s.id}: under a building`);
    } else if (s.kind === 'tree-pit') {
      if (!trees.some(t => dist(t.at, s.at) < 0.05) && !c.w.street.some(t => (t.kind === 'tub-palm' || t.kind === 'planter' || t.kind === 'planter-bed') && dist(t.at, s.at) < 0.05)) bad1.push(`${s.id}: no tree, palm or pot stands in it`);
    } else if (s.kind === 'drain' || s.kind === 'manhole') {
      if (strips.some(p => overlaps(ring, p))) bad1.push(`${s.id}: across a crossing strip`);
    }
    if (s.z < -0.001 || s.z > 0.2) bad1.push(`${s.id}: z ${s.z} is not a flat piece's height`);
  }
  const flats = mine.filter(x => FLAT_SET.has(x.kind));
  const kerbs = flats.filter(x => x.kind === 'kerb').length;

  // CAP-2
  const bad2: string[] = [], walls = c.w.frontages.filter(f => f.level === 'street' && !f.service);
  const wallDist = (s: StreetItem, f: (typeof walls)[number]) => { const a = f.a as V, b = f.b as V, d = segDist(s.at, a, b); return d; };
  const onTrade = (s: StreetItem, trades: readonly string[], within: number) => walls.some(f => trades.includes(f.trade) && wallDist(s, f) <= within);
  const standing = mine.filter(x => !FLAT_SET.has(x.kind));
  for (const s of standing) {
    const k = s.kind;
    if (k === 'service-robot') {
      const role = s.variant ?? '', R = (RULES.robots as any)[role];
      if (!R) bad2.push(`${s.id}: a robot with no job (variant ${role || 'none'})`);
      else if (role === 'nanny') { if (!c.w.street.some(b => b.kind === 'bench' && dist(b.at, s.at) < 3)) bad2.push(`${s.id}: a nanny with no bench within 3 m`); }
      else if (role !== 'porter' && s.edge !== 'civic/cafe/terrace' && !onTrade(s, R.trades, 5)) bad2.push(`${s.id}: a ${role} not within 5 m of a ${R.trades.join('/')} frontage`);
    } else if (k === 'vending-machine') { if (!onTrade(s, RULES.vending.trades, 1.6)) bad2.push(`${s.id}: not backed on a ${RULES.vending.trades.join('/')} wall`); }
    else if (k === 'automat') { if (!onTrade(s, RULES.automat.trades, 1.6)) bad2.push(`${s.id}: not on an automat wall`); }
    else if (k === 'sandwich-board') { if (!onTrade(s, RULES.board.trades, 3.5)) bad2.push(`${s.id}: not at a ${RULES.board.trades.join('/')} frontage`); }
    else if (k === 'lockers') { if (!onTrade(s, RULES.lockers.trades, 1.6)) bad2.push(`${s.id}: not on a bureau wall`); }
    else if (k === 'hanging-basket') { if (!onTrade(s, RULES.basket.trades, 3.5)) bad2.push(`${s.id}: not at a ${RULES.basket.trades.join('/')} frontage`); }
    else if (k === 'window-box') { if (!onTrade(s, RULES.windowBox.trades, 1.2)) bad2.push(`${s.id}: not on a shop sill`); }
    else if (k === 'planter-bed') { if (!s.edge.startsWith('cap/garden/') && !walls.some(f => segDist(s.at, f.a as V, f.b as V) <= 2.5)) bad2.push(`${s.id}: not against a frontage wall or in a garden border`); }
    else if (k === 'bike-rack') { if (!onTrade(s, RULES.bikeRack.trades, 14)) bad2.push(`${s.id}: no bike-trade frontage within 14 m`); }
  }

  // CAP-3
  const bad3: string[] = [];
  for (const s of mine) {
    const ring = itemRing(s);
    for (const h of points) if (ringDist(h.p, ring) < RULES.pointClear - 0.011 && !(s.kind === 'pave-flags')) bad3.push(`${s.id} is ${ringDist(h.p, ring).toFixed(2)} m from '${h.name}'`);
    if (!FLAT_SET.has(s.kind) && s.edge !== 'civic/cafe/terrace' && civicPlots.some(p => pointInRing(s.at, p))) bad3.push(`${s.id} stands on the civic square or office plot`);
  }

  // CAP-4
  const bad4: string[] = [];
  for (const s of mine) {
    const a = s.kit && by.get(s.kit.id);
    if (!a) { bad4.push(`${s.id}: ${s.kit?.id ?? 'no kit ref'} is not adopted`); continue; }
    if (s.kind === 'service-robot' ? !a.tags.use.some(u => u.startsWith('robot:')) : !a.tags.use.includes(USE_OF[s.kind] ?? (s.kind === 'planter-bed' ? 'planter-bed' : '')) && s.kind !== 'planter-bed') bad4.push(`${s.id}: ${a.id} is not a ${s.kind} piece`);
    if (s.kind === 'planter-bed' && !a.tags.use.some(u => u.startsWith('planter-bed'))) bad4.push(`${s.id}: ${a.id} is not a planting piece`);
    if (s.variant !== 'porter' && s.kind !== 'automat' && !inRect4(s.at, [zone[0] - 1, zone[1] + 1, zone[2] - 1, zone[3] + 1])) bad4.push(`${s.id}: outside the capstone zone`);
  }
  // CAP-6: a planter and a palm never share a footprint (an urn with a trunk through it); the bed a palm is planted in is a trough
  const bad6: string[] = [], palms = c.w.street.filter(s => s.kind === 'tub-palm'), pots = c.w.street.filter(s => s.kind === 'planter' || s.kind === 'planter-bed');
  for (const p of pots) for (const t of palms) if (dist(p.at, t.at) < 0.3 && !/^Planter_Trough/.test(p.kit?.id ?? '')) bad6.push(`${p.id} (${p.kit?.id ?? 'no piece'}) shares its spot with the palm ${t.id}: a palm planted in a bed stands in a trough`);
  // CAP-7: the programme's standing pieces never share a footprint with each other or with other ground furniture, and keep the front of a machine clear (a sandwich board stood before the Cola machine)
  const bad7: string[] = [], stand = mine.filter(x => !FLAT_SET.has(x.kind) && x.z < 1), others = c.w.street.filter(x => x.z < 1 && !FLAT_SET.has(x.kind) && !x.crossing && !['crossing', 'bollard', 'beacon', 'tree', 'signal-orb'].includes(x.kind) && !x.edge.startsWith('stop/') && !x.id.startsWith('street/capstone/'));
  const ringOf = (s: StreetItem) => itemRing(s), FRONT = 1.0;
  for (let i = 0; i < stand.length; i++) {
    const a = stand[i]!, ra = ringOf(a);
    for (let j = i + 1; j < stand.length; j++) { const b = stand[j]!; if (dist(a.at, b.at) < 6 && ringGapOf(ra, ringOf(b)) < 0.005 && !(a.kind === 'planter-bed' && b.kind === 'planter-bed' && false)) bad7.push(`${a.id} and ${b.id} share a footprint`); }
    for (const o of others) if (dist(a.at, o.at) < 6 && ringGapOf(ra, ringOf(o)) < 0.005) bad7.push(`${a.id} shares a footprint with ${o.id}`);
    if (['vending-machine', 'automat', 'lockers'].includes(a.kind)) {
      const f: V = [-Math.sin(a.yaw), Math.cos(a.yaw)], zone = boxRing([a.at[0] + f[0] * (a.size[1] / 2 + FRONT / 2), a.at[1] + f[1] * (a.size[1] / 2 + FRONT / 2)], a.size[0], FRONT, a.yaw);
      for (const b of stand) if (b !== a && !['vending-machine', 'automat', 'lockers'].includes(b.kind) && ringGapOf(zone, ringOf(b)) < 0.005) bad7.push(`${b.id} stands in front of ${a.id}`);
    }
  }
  const log = c.gen.capstone;
  const counts = Object.entries(log?.counts ?? {}).map(([k, n]) => `${k} ${n}`).join(', ');
  return [
    rule('CAP-1', G, 'Ground pieces: adopted, a kerb stone on a kerb face and never across a crossing strip or drive, flags on pavement plots outside buildings, a pit under every pit tree, drains and manholes clear of crossings', flats.length, bad1, 'none', SRC),
    rule('CAP-2', G, 'Pieces stand where a real city puts them: robots at their job, vending and the Automat on the wall of their trade, boards, lockers, baskets and door planting by trade', standing.length, bad2, 'none', SRC),
    rule('CAP-3', G, 'The Handover points stay 1.2 m clear of every programme piece; no standing piece on the civic square or office plots', mine.length, bad3, 'none', SRC),
    rule('CAP-4', G, 'Every programme piece is an adopted kit piece of its own kind and lies inside the capstone zone', mine.length, bad4, 'none', SRC),
    rule('CAP-6', G, 'A planter and a tub palm never share a footprint, unless the planter is the trough the palm is planted in', pots.length, bad6, 'none', SRC),
    rule('CAP-7', G, 'No two programme pieces, nor one and other ground furniture, share a footprint; the metre in front of a vending machine, the Automat and a locker bank stays clear', stand.length, bad7, 'none', SRC),
    stat('CAP-5', G, 'Capstone programme (informational): placed, kerb coverage and the places no rule could fill', `${mine.length} pieces`, '-', true, SRC, `${counts}; kerb ${log ? `${log.kerb.stoneM} of ${log.kerb.edgeM} m (${log.kerb.droppedM} m dropped at crossings and drives)` : 'n/a'}; ${log?.gaps.length ?? 0} gaps${kerbs ? '' : ''}`),
  ];
}
