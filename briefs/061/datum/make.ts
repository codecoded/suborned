// Brief 061 · far fewer crossings (Datum, 2026-10-24): the proposed coverage rules applied to the crossings the world exports today, the desire-line detours before and
// after, and the pictures. Not built: the generator does not read these rules until Sky approves. Reads the plan, the export (out/world/harbour-district.world.json:
// Splice is changing the generator, so nothing is generated here) and the walks of walks.ts (out/scratch/datum/061/walks.json). Writes counts.json next to this file
// and briefs/061/crossings-district.png and briefs/061/crossings-civic.png (before | after), rendered by rsvg-convert.
// usage (repo root): bun briefs/061/datum/walks.ts && bun briefs/061/datum/make.ts
//
// Every selection below is a rule over the plan and the export, never a list of ids, so the generator can apply the same rule:
//   stops      one crossing per tram stop: the platform end nearer a junction with a district or access road, or a footway's end (the end people walk in from)
//   island     one crossing per road into Crown Circus: its left footway line (seen leaving the circus)
//   foot       a mall or walk that meets or crosses a road keeps one crossing of it, the one nearest its axis (a tie: the one further east or north)
//   crossroads lanes meeting a district road from both sides at one node: one crossing of the district road, on the arm before the node (lower chainage)
//   junction   two district roads meet: one crossing of each, on its arm before the node, unless a tram stop's crossing of that road stands within 30 m
//   civic      the civic block's own crossing on the Records Office axis (Vantage's dressing; the other civic ones go to Vantage as a proposal)
// Gone: crossings at both ends of every plot edge (district and access), primary arms, the mid-block rule, the stair-foot patches of the station banks, a tram stop's second end.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { type V, dist, segDist } from '../../../tools/plan/geom';
import { derive, type Plan } from '../../../tools/plan/plan';
import { civic } from '../../../tools/plan/civic';
import { district } from '../../../tools/plan/draw';
import { check } from '../../../tools/plan/checks';
import { CSS } from '../../../tools/plan/sheet';

const root = resolve(import.meta.dir, '../../..'), here = import.meta.dir, pics = resolve(here, '..');
const json = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = json('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, json(plan.civic.block));
const w: any = json('out/world/harbour-district.world.json'), W: any = json('out/scratch/datum/061/walks.json');
const SERVED = 30, CLUSTER = 20;

// ---- the crossings today, by the rule that put them there
const cls = new Map<string, string>(plan.roads.map((r: any) => [r.id, r.class]));
const lineOf = new Map<string, V[]>(w.ground.roads.map((r: any) => [r.id.replace(/^road\//, ''), r.line as V[]])), halfOf = new Map<string, number>(w.ground.roads.map((r: any) => [r.id.replace(/^road\//, ''), r.width / 2]));
const lineDist = (p: V, line: V[]) => { let m = Infinity; for (let i = 1; i < line.length; i++) m = Math.min(m, segDist(p, line[i - 1]!, line[i]!)); return m; };
const chain = (line: V[], p: V) => { let acc = 0, best = { s: 0, d: Infinity }; for (let i = 1; i < line.length; i++) { const a = line[i - 1]!, b = line[i]!, l = dist(a, b), u: V = [(b[0] - a[0]) / l, (b[1] - a[1]) / l], t = Math.max(0, Math.min(l, (p[0] - a[0]) * u[0] + (p[1] - a[1]) * u[1])), q: V = [a[0] + u[0] * t, a[1] + u[1] * t], dd = dist(p, q); if (dd < best.d) best = { s: acc + t, d: dd }; acc += l; } return best.s; };
const cen = (l: V[]): V => [(l[0]![0] + l[l.length - 1]![0]) / 2, (l[0]![1] + l[l.length - 1]![1]) / 2];
type X = { id: string; kind: string; road: string; cls: string; rule: string; line: V[]; mid: V; lids: string };
const now: X[] = w.crossings.map((c: any) => {
  const lids = c.landings.map((l: any) => l.id).join('|');
  const rule = c.kind !== 'kerb' ? c.kind : lids.match(/street\/(arm|island|mall|mid)\//)?.[1] ?? (/crossing-x-/.test(lids) ? 'civic' : 'edge');
  return { id: c.id, kind: c.kind, road: c.road, cls: cls.get(c.road) ?? 'none', rule, line: c.line, mid: cen(c.line), lids };
});

// ---- the proposed rules
const nodeAt = (n: string) => plan.nodes[n] as V;
const roadsAt = (n: string) => plan.roads.filter((r: any) => r.path.includes(n));
const junctions = Object.keys(plan.nodes).filter(n => roadsAt(n).length >= 2 && !n.startsWith('off_') && roadsAt(n).some((r: any) => r.class !== 'primary')).map(nodeAt); // where a district or access road comes in: two primaries meeting at the district's corner bring no one
const footEnds = plan.foot.flatMap((f: any) => [f.pts[0], f.pts[f.pts.length - 1]] as V[]);
const keep = new Map<string, string>(); // crossing id → rule that keeps it
// stops
const stopGroups = new Map<string, X[]>();
for (const x of now.filter(x => x.kind === 'stop')) { const g = x.id.split('/').slice(0, 4).join('/'); (stopGroups.get(g) ?? stopGroups.set(g, []).get(g)!).push(x); }
for (const xs of stopGroups.values()) {
  const near = (x: X) => Math.min(...[...junctions, ...footEnds].map(p => dist(p, x.mid)));
  keep.set([...xs].sort((a, b) => near(a) - near(b))[0]!.id, 'stops');
}
// island
const isl = now.filter(x => x.rule === 'island'), byMouth = new Map<string, X[]>();
for (const x of isl) { const m = x.lids.match(/street\/island\/([^/]+)\/([lr])\//)!; (byMouth.get(m[1]!) ?? byMouth.set(m[1]!, []).get(m[1]!)!).push(x); }
for (const xs of byMouth.values()) keep.set((xs.find(x => /\/l\/a/.test(x.lids)) ?? xs[0]!).id, 'island');
// foot: a mall or walk meeting or crossing a road
const kerbX = now.filter(x => x.kind === 'kerb' && x.rule !== 'island');
for (const f of plan.foot as any[]) {
  const axis = f.pts as V[], half = f.w / 2;
  for (const road of plan.roads.filter((r: any) => !r.gyratory)) {
    const line = lineOf.get(road.id)!, h = halfOf.get(road.id)!;
    const meets = line.some((p, i) => i > 0 && axis.some((q, j) => j > 0 && segDist(p, axis[j - 1]!, q) <= h + 0.6)); if (!meets) continue;
    const cands = kerbX.filter(x => x.road === road.id && lineDist(x.mid, axis) <= half + 12).sort((a, b) => lineDist(a.mid, axis) - lineDist(b.mid, axis) || (b.mid[0] + b.mid[1]) - (a.mid[0] + a.mid[1]));
    if (cands[0]) keep.set(cands[0].id, keep.get(cands[0].id) ?? 'foot');
  }
}
// crossroads and district junctions
for (const r of plan.roads.filter((r: any) => r.class === 'district' && !r.gyratory) as any[]) {
  const line = lineOf.get(r.id)!;
  for (const n of r.path as string[]) {
    const others = roadsAt(n).filter((q: any) => q.id !== r.id), lanes = others.filter((q: any) => q.class === 'access'), dist2 = others.filter((q: any) => q.class === 'district' && !q.gyratory);
    const rule = lanes.length >= 2 ? 'crossroads' : dist2.length ? 'junction' : ''; if (!rule) continue;
    const s0 = chain(line, nodeAt(n));
    if (rule === 'junction' && now.some(x => x.kind === 'stop' && keep.has(x.id) && x.road === r.id && Math.abs(chain(line, x.mid) - s0) <= 30)) continue;
    const before = kerbX.filter(x => x.road === r.id && chain(line, x.mid) < s0 && s0 - chain(line, x.mid) <= 40).sort((a, b) => chain(line, b.mid) - chain(line, a.mid))[0];
    if (before) keep.set(before.id, keep.get(before.id) ?? rule);
  }
}
// civic: the block's own crossing on the Office axis
for (const x of now.filter(x => /crossing-x-orbit-axis/.test(x.lids))) keep.set(x.id, 'civic');
const after = now.filter(x => keep.has(x.id));

// ---- desire-line detours
const circ = w.ground.circuses[0];
const serves = (x: X, road: string) => road === 'crown-circus' ? Math.abs(dist(x.mid, circ.centre) - (circ.radius + circ.islandRadius) / 2) <= (circ.radius - circ.islandRadius) / 2 + 0.5 : x.kind !== 'bank' && lineDist(x.mid, lineOf.get(road)!) <= halfOf.get(road)! + 0.5;
const detour = (e: any, x: X) => { const p = x.line[0]!, q = x.line[x.line.length - 1]!; return Math.min(dist(e.a, p) + dist(p, q) + dist(q, e.b), dist(e.a, q) + dist(q, p) + dist(p, e.b)) - dist(e.a, e.b); };
const best = (e: any, xs: X[]) => xs.filter(x => serves(x, e.road)).map(x => ({ id: x.id, m: detour(e, x) })).sort((a, b) => a.m - b.m)[0];
const evs: any[] = W.events, clusters: { road: string; at: V; ev: any[] }[] = [];
for (const e of evs) { const c = clusters.find(c => c.road === e.road && dist(c.at, e.at) < CLUSTER); if (c) { c.ev.push(e); c.at = [(c.at[0] * (c.ev.length - 1) + e.at[0]) / c.ev.length, (c.at[1] * (c.ev.length - 1) + e.at[1]) / c.ev.length]; } else clusters.push({ road: e.road, at: e.at, ev: [e] }); }
const median = (xs: number[]) => { const s = [...xs].sort((a, b) => a - b); return s[Math.floor(s.length / 2)]!; };
const judge = (xs: X[]) => {
  const per = evs.map(e => best(e, xs)?.m ?? Infinity), cl = clusters.map(c => median(c.ev.map(e => best(e, xs)?.m ?? Infinity)));
  const used = new Set(evs.map(e => best(e, xs)).filter(b => b && b.m <= SERVED).map(b => b!.id));
  return { crossings: xs.length, served: per.filter(m => m <= SERVED).length, of: per.length, share: Math.round((100 * per.filter(m => m <= SERVED).length) / per.length), medianDetour: Math.round(median(per.filter(isFinite))), unused: xs.filter(x => !used.has(x.id)).length, per, cl };
};
const A = judge(now), B = judge(after);
const klass = (road: string) => road === 'crown-circus' ? 'circus' : cls.get(road)!;
const byClass = (pick: (i: number) => boolean) => { const o: Record<string, number> = {}; evs.forEach((e, i) => { if (pick(i)) o[klass(e.road)] = (o[klass(e.road)] ?? 0) + 1; }); return o; };
const servedBy = { before: byClass(i => A.per[i]! <= SERVED), after: byClass(i => B.per[i]! <= SERVED), all: byClass(() => true) };
const name = (id: string) => plan.roads.find((r: any) => r.id === id)?.name ?? (id === 'crown-circus' ? 'Crown Circus' : id);
// a lane's mouth: where an access road ends on a district or primary road (a gate). A walk along the bigger road's pavement crosses the mouth there.
const mouthAt = (road: string, at: V) => { const r = plan.roads.find((q: any) => q.id === road) as any; if (!r || r.class !== 'access') return false; return [r.path[0], r.path[r.path.length - 1]].some((n: string) => roadsAt(n).some((q: any) => q.class !== 'access') && dist(nodeAt(n), at) <= 25); };
const groupOf = (c: { road: string; at: V }, after: number) => klass(c.road) === 'access' ? (mouthAt(c.road, c.at) ? 'lane mouth' : 'lane') : after <= 60 ? 'busy road, ≤ 60 m' : 'busy road, > 60 m';
const lost = clusters.map((c, i) => ({ road: c.road, name: name(c.road), cls: klass(c.road), group: groupOf(c, B.cl[i]!), at: c.at.map(v => Math.round(v)) as V, walks: c.ev.length, before: A.cl[i]!, after: B.cl[i]!, cost: c.ev.length * ((isFinite(B.cl[i]!) ? B.cl[i]! : 400) - A.cl[i]!) }))
  .filter(c => c.before <= SERVED && c.after > SERVED).sort((a, b) => b.cost - a.cost);
// what the walks are, for each lost place: the most common pair of ends
const ends = (c: { road: string; at: V }) => { const k = new Map<string, number>(); for (const e of evs.filter(e => e.road === c.road && dist(e.at, c.at) < CLUSTER)) { const wk = W.walks[e.walk]; const t = [wk.from, wk.to].map((s: string) => s.replace(/^stop\//, '').replace(/^bank\/S1\//, 'S1 ').replace(/\/[lr]$/, '')).join(' – '); k.set(t, (k.get(t) ?? 0) + 1); } return [...k.entries()].sort((a, b) => b[1] - a[1]).slice(0, 2).map(([t, n]) => `${t} ×${n}`); };

// ---- counts
const tally = (xs: X[], key: (x: X) => string) => xs.reduce((o, x) => { const k = key(x); o[k] = (o[k] ?? 0) + 1; return o; }, {} as Record<string, number>);
const roadClass = (x: X) => x.kind === 'bank' ? (x.road ? `${x.cls} (stair foot)` : 'none (stair foot)') : x.rule === 'island' ? 'circus ring' : x.cls;
const counts = {
  note: 'Written by briefs/061/datum/make.ts. before = the crossings in out/world/harbour-district.world.json (2026-10-24 14:47); after = the proposed coverage rules applied to them (each kept crossing stands where the generator puts it today). Detours: brief 009\'s method on the walks of walks.ts (a station = its nearer stair bank), served = a crossing of that road within a 30 m detour.',
  before: { total: now.length, byClass: tally(now, roadClass), byRule: tally(now, x => x.rule) },
  after: { total: after.length, byClass: tally(after, roadClass), byRule: tally(after, x => keep.get(x.id)!) },
  kept: after.map(x => ({ id: x.id, rule: keep.get(x.id), road: x.road, class: roadClass(x), at: x.mid.map(v => Math.round(v)) })),
  walks: { lines: W.walks.length, roadCrossings: evs.length, places: clusters.length },
  served: { before: { n: A.served, share: A.share, medianDetour: A.medianDetour, crossingsUnused: A.unused }, after: { n: B.served, share: B.share, medianDetour: B.medianDetour, crossingsUnused: B.unused }, byClass: servedBy },
  lostByGroup: lost.reduce((o, l) => { const g = (o[l.group] ??= { places: 0, walkCrossings: 0 }); g.places++; g.walkCrossings += l.walks; return o; }, {} as Record<string, { places: number; walkCrossings: number }>),
  lost: lost.map(l => ({ ...l, before: Math.round(l.before), after: isFinite(l.after) ? Math.round(l.after) : null, walksAre: ends(l), cost: undefined })),
};
writeFileSync(resolve(here, 'counts.json'), JSON.stringify(counts, null, 1));
console.log(JSON.stringify({ before: counts.before, after: counts.after, served: counts.served }, null, 1));
console.log(counts.lostByGroup);
for (const l of counts.lost) console.log(String(l.walks).padStart(3), l.group.padEnd(18), l.name.padEnd(17), l.at.join(','), `${l.before} → ${l.after ?? 'none'} m`, l.walksAre.join(' · '));

// ---- pictures: the plan sheet's district drawing (no proposal layer) with the crossings drawn over it, before | after, rendered by rsvg-convert
const map = district(plan, d, { ...check(plan, d, civ), walks: [] }), M = 34;
const X_ = (x: number) => x - plan.sheet.x[0] + M, Y_ = (y: number) => plan.sheet.y[1] - y + M;
const inner = map.replace(/^<svg[^>]*>/, '').replace(/<\/svg>$/, '');
const pathOf = (poly: V[]) => 'M' + poly.map(p => `${X_(p[0]).toFixed(2)},${Y_(p[1]).toFixed(2)}`).join('L') + 'Z';
const feet = civ.objects.map(o => `<path class="obj ${o.kind}" d="${pathOf(o.poly)}"/>`).join('') + civ.structures.map(x => `<path class="struct" d="${pathOf(x.poly)}"/>`).join('') + civ.buildings.map(b => `<path class="bldg ${b.id}" d="${pathOf(b.poly)}"/>`).join('');
const COL: Record<string, string> = { stops: '#1f6fb8', island: '#2a8a4a', foot: '#c0157a', crossroads: '#d0731b', junction: '#7a4cc0', civic: '#111' };
const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
const seg = (l: V[], stroke: string, sw: number, extra = '') => `<line x1="${X_(l[0]![0])}" y1="${Y_(l[0]![1])}" x2="${X_(l[l.length - 1]![0])}" y2="${Y_(l[l.length - 1]![1])}" stroke="${stroke}" stroke-width="${sw}" stroke-linecap="round" ${extra}/>`;
const walksLayer = W.walks.map((wk: any) => `<polyline fill="none" stroke="#4a3d78" stroke-opacity=".13" stroke-width="1.2" vector-effect="non-scaling-stroke" points="${wk.path.map((p: V) => `${X_(p[0])},${Y_(p[1])}`).join(' ')}"/>`).join('');
const layer = (which: 'before' | 'after', k: number) => {
  if (which === 'before') return now.map(x => seg(x.line, keep.has(x.id) ? '#1f7a6a' : '#b03a2e', 4 * k, `stroke-opacity="${keep.has(x.id) ? 1 : .85}"`)).join('');
  return after.map(x => seg(x.line, COL[keep.get(x.id)!]!, 5.5 * k)).join('') + lost.map(l => `<circle cx="${X_(l.at[0])}" cy="${Y_(l.at[1])}" r="${7 * k}" fill="none" stroke="#b03a2e" stroke-width="${1.6 * k}" stroke-dasharray="${3 * k} ${2 * k}"/>`).join('');
};
const busy = ['primary', 'district', 'circus'], pct = (o: Record<string, number>, ks: string[]) => Math.round((100 * ks.reduce((t, k) => t + (o[k] ?? 0), 0)) / ks.reduce((t, k) => t + (servedBy.all[k] ?? 0), 0));
const legendFor = (which: 'before' | 'after') => which === 'before'
  ? [[`BEFORE · ${now.length} crossings`, 'h'], ['today\'s export, 2026-10-24', 's'], [`kept (${after.length})`, '#1f7a6a'], [`dropped (${now.length - after.length})`, '#b03a2e'], ...Object.entries(counts.before.byRule).map(([k, v]) => [`  ${({ edge: 'plot-edge ends', stop: 'tram stop ends', bank: 'stair-foot patches', arm: 'primary arms', island: 'circus island', mall: 'Parade at Harbour Rd', mid: 'mid-block', civic: 'civic block' } as Record<string, string>)[k] ?? k} · ${v}`, 's'])]
  : [[`AFTER · ${after.length} crossings`, 'h'], ['proposed rules, not built', 's'], ...Object.entries(counts.after.byRule).map(([k, v]) => [`${({ stops: 'tram stop, junction end', island: 'circus island, one per road', foot: 'where a walk meets a road', crossroads: 'lane crossroads on a district rd', junction: 'district × district junction', civic: 'civic: Office axis' } as Record<string, string>)[k] ?? k} · ${v}`, COL[k]!]), ['lost: walks now > 30 m off a crossing', 'lost'], ['(people still cross anywhere)', 's'], ['', 's'], [`busy roads served ${pct(servedBy.before, busy)} → ${pct(servedBy.after, busy)} %`, 'b'], [`lanes served ${pct(servedBy.before, ['access'])} → ${pct(servedBy.after, ['access'])} % (cross anywhere)`, 'b']];
const ROWS = 7, legendH = (s: number) => (ROWS + 0.8) * 1.45 * s;
const legend = (which: 'before' | 'after', x: number, y: number, s: number, w: number) => {
  const rows = legendFor(which); let out = `<rect x="${x}" y="${y}" width="${w}" height="${legendH(s)}" fill="#f8f2e4" stroke="#2b2723" stroke-width="1.2"/>`;
  rows.forEach(([t, k], j) => {
    const col = Math.floor(j / ROWS), i = j % ROWS, lx = x + col * (w / 2), sw = k!.startsWith('#'), ty = y + (i + 1.2) * 1.45 * s;
    if (sw) out += `<line x1="${lx + s * .6}" y1="${ty - s * .3}" x2="${lx + s * 2}" y2="${ty - s * .3}" stroke="${k}" stroke-width="${s * .4}" stroke-linecap="round"/>`;
    if (k === 'lost') out += `<circle cx="${lx + s * 1.3}" cy="${ty - s * .3}" r="${s * .45}" fill="none" stroke="#b03a2e" stroke-width="${s * .12}" stroke-dasharray="${s * .2} ${s * .12}"/>`;
    const font = k === 'h' ? `font-weight="700" font-size="${s * .95}"` : k === 'b' ? `font-weight="700" font-size="${s * .8}" fill="#c0157a"` : `font-size="${s * .78}"`;
    out += `<text x="${lx + s * (sw || k === 'lost' ? 2.4 : .6)}" y="${ty}" font-family="Menlo, monospace" ${font}>${esc(t!)}</text>`;
  });
  return out;
};
type View = { file: string; title: string; x: [number, number]; y: [number, number]; px: number; k: number; civic?: boolean };
const views: View[] = [
  { file: 'crossings-district.png', title: 'Harbour District crossings · before | after (brief 061, Datum)', x: [plan.sheet.x[0] - 10, plan.sheet.x[1] - 250], y: [plan.sheet.y[0], plan.sheet.y[1]], px: 1500, k: 1 },
  { file: 'crossings-civic.png', title: 'Civic block (Records Precinct) and its approaches · before | after', x: [-300, 30], y: [-450, -100], px: 1500, k: 0.6, civic: true },
];
const out = resolve(root, 'out/scratch/datum/061'); mkdirSync(out, { recursive: true });
for (const v of views) {
  const vw = v.x[1] - v.x[0], vh = v.y[1] - v.y[0], mw = v.px, mh = Math.round((mw * vh) / vw), head = 50, gap = 24, s = 21;
  const box = `${X_(v.x[0])} ${Y_(v.y[1])} ${vw} ${vh}`;
  const panel = (which: 'before' | 'after', ox: number) => `<svg x="${ox}" y="${head}" width="${mw}" height="${mh}" viewBox="${box}" preserveAspectRatio="xMidYMid meet"><rect x="${X_(v.x[0])}" y="${Y_(v.y[1])}" width="${vw}" height="${vh}" fill="#f4ecda"/>${inner}${v.civic ? feet : ''}<g>${walksLayer}</g><g>${layer(which, v.k)}</g></svg>` + legend(which, ox, head + mh + 10, s, mw);
  const H = head + mh + 20 + legendH(s);
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${mw * 2 + gap}" height="${H}" viewBox="0 0 ${mw * 2 + gap} ${H}"><style>${CSS}[data-layer="walks"]{display:none}</style><rect width="100%" height="100%" fill="#e9e1cd"/>`
    + `<text x="12" y="33" font-family="Menlo, monospace" font-weight="700" font-size="24" fill="#2b2723">${esc(v.title)}</text>` + panel('before', 0) + panel('after', mw + gap) + '</svg>';
  const svgFile = resolve(out, v.file.replace('.png', '.svg'));
  writeFileSync(svgFile, svg);
  const r = Bun.spawnSync(['rsvg-convert', svgFile, '-o', resolve(pics, v.file)]);
  if (r.exitCode !== 0) throw new Error(`rsvg-convert ${v.file}: ${r.stderr.toString()}`);
  console.log(`briefs/061/${v.file}: ${mw * 2 + gap} × ${Math.round(H)} px`);
}
