// Waymark scratch: which of these points an op can be ordered to (a move accepted) at a tick, after a plan's first n steps.
// Usage: bun out/scratch/waymark/tools/points.ts <plan> <n> <op> <m:ss> x,y x,y ...
import { compose } from '../../../../tools/missions/check/compose';
import { engineAt, playAll } from '../../../../tools/missions/check/engine';
import { resolve } from './probe';
const [planId, nRaw, op, at, ...pts] = process.argv.slice(2);
const [m, s] = at!.split(':'); const tick = Math.round((Number(m) * 60 + Number(s)) * 10);
const e = engineAt('tree', 'tree'), { id, plan } = resolve(planId!);
const { script } = await compose({ ...plan, steps: plan.steps.slice(0, Number(nRaw)) }, e, `wm-pts-${id}`);
const plays = await playAll(pts.map((p, i) => ({ name: `wm-pts-${id}-${i}`, script: { ...script, orders: [...script.orders, { tick, op: op as any, do: 'move', to: p.split(',').map(Number) }].sort((a, b) => a.tick - b.tick), until: tick + 5 } })), e, 12, { replay: false });
plays.forEach((p, i) => { const o = p.orders.findLast(x => x.order.tick === tick && x.order.do === 'move'); console.log(`${pts[i]} ${o?.ok ? 'ok' : `REFUSED ${o?.code} ${o?.why}`}`); });
process.exit(0);
