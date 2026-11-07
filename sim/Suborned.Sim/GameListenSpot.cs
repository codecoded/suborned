// Engine 0.39.0 (brief 085; Sky: "when Scouts go listen, they should sit down at cafe, or stand by a wall", "Listening shouldn't get noticed, scout is a
// scout", "We could have Inez walk near scout and talk to another NPC"; Motive, briefs/085/motive/discreet-listen.md; rulebook cell.listen). A Listen given
// out of place walks to a spot, not to the person: for anyone, on any mission. Someone with a known walk (PoseAhead): read ahead along it, take the earliest
// moment the agent can already stand in place, then the free chair nearest the agent if one comes up within cell.listen.chairGrace, else free ground beside
// something solid on a clear line, out of every watcher's cone. Someone who stays put: the same choice now, nearStill or farther off. Nothing found: the old
// walk in. The agent waits at the spot (the attention clock holds, GameAttention.AttnInAct); the act starts when the person is within reach.
// The choice is made only by the engine's walk (ListenWalk); the host's reads (ListenSpotNow, ListenSpotAhead) choose nothing and hash nothing.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>The spot an agent's Listen walks to: for which order, where it stands, the chair (the people file's place name; "" on the ground), when it was chosen.</summary>
    public sealed class ListenSpot { public Order For = null!; public Pt At; public string Chair = ""; public int Chose; }

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
            if (op.Act != null && op.Act.Do == "listen") return SpotOf(op) is not ListenSpot sp || G.Within(op.Pos, sp.At, R.ListenAtSpotCm);
            return !G.Within(op.Pos, at, R.ListenNearStillCm) && BesideSolid(op.Pos);
        }

        /// <summary>Is the agent waiting at a Listen's spot for the person to come: the attention clock holds on it.</summary>
        bool ListenWaiting(Op op) => op.Act != null && op.Act.Do == "listen" && op.Walk == null && SpotOf(op) is ListenSpot sp && G.Within(op.Pos, sp.At, R.ListenAtSpotCm);

        /// <summary>The walk in for a Listen: to its spot, held there up to cell.listen.hold from the choice, then chosen afresh; no spot to be had, the old walk in.</summary>
        Path? ListenWalk(Op op, Person p, Order o)
        {
            int reach = ActReachOf(op, "listen");
            if (op.Spot != null && ReferenceEquals(op.Spot.For, o))
            {
                var have = op.Spot;
                if (!G.Within(op.Pos, have.At, R.ListenAtSpotCm)) { if (Walk.Route(op.Pos, have.At) is Pt[] r0) return new Path(r0); }
                else if (Tick - have.Chose < T(R.ListenHoldS)) return new Path(new[] { op.Pos, op.Pos });
            }
            op.Spot = null;
            if (!PersonPos(p).HasValue) return Intercept(op, p, reach)?.path;
            if (ListenPick(op, p, reach) is not (Pt at, string chair, Path path, int _)) return Intercept(op, p, reach)?.path;
            op.Spot = new ListenSpot { For = o, At = at, Chair = chair, Chose = Tick };
            return path;
        }

        /// <summary>The choice itself: read only. startT: when the act can start, from now.</summary>
        (Pt at, string chair, Path path, int startT)? ListenPick(Op op, Person p, int reach)
        {
            int speed = Math.Max(1, PaceOf(op, false)), listen = T((int)R.Ev("scout.listen").Price[0].SV);
            if (ServerOf(p) == null)
            {
                var her = PersonPos(p); if (!her.HasValue) return null;
                var q = BestChair(op, her.Value, reach, R.ListenNearStillCm, Tick + listen + T(R.ListenChairMarginS), -1, speed) ?? BestGround(op, her.Value, reach, R.ListenNearStillCm, -1, speed);
                if (!q.HasValue || Walk.Route(op.Pos, q.Value.at) is not Pt[] rt) return null;
                var pa = new Path(rt); return (q.Value.at, q.Value.chair, pa, (int)((pa.Length + speed - 1) / speed));
            }
            (Pt at, string chair, Path path, int startT)? ground = null; int groundS = -1;
            for (int s = 0; s <= R.ListenLookS; s += R.ListenStepS)
            {
                if (ground.HasValue && s > groundS + R.ListenChairGraceS) break;
                var her = s == 0 ? PersonPos(p) : PoseAhead(p, T(s))?.pos; if (!her.HasValue) continue;
                int t = T(s);
                var c = BestChair(op, her.Value, reach, R.ListenNearWalkingCm, Tick + t + listen + T(R.ListenChairMarginS), t, speed);
                if (c.HasValue && Walk.Route(op.Pos, c.Value.at) is Pt[] rc) { var pc = new Path(rc); if ((pc.Length + speed - 1) / speed <= t) return (c.Value.at, c.Value.chair, pc, t); }
                if (ground.HasValue) continue;
                var g = BestGround(op, her.Value, reach, R.ListenNearWalkingCm, t, speed);
                if (g.HasValue && Walk.Route(op.Pos, g.Value.at) is Pt[] rg) { var pg = new Path(rg); if ((pg.Length + speed - 1) / speed <= t) { ground = (g.Value.at, "", pg, t); groundS = s; } }
            }
            return ground;
        }

        /// <summary>The free chair nearest the agent whose walkable point is in the ring round the person, out of every cone; within `t` ticks' straight walk unless t is -1.</summary>
        (Pt at, string chair)? BestChair(Op op, Pt her, int reach, int nearest, int until, int t, int speed)
        {
            if (People2 == null) return null;
            (Pt, string)? best = null; long bd = long.MaxValue;
            foreach (var name in People2.Chairs)
            {
                var chair = People2.Places[name]; var st = Walk.NearestFree(chair);
                if (!st.HasValue || G.Dist(st.Value, chair) > R.ListenChairGroundCm || !G.Within(st.Value, her, reach) || G.Within(st.Value, her, nearest)) continue;
                if (t >= 0 && G.Dist(op.Pos, st.Value) * 10L > speed * (long)t) continue;
                if (!ChairFree(name, Tick, until) || WatchedAt(st.Value)) continue;
                long d = G.D2(op.Pos, st.Value); if (d < bd) { bd = d; best = (st.Value, name); }
            }
            return best;
        }

        /// <summary>The free ground beside something solid nearest the agent, in the ring round the person, on a clear line to them, out of every cone.</summary>
        (Pt at, string chair)? BestGround(Op op, Pt her, int reach, int nearest, int t, int speed)
        {
            (Pt, string)? best = null; long bd = long.MaxValue; int cell = S.Grid.Cell;
            for (int dx = -reach; dx <= reach; dx += cell) for (int dy = -reach; dy <= reach; dy += cell)
                {
                    var q = new Pt(her.X + dx, her.Y + dy);
                    if (!S.Grid.Free(q) || !G.Within(q, her, reach) || G.Within(q, her, nearest) || !BesideSolid(q)) continue;
                    if (t >= 0 && G.Dist(op.Pos, q) * 10L > speed * (long)t) continue;
                    long d = G.D2(op.Pos, q); if (d >= bd || !Sees(q, her) || WatchedAt(q)) continue;
                    bd = d; best = (q, "");
                }
            return best;
        }

        internal bool BesideSolid(Pt q) { int c = S.Grid.Cell; return !S.Grid.Free(q + new Pt(c, 0)) || !S.Grid.Free(q - new Pt(c, 0)) || !S.Grid.Free(q + new Pt(0, c)) || !S.Grid.Free(q - new Pt(0, c)); }
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

        /// <summary>The spot this agent's Listen holds now, walking in, waiting or at the act; null if it holds none. At: where it stands (a chair's walkable point, about 1 m off the chair).
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
            if (ListenPick(op, p, ActReachOf(op, "listen")) is (Pt at, string chair, Path path, int startT))
            { int speed = Math.Max(1, PaceOf(op, false)); int walk = (int)((path.Length + speed - 1) / speed); read = (at, chair, walk, Math.Max(walk, startT)); }
            aheadCache[key] = read; return read;
        }
    }
}
