// The plan's rules: roads (Buchanan hierarchy, sealed environmental areas, gates, frontage), tram, sky rail, the civic
// block, plus the mission routes. Each check reports a value against its limit.
import { type V, centroid, dist, inPoly, lineDist, lineToPoly, segHit } from './geom';
import { type Derived, type Plan, inZone, spanLimit, stopChainage } from './plan';
import { type Civic, blockPlanRect } from './civic';
import { type Walk, walks as footWalks } from './walk';

export type Check = { id: string; group: string; rule: string; value: string; limit: string; ok: boolean; detail?: string };
export type Route = { id: string; name: string; to: string; length: number; roads: string[]; gates: string[] };
export type Report = { checks: Check[]; routes: Route[]; crossings: { line: string; road: string; p: V }[]; stops: any[]; stations: any[]; walks: Walk[] };

const f = (x: number, d = 0) => x.toFixed(d);
type Edge = { a: string; b: string; road: string; len: number };

function edges(plan: Plan, d: Derived): Edge[] {
  return d.roads.flatMap(r => r.path.slice(1).map((b, i) => ({ a: r.path[i], b, road: r.id, len: r.segs[i]?.len ?? dist(plan.nodes[r.path[i]], plan.nodes[b]) })));
}

function dijkstra(E: Edge[], from: string) {
  const D = new Map<string, number>([[from, 0]]), prev = new Map<string, Edge>(), done = new Set<string>();
  for (;;) {
    let u: string | undefined, best = Infinity;
    for (const [n, v] of D) if (!done.has(n) && v < best) { best = v; u = n; }
    if (u === undefined) break; done.add(u);
    for (const e of E) for (const [x, y] of [[e.a, e.b], [e.b, e.a]]) if (x === u && best + e.len < (D.get(y) ?? Infinity)) { D.set(y, best + e.len); prev.set(y, e); }
  }
  const trace = (to: string) => { const out: Edge[] = []; let n = to; while (prev.has(n)) { const e = prev.get(n)!; out.unshift(e); n = e.a === n ? e.b : e.a; } return out; };
  return { D, trace };
}

export function check(plan: Plan, d: Derived, civ: Civic): Report {
  const C: Check[] = [], add = (c: Check) => C.push(c), std = plan.standards;
  const E = edges(plan, d), roadById = new Map(d.roads.map(r => [r.id, r]));
  const at = (n: string) => d.roads.filter(r => r.path.includes(n));
  const used = [...new Set(d.roads.flatMap(r => r.path))];

  // ---- roads ----
  const seen = new Set([used[0]]), queue = [used[0]];
  while (queue.length) { const u = queue.pop()!; for (const e of E) for (const [x, y] of [[e.a, e.b], [e.b, e.a]]) if (x === u && !seen.has(y)) { seen.add(y); queue.push(y); } }
  const unused = Object.keys(plan.nodes).filter(n => !used.includes(n));
  add({ id: 'R-C1', group: 'Roads', rule: 'One connected network', value: `${seen.size}/${used.length} nodes`, limit: 'all', ok: seen.size === used.length && !unused.length, detail: unused.length ? `unused nodes: ${unused.join(', ')}` : undefined });

  const mixed = used.filter(n => { const k = new Set(at(n).map(r => r.class)); return k.has('access') && k.has('primary'); });
  add({ id: 'R-H1', group: 'Roads', rule: 'No access road joins a primary', value: `${mixed.length}`, limit: '0', ok: !mixed.length, detail: mixed.join(', ') || undefined });

  for (const cls of ['primary', 'district'] as const) {
    let min = Infinity, where = '';
    for (const r of d.roads.filter(r => r.class === cls && !r.gyratory)) {
      let s = 0, last: number | null = null;
      r.path.forEach((n, i) => {
        if (i) s += r.segs[i - 1].len;
        if (at(n).length < 2 || d.offsheet.has(n)) return;
        if (last !== null && s - last < min) { min = s - last; where = `${r.name} at ${n}`; }
        last = s;
      });
    }
    const lim = std.roads.junctionSpacing[cls];
    add({ id: `R-H2${cls[0]}`, group: 'Roads', rule: `Junction spacing on ${cls} distributors`, value: `${f(min)} m`, limit: `≥ ${lim} m`, ok: min >= lim, detail: `closest: ${where}` });
  }

  // environmental areas: walk access roads without passing through a distributor junction
  const isDist = (n: string) => at(n).some(r => r.class !== 'access');
  const access = E.filter(e => roadById.get(e.road)!.class === 'access');
  const comp = new Map<string, number>(); let nComp = 0; const bad: string[] = [];
  const derivedGates = new Set<string>();
  for (const e0 of access) {
    if (comp.has(e0.road + '|' + e0.a + '|' + e0.b)) continue;
    const id = nComp++, stack = [e0], dists = new Set<string>(), roads = new Set<string>();
    comp.set(e0.road + '|' + e0.a + '|' + e0.b, id);
    while (stack.length) {
      const e = stack.pop()!; roads.add(e.road);
      for (const n of [e.a, e.b]) {
        if (isDist(n)) { at(n).filter(r => r.class !== 'access').forEach(r => dists.add(r.id)); derivedGates.add(`${n}|${e.road}`); continue; }
        for (const o of access) { const k = o.road + '|' + o.a + '|' + o.b; if ((o.a === n || o.b === n) && !comp.has(k)) { comp.set(k, id); stack.push(o); } }
      }
    }
    if (dists.size > 1) bad.push(`${[...roads].join('+')} links ${[...dists].join(' & ')}`);
  }
  add({ id: 'R-E1', group: 'Roads', rule: 'No access route links two distributors (no rat-runs)', value: `${bad.length} of ${nComp} networks`, limit: '0', ok: !bad.length, detail: bad.join('; ') || undefined });

  const declared = new Set(plan.gates.map(g => `${g.node}|${g.road}`));
  const missing = [...derivedGates].filter(g => !declared.has(g)), extra = [...declared].filter(g => !derivedGates.has(g));
  add({ id: 'R-G1', group: 'Roads', rule: 'Every way in is a declared gate (seal point)', value: `${derivedGates.size} found, ${declared.size} declared`, limit: 'equal', ok: !missing.length && !extra.length, detail: [...missing.map(m => `undeclared ${m}`), ...extra.map(x => `not a gate ${x}`)].join('; ') || undefined });

  const tol = std.roads.frontage, built = d.plots;
  const fronts = (poly: V[], ok: (cls: string) => boolean) => d.roads.some(r => ok(r.class) && lineToPoly(r.line, poly) <= r.w / 2 + tol);
  const noFront = built.filter(p => !(fronts(p.polyV, c => c === 'access') || (['park', 'open', 'square'].includes(p.use) && (fronts(p.polyV, () => true) || plan.foot.some(w => lineToPoly(w.pts, p.polyV) <= w.w / 2 + tol)))));
  add({ id: 'R-F1', group: 'Roads', rule: 'Every plot fronts an access road (parks: any street or walk)', value: `${built.length - noFront.length}/${built.length}`, limit: 'all', ok: !noFront.length, detail: noFront.map(p => p.id).join(', ') || undefined });

  const clash: string[] = [];
  for (const p of built) {
    for (const r of d.roads) if (lineToPoly(r.line, p.polyV) < r.w / 2 - .5) clash.push(`${p.id}×${r.id}`);
    for (const w of plan.foot) if (lineToPoly(w.pts, p.polyV) < w.w / 2 - .5) clash.push(`${p.id}×${w.id}`);
    if (p.polyV.some(v => dist(v, plan.circus.c) < plan.circus.pavement - .5)) clash.push(`${p.id}×circus`);
  }
  for (let i = 0; i < built.length; i++) for (let j = i + 1; j < built.length; j++) {
    const A = built[i].polyV, B = built[j].polyV, cA = centroid(A), cB = centroid(B);
    const inner = (P: V[], c: V) => P.map(v => [v[0] + Math.sign(c[0] - v[0]) * .3, v[1] + Math.sign(c[1] - v[1]) * .3] as V);
    if (inner(A, cA).some(v => inPoly(v, B)) || inner(B, cB).some(v => inPoly(v, A))) clash.push(`${built[i].id}×${built[j].id}`);
  }
  add({ id: 'R-O1', group: 'Roads', rule: 'No plot overlaps a street, walk, the circus or another plot', value: `${clash.length}`, limit: '0', ok: !clash.length, detail: clash.slice(0, 8).join(', ') || undefined });

  const astray = built.filter(p => !inPoly(centroid(p.polyV), d.areas.find(a => a.id === p.area)!.polyV));
  add({ id: 'R-A1', group: 'Roads', rule: 'Every plot sits in its environmental area', value: `${built.length - astray.length}/${built.length}`, limit: 'all', ok: !astray.length, detail: astray.map(p => p.id).join(', ') || undefined });

  // ---- tram ----
  const T = std.tram, stops: any[] = [], crossings: Report['crossings'] = [];
  for (const line of plan.tram) {
    const paths = d.tram.filter(p => p.line === line.id);
    for (const role of ['running', 'loop']) {
      const min = Math.min(...paths.filter(p => p.role === role).flatMap(p => p.samples.map(s => s.r)));
      const lim = role === 'running' ? T.radiusRunning : T.radiusLoop;
      add({ id: `T-R${role === 'running' ? 1 : 2}`, group: 'Tram', rule: `${line.id} ${role === 'running' ? 'running' : 'terminal loop'} radius`, value: min === Infinity ? 'straight' : `R ${f(min)} m`, limit: `≥ ${lim} m`, ok: min >= lim });
    }
    const placed = (line.stops ?? []).map(s => ({ ...s, ...stopChainage(d, line.id, s, 'tram') })).sort((a, b) => a.s - b.s);
    const gaps = placed.slice(1).map((s, i) => s.s - placed[i].s);
    placed.forEach((s, i) => stops.push({ line: line.id, id: s.id, name: s.name, at: s.at, chainage: s.s, next: gaps[i] }));
    const [lo, hi] = T.stopSpacing;
    add({ id: 'T-S1', group: 'Tram', rule: `${line.id} stop spacing`, value: gaps.map(g => f(g)).join(', ') + ' m', limit: `${lo}–${hi} m`, ok: gaps.every(g => g >= lo && g <= hi) });
    const curved = placed.filter(s => s.path.samples.some(x => Math.abs(x.s - s.s) <= T.platform / 2 && x.r !== Infinity) || s.s - T.platform / 2 < 0 || s.s + T.platform / 2 > s.path.length);
    add({ id: 'T-S2', group: 'Tram', rule: `${line.id} platforms (${T.platform} m) on straight track`, value: `${placed.length - curved.length}/${placed.length}`, limit: 'all', ok: !curved.length, detail: curved.map(s => s.name).join(', ') || undefined });
    const trunk = paths.find(p => p.role === 'running')!, loop = paths.find(p => p.role === 'loop');
    const end = trunk.samples[trunk.samples.length - 1].p, half = T.trackCentres / 2;
    const joins = loop && Math.abs(dist(loop.samples[0].p, end) - half) < .05 && Math.abs(dist(loop.samples[loop.samples.length - 1].p, end) - half) < .05;
    add({ id: 'T-L1', group: 'Tram', rule: `${line.id} turns on a loop (single-ended trams, no crossover)`, value: joins ? 'loop joins both tracks' : 'no loop', limit: 'loop', ok: !!joins });
    const street = (p: V) => d.roads.some(r => lineDist(p, r.line) <= r.w / 2 + .01) || dist(p, plan.circus.c) <= plan.circus.island;
    const off = paths.flatMap(p => p.samples).filter(s => !street(s.p));
    add({ id: 'T-C1', group: 'Tram', rule: `${line.id} runs in street space or the circus island`, value: `${off.length / 2} m off`, limit: '0', ok: !off.length });
    for (const p of paths) for (const r of d.roads.filter(r => r.id !== 'boulevard')) {
      const pl = p.samples.map(s => s.p);
      for (let i = 1; i < pl.length; i++) for (let j = 1; j < r.line.length; j++) { const h = segHit(pl[i - 1], pl[i], r.line[j - 1], r.line[j]); if (h) crossings.push({ line: `${line.id} ${p.id}`, road: r.name, p: h }); }
      // a road whose vertex sits on the track (a junction on the tram's own street) crosses it there
      for (const v of r.line) if (lineDist(v, pl) < .05 && (r.line.length > 1)) crossings.push({ line: `${line.id} ${p.id}`, road: r.name, p: v });
    }
  }

  // ---- sky rail ----
  const S = std.skyrail, stations: any[] = [];
  for (const line of plan.skyrail) {
    const paths = d.sky.filter(p => p.line === line.id);
    for (const role of ['line', 'loop']) {
      const min = Math.min(...paths.filter(p => p.role === role).flatMap(p => p.samples.map(s => s.r)));
      const lim = role === 'line' ? S.radiusLine : S.radiusLoop;
      add({ id: `S-R${role === 'line' ? 1 : 2}`, group: 'Sky rail', rule: `${line.id} ${role === 'line' ? 'line' : 'terminal loop'} radius`, value: min === Infinity ? 'straight' : `R ${f(min)} m`, limit: `≥ ${lim} m`, ok: min >= lim });
    }
    const pyl = d.pylons.filter(p => p.line === line.id), gaps = d.pylonGaps.filter(g => g.line === line.id);
    const over = pyl.filter(p => { const path = paths.find(x => x.id === p.path)!; const lim = Math.min(...path.samples.filter(s => s.s >= p.s - p.span - .01 && s.s <= p.s + .01).map(s => spanLimit(S, s.r))); return p.span > lim + .01; });
    add({ id: 'S-P1', group: 'Sky rail', rule: `${line.id} pylon spans (30 / 23 / 18 m by curvature)`, value: `${pyl.length} pylons, longest ${f(Math.max(...pyl.map(p => p.span)), 1)} m`, limit: 'within limit', ok: !over.length && !gaps.length, detail: [...over.map(p => `span ${f(p.span, 1)} m at ${f(p.s)} m`), ...gaps.map(g => `no allowed ground ${f(g.from)}–${f(g.to)} m on ${g.path}`)].join('; ') || undefined });
    const kinds = new Set(S.pylonZones as string[]);
    const stray = pyl.filter(p => !d.zones.some(z => kinds.has(z.kind) && inZone(p.p, z)));
    add({ id: 'S-P2', group: 'Sky rail', rule: `${line.id} pylons stand in medians, malls, parks, quays, water or the island`, value: `${pyl.length - stray.length}/${pyl.length}`, limit: 'all', ok: !stray.length });
    const placed = (line.stations ?? []).map(s => ({ ...s, ...stopChainage(d, line.id, s, 'sky') })).sort((a, b) => a.s - b.s);
    placed.forEach((s, i) => stations.push({ line: line.id, id: s.id, name: s.name, at: s.at, chainage: s.s, next: placed[i + 1] ? placed[i + 1].s - s.s : undefined, length: s.length }));
    const curved = placed.filter(s => s.path.samples.some(x => Math.abs(x.s - s.s) <= (s.length ?? 50) / 2 && x.r !== Infinity));
    add({ id: 'S-S1', group: 'Sky rail', rule: `${line.id} stations on straight beam`, value: `${placed.length - curved.length}/${placed.length}`, limit: 'all', ok: !curved.length });
    for (const s of placed.filter(s => s.interchange)) {
      const [tl, ts] = s.interchange!.split('/'), stop = plan.tram.find(l => l.id === tl)?.stops?.find(x => x.id === ts);
      const m = stop ? dist(stop.at, s.at) : Infinity;
      add({ id: 'S-I1', group: 'Sky rail', rule: `Interchange ${s.name} ↔ tram ${stop?.name ?? ts}`, value: `${f(m)} m`, limit: `≤ ${S.interchange} m`, ok: m <= S.interchange });
    }
    const pub = (p: V) => d.roads.some(r => lineDist(p, r.line) <= r.w / 2 + .01) || plan.foot.some(w => lineDist(p, w.pts) <= w.w / 2 + .01)
      || d.zones.some(z => ['park', 'quay', 'water'].includes(z.kind) && inZone(p, z)) || dist(p, plan.circus.c) <= plan.circus.island;
    const off = paths.flatMap(p => p.samples).filter(s => !pub(s.p));
    add({ id: 'S-C1', group: 'Sky rail', rule: `${line.id} passes over public space only (no plots)`, value: `${f(off.length / 2)} m over plots`, limit: '0', ok: !off.length });
  }

  // ---- civic ----
  const diffs = ['square', 'office'].map(id => { const p = plan.plots.find(x => x.civic === id)!, r = blockPlanRect(plan.civic, id); return { id, ok: p.rect!.every((v, i) => Math.abs(v - r[i]) < .01), r }; });
  add({ id: 'C-1', group: 'Civic', rule: 'Records Square and Office plots = HandoverSite blocks', value: diffs.map(x => `${x.id} ${x.ok ? '=' : '≠ ' + x.r.join(',')}`).join(', '), limit: 'equal', ok: diffs.every(x => x.ok) });
  const plotOf = (id: string) => d.plots.find(p => p.civic === id)!.polyV;
  const homes: Record<string, string> = { cafe: 'square', 'civic-office': 'office' };
  const out = civ.buildings.filter(b => !b.poly.every(v => inPoly(v, plotOf(homes[b.id] ?? 'square'))));
  add({ id: 'C-2', group: 'Civic', rule: 'Café and office footprints inside their blocks', value: `${civ.buildings.length - out.length}/${civ.buildings.length}`, limit: 'all', ok: !out.length && civ.buildings.length === 2 });
  const lost = civ.points.filter(pt => !d.plots.some(p => inPoly(pt.p, p.polyV)) && !d.roads.some(r => lineDist(pt.p, r.line) <= r.w / 2));
  add({ id: 'C-3', group: 'Civic', rule: 'Mission points land on a plot or a street', value: `${civ.points.length - lost.length}/${civ.points.length}`, limit: 'all', ok: !lost.length, detail: lost.map(p => p.name).join(', ') || undefined });

  // ---- routes and walks ----
  const gateAt = new Map(plan.gates.map(g => [`${g.node}|${g.road}`, g.name]));
  const routes: Route[] = plan.routes.map(r => {
    const { D, trace } = dijkstra(E, r.from), to = r.to.reduce((b, t) => ((D.get(t) ?? Infinity) < (D.get(b) ?? Infinity) ? t : b));
    const path = trace(to), names: string[] = [];
    for (const e of path) { const n = roadById.get(e.road)!.name; if (names[names.length - 1] !== n) names.push(n); }
    const gates = path.flatMap(e => [gateAt.get(`${e.a}|${e.road}`), gateAt.get(`${e.b}|${e.road}`)]).filter(Boolean) as string[];
    const link = plan.links.find(l => l.node === to);
    return { id: r.id, name: r.name, to: link ? `${link.to} (off-sheet)` : r.toName ?? to, length: D.get(to) ?? Infinity, roads: names, gates: [...new Set(gates)] };
  });
  add({ id: 'M-1', group: 'Mission', rule: 'Every mission route reaches its end', value: routes.map(r => `${r.id} ${f(r.length)} m`).join(', '), limit: 'finite', ok: routes.every(r => r.length < Infinity) });
  const walks = footWalks(plan, d, civ), lim = std.walking.directness;
  const worst = walks.reduce((b, w) => (w.m / w.straight > b.m / b.straight ? w : b));
  add({ id: 'W-1', group: 'Walking', rule: 'Escape walks are direct (players cross any road; only buildings and water block)', value: walks.map(w => `${w.from} → ${w.to} ${f(w.m)} m`).join('; '), limit: `walk ÷ straight ≤ ${lim}`, ok: walks.every(w => w.m / w.straight <= lim), detail: `least direct: ${worst.from} → ${worst.to} ×${(worst.m / worst.straight).toFixed(2)}` });
  return { checks: C, routes, crossings, stops, stations, walks };
}
