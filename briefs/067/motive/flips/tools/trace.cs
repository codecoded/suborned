// Motive 067f tracer: play a script on the engine DLL it was built against, from a given root, and print the named bodies each second in a window.
//   trace <root> <script.json> <from m:ss> <to m:ss> <ids,comma>   ids: hostile ids or op names (scout, fixer, tech, enforcer)
using System; using System.Collections.Generic; using System.IO; using System.Linq; using Suborned.Sim;
static class P {
  static int Clk(string s){ var p=s.Split(':'); return (int)Math.Round((int.Parse(p[0])*60+double.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture))*10); }
  static string C(int t)=> $"{t/600}:{(t%600)/10.0:00.0}";
  static int Main(string[] a){
    var inputs = Engine.Load(a[0]); var sc = Script.Parse(File.ReadAllText(a[1]));
    int from = Clk(a[2]), to = Clk(a[3]); var ids = a[4].Split(',');
    var g = Engine.New(inputs, sc.Start); int until = sc.Until > 0 ? sc.Until : Game.T(inputs.SiteData.Over);
    var orders = sc.Orders.Select((o,k)=>(k,o)).OrderBy(x=>x.o.Tick).ThenBy(x=>x.k).Select(x=>x.o).ToList(); int oi=0;
    while(!g.Over && g.Tick < until){
      while(oi<orders.Count && orders[oi].Tick==g.Tick){ g.Issue(orders[oi]); oi++; }
      if(g.Tick>=from && g.Tick<=to && g.Tick%10==0){
        var car=g.GetawayCarPos(); var enf=g.Ops[3];
        var parts=new List<string>();
        foreach(var id in ids){
          var h=g.Hostiles.FirstOrDefault(x=>x.Id==id);
          if(h!=null){ if(!h.Present){parts.Add($"{id}:absent");continue;} var p=h.Pos(g.Tick); parts.Add($"{id}:({p.X/100.0:0.0},{p.Y/100.0:0.0}) car {G.Dist(p,car)/100.0:0.0} enf {G.Dist(p,enf.Pos)/100.0:0.0} {h.Gun.St}{(h.Moving(g.Tick)?" mv":"")}{(h.Down?" DOWN":"")} home({h.Home.X/100.0:0.0},{h.Home.Y/100.0:0.0})"); continue; }
          var op=g.Ops.FirstOrDefault(o=>Op.Name(o.Id)==id);
          if(op!=null){ parts.Add($"{id}:({op.Pos.X/100.0:0.0},{op.Pos.Y/100.0:0.0}) car {G.Dist(op.Pos,car)/100.0:0.0}{(op.Down?" DOWN":"")}{(op.InCar?" incar":"")}"); }
        }
        Console.WriteLine(C(g.Tick)+"  "+string.Join(" | ",parts));
      }
      g.Step();
    }
    while(oi<orders.Count && orders[oi].Tick==g.Tick){ g.Issue(orders[oi]); oi++; }
    Console.WriteLine($"end {g.Result} {g.FailCode} {C(g.Tick)} car ({g.GetawayCarPos().X/100.0:0.0},{g.GetawayCarPos().Y/100.0:0.0})");
    return 0;
  }
}
