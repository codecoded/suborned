// Harbour District plan: check the plan against its rules and render the CAD sheet.
// usage (repo root): bun tools/plan/cli.ts [plan.json] [--out sheet.html] [--json report.json]
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { derive, type Plan } from './plan';
import { civic } from './civic';
import { check } from './checks';
import { district, inset } from './draw';
import { sheet } from './sheet';

const root = resolve(import.meta.dir, '../..');
const args = process.argv.slice(2), opt = (k: string) => (args.includes(k) ? args[args.indexOf(k) + 1] : undefined);
const src = args.find((a: string) => a.endsWith('.json') && a !== opt('--json')) ?? 'contracts/world/harbour-district.plan.json';
const json = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));

const plan: Plan = json(src);
const d = derive(plan);
const civ = civic(plan.civic, json(plan.civic.manifest), json(plan.civic.destinations));
const R = check(plan, d, civ);

for (const c of R.checks) console.log(`${c.ok ? 'PASS' : 'FAIL'}  ${c.id.padEnd(6)} ${c.rule}: ${c.value} (${c.limit})${c.detail ? `\n        ${c.detail}` : ''}`);
for (const r of R.routes) console.log(`route ${r.id}: ${r.roads.join(' → ')} → ${r.to}, ${Math.round(r.length)} m, gates ${r.gates.join(', ') || '—'}`);
console.log(`pylons ${d.pylons.length}, tram crossings ${R.crossings.map(c => c.road).join(', ')}`);

const out = resolve(root, opt('--out') ?? `out/plan/${plan.id}-masterplan-v${plan.version.split('.').slice(0, 2).join('.')}-${plan.date}.html`);
const { svg, legend } = inset(plan, d, civ, R);
mkdirSync(dirname(out), { recursive: true });
writeFileSync(out, sheet(plan, R, { district: district(plan, d, R), inset: svg, legend }, src));
console.log(`sheet → ${out}`);
if (opt('--json')) writeFileSync(resolve(root, opt('--json')!), JSON.stringify({ checks: R.checks, routes: R.routes, stops: R.stops, stations: R.stations, pylons: d.pylons.map(p => ({ p: p.p.map(v => +v.toFixed(2)), line: p.line, path: p.path, span: +p.span.toFixed(2) })) }, null, 1));
process.exit(R.checks.every(c => c.ok) ? 0 : 1);
