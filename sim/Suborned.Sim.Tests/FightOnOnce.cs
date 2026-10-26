// Engine 0.35.2 (Fable, briefs/039/fable-fight-on.md; log only): every committed route script, played on this engine, logs "A fight is on"
// (fire.free-on) once per fight: never twice without a fire.free-off between. 0.35.1: 16 repeats over 11 routes (each lull in the final fight
// under one input, where Motive's ruling 1 says no fire.free-off). A test, not a scenario: it reads the repo's routes (as GateC037).
using System;
using System.Collections.Generic;
using IOPath = System.IO.Path; using File = System.IO.File;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class FightOnOnceTests
{
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);
    public static IEnumerable<object[]> Routes => GateC037Tests.Routes;

    [Theory, MemberData(nameof(Routes))]
    public void OneFightOnLinePerFight(string id)
    {
        var s = Script.Parse(File.ReadAllText(IOPath.Combine(Root, "contracts/missions/handover/routes", id + ".json")));
        var g = Engine.Play(Repo.Inputs, s.Start, s.Orders, s.Until);
        bool on = false; var seq = new List<string>();
        foreach (var l in g.Log)
        {
            if (l.Code == "fire.free-on") { Assert.False(on, $"{id}: fire.free-on again at {Report.Clock(l.Tick)} with no fire.free-off since ({string.Join(" ", seq)})"); on = true; seq.Add("on " + Report.Clock(l.Tick)); }
            else if (l.Code == "fire.free-off") { on = false; seq.Add("off " + Report.Clock(l.Tick)); }
        }
    }
}
