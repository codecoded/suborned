// The crowd in a fight (Pulse, briefs/011/pulse-fight-ai.md §4; rulebook `crowd`): the city's people scatter from shots and drawn weapons
// to the refuges of the site's cover data, shelter, and walk back when it is quiet; and a person on the street blocks a watcher's
// sightline. The four people cast by role keep their places. Integers only; people act in the walker order (by id, as loaded).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        readonly List<Pt> shotsThisTick = new List<Pt>();
        public int LastShotTick = -1; Pt lastShooter;
        readonly List<Pt> occluders = new List<Pt>(); int occludersTick = -1;

        Pt? ScatterPos(Walker w)
        {
            switch (w.St)
            {
                case "startled": return w.At;
                case "fleeing": return w.Run == null ? w.At : w.Run.At((long)(Tick - w.RunStart) * R.CrowdRunSpeed);
                case "returning": return w.Run == null ? w.At : w.Run.At((long)(Tick - w.RunStart) * opSpeed);
                default: return null; // sheltered: off the street
            }
        }

        /// <summary>One tick of the crowd (Pulse §4): startle on a shot within crowd.scatter or a drawn weapon in sight within witness range;
        /// flee to a refuge; shelter; walk back crowd.quiet after the last shot.</summary>
        void CrowdStep()
        {
            occludersTick = -1;
            if (People2 == null || !Start.Crowd) { shotsThisTick.Clear(); return; }
            var drawn = new List<Pt>();
            foreach (var op in Ops) if (op.Gun.Drawn && !op.Down && !op.Inside) drawn.Add(op.Pos);
            long scatter2 = (long)R.CrowdScatterCm * R.CrowdScatterCm;
            int startled = 0, sheltered = 0, back = 0;
            foreach (var w in Walkers)
            {
                if (w.P.Down) continue;
                switch (w.St)
                {
                    case "": case "returning":
                        {
                            var pos = CrowdPos(w); if (!pos.HasValue) break;
                            Pt? from = null;
                            foreach (var s in shotsThisTick) if (G.D2(s, pos.Value) <= scatter2) { from = s; break; }
                            if (!from.HasValue) foreach (var d in drawn) if (G.Within(d, pos.Value, witnessRange) && Sees(d, pos.Value)) { from = d; break; }
                            if (!from.HasValue) break;
                            w.St = "startled"; w.At = pos.Value; w.Run = null; w.RunStart = Tick; lastShooterFor[w.P.Id] = from.Value;
                            w.Until = Tick + (w.P.Temperament == "fearful" ? R.CrowdStartleFearfulTicks : R.CrowdStartleTicks); startled++;
                            break;
                        }
                    case "startled":
                        if (Tick < w.Until) break;
                        {
                            var shooter = LastShotTick >= w.RunStart ? lastShooter : lastShooterFor[w.P.Id]; // the most recent shot's shooter, else what startled them
                            var (id, at) = RefugeFor(w.At, shooter);
                            w.Refuge = id; w.Run = RouteOf(w.At, at); w.RunStart = Tick; w.St = "fleeing";
                        }
                        break;
                    case "fleeing":
                        if (w.Run == null || (long)(Tick - w.RunStart) * R.CrowdRunSpeed >= w.Run.Length) { w.St = "sheltered"; w.Until = Tick; sheltered++; }
                        break;
                    case "sheltered":
                        if (Tick - Math.Max(LastShotTick, w.Until) < T(R.CrowdQuietS)) break;
                        {
                            var home = RoutinePos(w, Tick);
                            if (!home.HasValue) { w.St = ""; break; }
                            var start = w.Run?.End ?? w.At;
                            w.Run = RouteOf(start, home.Value); w.RunStart = Tick; w.At = start; w.St = "returning";
                        }
                        break;
                }
                if (w.St == "returning" && (w.Run == null || (long)(Tick - w.RunStart) * opSpeed >= w.Run.Length)) { w.St = ""; w.Run = null; back++; }
            }
            shotsThisTick.Clear();
            if (startled > 0) Note("crowd.startled", $"{startled} on the street are startled and run.");
            if (sheltered > 0) Note("crowd.sheltered", $"{sheltered} reach shelter.");
            if (back > 0) Note("crowd.back", $"{back} are back on their way.");
        }
        readonly Dictionary<string, Pt> lastShooterFor = new Dictionary<string, Pt>(StringComparer.Ordinal);

        Path RouteOf(Pt a, Pt b)
        {
            var fa = S.Grid.NearestFree(a) ?? a; var fb = S.Grid.NearestFree(b) ?? b;
            var route = S.Grid.Route(fa, fb);
            return new Path(route != null && route.Length >= 2 ? route : new[] { a, b });
        }

        /// <summary>Pulse §4 refuge: within crowd.fleeMax, ending farther from the shooter than the person stands, the nearest (ties on id);
        /// if none, the refuge farthest from the shooter.</summary>
        (string id, Pt at) RefugeFor(Pt me, Pt shooter)
        {
            long mine = G.D2(me, shooter), max2 = (long)R.CrowdFleeMaxCm * R.CrowdFleeMaxCm;
            (string id, Pt at) best = ("", me); long bd = long.MaxValue;
            foreach (var (id, at, _) in S.Refuges)
            {
                long d = G.D2(me, at);
                if (d > max2 || G.D2(at, shooter) <= mine) continue;
                if (d < bd || (d == bd && string.CompareOrdinal(id, best.id) < 0)) { bd = d; best = (id, at); }
            }
            if (best.id.Length > 0) return best;
            long far = -1;
            foreach (var (id, at, _) in S.Refuges) { long d = G.D2(at, shooter); if (d > far || (d == far && string.CompareOrdinal(id, best.id) < 0)) { far = d; best = (id, at); } }
            return best;
        }

        /// <summary>Pulse §4: a person on the street, not sheltered, blocks a watcher's sightline that passes within crowd.occlude of them,
        /// between two bodies' width (2 × occlude) of either end.</summary>
        public bool CrowdBlocks(Pt a, Pt b)
        {
            if (People2 == null || !Start.Crowd) return false;
            if (occludersTick != Tick)
            {
                occluders.Clear(); occludersTick = Tick;
                foreach (var w in Walkers) { var p = CrowdPos(w); if (p.HasValue) occluders.Add(p.Value); }
            }
            long dx = b.X - a.X, dy = b.Y - a.Y, l2 = dx * dx + dy * dy; if (l2 == 0) return false;
            long occ = R.CrowdOccludeCm, end2 = 4 * occ * occ;
            foreach (var p in occluders)
            {
                long px = p.X - a.X, py = p.Y - a.Y, dot = px * dx + py * dy;
                if (dot <= 0 || dot >= l2) continue;
                if (G.D2(p, a) <= end2 || G.D2(p, b) <= end2) continue;
                long cross = px * dy - py * dx;
                if (cross * cross <= occ * occ * l2) return true;
            }
            return false;
        }
    }
}
