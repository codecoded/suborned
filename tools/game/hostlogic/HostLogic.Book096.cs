// Brief 096 (Glyph's mocks briefs/096/glyph, Sky 2026-11-04 "all. recommended"): the start briefing as the book's Job page, the slim feed's crucial queue, the Journal.
// Same harness as HostLogic.cs (the real engine, the real text.json, no views): the page's data and layout, the queue's order and staleness, and that none of it moves the run.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using UnityEngine;

public class Book096
{
    static float Tw(string s, float size, string font) => (s?.Length ?? 0) * size * .52f;

    // brief 108: the start's Job page is the briefing's three pages now: HostLogic.Brief108.cs holds them (the 096 page with faces, the shared end, the cell strip and the clock is retired)

    [Test] public void CrucialCardsQueueMostUrgentFirstAndAStaleOneLeavesByItself()
    {
        var h = Host.Fresh(); var g = h.Game; int hash0 = 0; ulong before = g.ClosingHash();
        void Log(string code, string text = "") => Host.Call(h, "CrucialFromLog", new LogLine { Tick = g.Tick, Code = code, Text = text });
        Log("vale.exits"); Log("turn.pay.turned", "The waitress takes the money (waitress)."); Log("seal.starts"); Log("rivals.coming");
        g.Ops[(int)OpId.Tech].Gun.Down = true; Host.Call(h, "CrucialTrack");
        var kinds = h.CrucialQueue.Select(c => c.Kind).ToList();
        Assert.That(kinds, Is.EqualTo(new List<string> { "down", "target-out", "rival-car", "turned" }), "agent down > Popovic out > rival car > turned (Sky's call 7); the seal is its plate's, never a card (Sky 2026-11-05)");
        Assert.That(h.CrucialNow.Kind, Is.EqualTo("down"));
        var turned = h.CrucialQueue.Find(c => c.Kind == "turned"); Assert.That(turned.Title, Does.Contain("Inez")); Assert.That(turned.Face, Does.Contain("cameo_waitress"));
        Assert.That(h.CrucialQueue.Find(c => c.Kind == "down").Title, Does.Contain(h.AgentName((int)OpId.Tech)));
        g.Ops[(int)OpId.Tech].Gun.Down = false; Host.Call(h, "CrucialTrack");
        Assert.That(h.CrucialQueue.Exists(c => c.Kind == "down"), Is.False, "the agent is up again: the card leaves by itself (Sky's call 4)");
        h.CrucialDismiss(); Assert.That(h.CrucialNow.Kind, Is.EqualTo("rival-car"), "a click reads the card and the next shows");
        Assert.That(h.CrucialLog.Count, Is.EqualTo(4), "every card stays in the Journal");
        g.Alert = 1; Host.Call(h, "CrucialTrack"); Assert.That(h.CrucialNow.Kind, Is.EqualTo("alert"), "Alert outranks Popovic out");
        Assert.That(h.CrucialLine(h.CrucialNow), Does.EndWith(HandoverHost.MissionClock(g.Tick)));
        g.Alert = 0; g.Ops[(int)OpId.Tech].Gun.Down = false;
        Assert.That(g.ClosingHash(), Is.EqualTo(before), "the queue only reads the run"); _ = hash0;
    }

    [Test] public void TheEnforcerBadlyHurtCarryingPopovicTopsTheQueueUntilHeIsInTheCar()
    {
        var h = Host.Fresh(); var g = h.Game; ulong before = g.ClosingHash(); var vale0 = g.Vale; var e = g.Ops[(int)OpId.Enforcer];
        void Log(string code) => Host.Call(h, "CrucialFromLog", new LogLine { Tick = g.Tick, Code = code, Text = "" });
        Log("rivals.coming"); Log("vale.out"); g.Vale = ValeAt.WithEnforcer; Log("enforcer.warning"); Host.Call(h, "CrucialTrack");
        Assert.That(h.CrucialNow.Kind, Is.EqualTo("hurt-carry"), "badly hurt while carrying Popovic outranks even Popovic out (Sky 2026-11-05)");
        Assert.That(h.CrucialLine(h.CrucialNow), Does.Contain(h.ValeWord).And.Contain($"{e.Gun.Health} health"), "the line names Popovic and the health left, live");
        g.Vale = ValeAt.Car; Host.Call(h, "CrucialTrack");
        Assert.That(h.CrucialQueue.Exists(c => c.Kind == "hurt-carry"), Is.False, "Popovic in the car: the card leaves by itself");
        g.Vale = vale0; Log("enforcer.warning"); Host.Call(h, "CrucialTrack");
        Assert.That(h.CrucialQueue.Single(c => c.Kind == "hurt").Rank, Is.EqualTo(System.Array.IndexOf(HandoverHost.CrucialOrder, "down")), "not carrying: it ranks with an agent down");
        e.Gun.Down = true; Host.Call(h, "CrucialTrack"); Assert.That(h.CrucialQueue.Exists(c => c.Kind == "hurt"), Is.False, "down: the down card says it instead");
        e.Gun.Down = false; Assert.That(g.ClosingHash(), Is.EqualTo(before), "the queue only reads the run");
    }

    [Test] public void TheJournalHoldsEveryRowNewestFirstCrucialMarkedUnreadDotted()
    {
        var h = Host.Fresh(); var g = h.Game;
        Host.Call(h, "CrucialFromLog", new LogLine { Tick = g.Tick, Code = "vale.exits", Text = "" });
        Assert.That(h.JournalUnread, Is.GreaterThan(0), "the Book key's dot"); Assert.That(h.BookSectionLive("journal"));
        var p = h.BookJournalPage(); var bw = BookWordsFile.Get();
        Assert.That(p.Rows[^1].Title, Is.EqualTo(bw.JournalBegins)); Assert.That(p.Rows[0].Crucial && p.Rows[0].Unread, "the newest row is the crucial one, unread");
        Assert.That(p.Filters.Select(f => f.Id).ToArray(), Is.EqualTo(new[] { "all", "crucial", "learned", "turned", "city" }));
        Assert.That(p.Sub, Does.Contain(bw.JournalNew.Replace("{n}", "1")));
        h.JournalFilterSet("learned"); Assert.That(h.BookJournalPage().Rows.Exists(r => r.Crucial), Is.False, "Learned shows no crucial row");
        h.JournalFilterSet("all");
        var r = BookLayout.Place(p, Tw); Assert.That(r.RowsShown, Is.EqualTo(p.Rows.Count)); foreach (var it in r.Of(BookSlot.Rows)) Assert.That(it.At.yMax, Is.LessThanOrEqualTo(1080f));
    }

    [Test] public void TheNewWordsAreInTheText()
    {
        var bw = BookWordsFile.Get();
        foreach (var k in HandoverHost.CrucialOrder.Append("refused")) Assert.That(bw.FeedCards.ContainsKey(k), $"book.feed.cards.{k}");
        Assert.That(bw.FeedCards["for-car"].Fixes.Count, Is.EqualTo(2)); Assert.That(bw.KeyWord, Is.EqualTo("Book")); Assert.That(bw.HeldFix, Is.Not.Empty); Assert.That(bw.Begin, Is.Not.Empty);
        Assert.That(BookWordsFile.Get().Sections().Select(s => s.Id), Does.Contain("menu"));
    }
}
