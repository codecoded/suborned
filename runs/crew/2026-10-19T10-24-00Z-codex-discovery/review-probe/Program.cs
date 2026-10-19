using System;
using System.Linq;
using Suborned.Sim;
var inputs=Engine.Load(Environment.CurrentDirectory);
var sc=Scenarios.Find("N56")!;
var run=Scenarios.Run(inputs,sc);
Console.WriteLine($"N56 checks={run.Checks.Count} failures={run.Checks.Count(c=>!c.ok)}");
var original=run.G.Reps.First(r=>r.Kind=="woken");
int wake=original.LandAt-Game.T(inputs.Rules.StaggerS+inputs.Rules.PostCallS);
var orders=run.G.Orders.Select(x=>x.Item1).ToArray();
foreach(var o in orders) Console.WriteLine($"order {o.Tick} {o.Op} {o.Do} {o.Target} {o.Point}");
// Keep the Enforcer beside the dropped body by omitting only N56's final retreat move.
var prefix=orders.Take(orders.Length-1).ToArray();
foreach(string action in new[]{"knockout","kill","fire"}) {
 var g=Engine.New(inputs,sc.Start);
 foreach(var o in prefix) {g.RunTo(o.Tick);g.Issue(o);}
 g.RunTo(wake+1);
 var rep=g.Reps.First(r=>r.Kind=="woken");
 var ans=g.Issue(new Order{Tick=g.Tick,Op="enforcer",Do=action=="fire"?"fire":"takedown",How=action,Target=rep.By,Force=true});
 Console.WriteLine($"{action} issued tick={g.Tick} accepted={ans.ok} reason={ans.why} phaseImmediately={rep.Phase} down={rep.P!.Down} due={rep.LandAt}");
 int firstDown=rep.P.Down?g.Tick:-1;
 while(g.Tick<rep.LandAt && !g.Over){g.RunTo(g.Tick+1);if(rep.P.Down && firstDown<0)firstDown=g.Tick;}
 g.Finish();
 var replay=Engine.Replay(inputs,g);
 Console.WriteLine($"{action} end={g.Tick} firstDown={firstDown} phase={rep.Phase} actTrail={g.Acts[rep.Act].ActTrail} down={rep.P.Down} acts={g.Acts.Count} checkpointReplay={g.Checkpoints.SequenceEqual(replay.Checkpoints)} stateReplay={Hash.State(g)==Hash.State(replay)} refusals={g.Orders.Count(x=>!x.Item2)}");
}
var n65=Scenarios.Run(inputs,Scenarios.Find("N65")!);
Console.WriteLine($"N65 checks={n65.Checks.Count} failures={n65.Checks.Count(c=>!c.ok)}");
var n47=Scenarios.Run(inputs,Scenarios.Find("N47")!);
var d=Debrief.Of(n47.G);var w=new JWriter();d.Write(w);
Console.WriteLine($"N47 checks={n47.Checks.Count} failures={n47.Checks.Count(c=>!c.ok)} cases={d.Next.Cases.Count} areaWatch={d.Next.AreaWatch.Count} jsonCases={w.ToString().Contains("\"cases\"")} jsonAreaWatch={w.ToString().Contains("\"areaWatch\"")}");
