using System.Collections.Generic;
using System.Linq;
using Suborned.Play;
using UnityEditor.Build;
using UnityEngine;

namespace Suborned.World.Play
{
    static class DistrictCrowdBake
    {
        public static DistrictCrowdFigure[] Make(FigureLibrary library)
        {
            if (!library) throw new BuildFailedException("Missing Harbour figure library.");
            var result = new List<DistrictCrowdFigure>();
            var materials = new Dictionary<Material, Material>();
            var keys = new HashSet<string>(library.poolIds.SelectMany(list => list.Split(',')));
            foreach (var f in library.figures.Where(f => keys.Contains(f.id) && f.model))
            {
                var instance = Object.Instantiate(f.model);
                try
                {
                    var lod = instance.GetComponentInChildren<LODGroup>();
                    if (!lod || lod.lodCount == 0) continue;
                    var far = lod.GetLODs().Last().renderers.OfType<SkinnedMeshRenderer>().ToArray();
                    if (far.Length == 0) continue;
                    var anim = instance.GetComponent<Animator>();
                    if (!anim) continue;
                    anim.runtimeAnimatorController = library.ControllerFor(f);
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    anim.Rebind();
                    var baked = new DistrictCrowdFigure { id = f.id, poses = new Mesh[7] };
                    baked.materials = far.SelectMany(r => r.sharedMaterials.Take(r.sharedMesh.subMeshCount)).Select(m =>
                    {
                        if (!materials.TryGetValue(m, out var instanced))
                        {
                            instanced = new Material(m) { name = m.name + " (district instances)", enableInstancing = true };
                            materials.Add(m, instanced);
                        }
                        return instanced;
                    }).ToArray();
                    for (int i = 0; i < baked.poses.Length; i++)
                    {
                        bool walk = i > 0;
                        anim.Play(walk ? "walk" : "idle", 0, walk ? (i - 1) / 6f : .37f);
                        anim.Update(0f);
                        var parts = new List<CombineInstance>(); var source = new List<Mesh>();
                        foreach (var r in far)
                        {
                            var mesh = new Mesh(); r.BakeMesh(mesh); source.Add(mesh);
                            for (int sub = 0; sub < mesh.subMeshCount; sub++)
                                parts.Add(new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = instance.transform.worldToLocalMatrix * r.localToWorldMatrix });
                        }
                        var pose = new Mesh { name = f.id + (walk ? " walk " + i : " idle") };
                        pose.CombineMeshes(parts.ToArray(), false, true);
                        pose.RecalculateBounds();
                        foreach (var scratch in source) Object.DestroyImmediate(scratch);
                        if (pose.subMeshCount != baked.materials.Length) throw new BuildFailedException("Incomplete crowd materials for " + f.id);
                        baked.poses[i] = pose;
                    }
                    result.Add(baked);
                }
                finally { Object.DestroyImmediate(instance); }
            }
            if (result.Count == 0) throw new BuildFailedException("Could not bake the lowest citizen LODs.");
            return result.ToArray();
        }
    }
}
