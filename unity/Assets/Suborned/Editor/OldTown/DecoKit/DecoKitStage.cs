#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Scene plumbing shared by the studio and the showroom: HDRI sky, environment bake, grade, camera, capture.
    internal static class DecoKitStage
    {
        internal const int ReflectOnlyLayer = 30;  // seen by reflection probes, culled from cameras
        internal const int PropLayer = 29;         // culled from the studio probe so props do not self-reflect

        /// Save the active scene. Objects made from script in batch mode do not dirty it, so SaveOpenScenes alone skips
        /// whatever was added after an earlier save.
        internal static void Save()
        {
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        internal static UnityEngine.SceneManagement.Scene NewScene(string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureFolder(ScenesDir);
            EditorSceneManager.SaveScene(scene, $"{ScenesDir}/{name}.unity");  // lighting bake needs a saved scene
            return scene;
        }

        /// Panoramic HDRI skybox as the ambient + default-reflection source (baked by BakeEnvironment).
        internal static void Sky(string hdri, float rotation, float exposure)
        {
            EnsureFolder(GeneratedDir);
            var path = $"{GeneratedDir}/DK_Sky_{hdri}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) AssetDatabase.CreateAsset(mat = new Material(Shader.Find("Skybox/Panoramic")), path);
            mat.SetTexture("_MainTex", DecoKitTextures.Hdri(hdri));
            mat.SetFloat("_Mapping", 1f);  // latitude-longitude
            mat.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
            mat.DisableKeyword("_MAPPING_6_FRAMES_LAYOUT");
            mat.SetFloat("_ImageType", 0f);
            mat.SetFloat("_Rotation", rotation);
            mat.SetFloat("_Exposure", exposure);
            mat.SetColor("_Tint", new Color(.5f, .5f, .5f));  // neutral (tint is x2 in the shader)
            EditorUtility.SetDirty(mat);
            RenderSettings.skybox = mat;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 256;
            RenderSettings.reflectionIntensity = 1f;
            RenderSettings.fog = false;
        }

        /// "Generate Lighting" with GI off: bakes only the ambient probe, default reflection and probes.
        internal static void BakeEnvironment(string name)
        {
            var path = $"{GeneratedDir}/{name}.lighting";
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (settings == null) AssetDatabase.CreateAsset(settings = new LightingSettings(), path);
            settings.bakedGI = false;
            settings.realtimeGI = false;
            Lightmapping.lightingSettings = settings;
            EditorSceneManager.SaveOpenScenes();
            Lightmapping.ClearDiskCache();  // a sky shader edit does not invalidate the cached sky cubemap
            if (!Lightmapping.Bake()) Debug.LogError("[DecoKit] environment bake failed");
            var sh = RenderSettings.ambientProbe;
            Debug.Log($"[DecoKit] ambient L0 = ({sh[0, 0]:F3}, {sh[1, 0]:F3}, {sh[2, 0]:F3})");
        }

        internal static Light Directional(string name, Vector3 travel, Color color, float intensity, bool shadows)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.LookRotation(travel.normalized, Vector3.up);
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = 1f;
            light.shadowBias = .02f;
            light.shadowNormalBias = .2f;
            if (shadows) RenderSettings.sun = light;
            var data = light.GetComponent<UniversalAdditionalLightData>() ?? light.gameObject.AddComponent<UniversalAdditionalLightData>();
            data.softShadowQuality = SoftShadowQuality.High;
            return light;
        }

        internal static VolumeProfile Grade(string name, TonemappingMode tone, float exposure, float contrast, float saturation)
        {
            var path = $"{GeneratedDir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);  // generated: rebuilt every run
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            Add<Tonemapping>(profile).mode.Override(tone);
            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(.35f);
            bloom.scatter.Override(.6f);
            var grade = Add<ColorAdjustments>(profile);
            grade.postExposure.Override(exposure);
            grade.contrast.Override(contrast);
            grade.saturation.Override(saturation);
            EditorUtility.SetDirty(profile);
            var volume = new GameObject("DK grade").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            return profile;
        }

        internal static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var c = profile.Add<T>(true);
            c.name = typeof(T).Name;
            c.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }

        internal static Camera Camera(string name, float fov)
        {
            var cam = new GameObject(name).AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.allowHDR = true;
            cam.allowMSAA = false;
            cam.cullingMask = ~(1 << ReflectOnlyLayer);
            cam.clearFlags = CameraClearFlags.Skybox;
            var urp = cam.GetUniversalAdditionalCameraData();
            urp.renderPostProcessing = true;
            urp.renderShadows = true;
            urp.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            urp.antialiasingQuality = AntialiasingQuality.High;
            return cam;
        }

        /// Renders at `ss`x and box-downsamples (linear-space average through sRGB targets).
        internal static Texture2D Render(Camera cam, int width, int height, int ss = 2)
        {
            // HDR target: URP sizes its intermediate buffer from targetTexture, so an LDR target would clamp at 1.0
            var big = new RenderTexture(width * ss, height * ss, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var small = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            var prev = RenderTexture.active;
            try
            {
                cam.targetTexture = big;
                // probes reach the culling system on an editor tick; a batch job builds and renders inside one call,
                // so without this every capture rendered with no reflection probes (the skybox only)
                ReflectionProbe.UpdateCachedState();
                cam.Render();
                Graphics.Blit(big, small);
                RenderTexture.active = small;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                return pixels;
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = prev;
                UnityEngine.Object.DestroyImmediate(big);
                UnityEngine.Object.DestroyImmediate(small);
            }
        }

        internal static void Capture(Camera cam, int width, int height, string file, int ss = 2)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? ".");
            var pixels = Render(cam, width, height, ss);
            File.WriteAllBytes(file, pixels.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(pixels);
        }

        /// Batch-mode knobs: -dkTone ACES -dkExposure -0.35 ... (falls back to the given default).
        internal static string Arg(string key, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-" + key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        internal static float Arg(string key, float fallback) =>
            float.TryParse(Arg(key, ""), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;

        /// Shadow settings live on the shared URP asset: tighten them for a capture, then restore.
        internal static void WithShadows(float distance, Action body)
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) { body(); return; }
            float d = urp.shadowDistance;
            int res = urp.mainLightShadowmapResolution;
            try
            {
                urp.shadowDistance = distance;
                urp.mainLightShadowmapResolution = 4096;
                body();
            }
            finally
            {
                urp.shadowDistance = d;
                urp.mainLightShadowmapResolution = res;
            }
        }

        /// Flat static slab (x/z rect at height y) with metric UVs: kit materials tile per metre, not per mesh.
        internal static GameObject Slab(string name, Rect rect, float y, Material mat)
        {
            var v = new[]
            {
                new Vector3(rect.xMin, y, rect.yMin), new Vector3(rect.xMax, y, rect.yMin),
                new Vector3(rect.xMax, y, rect.yMax), new Vector3(rect.xMin, y, rect.yMax),
            };
            var mesh = new Mesh { name = name, vertices = v, triangles = new[] { 0, 2, 1, 0, 3, 2 } };
            mesh.uv = System.Array.ConvertAll(v, p => new Vector2(p.x, p.z));
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            var path = $"{GeneratedDir}/{name.Replace(' ', '_')}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            var slab = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            slab.GetComponent<MeshFilter>().sharedMesh = mesh;
            slab.GetComponent<MeshCollider>().sharedMesh = mesh;
            slab.GetComponent<MeshRenderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(slab, StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.ContributeGI);
            return slab;
        }

        internal static void SetLayer(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        internal static Material Unlit(string name, Color linear, bool twoSided = true)
        {
            var path = $"{GeneratedDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) AssetDatabase.CreateAsset(mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")), path);
            mat.SetColor("_BaseColor", linear.gamma);  // Set* converts colour props gamma->linear: pre-encode so radiance is exact
            mat.SetFloat("_Cull", twoSided ? 0f : 2f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
#endif
