// Network rules for any number of tram and sky rail lines (brief 001, "A · Splice"): paths in order, stops on their line
// and in public space, no dead ends, every plot in reach of a stop, one connected network, sky rail station spacing.
import type { Check } from './checks';
import { centroid, dist, inPoly, lineDist, type V } from './geom';
import type { Derived, Plan } from './plan';
import { joinTol, modeOf, offSheet, placements, stopsOf, ends } from './transit';

const SRC = 'brief 001 · ';
const f = (x: number, d = 0) => x.toFixed(d);
const fmt = (p: V) => `(${f(p[0])}, ${f(p[1])})`;

export function transitChecks(plan: Plan, d: Derived): Check[] {
  const C: Check[] = [], std = plan.standards, T = std.transit ?? {}, add = (c: Omit<Check, 'group'>) => C.push({ group: 'Transit', ...c });
  const lines = modeOf(plan).flatMap(([mode, ls]) => ls.map(line => ({ mode, line, ...placements(plan, d, line, mode) })));
  const all = lines.flatMap(l => stopsOf(l.line).map(s => ({ ...s, lineId: l.line.id, mode: l.mode })));

  // order: valid chain of the line's own paths; required when stops sit on several paths
  const orderBad = lines.flatMap(({ mode, line, chain: c }) => {
    const ids = line.order ?? line.paths.map(p => p.id), plain = ids.map(i => i.replace(/^-/, '')), tol = joinTol(plan, mode), out: string[] = [];
    if (c.unknown.length) out.push(`${line.id}: order names unknown path ${c.unknown.join(', ')}`);
    if (new Set(plain).size !== plain.length || (line.order && line.paths.some(p => !plain.includes(p.id)))) out.push(`${line.id}: order must list every path once`);
    if (line.order) c.links.slice(1).forEach((l, i) => { const gap = dist(ends(c.links[i]!)[1], ends(l)[0]); if (gap > tol) out.push(`${line.id}: ${c.links[i]!.id} → ${l.id} do not join (${f(gap, 1)} m)`); });
    else if (new Set(stopsOf(line).map(s => s.path)).size > 1) out.push(`${line.id}: stops on several paths need an order`);
    return out;
  });
  add({ id: 'N-O1', rule: 'Each line has an order of its paths that chains end to end', value: `${orderBad.length} faults`, limit: '0', ok: !orderBad.length, detail: orderBad.join('; ') || undefined, source: SRC + 'a line `order` so stop spacing reads across paths' });

  // stops: on a path of their line, within the line's tolerance, never on a plot
  const ids = lines.map(l => l.line.id), dup = ids.filter((x, i) => ids.indexOf(x) !== i);
  const stray = lines.flatMap(l => stopsOf(l.line).flatMap(s => {
    const p = l.items.find(x => x.id === s.id);
    return !p ? [`${l.line.id}/${s.id} names no path of its line`] : p.off > (T.onLine ?? 3) ? [`${l.line.id}/${s.id} is ${f(p.off, 1)} m off its path`] : [];
  }));
  add({ id: 'N-P1', rule: 'Every stop and station sits on a path of its own line', value: `${stray.length + dup.length} faults`, limit: '0', ok: !stray.length && !dup.length, detail: [...stray, ...dup.map(x => `duplicate line id ${x}`)].join('; ') || undefined, source: SRC + 'stops and stations: positions on the lines' });
  const onPlot = all.filter(s => d.plots.some(p => inPoly(s.at, p.polyV)));
  add({ id: 'N-P2', rule: 'No stop or station stands on a plot (entrances front public space)', value: `${onPlot.length}`, limit: '0', ok: !onPlot.length, detail: onPlot.map(s => `${s.lineId}/${s.id}`).join(', ') || undefined, source: SRC + 'entrances fronting public space; plan.standards.roads.frontage' });

  // dead ends: a path end joins another path of its line, is off-sheet, or the path closes on itself (a real loop, longer than 4 joins).
  // A path lying wholly along other paths of its line is a stub, not a branch.
  const off = (p: V) => offSheet(plan, p), dead: string[] = [];
  for (const { mode, line } of lines) {
    const paths = d[mode].filter(p => p.line === line.id), tol = joinTol(plan, mode);
    for (const p of paths) {
      const a = p.samples[0]!.p, b = p.samples.at(-1)!.p, others = paths.filter(o => o !== p);
      if (others.length && p.samples.every(x => others.some(o => lineDist(x.p, o.samples.map(y => y.p)) <= tol))) { dead.push(`${line.id} ${p.id} lies along the line's other paths (a stub)`); continue; }
      if (dist(a, b) <= tol && p.length > 4 * tol) continue;
      for (const e of [a, b]) if (!off(e) && !others.some(o => o.samples.some(x => dist(x.p, e) <= tol))) dead.push(`${line.id} ${p.id} ends at ${fmt(e)}`);
    }
  }
  add({ id: 'N-D1', rule: 'No dead ends: every path end joins another path of its line or leaves the sheet; closed loops (longer than 4 beam centres) are fine', value: `${dead.length} dead ends`, limit: '0', ok: !dead.length, detail: dead.join('; ') || undefined, source: SRC + 'no dead ends' });

  // reach: every plot centroid within standards.transit.reach of a stop or station
  const reach = T.reach as number | undefined, near = d.plots.map(p => ({ id: p.id, m: Math.min(Infinity, ...all.map(s => dist(centroid(p.polyV), s.at))) })).sort((a, b) => b.m - a.m);
  const far = near.filter(n => n.m > (reach ?? Infinity));
  add({ id: 'N-R1', rule: 'Every area (plot centroid) is within standards.transit.reach of a tram stop or sky rail station', value: near[0] && near[0].m < Infinity ? `worst ${near[0].id} ${f(near[0].m)} m` : 'no stops', limit: `≤ ${reach} m`, ok: reach !== undefined && !far.length, detail: far.length ? `out of reach: ${far.slice(0, 8).map(n => `${n.id} ${f(n.m)} m`).join(', ')}${far.length > 8 ? ` (+${far.length - 8})` : ''}` : undefined, source: 'plan.standards.transit.reach · brief 001 · every plot centroid within 250 m of a stop or station (settled)' });

  // interchanges: the named partner exists on another line, within standards.skyrail.interchange; a partner that names an interchange must name this stop back
  const lim = std.skyrail.interchange as number, good = new Set<string>(), tied: string[] = [];
  for (const s of all.filter(x => x.interchange)) {
    const [tl, ts] = s.interchange!.split('/'), other = lines.find(l => l.line.id === tl && l.line.id !== s.lineId), stop = other && stopsOf(other.line).find(x => x.id === ts);
    const m = stop ? dist(stop.at, s.at) : Infinity, back = stop?.interchange, recip = !back || back === `${s.lineId}/${s.id}`;
    const ok = !!stop && m <= lim && recip;
    if (ok) good.add(`${s.lineId}|${tl}`);
    add({ id: 'S-I1', rule: `Interchange ${s.lineId}/${s.id} ↔ ${other ? other.mode === 'tram' ? 'tram' : 'sky rail' : 'no line'} ${tl}/${ts}`, value: stop ? `${f(m)} m` : 'no such stop', limit: `≤ ${lim} m, partner names no other stop`, ok, detail: !stop ? `${s.interchange} does not exist on another line` : !recip ? `${tl}/${ts} names ${back}, not ${s.lineId}/${s.id}` : undefined, source: SRC + 'interchange pairs within plan.standards.skyrail.interchange' });
    if (!ok) tied.push(`${s.lineId}/${s.id}`);
  }

  // connected: lines are nodes; a valid interchange or a stop pair within standards.skyrail.interchange joins two
  const parent = new Map(ids.map(i => [i, i])), root = (i: string): string => (parent.get(i) === i ? i : root(parent.get(i)!));
  const join = (a: string, b: string) => { if (parent.has(a) && parent.has(b)) parent.set(root(a), root(b)); };
  for (const g of good) { const [a, b] = g.split('|'); join(a!, b!); }
  for (const a of all) for (const b of all) if (a.lineId < b.lineId && dist(a.at, b.at) <= lim) join(a.lineId, b.lineId);
  const groups = new Set(ids.map(root));
  add({ id: 'N-C1', rule: 'The network is connected: every line reaches every other through interchanges', value: `${groups.size} network${groups.size === 1 ? '' : 's'}, ${ids.length} lines`, limit: '1', ok: groups.size <= 1, detail: groups.size > 1 ? [...groups].map(g => ids.filter(i => root(i) === g).join('+')).join(' | ') + (tied.length ? `; interchange not valid: ${tied.join(', ')}` : '') : undefined, source: SRC + 'network connected through interchanges; plan.standards.skyrail.interchange' });

  // sky rail station spacing, along the ordered paths
  const [lo, hi] = std.skyrail.stationSpacing ?? [200, 500];
  for (const l of lines.filter(x => x.mode === 'sky')) {
    const gaps = l.gaps.filter((g): g is number => g !== undefined);
    add({ id: 'S-S2', rule: `${l.line.id} station spacing`, value: gaps.map(g => f(g)).join(', ') + ' m', limit: `${lo}–${hi} m`, ok: l.items.length >= 2 && gaps.every(g => g >= lo && g <= hi), detail: l.items.length < 2 ? `needs at least 2 stations, has ${l.items.length}` : undefined, source: SRC + 'sky rail station spacing 200–500 m; plan.standards.skyrail.stationSpacing' });
  }
  return C;
}
