// Brief 072-C test fixture: today's catalogue with the every-face pieces (and RecordsTower) set back to candidate, so faces.test.ts can hold the pass's
// fall-back (nothing placed, every face logged with the pieces that would dress it, the wave-2 world as it was). `bun tools/world/gen/faces.fixture.ts <out.json>`
// writes it for `bun run world --kit <out.json>` (the world as it stood before Vela's pieces were adopted).
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Kit } from '../../../contracts/world/kit.schema';

export const FACE_PIECES = /^(SideBay_[34]|SideBayStack_(Base|Mid|Top)|CornerPier_(Out|In)_(3|4|Base|Mid|Top)|AlleyWall_[34]D?|SidePilaster_(3|4|Base|Mid|Top)|RecordsTower(_TB)?)$/; // brief 077: and its testbed rebuild
export function faceKitOff(from = resolve(import.meta.dir, '../../../contracts/world/kit.json')): Kit { // from: the catalogue to start from (faces.test.ts: the pre-remap pin)
  const kit: Kit = JSON.parse(readFileSync(from, 'utf8'));
  kit.assets = kit.assets.map(a => (FACE_PIECES.test(a.id) ? { ...a, status: 'candidate' } : a));
  return kit;
}
if (import.meta.main) { const out = process.argv[2] ?? 'out/scratch/splice/kit.faces-off.json'; writeFileSync(out, JSON.stringify(faceKitOff())); console.log(`kit without the every-face pieces → ${out}`); }
