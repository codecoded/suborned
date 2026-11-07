// Brief 098 step 1, passing traffic in the host (HostDial's Cars row, HostTraffic, HostViews.Traffic; Sky 2026-11-07: "our roads are empty, no reason can't have passing
// traffic"; "bring in more cars etc - the game then just does that"): the dial's cars, the -counts switch, a passing car as a car target, and the hover's plain words.
// Same harness as HostLogic.cs: the real engine with the bundle's population.json and handover.lanes.json, no views (HostViews is silent here; the player probe,
// ProofTraffic, draws them). Laws, not moments: every number is read from Population.Today or the engine, never written here.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;

public class Traffic098
{
    static HandoverHost AtStart() { var h = Host.Fresh(); Host.Set(h, "ShowBriefing", true); return h; }
    /// <summary>As Dial098.Views: a Begin of the dial's makes new (silent) views, their collections filled here the way Host.Fresh fills them.</summary>
    static void Views(HandoverHost h)
    {
        var views = Host.Get(h, "views");
        foreach (var f in views.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (f.GetValue(views) == null && f.FieldType.IsGenericType && f.FieldType.GetConstructor(Type.EmptyTypes) != null) f.SetValue(views, Activator.CreateInstance(f.FieldType));
    }
    static SortedDictionary<string, int> Counts(params (string kind, int n)[] kv) { var d = new SortedDictionary<string, int>(StringComparer.Ordinal); foreach (var (k, n) in kv) d[k] = n; return d; }

    [Test] public void TheCarsRowStartsAtNoneAndPlayBuildsTraffic()
    {
        var h = AtStart(); var g0 = h.Game; var t = h.DialTodayOf(Population.Cars);
        Assert.That(t, Is.Not.Null, "the panel has a Cars row"); Assert.That((t.Today, t.Floor), Is.EqualTo((0, 0))); Assert.That(t.Max, Is.GreaterThan(0));
        Assert.That(g0.TrafficOn, Is.False, "no counts: no traffic"); Assert.That(g0.TrafficNow(), Is.Empty);
        h.DialStep(Population.Cars, -1); Assert.That(h.DialShown(Population.Cars), Is.EqualTo(0), "never below none"); Assert.That(h.DialPending, Is.False);
        h.DialStep(Population.Cars, 100000); Assert.That(h.DialShown(Population.Cars), Is.EqualTo(t.Max), "never above the most");
        h.DialReset(); h.DialStep(Population.Cars, 10); h.DialStep(Population.Cars, -1); h.DialStep(Population.Cars, -1);
        Assert.That(h.DialShown(Population.Cars), Is.EqualTo(8)); Assert.That(h.DialPending); Assert.That(h.Game, Is.SameAs(g0), "a click builds nothing");
        h.DialPlay(); var g = h.Game;
        Assert.That(h.Fault, Is.Null, h.Status); Assert.That(g, Is.Not.SameAs(g0)); Assert.That(g.Seed, Is.EqualTo(g0.Seed), "the same street: only the numbers change");
        Assert.That(g.Start.Counts, Is.EqualTo(Counts((Population.Cars, 8))), "only the kind that changed is set");
        Assert.That(g.TrafficOn, "Play builds traffic"); Assert.That(g.TrafficNow(), Is.Not.Empty, "the roads are full at 0:00 (the warm-up ran)");
        Assert.That(g.Census.Single(c => c.Kind == Population.Cars).Count, Is.EqualTo(8));
        Assert.That(h.DialShown(Population.Cars), Is.EqualTo(8), "the panel shows what plays");
        Views(h); Host.Tick(h, 300); Assert.That(h.Fault, Is.Null, h.Status);
    }

    [Test] public void CarsBackToNoneIsTodaysWorldAndReplayKeepsThem()
    {
        var h = AtStart(); var start0 = h.Game.StartHash;
        h.DialStep(Population.Cars, 16); h.DialPlay(); var g1 = h.Game; Assert.That(g1.TrafficOn);
        h.BeginSameSeed(); var g2 = h.Game;
        Assert.That(g2.Start.Counts, Is.EqualTo(g1.Start.Counts), "Replay keeps the cars"); Assert.That(g2.StartHash, Is.EqualTo(g1.StartHash), "the same street, the same traffic");
        Assert.That(g2.TrafficNow().Select(v => (v.Id, v.At)), Is.EqualTo(g1.TrafficNow().Select(v => (v.Id, v.At))), "every car where it was");
        Host.Set(h, "ShowBriefing", true); h.DialStep(Population.Cars, -16); h.DialPlay(); var g3 = h.Game;
        Assert.That(g3.Start.Counts, Is.Empty, "cars back at none: unset"); Assert.That(g3.TrafficOn, Is.False); Assert.That(g3.StartHash, Is.EqualTo(start0), "today's world byte for byte");
    }

    [Test] public void APassingCarIsACarTargetAndTheMissionCarsAreNot()
    {
        HandoverHost.Target T(string kind, string key) => new HandoverHost.Target { Kind = kind, Key = key };
        Assert.That(HandoverHost.IsTrafficCar(T("car", "t-003-02")), Is.True);
        foreach (var t in new[] { T("car", "official"), T("car", "getaway"), T("car", "range-1"), T("rivals", "car:rivals"), T("person", "guard"), T("car", ""), null })
            Assert.That(HandoverHost.IsTrafficCar(t), Is.False, $"{t?.Kind} {t?.Key}");
        var h = AtStart(); h.DialStep(Population.Cars, 8); h.DialPlay(); var g = h.Game;
        foreach (var v in g.TrafficNow())
        {
            Assert.That(v.Id, Does.StartWith("car:"), "a car target's id"); Assert.That(g.CarAt(v.Id), Is.EqualTo(v.At), $"{v.Id}: the engine's own place for it");
            Assert.That(v.Model, Is.EqualTo("sedan").Or.EqualTo("van").Or.EqualTo("scooter")); Assert.That(v.Heading.X != 0 || v.Heading.Y != 0, $"{v.Id}: a heading");
        }
        var any = g.TrafficNow().First(); var tt = T("car", any.Id.Substring(4));
        Assert.That(h.TargetWorld(tt), Is.Null, "the views are silent in the harness: no drawn place (ProofTraffic draws them)");
    }

    /// <summary>The hover's words from the engine's TrafficWhy (its parts as GameTrafficHost.cs writes them), in plain words: stopped, slow, or on its way.</summary>
    [Test] public void TheHoverSaysWhatHoldsACarInPlainWords()
    {
        var m = typeof(HandoverHost).GetMethod("HeldWords", BindingFlags.NonPublic | BindingFlags.Static); Assert.That(m, Is.Not.Null);
        string W(string why, bool stopped) => (string)m.Invoke(null, new object[] { why, stopped });
        Assert.That(W("a person 2.4 m ahead", true), Is.EqualTo("waiting for someone to cross"));
        Assert.That(W("a person 9.0 m ahead", false), Is.EqualTo("slowing for someone in the road"));
        Assert.That(W("a person 3.0 m ahead (will not move on)", true), Is.EqualTo("going round someone in the road"));
        Assert.That(W("a car 6.0 m ahead", true), Is.EqualTo("queuing behind a car"));
        Assert.That(W("a car 6.0 m ahead (will not move on)", true), Is.EqualTo("stuck behind a stopped car"));
        Assert.That(W("a body 4.0 m ahead (will not move on)", true), Is.EqualTo("stopped for a body in the road"));
        Assert.That(W("a mission car's way 5.0 m ahead", true), Is.EqualTo("giving way to a car"));
        Assert.That(W("turn j2-n-e 3.5 m ahead: held:car:t-004-01", true), Is.EqualTo("waiting at the junction"));
        Assert.That(W("turn j2-n-e 3.5 m ahead: free", true), Is.EqualTo(""), "a free turn holds nobody");
        Assert.That(W("the road in is closed", true), Is.EqualTo("the roads are closed"));
        Assert.That(W("", true), Is.EqualTo(""));
        foreach (var w in new[] { "a person 2.4 m ahead", "a car 6.0 m ahead (will not move on)", "turn j2 1.0 m ahead: held:car:t-001-01" }) Host.NoTokens(w, W(w, true));
    }
}
