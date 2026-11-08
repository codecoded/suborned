// Brief 085's conversation cases, moved out of HostLogic.cs by topic (brief 084: files stay under 500 lines). Same harness: the real engine, the real text.json, no views.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using UnityEngine;
using Order = Suborned.Sim.Order;

/// <summary>Brief 085: which scene a landing plays, read from the engine (Fable's table), on the routes' real orders; what the read-time, the Door field and the docks do. No views here, so the cutaway itself falls to bubbles: Proof085Scenes plays it.</summary>
public class Conversations
{
    static (HandoverHost h, List<Order> orders) Route(string route, ConversationScenes scenes)
    {
        var h = Host.Fresh(); h.controls.conversations = scenes; Host.Set(h, "HintsOn", false); /* a learned hint writes PlayerPrefs, which has no store here */
        return (h, Script.Parse(File.ReadAllText(System.IO.Path.Combine(Root.Path, "contracts/missions/handover/routes", route + ".json"))).Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList());
    }
    static void Give(HandoverHost h, Order o) { if (o.Tick > h.Game.Tick) h.RunTo(o.Tick); if (o.Do is "watch" or "listen" or "turn" or "call" && o.Op != "cell") h.GiveScripted(o); else h.Issue(o); Host.Call(h, "Track"); }
    static HandoverHost PlayHost(string route, int until)
    {
        var (h, orders) = Route(route, ConversationScenes.Always); foreach (var o in orders) { if (o.Tick >= until || h.Game.Over) break; Give(h, o); }
        h.RunTo(until); Assert.That(h.Fault, Is.Null, h.Status); return h;
    }
    /// <summary>A route's orders up to its Scout's Listen on Inez, then on until that Listen lands. When it lands is the engine's (engine 0.49.0: the Scout walks to a free chair and sits first), never a tick written here.</summary>
    static HandoverHost ToTheListen(string route, ConversationScenes scenes)
    {
        var (h, orders) = Route(route, scenes); foreach (var o in orders) { Give(h, o); if (o.Do == "listen") break; }
        Assert.That(Host.Until(h, () => h.Game.Log.Exists(l => l.Code == "scout.listen.heard" && l.Text.Contains("(waitress)")), 1500), $"the Listen never landed: tick {h.Game.Tick}, {h.Status}"); Assert.That(h.Fault, Is.Null, h.Status); return h;
    }
    static List<string> Play(string route, int until) => PlayHost(route, until).ConvoPicks;
    [Test] public void TheHelpRoutePlaysEachScene() { var p = Play("route-a", 1600); Assert.That(p, Is.EqualTo(new[] { "listen-inez", "help-inez-2", "call-inez" }), string.Join(",", p)); } // engine 0.44.0: one Help after the Listen, the full offer
    /// <summary>Sky 2026-11-04 ("couldn't find a way for Fixer to pressure anyone" · "leave for now if it's there after listen"): Pressure needs the need
    /// known (GDD §04), so before the Scout's Listen the ring shows it locked, and once the Listen has landed the Fixer's ring offers it.</summary>
    [Test] public void PressureIsOfferedOnceTheListenLands()
    {
        static HandoverHost.PanelRow PressureRow(HandoverHost h) { h.Select(OpId.Fixer, false); return Host.Plan(h, "waitress").Pn.Rows.FirstOrDefault(r => r.I?.Order?.Do == "turn" && r.I.Order.Approach == "pressure"); }
        var before = PressureRow(PlayHost("route-a-pressure", 280));
        Assert.That(before == null || !before.Available, "before the Listen, Pressure is not open");
        var given = PressureRow(PlayHost("route-a-pressure", 300)); Assert.That(given == null || !given.Available, "the Listen given and not landed (a Listen takes 30 s): Pressure is not open yet");
        var after = PressureRow(ToTheListen("route-a-pressure", ConversationScenes.Always));
        Assert.That(after, Is.Not.Null, "after the Listen, no Pressure row on Inez");
        Assert.That(after.Available, after.Reason);
    }
    [Test] public void ThePressureRoutePlaysItsScenes() { var p = Play("route-a-pressure", 1600); Assert.That(p, Does.Contain("listen-inez")); Assert.That(p, Does.Contain("pressure-inez")); Assert.That(p, Does.Contain("call-inez-pressure"), string.Join(",", p)); Assert.That(p, Does.Not.Contain("call-inez")); }
    [Test] public void ThePayRoutePlaysTheDriver() { var p = Play("route-b", 1600); Assert.That(p, Does.Contain("pay-driver"), string.Join(",", p)); }
    [Test] public void OtherTemperamentsGetNoConversation()
    {
        var t = PlayText.Load(); Assert.That(t.LookupConversation(new PlayText.ConvQuery { Verb = "help", About = "waitress", Temperament = "mercenary", Outcome = "turned", Helped = true }), Is.Null);
        Assert.That(t.LookupConversation(new PlayText.ConvQuery { Verb = "listen", About = "waitress", Temperament = "fearful", Turned = true }), Is.Null, "a turned Inez's Listen keeps today's lines");
        Assert.That(t.LookupConversation(new PlayText.ConvQuery { Verb = "ask", About = "waitress", Temperament = "fearful" }), Is.Null);
        Assert.That(t.LookupConversation(new PlayText.ConvQuery { Verb = "help", About = "waitress", Temperament = "fearful", Outcome = "turned", Helped = false }).Id, Is.EqualTo("help-inez-2"), "the engine's outcome picks the scene"); Assert.That(t.LookupConversation(new PlayText.ConvQuery { Verb = "help", About = "waitress", Temperament = "fearful", Outcome = "turned", Helped = true }).Id, Is.EqualTo("help-inez-2-known"), "after a refused small offer"); Assert.That(t.LookupConversation(new PlayText.ConvQuery { Verb = "help", About = "waitress", Temperament = "fearful", Outcome = "small-offer" }).Id, Is.EqualTo("help-inez-1"));
        Assert.That(t.ClipService, Is.EqualTo("Service door")); Assert.That(t.DoorField, Is.EqualTo("Door {clock}"));
    }
    [Test] public void AnInterruptedActPlaysNoScene()
    {
        var h = Host.Fresh(); h.controls.conversations = ConversationScenes.Always; h.GiveWatchScripted(OpId.Scout, "waitress"); Assert.That(Host.Until(h, () => h.Game.Ops[0].Busy != null)); h.CancelAgent(0, null); Host.Tick(h, 5);
        Assert.That(h.ConvoPicks, Is.Empty);
    }
    [Test] public void NoSceneStillGetsTheAgreedWordsNotTheOldOnes()
    {
        var h = ToTheListen("route-a", ConversationScenes.Never); Host.Tick(h, 5);
        var card = h.Feed.Concat(h.FeedHistory).First(c => c.Verb == "listen"); var conv = h.Text.Conversations.Find(x => x.Id == "listen-inez");
        Assert.That(card.Live, Is.False, "the card has landed");
        Assert.That(card.Lines.Select(l => l.Text), Is.EqualTo(conv.Beats.Select(b => b.Line)), "the card holds the whole agreed scene"); Assert.That(card.Footer, Is.EqualTo(conv.Learned[0]));
        Assert.That(h.PersonCards.Exists(c => c.Id == "listen-inez" && c.Who == "waitress"), Is.True, "the cards still show, on her"); Assert.That(h.Talk.Any(t => t.Text.Contains("Yes. Yes")), Is.False);
    }
    [Test] public void ALinesHeldMomentsAreReadCutawayOnly()
    {
        var h = Host.Fresh(); var c = h.Text.Conversations.Find(x => x.Id == "listen-inez"); var call = h.Text.Conversations.Find(x => x.Id == "call-inez");
        Assert.That(c.Beats[10].RevealS, Is.EqualTo(0.9f).Within(0.001f)); Assert.That(c.Beats[10].Line.StartsWith(c.Beats[10].RevealAfter), Is.True); Assert.That(c.Beats[6].Lean, Is.Not.Empty); Assert.That(c.Beats[6].Line.Contains(c.Beats[6].Lean), Is.True); // brief 096 step 2: the full-sentence lines
        Assert.That(call.Beats[4].HoldBefore, Is.GreaterThan(0f)); Assert.That(call.Beats[9].HoldAfter, Is.GreaterThan(0f)); Assert.That(call.Beats[9].HoldOn, Is.EqualTo("waitress"));
        Assert.That(h.Text.ShortLock("target-unknown"), Is.EqualTo("Can't act on that")); Assert.That(h.Text.ConvNew, Is.Not.Empty); Assert.That(h.Text.ConvVoices.ContainsKey("milena"), Is.True);
        Assert.That(h.Game.Walkers.Exists(w => w.P.Name.StartsWith(h.Text.ConvName("milena") + " ")), Is.True, "the voice has a person of the street to stand in the doorway");
    }
    [Test] public void ALineIsHeldToRead()
    {
        Assert.That(HandoverHost.ReadS("Short."), Is.EqualTo(1f)); Assert.That(HandoverHost.ReadS(new string('x', 64)), Is.EqualTo(4f).Within(0.01f));
    }
    [Test] public void TheDoorFieldRounds() { Assert.That(HandoverHost.DoorWords(989), Is.EqualTo("1:40")); Assert.That(HandoverHost.DoorWords(1740), Is.EqualTo("2:50")); }
    [Test] public void TheDoorPriceShowsOnPressureAndAfterItOnTheCall()
    {
        var h = Host.Fresh(); h.Select(OpId.Fixer, false); var g = h.Game;
        var field = HudModel.FieldOrder.ToList(); Assert.That(field.IndexOf("door"), Is.EqualTo(field.IndexOf("trail") + 1));
        Assert.That(h.DoorClock(new Order { Op = "fixer", Do = "turn", Target = "waitress", Approach = "pressure" }), Does.StartWith("1:").Or.StartWith("2:"), "pressure on Inez");
        Assert.That(h.DoorClock(new Order { Op = "fixer", Do = "call", Target = "waitress" }), Is.Null, "a Call with no Pressure remembered");
        Assert.That(h.DoorClock(new Order { Op = "fixer", Do = "turn", Target = "driver", Approach = "pressure" }), Is.Null, "the driver has no lover to call");
    }
    [Test] public void ADockSlidesClearOfTheHudAndOfAnotherDock()
    {
        var hud = new List<Rect> { new Rect(1500, 900, 420, 180) }; var at = HudToolkit.DockSlide(new Vector2(1600, 1030), 1920, 1080, hud, new List<Vector2>()); Assert.That(new Rect(1500 - 27, 900 - 27, 420 + 54, 180 + 54).Contains(at), Is.False, at.ToString());
        var free = new Vector2(1000, 40); var a = HudToolkit.DockSlide(free, 1920, 1080, null, new List<Vector2>()); Assert.That(a, Is.EqualTo(free)); var b = HudToolkit.DockSlide(free, 1920, 1080, null, new List<Vector2> { a }); Assert.That(Vector2.Distance(a, b), Is.GreaterThanOrEqualTo(62f), "two docks in the same direction");
    }
}
