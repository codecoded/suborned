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
    public sealed class DialKind { public string Kind = "", Role = "", Job = "", IdPrefix = "", Name = ""; public string[] Of = Array.Empty<string>(); public bool Rest; public int Max; public string[] Allow = Array.Empty<string>(); }

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
                var k = kv.Value; k.Only("of", "role", "job", "id", "name", "max", "src", "proposed", "reason");
                var d = new DialKind { Kind = kv.Key, Role = k["role"].Str(), Job = k["job"].Str(), IdPrefix = k["id"].Str(), Name = k["name"].Str(), Max = k["max"].Int() };
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

        /// <summary>Today's census from the data alone, for a host's dial before any game: per kind, the people file's count, the floor and the most.</summary>
        public static List<KindCensus> Today(Engine.Inputs i)
        {
            var l = new List<KindCensus>();
            var pop = i.Population ?? throw new FormatException($"the dial needs its rules ({RulesFile})");
            foreach (var kind in Kinds)
            {
                if (kind == Cars) { l.Add(new KindCensus { Kind = kind, Max = pop.Traffic.Max }); continue; } // brief 098 step 1: no traffic today (0, floor 0)
                var d = pop.Kinds[kind]; var c = new KindCensus { Kind = kind, Max = d.Max };
                if (i.Crowd != null) foreach (var w in i.Crowd.Walkers) if (KindOf(pop, w.P.Job) == kind) c.Today++;
                foreach (var cr in i.SiteData.Cast) if (KindOf(pop, CastJob(cr.Role)) == kind) { c.Today++; c.Floor++; }
                c.Count = c.Today; l.Add(c);
            }
            return l;
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
