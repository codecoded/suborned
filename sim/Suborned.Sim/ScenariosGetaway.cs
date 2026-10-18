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
            if (!G.Within(r.Op(Enf).Pos, car, g.R.AtReachCm)) { int at = r.SendTo(Enf, r.Stand("d:Getaway bay")); while (!g.Over && g.Tick < at) g.Step(); }
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
        static void CoveredBoardAndGo(Run r, bool go, bool draw = true)
        {
            var g = r.G; string[] sup = { Scout, Fixer, Tech };
            int at = r.SendTo(Enf, r.Stand("d:Getaway bay"));
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
            Add("N111", "—", "The hold and Go refused (brief 018 §1, §2): route A, quiet. The Scout walks to the car and boards empty-handed (1 s) before the ambush. When it springs one rival (fight.blockers) goes for the road, to a point in the exit lane (site getaway.exit); the other ambush stops, 12–19 m off, hold nothing by standing there. Go (someone is in the car) is refused car-held while the blocker stands in the lane, and the refusal names every holder in id order; the Go card (GoQuery) reads the same. In the car the Scout doesn't fire, and any shot at her there is at one beside cover (×500).", r =>
            {
                var g = r.G; var car = r.At("d:Getaway bay");
                RouteA(r, reroute: true);
                int at = r.Send(Scout, "d:Getaway bay"); r.Send(Fixer, "crew:fixer"); r.Do(Fixer, "draw"); r.Do(Scout, "draw");
                r.Do(Enf, "draw"); r.Send(Enf, "d:Getaway bay");
                int boarded = -1;
                while (!g.Over && g.FightSprung == Never)
                {
                    if (boarded < 0 && g.Tick >= at && r.Op(Scout).Busy == null) { r.Do(Scout, "board", "car"); boarded = r.Op(Scout).BusyEnd - g.Tick; }
                    g.Step();
                }
                r.Eq(boarded, T(g.R.BoardS), "the Scout boards empty-handed: 1 s");
                r.Is(r.Op(Scout).InCar && g.Vale != ValeAt.Car, "the Scout is in the car before the ambush, without Vale");
                r.Eq(g.Blockers.Count, g.R.Blockers, "one hostile goes for the road (fight.blockers)");
                var b = g.Hostiles.Find(h => h.Blocker);
                if (!r.Is(b != null, "the blocker is one of the ambush")) return;
                r.Is(b!.Id.StartsWith("rival-", StringComparison.Ordinal) && g.S.InExitLane(b.Home), $"the blocker ({b.Id}) makes for a point in the exit lane");
                r.Is(!G.Within(b.Home, car, g.R.BlockNearCm - 1) && G.Within(b.Home, car, g.R.BlockFarCm), $"its post is in the 10–13 m ring from the car, not at the door ({b.Gun.Cover}, {G.Dist(b.Home, car) / 100} m; brief 018 §10)");
                bool holdsWhere(string id) { var h = g.Hostiles.Find(x => x.Id == id); var p = h != null ? h.Pos(g.Tick) : g.PosOf(id)!.Value; return G.Within(p, car, g.R.HoldCm) || g.S.InExitLane(p); }
                r.Is(g.CarHolders().TrueForAll(holdsWhere), "every holder stands within 8 m of the car or in the lane");
                r.Is(g.Hostiles.Exists(h => !h.Blocker && !h.Down && !g.CarHolders().Contains(h.Id)), "the ambush stops hold nothing by standing there");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "fire", Target = "hostiles" }).code, "fire-carrying-vale", "walking Vale, the Enforcer doesn't fire (brief 018, Waymark c018)");
                r.Engage(() => Targets(g, true), () => g.CarHolders().Contains(b.Id), 30, new[] { Enf, Fixer }, false, advance: false); // the rest fight on, sparing the blocker
                r.Is(!r.Shots().Exists(x => x.by == Enf), "and fires nothing while he holds her");
                r.Is(g.CarHolders().Contains(b.Id) && !g.Over, $"the blocker in the lane holds the car ({Report.Clock(g.Tick)})");
                var a = g.Issue(Go(g));
                r.Eq((a.ok, a.code), (false, "car-held"), "Go is refused while the car is held");
                r.Eq(a.ids, string.Join(",", g.CarHolders()), "the refusal names the holders, in id order");
                r.Is(a.ids.Split(',').Contains(b.Id), $"the blocker is among them ({a.ids})");
                var q = g.GoQuery();
                r.Eq((q.ok, q.code, q.ids), (a.ok, a.code, a.ids), "the Go card reads the same");
                r.Refused(Scout, "fire", "hostiles", "in the car the Scout doesn't fire");
                int from = g.Tick; r.Wait(T(20));
                r.Is(!r.Shots().Exists(x => x.tick > from && x.by == Scout), "she fires nothing from the car");
                var shots = ShotsAt(g, Scout, from);
                bool cover(int m, int pm) { for (int d = m * 100; d < m * 100 + 100; d++) if (g.R.Accuracy(g.R.Pistol, d, false, g.R.ModCover, false, true) == pm) return true; return false; }
                r.Is(shots.TrueForAll(x => cover(x.m, x.pm)), $"any shot at her in the car is at one beside cover, ×500 ({shots.Count} shots: {string.Join(", ", shots.Select(x => x.pm))} ‰)");
            });

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
            });

            Add("N113", "—", "§4's twin of N95 (brief 018 §4): route A, quiet, the feared rivals' ambush; the three by the car down the blocker and draw the fire while the Enforcer walks Vale in, and the car goes with the crew unbroken. The final fight is judged when the car leaves: public, but no outcome holds (driven off needs the crew broken): Standing 0 from the fight, cause none; Inez's word still adds +1 between missions (+2 → +3). The three left by the car are captured fighting: the captures move nothing. The blocker is not replaced.", r =>
            {
                var g = r.G;
                r.Eq(g.RivalsPublic, "menace", "the Handover's rivals are feared");
                RouteA(r, reroute: true);
                Support(r);
                ToAmbush(r);
                var b = g.Hostiles.Find(h => h.Blocker)!;
                CoveredBoardAndGo(r, go: false, draw: false); // the three by the car down the blocker and draw the fire while the Enforcer boards
                r.Is(!g.CarHeld() && (b.Down || b.Gun.St == "break" || b.Gun.St == "gone"), $"the car is free: the blocker {b.Id} is down");
                r.Is(g.Hostiles.Exists(h => h.Present && !h.Down && h.Gun.St != "break" && h.Gun.St != "gone"), "the crew is not broken");
                r.Eq(g.Hostiles.Count(h => h.Blocker), 1, "the job of the road did not pass on");
                var bo = g.Log.FindAll(l => l.Code == "blocker.out"); var cf = g.Log.FindAll(l => l.Code == "car.free");
                r.Is(bo.Count == 1 && cf.Count >= 1 && cf[0].Tick >= bo[0].Tick, $"logged by code: blocker.out, then car.free ({string.Join(", ", bo.Concat(cf).Select(l => l.Code + "@" + Report.Clock(l.Tick)))})");
                r.Is(r.Do("cell", "go"), "Go");
                r.Eq(g.Result, Outcome.Success, "Vale in the car when it leaves: the win");
                if (!r.Is(g.Fights.Count == 1, $"one fight: the final fight ({g.Fights.Count})")) return;
                var f = g.Fights[0];
                r.Eq((f.Final, f.Public, f.Outcome, f.Delta), (true, true, "none", 0), "judged when the car leaves: the crew unbroken, Standing 0");
                r.Eq(f.End, g.CarLeftTick, "it ends with the mission, at the tick the car leaves");
                r.Eq(FightMoves(g).Count, 0, "no move from the fight");
                r.Is(g.LeftBehindAtGo.Count == 3 && g.LeftBehindAtGo.TrueForAll(x => x.fate == "captured" && x.why == "fighting"), "the rest, left while the crew fights on, are captured fighting");
                r.Eq(g.StandingMoves.Count, 0, "the captures move nothing");
                var next = g.BetweenMissions(g.Investigate());
                var l = OneLine(r, next);
                r.Eq((l.From, l.End, l.Word, l.To), (2, 2, 1, 3), "2 + 0 the fight + 1 Inez's word = +3 (Motive's board-and-go row)");
                r.Eq(next.Captives.Count, 3, "three captives");
                r.Is(next.Captives.TrueForAll(c => c.Holds == 1 && c.Facts.Contains("contact:" + g.Inez.Id)), "each holds 1 mission and would give Inez as a contact");
            });

            Add("N114", "—", "Left behind, walked (brief 018 §3 row 4): route A fought out until the crew breaks (FightItOutAll; with the blocker on the road, the car comes free before the crew breaks), the Enforcer boards with Vale and the car goes before the guard's missed check-in (the rivals downed him) calls security. The fight is over and no seal runs: the three left walk out, nobody is captured, no captive in the debrief. The fight +1 (the crew driven off).", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true);
                Support(r); ToAmbush(r); // brief 018: walking Vale the Enforcer doesn't fire; the three by the car fight
                r.FightItOutAll(120, Scout, Fixer, Tech); // brief 018 §10: the blocker on the road falls first, so fight on until the crew breaks
                r.Is(g.FightOverTick != Never, "the fight is over: the crew broken");
                BoardVale(r);
                r.Is(g.Lockdown == Never, "no seal clock");
                r.Is(r.Do("cell", "go"), "Go");
                r.Eq(g.Result, Outcome.Success, "the win");
                r.Is(g.LeftBehindAtGo.Count == 3 && g.LeftBehindAtGo.TrueForAll(x => x.fate == "walked" && x.why == "walked"), $"the fight over and no seal: the three left walk out ({string.Join("; ", g.LeftBehindAtGo)})");
                r.Eq(g.Captured.Count, 0, "nobody captured");
                var next = Debrief.Of(g).Next;
                r.Is(next.Captives.Count == 0 && next.Lines.Count(l => l.Line == "left-behind" && l.Detail == "walked (walked)") == 3, "no captive; three walked lines");
                r.Eq(FightMoves(g).Sum(m => m.delta), 1, "the crew driven off: +1");
            });

            Add("N115", "—", "Left behind, sealed (brief 018 §3 row 3), and stepping out (§2): N68's route G-kill without the final fight (the dead guard left at his post; the entrance camera reads him when the Tech leaves the cabinet: Alert 2, the response called, the seal clock running). Nobody has fought the cell. The Fixer walks to the car and boards (1 s), steps out at the car's side (1 s) and boards again; Go before the seal (Withdrawn: no Vale) leaves the Scout, the Tech and the Enforcer (inside with Vale) standing under the seal: captured sealed.", r =>
            {
                var g = r.G;
                Orders(r, new Order { Tick = 0, Op = Tech, Do = "move", To = "cabinet", Run = true }, MvTo(0, Enf, BesideDoor), Mv(0, Scout, "d:Office approach"), Mv(0, Fixer, "crew:fixer"),
                    Act(136, Tech, "loop", "entrance"), Act(355, Scout, "watch", "guard"), Mv(555, Scout, "crew:scout"), Act(1060, Tech, "reroute", "patrol"),
                    MvTo(1440, Enf, BesideGuard), Act(1510, Enf, "takedown", "guard", "kill"), Act(1540, Enf, "enter"),
                    MvTo(1540, Tech, new Pt(675, -4625)), MvTo(1540, Scout, new Pt(1175, -4825)), Mv(1540, Fixer, "d:Getaway bay"));
                r.Is(r.Until(() => g.Lockdown != Never, T(120)) && !g.Over, $"the seal clock runs ({Report.Clock(g.Tick)}; seal {Report.Clock(g.Lockdown)})");
                r.Until(() => r.Op(Fixer).Walk == null, T(60));
                r.Do(Fixer, "board", "car"); r.Done(Fixer); g.Step();
                var a = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "move", To = "d:Getaway bay" });
                r.Is(a.ok && r.Op(Fixer).BusyEnd - g.Tick == T(g.R.BoardS), $"a move on a boarded operative: 1 s to step out ({a.why})");
                r.Done(Fixer); g.Step();
                r.Is(!r.Op(Fixer).InCar && r.Op(Fixer).Pos == r.Stand("d:Getaway bay"), "out at the car's side");
                r.Do(Fixer, "board", "car"); r.Eq(r.Op(Fixer).BusyEnd - g.Tick, T(g.R.BoardS), "back in: 1 s"); r.Done(Fixer); g.Step();
                r.Is(g.Lockdown != Never && g.Tick < g.Lockdown && !g.Over, "the seal clock runs, not yet sealed");
                r.Is(r.Do("cell", "go"), "Go before the seal");
                r.Eq(g.Result, Outcome.Withdrawn, "no Vale: the cell withdraws by car");
                r.Is(g.LeftBehindAtGo.Count == 3 && g.LeftBehindAtGo.TrueForAll(x => x.fate == "captured" && x.why == "sealed"), $"nobody fights, but the seal clock runs: captured, sealed ({string.Join("; ", g.LeftBehindAtGo)})");
                r.Is(Debrief.Of(g).Next.Captives.TrueForAll(c => c.Why == "sealed" && c.Holds == 1), "left standing: abandoned, each holds 1 mission");
            }, NoFight());

            Add("N116", "—", "Seen leaving (brief 018 §3; Sky 2026-10-18 answer 5), at level 1: the Fixer waits in the car while the Enforcer shoots the guard at the door; the response unit comes in from the north street and passes the bay with the car in its officers' cone. Go then (no Vale: Withdrawn) makes the departure one witnessed incident, +1 T (witness-trail), and the investigation hunts the car (cell-car); the debrief's car-seen line. The officers on the street are not holders: they stand outside 8 m and the lane. (On this block a loud final fight's officers hold stands the car is hidden from, or 19.4 m off, past security's 19 m: Keel's probe.)", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor), r.Send(Fixer, "d:Getaway bay")));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
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
            }, Level(1));

            Add("N118", "—", "The menu's Withdraw stays a free abort (Sky 2026-10-18 answer 4): N112's fixture, the Scout downed, withdrawn from the menu instead of by car: nobody is captured, nobody is left behind.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Scout, FrontOfGuard(r))));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Do(Scout, "draw"); g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "stance", Stance = "hold" });
                r.Is(r.Until(() => r.Op(Scout).Down, T(30)), "the guard downs the Scout");
                r.Is(g.Do(new Order { Tick = g.Tick, Op = "cell", Do = "withdraw" }), "withdraw");
                r.Eq((g.Result, g.Captured.Count, g.LeftBehindAtGo.Count), (Outcome.Withdrawn, 0, 0), "a free abort: nobody captured");
            });
        }

        /// <summary>N10's loud route to the car: the guard shot, the Enforcer in and out with Vale, the cell drawn, the Enforcer walking Vale to the car.</summary>
        static void LoudToCar(Run r)
        {
            var g = r.G;
            r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
            r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
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
