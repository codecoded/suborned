// suborned.audio-cues/1 (Chime, brief 059): which clip plays on which event, at what volume, on which bus. Checked by tools/audio/check.ts.
import { z } from 'zod';

const id = z.string().regex(/^[a-z][a-z0-9-]*$/);
const bus = z.string().regex(/^[a-z][a-z0-9-]*$/);
const spatial = z.strictObject({
  blend: z.number().min(0).max(1).describe('0 = 2D, 1 = fully 3D (AudioSource.spatialBlend)'),
  pan: z.string().optional(), reason: z.string().optional(),
  rolloff: z.enum(['logarithmic', 'linear']).optional(), minDistanceM: z.number().positive().optional(), maxDistanceM: z.number().positive().optional(),
  dopplerLevel: z.number().min(0).max(5).optional(), listener: z.string().optional(),
});

export const Cue = z.strictObject({
  id, on: z.strictObject({ log: z.string().regex(/^[a-z][a-z0-9.-]*$/).describe('a sim log code (Game.Log Code)'), at: z.string() }),
  clip: z.string(), bus, volume: z.number().min(0).max(1), pitch: z.number().positive(), spatial, priority: z.number().int().min(0).max(256),
  voices: z.strictObject({ max: z.number().int().positive(), cooldownS: z.number().min(0) }), playsWhile: z.string(), doesNotPlayOn: z.array(z.string()),
});
export const Loop = z.strictObject({
  id, follows: z.string(), clip: z.string(), bus, volume: z.number().min(0).max(1), loop: z.literal(true), spatial, priority: z.number().int().min(0).max(256),
  playsWhile: z.string(),
  gain: z.strictObject({ movingSpeedMps: z.tuple([z.number(), z.number()]), volumeScale: z.tuple([z.number(), z.number()]), pitch: z.tuple([z.number(), z.number()]), note: z.string() }),
  fade: z.strictObject({ inS: z.number().min(0), outS: z.number().min(0) }), restart: z.string(),
});
export const Cues = z.strictObject({
  schema: z.literal('suborned.audio-cues/1'), note: z.string(),
  buses: z.record(bus, z.strictObject({ db: z.number(), parent: bus.optional(), note: z.string().optional() })),
  family: z.strictObject({ note: z.string(), rows: z.array(z.strictObject({ id, meaning: z.string(), register: z.string(), length: z.string(), status: z.enum(['made', 'reserved']) })) }),
  cues: z.array(Cue), loops: z.array(Loop),
});
export type Cues = z.infer<typeof Cues>;
