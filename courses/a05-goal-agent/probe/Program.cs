using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Suborned.Sim;

var root = args[0];
var inputs = Engine.Load(root);
void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); }
var g = Engine.New(inputs);
var bin = g.S.Stashes.First(s => s.Kind == "container");
var scout = g.OpOf(OpId.Scout);
Pt? blocked = null;
for (int i = 0; i < g.S.Grid.Nx && blocked == null; i++)
for (int j = 0; j < g.S.Grid.Ny && blocked == null; j++)
{
    var p = g.S.Grid.Centre(i, j);
    if (g.S.Grid.Free(p) && G.Within(p, bin.Stand, g.R.SightReachCm) && !g.S.Sight(p, bin.Stand, g.R.Eye)) blocked = p;
}
Check(blocked != null, "walkable point in range blocked by actual site geometry exists");
scout.Pos = blocked!.Value;
Check(g.LearnQuery("scout", "watch", bin.Id).code == "no-clear-line", "query refuses blocked sightline");
Check(g.Issue(new Order { Op = "scout", Do = "watch", Target = bin.Id }).code == "no-clear-line", "Issue agrees on blocked sightline");
scout.Pos = bin.Stand;
Check(g.Issue(new Order { Op = "scout", Do = "watch", Target = bin.Id }).ok, "Scout begins container Watch");
g.RunTo(scout.BusyEnd);
Check(g.KnowsRound(bin.Id), "completed Watch stores knowledge");
scout.Gun.Down = true;
Check(g.NextRound(bin.Id).known, "knowledge survives Scout downed");
Check(!Engine.New(inputs).NextRound(bin.Id).known, "new mission does not inherit container knowledge");
scout.Gun.Down = false;
var field = typeof(Game).GetField("roundsKnown", BindingFlags.NonPublic | BindingFlags.Instance)!;
var knowledge = (IList)field.GetValue(g)!;
var item = knowledge[0]; var learnedHash = Hash.State(g);
knowledge.Clear();
Check(Hash.State(g) != learnedHash, "knowledge itself changes the state hash, independently of logs and orders");
knowledge.Add(item);
Check(Hash.State(g) == learnedHash, "restored knowledge restores exact state hash");
scout.InCar = true;
Console.WriteLine("Inherited in-car query: " + g.LearnQuery("scout", "watch", bin.Id).code + "; Issue: " + g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "watch", Target = bin.Id }).code);
