// Brief 096, engine 0.40.0 (Keel): bribes on camera (rulebook camera; Motive's rules page, briefs/096/motive/rules.html, tests CAM-1..CAM-9;
// Sky 2026-11-04 "all. recommended"). Values are read from the rulebook, never written here; seeds where Motive asked for them.
//  CAM-1 pay-live-camera      the Pay at Lukac's spot under the live pickup camera: recorded, lands at the desk's next scan + 3 s; +1 T on the Pay's own;
//                             Alert 1; one incident; Standing unmoved; the Fixer's face on the lead; the card said so before the commit
//  CAM-2 pay-under-loop       the same under a loop that covers the whole Pay: no record, the Pay's own Trail, Alert 0; the card shows no eye
//  CAM-3 loop-ends-mid-pay    the loop runs out 10 s into the Pay: recorded from the loop's end; the card said "loop ends · on camera"
//  CAM-4 help-stays-private   Help with the camera live: no record
//  CAM-5 pressure-one-incident a Pressure seen by a camera and by people: one incident, +1 T, not +2
//  CAM-6 out-of-view          a Pay where no camera sees either person (Inez at the café): no record
//  CAM-7 dark-camera          the pickup camera cut before the Pay: no record
//  CAM-8 quote-honest         200 seeds: the Pay card's eye, Alert and landing match what happens
//  CAM-9 replay               a run with a record replays byte-identical from its start and orders
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The Scout listens to the driver at his spot while the Fixer waits out of the camera (route B's opening, no turn yet).</summary>
        static void ListenDriver(Run r)
        {
            r.To(Max(r.Send(Scout, "d:Meet driver"), r.SendTo(Fixer, new Pt(7950, -3850)))); // the Fixer waits out of the pickup camera's sight (quiet-b's fixer-off)
            r.Do(Scout, "listen", Driver); r.Until(() => r.G.Log.Exists(l => l.Code == "scout.listen.heard"), T(240));
        }
        /// <summary>The Tech loops the pickup camera from the cabinet; returns when the loop runs.</summary>
        static void LoopPickup(Run r) { r.To(r.Send(Tech, "cabinet", true)); r.Do(Tech, "loop", "pickup"); r.Done(Tech); r.Is(r.G.LoopOn && r.G.LoopCam == "pickup", "the pickup camera is looped"); }
        /// <summary>A message in the invariant culture (Redline E7: every report reads the same in every culture).</summary>
        static string Iv(FormattableString f) => FormattableString.Invariant(f);
        static string AlertWhy(Game g) { var l = new List<string>(); foreach (var x in g.Ledger) if (x.what == "alert") l.Add(Iv($"{Report.Clock(x.tick)} {x.cause}")); return l.Count == 0 ? "" : Iv($" [{string.Join("; ", l)}]"); }
        static string Mark(Quote q) => Iv($"{q.Recorded} {q.RecordedBy} +{q.RecordTrail} T ▲{q.RecordAlert} in {q.RecordInS} s, loop ends {q.RecordLoopEnds}");

        static void Camera096()
        {
            Add("CAM-1", "096", "Motive CAM-1 (rulebook camera): the Fixer pays Lukac at his spot with the pickup camera live, standing in its view. The Pay card shows the camera's eye, +1 T and Alert 1 before the commit; the camera records it, the record lands at its desk's next scan + 3 s: the Pay's own Trail + 1, Alert 1 (the record's), one incident, Standing unmoved, the Fixer's face on the lead.", r =>
            {
                var g = r.G; ListenDriver(r);
                r.To(r.Send(Fixer, "d:Boarding pavilion")); // in the camera's view, beside the driver: no time to loiter
                var q = g.Preview("pay", Driver);
                r.Is(q.Recorded != "no" && q.RecordedBy == "pickup" && q.RecordTrail == g.R.CamTrail && q.RecordAlert == g.R.CamAlert && q.RecordInS > 0, Iv($"the card's camera mark before the commit ({Mark(q)})"));
                int t0 = g.Trail, s0 = g.Standing, a0 = g.Alert, inc0 = g.Incidents.Count, start = g.Tick; bool sawFixer = false;
                r.Do(Fixer, "turn", Driver, "pay");
                while (!g.Over && (r.Op(Fixer).Act != null || r.Op(Fixer).Busy != null)) { if (r.Op(Fixer).Busy != null) sawFixer |= g.CameraWatching("pickup", r.Op(Fixer).Pos); g.Step(); }
                if (!r.Is(g.CameraRecords().Count == 1, "the camera records the Pay")) return;
                var rec = g.CameraRecords()[0];
                r.Eq((rec.cam, rec.subject, rec.approach), ("pickup", Driver, "pay"), "the pickup camera's record of the Pay on the driver");
                r.Is(rec.landAt - rec.from >= T(g.R.CamCallS) && rec.landAt - rec.from < T(g.R.CamScanS) + T(g.R.CamCallS), Iv($"it lands at the desk's next scan + {g.R.CamCallS} s ({Report.Clock(rec.from)} → {Report.Clock(rec.landAt)})"));
                r.Is(Math.Abs(rec.landAt - (start + T(q.RecordInS))) <= T(1), Iv($"when the card said: {q.RecordInS} s after the order"));
                r.Until(() => g.Log.Exists(l => l.Code == "camera.record-lands"), T(60));
                r.Eq(g.Log.Find(l => l.Code == "camera.record-lands").Tick, rec.landAt, "camera.record-lands at its tick");
                r.Is(!g.Ledger.Exists(x => x.what == "alert" && x.tick < rec.landAt && x.tick >= start), "never at sight: Alert unmoved until it lands");
                r.Is(g.Driver.Turned, "the driver is turned");
                r.Eq(g.Trail, t0 + (int)q.Trail + g.R.CamTrail, Iv($"Trail: the Pay's own ({q.Trail}) + {g.R.CamTrail}"));
                r.Eq(g.Alert, Math.Max(a0, g.R.CamAlert), "Alert 1: a found fault" + AlertWhy(g));
                r.Is(g.Ledger.Exists(x => x.what == "alert" && x.cause.StartsWith("recorded:pay:driver", StringComparison.Ordinal)), "the Alert is the record's");
                r.Eq(g.Incidents.Skip(inc0).Count(i => i.Kind == "recorded"), 1, "one incident, the record's");
                r.Eq(g.Standing, s0, "Standing unmoved: the desk doesn't gossip");
                r.Is(sawFixer && rec.face, "the lead holds the Fixer's face (the camera watched him during the Pay)");
            });

            Add("CAM-2", "096", "Motive CAM-2: the same Pay under a loop that covers all of it: the card shows no eye; no record, the Pay's own Trail, Alert 0.", r =>
            {
                var g = r.G; ListenDriver(r); LoopPickup(r);
                var q = g.Preview("pay", Driver);
                r.Is(g.LoopUntil > g.Tick + T(q.WalkS) + T((int)q.S), Iv($"the loop runs past the Pay's end ({Report.Clock(g.LoopUntil)})"));
                r.Eq(q.Recorded, "no", Iv($"the card shows no eye ({Mark(q)})"));
                int t0 = g.Trail;
                r.Do(Fixer, "turn", Driver, "pay"); r.Done(Fixer); r.Wait(T(g.R.CamScanS + g.R.CamCallS + 1));
                r.Eq(g.CameraRecords().Count, 0, "no record");
                r.Eq(g.Trail, t0 + (int)q.Trail, "the Pay's own Trail only");
                r.Eq(g.Alert, 0, "Alert 0" + AlertWhy(g));
            });

            Add("CAM-3", "096", "Motive CAM-3: the loop runs out 10 s into the Pay. Before the commit the card says the loop ends inside the act, on camera; the camera records it from the loop's end.", r =>
            {
                var g = r.G; ListenDriver(r); LoopPickup(r);
                r.To(r.SendTo(Fixer, g.S.Grid.NearestFree(g.PersonPos(g.Driver)!.Value + new Pt(-150, 0)) ?? r.Op(Fixer).Pos)); // in reach: no walk in
                var q0 = g.Preview("pay", Driver);
                r.To(g.LoopUntil - T(10) - T(q0.WalkS));
                var q = g.Preview("pay", Driver);
                r.Is(q.Recorded != "no" && q.RecordLoopEnds == g.LoopUntil, Iv($"the card: loop ends {Report.Clock(g.LoopUntil)}, on camera ({Mark(q)})"));
                int ends = g.LoopUntil;
                r.Do(Fixer, "turn", Driver, "pay");
                r.Until(() => g.CameraRecords().Count > 0, T(60));
                r.Is(g.CameraRecords().Count == 1 && g.CameraRecords()[0].from == ends, Iv($"recorded from the loop's end ({Report.Clock(ends)})"));
            });

            Add("CAM-4", "096", "Motive CAM-4 (Q1 a): Help stays private: Help on the driver with the pickup camera live makes no record, and its card shows no eye.", r =>
            {
                var g = r.G; ListenDriver(r);
                r.Eq(g.Preview("help", Driver).Recorded, "no", "the Help card shows no eye");
                r.Do(Fixer, "turn", Driver, "help"); r.Done(Fixer); r.Wait(T(g.R.CamScanS + g.R.CamCallS + 1));
                r.Eq(g.CameraRecords().Count, 0, "no record");
                r.Eq(g.Alert, 0, "Alert 0" + AlertWhy(g));
            });

            Add("CAM-5", "096", "Motive CAM-5: a Pressure seen by a camera and by people at once: one act, one incident: +1 T, not +2; the record still raises Alert 1 when it lands. In the Handover no security stands in sight of a threat where a camera has a clear line (Inez's round passes through the mast camera's view, the off-duty guards elsewhere), so this plays on inputs where ordinary people mind a threat from Alert 0 (witness.notice.from 0, a behaviour-only variant as S1-S3's): the Fixer waits on Inez's round where the mast camera sees him and threatens her as she passes, the terrace in sight.", r =>
            {
                var alt = With059(r.Inputs!, "\"people-care-alert\": \\{ \"v\": 2", "\"people-care-alert\": { \"v\": 0");
                var a = Alt(r, alt); var g = a.G;
                a.Do(Scout, "watch", Inez); a.Done(Scout); a.Do(Scout, "listen", Inez); a.Until(() => g.Log.Exists(l => l.Code == "scout.listen.heard"), T(240)); // brief 101: from her start, out of the square's mast's view (17 m), so it never reports her loitering first
                Pt? spot = null; // a free point on her round, in the mast camera's view, that the Fixer reaches just before she does (engine 0.55.0: no waiting in the camera's view, which reports a loiterer)
                for (int k = 0; k < 600 && spot == null && !g.Over; k++)
                {
                    for (int t = T(4); t <= T(90) && spot == null; t += 5)
                    {
                        var ip = g.PoseAhead(g.Inez, t)?.pos; if (!ip.HasValue || !g.CameraWatching("mast-z0", ip.Value)) continue;
                        if (g.S.Grid.NearestFree(ip.Value + new Pt(0, 150)) is Pt f && g.CameraWatching("mast-z0", f) && g.Walk.Route(a.Op(Fixer).Pos, f) is Pt[] rt && new Path(rt).Length / 180 + T(2) <= t) spot = f;
                    }
                    if (spot == null) a.Wait(5);
                }
                if (!r.Is(spot.HasValue, "a point on Inez's round in the mast camera's view")) return;
                a.To(a.SendTo(Fixer, spot!.Value));
                bool Now() { var p0 = g.Preview("pressure", Inez); return p0.Witnesses > 0 && g.WalkInS(Fixer, "pressure", Inez) == 0 && g.CameraWatching("mast-z0", a.Op(Fixer).Pos); }
                for (int k = 0; k < 4200 && !Now() && !g.Over; k++) a.Wait(1); // she comes by again, people in sight
                var q = g.Preview("pressure", Inez);
                if (!r.Is(Now(), Iv($"she passes the Fixer in the camera's view, people in sight ({q.Witnesses} witnesses, {Mark(q)})"))) return;
                int t0 = g.Trail;
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = Inez, Approach = "pressure" }).ok, "the Pressure is taken where she passes (not at her post: ForHer would wait for it)");
                a.Done(Fixer);
                a.Until(() => g.CameraRecords().Exists(x => x.state == "landed") || g.Over, T(60));
                r.Is(g.Log.Exists(l => l.Code == "witness.threat") && g.CameraRecords().Count == 1, Iv($"people saw the threat and the camera recorded it (witnessed {g.Log.Exists(l => l.Code == "witness.threat")}, {g.CameraRecords().Count} records)"));
                r.Eq(g.Trail, t0 + (int)q.Trail + g.R.WitnessTrail, Iv($"Trail: the Pressure's own ({q.Trail}) + one incident's {g.R.WitnessTrail}, not two"));
                r.Is(g.Alert >= g.R.CamAlert && g.Ledger.Exists(x => x.what == "alert" && x.cause.StartsWith("recorded:pressure", StringComparison.Ordinal)), Iv($"the record lands: Alert {g.Alert}") + AlertWhy(g));
                foreach (var c in a.Checks) if (!c.ok) r.Is(false, "alt: " + c.what);
            });

            Add("CAM-6", "096", "Motive CAM-6: a Pay where no camera sees either person: no record, and the card shows no eye. Brief 101: the square's mast watches Inez's post, so the Pay is given at the far table her round serves out of its view (the act holds her there).", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "beside-inez"));
                for (int k = 0; k < 360 && g.Preview("pay", Inez).Recorded != "no" && !g.Over; k++) r.Wait(T(1)); // her round takes her out of the mast camera's view
                var q = g.Preview("pay", Inez);
                r.Eq(q.Recorded, "no", Iv($"the card shows no eye ({Mark(q)})"));
                bool anySeen = false; r.Is(g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = Inez, Approach = "pay" }).ok, "the Pay is given as the card reads, where she is (brief 101: ForHer would wait for her post, in the square's mast's view)");
                while (!g.Over && (r.Op(Fixer).Act != null || r.Op(Fixer).Busy != null)) { foreach (var c in g.S.Cameras) anySeen |= g.CameraWatching(c.Id, r.Op(Fixer).Busy != null ? r.Op(Fixer).Pos : new Pt(0, 0)) || (g.PersonPos(g.Inez) is Pt ip && r.Op(Fixer).Busy != null && g.CameraWatching(c.Id, ip)); g.Step(); }
                r.Wait(T(g.R.CamScanS + g.R.CamCallS + 1));
                r.Is(!anySeen, "no camera watched either of them during the Pay");
                r.Eq(g.CameraRecords().Count, 0, "no record");
            });

            Add("CAM-7", "096", "Motive CAM-7: the pickup camera cut (Disable) before the Pay: no record; the card shows no eye.", r =>
            {
                var g = r.G; ListenDriver(r);
                r.To(r.Send(Tech, "cabinet", true)); r.Do(Tech, "disable", "pickup"); r.Done(Tech);
                r.Is(g.CameraDark("pickup"), "the pickup camera is dark");
                r.Eq(g.Preview("pay", Driver).Recorded, "no", "the card shows no eye");
                r.Do(Fixer, "turn", Driver, "pay"); r.Done(Fixer); r.Wait(T(g.R.CamScanS + g.R.CamCallS + 1));
                r.Eq(g.CameraRecords().Count, 0, "no record");
            });

            Add("CAM-8", "096", "Motive CAM-8 (the R-4 check, extended), 200 seeds: on every seed the Pay card read before the commit (its eye, its Alert, when it lands) matches what happens.", r =>
            {
                var g = r.G; r.Wait(1); int honest = 0, recorded = 0; var bad = new List<string>();
                for (int seed = 1; seed <= 200; seed++)
                {
                    var s = new Run(Engine.New(r.Inputs!, new Start { Seed = seed })) { Inputs = r.Inputs };
                    ListenDriver(s); var q = s.G.Preview("pay", Driver); int t0 = s.G.Tick;
                    s.G.Issue(new Order { Tick = s.G.Tick, Op = Fixer, Do = "turn", Target = Driver, Approach = "pay" }); s.Done(Fixer); s.Wait(T(s.G.R.CamScanS + s.G.R.CamCallS + 1));
                    var recs = s.G.CameraRecords(); bool said = q.Recorded != "no", did = recs.Count > 0;
                    int said0 = t0 + T(q.RecordInS); bool when = !did || (recs[0].landAt >= said0 - T(1) && recs[0].landAt <= said0 + T(s.G.R.CamScanS) + T(q.WalkS) + T(2)); // the card cannot see the crowd: a later first watched tick lands up to a scan later
                    bool alert = !did || s.G.Alert == q.RecordAlert;
                    if (said == did && when && alert) honest++; else if (bad.Count < 5) bad.Add(Iv($"seed {seed}: card {Mark(q)}, did {did} {(did ? Report.Clock(recs[0].landAt) : "")} A{s.G.Alert}"));
                    if (did) recorded++;
                }
                r.Eq(honest, 200, Iv($"the card was right on every seed ({recorded} recorded){(bad.Count > 0 ? ": " + string.Join("; ", bad) : "")}"));
            });

            Add("CAM-9", "096", "Motive CAM-9: a run with a record replays byte-identical from its seed, start and orders (the record's tick, its landing and its Alert).", r =>
            {
                var g = r.G; ListenDriver(r); r.Do(Fixer, "turn", Driver, "pay"); r.Done(Fixer); r.Wait(T(g.R.CamScanS + g.R.CamCallS + 1));
                r.Is(g.CameraRecords().Exists(x => x.state == "landed"), "a record landed");
                var again = Engine.Replay(r.Inputs!, g);
                r.Is(again.CameraRecords().SequenceEqual(g.CameraRecords()) && again.Trail == g.Trail && again.Alert == g.Alert, "the replay's records, Trail and Alert are the run's (the suite checks its checkpoints)");
            }, new Start { Seed = 9 });
        }
    }
}
