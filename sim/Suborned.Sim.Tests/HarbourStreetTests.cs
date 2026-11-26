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
    public void ScheduledFleetServesBothPlatformSidesAndNeighbourhoodPatrols()
    {
        var district = Open().District!;
        var traffic = district.Traffic;
        Assert.True(traffic.CarCount > district.Map.Streets.Length);
        Assert.True(traffic.RobotCount > 10);
        Assert.True(traffic.TramCount >= 4);
        foreach (var stop in district.Map.Ground.Platforms)
            Assert.Contains(Enumerable.Range(0, 1500).Select(i => traffic.Tram(0, i * 10)),
                pose => !pose.Moving && G.Within(stop.Centre, pose.At, 650));
        var a = traffic.Robot(1, 0);
        Assert.Contains(Enumerable.Range(1, 90).Select(t => traffic.Robot(1, t * 10)), p => p.Moving && p.At != a.At);
    }
}
