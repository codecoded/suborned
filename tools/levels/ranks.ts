// usage: bun run levels:ranks [--evidence] → prints and writes out/levels/ranks.json (--evidence: also merges courses/a04-content-pipeline/evidence/ranks.json)
// Where do `standards › tram` (home of stopSpacing) and `§02 … travel` (boarding priced in time) rank for each objective's queries?
// `travelBefore` = the Pulse/Keel queries as they stood before the v0.15 §02 fix (BEFORE). Needs the index (bun run up, bun run ingest); no model call.
import { retrieve } from '../rag/retrieval';
import { objectives, OLD_QUERY, TRAM_STANDARDS, SKY_STANDARDS } from './objectives';
import { outRoot } from './paths';

type R = Awaited<ReturnType<typeof retrieve>>;
const TRAM = (s: string) => s.endsWith('standards › tram'), TRAVEL = (s: string) => s.startsWith('§02') && s.endsWith('travel');
const rank = (r: R, f: (s: string) => boolean) => { const i = r.hits.findIndex(h => f(h.section)); return i < 0 ? null : i + 1; };
const entry = (queries: string[], r: R, f: (s: string) => boolean) => ({ queries, rank: rank(r, f), inTop8: (rank(r, f) ?? 99) <= 8, top8: r.hits.slice(0, 8).map(h => h.section) });
const BEFORE: Record<string, string[]> = {
  pulse: [TRAM_STANDARDS, 'timetable headway dwell run times journey times between stations', 'the cell rides trams and sky rail between stops and stations; boarding has no price in the GDD; open question for Motive and Sky'],
  keel: [TRAM_STANDARDS, SKY_STANDARDS, 'plan checker rules: no dead ends, every area in reach, network connected, station spacing, order', 'rulebook: each event needs, changes, witnesses; unknown is rejected'],
};
const CIRCULAR = 'boarding is fast travel priced in time only: wait for the next service plus the ride'; // queries 2 and 3 of the v0.15 fix paraphrased the target line: retrieval proved nothing
const circular: Record<string, string[]> = { pulse: [TRAM_STANDARDS, 'timetable headway dwell run times journey times between stations', CIRCULAR], keel: [TRAM_STANDARDS, SKY_STANDARDS, 'rulebook: each event needs, changes, witnesses; unknown is rejected', CIRCULAR] };
const old = await retrieve(OLD_QUERY, 12), result: Record<string, any> = { chunk: 'standards › tram', topN: 8, old: { chunk: 'standards › tram', ...entry([OLD_QUERY], old, TRAM) } }, travelBefore: Record<string, unknown> = {};
for (const [agent, o] of Object.entries(objectives)) {
  const r = await retrieve(o.query, 12), t = entry([o.query].flat(), r, TRAVEL);
  result[agent] = { objective: o.id, ...entry([o.query].flat(), r, TRAM), travel: { rank: t.rank, inTop8: t.inTop8 } };
}
for (const [agent, q] of Object.entries(BEFORE)) { const t = entry(q, await retrieve(q, 12), TRAVEL); travelBefore[agent] = { queries: q, rank: t.rank, inTop8: t.inTop8, top8: t.top8 }; }
result.travel = { chunk: '§02 LOOP + RESOURCES (travel)', topN: 8, before: travelBefore, after: Object.fromEntries(['pulse', 'keel'].map(a => [a, { queries: [objectives[a as 'pulse'].query].flat(), ...result[a].travel }])) };
const circularRanks = Object.fromEntries(await Promise.all(Object.entries(circular).map(async ([a, q]) => { const t = entry(q, await retrieve(q, 12), TRAVEL); return [a, { queries: q, rank: t.rank, inTop8: t.inTop8, top8: t.top8 }]; })));
result.travel.circular = circularRanks;
const text = JSON.stringify(result, null, 2) + '\n';
await Bun.write(`${outRoot()}/ranks.json`, text);
if (process.argv.includes('--evidence')) {
  const f = 'courses/a04-content-pipeline/evidence/ranks.json', prev = await Bun.file(f).json().catch(() => ({}));
  await Bun.write(f, JSON.stringify({ ...prev, ...result }, null, 2) + '\n');
}
console.log(JSON.stringify({ travel: result.travel, tram: Object.fromEntries(Object.entries(result).filter(([k]) => ['old', 'datum', 'vantage', 'pulse', 'keel'].includes(k)).map(([k, v]) => [k, v.rank])) }, (k, v) => (k === 'top8' || k === 'queries' ? undefined : v), 1));
