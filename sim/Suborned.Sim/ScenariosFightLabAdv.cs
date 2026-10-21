// Brief 027 adversarial rows (Suture, briefs/027/suture/balance.md F7; Motive, briefs/027/motive/tuning.md §6.6, §6.8): the matrix's runs
// played by a smarter scripted player than PlayLabRun's: focus fire on one target, re-aim after every shot (cease + fire, the F1 reset),
// cycle targets, a stand-off band (never inside a hostile's effective ring + 1 m, fire from within max), the Enforcer stepping out of reach
// with Vale, and clear (the whole cell, carbine too, clears the response unit before the Enter). Ported from Suture's SEngage
// (briefs/027/suture/probe/SutureProbe.cs) with its decisions unchanged. Orders only, so every row replays from its log. Rows are named
// "<run>:<policy>" (L3:reset-kite, L3:focus-reset-kite, L3:cycle-kite, L3:carbine-kite, P-G:reset); PlayLabRun dispatches them here.
// Also Suture's two micro probes (panic spam, volley spam).
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    /// <summary>An adversarial player policy (Suture, briefs/027/suture/balance.md §1): the matrix player plus focus, reset, cycle, kite, enfkite, clear.</summary>
    public sealed class LabPolicy
    {
        /// <summary>Tuning.md §9.6 (engine 0.15.0, fire on the move): runfire (every move of the policy is a run, fire orders kept; with kite,
        /// the kite steps too), circle (a kite step runs across, round the threat at its effective ring + km, instead of straight away: K1),
        /// charge (out of point blank, run at the target to 4 m, firing: §8.5 "head first").</summary>
        public bool RunFire, Circle, Charge;
        public bool Blk, NoRivals, Aware; public int StepCm = 400; public bool Cycle, Focus, Reset, Kite, EnfKite, Clear, EnfGo, CarFree, Units, RawReset, RunKite, Hold; public int HoldS = 60, HoldCarCm, GoS, KMarginCm = 100, FMarginCm = 50; public string Name = "";
        /// <summary>Suture's retest flags (briefs/027/suture/probe/retest/harness-policies.diff, R5): enfgo (the Enforcer walks Vale straight to
        /// the car and never stops), carfree (board once the car is free, as matrix L3; not waiting for the fight's end), runkite (the kite steps
        /// are runs), units (the brief-013 units are targets too), rawreset (cease + fire after every shot). Second retest (retest2.md N3,
        /// probe/retest2/harness-at-hold.diff): at&lt;s&gt; (the Enforcer stands with Vale where he comes out until second s, then walks to the car
        /// without stopping), hold[30|90] / holdcar[20] (he steps out of reach while police are up, or near the car, for up to 60/30/90/100 s),
        /// and the band margins: the kite steps away inside a ring + km (default 100 cm) and fires inside max - fm (default 50 cm); tight = 30/20
        /// (km&lt;cm&gt;, fm&lt;cm&gt; set either). Third retest (retest3.md N8, probe/retest3/harness-step-norivals.diff): step&lt;cm&gt; (the kite
        /// step, default 400 cm), norivals (fire only on police and security; the ambush crew is still a threat to step from), blk (the blocker
        /// first), aware (the brief-013 units count as threats to step from, never targets). All off unless named, so the older rows don't change.</summary>
        public static LabPolicy Parse(string s)
        {
            int Num(string key, int dflt) { var m = System.Text.RegularExpressions.Regex.Match(s, @"(?:^|-)" + key + @"(\d+)"); return m.Success ? int.Parse(m.Groups[1].Value) : dflt; }
            bool tight = s.Contains("tight");
            return new LabPolicy { Name = s, Focus = s.Contains("focus"), Reset = s.Contains("reset") || s.Contains("cycle"), Cycle = s.Contains("cycle"), Kite = s.Contains("kite") && !s.Contains("enfkite") || s.Contains("allkite"), EnfKite = s.Contains("enfkite") || s.Contains("allkite"), Clear = s.Contains("clear") || s.Contains("carbine"), EnfGo = s.Contains("enfgo"), CarFree = s.Contains("carfree"), Units = s.Contains("units"), RawReset = s.Contains("rawreset"), RunKite = s.Contains("runkite") || s.Contains("runfire") || s.Contains("circle"), RunFire = s.Contains("runfire") || s.Contains("charge"), Circle = s.Contains("circle"), Charge = s.Contains("charge"),
                Hold = s.Contains("hold"), GoS = Num("at", 0), HoldCarCm = s.Contains("holdcar") ? (s.Contains("holdcar20") ? 2000 : 1700) : 0, HoldS = s.Contains("hold30") ? 30 : s.Contains("hold90") ? 90 : s.Contains("holdcar") ? 100 : 60,
                KMarginCm = Num("km", tight ? 30 : 100), FMarginCm = Num("fm", tight ? 20 : 50), StepCm = Num("step", 400), Blk = s.Contains("blk"), NoRivals = s.Contains("norivals"), Aware = s.Contains("aware") };
        }
    }

    public static partial class Scenarios
    {
        [ThreadStatic] static LabPolicy? pol;
        [ThreadStatic] static Dictionary<string, int>? lastNext; [ThreadStatic] static int holdFrom;
        /// <summary>The last adversarial run's re-aims (cease + fire, or a switch) and kite steps (this thread).</summary>
        [ThreadStatic] public static int Resets, Kites;

        /// <summary>One adversarial row: run `id` (L1c, L3, P-A, P-G, P-B) played by policy `p` (orders only, so it replays from its log).</summary>
        public static Run PlaySmart(Engine.Inputs i, string id, Start start, IReadOnlyDictionary<string, string> routes, LabPolicy p, bool coverPlayer = false)
        {
            pol = p; lastNext = new Dictionary<string, int>(); Resets = 0; Kites = 0; holdFrom = -1;
            var r = new Run(Engine.New(i, start)); var g = r.G; g.TrackContact = true; // verdict 6.4.8 (output only)
            r.CoverPlayer = coverPlayer && g.CoverSnapOn;
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

        /// <summary>Tuning.md §8.2 (engine 0.15.0): a fire order stops a walker; to walk and shoot, fire first, then give the walk again (its run too).</summary>
        static void FireKeepWalk(Run r, string op, string target)
        {
            if (op == Tech && r.G.LoopOn) return; // engine 0.15.0: the Tech's fire is no longer refused (a run latched by the loop's Engage), and its walk into range would end the loop: the player keeps him at the cabinet
            var e = r.Op(op); var end = e.Walk?.End; bool run = e.Running;
            r.G.Issue(new Order { Tick = r.G.Tick, Op = op, Do = "fire", Target = target });
            if (end.HasValue && e.Walk == null) r.G.Issue(new Order { Tick = r.G.Tick, Op = op, Do = "move", Point = end.Value, Run = run });
        }
        static bool SFree(Op e) => !(e.Down || e.Inside || e.InCar || e.Busy != null || e.Gun.W == null);

        /// <summary>As Run.Engage, with the policy: focus (one common target, the weakest any of the ops can reach), reset (cease + fire after
        /// each shot when the accumulator is below acc.start), kite (never stand inside a foe's effective ring; fire only from within max).</summary>
        public static void SEngage(Run r, Func<List<string>> targets, Func<bool> done, int limitS, string[] ops, bool carToEnforcer, bool advance = true)
        {
            var G0 = r.G; var p = pol ?? new LabPolicy(); int KM = p.KMarginCm, FM = p.FMarginCm; int end = G0.Tick + Game.T(limitS);
            while (!done() && !G0.Over && G0.Tick < end)
            {
                var ts0 = targets(); if (p.Aware) foreach (var u in G0.Units) if (!u.Legacy && !u.Patrol && !u.Gun.Down && u.St != "gone" && G0.PosOf(u.Id).HasValue && !ts0.Contains(u.Id)) ts0.Add(u.Id);
                var thrs = ts0.Where(id => G0.PosOf(id).HasValue && !G0.GunOf(id)!.Down && !(G0.FightOverTick != int.MaxValue / 4 && G0.Hostiles.Exists(h => h.Id == id))).ToList(); // tuning.md §9.1: after fight.over the player leaves them
                var ts = thrs.Where(id => !(p.NoRivals && id == "rivals") && !(p.Aware && !targets().Contains(id))).ToList();
                string blk = ""; if (p.Blk) foreach (var h in G0.Hostiles) if (h.Blocker && h.Present && !h.Down && ts.Contains(h.Id)) { blk = h.Id; break; }
                string focus = "";
                if (p.Focus && ts.Count > 0)
                {
                    int bh = int.MaxValue; long bd = long.MaxValue;
                    foreach (var id in ts)
                    {
                        var at = G0.PosOf(id)!.Value; int reach = 0; long sum = 0;
                        foreach (var name in ops) { var e = r.Op(name); if (!SFree(e)) continue; long d = G.D2(e.Pos, at); sum += d; if (d <= (long)e.Gun.W!.MaxCm * e.Gun.W.MaxCm && G0.S.Sight(e.Pos, at, G0.R.Eye)) reach++; }
                        if (reach == 0) continue;
                        int h = G0.GunOf(id)!.Health;
                        if (h < bh || (h == bh && sum < bd)) { bh = h; bd = sum; focus = id; }
                    }
                }
                foreach (var name in ops)
                {
                    var e = r.Op(name); var w = e.Gun.W;
                    if (!SFree(e)) continue; if (w == null) continue;
                    // reset: a shot just went (NextShot moved); the accumulator under acc.start: cease, and fire again on the same target
                    if (p.Reset && e.Gun.Order.Length > 0)
                    {
                        lastNext!.TryGetValue(name, out int ln);
                        if (e.Gun.NextShot != ln && e.Gun.NextShot == G0.Tick - 1 + w.Every) { lastNext[name] = e.Gun.NextShot; // a shot went last tick (Keel: Suture's test was "NextShot moved", which an aim also does; at lab-acc-start 0 a fresh aim reads under 500 and the script re-aimed every tick and never fired) if (p.Cycle && e.Gun.Acc < G0.R.AccStart && e.Gun.Target == e.Gun.Order) { string alt = ""; foreach (var id in ts) { if (id == e.Gun.Order) continue; var at = G0.PosOf(id)!.Value; if (G.D2(e.Pos, at) <= (long)(w.MaxCm - FM) * (w.MaxCm - FM) && G0.S.Sight(e.Pos, at, G0.R.Eye)) { alt = id; break; } } if (alt.Length > 0) { G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "fire", Target = alt }); Resets++; continue; } }
                        if ((p.RawReset || e.Gun.Acc < G0.R.AccStart) && e.Gun.Target == e.Gun.Order) { string t0 = e.Gun.Order; G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "cease" }); G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "fire", Target = t0 }); Resets++; } }
                    }
                    string near = ""; Pt np = default; long bd = long.MaxValue;
                    foreach (var id in ts) { var at = G0.PosOf(id)!.Value; long d = G.D2(e.Pos, at); if (d < bd) { bd = d; near = id; np = at; } }
                    if (near.Length == 0) { if (carToEnforcer && name == "enforcer" && e.Walk == null) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = G0.S.Stand(G0.S.GetawayCar) }); continue; }
                    if (name == "enforcer" && G0.Vale == ValeAt.WithEnforcer && p.GoS > 0) { if (G0.Tick < Game.T(p.GoS)) { if (e.Walk != null) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = e.Pos }); continue; } var stg = G0.S.Stand(G0.S.GetawayCar); if (e.Walk == null && !G.Within(e.Pos, G0.S.At(G0.S.GetawayCar), G0.R.AtReachCm)) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = stg }); continue; } // N3: at<s>, hold Vale, then go
                    if (name == "enforcer" && G0.Vale == ValeAt.WithEnforcer && p.Hold) { if (holdFrom < 0) holdFrom = G0.Tick; bool police = p.HoldCarCm > 0 ? G0.Hostiles.Any(h => h.Present && !h.Down && (h.Id.StartsWith("officer-") || h.Id.StartsWith("security-")) && G.Within(h.Pos(G0.Tick), G0.S.At(G0.S.GetawayCar), p.HoldCarCm)) : G0.Hostiles.Any(h => h.Present && !h.Down && !h.Blocker && (h.Id.StartsWith("officer-") || h.Id.StartsWith("security-"))); if (police && G0.Tick < holdFrom + Game.T(p.HoldS)) { string thr = ""; Pt tp = default; long td = long.MaxValue; foreach (var id in ts) { var at = G0.PosOf(id)!.Value; var fw = G0.GunOf(id)!.W; if (fw == null) continue; long d = G.D2(e.Pos, at); long rr = fw.EffCm + KM; if (d <= rr * rr && d < td) { td = d; thr = id; tp = at; } } if (thr.Length > 0) { long dx = e.Pos.X - tp.X, dy = e.Pos.Y - tp.Y; double n = Math.Sqrt((double)dx * dx + (double)dy * dy); if (n < 1) n = 1; var away = G0.S.Grid.NearestFree(new Pt(e.Pos.X + (int)(dx / n * 400), e.Pos.Y + (int)(dy / n * 400))); if (away.HasValue && (e.Walk == null || !G.Within(e.Walk.End, away.Value, 150))) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = away.Value }); } else if (e.Walk != null) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = e.Pos }); continue; } var st0 = G0.S.Stand(G0.S.GetawayCar); if (e.Walk == null && !G.Within(e.Pos, G0.S.At(G0.S.GetawayCar), G0.R.AtReachCm)) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = st0 }); continue; } // N3: hold, holdcar
                    if (name == "enforcer" && G0.Vale == ValeAt.WithEnforcer && p.EnfGo) { var st = G0.S.Stand(G0.S.GetawayCar); if (e.Walk == null && !G.Within(e.Pos, G0.S.At(G0.S.GetawayCar), G0.R.AtReachCm)) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = st }); continue; }
                    if (name == "enforcer" && G0.Vale == ValeAt.WithEnforcer && p.EnfKite)
                    {
                        string thr = ""; Pt tp = default; long td = long.MaxValue;
                        foreach (var id in ts) { var at = G0.PosOf(id)!.Value; var fw = G0.GunOf(id)!.W; if (fw == null) continue; long d = G.D2(e.Pos, at); long rr = fw.EffCm + KM; if (d <= rr * rr && d < td) { td = d; thr = id; tp = at; } }
                        if (thr.Length > 0) { long dx = e.Pos.X - tp.X, dy = e.Pos.Y - tp.Y; double n = Math.Sqrt((double)dx * dx + (double)dy * dy); if (n < 1) n = 1; var away = G0.S.Grid.NearestFree(new Pt(e.Pos.X + (int)(dx / n * 400), e.Pos.Y + (int)(dy / n * 400))); if (away.HasValue && (e.Walk == null || !G.Within(e.Walk.End, away.Value, 150))) { G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = away.Value }); Kites++; } }
                        else if (e.Walk != null) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = e.Pos });
                        continue;
                    }
                    if (name == "enforcer" && G0.Vale == ValeAt.WithEnforcer) { if (e.Walk != null) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = e.Pos }); continue; }
                    // kite: the nearest foe whose effective ring we stand in (plus 1 m): step 3 m straight away from it
                    if (p.Kite)
                    {
                        string thr = ""; Pt tp = default; long td = long.MaxValue;
                        foreach (var id in thrs) { var at = G0.PosOf(id)!.Value; var fw = G0.GunOf(id)!.W; if (fw == null) continue; long d = G.D2(e.Pos, at); long rr = fw.EffCm + KM; if (d <= rr * rr && d < td) { td = d; thr = id; tp = at; } }
                        if (thr.Length > 0)
                        {
                            long dx = e.Pos.X - tp.X, dy = e.Pos.Y - tp.Y; double n = Math.Sqrt((double)dx * dx + (double)dy * dy); if (n < 1) n = 1;
                            var away = G0.S.Grid.NearestFree(new Pt(e.Pos.X + (int)(dx / n * p.StepCm), e.Pos.Y + (int)(dy / n * p.StepCm)));
                            if (p.Circle) { var fw = G0.GunOf(thr)!.W!; double rr = fw.EffCm + KM, ang = Math.Atan2(dy, dx) + p.StepCm / rr; away = G0.S.Grid.NearestFree(new Pt(tp.X + (int)(Math.Cos(ang) * rr), tp.Y + (int)(Math.Sin(ang) * rr))); } // K1: run across, round the threat
                            if (away.HasValue && (e.Walk == null || !G.Within(e.Walk.End, away.Value, (p.StepCm >= 400 ? 150 : Math.Min(150, p.StepCm / 3))))) { G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = away.Value, Run = p.RunKite }); Kites++; }
                        }
                        else if (e.Walk != null && e.Gun.Order.Length > 0 && G0.PosOf(e.Gun.Order) is Pt op2 && G.Within(e.Pos, op2, w.MaxCm - 100) && G0.S.Sight(e.Pos, op2, G0.R.Eye)) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = e.Pos });
                    }
                    string tgt = near; Pt tpos = np; long tdist = bd;
                    if (blk.Length > 0) { var ba = G0.PosOf(blk)!.Value; tgt = blk; tpos = ba; tdist = G.D2(e.Pos, ba); focus = ""; }
                    if (focus.Length > 0) { var fa = G0.PosOf(focus)!.Value; long fd = G.D2(e.Pos, fa); if (fd <= (long)w.MaxCm * w.MaxCm && G0.S.Sight(e.Pos, fa, G0.R.Eye)) { tgt = focus; tpos = fa; tdist = fd; } }
                    bool line = G0.S.Sight(e.Pos, tpos, G0.R.Eye);
                    bool inMax = line && tdist <= (long)(w.MaxCm - FM) * (w.MaxCm - FM);
                    bool inEff = line && tdist <= (long)(w.EffCm - 200) * (w.EffCm - 200);
                    if (inMax && e.Gun.Order != tgt && (p.Focus || p.Kite || e.Gun.Order.Length == 0)) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "fire", Target = tgt });
                    else if (!p.Kite && e.Gun.Order.Length == 0 && line && tdist <= (long)w.MaxCm * w.MaxCm && (inEff || (advance && e.Walk == null) || G0.GunOf(tgt)!.Target == name)) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "fire", Target = tgt });
                    if (!advance) continue;
                    bool reach = p.Charge ? line && tdist <= (long)G0.R.PointBlankCm * G0.R.PointBlankCm : p.Kite ? inMax : inEff;
                    if (!reach)
                    {
                        if (p.Kite && e.Walk != null) continue; // a kite step in progress
                        var to = G0.S.Grid.NearestFree(tpos);
                        if (p.Charge) { long cx = tpos.X - e.Pos.X, cy = tpos.Y - e.Pos.Y; double cn = Math.Sqrt((double)cx * cx + (double)cy * cy); if (cn > 400) to = G0.S.Grid.NearestFree(new Pt(tpos.X - (int)(cx / cn * 400), tpos.Y - (int)(cy / cn * 400))); } // §8.5: head first, to 4 m
                        if (to.HasValue && (e.Walk == null || !G.Within(e.Walk.End, to.Value, 200))) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = to.Value, Run = p.RunFire });
                    }
                    else if (e.Walk != null && !p.Kite) G0.Issue(new Order { Tick = G0.Tick, Op = name, Do = "move", Point = e.Pos });
                }
                r.PerTick?.Invoke();
                G0.Step();
            }
        }

        static void AddUnits(Run r, List<string> l) { if (pol == null || !pol.Units) return; foreach (var u in r.G.Units) if (!u.Legacy && !u.Patrol && !u.Gun.Down && u.St != "gone" && r.G.PosOf(u.Id).HasValue && !l.Contains(u.Id)) l.Add(u.Id); }
        static void SHold(Run r, Func<bool> done, int limitS, bool advance, params string[] ops) => SEngage(r, () => { var l = Foes(r); AddUnits(r, l); return l; }, done, limitS, ops, false, advance);

        static void SFightItOut(Run r, int limitS, string[] with)
        {
            var G0 = r.G;
            Func<bool> free = pol != null && pol.CarFree ? () => !G0.CarHeld() : () => !G0.CarHeld() && G0.FightOverTick != int.MaxValue / 4;
            var ops = new List<string> { "enforcer" }; ops.AddRange(with);
            int end = G0.Tick + Game.T(limitS); var car = G0.S.At(G0.S.GetawayCar);
            while (!G0.Over && G0.Tick < end)
            {
                SEngage(r, () => { var l = new List<string>(); foreach (var h in G0.Hostiles) if (h.Present && !h.Down) l.Add(h.Id); l.AddRange(r.CityGunmen()); AddUnits(r, l); return l; }, free, Math.Max(1, (end - G0.Tick) / Game.Hz), ops.ToArray(), true);
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
            if (pol != null && pol.Focus) foreach (var op in new[] { Scout, Fixer, Tech }) FireKeepWalk(r, op, GuardR); // engine 0.15.0: a fire order plants a walker, so fire first, then the walk (tuning.md §8.2)
            SHold(r, () => g.GuardDown, 60, false, Scout, Fixer, Tech);
            if (pol != null && pol.Clear) // clear, carbine-kite (tuning.md §6.6.5): the whole cell, the carbine too, clears the response unit before the Enter
            {
                int quiet = -1;
                bool carbine = pol.Name.Contains("carbine"); // carbine-kite (tuning.md §6.6.5): the Enforcer alone, the others holstered, kiting from the carbine's band
                if (carbine) foreach (var op in new[] { Scout, Fixer, Tech }) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "holster" });
                SHold(r, () => { if (Foes(r).Count > 0) quiet = g.Tick; return (g.SecGuns.Length > 0 && g.SecGuns.All(x => x.Down)) || (quiet >= 0 && g.Tick - quiet >= T(10) && g.Tick > T(190)) || g.Tick >= T(200); }, 90, true, carbine ? new[] { Enf } : Cell4);
                if (carbine) DrawAll(r);
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
            return $"panic spam, cycles {cycles} hold {holdTicks}t gap {gapTicks}t: warnings opened {warns}, shouts {g.WarningShouts}, Alert {g.Alert} (max {maxAlert}), Trail {g.Trail}, Standing {g.StandingAtStart}->{g.Standing}, drawn.seen {g.Log.Count(l => l.Code == "drawn.seen")} (first: {string.Join("", g.Log.Where(l => l.Code == "drawn.seen").Take(1).Select(l => Report.Clock(l.Tick) + " " + l.Text))}), guard engaged {g.GuardEngaged}, guard memories {g.GuardP.Memory.Count} (saw-weapon-drawn {g.GuardP.Count("saw-weapon-drawn")}), over {g.Over} {g.FailCode}";
        }

        /// <summary>Target mode charge: the Scout volleys 6 on the guard again and again for `seconds`; the bar's low point and the volleys given.</summary>
        public static string VolleyCharge(Engine.Inputs i, int shots, int seconds, bool ceaseBetween)
        {
            var r = new Run(Engine.New(i, LabStart(s => { s.Security = 1; s.LabHealthScale = 100; }))); var g = r.G; r.To(T(2)); // Keel: health ×100, so the guard outlasts the spam and the bar is what is measured
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
            return $"volley spam, volley {shots} x until {seconds}s{(ceaseBetween ? " (cease between)" : "")}: given {given}, refused {refused}, bar low {low}, shots {fired}, hits {hits}, guard down {g.GuardDown} at {Report.Clock(g.Tick)}";
        }
    }
}
