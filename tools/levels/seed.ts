// Labelled seeds for the critic demo (brief 001). Kept out of apply.ts on purpose: that file is in the tools hash, and a seed is not checker code.
import type { Pulse } from './schemas';

export const SEEDS = ['station-on-plot', 'fare-claim'] as const;
export type Seed = (typeof SEEDS)[number];
/** The seeded agent: whose round-1 call record carries the label (replay reads it from here). */
export const SEEDED_AGENT = { 'station-on-plot': 'vantage', 'fare-claim': 'pulse' } as const;
export const FARE_CLAIM = 'Boarding price: a flat $5 fare per ride, charged at the gate, plus the wait (GDD §02 travel).';

/** fare-claim: one false claim in Pulse's notes (GDD §02 travel: price = time only, no fare). Content is wrong, not the citation: if Pulse retrieved the travel passage its id is added to sources, else sources are untouched. Lines, journeys and everything derived are untouched. Pure, so replay can recompute it. */
export function seedFareClaim(p: Pulse, hits: { key: string; text?: string }[]): { out: Pulse; note: string } {
  const out = structuredClone(p), travel = hits.find(h => /Price = time only/.test(h.text ?? ''))?.key;
  out.notes = out.notes.length >= 8 ? [...out.notes.slice(0, 7), FARE_CLAIM] : [...out.notes, FARE_CLAIM];
  if (travel && !out.sources.includes(travel)) out.sources = [...out.sources, travel];
  return { out, note: `added a false fare claim to Pulse's notes${travel ? `, citing the §02 travel passage ${travel}` : ' (the §02 travel passage was not retrieved; sources untouched)'}` };
}
