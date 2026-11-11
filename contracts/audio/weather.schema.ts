// suborned.audio-weather/1 (Chime, brief 107): the weather and night sounds. Rows read like the 087 pack's (briefs/087/cues.json: file, mix_group, voices_max,
// cooldown_s, loop, spatial, against that pack's buses and spatial table) and add when they play (the start state's weather and light), their role and where they sit.
// Written by briefs/107/chime/make/build.py; checked by tools/audio/weather.test.ts; staged into StreamingAssets/sim/sound.json by tools/sim/unity.ts.
import { z } from 'zod';

const id = z.string().regex(/^[a-z][a-z0-9_]*$/);
export const WeatherCue = z.strictObject({
  id, label: z.string(), role: z.string(), plays: z.string(), place: z.string(),
  when: z.strictObject({ weather: z.array(z.enum(['clear', 'overcast', 'rain', 'mist'])).optional(), light: z.array(z.enum(['day', 'night'])).optional() }),
  mix_group: z.string(), voices_max: z.number().int().positive(), cooldown_s: z.number().min(0), loop: z.boolean(), spatial: z.string(),
  level: z.strictObject({ lufs: z.number().max(-20), meter: z.enum(['integrated', 'momentary max']) }),
  import: z.enum(['decompress', 'memory', 'stream']), optional: z.boolean(),
  source: z.strictObject({ by: z.string(), takes: z.array(z.string()).min(1), ledger_ids: z.array(z.number().int()), cost_usd: z.number().min(0) }),
  file: z.string().regex(/^Audio\/107\/[a-z0-9_]+$/), path: z.string(), sha256: z.string().regex(/^[0-9a-f]{64}$/),
  measured: z.strictObject({
    seconds: z.number().positive(), sample_rate: z.number().int(), channels: z.union([z.literal(1), z.literal(2)]), bytes: z.number().int().positive(),
    lufs_integrated: z.number(), lufs_momentary_max: z.number(), true_peak_dbtp: z.number().max(-1),
    seam: z.strictObject({ step_percentile: z.number(), step_ratio: z.number(), level_jump_db: z.number(), typical_jump_db_p95: z.number() }).optional(),
  }),
});
export const Weather = z.strictObject({ schema: z.literal('suborned.audio-weather/1'), brief: z.literal('107'), note: z.string(), cues: z.array(WeatherCue).min(1) });
export type Weather = z.infer<typeof Weather>;
