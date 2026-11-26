using System.Linq;
using Suborned.Sim;
using Xunit;

public sealed class ContactIndexTests
{
    static Game StartDistrict() => Engine.New(Repo.Inputs, new Start { Seed = 9, District = true });
    static void Issue(Game g, string verb, int who, string to = "")
    {
        var answer = g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = verb, Target = g.ContactId(who), To = to });
        Assert.True(answer.ok, answer.why);
    }

    [Fact]
    public void StreetMeetingsAndSecondHandNamesSurviveReplay()
    {
        var g = StartDistrict();
        var d = g.District!;
        var job = d.Jobs[0];
        Assert.False(g.ContactKnown(job.A));
        Assert.False(g.ContactKnown(job.B));
        Assert.False(g.ContactKnown(job.C));
        Assert.False(g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "district-meet", Target = g.ContactId(job.A) }).ok);
        Assert.Equal("", g.ContactNewsKey(job.A));
        Issue(g, "district-visit", job.A);
        for (int n = 0; g.Ops[0].Walk != null && n < 24000; n++) g.Step();
        Issue(g, "district-meet", job.A);
        Assert.True(g.ContactKnown(job.A));
        var before = g.ContactNewsKey(job.A);
        Issue(g, "district-help", job.A, job.Key);
        Assert.True(d.Introduced[job.B]);
        Assert.True(g.ContactKnown(job.B));
        Assert.NotEqual(before, g.ContactNewsKey(job.A));
        Assert.True(Enumerable.Range(0, g.ContactCount).Count(g.ContactKnown) < g.ContactCount);
        var script = Script.Parse(Script.Write(g, Repo.Inputs.Hash));
        var replay = Engine.Play(Repo.Inputs, script.Start, script.Orders, script.Until);
        Assert.Equal(g.ClosingHash(), replay.ClosingHash());
        Assert.Equal(Enumerable.Range(0, g.ContactCount).Select(g.ContactNewsKey),
            Enumerable.Range(0, replay.ContactCount).Select(replay.ContactNewsKey));
    }

    [Fact]
    public void TheHandoverLoversShareAnEarnedConnection()
    {
        var g = StartDistrict();
        int a = g.District!.Map.People.Length + g.People.IndexOf(g.Inez);
        int b = g.District.Map.People.Length + g.People.IndexOf(g.GuardP);
        Assert.Equal(g.Inez.Id, g.ContactId(a));
        Assert.Equal(g.GuardP.Name, g.ContactName(b));
        Assert.Equal(g.District.Map.People.Length + g.People.Count, g.ContactCount);
        var before = g.ContactNewsKey(a);
        g.Inez.Learn("secret");
        Assert.True(g.ContactKnown(a));
        Assert.True(g.ContactKnown(b));
        Assert.NotEqual(before, g.ContactNewsKey(a));
    }
}
