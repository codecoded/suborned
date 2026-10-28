using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Brief 075 (the 1950s tram street, Splice's tools/world/gen/street/tramstreet.ts): what the export adds outside its counted layers.
    ///   kerbside: the kerbside bays as paint on the carriageway (look.json `kerbside/paint`, 0.10 m lines in the kerb's pearl): parallel and loading bays outlined on every side but
    ///             the kerb's, angle stalls by their two divider lines; and the primaries' palm islands, a pavement top inside a kerb ring in their road's kerb
    ///             surface (the palms and lamps on them are street items). One marker per bay and island under `kerbside`; no shadows (flat ground).
    ///   wires:    Vela's overhead pieces (TramWire_Contact / _Span stretched along X by their kit scale, _Hanger, _Rosette) baked into `wires` batches by
    ///             material and tile (about 7 km of strand in a handful of renderers); the poles are street items (TramWire_Pole). No shadows: a 12 mm strand
    ///             at 120 m shadow distance is below a shadow texel and only flickers.
    ///   dressing: the parked trams (dressing.trams), the kit Tram as an instance at its pivot; presentation only, never sim data.
    /// The track strips (transit.tram) step aside where the export lays rail tiles along them (TrackCovered).</summary>
    public static class TramStreet
    {
        public const string KerbsideLayer = "kerbside", WiresLayer = "wires", DressingLayer = "dressing";
        public const string PaintSurface = "kerbside/paint";
        const float Paint = 0.10f, PaintLift = 0.0025f, IslandKerb = 0.15f;

        public static void Kerbside(Nodes nodes, Transform root, GroundDto g)
        {
            if (g.Kerbside.Count == 0 && g.Islands.Count == 0) return;
            var layer = Nodes.Group(root, KerbsideLayer);
            var roads = new Dictionary<string, RoadDto>(System.StringComparer.Ordinal); foreach (var r in g.Roads) roads[r.Id] = r;
            RoadDto Road(string id) => roads.TryGetValue("road/" + id, out var r) ? r : roads.TryGetValue(id, out r) ? r : throw new InvalidDataException($"World: kerbside: road `{id}` is not in ground.roads");
            bool paint = nodes.Has(PaintSurface); int lines = 0;
            foreach (var b in g.Kerbside)
            {
                if (b.Ring.Length != 4) throw new InvalidDataException($"World: kerb bay {b.Id}: ring of {b.Ring.Length} points (4)");
                var road = Road(b.Road); var line = Poly.CleanLine(road.Line);
                // the edge farthest from the road line is the kerb's (no line there); an angle stall also leaves its traffic edge (the nearest) open
                int far = 0, near = 0; float dFar = float.MinValue, dNear = float.MaxValue;
                for (int i = 0; i < 4; i++) { Poly.NearestTangent(line, (b.Ring[i] + b.Ring[(i + 1) % 4]) * 0.5f, out float d); if (d > dFar) { dFar = d; far = i; } if (d < dNear) { dNear = d; near = i; } }
                var kit = new MeshKit(); float y = b.Level + PaintLift;
                for (int i = 0; i < 4; i++)
                {
                    if (i == far || (b.Kind == "angle" && i == near)) continue;
                    Line(kit, b.Ring[i], b.Ring[(i + 1) % 4], y); lines++;
                }
                var el = Nodes.Element(layer, b.Id);
                if (paint) nodes.Part(el, "paint", kit, PaintSurface);
                else throw new InvalidDataException($"World: kerbside bays need look.json `{PaintSurface}`");
            }
            foreach (var m in g.Islands)
            {   // a pavement top inside a kerb ring (the road's kerb surface): the ring's walls and its 0.15 m top band; the hole's walls lie under the pavement top
                var outer = Poly.Clean(m.Ring);
                var inner = Poly.Inset(outer, IslandKerb) ?? throw new InvalidDataException($"World: island {m.Id}: too small for its {IslandKerb} m kerb");
                var road = RoadOf(m.Id, roads);
                var el = Nodes.Element(layer, m.Id); float top = m.Base + m.Height;
                nodes.Part(el, "kerb", new MeshKit().Prism(outer, inner, m.Base, top), nodes.Ground(road?.KerbSurface ?? m.Surface));
                nodes.Part(el, "top", new MeshKit().Flat(inner, top), nodes.Ground(m.Surface));
            }
            Debug.Log($"World: kerbside {g.Kerbside.Count} bays ({g.Kerbside.FindAll(b => b.Kind == "parallel").Count} parallel, {g.Kerbside.FindAll(b => b.Kind == "angle").Count} angle, {g.Kerbside.FindAll(b => b.Kind == "loading").Count} loading; {lines} paint lines), {g.Islands.Count} kerbed palm islands (brief 075)");
        }

        /// <summary>ground/island/&lt;road&gt;/&lt;n&gt; → the road, else null (the island then keeps its own surface for the kerb).</summary>
        static RoadDto RoadOf(string id, Dictionary<string, RoadDto> roads)
        {
            var p = id.Split('/'); return p.Length >= 3 && roads.TryGetValue("road/" + p[2], out var r) ? r : null;
        }

        static void Line(MeshKit kit, Vector2 a, Vector2 b, float y)
        {
            var d = (b - a); if (d.sqrMagnitude < 1e-6f) return; d.Normalize(); var n = new Vector2(-d.y, d.x) * (Paint * 0.5f);
            Vector3 W(Vector2 p) => new(p.x, y, p.y);
            int i0 = kit.Vert(W(a - n)), i1 = kit.Vert(W(b - n)), i2 = kit.Vert(W(b + n)), i3 = kit.Vert(W(a + n));
            if (Vector3.Cross(W(b - n) - W(a - n), W(b + n) - W(a - n)).y >= 0f) { kit.Tri(i0, i1, i2); kit.Tri(i0, i2, i3); }
            else { kit.Tri(i0, i2, i1); kit.Tri(i0, i3, i2); }
        }

        public static void Wires(Transform root, List<WirePieceDto> pieces, KitPlacer placer)
        {
            if (pieces.Count == 0) return;
            var layer = Nodes.Group(root, WiresLayer); var none = new Dictionary<string, string>();
            var by = new SortedDictionary<string, (int n, float m)>(System.StringComparer.Ordinal);
            foreach (var p in pieces)
            {
                if (p.Kit.Scale.y <= 0f || p.Kit.Scale.z <= 0f || p.Kit.Scale.x <= 0f) throw new InvalidDataException($"World: wire piece {p.Id}: scale {p.Kit.Scale}");
                if ((p.Kind is "hanger" or "rosette") && Mathf.Abs(p.Kit.Scale.x - 1f) > 1e-3f) throw new InvalidDataException($"World: wire piece {p.Id}: a {p.Kind} is never stretched (scale {p.Kit.Scale})");
                placer.Bake(Nodes.Element(layer, p.Id), WiresLayer, p.Id, p.Kit, p.Kit.At, p.Z, none);
                var had = by.TryGetValue(p.Kind, out var h) ? h : (n: 0, m: 0f); by[p.Kind] = (had.n + 1, had.m + (p.Kind is "contact" or "span" ? p.Kit.Scale.x : 0f));
            }
            var parts = new List<string>(); foreach (var kv in by) parts.Add(kv.Value.m > 0f ? $"{kv.Value.n} {kv.Key} ({kv.Value.m:F0} m)" : $"{kv.Value.n} {kv.Key}");
            Debug.Log($"World: tram overhead {pieces.Count} pieces baked into `{WiresLayer}` batches: {string.Join(", ", parts)} (brief 075; poles are street items)");
        }

        public static void Dressing(Transform root, List<ParkedTramDto> trams, KitPlacer placer)
        {
            if (trams.Count == 0) return;
            var layer = Nodes.Group(root, DressingLayer);
            foreach (var t in trams) placer.Place(Nodes.Element(layer, t.Id), DressingLayer, t.Kit, t.Kit.At, t.Z, 0f);
            Debug.Log($"World: dressing {trams.Count} parked trams ({string.Join(", ", trams.ConvertAll(t => t.Id))}; presentation only, brief 075)");
        }

        /// <summary>The track ids whose strip the export's rail tiles cover (ground.pieces kind tram-rail, `edge` = the track): the strip (level 0.04) stood
        /// 2 cm proud of the flush rails (top 0.02). A track is covered when its tiles run at least 90 % of its rails' length (both rails of a double track).</summary>
        public static HashSet<string> RailCovered(TransitDto t, List<StreetItemDto> pieces)
        {
            var laid = new Dictionary<string, float>(System.StringComparer.Ordinal);
            foreach (var p in pieces) if (p.Kind == "tram-rail" && p.Edge != null) laid[p.Edge] = (laid.TryGetValue(p.Edge, out var l) ? l : 0f) + p.Size.x;
            var covered = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var x in t.Tram)
            {
                if (!laid.TryGetValue(x.Id, out var have)) continue;
                var line = Poly.CleanLine(x.Line); float len = 0f; for (int i = 1; i < line.Length; i++) len += Vector2.Distance(line[i - 1], line[i]);
                float want = len * (x.Spacing > 0f ? 2f : 1f);
                if (have >= 0.9f * want) covered.Add(x.Id);
                else Debug.LogWarning($"World: track {x.Id}: rail tiles run {have:F0} m of {want:F0} m; its strip stays drawn");
            }
            return covered;
        }
    }
}
