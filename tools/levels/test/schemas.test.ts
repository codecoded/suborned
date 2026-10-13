// Transit's recorded output schemas are the contract of ten committed runs: building them from the shared factories (tools/gen/verdict.ts)
// must not change them. The fixture was dumped from the schemas before the factories existed.
import { describe, expect, test } from 'bun:test';
import { join } from 'node:path';
import { z } from 'zod';
import { verdictOut } from '../../gen/verdict';
import { agents } from '../schemas';

const recorded = await Bun.file(join(import.meta.dir, 'fixtures/transit-schemas.json')).json();

describe('transit schemas', () => {
  for (const key of Object.keys(agents) as (keyof typeof agents)[]) {
    test(`${key}: the JSON schema is the recorded one`, () => {
      expect(z.toJSONSchema(agents[key])).toEqual(recorded[key]);
    });
  }
  test('verdictOut(owners) names only the objective\'s owners', () => {
    const skyline = verdictOut(z.enum(['Datum', 'Vantage'])), fault = { owner: 'Pulse', rule: 'critic', fault: 'x', fix: 'y', chunks: ['0'.repeat(24)] };
    const verdict = { verdict: 'fail', summary: '', faults: [fault], sources: [] };
    expect(skyline.safeParse(verdict).success).toBe(false);
    expect(skyline.safeParse({ ...verdict, faults: [{ ...fault, owner: 'Vantage' }] }).success).toBe(true);
    expect(agents.keel.safeParse(verdict).success).toBe(true);
  });
});
