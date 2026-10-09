// Loads a suborned.plan/1 file and runs everything the checker derives from it.
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { derive, type Plan } from './plan';
import { civic } from './civic';
import { check } from './checks';

export const root = resolve(import.meta.dir, '../..');
const text = (p: string) => readFileSync(resolve(root, p), 'utf8');
export const json = (p: string) => JSON.parse(text(p));

/** Shared CLI parsing: the plan path is the one .json argument that is not a flag's value; `opt('--x')` is a flag's value. */
export function cliArgs(args: string[]) {
  const opt = (k: string) => (args.includes(k) ? args[args.indexOf(k) + 1] : undefined);
  const src = args.find((a, i) => a.endsWith('.json') && !args[i - 1]?.startsWith('--')) ?? 'contracts/world/harbour-district.plan.json';
  return { src, opt };
}

export function load(src: string) {
  const plan: Plan = json(src), d = derive(plan);
  const civ = civic(plan.civic, json(plan.civic.block));
  // sha256 of the plan file and the civic block file it reads, so a build can show what it was made from.
  const planHash = createHash('sha256').update([src, plan.civic.block].map(text).join('\0')).digest('hex');
  return { plan, d, civ, R: check(plan, d, civ), planHash };
}
