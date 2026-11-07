// The skyline tests' kit digest (fixtures/kit.digest.json; brief 098, Splice): the live catalogue's digest (tools/rag/kit-corpus.ts kitDigest), every piece it adopts
// adopted here too (the tests build from the live civic look, which names them), with the pieces kit.json has since retired that the pre-remap pin keeps adopted
// (tools/world/test/preremap.kit.json) kept adopted: the frozen fixture plan was cut from the district before the brief 075 remap and stands them at their sites
// (the eleven towers Vela retired; rejected on 2026-11-08). fixture.test.ts holds the file to this.
// usage: bun tools/levels/skyline/test/digest.ts   (writes fixtures/kit.digest.json)
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { kitDigest } from '../../../rag/kit-corpus';
import { KIT } from './edits';

const ROOT = resolve(import.meta.dir, '../../../..');
type Doc = { schema: string; assets: { id: string; status: string }[] };
/** The ids the pre-remap pin adopts that the live catalogue no longer does: what the fixture plan still places. */
export function retiredKept(live: Doc, pin: Doc): string[] {
  const now = new Set(live.assets.filter(a => a.status === 'adopted').map(a => a.id));
  return pin.assets.filter(a => a.status === 'adopted' && !now.has(a.id) && live.assets.some(b => b.id === a.id)).map(a => a.id).sort();
}
export function skylineDigest(liveText = readFileSync(resolve(ROOT, 'contracts/world/kit.json'), 'utf8'), pinText = readFileSync(resolve(ROOT, 'tools/world/test/preremap.kit.json'), 'utf8')): string {
  const live = JSON.parse(kitDigest(liveText)) as Doc, keep = new Set(retiredKept(live, JSON.parse(pinText) as Doc));
  return JSON.stringify({ ...live, assets: live.assets.map(a => (keep.has(a.id) ? { ...a, status: 'adopted' } : a)) });
}
if (import.meta.main) { const text = skylineDigest(); writeFileSync(KIT, text); console.log(`${KIT}: ${JSON.parse(text).assets.length} pieces`); }
