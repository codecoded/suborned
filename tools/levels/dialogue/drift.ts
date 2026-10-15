// The dialogue objective's drift ledger: which code states recorded runs were made under, and the declared causes of a change.
//   bun tools/levels/dialogue/drift.ts declare "<what changed, and why>" --rules <ID,ID,...|none>
//   bun tools/levels/dialogue/drift.ts show
import type { Ledger } from '../../gen/plugin';
import { ledgerCli, ledgerPath, readLedger, recordState } from '../drift';
import { dialogueHash } from './hash';

const file = () => ledgerPath('tools/levels/dialogue/drift.json', 'dialogue');
export const dialogueLedger = (): Ledger => readLedger(file());
export const recordDialogue = (hash: string, label: string): void => recordState(file(), hash, label);
export const dialogueLedgerFile = 'tools/levels/dialogue/drift.json';

if (import.meta.main) await ledgerCli(process.argv.slice(2), file(), await dialogueHash());
