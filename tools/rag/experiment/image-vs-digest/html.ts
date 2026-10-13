// usage: bun tools/rag/experiment/image-vs-digest/html.ts      → out/review/007F-image-vs-digest.html (self-contained: the sheets are embedded)
// Reads the committed evidence in briefs/007/image-vs-digest/ (outputs, stripped call records, digest, grading) and lays the two arms side by side.
import { resolve } from 'node:path';
import { root } from '../../config';
import { imagePath } from '../../images';
import { ARMS, EVIDENCE, LISTS, SHEETS, type Arm, type Description } from './spec';

type Rec = {
  arm: Arm; costUsd: number | null; seconds: number; model: string; tools: string[]; usage: Record<string, number>;
  images: { path: string; bytes: number; sha256: string }[];
};
type Verdict = [grade: string, note: string] | null;
type Grading = { reader: string; scale: Record<string, string>; checks: { topic: string; image: Verdict; digest: Verdict }[]; verdict: string[] };

const esc = (s: unknown) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const read = async <T>(file: string) => (await Bun.file(resolve(root, `${EVIDENCE}/${file}`)).json()) as T;
const MICRO_TEST_USD = 0.0011; // the earlier mechanism check (Haiku, one small image), kept out of the comparison

const [image, digest, calls, grading] = await Promise.all([
  read<Description>('image.output.json'), read<Description>('digest.output.json'), read<Rec[]>('calls.json'), read<Grading>('grading.json'),
]);
const digestText = await Bun.file(resolve(root, `${EVIDENCE}/digest.md`)).text();

// numbers
const of = (arm: Arm) => calls.filter(c => c.arm === arm);
const sum = (arm: Arm, f: (c: Rec) => number) => of(arm).reduce((s, c) => s + f(c), 0);
const usage = (key: string) => (c: Rec) => c.usage[key] ?? 0;
const inputTokens = (c: Rec) => usage('input_tokens')(c) + usage('cache_creation_input_tokens')(c) + usage('cache_read_input_tokens')(c);
const cost = (arm: Arm) => sum(arm, c => c.costUsd ?? 0);
const total = ARMS.reduce((s, arm) => s + cost(arm), 0);

// pieces
const TITLES: Record<string, string> = { towerTypes: 'Tower types', crowns: 'Crowns', railMeetsBuilding: 'How the rail meets buildings', composition: 'Composition' };
const chips = (tiles: string[]) => tiles.map(t => `<code>${esc(t)}</code>`).join(' ');
const entry = (e: Description['towerTypes'][number]) => `<div class="e"><b>${esc(e.name)}</b> ${chips(e.tiles)}<p>${esc(e.claim)}</p></div>`;
const section = (title: string, n: number, body: string) => `<h4>${title} (${n})</h4>${body}`;

function column(arm: Arm, d: Description) {
  const lists = LISTS.map(k => section(TITLES[k]!, d[k].length, d[k].map(entry).join('')));
  const open = section('Not decidable', d.notDecidable.length, `<ul>${d.notDecidable.map(x => `<li>${esc(x)}</li>`).join('')}</ul>`);
  return `<div class="col"><h3>${arm === 'image' ? 'Image arm' : 'Digest arm'}</h3>${lists.join('')}${open}</div>`;
}

const pill = (v: Verdict) => (v ? `<span class="pill ${v[0]}">${v[0]}</span> ${esc(v[1])}` : '<span class="pill none">no claim</span>');
const checkRow = (c: Grading['checks'][number]) => `<tr><td>${esc(c.topic)}</td><td>${pill(c.image)}</td><td>${pill(c.digest)}</td></tr>`;

const callRow = (arm: Arm) => {
  const cells = [arm, of(arm)[0]?.model, of(arm).length, sum(arm, inputTokens), sum(arm, usage('cache_read_input_tokens')), sum(arm, usage('output_tokens')),
    `${sum(arm, c => c.seconds)} s`, `$${cost(arm).toFixed(4)}`, JSON.stringify(of(arm)[0]?.tools)];
  return `<tr>${cells.map(c => `<td>${esc(c)}</td>`).join('')}</tr>`;
};

const pins = (of('image')[0]?.images ?? []).map(i => `<li><code>${esc(i.path)}</code> ${i.bytes} bytes, sha256 <code>${i.sha256}</code></li>`).join('');

async function sheet(path: string) {
  const data = Buffer.from(await Bun.file(imagePath(path)).bytes()).toString('base64');
  return `<figure><img alt="${esc(path)}" src="data:image/jpeg;base64,${data}"><figcaption>${esc(path.split('/').slice(-2).join('/'))}</figcaption></figure>`;
}

const css = `
body { font: 15px/1.5 system-ui, sans-serif; max-width: 1500px; margin: 2rem auto; padding: 0 1.2rem; color: #1b2430 }
h1 { margin-bottom: .2rem } code { background: #eef1f5; padding: 0 .25em; border-radius: 3px; font-size: .85em }
table { border-collapse: collapse; margin: .6rem 0 } td, th { border: 1px solid #cfd6df; padding: .3rem .6rem; text-align: left; vertical-align: top } th { background: #f3f5f8 }
.grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1.4rem; align-items: start }
.col { background: #fafbfc; border: 1px solid #dde3ea; border-radius: 6px; padding: .2rem 1rem 1rem }
.e { margin: .5rem 0; padding-bottom: .4rem; border-bottom: 1px dotted #d5dbe3 } .e p { margin: .2rem 0 0 }
h4 { margin: 1.1rem 0 .2rem; border-bottom: 2px solid #1b2430 }
.pill { display: inline-block; padding: 0 .5em; border-radius: 1em; font-size: .8em; font-weight: 600; color: #fff }
.right { background: #2e7d4f } .partial { background: #b7791f } .wrong { background: #b83232 } .missing { background: #6b7785 } .none { background: #9aa5b1 }
.verdict { background: #fff8e6; border: 1px solid #e8d49a; border-radius: 6px; padding: .4rem 1rem }
figure { margin: .5rem 0 } img { max-width: 100% } pre { white-space: pre-wrap; background: #f3f5f8; padding: .8rem; border-radius: 6px } .note { color: #52606d }`;

const cached = sum('digest', usage('cache_read_input_tokens'));
const larger = sum('image', inputTokens) - sum('digest', inputTokens);
const html = `<!doctype html><html lang="en"><meta charset="utf-8"><title>007 F: image input against a text digest</title><style>${css}</style>
<h1>007 F: do the pictures add anything a digest loses?</h1>
<p class="note">Splice, tools engineer. One Datum call per arm (world-planner card, opus, effort medium, no tools, no retrieved passages), same task and schema.
The arms differ only in the form of the source: the two concept sheets as images, or a per-tile text digest of the same sheets written beforehand. One call each, no repeat.</p>
<div class="verdict"><h2>Answer</h2>${grading.verdict.map(p => `<p>${esc(p)}</p>`).join('')}</div>
<h2>Cost and calls</h2>
<table><tr><th>arm</th><th>model</th><th>calls</th><th>input tokens</th><th>of which cache read</th><th>output tokens</th><th>time</th><th>cost</th><th>tools (init)</th></tr>
${ARMS.map(callRow).join('')}<tr><td colspan="7"><b>Comparison total</b></td><td colspan="2"><b>$${total.toFixed(4)}</b></td></tr></table>
<p class="note">The image arm paid for the card from cold (cache creation); the digest arm ran second and read ${cached} card tokens from cache, so its cost is a little flattered.
The image arm's input was ${larger} tokens larger even though the digest arm carries the digest as text (inferred: the two sheets cost about 7.5 thousand input tokens).
An earlier mechanism check (Haiku, one small image, $${MICRO_TEST_USD}) is not part of the comparison. Both calls listed exactly one tool, StructuredOutput.</p>
<p>Images pinned in the image call record (sha256 only; the run replays offline without the pictures):</p><ul>${pins}</ul>
<h2>Checked against the sheets (Splice's reading)</h2><p class="note">${esc(grading.reader)}</p>
<p>${Object.entries(grading.scale).map(([k, v]) => `<span class="pill ${k}">${k}</span> ${esc(v)}`).join(' &nbsp; ')}</p>
<table><tr><th>on the sheet</th><th>image arm</th><th>digest arm</th></tr>${grading.checks.map(checkRow).join('')}</table>
<h2>The two answers, side by side</h2><div class="grid">${column('image', image)}${column('digest', digest)}</div>
<h2>The digest the digest arm was given</h2><details><summary>per-tile digest, written by Splice</summary><pre>${esc(digestText)}</pre></details>
<h2>The sheets</h2><details><summary>the two sheets as sent</summary>${(await Promise.all(SHEETS.map(sheet))).join('')}</details></html>`;

await Bun.write(resolve(root, 'out/review/007F-image-vs-digest.html'), html);
console.log(`out/review/007F-image-vs-digest.html ${(html.length / 1e6).toFixed(2)} MB`);
