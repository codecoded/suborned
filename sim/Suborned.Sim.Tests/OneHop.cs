// One hop (Pulse §3.4; rulebook memory: a told entry is never retold), held as a law across route scripts and seeds (brief 098: it replaces the
// scenario N82, whose pinned chain the law L9 moved). Whoever is told of an act and goes to report it tells nobody at their phone.
using System;
using System.Collections.Generic;
using System.Linq;
using IOPath = System.IO.Path; using File = System.IO.File;
using Suborned.Sim;
using Xunit;

public class OneHopTests
{
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);

    [Fact]
    public void ATellerToldOfAnActTellsNobodyWhenTheirReportLands()
    {
        int reports = 0; var bad = new List<string>();
        foreach (var route in new[] { "careless-takedown-seen-crowd", "careless-body-found", "careless-reporter-not-stopped", "route-g", "route-g-kill", "loud" })
            foreach (var seed in new[] { 0, 1, 2, 3 })
            {
                var s = Script.Parse(File.ReadAllText(IOPath.Combine(Root, "contracts/missions/handover/routes", route + ".json"))); s.Start.Seed = seed;
                var g = Engine.Play(Repo.Inputs, s.Start, s.Orders, s.Until);
                foreach (var rep in g.Reps.Where(x => x.P != null && x.Phase == "landed" && g.Acts.Exists(t => t.Id == x.Act && t.Told.Contains(x.P.Id))))
                {
                    reports++;
                    var everyone = g.People.Concat(g.Walkers.Select(w => w.P));
                    foreach (var q in everyone) if (q.Memory.Exists(m => m.Act == "told" && m.By == rep.P!.Id && m.Tick >= rep.LandAt)) bad.Add($"{route} #{seed}: {rep.P!.Id}, told, retold {q.Id} at their phone");
                }
            }
        Assert.True(reports > 0, "someone told of an act went to report it and the report landed");
        Assert.True(bad.Count == 0, string.Join("; ", bad.Take(5)));
    }
}
