import { readFileSync } from 'node:fs';
import { civic } from '../../../tools/plan/civic';
import { derive } from '../../../tools/plan/plan';
import { walker } from '../../../tools/people/walk';
const plan = JSON.parse(readFileSync('contracts/world/harbour-district.plan.json', 'utf8')), block = JSON.parse(readFileSync('contracts/world/civic-block.json', 'utf8'));
const d = derive(plan), civ = civic(plan.civic, block);
const w = (await Bun.file('out/world/harbour-district.world.json').json()) as any;
const doors: [number, number][] = w.frontages.map((f: any) => [f.entrance.c[0], f.entrance.c[1]]);
const t0 = performance.now();
const walk = walker(plan, d, civ, doors, 1.4);
console.log('grid built ms', Math.round(performance.now() - t0), 'doors', doors.length);
// time N sources, each to 40 random targets
let t1 = performance.now(), reach = 0, inf = 0, tot = 0;
const N = 6;
for (let i = 0; i < N; i++) {
  const a = doors[i * 150 % doors.length]!;
  for (let k = 0; k < 40; k++) { const b = doors[(k * 27 + i * 11) % doors.length]!; const m = walk.m(a, b); tot++; if (m === Infinity) inf++; else reach++; }
}
console.log('ms per source', Math.round((performance.now() - t1) / N), 'unreachable', inf, 'of', tot);
console.log('rss MB', Math.round(process.memoryUsage().rss / 1e6));
