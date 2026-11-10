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
    /// <summary>Engine 0.53.0 (brief 105; Sky 2026-11-10: "they can't take popovic from the car"): K3-PRESS, the whole cell in the held car with Popovic, is no longer
    /// a take: the rivals shoot the car, it is shot out, everyone gets out and fights on. The reached-car cause is retired from the words.</summary>
    [Test] public void AShotOutCarIsNoTakeAndReachedCarIsRetired()
    {
        var h = Host.Fresh(); var s = Scenarios.Find("K3-PRESS"); Assert.That(s, Is.Not.Null, "the engine's K3-PRESS scenario is gone");
        var g = Scenarios.Run(h.PlayInputs, s).G;
        Assert.That(g.Log.Exists(l => l.Code == "rivals.fire-car"), "the rivals shoot the car"); Assert.That(g.Log.Exists(l => l.Code == "car.shot-out"), "the car is shot out");
        Assert.That(g.Cause?.Why ?? "", Is.Not.EqualTo("reached-car")); Assert.That(g.FailCode ?? "", Is.Not.EqualTo("getaway-lost"), "a car the rivals shot out is no getaway-lost");
        Assert.That(h.Text.CauseFor("rivals-reach-vale", "reached-car"), Is.Null, "debrief.cause.rivals-reach-vale.reached-car is retired (Fable 105)");
        Assert.That(BookWordsFile.Get().ShotOut, Does.Contain("Popovic"), "book.cars.shotOut read");
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
        var engine = typeof(Game).GetMethod("Outside", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.That(host, Is.Not.Null, "HandoverHost.CarGuard is gone"); Assert.That(engine, Is.Not.Null, "Game.Outside is gone: re-read sim/Suborned.Sim/GameUnguarded.cs");
        var o = g.Ops[3]; int n = 0;
        foreach (var down in new[] { false, true }) foreach (var inCar in new[] { false, true }) foreach (var inside in new[] { false, true }) foreach (var drawn in new[] { false, true }) foreach (var left in new[] { false, true })
        {
            o.Gun.Down = down; o.InCar = inCar; o.Inside = inside; o.Gun.Drawn = drawn; o.LeftMap = left; n++;
            Assert.That(host.Invoke(null, new object[] { o }), Is.EqualTo(engine.Invoke(null, new object[] { o })), $"down {down} car {inCar} inside {inside} drawn {drawn} left {left}");
        }
        Assert.That(n, Is.EqualTo(32));
    }

    [Test] public void CarGuardedAndLeftSafeAreLiveWordsOnBothWays()
    {
        var h = Host.Fresh(); var g = h.Game;
        Assert.That(h.BookCheck("car-guarded"), Is.False, "Popovic is not aboard at the start"); Assert.That(h.BookCheck("left-safe"), Is.EqualTo(g.LeftBehind().All(x => x.fate == "walked")));
        g.Vale = ValeAt.Car; bool held = g.CarHeld(); Assert.That(h.BookCheck("car-guarded"), Is.EqualTo(!held || g.Ops.Any(o => !o.Down && !o.InCar && !o.Inside && !o.LeftMap)), $"held {held}");
        g.Ops[3].Gun.Down = true; Assert.That(h.BookCheck("left-safe"), Is.False, "a downed agent left behind is captured");
        var needs = BookWordsFile.Get().Needs;
        foreach (var way in new[] { "front-door", "pickup" })
        {
            var ids = needs[way].Select(x => x.Id).ToList(); int at = ids.IndexOf("getaway");
            Assert.That(ids.Skip(at), Is.EqualTo(way == "front-door" ? new[] { "getaway", "guarded", "go", "left-safe" } : new[] { "getaway", "go", "left-safe" }), way); // engine 0.42.0: the lend never needs our car guarded with Popovic aboard
        }
    }
}

/// <summary>Engine 0.53.0 (brief 105): the crucial card for-car comes at rivals.fire-car (Fable's "They're shooting at our car"), once, and leaves once one of ours is out.</summary>
public class ForCarCard
{
    [Test] public void TheFireCarCardComesOnceAndLeavesWhenOneOfUsIsOut()
    {
        var h = Host.Fresh(); var g = h.Game; g.Vale = ValeAt.Car; foreach (var o in g.Ops) o.InCar = true;
        LogLine L() => new LogLine { Tick = g.Tick, Code = "rivals.fire-car", Text = "The rivals are shooting the getaway car with Popovic inside: nobody of the cell is out to fight them." };
        Host.Call(h, "CrucialFromLog", L()); Host.Call(h, "CrucialFromLog", L());
        Assert.That(h.CrucialQueue.Count(c => c.Kind == "for-car"), Is.EqualTo(1), "one card while the shooting lasts");
        var c0 = h.CrucialQueue.First(c => c.Kind == "for-car"); Assert.That(c0.Title, Is.EqualTo(BookWordsFile.Get().Card("for-car").Title)); Host.NoTokens("for-car", c0.Title, h.CrucialLine(c0));
        Assert.That((bool)Host.Call(h, "CrucialTrue", c0), Is.True, "true while Popovic is in and none of us is out");
        g.Ops[0].InCar = false; Assert.That((bool)Host.Call(h, "CrucialTrue", c0), Is.False, "the Scout out: the rivals shoot at her, not the car");
        foreach (var o in g.Ops) o.InCar = false; g.Vale = ValeAt.Office;
    }
}
