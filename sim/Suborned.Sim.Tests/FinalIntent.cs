// Engine 0.20.0, tuning.md §15.2 (PROVISIONAL until Sky plays it): ai.intent.final agent. Motive §15.5's F15-RANGE and F15-SWEEP, and the
// route moves, as both-ways checks: the same runs under final agent (the rulebook) and final every (0.19.0's rule, Scenarios.WithFinal).
//    F15_RANGE   every range run hashes the same both ways (the range always reads every)
//    F15_SWEEP   the fight-lab matrix's eight rows under today and the lab hash the same both ways
//    F15_ROUTES  every route script hashes the same both ways, but the 5 loud rows and 3 failure rows Motive lists (§15.2); outcome and
//                fail code stay everywhere, Trail and Alert too but on the failure rows (fewer hits can cost less Trail)
// F15-CLEAN, F15-DRAWN and F15-FACE are scenarios (Suborned.Sim/ScenariosFinalIntent.cs).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Sim;
using Xunit;
using Path = System.IO.Path;

public class FinalIntentTests
{
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);
    static readonly Engine.Inputs Agent = Repo.Inputs;
    static readonly Engine.Inputs Every = Scenarios.WithFinal(Repo.Inputs, "every");

    /// <summary>Motive §15.2's measured moves: the rows whose hashes move (outcome, fail, T and Alert do not).</summary>
    static readonly HashSet<string> Moves = new HashSet<string>(StringComparer.Ordinal)
    {
        "loud", "loud-fast", "loud-fight", "loud-left-downed", "loud-left-standing",
        "careless-takedown-seen-crowd", "fight-loud-enforcer-down", "fight-safe-rivals-reach-vale",
    };

    [Fact]
    public void TheRulebookReadsAgentAndTheControlEvery()
    {
        Assert.False(Agent.Rules.Intent.FinalEvery);
        Assert.True(Every.Rules.Intent.FinalEvery);
        Assert.Equal(Agent.Hash, Every.Hash); // behaviour-only: the texts are the rulebook's, so start hashes and checkpoints compare directly
        Assert.Throws<FormatException>(() => Scenarios.WithFinal(Engine.FromTexts(Agent.Rulebook.Replace("\"final\": \"agent\"", "\"final\": \"some\""), Agent.PeopleRules, Agent.Site), "every"));
    }

    public static IEnumerable<object[]> RangeIds => Scenarios.Range.Select(s => new object[] { s.Id });

    [Theory, MemberData(nameof(RangeIds))]
    public void F15_RANGE(string id)
    {
        var sc = Scenarios.FindRange(id)!; var i = Engine.LoadRange(Root, sc.Layout);
        Assert.False(i.Rules.Intent.FinalEvery);
        var a = Scenarios.Run(i, sc).G; var b = Scenarios.Run(Scenarios.WithFinal(i, "every"), sc).G;
        Assert.Equal(a.Checkpoints, b.Checkpoints);
        Assert.Equal((a.Result, a.FailCode), (b.Result, b.FailCode));
    }

    static Dictionary<string, string> LabRoutes()
    {
        var d = new Dictionary<string, string>();
        foreach (var n in new[] { "route-a-board", "route-g-fast-board", "route-b-board" }) d[n] = File.ReadAllText(Path.Combine(Root, "contracts/missions/handover/routes", n + ".json"));
        return d;
    }
    static Start Preset(string name) => Start.Parse(JNode.Parse(File.ReadAllText(Path.Combine(Root, "briefs/027/presets", name + ".json")), name)["start"]);

    public static IEnumerable<object[]> LabRows => from p in new[] { "today", "lab" } from id in Scenarios.LabRuns select new object[] { p, id };

    [Theory, MemberData(nameof(LabRows))]
    public void F15_SWEEP(string preset, string id)
    {
        var routes = LabRoutes();
        var a = Scenarios.PlayLabRun(Agent, id, Preset(preset), routes).G;
        var b = Scenarios.PlayLabRun(Every, id, Preset(preset), routes).G;
        Assert.Equal(a.Checkpoints, b.Checkpoints);
        Assert.Equal((a.Result, a.FailCode, a.Trail, a.Alert), (b.Result, b.FailCode, b.Trail, b.Alert));
    }

    public static IEnumerable<object[]> RouteFiles()
    {
        var dir = Path.Combine(Root, "contracts/missions/handover/routes");
        foreach (var f in Directory.GetFiles(dir, "*.json").OrderBy(x => x, StringComparer.Ordinal))
            if (File.ReadAllText(f).Contains("suborned.sim.script/1")) yield return new object[] { Path.GetFileNameWithoutExtension(f) };
    }

    [Theory, MemberData(nameof(RouteFiles))]
    public void F15_ROUTES(string name)
    {
        var script = Script.Parse(File.ReadAllText(Path.Combine(Root, "contracts/missions/handover/routes", name + ".json")));
        var a = Engine.Play(Agent, script.Start, script.Orders, script.Until);
        var b = Engine.Play(Every, script.Start, script.Orders, script.Until);
        Assert.Equal((a.Result, a.FailCode), (b.Result, b.FailCode));
        if (!name.StartsWith("careless-", StringComparison.Ordinal) && !name.StartsWith("fight-", StringComparison.Ordinal)) Assert.Equal((a.Trail, a.Alert), (b.Trail, b.Alert)); // the failure rows' Trail may fall (fight-loud-enforcer-down 9 -> 8: fewer hits)
        if (Moves.Contains(name)) Assert.NotEqual(a.Checkpoints, b.Checkpoints); // Motive §15.2: the agents who did nothing are no longer shot
        else Assert.Equal(a.Checkpoints, b.Checkpoints);
    }
}
