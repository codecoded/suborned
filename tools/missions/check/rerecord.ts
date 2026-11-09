// `bun run missions:rerecord "<note>"`: compose the plans' route scripts, then check and RECORD the Handover (runs/missions/handover/cNNN) on the working tree's engine and inputs.
// The note is required and checked first, before the compose step rewrites any script: a bare call used to compose, record nothing and say nothing (Keel, 2026-10-24).
// Brief 101: the compose step stops the record only for a script the check plays; a plan that doesn't compose for one it doesn't play (retired, or played as its
// adaptive plan) is named and its script stands.
import { ROOT } from './engine';

const note = process.argv.slice(2).join(' ').trim();
if (!note || note.startsWith('--')) { console.error('missions:rerecord needs a note, what moved and why: bun run missions:rerecord "corners: floors and takedown places moved"\nnothing was composed, played or recorded'); process.exit(2); }
const run = (...a: string[]) => Bun.spawnSync(['bun', 'tools/missions/check/cli.ts', ...a], { cwd: ROOT, stdout: 'inherit', stderr: 'inherit' }).exitCode;
const c = run('compose', 'handover', '--engine', 'tree', '--inputs', 'tree', '--unplayed-ok'); // brief 101: a plan that doesn't compose leaves its script standing when the check doesn't play that script
if (c !== 0) process.exit(c);
process.exit(run('handover', '--engine', 'tree', '--inputs', 'tree', '--record', note));
