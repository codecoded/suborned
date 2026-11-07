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
