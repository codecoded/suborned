using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Brief 074-C (Latch, 2026-10-28): building colourways, before against after, offscreen. `-tag 074before` | `-tag 074`:
    /// the play views (office-front and cell-open at yaw 30 and 210, ortho 14, pitch 38) plus one wide view across lamplight-lane (the
    /// export's road segment with the most non-civic module buildings within 40 m, picked by out/scratch/latch/074/pickwide.ts), at the
    /// game's wide zoom 18.5, yaw 30. Shots → briefs/074/shots/{before,after}/, counts (After072's estimator, EveryFace's `today` mode
    /// skipped) → briefs/074/latch/{before,after}/offscreen.json, and the colourway census → census.json: column layouts (prefabs under
    /// Built/Columns, distinct column meshes placed, their triangles across LODs), materials in the scene, on the modules and on the shadow
    /// proxies, and module instances per wall material.</summary>
    public static partial class After072
    {
        internal const float WideZoom = 18.5f;
        internal static readonly Vector2 WideFocus = new(-215f, -120f);
        internal const float WideYaw = 30f;

        static float ZoomOf(Baseline072.View v) => v.Id == "wide-lamplight" ? WideZoom : Baseline072.PlayZoom;

        static void C074Views(Ctx c)
        {
            c.views.RemoveAll(v => v.Yaw != 30f && v.Yaw != 210f);
            c.views.Add(new Baseline072.View { Id = "wide-lamplight", Label = $"across lamplight-lane, ortho {WideZoom}", Focus = WideFocus, Yaw = WideYaw });
        }

        static void C074Census(string file, string export, string tag, Ctx c)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var all = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            var sceneMats = new HashSet<Material>(); foreach (var r in all) foreach (var m in r.sharedMaterials) if (m) sceneMats.Add(m);
            var ef = UnityEngine.Object.FindAnyObjectByType<EveryFace>(FindObjectsInactive.Include);
            var modMats = new HashSet<Material>(); var colPrefabs = new HashSet<GameObject>(); var colMeshes = new HashSet<Mesh>();
            var wallUse = new SortedDictionary<string, int>(StringComparer.Ordinal); int columns = 0, singles = 0;
            if (ef) foreach (var root in ef.modules)
            {
                if (!root) continue;
                bool col = root.name.StartsWith("Column_", StringComparison.Ordinal);
                if (col) { columns++; var src = PrefabUtility.GetCorrespondingObjectFromSource(root); if (src) colPrefabs.Add(src); } else singles++;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.gameObject.name == ShadowProxies.Name) continue;
                    if (col && r.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh) colMeshes.Add(mf.sharedMesh);
                    foreach (var m in r.sharedMaterials) if (m) { modMats.Add(m); if (m.name.Contains("Limestone") || m.name.Contains("Render_White")) seen.Add(m.name); }
                }
                foreach (var s in seen) wallUse[s] = (wallUse.TryGetValue(s, out var k) ? k : 0) + 1;
            }
            long colTris = 0; foreach (var m in colMeshes) for (int i = 0; i < m.subMeshCount; i++) colTris += m.GetIndexCount(i) / 3;
            var folder = KitColumns.Folder;
            int folderPrefabs = AssetDatabase.IsValidFolder(folder) ? AssetDatabase.FindAssets("t:Prefab", new[] { folder }).Length : 0;
            int folderMeshes = AssetDatabase.IsValidFolder(folder) ? AssetDatabase.FindAssets("t:Mesh", new[] { folder }).Length : 0;
            var proxyMats = new HashSet<Material>(); int proxies = 0, modProxies = 0;
            foreach (var sp in UnityEngine.Object.FindObjectsByType<ShadowProxy>(FindObjectsInactive.Include))
            {
                if (!sp.proxy) continue; proxies++; if (sp.module) modProxies++;
                foreach (var m in sp.proxy.sharedMaterials) if (m) proxyMats.Add(m);
            }
            var shaders = new HashSet<string>(sceneMats.Where(m => m.shader).Select(m => m.shader.name + "|" + string.Join(",", m.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal))));
            var modShaders = new HashSet<string>(modMats.Where(m => m.shader).Select(m => m.shader.name + "|" + string.Join(",", m.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal))));
            static string E(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var sb = new StringBuilder("{\n");
            sb.Append($"  \"schema\": \"suborned.074-census/1\",\n  \"tag\": \"{tag}\",\n  \"when\": \"{DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}\",\n  \"export\": \"{E(export)}\",\n");
            sb.Append($"  \"views\": [{string.Join(", ", c.views.Select(v => $"{{\"id\": \"{v.Id}\", \"focus\": [{v.Focus.x.ToString(CultureInfo.InvariantCulture)}, {v.Focus.y.ToString(CultureInfo.InvariantCulture)}], \"yaw\": {v.Yaw.ToString(CultureInfo.InvariantCulture)}, \"ortho\": {ZoomOf(v).ToString(CultureInfo.InvariantCulture)}, \"pitch\": {Baseline072.PlayPitch.ToString(CultureInfo.InvariantCulture)}, \"standOff\": {c.standOff.ToString(CultureInfo.InvariantCulture)}}}"))}],\n");
            sb.Append($"  \"columns\": {{\"placed\": {columns}, \"singleModules\": {singles}, \"layoutPrefabsPlaced\": {colPrefabs.Count}, \"layoutPrefabsInFolder\": {folderPrefabs}, \"meshAssetsInFolder\": {folderMeshes}, \"distinctMeshesPlaced\": {colMeshes.Count}, \"trianglesAllLodsOfPlacedMeshes\": {colTris}}},\n");
            sb.Append($"  \"materials\": {{\"scene\": {sceneMats.Count}, \"modules\": {modMats.Count}, \"shadowProxies\": {proxyMats.Count}, \"sceneShaderVariants\": {shaders.Count}, \"moduleShaderVariants\": {modShaders.Count}}},\n");
            sb.Append($"  \"shadowProxies\": {{\"all\": {proxies}, \"modules\": {modProxies}, \"materials\": [{string.Join(", ", proxyMats.Select(m => m.name).OrderBy(n => n, StringComparer.Ordinal).Select(n => $"\"{E(n)}\""))}]}},\n");
            sb.Append($"  \"moduleShaderVariants\": [{string.Join(", ", modShaders.OrderBy(n => n, StringComparer.Ordinal).Select(n => $"\"{E(n)}\""))}],\n");
            sb.Append($"  \"moduleMaterials\": [{string.Join(", ", modMats.Select(m => m.name).OrderBy(n => n, StringComparer.Ordinal).Select(n => $"\"{E(n)}\""))}],\n");
            sb.Append($"  \"moduleRootsByWallMaterial\": {{{string.Join(", ", wallUse.Select(kv => $"\"{E(kv.Key)}\": {kv.Value}"))}}}\n}}\n");
            File.WriteAllText(file, sb.ToString());
            Debug.Log($"074 census ({tag}): {columns} columns in {colPrefabs.Count} layouts ({folderPrefabs} prefabs, {colMeshes.Count} meshes placed), {singles} single modules; materials scene {sceneMats.Count}, modules {modMats.Count}, proxies {proxyMats.Count} → {file}");
        }
    }
}
