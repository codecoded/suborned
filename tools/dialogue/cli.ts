// usage: bun run dialogue [--plan <plan.json>] [--scope all|lexicon|scenes|street|civic] [--standin] [--tables <voices.json>]... [--people <people.json>] [--seed <s>] [--out <lines.json>] [--handover]
// The voice tables (the promoted ones of each scope, contracts/dialogue/<plan id>.<scope>.voices.json; or --tables; or --standin, code-made stand-ins with the Handover's text where it has the situation)
// + the people (the promoted cast, expanded; or --people) + the three rule files → each person's lines and scenes (suborned.lines/1) → the checker (D-rules); exit 1 on any fault. Same inputs, same bytes.
// --handover also binds the four Handover roles to generated people and compares them with contracts/missions/handover/text.json (what the generator must match or beat).
import { mkdir } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { Voices } from '../../contracts/dialogue/voices.schema';
import { sha256 } from '../gen/sha';
import { root } from '../rag/config';
import { castHandover } from '../people/handover';
import { readTexts, world } from '../people/inputs';
import { bindHandover, compareToReference, ROLES, type Binding } from './bind';
import { checkDialogue } from './check';
import { linesFile, merge, type ExpandCtx } from './expand';
import { parseTexts, peopleOf, readText, readTexts_ } from './inputs';
import { checkPromotion } from './promoted';
import { standIn } from './standin';
import { People } from '../../contracts/people/people.schema';

const argv = process.argv.slice(2), many = (k: string) => argv.flatMap((a, i) => (a === k ? [argv[i + 1]!] : [])), opt = (k: string) => many(k)[0];
const KNOWN = ['--plan', '--scope', '--standin', '--tables', '--people', '--seed', '--out', '--handover'];
for (const a of argv.filter(x => x.startsWith('--'))) if (!KNOWN.includes(a)) { console.error(`unknown option ${a}; known: ${KNOWN.join(' ')}`); process.exit(2); }
const planFile = opt('--plan') ?? 'contracts/world/harbour-district.plan.json', scope = opt('--scope') ?? 'all';

const generated = opt('--people') ? undefined : await peopleOf(planFile);
const peopleText = opt('--people') ? await Bun.file(resolve(root, opt('--people')!)).text() : generated!.text;
const texts = await readTexts_({ people: peopleText, names: generated?.pool }), { people, prules, rb, dr, pool: parsedPool } = parseTexts(texts), handoverText = await readText();
if (scope !== 'all' && !dr.scopes[scope]) { console.error(`unknown scope ${scope}; known: all, ${Object.keys(dr.scopes).join(', ')}`); process.exit(2); }
const scopes = scope === 'all' ? Object.keys(dr.scopes) : [scope];

let label: string, voices: Voices[];
if (many('--tables').length) {
  voices = await Promise.all(many('--tables').map(async f => Voices.parse(JSON.parse(await Bun.file(resolve(root, f)).text())))); label = `tables ${many('--tables').join(', ')}`;
} else if (argv.includes('--standin')) {
  voices = scopes.map(s => standIn(s, dr, rb, prules, handoverText)); label = 'stand-in tables (code-made; a test fixture, not words)';
  // a scope that needs the lexicon for its slots is checked with it
  if (scope !== 'all' && !voices.some(v => v.lexicon)) voices.push(standIn(Object.entries(dr.scopes).find(([, x]) => x.blocks.includes('lexicon'))![0], dr, rb, prules, handoverText));
} else {
  voices = [];
  for (const s of scopes) {
    const p = checkPromotion(people.plan.id, s);
    console.log(`${p.ok ? 'ok  ' : 'FAIL'} promoted ${s}  ${p.label}`);
    if (!p.ok) { for (const x of p.problems) console.log(`       ${x}`); console.log('       (no promoted tables yet: run `bun run levels dialogue --scope <scope>`, and promote the accepted run with tools/dialogue/promote.ts; or try --standin)'); process.exit(1); }
    voices.push(Voices.parse(JSON.parse(p.text!)));
  }
  label = `promoted tables (${scopes.join(', ')})`;
}
const seed = opt('--seed') ?? `dialogue|${people.plan.id}`;
const checked = checkDialogue({ dr, rules: prules, rb, people, voices, scope, seed, pool: [...parsedPool, ...people.persons.flatMap(x => x.name.split(/\s+/))], reference: handoverText });
for (const c of checked.checks) console.log(`${c.ok ? 'ok  ' : 'FAIL'} ${c.id}  ${c.rule}: ${c.value}${c.ok ? '' : `\n       ${c.detail}`}`);
const { merged } = merge(voices), ctx: ExpandCtx = { people, rules: prules, dr, rb, tables: merged, seed };
if (checked.lines) {
  const lines = { ...linesFile(ctx, checked.lines.persons, { people: sha256(peopleText), rules: sha256(texts.rules), tables: Object.fromEntries(voices.map(v => [v.scope, sha256(JSON.stringify(v))])) }, { id: people.plan.id, version: people.plan.version }) };
  const out = resolve(root, opt('--out') ?? `out/dialogue/${people.plan.id}.${scope}${argv.includes('--standin') ? '.standin' : ''}.lines.json`);
  await mkdir(dirname(out), { recursive: true });
  await Bun.write(out, JSON.stringify(lines, null, 1) + '\n');
  console.log(`${checked.counts.persons} people, ${checked.counts.entries} entries, ${checked.counts.scenes} scenes → ${out} (${label})`);
  if (argv.includes('--handover')) {
    const texts0 = await readTexts(planFile), w = world(texts0, 'civic-block'), casts = castHandover(People.parse(JSON.parse(peopleText)).persons as never, w.scope, w.mission, w.plan, w.rules);
    const binding: Binding = {}, temps: Partial<Record<(typeof ROLES)[number], string>> = {};
    for (const c of casts) if (c.ok && c.pinned && (ROLES as readonly string[]).includes(c.role)) { binding[c.role as (typeof ROLES)[number]] = c.pinned; temps[c.role as (typeof ROLES)[number]] = people.persons.find(p => p.id === c.pinned)!.temperament; }
    for (const c of casts.filter(x => !x.ok)) console.log(`note  ${c.role} is not cast by the people (${c.why.slice(0, 90)}): its lines are not bound`);
    const bound = bindHandover(lines as never, binding);
    const cmp = compareToReference(bound.entries, handoverText, temps);
    const hf = resolve(root, `out/dialogue/${people.plan.id}.handover.dialogue.json`);
    await Bun.write(hf, JSON.stringify(bound.entries, null, 1) + '\n');
    console.log(`${cmp.missing.length || cmp.fewer.length ? 'FAIL' : 'ok  '} reference  ${cmp.matched} of the Handover's hand-written person entries are matched for ${Object.keys(binding).join(', ') || 'no role'}${cmp.missing.length ? `; missing ${cmp.missing.slice(0, 6).join(', ')}` : ''}${cmp.fewer.length ? `; fewer variants ${cmp.fewer.slice(0, 4).join(', ')}` : ''} → ${hf}`);
    if (cmp.missing.length || cmp.fewer.length) process.exitCode = 1;
  }
}
for (const g of checked.gaps.slice(0, 5)) console.log(`gap  ${g.rule} ${g.where}: ${g.message}`);
process.exit(checked.ok && !process.exitCode ? 0 : 1);
