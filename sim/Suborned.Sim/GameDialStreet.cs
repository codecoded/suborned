// Brief 098 step 1b (Sky 2026-11-07: "people counter is for street"): the people kind counts people on the street, in the zone and out of doors,
// at a time on average over the mission's clock (0:00 to the close), not everyone in the zone. Read from the routines the game will play, so the
// number is the street's, not a guess: a person's street time is the ticks their stops and legs have them out of doors inside the window (the
// people file's, with this game's seeded shift of their day; an added passer-by's own visit; the cast by the people file's stops for them).
//  · today: the people file's street at the neutral start (Population.Today; about 19 people, of the zone's 114).
//  · more: passers-by are added in their id order, each with their own visit, until the street holds the count asked on average (the nearest a
//    whole passer-by allows); a passer-by whose visit misses the mission's clock altogether is not added (they would never be seen).
//  · fewer: street time is taken away by the dial's fixed order (population.fewer: a hash of the id), never a tenet (the cast, a lover's tie),
//    and never someone whose routine keeps them indoors all mission (they change nothing on the street): those in the order whose routine brings
//    them out leave, one by one, until the street holds the count asked (the nearest a whole person allows). The cast is never removed, so the
//    fewest is the street time the cast makes.
//  · the count asked of today's own number plays today's world exactly.
// Each step is a prefix of a fixed order (passers-by by id, leavers by the hash), so asking more never takes anyone away and asking fewer never
// brings anyone back. Integers only (ticks).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>Street time: the ticks of the mission's clock [0, over) a routine has a person out of doors inside the window.</summary>
    public static class Street
    {
        /// <summary>A routine's street ticks. Stops are (place, from, to, outside) in ticks, in order; a stop out of doors at a place inside the
        /// window counts whole; a leg between two stops (not "beyond") counts whole when both ends lie inside the window (the walk grid's route
        /// stays on it), else tick by tick where `legAt` puts the person inside it. The first stop or leg a tick falls in is the one that counts,
        /// as the crowd's routine reads them (GameCrowd.RoutineAt).</summary>
        public static long Ticks(IReadOnlyList<(string place, int from, int to, bool outside)> stops, int over, Grid g, Func<string, Pt> placeOf, Func<int, int, int, Pt> legAt)
        {
            long n = 0; int cur = 0;
            void Span(int a, int b, Func<int, bool>? at) { a = Math.Max(Math.Max(a, cur), 0); b = Math.Min(b, over); if (b <= a) return; if (at == null) n += b - a; else for (int t = a; t < b; t++) if (at(t)) n++; cur = b; }
            void Skip(int a, int b) { a = Math.Max(Math.Max(a, cur), 0); b = Math.Min(b, over); if (b > a) cur = b; }
            for (int i = 0; i < stops.Count; i++)
            {
                var s = stops[i];
                if (s.outside && s.place != "beyond" && g.InGrid(placeOf(s.place))) Span(s.from, s.to, null); else Skip(s.from, s.to);
                if (i + 1 >= stops.Count) continue;
                var nx = stops[i + 1]; if (s.place == "beyond" || nx.place == "beyond") { Skip(s.to, nx.from); continue; }
                if (g.InGrid(placeOf(s.place)) && g.InGrid(placeOf(nx.place))) Span(s.to, nx.from, null);
                else { int k = i, t0 = s.to; Span(s.to, nx.from, t => g.InGrid(legAt(k, t - t0, nx.from - t0))); }
            }
            return n;
        }
    }

    public sealed partial class Game
    {
        /// <summary>The shift SeedStart gives a walker's day (ticks): their own seeded phase, none for an added person, a post or a round's keeper,
        /// or at seed 0. A pure function of (seed, id): the dial reads it before SeedStart applies it.</summary>
        int PhaseShift(Walker w) => Seed == 0 || w.Added || (R.Seeds.StaffFixed && (R.StaffJobs.Contains(w.P.Job) || ServerOf(w.P) != null)) ? 0 : T(DrawBand(w.P.Id, "phase", R.Seeds.CrowdPhaseS)); // IsStaff's job test: a walker is never the cast's guard (and the cast is not yet cast when the dial reads this)
        /// <summary>A walker's stops as SeedStart will shift them (`k` ticks): every start after the first and every end before the last.</summary>
        static List<(string place, int from, int to, bool outside)> Shifted(Walker w, int k)
        {
            var l = new List<(string, int, int, bool)>(w.Place.Length);
            for (int i = 0; i < w.Place.Length; i++) l.Add((w.Place[i], k != 0 && i >= 1 ? Math.Max(0, w.From[i] + k) : w.From[i], k != 0 && i + 1 < w.Place.Length ? Math.Max(1, w.To[i] + k) : w.To[i], w.Out[i]));
            return l;
        }
        /// <summary>A routine leg's walk as the crowd walks it from the start's standing cars (CrowdPath's home leg), built once.</summary>
        Path HomeLeg(string a, string b)
        {
            var key = a + "→" + b;
            if (People2!.Legs.TryGetValue(key, out var p) || crowdPaths.TryGetValue(key, out p)) return p;
            p = Crowd.Leg(S, CrowdPlace(a), CrowdPlace(b), People2.Home(S, R)); crowdPaths[key] = p; return p;
        }
        /// <summary>A walker's street ticks this game: their routine with this game's shift of their day.</summary>
        long StreetTicks(Walker w) => StreetTicks(w, Shifted(w, PhaseShift(w)));
        long StreetTicks(Walker w, List<(string place, int from, int to, bool outside)> st) =>
            Street.Ticks(st, T(S.Over), S.Grid, CrowdPlace, (i, t, span) => { var path = HomeLeg(st[i].place, st[i + 1].place); return span <= 0 ? path.End : path.At(LegMm(w, path, t, span)); });
        /// <summary>A cast member's street ticks by the people file's stops for them (the cast is played by role; the file is their routine).</summary>
        long CastStreetTicks(string id) => People2!.Stops.TryGetValue(id, out var st) ? StreetTicks(new Walker { P = new Person { Id = id } }, st) : 0;

        /// <summary>Fewer or more people on the street (the people kind, measure street): fills the kind's census and adds `gone` its leavers; the
        /// passers-by it needs are added later (AddStreet), once the leavers are gone.</summary>
        void StreetCensus(DialKind d, KindCensus c, List<Walker> mine, HashSet<string> tenets, HashSet<string> cast, Crowd crowd, HashSet<string> gone) // c.Total and TotalToday hold the zone's count already
        {
            int over = T(S.Over); long sum = 0, floor = 0; var ticks = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var w in mine) { long n = StreetTicks(w); ticks[w.P.Id] = n; sum += n; if (tenets.Contains(w.P.Id)) floor += n; }
            long castTicks = 0; foreach (var cr in S.Cast) if (Population.KindOf(dial!, Population.CastJob(cr.Role)) == d.Kind) { long n = CastStreetTicks(cr.Id); sum += n; floor += n; castTicks += n; }
            c.CastStreet10 = (int)((castTicks * 20 + over) / (2L * over));
            c.Today = (int)((Population.StreetToday(S, crowd, R, dial!, d.Kind).sum * 2 + over) / (2L * over)); c.Floor = (int)((floor * 2 + over) / (2L * over)); c.Max = d.Max; // today's number is the panel's (the neutral start's)
            int want = c.Today; if (Start.Counts.TryGetValue(d.Kind, out var asked)) { c.Asked = asked; want = Math.Max(c.Floor, Math.Min(d.Max, asked)); }
            if (c.Asked < c.Floor) c.Note = $"{c.Floor} is the fewest on the street: the cast never leave"; else if (c.Asked > d.Max) c.Note = $"{d.Max} is the most the dial allows on the street";
            c.Count = want; streetSum = sum; streetWant = c.Asked.HasValue && want != c.Today ? (long)want * over : -1;
            if (streetWant >= 0 && want < c.Today)
                foreach (var w in LeaveOrder(mine, tenets, cast, mine.Count, crowd)) // the dial's fixed order; those who never come out change nothing on the street and stay
                {
                    long n = ticks[w.P.Id]; if (n == 0) continue;
                    if (2 * (streetSum - streetWant) <= n) break; // taking this one away would leave the street further from the count than keeping them
                    streetSum -= n; gone.Add(w.P.Id); c.Removed.Add(w.P.Id);
                }
        }
        long streetSum, streetWant = -1; // the street ticks summed over the mission so far, and the count asked × the clock (-1: no change)
        /// <summary>The passers-by the street needs (after the leavers have gone): in id order, each with their own visit, while adding them brings the
        /// street nearer the count asked; one whose visit misses the mission's clock is skipped (never seen).</summary>
        void AddStreet(DialKind d, KindCensus c, Crowd crowd)
        {
            if (streetWant < 0 || streetSum >= streetWant) { StreetDone(c); return; }
            for (int k = 1; k <= Population.CountCap && streetSum < streetWant; k++)
            {
                var w = MakePerson(d, k, crowd, out var stops); long n = StreetTicks(w);
                if (n == 0) continue;
                if (2 * (streetWant - streetSum) <= n) break; // this one would overshoot by more than they bring
                Register(w, stops); streetSum += n; c.Added.Add(w.P.Id);
                if (c.Added.Count >= StreetAddCap) { c.Note = $"{c.Added.Count} passers-by is the most the dial adds"; break; }
            }
            StreetDone(c);
        }
        void StreetDone(KindCensus c) { int over = T(S.Over); c.Street10 = (int)((streetSum * 20 + over) / (2L * over)); c.Total = c.TotalToday - c.Removed.Count + c.Added.Count; }
        /// <summary>The most passers-by the dial adds for a street count (a hopeless count still starts in time).</summary>
        const int StreetAddCap = 4000;
    }
}
