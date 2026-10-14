// The world cache key follows the inputs as this process loaded them, never the files' bytes on disk now (out/cache, 2026-10-14: a process that loaded the civic
// look before an edit generated from the old table, then keyed the world by the new file, so the old world sat in the cache under the new look's key).
import { expect, setDefaultTimeout, test } from 'bun:test';
import { mkdtemp, rm } from 'node:fs/promises';
import { existsSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { civic } from './civic';
import { derive, type Plan } from './plan';
import { look, withLook } from '../world/civic/look';
import { withKit } from '../world/gen/kitrefs';
import { generated, worldKey } from './world-ctx';

setDefaultTimeout(240000);
const root = resolve(import.meta.dir, '../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const scratch = await mkdtemp(join(tmpdir(), 'world-key-'));
/** A look file that differs from the one on disk by one number the generator's rules read. */
const otherLook = async () => { const l = structuredClone(look), f = join(scratch, `look-${Math.random()}.json`); l.budget.lod0Tris += 1; await Bun.write(f, JSON.stringify(l)); return f; };

test('the key follows the look held in memory, not the file on disk', async () => {
  const live = worldKey(plan, civ), stale = await otherLook();
  // memory holds a look that is not the file's bytes (a process that loaded it before an edit): the key it makes is its own, not the disk's
  expect(withLook(stale, () => worldKey(plan, civ))).not.toBe(live);
  expect(worldKey(plan, civ)).toBe(live); // put back
  // the same values laid out differently are the same inputs
  const spaced = join(scratch, 'spaced.json'); await Bun.write(spaced, JSON.stringify(look, null, 4));
  expect(withLook(spaced, () => worldKey(plan, civ))).toBe(live);
});

test('the key follows the adopted catalogue held in memory, and ignores a piece that is not adopted', async () => {
  const live = worldKey(plan, civ), doc = read('contracts/world/kit.json'), file = join(scratch, 'kit.json');
  const piece = doc.assets.find((a: { status: string }) => a.status === 'adopted'), candidate = doc.assets.find((a: { status: string }) => a.status === 'candidate');
  candidate.size = [...candidate.size].map((x: number) => x + 1); // the generator never reads a candidate
  await Bun.write(file, JSON.stringify(doc));
  expect(withKit(file, () => worldKey(plan, civ))).toBe(live);
  piece.size = [...piece.size].map((x: number) => x + 1);
  const changed = join(scratch, 'kit-changed.json'); // a new path: the catalogue is read once per file and kept, which is the point
  await Bun.write(changed, JSON.stringify(doc));
  expect(withKit(changed, () => worldKey(plan, civ))).not.toBe(live);
});

test('a world generated from a stale in-memory look is never filed under the key of the look on disk', async () => {
  const p: Plan = { ...plan, id: 'world-key-test' }, cache = resolve(root, 'out/cache/world-world-key-test.json'), stale = await otherLook();
  try {
    const disk = worldKey(p, civ);
    withLook(stale, () => generated(p, d, civ)); // the process whose memory is old generates and writes its cache
    const filed = JSON.parse(readFileSync(cache, 'utf8')).key;
    expect(filed).toBe(withLook(stale, () => worldKey(p, civ)));
    expect(filed).not.toBe(disk); // the old key is not the key of the file on disk: the next process regenerates rather than reading the stale world
  } finally { if (existsSync(cache)) await rm(cache); }
});
