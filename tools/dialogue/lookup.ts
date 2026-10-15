// The reference lookups for suborned.lines/1 (the host ports these, as it ports text.schema's lookup and pickIndex): a person's entries are text.json `dialogue` entries whose speaker is
// the person, so the same rules find and pick a line: the entry naming the most of the query's fields wins, and the n-th use of a key is fnv1a32(seed|key|i) mod count, never the same
// variant twice running. The key carries the person's id (for the Scout's read-outs, as `about`), so two people with one table do not say the same thing at the same moment.
import type { Lines, LineEntry, PersonLines } from '../../contracts/dialogue/voices.schema';
import { lineKey, lookup, pickIndex, type Entry, type Query } from '../../contracts/missions/handover/text.schema';

export type LineQuery = { event: string; verb?: string; outcome?: string; temperament?: string; speaker?: 'person' | 'scout'; side?: 'caller' | 'summoned' };
const personOf = (f: Pick<Lines, 'persons'>, id: string): PersonLines | undefined => f.persons.find(p => p.id === id);
/** A person's entries as text.json entries (speaker `scout` entries carry `about`); the types are text.schema's, the speaker strings are this file's. */
const asEntries = (p: PersonLines, speaker: 'person' | 'scout'): Pick<{ dialogue: Entry[] }, 'dialogue'> => ({
  dialogue: p.entries.filter(e => e.speaker === speaker).map(e => ({ event: e.event, ...(e.verb ? { verb: e.verb } : {}), ...(e.outcome ? { outcome: e.outcome } : {}), ...(e.temperament ? { temperament: e.temperament } : {}), ...(e.side ? { about: e.side } : {}), speaker: speaker === 'scout' ? 'scout' : 'fixer', lines: e.lines }) as unknown as Entry),
});
const queryOf = (id: string, q: LineQuery): Query => ({ event: q.event, verb: q.verb, outcome: q.outcome, temperament: q.temperament, speaker: q.speaker === 'scout' ? 'scout' : 'fixer', about: q.speaker === 'scout' ? id : q.side });

export function lookupLine(f: Pick<Lines, 'persons'>, id: string, q: LineQuery): LineEntry | undefined {
  const p = personOf(f, id);
  if (!p) return undefined;
  const sp = q.speaker ?? 'person', e = lookup(asEntries(p, sp), queryOf(id, q));
  return e && p.entries.find(x => x.speaker === sp && x.event === e.event && x.verb === e.verb && x.outcome === e.outcome && x.temperament === e.temperament && (sp === 'scout' || x.side === e.about));
}
/** The key a use is counted under: text.schema's lineKey with the person as the speaker (the Scout's read-outs: `about` the person). */
export const personKey = (id: string, q: LineQuery): string => lineKey(q.speaker === 'scout' ? { event: q.event, verb: q.verb, speaker: 'scout', outcome: q.outcome, temperament: q.temperament, about: id } : { event: q.event, verb: q.verb, speaker: id, outcome: q.outcome, temperament: q.temperament, about: q.side });
export function pickPersonLine(f: Pick<Lines, 'persons'>, seed: string, id: string, q: LineQuery, n: number): string | undefined {
  const e = lookupLine(f, id, q);
  return e ? e.lines[pickIndex(seed, personKey(id, q), n, e.lines.length)] : undefined;
}
/** A person's scene of a verb (and tie), one variant picked by the n-th use: its beats, speaker ids as expanded, and the Learned line (`{temperament}` for the host to fill). */
export function playPersonScene(f: Pick<Lines, 'persons'>, seed: string, id: string, q: { verb: 'listen' | 'watch' | 'call'; tie?: string }, n: number) {
  const s = personOf(f, id)?.scenes.find(x => x.verb === q.verb && (q.tie === undefined || x.tie === q.tie));
  if (!s) return undefined;
  const v = s.variants[pickIndex(seed, `scene/${q.verb}/${s.tie}/${id}`, n, s.variants.length)]!;
  return { beats: v.beats, result: s.result, learns: s.learns, with: s.with };
}
