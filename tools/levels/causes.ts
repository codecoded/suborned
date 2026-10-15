// Re-declares the standing drift causes (tools/levels/causes.json) for the current code hash of each objective, so a change to code a verdict rests on does not make every recorded run fail replay for want of a cause
// the ledger already knew. Idempotent: a cause already declared for the hash is left as it is (its rules are merged). It adds nothing new: a new cause is declared by hand, with the change that moved a rule
// (bun tools/levels/<objective>/drift.ts declare "<cause>" --rules ...), and then written into causes.json so the next hash carries it.
//   bun run causes [people|transit|skyline ...]     declare for these objectives (default: all), then print what each ledger holds for its hash
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { declaredFor } from '../gen/ledger';
import { root } from '../rag/config';
import { declareCause, ledgerPath, readLedger } from './drift';
import { peopleHash } from './people/hash';
import { skylineHash } from './skyline/hash';
import { toolsHash } from './toolhash';

type Causes = { people: { cause: string; rules: string[] }[]; transit: { cause: string; rules: string[] }[]; skyline: { cause: string; rules: string[] }[] };
const OBJECTIVES = {
  people: { ledger: () => ledgerPath('tools/levels/people/drift.json', 'people'), hash: () => peopleHash() },
  transit: { ledger: () => ledgerPath('tools/levels/drift.json', 'transit'), hash: () => toolsHash() },
  skyline: { ledger: () => ledgerPath('tools/levels/skyline/drift.json', 'skyline'), hash: () => skylineHash() },
} as const;

export async function redeclare(which: (keyof typeof OBJECTIVES)[] = ['people', 'transit', 'skyline'], causesFile = resolve(root, 'tools/levels/causes.json')): Promise<Record<string, { hash: string; causes: number }>> {
  const all = JSON.parse(readFileSync(causesFile, 'utf8')) as Causes, out: Record<string, { hash: string; causes: number }> = {};
  for (const o of which) {
    const obj = OBJECTIVES[o], hash = await obj.hash(), file = obj.ledger();
    for (const c of all[o]) declareCause(file, hash, c.cause, c.rules);
    out[o] = { hash: hash.slice(0, 12), causes: declaredFor(readLedger(file), hash).length };
  }
  return out;
}

if (import.meta.main) {
  const args = process.argv.slice(2), bad = args.filter(a => !(a in OBJECTIVES));
  if (bad.length) { console.error(`unknown objective ${bad.join(', ')}; known: ${Object.keys(OBJECTIVES).join(' ')}`); process.exit(2); }
  const r = await redeclare(args.length ? (args as (keyof typeof OBJECTIVES)[]) : undefined);
  for (const [o, v] of Object.entries(r)) console.log(`${o.padEnd(8)} ${v.hash}  ${v.causes} cause${v.causes === 1 ? '' : 's'} declared for this hash`);
}
