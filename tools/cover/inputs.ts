// What the cover file is made from, and the hashes that say it is stale (brief 011, Pulse). The site is hashed by the parts the cover reads (its walk grid, its places, its
// schedule and the references that name the fight), not as a file: the engine's site gains fields (arms, cameras) that move no cover, and a hash of the file would call the cover stale each time.
import { createHash } from 'node:crypto';

export type Inputs = { block: string; site: string; kit: string; plan: string };
export const sha = (s: string) => createHash('sha256').update(s).digest('hex');
/** The parts of handover.site.json the cover reads. */
export const siteDigest = (siteText: string): string => {
  const s = JSON.parse(siteText);
  return sha(JSON.stringify({ grid: s.grid, places: s.places, schedule: s.schedule, fight: s.fight, getaway: s.getaway, car: s.car, rivals: s.rivals }));
};
/** `t`: the texts of the four input files. */
export const hashes = (t: Inputs): Inputs => ({ block: sha(t.block), site: siteDigest(t.site), kit: sha(t.kit), plan: sha(t.plan) });
