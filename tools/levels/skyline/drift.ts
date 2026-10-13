// The skyline objective's drift ledger. The world generator moves every day, so a skyline run's code hash will differ
// from the current one soon after the run:
//   - a change that leaves every rule the run recorded rerunning identically is labelled drift with no declaration;
//   - a rule whose result changes needs the change declared:
//       bun tools/levels/skyline/drift.ts declare "<what changed in the generator, and why that rule moved>"
//   - `recorded` (the code states committed runs were made under) is written by the run itself when it ends.
//   bun tools/levels/skyline/drift.ts show
import type { Ledger } from '../../gen/plugin';
import { ledgerCli, ledgerPath, readLedger, recordState } from '../drift';
import { skylineHash } from './hash';

const file = () => ledgerPath('tools/levels/skyline/drift.json', 'skyline');
export const skylineLedger = (): Ledger => readLedger(file());
export const recordSkyline = (hash: string, label: string): void => recordState(file(), hash, label);
export const skylineLedgerFile = 'tools/levels/skyline/drift.json';

if (import.meta.main) await ledgerCli(process.argv.slice(2), file(), await skylineHash());
