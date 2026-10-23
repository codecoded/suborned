// Engine 0.25.0, brief 045 F5 (Redline, late lag): a Watch or Listen query out of reach (LearnQuery, engage false) asks only whether the
// walk in exists (CanWalkIn: one route to where the person stands), not the intercept (up to 120 routes along a server's round). The host's
// gates ask Listen on three people every frame. The answer must be the one the intercept gives: it is null exactly when that one route is.
//    QueryAgreesWithTheIntercept   every 5 s of Sky's two 0.2.58 sessions and route A, for the Scout's Listen and Watch and the Fixer's
//                                  Watch on the waitress, the driver and the guard: taken ⇒ WalkInS (the intercept) finds a walk;
//                                  no-walk ⇒ it finds none
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Sim;
using Xunit;
using Path = System.IO.Path;

public class GateQueryTests
{
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);

    [Theory]
    [InlineData("briefs/045/runs/sky-0.2.58-session1.script.json")]
    [InlineData("briefs/045/runs/sky-0.2.58-session2.script.json")]
    [InlineData("contracts/missions/handover/routes/route-a.json")]
    public void QueryAgreesWithTheIntercept(string file)
    {
        var sc = Script.Parse(File.ReadAllText(Path.Combine(Root, file)));
        var g = Engine.New(Repo.Inputs, sc.Start); int until = sc.Until > 0 ? sc.Until : Game.T(Repo.Inputs.SiteData.Over);
        var os = sc.Orders.Select((o, k) => (o, k)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).Select(x => x.o).ToList(); int oi = 0, walks = 0, asked = 0;
        var bad = new List<string>();
        while (!g.Over && g.Tick < until)
        {
            while (oi < os.Count && os[oi].Tick <= g.Tick) { var o = os[oi++]; if (o.Tick == g.Tick) g.Issue(o); }
            if (g.Tick % Game.T(5) == 0) // every 5 s: the intercept it is checked against is the dear part (checks under 10 min)
                foreach (var role in new[] { "waitress", "driver", "guard" })
                    foreach (var (agent, verb) in new[] { ("scout", "listen"), ("scout", "watch"), ("fixer", "watch") })
                    {
                        var a = g.LearnQuery(agent, verb, role); int w = g.WalkInS(agent, verb, role); asked++;
                        if (a.ok && w > 0) walks++;
                        if ((a.ok && w < 0 && !g.Ops[(int)Op.Parse(agent)].Driven) || (a.code == "no-walk" && w >= 0)) bad.Add($"{Report.Clock(g.Tick)} {agent} {verb} {role}: {(a.ok ? "taken" : a.code)}, WalkInS {w}");
                    }
            g.Step();
        }
        Assert.True(bad.Count == 0, $"{file}: {string.Join("; ", bad.Take(8))}");
        Assert.True(walks > 0, $"{file}: some queries were answered out of reach ({walks} of {asked})");
    }
}
