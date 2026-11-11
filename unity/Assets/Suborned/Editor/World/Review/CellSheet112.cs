using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Suborned.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Suborned.World.Review
{
    /// <summary>Brief 112, a check only (nothing saved but its own stills and report): the cell's Happy Mouse figures as the game draws them. The figure
    /// library is rebuilt (the import's warnings and errors captured), then every cell figure plays each of its own states through its own override
    /// of the shared controller, as HostViews does, with the props that state shows, and is photographed at three moments of the clip (front
    /// three-quarter, 2.4 m tall frame, feet on the line). The report (cell.json) carries what a picture cannot: each clip's length and loop flag,
    /// the lowest point of the body (the soles against y = 0), the Hips over the root (the seat), the exposed bones found, and the triangles drawn.
    /// Batchmode (needs a GPU): -executeMethod Suborned.World.Review.CellSheet112.Run -out &lt;dir&gt; [-only id,id] [-states a,b] [-yaw deg] [-pitch deg]
    /// (the camera's heading: 205 sees the figure from its front left, 270 from its left side, 180 head on; pitch 38 is the play camera's)</summary>
    public static class CellSheet112
    {
        static string Arg(string n) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        const int W = 300, H = 400;

        public static void Run()
        {
            int code = 1;
            try { Inner(); code = 0; } catch (System.Exception e) { Debug.LogException(e); }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        static void Inner()
        {
            var outDir = Path.GetFullPath(Arg("-out") ?? Path.Combine(HarbourWorldBuilder.RepoRoot, "out/scratch/claude/cell112"));
            Directory.CreateDirectory(outDir);
            var only = (Arg("-only") ?? "").Split(',').Where(x => x.Length > 0).ToArray(); var pick = (Arg("-states") ?? "").Split(',').Where(x => x.Length > 0).ToArray();
            var msgs = new List<string>();
            void Hook(string msg, string st, LogType t) { if (t != LogType.Log) msgs.Add($"{t}: {msg.Split('\n')[0]}"); }
            Application.logMessageReceived += Hook;
            FigureLibrary lib;
            try { lib = People.FigureBuild.Build(); } finally { Application.logMessageReceived -= Hook; }
            if (lib == null) throw new InvalidDataException("CellSheet112: no figure library (the figures are not installed)");
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main; cam.orthographic = true; cam.orthographicSize = 1.2f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.36f, 0.38f, 0.42f);
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 40f;
            float yaw = float.Parse(Arg("-yaw") ?? "205", CultureInfo.InvariantCulture), pitch = float.Parse(Arg("-pitch") ?? "8", CultureInfo.InvariantCulture);
            var look = new Vector3(0f, 1.0f, 0f); cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f); cam.transform.position = look - cam.transform.forward * 12f; // the figure faces +Z: by default seen from its front left
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.transform.localScale = Vector3.one * 0.6f; // 6 m: the ground line and the contact shadow
            // the café chair's cushion (tools/figures/seat.json: top 0.474 m, radius 0.198 m, on the chair's point = the figure's root), shown under the seated states
            var chair = GameObject.CreatePrimitive(PrimitiveType.Cylinder); chair.transform.localScale = new Vector3(0.396f, 0.237f, 0.396f); chair.transform.position = new Vector3(0f, 0.237f, 0f); chair.SetActive(false);
            var sb = new StringBuilder("{\n \"importMessages\": [" + string.Join(", ", msgs.Distinct().Select(Q)) + "],\n \"figures\": [\n"); bool firstFig = true;
            foreach (var e in lib.figures)
            {
                if (e.ownStates.Length == 0 || only.Length > 0 && !only.Contains(e.id)) continue;
                var dir = Path.Combine(outDir, e.id); Directory.CreateDirectory(dir);
                var go = (GameObject)Object.Instantiate(e.model); go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); an.runtimeAnimatorController = lib.ControllerFor(e); an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in smrs) { smr.forceMatrixRecalculationPerRender = true; smr.updateWhenOffscreen = true; }
                if (go.GetComponentInChildren<LODGroup>() is LODGroup lg) lg.ForceLOD(0);
                var props = e.propNodes.Select(n => smrs.FirstOrDefault(r => r.name == n) as Renderer ?? go.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == n)).ToArray();
                var bones = new[] { "Head", "Hips", "RightShoulder" }.ToDictionary(n => n, n => go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n));
                var clips = (an.runtimeAnimatorController as AnimatorOverrideController)?.animationClips.GroupBy(c => c.name).ToDictionary(g => g.Key, g => g.First()) ?? new Dictionary<string, AnimationClip>();
                var graph = PlayableGraph.Create(); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var output = AnimationPlayableOutput.Create(graph, "a", an); var ctl = AnimatorControllerPlayable.Create(graph, an.runtimeAnimatorController); output.SetSourcePlayable(ctl); graph.Play();
                if (!firstFig) sb.Append(",\n"); firstFig = false;
                sb.Append($"  {{\"id\": {Q(e.id)}, \"renderers\": {smrs.Length}, \"tris\": {smrs.Sum(r => r.sharedMesh ? r.sharedMesh.triangles.Length / 3 : 0)}, \"verts\": {smrs.Sum(r => r.sharedMesh ? r.sharedMesh.vertexCount : 0)}, \"materials\": {smrs.Sum(r => r.sharedMaterials.Length)}, \"lodGroup\": {(go.GetComponentInChildren<LODGroup>() ? "true" : "false")}, \"optimised\": {(an.hasTransformHierarchy ? "false" : "true")}, \"bones\": {{{string.Join(", ", bones.Select(b => $"{Q(b.Key)}: {(b.Value ? "true" : "false")}"))}}}, \"props\": [{string.Join(", ", e.propNodes.Select((n, i) => $"{{\"node\": {Q(n)}, \"found\": {(props[i] ? "true" : "false")}}}"))}],\n   \"states\": [\n");
                bool firstState = true;
                foreach (var state in e.ownStates)
                {
                    if (pick.Length > 0 && !pick.Contains(state)) continue;
                    clips.TryGetValue(state, out var clip); float len = clip ? clip.length : 0f;
                    chair.SetActive(state.StartsWith("sit") || state == "stand_to_sit");
                    for (int i = 0; i < props.Length; i++) if (props[i]) props[i].enabled = e.propStates[i] == "*" || ("," + e.propStates[i] + ",").Contains("," + state + ",");
                    float low = float.MaxValue; Vector3 hips = default;
                    var phases = new[] { 0.15f, 0.5f, 0.9f };
                    for (int p = 0; p < phases.Length; p++)
                    {
                        ctl.Play(state, 0, phases[p]); graph.Evaluate(0f); graph.Evaluate(0f);
                        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                        cam.targetTexture = rt; cam.Render(); cam.Render(); cam.targetTexture = null; RenderTexture.active = rt;
                        var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
                        File.WriteAllBytes(Path.Combine(dir, $"{state}_{p}.png"), tex.EncodeToPNG()); Object.DestroyImmediate(tex); RenderTexture.ReleaseTemporary(rt);
                        if (p == 1)
                        {
                            var baked = new Mesh();
                            foreach (var smr in smrs) { if (!smr.enabled || props.Contains(smr)) continue; smr.BakeMesh(baked, true); var m = smr.transform.localToWorldMatrix; foreach (var v in baked.vertices) low = Mathf.Min(low, m.MultiplyPoint3x4(v).y); }
                            Object.DestroyImmediate(baked); if (bones["Hips"]) hips = bones["Hips"].position;
                        }
                    }
                    if (!firstState) sb.Append(",\n"); firstState = false;
                    sb.Append($"    {{\"state\": {Q(state)}, \"clip\": {Q(clip ? clip.name : "")}, \"seconds\": {F(len)}, \"loop\": {(clip && clip.isLooping ? "true" : "false")}, \"lowM\": {F(low)}, \"hips\": [{F(hips.x)}, {F(hips.y)}, {F(hips.z)}], \"props\": [{string.Join(", ", props.Where(r => r && r.enabled).Select(r => Q(r.name)))}]}}");
                }
                sb.Append("\n   ]}");
                graph.Destroy(); Object.DestroyImmediate(go);
            }
            sb.Append("\n ]\n}\n");
            File.WriteAllText(Path.Combine(outDir, "cell.json"), sb.ToString());
            Debug.Log($"CellSheet112: stills and cell.json in {outDir}; import messages {msgs.Distinct().Count()}");
        }

        static string Q(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
