// Engine 0.30.0: the seal's car state (brief 057 §4: FailCause.Car), the debrief's route lines true to the run (brief 060 #8, Suture:
// Debrief.Of claimed "Vale reached the getaway car" on every front, service and pickup route), the seal in Windows() (brief 057 §4) and
// the patrol's path as walked (brief 057 §2: the rerouted patrol walks, Game.PatrolLegs and PatrolRoute). The first three are reads (no hash
// moves for them); the walk is a rule (cell.reroute.walk) and moves every rerouted run's patrol from the Reroute's landing.
//  N183 empty: N128's loud fight, the Enforcer holding with Vale where he came out; sealed with Vale never aboard. The old debrief said
//       "vale-reaches-getaway-car" here (the scenario that fails 0.29.0's Debrief.Of).
//  N184 ready: N115's quiet G-kill without the final fight; the Enforcer boards with Vale, nobody gives Go; sealed with the car free.
//       The seal window from the first seal-clock tick to the seal, its origin the cause's.
//  N185 held: N111's ambush; the Enforcer boards with Vale while the blocker holds the lane, Go refused car-held. Engine 0.35.2 (Motive
//       rulings-0.35.1 §2): the Tech standing outside undrawn is no guard, so the rivals take Vale before the seal (rivals-reach-vale);
//       0.34.0 to 0.35.1 sealed here with the car held (FailCause.Car held, holders CarHolders()): that state has no play here now.
//  N186 the patrol's path: route A's reroute, the walk out, the wait, the walk back, then a chase: after every tick the robot stands on the
//       path read before it, and never steps more than its pace in a tick.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>Every route line of the run's debrief is backed by the run (brief 060 #8, Q-DEBRIEF's sim half): its chain, its log, or what
        /// held at the Enter or the Take. Returns the debrief.</summary>
        static Debrief RouteBacked(Run r)
        {
            var g = r.G; var d = Debrief.Of(g);
            bool log(string code) => g.Log.Exists(l => l.Code == code);
            bool chain(string c) => g.Chain.Contains(c);
            foreach (var line in d.Route)
            {
                bool ok = line switch
                {
                    "vale-reaches-getaway-car" => chain("vale-in-getaway-car") && log("board.in"),
                    "vale-taken-through-office" => log("enter.out"),
                    "vale-intercepted-outside" => chain("vale-taken-outside"),
                    "patrol-diverted" => chain("patrol-rerouted") && g.AtEntry != null && g.AtEntry.PatrolAway,
                    "entry-timed-between-sweeps" => g.AtEntry != null && g.AtEntry.PatrolSweeping,
                    "entrance-camera-looped" => log("loop.on") && g.AtEntry != null && g.AtEntry.Looped,
                    "waitress-pulls-guard-away" => g.LureTick >= 0,
                    "guard-killed" => g.Chain.Exists(c => c.StartsWith("killed:", StringComparison.Ordinal)),
                    "guard-knocked-out" => g.Chain.Exists(c => c.StartsWith("knocked-out:", StringComparison.Ordinal)),
                    "guard-shot-beside-waitress" or "guard-shot-away-from-waitress" => chain("guard-shot"),
                    "driver-moves-vale-to-service-lane" => g.PickupService,
                    // "pickup-camera-blocked" is retired with car.blocks and the driver's Call (brief 059, engine 0.33.0): the debrief never gives it
                    "rivals-follow-false-tip" => chain("rivals-false-tip"),
                    "rivals-driven-back" => chain("rivals-driven-back"),
                    _ => false,
                };
                r.Is(ok, $"the debrief's route line '{line}' is backed by the run");
            }
            return d;
        }

        static void SealCarCases()
        {
            Add("N183", "—", "The seal's car state, empty (brief 057 §4), and a debrief true to the run (brief 060 #8): N128's fixture (route careless-body-found: the guard killed at his post, the Enter at once, the loud final fight; the Enforcer holds with Vale where he came out). The district seals with Vale never aboard: FailCause.Car empty, no holders. The debrief no longer says Vale reached the getaway car (0.29.0's Debrief.Of did, on every front route); it says he was taken through the Office, and every line it keeps is backed by the run.", r =>
            {
                var g = r.G;
                Script(r, "[{\"tick\":0,\"op\":\"tech\",\"do\":\"move\",\"to\":\"cabinet\",\"run\":true},{\"tick\":0,\"op\":\"enforcer\",\"do\":\"move\",\"to\":[6925,-4250]},{\"tick\":0,\"op\":\"scout\",\"do\":\"move\",\"to\":\"d:Office approach\"},{\"tick\":0,\"op\":\"fixer\",\"do\":\"move\",\"to\":\"crew:fixer\"},{\"tick\":166,\"op\":\"tech\",\"do\":\"loop\",\"target\":\"entrance\"},{\"tick\":462,\"op\":\"scout\",\"do\":\"watch\",\"target\":\"guard\"},{\"tick\":662,\"op\":\"scout\",\"do\":\"move\",\"to\":\"crew:scout\"},{\"tick\":1060,\"op\":\"tech\",\"do\":\"reroute\",\"target\":\"patrol\"},{\"tick\":1440,\"op\":\"enforcer\",\"do\":\"move\",\"to\":[6520,-4110]},{\"tick\":1510,\"op\":\"enforcer\",\"do\":\"takedown\",\"target\":\"guard\",\"how\":\"kill\"},{\"tick\":1540,\"op\":\"enforcer\",\"do\":\"enter\"},{\"tick\":1540,\"op\":\"tech\",\"do\":\"move\",\"to\":[675,-4625]},{\"tick\":1540,\"op\":\"scout\",\"do\":\"move\",\"to\":[1175,-4825]},{\"tick\":1540,\"op\":\"fixer\",\"do\":\"move\",\"to\":[1025,-4625]},{\"tick\":1920,\"op\":\"enforcer\",\"do\":\"stance\",\"stance\":\"hold\"}]"); // N128's orders
                r.To(T(g.ClockOver));
                r.Eq(g.FailCode, "sealed", "the loud fight's seal runs out");
                var c = CauseIs(r, "sealed", "sealed", false, "", "", Enf);
                r.Is(g.Vale != ValeAt.Car, "Vale never boarded");
                r.Eq((c?.Car, c?.Holders.Count, c?.AboardAt), ("empty", 0, -1), "FailCause.Car: empty, no holders, never aboard");
                r.Is(c != null && c.Json().Contains("\"car\":\"empty\""), "the fail camera's JSON carries it");
                var d = RouteBacked(r);
                r.Is(!d.Route.Contains("vale-reaches-getaway-car"), $"the debrief does not say Vale reached the getaway car (route: {string.Join(", ", d.Route)})");
                r.Is(d.Route.Contains("vale-taken-through-office"), "it says he was taken through the Office (enter.out)");
                r.Is(d.Route.Contains("guard-killed"), "and how the door was cleared");
            }, new Start { Security = 4, OneInput = false }); // brief 039: N128's fixture (stance hold)

            Add("N184", "—", "The seal's car state, ready (brief 057 §4), and the seal in Windows(): N115's fixture (route G-kill without the final fight: the dead guard read by the entrance camera when the Tech leaves the cabinet, Alert 2, the seal clock running; nobody fights). The Enforcer comes out with Vale, walks him to the getaway car and boards; nobody gives Go. While the clock runs, Windows() lists seal:district until Lockdown, from the tick the clock was last set or moved, its origin the report whose time is running. The district seals with Vale aboard and the car free: FailCause.Car ready, aboard at his board.in, no Go refused; the debrief says Vale reached the getaway car (true), every line backed.", r =>
            {
                var g = r.G;
                Orders(r, new Order { Tick = 0, Op = Tech, Do = "move", To = "cabinet", Run = true }, MvTo(0, Enf, BesideDoor), Mv(0, Scout, "d:Office approach"), Mv(0, Fixer, "crew:fixer"),
                    Act(300, Tech, "loop", "entrance") /* Loop v2 (brief 058 §4): planted at 0:30 so its 2:00 runs past the Enter */, Act(462, Scout, "watch", "guard"), Mv(662, Scout, "crew:scout"), Act(1060, Tech, "reroute", "patrol"), // N115's orders
                    MvTo(1440, Enf, BesideGuard), Act(1510, Enf, "takedown", "guard", "kill"), Act(1540, Enf, "enter"),
                    MvTo(1540, Tech, new Pt(675, -4625)), MvTo(1540, Scout, new Pt(1175, -4825)), Mv(1540, Fixer, "d:Getaway bay"));
                r.Is(r.Until(() => g.Lockdown != Never, T(120)) && !g.Over, $"the seal clock runs ({Report.Clock(g.Tick)}; seal {Report.Clock(g.Lockdown)})");
                // the seal window, tick by tick to the end: listed exactly while the clock runs, until Lockdown, from its last move
                bool same = true; string off = ""; int lastMove = g.Tick;
                void Check()
                {
                    var w = g.Windows().FirstOrDefault(x => x.Kind == "seal");
                    bool ok = g.Over ? w == null : w != null && w.Thing == "district" && w.Until == g.Lockdown && w.From >= 0 && w.From <= g.Tick && w.Origin != null;
                    if (!ok && same) { same = false; off = $"{Report.Clock(g.Tick)}: {string.Join(" ", g.Windows())}"; }
                }
                Check();
                var w0 = g.Windows().FirstOrDefault(x => x.Kind == "seal");
                if (!r.Is(w0 != null, $"the seal window is listed ({Report.Clock(g.Tick)}: {g.Result} {g.FailCode}, {string.Join(" ", g.Windows())})")) return;
                r.Is(g.Log.Exists(l => l.Tick == w0!.From && (l.Code == "seal.starts" || l.Code == "security.called" || l.Code == "fight.loud")), $"the window opens on the tick the clock was set ({Report.Clock(w0!.From)})");
                if (!r.Is(r.Until(() => { Check(); return g.Vale == ValeAt.WithEnforcer && !r.Op(Enf).Inside; }, T(120)), $"the Enforcer comes out with Vale ({Report.Clock(g.Tick)})")) return;
                int at = r.Send(Enf, "d:Getaway bay");
                while (!g.Over && g.Tick < at) { g.Step(); Check(); }
                bool aboard = !g.Over && g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "board", Target = "car" }).ok;
                if (!r.Line(aboard, $"he boards with Vale before the seal ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)}; brief 059: with no final fight the posted three round our car fire on the Vale holder in their cone, Sky's answer 3)"))
                {
                    while (!g.Over && g.Tick < T(g.ClockOver) + T(600)) { g.Step(); Check(); }
                    r.Is(same, $"the seal window matches Lockdown every tick, gone at the end ({off})");
                    return;
                }
                while (!g.Over && r.Op(Enf).Busy != null) { g.Step(); Check(); }
                r.Line(g.Vale == ValeAt.Car && !g.Over, $"Vale is in the car ({Report.Clock(g.Tick)}), the seal at {Report.Clock(g.Lockdown)}");
                int bin = g.Log.FindLastIndex(l => l.Code == "board.in"); var before = g.Windows().FirstOrDefault(x => x.Kind == "seal");
                if (!r.Line(bin >= 0 && before != null, $"Vale is aboard with the seal running ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)}; brief 059: the posted three round our car)")) { while (!g.Over && g.Tick < T(g.ClockOver) + T(600)) { g.Step(); Check(); } r.Is(same, $"the seal window matches Lockdown every tick, gone at the end ({off})"); return; }
                int boarded = g.Log[bin].Tick;
                while (!g.Over && g.Tick < T(g.ClockOver) + T(600)) { g.Step(); Check(); }
                r.Is(same, $"the seal window matches Lockdown every tick, gone at the end ({off})");
                r.Eq(g.FailCode, "sealed", "nobody gave Go: the district seals");
                var c = CauseIs(r, "sealed", "sealed", false, "", "", g.Cause?.Ops.Count > 0 ? string.Join(",", g.Cause.Ops) : "");
                r.Eq((c?.Car, c?.Holders.Count, c?.AboardAt, c?.GoRefusedAt), ("ready", 0, boarded, -1), "FailCause.Car: ready (aboard, free, no Go), aboard at his board.in");
                r.Is(g.CarHolders().Count == 0, "nothing holds the car at the seal");
                r.Is(c != null && before!.Origin == (c.LastMove ?? c.Origin), "the window's origin is the report whose time ran out (the cause's last move, else its origin)");
                var d = RouteBacked(r);
                r.Is(d.Route.Contains("vale-reaches-getaway-car") && d.Route.Contains("vale-taken-through-office"), $"the debrief: through the Office, and he reached the car (route: {string.Join(", ", d.Route)})");
            }, NoFight());

            Add("N185", "—", "The held car with Vale aboard and the seal running (brief 057 §4), restated on engine 0.35.2 (Motive briefs/039/motive/rulings-0.35.1.md §2, Sky's yes 2026-10-27): N111's fixture (route A, quiet, the ambush springs; one rival goes for the road and holds the exit lane). The Scout and the Fixer fight on and spare the blocker; the Enforcer walks Vale in and boards, and the two board after him; the Tech, never drawn, stands outside. Go is refused car-held, naming the holders. 0.34.0 to 0.35.1: the undrawn Tech kept K3's walk off and the district sealed with the car held (FailCause.Car held). Now an undrawn agent standing by is no guard: the rivals up and not broken walk to the held car and take Vale before the seal (rivals-reach-vale). FailCause.Car held at the seal has no play here any more (a held car with Vale aboard now needs a drawn agent outside, who is fought): flagged to Motive.", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true);
                r.Send(Scout, "crew:scout"); r.Send(Fixer, "crew:fixer"); r.Do(Fixer, "draw"); r.Do(Scout, "draw");
                r.Send(Enf, "d:Getaway bay");
                r.Until(() => g.Over || g.FightSprung != Never, T(240));
                var b = g.Hostiles.Find(h => h.Blocker);
                if (!r.Is(b != null && !g.Over, $"the ambush springs and one goes for the road ({Report.Clock(g.Tick)})")) return;
                // engine 0.33.0 (Motive M059-B1, B7): the crew fights from where it stands and shoots the carrier, so the three support him (focus
                // fire on whoever fires on him), sparing the blocker
                // engine 0.34.0 (Motive M067-SQ2, K3): the Scout and the Fixer cover him; the Tech, who never draws, stands outside. A rival with nobody
                // in reach now advances on an operative he has cause to fire on, so three firing at the car would break the crew (the blocker leaves
                // with it) or be shot down (then K3's walk to the car takes Vale): neither leaves the car held at the seal. Once Vale is aboard the
                // two board too; the Tech, outside and nobody's foe, keeps K3 off, and with no operative to advance on the rivals hold
                r.Focus = true;
                r.Engage(() => Targets(g, true), () => g.Over || (r.Op(Enf).Walk == null && r.Op(Enf).Busy == null && g.Vale == ValeAt.WithEnforcer), 120, new[] { Scout, Fixer }, false, advance: false);
                r.Focus = false;
                r.Is(!g.Over && r.Op(Enf).Walk == null && g.Vale == ValeAt.WithEnforcer, $"the Enforcer at the car with Vale ({Report.Clock(g.Tick)}; {g.Result} {g.FailCode})");
                if (!r.Is(r.Do(Enf, "board", "car"), "he boards with Vale")) return;
                r.Done(Enf); g.Step();
                r.Is(g.Vale == ValeAt.Car, "Vale is in the car");
                var two = new[] { Scout, Fixer }; var boarded = new HashSet<string>();
                foreach (var s0 in two) if (!r.Op(s0).Down) { g.Issue(new Order { Tick = g.Tick, Op = s0, Do = "cease" }); r.Send(s0, "d:Getaway bay"); }
                for (int k = 0; k < T(30) && !g.Over && two.Any(s0 => !r.Op(s0).Down && !r.Op(s0).InCar); k++)
                {
                    foreach (var s0 in two) { var o = r.Op(s0); if (!o.Down && !o.InCar && !boarded.Contains(s0) && o.Busy == null && g.AtGetaway(o.Pos)) { boarded.Add(s0); r.Do(s0, "board", "car"); } }
                    g.Step();
                }
                r.Is(two.All(s0 => r.Op(s0).Down || r.Op(s0).InCar), $"the two who fought are aboard ({string.Join(", ", two.Select(s0 => s0 + (r.Op(s0).InCar ? " in the car" : r.Op(s0).Down ? " down" : " outside")))})");
                r.Is(!r.Op(Tech).Down && !r.Op(Tech).InCar && !r.Op(Tech).Gun.Drawn && !r.Op(Tech).Gun.Fired, "the Tech stands outside, never drawn: nobody's foe");
                r.Until(() => g.Over || g.CarHolders().Contains(b!.Id), T(60));
                var a = g.Issue(new Order { Tick = g.Tick, Op = "cell", Do = "go" });
                r.Eq((a.ok, a.code), (false, "car-held"), $"Go is refused while the car is held ({a.ids})");
                int refused = g.Tick;
                List<string> last = new List<string>();
                // the three hold their fire on the blocker: they cease whenever one fires on it, so the lane stays held to the seal
                while (!g.Over && g.Tick < T(g.ClockOver) + T(900))
                {
                    foreach (var s in new[] { Scout, Fixer, Tech }) { var o = r.Op(s); if (!o.Down && o.Gun.Target == b!.Id) g.Issue(new Order { Tick = g.Tick, Op = s, Do = "cease" }); }
                    last = g.CarHolders(); g.Step();
                }
                // engine 0.35.2: the undrawn Tech outside is no guard (fight.unguarded a): the rivals walk to the held car and take Vale, before the seal
                int walk = LogTick(g, "rivals.for-car", refused - 1);
                r.Is(g.FailCode == "rivals-reach-vale" && walk >= 0, $"the rivals walk past the undrawn Tech to the held car and take Vale ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)}; rivals.for-car {(walk >= 0 ? Report.Clock(walk) : "-")})");
                r.Is(g.Tick < g.Lockdown, $"before the seal ({Report.Clock(g.Tick)}, Lockdown {Report.Clock(g.Lockdown)})");
                r.Is(!r.Op(Tech).Down && !r.Op(Tech).InCar && !r.Op(Tech).Gun.Drawn, "the Tech still stands outside, undrawn, up");
                r.Is(!g.Log.Exists(l => l.Code == "rivals.for-car" && l.Text.StartsWith(b!.Id + " ", StringComparison.Ordinal)), $"the blocker ({b!.Id}) keeps his lane");
                RouteBacked(r);
            });

            Add("N186", "—", "The rerouted patrol walks, and its path as walked (brief 057 §2, Sky 2026-10-24: the robot follows the drawn path and moves smoothly; cell.reroute.walk; Game.PatrolLegs, PatrolRoute): route A's opening with the reroute. On its sweep the path is the straight line between the site's patrol-a and patrol-b. Rerouted, it walks the walk grid at patrol-chase-cm-s to the wait spot (patrol-reroute), waits, and walks back at the same pace to stand on its sweep exactly where SweepPos has it at PatrolAwayUntil, all inside the false orders (blind throughout, as before: the Reroute buys what it bought); it never steps more than its pace in a tick. Then a drawn weapon in its sweep cone breaks the sweep: it chases on the grid route the path names. At every tick the robot stands within 30 cm of a walked leg of the path read a second before (or on a placement leg's point), the sweep ends are the site's, and the read moves no hash.", r =>
            {
                var g = r.G;
                var a = g.S.Stand(g.S.PatrolA); var b = g.S.Stand(g.S.PatrolB); var wait = g.S.Stand(g.S.PatrolReroute);
                var p0 = g.PatrolLegs();
                r.Is(p0.Mode == "sweep" && p0.SweepA == a && p0.SweepB == b && p0.Wait == wait, $"at the start: the sweep, its ends the site's patrol-a {a} and patrol-b {b}, the wait spot {wait} ({p0})");
                r.Is(p0.Legs.Count == 1 && p0.Legs[0].Walk && p0.Legs[0].Points[0] == p0.At && p0.Legs[0].Points.Contains(a) && p0.Legs[0].Points.Contains(b), "one walked leg from where it stands, to both ends");
                // the oracle: the robot now against the path read Hz ticks ago
                // the oracle: the robot after each tick against the path read before it, unless an event in that tick changed the path's mode
                // (an order landing, a shot: the read is "from now on", it cannot foresee them); the reroute's planned return is checked apart
                var cur = g.PatrolLegs(); int checkedTicks = 0, worst = 0, events = 0, stepMax = 0; Pt prevAt = g.PatrolPose()!.Value.pos; string firstOff = ""; var modes = new HashSet<string>();
                void Step()
                {
                    var old = cur; prevAt = g.PatrolPose()?.pos ?? prevAt; g.Step(); if (g.Over) return; cur = g.PatrolLegs();
                    var now = g.PatrolPose()!.Value.pos; modes.Add(old.Mode);
                    if (old.Mode != cur.Mode && !(old.Mode == "away" && cur.Mode == "sweep")) { events++; return; }
                    if (old.Mode == "away") stepMax = Math.Max(stepMax, (int)G.Isqrt(G.D2(prevAt, now)));
                    if (old.Mode == "down") return; // nothing to draw: it lies where it fell
                    long best = long.MaxValue;
                    foreach (var leg in old.Legs)
                    {
                        if (leg.From > g.Tick || (leg.Until >= 0 && leg.Until < g.Tick && leg.Kind != "back")) continue;
                        if (!leg.Walk) { foreach (var q in leg.Points) best = Math.Min(best, G.D2(q, now)); continue; }
                        for (int i = 1; i < leg.Points.Count; i++) best = Math.Min(best, SegD2(leg.Points[i - 1], leg.Points[i], now));
                        if (leg.Points.Count == 1) best = Math.Min(best, G.D2(leg.Points[0], now));
                    }
                    int d = best == long.MaxValue ? int.MaxValue : (int)G.Isqrt(best); checkedTicks++; worst = Math.Max(worst, d);
                    if (d > 30 && firstOff.Length == 0) firstOff = $"{Report.Clock(g.Tick)}: robot at {now}, {d} cm off the path read a tick before ({old})";
                }
                void Go(int tick) { while (g.Tick < tick && !g.Over) Step(); }
                Go(r.Send(Tech, "cabinet", run: true));
                r.Do(Tech, "reroute", "patrol"); Go(r.Op(Tech).BusyEnd); Go(g.Tick + 1);
                var pr = g.PatrolLegs(); var kinds = string.Join(",", pr.Legs.Select(x => x.Kind)); int until = g.PatrolAwayUntil;
                r.Is(pr.Mode == "away" && kinds.StartsWith("out,wait,back,", StringComparison.Ordinal) && kinds.EndsWith("sweep", StringComparison.Ordinal) && pr.Legs[0].Walk && pr.Legs[0].Points[pr.Legs[0].Points.Count - 1] == wait
                    && pr.Legs[2].Walk && pr.BackAt == until && pr.Legs[pr.Legs.Count - 1].From == until && pr.Legs[pr.Legs.Count - 1].Points[0] == SweepAt(g, until),
                    $"rerouted: the walk out to the wait spot, the wait, the walk back, on its sweep at {Report.Clock(until)} where SweepPos has it, all inside the false orders ({pr})");
                var route = g.PatrolRoute();
                r.Is(route != null && route.Length >= 4 && route.Contains(wait) && route[0] == pr.At, $"PatrolRoute(): one polyline from where it stands, through the wait spot ({route?.Length} points)");
                r.Is(pr.Legs.All(x => x.Ticks.Count == x.Points.Count && x.Ticks.Zip(x.Ticks.Skip(1), (a1, b1) => a1 <= b1).All(y => y)), "each point carries the tick it is reached, in order");
                int arrive = pr.Legs[0].Until; Go(arrive);
                r.Eq(g.PatrolPose()!.Value.pos, wait, $"it reaches the wait spot at {Report.Clock(arrive)}, the tick the out leg said");
                int leave = pr.Legs[1].Until; Go(leave + 1);
                r.Is(g.PatrolLegs().Mode == "away" && g.PatrolLegs().Legs[0].Kind == "back" && !g.ConeFan("patrol")!.Live, $"at {Report.Clock(leave)} it starts back, still on false orders: it watches nothing");
                var backAt = pr.Legs[pr.Legs.Count - 1].Points[0];
                Go(until);
                r.Is(g.PatrolPose()!.Value.pos == backAt && g.Log.Exists(l => l.Code == "patrol.returns" && l.Tick == until), $"back on its sweep at {Report.Clock(until)} where the plan said ({backAt}), patrol.returns: the Reroute buys what it bought");
                r.Is(stepMax <= g.R.RerouteWalkCmS / Game.Hz + 2, $"walking out and back it never steps more than its pace in a tick ({stepMax} cm; the pace {g.R.RerouteWalkCmS} cm/s)");
                Go(until + T(20));
                // a chase: a weapon drawn in its sight brings the patrol off its sweep, on the grid toward the operative (engine 0.34.0, brief 067 A: the
                // sweep is the service yard's, beyond 30 m of S16's shot at the public door, so the Scout draws 8 m in front of it in the yard)
                var pp0 = g.PatrolPose()!.Value; var front = g.Walk.NearestFree(pp0.pos + new Pt(pp0.facing.X * 8, pp0.facing.Y * 8)) ?? pp0.pos;
                Go(r.SendTo(Scout, front)); Go(g.Tick + 1);
                { int seeBy = g.Tick + T(100); while (!g.Over && g.Tick < seeBy && !g.PatrolWatching(r.Op(Scout).Pos)) Step(); } // until its cone has her
                r.Do(Scout, "draw"); int chaseBy = g.Tick + T(15);
                while (!g.Over && g.Tick < chaseBy && !g.PatrolChasing) Step();
                if (r.Is(g.PatrolChasing, $"the shot breaks the sweep: the patrol chases ({Report.Clock(g.Tick)})"))
                {
                    var pc = g.PatrolLegs();
                    r.Is(pc.Mode == "chase" && pc.Legs.Count >= 1 && (pc.Legs[0].Kind == "hold" || (pc.Legs[0].Walk && pc.Legs[0].Points.Count >= 2)), $"the path names the chase ({pc})");
                    Go(g.Tick + T(30));
                }
                r.Is(modes.Contains("sweep") && modes.Contains("away") && modes.Contains("chase"), $"the oracle saw the sweep, the reroute and the chase ({string.Join(",", modes)}; {events} ticks where an event changed the mode)");
                r.Is(firstOff.Length == 0, $"after every tick the robot stood within 30 cm of the path read the tick before ({checkedTicks} ticks, worst {worst} cm) {firstOff}");
            });
            Add("N187", "—", "The reroute walk fits the false orders from anywhere on the sweep (brief 057 §2, engine 0.30.0): the Tech's Reroute landing at every 5 s of the sweep's 100 s period (21 games). Each time the patrol walks from where it stood, never more than its pace in a tick, is blind throughout (no cone live, nobody noticed by it), and stands on its sweep exactly where SweepPos has it at PatrolAwayUntil, 115 s after the landing; from a start too far to reach the wait spot and still land on time it turns back short of it (counted).", r =>
            {
                var g = r.G; int okRuns = 0, turned = 0, worstStep = 0, minWait = int.MaxValue; string bad = "";
                int period = T(g.R.PatrolPeriodS), pace = g.R.RerouteWalkCmS / Game.Hz + 2;
                for (int off = 0; off <= period; off += T(5))
                {
                    var h = r.Inputs != null ? Engine.New(r.Inputs) : new Game(g.R, g.S, Start.Parse(JNode.Parse(g.Start.Canonical())), null, g.People2); var tech = h.OpOf(OpId.Tech); // engine 0.31.0: a run made without inputs (RoundsTests) plays its sub-runs on the run's own rules, site, start and people
                    h.Issue(new Order { Tick = 0, Op = Tech, Do = "move", To = "cabinet", Run = true });
                    while (tech.Walk != null && !h.Over) h.Step();
                    int give = Math.Max(h.Tick, T(40) + off - T(h.R.RerouteS)); // the landing at 40 s + off
                    h.RunTo(give);
                    if (!h.Issue(new Order { Tick = h.Tick, Op = Tech, Do = "reroute", Target = "patrol" }).ok) { bad += $" off {off}: refused;"; continue; }
                    while (h.PatrolAwayUntil <= h.Tick && !h.Over) h.Step();
                    var plan = h.PatrolLegs(); int until = h.PatrolAwayUntil; var prev = h.PatrolPose()!.Value.pos; int step = 0; bool blind = true;
                    var wait = plan.Legs.FirstOrDefault(x => x.Kind == "wait");
                    if (wait == null || wait.Points[0] != plan.Wait) turned++; else minWait = Math.Min(minWait, wait.Until - wait.From);
                    while (h.Tick < until && !h.Over)
                    {
                        h.Step(); var now = h.PatrolPose()!.Value.pos; step = Math.Max(step, (int)G.Isqrt(G.D2(prev, now))); prev = now;
                        if (h.Tick < until && (h.ConeFan("patrol")!.Live || h.Detection().Exists(n => n.Watcher == "patrol"))) blind = false;
                    }
                    worstStep = Math.Max(worstStep, step);
                    bool ok = !h.Over && h.PatrolPose()!.Value.pos == SweepAt(h, until) && step <= pace && blind;
                    if (ok) okRuns++; else bad += $" off {off}: at {h.PatrolPose()?.pos} want {SweepAt(h, until)}, step {step}, blind {blind};";
                }
                r.Eq(okRuns, period / T(5) + 1, $"every landing walks, stays blind and lands on its sweep at PatrolAwayUntil (worst step {worstStep} cm a tick; {turned} turned back short of the wait spot; the shortest wait {(minWait == int.MaxValue ? 0 : minWait / Game.Hz)} s){bad}");
                g.Step(); // the scenario's own run moves a tick, so its replay is its own
            });
            Add("NQ-1", "—", "The batch notice read (Redline 064 #1, engine 0.30.0): NoticeProbeOf resolves a person once a tick and NoticesAt answers exactly what NoticesNow does, for every walker on the street and the cast, at 0:30, 2:00 and 4:00, over a 1 m grid 14 m round each, for a takedown, a body and a drawn weapon. A probe from another tick is refused.", r =>
            {
                var g = r.G; int asked = 0, off = 0; string first = ""; var acts = new[] { "takedown", "body", "weapon-drawn" };
                foreach (int t in new[] { T(30), T(120), T(240) })
                {
                    r.To(t);
                    var ids = g.Walkers.Select(w => w.P.Id).Concat(new[] { Inez, Driver, GuardR }).Distinct().ToList();
                    foreach (var id in ids)
                    {
                        var pr = g.NoticeProbeOf(id); var p = g.PersonById(id);
                        if (pr == null) { foreach (var act in acts) if (g.NoticesNow(id, act, new Pt(0, 0))) { off++; if (first.Length == 0) first = $"{id}: no probe, but NoticesNow true"; } continue; }
                        for (int dx = -1400; dx <= 1400; dx += 100) for (int dy = -1400; dy <= 1400; dy += 100)
                        {
                            var q = pr.At + new Pt(dx, dy);
                            foreach (var act in acts) { asked++; if (g.NoticesAt(pr, act, q) != g.NoticesNow(id, act, q)) { off++; if (first.Length == 0) first = $"{Report.Clock(g.Tick)} {id} {act} at {q}"; } }
                        }
                    }
                }
                r.Is(asked > 10000 && off == 0, $"NoticesAt equals NoticesNow on {asked} reads ({off} differ) {first}");
                var stale = g.NoticeProbeOf(Inez); g.Step();
                bool threw = false; try { if (stale != null) g.NoticesAt(stale, "body", stale.At); } catch (InvalidOperationException) { threw = true; }
                r.Is(stale == null || threw, "a probe from another tick is refused");
            });
            Add("NQ-2", "—", "A Listen offered must be a Listen taken (the 0.2.116 bot row, Q-OFFER: Listen on Inez offered on the ring while the Scout was busy, refused needs-routine when clicked): while the Scout is busy on the guard's Watch, LearnQuery for a Listen on Inez says needs-routine (her Watch is still to give), not agent-busy; a Watch on Inez, whose only bar is the busy Scout, says agent-busy; once he is free, the query and Issue agree on needs-routine.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard"));
                r.Do(Scout, "watch", GuardR);
                r.Is(r.Until(() => r.Op(Scout).Busy != null, T(60)), "the Scout is busy on the guard's Watch (after any walk in)");
                r.Is(g.WatchOffered(Inez) && !g.Inez.KRoutine, "Inez's Watch is still to give");
                var busyListen = g.LearnQuery(Scout, "listen", Inez);
                r.Eq((busyListen.ok, busyListen.code), (false, "needs-routine"), "busy: the Listen's own refusal, not agent-busy");
                var busyWatch = g.LearnQuery(Scout, "watch", Inez);
                r.Eq((busyWatch.ok, busyWatch.code), (false, "agent-busy"), "busy: a Watch with no other bar says agent-busy");
                r.Done(Scout);
                var q = g.LearnQuery(Scout, "listen", Inez); var a = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "listen", Target = Inez });
                r.Eq((q.ok, q.code), (a.ok, a.code), $"free: the query and Issue agree ({a.code})");
            });
        }

        static Pt SweepAt(Game g, int t) { int period = T(g.R.PatrolPeriodS), half = period / 2, ph = t % period; return G.Lerp(g.S.Stand(g.S.PatrolA), g.S.Stand(g.S.PatrolB), ph <= half ? ph : period - ph, half); }
        /// <summary>The squared distance from p to the segment a–b (cm²).</summary>
        static long SegD2(Pt a, Pt b, Pt p)
        {
            long dx = b.X - a.X, dy = b.Y - a.Y, l2 = dx * dx + dy * dy;
            if (l2 == 0) return G.D2(a, p);
            long t = (p.X - a.X) * dx + (p.Y - a.Y) * dy;
            if (t <= 0) return G.D2(a, p); if (t >= l2) return G.D2(b, p);
            double u = (double)t / l2; double qx = a.X + u * dx - p.X, qy = a.Y + u * dy - p.Y;
            return (long)Math.Ceiling(qx * qx + qy * qy);
        }
    }
}
