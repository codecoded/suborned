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

        /// <summary>The kerb runs Ground built this build (the kerb's centre line after the drive drop, OffOtherRoads and OutsideRings), with their road's line,
        /// level and kerb width: ContactShade lays its kerb-foot strip along exactly these (Vela, 0.2.116: the strip ran both full edges of every road, across
        /// other roads' carriageways at every junction, the X seam; Splice measured 1,222 m inside other carriageways and 1,005 m in return fills).</summary>
        public static readonly System.Collections.Generic.List<(Vector2[] centre, Vector2[] roadLine, float level, float kerbWidth)> KerbFeet = new();

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
            KerbFeet.Clear();
            var sheets = RoadClip.Sheets(g); // Glyph 0.2.113/0.2.115 #5, Splice: each point of carriageway drawn once, at its road's level (no coplanar overlap, no step)
            foreach (var r in g.Roads)
            {
                var el = Nodes.Element(layer, r.Id);
                var kitC = new MeshKit(); foreach (var piece in sheets[r.Id]) kitC.Flat(piece, r.Level);
                nodes.Part(el, "carriageway", kitC, nodes.Ground(r.Surface));
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
                        if (!drop) { KerbKit.Build(kerbs, gutter ? gutters : null, piece, roadLine, r.KerbWidth, r.Level, Mathf.Max(r.Level + r.KerbHeight, KerbKit.PaveTop + KerbKit.TopProud), r.Level); if (r.KerbHeight > 0.02f) KerbFeet.Add((piece, roadLine, r.Level, r.KerbWidth)); } // Vela and Splice: the contact shade's kerb feet follow the kerb runs built, nothing else
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
            var tables = FootwayKit.ByTable(g); FootwayKit.Built = FootwayKit.Bands = 0; // brief 061: a footway's table is its element: table, lip, ramp and bands (FootwayKit)
            // brief 075: the rail tiles (ground.pieces tram-rail, top at their z) lie at the setts top: the setts flat steps aside under each tile's
            // footprint (less 2 cm a side, so no seam opens), else the two coplanar surfaces z-fight into a patchwork at distance
            var rails = new System.Collections.Generic.List<(Vector2[] ring, Rect box, float z)>();
            foreach (var p in g.Pieces) if (p.Kind == "tram-rail") { var rr = Poly.Rect(p.At, new Vector2(Mathf.Cos(p.Yaw), Mathf.Sin(p.Yaw)), Mathf.Max(0.01f, p.Size.x - 0.04f), Mathf.Max(0.01f, p.Size.y - 0.04f)); rails.Add((rr, BoxOf(rr), p.Z)); }
            int railCut = 0;
            foreach (var f in g.Flats)
                if (tables.TryGetValue(f.Id, out var fw)) FootwayKit.Build(nodes, Nodes.Element(layer, f.Id), fw);
                else if (rails.Count > 0 && RailCut(f, rails) is { } parts)
                {
                    var mk = new MeshKit(); foreach (var q in parts) mk.Flat(q, f.Level);
                    nodes.Solid(layer, f.Id, mk, nodes.Ground(f.Surface)); railCut++;
                }
                else nodes.Solid(layer, f.Id, new MeshKit().Flat(f.Ring, f.Level), nodes.Ground(f.Surface));
            if (rails.Count > 0) Debug.Log($"World: {railCut} flats cut round {rails.Count} rail tiles at their top (brief 075: no coplanar setts under the flush rails)");
            if (FootwayKit.Built != g.Footways.Count) throw new System.IO.InvalidDataException($"World: {g.Footways.Count} footways in the export, {FootwayKit.Built} with their table in ground.flats");
            if (g.Footways.Count > 0) Debug.Log($"World: continuous footways {FootwayKit.Built} (brief 061: table, lip, ramp and {FootwayKit.Bands} bands; posts with the street)");
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

        static Rect BoxOf(Vector2[] ring) { float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue; foreach (var v in ring) { x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y); x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y); } return Rect.MinMaxRect(x0, y0, x1, y1); }

        /// <summary>Brief 075: the flat less every rail tile whose top lies on it (within 5 mm) and whose footprint overlaps it; null when none does.</summary>
        static System.Collections.Generic.List<Vector2[]> RailCut(WaterDto f, System.Collections.Generic.List<(Vector2[] ring, Rect box, float z)> rails)
        {
            var box = BoxOf(f.Ring); System.Collections.Generic.List<Vector2[]> parts = null;
            foreach (var (ring, rb, z) in rails)
            {
                if (Mathf.Abs(z - f.Level) > 0.005f || !rb.Overlaps(box)) continue;
                parts ??= new System.Collections.Generic.List<Vector2[]> { Poly.Clean(f.Ring) };
                var keep = new System.Collections.Generic.List<Vector2[]>(); var cut = new System.Collections.Generic.List<Vector2[]>();
                foreach (var q in parts) (BoxOf(q).Overlaps(rb) ? cut : keep).Add(q);
                if (cut.Count == 0) continue;
                keep.AddRange(CutConvex(cut, (Vector2[])ring.Clone())); parts = keep;
            }
            return parts;
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

        public void Transit(Transform layer, TransitDto t, System.Collections.Generic.HashSet<string> railCovered = null, KitPlacer placer = null)
        {
            foreach (var x in t.Tram)
            {
                var beams = Beams(x.Line, x.Spacing, x.Width, x.Level - x.Depth, x.Level);
                // brief 075: where the export lays rail tiles along the track (TramStreet.RailCovered), the strip (2 cm proud of the flush rails) is not drawn:
                // its marker stands covered at the strip's volume, as an element a kit piece elsewhere stands in for
                if (railCovered != null && railCovered.Contains(x.Id)) { Combine.Cover(Nodes.Element(layer, x.Id), beams.Bounds); continue; }
                nodes.Solid(layer, x.Id, beams, x.Surface);
            }
            foreach (var m in t.Stops) Mass(layer, m);
            foreach (var m in t.Stations) Mass(layer, m);
            if (t.SkyTrack != null && placer != null) { SkyTrack(layer, t, placer); return; } // brief 075 roll-out: the old sky track replaces the ribbons and square posts
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

        /// <summary>Brief 075 roll-out (Vela's "Roll-out: sky track", the old DecoKitSkyRail.Beam/Sweep): each skyTrack beam is SkyRail_Beam_4m's section
        /// (Width x Depth, corners filleted Fillet, 4 points a corner at 0/30/60/90 deg, top at the beam's Top) swept along its centre line, world up, no
        /// banking; UVs metric (u = metres along x UPerMetre, v = metres round the section), smooth normals (each point's own: the fillet's radial, the
        /// flats' face normal), in DK_Hull_Ivory; the teal stripe a proud band on each flank (z StripeLo..StripeHi, 0.005 inside to StripeProud out) in
        /// DK_Enamel_Teal; ends capped. Cut into 24 m pieces so the batches tile and cull. Pylons: the kit stack per export pylon (foot, shaft stretched in
        /// height, head or twin head), each on its line's element. The skyRail lines keep their elements (ids), now holding these; no ribbon, no post.</summary>
        void SkyTrack(Transform layer, TransitDto t, KitPlacer placer)
        {
            var st = t.SkyTrack;
            Material body = KitImport.Library("DK_Hull_Ivory") ?? throw new System.IO.InvalidDataException("World: sky track: DK_Hull_Ivory missing from Kit/Materials (bun run kit)");
            Material stripe = KitImport.Library("DK_Enamel_Teal") ?? throw new System.IO.InvalidDataException("World: sky track: DK_Enamel_Teal missing from Kit/Materials (bun run kit)");
            var els = new System.Collections.Generic.Dictionary<string, Transform>();
            Transform Owner(string id)
            {
                foreach (var s in t.SkyRail) if (id.StartsWith(s.Id + "/", System.StringComparison.Ordinal)) { if (!els.TryGetValue(s.Id, out var e)) els[s.Id] = e = Nodes.Element(layer, s.Id); return e; }
                throw new System.IO.InvalidDataException($"World: sky track {id} belongs to no transit.skyRail line");
            }
            // the section (x across, z up, top at 0), CCW, and each point's outward normal
            float hw = st.Width * 0.5f, r = st.Fillet, d = st.Depth;
            var sec = new System.Collections.Generic.List<(Vector2 p, Vector2 n)>();
            foreach (var (x, z, a0) in new[] { (-hw, -d, 180f), (hw, -d, 270f), (hw, 0f, 0f), (-hw, 0f, 90f) })
            {
                float cx = x - Mathf.Sign(x) * r, cz = z + (z < 0f ? r : -r);
                for (int k = 0; k < 4; k++) { float a = (a0 + 30f * k) * Mathf.Deg2Rad; var n = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); sec.Add((new Vector2(cx, cz) + n * r, n)); }
            }
            var per = new float[sec.Count + 1];
            for (int i = 0; i < sec.Count; i++) per[i + 1] = per[i] + Vector2.Distance(sec[i].p, sec[(i + 1) % sec.Count].p);
            int beams = 0, pieces = 0, tris = 0;
            foreach (var b in st.Beams)
            {
                var el = Owner(b.Id); var line = Poly.CleanLine(b.Line); int n = line.Length;
                if (n < 2) throw new System.IO.InvalidDataException($"World: sky track {b.Id}: a beam line of {n} points");
                bool closed = Vector2.Distance(line[0], line[n - 1]) < 0.05f; if (closed) n--;
                var s = new float[n]; for (int i = 1; i < n; i++) s[i] = s[i - 1] + Vector2.Distance(line[i - 1], line[i]);
                Vector2 Tan(int i)
                {
                    Vector2 a = i > 0 ? line[i - 1] : closed ? line[n - 1] : line[0], c = i < n - 1 ? line[i + 1] : closed ? line[0] : line[n - 1];
                    return (c - a).normalized;
                }
                // ring i in world space: point j of the section and its normal
                (Vector3 p, Vector3 nr) Ring(int i, Vector2 q, Vector2 qn)
                {
                    var tg = Tan(i); var side = new Vector2(tg.y, -tg.x); // plan right of the direction
                    var c = line[i];
                    return (new Vector3(c.x + side.x * q.x, b.Top + q.y, c.y + side.y * q.x), new Vector3(side.x * qn.x, qn.y, side.y * qn.x));
                }
                int rings = closed ? n + 1 : n; const int Chunk = 12; // 12 steps of ~2 m a piece
                for (int r0 = 0; r0 < rings - 1; r0 += Chunk)
                {
                    int r1 = Mathf.Min(rings - 1, r0 + Chunk);
                    var kb = new MeshKit(); var ks = new MeshKit();
                    // body: one vertex per section point per ring (normals analytic, so the flats stay flat)
                    int[,] idx = new int[r1 - r0 + 1, sec.Count + 1];
                    for (int ri = r0; ri <= r1; ri++)
                    {
                        int i = ri % n; float u = (ri == n ? s[n - 1] + Vector2.Distance(line[n - 1], line[0]) : s[i]) * st.UPerMetre;
                        for (int j = 0; j <= sec.Count; j++) { var (p, nr) = Ring(i, sec[j % sec.Count].p, sec[j % sec.Count].n); idx[ri - r0, j] = kb.Vert(p, new Vector2(u, per[j]), nr); }
                    }
                    for (int ri = r0; ri < r1; ri++)
                        for (int j = 0; j < sec.Count; j++)
                            Quad(kb, idx[ri - r0, j], idx[ri - r0, j + 1], idx[ri - r0 + 1, j + 1], idx[ri - r0 + 1, j]);
                    // stripes: a proud band on each flank, flat faces (outer, top, bottom)
                    foreach (float sideSign in new[] { -1f, 1f })
                    {
                        float xi = sideSign * (hw - 0.005f), xo = sideSign * (hw + st.StripeProud);
                        var faces = new[] { (new Vector2(xi, st.StripeHi), new Vector2(xo, st.StripeHi), Vector2.up), (new Vector2(xo, st.StripeHi), new Vector2(xo, st.StripeLo), new Vector2(sideSign, 0f)), (new Vector2(xo, st.StripeLo), new Vector2(xi, st.StripeLo), Vector2.down) };
                        foreach (var (qa, qb, qn) in faces)
                        {
                            int prevA = -1, prevB = -1; float vb = Vector2.Distance(qa, qb);
                            for (int ri = r0; ri <= r1; ri++)
                            {
                                int i = ri % n; float u = (ri == n ? s[n - 1] + Vector2.Distance(line[n - 1], line[0]) : s[i]) * st.UPerMetre;
                                var (pa, na) = Ring(i, qa, qn); var (pb, _) = Ring(i, qb, qn);
                                int ia = ks.Vert(pa, new Vector2(u, 0f), na), ib = ks.Vert(pb, new Vector2(u, vb), na);
                                if (prevA >= 0) Quad(ks, prevA, prevB, ib, ia);
                                prevA = ia; prevB = ib;
                            }
                        }
                    }
                    if (!closed && r0 == 0) { var tg = Tan(0); Cap(kb, sec.ConvertAll(q => Ring(0, q.p, q.n).p), new Vector3(-tg.x, 0f, -tg.y)); }
                    if (!closed && r1 == rings - 1) { var tg = Tan(n - 1); Cap(kb, sec.ConvertAll(q => Ring(n - 1, q.p, q.n).p), new Vector3(tg.x, 0f, tg.y)); }
                    nodes.Part(el, $"beam/{b.Id}/{pieces}", kb, body); nodes.Part(el, $"stripe/{b.Id}/{pieces}", ks, stripe);
                    tris += kb.TriangleCount + ks.TriangleCount; pieces++;
                }
                beams++;
            }
            int pylons = 0;
            foreach (var p in st.Pylons)
            {
                var el = Owner(p.Id);
                if (p.Foot != null) placer.Place(el, layer.name, p.Foot, p.Foot.At, p.FootZ, p.Yaw);
                placer.Place(el, layer.name, p.Shaft, p.Shaft.At, p.ShaftZ, p.Yaw);
                KitLiveries.Apply(placer.Place(el, layer.name, p.Head, p.Head.At, p.HeadZ, p.Yaw), p.Id, "head", p.Head.Id);
                pylons++;
            }
            foreach (var s in t.SkyRail) if (!els.ContainsKey(s.Id)) Debug.LogWarning($"World: sky track: line {s.Id} has no beam or pylon in transit.skyTrack: not drawn");
            Debug.Log($"World: sky track (brief 075 roll-out): {beams} beams swept in {pieces} pieces ({tris} tris, section {sec.Count} points, {st.Width} x {st.Depth} r {st.Fillet}), {pylons} pylon stacks; skyRail ribbons and posts retired");
        }

        /// <summary>A quad a-b-c-d, wound so its face points along the vertices' own normals (Unity's clockwise front).</summary>
        static void Quad(MeshKit k, int a, int b, int c, int d)
        {
            Vector3 pa = k.At(a), pb = k.At(b), pc = k.At(c), pd = k.At(d);
            var face = Vector3.Cross(pb - pa, pc - pa); if (face.sqrMagnitude < 1e-12f) face = Vector3.Cross(pc - pa, pd - pa);
            if (Vector3.Dot(face, k.NormalAt(a) + k.NormalAt(c)) >= 0f) { k.Tri(a, b, c); k.Tri(a, c, d); } else { k.Tri(a, c, b); k.Tri(a, d, c); }
        }

        /// <summary>A flat end cap (a fan about the section's centre) over the ring's points, facing `outward`.</summary>
        static void Cap(MeshKit k, System.Collections.Generic.List<Vector3> ring, Vector3 outward)
        {
            var mid = Vector3.zero; foreach (var q in ring) mid += q; mid /= ring.Count;
            int centre = k.Vert(mid, Vector2.zero, outward);
            var at = new int[ring.Count];
            for (int j = 0; j < ring.Count; j++) at[j] = k.Vert(ring[j], new Vector2(ring[j].x, ring[j].y), outward);
            for (int j = 0; j < ring.Count; j++)
            {
                int x = at[j], y = at[(j + 1) % ring.Count];
                if (Vector3.Dot(Vector3.Cross(ring[j] - mid, ring[(j + 1) % ring.Count] - mid), outward) >= 0f) k.Tri(centre, x, y); else k.Tri(centre, y, x);
            }
        }

        /// <summary>One ribbon, or (spacing > 0) two ribbons spacing apart either side of the line.</summary>
        static MeshKit Beams(Vector2[] line, float spacing, float width, float lo, float hi)
        {
            if (spacing <= 0) return new MeshKit().Ribbon(line, width, lo, hi);
            var (l, r) = Poly.Ribbon(Poly.CleanLine(line), spacing, false);
            return new MeshKit().Ribbon(l, width, lo, hi).Ribbon(r, width, lo, hi);
        }

        /// <summary>A landmark with a DecoKit ref is that piece on its centre and base. Brief 075 NP-1 (Splice's rule, Sky: "ugly blue buildings"): the
        /// only procedural landmark left is a caisson's drum (a cylinder, smooth-shaded) under its `kits` (the pads) standing on top at their own place and
        /// yaw; a landmark with no piece and no pads, or a sphere without its piece, is a placeholder and fails the build.</summary>
        public void Landmarks(Transform layer, System.Collections.Generic.List<LandmarkDto> landmarks, KitPlacer placer)
        {
            foreach (var l in landmarks)
            {
                Transform el;
                if (l.Kit != null) { el = Nodes.Element(layer, l.Id); KitLiveries.Apply(placer.Place(el, layer.name, l.Kit, l.Centre, l.Base, 0f), l.Id, "kit", l.Kit.Id); }
                else
                {
                    if (l.Kits.Count == 0) throw new System.IO.InvalidDataException($"World: NP-1 {l.Id}: a landmark with no kit piece and no pads would stand as a placeholder ({l.Shape}, source {l.Source ?? "none"})");
                    if (l.Shape != "cylinder") throw new System.IO.InvalidDataException($"World: NP-1 {l.Id}: only a caisson drum (cylinder) stands procedural under its pads; a {l.Shape} needs its kit piece");
                    using (Nodes.Drum()) el = nodes.Solid(layer, l.Id, new MeshKit().Prism(Poly.Circle(l.Centre, l.Radius, 64), l.Base, l.Base + l.Height), l.Surface);
                    for (int i = 0; i < l.Kits.Count; i++) { var k = l.Kits[i]; KitLiveries.Apply(placer.Place(el, layer.name, k, k.At, l.Base + l.Height, 0f), l.Id, $"kits[{i}]", k.Id); }
                }
                if (l.Gateway != null)
                {   // brief 075 (Splice): the landmark's gateway arch (the Crown Avenue arch), on the landmark's own element: pivot between its feet on the
                    // pavement top, kit +X turned by its yaw (0 = feet across the avenue on plan x)
                    if (float.IsNaN(l.Gateway.Yaw)) throw new System.IO.InvalidDataException($"World: {l.Id}: gateway without a yaw");
                    float ground = KerbKit.PaveTop > 0f ? KerbKit.PaveTop : l.Base;
                    KitLiveries.Apply(placer.Place(el, layer.name, l.Gateway, l.Gateway.At, ground, 0f), l.Id, "gateway", l.Gateway.Id);
                }
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
