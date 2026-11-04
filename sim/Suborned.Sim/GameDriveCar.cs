// The getaway car's Drive (brief 059 K6; GDD v0.25.0 §07 getaway; Sky's answer 1a; rulebook cell.driveCar; Motive M059-2.1, M059-2.2).
// Its own engine id, drive-car: `drive` is the take-control playground's (brief 025). An operative in the getaway car orders it to one of
// the mission's car stands (site getaway.stands); it drives the drive grid at car.pace and everyone in it rides, busy until it arrives. The
// car's position, its seats, its exit lane, the fight's spots and the cover it gives are the current stand's (CurStand). $0, 0 T, no Alert.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>The stand the getaway car is at (or left, while it drives).</summary>
        public Stand CurStand = null!;
        /// <summary>The stand it is driving to, or null.</summary>
        public Stand? GetawayDest;
        Path? getawayPath; long[]? getawayProf; int getawayStart;
        /// <summary>The getaway car drives now (a Drive under way).</summary>
        public bool GetawayDriving => getawayPath != null;
        /// <summary>Where the getaway car is: its stand's car point, or along its Drive.</summary>
        public Pt GetawayCarPos()
        {
            if (getawayPath == null) return CurStand.CarAt;
            int k = Math.Max(0, Math.Min(Tick - getawayStart, getawayProf!.Length - 1));
            return getawayPath.At(getawayProf[k]);
        }
        /// <summary>The getaway car's heading for the host (never hashed): along its Drive, else its stand's facing.</summary>
        public Pt GetawayHeading()
        {
            if (getawayPath == null) return CurStand.Facing;
            int k = Math.Max(0, Math.Min(Tick - getawayStart, getawayProf!.Length - 1));
            return getawayPath.Heading(getawayProf[k]);
        }
        /// <summary>Where an operative stands by the car to board or step out: its stand's car place, on the walk grid.</summary>
        public Pt GetawaySeat => S.Stand(CurStand.Car);
        bool InLane(Pt p) => S.InExitLane(p, CurStand);
        Pt LaneAt(int cm) => S.LanePointAt(cm, CurStand);

        /// <summary>Drive (brief 059 K6): refused while held (drive-held), by an operative not in the car (drive-no-driver), while anyone in it is
        /// mid-act (drive-busy), for no stand (unknown-stand) or the one it is at (drive-here), and once the final fight has started (drive-fight);
        /// brief 096 (car.box): boxed in where it stands (drive-boxed), or the stand it would drive to boxed (stand-boxed).</summary>
        string DriveCar(Op op, Order o)
        {
            if (!R.DriveCarOn || Range) return No("unknown-verb", "drive-car is the getaway car's Drive (rulebook cell.driveCar): unknown = reject");
            var id = o.Target.Length > 0 ? o.Target : o.To;
            var st = S.StandById(id); if (st == null) return No("unknown-stand", $"'{id}' is no stand for the getaway car (site getaway.stands)");
            if (!op.InCar) return No("drive-no-driver", $"the {o.Op} is not in the getaway car: board it first");
            if (FightStart != INF) return No("drive-fight", "the final fight has started: it is fought where it started, and Go is the way out");
            foreach (var x in Ops) if (x.InCar && x.Busy != null) return No("drive-busy", $"the {Sim.Op.Name(x.Id)} in the car is busy until {Sim.Report.Clock(x.BusyEnd)}");
            if (st == CurStand) return No("drive-here", $"the car is at {st.Id} already");
            var hs = CarHolders(); if (hs.Count > 0) return No("drive-held", $"the car is held: {string.Join(", ", hs)}", ids: string.Join(",", hs));
            var bx = GetawayBoxers(); if (bx.Count > 0) return No("drive-boxed", BoxedWords(bx), ids: string.Join(",", bx)); // brief 096 (car.box): boxed in where it stands
            var sb = StandBoxers(st.Id); if (sb.Count > 0) return No("stand-boxed", $"can't get in: {string.Join(", ", sb)} {(sb.Count == 1 ? "is" : "are")} across the way out of {st.Id}", ids: string.Join(",", sb)); // and where it would go
            var path = new Path(S.Drive.CarRoute(CurStand.CarCell, st.CarCell, S.DriveLaneCm) ?? new[] { CurStand.CarCell, st.CarCell });
            var prof = CarProfile(path); int ticks = Math.Max(1, prof.Length - 1);
            getawayPath = path; getawayProf = prof; getawayStart = Tick; GetawayDest = st;
            foreach (var x in Ops)
            {
                if (!x.InCar) continue;
                if (x.Gun.Order.Length > 0) OrderEnd(x, false);
                x.Busy = x == op ? o : new Order { Tick = Tick, Op = Sim.Op.Name(x.Id), Do = "ride", Target = st.Id };
                x.BusyStart = Tick; x.BusyEnd = Tick + ticks; x.Walk = null; x.WalkMm = 0;
            }
            Note("order.drive-car", $"The {Sim.Op.Name(op.Id)} drives the getaway car to {st.Id} ({(ticks + Hz - 1) / Hz} s).");
            CrewCarMoves(st); // brief 059 F1: the posted three go after it
            return "";
        }
        /// <summary>The Drive lands: the car stands at its new stand; whoever is in it sits there.</summary>
        void LandDriveCar(Op op)
        {
            if (GetawayDest == null) return;
            CurStand = GetawayDest; GetawayDest = null; getawayPath = null; getawayProf = null; liveCoverKey = -1;
            foreach (var x in Ops) if (x.InCar) x.Pos = GetawaySeat;
            Note("car.parked", $"The getaway car is at {CurStand.Id}.");
            SawGetaway(Sim.Op.Name(op.Id)); // Motive M059-2.4: a driver at the wheel of his own car nearby learns it
        }
        /// <summary>How many cover points of `of` (car/getaway, car/rivals, …) stand now (a read; never hashed).</summary>
        public int LiveCoverOf(string of) => LiveCover.FindAll(p => p.Of == of).Count;
        /// <summary>The Drive's seconds from the car's stand to a stand (a host read for the price line; -1 for no stand).</summary>
        public int DriveTicks(string standId)
        {
            var st = S.StandById(standId); if (st == null || Range) return -1;
            var path = new Path(S.Drive.CarRoute(CurStand.CarCell, st.CarCell, S.DriveLaneCm) ?? new[] { CurStand.CarCell, st.CarCell });
            return Math.Max(1, CarProfile(path).Length - 1);
        }

        /// <summary>Motive M059-2.4 (memory.acts saw-getaway, facts cell-car and place-time): the turned driver, in his car, sees the getaway car park
        /// within witness range behind him: he knows the cell's car. Whether he gives it up is the Talker test, as for anyone.</summary>
        void SawGetaway(string by)
        {
            if (!R.MemoryActs.Contains("saw-getaway") || Driver.Down || valeDriveStart < 0 || by.Length == 0 || getawayPath != null) return;
            var dp = DriverPos(); if (!dp.HasValue || !G.Within(dp.Value, Car, witnessRange)) return;
            if (Driver.Memory.Exists(m => m.Act == "saw-getaway")) return;
            Remember(Driver, "saw-getaway", by, true);
        }
        /// <summary>Who of the cell sits in the getaway car (the first by id), or "".</summary>
        string InCarName() { foreach (var x in Ops) if (x.InCar) return Sim.Op.Name(x.Id); return ""; }

        /// <summary>The getaway's drive for the checkpoint hash: no bytes until it drives or stands elsewhere.</summary>
        internal void HashDriveCar(Hash.H h)
        {
            if (CurStand == S.Stands[0] && getawayPath == null) return;
            h.S("stand").S(CurStand.Id).S(GetawayDest?.Id ?? "").I(getawayStart).Z(getawayPath != null);
        }
    }
}
