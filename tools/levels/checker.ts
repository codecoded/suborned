// The plan checker, in process (for agent inputs and replay) and through its CLI (for the sheet and the report).
import { resolve } from 'node:path';
import { check, type Report } from '../plan/checks';
import { civic } from '../plan/civic';
import { legacyOverlay } from '../missions/handover/overlay';
import { derive, type Plan } from '../plan/plan';
import { root } from '../rag/config';

const read = async (p: string) => JSON.parse(await Bun.file(resolve(root, p)).text());
/** `block`: the run's pinned block (parsed); default is whatever plan.civic.block names on disk. */
export async function inProcess(plan: Plan, strict = true, block?: unknown): Promise<Report> {
  const b: any = block ?? await read(plan.civic.block);
  return check(plan, derive(plan), civic(plan.civic, b), strict, legacyOverlay(plan, b)); // a recorded run's pinned block carries its mission; a world-only block has none
}

/** `bun tools/plan/cli.ts <plan> --strict --out <sheet> --json <report>`; returns the exit code. */
export async function viaCli(planFile: string, sheet: string, report: string, log?: string, block?: string): Promise<number> {
  const p = Bun.spawn(['bun', 'tools/plan/cli.ts', planFile, '--strict', '--out', sheet, '--json', report, ...(block ? ['--block', block] : [])], { cwd: root, stdout: 'pipe', stderr: 'pipe' });
  const [out, err, code] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]);
  if (log) await Bun.write(log, out + err);
  return code;
}

export const ownerOf = (rule: string) => (/^(T-S|S-S|S-I|N-P|N-R|N-C)/.test(rule) ? 'Vantage' : 'Datum') as 'Datum' | 'Vantage';
// N-R1 (reach) is fixed by a line (Datum) or a stop (Vantage): both hear a synthesized one. Keel's named owner suppresses it (run.ts).
export const ownersOf = (rule: string): ('Datum' | 'Vantage')[] => (rule === 'N-R1' ? ['Datum', 'Vantage'] : [ownerOf(rule)]);
