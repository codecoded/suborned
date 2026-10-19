// Assignment 5 acceptance checks, authored independently before the goal agent writes the feature.
// GDD §05 bodies: a container's service time is learnable by Watch. Reuse the existing Scout Watch
// duration/reach (rulebook sight.watch); no new balance numbers. Cancel follows rulebook cancel.watch.
using System;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class GoalWatchTests
{
    static Game New() => Engine.New(Repo.Inputs);
    static StashPlace Bin(Game g) => g.S.Stashes.First(s => s.Kind == "container");
    static Answer Order(Game g, string verb, string target = "", string op = "scout") =>
        g.Issue(new Order { Tick = g.Tick, Op = op, Do = verb, Target = target });

    // Real orders, rather than teleporting a fixture, so every learning run can replay unmodified.
    static Game BesideBin()
    {
        var g = New();
        var moved = g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "move", Point = Bin(g).Stand });
        Assert.True(moved.ok, moved.why);
        var scout = g.OpOf(OpId.Scout);
        while (scout.Walk != null && !g.Over && g.Tick < Game.T(180)) g.Step();
        Assert.False(g.Over, g.FailText);
        Assert.Null(scout.Walk);
        return g;
    }

    static void Watch(Game g)
    {
        var a = Order(g, "watch", Bin(g).Id);
        Assert.True(a.ok, a.code + ": " + a.why);
    }

    [Fact]
    public void ExistingServiceClockIsDeterministicAndInitiallyUnknown()
    {
        var g = New(); var id = Bin(g).Id;
        var before = Hash.State(g);
        int period = Game.T(g.R.ServicePeriodS);
        int phase = (int)(Streams.Fnv1a32(id) % (uint)period);
        Assert.Equal((phase, false), g.NextRound(id));
        Assert.Equal(g.NextRound(id), New().NextRound(id));
        Assert.Equal(before, Hash.State(g));
        Assert.Equal((-1, false), g.NextRound("no-such-container"));
        Assert.Equal((-1, false), g.NextRound(g.S.Stashes.First(s => s.Kind == "door").Id));
    }

    [Fact]
    public void CompletedScoutWatchTeachesTheExistingRoundAtItsFullDuration()
    {
        var g = BesideBin(); var id = Bin(g).Id; int next = g.NextRound(id).tick;
        long cash = g.Cash, trail = g.Trail; int began = g.Tick;
        Watch(g);
        Assert.Equal(began + Game.T(g.R.SightWatchS), g.OpOf(OpId.Scout).BusyEnd);
        Assert.False(g.NextRound(id).known);
        g.Advance(Game.T(g.R.SightWatchS) - 1);
        Assert.False(g.NextRound(id).known);
        g.Step();
        Assert.Null(g.OpOf(OpId.Scout).Busy);
        Assert.True(g.NextRound(id).known);
        Assert.Equal(next, g.NextRound(id).tick);
        Assert.Equal(cash, g.Cash);
        Assert.Equal(trail, g.Trail);
        Assert.False(g.Over, g.FailText);
    }

    [Fact]
    public void CancellingWatchLearnsNothingAndAFreshCompleteWatchCanTeach()
    {
        var g = BesideBin(); var id = Bin(g).Id;
        Watch(g);
        g.Advance(Game.T(g.R.SightWatchS) / 2);
        Assert.True(Order(g, "cancel").ok);
        Assert.False(g.NextRound(id).known);
        g.Advance(Game.T(g.R.SightWatchS));
        Assert.False(g.NextRound(id).known);
        Watch(g);
        g.RunTo(g.OpOf(OpId.Scout).BusyEnd);
        Assert.True(g.NextRound(id).known);
    }

    [Fact]
    public void KnownContainerRejectsARepeatWithoutTimeOrPrice()
    {
        var g = BesideBin(); var id = Bin(g).Id;
        Watch(g); g.RunTo(g.OpOf(OpId.Scout).BusyEnd);
        var before = Hash.State(g); int tick = g.Tick; long cash = g.Cash, trail = g.Trail;
        Assert.Equal("already-watched", g.LearnQuery("scout", "watch", id).code);
        Assert.Equal(before, Hash.State(g));
        Assert.Equal("already-watched", Order(g, "watch", id).code);
        Assert.Equal(tick, g.Tick);
        Assert.Equal(cash, g.Cash);
        Assert.Equal(trail, g.Trail);
        Assert.Null(g.OpOf(OpId.Scout).Busy); // the refused order itself is still recorded (and hashed)
    }

    [Fact]
    public void QueryAgreesWithIssueAndDoesNotTeachOrChangeTheState()
    {
        var g = BesideBin(); var id = Bin(g).Id; var before = Hash.State(g);
        var query = g.LearnQuery("scout", "watch", id);
        Assert.True(query.ok, query.code + ": " + query.why);
        Assert.Equal(before, Hash.State(g));
        Assert.False(g.NextRound(id).known);
        Assert.Equal(query.ok, Order(g, "watch", id).ok);
    }

    [Fact]
    public void ContainersRequireScoutAndAnActualContainerTarget()
    {
        var g = BesideBin(); var id = Bin(g).Id;
        foreach (var op in new[] { "fixer", "tech", "enforcer" })
        {
            Assert.Equal("only-scout", g.LearnQuery(op, "watch", id).code);
            Assert.Equal("only-scout", Order(g, "watch", id, op).code);
        }
        foreach (var target in new[] { "no-such-container", g.S.Stashes.First(s => s.Kind == "door").Id })
        {
            Assert.Equal("target-unknown", g.LearnQuery("scout", "watch", target).code);
            Assert.Equal("target-unknown", Order(g, "watch", target).code);
        }
        Assert.False(g.NextRound(id).known);
    }

    [Fact]
    public void ContainerWatchUsesTheExistingWatchReach()
    {
        var g = New(); var bin = Bin(g); var scout = g.OpOf(OpId.Scout);
        // Query-only geometry fixture; no movement or replay claim for this fixture.
        scout.Pos = new Pt(bin.Stand.X + g.R.SightReachCm + 100, bin.Stand.Y);
        var before = Hash.State(g);
        Assert.Equal("too-far", g.LearnQuery("scout", "watch", bin.Id).code);
        Assert.Equal(before, Hash.State(g));
        Assert.Equal("too-far", Order(g, "watch", bin.Id).code);
        Assert.False(g.NextRound(bin.Id).known);
    }

    [Fact]
    public void LearnedRoundSurvivesReplayFromOnlyTheRecordedOrders()
    {
        var g = BesideBin(); var id = Bin(g).Id;
        Watch(g); g.RunTo(g.OpOf(OpId.Scout).BusyEnd);
        g.Advance(Game.T(2)); g.Finish();
        Assert.True(g.NextRound(id).known);
        var replay = Engine.Replay(Repo.Inputs, g);
        Assert.True(replay.NextRound(id).known);
        Assert.Equal(g.NextRound(id), replay.NextRound(id));
        Assert.Equal(g.Checkpoints, replay.Checkpoints);
        Assert.Equal(Hash.State(g), Hash.State(replay));
        // Knowledge belongs to the cell, and the UI's repeated reads remain pure.
        var before = Hash.State(g);
        for (int n = 0; n < 10; n++) Assert.True(g.NextRound(id).known);
        Assert.Equal(before, Hash.State(g));
    }
}
