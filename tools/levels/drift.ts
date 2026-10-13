// Drift ledger: which code states recorded runs were made under, and the declared causes of the current code's
// difference.
//   bun tools/levels/drift.ts declare "<cause>"          the current tools hash with a cause (after a checker change)
//   bun tools/levels/drift.ts record <hash> "<label>"    a code state that a recorded run was made under
//   bun tools/levels/drift.ts show
// Each objective has its own ledger (transit: tools/levels/drift.json; skyline: tools/levels/skyline/drift.json)
// and its own code hash. `recorded` is written by the run itself (the skyline objective records its code state when a
// run ends); `declared` by a person, after a change, with the cause.
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Ledger } from '../gen/plugin';
import { root } from '../rag/config';
import { toolsHash } from './toolhash';

/**
 * The ledger file: `rel` under the repo, or `<id>.json` in $LEVELS_LEDGER when that is set (tests point it at a scratch
 * folder, so that a stub run does not write the committed ledger).
 */
export const ledgerPath = (rel: string, id: string): string =>
  (process.env.LEVELS_LEDGER ? resolve(process.env.LEVELS_LEDGER, `${id}.json`) : resolve(root, rel));

export const readLedger = (file: string): Ledger =>
  (existsSync(file) ? JSON.parse(readFileSync(file, 'utf8')) : { recorded: {}, declared: {} });

const save = (file: string, l: Ledger) => writeFileSync(file, JSON.stringify(l, null, 1) + '\n');

/** Adds `cause` to the causes declared for the code hash `hash` in the ledger `file`; returns them. */
export function declareCause(file: string, hash: string, cause: string): string[] {
  const l = readLedger(file);
  l.declared[hash] = [...new Set([...(l.declared[hash] ?? []), cause])];
  save(file, l);
  return l.declared[hash]!;
}

/** Records the code state `hash` (its first 16 hex digits, as the transit ledger keeps them) with a label. */
export function recordState(file: string, hash: string, label: string): void {
  const l = readLedger(file), key = hash.slice(0, 16), old = l.recorded[key];
  if (old?.split('; ').includes(label)) return;
  l.recorded[key] = old ? `${old}; ${label}` : label;
  save(file, l);
}

/** The command line of a ledger: `declare "<cause>"`, `record <hash> "<label>"` or `show`. */
export async function ledgerCli(args: string[], file: string, now: string) {
  const [cmd, ...rest] = args;
  if (cmd === 'declare' && rest.length) {
    console.log(`declared ${now.slice(0, 12)}: ${declareCause(file, now, rest.join(' ')).join('; ')}`);
  } else if (cmd === 'record' && rest.length >= 2) {
    recordState(file, rest[0]!, rest.slice(1).join(' '));
    console.log(`recorded ${rest[0]!.slice(0, 16)}`);
  } else console.log(`current ${now.slice(0, 12)}`, JSON.stringify(readLedger(file), null, 1));
}

const file = () => ledgerPath('tools/levels/drift.json', 'transit');
export const driftLedger = (): Ledger => readLedger(file());

if (import.meta.main) await ledgerCli(process.argv.slice(2), file(), await toolsHash());
