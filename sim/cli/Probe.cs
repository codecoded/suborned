// Measurements on the real block, for writing and re-siting scenarios: walk times, the sweep's blind windows, the guard's walk.
using System;
using Suborned.Sim;

static class Probe
{
    public static void Run(Engine.Inputs i)
    {
        var g = Engine.New(i); var s = i.SiteData;
        string[] from = { "crew:scout", "crew:fixer", "crew:tech", "crew:enforcer" };
        string[] to = { "d:Courtyard", "beside-inez", "cabinet", "d:Office approach", "d:Office entrance", "d:Meet driver", "d:Service lane", "rival-park", "d:Getaway bay", "d:Café door" };
        int speed = i.Rules.RefI("cell", "speed");
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
        foreach (var (a, b) in new[] { ("security-entry", "d:Office approach"), ("d:Office approach", "d:Service lane"), ("d:Office entrance", "d:Café door"), ("rival-entry", "d:Office approach"), ("rival-park", "d:Office approach") })
            Console.WriteLine($"path {a} → {b}: {string.Join(" ", s.Grid.Route(s.Stand(a), s.Stand(b))!)}");
        Console.Write("guard at door sees:"); foreach (var id in s.PlaceIds) if (g.GuardWatching(s.Stand(id))) Console.Write($" {id}"); Console.WriteLine();
    }
}
