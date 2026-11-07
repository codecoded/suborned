// Brief 098 step 0, the dial's added people (GamePopulation.cs): who they are and the routine they walk, made at the start from the seed and the
// dial's rules (contracts/rules/population.json), with no generator run and nothing baked.
//  · a guard: on shift for the whole mission, walking a beat of post points (Population.SitePoints: staffed doors, police posts, call boxes) at
//    the city's walk, pausing at each, facing out of the door or to the kerb. The guards take the points in a seeded order, one to a point before
//    any shares, and each beat is that point and the nearest others. His marks run every check-in period from his shift's seeded start.
//  · a passer-by: in at a seeded edge at a seeded second, to a café seat free for the whole stay, a public door (inside for a while) or another
//    edge, then out at a seeded edge. Every leg is the crowd's own walk (Crowd.Leg, round the cars standing at the start), timed at the city's walk.
// Names are stand-ins (Guard 1, Passer-by 1): the engine has no name rules. No need record and no ties.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        SitePoints? dialPoints; uint[]? beatOrder;

        /// <summary>Add the k-th person of kind d (from 1): a guard on a beat, or a passer-by. Returns the id.</summary>
        string AddPerson(DialKind d, int k, Crowd crowd)
        {
            dialPoints ??= Population.PointsOf(S, dial!);
            string id = d.IdPrefix + k.ToString(d.Max >= 1000 ? "0000" : "000", System.Globalization.CultureInfo.InvariantCulture);
            var p = new Person { Id = id, Name = d.Name + " " + k.ToString(System.Globalization.CultureInfo.InvariantCulture), Job = d.Job, Temperament = DialTemperament(d, id, crowd) };
            var t = R.Temp(p.Temperament); for (int q = 0; q < 4; q++) p.Sc[q] = t.Start[q];
            var w = new Walker { P = p, Added = true };
            var stops = R.StaffJobs.Contains(d.Job) ? Beat(w, k, crowd) : Passing(w, crowd);
            w.Place = new string[stops.Count]; w.From = new int[stops.Count]; w.To = new int[stops.Count]; w.Out = new bool[stops.Count];
            for (int i = 0; i < stops.Count; i++) { (w.Place[i], w.From[i], w.To[i], w.Out[i]) = stops[i]; }
            xStops[id] = stops; Walkers.Add(w);
            return id;
        }

        /// <summary>The role's allowed temperaments (people.rules temperamentsBy.roles), weighted by the zone's own mix of that kind (the people file's
        /// and the cast's), no one holding more than temperamentsBy.mix.rowMaxShare; equal weights if the zone has none of that kind.</summary>
        string DialTemperament(DialKind d, string id, Crowd crowd)
        {
            var count = new int[d.Allow.Length]; int total = 0;
            void see(string job, string temp) { if (Population.KindOf(dial!, job) != d.Kind) return; int i = Array.IndexOf(d.Allow, temp); if (i >= 0) { count[i]++; total++; } }
            foreach (var w in crowd.Walkers) see(w.P.Job, w.P.Temperament);
            foreach (var c in S.Cast) see(Population.CastJob(c.Role), c.Temperament);
            var pm = new int[d.Allow.Length];
            for (int i = 0; i < pm.Length; i++) pm[i] = total == 0 ? 1000 / pm.Length : count[i] * 1000 / total;
            int cap = dial!.RowMaxPm;
            for (int round = 0; round < pm.Length; round++) // the cap: an over-share is cut to it and the rest goes to those under it, by their shares
            {
                int excess = 0, under = 0; for (int i = 0; i < pm.Length; i++) { if (pm[i] > cap) { excess += pm[i] - cap; pm[i] = cap; } else if (pm[i] < cap) under += pm[i]; }
                if (excess == 0) break;
                int given = 0, last = -1; for (int i = 0; i < pm.Length; i++) if (pm[i] < cap) { int g = under > 0 ? excess * pm[i] / under : excess / pm.Length; pm[i] += g; given += g; last = i; }
                if (last >= 0) pm[last] += excess - given;
            }
            int sum = 0; foreach (var x in pm) sum += x; if (sum <= 0) return d.Allow[0];
            int r = (int)(XDraw(id, "temperament") % (uint)sum);
            for (int i = 0; i < pm.Length; i++) { if (r < pm[i]) return d.Allow[i]; r -= pm[i]; }
            return d.Allow[d.Allow.Length - 1];
        }

        /// <summary>A place the dial found, registered for this game with its facing.</summary>
        string DialPlace(string id, Pt at, Pt face) { if (!xPlaces.ContainsKey(id)) { xPlaces[id] = at; xFaces[id] = face == new Pt(0, 0) ? new Pt(100, 0) : face; } return id; }
        /// <summary>A leg's walk, built once for every game on this crowd, and its ticks at the city's walk.</summary>
        int LegTicks(Crowd crowd, string a, string b)
        {
            var key = a + "→" + b;
            if (!crowdPaths.TryGetValue(key, out var path)) { path = crowd.XLeg(S, R, key, CrowdPlace(a), CrowdPlace(b)); crowdPaths[key] = path; }
            int walk = Math.Max(1, R.PeopleWalk); return (int)Math.Max(1, (path.Length + walk - 1) / walk);
        }

        /// <summary>The k-th added guard's shift: his beat's points in a loop from his shift's seeded start (shiftBefore before 0:00, plus up to a
        /// check-in period) to past the close (+ shiftAfter), pausing at each point a seeded pauseS.</summary>
        List<(string place, int from, int to, bool outside)> Beat(Walker w, int k, Crowd crowd)
        {
            var pts = dialPoints!.Posts; if (pts.Count == 0) throw new FormatException("start.counts: the site has no post points for a guard's beat (staffed doors, police posts, call boxes; brief 098)");
            if (beatOrder == null) // the guards take the points in one seeded order (Fisher-Yates over the set's stream), so adding a guard moves nobody else's
            {
                var o = new uint[pts.Count]; for (int i = 0; i < o.Length; i++) o[i] = (uint)i;
                for (int i = o.Length - 1; i > 0; i--) { int j = (int)(XDraw(dial!.Kinds["guards"].IdPrefix + "beats", "order", o.Length - i) % (uint)(i + 1)); (o[i], o[j]) = (o[j], o[i]); }
                beatOrder = o;
            }
            int a = (int)beatOrder[(k - 1) % pts.Count];
            var near = new List<int>(); for (int i = 0; i < pts.Count; i++) if (i != a) near.Add(i);
            near.Sort((x, y) => { int c = G.D2(pts[x].at, pts[a].at).CompareTo(G.D2(pts[y].at, pts[a].at)); return c != 0 ? c : string.CompareOrdinal(pts[x].id, pts[y].id); });
            var beat = new List<int> { a }; for (int i = 0; i < near.Count && beat.Count < dial!.BeatSize; i++) beat.Add(near[i]);
            var names = new List<string>(); foreach (var i in beat) names.Add(DialPlace(pts[i].id, pts[i].at, pts[i].face != new Pt(0, 0) ? pts[i].face : KerbFrom(pts[i].at) ?? new Pt(100, 0)));
            var id = w.P.Id; var stops = new List<(string, int, int, bool)>();
            int t = -T(dial!.ShiftBeforeS) - (int)(XDraw(id, "shift") % (uint)T(R.CheckInPeriodS)), end = T(S.Over + dial.ShiftAfterS), from = t;
            for (int n = 0; t < end; n++)
            {
                string here = names[n % names.Count], next = names[(n + 1) % names.Count];
                int pause = T(XBand(id, "pause", dial.PauseMinS, dial.PauseMaxS, n + 1));
                stops.Add((here, t, t + pause, true)); t += pause;
                if (names.Count > 1) t += LegTicks(crowd, here, next);
            }
            beats[id] = (from, stops[stops.Count - 1].Item3);
            return stops;
        }

        /// <summary>A passer-by's visit: in at a seeded edge at a seeded second of enterS, to a destination by the dial's weights (a seat free for the
        /// whole stay, a public door, or another edge), lingering a seeded while, then out at a seeded edge.</summary>
        List<(string place, int from, int to, bool outside)> Passing(Walker w, Crowd crowd)
        {
            var edges = dialPoints!.Edges; if (edges.Count == 0) throw new FormatException("start.counts: the site has no edges for a passer-by to come in at (brief 098)");
            var id = w.P.Id; var r = dial!;
            int e1 = (int)(XDraw(id, "in") % (uint)edges.Count), e2 = (int)(XDraw(id, "out") % (uint)edges.Count);
            if (e2 == e1 && edges.Count > 1) e2 = (e2 + 1) % edges.Count;
            string inAt = DialPlace(edges[e1].id, edges[e1].at, edges[e1].face), outAt = DialPlace(edges[e2].id, edges[e2].at, edges[e2].face);
            int t0 = T(XBand(id, "enter", r.EnterMinS, r.EnterMaxS));
            var doors = new List<string>(); foreach (var e in S.Entrances) if (e.kind == "public" && crowd.Places.ContainsKey(e.id)) doors.Add(e.id);
            var seats = crowd.Chairs;
            int sum = (seats.Count > 0 ? r.DestSeat : 0) + (doors.Count > 0 ? r.DestDoor : 0) + r.DestEdge, pick = (int)(XDraw(id, "dest") % (uint)sum);
            var stops = new List<(string, int, int, bool)> { (inAt, t0, t0, true) };
            string? dest = null; bool inside = false; int stay = 0;
            if (seats.Count > 0 && pick < r.DestSeat)
            {
                stay = T(XBand(id, "linger", r.LingerSeatMinS, r.LingerSeatMaxS));
                int s0 = (int)(XDraw(id, "seat") % (uint)seats.Count);
                for (int q = 0; q < seats.Count && dest == null; q++)
                {
                    var seat = seats[(s0 + q) % seats.Count]; int at = t0 + LegTicks(crowd, inAt, seat);
                    if (ChairFree(seat, at, at + stay)) dest = seat; // a seat is one person's at a time
                }
            }
            else if (doors.Count > 0 && pick < (seats.Count > 0 ? r.DestSeat : 0) + r.DestDoor) { dest = doors[(int)(XDraw(id, "door") % (uint)doors.Count)]; inside = true; stay = T(XBand(id, "linger", r.LingerDoorMinS, r.LingerDoorMaxS)); }
            int t = t0;
            if (dest != null) { t += LegTicks(crowd, inAt, dest); stops.Add((dest, t, t + stay, !inside)); t += stay; t += LegTicks(crowd, dest, outAt); }
            else t += LegTicks(crowd, inAt, outAt); // straight through: no seat free, or the way through drawn
            stops.Add((outAt, t, t, true));
            return stops;
        }
    }
}
