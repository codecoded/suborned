// Brief 098 L9, a cast member's place (Pulse's proposal, briefs/098/pulse/test-cleanup.md item 6; engine 0.48.0): nobody stops on a place a
// cast member holds. The cast hold their places on the street through the mission by their roles: the door guard his post at the Office
// entrance, the waitress hers at the café door (her round's post), the driver his stand by the car. L9 never moves them, so a routine whose
// stop was one of those places waited beside the holder for as long as they stood there (the dial's passers-by going into the café and the
// Office, the city's clerks at the Office door at the shift change).
//  · a door with a cast stand within its reach (two radii and a step: the distance a person goes in from, GameCrowd.Leaving) is held. Whoever
//    goes in or comes out there does so from beside the holder: at the nearest free point within that reach that stands personal space
//    (bodies.space) clear of every cast stand and of the door's own waiting points (where its posts' reliefs stand: the people file's
//    `beside`), as the dial's beat points keep doorOutCm off a door's own post. With no such point the door is shut to the dial while it is
//    held (GameDialPeople.Passing: the draw falls to a seat or the way through); the city's own routine keeps the door's point.
//  · a chair a cast member's stops name over a stay is not free (GameListenSpot.ChairFree).
//  · no beat point stands within personal space of a cast stand (Population.SitePoints).
// Worked out once at load from the site and the people file (Crowd.Warm), the same for every game on them; with L9 off, nothing changes.
// Integer cm, a fixed candidate order.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public static class CastStands
    {
        /// <summary>Where the cast stand on the street through the mission by their roles: the door guard's post, the waitress's post (her
        /// round's, else her place at the café door), the driver's stand by his car. The councillor holds none (indoors, then on his walk).</summary>
        public static List<Pt> Of(Site s)
        {
            var l = new List<Pt> { s.Stand(s.GuardPost) };
            var w = s.Cast.Find(c => c.Role == "waitress");
            if (w != null && s.Rounds != null && s.Rounds.Servers.TryGetValue(w.Id, out var sv)) l.Add(sv.At); else l.Add(s.Stand(s.GuardCafe));
            l.Add(s.Stand(s.CarDriver));
            return l;
        }

        /// <summary>A held door's going-in point: on rings round the door's point 10 cm apart out to `reachCm`, each in the 16 directions' order,
        /// the first free point on the holder's ground that stands `clearCm` or more from every point in `held` (the cast stands, the door's
        /// waiting points); null when there is none.</summary>
        public static Pt? GoingIn(Grid g, Pt door, Pt holder, IReadOnlyList<Pt> held, int clearCm, int reachCm)
        {
            var on = g.NearestFree(holder); if (!on.HasValue) return null; int piece = g.PieceOf(on.Value);
            for (int r = 0; r <= reachCm; r += RingStepCm)
                for (int d = 0; d < (r == 0 ? 1 : 16); d++)
                {
                    var p = door + new Pt(DirX[d] * r / 1000, DirY[d] * r / 1000);
                    if (!g.Free(p) || g.PieceOf(p) != piece) continue;
                    bool clear = true; foreach (var h in held) if (G.Within(p, h, clearCm - 1)) { clear = false; break; }
                    if (clear) return p;
                }
            return null;
        }
        const int RingStepCm = 10;
        // the law's 16 directions (GameBodyForms.cs), per mille, from 0° in 22.5° steps
        static readonly int[] DirX = { 1000, 924, 707, 383, 0, -383, -707, -924, -1000, -924, -707, -383, 0, 383, 707, 924 };
        static readonly int[] DirY = { 0, 383, 707, 924, 1000, 924, 707, 383, 0, -383, -707, -924, -1000, -924, -707, -383 };

        /// <summary>The doors of the people file a cast stand holds (an entrance of the site that is a place of the file, with a cast stand
        /// within `reachCm` of its point), each with its going-in point; `shut` gets those with none.</summary>
        public static SortedDictionary<string, Pt> Doors(Site s, Crowd crowd, int clearCm, int reachCm, SortedSet<string> shut)
        {
            var into = new SortedDictionary<string, Pt>(StringComparer.Ordinal); var stands = Of(s);
            foreach (var e in s.Entrances)
            {
                if (!crowd.Places.TryGetValue(e.id, out var door)) continue;
                Pt? holder = null; long hd = long.MaxValue; foreach (var st in stands) { long d2 = G.D2(st, door); if (d2 <= (long)reachCm * reachCm && d2 < hd) { hd = d2; holder = st; } }
                if (!holder.HasValue) continue; // nobody of the cast holds it: the door's own point
                var held = new List<Pt>(stands); foreach (var kv in crowd.Beside) if (kv.Value == e.id) held.Add(crowd.Places[kv.Key]);
                if (GoingIn(s.Grid, door, holder.Value, held, clearCm, reachCm) is Pt p) into[e.id] = p; else shut.Add(e.id);
            }
            return into;
        }

        /// <summary>A post point within `clearCm` of a cast stand moves to the nearest free point by walk on the same ground that clears every
        /// cast stand (within twice `clearCm`), else it is no post point (null).</summary>
        public static Pt? Clear(Site s, Pt at, int clearCm, Func<Pt, bool> ground)
        {
            var stands = Of(s); bool near(Pt p) { foreach (var st in stands) if (G.Within(p, st, clearCm - 1)) return true; return false; }
            if (clearCm <= 0 || !near(at)) return at;
            return s.Grid.Nearest(at, c => ground(c) && !near(c), 2 * clearCm);
        }
    }
}
