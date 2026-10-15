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

    [Fact]
    public void TheFightOrdersKeepTheOrderFormat()
    {
        // new keys are written only when set: an older order reads and writes as before
        var walk = Order.Parse(JNode.Parse("{\"tick\":3,\"op\":\"scout\",\"do\":\"move\",\"to\":\"d:Courtyard\"}"));
        Assert.Equal("{\"tick\":3,\"op\":\"scout\",\"do\":\"move\",\"to\":\"d:Courtyard\"}", walk.ToString().Replace(" ", ""));
        var forced = Order.Parse(JNode.Parse("{\"tick\":3,\"op\":\"enforcer\",\"do\":\"fire\",\"target\":\"waitress\",\"force\":true}"));
        Assert.True(forced.Force); Assert.Contains("\"force\"", forced.ToString());
        Assert.Equal("free", Order.Parse(JNode.Parse("{\"tick\":3,\"op\":\"scout\",\"do\":\"stance\",\"stance\":\"free\"}")).Stance);
        Assert.Throws<FormatException>(() => Order.Parse(JNode.Parse("{\"tick\":3,\"op\":\"scout\",\"do\":\"move\",\"to\":\"d:Courtyard\",\"force\":true}")));
        Assert.Throws<FormatException>(() => Order.Parse(JNode.Parse("{\"tick\":3,\"op\":\"scout\",\"do\":\"fire\",\"target\":\"guard\",\"stance\":\"free\"}")));
        var g = New();
        Assert.False(g.Issue(new Order { Tick = 0, Op = "scout", Do = "stance", Stance = "wild" }).ok); // unknown = reject
        Assert.False(g.Issue(new Order { Tick = 0, Op = "cell", Do = "panic" }).ok);                     // no fight on
        Assert.False(g.Issue(new Order { Tick = 0, Op = "enforcer", Do = "fire", Target = "plaza" }).ok);
    }

    [Fact]
    public void EveryRefusalHasAStableCodeAndTheHashReadsTheCodeNotTheWords()
    {
        var bad = new List<string>(); int refused = 0;
        foreach (var sc in Scenarios.All)
        {
            var g = Scenarios.Run(Repo.Inputs, sc).G;
            foreach (var (o, ok, code, why) in g.Orders)
            {
                if (ok) continue; refused++;
                if (!Answer.Codes.Contains(code) || code == "engine-error") bad.Add($"{sc.Id}: {o} → {code} ({why})");
            }
        }
        Assert.True(refused > 10, $"the scenarios refuse orders ({refused})");
        Assert.Empty(bad);
        var a = New(); var b = New();
        var x = a.Issue(new Order { Tick = 0, Op = "scout", Do = "watch", Target = "waitress" });
        Assert.Equal(("too-far", false), (x.code, x.ok)); Assert.True(x.m > 0); // the number the line needs, as a field
        var (ok2, code2, why2) = b.Issue(new Order { Tick = 0, Op = "scout", Do = "watch", Target = "waitress" });
        Assert.Equal(x.code, code2);
        Assert.Equal(Hash.State(a), Hash.State(b));
        Assert.Equal(Answer.Codes.Length, Answer.Codes.Distinct().Count());
    }

    [Fact]
    public void TheSummonsIsRefusedForEachReasonAtNoCost()
    {
        Start Inez() { var s = new Start(); s.Turned.Add("waitress"); return s; }
        (bool ok, string code, int trail) Call(Game g) { var a = g.Issue(new Order { Tick = g.Tick, Op = "fixer", Do = "call", Target = "waitress" }); return (a.ok, a.code, g.Trail); }
        var alert = New(Inez()); alert.Alert = 2;                                     // a search: he won't leave (summon.alertMax 1)
        Assert.Equal((false, "summon-alert", 0), Call(alert));
        var saw = New(Inez()); saw.GuardP.Memory.Add(new Mem { Act = "saw-gunfire", By = "enforcer", Place = "d:Office entrance" });
        Assert.Equal((false, "summon-saw-violence", 0), Call(saw));                 // he saw what you did
        var down = New(Inez()); down.GuardDown = true;
        Assert.Equal((false, "summon-busy", 0), Call(down));
        var i = Repo.Inputs; // a city where he lives too far: summon.walkMax 30 s against his 46 s walk
        var far = Engine.FromTexts(i.Rulebook.Replace("\"summon-walk-max-s\": { \"v\": 90,", "\"summon-walk-max-s\": { \"v\": 30,"), i.PeopleRules, i.Site, i.People);
        var g = Engine.New(far, Inez()); var a = g.Issue(new Order { Tick = 0, Op = "fixer", Do = "call", Target = "waitress" });
        Assert.Equal("summon-too-far", a.code); Assert.True(a.tick > 300, $"the walk rides as a field ({a.tick} ticks)");
        var ok = New(Inez()); Assert.Equal((true, "", 1), Call(ok));                  // the call: +1 T (cell.call.trail)
    }

    [Fact]
    public void TheCallQueryAndIssueAgree()
    {
        // every scenario's call orders, and a call on every role at every 10 s of a few runs: the query before the click is Issue's answer
        int checkedCalls = 0;
        foreach (var id in new[] { "S10", "N39", "N39c", "N39d", "S21", "N39b", "S07" })
        {
            var sc = Scenarios.Find(id)!;
            var played = Scenarios.Run(Repo.Inputs, sc).G;
            var g = Engine.New(Repo.Inputs, sc.Start); int k = 0;
            while (g.Tick <= played.Tick && !g.Over)
            {
                if (g.Tick % 600 == 0)
                    foreach (var role in new[] { "waitress", "driver", "guard", "councillor" })
                    {
                        var copy = g.Tick == 0 ? Engine.New(Repo.Inputs, sc.Start) : Engine.Play(Repo.Inputs, sc.Start, played.Orders.Take(k).Select(x => x.order).ToList(), g.Tick);
                        var q = copy.CallQuery(role); ulong before = Hash.State(copy);
                        Assert.Equal(before, Hash.State(copy)); // read-only
                        var a = copy.Issue(new Order { Tick = copy.Tick, Op = "fixer", Do = "call", Target = role });
                        Assert.True(q.ok == a.ok && q.code == a.code && q.tick == a.tick, $"{id} {Report.Clock(g.Tick)} call {role}: query {q.code} vs issue {a.code}");
                        checkedCalls++;
                    }
                while (k < played.Orders.Count && played.Orders[k].order.Tick == g.Tick) { var o = played.Orders[k++].order; if (o.Do == "call") { var q = g.CallQuery(o.Target); var a = g.Issue(o); Assert.Equal((q.ok, q.code), (a.ok, a.code)); checkedCalls++; } else g.Issue(o); }
                g.Step();
            }
        }
        Assert.True(checkedCalls > 40, $"calls checked ({checkedCalls})");
    }

    [Fact]
    public void ArmsDecideWhoCarriesASidearm()
    {
        var none = new Start { Arms = 0 };
        var g = New(none);
        Assert.Equal("carbine", g.Enforcer.Gun.W!.Id);                        // the cell's, uses no Arms
        Assert.Null(g.OpOf(OpId.Fixer).Gun.W);
        Assert.False(g.Issue(new Order { Tick = 0, Op = "fixer", Do = "draw" }).ok);
        var all = New();                                                        // the Handover issues three (site arms)
        Assert.All(all.Ops, o => Assert.NotNull(o.Gun.W));
        Assert.Equal(new[] { 4, 4, 4, 6 }, all.Ops.Select(o => o.Gun.Health).ToArray());
    }

    [Fact]
    public void CoverMatchesPulsesVectors()
    {
        // briefs/011/pulse-fight-ai.md §8.1: a point at (0, 0) facing (0, 1000), arc 50° (cos 642)
        var c = new CoverPoint { Id = "cv-001", At = new Pt(0, 0), Faces = new Pt(0, 1000), Cos = 642, Q = 700 };
        Assert.True(CoverRules.Protects(c, new Pt(0, 1500), 0, 500));
        Assert.True(CoverRules.Protects(c, new Pt(1000, 1500), 0, 500));
        Assert.False(CoverRules.Protects(c, new Pt(1800, 1500), 0, 500));
        Assert.False(CoverRules.Protects(c, new Pt(0, -1500), 0, 500));
        Assert.False(CoverRules.Protects(c, new Pt(0, 499), 0, 500));
        Assert.True(CoverRules.Protects(c, new Pt(0, 500), 0, 500));
        var car = new CoverPoint { Id = "cv-002", At = new Pt(0, 0), Faces = new Pt(0, 1000), Cos = 642, Q = 700, From = 100, To = 200 };
        Assert.Equal(new[] { false, true, true, false }, new[] { 99, 100, 199, 200 }.Select(t => CoverRules.Protects(car, new Pt(0, 1500), t, 500)).ToArray());
        // §8.2: the modifier
        int M(CoverPoint p, Pt body, Pt shooter) => CoverRules.Modifier(new[] { p }, body, shooter, 0, 70, 500, 600, 350, 500, 750);
        var low = new CoverPoint { Id = "cv-003", At = new Pt(0, 0), Faces = new Pt(0, 1000), Cos = 642, Q = 400 };
        Assert.Equal(500, M(c, new Pt(0, 0), new Pt(0, 2000)));
        Assert.Equal(750, M(low, new Pt(0, 0), new Pt(0, 2000)));
        Assert.Equal(1000, M(c, new Pt(0, 0), new Pt(0, -2000)));
        Assert.Equal(1000, M(c, new Pt(120, 0), new Pt(0, 2000)));
        Assert.Equal(1000, M(c, new Pt(0, 0), new Pt(0, 300)));
    }

    [Fact]
    public void AccuracyMatchesTheTypeScriptReference()
    {
        var r = New().R;
        // contracts/rules/rulebook.test.ts "bands and modifiers"
        Assert.Equal(750, r.Accuracy(r.Pistol, 1200, false, false, false, false));
        Assert.Equal(750, r.Accuracy(r.Pistol, 400, false, false, false, true));
        Assert.Equal(375, r.Accuracy(r.Pistol, 1200, true, false, false, false));
        Assert.Equal(187, r.Accuracy(r.Pistol, 1200, true, true, false, false));
        Assert.Equal(400, r.Accuracy(r.Pistol, 2000, false, false, false, false));
        Assert.Equal(0, r.Accuracy(r.Pistol, 2600, false, false, false, false));
        Assert.Equal(750, r.Accuracy(r.Carbine, 1800, false, false, false, false));
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
