// usage: bun run people:view [--run r015] [--cast <file>] [--against <run|promoted|file|none>] [--plan <plan.json>] [--out <file>]
// The roster viewer: one self-contained html file (out/people/view/<id>.html) for reviewing a cast before it is promoted: the run and Keel's verdict, the checks, the Handover's
// four roles, a map of the block with a day scrubber, the tie graph, character sheets, a sortable roster, and, against another cast, who was added, removed or changed.
// With no arguments it shows the promoted cast (contracts/people/<plan id>.cast.json, held to its run first). With --run or --cast it is compared against the promoted cast
// unless --against says otherwise (`none` for no comparison). Same inputs, same bytes; nothing is written but the html.
import { mkdir } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { root } from '../rag/config';
import { buildView } from './data';
import { render } from './page';

const argv = process.argv.slice(2), opt = (k: string) => { const i = argv.indexOf(k); return i < 0 ? undefined : argv[i + 1]; };
const KNOWN = ['--run', '--cast', '--against', '--plan', '--out', '--scope'];
for (const a of argv.filter(x => x.startsWith('--'))) if (!KNOWN.includes(a)) { console.error(`unknown option ${a}; known: ${KNOWN.join(' ')}`); process.exit(2); }
for (const k of KNOWN) if (argv.includes(k) && (opt(k) === undefined || opt(k)!.startsWith('--'))) { console.error(`${k} needs a value`); process.exit(2); }

let view;
try { view = await buildView({ plan: opt('--plan'), scope: opt('--scope'), run: opt('--run'), cast: opt('--cast'), against: opt('--against') }); }
catch (e) { console.error(`FAIL ${(e as Error).message}`); process.exit(1); }
const html = render(view), out = resolve(root, opt('--out') ?? `out/people/view/${view.id}.html`);
await mkdir(dirname(out), { recursive: true });
await Bun.write(out, html);
// as `bun run people` does, M-2 on the promoted cast is shown, not enforced (r013 does not play the Handover)
const failing = view.checks.filter(c => !c.ok && !(c.id === 'M-2' && view.source.kind === 'promoted'));
console.log(`${view.persons.length} people, ${view.units.length} units · ${view.source.label}`);
console.log(`checks ${view.checks.filter(c => c.ok).length}/${view.checks.length} ok${failing.length ? `; FAILING ${failing.map(c => c.id).join(', ')}` : ''}${view.run ? ` · run ${view.run.id} ${view.run.outcome}, $${view.run.cost.usd.toFixed(3)}, ${view.run.rounds.length} rounds, Keel ${view.run.keel.verdict}` : ''}`);
if (view.compare) console.log(`against ${view.against!.label.split(' (')[0]}: +${view.compare.added.length} added, −${view.compare.removed.length} removed, ${view.compare.changed.length} changed, ${view.compare.renamed} renamed`);
for (const n of view.notices.filter(n => n.level !== 'info')) console.log(`${n.level === 'fail' ? 'FAIL' : 'note'}  ${n.title}`);
console.log(`→ ${out} (${(html.length / 1024).toFixed(0)} KB)`);
process.exit(failing.length ? 1 : 0);
