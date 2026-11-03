// Engine 0.31.0 (brief 058 §2 and §6, Motive 2026-10-24; Sky 2026-10-25: "Your recommendation seem good": a loop runs 2:00, Disable is an
// incident). Two of the three ways to deal with a camera, both the Tech's at the cabinet (the third, Shoot, is GameShootAnyone.cs):
//  · Loop v2 (rulebook cell.loop.run): the loop lands after cell.loop.s and plays on its own for cell.loop.run from the landing, wherever
//    the Tech goes, then the live feed returns (loop.ends). One loop at a time (loop-holds, with the tick it ends); re-looping the camera
//    already looping re-splices it: charged again, a fresh run from the new landing. With the tether in the rulebook instead (a run pinned
//    before 0.31.0) the loop holds while the Tech stays at the cabinet, as before (Tether()).
//  · Disable (rulebook cell.disable): the camera is cut for the rest of the mission (CameraDark, recorded as cut, not shot). Its report is
//    a missed check-in by the camera: it lands at the desk's next scan of that screen plus witness.postCall, a reported incident
//    (alert.byIncident), and the dial dispatches its response to the camera. It holds no scene, has no face, no Standing, no witness.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>A cut camera's missed check-in on its way to the desk (brief 058 §2): it lands at LandAt.</summary>
    public sealed class CutRep { public string Cam = ""; public int From, LandAt; public bool Done; }

    public sealed partial class Game
    {
        /// <summary>Loop v2: the tick the running loop ends (the feed returns at it); INF when no loop runs or the rulebook has the tether.</summary>
        public int LoopUntil = INF;
        /// <summary>Cameras the Tech has cut (brief 058 §2), by id: dead for the rest of the mission.</summary>
        public readonly SortedSet<string> CutCams = new SortedSet<string>(StringComparer.Ordinal);
        /// <summary>The cut cameras' missed check-ins, in the order cut (read-only for the host: When a cut is noticed).</summary>
        public readonly List<CutRep> CutReps = new List<CutRep>();

        /// <summary>The Tech is tied to the cabinet by the loop: only under the tether (a rulebook pinned before 0.31.0). Loop v2 ties nobody.</summary>
        bool Tethered => LoopOn && !R.LoopRuns;
        /// <summary>A camera the Tech cut (brief 058 §2): dead for the rest of the mission, as a shot-out one is (CameraDark).</summary>
        public bool CameraCut(string camId) => CutCams.Contains(camId);

        /// <summary>The loop's end each tick: Loop v2 runs out at LoopUntil; the tether ends it when the Tech is down or steps off the cabinet.</summary>
        void Tether()
        {
            if (R.LoopRuns)
            {
                if (LoopOn && Tick >= LoopUntil) { LoopOn = false; LoopUntil = INF; Note("loop.ends", $"The {LoopCam} loop runs out. The camera feed returns."); }
                if (CutReps.Count > 0) CutReports();
                return;
            }
            var tech = OpOf(OpId.Tech);
            if (LoopOn && (tech.Down || !G.Within(tech.Pos, S.At("cabinet"), R.TetherCm)))
            {
                LoopOn = false; Note("loop.ends", tech.Down ? "The Tech is down. The camera feed returns." : "The Tech leaves the cabinet. The camera feed returns.");
            }
        }

        /// <summary>The Loop order (rulebook cell.loop): the Tech, at the cabinet, on a camera of loop.targets. Loop v2: one loop at a time
        /// (another camera while one runs: loop-holds, with the tick it ends), the camera already looping is re-spliced, a dead camera is
        /// refused camera-dead. Tether: refused while the loop holds.</summary>
        string Loop(Op op, Order o)
        {
            if (op.Id != OpId.Tech) return No("only-tech", "only the Tech loops cameras (§03)");
            if (!LoopTarget(o.Target)) return No("loop-wrong-target", "the Tech's cabinet loops the entrance camera, the pickup camera and the city's own cameras (the Records Square mast)");
            if (R.LoopRuns)
            {
                if (CameraDark(o.Target)) return No("camera-dead", $"the {o.Target} camera is dead already: there is no feed to loop");
                if (LoopOn && o.Target != LoopCam) return No("loop-holds", $"one loop at a time: the {LoopCam} loop runs until {Sim.Report.Clock(LoopUntil)}", tick: LoopUntil);
            }
            else if (LoopOn) return No("loop-holds", "the loop already holds");
            if (!G.Within(op.Pos, S.At("cabinet"), R.AtReachCm)) return No("stand-at-cabinet", "stand at the cabinet", cm: R.AtReachCm);
            Engage(op, o, R.LoopS); return "";
        }

        /// <summary>A loop lands: Loop v2 runs loop.run from now (a re-splice of the same camera restarts it and charges again); under the
        /// tether it holds while the Tech stays.</summary>
        void LandLoop(Order o)
        {
            if (R.LoopRuns && CameraDark(o.Target)) { Note("loop.dead", $"The {o.Target} camera went dead while the loop was spliced: nothing to loop."); return; } // shot or cut mid-splice: nothing lands, nothing charged
            bool again = R.LoopRuns && LoopOn && LoopCam == o.Target;
            LoopOn = true; LoopCam = o.Target; AddTrail(R.LoopTrail, "loop"); Actions.Add(o.Target == "entrance" ? "loop" : "loop:" + o.Target);
            if (!R.LoopRuns) { Note("loop.on", "One feed repeats. The Tech is tied to the cabinet."); return; }
            LoopUntil = Tick + T(R.LoopRunS);
            Note("loop.on", again ? $"The Tech re-splices the {o.Target} loop: it runs until {Sim.Report.Clock(LoopUntil)}." : $"One feed repeats until {Sim.Report.Clock(LoopUntil)}. The Tech is free to go.");
        }

        /// <summary>The Disable order (rulebook cell.disable): the Tech, at the cabinet, on a camera the cabinet loops that is not dead.</summary>
        string Disable(Op op, Order o)
        {
            if (!R.DisableOn) return No("unknown-verb", "verb 'disable' is unknown to this rulebook (cell.disable: engine 0.31.0; unknown = reject)");
            if (op.Id != OpId.Tech) return No("only-tech", "only the Tech cuts a camera's line (§03)");
            if (!LoopTarget(o.Target)) return No("disable-wrong-target", "the Tech's cabinet cuts the cameras it loops: the entrance camera, the pickup camera and the city's own cameras (the Records Square mast)");
            if (CameraDark(o.Target)) return No("camera-dead", $"the {o.Target} camera is dead already");
            if (!G.Within(op.Pos, S.At("cabinet"), R.AtReachCm)) return No("stand-at-cabinet", "stand at the cabinet", cm: R.AtReachCm);
            Engage(op, o, R.DisableS); return "";
        }

        /// <summary>How a camera died (brief 058 §6, Glyph's dead disc): "cut" (Disable), "shot" (shot out), "" while it lives. Read-only.</summary>
        public string CameraDeadAs(string camId) => CutCams.Contains(camId) ? "cut" : CameraDark(camId) ? "shot" : "";
        /// <summary>The Alert a Disable's report would raise if it landed next (the order line's siren: alert.byIncident for the next reported
        /// incident, counting cut reports still on their way, and at least the missed mark's witness.alert). Read-only.</summary>
        public int AlertIfCut()
        {
            int pending = 0; foreach (var c in CutReps) if (!c.Done) pending++;
            var list = R.AlertByIncident;
            return Math.Max(Alert, Math.Max(R.AlertMark, list[Math.Min(Reported + pending + 1, list.Length) - 1]));
        }

        /// <summary>The desk's next scan of a camera's screen from `tick` (witness.cameraScan, phase from the camera's id), as a live camera's
        /// sighting is read (WatcherNotices).</summary>
        int NextScan(string watcher, int tick)
        {
            int period = T(R.CameraScanS), phase = ScanPhase(watcher, period);
            return tick + ((phase - tick % period) % period + period) % period;
        }

        /// <summary>When a cut of this camera now would be noticed at the desk (the order line's "▲1 in n s"): the landing tick of a cut that
        /// lands at `landAt` (default: now). Read-only.</summary>
        public int CutNoticedAt(string camId, int landAt = -1) => NextScan("camera:" + camId, landAt < 0 ? Tick : landAt) + T(R.PostCallS);

        /// <summary>A Disable lands: the camera is dead (cut), a loop on it ends, +disable.trail, and its missed check-in starts for the desk.</summary>
        void LandDisable(Order o)
        {
            if (CameraDark(o.Target)) { Note("disable.dead", $"The {o.Target} camera was already dead."); return; } // shot mid-cut: nothing more to cut, nothing charged
            CutCams.Add(o.Target);
            AddTrail(R.DisableTrail, "disable:" + o.Target); Actions.Add("disable:" + o.Target); Chain.Add($"camera-cut:{o.Target}");
            if (LoopOn && LoopCam == o.Target) { LoopOn = false; LoopUntil = INF; Note("loop.ends", $"The {o.Target} loop ends: the line is cut."); }
            int land = CutNoticedAt(o.Target);
            CutReps.Add(new CutRep { Cam = o.Target, From = Tick, LandAt = land });
            Note("camera.cut", $"The Tech cuts the {o.Target} camera's line. The desk will see a dead screen at {Sim.Report.Clock(land - T(R.PostCallS))}.");
        }

        /// <summary>The cut cameras' missed check-ins that land now, in the order cut: a reported incident (alert.byIncident, at least the
        /// missed mark's witness.alert), and the dial's dispatch to the camera's place (an alarm with no body: no scene held, no door).</summary>
        void CutReports()
        {
            foreach (var r in CutReps)
            {
                if (r.Done || r.LandAt > Tick) continue;
                r.Done = true;
                var cam = S.Cameras.Find(c => c.Id == r.Cam); var at = cam != null ? (Walk.NearestFree(cam.At) ?? cam.At) : S.Stand(S.GuardPost);
                Reported++;
                var list = R.AlertByIncident;
                int to = Math.Max(R.AlertMark, list[Math.Min(Reported, list.Length) - 1]);
                string cause = "report:cut:camera:" + r.Cam;
                landingBy = "desk"; landingAt = at;
                RaiseAlert(to, cause);
                landingBy = "";
                AddIncident("report", cause, at);
                Note("camera.cut-reported", $"The desk sees the {r.Cam} camera's screen dead: a missed check-in. Alert {Alert}.");
                Alarm(null, at, null, "");
                if (Over) return;
            }
        }

        /// <summary>Engine 0.31.0: no bytes until a loop runs under Loop v2 or a camera is cut, so every run that meets neither hashes as before.</summary>
        internal void HashCameraLines(Hash.H h)
        {
            if (R.LoopRuns && LoopOn) h.S("loopUntil").I(LoopUntil);
            if (CutCams.Count == 0) return;
            h.S("cut"); foreach (var c in CutCams) h.S(c);
            foreach (var r in CutReps) h.S(r.Cam).I(r.LandAt).Z(r.Done);
        }
    }
}
