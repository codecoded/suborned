using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 096, the lend (engine 0.42.0, Keel's report-lend.md §10; Motive's lend addendum; Sky 2026-11-05 "build the lend now"): two cell cars, two Go's.
    /// Once the in-car Take lends Popovic's car (Game.Lent) it is ours: its own card and ring (Board, Step out, Go), its own refusals (boxed, held, no driver,
    /// disabled, already left: Game.LentGoQuery), Board full says someone is at the wheel. Its Go is vale.out (the win is fixed); our car's Go then gets the rest
    /// clear, never a withdraw. After vale.out the police close in at Game.SplitPoliceAt: one countdown plate under the clock (HudToolkit.Seal), the roster's
    /// left-behind flags stay up, and End (the plate's button) ends it now. Reads the engine, gives engine orders; never changes it.</summary>
    public sealed partial class HandoverHost
    {
        const int LendInf = int.MaxValue / 8;
        /// <summary>Popovic's car is ours and still standing (lent, not gone).</summary>
        public bool LentCarOurs => Game != null && !Game.Range && Game.Lent && !Game.LentGone;
        bool IsLentCar(Target t) => t != null && LentCarOurs && CarIdOf(t) == LentCarId; // engine 0.53.0 (car.take): Popovic's car lent, or any car taken (HostTakeCar)
        /// <summary>Either cell car: ours, or Popovic's once lent.</summary>
        bool IsCellCar(Target t) => IsGetaway(t) || IsLentCar(t);
        /// <summary>Popovic is out (vale.out): the win is fixed, the mission runs on until the police close in.</summary>
        public bool ValeOut => Game != null && Game.ValeOutTick < LendInf;
        /// <summary>The police countdown is on (after vale.out, the run not over).</summary>
        public bool PoliceOn => ValeOut && !Game.Over && Game.SplitPoliceAt < LendInf;
        public int PoliceLeftS => PoliceOn ? Math.Max(0, (Game.SplitPoliceAt - Game.Tick + Game.Hz - 1) / Game.Hz) : 0;
        public int PoliceSpanS => PoliceOn ? Math.Max(1, (Game.SplitPoliceAt - Game.ValeOutTick + Game.Hz - 1) / Game.Hz) : 0;
        /// <summary>The roster's left-behind flags: while a Go card is open, and all the time once the police countdown runs.</summary>
        public bool LeftFlagsOn => GoCardOpen || PoliceOn;
        /// <summary>Where Popovic's car is drawn now.</summary>
        public Vector3 LentWorld => Game == null ? Vector3.zero : frame.World(LentPt);

        /// <summary>The cars' names: ours stays names.targets.car-getaway; Popovic's is labelled as ours from the lend (book.cars.lentName).</summary>
        string OfficialCarName() => LentCarOurs ? BookWordsFile.Get().LentName : Text?.TargetName("car-official", $"{ValeWord}'s car") ?? $"{ValeWord}'s car";
        string GetawayCarName() => Text?.TargetName("car-getaway", "our car") ?? "our car";

        /// <summary>The agent at the lent car's wheel (standing, aboard), -1 none.</summary>
        public int LentDriver() { var g = Game; if (g == null) return -1; for (int k = 0; k < 4; k++) if (g.Ops[k].InLent && !g.Ops[k].Down && !g.Ops[k].LeftMap) return k; return -1; }

        Intent LentBoardIntent(OpId k)
        {
            var g = Game; var op = g.Ops[(int)k]; int secs = g.R.BoardS + (op.Carrying.HasValue ? g.R.CarryS : 0);
            var i = new Intent { Slot = -1, Label = "Board", Order = new Order { Op = Op.Name(k), Do = "board", Target = LentCarId } };
            i.Time = $"{secs} s"; i.Usd = "$0"; i.Trail = "T 0"; i.Alert = "A 0"; i.Outcome = op.Carrying.HasValue ? "with the " + Op.Name(op.Carrying.Value) : "";
            return i;
        }
        /// <summary>The engine's Board refusals for the lent car, in its order (BoardLent): one seat, so a standing agent at the wheel says who.</summary>
        Answer LentBoardLock(OpId k)
        {
            var g = Game; var op = g.Ops[(int)k];
            Answer No(string code, string why, string ids = "") => new Answer { ok = false, code = code, why = why, m = -1, cm = -1, usd = -1, tick = -1, ids = ids };
            if (!LentCarOurs) return No("car-left", "that car has already gone");
            var b = BoardLock(k); if (!b.ok) return b;
            bool dis = false; Guard("car disabled", () => dis = g.CarDisabled(LentCarId)); if (dis) return No("car-disabled", $"{LentCarName()} is disabled");
            int w = LentDriver(); if (LentCarId == "car:official" && w >= 0) return No("board-lent-full", $"the {Op.Name((OpId)w)} is at the wheel", Op.Name((OpId)w));
            if (LentCarId != "car:official" && LentSeatsFull(k)) return No("car-full", "no room"); // engine 0.53.0 (car.take): a taken car seats its kind's seats
            return new Answer { ok = true, code = "", why = "" };
        }
        /// <summary>The lent car's Go: the engine's own answer (boxed, held, no driver, disabled, already left).</summary>
        public Answer LentGoLock()
        {
            var a = new Answer { ok = false, code = "car-left", why = "that car has already gone", m = -1, cm = -1, usd = -1, tick = -1 };
            if (LentCarOurs) Guard("lent go query", () => a = Game.LentGoQuery());
            a.code ??= ""; a.why ??= ""; return a;
        }
        Intent LentGoIntent(OpId by)
        {
            var i = new Intent { Slot = -1, Label = BookWordsFile.Get().GoLent, Order = new Order { Op = Op.Name(by), Do = "go", Target = LentCarId } };
            i.Time = "now"; i.Usd = "$0"; i.Alert = "A 0"; i.Outcome = Game.Vale == ValeAt.Lent ? "win" : "clear";
            bool seen = LentWatcherNow().Length > 0; i.Trail = seen ? $"T +{Game.R.WitnessTrail}" : "T 0"; // engine 0.43.0: who would see it leave (lent.seen), the Go card's "+1 T if seen"
            if (seen) i.Label += " · " + (Text?.GoSeen ?? "+{trail} T if seen").Replace("{trail}", Game.R.WitnessTrail.ToString());
            return i;
        }
        /// <summary>A Go or Board lock for either car, by the order's target (Locked's go and board).</summary>
        Answer CarGoLock(string target) => LentOrder(target) ? LentGoLock() : GoLock();
        Answer CarBoardLock(OpId by, string target) => LentOrder(target) ? LentBoardLock(by) : BoardLock(by);

        /// <summary>The act knob on a cell car: Go from inside, else Board.</summary>
        Intent CarActIntent(OpId by, Target t)
        {
            var op = Game.Ops[(int)by];
            if (IsLentCar(t)) return op.InLent ? LentGoIntent(by) : LentBoardIntent(by);
            return op.InCar && !op.InLent ? GoIntent(by) : BoardIntent(by);
        }
        /// <summary>The hover chip on the lent car: Go for the agent at the wheel, else Board with its walk.</summary>
        Intent LentChipIntent(Target t)
        {
            Intent got = null;
            Guard("price", () => { got = CarActIntent(Agent, t); if (got.Order.Do == "board") { var aw = ActPlace(Agent, got.Order, t); if (aw != null) got.WalkS = aw.Seconds; } });
            return got;
        }

        /// <summary>The lent car's rows: Board for each selected agent outside a car, then Step out for whoever is aboard, then its Go (first when free). No Drive:
        /// it is never driven round (drive-lent).</summary>
        void LentRows(ActionPanel pn, List<int> order, Target t)
        {
            var g = Game;
            foreach (var k in order)
            {
                if (g.Ops[k].InCar) continue;
                var v = LentBoardIntent((OpId)k); var aw = ActPlace((OpId)k, v.Order, t);
                if (aw != null) { v.Walk = $"walks {aw.Cm / 100} m ({aw.Seconds} s) to {LentCarName()}, then"; v.WalkS = aw.Seconds; }
                Words(v, t); var lk = LentBoardLock((OpId)k);
                pn.Rows.Add(new PanelRow { By = (OpId)k, Slot = -1, I = v, Verb = v.Name, Lock = lk, Reason = lk.ok ? "" : ShortOf(v.Order, lk) ?? lk.why });
            }
            int w = LentDriver(); var by = w >= 0 ? (OpId)w : order.Count > 0 ? (OpId)order[0] : Agent;
            var gv = LentGoIntent(by); Words(gv, t); GoWaitWords(gv, LentCarId); gv.Name = gv.Label; var gl = LentGoLock(); // G1: the climber counted in (HostGoWaits)
            var goRow = new PanelRow { By = by, Slot = -1, I = gv, Verb = gv.Name, Lock = gl, Reason = gl.ok ? "" : ShortOf(gv.Order, gl) ?? gl.why };
            if (gl.ok) pn.Rows.Insert(0, goRow); else pn.Rows.Add(goRow);
            StepOutRows(pn, true);
            foreach (var r in pn.Rows) { r.Available = r.Reason.Length == 0; if (!r.Available) r.Unlock = LockLine(r.I.Order, r.Lock) ?? ""; }
        }

        /// <summary>Sky 2026-11-04 (a tester boarded without Popovic and couldn't get out): Step out on a car's ring for every standing agent aboard that car (ours, or the
        /// lent one), one way for all of them (same order, merged on the ring). The engine's move on a boarded operative (GDD §06 Board, brief 018): board.s to step
        /// out at the car's side, Popovic stays in. The point is the free cell beside the seat, so the agent stops there.</summary>
        void StepOutRows(ActionPanel pn, bool lent)
        {
            var g = Game; if (g == null || g.Range || g.Over) return;
            var seat = lent ? LentPt : g.GetawaySeat; var at = g.Walk.NearestFree(seat) ?? seat;
            string word = Text != null && Text.Actions.TryGetValue(StepOutHow, out var sa) && sa.Label.Length > 0 ? sa.Label : "Get out"; string does = Text != null && Text.Actions.TryGetValue(StepOutHow, out var sd) ? sd.Does ?? "" : ""; // Sky 2026-11-10: "Get out" (Move read as drive the car)
            for (int k = 0; k < 4; k++)
            {
                var op = g.Ops[k]; if (!op.InCar || op.InLent != lent || op.Down || op.LeftMap) continue;
                var v = new Intent { Slot = -1, Label = word, Name = word, Does = does, How = StepOutHow, Lead = "s_moving", Order = new Order { Op = Op.Name((OpId)k), Do = "move", Point = at } };
                v.Time = $"{g.R.BoardS} s"; v.Usd = "$0"; v.Trail = "T 0"; v.Alert = "A 0"; v.Outcome = (lent ? g.Vale == ValeAt.Lent : g.Vale == ValeAt.Car) ? $"{ValeWord} stays in" : "";
                var lk = op.Busy != null ? new Answer { ok = false, code = "agent-busy", why = "busy in the car", m = -1, cm = -1, usd = -1, tick = -1 } : new Answer { ok = true, code = "", why = "" };
                pn.Rows.Add(new PanelRow { By = (OpId)k, Slot = -1, I = v, Verb = v.Name, Lock = lk, Reason = lk.ok ? "" : ShortOf(v.Order, lk) ?? lk.why });
            }
        }

        /// <summary>The lent car's Go given: refused, it says why and pulses the holders; else at once (the others stay on: the mission runs on).</summary>
        void LentGoGive(OpId by, Vector2? screen)
        {
            var o = new Order { Op = Op.Name(LentDriver() >= 0 ? (OpId)LentDriver() : by), Do = "go", Target = LentCarId }; var lk = LentGoLock(); var at = LentWorld;
            if (!lk.ok) { Pop(ShortOf(o, lk) ?? lk.why, screen, screen.HasValue ? null : at, Feedback.Refused, 2f, PopIcon(lk.code ?? ""), LockLine(o, lk) ?? lk.why); if (lk.code is "car-held" or "car-boxed") HolderPulseAt = Time.unscaledTime; PulseBoxers(lk); return; } // brief 098 step 1b: a passing car boxing it in pulses
            Issue(o, screen, at);
        }

        /// <summary>End (after vale.out, in Withdraw's place): the mission ends now; everyone not in a car that has left is captured. The plate's button.</summary>
        public void EndNow()
        {
            if (!PoliceOn || Modal != ModalGate.Modal.None) return;
            Issue(new Order { Op = Op.Name(Agent), Do = "end" }, null, null);
        }

        /// <summary>The police plate's action line: who is still to get clear, else that everyone is.</summary>
        public string PoliceAction()
        {
            var w = BookWordsFile.Get(); var left = LeftBehindNow(); if (left.Count == 0) return w.PoliceClear;
            var names = left.ConvertAll(x => AgentNames[(int)Op.Parse(x.op)]);
            return w.PoliceLeft.Replace("{list}", string.Join(", ", names)).Replace("{n}", left.Count.ToString());
        }
    }
}
