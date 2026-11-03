// MOTIVE PROBE (brief 085), scratch only: a discreet Listen. ML_ON=1, ML_MIN cm, ML_SEATS=0 for no chairs, ML_NOTE=1 logs the spot.
using System;
using System.Collections.Generic;
using System.IO;
namespace Suborned.Sim
{
    public static class MotiveListen
    {
        static string E(string k, string d) => Environment.GetEnvironmentVariable(k) ?? d;
        public static readonly bool On = E("ML_ON", "0") == "1", Note = E("ML_NOTE", "0") == "1", SeatsOn = E("ML_SEATS", "1") == "1";
        public static readonly bool Path = E("ML_MODE", "") == "path" || E("ML_MODE", "") == "early", Early = E("ML_MODE", "") == "early";
        public static readonly int SeatGraceS = int.Parse(E("ML_GRACE", "10"));
        public static readonly int PathMinCm = int.Parse(E("ML_PMIN", "150")), SeatLookS = int.Parse(E("ML_LOOK", "60"));
        public static readonly bool Post = E("ML_POST", "0") == "1", NoCone = E("ML_NOCONE", "1") == "1";
        public static readonly int MinCm = int.Parse(E("ML_MIN", "300"));
        static List<(string name, Pt at)>? chairs;
        public static List<(string name, Pt at)> Chairs()
        {
            if (chairs != null) return chairs;
            var l = new List<(string, Pt)>();
            var path = E("ML_SEATFILE", "contracts/people/handover.seats.json");
            var j = JNode.Parse(File.ReadAllText(path), "seats");
            foreach (var t in j["tables"].Entries) foreach (var c in t.Value["chairs"].Items) { var a = c["at"].Ints(); l.Add(($"{t.Key}-seat-{c["chair"].Int()}", new Pt(a[0], a[1]))); }
            return chairs = l;
        }
    }
    public sealed partial class Game
    {
        readonly Dictionary<OpId, (Pt at, string kind, int chose)> mlSpot = new Dictionary<OpId, (Pt, string, int)>();
        bool mlArriving, mlDiag;
        bool MlBesideSolid(Pt q) { int c = S.Grid.Cell; return !S.Grid.Free(q + new Pt(c, 0)) || !S.Grid.Free(q - new Pt(c, 0)) || !S.Grid.Free(q + new Pt(0, c)) || !S.Grid.Free(q - new Pt(0, c)); }
        bool MlWatched(Pt q)
        {
            if (!MotiveListen.NoCone) return false;
            if (GuardWatching(q, false) || PatrolWatching(q, false)) return true;
            foreach (var c in S.Cameras) if (CameraWatching(c.Id, q, false)) return true;
            return false;
        }
        bool MlSeatFree(string name, int from, int to)
        {
            foreach (var w in Walkers) for (int i = 0; i < w.Place.Length; i++) if (w.Place[i] == name && w.From[i] < to && w.To[i] > from) return false;
            return true;
        }
        /// <summary>The spot for her pose: nearest to the Scout of free chairs in the ring, else ground beside something solid, on a clear line.</summary>
        (Pt at, string kind)? MlChoose(Op op, Pt her, int reach, int until)
        {
            (Pt, string)? best = null; long bd = long.MaxValue;
            if (Environment.GetEnvironmentVariable("ML_DIAG") == "1" && !mlDiag) { mlDiag = true; Console.Error.WriteLine($"tick {Tick}"); foreach (var (nm, aa) in MotiveListen.Chairs()) { var ss = new List<string>(); foreach (var w in Walkers) for (int i = 0; i < w.Place.Length; i++) if (w.Place[i] == nm && w.To[i] > 0 && w.From[i] < 5000) ss.Add($"{w.From[i]}-{w.To[i]}"); var nf = Walk.NearestFree(aa); Console.Error.WriteLine($"OCC {nm} {string.Join(" ", ss)} watched {(nf.HasValue && MlWatched(nf.Value))} guard {(nf.HasValue && GuardWatching(nf.Value, false))}"); } foreach (var (name, at) in MotiveListen.Chairs()) { var st = Walk.NearestFree(at); Console.Error.WriteLine($"{name} at {at.X},{at.Y} nf {st?.X},{st?.Y} dist {(st.HasValue ? G.Dist(st.Value, at) : -1)} her {G.Dist(at, her)} free {MlSeatFree(name, Tick, until)} route {(st.HasValue && Walk.Route(op.Pos, st.Value) != null)} sees {Sees(at, her)}"); } }
            if (MotiveListen.SeatsOn)
                foreach (var (name, at) in MotiveListen.Chairs())
                {
                    if (!G.Within(at, her, reach) || G.Within(at, her, MotiveListen.MinCm) || !MlSeatFree(name, Tick, until)) continue;
                    var st = Walk.NearestFree(at); if (!st.HasValue || G.Dist(st.Value, at) > 130 || !G.Within(st.Value, her, reach) || G.Within(st.Value, her, MotiveListen.MinCm) || MlWatched(st.Value)) continue;
                    long d = G.D2(op.Pos, st.Value); if (d < bd) { bd = d; best = (st.Value, "seat"); }
                }
            if (best.HasValue) return best;
            int cell = S.Grid.Cell;
            for (int dx = -reach; dx <= reach; dx += cell) for (int dy = -reach; dy <= reach; dy += cell)
                {
                    var q = new Pt(her.X + dx, her.Y + dy);
                    if (!S.Grid.Free(q) || !G.Within(q, her, reach) || G.Within(q, her, MotiveListen.MinCm)) continue;
                    bool solid = !S.Grid.Free(q + new Pt(cell, 0)) || !S.Grid.Free(q - new Pt(cell, 0)) || !S.Grid.Free(q + new Pt(0, cell)) || !S.Grid.Free(q - new Pt(0, cell));
                    if (!solid || !Sees(q, her) || MlWatched(q)) continue;
                    long d = G.D2(op.Pos, q); if (d < bd) { bd = d; best = (q, "lean"); }
                }
            return best;
        }
        (Pt at, string kind, Path path)? MlEarly(Op op, Person p, int reach, int listen, int speed)
        {
            (Pt at, string kind, Path path)? lean = null; int leanT = -1;
            for (int t = 0; t <= 120; t += 2)
            {
                if (lean.HasValue && t > leanT + MotiveListen.SeatGraceS) break;
                var her = t == 0 ? PersonPos(p) : PoseAhead(p, T(t))?.pos; if (!her.HasValue) continue;
                (Pt, long)? best = null;
                foreach (var (name, at) in MotiveListen.Chairs())
                {
                    var st = Walk.NearestFree(at); if (!st.HasValue || G.Dist(st.Value, at) > 130 || !G.Within(st.Value, her.Value, reach) || G.Within(st.Value, her.Value, MotiveListen.PathMinCm)) continue;
                    if (!MlSeatFree(name, Tick, Tick + T(t) + listen + T(5)) || MlWatched(st.Value) || G.Dist(op.Pos, st.Value) * 10L > speed * (long)T(t)) continue;
                    long d = G.D2(op.Pos, st.Value); if (best == null || d < best.Value.Item2) best = (st.Value, d);
                }
                if (best != null && Walk.Route(op.Pos, best.Value.Item1) is Pt[] rs) { var ps = new Path(rs); if ((ps.Length + speed - 1) / speed <= T(t)) return (best.Value.Item1, "seat", ps); }
                if (lean.HasValue) continue;
                best = null; int cell = S.Grid.Cell;
                for (int dx = -reach; dx <= reach; dx += cell) for (int dy = -reach; dy <= reach; dy += cell)
                    {
                        var q = new Pt(her.Value.X + dx, her.Value.Y + dy);
                        if (!S.Grid.Free(q) || !G.Within(q, her.Value, reach) || G.Within(q, her.Value, MotiveListen.PathMinCm) || !MlBesideSolid(q) || G.Dist(op.Pos, q) * 10L > speed * (long)T(t) || !Sees(q, her.Value) || MlWatched(q)) continue;
                        long d = G.D2(op.Pos, q); if (best == null || d < best.Value.Item2) best = (q, d);
                    }
                if (best != null && Walk.Route(op.Pos, best.Value.Item1) is Pt[] rl) { var pl = new Path(rl); if ((pl.Length + speed - 1) / speed <= T(t)) { lean = (best.Value.Item1, "lean", pl); leanT = t; } }
            }
            return lean;
        }
        /// <summary>The spot her walk will bring her near: earliest second she comes within reach (never nearer than PathMin) of a free chair the Scout can reach first, up to SeatLook s ahead; else ground beside something solid.</summary>
        (Pt at, string kind, Path path)? MlOnPath(Op op, Person p, int reach, int listen, int speed)
        {
            if (MotiveListen.Early) return MlEarly(op, p, reach, listen, speed);
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 0 && !MotiveListen.SeatsOn) continue;
                for (int t = 0; t <= (pass == 0 ? MotiveListen.SeatLookS : 120); t += 2)
                {
                    var her = t == 0 ? PersonPos(p) : PoseAhead(p, T(t))?.pos; if (!her.HasValue) continue;
                    (Pt, long)? best = null;
                    if (pass == 0)
                        foreach (var (name, at) in MotiveListen.Chairs())
                        {
                            var st = Walk.NearestFree(at); if (!st.HasValue || G.Dist(st.Value, at) > 130 || !G.Within(st.Value, her.Value, reach) || G.Within(st.Value, her.Value, MotiveListen.PathMinCm)) continue;
                            if (!MlSeatFree(name, Tick, Tick + T(t) + listen + T(5)) || MlWatched(st.Value) || G.Dist(op.Pos, st.Value) * 10L > speed * (long)T(t)) continue;
                            long d = G.D2(op.Pos, st.Value); if (best == null || d < best.Value.Item2) best = (st.Value, d);
                        }
                    else
                    {
                        int cell = S.Grid.Cell;
                        for (int dx = -reach; dx <= reach; dx += cell) for (int dy = -reach; dy <= reach; dy += cell)
                            {
                                var q = new Pt(her.Value.X + dx, her.Value.Y + dy);
                                if (!S.Grid.Free(q) || !G.Within(q, her.Value, reach) || G.Within(q, her.Value, MotiveListen.PathMinCm) || !MlBesideSolid(q) || G.Dist(op.Pos, q) * 10L > speed * (long)T(t) || !Sees(q, her.Value) || MlWatched(q)) continue;
                                long d = G.D2(op.Pos, q); if (best == null || d < best.Value.Item2) best = (q, d);
                            }
                    }
                    if (best == null) continue;
                    var rt = Walk.Route(op.Pos, best.Value.Item1); if (rt == null) continue;
                    var pa = new Path(rt); if ((pa.Length + speed - 1) / speed > T(t)) continue;
                    return (best.Value.Item1, pass == 0 ? "seat" : "lean", pa);
                }
            }
            return null;
        }
        Path? ListenRoute(Op op, Person p)
        {
            int reach = ActReachOf(op, "listen"), listen = T(R.Ev("scout.listen").Price[0].SV is var sv ? (int)sv : 30), speed = Math.Max(1, PaceOf(op, false));
            if (mlSpot.TryGetValue(op.Id, out var have))
            {
                bool there = G.Within(op.Pos, have.at, 60);
                if (!there) { var r0 = Walk.Route(op.Pos, have.at); if (r0 != null) return new Path(r0); }
                else if (Tick - have.chose < T(30)) return new Path(new[] { op.Pos, op.Pos });
                mlSpot.Remove(op.Id);
            }
            var at = PersonPos(p); if (!at.HasValue) return Intercept(op, p, reach)?.path;
            (Pt at, string kind)? pick = null; Path? path = null;
            if (MotiveListen.Path && ServerOf(p) != null)
            {
                var ch = MlOnPath(op, p, reach, listen, speed); if (ch.HasValue) { mlSpot[op.Id] = (ch.Value.at, ch.Value.kind, Tick); return ch.Value.path; }
            }
            if (MotiveListen.Post && ServerOf(p) is RoundsData.Server sv0)
            {
                var c0 = MlChoose(op, sv0.At, reach, Tick + T(30) + listen + T(5));
                if (c0.HasValue && Walk.Route(op.Pos, c0.Value.at) is Pt[] r1) { mlSpot[op.Id] = (c0.Value.at, c0.Value.kind, Tick); return new Path(r1); }
            }
            int tMax = ServerOf(p) == null ? 0 : 120;
            for (int t = 0; t <= tMax; t += 2)
            {
                var her = t == 0 ? at : PoseAhead(p, T(t))?.pos; if (!her.HasValue) continue;
                var c = MlChoose(op, her.Value, reach, Tick + T(t) + listen + T(5)); if (!c.HasValue) continue;
                var rt = Walk.Route(op.Pos, c.Value.at); if (rt == null) continue;
                var pa = new Path(rt); int w = (int)((pa.Length + speed - 1) / speed);
                pick = c; path = pa;
                if (w <= T(t)) break;
            }
            if (pick == null) { if (MotiveListen.Note) Note("mp.none", "0"); return Intercept(op, p, reach)?.path; }
            mlSpot[op.Id] = (pick.Value.at, pick.Value.kind, Tick);
            return path;
        }
    }
}
