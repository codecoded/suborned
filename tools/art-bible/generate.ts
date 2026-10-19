// Offline, deterministic illustrated companion. No model, store checkout or Unity required.
import { createHash } from 'node:crypto';
import { readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { selectLatestGdd } from '../rag/gdd-sources';

const root = resolve(import.meta.dir, '../..');
const esc = (v: unknown) => String(v).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]!);
const digest = (v: string | Uint8Array) => createHash('sha256').update(v).digest('hex');
const inputs: Record<string, string> = {};
async function read(path: string) {
  const data = new Uint8Array(await Bun.file(resolve(root, path)).arrayBuffer());
  inputs[path] = digest(data);
  return data;
}
const text = async (path: string) => new TextDecoder().decode(await read(path));
const latest = selectLatestGdd(readdirSync(resolve(root, 'GDD')));
const gdd = await text(latest.agent.path);
await read(latest.human.path);
await read('tools/art-bible/generate.ts');
const spec = JSON.parse(await text('contracts/style/suborned.style.json'));
const archived = await text('GDD/the-suborned-art-bible-v0.15.0-2026-10-10.html');
const art = gdd.split('## §15 ART')[1]?.split('\n## §')[0];
if (!art) throw new Error('Latest agent GDD has no §15 ART');
function rule(key: string) {
  const line = art!.split('\n').find(l => l.startsWith(`${key}:`) || l.startsWith(`${key} (`));
  if (!line) throw new Error(`Missing GDD art rule: ${key}`);
  // Colons and nested parentheses can occur in the source annotation and the body.
  let depth = 0;
  for (let i = key.length; i < line.length; i++) {
    if (line[i] === '(') depth++;
    else if (line[i] === ')') depth--;
    else if (line[i] === ':' && depth === 0) return line.slice(i + 1).trim();
  }
  throw new Error(`Malformed GDD art rule: ${key}`);
}
const para = (s: unknown) => `<p>${esc(s)}</p>`;
const list = (a: unknown[]) => `<ul>${a.map(x => `<li>${esc(x)}</li>`).join('')}</ul>`;
const rules = (keys: string[]) => keys.map(k => `<article><h3>${esc(k)}</h3>${para(rule(k))}</article>`).join('');
const section = (id: string, title: string, body: string) => `<section id="${id}"><h2>${title}</h2>${body}</section>`;
const disclosure = (title: string, body: string) => `<details><summary>${title}</summary>${body}</details>`;

// Reuse the exact approved embedded image bytes. Archived prose is never a design input.
const images = new Map<string, string>();
const archiveImages: string[] = [];
for (const m of archived.matchAll(/<img\b[^>]*>/g)) {
  const src = m[0].match(/src="([^"]+)"/)?.[1];
  if (!src?.startsWith('data:image/')) throw new Error('Archive image is not embedded');
  archiveImages.push(src);
  const alt = m[0].match(/alt="([^"]+)"/)?.[1];
  if (alt) images.set(alt, src);
}
const missing = new Set<string>();
function figure(src: string, caption: string) {
  return `<figure><img src="${src}" alt="${esc(caption)}" loading="lazy"><figcaption>${esc(caption)}</figcaption></figure>`;
}
function board(paths: string[]) {
  return `<div class="board">${paths.map(p => {
    const src = images.get(p);
    if (src) return figure(src, p);
    missing.add(p);
    return `<figure class="missing"><p>Reference thumbnail unavailable in the retained archive.</p><figcaption>${esc(p)}</figcaption></figure>`;
  }).join('')}</div>`;
}
const cityManifestPath = 'briefs/008/concepts/PROVENANCE.json';
const cityManifest = JSON.parse(await text(cityManifestPath));
const city: { src: string; caption: string }[] = [];
for (const f of cityManifest.files) {
  const path = `briefs/008/concepts/${f.file}`, bytes = await read(path);
  if (digest(bytes) !== f.sha256) throw new Error(`Approved concept hash mismatch: ${path}`);
  city.push({ src: `data:image/webp;base64,${Buffer.from(bytes).toString('base64')}`, caption: `${f.shows} — ${f.file}` });
}

// Art Sky approved after the v0.15 bible, each cited by §15. Linked by relative path (committed under the .gitignore keep list),
// not embedded, so the page stays small; every file is hashed into the drift check.
const approved: { title: string; why: string; files: [string, string][] }[] = [
  { title: 'Skyline', why: '§15 reference + skyline (brief 007): the AtomicAge and Blocks sheets; ocean liner concept B, "atomic"; the Parade open to the sky, with glazed canopies over the side galleries only (from the vault concept).', files: [
    ['briefs/007/concepts/AtomicAge/contact_sheet.jpg', 'AtomicAge sheet · skyline reference'], ['briefs/007/concepts/Blocks/contact_sheet.jpg', 'Blocks sheet · skyline reference'],
    ['briefs/007/liner-B-atomic-aerial.jpg', 'Liner B, atomic · aerial'], ['briefs/007/liner-B-atomic-quay.jpg', 'Liner B, atomic · quay'],
    ['briefs/007/parade-B-open-aerial.jpg', 'Parade B, open to the sky · aerial'], ['briefs/007/parade-B-open-eye.jpg', 'Parade B, open · eye level'],
    ['briefs/007/parade-A-vault-aerial.jpg', 'Parade A · side gallery canopies only'] ] },
  { title: 'Crossings', why: '§15 crossings (brief 009): floating signal eyes over the kerb, light set flush in the road, no zebra and no landing slab ("I do like those eyes").', files: [
    ['briefs/009/concepts/crossing-future-eye.jpg', 'Crossing · eye level'], ['briefs/009/concepts/crossing-future-eye-cafe.jpg', 'Crossing by the café'],
    ['briefs/009/concepts/crossing-future-play.jpg', 'Crossing · play camera'], ['briefs/009/concepts/crossing-future-play-avenue.jpg', 'Crossing on the avenue · play camera'] ] },
  { title: 'HUD', why: '§15 hud (brief 011): finish and dock closed as built, the round 4 v10 signage kit.', files: [
    ['briefs/011/ui/round4/v10-signage-hotbar.jpg', 'v10 signage hotbar'], ['briefs/011/ui/round4/v10-signage-hotbar-take.jpg', 'v10 · take'], ['briefs/011/ui/round4/v10-signage-hotbar-fight.jpg', 'v10 · fight'] ] },
  { title: 'The cell', why: '§15 cell: visitors from a nearby timeline. The latest lineup (Sky, 2026-10-18: "this is our latest cell", brief 020) and the four portraits with the final Tech (brief 012).', files: [
    ['briefs/020/people/cell-lineup-latest.png', 'The cell · latest lineup'], ['briefs/012/cell/tech-final/TF-portraits-four.jpg', 'The four portraits · final Tech'] ] },
];
const approvedHtml: string[] = [];
for (const g of approved) {
  for (const [path] of g.files) await read(path);
  approvedHtml.push(`<h3>${esc(g.title)}</h3>${para(g.why)}<div class="board">${g.files.map(([path, caption]) => figure(`../${path}`, `${caption} — ${path}`)).join('')}</div>`);
}

// Editorial reconciliation of already-approved decisions; never rewrite the style contract.
// Full original source hashes and the exact reconciliations are shown in the page.
const undated = (s: string) => s.replace('the imagined future of 1958-1972 pushed one step further', 'the imagined future pushed one step further').replace('1960s fashion illustration', 'space-age fashion illustration');
const people = new Set(['character', 'model', 'lineup', 'versions']);
const corrections = [
  'People: remove era dates from framing, finish and Enhance guidance, as required by §15 people and concept_prompt.rule. Keep all counts, sizes and reference limits.',
  'Cell: use §15 cell instead of the legacy Enhance sentence prescribing gold. No deco motifs, gold trim or 1950s tailoring.',
  'Backdrop: present painted plates as an optional extra under §15 world/split; remove the legacy “never modelled” requirement.',
  'Models: show the exact per-type finish.model values from the style contract, which is declared authoritative. This includes GPT Image for model sheets; the GDD shorthand “All Seedream” is not accurate for that type.',
  'Palette: show the current world roles separately from the concept-sheet palette. The crossing role is retained only as a retired value; §15 crossings specifies no landing slab.',
  'Signals: use the current signal_colours.rule; no reserved-colour or ΔE gate. LoRA training history is labelled historical, with its parked status from §15 lock plan.',
];
const typeRows: string[] = [], typeBodies: string[] = [];
for (const [key, t] of Object.entries(spec.types) as [string, any][]) {
  const fin = t.finish, ref = t.references;
  typeRows.push(`<tr><th scope="row"><a href="#type-${key}">${esc(t.label)}</a></th><td>${esc(fin.model)}</td><td>${fin.size.join(' × ')}</td><td>${ref.subject === 'none' ? 'New image' : 'Optional subject'}; ${ref.style} style refs</td></tr>`);
  const line = key === 'backdrop' ? 'Optional painted sky and distant plate. The built 3D world remains the primary presentation (§15 world/split).' : people.has(key) ? undated(t.line) : t.line;
  const look = people.has(key) ? undated(fin.look) : fin.look;
  const fields = (t.fields ?? []).map((f: any) => `<h4>${esc(f.label)} · default ${esc(f.default ?? 'none')}</h4>${list((f.options ?? []).map((o: any) => `${o.label}: ${o.line ?? ''}`))}`).join('');
  const body = para(t.use) + para(line) + (t.rules ? list(t.rules) : '') + `<h4>Finish</h4>${para(look)}${fields || '<p>No additional choice fields.</p>'}` + (t.guide ? `<h4>Enhance guide</h4>${list(t.guide)}` : '') + `<h4>Reference board</h4>${board(t.board ?? [])}`;
  typeBodies.push(`<details id="type-${key}"><summary>${esc(t.label)} <span>${esc(fin.model)} · ${fin.size.join(' × ')}</span></summary>${body}</details>`);
}
const palette = (values: Record<string, any>) => `<div class="swatches">${Object.entries(values).map(([k, v]) => `<div><i style="background:${esc(v.hex)}"></i><b>${esc(k)}</b><code>${esc(v.hex)}</code><small>${k === 'crossing' ? 'Retired landing-slab role' : esc(v.group ?? v.role ?? '')}</small></div>`).join('')}</div>`;
const signals = Object.entries(spec.signal_colours).filter(([k]) => k !== 'rule').map(([k, v]) => `<span class="signal"><i style="background:${esc(v)}"></i>${esc(k)} <code>${esc(v)}</code></span>`).join('');
const guide = spec.guide.map((s: string) => s.startsWith('Only when the brief is about the player cell:') ? rule('cell') : undated(s));
// Probe rows have no alt attribute in the archive; match each to its row's named id.
const probeImages = new Map<string, string>();
for (const row of archived.matchAll(/<tr>([\s\S]*?)<\/tr>/g)) {
  const id = row[1]!.match(/^<td><code>([^<]+)<\/code>/)?.[1];
  const src = row[1]!.match(/<img src="([^"]+)"/)?.[1];
  if (id && src) probeImages.set(id, src);
}
const probes = spec.probes.items.map((p: any) => `<article><h3>${esc(p.id)} · ${esc(p.type)}</h3>${para(p.brief)}${p.fields ? para(JSON.stringify(p.fields)) : ''}${p.size ? para(`Size override: ${p.size.join(' × ')}`) : ''}${probeImages.has(p.id) ? figure(probeImages.get(p.id)!, 'Historical probe · ' + p.id) : '<p class="muted">No retained probe image.</p>'}</article>`).join('');
const version = latest.agent.version, date = latest.agent.path.match(/(\d{4}-\d{2}-\d{2})\.md$/)![1]!;
const output = `GDD/the-suborned-art-bible-v${version}-${date}.html`;
const css = `
:root{color-scheme:light;--paper:#f7f2e7;--ink:#163e42;--muted:#506568;--line:#d8d4c6;--teal:#146b66;--coral:#a93e2e}*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.6 system-ui,sans-serif}header,main,footer{max-width:1160px;margin:auto;padding:32px}header{padding-top:60px}h1{font-size:clamp(36px,6vw,68px);letter-spacing:-.055em;line-height:1.05;margin:14px 0 24px}h2{font-size:30px;letter-spacing:-.035em;line-height:1.2}h3{text-transform:capitalize;font-size:19px}h4{margin-bottom:6px}.eyebrow{color:var(--coral);font-weight:750;letter-spacing:.14em;text-transform:uppercase;font-size:12px}.lede{font-size:21px;max-width:780px}.muted,small,figcaption{color:var(--muted)}a{color:var(--teal);text-underline-offset:3px}nav{display:flex;gap:10px 24px;flex-wrap:wrap;margin:24px 0}section{border-top:1px solid var(--line);padding:28px 0;scroll-margin-top:20px}article{margin:20px 0}p,li,td,th,code,figcaption{overflow-wrap:anywhere}code{font-size:.86em}figure{margin:0}img{display:block;width:100%;height:auto}figcaption{font-size:12px;padding:10px 0}.hero,.board,.columns{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:22px}.hero{align-items:start}.hero img{border-radius:12px}.board{grid-template-columns:repeat(auto-fit,minmax(220px,1fr))}.board img{aspect-ratio:1.3;object-fit:contain;background:#e8e5dc;border-radius:8px}.board figure{padding:10px;background:#fffaf1;border:1px solid var(--line);border-radius:10px}.missing{border-style:dashed!important}.note{padding:16px 20px;border-left:4px solid var(--teal);background:#e7eee5;border-radius:0 8px 8px 0}details{border:1px solid var(--line);padding:16px 20px;margin:12px 0;border-radius:10px;background:#fffaf1}summary{cursor:pointer;font-size:18px;font-weight:650}summary span{font-size:12px;color:var(--muted);font-weight:400;margin-left:12px}table{width:100%;border-collapse:collapse;font-size:14px}th,td{text-align:left;padding:12px 8px;border-bottom:1px solid var(--line);vertical-align:top}th{font-weight:600}.table{overflow:auto}.swatches{display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:12px}.swatches>div{padding:12px;background:#fffaf1;border:1px solid var(--line);border-radius:8px}.swatches i{height:45px;display:block;border-radius:4px;border:1px solid #0002;margin-bottom:8px}.swatches b,.swatches code,.swatches small{display:block;font-size:12px}.signal{display:inline-flex;align-items:center;gap:8px;margin:10px 16px 10px 0}.signal i{width:20px;height:20px;border-radius:50%}li{margin:9px 0}footer{font-size:13px;border-top:1px solid var(--line)}@media(max-width:600px){header,main,footer{padding:22px}header{padding-top:36px}.hero,.columns{grid-template-columns:1fr}.board{grid-template-columns:1fr}summary span{display:block;margin-left:0}h2{font-size:25px}details{padding:12px}.lede{font-size:18px}}@media print{details{break-inside:avoid}nav{display:none}}`;
const contents = [
  section('direction', 'The city we are building', `<div class="hero">${figure(city[0]!.src, city[0]!.caption)}<div>${para(rule('era'))}${para(rule('world'))}<div class="note">${para(rule('people'))}</div></div></div>${rules(['city','reference','skyline'])}${disclosure('Eight approved street-level reference paintings', para('These paintings establish the street-level look. Their people and HUD are historical concept imagery; current character and interface rules below take precedence.') + `<div class="board">${city.map(c => figure(c.src,c.caption)).join('')}</div>`)}`),
  section('approved', 'Approved since the v0.15 bible', para('Concept art Sky has approved since 10 October, each cited by a §15 rule. These images are linked from the repository (briefs/), so open this page from a checkout.') + approvedHtml.join('')),
  section('rules', 'Materials, people and readability', rules(['materials','cell','security + institutions','robots','props','crossings','camera','hud']) + `<h3>Signals share the city’s colours</h3>${signals}${para(spec.signal_colours.rule)}${rules(['always','avoid'])}`),
  section('palette', 'World palette and concept palette', para(spec.palette_world.rule) + para(spec.palette_world.status) + palette(spec.palette_world.roles) + disclosure('Concept-sheet palette · separate from world surfaces', palette(spec.palette) + para('Concept gold is not permission for large gold surfaces. Follow the world rule and the GDD’s brass guidance.')) + disclosure('Full GDD palette notes', para(art.split('\n').filter(l => l.startsWith('- ')).join('\n')))),
  section('prompts', 'Build a prompt from the brief', rules(['concept art first']) + `<div class="columns"><article><h3>Places, objects and machines</h3>${para(spec.concept_prompt.era)}${para(spec.concept_prompt.city)}</article><article><h3>People, character, lineup, model and versions</h3>${para(spec.concept_prompt.people)}<div class="note">No era dates in people prompts. Use the type’s framing, finish and reference policy below. For the cell, apply its separate design lineage.</div></article></div>${list(spec.concept_prompt.avoid)}${para(spec.concept_prompt.rule)}${list(spec.rules.all.concat(spec.rules.meta ?? []))}${disclosure('Enhance guide · a menu, not a checklist', list(guide))}`),
  section('types', 'Ten image types and their configured options', para('Model identifiers below are the checked-in project configuration, not a claim about provider availability. No model calls were made to generate this page.') + `<div class="table"><table><thead><tr><th>Type</th><th>Configured model</th><th>Pixels</th><th>Reference policy</th></tr></thead><tbody>${typeRows.join('')}</tbody></table></div>` + para('Open a type for its framing, finish, choice fields and approved reference board. Retained concept images are judged under the current rules above.') + typeBodies.join('')),
  section('pipeline', 'From concept to the built world', rules(['split','pipeline','probes','lock plan']) + disclosure('Regression probes · historical images and briefs', para(spec.probes.purpose) + para('These retained probe briefs predate some current rules (including the cell’s no-gold direction). Apply the current rules above before any new probe run; these are not ready-to-send prompts or a fresh validation result.') + `<div class="columns">${probes}</div>`) + disclosure('Historical LoRA training record · parked', para(spec.style_lock.plan))),
  section('sources', 'Sources and reconciliation', para(`Companion to GDD v${version} (${date}); style contract v${spec.version} (${spec.date}). Existing explicit GDD decisions resolve the legacy prose listed below. Configured options come directly from the style contract.`) + list(corrections) + `<h3>Reference coverage</h3>${para(`${archiveImages.length} original embedded image occurrences retained (including historical probes), plus eight approved street-level paintings. Two board assets had no thumbnail in the old bible; their slots remain explicitly labelled.`)}${list([...missing])}` + disclosure('Source hashes · SHA-256', `<div class="table"><table><tbody>${Object.entries(inputs).map(([p,h]) => `<tr><th>${esc(p)}</th><td><code>${h}</code></td></tr>`).join('')}</tbody></table></div>`) + para('Regenerate with bun run art:bible. Check exact output and source drift with bun run art:bible --check. The check does not claim to resolve future design contradictions automatically.')),
].join('\n');
const html = `<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>The Suborned · Art bible v${version}</title><style>${css}</style></head><body><header><div class="eyebrow">The Suborned / Art direction / GDD v${version}</div><h1>A bright city.<br>A coercive machine.</h1><p class="lede">The current illustrated guide to the world, its people and the images we make to build it.</p><p class="muted">GDD ${date} · Style contract v${spec.version} · Approved reference imagery preserved</p><nav>${[['direction','Direction'],['approved','Approved'],['rules','Rules'],['palette','Palette'],['prompts','Prompts'],['types','Image options'],['pipeline','Pipeline'],['sources','Sources']].map(([id,label])=>`<a href="#${id}">${label}</a>`).join('')}</nav></header><main>${contents}</main><footer>Generated companion · Edit the approved sources and regenerate. Historical images do not override current rules.</footer><script>document.querySelectorAll('a[href^="#type-"]').forEach(a=>a.addEventListener('click',()=>{const d=document.querySelector(a.getAttribute('href'));if(d)d.open=true}));</script></body></html>\n`;
// Every retained image must survive, even when repeated by several type boards.
for (const src of archiveImages) if (!html.includes(src)) throw new Error('An archived approved image was lost');
if (process.argv.includes('--check')) {
  if (!await Bun.file(resolve(root, output)).exists() || await Bun.file(resolve(root, output)).text() !== html) {
    console.error(`STALE ${output}; run bun run art:bible`); process.exit(1);
  }
  console.log(`PASS ${output}: sources, options and embedded imagery match`);
} else {
  await Bun.write(resolve(root, output), html);
  console.log(`WROTE ${output} (${Buffer.byteLength(html)} bytes)`);
}
