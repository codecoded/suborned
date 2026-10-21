// Brief 027 retest (EXPERIMENTAL fight.lab; Suture retest.md R1–R6; Pulse retest-fixes.md §6.3, renumbered; Motive tuning.md §7.2–7.5),
// each on a start with the lab on:
//    N159 the chase budget is never a freeze: a shooter stepping back after each shot for 40 s; no officer stands uncapped with less than a
//         grid cell of budget while it can't answer; a spent budget is capped, and re-arms kite.rearm later while the shooter still fires
//    N160 the police contest the getaway: in a loud final fight the first fight.lab.policeContest officers stand within ai.stopReach of
//         their effective ring of the car; the others keep their stands
//    N161 in the final fight a police body shot from out of its reach takes cover against the shooter, no farther from the car than its
//         stand, and never chases (no chase budget spent)
//    N162 the brief-013 police units answer: one shot from the band on a unit at its scene moves it (or draws its fire) within 1.0 s,
//         and alarms the other unit of its dispatch
//    N163 the pre-fight crew does not lean back out into a shooter who still stands on its line; past kite.hideMax it walks on toward him
//    N164 every drive-back of the pre-fight crew is a fresh +hit-trail (Motive §7.4): the crew's hit flag clears when it is driven back
//    N165 the snap never lands on a point a hostile stands on or walks to (Motive §7.5: the blocker's cv-087)
using System;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static bool IsRivalId(string id) => id.StartsWith("rival-", StringComparison.Ordinal);
        static void FightLabRetest()
        {
            Add("N159", "—", "Brief 027 retest R2 (Pulse retest-fixes.md §1b–1c): the Tech runs 6 m straight back from security-1 after each of his shots for 60 s. No officer ever stands uncapped with less than a grid cell of chase budget left while it can't answer (the old freeze); a spent budget is capped; kite.rearm later, the Tech still firing, it re-arms and closes again; no arm walks past kite.chase.", r =>
            {
                var g = r.G; var st = KiteStage(r);
                if (!r.Is(st.HasValue, "the response unit stands at the Office front")) return;
                var (s1, s2) = st!.Value; int eff = g.PistolW.EffCm, cell = g.S.Grid.Cell;
                var tp = KiteSpot(g, s1, eff + 50, eff + 250, eff + 50, s2);
                if (!r.Is(tp.HasValue, "a spot 15.5–17.5 m from security-1")) return;
                r.To(r.SendTo(Tech, tp!.Value)); r.Do(Tech, "draw"); r.Wait(g.DrawT + 1);
                r.Fire(Tech, "security-1");
                int seen = 0, maxWalk = 0, frozen = 0, end = g.Tick + T(60);
                var lastPos = new Pt?[g.SecGuns.Length]; var stillFor = new int[g.SecGuns.Length];
                while (g.Tick < end && !g.Over && !r.Op(Tech).Down && !g.SecGuns[0].Down)
                {
                    int n = r.Shots().Count(s => s.by == "tech");
                    if (n > seen)
                    {
                        seen = n; var me = r.Op(Tech).Pos; var them = g.SecOfficerPose(0)?.pos ?? s1;
                        long dx = me.X - them.X, dy = me.Y - them.Y; long len = G.Isqrt(dx * dx + dy * dy); if (len < 1) len = 1;
                        var back = g.S.Grid.NearestFree(new Pt(me.X + (int)(dx * 600 / len), me.Y + (int)(dy * 600 / len)));
                        if (back.HasValue) g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "move", Point = back.Value, Run = true });
                        g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "fire", Target = "security-1" });
                    }
                    for (int k = 0; k < g.SecGuns.Length; k++)
                    {
                        var sg = g.SecGuns[k]; maxWalk = Math.Max(maxWalk, sg.KiteWalked);
                        var p = g.SecOfficerPose(k)?.pos; stillFor[k] = p.HasValue && p == lastPos[k] ? stillFor[k] + 1 : 0; lastPos[k] = p;
                        if (p.HasValue && sg.KiteAt >= 0 && !sg.KiteCapped && g.Labr!.KiteChaseCm - sg.KiteWalked < cell && stillFor[k] > T(2) && !r.Shots().Any(s => s.by == sg.Id && s.tick > g.Tick - T(2))) frozen++;
                    }
                    g.Step();
                }
                int slack = g.R.SecuritySpeed / 10 + 1;
                r.Eq(frozen, 0, "ticks an officer stood uncapped and silent with less than a grid cell of budget");
                r.Is(maxWalk <= g.Labr!.KiteChaseCm + slack, $"no arm walks past the budget ({maxWalk} cm, budget {g.Labr.KiteChaseCm} cm)");
                var capped = g.Log.Where(l => l.Code == "ai.kite" && l.Text.Contains("has walked its")).ToList();
                var again = g.Log.Where(l => l.Code == "ai.kite" && l.Text.Contains("closes again")).ToList();
                r.Is(capped.Count > 0, $"a spent budget is capped ({capped.Count})");
                r.Is(again.Count > 0 && again[0].Tick - capped[0].Tick >= g.Labr.KiteRearm, $"kite.rearm ({g.Labr.KiteRearm} ticks) later, the Tech still firing, it closes again ({again.Count})");
            }, LabStart(s => s.Security = 1));

            Add("N160", "—", "Brief 027 retest R1 (Pulse retest-fixes.md §2; Motive tuning.md §7.2): N10's loud route under the lab. The first fight.lab.policeContest officers' stands are pulled to the first point of their route to the car within ai.stopReach of their effective ring of it; the other officers keep their stands as placed.", r =>
            {
                var g = r.G; LoudToCar(r);
                if (!r.Until(() => g.Hostiles.Count > 0, T(5))) { r.Is(false, "the officers come"); return; }
                var car = g.S.At(g.S.GetawayCar); int n = g.Labr!.PoliceContest;
                r.Is(n >= 1, $"fight.lab.policeContest is {n}");
                var officers = g.Hostiles.Where(h => h.Id.StartsWith("officer-", StringComparison.Ordinal)).ToList();
                for (int i = 0; i < officers.Count; i++)
                {
                    var h = officers[i]; var stand = g.S.Stand(g.S.FightPolice[i]);
                    long ring = (long)h.Gun.W!.EffCm * g.R.StopReachPerMille / 1000 + g.S.Grid.Cell;
                    if (i < n) r.Is(G.Dist(h.Home, car) <= ring && G.Dist(stand, car) > ring - g.S.Grid.Cell, $"{h.Id} stands {G.Dist(h.Home, car) / 100} m from the car (its stand as placed {G.Dist(stand, car) / 100} m)");
                    else r.Eq(h.Home, stand, $"{h.Id} keeps its stand");
                }
                r.Is(g.Log.Exists(l => l.Code == "ai.kite" && l.Text.Contains("comes for the car")), "the line says so");
            }, LabStart(s => s.Security = 1));

            Add("N161", "—", "Brief 027 retest (Motive tuning.md §7.2 rule 3): N10's loud route under the lab, fought out to the car by the matrix player. No police body of the final fight ever chases: none spends chase budget and none logs a chase answer after the fight starts; each one fired on from beyond its reach that moves takes a cover point shielding it from the shooter, no farther from the car than its stand, or, with no such point, closes to its ring of the shooter (Suture retest2.md N1).", r =>
            {
                var g = r.G; LoudToCar(r);
                if (!r.Until(() => g.Hostiles.Count > 0, T(5))) { r.Is(false, "the officers come"); return; }
                int f0 = g.FightStart; var car = g.S.At(g.S.GetawayCar);
                r.FightItOut(150, Scout, Fixer, Tech);
                var police = g.Hostiles.Where(h => !IsRivalId(h.Id)).ToList();
                r.Eq(police.Sum(h => h.Gun.KiteWalked), 0, "chase budget spent by the fight's police bodies");
                r.Is(!g.Log.Exists(l => l.Tick >= f0 && l.Code == "ai.kite" && l.Text.Contains(" answers ") && !l.Text.StartsWith("rival", StringComparison.Ordinal)), "no chase answer after the fight starts");
                var answers = g.Log.Where(l => l.Tick >= f0 && l.Code == "ai.kite" && l.Text.Contains("out of its reach")).ToList();
                var cover = answers.Where(l => l.Text.Contains("takes cover at ")).ToList();
                foreach (var l in answers.Where(l => !l.Text.Contains("takes cover at "))) { string id = l.Text.Substring(0, l.Text.IndexOf(' ')); r.Is(l.Text.Contains("has no cover") && l.Text.Contains("closes to its ring") && (police.Exists(x => x.Id == id) || id.StartsWith("police-", StringComparison.Ordinal)), $"{id} with no cover closes to its ring (retest2 N1): {l.Text}"); }
                foreach (var l in cover)
                {
                    string id = l.Text.Substring(0, l.Text.IndexOf(' ')); int at = l.Text.IndexOf("takes cover at ", StringComparison.Ordinal); string cid = l.Text.Substring(at + 15, l.Text.IndexOf(' ', at + 15) - at - 15);
                    var cv = g.S.Cover.First(c => c.Id == cid); var h = police.First(x => x.Id == id);
                    int k = int.Parse(id.Substring(id.IndexOf('-') + 1)) - 1; var stand = id.StartsWith("officer-", StringComparison.Ordinal) && k < g.S.FightPolice.Length ? g.S.Stand(g.S.FightPolice[k]) : (Pt?)null;
                    if (stand.HasValue && k >= g.Labr!.PoliceContest) r.Is(G.Dist(cv.At, car) <= G.Dist(stand.Value, car), $"{id} takes {cid}, {G.Dist(cv.At, car) / 100} m from the car (its stand {G.Dist(stand.Value, car) / 100} m)");
                    else if (stand.HasValue) r.Is(cv.Q >= g.R.CoverQPartial, $"{id}, the contest officer, answers as any police body (tuning.md §7.11.1, no longer held to the car): it takes {cid}, real cover ({G.Dist(cv.At, car) / 100} m from the car)");
                    else r.Is(G.Dist(cv.At, car) <= G.Dist(h.Home, car) + g.R.SeekPoliceCm, $"{id} takes {cid} on its way ({G.Dist(cv.At, car) / 100} m from the car)");
                }
                r.Is(cover.Count > 0, $"{cover.Count} police bodies fired on from beyond their reach took cover ({answers.Count - cover.Count} closed in, no cover)");
            }, LabStart(s => s.Security = 1));

            Add("N162", "—", "Brief 027 retest R3 (Pulse retest-fixes.md §4): a brief-013 police unit holding at its scene, shot by the Fixer from beyond its ring with a clear line, moves within 1.0 s (or fires on him), and the shot alarms the other unit of its dispatch.", r =>
            {
                var g = r.G; var st = KiteStage(r);
                if (!r.Is(st.HasValue, "the response unit stands at the Office front")) return;
                Unit? u = null;
                r.Until(() => { u = g.Units.FirstOrDefault(x => !x.Legacy && !x.Patrol && !x.Robot && !x.Gun.Down && (x.St == "hold" || x.St == "search" || x.St == "check") && g.UnitPose(x).HasValue); return u != null; }, T(120));
                if (!r.Is(u != null, "a police unit at its scene")) return;
                var at = g.UnitPose(u!)!.Value.pos; int eff = u!.Gun.W!.EffCm;
                var far = g.SecGuns.Select((sg, k) => g.SecOfficerPose(k)?.pos).Where(p => p.HasValue).Select(p => p!.Value).Concat(g.Units.Where(x => x != u && !x.Gun.Down && g.UnitPose(x).HasValue).Select(x => g.UnitPose(x)!.Value.pos)).ToArray();
                var fp = KiteSpot(g, at, eff + 50, eff + 250, eff + 50, far);
                if (!r.Is(fp.HasValue, $"a spot 15.5–17.5 m from {u.Id}, beyond every other gun's ring")) return;
                r.To(r.SendTo(Fixer, fp!.Value)); r.Do(Fixer, "draw"); r.Wait(g.DrawT + 1);
                if (!(u.St == "hold" || u.St == "search" || u.St == "check")) { r.Is(false, $"{u.Id} still at its scene"); return; }
                var at1 = g.UnitPose(u)!.Value.pos;
                r.Fire(Fixer, u.Id);
                if (!r.Is(r.Until(() => r.Shots().Any(s => s.by == "fixer" && s.target == u.Id), T(5)), "the Fixer's shot goes")) return;
                int t0 = r.Shots().First(s => s.by == "fixer" && s.target == u.Id).tick;
                r.To(t0 + T(1));
                bool moved = g.UnitPose(u)?.pos != at1, fired = r.Shots().Any(s => s.by == u.Id && s.tick >= t0);
                r.Is(moved || fired, $"{u.Id} answers within 1.0 s ({(moved ? "it moves" : fired ? "it fires" : "nothing")})");
                var mate = g.Units.FirstOrDefault(x => x != u && !x.Legacy && !x.Patrol && !x.Robot && x.Act == u.Act && !x.Gun.Down);
                if (mate != null) r.Is(mate.Gun.KiteAt >= 0 || mate.Gun.KiteLast >= 0, $"{mate.Id}, of the same dispatch, is alarmed too");
            }, LabStart(s => s.Security = 2));

            Add("N163", "—", "Brief 027 retest R4 (Pulse retest-fixes.md §5.2; at security 0, the patrol downed first, as it turns toward gunfire): N158's crew, shot at once from beyond its ring by the Tech, who then stays drawn where he stood. The crew hides and does not lean back out while he stands within its max ring of the lean-out point; past kite.hideMax of holding it walks on toward him.", r =>
            {
                var g = r.G; var st = KiteStage(r);
                if (!r.Is(st.HasValue, "the response unit stands at the Office front")) return;
                if (!r.Until(() => g.RivalsDrawn && g.RivalsPresent && g.RivalsPose().HasValue, T(120))) { r.Is(false, "the rivals come"); return; }
                Pt? lastR = null; int still = 0;
                r.Until(() => { var p = g.RivalsPose()?.pos; still = p == lastR ? still + 1 : 0; lastR = p; return still >= 2 || !g.RivalsPresent; }, T(150));
                var rp = g.RivalsPose()?.pos; if (!r.Is(rp.HasValue && still >= 2, "the rivals stand at the Office")) return;
                if (!g.PatrolDown) { var up = new[] { Scout, Fixer }.Where(op => !r.Op(op).Down).ToArray(); foreach (var op in up) r.Do(op, "draw"); r.Wait(g.DrawT + 1); foreach (var op in up) r.Fire(op, "patrol"); r.Until(() => g.PatrolDown || g.Over || up.All(op => r.Op(op).Down), T(90)); } // the patrol, which turns toward gunfire, out of the stage
                if (!r.Is(g.PatrolDown && !g.Over, "the patrol is down")) return;
                if (!r.Is(g.RivalsPose().HasValue, "the crew still stands")) return; rp = g.RivalsPose()!.Value.pos;
                int eff = g.RivalsGun.W!.EffCm; var (o1, o2) = st!.Value; var rpos = rp!.Value;
                Pt? tp = null;
                for (int rr = eff + 50; rr <= eff + 250 && tp == null; rr += 100)
                    for (int k = 0; k < 72 && tp == null; k++)
                    {
                        double ang = k * Math.PI / 36; var q0 = new Pt(rpos.X + (int)Math.Round(rr * Math.Cos(ang)), rpos.Y + (int)Math.Round(rr * Math.Sin(ang)));
                        if (!g.S.Grid.Free(q0) || G.D2(q0, o1) <= (long)(eff + 50) * (eff + 50) || G.D2(q0, o2) <= (long)(eff + 50) * (eff + 50) || !g.S.Sight(q0, rpos, g.R.Eye)) continue;
                        if (g.PreviewAnswer(OpId.Tech, "rivals", q0) is KiteAnswer ka && ka.Side == "rivals" && ka.Cover) tp = q0;
                    }
                if (!r.Is(tp.HasValue, "a spot 15.5–17.5 m from the crew from which it has cover to hide behind")) return;
                foreach (var op in new[] { Scout, Fixer }) if (!r.Op(op).Down) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "holster" });
                r.To(r.SendTo(Tech, tp!.Value)); r.Do(Tech, "draw"); r.Wait(g.DrawT + 1);
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "volley", Target = "rivals", Shots = 1 }).ok, "one targeted shot on the crew");
                if (!r.Is(r.Until(() => r.Shots().Any(s => s.by == "tech" && s.target == "rivals"), T(3)), "the Tech's shot goes")) return;
                g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "stance", Stance = "hold" }); // he stays drawn on the line, and fires no more
                if (!r.Is(r.Until(() => g.Log.Exists(l => l.Code == "ai.kite" && l.Text.StartsWith("rivals answer", StringComparison.Ordinal)), T(3)), "the crew answers")) return;
                int quietFrom = g.Tick;
                r.Until(() => g.Log.Exists(l => l.Code == "ai.kite" && (l.Text.StartsWith("rivals walk on", StringComparison.Ordinal) || l.Text.StartsWith("rivals lean out", StringComparison.Ordinal))) || g.Over, g.Labr!.KiteHide + g.Labr.KiteHideMax + T(30));
                r.Is(!g.Log.Exists(l => l.Code == "ai.kite" && l.Text.StartsWith("rivals lean out", StringComparison.Ordinal)), "it never leans back out into him");
                var on = g.Log.FirstOrDefault(l => l.Code == "ai.kite" && l.Text.StartsWith("rivals walk on", StringComparison.Ordinal));
                r.Is(on.Code == "ai.kite" && on.Tick - quietFrom >= g.Labr.KiteHideMax, $"past kite.hideMax ({g.Labr.KiteHideMax} ticks) it walks on toward him");
            }, LabStart(s => s.Security = 0));

            Add("N164", "—", "Brief 027 retest (Motive tuning.md §7.4): the cell drives the pre-fight crew back. Under the lab the crew's hit flag clears with its health, so its next drive-back's first hit is +hit-trail again (under today it stays set: one +hit-trail a mission; lab-gated, so today's hashes hold it), and a shot landing later in the drive-back's tick finds the crew gone.", r =>
            {
                var g = r.G; var st = KiteStage(r);
                if (!r.Is(st.HasValue, "the response unit stands at the Office front")) return;
                Pt? lr = null; int sl = 0; if (!r.Until(() => { var p = g.RivalsDrawn && g.RivalsPresent ? g.RivalsPose()?.pos : null; sl = p.HasValue && p == lr ? sl + 1 : 0; lr = p; return sl >= 2; }, T(150))) { r.Is(false, "the rivals come and stand"); return; }
                var rp = g.RivalsPose()!.Value.pos;
                var spot = g.S.Grid.Nearest(rp, q => G.D2(q, rp) <= 1000L * 1000 && G.D2(q, rp) >= 500L * 500 && g.S.Sight(q, rp, g.R.Eye), 2000);
                if (!r.Is(spot.HasValue, "a spot 5–10 m from the crew")) return;
                var up = new[] { Tech, Fixer, Scout }.Where(op => !r.Op(op).Down).ToArray();
                r.To(up.Select(op => r.SendTo(op, spot!.Value)).Max());
                foreach (var op in up) { r.Do(op, "draw"); }
                r.Wait(g.DrawT + 1);
                foreach (var op in up) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "fire", Target = "rivals" });
                if (!r.Is(r.Until(() => g.Log.Exists(l => l.Code == "rivals.retreat") || g.Over, T(30)) && !g.Over, "the crew is driven back")) return;
                r.Is(g.RivalsGun.Health == g.RivalsGun.Max && !g.RivalsGun.Hit, "its health back and its hit flag clear: the next drive-back's first hit is +hit-trail (today the flag stays set: one +hit-trail a mission)");
                r.Is(!g.Log.Exists(l => l.Tick == g.Tick && l.Code == "rivals.hit" && g.Log.IndexOf(l) > g.Log.FindIndex(x => x.Code == "rivals.retreat")), "a shot landing later in the drive-back's tick finds the crew gone");
            }, LabStart(s => s.Security = 1));

            Add("N165", "—", "Brief 027 retest (Motive tuning.md §7.5): route A to the safe fight under the lab, the Scout drawn. From the tick a blocker is picked to the end of the fight, a move of the Scout's onto the cover point the blocker walks to or stands on never snaps there (it is held, whatever its bookkeeping says).", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true); Support(r); ToAmbush(r);
                if (!r.Op(Scout).Gun.Drawn) r.Do(Scout, "draw");
                if (!r.Is(r.Until(() => g.Blockers.Count > 0 || g.Over, T(60)) && !g.Over, "a blocker goes for the road")) return;
                var b = g.Hostiles.First(h => h.Id == g.Blockers[0]); var dest = b.Home;
                var cv = g.S.Cover.FirstOrDefault(c => c.At == dest);
                if (!r.Is(cv != null, $"{b.Id} heads for a cover point ({cv?.Id})")) return;
                int bad = 0, checks = 0;
                r.Until(() => { if (!b.Down && b.Present && r.Op(Scout).Gun.Drawn && !r.Op(Scout).Down) { checks++; if (g.PreviewSnap(OpId.Scout, cv!.At)?.Id == cv.Id) bad++; } return b.Down || g.FightOverTick != Never || g.Over; }, T(40));
                r.Is(checks > 0, $"{checks} ticks checked");
                r.Eq(bad, 0, $"ticks a move onto {cv!.Id} would snap there while {b.Id} walks to it or holds it");
            }, LabStart());
        }
    }
}
