#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Real-time twin of render_icons.py: same view, lens and studio rig, one PNG per prefab, so Unity
    /// fidelity can be judged against the Cycles icons side by side (compose_sheet.py --pair icons_unity).
    internal static class DecoKitStudio
    {
        const string SceneName = "DecoKitStudio";
        const int Px = 720;
        const float Fov = 28.84f;  // Blender 70 mm lens on a 36 mm sensor, square frame
        static readonly Vector3 View = new Vector3(.55f, .36f, .83f).normalized;  // Blender (-0.55, -0.83, 0.36)
        static readonly Color KeyColor = new(1f, .96f, .9f);

        // render_icons.studio() rig, Blender (x, y, z) -> Unity (-x, z, -y), at scale k = 1.
        // Cards are the softboxes: emissive, reflection-only, so chrome and lacquer pick up the same streaks.
        // Blender's "Top" panel faces up and Cycles emission is one-sided, so it never reaches the subject:
        // it is deliberately absent here (a visible overhead card greys every chrome top).
        static readonly (string name, Vector3 pos, Vector2 size, float radiance)[] Cards =
        {
            ("Key", new Vector3(3f, 4.5f, 2.5f), new Vector2(3f, 3f), 31.8f),  // 900 W / (9 m2 * pi)
            ("Left", new Vector3(4f, 2f, 1f), new Vector2(4f, .8f), 9f),
            ("Rim", new Vector3(-3.5f, 2f, -2.5f), new Vector2(4f, .5f), 6f),
        };

        /// Batch knobs (see DecoKitStage.Arg): -dkTone ACES|Neutral -dkExposure -dkContrast -dkSaturation
        /// -dkRamp (reflected floor gain) -dkOnly A,B -dkOut <dir under SourceArt/DecoKit>.
        /// Defaults are the values tuned against the Cycles icons with the LookdevBalls calibration asset.
        internal static void CaptureAll()
        {
            DecoKitStage.NewScene(SceneName);
            DecoKitStage.Sky("studio", 0f, 1f);
            // Card irradiance at the subject (radiance * area / d^2) over pi -> Unity directional intensity.
            DecoKitStage.Directional("Key", -Cards[0].pos, KeyColor, 2.55f, true);
            DecoKitStage.Directional("Left fill", -Cards[1].pos, Color.white, .43f, false);
            DecoKitStage.Directional("Rim", -Cards[2].pos, Color.white, .17f, false);
            var tone = DecoKitStage.Arg("dkTone", "ACES") == "Neutral" ? TonemappingMode.Neutral : TonemappingMode.ACES;
            DecoKitStage.Grade("DK_StudioGrade", tone, DecoKitStage.Arg("dkExposure", -.6f),
                DecoKitStage.Arg("dkContrast", 8f), DecoKitStage.Arg("dkSaturation", 0f));
            var backdrop = Floor(out var floorRenderer);
            var rig = Rig();
            DecoKitStage.BakeEnvironment(SceneName);
            var cam = DecoKitStage.Camera("DK studio camera", Fov);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Backdrop;
            Calibrate(cam, backdrop);

            var outName = DecoKitStage.Arg("dkOut", "icons_unity");  // the default set is live; variants are tests
            var dir = Path.Combine(outName == "icons_unity" ? ArtDir : CaptureDir, outName);
            var order = new List<string>();
            var only = DecoKitStage.Arg("dkOnly", "");
            foreach (var row in DecoKitData.Assets().assets)
            {
                if (only != "" && !only.Split(',').Contains(row.name)) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(row));
                if (prefab == null) { Debug.LogError($"[DecoKit] no prefab for {row.name}; run Import"); continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                DecoKitStage.SetLayer(go, DecoKitStage.PropLayer);
                var b = DecoKitModels.Bounds(go);
                float r = b.extents.magnitude;
                float dist = r / Mathf.Sin(Fov * .5f * Mathf.Deg2Rad) * 1.02f;
                float k = Mathf.Max(r, .5f) / .6f;
                cam.transform.position = b.center + View * dist;
                cam.transform.LookAt(b.center, Vector3.up);
                cam.nearClipPlane = Mathf.Max(.02f, dist - 3f * r);
                cam.farClipPlane = dist + 400f;
                float floorY = Mathf.Min(b.min.y, 0f);  // sub-grade parts (rails, sumps) sit on the floor
                rig.position = new Vector3(b.center.x, floorY, b.center.z);
                floorRenderer.transform.position = new Vector3(0f, floorY, 0f);
                rig.localScale = Vector3.one * k;
                var shadowProxy = ShadowProxy(go);
                var probe = BakeProbe(row.name, b.center, dist);  // fresh probe: the Forward+ atlas caches by probe id
                Object.DestroyImmediate(shadowProxy);
                DecoKitStage.WithShadows(dist + 3f * r, () =>
                {
                    Object.DestroyImmediate(DecoKitStage.Render(cam, 32, 32, 1));  // warm-up: registers the new probe in the atlas
                    DecoKitStage.Capture(cam, Px, Px, Path.Combine(dir, $"{row.name}.png"));
                });
                order.Add(row.name);
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(probe.gameObject);
                Debug.Log($"[DecoKit] studio {row.name} r={r:F2}");
            }
            // every captured icon in manifest order, so a -dkOnly re-run never truncates the sheet list
            var all = DecoKitData.Assets().assets.Select(r => r.name).Where(n => File.Exists(Path.Combine(dir, $"{n}.png")));
            File.WriteAllText(Path.Combine(dir, "order.txt"), string.Join("\n", all));
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[DecoKit] studio captured {order.Count} -> {dir}");
        }

        /// Camera-visible backdrop: flat colour darkened by shadow + SSAO only (Blender shadow catcher + composite).
        static Material Floor(out Transform floorTransform)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Studio floor";
            floor.transform.localScale = Vector3.one * 40f;
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            var path = $"{GeneratedDir}/DK_StudioFloor.mat";
            AssetDatabase.DeleteAsset(path);
            var mat = new Material(Shader.Find("OldTown/DecoKit/Shadow Catcher")) { name = "DK_StudioFloor" };
            AssetDatabase.CreateAsset(mat, path);
            floor.GetComponent<Renderer>().sharedMaterial = mat;
            floorTransform = floor.transform;
            return mat;
        }

        /// The backdrop goes through the grade, so solve for the scene-linear value that lands on #FBF8EC.
        static void Calibrate(Camera cam, Material floor)
        {
            cam.transform.position = View * 4f;
            cam.transform.LookAt(Vector3.zero, Vector3.up);
            var target = Backdrop.linear;
            var b = target;
            for (int i = 0; i < 10; i++)
            {
                floor.SetColor("_BaseColor", b.gamma);
                var tex = DecoKitStage.Render(cam, 16, 16, 1);
                var got = tex.GetPixel(8, 8).linear;
                Object.DestroyImmediate(tex);
                b = new Color(b.r * Ratio(target.r, got.r), b.g * Ratio(target.g, got.g), b.b * Ratio(target.b, got.b));
                if (i == 9) Debug.Log($"[DecoKit] backdrop scene-linear {b} -> {got} (target {target})");
            }
        }

        static float Ratio(float want, float got) => Mathf.Clamp(Mathf.Pow(want / Mathf.Max(got, 1e-4f), 1.5f), .5f, 3f);

        static readonly Color Backdrop = new(.984f, .973f, .925f);  // #FBF8EC, the Cycles icon backdrop

        /// Reflection-only environment that scales with the subject: softbox cards + Blender's radial floor ramp.
        static Transform Rig()
        {
            var rig = new GameObject("Studio rig").transform;
            foreach (var (name, pos, size, radiance) in Cards)
            {
                var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
                card.name = $"Softbox {name}";
                Object.DestroyImmediate(card.GetComponent<Collider>());
                card.transform.SetParent(rig, false);
                card.transform.localPosition = pos;
                card.transform.localScale = new Vector3(size.x, size.y, 1f);
                card.transform.rotation = Quaternion.LookRotation(pos.normalized, Vector3.up);  // face the subject
                var tint = name == "Key" ? KeyColor : Color.white;
                card.GetComponent<Renderer>().sharedMaterial = DecoKitStage.Unlit($"DK_Softbox_{name}", tint * radiance);
                ReflectOnly(card);
            }
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ramp.name = "Reflected floor ramp";
            Object.DestroyImmediate(ramp.GetComponent<Collider>());
            ramp.transform.SetParent(rig, false);
            ramp.transform.localPosition = new Vector3(0f, .002f, 0f);
            ramp.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ramp.transform.localScale = new Vector3(60f, 60f, 1f);
            ramp.GetComponent<Renderer>().sharedMaterial = RampMaterial();
            ReflectOnly(ramp);
            return rig;
        }

        /// Shadow-only twin on the reflection layer: the probe sees the prop's shadow on the floor (as Cycles
        /// reflections do) without the prop itself, which a probe at its centre would smear over it.
        static GameObject ShadowProxy(GameObject prop)
        {
            var proxy = Object.Instantiate(prop);
            proxy.name = prop.name + " shadow proxy";
            DecoKitStage.SetLayer(proxy, DecoKitStage.ReflectOnlyLayer);
            foreach (var r in proxy.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            return proxy;
        }

        static void ReflectOnly(GameObject go)
        {
            DecoKitStage.SetLayer(go, DecoKitStage.ReflectOnlyLayer);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ReflectionProbeStatic);
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// Blender floor colour: Object-length ramp, 0.35 -> cream, 1.4 -> near black (60 m plane, radius 30).
        static Material RampMaterial()
        {
            const int n = 1024;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Clamp };
            var cream = new Color(.9f, .86f, .76f) * DecoKitStage.Arg("dkRamp", .5f);  // Cycles sees it shadowed + AO'd
            var dark = new Color(.05f, .05f, .05f);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = new Vector2(x + .5f - n * .5f, y + .5f - n * .5f).magnitude / (n * .5f) * 30f;
                px[y * n + x] = Color.Lerp(cream, dark, Mathf.InverseLerp(.35f, 1.4f, d));
            }
            tex.SetPixels(px);
            tex.Apply();
            var texPath = $"{GeneratedDir}/DK_StudioRamp.asset";
            AssetDatabase.DeleteAsset(texPath);
            AssetDatabase.CreateAsset(tex, texPath);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "DK_StudioRamp" };
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", .1f);
            AssetDatabase.CreateAsset(mat, $"{GeneratedDir}/DK_StudioRamp.mat");
            return mat;
        }

        /// Bakes the reflection-only environment for one subject into a fresh custom probe. Fresh each time:
        /// the Forward+ atlas caches by probe id. Imported uncompressed: BC6H greys the near-black studio
        /// around its 500-nit lamps.
        static ReflectionProbe BakeProbe(string name, Vector3 center, float dist)
        {
            EnsureFolder(GeneratedDir + "/Probes");
            var path = $"{GeneratedDir}/Probes/DK_StudioProbe_{name}.exr";
            var probe = new GameObject($"Studio probe {name}").AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom;
            probe.renderDynamicObjects = true;
            probe.cullingMask = 1 << DecoKitStage.ReflectOnlyLayer;
            probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
            probe.resolution = 512;
            probe.hdr = true;
            probe.boxProjection = false;
            probe.importance = 10;
            probe.transform.position = center;
            probe.size = Vector3.one * (dist * 6f);
            probe.nearClipPlane = .05f;
            probe.farClipPlane = 500f;
            if (!Lightmapping.BakeReflectionProbe(probe, path)) Debug.LogError("[DecoKit] probe bake failed");
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            probe.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
            return probe;
        }
    }
}
#endif
