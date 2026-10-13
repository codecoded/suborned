using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Separates solids whose visible faces share a plane and face the same way (a vent flush with a plant room, a
    /// pavilion support flush with the pavilion's wall): the smaller solid's face moves 2 cm inward, so the two never
    /// z-fight. Inputs are left untouched; adjusted copies keep their ids. Bottoms are never seen and are left alone.
    /// Counts what it moved, per build (reported with the build log).</summary>
    public static class Coplanar
    {
        const float Inset = 0.02f, Flush = 0.002f;
        public static int Faces;

        public static void Reset() => Faces = 0;

        // ---------------------------------------------------------------- boxes (export prims of one group)

        struct Ext { public float U0, U1, V0, V1, Z0, Z1; }

        static Ext ExtOf(PrimDto p, Vector2 u, Vector2 v)
        {
            float cu = Vector2.Dot(p.Centre, u), cv = Vector2.Dot(p.Centre, v), a = p.Size.x * 0.5f, b = p.Size.y * 0.5f;
            return new Ext { U0 = cu - a, U1 = cu + a, V0 = cv - b, V1 = cv + b, Z0 = p.Base, Z1 = p.Base + p.Size.z };
        }

        /// <summary>Same-yaw box pairs: top and side faces in one plane over a non-zero area.</summary>
        public static List<PrimDto> Boxes(List<PrimDto> parts)
        {
            var res = new List<PrimDto>(parts);
            for (int i = 0; i < res.Count; i++)
                for (int j = i + 1; j < res.Count; j++)
                {
                    PrimDto A = res[i], B = res[j];
                    if (A.Shape != "box" || B.Shape != "box" || Mathf.Abs(Mathf.DeltaAngle(A.Yaw * Mathf.Rad2Deg, B.Yaw * Mathf.Rad2Deg)) > 0.05f) continue;
                    var u = new Vector2(Mathf.Cos(A.Yaw), Mathf.Sin(A.Yaw)); var v = new Vector2(-u.y, u.x);
                    Ext a = ExtOf(A, u, v), b = ExtOf(B, u, v);
                    bool small = Vol(B) <= Vol(A); int k = small ? j : i; var s = small ? b : a;
                    bool z = Over(a.Z0, a.Z1, b.Z0, b.Z1), ou = Over(a.U0, a.U1, b.U0, b.U1), ov = Over(a.V0, a.V1, b.V0, b.V1);
                    int n = Faces;
                    if (ou && ov && Near(a.Z1, b.Z1)) { s.Z1 -= Inset; Faces++; }
                    if (ov && z && Near(a.U0, b.U0)) { s.U0 += Inset; Faces++; }
                    if (ov && z && Near(a.U1, b.U1)) { s.U1 -= Inset; Faces++; }
                    if (ou && z && Near(a.V0, b.V0)) { s.V0 += Inset; Faces++; }
                    if (ou && z && Near(a.V1, b.V1)) { s.V1 -= Inset; Faces++; }
                    if (n == Faces) continue;
                    var p = res[k];
                    res[k] = new PrimDto
                    {
                        Id = p.Id, Shape = p.Shape, Surface = p.Surface, Yaw = p.Yaw, Base = s.Z0,
                        Centre = u * ((s.U0 + s.U1) * 0.5f) + v * ((s.V0 + s.V1) * 0.5f), Size = new Vector3(s.U1 - s.U0, s.V1 - s.V0, s.Z1 - s.Z0),
                    };
                }
            return res;
        }

        static float Vol(PrimDto p) => p.Size.x * p.Size.y * p.Size.z;

        // ---------------------------------------------------------------- masses (extruded rings)

        /// <summary>Mass pairs: equal tops over overlapping footprints, and collinear same-facing walls over overlapping
        /// spans and heights. The smaller mass (footprint x height) gives way: its top drops, its wall moves in.</summary>
        public static List<MassDto> Masses(List<MassDto> masses)
        {
            var res = new List<MassDto>(masses.Count);
            foreach (var m in masses) res.Add(new MassDto { Id = m.Id, Surface = m.Surface, Ring = Poly.Clean(m.Ring), Base = m.Base, Height = m.Height, Kit = m.Kit });
            for (int i = 0; i < res.Count; i++)
                for (int j = i + 1; j < res.Count; j++)
                {
                    MassDto A = res[i], B = res[j];
                    if (!Over(A.Base, A.Base + A.Height, B.Base, B.Base + B.Height) || !Over(Box(A.Ring), Box(B.Ring))) continue;
                    var (s, big) = Size(B) <= Size(A) ? (B, A) : (A, B);
                    if (Near(s.Base + s.Height, big.Base + big.Height)) { s.Height -= Inset; Faces++; }
                    for (int e = 0; e < s.Ring.Length; e++)
                    {
                        var w = new WallFrame(s.Ring[e], s.Ring[(e + 1) % s.Ring.Length]);
                        if (!SharesPlane(w, big.Ring)) continue;
                        var d = -w.N * Inset;
                        s.Ring[e] += d; s.Ring[(e + 1) % s.Ring.Length] += d; Faces++;
                    }
                }
            return res;
        }

        static bool SharesPlane(in WallFrame w, Vector2[] ring)
        {
            for (int i = 0; i < ring.Length; i++)
            {
                Vector2 q0 = ring[i], q1 = ring[(i + 1) % ring.Length];
                if (Mathf.Abs(w.D(q0)) > Flush || Mathf.Abs(w.D(q1)) > Flush || Vector2.Dot(q1 - q0, w.U) <= 0) continue;
                if (Over(0, w.Len, w.S(q0), w.S(q1))) return true;
            }
            return false;
        }

        static Rect Box(Vector2[] ring)
        {
            Vector2 lo = ring[0], hi = ring[0];
            foreach (var q in ring) { lo = Vector2.Min(lo, q); hi = Vector2.Max(hi, q); }
            return Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
        }

        static float Size(MassDto m) { var b = Box(m.Ring); return b.width * b.height * m.Height; }

        static bool Over(Rect a, Rect b) => Over(a.xMin, a.xMax, b.xMin, b.xMax) && Over(a.yMin, a.yMax, b.yMin, b.yMax);
        static bool Near(float x, float y) => Mathf.Abs(x - y) <= Flush;
        static bool Over(float a0, float a1, float b0, float b1) => Mathf.Min(a1, b1) - Mathf.Max(a0, b0) > Flush;
    }
}
