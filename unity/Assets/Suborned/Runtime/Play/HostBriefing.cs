using System;
using System.Collections.Generic;
using Path = System.IO.Path;
using File = System.IO.File;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 062 B, "on the street" (Sky 2026-10-25: "Your recommendation seem good"; Glyph's spec, briefs/062-briefing-and-start.md
    /// §4, §5): the briefing is the game. The run opens with the play camera on the mission's briefing frame (brief.camera, else the pickup
    /// as the board's `overhead-pickup`: the Office door right of centre, the opening yaw, play zoom), the briefing screen over the live,
    /// paused street (Briefing.cs). Begin starts the start sequence: 0 to 0.3 s the screen lifts away; 0.3 to 3.3 s the camera glides to the
    /// cell (eased in and out; any key or click lands it at once); 3.3 s the four ground rings pulse and the roster slides in (0.25 s); 3.6 s
    /// the clock bar fades in and the clock starts at 0:00, the goal line up. View only: the engine sees the clock start, as before.
    /// Scripts and proofs that call StartClock or SetPaused(false) skip the sequence and land the camera on the opening view at once.</summary>
    public sealed partial class HandoverHost
    {
        public const float BriefExitS = 0.3f, BriefGlideS = 3.0f, BriefSettleS = 0.3f, BriefRosterS = 0.25f;
        /// <summary>When the clock starts, in seconds after Begin (3.6).</summary>
        public const float BriefPlayAt = BriefExitS + BriefGlideS + BriefSettleS;
        /// <summary>Seconds since Begin while the start sequence runs; -1 when it does not.</summary>
        public float StartT { get; private set; } = -1f;
        public bool Starting => StartT >= 0f;
        /// <summary>Proofs: the sequence holds where it is (StartSeek moves it) so a still can be taken at a given moment.</summary>
        public static bool StartFreeze;
        /// <summary>Automated runs open on the opening view as before; a proof of the briefing (FrontEnd.Proof) sets this to see the street briefing and the glide.</summary>
        public static bool BriefCamInAutomation;
        /// <summary>The camera stands on the briefing frame (it glides to the cell on Begin, or lands there when the clock starts another way).</summary>
        public bool BriefCamOn { get; private set; }
        /// <summary>The briefing's data, read once a run (BriefScreen.Load).</summary>
        public BriefScreen Brief { get; private set; }
        public int StartSkips { get; private set; }
        /// <summary>When the start sequence started the clock (the clock bar fades in from here); -100 when a script started it (no fade).</summary>
        public float SequencePlayedAt { get; private set; } = -100f;

        Vector3 glideFrom, glideTo; float glideYawFrom, glideYawTo;

        /// <summary>At a new run (Begin), when the briefing opens: the play camera on the briefing frame.</summary>
        void BriefingCamera()
        {
            StartT = -1f; BriefCamOn = false;
            Guard("briefing", () => Brief = BriefScreen.Load(this));
            if (Brief == null || !cam || !ShowBriefing) return;
            if (GameBoot.Automation && !BriefCamInAutomation) return; // proofs, shots and the bot keep the opening view after Begin (they assume it); the front-end proof turns it on
            TurnInit();
            yawNow = yawFrom = yawTo = baseYaw + Brief.YawFromOpening; turnStart = -1f; cam.transform.rotation = Quaternion.Euler(pitch, yawNow, 0f);
            LookAt(Brief.CameraAt); BriefCamOn = true;
        }

        /// <summary>The camera from the briefing frame to the opening view at once (a script's StartClock, Esc or B closing the briefing).</summary>
        void BriefCamLand()
        {
            if (!BriefCamOn || !cam) return; BriefCamOn = false; StartT = -1f;
            yawNow = yawFrom = yawTo = baseYaw; turnStart = -1f; cam.transform.rotation = Quaternion.Euler(pitch, yawNow, 0f);
            LookAt(frame.World(OpeningFocus));
        }

        /// <summary>The briefing's Begin (its word, Space): on the briefing frame the start sequence; a briefing opened again mid-run (B) just closes and runs.</summary>
        public void BriefingBegin()
        {
            if (!ShowBriefing) return;
            if (!BriefCamOn || !cam || Game == null || Game.Over) { StartClock(); return; }
            ShowBriefing = false; Paused = true; StartT = 0f; ClearHover();
            glideFrom = focus; glideTo = frame.World(OpeningFocus); glideTo.y = 0f; glideYawFrom = yawNow; glideYawTo = baseYaw;
            Status = "";
        }

        /// <summary>Per frame from Controls while the sequence runs (its keys only): true while it holds the input.</summary>
        bool StartFrame(Keyboard kb, UnityEngine.InputSystem.Mouse mouse)
        {
            if (StartT < 0f) return false;
            ClearHover();
            if (!StartFreeze) StartT += Time.unscaledDeltaTime;
            bool skip = kb != null && kb.anyKey.wasPressedThisFrame || mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame);
            if (skip && StartT < BriefExitS + BriefGlideS) { StartT = BriefExitS + BriefGlideS; StartSkips++; } // lands now; the settle and the clock follow as ever
            ApplyGlide();
            if (StartT >= BriefPlayAt) { StartT = -1f; BriefCamOn = false; StartClock(); SequencePlayedAt = Time.unscaledTime; }
            return true;
        }

        void ApplyGlide()
        {
            if (!cam) return;
            float u = Mathf.Clamp01((StartT - BriefExitS) / BriefGlideS), e = u < 0.5f ? 4f * u * u * u : 1f - Mathf.Pow(-2f * u + 2f, 3f) / 2f; // ease in and out (cubic)
            yawNow = yawFrom = yawTo = glideYawFrom + Mathf.DeltaAngle(glideYawFrom, glideYawTo) * e; turnStart = -1f;
            cam.transform.rotation = Quaternion.Euler(pitch, yawNow, 0f);
            focus = Vector3.Lerp(glideFrom, glideTo, e); focus.y = 0f; PlaceCamera();
        }

        /// <summary>Proofs: the sequence at `t` seconds after Begin (with StartFreeze, a still of that moment).</summary>
        public void StartSeek(float t) { if (StartT < 0f) return; StartT = Mathf.Max(0f, t); ApplyGlide(); if (StartT >= BriefPlayAt) { StartT = -1f; BriefCamOn = false; StartClock(); SequencePlayedAt = Time.unscaledTime; } }
    }

    /// <summary>Brief 062 B: what the briefing shows, read from the mission (nothing hand-authored for the screen): the title, hook and
    /// objective from brief.json (variants[pick]); the target from the mission's roles (brief.target, else the councillor) with its file
    /// photograph (Forma's portrait; the npc badge until it lands); the ways from mission brief.ways[] (Waymark), else derived from the
    /// roles, ties and windows; the clock marks from brief.clock[], else the timeline (0:00, the chime, the end); the camera from
    /// brief.camera, else the pickup.
    /// The shape agreed by Glyph, Fable and Waymark (briefs/062-briefing-and-start.md "Portraits and ids"), in brief.json next to variants
    /// (mission.json `brief` is read too): ways[]: { "id": "front-door", "contact": "waitress", "steps": ["badges/signal_guard", "enter"] };
    /// the name is text.json less.briefing.ways[id]; the contact's face is portraits/face_&lt;contact&gt; in a Cameo; a step is a kit single
    /// (icons/singles/&lt;name&gt;) or a badges/ path; "at" (optional: "door", "car" or a place id) is where its leader goes (front-door: the
    /// door, pickup: the car). clock[]: { "id": "start" | "chime" | "over", "at": "5:30" (optional: from the timeline by id), "icon", "lit" }.
    /// camera: { "place": "d:Meet driver", "offsetCm": [500, -500], "yaw": 0 } (yaw from the opening view). target: a role id. The file
    /// photograph is portraits/card_&lt;target&gt; (Glyph's duotone), the npc badge when it is missing.</summary>
    public sealed class BriefScreen
    {
        public sealed class Way { public string Name = ""; public readonly List<string> Steps = new(); public Vector3? At; }
        public sealed class Mark { public string At = "", Icon = ""; public bool Lit; public float S; }
        public string Title = "", Hook = "", Objective = "", TargetRole = "councillor", TargetName = "", TargetFact = "Alive", TargetPhoto = "", TargetBadge = "badges/npc_councillor";
        public Vector3? TargetAt, OfficeAt; public string OfficeName = "Records Office";
        public readonly List<Way> Ways = new(); public readonly List<Mark> Clock = new();
        public float EndS = 510f; public Vector3 CameraAt; public float YawFromOpening;
        /// <summary>Where each part came from ("mission brief.ways", "derived from roles", ...): the proof logs it.</summary>
        public readonly List<string> Sources = new();

        /// <summary>The mission's JSON file (contracts/missions/&lt;m&gt;/&lt;file&gt; in the Editor; StreamingAssets/missions/&lt;m&gt;/&lt;file&gt; in a build), or null.</summary>
        public static JNode MissionFile(string file, string mission = "handover")
        {
            string path = Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "contracts", "missions", mission, file)) : Path.Combine(Application.streamingAssetsPath, "missions", mission, file);
            try { return File.Exists(path) ? JNode.Parse(File.ReadAllText(path), file) : null; } catch (Exception e) { Debug.LogWarning($"Briefing: {path}: {e.Message}"); return null; }
        }

        static string S(JNode n, string k) => n?.Opt(k) is JNode v && v.Kind == JKind.Str ? v.Str() : null;
        static float Secs(string mss) { if (string.IsNullOrEmpty(mss)) return -1f; var p = mss.Split(':'); return p.Length == 2 && int.TryParse(p[0], out var m) && float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s) ? m * 60f + s : -1f; }

        public static BriefScreen Load(HandoverHost h)
        {
            var b = new BriefScreen(); var g = h.Game; var site = g.S;
            var fb = FrontEnd.LoadBrief(); var v = fb.Picked;
            b.Title = (fb.title ?? "").ToUpperInvariant(); b.Hook = v.hook ?? ""; b.Objective = v.dossier?.objective ?? "";
            var mission = MissionFile("mission.json"); var bj = MissionFile("brief.json");
            JNode Brief(string k) => bj?.Opt(k) ?? mission?.Opt("brief")?.Opt(k);
            // the target: brief.target, else the councillor; its name from the dossier's target line ("Councillor Vale. Holds…"), else the text's
            b.TargetRole = Brief("target") is JNode tr && tr.Kind == JKind.Str ? tr.Str() : "councillor";
            string dt = v.dossier?.target ?? ""; int dot = dt.IndexOf('.');
            b.TargetName = dot > 0 ? dt[..dot] : h.Text?.TargetName(b.TargetRole, "Vale") ?? "Vale";
            b.TargetPhoto = $"portraits/card_{b.TargetRole}"; b.TargetBadge = HudModel.PersonBadge(b.TargetRole);
            b.TargetFact = h.Text?.BriefWord("fact", "Alive") ?? "Alive"; b.OfficeName = h.Text?.BriefWord("office", "Records Office") ?? "Records Office";
            Vector3 W(Pt p) => h.frame.World(p);
            Vector3? Place(string id) { try { return id == "door" ? W(site.At(site.GuardPost)) : id == "car" ? W(g.CarPos()) : site.HasPlace(id) ? W(site.At(id)) : null; } catch (Exception) { return null; } }
            b.TargetAt = Place("door"); b.OfficeAt = b.TargetAt;
            // the ways
            string WayName(string id) => h.Text?.BriefWord("ways." + id, null) ?? (id == "front-door" ? "Front door" : id == "pickup" ? "Pickup" : id);
            if (Brief("ways") is JNode wl && wl.Kind == JKind.Arr)
            {
                foreach (var w in wl.Items)
                {
                    string id = S(w, "id") ?? ""; var way = new Way { Name = S(w, "name") ?? WayName(id) }; if (S(w, "contact") is string c) way.Steps.Add("face:" + c);
                    if (w.Opt("steps") is JNode st && st.Kind == JKind.Arr) foreach (var x in st.Items) if (x.Kind == JKind.Str && way.Steps.Count < 3) way.Steps.Add(x.Str());
                    way.At = Place(S(w, "at") ?? (id == "pickup" ? "car" : "door")); b.Ways.Add(way);
                }
                b.Sources.Add(bj?.Opt("ways") != null ? "ways: brief.json ways" : "ways: mission brief.ways");
            }
            else
            {   // derived: each window's role and the role tied to it (route A: Inez, her lover the guard, the door); the official driver's pickup
                var roles = new Dictionary<string, JNode>(); if (mission?.Opt("roles") is JNode rl) foreach (var r in rl.Items) if (S(r, "id") is string id) roles[id] = r;
                if (mission?.Opt("windows") is JNode ws)
                    foreach (var w in ws.Items)
                    {
                        string role = S(w, "role") ?? ""; string contact = null;
                        foreach (var kv in roles) if (kv.Value.Opt("tiedTo") is JNode tt) foreach (var t in tt.Items) if (S(t, "role") == role) contact = kv.Key;
                        var way = new Way { Name = WayName(S(w, "id") ?? ""), At = Place("door") };
                        if (contact != null) way.Steps.Add("face:" + contact); way.Steps.Add(HudModel.PersonBadge(role)); way.Steps.Add("enter");
                        b.Ways.Add(way);
                    }
                if (roles.ContainsKey("driver") && roles.ContainsKey(b.TargetRole)) { var way = new Way { Name = WayName("pickup"), At = Place("car") }; way.Steps.Add("face:driver"); way.Steps.Add("car"); way.Steps.Add("take"); b.Ways.Add(way); }
                b.Sources.Add("ways: derived from mission roles and windows (brief.ways absent)");
            }
            // the clock
            b.EndS = Mathf.Max(60f, g.R != null ? CloseS(h) : 510f);
            var tl = new List<(float s, string at, string what)>();
            if (mission?.Opt("timeline") is JNode tln) foreach (var t in tln.Items) { var at = S(t, "at"); float s = Secs(at); if (s >= 0f) tl.Add((s, at, S(t, "what") ?? "")); }
            string ById(string id) => tl.Count == 0 ? "" : id == "start" ? tl[0].at : id == "over" ? tl[tl.Count - 1].at : id == "chime" ? tl.Find(x => x.what.Contains("chime")).at ?? "" : "";
            if (Brief("clock") is JNode cl && cl.Kind == JKind.Arr)
            {
                foreach (var c in cl.Items) { string id = S(c, "id") ?? ""; var mk = new Mark { At = S(c, "at") ?? ById(id), Icon = S(c, "icon") ?? "p_time", Lit = c.Opt("lit") is JNode lt && lt.Kind == JKind.Bool ? lt.Bool() : id == "chime" }; mk.S = Secs(mk.At); if (mk.S >= 0f) b.Clock.Add(mk); }
                b.Sources.Add("clock: brief clock");
            }
            else
            {
                if (tl.Count > 0)
                {
                    b.Clock.Add(new Mark { At = tl[0].at, S = tl[0].s, Icon = "p_time" });
                    var chime = tl.Find(x => x.what.Contains("chime")); if (chime.at != null) b.Clock.Add(new Mark { At = chime.at, S = chime.s, Icon = "enter", Lit = true });
                    var last = tl[tl.Count - 1]; if (last.s > tl[0].s) b.Clock.Add(new Mark { At = last.at, S = last.s, Icon = "car" });
                }
                b.Sources.Add("clock: derived from mission timeline (brief.clock absent)");
            }
            if (b.Clock.Count > 0) b.EndS = Mathf.Max(b.EndS, b.Clock[b.Clock.Count - 1].S);
            // the camera: brief.camera, else the pickup (the board's overhead-pickup frame: 5 m on from the driver's meeting point)
            Vector3? camAt = null;
            if (Brief("camera") is JNode cm)
            {
                var p = S(cm, "place"); var off = cm.Opt("offsetCm") is JNode o && o.Kind == JKind.Arr && o.Items.Count == 2 ? new Pt(o.Items[0].Int(), o.Items[1].Int()) : new Pt(0, 0);
                if (p != null && site.HasPlace(p)) { var a = site.At(p); camAt = W(new Pt(a.X + off.X, a.Y + off.Y)); }
                if (cm.Opt("yaw") is JNode y && y.Kind == JKind.Num) b.YawFromOpening = y.Int();
                b.Sources.Add("camera: mission brief.camera");
            }
            if (!camAt.HasValue && site.HasPlace("d:Meet driver")) { var md = site.At("d:Meet driver"); camAt = W(new Pt(md.X + 500, md.Y - 500)); b.Sources.Add("camera: the pickup (brief.camera absent)"); }
            b.CameraAt = camAt ?? W(HandoverHost.OpeningFocus);
            return b;
        }

        static float CloseS(HandoverHost h) { var c = h.Text?.Close ?? "8:30"; float s = Secs(c); return s > 0f ? s : 510f; }
    }
}
