import { z } from 'zod';

export function campaignFields(ref: z.ZodString, text: z.ZodString) {
  return {
    districtEconomy: z.strictObject({ help: ref, surety: ref, honestFee: ref, bluffFee: ref }).optional(),
    missing: z.strictObject({ trail: ref, tiesFear: ref }),
    areaWatch: z.strictObject({ step: ref, missions: ref }),
    // The mission checker uses trailEq to price an open case.
    cases: z.strictObject({ perLevel: ref, cap: ref, close: ref, trailEq: ref }),
    // Earlier recorded campaigns predate custody.
    custody: z.strictObject({
      trail: ref, fear: ref, loyalty: ref, abandonedResentment: ref, abandonedLoyalty: ref, missions: ref,
      cellStart: z.strictObject({ bravery: ref, resentment: ref, fear: ref, loyalty: ref }),
      rule: text, src: text,
    }).optional(),
    rule: text,
  };
}
