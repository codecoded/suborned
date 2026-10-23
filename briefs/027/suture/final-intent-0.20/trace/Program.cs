// Suture 0.20 tracer: plays a script on the working tree and prints every body's position (m) each `step` ticks in [from, to].
using System; using System.Collections.Generic; using System.IO; using System.Linq; using Suborned.Sim;
var root = Engine.FindRoot(Directory.GetCurrentDirectory());
var inputs = Engine.Load(root);
var sc = Script.Parse(File.ReadAllText(args[0]));
if (args[1] == "cone") { int at = int.Parse(args[2]); var g0 = Engine.New(inputs, sc.Start); var os = sc.Orders.OrderBy(o => o.Tick).ToList(); int j = 0; for (int t = 0; t <= at; t++) { if (g0.Tick < t) g0.RunTo(t); while (j < os.Count && os[j].Tick == t) g0.Issue(os[j++]); } var rp = g0.PosOf("rivals"); Console.WriteLine($"rivals at {rp}"); for (int y = -3600; y <= -1600; y += 100) { var row = new System.Text.StringBuilder($"{y,6} "); for (int x = 4200; x <= 6400; x += 100) row.Append(g0.RivalsStreetWatching(new Pt(x, y)) ? "#" : "."); Console.WriteLine(row); } return; }
int from = int.Parse(args[1]), to = int.Parse(args[2]), step = args.Length > 3 ? int.Parse(args[3]) : 5;
var g = Engine.New(inputs, sc.Start);
var orders = sc.Orders.OrderBy(o => o.Tick).ToList(); int k = 0;
string[] ids = { "scout", "fixer", "tech", "enforcer", "rivals", "security-1", "security-2", "officer-1", "officer-2", "guard", "patrol" };
string m(Pt p) => $"({p.X / 100.0:0.0},{p.Y / 100.0:0.0})";
for (int t = 0; t <= Math.Min(to, sc.Until); t++)
{
    if (g.Tick < t) g.RunTo(t);
    while (k < orders.Count && orders[k].Tick == t) g.Issue(orders[k++]);
    if (g.Over) break;
    if (t >= from && (t - from) % step == 0)
    {
        var parts = new List<string>();
        foreach (var id in ids) { var p = g.PosOf(id); if (p.HasValue) parts.Add($"{id} {m(p.Value)}"); }
        foreach (var h in g.Hostiles) if (!ids.Contains(h.Id)) { var p = g.PosOf(h.Id); if (p.HasValue) parts.Add($"{h.Id} {m(p.Value)}{(h.Gun.Down ? " DOWN" : "")}"); }
        Console.WriteLine($"{Report.Clock(t)} " + string.Join(" | ", parts) + $" || faces[{string.Join(",", g.KnownFaces)}] rivalsSaw[{string.Join(",", g.RivalsSawAct)}]");
    }
}
