// Brief 096, G1 (engine 0.43.0, rulebook car.go.waits "board"): a Go given while an agent is climbing into the car waits for his Board and lands the tick it
// lands. The host reads it (HostGoWaits): the car marker offers Go, the Go row says it goes once he is in (brief 101: never the seconds, Sky's rule on numbers in
// play), then says it waits; the roster says the car goes once he is in. Same harness as HostLogic.cs: the real engine and text.json, a fresh game, no views.
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using UnityEngine;
using Order = Suborned.Sim.Order;

public class GoWaits096
{
    /// <summary>A fresh run with the Scout at our car's seat and her Board under way (nobody else aboard).</summary>
    static HandoverHost Climbing()
    {
        var h = Host.Fresh(); Host.Set(h, "HintsOn", false); var g = h.Game; var at = g.Walk.NearestFree(g.GetawaySeat) ?? g.GetawaySeat;
        g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "move", Point = at });
        Assert.That(Host.Until(h, () => g.AtGetaway(g.Ops[0].Pos) && g.Ops[0].Walk == null, 4000), $"the Scout never reached the car by {g.Tick}");
        var b = g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "board", Target = "car" }); Assert.That(b.ok, $"{b.code} {b.why}"); Host.Tick(h);
        Assert.That(g.Ops[0].Busy?.Do, Is.EqualTo("board"), "climbing in"); Assert.That(g.GoWouldWait("car"), "a Go now would wait for her");
        return h;
    }
    static HandoverHost.PanelRow GoRow(HandoverHost h) { h.Select(OpId.Scout, false); return Host.Plan(h, "getaway", "car").Pn.Rows.Single(r => r.I.Order?.Do == "go"); }

    [Test] public void AGoWhileSheClimbsInIsOfferedSaysItGoesOnceSheIsInWithNoSeconds()
    {
        var h = Climbing(); var g = h.Game; var bw = BookWordsFile.Get();
        Assert.That(g.AnyInCar, Is.False); Assert.That(h.GoAvailable, "the car marker offers Go: one climbing in counts as in");
        var go = GoRow(h); Assert.That(go.Available, go.Reason); Assert.That(go.Lock.code ?? "", Is.EqualTo(g.GoQuery().code ?? ""), "the engine's own answer");
        var once = bw.GoOnce.Replace("{list}", "Scout"); Assert.That(go.Verb, Does.EndWith(once)); Assert.That(go.I.Does.ToLowerInvariant(), Is.EqualTo(once.ToLowerInvariant()));
        Assert.That(h.LeftBehindNow().Any(x => x.op == "scout"), Is.False, "the Go card counts her aboard");
        Assert.That(HudModel.PriceLine(h, go.I).Exists(f => f.Kind == "time"), Is.False, "brief 101: no seconds until the car goes (the Board's own length)");
        Host.NoTokens("go row", go.Verb, go.I.Does);
    }

    [Test] public void GivenItWaitsOnTheGoRowAndTheRosterThenLandsWithHer()
    {
        var h = Climbing(); var g = h.Game; var bw = BookWordsFile.Get(); int lands = g.Ops[0].BusyEnd;
        var a = g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "go", Target = "car" }); Assert.That(a.ok, $"{a.code} {a.why}"); Host.Call(h, "Track");
        Assert.That(h.GoWaitingOn("car"), "the Go waits"); Assert.That(GoRow(h).Verb, Is.EqualTo(bw.GoWaiting.Replace("{list}", "Scout")));
        Assert.That(h.DoingOf(0).OnDone, Is.EqualTo(bw.GoWaitsRoster), "the roster: the car goes once she is in");
        HostLogicShim.Frame++; var m = new HudModel(); m.Frame(h); Assert.That(m.Slots[0].Under, Does.Contain(bw.GoWaitsRoster));
        Assert.That(Host.Until(h, () => g.Over || g.GetawayGone, 200), $"the car never went by {g.Tick}");
        Assert.That(g.CarLeftTick, Is.EqualTo(lands), "the car leaves the tick her Board lands"); Assert.That(h.GoWaitingOn("car"), Is.False);
    }
}
