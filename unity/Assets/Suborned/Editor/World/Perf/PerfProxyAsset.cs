using System.Collections.Generic;
using Suborned.Perf;
using UnityEditor;
using UnityEngine;

namespace Suborned.World.Perf
{
    /// <summary>The perf player's shadow-proxy meshes (PerfProxyMeshes): every kit facade's and street tree's LOD1 mesh, keyed by its
    /// LOD group's sibling-index path, written into a Resources folder just before PerfBuild builds the player and deleted just after,
    /// so no other build carries it and the project is left as it was.</summary>
    public static class PerfProxyAsset
    {
        public const string Folder = "Assets/Suborned/PerfBuildTemp";
        const string Path = Folder + "/Resources/" + PerfProxyMeshes.ResourceName + ".asset";

        public static int Write(Transform world)
        {
            var groups = new List<string>(); var meshes = new List<Mesh>();
            foreach (var g in world.GetComponentsInChildren<LODGroup>(true))
            {
                if (!PerfProxies.Wanted(g.transform, world)) continue;
                var lods = g.GetLODs();
                if (lods.Length < 2) continue;
                foreach (var r in lods[1].renderers)
                    if (r && r.TryGetComponent<MeshFilter>(out var f) && f.sharedMesh) { groups.Add(PerfProxies.PathOf(g.transform, world)); meshes.Add(f.sharedMesh); break; }
            }
            Delete();
            System.IO.Directory.CreateDirectory(Folder + "/Resources");
            AssetDatabase.Refresh();
            var table = ScriptableObject.CreateInstance<PerfProxyMeshes>();
            table.groups = groups.ToArray(); table.meshes = meshes.ToArray();
            AssetDatabase.CreateAsset(table, Path);
            AssetDatabase.SaveAssets();
            return groups.Count;
        }

        public static void Delete()
        {
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
        }
    }
}
