using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 105 round 2 (engine 0.53.0, Keel; Motive round2.md §1, rulebook car.take; Sky 2026-11-10: "if everyone is out, and there's other cars still
    /// around, then they can take those ones e.g. the rivals car"): the host's side of Take car and of the second cell car whichever car it is (Game.LentId).
    ///  · Take car is a ring way on any car the engine would let us take (standing, not shot out, nobody of ours in it): the rival car, a passing car, Popovic's car
    ///    once his driver is out. One row per selected agent; the walk to its door is ActPlace's (CarDoorWalk), then the engine order take-car &lt;car&gt;. Its locks
    ///    mirror the engine's own order (GameTakeCar.TakeCarWhy) so the ring can say "Draw first" or "Enforcer only" before the click; the engine still answers
    ///    the order, and its refusal pops as any other.
    ///  · Once taken, the car is ours: its target resolves as a cell car (IsLentCar by its engine id), its Board, Step out and Go are the lend's rows with its id.
    ///  · The car's health on its ring (RingCarHealth): a bar of car.target.health with a mark at car.target.disabledAt, shown once it has been hit.
    ///  · The fire-car warning: the crucial card for-car while the rivals shoot a car of ours with Popovic aboard and nobody of the cell outside (CarUnderFire).
    /// Reads the engine, gives engine orders; never changes it.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The second cell car's engine id (car:official until a Take car takes another).</summary>
        public string LentCarId => Game?.LentId ?? "car:official";
        bool LentOrder(string target) => target is "car:official" or "official" || Game != null && Game.Lent && target == Game.LentId;
        /// <summary>A car target's engine id: car:getaway, car:official, car:rivals (its own kind, or a car once ours), a passing car by its key; "" when not a car.</summary>
        static string CarIdOf(Target t) => t == null ? "" : t.Kind == "rivals" && t.Key == "car:rivals" ? "car:rivals" : t.Kind == "car" && t.Key.Length > 0 ? "car:" + t.Key : "";
        /// <summary>The rival car's target: the crew's kind until it is ours, then a car (its Board, Step out and Go).</summary>
        Target RivalCarTarget() => LentCarOurs && LentCarId == "car:rivals" ? new Target { Kind = "car", Key = "rivals", Label = LentCarName() } : new Target { Kind = "rivals", Key = "car:rivals", Id = "car:rivals", Label = Name("rivals") };
        /// <summary>The second cell car's name: Popovic's car (book.cars.lentName), or a taken car's (book.cars.takenName, "the {car}, now ours").</summary>
        string LentCarName() => LentCarId == "car:official" ? OfficialCarName() : BookWordsFile.Get().TakenName.Replace("{car}", PlainCarName(LentCarId));
        /// <summary>A car's name as the ring names it, without its article (the rival car → "rival car"), for the {car} lines.</summary>
        string PlainCarName(string id)
        {
            string n = id == "car:rivals" ? Text?.TargetName("rivals-car", "the rival car") ?? "the rival car" : id == "car:official" ? Text?.TargetName("car-official", $"{ValeWord}'s car") ?? $"{ValeWord}'s car" : TrafficNameOf(id) ?? takenName ?? "car";
            return n.StartsWith("the ", StringComparison.Ordinal) ? n.Substring(4) : n;
        }
        string takenName; // the taken car's name as it was when taken (a passing car's view goes with it)
        /// <summary>Where the second cell car stands (the engine's CarAt), else where Popovic's car is.</summary>
        Pt LentPt => Game.CarAt(LentCarId) ?? Game.CarPos();

        /// <summary>The car.take order is in the rulebook for this run (car.lend.split 1, the Handover, two cars).</summary>
        bool TakeOn => Game != null && !Game.Range && Game.R.LendSplit && !Game.Start.OneCar;

        /// <summary>A car Take car is offered on: standing now, not ours, not the getaway, not shot out; Popovic's car only once his driver is out of it.</summary>
        bool TakeableCar(string id)
        {
            var g = Game; if (!TakeOn || g.Over || id.Length == 0 || id == "car:getaway") return false;
            if (g.Lent && !g.LentGone && id == g.LentId) return false;
            if (id == "car:official" && !g.Driver.Down) return false; // his driver at the wheel: the in-car Take lends it (car-driver), never a Take car way
            bool standing = false; Guard("standing cars", () => standing = g.StandingCars().Exists(c => c.Id == id));
            return standing || g.CarAt(id).HasValue && id != "car:official";
        }

        /// <summary>The engine's Take car refusals in its own order (GameTakeCar.TakeCarWhy), read before the click; distance is a walk, never a lock.</summary>
        Answer TakeCarLock(OpId k, string id)
        {
            var g = Game; var op = g.Ops[(int)k];
            Answer No(string code, string why, string ids = "") => new Answer { ok = false, code = code, why = why, m = -1, cm = -1, usd = -1, tick = -1, ids = ids };
            if (op.Down) return No("agent-down", "down");
            if (op.Inside) return No("enforcer-inside", "inside the Office");
            if (op.InCar) return No("in-car", "in a car: a move steps out first");
            if (op.Running) return No("board-running", "running: stop first");
            if (k == OpId.Tech && g.LoopOn && !g.R.LoopRuns) return No("board-tethered", "the Tech is tethered to the cabinet while the loop holds");
            if (op.CarryBody.Length > 0) return No("already-carrying", "carrying a body");
            if (g.Lent && !g.LentGone && !g.CarDisabled(g.LentId)) return No(g.LentId == id ? "car-ours" : "car-taken", g.LentId == id ? "that car is ours: Board it" : "one taken car at a time");
            if (g.Lent && g.LentGone) return No("car-taken", "one taken car a mission");
            bool standing = false; Guard("standing cars", () => standing = g.StandingCars().Exists(c => c.Id == id));
            if (!standing) return No("car-moving", "that car is driving");
            if (g.CarDisabled(id) || g.CarBurning(id) || g.CarOf(id) is { Blown: true }) return No("car-disabled", "that car is shot out");
            if (id == "car:official") return g.Driver.Down ? new Answer { ok = true, code = "", why = "" } : No("car-driver", "his driver is at the wheel");
            int seats = id == "car:rivals" ? g.R.TakeSeat("rivals") : g.TrafficCarById(id) is TrafficCar tc0 ? g.R.TakeSeat(tc0.Model) : -1;
            if (seats < 0) return No("car-unknown", "that car can't be taken");
            int need = 1 + (op.Carrying.HasValue ? 1 : 0) + (k == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer ? 1 : 0);
            if (need > seats) return No("car-full", "no room");
            if (id == "car:rivals")
            {
                for (int i = 0; i < g.Crew.Count; i++) { var m = g.Crew[i]; if (i != 4 && !m.Gun.Down && m.Mode == CrewMode.Aboard) return No("car-crewed", $"{m.Id} is still in the rival car"); }
                bool wheel = g.Crew.Count > 4 && !g.Crew[4].Gun.Down && g.Crew[4].Mode == CrewMode.Aboard;
                if (wheel && k != OpId.Enforcer) return No("car-wheelman", "the wheelman is at the wheel: the Enforcer takes him out", g.Crew[4].Id);
            }
            else if (g.TrafficCarById(id) is TrafficCar tc && tc.Out == null && tc.Mode != TrafficMode.Abandoned && !(op.Gun.Drawn && op.Gun.HolsterAt < 0))
                return No("draw-first", "the driver is at the wheel: draw first");
            return new Answer { ok = true, code = "", why = "" };
        }

        /// <summary>Board on a taken car past its kind's seats (the engine's SeatsFull: Popovic aboard, everyone in it, and this agent with whoever he carries).</summary>
        bool LentSeatsFull(OpId k)
        {
            var g = Game; var id = g.LentId; var op = g.Ops[(int)k];
            int seats = id == "car:rivals" ? g.R.TakeSeat("rivals") : g.TrafficCarById(id) is TrafficCar tc ? g.R.TakeSeat(tc.Model) : -1; if (seats < 0) return false;
            int have = g.Vale == ValeAt.Lent ? 1 : 0; foreach (var o in g.Ops) if (o.InLent && !o.LeftMap) have++;
            return have + 1 + (op.Carrying.HasValue ? 1 : 0) + (k == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer ? 1 : 0) > seats;
        }

        Intent TakeCarIntent(OpId k, string id)
        {
            var g = Game; var op = g.Ops[(int)k];
            bool carrying = op.Carrying.HasValue || k == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer;
            bool wheel = id == "car:rivals" && k == OpId.Enforcer && g.Crew.Count > 4 && !g.Crew[4].Gun.Down && g.Crew[4].Mode == CrewMode.Aboard;
            var (label, does) = Text != null ? Text.Action("take-car", null, Op.Name(k)) : ("Take car", "");
            if (label == "take-car" || label.Length == 0) label = "Take car";
            var i = new Intent { Slot = -1, Label = label, Name = label, Does = does, Lead = "car", Order = new Order { Op = Op.Name(k), Do = "take-car", Target = id } };
            i.Time = $"{g.R.BoardS + (carrying ? g.R.CarryS : 0) + (wheel ? g.R.KoS : 0)} s"; i.Usd = "$0"; i.Trail = "T 0"; i.Alert = "A 0";
            i.Outcome = k == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer ? $"with {ValeWord}" : op.Carrying.HasValue ? "with the " + Op.Name(op.Carrying.Value) : "";
            return i;
        }

        /// <summary>BuildPanel's Take car rows on a car we may take: one per selected agent, the walk to its door priced, the lock in the engine's words.</summary>
        void TakeCarRows(ActionPanel pn, List<int> order, Target t)
        {
            var id = CarIdOf(t); if (!TakeableCar(id)) return;
            foreach (var k in order)
            {
                var v = TakeCarIntent((OpId)k, id); var aw = CarDoorWalk((OpId)k, id);
                if (aw != null) { v.Walk = $"walks {aw.Cm / 100} m ({aw.Seconds} s) to {t.Label}, then"; v.WalkS = aw.Seconds; }
                var lk = Game.Ops[k].Busy != null && !HoldOn ? new Answer { ok = false, code = "agent-busy", why = "busy", m = -1, cm = -1, usd = -1, tick = -1 } : TakeCarLock((OpId)k, id);
                var r = new PanelRow { By = (OpId)k, Slot = -1, I = v, Verb = v.Name, Lock = lk, Reason = lk.ok ? "" : ShortOf(v.Order, lk) ?? lk.why, Available = lk.ok };
                if (!r.Available) r.Unlock = LockLine(v.Order, lk) ?? "";
                if (r.Available) pn.Rows.Insert(0, r); else pn.Rows.Add(r);
            }
        }

        /// <summary>The ring's Take car wedge when every agent's is locked and the ring has room: shut, its short lock as its word ("Draw first", "Enforcer only").</summary>
        void TakeCarShutSeg(RingState rs, ContextPlan plan)
        {
            if (rs.Segs.Count >= RingMaxWays + 1 || rs.Segs.Exists(x => x.Row?.I?.Order?.Do == "take-car")) return;
            var rows = plan.Pn.Rows.FindAll(r => r.I?.Order?.Do == "take-car" && !r.Available); if (rows.Count == 0) return;
            var rep = rows.Find(r => r.By == Agent) ?? rows[0]; int at = Math.Max(0, rs.Segs.FindIndex(x => x.More)); if (rs.Segs.Count == 0 || !rs.Segs.Exists(x => x.More)) at = rs.Segs.Count;
            var sg = new RingSeg { Way = -1, Row = rep, Word = RingWord(rep.I), Icon = "car", Shut = true, ShutText = ShortOf(rep.I.Order, rep.Lock) ?? rep.Reason, ShutIcon = "" };
            foreach (var x in rows) if (!sg.By.Contains(x.By)) sg.By.Add(x.By);
            rs.Segs.Insert(at, sg);
        }

        /// <summary>The walk to a car's door (Take car, or Board on the second cell car): round the standing cars to beside its body; null when already in reach.</summary>
        ActWalk CarDoorWalk(OpId by, string id)
        {
            var g = Game; var op = g.Ops[(int)by]; var at = g.CarAt(id); if (!at.HasValue) return null;
            int d = -1; Guard("car body", () => d = g.CarBodyCm(id, op.Pos)); if (d >= 0 && d <= g.R.CarReachCm) return null;
            var r = MoveRoute(op.Pos, at.Value); if (r == null || r.Length == 0) return null;
            return Walk("", r[^1], r);
        }

        // ---------------------------------------------------------------- the car under fire, its health

        /// <summary>The car of ours Popovic sits in, "" when none: ours, or the second cell car (lent or taken).</summary>
        string ValeCarId() { var g = Game; if (g == null) return ""; return g.Vale == ValeAt.Car && !g.GetawayGone ? "car:getaway" : g.Vale == ValeAt.Lent && !g.LentGone ? g.LentId : ""; }
        /// <summary>The rivals' fire on a car of ours is on (fight.unguarded a): Popovic aboard, the car still driving, nobody of the cell outside, the fight not over.</summary>
        bool CarUnderFire()
        {
            var g = Game; var id = ValeCarId(); if (id.Length == 0 || g.Over || g.CarDisabled(id)) return false;
            foreach (var o in g.Ops) if (!o.Down && !o.InCar && !o.Inside && !o.LeftMap) return false;
            return true;
        }
        /// <summary>The car the open ring is on: its health left and the car's full health, and where it stops; null before its first hit (or not a car).</summary>
        public (int left, int max, int stops)? RingCarHealth()
        {
            var g = Game; if (g == null || g.Range || Ring?.Plan?.T is not Target t) return null;
            return RingHealthOf(CarIdOf(t));
        }
        (int left, int max, int stops)? RingHealthOf(string id)
        {
            var g = Game; if (g == null || id.Length == 0) return null;
            var c = g.CarOf(id); if (c == null) return null;
            int hs = Math.Max(1, g.HealthScale), max = g.R.HealthCar, left = Mathf.Clamp(c.Gun.Health / hs, 0, max);
            return left >= max && !g.CarDisabled(id) ? null : (left, max, g.R.CarDisabledAt);
        }

        /// <summary>A car's view key (car/getaway, car/rivals, car/&lt;slot&gt;-&lt;trip&gt;) by its engine id.</summary>
        static string ViewKeyOfCar(string id) => id.StartsWith("car:", StringComparison.Ordinal) ? "car/" + id.Substring(4) : "car/getaway";

        // ---------------------------------------------------------------- the words

        /// <summary>The end card's line for a win by the second car's Go: Popovic's own car (book.cars.endWinLine), or a taken one (takenWinLine, {car}).</summary>
        public string WinLine() { var bw = BookWordsFile.Get(); return LentCarId == "car:official" ? bw.EndWinLine : bw.TakenWinLine.Replace("{car}", PlainCarName(LentCarId)); }
        /// <summary>The taken car's name for the {car} lines (the debrief's takenSeen).</summary>
        public string TakenCarWord() => PlainCarName(LentCarId);
        /// <summary>Once a tick: the taken car's name kept while its view stands (a passing car's view goes when it leaves).</summary>
        void TakenNameTrack() { var g = Game; if (g != null && g.Lent && !g.LentGone && g.LentId != "car:official") takenName = TrafficNameOf(g.LentId) ?? takenName; else if (g == null || !g.Lent) takenName = null; }
    }
}
