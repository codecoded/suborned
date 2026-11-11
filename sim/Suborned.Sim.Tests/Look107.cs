// Brief 107 (engine 0.54.0; Sky 2026-11-11: "Yes to all three"): the weather and the light in the start state, a look only (GameLook.cs).
// The look moves nothing in play: a game with a look plays exactly the game without it, line for line but the look's own; a start without it adds no
// bytes and logs nothing (every run before 0.54.0); the draw is the rulebook's on its own stream (twin: contracts/rules/rulebook107.test.ts);
// the briefing's orders change it at tick 0 only, and a run with them replays exactly.
using System;
using System.IO;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class Look107Tests
{
    static readonly string Routes = System.IO.Path.Combine(Engine.FindRoot(AppContext.BaseDirectory), "contracts", "missions", "handover", "routes");
    static Script Route(string name, int seed) { var s = Script.Parse(File.ReadAllText(System.IO.Path.Combine(Routes, name + ".json"))); s.Expect = null; s.Start.Seed = seed; return s; }
    static readonly string[] LookCodes = { "start.weather", "start.light" };
    static string Story(Game g) => string.Join("\n", g.Log.Where(l => !LookCodes.Contains(l.Code)).Select(l => $"{l.Tick} {l.Code} {l.Text}"))
        + $"\n{g.Result} {g.FailCode} {g.Tick} {g.Cash} {g.Trail} {g.Alert} {g.Standing} " + string.Join(";", g.Ops.Select(o => $"{o.Pos.X},{o.Pos.Y}"))
        + "\n" + string.Join("\n", g.Orders.Select(o => $"{o.order} {o.ok} {o.code}"));

    [Fact]
    public void TheDrawIsTheRulebooksTwinAndSeedZeroIsClearByDay()
    {
        var L = Repo.Inputs.Rules.Look;
        Assert.Equal("clear", L.WeatherOf(0)); Assert.Equal("day", L.LightOf(0));
        string[] twin = { "overcast/night", "clear/day", "rain/night", "mist/night", "clear/day", "overcast/day", "rain/day", "overcast/night" }; // rulebook107.test.ts TWIN
        Assert.Equal(twin, Enumerable.Range(1, 8).Select(s => L.WeatherOf(s) + "/" + L.LightOf(s)).ToArray());
    }

    [Fact]
    public void AStartWithoutTheLookIsClearByDayAndAddsNoBytes()
    {
        var s = new Start { Seed = 42 }; Assert.DoesNotContain("weather", s.Canonical()); Assert.DoesNotContain("light", s.Canonical());
        var g = Engine.New(Repo.Inputs, s);
        Assert.Equal("clear", g.Weather); Assert.Equal("day", g.Light); Assert.False(g.Night);
        Assert.DoesNotContain(g.Log, l => LookCodes.Contains(l.Code));
    }

    [Fact]
    public void RollLookDrawsFromTheSeedKeepsWhatIsSetAndRoundTrips()
    {
        var r = Repo.Inputs.Rules;
        var s = new Start { Seed = 3 }.RollLook(r); Assert.Equal(("rain", "night"), (s.Weather, s.Light));
        var kept = new Start { Seed = 3, Weather = "mist" }.RollLook(r); Assert.Equal(("mist", "night"), (kept.Weather, kept.Light));
        var back = Start.Parse(JNode.Parse(s.Canonical(), "start")); Assert.Equal(s.Canonical(), back.Canonical()); Assert.Equal(("rain", "night"), (back.Weather, back.Light));
        Assert.Throws<FormatException>(() => Start.Parse(JNode.Parse("{\"weather\":\"snow\"}", "start")));
        Assert.Throws<FormatException>(() => Start.Parse(JNode.Parse("{\"light\":\"dusk\"}", "start")));
        var g = Engine.New(Repo.Inputs, s);
        Assert.Equal(("rain", "night"), (g.Weather, g.Light)); Assert.True(g.Night);
        Assert.Equal(new[] { "start.weather", "start.light" }, g.Log.Where(l => LookCodes.Contains(l.Code)).Select(l => l.Code).ToArray());
        Assert.All(g.Log.Where(l => LookCodes.Contains(l.Code)), l => Assert.Equal(0, l.Tick));
    }

    [Theory]
    [InlineData("route-a", 7)] [InlineData("route-b", 3)] [InlineData("loud-fight", 4)] [InlineData("route-g", 0)]
    public void TheLookChangesNothingInPlay(string route, int seed)
    {
        var plain = Route(route, seed); var looked = Route(route, seed); looked.Start.RollLook(Repo.Inputs.Rules);
        var a = Engine.Play(Repo.Inputs, plain.Start, plain.Orders, plain.Until);
        var b = Engine.Play(Repo.Inputs, looked.Start, looked.Orders, looked.Until);
        Assert.Equal(Story(a), Story(b));
        Assert.Equal(a.Checkpoints.Select(c => c.tick), b.Checkpoints.Select(c => c.tick));
    }

    [Fact]
    public void TheBriefingSetsTheLookAtTickZeroOnlyAndTheRunReplays()
    {
        var g = Engine.New(Repo.Inputs, new Start { Seed = 9 }.RollLook(Repo.Inputs.Rules));
        Assert.True(g.Issue(new Order { Tick = 0, Op = "cell", Do = "weather", To = "rain" }).ok);
        Assert.True(g.Issue(new Order { Tick = 0, Op = "cell", Do = "light", To = "night" }).ok);
        Assert.Equal(("rain", "night"), (g.Weather, g.Light));
        var unknown = g.Issue(new Order { Tick = 0, Op = "cell", Do = "weather", To = "snow" }); Assert.False(unknown.ok); Assert.Equal("look-unknown", unknown.code);
        g.RunTo(Game.T(5));
        var late = g.Issue(new Order { Tick = g.Tick, Op = "cell", Do = "light", To = "day" }); Assert.False(late.ok); Assert.Equal("look-fixed", late.code);
        Assert.Equal(("rain", "night"), (g.Weather, g.Light));
        Assert.Equal(2, g.Log.Count(l => l.Code == "start.weather")); Assert.Equal(2, g.Log.Count(l => l.Code == "start.light"));
        g.RunTo(Game.T(60)); g.Finish();
        var sc = Script.Parse(Script.Write(g, Repo.Inputs.Hash));
        var again = Engine.Replay(Repo.Inputs, g);
        Assert.Equal(g.Checkpoints, again.Checkpoints); Assert.Equal(("rain", "night"), (again.Weather, again.Light));
        Assert.Equal("", sc.Verify(again, Repo.Inputs.Hash));
    }
}
