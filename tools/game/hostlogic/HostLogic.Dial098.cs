// Brief 098 step 0, the dial's host side (HostDial, HudToolkit.Dial; Sky 2026-11-07: "add 3 more guards, take some away... the game then just does that"): which counts a game
// plays, the panel's numbers, Play and Begin, Replay, the saved run and the -counts switch. Same harness as HostLogic.cs: the real engine with the bundle's population.json,
// the real text.json, no views. Laws, not moments: every number is read from Population.Today, never written here.
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;

public class Dial098
{
    static HandoverHost AtStart() { var h = Host.Fresh(); Host.Set(h, "ShowBriefing", true); return h; } // the start page up over the stopped street
    /// <summary>The harness stubs HostViews' constructor (Patch.cs) and Host.Fresh fills its collections once; a Begin of the dial's (Play, Replay) makes new views, filled here the same way.</summary>
    static void Views(HandoverHost h)
    {
        var views = Host.Get(h, "views");
        foreach (var f in views.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            if (f.GetValue(views) == null && f.FieldType.IsGenericType && f.FieldType.GetConstructor(Type.EmptyTypes) != null) f.SetValue(views, Activator.CreateInstance(f.FieldType));
    }
    static SortedDictionary<string, int> Counts(params (string kind, int n)[] kv) { var d = new SortedDictionary<string, int>(StringComparer.Ordinal); foreach (var (k, n) in kv) d[k] = n; return d; }

    [Test] public void UnsetPlaysTodaysWorldAndThePanelShowsIt()
    {
        var h = Host.Fresh(); var g = h.Game;
        Assert.That(g.Start.Counts, Is.Empty, "no counts: the start hashes as before the dial"); Assert.That(g.DialOn, Is.False); Assert.That(g.Census, Is.Empty);
        Assert.That(g.Walkers.Any(w => w.Added), Is.False, "nobody added");
        Assert.That(h.DialToday.Select(c => c.Kind), Is.EqualTo(Population.Kinds), "the panel's kinds are the engine's");
        foreach (var c in h.DialToday)
        {
            Assert.That(h.DialShown(c.Kind), Is.EqualTo(c.Today)); Assert.That(c.Max, Is.GreaterThan(c.Today));
            if (c.Kind == Population.Cars) Assert.That((c.Today, c.Floor), Is.EqualTo((0, 0)), "step 1: no passing traffic today, and no car is a tenet: today 0, the fewest 0");
            else Assert.That(c.Floor, Is.InRange(1, c.Today), "the cast never leave: each people kind has a floor of at least 1");
        }
        Assert.That(h.DialPending, Is.False);
    }

    [Test] public void ThePanelKeepsEachNumberBetweenItsFloorAndItsMost()
    {
        var h = AtStart(); var t = h.DialTodayOf("guards"); var g0 = h.Game;
        h.DialStep("guards", -100000); Assert.That(h.DialShown("guards"), Is.EqualTo(t.Floor), "the cast never leave");
        h.DialStep("guards", 100000); Assert.That(h.DialShown("guards"), Is.EqualTo(t.Max));
        Assert.That(h.DialPending); Assert.That(h.Game, Is.SameAs(g0), "a click on the panel builds nothing");
        h.DialReset(); Assert.That(h.DialShown("guards"), Is.EqualTo(t.Today)); Assert.That(h.DialPending, Is.False, "back at today's: nothing to build");
        h.DialPlay(); Assert.That(h.Game, Is.SameAs(g0), "unchanged numbers: Play begins the street as it is"); Assert.That(h.ShowBriefing, Is.False);
    }

    [Test] public void PlayBuildsTheStreetWithTheNumbersOnTheSameSeed()
    {
        var h = AtStart(); var g0 = h.Game; var tg = h.DialTodayOf("guards"); var tp = h.DialTodayOf("people");
        h.DialStep("guards", 3); h.DialStep("people", 10); h.DialStep("people", 10); h.DialStep("people", 10); h.DialStep("people", 6);
        Assert.That(h.DialShown("guards"), Is.EqualTo(tg.Today + 3)); Assert.That(h.DialShown("people"), Is.EqualTo(tp.Today + 36));
        h.DialPlay();
        var g = h.Game; Assert.That(h.Fault, Is.Null, h.Status); Assert.That(g, Is.Not.SameAs(g0));
        Assert.That(g.Seed, Is.EqualTo(g0.Seed), "the same street: only the numbers change");
        Assert.That(g.Start.Counts, Is.EqualTo(Counts(("guards", tg.Today + 3), ("people", tp.Today + 36))));
        Assert.That(h.ShowBriefing, Is.False, "Play begins"); Assert.That(h.DialPending, Is.False);
        var cg = g.Census.Single(c => c.Kind == "guards"); var cp = g.Census.Single(c => c.Kind == "people");
        Assert.That(cg.Count, Is.EqualTo(tg.Today + 3)); Assert.That(cg.Added, Is.EqualTo(new[] { "x-guard-001", "x-guard-002", "x-guard-003" })); Assert.That(cg.Removed, Is.Empty);
        Assert.That(cp.Added.Count, Is.EqualTo(36)); Assert.That(cp.Added[0], Is.EqualTo("x-person-0001"));
        var added = g.Walkers.Where(w => w.Added).ToList(); Assert.That(added.Count, Is.EqualTo(39));
        Assert.That(added.Where(w => w.P.Job == "guard").Select(w => HostViews.FamilyOf(w.P.Job)).Distinct(), Is.EqualTo(new[] { "guard" }), "an added guard dresses as a guard");
        Assert.That(added.Where(w => w.P.Job != "guard").Select(w => HostViews.FamilyOf(w.P.Job)).Distinct(), Is.EqualTo(new[] { "passer" }), "an added person dresses as a passer-by");
        Assert.That(g.Walkers.Single(w => w.P.Id == "x-guard-001").P.Name, Is.EqualTo("Guard 1"));
        Assert.That(HandoverHost.CensusLine(cg), Does.Contain("x-guard-001..x-guard-003").And.Contain($"plays {tg.Today + 3}"));
    }

    [Test] public void BeginWithChangedNumbersBuildsThemFirst()
    {
        var h = AtStart(); var g0 = h.Game; h.DialStep("guards", 2);
        h.BriefingBegin();
        Assert.That(h.Game, Is.Not.SameAs(g0), "the page's Begin never starts a street the panel no longer shows");
        Assert.That(h.Game.Census.Single(c => c.Kind == "guards").Count, Is.EqualTo(h.DialTodayOf("guards").Today + 2)); Assert.That(h.ShowBriefing, Is.False);
    }

    [Test] public void FewerTakesNoTenetAndLeavesNoWalkerBehind()
    {
        var h = AtStart(); var tg = h.DialTodayOf("guards"); var tp = h.DialTodayOf("people");
        h.DialStep("guards", -(tg.Today - 8)); h.DialStep("people", -(tp.Today - 60)); h.DialPlay();
        var g = h.Game; var gone = g.Census.SelectMany(c => c.Removed).ToHashSet();
        Assert.That(gone.Count, Is.EqualTo(tg.Today - 8 + tp.Today - 60));
        Assert.That(g.Walkers.Any(w => gone.Contains(w.P.Id)), Is.False, "nobody who left has a walker (so no view)");
        foreach (var c in g.S.Cast) Assert.That(gone.Contains(c.Id), Is.False, $"the cast never leave: {c.Role}");
        Assert.That(g.Walkers.Count, Is.EqualTo(8 + 60 - g.S.Cast.Count), "the walkers are the zone's counts less the cast, who are drawn by role");
    }

    [Test] public void ReplayKeepsTheSeedAndTheCounts()
    {
        var h = AtStart(); h.DialStep("guards", 3); h.DialPlay(); var g1 = h.Game;
        h.BeginSameSeed(); var g2 = h.Game;
        Assert.That(g2.Seed, Is.EqualTo(g1.Seed)); Assert.That(g2.Start.Counts, Is.EqualTo(g1.Start.Counts)); Assert.That(g2.StartHash, Is.EqualTo(g1.StartHash), "the same street");
        var h0 = Host.Fresh(); var u = h0.Game; h0.BeginSameSeed();
        Assert.That(h0.Game.Start.Counts, Is.Empty, "a replay of today's world stays today's"); Assert.That(h0.Game.StartHash, Is.EqualTo(u.StartHash));
    }

    [Test] public void TheSavedRunCarriesTheCountsAndReplaysExactly()
    {
        var h = AtStart(); h.DialStep("guards", 5); h.DialStep("people", 20); h.DialPlay(); Views(h);
        Host.Tick(h, 600); var g = h.Game; g.Finish();
        var sc = Script.Parse(Script.Write(g, h.PlayInputs.Hash));
        Assert.That(sc.Start.Counts, Is.EqualTo(g.Start.Counts)); Assert.That(sc.Start.Seed, Is.EqualTo(g.Seed));
        var r = Engine.Play(h.PlayInputs, sc.Start, sc.Orders, sc.Until);
        Assert.That(sc.Verify(r, h.PlayInputs.Hash), Is.Empty, "seed + counts + orders replay");
    }

    /// <summary>The hover and the cones read an added person through the engine, as anyone: no baked seat, figure or name table is asked for their id.</summary>
    [Test] public void AddedPeopleHoverAndConeLikeAnyone()
    {
        var h = AtStart(); h.DialStep("guards", 2); h.DialStep("people", 10); h.DialPlay(); Views(h); Host.Tick(h, 600); var g = h.Game;
        foreach (var w in g.Walkers.Where(w => w.Added).Take(12))
        {
            int i = g.Walkers.IndexOf(w); bool sec = !w.P.Civilian;
            Assert.That(sec, Is.EqualTo(w.P.Job == "guard"), $"{w.P.Id}: an added guard is security, a passer-by a civilian");
            var t = new HandoverHost.Target { Kind = "crowd", Key = "crowd/" + i, Id = w.P.Id, Label = sec ? "a guard" : "a passer-by", Security = sec }; // as TargetOf builds it from the view crowd/i
            var plan = h.ContextFor(t, new UnityEngine.Vector2(100, 100)); Assert.That(plan, Is.Not.Null, w.P.Id);
            Assert.That(h.PersonName(w.P), Is.EqualTo(sec ? "a guard" : "a passer-by").Or.Not.Empty, w.P.Id); Host.NoTokens(w.P.Id, h.PersonName(w.P));
            var cone = g.PersonCone(w.P.Id); Assert.That(cone.Kind, Is.EqualTo(sec ? "ring" : "wedge").Or.EqualTo("none"), $"{w.P.Id}: security sees all round, a passer-by in a cone");
            Host.Set(h, "Hover", t); Host.Call(h, "ConesCore"); Assert.That(h.Fault, Is.Null, $"{w.P.Id}: the cones with the hover on them: {h.Status}");
        }
        Host.Set(h, "Hover", null);
    }

    /// <summary>A street rebuilt with fewer after more: the game holds no walker of anyone who left (the views are built from the walkers at every start; the player probe,
    /// ProofDial, counts them in the scene, as the harness stubs the views).</summary>
    [Test] public void FewerAfterMoreKeepsNoWalkerOfAnyoneWhoLeft()
    {
        var h = AtStart(); h.DialStep("guards", 10); h.DialStep("people", 40); h.DialPlay(); Views(h);
        Host.Set(h, "ShowBriefing", true);
        h.DialStep("guards", -(h.DialShown("guards") - 8)); h.DialStep("people", -(h.DialShown("people") - 60)); h.DialPlay(); Views(h);
        var g = h.Game; var gone = g.Census.SelectMany(c => c.Removed).ToHashSet();
        Assert.That(g.Census.Single(c => c.Kind == "guards").Count, Is.EqualTo(8)); Assert.That(g.Census.Single(c => c.Kind == "people").Count, Is.EqualTo(60));
        Assert.That(g.Walkers.Any(w => w.Added), Is.False, "fewer than today adds nobody: the added of the street before are gone");
        Assert.That(g.Walkers.Any(w => gone.Contains(w.P.Id)), Is.False);
        Host.Tick(h, 300); Assert.That(h.Fault, Is.Null, h.Status);
    }

    [Test] public void TheCountsSwitchPinsEveryNewGameAndRejectsWhatTheDialDoesNotBuild()
    {
        Assert.That(HandoverHost.ParseCounts("guards=19,people=150"), Is.EqualTo(Counts(("guards", 19), ("people", 150))));
        Assert.That(HandoverHost.ParseCounts(" guards:0 "), Is.EqualTo(Counts(("guards", 0))));
        Assert.That(HandoverHost.ParseCounts("cars=16"), Is.EqualTo(Counts(("cars", 16))), "step 1: cars are a kind of the dial (passing traffic)");
        Assert.Throws<FormatException>(() => HandoverHost.ParseCounts("trucks=3"), "unknown = reject");
        Assert.Throws<FormatException>(() => HandoverHost.ParseCounts("guards=-1"));
        Assert.Throws<FormatException>(() => HandoverHost.ParseCounts("guards"));
        try
        {
            HandoverHost.CountsPin = Counts(("guards", 0));
            var h = Host.Fresh(); var c = h.Game.Census.Single(x => x.Kind == "guards");
            Assert.That(h.Game.Start.Counts, Is.EqualTo(Counts(("guards", 0))), "the switch's counts as given");
            Assert.That(c.Count, Is.EqualTo(c.Floor)); Assert.That(c.Note, Is.Not.Empty, "the census says why it plays the floor");
            Assert.That(h.DialShown("guards"), Is.EqualTo(c.Floor), "the panel shows what plays"); Assert.That(h.DialPending, Is.False);
        }
        finally { HandoverHost.CountsPin = null; }
    }
}
