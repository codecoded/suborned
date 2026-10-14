// usage: bun run people [--scope civic-block] [--cast <cast.json>] [--plan <plan.json>] [--rules <file>] [--out <file>]
// Cast tables + plan + civic block + life.json + people rules → out/people/<plan id>.people.json, then the checker (P-rules); exit 1 on any fault.
// Without --cast the cast tables are the stand-in's (tools/people/standin.ts): a valid cast, not a good one. Same inputs, same bytes.
import { mkdir } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { Cast } from '../../contracts/people/cast.schema';
import { root } from '../rag/config';
import { checkPeople } from './check';
import { expand } from './expand';
import { readTexts, world } from './inputs';
import { standInCast } from './standin';

const argv = process.argv.slice(2), opt = (k: string) => { const i = argv.indexOf(k); return i < 0 ? undefined : argv[i + 1]; };
const KNOWN = ['--scope', '--cast', '--plan', '--rules', '--out'];
for (const a of argv.filter(x => x.startsWith('--'))) if (!KNOWN.includes(a)) { console.error(`unknown option ${a}; known: ${KNOWN.join(' ')}`); process.exit(2); }
const planFile = opt('--plan') ?? 'contracts/world/harbour-district.plan.json';
const w = world(await readTexts(planFile, { rules: opt('--rules') }), opt('--scope') ?? 'civic-block');
const castFile = opt('--cast'), cast = castFile ? Cast.parse(JSON.parse(await Bun.file(resolve(root, castFile)).text())) : standInCast(w.scope, w.rules);
const ctx = w.ctx(cast), gen = expand(ctx), checked = checkPeople(ctx, w.mission, w.plan, gen);
const out = resolve(root, opt('--out') ?? `out/people/${w.plan.id}.people.json`);
await mkdir(dirname(out), { recursive: true });
await Bun.write(out, JSON.stringify(gen.people, null, 1) + '\n');
for (const c of checked.checks) console.log(`${c.ok ? 'ok  ' : 'FAIL'} ${c.id}  ${c.rule}: ${c.value}${c.ok ? '' : `\n       ${c.detail}`}`);
console.log(`${checked.counts.persons} people, ${checked.counts.units} units → ${out}${castFile ? '' : ' (stand-in cast)'}`);
process.exit(checked.ok ? 0 : 1);
