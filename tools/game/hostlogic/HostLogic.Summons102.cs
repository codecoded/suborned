// Brief 102, Fable's findings on the refused call (briefs/102/fable/notes.md, "Still for the host"; text.json events summon-refused: "Inez answers the Fixer;
// the guard, if he is within earshot"): the contact answers the Fixer, and her tie only when he hears it (HostFeed.cs TieHears). Down, he says nothing; on his
// feet and busy (come round, in a fight, away on her last call) he still answers her, as at his door; off the street or too far, out of earshot, he says
// nothing; and on the Fixer's call to the turned guard, Inez's line is her reply to what she hears, so it plays only when she stands within earshot of him.
// Real engine states throughout, but two: the fight's flag and the off-street code, which the Handover never gives (the guard's pose is null only when he
// is down, and Inez always has her post), so that case calls the card with the code.
using System.IO;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using Order = Suborned.Sim.Order;

public class Summons102
{
    /// <summary>A host on a fresh run whose game starts with these roles turned (as a scenario's start fixes them), on these inputs, else the player's.</summary>
    static HandoverHost Turned(Engine.Inputs inputs, params string[] roles)
    {
        var h = Host.Fresh(); h.controls.conversations = ConversationScenes.Never; Host.Set(h, "HintsOn", false); // no cutaway holds a later order (as Why100.Play)
        var s = new Start(); foreach (var r in roles) s.Turned.Add(r);
        Host.Set(h, "Game", Engine.New(inputs ?? h.PlayInputs, s)); Host.Tick(h); return h;
    }
    /// <summary>The Fixer's call on `role` through the host's order path (as a click gives it), refused: the code, and the card it made.</summary>
    static (string code, HandoverHost.FeedCard card) Refused(HandoverHost h, string role)
    {
        var q = h.Game.CallQuery(role); Assert.That(q.ok, Is.False, $"the call on {role} was taken at {HandoverHost.MissionClock(h.Game.Tick)}");
        int n = h.Feed.Count; h.Issue(new Order { Op = "fixer", Do = "call", Target = role });
        var c = h.Feed.Skip(n).LastOrDefault(x => x.Kind == "call" && x.Tone == HandoverHost.Feedback.Refused);
        Assert.That(c, Is.Not.Null, $"no card for the refused call ({q.code})"); return (q.code, c);
    }
    static string Who(HandoverHost.FeedCard c) => string.Join(",", c.Lines.Select(l => l.Speaker));
    /// <summary>The line a speaker said on the card is one of Fable's for the row.</summary>
    static void Said(HandoverHost h, HandoverHost.FeedCard c, string speaker, string code)
    {
        var rows = h.Text.Lookup(new PlayText.Query { Event = "summon-refused", Speaker = speaker, Outcome = code })?.Lines; Assert.That(rows, Is.Not.Null.And.Not.Empty, $"text.json has no {speaker} row for {code}");
        Assert.That(rows, Does.Contain(c.Lines.First(l => l.Speaker == speaker).Text), $"the {speaker}'s line on the card");
    }
    static int Apart(Game g, Person a, Person b) => G.Dist(g.PersonPos(a).Value, g.PersonPos(b).Value);
    static void Free(HandoverHost h) => Assert.That(Host.Until(h, () => h.Game.Ops[(int)OpId.Fixer].Busy == null, 600), "the Fixer stays busy");

    /// <summary>1. Route G's knockout from beside, with Inez turned: while he is down her call card has her line and nothing of his; once he comes
    /// round (on his feet, walking back to his door) he answers her again.</summary>
    [Test] public void ADownGuardSaysNothingAndAnswersAgainOnceHeComesRound()
    {
        var h = Turned(null, "waitress"); var g = h.Game;
        var sc = Script.Parse(File.ReadAllText(System.IO.Path.Combine(Root.Path, "contracts/missions/handover/routes/route-g.json")));
        foreach (var o in sc.Orders.Where(x => x.Tick <= 1510).OrderBy(x => x.Tick)) { if (o.Tick > g.Tick) h.RunTo(o.Tick); if (o.Do is "watch" or "listen" or "turn") h.GiveScripted(o); else h.Issue(o); }
        Assert.That(Host.Until(h, () => g.GuardDown, 300), $"route G's knockout never landed: {h.Status}");
        h.Issue(new Order { Op = "enforcer", Do = "move", Point = new Pt(5650, -4750) }); // back where he waited, out of sight

        var (code, down) = Refused(h, "waitress");
        Assert.That(code, Is.EqualTo("summon-busy")); Assert.That(Who(down), Is.EqualTo("waitress"), "a man who is down can't answer"); Said(h, down, "waitress", code);

        Assert.That(Host.Until(h, () => !g.GuardDown && g.GuardHeld || g.Over, 2400), $"he never came round: {h.Status}"); Assert.That(g.Over, Is.False, $"{g.Result} {g.FailCode}");
        var (code2, up) = Refused(h, "waitress");
        Assert.That(code2, Is.EqualTo("summon-busy")); Assert.That(Who(up), Is.EqualTo("waitress,guard"), "come round, he answers her"); Said(h, up, "guard", code2);
    }

    /// <summary>1. Busy on his feet: away on her last call (walking over), and in a fight (the engine's own flag, as its summons test sets its states).</summary>
    [Test] public void ABusyGuardOnHisFeetStillAnswers()
    {
        var h = Turned(null, "waitress"); var g = h.Game;
        h.GiveScripted(new Order { Op = "fixer", Do = "call", Target = "waitress" }); Assert.That(Host.Until(h, () => g.LureTick >= 0, 60), "the call never pulled him");
        Free(h); Assert.That(g.GuardPose()?.phase, Is.EqualTo("walking to café"));
        var (code, away) = Refused(h, "waitress"); Assert.That(code, Is.EqualTo("summon-busy")); Assert.That(Who(away), Is.EqualTo("waitress,guard")); Said(h, away, "guard", code);

        var f = Turned(null, "waitress"); f.Game.GuardEngaged = true;
        var (code2, fight) = Refused(f, "waitress"); Assert.That(code2, Is.EqualTo("summon-busy")); Assert.That(Who(fight), Is.EqualTo("waitress,guard"), "in a fight"); Said(f, fight, "guard", code2);
    }

    /// <summary>At his door after his one visit, her call reaches him as the call scene has it: he answers (came-once; unchanged).</summary>
    [Test] public void AtHisDoorHeStillAnswersHer()
    {
        var h = Turned(null, "waitress"); var g = h.Game;
        h.GiveScripted(new Order { Op = "fixer", Do = "call", Target = "waitress" }); Assert.That(Host.Until(h, () => g.LureTick >= 0, 60), "the call never pulled him");
        Assert.That(Host.Until(h, () => g.GuardAtDoor || g.Over, 3000), "he never got back to his door"); Free(h);
        Assert.That(Apart(g, g.Inez, g.GuardP), Is.GreaterThan(g.R.WitnessRangeCm), "the café is beyond earshot of his door: her call is what reaches him");
        var (code, card) = Refused(h, "waitress"); Assert.That(code, Is.EqualTo("summon-came-once")); Assert.That(Who(card), Is.EqualTo("waitress,guard")); Said(h, card, "guard", code);
    }

    /// <summary>3. Too far (a city where his walk is beyond summon.walkMax, as the engine's own test makes it): out of earshot at his door, he says nothing.</summary>
    [Test] public void TooFarTheGuardSaysNothing()
    {
        var i = Host.Fresh().PlayInputs; const string at90 = "\"summon-walk-max-s\": { \"v\": 90,", at30 = "\"summon-walk-max-s\": { \"v\": 30,";
        Assert.That(i.Rulebook, Does.Contain(at90), "the rulebook's summon-walk-max-s moved: fix this case");
        var far = Engine.FromTexts(i.Rulebook.Replace(at90, at30), i.PeopleRules, i.Site, i.People, i.Provisional, i.Takedown, i.Rounds);
        var h = Turned(far, "waitress"); var g = h.Game;
        Assert.That(Apart(g, g.Inez, g.GuardP), Is.GreaterThan(g.R.WitnessRangeCm), "she is out of his earshot");
        var (code, card) = Refused(h, "waitress"); Assert.That(code, Is.EqualTo("summon-too-far")); Assert.That(Who(card), Is.EqualTo("waitress"), "he can't hear her"); Said(h, card, "waitress", code);
    }

    /// <summary>3. Off the street (a code the Handover never gives, so the card is called with it): with him at his door out of earshot, he says nothing.</summary>
    [Test] public void OffTheStreetOutOfEarshotTheGuardSaysNothing()
    {
        var h = Turned(null, "waitress"); var g = h.Game; Assert.That(g.GuardAtDoor && Apart(g, g.Inez, g.GuardP) > g.R.WitnessRangeCm);
        int n = h.Feed.Count;
        Host.Call(h, "FeedSummonRefused", "waitress", new Order { Op = "fixer", Do = "call", Target = "waitress" }, new Answer { ok = false, code = "summon-off-street", why = "he isn't on the street" });
        var c = h.Feed.Skip(n).Single(); Assert.That(Who(c), Is.EqualTo("waitress")); Said(h, c, "waitress", "summon-off-street");
    }

    /// <summary>2. The Fixer calls the turned guard: he answers; his tie Inez isn't on that call, so her line is her reply to what she hears. At his door,
    /// with her at the café, she can't hear him and says nothing; beside her at the café (on her own call), she replies.</summary>
    [Test] public void OnTheGuardsOwnCallInezRepliesOnlyWhenSheHearsHim()
    {
        var h = Turned(null, "waitress", "guard"); var g = h.Game;
        var (code, door) = Refused(h, "guard");
        Assert.That(code, Is.EqualTo("summon-no-visit")); Assert.That(Apart(g, g.Inez, g.GuardP), Is.GreaterThan(g.R.WitnessRangeCm));
        Assert.That(Who(door), Is.EqualTo("guard"), "at his door, she is out of earshot"); Said(h, door, "guard", code);

        h.GiveScripted(new Order { Op = "fixer", Do = "call", Target = "waitress" });
        Assert.That(Host.Until(h, () => g.GuardPose()?.phase == "at café" || g.Over, 1200), $"he never reached her: {g.GuardPose()?.phase}"); Free(h);
        Assert.That(Apart(g, g.Inez, g.GuardP), Is.LessThanOrEqualTo(g.R.WitnessRangeCm), "beside her");
        var (code2, cafe) = Refused(h, "guard");
        Assert.That(code2, Is.EqualTo("summon-no-visit")); Assert.That(Who(cafe), Is.EqualTo("guard,waitress"), "her reply, after his line"); Said(h, cafe, "waitress", code2);
    }
}
