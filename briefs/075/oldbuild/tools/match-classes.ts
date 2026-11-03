// bun briefs/075/oldbuild/tools/match-classes.ts [match-swap2.json]: sorts a plan-match run's mismatches into classes (first rule that fits wins), adds
// `classes` to the match file and writes match-<tag>.html beside it (self-contained). Splice, brief 075 remap step 2. The classes are a reading aid for
// Datum and Latch: each carries a proposed decision (apply to plan / old gives way / accept as drift); the mismatches themselves are the CLI's.
import { readFileSync, writeFileSync } from 'node:fs';
import { basename, dirname, resolve } from 'node:path';

type V = [number, number];
type M = { id: string; kind: string; side: string; el: string; near: string | null; dev: number | null; at: V; length?: number; zone: boolean; area?: string; detail?: string };
const root = resolve(import.meta.dir, '../../../..');
const file = resolve(root, process.argv[2] ?? 'briefs/075/oldbuild/match-swap2.json');
const run = JSON.parse(readFileSync(file, 'utf8'));
const plan = JSON.parse(readFileSync(resolve(root, run.plan), 'utf8'));
const M: M[] = run.mismatches;

const inR = (p: V, r: number[], g = 0) => p[0] >= r[0]! - g && p[0] <= r[1]! + g && p[1] >= r[2]! - g && p[1] <= r[3]! + g;
// the Handover's kept roads and plots (tools/plan/oldtown/transcribe.ts KEEP_ROADS / KEEP_PLOTS): the plan keeps them byte for byte
const src = readFileSync(resolve(root, 'tools/plan/oldtown/transcribe.ts'), 'utf8');
const list = (name: string) => JSON.parse((src.match(new RegExp(`const ${name} = (\\[[^\\]]*\\])`))?.[1] ?? '[]').replace(/'/g, '"')) as string[];
const keepRoads = new Set(list('KEEP_ROADS').map(r => `road/${r}`)), keepPlots = plan.plots.filter((p: any) => list('KEEP_PLOTS').includes(p.id) && p.rect);
const megas: { id: string; r: number[] }[] = plan.megastructures.flatMap((m: any) => (m.footprint ?? []).map((r: number[]) => ({ id: m.id, r })));
const hq = plan.signatures.find((s: any) => s.id === 'space-hq');
const LANDMARK = /^(SphereStation|Skylon|Skyport_Tower|LinerDock|DiscTower|ParabolicArch|CircusMonument|GoldTower|GateTower|RocketGantry)|^(landmark|signature)\//;
const piece = (s: string | null) => (s ?? '').replace(/#\d+$/, '');

type Cls = { id: string; title: string; decision: 'apply to plan' | 'old gives way' | 'accept as drift' | 'matcher'; owner: string; why: string; test: (m: M) => boolean };
const C: Cls[] = [
  { id: 'spacehq-form', title: 'SpaceHQ: the scene is "ours", the plan is "old"', decision: 'apply to plan', owner: 'Datum (plan form ours) · Splice (bank ground in the drum, done in this round)',
    why: 'Swap2 builds our SpaceHQ_Portal (Sky\'s pick) while plan 0.5.5 keeps form "old"; the old HQ\'s ground has no built piece. Clears when `transcribe --spacehq ours` passes and becomes the plan.',
    test: m => (m.el.includes('space-hq') || (m.near ?? '').includes('space-hq')) && !!hq && inR(m.at, hq.ground, 10) },
  { id: 'landmark-outline', title: 'Landmarks and flagships: old piece outline against the plan reservation', decision: 'matcher', owner: 'Splice',
    why: 'Plan landmarks are reservations (centre + r) and the old pieces stand at those centres; their foot outlines (legs, docks, podiums) are not a plot edge. Datum rejected the same class before ("a landmark, not a plot", 13 ops). Proposal: compare landmarks by centre and containment, not edge to edge.',
    test: m => LANDMARK.test(piece(m.el)) || (m.kind === 'footprint' && LANDMARK.test(piece(m.near))) },
  { id: 'rounded-corner', title: 'Rounded corners: StreamCorner arcs against the plan\'s square block corners', decision: 'apply to plan', owner: 'Datum (standard) · Splice (matcher reads it)',
    why: 'Built-only arcs at 1.6 m and plan-only corners at 2.3 m are one geometry: the old 8 × 8 corner cell rounds every outside corner (Sky asked for these corners). Proposal: a plan standard for the outside-corner radius of perimeter blocks; the matcher rounds the plan\'s outside corners by it before comparing.',
    test: m => m.kind === 'footprint' && m.dev !== null && ((m.side === 'built-only' && /^(StreamCorner|StreamEnd|HallCornerWing|ArcadeBay)/.test(piece(m.el)) && m.dev <= 2.5)
      || (m.side === 'plan-only' && (/^(StreamCorner|StreamEnd|HallCornerWing)/.test(piece(m.near)) ? m.dev <= 2.5 : m.dev >= 2.2 && m.dev <= 2.45))) },
  { id: 'handover-street', title: 'Old ground on the Handover\'s kept streets and plots', decision: 'old gives way', owner: 'Latch (swap cut)',
    why: 'The plan keeps these roads and plots byte for byte (KEEP_ROADS / KEEP_PLOTS; protected by the brief). The swap cut removes standing pieces and flat old ground under 30 m there, but old kerb stones, flags and city mass remain or our masses are not stood in. Proposal: the cut takes kerb stones, flag modules and city mass too, and the kept plots carry the export\'s masses (as the zone does).',
    test: m => keepRoads.has(m.near ?? '') || keepRoads.has(m.el.replace(/#.*$/, '')) || keepPlots.some((p: any) => inR(m.at, p.rect, 1) && !inR(m.at, [-226, -84, -372, -148])) },
  { id: 'southside', title: 'Southside ground (the 35° courses, Apollo Way, Market Street)', decision: 'old gives way', owner: 'Latch (E10 follow-up) · Datum for any street the plan should take',
    why: 'Kerbs and flags of the old Southside layout that E10 moved onto the plan blocks still run on the old lines in places (diagonal kerb runs on Market Street and Apollo Way, plan pavement edges with no old flag). Market Street is not a kept Handover street, so where the old line is the better street Datum may take it instead.',
    test: m => (m.kind === 'kerb' || (m.kind === 'pavement' && m.side === 'plan-only')) && m.at[1] < -370 },
  { id: 'megastructure', title: 'Megastructure podiums (Lantern and Crown Terraces) against old frontage runs', decision: 'old gives way', owner: 'Latch (swap cut) · Datum confirms',
    why: 'Old runs stand where the plan has a megastructure podium; Datum rejected RL2/RB1 in lantern-court on the same ground in the first round.',
    test: m => m.kind !== 'kerb' && (m.el.startsWith('mega/') || (m.near ?? '').startsWith('mega/') || megas.some(g => inR(m.at, g.r, 2))) },
  { id: 'doors', title: 'Doors against old bay fronts', decision: 'accept as drift', owner: 'Splice (matcher) · Datum confirms',
    why: 'Doors are no plan field outside the civic block (Datum, first round: 26 ops rejected); our frontage generator places them per bay class and the old builders model bays, not doors (in the west the SwapWest blocks lay their bays on the old module, so ours and theirs fall apart there too). Proposal: outside the Handover zone report these as information, not failures.',
    test: m => m.kind === 'entrance' },
  { id: 'old-flags', title: 'Old flag modules on plan plot ground (yards, forecourts, the Southside plots)', decision: 'accept as drift', owner: 'Datum confirms',
    why: 'A plan plot runs from the kerb (Datum, first round): flags laid on plot ground are paving inside the plot, not a pavement edge out of place. Proposal: the matcher counts built flags inside a plan plot as plot paving; only flags off every plot and off the plan pavements fail.',
    test: m => m.kind === 'pavement' && m.side === 'built-only' },
  { id: 'kerb-lines', title: 'Kerb lines off the plan\'s road edges', decision: 'apply to plan', owner: 'Datum (per road: plan takes the old line, or rejects with a reason)',
    why: 'Old kerb runs the plan does not have (Crown Avenue past the circus, the Boulevard\'s far south) and plan kerbs with no old stone within 0.5 m; each is a road width or line call.',
    test: m => m.kind === 'kerb' || m.kind === 'pavement' },
  { id: 'other', title: 'Other footprints', decision: 'apply to plan', owner: 'Datum (op by op in plan-patch-swap2.json)', why: 'Block and plot edges that differ by more than the corner rounding: the plan-patch ops.', test: () => true },
];

// OV-1 on the real outlines, by what met what (the built file's pieces: a raised piece whose base is at or above the other's top stands on it)
const built = JSON.parse(readFileSync(resolve(root, run.built), 'utf8')), sheet = plan.sheet as { x: number[]; y: number[] };
const pc = new Map<string, any>(built.buildings.map((b: any, i: number) => [`${b.piece}#${i}`, b]));
const BAY = /^(StreamBay|PortholeBay|ArcadeBay|BalconyBay|HallBay|StreamCorner|StreamEnd|DecoEnd|DecoCorner)/;
const ovc = new Map<string, number>();
for (const f of run.overlap.faults as { a: string; b: string; depth: number; at: V }[]) {
  const a = pc.get(f.a), b = pc.get(f.b), on = f.at[0] >= sheet.x[0]! && f.at[0] <= sheet.x[1]! && f.at[1] >= sheet.y[0]! && f.at[1] <= sheet.y[1]!;
  const [lo, hi] = a && b && a.base <= b.base ? [a, b] : [b, a];
  const k = !on ? 'beyond the sheet (the old city mass backdrop): not compared elsewhere in plan-match either'
    : lo && hi && hi.base > 1 && hi.base >= lo.top - 0.5 ? 'stacked: a raised piece (tower) standing on the bays under it, not side by side'
    : BAY.test(f.a) && BAY.test(f.b) && a?.building !== b?.building && f.depth <= 3.1 ? 'run joins: two runs\' end bays meeting at a block corner, the shopfront and canopy depth (≤ 3.1 m) crossing'
    : 'on the sheet, side by side (towers beside bays, ends into runs)';
  ovc.set(k, (ovc.get(k) ?? 0) + 1);
}
const ovClasses = [...ovc].sort((x, y) => y[1] - x[1]).map(([what, n]) => ({ what, n }));
const by = new Map<string, M[]>(C.map(c => [c.id, []]));
for (const m of M) { const c = C.find(c => c.test(m))!; by.get(c.id)!.push(m); }
const sum = (ms: M[]) => ({ n: ms.length, length: Math.round(ms.reduce((s, m) => s + (m.length ?? 0), 0)), kinds: Object.fromEntries([...new Set(ms.map(m => `${m.kind} ${m.side}`))].sort().map(k => [k, ms.filter(m => `${m.kind} ${m.side}` === k).length])) });
const classes = C.map(c => ({ id: c.id, title: c.title, decision: c.decision, owner: c.owner, why: c.why, ...sum(by.get(c.id)!), ids: by.get(c.id)!.map(m => m.id) })).filter(c => c.n).sort((a, b) => b.n - a.n);
run.overlapClasses = ovClasses;
run.classes = { by: 'briefs/075/oldbuild/tools/match-classes.ts (Splice)', note: 'first rule that fits wins; decisions are proposals for Datum and Latch', list: classes };
writeFileSync(file, JSON.stringify(run, null, 1));

const prev = (() => { try { return JSON.parse(readFileSync(resolve(dirname(file), 'match.json'), 'utf8')); } catch { return null; } })();
const kinds = [...new Set([...Object.keys(run.counts), ...Object.keys(prev?.counts ?? {})])].sort();
const esc = (s: unknown) => String(s).replace(/[&<>]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' })[c]!);
const ov = run.overlap.faults as { a: string; b: string }[];
const html = `<!doctype html><meta charset="utf-8"><title>Plan-match on swap2 (brief 075)</title>
<style>body{font:14px/1.45 system-ui,sans-serif;margin:24px;max-width:1100px;color:#222}table{border-collapse:collapse;margin:8px 0 18px}td,th{border:1px solid #ccc;padding:4px 8px;text-align:left;vertical-align:top}th{background:#f3efe6}td.n{text-align:right}.d{font-weight:600}code{background:#f4f4f4;padding:0 3px}</style>
<h1>Plan-match: swap2 against plan v${esc(run.planVersion)}</h1>
<p><code>bun run oldbuild:match --built ${esc(run.built)} --tag swap2</code> · tolerances footprints ${run.tolerance.footprint} m, kerbs/pavements ${run.tolerance.kerb} m, entrances ${run.tolerance.entrance} m, levels exact.</p>
<p><b>Inside the Handover zone: ${run.inZone}</b> (hard) · outside: <b>${run.outside}</b> (first swap: ${prev ? `${prev.inZone} / ${prev.outside}` : '0 / 1,273'}; Latch's scratch run on swap2 before this round's door fix: 0 / 1,041).</p>
<h2>By kind</h2><table><tr><th>kind</th><th>first swap (v${esc(prev?.planVersion ?? '?')})</th><th>swap2</th></tr>
${kinds.map(k => `<tr><td>${esc(k)}</td><td class=n>${prev?.counts?.[k] ?? 0}</td><td class=n>${run.counts[k] ?? 0}</td></tr>`).join('\n')}
<tr><th>total</th><th class=n>${prev?.mismatches?.length ?? ''}</th><th class=n>${M.length}</th></tr></table>
<p>Not compared (outside the old ground's coverage or the sheet): ${Object.entries(run.uncovered).map(([k, v]) => `${esc(k)} ${v}`).join(' · ')}.</p>
<h2>OV-1 on the real outlines</h2><p>${ov.length} faults over ${run.overlap.checked} footprints (first swap, bounds boxes: ${prev ? `${prev.overlap.faults.length} over ${prev.overlap.checked}` : '?'}).</p>
<table><tr><th>what met what</th><th>n</th></tr>${ovClasses.map(c => `<tr><td>${esc(c.what)}</td><td class=n>${c.n}</td></tr>`).join('')}</table>
<p>Proposed: OV-1 on the old-built file compares on the sheet only (as the mismatches do) and leaves out a raised piece standing on the piece under it (matcher, Splice); run joins are the old builders' open joins (accept as drift, or Latch trims the end bay's canopy at a join); the side-by-side rest goes to Latch as builder fixes.</p>
<h2>What's left, by class</h2><table><tr><th>class</th><th>n</th><th>m</th><th>kinds</th><th>proposed</th><th>owner</th><th>why</th></tr>
${classes.map(c => `<tr><td>${esc(c.title)}</td><td class=n>${c.n}</td><td class=n>${c.length || ''}</td><td>${Object.entries(c.kinds).map(([k, n]) => `${esc(k)} ${n}`).join('<br>')}</td><td class=d>${esc(c.decision)}</td><td>${esc(c.owner)}</td><td>${esc(c.why)}</td></tr>`).join('\n')}
</table><p>Every mismatch with its class: <code>${esc(basename(file))}</code> (<code>classes.list[].ids</code>); the plan-patch ops: <code>plan-patch-swap2.json</code>.</p>`;
const out = file.replace(/\.json$/, '.html');
writeFileSync(out, html);
console.log(ovClasses.map(c => `OV-1 ${String(c.n).padStart(4)}  ${c.what}`).join('\n'));
console.log(classes.map(c => `${c.id.padEnd(18)} ${String(c.n).padStart(4)} ${String(c.length).padStart(6)} m  ${c.decision}`).join('\n'));
console.log(`wrote ${file} (classes), ${out}`);
