// Brief 098 step 0, the dial (Sky 2026-11-07: "add 3 more guards, take some away... the game then just does that, no long processing, and I play
// and get a feel"). When the start sets counts (Start.Counts), the game builds that world before tick 0, from the seed and the dial's rules
// (contracts/rules/population.json, Population.cs); with none set this file does nothing and every run plays and hashes as before.
//  · fewer: people above a kind's count leave by a stable order (a hash of the id, the same every seed), never a tenet: the mission's cast and
//    anyone a cast member's lover tie names. Guards leave off-post ones first, then a post's reliefs (the latest shift first: the one before
//    stands the post on to the end of the shift that left), then the posts themselves, so a post is staffed for its whole shift or not at all
//    (GDD §05: a post is one person's; a relief keeps its marks). The ties that named them are cut (LiveTies).
//  · more guards: on shift for the whole mission, on a beat of post points (GameDialPeople.cs), checking in every check-in period from their
//    shift's start (PostOf reads BeatPost): staff under every rule that reads a job in witness.staff.
//  · more people: passers-by who come in at the site's edges at seeded times, go to a seat, a door or another edge, linger and leave.
// Every added person is a walker like the people file's: they see by their cone (a guard all round), witness, report by their nature, scatter
// at shots and block sight. Their draws are pure functions of (seed, id, stage, index): adding more never moves anyone already there.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>The dial's rules when the start sets counts; null otherwise (the dial is off and nothing here is read or hashed).</summary>
        PopulationRules? dial;
        /// <summary>Brief 098 step 0 (host read): per kind, what the start asked, what plays, who left and who was added. Empty when the start sets no counts.</summary>
        public readonly List<KindCensus> Census = new List<KindCensus>();
        /// <summary>The dial is on: the start set counts.</summary>
        public bool DialOn => dial != null;
        readonly Dictionary<string, Pt> xPlaces = new Dictionary<string, Pt>(StringComparer.Ordinal), xFaces = new Dictionary<string, Pt>(StringComparer.Ordinal);
        readonly Dictionary<string, List<(string place, int from, int to, bool outside)>> xStops = new Dictionary<string, List<(string, int, int, bool)>>(StringComparer.Ordinal);
        readonly Dictionary<string, (int from, int to)> beats = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        Dictionary<string, List<(string kind, string with)>>? liveTies;
        /// <summary>Everyone's ties as this game holds them: the people file's, less the ties to anyone the dial took away.</summary>
        Dictionary<string, List<(string kind, string with)>> LiveTies => liveTies ?? People2!.Ties;
        /// <summary>The start hash's share of the dial: its rules' text when the start sets counts, else nothing (every other start hashes as before).</summary>
        ulong DialHash() => dial == null ? 0UL : unchecked(dial.TextHash * 23UL ^ (TrafficAsked > 0 ? dial.Lanes!.TextHash * 29UL : 0UL)); // brief 098 step 1: and the lanes the cars drive, when the start sets any
        /// <summary>Where a crowd place is: the people file's, else one the dial found in the site (a post point, an edge).</summary>
        internal Pt CrowdPlace(string id) => People2!.Places.TryGetValue(id, out var p) ? p : xPlaces[id];
        internal bool TryCrowdPlace(string id, out Pt p) => People2!.Places.TryGetValue(id, out p) || xPlaces.TryGetValue(id, out p);
        /// <summary>A person's stops as this game holds them: a post the dial stood on (a survivor's) or an added person's, else the people file's.</summary>
        bool StopsBase(string id, out List<(string place, int from, int to, bool outside)> s) => xStops.TryGetValue(id, out s!) || People2!.Stops.TryGetValue(id, out s!);
        /// <summary>An added guard's post is his beat: where his beat has him at `tick` (else his first point), from his shift's start to its end.</summary>
        (string place, Pt at, int from, int to)? BeatPost(Person p, int tick)
        {
            var (from, to) = beats[p.Id]; if (to <= tick) return null;
            var w = WalkerOf(p); var at = w != null ? RoutineAt(w, Math.Max(tick, from)) ?? CrowdPlace(w.Place[0]) : default;
            return ("beat:" + p.Id, at, from, to);
        }

        /// <summary>Brief 098 (host read; nothing hashed): a person's stops as this game holds them, in ticks (an added person's, a survivor's that the dial
        /// stood on, else the people file's); null for the cast's roles and the unknown.</summary>
        public IReadOnlyList<(string place, int from, int to, bool outside)>? StopsNow(string id) => PersonById(id) is Person p && WalkerOf(p) != null ? StopsOf(p) : null;
        /// <summary>Brief 098 (host read): the post a staff person holds at `tick` (a staffed door by the people file's stops, the cast guard's all mission, an
        /// added guard's beat as "beat:&lt;id&gt;"), with its shift in ticks; null for anyone else. Their check-in marks run from `from` every check-in period.</summary>
        public (string place, int from, int to)? PostNow(string id, int tick) => PersonById(id) is Person p && PostOf(p, tick) is var q && q.HasValue ? (q.Value.place, q.Value.from, q.Value.to) : null;

        /// <summary>Brief 098 (host read): a person's ties as this game holds them (the people file's, less any to someone the dial took away).</summary>
        public IReadOnlyList<(string kind, string with)> TiesNow(string id) => People2 != null && LiveTies.TryGetValue(id, out var t) ? t : (IReadOnlyList<(string, string)>)Array.Empty<(string, string)>();

        /// <summary>This game's walkers: the people file's, fresh; with counts set, fewer or more by kind (the constructor's first act on the crowd).</summary>
        void InitWalkers(Crowd? crowd, PopulationRules? pop)
        {
            if (Start.Counts.Count == 0) { if (crowd != null) foreach (var w in crowd.Walkers) Walkers.Add(w.Fresh()); return; }
            if (crowd == null || Range) throw new FormatException("start.counts: the dial builds a mission's people, and this game has no people file (brief 098)");
            dial = pop ?? throw new FormatException($"start.counts: the dial needs its rules ({Population.RulesFile}); this host loaded none (brief 098)");
            var cast = new HashSet<string>(StringComparer.Ordinal); foreach (var c in S.Cast) cast.Add(c.Id);
            var tenets = new HashSet<string>(cast); // the cast, and anyone a cast member's lover tie names (the tie a tenet stands on)
            foreach (var id in cast) if (crowd.Ties.TryGetValue(id, out var ts)) foreach (var (k, with) in ts) if (k == "lover") tenets.Add(with);
            foreach (var c in S.Cast) if (c.Lover.Length > 0) foreach (var o in S.Cast) if (o.Role == c.Lover) tenets.Add(o.Id);
            var fresh = new List<Walker>(); foreach (var w in crowd.Walkers) fresh.Add(w.Fresh());
            var gone = new HashSet<string>(StringComparer.Ordinal);
            foreach (var kind in Population.PeopleKinds)
            {
                var d = dial.Kinds[kind]; var c = new KindCensus { Kind = kind, Max = d.Max };
                var mine = fresh.FindAll(w => Population.KindOf(dial, w.P.Job) == kind);
                foreach (var cr in S.Cast) if (Population.KindOf(dial, Population.CastJob(cr.Role)) == kind) { c.Today++; c.Floor++; }
                c.Today += mine.Count; foreach (var w in mine) if (tenets.Contains(w.P.Id)) c.Floor++;
                int want = c.Today;
                if (Start.Counts.TryGetValue(kind, out var asked))
                {
                    c.Asked = asked; want = Math.Max(c.Floor, Math.Min(d.Max, asked));
                    if (asked < c.Floor) c.Note = $"{c.Floor} is the fewest: the cast never leave";
                    else if (asked > d.Max) c.Note = $"{d.Max} is the most the dial allows";
                }
                c.Count = want;
                if (want < c.Today) foreach (var w in LeaveOrder(mine, tenets, cast, want < c.Today ? c.Today - want : 0, crowd)) { gone.Add(w.P.Id); c.Removed.Add(w.P.Id); }
                Census.Add(c);
            }
            foreach (var w in fresh) if (!gone.Contains(w.P.Id)) Walkers.Add(w);
            if (gone.Count > 0)
            {
                liveTies = new Dictionary<string, List<(string, string)>>(StringComparer.Ordinal);
                foreach (var kv in crowd.Ties) if (!gone.Contains(kv.Key)) liveTies[kv.Key] = kv.Value.FindAll(t => !gone.Contains(t.with));
            }
            foreach (var c in Census) { var d = dial.Kinds[c.Kind]; int add = c.Count - c.Today; for (int k = 1; k <= add; k++) c.Added.Add(AddPerson(d, k, crowd)); }
            CensusCars(); // brief 098 step 1: the traffic's count (GameTraffic.cs); the cars themselves start at the end of the constructor
        }

        /// <summary>Who of one kind leaves, `n` of them, in the dial's order: never a tenet; off-post first, then reliefs (the latest shift first), then
        /// holders; each tier by the hash of id/salt, ties by id. A post team cut to fewer: its latest remaining member stands the post to the team's end.</summary>
        List<Walker> LeaveOrder(List<Walker> mine, HashSet<string> tenets, HashSet<string> cast, int n, Crowd crowd)
        {
            var l = new List<Walker>(); if (n <= 0) return l;
            int over = T(S.Over);
            // the posts on the street in the mission: a staff person's stops at a staffed door or beside one (the people file's beside), out of doors
            string? DoorOf(string place) { if (crowd.Beside.TryGetValue(place, out var dr)) place = dr; foreach (var (id, _, kind) in S.Refuges) if (kind == "door" && id == "door:" + place) return place; return null; }
            var team = new SortedDictionary<string, List<(Walker w, int from, int to)>>(StringComparer.Ordinal); var held = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in S.Cast) if (cast.Contains(c.Id) && R.StaffJobs.Contains(Population.CastJob(c.Role)) && crowd.Stops.TryGetValue(c.Id, out var cs)) foreach (var s in cs) if (s.to > 0 && s.from < over && DoorOf(s.place) is string dc) held.Add(dc); // the cast guard holds his post all mission
            foreach (var w in mine)
            {
                if (!R.StaffJobs.Contains(w.P.Job)) continue;
                for (int i = 0; i < w.Place.Length; i++)
                {
                    if (!w.Out[i] || w.To[i] <= 0 || w.From[i] >= over || !(DoorOf(w.Place[i]) is string door) || held.Contains(door)) continue;
                    if (!team.TryGetValue(door, out var tl)) team[door] = tl = new List<(Walker, int, int)>();
                    int k = tl.FindIndex(x => x.w == w);
                    if (k < 0) tl.Add((w, w.From[i], w.To[i])); else tl[k] = (w, Math.Min(tl[k].from, w.From[i]), Math.Max(tl[k].to, w.To[i]));
                }
            }
            uint key(Walker w) => Streams.Fnv1a32(w.P.Id + "/" + dial!.LeaveSalt);
            int byKey(Walker a, Walker b) { int c = key(a).CompareTo(key(b)); return c != 0 ? c : string.CompareOrdinal(a.P.Id, b.P.Id); }
            var free = new List<Walker>(); var reliefs = new List<(Walker w, int from)>(); var holders = new List<Walker>();
            foreach (var w in mine)
            {
                if (tenets.Contains(w.P.Id)) continue;
                string? door = null; foreach (var kv in team) if (kv.Value.Exists(x => x.w == w)) door = kv.Key;
                if (door == null) { free.Add(w); continue; }
                var tl = team[door]; tl.Sort((a, b) => a.from != b.from ? a.from.CompareTo(b.from) : string.CompareOrdinal(a.w.P.Id, b.w.P.Id));
                if (tl[0].w == w) holders.Add(w); else reliefs.Add((w, tl.Find(x => x.w == w).from));
            }
            free.Sort(byKey); holders.Sort(byKey);
            reliefs.Sort((a, b) => a.from != b.from ? b.from.CompareTo(a.from) : byKey(a.w, b.w));
            foreach (var w in free) if (l.Count < n) l.Add(w);
            foreach (var (w, _) in reliefs) if (l.Count < n) l.Add(w);
            foreach (var w in holders) if (l.Count < n) l.Add(w);
            // a team that lost members keeps its post: the latest one left stands it to the end of the team's last shift
            foreach (var kv in team)
            {
                var left = kv.Value.FindAll(x => !l.Contains(x.w)); if (left.Count == 0 || left.Count == kv.Value.Count) continue;
                int end = 0; foreach (var x in kv.Value) end = Math.Max(end, x.to);
                var last = left[left.Count - 1].w; StandOn(last, kv.Key, end, crowd);
            }
            return l;
        }
        /// <summary>A survivor stands his post on to `end`: his last stop at the door (or beside it) runs to `end`; what he did meanwhile is dropped.</summary>
        void StandOn(Walker w, string door, int end, Crowd crowd)
        {
            int k = -1; for (int i = 0; i < w.Place.Length; i++) if (w.Out[i] && (w.Place[i] == door || (crowd.Beside.TryGetValue(w.Place[i], out var b) && b == door))) k = i;
            if (k < 0 || w.To[k] >= end) return;
            var pl = new List<string>(); var f = new List<int>(); var t = new List<int>(); var o = new List<bool>();
            for (int i = 0; i < w.Place.Length; i++)
            {
                if (i > k && w.From[i] < end) continue;
                pl.Add(i == k ? door : w.Place[i]); f.Add(w.From[i]); t.Add(i == k ? end : w.To[i]); o.Add(w.Out[i]);
            }
            w.Place = pl.ToArray(); w.From = f.ToArray(); w.To = t.ToArray(); w.Out = o.ToArray();
            var st = new List<(string, int, int, bool)>(); for (int i = 0; i < w.Place.Length; i++) st.Add((w.Place[i], w.From[i], w.To[i], w.Out[i]));
            xStops[w.P.Id] = st;
        }

        /// <summary>A start draw for an added person: their own stream for the stage (at seed 0 the stream of their id alone, so the neutral start spreads them too).</summary>
        uint XDraw(string id, string stage, int index = 1) => Streams.Nth(StreamKey(id, stage), index);
        int XBand(string id, string stage, int min, int max, int index = 1) => min + (int)(XDraw(id, stage, index) % (uint)(max - min + 1));
    }
}
