// Engine 0.44.0 (brief 096, Sky 2026-11-05 on paying Inez, option 1: "I think 1 is fine for now"; Motive briefs/096/motive/help-fearful.md):
// Help before the need is heard is a small offer everyone refuses ($0, 35 s, +1 T, a remembered refusal); after the Listen a Fearful
// person takes the full offer once ($40,000). H1-H7 of Motive's page, one scenario each where the engine can show it.
using System.Collections.Generic;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static void ListenToInez(Run r)
        {
            r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez); r.Done(Scout);
            r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
            r.Is(r.G.Inez.KNeed, "the Listen landed: her need is heard");
        }
        /// <summary>The small offer on a person whose need is only told: the card first, then the order, then what the refusal leaves.</summary>
        static void SmallOffer(Run r, string who, Person p, string at)
        {
            var g = r.G;
            r.To(r.Send(Fixer, at));
            var q = g.Preview("help", who);
            r.Eq((q.Outcome, q.Usd, q.WorstUsd, q.S, q.Trail, q.Orders), ("small-offer", 0L, 0L, 35L, 1L, 1L), $"{who}: the card shows the small offer refused before the click: $0, 35 s, +1 T");
            r.Is(q.Lines.TrueForAll(l => (l.Line == "small-offer") == (l.Holds == "yes")), $"{who}: the small-offer line alone holds");
            long cash = g.Cash; int trail = g.Trail;
            r.Do(Fixer, "turn", who, "help"); r.Done(Fixer);
            r.Is(!p.Turned, $"{who} refuses the small offer");
            r.Eq(g.Cash, cash, $"{who}: no money changes hands");
            r.Eq(g.Trail, trail + 1, $"{who}: +1 T");
            r.Is(p.Has("refusal") && g.HoldsFace(p), $"{who} remembers a refusal: the Fixer's face, the place and time");
            r.Is(!p.KnownRefused.Contains("help"), $"{who}: Help is not marked refused (the full offer stays open)");
            r.Is(g.Log.Exists(l => l.Code == "turn.help.small-offer"), "the log line turn.help.small-offer");
            r.Is(g.DebriefLines.Exists(d => d.line == "small-offer" && d.who == who), "the debrief line small-offer");
        }

        static void HelpOffer096()
        {
            Add("096-HELP-1", "096", "H1 (Motive help-fearful.md): Help on Inez before the Listen, her debt only told by the briefing, is a small offer: the card says she refuses it, $0, 35 s, +1 T; given, she refuses, no money moves, she remembers a refusal (the Fixer's face) and Ask cold is closed. A person with no need told stays locked needs-need.", r =>
            {
                var g = r.G;
                r.Is(!g.Inez.KNeed && g.Inez.Suspected == "debt", "her debt is told (the briefing), not heard");
                SmallOffer(r, Inez, g.Inez, "d:Courtyard");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = Inez, Approach = "ask" }).code, "refusal-remembered", "Ask cold is closed by the refusal");
                var q = g.Preview("help", Inez);
                r.Eq(q.Outcome, "small-offer", "a second small offer would be refused the same");
                r.To(T(75)); // the guard on his post from his 1:00 check-in
                var a = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = GuardR, Approach = "help" });
                r.Eq(a.code, "needs-need", "the guard's need is not even told: Help stays locked (Listen first)");
            });

            Add("096-HELP-2", "096", "H2: after the Listen, Help on a Fearful Inez is the full offer: the card $40,000, 35 s, +1 T, one order, turns; given, she turns on the first visit.", r =>
            {
                var g = r.G;
                ListenToInez(r);
                r.To(r.Send(Fixer, "d:Courtyard"));
                var q = g.Preview("help", Inez);
                r.Eq((q.Outcome, q.Usd, q.WorstUsd, q.S, q.Trail, q.Orders), ("turned", 40000L, 40000L, 35L, 1L, 1L), "the full offer: $40,000, 35 s, +1 T, one visit, turns");
                int trail = g.Trail;
                r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                r.Is(g.Inez.Turned, "she turns on the first visit");
                r.Eq((g.Cash, g.Trail), (60000L, trail + 1), "$40,000 and +1 T");
                r.Is(g.Log.Exists(l => l.Code == "turn.help.turned") && !g.Log.Exists(l => l.Code == "turn.help.not-yet"), "turned, never not-yet");
            });

            var lukacTold = new Start(); lukacTold.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            Add("096-HELP-3", "096", "H1 + H7: the small offer first leaves a remembered refusal, so the full offer after the Listen is $50,000 and +2 T (refused-before), and still turns her once; the driver then helped (one visit at $50,000 since engine 0.45.0) leaves $0: the purse fits, exactly.", r =>
            {
                var g = r.G;
                SmallOffer(r, Inez, g.Inez, "d:Courtyard");
                ListenToInez(r);
                r.To(r.Send(Fixer, "d:Courtyard"));
                var q = g.Preview("help", Inez); var rb = q.Lines.Find(l => l.Line == "refused-before")!;
                r.Eq((q.Outcome, q.Usd, q.Trail, rb.Holds), ("turned", 50000L, 2L, "yes"), "the full offer after a refusal: $50,000, +2 T");
                r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                r.Is(g.Inez.Turned, "she turns on the full offer");
                r.Eq((g.Cash, g.Trail), (50000L, 3), "$50,000 left; +1 T for the small offer and +2 T for the full");
                r.To(r.Send(Fixer, "d:Boarding pavilion")); HelpUntilTurned(r, Driver);
                r.Is(g.Driver.Turned, "the driver turned (one visit, one step dearer: engine 0.45.0)");
                r.Eq(g.Cash, 0L, "$0 left: Inez $50,000 + the driver $50,000 fit the $100,000 purse, exactly");
            }, lukacTold);

            var fearfulDriver = new Start(); fearfulDriver.Temperament[Driver] = "fearful";
            Add("096-HELP-4", "096", "H4: the rule is the temperament's, not Inez's: a driver made Fearful by the start, his need (cash) told by the briefing, refuses the small offer the same.", r =>
            {
                var g = r.G;
                r.Is(!g.Driver.KNeed && g.Driver.Suspected == "cash", "his need is told, not heard");
                SmallOffer(r, Driver, g.Driver, "d:Boarding pavilion");
            }, fearfulDriver);

            var fearfulDriverKnown = new Start(); fearfulDriverKnown.Temperament[Driver] = "fearful"; fearfulDriverKnown.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            Add("096-HELP-4b", "096", "H4: a Fearful driver whose need is heard (told whole, as a tip would) takes the full offer once: $40,000, turned.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Boarding pavilion"));
                var q = g.Preview("help", Driver);
                r.Eq((q.Outcome, q.Usd, q.Orders), ("turned", 40000L, 1L), "the full offer, one visit");
                r.Do(Fixer, "turn", Driver, "help"); r.Done(Fixer);
                r.Is(g.Driver.Turned && g.Cash == 60000L, "turned on the first visit, $40,000");
            }, fearfulDriverKnown);

            Add("096-HELP-5", "096", "H5: in a Menace district a Fearful Inez still folds free on Ask, unchanged ($0, +1 T); Help before the Listen is the same small offer, definite on the card though an Idealist or Loyal person there refuses anything.", r =>
            {
                var g = r.G;
                r.Eq(g.R.BandOf(g.Standing), Band.Menace, "a Menace district");
                r.To(r.Send(Fixer, "d:Courtyard"));
                r.Eq(g.Preview("help", Inez).Outcome, "small-offer", "the small offer, definite in Menace too");
                r.Do(Fixer, "turn", Inez, "ask"); r.Done(Fixer);
                r.Is(g.Inez.Turned, "she folds free");
                r.Eq((g.Cash, g.Trail), (100000L, 1), "Ask: $0, +1 T");
            }, StartingAt(-3));

            var menaceKnown = StartingAt(-3); menaceKnown.Known[Inez] = new List<string> { "routine", "need", "temperament" };
            Add("096-HELP-5b", "096", "H2 in Menace: once heard, Help on a Fearful Inez is the full offer once ($40,000), in every band.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard"));
                var q = g.Preview("help", Inez);
                r.Eq((q.Outcome, q.Usd, q.Orders), ("turned", 40000L, 1L), "Menace: the full offer, one visit");
            }, menaceKnown);

            Add("096-HELP-6", "096", "H1's reason: before the Listen the small offer's card is the same for Inez (Fearful) and the driver (Mercenary): it shows no temperament.", r =>
            {
                var g = r.G; r.Wait(1); // a scenario that steps nothing has no checkpoint to replay
                var a = g.Preview("help", Inez); var b = g.Preview("help", Driver);
                r.Eq((a.Outcome, a.Usd, a.S, a.Trail, a.Reports), (b.Outcome, b.Usd, b.S, b.Trail, b.Reports), "one card for both: small-offer, $0, 35 s, +1 T");
                r.Eq(a.Outcome, "small-offer", "the small offer");
            });
        }
    }
}
