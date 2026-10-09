using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>Land: the ground under everything, plots (ground slab + block by use), area outlines, water, quay and beyond.</summary>
    public static class LandLayers
    {
        const float GroundLevel = -0.05f, AreaLevel = 0.25f;

        /// <summary>A neutral sheet over the bounds of roads, plots, foot and beyond (land only, so water stays visible).</summary>
        public static void Ground(Transform root, object world)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue); var max = -min;
            void Grow(Vector2[] p) { foreach (var q in p) { min = Vector2.Min(min, q); max = Vector2.Max(max, q); } }
            foreach (var r in J.A(world, "roads")) Grow(J.Poly(r, "kerb"));
            foreach (var p in J.A(world, "plots")) Grow(J.Poly(p, "polygon"));
            foreach (var f in J.A(world, "foot")) Grow(J.Poly(f, "polygon"));
            foreach (var b in J.Polys(J.O(world)["water"], "beyond")) Grow(b);
            var sheet = new[] { min, new Vector2(max.x, min.y), max, new Vector2(min.x, max.y) };
            Nodes.Part(root, "sheet", new MeshKit().Flat(sheet, GroundLevel), "ground");
        }

        /// <summary>Every plot gets a pavement-height ground slab coloured by use. Built plots also get a block, inset by the
        /// pavement margin, to the plan's height above the pavement (export `height`; a built plot without one fails the build). Civic plots (the Handover square and office) stay empty: the
        /// Handover layer supplies their ground (blocks, with the lay-bys cut out) and buildings at exact footprint.</summary>
        public static void Plots(Transform root, object world, float pavement, CutAway cut)
        {
            foreach (var p in J.A(world, "plots"))
            {
                string use = J.S(p, "use");
                bool built = J.B(p, "built"), civic = J.Get(p, "civic") != null;
                var g = Nodes.Group(root, J.S(p, "id"));
                if (civic) continue;
                var poly = J.Poly(p, "polygon");
                Nodes.Part(g, "ground", new MeshKit().Prism(poly, 0, pavement), built ? "pavement" : "use/" + use);
                if (!built) continue;
                var inset = Poly.Inset(Poly.Clean(poly), Style.BlockInset);
                if (inset == null) { Debug.LogWarning($"Graybox: plot {J.S(p, "id")} too small for its block inset"); continue; }
                if (J.Get(p, "height") == null) throw new System.IO.InvalidDataException($"Graybox: built plot {J.S(p, "id")} has no height in the export");
                float h = J.F(p, "height");
                var block = Nodes.Part(g, "block", new MeshKit().Prism(inset, pavement, pavement + h), "use/" + use);
                cut.Add(J.S(p, "id"), inset, pavement, pavement + h, "use/" + use, block);
            }
        }

        public static void Areas(Transform root, object world)
        {
            foreach (var a in J.A(world, "areas"))
                Nodes.Part(root, J.S(a, "id"), new MeshKit().Ribbon(J.Poly(a, "polygon"), Style.AreaOutline, AreaLevel, AreaLevel, true), "area");
        }

        /// <summary>Water surface below the carriageway, quay walls from the water up to the pavement, beyond as raised land.</summary>
        public static void Water(Transform root, object world, float pavement)
        {
            var w = J.O(world)["water"];
            int i = 0;
            foreach (var p in J.Polys(w, "water")) Nodes.Part(root, "water-" + i++, new MeshKit().Flat(p, Style.WaterLevel), "water");
            i = 0;
            foreach (var p in J.Polys(w, "quay")) Nodes.Part(root, "quay-" + i++, new MeshKit().Prism(p, Style.WaterLevel, pavement), "quay");
            i = 0;
            foreach (var p in J.Polys(w, "beyond")) Nodes.Part(root, "beyond-" + i++, new MeshKit().Prism(p, 0, pavement), "beyond");
        }
    }
}
