// bun tools/world/roadfinish-dev.ts [--out dir]: the road finish (tools/world/gen/street/roadfinish.ts) against the committed plan and an in-memory proposal kit, for the road board
// (brief 028, Vela). Reads nothing it writes: contracts/world/harbour-district.plan.json and kit.json with the proposal of tools/world/roadfinish-kit.ts, and the committed export's
// crossings and tram platforms. Writes <out>/placements.json (items + log + the kerb runs the board's 'now' draws a gutter along) and <out>/kit.dev.json.
// Nothing in contracts/ is touched: the kit rows and the plan keys for the shoulder and the pinstripe are proposals until Splice and Datum take them (briefs/028-testbed-port.md, "Road, 2026-10-22").
import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { derive, type Plan } from '../plan/plan';
import { civic } from '../plan/civic';
import { ctxOf } from './gen/common';
import { withKit } from './gen/kitrefs';
import { dressRoadFinish } from './gen/street/roadfinish';
import { json, proposalPlan, writeProposalKit } from './roadfinish-kit';
import { kerbRuns } from './gen/street/capstone';

const root = resolve(import.meta.dir, '../..');
const args = process.argv.slice(2), opt = (k: string, d: string) => (args.includes(k) ? args[args.indexOf(k) + 1]! : d);
const out = resolve(root, opt('--out', 'out/vela028-road'));
mkdirSync(out, { recursive: true });

const plan: Plan = proposalPlan(json('contracts/world/harbour-district.plan.json'));
writeProposalKit(resolve(out, 'kit.dev.json'));

const d = derive(plan), civ = civic(plan.civic, json(plan.civic.block)), world = json('out/world/harbour-district.world.json');
const t0 = performance.now();
const res = withKit(resolve(out, 'kit.dev.json'), () => dressRoadFinish(ctxOf(plan, d, civ), { crossings: world.crossings, platforms: world.transit.stops.map((s: any) => s.ring) }));
if (!res) { console.error('roadfinish: the plan has no standards.streetscape.roads'); process.exit(1); }
const ms = Math.round(performance.now() - t0);
const sheet = plan.sheet, runs = kerbRuns(ctxOf(plan, d, civ), [sheet.x[0], sheet.x[1], sheet.y[0], sheet.y[1]]).map(r => ({ a: r.a, b: r.b, n: r.n, road: r.road, cls: r.cls })); // the kerb faces the board's 'now' draws a gutter along
writeFileSync(resolve(out, 'placements.json'), JSON.stringify({ schema: 'suborned.roadfinish.dev/1', note: 'Vela, brief 028 road, 2026-10-22: dev output of tools/world/gen/street/roadfinish.ts. Not the export: Splice wires the rule into ground.pieces.', items: res.items, log: res.log, runs }));
console.log(`roadfinish: ${res.items.length} pieces in ${ms} ms`, JSON.stringify(res.log, null, 1));
