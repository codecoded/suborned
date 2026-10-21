// Suture, brief 027 balance probe. SCRATCH COPY ONLY (out/suture027/src): never in sim/. The matrix runs of ScenariosFightLab.cs with a
// smarter scripted player: focus fire on one target, the accumulator reset (cease + fire again after each shot), and a stand-off band
// (fire from beyond the hostiles' effective ring). Orders only, so every run replays from its log.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public sealed class Pol
    {
        public bool Cycle, Focus, Reset, Kite, EnfFires, EnfKite; public string Name = "";
        public static Pol Parse(string s) => new Pol { Name = s, Focus = s.Contains("focus"), Reset = s.Contains("reset") || s.Contains("cycle"), Cycle = s.Contains("cycle"), Kite = s.Contains("kite") && !s.Contains("enfkite") || s.Contains("allkite"), EnfKite = s.Contains("enfkite") || s.Contains("allkite") };
    }

    public static partial class Scenarios
    {
        [ThreadStatic] static Pol? pol;
        [ThreadStatic] static Dictionary<string, int>? lastNext; static readonly Dictionary<string, string> cyc = new Dictionary<string, string>();
        public static int Resets, Kites;

        public static Run PlaySmart(Engine.Inputs i, string id, Start start, IReadOnlyDictionary<string, string> routes, Pol p)
        {
            pol = p; lastNext = new Dictionary<string, int>(); Resets = 0; Kites = 0;
            var r = new Run(Engine.New(i, start)); var g = r.G;
            if (g.Lab && g.Labr != null && (start.LabField ?? g.Labr.Field))
                r.PerTick = () =>
                {
                    foreach (var op in g.Ops)
                        if (!op.Down && !op.Inside && !op.InCar && op.Busy == null && op.Gun.Health * 2 < op.Gun.Max && op.FieldUntil <= g.Tick && op.Charge >= g.Labr.FieldCost && g.Combatants().Exists(c => g.GunOf(c)?.Target == Sim.Op.Name(op.Id)))
                            g.Issue(new Order { Tick = g.Tick, Op = Sim.Op.Name(op.Id), Do = "field" });
                };
            switch (id)
            {
                case "L1c": SLoudFront(r, loop: true); break;
                case "L3": SLoudGuardShot(r, routes["route-g-fast-board"]); break;
                case "P-A": SPrepared(r, routes["route-a-board"], 2490); break;
                case "P-G": SPrepared(r, routes["route-g-fast-board"], 2066); break;
                case "P-B": SPrepared(r, routes["route-b-board"], 4470); break;
                default: throw new ArgumentException($"no smart run {id}");
            }
            if (!g.Over) g.RunTo(Math.Max(g.Tick, T(g.S.Over)) + T(1));
            g.Finish();
            pol = null;
            return r;
        }

        static bool Free(Op e) => !(e.Down || e.Inside || e.InCar || e.Busy != null || e.Gun.W == null);

        /// <summary>As Run.Engage, with the policy: focus (one common target, the weakest any of the ops can reach), reset (cease + fire after
        /// each shot when the accumulator is below acc.start), kite (never stand inside a foe's effective ring; fire only from within max).</summary>
        public static void SEngage(Run r, Func<List<string>> targets, Func<bool> done, int limitS, string[] ops, bool carToEnforcer, bool advance = true)
        {
            var G0 = r.G; var p = pol ?? new Pol(); int end = G0.Tick + Game.T(limitS);
            while (!done() && !G0.Over && G0.Tick < end)
            {
                var ts = targets().Where(id => G0.PosOf(id).HasValue && !G0.GunOf(id)!.Down).ToList();
                string focus = "";
                if (p.Focus && ts.Count > 0)
                {
                    int bh = int.MaxValue; long bd = long.MaxValue;
                    foreach (var id in ts)
                    {
                        var at = G0.PosOf(id)!.Value; int reach = 0; long sum = 0;
                        foreach (var name in ops) { var e = r.Op(name); if (!Free(e)) continue; long d = G.D2(e.Pos, at); sum += d; if (d <= (long)e.Gun.W!.MaxCm * e.Gun.W.MaxCm && G0.S.Sight(e.Pos, at, G0.R.Eye)) reach++; }
                        if (reach == 0) continue;
                        int h = G0.GunOf(id)!.Health;
                        if (h < bh || (h == bh && sum < bd)) { bh = h; bd = sum; focus = id; }
                    }
                }
                foreach (var name in ops)
                {
                    var e = r.Op(name); var w = e.Gun.W;
                    if (!Free(e)) continue; if (w == null) continue;
                    // reset: a shot just went (NextShot moved); the accumulator under acc.start: cease, and fire again on the same target
                    if (p.Reset && e.Gun.Order.Length > 0)
                    {
                        lastNext!.TryGetValue(name, out int ln);
                        if (e.Gun.NextShot != ln) { lastNext[name] = e.Gun.NextShot; if (p.Cycle && e.Gun.Acc < G0.R.AccStart && e.Gun.Target == e.Gun.Order) { string alt = ""; foreach (var id in ts) { if (id == e.Gun.Order) continue; var at = G0.PosOf(id)!.Value; if (G.D2(e.Pos, at) <= (long)(w.MaxCm - 50) * (w.MaxCm - 50) && G0.S.Sight(e.Pos, at, G0.R.Eye)) { alt = id; break; } } if (alt.Length > 0) { G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "fire", Target = alt }); Resets++; cyc[name] = alt; continue; } }
                        if (e.Gun.Acc < G0.R.AccStart && e.Gun.Target == e.Gun.Order) { string t0 = e.Gun.Order; G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "cease" }); G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "fire", Target = t0 }); Resets++; } }
                    }
                    string near = ""; Pt np = default; long bd = long.MaxValue;
                    foreach (var id in ts) { var at = G0.PosOf(id)!.Value; long d = G.D2(e.Pos, at); if (d < bd) { bd = d; near = id; np = at; } }
                    if (near.Length == 0) { if (carToEnforcer && name == "enforcer" && e.Walk == null) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = G0.S.Stand(G0.S.GetawayCar) }); continue; }
                    if (name == "enforcer" && G0.Vale == ValeAt.WithEnforcer && p.EnfKite)
                    {
                        string thr = ""; Pt tp = default; long td = long.MaxValue;
                        foreach (var id in ts) { var at = G0.PosOf(id)!.Value; var fw = G0.GunOf(id)!.W; if (fw == null) continue; long d = G.D2(e.Pos, at); long rr = fw.EffCm + 100; if (d <= rr * rr && d < td) { td = d; thr = id; tp = at; } }
                        if (thr.Length > 0) { long dx = e.Pos.X - tp.X, dy = e.Pos.Y - tp.Y; double n = Math.Sqrt((double)dx * dx + (double)dy * dy); if (n < 1) n = 1; var away = G0.S.Grid.NearestFree(new Pt(e.Pos.X + (int)(dx / n * 400), e.Pos.Y + (int)(dy / n * 400))); if (away.HasValue && (e.Walk == null || !G.Within(e.Walk.End, away.Value, 150))) { G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = away.Value }); Kites++; } }
                        else if (e.Walk != null) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = e.Pos });
                        continue;
                    }
                    if (name == "enforcer" && G0.Vale == ValeAt.WithEnforcer) { if (e.Walk != null) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = e.Pos }); continue; }
                    // kite: the nearest foe whose effective ring we stand in (plus 1 m): step 3 m straight away from it
                    if (p.Kite)
                    {
                        string thr = ""; Pt tp = default; long td = long.MaxValue;
                        foreach (var id in ts) { var at = G0.PosOf(id)!.Value; var fw = G0.GunOf(id)!.W; if (fw == null) continue; long d = G.D2(e.Pos, at); long rr = fw.EffCm + 100; if (d <= rr * rr && d < td) { td = d; thr = id; tp = at; } }
                        if (thr.Length > 0)
                        {
                            long dx = e.Pos.X - tp.X, dy = e.Pos.Y - tp.Y; double n = Math.Sqrt((double)dx * dx + (double)dy * dy); if (n < 1) n = 1;
                            var away = G0.S.Grid.NearestFree(new Pt(e.Pos.X + (int)(dx / n * 400), e.Pos.Y + (int)(dy / n * 400)));
                            if (away.HasValue && (e.Walk == null || !G.Within(e.Walk.End, away.Value, 150))) { G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = away.Value }); Kites++; }
                        }
                        else if (e.Walk != null && e.Gun.Order.Length > 0 && G0.PosOf(e.Gun.Order) is Pt op2 && G.Within(e.Pos, op2, w.MaxCm - 100) && G0.S.Sight(e.Pos, op2, G0.R.Eye)) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = e.Pos });
                    }
                    string tgt = near; Pt tpos = np; long tdist = bd;
                    if (focus.Length > 0) { var fa = G0.PosOf(focus)!.Value; long fd = G.D2(e.Pos, fa); if (fd <= (long)w.MaxCm * w.MaxCm && G0.S.Sight(e.Pos, fa, G0.R.Eye)) { tgt = focus; tpos = fa; tdist = fd; } }
                    bool line = G0.S.Sight(e.Pos, tpos, G0.R.Eye);
                    bool inMax = line && tdist <= (long)(w.MaxCm - 50) * (w.MaxCm - 50);
                    bool inEff = line && tdist <= (long)(w.EffCm - 200) * (w.EffCm - 200);
                    if (inMax && e.Gun.Order != tgt && (p.Focus || p.Kite || e.Gun.Order.Length == 0)) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "fire", Target = tgt });
                    else if (!p.Kite && e.Gun.Order.Length == 0 && line && tdist <= (long)w.MaxCm * w.MaxCm && (inEff || (advance && e.Walk == null) || G0.GunOf(tgt)!.Target == name)) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "fire", Target = tgt });
                    if (!advance) continue;
                    bool reach = p.Kite ? inMax : inEff;
                    if (!reach)
                    {
                        if (p.Kite && e.Walk != null) continue; // a kite step in progress
                        var to = G0.S.Grid.NearestFree(tpos);
                        if (to.HasValue && (e.Walk == null || !G.Within(e.Walk.End, to.Value, 200))) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = to.Value });
                    }
                    else if (e.Walk != null && !p.Kite) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = e.Pos });
                }
                r.PerTick?.Invoke();
                G0.Step();
            }
        }

        static void SHold(Run r, Func<bool> done, int limitS, bool advance, params string[] ops) => SEngage(r, () => Foes(r), done, limitS, ops, false, advance);

        static void SFightItOut(Run r, int limitS, string[] with)
        {
            var G0 = r.G;
            Func<bool> free = () => !G0.CarHeld() && G0.FightOverTick != int.MaxValue / 4;
            var ops = new List<string> { "enforcer" }; ops.AddRange(with);
            int end = G0.Tick + Game.T(limitS); var car = G0.S.At(G0.S.GetawayCar);
            while (!G0.Over && G0.Tick < end)
            {
                SEngage(r, () => { var l = new List<string>(); foreach (var h in G0.Hostiles) if (h.Present && !h.Down) l.Add(h.Id); l.AddRange(r.CityGunmen()); return l; }, free, Math.Max(1, (end - G0.Tick) / Game.Hz), ops.ToArray(), true);
                if (G0.Over || G0.Tick >= end) return;
                if (!G.Within(r.Op("enforcer").Pos, car, G0.R.AtReachCm))
                {
                    int at = r.SendTo("enforcer", G0.S.Stand(G0.S.GetawayCar));
                    while (!G0.Over && G0.Tick < at && free()) G0.Step();
                }
                if (free() && G.Within(r.Op("enforcer").Pos, car, G0.R.AtReachCm)) return;
            }
        }

        static void SFightToCar(Run r)
        {
            var g = r.G; if (g.Over || g.Vale != ValeAt.WithEnforcer) return;
            DrawAll(r);
            g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", To = "d:Getaway bay" });
            SFightItOut(r, 150, new[] { Scout, Fixer, Tech });
            CarryDowned(r);
            if (!g.Over) r.GetAway(Cell4);
        }
        static void SEnterAndOut(Run r)
        {
            var g = r.G; if (g.Over) return;
            int at = r.Send(Enf, "d:Office entrance");
            SHold(r, () => g.Tick >= at || r.Op(Enf).Down, 60, false, Scout, Fixer, Tech);
            if (g.Over) return;
            for (int k = 0; k < T(20) && !g.Over; k++) { if (g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "enter" }).ok) break; SHold(r, () => true, 1, false, Scout, Fixer, Tech); g.Step(); }
            SHold(r, () => r.Op(Enf).Busy == null && !r.Op(Enf).Inside, 200, pol != null && pol.Kite, Scout, Fixer, Tech);
        }
        static void SLoudFront(Run r, bool loop)
        {
            var g = r.G; r.To(T(1)); PanicNow(r);
            if (loop) { r.To(r.Send(Tech, "cabinet")); r.Do(Tech, "loop", "entrance"); r.Done(Tech); }
            if (g.Over) return;
            r.SendTo(Enf, BesideDoor);
            foreach (var (op, dx, dy) in new[] { (Scout, -400, 0), (Fixer, -400, 300), (Tech, -400, -300) }) { if (loop && op == Tech) continue; var q = g.S.Grid.NearestFree(BesideDoor + new Pt(dx, dy)); if (q.HasValue) r.SendTo(op, q.Value); }
            SHold(r, () => r.Op(Enf).Walk == null || r.Op(Enf).Down, 120, false, Scout, Fixer, Tech);
            if (g.Over) return;
            if (!g.GuardDown) { foreach (var op in Cell4) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "fire", Target = GuardR }); SHold(r, () => g.GuardDown, 60, false, Scout, Fixer, Tech); }
            SEnterAndOut(r); SFightToCar(r);
        }
        static void SLoudGuardShot(Run r, string routeG)
        {
            var g = r.G; Replay(r, routeG, 1236);
            if (g.Over) return;
            g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "fire", Target = GuardR });
            DrawAll(r);
            if (pol != null && pol.Focus) foreach (var op in new[] { Scout, Fixer, Tech }) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "fire", Target = GuardR });
            SHold(r, () => g.GuardDown, 60, false, Scout, Fixer, Tech);
            if (pol != null && pol.Name.Contains("clear")) // the whole cell, the carbine too, clears the response unit before the Enter
            {
                int quiet = -1;
                SHold(r, () => { if (Foes(r).Count > 0) quiet = g.Tick; return (g.SecGuns.Length > 0 && g.SecGuns.All(x => x.Down)) || (quiet >= 0 && g.Tick - quiet >= T(10) && g.Tick > T(190)) || g.Tick >= T(200); }, 90, true, Cell4);
            }
            SEnterAndOut(r); SFightToCar(r);
        }
        static void SPrepared(Run r, string script, int fightTick)
        {
            var g = r.G; Replay(r, script, fightTick);
            if (g.Over) return;
            if (g.Vale != ValeAt.WithEnforcer) { r.Until(() => g.Vale == ValeAt.WithEnforcer, T(60)); if (g.Over) return; }
            Support(r); ToAmbush(r);
            SFightItOut(r, 150, new[] { Scout, Fixer, Tech });
            CarryDowned(r);
            if (!g.Over) r.GetAway(Cell4);
        }

        // ---------------------------------------------------------------- micro probes on a bare lab start

        /// <summary>Hit pattern of one shooter on one target at a fixed pip chance: plain (keep aim) vs cease-refire (reset to acc.start).</summary>
        public static (string plain, string reset) Pattern(int acc, int start, int shots)
        {
            string a = "", b = ""; int x = start;
            for (int k = 0; k < shots; k++) { x += acc; bool h = x >= 1000; if (h) x -= 1000; a += h ? "●" : "○"; }
            for (int k = 0; k < shots; k++) { int y = start + acc; b += y >= 1000 ? "●" : "○"; }
            return (a, b);
        }
    }
}
namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>Panic spam beside the guard: panic (all draw), holster after `holdTicks`, `cycles` times, every `gapTicks`. Before any shot.</summary>
        public static string PanicSpam(Engine.Inputs i, int cycles, int holdTicks, int gapTicks)
        {
            var r = new Run(Engine.New(i, LabStart(s => s.Security = 1))); var g = r.G;
            r.To(Max(r.Send(Tech, "cabinet"))); r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
            var post = g.PosOf(GuardR)!.Value; Pt? spot = null;
            for (int dx = -800; dx <= 800 && spot == null; dx += 100) for (int dy = -800; dy <= 800 && spot == null; dy += 100)
            { var q = new Pt(post.X + dx, post.Y + dy); if (g.S.Grid.Free(q) && G.D2(q, post) >= 300L * 300 && g.GuardWatching(q) && !g.PatrolWatching(q) && !g.CameraWatching("pickup", q)) spot = q; }
            if (spot == null) return "no spot";
            int a1 = r.SendTo(Scout, spot.Value), a2 = r.SendTo(Fixer, g.S.Grid.NearestFree(spot.Value + new Pt(150, 0)) ?? spot.Value); r.To(Math.Max(a1, a2));
            int warns = 0, maxAlert = 0;
            for (int k = 0; k < cycles && !g.Over; k++)
            {
                int before = g.Warnings.Count;
                g.Issue(new Order { Tick = g.Tick, Op = "cell", Do = "panic" }); r.Wait(1); warns += Math.Max(0, g.Warnings.Count - before);
                r.Wait(holdTicks - 1);
                foreach (var op in Cell4) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "holster" });
                r.Wait(gapTicks); maxAlert = Math.Max(maxAlert, g.Alert);
            }
            r.Wait(T(5));
            return $"cycles {cycles} hold {holdTicks}t gap {gapTicks}t: warnings opened {warns}, Alert {g.Alert} (max {maxAlert}), Trail {g.Trail}, Standing {g.StandingAtStart}->{g.Standing}, drawn.seen {g.Log.Count(l => l.Code == "drawn.seen")}, guard engaged {g.GuardEngaged}, guard memories {g.GuardP.Memory.Count} (saw-weapon-drawn {g.GuardP.Count("saw-weapon-drawn")}), over {g.Over} {g.FailCode}";
        }

        /// <summary>Target mode charge: the Scout volleys 6 on the guard again and again for `seconds`; the bar's low point and the volleys given.</summary>
        public static string VolleyCharge(Engine.Inputs i, int shots, int seconds, bool ceaseBetween)
        {
            var r = new Run(Engine.New(i, LabStart(s => s.Security = 1))); var g = r.G; r.To(T(2));
            var gp0 = g.PosOf(GuardR)!.Value; var near0 = g.S.Grid.Nearest(r.Op(Scout).Pos, q => G.D2(q, gp0) <= 1200L * 1200 && g.S.Sight(q, gp0, g.R.Eye), 9000); if (near0.HasValue) r.To(r.SendTo(Scout, near0.Value));
            int given = 0, refused = 0, low = 1000, hits = 0, fired = 0; int end = g.Tick + T(seconds);
            while (g.Tick < end && !g.Over && !g.GuardDown)
            {
                if (r.Op(Scout).Gun.VolleyLeft == 0)
                {
                    if (ceaseBetween && r.Op(Scout).Gun.Drawn) g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "cease" });
                    var a = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "volley", Target = GuardR, Shots = shots }); if (a.ok) given++; else refused++;
                }
                low = Math.Min(low, g.ChargeOf(OpId.Scout)); g.Step();
            }
            foreach (var s in r.Shots()) if (s.by == "scout") { fired++; if (s.hit) hits++; }
            return $"volley {shots} x until {seconds}s{(ceaseBetween ? " (cease between)" : "")}: given {given}, refused {refused}, bar low {low}, shots {fired}, hits {hits}, guard down {g.GuardDown} at {Report.Clock(g.Tick)}";
        }
    }
}
