// Suture 0.20: why a move to a point is refused (walkability of the in-cone cells), on the composed base script.

import { readFileSync } from 'node:fs';
import { engineAt, play } from '/Users/sky/Projects/suborned/tools/missions/check/engine';
const e = engineAt('tree', 'tree'); const base = JSON.parse(readFileSync('/Users/sky/Projects/suborned/briefs/027/suture/final-intent-0.20/scripts/base.json', 'utf8'));
const pts = [[5400,-2400],[5350,-2300],[5300,-2500],[5200,-2500],[5100,-2600],[5000,-2700],[4960,-2700],[5500,-2200],[5600,-1900],[5700,-1800]];
const s = structuredClone(base); s.orders = [...s.orders, ...pts.map((p, i) => ({ tick: 2300 + i, op: 'scout', do: 'move', to: p }))].sort((a: any, b: any) => a.tick - b.tick);
const r = await play(s, 's20-why', e);
for (const o of r.orders.filter(o => o.order.tick >= 2300 && o.order.tick < 2310 && o.order.op === 'scout')) console.log(JSON.stringify(o.order.to), o.ok ? 'TAKEN' : o.why);
