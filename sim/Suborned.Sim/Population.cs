// Brief 098 step 0, the dial (Sky 2026-11-07: "add 3 more guards, take some away... the game then just does that, no long processing"): the
// rules of contracts/rules/population.json, today's census of the zone by kind, and the points the engine finds in a site for the people it
// adds (post points for a guard's beat, edges where a passer-by comes in). Read only when a start sets counts (Start.Counts); the game builds
// that world in GamePopulation.cs. Integers only; every derived point is a pure function of the site.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Suborned.Sim
{
    /// <summary>One kind on the dial (population.json kinds.&lt;kind&gt;): which jobs of the people file it counts ("rest": every job no other kind
    /// names), the people.rules role an added one takes its temperament from, the job, id prefix and stand-in name it carries, and the most a start may ask.</summary>
    public sealed class DialKind
    {
        public string Kind = "", Role = "", Job = "", IdPrefix = "", Name = ""; public string[] Of = Array.Empty<string>(); public bool Rest; public int Max; public string[] Allow = Array.Empty<string>();
        /// <summary>Brief 098 step 1b (Sky 2026-11-07: "people counter is for street"): the kind's count is of people on the street at a time on average
        /// (measure street), not of everyone in the zone (measure zone).</summary>
        public bool Street;
    }

    /// <summary>contracts/rules/population.json (suborned.rules.population/1), checked against the people rules' roles and mix (unknown = reject).</summary>
    public sealed class PopulationRules
    {
        public readonly string Text; public readonly ulong TextHash;
        public readonly SortedDictionary<string, DialKind> Kinds = new SortedDictionary<string, DialKind>(StringComparer.Ordinal);
        public readonly string LeaveSalt;
        public readonly string[] BeatPoints; public readonly int BeatSize, PauseMinS, PauseMaxS, DoorOutCm, ShiftBeforeS, ShiftAfterS;
        public readonly int EnterMinS, EnterMaxS, LingerSeatMinS, LingerSeatMaxS, LingerDoorMinS, LingerDoorMaxS, EdgeMinCells, EdgeSpanCells;
        public readonly int DestSeat, DestDoor, DestEdge;
        /// <summary>people.rules temperamentsBy.mix.rowMaxShare, per mille: no temperament holds more of an added kind's draw.</summary>
        public readonly int RowMaxPm;
        /// <summary>Brief 098 step 1 (law L8): the cars kind and the traffic's rules (kinds.cars, traffic), and the lanes the host loaded with them
        /// (sim/data/handover.lanes.json, Engine.Load), or null when it loaded none: a start that sets cars is then rejected.</summary>
        public readonly TrafficRules Traffic = null!;
        public LaneNet? Lanes;
        public PopulationRules(string json, string peopleRules)
        {
            Text = JNode.Canon(json); TextHash = Hash.Of(Text);
            var n = JNode.Parse(Text, "population");
            n.Only("schema", "author", "gdd", "rule", "kinds", "fewer", "beat", "passers", "traffic");
            if (n["schema"].Str() != "suborned.rules.population/1") throw new FormatException("population schema is not suborned.rules.population/1");
            var pr = JNode.Parse(peopleRules, "people.rules")["temperamentsBy"];
            foreach (var kv in n["kinds"].Entries)
            {
                if (Array.IndexOf(Population.Kinds, kv.Key) < 0) throw new FormatException($"{kv.Value.Path}: '{kv.Key}' is not a kind the engine builds ({string.Join(", ", Population.Kinds)})");
                if (kv.Key == Population.Cars) { Traffic = new TrafficRules(kv.Value, n["traffic"], pr); continue; } // brief 098 step 1: cars are no people kind (Traffic.cs)
                var k = kv.Value; k.Only("of", "role", "job", "id", "name", "max", "measure", "src", "proposed", "reason");
                var d = new DialKind { Kind = kv.Key, Role = k["role"].Str(), Job = k["job"].Str(), IdPrefix = k["id"].Str(), Name = k["name"].Str(), Max = k["max"].Int() };
                var measure = k.Opt("measure")?.Str() ?? "zone"; if (measure != "zone" && measure != "street") throw new FormatException($"{k["measure"].Path}: zone or street (unknown = reject)"); d.Street = measure == "street"; // brief 098 step 1b
                if (k["of"].Kind == JKind.Str) { if (k["of"].Str() != "rest") throw new FormatException($"{k["of"].Path}: a list of jobs, or \"rest\""); d.Rest = true; }
                else d.Of = k["of"].Strs().ToArray();
                if (d.Max < 0 || d.Max > Population.CountCap) throw new FormatException($"{k["max"].Path}: 0..{Population.CountCap}");
                if (d.IdPrefix.Length == 0 || d.IdPrefix.StartsWith("p-", StringComparison.Ordinal)) throw new FormatException($"{k["id"].Path}: an added person's id prefix is not the people file's (p-)");
                var role = pr["roles"].Opt(d.Role) ?? throw new FormatException($"{k["role"].Path}: '{d.Role}' is not a people.rules temperamentsBy role (unknown = reject)");
                d.Allow = role["allow"].Strs().ToArray(); if (d.Allow.Length == 0) throw new FormatException($"{k["role"].Path}: the role allows no temperament");
                Kinds[kv.Key] = d;
            }
            foreach (var kind in Population.PeopleKinds) if (!Kinds.ContainsKey(kind)) throw new FormatException($"population.kinds.{kind} is missing");
            if (Traffic == null) throw new FormatException("population.kinds.cars is missing (brief 098 step 1: the traffic's kind)");
            int rests = 0; var named = new HashSet<string>(StringComparer.Ordinal); foreach (var d in Kinds.Values) { if (d.Rest) rests++; foreach (var j in d.Of) if (!named.Add(j)) throw new FormatException($"population.kinds: job '{j}' is named by two kinds"); }
            if (rests != 1) throw new FormatException("population.kinds: exactly one kind is \"rest\"");
            var f = n["fewer"]; f.Only("salt", "src", "proposed", "reason"); LeaveSalt = f["salt"].Str();
            var b = n["beat"]; b.Only("points", "size", "pauseS", "doorOutCm", "shiftBeforeS", "shiftAfterS", "src", "proposed", "reason");
            BeatPoints = b["points"].Strs().ToArray(); foreach (var p in BeatPoints) if (p != "door" && p != "post" && p != "call-box") throw new FormatException($"{b["points"].Path}: '{p}' (door, post, call-box)");
            BeatSize = b["size"].Int(); (PauseMinS, PauseMaxS) = Band(b["pauseS"]); DoorOutCm = b["doorOutCm"].Int(); ShiftBeforeS = b["shiftBeforeS"].Int(); ShiftAfterS = b["shiftAfterS"].Int();
            if (BeatSize < 1 || BeatSize > 8 || PauseMinS < 1 || DoorOutCm < 0 || DoorOutCm > 1000 || ShiftBeforeS < 0 || ShiftAfterS < 0) throw new FormatException("population.beat: size 1..8, pauses of a second or more, doorOutCm 0..1000, shifts 0 or more");
            var ps = n["passers"]; ps.Only("enterS", "dest", "lingerS", "edgeMinCells", "edgeSpanCells", "src", "proposed", "reason");
            (EnterMinS, EnterMaxS) = Band(ps["enterS"]);
            var dw = ps["dest"]; dw.Only("seat", "door", "edge"); DestSeat = dw["seat"].Int(); DestDoor = dw["door"].Int(); DestEdge = dw["edge"].Int();
            if (DestSeat < 0 || DestDoor < 0 || DestEdge < 1) throw new FormatException("population.passers.dest: weights 0 or more, edge at least 1 (the way through is always there)");
            var lg = ps["lingerS"]; lg.Only("seat", "door"); (LingerSeatMinS, LingerSeatMaxS) = Band(lg["seat"]); (LingerDoorMinS, LingerDoorMaxS) = Band(lg["door"]);
            EdgeMinCells = ps["edgeMinCells"].Int(); EdgeSpanCells = ps["edgeSpanCells"].Int();
            if (EdgeMinCells < 1 || EdgeSpanCells < EdgeMinCells) throw new FormatException("population.passers: edgeMinCells at least 1, edgeSpanCells no less");
            var share = pr["mix"]["rowMaxShare"].NumberText;
            if (!decimal.TryParse(share, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sh) || sh <= 0 || sh > 1) throw new FormatException("people.rules temperamentsBy.mix.rowMaxShare is a share 0..1");
            RowMaxPm = (int)(sh * 1000);
        }
        static (int, int) Band(JNode n) { var v = n.Ints(); if (v.Length != 2 || v[1] < v[0]) throw new FormatException($"{n.Path}: a band is [min, max]"); return (v[0], v[1]); }
    }

    /// <summary>One kind's census in a game (Game.Census) or in the data (Population.Today): today's count (the people file's), the floor (its
    /// tenets: the cast, who never leave), the most the dial allows, what the start asked (null: not set) and what plays.</summary>
    public sealed class KindCensus
    {
        public string Kind = ""; public int Today, Floor, Max, Count; public int? Asked;
        public readonly List<string> Removed = new List<string>(), Added = new List<string>();
        /// <summary>Brief 098 step 1b: what the kind's numbers count: "street" (people on the street at a time on average: Today, Floor, Max, Asked and
        /// Count are street numbers), "zone" (everyone of the kind in the zone) or "roads" (cars on the zone's roads at a time on average). Street10:
        /// the street at a time on average, in tenths, as the game holds it (for a street kind, what the count achieved). Total and TotalToday: how many of
        /// the kind are in the zone in this game and today, the cast included.</summary>
        public string Measure = ""; public int Street10, Total, TotalToday;
        /// <summary>Brief 098 step 1b: of Street10, the cast's share (tenths): they are played by role and never leave, so the fewest a street count plays.</summary>
        public int CastStreet10;
        /// <summary>Plain words when the count plays other than asked (below the floor, above the most), else "".</summary>
        public string Note = "";
    }

    public static class Population
    {
        /// <summary>The kinds the dial builds today: guards and people (the people file's, more or fewer), and cars (brief 098 step 1: passing traffic,
        /// none today). Patrol robots and cameras join here when their rules exist.</summary>
        public static readonly string[] Kinds = { "guards", "people", "cars" };
        /// <summary>The kinds that count people (the people file's walkers and the dial's added ones).</summary>
        public static readonly string[] PeopleKinds = { "guards", "people" };
        public const string Cars = "cars";
        /// <summary>The most any count may be in a start (each kind's own `max` is lower).</summary>
        public const int CountCap = 100000;
        public const string RulesFile = "contracts/rules/population.json";

        /// <summary>Today's census from the data alone, for a host's dial before any game: per kind, today's number, the floor and the most (brief
        /// 098 step 1b: for the people, on the street at a time on average; Total, everyone of the kind in the zone).</summary>
        public static List<KindCensus> Today(Engine.Inputs i)
        {
            var l = new List<KindCensus>();
            var pop = i.Population ?? throw new FormatException($"the dial needs its rules ({RulesFile})");
            foreach (var kind in Kinds)
            {
                if (kind == Cars) { l.Add(new KindCensus { Kind = kind, Max = pop.Traffic.Max, Measure = "roads" }); continue; } // brief 098 step 1: no traffic today (0, floor 0)
                var d = pop.Kinds[kind]; var c = new KindCensus { Kind = kind, Max = d.Max, Measure = d.Street ? "street" : "zone" };
                if (i.Crowd != null) foreach (var w in i.Crowd.Walkers) if (KindOf(pop, w.P.Job) == kind) c.Today++;
                foreach (var cr in i.SiteData.Cast) if (KindOf(pop, CastJob(cr.Role)) == kind) { c.Today++; c.Floor++; }
                c.Total = c.TotalToday = c.Today;
                if (i.Crowd != null) { var (sum, cast) = StreetToday(i.SiteData, i.Crowd, i.Rules, pop, kind); int over = Game.T(i.SiteData.Over); c.Street10 = (int)((sum * 20 + over) / (2L * over)); c.CastStreet10 = (int)((cast * 20 + over) / (2L * over)); if (d.Street) { c.Today = (int)((sum * 2 + over) / (2L * over)); c.Floor = (int)((cast * 2 + over) / (2L * over)); } }
                c.Count = c.Today; l.Add(c);
            }
            return l;
        }
        static readonly ConditionalWeakTable<Crowd, Dictionary<string, (long, long)>> streetToday = new ConditionalWeakTable<Crowd, Dictionary<string, (long, long)>>();
        /// <summary>Brief 098 step 1b: a kind's street ticks today, summed over the mission's clock (Street.Ticks): the people file's walkers of the kind
        /// at the neutral start (no seeded shift of their day, paces from the id), and of that the cast's, by the people file's stops for them. Once per crowd.</summary>
        public static (long sum, long cast) StreetToday(Site s, Crowd crowd, Rules r, PopulationRules pop, string kind)
        {
            lock (streetToday)
            {
                var m = streetToday.GetOrCreateValue(crowd); if (m.TryGetValue(kind, out var v)) return v;
                int over = Game.T(s.Over); long sum = 0, cast = 0;
                long Of(string id, IReadOnlyList<(string place, int from, int to, bool outside)> st)
                {
                    int pace = (int)(Streams.Fnv1a32(id + "/pace") % (uint)(r.CrowdPaceSpreadPct + 1));
                    return Street.Ticks(st, over, s.Grid, p => crowd.Places[p], (k, t, span) =>
                    {
                        var key = st[k].place + "→" + st[k + 1].place; var path = crowd.Legs.TryGetValue(key, out var lp) ? lp : Crowd.Leg(s, crowd.Places[st[k].place], crowd.Places[st[k + 1].place], crowd.Home(s, r));
                        long mm = span <= 0 ? path.Length : Math.Min(path.Length, path.Length * t * (100 + pace) / (100L * span)); return path.At(mm);
                    });
                }
                foreach (var w in crowd.Walkers)
                {
                    if (KindOf(pop, w.P.Job) != kind) continue;
                    var st = new List<(string, int, int, bool)>(); for (int k = 0; k < w.Place.Length; k++) st.Add((w.Place[k], w.From[k], w.To[k], w.Out[k]));
                    sum += Of(w.P.Id, st);
                }
                foreach (var cr in s.Cast) if (KindOf(pop, CastJob(cr.Role)) == kind && crowd.Stops.TryGetValue(cr.Id, out var cs)) { long n = Of(cr.Id, cs); sum += n; cast += n; }
                m[kind] = (sum, cast); return (sum, cast);
            }
        }
        /// <summary>The kind a job counts as: the kind that names it, else the "rest" kind.</summary>
        public static string KindOf(PopulationRules pop, string job)
        {
            string rest = "";
            foreach (var d in pop.Kinds.Values) { if (d.Rest) rest = d.Kind; else if (Array.IndexOf(d.Of, job) >= 0) return d.Kind; }
            return rest;
        }
        /// <summary>The cast are played by role, not as the crowd: the door guard is a guard; the waitress, the driver and the councillor are the rest.</summary>
        public static string CastJob(string role) => role == "guard" ? "guard" : role;

        static readonly ConditionalWeakTable<Site, SitePoints> points = new ConditionalWeakTable<Site, SitePoints>();
        /// <summary>The site's post points and edges (computed once per site, shared by every game on it: read-only after).</summary>
        public static SitePoints PointsOf(Site s, PopulationRules pop) { lock (points) { if (!points.TryGetValue(s, out var p)) { p = new SitePoints(s, pop); points.Add(s, p); } return p; } }
    }

    /// <summary>What the engine finds in a site for the people it adds. Post points: each staffed door (a point doorOutCm out of it, so the door's
    /// own post stays its holder's), each police post on the street, each call box (they stand at the corners, R-REPORT), on the walk grid's main
    /// ground. Edges: the runs of free cells on the walk grid's border (a street leaving the window), at least edgeMinCells wide, each entered
    /// at its middle (a run longer than edgeSpanCells split evenly). Sorted by id; ids "beat:&lt;the point's id&gt;" and "edge:&lt;side&gt;:&lt;k&gt;".</summary>
    public sealed class SitePoints
    {
        public readonly List<(string id, Pt at, Pt face)> Posts = new List<(string, Pt, Pt)>(), Edges = new List<(string, Pt, Pt)>();
        public readonly int MainPiece;
        public SitePoints(Site s, PopulationRules pop)
        {
            var g = s.Grid;
            // the main ground: the largest piece of free cells (the street network), so nobody is put on a pocket walled off from it
            int best = -1, bestSize = -1;
            for (int i = 0; i < g.Nx; i += 4) for (int j = 0; j < g.Ny; j += 4) { if (!g.FreeCell(i, j)) continue; int k = g.PieceOf(g.Centre(i, j)), sz = g.PieceSize(k); if (sz > bestSize) { bestSize = sz; best = k; } }
            MainPiece = best;
            bool main(Pt p) => g.Free(p) && g.PieceOf(p) == MainPiece;
            var raw = new List<(string id, Pt at, Pt face)>();
            if (Array.IndexOf(pop.BeatPoints, "door") >= 0)
                foreach (var (id, at, kind) in s.Refuges)
                {
                    if (kind != "door") continue;
                    var place = id.StartsWith("door:", StringComparison.Ordinal) ? id.Substring(5) : id;
                    Pt? stand = null; foreach (var e in s.Entrances) if (e.id == place && e.stand != e.at) stand = e.stand;
                    if (!stand.HasValue) continue;
                    var face = stand.Value - at; long len = Math.Max(1, G.Isqrt(G.D2(at, stand.Value)));
                    var p = new Pt(stand.Value.X + (int)(face.X * pop.DoorOutCm / len), stand.Value.Y + (int)(face.Y * pop.DoorOutCm / len));
                    var q = g.NearestFree(p); if (q.HasValue && main(q.Value)) raw.Add(("beat:" + id, q.Value, face));
                }
            if (Array.IndexOf(pop.BeatPoints, "post") >= 0) foreach (var (id, at) in s.Posts) if (!id.StartsWith("door:", StringComparison.Ordinal) && main(at)) raw.Add(("beat:" + id, at, default));
            if (Array.IndexOf(pop.BeatPoints, "call-box") >= 0) foreach (var (id, at) in s.CallBoxes) if (main(at)) raw.Add(("beat:" + id, at, default));
            raw.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            foreach (var x in raw) if (!Posts.Exists(y => y.id == x.id)) Posts.Add(x);
            // the edges: four sides of the window, each run of free main cells along it
            void side(string name, int n, Func<int, (int i, int j)> cell, Pt inward)
            {
                int k = 0, run = -1;
                for (int t = 0; t <= n; t++)
                {
                    bool free = t < n && g.FreeCell(cell(t).i, cell(t).j) && g.PieceOf(g.Centre(cell(t).i, cell(t).j)) == MainPiece;
                    if (free && run < 0) run = t;
                    if (free || run < 0) continue;
                    int len = t - run;
                    if (len >= pop.EdgeMinCells)
                    {
                        int parts = (len + pop.EdgeSpanCells - 1) / pop.EdgeSpanCells;
                        for (int q = 0; q < parts; q++) { var (ci, cj) = cell(run + (2 * q + 1) * len / (2 * parts)); Edges.Add(($"edge:{name}:{k++}", g.Centre(ci, cj), inward)); }
                    }
                    run = -1;
                }
            }
            side("n", g.Ny, t => (0, t), new Pt(100, 0)); side("s", g.Ny, t => (g.Nx - 1, t), new Pt(-100, 0));
            side("w", g.Nx, t => (t, 0), new Pt(0, 100)); side("e", g.Nx, t => (t, g.Ny - 1), new Pt(0, -100));
        }
    }
}
