// The host's Save() cost over a recorded session: Save runs on every order and every 5 s of clock; while a public fight is open it calls
// ClosingHash = Engine.Play from the start to now (a full replay). This replays the session headless and times exactly that call where the host would make it.
using System; using System.Collections.Generic; using System.Diagnostics; using System.IO; using System.Linq; using Suborned.Sim;
var root = args[0]; var script = Script.Parse(File.ReadAllText(args[1]));
var inputs = Engine.Load(root);
var orders = script.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
var g = Engine.New(inputs, script.Start); int oi = 0; int until = script.Until > 0 ? script.Until : 7000; int savedTick = 0;
var sw = new Stopwatch(); int fightFrom = -1; var rows = new List<(int tick, string why, double ms)>();
double Closing() { var os = new List<Order>(); foreach (var (o, _, _, _) in g.Orders) os.Add(o); sw.Restart(); var r = Engine.Play(inputs, g.Start, os, g.Tick); sw.Stop(); _ = r.Checkpoints[^1].hash; return sw.Elapsed.TotalMilliseconds; }
Closing(); // warm the JIT
while (!g.Over && g.Tick < until)
{
    while (oi < orders.Count && orders[oi].Tick <= g.Tick) { var o = orders[oi++]; if (o.Tick != g.Tick) continue; g.Issue(o); if (g.OpenFight != null && !g.Over) rows.Add((g.Tick, "order", Closing())); savedTick = g.Tick; }
    g.Step();
    if (g.OpenFight != null && fightFrom < 0) fightFrom = g.Tick; if (g.OpenFight == null) fightFrom = -1;
    if (g.Tick - savedTick >= 50 && !g.Over) { savedTick = g.Tick; if (g.OpenFight != null) rows.Add((g.Tick, "clock", Closing())); }
}
Console.WriteLine($"{g.Result} at tick {g.Tick}; Save calls that replay the whole run (public fight open): {rows.Count}");
foreach (var grp in rows.GroupBy(r => r.tick / 300)) Console.WriteLine($"  ticks {grp.Key * 300,5}-{grp.Key * 300 + 299,5}: {grp.Count(),3} replays ({grp.Count(r => r.why == "order")} on orders), each {grp.Average(r => r.ms),6:0} ms mean, {grp.Max(r => r.ms),6:0} ms max; {grp.Sum(r => r.ms) / 1000,5:0.0} s stalled");
var last60 = rows.Where(r => r.tick >= g.Tick - 600).ToList();
Console.WriteLine($"last 60 s of the run: {last60.Count} replays, {last60.Sum(r => r.ms) / 1000:0.0} s of main-thread stall in 60 s");
