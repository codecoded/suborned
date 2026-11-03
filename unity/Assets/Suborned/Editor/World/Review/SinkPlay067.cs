using System.Collections.Generic;
using System.IO;
using System.Text;
using Suborned.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Brief 067 (Sky on 0.2.128, "everyone sunk into the ground"), a measurement in Play mode: `-executeMethod Suborned.World.Review.SinkPlay067.Run
    /// -out &lt;dir&gt;` (batchmode, no -quit) opens HarbourDistrict.unity, enters Play, lets the host open as a person's launch does (the briefing over
    /// the paused street), then Begin (the start sequence), runs, pauses, takes the follow view and the drive, and at each stage measures every drawn
    /// figure: the lowest LOD0 vertex skinned now against the drawn ground under it (a downward ray onto the scene's own flat meshes), the model's
    /// root, its view's position and the SiteFrame height. Writes play-&lt;tag&gt;.tsv and exits. Never saves the scene.</summary>
    [InitializeOnLoad]
    public static class SinkPlay067
    {
        const string Key = "sink067.play";
        static string Arg(string n) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

        static SinkPlay067()
        {
            if (!SessionState.GetBool(Key, false)) return;
            EditorApplication.update += Step;
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/HarbourDistrict.unity", OpenSceneMode.Single);
            SessionState.SetBool(Key, true); SessionState.SetFloat(Key + ".t0", -1f);
            EditorApplication.update += Step;
            EditorApplication.EnterPlaymode();
        }

        static int stage = -1; static float stageAt; static StringBuilder tsv; static List<MeshCollider> cols;
        static readonly string[] Stages = { "briefing", "begin+5s", "run+20s", "paused+3s", "follow-enforcer+3s", "drive-enforcer+3s", "drive-paused+3s", "run+40s" };

        static void Step()
        {
            if (!EditorApplication.isPlaying) { if (stage >= Stages.Length) Finish(); return; }
            var h = Object.FindFirstObjectByType<HandoverHost>(); if (h == null || h.Game == null) return;
            float now = Time.realtimeSinceStartup;
            if (stage < 0) { stage = 0; stageAt = now + 3f; tsv = new StringBuilder("stage\ttick\tpaused\tkey\tfig\tstate\tanimSpeed\tlod\tviewY\trootY\tframeY\tgroundY\tsoleMin\tsoleVsGround\trootVsGround\tx\tz\tblockX\tblockY\tinWindow\n"); Colliders(); return; }
            if (stage >= Stages.Length) return;
            if (now < stageAt) return;
            Measure(h, Stages[stage]);
            stage++;
            switch (stage)
            {
                case 1: h.BriefingBegin(); stageAt = now + 5f; break;
                case 2: h.SetPaused(false); stageAt = now + 20f; break;
                case 3: h.SetPaused(true); stageAt = now + 3f; break;
                case 4: h.SetPaused(false); h.Select(Suborned.Sim.OpId.Enforcer, false); h.FollowToggle(); stageAt = now + 3f; break;
                case 5: h.FollowToggle(); h.DriveToggle(); stageAt = now + 3f; break;
                case 6: h.SetPaused(true); stageAt = now + 3f; break;
                case 7: h.SetPaused(false); h.DriveToggle(); stageAt = now + 40f; break;
                default: EditorApplication.ExitPlaymode(); break;
            }
        }

        static void Colliders()
        {
            cols = new List<MeshCollider>();
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var b = mr.bounds; if (!mr.enabled || b.size.y > 1.2f || b.min.y > 3f || !mr.TryGetComponent<MeshFilter>(out var mf) || !mf.sharedMesh || mr.GetComponent<Collider>()) continue;
                if (mr.GetComponentInParent<HandoverHost>()) continue; // the host's own views (markers, capsules)
                var c = mr.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = mf.sharedMesh; cols.Add(c);
            }
            Physics.SyncTransforms();
        }

        static void Measure(HandoverHost h, string name)
        {
            var mesh = new Mesh(); int n = 0;
            foreach (var an in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
            {
                if (an.name != "figure" || !an.gameObject.activeInHierarchy) continue;
                var smrs = an.GetComponentsInChildren<SkinnedMeshRenderer>(); if (smrs.Length == 0) continue;
                SkinnedMeshRenderer s0 = null; string lod = "";
                foreach (var s in smrs) { if (s.name.EndsWith("_LOD0")) s0 = s; if (s.isVisible) lod += s.name[^4..] + " "; }
                s0 ??= smrs[0];
                s0.BakeMesh(mesh, true); var m = Matrix4x4.TRS(s0.transform.position, s0.transform.rotation, Vector3.one); float lo = float.MaxValue; foreach (var v in mesh.vertices) lo = Mathf.Min(lo, m.MultiplyPoint3x4(v).y);
                var root = an.transform.position; var view = an.transform.parent ? an.transform.parent.position : root;
                var b = h.frame.ToBlock(new Vector2(root.x, root.z)); float fy = h.frame.Height(b.x, b.y);
                float gy = float.NaN; foreach (var hit in Physics.RaycastAll(new Vector3(root.x, root.y + 1.0f, root.z), Vector3.down, 3f)) if (hit.collider is MeshCollider mc && cols.Contains(mc) && (float.IsNaN(gy) || hit.point.y > gy)) gy = hit.point.y;
                var info = an.GetCurrentAnimatorClipInfo(0); string st = info.Length > 0 && info[0].clip ? info[0].clip.name : "";
                string key = an.transform.parent ? an.transform.parent.name : "?";
                tsv.Append($"{name}\t{h.Game.Tick}\t{h.Paused}\t{key}\t{s0.name.Replace("_LOD0", "")}\t{st}\t{an.speed:0.00}\t{lod.Trim()}\t{view.y:0.000}\t{root.y:0.000}\t{fy:0.000}\t{gy:0.000}\t{lo:0.000}\t{lo - gy:+0.000;-0.000}\t{root.y - gy:+0.000;-0.000}\t{root.x:0.00}\t{root.z:0.00}\t{b.x:0.00}\t{b.y:0.00}\t{(b.x >= h.frame.x0 && b.y >= h.frame.y0 && b.x < h.frame.x0 + h.frame.nx * h.frame.cell && b.y < h.frame.y0 + h.frame.ny * h.frame.cell ? 1 : 0)}\n"); n++;
            }
            Object.DestroyImmediate(mesh);
            Debug.Log($"sinkplay: {name}: {n} figures measured at tick {h.Game.Tick}");
        }

        static void Finish()
        {
            EditorApplication.update -= Step; SessionState.EraseBool(Key);
            var dir = Path.GetFullPath(Arg("-out") ?? Path.Combine(HarbourWorldBuilder.RepoRoot, "out/scratch/latch/sink")); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"play-{Arg("-tag") ?? "probe"}.tsv"), tsv?.ToString() ?? "no measurement\n");
            Debug.Log("sinkplay: done -> " + dir);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
