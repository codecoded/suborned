using System;
using System.Linq;
using System.Reflection;
using Suborned.Sim;
var inputs=Engine.Load(Environment.CurrentDirectory);
var sc=Scenarios.Find("N56")!;
var run=Scenarios.Run(inputs,sc);
Console.WriteLine("N56 baseline: "+run.Checks.Count+" checks, failures="+run.Checks.Count(c=>!c.ok));
foreach(var c in run.Checks.Where(c=>!c.ok)) Console.WriteLine("FAIL "+c.what);
var original=run.G.Reps.First(r=>r.Kind=="woken");
int wake=original.LandAt-Game.T(inputs.Rules.StaggerS+inputs.Rules.PostCallS);
var orders=run.G.Orders.Select(x=>x.Item1).ToArray();
foreach(string kind in new[]{"woken","seen"}) {
 var g=Engine.Play(inputs,sc.Start,orders,wake);
 var rep=g.Reps.First(r=>r.Kind=="woken");
 rep.Kind=kind; rep.P!.Down=true;
 // Narrow white-box boundary fixture: reporter incapacitated before the pending report lands.
 g.RunTo(rep.LandAt);
 Console.WriteLine($"interruption kind={kind} reporterDown={rep.P.Down} reportPhase={rep.Phase} actTrail={g.Acts[rep.Act].ActTrail} tick={g.Tick} due={rep.LandAt}");
}
foreach(bool down in new[]{false,true}) {
 var g=Engine.New(inputs,new Start {RivalsAtS=9999,FinalFight=false,Crowd=false});
 g.Driver.Down=down;
 g.RunTo(Game.T(inputs.SiteData.Over));
 Console.WriteLine($"deadline fixture driverDown={down} result={g.Result} cause={g.FailCode} Vale={g.Vale} tick={g.Tick}");
}
