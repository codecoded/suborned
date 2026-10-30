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
        public int PacePct = -1; // brief 023 (engine 0.8.2): the extra per cent this person walks a routine leg at (rulebook crowd.pace), from their id; -1 until first read
        public Walker Fresh()
        {
            var p = new Person { Id = P.Id, Name = P.Name, Job = P.Job, Temperament = P.Temperament, Need = P.Need, Role = "", Record = P.Record, Size = P.Size, Holder = P.Holder };
            Array.Copy(P.Sc, p.Sc, 4);
            return new Walker { P = p, Place = Place, From = From, To = To, Out = Out };
        }
    }

    public sealed class Crowd
    {
        public readonly List<Walker> Walkers = new List<Walker>();
        public readonly Dictionary<string, Pt> Places = new Dictionary<string, Pt>(StringComparer.Ordinal);
        public readonly int StartsAt;
        /// <summary>The need records (brief 017; sim people `needs.records`, from contracts/people/&lt;plan&gt;.needs.json): id → kind, label, size.</summary>
        public readonly SortedDictionary<string, (string kind, string label, string size)> Records = new SortedDictionary<string, (string, string, string)>(StringComparer.Ordinal);
        /// <summary>Everyone's need record, the cast's too: id → record, size, holder.</summary>
        public readonly Dictionary<string, (string record, string size, string holder)> RecordOf = new Dictionary<string, (string, string, string)>(StringComparer.Ordinal);
        /// <summary>The record a replaced need of `kind` takes (phase 1: the kind's one record, the first by id; "" if the catalogue has none).
        /// No draw: phase 1 moves no stream (brief 017: need.replaced draws records in phase 2).</summary>
        public string DefaultRecord(string kind) { foreach (var kv in Records) if (kv.Value.kind == kind) return kv.Key; return ""; }
        /// <summary>Everyone's ties, home and work, the cast's too (between missions: word along ties, need replacement's requires).</summary>
        public readonly Dictionary<string, List<(string kind, string with)>> Ties = new Dictionary<string, List<(string, string)>>(StringComparer.Ordinal);
        public readonly Dictionary<string, bool> HasHome = new Dictionary<string, bool>(StringComparer.Ordinal), HasWork = new Dictionary<string, bool>(StringComparer.Ordinal);
        /// <summary>Everyone's stops, the cast's too, in ticks (brief 013: check-in marks read a post's shift, suspicion reads shared stops).</summary>
        /// <summary>Every routine leg's walk (place → place), built once at load (Warm), read-only after: a leg's route never depends on when
        /// it is walked, so building them up front changes no run, and the chime (58 clerks starting new legs on one tick) costs no frame.</summary>
        public readonly Dictionary<string, Path> Legs = new Dictionary<string, Path>(StringComparer.Ordinal);
        public void Warm(Site site, Rules rules)
        {
            var home = Home(site, rules);
            foreach (var w in Walkers)
                for (int i = 0; i + 1 < w.Place.Length; i++)
                {
                    var a = w.Place[i]; var b = w.Place[i + 1]; if (a == "beyond" || b == "beyond") continue;
                    var key = a + "→" + b; if (Legs.ContainsKey(key)) continue;
                    Legs[key] = Leg(site, Places[a], Places[b], home);
                }
        }
        /// <summary>Engine 0.33.1 (rulebook car.footprint): the cells of the cars standing when the mission starts (Game.HomeCars), which every
        /// routine leg goes round; computed once per crowd (the same for every game on these inputs).</summary>
        public bool[]? Home(Site site, Rules rules) { if (!homeDone) { home = Game.HomeCars(site, rules); homeDone = true; } return home; }
        bool[]? home; bool homeDone;
        internal static Path Leg(Site site, Pt pa, Pt pb, bool[]? cars = null) => Leg(site, pa, pb, out _, cars);
        /// <summary>A routine leg: from pa onto the walk grid, the grid's route, and off it to pb. Engine 0.33.0 (brief 060, Suture's cause A): an
        /// end with no free cell within 6 m (off the window, as t1-records, or deep in a solid) joins the grid at the free cell nearest it with no
        /// cap (Grid.NearestFreeAny), so the leg walks the grid and leaves the window from there; 0.32.0 drew it as a straight line through the
        /// Records Office. An end whose cell lies on a pocket of free ground cut off from the other end's (a seat walled in by tables) joins
        /// the other's piece at its cell nearest that end. Only a leg with both ends off the window is still a straight line (routed false).</summary>
        public static Path Leg(Site site, Pt pa, Pt pb, out bool routed, bool[]? cars = null)
        {
            var g = site.Grid; bool both = !g.InGrid(pa) && !g.InGrid(pb);
            Pt? fa = both ? g.NearestFree(pa, cars) : g.NearestFreeAny(pa, cars), fb = both ? g.NearestFree(pb, cars) : g.NearestFreeAny(pb, cars);
            var pts = new List<Pt> { pa };
            if (fa.HasValue && fb.HasValue && g.PieceOf(fa.Value) != g.PieceOf(fb.Value)) // an end on a pocket of free ground (a seat walled in by tables): the smaller piece's end joins the larger's nearest cell
            {
                int ka = g.PieceOf(fa.Value), kb = g.PieceOf(fb.Value);
                if (g.PieceSize(kb) <= g.PieceSize(ka)) fb = g.NearestFreeIn(pb, ka); else fa = g.NearestFreeIn(pa, kb);
            }
            var route = fa.HasValue && fb.HasValue ? g.Route(fa.Value, fb.Value, cars) : null;
            routed = route != null;
            if (route != null) pts.AddRange(route); else pts.Add(fb ?? pb);
            pts.Add(pb);
            return new Path(pts.ToArray());
        }
        /// <summary>Brief 054 §3.3: the stop facings as the people file gives them: a direction (a seat), or a rule word (door, kerb, to:place).</summary>
        public readonly Dictionary<string, Pt> FaceVec = new Dictionary<string, Pt>(StringComparer.Ordinal);
        public readonly Dictionary<string, string> FaceRule = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly Dictionary<string, List<(string place, int from, int to, bool outside)>> Stops = new Dictionary<string, List<(string, int, int, bool)>>(StringComparer.Ordinal);
        /// <summary>Engine 0.34.0 (brief 067 L, Pulse): a post's waiting point and the staffed door it stands beside (the people file's `beside`).</summary>
        public readonly Dictionary<string, string> Beside = new Dictionary<string, string>(StringComparer.Ordinal);
        public Crowd(string json, Rules rules, ICollection<string> cast)
        {
            var n = JNode.Parse(json, "people");
            n.Only("schema", "from", "scope", "frame", "startsAt", "places", "faces", "facesFrame", "beside", "besideFrame", "needs", "people");
            if (n["schema"].Str() != "suborned.sim.people/2") throw new FormatException("people schema is not suborned.sim.people/2");
            // the need records (brief 017): every record's kind is a rules need and its size one the need kinds name (unknown = reject)
            var nd = n["needs"]; nd.Only("sizes", "records", "frame");
            var sizes = new HashSet<string>(StringComparer.Ordinal); foreach (var z in nd["sizes"].Items) sizes.Add(z.Str());
            foreach (var kv in nd["records"].Entries)
            {
                var x = kv.Value.Items; if (x.Count != 3) throw new FormatException($"{kv.Value.Path}: a record is [kind, label, size]");
                string kind = x[0].Str(), label = x[1].Str(), size = x[2].Str();
                rules.CheckNeed(kind);
                if (label.Length == 0) throw new FormatException($"{kv.Value.Path}: a record has a label");
                if (!sizes.Contains(size)) throw new FormatException($"{kv.Value.Path}: size '{size}' is not in needs.sizes (unknown = reject)");
                Records[kv.Key] = (kind, label, size);
            }
            StartsAt = n["startsAt"].Int();
            foreach (var kv in n["places"].Entries) Places[kv.Key] = Pt.Of(kv.Value);
            // brief 054 §3.3 (engine 0.28.0): how a person faces at each stop place; resolved against the site by Game (StopFaces)
            if (n.Opt("faces") is JNode fc)
                foreach (var kv in fc.Entries)
                {
                    if (!Places.ContainsKey(kv.Key)) throw new FormatException($"people faces: place '{kv.Key}' is not in places");
                    if (kv.Value.Kind == JKind.Str)
                    {
                        var f = kv.Value.Str();
                        if (f != "door" && f != "kerb" && !(f.StartsWith("to:", StringComparison.Ordinal) && Places.ContainsKey(f.Substring(3)))) throw new FormatException($"people faces.{kv.Key}: '{f}' is not door, kerb or to:<place> (unknown = reject)");
                        FaceRule[kv.Key] = f;
                    }
                    else { var v = Pt.Of(kv.Value); if (v == new Pt(0, 0)) throw new FormatException($"people faces.{kv.Key}: a facing can't be zero"); FaceVec[kv.Key] = v; }
                }
            if (n.Opt("beside") is JNode bs) // engine 0.34.0 (brief 067 L, Pulse): a post's waiting point and the staffed door it stands beside (Game.PostOf)
                foreach (var kv in bs.Entries)
                {
                    var door = kv.Value.Str();
                    if (!Places.ContainsKey(kv.Key) || !Places.ContainsKey(door)) throw new FormatException($"people beside.{kv.Key}: '{kv.Key}' and '{door}' are places of the file (unknown = reject)");
                    if (kv.Key == door) throw new FormatException($"people beside.{kv.Key}: a waiting point beside itself");
                    Beside[kv.Key] = door;
                }
            foreach (var p in n["people"].Items)
            {
                p.Only("id", "name", "job", "temperament", "need", "home", "work", "record", "size", "holder", "start", "ties", "stops");
                var ties = new List<(string, string)>(); foreach (var t in p["ties"].Items) ties.Add((t.Items[0].Str(), t.Items[1].Str()));
                Ties[p["id"].Str()] = ties; HasHome[p["id"].Str()] = p["home"].Int() == 1; HasWork[p["id"].Str()] = p["work"].Int() == 1;
                var id = p["id"].Str();
                string rec = p["record"].Str(), size = p["size"].Str(), holder = p["holder"].Str();
                if (!Records.TryGetValue(rec, out var r)) throw new FormatException($"{id}: need record '{rec}' is not in needs.records (unknown = reject)");
                if (r.kind != p["need"].Str()) throw new FormatException($"{id}: need record '{rec}' is a {r.kind} record, not {p["need"].Str()}");
                if (r.size != size) throw new FormatException($"{id}: size '{size}' is not record {rec}'s ({r.size})");
                RecordOf[id] = (rec, size, holder);
                var all = new List<(string, int, int, bool)>(); foreach (var sx in p["stops"].Items) { var x = sx.Items; all.Add((x[0].Str(), x[1].Int() * Game.Hz, x[2].Int() * Game.Hz, x[3].Int() == 1)); }
                Stops[id] = all;
                if (cast.Contains(id)) continue;
                var person = new Person { Id = id, Name = p["name"].Str(), Job = p["job"].Str(), Temperament = p["temperament"].Str(), Need = p["need"].Str(), Record = rec, Size = size, Holder = holder };
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
            // a holder is nobody on the sheet ("beyond") or a person of this file (phase 2 binds them)
            foreach (var kv in RecordOf) if (kv.Value.holder != "beyond" && !RecordOf.ContainsKey(kv.Value.holder)) throw new FormatException($"{kv.Key}: holder '{kv.Value.holder}' is not a person of the people file (unknown = reject)");
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
                    var path = CrowdPath(w.Place[i], w.Place[i + 1], w.To[i]);
                    int span = w.From[i + 1] - w.To[i];
                    return span <= 0 ? path.End : path.At(LegMm(w, path, tick - w.To[i], span, LeavesPost(w, i)));
                }
            }
            return null;
        }
        /// <summary>How far along a routine leg a person is, `t` ticks into a leg timed `span` (brief 023, engine 0.8.2; rulebook crowd.pace): each
        /// person walks it (100 + k) % of its timed pace, k = 0..pace from a hash of their id, so people who set out together string out; once
        /// at its end they wait there until the next stop starts (never late: the leg's own timing is the slowest).</summary>
        internal long LegMm(Walker w, Path path, long t, int span, bool leaving = false)
        {
            if (w.PacePct < 0) w.PacePct = PaceOf(w);
            long mm = path.Length * t * (100 + w.PacePct) / (100L * span);
            if (leaving) mm = Math.Max(mm, t * peopleWalk * (100 + w.PacePct) / 100L); // Motive's ruling (brief 080): an off-shift guard leaves directly, at the city's walk, and waits at the far end; a leg's timing is the stops' minutes, no one's pace
            return mm < path.Length ? mm : path.Length;
        }
        /// <summary>Crowd.staffDirect: this leg takes a guard off her post (she stood at a door or beside one and is bound elsewhere): she goes directly. A relief walking to her post keeps her timing.</summary>
        bool LeavesPost(Walker w, int i) => R.StaffLegDirect && !Start.OldClock && IsStaff(w.P) && IsPostPlace(w.Place[i]) && !IsPostPlace(w.Place[i + 1]);
        readonly Dictionary<string, bool> postPlace = new Dictionary<string, bool>(StringComparer.Ordinal);
        bool IsPostPlace(string place)
        {
            if (postPlace.TryGetValue(place, out var v)) return v;
            v = People2!.Beside.ContainsKey(place); if (!v) foreach (var (id, _, kind) in S.Refuges) if (kind == "door" && id == "door:" + place) { v = true; break; }
            postPlace[place] = v; return v;
        }
        /// <summary>A routine leg's walk, for a leg that sets out at tick `from`. Engine 0.33.1 (rulebook car.footprint): it goes round the cars
        /// standing when it sets out (CarEpochAt). While those are the mission's start cars (the getaway car at its first stand, the official car
        /// at the pickup), the leg built at load (Crowd.Warm, round those two); once a car stands elsewhere, that same leg if it crosses none of
        /// the cars standing then, else the leg walked again round them (kept for this game, by the cars' epoch).</summary>
        Path CrowdPath(string a, string b, int from)
        {
            var key = a + "→" + b;
            var c = People2!;
            var ep = CarEpochAt(from);
            if (!c.Legs.TryGetValue(key, out var p) && !crowdPaths.TryGetValue(key, out p)) { p = Crowd.Leg(S, c.Places[a], c.Places[b], c.Home(S, R)); crowdPaths[key] = p; } // built at load (Crowd.Warm), else once here
            if (ep.home) return p;
            var ek = ep.id.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + key;
            if (crowdPaths.TryGetValue(ek, out var q)) return q;
            q = Crosses(p, ep.mask) ? Crowd.Leg(S, c.Places[a], c.Places[b], ep.mask) : p;
            crowdPaths[ek] = q; return q;
        }
        /// <summary>Whether a walk passes over ground the mask's cars cover (sampled every 10 cm, as Grid.LineFree).</summary>
        bool Crosses(Path p, bool[]? m)
        {
            if (m == null) return false;
            var g = S.Grid;
            for (long mm = 0; mm <= p.Length; mm += 100) { var x = p.At(mm); int i = g.CI(x.X), j = g.CJ(x.Y); if (g.FreeCell(i, j) && m[i * g.Ny + j]) return true; }
            return false;
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
        /// <summary>Brief 054 §3.2: by the notice rule for `act` (a shot, a person shot and a car blown are heard: all round within reach).</summary>
        List<Person> WitnessesOf(Pt at, Person? except, string act, Person? also = null)
        {
            var seen = new List<Person>();
            foreach (var (p, wp) in OnTheStreet())
            {
                if (p == except || p == also) continue;
                if (Notices(p, wp, at, act)) seen.Add(p);
            }
            return seen;
        }
    }
}
