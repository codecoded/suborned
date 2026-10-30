// Engine 0.20.0, tuning.md §15.2 (PROVISIONAL until Sky plays it): ai.intent.final agent. Motive §15.5's F15-RANGE and F15-SWEEP, and the
// route moves, as both-ways checks: the same runs under final agent (the rulebook) and final every (0.19.0's rule, Scenarios.WithFinal).
//    F15_RANGE   every range run hashes the same both ways (the range always reads every)
//    F15_SWEEP   the fight-lab matrix's eight rows under today and the lab hash the same both ways
//    F15_ROUTES  every route script hashes the same both ways, but the 5 loud rows and 3 failure rows Motive lists (§15.2); outcome and
//                fail code stay everywhere, Trail and Alert too but on the failure rows (fewer hits can cost less Trail). Engine 0.34.0: a
//                safe fight where a rival advances (M067-SQ2, which reads the intent) is left out: it differs both ways by the rule.
//                c038: the set (Moves) is the loud rows less the retired loud-fast, plus the two loud-wait twins
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
        "fight-loud-enforcer-down",
        // c038 (Waymark, brief 067 §5.1): the loud-wait twins. Outcome, fail code, Trail and Alert agree both ways; under every the final
        // fight's police fire on the cell in the car who gave them no cause (square: security-1/-2 take the fixer, "final", and shoot him
        // down in the car at 3:49.1; yard: officer-2/-3 take the scout, aboard since 6:57.6 and never fired, at 7:27.5). Keel, 2026-10-26
        "fight-loud-wait-square",
        // fight-loud-wait-yard left the set with engine 0.35.0 (brief 039 B, one input): its drawn cell fights on its own once the loud final fight
        // comes (4 shots before the Enforcer boards at 7:29.3), so every officer has cause both ways and the two runs agree; Waymark re-records it
        // loud-fast left the set with c038: the route is retired (Waymark, brief 067)
        // careless-takedown-seen-crowd left the set with engine 0.28.0: guard 73's radio locks the doors before the Enter, so the run has no
        // final fight and the agent and every rules cannot differ (Aster via Waymark, 2026-10-24)
        // fight-safe-rivals-reach-vale left the set with engine 0.32.0 (brief 059): the rival car tails Vale's round to the service bay and its
        // wheelman reaches Vale's door at 6:36.0 with no shot fired (it was the safe fight's rivals at 7:29.3), so the two rules cannot differ
        // (Keel; Waymark re-composes the route)
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
        Start Old() { var st = Preset(preset); st.Pre080 = true; st.OldClock = true; return st; } // brief 080: on the shots and the clock the sweep was written for
        var a = Scenarios.PlayLabRun(Agent, id, Old(), routes).G;
        var b = Scenarios.PlayLabRun(Every, id, Old(), routes).G;
        if (!(preset == "lab" && id == "P-B")) Assert.Equal(a.Checkpoints, b.Checkpoints); // brief 080: the lab's P-B now reaches a fight the two rules read differently (talk is shorter on its fixed ticks); it still ends alike
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
        script.Start.Pre080 = true; script.Start.OldClock = true; // brief 080: this reads ai.intent.final both ways, on the shots and the clock its lists of rows were written for
        var a = Engine.Play(Agent, script.Start, script.Orders, script.Until);
        var b = Engine.Play(Every, script.Start, script.Orders, script.Until);
        // engine 0.34.0 (Motive M067-SQ2, ai.noStall): a rival with nobody in reach advances on an operative he has cause to fire on, which is
        // the intent's own reading (agent: provoked; every: anyone), so a safe fight where a rival advances runs differently both ways, its
        // outcome too (route-a-pressure: rivals-reach-vale under agent, a win under every). Those rows say only that the advance is the safe
        // fight's: both runs are safe fights, and one of them advanced
        bool advanced(Game g) => g.Log.Exists(l => l.Code == "ai.advance");
        if (advanced(a) || advanced(b)) { Assert.Equal(("safe", "safe"), (a.FightBranch, b.FightBranch)); return; }
        // engine 0.35.2 (Motive rulings-0.35.1 §2): the rivals walking to an unguarded held car fire as they walk at whoever the intent lets
        // them (agent: a cell that never drew, nobody; every: anyone), so the walk's hashes differ both ways by the rule; the outcome does not
        bool walked(Game g) => g.Log.Exists(l => l.Code == "rivals.for-car");
        if (walked(a) || walked(b)) { Assert.Equal(("safe", "safe"), (a.FightBranch, b.FightBranch)); Assert.Equal((a.Result, a.FailCode), (b.Result, b.FailCode)); return; }
        Assert.Equal((a.Result, a.FailCode), (b.Result, b.FailCode));
        if (!name.StartsWith("careless-", StringComparison.Ordinal) && !name.StartsWith("fight-", StringComparison.Ordinal) && !Moves.Contains(name)) Assert.Equal((a.Trail, a.Alert), (b.Trail, b.Alert)); // the failure rows' Trail may fall (fight-loud-enforcer-down 9 -> 8: fewer hits)
        else if (Moves.Contains(name)) { Assert.Equal(a.Alert, b.Alert); Assert.True(a.Trail <= b.Trail, $"{name}: Trail {a.Trail} under agent, {b.Trail} under every"); } // engine 0.33.1 (car.footprint): on the loud rows the agents who did nothing are no longer shot, and the Trail may differ too (12 under agent, 14 under every)
        if (Moves.Contains(name)) Assert.NotEqual(a.Checkpoints, b.Checkpoints); // Motive §15.2: the agents who did nothing are no longer shot
        else Assert.Equal(a.Checkpoints, b.Checkpoints);
    }
}
