using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>The export's crossings: one marker per crossing under `crossings` (named by id), drawn on the ground it crosses.
    /// A zebra is bars across the way; any other crossing the pair of lines along its two edges (where traffic stops); a refuge
    /// is its median ring raised as an island. Paint is solid geometry a little proud of what it lies on, never a decal, so
    /// nothing is coplanar. Paint and islands take their road's kerb surface and go into the ground layer's kerb batch that
    /// already reaches them (Combine.Fit), so a crossing on a road costs no renderer and no material of its own and brings
    /// no batch into a view it was not in; one off the roads (a station bank's) takes its own tile's batch. Sizes: kit.json
    /// `crossings`. No colour here (§15: a surface never carries a signal colour; the kerb surface is a palette colour).</summary>
    public sealed class CrossingKit
    {
        const string Ground = "ground"; // the batch layer paint lies in, with the kerbs

        readonly Nodes nodes; readonly Catalogue.CrossingSpec spec;
        public CrossingKit(Nodes nodes, Catalogue kit) { this.nodes = nodes; spec = kit.Crossings; }

        /// <summary>After the ground layer: its kerb batches are what paint goes into.</summary>
        public void Build(Transform layer, WorldDto w)
        {
            var roads = new Dictionary<string, RoadDto>(); foreach (var r in w.Ground.Roads) roads[r.Id] = r;
            var patches = new Dictionary<string, StreetItemDto>(); foreach (var s in w.Street) patches[s.Id] = s;
            int zebras = 0, bars = 0, pairs = 0, refuges = 0, inside = 0, grew = 0, alone = 0; float most = 0f;
            foreach (var c in w.Crossings)
            {
                if (c.Line.Length != 2 || (c.Line[1] - c.Line[0]).sqrMagnitude < 1e-6f) throw new InvalidDataException($"World: {c.Id}: `line` is not two points apart");
                roads.TryGetValue("road/" + c.Road, out var road);
                float on = On(c, road, patches);
                Vector2 a = c.Line[0]; float len = (c.Line[1] - a).magnitude; var u = (c.Line[1] - a) / len;

                var paint = new MeshKit();
                foreach (var (s0, s1) in Free(c, a, u, len))
                    switch (c.Control)
                    {
                        case "zebra": bars += Bars(paint, a, u, s0, s1, c.Width, on); break;
                        case null: case "signal": Edges(paint, a, u, s0, s1, c.Width, on); break;
                        default: throw new InvalidDataException($"World: {c.Id}: control `{c.Control}` is not drawn (zebra | signal)");
                    }
                if (c.Control == "zebra") zebras++; else pairs++;

                var islands = new MeshKit();
                foreach (var ring in c.Refuges)
                {
                    var median = w.Ground.Medians.Find(m => Poly.Contains(m.Ring, Centre(ring))) ?? throw new InvalidDataException($"World: {c.Id}: a refuge stands on no median");
                    var grown = Poly.Inset(Poly.Clean(ring), -spec.Lip) ?? throw new InvalidDataException($"World: {c.Id}: a refuge ring is degenerate");
                    islands.Prism(grown, median.Base, median.Base + median.Height + spec.Lip);
                    refuges++;
                }

                var el = Nodes.Element(layer, c.Id);
                string surface = road?.KerbSurface ?? spec.Surface;
                foreach (var (name, part) in new[] { ("paint", paint), ("refuges", islands) })
                    switch (nodes.PartIn(Ground, el, name, part, surface, spec.Reach))
                    {
                        case null: break; // nothing to draw (a crossing without refuges)
                        case < 0f: alone++; break;
                        case 0f: inside++; break;
                        case float g: grew++; most = Mathf.Max(most, g); break;
                    }
            }
            Debug.Log($"World: crossings {w.Crossings.Count}: {zebras} zebras ({bars} bars), {pairs} line pairs, {refuges} refuges; parts: {inside} inside a kerb batch, " +
                $"{grew} grew one (by at most {most:F2} m of {spec.Reach} m), {alone} off the roads in a ground tile of their own");
        }

        /// <summary>The height paint lies on: the carriageway for a kerb crossing, the top of its patch for a stop or bank one.</summary>
        static float On(CrossingDto c, RoadDto road, Dictionary<string, StreetItemDto> patches)
        {
            if (c.Kind == "kerb") return road?.Level ?? throw new InvalidDataException($"World: {c.Id}: road `{c.Road}` is not in the export");
            if (c.Items.Length != 1 || !patches.TryGetValue(c.Items[0], out var patch)) throw new InvalidDataException($"World: {c.Id}: a {c.Kind} crossing is one street item, in the export");
            return patch.Z + patch.Size.z;
        }

        /// <summary>The spans of the way (metres from its start) that are off its refuges.</summary>
        static List<(float from, float to)> Free(CrossingDto c, Vector2 a, Vector2 u, float len)
        {
            var taken = new List<(float from, float to)>();
            foreach (var ring in c.Refuges)
            {
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (var p in ring) { float s = Vector2.Dot(p - a, u); lo = Mathf.Min(lo, s); hi = Mathf.Max(hi, s); }
                taken.Add((lo, hi));
            }
            taken.Sort((x, y) => x.from.CompareTo(y.from));
            var free = new List<(float, float)>(); float at = 0f;
            foreach (var (lo, hi) in taken)
            {
                if (lo > at) free.Add((at, Mathf.Min(lo, len)));
                at = Mathf.Max(at, hi);
            }
            if (at < len) free.Add((at, len));
            return free;
        }

        /// <summary>As many bars as fit the span with a gap between, centred on it. Returns how many.</summary>
        int Bars(MeshKit k, Vector2 a, Vector2 u, float s0, float s1, float width, float on)
        {
            float span = s1 - s0, step = spec.Bar + spec.Gap;
            int n = Mathf.FloorToInt((span + spec.Gap) / step + 1e-4f);
            float at = s0 + (span - (n * spec.Bar + (n - 1) * spec.Gap)) * 0.5f;
            for (int i = 0; i < n; i++, at += step) k.Prism(Poly.Rect(a + u * (at + spec.Bar * 0.5f), u, spec.Bar, width), on, on + spec.Paint);
            return Mathf.Max(n, 0);
        }

        /// <summary>The two lines along the span's edges, inside the crossing's width.</summary>
        void Edges(MeshKit k, Vector2 a, Vector2 u, float s0, float s1, float width, float on)
        {
            var mid = a + u * ((s0 + s1) * 0.5f); var v = new Vector2(-u.y, u.x) * ((width - spec.Line) * 0.5f);
            k.Prism(Poly.Rect(mid + v, u, s1 - s0, spec.Line), on, on + spec.Paint);
            k.Prism(Poly.Rect(mid - v, u, s1 - s0, spec.Line), on, on + spec.Paint);
        }

        static Vector2 Centre(Vector2[] ring)
        {
            var c = Vector2.zero;
            foreach (var p in ring) c += p;
            return c / ring.Length;
        }
    }
}
