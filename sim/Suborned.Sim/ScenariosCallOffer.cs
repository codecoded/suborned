// Brief 102 (Sky 2026-11-09; Motive briefs/102/motive/turned-guard.md §1.1-1.4, §2.2; engine 0.51.0, GameCall.cs): the Call's offer, the one
// called's own state (step 4) and the visit from either end (option 1). What it holds:
//    C102-OFFER          offered where the call has a use (Inez, the turned guard); absent where it has none (the turned driver), with Issue's code
//    C102-UNTIED         an untied contact's Call is absent at both ends, and so is the Call on someone not turned
//    C102-SAW-TAKEDOWN   Inez saw a takedown, then is called: call-saw-violence at no cost; the guard stays at his door; the Call stays offered, shut
//    C102-SAW-KILLED     Inez saw her lover shot down at her side, then is called: call-saw-violence, before his state (he's down, he came once)
//    C102-UNSEEN         the guard knocked out out of her sight: she tries, and nobody comes (summon-busy), never call-saw-violence
//    C102-GUARD-SAW      the turned guard saw a weapon drawn, then is called: call-saw-violence, before his own busy and the alarm
//    C102-GUARD-CALL     the turned guard is called: he walks to Inez's post, stays a minute and walks back, the window hers would give
//    C102-ONCE           once a shift across both ends: his call, then hers (busy while he's away, came once after) and his again
//    C102-ONCE-B         the other way round: her call, then his
//    C102-PRESSED-GUARD  the guard pressured, then called himself: the short visit
//    C102-PRESSED-INEZ   Inez pressured, the guard called: the short visit (her Pressure, read from his end)
//    C102-PRESSED-EITHER the guard pressured, Inez helped and called: the short visit (his Pressure, read from her end)
// A told entry (not a sighting) is the xunit test AToldEntryIsNoMemoryOfViolence (Tests.cs): nothing in the Handover tells Inez in play, and a
// scenario replays from its start and orders only.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static (bool ok, string code) CallNow(Game g, string role) { var a = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "call", Target = role }); return (a.ok, a.code); }
        /// <summary>The call's refusal now, as the card and the order give it: the query first, then Issue; both must agree, and nothing is charged.</summary>
        static void RefusedCall(Run r, string role, string code, string what)
        {
            var g = r.G; long trail = g.Trail; var q = g.CallQuery(role);
            var (ok, got) = CallNow(g, role);
            r.Eq((ok, got), (false, code), what);
            r.Eq((q.ok, q.code), (ok, got), $"{role}: the card said so before the click");
            r.Eq((long)g.Trail, trail, $"{role}: refused at no cost");
        }
        static Start KnowsGuard(Start s) { s.Known[GuardR] = new List<string> { "routine", "need", "temperament", "secret" }; return s; } // the affair known: Pressure on him has its leverage
        static void PressGuard(Run r)
        {
            r.Do(Fixer, "turn", GuardR, "pressure"); r.Done(Fixer);
            r.Is(r.G.GuardP.Turned && r.G.GuardP.Has("pressure"), "the Pressure turned the guard");
            r.Is(r.G.Alert <= r.G.R.SummonAlertMax && r.G.GuardAtDoor, $"and left the door quiet: Alert {r.G.Alert}, he is at his post");
        }

        static void CallOfferCases()
        {
            Add("C102-OFFER", "102", "The offer law (Motive §1.1, Aster's (b)): a Call is offered on a turned contact only where it has a use under the rules. Inez's and the turned guard's are offered (option 1: a visit from either end); the turned driver's is absent (his camera block is retired), and an order given anyway gets nothing-to-call.", r =>
            {
                var g = r.G;
                r.Is(g.CallOffered(Inez) && g.CallOffered(GuardR), "offered: Inez and the turned guard");
                r.Is(!g.CallOffered(Driver), "absent: the turned driver");
                r.Is(!g.CallOffered(Vale) && !g.CallOffered("p-cafe-05"), "absent: Popovic and a crowd person, who are no contacts");
                r.Is(g.CallQuery(Inez).ok && g.CallQuery(GuardR).ok && g.CanCall(Inez) && g.CanCall(GuardR), "both ways are open now");
                r.Eq(g.CallQuery(Driver).code, "nothing-to-call", "the driver's card: nothing to call");
                RefusedCall(r, Driver, "nothing-to-call", "the driver's order, given anyway: nothing-to-call");
                r.Wait(1);
            }, Turned(Inez, GuardR, Driver));

            var untied = Turned(Inez, GuardR); untied.Untie.Add(Inez);
            Add("C102-UNTIED", "102", "An untied contact's Call has no use (no lover: absent at both ends of the cut tie), and nobody untouched has a Call at all.", r =>
            {
                var g = r.G;
                r.Is(g.Inez.Lover == null && g.GuardP.Lover == null, "the tie is cut at both ends (a test's start state)");
                r.Is(!g.CallOffered(Inez) && !g.CallOffered(GuardR), "absent on both");
                RefusedCall(r, Inez, "nothing-to-call", "Inez's order, given anyway: nothing-to-call");
                RefusedCall(r, GuardR, "nothing-to-call", "the guard's too");
                r.Is(!g.CallOffered(Driver) && g.CallQuery(Driver).code == "not-a-contact", "the driver, not turned: no Call, and the card says not-a-contact");
                r.Wait(1); r.Is(g.GuardAtDoor && g.LureTick < 0, "nobody left a post");
            }, untied);

            Add("C102-SAW-TAKEDOWN", "102", "Step 4 (Sky's \"yes, they refuse\"): Inez sees the Enforcer knock a customer out at the terrace (N48's), then is called. She won't act on it: call-saw-violence, at no cost, while nothing else would stop the call. The Call stays offered, shut (what happened shuts it).", r =>
            {
                var g = r.G;
                const string Victim = "p-pylon-row-east-05"; // N48's Mercenary customer at terrace table 2 from 0:30, in front of the café door
                r.To(T(240));
                var seat = g.CrowdPos(WalkerById(r, Victim)) ?? throw new InvalidOperationException($"{Victim} is not on the street at 4:00");
                var spot = Beside(r, seat) ?? throw new InvalidOperationException("no ground beside the terrace table");
                r.To(r.SendTo(Enf, spot));
                r.Takedown(Victim, "knockout"); r.Done(Enf);
                r.Is(g.Inez.Has("saw-takedown") && g.Inez.Turned, "Inez saw it, and is still turned");
                r.Is(g.Alert <= g.R.SummonAlertMax && g.GuardAtDoor && g.GuardP.Summoned == 0, $"nothing else would stop the call: Alert {g.Alert}, the guard at his door, no visit yet");
                RefusedCall(r, Inez, "call-saw-violence", "she won't make the call");
                r.Is(g.CallOffered(Inez), "the Call stays offered, shut");
                r.Wait(1); r.Is(g.GuardAtDoor && g.LureTick < 0, "the guard stays at his door");
            }, Turned(Inez));

            Add("C102-SAW-KILLED", "102", "Step 4: Inez calls the guard over, and the Enforcer shoots him down at her side (no takedown can take him there: away from his door he senses anyone within reach). Called again, her own memory is the reason that shows (call-saw-violence), before his state: he's down and he came once.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Courtyard")); // near the café, off her post: the guard's stop at her side stays free (PartnerStop reads who stands near it)
                r.Do(Fixer, "call", Inez);
                r.To(g.LureTick + g.VisitWalkTicks + 1);
                r.Eq(g.GuardPose()!.Value.phase, "at café", "he is with her");
                r.Fire(Enf, GuardR);
                r.Is(r.Until(() => g.GuardDown, T(30)), "the guard is down");
                r.Is(g.Inez.Has("saw-gunfire") && g.InezWitnessedShooting, "she saw him shot");
                RefusedCall(r, Inez, "call-saw-violence", "her memory shows, not his state (summon-busy) or his one visit (summon-came-once)");
                r.Is(g.CallOffered(Inez), "her Call stays offered, shut");
            }, Turned(Inez));

            Add("C102-UNSEEN", "102", "Harm she didn't see (Motive §1.4): the guard knocked out at his door far from the café (N46's quiet takedown). Inez holds no memory of it, so she tries the call and nobody comes: summon-busy (he's down), never call-saw-violence.", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                r.Takedown("guard", "knockout"); r.Done(Enf);
                r.Is(g.GuardDown, "he is down");
                r.Is(!g.Inez.Has("saw-takedown") && !g.Inez.Has("saw-violence") && !g.Inez.Has("saw-gunfire"), "she saw nothing");
                RefusedCall(r, Inez, "summon-busy", "she tries; he can't come");
            }, Turned(Inez));

            Add("C102-GUARD-SAW", "102", "Step 4 from his end (option 1): the turned guard sees the Enforcer draw at his door, then is called. His own memory is the reason (call-saw-violence), before his state (engaged) and the alarm.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Enf, FrontOfGuard(r)));
                r.Do(Enf, "draw");
                r.Is(r.Until(() => g.GuardP.Has("saw-weapon-drawn"), T(10)), "he sees the weapon drawn");
                RefusedCall(r, GuardR, "call-saw-violence", "he won't act on the call");
                r.Is(g.CallOffered(GuardR), "his Call stays offered, shut");
            }, Turned(GuardR));

            Add("C102-GUARD-CALL", "102", "Option 1 (Sky's \"leaves when called\"): the turned guard is called. He leaves his door, walks to Inez's post, stays a minute and walks back: the window her call gives, at the call's Trail. Inez needn't be turned.", r =>
            {
                var g = r.G;
                r.Is(g.CallOffered(GuardR) && !g.CallOffered(Inez), "his Call is offered; Inez isn't turned, so hers isn't");
                r.Eq((g.CallLeaver(GuardR), g.CallLeaver(Inez), g.CallLeaver(Driver)), (GuardR, GuardR, ""), "the one who'd leave a post: the guard, from either end; nobody for the driver");
                var w = g.GuardCallWindows(GuardR); var wi = g.GuardCallWindows(Inez);
                r.Is(w.HasValue && wi.HasValue && w.Value.plain == wi.Value.plain && w.Value.pressed == wi.Value.pressed, "the card's window is the same from either end");
                long trail = g.Trail;
                r.Do(Fixer, "call", GuardR);
                int lure = g.LureTick, walk = g.VisitWalkTicks, stay = T(g.R.SummonStayS);
                r.Eq(lure, g.Tick, "he takes the call at once");
                r.Eq((long)g.Trail, trail + g.R.CallTrail, "the call's Trail");
                r.Is(!g.GuardCallPressed, "nobody pressured: the plain visit");
                r.Eq(g.GuardAwayUntil - lure, w!.Value.plain, "the door's window is the card's");
                r.Eq(g.GuardAwayUntil - lure, 2 * walk + stay, "there and back at his walk, and a minute's stay");
                r.Is(g.LureCalled == GuardR && g.Chain.Contains("guard-goes-to-waitress") && g.GuardP.Has("job") && g.Log.Exists(l => l.Code == "call.guard"), "his own call: logged, chained and remembered as his");
                r.Wait(1); r.Is(!g.GuardAtDoor && g.GuardPose()!.Value.phase == "walking to café", "he leaves his door and walks to her");
                r.To(lure + walk);
                r.Eq(g.GuardPose()!.Value.phase, "at café", "he reaches her post");
                r.Is(G.Within(g.PersonPos(g.Inez)!.Value, g.GuardPose()!.Value.pos, 400), "they stand together at her post");
                r.To(lure + walk + stay - 1); r.Eq(g.GuardPose()!.Value.phase, "at café", "he stays the minute");
                r.To(lure + walk + stay + 1); r.Eq(g.GuardPose()!.Value.phase, "returning to door", "then walks back");
                r.To(g.GuardAwayUntil); r.Is(g.GuardAtDoor && LogTick(g, "guard.back") == g.GuardAwayUntil, "back at his door when the window closes");
                r.Eq(g.GuardP.Summoned, 1, "counted on him, the one who left");
            }, Turned(GuardR));

            Add("C102-ONCE", "102", "Once a shift, whichever of them is called (option 1): the guard's own call, then Inez's finds him away (summon-busy); back at his door, hers and his are came-once. Both Calls stay offered, shut.", r =>
            {
                var g = r.G;
                r.Do(Fixer, "call", GuardR);
                RefusedCall(r, Inez, "summon-busy", "while he's away, her call finds him gone");
                r.To(g.GuardAwayUntil + 1);
                RefusedCall(r, Inez, "summon-came-once", "back at his post: her call, after his own visit");
                RefusedCall(r, GuardR, "summon-came-once", "and his own call again");
                r.Is(g.CallOffered(Inez) && g.CallOffered(GuardR) && !g.CanCall(Inez) && !g.CanCall(GuardR), "both stay offered, and the cards say neither can be given");
            }, Turned(Inez, GuardR));

            Add("C102-ONCE-B", "102", "Once a shift, the other way round: Inez's call, then the guard's own is came-once.", r =>
            {
                var g = r.G;
                r.Do(Fixer, "call", Inez);
                RefusedCall(r, GuardR, "summon-busy", "while he's away, his own call finds him busy");
                r.To(g.GuardAwayUntil + 1);
                RefusedCall(r, GuardR, "summon-came-once", "back at his post: he came once already");
                r.Eq(g.GuardP.Summoned, 1, "one visit, counted on him");
            }, Turned(Inez, GuardR));

            Add("C102-PRESSED-GUARD", "102", "The short visit if either was pressured (option 1): the guard turned by Pressure, called himself, stays summon.pressed.stay and runs back.", r =>
            {
                var g = r.G;
                PressGuard(r);
                var w = g.GuardCallWindows(GuardR)!.Value;
                r.Is(w.pressed < w.plain, "the card's read: the pressed window is the shorter");
                r.Is(g.CallWouldBePressed(GuardR) && g.CallWouldBePressed(Inez), "the card's read: short from either end");
                r.Do(Fixer, "call", GuardR);
                r.Is(g.GuardCallPressed, "his own Pressure makes it the short visit");
                r.Eq(g.GuardAwayUntil - g.LureTick, w.pressed, "the pressed window");
                int leave = g.LureTick + g.VisitWalkTicks + T(g.R.SummonPressedStayS);
                r.To(leave + 1); r.Is(g.GuardRunningBack, "5 s at her side, then he runs back");
            }, KnowsGuard(new Start()));

            Add("C102-PRESSED-INEZ", "102", "The short visit if either was pressured: Inez turned by Pressure, the guard turned before the mission and called himself. Her Pressure, read from his end, makes it the short visit.", r =>
            {
                var g = r.G;
                r.Is(!g.CallWouldBePressed(GuardR), "before the Pressure: the plain visit");
                PressThenCall(r);
                r.Is(g.CallWouldBePressed(GuardR) && !g.GuardP.Has("pressure"), "her Pressure makes his call the short one, though he was never pressured");
                var w = g.GuardCallWindows(GuardR)!.Value;
                r.Do(Fixer, "call", GuardR);
                r.Is(g.GuardCallPressed && g.LureCalled == GuardR, "his call, the short visit");
                r.Eq(g.GuardAwayUntil - g.LureTick, w.pressed, "the pressed window");
            }, Turned2(Reads(), GuardR));

            Add("C102-PRESSED-EITHER", "102", "The short visit if either was pressured: the guard turned by Pressure, Inez helped and called. His Pressure, read from her end, makes her call the short visit.", r =>
            {
                var g = r.G;
                PressGuard(r);
                r.To(r.Send(Fixer, "d:Courtyard")); HelpUntilTurned(r, Inez);
                r.Is(g.Inez.Turned && !g.Inez.Has("pressure"), "Inez helped, never pressured");
                var w = g.GuardCallWindows(Inez)!.Value;
                r.Do(Fixer, "call", Inez);
                r.Is(g.GuardCallPressed && g.LureCalled == Inez, "her call, the short visit");
                r.Eq(g.GuardAwayUntil - g.LureTick, w.pressed, "the pressed window");
            }, KnowsGuard(Reads()));
        }
        static Start Turned2(Start s, params string[] roles) { s.Turned.AddRange(roles); return s; }
    }
}
