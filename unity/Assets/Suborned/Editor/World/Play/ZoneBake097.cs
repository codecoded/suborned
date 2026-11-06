using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Suborned.World.Play
{
    /// <summary>Brief 097 (Sky: "AAA standard lighting whilst always mindful of performance cost", "maximise Unity"; Sky 2026-11-05: the Handover zone only, no
    /// full-town rebake): Unity's GPU lightmapper over the Handover zone. Mixed lighting, Baked Indirect: the sun stays realtime (its light, its shadows,
    /// the cloud cookie), and the lightmaps hold only what the sun and sky bounce and what the geometry shades from the sky: the soft occlusion under
    /// cornices, at wall feet and between pieces, and the warm bounce off cream stone and pale paving into the shade. One lightmap fetch per pixel.
    ///  - lightmapped: the zone root's opaque DecoKit renderers (UV2 generated where a mesh lacks it);
    ///  - contributing, lit by probes: the town round the zone (it shades and bounces into the zone, takes no lightmap);
    ///  - probes only, not contributing: leaves, petals, glass, water, emitters, people and cars (they are dynamic or see-through);
    ///  - a light-probe grid over the zone at standing heights, so people and props pick up the same bounce.
    /// The scene's trilight ambient is the bake's sky. Run on the open scene (the lean copy or OldTown after Install); -bake on the scratch build.
    /// STATUS (2026-11-06, first bake, lean 0.2.254-scratch): NOT USABLE YET. The bake ran (313 s, 4,855 renderers, 6 lightmaps of 2048) but the
    /// atlases came out near black (the bake saw almost no sky: the trilight ambient is set at runtime by the look, not in the scene's RenderSettings
    /// when it bakes) and the player drew the lightmapped zone blown out white with per-slab noise, the signature of missing or mis-sampled LIGHTMAP_ON
    /// variants (suspects: the GPU Resident Drawer's lightmap path for the 719 instanced paving slabs, URP's lightmap prefilters). Needs an Editor session
    /// to read one renderer's lightmap index and scale/offset and the variant it draws with. The kit model importers it switched to generate UV2 were
    /// put back (generateSecondaryUV 0) so no kit mesh carries an unused channel. Off by default: only -bake runs it.</summary>
    public static class ZoneBake097
    {
        const string ZoneRoot = "Ours (Handover zone + network, our pipeline)";
        static readonly Rect ZoneXZ = Rect.MinMaxRect(-226f, -166f, -84f, 58f); // LeanScene's
        public static bool Asked => Array.IndexOf(Environment.GetCommandLineArgs(), "-bake") >= 0;

        public static string Bake()
        {
            var t0 = DateTime.UtcNow;
            var root = GameObject.Find(ZoneRoot) ?? throw new InvalidOperationException($"ZoneBake097: no \"{ZoneRoot}\"");
            var zone = new HashSet<Renderer>(root.GetComponentsInChildren<MeshRenderer>(true));
            int mapped = 0, contrib = 0, probesOnly = 0, uv2 = 0; var meshesDone = new HashSet<Mesh>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var go = r.gameObject; var mf = r.GetComponent<MeshFilter>(); var mesh = mf ? mf.sharedMesh : null;
                var flags = GameObjectUtility.GetStaticEditorFlags(go) & ~StaticEditorFlags.ContributeGI;
                bool solid = mesh && r.enabled && go.activeInHierarchy && r.sharedMaterials.All(Solid);
                bool near = Near(r.bounds, 60f);
                if (zone.Contains(r) && solid && mesh.vertexCount > 0)
                {
                    if (!HasUV2(mesh) && meshesDone.Add(mesh)) { if (MakeUV2(mesh)) uv2++; }
                    if (!HasUV2(mesh)) { r.receiveGI = ReceiveGI.LightProbes; GameObjectUtility.SetStaticEditorFlags(go, flags | StaticEditorFlags.ContributeGI); contrib++; continue; }
                    GameObjectUtility.SetStaticEditorFlags(go, flags | StaticEditorFlags.ContributeGI); r.receiveGI = ReceiveGI.Lightmaps;
                    var b = r.bounds; float area = 2f * (b.size.x * b.size.z + b.size.x * b.size.y + b.size.z * b.size.y);
                    r.scaleInLightmap = area > 2000f ? 0.35f : area > 300f ? 0.6f : 1f; // big ground and walls coarser: the bounce is soft anyway
                    mapped++;
                }
                else if (solid && near) { GameObjectUtility.SetStaticEditorFlags(go, flags | StaticEditorFlags.ContributeGI); r.receiveGI = ReceiveGI.LightProbes; contrib++; }
                else { GameObjectUtility.SetStaticEditorFlags(go, flags); probesOnly++; }
            }
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                l.lightmapBakeType = l.type == LightType.Directional && l.isActiveAndEnabled ? LightmapBakeType.Mixed : LightmapBakeType.Realtime;
            Probes(root.transform);
            var s = new LightingSettings
            {
                name = "ZoneBake097", bakedGI = true, realtimeGI = false, mixedBakeMode = MixedLightingMode.IndirectOnly,
                lightmapper = LightingSettings.Lightmapper.ProgressiveGPU, lightmapResolution = 5f, lightmapPadding = 3, lightmapMaxSize = 2048,
                directionalityMode = LightmapsMode.NonDirectional, lightmapCompression = LightmapCompression.NormalQuality,
                ao = true, aoMaxDistance = 1.2f, aoExponentIndirect = 1.0f, aoExponentDirect = 0f,
                directSampleCount = 32, indirectSampleCount = 384, environmentSampleCount = 256, maxBounces = 3, filteringMode = LightingSettings.FilterMode.Auto,
                prioritizeView = false, exportTrainingData = false,
            };
            Lightmapping.lightingSettings = s;
            Debug.Log($"ZoneBake097: ambient {RenderSettings.ambientMode} sky {RenderSettings.ambientSkyColor} equator {RenderSettings.ambientEquatorColor} ground {RenderSettings.ambientGroundColor} x{RenderSettings.ambientIntensity}; skybox {(RenderSettings.skybox ? RenderSettings.skybox.shader.name : "none")}; sun {(RenderSettings.sun ? RenderSettings.sun.intensity.ToString() : "none")}");
            bool ok = Lightmapping.Bake();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true).Where(x => x.lightmapIndex >= 0).Take(4)) Debug.Log($"ZoneBake097: {r.name} lightmap {r.lightmapIndex} st {r.lightmapScaleOffset}");
            var scene = EditorSceneManager.GetActiveScene(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return $"ZoneBake097: {(ok ? "baked" : "FAILED")} in {(DateTime.UtcNow - t0).TotalSeconds:F0} s: {mapped} lightmapped ({uv2} meshes given UV2), {contrib} contributing by probes, {probesOnly} probes only; {LightmapSettings.lightmaps.Length} lightmaps";
        }

        static bool Solid(Material m) => m && m.renderQueue < 2450 && m.shader && !m.shader.name.Contains("Leaf") && !m.shader.name.Contains("Fountain") && !m.shader.name.Contains("Signal")
            && !m.shader.name.Contains("Contact") && !m.name.Contains("Glass") && !m.name.StartsWith("DK_Leaf") && !m.name.StartsWith("DK_Petal");

        static bool Near(Bounds b, float margin) => b.max.x >= ZoneXZ.xMin - margin && b.min.x <= ZoneXZ.xMax + margin && b.max.z >= ZoneXZ.yMin - margin && b.min.z <= ZoneXZ.yMax + margin;

        static bool HasUV2(Mesh m) { var l = new List<Vector2>(); m.GetUVs(1, l); return l.Count == m.vertexCount; }

        /// <summary>A kit model gets its importer's lightmap UVs (a reimport); a mesh the builders made gets Unwrapping's (in place).</summary>
        static bool MakeUV2(Mesh m)
        {
            var path = AssetDatabase.GetAssetPath(m);
            if (!string.IsNullOrEmpty(path) && AssetImporter.GetAtPath(path) is ModelImporter mi)
            {
                if (!mi.generateSecondaryUV) { mi.generateSecondaryUV = true; mi.secondaryUVHardAngle = 70f; mi.secondaryUVPackMargin = 6f; mi.SaveAndReimport(); }
                return true;
            }
            if (!m.isReadable) return false;
            var p = new UnwrapParam(); UnwrapParam.SetDefaults(out p); p.hardAngle = 70f; p.packMargin = 6f / 1024f;
            return Unwrapping.GenerateSecondaryUVSet(m, p);
        }

        /// <summary>Probes every 4 m over the zone's ground at 0.6 and 2.2 m (feet and heads), one LightProbeGroup.</summary>
        static void Probes(Transform root)
        {
            var old = root.Find("Light probes (brief 097)"); if (old) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject("Light probes (brief 097)"); go.transform.SetParent(root, false);
            var g = go.AddComponent<LightProbeGroup>(); var pts = new List<Vector3>();
            for (float x = ZoneXZ.xMin; x <= ZoneXZ.xMax; x += 4f) for (float z = ZoneXZ.yMin; z <= ZoneXZ.yMax; z += 4f)
                { pts.Add(new Vector3(x, 0.6f, z)); pts.Add(new Vector3(x, 2.2f, z)); }
            g.probePositions = pts.Select(p => go.transform.InverseTransformPoint(p)).ToArray();
        }
    }
}
