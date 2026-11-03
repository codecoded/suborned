// Motive brief 067 square probe (scratch only, never the engine): `trace <script.json> [every]` plays an order script exactly as
// Engine.Play does (RunTo each order's tick, then Issue) and prints, every `every` ticks, where every operative, Vale, the final fight's
// hostiles and the rival crew stand (site cm), one JSON line a sample; then the play's log, one JSON line an entry.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Suborned.Sim;

static class TraceCli
{
    public static int Run(Engine.Inputs i, string root, string[] args)
    {
        var path = File.Exists(args[1]) ? args[1] : System.IO.Path.Combine(root, args[1]);
        int every = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 5;
        var sc = Script.Parse(File.ReadAllText(path));
        var g = Engine.New(i, sc.Start);
        int until = sc.Until <= 0 ? Game.T(i.SiteData.Over) : sc.Until;
        var list = new List<(int idx, Order o)>(); for (int k = 0; k < sc.Orders.Count; k++) list.Add((k, sc.Orders[k]));
        list.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.idx.CompareTo(b.idx));
        int n = 0;
        for (int t = 0; t <= until; t++)
        {
            g.RunTo(t);
            if (g.Over && g.Tick != t) break;
            while (n < list.Count && list[n].o.Tick == t) { if (g.Tick == t) g.Issue(list[n].o); n++; }
            if (t % every == 0) Console.WriteLine(Line(g));
            if (g.Over) break;
        }
        g.RunTo(until); g.Finish();
        foreach (var l in g.Log) Console.WriteLine($"{{\"log\":{l.Tick},\"code\":\"{l.Code}\",\"text\":{Q(l.Text)}}}");
        Console.WriteLine($"{{\"end\":{g.Tick},\"result\":\"{g.Result}\",\"fail\":\"{g.FailCode}\",\"trail\":{g.Trail},\"alert\":{g.Alert}}}");
        return 0;
    }
    static string Q(string s) { var b = new StringBuilder("\""); foreach (var c in s) { if (c == '"' || c == '\\') b.Append('\\'); if (c >= ' ') b.Append(c); } return b.Append('"').ToString(); }
    static string P(Pt? p) => p.HasValue ? $"[{p.Value.X},{p.Value.Y}]" : "null";
    static string Line(Game g)
    {
        var b = new StringBuilder(); b.Append($"{{\"t\":{g.Tick},\"ops\":{{");
        bool first = true;
        foreach (var o in g.Ops) { if (!first) b.Append(','); first = false; b.Append($"\"{Suborned.Sim.Op.Name(o.Id)}\":{{\"at\":[{o.Pos.X},{o.Pos.Y}],\"down\":{(o.Down ? "true" : "false")},\"car\":{(o.InCar ? "true" : "false")},\"in\":{(o.Inside ? "true" : "false")}}}"); }
        b.Append($"}},\"vale\":{P(g.PosOf("councillor"))},\"patrol\":{P(g.PosOf("patrol"))},\"h\":{{");
        first = true;
        foreach (var h in g.Hostiles) { if (!first) b.Append(','); first = false; b.Append($"\"{h.Id}\":{{\"at\":{P(h.Present ? h.Pos(g.Tick) : (Pt?)null)},\"down\":{(h.Down ? "true" : "false")},\"st\":\"{h.Gun.St}\",\"blocker\":{(h.Blocker ? "true" : "false")}}}"); }
        b.Append("},\"crew\":{");
        first = true;
        foreach (var m in g.Crew) { if (!first) b.Append(','); first = false; b.Append($"\"{m.Id}\":{{\"at\":{P(m.Fight ? (Pt?)null : g.PosOf(m.Id))},\"mode\":\"{m.Mode}\",\"fight\":{(m.Fight ? "true" : "false")}}}"); }
        b.Append("}}");
        return b.ToString();
    }
}
