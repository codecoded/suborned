using System;
using System.Linq;
using System.Text.Json.Nodes;
using Suborned.Sim;
using Xunit;

public class PublicActionsTests
{
    static Engine.Inputs Inputs(bool? idle = false)
    {
        var i = Repo.Inputs;
        var rules = JsonNode.Parse(i.Rulebook)!;
        if (idle.HasValue) rules["attention"]!["idle"] = idle.Value;
        else rules["attention"]!.AsObject().Remove("idle");
        rules["attention"]!.AsObject().Remove("run");
        return Engine.FromTexts(rules.ToJsonString(), i.PeopleRules, i.Site, i.People, i.Provisional, i.Takedown, i.Rounds);
    }

    static Pt AtCar(Game g, string id) => g.Walk.Nearest(g.CarAt(id)!.Value,
        p => g.CarBodyCm(id, p) is int d && d >= 0 && d <= g.R.CarReachCm, 1200)!.Value;

    [Theory]
    [InlineData("scout")] [InlineData("fixer")] [InlineData("tech")] [InlineData("enforcer")]
    public void RunningOperativeBoardsAndStopsAtCar(string name)
    {
        var g = Engine.New(Inputs(), new Start { Crowd = false });
        var op = g.OpOf(Op.Parse(name));
        op.Pos = AtCar(g, "car:getaway"); op.Running = true;
        var answer = g.Issue(new Order { Tick = g.Tick, Op = name, Do = "board", Target = "car" });
        Assert.True(answer.ok, answer.why);
        Assert.False(op.Running); Assert.Null(op.Walk);
        g.RunTo(g.Tick + Game.T(g.R.BoardS) + 1);
        Assert.True(op.InCar);
    }

    [Fact]
    public void RunningDoesNotHideRealTakeAndBoardRefusals()
    {
        var g = Engine.New(Inputs(), new Start { Crowd = false });
        var op = g.OpOf(OpId.Scout); op.Running = true;
        Assert.Equal("stand-at-getaway", g.Issue(new Order { Op = "scout", Do = "board", Target = "car" }).code);
        Assert.Equal("target-unknown", g.Issue(new Order { Op = "scout", Do = "take-car", Target = "car:absent" }).code);
        Assert.Equal("car-ours", g.Issue(new Order { Op = "scout", Do = "take-car", Target = "car:getaway" }).code);
    }

    [Theory]
    [InlineData(0)] [InlineData(-5)]
    public void PublicIdleStaysCalmAndKeepsArchivalSightings(int standing)
    {
        var g = Engine.New(Inputs(), new Start { Crowd = false, Standing = standing });
        var camera = g.S.Cameras.First(c => c.Id == "entrance");
        var spot = g.Walk.Nearest(camera.At, p => g.CameraWatching(camera.Id, p), 1500);
        Assert.NotNull(spot);
        g.Enforcer.Pos = spot!.Value;
        g.RunTo(Game.T(65));
        Assert.Equal(0, g.AttentionTicks("camera:entrance", "enforcer"));
        Assert.False(g.AttentionReported("camera:entrance", "enforcer"));
        Assert.Equal(0, g.Alert);
        Assert.Contains(g.Sightings, s => s.Watcher == "camera:entrance" && s.Op == "enforcer");
        Assert.DoesNotContain(g.Ledger, l => l.cause.StartsWith("loiter:") || l.cause.StartsWith("sighting:"));
    }

    [Fact]
    public void PickupCameraDistinguishesVisibleShotsFromHeardShots()
    {
        var g = Engine.New(Inputs(), new Start { Crowd = false });
        var camera = g.S.Cameras.First(c => c.Id == "pickup");
        var spot = g.Walk.Nearest(camera.At, p => g.CameraWatching(camera.Id, p)
            && !g.PatrolWatching(p) && !g.GuardWatching(p)
            && !g.S.Cameras.Any(c => c.Id != camera.Id && g.CameraWatching(c.Id, p)), 2000);
        Assert.NotNull(spot);
        g.Enforcer.Pos = spot!.Value;
        var shot = new Order { Op = "enforcer", Do = "fire", Target = "car:getaway", Force = true };
        var visible = g.GoingLoud(shot);
        Assert.Equal("", visible.Refused);
        Assert.Equal("seen", visible.ReportedBy);
        Assert.True(visible.PoliceInS > 0);
        g.LoopCam = camera.Id; g.LoopOn = true;
        Assert.False(g.CameraWatching(camera.Id, spot.Value));
        var looped = g.GoingLoud(shot);
        Assert.Equal("unseen", looped.ReportedBy);
        Assert.True(looped.PoliceInS > visible.PoliceInS);
        g.LoopOn = false; g.CutCams.Add(camera.Id);
        Assert.Equal("unseen", g.GoingLoud(shot).ReportedBy);
    }

    [Fact]
    public void LegacyIdleRuleRemainsAvailableForPinnedRuns()
    {
        Assert.True(Inputs(null).Rules.AttnIdle);
        Assert.True(Inputs(true).Rules.AttnIdle);
        Assert.False(Inputs().Rules.AttnRun);
    }
}
