// Brief 098 step 1b (Sky 2026-11-07: "people counter is for street"; who leaves: "same for now"): the people count is of people on the street,
// laws over seeds, never a pinned moment.
//    TodayIsTheStreet                 Population.Today's people number is the street's today (of the zone's 114), the floor the cast's street share
//    PeopleNGivesNOnTheStreet         asked N, the street as the game shows it (the crowd every tick, the cast's share) holds N on average: each seed
//                                     within 0.75 of N, the seeds' mean within 0.4 (asking fewer than the floor plays the floor)
//    TodaysNumberIsTodaysWorld        asking today's number plays the people file exactly (nobody added or taken)
//    FewerNeverRemovesTheCastNorTheIndoors   fewer never takes a tenet, nor anyone whose routine keeps them indoors all mission
//    FewerAndMoreArePrefixes          asking one fewer never brings anyone back; asking one more never takes anyone away
using System;
using System.Collections.Generic;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class DialStreetTests
{
    static Start With(int seed, int? people = null, int? guards = null) { var s = new Start { Seed = seed }; if (people.HasValue) s.Counts["people"] = people.Value; if (guards.HasValue) s.Counts["guards"] = guards.Value; return s; }
    static KindCensus Today => Population.Today(Repo.Inputs).Single(c => c.Kind == "people");
    static int Over => Game.T(Repo.Inputs.SiteData.Over);
    static PopulationRules Pop => Repo.Inputs.Population!;
    /// <summary>The street as the game shows it, on average over the mission's clock: the people of the kind among the crowd whose place is inside the
    /// window and out of doors (CrowdPos), stepping the game (no orders) while it runs and by routine after it ends, plus the cast's street share.</summary>
    static double Observed(Game g, out KindCensus c)
    {
        c = g.Census.SingleOrDefault(x => x.Kind == "people") ?? Today; long sum = 0;
        for (int t = 0; t < Over; t++)
        {
            if (!g.Over && g.Tick < t) g.Step();
            bool live = !g.Over && g.Tick == t;
            foreach (var w in g.Walkers) if (Population.KindOf(Pop, w.P.Job) == "people" && (live ? g.CrowdPos(w) : g.CrowdPos(w, t)) is Pt p && g.S.Grid.InGrid(p)) sum++;
        }
        return (double)sum / Over + c.CastStreet10 / 10.0;
    }

    [Fact]
    public void TodayIsTheStreet()
    {
        var t = Today; Assert.Equal("street", t.Measure);
        Assert.Equal(114, t.Total); Assert.Equal(t.Total, t.TotalToday); // everyone of the kind in the zone today, the cast included
        Assert.Equal((t.Street10 + 5) / 10, t.Today); Assert.InRange(t.Today, 10, 30); // the street's today, a few dozen at most of the zone's 114
        Assert.Equal((t.CastStreet10 + 5) / 10, t.Floor); Assert.True(t.Floor >= 1 && t.Floor < t.Today, "the fewest is the cast's own street time");
        var guards = Population.Today(Repo.Inputs).Single(c => c.Kind == "guards"); Assert.Equal("zone", guards.Measure); Assert.Equal(16, guards.Today); // the guards still count the zone
    }

    [Fact]
    public void PeopleNGivesNOnTheStreet()
    {
        int today = Today.Today, floor = Today.Floor;
        foreach (var n in new[] { today, 2 * today, today / 2, 0, 4 * today })
        {
            var seen = new List<double>(); int want = Math.Max(floor, n);
            foreach (var seed in new[] { 1, 2, 3, 4, 5, 6 })
            {
                var g = Engine.New(Repo.Inputs, With(seed, n)); double o = Observed(g, out var c);
                Assert.Equal(want, c.Count); Assert.InRange(o, want - 0.75, want + 0.75); Assert.InRange(c.Street10 / 10.0, want - 0.75, want + 0.75); // the street the census says it built is the street the game shows
                Assert.InRange(o, c.Street10 / 10.0 - 0.25, c.Street10 / 10.0 + 0.25);
                seen.Add(o);
            }
            Assert.InRange(seen.Average(), want - 0.4, want + 0.4);
        }
    }

    [Theory] [InlineData(0)] [InlineData(3)] [InlineData(11)]
    public void TodaysNumberIsTodaysWorld(int seed)
    {
        var plain = Engine.New(Repo.Inputs, With(seed)); var g = Engine.New(Repo.Inputs, With(seed, Today.Today));
        var c = g.Census.Single(x => x.Kind == "people"); Assert.Empty(c.Added); Assert.Empty(c.Removed); Assert.Equal(114, c.Total);
        Assert.Equal(plain.Walkers.Select(w => (w.P.Id, string.Join(",", w.From), string.Join(",", w.To))), g.Walkers.Select(w => (w.P.Id, string.Join(",", w.From), string.Join(",", w.To))));
    }

    [Fact]
    public void FewerNeverRemovesTheCastNorTheIndoors()
    {
        var casts = Repo.Inputs.SiteData.Cast.Select(c => c.Id).ToHashSet();
        foreach (var seed in new[] { 1, 7 })
            foreach (var n in new[] { 0, 1, 4, 9, Today.Today - 1 })
            {
                var g = Engine.New(Repo.Inputs, With(seed, n)); var c = g.Census.Single(x => x.Kind == "people");
                foreach (var role in new[] { "waitress", "driver", "guard", "councillor" }) Assert.NotNull(g.PersonByRole(role));
                Assert.Same(g.GuardP, g.Inez.Lover); Assert.Same(g.Inez, g.GuardP.Lover); // the tie the Handover stands on
                Assert.Empty(c.Removed.Intersect(casts));
                var plain = Engine.New(Repo.Inputs, With(seed));
                foreach (var id in c.Removed) { var w = plain.Walkers.Single(x => x.P.Id == id); Assert.True(Enumerable.Range(0, Over).Any(t => plain.CrowdPos(w, t) is Pt p && plain.S.Grid.InGrid(p)), $"{id} left, so their routine brought them out"); }
                var indoors = plain.Walkers.Where(w => w.P.Job != "guard" && !Enumerable.Range(0, Over / 10).Any(t => plain.CrowdPos(w, t * 10) is Pt p && plain.S.Grid.InGrid(p))).Select(w => w.P.Id).ToList();
                Assert.NotEmpty(indoors); Assert.All(indoors, id => Assert.Contains(g.Walkers, w => w.P.Id == id)); // whoever never comes out stays: the street is not theirs to change
                Assert.Equal(c.Total, 3 + g.Walkers.Count(w => w.P.Job != "guard")); // the zone's people: the cast's three and the crowd left
            }
    }

    [Fact]
    public void FewerAndMoreArePrefixes()
    {
        foreach (var seed in new[] { 2, 9 })
        {
            List<string> Removed(int n) => Engine.New(Repo.Inputs, With(seed, n)).Census.Single(x => x.Kind == "people").Removed;
            List<string> Added(int n) => Engine.New(Repo.Inputs, With(seed, n)).Census.Single(x => x.Kind == "people").Added;
            var before = Removed(Today.Today - 1);
            for (int n = Today.Today - 2; n >= 0; n -= 3) { var now = Removed(n); Assert.True(before.All(now.Contains), $"seed {seed}: asking {n} brings nobody back"); before = now; }
            var had = Added(Today.Today + 1);
            for (int n = Today.Today + 4; n <= Today.Today + 40; n += 6) { var now = Added(n); Assert.True(had.All(now.Contains), $"seed {seed}: asking {n} takes nobody away"); Assert.True(now.Count >= had.Count); had = now; }
        }
    }
}
