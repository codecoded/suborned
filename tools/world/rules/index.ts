// The world rules that live outside tools/plan (brief 007 round 3 walk), registered with the plan's in
// tools/plan/world-rules.ts: `bun tools/plan/cli.ts`, the sheet, `bun run world` and the skyline checker run them with the
// rest. F-7 signs, SF-18 pylons, L-8 connector ends, SF-19 crossings, CD-1..4 the civic dressing, SF-21 brass is thin, W-5 the walking clearance. Tests: rules.test.ts, a failing example each.
import type { Check } from '../../plan/checks';
import type { WCtx } from '../../plan/world-ctx';
import { civicDressRules } from './civic-dress';
import { connectorRules } from './connectors';
import { crossingRules } from './crossings';
import { pylonRules } from './pylons';
import { signRules } from './signs';
import { trimRules } from './trim';
import { walkingRules } from './walking';

export const extraRules = (c: WCtx): Check[] => [...signRules(c), ...pylonRules(c), ...connectorRules(c), ...crossingRules(c), ...civicDressRules(c), ...trimRules(c), ...walkingRules(c)];
