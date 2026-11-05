// Brief 096 (Sky 2026-11-04: "the End card needs to be plain english and clear but not too formal"): the end card by the engine's own reason, the story to the end,
// the book's getaway checks (car-guarded, left-safe) and the held-car feed card once a walk. Same harness as HostLogic.cs (the real engine, the real text.json).
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;

/// <summary>The end card, worded by Game.Cause (Fable's debrief.cause and debrief.story).</summary>
public class EndCause
{
    /// <summary>The engine's own scenario for Sky's run (seed 1562486586): Vale boarded, the car held, nobody outside drawn, the rivals walk up and take him.</summary>
    static Game Unguarded(HandoverHost h)
    {
        var s = Scenarios.Find("K3-PRESS"); Assert.That(s, Is.Not.Null, "the engine's K3-PRESS scenario is gone");
        var g = Scenarios.Run(h.PlayInputs, s).G;
        Assert.That(g.FailCode, Is.EqualTo("rivals-reach-vale"), $"{g.Result} {g.FailCode}"); Assert.That(g.Cause?.Why, Is.EqualTo("reached-car"));
        return g;
    }
    /// <summary>A played run adopted as the host's ended run, as ShowEnded does without its views and fail camera.</summary>
    static List<string> CardOf(HandoverHost h, Game g)
    {
        Host.Set(h, "Game", g); Host.Set(h, "EndDebrief", Debrief.Of(g)); Host.Set(h, "Ending", "test");
        var m = new HudModel(); Host.Call(m, "EndCard", h); return m.EndLines;
    }

    [Test] public void AnUnguardedHeldCarSaysTheRivalsTookHimFromTheCarAndTheStoryRunsToTheEnd()
    {
        var h = Host.Fresh(); var g = Unguarded(h); var t = h.Text; var ct = t.CauseFor("rivals-reach-vale", "reached-car");
        Assert.That(ct, Is.Not.Null, "text.json has no debrief.cause.rivals-reach-vale.reached-car");
        var holders = g.CarHolders(); var lines = CardOf(h, g);
        Assert.That(lines[0], Is.EqualTo(ct.Head.TrimEnd('.')), "the head is the cause's, not less.end.fail's"); Assert.That(lines[0], Is.EqualTo("Rivals took Popovic from the car"));
        Assert.That(lines[1], Is.EqualTo(ct.Line)); Assert.That(lines[1], Does.Not.Contain("got to Popovic first"));
        string reached = t.Route["vale-reaches-getaway-car"], route = lines.FirstOrDefault(l => l.Contains(reached)) ?? "";
        Assert.That(route, Is.Not.Empty, $"no route line with '{reached}': {string.Join(" | ", lines)}");
        string after = route[(route.IndexOf(reached, StringComparison.Ordinal) + reached.Length)..].Trim(), unguarded = t.DebriefStory["unguarded"];
        if (holders.Count > 0) Assert.That(after, Does.StartWith(Cap(t.DebriefStory["blocked"].Replace("{holders}", t.HoldersPhrase(string.Join(",", holders))))), $"holders {string.Join(",", holders)}: '{after}'");
        else Assert.That(after, Does.Not.Contain("blocked"), "no holders at the end: the blocked line is left out");
        Assert.That(after, Does.Contain(unguarded)); Host.NoTokens("end card", lines.ToArray());
        Console.WriteLine($"K3-PRESS card: {lines[0]} · {lines[1]} · …{after}  (holders {string.Join(",", holders)})");
    }

    [Test] public void TheBlockedLineNamesTheHoldersAndIsLeftOutWithoutThem()
    {
        var h = Host.Fresh(); var c = new FailCause { Fail = "rivals-reach-vale", Why = "reached-car" };
        var two = h.CauseWords(c, new[] { "rival-1", "rival-2" }); var none = h.CauseWords(c, new string[0]);
        Assert.That(two.Head, Is.EqualTo("Rivals took Popovic from the car"));
        Assert.That(two.Story, Is.EqualTo(new[] { "2 rivals blocked the car, so Go wouldn't work.", h.Text.DebriefStory["unguarded"] }));
        Assert.That(none.Story, Is.EqualTo(new[] { h.Text.DebriefStory["unguarded"] }), "no holders: the {holders} line is dropped");
    }

    [Test] public void EveryCauseIsWordedWithNoHoleAndTheSealIsKeyedByItsCar()
    {
        var h = Host.Fresh(); var t = h.Text; Assert.That(t.DebriefCause.Count, Is.GreaterThan(20), "debrief.cause not read");
        foreach (var key in t.DebriefCause.Keys)
        {
            var p = key.Split('|'); var c = new FailCause { Fail = p[0], Why = p[0] == "sealed" ? "sealed" : p[1] }; if (p[0] == "sealed" && p[1] != "unknown") c.Car = p[1];
            c.Culprits.Add(new Blamed { Id = "rivals", Kind = "rival" }); c.Ops.Add("enforcer");
            Assert.That(HandoverHost.CauseKey(c), Is.EqualTo(p[1]), key);
            var w = h.CauseWords(c, new[] { "rival-1" });
            Assert.That(w.Head, Is.Not.Empty, key); Assert.That(w.Line, Is.Not.Empty, key); Assert.That(w.Head.Length, Is.LessThanOrEqualTo(32), key);
            Host.NoTokens(key, new[] { w.Head, w.Line }.Concat(w.Story).ToArray());
            foreach (var s in w.Story.Append(w.Line)) Assert.That(char.IsUpper(s[0]), $"{key}: '{s}' starts lower case");
        }
        Assert.That(h.CauseWords(new FailCause { Fail = "rivals-reach-vale", Why = "unresolved" }, new string[0]).Head, Is.Null, "an unworded cause keeps the fallback (less.end.fail, briefing.fails)");
    }

    [Test] public void AWithdrawalAndAnEndedFreshRunReadTheirOwnCause()
    {
        var h = Host.Fresh(); var c = new FailCause { Fail = "withdraw", Why = "car-left-without-vale" };
        Assert.That(h.CauseWords(c, new string[0]).Head, Is.EqualTo("Left without Popovic"));
        var v = new FailCause { Fail = "vale-dead", Why = "killed" }; v.Ops.Add("enforcer");
        Assert.That(h.CauseWords(v, new string[0]).Line, Does.StartWith("The Enforcer killed him"), "{agent} filled and capitalised");
    }

    static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}

/// <summary>The book's getaway conditions read the engine as it decides: car-guarded is fight.unguarded's guard, left-safe is Go's left-behind fates.</summary>
public class BookGetaway
{
    [Test] public void TheHostsGuardIsTheEnginesGuard()
    {
        var h = Host.Fresh(); var g = h.Game; var host = typeof(HandoverHost).GetMethod("CarGuard", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var engine = typeof(Game).GetMethod("Guards", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.That(host, Is.Not.Null, "HandoverHost.CarGuard is gone"); Assert.That(engine, Is.Not.Null, "Game.Guards is gone: re-read sim/Suborned.Sim/GameUnguarded.cs");
        var o = g.Ops[3]; int n = 0;
        foreach (var down in new[] { false, true }) foreach (var inCar in new[] { false, true }) foreach (var inside in new[] { false, true }) foreach (var drawn in new[] { false, true }) foreach (var holster in new[] { -1, 5 })
        {
            o.Gun.Down = down; o.InCar = inCar; o.Inside = inside; o.Gun.Drawn = drawn; o.Gun.HolsterAt = holster; n++;
            Assert.That(host.Invoke(null, new object[] { o }), Is.EqualTo(engine.Invoke(null, new object[] { o })), $"down {down} car {inCar} inside {inside} drawn {drawn} holster {holster}");
        }
        Assert.That(n, Is.EqualTo(32));
    }

    [Test] public void CarGuardedAndLeftSafeAreLiveWordsOnBothWays()
    {
        var h = Host.Fresh(); var g = h.Game;
        Assert.That(h.BookCheck("car-guarded"), Is.False, "Popovic is not aboard at the start"); Assert.That(h.BookCheck("left-safe"), Is.EqualTo(g.LeftBehind().All(x => x.fate == "walked")));
        g.Vale = ValeAt.Car; bool held = g.CarHeld(); Assert.That(h.BookCheck("car-guarded"), Is.EqualTo(!held || g.Ops.Any(o => !o.Down && !o.InCar && !o.Inside && o.Gun.Drawn && o.Gun.HolsterAt < 0)), $"held {held}");
        g.Ops[3].Gun.Down = true; Assert.That(h.BookCheck("left-safe"), Is.False, "a downed agent left behind is captured");
        var needs = BookWordsFile.Get().Needs;
        foreach (var way in new[] { "front-door", "pickup" })
        {
            var ids = needs[way].Select(x => x.Id).ToList(); int at = ids.IndexOf("getaway");
            Assert.That(ids.Skip(at), Is.EqualTo(way == "front-door" ? new[] { "getaway", "guarded", "go", "left-safe" } : new[] { "getaway", "go", "left-safe" }), way); // engine 0.42.0: the lend never needs our car guarded with Popovic aboard
        }
    }
}

/// <summary>The held car's feed card: one a walk on the car (Game.RivalsForCar turning non-empty), not one a rival.</summary>
public class ForCarCard
{
    [Test] public void OneCardAWalkNotOneARival()
    {
        var h = Host.Fresh(); var g = h.Game; var walkers = (SortedSet<string>)Host.Get(g, "forCar");
        LogLine L(string id) => new LogLine { Tick = g.Tick, Code = "rivals.for-car", Text = $"{id} sees nobody of the cell guarding the car and goes for Popovic." };
        bool On() => (bool)Host.Get(h, "forCarCard");
        walkers.Add("rival-1"); walkers.Add("rival-2"); walkers.Add("rival-3");
        Host.Call(h, "FeedEvent", L("rival-1")); Assert.That(On(), "the first rival's line starts the walk's card");
        float pulse = h.HolderPulseAt; Host.Later(1f);
        Host.Call(h, "FeedClock"); Host.Call(h, "FeedEvent", L("rival-2")); Host.Call(h, "FeedEvent", L("rival-3"));
        Assert.That(h.HolderPulseAt, Is.EqualTo(pulse), "the rest of the walk's lines make no card (and no second pulse)");
        walkers.Clear(); Host.Call(h, "FeedClock"); Assert.That(On(), Is.False, "the walk is over once RivalsForCar is empty");
        walkers.Add("rival-2"); Host.Call(h, "FeedEvent", L("rival-2")); Assert.That(On()); Assert.That(h.HolderPulseAt, Is.GreaterThan(pulse), "a new walk has its own card");
        walkers.Clear();
    }
}
