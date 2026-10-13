using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Combines element geometry into one mesh per (layer, material, square tile), so draw calls scale with what a
    /// camera sees rather than with element count. Element objects stay in the hierarchy as empty markers (named by id,
    /// placed at the centre of their geometry); their bounds live in a per-build registry (Rig frames views with it).
    /// Static state, reset per build, like Poly's counters.</summary>
    public static class Combine
    {
        sealed class Acc { public Material Mat; public MeshKit Kit = new(); }

        static readonly Dictionary<string, Acc> accs = new(System.StringComparer.Ordinal);
        static readonly Dictionary<Transform, Bounds> bounds = new();
        static readonly Dictionary<Transform, int> triangles = new(), kitTris = new();
        static readonly List<Transform> elements = new();
        static readonly HashSet<Transform> covered = new();
        static float tile = 64f;

        public static void Reset(float tileSize)
        {
            accs.Clear(); bounds.Clear(); triangles.Clear(); kitTris.Clear(); elements.Clear(); covered.Clear(); tile = tileSize;
        }

        /// <summary>Registers an element marker (it must end up with geometry; Verify reports any that do not).</summary>
        public static void Element(Transform el) { elements.Add(el); triangles[el] = 0; }

        /// <summary>Adds an element's part to its layer's tile batch for this material.</summary>
        public static void Add(Transform el, string layer, MeshKit kit, Material mat)
        {
            var b = kit.Bounds;
            bounds[el] = bounds.TryGetValue(el, out var have) ? Union(have, b) : b;
            triangles[el] = (triangles.TryGetValue(el, out var n) ? n : 0) + kit.TriangleCount;
            int tx = Mathf.FloorToInt(b.center.x / tile), tz = Mathf.FloorToInt(b.center.z / tile);
            var key = $"{layer}|{mat.name}|{tx}|{tz}";
            if (!accs.TryGetValue(key, out var acc)) accs[key] = acc = new Acc { Mat = mat };
            acc.Kit.Append(kit);
        }

        /// <summary>Registers a placed kit piece's world bounds and triangles on its element (the piece renders itself, instanced).</summary>
        public static void Kit(Transform el, Bounds b, int tris)
        {
            bounds[el] = bounds.TryGetValue(el, out var have) ? Union(have, b) : b;
            triangles[el] = (triangles.TryGetValue(el, out var n) ? n : 0) + tris;
            kitTris[el] = (kitTris.TryGetValue(el, out var k) ? k : 0) + tris;
        }

        /// <summary>An element a kit piece placed on another element stands in for (a tower's upper mass, its roof): no geometry of
        /// its own, by design; its marker sits at its own volume.</summary>
        public static void Cover(Transform el, Bounds b) { bounds[el] = b; covered.Add(el); }

        /// <summary>Elements under `layer` with procedural (batched) geometry.</summary>
        public static int Procedural(Transform layer) =>
            elements.Count(e => e.IsChildOf(layer) && triangles[e] > (kitTris.TryGetValue(e, out var k) ? k : 0));

        static Bounds Union(Bounds a, Bounds b) { a.Encapsulate(b); return a; }

        /// <summary>Union of the registered bounds of t and its descendants; null when none.</summary>
        public static Bounds? Of(Transform t)
        {
            Bounds? r = null;
            foreach (var c in t.GetComponentsInChildren<Transform>(true))
                if (bounds.TryGetValue(c, out var b)) r = r is { } have ? Union(have, b) : b;
            return r;
        }

        /// <summary>The element has batched or kit geometry registered.</summary>
        public static bool HasGeometry(Transform el) => triangles.TryGetValue(el, out var n) && n > 0;

        /// <summary>Elements under `layer` registered without geometry.</summary>

        public static int Empty(Transform layer) =>
            elements.Count(e => e.IsChildOf(layer) && triangles[e] == 0 && !covered.Contains(e));

        /// <summary>Elements under `layer` a kit piece elsewhere stands in for.</summary>
        public static int Covered(Transform layer) => covered.Count(e => e.IsChildOf(layer));

        /// <summary>Places markers at their geometry centre (parents first) and writes the batch renderers under
        /// batches/&lt;layer&gt;/&lt;material&gt;@&lt;tx&gt;,&lt;tz&gt;, in key order. Returns the total vertex count.</summary>
        public static long Flush(Transform root)
        {
            // Markers move to their geometry centre; their children (kit pieces, nested markers) keep their world place.
            foreach (var e in elements.OrderBy(Depth))
                if (bounds.TryGetValue(e, out var b))
                {
                    var keep = new Vector3[e.childCount];
                    for (int i = 0; i < keep.Length; i++) keep[i] = e.GetChild(i).position;
                    e.position = b.center;
                    for (int i = 0; i < keep.Length; i++) e.GetChild(i).position = keep[i];
                }
            long verts = 0;
            var layers = new Dictionary<string, Transform>();
            foreach (var key in accs.Keys.OrderBy(k => k, System.StringComparer.Ordinal))
            {
                var acc = accs[key]; var p = key.Split('|');
                if (!layers.TryGetValue(p[0], out var layer)) layers[p[0]] = layer = Nodes.Group(root, p[0]);
                var go = new GameObject($"{p[1]}@{p[2]},{p[3]}") { isStatic = true };
                go.transform.SetParent(layer, false);
                go.AddComponent<MeshFilter>().sharedMesh = acc.Kit.Build($"{p[0]}/{go.name}");
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = acc.Mat;
                // Flat ground (roads, kerbs 0.18 m, paving, water) shadows nothing readable: ~40% of the shadow-caster vertices at play zoom.
                if (p[0] == "ground") mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                verts += acc.Kit.VertexCount;
            }
            return verts;
        }

        static int Depth(Transform t) { int d = 0; while (t.parent) { d++; t = t.parent; } return d; }
    }
}
