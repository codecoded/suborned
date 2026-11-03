#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Suborned.OldTown.Crowd;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The realtime crowd test: the saved street copied to DecoKitCrowdTest.unity with its placed people swapped at
    /// Play for a VatCrowd of the baked citizens (citizen_vat.py -> Art/Citizens3D/crowd), the game's 2.5D camera
    /// (CrowdTestCamera, DKCutaway) and the CrowdTestHud. Build() wraps each bake as a VatCitizen asset and saves the
    /// scene; Shoot() reopens it, runs the crowd in edit mode and captures play / close / tactical shots, a close walk
    /// strip, and times the crowd step (CPU) and a frame's render with the crowd at 0 / 3k / 10k agents. Player()
    /// builds it for macOS; run that with -crowdBench <out.json> for real frame and GPU times (CrowdBench).
    ///   -dkOut <prefix>
    internal static class DecoKitCrowdTest
    {
        const string Dir = "Assets/Suborned/OldTownArt/Citizens3D/crowd", SceneName = "DecoKitCrowdTest";
        const string ShaderPath = "Assets/Suborned/OldTownArt/Shaders/VatCrowd.shader", BlobShaderPath = "Assets/Suborned/OldTownArt/Shaders/VatCrowdBlob.shader";
        const float Back = CrowdTestCamera.Back;
        const string PlayerPath = "Builds/CrowdTest/CrowdTest.app";
        static readonly Vector3 Square = new(0f, 0f, -125f);  // Orbit Square, DecoKitIso's play shot
        static readonly Vector3 Mid = Square + new Vector3(-8f, 0f, -12f);

        [Serializable]
        class Meta
        {
            public int vertices;
            public VatCitizen.Clip[] clips;
        }

        internal static void Build()
        {
            DecoKitStreetDemo.OpenSaved().enabled = false;
            var all = Citizens();  // after the open: it unloads unreferenced assets
            var citizens = all.Where(c => !c.name.Contains('@')).ToArray();
            // the LOD bakes (<name>@near: the same mesh with 2K maps, <name>@far: 800 tris, 512 px), matched by name
            var near = Lod(all, citizens, "near");
            var far = Lod(all, citizens, "far");
            // the street's first-person Play rig: parked, F in play drops it where the 2.5D view looks (CrowdTestViews)
            var walker = Object.FindObjectsByType<FirstPersonController>().First();
            walker.gameObject.SetActive(false);
            var pace = new SerializedObject(walker);  // the crowd's pace, not the city-crossing 5.2 / 11 m/s
            pace.FindProperty("walkSpeed").floatValue = 1.8f;
            pace.FindProperty("sprintSpeed").floatValue = 6f;
            pace.ApplyModifiedPropertiesWithoutUndo();
            var root = new GameObject("DK crowd test");
            var crowd = root.AddComponent<VatCrowd>();
            var so = new SerializedObject(crowd);
            var list = so.FindProperty("citizens");
            list.arraySize = citizens.Length;
            for (int i = 0; i < citizens.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = citizens[i];
            foreach (var (field, set) in new[] { ("nearCitizens", near), ("farCitizens", far) })
            {
                if (set.Any(f => f == null)) continue;
                var lods = so.FindProperty(field);
                lods.arraySize = set.Length;
                for (int i = 0; i < set.Length; i++) lods.GetArrayElementAtIndex(i).objectReferenceValue = set[i];
            }
            so.FindProperty("shader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            so.FindProperty("blobShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(BlobShaderPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            var hud = new SerializedObject(root.AddComponent<CrowdTestHud>());
            hud.FindProperty("crowd").objectReferenceValue = crowd;
            hud.ApplyModifiedPropertiesWithoutUndo();
            var cam = DecoKitStage.Camera("DK crowd camera", 30f);
            cam.tag = "MainCamera";
            cam.gameObject.AddComponent<AudioListener>();
            cam.gameObject.AddComponent<DKCutaway>();
            var rig = cam.gameObject.AddComponent<CrowdTestCamera>();
            rig.Set(Square, 14f, 45f);
            var views = root.AddComponent<CrowdTestViews>();
            var vs = new SerializedObject(views);
            vs.FindProperty("crowd").objectReferenceValue = crowd;
            vs.FindProperty("iso").objectReferenceValue = rig;
            vs.FindProperty("walker").objectReferenceValue = walker;
            vs.ApplyModifiedPropertiesWithoutUndo();
            var bench = new SerializedObject(root.AddComponent<CrowdBench>());
            bench.FindProperty("crowd").objectReferenceValue = crowd;
            bench.FindProperty("rig").objectReferenceValue = rig;
            bench.FindProperty("views").objectReferenceValue = views;
            bench.ApplyModifiedPropertiesWithoutUndo();
            var scene = cam.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, $"{ScenesDir}/{SceneName}.unity");
            Debug.Log($"[DecoKit] crowd test: {ScenesDir}/{SceneName}.unity, {citizens.Length} citizens " +
                      string.Join(", ", all.Select(c => $"{c.name} {c.mesh.vertexCount} verts/{c.vertices} rows")));
        }

        /// A macOS player of the saved crowd test (frame timing stats on for CrowdBench's GPU times): Builds/CrowdTest.
        internal static void Player()
        {
            bool timing = PlayerSettings.enableFrameTimingStats;
            PlayerSettings.enableFrameTimingStats = true;
            try
            {
                var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { $"{ScenesDir}/{SceneName}.unity" }, locationPathName = PlayerPath,
                    target = BuildTarget.StandaloneOSX, options = BuildOptions.None,
                });
                Debug.Log($"[DecoKit] crowd player {r.summary.result}: {PlayerPath}, {r.summary.totalSize / 1e6:0} MB, " +
                          $"{r.summary.totalTime.TotalMinutes:0.0} min, {r.summary.totalErrors} errors");
                if (r.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("player build failed");
            }
            finally
            {
                PlayerSettings.enableFrameTimingStats = timing;
            }
        }

        /// Each bake in Dir (<name>.glb, .vat.bytes, .vat.json) as Dir/<name>.asset.
        static VatCitizen[] Lod(VatCitizen[] all, VatCitizen[] citizens, string level) =>
            citizens.Select(c => all.FirstOrDefault(f => f.name == $"{c.name}@{level}")).ToArray();

        static VatCitizen[] Citizens()
        {
            AssetDatabase.Refresh();
            var all = Directory.GetFiles(Dir, "*.vat.json").OrderBy(f => f).Select(json =>
            {
                string name = Path.GetFileName(json)[..^".vat.json".Length], path = $"{Dir}/{name}.asset";
                var meta = JsonUtility.FromJson<Meta>(File.ReadAllText(json));
                var c = AssetDatabase.LoadAssetAtPath<VatCitizen>(path);
                if (c == null)
                {
                    c = ScriptableObject.CreateInstance<VatCitizen>();
                    AssetDatabase.CreateAsset(c, path);
                }
                var subs = AssetDatabase.LoadAllAssetsAtPath($"{Dir}/{name}.glb");
                c.mesh = subs.OfType<Mesh>().First();
                var pbr = subs.OfType<Material>().First();  // glTFast's glTF material: its maps by glTF name
                c.albedo = pbr.GetTexture("baseColorTexture") as Texture2D ?? subs.OfType<Texture2D>().First();
                c.normal = pbr.HasTexture("normalTexture") ? pbr.GetTexture("normalTexture") as Texture2D : null;
                c.metallicRoughness = pbr.HasTexture("metallicRoughnessTexture") ? pbr.GetTexture("metallicRoughnessTexture") as Texture2D : null;
                if (c.mesh.tangents.Length == 0) Debug.LogWarning($"[DecoKit] {name}: no tangents, the normal map will shade wrong");
                if (c.normal != null && c.normal.isDataSRGB) Debug.LogWarning($"[DecoKit] {name}: normal map imported as sRGB, decodes skewed");
                c.frames = AssetDatabase.LoadAssetAtPath<TextAsset>($"{Dir}/{name}.vat.bytes");
                c.vertices = meta.vertices;
                c.clips = meta.clips;
                if (c.frames.dataSize != (long)c.vertices * c.clips.Sum(k => k.frames) * VatCitizen.Row * sizeof(float))
                    throw new InvalidDataException($"{name}: frames {c.frames.dataSize} B don't match {c.vertices} verts x {c.clips.Sum(k => k.frames)} frames");
                EditorUtility.SetDirty(c);
                return c;
            }).ToArray();
            AssetDatabase.SaveAssets();
            return all;
        }

        internal static void Shoot()
        {
            EditorSceneManager.OpenScene($"{ScenesDir}/{SceneName}.unity");
            DecoKitHarbour.Ocean();
            var crowd = Object.FindFirstObjectByType<VatCrowd>();
            var rig = Object.FindFirstObjectByType<CrowdTestCamera>();
            var cam = rig.GetComponent<Camera>();
            string prefix = DecoKitStage.Arg("dkOut", "crowd");
            crowd.Prime();
            float fog = RenderSettings.fogDensity;
            RenderSettings.fogDensity = fog * CrowdTestCamera.Fog;
            try
            {
                Object.DestroyImmediate(DecoKitStage.Render(cam, 480, 270));  // warm-up (NaN bloom on a fresh scene)
                Run(crowd, 4f);
                // name, focus, ortho size, yaw, agents, run share
                foreach (var (name, at, size, yaw, n, run) in new[]
                         {
                             ("play", Square, 14f, 45f, 3000, .1f), ("close", Square + new Vector3(-12f, 0f, -23f), 8f, 45f, 3000, .1f),
                             ("tactical", new Vector3(-30f, 0f, -150f), 48f, 45f, 10000, .1f), ("run", Square, 14f, 135f, 3000, .6f),
                         })
                {
                    crowd.Agents = n;
                    crowd.RunShare = run;
                    Run(crowd, 1f);
                    Shot(crowd, rig, cam, $"{prefix}_{name}", at, size, yaw);
                    Debug.Log($"[DecoKit] crowd {name}: {n} agents, {crowd.Drawn} drawn, {crowd.DrawnTris / 1e6f:0.00} M tris");
                }
                // LOD A/B, same instant: 3k tris vs the far 800 at tactical zoom, and the far LOD at play zoom
                float farSize = crowd.FarSize;
                crowd.Agents = 10000;
                Run(crowd, .5f);
                foreach (var (name, size, far) in new[] { ("tactical_far", 48f, 0f), ("tactical_lod0", 48f, 1e4f), ("play_far", 14f, 0f), ("play_lod0", 14f, 1e4f) })
                {
                    crowd.FarSize = far;
                    Shot(crowd, rig, cam, $"{prefix}_{name}", name.StartsWith("tactical") ? new Vector3(-30f, 0f, -150f) : Square, size, 45f);
                }
                crowd.FarSize = farSize;
                crowd.Agents = 3000;
                Run(crowd, .5f);
                Shot(crowd, rig, cam, $"{prefix}_closest", Mid, 4f, 45f);
                float nearSize = crowd.NearSize;  // same instant on the 3k mid LOD: what the near bake buys
                crowd.NearSize = 0f;
                Shot(crowd, rig, cam, $"{prefix}_closest_mid", Mid, 4f, 45f);
                crowd.NearSize = nearSize;
                Shot(crowd, rig, cam, $"{prefix}_closest_r", Mid, 4f, 135f);
                // separation A/B at the same spot: off for 3 s (the old crowd), then on again
                crowd.Separate = false;
                Run(crowd, 3f);
                Shot(crowd, rig, cam, $"{prefix}_closest_nosep", Mid, 4f, 45f);
                Crowding(crowd, "off");
                crowd.Separate = true;
                Run(crowd, 3f);
                Shot(crowd, rig, cam, $"{prefix}_closest_sep", Mid, 4f, 45f);
                Crowding(crowd, "on 3 s");
                Run(crowd, 12f);
                Crowding(crowd, "on 15 s");
                // sun A/B, same instant: the street's sun (travel .5,-.7,.5) sits behind the 45 deg lens and front-lights
                // everything flat; turned 60 deg either side it models the figures and their shadows fall into view
                var sun = RenderSettings.sun;
                var sunRot = sun.transform.rotation;
                foreach (var (name, turn) in new[] { ("front", 0f), ("left", -60f), ("right", 60f) })
                {
                    sun.transform.rotation = Quaternion.Euler(0f, turn, 0f) * sunRot;
                    Shot(crowd, rig, cam, $"{prefix}_sun_{name}", Mid, 4f, 45f);
                    Shot(crowd, rig, cam, $"{prefix}_sun_{name}_play", Square, 14f, 45f);
                }
                sun.transform.rotation = sunRot;
                RenderSettings.fogDensity = fog;  // the street eye looks through the full haze
                Eye(crowd, $"{prefix}_eye", Mid, 45f);
                Eye(crowd, $"{prefix}_eye_r", Mid, 225f);
                RenderSettings.fogDensity = fog * CrowdTestCamera.Fog;
                crowd.RunShare = .1f;
                Run(crowd, 1f);
                Shot(crowd, rig, cam, $"{prefix}_mid", Mid, 5f, 135f);
                // A/B, same instant, turned 90: at 45 the sun sits behind the lens and every shadow hides behind its caster
                crowd.Shadows = false;
                Shot(crowd, rig, cam, $"{prefix}_mid_noshadow", Mid, 5f, 135f);
                crowd.Shadows = true;
                Strip(crowd, rig, cam, prefix);
                Timing(crowd, rig, cam);
            }
            finally
            {
                DKCutaway.Clear();
                RenderSettings.fogDensity = fog;
                crowd.Release();  // the placed people come back, the buffers go
            }
        }

        /// How crowded the live agents are: the share overlapping anyone (closer than their radii's sum), the share
        /// stacked (closer than half that: the figures read as one), and the mean pace (share of stride speed).
        static void Crowding(VatCrowd crowd, string label)
        {
            int n = crowd.Agents, overlap = 0, stacked = 0;
            var p = new Vector2[n];
            var r = new float[n];
            for (int i = 0; i < n; i++) (p[i], r[i]) = (new Vector2(crowd.Position(i).x, crowd.Position(i).z), crowd.Radius(i));
            float pace = 0f;
            for (int i = 0; i < n; i++)
            {
                float worst = float.MaxValue;
                for (int j = 0; j < n; j++)
                    if (j != i) worst = Mathf.Min(worst, (p[i] - p[j]).magnitude / (r[i] + r[j]));
                overlap += worst < 1f ? 1 : 0;
                stacked += worst < .5f ? 1 : 0;
                pace += crowd.Pace(i);
            }
            Debug.Log($"[DecoKit] crowd separation {label}: {n} agents, {100f * overlap / n:0.0}% overlapping, " +
                      $"{100f * stacked / n:0.0}% stacked, mean pace {pace / n:0.00}");
        }

        static void Run(VatCrowd crowd, float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 30f) crowd.Step(1f / 30f);
        }

        static void Shot(VatCrowd crowd, CrowdTestCamera rig, Camera cam, string file, Vector3 at, float size, float yaw, int w = 1920, int h = 1080)
        {
            rig.Set(at, size, yaw);
            DKCutaway.Set(at, cam.transform.forward, Mathf.Min(40f, .8f * size), 2f, DKCutaway.Ghost, 4.5f, true, true);
            DecoKitStage.WithShadows(Back + size * 4f, () =>
            {
                crowd.Submit(cam);
                DecoKitStage.Capture(cam, w, h, Path.Combine(CaptureDir, file + ".png"));
            });
        }

        /// First person at street level: the walker's lens (eye 1.62 m, 62 deg) on the walk-graph node nearest `at`.
        static void Eye(VatCrowd crowd, string file, Vector3 at, float yaw)
        {
            var node = crowd.Graph.nodes.OrderBy(n => (n.x - at.x) * (n.x - at.x) + (n.z - at.z) * (n.z - at.z)).First();
            var eye = DecoKitStage.Camera("DK crowd eye", 62f);
            try
            {
                eye.nearClipPlane = .08f;
                eye.farClipPlane = 600f;
                eye.transform.SetPositionAndRotation(node + Vector3.up * 1.62f, Quaternion.Euler(4f, yaw, 0f));
                DKCutaway.Clear();
                crowd.Submit(eye);
                DecoKitStage.Capture(eye, 1920, 1080, Path.Combine(CaptureDir, file + ".png"));
                Debug.Log($"[DecoKit] crowd {Path.GetFileName(file)}: at {node}, yaw {yaw}, {crowd.Drawn} drawn, {crowd.DrawnTris / 1e6f:0.00} M tris");
            }
            finally
            {
                Object.DestroyImmediate(eye.gameObject);
            }
        }

        /// Per citizen, six frames 1/10 s apart following the agent nearest the close shot at a 2.2 m ortho size: gait,
        /// facing, feet, cape.
        static void Strip(VatCrowd crowd, CrowdTestCamera rig, Camera cam, string prefix)
        {
            crowd.Agents = 3000;
            crowd.RunShare = .1f;
            for (int c = 0; c < crowd.Citizens.Count; c++)
            {
                int who = crowd.Nearest(Square + new Vector3(-12f, 0f, -23f), c);
                for (int i = 0; i < 6; i++)
                {
                    crowd.Step(.1f);
                    Shot(crowd, rig, cam, $"{prefix}_strip{c}_{i}", crowd.Position(who) + Vector3.up * .9f, 1.1f, 45f, 540, 720);
                }
            }
        }

        /// Crowd step on the CPU, and a frame's render (incl. the GPU, read back) with and without the crowd.
        static void Timing(VatCrowd crowd, CrowdTestCamera rig, Camera cam)
        {
            rig.Set(Square, 14f, 45f);
            foreach (int n in new[] { 0, 3000, 10000 })
            {
                crowd.Agents = n;
                crowd.Separate = false;  // separation's own cost: the same 60 steps without it, then with
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 60; i++) crowd.Step(1f / 60f);
                double bare = sw.Elapsed.TotalMilliseconds / 60;
                crowd.Separate = true;
                for (int i = 0; i < 60; i++) crowd.Step(1f / 60f);  // settle the newly added agents first
                sw.Restart();
                for (int i = 0; i < 60; i++) crowd.Step(1f / 60f);
                double step = sw.Elapsed.TotalMilliseconds / 60;
                double render = 0;
                DecoKitStage.WithShadows(Back + 14f * 4f, () =>
                {
                    Object.DestroyImmediate(DecoKitStage.Render(cam, 1920, 1080, 1));
                    sw.Restart();
                    for (int i = 0; i < 10; i++)
                    {
                        crowd.Submit(cam);
                        Object.DestroyImmediate(DecoKitStage.Render(cam, 1920, 1080, 1));
                    }
                    render = sw.Elapsed.TotalMilliseconds / 10;
                });
                Debug.Log($"[DecoKit] crowd time {n}: step {step:0.00} ms ({bare:0.00} without separation, Burst {Unity.Burst.BurstCompiler.IsEnabled}), drawn {crowd.Drawn}, {crowd.DrawnTris / 1e6f:0.00} M tris, render+readback {render:0.0} ms (1080p)");
            }
        }
    }
}
#endif
