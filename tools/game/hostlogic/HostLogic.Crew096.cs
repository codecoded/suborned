// Brief 096, the rivals round in play (engine 0.41.0 rivals.turn; Motive briefs/096/motive/rivals.json lookoutJob and thinning): the cell's ways on a member of the
// rival crew on foot (Watch, Listen, Pay, Mislead, the takedowns and the body rows), their prices and locks against the engine's own, the feed footers, the
// lookout card's chips and the H8 hint. Same harness as HostLogic.cs: the real engine and text.json, a fresh game with no orders, no views.
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using UnityEngine;
using Order = Suborned.Sim.Order;

public class Crew096
{
    /// <summary>route-b's own orders (as Continue replays a saved run) until just before its Mislead on the lookout (tick 2700), with the Fixer and the Enforcer
    /// sent to the Scout's spot by him; then the host's, its feed and cards read up to there. The lookout is posted (rivals.lookout-posted) by then.</summary>
    static HandoverHost Posted()
    {
        var h = Host.Fresh(); Host.Set(h, "HintsOn", false);
        var sc = Script.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Root.Path, "contracts/missions/handover/routes/route-b.json")));
        var g = Engine.New(h.PlayInputs, sc.Start);
        var orders = sc.Orders.Where(o => o.Tick < 2690).OrderBy(o => o.Tick).ToList(); int at = 0; bool sent = false;
        while (g.Tick < 2690 && !g.Over)
        {
            for (; at < orders.Count && orders[at].Tick <= g.Tick; at++) g.Issue(orders[at]);
            if (!sent && g.Tick >= 2200) { sent = true; g.Issue(new Order { Tick = g.Tick, Op = "fixer", Do = "move", Point = new Pt(9150, -2800) }); g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "move", Point = new Pt(9050, -2800) }); }
            g.RunTo(g.Tick + 1);
        }
        Assert.That(g.Log.Exists(l => l.Code == "rivals.lookout-posted"), $"no lookout posted by {g.Tick}: {g.Result} {g.FailCode}");
        Host.Set(h, "Game", g); Host.Call(h, "ResetCrucial"); Host.Set(h, "crucialLogAt", 0); Host.Call(h, "Track"); Host.Call(h, "CrucialTrack");
        return h;
    }
    static string Lookout(HandoverHost h) => h.LookoutId();
    static string PostedMan(HandoverHost h, string temperament = "") => h.Game.Crew.First(m => m.Role == "posted" && m.Mode == CrewMode.Posted && (temperament.Length == 0 || m.Temperament == temperament)).Id;
    static List<HandoverHost.PanelRow> Rows(HandoverHost h, OpId by, string id) { h.Select(by, false); return Host.Plan(h, id, "rivals").Pn.Rows; }
    static HandoverHost.PanelRow Row(HandoverHost h, OpId by, string id, string verb) => Rows(h, by, id).FirstOrDefault(r => (r.I.Order.Do == "turn" ? r.I.Order.Approach : r.I.Order.Do) == verb);
    static void Give(HandoverHost h, HandoverHost.PanelRow r, string id) => Host.Call(h, "Give", r.By, r.I.Order, new HandoverHost.Target { Kind = "rivals", Key = id, Id = id, Label = id }, null);
    static bool Run(HandoverHost h, Func<bool> done, int ticks = 4000) { for (int i = 0; i < ticks && !done() && !h.Game.Over; i += 5) { h.RunTo(h.Game.Tick + 5); Host.Call(h, "Track"); } Host.Call(h, "CrucialTrack"); return done(); }

    /// <summary>The Pay card's Trail as HostTape writes it from the engine's quote: the record's Trail in it where a camera would tape the Pay, "?" for one not yet watched.</summary>
    static string TapedTrail(Quote q) => q.Recorded == "yes" ? $"T +{q.Trail + q.RecordTrail}" : q.Recorded == "?" ? $"T ?+{q.Trail + q.RecordTrail}" : $"T +{q.Trail}";

    /// <summary>Engine 0.52.0 (Keel's CAM-10: the crew's Pay card carries the tape): a Pay on a member in a live camera's view shows its mark as a person's card does (HostTape):
    /// the camera, the record's Trail and Alert in its price, "?" until the camera is watched; a Pay out of every view, none. Every member on foot at 4:29 on route B.</summary>
    [Test] public void APayOnAManInACamerasViewCarriesItsTape()
    {
        var h = Posted(); var g = h.Game; g.RivalsWatched = true; var seen = new List<string>();
        foreach (var m in g.Crew.Where(x => x.OnFoot && !x.Paid && !x.Fight && !x.Gun.Down))
        {
            var pay = Row(h, OpId.Fixer, m.Id, "pay"); if (pay == null) continue;
            var q = g.CrewQuote(pay.I.Order, m); string tape = q.Recorded is "yes" or "?" ? q.Recorded : "";
            Assert.That((pay.I.Tape, pay.I.Trail), Is.EqualTo((tape, TapedTrail(q))), $"{m.Id}: the card's mark and Trail are the engine's");
            if (tape.Length == 0) { Assert.That(pay.I.Alert, Is.EqualTo("A 0"), $"{m.Id}: private, out of every view"); continue; }
            Assert.That((pay.I.TapeBy, pay.I.Alert), Is.EqualTo((q.RecordedBy, tape == "yes" ? $"A →{q.RecordAlert}" : $"A ?→{q.RecordAlert}")), m.Id);
            Assert.That(HudModel.PriceLine(h, pay.I).Find(f => f.Kind == "eye")?.Eye, Is.EqualTo(tape == "yes" ? "open" : "unk"), m.Id); seen.Add($"{m.Id} {tape} {q.RecordedBy}");
        }
        Assert.That(seen, Is.Not.Empty, "no member on foot stands in a camera's view at 4:29 on route B: " + string.Join(", ", g.Crew.Select(x => $"{x.Id} {x.Mode}")));
        Console.WriteLine("taped: " + string.Join("; ", seen));
    }

    [Test] public void EachAgentHasItsWaysOnTheLookoutAtTheEnginesPrices()
    {
        var h = Posted(); var g = h.Game; var id = Lookout(h); var m = g.CrewOf(id);
        var listen = Row(h, OpId.Scout, id, "listen"); Assert.That(listen, Is.Not.Null, "the Scout listens to him"); Assert.That(listen.Available, listen.Reason);
        var lq = g.CrewQuote(listen.I.Order, m); Assert.That(listen.I.Time, Is.EqualTo($"{lq.S} s"));
        var mis = Row(h, OpId.Scout, id, "mislead"); Assert.That(mis, Is.Not.Null, "the Scout's word to a man on foot"); Assert.That(mis.Available, mis.Reason);
        var mq = g.CrewQuote(mis.I.Order, m); Assert.That(mis.I.Time, Is.EqualTo($"{mq.S} s")); Assert.That(mis.I.Trail, Is.EqualTo($"T +{mq.Trail}"));
        Assert.That(mis.I.Rows.Exists(x => x.text.Contains(HandoverHost.MissionClock(g.MisleadBackTick(id)))), "it says when he is back at his post: " + string.Join(" | ", mis.I.Rows.Select(x => x.text)));
        var unwatched = Row(h, OpId.Fixer, id, "pay"); Assert.That(unwatched.Available, Is.False, "engine 0.43.0: Pay on the crew needs a Watch first"); Assert.That(unwatched.Lock.code, Is.EqualTo("needs-routine"));
        g.RivalsWatched = true; // rivals.turn.needs "watched": the Watch the rules ask for before a Pay
        var pay = Row(h, OpId.Fixer, id, "pay"); Assert.That(pay, Is.Not.Null); Assert.That(pay.Available, pay.Reason);
        var pq = g.CrewQuote(pay.I.Order, m); Assert.That(ContextRank.Num(pay.I.Usd), Is.EqualTo(pq.Usd)); Assert.That(pq.Usd, Is.EqualTo(30000), "Motive's $30,000");
        Assert.That(pay.I.Outcome, Is.EqualTo("?"), "his temperament unknown before a Listen"); Assert.That(pay.I.Trail, Is.EqualTo(TapedTrail(pq)), "the Pay's Trail, with a camera's record when one would tape it (engine 0.52.0)");
        var en = Rows(h, OpId.Enforcer, id); Assert.That(en.Exists(r => r.I.Order.Do == "takedown" && r.I.How == "knockout"), "Knock out"); Assert.That(en.Exists(r => r.I.Order.Do == "takedown" && r.I.How == "kill"), "Kill");
        Assert.That(Rows(h, OpId.Tech, id).Exists(r => r.I.Order.Do is "listen" or "turn" or "mislead"), Is.False, "the Tech has none of the talking ways");
        foreach (var k in new[] { OpId.Scout, OpId.Fixer, OpId.Enforcer }) foreach (var r in Rows(h, k, id)) Host.NoTokens($"{k} on the lookout", r.Verb, r.I.Does, r.Reason, string.Join(" ", r.I.Rows.Select(x => x.text)));
    }

    [Test] public void EveryRowsLockIsTheEnginesAnswer()
    {
        foreach (var setup in new Action<HandoverHost>[] { _ => { }, x => x.Game.Cash = 1000, x => x.Game.CrewOf(Lookout(x)).Refused = true })
            foreach (var k in new[] { OpId.Scout, OpId.Fixer })
                foreach (var verb in new[] { "listen", "mislead", "pay", "watch" })
                {
                    var h = Posted(); setup(h); var id = Lookout(h); var r = Row(h, k, id, verb); if (r == null) continue;
                    var a = h.Game.Issue(new Order { Tick = h.Game.Tick, Op = r.I.Order.Op, Do = r.I.Order.Do, Target = r.I.Order.Target, Approach = r.I.Order.Approach });
                    Assert.That(a.ok, Is.EqualTo(r.Available), $"{k} {verb}: the row says {(r.Available ? "open" : r.Lock.code)}, the engine {(a.ok ? "takes it" : a.code)}");
                    if (!a.ok) Assert.That(r.Lock.code, Is.EqualTo(a.code), $"{k} {verb}");
                }
    }

    [Test] public void ListenTeachesHimAndPayingTheMercenaryLookoutSendsHimOff()
    {
        var h = Posted(); var g = h.Game; var id = Lookout(h); var m = g.CrewOf(id);
        Assert.That(m.Temperament, Is.EqualTo("mercenary"), "Keel's report: rival-4, the lookout, is a Mercenary");
        Give(h, Row(h, OpId.Scout, id, "listen"), id); Assert.That(Run(h, () => m.KTemperament), "the Listen never landed");
        Assert.That(h.Feed.Exists(c => c.Verb == "listen" && c.Target == id && c.Footer.Contains("money")), "the feed says what was heard: " + string.Join(" | ", h.Feed.Select(c => c.Verb + ":" + c.Footer)));
        Assert.That(Row(h, OpId.Scout, id, "listen").Available, Is.False, "nothing more to hear (already-heard)");
        g.RivalsWatched = true; // the Fixer's free Watch out of the way: the quick click is the Pay
        var pay = Row(h, OpId.Fixer, id, "pay"); Assert.That(pay.I.Outcome, Is.EqualTo("turned")); Assert.That(pay.I.Does, Does.Contain("$30,000"));
        var plan = Host.Plan(h, id, "rivals"); Assert.That(plan.Choice.Act, Is.EqualTo(ContextRank.Act.Default), plan.Choice.Why); Assert.That(plan.Row(plan.Choice.Pick).I.Order.Approach, Is.EqualTo("pay"));
        Assert.That(h.CrucialQueue.Exists(c => c.Kind == "lookout"), "the lookout card is up while he watches");
        long cash = g.Cash; Give(h, pay, id); Assert.That(Run(h, () => m.Paid), "the Pay never landed");
        Assert.That(g.Cash, Is.EqualTo(cash - 30000)); Assert.That(h.CrucialQueue.Exists(c => c.Kind == "lookout"), Is.False, "paid off, the card leaves by itself");
        Assert.That(Rows(h, OpId.Scout, id).Exists(r => r.I.Order.Do is "listen" or "mislead" or "watch"), Is.False, "gone: nothing to say to him"); Assert.That(Row(h, OpId.Fixer, id, "pay"), Is.Null);
    }

    [Test] public void PayingALoyalManIsRefusedAndTheMoneyStays()
    {
        var h = Posted(); var g = h.Game; var id = PostedMan(h, "loyal"); var m = g.CrewOf(id);
        g.RivalsWatched = true; // engine 0.43.0 (rivals.turn.needs "watched"): a Pay on one of the crew needs a Watch on them first
        var pay = Row(h, OpId.Fixer, id, "pay"); Assert.That(pay.Available, pay.Reason); long cash = g.Cash;
        Give(h, pay, id); Assert.That(Run(h, () => m.Refused), "the refusal never came");
        Assert.That(g.Cash, Is.EqualTo(cash), "a refusal gives the money back");
        var again = Row(h, OpId.Fixer, id, "pay"); Assert.That(again.Available, Is.False); Assert.That(again.Lock.code, Is.EqualTo("refused-before"));
        Assert.That(h.Feed.Exists(c => c.Verb == "pay" && c.Target == id && c.Footer.Contains("money stays")), string.Join(" | ", h.Feed.Select(c => c.Verb + ":" + c.Footer)));
    }

    [Test] public void MisleadingTheLookoutTakesHisCardAwayUntilHeIsBack()
    {
        var h = Posted(); var g = h.Game; var id = Lookout(h); var m = g.CrewOf(id);
        Give(h, Row(h, OpId.Scout, id, "mislead"), id); Assert.That(Run(h, () => m.Misled), "the word never landed");
        Assert.That(h.CrucialQueue.Exists(c => c.Kind == "lookout"), Is.False, "walking off to the other door: no card");
        Assert.That(Run(h, () => m.Mode == CrewMode.Posted && m.Post == "lookout", 6000), $"never back at his post by {g.Tick} ({m.Mode} {m.Post} {m.Goal})");
        Assert.That(h.CrucialQueue.Count(c => c.Kind == "lookout"), Is.EqualTo(1), "back watching Popovic's car: the card again, once");
    }

    [Test] public void TheLookoutCardsChipsOpenHisRingForEachAgent()
    {
        var h = Posted(); var id = Lookout(h); h.Game.RivalsWatched = true; // engine 0.43.0: the Fixer's chip opens a Pay only after a Watch on the crew
        var card = h.CrucialQueue.Single(c => c.Kind == "lookout"); Assert.That(card.View, Is.EqualTo(id), "its line pans to him, not to Popovic's car");
        Assert.That(card.FixBy, Is.EqualTo(new[] { "scout", "fixer", "enforcer" }), "Mislead, Pay, Take: one chip an agent");
        h.CrucialQueue.RemoveAll(c => c != card);
        foreach (var (i, k, verb) in new[] { (0, OpId.Scout, "mislead"), (1, OpId.Fixer, "pay"), (2, OpId.Enforcer, "takedown") })
        {
            h.CrucialFix(i); Assert.That(h.Selected[(int)k] && h.Selected.Count(x => x) == 1, $"chip {i}: the {k} alone selected");
            var rs = h.Ring; Assert.That(rs, Is.Not.Null, $"chip {i}: his ring opens"); Assert.That(rs.Plan.T.Key, Is.EqualTo(id));
            Assert.That(rs.Segs.Exists(s => s.Row?.I?.Order is Order o && (o.Do == "turn" ? o.Approach : o.Do) == verb), $"chip {i}: {verb} on the ring: {string.Join(", ", rs.Segs.Select(s => s.Word))}");
            Host.Call(h, "CloseRing");
        }
        Assert.That(h.CrucialQueue.Contains(card), "the card stays");
    }

    [Test] public void TheLookoutKnockedOutCanBeTiedCarriedAndHidden()
    {
        var h = Posted(); var g = h.Game; var id = Lookout(h); var m = g.CrewOf(id);
        var front = Rows(h, OpId.Enforcer, id).First(r => r.I.Order.Do == "takedown" && r.I.How == "knockout"); Assert.That(front.I.Td.Armed, "a rival carries his pistol");
        Assert.That(front.I.Rows.Exists(x => x.icon == "p_alert"), Is.EqualTo(front.I.Td.Aware != "no"), $"the card says he fires if he sees it coming (Aware {front.I.Td.Aware})");
        var pose = g.RivalPoses().members.Single(x => x.id == id); double len = Math.Sqrt((double)pose.facing.X * pose.facing.X + (double)pose.facing.Y * pose.facing.Y);
        var behind = g.Walk.NearestFree(new Pt(pose.pos.X - (int)(pose.facing.X * 250 / len), pose.pos.Y - (int)(pose.facing.Y * 250 / len))).Value; // his blind side: he faces Popovic's car
        g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "move", Point = behind }); Assert.That(Run(h, () => G.Dist(g.Ops[(int)OpId.Enforcer].Pos, behind) < 60), "never got behind him");
        var ko = Rows(h, OpId.Enforcer, id).First(r => r.I.Order.Do == "takedown" && r.I.How == "knockout"); Assert.That(ko.Available, ko.Reason);
        Assert.That(ko.I.Td.Aware, Is.EqualTo("no"), "from behind he doesn't see it"); Assert.That(ko.I.Rows.Exists(x => x.icon == "p_alert"), Is.False);
        Give(h, ko, id); Assert.That(Run(h, () => m.Gun.Down && g.Ops[(int)OpId.Enforcer].Busy == null && g.BodyById(id) is { Seizing: false }), $"never knocked out by {g.Tick}: {g.Result} {g.FailCode} · {string.Join(" | ", g.Log.Skip(Math.Max(0, g.Log.Count - 8)).Select(l => l.Code + ": " + l.Text))}");
        h.Select(OpId.Enforcer, false); var rows = Host.Plan(h, id, "body").Pn.Rows;
        Assert.That(rows.Exists(r => r.I.Order.Do == "restrain" && r.Available), "Tie: " + string.Join(", ", rows.Select(r => r.Verb + (r.Available ? "" : " (" + r.Reason + ")"))));
        Assert.That(rows.Exists(r => r.I.Order.Do == "carry" && r.Available), "Carry, then Hide at a stash");
        Assert.That(h.CrucialQueue.Exists(c => c.Kind == "lookout"), Is.False, "down: no lookout card");
    }

    [Test] public void TheHintPointsTheScoutAtTheLookoutNotTheRetiredTip()
    {
        var h = Posted(); var g = h.Game; if (g.Tick < 190 * Game.Hz) h.RunTo(190 * Game.Hz); var args = new object[] { "H8", null, null };
        Assert.That((bool)typeof(HandoverHost).GetMethod("Trigger", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(h, args), "H8 is on while the lookout watches and nobody has spoken to the crew");
        var id = Lookout(h); Give(h, Row(h, OpId.Scout, id, "mislead"), id); Host.Tick(h);
        Assert.That((bool)typeof(HandoverHost).GetMethod("Trigger", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(h, args), Is.False, "given a Mislead: gone");
    }
}
