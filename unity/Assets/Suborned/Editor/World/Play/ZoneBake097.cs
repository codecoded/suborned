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
    /// put back (generateSecondaryUV 0) so no kit mesh carries an unused channel. Off by default: only -bake runs it.
    /// Second run (same day): the bake saw a sane sky (trilight 0.71/0.72/0.80, sun 3) and wrote sane lightmap scale/offsets; the player drew the same
    /// per-slab noise with the GPU Resident Drawer off (-nogrd), so it is not the drawer. The noise is per texel at the scale of a metre-tiling texture:
    /// the lightmapped meshes are sampled through UVs that are not the ones the baker used (suspect: the player's mesh-data optimisation or the
    /// combined zone tiles' UV2). Next: in the Editor, compare one Pave_Slab's UV2 in the scene and in a development player.</summary>
    public static class ZoneBake097
    {
        const string ZoneRoot = "Ours (Handover zone + network, our pipeline)";
        static readonly Rect ZoneXZ = Rect.MinMaxRect(-226f, -166f, -84f, 58f); // LeanScene's
        public static bool Asked => Array.IndexOf(Environment.GetCommandLineArgs(), "-bake") >= 0;

        /// <summary>Batch: the played OldTown.unity's zone baked and saved (after OldTownPlay.Install; tools/game/batch.ts unity-world, once it is in).</summary>
        public static void BakeTown()
        {
            try { EditorSceneManager.OpenScene(OldTownPlay.ScenePath, OpenSceneMode.Single); Debug.Log(Bake()); EditorApplication.Exit(LightmapSettings.lightmaps.Length > 0 ? 0 : 1); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

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
                    r.scaleInLightmap = area > 2000f ? 0.6f : area > 300f ? 0.8f : 1f; // big ground and walls coarser: the bounce is soft anyway
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
                lightmapper = LightingSettings.Lightmapper.ProgressiveGPU, lightmapResolution = 8f, lightmapPadding = 3, lightmapMaxSize = 2048,
                directionalityMode = LightmapsMode.NonDirectional, lightmapCompression = LightmapCompression.NormalQuality,
                ao = true, aoMaxDistance = 1.2f, aoExponentIndirect = 1.0f, aoExponentDirect = 0f,
                indirectScale = Tune("bounce", 1f), directSampleCount = 32, indirectSampleCount = 768, environmentSampleCount = 384, maxBounces = 3, filteringMode = LightingSettings.FilterMode.Auto,
                prioritizeView = false, exportTrainingData = false,
            };
            Lightmapping.lightingSettings = s;
            // the bake lights the zone as the play camera does (OldTownPlayLook's light swap: our sun's heading, colour and intensity and our trilight),
            // not as the old stage's scene holds it (its sun 3 and higher, its baked old sky): first bake, 2026-11-06, bounce ~1.7x too strong and from the wrong side
            var look = Object.FindAnyObjectByType<Suborned.OldTown.OldTownPlayLook>(FindObjectsInactive.Include);
            Light lsun = look && look.lightSwap ? look.sun : null; Quaternion r0 = default; Color c0 = default; float i0 = 0;
            var am0 = RenderSettings.ambientMode; Color s0 = RenderSettings.ambientSkyColor, e0 = RenderSettings.ambientEquatorColor, g0 = RenderSettings.ambientGroundColor; float ai0 = RenderSettings.ambientIntensity;
            if (lsun)
            {
                r0 = lsun.transform.rotation; c0 = lsun.color; i0 = lsun.intensity;
                lsun.transform.rotation = look.sunRotation; lsun.color = look.sunColour; lsun.intensity = look.sunIntensity;
                float k = Tune("sky", 1f); // Unity's ambient intensity multiplier only scales a skybox source: a trilight is scaled through its colours, in linear
                Color Up(Color c) => (c.linear * k).gamma;
                RenderSettings.ambientMode = look.ambientMode; RenderSettings.ambientSkyColor = Up(look.ambientSky); RenderSettings.ambientEquatorColor = Up(look.ambientEquator); RenderSettings.ambientGroundColor = Up(look.ambientGround); RenderSettings.ambientIntensity = look.ambientIntensity;
            }
            Debug.Log($"ZoneBake097: {(lsun ? "the play light" : "the scene's light (no OldTownPlayLook light swap)")}: ambient {RenderSettings.ambientMode} sky {RenderSettings.ambientSkyColor} equator {RenderSettings.ambientEquatorColor} ground {RenderSettings.ambientGroundColor} x{RenderSettings.ambientIntensity}; skybox {(RenderSettings.skybox ? RenderSettings.skybox.shader.name : "none")}; sun {(RenderSettings.sun ? RenderSettings.sun.intensity.ToString() : "none")}");
            // emission does not bake: the kit's emitters are Realtime Emissive (m_LightmapFlags 1), which a baked-GI pass leaves out. Do not touch their GI
            // flags: setting EmissiveIsBlack for a bake (tried 2026-11-06) let URP's material validation strip _EMISSION from 23 kit materials (lamps, signals)
            // the reflection probes keep the cubemaps they have (Glint's street canyon, the play look's): Lightmapping.Bake re-renders every Baked probe from
            // the editor's raw scene (no play grade, the old stage's light), which flooded the square's box-projected probe white in the 2026-11-06 bakes
            int frozen = 0;
            foreach (var rp in Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include))
                if (rp.mode == UnityEngine.Rendering.ReflectionProbeMode.Baked && rp.bakedTexture) { var tex = rp.bakedTexture; rp.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom; rp.customBakedTexture = tex; frozen++; }
            Debug.Log($"ZoneBake097: {frozen} reflection probes kept as they are (Custom, their current cubemaps)");
            bool ok = Lightmapping.Bake();
            if (lsun) { lsun.transform.rotation = r0; lsun.color = c0; lsun.intensity = i0; RenderSettings.ambientMode = am0; RenderSettings.ambientSkyColor = s0; RenderSettings.ambientEquatorColor = e0; RenderSettings.ambientGroundColor = g0; RenderSettings.ambientIntensity = ai0; }
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true).Where(x => x.lightmapIndex >= 0).Take(4)) Debug.Log($"ZoneBake097: {r.name} lightmap {r.lightmapIndex} st {r.lightmapScaleOffset}");
            var scene = EditorSceneManager.GetActiveScene(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return $"ZoneBake097: {(ok ? "baked" : "FAILED")} in {(DateTime.UtcNow - t0).TotalSeconds:F0} s: {mapped} lightmapped ({uv2} meshes given UV2), {contrib} contributing by probes, {probesOnly} probes only; {LightmapSettings.lightmaps.Length} lightmaps";
        }

        /// <summary>A bake setting from Editor/World/look097.json "bake" (the default when it is not there).</summary>
        static float Tune(string key, float fallback)
        {
            var p = System.IO.Path.Combine(HarbourWorldBuilder.RepoRoot, "unity/Assets/Suborned/Editor/World/look097.json"); if (!System.IO.File.Exists(p)) return fallback;
            return J.Get(J.Get(Json.Parse(System.IO.File.ReadAllText(p)), "bake"), key) is double d ? (float)d : fallback;
        }

        static bool Solid(Material m) => m && m.renderQueue < 2450 && m.shader && !m.shader.name.Contains("Leaf") && !m.shader.name.Contains("Fountain") && !m.shader.name.Contains("Signal")
            && !m.shader.name.Contains("Contact") && !m.name.Contains("Glass") && !m.name.StartsWith("DK_Leaf") && !m.name.StartsWith("DK_Petal");

        static bool Near(Bounds b, float margin) => b.max.x >= ZoneXZ.xMin - margin && b.min.x <= ZoneXZ.xMax + margin && b.max.z >= ZoneXZ.yMin - margin && b.min.z <= ZoneXZ.yMax + margin;

        /// <summary>A lightmap layout, not merely a second UV set: every UV2 inside [0, 1] and the charts' total area at most 1. A merged tile whose sources had
        /// no UV2 carries a copy of the tiling texture UVs there (CombineMeshes' fallback), which the lightmapper would bake through as overlapping charts:
        /// the per-texel noise and near-black atlases of the first two bakes.</summary>
        static bool HasUV2(Mesh m)
        {
            var l = new List<Vector2>(); m.GetUVs(1, l); if (l.Count != m.vertexCount || l.Count == 0) return false;
            foreach (var u in l) if (u.x < -0.001f || u.y < -0.001f || u.x > 1.001f || u.y > 1.001f) return false;
            double area = 0; for (int s = 0; s < m.subMeshCount; s++) { var t = m.GetTriangles(s); for (int i = 0; i + 2 < t.Length; i += 3) { Vector2 a = l[t[i]], b = l[t[i + 1]], c = l[t[i + 2]]; area += Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5; } }
            return area > 1e-6 && area <= 1.02;
        }

        /// <summary>A kit model gets its importer's lightmap UVs (a reimport); a mesh the builders made gets Unwrapping's (in place).</summary>
        static bool MakeUV2(Mesh m)
        {
            var path = AssetDatabase.GetAssetPath(m);
            if (!string.IsNullOrEmpty(path) && AssetImporter.GetAtPath(path) is ModelImporter mi)
            {
                if (!mi.generateSecondaryUV) { mi.generateSecondaryUV = true; mi.secondaryUVHardAngle = 70f; mi.secondaryUVPackMargin = 6f; mi.SaveAndReimport(); }
                return true;
            }
            if (!m.isReadable) { Debug.LogWarning($"ZoneBake097: {m.name} not readable: lit by probes"); return false; }
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
