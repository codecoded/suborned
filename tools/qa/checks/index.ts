// Brief 060 (Suture): the tier-1 invariant checks over the engine's play output, as one module a runner calls (Splice's `bun run qa`).
//   checkPlay(play, item, ctx)  every per-play check; a list of findings (empty: clean)
//   checkTwins(pairs)           Q-DET across plays: the same script played twice (another thread, another moment) ends on the same hash
//   checkRoute(play, item)      Q-ROUTE: an unmutated route ends as routes.json expects (outcome, fail code)
import type { Ctx, Finding, Item, Play } from '../types';
import { qDebrief, qDet, qErr, qFail } from './outcome';
import { qDrop, qLearn, qOffer } from './orders';
import { qSpawn, qStuck } from './stuck';
import { qWarn } from './warn';

export { CLAIMS } from './outcome';
export const CHECKS = ['Q-ERR', 'Q-DET', 'Q-FAIL', 'Q-DEBRIEF', 'Q-WARN', 'Q-STUCK', 'Q-SPAWN', 'Q-OFFER', 'Q-DROP', 'Q-LEARN', 'Q-ROUTE'] as const;

export function checkPlay(p: Play, it: Item, ctx: Ctx): Finding[] {
  const err = qErr(p); if (p.error) return err;
  if (it.twin) return [...err, ...qDet(p)]; // a twin is there for Q-DET (checkTwins); its other findings are its seed's
  return [...err, ...qDet(p), ...qFail(p, ctx), ...qDebrief(p), ...qWarn(p, ctx), ...qStuck(p, ctx), ...qSpawn(p), ...qOffer(p, it), ...qDrop(p, it), ...qLearn(p), ...checkRoute(p, it)];
}

export function checkTwins(a: Play, b: Play): Finding[] {
  if (a.error || b.error || a.finalHash === b.finalHash) return [];
  return [{ check: 'Q-DET', severity: 'S1', key: 'Q-DET:twin', title: 'The same script played twice ends on different hashes', expected: `final hash ${a.finalHash} both times`, observed: `${a.finalHash} and ${b.finalHash}`, owner: 'Keel' }];
}

export function checkRoute(p: Play, it: Item): Finding[] {
  const want = it.seed.expects;
  if (p.error || it.mutations.length || it.seed.kind !== 'route' || !want?.outcome) return [];
  const wantFail = (want as any).fail as string | undefined;
  if (p.outcome === want.outcome && (!wantFail || wantFail === p.fail)) return [];
  return [{ check: 'Q-ROUTE', severity: 'S2', key: `Q-ROUTE:${it.seed.id}`, title: `Route ${it.seed.id} no longer ends as routes.json says`, expected: `${want.outcome}${wantFail ? ' (' + wantFail + ')' : ''}`, observed: `${p.outcome}${p.fail ? ' (' + p.fail + ')' : ''} at ${Math.floor(p.tick / 10)} s`, tick: p.tick, owner: 'Waymark' }];
}
