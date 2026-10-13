// Images for a crew call (brief 007 F, step 1b). A call may carry pictures (the concept sheets) as image blocks in its one user message; it stays
// tool-less: nothing is read from disk by the model, the bytes go in the message. The call record keeps each image as a reference, never the bytes:
// path, media type, size and sha256, so a run replays offline without the pictures, and with them on this machine the hashes are checked.
import { createHash } from 'node:crypto';
import { homedir } from 'node:os';
import { extname, resolve } from 'node:path';
import { root } from './config';

export type ImageRef = { path: string; mediaType: string; bytes: number; sha256: string };
export type LoadedImage = ImageRef & { data: Uint8Array };

const TYPES: Record<string, string> = { '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg', '.png': 'image/png', '.webp': 'image/webp', '.gif': 'image/gif' };
export const MAX_IMAGE_BYTES = 5 * 1024 * 1024; // the API's per-image limit

/** `~/x` is the home folder, a relative path is from the repo root. The path as written is what the record keeps (the store is outside the repo). */
export const imagePath = (path: string) => resolve(path.startsWith('~/') ? homedir() : root, path.startsWith('~/') ? path.slice(2) : path);
export const sha256 = (data: Uint8Array) => createHash('sha256').update(data).digest('hex');

export async function loadImages(paths: string[] = []): Promise<LoadedImage[]> {
  return Promise.all(paths.map(async path => {
    const mediaType = TYPES[extname(path).toLowerCase()];
    if (!mediaType) throw new Error(`image ${path}: unsupported type (use ${Object.keys(TYPES).join(' ')})`);
    const file = Bun.file(imagePath(path));
    if (!(await file.exists())) throw new Error(`image ${path}: not found`);
    const data = await file.bytes();
    if (data.length > MAX_IMAGE_BYTES) throw new Error(`image ${path}: ${data.length} bytes is over the ${MAX_IMAGE_BYTES} limit`);
    return { path, mediaType, bytes: data.length, sha256: sha256(data), data };
  }));
}

export const imageRef = ({ path, mediaType, bytes, sha256 }: ImageRef): ImageRef => ({ path, mediaType, bytes, sha256 });

/** The line that tells the model which attached picture is which, and what it is pinned as. Part of the saved prompt. */
export const imageNote = (images: ImageRef[]) =>
  images.length ? `Attached images (in order, shown before this text): ${images.map((x, i) => `${i + 1}. ${x.path.split('/').slice(-2).join('/')} (sha256 ${x.sha256.slice(0, 12)})`).join('; ')}.` : '';
