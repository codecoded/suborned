// Road finish and paving inlay standards RF-1, RF-2 (brief 026/028; Sky 2026-10-20: the 2.5D testbed's road). Plan-side: they hold
// standards.streetscape.roads and .inlays against the plan's road classes, the crossing strip and the kit (contracts/world/kit.json).
// The world-side rules SF-22 (ribbon breaks) and SF-23 (inlay siting) wait for the export to carry the ribbon and inlays (Splice).
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
import type { Plan } from './plan';

export type KitRow = { id: string; status?: string; size?: number[]; tags?: { use?: string[] } };
const root = resolve(import.meta.dir, '../..');
let cached: KitRow[] | undefined;
/** The committed kit catalogue's rows (contracts/world/kit.json `assets`). */
export const kitRows = (): KitRow[] => (cached ??= (JSON.parse(readFileSync(resolve(root, 'contracts/world/kit.json'), 'utf8')).assets ?? []) as KitRow[]);

const BREAKS = ['crossing', 'junction', 'bay', 'drive', 'platform', 'track'] as const;
const num = (x: unknown): x is number => typeof x === 'number' && Number.isFinite(x);

export function roadFinishChecks(plan: Plan, kit: KitRow[] = kitRows()): Check[] {
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
    out.push({ id: 'RF-1', group: 'Streetscape', rule: 'The road finish standard holds: every class enamel-or-named finish, no paint; ribbon, lozenge and seam are kit pieces; the ribbon clears the kerb and stops short at every break; crossings keep their own light (signal, never zebra)', value: `${classes.length} classes, ${(rb.classes ?? []).length} ribboned, stop ${sh} m`, limit: 'all', ok: !bad.length, detail: bad.join('; ') || undefined, source: 'streetscape.roads · crossings.strip.control · kit.json · brief 026/028' });
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
