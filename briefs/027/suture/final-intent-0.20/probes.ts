// Suture 0.20: direct order probes on the composed base script (scripts/base.json): can a clean agent take or carry Vale, or drive the car?
import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { engineAt, play, type Order } from '../../../../tools/missions/check/engine';
import { summary, clock } from './run';
const DIR = import.meta.dir, e = engineAt('tree', 'tree');
const base = JSON.parse(readFileSync(join(DIR, 'scripts/base.json'), 'utf8'));
const vale = 'councillor';
const probes: Record<string, Order[]> = {
  'scout-take-vale': [{ tick: 2257, op: 'scout', do: 'move', to: [6300, -1700] }, { tick: 2700, op: 'scout', do: 'take', target: vale }],
  'scout-carry-vale': [{ tick: 2257, op: 'scout', do: 'move', to: [6300, -1700] }, { tick: 2700, op: 'scout', do: 'carry', target: vale }],
  'scout-carry-enforcer': [{ tick: 2257, op: 'scout', do: 'move', to: [6300, -1700] }, { tick: 2700, op: 'scout', do: 'carry', target: 'enforcer' }],
  'tech-go': [{ tick: 3080, op: 'tech', do: 'go' } as Order],
};
for (const [id, extra] of Object.entries(probes)) {
  const s = structuredClone(base); s.orders = [...s.orders, ...extra].sort((a: Order, b: Order) => a.tick - b.tick);
  const r = await play(s, `s20-probe-${id}`, e);
  const mine = r.orders.filter(o => extra.some(x => x.tick === o.order.tick && x.op === o.order.op && x.do === o.order.do));
  console.log(summary(id, r).line); for (const o of mine) console.log(`    ${clock(o.order.tick)} ${o.order.op} ${o.order.do} ${o.order.target ?? ''}: ${o.ok ? 'TAKEN' : 'refused: ' + o.why}`);
}
