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
