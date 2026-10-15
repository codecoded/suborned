// dotnet test sim/Suborned.Sim.Tests — the done-check of brief 011 A: the 28 scenarios pass headless; the same orders give
// byte-identical checksums twice; a replay from the order log matches; the rules agree with contracts/rules/rulebook.test.ts.
using System;
using System.Collections.Generic;
using System.Linq;
using Suborned.Sim;
using Xunit;

public static class Repo
{
    public static readonly Engine.Inputs Inputs = Engine.Load(Engine.FindRoot(AppContext.BaseDirectory));
}

public class ScenarioTests
{
    public static IEnumerable<object[]> Ids => Scenarios.All.Select(s => new object[] { s.Id });

    [Theory, MemberData(nameof(Ids))]
    public void Passes(string id)
    {
        var run = Scenarios.Run(Repo.Inputs, Scenarios.Find(id)!);
        var failed = run.Checks.Where(c => !c.ok).Select(c => c.what).ToList();
        Assert.True(failed.Count == 0, $"{id}: " + string.Join("; ", failed));
        Assert.NotEmpty(run.Checks);
    }

    [Theory, MemberData(nameof(Ids))]
    public void SameOrdersSameChecksumsTwice(string id)
    {
        var a = Scenarios.Run(Repo.Inputs, Scenarios.Find(id)!).G;
        var b = Scenarios.Run(Repo.Inputs, Scenarios.Find(id)!).G;
        Assert.Equal(a.Checkpoints, b.Checkpoints);
        Assert.Equal(a.StartHash, b.StartHash);
    }

    [Theory, MemberData(nameof(Ids))]
    public void ReplaysFromStartStateAndOrders(string id)
    {
        var played = Scenarios.Run(Repo.Inputs, Scenarios.Find(id)!).G;
        var replay = Engine.Replay(Repo.Inputs, played);
        Assert.Equal(played.Checkpoints, replay.Checkpoints);
        Assert.Equal(played.Result, replay.Result);
    }

    [Fact]
    public void TwentyEightPortedPlusTheEnginesNCases()
    {
        Assert.Equal(28, Scenarios.All.Count(s => s.Id.StartsWith("S")));
        Assert.Equal(Enumerable.Range(1, 28).Select(i => $"S{i:00}"), Scenarios.All.Where(s => s.Id.StartsWith("S")).Select(s => s.Id));
    }

    [Fact]
    public void AScriptRoundTripsThroughTheOrderFormat()
    {
        var played = Scenarios.Run(Repo.Inputs, Scenarios.Find("S10")!).G;
        var script = Script.Parse(Script.Write(played, Repo.Inputs.Hash));
        var replay = Engine.Play(Repo.Inputs, script.Start, script.Orders, script.Until);
        Assert.Equal(Outcome.Success, replay.Result);
        Assert.Equal(played.Checkpoints, replay.Checkpoints);
        Assert.Equal("", script.Verify(replay, Repo.Inputs.Hash));
        Assert.NotEqual("", script.Verify(replay, Repo.Inputs.Hash ^ 1)); // other inputs: the record says so
    }
}

public class RulesTests
{
    static Game New(Start? s = null) => Engine.New(Repo.Inputs, s);
    static Start Knowing(string temperament)
    {
        var s = new Start(); s.Temperament["waitress"] = temperament; s.Known["waitress"] = new List<string> { "routine", "need", "temperament" }; return s;
    }

    [Fact]
    public void InezReferencePricesFromTheRulebook()
    {
        var g = New(Knowing("idealist"));
        var help = g.Preview("help", "waitress");
        Assert.Equal((40000L, 50L, 1L, 1L, "turned"), (help.Usd, help.S, help.Trail, help.Orders, help.Outcome));
        Assert.Equal("refused", g.Preview("pay", "waitress").Outcome); // Sky 2026-10-15: Inez is an Idealist, Pay is the lesson
        Assert.Equal(4L, g.Preview("pressure", "waitress").Trail);    // one step worse
        Assert.Equal("refused", g.Preview("ask", "waitress").Outcome);
    }

    [Theory]
    [InlineData("mercenary", "pay", 70000, 1, "turned")]
    [InlineData("mercenary", "help", 40000, 2, "not-yet")]
    [InlineData("fearful", "pay", 90000, 1, "turned")]
    [InlineData("loyal", "help", 40000, 2, "not-yet")]
    [InlineData("loyal", "pay", 90000, 1, "turned")]
    public void Q4GridMatchesTheTypeScriptEvaluator(string t, string approach, long usd, long orders, string outcome)
    {
        var q = New(Knowing(t)).Preview(approach, "waitress");
        Assert.Equal((usd, orders, outcome), (q.Usd, q.Orders, q.Outcome));
    }

    [Fact]
    public void ThePreviewShowsQuestionMarksOnlyWhereTemperamentsDiffer()
    {
        var s = new Start(); s.Known["waitress"] = new List<string> { "routine" };
        var g = New(s);
        var pay = g.Preview("pay", "waitress");
        Assert.Equal(("?", 70000L, 90000L), (pay.Outcome, pay.Lines[0].Usd, pay.WorstUsd));
        Assert.Equal("refused", g.Preview("ask", "waitress").Outcome);
        Assert.Equal("?", pay.Reports);
    }

    [Fact]
    public void TalkerTestReadsPeopleRules()
    {
        var r = Repo.Inputs.Rules;
        Assert.False(r.IsTalker(new[] { 1, 0, 0, 1 })); // helped Inez
        Assert.True(r.IsTalker(new[] { 1, 1, 1, 0 }));  // pressured Inez
        Assert.True(r.IsTalker(new[] { 0, 0, 1, 0 }));  // a Fearful start
        Assert.False(r.IsTalker(new[] { 2, 1, 1, 0 })); // equal is not a Talker
    }

    [Fact]
    public void UnknownIsRejected()
    {
        var i = Repo.Inputs;
        var bad = i.Rulebook.Replace("{ \"k\": \"tier\", \"is\": \"simulated\" }", "{ \"k\": \"mood\", \"is\": \"simulated\" }");
        Assert.NotEqual(i.Rulebook, bad);
        Assert.Throws<FormatException>(() => new Rules(bad, i.PeopleRules));
        var g = New();
        Assert.False(g.Issue(new Order { Tick = 0, Op = "scout", Do = "dance" }).ok);
        Assert.Throws<FormatException>(() => Order.Parse(JNode.Parse("{\"tick\":0,\"op\":\"scout\",\"do\":\"move\",\"speed\":9}")));
        Assert.Throws<FormatException>(() => JNode.Parse("{\"v\": 1.5}")["v"].Long()); // integer state only
    }
}

public class GeometryTests
{
    [Fact]
    public void IntegerSquareRootIsExact()
    {
        foreach (long n in new long[] { 0, 1, 2, 3, 4, 15, 16, 17, 99, 100, 101, 1L << 40, (1L << 40) - 1, 4_000_000_000_000_000L })
        {
            long r = G.Isqrt(n);
            Assert.True(r * r <= n && (r + 1) * (r + 1) > n, $"isqrt({n}) = {r}");
        }
    }

    [Fact]
    public void A_slab_above_eye_height_does_not_block_and_a_wall_does()
    {
        // N13 (§05 levels): sightlines run in 3D. A deck slab from 9.43 m does not block an eye-level line; a wall does.
        var square = new[] { new Pt(-100, -100), new Pt(100, -100), new Pt(100, 100), new Pt(-100, 100) };
        var slab = new Occluder("deck", 943, 40, square);
        var wall = new Occluder("wall", 0, 300, square);
        var a = new Pt(-500, 0); var b = new Pt(500, 0);
        Assert.False(slab.Blocks(a, b, 160));
        Assert.True(wall.Blocks(a, b, 160));
        Assert.True(slab.Blocks(a, b, 950)); // seen from the deck, the slab blocks the line along it
    }

    [Fact]
    public void ConesAreDirectional()
    {
        var o = new Pt(0, 0); var f = new Pt(100, 0);
        Assert.True(G.InCone(o, f, 1800, 900, 300, new Pt(1000, 200)));
        Assert.False(G.InCone(o, f, 1800, 900, 300, new Pt(-1000, 0)));
        Assert.True(G.InCone(o, f, 1800, 900, 300, new Pt(-200, 0)));  // within `near`
        Assert.False(G.InCone(o, f, 1800, 900, 300, new Pt(1900, 0))); // beyond reach
    }
}

public class StreamTests
{
    [Fact]
    public void TheNeedStreamIsTheTypeScriptOne()
    {
        // contracts/rules/rulebook.test.ts golden values: stream('p-cafe-11/need/1')() and replaceNeed for missions 1–3
        Assert.Equal(1572639180u, new Streams.Mulberry("p-cafe-11/need/1").Next());
        var items = new List<(string, int)> { ("cash", 2000), ("housing", 1000), ("papers", 1000), ("care", 500), ("advancement", 1000), ("secret", 500) };
        Assert.Equal(new[] { "housing", "care", "housing" }, new[] { 1, 2, 3 }.Select(m => Streams.Draw($"p-cafe-11/need/{m}", items)).ToArray());
    }
}
