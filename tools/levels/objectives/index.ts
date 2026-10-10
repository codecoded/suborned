// Layer-4 objectives for the transit run. Every query list includes one that surfaces `standards › tram` (where stopSpacing
// lives): the single query "tram stop spacing sky rail stations every area in reach of a stop interchange" missed it in the top 8.
import type { Objective } from '../../rag/agent';
import type { Agent } from '../schemas';

const dir = 'tools/levels/objectives';
export const TRAM_STANDARDS = 'standards tram stopSpacing radiusRunning radiusLoop platform trackCentres';
export const SKY_STANDARDS = 'standards skyrail stationSpacing radiusLine radiusLoop beamCentres interchange pylonZones';
export const OLD_QUERY = 'tram stop spacing sky rail stations every area in reach of a stop interchange';

export const objectives: Record<Agent, Objective & { owner: string }> = {
  datum: { id: 'transit-lines', role: 'world-planner', owner: 'Datum', brief: `${dir}/transit-lines.md`, effort: 'medium',
    query: [TRAM_STANDARDS, SKY_STANDARDS, 'sky rail loop round the district tram line through the circus Harbour Road alignment fillet radii double track', 'trams and sky rail are networks the cell rides between stops and stations; lines run in street space or over public space'] },
  vantage: { id: 'transit-stops', role: 'level-designer', owner: 'Vantage', brief: `${dir}/transit-stops.md`, effort: 'medium',
    query: [TRAM_STANDARDS, SKY_STANDARDS, 'every area in reach of a stop or station; entrances fronting public space; interchange pairs', 'platform on straight track stop spacing station positions on the lines'] },
  pulse: { id: 'transit-timetable', role: 'schedule-ai-designer', owner: 'Pulse', brief: `${dir}/transit-timetable.md`, effort: 'low',
    query: [TRAM_STANDARDS, 'timetable headway dwell run times journey times between stations', 'the cell rides trams and sky rail between stops and stations; boarding has no price in the GDD; open question for Motive and Sky'] },
  keel: { id: 'transit-critic', role: 'simulation-engineer', owner: 'Keel', brief: `${dir}/transit-critic.md`, effort: 'medium',
    query: [TRAM_STANDARDS, SKY_STANDARDS, 'plan checker rules: no dead ends, every area in reach, network connected, station spacing, order', 'rulebook: each event needs, changes, witnesses; unknown is rejected'] },
};
