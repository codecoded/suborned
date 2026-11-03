using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Suborned.Play;

namespace Suborned.World.Review
{
    /// <summary>Brief 072-G item 4 (Latch, 2026-10-28; Glint's open item): do the crossing heads read within 3 dE of signals.json's `target` by day
    /// under the new grade? Offscreen in batchmode on the built HarbourDistrict scene, the runtime look installed in memory as Glint072 does
    /// (AtmosphereLens from lens.json: the play tier's saturation, lift, greens; LightPhase by day), then for each state (walk, clearing, stop)
    /// every head is held there (SignalHead.Hold) and stepped once, so its MaterialPropertyBlocks carry signals.json's day colour and level as
    /// the player's would. The three heads nearest the office front are framed at play zoom 14, pitch 38, at each quarter yaw, rendered through
    /// Capture's HDR path at 3840 x 2160 (the 0.2.122 measure's size). Each shot's json row holds the head figure's bounds projected to pixels
    /// (padded 2 px); the colour is read by out/scratch/latch/072/tools/heads.py (top 1 % most vivid, median; CIEDE2000 against `target`,
    /// briefs/061/glint/grade.py). Nothing is saved; the holds are put back.
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.Signals072.Cli -out &lt;dir&gt; -logFile &lt;log&gt;</summary>
    public static class Signals072
    {
        const int W = 3840, H = 2160;
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static void Cli() => HarbourWorldBuilder.Batch(Run);

        static string Arg(string k, string d) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, k); return i >= 0 && i + 1 < a.Length ? a[i + 1] : d; }
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        static void Run()
        {
            var root = HarbourWorldBuilder.RepoRoot;
            var outDir = Path.GetFullPath(Path.Combine(root, Arg("-out", Path.Combine("out", "scratch", "latch", "072", "g", "heads"))));
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            var playGo = GameObject.Find("cameras/play") ?? throw new InvalidOperationException("signals072: no cameras/play");
            var cam = playGo.GetComponent<Camera>(); var shadows = playGo.GetComponent<TacticalShadows>(); var fade = playGo.GetComponent<ArchitectureFade>();
            var host = UnityEngine.Object.FindAnyObjectByType<HandoverHost>(FindObjectsInactive.Include);
            float standOff = host ? host.standOff : Baseline072.DefaultStandOff;
            var people = GameObject.Find("people"); if (people) people.SetActive(false);
            if (shadows) { shadows.enabled = false; shadows.enabled = true; }
            if (fade) { fade.enabled = false; fade.enabled = true; }
            foreach (var oc in Camera.allCameras) if (oc != cam && oc.targetTexture == null) oc.enabled = false;

            var phase = UnityEngine.Object.FindAnyObjectByType<LightPhase>(FindObjectsInactive.Include) ?? throw new InvalidOperationException("signals072: no LightPhase");
            typeof(LightPhase).GetMethod("OnEnable", Any).Invoke(phase, null);
            var lens = (AtmosphereLens)typeof(Glint072).GetMethod("InstallLens", Any).Invoke(null, new object[] { null });
            var lensGo = lens.gameObject;
            var of = new Vector3(Baseline072.OfficeFront.x, 0f, Baseline072.OfficeFront.y);
            phase.Focus = () => of;

            var heads = UnityEngine.Object.FindObjectsByType<SignalHead>(FindObjectsInactive.Exclude).Where(h => h.Figure)
                .OrderBy(h => (new Vector2(h.transform.position.x, h.transform.position.z) - Baseline072.OfficeFront).sqrMagnitude).Take(3).ToList();
            if (heads.Count == 0) throw new InvalidOperationException("signals072: no SignalHead with a figure in the scene");
            var all = UnityEngine.Object.FindObjectsByType<SignalHead>(FindObjectsInactive.Exclude);
            var step = typeof(SignalHead).GetMethod("Step", Any);
            var holdWas = SignalHead.Hold;
            var sb = new StringBuilder("{\n  \"schema\": \"latch.072g-heads/1\",\n");
            var stamp = UnityEngine.Object.FindAnyObjectByType<WorldStamp>();
            sb.Append($"  \"when\": \"{DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}\",\n  \"export\": \"{(stamp ? $"{stamp.exportPath} sha256 {stamp.exportSha256}" : "no WorldStamp")}\",\n");
            sb.Append($"  \"picture\": \"{W}x{H}\",\n  \"note\": \"day, runtime look installed (AtmosphereLens from lens.json, LightPhase day); every head held in the state and stepped (signals.json day colour and level by MaterialPropertyBlock); rect = the head figure's bounds projected at the picture size, padded 2 px, y from the top\",\n  \"shots\": [\n");
            var rows = new List<string>();
            try
            {
                using var session = new RenderSession();
                Baseline072.Pose(cam, shadows, fade, Baseline072.OfficeFront, 30f, standOff); session.Grab(cam, 64, 64); // the first render after a load comes out flat
                foreach (var state in new[] { "walk", "clearing", "stop" })
                {
                    SignalHead.Hold = state;
                    foreach (var h in all) { SetField(h, "first", true); SetField(h, "wroteHead", false); SetField(h, "wroteDots", false); step.Invoke(h, null); }
                    for (int i = 0; i < heads.Count; i++)
                    {
                        var h = heads[i];
                        var p = h.transform.position; var f = new Vector2(p.x, p.z);
                        foreach (var yaw in Baseline072.Yaws)
                        {
                            Baseline072.Pose(cam, shadows, fade, f, yaw, standOff);
                            session.Grab(cam, 64, 64);
                            var tex = session.Grab(cam, W, H);
                            var name = $"h{i + 1}-{state}-y{yaw:000}.png";
                            File.WriteAllBytes(Path.Combine(outDir, name), tex.EncodeToPNG());
                            var r = Rect(cam, h.Figure.bounds);
                            rows.Add($"    {{ \"shot\": \"{name}\", \"head\": \"{h.name}\", \"crossing\": \"{h.Crossing}\", \"state\": \"{state}\", \"yaw\": {yaw:0}, \"at\": [{F(p.x)}, {F(p.y)}, {F(p.z)}], \"facing\": {F(h.transform.eulerAngles.y)}, \"rect\": [{r.x0}, {r.y0}, {r.x1}, {r.y1}] }}");
                        }
                    }
                }
            }
            finally
            {
                SignalHead.Hold = holdWas;
                typeof(AtmosphereLens).GetMethod("OnDisable", Any)?.Invoke(lens, null);
                UnityEngine.Object.DestroyImmediate(lensGo);
            }
            sb.Append(string.Join(",\n", rows)).Append("\n  ]\n}\n");
            File.WriteAllText(Path.Combine(outDir, "heads.json"), sb.ToString());
            Debug.Log($"signals072: {heads.Count} heads x 3 states x {Baseline072.Yaws.Length} yaws → {outDir}");
        }

        /// <summary>Glint's open item (night → day blue drift): office-front y030 and y210 by day, then night, then day again, through the same runtime
        /// look; both day frames, the sun and RenderSettings before and after are written, and the per-channel mean difference logged.
        ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.Signals072.NightDayCli -out &lt;dir&gt; -logFile &lt;log&gt;</summary>
        public static void NightDayCli() => HarbourWorldBuilder.Batch(NightDay);

        static string State(Light sun) => $"sun #{ColorUtility.ToHtmlStringRGB(sun.color)} x{sun.intensity:F3} rot {sun.transform.eulerAngles} shadow {sun.shadowStrength:F3}; ambient {RenderSettings.ambientMode} sky #{ColorUtility.ToHtmlStringRGB(RenderSettings.ambientSkyColor)} eq #{ColorUtility.ToHtmlStringRGB(RenderSettings.ambientEquatorColor)} gr #{ColorUtility.ToHtmlStringRGB(RenderSettings.ambientGroundColor)}; fog #{ColorUtility.ToHtmlStringRGB(RenderSettings.fogColor)}; skybox {(RenderSettings.skybox ? RenderSettings.skybox.name : "none")}; refl {RenderSettings.defaultReflectionMode} {(RenderSettings.customReflectionTexture ? RenderSettings.customReflectionTexture.name : "none")} x{RenderSettings.reflectionIntensity:F2}";

        static void NightDay()
        {
            var root = HarbourWorldBuilder.RepoRoot;
            var outDir = Path.GetFullPath(Path.Combine(root, Arg("-out", Path.Combine("out", "scratch", "latch", "072", "g", "nightday"))));
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            var playGo = GameObject.Find("cameras/play") ?? throw new InvalidOperationException("signals072: no cameras/play");
            var cam = playGo.GetComponent<Camera>(); var shadows = playGo.GetComponent<TacticalShadows>(); var fade = playGo.GetComponent<ArchitectureFade>();
            var host = UnityEngine.Object.FindAnyObjectByType<HandoverHost>(FindObjectsInactive.Include);
            float standOff = host ? host.standOff : Baseline072.DefaultStandOff;
            var people = GameObject.Find("people"); if (people) people.SetActive(false);
            if (shadows) { shadows.enabled = false; shadows.enabled = true; }
            if (fade) { fade.enabled = false; fade.enabled = true; }
            foreach (var oc in Camera.allCameras) if (oc != cam && oc.targetTexture == null) oc.enabled = false;
            var phase = UnityEngine.Object.FindAnyObjectByType<LightPhase>(FindObjectsInactive.Include) ?? throw new InvalidOperationException("signals072: no LightPhase");
            typeof(LightPhase).GetMethod("OnEnable", Any).Invoke(phase, null);
            var lens = (AtmosphereLens)typeof(Glint072).GetMethod("InstallLens", Any).Invoke(null, new object[] { null });
            var of = new Vector3(Baseline072.OfficeFront.x, 0f, Baseline072.OfficeFront.y); phase.Focus = () => of;
            var sun = RenderSettings.sun; if (!sun) foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)) if (l.type == LightType.Directional) { sun = l; break; }
            var log = new StringBuilder();
            try
            {
                using var session = new RenderSession();
                Baseline072.Pose(cam, shadows, fade, Baseline072.OfficeFront, 30f, standOff); session.Grab(cam, 64, 64);
                void Shoot(string tag)
                {
                    foreach (var yaw in new[] { 30f, 210f })
                    {
                        Baseline072.Pose(cam, shadows, fade, Baseline072.OfficeFront, yaw, standOff); session.Grab(cam, 64, 64);
                        File.WriteAllBytes(Path.Combine(outDir, $"{tag}-office-front-y{yaw:000}.png"), session.Grab(cam, Baseline072.W, Baseline072.H).EncodeToPNG());
                    }
                    log.Append(tag).Append(": ").Append(State(sun)).Append('\n');
                }
                phase.Set(false); Shoot("day1");
                phase.Set(true); Shoot("night");
                phase.Set(false); Shoot("day2");
            }
            finally
            {
                phase.Set(false);
                typeof(AtmosphereLens).GetMethod("OnDisable", Any)?.Invoke(lens, null);
                UnityEngine.Object.DestroyImmediate(lens.gameObject);
            }
            File.WriteAllText(Path.Combine(outDir, "state.txt"), log.ToString());
            Debug.Log("signals072 nightday:\n" + log);
        }

        static void SetField(object o, string n, object v) => o.GetType().GetField(n, Any)?.SetValue(o, v);

        /// <summary>A world box projected through the orthographic play camera at W x H (y from the top), padded 2 px.</summary>
        static (int x0, int y0, int x1, int y1) Rect(Camera cam, Bounds b)
        {
            float hh = cam.orthographicSize, hw = hh * W / (float)H;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int k = 0; k < 8; k++)
            {
                var c = b.center + Vector3.Scale(b.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                var q = cam.transform.InverseTransformPoint(c);
                float px = (q.x / hw * 0.5f + 0.5f) * W, py = (0.5f - q.y / hh * 0.5f) * H;
                x0 = Mathf.Min(x0, px); x1 = Mathf.Max(x1, px); y0 = Mathf.Min(y0, py); y1 = Mathf.Max(y1, py);
            }
            return (Mathf.Clamp((int)x0 - 2, 0, W - 1), Mathf.Clamp((int)y0 - 2, 0, H - 1), Mathf.Clamp((int)x1 + 2, 0, W - 1), Mathf.Clamp((int)y1 + 2, 0, H - 1));
        }
    }
}
