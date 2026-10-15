// Binding the Handover's four roles to generated people (brief 012): the lines of the person cast as Inez, the driver, the door guard and Vale become
// text.json `dialogue` entries for the speakers waitress, driver, guard and councillor, so the generator can be held to the hand-written text
// (compareToReference) and the host could read generated lines through the lookup it already has. The Handover's own moments (the guard lured, a car at a camera)
// stay in text.json. Nothing here edits text.json.
import { Entry, type TextFile } from '../../contracts/missions/handover/text.schema';
import type { Lines } from '../../contracts/dialogue/voices.schema';
import { CATALOGUE, REFERENCE } from './fixture';

export const ROLES = ['waitress', 'driver', 'guard', 'councillor'] as const;
export type Role = (typeof ROLES)[number];
export type Binding = Partial<Record<Role, string>>;
/** Which end of the lovers' call each role is in the Handover: Inez calls, the guard is called. */
const SIDE: Partial<Record<Role, 'caller' | 'summoned'>> = { waitress: 'caller', guard: 'summoned' };

export function bindHandover(lines: Lines, binding: Binding): { entries: Entry[]; problems: string[] } {
  const entries: Entry[] = [], problems: string[] = [];
  for (const role of ROLES) {
    const id = binding[role];
    if (!id) continue;
    const p = lines.persons.find(x => x.id === id);
    if (!p) { problems.push(`${role}: ${id} has no lines in this file`); continue; }
    for (const e of p.entries) {
      const base = { ...(e.verb ? { verb: e.verb } : {}), ...(e.outcome ? { outcome: e.outcome } : {}), ...(e.temperament ? { temperament: e.temperament } : {}) };
      if (e.speaker === 'scout') { entries.push(Entry.parse({ event: e.event, ...base, speaker: 'scout', about: role, lines: e.lines })); continue; }
      if (e.event === 'summon') {
        if (SIDE[role] !== e.side) continue;
        entries.push(Entry.parse({ event: e.outcome === 'recalled' ? 'guard-recalled' : 'summon-refused', ...(e.outcome === 'recalled' ? {} : { outcome: e.outcome }), speaker: role, lines: e.lines }));
        continue;
      }
      entries.push(Entry.parse({ event: e.event, ...base, speaker: role, lines: e.lines }));
    }
  }
  return { entries, problems };
}

/** What the hand-written text gives the bound people and the generated lines do not: per role, the reference entries of the catalogue events for the person's own
 *  temperament (and none), by key, and the ones with more variants than the generated entry has. */
export function compareToReference(entries: readonly Entry[], text: Pick<TextFile, 'dialogue'>, temperaments: Partial<Record<Role, string>>): { missing: string[]; fewer: string[]; matched: number } {
  const missing: string[] = [], fewer: string[] = [];
  let matched = 0;
  for (const role of ROLES) {
    const t = temperaments[role];
    if (!t) continue;
    const speaker = role, group = Object.entries(REFERENCE).find(([, v]) => v === speaker)?.[0];
    if (!group) continue;
    for (const r of text.dialogue.filter(e => e.speaker === speaker && (CATALOGUE as readonly string[]).includes(e.event) && (!e.temperament || e.temperament === t))) {
      const mine = entries.find(e => e.speaker === speaker && e.event === r.event && e.verb === r.verb && e.outcome === r.outcome && e.temperament === r.temperament);
      const key = `${role}/${[r.event, r.verb, r.outcome, r.temperament].filter(Boolean).join('/')}`;
      if (!mine) missing.push(key); else { matched++; if (mine.lines.length < r.lines.length) fewer.push(`${key}: ${mine.lines.length} < ${r.lines.length}`); }
    }
  }
  return { missing, fewer, matched };
}
