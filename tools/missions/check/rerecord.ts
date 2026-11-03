// `bun run missions:rerecord "<note>"`: compose the plans' route scripts, then check and RECORD the Handover (runs/missions/handover/cNNN) on the working tree's engine and inputs.
// The note is required and checked first, before the compose step rewrites any script: a bare call used to compose, record nothing and say nothing (Keel, 2026-10-24).
import { ROOT } from './engine';

const note = process.argv.slice(2).join(' ').trim();
if (!note || note.startsWith('--')) { console.error('missions:rerecord needs a note, what moved and why: bun run missions:rerecord "corners: floors and takedown places moved"\nnothing was composed, played or recorded'); process.exit(2); }
const run = (...a: string[]) => Bun.spawnSync(['bun', 'tools/missions/check/cli.ts', ...a], { cwd: ROOT, stdout: 'inherit', stderr: 'inherit' }).exitCode;
const c = run('compose', 'handover', '--engine', 'tree', '--inputs', 'tree');
if (c !== 0) process.exit(c);
process.exit(run('handover', '--engine', 'tree', '--inputs', 'tree', '--record', note));
