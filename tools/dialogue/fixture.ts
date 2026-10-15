// The Handover's hand-written words (contracts/missions/handover/text.json) as fixture tables: the reference the generator must match or beat (brief 012).
// The Handover casts a café server (Inez), the official driver, the door guard and Councillor Vale; their lines are the reference for the voice groups
// counter, driver, security and official. Only the situations every person can be in are taken (reply, confirm, questioned, gunfire); the mission's own
// moments (the guard lured, the car crossing a camera) stay in text.json, which the game reads as it always has.
import type { Cell, Group, SceneT } from '../../contracts/dialogue/voices.schema';
import type { TextFile } from '../../contracts/missions/handover/text.schema';
import { TEMPERAMENTS } from './rules';

/** The Handover speaker whose lines are the reference for a voice group. */
export const REFERENCE: Record<string, string> = { counter: 'waitress', driver: 'driver', security: 'guard', official: 'councillor' };
/** The events of text.json every person can speak (the generator's catalogue). */
export const CATALOGUE = ['reply', 'confirm', 'questioned', 'saw-gunfire', 'gunfire'] as const;

export type Reference = { cells: Cell[]; watch: string[]; scenes: SceneT[] };

export function referenceOf(text: Pick<TextFile, 'dialogue' | 'scenes'>, group: string): Reference | undefined {
  const speaker = REFERENCE[group];
  if (!speaker) return undefined;
  const cells: Cell[] = text.dialogue.filter(e => e.speaker === speaker && (CATALOGUE as readonly string[]).includes(e.event)).map(e => ({
    event: e.event, ...(e.verb ? { verb: e.verb } : {}), ...(e.outcome ? { outcome: e.outcome } : {}), ...(e.temperament && (TEMPERAMENTS as readonly string[]).includes(e.temperament) ? { temperament: e.temperament } : {}), lines: [...e.lines],
  }));
  const watch = text.dialogue.find(e => e.event === 'read' && e.verb === 'watch' && e.speaker === 'scout' && e.about === speaker)?.lines ?? [];
  const scenes: SceneT[] = text.scenes.items.filter(s => s.kind === 'overheard' && s.verb === 'watch' && s.about === speaker).map(s => ({
    verb: 'watch' as const, group,
    variants: s.variants.map(v => ({ beats: v.beats.filter(b => 'line' in b).map(b => ({ who: (b.speaker === speaker ? 'focus' : b.speaker === 'scout' ? 'scout' : 'bystander') as 'focus' | 'scout' | 'bystander', line: (b as { line: string }).line })) })),
  }));
  return { cells, watch: [...watch], scenes };
}
export type { Group };
