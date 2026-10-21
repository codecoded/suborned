using System; using System.Collections.Generic; using System.IO; using System.Linq; using Suborned.Sim; using Path = System.IO.Path;
static class P {
  static int Main(string[] args) {
    var root = Engine.FindRoot(Directory.GetCurrentDirectory());
    string rd(string f) => File.ReadAllText(Path.Combine(root, f)); string? opt(string f) => File.Exists(Path.Combine(root, f)) ? rd(f) : null;
    string rb = args[0];
    var inputs = Engine.FromTexts(File.ReadAllText(Path.Combine(root, rb)), rd(Engine.PeopleRulesFile), rd(Engine.SiteFile), rd(Engine.PeopleFile), opt(Engine.ProvisionalFile), opt(Engine.TakedownFile), opt(Engine.RoundsFile));
    var routes = new Dictionary<string,string>(); foreach (var n in new[]{"route-a-board","route-g-fast-board","route-b-board"}) routes[n]=rd("contracts/missions/handover/routes/"+n+".json");
    string preset = Env("PRESET") ?? "lab";
    Start St(){ var n=JNode.Parse(rd($"briefs/027/presets/{preset}.json"),preset); var s=Start.Parse(n["start"]); if (Env("KITE")=="0") s.LabKite=false; if (Env("SNAP")=="0") s.LabCoverSnap=false; if (Env("FIELD")=="1") s.LabField=true; if (Env("CHARGE")=="0") s.LabCharge=false; if (Env("LOADOUT") is string lo) foreach (var kv in lo.Split(',')) { var a=kv.Split('='); s.LabLoadout[Array.IndexOf(new[]{"scout","fixer","tech","enforcer"},a[0])]=a[1]; } return s; }
    Console.WriteLine($"inputs {Hash.Hex(inputs.Hash)} rulebook {rb} preset {preset}");
    foreach (var id in (Env("RUNS") ?? "L3").Split(',')) {
      Run r = Scenarios.PlayLabRun(inputs,id,St(),routes, Env("COVER")=="1");
      var rep = Engine.Replay(inputs, r.G); bool same = rep.Checkpoints.SequenceEqual(r.G.Checkpoints) && rep.Result==r.G.Result;
      var row = Scenarios.Row(id,preset,r.G);
      Console.WriteLine($"{id,-24} {row.Result+" "+row.Fail,-26} {Report.Clock(row.EndTick),-7} T {row.TrailMission}/{row.TrailTotal} A {row.Alert} St {row.StandingStart}>{row.StandingEnd} down {row.OpsDown} capt {row.OpsCaptured} hp {row.HealthLostPct}% hits {row.CellHits} foes {row.FoesDown} shots {row.CellShots} free {row.FreeDowns}/{row.FreeSide} resets {Scenarios.Resets} kites {Scenarios.Kites} | {row.Verdict}{(row.Why.Count>0?" — "+string.Join("; ",row.Why):"")}{(same?" | replay identical":" REPLAY DIFFERS")}");
      if (Env("SCRIPTS") is string dir) { Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir, $"{id.Replace(':','.')}.{preset}.script.json"), Script.Write(r.G, inputs.Hash)+"\n"); }
      if (Env("TRACE") is string tr && Env("TRACEID") == id) {
        var orders = new List<Order>(); foreach (var (o,_,_,_) in r.G.Orders) orders.Add(o);
        var list = orders.Select((o,k)=>(k,o)).OrderBy(x=>x.o.Tick).ThenBy(x=>x.k).ToList();
        var g2 = Engine.New(inputs, r.G.Start); int oi = 0; int from = int.Parse(Env("FROM") ?? "0");
        while (g2.Tick < r.G.Tick && !g2.Over) {
          while (oi < list.Count && list[oi].o.Tick == g2.Tick) { g2.Issue(list[oi].o); oi++; }
          if (g2.Tick >= from && g2.Tick % 10 == 0) foreach (var gid in tr.Split(',')) { var gg = g2.GunOf(gid); var pp = g2.PosOf(gid); if (gg == null || !pp.HasValue) continue; long best = long.MaxValue; string bn=""; foreach (var op in g2.Ops) { if (op.Down||op.Inside) continue; long d = G.D2(op.Pos, pp.Value); if (d < best) { best = d; bn = Op.Name(op.Id);} }
            Console.WriteLine($"   {Report.Clock(g2.Tick)} {gid} hp {gg.Health} st {gg.St} kiteAt {gg.KiteAt} walked {gg.KiteWalked} cm capped {gg.KiteCapped} tgt {gg.Target} nearest {bn} {Math.Sqrt(best)/100:0.0} m"); }
          g2.Step();
        }
      }
      if (Env("BAND") is string band && Env("BANDID") == id) { // BAND=target@tick[@op[@lo-hi m]]: replay the run's orders before tick, then op walks to a band point and fires on target
        var parts = band.Split('@'); string tgt = parts[0]; int t0 = int.Parse(parts[1]); string who = parts.Length > 2 ? parts[2] : "scout";
        int lo = 1600, hi = 1750; if (parts.Length > 3) { var lh = parts[3].Split('-'); lo = int.Parse(lh[0]); hi = int.Parse(lh[1]); }
        var orders = new List<Order>(); foreach (var (o,_,_,_) in r.G.Orders) orders.Add(o);
        var list = orders.Select((o,k)=>(k,o)).Where(x=>x.o.Tick < t0).OrderBy(x=>x.o.Tick).ThenBy(x=>x.k).ToList();
        var g2 = Engine.New(inputs, r.G.Start); int oi = 0;
        while (g2.Tick < t0 && !g2.Over) { while (oi < list.Count && list[oi].o.Tick == g2.Tick) { g2.Issue(list[oi].o); oi++; } g2.Step(); }
        var tp = g2.PosOf(tgt); var me = g2.Ops.First(o => Op.Name(o.Id) == who);
        Console.WriteLine($"   band: at {Report.Clock(g2.Tick)} {tgt} at {tp} hp {g2.GunOf(tgt)?.Health}; {who} at {me.Pos}");
        if (tp.HasValue) {
          var spot = g2.S.Grid.Nearest(me.Pos, q => { long d = G.D2(q, tp.Value); return d >= (long)lo*lo && d <= (long)hi*hi && g2.S.Sight(q, tp.Value, g2.R.Eye); }, 20000);
          Console.WriteLine($"   band spot {spot}");
          if (spot.HasValue) {
            var a = g2.Issue(new Order { Tick = g2.Tick, Op = who, Do = "move", Point = spot.Value }); Console.WriteLine($"   move {a.ok}");
            int end = g2.Tick + 600; bool fired = false; int logFrom = g2.Log.Count;
            while (g2.Tick < end && !g2.Over && !(g2.GunOf(tgt)?.Down ?? true)) {
              if (me.Walk == null && !fired) { if (!me.Gun.Drawn) g2.Issue(new Order { Tick = g2.Tick, Op = who, Do = "draw" }); var f = g2.Issue(new Order { Tick = g2.Tick, Op = who, Do = "fire", Target = tgt }); fired = f.ok; if (fired) Console.WriteLine($"   {Report.Clock(g2.Tick)} fire from {Math.Sqrt(G.D2(me.Pos, g2.PosOf(tgt)!.Value))/100:0.0} m"); }
              if (g2.Tick % 10 == 0 && fired) { var tq = g2.PosOf(tgt); Console.WriteLine($"   {Report.Clock(g2.Tick)} {tgt} hp {g2.GunOf(tgt)?.Health} dist {(tq.HasValue? Math.Sqrt(G.D2(me.Pos,tq.Value))/100:0):0.0} m  {who} hp {me.Gun.Health}"); }
              g2.Step();
            }
            foreach (var l in g2.Log.Skip(logFrom)) if (l.Code.StartsWith("shot") || l.Code.Contains("down") || l.Code.StartsWith("ai") || l.Code.Contains(".hit")) Console.WriteLine($"     {Report.Clock(l.Tick)} {l.Code} {l.Text}");
            g2.Finish(); var rp = Engine.Replay(inputs, g2); Console.WriteLine($"   band replay {(rp.Checkpoints.SequenceEqual(g2.Checkpoints) && rp.Result == g2.Result ? "identical" : "DIFFERS")}");
            if (Env("SCRIPTS") is string bd) File.WriteAllText(Path.Combine(bd, $"band.{tgt}.{preset}.script.json"), Script.Write(g2, inputs.Hash)+"\n");
            Console.WriteLine($"   end {Report.Clock(g2.Tick)} {tgt} down {g2.GunOf(tgt)?.Down} {who} hp {me.Gun.Health} Trail {g2.Trail} Alert {g2.Alert}");
          }
        }
      }
      if (Env("LOG") == id) foreach (var l in r.G.Log) if (Env("ALL")=="1" || l.Code.StartsWith("shot")||l.Code.StartsWith("fire")||l.Code.Contains("down")||l.Code.StartsWith("ai")||l.Code.StartsWith("fight")||l.Code.StartsWith("unit")||l.Code.StartsWith("rival")) Console.WriteLine($"   {Report.Clock(l.Tick)} {l.Code} {l.Text}");
    }
    return 0;
  }
  static string? Env(string k) => Environment.GetEnvironmentVariable(k);
}
