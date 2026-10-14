// The people objective's outputs (brief 010 B): Pulse's cast tables and Keel's verdict. The cast tables are the whole proposal:
// per area × occupation the positions, temperaments, needs, ties and routine variants, and one name pool for the district.
// No person is written by the model; tools/people expands the tables.
import { z } from 'zod';
import { Cast } from '../../../contracts/people/cast.schema';
import { sources, verdictOut } from '../../gen/verdict';
import { notes } from '../schemas';

export const Owner = z.enum(['Pulse']);
export const PulseOut = z.object({ cast: Cast, notes, sources });
export const VerdictOut = verdictOut(Owner);
export type PulseOut = z.infer<typeof PulseOut>;
export const agents = { pulse: PulseOut, keel: VerdictOut } as const;
export type Agent = keyof typeof agents;
