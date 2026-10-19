// The skyline tests judge the world against a pinned kit digest (fixtures/kit.digest.json) but build it from the live civic look, so a piece the look
// starts naming that the digest lacks fails CD-2 and SF-21 for reasons that are not the test's. The digest must list every piece kit.json adopts.
import { expect, test } from 'bun:test';
import { resolve } from 'node:path';
import { root } from '../../../rag/config';
import { KIT } from './edits';

test('the kit digest fixture lists every piece kit.json has adopted, and no adopted piece it has dropped', async () => {
  const adopted = (j: { assets: { id: string; status: string }[] }) => j.assets.filter(a => a.status === 'adopted').map(a => a.id).sort();
  const live = adopted(await Bun.file(resolve(root, 'contracts/world/kit.json')).json()), pinned = adopted(await Bun.file(KIT).json());
  const missing = live.filter(i => !pinned.includes(i)), extra = pinned.filter(i => !live.includes(i));
  expect(
    { missing, extra },
    `tools/levels/skyline/test/fixtures/kit.digest.json is behind contracts/world/kit.json (regenerate: bun -e "import {kitDigest} from './tools/rag/kit-corpus'; import {readFileSync,writeFileSync} from 'node:fs'; writeFileSync('tools/levels/skyline/test/fixtures/kit.digest.json', kitDigest(readFileSync('contracts/world/kit.json','utf8')))")`,
  ).toEqual({ missing: [], extra: [] });
});
