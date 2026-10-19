// Pulse 023 scratch probe: the car's service-lane route and the crowd at a mission second. Reads the engine, changes nothing.
using System;
using System.IO;
using System.Linq;
using Suborned.Sim;
static class P
{
    static int Main(string[] a)
    {
        var root = Engine.FindRoot(Directory.GetCurrentDirectory());
        var inp = Engine.Load(root);
        var g = Engine.New(inp, new Start());
        var s = g.S;
        var from = s.Stand(s.CarPickup); var to = s.Stand(s.CarService);
        Console.WriteLine($"pickup stand {from.X},{from.Y}  service stand {to.X},{to.Y}  driver {s.Stand(s.CarDriver).X},{s.Stand(s.CarDriver).Y}");
        var r = s.Grid.Route(s.Grid.NearestFree(from) ?? from, to);
        Console.WriteLine(r == null ? "no route" : "route: " + string.Join(" ", r.Select(p => $"({p.X},{p.Y})")));
        return 0;
    }
}
