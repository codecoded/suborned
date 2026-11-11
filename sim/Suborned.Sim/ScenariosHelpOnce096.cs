// Engine 0.45.0 (brief 096, Sky 2026-11-05 "As you recommend" on Motive's Q1 (b), briefs/096/motive/help-fearful.md; Sky on paying Inez:
// "makes no sense to make a second payment... player is just standing there immediately doing the same action again"): a worse Help on a
// Mercenary or an Idealist is the full offer once, one step dearer (turn.help worse-once, +$10,000): the driver $50,000 in one visit after
// his Listen. Before the Listen the small offer is still refused (0.44.0); a Loyal person's repeated Help stays (Q2 (a)).
using System.Collections.Generic;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The Scout's Listen at the car (S20's way: no Watch to give on the driver).</summary>
        static void ListenToDriver(Run r)
        {
            r.To(r.Send(Scout, "d:Meet driver"));
            r.Do(Scout, "listen", Driver); r.Done(Scout);
            r.Is(r.G.Driver.KNeed && r.G.Driver.KTemperament, "the Listen landed: his need and temperament are heard");
        }
        static string HoldsOn(Quote q, string line) => q.Lines.Find(l => l.Line == line)?.Holds ?? "absent";

        static void HelpOnce096()
        {
            Add("096-HELP-7", "096", "Q1 (b): after his Listen, Help on the driver (Mercenary, Help his worse approach) is the full offer once, one step dearer: the card $50,000, 35 s, +1 T, one order, turns (worse-once holds, the two-visit lines don't); given, he turns on the first visit and no not-yet is logged.", r =>
            {
                var g = r.G;
                r.Eq(g.Driver.Temperament, "mercenary", "the cast's driver is a Mercenary");
                ListenToDriver(r);
                r.To(r.Send(Fixer, "d:Boarding pavilion"));
                var q = g.Preview("help", Driver);
                r.Eq((q.Outcome, q.Usd, q.WorstUsd, q.S, q.Trail, q.Orders), ("turned", 50000L, 50000L, 35L, 1L, 1L), "the full offer once: $50,000, 35 s, +1 T, one visit, turns");
                r.Eq((HoldsOn(q, "worse-once"), HoldsOn(q, "one-more-help"), HoldsOn(q, "visits-share-first"), HoldsOn(q, "visits-share-later")), ("yes", "no", "no", "absent"), "worse-once holds; the two-visit lines don't (visits-share-later is gone)");
                int trail = g.Trail;
                r.Do(Fixer, "turn", Driver, "help"); r.Done(Fixer);
                r.Is(g.Driver.Turned, "he turns on the first visit");
                r.Eq((g.Cash, g.Trail), (50000L, trail + 1), "$50,000 and +1 T");
                r.Is(g.Log.Exists(l => l.Code == "turn.help.turned") && !g.Log.Exists(l => l.Code == "turn.help.not-yet"), "turned, never not-yet");
            });

            Add("096-HELP-8", "096", "Q1 (b) before the Listen: the driver's need is only told (the briefing's \"Cash\"), so Help is still the small offer he refuses (0.44.0); after it and his Listen the full offer is $60,000 and +2 T (refused-before + worse-once), still one visit.", r =>
            {
                var g = r.G;
                SmallOffer(r, Driver, g.Driver, "d:Boarding pavilion");
                ListenToDriver(r);
                r.To(r.Send(Fixer, "d:Boarding pavilion"));
                var q = g.Preview("help", Driver);
                r.Eq((q.Outcome, q.Usd, q.Trail, q.Orders, HoldsOn(q, "refused-before"), HoldsOn(q, "worse-once")), ("turned", 60000L, 2L, 1L, "yes", "yes"), "after a refusal: $60,000, +2 T, one visit");
                r.Do(Fixer, "turn", Driver, "help"); r.Done(Fixer);
                r.Is(g.Driver.Turned && g.Cash == 40000L, "turned on the one visit, $40,000 left");
            });

            Add("096-HELP-9", "096", "The purse (Motive H7 with Q1 (b)): Help on Inez $40,000 + Help on the driver $50,000 = $90,000 fits the $100,000 purse, $10,000 left. Brief 101: the Tech loops the square's mast while the cell works at Inez's post.", r =>
            {
                var g = r.G;
                r.Send(Tech, "cabinet"); ListenToInez(r, loopMast: true);
                r.To(r.Send(Fixer, "d:Courtyard")); r.Do(Fixer, "turn", Inez, "help"); ListenToDriver(r); r.Done(Fixer); // engine 0.55.0: the Scout walks to the car while the Fixer helps Inez (a Listen holds her no longer, so the Help walks in to her round), as 9b
                r.To(r.Send(Fixer, "d:Boarding pavilion")); r.Do(Fixer, "turn", Driver, "help"); r.Done(Fixer);
                r.Is(g.Inez.Turned && g.Driver.Turned, "both turned, one visit each");
                r.Eq(g.Cash, 10000L, "$90,000 spent, $10,000 left");
            });

            Add("096-HELP-9b", "096", "The purse, tight: a refused small offer on Inez first makes her Help $50,000, and the driver's $50,000 takes the purse to $0, exactly; it still fits. Brief 101: the Tech loops the square's mast while the cell works at Inez's post.", r =>
            {
                var g = r.G;
                r.Send(Tech, "cabinet"); SmallOffer(r, Inez, g.Inez, "d:Courtyard");
                ListenToInez(r, loopMast: true);
                r.Do(Fixer, "turn", Inez, "help"); ListenToDriver(r); r.Done(Fixer); // the Scout walks to the car while the Fixer helps Inez
                r.Eq(g.Cash, 50000L, "Inez at $50,000 after her refusal");
                r.To(r.Send(Fixer, "d:Boarding pavilion")); r.Do(Fixer, "turn", Driver, "help"); r.Done(Fixer);
                r.Is(g.Inez.Turned && g.Driver.Turned, "both turned");
                r.Eq(g.Cash, 0L, "$100,000 spent: the purse, exactly");
            });

            Add("096-HELP-9c", "096", "The purse, over: small offers refused on both before their Listens make Inez $50,000 and the driver $60,000; $110,000 doesn't fit, so his Help is refused for money (cash-covers) and nothing is charged.", r =>
            {
                var g = r.G;
                SmallOffer(r, Inez, g.Inez, "d:Courtyard");
                r.Send(Fixer, "d:Boarding pavilion"); ListenToInez(r); // the Scout listens to Inez while the Fixer walks to the car
                SmallOffer(r, Driver, g.Driver, "d:Boarding pavilion");
                r.To(r.Send(Fixer, "d:Courtyard")); r.Do(Fixer, "turn", Inez, "help"); ListenToDriver(r); r.Done(Fixer);
                r.Is(g.Inez.Turned && g.Cash == 50000L, "Inez turned at $50,000");
                r.To(r.Send(Fixer, "d:Boarding pavilion"));
                r.Eq(g.Preview("help", Driver).Usd, 60000L, "his card asks $60,000");
                r.Refused(Fixer, "turn", Driver, "$50,000 left, $60,000 asked", "help");
                r.Is(!g.Driver.Turned && g.Cash == 50000L, "not turned, nothing charged");
            });

            var loyalDriver = new Start(); loyalDriver.Temperament[Driver] = "loyal"; loyalDriver.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            Add("096-HELP-10", "096", "Q2 (a): a Loyal person's repeated Help stays: a driver made Loyal by the start takes two Helps (trust over time), at $40,000 each, worse-once never holds.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Boarding pavilion"));
                var q = g.Preview("help", Driver);
                r.Eq((q.Usd, q.Orders, HoldsOn(q, "trust-over-time"), HoldsOn(q, "worse-once")), (40000L, 2L, "yes", "no"), "two Helps at $40,000, trust over time");
                r.Do(Fixer, "turn", Driver, "help"); r.Done(Fixer);
                r.Is(!g.Driver.Turned && g.Log.Exists(l => l.Code == "turn.help.not-yet"), "not yet after the first");
            }, loyalDriver);
        }
    }
}
