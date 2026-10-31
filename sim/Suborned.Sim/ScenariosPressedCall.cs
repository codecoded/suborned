// Engine 0.38.0 (brief 085; Motive briefs/085/motive/pressure-guard.md). What it holds:
//    P85-PRESSED  a pressed Inez's call: he walks over as ever, stays 5 s, runs back at security's speed; the door is clear for less; the Trail is the call's, Alert doesn't move
//    P85-PLAIN    Help and Pay: the visit as it always was
//    P85-MIXED    helped, then pressured: any Pressure counts (N93)
//    P85-LATE     an Enter after he is back fails on the guard, inside what today's window would have been
//    P85-ALARM    an alarm on his walk over still sends him back at a guard's pace, as a recall, not as a run
//    P85-RUN-ALARM  an alarm on his run back doesn't slow him: he keeps running, on the same clock, with no recall
//    P85-PRICE    both Help routes: the worse fit is Inez's and the driver's, $20,000 a visit, $20,000 of the purse left
//    P85-PURSE    Inez helped twice leaves $60,000: the driver's Pay is out of reach
//    P85-LOYAL    a Loyal person pays in full for each visit
//    P85-UNKNOWN  an unread temperament: a "?" line that would lower the price leaves the worst case alone
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static Start Reads(string temperamentOfInez = "")
        {
            var s = new Start();
            s.Known[Inez] = new List<string> { "routine", "need", "temperament" }; s.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            if (temperamentOfInez.Length > 0) s.Temperament[Inez] = temperamentOfInez;
            return s;
        }
        static void PressThenCall(Run r)
        {
            r.To(r.Send(Fixer, "d:Courtyard"));
            r.Do(Fixer, "turn", Inez, "pressure"); r.Done(Fixer);
            r.Is(r.G.Inez.Turned && r.G.Inez.Has("pressure"), "the Pressure turned her");
        }

        static void PressedCallCases()
        {
            Add("P85-PRESSED", "085", "A pressed Inez's call (rulebook summon.pressed): he walks to her as ever, stays 5 s, runs back at security's speed, and the door is clear for less. No Alert and no Trail beyond the call's.", r =>
            {
                var g = r.G; PressThenCall(r);
                var w = g.GuardCallWindows(Inez)!.Value;
                r.Is(w.pressed < w.plain, $"the card's read: {w.pressed} ticks pressed, shorter than the plain {w.plain}");
                long trail = g.Trail; int alert = g.Alert;
                r.Do(Fixer, "call", Inez);
                r.Is(g.GuardCallPressed, "this call is pressed");
                r.Eq(g.GuardAwayUntil - g.LureTick, w.pressed, "the door's window is the card's");
                r.Eq((long)g.Trail, trail + g.R.CallTrail, "the Trail is the call's");
                int walk = g.VisitWalkTicks, lure = g.LureTick, leave = lure + walk + g.R.SummonPressedStayS * 10;
                r.Eq(g.R.SummonPressedStayS, 5, "he stays 5 s"); r.Eq(g.R.SummonPressedSpeed, g.R.SecuritySpeed, "he runs at the response unit's speed");
                r.To(lure + walk - 1); r.Is(!g.GuardRunningBack && g.GuardPose()!.Value.phase == "walking to café", "he walks over");
                r.To(lure + walk); r.Is(LogTick(g, "guard.at-cafe") == lure + walk && !g.GuardRunningBack, "he reaches her at the usual tick");
                r.To(leave); r.Is(LogTick(g, "guard.leaves-cafe") == leave && g.GuardRunningBack, "5 s later he turns back, running");
                r.Eq(g.GuardPose()!.Value.phase, "returning to door", "the phase stays what it was");
                r.Is(g.GuardAwayUntil - leave < walk * g.R.PeopleWalk / g.R.SecuritySpeed + 10, "the way back takes his walk over the run's speed");
                r.Is(!g.GuardAtDoor, "off the door until he's back");
                r.To(g.GuardAwayUntil); r.Is(g.GuardAtDoor && !g.GuardRunningBack && LogTick(g, "guard.back") == g.GuardAwayUntil, "back at his door");
                r.Eq((g.Alert, (long)g.Trail), (alert, trail + g.R.CallTrail), "Alert and the Trail never moved");
                r.Is(!g.Log.Exists(l => l.Code == "guard.recalled"), "no alarm ran him back");
            }, Reads());

            Add("P85-PLAIN", "085", "Helped and paid Inez: an unpressed call is the visit as it always was: he stays summon.stay and walks back at his walk.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard")); HelpUntilTurned(r, Inez);
                var w = g.GuardCallWindows(Inez)!.Value;
                r.Do(Fixer, "call", Inez);
                r.Is(!g.GuardCallPressed, "helped: not pressed");
                r.Eq(g.GuardAwayUntil - g.LureTick, 2 * g.VisitWalkTicks + g.R.SummonStayS * 10, "there and back at his walk, and the stay");
                r.Eq(g.GuardAwayUntil - g.LureTick, w.plain, "the door's window is the card's");
                r.To(g.LureTick + g.VisitWalkTicks + g.R.SummonStayS * 10 + 1); r.Is(!g.GuardRunningBack && g.GuardPose()!.Value.phase == "returning to door", "he walks back");
                r.Is(g.Inez.Has("help") && !g.Inez.Has("pressure"), "the memory it read");
            }, Reads());
            Add("P85-PAID", "085", "Paid Inez: an unpressed call too.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard")); r.Do(Fixer, "turn", Inez, "pay"); r.Done(Fixer); r.Is(g.Inez.Turned, "paid");
                r.Do(Fixer, "call", Inez);
                r.Is(!g.GuardCallPressed, "paid: not pressed");
                r.Eq(g.GuardAwayUntil - g.LureTick, 2 * g.VisitWalkTicks + g.R.SummonStayS * 10, "there and back at his walk, and the stay");
            }, Reads("mercenary"));

            Add("P85-MIXED", "085", "Helped once, then pressured (N93): she remembers the Pressure, so the call is pressed.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard"));
                r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer); r.Is(g.Inez.Has("help") && !g.Inez.Turned, "one Help, not yet turned");
                r.Do(Fixer, "turn", Inez, "pressure"); r.Done(Fixer); r.Is(g.Inez.Turned, "the Pressure turned her");
                var w = g.GuardCallWindows(Inez)!.Value;
                r.Do(Fixer, "call", Inez);
                r.Is(g.GuardCallPressed, "any Pressure counts");
                r.Eq(g.GuardAwayUntil - g.LureTick, w.pressed, "the pressed window");
            }, Reads());

            Add("P85-LATE", "085", "The window is real: an Enter after he is back, with the camera looped and well inside what an unpressed visit would have left, fails on the guard.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Fixer, "d:Courtyard"), r.Send(Tech, "cabinet")));
                r.Do(Fixer, "turn", Inez, "pressure"); r.Done(Fixer);
                var w = g.GuardCallWindows(Inez)!.Value;
                r.Do(Fixer, "call", Inez); int plainEnd = g.LureTick + w.plain;
                r.To(r.Send(Enf, "d:Office entrance"));
                r.To(g.GuardAwayUntil + 5);
                r.Is(g.GuardAtDoor && g.Tick < plainEnd, "he is back, with an unpressed visit still to run");
                r.Do(Tech, "loop", "entrance");
                r.Do(Enf, "enter");
                r.Is(r.Until(() => g.Over, T(60)), "the Enter ends the run");
                r.Eq(g.FailCode, "entry-seen-guard", "seen by the guard, not the camera");
            }, Reads());

            Add("P85-ALARM", "085", "An alarm on his walk over ends a pressed visit as any: guard.recalled, at a guard's pace, not the pressed run.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Enf, BesideDoor)); PressThenCall(r);
                r.Do(Fixer, "call", Inez); int planned = g.GuardAwayUntil;
                r.Wait(T(5));
                r.Fire(Enf, "patrol");
                r.Is(r.Until(() => g.Alert >= 2, T(30)), "gunfire: Alert 2");
                r.Wait(1);
                r.Is(g.Log.Exists(l => l.Code == "guard.recalled"), "the visit ends");
                r.Is(g.GuardCallPressed && !g.GuardRunningBack, "still a pressed call, and no longer its running leg");
                r.Is(g.GuardAwayUntil != planned, "the window was re-timed");
            }, Reads());

            Add("P85-RUN-ALARM", "085", "An alarm on a pressed run back changes nothing about the run (the coordinator's ruling, brief 085): no recall, the same back-at-door tick, GuardRunningBack true throughout.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Enf, BesideDoor)); PressThenCall(r);
                r.Do(Fixer, "call", Inez); int planned = g.GuardAwayUntil, leave = g.LureTick + g.VisitWalkTicks + g.R.SummonPressedStayS * 10;
                r.To(leave + 1); r.Is(g.GuardRunningBack, "he is running back");
                r.Fire(Enf, "patrol");
                r.Is(r.Until(() => g.Alert >= 2, planned - g.Tick - 1), "gunfire: Alert 2 before he is back");
                r.Wait(1);
                r.Is(g.GuardRunningBack && !g.GuardHeld, "still on the running leg after the alarm");
                r.Is(!g.Log.Exists(l => l.Code == "guard.recalled"), "no recall");
                r.Eq(g.GuardAwayUntil, planned, "the same back-at-door tick");
                var p0 = g.GuardPose()!.Value.pos; r.Wait(1); var p1 = g.GuardPose()!.Value.pos; // the fight may still make him draw later (his own sight of it, the old rule); the run's pace is what's held here
                r.Is(G.Dist(p0, p1) > g.R.PeopleWalk / 10 + 2, $"a tick after the alarm he still covers more than a walk ({G.Dist(p0, p1)} cm)");
            }, Reads());

            Add("P85-PRICE", "085", "Both Help routes (Motive §5, checked): Inez (Fearful) and the driver (Mercenary) are each a worse fit on the first visit; on the second the driver's first Help has made him friendly (no longer worse) while Inez stays worse (her own fear keeps her uneasy), and each pays $20,000 either way: $80,000 of the purse, $20,000 left.", r =>
            {
                var g = r.G;
                r.Eq((g.Inez.Temperament, g.Driver.Temperament), ("fearful", "mercenary"), "the cast's Inez and driver");
                void Twice(string who, string at, Person p, string mood, string secondStillWorse, long left)
                {
                    r.To(r.Send(Fixer, at));
                    string Holds(Quote q, string line) => q.Lines.Find(l => l.Line == line)!.Holds;
                    var q1 = g.Preview("help", who);
                    r.Eq((Holds(q1, "one-more-help"), Holds(q1, "visits-share-first"), Holds(q1, "visits-share-later"), q1.Usd, q1.Orders), ("yes", "yes", "no", 20000L, 2L), who + ": first visit, a worse fit, two visits, $20,000");
                    r.Do(Fixer, "turn", who, "help"); r.Done(Fixer);
                    r.Eq(g.MoodOf(who), mood, who + ": the mood after the first Help");
                    var q2 = g.Preview("help", who);
                    r.Eq((Holds(q2, "one-more-help"), Holds(q2, "visits-share-first"), Holds(q2, "visits-share-later"), q2.Usd), (secondStillWorse, "no", "yes", 20000L), who + ": second visit, $20,000 by the later line");
                    r.Do(Fixer, "turn", who, "help"); r.Done(Fixer);
                    r.Is(p.Turned, who + " turned on the second visit");
                    r.Eq(g.Cash, left, $"${left} left");
                }
                Twice(Inez, "d:Courtyard", g.Inez, "uneasy", "yes", 60000L);
                Twice(Driver, "d:Boarding pavilion", g.Driver, "friendly", "no", 20000L);
            }, Reads());

            Add("P85-PURSE", "085", "§04's purse: Inez helped twice leaves $60,000, so the driver's Pay ($70,000) is refused for money.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard")); HelpUntilTurned(r, Inez);
                r.Eq(g.Cash, 60000L, "$40,000 over two visits");
                r.To(r.Send(Fixer, "d:Boarding pavilion"));
                r.Refused(Fixer, "turn", Driver, "$70,000 is over what's left", "pay");
            }, Reads());

            Add("P85-LOYAL", "085", "A Loyal person's Help suits them and repeating it is their whole road: the visits' share never applies (Motive §5); her second visit is the mood's ready-at-one, as before.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard"));
                string Share() { var q = g.Preview("help", Inez); return q.Lines.Find(l => l.Line == "visits-share-first")!.Holds + q.Lines.Find(l => l.Line == "visits-share-later")!.Holds; }
                r.Eq(Share(), "nono", "no share on the first visit"); r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                r.Eq(g.Cash, 60000L, "$40,000 in full"); r.Eq(Share(), "nono", "no share on the second visit"); r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                r.Is(g.Inez.Turned, "turned on the second visit");
                r.Eq(g.Cash, 60000L - 40000L - (long)g.R.Num("v:ready-help-one-usd"), "the second at the base and the ready line, no share");
            }, Reads("loyal"));

            var unread = new Start(); unread.Known[Inez] = new List<string> { "routine", "need" };
            Add("P85-UNKNOWN", "085", "Temperament unread: the visits' share is a \"?\" on the card, and the worst case the purse is held to stays the base price.", r =>
            {
                var g = r.G;
                var q = g.Preview("help", Inez);
                r.Eq(q.Lines.Find(l => l.Line == "visits-share-first")!.Holds, "?", "the share is a \"?\"");
                r.Eq(q.WorstUsd, 40000L, "the worst case stays $40,000"); r.Wait(1);
            }, unread);
        }
    }
}
