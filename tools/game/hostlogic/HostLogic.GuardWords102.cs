// Brief 102, Fable's words for the guard's end of the call (a8bf90bd8; briefs/102/fable/notes.md, "The keys (Latch reads them)") as the host plays them, on the real
// text: his call scene (scenes.items kind call, about guard); his own refusals (summon-refused rows `about` guard, HostCall.SummonAbout); "He's not answering"
// (summon-unanswered, in place of Inez's summon-busy line when he's down and she doesn't know it; summon-knows-down when she does, Game.KnowsDown, c94c7a657); a contact
// who is down picks nothing up (the lock alone); the café on his own call (its city scene `about` guard); the pressed
// visit's warning by who remembers the Pressure (Inez's conversation lines, or his `he-warns` scene), never by who called; the tie-unknown lock form before the cell
// knows the pair; the loitering card's place in its hover line only; the debrief's "answered" for his call.
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using Order = Suborned.Sim.Order;

public class GuardWords102
{
    /// <summary>A host on a fresh run whose game starts with these roles turned and these people known, no scenes cutting away (the bubbles' path).</summary>
    static HandoverHost Start(string[] turned, string[] known = null)
    {
        var h = Host.Fresh(); h.controls.conversations = ConversationScenes.Never; Host.Set(h, "HintsOn", false);
        var s = new Start(); foreach (var r in turned) s.Turned.Add(r);
        foreach (var r in known ?? new string[0]) s.Known[r] = new System.Collections.Generic.List<string> { "routine", "need", "temperament", "secret" };
        Host.Set(h, "Game", Engine.New(h.PlayInputs, s)); Host.Tick(h); return h;
    }
    static string[] Rows(HandoverHost h, string ev, string speaker, string outcome = null, string about = null) => h.Text.Lookup(new PlayText.Query { Event = ev, Speaker = speaker, Outcome = outcome, About = about })?.Lines.ToArray() ?? new string[0];
    static HandoverHost.FeedCard Refused(HandoverHost h, string role)
    {
        int n = h.Feed.Count; h.Issue(new Order { Op = "fixer", Do = "call", Target = role });
        var c = h.Feed.Skip(n).LastOrDefault(x => x.Kind == "call" && x.Tone == HandoverHost.Feedback.Refused); Assert.That(c, Is.Not.Null, $"no refused card on {role}: {h.Game.Orders.Last().code}"); return c;
    }
    static string Line(HandoverHost.FeedCard c, string speaker) => c.Lines.FirstOrDefault(l => l.Speaker == speaker)?.Text;
    static void FixerFree(HandoverHost h) => Assert.That(Host.Until(h, () => h.Game.Ops[(int)OpId.Fixer].Busy == null, 900), "the Fixer stays busy");
    static void ScoutAtTheCafe(HandoverHost h) { h.Issue(new Order { Op = "scout", Do = "move", To = "beside-inez" }); Assert.That(Host.Until(h, () => h.Game.Ops[(int)OpId.Scout].Walk == null, 3000), "the Scout never reached the café"); }

    /// <summary>His call plays Fable's scene for him (kind call, about guard, by his temperament): the Fixer's ask, his yes (confirm/call/guard), its result.</summary>
    [Test] public void HisCallPlaysHisScene()
    {
        var h = Start(new[] { "guard" }); var g = h.Game; int n = h.Feed.Count;
        h.GiveScripted(new Order { Op = "fixer", Do = "call", Target = "guard" });
        var card = h.Feed.Skip(n).FirstOrDefault(c => c.Kind == "call" && c.Target == "guard"); Assert.That(card, Is.Not.Null, "the call card");
        Assert.That((card.Scene?.Kind, card.Scene?.About, card.Scene?.Temperament), Is.EqualTo(("call", "guard", g.GuardP.Temperament)), "his own call scene");
        var said = card.Lines.Select(l => l.Speaker).ToArray(); Assert.That(said.Take(2), Is.EqualTo(new[] { "fixer", "guard" }), string.Join(" / ", card.Lines.Select(l => l.Speaker + ": " + l.Text)));
        var yes = h.Text.Lookup(new PlayText.Query { Event = "confirm", Verb = "call", Speaker = "guard", Temperament = g.GuardP.Temperament })?.Lines;
        Assert.That(yes, Does.Contain(Line(card, "guard")), "his yes is his confirm row");
        Assert.That(card.Lines.Where(l => l.Speaker == "fixer").Any(l => l.Text.Contains("Inez")), Is.False, "the Fixer never names Inez on his call");
    }

    /// <summary>His own refusals answer the Fixer (rows `about` guard); beside her at the café Inez replies to what she hears (her rows `about` guard); Inez's own call keeps
    /// her rows with no `about`, so her lines don't move.</summary>
    [Test] public void HisOwnRefusalsAreHis()
    {
        var h = Start(new[] { "waitress", "guard" }); var g = h.Game;
        h.GiveScripted(new Order { Op = "fixer", Do = "call", Target = "guard" }); Assert.That(Host.Until(h, () => g.GuardPose()?.phase == "at café" || g.Over, 1200), "he never reached her"); FixerFree(h);
        var his = Refused(h, "guard"); Assert.That(g.Orders.Last().code, Is.EqualTo("summon-busy"));
        Assert.That(Rows(h, "summon-refused", "guard", "summon-busy", "guard"), Does.Contain(Line(his, "guard")), "his words to the Fixer");
        Assert.That(Rows(h, "summon-refused", "waitress", "summon-busy", "guard"), Does.Contain(Line(his, "waitress")), "her reply to what she hears");
        var hers = Refused(h, "waitress"); Assert.That(g.Orders.Last().code, Is.EqualTo("summon-busy"));
        Assert.That(Rows(h, "summon-refused", "waitress", "summon-busy"), Does.Contain(Line(hers, "waitress")), "Inez's call keeps her rows");
        Assert.That(Rows(h, "summon-refused", "waitress", "summon-busy", "guard"), Does.Not.Contain(Line(hers, "waitress")), "never his call's");
    }

    /// <summary>Inez's call finds him down out of her sight (route G's quiet knockout): she tries and nobody answers (summon-unanswered), he says nothing.</summary>
    [Test] public void HeIsNotAnswering()
    {
        var h = Start(new[] { "waitress" }); var g = h.Game;
        var sc = Script.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Root.Path, "contracts/missions/handover/routes/route-g.json")));
        foreach (var o in sc.Orders.Where(x => x.Tick <= 1510).OrderBy(x => x.Tick)) { if (o.Tick > g.Tick) h.RunTo(o.Tick); if (o.Do is "watch" or "listen" or "turn") h.GiveScripted(o); else h.Issue(o); }
        Assert.That(Host.Until(h, () => g.GuardDown, 300), $"route G's knockout never landed: {h.Status}");
        Assert.That(g.Inez.Memory.Any(m => m.Act is "saw-takedown" or "saw-violence" or "found-body" or "saw-carry" or "told"), Is.False, "she knows nothing of it");
        var c = Refused(h, "waitress"); Assert.That(g.Orders.Last().code, Is.EqualTo("summon-busy")); Assert.That(g.KnowsDown("waitress", "guard"), Is.False, "the engine's read: she doesn't know");
        Assert.That(Rows(h, "summon-unanswered", "waitress"), Does.Contain(Line(c, "waitress")), "He's not answering");
        Assert.That(c.Lines.Any(l => l.Speaker == "guard"), Is.False, "he is down");
    }

    /// <summary>Engine 0.52.0's KnowsDown (Keel's C102-KNOWS-CARRY): the guard knocked out out of her sight, then carried past her. She knows he's down, so her call refused
    /// summon-busy plays Fable's summon-knows-down in place of her busy line ("He's not answering" would be untrue); he says nothing; the lock is summon-busy's.</summary>
    [Test] public void SheKnowsHesDownSoSheAsksAfterHim()
    {
        var h = Start(new[] { "waitress" }); var s = Scenarios.Find("C102-KNOWS-CARRY"); Assert.That(s, Is.Not.Null, "Keel's C102-KNOWS-CARRY is gone");
        var g = Scenarios.Run(h.PlayInputs, s).G; Host.Set(h, "Game", g); Host.Tick(h);
        Assert.That(g.GuardDown && g.KnowsDown("waitress", "guard"), $"she knows he's down (down {g.GuardDown}, knows {g.KnowsDown("waitress", "guard")})");
        var c = Refused(h, "waitress"); Assert.That(g.Orders.Last().code, Is.EqualTo("summon-busy"));
        Assert.That(Rows(h, "summon-knows-down", "waitress"), Does.Contain(Line(c, "waitress")), "Fable's line for what she knows");
        Assert.That(c.Lines.Any(l => l.Speaker == "guard"), Is.False, "he is down"); Assert.That(c.Footer, Is.Not.Empty, "the busy lock under her line");
    }

    /// <summary>Fable (brief 102): a contact who is down picks nothing up. The engine refuses a call on them before any summons (person-down): no call card, no line, the pop its
    /// lock. Should a summons' refusal reach the card with the one called down (his GuardDown read as he's down too), the card is the summon-busy lock alone, no line from anyone.
    /// The guard by route G's quiet knockout, Inez by the Enforcer's.</summary>
    [Test] public void ADownedContactAnswersNothing()
    {
        void Silent(HandoverHost h, string role, string what)
        {
            var g = h.Game; int f0 = h.Feed.Count, t0 = h.Talk.Count; h.Issue(new Order { Op = "fixer", Do = "call", Target = role });
            Assert.That(g.Orders.Last().code, Is.EqualTo("person-down"), what); Assert.That(h.Feed.Skip(f0).Any(c => c.Kind == "call"), Is.False, $"{what}: no call card"); Assert.That(h.Talk.Count, Is.EqualTo(t0), $"{what}: no line");
            int f1 = h.Feed.Count; Host.Call(h, "FeedSummonRefused", role, new Order { Op = "fixer", Do = "call", Target = role }, new Answer { ok = false, code = "summon-busy", why = role == "guard" ? "he's down" : "she's down" });
            var card = h.Feed.Skip(f1).Single(c => c.Kind == "call");
            Assert.That(card.Lines, Is.Empty, $"{what}: " + string.Join(" / ", card.Lines.Select(l => l.Speaker + ": " + l.Text))); Assert.That(card.Footer, Is.Not.Empty, $"{what}: the card is the lock");
        }
        var h = Start(new[] { "waitress", "guard" }); var g = h.Game;
        var sc = Script.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Root.Path, "contracts/missions/handover/routes/route-g.json")));
        foreach (var o in sc.Orders.Where(x => x.Tick <= 1510).OrderBy(x => x.Tick)) { if (o.Tick > g.Tick) h.RunTo(o.Tick); if (o.Do is "watch" or "listen" or "turn") h.GiveScripted(o); else h.Issue(o); }
        Assert.That(Host.Until(h, () => g.GuardDown, 300), $"route G's knockout never landed: {h.Status}");
        Silent(h, "guard", "his own call, him knocked out");
        var h2 = Start(new[] { "waitress" }); var g2 = h2.Game;
        h2.GiveScripted(new Order { Op = "enforcer", Do = "takedown", Target = "waitress", How = "knockout" }); // the host walks him in, as a click on her ring does
        Assert.That(Host.Until(h2, () => g2.Inez.Down, 3000), $"she was never knocked out: {g2.Orders.Last().code} {g2.Orders.Last().why}");
        Silent(h2, "waitress", "her call, her knocked out");
    }

    /// <summary>On his own call the café's scene is his (`about` guard: "Look who's left his door."); on Inez's call hers plays, as before.</summary>
    [Test] public void TheCafeOnHisOwnCallIsHis()
    {
        string Cafe(string role)
        {
            var h = Start(new[] { "waitress", "guard" }); var g = h.Game; ScoutAtTheCafe(h);
            int n = h.Feed.Count; h.GiveScripted(new Order { Op = "fixer", Do = "call", Target = role });
            Assert.That(Host.Until(h, () => h.Feed.Skip(n).Any(c => c.Event == "guard-at-cafe") || g.Over, 1500), $"no café card on {role}'s call (phase {g.GuardPose()?.phase})");
            var card = h.Feed.Skip(n).First(c => c.Event == "guard-at-cafe"); return card.Scene?.About ?? "";
        }
        Assert.That(Cafe("guard"), Is.EqualTo("guard"), "his own call: his scene");
        Assert.That(Cafe("waitress"), Is.EqualTo(""), "her call: hers");
    }

    /// <summary>The pressed visit's warning is the pressured one's, whoever called: the guard pressured and called himself, he warns her (`he-warns`); Inez pressured and
    /// the guard called, she warns him (her call conversation's street lines).</summary>
    [Test] public void TheWarningIsThePressuredOnes()
    {
        HandoverHost.FeedCard Warning(string pressed, string[] turned)
        {
            var h = Start(turned, new[] { "guard", "waitress" }); var g = h.Game;
            h.GiveScripted(new Order { Op = "fixer", Do = "turn", Target = pressed, Approach = "pressure" });
            Assert.That(Host.Until(h, () => g.PersonByRole(pressed).Turned && g.Ops[(int)OpId.Fixer].Busy == null, 2000), $"the Pressure on {pressed} never landed: {h.Status}");
            Assert.That(g.PersonByRole(pressed).Has("pressure"));
            int n = h.Feed.Count; h.GiveScripted(new Order { Op = "fixer", Do = "call", Target = "guard" }); Assert.That(g.GuardCallPressed, "a pressed visit");
            Assert.That(Host.Until(h, () => h.Feed.Skip(n).Any(c => c.Event == "guard-at-cafe") || g.Over, 1500), $"no café card ({g.GuardPose()?.phase})");
            return h.Feed.Skip(n).First(c => c.Event == "guard-at-cafe");
        }
        var his = Warning("guard", new[] { "waitress" });
        Assert.That(his.Lines.First().Speaker, Is.EqualTo("guard"), "he warns her: " + string.Join(" / ", his.Lines.Select(l => l.Speaker + ": " + l.Text)));
        var hers = Warning("waitress", new[] { "guard" });
        Assert.That(hers.Lines.First().Speaker, Is.EqualTo("waitress"), "she warns him: " + string.Join(" / ", hers.Lines.Select(l => l.Speaker + ": " + l.Text)));
    }

    /// <summary>A summons' lock before the cell knows the pair names only the one called (tie-unknown); the Handover's briefing names the pair, so its base words show.</summary>
    [Test] public void TheLockNamesOnlyTheOneCalledUntilThePairIsKnown()
    {
        var h = Start(new[] { "waitress" }); var g = h.Game;
        h.GiveScripted(new Order { Op = "fixer", Do = "call", Target = "waitress" }); Assert.That(Host.Until(h, () => g.GuardAtDoor && g.GuardP.Summoned > 0 || g.Over, 3000)); FixerFree(h);
        var a = g.CallQuery("waitress"); Assert.That(a.code, Is.EqualTo("summon-came-once"));
        var o = new Order { Op = "fixer", Do = "call", Target = "waitress" };
        string Short() => (string)Host.Call(h, "ShortOf", o, a);
        Assert.That(Short(), Is.EqualTo(h.Text.ShortLock("summon-came-once")), "the briefing names the pair: the base words");
        h.Text.Hints.Clear(); // a mission whose briefing names no pair, and no Listen yet
        Assert.That(Short(), Is.EqualTo(h.Text.ShortLock("summon-came-once", "tie-unknown")), "until the pair is known, the general form");
        Assert.That(Short(), Is.Not.EqualTo(h.Text.ShortLock("summon-came-once")));
    }

    /// <summary>The loitering card's short names who saw whom, with no place (its hover line keeps it); his call's how in the debrief is "answered".</summary>
    [Test] public void TheLoiteringCardAndTheDebriefsHow()
    {
        Assert.That((bool)typeof(HandoverHost).GetMethod("NoPlace", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { "loiter" }), "loiter has no place in its short");
        var how = (System.Array)typeof(HandoverHost).GetField("HowCodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
        Assert.That(how.Cast<object>().Select(x => x.ToString()), Has.Some.EqualTo("(guard-away, call.guard, answered)"), "his call: on the Fixer's call");
    }
}
