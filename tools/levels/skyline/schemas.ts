// The skyline objective's outputs: what Datum proposes (sites, portals, the threaded sky rail), what Vantage proposes
// (the sky rail stations), and Keel's verdict. Proposals are upserts by id on a copy of the plan: a field left out
// keeps its value, and nothing is removed unless it is named in `remove`. Every site cites the concept tiles it rests
// on (concepts.ts); standards other than the tower sites are never touched (apply.ts).
import { z } from 'zod';
import { answersOf, handoffsOf, sources, verdictOut } from '../../gen/verdict';
import { LinesOut, StopItem, id, notes, xy } from '../schemas';

export const Owner = z.enum(['Datum', 'Vantage']);
const handoffs = handoffsOf(Owner), answers = answersOf(Owner);

const rect = z.array(z.number()).length(4).describe('[x0, x1, y0, y1] in metres, plan frame');
const pair = z.array(z.number()).length(2);
const prose = (n: number) => z.string().max(n);
const concepts = z.array(z.string().regex(/^[a-z]+\/[a-z0-9_]+$/)).min(1)
  .describe('tile ids of the concept sheets this rests on, from the index in the task (at least one)');

/** The opening a sky rail beam enters a building through: how wide, how high. */
const opening = z.object({
  clearHalfWidth: z.number().positive().describe('half the clear width of the opening, m'),
  z: pair.describe('[sill, head] heights in m'),
});

const SignaturePortal = z.object({
  kind: z.enum(['station', 'running']), beams: z.enum(['twin', 'single']), line: id, path: id, axis: prose(160),
  clearHalfWidth: z.number().positive().describe('half the width kept clear of solids along the line'),
  soffit: z.number().positive().describe('ceiling of the opening; nothing stands in it below this'),
  openings: opening.extend({ at: pair.describe('[x0, x1] along the line the openings span') }),
  station: z.string().optional().describe('"<line>/<station id>" of the station inside, if any'),
});

export const SignatureEdit = z.object({
  id, name: prose(80), kind: prose(40), role: prose(40).describe('dominant, rival, harbour mark, ...'),
  areas: z.array(id).min(1).describe('ids of the environmental areas it stands in'),
  c: xy.describe('centre [x, y]'),
  ground: rect.describe('its ground: no plot building stands on it'), groundWhy: prose(1000),
  kit: z.string().optional().describe('id of the kit piece that stands there'),
  height: z.number().positive().optional().describe('tip height, m'),
  crownType: prose(40), crownWhy: prose(1000).optional(), why: prose(1000),
  regen: z.object({ variant: z.string().optional(), tip: z.number().optional() }).optional()
    .describe('the regenerated variant of the piece (Vela builds it) and its tip; other regen fields keep their value'),
  portal: SignaturePortal.optional(),
  concepts,
});

export const SiteEdit = z.object({
  id, plot: id.describe('id of the plot the tower stands on'), rect, storeys: z.number().int().positive(),
  crownType: prose(40), crownWhy: prose(1000).optional(), why: prose(1000),
  regen: z.object({
    from: z.string().describe('id of the kit piece the tower is regenerated from'),
    same: id.optional().describe('a matched pair: the id of the site it copies'),
    body: z.array(z.number()).length(3).describe('[width, depth, height] of the body, m'),
    tiers: z.array(z.array(z.number()).length(3)).describe('[width, from z, to z] per tier'),
    crown: prose(300).optional(), tip: z.number().positive().optional(), why: prose(500),
  }).optional(),
  concepts,
});

export const PortalEdit = z.object({
  structure: id.describe('id of the megastructure the portal is in'), id,
  kind: z.enum(['running', 'station']), beams: z.enum(['twin', 'single']),
  rect: rect.describe('the footprint of the portal along the line'),
  z: pair.describe('[from, to] heights of the structure over it'),
  axis: prose(160), opening, why: prose(1000),
  concepts,
});

const SkyLine = LinesOut.shape.lines.element.extend({ mode: z.literal('skyrail') });
const Rail = z.object({
  lines: z.array(SkyLine).describe('sky rail lines to add or replace by id'),
  remove: z.array(id).describe('ids of sky rail lines to delete; [] for none'),
});

export const SkylineOut = z.object({
  signatures: z.array(SignatureEdit), towerSites: z.array(SiteEdit), portals: z.array(PortalEdit),
  remove: z.object({ signatures: z.array(id), towerSites: z.array(id) }).describe('ids to delete; [] for none'),
  rail: Rail,
  pieceRequests: z.array(z.object({
    site: id, piece: z.string(), why: prose(300),
    size: z.array(z.number()).length(3).describe('wanted [width, depth, height], m'),
  })).describe('kit pieces Vela must build or rebuild for the sites above; they are not applied to the plan'),
  notes, sources, handoffs,
});

/**
 * A sky rail station as Vantage places it: the transit stop plus the three fields the plan already carries for a
 * station inside a building. A field left out keeps the value the station has in the plan (by id); the stations
 * proposed for a line replace its list.
 */
export const StationItem = StopItem.extend({
  in: z.string().regex(/^(signatures|megastructures)\/[A-Za-z0-9_-]+$/).optional()
    .describe('"signatures/<id>" or "megastructures/<id>": the structure the station stands inside, if any'),
  kit: z.string().optional().describe('id of the kit piece the station is built from'),
  kitWhy: prose(300).optional(),
});
export const StationsOut = z.object({ stops: z.array(StationItem), notes, sources, handoffs, answers });
export const VerdictOut = verdictOut(Owner);

export type SignatureEdit = z.infer<typeof SignatureEdit>;
export type SiteEdit = z.infer<typeof SiteEdit>;
export type PortalEdit = z.infer<typeof PortalEdit>;
export type SkylineOut = z.infer<typeof SkylineOut>;
export type StationItem = z.infer<typeof StationItem>;
export type StationsOut = z.infer<typeof StationsOut>;
export const agents = { datum: SkylineOut, vantage: StationsOut, keel: VerdictOut } as const;
export type Agent = keyof typeof agents;

/**
 * An empty proposal: no edits. What a preview answers for Datum and Vantage, and what a round that changes nothing
 * looks like.
 */
export const NO_SKYLINE: SkylineOut = {
  signatures: [], towerSites: [], portals: [], remove: { signatures: [], towerSites: [] },
  rail: { lines: [], remove: [] }, pieceRequests: [], notes: [], sources: [],
};
export const NO_STATIONS: StationsOut = { stops: [], notes: [], sources: [] };
