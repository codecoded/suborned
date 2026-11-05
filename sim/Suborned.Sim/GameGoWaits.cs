// Brief 096's misses round (engine 0.43.0, Keel; Motive briefs/096/motive/misses.html, misses.json; Sky 2026-11-05 "all recommended"):
//  · G1 (rulebook car.go, waits "board"): a Go given on a car while an agent is climbing into it (a Board under way: board.s, or board.s +
//    carry.s with someone carried) waits for that Board and lands the tick it lands. The Go's checks that the climb can't change (boxed in,
//    held, gated, the car gone) still refuse at once; an empty car, or one with only the downed aboard, waits for the agent climbing in. Those
//    climbing in count as aboard on the Go's card (LeftBehind). Shot down or called off mid-climb, the Go lands once nobody it waits for is
//    still climbing in; refused then (a holder came), the log says so (go.missed), with the refusal's code.
//  · Host read (Latch): who would see Popovic's car leave now (LentCarWatcher), for the lent car's Go card's "+1 T if seen".
// Hashed in HashGoWaits, no bytes while no Go waits: a run that never gives a Go during a Board hashes as 0.42.0.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>A Go waiting for a Board under way, by car ("car" ours, "car:official" the lent car): the order and the agents it waits for.</summary>
        readonly SortedDictionary<string, (Order o, int mask)> goWaits = new SortedDictionary<string, (Order, int)>(StringComparer.Ordinal);

        static string GoCar(string target) => LentTarget(target) ? "car:official" : "car";
        /// <summary>The agent is climbing into this car now (a Board under way, not down).</summary>
        bool ClimbingInto(Op op, string car) => R.GoWaitsBoard && !op.Down && op.Busy is Order b && b.Do == "board" && op.BusyEnd > Tick && GoCar(b.Target) == car;
        /// <summary>The agents climbing into this car now, as a mask by OpId.</summary>
        int ClimbingMask(string car) { int m = 0; foreach (var o in Ops) if (ClimbingInto(o, car)) m |= 1 << (int)o.Id; return m; }
        /// <summary>Host read: someone is climbing into this car ("car" or "car:official") now, so a Go would wait for him (G1).</summary>
        public bool GoWouldWait(string car) => ClimbingMask(GoCar(car)) != 0;
        /// <summary>Host read: a Go on this car is waiting for a Board under way.</summary>
        public bool GoWaiting(string car) => goWaits.ContainsKey(GoCar(car));

        /// <summary>G1 at the Go: with the checks the climb can't change passed, an agent climbing in makes the Go wait; "" taken, null not waiting.</summary>
        string? GoWaitFor(Order o)
        {
            string car = GoCar(o.Target); int m = ClimbingMask(car); if (m == 0) return null;
            if (goWaits.ContainsKey(car)) return ""; // already waiting: the same Go again changes nothing
            goWaits[car] = (o, m);
            var names = new List<string>(); foreach (var x in Ops) if ((m & (1 << (int)x.Id)) != 0) names.Add("the " + Sim.Op.Name(x.Id));
            Note("go.waits", $"{(car == "car" ? "Our car" : Cap(ValeName) + "'s car")} waits for {string.Join(" and ", names)} climbing in, then goes.");
            return "";
        }
        /// <summary>Once a tick, after the Boards land: a waiting Go whose agents are all in (or down, or called off) is given now.</summary>
        void GoWaitStep()
        {
            if (goWaits.Count == 0) return;
            foreach (var car in new List<string>(goWaits.Keys))
            {
                if (Over) return;
                var (o, m) = goWaits[car]; bool still = false;
                foreach (var x in Ops) if ((m & (1 << (int)x.Id)) != 0 && ClimbingInto(x, car)) still = true;
                if (still) continue;
                goWaits.Remove(car);
                var saved = refusal; refusal = default;
                var why = Go(o); var code = refusal.code ?? "uncoded"; refusal = saved;
                if (why.Length > 0) Note("go.missed", $"The Go on {(car == "car" ? "our car" : ValeName + "'s car")} doesn't land: {code}: {why}");
            }
        }

        /// <summary>Host read (Latch, the lent car's Go card's "+1 T if seen"): the security body that would see Popovic's car leave now, the first
        /// by id, or "" (as CarWatcher for ours); "" while the car isn't ours to drive.</summary>
        public string LentCarWatcher() => Lent && !lentLeft ? CarWatcherAt(CarPos()) : "";

        internal void HashGoWaits(Hash.H h)
        {
            if (goWaits.Count == 0) return;
            h.S("go-waits"); foreach (var kv in goWaits) h.S(kv.Key).S(kv.Value.o.ToString()).I(kv.Value.mask);
        }
    }
}
