using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Suborned.Play
{
    // The Old Town swap keeps pieces of the original tiled pavements outside the civic walk file.
    // Bake their actual top faces once. Grounding a wheel in play is a single bit lookup.
    public sealed class DistrictPaving : MonoBehaviour
    {
        const float PerMetre = 4f;
        public const float Top = .185f;
        [SerializeField] int x0, z0, width, depth;
        [SerializeField] byte[] raised = Array.Empty<byte>();

        void Awake() => GetComponent<HandoverHost>().frame.paving = this;

        public bool Contains(Vector2 plan)
        {
            int x = Mathf.FloorToInt(plan.x * PerMetre) - x0, z = Mathf.FloorToInt(plan.y * PerMetre) - z0;
            if ((uint)x >= (uint)width || (uint)z >= (uint)depth) return false;
            int bit = z * width + x;
            return (raised[bit >> 3] & (1 << (bit & 7))) != 0;
        }

        public void Bake(Scene scene)
        {
            var surfaces = new List<MeshFilter>();
            Bounds bounds = default;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.sharedMesh || !filter.transform.parent ||
                    !filter.transform.parent.name.EndsWith("tiles merged", StringComparison.OrdinalIgnoreCase) ||
                    !filter.TryGetComponent<MeshRenderer>(out var renderer) || !renderer.enabled) continue;
                if (surfaces.Count == 0) bounds = renderer.bounds;
                else bounds.Encapsulate(renderer.bounds);
                surfaces.Add(filter);
            }
            if (surfaces.Count == 0) { width = depth = 0; raised = Array.Empty<byte>(); return; }
            x0 = Mathf.FloorToInt(bounds.min.x * PerMetre);
            z0 = Mathf.FloorToInt(bounds.min.z * PerMetre);
            width = Mathf.CeilToInt(bounds.max.x * PerMetre) - x0 + 1;
            depth = Mathf.CeilToInt(bounds.max.z * PerMetre) - z0 + 1;
            raised = new byte[(width * depth + 7) / 8];
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            foreach (var surface in surfaces)
            {
                var mesh = surface.sharedMesh;
                var materials = surface.GetComponent<MeshRenderer>().sharedMaterials;
                var matrix = surface.transform.localToWorldMatrix;
                mesh.GetVertices(vertices);
                for (int i = 0; i < vertices.Count; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
                for (int sub = 0; sub < mesh.subMeshCount && sub < materials.Length; sub++)
                {
                    if (!materials[sub] || !materials[sub].name.StartsWith("DK_Pave", StringComparison.Ordinal)) continue;
                    mesh.GetTriangles(triangles, sub);
                    for (int i = 0; i < triangles.Count; i += 3)
                        Stamp(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
                }
            }
        }

        void Stamp(Vector3 a, Vector3 b, Vector3 c)
        {
            // Submeshes also contain kerb sides, lower faces and road-level tiles.
            if (Mathf.Min(a.y, Mathf.Min(b.y,c.y)) < .17f || Mathf.Max(a.y, Mathf.Max(b.y,c.y)) > .20f) return;
            float abX = b.x-a.x, abZ = b.z-a.z, acX = c.x-a.x, acZ = c.z-a.z;
            float area = abX*acZ - abZ*acX;
            if (Mathf.Abs(area) < .00001f) return;
            int left = Mathf.Max(x0, Mathf.CeilToInt(Mathf.Min(a.x, Mathf.Min(b.x,c.x))*PerMetre-.5f));
            int right = Mathf.Min(x0+width-1, Mathf.FloorToInt(Mathf.Max(a.x, Mathf.Max(b.x,c.x))*PerMetre-.5f));
            int near = Mathf.Max(z0, Mathf.CeilToInt(Mathf.Min(a.z, Mathf.Min(b.z,c.z))*PerMetre-.5f));
            int far = Mathf.Min(z0+depth-1, Mathf.FloorToInt(Mathf.Max(a.z, Mathf.Max(b.z,c.z))*PerMetre-.5f));
            for (int z = near; z <= far; z++)
            for (int x = left; x <= right; x++)
            {
                float dx = (x+.5f)/PerMetre-a.x, dz = (z+.5f)/PerMetre-a.z;
                float u = (dx*acZ-dz*acX)/area, v = (abX*dz-abZ*dx)/area;
                if (u < -.0001f || v < -.0001f || u+v > 1.0001f) continue;
                int bit = (z-z0)*width + x-x0;
                raised[bit >> 3] |= (byte)(1 << (bit & 7));
            }
        }
    }
}
