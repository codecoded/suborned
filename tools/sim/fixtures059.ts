// Engine 0.33.0 (Keel, brief 059): the scenario fixtures composed from Motive's probe plans (briefs/059/motive/balance/probe-plans), so the
// BAL-* scenarios replay Motive's own reference play on the engine they test. A fixture is an order script composed on the working tree's
// engine (tools/missions/check/compose.ts): the same plan on the same engine gives the same script. When the engine moves the scenario says so;
// re-compose with this command, then rebuild (the scripts are embedded: sim/Suborned.Sim/Suborned.Sim.csproj, Fixtures/*.json).
//   bun tools/sim/fixtures059.ts            compose every fixture and write sim/Suborned.Sim/Fixtures/bal-*.script.json
//   bun tools/sim/fixtures059.ts --check    compose and compare with what is there (exit 1 on a difference)
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import { compose, loadPlan } from '../missions/check/compose';
import { engineAt } from '../missions/check/engine';

const ROOT = join(import.meta.dir, '../..');
/** fixture name → Motive's plan (`dir`/<plan>.plan.json, default briefs/059/motive/balance/probe-plans) and why the scenario uses it. */
export const FIXTURES: Record<string, { plan: string; dir?: string; why: string }> = {
  'bal-loud-x4': { plan: 'loud-x4', why: 'BAL-LOUD-BREAK, BAL-NOPRESS (M059-B5, B3): loud, the four drawn on foot, the Scout and Fixer lock rival-1..3 in turn; the Enforcer walks Vale out on rivals.retreat' },
  // engine 0.34.0 (brief 067 A: the patrol sweeps the service yard): route B takes the Reroute (Motive's rb-rr and rbb-rr, briefs/067/motive-square.md §5.2)
  'bal-rb-ff': { plan: 'rb-rr', dir: 'briefs/067/motive/plans', why: 'BAL-YARD-FOCUS, BAL-YARD-FOCUS-LATE, BAL-YARD-POST (M059-B4, B7, B9): route B at the yard, the Tech reroutes the patrol at 5:00 (brief 067), the Scout and Fixer lock rival-1 then rival-2 from the spring, fight it out' },
  'bal-rbb-ff': { plan: 'rbb-rr', dir: 'briefs/067/motive/plans', why: 'BAL-YARD-AT-WILL (its locks dropped), BAL-YARD-FOCUS, BAL-YARD-FOCUS-LATE (M059-B4, B7): route B at the yard, the Tech reroutes the patrol at 5:00 (brief 067), board and go with the locks' },
  // engine 0.34.0 (brief 067, Motive briefs/067/motive-square.md §2 and §3): the square's two rules on the reference plays Motive measured them on
  'sq-route-a-board': { plan: 'route-a-board', dir: 'contracts/missions/handover/routes/plans', why: 'SQ1-WALK (M067-SQ1): route A, board and go: the whole cell boards a free car and Go is taken; the late car\'s man walks to his spot round the hold' },
  'sq-ra-h3': { plan: 'ra-h3', dir: 'briefs/067/motive/plans', why: 'SQ2-NO-STALL (M067-SQ2): route A, the Scout, Fixer and Tech fight it out at the square: the rivals left with nobody in reach advance, the fight ends and the run wins' },
};

const check = process.argv.includes('--check');
const e = engineAt('tree', 'tree');
let bad = 0;
for (const [name, f] of Object.entries(FIXTURES)) {
  const dir = f.dir ?? 'briefs/059/motive/balance/probe-plans';
  const plan = loadPlan(join(ROOT, dir, `${f.plan}.plan.json`));
  const { script } = await compose(plan, e, name);
  const out = { ...script, note: `Composed by tools/sim/fixtures059.ts from ${dir}/${f.plan}.plan.json on the working tree's engine: ${f.why}. Re-compose when the engine moves.` };
  const text = JSON.stringify(out, null, 1) + '\n', file = join(ROOT, 'sim/Suborned.Sim/Fixtures', `${name}.script.json`);
  if (check) { const same = existsSync(file) && readFileSync(file, 'utf8') === text; console.log(`${same ? 'ok  ' : 'DIFF'} ${name}`); if (!same) bad++; }
  else { writeFileSync(file, text); console.log(`wrote ${file} (${script.orders.length} orders, until ${script.until})`); }
}
process.exit(bad > 0 ? 1 : 0);
