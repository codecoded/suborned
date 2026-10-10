// Layer-4 objectives for the transit run. Every query list includes one that surfaces `standards › tram` (where stopSpacing
// lives): the single query "tram stop spacing sky rail stations every area in reach of a stop interchange" missed it in the top 8.
import type { Objective } from '../../rag/agent';
import type { Agent } from '../schemas';

const dir = 'tools/levels/objectives';
export const TRAM_STANDARDS = 'standards tram stopSpacing radiusRunning radiusLoop platform trackCentres';
export const SKY_STANDARDS = 'standards skyrail stationSpacing radiusLine radiusLoop beamCentres interchange pylonZones';
export const OLD_QUERY = 'tram stop spacing sky rail stations every area in reach of a stop interchange';

// TRAVEL_Q is a question an owner would ask not knowing the answer. An earlier version paraphrased the §02 rule itself (circular: see ranks.ts CIRCULAR).
export const TRAVEL_Q = 'how does the player use trams and sky rail and what does riding cost';

// Keel: the checker-rules query was dropped to make room: with it, the §02 travel chunk fell out of the top 8 (the checker report is in Keel's task anyway).
export const objectives: Record<Agent, Objective & { owner: string }> = {
  datum: { id: 'transit-lines', role: 'world-planner', owner: 'Datum', brief: `${dir}/transit-lines.md`, effort: 'medium',
    query: [TRAM_STANDARDS, SKY_STANDARDS, 'sky rail loop round the district tram line through the circus Harbour Road alignment fillet radii double track', 'trams and sky rail are networks the cell rides between stops and stations; lines run in street space or over public space'] },
  vantage: { id: 'transit-stops', role: 'level-designer', owner: 'Vantage', brief: `${dir}/transit-stops.md`, effort: 'medium',
    query: [TRAM_STANDARDS, SKY_STANDARDS, 'every area in reach of a stop or station; entrances fronting public space; interchange pairs', 'platform on straight track stop spacing station positions on the lines'] },
  pulse: { id: 'transit-timetable', role: 'schedule-ai-designer', owner: 'Pulse', brief: `${dir}/transit-timetable.md`, effort: 'low',
    query: [TRAM_STANDARDS, 'timetable headway dwell run times journey times between stations', TRAVEL_Q] },
  keel: { id: 'transit-critic', role: 'simulation-engineer', owner: 'Keel', brief: `${dir}/transit-critic.md`, effort: 'medium',
    query: [TRAM_STANDARDS, SKY_STANDARDS, 'rulebook: each event needs, changes, witnesses; unknown is rejected', TRAVEL_Q] },
};
