import { buildPeople } from '../../../tools/sim/people';
import { writeFileSync } from 'node:fs';
const [inp, out] = process.argv.slice(2);
writeFileSync(out!, buildPeople({ people: inp! }));
