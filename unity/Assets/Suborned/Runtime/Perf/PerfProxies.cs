using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Perf
{
    /// <summary>Shadow proxies (Redline fix 2), as an A/B switch: each kit facade (under the world's `facades` layer) and street tree
    /// (a `tree-*` element) gets a shadows-only renderer with its LOD1 mesh (from PerfProxyMeshes, which the perf player's build
    /// carries) and materials. Made disabled before the first render, so ArchitectureFade's reach takes them in (their shadow is cut
    /// like the rest). While the switch is on no LOD of the piece casts and the proxy does, so its shadow costs LOD1 triangles in
    /// every cascade whatever LOD the camera shows; off = the scene as built.</summary>
    public sealed class PerfProxies
    {
        readonly List<MeshRenderer> proxies = new();
        readonly List<(Renderer r, ShadowCastingMode was)> casters = new();
        bool on;

        public int Count => proxies.Count;
        public int Triangles { get; }
        public int Unresolved { get; }

        public PerfProxies(Transform world)
        {
            var table = Resources.Load<PerfProxyMeshes>(PerfProxyMeshes.ResourceName);
            if (!table) return;
            for (int i = 0; i < table.groups.Length; i++)
            {
                var g = Find(world, table.groups[i]);
                var mesh = table.meshes[i];
                if (!g || !mesh || !g.TryGetComponent<LODGroup>(out var group)) { Unresolved++; continue; }
                var lods = group.GetLODs();
                Renderer src = null;
                foreach (var r in lods[1].renderers) if (r) { src = r; break; }
                if (!src) { Unresolved++; continue; }
                foreach (var lod in lods) foreach (var r in lod.renderers) if (r) casters.Add((r, r.shadowCastingMode));
                var go = new GameObject("__ShadowProxy");
                go.transform.SetParent(src.transform.parent, false);
                go.transform.SetLocalPositionAndRotation(src.transform.localPosition, src.transform.localRotation);
                go.transform.localScale = src.transform.localScale;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var p = go.AddComponent<MeshRenderer>();
                p.sharedMaterials = src.sharedMaterials;
                p.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                p.receiveShadows = false;
                p.lightProbeUsage = LightProbeUsage.Off;
                p.reflectionProbeUsage = ReflectionProbeUsage.Off;
                p.renderingLayerMask = src.renderingLayerMask;
                p.enabled = false;
                proxies.Add(p);
                for (int s = 0; s < mesh.subMeshCount; s++) Triangles += (int)(mesh.GetIndexCount(s) / 3);
            }
        }

        /// <summary>Whether the LOD group `g` under the world root gets a proxy: a kit facade or a street tree.</summary>
        public static bool Wanted(Transform g, Transform world)
        {
            var parent = g.parent ? g.parent.name : "";
            if (parent[(parent.LastIndexOf('/') + 1)..].StartsWith("tree-")) return true;
            var p = g;
            while (p.parent && p.parent != world) p = p.parent;
            return p.parent == world && p.name == "facades";
        }

        /// <summary>A transform's sibling-index path under `root` ("3/0/12"): names hold slashes, indices do not.</summary>
        public static string PathOf(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (; t && t != root; t = t.parent) parts.Add(t.GetSiblingIndex().ToString());
            parts.Reverse();
            return string.Join("/", parts);
        }

        static Transform Find(Transform root, string path)
        {
            var t = root;
            foreach (var part in path.Split('/'))
            {
                if (!int.TryParse(part, out var i) || i >= t.childCount) return null;
                t = t.GetChild(i);
            }
            return t;
        }

        public void Set(bool value)
        {
            if (value == on) return;
            on = value;
            foreach (var (r, was) in casters) if (r) r.shadowCastingMode = on ? ShadowCastingMode.Off : was;
            foreach (var p in proxies) if (p) p.enabled = on;
        }
    }
}
