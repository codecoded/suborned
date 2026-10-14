// What a person knows at the start (rules `knowledge`, P5; GDD §08 the café owner tips needs, §03 the cell is known by face).
// Derived, never authored: a keeper knows the need of each workmate (a workmate tie) and of each person with a regular tie to
// anyone working in the keeper's unit. Nobody knows anything about the cell (memory is empty; the cell is known by face only).
import type { Knowledge, Person } from '../../contracts/people/people.schema';
import type { Rules } from './rules';

export function deriveKnowledge(rules: Rules, persons: Person[]): Knowledge {
  const byId = new Map(persons.map(p => [p.id, p])), k = rules.knowledge;
  const needsKnown: Knowledge['needsKnown'] = [];
  for (const rule of k.needsKnown) {
    for (const p of persons) {
      if (!p.work || (p.work.position ?? p.work.role) !== rule.position) continue;
      const knows = new Set<string>();
      if (rule.of.includes('workmate')) for (const t of p.ties) if (t.kind === 'workmate') knows.add(t.with);
      if (rule.of.includes('unit-regular')) for (const q of persons) if (q.ties.some(t => t.kind === 'regular' && byId.get(t.with)?.work?.unit === p.work!.unit)) knows.add(q.id);
      knows.delete(p.id);
      needsKnown.push({ keeper: p.id, knows: [...knows].sort() });
    }
  }
  needsKnown.sort((a, b) => (a.keeper < b.keeper ? -1 : 1));
  return { cellAtStart: k.cellAtStart, identity: [...k.identity], needsKnown };
}
