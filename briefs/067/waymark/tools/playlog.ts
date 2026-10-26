// Waymark scratch: play a route script file as it is and write its log. Usage: bun playlog.ts <script.json> [out.log]
import { readFileSync, writeFileSync } from 'node:fs';
import { engineAt, playAll, type Script } from '../../../../tools/missions/check/engine';
import { line } from './probe';
const [f, out] = process.argv.slice(2); const s = JSON.parse(readFileSync(f!, 'utf8')) as Script;
const clock = (t: number) => `${Math.floor(t / 600)}:${((t % 600) / 10).toFixed(1).padStart(4, '0')}`;
const [p] = await playAll([{ name: `wm-playlog-${f!.split('/').at(-1)}`, script: s }], engineAt('tree', 'tree'), 1, { replay: false });
writeFileSync(out ?? '/dev/stdout', p!.log.map(l => `${clock(l.tick)}  ${l.code.padEnd(28)} ${l.text}`).join('\n') + '\n');
console.error(line(f!.split('/').at(-1)!, p!));
process.exit(0);
