#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The 2 m ground modules (flags, kerbs, tram rail) are ~45% of what the boulevard camera draws (DecoKitDrawCost,
    /// city_g): one renderer per tile, kerbs with four materials each. Merge their meshes into square chunks with one
    /// submesh per material; the tiles keep their colliders, their renderers are switched off.
    internal static class DecoKitTileMerge
    {
        const float Chunk = 48f;
        static readonly string[] Tiles = { "DK_Pave_", "DK_Kerb_", "DK_TreePit_", "DK_Tile_TramRail" };

        /// `name` names the merged group and its mesh asset (one per call site: a second merge must not replace the first's).
        internal static void Merge(Transform root, string name = "Ground tiles merged", Transform parentTo = null)
        {
            var parts = new Dictionary<Vector2Int, Dictionary<Material, List<CombineInstance>>>();
            int tiles = 0;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                var piece = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
                var mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
                if (!piece || !mesh || !r.enabled || !Tiles.Any(t => piece.name.StartsWith(t))) continue;
                var p = r.bounds.center;
                var key = new Vector2Int(Mathf.FloorToInt(p.x / Chunk), Mathf.FloorToInt(p.z / Chunk));
                if (!parts.TryGetValue(key, out var byMat)) parts[key] = byMat = new();
                for (int i = 0; i < mesh.subMeshCount && i < r.sharedMaterials.Length; i++)
                {
                    var mat = r.sharedMaterials[i];
                    if (!byMat.TryGetValue(mat, out var list)) byMat[mat] = list = new();
                    list.Add(new CombineInstance { mesh = mesh, subMeshIndex = i, transform = r.localToWorldMatrix });
                }
                r.enabled = false;
                tiles++;
            }

            var parent = new GameObject(name).transform;
            if (parentTo) parent.SetParent(parentTo, false);
            var path = $"{GeneratedDir}/{name.Replace(' ', '_')}.asset";
            AssetDatabase.DeleteAsset(path);
            Mesh container = null;
            int submeshes = 0;
            foreach (var (key, byMat) in parts.OrderBy(kv => kv.Key.x).ThenBy(kv => kv.Key.y))
            {
                var mats = byMat.Keys.ToArray();
                var perMat = mats.Select(m =>
                {
                    var one = new Mesh { indexFormat = IndexFormat.UInt32 };
                    one.CombineMeshes(byMat[m].ToArray(), true, true);
                    return one;
                }).ToArray();
                var mesh = new Mesh { name = $"Tiles {key.x} {key.y}", indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(perMat.Select(m => new CombineInstance { mesh = m, transform = Matrix4x4.identity }).ToArray(), false, false);
                foreach (var m in perMat) Object.DestroyImmediate(m);
                if (container == null) AssetDatabase.CreateAsset(container = mesh, path);
                else AssetDatabase.AddObjectToAsset(mesh, container);

                var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(parent, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterials = mats;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
                submeshes += mats.Length;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[DecoKit] tile merge: {tiles} tile renderers -> {parts.Count} chunks, {submeshes} submeshes");
        }
    }
}
#endif
