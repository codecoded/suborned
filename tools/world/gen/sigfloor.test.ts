// A public floor declared inside a signature structure (brief 075, SpaceHQ "ours": the S1 station's banks stand in the drum's ground hall). The rule reads the
// plan's declaration (`signatures[].levels[]` with `floor: 'public'`) over the signature's ground less its legs; never an id. Failing examples: no declaration,
// a point on a leg, a point outside the ground, another level.
import { expect, test } from 'bun:test';
import type { Ctx } from './common';
import { onSignatureFloor } from './connectors';

const sig = (levels: unknown[]) => ({ id: 'drum', name: 'Drum', c: [0, 244], ground: [-40, 40, 204, 284], legs: [[-40, 40, 264, 284], [-40, 40, 204, 224]], levels });
const ctxOf = (levels: unknown[]) => ({ plan: { signatures: [sig(levels)] } }) as unknown as Ctx;

test('a declared public street floor makes the ground hall between the legs public ground', () => {
  const c = ctxOf([{ level: 'street', floor: 'public' }, { level: 'concourse' }]);
  expect(onSignatureFloor(c, [0, 255])).toBe(true);
  expect(onSignatureFloor(c, [-39, 226])).toBe(true);
});

test('failing examples: on a leg, outside the ground, undeclared, or another level is not public', () => {
  const c = ctxOf([{ level: 'street', floor: 'public' }]);
  expect(onSignatureFloor(c, [0, 270])).toBe(false); // on the north leg
  expect(onSignatureFloor(c, [0, 210])).toBe(false); // on the south leg
  expect(onSignatureFloor(c, [45, 244])).toBe(false); // outside the ground
  expect(onSignatureFloor(c, [0, 244], 'concourse')).toBe(false); // the concourse floor is not declared
  expect(onSignatureFloor(ctxOf([{ level: 'street', what: 'the Parade passes through' }]), [0, 255])).toBe(false); // described, not declared
  expect(onSignatureFloor({ plan: { signatures: [] } } as unknown as Ctx, [0, 255])).toBe(false);
  expect(onSignatureFloor(ctxOf([{ level: 'concourse', floor: 'public' }]), [0, 244], 'concourse')).toBe(true);
});
