// Brief 027 F2, the stand-off band (EXPERIMENTAL; rulebook fight.lab.kite; Pulse, briefs/027/pulse/kite-response.md; Motive, tuning.md §6.6).
// Sky 2026-10-20 (tuning.md §7.9): promoted to today (rulebook fight.kite); with the lab on, its own fight.lab.kite. Off, nothing here runs or adds a hash byte. The rule:
//  · the alarm: an operative's shot that lands (hit or miss) on an armed hostile marks every living body of the target's side with the
//    shooter and the tick (one incident, one dispatch: the side moves together). Sides: police (the response unit, before or after it joins,
//    and the final-fight officers) and rivals (the ambush crew); the pre-fight crew (`rivals`) is its own one-gun side. Not covered: the door
//    guard, the patrol, civilians and the brief-013 response units (police-n, robots: Pulse §6.4 open issue 1).
//  · cannot answer: kite.react after the first shot, a body acts only if no operative with a weapon out stands inside its effective ring on a
//    line it sees (a body already trading fire keeps its own fight).
//  · police close in: to the first point of the grid route to the shooter within ai.stopReach of its effective ring, re-planned every
//    kite.replan; at most kite.chase of walk a body for the run (then the cover that shields it from the shooter within ai.seek.police, held);
//    kite.lapse with no shot on its side clears the alarm (it does what it does today). Holders don't walk home while alarmed.
//  · rivals out of the line: the ambush crew takes the cover that shields it from the shooter (ai.seek.rival, once each cover.stepGap), else
//    the nearest free point within 8 m with no line to the shooter; the pre-fight crew walks out to it, holds, and walks back kite.hide after
//    the last shot on it, its arrival clock shifted by the detour (late by it, never early).
// Brief 027 retest (Suture retest.md R1–R4; Pulse retest-fixes.md; Motive tuning.md §7.2–7.4), still only with the lab kite on:
//  · the budget counts only the walk the answer itself orders, and only within kite.contact of the shooter; a walk farther out is free, and
//    the body's own walk (the walk in, its advance, a blocker's lane) never counts. A remainder shorter than a grid cell is spent (capped).
//    A capped body whose alarm is still open re-arms after kite.rearm (a fresh budget).
//  · the brief-013 police units (police-n; never robots) are a third side: a shot on one alarms every living police unit of its dispatch;
//    a unit standing at its scene (check, search, hold) answers as the response unit does, displaced off its post (unitDisp).
//  · in the final fight (Motive §7.2 rule 3, reworded by tuning.md §7.10 rule 5): a police body shot from out of its reach takes the cover
//    that shields it, fires back to its max ring, and after kite.coverHold with the shooter still out of its max, closes to the shielding
//    cover nearest its stop point of the shooter. It never chases a runner. The blocker keeps its lane and answers by rule 1 only (§7.11.1:
//    the lab's contest officer answers as any police body does).
//  · tuning.md §7.10 rule 1: a police body shot at within weapons.lineLapse by an operative it has a line to fires back while he is inside its
//    max ring (not only its effective ring), from where it stands, at its band's odds (FoesInReach). Never robots, the guard or the patrol.
// Second retest (Suture retest2.md N1, N4; his probe FB; Pulse reviews the motion): with no such cover the body closes to its stop point of
//    the shooter instead (its stand moves there; a police unit walks there off its post), once each cover.stepGap.
// Third retest (Suture retest3.md N7): the final-fight police body plans again kite.react after its last plan once where it stands no
//    longer shields it from where the shooter stands now (was cover.stepGap). N9 (let go the held cover on closing in) is not built: with
//    N7 it frees the point for the partner, who takes cover instead of closing, and reopens cheap wins (Pulse to rule).
//  · the pre-fight crew with no hide advances to its effective ring of the shooter (the ambush crew too); held out of the line it re-plans
//    while the shooter stands in its band (beyond eff, within max) and still fires, and does not lean back out while an operative with a
//    weapon out stands within its max of the lean-out point on a line; a hold past kite.hideMax advances. Its advances spend its budget.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>A body walked off its scripted path by the kite answer (the pre-fight response unit, the pre-fight rival crew): out, hold, back.</summary>
    public sealed class KiteDisp { public Path P = null!; public int Start, Phase, BackStart; public Pt Face; }

    /// <summary>What a hostile would do if the agent fired on it now (brief 027 F2, Pulse §3 the chip): its side ("police" or "rivals"), where it
    /// goes (police: the stop point inside its ring of the agent; rivals: the cover or blind point), and the ticks until it is there. Read-only.</summary>
    public sealed class KiteAnswer { public string Side = ""; public Pt Point; public int Ticks; public bool Cover; }

    public sealed partial class Game
    {
        /// <summary>The kite response is on (Sky 2026-10-20, tuning.md §7.9: today's fight.kite; with the lab on, its fight.lab.kite switch).</summary>
        bool LabKite;
        /// <summary>The kite response's numbers: the lab's with the lab on, else today's (fight.kite). Set at the start (InitKite).</summary>
        (int KiteReact, int KiteReplan, int KiteChaseCm, int KiteLapse, int KiteHide, int KiteContactCm, int KiteRearm, int KiteHideMax, int KiteCoverHold) Kr;
        void InitKite()
        {
            if (Lab) { var l = Labr!; Kr = (l.KiteReact, l.KiteReplan, l.KiteChaseCm, l.KiteLapse, l.KiteHide, l.KiteContactCm, l.KiteRearm, l.KiteHideMax, l.KiteCoverHold); return; }
            LabKite = R.KiteOn;
            Kr = (R.KiteReact, R.KiteReplan, R.KiteChaseCm, R.KiteLapse, R.KiteHide, R.KiteContactCm, R.KiteRearm, R.KiteHideMax, R.KiteCoverHold);
        }
        KiteDisp? rivalsDisp;
        readonly SortedDictionary<int, KiteDisp> secDisp = new SortedDictionary<int, KiteDisp>();
        readonly SortedDictionary<string, KiteDisp> unitDisp = new SortedDictionary<string, KiteDisp>(StringComparer.Ordinal); // retest R3: police units off their post
        const int KiteBlindCm = 800; // Pulse §2: the rivals' blind-point fallback, within 8 m

        static int KiteSide(string id) => id == "rivals" || IsRival(id) ? 2 : id.StartsWith("security-", StringComparison.Ordinal) || id.StartsWith("officer-", StringComparison.Ordinal) || id.StartsWith("response-", StringComparison.Ordinal) ? 1 : id.StartsWith("police-", StringComparison.Ordinal) ? 3 : 0;
        /// <summary>Retest R3: a brief-013 police unit (never a robot, the patrol or the legacy response unit) a kite side covers.</summary>
        Unit? KiteUnit(string id) { var u = UnitOf(id); return u != null && !u.Robot ? u : null; }
        static bool UnitStill(Unit u) => u.St == "check" || u.St == "search" || u.St == "hold";
        /// <summary>A police unit's pose off its post (retest R3), while it stands at its scene; null otherwise.</summary>
        (Pt pos, Pt facing)? UnitDispPose(Unit u) => LabKite && UnitStill(u) && unitDisp.TryGetValue(u.Id, out var d) ? DispPose(d, u.Speed, Tick) : ((Pt, Pt)?)null;
        bool? UnitDispMoving(Unit u) => LabKite && UnitStill(u) && unitDisp.TryGetValue(u.Id, out var d) ? DispMoving(d, u.Speed, Tick) : (bool?)null;
        /// <summary>Holders keep off the walk home while their alarm is open.</summary>
        bool KiteHolds(Gun g) => LabKite && g.KiteAt >= 0;

        /// <summary>An operative's shot landed on `t`: its side is alarmed (Resolve).</summary>
        void KiteAlarm(string by, string t)
        {
            int side = KiteSide(t); if (side == 0) return;
            if (t == "rivals") { KiteMark(RivalsGun, by); return; }
            if (side == 3) { var u0 = KiteUnit(t); if (u0 == null) return; foreach (var u in Units) if (!u.Legacy && !u.Patrol && !u.Robot && u.Act == u0.Act) KiteMark(u.Gun, by); return; } // one incident, one dispatch
            if (side == 1) { foreach (var g in SecGuns) KiteMark(g, by); foreach (var h in Hostiles) if (KiteSide(h.Id) == 1 && SecIndex(h.Id) < 0) KiteMark(h.Gun, by); }
            else foreach (var h in Hostiles) if (KiteSide(h.Id) == 2) KiteMark(h.Gun, by);
        }
        void KiteMark(Gun g, string by) { if (g.Down) return; g.KiteBy = by; g.KiteSeen = Tick; if (g.KiteAt < 0) g.KiteAt = Tick; }
        void KiteClear(Gun g) { g.KiteAt = -1; g.KiteLast = -1; g.KiteBy = ""; g.KiteHasPrev = false; g.KiteCoverAt = -1; }

        /// <summary>Tuning.md §7.10 rule 1: a police body (final-fight officers, the response unit, the brief-013 police units; never robots, the
        /// guard or the patrol) shot at within weapons.lineLapse, hit or miss, by this operative: it may answer him out to its max ring.</summary>
        bool PoliceBody(string id) => (KiteSide(id) == 1 || KiteSide(id) == 3) && !(UnitOf(id) is Unit pu && pu.Robot);
        /// <summary>Tuning.md §12.2 (engine 0.17.0, both presets): any operative who fired on this body within weapons.lineLapse, not only the last shooter.</summary>
        bool ShotAtRecently(Gun g, string op) => LabKite && ShotByOpRecently(g, op);
        bool ShotByOpRecently(Gun g, string op) { int st = g.OpShot[(int)Sim.Op.Parse(op)]; return st >= 0 && Tick - st <= T(R.LineLapseS); }

        /// <summary>Any operative with a weapon out inside `effCm` of `pos` that the body sees by its own sight (a pre-fight officer's watch, the
        /// crew's, a fight body's line): the body can answer, and keeps its own fight.</summary>
        bool KiteCanAnswer(Pt pos, int effCm, Func<Pt, bool> sees)
        {
            foreach (var o in Ops) { if (o.Down || o.Inside || !(o.Gun.Drawn || o.Gun.Fired)) continue; if (G.D2(pos, o.Pos) <= (long)effCm * effCm && sees(o.Pos)) return true; }
            return false;
        }
        Func<Pt, bool> KiteSees(string id, Pt pos)
        {
            if (id == "rivals") return RivalsWatching;
            int k = SecIndex(id); if (k >= 0 && !secJoined[k]) return p => SecOfficerWatching(k, p, false);
            return p => Sees(pos, p);
        }

        /// <summary>The tick's answers, before security's bodies act (Combat): the pre-fight crew, the response unit not yet joined, the hostiles.</summary>
        void KiteTick()
        {
            KiteRivals();
            for (int k = 0; k < SecGuns.Length; k++)
            {
                var g = SecGuns[k]; if (g.Down || secJoined[k]) continue;
                var pose = SecOfficerPose(k); if (!pose.HasValue) continue;
                KiteBody(g, pose.Value.pos, null, k);
            }
            foreach (var h in Hostiles)
            {
                if (h.Down || !h.Present || h.Gun.St == "break" || h.Gun.St == "gone") continue;
                KiteBody(h.Gun, h.Pos(Tick), h, -1);
            }
            if (unitDisp.Count > 0) { var stale = new List<string>(); foreach (var kv in unitDisp) { var su = KiteUnit(kv.Key); if (su == null || su.Gun.Down || !UnitStill(su)) stale.Add(kv.Key); } foreach (var x in stale) unitDisp.Remove(x); } // it walks on (or back): its own route starts where it stood
            foreach (var u in Units)
            {
                if (u.Legacy || u.Patrol || u.Robot || u.Gun.Down || !u.OnStreet(Tick)) continue;
                var up = UnitPose(u); if (!up.HasValue) continue;
                KiteBody(u.Gun, up.Value.pos, null, -1, u);
            }
        }

        void KiteBody(Gun g, Pt pos, Hostile? h, int k, Unit? u = null)
        {
            if (g.KiteAt < 0 || g.W == null) return;
            var shooter = OpOf(Sim.Op.Parse(g.KiteBy));
            if (Tick - g.KiteSeen > Kr.KiteLapse || shooter.Down || shooter.Inside) { KiteClear(g); return; }
            var sp = shooter.Pos; int eff = g.W.EffCm, side = KiteSide(g.Id), contact = Kr.KiteContactCm;
            // retest R2: only the walk the answer orders counts, and only within kite.contact of the shooter (Pulse §1a; Motive §7.2 rule 3)
            bool onKite = k >= 0 ? secDisp.TryGetValue(k, out var kd0) && DispMoving(kd0, secSpeed, Tick) : u != null && unitDisp.TryGetValue(u.Id, out var ud0) && DispMoving(ud0, u.Speed, Tick);
            if (g.KiteHasPrev && onKite && G.D2(pos, sp) <= (long)contact * contact) g.KiteWalked += G.Dist(g.KitePrev, pos);
            g.KitePrev = pos; g.KiteHasPrev = true;
            if (Tick < g.KiteAt + Kr.KiteReact) return;
            bool moving = h != null ? h.Moving(Tick) : u != null ? Moving(u.Id) : Moving(g.Id);
            var sees = u != null ? (p => UnitSees(u, p, false)) : KiteSees(g.Id, pos);
            bool maxAnswer = side != 2 && ShotAtRecently(g, g.KiteBy) && G.D2(pos, sp) <= (long)g.W.MaxCm * g.W.MaxCm && sees(sp); // §7.10 rule 1
            if (side != 2 && !maxAnswer) foreach (var o in Ops) { if (o.Down || o.Inside) continue; if (ShotAtRecently(g, Sim.Op.Name(o.Id)) && G.D2(pos, o.Pos) <= (long)g.W.MaxCm * g.W.MaxCm && sees(o.Pos)) { maxAnswer = true; break; } } // §12.2: any of them in its max ring; rule 3's cover only when it can answer none
            if (maxAnswer) g.KiteCoverAt = -1;
            if (KiteCanAnswer(pos, eff, sees) || maxAnswer)
            {
                if (moving && side == 1 && h != null && !h.Blocker) Stop(h);
                if (moving && side == 1 && k >= 0) SecStop(k, pos, sp);
                if (moving && u != null && unitDisp.ContainsKey(u.Id)) unitDisp[u.Id] = new KiteDisp { P = new Path(new[] { pos, pos }), Start = Tick, Face = Facing(pos, sp) };
                return;
            }
            if (side != 2 && (h != null || (u != null && FightBranch.Length > 0))) { KiteFightCover(g, pos, sp, h, u); return; } // Motive §7.2 rule 3: in the final fight, cover, never a chase
            if (side != 2)
            {
                if (u != null && !UnitStill(u)) return; // a unit on its way keeps its walk
                if (g.KiteCapped && Tick - g.KiteCapTick >= Kr.KiteRearm) { g.KiteCapped = false; g.KiteWalked = 0; Note("ai.kite", $"{g.Id} closes again: {g.KiteBy} is still firing."); } // Pulse §1c
                if (g.KiteCapped) return;
                if (moving && g.KiteLast >= 0 && Tick - g.KiteLast < Kr.KiteReplan) return;
                int rem = Kr.KiteChaseCm - g.KiteWalked;
                int free = (int)Math.Max(0, G.Dist(pos, sp) - contact);
                if (rem <= 0 || (rem < S.Grid.Cell && free == 0)) // Pulse §1b: a remainder shorter than a grid cell is spent (KitePath would find no cut)
                {
                    g.KiteCapped = true; g.KiteCapTick = Tick;
                    if (k >= 0 && moving) SecStop(k, pos, sp);
                    if (u != null && moving) unitDisp[u.Id] = new KiteDisp { P = new Path(new[] { pos, pos }), Start = Tick, Face = Facing(pos, sp) };
                    Note("ai.kite", $"{g.Id} has walked its {Kr.KiteChaseCm / 100} m: it holds against {g.KiteBy}.");
                    return;
                }
                var path = KitePath(pos, StopPoint(pos, sp, eff), rem + free);
                if (path == null) return; // no route: it holds, uncapped
                if (g.KiteLast < 0) Note("ai.kite", $"{g.Id} answers {g.KiteBy}: first shot at {Sim.Report.Clock(g.KiteAt)}, moves at {Sim.Report.Clock(Tick)} ({Tick - g.KiteAt} ticks), {G.Dist(pos, sp) / 100} m out.");
                g.KiteLast = Tick;
                var disp = new KiteDisp { P = path, Start = Tick, Face = Facing(path.End, sp) };
                if (u != null) unitDisp[u.Id] = disp; else secDisp[k] = disp;
                return;
            }
            // rivals: out of the shooter's line
            if (h == null || Tick - g.StepAt < T(R.CoverStepGapS) || Protected(pos, sp)) return;
            g.StepAt = Tick;
            var dest = KiteHide(g, pos, sp, Car, out var c);
            if (!dest.HasValue) { var sp2 = StopPoint(pos, sp, eff); if (sp2 != pos) dest = sp2; } // retest R4 (Pulse §5): no hide, it advances to its ring of the shooter
            if (!dest.HasValue) return;
            if (g.KiteLast < 0) Note("ai.kite", $"{g.Id} answers {g.KiteBy}: first shot at {Sim.Report.Clock(g.KiteAt)}, moves at {Sim.Report.Clock(Tick)} ({Tick - g.KiteAt} ticks), {(c != null ? $"out of the line to {c.Id}" : "out of the line")}.");
            g.KiteLast = Tick;
            if (c != null) TakeCover(g, c);
            MoveTo(h, dest.Value);
        }

        /// <summary>Motive tuning.md §7.2 rule 3: a final-fight police body (an officer, the joined response unit, a police unit) shot by an operative
        /// it can't answer takes the cover that shields it from the shooter, within ai.seek.police and no farther from the car than its stand (a
        /// unit: than it stands), planning again kite.react after its last plan once it is unshielded (retest3 N7); its stand becomes that point. It never chases. The blocker keeps its lane.</summary>
        /// <summary>Rule 3's cover: free, at least partial, present, within ai.seek.police of `pos`, shielding from `sp`, no farther from the car
        /// than `stand`; the nearest less its quality, then the lower id. Read-only.</summary>
        CoverPoint? FightCover(Pt pos, Pt sp, Pt stand)
        {
            int sec = Tick / Hz; long lim = G.Dist(stand, Car); var taken = Taken();
            CoverPoint? best = null; long bk = long.MaxValue;
            foreach (var c in LiveCover)
            {
                if (taken.Contains(c.Id) || c.Q < R.CoverQPartial || !c.Present(sec)) continue;
                long d = G.Dist(pos, c.At); if (d > R.SeekPoliceCm || G.Dist(c.At, Car) > lim || !CoverRules.Protects(c, sp, sec, R.PointBlankCm)) continue;
                long key = d - c.Q;
                if (key < bk || (key == bk && best != null && string.CompareOrdinal(c.Id, best.Id) < 0)) { best = c; bk = key; }
            }
            return best;
        }

        void KiteFightCover(Gun g, Pt pos, Pt sp, Hostile? h, Unit? u)
        {
            if (h != null && h.Blocker) return; // §7.10 rule 4 (§7.11.1): the blocker keeps its lane and answers by rule 1 only
            if (u != null && !UnitStill(u)) return;
            if (Protected(pos, sp))
            {
                // §7.10 rule 2: shielded, the shooter beyond its max ring: hold at most kite.coverHold from the first shot it can't answer here,
                // then close to the shielding cover nearest its stop point of the shooter; again each coverHold while shots land
                if (g.KiteCoverAt < 0) { g.KiteCoverAt = Tick; return; }
                if (Tick - g.KiteCoverAt < Kr.KiteCoverHold) return;
                if (g.KiteLast >= 0 && g.KiteSeen <= g.KiteLast) return; // no shot since its last plan: it holds
                bool walking = h != null ? h.Moving(Tick) : unitDisp.TryGetValue(u!.Id, out var wd) && DispMoving(wd, u.Speed, Tick);
                if (walking) return; // it fires nothing and plans nothing while it walks
                KiteCloseToCover(g, pos, sp, h, u);
                g.KiteCoverAt = Tick;
                return;
            }
            // third retest (Suture retest3.md N7, his PROBE_REPLAN): a body whose position no longer shields it from where the shooter stands
            // now re-plans kite.react after its last step, not cover.stepGap
            if (Tick - g.StepAt < Kr.KiteReact) return;
            g.StepAt = Tick;
            var best = FightCover(pos, sp, h != null ? h.Home : pos); if (best == null) { KiteFightClose(g, pos, sp, h, u); return; }
            if (g.KiteLast < 0) Note("ai.kite", $"{g.Id} is fired on by {g.KiteBy} out of its reach: first shot at {Sim.Report.Clock(g.KiteAt)}, it takes cover at {best.Id} ({Tick - g.KiteAt} ticks).");
            g.KiteLast = Tick;
            if (h != null) { TakeCover(g, best); h.Home = best.At; MoveTo(h, best.At); g.St = "advance"; return; }
            var path = KitePath(pos, best.At, int.MaxValue / 20); if (path == null) return;
            unitDisp[u!.Id] = new KiteDisp { P = path, Start = Tick, Face = Facing(path.End, sp) };
        }

        /// <summary>Tuning.md §7.10 rule 2's close: the best shielding cover (free, at least partial, present, shielding from the shooter) within
        /// ai.seek.police of the body's stop point of the shooter (90 % of its effective ring along its route), nearest the stop point less its
        /// quality, then the lower id; none: the stop point in the open. A fight body's stand becomes it; a police unit walks there off its post.</summary>
        void KiteCloseToCover(Gun g, Pt pos, Pt sp, Hostile? h, Unit? u)
        {
            if (g.W == null) return;
            var stop = StopPoint(pos, sp, g.W.EffCm);
            int sec = Tick / Hz; var taken = Taken(); CoverPoint? best = null; long bk = long.MaxValue;
            foreach (var c in LiveCover)
            {
                if (taken.Contains(c.Id) || c.Q < R.CoverQPartial || !c.Present(sec)) continue;
                long d = G.Dist(stop, c.At); if (d > R.SeekPoliceCm || !CoverRules.Protects(c, sp, sec, R.PointBlankCm)) continue;
                long key = d - c.Q;
                if (key < bk || (key == bk && best != null && string.CompareOrdinal(c.Id, best.Id) < 0)) { best = c; bk = key; }
            }
            var to = best?.At ?? stop; if (to == pos) return;
            Note("ai.kite", $"{g.Id} is still fired on by {g.KiteBy} beyond its reach: it closes {(best != null ? $"to cover at {best.Id}" : "to its ring")} ({G.Dist(pos, sp) / 100} m out).");
            g.KiteLast = Tick; g.StepAt = Tick;
            if (h != null) { if (best != null) { ReleaseCover(g); TakeCover(g, best); } h.Home = to; MoveTo(h, to); g.St = "advance"; return; }
            var path = KitePath(pos, to, int.MaxValue / 20); if (path == null) return;
            unitDisp[u!.Id] = new KiteDisp { P = path, Start = Tick, Face = Facing(path.End, sp) };
        }

        /// <summary>Second retest (Suture retest2.md N1, N4; his probe FB, probe/retest2/probe-fb-nocover-fallback.diff; Pulse to review the
        /// motion): rule 3 with no qualifying cover. The body closes to its stop point of the shooter (the first point of its route within
        /// ai.stopReach of its effective ring), on KiteFightCover's cadence; a fight body's stand becomes that point, a police unit walks there off
        /// its post. Its cover, if it held one, is kept (retest3 N9 not built: releasing it sends the partner to cover instead of closing). The blocker keeps its lane (the caller returns before).</summary>
        void KiteFightClose(Gun g, Pt pos, Pt sp, Hostile? h, Unit? u)
        {
            if (g.W == null) return;
            var to = StopPoint(pos, sp, g.W.EffCm); if (to == pos) return;
            if (h != null)
            {
                if (g.KiteLast < 0) Note("ai.kite", $"{g.Id} is fired on by {g.KiteBy} out of its reach and has no cover: first shot at {Sim.Report.Clock(g.KiteAt)}, it closes to its ring ({Tick - g.KiteAt} ticks).");
                g.KiteLast = Tick; h.Home = to; MoveTo(h, to); g.St = "advance"; return;
            }
            var path = KitePath(pos, to, int.MaxValue / 20); if (path == null) return;
            if (g.KiteLast < 0) Note("ai.kite", $"{g.Id} is fired on by {g.KiteBy} out of its reach and has no cover: first shot at {Sim.Report.Clock(g.KiteAt)}, it closes to its ring ({Tick - g.KiteAt} ticks).");
            g.KiteLast = Tick;
            unitDisp[u!.Id] = new KiteDisp { P = path, Start = Tick, Face = Facing(path.End, sp) };
        }

        /// <summary>A rival's hide from `sp`: the cover the data give that shields it (ai.seek.rival), else the nearest free point within 8 m with
        /// no line to the shooter. Read-only (the caller takes the cover).</summary>
        Pt? KiteHide(Gun g, Pt pos, Pt sp, Pt objective, out CoverPoint? c)
        {
            c = SelectCover(pos, objective, sp, R.SeekRivalCm);
            if (c != null) return c.At;
            return S.Grid.Nearest(pos, q => !Sees(q, sp), KiteBlindCm);
        }

        /// <summary>The walk from `pos` to `dest` on the grid, cut at `remCm` of walk (the chase budget left); null if there is none.</summary>
        Path? KitePath(Pt pos, Pt dest, int remCm)
        {
            var a = S.Grid.NearestFree(pos) ?? pos; var b = S.Grid.NearestFree(dest) ?? dest;
            var route = S.Grid.Route(a, b); if (route == null || route.Length < 2) return null;
            var path = new Path(route); long remMm = (long)remCm * 10;
            if (path.Length > remMm)
            {
                var cut = path.At(remMm); route = S.Grid.Route(a, S.Grid.NearestFree(cut) ?? cut);
                if (route == null || route.Length < 2) return null;
                path = new Path(route);
            }
            return path.Length == 0 ? null : path;
        }
        static Pt Facing(Pt from, Pt to) { var f = new Pt(to.X - from.X, to.Y - from.Y); return f == new Pt(0, 0) ? new Pt(100, 0) : f; }

        void SecStop(int k, Pt pos, Pt sp) => secDisp[k] = new KiteDisp { P = new Path(new[] { pos, pos }), Start = Tick, Face = Facing(pos, sp) };

        /// <summary>The pre-fight rival crew: out to the hide, hold, back after kite.hide with no shot on it; the arrival clock shifts by the detour.</summary>
        void KiteRivals()
        {
            var g = RivalsGun; if (!RivalsDrawn || !RivalsPresent || g.Down) { rivalsDisp = null; return; }
            if (rivalsDisp != null)
            {
                var d = rivalsDisp; long len = d.P.Length;
                if (d.Phase == 0 && (long)(Tick - d.Start) * rivSpeed >= len) { d.Phase = 1; g.KiteHoldAt = Tick; }
                if (d.Phase == 1)
                {
                    var sh = g.KiteBy.Length > 0 ? OpOf(Sim.Op.Parse(g.KiteBy)) : null;
                    bool quiet = Tick - g.KiteSeen >= Kr.KiteHide, w = g.W != null;
                    // retest R4 (Pulse §5.1): the shooter stands in the crew's band and still fires: it re-plans toward its ring, once each kite.replan
                    if (!quiet && w && sh != null && !sh.Down && !sh.Inside && Tick - g.KiteLast >= Kr.KiteReplan)
                    {
                        long dd = G.D2(d.P.End, sh.Pos);
                        if (dd > (long)g.W!.EffCm * g.W.EffCm && dd <= (long)g.W.MaxCm * g.W.MaxCm && RivalsWatching(sh.Pos)) { CrewAdvance(d, sh.Pos); return; }
                    }
                    if (quiet)
                    {
                        if (!w || !ShooterOnLine(d.P.At(0), g.W!.MaxCm)) { d.Phase = 2; d.BackStart = Tick; } // Pulse §5.2: no lean-out into a shooter still there
                        else if (Tick - g.KiteHoldAt >= Kr.KiteHideMax && sh != null && !sh.Down && !sh.Inside) { CrewAdvance(d, sh.Pos); return; } // a long hold advances
                    }
                }
                if (d.Phase == 2 && (long)(Tick - d.BackStart) * rivSpeed >= len)
                {
                    if (RivalsDelayedUntil <= RivalsDrawnTick) RivalsDrawnTick += Tick - d.Start;
                    Note("ai.kite", $"rivals lean out again after {(Tick - d.Start) / Hz} s out of the line.");
                    rivalsDisp = null; KiteClear(g);
                }
                return;
            }
            if (g.KiteAt < 0 || Tick < g.KiteAt + Kr.KiteReact) return;
            var shooter = OpOf(Sim.Op.Parse(g.KiteBy)); if (shooter.Down || shooter.Inside) { KiteClear(g); return; }
            var pose = RivalsPose(); if (!pose.HasValue || g.W == null) return;
            var pos = pose.Value.pos; var sp = shooter.Pos;
            if (KiteCanAnswer(pos, g.W.EffCm, RivalsWatching) || RivalsDelayedUntil > RivalsDrawnTick) return;
            var dest = KiteHide(g, pos, sp, pos, out var c);
            Path? path;
            if (dest.HasValue) path = KitePath(pos, dest.Value, int.MaxValue / 20);
            else // retest R4 (Pulse §5): no hide, it advances to its ring of the shooter, on its chase budget
            {
                int rem = Kr.KiteChaseCm - g.KiteWalked, free = (int)Math.Max(0, G.Dist(pos, sp) - Kr.KiteContactCm);
                if (rem < S.Grid.Cell && free == 0) return;
                path = KitePath(pos, StopPoint(pos, sp, g.W.EffCm), rem + free);
                if (path != null) g.KiteWalked += (int)Math.Max(0, path.Length / 10 - free);
            }
            if (path == null) return;
            if (c != null) TakeCover(g, c);
            g.KiteLast = Tick;
            Note("ai.kite", $"rivals answer {g.KiteBy}: first shot at {Sim.Report.Clock(g.KiteAt)}, move at {Sim.Report.Clock(Tick)} ({Tick - g.KiteAt} ticks), {(dest.HasValue ? "out of the line" + (c != null ? $" to {c.Id}" : "") : "to their ring of the shooter")}.");
            rivalsDisp = new KiteDisp { P = path, Start = Tick, Face = Facing(path.End, sp) };
        }

        /// <summary>Retest R4 (Pulse §5.1–5.2): the pre-fight crew, held out of the line, walks on toward its ring of `sp`: the old walk up to where
        /// it stands, then the route on (cut at its chase budget), so it comes back the way it went; the detour clock is the whole walk.</summary>
        void CrewAdvance(KiteDisp d, Pt sp)
        {
            var g = RivalsGun; var pos = d.P.End;
            int rem = Kr.KiteChaseCm - g.KiteWalked, free = (int)Math.Max(0, G.Dist(pos, sp) - Kr.KiteContactCm);
            g.KiteLast = Tick;
            if (rem < S.Grid.Cell && free == 0) return; // its budget is spent: it holds
            var add = KitePath(pos, StopPoint(pos, sp, g.W!.EffCm), rem + free); if (add == null) return;
            var pts = new List<Pt>(d.P.Points); foreach (var q in add.Points) if (pts[pts.Count - 1] != q) pts.Add(q);
            if (pts.Count == d.P.Points.Length) return;
            var np = new Path(pts.ToArray()); long done = d.P.Length;
            g.KiteWalked += (int)Math.Max(0, add.Length / 10 - free);
            d.P = np; d.Phase = 0; d.Start = Tick - (int)((done + rivSpeed - 1) / rivSpeed); d.Face = Facing(np.End, sp); g.KiteHoldAt = -1;
            Note("ai.kite", $"rivals walk on toward {g.KiteBy}, {G.Dist(pos, sp) / 100} m out.");
        }

        /// <summary>An operative with a weapon out within `reachCm` of `at` on a clear line (the crew's lean-out point is still covered).</summary>
        bool ShooterOnLine(Pt at, int reachCm)
        {
            foreach (var o in Ops) { if (o.Down || o.Inside || !(o.Gun.Drawn || o.Gun.Fired)) continue; if (G.D2(at, o.Pos) <= (long)reachCm * reachCm && Sees(at, o.Pos)) return true; }
            return false;
        }

        /// <summary>A displaced body's pose: out along its walk (then facing the shooter), held, or back along it.</summary>
        static (Pt pos, Pt facing) DispPose(KiteDisp d, int speed, int tick)
        {
            long len = d.P.Length;
            if (d.Phase == 2) { long mm = len - (long)Math.Max(0, tick - d.BackStart) * speed; if (mm <= 0) return (d.P.At(0), Neg(d.P.Heading(0))); return (d.P.At(mm), Neg(d.P.Heading(mm))); }
            if (d.Phase == 1) return (d.P.End, d.Face);
            long m0 = (long)Math.Max(0, tick - d.Start) * speed;
            return m0 >= len ? (d.P.End, d.Face) : (d.P.At(m0), d.P.Heading(m0));
        }
        static Pt Neg(Pt p) => new Pt(-p.X, -p.Y);
        static bool DispMoving(KiteDisp d, int speed, int tick) =>
            d.Phase == 0 ? (long)(tick - d.Start) * speed < d.P.Length : d.Phase == 2 && (long)(tick - d.BackStart) * speed < d.P.Length;

        /// <summary>Brief 027 F2 (Pulse §3, the chip): what `target` would do if `op` fired on it now (from where it stands, or from `from`, the
        /// stop of a walk into range); null when it would answer from where it stands, it is not an armed hostile of a side, or the kite is off.
        /// Read-only.</summary>
        public KiteAnswer? PreviewAnswer(OpId op, string target, Pt? from = null)
        {
            if (!LabKite) return null;
            int side = KiteSide(target); var g = GunOf(target); var at = PosOf(target);
            if (side == 0 || g == null || g.Down || g.W == null || !at.HasValue) return null;
            var o = OpOf(op); var pos = at.Value; var sp = from ?? o.Pos; int eff = g.W.EffCm;
            var un = side == 3 ? KiteUnit(target) : null; if (side == 3 && (un == null || !UnitStill(un))) return null;
            var sees = un != null ? (p => UnitSees(un, p, false)) : KiteSees(target, pos);
            if (G.D2(pos, sp) <= (long)eff * eff && sees(sp)) return null;
            if (KiteCanAnswer(pos, eff, sees)) return null;
            var h = HostileOf(target); int react = Kr.KiteReact;
            if (side != 2)
            {
                int speed = h?.Speed ?? un?.Speed ?? secSpeed;
                if (h != null || (un != null && FightBranch.Length > 0)) // retest, Motive §7.2 rule 3: the final fight's answer is cover, never a chase
                {
                    if (h != null && h.Blocker) return null;
                    var cv = FightCover(pos, sp, h != null ? h.Home : pos);
                    if (cv == null) // retest2 N1/N4: no cover, it closes to its ring of the shooter (KiteFightClose)
                    {
                        var to = StopPoint(pos, sp, eff); if (to == pos) return null;
                        var tw = KitePath(pos, to, int.MaxValue / 20); if (tw == null) return null;
                        return new KiteAnswer { Side = "police", Point = h != null ? to : tw.End, Ticks = react + (int)((tw.Length + speed - 1) / speed) };
                    }
                    var cw = KitePath(pos, cv.At, int.MaxValue / 20);
                    return new KiteAnswer { Side = "police", Point = cv.At, Cover = true, Ticks = react + (cw == null ? 0 : (int)((cw.Length + speed - 1) / speed)) };
                }
                if (g.KiteCapped) return null;
                int free = (int)Math.Max(0, G.Dist(pos, sp) - Kr.KiteContactCm);
                var path = KitePath(pos, StopPoint(pos, sp, eff), Kr.KiteChaseCm - g.KiteWalked + free); if (path == null) return null;
                return new KiteAnswer { Side = "police", Point = path.End, Ticks = react + (int)((path.Length + speed - 1) / speed) };
            }
            var dest = KiteHide(g, pos, sp, h != null ? Car : pos, out var c);
            if (!dest.HasValue) { var sp2 = StopPoint(pos, sp, eff); if (sp2 == pos) return null; dest = sp2; } // retest R4: no hide, it advances
            var walk = KitePath(pos, dest.Value, int.MaxValue / 20); int rs = h?.Speed ?? rivSpeed;
            return new KiteAnswer { Side = "rivals", Point = dest.Value, Cover = c != null, Ticks = react + (walk == null ? 0 : (int)((walk.Length + rs - 1) / rs)) };
        }

        /// <summary>Brief 027 verdict 6.4.8 (Motive tuning.md §7.1; the fight-lab matrix only, output: nothing in the run reads it and it is not
        /// hashed): from Vale coming out to the end, every final-fight police body seen up (officers, the joined response unit, police units) and
        /// those that once had the Vale holder (the car once he is in it), or a cell agent within 15 m of him, inside their effective ring on a
        /// clear line. Off unless a harness sets it.</summary>
        public bool TrackContact;
        public readonly SortedSet<string> ContactPolice = new SortedSet<string>(StringComparer.Ordinal), ContactHad = new SortedSet<string>(StringComparer.Ordinal);
        const int ContactNearCm = 1500;
        void ContactStep()
        {
            if (!TrackContact || FightBranch.Length == 0 || (Vale != ValeAt.WithEnforcer && Vale != ValeAt.Car)) return;
            var e = Enforcer; Pt holder = Vale == ValeAt.Car || e.Inside ? Car : e.Pos;
            var near = new List<Pt> { holder };
            foreach (var o in Ops) if (!o.Down && !o.Inside && G.D2(o.Pos, holder) <= (long)ContactNearCm * ContactNearCm) near.Add(o.Pos);
            void Body(string id, Gun g, Pt pos)
            {
                ContactPolice.Add(id); if (ContactHad.Contains(id) || g.W == null) return;
                long eff = (long)g.W.EffCm * g.W.EffCm;
                foreach (var q in near) if (G.D2(pos, q) <= eff && Sees(pos, q)) { ContactHad.Add(id); return; }
            }
            foreach (var h in Hostiles) if (!IsRival(h.Id) && h.Present && !h.Down) Body(h.Id, h.Gun, h.Pos(Tick));
            foreach (var u in Units) { if (u.Legacy || u.Patrol || u.Robot || u.Gun.Down || !u.OnStreet(Tick)) continue; var up = UnitPose(u); if (up.HasValue) Body(u.Id, u.Gun, up.Value.pos); }
        }
        /// <summary>6.4.8 (a)'s police up at the end that never had contact (ids).</summary>
        public List<string> ContactMissing() { var l = new List<string>(); foreach (var id in ContactPolice) { if (ContactHad.Contains(id)) continue; var g = GunOf(id); if (g != null && !g.Down) l.Add(id); } return l; }

        /// <summary>Today's kite state for the checkpoint hash (the lab hashes its own in HashLab).</summary>
        internal void HashKiteToday(Hash.H h) { if (LabKite) HashKite(h); }
        /// <summary>Brief 030: today's walk into range for the checkpoint hash: no bytes for an operative that never walked in.</summary>
        internal void HashCloseToday(Hash.H h) { foreach (var op in Ops) if (op.CloseWalk || op.CloseAt != 0) h.S("close").I((int)op.Id).Z(op.CloseWalk).I(op.CloseAt); }
        internal void HashKite(Hash.H h)
        {
            h.S("kite");
            void G1(Gun g) => h.S(g.Id).I(g.KiteAt).I(g.KiteSeen).I(g.KiteLast).I(g.KiteWalked).S(g.KiteBy).P(g.KitePrev).Z(g.KiteHasPrev).Z(g.KiteCapped).I(g.KiteCapTick).I(g.KiteHoldAt).I(g.KiteCoverAt);
            foreach (var g in SecGuns) G1(g);
            foreach (var x in Hostiles) G1(x.Gun);
            G1(RivalsGun);
            foreach (var u in Units) if (!u.Legacy && !u.Patrol && !u.Robot) G1(u.Gun); // retest R3
            void D(KiteDisp d) { h.I(d.Start).I(d.Phase).I(d.BackStart).P(d.Face).I(d.P.Points.Length); foreach (var p in d.P.Points) h.P(p); }
            foreach (var kv in secDisp) { h.I(kv.Key); D(kv.Value); }
            foreach (var kv in unitDisp) { h.S(kv.Key); D(kv.Value); }
            h.Z(rivalsDisp != null); if (rivalsDisp != null) D(rivalsDisp);
        }
    }
}
