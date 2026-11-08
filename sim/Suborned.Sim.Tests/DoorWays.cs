// Brief 098 L9 (engine 0.50.0; Aster's ruling on the 0.49.0 report): a doorway is a way (rulebook bodies.door), the law's tests, none pinned to a
// moment: the site's door posts, and the dial's guards on seeds under the dial's top settings.
//  · a door's beat point stands beside its door: on its open side, a body's radius outside every door's way, within the way's depth of the
//    door, on foot ground, with a passage (two personal spaces) from the door's own people (the cast stands, its waiting points, its going-in point);
//  · no guard of the dial at rest at a post stands in a door's way (0.49.0 stood the public door's beat 2 m out on its steps, and the guards
//    sharing it spread across them).
//   dotnet test sim/Suborned.Sim.Tests -c Release --filter FullyQualifiedName~DoorWayTests
using System;
using System.Collections.Generic;
using System.Linq;
using IOPath = System.IO.Path; using File = System.IO.File;
using Suborned.Sim;
using Xunit;

public class DoorWayTests
{
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);
    static Game Play(string route, int seed, string counts, Action<Game> read)
    {
        var s = Script.Parse(File.ReadAllText(IOPath.Combine(Root, "contracts/missions/handover/routes", route + ".json"))); s.Start.Seed = seed; s.Start.Counts.Clear();
        if (counts != "unset") foreach (var kv in counts.Split(',')) { var p = kv.Split('='); s.Start.Counts[p[0]] = int.Parse(p[1]); }
        var g = Engine.New(Repo.Inputs, s.Start); int until = s.Until > 0 ? s.Until : Game.T(Repo.Inputs.SiteData.Over);
        var orders = s.Orders.Select((o, k) => (o, k)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).Select(x => x.o).ToList(); int oi = 0;
        read(g);
        while (g.Tick < until && !g.Over) { while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]); g.Step(); read(g); }
        g.Finish(); return g;
    }

    [Fact]
    public void EveryDoorPostStandsBesideItsDoorOffItsWay()
    {
        var i = Repo.Inputs; var s = i.SiteData; var r = i.Rules; var crowd = i.Crowd!; var ways = DoorWays.Of(s);
        var posts = Population.PointsOf(s, i.Population!, r, crowd).Posts.Where(p => p.id.StartsWith("beat:door:", StringComparison.Ordinal)).ToList();
        Assert.True(posts.Count >= 3, $"the staffed doors have their beat points ({posts.Count})"); // the café, the Office's public door, its official door
        foreach (var (id, at, face) in posts)
        {
            var door = id.Substring("beat:door:".Length); var w = ways.Single(x => x.Id == door);
            Assert.False(DoorWays.In(ways, at, r.BodyDoorDepthCm, r.BodyDoorHalfCm, r.BodyRadiusCm), $"{id} at {at} stands in a door's way");
            long dx = at.X - w.At.X, dy = at.Y - w.At.Y, along = (dx * w.Out.X + dy * w.Out.Y) / 100, across = Math.Abs(dx * w.Side.X + dy * w.Side.Y) / 100;
            Assert.True(along >= 0 && along <= r.BodyDoorDepthCm, $"{id} stands on its door's open side, within the way's depth ({along} cm out)");
            Assert.True(across >= r.BodyDoorHalfCm + r.BodyRadiusCm && across <= r.BodyDoorHalfCm + r.BodyRadiusCm + r.BodyDoorDepthCm, $"{id} stands beside its way ({across} cm off its line)");
            Assert.True(DoorWays.Foot(s, at), $"{id} at {at} is foot ground");
            var held = CastStands.Of(s); foreach (var kv in crowd.Beside) if (kv.Value == door) held.Add(crowd.Places[kv.Key]); if (crowd.DoorIn.TryGetValue(door, out var din)) held.Add(din);
            foreach (var h in held) Assert.True(G.Dist(at, h) >= 2 * r.BodySpaceCm - 1, $"{id} at {at} leaves a passage from the door's people, a person walks between at personal space from both ({G.Dist(at, h)} cm from {h})");
            Assert.Equal(w.Out, face); // he faces out of the door, over the street
        }
    }

    [Theory] [InlineData("guards=64")] [InlineData("guards=64,people=64")]
    public void NoGuardAtRestAtAPostStandsInADoorway(string counts)
    {
        var bad = new List<string>(); int looks = 0, atDoors = 0; var was = new Dictionary<string, Pt>(StringComparer.Ordinal);
        foreach (var route in new[] { "route-a", "route-b" })
            foreach (var seed in new[] { 1, 2, 3 })
            {
                was.Clear();
                Play(route, seed, counts, g =>
                {
                    if (g.Tick % 10 != 0) return; var r = g.R; var ways = DoorWays.Of(g.S);
                    foreach (var w in g.Walkers)
                    {
                        if (!w.Added || w.P.Job != "guard") continue; var bi = g.BodyInfo(w.P.Id); if (!bi.HasValue || bi.Value.stop == null) continue;
                        var at = bi.Value.at; bool still = was.TryGetValue(w.P.Id, out var p0) && p0 == at; was[w.P.Id] = at;
                        if (!still || g.BodyHeld(w.P.Id) || g.BodyMove(w.P.Id) != null) continue; // at rest: not on his way, not held, not making room
                        looks++; if (bi.Value.stop.StartsWith("beat:door:", StringComparison.Ordinal)) atDoors++;
                        if (DoorWays.In(ways, at, r.BodyDoorDepthCm, r.BodyDoorHalfCm, 0)) bad.Add($"{route} #{seed} {counts} {Report.Clock(g.Tick)}: {w.P.Id} at rest at {bi.Value.stop} stands in a doorway at {at}");
                    }
                });
            }
        Assert.True(atDoors > 0, $"guards stood at a door's post ({atDoors} of {looks} looks at rest)");
        Assert.True(bad.Count == 0, $"{bad.Count} looks: " + string.Join("; ", bad.Distinct().Take(5)));
    }
}
