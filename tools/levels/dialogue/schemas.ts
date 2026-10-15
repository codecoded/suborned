// The dialogue objective's outputs (brief 012): Fable's voice tables for one scope and Keel's verdict. The tables are the whole proposal: how a kind of person sounds,
// by voice group × temperament × situation, and the shared words. No person is written by the model; tools/dialogue expands the tables.
import { z } from 'zod';
import { Voices } from '../../../contracts/dialogue/voices.schema';
import { sources, verdictOut } from '../../gen/verdict';
import { notes } from '../schemas';

export const Owner = z.enum(['Fable']);
export const FableOut = z.object({ voices: Voices, notes, sources });
export const VerdictOut = verdictOut(Owner);
export type FableOut = z.infer<typeof FableOut>;
export const agents = { fable: FableOut, keel: VerdictOut } as const;
export type Agent = keyof typeof agents;
