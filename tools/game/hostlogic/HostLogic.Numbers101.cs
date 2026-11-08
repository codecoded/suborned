// Brief 101, Sky's rule on numbers in play (2026-11-08: "I agree", no hit chance; "Don't look at how long something takes because I don't have a choice with the
// action so it's irrelevant as a player. Only thing that feels time is important are the key events that happen"): the words the HUD draws for the player's own
// orders carry no hit chance and no length of an act or a walk. Read here: the price line (the pill, the ring's plates, the chip, the card), its Alt fragments, the
// pill's live words, the portrait plates, the fire words, a queued walk's ghost and the cancel tag. Key events keep their time (the seal, police, the rivals, a report
// on its way, a loop's end) and are not read here. Same harness as HostLogic.cs: the real engine and text.json, a fresh game, no views.
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using UnityEngine;
using Order = Suborned.Sim.Order;

public class Numbers101
{
    static readonly Regex Secs = new(@"\d+(\.\d+)?\s?s\b"), Pct = new(@"\d+\s?%");
    static void None(string where, string w)
    {
        Assert.That(Pct.IsMatch(w ?? ""), Is.False, $"{where}: a hit chance in '{w}'");
        Assert.That(Secs.IsMatch(w ?? ""), Is.False, $"{where}: an act's or a walk's seconds in '{w}'");
    }
    static HandoverHost.Target T(string kind, string key, string label) => new() { Kind = kind, Key = key, Id = key, Label = label };
    /// <summary>The things the ring opens on at the start: the cast, the cameras, the patrol, the door, the cars. Scout, Fixer, Tech (a panel with the
    /// Enforcer in it needs HostViews.KeyOf, silent here: HostLogic.cs's Squad).</summary>
    static readonly HandoverHost.Target[] Things =
    {
        T("person", "waitress", "Inez"), T("person", "guard", "the guard"), T("person", "driver", "the driver"), T("patrol", "patrol", "the patrol"), T("door", "door", "the public door"),
        new() { Kind = "camera", Key = "entrance", Label = "the entrance camera" }, new() { Kind = "camera", Key = "pickup", Label = "the pickup camera" },
        new() { Kind = "car", Key = "getaway", Label = "our car" }, new() { Kind = "car", Key = "official", Label = "the official car" },
    };

    [Test] public void NoWayOnTheRingIsPricedInTimeOrChance()
    {
        var h = Host.Fresh(); int rows = 0; var verbs = new HashSet<string>();
        foreach (var k in new[] { OpId.Scout, OpId.Fixer, OpId.Tech })
        {
            h.Select(k, false);
            foreach (var t in Things)
            {
                var p = h.ContextFor(t, new Vector2(100, 100)); if (p?.Pn == null) continue;
                foreach (var r in p.Pn.Rows)
                {
                    var i = r.I; if (i?.Order == null) continue; rows++; verbs.Add(i.Order.Do); string at = $"{k} on {t.Key}: {i.Order.Do}";
                    var line = HudModel.PriceLine(h, i);
                    Assert.That(line.Exists(f => f.Kind == "time"), Is.False, $"{at}: a time field on the price");
                    foreach (var f in line) if (f.Kind != "door") None($"{at} {f.Kind}", f.Text); // the door field is the window a pressed call opens (m:ss), not the act's length
                    None($"{at}: the pill", HudToolkit.PillLine(h, i));
                    foreach (var f in HudModel.AltLine(h, i)) Assert.That(f.Icon is "s_moving" or "p_time", Is.False, $"{at}: Alt splits the walk from the act ('{f.Text}')");
                }
            }
        }
        Assert.That(rows, Is.GreaterThan(20), $"the sweep read {rows} rows ({string.Join(",", verbs)})"); Assert.That(verbs, Does.Contain("watch").And.Contain("listen"));
    }

    /// <summary>On rolled games (the seed a new game rolls), where brief 080's odds used to show.</summary>
    [Test] public void TheFireOfferSaysWhatTheStreetWillDoNotTheOdds()
    {
        foreach (var seed in new[] { 7, 1234 })
        {
            HandoverHost.SeedPin = seed;
            try
            {
                var h = Host.Fresh(); h.Select(OpId.Enforcer, false); var g = h.Game; Assert.That(g.Rolled, $"seed {seed} rolls");
                g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "draw" }); Host.Tick(h, 20);
                var fo = h.FireOfferFor(T("person", "guard", "the guard")); Assert.That(fo, Is.Not.Null, "the Enforcer can fire on the guard");
                var pill = HudToolkit.PillLine(h, fo); None($"seed {seed}: the fire offer", pill);
                Assert.That(pill, Does.Contain("Trail").And.Contain("Alert"), $"a shot's price is its Trail and the Alert it raises: '{pill}'");
                Assert.That(fo.Time, Is.Empty, "no seconds to down on the offer");
            }
            finally { HandoverHost.SeedPin = 0; }
        }
    }

    [Test] public void LiveWordsAndPlatesSayTheActNotItsSeconds()
    {
        var h = Host.Fresh(); var g = h.Game; var inez = T("person", "waitress", "Inez"); var p = h.ContextFor(inez, new Vector2(100, 100));
        int w0 = p.Rep.FindIndex(i => p.Pn.Rows[i].I.Order.Do == "watch"); Assert.That(w0, Is.GreaterThanOrEqualTo(0), "the Scout can Watch Inez");
        Host.Call(h, "HoldGive", p, w0, inez, Vector2.zero, false, "left", true, null); // as a hold gives it: the pill remembers the act
        for (int i = 0; i < 40 && g.Ops[0].Busy == null; i++) { Host.Later(1f); None("walking in: the pill", h.OrderStatus(inez)?.Text); None("walking in: the plate", h.PlateWords(0)); Host.Tick(h, 10); }
        Assert.That(Host.Until(h, () => g.Ops[0].Busy != null, 1500), "the Scout reaches Inez"); Host.Later(1f); Host.Tick(h, 20);
        var w = h.OrderStatus(inez); Assert.That(w?.Text, Is.EqualTo("Watching"), $"the pill on Inez: '{w?.Text}'"); Assert.That(h.PlateWords(0), Is.Empty, "the plate is the act's icon and its bar");
        var cancel = h.WouldCancel(0); Assert.That(cancel.HasValue, "a new order would drop the Watch"); None("the cancel tag", cancel.Value.cost);
        var m = new HudModel(); HostLogicShim.Frame++; m.Frame(h); None("the roster's under line", m.Slots[0].Under);
    }

    [Test] public void FiringAndAimingSayNoSecondsToDown()
    {
        var h = Host.Fresh(); var g = h.Game; h.SelectAll();
        var c = new HandoverHost.HoldCharge { Fire = true, FireId = "guard", Button = 1, T = T("person", "guard", "the guard") }; Host.Call(h, "BeginFireLock", c, Vector2.zero);
        var seen = new List<string>();
        for (int i = 0; i < 60; i++)
        {
            Host.Tick(h, 5); Host.Later(0.5f);
            var s = h.FireStatus(null, true); if (s != null) { seen.Add(s.Text); None("the fire pill", s.Text); }
            for (int k = 0; k < 4; k++) { None($"agent {k}'s plate", h.PlateWords(k)); None($"agent {k}'s fire words", h.FireWords(k)); }
            if (g.GunOf("guard")?.Down == true) break;
        }
        Assert.That(seen.Exists(x => x is "Aiming" or "Firing" or "Drawing"), $"the pill said: {string.Join(" | ", seen.Distinct())}");
    }

    [Test] public void AQueuedWalkSaysItsGaitOnly()
    {
        var h = Host.Fresh(); var g = h.Game; h.SetPaused(true); h.controls.planTogether = true;
        var at = g.Ops[0].Pos; var to = g.Walk.NearestFree(new Pt(at.X + 400, at.Y + 300)) ?? at; h.GroupWalk(to, false, null, null);
        Assert.That(h.Plan.Count, Is.EqualTo(1), "paused with start together: the walk waits as a ghost"); Assert.That(h.Plan[0].Label, Is.EqualTo("Walk"));
        h.GroupWalk(to, true, null, null); Assert.That(h.Plan.Count, Is.EqualTo(1)); Assert.That(h.Plan[0].Label, Is.EqualTo("Run"));
    }
}
