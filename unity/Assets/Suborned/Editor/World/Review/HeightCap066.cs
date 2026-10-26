using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Suborned.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Brief 066 (Datum's height-cap mock), scratch renders only: opens a scratch scene built from one of Datum's two worlds
    /// (`-scene Assets/Scenes/HarbourDistrict-today066.unity` or `-cap066`), stages the Handover's people from Forma's figure library on
    /// the points out/scratch/latch/r066/points.*.json gives (computed from mission.json with tools/plan/civic.ts), and renders the play
    /// camera (orthographic, zoom 14, pitch 38, 1920x1080, the scene's day look) at yaws 30/120/210/300 on the café front and the Records
    /// Office front, plus a follow-camera frame per front (HostFollow's defaults: 5.5 m back, 3.06 m up, look 8 m ahead at 1.35 m, fov 55,
    /// far 350). Per frame it writes the frustum count: renderers drawn (enabled, in the camera's mask, bounds in the frustum, the LOD the
    /// frame selects) and their triangles. Never saves the scene; never opens HarbourDistrict.unity.
    /// Batchmode (needs a GPU): -executeMethod Suborned.World.Review.HeightCap066.Render -scene &lt;scratch&gt; -tag today|cap -points &lt;json&gt; -out &lt;dir&gt;</summary>
    public static class HeightCap066
    {
        static string Arg(string n) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
        static readonly int[] Yaws = { 30, 120, 210, 300 };
        const float MaxStand = 600f;

        public static void Render() => HarbourWorldBuilder.Batch(() =>
        {
            var scenePath = Arg("-scene"); if (scenePath == null || scenePath.EndsWith("HarbourDistrict.unity")) throw new IOException("066: a scratch -scene only, never the scene of record");
            string tag = Arg("-tag") ?? "scratch", outDir = Path.GetFullPath(Arg("-out") ?? Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs/066/latch"));
            var pts = JsonUtility.FromJson<Points>(File.ReadAllText(Arg("-points")));
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(outDir);
            var play = GameObject.Find("cameras/play")?.GetComponent<Camera>() ?? throw new FileNotFoundException("066: no cameras/play in the scene");
            var lib = AssetDatabase.LoadAssetAtPath<FigureLibrary>("Assets/Suborned/People/Generated/FigureLibrary.asset") ?? throw new FileNotFoundException("066: no FigureLibrary");
            float groundY = play.transform.position.y + play.transform.forward.y * play.GetComponent<TacticalShadows>().focusDepth; // the street datum under the export's focus
            var rig = Object.Instantiate(play.gameObject); rig.name = "cam-066"; rig.hideFlags = HideFlags.HideAndDontSave; var cam = rig.GetComponent<Camera>(); cam.enabled = false;
            var counts = new StringBuilder("{\n \"tag\": \"" + tag + "\", \"scene\": \"" + scenePath + "\", \"method\": \"renderers enabled, active, in the camera's culling mask, bounds inside the frustum; LODGroups count only the LOD the frame selects (QualitySettings.lodBias); triangles = that mesh's index count / 3 (skinned and static); shadow-only renderers excluded\",\n \"frames\": [");
            bool firstRow = true; var staged = new List<GameObject>();
            try
            {
                using var session = new RenderSession(); bool warm = false;
                foreach (var (key, fr) in new[] { ("cafe", pts.frames.cafe), ("office", pts.frames.office) })
                {
                    foreach (var g in staged) Object.DestroyImmediate(g); staged.Clear();
                    foreach (var c in fr.cast) staged.Add(Stage(lib, c, groundY));
                    var focus = new Vector3(fr.focus[0], groundY, fr.focus[1]);
                    cam.orthographic = true; cam.orthographicSize = 14f;
                    foreach (int yaw in Yaws)
                    {
                        var rot = Quaternion.Euler(38f, yaw, 0f); float stand = Mathf.Min(Rig.StandOff(rot, focus, 14f, 1920f / 1080f), MaxStand); // the clearance test meets the horizon band or the sea at yaw 300 (7.9 km back: the haze washed the frame out); 600 m clears every roof in the frame column here
                        rig.transform.SetPositionAndRotation(focus - rot * Vector3.forward * stand, rot); cam.farClipPlane = Mathf.Max(play.farClipPlane, stand + 250f); // yaw 300 stood back past the play camera's far clip (an empty frame)
                        Debug.Log($"066: {key} yaw {yaw}: stand-off {stand:0.0} m, far {cam.farClipPlane:0}");
                        var ts = rig.GetComponent<TacticalShadows>(); ts.focusDepth = stand; if (rig.TryGetComponent<ArchitectureFade>(out var af)) af.focus = focus;
                        if (!warm) { session.Grab(cam, 64, 64); warm = true; } // the first render after opening a scene comes out flat
                        string name = $"{tag}-{key}-y{yaw:000}";
                        using (ts.Use()) File.WriteAllBytes(Path.Combine(outDir, name + ".png"), session.Grab(cam, 1920, 1080).EncodeToPNG());
                        Row(counts, ref firstRow, name, cam);
                    }
                    foreach (var f in pts.follow)
                    {
                        if (f.frame != key) continue;
                        var s = new Vector3(f.subject[0], groundY, f.subject[1]); var dir = new Vector3(f.toward[0] - f.subject[0], 0f, f.toward[1] - f.subject[1]).normalized;
                        var eye = s - dir * 5.5f + Vector3.up * (1.3f + 0.32f * 5.5f); var look = s + dir * 8f + Vector3.up * 1.35f;
                        cam.orthographic = false; cam.fieldOfView = 55f; cam.nearClipPlane = 0.2f; cam.farClipPlane = 350f;
                        rig.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye)); if (rig.TryGetComponent<ArchitectureFade>(out var af)) af.focus = s;
                        var ts = rig.GetComponent<TacticalShadows>(); ts.focusDepth = 5.5f;
                        string name = $"{tag}-follow-{f.id}";
                        using (ts.Use()) File.WriteAllBytes(Path.Combine(outDir, name + ".png"), session.Grab(cam, 1920, 1080).EncodeToPNG());
                        Row(counts, ref firstRow, name, cam);
                        cam.orthographic = true; cam.nearClipPlane = play.nearClipPlane; cam.farClipPlane = play.farClipPlane;
                    }
                }
            }
            finally { foreach (var g in staged) Object.DestroyImmediate(g); Object.DestroyImmediate(rig); }
            File.WriteAllText(Path.Combine(outDir, $"counts-{tag}.json"), counts.Append("\n ]\n}\n").ToString());
            Debug.Log($"066: {tag} frames and counts -> {outDir}");
        });

        static GameObject Stage(FigureLibrary lib, Cast c, float y)
        {
            var f = lib.Find(c.fig) ?? throw new FileNotFoundException($"066: no figure {c.fig}");
            var go = (GameObject)Object.Instantiate(f.model); go.name = "066-" + c.id; go.hideFlags = HideFlags.DontSave;
            var at = new Vector3(c.at[0], y, c.at[1]); var to = new Vector3(c.face[0], y, c.face[1]) - at; to.y = 0f;
            go.transform.SetPositionAndRotation(at, to.sqrMagnitude > 0.01f ? Quaternion.LookRotation(to) : Quaternion.identity);
            var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); an.runtimeAnimatorController = lib.ControllerFor(f); an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            an.Rebind(); an.Play("idle", 0, 0.25f); an.Update(0f); an.Update(0.02f);
            return go;
        }

        static void Row(StringBuilder sb, ref bool first, string name, Camera cam)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(cam); int drawn = 0; long tris = 0; var inLod = new HashSet<Renderer>(); var chosen = new HashSet<Renderer>();
            foreach (var lg in Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!lg.enabled) continue; var lods = lg.GetLODs(); foreach (var l in lods) foreach (var r in l.renderers) if (r) inLod.Add(r);
                float size = lg.size * Mathf.Max(Mathf.Abs(lg.transform.lossyScale.x), Mathf.Abs(lg.transform.lossyScale.y), Mathf.Abs(lg.transform.lossyScale.z));
                var wc = lg.transform.TransformPoint(lg.localReferencePoint);
                float rel = cam.orthographic ? size / (2f * cam.orthographicSize) : size / (2f * Mathf.Max(0.01f, Vector3.Dot(wc - cam.transform.position, cam.transform.forward)) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
                rel *= QualitySettings.lodBias;
                for (int i = 0; i < lods.Length; i++) if (rel >= lods[i].screenRelativeTransitionHeight) { foreach (var r in lods[i].renderers) if (r) chosen.Add(r); break; }
            }
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || (cam.cullingMask & (1 << r.gameObject.layer)) == 0) continue;
                if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) continue;
                if (inLod.Contains(r) && !chosen.Contains(r)) continue;
                if (!GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;
                Mesh m = r is SkinnedMeshRenderer sk ? sk.sharedMesh : r.TryGetComponent<MeshFilter>(out var mf) ? mf.sharedMesh : null;
                drawn++; if (m != null) for (int s = 0; s < m.subMeshCount; s++) tris += (long)m.GetIndexCount(s) / 3;
            }
            sb.Append(first ? "\n" : ",\n").Append($"  {{\"frame\": \"{name}\", \"renderers\": {drawn}, \"triangles\": {tris}}}"); first = false;
            Debug.Log($"066: {name}: {drawn} renderers drawn, {tris.ToString("N0", CultureInfo.InvariantCulture)} triangles");
        }

        [System.Serializable] public sealed class Points { public Frames frames; public Follow[] follow; }
        [System.Serializable] public sealed class Frames { public Frame cafe, office; }
        [System.Serializable] public sealed class Frame { public float[] focus; public Cast[] cast; }
        [System.Serializable] public sealed class Cast { public string id, fig; public float[] at, face; }
        [System.Serializable] public sealed class Follow { public string id, frame; public float[] subject, toward; }
    }
}
