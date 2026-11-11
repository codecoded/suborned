// The host's decisions, run for real with no Unity (brief 078; Sky: "a lot of this can be done outside Unity"): the player's compiled scripts, the real engine and the real text.json,
// on a copy of Unity's assemblies whose native calls answer defaults (Patch.cs). No scene, no views, no input devices, no pictures: those stay in the player's proof.
// Run with `bun tools/game/hostlogic.ts [filter]`; a case is one method here. A case that needs a private member reads it by name, so a rename fails here with the name to fix.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using UnityEngine;
using Order = Suborned.Sim.Order;

public static class Host
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    public static object Call(object o, string m, params object[] a)
    {
        var f = o.GetType().GetMethods(Any).FirstOrDefault(x => x.Name == m && x.GetParameters().Length == a.Length) ?? throw new MissingMethodException($"{o.GetType().Name}.{m} is gone: fix tools/game/hostlogic/HostLogic.cs");
        return f.Invoke(o, a);
    }
    public static void Set(object o, string n, object v)
    {
        var t = o.GetType(); var p = t.GetProperty(n, Any);
        if (p != null) { p.SetValue(o, v); return; }
        (t.GetField(n, Any) ?? throw new MissingFieldException($"{t.Name}.{n} is gone: fix tools/game/hostlogic/HostLogic.cs")).SetValue(o, v);
    }
    public static object Get(object o, string n)
    {
        var t = o.GetType(); var p = t.GetProperty(n, Any);
        return p != null ? p.GetValue(o) : (t.GetField(n, Any) ?? throw new MissingFieldException($"{t.Name}.{n} is gone: fix tools/game/hostlogic/HostLogic.cs")).GetValue(o);
    }
    /// <summary>A host on a fresh run: the real engine and words, no views. Only what Begin does that is not a GameObject.</summary>
    public static HandoverHost Fresh()
    {
        HostLogicShim.UnscaledTime = 100f; HostLogicShim.Frame++;
        var h = new HandoverHost();
        h.Begin(); if (h.Fault != null) throw new InvalidOperationException($"Begin faulted: {h.Status}: {h.Fault}");
        var views = Get(h, "views"); // its members are silent (Patch.cs); the collections the host fills for it are real
        foreach (var f in views.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) if (f.GetValue(views) == null && f.FieldType.IsGenericType && f.FieldType.GetConstructor(Type.EmptyTypes) != null) f.SetValue(views, Activator.CreateInstance(f.FieldType));
        Set(h, "ShowBriefing", false); h.IntelOpen = false; h.StartClock(); h.SetPaused(false);
        h.Select(OpId.Scout, false); Call(h, "Track");
        return h;
    }
    public static void Tick(HandoverHost h, int n = 1) { h.RunTo(h.Game.Tick + n); Call(h, "Track"); }
    public static bool Until(HandoverHost h, Func<bool> done, int ticks = 3000) { for (int i = 0; i < ticks && !done(); i++) Tick(h, 1); return done(); }
    public static void Later(float s) => HostLogicShim.UnscaledTime += s;
    public static (bool spent, List<int> who) Click(HandoverHost h)
    {
        var a = new object[] { Vector2.zero, null };
        var r = (bool)typeof(HandoverHost).GetMethod("BusyClick", Any).Invoke(h, a);
        return (r, (List<int>)a[1]);
    }
    public static HandoverHost.ContextPlan Plan(HandoverHost h, string id, string kind = "person")
    {
        var t = new HandoverHost.Target { Kind = kind, Key = id, Id = id, Label = id };
        return h.ContextFor(t, new Vector2(100, 100));
    }
    public static List<string> Orders(HandoverHost h, int from) { var g = h.Game; var l = new List<string>(); for (int i = from; i < g.Orders.Count; i++) l.Add(g.Orders[i].order.Op + ":" + g.Orders[i].order.Do); return l; }
    /// <summary>A person or thing's visible words carry no unfilled {token}, in any row of a plan.</summary>
    public static void NoTokens(string where, params string[] words) { foreach (var w in words) Assert.That(w ?? "", Does.Not.Contain("{"), $"{where}: unfilled token in '{w}'"); }
}

/// <summary>Who is busy, and what a click does to them (Proof083's firstClickAsks, doubleClickIsOneSlip, secondClickStops, squadSkipsBusy, sameActKept, without the devices).</summary>
public class Busy
{
    static HandoverHost Watching()
    {
        var h = Host.Fresh(); h.GiveWatchScripted(OpId.Scout, "waitress");
        Assert.That(Host.Until(h, () => h.Game.Ops[0].Busy != null), $"the Scout never got to the watch: tick {h.Game.Tick}, orders {h.Game.Orders.Count}, {h.Status}, fault {h.Fault}, last log {(h.Game.Log.Count > 0 ? h.Game.Log[^1].Code : "")}"); return h;
    }
    [Test] public void AScoutOnAWatchIsBusyAndSaysSo()
    {
        var h = Watching(); var d = h.DoingOf(0);
        Assert.That(d.Busy && d.CanCancel, $"phase {d.Phase}"); Assert.That(d.Label, Does.StartWith("Watching").Or.Contain("atch"), d.Label); Assert.That(d.TicksLeft, Is.GreaterThan(0)); Host.NoTokens("doing", d.Label, d.CancelText);
    }
    [Test] public void FirstClickAsksAndStopsNothing()
    {
        var h = Watching(); int n0 = h.Game.Orders.Count, asks = h.BusyAsks; var (spent, _) = Host.Click(h);
        Assert.That(spent); Assert.That(h.BusyAsks, Is.EqualTo(asks + 1)); Assert.That(h.BusyWords, Is.EqualTo("Click again to stop")); Assert.That(h.Game.Orders.Count, Is.EqualTo(n0)); Assert.That(h.Game.Ops[0].Busy, Is.Not.Null);
    }
    [Test] public void ADoubleClickIsOneSlip()
    {
        var h = Watching(); Host.Click(h); int asks = h.BusyAsks, stops = h.BusyStops; Host.Later(0.05f); var (spent, _) = Host.Click(h);
        Assert.That(spent); Assert.That(h.BusyAsks, Is.EqualTo(asks)); Assert.That(h.BusyStops, Is.EqualTo(stops));
    }
    [Test] public void ASecondClickInTimeStopsAndWalks()
    {
        var h = Watching(); Host.Click(h); int stops = h.BusyStops; Host.Later(1f); var (spent, who) = Host.Click(h);
        Assert.That(spent, Is.False, "the walk goes on"); Assert.That(who, Is.EquivalentTo(new[] { 0 })); Assert.That(h.BusyStops, Is.GreaterThan(stops));
    }
    [Test] public void TooLateAsksAgain()
    {
        var h = Watching(); Host.Click(h); int asks = h.BusyAsks; Host.Later(HandoverHost.BusyConfirmS + 0.5f); var (spent, _) = Host.Click(h);
        Assert.That(spent); Assert.That(h.BusyAsks, Is.EqualTo(asks + 1));
    }
    [Test] public void ASquadClickLeavesTheBusyAgentAtItsAct()
    {
        var h = Watching(); h.SelectAll(); int skips = h.BusySkips; var (spent, who) = Host.Click(h);
        Assert.That(spent, Is.False); Assert.That(who.Contains(0), Is.False); Assert.That(who.Count, Is.EqualTo(3)); Assert.That(h.BusySkips, Is.GreaterThan(skips));
    }
    [Test] public void TheSameActAgainRestartsNothing()
    {
        var h = Watching(); int end = h.Game.Ops[0].BusyEnd, n0 = h.Game.Orders.Count, kept = h.SameKept; h.GiveWatchRepeatScripted(OpId.Scout, "waitress");
        Assert.That(h.Game.Ops[0].BusyEnd, Is.EqualTo(end)); Assert.That(h.Game.Orders.Count, Is.EqualTo(n0)); Assert.That(h.SameKept, Is.GreaterThan(kept));
    }
    [Test] public void AnIdleAgentsClickIsNotAsked()
    {
        var h = Host.Fresh(); var (spent, who) = Host.Click(h); Assert.That(spent, Is.False); Assert.That(who, Is.Null);
    }
    [Test] public void CancellingDropsTheActWithAPrice()
    {
        var h = Watching(); int n0 = h.Game.Orders.Count; h.CancelAgent(0, null); Host.Tick(h);
        Assert.That(Host.Orders(h, n0), Does.Contain("scout:cancel")); Assert.That(h.Game.Ops[0].Busy, Is.Null); Assert.That(h.DoingOf(0).Busy, Is.False); Assert.That(h.LastCancelPop, Does.StartWith("Watch cancelled"));
    }
    [Test] public void AWalkInSaysWhereItIsGoing()
    {
        var h = Host.Fresh(); h.GiveWatchScripted(OpId.Scout, "waitress"); Host.Tick(h); Assert.That(h.Game.Ops[0].Busy, Is.Null, "the case wants a Scout who has to walk first");
        Assert.That(h.Game.Ops[0].Act?.Do, Is.EqualTo("watch"), "the walk-in's act"); Assert.That(h.PlateWords(0), Does.StartWith("\u2192"), h.PlateWords(0)); Assert.That(h.DoingOf(0).Queued, Does.Contain("Watch"));
    }
}

/// <summary>What the ring offers, and the prices on it (the rows the chip, the ring and the panel share: HostContext, BuildPanel, Locked, ContextRank).</summary>
public class Rings
{
    [Test] public void TheScoutOnInezOffersAWatch()
    {
        var h = Host.Fresh(); var p = Host.Plan(h, "waitress"); var verbs = p.Pn.Rows.Select(r => r.I?.Order?.Do).ToList();
        Assert.That(verbs, Does.Contain("watch"), string.Join(",", verbs)); Assert.That(p.Pn.Rows.All(r => r.By == OpId.Scout), "only the selected agent's rows");
    }
    [Test] public void TheFixersTurnsAreOfferedAndPriced()
    {
        var h = Host.Fresh(); h.Select(OpId.Fixer, false); var p = Host.Plan(h, "waitress"); var turns = p.Pn.Rows.Where(r => r.I?.Order?.Do == "turn").ToList();
        Assert.That(turns.Count, Is.GreaterThan(0), "no turn on Inez"); var approaches = turns.Select(r => r.I.Order.Approach).ToList();
        foreach (var a in new[] { "ask", "pay", "help", "pressure" }) if (!approaches.Contains(a)) Assert.That(p.Pn.Rows.Any(r => r.Verb.ToLowerInvariant().Contains(a)), $"{a}: neither offered nor shown locked: {string.Join(",", approaches)}");
        var pay = turns.FirstOrDefault(r => r.I.Order.Approach == "pay"); if (pay != null) Assert.That(ContextRank.Num(pay.I.Usd), Is.GreaterThan(0), $"Pay costs nothing: '{pay.I.Usd}'");
    }
    [Test] public void AQuickClickIsNeverViolentOrAGamble()
    {
        var h = Host.Fresh();
        foreach (var who in new[] { OpId.Scout, OpId.Fixer, OpId.Tech, OpId.Enforcer })
        {
            h.Select(who, false);
            foreach (var id in new[] { "waitress", "guard", "councillor", "driver" })
            {
                var p = Host.Plan(h, id); if (p.Choice.Act != ContextRank.Act.Default) continue;
                var i = p.Row(p.Choice.Pick).I; Assert.That(i.Order.Do, Is.Not.EqualTo("takedown").And.Not.EqualTo("fire"), $"{who} on {id}"); Assert.That(i.Outcome ?? "", Is.Not.EqualTo("?"), $"{who} on {id}");
            }
        }
    }
    [Test] public void ALockedRowSaysWhyInPlainWords()
    {
        var h = Host.Fresh(); int locked = 0;
        foreach (var who in new[] { OpId.Scout, OpId.Fixer, OpId.Tech, OpId.Enforcer })
        {
            h.Select(who, false);
            foreach (var id in new[] { "waitress", "guard", "councillor", "driver" })
                foreach (var r in Host.Plan(h, id).Pn.Rows)
                {
                    Host.NoTokens($"{who} on {id}", r.Verb, r.Reason, r.Unlock, r.I?.Label);
                    if (!r.Available) { locked++; Assert.That(r.Reason ?? "", Is.Not.Empty, $"{who} on {id}: {r.Verb} is locked with no reason"); }
                }
        }
        Assert.That(locked, Is.GreaterThan(0), "no row was locked: this case tests nothing");
    }
    [Test] public void AGivenOrderCostsWhatTheRowSaid()
    {
        var h = Host.Fresh(); var g = h.Game; h.Issue(new Order { Op = "fixer", Do = "move", To = "d:Boarding pavilion" }); Host.Until(h, () => g.Ops[1].Walk == null, 3000);
        h.Select(OpId.Fixer, false); var row = Host.Plan(h, "driver").Pn.Rows.FirstOrDefault(r => r.Available && r.I?.Order?.Do == "turn" && r.I.Order.Approach == "pay");
        Assert.That(row, Is.Not.Null, "no Pay on the driver for the Fixer beside him"); var said = ContextRank.Num(row.I.Usd); Assert.That(said, Is.GreaterThan(0)); var cash = g.Cash;
        h.GiveScripted(row.I.Order); Assert.That(Host.Until(h, () => g.Cash != cash, 6000), "the order never charged"); Assert.That(cash - g.Cash, Is.EqualTo(said));
    }
}

/// <summary>What the HUD model says: the real HudModel.Frame over a real run (the roster, the pill, the chip's parts), no drawing.</summary>
public class Model
{
    static HudModel Built(HandoverHost h) { var m = new HudModel(); HostLogicShim.Frame++; m.Refresh(h); return m; }
    [Test] public void TheRosterNamesTheFourAndTheirState()
    {
        var h = Host.Fresh(); h.GiveWatchScripted(OpId.Scout, "waitress"); Host.Until(h, () => h.Game.Ops[0].Busy != null); HostLogicShim.Frame++; var m = new HudModel(); m.Frame(h);
        Assert.That(m.Slots.Length, Is.EqualTo(4)); Assert.That(m.Slots[0].Busy, "the Scout is busy on the roster"); Assert.That(m.Slots[1].Busy, Is.False); Host.NoTokens("roster", m.Slots.SelectMany(s => new[] { s.Name, s.Label, s.Under, s.HintLine, s.CancelBody }).ToArray());
        Assert.That(m.Clock, Is.Not.Empty); Assert.That(m.Cash, Does.Contain("$"));
    }
    [Test] public void TheEndCardAndBriefingHaveNoHoles()
    {
        var h = Host.Fresh(); Host.Set(h, "ShowBriefing", true); HostLogicShim.Frame++; var m = new HudModel(); m.Frame(h); Host.NoTokens("briefing", m.BriefTitle, m.BriefSub, m.BriefSituation, m.BriefObjective, m.BriefWin, m.BriefClock); Assert.That(m.BriefTitle, Is.Not.Empty);
    }
}

/// <summary>Windows the player can see coming (ProofDone's callWindow, without the picture): Inez turned, the call opens the guard's window, and it closes.</summary>
public class Windows
{
    [Test] public void TheCallOpensTheGuardsWindowWithItsSeconds()
    {
        var h = Host.Fresh(); var g = h.Game; h.GiveScripted(new Order { Op = "scout", Do = "watch", Target = "waitress" }); Host.Until(h, () => g.Ops[0].Busy != null, 3000); Host.Until(h, () => g.Ops[0].Busy == null, 3000);
        int l1 = g.Log.Count; h.GiveScripted(new Order { Op = "scout", Do = "listen", Target = "waitress" });
        Assert.That(Host.Until(h, () => g.Log.Skip(l1).Any(x => (x.Code ?? "").StartsWith("scout.listen.")), 3000), $"the Scout never heard Inez: {string.Join(", ", g.Log.Skip(l1).Select(x => x.Code))}, {h.Status}");
        for (int tries = 0; tries < 3 && !g.Inez.Turned; tries++)
        {
            int l0 = g.Log.Count; h.GiveScripted(new Order { Op = "fixer", Do = "turn", Target = "waitress", Approach = "help" });
            Host.Until(h, () => g.Log.Skip(l0).Any(x => (x.Code ?? "").StartsWith("turn.help.") || x.Code == "order.dropped"), 2400);
        }
        Assert.That(g.Inez.Turned, "Inez was not turned by three helps"); Assert.That(Host.Until(h, () => g.CallQuery("waitress").ok, 3000), "the call never opened");
        int w0 = g.Log.Count; h.GiveScripted(new Order { Op = "fixer", Do = "call", Target = "waitress" });
        Assert.That(Host.Until(h, () => g.Windows().Exists(w => w.Kind == "call" && w.Thing == "guard"), 300), "no guard window after the call"); var w = g.Windows().First(x => x.Kind == "call" && x.Thing == "guard"); Assert.That(w.Until, Is.GreaterThan(g.Tick));
        int until = w.Until; Host.Until(h, () => g.Over || g.Tick > until, until - g.Tick + 5); if (!g.Over) Assert.That(g.Windows().Exists(x => x.Kind == "call" && x.Thing == "guard" && x.Until == until), Is.False, "the window outlived its own end");
    }
}

/// <summary>Brief 085, Sky on 0.2.177: a squad order reaches every selected agent, or the one it can't says why; in third person too. The T key, the mouse and the door's box are the player's proof (Proof085: this calls the Give methods, not the input path).</summary>
public class Squad
{
    static readonly int[] Seeds = { 7, 1234, 98765 };
    static void EachSeed(Action<HandoverHost> c) { foreach (var s in Seeds) { HandoverHost.SeedPin = s; try { c(Host.Fresh()); } finally { HandoverHost.SeedPin = 0; } } }
    /// <summary>Scout, Fixer, Tech: a panel with the Enforcer in it needs HostViews.KeyOf, silent here (his takedown: Proof085).</summary>
    static void Three(HandoverHost h) { h.Select(OpId.Scout, false); h.Select(OpId.Fixer, true); h.Select(OpId.Tech, true); }
    static void Hand(HandoverHost h, int k) { Host.Set(h, "Driving", true); Host.Set(h, "dAgent", k); h.TakeInSquad(k); }
    static void Fire(HandoverHost h, string how, string id = "guard")
    {
        var c = new HandoverHost.HoldCharge { Fire = true, FireId = id, Button = 1, T = new HandoverHost.Target { Kind = "person", Key = id, Id = id, Label = id } };
        Host.Call(h, how, c, Vector2.zero);
    }
    /// <summary>Every agent holds the order and has his gun out, with no F, or says why on his plate.</summary>
    static void AllFireOrSay(HandoverHost h, string id, string where, params int[] say)
    {
        for (int k = 0; k < 4; k++)
        {
            var gun = h.Game.Ops[k].Gun;
            if (Array.IndexOf(say, k) >= 0) { Assert.That(gun.Order, Is.Empty, $"{where}: agent {k}"); Assert.That(h.FireRefusalWord(k), Is.Not.Empty, $"{where}: agent {k} left out with no word"); continue; }
            Assert.That(gun.Order, Is.EqualTo(id), $"{where}: agent {k} took no fire order: refused '{h.FireRefusalOf(k).code}'"); Assert.That(gun.Drawn, $"{where}: agent {k} needed F");
        }
    }
    /// <summary>Brief 085 (Sky on 0.2.212): a wedge taken as the player takes it: the ring opened on the patrol, its Fire wedge's held hold done (holdCommits), RingTake. Returns the wedge's faces.</summary>
    static void Near(HandoverHost h, params int[] who) { var g = h.Game; var at = g.PosOf("patrol").Value; foreach (var k in who) g.Ops[k].Pos = new Pt(at.X + 300 + 120 * k, at.Y + 300); }
    static int[] RingFire(HandoverHost h, bool held = true)
    {
        Near(h, 0, 1, 2); var p = Host.Plan(h, "patrol", "patrol"); Host.Call(h, "OpenRing", p, new Vector2(100, 100), false); var rs = h.Ring; Assert.That(rs, Is.Not.Null, "no ring on the patrol"); int seg = rs.Segs.FindIndex(x => x.Row?.I?.Order?.Do == "fire"); Assert.That(seg, Is.GreaterThanOrEqualTo(0), $"no Fire wedge: {h.RingWords}; rows {string.Join(",", p.Pn.Rows.Select(r => $"{r.By}:{r.I?.Order?.Do}:{r.Available}:{r.Lock.code}"))}; order {string.Join(",", p.Choice.Order)}; patrol at {h.Game.PosOf("patrol")}");
        var faces = rs.Segs[seg].By.Select(k => (int)k).OrderBy(k => k).ToArray(); Host.Set(h, "holdCommits", held); try { Host.Call(h, "RingTake", seg); } finally { Host.Set(h, "holdCommits", false); } return faces;
    }
    static void OnlyFire(HandoverHost h, params int[] who) { for (int k = 0; k < 4; k++) Assert.That(h.Game.Ops[k].Gun.Order, Is.EqualTo(who.Contains(k) ? "patrol" : ""), $"agent {k}"); }
    [Test] public void AFireFromTheRingGoesToEveryoneSelected() => EachSeed(h =>
    {
        Three(h); int n0 = h.Game.Orders.Count; var faces = RingFire(h); OnlyFire(h, 0, 1, 2); Assert.That(faces, Is.EqualTo(new[] { 0, 1, 2 }), "the wedge's faces"); Assert.That(h.Loud, Is.Null, "a held wedge was the card's hold");
        Assert.That(Host.Orders(h, n0).Where(x => x.EndsWith(":fire")).ToArray(), Is.EquivalentTo(new[] { "scout:fire", "fixer:fire", "tech:fire" })); Assert.That(h.Feed.Count(c => c.Title == "Going loud"), Is.LessThanOrEqualTo(1), "one card for the squad");
    });
    [Test] public void AnUnheldFireFromTheRingOpensOneCardForTheSquad()
    {
        var h = Host.Fresh(); Three(h); RingFire(h, held: false); Assert.That(h.Loud, Is.Not.Null, "calm patrol: the going-loud card"); Assert.That(h.Loud.Orders.Select(o => o.Op + ":" + o.Do).ToArray(), Is.EquivalentTo(new[] { "scout:fire", "fixer:fire", "tech:fire" }), "one card, the squad's orders on it"); OnlyFire(h);
    }
    [Test] public void AFireFromTheRingLeavesTheBusyOneSayingWhyAndShowsWhoFires()
    {
        var h = Host.Fresh(); h.GiveWatchScripted(OpId.Scout, "waitress"); Assert.That(Host.Until(h, () => h.Game.Ops[0].Busy != null)); Three(h); var faces = RingFire(h); OnlyFire(h, 1, 2); Assert.That(h.FireRefusalWord(0), Is.Not.Empty, "the Scout left out with no word"); Assert.That(faces, Is.EqualTo(new[] { 1, 2 }), "the wedge named who wouldn't fire");
    }
    [Test] public void AFireFromTheRingWithOnlyTheBusyOneSelectedStillCancelsHisAct()
    {
        var h = Host.Fresh(); h.GiveWatchScripted(OpId.Scout, "waitress"); Assert.That(Host.Until(h, () => h.Game.Ops[0].Busy != null)); h.Select(OpId.Scout, false); RingFire(h); OnlyFire(h, 0); Assert.That(h.Game.Ops[0].Busy, Is.Null, "the wedge offered Fire: his act gave way, the 0.2.113 row");
    }
    [Test] public void AFireFromTheRingInThirdPersonTakesTheOneInHandAndTheSquad()
    {
        var h = Host.Fresh(); Three(h); Hand(h, 1); var faces = RingFire(h); OnlyFire(h, 0, 1, 2); Assert.That(faces, Is.EqualTo(new[] { 0, 1, 2 })); Assert.That(h.Selected.Take(3).All(x => x), "nobody leaves the squad for a fire");
    }
    [Test] public void AFireThatNamesOneAgentStaysOneAgent()
    {
        var h = Host.Fresh(); Three(h); Near(h, 0, 1, 2); var p = Host.Plan(h, "patrol", "patrol"); var row = p.Pn.Rows.Find(r => r.By == OpId.Fixer && r.I?.Order?.Do == "fire"); Host.Set(h, "holdCommits", true); Host.Call(h, "DoRow", row, p.T, Vector2.zero, null, "left"); OnlyFire(h, 1);
        h = Host.Fresh(); Three(h); Near(h, 0, 1, 2); p = Host.Plan(h, "patrol", "patrol"); Host.Call(h, "OpenRing", p, new Vector2(100, 100), false); var rs = h.Ring; int seg = rs.Segs.FindIndex(x => x.Row?.I?.Order?.Do == "fire"); Host.Set(h, "holdCommits", true);
        Assert.That((bool)Host.Call(h, "RingKeyFire", rs, seg), "Shift+1-4 on a Fire wedge"); OnlyFire(h, (int)rs.Segs[seg].Row.By);
    }
    [Test] public void ASaveDuringTheFailCameraLeavesTheWholeScript()
    {
        var h = Host.Fresh(); var path = System.IO.Path.GetTempFileName(); Host.Set(h, "SessionPath", path); Host.Tick(h, 40); Host.Call(h, "Save"); var whole = File.ReadAllText(path); var played = h.Game;
        var copy = Engine.New(h.Bundle.Inputs, played.Start); Host.Set(h, "failGame", copy); Host.Set(h, "Game", copy); Host.Call(h, "Save"); Assert.That(File.ReadAllText(path), Is.EqualTo(whole), "the rewound copy wrote over the session"); File.Delete(path);
    }
    [Test] public void AGroundClickWalksEveryFreeAgent() => EachSeed(h =>
    {
        h.SelectAll(); int n0 = h.Game.Orders.Count; var (spent, who) = Host.Click(h); Assert.That(spent, Is.False); Assert.That(who, Is.Null, "nobody is busy: the whole selection goes");
        var at = h.Game.Ops[0].Pos; h.GroupWalk(new Pt(at.X + 200, at.Y + 200), false, null, who);
        Assert.That(Host.Orders(h, n0), Is.EquivalentTo(new[] { "scout:move", "fixer:move", "tech:move", "enforcer:move" }));
    });
    [Test] public void AnAgentASquadClickLeavesBehindSaysWhatHeIsOn()
    {
        var h = Host.Fresh(); h.GiveWatchScripted(OpId.Scout, "waitress"); Assert.That(Host.Until(h, () => h.Game.Ops[0].Busy != null)); h.SelectAll(); h.Pops.Clear();
        var (_, who) = Host.Click(h); Assert.That(who, Is.EquivalentTo(new[] { 1, 2, 3 })); Assert.That(h.Pops.Exists(p => p.Text == "Busy: Watch" && p.World.HasValue), $"pops: {string.Join(" | ", h.Pops.Select(p => p.Text))}"); Host.NoTokens("stays", h.Pops.Select(p => p.Text).ToArray());
    }
    [Test] public void ASquadFireOrderStartsAllWithNoF() => EachSeed(h => { h.SelectAll(); Fire(h, "BeginFireLock"); AllFireOrSay(h, "guard", "map"); Assert.That(h.Loud, Is.Null); });
    [Test] public void ABusyAgentSaysWhyAndTheRestFire()
    {
        var h = Host.Fresh(); h.GiveWatchScripted(OpId.Scout, "waitress"); Assert.That(Host.Until(h, () => h.Game.Ops[0].Busy != null)); h.SelectAll(); Fire(h, "BeginFireLock"); AllFireOrSay(h, "guard", "one busy", 0);
    }
    [Test] public void InThirdPersonTheSquadFiresWithTheAgentInHand() => EachSeed(h => { h.SelectAll(); Hand(h, 2); Fire(h, "TpFireLock"); AllFireOrSay(h, "guard", "third person"); Assert.That(h.Agent, Is.EqualTo(OpId.Tech)); });
    [Test] public void InThirdPersonAloneOnlyTheAgentInHandFires()
    {
        var h = Host.Fresh(); h.Select(OpId.Fixer, false); Hand(h, 1); Fire(h, "TpFireLock"); Assert.That(h.Game.Ops[1].Gun.Order, Is.EqualTo("guard")); foreach (var k in new[] { 0, 2, 3 }) Assert.That(h.Game.Ops[k].Gun.Order, Is.Empty, $"agent {k} is not in the squad");
    }
    [Test] public void AFocusInAFightTakesTheSquadInThirdPerson()
    {
        var h = Host.Fresh(); h.SelectAll(); Hand(h, 3); Host.Call(h, "FocusFire", new HandoverHost.Target { Kind = "patrol", Key = "patrol", Id = "patrol", Label = "patrol" }, "patrol", false, Vector2.zero, null);
        AllFireOrSay(h, "patrol", "focus"); Host.Tick(h, 30); for (int k = 0; k < 4; k++) Assert.That(h.PlateWords(k), Is.Not.Empty, $"agent {k} holds a far focus with nothing on his plate");
    }
    [Test] public void ChangingWhoIsInHandKeepsTheSquad()
    {
        var h = Host.Fresh(); h.SelectAll(); Hand(h, 0); h.TakeInSquad(2); Assert.That(h.Selected.All(x => x), "1-4 cut the squad"); Assert.That(h.Agent, Is.EqualTo(OpId.Tech)); Host.Set(h, "dAgent", 2); // DriveFrame moves the hand to Agent
        h.Select(OpId.Scout, true); Assert.That(h.Selected[0], Is.False, "Shift toggles her out"); Assert.That(h.Agent, Is.EqualTo(OpId.Tech)); h.Select(OpId.Scout, true); Assert.That(h.Selected[0]); Assert.That(h.Agent, Is.EqualTo(OpId.Tech), "Shift adds; the one in hand stays in hand");
        h.Select(OpId.Tech, false); h.TakeInSquad(1); Assert.That(h.Selected.Count(x => x), Is.EqualTo(1), "out of the squad: replaces it, as a key on the map"); Assert.That(h.Agent, Is.EqualTo(OpId.Fixer));
    }
    [Test] public void AnActInThirdPersonGoesToTheAgentInHandAndNobodyLeaves()
    {
        var h = Host.Fresh(); Three(h); Hand(h, 1); var t = new HandoverHost.Target { Kind = "person", Key = "waitress", Id = "waitress", Label = "Inez" }; var p = h.ContextFor(t, new Vector2(100, 100));
        int w = p.Rep.FindIndex(i => p.Pn.Rows[i].I.Order.Do == "watch"); Assert.That(w, Is.GreaterThanOrEqualTo(0)); Assert.That(h.DoersOf(p, w), Is.EqualTo(new[] { 1 }), "the one in hand can Watch");
        Host.Call(h, "HoldGive", p, w, t, Vector2.zero, false, "left", true, null); Assert.That(h.Selected[1], "the agent in hand left the selection: the view would jump"); Assert.That(h.Agent, Is.EqualTo(OpId.Fixer));
    }
    [Test] public void AnActOnTheMapStillGoesToTheBestPlacedAndHeLeaves()
    {
        var h = Host.Fresh(); Three(h); var t = new HandoverHost.Target { Kind = "person", Key = "waitress", Id = "waitress", Label = "Inez" }; var p = h.ContextFor(t, new Vector2(100, 100));
        int w = p.Rep.FindIndex(i => p.Pn.Rows[i].I.Order.Do == "watch"); var d = h.DoersOf(p, w); Assert.That(d.Count, Is.EqualTo(1)); int n0 = h.Game.Orders.Count; Host.Call(h, "HoldGive", p, w, t, Vector2.zero, false, "left", true, null); Assert.That(h.Selected[d[0]], Is.False, $"Sky 2026-10-22: an actioned agent leaves the squad: doer {d[0]}, orders [{string.Join(",", Host.Orders(h, n0))}], status {h.Status}, loud {h.Loud != null}, pending {h.PendingOf(d[0]).verb}"); Assert.That(h.Selected.Count(x => x), Is.EqualTo(2));
    }
    [Test] public void TheScoutOnTheGuardOnceHeardShowsTheLock()
    {
        var h = Host.Fresh(); var g = h.Game; h.GiveWatchScripted(OpId.Scout, "guard"); Assert.That(Host.Until(h, () => h.CastOf("guard")?.KRoutine == true || g.Known("guard") is { Sight: true }, 1500)); h.GiveScripted(new Order { Op = "scout", Do = "listen", Target = "guard" }); Assert.That(Host.Until(h, () => h.CastOf("guard")?.KNeed == true && g.Ops[0].Busy == null, 1500));
        h.Select(OpId.Scout, false); var t = new HandoverHost.Target { Kind = "person", Key = "guard", Id = "guard", Label = "the guard" }; var i = (HandoverHost.Intent)Host.Call(h, "HoldIntent", t); var p = h.ContextFor(t, new Vector2(100, 100));
        Assert.That(i?.Locked == true, $"pill '{i?.Label}' actionable {i?.Actionable} rows [{string.Join(",", p.Pn.Rows.Select(r => $"{r.I?.Order?.Do}{(r.Available ? "" : "!")}"))}]");
    }
    [Test] public void WhoCanActOnTheDoorGuardOutranksTheDoor()
    {
        var h = Host.Fresh(); var guard = new HandoverHost.Target { Kind = "person", Key = "guard", Id = "guard", Label = "the guard" }; var door = new HandoverHost.Target { Kind = "door", Key = "door", Label = "the public door" };
        foreach (var who in new[] { OpId.Scout, OpId.Fixer, OpId.Tech }) { h.Select(who, false); Assert.That((int)Host.Call(h, "RowRank", guard), Is.LessThan((int)Host.Call(h, "RowRank", door)), $"{who}"); Assert.That(h.ContextFor(guard, new Vector2(100, 100)).Pn.Rows.Count, Is.GreaterThan(0), $"{who} has no way on him"); }
    }
}

/// <summary>Every word the host can say for an agent's state (the plate, the doing lines) comes filled.</summary>
public class Words
{
    [Test] public void ThePlatesAndDoingLinesAreFilled()
    {
        var h = Host.Fresh(); for (int k = 0; k < 4; k++) Host.NoTokens($"agent {k}", h.PlateWords(k), h.FireWords(k), h.DoingOf(k).Label);
        h.GiveWatchScripted(OpId.Scout, "waitress"); Host.Until(h, () => h.Game.Ops[0].Busy != null); Host.NoTokens("watching", h.PlateWords(0), h.DoingOf(0).Label, h.DoingOf(0).CancelText, h.DoingOf(0).OnDone);
    }
    [Test] public void APinnedSeedIsThatSeed()
    {
        HandoverHost.SeedPin = 7; try { var a = Host.Fresh(); Assert.That(a.Game.Seed, Is.EqualTo(7)); Assert.That(Host.Fresh().Game.Ops[0].Pos, Is.EqualTo(a.Game.Ops[0].Pos)); } finally { HandoverHost.SeedPin = 0; }
    }
    [Test] public void TheTextIsFableNotThePlaceholder() => Assert.That(PlayText.Load().Source, Does.StartWith("text.json"));
}

public static class Root { public static string Path = ""; }

public static class Runner
{
    public static int Main(string[] args)
    {
        var root = args[0]; Root.Path = root; var filter = args.Length > 1 ? args[1] : "";
        HostLogicShim.Streaming = System.IO.Path.Combine(root, "unity/Assets/StreamingAssets"); HostLogicShim.Data = System.IO.Path.Combine(root, "unity/Assets"); HostLogicShim.Persistent = System.IO.Path.Combine(root, "out/batch/hostlogic/persistent"); Directory.CreateDirectory(HostLogicShim.Persistent);
        HostLogicShim.ScreenW = 1920; HostLogicShim.ScreenH = 1080; HostLogicShim.Delta = 0.016f;
        var sw = Stopwatch.StartNew(); int pass = 0, fail = 0, skip = 0;
        foreach (var t in Assembly.GetExecutingAssembly().GetTypes().OrderBy(t => t.FullName))
            foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => m.GetCustomAttribute<TestAttribute>() != null))
            {
                var name = $"{t.Name}.{m.Name}"; if (filter.Length > 0 && !name.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
                try { m.Invoke(Activator.CreateInstance(t), null); pass++; }
                catch (TargetInvocationException e) when (e.InnerException is InconclusiveException) { skip++; Console.WriteLine($"SKIP {name}: {e.InnerException.Message.Trim()}"); }
                catch (TargetInvocationException e) { fail++; var x = e.InnerException; while (x is TargetInvocationException && x.InnerException != null) x = x.InnerException; var at = (x.StackTrace ?? "").Split('\n').Where(l => l.Contains("/unity/") || l.Contains("/hostlogic/")).Take(4).Select(l => l.Trim().Replace(root + "/", "")); Console.WriteLine($"FAIL {name}: {x.GetType().Name}: {x.Message.Trim()}\n     {string.Join("\n     ", at)}"); }
            }
        Console.WriteLine($"host logic: {pass} pass, {fail} fail, {skip} skipped in {sw.Elapsed.TotalSeconds:F1} s (no Unity)");
        return fail > 0 ? 1 : 0;
    }
}

/// <summary>Brief 088: the book's slot rule, and its pages against the ring, the words and the engine. The picture, the clock under real frames and the keys are ProofBook's (in r083).</summary>
public sealed class BookCases
{
    static float Tw(string t, float size, string font) => (t ?? "").Length * size * .52f;
    [Test] public void EmptySlotsTakeNoRoom()
    {
        BookPage Full() { var p = new BookPage { HeroFace = "x", Name = "A name", Sub = "a sub", State = "On", FactsHead = "head" }; p.Facts.Add(new BookFact { Word = "fact" }); p.Gear.Add(new BookFact { Word = "gear" }); p.Weapon = new BookWeapon { Word = "w" }; p.Weapon.Stats.Add(("damage", "Damage", 2, 5, 1f)); p.Ways.Add(new BookWay { Word = "way" }); p.Can.Add(new BookCan { Word = "can", Line = "a line" }); p.Scales.Add(new BookScale { Word = "s", Value = 1, Max = 2 }); p.Knows.Add(new BookKnow { Word = "k" }); p.Track.Add(new BookStop { Word = "t", Time = "0:01" }); p.Others.Add(new BookOther { Word = "o" }); return p; }
        var full = BookLayout.Place(Full(), Tw); Assert.That(BookLayout.Place(new BookPage(), Tw).Items, Is.Empty); Assert.That(full.Items.Count, Is.GreaterThanOrEqualTo(10));
        void Without(Action<BookPage> cut, BookSlot slot) { var p = Full(); cut(p); var r = BookLayout.Place(p, Tw); Assert.That(r.Has(slot), Is.False, slot + " is empty"); Assert.That(r.Bottom, Is.LessThanOrEqualTo(full.Bottom), slot + " takes no room"); }
        Without(p => p.Facts.Clear(), BookSlot.Facts); Without(p => p.Gear.Clear(), BookSlot.Gear); Without(p => p.Weapon = null, BookSlot.Weapon); Without(p => p.Ways.Clear(), BookSlot.Ways); Without(p => p.Can.Clear(), BookSlot.Can); Without(p => p.Scales.Clear(), BookSlot.Scales); Without(p => p.Knows.Clear(), BookSlot.Knows); Without(p => p.Track.Clear(), BookSlot.Track); Without(p => p.Others.Clear(), BookSlot.Others); Without(p => p.Name = "", BookSlot.Name); Without(p => { p.Sub = ""; p.State = ""; }, BookSlot.Sub);
        var noGear = Full(); noGear.Gear.Clear(); noGear.Weapon = null; noGear.Track.Clear(); var withGear = Full(); withGear.Track.Clear(); Assert.That(BookLayout.Place(withGear, Tw).Of(BookSlot.Ways).First().At.y, Is.GreaterThan(BookLayout.Place(noGear, Tw).Of(BookSlot.Ways).First().At.y), "the ways rise when the gear is empty");
    }
    [Test] public void FitRulesStepDown()
    {
        var p = new BookPage(); for (int i = 0; i < 12; i++) p.Facts.Add(new BookFact { Word = "A fact of some length" }); var f = BookLayout.FitFacts(p.Facts, Tw); Assert.That(f.cols, Is.EqualTo(2)); Assert.That(f.size, Is.LessThan(30f)); Assert.That(BookLayout.NameSize(new string('n', 30), 0, Tw), Is.LessThan(112f));
        var cn = new BookPage(); for (int i = 0; i < 9; i++) cn.Can.Add(new BookCan { Word = "w", Line = string.Join(" ", Enumerable.Repeat("nnnnnnn", 14)) }); Assert.That(BookLayout.Place(cn, Tw).CanLines[0].Length, Is.EqualTo(2)); Assert.That(BookLayout.Place(cn, Tw).Of(BookSlot.Can).First().Size, Is.LessThan(32f), "many verbs step down");
        var w = new BookPage(); for (int i = 0; i < 6; i++) w.Ways.Add(new BookWay { Word = "Pressure" }); var ys = BookLayout.Place(w, Tw).Of(BookSlot.Ways).Select(i => i.At.y).Distinct().Count(); Assert.That(ys, Is.GreaterThan(1), "ways wrap");
        Assert.That(BookLayout.SignY(0, 5), Is.LessThan(BookLayout.SignY(0, 4)), "a fifth sign re-spaces the list");
    }
    [Test] public void EveryFailIsOnTheJobPage()
    {
        var bw = BookWordsFile.Get(); var listed = new HashSet<string>(bw.Ends.SelectMany(e => e.Fails));
        var engine = new HashSet<string>(); foreach (var f in Directory.GetFiles(System.IO.Path.Combine(Root.Path, "sim/Suborned.Sim"), "*.cs")) if (!System.IO.Path.GetFileName(f).StartsWith("Scenarios")) foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(f), "Fail\\(\"([a-z-]+)\"")) engine.Add(m.Groups[1].Value);
        engine.Remove("engine-error"); Assert.That(engine.Count, Is.GreaterThan(8)); Assert.That(listed, Is.EquivalentTo(engine), "the job page's list is every way the engine ends a run");
    }
    [Test] public void JobAndCellPagesFromTheGame()
    {
        var h = Host.Fresh(); var job = h.BookJobPage(); Assert.That(job.Name, Is.Not.Empty); /* the ways read the built copy of brief.json: ProofBook.bookJobWay holds them */ Assert.That(job.Step, Is.EqualTo(0)); Assert.That(job.Track, Is.Empty, "brief 108: the briefing's pages have no clock strip"); Assert.That(BookWordsFile.Get().Dangers.Single(d => d.Id == "rivals").When, Is.EqualTo("2:35"), "brief 096, Sky's call 2: the rivals' 2:35 shows from the start (brief 108: on the look-out page)");
        for (int k = 0; k < 4; k++)
        {
            var c = h.BookCellPage(k); Assert.That(c.Weapon, Is.Not.Null); Assert.That(c.Weapon.Stats.ConvertAll(x => x.id), Is.EqualTo(new List<string> { "damage", "rate", "range", "aim" })); Assert.That(c.Scales[0].Max, Is.EqualTo(h.Game.Ops[k].Gun.Max)); Assert.That(c.Ways, Is.Empty, "no ways on an agent"); Assert.That(c.Can.Count, Is.EqualTo(HandoverHost.Slots[k].Length + 1), "one row a verb of the plate, then the car's (brief 096)"); Assert.That(c.Can[^1].Id + ":" + c.Can[^1].Icon, Is.EqualTo("car:car"));
            foreach (var w in c.Can) { Assert.That(w.Word, Is.Not.Empty, w.Id); Assert.That(w.Line, Is.Not.Empty, w.Id); foreach (var n in new[] { "Inez", "Vale", "Okafor" }) Assert.That(w.Word + w.Line, Does.Not.Contain(n)); }
        }
        var s = h.BookCellPage(0); Assert.That(s.Facts.Exists(f => f.Word.Contains("30")), "the Scout's reach is the rulebook's 30 m");
        int Pips(int k, string id) => h.BookCellPage(k).Weapon.Stats.Find(x => x.id == id).pips;
        Assert.That(Pips(3, "damage"), Is.GreaterThan(Pips(0, "damage")), "the carbine reads heavier"); Assert.That(Pips(3, "range"), Is.GreaterThan(Pips(0, "range")), "and longer"); Assert.That(Pips(3, "aim"), Is.GreaterThan(Pips(2, "aim")), "the Enforcer aims better than the Tech");
        Assert.That(HandoverHost.BookPips(0f, 3f, 5), Is.EqualTo(0)); Assert.That(HandoverHost.BookPips(0.1f, 3f, 5), Is.EqualTo(1)); Assert.That(HandoverHost.BookPips(99f, 3f, 5), Is.EqualTo(5));
        foreach (var l in BookWordsFile.Get().Needs.Values) foreach (var n in l) { Assert.That(h.BookCheck(n.Check), Is.Not.Null, n.Id + ": " + n.Check); foreach (var m in new[] { "call", "loop", "takedown", "knock", "kill", "pay", "help", "pressure", "cut" }) Assert.That(n.Words.ToLowerInvariant(), Does.Not.Contain(m), n.Id + " names a method"); }
        Assert.That(h.BookCheck("guard-away"), Is.EqualTo(!h.Game.GuardAtDoor)); Assert.That(h.BookCheck("vale:Car"), Is.False); Assert.That(h.BookCheck("no-such-word"), Is.Null);
        Assert.That(h.BookCellPage(0).Sub, Does.StartWith("Young"), "the who line, not the mission's");
        var e = h.BookCellPage(3).Can; Assert.That(e.FindAll(x => x.Id == "takedown").Count, Is.EqualTo(1), "the takedown is one verb"); Assert.That(e.Exists(x => x.Word is "Kill" or "Knockout"), Is.False);
        var before = string.Join("|", h.BookCellPage(3).Can.ConvertAll(x => x.Word)); Host.Tick(h, 400); Assert.That(string.Join("|", h.BookCellPage(3).Can.ConvertAll(x => x.Word)), Is.EqualTo(before), "the rows don't move with the clock"); int orders = h.Game.Orders.Count; h.BookCellPage(1); Assert.That(h.Game.Orders.Count, Is.EqualTo(orders), "a page gives no order");
    }
}
