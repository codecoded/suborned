// withLook: a run's pinned civic-look.json stands in for the one on disk while the world is built, and goes back after, whatever happens.
import { expect, test } from 'bun:test';
import { mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { personPieces } from '../build';
import { look, withLook } from './look';

const scratchLook = async (edit: (l: typeof look) => void) => {
  const l = structuredClone(look), file = join(await mkdtemp(join(tmpdir(), 'look-')), 'civic-look.json');
  edit(l); await Bun.write(file, JSON.stringify(l));
  return file;
};

test('withLook swaps the shared look for the call and puts it back, also when the call throws', async () => {
  const before = look.budget.lod0Tris, file = await scratchLook(l => { l.budget.lod0Tris += 7; });
  expect(withLook(file, () => look.budget.lod0Tris)).toBe(before + 7);
  expect(look.budget.lod0Tris).toBe(before);
  expect(() => withLook(file, () => { throw new Error('boom'); })).toThrow('boom');
  expect(look.budget.lod0Tris).toBe(before);
  expect(withLook(undefined, () => look.budget.lod0Tris)).toBe(before); // no pin: the look on disk
});

test('what the generator derives from the look follows the swap: the person-scale set was computed once at load', async () => {
  const file = await scratchLook(l => { l.swaps = { ...l.swaps, CafeSet_Tulip: 'CafeSet_Pinned' }; });
  expect(personPieces().has('CafeSet_Pinned')).toBe(false);
  expect(withLook(file, () => personPieces().has('CafeSet_Pinned'))).toBe(true);
  expect(personPieces().has('CafeSet_Pinned')).toBe(false);
});
