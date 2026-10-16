// Cancel and the activity read (Sky 2026-10-15 playtest: "there's nothing showing me what the cell player is doing when they're busy,
// nor is there a way to cancel their action"): each verb cancelled mid-way, at the rulebook cancel line's cost. Each scenario also runs
// twice and replays from its orders (the Theory tests), and checks the state it touches before and after.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static void CancelCases()
        {
            Add("N27", "—", "Cancel a run and a walk: the operative stops where they stand; nothing to cancel when idle.", r =>
            {
                var g = r.G;
                r.Refused(Scout, "cancel", "", "idle: nothing to cancel");
                r.Send(Scout, "d:Courtyard", run: true); r.Wait(T(3));
                var a = g.ActivityOf(OpId.Scout);
                r.Is(a.Verb == "move" && a.Phase == "running" && a.CanCancel && a.CancelCost == "stop" && a.TicksLeft > 0 && a.Progress > 0 && a.Progress < 1000, $"running: {a.Phase} {a.Progress}‰, {a.TicksLeft} ticks left");
                r.Do(Scout, "cancel");
                var at = r.Op(Scout).Pos; r.Wait(T(2));
                r.Eq(r.Op(Scout).Pos, at, "stopped where they stood");
                r.Is(!r.Op(Scout).Running && g.ActivityOf(OpId.Scout).Phase == "idle", "and idle");
                r.Send(Fixer, "d:Courtyard"); r.Wait(T(3)); r.Do(Fixer, "cancel");
                r.Is(r.Op(Fixer).Walk == null, "a walk stops the same way");
            });

            Add("N28", "—", "Cancel Watch and Listen part-done: the time is spent, nothing is learned (the person's state is as before), and the act can be given again.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard"));
                ulong before = Hash.Person(g.Inez);
                r.Do(Scout, "watch", Inez); r.Wait(T(10));
                var a = g.ActivityOf(OpId.Scout);
                r.Eq((a.Verb, a.Phase, a.Target, a.OnDone), ("watch", "acting", Inez, "learns the routine"), "the HUD reads Watch on Inez");
                r.Eq(a.Progress, 500, "halfway through 20 s: 500 ‰");
                r.Eq(a.TicksLeft, T(10), "10 s left");
                r.Eq(a.CancelCost, "lost", "cancel: lost");
                r.Do(Scout, "cancel"); r.Wait(T(15));
                r.Eq(Hash.Person(g.Inez), before, "nothing learned: Inez's state hashes as before");
                r.Is(!g.Inez.KRoutine, "the routine is not known");
                r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.Is(g.Inez.KRoutine, "watched again in full, it lands");
                r.To(r.Send(Scout, "beside-inez"));
                before = Hash.Person(g.Inez);
                r.Do(Scout, "listen", Inez); r.Wait(T(20)); r.Do(Scout, "cancel"); r.Wait(T(30));
                r.Eq(Hash.Person(g.Inez), before, "a Listen cancelled learns nothing either");
            });

            var help = new Start(); help.Known[Inez] = new List<string> { "routine", "need", "temperament" };
            Add("N29", "—", "Cancel an approach mid-conversation (rulebook cancel.turn aborted): the money held comes back, no Trail, and Inez remembers an aborted approach with the Fixer's face; it is no refusal, so Help can be given again, and given as often as the card asks it turns her.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard"));
                long cash = g.Cash; int trail = g.Trail;
                var q = g.Preview("help", Inez); // the card's outcome is the cast's: turned (an Idealist), not yet (a Fearful Inez's first of two)
                r.Do(Fixer, "turn", Inez, "help");
                r.Is(g.Cash < cash, "the price is held at the order");
                r.Wait(T(20));
                var a = g.ActivityOf(OpId.Fixer);
                r.Eq((a.Verb, a.OnDone, a.CancelCost), ("turn", q.Outcome, "aborted"), "the HUD reads the approach, the card's outcome and the cancel's cost");
                r.Do(Fixer, "cancel");
                r.Eq(g.Cash, cash, "the money comes back");
                r.Eq(g.Trail, trail, "no Trail");
                r.Is(g.Inez.Has("aborted") && !g.Inez.Has("refusal") && !g.Inez.Turned, "she remembers an aborted approach, not a refusal");
                r.Is(g.HoldsFace(g.Inez), "with the Fixer's face");
                r.Is(r.Try(Fixer, "turn", Inez, "help"), "no refusal: Help can be given again"); r.Done(Fixer);
                HelpUntilTurned(r, Inez);
                r.Is(g.Inez.Turned, "Help again turns her");
            }, help);

            Add("N30", "—", "Cancel a Loop: part-done, nothing lands and no Trail; held, the loop ends and the camera comes back.", r =>
            {
                var g = r.G;
                r.To(r.Send(Tech, "cabinet"));
                int trail = g.Trail;
                r.Do(Tech, "loop", "entrance"); r.Wait(T(30)); r.Do(Tech, "cancel");
                r.Wait(T(40));
                r.Is(!g.LoopOn && g.Trail == trail, "part-done: no loop, no Trail");
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.Is(!g.CameraWatching("entrance", r.Stand("d:Office entrance")), "looped in full, the camera is blind");
                var a = g.ActivityOf(OpId.Tech);
                r.Eq((a.Verb, a.Phase, a.CancelCost), ("loop", "holding", "ends"), "the HUD reads the loop held");
                r.Do(Tech, "cancel");
                r.Is(!g.LoopOn && g.CameraWatching("entrance", r.Stand("d:Office entrance")), "cancelled, the camera comes back");
            });

            Add("N31", "—", "Cancel Reroute and Mislead part-done: the patrol keeps its sweep and the rivals stay; no Trail.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Scout, new Pt(8250, -3100))));
                int trail = g.Trail;
                r.Do(Tech, "reroute", "patrol"); r.Wait(T(20)); r.Do(Tech, "cancel"); r.Wait(T(30));
                r.Is(g.PatrolAwayUntil <= g.Tick && g.Trail == trail, "the patrol keeps its sweep; no Trail");
                r.To(g.RivalAt);
                r.Do(Scout, "mislead", "rivals"); r.Wait(T(20)); r.Do(Scout, "cancel"); r.Wait(T(30));
                r.Is(g.RivalsPresent && g.RivalsDelayedUntil == 0 && g.Trail == trail, "the rivals stay; no Trail");
            });

            Add("N32", "—", "Cancel Enter (rulebook cancel.enter staff-report): the Enforcer comes back out at the public door without Vale; the staff report the intruder, one reported incident.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Scout, "d:Courtyard"), r.Send(Tech, "cabinet")));
                r.Do(Scout, "watch", Inez); r.Do(Tech, "loop", "entrance"); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez);
                r.Send(Fixer, "d:Courtyard"); r.SendTo(Enf, new Pt(4900, -1700));
                r.Done(Scout); HelpUntilTurned(r, Inez);
                while (!g.SweepSeesEntrance(g.Tick)) g.Step();
                r.Do(Fixer, "call", Inez);
                r.To(r.Send(Enf, "d:Office entrance"));
                while (g.PatrolWatching(r.At("d:Office entrance"))) g.Step();
                r.Do(Enf, "enter"); r.Wait(T(10));
                var a = g.ActivityOf(OpId.Enforcer);
                r.Eq((a.Verb, a.CancelCost), ("enter", "staff-report"), "the HUD reads Enter and the cancel's cost");
                int reported = g.Reported;
                r.Do(Enf, "cancel");
                r.Is(!r.Op(Enf).Inside && r.Op(Enf).Pos == r.Stand("d:Office entrance"), "back out at the public door");
                r.Is(g.Vale == ValeAt.Office && !g.EntryInProgress, "without Vale");
                r.Eq(g.Reported, reported + 1, "the staff report the intruder");
                r.Eq(g.Alert, 1, "one reported incident: Alert 1");
            });

            var drv = Turned(Driver); drv.Known[Driver] = new List<string> { "routine", "need", "temperament" };
            Add("N33", "—", "Cancel Take (rulebook cancel.take vale-reports): Vale is let go at his car and reports it; the Enforcer can take him again; holding Vale is no cancel.", r =>
            {
                var g = r.G;
                r.Send(Enf, "d:Service lane");
                r.To(r.SendTo(Scout, new Pt(8250, -3100))); r.To(T(320)); r.Do(Scout, "mislead", "rivals");
                r.To(T(370));
                r.Do(Enf, "take"); r.Wait(T(10));
                r.Eq(g.ActivityOf(OpId.Enforcer).CancelCost, "vale-reports", "the HUD reads the cost");
                r.Do(Enf, "cancel");
                r.Eq(g.Vale, ValeAt.Pickup, "Vale is let go at his car");
                r.Eq(g.Alert, 1, "he reports it: Alert 1");
                r.Is(!g.Acts.Exists(x => x.Take), "the Take was inside his car (the driver turned, rev 4): nobody saw it, so his report is the one incident");
                r.Is(g.ValeP.Has("aborted"), "and remembers it: a lead for the investigators (Motive addendum Q4)");
                r.Do(Enf, "take"); r.Done(Enf);
                r.Eq(g.Vale, ValeAt.WithEnforcer, "taken again");
                var a = g.ActivityOf(OpId.Enforcer);
                r.Is(!a.CanCancel && a.CancelCost == "keeps-vale", "holding Vale: putting him down is no cancel");
                r.Refused(Enf, "cancel", "", "the cancel is refused");
            }, drv);

            Add("N34", "—", "Cancel a carry: lifting part-done, nothing is lifted; carrying, the downed operative is laid down where the carrier stands.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Scout, FrontOfGuard(r))));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Do(Scout, "draw"); g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "stance", Stance = "hold" });
                r.Send(Fixer, "d:Office approach");
                r.Is(r.Until(() => r.Op(Scout).Down, T(30)), "the Scout is down");
                r.To(r.SendTo(Fixer, r.Op(Scout).Pos));
                r.Do(Fixer, "carry", Scout); r.Wait(T(1)); r.Do(Fixer, "cancel"); r.Wait(T(5));
                r.Is(!r.Op(Fixer).Carrying.HasValue && !r.Op(Scout).CarriedBy.HasValue, "lifting cancelled: nothing lifted");
                r.Do(Fixer, "carry", Scout); r.Done(Fixer);
                r.Send(Fixer, "d:Courtyard"); r.Wait(T(3));
                r.Eq(g.ActivityOf(OpId.Fixer).Phase, "walking", "carrying, he walks");
                r.Do(Fixer, "cancel"); // a cancel stops the walk first
                r.Eq(g.ActivityOf(OpId.Fixer).CancelCost, "put-down", "standing, the HUD offers to lay him down");
                r.Do(Fixer, "cancel");
                var at = r.Op(Fixer).Pos;
                r.Is(!r.Op(Fixer).Carrying.HasValue && r.Op(Scout).Pos == at, "laid down where the Fixer stands");
                r.Send(Fixer, "d:Office approach"); r.Wait(T(3));
                r.Eq(r.Op(Scout).Pos, at, "and stays there");
            });

            Add("N35", "—", "Cancel a draw and a fire order: a draw not yet done goes back; a fire order ceases before the first shot, the weapon stays drawn.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Do(Enf, "draw"); r.Wait(5);
                r.Eq(g.ActivityOf(OpId.Enforcer).Phase, "drawing", "the HUD reads the draw");
                r.Do(Enf, "cancel");
                r.Is(!r.Op(Enf).Gun.Drawn, "the weapon goes back");
                r.Fire(Enf, GuardR); r.Wait(T(1) + 5);
                var a = g.ActivityOf(OpId.Enforcer);
                r.Is(a.Verb == "fire" && a.Phase == "firing" && a.Target == GuardR && a.TicksLeft > 0, $"the HUD reads the fire order, next shot in {a.TicksLeft} ticks");
                r.Do(Enf, "cancel"); r.Wait(T(5));
                r.Eq(g.Gunfire, 0, "no shot fired");
                r.Is(r.Op(Enf).Gun.Drawn && r.Op(Enf).Gun.Order == "", "ceased, the weapon stays drawn");
                r.Eq(g.GuardHealth, g.R.HealthGuard, "the guard is untouched");
            });
        }
    }
}
