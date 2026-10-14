// The people crew's objectives (brief 010 B): Pulse proposes the cast tables, Keel critiques them against the people rules, the GDD
// and the checker. Pulse's card is the schedule designer; Keel's the simulation engineer.
import type { Objective } from '../../rag/agent';
import type { Agent } from './schemas';

const dir = 'tools/levels/objectives';
type Member = Objective & { owner: string };

const WHO = 'cast table for the corner café and the Records Office: trade, opening hours, staff per shift and positions';
const RULES = 'people rules: temperaments, needs and the approaches that meet them, tie kinds and limits, positions, names';
const DAY = 'life.json routine skeletons worker resident, day bands, civic employers, tiers, walk speed';
const BLOCK = 'civic block café Records Office entrances bays keep-clear zones; the Handover timeline destinations';

export const objectives: Record<Agent, Member> = {
  pulse: {
    id: 'people-pulse', role: 'schedule-ai-designer', owner: 'Pulse', brief: `${dir}/people-pulse.md`, effort: 'medium',
    query: [WHO, RULES, DAY, BLOCK, 'GDD §04 person routine need temperament memory; §09 people occupation temperament need relationships routine', 'people rules knowledge needs known keeper; mix cap per row and unit; ties that introduce; readBy fields'],
  },
  keel: {
    id: 'people-critic', role: 'simulation-engineer', owner: 'Keel', brief: `${dir}/people-critic.md`, effort: 'medium',
    query: [RULES, WHO, DAY, 'rulebook: each event needs, changes, witnesses; unknown is rejected', 'GDD §09 risk sameness: history, not backstory; people are memorable via what the systems record', 'people rules knowledge needs known keeper; mix cap per row and unit; readBy fields'],
  },
};
