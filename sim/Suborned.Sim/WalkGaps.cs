// The zone-wide gap check, the engine's half (brief 098, Sky 2026-11-08: "don't be one of those game engines where you can't get past even though
// visibly it's clear you have a lot of space either side. We can do better"; engine 0.49.0, Keel). sim/data/handover.gaps.json (tools/sim/gaps.ts,
// written with the site) lists every gap between two of the walk's solids that a body, its margin and the raster's cell fit through, found on the
// solids' own geometry. Each must be crossed both ways by the engine's own walk: Route from `from` to `to` and back, passing between p and q (the
// route crosses the segment between the two solids' nearest points), no more than DetourCm longer than the straight line. Read by
// `suborned-sim gaps` and sim/Suborned.Sim.Tests (Gaps.cs); never by a game.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public static class WalkGaps
    {
        public const string File = "sim/data/handover.gaps.json";
        /// <summary>A crossing is no detour within this of the straight line (cm).</summary>
        public const int DetourCm = 100;

        public sealed class Gap { public string A = "", B = ""; public int Cm; public Pt P, Q, M, From, To; }
        public sealed class Result { public Gap G = null!; public bool There, Back; public string Why = ""; public int LenThere, LenBack; }

        public static List<Gap> Read(string json, Grid grid)
        {
            var r = JNode.Parse(json, "gaps");
            r.Only("schema", "rule", "clear", "fits", "max", "gaps", "skipped");
            if (r["schema"].Str() != "suborned.sim.gaps/1") throw new FormatException("gaps: not suborned.sim.gaps/1");
            if (grid.HasFine && r["clear"].Int() != grid.Body) throw new FormatException($"gaps: found at a {r["clear"].Int()} cm clearance, the site's walk is {grid.Body} cm (rebuild with bun run sim:site)");
            var o = new List<Gap>();
            foreach (var x in r["gaps"].Items)
            {
                x.Only("a", "b", "gap", "p", "q", "m", "from", "to");
                o.Add(new Gap { A = x["a"].Str(), B = x["b"].Str(), Cm = x["gap"].Int(), P = Pt.Of(x["p"]), Q = Pt.Of(x["q"]), M = Pt.Of(x["m"]), From = Pt.Of(x["from"]), To = Pt.Of(x["to"]) });
            }
            return o;
        }

        public static List<Result> Check(Grid grid, List<Gap> gaps)
        {
            var o = new List<Result>();
            foreach (var g in gaps)
            {
                var res = new Result { G = g };
                string? one(Pt a, Pt b, out int len)
                {
                    len = 0; var r = grid.Route(a, b);
                    if (r == null) return grid.Free(a) && grid.Free(b) ? "no route" : $"{(grid.Free(a) ? "" : "its start ")}{(grid.Free(b) ? "" : "its end ")}off free ground";
                    for (int i = 1; i < r.Length; i++) len += G.Dist(r[i - 1], r[i]);
                    if (!Crosses(r, g.P, g.Q)) return $"goes round ({len} cm)";
                    if (len > G.Dist(a, b) + DetourCm) return $"a detour ({len} cm, straight {G.Dist(a, b)})";
                    return null;
                }
                var w1 = one(g.From, g.To, out res.LenThere); var w2 = one(g.To, g.From, out res.LenBack);
                res.There = w1 == null; res.Back = w2 == null; res.Why = w1 ?? w2 ?? "";
                o.Add(res);
            }
            return o;
        }

        /// <summary>Does the polyline cross (or touch) the segment p–q? Integer orientation tests.</summary>
        static bool Crosses(Pt[] r, Pt p, Pt q)
        {
            for (int i = 1; i < r.Length; i++) if (SegHit(r[i - 1], r[i], p, q)) return true;
            return false;
        }
        static int Orient(Pt a, Pt b, Pt c) { long v = (long)(b.X - a.X) * (c.Y - a.Y) - (long)(b.Y - a.Y) * (c.X - a.X); return v > 0 ? 1 : v < 0 ? -1 : 0; }
        static bool OnSeg(Pt a, Pt b, Pt c) => Math.Min(a.X, b.X) <= c.X && c.X <= Math.Max(a.X, b.X) && Math.Min(a.Y, b.Y) <= c.Y && c.Y <= Math.Max(a.Y, b.Y);
        static bool SegHit(Pt a, Pt b, Pt c, Pt d)
        {
            int o1 = Orient(a, b, c), o2 = Orient(a, b, d), o3 = Orient(c, d, a), o4 = Orient(c, d, b);
            if (o1 != o2 && o3 != o4) return true;
            return (o1 == 0 && OnSeg(a, b, c)) || (o2 == 0 && OnSeg(a, b, d)) || (o3 == 0 && OnSeg(c, d, a)) || (o4 == 0 && OnSeg(c, d, b));
        }
    }
}
