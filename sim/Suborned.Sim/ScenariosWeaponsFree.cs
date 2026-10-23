// Engine 0.19.0 scenarios: weapons free (Motive tuning.md §14.5; Sky's §14 note: a fight ends on contact, not a clock) and hostile intent,
// per agent (Pulse briefs/027/pulse/hostile-initiative.md, Sky's answers 2026-10-21). Ids avoid the S prefix (the 28).
//    F14-OPEN      a guard aims at a drawn agent and has not fired: no fight, the agent does not fire
//    F14-ATTACKER  the attacker is taken before a nearer foe
//    F14-STICK     no change of target while the one in hand stays valid
//    F14-X         cease stands the agent down until it is shot at
//    F14-OFF       weapons free off: a drawn agent nobody shoots at holds its fire (0.18.0's return)
//    F14-CONTACT   the fight goes off weapons.free.settle after the last contact (no 30 s clock)
//    F14-4V4       Motive's line: one opening shot, then weapons free, against four officers at 14 m
//    F14-REPLAY    toggled mid-fight, the run replays hash-identical
//    F14-FOCUS     a focus is a lock: lasting, it never walks in; the order round-trips
//    F14-UNDER     the under-fire read: who, since when; a holstered agent fired on is never drawn
//    F12-ROT-WF    F12-ROT under weapons free: the officer still keeps one target (§12.2)
//    HI-SCOUT      the Enforcer goes loud; the holstered Scout in their sight is never fired on
//    HI-SCOUT-ACT  the same, but the Scout draws in their sight: he is
//    HI-FACE       a drop-it warning heeded (+1 T) makes the Scout's face known: at Alert 2 the police fire on him holstered
//    HI-A3         Alert 3 alone fires on nobody: the unknown holstered Scout in an officer's cone is not shot
//    HI-ROBOT      a reported face is fired on by robots and police; the unknown, holstered agents are not
//    HI-RIVALS     the crew sees the street from its arrival: it fires on the drawn Enforcer, never the holstered Scout, and a fight it starts costs no Standing
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static Answer Free(Run r, bool on) => r.G.Issue(new Order { Tick = r.G.Tick, Op = "cell", Do = "free", On = on });
        static List<(int tick, string by, string target, bool hit)> ShotsAt(Run r, string t, int from = 0) => r.Shots().Where(s => s.target == t && s.tick >= from).ToList();

        static void WeaponsFreeRange()
        {
            AddRange("F14-ATTACKER", "today", "lane", "§14.1: an agent attacked by the farther officer takes it before the nearer one, who aims at someone else", r =>
            {
                var g = r.G; var a = g.RangeSlot("near", 1); var b = g.RangeSlot("near", 2);
                // the Tech within 3 m of officer-2 (its nearest drawn agent); the Scout nearer officer-2 than officer-1, yet officer-1's nearest
                // drawn agent; both in officer-1's effective ring on a line. Found on the grid (the lane's geometry), then checked.
                Pt? tech = null, scout = null;
                for (int dx = -400; dx <= 400 && tech == null; dx += 50) for (int dy = -400; dy <= 400 && tech == null; dy += 50)
                { var q = b + new Pt(dx, dy); long db = G.D2(q, b); if (db >= 200L * 200 && db <= 350L * 350 && G.D2(q, a) > G.D2(b, a) && g.S.Grid.Free(q) && g.S.Sight(b, q, g.R.Eye)) tech = q; }
                if (tech == null) { r.Is(false, "a post for the tech beside officer-2"); return; }
                long techA = G.D2(tech.Value, a), techB = G.D2(tech.Value, b);
                for (int dx = -1400; dx <= 1400 && scout == null; dx += 25) for (int dy = -1400; dy <= 1400 && scout == null; dy += 25)
                {
                    var q = a + new Pt(dx, dy); long qa = G.D2(q, a), qb = G.D2(q, b);
                    if (qb < qa && qa < techA && qb > techB && qa <= 1400L * 1400 && g.S.Grid.Free(q) && g.S.Sight(a, q, g.R.Eye) && g.S.Sight(b, q, g.R.Eye)) scout = q;
                }
                if (scout == null) { r.Is(false, $"a post for the scout (officers {G.Isqrt(G.D2(a, b)) / 100.0:0.0} m apart, tech {G.Isqrt(techB) / 100.0:0.0} m from officer-2)"); return; }
                r.To(Math.Max(r.SendTo("tech", tech.Value), r.SendTo("scout", scout.Value)));
                r.Do("scout", "draw"); r.Do("tech", "draw"); r.Wait(Game.T(2));
                Spawned(r, "police", "near", 2);
                var o1 = H(r, "officer-1"); var o2 = H(r, "officer-2"); var s = r.Op("scout");
                r.Is(r.Until(() => o1.Gun.Target == "scout" && o2.Gun.Target == "tech", Game.T(6)), $"officer-1 aims at the scout, officer-2 at the tech ({o1.Gun.Target}/{o2.Gun.Target})");
                r.Is(G.D2(s.Pos, o2.Pos(g.Tick)) < G.D2(s.Pos, o1.Pos(g.Tick)), "officer-2 is the nearer to the scout");
                r.Is(r.Until(() => g.FreeFightOn && s.Gun.Target.Length > 0, Game.T(6)), "the fight is on and the scout picks a target of its own");
                r.Eq(s.Gun.Target, "officer-1", "its attacker, not the nearer officer");
                r.Eq(g.FireStateOf(OpId.Scout).Mode, "weapons-free", "the host reads the pick as weapons free");
                r.Is(g.Log.Exists(l => l.Code == "fire.free" && l.Text.StartsWith("scout picks officer-1", StringComparison.Ordinal)), "and the log says so (fire.free)");
            });

            AddRange("F14-STICK", "today", "lane", "§14.1: the target in hand is kept while it stays a combatant in reach and line", r =>
            {
                var g = r.G; F8Setup(r, 2, 1400);
                r.Is(FireOn(r, "enforcer", "officer-1").ok, "one opening order"); r.Wait(Game.T(1)); r.G.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "cease" });
                var s = r.Op("scout"); string first = ""; int flips = 0, end = g.Tick + Game.T(8);
                while (g.Tick < end && !s.Down)
                {
                    g.Step(); var t = s.Gun.Target;
                    if (t.Length == 0) continue;
                    if (first.Length == 0) { first = t; continue; }
                    var fg = g.GunOf(first)!;
                    if (t != first) { if (!fg.Down && g.PosOf(first).HasValue) flips++; first = t; }
                }
                r.Is(first.Length > 0, "the scout picked a target");
                r.Eq(flips, 0, "it changed target only when the one in hand was down");
            });

            AddRange("F14-X", "today", "lane", "§14.1: cease (X) stands the agent down from weapons free until it is shot at", r =>
            {
                var g = r.G; F8Setup(r, 1, 1400); var f = r.Op("fixer");
                r.Is(FireOn(r, "enforcer", "officer-1").ok, "the opening order");
                r.Is(r.Until(() => g.FreeFightOn && f.Gun.Target == "officer-1", Game.T(5)), "the fight is on and the fixer has picked the officer");
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = "fixer", Do = "cease" }).ok, "X on the fixer");
                int x = g.Tick; r.Eq(g.StandDownOf(OpId.Fixer), x, "it is stood down");
                var off = H(r, "officer-1");
                r.Until(() => off.Down || f.Gun.ShotByTick > x, Game.T(20));
                int shotAt = f.Gun.ShotByTick > x ? f.Gun.ShotByTick : int.MaxValue;
                r.Eq(ShotTicks(r, "fixer", x + 1, Math.Min(shotAt, g.Tick + 1)).Count, 0, "it fires nothing while stood down");
                if (shotAt != int.MaxValue) { r.Wait(Game.T(3)); r.Is(ShotTicks(r, "fixer", shotAt).Count > 0 || off.Down, "shot at, it picks again"); r.Eq(g.StandDownOf(OpId.Fixer), -1, "the stand-down ends"); }
                else r.Is(off.Down, "the officer fell to the others first");
            });

            var offStart = RangePreset("today"); offStart.WeaponsFree = false;
            range!.Add(new Scenario { Id = "F14-OFF", Proto = "brief 029", Layout = "lane", Start = offStart, Intent = "§14.1: weapons free off, a drawn agent nobody fires on holds its fire (0.18.0's return; the BASE gate proves the bytes: briefs/027/keel)", Body = r =>
            {
                var g = r.G; r.Is(!g.WeaponsFree, "weapons free is off at the start");
                F8Setup(r, 1, 1400); int t0 = g.Tick;
                r.Is(FireOn(r, "enforcer", "officer-1").ok, "the opening order");
                var off = H(r, "officer-1"); r.Until(() => off.Down, Game.T(15));
                foreach (var op in new[] { "scout", "fixer", "tech" })
                {
                    int shotAt = r.Shots().Where(x => x.target == op && x.tick >= t0).Select(x => x.tick).DefaultIfEmpty(int.MaxValue - 1).Min();
                    r.Eq(ShotTicks(r, op, t0, shotAt + 1).Count, 0, $"the {op} fires nothing before it is fired on");
                }
                r.Is(!g.Log.Exists(l => l.Code == "fire.free" || l.Code == "fire.free-on"), "no weapons-free pick and no fight-on line");
            } });

            AddRange("F14-CONTACT", "today", "lane", "Sky's §14 note: the fight lasts while there is contact and goes off weapons.free.settle after the last one, not on a 30 s clock", r =>
            {
                var g = r.G; F8Setup(r, 1, 1400);
                r.Is(FireOn(r, "enforcer", "officer-1").ok, "the opening order");
                r.Is(r.Until(() => g.FreeFightOn, Game.T(3)), "the first shot puts the fight on");
                var off = H(r, "officer-1");
                r.Is(r.Until(() => off.Down, Game.T(20)), "the officer goes down");
                int down = g.Tick; int lastShot = r.Shots().Max(x => x.tick);
                r.Is(r.Until(() => !g.FreeFightOn, Game.T(10)), "then the fight goes off");
                r.Eq(g.Tick - lastShot, Game.T(g.R.FreeSettleS), $"weapons.free.settle ({g.R.FreeSettleS} s) after the last shot, no contact left");
                r.Is(g.Log.Exists(l => l.Code == "fire.free-off"), "the log says the fight is off");
                r.Is(g.Tick - down < Game.T(g.R.CrowdQuietS), "well before the public fight's crowd.quiet");
            });

            AddRange("F14-4V4", "today", "lane", "§14.1 measured: four drawn agents 14 m from four trained officers, one opening order, then weapons free (Motive: 5.6 s, 0 down, 44 %)", r =>
            {
                var g = r.G; F8Setup(r, 4, 1400); int t0 = g.Tick;
                string lead = g.Hostiles.Where(h => !h.Down).OrderBy(h => h.Id, StringComparer.Ordinal).First().Id;
                r.Is(FireOn(r, "enforcer", lead).ok, "one opening order");
                r.Until(() => g.Hostiles.All(h => h.Down) || g.Ops.All(o => o.Down), Game.T(40));
                bool won = g.Hostiles.All(h => h.Down);
                int clear = won ? r.Shots().Where(x => x.hit && x.target.StartsWith("officer-", StringComparison.Ordinal)).Max(x => x.tick) - t0 : -1;
                int max = g.Ops.Sum(o => o.Gun.Max), lost = g.Ops.Sum(o => o.Gun.Max - o.Gun.Health);
                r.Is(won, "the cell wins");
                r.Line(clear == 56 && DownOf(r) == 0 && lost * 100 / max == 44, $"Motive §14.1: clear 5.6 s, 0 down, 44 % (got {(clear / 10.0):0.0} s, {DownOf(r)} down, {lost * 100 / max} %)");
            });

            AddRange("F14-REPLAY", "today", "lane", "§14.4: weapons free toggled mid-fight (off, then on) and an X replay hash-identical from the start and the orders", r =>
            {
                r.Wait(1);
                var b = PlayRangeBody(r.Inputs!, r.G.Start, rb =>
                {
                    var g = rb.G; F8Setup(rb, 3, 1400);
                    FireOn(rb, "enforcer", "officer-1"); rb.Wait(Game.T(2));
                    Free(rb, false); rb.Wait(Game.T(3)); Free(rb, true); rb.Wait(Game.T(2));
                    g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "cease" }); rb.Wait(Game.T(6)); g.Finish();
                });
                var rep = Engine.Replay(r.Inputs!, b.G);
                r.Eq(b.G.Orders.Count(o => o.order.Do == "free"), 2, "the session logs both free orders");
                r.Is(b.G.Log.Exists(x => x.Code == "fire.free"), "weapons-free picks were derived");
                r.Is(rep.Checkpoints.SequenceEqual(b.G.Checkpoints) && rep.Checkpoints.Count == b.G.Checkpoints.Count, "every checkpoint is hash-identical");
                r.Eq(rep.Result, b.G.Result, "with the same result");
                r.Eq(r.G.Issue(new Order { Tick = r.G.Tick, Op = "scout", Do = "free", On = true }).code, "free-not-cell", "weapons free is the cell's setting");
            });

            AddRange("F14-FOCUS", "today", "lane", "§14.1: a focus click is a lasting fire order that never walks in; the order shape round-trips", r =>
            {
                var g = r.G; F8Setup(r, 2, 1400); var s = r.Op("scout");
                var o = new Order { Tick = g.Tick, Op = "scout", Do = "fire", Target = "officer-2", Focus = true };
                var back = Order.Parse(JNode.Parse(o.ToString())); r.Is(back.Focus && back.Do == "fire" && back.ToString() == o.ToString(), $"the order round-trips ({o})");
                r.Is(g.Issue(o).ok, "the focus is taken");
                r.Is(s.CloseSpent && !s.CloseWalk && s.Gun.Order == "officer-2", "it is a lock: its walk-in spent");
                var off2 = H(r, "officer-2");
                r.Until(() => off2.Down || s.Down, Game.T(20));
                r.Is(ShotsAt(r, "officer-2").Exists(x => x.by == "scout"), "the scout fires on its focus");
                if (off2.Down) { r.Wait(2); r.Eq(s.Gun.Order, "", "the focus ends with its target down"); }
            });

            AddRange("F14-UNDER", "today", "lane", "Sky's answer 1 (hostile-initiative.md): a holstered agent fired on is never drawn by it, and the host reads who is shooting at it and since when", r =>
            {
                var g = r.G; var h = g.RangeSlot("near", 1);
                int t = r.SendTo("scout", Ray(r, h, 0, 1000)); int k0 = 0; foreach (var op in new[] { "enforcer", "fixer", "tech" }) t = Math.Max(t, r.SendTo(op, Ray(r, h, 0, 3500 + 300 * k0++)));
                r.To(t); Spawned(r, "police", "near", 1); var s = r.Op("scout");
                r.Is(r.Until(() => ShotsAt(r, "scout").Count >= 2, Game.T(10)), "the officer fires on the holstered scout (the range's every-operative rule)");
                var uf = g.UnderFireOf(OpId.Scout);
                r.Is(uf.Count == 1 && uf[0].By == "officer-1" && uf[0].Since == ShotsAt(r, "scout")[0].tick && uf[0].Last == ShotsAt(r, "scout").Last().tick, $"UnderFireOf: officer-1, since its first shot ({(uf.Count > 0 ? uf[0].Since : -1)})");
                r.Is(!s.Gun.Drawn, "the scout is not drawn by it (no auto-draw)");
                r.Eq(g.UnderFireOf(OpId.Fixer).Count, 0, "nobody shoots at the fixer");
            });

            AddRange("F12-ROT-WF", "today", "lane", "§12.2 under weapons free (Motive §14.5): three drawn shooters 15.3 m from one officer; whatever they fire, the officer keeps one target", r =>
            {
                var g = r.G; var h = g.RangeSlot("near", 1); var ops = new[] { "scout", "fixer", "tech" };
                int t = r.SendTo("enforcer", Ray(r, h, 0, 3500));
                for (int k = 0; k < 3; k++) t = Math.Max(t, r.SendTo(ops[k], Ray(r, h, new[] { -6.0, 0.0, 6.0 }[k], 1530)));
                r.To(t); foreach (var op in ops) r.Do(op, "draw"); r.Wait(Game.T(2));
                Spawned(r, "police", "near", 1); var off = H(r, "officer-1");
                FireOn(r, "scout", "officer-1");
                int end = g.Tick + Game.T(12); var seen = new List<string>();
                while (g.Tick < end && !off.Down && ops.All(o => !r.Op(o).Down)) { g.Step(); if (off.Gun.Target.Length > 0) seen.Add(off.Gun.Target); }
                int flips = 0; for (int k = 1; k < seen.Count; k++) if (seen[k] != seen[k - 1]) flips++;
                r.Is(seen.Count > 0, "the officer takes a target");
                r.Eq(flips, 0, "and keeps it");
            });
        }

        /// <summary>N48's seen takedown: a terrace customer knocked out in front of the others at 4:00, the Scout holstered beside the Enforcer.</summary>
        static void TerraceTakedown(Run r)
        {
            var g = r.G; const string Victim = "p-pylon-row-east-05";
            r.To(T(240));
            var seat = g.CrowdPos(WalkerById(r, Victim)) ?? throw new InvalidOperationException($"{Victim} is not on the street at 4:00");
            var spot = Beside(r, seat) ?? throw new InvalidOperationException("no ground beside the terrace table");
            r.To(Math.Max(r.SendTo(Enf, spot), r.SendTo(Scout, Beside(r, spot) ?? spot)));
            r.Takedown(Victim, "knockout");
        }

        static void IntentCases()
        {
            Add("F14-OPEN", "—", "§14.2: a guard engaging a seen draw aims for his 2 s react and has not fired: no fight is on, so weapons free fires nothing (a stance never starts a loud run)", r =>
            {
                var g = r.G;
                // Alert 2 first, by a real act: the Fixer draws on the pickup camera (a camera gives no warning) and holsters, far from the door
                r.To(Math.Max(r.Send(Enf, "d:Office approach"), r.Send(Fixer, "d:Official drop-off")));
                r.Is(g.CameraWatching("pickup", r.Op(Fixer).Pos) && !g.GuardWatching(r.Op(Fixer).Pos, false), "the Fixer stands on the pickup camera, out of the guard's cone");
                r.Do(Fixer, "draw"); r.Is(r.Until(() => g.Alert >= 2, T(3)), "the camera shows a drawn weapon: Alert 2"); r.Do(Fixer, "holster"); r.Wait(T(2));
                r.Eq(r.Shots().Count, 0, "no shot yet");
                r.Do(Enf, "draw"); // at Alert 2 a seen draw is drawn-seen at once (no warning)
                r.Is(r.Until(() => g.GuardGun.Target == Enf, T(3)), "the guard sees the draw and aims");
                int aim = g.Tick;
                r.Until(() => r.Shots().Count > 0, T(4));
                var first = r.Shots().FirstOrDefault();
                r.Eq(first.by, GuardR, "the guard's shot is the first");
                r.Is(!r.Shots().Exists(x => x.by == Enf && x.tick <= first.tick), "the Enforcer fired nothing while the guard only aimed");
                r.Is(r.Until(() => r.Shots().Exists(x => x.by == Enf), T(4)), "the guard's shot puts the fight on: the Enforcer answers");
            });

            // the Enforcer goes loud on the guard from the Office approach; the Scout stands holstered nearby, in the hostiles' sight
            void Loud(Run r, Pt scoutAt)
            {
                var g = r.G;
                r.To(Math.Max(r.Send(Enf, "d:Office approach"), r.SendTo(Scout, scoutAt)));
                r.Fire(Enf, GuardR);
            }
            bool SeenByHostile(Game g, Pt p) => g.GuardWatching(p, false) || g.SecurityWatching(p) || g.PatrolWatching(p) || g.RivalsStreetWatching(p);
            Pt ScoutPost(Run r)
            {
                var g = r.G; var ap = r.Stand("d:Office approach");
                for (int d = 200; d <= 1200; d += 50) foreach (var dir in new[] { new Pt(1, 0), new Pt(-1, 0), new Pt(1, -1), new Pt(-1, -1), new Pt(0, -1), new Pt(1, 1), new Pt(-1, 1) })
                { var q = ap + new Pt(dir.X * d, dir.Y * d); if (g.S.Grid.Free(q) && g.S.Grid.Route(r.Op(Scout).Pos, q) != null && g.GuardWatching(q, false) && !g.CameraWatching("entrance", q) && !g.CameraWatching("pickup", q)) return q; }
                throw new InvalidOperationException("no post in the guard's cone beside the Office approach");
            }

            Add("HI-SCOUT", "—", "Sky 2026-10-21 (hostile-initiative.md answer 2): the Enforcer goes in wild; the holstered Scout in the hostiles' sight is never fired on", r =>
            {
                var g = r.G; Loud(r, ScoutPost(r)); int t0 = g.Tick; bool seen = false;
                int end = g.Tick + T(70);
                while (g.Tick < end && !g.Over) { if (SeenByHostile(g, r.Op(Scout).Pos)) seen = true; g.Step(); }
                r.Is(r.Shots().Exists(x => x.target == Enf), "the Enforcer is fired on");
                r.Is(seen, "a hostile had the Scout in its sight");
                r.Eq(ShotsAt(r, Scout, t0).Count, 0, "nobody fires on the Scout: he did nothing they saw");
                r.Is(!g.KnownFaces.Contains(Scout), "his face is not known");
            });

            Add("HI-SCOUT-ACT", "—", "hostile-initiative.md answer 2: the same, but the Scout draws in their sight: he is fired on for his own act", r =>
            {
                var g = r.G; Loud(r, ScoutPost(r));
                r.Until(() => SeenByHostile(g, r.Op(Scout).Pos) && g.GuardDown == false, T(5));
                r.Do(Scout, "draw");
                r.Is(r.Until(() => ShotsAt(r, Scout).Count > 0 || g.Over, T(60)), "the drawn Scout is fired on");
            });

            Add("HI-FACE", "—", "Pulse §2.1 with Sky's answers: a drawn weapon at Alert 0-1 gets the drop-it warning; holstered in time it costs +1 T and the face is known; from Alert 2 the police fire on that face, holstered", r =>
            {
                var g = r.G;
                r.To(Math.Max(r.SendTo(Scout, ScoutPost(r)), r.Send(Enf, "d:Office approach")));
                r.Is(g.GuardWatching(r.Op(Scout).Pos), "the guard has the Scout in his cone");
                int trail = g.Trail;
                r.Do(Scout, "draw");
                r.Is(r.Until(() => g.Warnings.Count > 0, T(2)), "the guard shouts: drop it (a warning, not drawn-seen)");
                r.Eq(g.Alert, g.R.Intent.WarnAlert, "Alert to the warning's level");
                r.Is(g.KnownFaces.Contains(Scout), "the guard has his face");
                r.Do(Scout, "holster");
                r.Is(r.Until(() => g.Warnings.Count == 0, T(3)), "the window closes");
                r.Eq(g.Trail, trail + g.R.Intent.WarnTrail, "heeded: +1 T (the watcher logs it)");
                r.Eq(ShotsAt(r, Scout).Count, 0, "and nobody fired on him");
                r.Fire(Enf, GuardR); // the Enforcer goes loud: Alert 2
                r.Is(r.Until(() => g.Alert >= 2, T(5)), "Alert 2");
                r.Is(r.Until(() => ShotsAt(r, Scout).Count > 0 || g.Over, T(90)), "his known face is fired on, holstered, from Alert 2");
                r.Is(!r.Op(Scout).Gun.Drawn || ShotsAt(r, Scout).Count > 0, "for his own face, not for the Enforcer's acts");
            });

            Add("HI-A3", "—", "Sky 2026-10-21 (answer 2): Alert 3 is not anyone in a cone: at lockdown (a seen takedown reported), an officer or robot that has the unknown, holstered Scout in its sight does not fire on him", r =>
            {
                var g = r.G; TerraceTakedown(r); r.Done(Enf); r.Send(Enf, "crew:enforcer"); // his face will be on the report: he leaves the block
                r.Is(r.Until(() => g.Alert >= 3, T(60)), "the report lands: Alert 3");
                r.Is(r.Until(() => g.Units.Exists(u => !u.Legacy && !u.Gun.Down && u.OnStreet(g.Tick)) || g.Over, T(60)), "the response's units are on the street");
                var unit = g.Units.First(u => !u.Legacy && !u.Gun.Down && u.OnStreet(g.Tick)); int t0 = g.Tick; bool seen = false;
                int end = g.Tick + T(60), next = g.Tick;
                while (g.Tick < end && !g.Over && !unit.Gun.Down)
                {
                    if (g.Tick >= next && g.UnitPose(unit) is var up && up.HasValue) // the Scout keeps walking into its cone, 6 m ahead of it
                    {
                        var (pos, f) = up.Value; long fl = Math.Max(1, G.Isqrt((long)f.X * f.X + (long)f.Y * f.Y));
                        var ahead = new Pt(pos.X + (int)(f.X * 600 / fl), pos.Y + (int)(f.Y * 600 / fl));
                        if (g.S.Grid.NearestFree(ahead) is Pt q) g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "move", Point = q });
                        next = g.Tick + T(3);
                    }
                    foreach (var w in g.WatcherIds()) if (!w.StartsWith("camera:", StringComparison.Ordinal) && w != "rivals" && g.SeesNow(w, r.Op(Scout).Pos)) seen = true;
                    g.Step();
                }
                r.Is(seen, "an armed watcher had him in its sight at Alert 3");
                r.Eq(ShotsAt(r, Scout, t0).Count, 0, "nobody fires on him");
            });

            Add("HI-ROBOT", "—", "Sky's answer 4: the witnesses' report carries the Enforcer's face; at Alert 3 the robots and police fire on it; the unknown, holstered agents are never fired on", r =>
            {
                var g = r.G; TerraceTakedown(r);
                r.Is(r.Until(() => g.KnownFaces.Contains(Enf), T(60)), "a report lands with the Enforcer's face");
                r.Is(!g.KnownFaces.Contains(Scout), "the Scout, beside him and holstered, is not on it");
                int t0 = g.Tick;
                r.Is(r.Until(() => r.Shots().Exists(x => x.target == Enf && (x.by.StartsWith("robot-", StringComparison.Ordinal) || x.by == "patrol")) || g.Over, T(120)), "a robot fires on the known face");
                foreach (var op in new[] { Scout, Fixer, Tech }) r.Eq(ShotsAt(r, op, t0).Count, 0, $"nobody fires on the {op}");
            });

            Add("HI-RIVALS", "—", "Sky's answer 3: the crew watches the street from its arrival; calm, it fires on the drawn Enforcer in its cone and never on the holstered Scout beside him; a fight it starts costs the cell no Standing", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Enf, new Pt(8600, -3400)));
                r.Is(r.Until(() => g.RivalsPresent, T(g.S.RivalsArrive) + T(5) - g.Tick), $"the rival crew is here at {Report.Clock(g.Tick)}");
                var park = g.S.Stand(g.S.RivalsPark);
                var e = Beside(r, park) ?? park; var sc = Beside(r, e) ?? e;
                r.To(Math.Max(r.SendTo(Enf, e), r.SendTo(Scout, sc)));
                r.Is(g.RivalsStreetWatching(r.Op(Enf).Pos) && g.RivalsStreetWatching(r.Op(Scout).Pos), "both stand in the crew's cone");
                r.Is(g.SeesNow("rivals", r.Op(Enf).Pos), "and the host's cone shows it (the Scout's skill: learned at arrival)");
                r.Wait(T(5));
                r.Eq(r.Shots().Count, 0, "holstered, nobody is fired on");
                r.Do(Enf, "draw");
                r.Is(r.Until(() => r.Shots().Exists(x => x.by == "rivals") || g.Over, T(6)), "the crew fires on the drawn Enforcer");
                r.Is(r.Shots().First().by == "rivals" && r.Shots().First().target == Enf, "the first shot is theirs, at him");
                r.Wait(T(8));
                r.Eq(ShotsAt(r, Scout).Count(x => x.by == "rivals"), 0, "never at the holstered Scout");
                r.Is(!FightMoves(g).Exists(m => m.delta < 0), "a fight the rivals start costs no Standing");
                var fight = g.OpenFight ?? g.Fights.LastOrDefault();
                r.Is(fight != null && fight.RivalsFirst == "rivals", "the fight's first shot is the rivals' (fight.public: fired first is the cell's only)");
            });
        }
    }
}
