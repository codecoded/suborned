// The parked car noticed (brief 096, engine 0.40.0, Keel; rulebook attention.parked; Motive's rules page; Sky 2026-11-04 "all. recommended").
// The getaway car standing on private ground (a stand whose car is on a drive, or in a bay whose use is official or service: Site.Ground.Private,
// worked out at load) is a loiterer: every watcher that keeps attention clocks (AttnWatcher: the live cameras, the door guard unless he is the
// cell's ally, the patrol robot, the response's officers and robots) keeps one clock for the car, whether anyone is aboard or not.
//  · seen: in the watcher's cone (a person also within attention.near, whatever his facing) on a clear line from its eye to attention.parked.sight
//    over the car's centre, no crowd across it;
//  · the clock is attention's: +1 a tick standing there in its sight; out of its sight, driving or off private ground, -1 a tick to 0, or to the
//    wary mark once wary; the Heat band's marks (noticed, wary, report);
//  · the report: once per watcher per mission; lands as attention's (a camera's at its desk's next scan plus the call, a person's or robot's by
//    radio after the call; one downed first drops it): an incident (Alert to at least alert.byIncident[n-1] and attention.alert), no Trail, a lead
//    (the cell's car, the place, the time), the dial's dispatch to the car.
// Operatives aboard keep no clocks of their own (AttentionStep: in the car is off the street). Integers only; watchers in WatcherIds order.
// Hashed only once a clock has moved, so a run whose car never stood on private ground in sight hashes as before.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        sealed class ParkRow { public int C, Noted; public bool Wary, Reported; }
        sealed class ParkRep { public string W = ""; public int From, LandAt; public Pt At; public bool Done, Stopped; }
        readonly SortedDictionary<string, ParkRow> parked = new SortedDictionary<string, ParkRow>(StringComparer.Ordinal);
        readonly List<ParkRep> parkReps = new List<ParkRep>();
        readonly HashSet<string> parkSeen = new HashSet<string>(StringComparer.Ordinal); // derived each tick, for the cues: never hashed

        /// <summary>The getaway car stands now on private ground (attention.parked): not driving, not blown, at a stand the ground calls private.</summary>
        public bool CarParkedPrivate => !Range && getawayPath == null && CurStand != null && !(CarOf("car:getaway") is CarTarget t && t.Blown) && S.GroundOf(R).Private.TryGetValue(CurStand.Id, out var pv) && pv;

        /// <summary>Does this watcher see a car standing at `at` (its cone or sweep, a person's near ring too; a clear line to the car's sight point; no crowd)?</summary>
        bool SeesCar(string w, Shape sh, Pt at)
        {
            bool shape = InShape(sh, at) || (AttnPerson(w) && G.Within(sh.At, at, R.AttnNearCm));
            if (!shape) return false;
            var e = WatcherEye(w, sh.At); int top = FloorAt(at) + R.CarSightCm;
            bool line = e.HasValue ? !Steamed(sh.At, at) && S.Sight3(sh.At, e.Value.z, at, top, e.Value.own) : Sees(sh.At, at);
            if (!line) return false;
            return !(e.HasValue ? CrowdBlocksZ(sh.At, e.Value.z, at, top) : CrowdBlocks(sh.At, at));
        }

        /// <summary>Once a tick (from AttentionStep): each clock-keeping watcher's clock on the cell's car, its stages and its reports.</summary>
        void ParkedCarStep()
        {
            parkSeen.Clear();
            if (Range) return;
            bool priv = CarParkedPrivate;
            if (priv || parked.Count > 0)
            {
                var band = R.AttnBand(Heat); int nT = T(band.noticeS), wT = T(band.waryS), rT = T(band.reportS);
                var at = CurStand.CarAt;
                foreach (var w in WatcherIds())
                {
                    if (!AttnWatcher(w)) continue;
                    var sh = ShapeOf(w); bool live = sh.HasValue && sh.Value.Live;
                    parked.TryGetValue(w, out var row);
                    bool seen = priv && live && SeesCar(w, sh!.Value, at);
                    if (row == null) { if (!seen) continue; parked[w] = row = new ParkRow(); }
                    if (seen) parkSeen.Add(w);
                    int c = seen ? row.C + 1 : Math.Max(row.Wary ? wT : 0, row.C - 1);
                    row.C = c; if (c >= wT) row.Wary = true;
                    int st = c >= rT ? 3 : c >= wT ? 2 : c >= nT ? 1 : 0;
                    string by = w.Replace("camera:", "the camera ");
                    if (st > row.Noted)
                    {
                        if (row.Noted < 1) Note("car.noticed", $"{by} notices our car standing on the {CurStand.Id}.");
                        if (st >= 2 && row.Noted < 2) Note("car.wary", $"{by} is wary of our car for the rest of the mission.");
                        row.Noted = st;
                    }
                    if (c >= rT && !row.Reported)
                    {
                        row.Reported = true;
                        int land = WatcherKind(w) == "camera" ? CameraLandsAttn(w) : Tick + T(R.AttnCallS);
                        parkReps.Add(new ParkRep { W = w, From = Tick, LandAt = land, At = at });
                        Note("car.reported", $"{by} reports our car on the {CurStand.Id} (lands {Sim.Report.Clock(land)}).");
                    }
                }
            }
            foreach (var r in parkReps)
            {
                if (r.Done || r.LandAt > Tick) continue;
                r.Done = true;
                if (AttnReporterDown(r.W)) { r.Stopped = true; Note("car.report-stopped", $"{r.W.Replace("camera:", "the camera ")}'s report of our car never lands."); continue; }
                string cause = $"car-reported:{r.W}";
                Reported++;
                RaiseAlert(Math.Max(R.AttnAlert, R.AlertByIncident[Math.Min(Reported, R.AlertByIncident.Length) - 1]), cause);
                AddIncident("car-reported", cause, r.At);
                Note("car.report-lands", $"{r.W.Replace("camera:", "the camera ")}'s report lands: our car parked where cars don't stand. Alert {Alert}.");
                Alarm(null, r.At, null, "");
                if (Over) return;
            }
        }
        int CameraLandsAttn(string w) { int period = T(R.AttnScanS), phase = ScanPhase(w, period); return Tick + ((phase - Tick % period) % period + period) % period + T(R.AttnCallS); }

        // ---------------------------------------------------------------- the host's reads (the car's stage mark, the Drive card)

        /// <summary>A watcher's stage toward the cell's car now (0 calm, 1 noticed, 2 wary, 3 reporting): calm unless it saw the car on the last tick.</summary>
        public int ParkedStage(string watcher)
        {
            if (!parkSeen.Contains(watcher) || !parked.TryGetValue(watcher, out var row)) return 0;
            foreach (var r in parkReps) if (!r.Done && r.W == watcher) return 3;
            if (row.Wary) return 2;
            return row.C >= T(R.AttnBand(Heat).noticeS) ? 1 : 0;
        }
        /// <summary>The car's stage mark: its highest stage among the watchers that see it now, and that watcher (WatcherIds order on a tie); (0, "") calm.</summary>
        public (int stage, string watcher) ParkedCue() { int best = 0; string who = ""; foreach (var w in WatcherIds()) { int s = ParkedStage(w); if (s > best) { best = s; who = w; } } return (best, who); }
        /// <summary>A watcher's clock on the car, in ticks (0 when it keeps none), and whether it has reported the car this mission.</summary>
        public int ParkedTicks(string watcher) => parked.TryGetValue(watcher, out var row) ? row.C : 0;
        public bool ParkedReported(string watcher) => parked.TryGetValue(watcher, out var row) && row.Reported;
        public bool ParkedWary(string watcher) => parked.TryGetValue(watcher, out var row) && row.Wary;
        /// <summary>The car reports made so far, in order: watcher, when made, when it lands, and its state (pending, landed, stopped).</summary>
        public List<(string watcher, int from, int landAt, string state)> ParkedReports()
        {
            var l = new List<(string, int, int, string)>();
            foreach (var r in parkReps) l.Add((r.W, r.From, r.LandAt, r.Stopped ? "stopped" : r.Done ? "landed" : "pending"));
            return l;
        }

        /// <summary>The Drive card's line for a stand (attention.parked): whether the car would be on private ground there; the watchers that would
        /// see it standing there now or at some tick of their round (the patrol's sweep, unless it is rerouted), known ("yes") or not ("?"); the Heat
        /// band's marks in seconds; the tick a rerouted patrol is back (-1 not rerouted). Read-only.</summary>
        public (bool priv, List<string> seenBy, List<string> maybe, int noticeS, int waryS, int reportS, int patrolBack) ParkedQuote(string standId)
        {
            var seen = new List<string>(); var maybe = new List<string>(); var b = R.AttnBand(Heat);
            var st = S.StandById(standId);
            bool priv = !Range && st != null && S.GroundOf(R).Private.TryGetValue(st.Id, out var pv) && pv;
            if (priv)
                foreach (var w in WatcherIds())
                {
                    if (!AttnWatcher(w)) continue;
                    bool sees = false;
                    if (w == "patrol" && !PatrolDown) { for (int t = 0, n = T(R.PatrolPeriodS); t < n && !sees; t += Hz) { var pos = SweepPos(Tick + t); sees = InSweepCone(pos, S.PatrolFacing, st!.CarAt) && SeesFrom(pos, FloorAt(pos) + R.MountPatrolCm, st.CarAt); } }
                    else if (ShapeOf(w) is Shape sh && sh.Live) sees = SeesCar(w, sh, st!.CarAt);
                    if (!sees) continue;
                    if (Known(w)?.Sight ?? false) seen.Add(w); else maybe.Add(w);
                }
            return (priv, seen, maybe, b.noticeS, b.waryS, b.reportS, PatrolAwayUntil > Tick ? PatrolAwayUntil : -1);
        }

        /// <summary>The car's clocks and reports for the checkpoint hash: no bytes until a clock has moved.</summary>
        internal void HashParked(Hash.H h)
        {
            if (parked.Count == 0 && parkReps.Count == 0) return;
            h.S("parked");
            foreach (var kv in parked) h.S(kv.Key).I(kv.Value.C).Z(kv.Value.Wary).Z(kv.Value.Reported).I(kv.Value.Noted);
            foreach (var r in parkReps) h.S(r.W).I(r.From).I(r.LandAt).P(r.At).Z(r.Done).Z(r.Stopped);
        }
    }
}
