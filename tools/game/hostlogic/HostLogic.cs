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

public static class Runner
{
    public static int Main(string[] args)
    {
        var root = args[0]; var filter = args.Length > 1 ? args[1] : "";
        HostLogicShim.Streaming = System.IO.Path.Combine(root, "unity/Assets/StreamingAssets"); HostLogicShim.Data = System.IO.Path.Combine(root, "unity/Assets"); HostLogicShim.Persistent = System.IO.Path.Combine(root, "out/batch/hostlogic/persistent"); Directory.CreateDirectory(HostLogicShim.Persistent);
        HostLogicShim.ScreenW = 1920; HostLogicShim.ScreenH = 1080; HostLogicShim.Delta = 0.016f;
        var sw = Stopwatch.StartNew(); int pass = 0, fail = 0, skip = 0;
        foreach (var t in Assembly.GetExecutingAssembly().GetTypes().OrderBy(t => t.FullName))
            foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => m.GetCustomAttribute<TestAttribute>() != null))
            {
                var name = $"{t.Name}.{m.Name}"; if (filter.Length > 0 && !name.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
                try { m.Invoke(Activator.CreateInstance(t), null); pass++; }
                catch (TargetInvocationException e) when (e.InnerException is InconclusiveException) { skip++; Console.WriteLine($"SKIP {name}: {e.InnerException.Message.Trim()}"); }
                catch (TargetInvocationException e) { fail++; var x = e.InnerException; while (x is TargetInvocationException && x.InnerException != null) x = x.InnerException; var at = (x.StackTrace ?? "").Split('\n').Where(l => l.Contains("/unity/") || l.Contains("HostLogic.cs")).Take(4).Select(l => l.Trim().Replace(root + "/", "")); Console.WriteLine($"FAIL {name}: {x.GetType().Name}: {x.Message.Trim()}\n     {string.Join("\n     ", at)}"); }
            }
        Console.WriteLine($"host logic: {pass} pass, {fail} fail, {skip} skipped in {sw.Elapsed.TotalSeconds:F1} s (no Unity)");
        return fail > 0 ? 1 : 0;
    }
}
