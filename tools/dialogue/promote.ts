// usage: bun tools/dialogue/promote.ts <run> --scope <scope> --by "<who, date>" --why "<why>" [--plan <plan id>]
// Promotes the accepted tables of a dialogue run (promoted.ts promoteRun): contracts/dialogue/<plan>.<scope>.voices.json, the run's `voices` byte for byte, and its record.
// Only an ACCEPTED dialogue run of that scope; never over an existing pair. Sky's yes comes first.
import { promoteRun } from './promoted';

const argv = process.argv.slice(2), opt = (k: string) => { const i = argv.indexOf(k); return i < 0 ? undefined : argv[i + 1]; };
const run = argv[0], scope = opt('--scope'), by = opt('--by'), why = opt('--why'), plan = opt('--plan') ?? 'harbour-district';
if (!run || !/^r\d{3}$/.test(run) || !scope || !by || !why) { console.error('usage: bun tools/dialogue/promote.ts <run> --scope <scope> --by "<who, date>" --why "<why>" [--plan <plan id>]'); process.exit(2); }
const c = promoteRun({ run, scope, by, why, plan });
console.log(`${c.ok ? 'ok  ' : 'FAIL'} ${c.label}`);
for (const p of c.problems) console.log(`     ${p}`);
process.exit(c.ok ? 0 : 1);
