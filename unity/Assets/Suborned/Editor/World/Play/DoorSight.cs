using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World.Play
{
    /// <summary>GDD v0.24 §10 (Sky picked (h): the Records Office front is played at yaw 210): how much of the public door the play camera sees
    /// from each of its four yaws, for the door marker's turn glyph and H13's door cause (HostDoorView). V-1's measure
    /// (tools/missions/handover/visibility.ts): a 1.75 m figure standing at the door, seen past architecture, without the fade; here the
    /// architecture is the scene's own meshes, the renderers the fade may cut (rendering layer ArchitectureFade.Layer), so what the view hides
    /// is what the player would see hidden. Rays from 18 points of the figure (six heights, three across) toward the camera at play pitch;
    /// a point is seen when no architecture triangle stands on its ray. Editor only (mesh data is readable here, not in a player): the result
    /// is baked on the host at install.</summary>
    public static class DoorSight
    {
        public static float[] Measure(Vector3 door, float baseYaw = 30f, float pitch = 38f)
        {
            var rs = new List<(MeshRenderer r, Mesh m)>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>())
            {
                if (!r.enabled || (r.renderingLayerMask & ArchitectureFade.Layer) == 0) continue;
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                var b = r.bounds; var d = b.center - door; d.y = 0f; if (d.magnitude > 120f + b.extents.magnitude) continue; // 38° from 1.75 m: past 120 m a ray is far over any tower here
                rs.Add((r, mf.sharedMesh));
            }
            var verts = new Dictionary<Mesh, (Vector3[] v, int[] t)>();
            var seen = new float[4];
            for (int k = 0; k < 4; k++)
            {
                float yaw = baseYaw + 90f * k; var q = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 toCam = -(q * Vector3.forward), right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                int n = 0, ok = 0;
                for (int i = 0; i < 6; i++)
                    for (int j = -1; j <= 1; j++)
                    {
                        var p = door + Vector3.up * (0.15f + 0.3f * i) + right * (0.2f * j); var ray = new Ray(p, toCam); n++;
                        bool hit = false;
                        foreach (var (r, m) in rs)
                        {
                            if (!r.bounds.IntersectRay(ray, out var bd) || bd > 400f) continue;
                            if (!verts.TryGetValue(m, out var vt)) verts[m] = vt = (m.vertices, m.triangles);
                            if (Hits(r.transform, vt.v, vt.t, ray)) { hit = true; break; }
                        }
                        if (!hit) ok++;
                    }
                seen[k] = n == 0 ? 0f : ok / (float)n;
            }
            return seen;
        }

        /// <summary>For checks: at view k, each blocked point of the figure (height, across) and the renderer that blocks it.</summary>
        public static string Explain(Vector3 door, int k, float baseYaw = 30f, float pitch = 38f)
        {
            float yaw = baseYaw + 90f * k; var q = Quaternion.Euler(pitch, yaw, 0f); Vector3 toCam = -(q * Vector3.forward), right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            var sb = new System.Text.StringBuilder($"yaw {yaw}:");
            for (int i = 0; i < 6; i++)
                for (int j = -1; j <= 1; j++)
                {
                    var ray = new Ray(door + Vector3.up * (0.15f + 0.3f * i) + right * (0.2f * j), toCam);
                    foreach (var r in Object.FindObjectsByType<MeshRenderer>())
                    {
                        if (!r.enabled || (r.renderingLayerMask & ArchitectureFade.Layer) == 0 || !r.bounds.IntersectRay(ray)) continue;
                        var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                        if (Hits(r.transform, mf.sharedMesh.vertices, mf.sharedMesh.triangles, ray)) { sb.Append($" [{0.15f + 0.3f * i:0.00} m, {0.2f * j:0.0}: {r.transform.parent?.name}/{r.name}]"); break; }
                    }
                }
            return sb.ToString();
        }

        /// <summary>Any triangle of the mesh on the ray (Möller-Trumbore in the renderer's local space; the first metre skipped).</summary>
        static bool Hits(Transform t, Vector3[] v, int[] tri, Ray world)
        {
            var w2l = t.worldToLocalMatrix; Vector3 o = w2l.MultiplyPoint3x4(world.origin), d = w2l.MultiplyVector(world.direction);
            const float skin = 1f; // metres along the world ray (the parameter is the same in both spaces, an affine map): the first metre is the figure's own footing (the entrance steps it stands on; V-1 stands it on the landing)
            for (int i = 0; i + 2 < tri.Length; i += 3)
            {
                Vector3 a = v[tri[i]], e1 = v[tri[i + 1]] - a, e2 = v[tri[i + 2]] - a, pv = Vector3.Cross(d, e2);
                float det = Vector3.Dot(e1, pv); if (det > -1e-9f && det < 1e-9f) continue;
                float inv = 1f / det; var tv = o - a; float u = Vector3.Dot(tv, pv) * inv; if (u < 0f || u > 1f) continue;
                var qv = Vector3.Cross(tv, e1); float w = Vector3.Dot(d, qv) * inv; if (w < 0f || u + w > 1f) continue;
                if (Vector3.Dot(e2, qv) * inv > skin) return true;
            }
            return false;
        }
    }
}
