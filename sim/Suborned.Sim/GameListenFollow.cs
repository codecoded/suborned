// Engine 0.55.0 (brief 110; Sky 2026-11-11, playing 0.2.330: "I think scout has to wait to listen to Inez - which is crazy. If I order a listen
// command, then it should be actioned. No commands should be waiting. It either is accepted or not available."; rulebook cell.listen.follow).
// A Listen acts at once. Out of reach it is the plain walk in (cell.approach, GameApproach.cs: no spot ahead, no wait there) and starts on the
// first tick in reach. Nobody is held for it (rounds.holds names no listen): the person goes on. From its start, once a tick (ListenFollow):
//  · the act's clock runs only while the Scout is within reach of the person: out of reach, BusyEnd (and BusyStart, so the HUD's progress holds)
//    moves on a tick, and the Listen lands later;
//  · the person walking, she follows: farther than cell.listen.follow, she walks after them (Intercept, re-aimed every cell.approach.repath s)
//    and stops once within cell.listen.nearWalking;
//  · the person standing (a server whose round keeps her within atSpot for the next `step` s; anyone else, where they stand), she settles.
//    Engine 0.56.0 (Sky 2026-11-12: "the scout (she) prefers a chair"): a free chair first (out of every cone if one is, else in a watcher's view:
//    the act holds every watcher's clock), then a wall (out of every cone), then where she stands. A chair or wall
//    in reach of a server's post as well as of where she is now comes first, so the server can go home without the Scout moving. Standing by a
//    wall, she takes a chair as soon as one is free;
//  · a spot whose seat the person has walked out of reach of is given up (she gets up and follows);
//  · engine 0.56.0, earshot (cell.listen.earshot; Sky 2026-11-12: "Once someone has got to someone to listen to them, the other person should
//    remain in earshot"): a server listened to keeps her round in the Scout's reach. A table that would take her out of it is skipped
//    (GameRounds.EarshotMask): she rests at her post through it. Her round's own moves still win (the summons, its end), as does anyone's
//    schedule (the driver, the councillor, the crew): the Scout follows, and if they leave the street the Listen ends;
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
            Person? p = null; CrewMember? m = null; Pt? her; bool still; int reach; RoundsData.Server? sv = null;
            if (IsCrewId(o.Target) && !Range)
            {
                m = CrewOf(o.Target); her = m != null && Up(m) && !m.Fight ? MemberPose(m)?.pos : null;
                still = m != null && m.Mode == CrewMode.Posted; reach = listenReach;
            }
            else
            {
                p = PersonByRole(o.Target); her = PersonPos(p); reach = ActReachOf(op, "listen"); sv = her.HasValue ? ServerOf(p) : null;
                still = !her.HasValue || sv == null || (PoseAhead(p, T(R.ListenStepS)) is var a && a.HasValue && G.Within(a.Value.pos, her.Value, R.ListenAtSpotCm));
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
            if (sv != null && R.ListenEarshot && G.Within(op.Pos, at, reach) && EarshotMask(sv, sp?.At ?? op.Pos, reach) is string table) // engine 0.56.0: the person stays in earshot
                Note("round.earshot", $"{p!.Name} skips {table} for now: the Scout is listening, so the round keeps to the post and the tables near her.");
            if (sp == null && op.Walk != null && G.Within(op.Pos, at, R.ListenNearWalkingCm)) { op.Walk = null; op.WalkMm = 0; } // near enough while following
            if (op.Walk != null && Tick < op.ActNext) return;
            op.ActNext = Tick + T(R.ApproachRepathS);
            Pt? post = sv != null ? sv.At : (Pt?)null;
            if (sp != null)
            {
                bool there = sp.Chair.Length > 0 ? op.Pos == sp.At : G.Within(op.Pos, sp.At, R.ListenAtSpotCm);
                if (there && sp.Chair.Length == 0 && R.ListenEarshot && still && SettleChair(op, o, at, post, reach)) return; // engine 0.56.0: by a wall, a chair is taken once one is free
                if (!there && op.Walk == null) { if (SpotRoute(op.Pos, sp.At, sp.Stand) is Path r0 && r0.Length > 0) SetFollowWalk(op, r0); else op.Spot = null; }
                return;
            }
            if (still)
            {
                bool seated = SeatOf(op) != null;
                if (seated && SettledAt(op, at, reach)) { op.Walk = null; op.WalkMm = 0; return; }
                if (R.ListenEarshot ? SettleChair(op, o, at, post, reach) : false) return; // engine 0.56.0: a chair first
                if (SettledAt(op, at, reach)) { op.Walk = null; op.WalkMm = 0; return; }
                if (SettleSpot(op, o, at, post, reach)) return;
                if (G.Within(op.Pos, at, reach)) { op.Walk = null; op.WalkMm = 0; return; } // nothing discreet to be had: she stays in reach
            }
            if (G.Within(op.Pos, at, still ? reach : R.ListenFollowCm)) return;
            Path? path = p != null ? Intercept(op, p, R.ListenNearWalkingCm)?.path : Walk.NearestFree(at) is Pt to && Walk.Route(op.Pos, to) is Pt[] rt ? new Path(rt) : null;
            if (path == null) { EndListen(op, o, "no-walk", $"no walk to {o.Target}: the Listen ends, nothing heard"); return; }
            if (path.Length > 0) SetFollowWalk(op, path); else { op.Walk = null; op.WalkMm = 0; }
        }

        /// <summary>In place for a settled Listen: in reach, no nearer than nearStill, and seated or by a wall out of the way (ListenInPlace's own test).</summary>
        bool SettledAt(Op op, Pt at, int reach) => G.Within(op.Pos, at, reach) && !G.Within(op.Pos, at, R.ListenNearStillCm)
            && (SeatOf(op) != null || (op.Walk == null && BesideSolid(op.Pos) && !S.Grid.Cuts(op.Pos, R.BodyMinCm, R.ListenPassCm)));

        /// <summary>A free chair for the rest of the act round someone who stays put (the 0.39.0 choice), in reach of a server's post too if one is to be had; walked to now.</summary>
        bool SettleChair(Op op, Order o, Pt at, Pt? post, int reach)
        {
            int speed = Math.Max(1, PaceOf(op, false)), until = op.BusyEnd + T(R.ListenChairMarginS);
            var c = (post.HasValue ? BestChair(op, at, reach, R.ListenNearStillCm, until, -1, speed, post) : null) ?? BestChair(op, at, reach, R.ListenNearStillCm, until, -1, speed);
            if (!c.HasValue && R.ListenEarshot) c = (post.HasValue ? BestChair(op, at, reach, R.ListenNearStillCm, until, -1, speed, post, inView: true) : null) ?? BestChair(op, at, reach, R.ListenNearStillCm, until, -1, speed, inView: true); // engine 0.56.0: she prefers a chair, in a camera's view too (the act holds every watcher's clock)
            if (!c.HasValue || SpotRoute(op.Pos, c.Value.seat, c.Value.stand) is not Path pc) return false;
            op.Spot = new ListenSpot { For = o, At = c.Value.seat, Stand = c.Value.stand, Chair = c.Value.chair, Chose = Tick, Until = op.BusyEnd };
            if (pc.Length > 0) SetFollowWalk(op, pc); else { op.Walk = null; op.WalkMm = 0; }
            return true;
        }

        /// <summary>The settle: a free chair (0.55.0, without earshot), else free ground by a wall, round someone who stays put (the 0.39.0 choice); walked to now.</summary>
        bool SettleSpot(Op op, Order o, Pt at, Pt? post, int reach)
        {
            if (!R.ListenEarshot && SettleChair(op, o, at, null, reach)) return true;
            int speed = Math.Max(1, PaceOf(op, false));
            var g = (R.ListenEarshot && post.HasValue ? BestGround(op, at, reach, R.ListenNearStillCm, -1, speed, post) : null) ?? BestGround(op, at, reach, R.ListenNearStillCm, -1, speed);
            if (g.HasValue && Walk.Route(op.Pos, g.Value) is Pt[] rg)
            { op.Spot = new ListenSpot { For = o, At = g.Value, Stand = g.Value, Chose = Tick, Until = op.BusyEnd }; var pg = new Path(rg); if (pg.Length > 0) SetFollowWalk(op, pg); return true; }
            return false;
        }

        /// <summary>Host read (never hashed, chooses nothing): the chair a Listen by this agent on this person would settle in now, were the person standing
        /// where they are (engine 0.56.0's order: out of every cone, else in view; in reach of a server's post too first), or null when none is free.</summary>
        public string? ListenChairFor(string agent, string role)
        {
            if (!R.ListenEarshot || !IsPersonRoleName(role)) return null;
            var op = OpOf(Sim.Op.Parse(agent)); var p = PersonByRole(role); if (PersonPos(p) is not Pt at) return null;
            int reach = ActReachOf(op, "listen"), speed = Math.Max(1, PaceOf(op, false)), until = Tick + T((int)R.Num("v:listen-s")) + T(R.ListenChairMarginS);
            Pt? post = ServerOf(p)?.At;
            var c = (post.HasValue ? BestChair(op, at, reach, R.ListenNearStillCm, until, -1, speed, post) : null) ?? BestChair(op, at, reach, R.ListenNearStillCm, until, -1, speed)
                ?? (post.HasValue ? BestChair(op, at, reach, R.ListenNearStillCm, until, -1, speed, post, inView: true) : null) ?? BestChair(op, at, reach, R.ListenNearStillCm, until, -1, speed, inView: true);
            return c?.chair;
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
