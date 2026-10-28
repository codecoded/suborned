// RCH-1, reachability (brief 075, Sky 2026-10-28: "we need the skytrak to get here - it's unreachable right now. that's datums job to map the world to make sure
// everywhere has a real structure"). Every landmark, megastructure, sky rail station, pier, platform and deck joins the street network by a walkable or rideable path.
//
// The street network on the ground: a 1 m raster of the plan, a cell open unless it is water (outside every quay and pier), beyond, outside the sheet or under a
// mass that stands on the ground (plot buildings, civic masses, megastructure parts); flooded (4 neighbours) from the carriageway centrelines. A landmark disc is
// open ground of its own (it is the thing reached), but a caisson's disc stands in the water, so only what the plan builds into it counts.
// Up in the air: decks and station platforms grouped as the generator groups them (connectors.ts deckGroups); a group is reached when a connector that leaves
// from the street stands on the street network (its ring grown JOIN_GROW) or inside a ground mass whose box meets it (a core in a podium), through a connector from a reached group, or by riding: every platform of one sky
// rail line is reached once one of them is.
// A thing is joined when at least JOINT open cells (3 x 3 m: a stair's width each way, L-3) of the street network or of a reached deck lie in its footprint: a
// pier that only touches a caisson's drum at a point is no way in. Unknown = reject: a plan station with no platform in the export, a landmark or pier the rule
// cannot read, fails.
import type { World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import type { V } from '../../plan/geom';
import { type WCtx, rule } from '../../plan/world-ctx';
import { deckGroups } from '../gen/connectors';

export const CELL = 1; // m
export const JOINT = 9; // cells: a 3 x 3 m joint
const JOIN_GROW = 2; // m round a connector's ring for its foot on the street network
const MASS_MIN_H = 2; // m: a ground mass lower than this (a kerb, a bench) is walked round, not a wall
const MASS_MAX_BASE = 1; // m: a mass whose base is higher stands over the ground (a deck slab, a bridge)

/**
 * Whether RCH-1 fails the check (false: it reports, WAIVE). Informational until Datum's sky track branch to the Skyport lands (brief 075): the live plan has the
 * Skyport Tower and the liner berth in the water with no way in. Flip to true once `bun run plan` shows RCH-1 at all/all.
 */
export const RCH_ENFORCED = false;

type Ring = V[];
export type ReachInput = {
  roads: V[][]; water: Ring[]; quay: Ring[]; beyond: Ring[];
  /** the masses standing on the ground (blocked) */
  masses: Ring[];
  landmarks: { id: string; c: V; r: number; caisson?: boolean }[];
  megastructures: { id: string; footprint: [number, number, number, number][] }[];
  /** plan stations: line + id */
  stations: { line: string; id: string }[];
  decks: { id: string; level: string; ring: Ring }[];
  /** station platforms (export transit.stations): id station/<line>/<station>/<side> */
  platforms: { id: string; ring: Ring }[];
  connectors: { id: string; from: string; serves: string[]; ring: Ring }[];
};
export type ReachReport = { islands: string[]; checked: number; joined: string[]; groups: number; reachedGroups: number };

type R4 = [number, number, number, number];
const bbox = (r: Ring): R4 => r.reduce<R4>((b, p) => [Math.min(b[0], p[0]), Math.max(b[1], p[0]), Math.min(b[2], p[1]), Math.max(b[3], p[1])], [Infinity, -Infinity, Infinity, -Infinity] as [number, number, number, number]);
const centreOf = (r: Ring): V => [r.reduce((t, p) => t + p[0], 0) / r.length, r.reduce((t, p) => t + p[1], 0) / r.length];
const inRing = (p: V, r: Ring) => { let c = false; for (let i = 0, j = r.length - 1; i < r.length; j = i++) { const a = r[i]!, b = r[j]!; if ((a[1] > p[1]) !== (b[1] > p[1]) && p[0] < ((b[0] - a[0]) * (p[1] - a[1])) / (b[1] - a[1]) + a[0]) c = !c; } return c; };
const rectRing = (r: [number, number, number, number], g = 0): Ring => [[r[0] - g, r[2] - g], [r[1] + g, r[2] - g], [r[1] + g, r[3] + g], [r[0] - g, r[3] + g]];

class Grid {
  nx: number; ny: number; x0: number; y0: number;
  constructor(b: [number, number, number, number]) { this.x0 = Math.floor(b[0]); this.y0 = Math.floor(b[2]); this.nx = Math.ceil(b[1] - this.x0) + 1; this.ny = Math.ceil(b[3] - this.y0) + 1; }
  cx(i: number) { return this.x0 + (i + 0.5) * CELL; }
  cy(j: number) { return this.y0 + (j + 0.5) * CELL; }
  at(p: V): number { const i = Math.floor((p[0] - this.x0) / CELL), j = Math.floor((p[1] - this.y0) / CELL); return i < 0 || j < 0 || i >= this.nx || j >= this.ny ? -1 : j * this.nx + i; }
  /** Calls f(k) for every cell whose centre is inside the polygon (scanline, even-odd). */
  fill(r: Ring, f: (k: number) => void) {
    if (r.length < 3) return;
    const b = bbox(r), j0 = Math.max(0, Math.floor((b[2] - this.y0) / CELL)), j1 = Math.min(this.ny - 1, Math.ceil((b[3] - this.y0) / CELL));
    for (let j = j0; j <= j1; j++) {
      const y = this.cy(j), xs: number[] = [];
      for (let a = 0, c = r.length - 1; a < r.length; c = a++) { const p = r[a]!, q = r[c]!; if ((p[1] > y) !== (q[1] > y)) xs.push(p[0] + ((y - p[1]) * (q[0] - p[0])) / (q[1] - p[1])); }
      xs.sort((u, v) => u - v);
      for (let t = 0; t + 1 < xs.length; t += 2) {
        const i0 = Math.max(0, Math.ceil((xs[t]! - this.x0) / CELL - 0.5)), i1 = Math.min(this.nx - 1, Math.floor((xs[t + 1]! - this.x0) / CELL - 0.5));
        for (let i = i0; i <= i1; i++) f(j * this.nx + i);
      }
    }
  }
  disc(c: V, r: number, f: (k: number) => void) {
    const i0 = Math.max(0, Math.floor((c[0] - r - this.x0) / CELL)), i1 = Math.min(this.nx - 1, Math.ceil((c[0] + r - this.x0) / CELL)), j0 = Math.max(0, Math.floor((c[1] - r - this.y0) / CELL)), j1 = Math.min(this.ny - 1, Math.ceil((c[1] + r - this.y0) / CELL));
    for (let j = j0; j <= j1; j++) for (let i = i0; i <= i1; i++) if (Math.hypot(this.cx(i) - c[0], this.cy(j) - c[1]) < r) f(j * this.nx + i);
  }
}

/** The islands: everything the rule names that no walkable or rideable path joins to the street network. */
export function reachReport(I: ReachInput): ReachReport {
  const islands: string[] = [], joined: string[] = [];
  const all: Ring[] = [...I.water, ...I.quay, ...I.beyond, ...I.roads, ...I.landmarks.map(l => rectRing([l.c[0] - l.r, l.c[0] + l.r, l.c[1] - l.r, l.c[1] + l.r])), ...I.megastructures.flatMap(m => m.footprint.map(f => rectRing(f))), ...I.decks.map(d => d.ring), ...I.platforms.map(p => p.ring)];
  const B = all.reduce<R4>((b, r) => { const q = bbox(r); return [Math.min(b[0], q[0]), Math.max(b[1], q[1]), Math.min(b[2], q[2]), Math.max(b[3], q[3])] as [number, number, number, number]; }, [Infinity, -Infinity, Infinity, -Infinity] as [number, number, number, number]);
  const G = new Grid([B[0] - 4, B[1] + 4, B[2] - 4, B[3] + 4]), N = G.nx * G.ny;
  // 0 open, 1 blocked; water is blocked outside the quays and piers; a landmark's disc is not a mass here (it is the thing reached), so a caisson's is water
  const blocked = new Uint8Array(N), quay = new Uint8Array(N);
  for (const q of I.quay) G.fill(q, k => { quay[k] = 1; });
  for (const w of I.water) G.fill(w, k => { if (!quay[k]) blocked[k] = 1; });
  for (const b of I.beyond) G.fill(b, k => { blocked[k] = 1; });
  for (const m of I.masses) G.fill(m, k => { blocked[k] = 1; });
  // flood from the carriageway centrelines
  const main = new Uint8Array(N), stack: number[] = [];
  for (const line of I.roads) for (let s = 0; s + 1 < line.length; s++) {
    const a = line[s]!, b = line[s + 1]!, n = Math.max(1, Math.ceil(Math.hypot(b[0] - a[0], b[1] - a[1]) / CELL));
    for (let t = 0; t <= n; t++) { const k = G.at([a[0] + ((b[0] - a[0]) * t) / n, a[1] + ((b[1] - a[1]) * t) / n]); if (k >= 0 && !blocked[k] && !main[k]) { main[k] = 1; stack.push(k); } }
  }
  while (stack.length) {
    const k = stack.pop()!, i = k % G.nx, j = (k - i) / G.nx;
    for (const [di, dj] of [[1, 0], [-1, 0], [0, 1], [0, -1]] as const) { const a = i + di, b = j + dj; if (a < 0 || b < 0 || a >= G.nx || b >= G.ny) continue; const q = b * G.nx + a; if (!blocked[q] && !main[q]) { main[q] = 1; stack.push(q); } }
  }
  const count = (mask: Uint8Array, fill: (f: (k: number) => void) => void) => { let n = 0; fill(k => { if (mask[k]) n++; }); return n; };

  // the air: deck and platform groups, joined by connectors and rides
  const pieces = [...I.decks.map(d => ({ id: d.id, level: d.level, rect: bbox(d.ring) })), ...I.platforms.map(p => ({ id: p.id, level: 'concourse', rect: bbox(p.ring) }))];
  const groupOf = deckGroups(pieces as never), groups = new Set(groupOf.values()), reached = new Set<string>(), links = new Map<string, Set<string>>();
  const link = (a: string, b: string) => { if (a === b) return; (links.get(a) ?? links.set(a, new Set()).get(a)!).add(b); (links.get(b) ?? links.set(b, new Set()).get(b)!).add(a); };
  for (const k of I.connectors) {
    const gs = [...new Set(k.serves.map(s => groupOf.get(s)).filter((x): x is string => !!x))];
    if (k.from === 'street' && gs.length) {
      // its foot on the street network, or inside a ground mass (a core in a podium) whose own walls meet the street network: entered through that building
      const b = bbox(k.ring), foot = centreOf(k.ring), host = I.masses.filter(m => inRing(foot, m));
      if (count(main, f => G.fill(rectRing(b, JOIN_GROW), f)) > 0 || host.some(m => count(main, f => G.fill(rectRing(bbox(m), JOIN_GROW), f)) > 0)) gs.forEach(g => reached.add(g));
    }
    for (const a of gs) for (const b of gs) link(a, b);
  }
  const lineOf = (id: string) => id.split('/')[1] ?? '', byLine = new Map<string, string[]>();
  for (const p of I.platforms) { const g = groupOf.get(p.id)!; (byLine.get(lineOf(p.id)) ?? byLine.set(lineOf(p.id), []).get(lineOf(p.id))!).push(g); }
  for (const gs of byLine.values()) for (const g of gs) link(gs[0]!, g); // riding the line
  const queue = [...reached];
  while (queue.length) { const g = queue.pop()!; for (const h of links.get(g) ?? []) if (!reached.has(h)) { reached.add(h); queue.push(h); } }
  const air = new Uint8Array(N);
  for (const d of I.decks) if (reached.has(groupOf.get(d.id)!)) G.fill(d.ring, k => { air[k] = 1; });
  for (const p of I.platforms) if (reached.has(groupOf.get(p.id)!)) G.fill(p.ring, k => { air[k] = 1; });
  const either = new Uint8Array(N); for (let k = 0; k < N; k++) either[k] = main[k] || air[k] ? 1 : 0;

  const judge = (name: string, ok: boolean, why: string) => (ok ? joined.push(name) : islands.push(`${name}: ${why}`));
  for (const l of I.landmarks) {
    if (!Array.isArray(l.c) || !(l.r > 0)) { judge(`landmark/${l.id}`, false, 'no disc the rule can read (unknown = reject)'); continue; }
    const n = count(either, f => G.disc(l.c, l.r, f));
    judge(`landmark/${l.id}`, n >= JOINT, `${n} m² of its ${l.caisson ? 'caisson' : 'disc'} (r ${l.r} at ${l.c.join(', ')}) on the street network or a reached deck (< ${JOINT})${l.caisson ? '; it stands in the water' : ''}`);
  }
  I.quay.forEach((q, i) => {
    const b = bbox(q), name = `pier/quay[${i}] [${b.map(Math.round).join(', ')}]`;
    if (q.length < 3) { judge(name, false, 'no ring the rule can read (unknown = reject)'); return; }
    const n = count(main, f => G.fill(q, f)); judge(name, n >= JOINT, `${n} m² on the street network (< ${JOINT})`);
  });
  for (const m of I.megastructures) {
    const n = m.footprint.reduce((s, f) => s + count(main, g => G.fill(rectRing(f, JOIN_GROW), g)), 0), up = I.decks.some(d => d.id.startsWith(`mega/${m.id}/`) && reached.has(groupOf.get(d.id)!));
    judge(`mega/${m.id}`, n >= JOINT || up, `${n} m² of its footprint (grown ${JOIN_GROW} m) on the street network (< ${JOINT}) and no deck of it reached`);
  }
  const deckIds = new Map<string, string[]>(); for (const p of pieces) (deckIds.get(groupOf.get(p.id)!) ?? deckIds.set(groupOf.get(p.id)!, []).get(groupOf.get(p.id)!)!).push(p.id);
  for (const g of [...groups].sort()) { const ids = deckIds.get(g) ?? [g]; judge(`${ids.some(i => i.startsWith('station/')) ? 'platform' : 'deck'} ${ids.length > 1 ? `group ${g} (${ids.length} pieces)` : g}`, reached.has(g), 'no connector from the street network, no connector to a reached deck and no ride to a reached station'); }
  for (const s of I.stations) {
    const ps = I.platforms.filter(p => p.id.startsWith(`station/${s.line}/${s.id}/`));
    judge(`station/${s.line}/${s.id}`, ps.length > 0 && ps.some(p => reached.has(groupOf.get(p.id)!)), ps.length ? 'no platform of it reached' : 'no platform in the export (unknown = reject)');
  }
  return { islands, checked: islands.length + joined.length, joined, groups: groups.size, reachedGroups: [...groups].filter(g => reached.has(g)).length };
}

/** The rule's input from a plan and its generated world. */
export function reachInputOf(c: Pick<WCtx, 'plan' | 'd' | 'w'>): ReachInput {
  const { plan, d } = c, w = c.w as World;
  const ground = (m: { base: number; height: number; ring: V[] }) => m.base <= MASS_MAX_BASE && m.height >= MASS_MIN_H;
  return {
    roads: d.roads.map(r => r.line), water: plan.water, quay: plan.quay, beyond: plan.beyond ?? [],
    masses: [...w.buildings.filter(ground), ...(w.civic as any[]).filter(m => Array.isArray(m.ring) && ground(m)), ...w.megastructures.flatMap(m => m.parts.filter(ground))].map(m => m.ring as V[]),
    landmarks: plan.landmarks.map(l => ({ id: l.id, c: l.c, r: l.r, caisson: !!l.caisson })),
    megastructures: (plan.megastructures ?? []).map(m => ({ id: m.id, footprint: (m as any).footprint ?? [] })),
    stations: plan.skyrail.flatMap(l => (l.stations ?? []).map(s => ({ line: l.id, id: s.id }))),
    decks: w.levels.flatMap(l => l.decks.map(x => ({ id: x.id, level: l.name, ring: x.ring as V[] }))),
    platforms: w.transit.stations.map(s => ({ id: s.id, ring: s.ring as V[] })),
    connectors: w.connectors.map(k => ({ id: k.id, from: k.from, serves: k.serves, ring: (k.ring ?? [k.foot]) as V[] })),
  };
}

const SOURCE = 'brief 075 (Sky 2026-10-28: "we need the skytrak to get here - it\'s unreachable right now … make sure everywhere has a real structure") · standards.levels (L-1, L-3 stair 3 m) · '
  + 'tools/world/rules/reach.ts · failing examples (reach.test.ts): a caisson only touching its pier\'s end; a pier cut off by water; a deck whose stair foot stands in a building; a station with no platform';

/** RCH-1: every landmark, megastructure, station, pier, platform and deck joins the street network. Informational (WAIVE) while RCH_ENFORCED is false. */
export function reachRules(c: WCtx, enforced = RCH_ENFORCED): Check[] {
  const r = reachReport(reachInputOf(c));
  const k = rule('RCH-1', 'Levels', `Every landmark, megastructure, station, pier, platform and deck joins the street network (road, pavement, pier, stair, lift, ramp or a reached sky track station) by a walkable or rideable path, a joint of at least ${JOINT} m²`, r.checked, r.islands, 'no island', SOURCE);
  if (!enforced && !k.ok) return [{ ...k, ok: true, waived: 'informational until the sky track reaches the Skyport (brief 075, Datum)', detail: `WAIVED (informational until the sky track reaches the Skyport, brief 075); ${k.detail}` }];
  return [k];
}
