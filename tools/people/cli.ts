// usage: bun run people [--scope civic-block] [--cast <cast.json>] [--plan <plan.json>] [--rules <file>] [--out <file>] [--review [<file>]]
// Cast tables + plan + civic block + life.json + people rules → out/people/<plan id>.people.json (the promoted cast only; --cast writes beside it), then the checker (P-rules); exit 1 on any fault.
// Without --cast the cast tables are the promoted cast of the plan, contracts/people/<plan id>.cast.json (an accepted people run, promoted by Sky;
// tools/people/promoted.ts holds it to its record and its run first, exit 1 if it drifted). The stand-in cast (tools/people/standin.ts) is a test
// fixture, not an input of this command. Same inputs, same bytes.
import { mkdir } from 'node:fs/promises';
import { basename, dirname, resolve } from 'node:path';
import { Cast } from '../../contracts/people/cast.schema';
import { peopleHash } from '../levels/people/hash';
import { root } from '../rag/config';
import { checkPeople } from './check';
import { expand } from './expand';
import { readTexts, world } from './inputs';
import { castFileOf, checkPromotion } from './promoted';
import { reviewHtml } from './review';

const argv = process.argv.slice(2), opt = (k: string) => { const i = argv.indexOf(k); return i < 0 ? undefined : argv[i + 1]; };
// --review [<file>]: also write the review page (default out/review/010-cast.html)
const KNOWN = ['--scope', '--cast', '--plan', '--rules', '--out', '--review'];
for (const a of argv.filter(x => x.startsWith('--'))) if (!KNOWN.includes(a)) { console.error(`unknown option ${a}; known: ${KNOWN.join(' ')}`); process.exit(2); }
const planFile = opt('--plan') ?? 'contracts/world/harbour-district.plan.json';
const w = world(await readTexts(planFile, { rules: opt('--rules') }), opt('--scope') ?? 'civic-block');
let castFile = opt('--cast'), label: string;
if (castFile) label = `cast ${castFile}`;
else {
  const promo = checkPromotion(w.plan.id);
  console.log(`${promo.ok ? 'ok  ' : 'FAIL'} promoted cast  ${promo.label}`);
  if (!promo.ok) { for (const p of promo.problems) console.log(`       ${p}`); process.exit(1); }
  castFile = castFileOf(w.plan.id); label = `promoted ${promo.label}`;
}
const cast = Cast.parse(JSON.parse(await Bun.file(resolve(root, castFile)).text()));
const ctx = w.ctx(cast), gen = expand(ctx), checked = checkPeople(ctx, w.mission, w.plan, gen);
// out/people/<plan id>.people.json is what M-2's readers and the Unity people layer take: only the promoted cast writes it. Another cast (--cast) writes beside it,
// out/people/<plan id>.<cast file name>.people.json, unless --out says where.
const out = resolve(root, opt('--out') ?? (opt('--cast') ? `out/people/${w.plan.id}.${basename(opt('--cast')!).replace(/\.json$/, '')}.people.json` : `out/people/${w.plan.id}.people.json`));
await mkdir(dirname(out), { recursive: true });
// the file records the hash of the code that made it (the checker above holds the expansion itself; `tools` is not part of it), so M-2 and the Unity people layer can tell a file
// made by other code, another cast, mission or plan from the current ones
const written = { ...gen.people, inputs: { ...gen.people.inputs, tools: await peopleHash() } };
await Bun.write(out, JSON.stringify(written, null, 1) + '\n');
for (const c of checked.checks) console.log(`${c.ok ? 'ok  ' : 'FAIL'} ${c.id}  ${c.rule}: ${c.value}${c.ok ? '' : `\n       ${c.detail}`}`);
console.log(`${checked.counts.persons} people, ${checked.counts.units} units → ${out} (${label})`);
const ri = argv.indexOf('--review');
if (ri >= 0) {
  const next = argv[ri + 1], file = resolve(root, next && !next.startsWith('--') ? next : 'out/review/010-cast.html');
  await mkdir(dirname(file), { recursive: true });
  await Bun.write(file, reviewHtml(ctx, gen.people, checked.checks, label));
  console.log(`review page → ${file}`);
}
process.exit(checked.ok ? 0 : 1);
