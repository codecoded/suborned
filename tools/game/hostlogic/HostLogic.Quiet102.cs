// Brief 102, natural words (Sky 2026-11-08, on the agents' barks: "Maybe even drop them? They will feel repetitive"): the cell is quiet in play. One rule in the host
// (HostQuiet: CellQuiet, its moments in CellSpeaks): no bubble, log or card line from the cell but a scene's; the world's people speak as before, and the run is the same.
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using Order = Suborned.Sim.Order;

public class Quiet102
{
    const string R = "contracts/missions/handover/routes/";
    /// <summary>The routes between them: the front door and the pickup, Pressure, a takedown, a loud fight, a fail, the rivals at the car, a withdrawal.</summary>
    static readonly string[] Routes = { "route-a", "route-b", "route-a-pressure", "route-g", "loud", "fail-enforcer-down", "fight-safe-unguarded", "withdraw" };
    static bool Cell(string sp) => sp is "scout" or "fixer" or "tech" or "enforcer";

    /// <summary>A route played through the host's own order paths (Why100.Play), every bubble kept as it comes (the host keeps only its last 12).</summary>
    static (HandoverHost h, List<HandoverHost.Bubble> bubbles) Play(string route)
    {
        var seen = new List<HandoverHost.Bubble>(); var known = new HashSet<HandoverHost.Bubble>();
        var h = Why100.Play(R + route + ".json", -1, x => { foreach (var b in x.Bubbles) if (known.Add(b)) seen.Add(b); });
        foreach (var b in h.Bubbles) if (known.Add(b)) seen.Add(b);
        return (h, seen);
    }

    /// <summary>The words a scene may give the cell this run: the conversations' beats and street lines, the scenes' cards, and the lines of the moments CellSpeaks keeps.</summary>
    static HashSet<string> SceneWords(HandoverHost h)
    {
        var w = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in h.Text.Conversations) { foreach (var b in c.Beats) w.Add(b.Line); foreach (var s in c.Street) foreach (var b in s.Beats) w.Add(b.Line); }
        foreach (var c in h.Feed.Concat(h.FeedHistory)) if (c.Scene != null) foreach (var l in c.Lines) w.Add(l.Text);
        foreach (var l in h.CellLines) if (l.Said) w.Add(l.Text);
        return w;
    }

    [Test] public void TheCellSpeaksOnlyInScenesOnEveryRoute()
    {
        var bad = new List<string>(); int quiet = 0;
        foreach (var r in Routes)
        {
            var (h, bubbles) = Play(r); var ok = SceneWords(h); quiet += h.CellLines.Count(l => !l.Said);
            foreach (var l in h.CellLines) if (l.Said && !HandoverHost.CellSpeaks.Contains(l.Moment)) bad.Add($"{r}: said at {l.Moment}: {l.Text}");
            foreach (var t in h.Talk) if (Cell(t.Speaker) && !ok.Contains(t.Text)) bad.Add($"{r}: the log has the {t.Speaker} at {HandoverHost.MissionClock(t.Tick)}: '{t.Text}'");
            foreach (var b in bubbles) if (Cell(b.Speaker) && !ok.Contains(b.Text)) bad.Add($"{r}: a bubble over the {b.Speaker} at {HandoverHost.MissionClock(b.Tick)}: '{b.Text}'");
            foreach (var c in h.Feed.Concat(h.FeedHistory)) if (c.Scene == null) foreach (var l in c.Lines) if (Cell(l.Speaker) && !ok.Contains(l.Text)) bad.Add($"{r}: the card '{c.Title}' has the {l.Speaker}: '{l.Text}'");
        }
        Assert.That(bad, Is.Empty, string.Join("\n", bad.Take(12)));
        Assert.That(quiet, Is.GreaterThan(20), "the rule hardly met a line: is the cell still routed through Say?");
    }

    /// <summary>"Keep the world's people's barks as they are": the old chatter on ("*") and off, the same run, the same people's lines at the same ticks, the same cards.</summary>
    [Test] public void PeopleSpeakAsBeforeAndTheRunIsTheSame()
    {
        static string People(HandoverHost h) => string.Join("\n", h.Talk.Where(t => !Cell(t.Speaker)).Select(t => $"{t.Tick} {t.Speaker}: {t.Text}"));
        static string Cards(HandoverHost h) => string.Join("\n", h.Feed.Concat(h.FeedHistory).OrderBy(c => c.Id).Select(c => $"{c.Id} {c.Kind} {c.Title} · {c.Footer} · {string.Join(" | ", c.Lines.Where(l => !Cell(l.Speaker)).Select(l => l.Text))}"));
        foreach (var r in new[] { "route-a", "route-b", "loud", "fail-vale-dead" })
        {
            HandoverHost old; HandoverHost.CellSpeaks.Add("*");
            try { old = Play(r).h; } finally { HandoverHost.CellSpeaks.Remove("*"); }
            var now = Play(r).h;
            Assert.That(now.Game.Checkpoints[^1].hash, Is.EqualTo(old.Game.Checkpoints[^1].hash), $"{r}: the words changed the run");
            Assert.That(old.Talk.Count, Is.LessThan(200), $"{r}: the old log was cut, so it can't be compared");
            Assert.That(People(now), Is.EqualTo(People(old)), $"{r}: the people's lines");
            Assert.That(Cards(now), Is.EqualTo(Cards(old)), $"{r}: the cards");
            Assert.That(old.Talk.Count(t => Cell(t.Speaker)), Is.GreaterThan(now.Talk.Count(t => Cell(t.Speaker))), $"{r}: the old chatter didn't come back with '*'");
        }
    }

    [Test] public void OneKeyGivesAMomentItsLineBack()
    {
        var quiet = Play("route-a").h;
        Assert.That(quiet.CellLines.Exists(l => l.Moment == "done/loop" && l.Speaker == "tech" && !l.Said), "the Tech's loop landing is kept quiet");
        Assert.That(quiet.Talk.Exists(t => t.Speaker == "tech"), Is.False, "the Tech says nothing on route A");
        HandoverHost.CellSpeaks.Add("done/loop");
        try
        {
            var back = Play("route-a").h; var l = back.CellLines.Find(x => x.Moment == "done/loop");
            Assert.That(l?.Said == true, "with its key, the moment speaks");
            Assert.That(back.Talk.Exists(t => t.Speaker == "tech" && t.Text == l.Text), "and its line is said");
        }
        finally { HandoverHost.CellSpeaks.Remove("done/loop"); }
    }

    /// <summary>Route A: the Listen, the Help and the Call keep the cell's lines; the Enforcer bringing Popovic out speaks, and Popovic answers.</summary>
    [Test] public void TheScenesKeepTheCellsVoice()
    {
        var h = Play("route-a").h; var g = h.Game; var cards = h.Feed.Concat(h.FeedHistory).ToList();
        Assert.That(cards.First(c => c.Verb == "listen").Lines.Exists(l => l.Speaker == "scout"), "the Listen scene");
        Assert.That(cards.First(c => c.Verb == "help").Lines.Exists(l => l.Speaker == "fixer"), "the Help scene");
        Assert.That(cards.First(c => c.Kind == "call").Lines.Exists(l => l.Speaker == "fixer"), "the call");
        var o = g.Log.Find(l => l.Code == "enter.out"); Assert.That(o.Code, Is.EqualTo("enter.out"), "route A brings Popovic out");
        Assert.That(h.Talk.Exists(t => t.Speaker == "enforcer" && Math.Abs(t.Tick - o.Tick) <= 1), "the Enforcer taking Popovic");
        Assert.That(h.Talk.Exists(t => t.Speaker == "councillor" && Math.Abs(t.Tick - o.Tick) <= 1), "and Popovic's reply");
    }

    /// <summary>Route B's Mislead is a scene with a person: the Scout's tip to the rival is said (arrive/mislead, Aster's default); her report after it is a landing (quiet);
    /// the rival's answer is his own. Nothing else of hers is said on route B.</summary>
    [Test] public void AMisleadKeepsTheTipAndTheRivalsAnswerNotTheScoutsReport()
    {
        var h = Play("route-b").h; var m = h.Game.Log.Find(l => l.Code == "rivals.misled"); Assert.That(m.Code, Is.EqualTo("rivals.misled"), "route B's Mislead lands");
        Assert.That(h.ConvoPicks, Does.Contain("mislead/rival-4"));
        Assert.That(h.CellLines.Exists(l => l.Moment == "done/mislead" && !l.Said), "her report is kept quiet");
        Assert.That(h.CellLines.Exists(l => l.Moment == "arrive/mislead" && l.Speaker == "scout" && l.Said), "her tip to the rival is said");
        Assert.That(h.Talk.Exists(t => t.Speaker == "rivals" && Math.Abs(t.Tick - m.Tick) <= 1), "the rival answers");
        var tips = h.CellLines.Where(l => l.Said && l.Moment == "arrive/mislead").Select(l => l.Text).ToHashSet();
        Assert.That(h.Talk.Where(t => t.Speaker == "scout").Select(t => t.Text), Is.All.Matches<string>(tips.Contains), "the Scout says only her tip on route B");
    }

    /// <summary>An order given by a click: no acknowledgement, the stock "On it." included.</summary>
    [Test] public void AnOrderTakenSaysNothing()
    {
        var h = Host.Fresh(); h.GiveWatchScripted(OpId.Scout, "waitress"); Host.Tick(h, 2);
        Assert.That(h.CellLines.Exists(l => l.Moment == "ack/watch" && l.Speaker == "scout" && !l.Said), "the Watch's acknowledgement met the rule");
        Host.Call(h, "SayOrder", OpId.Tech, new Order { Op = "tech", Do = "no-such-verb" }, null);
        Assert.That(h.CellLines.Exists(l => l.Moment == "ack/no-such-verb" && l.Text == "On it." && !l.Said), "the stock line met the rule");
        Assert.That(h.Talk.Exists(t => Cell(t.Speaker)), Is.False); Assert.That(h.Bubbles.Exists(b => Cell(b.Speaker)), Is.False);
    }
}
