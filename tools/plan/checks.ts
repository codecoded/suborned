// The plan's rules: roads (Buchanan hierarchy, sealed environmental areas, gates, frontage), tram, sky rail, the civic
// block, plus whatever an overlay adds (tools/plan/overlay.ts). Each check reports a value against its limit.
import { type V, centroid, dist, inPoly, lineDist, nearLine, lineToPoly, segHit } from './geom';
import { type Derived, type Plan, inZone, portalsOf, spanMax } from './plan';
import { offSheet, placements, stopsOf } from './transit';
import { transitChecks } from './transit-checks';
import { type Civic, blockPlanRect, footprints } from './civic';
import { type Walk, walks as footWalks } from './walk';
import { entranceChecks } from './entrances';
import { driveChecks } from './drives';
import { roadFinishChecks } from './road-finish';
import { edges } from './graph';
import type { Overlay, Route } from './overlay';

export type Check = { id: string; group: string; rule: string; value: string; limit: string; ok: boolean; detail?: string; source?: string; waived?: string };
export type Report = { checks: Check[]; routes: Route[]; crossings: { line: string; road: string; p: V }[]; stops: any[]; stations: any[]; walks: Walk[] };

const f = (x: number, d = 0) => x.toFixed(d);
/** `strict` ignores `standards.transit.waived` (rule id → reason), which lets a draft plan pass `bun run plan` while generation runs never use it. */
/** Points just inside a polygon (0.3 m along the inward edge normals): each vertex on the bisector of its two edges, each edge's midpoint. Right for L-shapes, where nudging toward the centroid lands outside. */
export function inset(P: V[], d = .3): V[] {
  const n = P.length, area = P.reduce((a, v, i) => a + (v[0] * P[(i + 1) % n][1] - P[(i + 1) % n][0] * v[1]), 0), sg = area >= 0 ? 1 : -1;
  const nor = (a: V, b: V): V => { const l = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1; return [-sg * (b[1] - a[1]) / l, sg * (b[0] - a[0]) / l]; };
  const out: V[] = [];
  for (let i = 0; i < n; i++) {
    const a = P[(i + n - 1) % n], v = P[i], b = P[(i + 1) % n], n1 = nor(a, v), n2 = nor(v, b);
    const bx = n1[0] + n2[0], by = n1[1] + n2[1], k = Math.hypot(bx, by) || 1;
    out.push([v[0] + bx / k * d, v[1] + by / k * d], [(v[0] + b[0]) / 2 + n2[0] * d, (v[1] + b[1]) / 2 + n2[1] * d]);
  }
  return out;
}

export function check(plan: Plan, d: Derived, civic: Civic, strict = false, overlay?: Overlay): Report {
  const civ: Civic = overlay ? { ...civic, walkFrom: overlay.origins(plan, civic) } : civic;
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
    const A = built[i].polyV, B = built[j].polyV;
    if (inset(A).some(v => inPoly(v, B)) || inset(B).some(v => inPoly(v, A))) clash.push(`${built[i].id}×${built[j].id}`);
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
    const { items: placed, gaps: all } = placements(plan, d, line, 'tram'), gaps = all.filter((g): g is number => g !== undefined);
    placed.forEach((s, i) => stops.push({ line: line.id, id: s.id, name: s.name, at: s.at, chainage: s.s, next: all[i] }));
    const [lo, hi] = T.stopSpacing;
    add({ id: 'T-S1', group: 'Tram', rule: `${line.id} stop spacing`, value: gaps.map(g => f(g)).join(', ') + ' m', limit: `${lo}–${hi} m`, ok: placed.length >= 2 && gaps.every(g => g >= lo && g <= hi), detail: placed.length < 2 ? `needs at least 2 stops, has ${placed.length}` : undefined });
    const curved = placed.filter(s => s.path.samples.some(x => Math.abs(x.s - s.local) <= T.platform / 2 && x.r !== Infinity) || s.local - T.platform / 2 < 0 || s.local + T.platform / 2 > s.path.length);
    add({ id: 'T-S2', group: 'Tram', rule: `${line.id} platforms (${T.platform} m) on straight track`, value: `${placed.length - curved.length}/${placed.length}`, limit: 'all', ok: !curved.length, detail: curved.map(s => s.name).join(', ') || undefined });
    // every running end inside the sheet (not off-sheet, not joined to another running path) turns on a terminal loop
    const half = T.trackCentres / 2, running = paths.filter(p => p.role === 'running'), loops = paths.filter(p => p.role === 'loop');
    const tails = running.flatMap(p => [p.samples[0].p, p.samples[p.samples.length - 1].p].filter(e => !offSheet(plan, e) && !running.some(o => o !== p && o.samples.some(x => dist(x.p, e) <= half + .05))));
    const open = tails.filter(e => !loops.some(l => Math.abs(dist(l.samples[0].p, e) - half) < .05 && Math.abs(dist(l.samples[l.samples.length - 1].p, e) - half) < .05));
    add({ id: 'T-L1', group: 'Tram', rule: `${line.id} turns on a loop at every end inside the sheet (single-ended trams, no crossover)`, value: tails.length ? `${tails.length - open.length}/${tails.length} ends loop` : 'all ends off-sheet', limit: 'all', ok: !open.length, detail: open.map(e => `no loop at (${e.map(v => v.toFixed(0))})`).join('; ') || undefined });
    const street = (p: V) => d.roads.some(r => nearLine(p, r.line, r.w / 2 + .01)) || dist(p, plan.circus.c) <= plan.circus.island;
    const off = paths.flatMap(p => p.samples).filter(s => !street(s.p));
    add({ id: 'T-C1', group: 'Tram', rule: `${line.id} runs in street space or the circus island`, value: `${off.length / 2} m off`, limit: '0', ok: !off.length });
    for (const p of paths) for (const r of d.roads.filter(r => r.id !== 'boulevard')) {
      const pl = p.samples.map(s => s.p);
      const apart = (A: V[], B: V[]) => Math.max(...A.map(v => v[0])) < Math.min(...B.map(v => v[0])) || Math.max(...B.map(v => v[0])) < Math.min(...A.map(v => v[0])) || Math.max(...A.map(v => v[1])) < Math.min(...B.map(v => v[1])) || Math.max(...B.map(v => v[1])) < Math.min(...A.map(v => v[1]));
      const rx0 = Math.min(...r.line.map(v => v[0])), rx1 = Math.max(...r.line.map(v => v[0])), ry0 = Math.min(...r.line.map(v => v[1])), ry1 = Math.max(...r.line.map(v => v[1]));
      if (!apart(pl, r.line)) for (let i = 1; i < pl.length; i++) {
        const lx = Math.min(pl[i - 1][0], pl[i][0]), hx = Math.max(pl[i - 1][0], pl[i][0]), ly = Math.min(pl[i - 1][1], pl[i][1]), hy = Math.max(pl[i - 1][1], pl[i][1]);
        if (hx < rx0 || rx1 < lx || hy < ry0 || ry1 < ly) continue;
        for (let j = 1; j < r.line.length; j++) {
          const c = r.line[j - 1], e = r.line[j];
          if (hx < Math.min(c[0], e[0]) || Math.max(c[0], e[0]) < lx || hy < Math.min(c[1], e[1]) || Math.max(c[1], e[1]) < ly) continue;
          const h = segHit(pl[i - 1], pl[i], c, e); if (h) crossings.push({ line: `${line.id} ${p.id}`, road: r.name, p: h });
        }
      }
      // a road whose vertex sits on the track (a junction on the tram's own street) crosses it there
      for (const v of r.line) if (nearLine(v, pl, .05, true) && (r.line.length > 1)) crossings.push({ line: `${line.id} ${p.id}`, road: r.name, p: v });
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
    const over = pyl.filter(p => { const path = paths.find(x => x.id === p.path)!; return p.span > spanMax(S, plan.landmarks, path, p.s - p.span, p.s) + .01; });
    add({ id: 'S-P1', group: 'Sky rail', rule: `${line.id} pylon spans (30 / 23 / 18 m by curvature${S.pylonClear?.span ? `; ${S.pylonClear.span} m over a landmark disc` : ''})`, value: `${pyl.length} pylons, longest ${f(Math.max(...pyl.map(p => p.span)), 1)} m`, limit: 'within limit', ok: !over.length && !gaps.length, detail: [...over.map(p => `span ${f(p.span, 1)} m at ${f(p.s)} m`), ...gaps.map(g => `no allowed ground ${f(g.from)}–${f(g.to)} m on ${g.path}`)].join('; ') || undefined });
    const kinds = new Set(S.pylonZones as string[]);
    const stray = pyl.filter(p => !d.zones.some(z => kinds.has(z.kind) && inZone(p.p, z)));
    add({ id: 'S-P2', group: 'Sky rail', rule: `${line.id} pylons stand in medians, malls, parks, quays, water or the island`, value: `${pyl.length - stray.length}/${pyl.length}`, limit: 'all', ok: !stray.length });
    const { items: placed, gaps: sgaps } = placements(plan, d, line, 'sky');
    placed.forEach((s, i) => stations.push({ line: line.id, id: s.id, name: s.name, at: s.at, chainage: s.s, next: sgaps[i], length: s.length }));
    const curved = placed.filter(s => s.path.samples.some(x => Math.abs(x.s - s.local) <= (s.length ?? 50) / 2 && x.r !== Infinity));
    add({ id: 'S-S1', group: 'Sky rail', rule: `${line.id} stations on straight beam`, value: `${placed.length - curved.length}/${placed.length}`, limit: 'all', ok: !curved.length });
    const pub = (p: V) => d.roads.some(r => nearLine(p, r.line, r.w / 2 + .01)) || plan.foot.some(w => nearLine(p, w.pts, w.w / 2 + .01))
      || d.zones.some(z => ['park', 'quay', 'water'].includes(z.kind) && inZone(p, z)) || dist(p, plan.circus.c) <= plan.circus.island;
    // brief 007: a beam may also pass over a plot through a declared portal (signatures[].portal, megastructures[].portals, supports.portal), where the building carries it (P-1)
    const portals = portalsOf(plan), inPortal = (p: V) => portals.some(q => p[0] >= q.rect[0] && p[0] <= q.rect[1] && Math.abs(p[1]) <= q.rect[3] && p[1] >= q.rect[2]);
    const off = paths.flatMap(p => p.samples).filter(s => !pub(s.p) && !inPortal(s.p));
    add({ id: 'S-C1', group: 'Sky rail', rule: `${line.id} passes over public space only (no plots)`, value: `${f(off.length / 2)} m over plots`, limit: '0', ok: !off.length });
  }

  C.push(...transitChecks(plan, d));

  // ---- civic ----
  // C-0 holds the civic block to its Oct 9 city scale (plan.civic.scale): the café and office regressed to an older,
  // smaller block twice when a stale file was copied in; this makes that a failed check instead of a silent change.
  const sc = plan.civic.scale, bb = (p: V[]) => [Math.max(...p.map(v => v[0])) - Math.min(...p.map(v => v[0])), Math.max(...p.map(v => v[1])) - Math.min(...p.map(v => v[1]))].sort((a, b) => b - a);
  const cafe = civ.buildings.find(b => b.id === 'cafe'), office = civ.buildings.find(b => b.id === 'civic-office');
  const near = (a: number, b: number) => Math.abs(a - b) < .01;
  const [cw, cd] = cafe ? bb(cafe.poly) : [0, 0];
  const scaleOk = !!cafe && !!office && near(cw, sc.cafe.width) && near(cd, sc.cafe.depth) && near(cafe.height, sc.cafe.height)
    && near(office.heights?.wings, sc['civic-office'].wings) && near(office.heights?.crown, sc['civic-office'].crown);
  add({ id: 'C-0', group: 'Civic', rule: 'Civic block at its city scale (café footprint and height, office wings and crown)', value: cafe ? `café ${f(cw, 1)} × ${f(cd, 1)} × ${cafe.height} m, office ${office?.heights?.wings ?? '?'} / ${office?.heights?.crown ?? '?'} m` : 'no café', limit: `café ${sc.cafe.width} × ${sc.cafe.depth} × ${sc.cafe.height} m, office ${sc['civic-office'].wings} / ${sc['civic-office'].crown} m`, ok: scaleOk });
  const diffs = ['square', 'office'].map(id => { const p = plan.plots.find(x => x.civic === id)!, r = blockPlanRect(plan.civic, id); return { id, ok: p.rect!.every((v, i) => Math.abs(v - r[i]) < .01), r }; });
  add({ id: 'C-1', group: 'Civic', rule: 'Records Square and Office plots = HandoverSite blocks', value: diffs.map(x => `${x.id} ${x.ok ? '=' : '≠ ' + x.r.join(',')}`).join(', '), limit: 'equal', ok: diffs.every(x => x.ok) });
  // SEC-1: the response dial's base level (GDD §05; briefs/013/motive-takedowns.md §5.3) is plan data. An area that sets securityBase gives an integer level 0..4 and says why.
  // That the area the Handover stands in sets one is held where it is read: tools/sim/site.ts refuses to write the sim data without it (tools/sim/sim.test.ts), so a pinned older plan stays checkable.
  const lvl = (n: unknown) => Number.isInteger(n) && (n as number) >= 0 && (n as number) <= 4;
  const secBad = plan.areas.flatMap(a => a.securityBase === undefined ? [] : [...(lvl(a.securityBase) ? [] : [`${a.id}: securityBase ${JSON.stringify(a.securityBase)} is not a level 0..4`]), ...(a.securityWhy ? [] : [`${a.id}: securityBase has no securityWhy`])]);
  add({ id: 'SEC-1', group: 'Security level', rule: 'Each area that sets a security base gives a level 0..4 and a reason', value: plan.areas.filter(a => a.securityBase !== undefined).map(a => `${a.id} ${a.securityBase}`).join(', ') || 'none set', limit: 'level 0..4, a reason', ok: !secBad.length, ...(secBad.length ? { detail: secBad.join('; ') } : {}), source: 'GDD v0.23 §05 response dial · briefs/013/motive-takedowns.md §5.3' });
  const plotOf = (id: string) => d.plots.find(p => p.civic === id)!.polyV;
  const homes: Record<string, string> = { cafe: 'square', 'civic-office': 'office' };
  const out = civ.buildings.filter(b => !b.poly.every(v => inPoly(v, plotOf(homes[b.id] ?? 'square'))));
  add({ id: 'C-2', group: 'Civic', rule: 'Café and office footprints inside their blocks', value: `${civ.buildings.length - out.length}/${civ.buildings.length}`, limit: 'all', ok: !out.length && civ.buildings.length === 2 });
  C.push(...driveChecks(plan, d, civ));
  const ctx = { plan, d, civ, E, roadById };
  C.push(...(overlay?.civic(ctx) ?? []));

  // ---- routes and walks ----
  const { checks: routeChecks, routes } = overlay?.routes(ctx) ?? { checks: [], routes: [] as Route[] };
  C.push(...routeChecks);
  const walks = footWalks(plan, d, civ), lim = std.walking.directness;
  const worst = walks.reduce((b, w) => (w.m / w.straight > b.m / b.straight ? w : b));
  add({ id: 'W-1', group: 'Walking', rule: 'Escape walks are direct (players cross any road; only buildings and water block)', value: walks.map(w => `${w.from} → ${w.to} ${f(w.m)} m`).join('; '), limit: `walk ÷ straight ≤ ${lim}`, ok: walks.every(w => w.m / w.straight <= lim), detail: `least direct: ${worst.from} → ${worst.to} ×${(worst.m / worst.straight).toFixed(2)}` });
  const ring = (poly: V[]) => [...poly, poly[0]!], DOOR = 1; // a door stands within 1 m of its building's footprint edge: the wing's or a tower's
  const stray = civ.walkFrom.filter(o => !footprints(civ).some(f => lineDist(o.p, ring(f)) <= DOOR));
  add({ id: 'W-4', group: 'Walking', rule: 'Every walk starts at a building entrance (doors are world)', value: `${civ.walkFrom.length - stray.length}/${civ.walkFrom.length}`, limit: `all, within ${DOOR} m of a building edge`, ok: !stray.length && civ.walkFrom.length > 0, detail: stray.map(o => o.name).join(', ') || undefined, source: 'plan.standards.walking · GDD §01 exteriors only: the player starts at a door' });
  C.push(...entranceChecks(plan, d, civ));
  C.push(...roadFinishChecks(plan)); // RF-1, RF-2: opt-in on standards.streetscape.roads / .inlays (brief 026/028)
  const waived = (std.transit?.waived ?? {}) as Record<string, string>;
  if (!strict) for (const c of C) if (!c.ok && waived[c.id]) { c.ok = true; c.waived = waived[c.id]; c.detail = `WAIVED (${waived[c.id]})${c.detail ? '; ' + c.detail : ''}`; }
  return { checks: C, routes, crossings, stops, stations, walks };
}
