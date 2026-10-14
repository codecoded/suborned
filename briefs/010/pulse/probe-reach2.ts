import { readFileSync } from 'node:fs';
import { civic } from '../../../tools/plan/civic';
import { derive } from '../../../tools/plan/plan';
import { walker } from '../../../tools/people/walk';
const plan = JSON.parse(readFileSync('contracts/world/harbour-district.plan.json', 'utf8')), block = JSON.parse(readFileSync('contracts/world/civic-block.json', 'utf8'));
const d = derive(plan), civ = civic(plan.civic, block);
const w = (await Bun.file('out/world/harbour-district.world.json').json()) as any;
const F = w.frontages as any[];
const walk = walker(plan, d, civ, F.map(f => [f.entrance.c[0], f.entrance.c[1]] as [number, number]), 1.4);
const src: [number, number] = [-130, -230];
for (const off of [0, 2, 4, 6]) {
  const st: Record<string, [number, number]> = {};
  for (const f of F) {
    const b: [number, number] = [f.entrance.c[0] + f.normal[0] * off, f.entrance.c[1] + f.normal[1] * off], ok = walk.m(src, b) < Infinity;
    const k = f.level; st[k] ??= [0, 0]; st[k]![0]++; if (ok) st[k]![1]++;
  }
  console.log('offset', off, JSON.stringify(st));
}
// where are the unreachable street doors (area, trade)
const bad: Record<string, number> = {};
for (const f of F) if (f.level === 'street') { const b: [number, number] = [f.entrance.c[0] + f.normal[0] * 4, f.entrance.c[1] + f.normal[1] * 4]; if (walk.m(src, b) === Infinity) bad[f.area + '/' + f.street] = (bad[f.area + '/' + f.street] ?? 0) + 1; }
console.log(JSON.stringify(bad));
