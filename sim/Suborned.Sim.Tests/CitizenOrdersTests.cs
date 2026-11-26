using System;
using System.Linq;
using System.Reflection;
using Suborned.Sim;
using Xunit;

public sealed class CitizenOrdersTests
{
    static Game Open() => Engine.New(Repo.Inputs, new Start { Seed = 9, District = true });
    static void Go(Game g, int contact)
    {
        var o = new Order { Tick = g.Tick, Op = "scout", Do = "district-visit", Run = true, Target = g.District!.Map.People[contact].Id };
        Assert.True(g.Do(o), g.Orders.Last().why);
        for (int i = 0; i < 26000 && g.Ops[0].Walk != null && !g.Over; i++) g.Step();
        Assert.False(g.Over, g.FailText);
        Assert.Null(g.Ops[0].Walk);
    }
    static Answer Ask(Game g, string verb, int from, int to, string place = "here")
    {
        var map = g.District!.Map;
        return g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = verb, Target = map.People[from].Id, To = map.People[to].Id, Approach = place });
    }
    static void Until(Game g, Func<bool> done)
    {
        for (int i = 0; i < 40000 && !done() && !g.Over; i++) g.Step();
        Assert.False(g.Over, g.FailText);
        Assert.True(done(), g.District!.News);
    }
    static void Replay(Game g)
    {
        var s = Script.Parse(Script.Write(g, Repo.Inputs.Hash));
        var replay = Engine.Play(Repo.Inputs, s.Start, s.Orders, s.Until);
        Assert.Equal(g.ClosingHash(), replay.ClosingHash());
    }

    [Fact]
    public void AChainClosesAStreetWhileOnlyTheFirstCitizenKnowsTheCell()
    {
        var g = Open();
        var d = g.District!;
        int first = d.Jobs[0].A;
        Go(g, first);
        Assert.True(Ask(g, "district-back", first, first).ok);
        var path = g.CitizenRoutes(first).First(p => p.Length >= 3 && d.Roams(p.Last()));
        int actor = path.Last();
        var answer = Ask(g, "district-block", first, actor, "road/market-street");
        Assert.True(answer.ok, answer.why);
        var r = Assert.Single(d.Network.Requests);
        var scout = g.Ops[0].Pos;
        Assert.Null(d.Network.AskedBy[actor]);
        g.Advance(DistrictNetwork.CallTicks);
        Assert.Equal(d.Map.People[first].Name, d.Network.AskedBy[path[1]]);
        Assert.Null(d.Network.AskedBy[actor]);
        Until(g, () => r.Stage == CitizenStage.Walking);
        Assert.Equal(d.Map.People[path[path.Length - 2]].Name, d.Network.AskedBy[actor]);
        Assert.False(d.Network.KnowsCell[actor]);
        Assert.False(d.Network.HoldsRoad(r.Site));
        Replay(g);
        Until(g, () => r.Stage == CitizenStage.Acting);
        Assert.True(d.Network.HoldsRoad(r.Site));
        Assert.True(G.Within(g.DistrictPersonAt(actor)!.Value, r.Site, 600));
        Assert.True(G.Within(scout, g.Ops[0].Pos, 650));
        Assert.DoesNotContain(g.Ledger, l => l.cause == "district-known-organiser");
        Until(g, () => d.Network.Completed == 1);
        Assert.False(d.Network.HoldsRoad(r.Site));
        Until(g, () => !d.Network.Away(actor));
        Replay(g);
    }

    [Fact]
    public void DirectRiotDrawsRealResidentsAndLeavesAnAttributablePublicAct()
    {
        var g = Open();
        var d = g.District!;
        int source = d.Jobs[2].A;
        Go(g, source);
        Assert.False(Ask(g, "district-rally", source, source).ok);
        Assert.True(Ask(g, "district-back", source, source).ok);
        var a = Ask(g, "district-rally", source, source, "road/rocket-lane");
        Assert.True(a.ok, a.why);
        var r = Assert.Single(d.Network.Requests);
        Until(g, () => r.Stage == CitizenStage.Acting);
        Assert.InRange(r.Crowd.Count, 1, 4);
        foreach (int p in r.Crowd)
        {
            Assert.Equal(d.Map.People[source].Name, d.Network.AskedBy[p]);
            Assert.Contains(d.Network.Neighbours(p), t => t.person == source);
            Assert.True(G.Within(g.DistrictPersonAt(p)!.Value, r.Site, 900));
        }
        Assert.True(g.Alert >= 2);
        Assert.True(d.Network.HoldsRoad(r.Site));
        Assert.Contains(g.Ledger, l => l.cause == "district-known-organiser");
        Assert.True(g.Do(new Order { Tick = g.Tick, Op = "cell", Do = "district-recall", Target = d.Map.People[source].Id }));
        Assert.False(d.Network.HoldsRoad(r.Site));
        Assert.Empty(d.Network.Requests);
        Replay(g);
    }

    [Fact]
    public void LoyaltyAndStandingDetermineWhetherSomeoneRisksTheGuardFight()
    {
        var g = Open();
        var d = g.District!;
        int first = d.Jobs[1].A;
        Go(g, first);
        Assert.True(Ask(g, "district-back", first, first).ok);
        Assert.False(Ask(g, "district-guard", first, first).ok);
        Assert.True(Ask(g, "district-bond", first, first).ok);
        var route = g.CitizenRoutes(first).First(p => p.Length > 1 && d.Roams(p.Last()));
        var actor = route.Last();
        var order = Ask(g, "district-guard", first, actor);
        Assert.True(order.ok, order.why);
        Until(g, () => d.Network.GuardOccupied);
        Assert.False(g.GuardAtDoor);
        Until(g, () => d.Network.Requests.Count == 0);
        Assert.True(g.GuardDown);
        Assert.True(d.Network.Fallen[actor]);
        Assert.Null(g.DistrictPersonAt(actor));
        Assert.DoesNotContain(g.CitizenRoutes(first), p => p.Contains(actor));
        Assert.Contains(d.Map.People[actor].Ties, t => d.Memory[t.person]?.Contains(d.Map.People[actor].Name) == true);
        Assert.False(g.Over);
        Replay(g);
    }

    [Fact]
    public void GoodStandingCanWinProtectionWithoutAPriorPersonalFavour()
    {
        var g = Open();
        var d = g.District!;
        int first = d.Jobs[2].A;
        Go(g, first);
        Assert.Equal(0, d.Trust[first]);
        Assert.False(Ask(g, "district-protect", first, first).ok);
        g.Standing = 4;
        Assert.True(Ask(g, "district-protect", first, first).ok);
        Until(g, () => d.Network.Requests[0].Stage == CitizenStage.Acting);
        Assert.True(d.Network.KnowsCell[first]);
        Assert.True(g.Do(new Order { Tick = g.Tick, Op = "cell", Do = "district-recall", Target = d.Map.People[first].Id }));
        Until(g, () => !d.Network.Away(first));
    }

    [Fact]
    public void TheCellCarWaitsForAResidentsBlockThenContinuesItsOriginalOrder()
    {
        var g = Open();
        var d = g.District!;
        int source = d.Jobs[0].A;
        Go(g, source);
        Assert.True(Ask(g, "district-back", source, source).ok);
        Assert.True(Ask(g, "district-block", source, source, "road/market-street").ok);
        var r = d.Network.Requests.Single();
        var driver = g.Ops[(int)OpId.Fixer];
        void DriveOrder(string verb, Pt? where = null, string target = "")
        {
            var a = g.Issue(new Order { Tick = g.Tick, Op = "fixer", Do = verb, Point = where, Target = target });
            Assert.True(a.ok, a.why);
        }
        DriveOrder("move", g.Walk.NearestFree(g.GetawaySeat));
        Until(g, () => driver.Walk == null);
        DriveOrder("board", target: "car");
        Until(g, () => driver.InCar);
        Until(g, () => r.Stage == CitizenStage.Acting);
        DriveOrder("drive-car", r.Site);
        Until(g, () => G.Within(r.Site, g.GetawayCarPos(), 950));
        g.Advance(12);
        var waiting = g.GetawayCarPos();
        Assert.True(g.GetawayDriving);
        g.Advance(18);
        Assert.Equal(waiting, g.GetawayCarPos());
        Assert.True(g.Do(new Order { Tick = g.Tick, Op = "cell", Do = "district-recall", Target = d.Map.People[source].Id }));
        Until(g, () => !g.GetawayDriving);
        Assert.True(G.Within(r.Site, g.GetawayCarPos(), 500));
        Replay(g);
    }

    [Fact]
    public void ALocalDefenderCanDieForAnOperativeTheyNeverMet()
    {
        var g = Open();
        var d = g.District!;
        int first = d.Jobs[0].A;
        Go(g, first);
        Assert.True(Ask(g, "district-back", first, first).ok);
        var path = g.CitizenRoutes(first).First(p => p.Length >= 3 && d.Roams(p.Last()));
        int actor = path.Last();
        Assert.True(Ask(g, "district-protect", first, actor).ok);
        Until(g, () => d.Network.Requests[0].Stage == CitizenStage.Acting);
        var scout = g.Ops[0];
        int hp = scout.Gun.Health;
        var damage = typeof(Game).GetMethod("Damage", BindingFlags.NonPublic | BindingFlags.Instance)!;
        damage.Invoke(g, new object[] { "scout", scout.Gun, g.R.HealthCivilian, "guard", false, false });
        Assert.Equal(hp, scout.Gun.Health);
        Assert.True(d.Network.Fallen[actor]);
        Assert.False(d.Network.KnowsCell[actor]);
        Assert.Empty(d.Network.Requests);
        damage.Invoke(g, new object[] { "scout", scout.Gun, 1, "guard", false, false });
        Assert.Equal(hp - 1, scout.Gun.Health);
    }
}
