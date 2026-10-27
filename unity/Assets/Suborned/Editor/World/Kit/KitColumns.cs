using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World
{
    /// <summary>Brief 072-E (Redline's plan 3.2, "bake stack columns"): the every-face modules that share an export `column` (the storeys of one
    /// bay position on one face: SideBayStack Base + Mid × k + Top, pier storeys, pilasters) become one prefab instance per column, its mesh
    /// baked once per column layout: (piece, offset and scale of each module relative to the lowest, colourway). A layout is one prefab under
    /// Built/Columns/ with an LODGroup (class `column`: switch distances from kit.json lodClasses against the baked column's own size) and one
    /// renderer per level, a submesh per material, so the GPU Resident Drawer still instances repeats and the architecture fade still reaches
    /// one bay at a time. Nothing is combined across columns, nothing static-batched. A column of one module, or one whose modules turn
    /// differently, is placed as the plain piece. Every module keeps its export element (Inspect counts them); the column stands under its
    /// lowest module's element and the others register their own bounds. Static per build.</summary>
    public static class KitColumns
    {
        public const string Folder = KitPlacer.BuiltRoot + "Columns";
        public static int Columns, Modules, Variants, Singles, Tris0, Index16, Index32;
        /// <summary>The LOD groups of every placed module (baked columns and single modules): ShadowProxies casts them from L2.</summary>
        public static readonly HashSet<LODGroup> ModuleGroups = new();
        /// <summary>Every placed module root (the switch `everyFace` hides them).</summary>
        public static readonly List<GameObject> ModuleRoots = new();

        sealed class Variant { public GameObject Prefab; public int Tris0; }
        static readonly Dictionary<string, Variant> variants = new(System.StringComparer.Ordinal);

        public static void Reset()
        {
            Columns = Modules = Variants = Singles = Tris0 = Index16 = Index32 = 0; ModuleGroups.Clear(); ModuleRoots.Clear(); variants.Clear();
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder); // rebuilt from the export every build
            AssetDatabase.CreateFolder(KitPlacer.BuiltRoot.TrimEnd('/'), "Columns");
        }

        /// <summary>Places the facades layer: every facade gets its element; modules of a column of two or more are baked and placed as one.</summary>
        public static void Place(Transform layer, List<FacadeDto> facades, KitPlacer placer, float[] columnDistances)
        {
            var els = new Dictionary<FacadeDto, Transform>();
            foreach (var f in facades) els[f] = Nodes.Element(layer, f.Id); // export order, as before
            var groups = facades.Where(f => f.Module && f.Column != null).GroupBy(f => f.Column, System.StringComparer.Ordinal)
                                .Where(g => g.Count() > 1).ToDictionary(g => g.Key, g => g.OrderBy(f => f.Base).ThenBy(f => f.Id, System.StringComparer.Ordinal).ToList(), System.StringComparer.Ordinal);
            var done = new HashSet<FacadeDto>();
            foreach (var f in facades)
            {
                if (done.Contains(f)) continue;
                if (f.Module && f.Column != null && groups.TryGetValue(f.Column, out var col) && Bakeable(col))
                {
                    PlaceColumn(layer, col, els, placer, columnDistances);
                    foreach (var m in col) done.Add(m);
                    continue;
                }
                var t = placer.Place(els[f], layer.name, f.Kit, placer.PivotForCentre(f.Kit, f.Kit.At), f.Base, 0f);
                if (f.Module)
                {
                    Singles++; Modules++; ModuleRoots.Add(t.gameObject);
                    if (t.TryGetComponent<LODGroup>(out var lg)) ModuleGroups.Add(lg);
                }
                done.Add(f);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"World: every-face modules {Modules}: {Columns} baked columns ({Modules - Singles} modules) in {Variants} column layouts (Built/Columns, one LODGroup each, class `column` at {string.Join(" / ", columnDistances.Select(d => d.ToString("0", CultureInfo.InvariantCulture)))} m), {Singles} single modules placed as their piece; baked meshes {Index16} 16-bit and {Index32} 32-bit indices, saved not CPU-readable (F3)");
        }

        static bool Bakeable(List<FacadeDto> col)
        {
            float y = col[0].Kit.Yaw;
            return col.All(m => Mathf.Abs(Mathf.DeltaAngle(m.Kit.Yaw * Mathf.Rad2Deg, y * Mathf.Rad2Deg)) < 0.01f && m.Colourway == col[0].Colourway);
        }

        static string R(float v) => v.ToString("0.000", CultureInfo.InvariantCulture);

        static void PlaceColumn(Transform layer, List<FacadeDto> col, Dictionary<FacadeDto, Transform> els, KitPlacer placer, float[] dist)
        {
            var rot = KitPlacer.Rotation(col[0].Kit.Yaw);
            var p0 = placer.PivotForCentre(col[0].Kit, col[0].Kit.At);
            var origin = new Vector3(p0.x, col[0].Base, p0.y);
            var cs = new Vector3(col[0].Kit.Scale.x, 1f, 1f);
            var frame = Matrix4x4.TRS(origin, rot, cs);
            var inv = frame.inverse;
            var key = new StringBuilder(col[0].Colourway ?? "base");
            var locals = new List<(string id, Matrix4x4 m)>();
            foreach (var m in col)
            {
                var p = placer.PivotForCentre(m.Kit, m.Kit.At);
                var world = Matrix4x4.TRS(new Vector3(p.x, m.Base, p.y), rot, m.Kit.Scale);
                var local = inv * world;
                // rounded to the millimetre so equal layouts share one bake
                var lp = local.GetColumn(3); var ls = new Vector3(m.Kit.Scale.x / cs.x, m.Kit.Scale.y, m.Kit.Scale.z);
                lp = new Vector3(float.Parse(R(lp.x), CultureInfo.InvariantCulture), float.Parse(R(lp.y), CultureInfo.InvariantCulture), float.Parse(R(lp.z), CultureInfo.InvariantCulture));
                ls = new Vector3(float.Parse(R(ls.x), CultureInfo.InvariantCulture), float.Parse(R(ls.y), CultureInfo.InvariantCulture), float.Parse(R(ls.z), CultureInfo.InvariantCulture));
                key.Append('|').Append(m.Kit.Id).Append('@').Append(R(lp.x)).Append(',').Append(R(lp.y)).Append(',').Append(R(lp.z)).Append('x').Append(R(ls.x)).Append(',').Append(R(ls.y)).Append(',').Append(R(ls.z));
                locals.Add((m.Kit.Id, Matrix4x4.TRS(lp, Quaternion.identity, ls)));
                // the module's own element keeps its bounds and triangles (Inspect, the fade cells, Rig)
                Combine.Kit(els[m], WorldBounds(placer.LocalBounds(m.Kit.Id), world), Lod0Tris(placer.Prefab(m.Kit.Id)));
            }
            if (!variants.TryGetValue(key.ToString(), out var v)) variants[key.ToString()] = v = Bake(key.ToString(), locals, placer, dist);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(v.Prefab, els[col[0]]);
            go.transform.SetPositionAndRotation(origin, rot);
            go.transform.localScale = cs; // the column's element has scale 1 at the origin until Combine.Flush moves it (children keep their world place)
            if (go.TryGetComponent<LODGroup>(out var lg)) ModuleGroups.Add(lg);
            ModuleRoots.Add(go);
            placer.Count(layer.name, col.Count, v.Tris0);
            Columns++; Modules += col.Count; Tris0 += v.Tris0;
        }

        static Bounds WorldBounds(Bounds b, Matrix4x4 m)
        {
            var r = new Bounds(m.MultiplyPoint3x4(b.min), Vector3.zero);
            for (int i = 0; i < 8; i++)
                r.Encapsulate(m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return r;
        }

        static int Lod0Tris(GameObject prefab)
        {
            int n = 0;
            foreach (var mr in KitLods.Lod0(prefab)) { var mesh = mr.GetComponent<MeshFilter>().sharedMesh; for (int i = 0; i < mesh.subMeshCount; i++) n += (int)mesh.GetIndexCount(i) / 3; }
            return n;
        }

        /// <summary>The renderers of each LOD level of a placed piece's prefab (LODGroup levels; LOD0 only when it has none).</summary>
        static List<Renderer[]> Levels(GameObject prefab)
        {
            if (prefab.TryGetComponent<LODGroup>(out var lg))
                return lg.GetLODs().Select(l => l.renderers.Where(r => r).ToArray()).ToList();
            return new List<Renderer[]> { KitLods.Lod0(prefab).Cast<Renderer>().ToArray() };
        }

        static Variant Bake(string key, List<(string id, Matrix4x4 m)> locals, KitPlacer placer, float[] dist)
        {
            string hash;
            using (var sha = SHA1.Create()) hash = string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(key)).Take(6).Select(b => b.ToString("x2")));
            string name = $"Column_{locals.Count}_{hash}";
            var levels = locals.Select(l => (l.m, root: placer.Prefab(l.id).transform, lv: Levels(placer.Prefab(l.id)))).ToList();
            int count = levels.Max(l => l.lv.Count);
            var go = new GameObject(name);
            var lodRenderers = new List<Renderer>();
            Renderer template = levels[0].lv[0][0];
            int tris0 = 0;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    // one mesh per material across the column's modules, then one submesh per material
                    var byMat = new SortedDictionary<string, (Material mat, List<CombineInstance> parts)>(System.StringComparer.Ordinal);
                    foreach (var (m, root, lv) in levels)
                        foreach (var r in lv[Mathf.Min(i, lv.Count - 1)])
                        {
                            var mesh = r.GetComponent<MeshFilter>().sharedMesh; var mats = r.sharedMaterials;
                            var xf = m * (root.worldToLocalMatrix * r.transform.localToWorldMatrix);
                            for (int s = 0; s < mesh.subMeshCount; s++)
                            {
                                var mat = mats[Mathf.Min(s, mats.Length - 1)];
                                var k = mat ? mat.name + "#" + AssetDatabase.GetAssetPath(mat) : "";
                                if (!byMat.TryGetValue(k, out var e)) byMat[k] = e = (mat, new List<CombineInstance>());
                                e.parts.Add(new CombineInstance { mesh = mesh, subMeshIndex = s, transform = xf });
                            }
                        }
                    var perMat = new List<CombineInstance>(); var matList = new List<Material>(); var temps = new List<Mesh>();
                    foreach (var (mat, parts) in byMat.Values)
                    {
                        var one = new Mesh { indexFormat = IndexFormat.UInt32 };
                        one.CombineMeshes(parts.ToArray(), true, true);
                        temps.Add(one); perMat.Add(new CombineInstance { mesh = one, transform = Matrix4x4.identity }); matList.Add(mat);
                    }
                    // Redline phase 2 F3: 16-bit indices when the column's vertices allow (all do today: the largest L0 is 23,768), and the asset
                    // saved not CPU-readable, so the player keeps only the GPU copy (the builder reads only index counts, taken before).
                    // CombineMeshes writes 32-bit indices when a source has them (the per-material meshes above may pass 65,535 before they are
                    // trimmed), so the format is set after the combine and the indices put back in it.
                    var baked = new Mesh { name = $"{name}_L{i}", indexFormat = IndexFormat.UInt32 };
                    baked.CombineMeshes(perMat.ToArray(), false, false);
                    foreach (var t in temps) Object.DestroyImmediate(t);
                    if (baked.vertexCount < 65535 && baked.indexFormat == IndexFormat.UInt32)
                    {
                        var subs = new int[baked.subMeshCount][];
                        for (int s = 0; s < subs.Length; s++) subs[s] = baked.GetIndices(s);
                        baked.indexFormat = IndexFormat.UInt16; // clears the index data and the submeshes
                        baked.subMeshCount = subs.Length;
                        for (int s = 0; s < subs.Length; s++) baked.SetIndices(subs[s], MeshTopology.Triangles, s, false, 0);
                        for (int s = 0; s < subs.Length; s++) if (baked.GetIndexCount(s) != subs[s].Length) throw new InvalidDataException($"World: column {name} L{i} submesh {s} lost indices going 16-bit");
                    }
                    baked.RecalculateBounds();
                    if (baked.indexFormat == IndexFormat.UInt16) Index16++; else Index32++;
                    if (i == 0) for (int s = 0; s < baked.subMeshCount; s++) tris0 += (int)baked.GetIndexCount(s) / 3;
                    AssetDatabase.CreateAsset(baked, $"{Folder}/{name}_L{i}.asset");
                    var so = new SerializedObject(baked); so.FindProperty("m_IsReadable").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(baked);
                    var child = new GameObject(i == 0 ? name + "_L0" : "__LOD" + i);
                    child.transform.SetParent(go.transform, false);
                    GameObjectUtility.SetStaticEditorFlags(child, GameObjectUtility.GetStaticEditorFlags(template.gameObject));
                    child.layer = template.gameObject.layer;
                    child.AddComponent<MeshFilter>().sharedMesh = baked;
                    var mr = child.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = matList.ToArray();
                    mr.shadowCastingMode = template.shadowCastingMode; mr.receiveShadows = template.receiveShadows;
                    mr.lightProbeUsage = template.lightProbeUsage; mr.reflectionProbeUsage = template.reflectionProbeUsage;
                    mr.motionVectorGenerationMode = template.motionVectorGenerationMode; mr.allowOcclusionWhenDynamic = template.allowOcclusionWhenDynamic;
                    lodRenderers.Add(mr);
                }
                // class `column`: L1, L2 and the cull by distance from the baked column's own size (its height, for a stack)
                var lg = go.AddComponent<LODGroup>();
                lg.fadeMode = LODFadeMode.None;
                var b = new Bounds(lodRenderers[0].bounds.center, Vector3.zero); b.Encapsulate(lodRenderers[0].bounds);
                float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
                var lods = new List<LOD>();
                for (int i = 0; i < lodRenderers.Count; i++)
                {
                    // the class is [L1, L2, cull]: the last level ends at the cull (distances[2]) whatever the level count
                    int di = i == lodRenderers.Count - 1 ? 2 : i;
                    float d = di < dist.Length ? dist[di] : 0f;
                    float screen = d > 0f ? KitLods.ScreenAt(size, d) : 0.004f;
                    if (lods.Count > 0 && screen >= lods[^1].screenRelativeTransitionHeight) screen = lods[^1].screenRelativeTransitionHeight * 0.5f;
                    lods.Add(new LOD(screen, new[] { lodRenderers[i] }));
                }
                lg.SetLODs(lods.ToArray());
                lg.RecalculateBounds();
                var path = $"{Folder}/{name}.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, path) ?? throw new IOException($"World: could not save {path}");
                Variants++;
                return new Variant { Prefab = prefab, Tris0 = tris0 };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
