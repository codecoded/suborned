// Road finish and paving inlay standards RF-1, RF-2 (brief 026/028; Sky 2026-10-20: the 2.5D testbed's road). Plan-side: they hold
// standards.streetscape.roads and .inlays against the plan's road classes, the crossing strip and the kit (contracts/world/kit.json).
// The world-side rule is RF-3 (tools/world/rules/roadfinish.ts: the pieces the generator lays are adopted, at their level, and stop short at the breaks); SF-23 (inlay siting) still waits for the export to carry the inlays (Splice).
//       Added 2026-10-22 (brief 028 Road, Sky "can try"): the finish keys carry the export's look `surface`; the shoulder and pinstripe clauses; and the kerb faces no plot edge runs on (roads.kerbs: circus and foot-way mouths, kerbed bays).
// RF-1  every road class has a finish key the finish names; the ribbon covers named classes only; its pieces, the lozenge and the seam are kit pieces with their
//       use tags (not rejected); the ribbon's section sits clear of the kerb face (offset ≥ section / 2); breaks stop short (> 0) at all six break kinds;
//       lozenges spaced ≥ their length + 2 × stopShort apart; seams spaced > 0 and long > 0; no paint; crossings meet by stopping, never zebra or paint,
//       and every class's crossing control is a signal.
// RF-2  the ring and band pieces are kit pieces with their use tags; the ring's slab round its largest centrepiece is ≥ minSlab (inner − maxR); spokes start
//       outside the ring (from ≥ outer); band length is the piece's length (bands are never scaled: fixed tile pitch).
// Both are opt-in, key by key: a plan without the key (every recorded run's pinned plan) gets no check, so a replay sees what it was made with.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Check } from './checks';
import type { Civic } from './civic';
import { dist, lineDist } from './geom';
import type { Derived, Plan } from './plan';

export type KitRow = { id: string; status?: string; size?: number[]; tags?: { use?: string[] } };
const root = resolve(import.meta.dir, '../..');
let cached: KitRow[] | undefined;
/** The committed kit catalogue's rows (contracts/world/kit.json `assets`). */
export const kitRows = (): KitRow[] => (cached ??= (JSON.parse(readFileSync(resolve(root, 'contracts/world/kit.json'), 'utf8')).assets ?? []) as KitRow[]);

const BREAKS = ['crossing', 'junction', 'bay', 'drive', 'platform', 'track'] as const;
const num = (x: unknown): x is number => typeof x === 'number' && Number.isFinite(x);

/** `geo`: the derived roads and the civic block, for the clauses that read the kerb faces (roads.kerbs); without it those are skipped. */
export function roadFinishChecks(plan: Plan, kit: KitRow[] = kitRows(), geo?: { d: Derived; civ: Civic }): Check[] {
  const S = plan.standards?.streetscape ?? {}, out: Check[] = [];
  const piece = (id: unknown, use: string, bad: string[], what: string) => {
    const row = typeof id === 'string' ? kit.find(r => r.id === id) : undefined;
    if (!row) bad.push(`${what}: ${JSON.stringify(id)} is not in the kit`);
    else if (row.status === 'rejected') bad.push(`${what}: ${row.id} is rejected in the kit`);
    else if (!(row.tags?.use ?? []).includes(use)) bad.push(`${what}: ${row.id} has no use tag ${use}`);
  };

  const R = S.roads;
  if (R) {
    const bad: string[] = [], classes = [...new Set(plan.roads.map(r => r.class as string))];
    const keys = R.finish?.keys ?? {}, by = R.finish?.byClass ?? {};
    for (const c of classes) if (!by[c]) bad.push(`class ${c} has no finish`); else if (!keys[by[c]]?.material) bad.push(`class ${c}: finish ${by[c]} is not a key with a material`);
    if (R.finish?.paint !== false) bad.push('finish.paint is not false: the road is never painted');
    const rb = R.ribbon ?? {};
    for (const c of rb.classes ?? []) if (!classes.includes(c)) bad.push(`ribbon class ${c} is no road's class`);
    piece(rb.pieces?.straight, 'ground:road-ribbon', bad, 'ribbon straight');
    for (const [r, id] of Object.entries(rb.pieces?.arcs ?? {})) piece(id, 'ground:road-ribbon', bad, `ribbon arc R${r}`);
    if (!num(rb.offset) || !num(rb.section) || rb.offset < rb.section / 2) bad.push(`ribbon offset ${rb.offset} < section / 2 (${num(rb.section) ? rb.section / 2 : '?'}): the brass would run under the kerb`);
    const sh = R.breaks?.stopShort;
    if (!num(sh) || sh <= 0) bad.push(`breaks.stopShort ${sh} is not > 0`);
    for (const k of BREAKS) if (!R.breaks?.at?.[k]) bad.push(`no break at ${k}`);
    const lz = R.lozenges ?? {}, sm = R.seams ?? {};
    piece(lz.piece, 'ground:road-lozenge', bad, 'lozenge'); piece(sm.piece, 'ground:road-seam', bad, 'seam');
    if (!num(lz.spacing) || !num(lz.length) || lz.spacing < lz.length + 2 * (num(sh) ? sh : 0)) bad.push(`lozenge spacing ${lz.spacing} < length ${lz.length} + 2 × stopShort`);
    if (!num(sm.spacing) || sm.spacing <= 0 || !num(sm.length) || sm.length <= 0) bad.push(`seam spacing ${sm.spacing} / length ${sm.length} not > 0`);
    if (R.crossings?.meet !== 'stop') bad.push(`crossings.meet ${JSON.stringify(R.crossings?.meet)} is not stop`);
    for (const k of ['zebra', 'paint']) if (!(R.crossings?.never ?? []).includes(k)) bad.push(`crossings.never misses ${k}`);
    const ctl = S.crossings?.strip?.control ?? {};
    for (const c of classes) if (ctl[c] !== undefined && ctl[c] !== 'signal') bad.push(`crossing control on ${c} is ${ctl[c]}, not signal`);
    // the look keys the export carries (build.ts roadSurfaces): a surface for every key a class names, one for the kerb cap, and the kerb's pieces
    for (const c of classes) { const k = keys[by[c]]; if (k && !(typeof k.surface === 'string' && k.surface)) bad.push(`class ${c}: finish ${by[c]} has no \`surface\` (the export's look key)`); }
    const fk = R.finish?.kerb; if (fk) { if (!(typeof fk.surface === 'string' && fk.surface)) bad.push('finish.kerb has no `surface` (the export\'s look key)'); for (const id of fk.pieces ?? []) piece(id, 'ground:kerb', bad, 'finish.kerb piece'); }
    // the shoulder: the pearl band beside the ribbon (brief 028 Road, Vela)
    const shd = R.shoulder, lzw = (kit.find(r => r.id === lz.piece)?.size?.[2] ?? 0.54) / 2, stop = num(sh) ? sh : 0;
    if (shd) {
      piece(shd.piece, 'ground:road-shoulder', bad, 'shoulder straight');
      for (const [r, id] of Object.entries(shd.arcs ?? {})) piece(id, 'ground:road-shoulder', bad, `shoulder arc R${r}`);
      if (!num(shd.from) || !num(shd.to) || !num(shd.chrome) || !(shd.from < shd.to) || shd.chrome <= 0) bad.push(`shoulder from ${shd.from} < to ${shd.to}, chrome ${shd.chrome} > 0 does not hold`);
      else if (num(rb.offset) && shd.from < rb.offset + (rb.section ?? 0) / 2) bad.push(`shoulder from ${shd.from} starts under the ribbon (lens ${rb.offset} + ${(rb.section ?? 0) / 2})`);
      else {
        // the narrowest carriageway half: its middle line (a median's or a reservation's two carriageways each take their own) must stay clear of the band and the seams start at its outer chrome
        const half = Math.min(...plan.roads.filter(r => !(r as any).gyratory && (rb.classes ?? []).includes(r.class)).map(r => { const m = ((r.median ?? 0) || (r as any).reservation || 0) / 2; return m ? (r.w / 2 - m) / 2 : r.w / 2; }));
        if (half <= shd.to + shd.chrome + lzw + stop) bad.push(`shoulder to ${shd.to} + chrome ${shd.chrome} + lozenge half-width ${lzw} + stopShort ${stop} reaches the narrowest middle line (${half} m from the kerb face)`);
        else if (half - (shd.to + shd.chrome) < 0.5) bad.push(`the seam span from the shoulder's outer chrome is ${half - (shd.to + shd.chrome)} m half-width on the narrowest carriageway (< 0.5)`);
      }
    }
    const pst = R.pinstripe;
    if (pst) {
      piece(pst.piece, 'ground:pave-pinstripe', bad, 'pinstripe');
      if (!num(pst.offset) || !num(pst.width) || pst.width <= 0 || pst.offset <= 0.02 + pst.width / 2) bad.push(`pinstripe offset ${pst.offset} must exceed the kerb crown's brass (0.02) + width ${pst.width} / 2`);
    }
    // the kerb faces no plot edge runs on (roads.kerbs): each lies on its road's carriageway edge, ends on the circus ring or a plot corner, faces into the carriageway; every kerbed bay touches its road
    const kb = R.kerbs;
    if (kb && geo) {
      const roads = new Map(geo.d.roads.map(r => [r.id, r])), corners = geo.d.plots.flatMap(p => p.polyV), c0 = plan.circus.c, rOut = plan.circus.ring[1];
      const faces: { id: string; road: string; a: [number, number]; b: [number, number]; toward: [number, number] }[] = [...(kb.circusMouths?.faces ?? []), ...(kb.footMouths?.faces ?? [])];
      const seen = new Set<string>();
      for (const f of faces) {
        const rd = roads.get(f.road); if (seen.has(f.id)) bad.push(`face ${f.id} is listed twice`); seen.add(f.id);
        if (!rd) { bad.push(`face ${f.id}: no road ${f.road}`); continue; }
        for (const [n, q] of [['a', f.a], ['b', f.b]] as const) {
          const e = Math.abs(lineDist(q, rd.line) - rd.w / 2); if (e > 0.05) bad.push(`face ${f.id} ${n}: ${e.toFixed(2)} m off ${f.road}'s carriageway edge`);
          const onRing = Math.abs(dist(q, c0) - rOut) <= 0.05, onCorner = corners.some(k => dist(k, q) <= 0.05);
          if (!onRing && !onCorner) bad.push(`face ${f.id} ${n}: ends neither on the circus ring (r ${rOut}) nor at a plot corner`);
        }
        const len = dist(f.a, f.b), tl = Math.hypot(f.toward[0], f.toward[1]), mid: [number, number] = [(f.a[0] + f.b[0]) / 2, (f.a[1] + f.b[1]) / 2];
        if (len < 0.4) bad.push(`face ${f.id} is ${len.toFixed(2)} m long`);
        else if (Math.abs(tl - 1) > 0.01 || Math.abs((f.toward[0] * (f.b[0] - f.a[0]) + f.toward[1] * (f.b[1] - f.a[1])) / len) > 0.02) bad.push(`face ${f.id}: toward is not the unit normal of the face`);
        else if (!(lineDist([mid[0] + f.toward[0] * 0.5, mid[1] + f.toward[1] * 0.5], rd.line) < lineDist(mid, rd.line))) bad.push(`face ${f.id}: toward points out of the carriageway`);
      }
      for (const k of kb.bays?.kerbed ?? []) {
        const bay = geo.civ.bays.find(b => b.id === k.bay), rd = roads.get(k.road);
        if (!bay) bad.push(`kerbed bay ${k.bay} is not in the civic block`); else if (!rd) bad.push(`kerbed bay ${k.bay}: no road ${k.road}`);
        else if (!bay.poly.some((q, i) => [q, bay.poly[(i + 1) % bay.poly.length]!].every(e => Math.abs(lineDist(e, rd.line) - rd.w / 2) <= 0.1))) bad.push(`kerbed bay ${k.bay} does not touch ${k.road}'s carriageway edge`);
      }
      for (const [id, k] of Object.entries(kb.bays?.notKerbed ?? {}) as [string, unknown][]) if (k && !geo.civ.bays.some(b => b.id === id)) bad.push(`notKerbed bay ${id} is not in the civic block`);
    }
    out.push({ id: 'RF-1', group: 'Streetscape', rule: 'The road finish standard holds: every class enamel-or-named finish with its look key, no paint; ribbon, shoulder, pinstripe, lozenge and seam are kit pieces; the ribbon clears the kerb, the band clears the middle line and everything stops short at every break; the kerb faces no plot edge runs on lie on their road\'s edge and face into it; crossings keep their own light (signal, never zebra)', value: `${classes.length} classes, ${(rb.classes ?? []).length} ribboned, stop ${sh} m`, limit: 'all', ok: !bad.length, detail: bad.join('; ') || undefined, source: 'streetscape.roads · crossings.strip.control · kit.json · brief 026/028 (Road, Sky 2026-10-22)' });
  }

  const I = S.inlays;
  if (I) {
    const bad: string[] = [], ring = I.ring ?? {}, band = I.bands ?? {}, cp = ring.centrepiece ?? {};
    piece(ring.piece, 'inlay:tile-ring', bad, 'ring'); piece(band.piece, 'inlay:tile-band', bad, 'band');
    if (!num(ring.inner) || !num(ring.outer) || ring.outer <= ring.inner) bad.push(`ring ${ring.inner}–${ring.outer} is not a band`);
    if (!num(cp.maxR) || !num(cp.minSlab) || ring.inner - cp.maxR < cp.minSlab - 1e-9) bad.push(`ring inner ${ring.inner} − centrepiece maxR ${cp.maxR} < minSlab ${cp.minSlab}`);
    if (!num(band.spokes?.from) || band.spokes.from < ring.outer) bad.push(`spokes from ${band.spokes?.from} start inside the ring (outer ${ring.outer})`);
    const row = kit.find(r => r.id === band.piece), len = row?.size?.[0];
    if (num(len) && Math.abs(len - band.length) > 0.01) bad.push(`band length ${band.length} ≠ the piece's ${len} (bands are never scaled)`);
    out.push({ id: 'RF-2', group: 'Streetscape', rule: 'The paving inlay standard holds: ring and band are kit pieces; the ring leaves its slab round the largest centrepiece; spokes start outside it; bands keep the piece\'s length', value: `ring ${ring.inner}–${ring.outer}, centrepiece ≤ ${cp.maxR}, band ${band.length} m`, limit: 'all', ok: !bad.length, detail: bad.join('; ') || undefined, source: 'streetscape.inlays · kit.json · testbed fountain-plaza.js' });
  }
  return out;
}
