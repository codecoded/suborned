using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>Brief 025 item 1, the camera lab (Sky 2026-10-19: "would love a debug on this to try various options"; "2.5D zoom has been too
    /// limited for me and third person maybe too down to the ground or close"). P opens a panel of live sliders and switches for both views
    /// (HostCameraLab's CamPreset): the overhead view's zoom range, pitch, free or quarter yaw, orthographic or perspective and field of view; the
    /// follow view's distance and range, height, pitch, shoulder, field of view, orbit on a left-drag, collision and smoothing; the wheel stepped
    /// or proportional (a trackpad scroll moves the zoom by its size). Named presets save as JSON: in the Editor in briefs/025/presets/ (the
    /// repo), in a build beside the app ("Suborned Camera Presets", else the player's data folder when the app runs from a read-only place);
    /// the build carries briefs/025/presets in StreamingAssets/presets. One preset can be the default (default.txt), loaded at start.
    /// A debug state: closed by default, never in the proof's shots or the HUD budget's play shots; the street takes no click under it, and
    /// no key while a name is being typed.</summary>
    [DefaultExecutionOrder(-60)]
    public sealed class CameraLab : MonoBehaviour
    {
        public static bool Open { get; private set; }
        static Rect window = new(0, 0, 440, 640); static float scale = 1f;
        /// <summary>The panel is under this screen point (Input System coordinates, origin bottom left): the street takes no click there.</summary>
        public static bool Blocks(Vector2 screen) => Open && new Rect(window.x * scale, window.y * scale, window.width * scale, window.height * scale).Contains(new Vector2(screen.x, Screen.height - screen.y));
        /// <summary>A preset name is being typed: the host reads no keys.</summary>
        public static bool Typing => Open && GUIUtility.keyboardControl != 0;

        HandoverHost host; Vector2 scroll; string presetName = "", note = ""; string[] names = Array.Empty<string>(); string defaultName = "";
        bool started;

        void Awake() => host = GetComponent<HandoverHost>();

        void Update()
        {
            if (host == null) return;
            if (!started && host.View != null) { started = true; Refresh(); var d = LoadDefault(); if (d != null) { host.CamUse(d); note = $"default preset: {d.name}"; } }
            var kb = Keyboard.current;
            if (!Typing && host.controls.Down(kb, PlayAction.CameraLab)) { Open = !Open; GUIUtility.keyboardControl = 0; if (Open) Refresh(); }
        }

        // ---------------------------------------------------------------- presets

        static string Safe(string n) { var s = new string((n ?? "").Trim().Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or ' ' ? c : '-').ToArray()).Trim(); return s.Length == 0 ? "preset" : s; }

        /// <summary>Where presets are written: the repo's briefs/025/presets (Editor); beside the app, else the player's data folder (build).</summary>
        public static string WriteDir()
        {
            if (Application.isEditor) return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "briefs", "025", "presets"));
            var beside = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Suborned Camera Presets")); // dataPath = …/The Suborned.app/Contents
            try { Directory.CreateDirectory(beside); var probe = Path.Combine(beside, ".write"); File.WriteAllText(probe, ""); File.Delete(probe); return beside; }
            catch { return Path.Combine(Application.persistentDataPath, "camera-presets"); } // a translocated (quarantined) app runs read-only
        }
        /// <summary>The presets the build carries (briefs/025/presets at build time), read-only.</summary>
        static string BundledDir => Path.Combine(Application.streamingAssetsPath, "presets");
        static IEnumerable<string> Dirs() { yield return WriteDir(); if (!Application.isEditor) yield return BundledDir; }

        void Refresh()
        {
            var all = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in Dirs()) if (Directory.Exists(d)) foreach (var f in Directory.GetFiles(d, "*.json")) all.Add(Path.GetFileNameWithoutExtension(f));
            names = all.ToArray(); defaultName = ReadDefaultName() ?? "";
        }

        static string ReadDefaultName() { foreach (var d in Dirs()) { var f = Path.Combine(d, "default.txt"); if (File.Exists(f)) { var n = File.ReadAllText(f).Trim(); if (n.Length > 0) return n; } } return null; }

        static CamPreset Load(string name)
        {
            foreach (var d in Dirs())
            {
                var f = Path.Combine(d, Safe(name) + ".json"); if (!File.Exists(f)) continue;
                try { var p = JsonUtility.FromJson<CamPreset>(File.ReadAllText(f)); if (p != null) { p.name = name; return p; } } catch (Exception e) { Debug.LogWarning($"CameraLab: {f}: {e.Message}"); }
            }
            return null;
        }

        static CamPreset LoadDefault() { var n = ReadDefaultName(); return n == null ? null : Load(n); }

        void Save(string name)
        {
            var dir = WriteDir(); Directory.CreateDirectory(dir);
            var p = host.Cam.Clone(); p.name = Safe(name); if (host.View) p.overhead.zoom = host.View.orthographicSize; // the zoom as it stands
            var file = Path.Combine(dir, p.name + ".json"); File.WriteAllText(file, JsonUtility.ToJson(p, true) + "\n");
            note = $"saved {file}"; Refresh();
        }

        void SetDefault(string name) { var dir = WriteDir(); Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir, "default.txt"), name + "\n"); note = $"default: {name}"; Refresh(); }
        void ClearDefault() { var f = Path.Combine(WriteDir(), "default.txt"); if (File.Exists(f)) File.Delete(f); note = "default: built-in"; Refresh(); }

        // ---------------------------------------------------------------- the panel (IMGUI: a debug tool, not the kit's HUD)

        void OnGUI()
        {
            if (!Open || host == null) return;
            scale = Mathf.Max(1f, Mathf.Round(Screen.height / 1080f)); var m = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            window.x = Screen.width / scale - window.width - 12; window.y = 84; window.height = Mathf.Min(760, Screen.height / scale - 96); // below the clock and the response pill: the clock stays readable (and no HUD shows through its words)
            var bg = GUI.color; GUI.color = new Color(0.06f, 0.13f, 0.13f, 0.94f); GUI.DrawTexture(window, Texture2D.whiteTexture); GUI.color = bg; // an opaque card: the default skin's window is see-through over the bright street
            window = GUI.Window(0x5ab, window, Draw, "Camera lab (P)");
            GUI.matrix = m;
        }

        void Draw(int id)
        {
            var c = host.Cam; var o = c.overhead; var f = c.follow; var w = c.wheel; var cam = host.View; bool changed = false, zoom = false;
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label($"<b>{(host.Following ? "Follow view" : "Overhead view")}</b>  yaw {host.Yaw:0}°  zoom {(cam ? cam.orthographicSize : 0):0.0}  scroll {host.LastScroll:0.###}", Rich);
            if (!host.Driving && GUILayout.Button(host.Following ? "Back to the overhead view" : "Follow view only (no control)")) host.FollowToggle();

            Head("Overhead");
            float z = cam ? cam.orthographicSize : o.zoom; float nz = Slider("zoom (half-height, m)", z, Mathf.Min(o.zoomMin, 1f), Mathf.Max(o.zoomMax, z), "0.0");
            if (!Mathf.Approximately(nz, z)) { o.zoom = nz; zoom = true; }
            changed |= Edit(ref o.zoomMin, Slider("zoom min", o.zoomMin, 1f, 60f, "0.0"));
            changed |= Edit(ref o.zoomMax, Slider("zoom max", o.zoomMax, 2f, 200f, "0.0"));
            changed |= Edit(ref o.pitch, Slider("pitch (°)", o.pitch, 10f, 89f, "0"));
            changed |= Edit(ref o.freeYaw, GUILayout.Toggle(o.freeYaw, " free yaw (Alt + middle-drag, no snap)"));
            changed |= Edit(ref o.perspective, GUILayout.Toggle(o.perspective, " perspective"));
            if (o.perspective) changed |= Edit(ref o.fov, Slider("field of view (°)", o.fov, 5f, 90f, "0"));

            Head("Follow / third person");
            changed |= Edit(ref f.distance, Slider("distance (m)", f.distance, 0.5f, 40f, "0.0"));
            changed |= Edit(ref f.distanceMin, Slider("distance min", f.distanceMin, 0.5f, 20f, "0.0"));
            changed |= Edit(ref f.distanceMax, Slider("distance max", f.distanceMax, 1f, 80f, "0.0"));
            changed |= Edit(ref f.height, Slider("height (m)", f.height, 0f, 10f, "0.00"));
            changed |= Edit(ref f.rise, Slider("rise (x distance)", f.rise, 0f, 1.5f, "0.00"));
            bool setPitch = f.pitch >= 0f; bool np = GUILayout.Toggle(setPitch, " set pitch (off: look down the street)");
            if (np != setPitch) { f.pitch = np ? 20f : -1f; changed = true; }
            if (f.pitch >= 0f) changed |= Edit(ref f.pitch, Slider("pitch (°)", f.pitch, 0f, 89f, "0"));
            else { changed |= Edit(ref f.lookAhead, Slider("look ahead (m)", f.lookAhead, 0f, 30f, "0.0")); changed |= Edit(ref f.lookHeight, Slider("look height (m)", f.lookHeight, 0f, 5f, "0.00")); }
            changed |= Edit(ref f.shoulder, Slider("shoulder (m, + right)", f.shoulder, -2f, 2f, "0.00"));
            changed |= Edit(ref f.fov, Slider("field of view (°)", f.fov, 20f, 100f, "0"));
            changed |= Edit(ref f.orbitDrag, GUILayout.Toggle(f.orbitDrag, " orbit on left-drag"));
            if (f.orbitDrag) changed |= Edit(ref f.orbitDegPerPixel, Slider("orbit (°/px)", f.orbitDegPerPixel, 0.05f, 1f, "0.00"));
            changed |= Edit(ref f.collision, GUILayout.Toggle(f.collision, " collision"));
            changed |= Edit(ref f.smoothing, Slider("smoothing (s)", f.smoothing, 0f, 1f, "0.00"));

            DriveSection();

            Head("Wheel and trackpad");
            changed |= Edit(ref w.proportional, GUILayout.Toggle(w.proportional, " proportional wheel (trackpad)"));
            if (w.proportional) changed |= Edit(ref w.rate, Slider("rate", w.rate, 0.005f, 0.5f, "0.000"));
            else changed |= Edit(ref w.step, Slider("step (m a notch)", w.step, 0.1f, 5f, "0.0"));

            Head("Presets");
            GUILayout.BeginHorizontal(); GUILayout.Label("name", GUILayout.Width(40)); presetName = GUILayout.TextField(presetName ?? "", GUILayout.MinWidth(150));
            if (GUILayout.Button("Save", GUILayout.Width(60)) && presetName.Trim().Length > 0) { Save(presetName); GUIUtility.keyboardControl = 0; }
            GUILayout.EndHorizontal();
            foreach (var n in names)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label((string.Equals(n, defaultName, StringComparison.OrdinalIgnoreCase) ? "★ " : "   ") + n, GUILayout.MinWidth(150));
                if (GUILayout.Button("Load", GUILayout.Width(50))) { var p = Load(n); if (p != null) { host.CamUse(p); presetName = n; note = $"loaded {n}"; } }
                if (GUILayout.Button("Default", GUILayout.Width(60))) SetDefault(n);
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Built-in")) { host.CamUse(new CamPreset()); note = "built-in view"; }
            if (GUILayout.Button("Default: built-in")) ClearDefault();
            GUILayout.EndHorizontal();
            GUILayout.Label(note, Small); GUILayout.Label("folder: " + WriteDir(), Small);
            GUILayout.Label(BuildStamp.Line, Small);
            GUILayout.EndScrollView();
            if (zoom && cam) { cam.orthographicSize = o.zoom; o.zoomMin = Mathf.Min(o.zoomMin, o.zoom); o.zoomMax = Mathf.Max(o.zoomMax, o.zoom); host.CamApply(); }
            else if (changed) host.CamApply();
        }

        static readonly string[] BusyWays = { "rulebook", "cancel", "hold" }, ReleaseWays = { "rulebook", "stand", "resume" };

        /// <summary>Brief 025: take control, explore, and the driven agent's playground switches (pace, onBusy, onRelease, the people gap). The
        /// four switches are the run's start state (hashed into it): they apply from a restart, which the panel offers when they differ.</summary>
        void DriveSection()
        {
            var lab = host.Lab; var g = host.Game; var R = g?.R.Drive;
            Head("Take control (playground)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(host.Driving ? "Let go (T)" : "Take control (T)")) host.DriveToggle();
            bool ex = GUILayout.Toggle(host.Exploring, " explore (clock held)"); if (ex != host.Exploring) host.ExploreSet(ex);
            GUILayout.EndHorizontal();
            if (host.Driving) GUILayout.Label($"driving the {Suborned.Sim.Op.Name((Suborned.Sim.OpId)host.DriveAgent)}{(host.Exploring ? " (explore: view only)" : "")} · corrections {host.DriveCorrections}", Small);
            int pace = lab.pace > 0 ? lab.pace : 1000;
            float px = Slider("pace (x rulebook)", pace / 1000f, 0.25f, 4f, "0.00"); int np = Mathf.RoundToInt(px * 20f) * 50; if (np != pace) lab.pace = Mathf.Clamp(np, 250, 4000);
            GUILayout.BeginHorizontal(); GUILayout.Label($"busy agent ({R?.OnBusy ?? "?"})", GUILayout.Width(140));
            int bi = Mathf.Max(0, Array.IndexOf(BusyWays, lab.onBusy.Length == 0 ? "rulebook" : lab.onBusy)); int nb = GUILayout.Toolbar(bi, BusyWays); if (nb != bi) lab.onBusy = nb == 0 ? "" : BusyWays[nb];
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.Label($"on release ({R?.OnRelease ?? "?"})", GUILayout.Width(140));
            int ri = Mathf.Max(0, Array.IndexOf(ReleaseWays, lab.onRelease.Length == 0 ? "rulebook" : lab.onRelease)); int nr = GUILayout.Toolbar(ri, ReleaseWays); if (nr != ri) lab.onRelease = nr == 0 ? "" : ReleaseWays[nr];
            GUILayout.EndHorizontal();
            bool own = lab.gap >= 0; bool no = GUILayout.Toggle(own, $" set the people gap (rulebook {R?.GapCm ?? 0} cm)"); if (no != own) lab.gap = no ? R?.GapCm ?? 40 : -1;
            if (lab.gap >= 0) lab.gap = Mathf.RoundToInt(Slider("gap (cm, 0 off)", lab.gap, 0f, 150f, "0"));
            if (host.LabPending)
            {
                GUILayout.Label("These switches are the run's start state: they apply from a restart.", Small);
                if (GUILayout.Button("Restart with these")) host.Begin();
            }
            else if (g != null) GUILayout.Label($"this run: pace {(g.Start.DrivePace ?? 1000) / 1000f:0.00}x · busy {g.Start.DriveOnBusy ?? R?.OnBusy} · release {g.Start.DriveOnRelease ?? R?.OnRelease} · gap {g.Start.DriveGap ?? R?.GapCm} cm", Small);
            GUILayout.Label("Fights stay hit chances (Syndicate-simple): no aiming while driven.", Small);
        }

        static GUIStyle rich, small, head;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true };
        static GUIStyle Small => small ??= new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true };
        static void Head(string s) { GUILayout.Space(8); GUILayout.Label($"<b>{s}</b>", head ??= new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 }); }

        static float Slider(string label, float v, float min, float max, string fmt)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(label, GUILayout.Width(140));
            float r = GUILayout.HorizontalSlider(v, min, max, GUILayout.Width(190)); GUILayout.Label(r.ToString(fmt), GUILayout.Width(50));
            GUILayout.EndHorizontal(); return r;
        }
        static bool Edit(ref float field, float v) { if (Mathf.Approximately(field, v)) return false; field = v; return true; }
        static bool Edit(ref bool field, bool v) { if (field == v) return false; field = v; return true; }
    }
}
