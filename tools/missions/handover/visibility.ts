// Rule V-1 (brief 003 B; brief 011, Vantage 2026-10-15; heading per point, 2026-10-18): every Handover point is seen at the play camera,
// past architecture, without the fade, from the heading it is played at. GDD §10 (v0.24) fixes the camera (zoom 14 at 38°, opening yaw 30,
// turned by Q/E in quarter steps: 30 / 120 / 210 / 300); the Records Office front is played at yaw 210. A point's `yaw` in the mission
// says so (default 30). §15's fade thins a tower over the focus to a 0.2 ghost, which shows the street behind it but is not how a
// mission beat should read. Measured on the generated world (tools/missions/sight.ts): people points as a 1.75 m figure, camera cones
// and the patrol's sweep as ground. Limit [S]: half the figure, half the ground. A camera cone counts only its open ground (outside
// building footprints: a cone cannot reach through a building, and a roof is no place the player looks for a person).
// The studies behind it: briefs/011/vantage-visibility.md, briefs/011/vantage/door-recheck.ts (Sky picked option (h), 2026-10-18).
import type { Mass, World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import type { Civic } from '../../plan/civic';
import { toPlanFn } from '../../plan/civic';
import { type V, inPoly } from '../../plan/geom';
import { type Blocker, type PieceShape, type View, PLAY, blockersOf, groundSeen, seen } from '../sight';

/** [S] share of a figure (or of a cone's or sweep's ground) that must be seen past architecture. */
export const V1_LIMIT = 0.5;
/** The headings the view can take (GDD §10: Q/E turn it in quarter steps from the opening yaw 30). A point's `yaw` is one of these. */
export const YAWS = [30, 120, 210, 300] as const;
const PAVE = 0.18, LANDING = 0.75;
const viewAt = (yaw?: number): View => ({ ...PLAY, yaw: yaw ?? PLAY.yaw });

type M = {
  /** `hidden`: the mission's own reason a point is out of the camera's view by design (a hiding place); V-1 lists it and does not fail it.
   *  `yaw`: the heading the point is played at (30 / 120 / 210 / 300; default 30). A camera's `yaw` is its cone's; the patrol's sweep takes patrol-a's and patrol-b's. */
  destinations: { name: string; at: V; surface: string; hidden?: string; yaw?: number }[]; crew: { name: string; at: V; yaw?: number }[];
  points?: { id: string; at?: V; structure?: string; hidden?: string; yaw?: number }[]; cameras?: { id: string; at: V; looksAt: string; reachM: number; halfM: number; yaw?: number }[];
};
export type Row = { what: string; kind: 'person' | 'cone' | 'sweep'; p: V; z: number; seen: number; clear: number; onlyFade: number; by: string[]; yaw: number; declared?: string; problem?: string };

/** The Records Office entrances as the camera meets them: piers, pilasters and cheeks standing, the marquee and canopy overhead
 *  (the block's dressing pieces and the official canopy structure); the export's ring for these is the piece's whole box. */
export function entranceShapes(w: World, civ: Civic, toPlan: (p: V) => V): PieceShape[] {
  const pieces = ((civ.dressing as any)?.pieces ?? []) as { piece: string; solids?: { id: string; polygon: V[]; z?: number; height?: number }[] }[];
  const canopy = civ.structures.filter(s => s.category === 'boarding-canopy' && s.z > 0);
  return pieces.filter(p => /^RecordsEntrance/.test(p.piece)).flatMap(p => {
    const m = w.civic.find(x => x.kit?.id === p.piece) as Mass | undefined;
    if (!m) return [];
    const top = m.base + m.height, parts = (p.solids ?? []).map(s => s.z !== undefined && s.z !== null
      ? { ring: s.polygon.map(toPlan), z0: PAVE + s.z, z1: PAVE + s.z + (s.height ?? 0.5) }
      : { ring: s.polygon.map(toPlan), z0: PAVE, z1: /^cheek/.test(s.id) ? PAVE + 1.1 : top });
    // the official piece is flush: its canopy is the block's structure (z 4.05, 0.57 thick), carried on the tower
    for (const c of canopy) if (c.poly.some(q => inPoly(q, m.ring.map(r => [r[0], r[1]] as V))) || m.ring.some(q => inPoly(q, c.poly))) parts.push({ ring: c.poly, z0: PAVE + c.z, z1: PAVE + c.z + c.height });
    return [{ id: m.id, parts }];
  });
}

/** Every Handover point measured, each from its own heading (`yaw`, default 30). `blockers` from blockersFor (or a study's own). */
export function visibility(m: M, civ: Civic, plan: any, blockers: Blocker[]): Row[] {
  const toPlan = toPlanFn(plan.civic), rows: Row[] = [];
  const zAt = (p: V, surface?: string) => (surface === 'office-public' ? PAVE + LANDING : civ.bays.some(b => inPoly(p, b.poly)) ? 0 : PAVE);
  const person = (what: string, at: V, surface?: string, declared?: string, yaw?: number) => {
    const p = toPlan(at), z = zAt(p, surface), v = viewAt(yaw), s = seen(p, z, blockers, v);
    rows.push({ what, kind: 'person', p, z, seen: s.pastArchitecture, clear: s.clear, onlyFade: s.onlyFade, by: s.by, yaw: v.yaw, ...(declared ? { declared } : {}) });
  };
  for (const d of m.destinations) person(d.name, d.at, d.surface, d.hidden, d.yaw);
  for (const c of m.crew) person(`crew ${c.name}`, c.at, undefined, undefined, c.yaw);
  for (const pt of m.points ?? []) {
    if (pt.at) person(pt.id, pt.at, undefined, pt.hidden, pt.yaw);
    else if (pt.structure) { const s = civ.structures.find(x => x.id === pt.structure); if (s) { const c: V = [s.poly.reduce((a, q) => a + q[0], 0) / s.poly.length, s.poly.reduce((a, q) => a + q[1], 0) / s.poly.length]; const z = PAVE, v = viewAt(pt.yaw), r = seen(c, z, blockers, v); rows.push({ what: pt.id, kind: 'person', p: c, z, seen: r.pastArchitecture, clear: r.clear, onlyFade: r.onlyFade, by: r.by, yaw: v.yaw, ...(pt.hidden ? { declared: pt.hidden } : {}) }); } }
  }
  const footprints = civ.buildings.flatMap(b => [b.poly, ...(b.towers ?? []).flatMap(t => (t.poly ? [t.poly] : []))]);
  for (const c of m.cameras ?? []) { // the cone's ground: a wedge from the pole toward its target, reachM long, halfM either side at the far end
    const a = toPlan(c.at), t = m.destinations.find(d => d.name === c.looksAt);
    if (!t) continue;
    const b = toPlan(t.at), L = Math.hypot(b[0] - a[0], b[1] - a[1]), u: V = [(b[0] - a[0]) / L, (b[1] - a[1]) / L], n: V = [-u[1], u[0]], pts: V[] = [];
    for (let i = 1; i <= 12; i++) for (let j = -4; j <= 4; j++) { const s = (c.reachM * i) / 12, w = (c.halfM * s / c.reachM) * (j / 4); pts.push([a[0] + u[0] * s + n[0] * w, a[1] + u[1] * s + n[1] * w]); }
    const open = pts.filter(q => !footprints.some(f => inPoly(q, f))), v = viewAt(c.yaw), g = groundSeen(open, PAVE + 0.05, blockers, v);
    rows.push({ what: `camera ${c.id} cone`, kind: 'cone', p: a, z: PAVE, seen: g, clear: g, onlyFade: 0, by: [], yaw: v.yaw });
  }
  const A = m.points?.find(p => p.id === 'patrol-a'), B = m.points?.find(p => p.id === 'patrol-b');
  if (A?.at && B?.at) { // the patrol's sweep: a figure every metre between its two ends, read from the heading its ends are played at
    const a = toPlan(A.at), b = toPlan(B.at), v = viewAt(A.yaw), n = Math.max(2, Math.round(Math.hypot(b[0] - a[0], b[1] - a[1]))), S = Array.from({ length: n + 1 }, (_, i) => seen([a[0] + ((b[0] - a[0]) * i) / n, a[1] + ((b[1] - a[1]) * i) / n], PAVE, blockers, v));
    const apart = viewAt(B.yaw).yaw !== v.yaw;
    rows.push({ what: 'patrol sweep', kind: 'sweep', p: a, z: PAVE, seen: S.reduce((s, x) => s + x.pastArchitecture, 0) / S.length, clear: S.reduce((s, x) => s + x.clear, 0) / S.length, onlyFade: S.reduce((s, x) => s + x.onlyFade, 0) / S.length, by: [...new Set(S.flatMap(x => x.by))].sort(), yaw: v.yaw, ...(apart ? { problem: `patrol-a is played at yaw ${v.yaw} and patrol-b at ${viewAt(B.yaw).yaw}: the sweep joins them and needs one heading` } : {}) });
  }
  return rows;
}

/** The blockers around the civic site: the plan's two civic plots and 130 m round them, on every side now that the view turns (the
 *  ray climbs 0.78 m a metre: nothing near is taller than about 100 m, so nothing further back reaches over a point on the block). */
export function blockersFor(w: World, civ: Civic, plan: any): Blocker[] {
  const toPlan = toPlanFn(plan.civic), xs = civ.buildings.flatMap(b => b.poly.map(p => p[0])), ys = civ.buildings.flatMap(b => b.poly.map(p => p[1]));
  return blockersOf(w, [Math.min(...xs) - 130, Math.max(...xs) + 130, Math.min(...ys) - 130, Math.max(...ys) + 130], entranceShapes(w, civ, toPlan));
}

const pc = (x: number) => `${Math.round(x * 100)} %`;
/** V-1 on the rows. */
export function v1(rows: Row[]): Check {
  const below = rows.filter(r => r.problem || r.seen < V1_LIMIT - 1e-9), bad = below.filter(r => r.problem || !r.declared), declared = below.filter(r => !r.problem && r.declared), ghost = bad.filter(r => r.onlyFade > 0);
  const dressed = rows.filter(r => !r.problem && r.seen >= V1_LIMIT && r.clear < V1_LIMIT), turned = rows.filter(r => r.yaw !== PLAY.yaw).length;
  const name = (r: Row) => `${r.what}${r.yaw !== PLAY.yaw ? ` at yaw ${r.yaw}` : ''} ${pc(r.seen)}${r.kind === 'person' && r.onlyFade ? ` (${pc(r.onlyFade)} only through the fade)` : ''}${r.by.length ? ` behind ${r.by.filter(b => !/tree|parasol|cyp/.test(b)).slice(0, 3).join(', ')}` : ''}${r.problem ? ` (${r.problem})` : ''}`;
  return {
    id: 'V-1', group: 'Mission', rule: 'Every mission point is seen at the play camera (zoom 14, 38°, from the heading it is played at: yaw 30 unless the point says otherwise) past architecture, without the fade: a standing figure, a camera cone\'s open ground, the patrol\'s sweep',
    value: `${rows.length - below.length}/${rows.length}${declared.length ? ` (+${declared.length} hidden by design)` : ''}${turned ? `, ${turned} read from a turned heading` : ''}`, limit: `≥ ${pc(V1_LIMIT)} each [S]`, ok: !bad.length,
    detail: [bad.length ? `${bad.length} hidden: ${bad.map(name).join('; ')}` : '', ghost.length ? `${ghost.length} of them read only as a ghost when the fade's disc holds them` : '', declared.length ? `hidden by the mission's design: ${declared.map(r => `${r.what} (${r.declared})`).join('; ')}` : '', dressed.length ? `seen past architecture but under dressing (parasol, cypress, tree; the fade or the piece's job): ${dressed.map(r => `${r.what} ${pc(r.clear)} clear`).join('; ')}` : ''].filter(Boolean).join(' · ') || undefined,
    source: 'brief 003 B (V-1: mission points visible at the play camera); GDD §10 v0.24 (camera, quarter-step turn; the Records Office front played at yaw 210); tools/missions/sight.ts; briefs/011/vantage-visibility.md; briefs/011/vantage/door-recheck.ts (Sky\'s pick (h), 2026-10-18)',
  };
}
