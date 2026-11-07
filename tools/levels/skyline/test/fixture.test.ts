// The skyline tests judge the world against a pinned kit digest (fixtures/kit.digest.json) but build it from the live civic look, so a piece the look
// starts naming that the digest lacks fails CD-2 and SF-21 for reasons that are not the test's. The digest must list every piece kit.json adopts, and keep
// adopted the retired pieces the frozen fixture plan still stands at its sites (the pre-remap pin's; brief 098: the eleven towers rejected 2026-11-08).
import { expect, test } from 'bun:test';
import { resolve } from 'node:path';
import { root } from '../../../rag/config';
import { KIT } from './edits';
import { retiredKept, skylineDigest } from './digest';

test('the kit digest fixture lists every piece kit.json has adopted, and no adopted piece it has dropped but the retired ones the fixture plan places', async () => {
  const adopted = (j: { assets: { id: string; status: string }[] }) => j.assets.filter(a => a.status === 'adopted').map(a => a.id).sort();
  const liveDoc = await Bun.file(resolve(root, 'contracts/world/kit.json')).json(), pinDoc = await Bun.file(resolve(root, 'tools/world/test/preremap.kit.json')).json();
  const live = adopted(liveDoc), pinned = adopted(await Bun.file(KIT).json()), kept = retiredKept(liveDoc, pinDoc);
  const missing = live.filter(i => !pinned.includes(i)), extra = pinned.filter(i => !live.includes(i) && !kept.includes(i));
  expect({ missing, extra }, 'tools/levels/skyline/test/fixtures/kit.digest.json is behind contracts/world/kit.json (regenerate: bun tools/levels/skyline/test/digest.ts)').toEqual({ missing: [], extra: [] });
  expect(await Bun.file(KIT).text(), 'regenerate: bun tools/levels/skyline/test/digest.ts').toBe(skylineDigest());
  expect(kept).toHaveLength(11); // the towers Vela retired, which the fixture plan still places
});
