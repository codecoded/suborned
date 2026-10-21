// Scratch (brief 031, Motive): for each reference route, how long each operative stands still inside each security watcher's sight.
using System; using System.IO; using System.Linq; using System.Collections.Generic; using Suborned.Sim;
var root = args[0]; int WARY = int.Parse(Environment.GetEnvironmentVariable("WARY") ?? "150"), REP = int.Parse(Environment.GetEnvironmentVariable("REP") ?? "300"); int robotNear = 0; var i = Engine.Load(root);
foreach (var f in args.Skip(1))
{
    var sc = Script.Parse(File.ReadAllText(f)); var g = new Game(i.Rules, i.SiteData, sc.Start, i.Hash.ToString(System.Globalization.CultureInfo.InvariantCulture), i.Crowd);
    var list = sc.Orders.Select((o, k) => (k, o)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).ToList();
    int until = sc.Until; int li = 0;
    var last = new Pt[4]; var still = new int[4]; // ticks still
    var run = new Dictionary<string, int>(); var memo = new HashSet<string>(); var rep = new Dictionary<string,int>(); var best = new Dictionary<string, (int len, int end)>();
    var cams = i.SiteData.Cameras.Select(c => c.Id).ToList();
    for (int k = 0; k < 4; k++) last[k] = g.Ops[k].Pos;
    while (g.Tick < until && !g.Over)
    {
        while (li < list.Count && list[li].o.Tick == g.Tick) { g.Issue(list[li].o); li++; }
        g.Step();
        for (int k = 0; k < 4; k++)
        {
            var op = g.Ops[k]; bool moved = op.Pos.X != last[k].X || op.Pos.Y != last[k].Y; last[k] = op.Pos;
            if (op.InCar || op.Inside) { foreach (var key in run.Keys.Where(x => x.StartsWith(op.Id + "|")).ToList()) run[key] = 0; continue; }
            var seen = new List<string>();
            foreach (var c in cams) if (g.CameraWatching(c, op.Pos)) seen.Add("cam:" + c);
            if (g.GuardWatching(op.Pos)) seen.Add("guard"); if (g.PatrolWatching(op.Pos)) seen.Add("patrol"); if (g.SecurityWatching(op.Pos)) seen.Add("security");
            foreach (var u in g.Units) { if (!u.Robot || !u.OnStreet(g.Tick)) continue; var up = g.UnitPose(u); if (!up.HasValue) continue;
                if (G.Within(up.Value.pos, op.Pos, 300) && !G.InCone(up.Value.pos, up.Value.facing, g.R.SecReachCm, g.R.SecHalfCm, 0, op.Pos)) robotNear++; }
            { var gp = g.GuardPose(); if (gp.HasValue && G.Within(gp.Value.pos, op.Pos, 300)) seen.Add("guardnear"); }
            foreach (var w in new[] { "guard", "guardnear", "patrol", "security" }.Concat(cams.Select(c => "cam:" + c)))
            {
                var key = op.Id + "|" + w; run.TryGetValue(key, out var r);
                bool insight = seen.Contains(w); string busy = op.Busy?.Do ?? "";
                bool talk = busy is "listen" or "turn" or "call" || (busy.Length > 0 && busy != "watch");
                if (insight && !moved && !talk) r += 1; else if (insight && talk) { } else r = Math.Max(memo.Contains(key) ? WARY : 0, r - 1);
                if (r >= WARY) memo.Add(key);
                if (r >= REP && !rep.ContainsKey(key)) rep[key] = g.Tick;
                run[key] = r;
                if (r > 0 && (!best.ContainsKey(key) || best[key].len < r)) best[key] = (r, g.Tick);
            }
        }
    }
    Console.WriteLine($"== {System.IO.Path.GetFileName(f)} robot-near-only ticks {robotNear}"); robotNear = 0;
    Console.WriteLine($"    {g.Result} {Report.Clock(g.Tick)}");
    foreach (var kv in best.OrderByDescending(x => x.Value.len)) if (kv.Value.len >= 50) Console.WriteLine($"   {kv.Key,-24} peak {kv.Value.len / 10.0,6:0.0} s at {Report.Clock(kv.Value.end)}{(memo.Contains(kv.Key) ? "  WARY" : "")}{(rep.ContainsKey(kv.Key) ? "  REPORT at " + Report.Clock(rep[kv.Key]) : "")}");
}
