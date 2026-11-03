#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using G = Suborned.OldTown.Editor.DecoKit.DecoKitGroundMesh;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Orbit Square's floor (the terrazzo_brass and starlet_terrazzo studies): the ivory terrazzo field framed by a
    /// seafoam band edged in brass, three brass orbits round the pavilion (the atom, rotated 0 / 60 / 120 deg) and brass
    /// and coral star inlays over the field. Flush inlays (2 mm proud against z-fighting), so the square still reads as
    /// one open floor from the play camera, with structure at the metre scale the camera resolves.
    internal static class DecoKitSquareInlay
    {
        const float Lift = .002f, Line = .07f;  // inlay over the square's top; brass line half width (14 cm)
        const float Border = 2.5f, Band = .7f;   // band centre in from the edge; its half width
        const float OrbitA = 19f, OrbitB = 6.5f; // the orbits' semi-axes (clear of the pavilion, across the bench ring)

        internal static void Build(Rect square, float top, Transform root)
        {
            float y = top + Lift;
            var c = square.center;
            G brass = new(), band = new(), coral = new();
            var inner = Inset(square, Border);
            band.Strip(Corners(inner), Band, y, true);
            foreach (var d in new[] { -Band - .12f, Band + .12f })
                brass.Strip(Corners(Inset(square, Border + d)), .045f, y + .0005f, true);
            for (int k = 0; k < 3; k++) brass.Strip(Ellipse(c, OrbitA, OrbitB, k * 60f, 96), Line, y + .0005f, true);
            brass.Star(c, 2.2f, .7f, 8, 22.5f, y + .001f);  // the nucleus, under the pavilion's edge
            // stars on a jittered 9 m lattice, clear of the orbits' core, the arcade terraces and the border
            var rng = new System.Random(13);
            int stars = 0;
            for (float x = inner.xMin + 5f; x < inner.xMax - 3f; x += 9f)
                for (float z = inner.yMin + 4f; z < inner.yMax - 3f; z += 9f)
                {
                    var at = new Vector2(x + (float)rng.NextDouble() * 3f - 1.5f, z + (float)rng.NextDouble() * 3f - 1.5f);
                    var e = at - c;
                    if (e.x * e.x / (OrbitA * OrbitA) + e.y * e.y / (OrbitA * OrbitA * .3f) < 1.2f) continue;
                    if (Mathf.Abs(e.x) > 36f && Mathf.Abs(e.y) < 8f) continue;  // café terraces
                    (stars++ % 3 == 0 ? coral : brass).Star(at, .75f, .22f, 4, (float)rng.NextDouble() * 90f, y + .001f);
                }
            band.Emit("Orbit Square band", G.Mat("DK_Terrazzo_Seafoam"), false, root);
            brass.Emit("Orbit Square brass", G.Plain("DK_Brass_Inlay", "#B8914F", .45f), false, root);
            coral.Emit("Orbit Square coral", G.Plain("DK_Coral_Inlay", "#D07B64", .35f), false, root);
            Debug.Log($"[DecoKit] square inlay: 3 orbits, {stars} stars");
        }

        static Rect Inset(Rect r, float d) => Rect.MinMaxRect(r.xMin + d, r.yMin + d, r.xMax - d, r.yMax - d);

        static Vector2[] Corners(Rect r) =>
            new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };

        static List<Vector2> Ellipse(Vector2 c, float a, float b, float degrees, int n)
        {
            var pts = new List<Vector2>(n);
            float cs = Mathf.Cos(degrees * Mathf.Deg2Rad), sn = Mathf.Sin(degrees * Mathf.Deg2Rad);
            for (int i = 0; i < n; i++)
            {
                float t = i * Mathf.PI * 2f / n;
                float x = Mathf.Cos(t) * a, z = Mathf.Sin(t) * b;
                pts.Add(c + new Vector2(x * cs - z * sn, x * sn + z * cs));
            }
            return pts;
        }
    }
}
#endif
