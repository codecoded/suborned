// BG-1..BG-5 (brief 075, Sky 2026-10-28: "where's the rounded corners etc - why can't we just use the old buildings ?"): the plan's blocks outside the Handover zone keep the old DecoKit block
// grammar (standards.blocks, Datum), so the old frontage pieces stand at true size: runs of 6 m bays between caps (corner = the 8 m cell, end / open / join / arc = 0), 14 m bays, 28 back to back,
// corner cells at public corners, crescents of 6 m chords round Crown Circus. Opt-in: a plan without standards.blocks has no check (a pinned plan replays as it was made).
//   BG-1  the standard is whole; every built plot is laid as blocks or exempt for a reason the plan can prove (handover: reaches into the capstone zone; megastructure: a footprint covers it;
//         shed: industrial or port use); the zone equals the capstone zone (tools/world/gen/street/capstone.ts zoneOf), so it cannot be widened to exempt a plot
//   BG-2  every run: axis-aligned, its length = caps + 6 x bays exactly (0.01 m), bays >= 1, known caps; a gap only at a join or arc end (or a passage face's open ends) and under 6 m; every corner cap meets the corner cap of a
//         perpendicular run of its block at the same point (the shared 8 x 8 cell)
//   BG-3  every run's 14 m band lies inside its plot's building line and clear of every reserved mass (tower podium, megastructure, signature ground) and of the circus; a perimeter block is at least
//         28 deep, solid under 40 and open from 40; an inner block stands in its plot's perimeter court, innerClear from the ring
//   BG-4  every convex building-line corner of a laid plot where two road edges meet has a corner cell within 6 m on each axis, unless a reserved mass stands within one band (14 m) of it
//   BG-5  circus.crescents: one per laid circus plot, chords of buildings.bay on the building-line circle (midpoints on r, joints on rEnds), bays x stepDeg = the swept angle, inside the quadrant
//         (never past the plot's arc), the band clear of every reserved mass, starting on its N-S run's facade line within a bay of the run's arc end, caps open / end
// Failing examples: block-grammar.test.ts.
import type { Check } from './checks';
import type { Civic } from './civic';
import { type V, inPoly } from './geom';
import type { Derived, Plan } from './plan';
import { ctxOf } from '../world/gen/common';
import { buildingLine, kindOf, plotRing } from '../world/gen/edges';
import { reservedRects, towerSites } from '../world/gen/masses';
import { zoneOf } from '../world/gen/street/capstone';

const SRC = 'standards.blocks (Datum, brief 075) · the old DecoKit town: DecoKitFrontage.cs, DecoKitBlock.cs, DecoKitCityBlocks.cs, DecoKitCircus.cs';
const BUILT = ['residential', 'mixed', 'commercial', 'industrial', 'port'];
const ROADS = ['primary', 'district', 'access'];
const CAPS = new Set(['corner', 'end', 'open', 'join', 'arc']);
type R4 = [number, number, number, number];
type Run = { face: string; a: V; b: V; n: V; faces: string; caps: [string, string]; bays: number; gap?: [number, number] };
type Block = { id: string; plot: string; kind: 'perimeter' | 'inner'; rect: R4; depth: number; court: 'solid' | 'open'; runs: Run[]; corners: V[] };

const overlaps = (a: R4, b: R4, tol = 0.05) => Math.min(a[1], b[1]) - Math.max(a[0], b[0]) > tol && Math.min(a[3], b[3]) - Math.max(a[2], b[2]) > tol;
const near = (a: V, b: V, t = 0.01) => Math.abs(a[0] - b[0]) <= t && Math.abs(a[1] - b[1]) <= t;
/** The run's band: the run on the facade line and 14 m inward (against its normal). */
const bandOf = (r: Run, depth: number): R4 => {
  const xs = [r.a[0], r.b[0], r.a[0] - r.n[0] * depth, r.b[0] - r.n[0] * depth], ys = [r.a[1], r.b[1], r.a[1] - r.n[1] * depth, r.b[1] - r.n[1] * depth];
  return [Math.min(...xs), Math.max(...xs), Math.min(...ys), Math.max(...ys)];
};
/** A point inside the polygon or within tol of it (on its edge or corner; the building line's arc is chords). */
const inside = (p: V, poly: V[], tol = 0.05) => inPoly(p, poly) || [[tol, 0], [-tol, 0], [0, tol], [0, -tol], [tol, tol], [tol, -tol], [-tol, tol], [-tol, -tol]].some(([dx, dy]) => inPoly([p[0] + dx!, p[1] + dy!], poly));
/** A convex quad (the crescent bay's band) meets a rect with area. */
function quadHits(q: V[], r: R4): boolean {
  const inR = (p: V) => p[0] > r[0] + 0.05 && p[0] < r[1] - 0.05 && p[1] > r[2] + 0.05 && p[1] < r[3] - 0.05;
  const inQ = (p: V) => { let s = 0; for (let i = 0; i < 4; i++) { const a = q[i]!, b = q[(i + 1) % 4]!, c = (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0]); if (Math.abs(c) < 1e-9) continue; if (!s) s = Math.sign(c); else if (Math.sign(c) !== s) return false; } return true; };
  return q.some(inR) || ([[r[0], r[2]], [r[1], r[2]], [r[1], r[3]], [r[0], r[3]]] as V[]).some(inQ);
}
const list = (bad: string[]) => (bad.length ? bad.slice(0, 6).join('; ') + (bad.length > 6 ? `; … (${bad.length})` : '') : undefined);

export function blockGrammarChecks(plan: Plan, d: Derived, civ: Civic): Check[] {
  const S = (plan.standards as any)?.blocks; if (!S) return [];
  const P = plan as any, blocks: Block[] = P.blocks ?? [], crescents: any[] = P.circus?.crescents ?? [];
  const ctx = ctxOf(plan, d, civ, 0.18), reserved = reservedRects(ctx, towerSites(ctx)) as R4[];
  const line = new Map(d.plots.map(pl => [pl.id, buildingLine(ctx, plotRing(ctx, pl))]));
  const num = (x: unknown): x is number => typeof x === 'number' && Number.isFinite(x);
  const out: Check[] = [];

  // ---- BG-1: the standard, coverage and exemptions
  const b1: string[] = [];
  for (const k of ['bay', 'cell', 'band', 'backToBack', 'solidBelow', 'innerClear', 'passage', 'split']) if (!num(S[k]) || S[k] <= 0) b1.push(`standards.blocks.${k} is ${JSON.stringify(S[k])}`);
  if (num(S.bay) && S.bay !== (plan.standards as any).buildings?.bay) b1.push(`blocks.bay ${S.bay} is not buildings.bay ${(plan.standards as any).buildings?.bay}`);
  if (!S.caps || S.caps.corner !== S.cell || ['end', 'open', 'join', 'arc'].some(c => S.caps[c] !== 0)) b1.push('caps must be corner = cell, end / open / join / arc = 0');
  const zone = zoneOf(ctx) as R4;
  if (!Array.isArray(S.zone) || S.zone.some((v: number, i: number) => Math.abs(v - zone[i]!) > 0.01)) b1.push(`zone ${JSON.stringify(S.zone)} is not the capstone zone ${JSON.stringify(zone)}`);
  const laid = new Set<string>(S.plots ?? []), exempt: Record<string, string> = S.exempt ?? {};
  for (const pl of d.plots) {
    if (!BUILT.includes(pl.use) || pl.civic) { if (laid.has(pl.id)) b1.push(`${pl.id} (${pl.use}) is not a built plot`); continue; }
    const why = exempt[pl.id];
    if (laid.has(pl.id) && why) b1.push(`${pl.id} is both laid and exempt`);
    if (!laid.has(pl.id) && !why) { b1.push(`${pl.id} is neither laid as blocks nor exempt`); continue; }
    if (laid.has(pl.id) && !blocks.some(b => b.plot === pl.id)) b1.push(`${pl.id} is listed but has no blocks`);
    if (!why) continue;
    const xs = pl.polyV.map(p => p[0]), ys = pl.polyV.map(p => p[1]), bb: R4 = [Math.min(...xs), Math.max(...xs), Math.min(...ys), Math.max(...ys)];
    const kind = why.split(':')[0];
    if (kind === 'handover' ? !overlaps(bb, zone, 0) : kind === 'megastructure' ? !(plan.megastructures ?? []).some((m: any) => (m.footprint as R4[]).some(f => f[0] <= bb[0] + 0.5 && f[1] >= bb[1] - 0.5 && f[2] <= bb[2] + 0.5 && f[3] >= bb[3] - 0.5)) : kind === 'shed' ? !['industrial', 'port'].includes(pl.use) : true)
      b1.push(`${pl.id}: "${why}" does not hold (handover: reaches into the zone; megastructure: a footprint covers the plot; shed: industrial or port)`);
  }
  for (const id of Object.keys(exempt)) if (!d.plots.some(p => p.id === id)) b1.push(`exempt ${id}: no such plot`);
  for (const pl of d.plots) if (pl.civic && blocks.some(b => b.plot === pl.id)) b1.push(`${pl.id}: the civic block is never laid as blocks`);
  out.push({ id: 'BG-1', group: 'Blocks', rule: 'The old block grammar is whole (6 m bays, 8 m corner cells, 14 m bands) and every built plot outside the Handover zone is laid as blocks; an exemption is the Handover zone, a covering megastructure or a shed', value: `${laid.size} laid, ${Object.keys(exempt).length} exempt`, limit: 'all', ok: !b1.length, detail: list(b1), source: SRC });

  // ---- BG-2: runs
  const b2: string[] = [], cell = S.cell as number, bay = S.bay as number;
  let nRuns = 0, nCells = 0;
  for (const b of blocks) {
    const ids = new Set<string>();
    if (ids.has(b.id) || blocks.filter(x => x.id === b.id).length > 1) b2.push(`${b.id}: id not unique`);
    ids.add(b.id);
    for (const r of b.runs) {
      nRuns++;
      const where = `${b.id} ${r.face} (${r.a.join(',')} → ${r.b.join(',')})`;
      const alongX = Math.abs(r.a[1] - r.b[1]) < 1e-6, alongY = Math.abs(r.a[0] - r.b[0]) < 1e-6;
      if (!alongX && !alongY) { b2.push(`${where}: not axis-aligned`); continue; }
      if ((alongX ? r.b[0] - r.a[0] : r.b[1] - r.a[1]) <= 0) b2.push(`${where}: a → b must run in +x or +y`);
      if (Math.abs(r.n[0]) + Math.abs(r.n[1]) !== 1 || (alongX ? r.n[0] !== 0 : r.n[1] !== 0)) b2.push(`${where}: normal ${JSON.stringify(r.n)} is not square to the run`);
      if (!r.caps || r.caps.length !== 2 || r.caps.some(c => !CAPS.has(c))) { b2.push(`${where}: caps ${JSON.stringify(r.caps)}`); continue; }
      if (!Number.isInteger(r.bays) || r.bays < 1) b2.push(`${where}: ${r.bays} bays`);
      const L = Math.abs(r.b[0] - r.a[0]) + Math.abs(r.b[1] - r.a[1]), want = r.caps.reduce((s, c) => s + (c === 'corner' ? cell : 0), 0) + bay * r.bays;
      if (Math.abs(L - want) > 0.01) b2.push(`${where}: ${L.toFixed(2)} m long, caps + 6 × ${r.bays} = ${want} (8 + 6k + 8 corner to corner)`);
      (r.gap ?? [0, 0]).forEach((g, e) => { if (g > 0.01 && !['join', 'arc'].includes(r.caps[e]!) && !(r.faces === 'passage' && r.caps[e] === 'open')) b2.push(`${where}: a ${g} m gap at a ${r.caps[e]} end (only a join or an arc end, or a passage face's open end between the long faces' end bays, leaves one)`); if (g >= bay) b2.push(`${where}: gap ${g} m, under ${bay} m or it is a bay`); });
      for (const e of [0, 1] as const) {
        if (r.caps[e] !== 'corner') continue;
        const p = e ? r.b : r.a, mate = b.runs.find(o => o !== r && (Math.abs(o.a[1] - o.b[1]) < 1e-6) !== alongX && ((o.caps[0] === 'corner' && near(o.a, p)) || (o.caps[1] === 'corner' && near(o.b, p))));
        if (!mate) b2.push(`${where}: corner cap at (${p.join(',')}) meets no perpendicular corner cap`);
        else if (alongX) nCells++;
        if (!b.corners.some(c => near(c, p))) b2.push(`${where}: corner (${p.join(',')}) is not in the block's corners`);
      }
    }
    for (const c of b.corners) if (!b.runs.some(r => (r.caps[0] === 'corner' && near(r.a, c)) || (r.caps[1] === 'corner' && near(r.b, c)))) b2.push(`${b.id}: listed corner (${c.join(',')}) has no corner cap`);
  }
  out.push({ id: 'BG-2', group: 'Blocks', rule: 'Every run is 6 m bays between its caps exactly (a corner cap is the 8 m cell, shared with the perpendicular run; end, open, join and arc are 0), a gap only at a join or arc end and under 6 m', value: `${nRuns} runs, ${nCells} corner cells`, limit: 'all exact', ok: !b2.length, detail: list(b2), source: SRC });

  // ---- BG-3: bands in the building line, clear of reserved masses and the circus; depths
  const b3: string[] = [], band = S.band as number, circ = plan.circus as any;
  for (const b of blocks) {
    const bl = line.get(b.plot);
    if (!bl) { b3.push(`${b.id}: plot ${b.plot} has no building line`); continue; }
    if (b.depth !== band) b3.push(`${b.id}: depth ${b.depth}, the band is ${band}`);
    for (const r of b.runs) {
      const q = bandOf(r, band), pts: V[] = [[q[0], q[2]], [q[1], q[2]], [q[1], q[3]], [q[0], q[3]]];
      const out1 = pts.filter(p => !inside(p, bl.p));
      if (out1.length) b3.push(`${b.id} ${r.face}: band corner (${out1[0]!.map(v => v.toFixed(1)).join(',')}) leaves the building line`);
      const hit = reserved.find(x => overlaps(q, x));
      if (hit) b3.push(`${b.id} ${r.face}: band enters the reserved mass [${hit.join(', ')}]`);
      if (circ && pts.some(p => Math.hypot(p[0] - circ.c[0], p[1] - circ.c[1]) < circ.pavement - 0.05)) b3.push(`${b.id} ${r.face}: band enters the circus (r ${circ.pavement})`);
    }
    const W = b.rect[1] - b.rect[0], D = b.rect[3] - b.rect[2], m = Math.min(W, D);
    if (m < S.backToBack - 0.01) b3.push(`${b.id}: ${m.toFixed(1)} m deep, under ${S.backToBack} (14 back to back)`);
    if (b.kind === 'perimeter' && (b.court === 'open') !== (m >= S.solidBelow - 0.01)) b3.push(`${b.id}: ${m.toFixed(1)} m deep is ${m >= S.solidBelow ? 'an open court' : 'solid'}, the plan says ${b.court}`);
    if (b.kind === 'inner') {
      const ring = blocks.filter(x => x.plot === b.plot && x.kind === 'perimeter' && x.rect[0] <= b.rect[0] && x.rect[1] >= b.rect[1] && x.rect[2] <= b.rect[2] && x.rect[3] >= b.rect[3]);
      if (!ring.length) b3.push(`${b.id}: stands in no perimeter block's court`);
      for (const x of ring) { const c = S.innerClear + band; if (b.rect[0] - x.rect[0] < c - 0.01 || x.rect[1] - b.rect[1] < c - 0.01 || b.rect[2] - x.rect[2] < c - 0.01 || x.rect[3] - b.rect[3] < c - 0.01) b3.push(`${b.id}: under ${S.innerClear} m from ${x.id}'s ring`); }
      if (reserved.some(x => overlaps(b.rect, x))) b3.push(`${b.id}: stands on a reserved mass`);
    }
  }
  out.push({ id: 'BG-3', group: 'Blocks', rule: 'Every run\'s 14 m band stands inside its plot\'s building line, clear of tower podiums, megastructures, signature grounds and the circus; blocks 28 deep or more, solid under 40, an open court from 40, inner blocks 10 m clear in the court', value: `${blocks.length} blocks`, limit: 'all', ok: !b3.length, detail: list(b3), source: SRC });

  // ---- BG-4: corner cells on the street corners
  const b4: string[] = [];
  let nCorners = 0;
  for (const id of laid) {
    const bl = line.get(id), mine = blocks.filter(b => b.plot === id); if (!bl) continue;
    const n = bl.p.length;
    for (let i = 0; i < n; i++) {
      const c0 = bl.c[(i + n - 1) % n]!, c1 = bl.c[i]!; if (!ROADS.includes(kindOf(c0)) || !ROADS.includes(kindOf(c1))) continue;
      const a = bl.p[(i + n - 1) % n]!, p = bl.p[i]!, b = bl.p[(i + 1) % n]!, cr = (p[0] - a[0]) * (b[1] - p[1]) - (p[1] - a[1]) * (b[0] - p[0]);
      if (cr <= 0) continue; // concave: no corner cell
      nCorners++;
      if (reserved.some(x => p[0] >= x[0] - band && p[0] <= x[1] + band && p[1] >= x[2] - band && p[1] <= x[3] + band)) continue; // a reserved mass within one band of the corner: no room for a block between them
      if (!mine.some(bk => bk.corners.some(c => Math.abs(c[0] - p[0]) < 6 && Math.abs(c[1] - p[1]) < 6))) b4.push(`${id}: street corner (${p.map(v => v.toFixed(1)).join(',')}) of ${c0} and ${c1} has no corner cell within 6 m`);
    }
  }
  out.push({ id: 'BG-4', group: 'Blocks', rule: 'Every street corner of a laid plot (two roads meeting at a convex building-line corner) carries an 8 m corner cell within 6 m, unless a reserved mass stands within one band (14 m) of it, leaving no room for a block', value: `${nCorners - b4.length}/${nCorners}`, limit: 'all', ok: !b4.length, detail: list(b4), source: SRC });

  // ---- BG-5: crescents
  const b5: string[] = [];
  const notched = [...laid].filter(id => d.plots.find(p => p.id === id)?.notch);
  for (const id of notched) if (crescents.filter(c => c.plot === id).length !== 1) b5.push(`${id}: ${crescents.filter(c => c.plot === id).length} crescents, one per circus plot`);
  for (const c of crescents) {
    const r = c.r as number, half = Math.asin(bay / 2 / r), step = (2 * half * 180) / Math.PI, rEnds = r / Math.cos(half);
    if (!circ || Math.abs(r - circ.pavement) > 0.01) b5.push(`${c.id}: r ${r}, the circus building line is ${circ?.pavement}`);
    if (c.chord !== bay) b5.push(`${c.id}: chord ${c.chord}, the bay is ${bay}`);
    if (Math.abs(c.stepDeg - step) > 0.001 || Math.abs(c.rEnds - rEnds) > 0.01) b5.push(`${c.id}: stepDeg / rEnds ${c.stepDeg} / ${c.rEnds}, the chord gives ${step.toFixed(4)} / ${rEnds.toFixed(2)}`);
    const sweep = c.deg[1] - c.deg[0];
    if (Math.abs(Math.abs(sweep) - c.bays * step) > 0.01 || (sweep > 0 ? 'ccw' : 'cw') !== c.sweep || !(c.bays >= 1)) b5.push(`${c.id}: ${c.bays} bays sweep ${sweep.toFixed(3)}°, ${(c.bays * step).toFixed(3)}° ${c.sweep} asked`);
    if (JSON.stringify(c.caps) !== '["open","end"]') b5.push(`${c.id}: caps ${JSON.stringify(c.caps)}, open at the street wall and end toward the E-W face`);
    const bk = blocks.find(b => b.id === c.block), bl = line.get(c.plot);
    const start = bk?.runs.find(x => x.caps.includes('arc') && (x.face === 'e' || x.face === 'w'));
    const sp = start && (start.caps[1] === 'arc' ? start.b : start.a);
    const [ox, oy] = (c.c ?? P.circus?.c ?? [0, 0]) as V, at = (rad: number, g: number): V => [ox + rad * Math.cos(g), oy + rad * Math.sin(g)]; // about the crescent's centre (the circus), not the plan origin
    const s0: V = at(rEnds, (c.deg[0] * Math.PI) / 180);
    if (!sp || Math.abs(s0[0] - sp[0]) > 0.01 || Math.hypot(s0[0] - sp[0], s0[1] - sp[1]) >= bay) b5.push(`${c.id}: does not start on ${c.block}'s N-S facade line within a bay of its run's arc end`);
    for (let k = 0; k < c.bays; k++) {
      const g0 = ((c.deg[0] + Math.sign(sweep) * k * step) * Math.PI) / 180, g1 = ((c.deg[0] + Math.sign(sweep) * (k + 1) * step) * Math.PI) / 180;
      const q: V[] = [at(rEnds, g0), at(rEnds, g1), at(rEnds + band, g1), at(rEnds + band, g0)];
      const hit = reserved.find(x => quadHits(q, x)); if (hit) b5.push(`${c.id} bay ${k + 1}: band enters the reserved mass [${hit.join(', ')}]`);
      if (bl && q.some(p => !inside(p, bl.p, 0.1))) b5.push(`${c.id} bay ${k + 1}: leaves ${c.plot}'s building line`);
    }
  }
  out.push({ id: 'BG-5', group: 'Blocks', rule: 'Crown Circus is ringed by crescents: one per laid circus plot, 6 m chords on the building-line circle from the N-S street wall (open) to an end cap, bands inside the plot and clear of reserved masses', value: crescents.map(c => `${c.id} ${c.bays}`).join(', ') || 'none', limit: `${notched.length} crescents`, ok: !b5.length, detail: list(b5), source: SRC });
  return out;
}
