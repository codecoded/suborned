using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Graybox
{
    /// <summary>Builds flat-shaded meshes from plan geometry. Plan (x, y) at height h → Unity (x, h, y); vertices are in
    /// world space (objects stay at the origin). Plan counter-clockwise also reads counter-clockwise from above,
    /// which is a back face under Unity's clockwise front-face rule, so caps are emitted reversed to face up; walls face
    /// outward. No bottoms (never seen).</summary>
    public sealed class MeshKit
    {
        readonly List<Vector3> v = new();
        readonly List<int> t = new();

        static Vector3 W(Vector2 p, float h) => new(p.x, h, p.y);

        public bool Empty => t.Count == 0;

        /// <summary>Ear-clipping fallbacks taken while building this mesh (0 for clean input).</summary>
        public int Fallbacks { get; private set; }

        /// <summary>Repairs Poly.Clean made to this mesh's polygons (0 for the simple polygons the export promises).</summary>
        public int Repairs { get; private set; }

        Vector2[] Clean(Vector2[] raw)
        {
            int before = Poly.Repairs;
            var p = Poly.Clean(raw);
            Repairs += Poly.Repairs - before;
            return p;
        }

        /// <summary>Top face of a counter-clockwise ring at height h.</summary>
        public MeshKit Cap(Vector2[] ccw, float h)
        {
            int b = v.Count;
            foreach (var p in ccw) v.Add(W(p, h));
            int before = Poly.Fallbacks;
            var tri = Poly.Triangulate(ccw);
            Fallbacks += Poly.Fallbacks - before;
            for (int i = 0; i < tri.Length; i += 3) { t.Add(b + tri[i]); t.Add(b + tri[i + 2]); t.Add(b + tri[i + 1]); }
            return this;
        }

        /// <summary>Vertical quad on plan edge a→b, facing right of the direction (outward for a counter-clockwise ring).</summary>
        public MeshKit Wall(Vector2 a, Vector2 b, float lo, float hi)
        {
            if (hi - lo < 1e-4f || (b - a).sqrMagnitude < 1e-8f) return this;
            int i = v.Count;
            v.Add(W(a, lo)); v.Add(W(a, hi)); v.Add(W(b, hi)); v.Add(W(b, lo));
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
            return this;
        }

        public MeshKit Flat(Vector2[] raw, float h) => Cap(Clean(raw), h);

        /// <summary>Flat annulus: outer ring minus a hole inside it (any winding).</summary>
        public MeshKit Flat(Vector2[] raw, Vector2[] rawHole, float h) => Cap(Poly.Bridge(Clean(raw), Clean(rawHole)), h);

        /// <summary>Solid extrusion of a polygon (any winding, may be concave) from lo to hi.</summary>
        public MeshKit Prism(Vector2[] raw, float lo, float hi)
        {
            var p = Clean(raw);
            Cap(p, hi);
            for (int i = 0; i < p.Length; i++) Wall(p[i], p[(i + 1) % p.Length], lo, hi);
            return this;
        }

        /// <summary>Solid extrusion of an outer ring minus a hole, from lo to hi; the hole's walls face into the hole.</summary>
        public MeshKit Prism(Vector2[] raw, Vector2[] rawHole, float lo, float hi)
        {
            var p = Clean(raw); var q = Clean(rawHole);
            Cap(Poly.Bridge(p, q), hi);
            for (int i = 0; i < p.Length; i++) Wall(p[i], p[(i + 1) % p.Length], lo, hi);
            for (int i = 0; i < q.Length; i++) Wall(q[(i + 1) % q.Length], q[i], lo, hi);
            return this;
        }

        /// <summary>Annulus between two counter-clockwise loops of equal count whose starts are aligned.</summary>
        public MeshKit Ring(Vector2[] outer, Vector2[] inner, float lo, float hi)
        {
            int n = outer.Length;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n, b = v.Count;
                v.Add(W(outer[i], hi)); v.Add(W(outer[j], hi)); v.Add(W(inner[j], hi)); v.Add(W(inner[i], hi));
                t.Add(b); t.Add(b + 2); t.Add(b + 1);
                t.Add(b); t.Add(b + 3); t.Add(b + 2);
                Wall(outer[i], outer[j], lo, hi);
                Wall(inner[j], inner[i], lo, hi);
            }
            return this;
        }

        /// <summary>Ribbon of the given width along a polyline: a flat strip (lo == hi) or a beam with sides and ends.</summary>
        public MeshKit Ribbon(Vector2[] rawLine, float width, float lo, float hi, bool closed = false)
        {
            var line = Poly.CleanLine(rawLine);
            if (closed && line.Length > 1 && (line[0] - line[^1]).sqrMagnitude < 1e-8f) line = line[..^1];
            if (line.Length < 2) return this;
            var (l, r) = Poly.Ribbon(line, width, closed);
            int segs = closed ? line.Length : line.Length - 1;
            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % line.Length, b = v.Count;
                v.Add(W(r[i], hi)); v.Add(W(r[j], hi)); v.Add(W(l[j], hi)); v.Add(W(l[i], hi));
                t.Add(b); t.Add(b + 2); t.Add(b + 1);
                t.Add(b); t.Add(b + 3); t.Add(b + 2);
                Wall(r[i], r[j], lo, hi);
                Wall(l[j], l[i], lo, hi);
            }
            if (!closed)
            {
                Wall(r[^1], l[^1], lo, hi);
                Wall(l[0], r[0], lo, hi);
            }
            return this;
        }

        /// <summary>Flat strip of the given width along an open polyline, segment i at height level[i] (steps where levels change).</summary>
        public MeshKit Strip(Vector2[] line, float width, float[] level)
        {
            if (line.Length < 2) return this;
            var (l, r) = Poly.Ribbon(line, width, false);
            for (int i = 0; i + 1 < line.Length; i++)
            {
                int b = v.Count; float h = level[i];
                v.Add(W(r[i], h)); v.Add(W(r[i + 1], h)); v.Add(W(l[i + 1], h)); v.Add(W(l[i], h));
                t.Add(b); t.Add(b + 2); t.Add(b + 1);
                t.Add(b); t.Add(b + 3); t.Add(b + 2);
            }
            return this;
        }

        public Mesh Build(string name)
        {
            var m = new Mesh { name = name };
            if (v.Count > 65535) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
