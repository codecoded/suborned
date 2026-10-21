using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Connectors and street items (roofs and frontages: FacadeKit, RoofKit, ShopKit). One marker per export element (named by
    /// id); every part is a Prim (export or kit.json) batched per layer, material and tile.</summary>
    public sealed class CityLayers
    {
        readonly Nodes nodes; readonly Catalogue kit; readonly KitPlacer placer;
        public CityLayers(Nodes nodes, Catalogue kit, KitPlacer placer) { this.nodes = nodes; this.kit = kit; this.placer = placer; }

        /// <summary>Vertical connectors (foot = head: lifts, cores, stair towers) are shafts on their ring from bottom to top;
        /// inclined ones (escalators, ramps, flights) a sloped slab foot→head, width wide.</summary>
        public void Connectors(Transform layer, List<ConnectorDto> connectors)
        {
            foreach (var c in connectors)
            {
                if (c.Dress.Count > 0)
                {   // the dressed form (glazed stadium stair towers, hooped lift tubes) instead of the ring prism; one part per surface
                    var el = Nodes.Element(layer, c.Id);
                    var bySurface = new SortedDictionary<string, MeshKit>(System.StringComparer.Ordinal);
                    foreach (var p in c.Dress)
                    {
                        if (!bySurface.TryGetValue(p.Surface, out var mkS)) bySurface[p.Surface] = mkS = new MeshKit();
                        PrimKit.Add(mkS, p);
                    }
                    foreach (var kv in bySurface) nodes.Part(el, kv.Key, kv.Value, kv.Key);
                    continue;
                }
                var surface = kit.ConnectorSurface(c.Kind);
                var mk = new MeshKit();
                if ((c.Head - c.Foot).sqrMagnitude < 1e-4f) mk.Prism(c.Ring, c.Bottom, c.Top);
                else Slope(mk, c.Foot, c.Head, c.Width, c.Bottom, c.Top, kit.Slab);
                nodes.Solid(layer, c.Id, mk, surface);
            }
        }

        /// <summary>Each item with a DecoKit ref is that piece; the rest (source "procedural") are built from their kit.json parts,
        /// scaled to the item's size and turned by its yaw.</summary>
        /// <summary>Brief 026 (Splice): the export's flat ground pieces (ground.pieces: tactile and terrazzo tiles, drains, manholes; kerb stones,
        /// flags and tree pits when adopted), each its kit piece at its pivot, under their own root (`ground-pieces`: the export's counts.ground
        /// leaves them out, so the ground layer's count stays the export's). A piece without a kit ref = reject.</summary>
        public int GroundPieces(Transform layer, List<StreetItemDto> items)
        {
            foreach (var s in items)
            {
                if (s.Kit == null) throw new System.IO.InvalidDataException($"World: ground piece {s.Id} ({s.Kind}) has no kit ref");
                placer.Place(Nodes.Element(layer, s.Id), layer.name, s.Kit, s.At, s.Z, s.Yaw);
            }
            return items.Count;
        }

        public void Street(Transform layer, List<StreetItemDto> items)
        {
            foreach (var s in items)
            {
                var el = Nodes.Element(layer, s.Id);
                if (s.Kit != null) { placer.Place(el, layer.name, s.Kit, s.At, s.Z, s.Yaw); continue; }
                var u = new Vector2(Mathf.Cos(s.Yaw), Mathf.Sin(s.Yaw)); var v = new Vector2(-u.y, u.x);
                foreach (var p in kit.Street(s.Kind, s.Variant))
                {
                    var scale = p.ByH ? new Vector3(s.Size.z, s.Size.z, s.Size.z) : s.Size;
                    var c = s.At + u * (p.C.x * scale.x) + v * (p.C.y * scale.y);
                    var size = Vector3.Scale(p.Size, scale);
                    nodes.Part(el, p.Shape, PrimKit.Add(new MeshKit(), p.Shape, c, s.Z + p.Base * s.Size.z, size, s.Yaw + p.Yaw), p.Surface ?? s.Surface);
                }
            }
        }

        /// <summary>Closed sloped slab: top surface from (foot, bottom) to (head, top), `thick` deep below it.</summary>
        static void Slope(MeshKit k, Vector2 foot, Vector2 head, float width, float bottom, float top, float thick)
        {
            var d = (head - foot).normalized; var n = new Vector2(-d.y, d.x) * (width * 0.5f);
            Vector3 P(Vector2 q, float h) => new(q.x, h, q.y);
            var top4 = new[] { P(foot - n, bottom), P(head - n, top), P(head + n, top), P(foot + n, bottom) };
            var bot4 = new Vector3[4]; for (int i = 0; i < 4; i++) bot4[i] = top4[i] - Vector3.up * thick;
            var mid = (P(foot, bottom) + P(head, top)) * 0.5f - Vector3.up * (thick * 0.5f);
            Quad(k, mid, top4[0], top4[1], top4[2], top4[3]);
            Quad(k, mid, bot4[0], bot4[1], bot4[2], bot4[3]);
            for (int i = 0; i < 4; i++) { int j = (i + 1) % 4; Quad(k, mid, top4[i], top4[j], bot4[j], bot4[i]); }
        }

        static void Quad(MeshKit k, Vector3 inside, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            int a = k.Vert(p0), b = k.Vert(p1), c = k.Vert(p2), d = k.Vert(p3);
            var nrm = Vector3.Cross(p1 - p0, p2 - p0);
            bool fwd = Vector3.Dot(nrm, (p0 + p1 + p2 + p3) * 0.25f - inside) >= 0;
            if (fwd) { k.Tri(a, b, c); k.Tri(a, c, d); } else { k.Tri(a, c, b); k.Tri(a, d, c); }
        }
    }
}
