// Who notices what (brief 054 §3, Motive rev 4; Sky 2026-10-24: "Ordinary people shouldn't really care unless there's high alert status -
// think real world. And yeah, would be in cone area only for seeing"; engine 0.28.0; rulebook perception.people, witness.notice).
//  · Notices(person, at, point, act): the one test every civilian witness path and every card that counts people reads. With
//    perception.people.sees cone: a civilian sees by their facing cone (reach witness-range-m, half people-half-cm, no near sense) on a
//    clear line, the odd acts only from people-care-alert, the never acts never, the heard acts (gunfire, a scream) all round within reach on
//    a line; security (the door guard, a job in witness.staff) all round within reach on a line, every act, every Alert, as before.
//    Off (around, or a rulebook pinned before it): within reach on a line, as 0.27.0.
//  · FacingOf / PersonFacing: how a person faces now: walking a leg, the way they walk; at a stop, the stop's facing (the people file's faces,
//    resolved here against the site: a seat's chair, out of a door, toward a unit's door, toward the kerb); early at a leg's end, the stop's;
//    startled, toward what startled them; the cast by their own rules (a server's round, the guard's post, the driver and Vale their car).
//  · Host reads: PersonCone(id) (the shape a person sees by now: wedge, ring or none) and NoticesNow(id, act) (would they notice the act if
//    it happened in their shape now), for brief 054 §3.1's hover. Read-only, nothing hashed.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>A person's sight now, for the host's hover (brief 054 §3.1): Kind "wedge" (a civilian's cone: At, Facing, Reach, HalfCm at the
    /// reach), "ring" (security: all round within Reach), "none" (not on the street, or no facing). NearCm (brief 054 §9.1, engine 0.29.0): a
    /// civilian's near sense for a body (witness.notice.near, 150), whatever the facing, so it is set on a civilian with no facing too; 0 for
    /// security, off the street, or with no near row. The host draws it as a ring rim at the feet only while the armed order leaves or drops a body.</summary>
    public sealed class PersonSight { public string Person = "", Kind = "none"; public Pt At, Facing; public int Reach, HalfCm, NearCm; }

    public sealed partial class Game
    {
        /// <summary>Brief 054 §3.3: every on-street stop place's facing, resolved at the start (cone only); a stop with none is rejected.</summary>
        Dictionary<string, Pt>? stopFace;

        void ResolveStopFaces()
        {
            if (!R.PeopleCone || People2 == null || Range) return;
            var c = People2; stopFace = new Dictionary<string, Pt>(StringComparer.Ordinal);
            var places = new List<string>(c.Places.Keys); places.Sort(StringComparer.Ordinal);
            foreach (var id in places)
            {
                var at = c.Places[id];
                if (c.FaceVec.TryGetValue(id, out var v)) { stopFace[id] = v; continue; }
                if (!c.FaceRule.TryGetValue(id, out var rule)) continue;
                if (OffSheet(at)) continue; // off the site's window by more than witness range (the tram and sky rail stops): it sees nothing of the block
                Pt? f = null;
                if (rule == "door") { foreach (var e in S.Entrances) if ((G.Within(e.at, at, 300) || G.Within(e.stand, at, 300)) && e.stand != e.at) { f = e.stand - e.at; break; } if (!f.HasValue && !S.HasTakedownWorld) f = KerbFrom(at); } // a site without the world's entrances (a test fixture): the kerb
                else if (rule.StartsWith("to:", StringComparison.Ordinal)) { var to = c.Places[rule.Substring(3)]; if (to != at) f = to - at; }
                else if (rule == "kerb") f = KerbFrom(at);
                if (!f.HasValue) throw new FormatException($"people faces.{id}: '{rule}' finds no facing on this site (brief 054 §3.3: unknown = reject)");
                stopFace[id] = f.Value;
            }
            // every on-street stop has a facing (Pulse's stop facings; unknown = reject)
            foreach (var w in Walkers)
                for (int i = 0; i < w.Place.Length; i++)
                    if (w.Out[i] && w.Place[i] != "beyond" && !stopFace.ContainsKey(w.Place[i]) && !OffSheet(c.Places[w.Place[i]]))
                        throw new FormatException($"{w.P.Id}: stop {w.Place[i]} is on the street and has no facing (people faces; brief 054 §3.3: a stop with no facing is rejected)");
        }

        /// <summary>A point farther than witness range outside the site's walk grid: no sight from it reaches the block (brief 054 §3.3).</summary>
        bool OffSheet(Pt p)
        {
            var g = S.Grid; int x1 = g.X0 + g.Nx * g.Cell, y1 = g.Y0 + g.Ny * g.Cell;
            long dx = p.X < g.X0 ? g.X0 - p.X : p.X > x1 ? p.X - x1 : 0, dy = p.Y < g.Y0 ? g.Y0 - p.Y : p.Y > y1 ? p.Y - y1 : 0;
            return dx * dx + dy * dy > (long)witnessRange * witnessRange;
        }
        /// <summary>Toward the nearest carriageway cell (drive grid 'r') within 30 m of p, or null.</summary>
        Pt? KerbFrom(Pt p)
        {
            var d = S.Drive; int ci = d.CI(p.X), cj = d.CJ(p.Y), maxR = 3000 / Math.Max(1, d.Cell);
            for (int rr = 1; rr <= maxR; rr++)
            {
                Pt? best = null; long bd = long.MaxValue;
                for (int i = ci - rr; i <= ci + rr; i++)
                    for (int j = cj - rr; j <= cj + rr; j++)
                    {
                        if (Math.Max(Math.Abs(i - ci), Math.Abs(j - cj)) != rr || i < 0 || j < 0 || i >= d.Nx || j >= d.Ny) continue;
                        var q = d.Centre(i, j); if (d.SurfaceAt(q) != 'r') continue;
                        long dd = G.D2(p, q); if (dd < bd) { bd = dd; best = q; }
                    }
                if (best.HasValue && best.Value != p) return best.Value - p;
            }
            return null;
        }

        /// <summary>A walker's facing at tick t by its routine alone (a leg's heading, a stop's facing): the investigators' read after the fact.</summary>
        Pt? FacingAt(Walker w, int t)
        {
            if (stopFace == null) return null;
            for (int i = 0; i + 1 < w.Place.Length; i++)
                if (t >= w.To[i] && t < w.From[i + 1] && w.Place[i] != "beyond" && w.Place[i + 1] != "beyond")
                {
                    var path = CrowdPath(w.Place[i], w.Place[i + 1]); int span = w.From[i + 1] - w.To[i]; if (span <= 0) return null;
                    long lm = LegMm(w, path, t - w.To[i], span);
                    return lm >= path.Length ? StopFace(w.Place[i + 1]) : path.Heading(lm);
                }
            for (int i = 0; i < w.Place.Length; i++) if (t >= w.From[i] && t < w.To[i] && w.Out[i]) return StopFace(w.Place[i]);
            return null;
        }
        /// <summary>A stop's facing (cone only), or null.</summary>
        Pt? StopFace(string place) => stopFace != null && stopFace.TryGetValue(place, out var f) ? f : (Pt?)null;

        /// <summary>The cast's own facings (brief 054 §3.3): a server by her round, the door guard by his post or the café, the driver at his
        /// stand and Vale at the bay toward their car, Vale walking out the way he walks.</summary>
        Pt? CastFacing(Person p, Pt at)
        {
            if (ServerOf(p) is RoundsData.Server sv) { var q = RoundPoseAt(sv, EvRead(sv), Tick); return q.Face == new Pt(0, 0) ? (Pt?)null : q.Face; }
            if (p == GuardP) { var gp = GuardPose(); return gp.HasValue && gp.Value.facing != new Pt(0, 0) ? gp.Value.facing : (Pt?)null; }
            if (p == ValeP && Vale == ValeAt.Leaving && valePath != null)
            {
                int span = T(valeAtCarS) - T(valeExitsS); if (span <= 0) return null;
                return valePath.Heading(valePath.Length * Math.Min(span, Math.Max(0, Tick - T(valeExitsS))) / span);
            }
            if (p == Driver || p == ValeP) { var d = CarPos() - at; return d == new Pt(0, 0) ? (Pt?)null : d; }
            return null;
        }

        /// <summary>How the person faces now: FacingOf (a walker), else the cast's rule (cone only).</summary>
        Pt? PersonFacing(Person p, Pt at) => FacingOf(p) ?? (R.PeopleCone && WalkerOf(p) == null ? CastFacing(p, at) : null);

        /// <summary>Security for sight (brief 054 §3.4): the door guard and a job in witness.staff, on shift or off.</summary>
        bool SecurityPerson(Person p) => IsStaff(p);

        /// <summary>Brief 054 §3.2: would person p, standing at wp, notice `act` at `at` now? The engine's one test (cards read it too).</summary>
        bool Notices(Person p, Pt wp, Pt at, string act) => NoticesAt(p, wp, at, act, Alert, null);
        bool NoticesAt(Person p, Pt wp, Pt at, string act, int alert, Pt? facing)
        {
            if (!R.PeopleCone) return G.Within(wp, at, witnessRange) && Sees(wp, at);
            if (Array.IndexOf(Rules.NoticeActs, act) < 0) throw new InvalidOperationException($"Notices: '{act}' is no witness.notice act (unknown = reject)");
            if (R.NoticeHeard.Contains(act) || SecurityPerson(p)) return G.Within(wp, at, witnessRange) && Sees(wp, at);
            if (R.NoticeNever.Contains(act)) return false;
            if (R.NoticeOdd.Contains(act) && alert < R.PeopleCareAlert) return false;
            if (R.NoticeNear.Contains(act) && G.Within(wp, at, R.PeopleNearCm) && Sees(wp, at)) return true; // brief 054 §9.1: a body at your feet, any facing
            return InPeopleCone(p, wp, at, facing) && Sees(wp, at);
        }
        /// <summary>The civilian cone: within perception.people.reach and ±60° (half at the reach) of the facing; no facing, no sight.</summary>
        bool InPeopleCone(Person p, Pt wp, Pt at, Pt? facing = null)
        {
            if (!G.Within(wp, at, R.PeopleReachCm)) return false;
            var f = facing ?? PersonFacing(p, wp); if (!f.HasValue) return false;
            return G.InCone(wp, f.Value, R.PeopleReachCm, R.PeopleHalfCm, 0, at);
        }

        // ---------------------------------------------------------------- host reads (brief 054 §3.1), nothing hashed

        /// <summary>The shape person `id` sees by now (wedge, ring or none). Read-only.</summary>
        public PersonSight PersonCone(string id)
        {
            var s = new PersonSight { Person = id };
            var p = PersonById(id); if (p == null) return s;
            Pt? at = null; foreach (var (q, wp) in OnTheStreet()) if (q == p) { at = wp; break; }
            if (!at.HasValue) return s;
            s.At = at.Value; s.Reach = witnessRange;
            if (!R.PeopleCone || SecurityPerson(p)) { s.Kind = "ring"; return s; }
            s.NearCm = R.NoticeNear.Count > 0 ? R.PeopleNearCm : 0;
            var f = PersonFacing(p, at.Value); if (!f.HasValue) return s;
            s.Kind = "wedge"; s.Facing = f.Value; s.Reach = R.PeopleReachCm; s.HalfCm = R.PeopleHalfCm;
            return s;
        }
        /// <summary>Would person `id` notice `act` (a witness.notice act id) at point `at` now? The engine's own test. Read-only.</summary>
        public bool NoticesNow(string id, string act, Pt at)
        {
            var p = PersonById(id); if (p == null) return false;
            foreach (var (q, wp) in OnTheStreet()) if (q == p) return Notices(p, wp, at, act);
            return false;
        }
        /// <summary>Would person `id` notice `act` at all at the Alert now, wherever it happened in their shape (brief 054 §3.1: draw their cone
        /// on hover only when it matters)? False for a civilian and an odd act below people-care-alert, or a never act.</summary>
        public bool CaresAbout(string id, string act)
        {
            var p = PersonById(id); if (p == null) return false;
            if (!R.PeopleCone || R.NoticeHeard.Contains(act) || SecurityPerson(p)) return true;
            if (R.NoticeNever.Contains(act)) return false;
            return !(R.NoticeOdd.Contains(act) && Alert < R.PeopleCareAlert);
        }
    }
}
