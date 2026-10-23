// Engine 0.25.0, brief 045 F3 (Redline, late lag): Game.ClosingHash() is the closing checkpoint Engine.Play(start, orders, Tick) ends with,
// taken from the live game without a replay and without changing it. The host's Save() called a full replay for it whenever a public fight
// was open (150–270 ms headless late in the mission, 0.3–0.7 s in the player).
//    EveryScenario / EveryRangeRun / EveryRouteScript   a live replay calls ClosingHash at every order tick and every 5 s (as Save does);
//        its value at the end is Finish's checkpoint, mid-fight (the first and last order tick with a fight open) it is Engine.Play's to
//        that tick, and the run's checkpoints are still the played run's (the calls changed nothing)
//    LateFightAtEveryOrderTick   loud-fight and Sky's session 1 (briefs/045/runs): at every save with a fight open (order ticks and every 5 s), Engine.Play's hash
//    CostsUnderOneMillisecond    at Sky's session 1's last save with a fight open (tick ~4194) the call costs < 1 ms (a full replay there
//                                costs ~160 ms headless); 0.43 ms median measured, since Hash.State reads each order's text as issued
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Suborned.Sim;
using Xunit;
using Path = System.IO.Path;

public class ClosingHashTests
{
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);
    const string Session1 = "briefs/045/runs/sky-0.2.58-session1.script.json", LoudFight = "contracts/missions/handover/routes/loud-fight.json";

    /// <summary>Play `orders` live as Engine.Play does, calling ClosingHash after each order tick and every 5 s; returns the live game
    /// (not finished), the hash at the end and the order ticks at which a fight was open with the hash taken there.</summary>
    static (Game g, ulong end, List<(int tick, ulong hash)> fight) Live(Engine.Inputs i, Start start, IReadOnlyList<Order> orders, int until)
    {
        var g = Engine.New(i, start); var fight = new List<(int, ulong)>();
        if (until <= 0) until = Game.T(i.SiteData.Over);
        var list = orders.Select((o, k) => (o, k)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).Select(x => x.o).ToList();
        int saved = 0;
        void Save() { var h = g.ClosingHash(); saved = g.Tick; if (g.OpenFight != null && !g.Over && (fight.Count == 0 || fight[^1].Item1 != g.Tick)) fight.Add((g.Tick, h)); }
        for (int k = 0; k < list.Count; k++)
        {
            var o = list[k];
            if (o.Tick > until) break;
            while (g.Tick < o.Tick && !g.Over) { g.Step(); if (g.Tick - saved >= Game.T(5)) Save(); }
            if (g.Over && g.Tick != o.Tick) break;
            if (g.Tick == o.Tick) g.Issue(o);
            if (k + 1 == list.Count || list[k + 1].Tick != o.Tick) Save();
        }
        while (g.Tick < until && !g.Over) { g.Step(); if (g.Tick - saved >= Game.T(5)) Save(); }
        return (g, g.ClosingHash(), fight);
    }

    static void Check(Engine.Inputs i, Start start, IReadOnlyList<Order> orders, int until, Game played, string what, bool everyFight = false)
    {
        var (g, end, fight) = Live(i, start, orders, until);
        Assert.Equal(played.Tick, g.Tick);
        g.Finish();
        Assert.True(end == g.Checkpoints[^1].hash, $"{what}: ClosingHash at {g.Tick} is Finish's checkpoint");
        Assert.True(played.Checkpoints.SequenceEqual(g.Checkpoints), $"{what}: the calls changed nothing (checkpoints as played)");
        var at = everyFight ? fight : fight.Count <= 2 ? fight : new List<(int, ulong)> { fight[0], fight[^1] };
        foreach (var (t, h) in at)
            Assert.True(h == Engine.Play(i, start, orders, t).Checkpoints[^1].hash, $"{what}: mid-fight at tick {t}, ClosingHash is Engine.Play's closing checkpoint");
    }

    static List<Order> OrdersOf(Game g) { var l = new List<Order>(); foreach (var (o, _, _, _) in g.Orders) l.Add(o); return l; }

    public static IEnumerable<object[]> Ids => Scenarios.All.Select(s => new object[] { s.Id });
    public static IEnumerable<object[]> RangeIds => Scenarios.Range.Select(s => new object[] { s.Id });
    public static IEnumerable<object[]> Routes => Directory.GetFiles(Path.Combine(Root, "contracts/missions/handover/routes"), "*.json").Where(f => Path.GetFileName(f) != "routes.json").OrderBy(f => f, StringComparer.Ordinal)
        .Append(Path.Combine(Root, "briefs/011/motive-fights/loud-carry.script.json")).Select(f => new object[] { Path.GetRelativePath(Root, f) });

    [Theory, MemberData(nameof(Ids))]
    public void EveryScenario(string id)
    {
        var played = Scenarios.Run(Repo.Inputs, Scenarios.Find(id)!).G;
        Check(Repo.Inputs, played.Start, OrdersOf(played), played.Tick, played, id);
    }

    [Theory, MemberData(nameof(RangeIds))]
    public void EveryRangeRun(string id)
    {
        var s = Scenarios.FindRange(id)!; var i = Engine.LoadRange(Root, s.Layout);
        var played = Scenarios.Run(i, s).G;
        Check(i, played.Start, OrdersOf(played), played.Tick, played, id);
    }

    [Theory, MemberData(nameof(Routes))]
    public void EveryRouteScript(string file)
    {
        var sc = Script.Parse(File.ReadAllText(Path.Combine(Root, file)));
        var played = Engine.Play(Repo.Inputs, sc.Start, sc.Orders, sc.Until);
        Check(Repo.Inputs, sc.Start, sc.Orders, sc.Until, played, file);
    }

    [Theory, InlineData(LoudFight), InlineData(Session1)]
    public void LateFightAtEveryOrderTick(string file)
    {
        var sc = Script.Parse(File.ReadAllText(Path.Combine(Root, file)));
        var played = Engine.Play(Repo.Inputs, sc.Start, sc.Orders, sc.Until);
        var (_, _, fight) = Live(Repo.Inputs, sc.Start, sc.Orders, sc.Until);
        Assert.True(fight.Count >= (file == LoudFight ? 20 : 1), $"{file}: a fight is open at {fight.Count} saves"); // session 1 was recorded on 0.2.58's engine: today it meets fewer
        Check(Repo.Inputs, sc.Start, sc.Orders, sc.Until, played, file, everyFight: true);
    }

    [Fact]
    public void CostsUnderOneMillisecond()
    {
        var sc = Script.Parse(File.ReadAllText(Path.Combine(Root, Session1)));
        var (_, _, fight) = Live(Repo.Inputs, sc.Start, sc.Orders, sc.Until);
        Assert.True(fight.Count > 0 && fight[^1].tick >= Game.T(400), "Sky's session 1 has a fight open late (after 6:40)");
        int at = fight[^1].tick; // the run's last save with a fight open: the dear case (the very last tick is over, a lookup)
        var g = Engine.New(Repo.Inputs, sc.Start);
        foreach (var o in sc.Orders.Select((o, k) => (o, k)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).Select(x => x.o)) { if (o.Tick > at) break; g.RunTo(o.Tick); if (g.Tick == o.Tick) g.Issue(o); }
        g.RunTo(at);
        Assert.True(g.OpenFight != null && g.Tick == at, $"a fight is open at tick {at}");
        var ms = new List<double>(); var sw = new Stopwatch();
        for (int k = 0; k < 21; k++) { sw.Restart(); g.ClosingHash(); sw.Stop(); ms.Add(sw.Elapsed.TotalMilliseconds); }
        ms.Sort(); double median = ms[ms.Count / 2];
        Assert.True(median < 1.0, $"ClosingHash at tick {g.Tick} (fight open) costs {median:0.000} ms (median of 21), want < 1 ms");
    }
}
