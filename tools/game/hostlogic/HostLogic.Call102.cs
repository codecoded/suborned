// Brief 102, engine 0.51.0 (Sky's calls on the guard; Keel's report briefs/102/keel/report-turned-guard-call.md, Motive's briefs/102/motive/turned-guard.md §1,
// Glyph's briefs/101/glyph/r4/notes.md "For Aster" 2): the host's side of the Call's offer. A turned contact with nothing to call (the turned driver, a contact whose
// tie a test cuts) has no Call row anywhere, and the Fixer's click on them answers with one "Already turned" lock; the turned guard's Call is offered and gives
// call.guard (option 1); a Call shut by a memory of the cell's violence (call-saw-violence) rings nothing; the door clocks read the pair. Real engine states
// throughout: a scenario's start fixes who is turned (or untied), and the rest is played through the host as a click gives it.
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using UnityEngine;
using Order = Suborned.Sim.Order;

public class Call102
{
    /// <summary>A host on a fresh run whose game starts with these roles turned, the ties of `untie` cut (a scenario fixture), the Fixer selected.</summary>
    static HandoverHost Start(string[] turned, string[] untie = null, System.Action<Suborned.Sim.Start> more = null)
    {
        var h = Host.Fresh(); h.controls.conversations = ConversationScenes.Never; Host.Set(h, "HintsOn", false);
        var s = new Start(); foreach (var r in turned) s.Turned.Add(r); foreach (var r in untie ?? new string[0]) s.Untie.Add(r); more?.Invoke(s);
        Host.Set(h, "Game", Engine.New(h.PlayInputs, s)); Host.Tick(h); h.Select(OpId.Fixer, false); Host.Call(h, "Track"); return h;
    }
    static HandoverHost.Target Person(HandoverHost h, string role) => new HandoverHost.Target { Kind = "person", Key = role, Id = role, Label = role };
    static HandoverHost.PanelRow[] FixerRows(HandoverHost.ContextPlan p) => p.Pn.Rows.Where(r => r.By == OpId.Fixer).ToArray();
    static string Rows(HandoverHost.ContextPlan p) => string.Join("; ", p.Pn.Rows.Select(r => $"{r.By}:{r.I?.Order?.Do}:{r.I?.Order?.Approach}:{(r.Available ? "open" : r.Lock.code)}"));
    static HandoverHost.RingState Ring(HandoverHost h, HandoverHost.ContextPlan p) { Host.Call(h, "OpenRing", p, new Vector2(400, 400), false); Assert.That(h.Ring, Is.Not.Null, "no ring"); return h.Ring; }
    static HandoverHost.PopUp LastPop(HandoverHost h) => h.Pops.LastOrDefault();
    static string Intel(HandoverHost h, string role) { var m = new HudModel(); m.Refresh(h); return m.Intel.First(r => r.Role == role).Next; }

    /// <summary>The Fixer's way on a turned contact with nothing to call: no Call row, none of the four turn ways (absent by the same law), and one shut Turn
    /// wedge at 12 o'clock with the turned pictogram and Fable's already-turned short; a click (the quick click, a picked verb) answers with that lock and gives nothing.</summary>
    static void AlreadyTurnedOnly(HandoverHost h, string role)
    {
        var g = h.Game; Assert.That(g.PersonByRole(role).Turned && !g.CallOffered(role), $"{role}: turned, with no Call offered");
        var p = Host.Plan(h, role); var fx = FixerRows(p);
        Assert.That(p.Pn.Rows.Any(r => r.I?.Order?.Do == "call"), Is.False, $"{role}: a Call row: {Rows(p)}");
        Assert.That(fx.Length, Is.EqualTo(1), $"{role}: the Fixer's one row: {Rows(p)}");
        var lk = fx[0]; string shortLock = h.Text.ShortLock("already-turned");
        Assert.That((lk.I.Order.Do, lk.I.Order.Approach, lk.Available, lk.Lock.code, lk.I.Lead), Is.EqualTo(("turn", "", false, "already-turned", "turned")), $"{role}: the Already turned lock");
        Assert.That(shortLock, Is.Not.Null.And.Not.Empty, "text.json has a short for already-turned");
        var rs = Ring(h, p); var w = rs.Segs[0];
        Assert.That((w.Shut, w.Icon, w.Word, w.ShutText), Is.EqualTo((true, "turned", h.Text.Actions["ring-turn"].Label, shortLock)), $"{role}: the wedge at 12 o'clock: {h.RingWords}");
        Assert.That(rs.Segs.Count(s => !s.More), Is.EqualTo(1), $"{role}: one wedge, not four: {h.RingWords}"); h.CloseRing();
        int n0 = g.Orders.Count; h.ActContext(Person(h, role), new Vector2(400, 400));
        Assert.That((g.Orders.Count, LastPop(h)?.Text), Is.EqualTo((n0, shortLock)), $"{role}: the quick click pops the lock and gives nothing");
        h.Pick(0); Assert.That(h.PickedId, Is.EqualTo("ask")); h.Pops.Clear(); h.ActContext(Person(h, role), new Vector2(420, 400));
        Assert.That((g.Orders.Count, LastPop(h)?.Text), Is.EqualTo((n0, shortLock)), $"{role}: a picked Ask pops the lock and gives nothing"); Host.Set(h, "Picked", -1);
        Assert.That(Intel(h, role), Is.EqualTo(""), $"{role}: Intel's call line shows only where a Call is offered");
    }

    [Test] public void TheTurnedDriverHasNoCallRow() => AlreadyTurnedOnly(Start(new[] { "driver" }), "driver");

    [Test] public void AnUntiedContactHasNoCallRow()
    {
        var h = Start(new[] { "waitress", "guard" }, untie: new[] { "waitress" });
        Assert.That(h.Game.Inez.Lover == null && h.Game.GuardP.Lover == null, "the tie is cut at both ends");
        AlreadyTurnedOnly(h, "waitress"); AlreadyTurnedOnly(h, "guard");
    }

    /// <summary>Option 1: the turned guard's Call is offered (Inez needn't be turned), open, and a click on its wedge gives it: call.guard, the Fixer's call card,
    /// and he walks off his door to her post. Intel says call him.</summary>
    [Test] public void TheTurnedGuardsCallIsOfferedAndGivesCallGuard()
    {
        var h = Start(new[] { "guard" }); var g = h.Game;
        Assert.That(g.CallOffered("guard") && !g.CallOffered("waitress"), "his Call is offered; hers isn't (she isn't turned)");
        Assert.That(Intel(h, "guard"), Is.EqualTo("Call them when it counts"));
        var p = Host.Plan(h, "guard"); var fx = FixerRows(p);
        Assert.That(fx.Length == 1 && fx[0].I.Order.Do == "call" && fx[0].I.Order.Target == "guard" && fx[0].Available, $"the Fixer's one row is his open Call: {Rows(p)}");
        Ring(h, p); int f0 = h.Feed.Count, l0 = g.Log.Count;
        Assert.That(h.RingTakeScripted("call", "guard"), $"no Call wedge to take: {h.RingWords}");
        Assert.That(g.Log.Skip(l0).Any(l => l.Code == "call.guard") && g.LureCalled == "guard", $"no call.guard: {string.Join(", ", g.Log.Skip(l0).Select(l => l.Code))}");
        var card = h.Feed.Skip(f0).FirstOrDefault(c => c.Kind == "call" && c.Target == "guard");
        Assert.That(card, Is.Not.Null, "the call card"); Assert.That(card.Tone, Is.EqualTo(HandoverHost.Feedback.Ok));
        var pick = h.ConvoPicks.LastOrDefault(); // the guard's own scene: Fable's conversation or scene `about` him once they land, else the act's own key
        Assert.That(pick == "call/guard" || h.Text.Conversations.Any(c => c.Id == pick && c.About == "guard") || h.Text.Scenes.Any(sc => PlayText.SceneKey(sc) == pick && sc.About == "guard"), $"the call's scene is the guard's: {pick}");
        Host.Tick(h, 2); Assert.That(g.GuardPose()?.phase, Is.EqualTo("walking to café"), "he leaves his door for her post");
    }

    /// <summary>Step 4 from his end: the Enforcer draws in front of the turned guard at his door; the Call stays offered, shut with Fable's call-saw-violence short. The
    /// wedge's press pops that lock and gives nothing; the order given anyway (an armed click, a race) is refused at no cost and rings nothing: no card, no line.</summary>
    [Test] public void ACallShutByViolenceRingsNothing()
    {
        var h = Start(new[] { "guard" }); var g = h.Game;
        var gp = g.GuardPose().Value; long len = System.Math.Max(1L, (long)System.Math.Sqrt((double)gp.facing.X * gp.facing.X + (double)gp.facing.Y * gp.facing.Y));
        var front = g.S.Grid.NearestFree(gp.pos + new Pt((int)(gp.facing.X * 300L / len), (int)(gp.facing.Y * 300L / len))) ?? gp.pos; // 3 m along his facing (the scenarios' FrontOfGuard)
        h.Issue(new Order { Op = "enforcer", Do = "move", Point = front }); Assert.That(Host.Until(h, () => g.Ops[(int)OpId.Enforcer].Walk == null, 3000), "the Enforcer never got in front of him");
        h.Issue(new Order { Op = "enforcer", Do = "draw" }); Assert.That(Host.Until(h, () => g.GuardP.Has("saw-weapon-drawn"), 100), "he never saw the weapon drawn");
        var q = g.CallQuery("guard"); Assert.That((q.ok, q.code, g.CallOffered("guard")), Is.EqualTo((false, "call-saw-violence", true)), "offered, shut by what he saw");
        string shortLock = h.Text.ShortLock("call-saw-violence"); Assert.That(shortLock, Is.Not.Null.And.Not.Empty);
        var p = Host.Plan(h, "guard"); var call = FixerRows(p).Single(r => r.I.Order.Do == "call");
        Assert.That((call.Available, call.Lock.code), Is.EqualTo((false, "call-saw-violence")), Rows(p));
        var rs = Ring(h, p); var w = rs.Segs.First(s => s.Row?.I?.Order?.Do == "call");
        Assert.That((w.Shut, w.ShutText), Is.EqualTo((true, shortLock)), h.RingWords);
        int f0 = h.Feed.Count, t0 = h.Talk.Count, b0 = h.Bubbles.Count, n0 = g.Orders.Count; long trail = g.Trail;
        Assert.That(h.RingTakeScripted("call", "guard"), "the shut Call wedge"); // a shut wedge's press: its pop, nothing given
        Assert.That((g.Orders.Count, LastPop(h)?.Text), Is.EqualTo((n0, shortLock)), "the press pops the lock and gives nothing");
        h.Pops.Clear(); h.Issue(new Order { Op = "fixer", Do = "call", Target = "guard" }, new Vector2(400, 400), null);
        Assert.That(g.Orders.Last().code, Is.EqualTo("call-saw-violence")); Assert.That((long)g.Trail, Is.EqualTo(trail), "refused at no cost");
        Assert.That(h.Feed.Count, Is.EqualTo(f0), $"no card: {string.Join(" | ", h.Feed.Skip(f0).Select(c => c.Title + ": " + string.Join(" / ", c.Lines.Select(l => l.Text))))}");
        Assert.That((h.Talk.Count, h.Bubbles.Count), Is.EqualTo((t0, b0)), "no line, no bubble");
        Assert.That(LastPop(h)?.Text, Is.EqualTo(shortLock), "the pop repeats the lock");
    }

    /// <summary>The door clocks read the pair (engine 0.51.0): the guard's Pressure card now shows the pressed window, as Inez's does; a Call card shows it only when
    /// the visit would be the short one, read from either end (CallWouldBePressed), never from the one called's memory alone.</summary>
    [Test] public void TheDoorClocksReadThePair()
    {
        var known = new System.Action<Suborned.Sim.Start>(s => { foreach (var r in new[] { "guard", "waitress" }) s.Known[r] = new System.Collections.Generic.List<string> { "routine", "need", "temperament", "secret" }; });
        var h = Start(new[] { "waitress" }, more: known); var g = h.Game;
        string Door(string role, string verb) { var p = Host.Plan(h, role); var r = p.Pn.Rows.FirstOrDefault(x => x.By == OpId.Fixer && (verb == "call" ? x.I.Order.Do == "call" : x.I.Order.Approach == verb)); Assert.That(r, Is.Not.Null, $"no {verb} row on {role}: {Rows(p)}"); return HudModel.PriceLine(h, r.I).FirstOrDefault(f => f.Kind == "door")?.Text; }
        string Want(int ticks) => (h.Text.DoorField ?? "Door {clock}").Replace("{clock}", HandoverHost.DoorWords(ticks));
        var w = g.GuardCallWindows("guard"); Assert.That(w, Is.Not.Null, "his visit would go");
        Assert.That(Door("guard", "pressure"), Is.EqualTo(Want(w.Value.pressed)), "the guard's Pressure card: the pressed window");
        Assert.That(Door("guard", "pressure"), Does.Contain("1:"), "about 1:40");
        Assert.That(Door("waitress", "call"), Is.Null, "Inez helped (no Pressure on either): her Call card has no door clock");
        h.GiveScripted(new Order { Op = "fixer", Do = "turn", Target = "guard", Approach = "pressure" });
        Assert.That(Host.Until(h, () => g.GuardP.Turned && g.Ops[(int)OpId.Fixer].Busy == null, 1500), $"the Pressure never turned him: {h.Status}");
        Assert.That(g.GuardP.Has("pressure") && !g.Inez.Has("pressure") && g.CallWouldBePressed("waitress") && g.CallWouldBePressed("guard"), "his Pressure, read from either end");
        var w2 = g.GuardCallWindows("waitress"); Assert.That(w2, Is.Not.Null);
        Assert.That(Door("waitress", "call"), Is.EqualTo(Want(w2.Value.pressed)), "Inez's Call card: the short visit his Pressure makes");
        Assert.That(Door("guard", "call"), Is.EqualTo(Want(g.GuardCallWindows("guard").Value.pressed)), "and his own");
    }
}
