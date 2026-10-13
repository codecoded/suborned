// Replay of a call that was shown pictures: they are pinned by sha256 in the call record and never copied into runs/. Offline, without the store,
// the pin is all there is; with a picture on this machine, its bytes must still hash to the pin.
import { imagePath, sha256, type ImageRef } from '../rag/images';
import type { Check } from './plugin';

export async function checkImages(c: Check, n: string, a: string, call: { images?: ImageRef[] }): Promise<void> {
  const images = call.images;
  if (!images) return;
  let here = 0, away = 0, bad = 0;
  for (const im of images) {
    if (!/^[0-9a-f]{64}$/.test(im.sha256)) { c.fail(`${n}/${a}: image ${im.path} has no valid sha256 pin`); bad++; continue; }
    const file = Bun.file(imagePath(im.path));
    if (!(await file.exists())) { away++; continue; }
    if (sha256(await file.bytes()) === im.sha256) here++; else { c.fail(`${n}/${a}: image ${im.path} on this machine differs from its pinned sha256`); bad++; }
  }
  if (!bad) c.ok(`${n}/${a}: ${images.length} image(s) pinned by sha256 (${here} checked here, ${away} not on this machine)`);
}
