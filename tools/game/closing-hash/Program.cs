// closing-hash <repo root> <script.json>...: one JSON line per script (the contract in tools/game/closing-hash.test.ts).
// It plays the script as the host does (an order is Issued on its tick; a save on every order and every 5 s of clock), and at each save while a public fight is open
// takes Game.ClosingHash() and the replay's closing checkpoint (Engine.Play from the start with the orders so far, to now: what the host's Save did before engine 0.25.0).
using System; using System.Collections.Generic; using System.Diagnostics; using System.Globalization; using System.IO; using System.Linq; using System.Text; using Suborned.Sim;

var root = args[0];
var inputs = Engine.Load(root);
foreach (var path in args.Skip(1))
{
    var script = Script.Parse(File.ReadAllText(path));
    var orders = script.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
    var g = Engine.New(inputs, script.Start); int oi = 0, until = script.Until > 0 ? script.Until : 7000, savedTick = 0, lastFight = -1;
    var rows = new List<(int tick, string why, ulong cheap, ulong replay)>();
    void Save(string why)
    {
        savedTick = g.Tick;
        if (g.OpenFight == null || g.Over) return;
        lastFight = g.Tick;
        var os = new List<Order>(); foreach (var (o, _, _, _) in g.Orders) os.Add(o);
        ulong replay = Engine.Play(inputs, g.Start, os, g.Tick).Checkpoints[^1].hash;
        ulong cheap = g.ClosingHash();
        rows.Add((g.Tick, why, cheap, replay));
    }
    while (!g.Over && g.Tick < until)
    {
        while (oi < orders.Count && orders[oi].Tick <= g.Tick) { var o = orders[oi++]; if (o.Tick != g.Tick) continue; g.Issue(o); Save("order"); }
        g.Step();
        if (g.Tick - savedTick >= 50 && !g.Over) Save("clock");
    }
    // the cost at the last tick a fight was open: replay the run to there (no replay of this in the number), warm one call, best of 20
    double best = double.MaxValue;
    if (lastFight >= 0)
    {
        var h = Engine.New(inputs, script.Start); int hi = 0;
        while (!h.Over && h.Tick < lastFight) { while (hi < orders.Count && orders[hi].Tick <= h.Tick) { var o = orders[hi++]; if (o.Tick == h.Tick) h.Issue(o); } h.Step(); }
        while (hi < orders.Count && orders[hi].Tick <= h.Tick) { var o = orders[hi++]; if (o.Tick == h.Tick) h.Issue(o); }
        if (h.OpenFight != null) { h.ClosingHash(); var sw = new Stopwatch(); for (int k = 0; k < 20; k++) { sw.Restart(); h.ClosingHash(); sw.Stop(); best = Math.Min(best, sw.Elapsed.TotalMilliseconds); } }
    }
    var j = new StringBuilder();
    string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    j.Append("{\"script\":\"").Append(Esc(path)).Append("\",\"result\":\"").Append(g.Result).Append("\",\"tick\":").Append(g.Tick).Append(",\"rows\":[");
    j.Append(string.Join(",", rows.Select(r => $"{{\"tick\":{r.tick},\"why\":\"{r.why}\",\"cheap\":\"{r.cheap:x16}\",\"replay\":\"{r.replay:x16}\"}}")));
    j.Append("],\"lastFightTick\":").Append(lastFight).Append(",\"cheapMs\":").Append(best == double.MaxValue ? "-1" : best.ToString("0.0000", CultureInfo.InvariantCulture)).Append('}');
    Console.WriteLine(j);
}
