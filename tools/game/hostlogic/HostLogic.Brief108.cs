// Brief 108 (Glyph's pages briefs/108/glyph/pages; Sky 2026-11-11: "Yes, much better and clearer"): the start briefing as three calm pages on the book's Job page, then Begin.
// Same harness as HostLogic.cs (the real engine, the real text.json, no views): each page's data from the text, its layout on the grid, the ways' steps behind their chips,
// that Sky's hint about the square's cameras is true by the engine's own sight, and that none of it moves the run.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using UnityEngine;

public class Brief108
{
    static float Tw(string s, float size, string font) => (s?.Length ?? 0) * size * .52f;

    /// <summary>A step's page as the start (or play) builds it. hostlogic has no built brief.json (the player's StreamingAssets), so the two ways are given as
    /// contracts/missions/handover/brief.json has them, and their conditions are read as BookJobPage reads them.</summary>
    static BookPage Page(HandoverHost h, int step, bool start = true, string wayOpen = "")
    {
        Host.Set(h, "BookStart", start); Host.Set(h, "BookStep", step); Host.Set(h, "BookWayOpen", wayOpen);
        try
        {
            var p = h.BookJobPage(); p.Begin = start;
            if (p.Ways.Count == 0)
            {
                foreach (var (id, contact) in new[] { ("front-door", "waitress"), ("pickup", "driver") }) p.Ways.Add(new BookWay { Verb = id, Word = id, Target = new HandoverHost.Target { Kind = "person", Key = contact, Id = contact } });
                p.Needs.Clear(); Host.Call(h, "BookNeeds", p);
            }
            return p;
        }
        finally { Host.Set(h, "BookStart", false); Host.Set(h, "BookStep", 0); Host.Set(h, "BookWayOpen", ""); }
    }

    static void Fits(BookPage p, string what)
    {
        var r = BookLayout.Place(p, Tw);
        foreach (var it in r.Items) Assert.That(it.At.x >= 0f && it.At.y >= 0f && it.At.xMax <= 1920f && it.At.yMax <= 1080f, $"{what}: {it.Slot} {it.At} off the page");
        foreach (var it in r.Items) if (it.Slot != BookSlot.Hero) Assert.That(it.At.xMax, Is.LessThanOrEqualTo(BookLayout.BriefRight + .01f), $"{what}: {it.Slot} {it.At} runs into the signs' rail");
        foreach (var it in r.Items) if (it.Slot is BookSlot.Name or BookSlot.Kicker or BookSlot.Story or BookSlot.Place or BookSlot.Danger) Assert.That(it.At.x, Is.GreaterThanOrEqualTo(p.CurveLeft), $"{what}: {it.Slot} {it.At} on the street, left of the page's edge {p.CurveLeft}");
        var body = r.Items.Where(i => i.Slot is BookSlot.Story or BookSlot.Place or BookSlot.Danger or BookSlot.Needs or BookSlot.Pager or BookSlot.Begin or BookSlot.Name).ToList();
        for (int a = 0; a < body.Count; a++) for (int b = a + 1; b < body.Count; b++) Assert.That(body[a].At.Overlaps(body[b].At), Is.False, $"{what}: {body[a].Slot} {body[a].At} over {body[b].Slot} {body[b].At}");
        var floor = r.Items.Single(i => i.Slot == BookSlot.Pager).At.y; foreach (var it in body) if (it.Slot is not (BookSlot.Pager or BookSlot.Begin)) Assert.That(it.At.yMax, Is.LessThanOrEqualTo(floor), $"{what}: {it.Slot} {it.At} reaches the pager");
    }

    static int Words(BookPage p) => new[] { p.Kicker, p.Name }.Concat(p.Story).Concat(p.Places.SelectMany(x => new[] { x.Word, x.Line, x.Chip, x.When })).Concat(p.Dangers.SelectMany(x => new[] { x.Word, x.Line, x.When }))
        .Sum(s => (s ?? "").Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Length);

    [Test] public void TheStartIsThreeCalmPagesFromTheText()
    {
        var h = Host.Fresh(); var g = h.Game; var bw = BookWordsFile.Get(); ulong before = g.ClosingHash();
        Assert.That(bw.HasBrief, "text.json book.brief: three titles");
        for (int st = 0; st < 3; st++)
        {
            var p = Page(h, st);
            Assert.That(p.Step, Is.EqualTo(st)); Assert.That(p.Kicker, Is.EqualTo("THE HANDOVER")); Assert.That(p.Name, Is.EqualTo(bw.BriefTitles[st]));
            Assert.That(p.Purse + p.Heat + p.Shared, Is.Empty, $"page {st}: no purse, Heat or shared line (the HUD has the cash; Heat says nothing on the first mission)");
            Assert.That(p.Crew.Count + p.Track.Count + p.Facts.Count, Is.Zero, $"page {st}: no cell strip, clock or ways to fail");
            Assert.That(p.Ways.Select(w => w.Verb), Is.EqualTo(new[] { "front-door", "pickup" }), "the ways stay in the page's data (a way is still the ring's: ProofBook.bookWayIsTheRing)");
            Assert.That(p.Needs.Count, Is.GreaterThan(6), "the ways' conditions stay in the data on every page (ProofBook.bookNeeds)");
            Assert.That(Words(p), Is.LessThanOrEqualTo(55), $"page {st} reads {Words(p)} words: a calm page");
            Host.NoTokens($"page {st}", new[] { p.Name, p.NextWord, p.BackWord, p.SkipWord }.Concat(p.Story).Concat(p.Places.SelectMany(x => new[] { x.Word, x.Line, x.Chip })).Concat(p.Dangers.SelectMany(x => new[] { x.Word, x.Line })).ToArray());
            var r = BookLayout.Place(p, Tw);
            Assert.That(r.Has(BookSlot.Begin), Is.EqualTo(st == 2), $"page {st}: Begin only on the last page"); Assert.That(r.Has(BookSlot.Pager), $"page {st}: the pager");
            Assert.That(r.Has(BookSlot.Hero), Is.EqualTo(st == 0), $"page {st}: Popovic's face on the mission page only");
            Fits(p, $"page {st}");
        }
        var p0 = Page(h, 0); Assert.That(p0.Story, Is.EqualTo(bw.Story)); Assert.That(p0.HeroFace, Does.Contain("councillor")); Assert.That(p0.CurveLeft, Is.EqualTo(BookLayout.BriefCurve));
        var p1 = Page(h, 1); Assert.That(p1.CurveLeft, Is.EqualTo(BookLayout.PlaceCurve), "the place page gives the street room");
        Assert.That(p1.Places.Select(x => x.Way).Where(w => w.Length > 0), Is.EqualTo(new[] { "front-door", "pickup" }), "the two ways in are places on the place page");
        Assert.That(p1.Places.Single(x => x.Way == "front-door").Chip, Does.EndWith(HandoverHost.MissionClock(g.S.ValeExits * Game.Hz)), "the door's chip carries Popovic's exit, from the mission's own clock");
        Assert.That(p1.Places.Single(x => x.Way == "pickup").Chip, Does.EndWith(HandoverHost.MissionClock(g.S.ValeAtCar * Game.Hz)));
        var p2 = Page(h, 2); Assert.That(p2.Dangers.Select(x => x.Id), Is.EqualTo(new[] { "guard", "cameras", "rivals", "patrol", "police" }));
        Assert.That(p2.Dangers.Single(x => x.Id == "rivals").When, Is.EqualTo("2:35"));
        Assert.That(g.ClosingHash(), Is.EqualTo(before), "the pages only read the run");
    }

    [Test] public void TheSquaresCamerasSeeMostOfTheFrontDoorRoutesWork()
    {
        // Sky's hint (2026-11-11): "the main square cameras observe most of the actions so should be watched early". The row's view is a world camera the site really has (the
        // Tech's cabinet loops the city's own cameras), and by the engine's own sight it has Inez, whom the front-door route works through, in view most of the mission.
        var h = Host.Fresh(); var g = h.Game; var row = Page(h, 2).Dangers.Single(x => x.Id == "cameras");
        Assert.That(row.View, Does.StartWith("camera/")); var id = row.View[7..];
        var cam = g.S.Cameras.SingleOrDefault(c => c.Id == id); Assert.That(cam, Is.Not.Null, $"no camera '{id}' on the site"); Assert.That(cam.World, "the square's mast is the city's own camera");
        Assert.That(cam.Facings.Length, Is.GreaterThanOrEqualTo(3), "its three heads: the cameras, plural");
        var inez = g.PersonByRole("waitress"); int seen = 0, n = 0;
        for (int t = 0; t < 330 * Game.Hz && !g.Over; t += 5 * Game.Hz) { if (t > g.Tick) h.RunTo(t); if (g.PosOfPerson(inez) is Pt at) { n++; if (g.InSight("camera:" + id, at, false)) seen++; } }
        TestContext.Progress.WriteLine($"Brief108: the square's cameras ({id}) have Inez in view {seen} of {n} samples, 0:00 to 5:30");
        Assert.That(n, Is.GreaterThan(20)); Assert.That(seen * 2, Is.GreaterThan(n), $"the square's cameras had Inez in view {seen} of {n} times before 5:30: the hint would not be true");
    }

    [Test] public void AWaysChipOpensItsStepsInTheListsPlace()
    {
        var h = Host.Fresh(); var p = Page(h, 1, wayOpen: "front-door"); var r = BookLayout.Place(p, Tw);
        Assert.That(r.Of(BookSlot.Place).Select(i => p.Places[i.Index].Way), Is.EqualTo(new[] { "front-door" }), "the open way's row alone");
        var shown = r.Of(BookSlot.Needs).Select(i => p.Needs[i.Index]).ToList();
        Assert.That(shown.Select(n => n.Id), Is.EqualTo(p.Needs.Where(n => n.Group == "front-door").Select(n => n.Id)), "every step of the front door, in order");
        foreach (var n in shown) Assert.That(n.Met, Is.EqualTo(!n.Unknown && h.BookCheck(BookWordsFile.Get().Needs["front-door"].Single(x => x.Id == n.Id).Check) == true), $"{n.Id}: its mark is its live check");
        Fits(p, "the front door's steps"); Fits(Page(h, 1, wayOpen: "pickup"), "the pickup's steps");
    }

    [Test] public void InPlayThePagesKeepBackAndNoBegin()
    {
        var h = Host.Fresh();
        for (int st = 0; st < 3; st++) { var p = Page(h, st, start: false); var r = BookLayout.Place(p, Tw); Assert.That(r.Has(BookSlot.Begin), Is.False); Assert.That(p.Skip, Is.False); Assert.That(r.Has(BookSlot.Pager)); }
    }

    [Test] public void SkipToBeginOnlyAfterAFirstPlay()
    {
        var h = Host.Fresh();
        try
        {
            HandoverHost.BriefSeenPin = false; Assert.That(Page(h, 0).Skip, Is.False, "a first play reads the pages");
            HandoverHost.BriefSeenPin = true; Assert.That(Page(h, 0).Skip, "after a first play: the quiet Skip to Begin"); Assert.That(Page(h, 1).Skip, Is.False, "only on the mission page");
            var r = BookLayout.Place(Page(h, 0), Tw); Assert.That(r.Has(BookSlot.Begin), Is.False, "Skip is a link, not a second Begin");
        }
        finally { HandoverHost.BriefSeenPin = null; }
    }
}
