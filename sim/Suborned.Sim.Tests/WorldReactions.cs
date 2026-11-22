using Suborned.Sim;
using Xunit;

public class WorldReactions
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void OfficersRecoverOnlyExposedDeadAfterTheStreetQuiets(bool dead, bool stashed, bool recover)
    {
        var g = Engine.New(Repo.Inputs, new Start { Crowd = false });
        var p = g.Inez;
        var at = g.OpOf(OpId.Scout).Pos;
        var a = new TdAct { Id = g.Acts.Count, P = p, Victim = p.Id, At = at, Scene = at, Landed = true, Kill = dead };
        var b = new Body { Id = p.Id, P = p, Act = a.Id, Dead = dead, At = at,
            Stash = stashed ? g.S.Stashes[0].Id : "", WakeAt = int.MaxValue };
        p.Down = true; g.Acts.Add(a); g.Bodies.Add(b);
        g.Advance(Game.T(16));
        Assert.False(b.Recovered);
        var officer = new Unit { Id = "test-officer", Dest = at, St = "hold", Until = int.MaxValue, Gun = new Gun("test-officer") };
        g.Units.Add(officer);
        g.Advance(Game.T(8));
        Assert.False(b.Recovered);
        g.LastShotTick = g.Tick;
        g.Advance(Game.T(g.R.CrowdQuietS + 10));
        Assert.False(b.Recovered);
        g.Advance(Game.T(6));
        Assert.Equal(recover, b.Recovered);
        Assert.Same(a, g.Acts[b.Act]);
        Assert.Equal(!stashed && !recover, b.Open);
        Assert.Equal(recover ? 1 : 0, g.Log.FindAll(x => x.Code == "body.recovered").Count);
    }
}
