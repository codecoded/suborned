using System;
using Suborned.Sim;
using Xunit;

public class RivalResponseTests
{
    static (Game g, CrewMember rival) Encounter(int distance, bool behind = false)
    {
        var g = Engine.New(Repo.Inputs, new Start { Crowd = false });
        foreach (var op in g.Ops) { op.Walk = null; if (op.Id != OpId.Enforcer) op.Inside = true; }
        var rival = g.Crew[0];
        rival.Mode = CrewMode.Posted; rival.Post = "front";
        rival.Place = g.Enforcer.Pos; rival.Gun.Health = rival.Gun.Max = 10000;
        var at = rival.Place;
        var spot = g.Walk.Nearest(at + new Pt(distance, 0), p =>
            Math.Abs(G.Dist(at, p) - distance) <= 20 && g.S.Sight(at, p, g.R.Eye), distance * 2);
        Assert.NotNull(spot);
        // Discard the initial placement cache when teleporting a test actor; otherwise
        // the body-spacing law eases him back through his original spawn position.
        g.Enforcer.Pos = spot!.Value; g.Enforcer.Body = default; g.Enforcer.Gun.Health = g.Enforcer.Gun.Max = 10000;
        rival.Base = behind ? at - spot.Value : spot.Value - at;
        g.RivalsDrawn = true;
        return (g, rival);
    }

    static void Fire(Game g, CrewMember rival)
    {
        var answer = g.Issue(new Order { Op = "enforcer", Do = "fire", Target = rival.Id, Force = true });
        Assert.True(answer.ok, answer.why);
        while (!g.Enforcer.Gun.Fired && !g.Over && g.Tick < Game.T(5)) g.Step();
        Assert.Contains(g.Log, l => l.Code.StartsWith("shot.") && l.Text.StartsWith("enforcer fires on " + rival.Id));
        g.Step();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void StreetRivalReturnsFireAndShowsHisWeapon(bool behind)
    {
        var (g, rival) = Encounter(700, behind);
        if (behind) Assert.False(g.MemberWatching(rival, g.Enforcer.Pos));
        Fire(g, rival);
        g.RunTo(g.Tick + Game.T(5));
        Assert.Contains(g.Log, l => l.Code.StartsWith("shot.") && l.Text.StartsWith(rival.Id + " fires on enforcer"));
        Assert.True(rival.Gun.Drawn);
        Assert.NotEmpty(g.FireCauses(rival.Id, OpId.Enforcer));
    }

    [Fact]
    public void RivalRespondsToAnAttackerBeyondHisEffectiveRing()
    {
        var (g, rival) = Encounter(1550);
        Assert.True(G.Dist(rival.Place, g.Enforcer.Pos) > rival.Gun.W!.EffCm);
        Fire(g, rival);
        g.RunTo(g.Tick + Game.T(5));
        Assert.Contains(g.Log, l => l.Code.StartsWith("shot.") && l.Text.StartsWith(rival.Id + " fires on enforcer"));
    }

    [Fact]
    public void WalkingRivalsInterruptTheirErrandAndResumeFromContact()
    {
        var (g, first) = Encounter(700, true);
        var mate = g.Crew[1];
        var dest = g.Walk.NearestFree(first.Place + new Pt(0, 1800))!.Value;
        void Send(CrewMember m, Pt from)
        {
            m.Mode = CrewMode.Walking; m.Place = from; m.Goal = "front";
            m.Walk = new Suborned.Sim.Path(g.Walk.Route(from, dest)!); m.WalkStart = g.Tick; m.Speed = 120;
        }
        Send(mate, g.Walk.NearestFree(first.Place + new Pt(150, 0))!.Value);
        Send(first, first.Place);
        Fire(g, first);
        g.Advance(Game.T(2));
        foreach (var man in new[] { first, mate })
        {
            Assert.Equal("enforcer", man.Gun.Target);
            Assert.True(man.Disp?.Contact);
            Assert.Equal(dest, man.Walk!.End);
        }
        Assert.Contains("crewmate", g.FireCauses(mate.Id, OpId.Enforcer));
        var hold = g.MemberPos(first, g.Tick);
        g.Advance(Game.T(3));
        Assert.True(G.Within(hold, g.MemberPos(first, g.Tick), 60));
        Assert.Contains(g.Log, l => l.Text.StartsWith(mate.Id + " fires on enforcer"));
        g.Enforcer.Inside = true;
        g.Advance(Game.T(g.R.EngageLapseS + 2));
        Assert.Null(first.Disp);
        Assert.Equal("front", first.Goal);
        Assert.True(G.Dist(g.MemberPos(first, g.Tick), dest) < G.Dist(hold, dest));
    }

    [Fact]
    public void RivalBlockerDropsHisRoadApproachOnContact()
    {
        var (g, m) = Encounter(700);
        var end = g.Walk.NearestFree(m.Place + new Pt(0, 1500))!.Value;
        var h = new Hostile { Id = m.Id, At = end, Home = end, Present = true, Blocker = true,
            Speed = 120, Run = new Suborned.Sim.Path(g.Walk.Route(m.Place, end)!), Gun = m.Gun };
        m.Fight = true; m.H = h; h.Gun.St = "alerted"; h.Gun.Until = g.Tick;
        g.Hostiles.Add(h);
        g.Step(); Assert.Equal("engage", h.Gun.St); Assert.Equal("enforcer", h.Gun.Target);
        Assert.False(h.Run?.End == end);
    }

    [Fact]
    public void NeutralPasserByDoesNotProvokeARival()
    {
        var (g, rival) = Encounter(700);
        g.RunTo(Game.T(5));
        Assert.False(rival.Gun.Fired);
        Assert.False(rival.Gun.Drawn);
        Assert.Empty(g.FireCauses(rival.Id, OpId.Enforcer));
    }

    [Fact]
    public void FinalFightRivalDrawsAsHeAcquiresATarget()
    {
        var (g, rival) = Encounter(700);
        var h = new Hostile { Id = rival.Id, At = rival.Place, Home = rival.Place, Present = true, Gun = rival.Gun };
        rival.Fight = true; rival.H = h; rival.Gun.St = "engage";
        g.Hostiles.Add(h);
        g.Step();
        Assert.Equal("enforcer", rival.Gun.Target);
        Assert.True(rival.Gun.Drawn);
        Assert.False(rival.Gun.Fired); // The host can show aim during the reaction beat, before the first recoil.
    }

    [Fact]
    public void BrokenCrewDoesNotReturnFireOrDrawAgain()
    {
        var (g, rival) = Encounter(700);
        g.CrewBrokenPre = true;
        Fire(g, rival);
        g.RunTo(g.Tick + Game.T(3));
        Assert.False(rival.Gun.Fired);
        Assert.False(rival.Gun.Drawn);
    }

    [Fact]
    public void RivalDoesNotFireBeyondHisWeaponRange()
    {
        var (g, rival) = Encounter(1700);
        g.Enforcer.Gun.Drawn = true;
        Assert.True(G.Dist(rival.Place, g.Enforcer.Pos) > rival.Gun.W!.MaxCm);
        g.RunTo(Game.T(3));
        Assert.False(rival.Gun.Fired, string.Join(" | ", g.Log.ConvertAll(l => l.Text)) + $" current distance={G.Dist(g.MemberPos(rival, g.Tick), g.Enforcer.Pos)} rival={g.MemberPos(rival,g.Tick)} raw={rival.Place} op={g.Enforcer.Pos} mode={rival.Mode}");
        Assert.Equal("", rival.Gun.Target);
    }

    [Fact]
    public void RivalDoesNotFireThroughAnOccludingWall()
    {
        var (g, rival) = Encounter(700);
        var at = rival.Place;
        var hidden = g.Walk.Nearest(at, p => G.Dist(at, p) <= rival.Gun.W!.EffCm
            && !g.S.Sight(at, p, g.R.Eye), rival.Gun.W!.EffCm);
        Assert.NotNull(hidden);
        Fire(g, rival);
        g.Enforcer.Pos = hidden!.Value; g.Enforcer.Body = default; g.Enforcer.Gun.Order = "";
        rival.Base = hidden.Value - at;
        Assert.False(g.MemberWatching(rival, hidden.Value));
        g.Step(); // Even a recent attacker cannot be aimed at through a wall.
        Assert.DoesNotContain(g.ShotsThisTick, s => s.By == rival.Id);
        Assert.Equal("", rival.Gun.Target);
    }
}
