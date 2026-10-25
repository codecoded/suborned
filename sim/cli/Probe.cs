// Measurements on the real block, for writing and re-siting scenarios: walk times, the sweep's blind windows, the guard's walk.
using System;
using System.Collections.Generic;
using Suborned.Sim;

static class Probe
{
    public static void Run(Engine.Inputs i)
    {
        var g = Engine.New(i); var s = i.SiteData;
        string[] from = { "crew:scout", "crew:fixer", "crew:tech", "crew:enforcer" };
        string[] to = { "d:Courtyard", "beside-inez", "cabinet", "d:Office approach", "d:Office entrance", "d:Meet driver", "d:Service lane", "rival-park", "d:Getaway bay", "d:Café door" };
        int speed = i.Rules.CellSpeed;
        foreach (var f in from) foreach (var t in to)
        {
            var r = s.Grid.Route(s.Stand(f), s.Stand(t));
            if (r == null) { Console.WriteLine($"{f} → {t}: no walk"); continue; }
            var p = new Path(r); Console.WriteLine($"{f,-14} → {t,-18} {p.Length / 1000.0,6:0.0} m  {(p.Length + speed - 1) / speed / 10.0,5:0.0} s");
        }
        foreach (var (a, b) in new[] { ("d:Service lane", "d:Getaway bay"), ("d:Office entrance", "d:Café door"), ("d:Café door", "d:Office entrance"), ("d:Official entrance", "d:Service lane"), ("d:Meet driver", "rival-park"), ("d:Courtyard", "d:Office approach") })
        {
            var p = new Path(s.Grid.Route(s.Stand(a), s.Stand(b))!); Console.WriteLine($"{a} → {b}: {p.Length / 1000.0:0.0} m {(p.Length + speed - 1) / speed / 10.0:0.0} s");
        }
        Console.Write("sweep sees the public door at t mod 100 (s): ");
        for (int t = 0; t < 100; t++) Console.Write(g.SweepSeesEntrance(t * 10) ? "#" : ".");
        Console.WriteLine();
        foreach (var c in s.Cameras)
        {
            Console.Write($"camera {c.Id} sees:");
            foreach (var id in s.PlaceIds) if (G.InCone(c.At, c.Facing, c.Reach, c.Half, 0, s.Stand(id)) && s.Sight(c.At, s.Stand(id), 160)) Console.Write($" {id}");
            Console.WriteLine();
        }
        foreach (var (a, b) in new[] { ("security-entry", "d:Office approach"), ("d:Office approach", "d:Service lane"), ("d:Office entrance", "d:Café door"), ("rival-front-stop", "d:Office approach"), ("rival-lookout", "d:Meet driver") }) // brief 059: the drawn crew's walk from its car, and the lookout's to the driver
            Console.WriteLine($"path {a} → {b}: {string.Join(" ", s.Grid.Route(s.Stand(a), s.Stand(b))!)}");
        // brief 059 (engine 0.32.0): the rival car's timetable and legs on the drive grid (rulebook rivals; car.pace), and the getaway car's Drive
        Console.WriteLine($"rival car: enters {Report.Clock(g.CrewEntry)}, stops at the drop {Report.Clock(g.CrewDropArrive)}, leaves it {Report.Clock(g.CrewDropLeave)}, parks {Report.Clock(g.RivalAt)} at rival-park {s.At(s.RivalsPark)}");
        foreach (var st in s.Stands) Console.WriteLine($"stand {st.Id}: car {st.CarAt} (drive cell {st.CarCell}), facing {st.Facing}, rival stop {st.RivalStop}, Drive from the square {g.DriveTicks(st.Id) / 10.0:0.0} s");
        Console.Write("guard at door sees:"); foreach (var id in s.PlaceIds) if (g.GuardWatching(s.Stand(id))) Console.Write($" {id}"); Console.WriteLine();
    }

    /// <summary>Engine 0.33.1 (rulebook car.footprint): the cars standing at the mission's start (the getaway car at its first stand, the
    /// official car at the pickup), every site and people place on their ground, the routine legs that cross it, and the response unit's walk.</summary>
    public static void Cars(Engine.Inputs i)
    {
        var g = Engine.New(i); var s = i.SiteData; var w = g.Walk;
        foreach (var c in g.StandingCars()) Console.WriteLine($"car {c.Id} at {c.At} heading {c.Heading}");
        int cells = 0; for (int a = 0; a < s.Grid.Nx; a++) for (int b = 0; b < s.Grid.Ny; b++) if (s.Grid.FreeCell(a, b) && !w.FreeCell(a, b)) cells++;
        Console.WriteLine($"cells the standing cars cover: {cells}");
        foreach (var id in s.PlaceIds) { var p = s.At(id); var st = s.Stand(id); if (w.CarCovers(p) || w.CarCovers(st)) Console.WriteLine($"site place {id} {p} (stand {st}) lies on a car's ground"); }
        foreach (var cp in s.Cover) if (w.CarCovers(cp.At)) Console.WriteLine($"cover point {cp.Id} ({cp.Of}) {cp.At} lies on a car's ground");
        var crowd = g.People2;
        if (crowd != null)
        {
            foreach (var kv in crowd.Places) if (w.CarCovers(kv.Value)) Console.WriteLine($"people place {kv.Key} {kv.Value} lies on a car's ground");
            int legs = 0, crossing = 0;
            foreach (var kv in crowd.Legs)
            {
                legs++; var p = kv.Value; long on = 0;
                for (long mm = 0; mm <= p.Length; mm += 100) if (w.CarCovers(p.At(mm))) on += 100;
                if (on > 0) { crossing++; Console.WriteLine($"leg {kv.Key} walks {on / 1000.0:0.0} m on a car's ground"); }
            }
            Console.WriteLine($"routine legs: {legs}, crossing a standing car: {crossing}");
            // brief 041 ("x2" over Vale's car): security people standing on one spot, a no-orders run read once a second
            var hg = Engine.New(i); var spans = new SortedDictionary<string, (int from, int to, int most, string who)>(StringComparer.Ordinal);
            while (!hg.Over && hg.Tick < Game.T(s.Over))
            {
                hg.Advance(Game.Hz); var at = new Dictionary<Pt, List<string>>();
                void Put(string who, Pt? p) { if (!p.HasValue) return; if (!at.TryGetValue(p.Value, out var l)) at[p.Value] = l = new List<string>(); l.Add(who); }
                Put("guard (cast)", hg.GuardPose()?.pos);
                foreach (var wk in hg.Walkers) if (wk.P.Job == "guard" || wk.P.Job == "patrol-officer" || wk.P.Job == "watcher") Put($"{wk.P.Id} ({wk.P.Job})", hg.CrowdPos(wk));
                foreach (var kv in at) if (kv.Value.Count > 1) { var k = $"{kv.Key}"; if (!spans.TryGetValue(k, out var sp)) sp = (hg.Tick, hg.Tick, 0, ""); spans[k] = (sp.from, hg.Tick, Math.Max(sp.most, kv.Value.Count), kv.Value.Count > sp.most ? string.Join(" + ", kv.Value) : sp.who); }
            }
            foreach (var kv in spans) Console.WriteLine($"security people on one spot {kv.Key}: from {Report.Clock(kv.Value.from)} to {Report.Clock(kv.Value.to)} (read each second), at most {kv.Value.most}: {kv.Value.who}");
            // the cars' other places (not in the crowd's legs: they are made once, round the start's cars)
            var other = new List<(string, Pt, Pt)> { ("rival car at the drop", s.CarStand(s.RivalCarDrop), s.RivalFacingDrop), ("rival car at the park", s.CarStand(s.RivalsPark), s.RivalFacingPark),
                ("rival car at the tail", s.CarStand(s.RivalCarTail), s.RivalFacingTail), ("rival car at the front stop", s.CarStand(s.RivalCarFront), s.RivalFacingFront),
                ("official car in the service bay", s.CarStand(s.CarService), s.CarServiceFacing) };
            foreach (var st in s.Stands) if (st != s.Stands[0]) other.Add(($"getaway car at {st.Id}", st.CarAt, st.Facing));
            foreach (var (what, at, h) in other)
            {
                var m = Game.MaskOf(s.Grid, i.Rules, new[] { (at, h) }); int n = 0; double metres = 0;
                foreach (var kv in crowd.Legs) { long on = 0; var p = kv.Value; for (long mm = 0; mm <= p.Length; mm += 100) { var q = p.At(mm); if (m != null && s.Grid.Free(q) && !s.Grid.Free(q, m)) on += 100; } if (on > 0) { n++; metres += on / 1000.0; } }
                Console.WriteLine($"{what} {at} heading {h}: {n} legs cross it ({metres:0.0} m in all)");
                foreach (var id in s.PlaceIds) { var p = s.Stand(id); if (m != null && s.Grid.Free(p) && !s.Grid.Free(p, m)) Console.WriteLine($"    site place {id} {p} lies on its ground"); }
                foreach (var cp in s.Cover) if (m != null && s.Grid.Free(cp.At) && !s.Grid.Free(cp.At, m)) Console.WriteLine($"    cover point {cp.Id} ({cp.Of}) {cp.At} lies on its ground");
            }
        }
    }
}
