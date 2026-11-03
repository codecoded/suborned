#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Every kit prefab laid out by category on a sunlit terrazzo plaza (courtyard HDRI), plus a hero capture.
    internal static class DecoKitShowroom
    {
        const string SceneName = "DecoKitShowroom";
        static readonly string[] CategoryOrder = { "Street", "Signals", "Booths", "Ground", "Vehicles", "Robots", "Structures", "Plants" };
        const float Gap = 1.8f, RowGap = 4.5f;

        internal static void Build()
        {
            DecoKitStage.NewScene(SceneName);
            DecoKitStage.Sky("courtyard", 0f, 1f);
            DecoKitStage.Directional("Sun", new Vector3(-.45f, -.72f, -.53f), new Color(1f, .95f, .86f), 3.2f, true);
            DecoKitStage.Grade("DK_ShowroomGrade", UnityEngine.Rendering.Universal.TonemappingMode.ACES, -.2f, 10f, 8f);
            var root = new GameObject("Deco Kit").transform;
            var bounds = Layout(root);
            Plaza(bounds);
            Probe(bounds);
            DecoKitStage.BakeEnvironment(SceneName);
            var cam = HeroCamera(bounds);
            DecoKitStage.WithShadows(120f, () => DecoKitStage.Capture(cam, 1920, 1080,
                Path.Combine(CaptureDir, "unity_showroom.png")));
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[DecoKit] showroom built: {root.childCount} rows, bounds {bounds.size}");
        }

        /// One row per category, fronts (+Z) toward the camera, rows receding into -Z.
        static Bounds Layout(Transform root)
        {
            var rows = DecoKitData.Assets().assets.Where(a => a.category != "Calibration").GroupBy(a => a.category)
                .OrderBy(g => Rank(g.Key));
            float z = 0f;
            var all = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var group in rows)
            {
                var row = new GameObject(group.Key).transform;
                row.SetParent(root, false);
                var items = group.Select(a => (a, prefab: AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(a))))
                                 .Where(t => t.prefab != null).ToList();
                float width = items.Sum(t => t.a.size[0]) + Gap * (items.Count - 1);
                float depth = items.Count > 0 ? items.Max(t => t.a.size[2]) : 0f;
                float x = -width * .5f;
                z -= depth * .5f;
                foreach (var (a, prefab) in items)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, row);
                    go.transform.position = new Vector3(x + a.size[0] * .5f, 0f, z);
                    x += a.size[0] + Gap;
                    var b = DecoKitModels.Bounds(go);
                    if (all.size == Vector3.zero) all = b; else all.Encapsulate(b);
                }
                z -= depth * .5f + RowGap;
            }
            return all;
        }

        static int Rank(string category)
        {
            int i = System.Array.IndexOf(CategoryOrder, category);
            return i < 0 ? CategoryOrder.Length : i;
        }

        static void Plaza(Bounds content)
        {
            var size = new Vector2(content.size.x + 120f, content.size.z + 120f);  // edges never in frame
            var rect = new Rect(content.center.x - size.x * .5f, content.center.z - size.y * .5f, size.x, size.y);
            DecoKitStage.Slab("Plaza slab", rect, 0f, AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/DK_Terrazzo_Ivory.mat"));
        }

        static void Probe(Bounds content)
        {
            var probe = new GameObject("Showroom probe").AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
            probe.transform.position = new Vector3(content.center.x, 1.6f, content.center.z);
            probe.size = content.size + new Vector3(30f, 20f, 30f);
            probe.resolution = 512;
            probe.hdr = true;
            probe.boxProjection = true;
        }

        static Camera HeroCamera(Bounds content)
        {
            var cam = DecoKitStage.Camera("DK showroom camera", 32f);
            var target = new Vector3(content.center.x, .8f, content.center.z + content.extents.z * .25f);
            float dist = Mathf.Max(content.size.x * .95f, content.size.z * 1.3f);
            cam.transform.position = target + new Vector3(.15f, .9f, 1f).normalized * dist;  // ~41 deg down: sky out of frame
            cam.transform.LookAt(target, Vector3.up);
            cam.nearClipPlane = .3f;
            cam.farClipPlane = dist * 4f;
            return cam;
        }
    }
}
#endif
