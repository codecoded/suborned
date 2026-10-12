// Where the DecoKit binaries are. SUBORNED_OLD = the old repo (default ~/Projects/the-suborned), SUBORNED_STORE = the store (default ~/Projects/the-suborned-store).
import { existsSync } from 'node:fs';
import { homedir } from 'node:os';
import { join, resolve } from 'node:path';

const home = (p: string) => p.replace(/^~(?=\/|$)/, homedir());
export const OLD = resolve(home(process.env.SUBORNED_OLD ?? '~/Projects/the-suborned'));
export const STORE = resolve(home(process.env.SUBORNED_STORE ?? '~/Projects/the-suborned-store'));
export const KIT_ROOTS = [join(OLD, 'game/Assets/TheSuborned/Art/DecoKit'), join(STORE, 'DecoKit')];
export const kitRoot = (): string => {
  const r = KIT_ROOTS.find(p => existsSync(join(p, 'decokit_manifest.json')));
  if (!r) throw new Error(`DecoKit not found; looked in ${KIT_ROOTS.join(', ')} (set SUBORNED_OLD or SUBORNED_STORE)`);
  return r;
};
// Where the Blender generator (tools/decokit) writes: $DECOKIT_OUT, else out/decokit/ in this repo. Never committed.
export const GEN_ROOT = resolve(process.env.DECOKIT_OUT ? home(process.env.DECOKIT_OUT) : join(import.meta.dir, '../../out/decokit'));
