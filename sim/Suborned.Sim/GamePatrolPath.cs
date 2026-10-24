// The patrol's path as the engine moves it (brief 057 §2, engine 0.30.0): the reads for the host to draw the patrol's real route, its sweep
// and, after a Reroute, its walk to the wait spot, the wait and its walk back, all inside the false orders (GameRerouteWalk.cs). Read-only: built from the state
// PatrolPose reads, never hashed, never read by a rule. Every point is block cm, in walk order from where the robot stands now, with the tick
// it is there. A leg the engine moves the robot along (Walk true) is a polyline: PatrolPose().pos lies on it at every tick the leg covers
// (the sweep's straight line between the site's patrol-a and patrol-b, the reroute's grid walks, a chase's or an alarm's grid route). A leg
// with Walk false is a stand (a hold at one point: the wait spot, waiting for its sweep, engaging) or, on inputs without cell.reroute.walk,
// a placement (0.29.0: the engine put it at the wait spot and back at once).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>One leg of the patrol's path (Game.PatrolLegs).</summary>
    public sealed class PatrolLeg
    {
        /// <summary>sweep (its entrance sweep, back and forth; repeats with no end), out (the Reroute's walk to the wait spot), wait (at the wait
        /// spot, or the point it turns back at, until it starts back), back (the walk back to its sweep), meet (on its sweep's point, waiting for
        /// the false orders to end),
        /// chase (its grid route to the shot's origin or back to its sweep), alarm (answering an alarm on the response's route), hold (standing
        /// where it is: engaging, or at an alarm's end). Without cell.reroute.walk: away (placed at the wait spot) and back as placements.</summary>
        public string Kind = "";
        /// <summary>True: the engine moves the robot along Points (a chase steps a second's chase at a time, on the whole second). False: it
        /// stands at Points[0] from From to Until (or is placed there, on 0.29.0 inputs).</summary>
        public bool Walk;
        /// <summary>Block cm, in walk order; a walked leg starts where the robot will be at From.</summary>
        public readonly List<Pt> Points = new List<Pt>();
        /// <summary>The tick the robot is at each point (Points[i] at Ticks[i]); a chase's are estimates (it re-plans every second).</summary>
        public readonly List<int> Ticks = new List<int>();
        /// <summary>The tick the leg starts, and the tick it ends as the engine has it now (-1: no end the engine knows: a sweep repeats, a
        /// chase or an alarm ends on an event).</summary>
        public int From, Until = -1;
        public override string ToString() => $"{Kind}{(Walk ? "" : "!")}[{string.Join(" ", Points)}]{From}..{Until}";
    }

    /// <summary>The patrol's path from now on (Game.PatrolLegs).</summary>
    public sealed class PatrolPlan
    {
        /// <summary>sweep, away (on false orders: its walk out, its wait, its walk back), chase, alarm, down.</summary>
        public string Mode = "";
        /// <summary>Where the robot stands now (PatrolPose().pos), block cm.</summary>
        public Pt At;
        /// <summary>The sweep's ends (the site's patrol-a and patrol-b stands, as SweepPos reads them) and the Reroute's wait spot (patrol-reroute).</summary>
        public Pt SweepA, SweepB, Wait;
        /// <summary>The false orders' end (PatrolAwayUntil) and the tick it is back on its sweep (the same tick: the walk back lands then), -1 when not rerouted.</summary>
        public int AwayUntil = -1, BackAt = -1;
        public readonly List<PatrolLeg> Legs = new List<PatrolLeg>();
        public override string ToString() => $"{Mode}@{At}: {string.Join(" ; ", Legs)}";
    }

    public sealed partial class Game
    {
        /// <summary>The patrol's path from now on as one polyline (brief 057 §2, engine 0.30.0): every leg's points in order (PatrolLegs), the
        /// sweep's last. On its sweep: where it stands, then the sweep's two ends. Rerouted: the walk out, the wait spot, the walk back and the
        /// sweep. Null when it is down. Read-only.</summary>
        public Pt[]? PatrolRoute()
        {
            var plan = PatrolLegs(); if (plan.Mode == "down") return null;
            var l = new List<Pt>();
            foreach (var leg in plan.Legs) foreach (var p in leg.Points) if (l.Count == 0 || l[l.Count - 1] != p) l.Add(p);
            if (l.Count == 1) l.Add(l[0]);
            return l.ToArray();
        }

        /// <summary>The patrol's path from now on, leg by leg, as the engine will move it (brief 057 §2, engine 0.30.0). Read-only: no state,
        /// no hash moves. Down: Mode "down", no legs. On its sweep: one sweep leg, from where it stands to the end it walks to, then the other
        /// end. Rerouted (cell.reroute.walk; all inside the false orders): out (the walk to the wait spot; gone once it is there), wait (until it
        /// starts back), back (the walk to the point of its sweep SweepPos has at PatrolAwayUntil), meet (if it lands early), then the sweep from
        /// PatrolAwayUntil. Chasing: the grid route PatrolChase walks from its chase point to the shot's origin (or back to the sweep, then the sweep),
        /// or a hold while it engages. Answering an alarm: the rest of the unit's route, then a hold at its end.</summary>
        public PatrolPlan PatrolLegs()
        {
            var r = new PatrolPlan { SweepA = S.Stand(S.PatrolA), SweepB = S.Stand(S.PatrolB), Wait = S.Stand(S.PatrolReroute) };
            var pose = PatrolPose(); if (pose.HasValue) r.At = pose.Value.pos;
            if (PatrolDown || !pose.HasValue) { r.Mode = "down"; return r; }
            if (PatrolAwayUntil > Tick) { r.AwayUntil = PatrolAwayUntil; r.BackAt = PatrolAwayUntil; }
            if (PatrolUnit() is Unit u && UnitPose(u).HasValue)
            {
                r.Mode = "alarm";
                if ((u.St == "walk" || u.St == "back") && u.Route != null)
                {
                    long mm = Math.Max(0, (long)(Tick - u.Launch) * u.Speed);
                    if (mm < u.Route.Length)
                    {
                        var leg = new PatrolLeg { Kind = "alarm", Walk = true, From = Tick, Until = u.Launch + (int)((u.Route.Length + u.Speed - 1) / u.Speed) };
                        AddWalk(leg, u.Route, mm, u.Launch, u.Speed); r.Legs.Add(leg);
                    }
                }
                Hold(r, "hold", r.At, Tick, -1);
                return r;
            }
            if (PatrolAwayUntil > Tick)
            {
                r.Mode = "away";
                if (RerouteWalkPlan() == null) // 0.29.0 inputs: placed at the wait spot, and back on the sweep at once
                {
                    Hold(r, "away", r.Wait, Tick, PatrolAwayUntil);
                    Hold(r, "back", SweepPos(PatrolAwayUntil), PatrolAwayUntil, PatrolAwayUntil);
                    r.Legs.Add(SweepLeg(PatrolAwayUntil));
                    return r;
                }
                var (ow, turn, outAt, leave, bw, v) = RerouteWalkPlan()!.Value;
                long outMm = Math.Min((long)(Tick - outAt) * v, turn); int reach = outAt + (int)((turn + v - 1) / v);
                if (Tick < leave && outMm < turn)
                {
                    var o = new PatrolLeg { Kind = "out", Walk = true, From = Tick, Until = reach };
                    AddWalk(o, ow.Cut(turn), outMm, outAt, v); r.Legs.Add(o);
                }
                if (Tick < leave && reach < leave) Hold(r, "wait", ow.At(turn), Math.Max(Tick, reach), leave);
                long bm = Math.Max(0, (long)(Tick - leave) * v); int arrive = leave + (int)((bw.Length + v - 1) / v);
                if (bm < bw.Length)
                {
                    var b = new PatrolLeg { Kind = "back", Walk = true, From = Math.Max(Tick, leave), Until = arrive };
                    AddWalk(b, bw, bm, leave, v); r.Legs.Add(b);
                }
                if (arrive < PatrolAwayUntil) Hold(r, "meet", bw.End, Math.Max(Tick, arrive), PatrolAwayUntil);
                r.Legs.Add(SweepLeg(PatrolAwayUntil));
                return r;
            }
            if (PatrolChasing)
            {
                r.Mode = "chase";
                if (PatrolGun.Target.Length > 0 && !patrolReturning) { Hold(r, "hold", r.At, Tick, -1); return r; } // engaging: it stops (PatrolChase)
                int next = Tick % Hz == 0 ? Tick : Tick + Hz - Tick % Hz; // PatrolChase moves on the whole second
                var target = patrolReturning ? SweepPos(next) : patrolOrigin;
                var t = S.Grid.NearestFree(target); var from = S.Grid.NearestFree(chasePos);
                var route = t.HasValue && from.HasValue ? S.Grid.Route(from.Value, t.Value) : null;
                if (route == null) { Hold(r, "hold", r.At, Tick, -1); return r; }
                var leg = new PatrolLeg { Kind = "chase", Walk = true, From = Tick };
                var pts = new List<Pt> { chasePos }; foreach (var p in route) if (pts[pts.Count - 1] != p) pts.Add(p);
                var path = new Path(pts.ToArray()); long step = Math.Max(1, (long)chaseSpeed * Hz);
                AddWalk(leg, path, 0, next - Hz, chaseSpeed); // a second's chase a step, from `next`: about the walk's own pace
                r.Legs.Add(leg);
                if (patrolReturning) // back to its sweep: on it about steps − 1 seconds after `next` (within 1 m of the moving sweep point: an estimate)
                {
                    long steps = Math.Max(1, (path.Length + step - 1) / step);
                    leg.Until = next + Hz * (int)Math.Min(int.MaxValue / 4 / Hz - 1, steps - 1); r.Legs.Add(SweepLeg(leg.Until));
                }
                return r;
            }
            r.Mode = "sweep";
            r.Legs.Add(SweepLeg(Tick));
            return r;
        }

        /// <summary>A walked leg's points from mm on, each with the tick it is reached at `perTick` mm a tick from `start`.</summary>
        static void AddWalk(PatrolLeg leg, Path p, long mm, int start, long perTick)
        {
            perTick = Math.Max(1, perTick); long acc = 0;
            leg.Points.Add(p.At(mm)); leg.Ticks.Add(start + (int)((mm + perTick - 1) / perTick));
            for (int i = 1; i < p.Points.Length; i++)
            {
                acc += G.Isqrt(G.D2(p.Points[i - 1], p.Points[i]) * 100);
                if (acc > mm && leg.Points[leg.Points.Count - 1] != p.Points[i]) { leg.Points.Add(p.Points[i]); leg.Ticks.Add(start + (int)((acc + perTick - 1) / perTick)); }
            }
        }

        /// <summary>The sweep from tick t: from SweepPos(t) to the end it walks toward, then back to the other end (SweepPos's ping-pong).</summary>
        PatrolLeg SweepLeg(int t)
        {
            Pt a = S.Stand(S.PatrolA), b = S.Stand(S.PatrolB);
            int period = T(R.PatrolPeriodS), half = period / 2, ph = t % period;
            bool toB = ph < half; // SweepPos: A at phase 0, B at half, back to A at period
            var leg = new PatrolLeg { Kind = "sweep", Walk = true, From = t };
            var at = SweepPos(t); leg.Points.Add(at); leg.Ticks.Add(t);
            int firstAt = toB ? t + (half - ph) : t + (period - ph);
            var first = toB ? b : a; var second = toB ? a : b;
            if (first != at) { leg.Points.Add(first); leg.Ticks.Add(firstAt); }
            leg.Points.Add(second); leg.Ticks.Add(firstAt + half);
            return leg;
        }
        static void Hold(PatrolPlan r, string kind, Pt at, int from, int until)
        {
            var h = new PatrolLeg { Kind = kind, Walk = false, From = from, Until = until }; h.Points.Add(at); h.Ticks.Add(from); r.Legs.Add(h);
        }
    }
}
