// Brief 096, engine 0.40.0 (Keel): the parked car noticed (rulebook attention.parked; Motive's rules page, tests PARK-1..8; Sky 2026-11-04 "all.
// recommended"). The service-drive stand is private ground (a drive); the square's bay is public. Marks are read from the rulebook's bands.
//  PARK-1 reported-in-band  200 seeds, Heat 0, our car on the service drive, no Reroute: the patrol reports it 45-55 s after it parks; Alert 1; no Trail
//  PARK-2 reroute-covers    the Reroute lands first: nothing while the patrol is away; reported 45-55 s after it is back
//  PARK-3 wary-holds        seen past the wary mark, then rerouted: when the patrol is back the clock starts at the wary mark, reported within 25 s
//  PARK-4 public-bay        our car at the square to the close: no clock, no report
//  PARK-5 aboard-no-clock   agents in the parked car keep no clocks; out beside it, theirs and the car's both run
//  PARK-6 heat-scales       Heat 10: reported 18-25 s after it parks
//  PARK-7 once-per-watcher  one report from the patrol per car per mission, however long the car stays
//  PARK-8 drive-card        200 seeds: the Drive card's eye (the patrol) and marks match what happens
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static int ParkedAt(Game g) => g.Log.FindLast(l => l.Code == "car.parked").Tick;
        static int Made(Game g, string w = "patrol") => g.ParkedReports().Find(x => x.watcher == w).from is var f && g.ParkedReports().Exists(x => x.watcher == w) ? f : -1;
        /// <summary>The Tech walks to the car, boards and drives it to the service drive; the run waits there until the patrol reports it (or `limitS`).</summary>
        static (int parked, int made) ParkAndWait(Run r, int limitS = 120)
        {
            var g = r.G; r.To(DriveAtStart(r)); int parked = ParkedAt(g);
            r.Until(() => Made(g) >= 0 || g.Over, T(limitS));
            return (parked, Made(g));
        }

        static void Parked096()
        {
            Add("PARK-1", "096", "Motive PARK-1, 200 seeds: Heat 0, our car on the service drive (private ground: a drive), no Reroute: the patrol's sweep sees it most of its round and reports it 45-55 s after it parks, by seed (the sweep's phase); the report is a found fault: Alert 1, no Trail.", r =>
            {
                var g = r.G; r.Wait(1); int inBand = 0, alert = 0, trail = 0; var bad = new List<string>(); var lat = new List<int>();
                for (int seed = 1; seed <= 200; seed++)
                {
                    var s = new Run(Engine.New(r.Inputs!, new Start { Seed = seed })) { Inputs = r.Inputs };
                    var (parked, made) = ParkAndWait(s); s.Until(() => s.G.ParkedReports().Exists(x => x.state != "pending") || s.G.Over, T(30));
                    int d = made - parked; lat.Add(d);
                    if (made >= 0 && d >= T(s.G.AttentionMarks.report) - 1 && d <= T(55)) inBand++; else if (bad.Count < 5) bad.Add(Iv($"seed {seed}: {(made < 0 ? "none" : (d / 10.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s")}"));
                    if (s.G.Alert >= 1 && s.G.Ledger.Exists(x => x.what == "alert" && x.cause == "car-reported:patrol")) alert++;
                    if (!s.G.Ledger.Exists(x => x.what == "trail" && x.cause.StartsWith("car-reported", StringComparison.Ordinal))) trail++;
                }
                lat.Sort();
                r.Is(lat[0] >= T(g.AttentionMarks.report) - 1 && lat[199] <= T(90), Iv($"reported on every seed, never before the report mark ({g.AttentionMarks.report} s of sight) nor past 90 s"));
                r.Line(inBand == 200, Iv($"Motive's band: reported 45-55 s after parking on every seed: {inBand}/200 (median {lat[100] / 10.0:0.0} s, {lat[0] / 10.0:0.0}-{lat[199] / 10.0:0.0}){(bad.Count > 0 ? ": " + string.Join("; ", bad) : "")}"));
                r.Eq(alert, 200, "Alert 1 from the report (a found fault) on every seed");
                r.Eq(trail, 200, "no Trail from it");
            });

            Add("PARK-2", "096", "Motive PARK-2: the Reroute lands before our car parks on the service drive: no clock moves while the patrol is away; once it is back, the patrol reports the car 45-55 s later.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet", true), r.SendTo(Scout, g.GetawaySeat)));
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                if (!r.Is(g.PatrolAwayUntil > g.Tick, Iv($"the patrol is away until {Report.Clock(g.PatrolAwayUntil)}"))) return;
                r.Do(Scout, "board", "car"); r.Done(Scout); r.Do(Scout, "drive-car", "service-drive"); r.Done(Scout);
                int back = g.PatrolAwayUntil; r.To(back - 1);
                r.Is(g.ParkedTicks("patrol") == 0 && !g.ParkedReports().Exists(x => x.watcher == "patrol"), "the patrol keeps no clock on the car while it is away" + string.Concat(g.Log.Where(l => l.Code == "car.noticed").Select(l => Iv($" [{Report.Clock(l.Tick)} {l.Text}]"))));
                r.Until(() => Made(g) >= 0 || g.Over, T(120));
                int d = Made(g) - back;
                r.Is(d >= T(g.AttentionMarks.report) - 1 && d <= T(90), Iv($"back at {Report.Clock(back)}: reported {d / 10.0:0.0} s later, no sooner than the report mark"));
                r.Line(d <= T(55), Iv($"Motive's band: within 55 s of its return ({d / 10.0:0.0} s)"));
            });

            Add("PARK-3", "096", "Motive PARK-3: seen past the wary mark, then the Reroute lands: the patrol stays wary; back from its false orders, its clock starts at the wary mark and it reports the car within (report - wary) s.", r =>
            {
                var g = r.G; var marks = g.AttentionMarks;
                r.To(Max(r.Send(Tech, "cabinet", true), r.SendTo(Scout, g.GetawaySeat)));
                r.Do(Scout, "board", "car"); r.Done(Scout); r.Do(Scout, "drive-car", "service-drive"); int parkedAt = r.Op(Scout).BusyEnd;
                r.To(Math.Max(g.Tick, parkedAt + T(32) - T(g.R.RerouteS))); // the Reroute lands 32 s after the car parks: past the wary mark, before the report
                r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                r.Is(g.PatrolAwayUntil > g.Tick && Made(g) < 0 && g.ParkedWary("patrol"), Iv($"rerouted at {Report.Clock(g.Tick)}: wary ({g.ParkedTicks("patrol") / 10.0:0.0} s), not yet reported"));
                int back = g.PatrolAwayUntil; r.To(back - 1);
                r.Eq(g.ParkedTicks("patrol"), T(marks.wary), "while it was away its clock fell to the wary mark and held there");
                r.Until(() => Made(g) >= 0 || g.Over, T(90));
                int d = Made(g) - back;
                r.Is(d >= T(marks.report - marks.wary) - 2 && d <= T(90), Iv($"back at {Report.Clock(back)}: reported {d / 10.0:0.0} s later, after {marks.report - marks.wary} s more of sight, not {marks.report} (it stayed wary)"));
                r.Line(d <= T(marks.report - marks.wary) + T(1), Iv($"Motive's PARK-3: within {marks.report - marks.wary} s of its return ({d / 10.0:0.0} s: the sweep's blind stretches drain the clock to the wary mark between passes)"));
            });

            Add("PARK-4", "096", "Motive PARK-4: our car stands at the square's bay (public) to the close: no clock, no report, though the square's watchers see it.", r =>
            {
                var g = r.G; r.To(T(g.ClockOver));
                r.Is(!g.CarParkedPrivate && g.ParkedReports().Count == 0 && !g.Log.Exists(l => l.Code == "car.noticed") && g.WatcherIds().All(w => g.ParkedTicks(w) == 0), "no clock and no report at a public bay");
            });

            Add("PARK-5", "096", "Motive PARK-5: the Tech in the parked car keeps no clock of his own while the car's runs; stepped out beside it, his and the car's both run.", r =>
            {
                var g = r.G; r.To(DriveAtStart(r)); r.Wait(T(15));
                r.Is(g.ParkedTicks("patrol") > 0 && g.AttentionTicks("patrol", Tech) == 0, Iv($"aboard: the car's clock {g.ParkedTicks("patrol") / 10.0:0.0} s, the Tech's 0"));
                var beside = g.S.Grid.NearestFree(g.GetawayCarPos() + new Pt(-300, -250))!.Value;
                r.To(r.SendTo(Tech, beside)); r.Until(() => r.Op(Tech).Walk == null && !r.Op(Tech).InCar && g.PatrolWatching(r.Op(Tech).Pos), T(60)); int car0 = g.ParkedTicks("patrol"); r.Wait(T(3));
                r.Is(g.AttentionTicks("patrol", Tech) > 0 && g.ParkedTicks("patrol") > car0, Iv($"out beside it: the Tech's clock {g.AttentionTicks("patrol", Tech) / 10.0:0.0} s and the car's both run"));
            });

            Add("PARK-6", "096", "Motive PARK-6: at Heat 10 the marks are the band's (3 s, 12 s, 18 s): the patrol reports our car on the service drive 18-25 s after it parks.", r =>
            {
                var g = r.G; var (parked, made) = ParkAndWait(r, 60); int d = made - parked;
                r.Eq(g.AttentionMarks, g.R.AttnBand(10) is var b ? (b.noticeS, b.waryS, b.reportS) : default, "Heat 10's marks");
                r.Is(made >= 0 && d >= T(18) - 1 && d <= T(25), Iv($"reported {d / 10.0:0.0} s after it parks"));
            }, new Start { Heat = 10 });

            Add("PARK-7", "096", "Motive PARK-7: one report from the patrol per car per mission: the car stays on the drive three minutes past its report; still one.", r =>
            {
                var g = r.G; var (parked, made) = ParkAndWait(r); r.Wait(T(180));
                r.Is(made >= 0 && g.ParkedReports().Count(x => x.watcher == "patrol") == 1 && g.Log.Count(l => l.Code == "car.reported") == g.ParkedReports().Count, Iv($"one report from the patrol ({g.ParkedReports().Count} in all)"));
            });

            Add("PARK-8", "096", "Motive PARK-8, 200 seeds: the Drive card to the service drive says private ground, names the patrol (or \"?\") and the band's marks before the Drive; on every seed the patrol then notices the car. The square's card says public ground.", r =>
            {
                var g = r.G; r.Wait(1); int match = 0; var bad = new List<string>();
                var sq = g.ParkedQuote("square"); r.Is(!sq.priv && sq.seenBy.Count == 0 && sq.maybe.Count == 0, "the square's card: public ground, nobody keeps a clock");
                for (int seed = 1; seed <= 200; seed++)
                {
                    var s = new Run(Engine.New(r.Inputs!, new Start { Seed = seed })) { Inputs = r.Inputs };
                    var q = s.G.ParkedQuote("service-drive"); var m = s.G.AttentionMarks;
                    bool said = q.priv && (q.seenBy.Contains("patrol") || q.maybe.Contains("patrol")) && (q.noticeS, q.waryS, q.reportS) == (m.notice, m.wary, m.report);
                    s.To(DriveAtStart(s)); s.Until(() => s.G.Log.Exists(l => l.Code == "car.noticed") || s.G.Over, T(60));
                    bool did = s.G.Log.Exists(l => l.Code == "car.noticed" && l.Text.Contains("patrol"));
                    if (said && did) match++; else if (bad.Count < 4) bad.Add(Iv($"seed {seed}: card {q.priv} [{string.Join(",", q.seenBy)}|{string.Join(",", q.maybe)}] noticed {did}"));
                }
                r.Eq(match, 200, Iv($"the card matched on every seed{(bad.Count > 0 ? ": " + string.Join("; ", bad) : "")}"));
            });
        }
    }
}
