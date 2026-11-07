// Brief 098 step 1, law L8 (traffic as real cars on the dial): laws that hold for every seed and count, never a pinned moment.
//    UnsetIsNoTraffic / CarsZeroIsTodaysWorld   no cars: nothing runs, the start hashes as before; cars 0: today's world, its own start hash
//    UnsetLeavesEveryHashIdentical              a route unset plays the same checkpoints with or without the lanes and the dial's rules loaded
//    ACarStopsForAPersonInItsLane...             it stops short of someone standing in its lane, never touching them, and goes on once they've crossed
//    ACarQueuesBehindAStandingCar               the car behind stops behind the stopped one, a gap clear, never meeting it
//    GunfireNearACarMakesItStop                 a shot within crowd.scatter: every driver at the wheel brakes to a stop, then ducks or flees by nature
//    ALockdownStopsNewEntries                   from the lockdown's Alert no car comes into the zone, and the zone's cars leave it
//    ACarShotToDisabledStopsAndBlocks           disabled, it stands for good where it stopped, its driver out and running, ground nobody walks or drives through
//    ADriverWhoSeesADrawnGunReactsByNature      a drawn weapon in the driver's cone: they brake, then duck or flee by their temperament
//    AddingCarsMovesNoCarAlreadyThere           a slot's trips are the same whatever the count
//    CountsReplayAndASmallSweepRunsClean        seed + counts + orders replay exactly; cars 0, 8 and 32 play to an end with no engine error
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class TrafficTests
{
    static readonly string Routes = System.IO.Path.Combine(Engine.FindRoot(AppContext.BaseDirectory), "contracts", "missions", "handover", "routes");
    static Script Route(string name) { var s = Script.Parse(File.ReadAllText(System.IO.Path.Combine(Routes, name + ".json"))); s.Expect = null; return s; }
    static Start Cars(int seed, int cars) { var s = new Start { Seed = seed }; s.Counts[Population.Cars] = cars; return s; }
    static Game New(int seed, int cars) => Engine.New(Repo.Inputs, Cars(seed, cars));
    static Game Play(string route, int seed, int? cars) { var s = Route(route); s.Start.Seed = seed; if (cars.HasValue) s.Start.Counts[Population.Cars] = cars.Value; return Engine.Play(Repo.Inputs, s.Start, s.Orders, s.Until); }
    static TrafficRules Tr => Repo.Inputs.Population!.Traffic;
    static Pt[] Box(TrafficView v) => CarGeo.Rect(v.At, v.Heading, v.Length / 2, v.Width / 2);
    static TrafficView View(Game g, string id) => g.TrafficNow().Single(v => v.Id == id);
    /// <summary>A car on its way on a straight stretch: in the zone, at speed, on its lane's line, `ahead` mm of lane before its next turn or its end.</summary>
    static TrafficCar? Cruising(Game g, long ahead)
    {
        foreach (var c in g.Traffic)
        {
            var v = View(g, c.Id); if (!v.InZone || c.V < 300 || c.Lat != 0 || c.LatGoal != 0 || c.Mode != TrafficMode.Drive) continue;
            int li = c.Route.LaneAt(c.Mm); if (li < 0) continue; var (lane, _, to) = c.Route.Lanes[li]; if (to - c.Front < ahead || lane.To.StartsWith("mouth:", StringComparison.Ordinal)) continue; // a lane to a junction, inside the zone
            if (!g.S.Grid.InGrid(Ahead(c, ahead))) continue;
            if (g.TrafficWhy(c.Id).Contains(" m ahead") && !g.TrafficWhy(c.Id).StartsWith("turn", StringComparison.Ordinal)) continue; // something already in its way
            return c;
        }
        return null;
    }
    /// <summary>A car moving on its lane's line in the zone with `ahead` mm of the lane still before it.</summary>
    static bool Cruising1(Game g, TrafficCar c, long ahead) { var v = View(g, c.Id); int li = c.Route.LaneAt(c.Mm); return v.InZone && c.V >= 100 && c.Lat == 0 && c.LatGoal == 0 && li >= 0 && c.Route.Lanes[li].to - c.Front >= ahead; }
    static Pt Ahead(TrafficCar c, long mm) => c.Route.Path.At(Math.Min(c.Route.Path.Length, c.Front + mm));
    static void Stand(Game g, OpId who, Pt at) { var op = g.OpOf(who); op.Walk = null; op.WalkMm = 0; op.Pos = at; }
    /// <summary>A free walk spot `minCm`..`maxCm` from `at` with a clear eye line to it.</summary>
    static Pt? SpotNear(Game g, Pt from, Pt at, int minCm, int maxCm) => g.S.Grid.Nearest(from, q => G.D2(q, at) >= (long)minCm * minCm && G.D2(q, at) <= (long)maxCm * maxCm && g.S.Sight(q, at, g.R.Eye), 6000);

    [Fact]
    public void UnsetIsNoTraffic()
    {
        var g = Engine.New(Repo.Inputs);
        Assert.False(g.TrafficOn); Assert.Empty(g.Traffic); Assert.Empty(g.TrafficNow()); Assert.Equal(0, g.TrafficSlots);
        var plain = new Game(Repo.Inputs.Rules, Repo.Inputs.SiteData, new Start(), Repo.Inputs.Hash.ToString(System.Globalization.CultureInfo.InvariantCulture), Repo.Inputs.Crowd);
        Assert.Equal(plain.StartHash, g.StartHash);
        var today = Population.Today(Repo.Inputs).Single(c => c.Kind == Population.Cars);
        Assert.Equal((0, 0), (today.Today, today.Floor)); Assert.True(today.Max > 0);
        Assert.Throws<FormatException>(() => Engine.New(NoLanes(), Cars(1, 8))); // cars need their lanes: a host that loaded none is told so
    }
    static Engine.Inputs NoLanes() { var i = Repo.Inputs; return Engine.FromTexts(i.Rulebook, i.PeopleRules, i.Site, i.People, i.Provisional, i.Takedown, i.Rounds, i.Population!.Text); }

    [Theory] [InlineData("route-a", 3)] [InlineData("loud-fight", 2)]
    public void CarsZeroIsTodaysWorld(string route, int seed)
    {
        var a = Play(route, seed, null); var b = Play(route, seed, 0);
        Assert.False(b.TrafficOn); Assert.NotEqual(a.StartHash, b.StartHash); // the counts are hashed into the start
        Assert.Equal((a.Result, a.FailCode, a.Tick), (b.Result, b.FailCode, b.Tick));
        Assert.Equal(a.Log.Select(l => (l.Tick, l.Code)), b.Log.Select(l => (l.Tick, l.Code)));
    }

    [Theory] [InlineData("route-a")] [InlineData("route-b")] [InlineData("loud-fight")]
    public void UnsetLeavesEveryHashIdentical(string route)
    {
        var s = Route(route); var a = Engine.Play(Repo.Inputs, s.Start, s.Orders, s.Until);
        var i = Repo.Inputs; var bare = Engine.FromTexts(i.Rulebook, i.PeopleRules, i.Site, i.People, i.Provisional, i.Takedown, i.Rounds); // no dial, no lanes
        var b = Engine.Play(bare, s.Start, s.Orders, s.Until);
        Assert.Equal(b.Checkpoints, a.Checkpoints);
    }

    [Theory] [InlineData(1)] [InlineData(4)] [InlineData(9)]
    public void ACarStopsForAPersonInItsLaneAndGoesOnOnceTheyHaveCrossed(int seed)
    {
        var g = New(seed, 8); TrafficCar? c = null;
        for (int t = 1; t <= 120 && c == null; t++) { g.RunTo(Game.T(t)); c = Cruising(g, 25000); }
        Assert.NotNull(c);
        var home = g.OpOf(OpId.Scout).Pos; var spot = Ahead(c!, 14000); Stand(g, OpId.Scout, spot);
        bool stopped = false; long clear = g.R.CrowdOccludeCm;
        for (int k = 0; k < Game.T(6); k++)
        {
            g.Step(); Stand(g, OpId.Scout, spot); var v = View(g, c!.Id);
            Assert.False(CarGeo.Inside(spot, Box(v)), "a car never drives over a person");
            long body = (long)Math.Max(0, G.Dist(v.At, spot) - v.Length / 2); Assert.True(body >= clear, $"it keeps clear of them ({body} cm)");
            if (v.Speed == 0) stopped = true;
        }
        Assert.True(stopped, $"it stops for the person in its lane ({g.TrafficWhy(c!.Id)})");
        long at = c!.Mm; Stand(g, OpId.Scout, home);
        bool on = false; for (int k = 0; k < Game.T(10) && !on; k++) { g.Step(); on = c.Mm - at > 3000; }
        Assert.True(on, $"and goes on once they have crossed ({g.TrafficWhy(c.Id)})");
    }

    [Theory] [InlineData(2)] [InlineData(5)] [InlineData(7)]
    public void ACarQueuesBehindAStandingCar(int seed)
    {
        var g = New(seed, 16); TrafficCar? lead = null, next = null;
        for (int t = 1; t <= 400 && next == null; t++)
        {
            g.RunTo(Game.T(t)); lead = g.Traffic.FirstOrDefault(c => Cruising1(g, c, 20000) && c.Mode == TrafficMode.Drive && c.Route.Lanes[c.Route.LaneAt(c.Mm)].lane.To.StartsWith("j:", StringComparison.Ordinal) && g.S.Grid.InGrid(Ahead(c, 14000))); if (lead == null) continue;
            int li = lead.Route.LaneAt(lead.Mm); var lane = lead.Route.Lanes[li].lane; long leadOn = lead.Mm - lead.Route.Lanes[li].from; // where along the lane, whatever the route
            next = g.Traffic.FirstOrDefault(o => o != lead && o.Route.LaneAt(o.Mm) is int oi && oi >= 0 && o.Route.Lanes[oi].lane == lane && o.Lat == 0 && o.Mode == TrafficMode.Drive
                && (leadOn - lead.HalfLen * 10L) - (o.Mm - o.Route.Lanes[oi].from + o.HalfLen * 10L) is long gap && gap > 0 && gap < 30000);
        }
        Assert.True(next != null, $"a pair on one lane (cars {g.Traffic.Count}, cruising {g.Traffic.Count(c => Cruising1(g, c, 25000))})");
        var spot = Ahead(lead!, 14000); Stand(g, OpId.Scout, spot);
        bool queued = false;
        for (int k = 0; k < Game.T(14); k++)
        {
            g.Step(); Stand(g, OpId.Scout, spot);
            Assert.True(g.TrafficCarById(lead!.Id) != null && g.TrafficCarById(next!.Id) != null, $"both still on the roads at {g.Tick} (lead {lead.Id} {lead.Mm}/{lead.Route.Path.Length} {lead.Gone}, next {next!.Id} {next.Mm}/{next.Route.Path.Length} {next.Gone}, routes {lead.Route.Id} {next.Route.Id})");
            var a = View(g, lead.Id); var b = View(g, next.Id);
            Assert.False(CarGeo.PolysMeet(Box(a), Box(b)), "the car behind never meets the stopped car");
            if (a.Speed == 0 && b.Speed == 0) { queued = true; long apart = G.Dist(a.At, b.At) - a.Length / 2 - b.Length / 2; Assert.True(apart >= Tr.GapCm - 70, $"it stops a gap behind ({apart} cm)"); }
        }
        Assert.True(queued, "the car behind queues behind the standing car");
    }

    [Theory] [InlineData(1)] [InlineData(3)] [InlineData(6)]
    public void GunfireNearACarMakesItStop(int seed)
    {
        // the setup needs another car near the shot; a moment where none passes says nothing about the law, so the next moment is tried (a fresh game)
        int tried = 0;
        for (int t0 = 20; t0 <= 200; t0 += 15)
        {
            var g = New(seed, 16); TrafficCar? target = null; Pt? spot = null;
            for (int t = t0; t <= 200 && spot == null; t++) { g.RunTo(Game.T(t)); target = Cruising(g, 15000); if (target != null) spot = SpotNear(g, g.Enforcer.Pos, g.TrafficNow().Single(v => v.Id == target.Id).At, 600, 1000); }
            if (spot == null) break;
            Stand(g, OpId.Enforcer, spot!.Value); g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "draw" });
            for (int k = 0; k < Game.T(2); k++) { g.Step(); Stand(g, OpId.Enforcer, spot.Value); }
            int shot0 = g.LastShotTick; var a = g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "fire", Target = target!.Id }); Assert.True(a.ok, a.code);
            var near = new List<TrafficCar>();
            for (int k = 0; k < Game.T(10) && near.Count == 0; k++)
            {
                g.Step(); Stand(g, OpId.Enforcer, spot.Value);
                if (g.LastShotTick > shot0 && g.LastShotTick == g.Tick) foreach (var c in g.Traffic) if (c != target && c.Out == null && G.Within(View(g, c.Id).At, spot.Value, g.R.CrowdScatterCm - 200) && c.Mode != TrafficMode.Abandoned) near.Add(c); // engine 0.48.0: near on a shot's own tick (a car that drives up after the shot never heard it; 0.47 took it on any tick after)
            }
            if (near.Count == 0) continue;
            tried++;
            var stoppedAt = new Dictionary<string, int>();
            for (int k = 0; k < Game.T(6); k++) { g.Step(); foreach (var c in near) if (c.V == 0 && !stoppedAt.ContainsKey(c.Id)) stoppedAt[c.Id] = g.Tick; }
            foreach (var c in near)
            {
                Assert.True(stoppedAt.ContainsKey(c.Id) || c.Mode == TrafficMode.Flee, $"{c.Id} brakes and stops at the gunfire (now {c.Mode}, {c.V} cm/s, {G.Dist(View(g, c.Id).At, spot.Value)} cm from the shot)");
                if (c.Out != null) continue; // a stray round on it: its driver got out
                var nature = Tr.Ducks(c.Driver.Temperament) ? TrafficMode.Duck : TrafficMode.Flee;
                Assert.True(c.Mode == nature || (nature == TrafficMode.Duck && c.Mode == TrafficMode.Drive), $"{c.Id}'s {c.Driver.Temperament} driver: {nature}, not {c.Mode}");
            }
            break;
        }
        Assert.True(tried > 0, "some moment had another car near the shot");
    }

    [Fact]
    public void ALockdownStopsNewEntries()
    {
        var g = New(4, 16); g.RunTo(Game.T(30));
        g.Alert = Tr.LockAlert; g.Step(); // the response at lockdown (a test sets the Alert; play gets there by its own reports and gunfire)
        Assert.Contains(g.Log, l => l.Code == "traffic.closed");
        var outside = g.Traffic.Where(c => c.Mm < c.Route.InMm).Select(c => c.Id).ToHashSet(); int trips = g.TrafficTrips;
        for (int k = 0; k < Game.T(90); k++)
        {
            g.Step(); if (g.Over) break;
            foreach (var c in g.Traffic) if (outside.Contains(c.Id)) Assert.True(c.Front < c.Route.InMm, "a car outside stays outside: the road in is closed");
            Assert.Equal(trips, g.TrafficTrips);
        }
        Assert.All(g.Traffic.Where(c => c.Mm >= c.Route.InMm && c.Mm <= c.Route.OutMm), c => Assert.True(c.Standing || c.Mode != TrafficMode.Drive, $"{c.Id} has left or stands for a reason ({c.Mode})"));
    }

    [Theory] [InlineData(2)] [InlineData(8)]
    public void ACarShotToDisabledStopsAndBlocks(int seed)
    {
        var g = New(seed, 16); TrafficCar? car = null; Pt? spot = null;
        for (int t = 20; t <= 200 && spot == null; t++) { g.RunTo(Game.T(t)); car = Cruising(g, 15000); if (car != null) spot = SpotNear(g, g.Enforcer.Pos, View(g, car.Id).At, 500, 900); }
        Assert.NotNull(spot);
        Stand(g, OpId.Enforcer, spot!.Value); g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "draw" });
        for (int k = 0; k < Game.T(2); k++) { g.Step(); Stand(g, OpId.Enforcer, spot.Value); }
        g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "fire", Target = car!.Id });
        for (int k = 0; k < Game.T(30) && !g.CarDisabled(car.Id); k++) { g.Step(); Stand(g, OpId.Enforcer, spot.Value); }
        Assert.True(g.CarDisabled(car.Id), "shot to disabled");
        g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "holster" });
        var at = View(g, car.Id).At;
        Assert.Contains(g.Walkers, w => w.P.Id == car.Driver.Id); Assert.NotNull(car.Out); // its driver got out and runs (the crowd's flight)
        for (int k = 0; k < Game.T(20) && !g.Over; k++)
        {
            g.Step(); var v = View(g, car.Id); Assert.Equal(at, v.At); Assert.Equal(0, v.Speed);
            if (k > Game.T(1)) Assert.Contains(g.StandingCars(), s => s.Id == car.Id); // ground nobody walks through, a box on a way out
            foreach (var o in g.TrafficNow()) if (o.Id != car.Id && o.Speed > 0) Assert.False(CarGeo.PolysMeet(Box(o), Box(v)), $"{o.Id} drives through the disabled car");
        }
        if (car.Cover) Assert.True(g.CoverStanding("car/" + car.Id.Substring(4)), "a standing car is cover");
    }

    [Theory] [InlineData(1)] [InlineData(2)] [InlineData(5)]
    public void ADriverWhoSeesADrawnGunReactsByNature(int seed)
    {
        // the gun alone: a moment where someone fires (security seeing the drawn gun) says nothing about the driver's law, so the next is tried
        int tried = 0;
        for (int t0 = 20; t0 <= 200 && tried == 0; t0 += 15)
        {
            var g = New(seed, 16); TrafficCar? c = null;
            for (int t = t0; t <= 200 && c == null; t++) { g.RunTo(Game.T(t)); c = Cruising(g, 20000); }
            if (c == null) break;
            var spot = g.S.Grid.NearestFree(Ahead(c!, 7000)) ?? Ahead(c!, 7000); Stand(g, OpId.Fixer, spot);
            g.Issue(new Order { Tick = g.Tick, Op = "fixer", Do = "draw" });
            var seen = new List<TrafficMode>(); int shots = g.LastShotTick;
            for (int k = 0; k < Game.T(5); k++) { g.Step(); Stand(g, OpId.Fixer, spot); seen.Add(c!.Mode); }
            if (g.LastShotTick != shots) continue;
            tried++;
            Assert.Contains(TrafficMode.Brake, seen);
            var nature = Tr.Ducks(c!.Driver.Temperament) ? TrafficMode.Duck : TrafficMode.Flee;
            Assert.True(seen.Contains(nature), $"a {c.Driver.Temperament} driver: {nature} (saw {string.Join(",", seen.Distinct())})");
        }
        Assert.True(tried > 0, "some moment let the gun be drawn with no shot");
    }

    [Fact]
    public void AddingCarsMovesNoCarAlreadyThere()
    {
        var a = New(11, 8); var b = New(11, 24);
        Assert.True(b.TrafficSlots > a.TrafficSlots);
        for (int s = 1; s <= a.TrafficSlots; s++) for (int t = 1; t <= 4; t++) Assert.Equal(a.TrafficTripOf(s, t), b.TrafficTripOf(s, t));
    }

    [Fact]
    public void CountsReplayAndASmallSweepRunsClean()
    {
        foreach (var route in new[] { "route-a", "loud-fight", "route-b" })
            foreach (var seed in new[] { 1, 6 })
                foreach (var cars in new[] { 0, 8, 32 })
                {
                    var g = Play(route, seed, cars);
                    Assert.NotEqual("engine-error", g.FailCode);
                    var r = Engine.Replay(Repo.Inputs, g);
                    Assert.Equal(g.Checkpoints, r.Checkpoints);
                }
        var s = Route("route-a"); s.Start.Seed = 3; s.Start.Counts[Population.Cars] = 16; // through a saved script too
        var text = new JWriter().Raw(s.Start.Canonical()).ToString(); var back = Start.Parse(JNode.Parse(text));
        Assert.Equal(16, back.Counts[Population.Cars]);
        Assert.Equal(Engine.Play(Repo.Inputs, s.Start, s.Orders, s.Until).Checkpoints, Engine.Play(Repo.Inputs, back, s.Orders, s.Until).Checkpoints);
    }
}
