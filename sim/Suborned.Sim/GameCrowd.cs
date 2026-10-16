// The city's people stepping their routines (N12; GDD §04 person = routine + need + temperament + memory; brief 010): every
// simulated person of sim/data/handover.people.json stands at the stop their routine gives for the mission's clock, or walks the
// grid between two stops, timed from one stop's end to the next one's start. At home, working inside or beyond the sheet they are
// not on the street. On the street they witness (§05; rulebook witness.sees): within 13 m on a clear sightline they add Trail once
// an incident, move standing, and remember. The four people the Handover casts by role are played by their role, not here.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed class Walker
    {
        public Person P = null!;
        public string[] Place = Array.Empty<string>(); public int[] From = Array.Empty<int>(), To = Array.Empty<int>(); public bool[] Out = Array.Empty<bool>(); // ticks from mission 0:00
        /// <summary>A fresh copy for one game: the routine is shared, the person's state is not.</summary>
        // in a fight (Pulse §4): "" routine, startled, fleeing, sheltered, returning; where they stand or run, and since when
        public string St = ""; public int Until, RunStart; public Pt At; public Path? Run; public string Refuge = "";
        public int Pace; // brief 013: the pace of a report's walk or a check (mm a tick); 0 for the crowd machine's own states
        public Walker Fresh()
        {
            var p = new Person { Id = P.Id, Name = P.Name, Job = P.Job, Temperament = P.Temperament, Need = P.Need, Role = "" };
            Array.Copy(P.Sc, p.Sc, 4);
            return new Walker { P = p, Place = Place, From = From, To = To, Out = Out };
        }
    }

    public sealed class Crowd
    {
        public readonly List<Walker> Walkers = new List<Walker>();
        public readonly Dictionary<string, Pt> Places = new Dictionary<string, Pt>(StringComparer.Ordinal);
        public readonly int StartsAt;
        /// <summary>Everyone's ties, home and work, the cast's too (between missions: word along ties, need replacement's requires).</summary>
        public readonly Dictionary<string, List<(string kind, string with)>> Ties = new Dictionary<string, List<(string, string)>>(StringComparer.Ordinal);
        public readonly Dictionary<string, bool> HasHome = new Dictionary<string, bool>(StringComparer.Ordinal), HasWork = new Dictionary<string, bool>(StringComparer.Ordinal);
        /// <summary>Everyone's stops, the cast's too, in ticks (brief 013: check-in marks read a post's shift, suspicion reads shared stops).</summary>
        /// <summary>Every routine leg's walk (place → place), built once at load (Warm), read-only after: a leg's route never depends on when
        /// it is walked, so building them up front changes no run, and the chime (58 clerks starting new legs on one tick) costs no frame.</summary>
        public readonly Dictionary<string, Path> Legs = new Dictionary<string, Path>(StringComparer.Ordinal);
        public void Warm(Site site)
        {
            foreach (var w in Walkers)
                for (int i = 0; i + 1 < w.Place.Length; i++)
                {
                    var a = w.Place[i]; var b = w.Place[i + 1]; if (a == "beyond" || b == "beyond") continue;
                    var key = a + "→" + b; if (Legs.ContainsKey(key)) continue;
                    Legs[key] = Leg(site, Places[a], Places[b]);
                }
        }
        internal static Path Leg(Site site, Pt pa, Pt pb)
        {
            var fa = site.Grid.NearestFree(pa) ?? pa; var fb = site.Grid.NearestFree(pb) ?? pb;
            var pts = new List<Pt> { pa };
            var route = site.Grid.Route(fa, fb);
            if (route != null) pts.AddRange(route); else pts.Add(fb);
            pts.Add(pb);
            return new Path(pts.ToArray());
        }
        public readonly Dictionary<string, List<(string place, int from, int to, bool outside)>> Stops = new Dictionary<string, List<(string, int, int, bool)>>(StringComparer.Ordinal);
        public Crowd(string json, Rules rules, ICollection<string> cast)
        {
            var n = JNode.Parse(json, "people");
            n.Only("schema", "from", "scope", "frame", "startsAt", "places", "people");
            if (n["schema"].Str() != "suborned.sim.people/1") throw new FormatException("people schema is not suborned.sim.people/1");
            StartsAt = n["startsAt"].Int();
            foreach (var kv in n["places"].Entries) Places[kv.Key] = Pt.Of(kv.Value);
            foreach (var p in n["people"].Items)
            {
                p.Only("id", "name", "job", "temperament", "need", "home", "work", "start", "ties", "stops");
                var ties = new List<(string, string)>(); foreach (var t in p["ties"].Items) ties.Add((t.Items[0].Str(), t.Items[1].Str()));
                Ties[p["id"].Str()] = ties; HasHome[p["id"].Str()] = p["home"].Int() == 1; HasWork[p["id"].Str()] = p["work"].Int() == 1;
                var id = p["id"].Str();
                var all = new List<(string, int, int, bool)>(); foreach (var sx in p["stops"].Items) { var x = sx.Items; all.Add((x[0].Str(), x[1].Int() * Game.Hz, x[2].Int() * Game.Hz, x[3].Int() == 1)); }
                Stops[id] = all;
                if (cast.Contains(id)) continue;
                var person = new Person { Id = id, Name = p["name"].Str(), Job = p["job"].Str(), Temperament = p["temperament"].Str(), Need = p["need"].Str() };
                rules.Temp(person.Temperament); rules.CheckNeed(person.Need);
                var st = p["start"].Ints(); for (int k = 0; k < 4; k++) person.Sc[k] = st[k];
                var stops = p["stops"].Items; var w = new Walker { P = person, Place = new string[stops.Count], From = new int[stops.Count], To = new int[stops.Count], Out = new bool[stops.Count] };
                for (int i = 0; i < stops.Count; i++)
                {
                    var s = stops[i].Items;
                    w.Place[i] = s[0].Str(); w.From[i] = s[1].Int() * Game.Hz; w.To[i] = s[2].Int() * Game.Hz; w.Out[i] = s[3].Int() == 1;
                    if (w.Place[i] != "beyond" && !Places.ContainsKey(w.Place[i])) throw new FormatException($"{id}: place {w.Place[i]} is unknown");
                }
                Walkers.Add(w);
            }
        }
    }

    public sealed partial class Game
    {
        public Crowd? People2;
        public readonly List<Walker> Walkers = new List<Walker>(); // this game's people, fresh from People2
        readonly Dictionary<string, Path> crowdPaths = new Dictionary<string, Path>(StringComparer.Ordinal);

        /// <summary>Where a person of the crowd is at this tick, or null if they are not on the street.</summary>
        public Pt? CrowdPos(Walker w) => CrowdPos(w, Tick);
        public Pt? CrowdPos(Walker w, int tick)
        {
            if (w.P.Down) return null;
            if (w.St.Length > 0 && tick == Tick) return ScatterPos(w);
            return RoutinePos(w, tick);
        }
        /// <summary>Where the routine puts a person of the crowd at this tick (no fight).</summary>
        Pt? RoutinePos(Walker w, int tick)
        {
            if (w.P.Down) return null;
            var c = People2!;
            for (int i = 0; i < w.Place.Length; i++)
            {
                if (tick >= w.From[i] && tick < w.To[i]) return w.Out[i] ? c.Places[w.Place[i]] : (Pt?)null;
                if (i + 1 < w.Place.Length && tick >= w.To[i] && tick < w.From[i + 1])
                {
                    if (w.Place[i] == "beyond" || w.Place[i + 1] == "beyond") return null;
                    var path = CrowdPath(w.Place[i], w.Place[i + 1]);
                    int span = w.From[i + 1] - w.To[i];
                    return span <= 0 ? path.End : path.At(path.Length * (tick - w.To[i]) / span);
                }
            }
            return null;
        }
        Path CrowdPath(string a, string b)
        {
            var key = a + "→" + b;
            var c = People2!;
            if (c.Legs.TryGetValue(key, out var p)) return p; // built at load (Crowd.Warm)
            if (crowdPaths.TryGetValue(key, out p)) return p;
            p = Crowd.Leg(S, c.Places[a], c.Places[b]); crowdPaths[key] = p; return p;
        }

        /// <summary>Everyone on the street who could witness: the cast by their role, then the crowd by id (Start.Crowd, Suture's T4 arm).</summary>
        IEnumerable<(Person p, Pt at)> OnTheStreet()
        {
            foreach (var p in People)
            {
                if (p == ValeP && (Vale == ValeAt.WithEnforcer || Vale == ValeAt.Taking)) continue; // the captive is no witness: he is the act
                var at = PersonPos(p); if (at.HasValue) yield return (p, at.Value);
            }
            if (People2 == null || !Start.Crowd) yield break;
            foreach (var w in Walkers) { var at = CrowdPos(w); if (at.HasValue) yield return (w.P, at.Value); }
        }

        /// <summary>The people within witness range of `at` on a clear sightline, but not `except`.</summary>
        List<Person> WitnessesOf(Pt at, Person? except, Person? also = null)
        {
            var seen = new List<Person>();
            foreach (var (p, wp) in OnTheStreet())
            {
                if (p == except || p == also) continue;
                if (G.Within(wp, at, witnessRange) && Sees(wp, at)) seen.Add(p);
            }
            return seen;
        }
    }
}
