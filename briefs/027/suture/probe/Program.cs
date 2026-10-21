using System; using System.Collections.Generic; using System.IO; using System.Linq; using Suborned.Sim; using Path = System.IO.Path;
static class P {
  static int Main(string[] args) {
    var root = Engine.FindRoot(Directory.GetCurrentDirectory());
    string rd(string f) => File.ReadAllText(Path.Combine(root, f)); string? opt(string f) => File.Exists(Path.Combine(root, f)) ? rd(f) : null;
    string rb = args.Length > 0 && args[0].EndsWith(".json") ? args[0] : "out/suture027/rulebook.snapshot.json";
    var inputs = Engine.FromTexts(File.ReadAllText(Path.Combine(root, rb)), rd(Engine.PeopleRulesFile), rd(Engine.SiteFile), rd(Engine.PeopleFile), opt(Engine.ProvisionalFile), opt(Engine.TakedownFile), opt(Engine.RoundsFile));
    var routes = new Dictionary<string,string>(); foreach (var n in new[]{"route-a-board","route-g-fast-board","route-b-board"}) routes[n]=rd("contracts/missions/handover/routes/"+n+".json");
    Start Lab(Action<Start>? f=null){ var n=JNode.Parse(rd("briefs/027/presets/lab.json"),"lab"); var s=Start.Parse(n["start"]); if (Env("FIELD")=="1") s.LabField = true; if (Env("CHARGE")=="0") s.LabCharge = false; f?.Invoke(s); return s; }
    if (Env("MICRO")=="1") { foreach (var (c,h,gp) in new[]{(1,5,25),(10,5,10),(10,15,5),(30,10,5)}) Console.WriteLine(Scenarios.PanicSpam(inputs,c,h,gp)); foreach (var (n,cb) in new[]{(6,false),(1,false),(1,true),(3,true)}) Console.WriteLine(Scenarios.VolleyCharge(inputs,n,30,cb)); foreach (var a in new[]{250,500,600,1000}) { var pt=Scenarios.Pattern(a,500,8); Console.WriteLine($"pattern {a}: plain {pt.plain} reset {pt.reset}"); } return 0; }
    var runs = (Env("RUNS") ?? "L1c,L3,P-A,P-G,P-B").Split(',');
    var pols = (Env("POLS") ?? "matrix,focus,reset,focus-reset,kite,focus-reset-kite").Split(',');
    Console.WriteLine($"inputs {Hash.Hex(inputs.Hash)} rulebook {rb}");
    Console.WriteLine("policy             run  result             end     T  A St     down hp-lost hits foes shots resets verdict");
    foreach (var id in runs) foreach (var pn in pols) {
      Run r = pn=="matrix" ? Scenarios.PlayLabRun(inputs,id,Lab(),routes) : Scenarios.PlaySmart(inputs,id,Lab(),routes,Pol.Parse(pn));
      var rep = Engine.Replay(inputs, r.G); bool same = rep.Checkpoints.SequenceEqual(r.G.Checkpoints) && rep.Result==r.G.Result;
      var row = Scenarios.Row(id,"lab",r.G);
      Console.WriteLine($"{pn,-18} {id,-4} {row.Result+" "+row.Fail,-18} {Report.Clock(row.EndTick),-7} {row.TrailTotal,2} {row.Alert} {row.StandingStart,2}>{row.StandingEnd,-3} {row.OpsDown,4} {row.HealthLostPct,5}% {row.CellHits,4} {row.FoesDown,4} {row.CellShots,5} {(pn=="matrix"?0:Scenarios.Resets),6} {row.Verdict}{(row.Why.Count>0?" — "+string.Join("; ",row.Why):"")}{(same?"":" REPLAY DIFFERS")}");
      if (Env("SCRIPTS") is string dir) { Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir, $"{pn}.{id}.script.json"), Script.Write(r.G, inputs.Hash)+"\n"); }
      if (Env("LOG") == id+":"+pn) foreach (var l in r.G.Log) if (Env("ALL")=="1" || l.Code.StartsWith("shot")||l.Code.StartsWith("fire")||l.Code.Contains("down")||l.Code.StartsWith("ai")||l.Code.StartsWith("fight")) Console.WriteLine($"   {Report.Clock(l.Tick)} {l.Code} {l.Text}");
    }
    return 0;
  }
  static string? Env(string k) => Environment.GetEnvironmentVariable(k);
}
