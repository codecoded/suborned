// Brief 018 §1 read from a play's log: when the hostiles the engine sent for the road were all out of the fight. The engine logs it
// (Keel, engine 0.7.x): `blocker.out` once every blocker is down, broken or gone, and `car.free` when a held car comes free. The
// composer's `when: "blocker.out"` waits for the code itself; the judge's MC-11 (every reference win's car was free when it left)
// reads this.
type Line = { tick: number; code: string; text: string };
/** The tick every blocker was out (the engine's `blocker.out`), or undefined if none was picked or one still stands. */
export const blockerOut = (log: Line[]): number | undefined => log.find(l => l.code === 'blocker.out')?.tick;
/** The tick a held car came free (the engine's `car.free`), or undefined if it was never held or never came free. */
export const carFree = (log: Line[]): number | undefined => log.find(l => l.code === 'car.free')?.tick;
