using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Suborned.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>Brief 072-G item 3 (Glint, 2026-10-27): the day grade toward the old DecoKit's, shot offscreen in batchmode on the built
    /// HarbourDistrict scene (the Studio is in use: no Editor window, no Game view, no desktop capture). The play camera is posed as Baseline072
    /// does (ortho 14, pitch 38) at office-front yaw 30 and 210 and cell-open yaw 30, rendered through Capture's HDR path at 1920 x 1080.
    /// Unlike After072, the runtime look is installed in memory first, so the frames are what the player shows: AtmosphereLens (lens.json: the
    /// play tier's saturation x(1 + gradePlay.saturationPercent), lift, greens curve, the sun's shadowStrength; the night floor) and LightPhase
    /// (day, or night with the lamp cap around the view's focus). RoadLight's dusk glow (lit windows, the road strip at night) is not run.
    /// A variant (out/scratch/glint/072g/variants/&lt;name&gt;.json, optional) is applied in memory on top of the scene as built: sun colour and
    /// intensity, ambient colours, the day grade profile (a runtime copy: postExposure, contrast, saturation, shadows tint) and lens values
    /// (gradePlay.saturationPercent, sun.shadowStrength). Nothing is saved; everything is put back.
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.Glint072.Cli -variants a,b,c [-night] -out &lt;dir&gt; -logFile &lt;log&gt;
    /// Variant "today" (or a name with no file) = the scene and lens as built.</summary>
    public static class Glint072
    {
        const int W = 1920, H = 1080;
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static void Cli() => HarbourWorldBuilder.Batch(Run);

        static string Arg(string name, string fallback)
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : fallback;
        }

        static void Run()
        {
            var root = HarbourWorldBuilder.RepoRoot;
            var outDir = Path.GetFullPath(Path.Combine(root, Arg("-out", Path.Combine("out", "scratch", "glint", "072g", "shots")))); // relative to the repo, not unity/
            var varDir = Path.Combine(root, "out", "scratch", "glint", "072g", "variants");
            var names = Arg("-variants", "today").Split(',');
            bool night = Array.IndexOf(Environment.GetCommandLineArgs(), "-night") >= 0;
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            var playGo = GameObject.Find("cameras/play") ?? throw new InvalidOperationException("glint072: no cameras/play");
            var cam = playGo.GetComponent<Camera>(); var shadows = playGo.GetComponent<TacticalShadows>(); var fade = playGo.GetComponent<ArchitectureFade>();
            var host = UnityEngine.Object.FindAnyObjectByType<HandoverHost>(FindObjectsInactive.Include);
            float standOff = host ? host.standOff : Baseline072.DefaultStandOff;
            Vector2 cellOpen = Baseline072.CellOpenFallback;
            try { if (host != null) { var w = host.frame.World(HandoverHost.OpeningFocus); if (!float.IsNaN(w.x) && (w.x != 0 || w.z != 0)) cellOpen = new Vector2(w.x, w.z); } } catch { }
            var views = new List<(string id, Vector2 f, float yaw)> { ("office-front", Baseline072.OfficeFront, 30f), ("office-front", Baseline072.OfficeFront, 210f), ("cell-open", cellOpen, 30f) };
            var people = GameObject.Find("people"); if (people) people.SetActive(false);
            if (shadows) { shadows.enabled = false; shadows.enabled = true; }
            if (fade) { fade.enabled = false; fade.enabled = true; }
            foreach (var oc in Camera.allCameras) if (oc != cam && oc.targetTexture == null) oc.enabled = false;

            // the runtime look, installed as AtmosphereLens.Install does (its own Install calls DontDestroyOnLoad, play mode only)
            var phase = UnityEngine.Object.FindAnyObjectByType<LightPhase>(FindObjectsInactive.Include) ?? throw new InvalidOperationException("glint072: no LightPhase");
            typeof(LightPhase).GetMethod("OnEnable", Any).Invoke(phase, null);
            var lens = InstallLens(out var lensGo);
            Vector3 focusNow = Vector3.zero; phase.Focus = () => focusNow;

            var sun = RenderSettings.sun; if (!sun) foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)) if (l.type == LightType.Directional) { sun = l; break; }
            var dayVol = phase.day.grade; var dayProfile0 = dayVol ? dayVol.sharedProfile : null;
            var report = new System.Text.StringBuilder("{\n");
            try
            {
                using var session = new RenderSession();
                Baseline072.Pose(cam, shadows, fade, views[0].f, views[0].yaw, standOff); session.Grab(cam, 64, 64); // the first render after a load comes out flat
                foreach (var name in names)
                {
                    var undo = new List<Action>();
                    try
                    {
                        var file = Path.Combine(varDir, name + ".json");
                        var spec = File.Exists(file) ? J.O(Json.Parse(File.ReadAllText(file))) : null;
                        if (spec != null) Apply(spec, sun, dayVol, lens, undo);
                        SetLensField(lens, "baseSaturation", -999f); // the play grade re-reads the (variant) scene grade on the next camera
                        report.Append($"  \"{name}\": {{ \"file\": {(spec != null ? "true" : "false")}, \"sun\": \"#{ColorUtility.ToHtmlStringRGB(sun.color)}\", \"sunI\": {sun.intensity:F3} }},\n");
                        foreach (var v in views)
                        {
                            focusNow = new Vector3(v.f.x, 0f, v.f.y);
                            Baseline072.Pose(cam, shadows, fade, v.f, v.yaw, standOff);
                            session.Grab(cam, 64, 64);
                            File.WriteAllBytes(Path.Combine(outDir, $"{name}-day-{v.id}-y{v.yaw:000}.png"), session.Grab(cam, W, H).EncodeToPNG());
                        }
                        if (night)
                        {
                            phase.Set(true);
                            try
                            {
                                foreach (var v in views)
                                {
                                    focusNow = new Vector3(v.f.x, 0f, v.f.y);
                                    typeof(LightPhase).GetMethod("Cap", Any).Invoke(phase, new object[] { true });
                                    Baseline072.Pose(cam, shadows, fade, v.f, v.yaw, standOff);
                                    session.Grab(cam, 64, 64);
                                    File.WriteAllBytes(Path.Combine(outDir, $"{name}-night-{v.id}-y{v.yaw:000}.png"), session.Grab(cam, W, H).EncodeToPNG());
                                }
                            }
                            finally { phase.Set(false); }
                        }
                    }
                    finally { for (int i = undo.Count - 1; i >= 0; i--) undo[i](); }
                }
            }
            finally
            {
                if (dayVol) dayVol.sharedProfile = dayProfile0;
                typeof(AtmosphereLens).GetMethod("OnDisable", Any)?.Invoke(lens, null);
                UnityEngine.Object.DestroyImmediate(lensGo);
            }
            report.Append("  \"_\": 0\n}\n");
            File.WriteAllText(Path.Combine(outDir, "glint072.json"), report.ToString());
            Debug.Log($"glint072: {names.Length} variant(s), night {night} → {outDir}");
        }

        static AtmosphereLens InstallLens(out GameObject go)
        {
            var text = Resources.Load<TextAsset>(AtmosphereLens.ResourcePath) ?? throw new InvalidOperationException("glint072: no lens.json");
            var d = JsonUtility.FromJson<AtmosphereLens.Lens>(text.text);
            var why = (string)typeof(AtmosphereLens).GetMethod("Check", Any).Invoke(null, new object[] { d });
            if (why != null) throw new InvalidDataException("glint072: lens.json rejected: " + why);
            typeof(AtmosphereLens).GetProperty("EveningData", Any).SetValue(null, d.evening);
            typeof(AtmosphereLens).GetProperty("NightData", Any).SetValue(null, d.night);
            go = new GameObject("atmosphere lens (glint072)"); go.SetActive(false);
            var a = go.AddComponent<AtmosphereLens>();
            typeof(AtmosphereLens).GetField("data", Any).SetValue(a, d);
            typeof(AtmosphereLens).GetMethod("Build", Any).Invoke(a, null);
            go.SetActive(true);
            typeof(AtmosphereLens).GetMethod("OnEnable", Any).Invoke(a, null); // edit mode does not call it
            return a;
        }

        static void SetLensField(AtmosphereLens a, string field, object v) => typeof(AtmosphereLens).GetField(field, Any).SetValue(a, v);
        static AtmosphereLens.Lens LensData(AtmosphereLens a) => (AtmosphereLens.Lens)typeof(AtmosphereLens).GetField("data", Any).GetValue(a);

        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : throw new InvalidDataException("glint072: colour " + h);

        static void Apply(Dictionary<string, object> v, Light sun, Volume dayVol, AtmosphereLens lens, List<Action> undo)
        {
            if (J.Get(v, "sun") is { } s)
            {
                float i0 = sun.intensity; var c0 = sun.color; undo.Add(() => { sun.intensity = i0; sun.color = c0; });
                if (J.Get(s, "intensity") != null) sun.intensity = J.F(s, "intensity");
                if (J.Get(s, "colour") != null) sun.color = Hex(J.S(s, "colour"));
            }
            if (J.Get(v, "ambient") is { } am)
            {
                var a0 = (RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor);
                undo.Add(() => { RenderSettings.ambientSkyColor = a0.Item1; RenderSettings.ambientEquatorColor = a0.Item2; RenderSettings.ambientGroundColor = a0.Item3; DynamicGI.UpdateEnvironment(); });
                // as light.json builds them: hex with its HSV value scaled
                if (J.Get(am, "sky") is { } k) RenderSettings.ambientSkyColor = AtmosphereLens.Scaled(J.S(k, "hex"), J.F(k, "value"));
                if (J.Get(am, "equator") is { } e) RenderSettings.ambientEquatorColor = AtmosphereLens.Scaled(J.S(e, "hex"), J.F(e, "value"));
                if (J.Get(am, "ground") is { } g) RenderSettings.ambientGroundColor = AtmosphereLens.Scaled(J.S(g, "hex"), J.F(g, "value"));
                DynamicGI.UpdateEnvironment();
            }
            if (J.Get(v, "grade") is { } gr && dayVol)
            {
                var p0 = dayVol.sharedProfile; var p = UnityEngine.Object.Instantiate(p0); p.name = p0.name + " (glint072)";
                dayVol.sharedProfile = p; undo.Add(() => { dayVol.sharedProfile = p0; UnityEngine.Object.DestroyImmediate(p); });
                if (p.TryGet<ColorAdjustments>(out var ca))
                {
                    if (J.Get(gr, "postExposure") != null) ca.postExposure.Override(J.F(gr, "postExposure"));
                    if (J.Get(gr, "contrast") != null) ca.contrast.Override(J.F(gr, "contrast"));
                    if (J.Get(gr, "saturation") != null) ca.saturation.Override(J.F(gr, "saturation"));
                }
                if (J.Get(gr, "shadows") != null && p.TryGet<ShadowsMidtonesHighlights>(out var smh))
                {
                    var c = Hex(J.S(gr, "shadows")); smh.shadows.Override(new Vector4(c.r, c.g, c.b, 0f));
                }
                if (J.Get(gr, "bloom") != null && p.TryGet<Bloom>(out var bl)) bl.intensity.Override(J.F(gr, "bloom"));
            }
            if (J.Get(v, "lens") is { } ln)
            {
                var d = LensData(lens);
                float sp0 = d.gradePlay.saturationPercent, ss0 = d.sun.shadowStrength, lo0 = d.gradePlay.liftOffset;
                undo.Add(() => { d.gradePlay.saturationPercent = sp0; d.sun.shadowStrength = ss0; d.gradePlay.liftOffset = lo0; SetLensField(lens, "baseSaturation", -999f); });
                if (J.Get(ln, "saturationPercent") != null) d.gradePlay.saturationPercent = J.F(ln, "saturationPercent");
                if (J.Get(ln, "shadowStrength") != null) d.sun.shadowStrength = J.F(ln, "shadowStrength");
                if (J.Get(ln, "liftOffset") != null) d.gradePlay.liftOffset = J.F(ln, "liftOffset");
            }
        }
    }
}
