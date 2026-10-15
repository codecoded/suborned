// The dialogue crew's objectives (brief 012): Fable writes the voice tables for a scope, Keel critiques them against the dialogue rules, the people rules, the GDD
// and the checker. Fable's card is the dialogue designer; Keel's the simulation engineer.
import type { Objective } from '../../rag/agent';
import type { Agent } from './schemas';

const dir = 'tools/levels/objectives';
type Member = Objective & { owner: string };

const TEMP = 'GDD §04 temperament table: Idealist Mercenary Fearful Loyal, responds to, refuses, Hero, Menace; wrong approach works one step worse; Loyal reports any approach';
const APPR = 'GDD §04 approaches Ask Pay Help Pressure: requires, price, leaves; needs list debt cash housing papers kin care protection advancement secret; ties family workmate regular debtor rival';
const TALK = 'GDD §04 talker: questioned, a person gives up known facts if fear + resentment > bravery + loyalty; a brave helped contact lies; knowledge only witnessed or told';
const GEN = 'GDD §09 generation: dialogue, portraits, voices from person data and state (brave, resentful, promoted), checked by tone and rating filters; risk sameness: history, not backstory; text is a light layer';
const WORLD = 'GDD §01 world: an alternate 1950s that discovered atomic power centuries ago, no thinking machines or networks, records are paper, calls are voice; optimistic surface, coercive underneath';

export const objectives: Record<Agent, Member> = {
  fable: {
    id: 'dialogue-fable', role: 'dialogue-designer', owner: 'Fable', brief: `${dir}/dialogue-fable.md`, effort: 'medium',
    query: [TEMP, APPR, TALK, GEN, WORLD, 'GDD §14 Fable dialogue designer: lines that match what each person can do and how they feel; hold: history not backstory, built at build time never live, tone and rating filters, civilians are never the payload'],
  },
  keel: {
    id: 'dialogue-critic', role: 'simulation-engineer', owner: 'Keel', brief: `${dir}/dialogue-critic.md`, effort: 'medium',
    query: [TEMP, APPR, TALK, GEN, 'rulebook: each event needs, changes, witnesses; unknown is rejected; debrief lines carry their cause', WORLD],
  },
};
