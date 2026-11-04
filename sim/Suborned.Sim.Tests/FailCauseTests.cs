// The fail camera's culprit (Sky 2026-10-18; Suborned.Sim/FailCause.cs): every run that ends without Vale says what failed it, in a form a
// host can read, and saying so moves nothing (the cause is read-only: same checkpoints, same replay, the same cause when replayed).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class FailCauseTests
{
    static readonly Engine.Inputs I = Repo.Inputs;
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);
    static readonly string[] Fails = { "vale-dead", "vale-leaves", "rivals-reach-vale", "sealed", "enforcer-down", "entry-seen-guard", "entry-seen-camera", "entry-seen-patrol", "abduction-seen-camera", "driver-shot", "getaway-lost", "cell-down", "closed-in", "engine-error" }; // cell-down: engine 0.34.0 (Motive M067-K3 b)
    static readonly string[] Kinds = { "camera", "guard", "patrol", "officer", "robot", "rival", "person", "vale", "op", "car", "desk", "heard" };

    /// <summary>The cause's shape, whatever the ending: it matches the result, it is at the last tick, a single culprit is named and placed
    /// first, a seal names its origin, and its JSON is what the outcome writes.</summary>
    static void WellFormed(Game g, string what)
    {
        if (g.Result == Outcome.Success || g.Result == Outcome.None) { Assert.True(g.Cause == null, $"{what}: no cause without a failure"); return; }
        var c = g.Cause; Assert.True(c != null, $"{what}: {g.Result} {g.FailCode} carries a cause");
        Assert.Equal(g.Result == Outcome.Withdrawn ? "withdraw" : g.FailCode, c!.Fail);
        Assert.Contains(c.Fail, Fails.Append("withdraw"));
        Assert.Equal(g.Tick, c.Tick);
        Assert.NotEqual("unresolved", c.Why);
        foreach (var b in c.Culprits) { Assert.Contains(b.Kind, Kinds); Assert.False(string.IsNullOrEmpty(b.Id), $"{what}: a culprit with no id"); }
        foreach (var o in c.Ops) Assert.Contains(o, new[] { "scout", "fixer", "tech", "enforcer" });
        if (c.Single) { Assert.NotEmpty(c.Culprits); Assert.True(c.Culprits[0].Placed, $"{what}: the culprit to orbit is placed ({c.Culprits[0]})"); }
        if (c.Fail == "sealed") { Assert.False(c.Single); Assert.True(c.Origin != null && c.Origin.Tick < c.Tick && c.Origin.Report.Length > 0, $"{what}: the seal names what started it"); }
        var json = Report.Json(what, g, new List<(bool, string)>(), true);
        Assert.Contains("\"cause\":" + c.Json(), json);
    }

    public static IEnumerable<object[]> Ids => Scenarios.All.Select(s => new object[] { s.Id });

    [Theory, MemberData(nameof(Ids))]
    public void EveryScenarioEndsWithAWellFormedCauseThatReplays(string id)
    {
        var g = Scenarios.Run(I, Scenarios.Find(id)!).G;
        WellFormed(g, id);
        var replay = Engine.Replay(I, g);
        Assert.Equal(g.Cause?.Json(), replay.Cause?.Json());
    }

    /// <summary>The route scripts (Waymark's and Suture's fixtures): each plays to its record, and an ending without Vale carries its cause,
    /// the same when replayed.</summary>
    public static IEnumerable<object[]> Routes()
    {
        var dirs = new[] { "contracts/missions/handover/routes", "briefs/011/motive-fights" };
        foreach (var d in dirs)
        {
            var dir = System.IO.Path.Combine(Root, d); if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.GetFiles(dir, "*.json").OrderBy(x => x, StringComparer.Ordinal))
            {
                var text = File.ReadAllText(f);
                if (!text.Contains("suborned.sim.script/1")) continue;
                yield return new object[] { System.IO.Path.GetRelativePath(Root, f) };
            }
        }
    }

    [Theory, MemberData(nameof(Routes))]
    public void EveryRouteScriptEndsWithAWellFormedCauseAndItsRecord(string rel)
    {
        var script = Script.Parse(File.ReadAllText(System.IO.Path.Combine(Root, rel)));
        var g = Engine.Play(I, script.Start, script.Orders, script.Until);
        if (script.Expect != null && script.Expect.Engine == Engine.Version) Assert.Equal("", script.Verify(g, I.Hash)); // the cause moved no checkpoint
        WellFormed(g, rel);
        Assert.Equal(g.Cause?.Json(), Engine.Replay(I, g).Cause?.Json());
    }

    [Fact]
    public void EveryFailKindTheHandoverReachesIsCoveredByAScenario()
    {
        var seen = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var s in Scenarios.All) { var g = Scenarios.Run(I, s).G; if (g.Cause != null) seen.Add(g.Cause.Fail + "/" + g.Cause.Why); }
        // engine-error is never reached by play (EngineErrorNamesNoCulprit); brief 018's rival at the car is a fixture (ARivalAtTheCar…).
        // Brief 059 (engine 0.32.0; GDD v0.25.0 §07): reached-at-close is retired (at the close Vale's driver always drives him on: vale-leaves);
        // the tail (the wheelman at Vale's door) and driven-away (an unturned driver at 6:13) are new.
        // Engine 0.34.2 (Sky 2026-10-26: "entry seen by patrol" is no failure; rulebook cell.enter.seen.patrol report): no live run ends in
        // entry-seen-patrol on any site; the code stays for rulebooks pinned before 0.34.2 (APinnedRulebookStillFailsAnEntryThePatrolSees).
        // cell-down (Motive M067-K3 b) is new in 0.34.0.
        var want = new[] { "abduction-seen-camera/seen", "cell-down/shot", "driver-shot/shot", "enforcer-down/shot", "entry-seen-camera/seen", "entry-seen-guard/seen", "getaway-lost/wrecked",
            "rivals-reach-vale/downed-enforcer", "rivals-reach-vale/lookout", "sealed/sealed", // brief 096 (engine 0.41.0): the tail and the parked car's beat-to-pickup are retired; the lookout follows on foot (096-RIV-6) "vale-dead/killed", "vale-dead/shot", "vale-leaves/too-late",
            "vale-leaves/driven-away", "withdraw/car-left-without-vale", "withdraw/withdrew" };
        foreach (var w in want) Assert.True(seen.Contains(w), $"no scenario ends in {w} (seen: {string.Join(", ", seen)})");
        Assert.DoesNotContain("entry-seen-patrol/seen", seen); // engine 0.34.2: the patrol's sight of an Enter is a report, never a fail
    }

    [Fact]
    public void APinnedRulebookStillFailsAnEntryThePatrolSees()
    {
        // Engine 0.34.2: N120's Enter, seen by the patrol robot alone, is a report on today's rulebook; on one pinned before 0.34.2 (no
        // cell.enter.seen) it fails entry-seen-patrol as it did, and the fail camera names the patrol where it stood (FailCause "patrol").
        var at = I.Rulebook.IndexOf("\"seen\": {\"patrol\": \"report\"", StringComparison.Ordinal);
        Assert.True(at > 0, "the live rulebook has cell.enter.seen.patrol report");
        var pinned = Engine.FromTexts(I.Rulebook.Replace("\"seen\": {\"patrol\": \"report\"", "\"seen\": {\"patrol\": \"fail\""), I.PeopleRules, I.Site, I.People, I.Provisional.Length > 0 ? I.Provisional : null, I.Takedown.Length > 0 ? I.Takedown : null, I.Rounds.Length > 0 ? I.Rounds : null);
        var g = Scenarios.Run(pinned, Scenarios.Find("N120")!).G;
        Assert.Equal(Outcome.Failure, g.Result); Assert.Equal("entry-seen-patrol", g.FailCode);
        WellFormed(g, "N120 pinned");
        Assert.Equal("seen", g.Cause!.Why); Assert.Equal("patrol", g.Cause.Culprits[0].Kind); Assert.Equal("saw", g.Cause.Culprits[0].Role);
        var live = Scenarios.Run(I, Scenarios.Find("N120")!).G;
        Assert.NotEqual("entry-seen-patrol", live.FailCode); Assert.Equal("landed", live.EntryReport.state);
    }

    [Fact]
    public void ASuccessCarriesNoCauseAndItsDebriefIsAsItWas()
    {
        var g = Scenarios.Run(I, Scenarios.Find("S10")!).G;
        Assert.Equal(Outcome.Success, g.Result);
        Assert.Null(g.Cause);
        var w = new JWriter(); Debrief.Of(g).Write(w);
        Assert.DoesNotContain("\"cause\":{\"fail\"", w.ToString()); // the investigation's own "cause" (a string) is another field
        Assert.DoesNotContain("\"cause\":{\"fail\"", Report.Json("S10", g, new List<(bool, string)>(), true));
    }

    [Fact]
    public void AFailedDebriefCarriesTheCause()
    {
        var g = Scenarios.Run(I, Scenarios.Find("N119")!).G;
        var w = new JWriter(); Debrief.Of(g).Write(w);
        Assert.Contains("\"cause\":" + g.Cause!.Json(), w.ToString());
    }

    [Fact]
    public void EngineErrorNamesNoCulprit()
    {
        // an engine fault (Redline E1: Step never throws): a walk whose path is broken throws inside the tick
        var g = Engine.New(I); var op = g.OpOf(OpId.Scout);
        var broken = new Suborned.Sim.Path(new[] { op.Pos, op.Pos + new Pt(1000, 0) });
        typeof(Suborned.Sim.Path).GetField("cum", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(broken, Array.Empty<long>());
        op.Walk = broken; op.WalkMm = 0;
        g.Step();
        Assert.Equal((Outcome.Failure, "engine-error"), (g.Result, g.FailCode));
        var c = g.Cause!;
        Assert.Equal(("engine-error", "error", false, g.Tick), (c.Fail, c.Why, c.Single, c.Tick));
        Assert.Empty(c.Culprits); Assert.Empty(c.Ops); Assert.Null(c.Origin);
    }

    [Fact]
    public void ARivalAtTheCarWithNobodyBesideItIsTheCulprit()
    {
        // brief 018 §1 (GameFight.FightStep): Vale in the car, a rival within cell.atReach of it, no operative standing within reach. No route
        // reaches it on today's data (the rivals stop at cover or their weapon's reach), so the state is placed: a fixture, not a scenario.
        // The whole cell is in the car (boarded, Go refused while the car is held): nobody stands beside it.
        // Engine 0.34.0 (Motive M067-K3 a): the same state now comes in play too (the rivals walk to a held car nobody stands outside: K3-PRESS,
        // BAL-YARD-AT-WILL). Each rival stands on a free walk cell beside the car's body (since 0.33.1's car.footprint nobody stands on its
        // ground; K3's walk starts from the free cell nearest, so a body placed on the ground would be read there)
        var g = Engine.New(I); var car = g.S.At(g.S.GetawayCar);
        foreach (var o in g.Ops) o.InCar = true;
        g.Vale = ValeAt.Car; g.FightBranch = "safe";
        var at = g.Walk.NearestFree(car + new Pt(100, 0))!.Value; var at2 = g.Walk.NearestFree(at + new Pt(60, 0))!.Value;
        Assert.True(g.AtGetaway(at) && G.D2(at2, car) > G.D2(at, car), $"the fixture's points: {at} at the car, {at2} farther");
        g.Hostiles.Add(new Hostile { Id = "rival-2", At = at2, Home = at2, Present = true, Gun = new Gun("rival-2") { W = g.R.Pistol, Health = 2, Max = 2 } });
        g.Hostiles.Add(new Hostile { Id = "rival-1", At = at, Home = at, Present = true, Gun = new Gun("rival-1") { W = g.R.Pistol, Health = 2, Max = 2 } });
        g.Step();
        Assert.Equal((Outcome.Failure, "rivals-reach-vale"), (g.Result, g.FailCode));
        var c = g.Cause!;
        Assert.Equal(("reached-car", true), (c.Why, c.Single));
        Assert.Equal(new[] { "rival-1", "rival-2", "councillor" }, c.Culprits.Select(b => b.Id)); // nearest first, then Vale
        Assert.True(c.Culprits[0].Placed && c.Culprits[0].At == at && c.Culprits[0].Kind == "rival");
        Assert.True(c.Culprits[2].Placed && c.Culprits[2].At == car && c.Culprits[2].Kind == "vale", "Vale in the car, at the car");
        Assert.Empty(c.Ops);
    }
}
