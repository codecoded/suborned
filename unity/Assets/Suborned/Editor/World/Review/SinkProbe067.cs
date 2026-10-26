using System.Collections.Generic;
using System.IO;
using System.Text;
using Suborned.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Brief 067 (Sky on 0.2.128: "why is everyone sunk into the ground"), a measurement only: opens HarbourDistrict.unity read-only (never
    /// saved), stands the cell, Inez, the guard and two crowd figures from the figure library on plaza points exactly as the game does (the figure's
    /// root at the host's SiteFrame height, applyRootMotion off, the shared controller), steps each idle-family clip through its length and, per
    /// sample, skins LOD0 (BakeMesh) for the lowest sole vertex and reads the foot and toe bones. The drawn ground under each point comes from a
    /// downward ray on temporary mesh colliders over the scene's own meshes (figures excluded). Writes sink.json and, with -shots, two 1080p frames:
    /// the follow view behind the Enforcer (HostFollow's defaults) and a conversation close-up (HostConvo's profile framing, approximated).
    /// Batchmode: -executeMethod Suborned.World.Review.SinkProbe067.Run -points &lt;points.json&gt; -out &lt;dir&gt; -tag before|after [-shots]</summary>
    public static class SinkProbe067
    {
        static string Arg(string n) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
        static bool Flag(string n) => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), n) >= 0;
        static readonly string[] States = { "idle", "idle_alt", "talk" };

        public static void Run() => HarbourWorldBuilder.Batch(() =>
        {
            string outDir = Path.GetFullPath(Arg("-out") ?? Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs/067/latch-sink")), tag = Arg("-tag") ?? "probe";
            var pts = JsonUtility.FromJson<HeightCap066.Points>(File.ReadAllText(Arg("-points")));
            EditorSceneManager.OpenScene("Assets/Scenes/HarbourDistrict.unity", OpenSceneMode.Single);
            Directory.CreateDirectory(outDir);
            var host = Object.FindFirstObjectByType<HandoverHost>() ?? throw new FileNotFoundException("sink: no HandoverHost in the scene");
            var frame = host.frame;
            var lib = AssetDatabase.LoadAssetAtPath<FigureLibrary>("Assets/Suborned/People/Generated/FigureLibrary.asset") ?? throw new FileNotFoundException("sink: no FigureLibrary");
            // who: the cell, Inez, the guard from the 066 points; two crowd figures from the passer pool on two more plaza points
            var cast = new List<(string id, string fig, Vector3 at, Vector3 face)>();
            foreach (var fr in new[] { pts.frames.cafe, pts.frames.office })
                foreach (var c in fr.cast) if (c.id is "scout" or "fixer" or "tech" or "enforcer" or "inez" or "guard" && !cast.Exists(k => k.id == c.id))
                        cast.Add((c.id, c.fig, new Vector3(c.at[0], 0f, c.at[1]), new Vector3(c.face[0], 0f, c.face[1])));
            var passers = Pool(lib, "family/passer");
            var cafe = pts.frames.cafe.focus; var ent = new Vector3(cafe[0], 0f, cafe[1]);
            if (passers.Count > 0) cast.Add(("crowd1:" + passers[0], passers[0], ent + new Vector3(-3f, 0f, 2f), ent));
            if (passers.Count > 1) cast.Add(("crowd2:" + passers[passers.Count / 2], passers[passers.Count / 2], ent + new Vector3(2f, 0f, -3f), ent));

            var added = new List<MeshCollider>();
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!mr.enabled || !mr.TryGetComponent<MeshFilter>(out var mf) || !mf.sharedMesh || mr.GetComponent<Collider>()) continue;
                var b = mr.bounds; bool near = false; foreach (var c in cast) { var p = c.at; if (b.min.x - 1f < p.x && p.x < b.max.x + 1f && b.min.z - 1f < p.z && p.z < b.max.z + 1f && b.min.y < 3f) near = true; }
                if (!near) continue; var col = mr.gameObject.AddComponent<MeshCollider>(); col.sharedMesh = mf.sharedMesh; added.Add(col);
            }
            Physics.SyncTransforms();
            var sb = new StringBuilder("{\n \"tag\": \"" + tag + "\",\n \"method\": \"root at SiteFrame.Height (as HostViews), the shared controller, applyRootMotion off; each state sampled at 12 points of its length; sole = lowest vertex of LOD0 skinned (BakeMesh); ground = downward ray on the scene's meshes; all in metres, sole - ground < 0 is sunk\",\n \"figures\": [");
            var staged = new List<GameObject>(); bool first = true;
            try
            {
                foreach (var c in cast)
                {
                    var bk = frame.ToBlock(new Vector2(c.at.x, c.at.z)); float fy = frame.Height(bk.x, bk.y);
                    float gy = Physics.Raycast(new Vector3(c.at.x, fy + 3f, c.at.z), Vector3.down, out var hit, 10f) ? hit.point.y : float.NaN;
                    string gname = hit.collider ? hit.collider.name : "";
                    var f = lib.Find(c.fig); if (f == null) { Debug.LogWarning($"sink: no figure {c.fig}"); continue; }
                    var go = (GameObject)Object.Instantiate(f.model); go.hideFlags = HideFlags.DontSave; staged.Add(go);
                    var to = c.face - c.at; to.y = 0f;
                    go.transform.SetPositionAndRotation(new Vector3(c.at.x, fy, c.at.z), to.sqrMagnitude > 0.01f ? Quaternion.LookRotation(to) : Quaternion.identity);
                    var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); an.runtimeAnimatorController = lib.ControllerFor(f); an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    var smr = LOD0(go);
                    sb.Append(first ? "\n" : ",\n").Append($"  {{\"id\": \"{c.id}\", \"fig\": \"{c.fig}\", \"at\": [{c.at.x:0.00}, {c.at.z:0.00}], \"rootY\": {fy:0.000}, \"groundY\": {gy:0.000}, \"ground\": \"{gname}\", \"states\": {{"); first = false;
                    bool fs = true;
                    foreach (var st in States)
                    {
                        float soleMin = float.MaxValue, soleMax = float.MinValue, toeMin = float.MaxValue, footMin = float.MaxValue; var mesh = new Mesh();
                        an.Rebind(); an.Update(0f);
                        for (int k = 0; k < 12; k++)
                        {
                            an.Play(st, 0, k / 12f); an.Update(0f);
                            float s = float.MaxValue;
                            if (smr) { smr.BakeMesh(mesh, true); var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one); foreach (var v in mesh.vertices) s = Mathf.Min(s, m.MultiplyPoint3x4(v).y); }
                            soleMin = Mathf.Min(soleMin, s); soleMax = Mathf.Max(soleMax, s);
                            foreach (var hb in new[] { HumanBodyBones.LeftToes, HumanBodyBones.RightToes }) if (an.GetBoneTransform(hb) is Transform tb) toeMin = Mathf.Min(toeMin, tb.position.y);
                            foreach (var hb in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot }) if (an.GetBoneTransform(hb) is Transform tb) footMin = Mathf.Min(footMin, tb.position.y);
                        }
                        Object.DestroyImmediate(mesh);
                        float g = float.IsNaN(gy) ? fy : gy;
                        sb.Append(fs ? "" : ", ").Append($"\"{st}\": {{\"soleMinVsGround\": {soleMin - g:0.000}, \"soleMaxVsGround\": {soleMax - g:0.000}, \"toeBoneMinVsGround\": {toeMin - g:0.000}, \"footBoneMinVsGround\": {footMin - g:0.000}}}"); fs = false;
                        Debug.Log($"sink {tag}: {c.id,-24} {st,-9} root-ground {fy - g:+0.000;-0.000} sole {soleMin - g:+0.000;-0.000}..{soleMax - g:+0.000;-0.000} toe {toeMin - g:+0.000;-0.000} foot {footMin - g:+0.000;-0.000} ({gname})");
                    }
                    sb.Append("}, \"rootVsGround\": ").Append(float.IsNaN(gy) ? "null" : (fy - gy).ToString("0.000")).Append('}');
                    an.Play("idle", 0, 0.25f); an.Update(0f);
                    { // every LOD's lowest vertex in the same pose (the play zoom draws LOD1-2)
                        var lodLine = new StringBuilder(); float gg = float.IsNaN(gy) ? fy : gy; var mesh = new Mesh();
                        foreach (var s in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        {
                            s.BakeMesh(mesh, true); var m = Matrix4x4.TRS(s.transform.position, s.transform.rotation, Vector3.one); float lo = float.MaxValue; foreach (var v in mesh.vertices) lo = Mathf.Min(lo, m.MultiplyPoint3x4(v).y);
                            lodLine.Append($" {s.name}={lo - gg:+0.000;-0.000}");
                        }
                        Object.DestroyImmediate(mesh); Debug.Log($"sink {tag}: {c.id} lods idle@0.25:{lodLine}");
                    }
                }
                File.WriteAllText(Path.Combine(outDir, $"sink-{tag}.json"), sb.Append("\n ]\n}\n").ToString());
                foreach (var c in added) Object.DestroyImmediate(c); added.Clear();
                if (Flag("-shots")) Shots(outDir, tag, cast, staged);
            }
            finally { foreach (var c in added) if (c) Object.DestroyImmediate(c); foreach (var g in staged) Object.DestroyImmediate(g); }
            Debug.Log($"sink {tag}: -> {outDir}");
        });

        /// <summary>The drawn ground against the host's SiteFrame over the whole site window: per 0.5 m, a downward ray onto the flat pieces (renderers
        /// under 1.2 m tall whose bottom is below 3 m); the drawn top minus the frame's height, written as a grid (survey-&lt;tag&gt;.json) and summarised.</summary>
        public static void Survey() => HarbourWorldBuilder.Batch(() =>
        {
            string outDir = Path.GetFullPath(Arg("-out") ?? Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs/067/latch-sink")), tag = Arg("-tag") ?? "probe";
            EditorSceneManager.OpenScene("Assets/Scenes/HarbourDistrict.unity", OpenSceneMode.Single);
            var host = Object.FindFirstObjectByType<HandoverHost>(); var fr = host.frame; Directory.CreateDirectory(outDir);
            var added = new List<MeshCollider>();
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var b = mr.bounds; if (!mr.enabled || (b.size.y > 1.2f && !Flag("-all")) || b.min.y > 3f || b.max.y < -0.5f || !mr.TryGetComponent<MeshFilter>(out var mf) || !mf.sharedMesh || mr.GetComponent<Collider>()) continue;
                var col = mr.gameObject.AddComponent<MeshCollider>(); col.sharedMesh = mf.sharedMesh; added.Add(col);
            }
            Physics.SyncTransforms();
            var hist = new SortedDictionary<int, int>(); var sb = new StringBuilder("{\n \"tag\": \"" + tag + "\", \"cellsM\": 0.5, \"rows\": [\n"); var worst = new List<(float d, float x, float z, float fy, string n)>();
            float bx0 = fr.x0, by0 = fr.y0, bx1 = fr.x0 + fr.nx * fr.cell, by1 = fr.y0 + fr.ny * fr.cell; int n = 0;
            for (float bx = bx0 + 0.25f; bx < bx1; bx += 0.5f)
                for (float by = by0 + 0.25f; by < by1; by += 0.5f)
                {
                    float fy = fr.Height(bx, by); var p = fr.ToPlan(bx, by);
                    if (!Physics.Raycast(new Vector3(p.x, fy + 0.6f, p.y), Vector3.down, out var hit, 1.2f)) continue;
                    float d = hit.point.y - fy; int bin = Mathf.RoundToInt(d * 100f); hist[bin] = (hist.TryGetValue(bin, out var c) ? c : 0) + 1; n++;
                    if (Mathf.Abs(d) > 0.03f) { worst.Add((d, p.x, p.y, fy, hit.collider.name)); sb.Append($"  [{p.x:0.00}, {p.y:0.00}, {fy:0.000}, {d:0.000}, \"{hit.collider.name}\"],\n"); }
                }
            foreach (var c in added) Object.DestroyImmediate(c);
            var summary = new StringBuilder(); foreach (var kv in hist) summary.Append($"{kv.Key}cm:{kv.Value} ");
            File.WriteAllText(Path.Combine(outDir, $"survey-{tag}.json"), sb.Append($"  null\n ],\n \"samples\": {n},\n \"histogramCm\": \"{summary}\"\n}}\n").ToString());
            Debug.Log($"survey {tag}: {n} samples; drawn - frame (cm): {summary}");
            worst.Sort((a, b) => b.d.CompareTo(a.d)); for (int i = 0; i < Mathf.Min(40, worst.Count); i++) Debug.Log($"survey {tag}: {worst[i].d:+0.000;-0.000} at ({worst[i].x:0.0}, {worst[i].z:0.0}) frame {worst[i].fy:0.000} on {worst[i].n}");
        });

        /// <summary>The runtime's own sequence (HostViews.Dress then Figures), off the scene: a fresh figure, the animator speed the clock gives
        /// (pace 0 while paused or held, 1 running), CrossFadeInFixedTime to idle or idle_alt at the figure's phase offset, then frames of 1/60 s;
        /// the lowest LOD0 vertex against the figure's own root after each case. Logs only.</summary>
        public static void Sequences() => HarbourWorldBuilder.Batch(() =>
        {
            var lib = AssetDatabase.LoadAssetAtPath<FigureLibrary>("Assets/Suborned/People/Generated/FigureLibrary.asset");
            foreach (var f in lib.figures)
            {   // the model as it comes from the FBX, never animated (what an animator that never evaluates leaves drawn)
                var go = (GameObject)Object.Instantiate(f.model); go.hideFlags = HideFlags.DontSave; var smr = LOD0(go); if (!smr) { Object.DestroyImmediate(go); continue; }
                var mesh = new Mesh(); smr.BakeMesh(mesh, true); var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one); float lo = float.MaxValue; foreach (var v in mesh.vertices) lo = Mathf.Min(lo, m.MultiplyPoint3x4(v).y);
                var an = go.GetComponent<Animator>(); var hips = an && an.isHuman ? an.GetBoneTransform(HumanBodyBones.Hips) : null;
                Debug.Log($"rest: {f.id,-14} sole {lo:+0.000;-0.000} hips {(hips ? hips.position.y : float.NaN):0.000}");
                Object.DestroyImmediate(mesh); Object.DestroyImmediate(go);
            }
            foreach (var id in new[] { "scout", "fixer", "tech", "enforcer", "waitress", "guard" })
                foreach (var st in new[] { "idle", "idle_alt" })
                    foreach (var (name, s0, frames0, s1, frames1) in new[] { ("run", 1f, 60, 1f, 0), ("paused-from-start", 0f, 60, 0f, 0), ("run-then-pause", 1f, 30, 0f, 30), ("paused-then-run", 0f, 30, 1f, 30) })
                    {
                        var f = lib.Find(id); var go = (GameObject)Object.Instantiate(f.model); go.hideFlags = HideFlags.DontSave;
                        var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); an.runtimeAnimatorController = lib.ControllerFor(f); an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        an.speed = s0; an.CrossFadeInFixedTime(st, 0f, 0, 0.37f * 5f);
                        for (int i = 0; i < frames0; i++) an.Update(1f / 60f);
                        an.speed = s1; for (int i = 0; i < frames1; i++) an.Update(1f / 60f);
                        var smr = LOD0(go); var mesh = new Mesh(); smr.BakeMesh(mesh, true); var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one); float lo = float.MaxValue; foreach (var v in mesh.vertices) lo = Mathf.Min(lo, m.MultiplyPoint3x4(v).y);
                        var hips = an.GetBoneTransform(HumanBodyBones.Hips);
                        Debug.Log($"seq: {id,-9} {st,-9} {name,-18} sole {lo:+0.000;-0.000} hips {hips.position.y:0.000} state {an.GetCurrentAnimatorStateInfo(0).IsName(st)}");
                        Object.DestroyImmediate(mesh); Object.DestroyImmediate(go);
                    }
        });

        static SkinnedMeshRenderer LOD0(GameObject go)
        {
            SkinnedMeshRenderer best = null;
            foreach (var s in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (best == null || s.name.EndsWith("_LOD0") || (!best.name.EndsWith("_LOD0") && s.sharedMesh && best.sharedMesh && s.sharedMesh.vertexCount > best.sharedMesh.vertexCount)) best = s;
            return best;
        }

        static List<string> Pool(FigureLibrary lib, string key)
        {
            for (int i = 0; i < lib.poolKeys.Length; i++) if (lib.poolKeys[i] == key) return new List<string>(lib.poolIds[i].Split(','));
            return new List<string>();
        }

        static void Shots(string outDir, string tag, List<(string id, string fig, Vector3 at, Vector3 face)> cast, List<GameObject> staged)
        {
            var play = GameObject.Find("cameras/play")?.GetComponent<Camera>() ?? throw new FileNotFoundException("sink: no cameras/play");
            var rig = Object.Instantiate(play.gameObject); rig.hideFlags = HideFlags.HideAndDontSave; var cam = rig.GetComponent<Camera>(); cam.enabled = false;
            try
            {
                using var session = new RenderSession(); session.Grab(cam, 64, 64);
                GameObject Fig(string id) { int i = cast.FindIndex(c => c.id == id); return i >= 0 && i < staged.Count ? staged[i] : null; }
                // the follow view behind the Enforcer, toward the cell (HostFollow's defaults, as HeightCap066)
                var enf = Fig("enforcer"); var fix = Fig("fixer");
                if (enf && fix)
                {
                    var s = enf.transform.position; var dir = fix.transform.position - s; dir.y = 0f; dir.Normalize(); s -= dir * 1.0f;
                    var eye = s - dir * 5.5f + Vector3.up * (1.3f + 0.32f * 5.5f); var look = s + dir * 8f + Vector3.up * 1.35f;
                    cam.orthographic = false; cam.fieldOfView = 55f; cam.nearClipPlane = 0.2f; cam.farClipPlane = 350f;
                    rig.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
                    var ts = rig.GetComponent<TacticalShadows>(); if (ts) ts.focusDepth = 5.5f; if (rig.TryGetComponent<ArchitectureFade>(out var af)) af.focus = s;
                    Save(session, cam, ts, Path.Combine(outDir, $"{tag}-follow-enforcer.png"));
                }
                // a conversation close-up: the Fixer and the Tech in profile, the lens low and level (HostConvo's profile shot, approximated)
                var a = Fig("fixer"); var b = Fig("tech");
                if (a && b)
                {
                    var mid = (a.transform.position + b.transform.position) / 2f; var ab = b.transform.position - a.transform.position; ab.y = 0f; var side = Vector3.Cross(Vector3.up, ab.normalized);
                    float dist = Mathf.Max(3.2f, ab.magnitude * 1.4f); var lk = mid + Vector3.up * 1.0f; var eye = lk + side * dist + Vector3.up * 0.35f;
                    cam.orthographic = false; cam.fieldOfView = 35f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 350f;
                    rig.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(lk - eye));
                    var ts = rig.GetComponent<TacticalShadows>(); if (ts) ts.focusDepth = dist; if (rig.TryGetComponent<ArchitectureFade>(out var af)) af.focus = mid;
                    Save(session, cam, ts, Path.Combine(outDir, $"{tag}-convo-fixer-tech.png"));
                }
            }
            finally { Object.DestroyImmediate(rig); }
        }

        static void Save(RenderSession s, Camera cam, TacticalShadows ts, string file)
        {
            if (ts) using (ts.Use()) File.WriteAllBytes(file, s.Grab(cam, 1920, 1080).EncodeToPNG());
            else File.WriteAllBytes(file, s.Grab(cam, 1920, 1080).EncodeToPNG());
        }
    }
}
