using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 051 (Glyph's spec briefs/051/layout/SPEC.md): the player's settings in one JSON file, `settings.json` in the player's data folder
    /// ({ version, display, audio, camera, gameplay, bindings }). On the first launch after this build every pause-menu toggle's PlayerPrefs key (and the
    /// hints key) is read into the file once, so no saved value is lost, and the keys are not read again. A proof or batch run (any automation flag)
    /// keeps its settings in memory and never writes Sky's file; `-settings &lt;path&gt;` points a run at a file of its own (the front-end proof's round trip).</summary>
    [Serializable]
    public sealed class Settings
    {
        public int version = 1;
        public DisplayS display = new();
        public AudioS audio = new();
        public CameraS camera = new();
        public GameplayS gameplay = new();
        public List<BindingS> bindings = new();

        [Serializable] public sealed class DisplayS { public string windowMode = "borderless"; public int width, height; public int renderScale = 100; public string quality = "balanced"; public bool vsync = true; public int frameCap = 60; public int hudScale = 100; }
        [Serializable] public sealed class AudioS { public int master = 80, music = 60, effects = 80, voices = 90; }
        [Serializable] public sealed class CameraS { public float panSpeed = 18f; public bool edgePan; public float orbitSensitivity = 1f; public bool invertOrbit, trackpadOrbit; public string thirdPersonDistance = "default"; public bool settle = true; }
        [Serializable]
        public sealed class GameplayS
        {
            public int clickScheme = (int)ControlPreset.HoldActs; public bool planTogether = true; public bool hints = true; public int defaultSpeed = 1; public int conversations = (int)ConversationScenes.FirstTime;
            public bool followLock; public bool commanderHoldFire; public bool actionPlate; public bool quickAction; public bool clickActs = true; public int aimMode; public bool tpRightFires = true;
        }
        /// <summary>One changed binding: the action's name, its keys (Key names; keys[0] the primary), its mods.</summary>
        [Serializable] public sealed class BindingS { public string action = ""; public string[] keys = Array.Empty<string>(); public string mods = ""; }

        // ---------------------------------------------------------------- the one instance

        static Settings current;
        /// <summary>The settings in use (loaded, or migrated from PlayerPrefs, on first read).</summary>
        public static Settings Current => current ??= Load();
        /// <summary>True when this run writes the file (a person's launch, the Editor, or `-settings`); a proof keeps its settings in memory.</summary>
        public static bool Persists => !Application.isBatchMode && (GameBoot.ArgValue("-settings") != null || !GameBoot.Automation);
        public static string FilePath => GameBoot.ArgValue("-settings") ?? Path.Combine(Application.persistentDataPath, "settings.json");
        /// <summary>How this run's settings came to be (for the log and the proof): "file", "migrated", "defaults", "memory".</summary>
        public static string Source { get; private set; } = "";
        public static event Action Changed;

        static Settings Load()
        {
            if (!Persists) { Source = "memory"; return Migrate(); }
            try
            {
                if (File.Exists(FilePath))
                {
                    var s = JsonUtility.FromJson<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
                    s.Fix(); Source = "file"; return s;
                }
            }
            catch (Exception e) { Debug.LogWarning($"Settings: {FilePath} would not read ({e.Message}); the defaults and any saved keys are used"); }
            var m = Migrate(); Source = m.migratedKeys > 0 ? "migrated" : "defaults"; m.Save(); return m;
        }

        [NonSerialized] int migratedKeys;
        const string PresetPref = "Suborned.Controls.Preset2", SettlePref = "Suborned.Controls.LeashSettle", TpFirePref = "Suborned.Controls.TpRightFires", PlanPref = "Suborned.Controls.PlanTogether", EdgePref = "Suborned.Controls.EdgePan";
        const string PlatePref = "Suborned.Controls.ActionPlate", ConvoPref = "Suborned.Controls.Conversations", QuickPref = "Suborned.Controls.QuickAction", ClickPref = "Suborned.Controls.ClickActs", AimPref = "Suborned.Controls.AimMode", HoldFirePref = "Suborned.Controls.CommanderHoldFire", FollowPref = "Suborned.Controls.FollowLock";
        const string HintsPref = "Suborned.Play.Hints";
        /// <summary>The PlayerPrefs keys the twelve pause-menu toggles and the hints were kept in (brief 051's table), read once into a new file.</summary>
        public static readonly string[] LegacyKeys = { PresetPref, SettlePref, ConvoPref, PlatePref, QuickPref, ClickPref, AimPref, FollowPref, HoldFirePref, TpFirePref, PlanPref, EdgePref, HintsPref };

        static Settings Migrate() => MigrateFrom(k => PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k) : null);
        /// <summary>A new settings object from the old keys (`read` gives a key's saved int, or null when it was never saved): the front-end proof feeds it
        /// a table instead of the player's PlayerPrefs.</summary>
        public static Settings MigrateFrom(Func<string, int?> read)
        {
            var s = new Settings(); var g = s.gameplay; int n = 0;
            bool B(string k, bool d) { var v = read(k); if (v == null) return d; n++; return v.Value == 1; }
            int I(string k, int d, int lo, int hi) { var v = read(k); if (v == null) return d; n++; return Math.Clamp(v.Value, lo, hi); }
            g.clickScheme = I(PresetPref, g.clickScheme, 0, 2); s.camera.settle = B(SettlePref, s.camera.settle); g.conversations = I(ConvoPref, g.conversations, 0, 2);
            g.actionPlate = B(PlatePref, g.actionPlate); g.quickAction = B(QuickPref, g.quickAction); g.clickActs = B(ClickPref, g.clickActs); g.aimMode = I(AimPref, g.aimMode, 0, 1);
            g.followLock = B(FollowPref, g.followLock); g.commanderHoldFire = B(HoldFirePref, g.commanderHoldFire); g.tpRightFires = B(TpFirePref, g.tpRightFires);
            g.planTogether = B(PlanPref, g.planTogether); s.camera.edgePan = B(EdgePref, s.camera.edgePan); g.hints = B(HintsPref, g.hints);
            s.migratedKeys = n; return s;
        }
        public int MigratedKeys => migratedKeys;

        /// <summary>Out-of-range values from a hand-edited file come back into range.</summary>
        void Fix()
        {
            display ??= new(); audio ??= new(); camera ??= new(); gameplay ??= new(); bindings ??= new();
            display.renderScale = Math.Clamp(display.renderScale, 50, 100); display.hudScale = Math.Clamp(display.hudScale, 80, 150);
            if (display.windowMode is not ("windowed" or "borderless" or "fullscreen")) display.windowMode = "borderless";
            if (display.quality is not ("low" or "balanced" or "high")) display.quality = "balanced";
            if (display.frameCap is not (30 or 60 or 120 or 0)) display.frameCap = 60;
            audio.master = Math.Clamp(audio.master, 0, 100); audio.music = Math.Clamp(audio.music, 0, 100); audio.effects = Math.Clamp(audio.effects, 0, 100); audio.voices = Math.Clamp(audio.voices, 0, 100);
            camera.panSpeed = Mathf.Clamp(camera.panSpeed, 6f, 36f); camera.orbitSensitivity = Mathf.Clamp(camera.orbitSensitivity, 0.25f, 2f);
            if (camera.thirdPersonDistance is not ("close" or "default" or "far")) camera.thirdPersonDistance = "default";
            gameplay.clickScheme = Math.Clamp(gameplay.clickScheme, 0, 2); gameplay.conversations = Math.Clamp(gameplay.conversations, 0, 2); gameplay.aimMode = Math.Clamp(gameplay.aimMode, 0, 1);
            if (gameplay.defaultSpeed is not (1 or 2 or 4 or 8)) gameplay.defaultSpeed = 1;
        }

        /// <summary>Writes the file (a person's run). `notify`: an Options or Controls page changed it, so the listeners (the host's table, the display,
        /// the camera) apply it now; the game's own writes (a toggle key, the range panel) do not notify (they already hold the value).</summary>
        public void Save(bool notify = false)
        {
            Fix();
            if (Persists)
                try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); File.WriteAllText(FilePath, JsonUtility.ToJson(this, true) + "\n"); }
                catch (Exception e) { Debug.LogWarning($"Settings: not saved to {FilePath}: {e.Message}"); }
            if (notify) Changed?.Invoke();
        }

        /// <summary>Reads the file again (the proof's round trip: what a relaunch would see).</summary>
        public static Settings Reload() { current = null; return Current; }

        // ---------------------------------------------------------------- the pages' defaults (Reset page)

        public void ResetPage(string page)
        {
            switch (page)
            {
                case "display": display = new(); break;
                case "audio": audio = new(); break;
                case "camera": camera = new(); break;
                case "gameplay": gameplay = new(); break;
                case "controls": bindings = new(); break;
            }
            Save(true);
        }

        // ---------------------------------------------------------------- applying what is not the host's (display, audio)

        /// <summary>Window mode, resolution, V-sync and frame cap, the 3D render scale, quality and the master volume. A proof's window is CaptureSize's:
        /// the window and resolution are left alone there.</summary>
        public void ApplyDisplay()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            var d = display;
            if (!CaptureSize.Captures && !GameBoot.Arg("-windowed"))
            {
                var mode = d.windowMode switch { "windowed" => FullScreenMode.Windowed, "fullscreen" => FullScreenMode.ExclusiveFullScreen, _ => FullScreenMode.FullScreenWindow };
                var r = Screen.currentResolution; int w = d.width > 0 ? d.width : r.width, h = d.height > 0 ? d.height : r.height;
                if (mode == FullScreenMode.Windowed && d.width <= 0) { w = r.width * 4 / 5 / 2 * 2; h = r.height * 4 / 5 / 2 * 2; }
                if (Screen.fullScreenMode != mode || Screen.width != w || Screen.height != h) Screen.SetResolution(w, h, mode);
            }
            if (!GameBoot.Arg("-novsync")) { QualitySettings.vSyncCount = d.vsync ? 1 : 0; Application.targetFrameRate = d.vsync || d.frameCap == 0 ? -1 : d.frameCap; }
            RenderCap.UserScale = d.renderScale / 100f;
            QualityPreset(d.quality);
            HudToolkit.UserScale = d.hudScale / 100f;
            AudioListener.volume = audio.master / 100f;
        }

        static float? lodBias0; static int? msaa0;
        /// <summary>Low / Balanced / High on the one PC pipeline (Balanced = as shipped, the Air's default): Low draws LODs sooner and drops MSAA, High
        /// keeps detail further. The Mobile quality level is not used (its pipeline asset lacks the game's renderer features).</summary>
        static void QualityPreset(string q)
        {
            lodBias0 ??= QualitySettings.lodBias;
            var urp = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset; if (urp != null) msaa0 ??= urp.msaaSampleCount;
            QualitySettings.lodBias = q switch { "low" => lodBias0.Value * 0.75f, "high" => lodBias0.Value * 1.25f, _ => lodBias0.Value };
            if (urp != null && msaa0.HasValue) urp.msaaSampleCount = q == "low" ? 1 : msaa0.Value;
        }

        // ---------------------------------------------------------------- bindings (PlayControls reads these over its defaults)

        public void SetBinding(PlayAction a, UnityEngine.InputSystem.Key[] keys, string mods)
        {
            bindings.RemoveAll(b => b.action == a.ToString());
            bindings.Add(new BindingS { action = a.ToString(), keys = keys.Select(k => k.ToString()).ToArray(), mods = mods ?? "" });
        }
    }
}
