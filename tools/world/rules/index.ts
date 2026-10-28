// The world rules that live outside tools/plan (brief 007 round 3 walk), registered with the plan's in
// tools/plan/world-rules.ts: `bun tools/plan/cli.ts`, the sheet, `bun run world` and the skyline checker run them with the
// rest. F-7 signs, SF-18 pylons, L-8 connector ends, SF-19 crossings, SF-24 the continuous footways at the gates, CD-1..4 the civic dressing, SF-21 brass is thin, W-5 the walking clearance, R-HIDE / R-STASH / R-REPORT / R-DOCK the takedown places (brief 013), RF-3 the road finish, KR-2 and KR-3 the kerb returns and the lay-by (brief 028), K-6 no facade piece leaves its building (brief 072, contain.ts), CW-1 the building colourways (brief 074, colourways.ts), LV-1 the landmark livery (brief 075-C, livery.ts). Tests: rules.test.ts, a failing example each.
import type { Check } from '../../plan/checks';
import type { WCtx } from '../../plan/world-ctx';
import { civicDressRules } from './civic-dress';
import { containRules } from './contain';
import { colourwayRules } from './colourways';
import { liveryRules } from './livery';
import { capstoneRules } from './capstone';
import { connectorRules } from './connectors';
import { crossingRules } from './crossings';
import { footwayRules } from './footway';
import { pylonRules } from './pylons';
import { roadFinishRules } from './roadfinish';
import { kerbReturnRules } from './kerbreturn';
import { quietRules } from './quiet';
import { signRules } from './signs';
import { trimRules } from './trim';
import { walkingRules } from './walking';

export const extraRules = (c: WCtx): Check[] => [...signRules(c), ...pylonRules(c), ...connectorRules(c), ...crossingRules(c), ...footwayRules(c), ...civicDressRules(c), ...trimRules(c), ...walkingRules(c), ...quietRules(c), ...capstoneRules(c), ...roadFinishRules(c), ...kerbReturnRules(c), ...containRules(c), ...colourwayRules(c), ...liveryRules(c)];
