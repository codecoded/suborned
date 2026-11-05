using System.Collections.Generic;
using NUnit.Framework;

namespace Suborned.Play.Tests
{
    /// <summary>Brief 030 done-check 1: every target kind in the table gives the stated default (a unit test over the ranking, no engine step),
    /// and a quick click never gambles, never takes a violent holstered way, never fires on a civilian. Rows are what the panel builds for the
    /// target (ContextRank.Way: verb, approach, outcome, price, lock); Fire on a hostile and a drawn Fire act on the press before the ranking.</summary>
    public sealed class ContextRankTests
    {
        static ContextRank.Way W(string verb, string approach = "", string outcome = "", bool ok = true, bool mine = true, long usd = 0, long trail = 0, long orders = 1, string how = "")
            => new() { Do = verb, Approach = approach, Outcome = outcome, Available = ok, Mine = mine, Usd = usd, Trail = trail, Orders = orders, How = how };

        static ContextRank.Choice C(string kind, params ContextRank.Way[] ws)
        {
            var l = new List<ContextRank.Way>(ws); for (int i = 0; i < l.Count; i++) l[i].Index = i;
            return ContextRank.Choose(l, kind);
        }

        static string Picked(ContextRank.Choice c, params ContextRank.Way[] ws) => c.Act == ContextRank.Act.Default ? ws[c.Pick].Do + (ws[c.Pick].Approach.Length > 0 ? ":" + ws[c.Pick].Approach : "") : c.Act.ToString();

        [Test] public void ScoutOnAPersonWatchesThenListens()
        {
            var w = new[] { W("watch"), W("listen") }; Assert.AreEqual("watch", Picked(C("person", w), w));
            var l = new[] { W("watch", ok: false), W("listen") }; Assert.AreEqual("listen", Picked(C("person", l), l), "a finished Watch drops out: the next sensible step");
        }

        [Test] public void ARivalOnFootListensFirstPaysAGambleOpensTheRingAndTheMisleadIsNextAfterTheListen()
        {   // brief 096 (engine 0.41.0 rivals.turn): a member of the crew on foot: Listen, Mislead, Pay ($30,000, "?" until a Listen), the takedowns and Fire
            var all = new[] { W("listen"), W("mislead", outcome: "misled", trail: 1), W("turn", "pay", "?", mine: false, usd: 30000, trail: 2), W("takedown", how: "knockout", mine: false), W("fire", outcome: "loud") };
            var c = C("rivals", all); Assert.AreEqual("listen", Picked(c, all), "a free learn first"); Assert.AreEqual(new[] { 0, 2, 1, 3, 4 }, c.Order.ToArray(), "Listen, Pay, Mislead, Knock out, Fire");
            var heard = new[] { W("listen", ok: false), W("mislead", outcome: "misled", trail: 1) }; Assert.AreEqual("mislead", Picked(C("rivals", heard), heard), "heard: the Scout's word is the click");
            var gamble = C("rivals", W("turn", "pay", "?", usd: 30000, trail: 2), W("fire", outcome: "loud")); Assert.AreEqual(ContextRank.Act.Ring, gamble.Act); Assert.AreEqual("gamble", gamble.Why);
            var merc = new[] { W("turn", "pay", "turned", usd: 30000, trail: 2), W("fire", outcome: "loud") }; Assert.AreEqual("turn:pay", Picked(C("rivals", merc), merc), "a known Mercenary: the Pay is the click");
            var loyal = C("rivals", W("turn", "pay", "refused", usd: 30000, trail: 2), W("fire", outcome: "loud")); Assert.AreEqual(ContextRank.Act.Ring, loyal.Act, "a known Loyal man: never paid by a click");
        }

        [Test] public void FixerWithTheTemperamentUnknownOpensTheRing()
        {
            var c = C("person", W("turn", "ask", "?", usd: 0), W("turn", "pay", "?", usd: 100), W("turn", "help", "?"), W("turn", "pressure", "?"));
            Assert.AreEqual(ContextRank.Act.Ring, c.Act); Assert.AreEqual("gamble", c.Why);
        }

        [Test] public void FixerWithTheTemperamentKnownTakesTheFittingWayNeverPressure()
        {
            var w = new[] { W("turn", "ask", "turned", orders: 2), W("turn", "pay", "refused", usd: 500), W("turn", "help", "turned", usd: 50), W("turn", "pressure", "turned") };
            Assert.AreEqual("turn:help", Picked(C("person", w), w));
            var p = new[] { W("turn", "pressure", "turned"), W("turn", "pay", "refused") };
            Assert.AreEqual(ContextRank.Act.Ring, C("person", p).Act, "Pressure is never a default, a refused way never taken");
            var cheap = new[] { W("turn", "pay", "turned", usd: 500), W("turn", "help", "turned", usd: 100) };
            Assert.AreEqual("turn:help", Picked(C("person", cheap), cheap), "the cheapest of two that fit");
        }

        [Test] public void FixerOnATurnedContactCalls() { var w = new[] { W("call") }; Assert.AreEqual("call", Picked(C("person", w), w)); }

        [Test] public void EnforcerOnTheGuardOpensTheRing()
        {
            var c = C("person", W("takedown", how: "knockout"), W("takedown", how: "kill"), W("fire"));
            Assert.AreEqual(ContextRank.Act.Ring, c.Act); Assert.AreEqual("violent", c.Why);
        }

        [Test] public void TakeVale() { var w = new[] { W("take"), W("takedown", how: "kill") }; Assert.AreEqual("take", Picked(C("person", w), w)); }
        [Test] public void ValeForAnotherAgentIsTheLockPop() => Assert.AreEqual(ContextRank.Act.Lock, C("person", W("take", ok: false)).Act);
        [Test] public void APasserByIsSilent() => Assert.AreEqual(ContextRank.Act.Silent, C("crowd", W("takedown", how: "knockout"), W("takedown", how: "kill")).Act);
        [Test] public void AUnitIsSilentUnlessFired() => Assert.AreEqual(ContextRank.Act.Silent, C("unit").Act);
        [Test] public void DoorEnters() { var w = new[] { W("enter") }; Assert.AreEqual("enter", Picked(C("door", w), w)); }

        [Test] public void CabinetLoopAndRerouteOpenTheRingThenRerouteAlone()
        {
            Assert.AreEqual(ContextRank.Act.Ring, C("cabinet", W("loop"), W("reroute")).Act, "both open at once: the ring");
            var w = new[] { W("loop", ok: false), W("reroute") }; Assert.AreEqual("reroute", Picked(C("cabinet", w), w));
        }

        [Test] public void CameraWatchesUntilLearnedThenLoops()
        {
            var a = new[] { W("watch"), W("loop") }; Assert.AreEqual("watch", Picked(C("camera", a), a));
            var b = new[] { W("watch", ok: false), W("loop") }; Assert.AreEqual("loop", Picked(C("camera", b), b));
        }

        [Test] public void OnAStandDriveIsTheQuickClick()
        {
            var w = new[] { W("drive-car") }; Assert.AreEqual("drive-car", Picked(C("stand", w), w), "brief 059 K6: a stand's one way is Drive");
            var l = new[] { W("drive-car", ok: false) }; Assert.AreEqual(ContextRank.Act.Lock, C("stand", l).Act, "a refused Drive (held, busy) says its lock");
        }

        [Test] public void RivalsTakeTheFalseTipAtTheirCar()
        {
            var w = new[] { W("watch"), W("mislead"), W("fire") }; Assert.AreEqual("watch", Picked(C("rivals", w), w), "brief 059 §2.2: a free learn first (the timetable)");
            var t = new[] { W("watch", ok: false), W("mislead"), W("fire") }; Assert.AreEqual("mislead", Picked(C("rivals", t), t), "then the tip; never Fire as a click");
        }

        [Test] public void OnACameraLoopLeadsTheRingAndWatchStandsAtThree()
        {
            var t = new[] { W("watch"), W("loop"), W("disable"), W("fire") }; var c = C("camera", t);
            Assert.AreEqual(new List<int> { 1, 0, 2, 3 }, c.Order, "the Tech: Loop 12, Watch 3, Disable 6, Fire 9");
            Assert.AreEqual("watch", Picked(c, t), "the quick click still watches first");
            var s = new[] { W("watch"), W("fire") }; Assert.AreEqual(new List<int> { 0, 1 }, C("camera", s).Order, "the Scout: Watch keeps 12");
            var p = new[] { W("watch"), W("loop") }; Assert.AreEqual(new List<int> { 0, 1 }, C("person", p).Order, "only a camera's ring moves Loop");
        }

        [Test] public void PatrolWatchesThenReroutesAndTheEnforcerAloneGetsTheRing()
        {
            var a = new[] { W("watch"), W("reroute") }; Assert.AreEqual("watch", Picked(C("patrol", a), a));
            var b = new[] { W("watch", ok: false), W("reroute") }; Assert.AreEqual("reroute", Picked(C("patrol", b), b));
            Assert.AreEqual(ContextRank.Act.Ring, C("patrol", W("fire")).Act, "holstered Fire on a non-hostile only through the ring");
        }

        [Test] public void RivalsTakeTheFalseTip() { var w = new[] { W("watch", ok: false), W("mislead") }; Assert.AreEqual("mislead", Picked(C("rivals", w), w)); }

        [Test] public void TheGetawayBoardsThenGoes()
        {
            var a = new[] { W("board") }; Assert.AreEqual("board", Picked(C("car", a), a));
            var b = new[] { W("go") }; Assert.AreEqual("go", Picked(C("car", b), b));
        }

        /// <summary>Brief 067 C1.5 (Glyph): on the car Go leads only once Vale is aboard (its outcome "win"); Board before Drive (the two no longer
        /// tie, so the hold has one way); Go before Vale (a withdraw) is last of the car's ways.</summary>
        [Test] public void OnTheCarGoLeadsOnlyWithValeAboardAndTheWithdrawIsLast()
        {
            var won = new[] { W("board"), W("drive-car"), W("go", outcome: "win") };
            var cw = C("car", won); Assert.AreEqual(2, cw.Order[0], "Vale aboard: Go first"); Assert.AreEqual(0, cw.Order[1]); Assert.AreEqual(1, cw.Order[2]);
            var out0 = new[] { W("go", outcome: "withdraw"), W("drive-car"), W("board") };
            var co = C("car", out0); Assert.AreEqual(new List<int> { 2, 1, 0 }, co.Order, "Vale not aboard: Board, Drive, then the withdraw last");
            var bd = new[] { W("drive-car"), W("board") }; Assert.AreEqual("board", Picked(C("car", bd), bd), "Board and Drive no longer tie: Board is the quick way while someone is outside");
        }

        /// <summary>Sky 2026-11-04 (a tester boarded without Popovic and couldn't get out): Step out is on the car's ring, after Board and Drive and
        /// before the withdraw; once Popovic is aboard Go still leads.</summary>
        [Test] public void OnTheCarStepOutComesBeforeTheWithdraw()
        {
            var step = W("move", how: HandoverHost.StepOutHow);
            var a = new[] { W("go", outcome: "withdraw"), step, W("drive-car"), W("board") };
            Assert.AreEqual(new List<int> { 3, 2, 1, 0 }, C("car", a).Order, "Board, Drive, Step out, then the withdraw last");
            var b = new[] { step, W("go", outcome: "win") };
            Assert.AreEqual(1, C("car", b).Order[0], "Popovic aboard: Go first, Step out after");
            var c = new[] { W("move") };
            Assert.AreEqual(8, ContextRank.Tier(c[0]), "a plain move keeps its tier");
        }

        [Test] public void ABodyIsCarriedNeverHidden()
        {
            var a = new[] { W("carry"), W("restrain") }; Assert.AreEqual("carry", Picked(C("body", a), a));
            var b = new[] { W("drop") }; Assert.AreEqual("drop", Picked(C("body", b), b), "carrying it: Put down");
        }

        [Test] public void AStashHidesOnlyWhatIsCarried()
        {
            var a = new[] { W("stash") }; Assert.AreEqual("stash", Picked(C("stash", a), a));
            Assert.AreEqual(ContextRank.Act.Silent, C("stash").Act);
        }

        [Test] public void NothingForTheSelectionIsTheHint() => Assert.AreEqual(ContextRank.Act.Hint, C("person").Act);

        [Test] public void ThePrimaryAgentsWayWins()
        {
            var w = new[] { W("watch", mine: false), W("enter", mine: true) };
            Assert.AreEqual("enter", Picked(C("door", w), w)); // tier 1 anyway; and with equal tiers the primary's first
            var x = new[] { W("loop", mine: false), W("reroute", mine: true) };
            Assert.AreEqual("reroute", Picked(C("cabinet", x), x), "the default goes to the primary selected agent");
        }

        [Test] public void TheRingIsInRankOrder()
        {
            var c = C("person", W("fire"), W("turn", "pressure", "?"), W("watch"), W("takedown", how: "kill"), W("turn", "ask", "?"), W("take", ok: false));
            Assert.AreEqual(new List<int> { 2, 4, 1, 3, 0 }, c.Order, "watch, ask, pressure, kill, fire; the locked Take is not a wedge");
        }

        [Test] public void NoQuickClickIsEverViolent()
        {
            string[] verbs = { "watch", "listen", "turn", "loop", "reroute", "mislead", "take", "enter", "board", "go", "call", "carry", "stash", "restrain", "drop", "takedown", "fire" };
            foreach (var a in verbs) foreach (var b in verbs)
            {
                var c = C("person", W(a, a == "turn" ? "help" : ""), W(b, b == "turn" ? "pressure" : "", b == "turn" ? "turned" : ""));
                if (c.Act == ContextRank.Act.Default) { var v = new[] { a, b }[c.Pick]; Assert.IsFalse(v is "takedown" or "fire", $"{a}+{b} took {v}"); }
            }
        }

        [Test] public void PricesParse()
        {
            Assert.AreEqual(1000, ContextRank.Num("$1,000–2,000")); Assert.AreEqual(2, ContextRank.Num("T +2")); Assert.AreEqual(0, ContextRank.Num("A ?"));
        }
    }
}
