// Drift ledger: which checker code states recorded runs were made under, and the declared causes of the current code's difference.
//   bun tools/levels/drift.ts declare "<cause>"   record the current tools hash with a cause (run after a checker change)
//   bun tools/levels/drift.ts show
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { root } from '../rag/config';
import { toolsHash } from './toolhash';

export type Ledger = { recorded: Record<string, string>; declared: Record<string, string[]> };
const file = resolve(root, 'tools/levels/drift.json');
export const driftLedger = (): Ledger => (existsSync(file) ? JSON.parse(readFileSync(file, 'utf8')) : { recorded: {}, declared: {} });

if (import.meta.main) {
  const [cmd, ...rest] = process.argv.slice(2), l = driftLedger(), now = await toolsHash();
  if (cmd === 'declare' && rest.length) {
    l.declared[now] = [...new Set([...(l.declared[now] ?? []), rest.join(' ')])];
    writeFileSync(file, JSON.stringify(l, null, 1) + '\n'); console.log(`declared ${now.slice(0, 12)}: ${l.declared[now]!.join('; ')}`);
  } else { console.log(`current ${now.slice(0, 12)}`, JSON.stringify(l, null, 1)); }
}
