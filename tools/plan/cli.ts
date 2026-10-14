// Harbour District plan: check the plan against its rules and render the CAD sheet.
// usage (repo root): bun tools/plan/cli.ts [plan.json] [--out sheet.html] [--json report.json] [--strict] [--block block.json] [--kit kit.json] [--look civic-look.json] [--overlay module.ts]
// --overlay loads a rule overlay (tools/plan/overlay.ts) from a module exporting overlay(plan, block).
// Without --overlay, a block that carries its own destinations (a recorded run's pinned block) supplies its legacy overlay.
// --no-world skips the world rules (B, L, MS, F, SF), which generate the world (cached in out/cache).
// --block checks against that block file instead of plan.civic.block (a run's pinned block).
// --kit / --look build the world the world rules read from that catalogue (a kit.json or a run's pinned kit digest) and that civic look (a run's pinned copy) instead of the
// files in contracts/world; a world built from either is generated in process, not cached.
// --strict ignores standards.transit.waived (generation runs always do). Sheet default: out/plan/<id>-v<full version>.html
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { derive, type Plan } from './plan';
import { civic } from './civic';
import { check } from './checks';
import type { Overlay } from './overlay';
import { district, inset } from './draw';
import { sheet } from './sheet';
import { legacyOverlay } from '../missions/handover/overlay';
import { worldChecks, worldChecksUnder } from './world-rules';
import { generated } from './world-ctx';
import { existsSync } from 'node:fs';

const root = resolve(import.meta.dir, '../..');
const args = process.argv.slice(2), opt = (k: string) => (args.includes(k) ? args[args.indexOf(k) + 1] : undefined);
const src = args.find((a: string) => a.endsWith('.json') && a !== opt('--json') && a !== opt('--block') && a !== opt('--overlay') && a !== opt('--kit') && a !== opt('--look')) ?? 'contracts/world/harbour-district.plan.json';
const json = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));

const plan: Plan = json(src);
const d = derive(plan);
const block = json(opt('--block') ?? plan.civic.block), civ = civic(plan.civic, block);
const overlay: Overlay | undefined = opt('--overlay') ? (await import(resolve(root, opt('--overlay')!))).overlay(plan, block) : legacyOverlay(plan, block); // a pinned suborned.block/2 carries its own mission (recorded runs)
const R = check(plan, d, civ, args.includes('--strict'), overlay);
if (!args.includes('--no-world')) R.checks.push(...(opt('--kit') || opt('--look') ? worldChecksUnder(plan, d, civ, { kit: opt('--kit') && resolve(root, opt('--kit')!), look: opt('--look') && resolve(root, opt('--look')!) }) : worldChecks(plan, d, civ))); // B, L, MS, F, SF...: on the generated world (cached by content hash; --no-world skips)

for (const c of R.checks) console.log(`${c.waived ? 'WAIVE' : c.ok ? 'PASS' : 'FAIL'}  ${c.id.padEnd(6)} ${c.rule}: ${c.value} (${c.limit})${c.detail ? `\n        ${c.detail}` : ''}`);
for (const r of R.routes) console.log(`route ${r.id}: ${r.roads.join(' → ')} → ${r.to}, ${Math.round(r.length)} m, gates ${r.gates.join(', ') || '—'}`);
console.log(`pylons ${d.pylons.length}, tram crossings ${R.crossings.map(c => c.road).join(', ')}`);

const out = resolve(root, opt('--out') ?? `out/plan/${plan.id}-v${plan.version}.html`);
const { svg, legend } = inset(plan, d, civ, R);
mkdirSync(dirname(out), { recursive: true });
// the crossing proposal layer (plan crossings.coverage, brief 009): the crossings as generated and the desire lines behind it; off with --no-world, --kit or --look
const cov = plan.standards?.streetscape?.crossings?.coverage, walksFile = cov?.walks && resolve(root, cov.walks);
const prop = cov && !args.includes('--no-world') && !opt('--kit') && !opt('--look') ? { crossings: (generated(plan, d, civ).w.crossings ?? []).map(c => ({ id: c.id, line: c.line as [number, number][] })), walks: walksFile && existsSync(walksFile) ? (JSON.parse(readFileSync(walksFile, 'utf8')).walks as { path: [number, number][]; hot?: boolean }[]).map(w => ({ path: w.path, hot: w.hot })) : undefined } : undefined;
writeFileSync(out, sheet(plan, R, { district: district(plan, d, R, prop), inset: svg, legend }, src));
console.log(`sheet → ${out}`);
if (opt('--json')) writeFileSync(resolve(root, opt('--json')!), JSON.stringify({ plan: plan.id, version: plan.version, strict: args.includes('--strict'), ok: R.checks.every(c => c.ok), checks: R.checks, routes: R.routes, stops: R.stops, stations: R.stations, pylons: d.pylons.length }, null, 1));
process.exit(R.checks.every(c => c.ok) ? 0 : 1);
