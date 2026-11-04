// The block on arrival (engine 0.41.0, brief 096; Motive briefs/096/motive/rivals.html §3, rivals.json routine; Pulse's routine as data;
// Sky 2026-11-04 "all recommended"; rulebook rivals.block, rivals.lookout). The rival car's routine, and the lookout's watch on the target's car.
//  · The car turns in on its timetable (CrewEntry) and drives the drive grid to the block point of the stand the cell's car stands at, or is
//    driving to: the first point of that stand's exit lane at least rivals.block.standOff from its car point (the stand's own data, so any
//    mission's rivals run it; a stand with no such point is rejected at load). A Drive of the cell's car started before it stops turns it for
//    the new stand's point. Stopped there it is a standing car on the way out: box-in (car.box, GameBoxIn.cs) does the rest.
//  · rivals.out after it stops the members aboard but the wheelman step out rivals.step apart in id order (the posted three, then the lookout);
//    engine 0.42.0 (Motive F1): the wheelman stays at the wheel through the final fight; he gets out only when his car is disabled or set
//    burning (every rider at once then, Motive V1). The wheelmanNear ring is retired (its hashed byte stays, false).
//  · The block ends when, in the final fight, the crew breaks with the wheelman at the wheel: he drives off at once the way it came, for good,
//    picking nobody up (F1); or when it explodes. A pre-fight break takes it away for security.rivals.retreat, then it blocks again.
//  · Engine 0.42.0 (Motive L10, car.lend, split on): a stand whose car stands on a drive has its block point across the drive's mouth, so the
//    rival car boxes both the cell's car on the drive and the official car at its kerbside stop.
//  · The lookout, posted, sees the target's car leave its stand with the target aboard, or standing elsewhere with him aboard, or a Take at it:
//    he runs after it (rivals.lookout.follow), re-aimed each second; within act reach of it while the target is in it or being taken, before
//    the final fight, the rivals reach the target (rivals-reach-vale, cause lookout).
// Everything here is a function of ticks and orders, hashed with the crew (HashCrew).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        public CrewCar CarSt = CrewCar.Off; public int CarStTick;
        Path? ccPath; long[]? ccProf; int ccStart; Pt ccAt, ccHead; string ccStop = "";
        int crewNextAt = INF; string crewNext = "";               // the car's next scheduled move (back after a break)
        /// <summary>The car's next scheduled move and its tick (a read; never hashed beyond the state it is).</summary>
        public (int tick, string what) CrewNextMove => (crewNextAt, crewNext);
        string blockStandId = "";   // the stand the car blocks (or is driving to block)
        const bool wheelmanCalled = false; // retired by engine 0.42.0 (F1: he stays at the wheel); its hashed byte stays, false
        bool carForGood;            // the block ended in the final fight: the car leaves for good
        bool rivalBlockFight;       // the safe fight's spring found the rival car as its block: no blocker is picked, the box is the gate
        bool boxedBefore;           // the getaway car was boxed in last tick (car.unboxed's edge; log only)
        /// <summary>Engine 0.41.0 (rivals.turn): the agents the crew counts a foe at the spring (a Loyal man told them of the Fixer).</summary>
        readonly SortedSet<string> springFoes = new SortedSet<string>(StringComparer.Ordinal);
        /// <summary>Host read: the agents the crew counts a foe at the spring (a Loyal refusal names the Fixer).</summary>
        public IReadOnlyCollection<string> CrewSpringFoes => springFoes;

        /// <summary>The block point of a stand: the first point along its exit lane at least rivals.block.standOff from its car point (cm, integer
        /// walk along each segment). Null when the lane never gets that far (the load rejects such a site: CheckBlockPoints).</summary>
        Pt? BlockPoint(Stand st)
        {
            if (DriveMouthBlock(st) is var dm && dm.HasValue) return dm.Value.at; // engine 0.42.0 (Motive L10): across the drive's mouth
            var lane = st.Exit; long need = (long)Cr.BlockCm * Cr.BlockCm;
            for (int i = 0; i < lane.Length; i++)
            {
                if (G.D2(lane[i], st.CarAt) >= need) { if (i == 0) return lane[0]; var a = lane[i - 1]; var b = lane[i]; long len = G.Dist(a, b); long lo = 0, hi = len; // the first cm along a→b far enough
                    while (lo < hi) { long mid = (lo + hi) / 2; var p = G.Lerp(a, b, (int)mid, (int)Math.Max(1, len)); if (G.D2(p, st.CarAt) >= need) hi = mid; else lo = mid + 1; }
                    return G.Lerp(a, b, (int)lo, (int)Math.Max(1, len)); }
            }
            return null;
        }
        /// <summary>A stand's block point and the stand it is for (the routine's target); a host read too (the Watch's dashed stop).</summary>
        public (Pt at, string stand) BlockAt(Stand st) => (BlockPoint(st) ?? throw new InvalidOperationException($"stand {st.Id} has no block point"), st.Id);
        /// <summary>RIV-30: every stand of a mission with rivals has a block point (unknown = reject at load, the stand and the rule named).</summary>
        void CheckBlockPoints()
        {
            foreach (var st in S.Stands)
                if (BlockPoint(st) == null) throw new FormatException($"rivals.block: stand '{st.Id}' has no block point: its exit lane never runs {Cr.BlockCm / 100} m from its car (rivals.block.standOff)");
        }

        // ---------------------------------------------------------------- the car's legs

        void CarLeg(CrewCar st, Path p, string stop)
        {
            CarSt = st; CarStTick = Tick; ccPath = p; ccProf = CarProfile(p); ccStart = Tick; ccStop = stop;
            if (ccProf.Length <= 1) CarArrive(); // already there
        }
        void CarHalt(CrewCar st, string stop, Pt at, Pt head) { CarSt = st; CarStTick = Tick; ccPath = null; ccProf = null; ccAt = at; ccHead = head; ccStop = stop; }
        bool CarMoving => ccPath != null && ccProf != null && (CarSt == CrewCar.Entering || CarSt == CrewCar.Returning || CarSt == CrewCar.Leaving);
        long CcMm(int tick) { int k = tick - ccStart; var d = ccProf!; return k <= 0 ? 0 : k >= d.Length ? d[d.Length - 1] : d[k]; }
        /// <summary>Where the rival car is and which way it points, or null while it is off the map (Off, Away, Gone).</summary>
        (Pt pos, Pt heading)? RivalCarPoseCrew()
        {
            if (CarSt == CrewCar.Off || CarSt == CrewCar.Away || CarSt == CrewCar.Gone) return null;
            if (CarMoving) { long mm = CcMm(Tick); return (ccPath!.At(mm), ccPath.Heading(mm)); }
            return (ccAt, ccHead);
        }
        /// <summary>The leg to a stand's block point from where the car is (or its entry).</summary>
        void LegToBlock(CrewCar st, Stand stand, Pt? from = null)
        {
            var f = from ?? S.CarStand(S.RivalCarEntry); blockStandId = stand.Id;
            CarLeg(st, CarRouteP(S.Drive.NearestFree(f) ?? f, BlockAt(stand).at), "block:" + stand.Id);
        }

        void Enter()
        {
            RivalsArrived = true;
            LegToBlock(CrewCar.Entering, StandForCrew());
            Note("rivals.coming", RivalsDrawn ? "A rival car turns in off Orbit Walk, racing in at the gunfire. It is coming for our car." : $"A rival car turns in off Orbit Walk. It is after {ValeName} too, and it is coming for our car.");
        }

        /// <summary>F1 for the car: the cell's car starts a Drive while the rival car still drives in (or back): it turns for the new stand's point.</summary>
        void BlockRetarget(Stand to)
        {
            if ((CarSt != CrewCar.Entering && CarSt != CrewCar.Returning) || blockStandId == to.Id) return;
            var from = RivalCarPose()!.Value.pos; LegToBlock(CarSt, to, from);
            Note("rivals.retarget", "The rival car turns after our car.");
        }

        /// <summary>The car reaches the end of its leg.</summary>
        void CarArrive()
        {
            var end = ccPath!.End; var head = ccPath.Heading(ccPath.Length); var was = CarSt;
            switch (was)
            {
                case CrewCar.Entering: case CrewCar.Returning:
                    CarHalt(CrewCar.Blocking, ccStop, end, S.StandById(blockStandId) is Stand bs && DriveMouthBlock(bs) is var dm && dm.HasValue ? dm.Value.heading : head); // engine 0.42.0 (L10): square across the drive's mouth
                    if (was == CrewCar.Returning) Note("rivals.back", "The rival car is back across our way out."); else Note("rivals.block", "The rival car stops across our way out: our car is boxed in.");
                    { int k = 0; foreach (var m in Crew) if (m.Mode == CrewMode.Aboard && Up(m) && m != Wheelman && m.OutAt >= INF) m.OutAt = Tick + T(Cr.OutS) + T(Cr.StepS) * k++; // the posted three, then the lookout
                      if (was == CrewCar.Returning) CrewBrokenPre = false;
                      } // engine 0.42.0 (Motive F1): the wheelman stays at the wheel
                    return;
                case CrewCar.Leaving:
                    CarSt = carForGood ? CrewCar.Gone : CrewCar.Away; CarStTick = Tick; ccPath = null; ccProf = null;
                    if (carForGood) foreach (var m in Crew) if (m.Mode == CrewMode.Aboard && Up(m)) { m.Mode = CrewMode.Gone; m.ModeTick = Tick; }
                    Note("rivals.away", carForGood ? "The rival car is gone for good." : "The rival car leaves the district. It may come back.");
                    return;
                default: CarHalt(CrewCar.Stopped, ccStop, end, head); return;
            }
        }

        void CrewNext()
        {
            var what = crewNext; crewNextAt = INF; crewNext = "";
            if (what == "back" && CarSt == CrewCar.Away && !carForGood && !crewCarless && Crew.Exists(m => Up(m) && m.Mode == CrewMode.Aboard))
            { LegToBlock(CrewCar.Returning, StandForCrew()); Note("rivals.coming", "The rival car turns in again. It is coming for our car."); }
        }

        // ---------------------------------------------------------------- the wheelman

        /// <summary>Engine 0.42.0 (Motive F1): he stays at the wheel through the final fight; only his car disabled (CarDisable) or set burning
        /// (CarHit, V1) sets his step-out. Kept as the crew step's hook.</summary>
        void WheelmanStep() { }
        /// <summary>Every shot (Resolve): engine 0.42.0 retires the wheelman's gunfire ring (F1); nothing calls him out.</summary>
        void RivalCarShotNear(Pt from, Pt at, string target) { }
        /// <summary>F1 (Motive): the wheelman still at the wheel of his car (not counted in the break, not a fighter, no fire).</summary>
        bool AtTheWheel(CrewMember m) => m == Wheelman && !m.Fight && m.Mode == CrewMode.Aboard && Up(m);

        // ---------------------------------------------------------------- the block ends

        /// <summary>Where the rival car is the safe fight's block (Motive rivals.json: the box is the gate; no blocker is picked): it boxes the
        /// getaway car in, or drives in to do it.</summary>
        bool RivalBlockHere => !Range && (CarSt == CrewCar.Entering || CarSt == CrewCar.Returning || GetawayBoxers().Contains("car:rivals"));

        /// <summary>Once a tick: the block ends when the crew breaks in the final fight with its wheelman at the wheel (F1: he never got out): the
        /// car drives off the way it came at once, for good, picking nobody up. The edge of the box clearing is logged (car.unboxed).</summary>
        void BlockEnds()
        {
            if (FightBranch == "safe" && CarSt == CrewCar.Blocking && !CarDisabled("car:rivals") && FightWhy.Contains("rivals-break"))
            {
                var w = Wheelman; var h = w.H;
                bool back = !w.Gun.Down && (w.Mode == CrewMode.Aboard || (h != null && h.Gun.St == "gone"));
                if (back)
                {
                    w.Mode = CrewMode.Aboard; w.ModeTick = Tick; w.Fight = false; carForGood = true;
                    var cp = RivalCarPose()!.Value;
                    CarLeg(CrewCar.Leaving, CarRouteP(S.Drive.NearestFree(cp.pos) ?? cp.pos, S.CarStand(S.RivalCarEntry)), S.RivalCarEntry);
                    Note("rivals.drives-off", "The beaten crew's wheelman drives off: the rival car is out of our way."); Chain.Add("rivals-block-off");
                }
            }
            if (Range) return;
            bool boxed = GetawayBoxers().Count > 0;
            if (boxedBefore && !boxed && !Over) Note("car.unboxed", "Nothing stands across our way out now.");
            boxedBefore = boxed;
        }

        // ---------------------------------------------------------------- the lookout's watch

        /// <summary>The target is in his car or being taken at it.</summary>
        bool ValeAtHisCar => Vale == ValeAt.InCar || Vale == ValeAt.Taking || Vale == ValeAt.Lent;
        /// <summary>The lookout, posted at his post, sees the target's car leave its stand with the target aboard, or standing elsewhere with him
        /// aboard, or a Take at it: he follows it (rivals.lookout).</summary>
        void LookoutWatch()
        {
            var l = Lookout;
            if (Over || FightBranch.Length > 0 || !Up(l) || l.Fight || l.Mode != CrewMode.Posted || l.Post != "lookout" || !ValeAtHisCar) return;
            if (valeDriveStart < 0 && Vale != ValeAt.Taking) return; // in his car at the pickup, not yet driving: nothing to follow
            var pose = MemberPose(l); if (!pose.HasValue) return; var car = CarPos();
            // his job is the car: his cone is read along his post's facing (m.Base, toward the target's car), not turned by an idle beat's yaw
            if (!G.InCone(pose.Value.pos, l.Base, R.RivReachCm, R.RivHalfCm, near, car) || !Sees(pose.Value.pos, car)) return;
            StartWalk(l, pose.Value.pos, car, "follow", Cr.LookoutRunCmS); l.Until = Tick + Hz;
            Note("rivals.lookout-follows", $"The rival lookout is going after {ValeName}'s car."); Chain.Add("lookout-follows"); // brief 096: the card that stays until clicked
        }
        /// <summary>Each second of his run: re-aimed at the target's car where it is now (it may still drive); given up once the target is out of it.</summary>
        void FollowAim(CrewMember m)
        {
            m.Until = Tick + Hz;
            if (!ValeAtHisCar) { StartWalk(m, MemberPos(m, Tick), S.Stand(S.RivalLookout), "lookout", rivSpeed); return; }
            var car = CarPos(); if (m.Walk != null && G.Within(m.Walk.End, Walk.NearestFree(car) ?? car, 100)) return;
            var at = MemberPos(m, Tick); StartWalk(m, at, car, "follow", Cr.LookoutRunCmS); m.Until = Tick + Hz;
        }
        /// <summary>The lookout on foot within act reach of the target's car while the target is in it or being taken, before the final fight.</summary>
        void LookoutReach()
        {
            if (Over || FightBranch.Length > 0 || !ValeAtHisCar) return;
            var l = Lookout; if (!Up(l) || l.Fight || !l.OnFoot || !(l.Goal == "follow" || l.Post == "follow")) return;
            var at = MemberPos(l, Tick); if (!G.Within(at, CarPos(), actReach) && CarBodyCm("car:official", at) > actReach) return; // act reach of the car's centre or its body (car.footprint)
            Fail("rivals-reach-vale", $"The rival lookout followed {ValeName}'s car and reached him first.", () => With(Because("lookout", true, Locate(l.Id, "reached", at), Locate("councillor", "victim")), ""));
        }
    }
}
