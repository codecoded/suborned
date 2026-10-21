// Brief 029, the fight range (Keel): the range scenarios pass, give the same checksums twice and replay from their order log, on the
// range's own inputs; the Handover's scenario set never gains a range scenario; the host's reads change nothing; a Handover run refuses
// the range's spawn.
using System;
using System.Collections.Generic;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class RangeScenarioTests
{
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);
    public static IEnumerable<object[]> Ids => Scenarios.Range.Select(s => new object[] { s.Id });

    [Theory, MemberData(nameof(Ids))]
    public void Passes(string id)
    {
        var run = Scenarios.RunRange(Root, Scenarios.FindRange(id)!);
        var failed = run.Checks.Where(c => !c.ok).Select(c => c.what).ToList();
        Assert.True(failed.Count == 0, $"{id}: " + string.Join("; ", failed));
        Assert.NotEmpty(run.Checks);
        Assert.True(run.G.Range);
    }

    [Theory, MemberData(nameof(Ids))]
    public void SameOrdersSameChecksumsTwice(string id)
    {
        var a = Scenarios.RunRange(Root, Scenarios.FindRange(id)!).G;
        var b = Scenarios.RunRange(Root, Scenarios.FindRange(id)!).G;
        Assert.Equal(a.Checkpoints, b.Checkpoints);
        Assert.Equal(a.StartHash, b.StartHash);
    }

    [Theory, MemberData(nameof(Ids))]
    public void ReplaysFromStartStateAndOrders(string id)
    {
        var run = Scenarios.RunRange(Root, Scenarios.FindRange(id)!);
        var replay = Engine.Replay(run.Inputs!, run.G);
        Assert.Equal(run.G.Checkpoints, replay.Checkpoints);
        Assert.Equal(run.G.Result, replay.Result);
    }

    [Fact]
    public void TheHandoversSetNeverGainsARangeScenario()
    {
        Assert.DoesNotContain(Scenarios.All, s => s.Layout.Length > 0 || s.Id.StartsWith("R", StringComparison.Ordinal));
        Assert.All(Scenarios.Range, s => Assert.True(s.Layout == "open" || s.Layout == "dense" || s.Layout == "lane")); // lane: tuning.md §10.4 (F8-BREAK)
    }

    [Fact]
    public void BothLayoutsLoadWithEverySlot()
    {
        foreach (var layout in Engine.RangeSiteFiles.Keys)
        {
            var i = Engine.LoadRange(Root, layout); var g = Engine.New(i);
            Assert.True(g.Range); Assert.Equal("range", g.FightBranch); Assert.True(g.FightOn);
            foreach (var p in Game.RangePlacements) for (int k = 1; k <= Game.RangeSlots; k++) Assert.True(i.SiteData.Grid.Free(g.RangeSlot(p, k)), $"{layout} {p} {k}");
            Assert.NotEqual(Repo.Inputs.Hash, i.Hash);
        }
    }

    [Fact]
    public void TheHostsReadsChangeNothing()
    {
        var g = Engine.New(Engine.LoadRange(Root, "open"), Scenarios.RangePreset("lab"));
        Assert.True(g.Issue(new Order { Tick = 0, Do = "spawn", Target = "rivals", To = "eff", Count = 3 }).ok);
        Assert.True(g.Issue(new Order { Tick = 0, Op = "cell", Do = "panic" }).ok);
        g.RunTo(Game.T(8));
        var before = Hash.State(g);
        var lines = g.RangeLines(); var hs = g.RangeHostiles();
        foreach (var op in g.Ops) foreach (var h in hs) g.PreviewFire(op.Id, h.Id);
        Assert.Equal(before, Hash.State(g));
        Assert.NotEmpty(lines); Assert.Equal(3, hs.Count);
        Assert.Contains(lines, l => l.Shooter.StartsWith("rival-", StringComparison.Ordinal)); Assert.Contains(lines, l => l.Target.StartsWith("rival-", StringComparison.Ordinal));
    }

    [Fact]
    public void AHandoverRunRefusesTheSpawn()
    {
        var g = Engine.New(Repo.Inputs);
        var a = g.Issue(new Order { Tick = 0, Do = "spawn", Target = "police", To = "near", Count = 1 });
        Assert.False(a.ok); Assert.Equal("unknown-verb", a.code);
        Assert.Empty(g.RangeLines());
    }
}
