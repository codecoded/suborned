// usage: bun run levels:ranks → prints and writes out/levels/ranks.json
// Where does `standards › tram` (home of stopSpacing) rank for the old single transit query and for each objective's queries?
// Needs the index (bun run up, bun run ingest); no model call.
import { retrieve } from '../rag/retrieval';
import { objectives, OLD_QUERY } from './objectives';
import { outRoot } from './paths';

const rank = (r: Awaited<ReturnType<typeof retrieve>>) => { const i = r.hits.findIndex(h => h.section.endsWith('standards › tram')); return i < 0 ? null : i + 1; };
const old = await retrieve(OLD_QUERY, 12), result: Record<string, unknown> = { chunk: 'standards › tram', topN: 8, old: { queries: [OLD_QUERY], rank: rank(old), inTop8: (rank(old) ?? 99) <= 8, top8: old.hits.slice(0, 8).map(h => h.section) } };
for (const [agent, o] of Object.entries(objectives)) {
  const r = await retrieve(o.query, 12);
  result[agent] = { objective: o.id, queries: o.query, rank: rank(r), inTop8: (rank(r) ?? 99) <= 8, top8: r.hits.slice(0, 8).map(h => h.section) };
}
await Bun.write(`${outRoot()}/ranks.json`, JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
