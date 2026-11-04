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

    /// <summary>The start's Job page. hostlogic has no built copy of brief.json (the player's StreamingAssets: ProofBook.bookJobWay holds the ways there), so the two ways
    /// are given as contracts/missions/handover/brief.json has them and the page's start pieces are built on them as BookJobPage does.</summary>
    static BookPage StartPage(HandoverHost h)
    {
        Host.Set(h, "BookStart", true);
        try
        {
            var p = h.BookJobPage(); p.Begin = true;
            if (p.Ways.Count == 0)
            {
                foreach (var (id, contact) in new[] { ("front-door", "waitress"), ("pickup", "driver") }) p.Ways.Add(new BookWay { Verb = id, Word = id, Target = new HandoverHost.Target { Kind = "person", Key = contact, Id = contact } });
                p.Needs.Clear(); p.Crew.Clear(); Host.Call(h, "BookNeeds", p); Host.Call(h, "BookJobStart", p, BookWordsFile.Get());
            }
            return p;
        }
        finally { Host.Set(h, "BookStart", false); }
    }

    [Test] public void TheStartIsTheJobPageWithFacesTheSharedEndTheCellAndBegin()
    {
        var h = Host.Fresh(); var p = StartPage(h); var bw = BookWordsFile.Get();
        Assert.That(p.Kicker, Is.EqualTo("THE HANDOVER")); Assert.That(p.Purse, Is.EqualTo($"${h.Game.Cash:N0}")); Assert.That(p.Heat, Is.Not.Empty);
        Assert.That(p.Subject, Is.Null, "the start stays on the opening view (the cell): only a hover turns the street");
        var faces = p.Ways.ToDictionary(w => w.Verb, w => w.Faces.Select(f => f.View).ToList());
        Assert.That(faces["front-door"], Is.EqualTo(new List<string> { "waitress", "guard" }), "Inez, then the guard her way needs off the door");
        Assert.That(faces["pickup"], Is.EqualTo(new List<string> { "driver" }));
        Assert.That(p.Shared, Is.EqualTo(bw.Shared).And.Not.Empty);
        var tail = p.Needs.Where(n => n.Group == BookLayout.Tail).Select(n => n.Id).ToList();
        Assert.That(tail, Is.EqualTo(new List<string> { "guarded", "go", "left-safe" }), "the shared conditions after the getaway sit once under the shared line");
        foreach (var w in p.Ways) Assert.That(p.Needs.Count(n => n.Group == w.Verb && n.Id == "getaway"), Is.EqualTo(1), $"{w.Verb} keeps its own 'reaches the car'");
        Assert.That(p.Needs.Where(n => n.Group != BookLayout.Tail).All(n => !n.Met), "no way's own condition is met at 0:00 (left-safe is honestly true: no fight or seal yet)");
        Assert.That(p.Crew.Select(c => c.Line).ToArray(), Is.EqualTo(new[] { "scout", "fixer", "tech", "enforcer" }.Select(r => bw.StartCell[r]).ToArray()), "the cell strip's lines are book.start.cell");
        Assert.That(string.Join(" ", p.Crew.Select(c => c.Line)), Does.Not.Contain("false tip"), "the stale false tip is gone");
        Assert.That(p.Track.Select(t => t.Time).ToArray(), Is.EqualTo(new[] { "2:35", "5:10", "5:20", "5:30", "5:50" }), "the 5:30 shift's clock, the rivals' 2:35 shown (Sky's call 2)");
        Assert.That(p.Track[^1].Word, Is.EqualTo(bw.Stations["5:50"]));
        // laid out on the grid: everything inside 1920 x 1080, Begin clear of the cell strip and the clock, no slot over another in the text column
        var r = BookLayout.Place(p, Tw);
        foreach (var it in r.Items) Assert.That(it.At.xMax <= 1920f && it.At.yMax <= 1080f && it.At.x >= 0f && it.At.y >= 0f, $"{it.Slot} {it.At} off the page");
        var sh = r.Of(BookSlot.Shared).Single(); Assert.That(Tw(p.Shared, sh.Size, "") + 52f, Is.LessThanOrEqualTo(sh.At.width), "the shared line fits its column, clear of the rail");
        var begin = r.Of(BookSlot.Begin).Single().At;
        foreach (var it in r.Items) if (it.Slot is BookSlot.Crew or BookSlot.Track or BookSlot.Needs or BookSlot.Shared) Assert.That(it.At.Overlaps(begin), Is.False, $"{it.Slot} {it.At} under Begin {begin}");
        var col = r.Items.Where(i => i.Slot is BookSlot.Ways or BookSlot.Faces or BookSlot.Needs or BookSlot.Shared or BookSlot.Track or BookSlot.Crew).ToList();
        for (int a = 0; a < col.Count; a++) for (int b = a + 1; b < col.Count; b++)
                if (col[a].Slot != col[b].Slot) Assert.That(col[a].At.Overlaps(col[b].At), Is.False, $"{col[a].Slot} {col[a].At} over {col[b].Slot} {col[b].At}");
    }

    [Test] public void InPlayTheJobPageKeepsBackAndFramesTheDoor()
    {
        var h = Host.Fresh(); var p = h.BookJobPage();
        Assert.That(p.Begin, Is.False); Assert.That(p.Ways.All(w => w.Faces.Count > 0)); Assert.That(p.Crew.Count, Is.EqualTo(4));
        Assert.That(BookLayout.Place(p, Tw).Has(BookSlot.Begin), Is.False);
    }

    [Test] public void CrucialCardsQueueMostUrgentFirstAndAStaleOneLeavesByItself()
    {
        var h = Host.Fresh(); var g = h.Game; int hash0 = 0; ulong before = g.ClosingHash();
        void Log(string code, string text = "") => Host.Call(h, "CrucialFromLog", new LogLine { Tick = g.Tick, Code = code, Text = text });
        Log("vale.exits"); Log("turn.pay.turned", "The waitress takes the money (waitress)."); Log("seal.starts"); Log("rivals.coming");
        g.Ops[(int)OpId.Tech].Gun.Down = true; Host.Call(h, "CrucialTrack");
        var kinds = h.CrucialQueue.Select(c => c.Kind).ToList();
        Assert.That(kinds, Is.EqualTo(new List<string> { "seal", "down", "target-out", "rival-car", "turned" }), "seal > agent down > Popovic out > rival car > turned (Sky's call 7)");
        Assert.That(h.CrucialNow.Kind, Is.EqualTo("seal"));
        var turned = h.CrucialQueue.Find(c => c.Kind == "turned"); Assert.That(turned.Title, Does.Contain("Inez")); Assert.That(turned.Face, Does.Contain("cameo_waitress"));
        Assert.That(h.CrucialQueue.Find(c => c.Kind == "down").Title, Does.Contain(h.AgentName((int)OpId.Tech)));
        h.CrucialDismiss(); Assert.That(h.CrucialNow.Kind, Is.EqualTo("down"), "a click reads the card and the next shows");
        g.Ops[(int)OpId.Tech].Gun.Down = false; Host.Call(h, "CrucialTrack");
        Assert.That(h.CrucialQueue.Exists(c => c.Kind == "down"), Is.False, "the agent is up again: the card leaves by itself (Sky's call 4)");
        Assert.That(h.CrucialLog.Count, Is.EqualTo(5), "every card stays in the Journal");
        g.Alert = 1; Host.Call(h, "CrucialTrack"); Assert.That(h.CrucialNow.Kind, Is.EqualTo("alert"), "Alert outranks Popovic out");
        Assert.That(h.CrucialLine(h.CrucialNow), Does.EndWith(HandoverHost.MissionClock(g.Tick)));
        g.Alert = 0; g.Ops[(int)OpId.Tech].Gun.Down = false;
        Assert.That(g.ClosingHash(), Is.EqualTo(before), "the queue only reads the run"); _ = hash0;
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
