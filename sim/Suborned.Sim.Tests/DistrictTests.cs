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

    static Game ReplayOldDistrictPrices(Game run, string version = "suborned.sim/0.61.0")
    {
        // Historical saves identified their engine in the record and had no per-order price marker.
        var record = Script.Write(run, Repo.Inputs.Hash).Replace(Engine.Version, version);
        var script = Script.Parse(record);
        var live = Engine.New(Repo.Inputs, script.Start);
        foreach (var order in script.Orders)
        {
            live.RunTo(order.Tick);
            live.Issue(order);
        }
        live.RunTo(script.Until);
        return live;
    }

    [Theory]
    [InlineData(14999, false, false)]
    [InlineData(15000, true, false)]
    [InlineData(39999, true, false)]
    [InlineData(40000, true, true)]
    public void ContactPaymentsUseTheQuotedPurseAndCannotChargeTwice(long purse, bool help, bool surety)
    {
        var g = Engine.New(Repo.Inputs, new Start { District = true, Seed = 9, Cash = purse });
        const string jamie = "p-z-harbour-lantern-papers-04";
        Order(g, "district-visit", who: jamie);
        Arrive(g);
        Answer Pay(string verb) => g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = verb, Target = jamie });
        var favour = Pay("district-back");
        Assert.Equal(help, favour.ok);
        if (!help) { Assert.Equal("no-cash", favour.code); Assert.Equal(15000, favour.usd); }
        Assert.False(Pay("district-back").ok);
        var guarantee = Pay("district-bond");
        Assert.Equal(surety, guarantee.ok);
        if (help && !surety) { Assert.Equal("no-cash", guarantee.code); Assert.Equal(25000, guarantee.usd); }
        Assert.False(Pay("district-bond").ok);
        Assert.Equal(purse - (help ? 15000 : 0) - (surety ? 25000 : 0), g.Cash);
        Assert.Equal((help ? 1 : 0) + (surety ? 1 : 0), g.District!.Trust[g.District.Map.Index[jamie]]);
    }

    [Theory]
    [InlineData("suborned.sim/0.60.1")]
    [InlineData("suborned.sim/0.61.0")]
    public void ContinueKeepsOldPaymentsAndUsesCurrentTermsForTheNextDeal(string version)
    {
        var fresh = Open();
        const string jamie = "p-z-harbour-lantern-papers-04";
        Order(fresh, "district-visit", who: jamie);
        Arrive(fresh);
        Order(fresh, "district-back", who: jamie);
        var continued = ReplayOldDistrictPrices(fresh, version);
        Assert.Equal(100000 - 15, continued.Cash);
        Order(continued, "district-bond", who: jamie);
        Assert.Equal(100000 - 15 - 25000, continued.Cash);
        var mixed = Script.Parse(Script.Write(continued, Repo.Inputs.Hash));
        var reopened = Engine.Play(Repo.Inputs, mixed.Start, mixed.Orders, mixed.Until);
        Assert.Equal(continued.Cash, reopened.Cash);
        Assert.Equal(continued.ClosingHash(), reopened.ClosingHash());
    }

    [Fact]
    public void LanternCourtIsAThroughWalkWithOccupiedWingsAndAClockPedestal()
    {
        var g = Open();
        var map = g.District!.Map;
        var court = map.Ground.Venues.Single(v => v.Id == "meridian-court");
        var market = map.Ground.Venues.Single(v => v.Id == "lantern-market");
        Assert.DoesNotContain(market.Fixtures, f => f.Kit == "CurvedKiosk_Play");
        var neighbours = map.People.Where(p => p.Id.Contains("meridian-court")).ToArray();
        Assert.Equal(16, neighbours.Length);
        Assert.All(neighbours, p => Assert.True(p.Walk.Length > 20000));
        Assert.Contains(neighbours[0].Ties, tie => map.People[tie.person].Id.Contains("lantern-papers"));
        Assert.False(map.Walk.Free(new Pt(22800, -1800))); // stone seat below the globe
        Assert.False(map.Walk.Free(new Pt(22600, -3700))); // a ground-floor apartment
        foreach (var point in new[] { new Pt(25100, 100), court.At, new Pt(21200, 200), new Pt(19200, 200) })
        {
            Assert.True(map.Walk.Free(point));
            Assert.False(map.Roads.Free(point));
            Order(g, "move", point);
            Arrive(g);
            Assert.True(G.Within(g.Ops[0].Pos, point, 210));
        }
    }

    [Fact]
    public void LanternTradersWorkBesideTheirStockAndCanIntroduceARegular()
    {
        var district = Open().District!;
        var map = district.Map;
        var market = map.Ground.Venues.Single(v => v.Id == "lantern-market");
        var court = map.Ground.Venues.Single(v => v.Id == "meridian-court");
        var newsreader = map.Index["p-z-harbour-lantern-papers-04"];
        var florist = map.Index["p-z-harbour-meridian-court-02"];
        var fruiterer = map.Index["p-z-harbour-lantern-market-02"];
        foreach (var (worker, role, counter) in new[] {
            (florist, "flower-seller", court.Fixtures.Single(f => f.Kit == "FlowerBarrow")),
            (fruiterer, "fruiterer", market.Fixtures.Single(f => f.Kit == "ProduceStall")) })
        {
            var seller = map.People[worker];
            Assert.Equal(role, seller.Job);
            Assert.True(map.Walk.Free(seller.Walk.End));
            Assert.True(G.Within(seller.Walk.End, counter.At, 460));
            Assert.False(map.Roads.Free(counter.At));
        }
        Assert.Equal("Angela Hardin", map.People[florist].Name);
        Assert.Contains(district.Network.Routes(newsreader, _ => true),
            route => route.SequenceEqual(new[] { newsreader, florist, fruiterer }));
    }

    [Fact]
    public void FerryForecourtConnectsToThePromenadeWithResidentsAndASeawardBoundary()
    {
        var g = Open();
        var map = g.District!.Map;
        var landing = map.Ground.Venues.Single(v => v.Id == "ferry-landing");
        var outer = map.Ground.Waterfront.Where(s => s.Id == "quay/3").OrderByDescending(s => s.B.Y).First();
        Assert.True(outer.Contains(landing.At));
        Assert.True(map.Walk.Free(landing.At));
        Assert.False(map.Roads.Free(landing.At));
        Assert.False(map.Ground.IsWater(landing.At));
        Assert.Equal(-85, map.Ground.ElevationCm(landing.At));
        var sea = new Pt(outer.Centre.X, outer.B.Y + 300);
        Assert.True(map.Ground.IsWater(sea));
        Assert.False(map.Walk.Free(sea));
        var passengers = map.People.Where(p => p.Id.Contains("ferry-landing")).ToArray();
        var boarding = map.Ground.Waterfront.Single(s => s.Id == "quay/1");
        Assert.Equal(12, passengers.Length);
        Assert.All(passengers, p => Assert.True(boarding.Contains(p.Walk.End)));
        var clerk = passengers.Single(p => p.Job == "booking-clerk");
        var porter = passengers.Single(p => p.Job == "ferry-porter");
        var conductor = System.Array.FindIndex(map.People, p => p.Job == "tram-conductor");
        Assert.Contains(clerk.Ties, t => t.person == map.Index[porter.Id] && t.kind == "workmate");
        Assert.Contains(porter.Ties, t => t.person == conductor && t.kind == "regular");
        var introductions = new[] { map.Index[clerk.Id], map.Index[porter.Id], conductor };
        Assert.Contains(g.District.Network.Routes(introductions[0], _ => true), chain => chain.SequenceEqual(introductions));
        Order(g, "move", landing.At);
        Arrive(g);
        Assert.True(G.Within(g.Ops[0].Pos, landing.At, 210));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(23)]
    [InlineData(73)]
    public void MissedWindowStillAllowsAnHonestAndAFalseIntroductionWithReplay(int seed)
    {
        var g = Engine.New(Repo.Inputs, new Start { District = true, Seed = seed });
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
            long before = g.Cash;
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
                Assert.Equal(before + (k == 2 ? (n == 0 ? 35000 : 70000) : 0), g.Cash);
            }

            Assert.NotEqual(j, d.Jobs[n]);
            Assert.NotNull(d.Memory[j.C]);
        }

        Assert.Equal(2, d.Completed);
        Assert.Equal(start + 105000, g.Cash);
        Assert.Equal(start + 105, ReplayOldDistrictPrices(g).Cash);
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
