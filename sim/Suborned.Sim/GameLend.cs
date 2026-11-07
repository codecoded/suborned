// The lend (engine 0.42.0, brief 096; Motive's lend addendum briefs/096/motive/lend.html, lend.json; Sky 2026-11-05 "build the lend now",
// decided 2026-10-26 in brief 071, Option A; Sky 082.1-082.4; rulebook car.lend, read only with car.lend.split 1). Two cell cars, two Go's.
//  · L1: the in-car Take (a turned driver's car standing where he stopped: the service bay, or his kerbside stop) ends with the car lent: the
//    Enforcer at the wheel, the target in the back (ValeAt.Lent); the driver steps out and walks off (to where the Scout met him, then off the map).
//  · L3: it seats one standing operative; another boards only while none standing is aboard (board-lent-full), to drive a downed Enforcer out.
//  · L4, L6: its hold is a hostile within fight.hold of it or on its lane (the first fight-block-m of its way out: down the drive, on along Apron
//    Road; at the drive its polygon, on the road the nearest getaway stand's exit half-width); Go is refused boxed in (its way out), held, gated
//    (the loud fight is at it: its blocker, picked for its lane, holds it while up) or with no standing operative aboard. It is never Driven.
//  · L5, L7: its Go is the win, fixed (vale.out); the mission runs on; the posted crew springs that tick.
//  · L8: split-police-s after vale.out (or the seal, or the close) the mission ends: everyone not in a car that has left is captured, standing
//    or down; sooner when nobody is left on the map outside a car that has left, or at End (Withdraw), or when the cell's car leaves. The seal,
//    the close and the cell down no longer fail after vale.out; the cell's car wrecked while the target is lent is no failure.
//  · L9: the cell's car still standing at the end is lost (+witness-trail, hunted: debrief car-seen 'left'); the lent car seen leaving is
//    +witness-trail and a lead, not hunted. L10 (the drive-round block point) is GameRivalBlock's BlockPoint. L11: RivalReachesLent.
// Hashed in HashLend, no bytes until the car is lent: with car.lend.split 0 every run hashes as 0.41.0 (with F1-F3, V1-V2).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        bool LendOn => R.LendSplit && !Range && !Start.OneCar; // Start.OneCar: test only, the one-car twin
        /// <summary>The official car is a cell car (lent at the in-car Take's end). Host read: its card, ring and Go.</summary>
        public bool Lent { get; private set; }
        /// <summary>The tick the car was lent (-1 never), the tick it left with the target (vale.out; INF not yet), and who saw it leave.</summary>
        public int LentTick { get; private set; } = -1;
        public int ValeOutTick { get; private set; } = INF;
        public string LentSeenBy { get; private set; } = "";
        /// <summary>Host read: the tick the police close in after vale.out (the countdown), INF before.</summary>
        public int SplitPoliceAt => ValeOutTick == INF ? INF : Math.Min(splitEnd, Math.Min(Lockdown, T(overS)));
        /// <summary>The lent car has left; the cell's car has left.</summary>
        public bool LentGone => lentLeft;
        public bool GetawayGone => CarLeftTick != INF;
        bool lentLeft; int splitEnd = INF; Pt[] lentLane = Array.Empty<Pt>(); long lentHalf; Stand? lentStand;
        Path? driverOff; int driverOffStart = -1;

        static bool LentTarget(string t) => t == "car:official" || t == "official";
        /// <summary>Where an operative sits in the lent car, and where he steps out (the road side, off its ground).</summary>
        Pt LentSeat => Walk.NearestFree(CarPos()) ?? CarPos();
        Pt LentSideOut => BesideCar(CarPos(), -1);
        bool FightAtLent => Lent && !lentLeft && FightBranch == "loud" && Vale == ValeAt.Lent;
        /// <summary>The stand whose police stands the fight uses: the lent car's while the loud fight is at it, else the cell's car's.</summary>
        Stand FightStandNow => FightAtLent && lentStand != null ? lentStand : CurStand;
        bool LentHasDriver => Array.Exists(Ops, o => o.InLent && !o.LeftMap && !o.Down);
        Op? LentStanding() { foreach (var o in Ops) if (o.InLent && !o.LeftMap && !o.Down) return o; return null; }
        bool AtLent(Pt p) { if (!Lent || lentLeft) return false; int d = CarBodyCm("car:official", p); return d >= 0 && d <= R.CarReachCm; }

        // ---------------------------------------------------------------- the lend (L1)

        /// <summary>The in-car Take lands with the lend on (GameOrders "take"): the Enforcer at the wheel, the target in the back, the driver out.</summary>
        void Lend(Op e)
        {
            Lent = true; LentTick = Tick; Vale = ValeAt.Lent;
            e.InCar = true; e.InLent = true; e.Pos = LentSeat; e.Walk = null; e.WalkMm = 0; e.Running = false; e.Gun.Order = ""; e.Gun.Force = false; e.Gun.Target = "";
            var g = S.GroundOf(R); var way = OfficialWay();
            lentLane = way.Length >= 2 ? new Path(way).Cut((long)g.BlockCm * 10).Points : way;
            long bd = long.MaxValue; foreach (var st in S.Stands) { long d = G.D2(st.CarAt, CarPos()); if (d < bd) { bd = d; lentStand = st; } }
            lentHalf = lentStand?.ExitHalfCm ?? R.BoxHalfCm;
            if (!Driver.Down)
            {
                var from = Walk.NearestFree(BesideCar(CarPos(), -1)) ?? CarPos(); var to = Walk.NearestFree(S.Stand(S.CarDriver)) ?? S.Stand(S.CarDriver);
                driverOff = new Path(Walk.Route(from, to) ?? new[] { from, to }); driverOffStart = Tick;
            }
            Chain.Add("vale-taken-outside"); Chain.Add("vale-lent");
            Note("take.done", $"{Cap(ValeName)} is in the back of his own car with the Enforcer at the wheel. Go when the drive is clear.");
            Note("lend.lent", $"The driver gets out and walks off. {Cap(ValeName)}'s car is ours now: the Enforcer drives it out down the drive.");
        }
        /// <summary>The driver's walk off (DriverPos while lent): from his car's road side to where the Scout met him, then off the map.</summary>
        Pt? LentDriverPos()
        {
            if (driverOff == null) return null;
            long mm = (long)(Tick - driverOffStart) * peopleWalk; return mm >= driverOff.Length ? (Pt?)null : driverOff.At(mm);
        }
        /// <summary>The lent car disabled (CarDisable): it stays; the Enforcer steps out with the target (the one-car way from here).</summary>
        void LentDisabled()
        {
            foreach (var o in Ops) if (o.InLent && !o.LeftMap) { o.InCar = false; o.InLent = false; o.Pos = Walk.NearestFree(LentSideOut) ?? LentSideOut; }
            if (Vale == ValeAt.Lent) Vale = Enforcer.Down ? ValeAt.Pickup : ValeAt.WithEnforcer;
            Note("car.out", $"{Cap(ValeName)}'s car is disabled: the Enforcer gets out with him.");
        }

        // ---------------------------------------------------------------- Board, the hold, Go (L3, L4, L6)

        string BoardLent(Op op, Order o)
        {
            if (!Lent || lentLeft) return No("car-left", $"{Cap(ValeName)}'s car is not ours to board");
            if (op.Running) return No("board-running", "a running operative doesn't board: stop first");
            if (op.Id == OpId.Tech && Tethered) return No("board-tethered", "the Tech is tethered to the cabinet while the loop holds");
            if (op.CarryBody.Length > 0) return No("already-carrying", "carrying a body: the car seats the cell and the target");
            if (CarDisabled("car:official")) return No("car-disabled", $"{Cap(ValeName)}'s car is disabled");
            if (!AtLent(op.Pos)) return No("stand-at-car", $"stand at {ValeName}'s car", cm: R.CarReachCm);
            if (LentStanding() is Op x) return No("board-lent-full", $"Only one of us drives {ValeName}'s car: the {Sim.Op.Name(x.Id)} is at the wheel.");
            op.Gun.Order = ""; op.Gun.Force = false; op.Gun.Target = "";
            Engage(op, o, R.BoardS + (op.Carrying.HasValue ? R.CarryS : 0));
            return "";
        }
        void LandBoardLent(Op op)
        {
            if (op.Down) return;
            if (!AtLent(op.Pos) || LentStanding() != null) { Note("board.missed", $"The {Sim.Op.Name(op.Id)} can't get into {ValeName}'s car."); return; }
            op.InCar = true; op.InLent = true; op.Pos = LentSeat; op.Walk = null; op.WalkMm = 0; op.Running = false; op.Gun.Order = ""; op.Gun.Force = false; op.Gun.Target = "";
            string with = "";
            if (op.Carrying.HasValue) { var c = OpOf(op.Carrying.Value); op.Carrying = null; c.CarriedBy = null; c.InCar = true; c.InLent = true; c.Pos = op.Pos; with = " with the " + Sim.Op.Name(c.Id); }
            Note("board.in", $"The {Sim.Op.Name(op.Id)} is in {ValeName}'s car{with}.");
        }

        /// <summary>Host read (and LEND-12): a hostile standing at p would hold the lent car (its hold or its lane).</summary>
        public bool LentHoldsAt(Pt p) => Lent && !lentLeft && HoldsLent(p);
        bool HoldsLent(Pt p) => G.Within(p, CarPos(), R.HoldCm) || InLentLane(p);
        /// <summary>On the lent car's lane: within its half-width of the lane's line, on a carriageway or on a drive.</summary>
        bool InLentLane(Pt p)
        {
            bool near = false; for (int i = 1; i < lentLane.Length && !near; i++) near = CarGeo.NearSeg(p, lentLane[i - 1], lentLane[i], lentHalf);
            if (!near) return false;
            if (S.Drive.SurfaceAt(p) == 'r') return true;
            foreach (var d in S.Drives) if (CarGeo.Inside(p, d.Poly)) return true;
            return false;
        }
        Pt LentLaneAt(int cm) => lentLane.Length >= 2 ? Walk.NearestFree(new Path(lentLane).At((long)cm * 10)) ?? lentLane[0] : CarPos();

        /// <summary>The lent car's holders now, in id order (L6): the HUD names them, its Go's refusal lists them. Read-only.</summary>
        public List<string> LentHolders()
        {
            var l = new List<string>(); if (!Lent || lentLeft) return l;
            foreach (var h in Hostiles)
            {
                if (!Up(h) || (CrewOf(h.Id) is CrewMember rm && !rm.Fight)) continue;
                if ((h.Present || FightBranch == "safe") && HoldsLent(h.Pos(Tick))) l.Add(h.Id);
            }
            foreach (var id in CombatantIds())
            {
                if (IsFightHostile(id) || l.Contains(id) || !Combatant(id)) continue;
                var at = PosOf(id); if (at.HasValue && HoldsLent(at.Value)) l.Add(id);
            }
            if (FightAtLent && R.FightAlways && FightOverTick == INF && !blockerOut) // the gate: the loud fight is at this car
            {
                if (Blockers.Count > 0) { foreach (var id in Blockers) if (HostileOf(id) is Hostile b && Up(b) && !l.Contains(id)) l.Add(id); }
                else foreach (var h in Hostiles) if (Up(h) && h.Present && !l.Contains(h.Id)) l.Add(h.Id);
            }
            l.Sort(StringComparer.Ordinal);
            return l;
        }
        /// <summary>The lent car's Go refusal now ("" when it would go). A host read for its card, and Go's own test.</summary>
        string LentRefusal()
        {
            if (Over) return No("mission-over", "the mission is over");
            if (!Lent || lentLeft) return No("car-left", $"{Cap(ValeName)}'s car is not ours to drive");
            if (CarDisabled("car:official")) return No("car-disabled", $"{Cap(ValeName)}'s car is disabled");
            if (!LentHasDriver && !GoWouldWait("car:official")) return No("car-no-driver", "nobody aboard can drive: only the downed are in the car");
            var bx = OfficialBoxers(); var hs = LentHolders();
            if (bx.Count > 0) return No("car-boxed", BoxedWords(bx, hs), ids: BoxedIds(bx, hs));
            if (hs.Count > 0) return No("car-held", $"the car is held: {string.Join(", ", hs)}", ids: string.Join(",", hs));
            return "";
        }
        /// <summary>The lent car's Go card before the click (Latch): would its Go be taken now, and if not, why. Read-only.</summary>
        public Answer LentGoQuery()
        {
            var saved = refusal; refusal = default; string why;
            try { why = LentRefusal(); }
            catch (Exception e) when (!(e is OutOfMemoryException)) { why = No("engine-error", "engine error: " + e.GetType().Name + ": " + e.Message); }
            var a = AnswerOf(why); refusal = saved; return a;
        }
        string GoLent(Order go)
        {
            var why = LentRefusal(); if (why.Length > 0) { goRefusedAt = Tick; goRefused = refusal.code ?? ""; return why; }
            if (GoWaitFor(go) is string w) return w; // engine 0.43.0 (G1)
            ShoveOffWay(OfficialWay(), "car:official"); lentLeft = true; // brief 098 step 1b: a wreck on its way out is pushed aside as it goes
            var by = CarWatcherAt(CarPos());
            if (by.Length > 0) { LentSeenBy = by; if (incidents.Add("lent-seen")) AddTrail(R.WitnessTrail, "lent-seen:" + by); Note("lent.seen", $"The {by} sees {ValeName}'s car leave: a lead, not our car."); }
            var names = new List<string>(); foreach (var o in Ops) if (o.InLent) { o.LeftMap = true; names.Add(Sim.Op.Name(o.Id)); }
            if (Vale == ValeAt.Lent)
            {
                Vale = ValeAt.Out; ValeOutTick = Tick; splitEnd = Tick + T(R.SplitPoliceS);
                Chain.Add("vale-out"); Note("vale.out", $"The {string.Join(" and the ", names)} drives {ValeName} out in his own car. He is out: the job is done.");
                Note("split.police", $"The police close in at {Sim.Report.Clock(SplitPoliceAt)}: anyone not in a car that has left by then is taken.");
            }
            if (FightBranch == "safe" && FightSprung == INF) { Note("lend.spring", $"The rival crew sees {ValeName}'s car go and turns on ours."); Spring(false); }
            if (NobodyLeft()) SplitEnd("all-out");
            return "";
        }

        // ---------------------------------------------------------------- the spring, the reach (L7, L11)

        /// <summary>While the target is in the lent car: an operative standing within fight.safe.ambush of the cell's car springs the crew.</summary>
        bool LendSpringNear(Pt car)
        {
            if (!Lent || Vale != ValeAt.Lent) return false;
            foreach (var o in Ops) if (!o.Down && !o.Inside && !o.InCar && !o.LeftMap && G.Within(o.Pos, car, R.SafeAmbushCm)) return true;
            return false;
        }
        /// <summary>L11, in the final fight: a rival at the lent car's body with the target in it, no drawn operative standing within cell.reach of it.</summary>
        bool RivalReachesLent()
        {
            if (lentLeft) return false;
            bool rival = false;
            foreach (var h in Hostiles) if (IsRival(h.Id) && h.Present && Up(h) && CarBodyCm("car:official", h.Pos(Tick)) is int d && d >= 0 && d <= R.CarReachCm) rival = true;
            if (!rival) return false;
            foreach (var op in Ops) if (Guards(op) && G.Within(op.Pos, CarPos(), actReach)) return false;
            return true;
        }

        // ---------------------------------------------------------------- after vale.out (L8, L9)

        /// <summary>The cell's car leaves while the target is lent (LEND-17: the mission goes on) or after vale.out (it ends: no car is left).</summary>
        void GetawayLeavesSplit()
        {
            SeenLeaving(); CarLeftTick = Tick; ShoveOffGetaway(); // brief 098 step 1b: a wreck on the way out is pushed aside as it goes
            var names = new List<string>(); foreach (var o in Ops) if (o.InCar && !o.InLent) { o.LeftMap = true; names.Add(Sim.Op.Name(o.Id)); }
            Chain.Add("getaway-car-leaves"); Note("car.leaves-split", $"The getaway car leaves with the {string.Join(", the ", names)}.");
            if (ValeOutTick != INF || NobodyLeft()) SplitEnd("car-left");
        }
        bool NobodyLeft() { foreach (var o in Ops) if (!o.LeftMap) return false; return true; }
        /// <summary>Once a tick while lent (GetawayAfterFire): after vale.out the police clock, the seal and the close end the mission; so does nobody left.</summary>
        void LendStep()
        {
            if (!Lent || Over || ValeOutTick == INF) return;
            if (NobodyLeft()) { SplitEnd("all-out"); return; }
            if (Tick >= splitEnd) SplitEnd("police"); else if (Tick >= Lockdown) SplitEnd("sealed"); else if (Tick >= T(overS)) SplitEnd("closed");
        }
        /// <summary>Fail reads it: after vale.out the seal and the close end the mission as a win (captures), the cell down and the cell's car
        /// wrecked fail nothing; while the target is lent the cell's car wrecked is no failure.</summary>
        bool LendSwallows(string code)
        {
            if (!Lent) return false;
            if (ValeOutTick != INF)
            {
                if (code == "cell-down" || code == "getaway-lost") { Note("lend.goes-on", code == "cell-down" ? "The cell is down, but he is out: the police will take whoever is left." : "Our car is wrecked, but he is out."); return true; }
                SplitEnd(code); return true;
            }
            if (code == "getaway-lost" && !lentLeft && Vale == ValeAt.Lent) { Note("lend.goes-on", $"Our car is wrecked, but {ValeName}'s car can still go."); return true; }
            return false;
        }
        /// <summary>End (the menu's Withdraw once the target is out): the mission ends now; before vale.out End is unknown (Withdraw withdraws).</summary>
        string EndOrder()
        {
            if (ValeOutTick == INF) return No("not-out", $"End is for after {ValeName} is out: Withdraw ends the attempt");
            SplitEnd("end"); return "";
        }
        /// <summary>The mission ends after vale.out: a success; everyone not in a car that has left is captured; the cell's car left standing is lost.</summary>
        void SplitEnd(string why)
        {
            if (Over) return;
            RecordsAtEnd();
            foreach (var o in Ops)
            {
                if (o.LeftMap) continue;
                string name = Sim.Op.Name(o.Id), w = o.Down ? "downed" : why == "sealed" ? "sealed" : "police";
                LeftBehindAtGo.Add((name, "captured", w)); Captured.Add(name); Chain.Add(name + "-captured"); Note("captured", $"The {name} is left behind and captured ({w}).");
            }
            if (!GetawayGone)
            {
                if (incidents.Add("car-lost")) AddTrail(R.WitnessTrail, "car-lost");
                if (CarSeenBy.Length == 0) CarSeenBy = "left";
                Chain.Add("getaway-car-lost"); Note("car.lost", "Our car is left behind: the police will trace it.");
            }
            Result = Outcome.Success; Chain.Add("car-leaves-with-vale");
            Note("success", $"{Cap(ValeName)} is out. The mission ends ({why}).");
            EndOpenFight(); fightClosedLate = true;
        }
        /// <summary>LeftBehind() while lent: after vale.out everyone not in a car that has left is captured; before, they stay on (the mission goes on).</summary>
        List<(string op, string fate, string why)> LeftBehindSplit()
        {
            var l = new List<(string, string, string)>();
            foreach (var o in Ops)
            {
                if (o.LeftMap || o.InCar || ClimbingInto(o, "car") || ClimbingInto(o, "car:official")) continue; // engine 0.43.0 (G1)
                l.Add((Sim.Op.Name(o.Id), ValeOutTick != INF ? "captured" : "stays", o.Down ? "downed" : ValeOutTick != INF ? "police" : "lent"));
            }
            return l;
        }
        /// <summary>The close with the target still in the lent car (CloseStep): why it never went.</summary>
        (string why, Blamed? who, string words) LentCloseWhy()
        {
            if (!LentHasDriver) return ("no-driver", Locate("car:official", "closed-in"), "nobody left who can drive");
            var bx = OfficialBoxers(); var hs = LentHolders();
            if (bx.Count > 0) return ("boxed", Locate(LocateCar(bx[0]), "closed-in"), $"{ValeName}'s car boxed in by {string.Join(", ", bx)}");
            if (hs.Count > 0) return ("car-held", Locate(hs[0], "closed-in"), $"{ValeName}'s car held");
            return ("no-go", Locate("car:official", "closed-in"), $"{ValeName}'s car never told to go");
        }

        /// <summary>L10: a stand whose car stands on a drive blocks across the drive's mouth (with the lend on): on the carriageway, half a car's length
        /// out from the middle of the drive's kerb line, square to it (away from the stand's car), so the rival car boxes both the cell's car on the
        /// drive and the official car at its kerbside stop. Null for any other stand, or with the lend off (0.41.0's point).</summary>
        (Pt at, Pt heading)? DriveMouthBlock(Stand st)
        {
            if (!LendOn) return null;
            foreach (var d in S.Drives)
            {
                if (!CarGeo.Inside(st.CarAt, d.Poly)) continue;
                Pt a = d.Kerb[0], b = d.Kerb[1], mid = new Pt((a.X + b.X) / 2, (a.Y + b.Y) / 2);
                var n = new Pt(-(b.Y - a.Y), b.X - a.X); // square to the kerb line
                if ((long)n.X * (mid.X - st.CarAt.X) + (long)n.Y * (mid.Y - st.CarAt.Y) < 0) n = new Pt(-n.X, -n.Y); // away from the stand's car
                // Motive's property (lend.json L10): it boxes our car on the drive and the official car at its kerbside stop, stands >= rivals.block.standOff
                // from our car and outside the blast (car.target.blast.m + 0.5 m) of the kerbside car's centre: from the kerb line's middle, 25 cm steps
                // along it either way in turn, the first point that holds; else the middle
                var gr = S.GroundOf(R); long hl = R.CarLenCm / 2, hw = R.CarWCm / 2; var way = WayOut(st.Id); long klen = Math.Max(1, G.Dist(a, b));
                gr.Kerb.TryGetValue(st.Id, out var ks); var kway = ks == null ? null : new[] { ks.At, CarGeo.Ahead(ks.At, ks.Heading, gr.BlockCm) };
                for (long t = 0; t <= klen / 2; t += 25)
                    foreach (var sgn in t == 0 ? new[] { 1 } : new[] { 1, -1 })
                    {
                        var c = CarGeo.Ahead(new Pt(mid.X + (int)((b.X - a.X) * t * sgn / klen), mid.Y + (int)((b.Y - a.Y) * t * sgn / klen)), n, hl); var rect = CarGeo.Rect(c, n, hl, hw);
                        if (!CarGeo.OnWay(rect, way, gr.Half) || G.D2(c, st.CarAt) < (long)Cr.BlockCm * Cr.BlockCm) continue;
                        if (ks != null && (!CarGeo.OnWay(rect, kway!, gr.Half) || G.D2(c, ks.At) < (long)(R.CarBlastCm + 50) * (R.CarBlastCm + 50))) continue;
                        return (c, n);
                    }
                return (CarGeo.Ahead(mid, n, hl), n);
            }
            return null;
        }

        /// <summary>L10's companion (engine 0.42.0, lend on): with the drive taken (our car at a stand on it, or driving there), the turned driver's way
        /// out from the pickup runs only to his kerbside stop for that stand: the pickup way cut at its point nearest the stop (25 cm steps), the stop
        /// itself last. Else the pickup way, as 0.40.0.</summary>
        Pt[] PickupWayToKerb(Site.Ground g)
        {
            var st = getawayPath != null ? GetawayDest : CurStand;
            if (st == null || !g.Kerb.TryGetValue(st.Id, out var k) || g.PickupWay.Length < 2) return g.PickupWay; // a stand with a kerbside stop takes his drive
            var p = new Path(g.PickupWay); long best = 0, bd = long.MaxValue;
            for (long mm = 0; mm <= p.Length; mm += 250) { long d = G.D2(p.At(mm), k.At); if (d < bd) { bd = d; best = mm; } }
            var l = new List<Pt>(p.Cut(Math.Max(1, best)).Points); if (l[l.Count - 1] != k.At) l.Add(k.At);
            return l.ToArray();
        }

        internal void HashLend(Hash.H h)
        {
            if (!Lent) return;
            h.S("lend").I(LentTick).I(ValeOutTick).Z(lentLeft).I(splitEnd).S(LentSeenBy).I(driverOffStart).I(lentLane.Length);
            foreach (var o in Ops) h.Z(o.InLent).Z(o.LeftMap);
        }
    }
}
