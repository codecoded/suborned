// Take car (engine 0.53.0, brief 105 round 2; Motive briefs/105/motive/round2.md §1; Sky 2026-11-10: "if everyone is out, and there's other
// cars still around, then they can take those ones e.g. the rivals car"; rulebook car.take). The lend generalised: any car the mission
// simulates becomes the second cell car (lentId) when it stands (StandingCars), is not disabled or burning, and nobody is at its wheel.
//  · The order: take-car <car>, from within car.footprint.reach of its body, by any standing operative (one carrying Vale or a downed agent
//    takes them in too, seats allowing). board-s later he is at the wheel (car.taken); from then on it has its own Board, Step out and Go: the
//    lend's (GameLend.cs), with the lend's prices (L5, L7, L8, L9) and its hold, lane and box.
//  · At the wheel: a passing car's driver (or none: abandoned) gets out and runs from a drawn gun (refused draw-first undrawn; DriverOut, the
//    traffic law's flight); the rival wheelman is the Enforcer's alone (refused car-wheelman for anyone else): knocked out at the door, the
//    takedown's ko-s on top of board-s, Vale in hand or not (Motive's check (a): the only way into a blocking rival car with Vale out). The official car with its driver at the wheel is the in-car Take's (car-driver).
//  · Seats: car.take.seats by kind (a passing car's model; the rival car "rivals"); Vale takes one, a carried agent one; Board past them is
//    car-full. The official car keeps the lend's L3.
//  · Its way out: the first fight-block-m straight on from where it stands (the lent car's lane law at the kerb); a car standing on it boxes it.
//  · One second car at a time: refused car-taken while one stands that is ours and still drives.
// Hashed in HashLend from the take on (no bytes until a car is taken).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>The second cell car's id: car:official (the lend) until a Take car takes another. Host read: its ring, card and Go.</summary>
        public string LentId => lentId;
        string lentId = "car:official"; Pt takenAt, takenHead;
        bool RivalCarTaken => Lent && lentId == "car:rivals";
        /// <summary>Where the second cell car stands: the official car's own place, or where a taken car was taken.</summary>
        Pt LentPos() => lentId == "car:official" ? CarPos() : takenAt;
        Pt[] LentWay() => lentId == "car:official" ? OfficialWay() : lentLane;
        List<string> LentBoxers() => lentId == "car:official" ? OfficialBoxers() : Range || lentLeft ? new List<string>() : CarsOn(lentLane, lentId);
        string LentName => lentId == "car:official" ? $"{ValeName}'s car" : "the " + CarName(lentId);
        /// <summary>The free ground 1.5 m to one side of a car (side +1 its right, the kerb side), across its heading.</summary>
        Pt Beside(Pt car, Pt h, int side)
        {
            long n = G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y); if (n == 0) { h = new Pt(0, 100); n = 100; }
            var right = new Pt((int)(h.Y * 150L / n), (int)(-h.X * 150L / n));
            return Walk.NearestFree(car + new Pt(right.X * side, right.Y * side)) ?? car;
        }

        int SeatsOf(string id) => id == "car:rivals" ? R.TakeSeat("rivals") : TrafficCarById(id) is TrafficCar tc ? R.TakeSeat(tc.Model) : -1;
        /// <summary>The seats an operative boarding takes: himself, whoever he carries, and Vale with the Enforcer.</summary>
        int SeatsFor(Op op) => 1 + (op.Carrying.HasValue ? 1 : 0) + (op.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer ? 1 : 0);
        int SeatsTaken() { int n = Vale == ValeAt.Lent ? 1 : 0; foreach (var o in Ops) if (o.InLent && !o.LeftMap) n++; return n; }
        string? SeatsFull(Op op, string? id = null)
        {
            int seats = SeatsOf(id ?? lentId), need = SeatsFor(op), have = id == null ? SeatsTaken() : 0;
            return have + need > seats ? No("car-full", $"{(id == null ? Cap(LentName) : "That car")} seats {seats}: no room for the {Sim.Op.Name(op.Id)}{(need > 1 ? " and who he carries" : "")}") : null;
        }

        /// <summary>Take car's refusal for this operative and car now, or "" (the order's check and the ring's card).</summary>
        string TakeCarWhy(Op op, string id)
        {
            if (!LendOn) return No("unknown-verb", "take-car runs with car.lend.split 1 (the two cars)");
            if (op.InCar) return No("in-car", $"the {Sim.Op.Name(op.Id)} is in a car: a move steps out first");
            if (op.Running) return No("board-running", "a running operative doesn't board: stop first");
            if (op.Id == OpId.Tech && Tethered) return No("board-tethered", "the Tech is tethered to the cabinet while the loop holds");
            if (op.CarryBody.Length > 0) return No("already-carrying", "carrying a body: the car seats the cell and the target");
            if (id == "car:getaway" || id == "car") return No("car-ours", "the getaway car is ours: Board it");
            if (Lent && !lentLeft && !CarDisabled(lentId)) return No(lentId == id ? "car-ours" : "car-taken", lentId == id ? $"{Cap(LentName)} is ours: Board it" : $"{Cap(LentName)} is ours already: one taken car at a time");
            if (Lent && lentLeft) return No("car-taken", $"{Cap(LentName)} has left: one taken car a mission");
            StandingCar? sc = null; foreach (var c in StandingCars()) if (c.Id == id) sc = c;
            if (sc == null) return No(CarAt(id).HasValue ? "car-moving" : "target-unknown", CarAt(id).HasValue ? "that car is driving: only a standing car can be taken" : $"'{id}' is no car standing here (unknown = reject)");
            if (CarDisabled(id) || CarBurning(id) || (CarOf(id) is CarTarget ct && ct.Blown)) return No("car-disabled", $"the {CarName(id)} is disabled");
            int d = CarBodyCm(id, op.Pos); if (d < 0 || d > R.CarReachCm) return No("stand-at-car", $"stand at the {CarName(id)}", cm: R.CarReachCm);
            if (id == "car:official")
            {
                if (!Driver.Down && driverOff == null) return No("car-driver", $"{Cap(ValeName)}'s driver is at the wheel: the in-car Take lends it");
                return "";
            }
            if (SeatsOf(id) < 0) return No("car-unknown", $"the {CarName(id)} has no seat count (car.take.seats; unknown = reject)");
            if (SeatsFull(op, id) is string full) return full;
            if (id == "car:rivals")
            {
                foreach (var m in Crew) if (m != Wheelman && Up(m) && m.Mode == CrewMode.Aboard) return No("car-crewed", $"{m.Id} is still in the rival car");
                if (WheelmanIn && op.Id != OpId.Enforcer) return No("car-wheelman", "the wheelman is at the wheel: the Enforcer takes him out", ids: Wheelman.Id);
            }
            else if (TrafficCarById(id) is TrafficCar tc && tc.Out == null && tc.Mode != TrafficMode.Abandoned && !(op.Gun.Drawn && op.Gun.HolsterAt < 0))
                return No("draw-first", $"the {CarName(id)}'s driver is at the wheel: draw first");
            return "";
        }
        bool WheelmanIn => Up(Wheelman) && Wheelman.Mode == CrewMode.Aboard;

        string TakeCar(Op op, Order o)
        {
            var why = TakeCarWhy(op, o.Target); if (why.Length > 0) return why;
            op.Gun.Order = ""; op.Gun.Force = false; op.Gun.Target = "";
            bool carrying = op.Carrying.HasValue || CarriesVale(op);
            Engage(op, o, R.BoardS + (carrying ? R.CarryS : 0) + (o.Target == "car:rivals" && WheelmanIn ? R.KoS : 0));
            return "";
        }

        void LandTakeCar(Op op, Order o)
        {
            if (op.Down) return;
            var id = o.Target; bool wheel = id == "car:rivals" && WheelmanIn && op.Id == OpId.Enforcer;
            if (wheel) WheelmanOut(op); // the takedown at the door, before the rest of the law is read again
            var why = TakeCarWhy(op, id);
            if (why.Length > 0) { Note("take-car.missed", $"The {Sim.Op.Name(op.Id)} can't take the {CarName(id)}: {why}."); return; }
            StandingCar sc = default; foreach (var c in StandingCars()) if (c.Id == id) sc = c;
            if (TrafficCarById(id) is TrafficCar tc && tc.Out == null) DriverOut(tc, op.Pos); // the driver gets out and runs from the drawn gun
            if (id == "car:rivals") CarHalt(CrewCar.Stopped, ccStop, sc.At, sc.Heading);
            bool first = !Lent; lentId = id; Lent = true; lentLeft = false; if (first) LentTick = Tick;
            if (id == "car:official") { lentLane = new Path(OfficialWay()).Cut((long)S.GroundOf(R).BlockCm * 10).Points; lentHalf = R.BoxHalfCm; }
            else { takenAt = sc.At; takenHead = sc.Heading; lentLane = new[] { sc.At, CarGeo.Ahead(sc.At, sc.Heading, S.GroundOf(R).BlockCm) }; lentHalf = R.BoxHalfCm; }
            long bd = long.MaxValue; foreach (var st in S.Stands) { long d = G.D2(st.CarAt, LentPos()); if (d < bd) { bd = d; lentStand = st; } }
            op.InCar = true; op.InLent = true; op.Pos = LentSeat; op.Walk = null; op.WalkMm = 0; op.Running = false;
            string with = "";
            if (op.Carrying.HasValue) { var c = OpOf(op.Carrying.Value); op.Carrying = null; c.CarriedBy = null; c.InCar = true; c.InLent = true; c.Pos = op.Pos; with = " with the " + Sim.Op.Name(c.Id); }
            if (op.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer) { Vale = ValeAt.Lent; with = $" with {ValeName}"; Chain.Add("vale-in-taken-car"); }
            Chain.Add("car-taken:" + id.Substring(4));
            Note("car.taken", $"The {Sim.Op.Name(op.Id)} takes the {CarName(id)}{with}: it is ours now. Board it, then Go.");
        }

        /// <summary>The rival wheelman knocked out at the door by the Enforcer (takedown ko-s): out on the road side, down.</summary>
        void WheelmanOut(Op op)
        {
            var cp = RivalCarPose(); var at = cp.HasValue ? Beside(cp.Value.pos, cp.Value.heading, -1) : op.Pos;
            var w = Wheelman; w.P.Down = true; CrewSeized(w, at); w.OutAt = INF; // down at the door: no longer aboard (Inside) for a blast
            Chain.Add("wheelman-taken-out"); Note("take-car.wheelman", $"The Enforcer pulls {w.Id} out of the rival car and knocks him out.");
        }

        /// <summary>A taken car's Go (GoLent): it leaves the street; the rival car's routine and a passing car's route end with it.</summary>
        void TakenLeaves()
        {
            if (lentId == "car:rivals") CarHalt(CrewCar.Gone, ccStop, takenAt, takenHead);
            else if (TrafficCarById(lentId) is TrafficCar tc) tc.Gone = true;
        }
    }
}
