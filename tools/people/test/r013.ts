// The r013 cast, the table the promoted cast was until 2026-10-16 (r016 replaced it). Tests that pin what r013 made, byte for byte or by id, read it here, from
// the run's own accepted output (committed, and the file it was promoted from), never from contracts/people/harbour-district.cast.json, which is whatever cast is
// promoted now. The bytes are held to the sha256 the r013 promotion record carried, so this fixture cannot drift from the run.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { Cast } from '../../../contracts/people/cast.schema';
import { sha256 } from '../../gen/sha';
import { root } from '../../rag/config';
import { castBytes } from '../promoted';

export const R013 = { file: 'runs/levels/harbour-district/r013/round-2/pulse.json', sha256: 'd4d8e2e70cb9a3ea28e9a29b6f1349e1fdec6cf8e3d1db95475f64b293f0476e' };

/** r013's accepted cast (round 2), held to the sha256 of the cast file r013's promotion record named. */
export function r013Cast(): Cast {
  const raw = (JSON.parse(readFileSync(resolve(root, R013.file), 'utf8')) as { cast: unknown }).cast;
  if (sha256(new TextEncoder().encode(castBytes(raw))) !== R013.sha256) throw new Error(`${R013.file}: its cast is no longer the r013 cast the tests pin`);
  return Cast.parse(raw);
}
