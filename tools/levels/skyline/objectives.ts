// The skyline crew's objectives: who is asked, with which brief, what is retrieved, and the pictures they are shown.
// Datum, Vantage and Keel each see the two concept sheets as images (no prose digest of them).
import type { Objective } from '../../rag/agent';
import { SKY_STANDARDS } from '../objectives';
import { conceptSheets } from './concepts';
import type { Agent } from './schemas';

const dir = 'tools/levels/objectives';

const SKYLINE = 'skyline standards dominant rival spires rivalMax harbour icon tiers crown types tower sites'
  + ' signatures';
const PORTAL = 'sky rail portal clearance running twin station soffit pylons beam through a building';
const STATION = 'station inside a building reaches the street stair banks lifts levels station concourse';
const PIECES = 'signature structures SpaceHQ Skylon DiscTower Tower_Needle Tower_Starburst Tower_Terrace LinerDock'
  + ' SkyBridge kit pieces adopted rejected 006 B';
const REACH = 'every area in reach of a stop or station; interchange pairs; entrances fronting public space';

const LOOK = 'bright retro-future skyline towers rockets planets crowns on every tower';

type Member = Objective & { owner: string };
/**
 * `images` is read when a call is made, not when this module loads: every replay imports the plug-ins (the ten A04 runs
 * included), and none of them needs the concept index.
 */
const member = (m: Omit<Member, 'images' | 'effort'>): Member => ({
  ...m, effort: 'medium',
  get images() { return conceptSheets(); },
});

export const objectives: Record<Agent, Member> = {
  datum: member({
    id: 'skyline-datum', role: 'world-planner', owner: 'Datum', brief: `${dir}/skyline-datum.md`,
    query: [SKYLINE, PORTAL, SKY_STANDARDS, PIECES, LOOK],
  }),
  vantage: member({
    id: 'skyline-vantage', role: 'level-designer', owner: 'Vantage', brief: `${dir}/skyline-vantage.md`,
    query: [SKY_STANDARDS, PORTAL, STATION, REACH],
  }),
  keel: member({
    id: 'skyline-critic', role: 'simulation-engineer', owner: 'Keel', brief: `${dir}/skyline-critic.md`,
    query: [SKYLINE, PORTAL, STATION, 'rulebook: each event needs, changes, witnesses; unknown is rejected'],
  }),
};
