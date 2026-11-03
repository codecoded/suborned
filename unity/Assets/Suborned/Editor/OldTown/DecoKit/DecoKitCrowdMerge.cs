#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Folds each placed citizen or car (a glTF hierarchy of ~25 part renderers) into one renderer on the agent itself:
    /// its parts combined, one submesh per material, with generated Mesh LODs. Agents of one source and material
    /// pattern share a mesh, and each keeps its own (tinted) materials, so the city reads the same.
    internal static class DecoKitCrowdMerge
    {
        const string Folder = "Assets/Suborned/OldTownArt/DecoKit/Generated/Crowd";

        /// Returns the number of part renderers removed.
        internal static int Merge(Transform crowd)
        {
            var cache = new Dictionary<string, Mesh>(); int removed = 0;
            var folder = $"{Folder}/{crowd.name}";
            AssetDatabase.DeleteAsset(folder);
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(Folder), "Crowd");
            AssetDatabase.CreateFolder(Folder, crowd.name);
            foreach (Transform agent in crowd)
            {
                var parts = agent.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.transform != agent)
                    .Select(r => (r, f: r.GetComponent<MeshFilter>())).Where(p => p.f && p.f.sharedMesh).ToArray();
                if (parts.Length < 2) continue;
                // Each part submesh goes to the slot of its material's first use; the pattern keys the shared mesh.
                var materials = new List<Material>(); var slots = new List<(MeshFilter f, int sub, int slot)>();
                foreach (var (r, f) in parts)
                    for (int s = 0; s < f.sharedMesh.subMeshCount; s++)
                    {
                        var m = r.sharedMaterials[Mathf.Min(s, r.sharedMaterials.Length - 1)];
                        int slot = materials.IndexOf(m); if (slot < 0) { slot = materials.Count; materials.Add(m); }
                        slots.Add((f, s, slot));
                    }
                var key = string.Join(",", slots.Select(p => $"{p.f.sharedMesh.GetEntityId()}.{p.sub}.{p.slot}"));
                if (!cache.TryGetValue(key, out var mesh))
                {
                    cache[key] = mesh = Combine(agent, slots, materials.Count); // the scene refers to it, not carries it
                    AssetDatabase.CreateAsset(mesh, $"{folder}/{mesh.name} {cache.Count}.asset");
                }
                var shadows = parts[0].r.shadowCastingMode;
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(agent.gameObject)) PrefabUtility.UnpackPrefabInstance(agent.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                for (int i = agent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(agent.GetChild(i).gameObject);
                agent.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = agent.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = materials.ToArray(); renderer.shadowCastingMode = shadows;
                removed += parts.Length - 1;
            }
            return removed;
        }

        /// One combine (a submesh per part submesh), then the submeshes regrouped by material slot.
        internal static Mesh Combine(Transform agent, List<(MeshFilter f, int sub, int slot)> slots, int count)
        {
            var toAgent = agent.worldToLocalMatrix;
            var mesh = new Mesh { name = agent.name + " merged", indexFormat = slots.Sum(p => p.f.sharedMesh.vertexCount) > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.CombineMeshes(slots.Select(p => new CombineInstance { mesh = p.f.sharedMesh, subMeshIndex = p.sub, transform = toAgent * p.f.transform.localToWorldMatrix }).ToArray(), false, true);
            var bySlot = Enumerable.Range(0, count).Select(k => Enumerable.Range(0, slots.Count).Where(i => slots[i].slot == k).SelectMany(i => mesh.GetIndices(i)).ToArray()).ToArray();
            mesh.subMeshCount = count;
            for (int k = 0; k < count; k++) mesh.SetIndices(bySlot[k], MeshTopology.Triangles, k, false);
            mesh.RecalculateBounds();
            MeshLodUtility.GenerateMeshLods(mesh, 6);
            return mesh;
        }
    }
}
#endif
