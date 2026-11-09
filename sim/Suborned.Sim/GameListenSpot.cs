// Engine 0.39.0 (brief 085; Sky: "when Scouts go listen, they should sit down at cafe, or stand by a wall", "Listening shouldn't get noticed, scout is a
// scout", "We could have Inez walk near scout and talk to another NPC"; Motive, briefs/085/motive/discreet-listen.md; rulebook cell.listen). A Listen given
// out of place walks to a spot, not to the person: for anyone, on any mission. Someone with a known walk (PoseAhead): read ahead along it, take the earliest
// moment the agent can already stand in place, then the free chair nearest the agent if one comes up within cell.listen.chairGrace, else free ground beside
// something solid on a clear line, out of every watcher's cone. Someone who stays put: the same choice now, nearStill or farther off. Nothing found: the old
// walk in. The agent waits at the spot (the attention clock holds, GameAttention.AttnInAct); the act starts when the person is within reach.
// Engine 0.49.0 (brief 098, Sky 2026-11-08: "Sit them down if they're listening, they're just regular people"): sitting is the strong default. A free
// chair comes first whenever one comes up in the look-ahead (cell.listen.chairGrace is the whole of it), walking a little further or waiting for one;
// the agent walks to the chair's free ground and sits in it, as a café customer does (its spot is the seat), and a seated agent is a body that keeps
// its chair (GameBodies.cs). With no seat it stands by a wall (cell.listen.wall) and never across a passage (cell.listen.pass, Grid.Cuts).
// The choice is made only by the engine's walk (ListenWalk); the host's reads (ListenSpotNow, ListenSpotAhead, SeatedAt) choose nothing and hash nothing.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>The spot an agent's Listen walks to: for which order, where it stands (engine 0.49.0: a chair's seat), the chair (the people file's place
    /// name; "" on the ground), when it was chosen, until when it stands (engine 0.49.0: the moment it was chosen for and cell.listen.hold), and the chair's
    /// free ground its walk leaves the street at (At on the ground).</summary>
    public sealed class ListenSpot { public Order For = null!; public Pt At, Stand; public string Chair = ""; public int Chose, Until; }

    public sealed partial class Game
    {
        bool listenArriving; // on ApproachStep's call of Scout: the act was in place, so reach alone is asked again
        int aheadTick = -1; Dictionary<string, (Pt at, string chair, int walkT, int startT)?>? aheadCache; // the card's read, a tick at a time; never read by the engine's own choice

        bool ListenSpots(Op op, string verb) => verb == "listen" && R.ListenSpotOn && WalksIn(op);

        /// <summary>The op's spot while its order stands: walking in, waiting or at the act. Null otherwise.</summary>
        ListenSpot? SpotOf(Op op) => op.Spot != null && (ReferenceEquals(op.Spot.For, op.Act) || ReferenceEquals(op.Spot.For, op.Busy)) ? op.Spot : null;

        /// <summary>A Listen's place (rulebook cell.listen): at its spot and in reach while it walks in; with no spot, in reach; given afresh, in reach, no nearer than
        /// nearStill and beside something solid, so an agent placed well stays put.</summary>
        bool ListenInPlace(Op op, Pt at, int reach)
        {
            if (!G.Within(op.Pos, at, reach)) return false;
            if (listenArriving) return true;
            if (op.Act != null && op.Act.Do == "listen") return SpotOf(op) is not ListenSpot sp || (sp.Chair.Length > 0 ? op.Pos == sp.At : G.Within(op.Pos, sp.At, R.ListenAtSpotCm) && (op.Pos == sp.At || !WatchedAt(op.Pos))); // engine 0.49.0: a chair is sat in, not stood by; short of a spot on the ground, only out of every cone (the spot was chosen out of them, not the ground round it)
            if (G.Within(op.Pos, at, R.ListenNearStillCm)) return false;
            return SeatOf(op) != null || (BesideSolid(op.Pos) && !S.Grid.Cuts(op.Pos, R.BodyMinCm, R.ListenPassCm)); // engine 0.49.0: seated, or by a wall and out of the way
        }

        /// <summary>Is the agent waiting at a Listen's spot for the person to come: the attention clock holds on it.</summary>
        bool ListenWaiting(Op op) => op.Act != null && op.Act.Do == "listen" && op.Walk == null && SpotOf(op) is ListenSpot sp && (sp.Chair.Length > 0 ? op.Pos == sp.At : G.Within(op.Pos, sp.At, R.ListenAtSpotCm));

        /// <summary>The walk in for a Listen: to its spot, held there up to cell.listen.hold from the choice, then chosen afresh; no spot to be had, the old walk in.</summary>
        Path? ListenWalk(Op op, Person p, Order o)
        {
            int reach = ActReachOf(op, "listen");
            if (op.Spot != null && ReferenceEquals(op.Spot.For, o))
            {
                var have = op.Spot;
                if (have.Chair.Length > 0 ? op.Pos != have.At : !G.Within(op.Pos, have.At, R.ListenAtSpotCm)) { if (SpotRoute(op.Pos, have.At, have.Stand) is Path r0) return r0; }
                else if (Tick < have.Until) return new Path(new[] { op.Pos, op.Pos });
            }
            op.Spot = null;
            if (!PersonPos(p).HasValue) return Intercept(op, p, reach)?.path;
            if (ListenPick(op, p, reach) is not (Pt at, string chair, Path path, int startT, Pt stand)) return Intercept(op, p, reach)?.path;
            int walkT = (int)((path.Length + Math.Max(1, PaceOf(op, false)) - 1) / Math.Max(1, PaceOf(op, false)));
            op.Spot = new ListenSpot { For = o, At = at, Stand = stand, Chair = chair, Chose = Tick, Until = Tick + Math.Max(walkT, startT) + T(R.ListenHoldS) }; // engine 0.49.0: it stands until the moment it was chosen for and the hold (a chair may come up later in her walk)
            return path;
        }
        /// <summary>The walk to a spot: to it on free ground; to a chair, to its free ground and the step into the seat across the last of the solid ground
        /// (as a café customer's walk ends at the chair, Crowd.Leg); seated already, none.</summary>
        Path? SpotRoute(Pt from, Pt at, Pt stand)
        {
            if (from == at) return new Path(new[] { from, from });
            if (at == stand) return Walk.Route(from, at) is Pt[] r ? new Path(r) : null;
            if (!Walk.Free(from) && G.Within(from, at, StepInCm)) return new Path(new[] { from, at }); // on its last step in already (re-aimed there): straight on into the seat
            var w = from == stand ? new[] { from } : Walk.Route(from, stand); if (w == null) return null;
            var l = new Pt[w.Length + 1]; Array.Copy(w, l, w.Length); l[w.Length] = at; return new Path(l);
        }

        /// <summary>The choice itself: read only. startT: when the act can start, from now; stand: where its walk leaves free ground (a chair's), else at.</summary>
        (Pt at, string chair, Path path, int startT, Pt stand)? ListenPick(Op op, Person p, int reach)
        {
            int speed = Math.Max(1, PaceOf(op, false)), listen = T((int)R.Ev("scout.listen").Price[0].SV);
            if (ServerOf(p) == null)
            {
                var her = PersonPos(p); if (!her.HasValue) return null;
                var c0 = BestChair(op, her.Value, reach, R.ListenNearStillCm, Tick + listen + T(R.ListenChairMarginS), -1, speed);
                if (c0.HasValue && SpotRoute(op.Pos, c0.Value.seat, c0.Value.stand) is Path p0) return (c0.Value.seat, c0.Value.chair, p0, (int)((p0.Length + speed - 1) / speed), c0.Value.stand);
                var q = BestGround(op, her.Value, reach, R.ListenNearStillCm, -1, speed);
                if (!q.HasValue || Walk.Route(op.Pos, q.Value) is not Pt[] rt) return null;
                var pa = new Path(rt); return (q.Value, "", pa, (int)((pa.Length + speed - 1) / speed), q.Value);
            }
            (Pt at, string chair, Path path, int startT, Pt stand)? ground = null; int groundS = -1;
            for (int s = 0; s <= R.ListenLookS; s += R.ListenStepS)
            {
                if (ground.HasValue && s > groundS + R.ListenChairGraceS) break;
                var her = s == 0 ? PersonPos(p) : PoseAhead(p, T(s))?.pos; if (!her.HasValue) continue;
                int t = T(s);
                var c = BestChair(op, her.Value, reach, R.ListenNearWalkingCm, Tick + t + listen + T(R.ListenChairMarginS), t, speed);
                if (c.HasValue && SpotRoute(op.Pos, c.Value.seat, c.Value.stand) is Path pc && (pc.Length + speed - 1) / speed <= t) return (c.Value.seat, c.Value.chair, pc, t, c.Value.stand);
                if (ground.HasValue) continue;
                var g = BestGround(op, her.Value, reach, R.ListenNearWalkingCm, t, speed);
                if (g.HasValue && Walk.Route(op.Pos, g.Value) is Pt[] rg) { var pg = new Path(rg); if ((pg.Length + speed - 1) / speed <= t) { ground = (g.Value, "", pg, t, g.Value); groundS = s; } }
            }
            return ground;
        }

        /// <summary>The free chair nearest the agent whose seat is in the ring round the person, its free ground within chairGround of it, out of every cone,
        /// free through the act (no walker's stop holds it, nobody else of the cell has it, nobody else sits in it); within `t` ticks' straight walk unless t is -1.</summary>
        (Pt seat, Pt stand, string chair)? BestChair(Op op, Pt her, int reach, int nearest, int until, int t, int speed)
        {
            if (People2 == null) return null;
            (Pt, Pt, string)? best = null; long bd = long.MaxValue; string me = Sim.Op.Name(op.Id);
            foreach (var name in People2.Chairs)
            {
                var seat = People2.Places[name]; var st = Walk.Free(seat) ? seat : Walk.NearestFree(seat);
                if (!st.HasValue || G.Dist(st.Value, seat) > R.ListenChairGroundCm || !G.Within(seat, her, reach) || G.Within(seat, her, nearest)) continue;
                if (t >= 0 && G.Dist(op.Pos, seat) * 10L > speed * (long)t) continue;
                if (!ChairFree(name, Tick, until) || (seatHeld.TryGetValue(name, out var who) && who != me) || ChairOfCell(name, op) || WatchedAt(seat)) continue;
                long d = G.D2(op.Pos, seat); if (d < bd) { bd = d; best = (seat, st.Value, name); }
            }
            return best;
        }
        /// <summary>Another operative's Listen has this chair, or another sits in it.</summary>
        bool ChairOfCell(string chair, Op me) { foreach (var o in Ops) if (o != me && ((SpotOf(o) is ListenSpot sp && sp.Chair == chair) || SeatOf(o) == chair)) return true; return false; }

        /// <summary>The free ground by a wall nearest the agent, in the ring round the person, on a clear line to them, out of every cone, and never across a
        /// passage (engine 0.49.0: with its body there, the ground round it stays as joined as it was: Grid.Cuts). Tried every GroundStepCm, nearest first.</summary>
        Pt? BestGround(Op op, Pt her, int reach, int nearest, int t, int speed)
        {
            groundTry.Clear();
            for (int dx = -reach; dx <= reach; dx += GroundStepCm) for (int dy = -reach; dy <= reach; dy += GroundStepCm)
                {
                    var q = new Pt(her.X + dx, her.Y + dy);
                    if (!S.Grid.Free(q) || !G.Within(q, her, reach) || G.Within(q, her, nearest) || !BesideSolid(q)) continue;
                    if (t >= 0 && G.Dist(op.Pos, q) * 10L > speed * (long)t) continue;
                    groundTry.Add((G.D2(op.Pos, q), q));
                }
            groundTry.Sort((a, b) => a.d != b.d ? a.d.CompareTo(b.d) : a.q.X != b.q.X ? a.q.X.CompareTo(b.q.X) : a.q.Y.CompareTo(b.q.Y));
            foreach (var (_, q) in groundTry) if (Sees(q, her) && !WatchedAt(q) && !S.Grid.Cuts(q, R.BodyMinCm, R.ListenPassCm)) return q;
            return null;
        }
        readonly List<(long d, Pt q)> groundTry = new List<(long, Pt)>();
        /// <summary>The ground a Listen tries, on a lattice round the person this fine (cm): finer than cell.listen.wall, so a wall's edge is never stepped over.</summary>
        const int GroundStepCm = 20;

        /// <summary>By a wall (engine 0.49.0, cell.listen.wall): a solid within `wall` of where its body keeps clear of one, any of eight ways. Was (0.39.0 to
        /// 0.48.0): a blocked 50 cm cell next to it.</summary>
        public bool BesideSolid(Pt q)
        {
            int c = R.ListenWallCm, k = c * 707 / 1000;
            return !S.Grid.Free(q + new Pt(c, 0)) || !S.Grid.Free(q - new Pt(c, 0)) || !S.Grid.Free(q + new Pt(0, c)) || !S.Grid.Free(q - new Pt(0, c))
                || !S.Grid.Free(q + new Pt(k, k)) || !S.Grid.Free(q + new Pt(k, -k)) || !S.Grid.Free(q + new Pt(-k, k)) || !S.Grid.Free(q + new Pt(-k, -k));
        }

        /// <summary>The chair an operative sits in (engine 0.49.0): it stands at a chair's seat with no walk (its walk took it in there), on the street.</summary>
        internal string? SeatOf(Op op)
        {
            if (People2 == null || op.Walk != null || op.Inside || op.InCar || op.Down || op.CarriedBy.HasValue || op.LeftMap) return null;
            if (seatAt == null) { var d = new Dictionary<Pt, string>(); foreach (var c in People2.Chairs) d.TryAdd(People2.Places[c], c); seatAt = d; }
            return seatAt.TryGetValue(op.Pos, out var name) ? name : null;
        }
        Dictionary<Pt, string>? seatAt;
        /// <summary>The chair an operative is taking his last step into (his Listen's seat, his walk's end within StepInCm): he holds it already, and the
        /// law lets him walk his own walk in across the chair's ground (as a café customer does), never stepping him back off it (GameBodyForms.cs).</summary>
        string? SittingInto(Op op) => op.Walk != null && SpotOf(op) is ListenSpot sp && sp.Chair.Length > 0 && op.Walk.End == sp.At && G.Within(op.Pos, sp.At, StepInCm) ? sp.Chair : null;
        /// <summary>Host read (never hashed): the chair an operative sits in now and the way it faces (the chair's own), or null. Latch draws it seated.</summary>
        public (string chair, Pt face)? SeatedAt(string agent) { var op = OpOf(Sim.Op.Parse(agent)); return SeatOf(op) is string c ? (c, StopFace(c) ?? default) : ((string, Pt)?)null; }
        internal bool WatchedAt(Pt q)
        {
            if (GuardWatching(q, false) || PatrolWatching(q, false)) return true;
            foreach (var c in S.Cameras) if (CameraWatching(c.Id, q, false)) return true;
            return false;
        }
        internal Pt? ServerFace(Person p) => ServerOf(p)?.Face;
        bool ChairFree(string name, int from, int to)
        {
            foreach (var w in Walkers) for (int i = 0; i < w.Place.Length; i++) if (w.Place[i] == name && w.From[i] < to && w.To[i] > from) return false;
            foreach (var c in S.Cast) if (People2 != null && People2.Stops.TryGetValue(c.Id, out var cs)) foreach (var (place, f, t, _) in cs) if (place == name && f < to && t > from) return false; // brief 098 L9: a chair a cast member's stops name over the stay (the cast are played by role, not as walkers)
            return true;
        }

        // ---------------------------------------------------------------- the host's reads: read only, never hashed

        /// <summary>The spot this agent's Listen holds now, walking in, waiting or at the act; null if it holds none. At: where it stands (engine 0.49.0: a chair's seat; was its walkable point, about 1 m off).
        /// Chair: the chair's place name, or "". Waiting: at the spot, the person not yet in reach.</summary>
        public (Pt at, string chair, string target, bool waiting)? ListenSpotNow(string agent)
        {
            var op = OpOf(Sim.Op.Parse(agent)); return SpotOf(op) is ListenSpot sp ? (sp.At, sp.Chair, sp.For.Target, ListenWaiting(op)) : ((Pt, string, string, bool)?)null;
        }

        /// <summary>The spot a Listen given now would take, for the card; null when it would use none (in place already, no spot to be had, the rule off). WalkS: the walk to it.
        /// StartS: until the act can start (the walk, or the wait for the person). The same choice the engine would make on this tick, kept for the tick (the host asks often).</summary>
        public (Pt at, string chair, int walkS, int startS)? ListenSpotAhead(string agent, string role)
        {
            if (!IsPersonRoleName(role)) return null;
            var v = AheadRead(OpOf(Sim.Op.Parse(agent)), PersonByRole(role)); if (!v.HasValue) return null;
            return (v.Value.at, v.Value.chair, (v.Value.walkT + Hz - 1) / Hz, (v.Value.startT + Hz - 1) / Hz);
        }
        (Pt at, string chair, int walkT, int startT)? AheadRead(Op op, Person p)
        {
            if (!ListenSpots(op, "listen") || !PersonPos(p).HasValue || ListenInPlace(op, PersonPos(p)!.Value, ActReachOf(op, "listen"))) return null;
            if (aheadTick != Tick || aheadCache == null) { aheadTick = Tick; aheadCache = new Dictionary<string, (Pt, string, int, int)?>(StringComparer.Ordinal); }
            string key = (int)op.Id + "/" + p.Role;
            if (aheadCache.TryGetValue(key, out var hit)) return hit;
            (Pt, string, int, int)? read = null;
            if (ListenPick(op, p, ActReachOf(op, "listen")) is (Pt at, string chair, Path path, int startT, Pt _))
            { int speed = Math.Max(1, PaceOf(op, false)); int walk = (int)((path.Length + speed - 1) / speed); read = (at, chair, walk, StartsAt(p, at, walk, Math.Max(walk, startT), ActReachOf(op, "listen"))); }
            aheadCache[key] = read; return read;
        }
        /// <summary>When a Listen walked to its spot starts (engine 0.52.0, the card's read): the agent there from `walk` on, the first tick the person is
        /// within reach of it (ListenInPlace), at the latest the moment the spot was chosen for, when her pose is in its ring. Brief 101 (N130): a chair
        /// chosen for her walk home 12 s ahead starts when the Scout sits in it, 9 s, as she still serves beside it; the card said 12.</summary>
        int StartsAt(Person p, Pt at, int walk, int by, int reach)
        {
            for (int t = walk; t < by; t++) if (PoseAhead(p, t)?.pos is Pt q && G.Within(q, at, reach)) return t;
            return by;
        }
    }
}
