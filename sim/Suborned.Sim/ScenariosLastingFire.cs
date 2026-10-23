// Engine 0.21.0: the lasting fire order on the move (Sky 2026-10-22, playing 0.2.48; brief 041's last line: "Keep them firing until out of
// range I would say, same for running"). A lasting fire order (the commit-on-release fire, a lock, a focus) survives a walk and a run and
// fires on the move at the gait's chance (tuning.md §8.3, unchanged); it ends when its target is down, when the target leaves the weapon's max
// ring after the order has had it in reach and line (fire.out-of-range: no walk back in, no lapse), on cease (X), or on a new act
// (fire.cancelled). An order given out of reach still walks in first (brief 030), as before. Range cases under today and the lab (-LAB);
// the act case needs the Handover's people (LF-ACT).
using System;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The scout, drawn and holding (so only the order fires), with a standing fire order on one officer at the effective slot that
        /// has fired once (the order has had reach and line). Returns the scout.</summary>
        static Op LfSetup(Run r)
        {
            var g = r.G; r.Do("scout", "draw"); r.Wait(Game.T(2)); Hold(r, "scout"); Spawned(r, "police", "eff", 1);
            var o = r.Op("scout");
            r.Is(FireOn(r, "scout", "officer-1").ok, "fire on officer-1 taken standing");
            r.Is(r.Until(() => r.Shots().Any(x => x.by == "scout"), Game.T(5)), "the scout fires: the order has had reach and line");
            r.Is(o.Gun.Reached && o.Gun.Order == "officer-1", "the order is reached");
            return o;
        }
        /// <summary>A point `cm` from the scout, square to the line to the officer (`side` ±1), or straight away from it (`side` 0).</summary>
        static Pt LfPoint(Run r, Op o, int cm, int side)
        {
            var h = H(r, "officer-1").Pos(r.G.Tick); long dx = o.Pos.X - h.X, dy = o.Pos.Y - h.Y, n = Math.Max(1, G.Isqrt(dx * dx + dy * dy));
            if (side != 0) { long t = dx; dx = -dy * side; dy = t * side; }
            return new Pt(o.Pos.X + (int)(dx * cm / n), o.Pos.Y + (int)(dy * cm / n));
        }
        /// <summary>Steps until the scout's move ends (or `limit`), checking each tick that the order stands while the officer is in the max ring.
        /// Returns the scout's shots on officer-1 during the move.</summary>
        static int LfMoveKeeps(Run r, Op o, int limit)
        {
            var g = r.G; int t0 = g.Tick, end = g.Tick + limit; bool kept = true, inRing = true;
            while (o.Walk != null && g.Tick < end && !g.Over)
            {
                g.Step();
                inRing &= G.Within(o.Pos, H(r, "officer-1").Pos(g.Tick), o.Gun.W!.MaxCm);
                kept &= o.Gun.Order == "officer-1";
            }
            r.Is(o.Walk == null, $"the move ends ({Report.Clock(g.Tick)})");
            r.Is(inRing, "the officer stays inside the max ring the whole way");
            r.Is(kept, "the order stands every tick of the move");
            return r.Shots().Count(x => x.by == "scout" && x.target == "officer-1" && x.tick > t0 && x.tick <= g.Tick);
        }
        /// <summary>Steps until the scout's distance to the officer passes the max ring; checks the order ends that tick with fire.out-of-range,
        /// the move goes on to its own end with no walk back in, and nothing more is fired.</summary>
        static void LfLeaves(Run r, Op o, Pt dest)
        {
            var g = r.G; int max = o.Gun.W!.MaxCm, outAt = -1;
            for (int k = 0; k < Game.T(30) && outAt < 0 && !g.Over; k++)
            {
                g.Step();
                if (!G.Within(o.Pos, H(r, "officer-1").Pos(g.Tick), max)) outAt = g.Tick;
                else if (o.Gun.Order != "officer-1") { r.Is(false, $"the order ended in range at {Report.Clock(g.Tick)}"); return; }
            }
            if (!r.Is(outAt >= 0, $"the scout passes the {max / 100} m ring")) return;
            int end = g.Log.FindLastIndex(l => l.Code == "fire.out-of-range");
            r.Is(end >= 0 && g.Log[end].Tick == outAt && o.Gun.Order.Length == 0, $"the order ends the tick the officer leaves the ring ({Report.Clock(outAt)}), logged fire.out-of-range");
            r.Is(!g.Log.Exists(l => l.Code == "fire.lapses"), "it ends at once, no lapse");
            r.Is(!o.CloseWalk && o.Walk != null && o.Walk.End == dest, "the move goes on to its own end: no walk back in");
            r.Until(() => o.Walk == null, Game.T(30)); r.Wait(Game.T(4));
            r.Is(!r.Shots().Any(x => x.by == "scout" && x.tick > outAt), "nothing more is fired");
            r.Is(o.Gun.Order.Length == 0 && !o.CloseWalk, "and the order stays ended");
            r.Is(FireOn(r, "scout", "officer-1").ok && o.CloseWalk, "a new fire order out of reach walks in, as brief 030");
        }

        static void LastingFireRange()
        {
            foreach (var preset in new[] { "today", "lab" })
            {
                string sfx = preset == "lab" ? "-LAB" : "";
                AddRange("LF-WALK" + sfx, preset, "open", "Sky 2026-10-22: a lasting fire order keeps firing through a walk, at the walk's chance, while the target stays in range", r =>
                {
                    var o = LfSetup(r);
                    r.Is(Move(r, "scout", LfPoint(r, o, -700, 0)).ok, "a 7 m walk taken toward the officer");
                    r.Is(o.Walk != null && !o.Running, "it walks");
                    ShotAtGait(r, "scout", "officer-1", Gait.Walk, () => o.Walk == null);
                    int shots = LfMoveKeeps(r, o, Game.T(10));
                    r.Is(shots >= 1, $"it fires on the walk ({shots} shots)");
                    r.Eq(o.Gun.Order, "officer-1", "the order stands when the walk ends");
                    r.Is(!r.G.Log.Exists(l => l.Code == "fire.cancelled" || l.Code == "fire.out-of-range" || l.Code == "fire.lapses"), "nothing ended it");
                });
                AddRange("LF-RUN" + sfx, preset, "open", "Sky 2026-10-22 (\"same for running\"): a lasting fire order keeps firing through a run, at the run's chance, while the target stays in range", r =>
                {
                    var o = LfSetup(r);
                    r.Is(Move(r, "scout", LfPoint(r, o, -800, 0), run: true).ok, "an 8 m run taken toward the officer (head first)");
                    r.Is(o.Walk != null && o.Running, "it runs");
                    ShotAtGait(r, "scout", "officer-1", Gait.Run, () => o.Walk == null);
                    int shots = LfMoveKeeps(r, o, Game.T(10));
                    r.Is(shots >= 1, $"it fires on the run ({shots} shots)");
                    r.Eq(o.Gun.Order, "officer-1", "the order stands when the run ends");
                    r.Is(!r.G.Log.Exists(l => l.Code == "fire.cancelled" || l.Code == "fire.out-of-range" || l.Code == "fire.lapses"), "nothing ended it");
                });
                AddRange("LF-OUT" + sfx, preset, "open", "Sky 2026-10-22: a walk that takes the target out of the weapon's max ring ends the lasting order there (fire.out-of-range): no walk back in, no lapse", r =>
                {
                    var o = LfSetup(r);
                    var dest = LfPoint(r, o, 1400, 1);
                    r.Is(Move(r, "scout", dest).ok, "a 14 m walk across the line of fire taken: it ends past the max ring");
                    LfLeaves(r, o, dest);
                });
                AddRange("LF-RUN-OUT" + sfx, preset, "open", "Sky 2026-10-22: a run that takes the target out of the weapon's max ring ends the lasting order there (fire.out-of-range)", r =>
                {
                    var o = LfSetup(r);
                    var dest = LfPoint(r, o, 1400, 1);
                    r.Is(Move(r, "scout", dest, run: true).ok, "a 14 m run across the line of fire taken: it ends past the max ring");
                    LfLeaves(r, o, dest);
                });
            }
        }

        /// <summary>The Handover case: an act ends the lasting fire order (it no longer waits through the act and resumes after it).</summary>
        static void LastingFireCases()
        {
            Add("LF-ACT", "—", "Sky 2026-10-22: a new act ends a lasting fire order (fire.cancelled); the agent does not resume firing after the act", r =>
            {
                var g = r.G; var o = r.Op(Scout);
                r.To(r.Send(Scout, "beside-inez"));
                r.Fire(Scout, Inez, force: true);
                r.Is(o.Gun.Order == Inez, "the fire order stands (the scout draws first)");
                int t0 = g.Tick;
                r.Is(r.Do(Scout, "watch", Inez), "a Watch on Inez is taken while the order stands");
                r.Is(o.Gun.Order.Length == 0, "the act ends the fire order");
                int l = g.Log.FindLastIndex(x => x.Code == "fire.cancelled");
                r.Is(l >= 0 && g.Log[l].Tick == t0, "logged fire.cancelled the tick of the act");
                r.Done(Scout); r.Wait(Game.T(5));
                r.Is(o.Gun.Order.Length == 0, "after the Watch the order is still gone");
                r.Is(!r.Shots().Any(x => x.by == Scout), "and the scout never fired");
            });
        }
    }
}
