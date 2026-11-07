// Law L9's independent check (brief 098): every upright body on the street read the way the host reads it (op.Pos, PersonPos, PatrolPose,
// MemberPose, Hostile.Pos, SecOfficerPose, UnitPose, CrowdPos), never the law's own list, and every pair closer than two radii. The law's tests
// and `suborned-sim dial bodies` read it. Read-only: it moves nothing and is never hashed.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public static class BodyCheck
    {
        /// <summary>One upright body as the host draws it: id, where, its kind, and whether it is a tenet (the cast, the patrol, a person seated;
        /// the law never moves those).</summary>
        public readonly struct Upright { public readonly string Id, Kind; public readonly Pt At; public readonly bool Tenet; public Upright(string id, string kind, Pt at, bool tenet) { Id = id; Kind = kind; At = at; Tenet = tenet; } }

        public static List<Upright> Of(Game g)
        {
            var l = new List<Upright>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            void add(string id, string kind, Pt? at, bool tenet) { if (at.HasValue && g.S.Grid.InGrid(at.Value) && seen.Add(id)) l.Add(new Upright(id, kind, at.Value, tenet || g.BodyIsTenet(id))); }
            foreach (var op in g.Ops) if (!op.Inside && !op.InCar && !op.CarriedBy.HasValue && !op.Down && !op.LeftMap) add(Op.Name(op.Id), "cell", op.Pos, false);
            if (!g.Inez.Down) add(g.Inez.Id, "cast", g.PersonPos(g.Inez), true);
            if (!g.Driver.Down && g.DriverPos() is Pt dp && dp != g.CarPos()) add(g.Driver.Id, "cast", dp, true);
            if (!g.GuardDown) add(g.GuardP.Id, "cast", g.GuardPose()?.pos, true);
            if (!g.ValeP.Down && (g.Vale == ValeAt.Leaving || g.Vale == ValeAt.Pickup || g.Vale == ValeAt.WithEnforcer || g.Vale == ValeAt.Taking) && g.ValePos() is Pt vp && vp != g.CarPos()) add(g.ValeP.Id, "cast", vp, true);
            if (!g.PatrolDown) add("patrol", "patrol", g.PatrolPose()?.pos, true);
            foreach (var h in g.Hostiles) if (h.Present && !h.Down && h.Gun.St != "gone") add(h.Id, "fight", h.Pos(g.Tick), false);
            foreach (var m in g.Crew) if (!m.Fight && !m.Gun.Down && m.Mode != CrewMode.Aboard && m.Mode != CrewMode.Gone) add(m.Id, "rival", g.MemberPose(m)?.pos, false);
            for (int k = 0; k < g.SecGuns.Length; k++) add(g.SecGuns[k].Id, "officer", g.SecOfficerPose(k)?.pos, false);
            foreach (var u in g.Units) if (!u.Legacy && !u.Patrol && u.OnStreet(g.Tick)) add(u.Id, u.Robot ? "robot" : "police", g.UnitPose(u)?.pos, false);
            if (g.People2 != null && g.Start.Crowd) foreach (var w in g.Walkers) add(w.P.Id, w.Added ? (w.P.Job == "guard" ? "dial-guard" : "dial-person") : "crowd", g.CrowdPos(w), false);
            return l;
        }

        /// <summary>The fastest a body of this kind goes on its own feet, cm a tick (rounded up): the rulebook's run paces (Game.BodyPaceCmS: the
        /// cell's and the cast's run, the city's run, a rival's following pace, security's for officers and police, a robot's).</summary>
        public static int PaceCmT(Game g, string kind) => (g.BodyPaceCmS(kind) + Game.Hz - 1) / Game.Hz;
        /// <summary>A tick-to-tick move this far over its pace is rounding (integer cm off a walk in mm), not a jump.</summary>
        public const int RoundCm = 2;

        /// <summary>Law L9's "nobody jumps" (brief 098, Sky 2026-11-07: "the jumping back to spot should be better to dealt with"): each body upright on
        /// the street in two ticks running, read as the host reads it, moved no farther than its own pace (PaceCmT). One appearing (out of a door,
        /// a car, a refuge) or leaving the street has no move. Read-only; never hashed.</summary>
        public sealed class Moves
        {
            Dictionary<string, Pt> last = new Dictionary<string, Pt>(StringComparer.Ordinal), now = new Dictionary<string, Pt>(StringComparer.Ordinal);
            /// <summary>Moves looked at, jumps (over the pace), the longest move over the pace, and the first jumps (who, how far, when).</summary>
            public long Looks; public int Jumps, WorstOverCm; public readonly List<(string id, string kind, int cm, int pace, int tick, Pt from, Pt to)> First = new List<(string, string, int, int, int, Pt, Pt)>();
            public readonly SortedDictionary<string, int> ByKind = new SortedDictionary<string, int>(StringComparer.Ordinal);
            public void Look(Game g, List<Upright> up, int keep = 8)
            {
                now.Clear();
                foreach (var u in up)
                {
                    if (last.TryGetValue(u.Id, out var p))
                    {
                        Looks++; int d = G.Dist(u.At, p), pace = PaceCmT(g, u.Kind);
                        if (d > pace + RoundCm) { Jumps++; ByKind[u.Kind] = ByKind.GetValueOrDefault(u.Kind) + 1; WorstOverCm = Math.Max(WorstOverCm, d - pace); if (First.Count < keep) First.Add((u.Id, u.Kind, d, pace, g.Tick, p, u.At)); }
                    }
                    now[u.Id] = u.At;
                }
                (last, now) = (now, last);
            }
        }

        /// <summary>Whether a pair inside two radii is a squeeze the law counts (engine 0.47.0): each squeezes past the other, and they stand at least
        /// two squeeze widths apart. Any other pair inside two radii is the law's failure.</summary>
        public static bool Squeezing(Game g, in Upright a, in Upright b, int cm) => cm >= 2 * g.R.BodySqueezeCm && g.BodySqueeze(a.Id) is var sa && sa.HasValue && sa.Value.with == b.Id && g.BodySqueeze(b.Id) is var sb && sb.HasValue && sb.Value.with == a.Id;

        /// <summary>Every pair of upright bodies closer than `minCm` centre to centre, nearest first (a 1 m bucket grid: cheap at hundreds).</summary>
        public static List<(Upright a, Upright b, int cm)> Closer(List<Upright> l, int minCm)
        {
            var cells = new Dictionary<long, List<int>>(); var r = new List<(Upright, Upright, int)>(); int cell = Math.Max(100, minCm);
            long Key(int cx, int cy) => unchecked(((long)cx << 32) ^ (uint)cy); // a negative y is its bits (unchecked, as in Release)
            int Fd(int a) => a >= 0 ? a / cell : -((-a + cell - 1) / cell);
            for (int i = 0; i < l.Count; i++) { long k = Key(Fd(l[i].At.X), Fd(l[i].At.Y)); if (!cells.TryGetValue(k, out var c)) cells[k] = c = new List<int>(); c.Add(i); }
            long m2 = (long)minCm * minCm;
            for (int i = 0; i < l.Count; i++)
            {
                int cx = Fd(l[i].At.X), cy = Fd(l[i].At.Y);
                for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                    if (cells.TryGetValue(Key(cx + dx, cy + dy), out var c))
                        foreach (var j in c) if (j > i && G.D2(l[i].At, l[j].At) < m2) r.Add((l[i], l[j], G.Dist(l[i].At, l[j].At)));
            }
            r.Sort((x, y) => x.Item3.CompareTo(y.Item3));
            return r;
        }
    }
}
