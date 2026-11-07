// Brief 098 step 0, the dial (GamePopulation.cs): laws that hold for every count and seed, never a pinned moment.
//    UnsetIsToday / TodayCountsPlayTheSameWorld   no counts: the start and every walker as before; counts at today's numbers: the same world, its own start hash
//    RemovalNeverTakesATenet / PostsStayWhole     fewer: the cast and the lover tie always stand, nobody keeps a tie to someone gone, a post is staffed all mission or not at all
//    AddedGuardsKeepTheirCheckIns                 an added guard is staff on a beat, his marks every check-in period from his shift's start, and a downed one misses his next
//    AddedPeopleReactToADrawnGunByNature          a passer-by who notices a drawn gun (the engine's own test) startles for their nature's time, remembers the face, then flees
//    AnAddedPersonBlocksASightline                an added passer-by alone on a clear line between two eyes blocks it, as the crowd does
//    AddingMovesNobodyAlreadyThere                more people never changes the ones already added, nor anyone else's draws
//    CountsReplayAndASmallSweepRunsClean          seed + counts + orders replay exactly; fewer, today, more and many more play to an end on several seeds
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class DialTests
{
    static readonly string Routes = System.IO.Path.Combine(Engine.FindRoot(AppContext.BaseDirectory), "contracts", "missions", "handover", "routes");
    static Script Route(string name) { var s = Script.Parse(File.ReadAllText(System.IO.Path.Combine(Routes, name + ".json"))); s.Expect = null; return s; }
    static Start With(int seed, int? guards = null, int? people = null)
    {
        var s = new Start { Seed = seed };
        if (guards.HasValue) s.Counts["guards"] = guards.Value; if (people.HasValue) s.Counts["people"] = people.Value;
        return s;
    }
    static Game Play(string route, int seed, int? guards = null, int? people = null)
    {
        var s = Route(route); s.Start.Seed = seed; if (guards.HasValue) s.Start.Counts["guards"] = guards.Value; if (people.HasValue) s.Start.Counts["people"] = people.Value;
        return Engine.Play(Repo.Inputs, s.Start, s.Orders, s.Until);
    }
    static int Over => Game.T(Repo.Inputs.SiteData.Over);
    static string[] CastIds => Repo.Inputs.SiteData.Cast.Select(c => c.Id).ToArray();

    [Fact]
    public void UnsetIsToday()
    {
        var plain = new Game(Repo.Inputs.Rules, Repo.Inputs.SiteData, new Start(), Repo.Inputs.Hash.ToString(System.Globalization.CultureInfo.InvariantCulture), Repo.Inputs.Crowd); // no dial rules at all
        var g = Engine.New(Repo.Inputs); // the dial's rules loaded, no counts set
        Assert.False(g.DialOn); Assert.Empty(g.Census);
        Assert.Equal(plain.StartHash, g.StartHash);
        Assert.DoesNotContain("counts", new Start().Canonical());
        Assert.Equal(Repo.Inputs.Crowd!.Walkers.Select(w => w.P.Id), g.Walkers.Select(w => w.P.Id));
        var today = Population.Today(Repo.Inputs);
        Assert.Equal(16, today.Single(c => c.Kind == "guards").Today); Assert.Equal(1, today.Single(c => c.Kind == "guards").Floor);
        Assert.Equal(114, today.Single(c => c.Kind == "people").Today); Assert.Equal(3, today.Single(c => c.Kind == "people").Floor);
    }

    [Theory]
    [InlineData("route-a", 0)] [InlineData("route-a", 5)] [InlineData("route-b", 9)] [InlineData("loud-fight", 2)]
    public void TodayCountsPlayTheSameWorld(string route, int seed)
    {
        var a = Play(route, seed); var b = Play(route, seed, 16, 114);
        Assert.True(b.DialOn); Assert.All(b.Census, c => { Assert.Empty(c.Added); Assert.Empty(c.Removed); });
        Assert.NotEqual(a.StartHash, b.StartHash); // the counts are hashed into the start
        Assert.Equal(a.Result, b.Result); Assert.Equal(a.FailCode, b.FailCode); Assert.Equal(a.Tick, b.Tick);
        Assert.Equal(a.Log.Select(l => (l.Tick, l.Code)), b.Log.Select(l => (l.Tick, l.Code)));
        Assert.Equal(a.Walkers.Select(w => (w.P.Id, w.St, a.CrowdPos(w))), b.Walkers.Select(w => (w.P.Id, w.St, b.CrowdPos(w))));
    }

    [Fact]
    public void RemovalNeverTakesATenet()
    {
        foreach (var guards in new[] { 0, 1, 2, 3, 9, 15 })
            foreach (var people in new[] { 0, 3, 4, 40, 113 })
            {
                var g = Engine.New(Repo.Inputs, With(7, guards, people));
                foreach (var role in new[] { "waitress", "driver", "guard", "councillor" }) Assert.NotNull(g.PersonByRole(role));
                Assert.Same(g.GuardP, g.Inez.Lover); Assert.Same(g.Inez, g.GuardP.Lover); // the tie the Handover stands on
                var gone = new HashSet<string>(g.Census.SelectMany(c => c.Removed));
                Assert.Empty(gone.Intersect(CastIds));
                Assert.All(g.Walkers, w => Assert.DoesNotContain(w.P.Id, gone));
                foreach (var c in g.Census) Assert.Equal(Math.Max(c.Floor, c.Asked!.Value), c.Count);
                Assert.Equal(g.Census.Single(c => c.Kind == "guards").Count, 1 + g.Walkers.Count(w => w.P.Job == "guard"));
                Assert.Equal(g.Census.Single(c => c.Kind == "people").Count, 3 + g.Walkers.Count(w => w.P.Job != "guard"));
                // nobody left keeps a tie to someone gone: who they'd tell, grieve for or miss is someone still in the zone
                foreach (var w in g.Walkers) foreach (var other in gone) Assert.DoesNotContain(other, g.TiesNow(w.P.Id).Select(t => t.with));
            }
    }

    [Fact]
    public void PostsStayWhole()
    {
        var site = Repo.Inputs.SiteData; var beside = Repo.Inputs.Crowd!.Beside;
        var doors = site.Refuges.Where(r => r.kind == "door").Select(r => r.id.Substring(5)).ToList();
        for (int guards = 1; guards <= 16; guards++)
        {
            var g = Engine.New(Repo.Inputs, With(0, guards));
            foreach (var door in doors)
            {
                var cover = new bool[Over];
                if (g.PostNow("guard", 0) is var gp && gp.HasValue && gp.Value.place == door) Array.Fill(cover, true); // the door guard holds his all mission
                foreach (var w in g.Walkers.Where(w => w.P.Job == "guard"))
                    foreach (var (place, from, to, outside) in g.StopsNow(w.P.Id)!)
                        if (outside && (place == door || (beside.TryGetValue(place, out var b) && b == door))) for (int t = Math.Max(0, from); t < Math.Min(Over, to); t++) cover[t] = true;
                int held = cover.Count(x => x);
                Assert.True(held == 0 || held == Over, $"guards {guards}: {door} is staffed {held} of {Over} ticks, never half");
            }
        }
        var two = Engine.New(Repo.Inputs, With(0, 2)); // the door guard and one more: the official entrance's pair is cut to one, who stands it all mission
        Assert.Single(two.Walkers, w => w.P.Job == "guard");
    }

    [Theory]
    [InlineData(0)] [InlineData(4)] [InlineData(31)]
    public void AddedGuardsKeepTheirCheckIns(int seed)
    {
        var g = Engine.New(Repo.Inputs, With(seed, 16 + 6));
        var added = g.Walkers.Where(w => w.Added).ToList(); Assert.Equal(6, added.Count);
        int period = Game.T(g.R.CheckInPeriodS);
        foreach (var w in added)
        {
            Assert.Equal("guard", w.P.Job); Assert.False(w.P.Civilian);
            var post = g.PostNow(w.P.Id, 0); Assert.True(post.HasValue, $"{w.P.Id} holds a post at 0:00"); Assert.StartsWith("beat:", post!.Value.place);
            Assert.True(post.Value.from <= 0 && post.Value.to > Over, "on shift for the whole mission");
            for (int t = 0, last = -1; t < Over; t += 50)
            {
                int m = g.NextMarkOf(w.P, t); Assert.True(m >= t && (m - post.Value.from) % period == 0, $"{w.P.Id}: a mark every check-in period from his shift's start");
                if (last >= 0 && m != last) Assert.Equal(period, m - last); last = m;
            }
            Assert.True(added.Count(x => x.P.Id != w.P.Id && x.Place[0] == w.Place[0]) == 0 || added.Count > 22, "one guard to a point before any shares");
        }
    }

    [Fact]
    public void ADownedAddedGuardMissesHisNextMark()
    {
        int missed = 0;
        foreach (var seed in new[] { 0, 4, 31, 9 })
        {
            var g = Engine.New(Repo.Inputs, With(seed, 16 + 6)); var x = g.Walkers.First(w => w.Added);
            g.RunTo(Game.T(20)); var at = g.CrowdPos(x)!.Value; g.Enforcer.Pos = g.Walk.NearestFree(at) ?? at; // the Enforcer at his elbow, where his beat has him
            var (ok, why) = g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "takedown", Target = x.P.Id, How = "knockout" }); Assert.True(ok, why);
            int mark = -1; for (int k = 0; k < Game.T(60) && !g.Over && mark < 0; k++) { g.Step(); if (x.P.Down) mark = g.NextMarkOf(x.P, g.Tick); }
            Assert.True(mark > 0, $"seed {seed}: he went down");
            g.RunTo(mark - 1); bool stillDown = x.P.Down && g.BodyOf(x.P) != null; g.RunTo(mark + 1);
            if (!stillDown) continue; // found and brought round before his mark (the patrol confirmed him): he has no mark to miss
            Assert.Contains(g.Log, l => l.Code == "post.missed" && l.Text.Contains(x.P.Id) && l.Tick == mark); missed++; // down at his mark: his post is reported empty, at the mark
        }
        Assert.True(missed >= 2, $"at least two seeds left him down at his mark ({missed})");
    }

    [Theory]
    [InlineData(0)] [InlineData(3)] [InlineData(12)] [InlineData(40)]
    public void AddedPeopleReactToADrawnGunByNature(int seed)
    {
        int seen = 0;
        foreach (var at in new[] { 40, 120, 200 })
        {
            var g = Engine.New(Repo.Inputs, With(seed, null, 114 + 80)); g.RunTo(Game.T(at));
            Assert.All(g.Walkers.Where(w => w.Added), w => { Assert.True(w.P.Civilian); Assert.Equal("visitor", w.P.Job); Assert.Contains(w.P.Temperament, new[] { "idealist", "mercenary", "fearful", "loyal" }); });
            var op = g.Ops.First(o => o.Gun.W != null && o.Id != OpId.Enforcer);
            foreach (var w in g.Walkers.Where(w => w.Added && w.St.Length == 0))
            {
                var pos = g.CrowdPos(w); var f = g.FacingNow(w.P.Id); if (!pos.HasValue || !f.HasValue) continue;
                long len = Math.Max(1, G.Isqrt(G.D2(new Pt(0, 0), f.Value)));
                var spot = g.Walk.NearestFree(new Pt(pos.Value.X + (int)(f.Value.X * 400 / len), pos.Value.Y + (int)(f.Value.Y * 400 / len)));
                if (!spot.HasValue || !g.NoticesNow(w.P.Id, "weapon-drawn", spot.Value)) continue; // only who would notice it: the engine's own test
                op.Pos = spot.Value;
                var (ok, why) = g.Issue(new Order { Tick = g.Tick, Op = Op.Name(op.Id), Do = "draw" }); Assert.True(ok, why);
                g.Step();
                if (!w.P.Memory.Exists(m => m.Act == "saw-weapon-drawn")) continue; // walked out of it in the tick: no witness, nothing owed
                Assert.True(w.St == "startled", $"{w.P.Id} ({w.P.Temperament}) saw the gun drawn and startles");
                Assert.Equal(w.P.Temperament == "fearful" ? g.R.CrowdStartleFearfulTicks : g.R.CrowdStartleTicks, w.Until - w.RunStart); // the startle is their nature's
                Assert.Contains(w.P.Memory, m => m.Act == "saw-weapon-drawn" && m.By == Op.Name(op.Id) && m.Witnessed); // they remember the face
                g.RunTo(w.Until + 1); Assert.True(w.St == "fleeing" || w.St == "sheltered", $"{w.P.Id} then runs for shelter");
                seen++; break;
            }
        }
        Assert.True(seen >= 2, $"seed {seed}: at least two passers-by were in a spot to see a gun drawn ({seen})");
    }

    [Fact]
    public void AnAddedPersonBlocksASightline()
    {
        var g = Engine.New(Repo.Inputs, With(6, null, 114 + 60)); g.RunTo(Game.T(90)); int lines = 0;
        foreach (var w in g.Walkers.Where(w => w.Added))
        {
            if (!(g.CrowdPos(w) is Pt at) || g.Walkers.Any(o => o != w && g.CrowdPos(o) is Pt q && G.Within(q, at, 400))) continue; // alone on their bit of street
            Pt a = new Pt(at.X - 600, at.Y), b = new Pt(at.X + 600, at.Y); if (!g.S.Sight(a, b, g.R.Eye)) continue; // a line nothing solid cuts
            Assert.True(g.CrowdBlocks(a, b), $"{w.P.Id} on the line between two eyes blocks it"); lines++;
        }
        Assert.True(lines >= 3, $"some added people stood alone on a clear line ({lines})");
    }

    [Fact]
    public void AddingMovesNobodyAlreadyThere()
    {
        var few = Engine.New(Repo.Inputs, With(17, 16 + 3, 114 + 10)); var many = Engine.New(Repo.Inputs, With(17, 16 + 12, 114 + 60));
        foreach (var w in few.Walkers)
        {
            var m = many.Walkers.Single(x => x.P.Id == w.P.Id);
            Assert.Equal(w.P.Temperament, m.P.Temperament);
            Assert.Equal(few.StopsNow(w.P.Id), many.StopsNow(w.P.Id)); // the people file's routines, every added guard's beat and the first ten passers-by's visits, whoever else came
        }
        var fewAdded = few.Walkers.Where(w => w.Added && w.P.Job != "guard").Select(w => w.P.Id).ToList();
        Assert.Equal(fewAdded, many.Walkers.Where(w => w.Added && w.P.Job != "guard").Take(fewAdded.Count).Select(w => w.P.Id)); // the first ten are the same ten
        var plain = Engine.New(Repo.Inputs, With(17)); Assert.Equal(plain.PatrolPhase, many.PatrolPhase); // and nobody else's draws moved
    }

    [Fact]
    public void CountsReplayAndASmallSweepRunsClean()
    {
        var a = Play("route-a", 8, 32, 228); var b = Play("route-a", 8, 32, 228); var c = Play("route-a", 8, 33, 228);
        Assert.Equal(a.Checkpoints, b.Checkpoints); Assert.Equal(a.Checkpoints, Engine.Replay(Repo.Inputs, a).Checkpoints);
        Assert.NotEqual(a.StartHash, c.StartHash);
        var saved = Script.Parse(Script.Write(a, Repo.Inputs.Hash)); Assert.Equal(32, saved.Start.Counts["guards"]); Assert.Equal(228, saved.Start.Counts["people"]); // the counts are saved with the run
        Assert.Equal("", saved.Verify(Engine.Play(Repo.Inputs, saved.Start, saved.Orders, saved.Until), Repo.Inputs.Hash)); // and a saved run replays to its own record
        var outcomes = new List<string>();
        foreach (var route in new[] { "route-a", "route-b", "loud-fight" })
            foreach (var seed in new[] { 1, 2, 3 })
                foreach (var (guards, people) in new[] { (8, 57), (16, 114), (32, 228), (64, 456) })
                {
                    var g = Play(route, seed, guards, people);
                    Assert.NotEqual("engine-error", g.FailCode); Assert.True(g.Over || g.Tick >= Over, $"{route} #{seed} {guards}/{people} plays to an end");
                    outcomes.Add($"{route}#{seed} {guards}/{people}: {g.Result} {g.FailCode}");
                }
        Assert.Equal(36, outcomes.Count);
    }
}
