using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 100's reports, read once a tick from the engine's own lists (never its sentences): the takedown reports in transit (Game.Reps), the loiterer
    /// reports (AttentionReportsMade), the parked car's (ParkedReports), the cameras' records of a Pay or a Pressure (CameraRecords), the cut screens (CutReps) and
    /// the entry report (EntryReport). Each read is compared with the last: a report that landed on this tick tells its Alert row who made it and where
    /// (landedNow); one stopped pops "Report stopped" where it was marked; one still on its way is a marked reporter (OnWay, part 2: Law L4, news travels on
    /// foot). Told and learned (Sky's call, 2026-11-07): a camera's tape is marked in flight only for a camera the cell has learned (a Watch); the rest show
    /// their cause when the Alert rises. Read only: nothing here changes the run.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>A report that landed on this tick: its ledger cause, who made it (the engine's id), where it happened.</summary>
        sealed class WhyLanding { public string Cause = "", By = ""; public Pt? At; }
        readonly List<WhyLanding> landedNow = new();
        WhyLanding LandedFor(string cause) { foreach (var l in landedNow) if (l.Cause == cause) return l; return null; }

        /// <summary>A report still to land, marked on the street: how it travels (onWay.ways: radio, walk, run, look, camera, screen, robot, tell), who carries it
        /// (the engine's id, and the view that draws them), where it goes ({to}), when it lands (-1 not yet known: a look), and whether the cell can still stop it.</summary>
        public sealed class OnWayMark { public string Id = "", Way = "", By = "", View = "", To = "", About = ""; public bool Fix; public int LandAt = -1; public Pt? At; }
        /// <summary>The reports on their way now (rebuilt once a tick); the HUD draws each over whoever carries it.</summary>
        public readonly List<OnWayMark> OnWay = new();

        readonly List<string> repPhase = new(), attnState = new(), parkState = new(), camState = new();
        readonly List<Pt?> attnAt = new(), parkAt = new(), camAt = new();
        readonly List<bool> cutDone = new();
        string entryState = "";
        readonly Dictionary<int, HashSet<string>> noticedAll = new(); readonly Dictionary<int, List<string>> noticedNew = new();

        void WhyReportsReset()
        {
            landedNow.Clear(); OnWay.Clear(); repPhase.Clear(); attnState.Clear(); parkState.Clear(); camState.Clear(); attnAt.Clear(); parkAt.Clear(); camAt.Clear();
            cutDone.Clear(); entryState = ""; noticedAll.Clear(); noticedNew.Clear(); whyPlaces = null;
        }

        /// <summary>Who of a takedown's witnesses and watchers noticed it on this tick (TdAct.Noticed is a set: what is new since the last tick), a watcher first.</summary>
        string NoticedNow(TdAct a)
        {
            if (a == null || !noticedNew.TryGetValue(a.Id, out var l) || l.Count == 0) return "";
            foreach (var id in l) if (Game.PersonById(id) == null) return id;
            return l[0];
        }

        static string Was(List<string> l, int i, string now) { string w = i < l.Count ? l[i] : null; if (i < l.Count) l[i] = now; else l.Add(now); return w; }

        void WhyReports()
        {
            var g = Game; landedNow.Clear(); noticedNew.Clear();
            foreach (var a in g.Acts)
            {
                if (!noticedAll.TryGetValue(a.Id, out var all)) noticedAll[a.Id] = all = new HashSet<string>();
                foreach (var id in a.Noticed) if (all.Add(id)) { if (!noticedNew.TryGetValue(a.Id, out var nl)) noticedNew[a.Id] = nl = new List<string>(); nl.Add(id); }
            }
            for (int i = 0; i < g.Reps.Count; i++) // the takedown reports (Pulse §3.2)
            {
                var r = g.Reps[i]; var was = Was(repPhase, i, r.Phase); if (r.Phase == was || r.Kind == "tell") continue;
                if (r.Phase == "landed") landedNow.Add(new WhyLanding { Cause = $"report:{r.Kind}:{g.Acts[r.Act].Victim}", By = r.P?.Id ?? r.By, At = r.Place });
                else if (r.Phase == "stopped" && was != null && (was != "check" || (r.P?.Down ?? false))) Stopped("rep/" + r.Seq, r.P?.Id ?? r.By); // a check that ends walks on as a new report: not a stop
            }
            var attn = g.AttentionReportsMade(); // the loiterer reports (rulebook attention)
            for (int i = 0; i < attn.Count; i++)
            {
                var (w, op, _, _, st) = attn[i]; bool fresh = i >= attnState.Count; var was = Was(attnState, i, st);
                if (fresh) attnAt.Add(WhyPos(op));
                if (st == was) continue;
                if (st == "landed") landedNow.Add(new WhyLanding { Cause = $"loiter:{w}:{op}", By = w, At = attnAt[i] }); else if (st == "stopped") Stopped("attn/" + i, w);
            }
            var park = g.ParkedReports(); // our parked car (attention.parked)
            for (int i = 0; i < park.Count; i++)
            {
                var (w, _, _, st) = park[i]; bool fresh = i >= parkState.Count; var was = Was(parkState, i, st);
                if (fresh) parkAt.Add(g.GetawayCarPos());
                if (st == was) continue;
                if (st == "landed") landedNow.Add(new WhyLanding { Cause = $"car-reported:{w}", By = w, At = parkAt[i] }); else if (st == "stopped") Stopped("park/" + i, w);
            }
            var recs = g.CameraRecords(); // a Pay or a Pressure on tape (camera.approaches)
            for (int i = 0; i < recs.Count; i++)
            {
                var (cam, subject, approach, _, _, _, st) = recs[i]; bool fresh = i >= camState.Count; var was = Was(camState, i, st);
                if (fresh) camAt.Add(WhyPos(subject) ?? WhyPos("fixer"));
                if (st != was && st == "landed") landedNow.Add(new WhyLanding { Cause = $"recorded:{approach}:{subject}:camera:{cam}", By = "camera:" + cam, At = camAt[i] });
            }
            for (int i = 0; i < g.CutReps.Count; i++) // the desk sees a cut camera's dead screen
            {
                var c = g.CutReps[i]; bool was = i < cutDone.Count && cutDone[i]; if (i < cutDone.Count) cutDone[i] = c.Done; else cutDone.Add(c.Done);
                string cam = c.Cam.StartsWith("camera:", StringComparison.Ordinal) ? c.Cam[7..] : c.Cam;
                if (c.Done && !was) landedNow.Add(new WhyLanding { Cause = $"report:cut:camera:{cam}", By = "desk", At = WhyPos("camera:" + cam) });
            }
            var (_, _, est) = g.EntryReport; // the patrol saw the entry
            if (est != entryState) { if (est == "landed") landedNow.Add(new WhyLanding { Cause = "entry:patrol:enforcer", By = "patrol", At = g.S.HasPlace(g.S.GuardPost) ? g.S.At(g.S.GuardPost) : null }); else if (est == "stopped") Stopped("entry", "patrol"); entryState = est; }
            OnWayNow(attn, park, recs);
        }

        /// <summary>The marks for every report still on its way (rebuilt each tick, in the engine's order).</summary>
        void OnWayNow(List<(string watcher, string op, int from, int landAt, string state)> attn, List<(string watcher, int from, int landAt, string state)> park,
            List<(string cam, string subject, string approach, int from, int landAt, bool face, string state)> recs)
        {
            var g = Game; OnWay.Clear();
            void Add(string id, string way, string by, int landAt, bool fix, string to = "", string about = "", Pt? at = null)
            {
                var p = IsOpName(by) ? null : by is "waitress" or "driver" or "guard" or "councillor" ? g.PersonByRole(by) : g.PersonById(by);
                string view = p != null ? (p.Role.Length > 0 ? p.Role : CrowdKey(p)) : by.StartsWith("camera:", StringComparison.Ordinal) ? "camera/" + by[7..] : by is "patrol" ? "patrol" : "";
                OnWay.Add(new OnWayMark { Id = id, Way = way, By = by, View = view, LandAt = landAt >= int.MaxValue / 8 ? -1 : landAt, Fix = fix, To = to, About = about, At = at ?? WhyPos(by) });
            }
            bool Learned(string cam) => g.Known(cam.StartsWith("camera:", StringComparison.Ordinal) ? cam : "camera:" + cam)?.Sight ?? false;
            string WatcherWay(string w) => w.StartsWith("camera:", StringComparison.Ordinal) ? "camera" : w == "patrol" || w.StartsWith("robot", StringComparison.Ordinal) ? "robot" : "radio";
            foreach (var r in g.Reps)
            {
                if (r.Phase is "stopped" or "landed" or "flee") continue;
                string by = r.P?.Id ?? r.By, id = "rep/" + r.Seq;
                if (r.Kind == "tell") { if (r.Phase == "walk") Add(id, "tell", by, -1, true, WhyTo(r.W?.Refuge)); continue; }
                switch (r.Phase)
                {
                    case "radio": Add(id, "radio", by, r.LandAt, true); break;
                    case "walk": case "call": Add(id, r.W != null && r.W.Pace >= g.R.CrowdRunSpeed ? "run" : "walk", by, r.LandAt, true, WhyTo(r.W?.Refuge)); break;
                    case "check": Add(id, "look", by, -1, true); break;
                    case "watcher":
                        if (by.StartsWith("camera:", StringComparison.Ordinal)) { if (Learned(by)) Add(id, "camera", by, r.LandAt, false); }
                        else Add(id, "robot", by, r.LandAt, false, at: by.StartsWith("round:", StringComparison.Ordinal) ? r.Place : (Pt?)null);
                        break;
                }
            }
            for (int i = 0; i < attn.Count; i++) { var (w, op, _, land, st) = attn[i]; if (st != "pending") continue; string way = WatcherWay(w); if (way == "camera" && !Learned(w)) continue; Add("attn/" + i, way, w, land, way != "camera", about: op); }
            for (int i = 0; i < park.Count; i++) { var (w, _, land, st) = park[i]; if (st != "pending") continue; string way = WatcherWay(w); if (way == "camera" && !Learned(w)) continue; Add("park/" + i, way, w, land, way != "camera"); }
            for (int i = 0; i < recs.Count; i++) { var (cam, subject, _, _, land, _, st) = recs[i]; if (st == "pending" && Learned(cam)) Add("rec/" + i, "camera", "camera:" + cam, land, false, about: subject); }
            foreach (var c in g.CutReps) if (!c.Done) { string cam = c.Cam.StartsWith("camera:", StringComparison.Ordinal) ? c.Cam : "camera:" + c.Cam; Add("cut/" + cam, "screen", cam, c.LandAt, false, about: cam); }
            var (_, eland, est) = g.EntryReport; if (est == "pending") Add("entry", "robot", "patrol", eland, true, about: "enforcer");
        }

        /// <summary>A report that was marked on its way never lands: the stopped pop where its reporter is (onWay.stopped), its line kept for Alt. Called during the
        /// tick's read, while OnWay still holds the last tick's marks.</summary>
        void Stopped(string id, string by)
        {
            if (Text == null || !OnWay.Exists(x => x.Id == id) || Text.OnWayStoppedPlate.Length == 0) return;
            var line = PlayText.FillWhy(Text.OnWayStoppedLine, new Dictionary<string, string> { ["who"] = WhyWho(by) });
            popCode = "report-stopped"; Pop(Text.OnWayStoppedPlate, null, WhyWorld(WhyPos(by)), Feedback.Ok, 2.5f, "icons/singles/check", PlayText.Unfilled(line) ? "" : line);
            StoppedReports++;
        }
        /// <summary>The host check reads it: marked reports that never landed this run.</summary>
        public int StoppedReports { get; private set; }

        // ---------------------------------------------------------------- the plate's words (the HUD asks per frame)

        /// <summary>Whole seconds until a mark lands (ceil, never below 0); -1 when it has no landing yet.</summary>
        public int OnWaySeconds(OnWayMark m) => m.LandAt < 0 || Game == null ? -1 : Math.Max(0, (m.LandAt - Game.Tick + Game.Hz - 1) / Game.Hz);
        /// <summary>Fable's plate for the way ("Going to the Office door · 8 s"), and its fix line ("Stop them before it lands" / "Too late to stop it").</summary>
        public (string plate, string fix, string line) OnWayWords(OnWayMark m)
        {
            if (Text == null || !Text.OnWayWays.TryGetValue(m.Way, out var w)) return ("", "", "");
            int s = OnWaySeconds(m);
            var f = new Dictionary<string, string> { ["to"] = m.To, ["who"] = WhyWho(m.By), ["about"] = WhyWho(m.About), ["s"] = s >= 0 ? s.ToString() : null };
            string F(string x) { var o = PlayText.FillWhy(x, f); return PlayText.Unfilled(o) ? "" : o; }
            return (F(w.plate), m.Fix ? Text.OnWayFix : Text.OnWayNofix, F(w.line));
        }
        /// <summary>The way's kit icon (icons/singles), for the plate's disc.</summary>
        public static string OnWayIcon(string way) => way switch
        {
            "radio" => "radio", "walk" => "s_moving", "run" => "run", "look" => "eye_open", "camera" => "watch", "screen" => "disable", "robot" => "call", "checkin" => "routine", "tell" => "s_calling", _ => "dot",
        };
    }
}
