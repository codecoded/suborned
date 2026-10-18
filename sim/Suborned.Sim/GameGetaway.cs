// The getaway (brief 018, Motive; approved by Sky 2026-10-18: "I want to get my team into the car, and get away"). Rulebook fight.hold,
// fight.block, fight.blockers, cell.board, campaign.custody, debrief.lines left-behind, captive, car-seen; site getaway.exit.
//  · The hold: a holder is a combatant (a final-fight hostile, or anyone who has fired on or aims at the cell) not down, broken or gone,
//    within fight.hold of the car (any level, line or none) or on the street inside the exit lane. Every position the engine keeps is on
//    the street level, so "on the street" holds for every body here. An ambusher lying in wait holds only if its spot does.
//  · The blocker: fight.blockers hostiles per fight go for the road, by the shortest grid walk to the lane (to the lane's nearest point a
//    metre inside its sides), ties by id; safe: picked from the ambush when it springs; loud: from the officers on the fight's first tick
//    (they all walk at one pace, so the shortest walk is the first to arrive). It walks to the nearest free cover point inside the lane,
//    else to that lane point in the open, without stopping on the way, and holds it as police hold a stand. Not replaced.
//  · Board (1 s; carrying, +carry.s, the carried first) and Go (instant: refused while held, naming the holders). The win is Vale in the
//    car when it leaves. Left behind at Go: downed → captured; standing → captured while a combatant is up and not broken (fighting) or
//    the seal clock runs (sealed), else walks out. Captures happen only at Go.
//  · Seen leaving: a police officer, a robot or an engaged patrol up with the car in its cone or within witness range on a clear line: one
//    witnessed incident (+witness-trail) and the investigation hunts the car (cell-car). Rivals never report.
//  · The custody projection for the debrief (no campaign layer runs yet): +captured-trail each, held at the police post, the Talker test
//    once each between-missions phase, the facts a captive would give.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Suborned.Sim
{
    /// <summary>One captured operative as the debrief projects them (brief 018 §3; rulebook campaign.custody).</summary>
    public sealed class Captive
    {
        public string Op = "", Why = "", HeldAt = "";
        /// <summary>Missions the captive holds before the questioning they talk at; TalksAt that questioning (1-based), 0 if they hold the whole rescue window.</summary>
        public int Holds, TalksAt, RescueUntil;
        public readonly int[] Sc = new int[4]; // bravery, resentment, fear, loyalty when they talk (or at the window's end)
        public readonly List<string> Facts = new List<string>(); public string Left = "", Right = "";
    }

    public sealed partial class Game
    {
        /// <summary>The hostiles that went for the road (brief 018 §1), in the order picked.</summary>
        public readonly List<string> Blockers = new List<string>();
        bool blockersPicked; string pickFrom = ""; bool carWasHeld, blockerOut; // the pick due this tick (rivals, police); the hold's last state; blocker.out logged

        /// <summary>End of the tick's fire (Waymark, c018): a pick due this tick is made among the candidates still standing after it (a
        /// candidate downed by this tick's shots is no candidate); the blocker's fall and the hold coming free are logged by code.</summary>
        void GetawayAfterFire()
        {
            if (pickFrom.Length > 0)
            {
                var from = pickFrom; pickFrom = "";
                PickBlockers(Hostiles.FindAll(h => from == "rivals" ? IsRival(h.Id) : !IsRival(h.Id) && h.Present));
            }
            if (FightBranch.Length == 0) return;
            if (!blockerOut && Blockers.Count > 0)
            {
                bool any = false; foreach (var id in Blockers) { var b = HostileOf(id); if (b != null && Up(b)) any = true; }
                if (!any) { blockerOut = true; var b0 = HostileOf(Blockers[0])!; Note("blocker.out", $"{b0.Id}, on the road, is {(b0.Down ? "down" : "gone")}: nobody blocks the lane now."); }
            }
            bool held = CarHeld();
            if (carWasHeld && !held) Note("car.free", "Nothing holds the getaway car: it can go.");
            carWasHeld = held;
        }
        /// <summary>The tick the getaway car left (INF while it hasn't).</summary>
        public int CarLeftTick = INF;
        /// <summary>The security body that saw the car leave ("" if none): the departure was one witnessed incident and the car is hunted.</summary>
        public string CarSeenBy = "";
        /// <summary>Everyone not in the car when it left, as LeftBehind() gave them at that tick.</summary>
        public readonly List<(string op, string fate, string why)> LeftBehindAtGo = new List<(string, string, string)>();

        /// <summary>Anyone in the getaway car: an operative, or Vale.</summary>
        public bool AnyInCar { get { foreach (var op in Ops) if (op.InCar) return true; return Vale == ValeAt.Car; } }

        bool HoldsCar(Pt p) => G.Within(p, Car, R.HoldCm) || S.InExitLane(p);
        static bool Up(Hostile h) => !h.Down && h.Gun.St != "break" && h.Gun.St != "gone";

        /// <summary>The holders of the getaway car now, in id order (brief 018 §1): the HUD names them, Go's refusal lists them. Read-only.</summary>
        public List<string> CarHolders()
        {
            var l = new List<string>();
            foreach (var h in Hostiles) if (Up(h) && (h.Present || FightBranch == "safe") && HoldsCar(h.Pos(Tick))) l.Add(h.Id); // a broken rival no longer holds it (Pulse §2.1)
            foreach (var id in CombatantIds())
            {
                if (IsFightHostile(id) || l.Contains(id) || !Combatant(id)) continue;
                var at = PosOf(id); if (at.HasValue && HoldsCar(at.Value)) l.Add(id);
            }
            l.Sort(StringComparer.Ordinal);
            return l;
        }
        /// <summary>A holder still stands (rulebook fight.hold): Go is refused.</summary>
        public bool CarHeld() => CarHolders().Count > 0;

        /// <summary>A combatant still up and not broken, anywhere (a lying-in-wait ambusher counts, as for the hold): the fight is still on.</summary>
        bool FightStillOn()
        {
            foreach (var h in Hostiles) if (Up(h) && (h.Present || FightBranch == "safe")) return true;
            foreach (var id in CombatantIds()) if (!IsFightHostile(id) && Combatant(id)) return true;
            return false;
        }

        /// <summary>Everyone a Go now would leave behind, with their fate and why (brief 018 §3): the Go card's preview and the H11 warning.
        /// Read-only. fate: captured or walked; why: downed, fighting, sealed or walked.</summary>
        public List<(string op, string fate, string why)> LeftBehind()
        {
            var l = new List<(string, string, string)>(); bool fighting = FightStillOn(), sealing = Lockdown != INF;
            foreach (var op in Ops)
            {
                if (op.InCar) continue;
                string name = Sim.Op.Name(op.Id);
                if (op.Down) l.Add((name, "captured", "downed"));
                else if (fighting) l.Add((name, "captured", "fighting"));
                else if (sealing) l.Add((name, "captured", "sealed"));
                else l.Add((name, "walked", "walked"));
            }
            return l;
        }

        /// <summary>Retired by brief 018 (LeftBehind() replaces it): the names a Go now would leave captured. Kept for hosts mid-migration only.</summary>
        [Obsolete("brief 018: LeftBehind() gives every op left behind with fate and why")]
        public List<string> WouldBeCaptured() { var l = new List<string>(); foreach (var x in LeftBehind()) if (x.fate == "captured") l.Add(x.op); return l; }

        // ---------------------------------------------------------------- the orders

        /// <summary>Board (brief 018 §2): into the getaway car, with whoever the operative carries (Vale, or a downed operative), the carried first.</summary>
        string Board(Op op, Order o)
        {
            if (o.Target.Length > 0 && o.Target != "car") return No("target-unknown", $"board names the getaway car (\"car\"), not '{o.Target}'");
            if (op.Running) return No("board-running", "a running operative doesn't board: stop first");
            if (op.Id == OpId.Tech && LoopOn) return No("board-tethered", "the Tech is tethered to the cabinet while the loop holds");
            if (op.CarryBody.Length > 0) return No("already-carrying", "carrying a body: the car seats the cell and the target");
            if (!G.Within(op.Pos, Car, R.AtReachCm)) return No("stand-at-getaway", "stand at the getaway car", cm: R.AtReachCm);
            bool carrying = op.Carrying.HasValue || (op.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer);
            op.Gun.Order = ""; op.Gun.Force = false; op.Gun.Target = "";
            Engage(op, o, R.BoardS + (carrying ? R.CarryS : 0));
            return "";
        }
        void LandBoard(Op op)
        {
            if (op.Down) return;
            var seat = S.Stand(S.GetawayCar); string name = Sim.Op.Name(op.Id), with = "";
            op.InCar = true; op.Pos = seat; op.Walk = null; op.WalkMm = 0; op.Running = false; op.Gun.Order = ""; op.Gun.Force = false; op.Gun.Target = "";
            if (op.Carrying.HasValue)
            {
                var c = OpOf(op.Carrying.Value); op.Carrying = null; c.CarriedBy = null; c.InCar = true; c.Pos = seat;
                with = " with the " + Sim.Op.Name(c.Id); Chain.Add(Sim.Op.Name(c.Id) + "-in-getaway-car");
            }
            if (op.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer) { Vale = ValeAt.Car; with = " with Vale"; Chain.Add("vale-in-getaway-car"); }
            Note("board.in", $"The {name} is in the car{with}.");
        }

        /// <summary>A move on a boarded operative (brief 018 §2): board.s to step out at the car's side, then the move. Vale stays in.</summary>
        string StepOut(Op op, Order o)
        {
            Pt to;
            if (o.Point.HasValue) to = o.Point.Value;
            else if (S.HasPlace(o.To)) to = S.Stand(o.To);
            else return No("no-place", $"no place '{o.To}'");
            if (!S.Grid.Free(to)) return No("unwalkable", "that ground can't be walked (a roof, a wall or a solid)");
            if (S.Grid.Route(S.Stand(S.GetawayCar), to) == null) return No("no-walk", "no walk there");
            Engage(op, o, R.BoardS);
            return "";
        }
        void LandStepOut(Op op, Order o)
        {
            if (op.Down || !op.InCar) return;
            op.InCar = false; op.Pos = S.Stand(S.GetawayCar);
            Note("board.out", $"The {Sim.Op.Name(op.Id)} steps out of the car.");
            var why = Move(op, o);
            if (why.Length > 0) Note("order.rejected", $"{o.Op} move {o.To}: {why}");
        }

        /// <summary>Go (brief 018 §2): the car leaves at once with whoever is in it, and the mission ends at this tick. Refused while the car is
        /// held (car-held, its holders' ids in the answer) or empty.</summary>
        string Go(Order o)
        {
            if (o.Op.Length > 0 && o.Op != "cell" && !IsOpId(o.Op)) return No("target-unknown", $"'{o.Op}' gives no Go: the cell or an operative does");
            if (!AnyInCar) return No("car-empty", "nobody is in the car");
            var hs = CarHolders();
            if (hs.Count > 0) return No("car-held", $"the car is held: {string.Join(", ", hs)}", ids: string.Join(",", hs));
            Leave();
            return "";
        }

        /// <summary>The Go card before the click (Latch): would Go be taken now, and if not, why (car-held names the holders in `ids`). Read-only;
        /// LeftBehind() is the card's list of who stays.</summary>
        public Answer GoQuery()
        {
            var saved = refusal; refusal = default; string why;
            try
            {
                if (Over) why = No("mission-over", "the mission is over");
                else if (!AnyInCar) why = No("car-empty", "nobody is in the car");
                else { var hs = CarHolders(); why = hs.Count > 0 ? No("car-held", $"the car is held: {string.Join(", ", hs)}", ids: string.Join(",", hs)) : ""; }
            }
            catch (Exception e) when (!(e is OutOfMemoryException)) { why = No("engine-error", "engine error: " + e.GetType().Name + ": " + e.Message); }
            var a = AnswerOf(why); refusal = saved; return a;
        }

        void Leave()
        {
            SeenLeaving();
            var lb = LeftBehind(); LeftBehindAtGo.AddRange(lb);
            foreach (var (name, fate, why) in lb)
            {
                if (fate == "captured") { Captured.Add(name); Chain.Add(name + "-captured"); Note("captured", $"The {name} is left behind and captured ({why})."); }
                else { Chain.Add(name + "-walks-out"); Note("walked", $"The {name} is left behind on a calm street and walks out."); }
            }
            CarLeftTick = Tick;
            if (Vale == ValeAt.Car) { Result = Outcome.Success; Chain.Add("car-leaves-with-vale"); Note("success", "The car leaves with Vale. The crew escapes."); }
            else { Result = Outcome.Withdrawn; Chain.Add("car-leaves-without-vale"); Note("withdraw.car", "The car leaves without Vale: the cell withdraws."); }
            EndOpenFight(); // brief 018 §4: the final fight is judged at the tick the car leaves
            fightClosedLate = true; // Finish replaces a checkpoint already taken at this tick with the state after Go
        }

        /// <summary>Seen leaving (brief 018 §3): one witnessed incident and the car hunted.</summary>
        void SeenLeaving()
        {
            var by = CarWatcher(); if (by.Length == 0) return;
            CarSeenBy = by;
            if (incidents.Add("car-seen")) AddTrail(R.WitnessTrail, "car-seen:" + by);
            Note("car.seen", $"The {by} sees the car leave. The investigation will hunt it.");
        }

        /// <summary>The security body that would see the car leave now, the first by id, or "": the response unit's officers and the final
        /// fight's (their cone, else witness range on a clear line), the response's robots and police (theirs, likewise), the patrol only while
        /// engaged (chasing or answering an alarm). Never the rivals, never the guard. Read-only: the Go card's "+1 T".</summary>
        public string CarWatcher()
        {
            var car = Car; var c = new List<string>();
            bool near(Pt p) => G.Within(p, car, witnessRange) && Sees(p, car);
            for (int k = 0; k < SecGuns.Length; k++)
            {
                if (SecGuns[k].Down) continue; var pose = SecOfficerPose(k); if (!pose.HasValue) continue;
                if (SecOfficerWatching(k, car) || near(pose.Value.pos)) c.Add(SecGuns[k].Id);
            }
            // the final fight's officers watch the fight and the car is where it is: a clear line within their pistol's maximum reach
            // (brief 018 §10, Pulse; the response unit's officers, robots and patrol keep their cones)
            foreach (var h in Hostiles)
            {
                if (IsRival(h.Id) || SecIndex(h.Id) >= 0 || !h.Present || h.Down || h.Gun.St == "gone") continue;
                var p = h.Pos(Tick); if ((G.Within(p, car, R.Pistol.MaxCm) && Sees(p, car)) || near(p)) c.Add(h.Id);
            }
            foreach (var u in Units)
            {
                if (u.Legacy || u.Patrol || !u.OnStreet(Tick) || u.Gun.Down) continue;
                var up = UnitPose(u); if (UnitSees(u, car) || (up.HasValue && near(up.Value.pos))) c.Add(u.Id);
            }
            if (!PatrolDown && PatrolAwayUntil <= Tick && (PatrolChasing || PatrolUnit() != null || IsFightHostile("patrol")))
            {
                var pp = PatrolPose(); if (PatrolWatching(car) || (pp.HasValue && near(pp.Value.pos))) c.Add("patrol");
            }
            c.Sort(StringComparer.Ordinal);
            return c.Count > 0 ? c[0] : "";
        }

        // ---------------------------------------------------------------- the blocker and the car in the fight

        /// <summary>Pick the fight's blockers (rulebook fight.blockers) from these hostiles and send them to the road. Once per fight.</summary>
        void PickBlockers(List<Hostile> cands)
        {
            if (blockersPicked) return; blockersPicked = true;
            // brief 018 §10 (Pulse): each candidate is scored by the grid walk to the post it would take; cover is taken by the winner only
            var scored = new List<(long mm, string id, Hostile h)>();
            foreach (var h in cands)
            {
                if (!Up(h)) continue;
                var from = h.Pos(Tick); var post = BlockPost(from, out _);
                var to = S.Grid.NearestFree(post); if (!to.HasValue) continue;
                var route = S.Grid.Route(S.Grid.NearestFree(from) ?? from, to.Value); if (route == null) continue;
                scored.Add((new Path(route).Length, h.Id, h));
            }
            scored.Sort((a, b) => a.mm != b.mm ? a.mm.CompareTo(b.mm) : string.CompareOrdinal(a.id, b.id));
            for (int i = 0; i < scored.Count && i < R.Blockers; i++)
            {
                var (_, _, h) = scored[i];
                var post = BlockPost(h.Pos(Tick), out var cv); Pt to = cv != null ? post : S.Grid.NearestFree(post) ?? post;
                if (cv != null) TakeCover(h.Gun, cv); else ReleaseCover(h.Gun); // the cover it held before (a joined officer's) is free again
                h.Blocker = true; h.Home = to; MoveTo(h, to); Blockers.Add(h.Id);
                Note("fight.blocker", $"{h.Id} goes for the road ({(cv != null ? "cover at " + cv.Id : "the lane, in the open")}).");
            }
        }
        /// <summary>The blocker's post (brief 018 §10, Pulse): the cover BlockCover gives, else the open lane centreline fight.blockNear along it.</summary>
        Pt BlockPost(Pt from, out CoverPoint? cv) { cv = BlockCover(from); return cv != null ? cv.At : S.LanePointAt(R.BlockNearCm); }
        /// <summary>The free cover point inside the exit lane and between fight.blockNear and fight.blockFar of the car point (straight line, as
        /// the hold), nearest `from` (present, at least partial; ties by id), or null.</summary>
        CoverPoint? BlockCover(Pt from)
        {
            CoverPoint? best = null; long bd = long.MaxValue;
            foreach (var c in S.Cover)
            {
                if (coverTaken.ContainsKey(c.Id) || c.Q < R.CoverQPartial || !c.Present(Tick / Hz) || !S.InExitLane(c.At)) continue;
                if (G.Within(c.At, Car, R.BlockNearCm - 1) || !G.Within(c.At, Car, R.BlockFarCm)) continue; // the ring: not at the door, in the cell's reach
                long d = G.D2(from, c.At);
                if (d < bd || (d == bd && best != null && string.CompareOrdinal(c.Id, best.Id) < 0)) { bd = d; best = c; }
            }
            return best;
        }

        /// <summary>A rival within cell.atReach of the car while Vale is in it, with no operative standing within cell.reach of it (brief 018 §1).</summary>
        bool RivalReachesVale()
        {
            if (Vale != ValeAt.Car) return false;
            bool rival = false;
            foreach (var h in Hostiles) if (IsRival(h.Id) && h.Present && Up(h) && G.Within(h.Pos(Tick), Car, R.AtReachCm)) rival = true;
            if (!rival) return false;
            foreach (var op in Ops) if (!op.Down && !op.InCar && !op.Inside && G.Within(op.Pos, Car, actReach)) return false;
            return true;
        }

        // ---------------------------------------------------------------- the custody projection (the debrief)

        /// <summary>The district's police post a captive is held at: the post (not a staffed door) nearest the car, ties by id. A world with no
        /// police post is rejected (unknown = reject).</summary>
        string PostOfCaptives()
        {
            string best = ""; long bd = long.MaxValue;
            foreach (var (id, at) in S.Posts)
            {
                if (id.StartsWith("door:", StringComparison.Ordinal)) continue;
                long d = G.D2(at, Car);
                if (d < bd || (d == bd && string.CompareOrdinal(id, best) < 0)) { bd = d; best = id; }
            }
            if (best.Length == 0) throw new InvalidOperationException("a captive has no police post to be held at: the world places none (brief 018 §3; unknown = reject)");
            return best;
        }

        /// <summary>Brief 018 §3 in the between-missions phase: the left-behind lines, the car seen, each captive's custody (+captured-trail to
        /// the campaign, the Talker test once per phase held, projected) and what they would give.</summary>
        void Custody(Between b, Func<int, string> fmt)
        {
            foreach (var (op, fate, why) in LeftBehindAtGo) b.Lines.Add(new BetweenLine { Line = "left-behind", Who = op, Detail = $"{fate} ({why})" });
            if (CarSeenBy.Length > 0) { b.CarSeenBy = CarSeenBy; b.Lines.Add(new BetweenLine { Line = "car-seen", Who = CarSeenBy, Detail = $"+{fmt(R.WitnessTrail)} T; the investigation hunts the car (cell-car)" }); }
            if (!LeftBehindAtGo.Exists(x => x.fate == "captured")) return;
            string post = PostOfCaptives();
            var contacts = new List<string>();
            foreach (var p in People) if (p.Turned && !p.Down) contacts.Add(p.Id);
            foreach (var w in Walkers) if (w.P.Turned && !w.P.Down && !contacts.Contains(w.P.Id)) contacts.Add(w.P.Id);
            contacts.Sort(StringComparer.Ordinal);
            string side(int[] sc, int[] ix) { var parts = new List<string>(); foreach (var i in ix) parts.Add(Rules.ScaleNames[i] + " " + R.Step(i, sc[i]) + " " + fmt(sc[i])); return string.Join(" + ", parts); }
            foreach (var (op, fate, why) in LeftBehindAtGo)
            {
                if (fate != "captured") continue;
                var c = new Captive { Op = op, Why = why, HeldAt = post, RescueUntil = R.CustodyMissions };
                Array.Copy(R.CellStart, c.Sc, 4); // the founding cell (brief 018 §3); recruits would keep their own scales
                if (why != "downed") { c.Sc[Rules.Resentment] = R.Clamp(Rules.Resentment, c.Sc[Rules.Resentment] + R.AbandonedResentment); c.Sc[Rules.Loyalty] = R.Clamp(Rules.Loyalty, c.Sc[Rules.Loyalty] - R.AbandonedLoyalty); }
                for (int q = 1; q <= R.CustodyMissions; q++)
                {
                    c.Sc[Rules.Fear] = R.Clamp(Rules.Fear, c.Sc[Rules.Fear] + R.CustodyFear);
                    c.Sc[Rules.Loyalty] = R.Clamp(Rules.Loyalty, c.Sc[Rules.Loyalty] - R.CustodyLoyalty);
                    if (R.IsTalker(c.Sc)) { c.TalksAt = q; break; }
                }
                c.Holds = c.TalksAt > 0 ? c.TalksAt - 1 : R.CustodyMissions;
                foreach (var o in Ops) c.Facts.Add("cell-face:" + Sim.Op.Name(o.Id));
                c.Facts.Add("cell-car"); c.Facts.Add("place-time");
                foreach (var id in contacts) c.Facts.Add("contact:" + id);
                c.Left = side(c.Sc, R.TalkerLeft); c.Right = side(c.Sc, R.TalkerRight);
                b.CampaignTrail += R.CapturedTrail; b.Captives.Add(c);
                b.Lines.Add(new BetweenLine { Line = "captive", Who = op, Detail = $"held at {post}; holds {fmt(c.Holds)} missions; rescue for {fmt(c.RescueUntil)} missions; +{fmt(R.CapturedTrail)} T" });
                if (c.TalksAt > 0) b.Lines.Add(new BetweenLine { Line = "give-up", Who = op, Detail = $"{string.Join(", ", c.Facts)} at questioning {fmt(c.TalksAt)}: {c.Left} > {c.Right}" });
            }
        }

        /// <summary>The getaway's state for the checkpoint hash: no bytes until someone boards, a blocker is picked or the car leaves.</summary>
        internal void HashGetaway(Hash.H h)
        {
            bool any = Blockers.Count > 0 || CarLeftTick != INF || CarSeenBy.Length > 0 || carWasHeld || pickFrom.Length > 0; foreach (var op in Ops) any |= op.InCar;
            if (!any) return;
            foreach (var op in Ops) h.Z(op.InCar);
            foreach (var x in Blockers) h.S(x);
            h.Z(carWasHeld).Z(blockerOut);
            h.I(CarLeftTick).S(CarSeenBy);
            foreach (var (op, fate, why) in LeftBehindAtGo) h.S(op).S(fate).S(why);
        }
    }
}
