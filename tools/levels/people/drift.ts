// The people objective's drift ledger: which code states recorded runs were made under, and the declared causes of a change.
//   bun tools/levels/people/drift.ts declare "<what changed, and why>" --rules <ID,ID,...|none>
//   bun tools/levels/people/drift.ts show
import type { Ledger } from '../../gen/plugin';
import { ledgerCli, ledgerPath, readLedger, recordState } from '../drift';
import { peopleHash } from './hash';

const file = () => ledgerPath('tools/levels/people/drift.json', 'people');
export const peopleLedger = (): Ledger => readLedger(file());
export const recordPeople = (hash: string, label: string): void => recordState(file(), hash, label);
export const peopleLedgerFile = 'tools/levels/people/drift.json';

if (import.meta.main) await ledgerCli(process.argv.slice(2), file(), await peopleHash());
