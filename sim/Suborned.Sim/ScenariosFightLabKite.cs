// Brief 027 F2, the hostiles' answer to fire from beyond their reach (EXPERIMENTAL fight.lab.kite; Pulse, briefs/027/pulse/kite-response.md
// §6.3, renumbered: N150–N154 were taken), each on a start with the lab on:
//    N155 police close in: one shot from 15.5–17.5 m moves both officers of the response unit kite.react later (one incident, one dispatch),
//         straight at the shooter (never away), until he is inside their ring; they fire. Kite off: nobody moves
//    N156 the chase budget: a shooter stepping back 4 m after each shot never draws more than kite.chase of walk from any officer
//    N157 an officer trading fire with an agent inside his ring is not pulled off it by a second agent's shot from beyond it
//    N158 the pre-fight rival crew shot at from beyond its ring walks out of the line, holds, comes back kite.hide after the last shot, and
//         its arrival clock is late by exactly the detour
// The rows (Pulse's N155) are the fight-lab adversarial rows (ScenariosFightLabAdv.cs; suborned-sim fight-lab).
using System;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The kite stage: the camera looped, the patrol rerouted, the Scout drops the guard and walks off to the getaway bay; the response
        /// unit comes and stands. Returns the two officers' standing points, or null if the stage didn't set.</summary>
        static (Pt s1, Pt s2)? KiteStage(Run r)
        {
            var g = r.G; var spot = GuardOnlySpot(r); if (!spot.HasValue) return null;
            r.To(r.SendTo(Scout, spot.Value)); r.Do(Scout, "draw"); r.Wait(g.DrawT + 1); r.Fire(Scout, GuardR);
            if (!r.Until(() => g.GuardDown, T(10))) return null;
            r.Do(Scout, "holster"); r.Send(Scout, "d:Getaway bay");
            Pt? last1 = null; int still = 0;
            r.Until(() =>
            {
                var a = g.SecOfficerPose(0)?.pos; var b = g.SecOfficerPose(1)?.pos;
                if (!a.HasValue || !b.HasValue) { still = 0; return false; }
                still = last1 == a ? still + 1 : 0; last1 = a; return still >= 2;
            }, T(150));
            var p1 = g.SecOfficerPose(0)?.pos; var p2 = g.SecOfficerPose(1)?.pos;
            return p1.HasValue && p2.HasValue && still >= 2 ? (p1.Value, p2.Value) : ((Pt, Pt)?)null;
        }
        /// <summary>A free point `lo`..`hi` cm from `a` with a line to it, and beyond `away` cm of every point in `far`.</summary>
        static Pt? KiteSpot(Game g, Pt a, int lo, int hi, int away, params Pt[] far) =>
            g.S.Grid.Nearest(a, q => G.D2(q, a) >= (long)lo * lo && G.D2(q, a) <= (long)hi * hi && g.S.Sight(q, a, g.R.Eye) && far.All(f => G.D2(q, f) > (long)away * away), hi + 500);

        static void FightLabKite()
        {
            Add("N155", "—", "Brief 027 F2 (EXPERIMENTAL fight.lab.kite; Pulse, kite-response.md): the Tech fires on security-1 from 15.5–17.5 m, beyond both officers' 15 m ring. kite.react after the shot lands both officers move (one shot, the whole side), straight at him and never away, until he is inside their ring on a line; then they fire. The same stage with the kite off: nobody moves.", r =>
            {
                var g = r.G; var st = KiteStage(r);
                if (!r.Is(st.HasValue, "the response unit stands at the Office front")) return;
                var (s1, s2) = st!.Value; int eff = g.PistolW.EffCm;
                var tp = KiteSpot(g, s1, eff + 50, eff + 250, eff + 50, s2);
                if (!r.Is(tp.HasValue, "a spot 15.5–17.5 m from security-1, beyond security-2's ring, with a line")) return;
                r.To(r.SendTo(Tech, tp!.Value)); r.Do(Tech, "draw"); r.Wait(g.DrawT + 1);
                r.Fire(Tech, "security-1");
                r.Until(() => r.Shots().Any(s => s.by == "tech"), T(3));
                int t0 = r.Shots().First(s => s.by == "tech").tick, react = g.Labr!.KiteReact;
                r.To(t0 + react - 1);
                r.Is(g.SecOfficerPose(0)?.pos == s1 && g.SecOfficerPose(1)?.pos == s2, "neither officer has moved before kite.react");
                r.Wait(2);
                r.Is(g.SecOfficerPose(0)?.pos != s1 && g.SecOfficerPose(1)?.pos != s2, $"both officers move {react} ticks after the one shot (the side together)");
                r.Eq(g.Log.Count(l => l.Code == "ai.kite" && l.Text.Contains("answers tech")), 2, "an ai.kite line for each");
                bool away = false; int firedAt = -1; long last1 = long.MaxValue;
                r.Until(() =>
                {
                    var p = g.SecOfficerPose(0)?.pos; if (!p.HasValue) return true;
                    long d = G.D2(p.Value, r.Op(Tech).Pos); if (d > last1) away = true; last1 = d;
                    if (r.Shots().Any(s => s.by.StartsWith("security-", StringComparison.Ordinal) && s.target == "tech")) { firedAt = g.Tick; return true; }
                    return r.Op(Tech).Down;
                }, T(12));
                r.Is(!away, "security-1 walks only toward the Tech (no step away)");
                r.Is(firedAt >= 0, FormattableString.Invariant($"the officers fire on the Tech {(firedAt - t0) / (double)Game.Hz:0.0} s after his first shot"));
                // the kite off: the same stage, nobody moves
                var off = new Run(Twin(g, LabStart(s => { s.Security = 1; s.LabKite = false; }))); var o = off.G;
                var so = KiteStage(off);
                if (!r.Is(so.HasValue, "kite off: the same stage")) return;
                off.To(off.SendTo(Tech, tp.Value)); off.Do(Tech, "draw"); off.Wait(o.DrawT + 1); off.Fire(Tech, "security-1");
                off.Until(() => off.Shots().Any(s => s.by == "tech"), T(3)); off.Wait(T(3));
                r.Is(o.SecOfficerPose(0)?.pos == so!.Value.s1 && o.SecOfficerPose(1)?.pos == so!.Value.s2, "kite off: 3 s after the shot both still stand (today's band)");
            }, LabStart(s => s.Security = 1));

            Add("N156", "—", "Brief 027 F2 chase budget (Pulse §2.5): the Tech steps 4 m straight back from security-1 after each of his shots for 40 s. No officer's walk on the alarm passes kite.chase (one tick's walk of slack); a spent budget is held, not refilled.", r =>
            {
                var g = r.G; var st = KiteStage(r);
                if (!r.Is(st.HasValue, "the response unit stands at the Office front")) return;
                var (s1, s2) = st!.Value; int eff = g.PistolW.EffCm;
                var tp = KiteSpot(g, s1, eff + 50, eff + 250, eff + 50, s2);
                if (!r.Is(tp.HasValue, "a spot 15.5–17.5 m from security-1")) return;
                r.To(r.SendTo(Tech, tp!.Value)); r.Do(Tech, "draw"); r.Wait(g.DrawT + 1);
                r.Fire(Tech, "security-1");
                int seen = 0, maxWalk = 0, end = g.Tick + T(40); bool capped = false;
                while (g.Tick < end && !g.Over && !r.Op(Tech).Down && !g.SecGuns[0].Down)
                {
                    int n = r.Shots().Count(s => s.by == "tech");
                    if (n > seen)
                    {
                        seen = n; var me = r.Op(Tech).Pos; var them = g.SecOfficerPose(0)?.pos ?? s1;
                        long dx = me.X - them.X, dy = me.Y - them.Y; long len = G.Isqrt(dx * dx + dy * dy); if (len < 1) len = 1;
                        var back = g.S.Grid.NearestFree(new Pt(me.X + (int)(dx * 400 / len), me.Y + (int)(dy * 400 / len)));
                        if (back.HasValue) g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "move", Point = back.Value });
                        g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "fire", Target = "security-1" });
                    }
                    foreach (var sg in g.SecGuns) { maxWalk = Math.Max(maxWalk, sg.KiteWalked); capped |= sg.KiteCapped; }
                    g.Step();
                }
                int slack = g.R.SecuritySpeed / 10 + 1; // one tick's walk, in cm (the speed is mm a tick)
                r.Is(maxWalk <= g.Labr!.KiteChaseCm + slack, $"the most any officer walked on the alarm: {maxWalk} cm, budget {g.Labr.KiteChaseCm} cm{(capped ? " (spent, then held)" : "")}");
                r.Is(seen >= 2, $"the Tech fired {seen} shots, stepping back after each");
            }, LabStart(s => s.Security = 1));

            Add("N157", "—", "Brief 027 F2 engaged exemption (Pulse §2): with the Fixer drawn 11–13 m from security-1 (inside its ring and its watch), a shot from the Tech at 15.5–17.5 m moves no officer: a body that can answer keeps its own fight.", r =>
            {
                var g = r.G; var st = KiteStage(r);
                if (!r.Is(st.HasValue, "the response unit stands at the Office front")) return;
                var (s1, s2) = st!.Value; int eff = g.PistolW.EffCm;
                var tp = KiteSpot(g, s1, eff + 50, eff + 250, eff + 50, s2);
                var fp = g.S.Grid.Nearest(s1, q => G.D2(q, s1) >= 1100L * 1100 && G.D2(q, s1) <= 1300L * 1300 && g.SecOfficerWatching(0, q, false), 1800); // inside the ring, in his watch
                if (!r.Is(tp.HasValue && fp.HasValue, "a spot for the Tech beyond the ring, one for the Fixer inside it")) return;
                r.To(Max(r.SendTo(Tech, tp!.Value), r.SendTo(Fixer, fp!.Value)));
                r.Do(Fixer, "draw"); r.Do(Tech, "draw"); r.Wait(g.DrawT + 1);
                var a1 = g.SecOfficerPose(0)?.pos; var a2 = g.SecOfficerPose(1)?.pos;
                r.Fire(Tech, "security-1");
                r.Until(() => r.Shots().Any(s => s.by == "tech"), T(3));
                int t0 = r.Shots().First(s => s.by == "tech").tick;
                r.To(t0 + g.Labr!.KiteReact + T(2));
                r.Is(g.SecOfficerPose(0)?.pos == a1 && g.SecOfficerPose(1)?.pos == a2, "2 s past kite.react neither officer has moved");
                r.Is(!g.Log.Exists(l => l.Code == "ai.kite"), "no ai.kite answer");
                r.Is(r.Shots().Any(s => s.by.StartsWith("security-", StringComparison.Ordinal) && s.target == "fixer"), "they keep trading fire with the Fixer");
            }, LabStart(s => s.Security = 1));

            Add("N158", "—", "Brief 027 F2 the pre-fight rival crew (Pulse §2): drawn by gunfire to the Office, shot at once from beyond its ring, the crew walks out of the shooter's line kite.react later, holds, walks back kite.hide after the last shot, and its arrival clock is later by exactly the detour.", r =>
            {
                var g = r.G; var st = KiteStage(r);
                if (!r.Is(st.HasValue, "the response unit stands at the Office front")) return;
                if (!r.Until(() => g.RivalsDrawn && g.RivalsPresent && g.RivalsPose().HasValue, T(120))) { r.Is(false, "the rivals come"); return; }
                Pt? lastR = null; int still = 0;
                r.Until(() => { var p = g.RivalsPose()?.pos; still = p == lastR ? still + 1 : 0; lastR = p; return still >= 2 || !g.RivalsPresent; }, T(150));
                var rp = g.RivalsPose()?.pos;
                if (!r.Is(rp.HasValue && still >= 2, "the rivals stand at the Office")) return;
                int eff = g.RivalsGun.W!.EffCm;
                var (o1, o2) = st!.Value; var rpos = rp!.Value;
                Pt? tp = null; // 72 bearings × three radii (15.5, 16.5, 17.5 m), in order: the first free point that meets the stage
                for (int rr = eff + 50; rr <= eff + 250 && tp == null; rr += 100)
                    for (int k = 0; k < 72 && tp == null; k++)
                    {
                        double ang = k * Math.PI / 36; var q0 = new Pt(rpos.X + (int)Math.Round(rr * Math.Cos(ang)), rpos.Y + (int)Math.Round(rr * Math.Sin(ang)));
                        if (!g.S.Grid.Free(q0) || G.D2(q0, o1) <= (long)(eff + 50) * (eff + 50) || G.D2(q0, o2) <= (long)(eff + 50) * (eff + 50) || !g.S.Sight(q0, rpos, g.R.Eye)) continue;
                        if (g.PreviewAnswer(OpId.Tech, "rivals", q0) is KiteAnswer ka && ka.Side == "rivals") tp = q0;
                    }
                if (!r.Is(tp.HasValue, "a spot 15.5–17.5 m from the crew, beyond the officers' rings, from which the crew has somewhere to hide (PreviewAnswer)")) return;
                r.To(r.SendTo(Tech, tp!.Value)); r.Do(Tech, "draw"); r.Wait(g.DrawT + 1);
                var at = g.RivalsPose()?.pos; int drawn0 = g.RivalsDrawnTick;
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "volley", Target = "rivals", Shots = 1 }).ok, "one targeted shot on the crew");
                if (!r.Is(r.Until(() => r.Shots().Any(s => s.by == "tech" && s.target == "rivals"), T(3)), "the Tech's shot goes")) return;
                int t0 = r.Shots().First(s => s.by == "tech" && s.target == "rivals").tick;
                r.Do(Tech, "holster");
                r.To(t0 + g.Labr!.KiteReact + 2);
                r.Is(g.RivalsPose()?.pos != at && g.Log.Exists(l => l.Code == "ai.kite" && l.Text.StartsWith("rivals answer", StringComparison.Ordinal)), "the crew moves out of the line after kite.react");
                var ans = g.Log.Where(l => l.Code == "ai.kite" && l.Text.StartsWith("rivals answer", StringComparison.Ordinal)).ToList();
                if (ans.Count == 0) return;
                var answered = ans[0].Tick;
                r.Until(() => g.Log.Exists(l => l.Code == "ai.kite" && l.Text.StartsWith("rivals lean out", StringComparison.Ordinal)), T(60));
                var back = g.Log.FirstOrDefault(l => l.Code == "ai.kite" && l.Text.StartsWith("rivals lean out", StringComparison.Ordinal));
                if (!r.Is(back.Code == "ai.kite", "the crew comes back")) return;
                r.Is(back.Tick - answered >= g.Labr.KiteHide, $"not before kite.hide ({g.Labr.KiteHide} ticks) of quiet");
                r.Eq(g.RivalsDrawnTick - drawn0, back.Tick - answered, "the arrival clock is late by exactly the detour");
                r.Eq(g.RivalsPose()?.pos, at, "back where it stood");
            }, LabStart(s => s.Security = 1));
        }
    }
}
