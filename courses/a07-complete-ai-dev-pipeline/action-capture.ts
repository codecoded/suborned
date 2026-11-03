/** Longer A07 opening: ordinary loud-route orders in the retained release player.
 * No scene edits, spawned actors, rule overrides or new Unity compilation.
 * bun run guard bun courses/a07-complete-ai-dev-pipeline/action-capture.ts
 */
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';
import { resolveShots } from '../../tools/game/shots';

const ROOT = resolve(import.meta.dir, '../..');
const OUT = join(ROOT, 'out/scratch/a07/action-opening');
const APP = join(ROOT, 'out/build/play/The Suborned.app');
const script = 'tools/game/pins/loud.seed3.json';
const source = {
  schema: 'suborned.shots/1',
  defaults: { size: [1920, 1080], hud: true, format: 'png', settle: 0.6, clip: { seconds: 6, fps: 20 } },
  shots: [
    { name: 'officers-wide', script, tick: 2242, focus: 'op/scout', yaw: 2, zoom: 10, select: 'scout' },
    { name: 'rivals-follow', script, tick: 2514, view: 'follow', agent: 'scout', face: 'rival-2' },
    { name: 'rivals-wide', script, tick: 2514, focus: 'op/scout', yaw: 2, zoom: 10, select: 'scout' },
  ],
};
mkdirSync(join(OUT, 'raw'), { recursive: true });
const resolved = join(OUT, 'resolved.json');
writeFileSync(join(OUT, 'source.json'), JSON.stringify(source, null, 2) + '\n');
writeFileSync(resolved, JSON.stringify(resolveShots(source, join(OUT, 'raw')), null, 2) + '\n');
const child = Bun.spawn([join(APP, 'Contents/MacOS/The Suborned'), '-shots', resolved,
  join(OUT, 'raw'), '-fastExit', '-windowed', '-logFile', join(OUT, 'player.log')], { stdout: 'ignore', stderr: 'ignore' });
const exit = await child.exited;
if (exit !== 0) throw new Error(`Player exited ${exit}; see ${OUT}/player.log`);
const report = JSON.parse(readFileSync(join(OUT, 'raw/shots.json'), 'utf8'));
if (report.status !== 'ok' || report.shots.some((s: any) => s.status !== 'ok' || s.clipFrames !== 120))
  throw new Error(JSON.stringify(report));
const receipt = { report, player: 'out/build/play/The Suborned.app',
  script, script_sha256: createHash('sha256').update(readFileSync(join(ROOT, script))).digest('hex'),
  capture: 'Ordinary mission start, seed 3; route orders through the real host; no fixtures or relationship overrides',
  limits: 'Retained 0.2.232 release binary, separate from the people replay variant; no fresh Unity compilation' };
writeFileSync(join(OUT, 'receipt.json'), JSON.stringify(receipt, null, 2) + '\n');
console.log(JSON.stringify(receipt, null, 2));
