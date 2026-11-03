// Brief 085: proves each excerpt is word for word in the file it was fetched into, and records the proof. Needs the downloads under out/scratch/fable/src/<slug>/source.txt (git-ignored); refetch from sources.json if they are gone.
//   bun tools/dialogue/exemplars/verify.ts
import { createHash } from 'node:crypto';
import { resolve } from 'node:path';
import { root } from '../../rag/config';
import { LAST_PD_YEAR, inSource, loadJobs, loadSources, normalise, stripHtml } from './exemplars';

export const sha = (s: string | Uint8Array) => createHash('sha256').update(s).digest('hex');
if (import.meta.main) {
  const sources = loadSources(), files = new Map<string, { text: string; sha256: string }>();
  for (const s of sources) {
    const f = Bun.file(resolve(root, `out/scratch/fable/src/${s.slug}/source.txt`));
    if (!(await f.exists())) { console.error(`missing download: ${s.slug}, ${s.url}`); process.exit(1); }
    const raw = await f.text();
    files.set(s.slug, { text: s.format === 'html' ? stripHtml(raw) : raw, sha256: sha(await f.bytes()) });
  }
  let bad = 0; const rows: { id: string; job: string; source: string; sha256: string; words: number }[] = [];
  for (const s of sources) if (Math.max(s.published, s.translationPublished ?? 0) > LAST_PD_YEAR) { console.log(`NOT PUBLIC DOMAIN ${s.slug}`); bad++; }
  for (const j of loadJobs()) for (const e of j.excerpts) {
    const f = files.get(e.source);
    if (!f) { console.log(`NO SOURCE ${e.id}: ${e.source}`); bad++; continue; }
    if (!inSource(e.text, f.text)) { console.log(`NOT FOUND ${e.id}`); bad++; continue; }
    for (const m of e.moves) if (!normalise(e.text).includes(normalise(m.at))) { console.log(`NOTE ANCHOR NOT IN EXCERPT ${e.id}: ${m.at}`); bad++; }
    rows.push({ id: e.id, job: j.job, source: e.source, sha256: sha(normalise(e.text)), words: normalise(e.text).split(' ').length });
  }
  if (bad) { console.log(`${bad} failures`); process.exit(1); }
  await Bun.write(resolve(root, 'tools/dialogue/exemplars/verified.json'), JSON.stringify({ checked: 'whitespace, Gutenberg _ marks and word joiners ignored; every other character must match', sources: sources.map(s => ({ slug: s.slug, url: s.url, sha256: files.get(s.slug)!.sha256 })), excerpts: rows }, null, 1) + '\n');
  console.log(`${rows.length} excerpts found verbatim in ${new Set(rows.map(r => r.source)).size} fetched files; ${rows.reduce((n, r) => n + r.words, 0)} words`);
}
