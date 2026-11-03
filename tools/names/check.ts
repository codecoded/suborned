// The name check (brief 089, Splice; Sky: "See the same Name like Okafar and vale in tottally unrelated other AI built apps"): no name
// language models favour in what the player sees. Fails on any banned name in the sources (scan.ts) that the expected list does not
// hold; the expected list (names.check.json `expected`) holds the hits known on the day the check landed, until the rename (Fable)
// and the pools (Pulse) land. It cannot silently stay: an expected hit that is gone fails ("take it off"), and past `until` every
// expected hit fails as new. A lookalike (Fable's patterns: Vidal, Varma, Elias) is a warning printed for review, never a failure
// (Aster, 2026-11-02: Sky wants normal names). The generator must read the same ban list (people.rules.json names.lists.banList).
// usage: bun tools/names/check.ts [--all] [--expected]   (--all: every hit with its path; --expected: today's hits as the JSON for `expected.hits`)
import { type Ban, type Config, type Expected, type Ignore, ROOT, loadBan, match, readConfig } from './ban.ts';
import { type Str, generatorBan, present } from './scan.ts';

export type Hit = Str & { word: string; name: string; why: string; warn?: boolean };
/** hits: the failing kind (strong, reported), less the ignore list; warnings: the lookalikes; generator: the people generator's ban list path when it is not `from`. */
export type Report = { from: string; hits: Hit[]; warnings: Hit[]; fresh: Hit[]; stale: Expected[]; unused: Ignore[]; expired: boolean; sources: string[]; strings: number; generator: string | null };

export const keyOf = (h: Expected) => `${h.source}|${h.name.toLowerCase()}`;

export function hitsIn(ban: Ban, strings: Str[]): Hit[] {
  return strings.flatMap(s => match(ban, s.text, s.person, s.capitals).map(m => ({ ...s, ...m })));
}
/** An ignore entry covers a hit in its source whose path starts with `at` and whose whole string is `text`. */
const covers = (i: Ignore, h: Hit) => i.source === h.source && h.path.startsWith(i.at) && h.text === i.text;
/** Hits less the ignore list (a string the check reads that is never on screen), and the ignore entries that cover nothing on this machine. */
export function ignored(hits: Hit[], config: Config, sources: string[]): { hits: Hit[]; unused: Ignore[] } {
  return { hits: hits.filter(h => !config.ignore.some(i => covers(i, h))), unused: config.ignore.filter(i => sources.includes(i.source) && !hits.some(h => covers(i, h))) };
}

/** today: yyyy-mm-dd, the day the check runs (a parameter so the test can move it). */
export function judge(hits: Hit[], config: Config, sources: string[], today: string): Pick<Report, 'fresh' | 'stale' | 'expired'> {
  const expired = today > config.expected.until;
  const held = new Set(expired ? [] : config.expected.hits.map(keyOf)), found = new Set(hits.map(keyOf));
  const fresh = hits.filter(h => !held.has(keyOf(h)));
  const stale = expired ? [] : config.expected.hits.filter(e => sources.includes(e.source) && !found.has(keyOf(e)));
  return { fresh, stale, expired };
}

/** only: the source ids to read (the words step reads the committed ones before it restages StreamingAssets); default every present one. */
export function check(root = ROOT, today = new Date().toISOString().slice(0, 10), only?: string[]): Report {
  const config = readConfig(root), ban = loadBan(root, config), srcs = present(root).filter(s => !only || only.includes(s.id));
  const strings = srcs.flatMap(s => s.strings(root)), sources = srcs.map(s => s.id), all = hitsIn(ban, strings);
  const { hits, unused } = ignored(all.filter(h => !h.warn), config, sources), gen = generatorBan(root);
  return { from: ban.from, hits, warnings: all.filter(h => h.warn), unused, sources, strings: strings.length, generator: gen === ban.from ? null : gen ?? 'none', ...judge(hits, config, sources, today) };
}

/** What fails, one line each (empty: pass): for callers such as the batch's words step. */
export function failures(r: Report): string[] {
  return [...(r.generator ? [`the people generator reads its ban list from ${r.generator}, not ${r.from} (people.rules.json names.lists.banList)`] : []), ...(r.expired ? ['the expected list has run out (names.check.json expected.until)'] : []), ...summary(r.fresh).map(l => `banned name:${l}`),
    ...r.stale.map(e => `expected hit gone: ${e.source} ${e.name}; take it off tools/names/names.check.json expected.hits`),
    ...r.unused.map(i => `ignore entry covers nothing: ${i.source} ${i.at} "${i.text}"`)];
}

/** Today's hits as `expected.hits` entries, one per source and name. */
export function expectedOf(hits: Hit[]): Expected[] {
  const seen = new Map<string, Expected>();
  for (const h of hits) if (!seen.has(keyOf(h))) seen.set(keyOf(h), { source: h.source, name: h.name });
  return [...seen.values()].sort((a, b) => keyOf(a).localeCompare(keyOf(b)));
}

/** One line per source and name: how many strings, and the first few paths. */
export function summary(hits: Hit[], max = 3): string[] {
  const by = new Map<string, Hit[]>();
  for (const h of hits) by.set(keyOf(h), [...(by.get(keyOf(h)) ?? []), h]);
  return [...by.values()].sort((a, b) => keyOf(a[0]!).localeCompare(keyOf(b[0]!))).map(g => {
    const h = g[0]!, more = g.length > max ? ` (+${g.length - max})` : '';
    return `  ${h.source.padEnd(16)} ${h.name.padEnd(12)} ${String(g.length).padStart(4)}×  ${h.why}: ${g.slice(0, max).map(x => `${x.path} "${x.word}"`).join(', ')}${more}`;
  });
}

/** Held hits, one line per source: "text: Okafor 11, Vale 217". */
export function brief(hits: Hit[]): string[] {
  const by = new Map<string, Map<string, number>>();
  for (const h of hits) { const m = by.get(h.source) ?? new Map<string, number>(); m.set(h.name, (m.get(h.name) ?? 0) + 1); by.set(h.source, m); }
  return [...by].sort(([a], [b]) => a.localeCompare(b)).map(([src, m]) => `  ${src}: ${[...m].sort(([a], [b]) => a.localeCompare(b)).map(([n, c]) => `${n} ${c}`).join(', ')}`);
}

if (import.meta.main) {
  const t0 = performance.now(), args = process.argv.slice(2), config = readConfig(), r = check();
  if (args.includes('--expected')) { console.log(JSON.stringify(expectedOf(r.hits), null, 1)); process.exit(0); }
  const all = args.includes('--all'), held = r.hits.filter(h => !r.fresh.includes(h));
  console.log(`names: ${r.strings} strings in ${r.sources.join(', ')} against ${r.from}; ${r.hits.length} banned words, ${r.warnings.length} lookalikes`);
  if (all) for (const h of r.hits) console.log(`  ${h.source} ${h.path} "${h.word}" = ${h.name} (${h.why}): ${h.text.slice(0, 90)}`);
  if (held.length) console.log(`expected until ${config.expected.until} (brief ${config.expected.brief}; --all lists every hit):\n${(all ? summary(held) : brief(held)).join('\n')}`);
  if (r.warnings.length) console.log(`lookalikes to review (a warning, never a failure):\n${(all ? summary(r.warnings) : brief(r.warnings)).join('\n')}`);
  if (r.generator) console.error(`the people generator reads its ban list from ${r.generator}, not ${r.from}: point people.rules.json names.lists.banList at the same list`);
  if (r.expired) console.error(`the expected list ran out on ${config.expected.until}: every hit now fails; rename them or, with Aster, move the date`);
  if (r.fresh.length) console.error(`banned names in what the player sees (rename, or allow a name Sky pinned in tools/names/names.check.json \`allow\`):\n${summary(r.fresh).join('\n')}`);
  for (const e of r.stale) console.error(`expected hit gone: ${e.source} ${e.name}; take it off tools/names/names.check.json expected.hits`);
  for (const i of r.unused) console.error(`ignore entry covers nothing: ${i.source} ${i.at} "${i.text}"; take it off tools/names/names.check.json ignore`);
  const ok = !r.fresh.length && !r.stale.length && !r.unused.length && !r.generator;
  console.log(`names: ${ok ? 'pass' : 'FAIL'} in ${((performance.now() - t0) / 1000).toFixed(2)} s`);
  process.exit(ok ? 0 : 1);
}
