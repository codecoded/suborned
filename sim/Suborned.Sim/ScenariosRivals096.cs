// Brief 096's rivals round, engine 0.41.0 (Keel; Motive briefs/096/motive/rivals.html §7, rivals.json tests; Sky 2026-11-04 "all recommended").
// Motive's RIV scenarios (engine ids 096-RIV-n: an id starting R is the fight range's, RangeScenarioTests), part 1: the block on arrival and the lookout (rulebook rivals.block, rivals.lookout, rivals.turn).
//  RIV-1  arrive-blocks-square (200 seeds)  no orders: the car turns in 2:35 ±15, stops at the square's block point; Go car-boxed, Drive drive-boxed, naming car:rivals; the lookout posted by 4:20 on every seed (RIV-4's seeds)
//  RIV-2  arrive-follows-drive              our car Driven to the service drive at 2:20: the car blocks the drive's mouth; the three post at the svc spots
//  RIV-3  retarget-mid-entry                a Drive started after the turn-in, before the stop: the car turns for the new stand's block point
//  RIV-4  crew-out-and-posts                the three step out rivals.out then rivals.step apart for the stand's first three spots; the lookout next, facing the bay; the wheelman aboard
//  RIV-5  wheelman-stays-aboard             the cell stands at its car until the close: the wheelman never gets out
//  RIV-6  lookout-follows-fails (200 seeds) route B with the lookout ignored: he runs at 5:53 and reaches Popovic's car: rivals-reach-vale, cause lookout
//  RIV-6b lookout-sees-the-take             a Take at the pickup in his sight: he runs to it and reaches Popovic first (P59-BEAT's place)
//  RIV-7  lookout-misled-covers             Mislead landing 5:00: the other door, 60 s, back; no follow; +1 T
//  RIV-8  lookout-misled-early              landing 4:10: back before 5:53, so he follows; the card's "back at m:ss" said so
//  RIV-9  lookout-paid-gone                 a Mercenary takes $30,000 (+2 T), walks off, is never recalled: 4 rivals in the fight
//  RIV-10 loyal-refuses-tells-crew          a Loyal man refuses: the money stays, the refusal is remembered, the crew counts the Fixer a foe
//  RIV-11 lookout-stashed-unseen            KO, tie, carry and stash with the patrol rerouted: out for good, down for the break
//  RIV-12 lookout-ko-wakes                  knocked out and left untied: he wakes and walks back to his post
//  RIV-13 driver-no-word                    the driver turned: no rivals.warned; the lookout stays at his post
//  RIV-14 posted-paid                       a posted Mercenary paid: one fewer in the fight
//  RIV-15 posted-misled-joins-late          misled before the spring: he joins the fight from where he is
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static CrewMember Lk(Run r) => r.G.Crew[3];
        static bool LookoutAtPost(Game g) => g.Crew[3].Mode == CrewMode.Posted && g.Crew[3].Post == "lookout";
        /// <summary>Beside the lookout's post (the free ground 3 m west of it, behind his facing toward the official bay's north).</summary>
        static Pt BesideLookout(Game g) { var p = g.S.Stand(g.S.RivalLookout); return g.S.Grid.NearestFree(p + new Pt(-300, 0)) ?? p; }
        static Start Temper(Start s, string id, string kind) { s.Temperament[id] = kind; return s; }
        static Order GoOrder(Game g) => new Order { Tick = g.Tick, Op = "cell", Do = "go" };
        /// <summary>The Enforcer walks in behind a member (east of the lookout, his back) and knocks him out once in reach (re-aimed each second, 30 s at most).</summary>
        static bool KoFromBehind(Run r, string id)
        {
            var g = r.G; var m = g.CrewOf(id)!;
            for (int k = 0; k < 30 && !g.Over; k++)
            {
                var at = g.MemberPos(m, g.Tick); var to = g.S.Grid.NearestFree(at + new Pt(110, 0)) ?? at;
                if (G.Within(r.Op(Enf).Pos, at, 140)) { var a = g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "takedown", Target = id, How = "knockout" }); if (a.ok) { r.Done(Enf); return true; } }
                g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", Point = to }); r.Wait(T(1));
            }
            return r.Is(false, $"the Enforcer gets behind {id} and knocks him out");
        }
        /// <summary>Route B's opening on the 0.41.0 engine (Open059) and, at `landS`, the lookout dealt with: "mislead" (the Scout), "pay" (the Fixer),
        /// "none". Returns once the order has landed (or at once for none).</summary>
        static void DealLookout(Run r, string how, int landS)
        {
            var g = r.G; if (how == "none") return;
            string who = how == "pay" ? Fixer : Scout; int s = how == "pay" ? g.R.Crew.PayS : g.R.MisleadS;
            r.To(Math.Max(g.Tick, r.SendTo(who, BesideLookout(g))));
            r.Until(() => LookoutAtPost(g), T(330));
            r.To(Math.Max(g.Tick, T(landS - s)));
            if (how == "pay") r.Do(Fixer, "turn", "rival-4", "pay"); else r.Do(Scout, "mislead", "rival-4");
            r.Done(who);
        }
        /// <summary>The Enforcer takes Vale from the back seat in the service bay (the driver turned); returns whether the Take landed.</summary>
        static bool TakeAtBay(Run r)
        {
            var g = r.G;
            r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick, T(480));
            if (g.Over) return false;
            var at = g.S.Grid.NearestFree(g.CarPos() + new Pt(0, -250)) ?? g.CarPos();
            r.To(Math.Max(g.Tick, r.SendTo(Enf, at)));
            r.Do(Enf, "take"); r.Done(Enf);
            return g.Vale == ValeAt.WithEnforcer;
        }

        static void Rivals096()
        {
            Add("096-RIV-1", "096", "Motive RIV-1 (with RIV-4's every-seed line), 200 seeds, no orders but the Tech in our car: the rival car turns in 2:35 ±15 s, drives to the square's block point and stops there; Go is refused car-boxed and Drive drive-boxed, both naming car:rivals; the lookout is posted by 4:20, facing the official bay; the wheelman aboard.", r =>
            {
                if (!r.Is(r.Inputs != null, "inputs")) return;
                var bad = new List<string>(); int lo = int.MaxValue, hi = 0, lkLate = 0;
                for (int seed = 1; seed <= 200; seed++)
                {
                    var s = new Start { Seed = seed }; var g = Engine.New(r.Inputs!, s); var q = new Run(g) { Inputs = r.Inputs };
                    q.To(q.SendTo(Tech, g.GetawaySeat)); q.Do(Tech, "board", "car"); q.Done(Tech);
                    q.Until(() => g.CarSt == CrewCar.Blocking, T(300));
                    lo = Math.Min(lo, g.CrewEntry); hi = Math.Max(hi, g.CrewEntry);
                    var bp = g.BlockAt(g.S.Stands[0]).at; var go = g.GoQuery(); var dr = g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "drive-car", Target = "service-drive" });
                    if (g.CarSt != CrewCar.Blocking || g.RivalCarPose()!.Value.pos != bp || go.code != "car-boxed" || go.ids != "car:rivals" || dr.code != "drive-boxed" || dr.ids != "car:rivals") bad.Add($"seed {seed}: {g.CarSt} {go.code}/{go.ids} {dr.code}");
                    q.Until(() => LookoutAtPost(g), T(4 * 60 + 20) - g.Tick);
                    if (!LookoutAtPost(g) || g.Crew[4].Mode != CrewMode.Aboard) lkLate++;
                }
                int mid = r.G.CrewEntry; r.Is(lo >= mid - T(15) && hi <= mid + T(15), $"the turn-in {Report.Clock(mid)} ±15 s across the seeds (seed 0's, the rulebook's band): {Report.Clock(lo)} to {Report.Clock(hi)}");
                r.Is(bad.Count == 0, Iv($"on every seed it stops at the square's block point and boxes our car in: {(bad.Count == 0 ? "200/200" : string.Join("; ", bad.Take(4)))}"));
                r.Eq(lkLate, 0, "on every seed the lookout is posted by 4:20 and the wheelman is aboard");
                r.Wait(1);
            });

            Add("096-RIV-2", "096", "Motive RIV-2: our car Driven to the service drive at 2:20: the rival car comes to the drive's mouth on Apron Road (its block point) and boxes it; the three post at the service drive's spots.", r =>
            {
                var g = r.G; r.To(T(140) - T(30)); r.To(r.SendTo(Tech, g.GetawaySeat)); r.Do(Tech, "board", "car"); r.Done(Tech);
                r.To(Math.Max(g.Tick, T(140))); r.Do(Tech, "drive-car", "service-drive");
                if (!r.Is(UntilBlocked(r), Iv($"it stops at {Report.Clock(g.Tick)}"))) return;
                var bp = g.BlockAt(g.S.StandById("service-drive")!).at;
                r.Is(g.RivalCarPose()!.Value.pos == bp && G.Within(bp, new Pt(8600, -1500), 50), Iv($"at the drive's mouth on Apron Road ({bp}; Motive's (86.00, -15.00))"));
                r.Eq(string.Join(",", g.GetawayBoxers()), "car:rivals", "our car on the drive is boxed in");
                r.Until(() => g.Crew.Take(3).All(m => m.Mode == CrewMode.Posted), T(60));
                r.Is(g.Crew.Take(3).All(m => m.SpotStand == "service-drive"), "the three post at the service drive's spots");
            });

            Add("096-RIV-3", "096", "Motive RIV-3: the rival car has turned in; our car starts a Drive to the service drive before it stops: it turns for the service drive's block point (rivals.retarget) and boxes the drive's mouth.", r =>
            {
                var g = r.G; r.To(r.SendTo(Tech, g.GetawaySeat)); r.Do(Tech, "board", "car"); r.Done(Tech);
                r.To(g.CrewEntry + T(5));
                r.Is(g.CarSt == CrewCar.Entering, "the rival car is driving in");
                r.Do(Tech, "drive-car", "service-drive");
                r.Is(g.Log.Exists(l => l.Code == "rivals.retarget" && l.Tick == g.Tick), "it turns after our car at once (rivals.retarget)");
                if (!r.Is(UntilBlocked(r), Iv($"it stops at {Report.Clock(g.Tick)}"))) return;
                r.Until(() => g.CurStand.Id == "service-drive" && !g.GetawayDriving, T(40));
                r.Is(g.RivalCarPose()!.Value.pos == g.BlockAt(g.S.StandById("service-drive")!).at && g.GetawayBoxers().Contains("car:rivals"), Iv($"at the service drive's block point, boxing our car in ({g.RivalCarPose()!.Value.pos} vs {g.BlockAt(g.S.StandById("service-drive")!).at}; boxers {string.Join(",", g.GetawayBoxers())})"));
            });

            Add("096-RIV-4", "096", "Motive RIV-4, seed 0: rivals.out after the stop the three step out rivals.step apart and walk to the stand's first three spots; the lookout steps out next and posts facing the official bay; the wheelman stays aboard.", r =>
            {
                var g = r.G; if (!r.Is(UntilBlocked(r), "the rival car stops at the block")) return;
                int stop = g.Tick; int o = T(g.R.Crew.OutS), st = T(g.R.Crew.StepS);
                r.Eq((g.Crew[0].OutAt, g.Crew[1].OutAt, g.Crew[2].OutAt, g.Crew[3].OutAt), (stop + o, stop + o + st, stop + o + 2 * st, stop + o + 3 * st), "out at rivals.out, then rivals.step apart: the three, then the lookout");
                r.Eq(g.Crew[4].OutAt >= int.MaxValue / 8, true, "the wheelman has no step-out");
                r.Until(() => LookoutAtPost(g) && g.Crew.Take(3).All(m => m.Mode == CrewMode.Posted), T(90));
                var spots = g.Crew.Take(3).Select(m => g.S.Stands[0].Ambush[m.Spot]).OrderBy(x => x, StringComparer.Ordinal).ToList();
                r.Eq(string.Join(",", spots), "ambush-1,ambush-2,ambush-3", "the three take the stand's first three spots");
                var lk = g.MemberPose(Lk(r))!.Value; r.Is(G.InCone(lk.pos, lk.facing, g.R.RivReachCm, g.R.RivHalfCm, 0, g.CarPos()), Iv($"the lookout posted at {Report.Clock(g.Tick)} with Popovic's car in his cone"));
                r.Is(g.Log.Exists(l => l.Code == "rivals.lookout-posted"), "rivals.lookout-posted: the crucial card (Latch)");
                r.Eq(g.Crew[4].Mode, CrewMode.Aboard, "the wheelman is aboard");
            });

            Add("096-RIV-5", "096", "Motive RIV-5: the cell stands at its car until the close (Vale waits, test only): the wheelman never gets out without a spring, gunfire or a disable.", r =>
            {
                var g = r.G; bool out_ = false;
                while (!g.Over && g.Tick < T(g.ClockOver) - 1) { g.Step(); if (g.Crew[4].Mode != CrewMode.Aboard) out_ = true; }
                r.Is(!out_ && g.CarSt == CrewCar.Blocking, Iv($"the wheelman stayed aboard to {Report.Clock(g.Tick)}"));
            }, KeepVale(new Start()));

            Add("096-RIV-6", "096", "Motive RIV-6, 200 seeds: route B with the lookout ignored (the driver turned, nothing else): Popovic's car leaves the bay at 5:53, the lookout sees it, runs after it and reaches it in the service bay: rivals-reach-vale, cause lookout, on every seed.", r =>
            {
                if (!r.Is(r.Inputs != null, "inputs")) return;
                int fails = 0; var other = new List<string>(); int first = int.MaxValue, last = 0;
                for (int seed = 1; seed <= 200; seed++)
                {
                    var s = Turned(Driver); s.Seed = seed; var g = Engine.New(r.Inputs!, s); var q = new Run(g) { Inputs = r.Inputs };
                    q.Until(() => g.Over, T(420));
                    if (g.FailCode == "rivals-reach-vale" && g.Cause?.Why == "lookout") { fails++; first = Math.Min(first, g.Tick); last = Math.Max(last, g.Tick); }
                    else other.Add($"seed {seed}: {g.Result} {g.FailCode} {g.Cause?.Why}");
                }
                r.Eq(fails, 200, Iv($"the lookout reaches Popovic on every seed, {Report.Clock(first)} to {Report.Clock(last)}{(other.Count > 0 ? ": " + string.Join("; ", other.Take(3)) : "")}"));
                var g0 = r.G; r.To(T(380)); r.Is(g0.Log.Exists(l => l.Code == "rivals.lookout-follows"), "seed 0: rivals.lookout-follows, the card that stays until clicked");
            }, Turned(Driver));

            Add("096-RIV-6b", "096", "The lookout sees a Take at the pickup (the front, the driver unturned, the pickup camera looped): he runs to it and reaches Popovic before it ends: rivals-reach-vale, cause lookout (P59-BEAT's place: the parked car's wheelman is retired).", r =>
            {
                var g = r.G;
                r.To(r.Send(Tech, "cabinet")); r.To(T(300)); r.Do(Tech, "loop", "pickup"); r.Done(Tech);
                r.To(r.SendTo(Enf, Beside(r, g.CarPos())!.Value)); r.To(T(g.ClockValeAtCar));
                r.Is(LookoutAtPost(g), "the lookout is at his post");
                r.Do(Enf, "take", Vale); r.Until(() => g.Over, T(60));
                r.Eq((g.FailCode, g.Cause?.Why), ("rivals-reach-vale", "lookout"), Iv($"the lookout reaches him first at {Report.Clock(g.Tick)}"));
            }, KeepVale(new Start()));

            Add("096-RIV-7", "096", "Motive RIV-7: the lookout misled, landing 5:00: he walks to the Office's public door (the target's other door), watches it 60 s and walks back; Popovic's car leaves at 5:53 unfollowed; +1 T.", r =>
            {
                var g = r.G; int t0 = g.Trail; DealLookout(r, "mislead", 300);
                r.Eq(g.Trail - t0, g.R.MisleadTrail, "Mislead's +1 T");
                r.Is(Lk(r).Mode == CrewMode.Walking && Lk(r).Goal == "misled", "he walks to the other door");
                r.Until(() => Lk(r).Mode == CrewMode.Posted, T(60));
                var door = g.S.Entrances.Find(e => e.id == "office-entrance").stand;
                r.Is(Lk(r).Post == "misled" && G.Within(g.MemberPos(Lk(r), g.Tick), door, 300), Iv($"he watches the public door from {Report.Clock(g.Tick)}"));
                int back = g.Tick + T(g.R.Crew.MisledS);
                r.To(back + 1); r.Is(Lk(r).Mode == CrewMode.Walking && Lk(r).Goal == "back", "after rivals.lookout.misled he walks back");
                r.To(T(400)); r.Is(!g.Log.Exists(l => l.Code == "rivals.lookout-follows") && !g.Over, "nobody follows Popovic's car (the Take is yours to make)");
            }, Turned(Driver));

            Add("096-RIV-8", "096", "Motive RIV-8 (lookout-misled-late): the Mislead given at 5:30 lands at 6:00, after Popovic's car has left: the lookout is already running after it, the word misses him (mislead.missed), and he reaches Popovic. (Too early is not reachable on the Handover: he is posted at 3:58, so the earliest landing is 4:28 and he is back at 6:12, after the car leaves.)", r =>
            {
                var g = r.G; r.To(Math.Max(g.Tick, r.SendTo(Scout, BesideLookout(g)))); r.Until(() => LookoutAtPost(g), T(330));
                int said = g.MisleadBackTick("rival-4");
                r.Is(said > T(353), Iv($"the card, given at {Report.Clock(g.Tick)}: back at his post {Report.Clock(said)}, after the car leaves (5:53)"));
                r.To(T(354) - T(g.R.MisleadS)); r.Do(Scout, "mislead", "rival-4"); r.Done(Scout); // due 5:54, a second after he starts running
                r.Is(!Lk(r).Misled, $"the word never takes: he is running after the car ({(g.Over ? "he reached it first, " + Report.Clock(g.Tick) : "mislead.missed")})");
                r.Until(() => g.Over, T(200));
                r.Eq((g.FailCode, g.Cause?.Why), ("rivals-reach-vale", "lookout"), "he reaches Popovic");
            }, Turned(Driver));

            Add("096-RIV-9", "096", "Motive RIV-9: the lookout a Mercenary (test only), paid $30,000: +2 T, he walks off the map and is never recalled; the Take at the bay; the safe fight has four rivals.", r =>
            {
                var g = r.G; int t0 = g.Trail; long c0 = g.Cash;
                r.Eq(g.CrewTemperament("rival-4"), "?", "before a Listen the Pay card reads ?");
                DealLookout(r, "pay", 270);
                r.Is(g.CrewPaid("rival-4") && g.Cash == c0 - g.R.Crew.PayUsd && g.Trail - t0 == g.R.Crew.PayTrail, Iv($"he takes it: -${g.R.Crew.PayUsd}, +{g.Trail - t0} T"));
                r.Until(() => Lk(r).Mode == CrewMode.Gone, T(120)); r.Eq(Lk(r).Mode, CrewMode.Gone, "he walks off the map");
                if (!r.Is(TakeAtBay(r), Iv($"the Take at the bay lands ({g.Result} {g.FailCode})"))) return;
                r.Eq(string.Join(",", RivalBodies(g)), "rival-1,rival-2,rival-3,rival-5", "four in the fight: the paid man is not recalled (the wheelman's body still aboard)");
            }, Temper(Turned(Driver), "rival-4", "mercenary"));

            Add("096-RIV-10", "096", "Motive RIV-10: the lookout Loyal (test only): the Pay is refused, the money stays with the cell, the refusal is remembered (a second Pay refused-before), and the crew counts the Fixer a foe at the spring.", r =>
            {
                var g = r.G; long c0 = g.Cash; int t0 = g.Trail;
                DealLookout(r, "pay", 270);
                r.Is(!g.CrewPaid("rival-4") && g.Cash == c0 && g.Trail == t0 && g.Log.Exists(l => l.Code == "rivals.refuses"), "he refuses: no money, no Trail (rivals.refuses)");
                r.Eq(g.CrewTemperament("rival-4"), "loyal", "the refusal teaches his temperament");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = "rival-4", Approach = "pay" }).code, "refused-before", "a second Pay is refused-before");
                r.Is(g.CrewSpringFoes.Contains("fixer"), "the crew counts the Fixer a foe at the spring");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = "rival-1", Approach = "help" }).code, "crew-no-need", "Help on a member: crew-no-need");
            }, Temper(Turned(Driver), "rival-4", "loyal"));

            Add("096-RIV-11", "096", "Motive RIV-11: the lookout taken down unseen: the Tech reroutes the patrol, the Enforcer knocks him out from his blind side, ties him, carries him to the Office's service door and stashes him. He is out for good and down for the crew's break; measured: the Trail and Alert of who saw it.", r =>
            {
                var g = r.G; int t0 = g.Trail;
                r.Send(Tech, "cabinet"); r.SendTo(Enf, g.S.Grid.NearestFree(g.S.Stand(g.S.RivalLookout) + new Pt(800, 0)) ?? g.GetawaySeat); // out early, so the job is done before the shift comes out at 5:10
                r.Until(() => LookoutAtPost(g), T(330));
                r.Do(Tech, "reroute", "patrol");
                var behind = g.S.Grid.NearestFree(g.S.Stand(g.S.RivalLookout) + new Pt(250, 0)) ?? g.S.Stand(g.S.RivalLookout);
                r.To(Math.Max(g.Tick, r.SendTo(Enf, behind)));
                if (!KoFromBehind(r, "rival-4")) return;
                r.Is(Lk(r).Gun.Down, "he is down");
                r.Body(Enf, "restrain", "rival-4"); r.Done(Enf);
                r.Is(Lk(r).Gun.Down && g.CrewOf("rival-4")!.Mode != CrewMode.Gone, "knocked out from his blind side and tied: down for the crew's break (CrewBreakCheck counts him)");
                r.Until(() => g.Log.Exists(l => l.Code == "patrol.rerouted") || g.Over, T(40));
                r.Body(Enf, "carry", "rival-4"); r.Done(Enf);
                r.To(Math.Max(g.Tick, r.SendTo(Enf, g.S.Stashes.Find(x => x.Id == "stash/office-service-door/door")!.Stand)));
                r.Body(Enf, "stash", "stash/office-service-door/door"); r.Done(Enf);
                r.Line(g.Trail == t0 + g.R.RerouteTrail && g.Alert == 0 && g.Log.Exists(l => l.Code == "stash.door"), $"seed 0, this choreography: stashed {g.Log.Exists(l => l.Code == "stash.door")}, {g.Trail - t0} T (the Reroute's own {g.R.RerouteTrail}), Alert {g.Alert}: the Office's staff on Apron Road and the patrol's sweep decide who sees the carry (Waymark's plan times it)");
            }, Turned(Driver));

            Add("096-RIV-12", "096", "Motive RIV-12: the lookout knocked out and left untied: he wakes after wake-s and walks back to his post.", r =>
            {
                var g = r.G; r.Until(() => LookoutAtPost(g), T(330));
                var behind = g.S.Grid.NearestFree(g.S.Stand(g.S.RivalLookout) + new Pt(250, 0)) ?? g.S.Stand(g.S.RivalLookout);
                r.To(Math.Max(g.Tick, r.SendTo(Enf, behind)));
                if (!KoFromBehind(r, "rival-4")) return; int down = g.Tick;
                r.SendTo(Enf, g.S.Grid.NearestFree(behind + new Pt(1500, 0)) ?? behind);
                r.Until(() => !Lk(r).Gun.Down, T(g.R.WakeS + 30));
                r.Is(!Lk(r).Gun.Down && g.Tick - down >= T(g.R.WakeS) - T(3), Iv($"he wakes at {Report.Clock(g.Tick)}, {(g.Tick - down) / 10} s after"));
                r.Until(() => LookoutAtPost(g), T(60));
                r.Is(LookoutAtPost(g), "and walks back to his post");
            }, KeepVale(Turned(Driver)));

            Add("096-RIV-13", "096", "Motive RIV-13: the driver turned before the mission: no rivals.warned; the lookout walks to his post and stays there until Popovic's car leaves.", r =>
            {
                var g = r.G; r.Until(() => LookoutAtPost(g), T(330)); int posted = g.Tick; bool left = false;
                while (!g.Over && g.Tick < T(352)) { g.Step(); if (!LookoutAtPost(g)) left = true; }
                r.Is(!left && !g.Log.Exists(l => l.Code == "rivals.warned"), Iv($"posted from {Report.Clock(posted)}, never warned off (F4 is retired)"));
            }, Turned(Driver));

            Add("096-RIV-14", "096", "Motive RIV-14: a posted Mercenary (rival-2, test only) paid at his spot: he walks off; the Take at the bay; the fight has four.", r =>
            {
                var g = r.G; r.Until(() => g.Crew[1].Mode == CrewMode.Posted, T(330));
                var at = g.MemberPos(g.Crew[1], g.Tick); r.To(Math.Max(g.Tick, r.SendTo(Fixer, g.S.Grid.NearestFree(at + new Pt(0, 200)) ?? at)));
                r.Do(Fixer, "turn", "rival-2", "pay"); r.Done(Fixer);
                r.Is(g.CrewPaid("rival-2"), "he takes it and walks off");
                DealLookout(r, "mislead", 300);
                if (!r.Is(TakeAtBay(r), Iv($"the Take at the bay ({g.Result} {g.FailCode})"))) return;
                r.Eq(string.Join(",", RivalBodies(g)), "rival-1,rival-3,rival-4,rival-5", "four in the fight");
            }, Temper(Turned(Driver), "rival-2", "mercenary"));

            Add("096-RIV-15", "096", "Motive RIV-15: a posted man misled (5:40: 'your lookout wants you') is on his way when the fight starts: he joins from wherever he is.", r =>
            {
                var g = r.G; DealLookout(r, "mislead", 300);
                r.Until(() => g.Crew[0].Mode == CrewMode.Posted, T(30));
                var at = g.MemberPos(g.Crew[0], g.Tick); r.To(Math.Max(g.Tick, r.SendTo(Scout, g.S.Grid.NearestFree(at + new Pt(0, 200)) ?? at)));
                r.To(Math.Max(g.Tick, T(340) - T(g.R.MisleadS))); r.Do(Scout, "mislead", "rival-1"); r.Done(Scout);
                r.Is(g.Crew[0].Misled && g.Crew[0].Goal == "misled", "he walks off to the lookout's post");
                if (!r.Is(TakeAtBay(r), Iv($"the Take at the bay ({g.Result} {g.FailCode})"))) return;
                var h = g.Crew[0].H; var spot = g.S.Stand(g.S.Stands[0].Ambush[g.Crew[0].Spot]);
                r.Is(g.Crew[0].Fight && h != null && h.Run != null && !G.Within(h.Run.Points[0], spot, 1000), Iv($"he joins the fight from {h?.Run?.Points[0]}, not his spot"));
            }, Turned(Driver));

            Rivals096b();
        }
    }
}
