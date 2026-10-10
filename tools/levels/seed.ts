// Labelled seeds for the critic demo (brief 001). Kept out of apply.ts on purpose: that file is in the tools hash, and a seed is not checker code.
import type { Pulse } from './schemas';

export const SEEDS = ['station-on-plot', 'fare-claim', 'fare-claim-uncited'] as const;
export type Seed = (typeof SEEDS)[number];
/** The seeded agent: whose round-1 call record carries the label (replay reads it from here). */
export const SEEDED_AGENT = { 'station-on-plot': 'vantage', 'fare-claim': 'pulse', 'fare-claim-uncited': 'pulse' } as const;
export const FARE_CLAIM = 'Boarding price: a flat $5 fare per ride, charged at the gate, plus the wait (GDD §02 travel).';
/** fare-claim-uncited: the same false claim with no citation, so nothing in Keel's prompt points at §02; Keel can only catch it from its own retrieval. */
export const FARE_CLAIM_UNCITED = 'Boarding price: a flat $5 fare per ride, charged at the gate, plus the wait.';
export const isFareSeed = (s: unknown): s is 'fare-claim' | 'fare-claim-uncited' => s === 'fare-claim' || s === 'fare-claim-uncited';
export const fareClaimText = (s: 'fare-claim' | 'fare-claim-uncited') => (s === 'fare-claim' ? FARE_CLAIM : FARE_CLAIM_UNCITED);

/** fare-claim: one false claim in Pulse's notes (GDD §02 travel: price = time only, no fare). Content is wrong, not the citation: if Pulse retrieved the travel passage its id is added to sources, else sources are untouched. Lines, journeys and everything derived are untouched. Pure, so replay can recompute it. */
export function seedFareClaim(p: Pulse, hits: { key: string; text?: string }[], cite = true): { out: Pulse; note: string } {
  if (!cite) {
    const out = structuredClone(p);
    out.notes = out.notes.length >= 8 ? [...out.notes.slice(0, 7), FARE_CLAIM_UNCITED] : [...out.notes, FARE_CLAIM_UNCITED];
    return { out, note: 'added a false, uncited fare claim to Pulse\'s notes (sources untouched)' };
  }
  const out = structuredClone(p), travel = hits.find(h => /Price = time only/.test(h.text ?? ''))?.key;
  out.notes = out.notes.length >= 8 ? [...out.notes.slice(0, 7), FARE_CLAIM] : [...out.notes, FARE_CLAIM];
  if (travel && !out.sources.includes(travel)) out.sources = [...out.sources, travel];
  return { out, note: `added a false fare claim to Pulse's notes${travel ? `, citing the §02 travel passage ${travel}` : ' (the §02 travel passage was not retrieved; sources untouched)'}` };
}
