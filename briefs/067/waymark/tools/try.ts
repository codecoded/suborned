// Waymark scratch: compose a plan's first n steps, then try one extra order at a range of ticks and print what the engine answers.
// Usage: bun out/scratch/waymark/tools/try.ts <plan> <n steps> '<order json without tick>' <from m:ss> <to m:ss> [every s]
import { compose } from '../../../../tools/missions/check/compose';
import { engineAt, playAll, type Order } from '../../../../tools/missions/check/engine';
import { resolve } from './probe';

const [planId, nRaw, orderRaw, fromRaw, toRaw, everyRaw] = process.argv.slice(2);
const tk = (a: string) => { const [m, s] = a.split(':'); return Math.round((Number(m) * 60 + Number(s)) * 10); };
const clock = (t: number) => `${Math.floor(t / 600)}:${((t % 600) / 10).toFixed(1).padStart(4, '0')}`;
const e = engineAt('tree', 'tree'), { id, plan } = resolve(planId!), n = Number(nRaw);
const { script } = await compose({ ...plan, steps: plan.steps.slice(0, n) }, e, `wm-try-${id}`);
const extra = JSON.parse(orderRaw!) as Order, from = tk(fromRaw!), to = tk(toRaw!), every = Math.round(Number(everyRaw ?? 1) * 10);
const ticks: number[] = []; for (let t = from; t <= to; t += every) ticks.push(t);
const plays = await playAll(ticks.map(t => ({ name: `wm-try-${id}-${t}`, script: { ...script, orders: [...script.orders, { ...extra, tick: t }].sort((a, b) => a.tick - b.tick), until: t + 30 } })), e, 12, { replay: false });
plays.forEach((p, i) => {
  const t = ticks[i]!, o = p.orders.findLast(x => x.order.tick === t && x.order.do === extra.do);
  const after = p.log.filter(l => l.tick >= t && l.tick <= t + 30 && !l.code.startsWith('crowd.')).slice(0, 4).map(l => `${clock(l.tick)} ${l.code} ${l.text.slice(0, 80)}`).join(' | ');
  console.log(`${clock(t)} ${o?.ok ? 'ok' : `REFUSED ${o?.why ?? '?'}`} ${p.outcome !== 'none' ? `[${p.outcome} ${p.fail}]` : ''} ${after}`);
});
process.exit(0);
