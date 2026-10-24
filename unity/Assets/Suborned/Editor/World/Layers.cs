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
        static bool NoGutter(string roadSurface) => roadSurface is "road-enamel" or "road-asphalt"; // brief 055: the asphalt option likewise (Vela)

        public void Ground(Transform layer, GroundDto g)
        {
            KerbKit.PaveTop = 0f; foreach (var p in g.Plots) if (p.Height < 0.5f) KerbKit.PaveTop = Mathf.Max(KerbKit.PaveTop, p.Base + p.Height);
            // brief 028 (Sky 2026-10-24, rounded pavement corners; Splice's kerbreturn.ts): a convex kerb return's corner is carriageway now (the
            // export's flats ground/return/<id>), and its arc of kerb is the kit's kerb-return pieces (street/kerbreturn/*): the straight kerb
            // stones stop where they enter a return's ring, so no square stone crosses the rounded corner
            // tools/geometry/returns.ts mirrors this (one definition): return fills and bay flats (a lay-by's mouth is open carriageway, no kerb
            // across it) and the concave bends' slabs (plot/bend/*), each grown by StripGrow, take the straight strip with them
            var returns = new System.Collections.Generic.List<Vector2[]>(); foreach (var f in g.Flats) if (IsReturn(f.Id) || IsBay(f.Id)) returns.Add(f.Ring); foreach (var p in g.Plots) if (p.Id.StartsWith("plot/bend/", System.StringComparison.Ordinal)) returns.Add(p.Ring);
            // Splice's diagnosis of Glyph's 0.2.104 review #1 (the stray kerb bars at the civic junction and junction NE): a road's edge strip ran
            // on to its node, across the carriageway of the road it meets (about 1,188 m district-wide). The side line is resampled at 0.5 m and a
            // sample within another road's half width (less 1 cm) or inside a circus's carriageway is dropped; the runs left are the pieces.
            // tools/geometry/visible.ts and tools/sim/solids.ts mirror it (Splice).
            var others = new System.Collections.Generic.List<(string id, Vector2[] line, float half, Rect box)>();
            foreach (var o in g.Roads) { var ol = Poly.CleanLine(o.Line); float hw = o.Width / 2f - 0.01f; float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue; foreach (var v in ol) { x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y); x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y); } others.Add((o.Id, ol, hw, Rect.MinMaxRect(x0 - hw, y0 - hw, x1 + hw, y1 + hw))); }
            var sink = RoadSinks(g); // Glyph's 0.2.113 review #5 (SeamProbe: 1,867 half-metre samples of two road batches coplanar at Boulevard x Market)
            foreach (var r in g.Roads)
            {
                var el = Nodes.Element(layer, r.Id);
                float cy = r.Level - (sink.TryGetValue(r.Id, out var sk) ? sk : 0f);
                nodes.Part(el, "carriageway", new MeshKit().Ribbon(r.Line, r.Width, cy, cy), nodes.Ground(r.Surface));
                // Kerb strips centred on the carriageway edges' outer side: offset lines at (width + kerb) / 2.
                var (l, rr) = Poly.Ribbon(Poly.CleanLine(r.Line), r.Width + r.KerbWidth, false);
                var drives = g.Drives.FindAll(d => d.Road == r.Id && d.Kerb is { Length: 2 });
                var kerbs = new MeshKit(); var dropped = new MeshKit(); var gutters = new MeshKit(); bool anyDrop = false;
                var roadLine = Poly.CleanLine(r.Line); bool gutter = nodes.Has(GutterSurface) && !NoGutter(r.Surface); // brief 028 road (Vela): no gutter on an enamel road
                foreach (var side in new[] { l, rr }) // brief 023: the kerb is dropped where a drive crosses it (the drive's kerb segment)
                    foreach (var (whole, drop, h) in KerbPieces(side, drives, r.KerbWidth))
                    foreach (var off in OffOtherRoads(whole, r.Id, others, g.Circuses))
                    foreach (var piece in OutsideRings(off, returns))
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
            // brief 023: a drive and the bay it reaches lie below the plot slab's top: the slab is cut round them (each a convex ring).
            // Brief 055 (Datum; Aster: follow the block, sunk): every bay is a carriageway-level lay-by, a drive or not (the square's and the
            // office's), so the slab is cut round every bay (tools/geometry visible.ts and sim.ts cut the same).
            var cuts = new System.Collections.Generic.List<Vector2[]>();
            foreach (var d in g.Drives) cuts.Add(d.Ring);
            foreach (var f in g.Flats) if (IsBay(f.Id)) cuts.Add(f.Ring);
            foreach (var p in g.Plots)
            {   // a plan signature's ground (SpaceHQ's drum) is the piece's own floor: the pavement slab stops at its edge
                var parts = KitCover.OutsideGrounds(Poly.Clean(p.Ring));
                foreach (var c in cuts) parts = CutConvex(parts, Poly.Clean(c));
                var kit = new MeshKit();
                foreach (var q in parts) kit.Prism(q, p.Base, p.Base + p.Height);
                nodes.Solid(layer, p.Id, kit, nodes.Ground(p.Surface));
            }
        }

        /// <summary>A flat that is a kerb bay (the civic block's `civic/bay/*`): a lay-by at carriageway level, cut out of the plot slab.</summary>
        public static bool IsBay(string id) => id.Contains("/bay/");
        /// <summary>Brief 028: a convex kerb return's carriageway fill (tools/world/gen/kerbreturn.ts fillRing; build.ts ground/return/&lt;id&gt;).</summary>
        public static bool IsReturn(string id) => id.StartsWith("ground/return/", System.StringComparison.Ordinal);

        /// <summary>The stretches of a polyline outside every ring (sampled every 0.1 m; a ring is a kerb return's fill): the whole line when none touches it.</summary>
        static System.Collections.Generic.List<Vector2[]> OutsideRings(Vector2[] line, System.Collections.Generic.List<Vector2[]> rings)
        {
            var outp = new System.Collections.Generic.List<Vector2[]>();
            if (rings.Count == 0 || line.Length < 2) { outp.Add(line); return outp; }
            { float lx0 = float.MaxValue, ly0 = float.MaxValue, lx1 = float.MinValue, ly1 = float.MinValue; foreach (var v in line) { lx0 = Mathf.Min(lx0, v.x); ly0 = Mathf.Min(ly0, v.y); lx1 = Mathf.Max(lx1, v.x); ly1 = Mathf.Max(ly1, v.y); }
              var lb = Rect.MinMaxRect(lx0, ly0, lx1, ly1); var near = new System.Collections.Generic.List<Vector2[]>();
              foreach (var rg in rings) { float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue; foreach (var v in rg) { x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y); x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y); } if (Rect.MinMaxRect(x0 - StripGrow, y0 - StripGrow, x1 + StripGrow, y1 + StripGrow).Overlaps(lb)) near.Add(rg); }
              if (near.Count == 0) { outp.Add(line); return outp; } rings = near; }
            var boxes = new System.Collections.Generic.List<Rect>(rings.Count); foreach (var rg in rings) { float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue; foreach (var v in rg) { x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y); x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y); } boxes.Add(Rect.MinMaxRect(x0 - StripGrow, y0 - StripGrow, x1 + StripGrow, y1 + StripGrow)); }
            bool Inside(Vector2 q) { for (int r = 0; r < rings.Count; r++) if (boxes[r].Contains(q) && InGrown(rings[r], q)) return true; return false; }
            var cur = new System.Collections.Generic.List<Vector2>();
            for (int i = 1; i < line.Length; i++)
            {
                Vector2 a = line[i - 1], b = line[i]; float len = Vector2.Distance(a, b); int n = Mathf.Max(1, Mathf.CeilToInt(len / 0.1f));
                for (int k = 0; k < n; k++)
                {
                    Vector2 p0 = Vector2.Lerp(a, b, k / (float)n), p1 = Vector2.Lerp(a, b, (k + 1) / (float)n);
                    if (Inside((p0 + p1) * 0.5f)) { if (cur.Count > 0) { if (cur[cur.Count - 1] != p0) cur.Add(p0); if (cur.Count >= 2) outp.Add(cur.ToArray()); } cur.Clear(); continue; }
                    if (cur.Count == 0) cur.Add(p0);
                    if (k == n - 1) cur.Add(p1); // keep the line's own vertices; the interior samples are not new corners
                }
            }
            if (cur.Count >= 2) outp.Add(cur.ToArray());
            if (outp.Count == 1 && outp[0].Length == line.Length) { outp[0] = line; }
            return outp;
        }
        /// <summary>m: a fill grown this much takes the 0.3 m kerb strip with it (tools/geometry/returns.ts STRIP_GROW).</summary>
        public const float StripGrow = 0.35f;
        static bool InGrown(Vector2[] ring, Vector2 p)
        {
            if (InRing(ring, p)) return true;
            for (int i = 0; i < ring.Length; i++) { Vector2 a = ring[i], b = ring[(i + 1) % ring.Length], ab = b - a; float t = ab.sqrMagnitude < 1e-9f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); if ((a + ab * t - p).sqrMagnitude <= StripGrow * StripGrow) return true; }
            return false;
        }
        /// <summary>The runs of a kerb line (resampled at 0.5 m) that lie on no other road's carriageway and in no circus's (Splice's rule).</summary>
        static System.Collections.Generic.List<Vector2[]> OffOtherRoads(Vector2[] line, string self, System.Collections.Generic.List<(string id, Vector2[] line, float half, Rect box)> roads, System.Collections.Generic.List<CircusDto> circuses)
        {
            var outp = new System.Collections.Generic.List<Vector2[]>(); if (line.Length < 2) { outp.Add(line); return outp; }
            var pts = new System.Collections.Generic.List<Vector2> { line[0] };
            for (int i = 1; i < line.Length; i++) { float l = Vector2.Distance(line[i - 1], line[i]); int n = Mathf.Max(1, Mathf.CeilToInt(l / 0.5f)); for (int k = 1; k <= n; k++) pts.Add(Vector2.Lerp(line[i - 1], line[i], (float)k / n)); }
            bool Blocked(Vector2 p)
            {
                foreach (var o in roads)
                {
                    if (o.id == self || !o.box.Contains(p)) continue;
                    for (int i = 1; i < o.line.Length; i++) { Vector2 a = o.line[i - 1], b = o.line[i], ab = b - a; float t = ab.sqrMagnitude < 1e-9f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); if ((a + ab * t - p).sqrMagnitude < o.half * o.half) return true; }
                }
                foreach (var c in circuses) if ((p - c.Centre).sqrMagnitude < c.Radius * c.Radius) return true;
                return false;
            }
            var cur = new System.Collections.Generic.List<Vector2>(); int dropped = 0;
            foreach (var p in pts) { if (Blocked(p)) { dropped++; if (cur.Count >= 2) outp.Add(cur.ToArray()); cur.Clear(); } else cur.Add(p); }
            if (cur.Count >= 2) outp.Add(cur.ToArray());
            if (dropped == 0) { outp.Clear(); outp.Add(line); } // untouched: the line as it was (its own vertices)
            return outp;
        }

        static bool InRing(Vector2[] ring, Vector2 p)
        {
            bool inside = false; int n = ring.Length;
            for (int i = 0, j = n - 1; i < n; j = i++) { var a = ring[i]; var b = ring[j]; if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside; }
            return inside;
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
    
        /// <summary>Glyph's 0.2.113 review #5: where two carriageways cross (or a road meets a circus) both ribbons lay the junction at one level,
        /// from two batches, and z-fight into faint tonal steps and hairlines. The junction is drawn by the higher road (a circus first, then the
        /// wider, then by id): each road takes the shallowest step (SinkStep each) that no higher road it overlaps holds, so two crossing roads
        /// never share a level and a crossing is never coplanar. Millimetres: the kerbs, gutters and the road's own pieces stand on their own levels; the
        /// walk file's 5 cm tests do not see it. No export change.</summary>
        public const float SinkStep = 0.003f;
        static System.Collections.Generic.Dictionary<string, float> RoadSinks(GroundDto g)
        {
            var roads = new System.Collections.Generic.List<RoadDto>(g.Roads);
            roads.Sort((a, b) => b.Width != a.Width ? b.Width.CompareTo(a.Width) : string.CompareOrdinal(a.Id, b.Id));
            var step = new System.Collections.Generic.Dictionary<string, int>(); var lines = new System.Collections.Generic.Dictionary<string, Vector2[]>();
            foreach (var r in roads) lines[r.Id] = Poly.CleanLine(r.Line);
            static float SegD(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float t = ab.sqrMagnitude < 1e-8f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return (a + ab * t - p).magnitude; }
            static float LineD(Vector2 p, Vector2[] l) { float d = float.MaxValue; for (int i = 1; i < l.Length; i++) d = Mathf.Min(d, SegD(p, l[i - 1], l[i])); return d; }
            bool Overlaps(Vector2[] a, float ha, Vector2[] b, float hb)
            {
                for (int i = 1; i < a.Length; i++)
                {
                    float len = Vector2.Distance(a[i - 1], a[i]); int n = Mathf.Max(1, Mathf.CeilToInt(len / 1f));
                    for (int s = 0; s <= n; s++) if (LineD(Vector2.Lerp(a[i - 1], a[i], s / (float)n), b) < hb + 0.01f) return true; // a's centreline inside b's carriageway
                }
                return false;
            }
            bool OnCircus(Vector2[] l, float h) { foreach (var c in g.Circuses) foreach (var v in l) if (Vector2.Distance(v, c.Centre) < c.Radius + h) return true; return false; }
            var sink = new System.Collections.Generic.Dictionary<string, float>();
            for (int i = 0; i < roads.Count; i++)
            {
                var r = roads[i]; var taken = new System.Collections.Generic.HashSet<int>(); if (OnCircus(lines[r.Id], r.Width / 2f)) taken.Add(0); // a circus is level 0
                for (int j = 0; j < i; j++) { var o = roads[j]; if (Overlaps(lines[r.Id], r.Width / 2f, lines[o.Id], o.Width / 2f) || Overlaps(lines[o.Id], o.Width / 2f, lines[r.Id], r.Width / 2f)) taken.Add(step[o.Id]); }
                int k = 0; while (taken.Contains(k)) k++; // the shallowest step no road it crosses holds (a colouring: two crossing roads never share a level; 0.2.113 first try stacked 7 deep, 21 mm)
                step[r.Id] = k; if (k > 0) sink[r.Id] = k * SinkStep;
            }
            int deepest = 0; foreach (var v in step.Values) deepest = Mathf.Max(deepest, v);
            Debug.Log($"World: carriageways under a crossing road: {sink.Count} of {roads.Count} sunk, deepest {deepest} step(s) of {SinkStep * 1000f:0} mm (Glyph 0.2.113 #5)");
            return sink;
        }
}
}
