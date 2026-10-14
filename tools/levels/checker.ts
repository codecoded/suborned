// The plan checker, in process (for agent inputs and replay) and through its CLI (for the sheet and the report).
import { resolve } from 'node:path';
import { check, type Report } from '../plan/checks';
import { civic } from '../plan/civic';
import { legacyOverlay } from '../missions/handover/overlay';
import { derive, type Plan } from '../plan/plan';
import { type Pinned, worldChecksUnder } from '../plan/world-rules';
import { root } from '../rag/config';

const read = async (p: string) => JSON.parse(await Bun.file(resolve(root, p)).text());
/**
 * `block`: the run's pinned block (parsed); default is whatever plan.civic.block names on disk.
 * `world`: also the world rules (B, L, MS, F, SF, K, SG, P, SK), as `bun tools/plan/cli.ts` runs them: on the world generated in
 * process, never through the shared out/cache. A plan without the world standards (a recorded run's) has none to run. `kit`, `look`:
 * the catalogue and the civic look the world is built from (a kit.json or a run's pinned kit digest, a run's pinned civic-look.json),
 * default the files in contracts/world.
 */
export async function inProcess(plan: Plan, strict = true, block?: unknown, opt: { world?: boolean } & Pinned = {}): Promise<Report> {
  const b: any = block ?? await read(plan.civic.block), d = derive(plan), civ = civic(plan.civic, b);
  const R = check(plan, d, civ, strict, legacyOverlay(plan, b)); // a recorded run's pinned block carries its mission; a world-only block has none
  if (opt.world) R.checks.push(...worldChecksUnder(plan, d, civ, opt)); // F-7, SF-18, L-8, SF-19 (tools/world/rules) are registered there
  return R;
}

/**
 * `bun tools/plan/cli.ts <plan> --strict --out <sheet> --json <report> [--block <pinned block>] [--kit <pinned kit digest>] [--look <pinned look>]`;
 * returns the exit code. The plan rules and, for a plan that carries the world standards, the world rules, as `inProcess(..., { world: true, kit, look })`
 * runs them: a transit run is replayed by rerunning exactly what it recorded (transit-replay.ts), so the two must list the same rules. `pin` is the run's
 * (pinned.ts): the world is built from the pieces and the look the run ran with, not whatever Vela has changed since.
 */
export async function viaCli(planFile: string, sheet: string, report: string, log?: string, block?: string, pin: Pinned = {}): Promise<number> {
  const args = ['bun', 'tools/plan/cli.ts', planFile, '--strict', '--out', sheet, '--json', report];
  const p = Bun.spawn([...args, ...(block ? ['--block', block] : []), ...(pin.kit ? ['--kit', pin.kit] : []), ...(pin.look ? ['--look', pin.look] : [])], { cwd: root, stdout: 'pipe', stderr: 'pipe' });
  const [out, err, code] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]);
  if (log) await Bun.write(log, out + err);
  return code;
}

export const ownerOf = (rule: string) => (/^(T-S|S-S|S-I|N-P|N-R|N-C)/.test(rule) ? 'Vantage' : 'Datum') as 'Datum' | 'Vantage';
// N-R1 (reach) is fixed by a line (Datum) or a stop (Vantage): both hear a synthesized one. Keel's named owner suppresses it (run.ts).
export const ownersOf = (rule: string): ('Datum' | 'Vantage')[] => (rule === 'N-R1' ? ['Datum', 'Vantage'] : [ownerOf(rule)]);
