// The plan checker, in process (for agent inputs and replay) and through its CLI (for the sheet and the report).
import { resolve } from 'node:path';
import { check, type Report } from '../plan/checks';
import { civic } from '../plan/civic';
import { derive, type Plan } from '../plan/plan';
import { root } from '../rag/config';

const read = async (p: string) => JSON.parse(await Bun.file(resolve(root, p)).text());
export async function inProcess(plan: Plan, strict = true): Promise<Report> {
  return check(plan, derive(plan), civic(plan.civic, await read(plan.civic.manifest), await read(plan.civic.destinations)), strict);
}

/** `bun tools/plan/cli.ts <plan> --strict --out <sheet> --json <report>`; returns the exit code. */
export async function viaCli(planFile: string, sheet: string, report: string, log?: string): Promise<number> {
  const p = Bun.spawn(['bun', 'tools/plan/cli.ts', planFile, '--strict', '--out', sheet, '--json', report], { cwd: root, stdout: 'pipe', stderr: 'pipe' });
  const [out, err, code] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]);
  if (log) await Bun.write(log, out + err);
  return code;
}

export const ownerOf = (rule: string) => (/^(T-S|S-S|S-I|N-P|N-R|N-C)/.test(rule) ? 'Vantage' : 'Datum') as 'Datum' | 'Vantage';
