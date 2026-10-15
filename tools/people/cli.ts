// usage: bun run people [--scope civic-block] [--cast <cast.json>] [--plan <plan.json>] [--rules <file>] [--mission <file>] [--out <file>] [--review [<file>]]
// Cast tables + plan + civic block + life.json + people rules → out/people/<plan id>.people.json (the promoted cast only; --cast writes beside it), then the checker (P-rules); exit 1 on any fault.
// Without --cast the cast tables are the promoted cast of the plan, contracts/people/<plan id>.cast.json (an accepted people run, promoted by Sky;
// tools/people/promoted.ts holds it to its record and its run first, exit 1 if it drifted). The stand-in cast (tools/people/standin.ts) is a test
// fixture, not an input of this command. Same inputs, same bytes.
import { mkdir } from 'node:fs/promises';
import { basename, dirname, resolve } from 'node:path';
import { Cast } from '../../contracts/people/cast.schema';
import { peopleHash } from '../levels/people/hash';
import { overlay } from '../missions/handover/overlay';
import { civic } from '../plan/civic';
import { derive } from '../plan/plan';
import { root } from '../rag/config';
import { checkPeople } from './check';
import { expand } from './expand';
import { readTexts, world } from './inputs';
import { castFileOf, checkPromotion } from './promoted';
import { reviewHtml } from './review';

const argv = process.argv.slice(2), opt = (k: string) => { const i = argv.indexOf(k); return i < 0 ? undefined : argv[i + 1]; };
// --review [<file>]: also write the review page (default out/review/010-cast.html)
const KNOWN = ['--scope', '--cast', '--plan', '--rules', '--mission', '--out', '--review'];
for (const a of argv.filter(x => x.startsWith('--'))) if (!KNOWN.includes(a)) { console.error(`unknown option ${a}; known: ${KNOWN.join(' ')}`); process.exit(2); }
const planFile = opt('--plan') ?? 'contracts/world/harbour-district.plan.json';
const texts = await readTexts(planFile, { rules: opt('--rules'), mission: opt('--mission') }), w = world(texts, opt('--scope') ?? 'civic-block');
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
// stops no seat could take are not made (a meal is eaten on site, an evening not made): a busy square, not a fault; said here so a table that fills the square is seen
if (gen.notes.length) console.log(`note  ${gen.notes.length} stop(s) not made for want of a free seat or standing point (a meal is eaten on site, an evening or a stop before the shift is dropped): ${gen.notes.slice(0, 3).join('; ')}${gen.notes.length > 3 ? '; …' : ''}`);
// M-2 on these people: the casting rule the plan checker's overlay runs (the Handover's roles, there when needed, one pinned each, and the cast can play them: alone, tied,
// leaving, waiting; Waymark's castRoles with `play`), here on the expansion just made, so a candidate cast is held to it before it is promoted. The overlay itself expands the
// promoted cast, so for that cast this line only repeats what `bun tools/plan/cli.ts --overlay tools/missions/handover/overlay.ts` says, and it is not enforced here (r013 does not
// play the Handover; a cast that does is a live run away); for any other cast (`--cast`) it is.
const block = JSON.parse(texts.block), m2 = overlay(w.plan, block, gen.people as never, JSON.parse(texts.mission)).civic!({ d: derive(w.plan), civ: civic(w.plan.civic, block) } as never).find(c => c.id === 'M-2')!;
console.log(`${m2.ok ? 'ok  ' : 'FAIL'} M-2  ${m2.rule}: ${m2.value}${m2.ok ? '' : `\n       ${(m2.detail ?? '').split('; ').join('\n       ')}`}${opt('--cast') || m2.ok ? '' : '\n       (the promoted cast: shown, not enforced here)'}`);
console.log(`${checked.counts.persons} people, ${checked.counts.units} units → ${out} (${label})`);
const ri = argv.indexOf('--review');
if (ri >= 0) {
  const next = argv[ri + 1], file = resolve(root, next && !next.startsWith('--') ? next : 'out/review/010-cast.html');
  await mkdir(dirname(file), { recursive: true });
  await Bun.write(file, reviewHtml(ctx, gen.people, checked.checks, label));
  console.log(`review page → ${file}`);
}
process.exit(checked.ok && (m2.ok || !opt('--cast')) ? 0 : 1);
