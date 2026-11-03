// Waymark scratch: one jitter sample of a plan, its log written to out/scratch/waymark/logs/<id>-k<k>.log. Usage: bun jlog.ts <plan> <k>
import { writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { jitter, line, resolve, run } from './probe';
const [id0, kRaw] = process.argv.slice(2), k = Number(kRaw), { id, plan } = resolve(id0!);
const clock = (t: number) => `${Math.floor(t / 600)}:${((t % 600) / 10).toFixed(1).padStart(4, '0')}`;
const r = await run(`${id}-j${k}`, jitter(plan, k));
writeFileSync(join(import.meta.dir, '..', 'logs', `${id}-k${k}.log`), r.p.log.map(l => `${clock(l.tick)}  ${l.code.padEnd(28)} ${l.text}`).join('\n') + '\n');
writeFileSync(join(import.meta.dir, '..', 'scripts', `${id}-k${k}.json`), JSON.stringify(r.script, null, 1) + '\n');
console.log(line(`${id} k${k}`, r.p, r.partial));
process.exit(0);
