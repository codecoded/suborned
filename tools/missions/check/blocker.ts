// Brief 018 §1 read from a play's log: when the hostiles the engine sent for the road were all out of the fight. The engine logs it
// (Keel, engine 0.7.x): `blocker.out` once every blocker is down, broken or gone, and `car.free` when a held car comes free. The
// composer's `when: "blocker.out"` waits for the code itself; the judge's MC-11 (every reference win's car was free when it left)
// reads this.
type Line = { tick: number; code: string; text: string };
/** The tick every blocker was out (the engine's `blocker.out`), or undefined if none was picked or one still stands. */
export const blockerOut = (log: Line[]): number | undefined => log.find(l => l.code === 'blocker.out')?.tick;
/** The tick a held car came free (the engine's `car.free`), or undefined if it was never held or never came free. */
export const carFree = (log: Line[]): number | undefined => log.find(l => l.code === 'car.free')?.tick;
/** Engine 0.41.0's block (brief 096, engine 0.43.0 reading; Waymark's MC-11/MC-14): where the rival car boxes the stand in it is the final
 *  fight's block, no body is picked as blocker and the way clears when it drives off or explodes (`car.unboxed`). On the lend (engine 0.42.0)
 *  the win is fixed as Popovic's car leaves and the crew springs that tick (L7): the fight has come by rule, so a lend win the police end
 *  with the box still standing reads its spring (`fight.ambush`). The tick: `blocker.out`, else `car.unboxed`, else a lend win's spring. */
export const wayFreeAt = (marks: Record<string, number>): number | undefined => marks['blocker.out'] ?? marks['car.unboxed'] ?? ('vale.out' in marks ? marks['fight.ambush'] ?? marks['vale.out'] : undefined);
export const wayFree = (log: Line[]): number | undefined => { const m: Record<string, number> = {}; for (const l of log) if (!(l.code in m)) m[l.code] = l.tick; return wayFreeAt(m); };
/** A hold by one of the rival crew while their car boxes the stand in (rivals.block or rivals.back, until car.unboxed) came for the car:
 *  the crew posted round a car they blocked on arrival (Sky 2026-11-04: "they see your car, so they come, block you off and get out"). */
export const boxCrewHold = (marks: Record<string, number>, h: { tick: number; who: string }): boolean => {
  const from = Math.min(marks['rivals.block'] ?? Infinity, marks['rivals.back'] ?? Infinity), to = marks['car.unboxed'] ?? Infinity;
  return /^rival-\d+$/.test(h.who) && h.tick >= from && h.tick < to;
};
