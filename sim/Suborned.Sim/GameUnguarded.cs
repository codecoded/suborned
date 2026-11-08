// Engine 0.34.0 (brief 067; Motive briefs/067/motive.md §2.3, motive-values.json M067-K3; rulebook fight.unguarded): the end of a fight the
// cell can no longer fight. BAL-YARD-AT-WILL on 0.33.1: the Enforcer boards with Vale and is downed in the car, the Fixer and the Scout fall,
// the Tech sits in the car as no foe, Go is refused car-held, and the run idles to its end. GDD §06 says hiding in a held car loses; the engine
// had no way to lose that way.
//  (a) In the safe fight, with Vale in the getaway car, the car held (a holder stands; Motive's amendment, briefs/067/motive-square.md §4: a
//      free car with the cell aboard waits for Go) and no operative standing outside it, the rivals up and not broken (the blocker keeps
//      his lane) walk to the car, each to the side he comes to, firing as they walk and taking no cover or kite step; the first within
//      car.footprint.reach of its body takes Vale (rivals-reach-vale, RivalReachesVale). Read as each tick starts, after the last tick's
//      orders, so a Go given as the last Board lands is taken first (Motive: then no hash of the plans that board and go in one tick moves).
//      An operative standing outside again, or the car no longer held, sends them back to cover, as an advancing rival takes it (Advance). The walk is the fight AI's own
//      movement (an advance to a point), not a routine: nothing for Pulse's data.
//  (b) The cell down: the tick the last operative goes down (OpHit), the mission fails cell-down.
//  (c) Go needs a driver: refused car-no-driver with only the downed aboard (Go, GoQuery).
// Engine 0.35.2 (Motive briefs/039/motive/rulings-0.35.1.md §2; Sky's yes 2026-10-27): "an operative standing outside the car" in (a) and in
// RivalReachesVale's "no operative within cell.reach of the car" counts a drawn one only (gun out or drawing, not holstering: Guards). An
// undrawn agent standing by is no guard, so a cell that never draws beside a held car with Vale aboard loses rivals-reach-vale (0.35.1: the
// run idled to its close with no outcome). Pressing F answers it: a drawn agent outside sends the walkers back to cover.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>K3 (a): the rivals walking to the unguarded car (hashed in HashAi, only while any).</summary>
        readonly SortedSet<string> forCar = new SortedSet<string>(StringComparer.Ordinal);
        /// <summary>K3 (a), read-only: the rivals walking to the getaway car now because nobody of the cell stands outside it (ids, sorted).</summary>
        public IReadOnlyCollection<string> RivalsForCar => forCar;

        /// <summary>Engine 0.35.2 (Motive rulings-0.35.1 §2): an operative who guards the car: up, on the street outside it, and drawn (gun out
        /// or drawing; a gun being holstered doesn't guard). An undrawn agent standing by is no guard.</summary>
        static bool Guards(Op o) => !o.Down && !o.InCar && !o.Inside && o.Gun.Drawn && o.Gun.HolsterAt < 0;

        /// <summary>K3 (a): no drawn operative stands outside the getaway car, held (a holder stands), with Vale in it, in the safe fight still on.</summary>
        bool CarUnguarded() => !Range && FightBranch == "safe" && FightOverTick == INF && Vale == ValeAt.Car && !Array.Exists(Ops, Guards) && (CarHeld() || GetawayBoxers().Contains("car:rivals")); // engine 0.42.0: the rival car's box holds it too (F1 keeps the wheelman at the wheel; the box is the gate)

        /// <summary>K3 (a), once a tick as it starts: the rivals go for the unguarded car, or back to cover once it is guarded again.</summary>
        void UnguardedStep()
        {
            if (Over) return;
            if (!CarUnguarded())
            {
                if (forCar.Count == 0) return;
                bool outside = Array.Exists(Ops, Guards);
                foreach (var id in forCar)
                {
                    var h = HostileOf(id); if (h == null || !Up(h) || !h.Present) continue; // the fight over: the walkers are down or broken
                    h.Gun.St = "advance"; Advance(h); // as an advancing rival: the cover the data give with the Enforcer (or the car) as its foe, else its stop point
                    Note("rivals.back-to-cover", outside ? $"{h.Id} sees the cell drawn outside the car again and goes back to cover." : $"{h.Id} sees the car is no longer held and goes back to cover.");
                }
                forCar.Clear();
                return;
            }
            List<string>? quit = null;
            foreach (var h in Hostiles)
            {
                if (!IsRival(h.Id) || !h.Present || !Up(h)) { if (forCar.Contains(h.Id)) (quit ??= new List<string>()).Add(h.Id); continue; }
                if (h.Blocker) { if (forCar.Contains(h.Id)) (quit ??= new List<string>()).Add(h.Id); continue; } // he keeps his lane (picked blocker later: he goes back to it)
                if (forCar.Contains(h.Id)) { if (!h.Moving(Tick) && !AtGetaway(h.Pos(Tick))) ToCar(h); continue; } // stopped short (a walk round a body or a car): on again
                forCar.Add(h.Id); ReleaseCover(h.Gun); h.Gun.St = "engage"; ToCar(h);
                Note("rivals.for-car", $"{h.Id} sees nobody of the cell guarding the car and goes for {ValeName}.");
            }
            if (quit != null) foreach (var id in quit) forCar.Remove(id);
        }

        /// <summary>K3 (a): a walk to the car's side the rival comes to (WalkGrid.RouteBeside), from the free cell nearest him.</summary>
        void ToCar(Hostile h)
        {
            var from = h.Pos(Tick); var a = Walk.NearestFree(from) ?? from;
            var route = Walk.RouteBeside(a, Car);
            if (route == null || route.Length < 2) { h.At = from; h.Run = null; return; }
            h.Run = new Path(route); h.RunStart = Tick; h.At = h.Run.End; h.Why = "press"; h.Closing = false; CommitEnd(h.Gun); // engine 0.50.0 (fight.cover): the press is his crew's plan, a reason to leave
            if (h.Run.Length == 0) h.Run = null;
        }

        /// <summary>K3 (a): a rival going for the car fires as he walks at whoever he may shoot (the final fight's foes), with no cover or kite
        /// step: the walk is the press's. True when it handled the body this tick (FightBody returns).</summary>
        bool ForCarBody(Hostile h)
        {
            if (!forCar.Contains(h.Id)) return false;
            var g = h.Gun; var pos = h.Pos(Tick);
            var foes = FoesInReach(h.Id, g, pos, p => Sees(pos, p), true);
            if (foes.Count == 0) { LoseTarget(g); return true; }
            string t = ChooseTarget(h.Id, g, pos, foes); var at = PosOf(t)!.Value;
            AimAt(g, t, false);
            if (Tick >= g.NextShot) Shoot(h.Id, g, pos, t, at, Dist(pos, at), h.Moving(Tick) ? Gait.Walk : Gait.Stand, false);
            return true;
        }

        /// <summary>K3 (b): the tick the last operative goes down (in the car or not), the mission fails.</summary>
        bool CellDown(string name)
        {
            if (Range || !Array.TrueForAll(Ops, o => o.Down)) return false;
            Fail("cell-down", "The whole cell is down.", () => ShotDown("shot", name)); // the fail camera: the shot that downed the last to fall (why shot, as enforcer-down's)
            return true;
        }

        /// <summary>K3 (c): someone standing is in the getaway car to drive it.</summary>
        bool CarHasDriver() => Array.Exists(Ops, o => o.InCar && !o.InLent && !o.Down); // engine 0.42.0: the cell's car (the lent car's is LentHasDriver)
    }
}
