using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>The export's primitive solids into a MeshKit (world space). Plan frame: c = centre, base = lowest z, yaw radians
    /// from +x toward +y; size (a along yaw, b across, h). Shapes: box | cyl | sphere | vault | saucer | wedge (see
    /// contracts/world/world.schema.ts `Prim`). Faces are oriented outward from the solid's axis, so winding never depends on
    /// the sign of yaw or size. Curved surfaces share vertices around (smooth); flat faces have their own (crisp).</summary>
    public static class PrimKit
    {
        const int Round = 12, Lat = 6, Lens = 3, Arch = 8;

        public static MeshKit Add(MeshKit k, string shape, Vector2 c, float baseZ, Vector3 size, float yaw)
        {
            var f = new Frame(c, baseZ, yaw);
            float a = size.x * 0.5f, b = size.y * 0.5f, h = size.z;
            switch (shape)
            {
                case "box": return k.Block(Poly.Rect(c, f.U, size.x, size.y), baseZ, baseZ + h);
                case "cyl":
                    Revolve(k, f, a, b, new[] { new Vector2(1, 0), new Vector2(1, h) });
                    Disc(k, f, a, b, h, true); Disc(k, f, a, b, 0, false);
                    return k;
                case "sphere":
                {
                    var p = new Vector2[Lat + 1];
                    for (int i = 0; i <= Lat; i++) { float t = Mathf.PI * (i / (float)Lat - 0.5f); p[i] = new Vector2(Mathf.Cos(t), (Mathf.Sin(t) + 1) * 0.5f * h); }
                    return Revolve(k, f, a, b, p);
                }
                case "saucer":
                {
                    // Lens: two shallow paraboloid shells meeting at a crisp rim at mid-height.
                    var lo = new Vector2[Lens + 1]; var hi = new Vector2[Lens + 1];
                    for (int i = 0; i <= Lens; i++)
                    {
                        float t = i / (float)Lens;
                        lo[i] = new Vector2(t, 0.5f * h * t * t);
                        hi[i] = new Vector2(1 - t, h - 0.5f * h * (1 - t) * (1 - t));
                    }
                    return Revolve(Revolve(k, f, a, b, lo), f, a, b, hi);
                }
                case "vault": return Vault(k, f, a, b, h);
                case "wedge": return Wedge(k, f, a, b, h);
                default: throw new System.IO.InvalidDataException($"World: prim shape `{shape}` unknown");
            }
        }

        public static MeshKit Add(MeshKit k, PrimDto p) => Add(k, p.Shape, p.Centre, p.Base, p.Size, p.Yaw);

        /// <summary>Local (s along yaw, r across, z up) → Unity world.</summary>
        readonly struct Frame
        {
            public readonly Vector2 C, U, V; public readonly float Z;
            public Frame(Vector2 c, float z, float yaw) { C = c; Z = z; U = new Vector2(Mathf.Cos(yaw), Mathf.Sin(yaw)); V = new Vector2(-U.y, U.x); }
            public Vector3 P(float s, float r, float z) { var q = C + U * s + V * r; return new Vector3(q.x, Z + z, q.y); }
        }

        /// <summary>Elliptical surface of revolution: profile (radius fraction, height) from bottom to top, Round segments.</summary>
        static MeshKit Revolve(MeshKit k, Frame f, float ra, float rb, Vector2[] profile)
        {
            var rows = new int[profile.Length, Round];
            for (int i = 0; i < profile.Length; i++)
                for (int j = 0; j < Round; j++)
                {
                    float t = 2 * Mathf.PI * j / Round;
                    rows[i, j] = k.Vert(f.P(Mathf.Cos(t) * ra * profile[i].x, Mathf.Sin(t) * rb * profile[i].x, profile[i].y));
                }
            for (int i = 0; i + 1 < profile.Length; i++)
            {
                var axis = f.P(0, 0, (profile[i].y + profile[i + 1].y) * 0.5f);
                for (int j = 0; j < Round; j++)
                {
                    int n = (j + 1) % Round;
                    Face(k, rows[i, j], rows[i + 1, j], rows[i + 1, n], axis);
                    Face(k, rows[i, j], rows[i + 1, n], rows[i, n], axis);
                }
            }
            return k;
        }

        /// <summary>Flat elliptical disc at height z, facing up or down.</summary>
        static void Disc(MeshKit k, Frame f, float ra, float rb, float z, bool up)
        {
            int c = k.Vert(f.P(0, 0, z)); var ring = new int[Round];
            for (int j = 0; j < Round; j++) { float t = 2 * Mathf.PI * j / Round; ring[j] = k.Vert(f.P(Mathf.Cos(t) * ra, Mathf.Sin(t) * rb, z)); }
            var inside = f.P(0, 0, up ? z - 1 : z + 1);
            for (int j = 0; j < Round; j++) Face(k, c, ring[j], ring[(j + 1) % Round], inside);
        }

        /// <summary>Half-cylinder lying along yaw: length 2a, span 2b, rise h; flat ends.</summary>
        /// <summary>Arch profile point i of Arch: a circular segment through the springings (±b, 0) and the crown (0, h) when the rise is
        /// under the half-span (a segmental vault, e.g. the Parade's gallery canopies: span 5, rise 1.25), else the half-ellipse.</summary>
        static Vector2 ArchAt(int i, float b, float h)
        {
            if (h < b - 1e-4f && h > 1e-4f)
            {
                float R = (b * b + h * h) / (2f * h), half = Mathf.Asin(b / R), t = -half + 2f * half * (1f - i / (float)Arch);
                return new Vector2(Mathf.Sin(t) * R, Mathf.Cos(t) * R - (R - h));
            }
            float u = Mathf.PI * i / Arch; return new Vector2(Mathf.Cos(u) * b, Mathf.Sin(u) * h);
        }

        static MeshKit Vault(MeshKit k, Frame f, float a, float b, float h)
        {
            var e0 = new int[Arch + 1]; var e1 = new int[Arch + 1];
            for (int i = 0; i <= Arch; i++)
            {
                var q = ArchAt(i, b, h); float r = q.x, z = q.y;
                e0[i] = k.Vert(f.P(-a, r, z)); e1[i] = k.Vert(f.P(a, r, z));
            }
            for (int i = 0; i < Arch; i++)
            {
                var axis = f.P(0, 0, 0);
                Face(k, e0[i], e0[i + 1], e1[i + 1], axis);
                Face(k, e0[i], e1[i + 1], e1[i], axis);
            }
            foreach (var s in new[] { -a, a })
            {
                int c = k.Vert(f.P(s, 0, 0)); var ring = new int[Arch + 1];
                for (int i = 0; i <= Arch; i++) { var q = ArchAt(i, b, h); ring[i] = k.Vert(f.P(s, q.x, q.y)); }
                var inside = f.P(-s, 0, h * 0.5f);
                for (int i = 0; i < Arch; i++) Face(k, c, ring[i], ring[i + 1], inside);
            }
            return k;
        }

        /// <summary>Box whose top slopes from 0 at -b to h at +b: bottom, slope, back wall and two triangular ends.</summary>
        static MeshKit Wedge(MeshKit k, Frame f, float a, float b, float h)
        {
            var mid = f.P(0, b * 0.33f, h * 0.33f);
            Quad(k, mid, f.P(-a, -b, 0), f.P(a, -b, 0), f.P(a, b, 0), f.P(-a, b, 0));   // bottom
            Quad(k, mid, f.P(-a, -b, 0), f.P(a, -b, 0), f.P(a, b, h), f.P(-a, b, h));   // slope
            Quad(k, mid, f.P(-a, b, 0), f.P(a, b, 0), f.P(a, b, h), f.P(-a, b, h));     // back
            foreach (var s in new[] { -a, a })
                Face(k, k.Vert(f.P(s, -b, 0)), k.Vert(f.P(s, b, 0)), k.Vert(f.P(s, b, h)), mid);
            return k;
        }

        static void Quad(MeshKit k, Vector3 inside, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            int a = k.Vert(p0), b = k.Vert(p1), c = k.Vert(p2), d = k.Vert(p3);
            Face(k, a, b, c, inside); Face(k, a, c, d, inside);
        }

        /// <summary>Triangle a, b, c wound so its front faces away from `inside` (Unity: front = Cross(b - a, c - a)).</summary>
        static void Face(MeshKit k, int a, int b, int c, Vector3 inside)
        {
            Vector3 pa = k.At(a), pb = k.At(b), pc = k.At(c);
            var n = Vector3.Cross(pb - pa, pc - pa);
            if (n.sqrMagnitude < 1e-12f) return; // degenerate (a pole)
            if (Vector3.Dot(n, (pa + pb + pc) / 3f - inside) >= 0) k.Tri(a, b, c); else k.Tri(a, c, b);
        }
    }
}
