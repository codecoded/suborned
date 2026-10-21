// Engine 0.15.0: firing on the move and anyone can be shot (Sky 2026-10-21; Motive briefs/027/motive/tuning.md §8, §9). The scenarios Motive
// named, on the fight range (brief 029: F8-*) and in the Handover (§9.5: S9-*, and F8-O9, which needs Vale). Each range scenario is run under
// today and the lab where Motive asks for both (the id's -LAB twin). The balance cases (F8-RUSH, -RUSH-B, -BREAK, -CIRCLE) check Motive's
// lines; where the engine and his 1-D arithmetic part, the check names both numbers.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The Handover cases of tuning.md §8.7 (F8-O9) and §9.5 (S9-*).</summary>
        static void FireMoveCases()
        {
            Add("F8-O9", "—", "§8.2, unchanged: an operative carrying a body is still refused fire (fire-carrying)", r =>
            {
                var g = r.G;
                r.SendTo(Fixer, BesideDoor + new Pt(0, -150));
                GuardQuiet(r); r.Takedown(GuardR, "knockout"); r.Done(Enf);
                r.To(r.SendTo(Fixer, BesideGuard)); r.Do(Fixer, "draw"); r.Wait(T(1));
                r.Body(Fixer, "carry", GuardR); r.Done(Fixer);
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "fire", Target = "patrol" }).code, "fire-carrying", "carrying a body: fire-carrying");
            });
            Add("F8-O9-VALE", "—", "§8.2, unchanged: walking Vale the Enforcer is still refused fire (fire-carrying-vale) and a run (run-carrying-vale)", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true); Support(r); ToAmbush(r);
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "fire", Target = "hostiles" }).code, "fire-carrying-vale", "walking Vale: fire-carrying-vale");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", Point = g.S.Stand(g.S.GetawayCar), Run = true }).code, "run-carrying-vale", "and no run with Vale");
            });

            Add("S9-VALE", "—", "§9.1, §9.3: Vale is a target; a forced fire on him always takes the 0.5 s hold (the card: vale-dead), then vale-dead fails the mission", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true); Support(r); ToAmbush(r);
                r.Is(g.Vale == ValeAt.WithEnforcer && g.PosOf(Vale).HasValue, "Vale is out, with the Enforcer");
                var card = g.GoingLoud(new Order { Tick = g.Tick, Op = Scout, Do = "fire", Target = Vale, Force = true });
                r.Eq((card.Fails, card.Hold, card.HoldTicks), ("vale-dead", true, g.R.LoudHoldTicks), "the card: vale-dead, the hold, 0.5 s");
                r.Eq(g.R.LoudHoldTicks, 5, "loud-commit-hold-ticks: 0.5 s");
                var drv = g.GoingLoud(new Order { Tick = g.Tick, Op = Scout, Do = "fire", Target = Driver, Force = true });
                r.Eq(drv.Fails, "driver-shot", "the driver while Vale is out: driver-shot");
                r.Is(r.Fire(Scout, Vale, force: true), "the forced fire on Vale is taken");
                r.Until(() => g.Over, T(30));
                r.Eq(g.FailCode, "vale-dead", "Vale is shot: vale-dead");
            });

            Add("S9-CROWD", "—", "§9.2 gap 1: a café customer shot dead in sight of the others: Alert 3, a Menace step, a dead body where she fell, found, a case, found-kill-standing", r =>
            {
                var g = r.G;
                const string Victim = "p-pylon-row-east-05"; // N48's customer at terrace table 2
                r.To(T(240));
                var seat = g.CrowdPos(WalkerById(r, Victim)) ?? throw new InvalidOperationException($"{Victim} is not on the street at 4:00");
                r.To(r.SendTo(Enf, g.S.Grid.NearestFree(seat + new Pt(400, 0)) ?? seat));
                int standing = g.Standing;
                r.Is(r.Fire(Enf, Victim, force: true), "the forced fire on the customer is taken");
                r.Is(r.Until(() => g.PersonById(Victim)!.Down, T(10)), "she is down");
                r.Eq(g.Alert, 3, "violence seen by civilians: Alert 3");
                r.Is(g.StandingMoves.Exists(m => m.cause == "seen-violence"), "a Menace step (seen-violence)");
                var b = g.BodyById(Victim);
                r.Is(b != null && b.Dead && g.Acts[b.Act].How == "shot" && g.Acts[b.Act].Kill, "a dead body where she fell (a shot act)");
                r.Is(r.Until(() => g.Log.Exists(l => l.Code == "witness.react" && l.Text.Contains("finds a body")) || g.Acts[b!.Act].Confirmed, T(120)), "the body is found");
                r.Until(() => g.Over || g.Acts[b!.Act].Confirmed, T(240));
                var next = g.BetweenMissions(g.Investigate());
                r.Is(next.Cases.Exists(c => c.kind == "found-killing"), "a case: found-killing");
                bool stood = g.StandingMoves.Exists(m => m.cause == "found-killing") || next.Standing.Exists(l => l.Moves.Exists(m => m.cause == "found-killing"));
                r.Is(stood, "found-kill-standing (−1): in the mission when confirmed, else between missions");
            });

            Add("S9-INEZ", "—", "§9.2: turned Inez shot down: Turned false; the call that pulls the guard off his door is refused; between missions the guard grieves", r =>
            {
                var g = r.G;
                r.Is(g.Inez.Turned, "Inez is turned (start)");
                var at = g.PosOf(Inez)!.Value;
                r.To(r.SendTo(Enf, g.S.Grid.NearestFree(at + new Pt(0, -500)) ?? at));
                r.Is(r.Fire(Enf, Inez, force: true), "the forced fire on Inez is taken");
                r.Is(r.Until(() => g.Inez.Down, T(10)), "she is down");
                r.Is(!g.Inez.Turned, "Turned: false");
                var a = g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "call", Target = Inez });
                r.Is(!a.ok, $"the call is refused ({a.code})");
                var b = g.BodyOf(g.Inez); r.Is(b != null && b.Dead, "her body lies where she fell");
                var next = g.BetweenMissions(g.Investigate());
                r.Is(next.Lines.Exists(l => l.Line == "grief" && l.Who == GuardR), "the guard grieves");
            }, Turned(Inez));

            Add("S9-CAM", "—", "§9.2 gap 2: a camera is a target: one hit and it is dark; the shot is gunfire (Alert ≥ 2, the response, gunfire-trail once); no hit or down Trail, no Standing", r =>
            {
                var g = r.G; var cam = g.S.Cameras.First(c => c.Id == "entrance");
                var spot = g.S.Grid.Nearest(r.Op(Scout).Pos, q => G.D2(q, cam.At) <= 1200L * 1200 && g.S.Sight(q, cam.At, g.R.Eye), 9000);
                if (!r.Is(spot.HasValue, "a spot with a line to the entrance camera")) return;
                r.To(r.SendTo(Scout, spot!.Value));
                int trail = g.Trail, standing = g.Standing;
                var pv = g.PreviewFire(OpId.Scout, "camera:entrance");
                r.Eq((pv.Refused, pv.TrailHit, pv.TrailDown), ("", 0, 0), "the chip: a target, no hit or down Trail");
                r.Is(r.Fire(Scout, "camera:entrance"), "fire on the camera taken");
                r.Is(r.Until(() => g.CameraDark("entrance"), T(10)), "it goes dark");
                r.Is(!g.CameraWatching("entrance", cam.At + new Pt(0, 300)), "it watches nothing");
                r.Is(g.Alert >= 2, $"gunfire: Alert ≥ 2 ({g.Alert})");
                r.Is(g.Log.Exists(l => l.Code == "security.called"), "the response is called");
                r.Eq(g.Trail - trail, g.R.GunfireTrail, "gunfire-trail once, no hit or down Trail");
                r.Eq(g.Standing, standing, "no Standing step");
                r.Is(g.Log.Exists(l => l.Code == "fire.camera-dark"), "logged fire.camera-dark");
            });

            Add("S9-AFTER", "—", "§9.1: fire on a final-fight hostile after fight.over is taken (fight-over is no longer given) and traced", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true); Support(r); ToAmbush(r);
                r.FightItOutAll(120, Scout, Fixer, Tech);
                r.Is(g.FightOverTick != int.MaxValue / 4, "the final fight is over");
                var still = g.Hostiles.Find(h => h.Present && !h.Down && g.PosOf(h.Id).HasValue);
                if (still == null) { var dn = g.Hostiles.First(); r.Is(g.Hostiles.TrueForAll(h => h.Down || !h.Present), "nobody of the fight is left standing to shoot (all down or gone): the order is taken or no-target"); return; }
                int trail = g.Trail;
                string by = new[] { Scout, Fixer, Tech }.First(n => !r.Op(n).Down && r.Op(n).Gun.W != null);
                var a = g.Issue(new Order { Tick = g.Tick, Op = by, Do = "fire", Target = still.Id });
                r.Is(a.ok || a.code == "out-of-range" || a.code == "no-clear-line", $"the order on {still.Id} ({still.Gun.St}) is not refused fight-over ({a.code})");
                r.Is(a.code != "fight-over", "fight-over is no longer given");
                if (a.ok) { r.Until(() => r.Shots().Exists(x => x.by == by && x.target == still.Id && x.tick > g.FightOverTick), T(10)); if (r.Shots().Exists(x => x.by == by && x.target == still.Id && x.tick > g.FightOverTick)) r.Is(g.Trail > trail, $"traced: Trail {trail} → {g.Trail}"); }
            });

            Add("S9-KO", "—", "§9.1: a knocked-out body is a target where it lies: a shot kills (the act becomes a kill), plus the gunfire's own price", r =>
            {
                var g = r.G;
                r.SendTo(Fixer, BesideDoor + new Pt(0, -150));
                GuardQuiet(r); r.Takedown(GuardR, "knockout"); r.Done(Enf);
                var b = g.BodyById(GuardR)!; var a0 = ActOn(r, GuardId);
                r.Is(!b.Dead && !a0.Kill, "he is out, not dead");
                r.Is(g.PosOf(GuardR).HasValue, "a target where he lies");
                r.Do(Enf, "draw"); r.Wait(T(1));
                r.Is(r.Fire(Enf, GuardR), "fire on the knocked-out guard taken");
                r.Is(r.Until(() => b.Dead, T(10)), "the shot kills him");
                r.Is(a0.Kill && g.Chain.Contains("killed:guard"), "the act is a kill");
                r.Is(g.Gunfire > 0 && g.Alert >= 2, "and it is gunfire");
            });

            Add("S9-SELF", "—", "§9.1, Sky §9.7: the cell's own agents are the one target refusal (fire-on-cell)", r =>
            {
                var g = r.G; r.Do(Scout, "draw"); r.Wait(T(1));
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "fire", Target = Fixer }).code, "fire-on-cell", "fire on the Fixer: fire-on-cell");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "fire", Target = Fixer, Force = true }).code, "fire-on-cell", "forced too");
            });

            Add("S9-UNIT", "—", "§9.1: a fire order on a brief-013 unit or robot targets that one, by its own id", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.Fire(Enf, GuardR); r.Until(() => g.GuardDown, T(30));
                r.Is(r.Until(() => g.Units.Exists(u => !u.Legacy && !u.Patrol && u.OnStreet(g.Tick) && !u.Gun.Down && g.PosOf(u.Id).HasValue), T(120)), "a unit is on the street");
                var unit = g.Units.First(u => !u.Legacy && !u.Patrol && u.OnStreet(g.Tick) && !u.Gun.Down && g.PosOf(u.Id).HasValue);
                var a = g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "fire", Target = unit.Id });
                r.Is(a.ok || a.code == "out-of-range" || a.code == "no-clear-line", $"fire on {unit.Id} ({a.code})");
                if (a.ok) r.Eq(r.Op(Enf).Gun.Order, unit.Id, "the order names that unit");
            });
        }

        static readonly string[] Cell4R = { "enforcer", "scout", "fixer", "tech" };
        /// <summary>F8-BREAK's run-away bound on layout lane (tuning.md §10.4): a metre short of the 90 m floor's end (tools/sim/range.ts FLOOR_X1).</summary>
        const int LaneEndCm = 8800;
        /// <summary>A point `cm` from `from` at `deg` degrees (x along the range, y across), on free ground (scenario geometry; the order
        /// log carries the integer point, so the run replays).</summary>
        static Pt Ray(Run r, Pt from, double deg, int cm) { double a = deg * Math.PI / 180; var p = new Pt(from.X + (int)Math.Round(Math.Cos(a) * cm), from.Y + (int)Math.Round(Math.Sin(a) * cm)); return r.G.S.Grid.NearestFree(p) ?? p; }
        /// <summary>The ‰ of the first shot by → t at or after `from` (-1 if none yet).</summary>
        static int FirstAcc(Run r, string by, string t, int from)
        {
            foreach (var x in r.G.Log)
            {
                if (x.Tick < from || (x.Code != "shot.hit" && x.Code != "shot.miss") || !x.Text.StartsWith(by + " fires on " + t + " at ", StringComparison.Ordinal)) continue;
                int a = x.Text.IndexOf('(') + 1, b = x.Text.IndexOf(" ‰", StringComparison.Ordinal);
                return int.Parse(x.Text.Substring(a, b - a), CultureInfo.InvariantCulture);
            }
            return -1;
        }
        static Answer Move(Run r, string op, Pt to, bool run = false) => r.G.Issue(new Order { Tick = r.G.Tick, Op = op, Do = "move", Point = to, Run = run });
        static Answer FireOn(Run r, string op, string t) => r.G.Issue(new Order { Tick = r.G.Tick, Op = op, Do = "fire", Target = t });
        static int HitsOn(Run r, string[] ops, int from) => r.Shots().Count(s => s.tick >= from && s.hit && ops.Contains(s.target));
        static int DownOf(Run r) => r.G.Ops.Count(o => o.Down);

        /// <summary>The F8 set-up: the four walk (holstered) to `deg` rays `cm` from the near slots' first stand, draw, then `n` police spawn
        /// at near (they hold their slots; police are trained). Returns the hostile's stand.</summary>
        static Pt F8Setup(Run r, int n, int cm, string spawnAt = "near")
        {
            var g = r.G; var h = g.RangeSlot(spawnAt, 1); var degs = new[] { -10.0, -4.0, 2.0, 8.0 };
            int at = 0; for (int k = 0; k < 4; k++) at = Math.Max(at, r.SendTo(Cell4R[k], Ray(r, h, degs[k], cm)));
            r.To(at); foreach (var op in Cell4R) r.Do(op, "draw"); r.Wait(Game.T(2));
            Spawned(r, "police", spawnAt, n);
            return h;
        }

        static void FireMoveRange()
        {
            foreach (var preset in new[] { "today", "lab" })
            {
                string sfx = preset == "lab" ? "-LAB" : ""; bool lab = preset == "lab";

                // ---------------------------------------------------------------- the order rule (§8.2), unit-sized
                AddRange("F8-O1" + sfx, preset, "open", "§8.2 rule 1: a walker given a fire order in reach stands that tick", r =>
                {
                    var g = r.G; r.Do("scout", "draw"); r.Wait(Game.T(2)); Spawned(r, "police", "eff", 1);
                    Move(r, "scout", new Pt(300, 3000)); r.Wait(3);
                    r.Is(r.Op("scout").Walk != null, "the scout walks");
                    var a = FireOn(r, "scout", "officer-1"); r.Is(a.ok, $"fire taken ({a.code})");
                    r.Is(r.Op("scout").Walk == null, "the walk ends at the order: it stands and fires");
                });
                AddRange("F8-O2" + sfx, preset, "open", "§8.2 rule 1: a walker given a fire order out of reach walks in instead", r =>
                {
                    var g = r.G; r.Do("scout", "draw"); r.Wait(Game.T(2)); Spawned(r, "police", "beyond", 1);
                    var o = r.Op("scout"); var hp = H(r, "officer-1").Pos(g.Tick);
                    r.Is(!G.Within(o.Pos, hp, o.Gun.W!.MaxCm), "officer-1 is out of the scout's reach");
                    Move(r, "scout", new Pt(300, 3000)); r.Wait(3);
                    var a = FireOn(r, "scout", "officer-1"); r.Is(a.ok, $"fire taken ({a.code})");
                    r.Is(o.CloseWalk && o.Walk != null && G.Within(o.Walk.End, hp, o.Gun.W.EffCm), "the walk-in replaces the walk, into the effective ring");
                });
                AddRange("F8-O3" + sfx, preset, "open", "§8.2 rule 2: a runner given a fire order keeps its run and fires at the run's chance (today mod.running; lab −2 pips)", r =>
                {
                    var g = r.G; r.Do("scout", "draw"); r.Wait(Game.T(2)); Hold(r, "scout"); Spawned(r, "police", "eff", 1);
                    var o = r.Op("scout"); var end = new Pt(o.Pos.X, 3200);
                    Move(r, "scout", end, run: true); r.Wait(2);
                    var a = FireOn(r, "scout", "officer-1"); r.Is(a.ok, $"fire taken while running ({a.code})");
                    r.Is(o.Walk != null && o.Running && o.Walk.End == end, "the run goes on to its end");
                    ShotAtGait(r, "scout", "officer-1", Gait.Run, () => o.Walk == null);
                });
                AddRange("F8-O4" + sfx, preset, "open", "§8.2 rule 4: fire, then a walk: it walks and fires at the walk's chance", r =>
                {
                    var g = r.G; r.Do("scout", "draw"); r.Wait(Game.T(2)); Hold(r, "scout"); Spawned(r, "police", "eff", 1);
                    var o = r.Op("scout"); r.Is(FireOn(r, "scout", "officer-1").ok, "fire taken standing");
                    Move(r, "scout", new Pt(o.Pos.X, 3200)); r.Wait(2);
                    r.Is(o.Walk != null && !o.Running && o.Gun.Order == "officer-1", "it walks, the order stands");
                    ShotAtGait(r, "scout", "officer-1", Gait.Walk, () => o.Walk == null);
                });
                AddRange("F8-O5" + sfx, preset, "open", "§8.2 rule 4: fire, then a run: the order stands (no fire.cancelled)", r =>
                {
                    var g = r.G; r.Do("scout", "draw"); r.Wait(Game.T(2)); Spawned(r, "police", "eff", 1);
                    var o = r.Op("scout"); r.Is(FireOn(r, "scout", "officer-1").ok, "fire taken standing");
                    r.Is(Move(r, "scout", new Pt(o.Pos.X, 3200), run: true).ok, "the run taken"); r.Wait(3);
                    r.Is(o.Running && o.Walk != null, "it runs");
                    r.Eq(o.Gun.Order, "officer-1", "the fire order stands");
                    r.Is(!g.Log.Exists(l => l.Code == "fire.cancelled"), "no fire.cancelled");
                });
                AddRange("F8-SETTLE" + sfx, preset, "open", "§10.2 settle: a runner with a fire order, its run ending, waits aimNew before its next shot; it keeps its target and the pair's count", r =>
                {
                    var g = r.G; r.Do("scout", "draw"); r.Wait(Game.T(2)); Spawned(r, "police", "eff", 1);
                    var o = r.Op("scout"); r.Is(FireOn(r, "scout", "officer-1").ok, "fire taken standing");
                    r.Is(r.Until(() => r.Shots().Count(x => x.by == "scout") >= 2, Game.T(10)), "two standing shots: the count is running");
                    r.Is(Move(r, "scout", new Pt(o.Pos.X, o.Pos.Y + 400), run: true).ok, "a 4 m run taken with the order standing");
                    r.Is(r.Until(() => o.Running && o.Walk != null, 2), "it runs");
                    string target = ""; int acc = -1;
                    while (o.Walk != null && g.Tick < Game.T(600)) { target = o.Gun.Target; acc = o.Gun.Acc; g.Step(); }
                    int end = g.Tick;
                    r.Is(!o.Running && o.Walk == null, $"the run ends at tick {end}");
                    r.Is(o.Gun.NextShot >= end + g.AimNewT, $"the next shot waits aimNew from the run's end ({o.Gun.NextShot - end} ticks, aimNew {g.AimNewT})");
                    r.Eq((o.Gun.Order, o.Gun.Target), ("officer-1", target), "it keeps the order and the target");
                    r.Eq(o.Gun.Acc, acc, "and the pair's count (no reset: §10.2)");
                    int before = r.Shots().Count(x => x.by == "scout");
                    r.Is(r.Until(() => r.Shots().Count(x => x.by == "scout") > before, Game.T(10)), "it fires again standing");
                    var next = r.Shots().First(x => x.by == "scout" && x.tick > end);
                    r.Is(next.tick >= end + g.AimNewT, $"no sooner than aimNew after the run ({next.tick - end} ticks)");
                });
                AddRange("F8-O6" + sfx, preset, "open", "§8.2 rule 4: a walk-in, then a player move that ends out of reach: no second walk-in; the order lapses line-lapse after the move ends", r =>
                {
                    var g = r.G; r.Do("scout", "draw"); r.Wait(Game.T(2)); Hold(r, "scout"); Spawned(r, "police", "beyond", 1);
                    var o = r.Op("scout");
                    r.Is(FireOn(r, "scout", "officer-1").ok && o.CloseWalk, "fire out of reach: the walk-in starts");
                    r.Wait(5);
                    var back = new Pt(300, 600); int arrive = r.SendTo("scout", back);
                    r.Is(o.CloseSpent && !o.CloseWalk && o.Gun.Order == "officer-1", "the player's move spends the walk-in; the order stands");
                    r.To(arrive);
                    r.Is(o.Walk == null && !G.Within(o.Pos, H(r, "officer-1").Pos(g.Tick), o.Gun.W!.MaxCm), "the move ends out of reach");
                    r.Wait(Game.T(g.R.LineLapseS) - 2);
                    r.Is(o.Gun.Order == "officer-1" && o.Walk == null, "it holds its aim, no second walk-in");
                    r.Wait(4);
                    r.Is(o.Gun.Order.Length == 0 && g.Log.Exists(l => l.Code == "fire.lapses"), $"and lapses {g.R.LineLapseS} s after the move ends");
                    r.Is(FireOn(r, "scout", "officer-1").ok && o.CloseWalk, "a new fire order walks in again");
                });
                AddRange("F8-O7" + sfx, preset, "open", "§8.2 rule 3: a driven run fires at the run's chance; driven with no key held, as standing", r =>
                {
                    var g = r.G; r.Do("scout", "draw"); r.Wait(Game.T(2)); Hold(r, "scout"); Spawned(r, "police", "eff", 1);
                    var o = r.Op("scout"); r.Is(FireOn(r, "scout", "officer-1").ok, "fire taken");
                    ShotAtGait(r, "scout", "officer-1", Gait.Run, () => false, () => g.Issue(g.DriveSample(OpId.Scout, new Pt(o.Pos.X, o.Pos.Y + g.R.RunSpeed), true, 90)));
                    r.Is(o.Driven, "driven");
                    ShotAtGait(r, "scout", "officer-1", Gait.Stand, () => false, () => g.Issue(g.DriveSample(OpId.Scout, o.Pos, false, 90)));
                    r.Eq(g.PreviewFire(OpId.Scout, "officer-1").Gait, "stand", "the chip reads stand");
                });
                AddRange("F8-O8" + sfx, preset, "open", "§8.3: the away cone (60°, the edge is away): straight away fires at mod.runningAway (lab 0 pips); the integer predicate either side of 60° and at 61°", r =>
                {
                    var g = r.G;
                    // exact 60° is not reachable by integer vectors (tan 60° is irrational); the predicate is ≥, so the edge counts as away
                    Pt V(double deg) => new Pt((int)Math.Round(Math.Cos(deg * Math.PI / 180) * 100000), (int)Math.Round(Math.Sin(deg * Math.PI / 180) * 100000));
                    var t = new Pt(-100000, 0); // the target is behind: straight away is 0°
                    r.Is(g.R.RunsAway(V(0), t) && g.R.RunsAway(V(59.99), t) && g.R.RunsAway(V(-59.99), t), "inside 60°: away");
                    r.Is(!g.R.RunsAway(V(60.01), t) && !g.R.RunsAway(V(61), t) && !g.R.RunsAway(V(-61), t), "past 60°, and at 61°: across");
                    r.Is(g.R.RunsAway(new Pt(1, 0), new Pt(-1, 0)) && !g.R.RunsAway(new Pt(0, 1), new Pt(-1, 0)) && !g.R.RunsAway(new Pt(-1, 0), new Pt(-1, 0)), "straight away; square across; toward");
                    r.Eq(g.R.RunAwayCosPm, 500, "cos 60° = 500 ‰ at load");
                    r.To(r.SendTo("scout", new Pt(2000, 2100))); r.Do("scout", "draw"); r.Wait(Game.T(2)); Hold(r, "scout"); Spawned(r, "police", "near", 1);
                    var o = r.Op("scout"); var hp = H(r, "officer-1").Pos(g.Tick);
                    r.Is(FireOn(r, "scout", "officer-1").ok, "fire taken");
                    var away = g.S.Grid.NearestFree(new Pt(3800, 2100))!.Value; // straight away from officer-1 (west of the scout)
                    int t0 = g.Tick; Move(r, "scout", away, run: true); g.Step();
                    r.Eq(g.PreviewFire(OpId.Scout, "officer-1").Gait, "run-away", "the chip reads run away");
                    r.Until(() => FirstAcc(r, "scout", "officer-1", t0) >= 0 || o.Walk == null, Game.T(3));
                    int acc = FirstAcc(r, "scout", "officer-1", t0);
                    long d = G.Isqrt(G.D2(o.Pos, hp));
                    int band = d <= g.R.PointBlankCm ? g.R.AccPoint : d <= o.Gun.W!.EffCm ? g.R.AccEff : g.R.AccLong;
                    if (acc >= 0) r.Eq(acc, lab ? g.Labr!.Chance[0] : band * g.R.ModRunningAway / 1000 * g.R.ModUntrained / 1000, "a run straight away: mod.runningAway (lab: 0 pips)");
                    else r.Is(false, "it fired while running away");
                });
                AddRange("F8-O10" + sfx, preset, "open", "§8.3: a hostile walking in a fight fires at the walk's chance (today too): rivals stepping to cover", r =>
                {
                    var g = r.G; r.Do("cell", "panic"); Spawned(r, "rivals", "eff", 3);
                    int moving = 0, bad = 0; string ex = "";
                    for (int k = 0; k < Game.T(40) && moving < 3; k++)
                    {
                        g.Step();
                        foreach (var x in g.Log.Where(l => l.Tick == g.Tick && (l.Code == "shot.hit" || l.Code == "shot.miss") && l.Text.StartsWith("rival-", StringComparison.Ordinal)))
                        {
                            string by = x.Text.Substring(0, x.Text.IndexOf(' ')); var h = H(r, by); if (!h.Moving(g.Tick)) continue;
                            string t = x.Text.Substring(by.Length + 10); t = t.Substring(0, t.IndexOf(' '));
                            var ln = LineOf(r, by, t); int acc = FirstAcc(r, by, t, g.Tick); moving++;
                            if (ln == null || !ln.Moving || acc != ln.Acc || acc != WantAcc(g, h.Gun, ln.Band, Gait.Walk, ln.Cover)) { bad++; ex = $"{by} → {t} {acc} ‰ (line {ln?.Acc}, {ln?.Band}, {ln?.Cover})"; }
                        }
                    }
                    r.Is(moving > 0, $"a rival fires while it walks ({moving} shots on the move)");
                    r.Eq(bad, 0, $"each at the walk's chance{(ex.Length > 0 ? ": " + ex : "")}");
                });
                AddRange("F8-O11" + sfx, preset, "open", "§8.3: a running target takes mod.targetRunning outside point blank and no step inside it", r =>
                {
                    var g = r.G;
                    // inside point blank first: the tech 3 m from the first near slot, then an officer spawns there, then the tech runs
                    r.To(r.SendTo("tech", new Pt(1375, 2075))); Spawned(r, "police", "near", 1);
                    r.Is(Move(r, "tech", new Pt(1375, 1800), run: true).ok, "the tech runs"); g.Step();
                    var l1 = LineOf(r, "officer-1", "tech");
                    if (r.Is(l1 != null && l1.DistanceCm <= g.R.PointBlankCm && g.TargetRunning("tech"), $"the tech runs within point blank ({l1?.DistanceCm} cm)"))
                        r.Eq(l1!.Acc, lab ? g.Labr!.Chance[4] : g.R.AccPoint, "no running-target step at arm's length");
                    // past point blank: the scout runs across at the near end, officer-2 at the effective ring
                    Spawned(r, "police", "eff", 1); var h = H(r, "officer-2"); var o = r.Op("scout");
                    Move(r, "scout", new Pt(o.Pos.X, 3200), run: true); g.Step();
                    r.Is(g.TargetRunning("scout"), "the scout runs this tick");
                    var ln = LineOf(r, "officer-2", "scout");
                    if (!r.Is(ln != null, "officer-2 has a line on the scout")) return;
                    long d = ln!.DistanceCm; int band = d <= g.R.PointBlankCm ? g.R.AccPoint : d <= h.Gun.W!.EffCm ? g.R.AccEff : g.R.AccLong;
                    int want = lab ? g.Labr!.Chance[ln.Pips] * g.R.ModTargetRunning / 1000 : band * g.R.ModTargetRunning / 1000;
                    r.Eq(ln.Acc, want, $"past point blank ({d / 100} m): ×{g.R.ModTargetRunning} ‰");
                    r.Eq(ln.Cover, "none", "and no cover with it (a mover has none)");
                });

                // ---------------------------------------------------------------- the balance cases (§8.7)
                AddRange("F8-RUSH" + sfx, preset, "open", "§8.7: the whole squad runs for a quick takedown: four drawn agents 25 m from one trained officer in the open run to 12 m, fire orders given while running; and the same start walked in", r =>
                {
                    var g = r.G; var h = F8Setup(r, 1, 2500); var off = H(r, "officer-1");
                    int t0 = g.Tick; for (int k = 0; k < 4; k++) Move(r, Cell4R[k], Ray(r, h, new[] { -10.0, -4.0, 2.0, 8.0 }[k], 1200), run: true);
                    foreach (var op in Cell4R) r.Is(FireOn(r, op, "officer-1").ok && r.Op(op).Running, $"{op}: fire given while running, the run goes on");
                    r.Until(() => off.Down, Game.T(30));
                    int rush = g.Tick - t0, hits = HitsOn(r, Cell4R, t0);
                    r.Line(off.Down && rush <= Game.T(8), $"down within 8 s of the run order ({rush / 10.0:0.0} s; Motive's arithmetic {(lab ? 6.2 : 4.8)} s)");
                    r.Line(DownOf(r) == 0, $"no agent down ({DownOf(r)})");
                    r.Line(hits <= 2, $"≤ 2 hits taken ({hits})");
                    int walk = -1;
                    PlayRangeBody(r.Inputs!, g.Start, rb => { F8Setup(rb, 1, 2500); var o1 = H(rb, "officer-1"); int w0 = rb.G.Tick; foreach (var op in Cell4R) FireOn(rb, op, "officer-1"); rb.Until(() => o1.Down, Game.T(40)); if (o1.Down) walk = rb.G.Tick - w0; });
                    if (!lab) r.Line(walk > 0 && rush < walk, $"today: sooner than the walk-in ({rush / 10.0:0.0} s against {walk / 10.0:0.0} s; Motive 4.8 against 8.0)");
                    else r.Is(walk > 0, $"lab: the walk-in downs it too ({walk / 10.0:0.0} s; no line asked)");
                });
                AddRange("F8-RUSH-B" + sfx, preset, "open", "§8.7: a charge costs: against four trained officers, A runs to 4 m firing, B walks in; A loses at least 2 more agents than B", r =>
                {
                    var g = r.G; var h = F8Setup(r, 4, 2500);
                    for (int k = 0; k < 4; k++) Move(r, Cell4R[k], Ray(r, h, new[] { -10.0, -4.0, 2.0, 8.0 }[k], 400), run: true);
                    foreach (var op in Cell4R) FireOn(r, op, "officer-" + (Array.IndexOf(Cell4R, op) + 1));
                    FightOn(r, Game.T(40));
                    int a = DownOf(r);
                    var bRun = PlayRangeBody(r.Inputs!, r.G.Start, rb => { F8Setup(rb, 4, 2500); foreach (var op in Cell4R) FireOn(rb, op, "officer-" + (Array.IndexOf(Cell4R, op) + 1)); FightOn(rb, Game.T(40)); });
                    int b = DownOf(bRun);
                    r.Line(a >= b + 2, $"the charge loses at least 2 more agents than the walk-in (charge {a} down, walk-in {b}; Motive: 4 and 1)");
                });
                AddRange("F8-BREAK" + sfx, preset, "lane", "§8.7, §10.4: run away but take some of them with me: four at 14 m from six trained officers in a hot fight, a straight 60 m lane behind them (layout lane); A breaks at once, B after 2 s of standing fire; standing to the end loses at least 3; firing while breaking takes no fewer hits than ceasing", r =>
                {
                    var g = r.G;
                    Pt hot(Run rr) { F8Setup(rr, 6, 1400); rr.Until(() => Enumerable.Range(1, 6).All(k => H(rr, "officer-" + k).Gun.Target.Length > 0), Game.T(5)); for (int k = 0; k < 4; k++) FireOn(rr, Cell4R[k], "officer-" + (k + 1)); return rr.G.RangeSlot("near", 1); }
                    void brk(Run rr, int standTicks, bool cease)
                    {
                        rr.Wait(standTicks); int end = rr.G.Tick + Game.T(20);
                        if (cease) foreach (var op in Cell4R) rr.G.Issue(new Order { Tick = rr.G.Tick, Op = op, Do = "cease" });
                        while (rr.G.Tick < end) { foreach (var op in Cell4R) { var o = rr.Op(op); if (o.Down || o.Walk != null) continue; var to = rr.G.S.Grid.NearestFree(new Pt(Math.Min(LaneEndCm, o.Pos.X + 1200), o.Pos.Y)) ?? o.Pos; if (to == o.Pos || G.Within(to, o.Pos, 100)) to = rr.G.S.Grid.NearestFree(new Pt(o.Pos.X, o.Pos.Y < 2000 ? Math.Max(200, o.Pos.Y - 800) : Math.Min(3800, o.Pos.Y + 800))) ?? o.Pos; Move(rr, op, to, run: true); } rr.G.Step(); }
                    }
                    int hp(Run rr) => rr.G.Ops.Sum(o => o.Gun.Max - o.Gun.Health);
                    int max = g.Ops.Sum(o => o.Gun.Max);
                    hot(r); brk(r, 0, false);
                    int aDown = DownOf(r), aFoes = g.Hostiles.Count(x => x.Down), aLost = hp(r);
                    r.Line(aDown == 0, $"A: no agent down ({aDown})");
                    r.Line(aLost * 2 <= max, $"A: health lost ≤ 50 % ({aLost * 100 / max} %)");
                    r.Line(aFoes <= 1, $"A: at most one hostile down ({aFoes})");
                    var b = PlayRangeBody(r.Inputs!, g.Start, rb => { hot(rb); brk(rb, Game.T(2), false); });
                    r.Line(DownOf(b) <= 1, $"B: at most one agent down ({DownOf(b)})");
                    r.Line(b.G.Hostiles.Count(x => x.Down) >= 1, $"B: at least one hostile down ({b.G.Hostiles.Count(x => x.Down)})");
                    var st = PlayRangeBody(r.Inputs!, g.Start, rb => { hot(rb); FightOn(rb, Game.T(40)); });
                    r.Line(DownOf(st) >= 3, $"standing and fighting to the end loses at least 3 ({DownOf(st)})");
                    var c = PlayRangeBody(r.Inputs!, g.Start, rb => { hot(rb); brk(rb, 0, true); });
                    int aHits = r.Shots().Count(s => s.hit && Cell4R.Contains(s.target)), cHits = c.Shots().Count(s => s.hit && Cell4R.Contains(s.target));
                    r.Line(aHits >= cHits, $"firing while breaking takes no fewer hits than ceasing ({aHits} against {cHits}; tuning.md §10.4, the line the right way round)");
                    int farX = r.G.Ops.Where(o => !o.Down).Select(o => o.Pos.X).DefaultIfEmpty(0).Max();
                    r.Is(farX < LaneEndCm, $"the lane, not a wall, held the run: the farthest agent up is at x {farX / 100} m, short of the lane's end {LaneEndCm / 100} m");
                });
                AddRange("F8-CIRCLE" + sfx, preset, "open", "§8.7, the cap: the Enforcer runs across at 15.5 m from two trained officers for 30 s, firing; he takes more hits than he deals; standing at the same spot downs both sooner", r =>
                {
                    var g = r.G;
                    Pt setup(Run rr)
                    {
                        var h = rr.G.RangeSlot("near", 1);
                        foreach (var op in new[] { "scout", "fixer", "tech" }) rr.G.Issue(new Order { Tick = rr.G.Tick, Op = op, Do = "move", Point = new Pt(200, op == "scout" ? 300 : op == "fixer" ? 600 : 900) });
                        rr.To(rr.SendTo("enforcer", Ray(rr, h, 0, 1550))); rr.Do("enforcer", "draw"); rr.Wait(Game.T(2));
                        rr.Wait(Game.T(12)); Spawned(rr, "police", "near", 2); FireOn(rr, "enforcer", "officer-1"); return h;
                    }
                    bool both(Run rr) => H(rr, "officer-1").Down && H(rr, "officer-2").Down;
                    var hh = setup(r); int t0 = g.Tick, end = t0 + Game.T(30); double ang = 0; var e = r.Op("enforcer");
                    while (g.Tick < end && !e.Down && !both(r))
                    {
                        if (e.Walk == null) { ang += 400.0 / 1550 * 180 / Math.PI; Move(r, "enforcer", Ray(r, hh, ang, 1550), run: true); }
                        if (e.Gun.Order.Length == 0) FireOn(r, "enforcer", H(r, "officer-1").Down ? "officer-2" : "officer-1");
                        g.Step();
                    }
                    int taken = r.Shots().Count(s => s.tick >= t0 && s.hit && s.target == "enforcer"), dealt = r.Shots().Count(s => s.tick >= t0 && s.hit && s.by == "enforcer");
                    r.Line(taken > dealt, $"circling: he takes more hits than he deals ({taken} taken, {dealt} dealt)");
                    int circleT = both(r) ? g.Tick - t0 : int.MaxValue, standT = int.MaxValue;
                    PlayRangeBody(r.Inputs!, g.Start, rb => { setup(rb); var eo = rb.Op("enforcer"); int s0 = rb.G.Tick, e2 = s0 + Game.T(30); while (rb.G.Tick < e2 && !eo.Down && !both(rb)) { if (eo.Gun.Order.Length == 0) FireOn(rb, "enforcer", H(rb, "officer-1").Down ? "officer-2" : "officer-1"); rb.G.Step(); } if (both(rb)) standT = rb.G.Tick - s0; });
                    r.Line(standT < circleT, $"standing at the same spot downs both sooner (standing {(standT == int.MaxValue ? "not in 30 s" : standT / 10.0 + " s")}, circling {(circleT == int.MaxValue ? "not in 30 s" : circleT / 10.0 + " s")})");
                });
            }
        }
        /// <summary>Steps (each tick `each` first) until `by` fires on `t`, or `stop`, or 5 s; checks the shot at `gait`'s chance by the table
        /// (WantAcc) and the host's line (FireLine) and the chip's gait the same tick.</summary>
        static void ShotAtGait(Run r, string by, string t, Gait gait, Func<bool> stop, Action? each = null)
        {
            var g = r.G; int t0 = g.Tick + 1, acc = -1; FireLine? ln = null; string chip = "";
            for (int k = 0; k < Game.T(5) && acc < 0 && !stop(); k++) { each?.Invoke(); g.Step(); acc = FirstAcc(r, by, t, t0); if (acc >= 0) { ln = LineOf(r, by, t); chip = g.PreviewFire(Sim.Op.Parse(by), t).Gait; } }
            if (!r.Is(acc >= 0 && ln != null, $"{by} fires on {t} at a {gait}")) return;
            int want = WantAcc(g, r.Op(by).Gun, ln!.Band, gait, ln.Cover);
            r.Eq(acc, want, $"at the {gait}'s chance ({ln.Band} band{(g.Lab ? ", pips" : "")})");
            r.Eq(ln.Acc, acc, "the line reads the same chance");
            r.Eq(chip, gait == Gait.Walk ? "walk" : gait == Gait.Run ? "run" : gait == Gait.RunAway ? "run-away" : "stand", "the chip's gait");
        }
        /// <summary>The §8.3 tables, from the rulebook's numbers: today band × gait × cover × untrained; the lab's pips. Target standing.</summary>
        static int WantAcc(Game g, Gun shooter, string band, Gait gait, string cover)
        {
            var R = g.R; bool pb = band == "point", untrained = !shooter.Trained; int cov = pb && !R.PointBlankCover ? 1000 : cover == "full" ? R.ModCover : cover == "partial" ? R.ModCoverPartial : 1000;
            if (g.Lab)
            {
                if (gait == Gait.RunAway) return g.Labr!.Chance[0];
                int p = pb ? 4 : band == "eff" ? 3 : 2; if (!pb) { if (untrained) p--; if (cov < 1000) p--; }
                if (gait == Gait.Walk) p--; if (gait == Gait.Run) p -= g.Labr!.RunPips;
                return g.Labr!.Chance[Math.Max(gait == Gait.Run ? 0 : 1, Math.Min(4, p))];
            }
            long a = pb ? R.AccPoint : band == "eff" ? R.AccEff : R.AccLong;
            a = a * R.GaitMod(gait) / 1000; a = a * cov / 1000; if (untrained) a = a * R.ModUntrained / 1000;
            return (int)a;
        }
        /// <summary>Fight it out where they stand: each agent up, not walking, with no order fires on the nearest officer up.</summary>
        static void FightOn(Run r, int ticks)
        {
            var g = r.G; int end = g.Tick + ticks;
            while (g.Tick < end && g.Hostiles.Any(h => !h.Down) && g.Ops.Any(o => !o.Down))
            {
                foreach (var op in Cell4R) { var o = r.Op(op); if (o.Down || o.Gun.Order.Length > 0) continue; var t = g.Hostiles.Where(h => !h.Down).OrderBy(h => G.D2(h.Pos(g.Tick), o.Pos)).FirstOrDefault(); if (t != null) FireOn(r, op, t.Id); }
                g.Step();
            }
        }
        /// <summary>Another run of the same range start (its own game), for the comparisons inside one scenario.</summary>
        static Run PlayRangeBody(Engine.Inputs i, Start s, Action<Run> body) { var r = new Run(Engine.New(i, s)) { Inputs = i }; body(r); return r; }
    }
}
