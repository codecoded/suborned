using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Play
{
    [Serializable]
    public sealed class DistrictCrowdFigure
    {
        public string id;
        public Mesh[] poses;
        public Material[] materials;
    }

    // A distant resident still has their own position and walk phase. All instances of one
    // dressed figure in the same pose share a draw, with no Animator or GameObject per resident.
    sealed class DistrictCrowdLod
    {
        sealed class Batch
        {
            public DistrictCrowdFigure figure;
            public Mesh mesh;
            public int count, triangles;
            public Matrix4x4[] instances = new Matrix4x4[8];
        }

        readonly Dictionary<string, Batch[]> skins = new();
        readonly List<Batch> used = new();
        public int SubmittedTriangles { get; private set; }
        public DistrictCrowdLod(DistrictCrowdFigure[] figures)
        {
            foreach (var f in figures)
            {
                var poses = new Batch[f.poses.Length];
                for (int i = 0; i < poses.Length; i++)
                {
                    var mesh = f.poses[i];
                    int triangles = 0;
                    for (int s = 0; s < mesh.subMeshCount; s++) triangles += (int)mesh.GetIndexCount(s) / 3;
                    poses[i] = new Batch { figure = f, mesh = mesh, triangles = triangles };
                }
                skins[f.id] = poses;
            }
        }
        public bool Has(string skin) => skin != null && skins.ContainsKey(skin) && SystemInfo.supportsInstancing;
        public void Add(string skin, Matrix4x4 matrix, int tick, int id, bool moving)
        {
            var poses = skins[skin];
            int phase = moving ? 1 + (tick + id * 7) % (poses.Length - 1) : 0;
            var b = poses[phase];
            if (b.count == 0) used.Add(b);
            if (b.count == b.instances.Length) Array.Resize(ref b.instances, b.count * 2);
            b.instances[b.count++] = matrix;
        }
        public void Draw()
        {
            SubmittedTriangles = 0;
            foreach (var b in used)
            {
                SubmittedTriangles += b.count * b.triangles;
                for (int s = 0; s < b.mesh.subMeshCount; s++)
                {
                    var rp = new RenderParams(b.figure.materials[s])
                    {
                        shadowCastingMode = ShadowCastingMode.Off,
                        receiveShadows = true,
                        lightProbeUsage = LightProbeUsage.Off
                    };
                    for (int i = 0; i < b.count; i += 511)
                        Graphics.RenderMeshInstanced(rp, b.mesh, s, b.instances, Math.Min(511, b.count - i), i);
                }
                b.count = 0;
            }
            used.Clear();
        }
    }
}
