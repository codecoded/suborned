using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World
{
    /// <summary>Shadow proxies (brief 008 B6, Redline fix 2; Suborned.ShadowProxy): every kit facade (under the world's `facades`
    /// layer) and street tree (a `tree-*` element) with a LOD1 casts its shadow from a shadows-only renderer carrying its LOD1 mesh and
    /// materials (alpha-tested leaves keep their cut-out), and none of its LODs casts. Run after FadeTags: the proxy takes its LOD1's
    /// rendering layers, so the architecture fade's reach cuts its shadow like the piece's. The perf harness switches them off to
    /// measure the scene as it was.</summary>
    public static class ShadowProxies
    {
        public const string Name = "__ShadowProxy";

        /// <summary>Adds the proxies under `world`; returns their count and LOD1 triangles.</summary>
        public static (int count, long triangles) Build(Transform world)
        {
            int count = 0; long tris = 0;
            foreach (var g in world.GetComponentsInChildren<LODGroup>(true))
            {
                if (!Wanted(g.transform, world)) continue;
                var lods = g.GetLODs();
                if (lods.Length < 2) continue;
                Renderer src = null; Mesh mesh = null;
                foreach (var r in lods[1].renderers)
                    if (r && r.TryGetComponent<MeshFilter>(out var f) && f.sharedMesh) { src = r; mesh = f.sharedMesh; break; }
                if (!src) continue;
                var casters = new List<Renderer>(); var was = new List<ShadowCastingMode>();
                foreach (var lod in lods) foreach (var r in lod.renderers) if (r && !casters.Contains(r)) { casters.Add(r); was.Add(r.shadowCastingMode); }
                var go = new GameObject(Name);
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
                var sp = go.AddComponent<ShadowProxy>();
                sp.proxy = p; sp.casters = casters.ToArray(); sp.was = was.ToArray();
                sp.Use(true);
                count++;
                for (int s = 0; s < mesh.subMeshCount; s++) tris += mesh.GetIndexCount(s) / 3;
            }
            return (count, tris);
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
    }
}
