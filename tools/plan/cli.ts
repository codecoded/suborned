// Harbour District plan: check the plan against its rules and render the CAD sheet.
// usage (repo root): bun tools/plan/cli.ts [plan.json] [--out sheet.html] [--json report.json]
import { writeFileSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { load, root, cliArgs } from './load';
import { district, inset } from './draw';
import { sheet } from './sheet';
import { fmtCheck } from './checks';

const { src, opt } = cliArgs(process.argv.slice(2));

const { plan, d, civ, R } = load(src);

for (const c of R.checks) console.log(fmtCheck(c));
for (const r of R.routes) console.log(`route ${r.id}: ${r.roads.join(' → ')} → ${r.to}, ${Math.round(r.length)} m, gates ${r.gates.join(', ') || '—'}`);
console.log(`pylons ${d.pylons.length}, tram crossings ${R.crossings.map(c => c.road).join(', ')}`);

const out = resolve(root, opt('--out') ?? `out/plan/${plan.id}-masterplan-v${plan.version.split('.').slice(0, 2).join('.')}-${plan.date}.html`);
const { svg, legend } = inset(plan, d, civ, R);
mkdirSync(dirname(out), { recursive: true });
writeFileSync(out, sheet(plan, R, { district: district(plan, d, R), inset: svg, legend }, src));
console.log(`sheet → ${out}`);
if (opt('--json')) writeFileSync(resolve(root, opt('--json')!), JSON.stringify({ checks: R.checks, routes: R.routes, stops: R.stops, stations: R.stations, pylons: d.pylons.map(p => ({ p: p.p.map(v => +v.toFixed(2)), line: p.line, path: p.path, span: +p.span.toFixed(2) })) }, null, 1));
process.exit(R.checks.every(c => c.ok) ? 0 : 1);
