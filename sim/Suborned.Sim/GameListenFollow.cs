// Engine 0.55.0 (brief 110; Sky 2026-11-11, playing 0.2.330: "I think scout has to wait to listen to Inez - which is crazy. If I order a listen
// command, then it should be actioned. No commands should be waiting. It either is accepted or not available."; rulebook cell.listen.follow).
// A Listen acts at once. Out of reach it is the plain walk in (cell.approach, GameApproach.cs: no spot ahead, no wait there) and starts on the
// first tick in reach. Nobody is held for it (rounds.holds names no listen): the person goes on. From its start, once a tick (ListenFollow):
//  · the act's clock runs only while the agent is within reach of the person: out of reach, BusyEnd (and BusyStart, so the HUD's progress holds)
//    moves on a tick, and the Listen lands later;
//  · the person walking, the agent follows: farther than cell.listen.follow, it walks after them (Intercept, re-aimed every cell.approach.repath s)
//    and stops once within cell.listen.nearWalking;
//  · the person standing (a server whose round keeps her within atSpot for the next `step` s; anyone else, where they stand), the agent settles:
//    in place already (in reach, no nearer than nearStill, seated or by a wall out of the way) it stays; else the chair or the wall spot of the
//    engine 0.39.0 choice for someone who stays put (BestChair, BestGround: out of every cone), walked to at once; none, it stays in reach;
//  · a spot whose seat the person has walked out of reach of is given up (he gets up and follows);
//  · the person off the street (inside, down, gone) or no walk to them: the Listen ends there, nothing learned, nothing charged (order.dropped).
// With no `follow` (a rulebook pinned before 0.55.0) none of this runs: the spot and its wait (GameListenSpot.cs) and the hold, as before.
using System;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Once a tick, before the busy act lands: a Listen under way under cell.listen.follow keeps to its person (see the file's head).</summary>
        void ListenFollow(Op op)
        {
            if (!R.ListenFollowOn || op.Busy is not Order o || o.Do != "listen" || op.BusyEnd <= Tick) return;
            if (op.Down || op.Inside || op.InCar || op.Driven || op.CarriedBy.HasValue) return;
            Person? p = null; CrewMember? m = null; Pt? her; bool still; int reach;
            if (IsCrewId(o.Target) && !Range)
            {
                m = CrewOf(o.Target); her = m != null && Up(m) && !m.Fight ? MemberPose(m)?.pos : null;
                still = m != null && m.Mode == CrewMode.Posted; reach = listenReach;
            }
            else
            {
                p = PersonByRole(o.Target); her = PersonPos(p); reach = ActReachOf(op, "listen");
                still = !her.HasValue || ServerOf(p) == null || (PoseAhead(p, T(R.ListenStepS)) is var a && a.HasValue && G.Within(a.Value.pos, her.Value, R.ListenAtSpotCm));
            }
            if (!her.HasValue)
            {
                bool down = p != null ? p.Down : m != null && m.Gun.Down;
                EndListen(op, o, down ? "person-down" : m != null && m.Paid ? "crew-gone" : "person-away", $"{o.Target} {(down ? "is down" : "is not out on the street")}: the Listen ends, nothing heard");
                return;
            }
            var at = her.Value;
            if (!G.Within(op.Pos, at, reach)) { op.BusyEnd++; op.BusyStart++; } // the clock runs only in reach
            var sp = SpotOf(op);
            if (sp != null && !G.Within(sp.At, at, reach)) { op.Spot = null; sp = null; } // they walked off from the seat's reach: up and after them
            if (sp == null && op.Walk != null && G.Within(op.Pos, at, R.ListenNearWalkingCm)) { op.Walk = null; op.WalkMm = 0; } // near enough while following
            if (op.Walk != null && Tick < op.ActNext) return;
            op.ActNext = Tick + T(R.ApproachRepathS);
            if (sp != null)
            {
                bool there = sp.Chair.Length > 0 ? op.Pos == sp.At : G.Within(op.Pos, sp.At, R.ListenAtSpotCm);
                if (!there && op.Walk == null) { if (SpotRoute(op.Pos, sp.At, sp.Stand) is Path r0 && r0.Length > 0) SetFollowWalk(op, r0); else op.Spot = null; }
                return;
            }
            if (still)
            {
                if (SettledAt(op, at, reach)) { op.Walk = null; op.WalkMm = 0; return; }
                if (SettleSpot(op, o, at, reach)) return;
                if (G.Within(op.Pos, at, reach)) { op.Walk = null; op.WalkMm = 0; return; } // nothing discreet to be had: it stays in reach
            }
            if (G.Within(op.Pos, at, still ? reach : R.ListenFollowCm)) return;
            Path? path = p != null ? Intercept(op, p, R.ListenNearWalkingCm)?.path : Walk.NearestFree(at) is Pt to && Walk.Route(op.Pos, to) is Pt[] rt ? new Path(rt) : null;
            if (path == null) { EndListen(op, o, "no-walk", $"no walk to {o.Target}: the Listen ends, nothing heard"); return; }
            if (path.Length > 0) SetFollowWalk(op, path); else { op.Walk = null; op.WalkMm = 0; }
        }

        /// <summary>In place for a settled Listen: in reach, no nearer than nearStill, and seated or by a wall out of the way (ListenInPlace's own test).</summary>
        bool SettledAt(Op op, Pt at, int reach) => G.Within(op.Pos, at, reach) && !G.Within(op.Pos, at, R.ListenNearStillCm)
            && (SeatOf(op) != null || (op.Walk == null && BesideSolid(op.Pos) && !S.Grid.Cuts(op.Pos, R.BodyMinCm, R.ListenPassCm)));

        /// <summary>The settle: a free chair for the rest of the act, else free ground by a wall, round someone who stays put (the 0.39.0 choice); walked to now.</summary>
        bool SettleSpot(Op op, Order o, Pt at, int reach)
        {
            int speed = Math.Max(1, PaceOf(op, false));
            var c = BestChair(op, at, reach, R.ListenNearStillCm, op.BusyEnd + T(R.ListenChairMarginS), -1, speed);
            if (c.HasValue && SpotRoute(op.Pos, c.Value.seat, c.Value.stand) is Path pc)
            { op.Spot = new ListenSpot { For = o, At = c.Value.seat, Stand = c.Value.stand, Chair = c.Value.chair, Chose = Tick, Until = op.BusyEnd }; if (pc.Length > 0) SetFollowWalk(op, pc); return true; }
            var g = BestGround(op, at, reach, R.ListenNearStillCm, -1, speed);
            if (g.HasValue && Walk.Route(op.Pos, g.Value) is Pt[] rg)
            { op.Spot = new ListenSpot { For = o, At = g.Value, Stand = g.Value, Chose = Tick, Until = op.BusyEnd }; var pg = new Path(rg); if (pg.Length > 0) SetFollowWalk(op, pg); return true; }
            return false;
        }

        void SetFollowWalk(Op op, Path path) { op.Walk = path; op.WalkMm = 0; op.Running = false; op.WalkSpeed = PaceOf(op, false); op.CloseWalk = false; }

        /// <summary>A Listen under way ends early: the person is gone or out of every walk. Nothing is learned or charged; the log says so (order.dropped).</summary>
        void EndListen(Op op, Order o, string code, string why)
        {
            op.Busy = null; op.BusyQuote = null; op.BusyPaid = 0; op.BusyEnd = Tick; op.Spot = null; op.Walk = null; op.WalkMm = 0;
            Dropped.Add((Tick, op.Id, o, code, why));
            Note("order.dropped", $"{Sim.Op.Name(op.Id)} listen {o.Target}: {code}: {why}");
        }
    }
}
