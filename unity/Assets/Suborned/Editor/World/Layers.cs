using UnityEngine;

namespace Suborned.World
{
    /// <summary>One method per scene layer. Every export element becomes exactly one child of its layer root (named by id),
    /// so object counts can be checked against the export. Geometry sizes come only from the DTOs.</summary>
    public sealed class Layers
    {
        readonly Nodes nodes;
        public Layers(Nodes nodes) => this.nodes = nodes;

        public void Ground(Transform layer, GroundDto g)
        {
            foreach (var r in g.Roads)
            {
                var el = Nodes.Element(layer, r.Id);
                nodes.Part(el, "carriageway", new MeshKit().Ribbon(r.Line, r.Width, r.Level, r.Level), r.Surface);
                // Kerb strips centred on the carriageway edges' outer side: offset lines at (width + kerb) / 2.
                var (l, rr) = Poly.Ribbon(Poly.CleanLine(r.Line), r.Width + r.KerbWidth, false);
                var kerbs = new MeshKit()
                    .Ribbon(l, r.KerbWidth, r.Level, r.Level + r.KerbHeight)
                    .Ribbon(rr, r.KerbWidth, r.Level, r.Level + r.KerbHeight);
                nodes.Part(el, "kerbs", kerbs, r.KerbSurface);
            }
            foreach (var c in g.Circuses)
            {
                var el = Nodes.Element(layer, c.Id);
                var outer = Poly.Circle(c.Centre, c.Radius, 64); var island = Poly.Circle(c.Centre, c.IslandRadius, 64);
                nodes.Part(el, "carriageway", new MeshKit().Flat(outer, island, c.Level), c.Surface);
                nodes.Part(el, "island", new MeshKit().Prism(island, c.Level, c.Level + c.IslandHeight), c.IslandSurface);
            }
            foreach (var f in g.Foot) nodes.Solid(layer, f.Id, new MeshKit().Ribbon(f.Line, f.Width, f.Level, f.Level), f.Surface);
            foreach (var m in g.Medians) Mass(layer, m);
            foreach (var f in g.Flats) nodes.Solid(layer, f.Id, new MeshKit().Flat(f.Ring, f.Level), f.Surface);
            foreach (var w in g.Water) nodes.Solid(layer, w.Id, new MeshKit().Flat(w.Ring, w.Level), w.Surface);
            foreach (var q in g.Quays) Mass(layer, q);
            foreach (var p in g.Plots) Mass(layer, p);
        }

        public void Transit(Transform layer, TransitDto t)
        {
            foreach (var x in t.Tram) nodes.Solid(layer, x.Id, Beams(x.Line, x.Spacing, x.Width, x.Level - x.Depth, x.Level), x.Surface);
            foreach (var m in t.Stops) Mass(layer, m);
            foreach (var m in t.Stations) Mass(layer, m);
            foreach (var s in t.SkyRail)
            {
                var el = Nodes.Element(layer, s.Id);
                nodes.Part(el, "beam", Beams(s.Line, s.Spacing, s.Width, s.Height - s.Depth, s.Height), s.Surface);
                var pylons = new MeshKit();
                foreach (var p in s.Pylons)
                {
                    var tangent = Poly.NearestTangent(s.Line, p, out _);
                    float soffit = s.Height - s.Depth;
                    if (s.Spacing > 0) // double line: a cross-head carries both beams
                    {
                        pylons.Prism(Poly.Rect(p, tangent, s.PylonSize, s.Spacing + s.Width), soffit - s.CapDepth, soffit);
                        soffit -= s.CapDepth;
                    }
                    pylons.Prism(Poly.Rect(p, tangent, s.PylonSize, s.PylonSize), s.Ground, soffit);
                }
                nodes.Part(el, "pylons", pylons, s.PylonSurface);
            }
        }

        /// <summary>One ribbon, or (spacing > 0) two ribbons spacing apart either side of the line.</summary>
        static MeshKit Beams(Vector2[] line, float spacing, float width, float lo, float hi)
        {
            if (spacing <= 0) return new MeshKit().Ribbon(line, width, lo, hi);
            var (l, r) = Poly.Ribbon(Poly.CleanLine(line), spacing, false);
            return new MeshKit().Ribbon(l, width, lo, hi).Ribbon(r, width, lo, hi);
        }

        /// <summary>A landmark with a DecoKit ref is that piece on its centre and base; otherwise spheres are stacked discs (24 slices)
        /// and cylinders prisms, with any `kits` standing on top at their own place and yaw.</summary>
        public void Landmarks(Transform layer, System.Collections.Generic.List<LandmarkDto> landmarks, KitPlacer placer)
        {
            foreach (var l in landmarks)
            {
                if (l.Kit != null) { placer.Place(Nodes.Element(layer, l.Id), layer.name, l.Kit, l.Centre, l.Base, 0f); continue; }
                var kit = new MeshKit();
                if (l.Shape == "sphere")
                {
                    const int slices = 24;
                    for (int i = 0; i < slices; i++)
                    {
                        float z0 = -l.Radius + 2 * l.Radius * i / slices, z1 = z0 + 2 * l.Radius / slices, zm = (z0 + z1) * 0.5f;
                        kit.Prism(Poly.Circle(l.Centre, Mathf.Sqrt(l.Radius * l.Radius - zm * zm), 48), l.Base + l.Radius + z0, l.Base + l.Radius + z1);
                    }
                }
                else kit.Prism(Poly.Circle(l.Centre, l.Radius, 48), l.Base, l.Base + l.Height);
                var el = nodes.Solid(layer, l.Id, kit, l.Surface);
                foreach (var k in l.Kits) placer.Place(el, layer.name, k, k.At, l.Base + l.Height, 0f);
            }
        }

        /// <summary>Levels are named heights (no geometry of their own) holding their stairs; their decks go to `decks`.</summary>
        public void Levels(Transform layer, Transform decks, System.Collections.Generic.List<LevelDto> levels)
        {
            foreach (var lv in levels)
            {
                var el = Nodes.Group(layer, lv.Id);
                foreach (var d in lv.Decks)
                {
                    var deck = Nodes.Element(decks, d.Id);
                    nodes.Part(deck, "slab", new MeshKit().Block(d.Ring, lv.Height - d.Thickness, lv.Height), d.Surface);
                    var cols = new MeshKit();
                    foreach (var c in d.Columns) cols.Prism(Poly.Rect(c, Vector2.right, d.ColumnSize, d.ColumnSize), d.Ground, lv.Height - d.Thickness);
                    nodes.Part(deck, "columns", cols, d.ColumnSurface);
                }
                foreach (var s in lv.Stairs) nodes.Solid(el, s.Id, Stair(s), s.Surface);
            }
        }

        void Mass(Transform parent, MassDto m) => nodes.Solid(parent, m.Id, new MeshKit().Prism(m.Ring, m.Base, m.Base + m.Height), m.Surface);

        /// <summary>Solid straight flight: n equal steps (n = rise / riser, rounded up), each a block from the foot level.</summary>
        static MeshKit Stair(StairDto s)
        {
            var run = s.Head - s.Foot; float rise = s.Top - s.Bottom;
            int n = Mathf.Max(1, Mathf.CeilToInt(rise / s.Riser - 1e-4f));
            var kit = new MeshKit(); float tread = run.magnitude / n;
            for (int i = 0; i < n; i++)
            {
                var c = s.Foot + run * ((i + 0.5f) / n);
                kit.Prism(Poly.Rect(c, run, tread, s.Width), s.Bottom, s.Bottom + rise * (i + 1) / n);
            }
            return kit;
        }
    }
}
