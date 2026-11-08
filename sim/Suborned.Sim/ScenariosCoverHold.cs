// Brief 098 (engine 0.50.0; Aster's ruling on the 0.49.0 report): fighters hold cover (rulebook fight.cover), on the fight range. Cover is
// somewhere to fight from: a rival whose target is in his reach steps only to cover within fight.cover.reach of his effective reach of that
// target, with the line to him; with none, he fights from where he stands. (The holding and the commitment are read across the Handover's
// fights: sim/Suborned.Sim.Tests/CoverHold.cs.)
//   CH-REACH-NONE  the open range at the effective ring: its low walls' arcs miss the cell and a car's cover stands only with a car, so no
//                  cover keeps a rival's target in reach: the rivals take none, and fire from where they stand
//   CH-REACH-CAR   a car on the first slot and the rivals near the cell: each cover a rival takes, he takes within his reach of his target
using System;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static void CoverHoldRange()
        {
            AddRange("CH-REACH-NONE", "today", "open", "fight.cover (engine 0.50.0): cover is somewhere to fight from: with none in reach of his target, a rival fights from where he stands", r =>
            {
                var g = r.G; foreach (var op in new[] { "scout", "fixer", "tech", "enforcer" }) r.Do(op, "draw"); Spawned(r, "rivals", "eff", 3); int t0 = g.Tick;
                r.Wait(Game.T(6));
                r.Is(!g.Log.Exists(l => l.Tick >= t0 && l.Code == "ai.cover"), "no rival takes cover: on the open range none keeps his target in his reach");
                int shots = g.Log.Count(l => l.Tick >= t0 && (l.Code == "shot.hit" || l.Code == "shot.miss") && l.Text.StartsWith("rival-", StringComparison.Ordinal));
                r.Is(shots >= 3, $"they fire from where they stand ({shots} shots in 6 s)");
            });
            AddRange("CH-REACH-CAR", "today", "open", "fight.cover (engine 0.50.0): a rival with his target in reach takes only cover from which his target stays in his reach", r =>
            {
                var g = r.G; foreach (var op in new[] { "scout", "fixer", "tech", "enforcer" }) r.Do(op, "draw");
                r.Is(g.Issue(new Order { Tick = g.Tick, Do = "spawn", Target = "car", To = "car:1", Count = 1 }).ok, "a car on the first slot");
                Spawned(r, "rivals", "near", 3); int taken = 0, bad = 0; string ex = "";
                for (int k = 0; k < Game.T(10); k++)
                {
                    g.Step();
                    foreach (var x in g.Log.Where(l => l.Tick == g.Tick && l.Code == "ai.cover"))
                    {
                        string by = x.Text.Substring(0, x.Text.IndexOf(' ')), cv = x.Text.Substring(x.Text.IndexOf(" at ", StringComparison.Ordinal) + 4); cv = cv.Substring(0, cv.IndexOf(' '));
                        var h = H(r, by); var c = g.CoverNow().Find(p => p.Id == cv); var tp = h.Gun.Target.Length > 0 ? g.PosOf(h.Gun.Target) : null; taken++;
                        if (c == null || !tp.HasValue || G.Dist(c.At, tp.Value) > (long)h.Gun.W!.EffCm * g.R.CoverHoldReachPerMille / 1000) { bad++; ex = $"{by} to {cv} ({(c != null && tp.HasValue ? G.Dist(c.At, tp.Value) : -1)} cm from {h.Gun.Target})"; }
                    }
                }
                r.Is(taken > 0, $"rivals near the cell take cover behind the car ({taken})");
                r.Eq(bad, 0, $"each within his effective reach of his target{(ex.Length > 0 ? ": " + ex : "")}");
            });
        }
    }
}
