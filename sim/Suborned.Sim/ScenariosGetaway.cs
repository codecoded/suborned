// The getaway (brief 018, Motive; Sky 2026-10-18: "I want to get my team into the car, and get away"). On the real block, the promoted
// cast and the crowd, at the mission's own security level unless a case says otherwise.
//  N111 the hold and Go refused: the Scout boards empty-handed (1 s) before the ambush; one rival goes for the road; the ambush stops
//       hold nothing by standing; Go is refused car-held, naming the holders in id order (the Go card reads the same); in the car she
//       doesn't fire, and any shot at her is at one beside cover (×500)
//  N112 left behind, downed and fighting (§3 rows 1, 2): the guard downs the Scout; the Fixer boards and the car goes without Vale
//       (Withdrawn): the Scout captured downed, the Tech and the Enforcer captured fighting; custody holds 2 and 1 missions
//  N113 §4's twin of N95: route A, board and go with the crew unbroken (Board with Vale 4 s): the fight moves Standing 0; the three left
//       are captured fighting and the captures move nothing; the blocker is not replaced
//  N114 left behind, walked (§3 row 4): route A fought out (the crew broken), the car goes before any seal: the three walk out
//  N115 left behind, sealed (§3 row 3), and stepping out: N68's route G-kill without the final fight (the dead guard read by the camera:
//       the seal clock runs, nobody fights); the Fixer boards, steps out and back in; Go (Withdrawn): the three captured sealed
//  N116 seen leaving: the response unit passes the bay with the car in its cone; Go: +1 T, the car hunted, the car-seen line
//  N118 the menu's Withdraw stays a free abort: N112's fixture withdrawn from the menu captures nobody
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        const int Never = int.MaxValue / 4;
        /// <summary>East of the getaway bay, 14 m off, within 30 m of the patrol's sweep at its south end (patrol-a).</summary>
        static readonly Pt EastOfBay = new Pt(2200, -4500);

        /// <summary>Route A's Enforcer draws and walks Vale to the bay until the ambush springs.</summary>
        static void ToAmbush(Run r)
        {
            var g = r.G;
            r.Do(Enf, "draw");
            r.Send(Enf, "d:Getaway bay");
            while (!g.Over && g.FightSprung == Never)
            {
                if (supportTech && r.Op(Tech).Busy == null) { supportTech = false; r.Send(Tech, "crew:tech"); r.Do(Tech, "draw"); }
                g.Step();
            }
            supportTech = false;
        }
        /// <summary>The Enforcer walks the rest of the way to the car and boards with Vale; returns the ticks the Board took.</summary>
        static int BoardVale(Run r)
        {
            var g = r.G; var car = r.At("d:Getaway bay");
            if (!g.AtGetaway(r.Op(Enf).Pos)) { int at = r.SendTo(Enf, r.Stand("d:Getaway bay")); while (!g.Over && g.Tick < at) g.Step(); }
            r.Do(Enf, "board", "car"); int took = r.Op(Enf).BusyEnd - g.Tick; r.Done(Enf); if (!g.Over) g.Step();
            return took;
        }
        /// <summary>The Scout, the Fixer and the Tech walk to their start points by the car (crew:*) and draw: they will draw the ambush's fire.</summary>
        static void Support(Run r)
        {
            var g = r.G; supportTech = true;
            r.Send(Scout, "crew:scout"); r.Send(Fixer, "crew:fixer"); r.Do(Scout, "draw"); r.Do(Fixer, "draw"); // no waiting: the Enforcer goes at once (a man held still is seen)
        }
        [ThreadStatic] static bool supportTech; // the Tech follows to crew:tech once his reroute lands (ToAmbush)
        /// <summary>The hostiles up and present, optionally sparing the blocker.</summary>
        static List<string> Targets(Game g, bool spareBlocker) { var l = new List<string>(); foreach (var h in g.Hostiles) if (h.Present && !h.Down && h.Gun.St != "break" && h.Gun.St != "gone" && !(spareBlocker && h.Blocker)) l.Add(h.Id); return l; }
        /// <summary>The Enforcer walks Vale into the car while the three by the car fire on the ambush from their posts (the blocker spared or not);
        /// returns the ticks the Board took.</summary>
        static int CoveredBoarding(Run r, bool spareBlocker)
        {
            var g = r.G; string[] sup = { Scout, Fixer, Tech };
            int at = r.SendTo(Enf, r.Stand("d:Getaway bay"));
            r.Engage(() => Targets(g, spareBlocker), () => g.Tick >= at, 60, sup, false, advance: false);
            if (g.Over) return -1;
            r.Do(Enf, "board", "car"); int took = r.Op(Enf).BusyEnd - g.Tick;
            r.Engage(() => Targets(g, spareBlocker), () => r.Op(Enf).InCar, 10, sup, false, advance: false);
            return took;
        }
        /// <summary>The Enforcer walks Vale into the car while the three by the car fire on the blocker alone, from their posts.</summary>
        static void BlockerOnly(Run r)
        {
            var g = r.G; string[] sup = { Scout, Fixer, Tech }; var b = g.Hostiles.Find(h => h.Blocker);
            List<string> t() => b == null || b.Down || b.Gun.St == "break" || b.Gun.St == "gone" ? new List<string>() : new List<string> { b.Id };
            int at = r.SendTo(Enf, r.Stand("d:Getaway bay"));
            r.Engage(t, () => g.Tick >= at, 60, sup, false, advance: false);
            if (g.Over) return;
            r.Do(Enf, "board", "car");
            r.Engage(t, () => r.Op(Enf).InCar, 10, sup, false, advance: false);
            r.Engage(t, () => t().Count == 0, 60, sup, false, advance: true);
        }
        /// <summary>Board and go, covered (brief 018 §5): the Enforcer walks Vale straight to the car while the three by it fire on the blocker
        /// alone, then on whoever is up to draw the fire off him (rivals shoot whoever last shot at them), until he is in with Vale; then they hold.</summary>
        static void CoveredBoardAndGo(Run r, bool go, bool draw = true, Pt? door = null)
        {
            var g = r.G; string[] sup = { Scout, Fixer, Tech };
            int at = r.SendTo(Enf, door ?? r.Stand("d:Getaway bay"));
            DownBlocker(r, sup);
            if (!draw) foreach (var op in sup) { g.Issue(new Order { Tick = g.Tick, Op = op, Do = "cease" }); g.Issue(new Order { Tick = g.Tick, Op = op, Do = "stance", Stance = "hold" }); }
            if (draw) r.Engage(() => Targets(g, false), () => g.Tick >= at, 60, sup, false, advance: false);
            else DrawFire(r, () => g.Tick >= at, 60);
            if (g.Over) return;
            r.Do(Enf, "board", "car");
            if (draw) r.Engage(() => Targets(g, false), () => r.Op(Enf).InCar, 10, sup, false, advance: false);
            else DrawFire(r, () => r.Op(Enf).InCar, 10);
            foreach (var op in sup) { g.Issue(new Order { Tick = g.Tick, Op = op, Do = "cease" }); g.Issue(new Order { Tick = g.Tick, Op = op, Do = "stance", Stance = "hold" }); }
            if (go && !g.Over) r.Is(r.Do("cell", "go"), "Go");
        }
        /// <summary>Draw the fire off the Enforcer (brief 018 §5: rivals shoot whoever last shot at them) without breaking the crew: each of the
        /// three by the car fires one shot at a rival aiming at him, ceases, and fires again only when that rival's grudge (weapons.lineLapse) is
        /// about to lapse. Runs until `until` or the run ends.</summary>
        static void DrawFire(Run r, Func<bool> until, int limitS)
        {
            var g = r.G; string[] sup = { Scout, Fixer, Tech }; var last = new Dictionary<string, int>(); int seen = g.Log.Count, end = g.Tick + T(limitS);
            while (!until() && !g.Over && g.Tick < end)
            {
                for (; seen < g.Log.Count; seen++)
                {
                    var l = g.Log[seen]; if (l.Code != "shot.hit" && l.Code != "shot.miss") continue;
                    foreach (var s in sup) if (l.Text.StartsWith(s + " fires on ", StringComparison.Ordinal)) { last[s] = l.Tick; g.Issue(new Order { Tick = g.Tick, Op = s, Do = "cease" }); }
                }
                foreach (var s in sup)
                {
                    var o = r.Op(s); if (o.Down || o.InCar || o.Busy != null || o.Gun.W == null || o.Gun.Order.Length > 0) continue;
                    if (last.TryGetValue(s, out var t0) && g.Tick - t0 < T(g.R.LineLapseS) - 5) continue;
                    string best = ""; long bd = long.MaxValue;
                    foreach (var h in g.Hostiles)
                    {
                        if (!h.Present || h.Down || h.Gun.St == "break" || h.Gun.St == "gone" || h.Gun.Target != Enf) continue;
                        var p = h.Pos(g.Tick); long d = G.D2(o.Pos, p);
                        if (d < bd && d <= (long)o.Gun.W.MaxCm * o.Gun.W.MaxCm && g.S.Sight(o.Pos, p, g.R.Eye)) { bd = d; best = h.Id; }
                    }
                    if (best.Length > 0) g.Issue(new Order { Tick = g.Tick, Op = s, Do = "fire", Target = best });
                }
                g.Step();
            }
        }
        /// <summary>Fire on the blocker alone until it is down, broken or gone.</summary>
        static void DownBlocker(Run r, params string[] ops)
        {
            var g = r.G; var b = g.Hostiles.Find(h => h.Blocker);
            if (b == null) return;
            r.Engage(() => b.Down || b.Gun.St == "break" || b.Gun.St == "gone" ? new List<string>() : new List<string> { b.Id }, () => b.Down || b.Gun.St == "break" || b.Gun.St == "gone", 120, ops, true);
        }
        static Order Go(Game g) => new Order { Tick = g.Tick, Op = "cell", Do = "go" };
        static (string op, string fate, string why) FateOf(Game g, string op) => g.LeftBehindAtGo.Find(x => x.op == op);
        /// <summary>Shots in the log at a target after a tick: (shooter, metres, per mille).</summary>
        static List<(string by, int m, int pm)> ShotsAt(Game g, string target, int after)
        {
            var l = new List<(string, int, int)>();
            foreach (var x in g.Log)
            {
                if (x.Tick <= after || (x.Code != "shot.hit" && x.Code != "shot.miss")) continue;
                int i = x.Text.IndexOf(" fires on " + target + " at ", StringComparison.Ordinal); if (i < 0) continue;
                var rest = x.Text.Substring(i + (" fires on " + target + " at ").Length);
                int m = int.Parse(rest.Substring(0, rest.IndexOf(' ')), CultureInfo.InvariantCulture);
                int p0 = rest.IndexOf('(') + 1; int pm = int.Parse(rest.Substring(p0, rest.IndexOf(' ', p0) - p0), CultureInfo.InvariantCulture);
                l.Add((x.Text.Substring(0, i), m, pm));
            }
            return l;
        }

        static void GetawayCases()
        {

            Add("N112", "—", "Left behind, downed and fighting (brief 018 §3 rows 1 and 2), and Go without Vale: the guard downs the Scout in front of the Office (N26's fixture); the Fixer boards the empty car and the car goes. The outcome is Withdrawn and the left-behind rules apply: the Scout is captured downed; the Tech and the Enforcer, standing while the guard still fights, are captured fighting. Custody (campaign.custody, founding cell bravery 2, loyalty 3): downed holds 2 missions (talks at the third questioning, 3 + 0 > 2 + 0), abandoned holds 1 (2 + 1 > 2 + 0); +4 T each to the campaign; a captive gives every face, the car and place and time.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Scout, FrontOfGuard(r))));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Do(Scout, "draw"); g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "stance", Stance = "hold" });
                int at = r.Send(Fixer, "d:Getaway bay");
                r.Is(r.Until(() => r.Op(Scout).Down, T(30)), "the guard downs the Scout");
                r.To(Math.Max(g.Tick, at));
                r.Is(g.LeftBehind().Contains((Scout, "captured", "downed")), "the Go card: the Scout would be captured, downed");
                r.Do(Fixer, "board", "car"); r.Eq(r.Op(Fixer).BusyEnd - g.Tick, T(g.R.BoardS), "boarding empty-handed: 1 s"); r.Done(Fixer); g.Step();
                r.Is(r.Op(Fixer).InCar && g.Vale != ValeAt.Car, "the Fixer is in the car, without Vale");
                var lb = g.LeftBehind();
                var a = g.Issue(Go(g));
                r.Is(a.ok, $"Go is taken: nothing holds the car ({a.why})");
                r.Eq(g.Result, Outcome.Withdrawn, "Go without Vale: the cell withdraws by car");
                r.Eq(g.LeftBehindAtGo.Count, 3, "everyone not in the car is left behind");
                r.Is(g.LeftBehindAtGo.SequenceEqual(lb), "as the Go card said");
                r.Eq(FateOf(g, Scout), (Scout, "captured", "downed"), "downed, not in the car: captured");
                r.Eq(FateOf(g, Tech), (Tech, "captured", "fighting"), "standing while the guard still fights: captured");
                r.Eq(FateOf(g, Enf), (Enf, "captured", "fighting"), "the Enforcer too");
                r.Eq(string.Join(",", g.Captured), "scout,tech,enforcer", "captures only at Go, in the cell's order");
                var d = Debrief.Of(g); var next = d.Next;
                var sc = next.Captives.Find(c => c.Op == Scout); var te = next.Captives.Find(c => c.Op == Tech);
                if (!r.Is(sc != null && te != null && next.Captives.Count == 3, "three captives in the debrief")) return;
                r.Eq((sc!.Why, sc.Holds, sc.TalksAt, sc.Sc[Rules.Fear], sc.Sc[Rules.Resentment], sc.Sc[Rules.Bravery], sc.Sc[Rules.Loyalty]), ("downed", 2, 3, 3, 0, 2, 0), "downed: holds 2 missions, talks at the third (3 + 0 > 2 + 0)");
                r.Eq((te!.Why, te.Holds, te.TalksAt, te.Sc[Rules.Fear], te.Sc[Rules.Resentment], te.Sc[Rules.Bravery], te.Sc[Rules.Loyalty]), ("fighting", 1, 2, 2, 1, 2, 0), "abandoned: holds 1 mission, talks at the second (2 + 1 > 2 + 0)");
                r.Is(sc.HeldAt.Length > 0 && sc.RescueUntil == g.R.CustodyMissions, $"held at {sc.HeldAt}; a rescue for {sc.RescueUntil} missions");
                r.Is(sc.Facts.Take(6).SequenceEqual(new[] { "cell-face:scout", "cell-face:fixer", "cell-face:tech", "cell-face:enforcer", "cell-car", "place-time" }), $"a captive gives every face, the car, place and time ({string.Join(", ", sc.Facts)})");
                r.Is(next.CampaignTrail >= 3 * g.R.CapturedTrail, $"+4 T each to the campaign ({next.CampaignTrail})");
                r.Eq(next.Lines.Count(l => l.Line == "left-behind"), 3, "a left-behind line each");
                r.Eq(next.Lines.Count(l => l.Line == "captive"), 3, "a captive line each");
                r.Eq(d.LeftBehind.Count, 3, "the debrief carries who was left");
                var fc = CauseIs(r, "withdraw", "car-left-without-vale", false, "getaway-car", "car", ""); // the fail camera (ScenariosFailCause.cs): the car that left without him
                r.Is(fc != null && fc.Culprits[0].At == r.At(g.S.GetawayCar) && d.Cause == fc, "at the getaway bay; the debrief carries the same cause");
            }, new Start { OneInput = false }); // brief 039: the old controls (stance hold)


            Add("N115", "—", "Left behind, sealed (brief 018 §3 row 3), and stepping out (§2): N68's route G-kill without the final fight (the dead guard left at his post; the entrance camera reads him when the Tech leaves the cabinet: Alert 2, the response called, the seal clock running). Nobody has fought the cell. The Fixer walks to the car and boards (1 s), steps out at the car's side (1 s) and boards again; Go before the seal (Withdrawn: no Vale) leaves the Scout, the Tech and the Enforcer (inside with Vale) standing under the seal: captured sealed.", r =>
            {
                var g = r.G;
                Orders(r, new Order { Tick = 0, Op = Tech, Do = "move", To = "cabinet", Run = true }, MvTo(0, Enf, BesideDoor), Mv(0, Scout, "d:Office approach"), Mv(0, Fixer, "crew:fixer"),
                    Act(300, Tech, "loop", "entrance") /* Loop v2 (brief 058 §4): planted at 0:30 so its 2:00 runs past the Enter */, Act(462, Scout, "watch", "guard"), Mv(662, Scout, "crew:scout"), Act(1060, Tech, "reroute", "patrol"), // c022's route G-kill timings (N68)
                    MvTo(1440, Enf, BesideGuard), Act(1510, Enf, "takedown", "guard", "kill"), Act(1540, Enf, "enter"),
                    MvTo(1540, Tech, new Pt(675, -4625)), MvTo(1540, Scout, new Pt(1175, -4825)), Mv(1540, Fixer, "d:Getaway bay"));
                r.Is(r.Until(() => g.Lockdown != Never, T(120)) && !g.Over, $"the seal clock runs ({Report.Clock(g.Tick)}; seal {Report.Clock(g.Lockdown)})");
                r.Until(() => r.Op(Fixer).Walk == null, T(60));
                r.Do(Fixer, "board", "car"); r.Done(Fixer); g.Step();
                var a = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "move", To = "d:Getaway bay" });
                r.Is(a.ok && r.Op(Fixer).BusyEnd - g.Tick == T(g.R.BoardS), $"a move on a boarded operative: 1 s to step out ({a.why})");
                r.Done(Fixer); g.Step();
                r.Is(!r.Op(Fixer).InCar && r.Op(Fixer).Pos == g.Walk.NearestFree(r.Stand("d:Getaway bay")), "out at the car's side (engine 0.33.1: the free cell nearest its seat, off its ground)");
                r.Do(Fixer, "board", "car"); r.Eq(r.Op(Fixer).BusyEnd - g.Tick, T(g.R.BoardS), "back in: 1 s"); r.Done(Fixer); g.Step();
                r.Is(g.Lockdown != Never && g.Tick < g.Lockdown && !g.Over, "the seal clock runs, not yet sealed");
                r.Is(r.Do("cell", "go"), "Go before the seal");
                r.Eq(g.Result, Outcome.Withdrawn, "no Vale: the cell withdraws by car");
                r.Is(g.LeftBehindAtGo.Count == 3 && g.LeftBehindAtGo.TrueForAll(x => x.fate == "captured" && x.why == "sealed"), $"nobody fights, but the seal clock runs: captured, sealed ({string.Join("; ", g.LeftBehindAtGo)})");
                r.Is(Debrief.Of(g).Next.Captives.TrueForAll(c => c.Why == "sealed" && c.Holds == 1), "left standing: abandoned, each holds 1 mission");
            }, NoCrew(NoFight()));

            Add("N116", "—", "Seen leaving (brief 018 §3; Sky 2026-10-18 answer 5), at level 1: the Fixer waits in the car while the Enforcer shoots the guard at the door; the response unit comes in from the north street and passes the bay with the car in its officers' cone. Go then (no Vale: Withdrawn) makes the departure one witnessed incident, +1 T (witness-trail), and the investigation hunts the car (cell-car); the debrief's car-seen line. The officers on the street are not holders: they stand outside 8 m and the lane. (On this block a loud final fight's officers hold stands the car is hidden from, or 19.4 m off, past security's 19 m: Keel's probe.)", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor), r.Send(Fixer, "d:Getaway bay")));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech); r.Do(Tech, "loop", "entrance"); r.Done(Tech); /* Loop v2 (engine 0.31.0, brief 058 §4): the 2:00 loop is planted after the Reroute */
                r.Do(Fixer, "board", "car"); r.Done(Fixer);
                r.Fire(Enf, GuardR); r.Until(() => g.GuardDown, T(30));
                r.Is(r.Until(() => g.CarWatcher().Length > 0, T(120)), $"the response unit has the car in sight ({Report.Clock(g.Tick)}: {g.CarWatcher()})");
                var watcher = g.CarWatcher();
                r.Is(watcher.StartsWith("security-", StringComparison.Ordinal) && !g.CarHeld(), "a response officer, and nothing holds the car");
                int trail = g.Trail;
                r.Is(r.Do("cell", "go"), "Go");
                r.Eq((g.Result, g.CarSeenBy, g.Trail - trail), (Outcome.Withdrawn, watcher, g.R.WitnessTrail), "seen leaving: +1 T");
                r.Is(g.Ledger.Exists(x => x.what == "trail" && x.cause == "car-seen:" + watcher), "the ledger names it");
                var d = Debrief.Of(g);
                r.Is(d.Inv.Hunted.Contains("cell-car"), "the investigation hunts the car");
                r.Is(d.Next.Lines.Exists(l => l.Line == "car-seen" && l.Who == watcher), "the debrief's car-seen line");
            }, NoCrew(Level(1)));

            Add("N118", "—", "The menu's Withdraw stays a free abort (Sky 2026-10-18 answer 4): N112's fixture, the Scout downed, withdrawn from the menu instead of by car: nobody is captured, nobody is left behind.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Scout, FrontOfGuard(r))));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Do(Scout, "draw"); g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "stance", Stance = "hold" });
                r.Is(r.Until(() => r.Op(Scout).Down, T(30)), "the guard downs the Scout");
                r.Is(g.Do(new Order { Tick = g.Tick, Op = "cell", Do = "withdraw" }), "withdraw");
                r.Eq((g.Result, g.Captured.Count, g.LeftBehindAtGo.Count), (Outcome.Withdrawn, 0, 0), "a free abort: nobody captured");
            }, new Start { OneInput = false }); // brief 039: the old controls (stance hold)
        }

        /// <summary>N10's loud route to the car: the guard shot, the Enforcer in and out with Vale, the cell drawn, the Enforcer walking Vale to the car.</summary>
        static void LoudToCar(Run r)
        {
            var g = r.G;
            r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
            r.Do(Tech, "reroute", "patrol"); r.Done(Tech); r.Do(Tech, "loop", "entrance"); r.Done(Tech); /* Loop v2 (engine 0.31.0, brief 058 §4): the 2:00 loop is planted after the Reroute */
            int mark = g.NextMarkOf(g.GuardP, g.Tick); if (mark - g.Tick < T(30)) r.To(mark + 1);
            r.Fire(Enf, GuardR); r.Until(() => g.GuardDown, T(30));
            r.To(r.Send(Enf, "d:Office entrance"));
            r.Do(Enf, "enter"); r.Done(Enf);
            r.Eq(g.FightBranch, "loud", "a loud final fight");
            foreach (var op in new[] { Scout, Fixer, Tech }) r.Do(op, "draw");
            r.Send(Enf, "d:Getaway bay");
        }
    }
}
