import { expect, test } from 'bun:test';
import { check } from './check.ts';
import { build } from './synth.ts';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { root } from './synth.ts';

test('the cue mapping, the clips on disk and clips.json agree', () => { expect(check()).toEqual([]); });

test('the synth is deterministic: rebuilding gives the recorded hashes', () => {
  const rec = JSON.parse(readFileSync(join(root, 'contracts/audio/clips.json'), 'utf8')).clips;
  const now = build() as Record<string, { sha256: string }>;
  for (const k of Object.keys(rec)) expect(now[k].sha256).toBe(rec[k].sha256);
});
