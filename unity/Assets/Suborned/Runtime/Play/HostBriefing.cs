using System;
using System.Collections.Generic;
using Path = System.IO.Path;
using File = System.IO.File;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 070 (GDD v0.27.0 §10 briefing), after brief 062 B's start: the run opens with the briefing page over the paused street and the
    /// play camera standing at the target's door (brief.target.at, the quarter step facing its outward side, play zoom), hidden under the page
    /// (Briefing.cs). Begin starts the start sequence: 0 to 0.3 s the page lifts away onto that camera; 0.3 to 3.3 s the camera glides to the
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
        /// <summary>Glyph's N4 (0.2.122): the pointer's badge and every hover cue stay hidden while the briefing is up and through the glide, until the
        /// roster lands (the settle, 3.3 s after Begin); no order can be given there.</summary>
        public bool PointerHeld => ShowBriefing || Starting && StartT < BriefExitS + BriefGlideS;
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
            yawNow = yawFrom = yawTo = baseYaw + BriefYaw(); turnStart = -1f; cam.transform.rotation = Quaternion.Euler(pitch, yawNow, 0f);
            LookAt(Brief.CameraAt); BriefCamOn = true;
        }

        /// <summary>Brief 070: the quarter step (from the opening yaw) whose view looks most squarely onto the target door's outward side (stills.json
        /// targetDoor.normal, block frame); brief.camera.yaw without it.</summary>
        float BriefYaw()
        {
            if (Brief.TargetNormal is not Vector2 n) return Brief.YawFromOpening;
            var wn = new Vector3(frame.axisX.x * n.x + frame.axisY.x * n.y, 0f, frame.axisX.y * n.x + frame.axisY.y * n.y).normalized;
            float best = 0f, bestDot = float.NegativeInfinity;
            for (int k = 0; k < 4; k++) { float y = (baseYaw + 90f * k) * Mathf.Deg2Rad; float d = Vector3.Dot(new Vector3(Mathf.Sin(y), 0f, Mathf.Cos(y)), -wn); if (d > bestDot + 1e-4f) { bestDot = d; best = 90f * k; } }
            return best;
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

    /// <summary>Brief 070 (GDD v0.27.0 §10 briefing; Glyph's mock briefs/070/glyph): what the briefing page shows, read from the mission's data, nothing
    /// hand-authored for the screen. brief.json: the title, place and time, variants[pick] hook and objective, the target (role, at) and the ways in
    /// (id, contact). text.json: the way names (less.briefing.ways), the first sentence of each way (briefing.hints, after "Name: "), and the page's
    /// own lines (briefing.page: the target's second fact, each way's `when` and second sentence, the ending, the clock's stations, one line per
    /// face); each station's label from briefing.timeline; the stills' captions from targets.&lt;key&gt;.name; Heat from briefing.heat. The stills, their
    /// pins and the plan come from the build's stills folder (tools/game/briefstills.ts: StreamingAssets/missions/&lt;m&gt;/stills/stills.json and one
    /// JPEG per pin; in the Editor out/build/play/stills/&lt;m&gt;/). Begin lifts the page onto the camera at the target's door (brief.target.at,
    /// the quarter step facing its outward side), then the glide to the cell.</summary>
    public sealed class BriefScreen
    {
        public sealed class Pin { public string Id = "", Way = "", N = "", Caption = "", File = ""; public Vector2 At; public Texture2D Still; }
        public sealed class Way { public string Id = "", Name = "", When = ""; public readonly List<string> Lines = new(); public readonly List<Pin> Pins = new(); }
        public sealed class Station { public string At = "", Label = ""; public bool Lit; public float S; }
        public string Title = "", Place = "", Time = "", Hook = "", Objective = "", TargetRole = "councillor", TargetName = "", TargetFact = "", TargetFact2 = "", TargetPhoto = "", TargetBadge = "badges/npc_councillor";
        public string Ending = "";
        public Pin EndPin;
        public Vector3? TargetAt; public Vector2? TargetNormal;
        public readonly List<Way> Ways = new(); public readonly List<Station> Clock = new();
        public readonly Dictionary<string, string> CellLines = new();
        /// <summary>The plan (block metres, x south, y east): its bounds, the buildings (name, polygon), the bays and drives, the green and the water.</summary>
        public Rect PlanBounds; public readonly List<(string name, Vector2[] poly)> PlanBuildings = new(); public readonly List<Vector2[]> PlanGround = new(), PlanGreen = new(), PlanWater = new();
        public Vector3 CameraAt; public float YawFromOpening;
        public int StillsFound, StillsWanted;
        /// <summary>Where each part came from: the proof logs it.</summary>
        public readonly List<string> Sources = new();

        /// <summary>The mission's JSON file (contracts/missions/&lt;m&gt;/&lt;file&gt; in the Editor; StreamingAssets/missions/&lt;m&gt;/&lt;file&gt; in a build), or null.</summary>
        public static JNode MissionFile(string file, string mission = "handover")
        {
            string path = Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "contracts", "missions", mission, file)) : Path.Combine(Application.streamingAssetsPath, "missions", mission, file);
            try { return File.Exists(path) ? JNode.Parse(File.ReadAllText(path), file) : null; } catch (Exception e) { Debug.LogWarning($"Briefing: {path}: {e.Message}"); return null; }
        }
        /// <summary>The build's stills folder (tools/game/briefstills.ts): in the app's StreamingAssets, or the last build's out/ folder in the Editor.</summary>
        public static string StillsDir(string mission = "handover") => Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "out", "build", "play", "stills", mission)) : Path.Combine(Application.streamingAssetsPath, "missions", mission, "stills");

        static string S(JNode n, string k) => n?.Opt(k) is JNode v && v.Kind == JKind.Str ? v.Str() : null;
        static float Secs(string mss) { if (string.IsNullOrEmpty(mss)) return -1f; var p = mss.Split(':'); return p.Length == 2 && int.TryParse(p[0], out var m) && float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s) ? m * 60f + s : -1f; }
        static float F(JNode n) => n != null && n.Kind == JKind.Num && float.TryParse(n.NumberText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0f;
        static Vector2 V(JNode n) => n != null && n.Kind == JKind.Arr && n.Items.Count == 2 ? new Vector2(F(n.Items[0]), F(n.Items[1])) : Vector2.zero;
        static Vector2[] Poly(JNode n) { var l = new List<Vector2>(); if (n != null && n.Kind == JKind.Arr) foreach (var x in n.Items) l.Add(V(x)); return l.ToArray(); }
        static string Cap1(string s) => string.IsNullOrEmpty(s) ? "" : char.ToUpperInvariant(s[0]) + s[1..];

        public static BriefScreen Load(HandoverHost h)
        {
            var b = new BriefScreen(); var g = h.Game; var site = g.S; var tx = h.Text;
            var fb = FrontEnd.LoadBrief(); var v = fb.Picked;
            b.Title = fb.title ?? ""; b.Place = fb.place ?? ""; b.Time = fb.time ?? ""; b.Hook = v.hook ?? ""; b.Objective = v.dossier?.objective ?? "";
            var mission = MissionFile("mission.json"); var bj = MissionFile("brief.json");
            JNode Brief(string k) => bj?.Opt(k) ?? mission?.Opt("brief")?.Opt(k);
            // the target: brief.target { role, at }; the name and first fact from the dossier's target line ("Councillor Vale. Holds the file on you.")
            var tnode = Brief("target"); string targetAt = tnode != null && tnode.Kind != JKind.Str ? S(tnode, "at") : null;
            b.TargetRole = tnode is JNode tr && tr.Kind == JKind.Str ? tr.Str() : S(tnode, "role") ?? "councillor";
            string dt = v.dossier?.target ?? ""; int dot = dt.IndexOf('.');
            b.TargetName = dot > 0 ? dt[..dot] : tx?.TargetName(b.TargetRole, "Vale") ?? "Vale";
            b.TargetFact = dot > 0 ? dt[(dot + 1)..].Trim() : ""; b.TargetFact2 = tx?.PageTarget ?? "";
            b.TargetPhoto = $"portraits/card_{b.TargetRole}"; b.TargetBadge = HudModel.PersonBadge(b.TargetRole);
            Vector3 W(Pt p) => h.frame.World(p);
            Vector3? Place(string id) { try { return site.HasPlace(id) ? W(site.At(id)) : site.HasPlace("d:" + id) ? W(site.At("d:" + id)) : null; } catch (Exception) { return null; } }
            b.TargetAt = (targetAt != null ? Place(targetAt) : null) ?? (site.HasPlace(site.GuardPost) ? W(site.At(site.GuardPost)) : null);
            b.Sources.Add(targetAt != null ? $"target at: brief.target.at {targetAt}" : "target at: the door (brief.target.at absent)");
            // the stills, their pins and the plan (the build's stills folder)
            var dir = StillsDir(); JNode st = null;
            try { var f = Path.Combine(dir, "stills.json"); if (File.Exists(f)) st = JNode.Parse(File.ReadAllText(f), "stills.json"); } catch (Exception e) { Debug.LogWarning($"Briefing: {dir}/stills.json: {e.Message}"); }
            var pins = new List<Pin>();
            if (st != null)
            {
                foreach (var p in st["pins"].Items)
                {
                    var pin = new Pin { Id = S(p, "id") ?? "", Way = S(p, "way") ?? "", N = S(p, "n") ?? "", File = S(p, "file") ?? "", At = V(p.Opt("at")) };
                    var key = S(p, "text") ?? ""; pin.Caption = key.Length > 0 && tx != null && tx.TargetNames.TryGetValue(key, out var nm) ? Cap1(nm) : "";
                    var file = Path.Combine(dir, pin.File); b.StillsWanted++;
                    if (pin.File.Length > 0 && File.Exists(file)) { var t = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = "brief-still " + pin.Id, wrapMode = TextureWrapMode.Clamp }; if (t.LoadImage(File.ReadAllBytes(file), true)) { pin.Still = t; b.StillsFound++; } }
                    pins.Add(pin);
                }
                if (st.Opt("plan") is JNode pl)
                {
                    var bd = pl["bounds"].Items; b.PlanBounds = Rect.MinMaxRect(F(bd[0]), F(bd[1]), F(bd[2]), F(bd[3]));
                    foreach (var x in pl["buildings"].Items) b.PlanBuildings.Add((S(x, "name") ?? "", Poly(x.Opt("polygon"))));
                    foreach (var (k, into) in new[] { ("ground", b.PlanGround), ("green", b.PlanGreen), ("water", b.PlanWater) }) if (pl.Opt(k) is JNode arr) foreach (var x in arr.Items) into.Add(Poly(x));
                }
                if (st.Opt("targetDoor") is JNode td && td.Kind == JKind.Obj && td.Opt("normal") is JNode tn && tn.Kind == JKind.Arr) b.TargetNormal = V(tn);
                b.Sources.Add($"stills: {b.StillsFound}/{b.StillsWanted} from {dir}");
            }
            else b.Sources.Add($"stills: none ({dir}/stills.json absent: bun tools/game/briefstills.ts after the build)");
            // the ways: brief.json ways[] (id) → the name (less.briefing.ways), the two sentences (briefing.hints after "Name: ", briefing.page.ways.<id>.line), the plate (when), the pins
            if (Brief("ways") is JNode wl && wl.Kind == JKind.Arr)
                foreach (var w in wl.Items)
                {
                    string id = S(w, "id") ?? ""; var way = new Way { Id = id, Name = tx?.BriefWord("ways." + id, null) ?? Cap1(id.Replace('-', ' ')) };
                    if (tx != null && tx.HintBy.TryGetValue(id, out var hint)) { int c = hint.IndexOf(": ", StringComparison.Ordinal); way.Lines.Add(Cap1(c >= 0 ? hint[(c + 2)..] : hint)); }
                    if (tx != null && tx.PageWays.TryGetValue(id, out var pw)) { way.When = pw.when; if (pw.line.Length > 0) way.Lines.Add(pw.line); }
                    foreach (var p in pins) if (p.Way == id) way.Pins.Add(p);
                    b.Ways.Add(way);
                }
            b.EndPin = pins.Find(p => p.Way.Length == 0); b.Ending = tx?.PageEnding ?? "";
            // the clock: briefing.page.clock's stations, each worded by its briefing.timeline label; a station that names the target is lit
            if (tx != null)
                foreach (var at in tx.PageClock)
                {
                    var lab = tx.Timeline.Find(x => x.at == at).label ?? ""; float s = Secs(at); if (s < 0f) continue;
                    b.Clock.Add(new Station { At = at, Label = lab, S = s, Lit = b.TargetName.Length > 0 && lab.Contains(b.TargetName.Split(' ')[^1]) });
                }
            b.Sources.Add($"clock: briefing.page.clock ({b.Clock.Count} stations)");
            if (tx != null) foreach (var kv in tx.PageCell) b.CellLines[kv.Key] = kv.Value;
            // the camera after Begin lifts the page: the target's door at the quarter step facing its outward side (the page's camera); else brief.camera
            if (Brief("camera") is JNode cm && cm.Opt("yaw") is JNode y && y.Kind == JKind.Num) b.YawFromOpening = y.Int();
            b.CameraAt = b.TargetAt ?? W(HandoverHost.OpeningFocus);
            return b;
        }
    }
}
