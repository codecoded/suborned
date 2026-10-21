// Brief 027, Motive's rulings on Suture's balance check (EXPERIMENTAL; briefs/027/motive/tuning.md §6.1–6.4), each on a start with the lab on:
//    N152 aim memory: the count is the pair (shooter, target)'s; the first shot on a new target ranges (fight.lab.accStart); cease and fire
//         again resume it, and the chip reads it; a re-aim still waits aimNew; lab off, no memory
//    N153 the charge rests charge.rest after a targeted shot (a volley's), and a refunded volley stamps nothing
//    N154 one warning per watcher: two agents drawn in his view share one shout; heeded, +1 Trail once; the next draw he sees is drawn-seen at once
using System;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The Tech loops the entrance camera and reroutes the patrol; a free spot 3–8 m from the guard that only he watches (N146's).</summary>
        static Pt? GuardOnlySpot(Run r)
        {
            var g = r.G;
            r.To(Max(r.Send(Tech, "cabinet"))); r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
            var post = g.PosOf(GuardR)!.Value;
            for (int dx = -800; dx <= 800; dx += 100) for (int dy = -800; dy <= 800; dy += 100)
            { var q = new Pt(post.X + dx, post.Y + dy); if (g.S.Grid.Free(q) && G.D2(q, post) >= 300L * 300 && g.GuardWatching(q) && !g.PatrolWatching(q) && !g.CameraWatching("pickup", q)) return q; }
            return null;
        }

        static void FightLabTuning()
        {
            Add("N152", "—", "Brief 027 aim memory (EXPERIMENTAL; Motive, tuning.md §6.1–6.2; Suture F1, F3): a shooter's count belongs to the pair (shooter, target). The first shot on a new target starts at fight.lab.accStart (0: below four pips it ranges, a miss); cease and fire again on the same target resume the count it left (no reset to acc.start), and the chip reads the same before and after; the re-aim still waits aimNew; with the lab off a gun keeps no memory.", r =>
            {
                var g = r.G; var spot = GuardOnlySpot(r);
                if (!r.Is(spot.HasValue, "a spot the guard watches")) return;
                r.To(r.SendTo(Scout, spot!.Value));
                r.Do(Scout, "draw"); r.Wait(g.DrawT + 1);
                var p0 = g.PreviewFire(OpId.Scout, GuardR);
                r.Is(p0.Refused == "" && p0.Pips >= 1 && p0.Pips <= 3, $"the Scout reaches the guard at {p0.Pips} pips ({p0.Acc} ‰, {p0.DistanceCm / 100} m)");
                r.Eq(g.Labr!.AccStart, 0, "fight.lab.accStart is 0 (tuning.md §6.2)");
                r.Fire(Scout, GuardR);
                r.Until(() => r.Shots().Any(s => s.by == "scout"), T(3));
                var first = r.Shots().First(s => s.by == "scout");
                r.Eq(first.hit, g.Labr.AccStart + p0.Acc >= 1000, "the first shot on a new target ranges: a miss below four pips");
                var gun = r.Op(Scout).Gun; int acc1 = gun.Acc;
                r.Eq(acc1, (g.Labr.AccStart + p0.Acc) % 1000, "the count after one shot");
                int chip = g.PreviewFire(OpId.Scout, GuardR).ShotsToDown;
                r.Do(Scout, "cease");
                r.Is(gun.Target.Length == 0, "cease drops the aim");
                r.Eq(g.PreviewFire(OpId.Scout, GuardR).ShotsToDown, chip, "the chip reads the remembered count after the cease");
                int t = g.Tick; r.Fire(Scout, GuardR);
                r.Eq(gun.Acc, acc1, "firing again resumes the pair's count (no reset to acc.start)");
                r.Is(gun.NextShot >= t + g.AimNewT, $"the re-aim still waits aimNew ({g.AimNewT} ticks)");
                r.Is(gun.Mem != null && gun.Mem.ContainsKey(GuardR), "the gun remembers the guard");
                var today = Twin(g, new Start { Security = 1 });
                r.Is(today.OpOf(OpId.Scout).Gun.Mem == null && !today.Lab, "lab off, before any shot: no memory (today keeps the pair's count too since tuning.md §13.1, from acc.start: F11-SPAM)");
            }, LabStart(s => s.Security = 1));

            Add("N153", "—", "Brief 027 charge rest (EXPERIMENTAL; Motive, tuning.md §6.3; Suture F5): a targeted shot (a volley's, any weapon) stamps the bar's rest, as a charged shot does; the bar comes back only charge.rest after the last one; a volley ended early refunds its unfired charge and stamps nothing.", r =>
            {
                var g = r.G; var spot = GuardOnlySpot(r);
                if (!r.Is(spot.HasValue, "a spot the guard watches")) return;
                r.To(r.SendTo(Scout, spot!.Value));
                r.Do(Scout, "draw"); r.Wait(g.DrawT + 1);
                int rest = g.Labr!.ChargeRestTicks, tc = r.Op(Scout).Gun.W!.TargetCharge, max = g.Labr.ChargeMax;
                r.Eq(r.Op(Scout).Gun.W!.Charge, 0, "the sidearm draws no charge of its own");
                // a volley ended at once: the refund comes back and nothing is stamped
                int lc0 = r.Op(Scout).LastCharged;
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "volley", Target = GuardR, Shots = 6 }).ok, "a volley of 6 is taken");
                r.Eq(g.ChargeOf(OpId.Scout), max - 6 * tc, $"paid {6 * tc} when given");
                r.Do(Scout, "cease");
                r.Eq((g.ChargeOf(OpId.Scout), r.Op(Scout).LastCharged), (max, lc0), "ended before a shot: all of it back, and the rest is not stamped");
                // a volley of 1: its shot stamps the rest
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "volley", Target = GuardR, Shots = 1 }).ok, "a volley of 1 is taken");
                r.Until(() => r.Shots().Any(s => s.by == "scout"), T(3));
                int shotAt = r.Shots().First(s => s.by == "scout").tick, c0 = g.ChargeOf(OpId.Scout);
                r.Eq(r.Op(Scout).LastCharged, shotAt, "the targeted shot stamps the bar's rest");
                r.Is(c0 < max, $"the bar is short after the volley ({c0} of {max})");
                r.To(shotAt + rest);
                r.Eq(g.ChargeOf(OpId.Scout), c0, $"no charge comes back within the rest ({rest} ticks)");
                r.Wait(2);
                r.Is(g.ChargeOf(OpId.Scout) > c0, $"after the rest it comes back ({g.ChargeOf(OpId.Scout)})");
            }, LabStart(s => s.Security = 1));

            Add("N154", "—", "Brief 027 one warning per watcher (EXPERIMENTAL; Motive, tuning.md §6.4; Suture F4): the Scout and the Fixer draw in the guard's view together: one shout, both in its window; both holstered in time: Alert 1 and +1 Trail once (he logs it); the next draw he sees before any shot is drawn-seen at once, with no second warning.", r =>
            {
                var g = r.G; var spot = GuardOnlySpot(r);
                if (!r.Is(spot.HasValue, "a spot the guard watches")) return;
                var spot2 = g.S.Grid.NearestFree(spot!.Value + new Pt(100, 0)) ?? spot.Value;
                r.To(Max(r.SendTo(Scout, spot.Value), r.SendTo(Fixer, spot2)));
                if (!r.Is(g.GuardWatching(r.Op(Fixer).Pos), "the guard watches the Fixer too")) return;
                int trail0 = g.Trail;
                r.Do(Scout, "draw"); r.Do(Fixer, "draw"); r.Wait(1);
                r.Eq((g.Warnings.Count, g.WarningShouts), (2, 1), "one shout from the guard; both agents in its window");
                r.Is(g.Warnings.All(x => x.Watcher == "guard" && x.Until == g.Warnings[0].Until), "one window for both");
                r.Wait(T(1)); r.Do(Scout, "holster"); r.Do(Fixer, "holster"); r.Wait(T(3));
                r.Is(g.Alert == 1 && !g.Log.Exists(l => l.Code == "drawn.seen"), "holstered in time: Alert 1, no drawn-seen");
                r.Eq(g.Trail, trail0 + g.Labr!.WarnTrail, "the heeded warning is logged: +1 Trail, once for the shout");
                r.Do(Fixer, "draw"); r.Wait(1);
                r.Is(g.Log.Exists(l => l.Code == "drawn.seen") && g.WarningShouts == 1 && g.Warnings.Count == 0, $"the next draw he sees is drawn-seen at once (no second warning), Alert {g.Alert}");
            }, LabStart(s => s.Security = 1));
        }
    }
}
