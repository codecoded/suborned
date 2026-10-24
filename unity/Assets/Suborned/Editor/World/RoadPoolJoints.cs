using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Glyph's 0.2.104 review #4 (night: radial spikes in the strip light's pool at a rounded corner): the export lays the kerb strip
    /// light round a kerb return as straight chords of RoadRibbon_Straight_6m, and each chord's pool band (1 m on the carriageway side,
    /// DK_Road_Pool, additive; UV v 0 at the ribbon to 1 across, CrossGlow _Mode 3) is a rectangle. On the outside of an arc neighbouring bands
    /// leave a thin wedge of dark road between them; on the inside they overlap and the additive pool doubles: a fan of dark or bright spikes.
    /// GroundPieces holds the bands (Collecting) instead of baking them; Fill matches each band's end edge with the next chord's start edge (the
    /// ribbon-side corners within 0.15 m) and mitres the far corners: both move to where the two bands' far edges meet, so the wedge closes and
    /// the overlap goes. Then every band is baked. A joint whose mitre would move a corner more than 0.6 m (a sharp turn) keeps its corners and
    /// gets a filler quad on the open side, as before.</summary>
    public static class RoadPoolJoints
    {
        public const string Pool = "DK_Road_Pool";
        /// <summary>Between Clear and Fill: KitPlacer hands pool bands here instead of baking them.</summary>
        public static bool Collecting { get; private set; }
        sealed class Band { public Transform El; public string Layer; public Material Mat; public MeshKit Kit; public int StartNear, StartFar, EndNear, EndFar; }
        static readonly List<Band> bands = new();
        public static int Filled { get; private set; }
        public static int Mitred { get; private set; }

        public static void Clear() { bands.Clear(); Filled = 0; Mitred = 0; Collecting = true; }

        /// <summary>A pool band as placed (world vertices and UVs), held until Fill bakes it.</summary>
        public static void Add(Transform el, string layer, MeshKit kit, Material mat)
        {
            int n = kit.VertexCount;
            if (n < 4) { Combine.Baked(el, layer, kit, mat); return; }
            // the long axis from the ribbon-side vertices (v < 0.5): round a tight return a chord (about 0.3 m) is shorter than the band (1 m),
            // so the farthest pair of all is a diagonal
            int fa = -1, fb = -1; float far = -1f;
            for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++)
                {
                    if (kit.UvAt(i).y >= 0.5f || kit.UvAt(j).y >= 0.5f) continue;
                    float d = (Flat(kit.At(i)) - Flat(kit.At(j))).sqrMagnitude; if (d > far) { far = d; fa = i; fb = j; }
                }
            if (fa < 0 || far < 1e-6f) { Combine.Baked(el, layer, kit, mat); return; }
            var axis = (Flat(kit.At(fb)) - Flat(kit.At(fa))).normalized;
            float Along(int i) => Vector2.Dot(Flat(kit.At(i)), axis);
            int sn = -1, sf = -1, en = -1, ef = -1;
            for (int i = 0; i < n; i++)
            {
                bool near = kit.UvAt(i).y < 0.5f;
                if (near) { if (sn < 0 || Along(i) < Along(sn)) sn = i; if (en < 0 || Along(i) > Along(en)) en = i; }
                else { if (sf < 0 || Along(i) < Along(sf)) sf = i; if (ef < 0 || Along(i) > Along(ef)) ef = i; }
            }
            if (sf < 0 || ef < 0 || sf == ef) { Combine.Baked(el, layer, kit, mat); return; }
            bands.Add(new Band { El = el, Layer = layer, Mat = mat, Kit = kit, StartNear = sn, StartFar = sf, EndNear = en, EndFar = ef });
        }

        static Vector2 Flat(Vector3 p) => new(p.x, p.z);

        /// <summary>Every vertex of the band at the far corner's place (a split mesh may repeat it) moves to `to`.</summary>
        static void Move(Band b, Vector3 from, Vector3 to)
        {
            for (int i = 0; i < b.Kit.VertexCount; i++) if ((b.Kit.At(i) - from).sqrMagnitude < 1e-6f) b.Kit.SetAt(i, to);
        }

        /// <summary>Mitres every chord joint, bakes the bands, fills what a mitre could not close; returns the joints closed.</summary>
        public static int Fill()
        {
            Collecting = false;
            // the edges: (band, near corner, far corner, the far corner at the band's other end)
            var edges = new List<(int band, int near, int far, int otherFar)>();
            for (int i = 0; i < bands.Count; i++) { var b = bands[i]; edges.Add((i, b.StartNear, b.StartFar, b.EndFar)); edges.Add((i, b.EndNear, b.EndFar, b.StartFar)); }
            var grid = new Dictionary<(int, int), List<int>>();
            (int, int) Cell(Vector3 p) => (Mathf.FloorToInt(p.x / 0.5f), Mathf.FloorToInt(p.z / 0.5f));
            for (int e = 0; e < edges.Count; e++) { var c = Cell(bands[edges[e].band].Kit.At(edges[e].near)); if (!grid.TryGetValue(c, out var l)) grid[c] = l = new(); l.Add(e); }
            var moves = new List<(Band b, Vector3 from, Vector3 to)>(); var used = new HashSet<int>(); var fills = new List<(Band b, Vector3 a0, Vector2 u0, Vector3 a1, Vector2 u1, Vector3 b1, Vector2 v1, Vector3 b0, Vector2 v0)>();
            for (int e = 0; e < edges.Count; e++)
            {
                if (used.Contains(e)) continue;
                var E = edges[e]; var A = bands[E.band]; var an = A.Kit.At(E.near); var (cx, cz) = Cell(an);
                int best = -1; float bestD = 0.15f;
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                    {
                        if (!grid.TryGetValue((cx + dx, cz + dz), out var near)) continue;
                        foreach (var f in near) { if (f == e || used.Contains(f) || edges[f].band == E.band) continue; float d = Vector3.Distance(an, bands[edges[f].band].Kit.At(edges[f].near)); if (d <= bestD) { bestD = d; best = f; } }
                    }
                if (best < 0) continue;
                var F = edges[best]; var B = bands[F.band];
                Vector3 af = A.Kit.At(E.far), bf = B.Kit.At(F.far);
                if ((af - bf).sqrMagnitude < 1e-4f) { used.Add(e); used.Add(best); continue; } // a straight run: the corners meet
                // the far edges' lines: A's from its other far corner through this one, B's likewise; they meet at the mitre
                Vector2 p = Flat(A.Kit.At(E.otherFar)), r = Flat(af) - p, q = Flat(B.Kit.At(F.otherFar)), s = Flat(bf) - q;
                float rxs = r.x * s.y - r.y * s.x; bool mitred = false;
                if (Mathf.Abs(rxs) > 1e-6f * r.magnitude * s.magnitude)
                {
                    var qp = q - p; float t = (qp.x * s.y - qp.y * s.x) / rxs; var m2 = p + r * t;
                    if ((m2 - Flat(af)).magnitude <= 0.6f && (m2 - Flat(bf)).magnitude <= 0.6f)
                    {
                        var m = new Vector3(m2.x, (af.y + bf.y) * 0.5f, m2.y); moves.Add((A, af, m)); moves.Add((B, bf, m)); var bn = B.Kit.At(F.near); var mid = (an + bn) * 0.5f; if ((an - bn).sqrMagnitude > 1e-8f) { moves.Add((A, an, mid)); moves.Add((B, bn, mid)); } mitred = true; Mitred++; /* the ribbon-side corners to one point too: a shared edge with identical ends rasterises watertight (no hairline crack or doubled line in the additive pool) */ // applied after every joint is measured on the bands as placed
                    }
                }
                if (!mitred && Vector3.Distance(af, bf) <= 0.6f)
                    fills.Add((A, an, A.Kit.UvAt(E.near), af, A.Kit.UvAt(E.far), bf, B.Kit.UvAt(F.far), B.Kit.At(F.near), B.Kit.UvAt(F.near)));
                used.Add(e); used.Add(best);
            }
            foreach (var (mb, from, to) in moves) Move(mb, from, to);
            foreach (var b in bands) Combine.Baked(b.El, b.Layer, b.Kit, b.Mat);
            foreach (var f in fills)
            {
                var k = new MeshKit();
                int p0 = k.Vert(f.a0, f.u0), p1 = k.Vert(f.a1, f.u1), p2 = k.Vert(f.b1, f.v1), p3 = k.Vert(f.b0, f.v0);
                var nrm = Vector3.Cross(k.At(p1) - k.At(p0), k.At(p2) - k.At(p0));
                if (nrm.y >= 0f) { k.Tri(p0, p1, p2); k.Tri(p0, p2, p3); } else { k.Tri(p0, p2, p1); k.Tri(p0, p3, p2); }
                Combine.Baked(f.b.El, f.b.Layer, k, f.b.Mat); Filled++;
            }
            var n = bands.Count; bands.Clear();
            Debug.Log($"World: strip light pool bands {n}, joints mitred {Mitred}, filled {Filled}");
            return Mitred + Filled;
        }
    }
}
