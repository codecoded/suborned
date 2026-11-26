using System.Linq;
using Suborned.Sim;
using Xunit;

public sealed class DistrictTests
{
    static Game Open() => Engine.New(Repo.Inputs, new Start { District = true, Seed = 9 });
    static void Order(Game g, string verb, Pt? point = null, string who = "", string to = "")
    {
        var a = g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = verb, Point = point, Target = who, To = to });
        Assert.True(a.ok, verb + " " + a.code + " " + a.why);
    }

    static void Arrive(Game g)
    {
        var scout = g.Ops[0];
        for (int k = 0; k < 24000 && !g.Over && (scout.Walk != null || scout.Busy != null); k++)
            g.Step();
        Assert.False(g.Over, g.FailCode + " " + g.FailText);
        Assert.Null(scout.Walk);
        Assert.Null(scout.Busy);
    }

    [Fact]
    public void MissedWindowStillAllowsAnHonestAndAFalseIntroductionWithReplay()
    {
        var g = Open();
        var d = g.District!;
        Assert.True(d.Map.People.Length > 100);
        // Leave the cell clock running past the opportunity. Nobody is automatically incapacitated.
        g.RunTo(Game.T(g.S.Over + 120));
        Assert.False(g.Over, g.FailText);
        Assert.False(g.HandoverOpen);
        long start = g.Cash;
        int trail = g.Trail;
        for (int n = 0; n < 2; n++)
        {
            var j = d.Jobs[n];
            var names = new[]
            {
                j.A,
                j.B,
                j.C
            };
            Assert.Contains(d.Map.People[j.A].Ties, t => t.person == j.B);
            Assert.Contains(d.Map.People[j.B].Ties, t => t.person == j.C);
            for (int k = 0; k < 3; k++)
            {
                int who = names[k];
                string target = d.Map.People[who].Id;
                Order(g, "district-visit", who: target);
                Arrive(g);
                Assert.True(G.Within(g.Ops[0].Pos, d.Pose(who, g.Tick).at, 650));
                Order(g, n == 1 && k == 1 ? "district-bluff" : "district-help", who: target, to: j.Key);
            }

            Assert.NotEqual(j, d.Jobs[n]);
            Assert.NotNull(d.Memory[j.C]);
        }

        Assert.Equal(2, d.Completed);
        Assert.Equal(start + 105, g.Cash);
        Assert.True(g.Trail > trail);
        var s = Script.Parse(Script.Write(g, Repo.Inputs.Hash));
        var replay = Engine.Play(Repo.Inputs, s.Start, s.Orders, s.Until);
        Assert.Equal(g.ClosingHash(), replay.ClosingHash());
        Assert.Equal(d.News, replay.District!.News);
    }

    [Fact]
    public void DriveAnUnmarkedRoadRedirectStepOutAndBoardAgain()
    {
        var g = Open();
        var scout = g.Ops[0];
        var d = g.District!;
        var at = g.Walk.NearestFree(g.GetawaySeat);
        Assert.NotNull(at);
        Order(g, "move", at);
        Arrive(g);
        Order(g, "board", who: "car");
        Arrive(g);
        var road = d.Map.Streets.First(s => s.Id == "road/market-street").Path;
        Order(g, "drive-car", road.At(road.Length / 3));
        g.Advance(35);
        var mid = g.GetawayCarPos();
        var want = road.At(road.Length * 2 / 3);
        Order(g, "drive-car", want);
        Assert.Equal(mid, g.GetawayCarPos());
        Arrive(g);
        Assert.True(G.Within(want, g.GetawayCarPos(), 500));
        Assert.True(scout.InCar);
        Order(g, "move", g.DistrictExitDoor(scout));
        Arrive(g);
        Assert.False(scout.InCar);
        Assert.False(g.S.Grid.InGrid(scout.Pos));
        Order(g, "board", who: "car");
        Arrive(g);
        Assert.True(scout.InCar);
        var water = new Pt(-90000, 90000);
        var old = g.GetawayCarPos();
        Assert.False(g.Do(new Order { Tick = g.Tick, Op = "scout", Do = "drive-car", Point = water }));
        Assert.Equal(old, g.GetawayCarPos());
    }

    [Fact]
    public void ContactsCanIntroduceAnUnconnectedCellAndMeetingsResumeInPlace()
    {
        var g = Open();
        var d = g.District!;
        var j = d.Jobs[0];
        var a = d.Map.People[j.A];
        var b = d.Map.People[j.B];
        Order(g, "district-visit", who: a.Id);
        Arrive(g);
        var at = d.Pose(j.A, g.Tick).at;
        g.Advance(900);
        Assert.Equal(at, d.Pose(j.A, g.Tick).at);
        Assert.False(g.Do(new Order { Tick = g.Tick, Op = "scout", Do = "district-refer", Target = a.Id, To = b.Id }));
        Order(g, "district-back", who: a.Id);
        Assert.Equal(at, d.Pose(j.A, g.Tick).at);
        Order(g, "district-refer", who: a.Id, to: b.Id);
        Assert.True(d.Trust[j.B] > 0);
        Assert.False(g.Do(new Order { Tick = g.Tick, Op = "scout", Do = "district-refer", Target = a.Id, To = b.Id }));
    }

    [Fact]
    public void ContinuingAnOldRunRetainsItsOriginalHistory()
    {
        var g = Engine.New(Repo.Inputs, new Start { Seed = 9 });
        g.RunTo(Game.T(g.S.Over + 120));
        Assert.True(g.Over);
        var end = g.Result;
        int tick = g.Tick;
        long cash = g.Cash;
        Assert.True(g.Do(new Order { Op = "cell", Do = "open-district", Tick = tick }));
        Assert.Equal(end, g.District!.Handover);
        Assert.Equal(cash, g.Cash);
        g.Advance(31);
        Assert.Equal(tick + 31, g.Tick);
        var s = Script.Parse(Script.Write(g, Repo.Inputs.Hash));
        var copy = Engine.Play(Repo.Inputs, s.Start, s.Orders, s.Until);
        Assert.Equal(g.ClosingHash(), copy.ClosingHash());
        Assert.NotNull(copy.District);
    }

    [Fact]
    public void CompletingTheOriginalPrimaryRouteKeepsTheCellPlayable()
    {
        var script = Script.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Engine.FindRoot(System.AppContext.BaseDirectory), "contracts/missions/handover/routes/route-a.json")));
        script.Start.District = true;
        var g = Engine.Play(Repo.Inputs, script.Start, script.Orders, script.Until);
        Assert.False(g.Over, g.FailCode + " " + g.FailText);
        Assert.Equal(Outcome.Success, g.District!.Handover);
        Assert.Contains(g.Ops, o => !o.Down && !o.LeftMap);
        Assert.Equal(3, g.District.Jobs.Length);
        int tick = g.Tick;
        g.Advance(150);
        Assert.Equal(tick + 150, g.Tick);
    }

    [Fact]
    public void TheRivalsArriveWhileTheCarIsParkedAcrossTown()
    {
        var g = Engine.New(Repo.Inputs, new Start { District = true });
        Order(g, "move", g.Walk.NearestFree(g.GetawaySeat));
        Arrive(g);
        Order(g, "board", who: "car");
        Arrive(g);
        Order(g, "drive-car", new Pt(18000,-9667));
        Arrive(g);
        g.RunTo(Game.T(g.S.Over + 120));
        Assert.False(g.Over,g.FailText);
        Assert.False(g.HandoverOpen);
        Assert.True(g.Ops[0].InCar);
    }

    [Fact]
    public void OnlyTheLastIncapacitatedOperativeEndsTheDistrict()
    {
        var g = Open();
        foreach (var o in g.Ops.Take(3))
            o.Gun.Down = true;
        g.Step();
        Assert.False(g.Over);
        g.Ops.Last().Gun.Down = true;
        g.Step();
        Assert.True(g.Over);
        Assert.Equal("cell-down", g.FailCode);
    }
}
