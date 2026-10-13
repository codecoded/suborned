// Experiment 007 F, image input: what is asked, in which form, and the answer's shape. One spec, two arms; only the source block differs.
import { z } from 'zod';

export const SHEETS = ['~/Projects/the-suborned-store/concepts/Blocks/contact_sheet.jpg', '~/Projects/the-suborned-store/concepts/AtomicAge/contact_sheet.jpg'];
export const DIGEST_FILE = 'briefs/007/image-vs-digest/digest.md';
export const BRIEF_FILE = 'briefs/007/image-vs-digest/brief.md';
export const EVIDENCE = 'briefs/007/image-vs-digest'; // small JSON worth committing
export const WORK = 'out/review/007F-image-vs-digest'; // full call records (prompts, transcripts)
export const ARMS = ['image', 'digest'] as const;
export type Arm = (typeof ARMS)[number];

const Entry = z.object({
  name: z.string(),
  claim: z.string().describe('what the source shows, in one or two sentences'),
  tiles: z.array(z.string()).min(1).describe('tile ids that show it, e.g. blocks/rail_through'),
});
export const Description = z.object({
  towerTypes: z.array(Entry).describe('the distinct tall-building types'),
  crowns: z.array(Entry).describe('the kinds of crown, and which tower types carry them'),
  railMeetsBuilding: z.array(Entry).describe('every way the rail is shown meeting a building or station'),
  composition: z.array(Entry).describe('how towers group, and where the height hierarchy sits within a block'),
  notDecidable: z.array(z.string()).describe('what a planner would need that the source does not settle'),
});
export type Description = z.infer<typeof Description>;
export const LISTS = ['towerTypes', 'crowns', 'railMeetsBuilding', 'composition'] as const;

const common = `Describe the skyline grammar shown by the Harbour concept sheets, as input for the world plan's tower signatures.
The source is two sheets: Blocks (eight aerial views of one city block each) and AtomicAge (twelve street-level views of single buildings and interiors).
Cite tiles as blocks/<caption as printed> (e.g. blocks/rail_through) and atomic/<caption in lower case with underscores> (e.g. atomic/power_spire).
Fill the schema: towerTypes (the distinct tall-building types, as a planner would list them), crowns (the kinds of crown and which tower types carry them),
railMeetsBuilding (every way the rail is shown meeting a building or station), composition (how towers group, where the height hierarchy sits within a block),
notDecidable (what a planner would need that the sheets do not settle).
State only what the source shows. No heights in metres. Every entry cites at least one tile. Fewer accurate entries beat many guesses.`;

/** The task text of one arm. The images themselves travel as image blocks; the digest travels in the text. */
export const task = (arm: Arm, digest: string) => arm === 'image'
  ? `${common}\n\nThe source is the two sheets attached as images (in the order Blocks, AtomicAge). Each tile carries its caption.`
  : `${common}\n\nThe source is a text digest of the two sheets, one line per tile, written from the sheets beforehand:\n\n${digest}`;
