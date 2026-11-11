using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace Suborned.World.Play
{
    /// <summary>Brief 109's authoring test (Sky: "surely using shader graph if means faster iteration is better?"): can the crew author and edit
    /// .shadergraph and .vfx assets reliably without the graph editors? tools/juice/author.ts writes every version as text from Unity's own
    /// templates; this imports each one and says what Unity made of it: compiled or not (and its messages), the properties and their defaults,
    /// whether the importer rewrote the file the agent wrote, and for a shader one pixel drawn with it (a quad with vertex colour (1, 0.5, 0.25),
    /// additive on black), so a tuned value and an added node show up in what draws, not only in the text.
    /// Batch: Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Play.Juice109Authoring.Run -juiceOut [json].</summary>
    public static class Juice109Authoring
    {
        const string Dir = "Assets/Suborned/Generated/Juice109";

        public static void Run()
        {
            string outPath = Arg("-juiceOut") ?? Path.Combine(HarbourWorldBuilder.RepoRoot, "out/scratch/latch/109/authoring.json");
            var msgs = new List<string>(); void OnLog(string c, string st, LogType t) { if (t != LogType.Log) msgs.Add($"{t}: {c.Split('\n')[0]}"); }
            Application.logMessageReceived += OnLog;
            var sb = new StringBuilder("{\n  \"schema\": \"suborned.juice109-authoring/1\",\n  \"unity\": \"" + Application.unityVersion + "\",\n  \"shaderGraphs\": [\n");
            string[] graphs = { "Test/Juice_Glow_v1", "Test/Juice_Glow_v2", "Test/Juice_Glow_v3", "Juice_Glow", "Juice_TiltShift", "Juice_Beam", "Juice_Streak" };
            for (int i = 0; i < graphs.Length; i++)
            {
                string path = $"{Dir}/{graphs[i]}.shadergraph", full = Path.Combine(Application.dataPath, "..", path);
                string before = File.Exists(full) ? File.ReadAllText(full) : null; msgs.Clear();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var sh = AssetDatabase.LoadAssetAtPath<Shader>(path);
                bool err = sh && ShaderUtil.ShaderHasError(sh); var props = new List<string>(); string tint = "", pixel = "";
                if (sh)
                {
                    for (int p = 0; p < sh.GetPropertyCount(); p++) props.Add(sh.GetPropertyName(p));
                    int ti = sh.FindPropertyIndex("_Tint"); if (ti >= 0) tint = V(sh.GetPropertyDefaultVectorValue(ti));
                    foreach (var m in ShaderUtil.GetShaderMessages(sh)) msgs.Add($"{m.severity}: {m.message}");
                    if (!err && graphs[i].Contains("Glow")) pixel = Pixel(sh); // a fullscreen graph draws only in its pass: compiled is the check
                }
                bool rewritten = before != null && File.Exists(full) && File.ReadAllText(full) != before;
                sb.Append($"    {{ \"graph\": \"{graphs[i]}\", \"loaded\": {B(sh)}, \"compileError\": {B(err)}, \"passes\": {(sh ? sh.passCount : 0)}, \"properties\": [{string.Join(", ", props.ConvertAll(Q))}], \"tintDefault\": {Q(tint)}, \"pixel\": {Q(pixel)}, \"fileRewrittenByImporter\": {B(rewritten)}, \"messages\": [{string.Join(", ", msgs.ConvertAll(Q))}] }}{(i < graphs.Length - 1 ? "," : "")}\n");
            }
            sb.Append("  ],\n  \"vfxGraphs\": [\n");
            string[] vfx = { "Test/Juice_Puff_v1", "Test/Juice_Puff_v2" };
            for (int i = 0; i < vfx.Length; i++)
            {
                string path = $"{Dir}/{vfx[i]}.vfx", full = Path.Combine(Application.dataPath, "..", path);
                string before = File.Exists(full) ? File.ReadAllText(full) : null; msgs.Clear();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var a = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                string after = File.Exists(full) ? File.ReadAllText(full) : "";
                bool rewritten = before != null && after != before;
                string count = System.Text.RegularExpressions.Regex.Match(after, @"System\.Single[^\n]*\n\s*m_SerializableObject: (\d+)\n").Groups[1].Value;
                int alive = -1;
                if (a)
                {
                    var go = new GameObject("juice109-vfx-probe") { hideFlags = HideFlags.HideAndDontSave }; var ve = go.AddComponent<VisualEffect>(); ve.visualEffectAsset = a;
                    ve.Reinit(); ve.Simulate(1f / 60f, 10); alive = ve.aliveParticleCount; Object.DestroyImmediate(go);
                }
                var exposed = new List<VFXExposedProperty>(); if (a) a.GetExposedProperties(exposed);
                sb.Append($"    {{ \"graph\": \"{vfx[i]}\", \"loaded\": {B(a)}, \"burstCountInFile\": {Q(count)}, \"exposed\": {exposed.Count}, \"aliveAfterSimulate\": {alive}, \"fileRewrittenByImporter\": {B(rewritten)}, \"messages\": [{string.Join(", ", msgs.ConvertAll(Q))}] }}{(i < vfx.Length - 1 ? "," : "")}\n");
            }
            sb.Append("  ],\n");
            int made = 0; try { made = Juice109Assets.Ensure(); } catch (System.Exception e) { msgs.Add("Juice109Assets: " + e.Message); }
            sb.Append($"  \"gameAssetsWritten\": {made}\n}}\n");
            Application.logMessageReceived -= OnLog;
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)); File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"Juice109Authoring: wrote {outPath}");
        }

        /// <summary>One pixel of a quad drawn with the shader (white map, vertex colour (1, 0.5, 0.25, 1)), additive over black, linear.</summary>
        static string Pixel(Shader sh)
        {
            var rt = new RenderTexture(32, 32, 24, RenderTextureFormat.ARGBHalf) { name = "juice109-probe" }; rt.Create();
            var mat = new Material(sh); if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            var mesh = new Mesh { vertices = new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) }, uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up }, triangles = new[] { 0, 2, 1, 0, 3, 2 } };
            mesh.colors = new[] { new Color(1f, 0.5f, 0.25f, 1f), new Color(1f, 0.5f, 0.25f, 1f), new Color(1f, 0.5f, 0.25f, 1f), new Color(1f, 0.5f, 0.25f, 1f) };
            var go = new GameObject("juice109-probe-quad") { hideFlags = HideFlags.HideAndDontSave, layer = 31 }; go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.transform.position = new Vector3(0, -5000, 0);
            var cg = new GameObject("juice109-probe-cam") { hideFlags = HideFlags.HideAndDontSave }; var cam = cg.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 0.5f; cam.cullingMask = 1 << 31; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0, -5000, -2); cam.nearClipPlane = 0.1f; cam.farClipPlane = 10f; cam.targetTexture = rt; cam.enabled = false;
            string s;
            try
            {
                cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt; var t = new Texture2D(32, 32, TextureFormat.RGBAHalf, false); t.ReadPixels(new Rect(0, 0, 32, 32), 0, 0); t.Apply(); RenderTexture.active = prev;
                s = V(t.GetPixel(16, 16)); Object.DestroyImmediate(t);
            }
            catch (System.Exception e) { s = "render failed: " + e.Message; }
            Object.DestroyImmediate(go); Object.DestroyImmediate(cg); Object.DestroyImmediate(mat); Object.DestroyImmediate(mesh); rt.Release(); Object.DestroyImmediate(rt);
            return s;
        }

        static string V(Vector4 v) => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.000} {1:0.000} {2:0.000} {3:0.000}", v.x, v.y, v.z, v.w);
        static string V(Color c) => V((Vector4)c);
        static string B(bool b) => b ? "true" : "false";
        static string B(Object o) => o ? "true" : "false";
        static string Q(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ") + "\"";
        static string Arg(string name) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
