// Brief 101 (Sky 2026-11-09, pick B; engine 0.52.0, Keel): the square's mast is the terrace's west lamp, beside the cell's start (Vantage,
// briefs/101/vantage/lamp-mast-built.md; sim/data/handover.takedown.json). What it holds:
//    N67          the mast's facts: three heads fanned 60° (compass ESE, ENE, N), 70° and 30 m each, its back to the kerb; it sees the café front,
//                 the terrace and the fountain plaza, never the cell's start nor the car; watched, looped and one loop at a time like any camera
//    LoopMast     the helper for the scenarios of other rules that play under it: the Tech loops it first, as a player would (Motive's counter,
//                 briefs/101/motive/mast-pressure.md §1)
//    CAM-10       Motive's missing proof (§2): a rival posted under the mast is paid; the Pay card shows the tape before the commit (CrewQuote, engine
//                 0.52.0), the record lands, the dial's robots go to his post
//    CAM-11       one act, one incident (engine 0.52.0): a taped Pressure its subject reports is one incident (Alert 1); the next act on camera is the second
//    M101-LOGGED  the exposure law under the mast (Sky 2026-11-09, "Keep it"; N18): it logs the Fixer beside a turned Inez, so her job is exposed
//    M101-LOOPED  its counter: looped across the turn, nobody ties her to the cell, and her job is unexposed
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The square's mast camera (brief 101: the terrace's west lamp, beside the cell's start).</summary>
        const string MastCam = "mast-z0";

        /// <summary>Brief 101: the mast watches the café front, the terrace and the fountain plaza, where many scenarios of other rules play (a Pay, a
        /// draw, a Listen's spot, a rival's post). They loop it first, as a player would (rulebook cell.loop: the Tech at the cabinet, +1 T), so they
        /// test the rule they were written for. Returns when the loop runs: loop.run from then (2:00), about 0:45 to 2:45 from the start.</summary>
        static void LoopMast(Run r, bool run = false)
        {
            r.To(r.Send(Tech, "cabinet", run)); r.Do(Tech, "loop", MastCam); r.Done(Tech);
            r.Is(r.G.LoopOn && r.G.LoopCam == MastCam, $"the Tech loops the square's mast (brief 101: it watches the café front) until {Report.Clock(r.G.LoopUntil)}");
        }

        /// <summary>Free points on a lattice within `cm` of a place, the cell's loose stance round a mark (GDD §07: ± 2 m).</summary>
        static IEnumerable<Pt> Round(Run r, Pt at, int cm, int step)
        {
            for (int dx = -cm; dx <= cm; dx += step) for (int dy = -cm; dy <= cm; dy += step) { var q = new Pt(at.X + dx, at.Y + dy); if (G.Within(q, at, cm) && r.G.S.Grid.Free(q)) yield return q; }
        }

        static void Mast101()
        {
            Add("N67", "—", "The Records Square mast (brief 101, Sky's pick B: the terrace's west lamp, beside the cell's start; sim/data/handover.takedown.json): three heads fanned 60° apart (compass ESE, ENE and N), 70° and 30 m each, its back to the kerb. Each head sees its side of the square: the fountain, the café front, the west side. No head sees the cell's start (each mark ± 2 m: Vantage's proof, 18.7° to spare), the getaway bay or the Office side (beyond 30 m). The Scout learns its sight with a Watch like any camera; the Tech's cabinet loops it like the entrance camera (one loop at a time), and looped it sees nothing.", r =>
            {
                var g = r.G; const string Mast = "camera:" + MastCam;
                var cam = g.S.Cameras.Find(c => c.Id == MastCam);
                if (!r.Is(g.WatcherIds().Contains(Mast) && cam != null && cam.World && cam.Facings.Length == 3, "the mast is a watcher, three heads")) return;
                r.Eq(cam!.At, new Pt(-385, -4255), "on the terrace's west lamp (block −3.85, −42.55 m)");
                foreach (var (head, place) in new[] { (0, "d:Fountain"), (1, "d:Courtyard"), (2, "d:West side") })
                {
                    var p = r.Stand(place);
                    r.Is(G.InCone(cam.At, cam.Facings[head], cam.Reach, cam.Half, 0, p) && g.CameraWatching(MastCam, p, false), $"head {head} sees {place}");
                }
                r.Is(g.CameraWatching(MastCam, r.Stand("d:Café door"), false) && g.CameraWatching(MastCam, r.Stand("beside-inez"), false), "the café front: Inez's post and the Scout's spot beside it");
                foreach (var mark in new[] { "crew:scout", "crew:fixer", "crew:tech", "crew:enforcer", "d:Getaway bay" })
                {
                    var seen = Round(r, r.Stand(mark), 200, 10).FirstOrDefault(q => g.CameraWatching(MastCam, q, false));
                    r.Is(seen == default, $"not {mark}, nor anywhere within 2 m of it{(seen == default ? "" : $" (seen at {seen})")}");
                }
                r.Is(!g.CameraWatching(MastCam, r.Stand("d:Office entrance"), false), "nor the Office side (beyond 30 m)");
                var spot = WatchSpot(r, Mast);
                r.To(r.SendTo(Scout, spot)); r.Do(Scout, "watch", Mast); r.Done(Scout);
                r.Is(g.Known(Mast)!.Sight && g.ConeFan(Mast)!.More.Length == 2, "watched: its sight is known, a fan for each head");
                r.To(r.Send(Tech, "cabinet"));
                r.Do(Tech, "loop", MastCam); r.Done(Tech);
                r.Is(g.LoopOn && g.LoopCam == MastCam && !g.CameraWatching(MastCam, r.Stand("d:Courtyard"), false), "looped: it sees nothing");
                r.Is(g.CameraWatching("entrance", r.Stand("d:Office entrance"), false), "the entrance camera stays live");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "loop", Target = "entrance" }).code, "loop-holds", "one loop at a time");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "loop", Target = "pickup" }).code, "loop-holds", "the pickup camera is on the cabinet too (brief 013 D), but one loop at a time");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "loop", Target = "gate" }).code, "loop-wrong-target", "no camera of that name (unknown = reject)");
            });

            Add("CAM-10", "101", "Motive's missing proof (briefs/101/motive/mast-pressure.md §2): a member of the rival crew posted under the square's mast (rival-3 at ambush-2 on this seed; a Mercenary by the start, test only) is paid there. Before the commit the Pay card shows the tape, as a person's does: the mast's eye ('yes': the Scout watched it), +1 T on the Pay's own and ▲1 in n s (CrewQuote, engine 0.52.0; it showed nothing before). The mast records it; the record lands at its desk's next scan + 3 s: the Pay's Trail + 1, Alert 1, one incident, and the dial's robots go to his post.", r =>
            {
                var g = r.G; var m = g.CrewOf("rival-3")!;
                r.Do(Scout, "watch", "camera:" + MastCam); r.Done(Scout); // from her start: the mast stands 3 m away
                r.Until(() => m.Mode == CrewMode.Posted || g.Over, T(260));
                var post = g.MemberPose(m)!.Value.pos;
                if (!r.Is(g.CameraWatching(MastCam, post, false), $"rival-3's post is in the mast's view ({post})")) return;
                r.To(r.SendTo(Fixer, g.S.Grid.NearestFree(post + new Pt(0, 150)) ?? post)); r.Do(Fixer, "watch", "rival-3"); r.Done(Fixer); // rivals.turn.needs: a Watch first
                var q = g.CrewQuote(new Order { Op = Fixer, Do = "turn", Target = "rival-3", Approach = "pay" }, m);
                r.Is(q.Recorded == "yes" && q.RecordedBy == MastCam && q.RecordTrail == g.R.CamTrail && q.RecordAlert == g.R.CamAlert && q.RecordInS > 0, Iv($"the Pay card's tape before the commit ({Mark(q)})"));
                int t0 = g.Trail, inc0 = g.Incidents.Count, start = g.Tick;
                r.Do(Fixer, "turn", "rival-3", "pay"); r.Done(Fixer);
                r.Is(g.CrewPaid("rival-3"), "he takes it and walks off");
                r.Until(() => g.CameraRecords().Exists(x => x.subject == "rival-3" && x.state == "landed") || g.Over, T(30));
                var rec = g.CameraRecords().Find(x => x.subject == "rival-3");
                if (!r.Is(rec.cam == MastCam && rec.approach == "pay" && rec.state == "landed", Iv($"the mast's record of the Pay on rival-3 lands ({rec.cam} {rec.state})"))) return;
                r.Is(Math.Abs(rec.landAt - (start + T(q.RecordInS))) <= T(1), Iv($"when the card said: {q.RecordInS} s after the order ({Report.Clock(rec.landAt)})"));
                r.Eq(g.Trail, t0 + (int)q.Trail + g.R.CamTrail, Iv($"Trail: the Pay's own ({q.Trail}) + the tape's {g.R.CamTrail}"));
                r.Eq(g.Alert, g.R.CamAlert, "Alert 1: a found fault" + AlertWhy(g));
                r.Eq(g.Incidents.Skip(inc0).Count(i => i.Kind == "recorded"), 1, "one incident, the record's");
                r.Is(g.AlarmDestinations.Count > 0 && G.Within(g.AlarmDestinations[0], post, g.R.WitnessRangeCm) && g.Units.Exists(u => u.Robot && G.Within(u.Dest, post, g.R.WitnessRangeCm)), "the dial's robots go to his post");
            }, Temper(new Start(), "rival-3", "mercenary"));

            Add("CAM-11", "101", "One act, one incident (engine 0.52.0; rulebook alert and camera; Keel's hold: one incident counts once toward Alert and dispatches once): a Pressure the square's mast tapes, on an Inez who reports it (Loyal by the start, test only; her secret unread, so it is no exposure and she reports). Before the commit the card reads ▲1 for the tape and for her report. The record lands first: Alert 1, the dial's robots. Her report at the act's end is the same incident: Alert stays 1 and nothing more is sent. The next act on camera, a Pay on the driver under the pickup camera, is the second incident: Alert 2. Before 0.52.0 the Pressure alone made Alert 2.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard"));
                var q = g.Preview("pressure", Inez);
                r.Is(q.Recorded != "no" && q.RecordedBy == MastCam && q.RecordAlert == 1 && q.Reports == "yes" && q.AlertIfReported == 1, Iv($"the card: the mast would tape it and she would report it, ▲1 each ({Mark(q)}; reports {q.Reports} ▲{q.AlertIfReported})"));
                int start = g.Tick;
                r.Do(Fixer, "turn", Inez, "pressure"); r.Done(Fixer);
                r.Is(g.CameraRecords().Exists(x => x.cam == MastCam && x.subject == Inez && x.state == "landed") && g.Log.Exists(l => l.Code == "report" && l.Tick >= start), "the mast's record landed and she reported the approach");
                r.Eq((g.Alert, g.Reported), (1, 1), "one act, one incident: Alert 1" + AlertWhy(g));
                r.Eq(g.Log.Count(l => l.Code == "alarm.dispatches" && l.Tick >= start), 1, "the desk sent its robots once");
                r.To(r.Send(Fixer, "d:Boarding pavilion")); r.Do(Fixer, "turn", Driver, "pay"); r.Done(Fixer); // the start state knows him: no Listen
                r.Until(() => g.CameraRecords().Exists(x => x.cam == "pickup" && x.state == "landed") || g.Over, T(30));
                r.Eq((g.Alert, g.Reported), (2, 2), "the Pay the pickup camera tapes is the second incident: Alert 2" + AlertWhy(g));
            }, Reads("loyal"));

            static void ExposureRun(Run r, bool looped)
            {
                var g = r.G;
                if (looped) LoopMast(r);
                r.To(r.Send(Fixer, "d:Courtyard")); HelpUntilTurned(r, Inez); r.Do(Fixer, "call", Inez);
                r.SendTo(Fixer, r.Stand("crew:fixer")); r.To(Math.Max(g.Tick + T(30), g.LoopUntil + T(5))); // he walks off before a loop runs out
                r.Is(g.Inez.Turned && g.Inez.Has("job"), "Inez helped and called: her job is done");
            }
            Add("M101-LOGGED", "101", "The exposure law under the square's mast (rulebook exposure; Motive mast-pressure.md §4; Sky 2026-11-09, \"Keep it\"): the Fixer helps and calls Inez at her post, which the mast watches. Once she is turned it logs him beside her (within approach reach), which ties her to the cell: between missions her job is exposed (resentment), whether or not an investigator reaches her.", r =>
            {
                var g = r.G; ExposureRun(r, looped: false);
                r.Is(g.ExposedByWatcher.Contains(Inez) && g.Sightings.Exists(x => x.Watcher == "camera:" + MastCam && x.Op == Fixer), "the mast logged the Fixer beside her: exposed");
                var next = g.BetweenMissions(g.Investigate());
                r.Is(next.Lines.Exists(l => l.Line == "ladder" && l.Who == Inez && l.Detail.StartsWith("job-exposed", StringComparison.Ordinal)), "her ladder: job-exposed");
            }, Reads());
            Add("M101-LOOPED", "101", "Its counter (Motive mast-pressure.md §4): the Tech loops the square's mast first, and the Fixer helps and calls Inez under it and walks off before it runs out. No watcher logged an operative beside her, no investigator questions her (nothing leads to her), and between missions her job is unexposed (bravery).", r =>
            {
                var g = r.G; ExposureRun(r, looped: true);
                r.Is(!g.ExposedByWatcher.Contains(Inez), "nothing logged beside her");
                var inv = g.Investigate(); var next = g.BetweenMissions(inv);
                r.Is(!inv.Exposed.Contains(Inez), "no investigator questioned her");
                r.Is(next.Lines.Exists(l => l.Line == "ladder" && l.Who == Inez && l.Detail.StartsWith("job-unexposed", StringComparison.Ordinal)), "her ladder: job-unexposed");
            }, Reads());
        }
    }
}
