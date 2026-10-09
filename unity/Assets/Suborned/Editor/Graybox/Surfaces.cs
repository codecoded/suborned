using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>Top of the ground surface at a plan point, from the same exported polygons the layers extrude: raised
    /// surfaces (medians, foot, plot ground, circus pavement and island, garden, quay, beyond, Handover blocks) stand at
    /// their top; anywhere else is the carriageway (0). Lets line features (tram track) sit on whatever they cross.</summary>
    public sealed class Surfaces
    {
        readonly List<(Vector2[] poly, float top)> raised = new();

        public Surfaces(object world, float pavement)
        {
            var w = J.O(world);
            void Add(Vector2[] p, float top) => raised.Add((p, top));
            foreach (var m in J.A(world, "medians")) Add(J.Poly(m, "polygon"), pavement);
            foreach (var f in J.A(world, "foot")) Add(J.Poly(f, "polygon"), pavement);
            foreach (var p in J.A(world, "plots")) if (J.Get(p, "civic") == null) Add(J.Poly(p, "polygon"), pavement);
            var c = w["circus"];
            foreach (var p in J.Polys(c, "pavement")) Add(p, pavement);
            Add(J.Poly(c, "island"), pavement);
            Add(J.Poly(c, "garden"), pavement + StreetLayers.GardenRise);
            foreach (var p in J.Polys(w["water"], "quay")) Add(p, pavement);
            foreach (var p in J.Polys(w["water"], "beyond")) Add(p, pavement);
            foreach (var b in J.A(w["handover"], "blocks")) Add(J.Poly(b, "outer"), pavement);
        }

        /// <summary>Highest raised surface containing q, else 0.</summary>
        public float Top(Vector2 q)
        {
            float top = 0;
            foreach (var (poly, h) in raised) if (h > top && Poly.Contains(poly, q)) top = h;
            return top;
        }

        /// <summary>Surface under a segment: the highest of its two ends and its midpoint (a strip on a kerb step rides the upper side).</summary>
        public float Top(Vector2 a, Vector2 b) => Mathf.Max(Top(a), Mathf.Max(Top(b), Top((a + b) * 0.5f)));
    }
}
