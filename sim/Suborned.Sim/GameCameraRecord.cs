// Bribes on camera (brief 096, engine 0.40.0, Keel; rulebook camera; Motive's rules page Q1 (a), Q2 (a); Sky 2026-11-04 "all. recommended").
//  · A Pay or a Pressure (camera.approaches) is recorded when a live camera watches the Fixer or the person being turned at any tick of the act
//    (CameraWatching: not looped, not cut or shot out; in a head's cone, a clear line from its lens, no crowd across it), from its start in
//    place to its end or its cancel. One act, one record: the first camera (site order) to see it is the record's; the face is on it when a live
//    camera watched the Fixer at any tick of the act.
//  · It lands at that camera's desk's next scan at or after the first watched tick, plus the call (camera.report: as attention's camera reports):
//    +camera.trail Trail, an incident (Alert to at least alert.byIncident[n-1] and camera.alert), the dial's dispatch to the subject's place, a
//    lead (the incident: place, time, the subject; the face). No Standing. On tape: a later loop, cut or shot does not undo it.
//  · A record still to land when the mission ends is charged then: its Trail and its lead, no Alert.
//  · The cards (Preview): QuoteRecord says it before the commit.
//  · One act, one incident (engine 0.52.0; rulebook alert and camera, §05 witness, Keel's hold: one incident counts once toward Alert and
//    dispatches once). The person turned may report the act too (a Loyal person reports an approach: rulebook reports): the record and the
//    report are that one incident, counted toward Alert by whichever lands first; the other raises Alert no higher than that incident's own
//    level (the record still at least camera.alert). Before 0.52.0 a taped Pressure on a Loyal Inez counted twice: Alert 2 (P85-MIXED, brief 101).
// Integers only; records in the order made. Hashed only once a record exists, so a run with none hashes as before.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        sealed class CamRec { public string Cam = "", Subject = "", Approach = ""; public int From, LandAt, Incident; public Pt At; public bool Face, Landed, AtEnd, Witnessed; public Order? Act; } // Incident: the act's number among the reported incidents once counted (0 until)
        readonly List<CamRec> camRecs = new List<CamRec>();
        Order? camAct; CamRec? camCur; bool witnessTrailOff;
        /// <summary>One act, one incident (rulebook camera, alert): a record of this act has landed and charged its Trail, so the act's witnesses add none.</summary>
        bool RecordTrailPaid(Order o) { foreach (var r in camRecs) if (ReferenceEquals(r.Act, o) && r.Landed && !r.Witnessed) return true; return false; }
        /// <summary>The act was seen by people too (witness.sees charged its Trail and made its incident): its record, still to land, charges no Trail and adds no incident.</summary>
        void ActWitnessed(Order o) { foreach (var r in camRecs) if (ReferenceEquals(r.Act, o) && !r.Landed) r.Witnessed = true; }

        /// <summary>The tick a camera's report made now lands: its desk's next scan at or after now (phase from the camera's id), plus the call.</summary>
        int CameraLands(string camId, int from)
        {
            int period = T(R.CamScanS), phase = ScanPhase("camera:" + camId, period);
            return from + ((phase - from % period) % period + period) % period + T(R.CamCallS);
        }
        bool RecordsApproach(Order? o) => o != null && o.Do == "turn" && Array.IndexOf(R.CamApproaches, o.Approach) >= 0;

        /// <summary>Once a tick (from AttentionStep): the Fixer's act in progress watched by a live camera, and the records that land now.</summary>
        void CameraRecordStep()
        {
            var f = OpOf(OpId.Fixer); var o = f.Busy;
            if (!RecordsApproach(o) || f.Down) { camAct = null; camCur = null; }
            else
            {
                if (!ReferenceEquals(o, camAct)) { camAct = o; camCur = null; }
                var p = TurnSubject(o!.Target); var pp = PersonPos(p); // brief 096 (engine 0.41.0): a member of the rival crew paid on camera is recorded too
                foreach (var c in S.Cameras)
                {
                    bool sf = CameraWatching(c.Id, f.Pos), ss = pp.HasValue && CameraWatching(c.Id, pp.Value);
                    if (!sf && !ss) continue;
                    if (camCur == null)
                    {
                        camCur = new CamRec { Act = o, Cam = c.Id, Subject = p.Role.Length > 0 ? p.Role : p.Id, Approach = o.Approach, From = Tick, LandAt = CameraLands(c.Id, Tick), At = pp ?? f.Pos, Face = sf && R.CamFace };
                        camRecs.Add(camCur);
                        Note("camera.recorded", $"The {c.Id} camera records the Fixer's {o.Approach} on {p.Role} (it reaches the desk {Sim.Report.Clock(camCur.LandAt)}).");
                    }
                    else if (sf && R.CamFace) camCur.Face = true;
                }
            }
            foreach (var r in camRecs)
            {
                if (r.Landed || r.LandAt > Tick) continue;
                r.Landed = true; LandRecord(r, true);
                if (Over) return;
            }
        }

        void LandRecord(CamRec r, bool alert)
        {
            string cause = $"recorded:{r.Approach}:{r.Subject}:camera:{r.Cam}";
            var p = TurnSubject(r.Subject); // brief 096: a crew member by id
            if (!r.Witnessed) { AddTrail(R.CamTrail, cause); AddIncident("recorded", cause + (r.Face ? ":face" : ""), r.At, new[] { (p, r.At) }); } // one act, one incident: seen by people too, theirs stands
            if (!alert) { r.AtEnd = true; Note("camera.record-lands", $"The {r.Cam} camera's record of the {r.Approach} on {r.Subject} is on file at the end (+{R.CamTrail} T)."); return; }
            if (r.Incident == 0) r.Incident = ++Reported; // one act, one incident: counted already if the person turned reported it first
            RaiseAlert(Math.Max(R.CamAlert, R.AlertByIncident[Math.Min(r.Incident, R.AlertByIncident.Length) - 1]), cause);
            Note("camera.record-lands", $"The {r.Cam} camera's record reaches the desk: the Fixer's {r.Approach} on {r.Subject}{(r.Face ? ", his face on tape" : "")}. Alert {Alert}.");
            Alarm(null, r.At, null, "");
        }
        /// <summary>A report by the person an act was done to (rulebook reports): an incident for Alert, the nth reported incident raising it to at
        /// least byIncident[n−1]. One act, one incident: when a camera recorded this act, the record and the report are that incident, counted once.</summary>
        void Report(Person p, Order act)
        {
            var rec = camRecs.Find(x => ReferenceEquals(x.Act, act));
            int n = rec == null ? ++Reported : rec.Incident > 0 ? rec.Incident : (rec.Incident = ++Reported);
            RaiseAlert(R.AlertByIncident[Math.Min(n, R.AlertByIncident.Length) - 1], $"report:{p.Role}");
            AddIncident("report", "report:" + p.Role, PersonPos(p) ?? S.Stand(S.GuardPost), new[] { (p, PersonPos(p) ?? S.Stand(S.GuardPost)) });
            Note("report", $"{p.Name} reports the approach. Alert {Alert}.");
        }

        /// <summary>The mission ends (a fail, Go, a withdrawal): a record still to land is charged now, its Trail and its lead, no Alert.</summary>
        void RecordsAtEnd() { foreach (var r in camRecs) if (!r.Landed) { r.Landed = true; LandRecord(r, false); } }

        /// <summary>The records made so far, in order (host and debrief read): the camera, the subject's role, the approach, when made, when it lands,
        /// whether the Fixer's face is on it, and its state (pending, landed, or end: charged at the mission's end).</summary>
        public List<(string cam, string subject, string approach, int from, int landAt, bool face, string state)> CameraRecords()
        {
            var l = new List<(string, string, string, int, int, bool, string)>();
            foreach (var r in camRecs) l.Add((r.Cam, r.Subject, r.Approach, r.From, r.LandAt, r.Face, r.AtEnd ? "end" : r.Landed ? "landed" : "pending"));
            return l;
        }

        /// <summary>The Pay or Pressure card's camera mark (Quote.Recorded*), from what the cell can know: where the act is done (the Fixer's place
        /// in reach, else the person's), when it starts (after the walk in) and how long it runs; for each camera, whether it will be live at any
        /// tick of it (a loop on it ends then; a dead camera stays dead) and sees either place from its lens (no crowd: it moves). "yes" for a
        /// camera whose sight the cell knows, "?" for one it does not; the Trail, the Alert and the seconds to its landing; the loop's end inside
        /// the act. Read-only.</summary>
        void QuoteRecord(Quote q, string approach, Person p, Op op, Pt? where, Pt? subject)
        {
            q.Recorded = "no"; q.RecordedBy = ""; q.RecordTrail = 0; q.RecordAlert = Alert; q.RecordInS = -1; q.RecordLoopEnds = -1;
            if (Range || Array.IndexOf(R.CamApproaches, approach) < 0) return;
            var at = subject ?? PersonPos(p); if (!at.HasValue) return; // where she will be when the Fixer reaches her (a server's round read ahead, as the card's walk)
            Pt fixer = where ?? op.Pos;
            if (ServerOf(p) != null && PersonPos(p) is Pt now && !G.Within(op.Pos, now, approachReach) && Intercept(op, p, approachReach) is var w && w.HasValue && PoseAhead(p, w.Value.ticks) is var pa && pa.HasValue) { fixer = w.Value.path.End; at = pa.Value.pos; } // a server on her round: the walk in the order will make, and where she is when it ends
            int walkT = op.Id == OpId.Fixer ? Math.Max(0, WalkInTicks(op, "turn", p)) : q.WalkS * Hz; // the walk in as the order will walk it
            int start = Tick + walkT, end = start + (int)q.S * Hz, first = int.MaxValue; string by = ""; bool known = false; int loopEnds = -1;
            foreach (var c in S.Cameras)
            {
                if (CameraDark(c.Id) || (c.Id == "pickup" && !PickupCameraLive())) continue;
                if (!(InHeads(c, fixer) && CamSees(c, fixer)) && !(InHeads(c, at.Value) && CamSees(c, at.Value))) continue;
                int live = start;
                if (LoopOn && LoopCam == c.Id) { if (LoopUntil >= end) continue; live = Math.Max(start, LoopUntil); if (LoopUntil > start) loopEnds = LoopUntil; }
                if (live < first) { first = live; by = c.Id; known = Known("camera:" + c.Id)?.Sight ?? false; }
            }
            if (by.Length == 0) return;
            q.Recorded = known ? "yes" : "?"; q.RecordedBy = by; q.RecordTrail = R.CamTrail; q.RecordLoopEnds = loopEnds;
            q.RecordAlert = Math.Max(Alert, Math.Max(R.CamAlert, R.AlertByIncident[Math.Min(Reported + 1, R.AlertByIncident.Length) - 1]));
            q.RecordInS = (CameraLands(by, first) - Tick + Hz - 1) / Hz;
        }

        /// <summary>The records for the checkpoint hash: no bytes until one is made.</summary>
        internal void HashRecords(Hash.H h)
        {
            if (camRecs.Count == 0) return;
            h.S("recorded");
            foreach (var r in camRecs) h.S(r.Cam).S(r.Subject).S(r.Approach).I(r.From).I(r.LandAt).P(r.At).Z(r.Face).Z(r.Landed).Z(r.AtEnd).Z(r.Witnessed).I(r.Incident);
            h.Z(camCur != null);
        }
    }
}
