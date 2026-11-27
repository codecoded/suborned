using System;
using System.Linq;
using Suborned.Sim;
using Xunit;

public sealed class HarbourStreetTests
{
    static Pt Plan(int east, int north) => new((-230 - north) * 100, (east + 130) * 100);
    static Game Open() => Engine.New(Repo.Inputs, new Start { District = true, Seed = 9 });

    [Fact]
    public void PublicGroundReachesThePierLawnsAndBothEndsOfCrownWalk()
    {
        var game = Open();
        var floor = game.District!.Map.Ground;
        foreach (var spot in new[] { Plan(68, -148), Plan(174, -138), Plan(232, -138),
            Plan(-200, 101), Plan(-200, 82), Plan(0, 244), Plan(-50, 244), Plan(44, 244) })
        {
            var clicked = game.DistrictWalkPoint(spot);
            Assert.NotNull(clicked);
            Assert.True(G.Within(spot, clicked!.Value, 300));
            Assert.NotNull(game.StreetWalk(game.Ops[0].Pos, clicked.Value));
        }
        Assert.Equal(Plan(68, -148), game.DistrictWalkPoint(Plan(68, -148)));
        Assert.Equal(-85, floor.ElevationCm(Plan(245, -138)));
        Assert.InRange(floor.ElevationCm(Plan(77, -138)), -84, 17);
        Assert.Null(game.DistrictWalkPoint(Plan(245, -138))); // inside the moored liner
        Assert.Null(game.DistrictWalkPoint(Plan(210, -170)));
        Assert.Null(game.DistrictWalkPoint(Plan(174, -148)));
    }

    [Fact]
    public void LocalGatheringsArePopulatedReachableAndConnectedThroughPeople()
    {
        var d = Open().District!;
        foreach (var venue in d.Map.Ground.Venues)
        {
            var locals = d.Map.People.Where(p => p.Id.StartsWith("p-z-harbour-" + venue.Id + "-", StringComparison.Ordinal)).ToArray();
            Assert.True(locals.Length >= 3, venue.Name);
            Assert.All(locals, p => Assert.NotEmpty(p.Ties));
            Assert.Contains(locals, p => p.Ties.Any(t => Array.IndexOf(locals, d.Map.People[t.person]) < 0));
            foreach (int tick in new[] { 0, Game.T(90), Game.T(1200) })
                Assert.True(locals.Count(p => G.Within(venue.At, d.Pose(d.Map.Index[p.Id], tick).at, 3500)) >= 2, venue.Name);
        }
        foreach (int i in Enumerable.Range(0, d.Map.People.Length).Where(i => d.Map.People[i].Local))
        {
            var first = d.Pose(i, Game.T(86400) - 1);
            var next = d.Pose(i, Game.T(86400));
            Assert.True(G.Within(first.at, next.at, 100), d.Map.People[i].Name);
        }
    }

    [Fact]
    public void CommutersReachTheirNamedDestinationAndServiceMachinesWorkThePublicFloor()
    {
        var d = Open().District!;
        var map = d.Map;
        foreach (var (origin, destination) in new[] { ("apron-walkups", "rocket-gate"),
            ("orbit-walkups", "apron-walkups"), ("lantern-commuters", "meridian-court"),
            ("lamplight-commuters", "lamplight"), ("quay-commuters", "ferry-landing"), ("garden-commuters", "gardens") })
        {
            var walker = map.People[map.Index["p-z-harbour-" + origin + "-04"]];
            var other = map.Ground.Venues.Single(v => v.Id == destination);
            Assert.True(walker.Walk.Length > 15000 && G.Within(walker.Walk.End, other.At, 1800), origin);
            Assert.Contains(walker.Ties, t => t.kind == "regular" && t.person == map.Index["p-z-harbour-" + destination + "-02"]);
        }
        var traffic = d.Traffic;
        var robots = Enumerable.Range(0, traffic.RobotCount).ToArray();
        foreach (StreetRobot kind in Enum.GetValues(typeof(StreetRobot)))
            Assert.Contains(robots, i => traffic.RobotKind(i) == kind);
        foreach (var (id, kind) in new[] { ("ferry-landing", StreetRobot.Porter),
            ("lantern-market", StreetRobot.Shopkeeper), ("gardens", StreetRobot.Nanny) })
        {
            var site = map.Ground.Venues.Single(v => v.Id == id);
            Assert.Contains(robots, i => traffic.RobotKind(i) == kind && Enumerable.Range(0, 60)
                .Select(t => traffic.Robot(i, t * 13)).Any(p => p.Moving && G.Within(p.At, site.At, 3000)));
        }
        Assert.Contains(Enumerable.Range(0, traffic.CarCount), traffic.DeliveryVan);
    }

    [Fact]
    public void PassingCarWaitsAtTheCrossingAndResumesWhenThePedestrianLeaves()
    {
        var g = Open();
        var traffic = g.District!.Traffic;
        var op = g.Ops[0]; var home = op.Pos;
        int car = Enumerable.Range(0, traffic.CarCount).First(i => {
            var p = traffic.Car(i, 0);
            return p.Moving && !g.S.Grid.InGrid(p.At) && g.District.Map.Roads.Free(p.At)
                && G.D2(p.At, traffic.Car(i, 20).At) > 500L * 500L;
        });
        var approach = traffic.Car(car, 3);
        op.Pos = CarGeo.Ahead(approach.At, approach.Facing, traffic.DeliveryVan(car) ? 510 : 420);
        void TrafficTick() { g.Tick++; traffic.Step(g, g.District!); }
        for (int i = 0; i < 3; i++) TrafficTick();
        var stopped = traffic.Car(car, g.Tick);
        Assert.False(stopped.Moving);
        for (int i = 0; i < 14; i++)
        {
            TrafficTick();
            Assert.Equal(stopped.At, traffic.Car(car, g.Tick).At);
        }
        op.Pos = home;
        for (int i = 0; i < 30; i++) TrafficTick();
        Assert.True(G.D2(stopped.At, traffic.Car(car, g.Tick).At) > 200L * 200L);
    }

    [Fact]
    public void ScheduledFleetServesBothPlatformSidesAndNeighbourhoodPatrols()
    {
        var district = Open().District!;
        var traffic = district.Traffic;
        var roads = district.Map.Roads;
        var origin = roads.NearestFree(Plan(-186, -406))!.Value;
        foreach (var street in district.Map.Streets)
            foreach (int quarter in new[] { 1, 2, 3 })
            {
                var target = roads.NearestFree(street.Path.At(street.Path.Length * quarter / 4));
                Assert.True(target.HasValue && roads.PieceOf(target.Value) == roads.PieceOf(origin), street.Id);
            }
        Assert.True(traffic.CarCount > district.Map.Streets.Length);
        Assert.True(traffic.RobotCount > 10);
        Assert.True(traffic.TramCount >= 4);
        foreach (var stop in district.Map.Ground.Platforms)
            Assert.Contains(Enumerable.Range(0, 1500).Select(i => traffic.Tram(0, i * 10)),
                pose => !pose.Moving && G.Within(stop.Centre, pose.At, 650));
        var stopCentre = Plan(-340, 131);
        Assert.False(district.Map.Roads.Free(stopCentre));
        var carsAtStop = Enumerable.Range(0, traffic.CarCount).SelectMany(i =>
            Enumerable.Range(0, 20).Select(t => traffic.Car(i, t * 29))).Where(car =>
            Math.Abs(car.At.X - stopCentre.X) < 1500 && Math.Abs(car.At.Y - stopCentre.Y) < 1500).ToArray();
        Assert.NotEmpty(carsAtStop);
        Assert.All(carsAtStop, car => Assert.InRange(Math.Abs(car.At.Y - stopCentre.Y), 700, 1199));
        var court = district.Map.Ground.Venues.Single(v => v.Id == "sunset-coffee");
        Assert.True(court.Court.Length > 3);
        Assert.Equal(18, district.Map.Ground.ElevationCm(court.At));
        foreach (string use in new[] { "homes", "market", "gardens", "quay" })
            Assert.Contains(district.Map.Ground.Courts, c => c.Style == use);
        Assert.NotEmpty(district.Map.Ground.Buildings);
        foreach (var corner in district.Map.Ground.Buildings)
        {
            Assert.True(district.Map.Ground.IsWall(corner.At));
            Assert.False(district.Map.Walk.Free(corner.At));
        }
        foreach (var building in new[] { Plan(-232, -60), Plan(-167, -456) })
        {
            Assert.True(district.Map.Ground.IsWall(building));
            Assert.False(district.Map.Walk.Free(building));
        }
        var lanternPassage = Plan(-132, -431); // The old housing podium now opens onto Market Street.
        Assert.False(district.Map.Ground.IsWall(lanternPassage));
        Assert.True(district.Map.Walk.Free(lanternPassage));
        Assert.Contains(district.Map.Ground.Galleries, g => G.Within(Plan(-244,-64),g.At,200));
        var a = traffic.Robot(1, 0);
        Assert.Contains(Enumerable.Range(1, 90).Select(t => traffic.Robot(1, t * 10)), p => p.Moving && p.At != a.At);
    }
}
