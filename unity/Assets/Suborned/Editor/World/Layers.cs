using UnityEngine;

namespace Suborned.World
{
    /// <summary>One method per scene layer. Every export element becomes exactly one child of its layer root (named by id),
    /// so object counts can be checked against the export. Geometry sizes come only from the DTOs.</summary>
    public sealed class Layers
    {
        readonly Nodes nodes;
        public Layers(Nodes nodes) => this.nodes = nodes;

        /// <summary>The look surface the kerbs' gutter strip draws in (look.json; absent = no gutter).</summary>
        const string GutterSurface = "ground/gutter";
        /// <summary>Brief 028 road finish (Vela): the enamel carriageway runs to the pearl kerb with no gutter strip.</summary>
        static bool NoGutter(string roadSurface) => roadSurface == "road-enamel";

        public void Ground(Transform layer, GroundDto g)
        {
            KerbKit.PaveTop = 0f; foreach (var p in g.Plots) if (p.Height < 0.5f) KerbKit.PaveTop = Mathf.Max(KerbKit.PaveTop, p.Base + p.Height);
            foreach (var r in g.Roads)
            {
                var el = Nodes.Element(layer, r.Id);
                nodes.Part(el, "carriageway", new MeshKit().Ribbon(r.Line, r.Width, r.Level, r.Level), nodes.Ground(r.Surface));
                // Kerb strips centred on the carriageway edges' outer side: offset lines at (width + kerb) / 2.
                var (l, rr) = Poly.Ribbon(Poly.CleanLine(r.Line), r.Width + r.KerbWidth, false);
                var drives = g.Drives.FindAll(d => d.Road == r.Id && d.Kerb is { Length: 2 });
                var kerbs = new MeshKit(); var dropped = new MeshKit(); var gutters = new MeshKit(); bool anyDrop = false;
                var roadLine = Poly.CleanLine(r.Line); bool gutter = nodes.Has(GutterSurface) && !NoGutter(r.Surface); // brief 028 road (Vela): no gutter on an enamel road
                foreach (var side in new[] { l, rr }) // brief 023: the kerb is dropped where a drive crosses it (the drive's kerb segment)
                    foreach (var (piece, drop, h) in KerbPieces(side, drives, r.KerbWidth))
                    {   // brief 026 round 2: a stone (top, face, end caps) with UVs in metres, and the gutter along its foot
                        if (!drop) KerbKit.Build(kerbs, gutter ? gutters : null, piece, roadLine, r.KerbWidth, r.Level, Mathf.Max(r.Level + r.KerbHeight, KerbKit.PaveTop + KerbKit.TopProud), r.Level);
                        else { KerbKit.Build(dropped, gutter ? gutters : null, piece, roadLine, r.KerbWidth, r.Level, r.Level + h, r.Level); anyDrop = true; }
                    }
                var kerbMat = nodes.Mat(nodes.Ground(r.KerbSurface)); // its own UVs: no planar UV pass
                nodes.Part(el, "kerbs", kerbs, kerbMat);
                if (anyDrop) nodes.Part(el, "kerbs-dropped", dropped, kerbMat);
                if (gutter) nodes.Part(el, "gutters", gutters, nodes.Mat(GutterSurface));
                // the drive itself: a flat at its level from the carriageway's edge to its bay, drawn with the road it leaves (one element per
                // export id, and counts.ground has no drives: world.schema.ts)
                foreach (var d in drives) nodes.Part(el, "drive/" + d.Id, new MeshKit().Flat(d.Ring, d.Level), nodes.Ground(d.Surface));
            }
            foreach (var c in g.Circuses)
            {
                var el = Nodes.Element(layer, c.Id);
                var outer = Poly.Circle(c.Centre, c.Radius, 64); var island = Poly.Circle(c.Centre, c.IslandRadius, 64);
                nodes.Part(el, "carriageway", new MeshKit().Flat(outer, island, c.Level), nodes.Ground(c.Surface));
                nodes.Part(el, "island", new MeshKit().Prism(island, c.Level, c.Level + c.IslandHeight), nodes.Ground(c.IslandSurface));
            }
            foreach (var f in g.Foot) nodes.Solid(layer, f.Id, new MeshKit().Ribbon(f.Line, f.Width, f.Level, f.Level), nodes.Ground(f.Surface));
            foreach (var m in g.Medians) Mass(layer, m, true);
            foreach (var f in g.Flats) nodes.Solid(layer, f.Id, new MeshKit().Flat(f.Ring, f.Level), nodes.Ground(f.Surface));
            foreach (var w in g.Water) nodes.Solid(layer, w.Id, new MeshKit().Flat(w.Ring, w.Level), w.Surface);
            foreach (var q in g.Quays) Mass(layer, q, true);
            // brief 023: a drive and the bay it reaches lie below the plot slab's top: the slab is cut round them (each a convex ring)
            var cuts = new System.Collections.Generic.List<Vector2[]>();
            foreach (var d in g.Drives) { cuts.Add(d.Ring); var bay = g.Flats.Find(f => f.Id == d.To); if (bay != null) cuts.Add(bay.Ring); }
            foreach (var p in g.Plots)
            {   // a plan signature's ground (SpaceHQ's drum) is the piece's own floor: the pavement slab stops at its edge
                var parts = KitCover.OutsideGrounds(Poly.Clean(p.Ring));
                foreach (var c in cuts) parts = CutConvex(parts, Poly.Clean(c));
                var kit = new MeshKit();
                foreach (var q in parts) kit.Prism(q, p.Base, p.Base + p.Height);
                nodes.Solid(layer, p.Id, kit, nodes.Ground(p.Surface));
            }
        }

        /// <summary>A kerb line in pieces: kept, and dropped (to the drive's kerb height) where a drive's kerb segment runs along it within the
        /// kerb's width; in order along the line.</summary>
        static System.Collections.Generic.List<(Vector2[] piece, bool drop, float h)> KerbPieces(Vector2[] line, System.Collections.Generic.List<DriveDto> drives, float kerbWidth)
        {
            var cuts = new System.Collections.Generic.List<(float a, float b, float h)>();
            foreach (var d in drives)
            {
                var (sa, da) = Project(line, d.Kerb[0]); var (sb, db) = Project(line, d.Kerb[1]);
                if (da > kerbWidth + 0.05f || db > kerbWidth + 0.05f || Mathf.Abs(sb - sa) < 0.01f) continue; // the other side of the road
                cuts.Add((Mathf.Min(sa, sb), Mathf.Max(sa, sb), Mathf.Max(0f, d.KerbHeight)));
            }
            var r = new System.Collections.Generic.List<(Vector2[], bool, float)>();
            float total = Length(line), at = 0f;
            cuts.Sort((x, y) => x.a.CompareTo(y.a));
            foreach (var (a, b, h) in cuts)
            {
                if (a > at + 0.01f) r.Add((Sub(line, at, a), false, 0f));
                if (b > Mathf.Max(a, at) + 0.01f) r.Add((Sub(line, Mathf.Max(a, at), b), true, h));
                at = Mathf.Max(at, b);
            }
            if (total > at + 0.01f) r.Add((Sub(line, at, total), false, 0f));
            return r;
        }

        static float Length(Vector2[] line) { float s = 0f; for (int i = 1; i < line.Length; i++) s += Vector2.Distance(line[i - 1], line[i]); return s; }

        /// <summary>The arc length along the line of its nearest point to p, and the distance to it.</summary>
        static (float s, float d) Project(Vector2[] line, Vector2 p)
        {
            float best = float.MaxValue, bestS = 0f, s = 0f;
            for (int i = 1; i < line.Length; i++)
            {
                Vector2 a = line[i - 1], b = line[i], ab = b - a; float len = ab.magnitude; if (len < 1e-6f) continue;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / (len * len)); float d = Vector2.Distance(p, a + ab * t);
                if (d < best) { best = d; bestS = s + t * len; }
                s += len;
            }
            return (bestS, best);
        }

        /// <summary>The line between arc lengths s0 and s1.</summary>
        static Vector2[] Sub(Vector2[] line, float s0, float s1)
        {
            var r = new System.Collections.Generic.List<Vector2>(); float s = 0f;
            for (int i = 1; i < line.Length; i++)
            {
                Vector2 a = line[i - 1], b = line[i]; float len = Vector2.Distance(a, b); if (len < 1e-6f) continue;
                float e = s + len;
                if (e >= s0 && s <= s1)
                {
                    if (r.Count == 0) r.Add(Vector2.Lerp(a, b, Mathf.Clamp01((s0 - s) / len)));
                    if (e <= s1) r.Add(b); else { r.Add(Vector2.Lerp(a, b, Mathf.Clamp01((s1 - s) / len))); break; }
                }
                s = e;
            }
            return r.ToArray();
        }

        /// <summary>Each part less a convex ring: split along the ring's edges (Sutherland–Hodgman), keeping what lies outside.</summary>
        static System.Collections.Generic.List<Vector2[]> CutConvex(System.Collections.Generic.List<Vector2[]> parts, Vector2[] convex)
        {
            if (convex.Length < 3) return parts;
            if (Poly.SignedArea(convex) < 0) System.Array.Reverse(convex = (Vector2[])convex.Clone()); // counter-clockwise: outside is right of each edge
            var outParts = new System.Collections.Generic.List<Vector2[]>();
            foreach (var p in parts)
            {
                var rest = p;
                for (int i = 0; i < convex.Length && rest.Length >= 3; i++)
                {
                    Vector2 a = convex[i], b = convex[(i + 1) % convex.Length], e = b - a; var n = new Vector2(e.y, -e.x).normalized; float k = Vector2.Dot(n, a);
                    var outside = Half(rest, n, k + 1e-4f); if (outside.Length >= 3 && Mathf.Abs(Poly.SignedArea(outside)) > 0.01f) outParts.Add(outside);
                    rest = Half(rest, -n, -k - 1e-4f);
                }
            }
            return outParts;
        }

        static Vector2[] Half(Vector2[] p, Vector2 n, float k)
        {
            var r = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < p.Length; i++)
            {
                var a = p[i]; var b = p[(i + 1) % p.Length];
                float da = Vector2.Dot(n, a) - k, db = Vector2.Dot(n, b) - k;
                if (da >= 0) r.Add(a);
                if ((da >= 0) != (db >= 0)) r.Add(a + (b - a) * (da / (da - db)));
            }
            return r.ToArray();
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

        void Mass(Transform parent, MassDto m, bool ground = false) => nodes.Solid(parent, m.Id, new MeshKit().Prism(m.Ring, m.Base, m.Base + m.Height), ground ? nodes.Ground(m.Surface) : m.Surface);

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
