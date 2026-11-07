// N65, a witness stopped before the report lands (Pulse §3.2), moved out of ScenariosTakedown.cs (over 500 lines) by engine 0.48.0 and
// rewritten to its rule: Pulse's people rebuilt on today's plan (brief 098) put clerk 34, the finder it named, in a rocket-west home, so he
// never passes the body. The body is laid where the rule wants it, found from today's walks (FinderSpot): on the way home of people of the
// city who must walk to a phone to report it (not staff, who radio it), far from every phone, carried there unseen.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static void WitnessStopped(Run r)
        {
            var g = r.G;
            var w73 = WalkerById(r, "p-records-office-73");
            int t = T(245); // brief 055: 4:05, was 4:00, as N56 (p-pylon-row-east-04 now walks into view at 4:00)
            r.To(t - T(25)); MeetFromBehind(r, w73, t); r.To(t);
            r.Takedown("p-records-office-73", "kill"); r.Done(Enf);
            var a = ActOn(r, "p-records-office-73");
            r.Body(Enf, "carry", "p-records-office-73"); r.Done(Enf);
            var found = FinderSpot(r, g.Tick, T(460)); // straight on from the kill, while the clerks are at their desks
            if (!found.HasValue)
            {
                // Pulse's café fix (c013): the terrace's residents walk home past the Office side 3:30-5:30. Wait them out at the bins
                // (Vantage K5, as N56), then carry on at 5:30: they are home.
                r.To(r.SendTo(Enf, g.S.Grid.NearestFree(new Pt(740, -6180))!.Value));
                r.Body(Enf, "drop"); r.Done(Enf);
                r.To(T(330));
                r.Body(Enf, "carry", "p-records-office-73"); r.Done(Enf);
                found = FinderSpot(r, g.Tick, T(460));
            }
            if (!r.Is(found.HasValue, "a spot on today's walks home, far from every phone and out of every watcher's view, where whoever finds him must walk to a phone")) return;
            var (spot, finder, passes, setOff) = found!.Value;
            r.To(setOff); WalkUnseen(r, spot); // he waits until the way is clear, then carries him round what the watchers see
            r.Body(Enf, "drop"); r.Done(Enf);
            r.Is(g.Tick < passes, $"laid before {finder} passes ({Report.Clock(passes)})");
            r.Is(!a.Witnessed, "unseen: the kill and the carry");
            if (g.ReportPoint(spot) is var rp && rp.HasValue) { var pw = rp.Value.path; r.SendTo(Enf, g.Walk.NearestFree(pw.At(Math.Min(pw.Length, 20000))) ?? spot); } // off the body, 20 m along the way its finder will walk to the phone
            r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id), passes + T(30) - g.Tick), $"someone finds the body ({Report.Clock(g.Tick)})");
            // by his nature he may look first (check), then walk to the phone: the report in transit is the first that walks
            r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.Phase == "walk"), T(60)), $"a finder walks to a phone ({Report.Clock(g.Tick)})");
            var rep = g.Reps.FirstOrDefault(x => x.Act == a.Id && x.Phase == "walk");
            if (!r.Is(rep != null && rep.LandAt > g.Tick + T(10) && g.Walkers.Exists(x => x.P.Id == rep.By), $"the first finder walks to a phone: the report is in transit ({(rep == null ? "none" : rep.By + ", lands " + Report.Clock(rep.LandAt))})")) return;
            var who = rep!.By; var wf = WalkerById(r, who);
            // meet him on his walk: the point of his route the Enforcer reaches first
            Pt meet = wf.Run!.End; int meetT = int.MaxValue;
            for (long mm = 0; mm <= wf.Run.Length; mm += 200)
            {
                var q = wf.Run.At(mm); int his = wf.RunStart + (int)(mm / wf.Pace);
                var route = g.S.Grid.Route(r.Op(Enf).Pos, g.S.Grid.NearestFree(q) ?? q); if (route == null) continue;
                int mine = g.Tick + (int)(new Path(route).Length / g.R.RunSpeed) + T(2);
                if (mine < his) { meet = q; meetT = his; break; }
            }
            r.Is(meetT < rep.LandAt, "the Enforcer can be on his way before he reaches the door");
            g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", Point = g.S.Grid.NearestFree(meet) ?? meet, Run = true });
            r.Until(() => g.CrowdPos(wf) is Pt pf && G.Within(pf, r.Op(Enf).Pos, g.R.TakedownReachCm), T(60));
            r.Takedown(who, "knockout");
            r.Is(rep.Phase == "stopped" && g.Tick < rep.LandAt, "taken down before his report lands: it is dropped");
            var af = ActOn(r, who);
            bool screamed = g.Log.Exists(l => l.Code == "witness.scream" && l.Text.StartsWith(who, StringComparison.Ordinal));
            r.Is(af.Aware == screamed, af.Aware ? "he saw the Enforcer coming: he struggles (+3 s) and screams" : "taken unawares: no struggle, no scream"); // engine 0.48.0: the takedown's own rule, either way (where they meet is today's walks')
            // engine 0.30.0 (the face margin's walks): a knockout seen is radioed in (Alert 3), so the response may come and a unit find and
            // confirm the dead guard before any other walker's report lands: the body is still found by someone else, later
            r.Is(r.Until(() => a.Confirmed || g.Reps.Exists(x => x.Act == a.Id && x.Phase == "landed"), T(150)), $"the body is found by the next finder or a unit ({Report.Clock(g.Tick)})");
            var next = g.Reps.FirstOrDefault(x => x.Act == a.Id && x.Phase == "landed");
            r.Is(a.Confirmed || (next != null && next.By != who), $"by someone else, later ({(next != null ? next.By : a.Confirmed ? "a unit's confirmation" : "nobody")})");
            // the Mercenary check (Sky merged call 4) moved to N68 on 2026-10-19 (Keel, brief 023): with Pulse's leave stagger no Mercenary passes this body
            bool face = g.PersonById(who)!.Has("attacked-aware") && g.FactsOf(g.PersonById(who)!).Contains("enforcer-face");
            r.Is(face == af.Aware, af.Aware ? $"{who} holds the Enforcer's face" : $"{who}, taken unawares, holds no face");
        }

        /// <summary>The walk grid's cells a watcher sees now (the door guard, a camera, the patrol where it stands), for a carry round them.</summary>
        static bool[] WatchedMask(Game g)
        {
            var gr = g.S.Grid; var m = new bool[gr.Nx * gr.Ny];
            for (int i = 0; i < gr.Nx; i++) for (int j = 0; j < gr.Ny; j++) if (gr.FreeCell(i, j) && g.WatchedAt(gr.Centre(i, j))) m[i * gr.Ny + j] = true;
            return m;
        }
        /// <summary>The Enforcer walks the carry's way round the watched cells, a few cells a leg (each leg the grid's own short walk).</summary>
        static void WalkUnseen(Run r, Pt to)
        {
            var g = r.G; var rt = g.S.Grid.Route(g.S.Grid.NearestFree(r.Op(Enf).Pos) ?? r.Op(Enf).Pos, to, WatchedMask(g));
            if (rt == null) { r.To(r.SendTo(Enf, to)); return; }
            for (int k = 3; k < rt.Length; k += 3) r.To(r.SendTo(Enf, rt[k]));
            r.To(r.SendTo(Enf, to));
        }

        /// <summary>Where N65 lays the body, by its rule: a point on the way home of a person of the city (not staff, who radio it; not one the dial
        /// added), 15 m or more from every phone (a staffed door, a call box: a walk of 10 s or more) and out of every watcher's view, which he
        /// passes between `from` + 20 s and `to`, once the Enforcer can have carried the body there at his walk round every cell a watcher sees
        /// (WatchedMask) with nobody within witness range and 2 m of him on the way (setting off from where he waits, 5 s at a time, once the way
        /// is clear) and dropped it, while everyone else who comes within that of it before then is a person of the city too: whoever finds him
        /// walks to a phone. The nearest such pass to the Enforcer (the shortest carry; then the earliest), with who passes, when, and when he
        /// sets off; null when there is none. Their routine's places, a second at a time.</summary>
        static (Pt at, string who, int when, int carry)? FinderSpot(Run r, int from, int to)
        {
            var g = r.G; int range = g.R.WitnessRangeCm + 200, n = Math.Max(0, (to - from) / Game.Hz) + 1, walk = Math.Max(1, g.R.CellSpeed);
            var phones = new List<Pt>(); foreach (var (_, at, kind) in g.S.Refuges) if (kind == "door") phones.Add(at); foreach (var (_, at) in g.S.CallBoxes) phones.Add(at);
            var ws = g.Walkers.Where(w => !w.P.Down).ToList(); var pos = new Pt?[ws.Count, n]; var radios = new bool[ws.Count];
            for (int k = 0; k < ws.Count; k++) { radios[k] = ws[k].Added || g.R.StaffJobs.Contains(ws[k].P.Job); for (int s = 0; s < n; s++) pos[k, s] = g.CrowdPos(ws[k], from + s * Game.Hz); }
            (Pt at, string who, int when, int carry)? best = null; long bd = long.MaxValue; var me = r.Op(Enf).Pos;
            var unseenMask = WatchedMask(g);
            for (int k = 0; k < ws.Count; k++)
            {
                if (radios[k]) continue;
                for (int s = 20; s < n; s++)
                {
                    if (pos[k, s] is not Pt p || !g.S.Grid.Free(p) || g.WatchedAt(p) || phones.Exists(ph => G.Within(p, ph, 1500))) continue;
                    if ((long)G.Dist(me, p) * Game.Hz > (long)walk * s * Game.Hz) continue; // too far to carry him there in time, by the straight line
                    if (g.S.Grid.Route(g.S.Grid.NearestFree(me) ?? me, p, unseenMask) is not Pt[] rt || rt.Length < 2) continue; // round every cell a watcher sees
                    var way = new Path(rt); int carry = (int)((way.Length / 10 + walk - 1) / walk);
                    int wait = -1; // the first start (5 s steps) whose carry nobody sees, laid and the Enforcer off it before he comes
                    for (int d = 0; d + carry + g.R.DropS + 15 <= s && wait < 0; d += 5)
                    {
                        bool unseen = true;
                        for (int u = 0; u <= carry && unseen; u++) { var e = way.At(Math.Min(way.Length, (long)u * walk * 10)); for (int o = 0; o < ws.Count; o++) if (pos[o, d + u] is Pt q && G.Within(q, e, range)) { unseen = false; break; } } // and nobody of the city sees the carry
                        if (unseen) wait = d;
                    }
                    if (wait < 0) continue;
                    int lay = wait + carry + g.R.DropS + 5; bool ok = true;
                    for (int o = 0; o < ws.Count && ok; o++) { if (o == k || !radios[o]) continue; for (int u = lay; u <= s; u++) if (pos[o, u] is Pt q && G.Within(q, p, range)) { ok = false; break; } }
                    if (!ok) continue;
                    long d2 = G.D2(p, me); int when = from + s * Game.Hz;
                    if (best == null || d2 < bd || (d2 == bd && when < best.Value.when)) { bd = d2; best = (p, ws[k].P.Id, when, from + wait * Game.Hz); }
                    break;
                }
            }
            return best;
        }
    }
}
