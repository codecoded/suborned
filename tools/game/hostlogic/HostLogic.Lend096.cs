// Brief 096, the lend (engine 0.42.0) and the rules round's marks (0.40.0, 0.41.0): two Go's, the cars' names, vale.out's card and the police countdown, the
// left-behind flags, End, the debrief's car lines, the lookout and boxed-in cards, the camera mark on Pay and Pressure, the Drive card's parked notice.
// Same harness as HostLogic.cs: the real engine and text.json, the route-b reference run (c052: the lent car out at 7:03, our car's Go at 7:47.2), no views.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using UnityEngine;
using Order = Suborned.Sim.Order;

public class Lend096
{
    const int Inf = int.MaxValue / 8;
    static HandoverHost.Target Car(string key) => new HandoverHost.Target { Kind = "car", Key = key, Id = key, Label = key };
    /// <summary>route-b's real orders played in the engine (as Continue replays a saved run) up to `until`, or until `stop` holds after a tick; the run is then the
    /// host's, its log read into the crucial cards from the start.</summary>
    static HandoverHost RouteB(int until, Func<Game, bool> stop = null)
    {
        var h = Host.Fresh(); Host.Set(h, "HintsOn", false);
        var sc = Script.Parse(File.ReadAllText(System.IO.Path.Combine(Root.Path, "contracts/missions/handover/routes/route-b.json")));
        var g = Engine.New(h.PlayInputs, sc.Start);
        var orders = sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList(); int at = 0;
        while (g.Tick < until && !g.Over && !(stop != null && stop(g)))
        {
            for (; at < orders.Count && orders[at].Tick <= g.Tick; at++) g.Issue(orders[at]);
            g.RunTo(g.Tick + 1);
        }
        Host.Set(h, "Game", g); Host.Call(h, "ResetCrucial"); Host.Set(h, "crucialLogAt", 0); Host.Call(h, "CrucialTrack");
        return h;
    }
    static HandoverHost Lent() { var h = RouteB(4230, x => x.Lent); Assert.That(h.Game.Lent, $"route-b never lent Popovic's car by tick {h.Game.Tick}"); return h; }
    static HandoverHost Out() { var h = RouteB(4600, x => x.ValeOutTick < Inf); Assert.That(h.Game.ValeOutTick, Is.LessThan(Inf), $"Popovic never out by tick {h.Game.Tick}: {h.Game.Result} {h.Game.FailCode}"); return h; }

    [Test] public void ThePopovicsCarIsOursWithItsOwnBoardStepOutAndGoAndNoDrive()
    {
        var h = Lent(); var g = h.Game; var bw = BookWordsFile.Get();
        Assert.That(Host.Call(h, "OfficialCarName"), Is.EqualTo(bw.LentName), "labelled as ours from lend.lent");
        Assert.That(Host.Call(h, "GetawayCarName"), Is.EqualTo(h.Text.TargetName("car-getaway", "our car")), "ours keeps its own name");
        h.Select(OpId.Scout, false); var rows = Host.Plan(h, "official", "car").Pn.Rows;
        var go = rows.Single(r => r.I.Order?.Do == "go"); Assert.That(go.I.Order.Target, Is.EqualTo("car:official")); Assert.That(go.I.Outcome, Is.EqualTo("win"));
        Assert.That(go.Lock.code ?? "", Is.EqualTo(g.LentGoQuery().code ?? ""), "its refusal is the engine's own (boxed, held, no driver, disabled, gone)");
        Assert.That(rows.Exists(r => r.I.Order?.Do == "move" && r.I.How == HandoverHost.StepOutHow && r.By == OpId.Enforcer), "the Enforcer at its wheel can step out");
        Assert.That(rows.Exists(r => r.I.Order?.Do == "drive-car"), Is.False, "never driven round (drive-lent)");
        var board = rows.Single(r => r.I.Order?.Do == "board" && r.By == OpId.Scout);
        Assert.That(board.I.Order.Target, Is.EqualTo("car:official")); Assert.That(board.Available, Is.False); Assert.That(board.Lock.code, Is.EqualTo("board-lent-full"));
        Assert.That(board.Lock.ids, Is.EqualTo("enforcer"), "full: it says who is at the wheel"); Assert.That(board.Unlock, Does.Contain("wheel"));
        var ours = Host.Plan(h, "getaway", "car").Pn.Rows;
        Assert.That(ours.Exists(r => r.I.Order?.Do == "drive-car" && r.By == OpId.Enforcer), Is.False, "the Enforcer at Popovic's wheel never drives ours");
        Assert.That(ours.Exists(r => r.I.How == HandoverHost.StepOutHow && r.By == OpId.Enforcer), Is.False, "Step out on our car is for those in ours");
        Host.NoTokens("lent rows", rows.Select(r => r.Verb).Concat(rows.Select(r => r.Reason)).ToArray());
    }

    [Test] public void WhilePopovicIsLentOurGoGetsTheRestClearAndNobodyIsCapturedYet()
    {
        var h = Lent(); var g = h.Game;
        var gv = (HandoverHost.Intent)Host.Call(h, "GoIntent", OpId.Scout); Assert.That(gv.Outcome, Is.EqualTo("clear"), "never a withdraw once lent");
        Assert.That(h.LeftBehindNow().All(x => x.fate == "stays"), string.Join(",", h.LeftBehindNow().Select(x => x.op + ":" + x.fate)));
        Assert.That(h.PoliceOn, Is.False); Assert.That(h.LeftFlagsOn, Is.False, "no Go card open and no countdown: no flags");
        Assert.That(h.CrucialLog.Exists(c => c.Kind == "lent"), "lend.lent is a crucial card");
    }

    [Test] public void ValeOutIsTheTopCardThenThePoliceCountdownFlagsTheRestAndEndEndsIt()
    {
        var h = Out(); var g = h.Game; Host.Call(h, "CrucialTrack");
        var card = h.CrucialLog.Find(c => c.Kind == "vale-out"); Assert.That(card, Is.Not.Null, "vale.out makes a crucial card");
        Assert.That(card.Rank, Is.EqualTo(System.Array.IndexOf(HandoverHost.CrucialOrder, "vale-out")), "the most urgent card but the Enforcer badly hurt carrying Popovic"); Assert.That(h.CrucialQueue.Contains(card) || card.Read, "it stays until clicked");
        Assert.That(h.CrucialLine(card), Does.Contain(HandoverHost.MissionClock(g.SplitPoliceAt)));
        Assert.That(h.PoliceOn); Assert.That(h.PoliceSpanS, Is.EqualTo((g.SplitPoliceAt - g.ValeOutTick + Game.Hz - 1) / Game.Hz)); Assert.That(h.PoliceLeftS, Is.InRange(1, h.PoliceSpanS));
        Assert.That(h.LeftFlagsOn, "the flags stay up while the countdown runs");
        var left = h.LeftBehindNow(); Assert.That(left.Count, Is.GreaterThan(0)); Assert.That(left.All(x => x.fate == "captured"), "after vale.out everyone not in a car that has left is captured");
        HostLogicShim.Frame++; var m = new HudModel(); m.Frame(h);
        foreach (var (op, fate, _) in left) Assert.That(m.Slots[(int)Op.Parse(op)].LeftFate, Is.EqualTo("captured"), op);
        Assert.That(h.PoliceAction(), Does.Contain(h.AgentName((int)Op.Parse(left[0].op))).Or.Contain("Scout").Or.Contain("Fixer").Or.Contain("Tech"));
        var gv = (HandoverHost.Intent)Host.Call(h, "GoIntent", OpId.Scout); Assert.That(gv.Outcome, Is.EqualTo("clear"));
        h.EndNow(); g.RunTo(g.Tick + 1);
        Assert.That(g.Over); Assert.That(g.Result, Is.EqualTo(Outcome.Success), "End after vale.out is a win"); Assert.That(g.Orders.Exists(o => o.order.Do == "end"));
        Assert.That(g.LeftBehindAtGo.Where(x => x.fate == "captured").Select(x => x.op), Is.EquivalentTo(left.Select(x => x.op)), "and those left are captured");
    }

    [Test] public void TheEndCardAfterTheLendSaysPopovicIsOutAndTheCarLinesArePlain()
    {
        var h = RouteB(4800); var g = h.Game; Assert.That(g.Result, Is.EqualTo(Outcome.Success), $"{g.Result} {g.FailCode} at {g.Tick}: {string.Join(" | ", g.Log.Skip(Math.Max(0, g.Log.Count - 12)).Select(l => l.Tick + " " + l.Code + " " + l.Text))}"); Assert.That(h.ValeOut);
        Host.Set(h, "EndDebrief", Debrief.Of(g)); Host.Set(h, "Ending", "test"); var m = new HudModel(); Host.Call(m, "EndCard", h);
        var bw = BookWordsFile.Get(); Assert.That(m.EndLines[0], Is.EqualTo(bw.EndWinHead), string.Join(" | ", m.EndLines)); Host.NoTokens("end card", m.EndLines.ToArray());
        // the car lines on their own: our car left behind, Popovic's car seen leaving (a lead)
        var d = new Debrief(); d.Next.CarSeenBy = "left"; d.Next.Lines.Add(new BetweenLine { Line = "lent-seen", Who = "officer-1" });
        var m2 = new HudModel(); Func<Dictionary<string, string>, string, string> W = (mm, k) => mm.TryGetValue(k, out var v) ? v : k;
        Host.Call(m2, "CarLines", h, d, h.Text, W);
        Assert.That(m2.EndLines, Is.EqualTo(new[] { bw.CarLost.Replace("{trail}", g.R.WitnessTrail.ToString()), Cap(bw.LentSeen.Replace("{by}", W(h.Text.GetawayBy, "officer")).Replace("{trail}", g.R.WitnessTrail.ToString())) }));
        Assert.That(string.Join(" ", m2.EndLines), Does.Not.Contain("left saw"), "the engine's 'left' is never read as a watcher");
        Host.NoTokens("car lines", m2.EndLines.ToArray());
    }
    static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    [Test] public void TheLookoutAndBoxedInRankWithRivalsForOurCarAndLeaveOnceNotTrue()
    {
        var h = Host.Fresh(); var g = h.Game;
        void Log(string code) => Host.Call(h, "CrucialFromLog", new LogLine { Tick = g.Tick, Code = code, Text = "" });
        Log("rivals.coming"); Log("rivals.lookout-posted"); Log("rivals.block"); Log("vale.out");
        var rank = h.CrucialQueue.ToDictionary(c => c.Kind, c => c.Rank);
        Assert.That(rank["vale-out"], Is.EqualTo(Array.IndexOf(HandoverHost.CrucialOrder, "vale-out"))); Assert.That(rank["lookout"], Is.EqualTo(Array.IndexOf(HandoverHost.CrucialOrder, "for-car"))); Assert.That(rank["boxed"], Is.EqualTo(rank["lookout"]));
        Assert.That(h.CrucialQueue.Select(c => c.Kind).ToList(), Is.EqualTo(new List<string> { "vale-out", "lookout", "boxed", "rival-car" }));
        Host.Call(h, "CrucialTrack");
        Assert.That(h.CrucialQueue.Exists(c => c.Kind is "lookout" or "boxed"), Is.False, "no lookout posted and nothing across our way out: both leave by themselves");
        var r = RouteB(4230, x => x.Log.Exists(l => l.Code == "rivals.lookout-posted"));
        var lk = r.CrucialLog.Find(c => c.Kind == "lookout"); Assert.That(lk, Is.Not.Null, "route-b's lookout takes his post: a crucial card");
        Assert.That(r.CrucialQueue.Contains(lk), "it stays while he watches Popovic's car"); Host.NoTokens("lookout", lk.Title, r.CrucialLine(lk));
    }

    [Test] public void APayOnCameraIsMarkedOnItsPriceAndHelpStaysPrivate()
    {
        var h = Host.Fresh(); var bw = BookWordsFile.Get();
        var i = new HandoverHost.Intent { Order = new Order { Op = "fixer", Do = "turn", Target = "driver", Approach = "pay" }, Usd = "$70,000", Time = "30 s", Trail = "T +1", Alert = "A 0" };
        var q = new Quote { Recorded = "yes", RecordedBy = "pickup", RecordTrail = 1, RecordAlert = 1, RecordInS = 12, RecordLoopEnds = 300 };
        Host.Call(h, "Tape", i, q);
        Assert.That(i.Trail, Is.EqualTo("T +2"), "the record's Trail is in the Trail"); Assert.That(i.Alert, Is.EqualTo("A →1"));
        Assert.That(i.TapeTip, Does.Contain("pickup camera")); Assert.That(i.TapeTip, Does.Contain("12 s")); Assert.That(i.TapeTip, Does.Contain(HandoverHost.MissionClock(300)), "loop ends inside the act");
        var eye = HudModel.PriceLine(h, i).Find(f => f.Kind == "eye"); Assert.That(eye.Eye, Is.EqualTo("open")); Assert.That(eye.Tip, Is.EqualTo(i.TapeTip));
        Assert.That(HudModel.AltLine(h, i).Exists(f => f.Icon == "loop" && f.Text == HandoverHost.MissionClock(300)));
        var u = new HandoverHost.Intent { Order = i.Order, Trail = "T +1", Alert = "A 0" }; Host.Call(h, "Tape", u, new Quote { Recorded = "?", RecordedBy = "pickup", RecordTrail = 1, RecordAlert = 1, RecordInS = 12 });
        Assert.That(HudModel.PriceLine(h, u).Find(f => f.Kind == "eye").Eye, Is.EqualTo("unk"), "a camera the Scout hasn't watched is '?'"); Assert.That(u.Trail, Is.EqualTo("T +1")); Assert.That(u.Alert, Is.EqualTo("A ?→1"));
        Host.NoTokens("tape", i.TapeTip, u.TapeTip);
        // the real cards: every turn row carries the engine's own mark (Help, Ask: never)
        h.Select(OpId.Fixer, false);
        foreach (var who in new[] { "driver", "waitress" })
            foreach (var r in Host.Plan(h, who).Pn.Rows.Where(x => x.I.Order?.Do == "turn"))
            {
                var pq = h.Game.Preview(r.I.Order.Approach, who, OpId.Fixer); string want = pq.Recorded is "yes" or "?" ? pq.Recorded : "";
                Assert.That(r.I.Tape, Is.EqualTo(want), $"{who} {r.I.Order.Approach}");
                if (r.I.Order.Approach is "help" or "ask") Assert.That(r.I.Tape, Is.Empty, "Help and Ask stay private");
            }
    }

    [Test] public void TheDriveCardSaysWhoWouldSeeTheCarParked()
    {
        var h = Host.Fresh(); var g = h.Game; int checkedStands = 0;
        foreach (var st in g.S.Stands)
        {
            var i = (HandoverHost.Intent)Host.Call(h, "DriveIntent", OpId.Tech, st); var q = g.ParkedQuote(st.Id);
            bool shows = q.priv && (q.seenBy.Count > 0 || q.maybe.Count > 0);
            Assert.That(i.Parked.Length > 0, Is.EqualTo(shows), st.Id);
            if (!shows) { Assert.That(HudModel.PriceLine(h, i).Exists(f => f.Kind == "eye"), Is.False, $"{st.Id}: a public bay has no eye"); continue; }
            checkedStands++; if (q.seenBy.Count > 0) Assert.That(i.ParkedTip, Does.Contain(q.reportS.ToString()), "a known watcher: when it would report"); Assert.That(i.Rows.Exists(r => r.text == i.ParkedTip));
            var eye = HudModel.PriceLine(h, i).Find(f => f.Kind == "eye"); Assert.That(eye, Is.Not.Null); Assert.That(eye.Tip, Is.EqualTo(i.ParkedTip)); Host.NoTokens("parked", i.ParkedTip);
        }
        Assert.That(checkedStands, Is.GreaterThan(0), "the service drive is private ground in the patrol's sweep: its Drive card says so");
    }

    [Test] public void TheNewWordsAreAllInTheText()
    {
        var bw = BookWordsFile.Get(); var t = PlayText.Load();
        foreach (var k in new[] { "vale-out", "lent", "lookout", "boxed" }) { Assert.That(bw.FeedCards.ContainsKey(k), k); Assert.That(bw.FeedCards[k].Title, Is.Not.Empty); }
        foreach (var w in new[] { bw.GoLent, bw.EndWinHead, bw.EndWinLine, bw.LentName, bw.CarLost, bw.LentSeen, bw.PoliceHead, bw.PoliceWhy, bw.PoliceLeft, bw.PoliceClear, bw.PoliceEnd, bw.TapeSeen, bw.TapeMaybe, bw.TapeLoopEnds, bw.ParkedSeen, bw.ParkedMaybe, bw.ParkedBack }) Assert.That(w, Is.Not.Empty);
        Assert.That(t.GoWhy.ContainsKey("police") && t.GoWhy.ContainsKey("lent") && t.GoFate.ContainsKey("stays") && t.GetawayWhy.ContainsKey("police"));
    }
}
