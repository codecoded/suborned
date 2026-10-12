// bun run world: write out/world/<plan id>.world.json (suborned.world/2, contracts/world/world.schema.ts) from the plan.
// Exits 1, writing nothing, unless the plan passes the checker and every generated deck has its connectors.
// usage: bun tools/world/export.ts [plan.json] [--out file] [--dev]   (--dev: develop against a plan the checker refuses; the export is marked)
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { derive, type Plan } from '../plan/plan';
import { civic } from '../plan/civic';
import { check } from '../plan/checks';
import { worldOf } from './build';
import { report } from './report';
import { hasWorldStandards, worldRules } from '../plan/world-rules';
import { ctxOf as worldCtx } from '../plan/world-ctx';

const root = resolve(import.meta.dir, '../..');
const args = process.argv.slice(2), opt = (k: string) => (args.includes(k) ? args[args.indexOf(k) + 1] : undefined), dev = args.includes('--dev');
const src = args.find(a => a.endsWith('.json') && a !== opt('--out')) ?? 'contracts/world/harbour-district.plan.json';
const json = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));

const plan: Plan = json(src), d = derive(plan), civ = civic(plan.civic, json(plan.civic.block));
const failed = check(plan, d, civ).checks.filter(c => !c.ok);
for (const c of failed) console.error(`${dev ? 'dev: ' : ''}FAIL ${c.id} ${c.rule}: ${c.value}`);
if (failed.length && !dev) { console.error('world: plan fails the checker; no export (--dev to develop against it)'); process.exit(1); }

const { world: w, gen } = worldOf(plan, d, civ);
if (gen.issues.length) { for (const i of gen.issues) console.error(`world: ${i}`); console.error('world: generator issues; no export'); process.exit(1); }
// the generated world must pass the world rules (B, L, MS, F, SF) too
const worldFailed = hasWorldStandards(plan) ? worldRules(worldCtx(plan, d, civ, { w, gen })).filter(c => !c.ok) : [];
for (const c of worldFailed) console.error(`${dev ? 'dev: ' : ''}FAIL ${c.id} ${c.rule}: ${c.value}${c.detail ? `\n        ${c.detail}` : ''}`);
if (worldFailed.length && !dev) { console.error('world: the generated world fails the world rules; no export (--dev to develop against it)'); process.exit(1); }
const out = resolve(root, opt('--out') ?? `out/world/${plan.id}.world.json`);
mkdirSync(dirname(out), { recursive: true });
const text = JSON.stringify(w);
writeFileSync(out, text);
console.log(`world → ${out} (${(text.length / 1024).toFixed(0)} KiB)${failed.length || worldFailed.length ? ' [DEV: fails the checker]' : ''}`);
report(w, gen, d);
