using UnityEngine;

namespace Suborned.World
{
    /// <summary>Brief 026 round 2 (Vela's round 1 review): the kerb as a stone, not a slab. Each kept kerb piece becomes a granite top and a
    /// face toward the carriageway (the upstand that draws the kerb line), end caps where the piece stops (a drive, a junction), and a
    /// gutter strip on the road along its foot. UVs are metres: top u along the kerb, v across (0 at the road edge); face u along, v up from
    /// the road; gutter u along, v 0 at its road side to 0.375 at the kerb face (DK_Ground_Gutter's sheet is 4 × 0.375 m, u along the kerb,
    /// v road edge to kerb: tools/decokit/ground_tex.py). The materials' m_Scale = 1 / tile does the repeat.</summary>
    public static class KerbKit
    {
        public const float GutterWidth = 0.375f, GutterLift = 0.0015f, FaceProud = 0.003f, TopProud = 0.004f;
        /// <summary>The pavement slab's top (the plots' slab, 0.18 m): a full-height stone stands TopProud over it, so its granite top reads as a line.</summary>
        public static float PaveTop; // over the asphalt, under the crossing inlays' 2 mm
        public static int Stones, Gutters, Covered;

        /// <summary>Kit kerb stones the export places (ground.pieces kind `kerb`, Kerb_Granite_*: stone and gutter setts in one piece): where one
        /// lies, the builder draws neither its stone nor its gutter, so a kerb is drawn once. Hashed on a 2 m grid by pivot.</summary>
        static readonly System.Collections.Generic.Dictionary<(int, int), System.Collections.Generic.List<Vector2>> kitKerbs = new();
        public static void KitKerbs(System.Collections.Generic.IEnumerable<StreetItemDto> pieces)
        {
            kitKerbs.Clear(); Stones = Gutters = Covered = 0;
            foreach (var p in pieces)
            {
                if (p.Kind != "kerb") continue;
                var k = (Mathf.FloorToInt(p.At.x / 2f), Mathf.FloorToInt(p.At.y / 2f));
                if (!kitKerbs.TryGetValue(k, out var l)) kitKerbs[k] = l = new();
                l.Add(p.At);
            }
        }
        const float KitReach = 1.25f; // a 2 m stone's pivot reaches 1 m along it, + the 0.15 m from the kerb's centreline
        static bool ByKit(Vector2 p)
        {
            int x = Mathf.FloorToInt(p.x / 2f), y = Mathf.FloorToInt(p.y / 2f);
            for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++)
                    if (kitKerbs.TryGetValue((x + i, y + j), out var l)) foreach (var q in l) if ((q - p).sqrMagnitude < KitReach * KitReach) return true;
            return false;
        }

        /// <summary>The line resampled to steps of at most `step` metres (so a kit stone covers whole segments).</summary>
        static Vector2[] Resample(Vector2[] line, float step)
        {
            var r = new System.Collections.Generic.List<Vector2> { line[0] };
            for (int i = 1; i < line.Length; i++)
            {
                float l = Vector2.Distance(line[i - 1], line[i]); int n = Mathf.Max(1, Mathf.CeilToInt(l / step));
                for (int k = 1; k <= n; k++) r.Add(Vector2.Lerp(line[i - 1], line[i], (float)k / n));
            }
            return r.ToArray();
        }

        /// <summary>One kerb piece (its centreline, `piece`) beside the road whose centreline is `road`: stone into `stone`, gutter into
        /// `gutter` (null = none). lo/hi: the stone's foot and top; roadLevel: the carriageway's surface.</summary>
        public static void Build(MeshKit stone, MeshKit gutter, Vector2[] piece, Vector2[] road, float width, float lo, float hi, float roadLevel)
        {
            piece = Poly.CleanLine(piece);
            if (piece.Length < 2 || hi - lo < 0.005f && gutter == null) return;
            if (kitKerbs.Count > 0) piece = Resample(piece, 1f);
            var (a, b) = Poly.Ribbon(piece, width, false);
            int n = piece.Length, mid = n / 2;
            Poly.NearestTangent(road, a[mid], out float da); Poly.NearestTangent(road, b[mid], out float db);
            var R = da < db ? a : b; var O = da < db ? b : a; // R: the road-side edge (the kerb face), O: the pavement side
            // the face stands 3 mm proud of the plot slab's own edge (they share the kerb line: plots run to the kerb, plan standard), so they never fight
            for (int i = 0; i < n; i++) R[i] += (R[i] - O[i]).normalized * FaceProud;
            float h = hi - lo, s = 0f;
            for (int i = 0; i < n - 1; i++)
            {
                float s1 = s + Vector2.Distance(piece[i], piece[i + 1]);
                if (s1 - s < 1e-4f) continue;
                if (kitKerbs.Count > 0 && ByKit((piece[i] + piece[i + 1]) * 0.5f)) { Covered++; s = s1; continue; }
                Vector2 into0 = (R[i] - O[i]).normalized, into1 = (R[i + 1] - O[i + 1]).normalized; // toward the road
                var toRoad = new Vector3(into0.x + into1.x, 0f, into0.y + into1.y);
                if (h > 0.005f)
                {
                    Quad(stone, new[] { W(R[i], hi), W(R[i + 1], hi), W(O[i + 1], hi), W(O[i], hi) },
                        new[] { new Vector2(s, 0f), new Vector2(s1, 0f), new Vector2(s1, width), new Vector2(s, width) }, Vector3.up);
                    Quad(stone, new[] { W(R[i], lo), W(R[i + 1], lo), W(R[i + 1], hi), W(R[i], hi) },
                        new[] { new Vector2(s, 0f), new Vector2(s1, 0f), new Vector2(s1, h), new Vector2(s, h) }, toRoad);
                    Stones++;
                }
                if (gutter != null)
                {
                    Vector2 g0 = R[i] + into0 * GutterWidth, g1 = R[i + 1] + into1 * GutterWidth; float y = roadLevel + GutterLift;
                    Quad(gutter, new[] { W(g0, y), W(g1, y), W(R[i + 1], y), W(R[i], y) },
                        new[] { new Vector2(s, 0f), new Vector2(s1, 0f), new Vector2(s1, GutterWidth), new Vector2(s, GutterWidth) }, Vector3.up);
                    Gutters++;
                }
                s = s1;
            }
            if (h <= 0.005f) return;
            // end caps: the stone's cut ends
            for (int e = 0; e < 2; e++)
            {
                int i = e == 0 ? 0 : n - 1, j = e == 0 ? 1 : n - 2;
                var outward = piece[i] - piece[j]; var o = new Vector3(outward.x, 0f, outward.y);
                Quad(stone, new[] { W(R[i], lo), W(O[i], lo), W(O[i], hi), W(R[i], hi) },
                    new[] { new Vector2(0f, 0f), new Vector2(width, 0f), new Vector2(width, h), new Vector2(0f, h) }, o);
            }
        }

        static Vector3 W(Vector2 p, float y) => new(p.x, y, p.y);

        /// <summary>A quad wound so its front faces `want` (Unity: cross(p1 - p0, p2 - p0) is the front normal).</summary>
        static void Quad(MeshKit k, Vector3[] p, Vector2[] uv, Vector3 want)
        {
            var nrm = Vector3.Cross(p[1] - p[0], p[2] - p[0]);
            int[] i = new int[4];
            for (int c = 0; c < 4; c++) i[c] = k.Vert(p[c], uv[c]);
            if (Vector3.Dot(nrm, want) >= 0f) { k.Tri(i[0], i[1], i[2]); k.Tri(i[0], i[2], i[3]); }
            else { k.Tri(i[0], i[2], i[1]); k.Tri(i[0], i[3], i[2]); }
        }
    }
}
