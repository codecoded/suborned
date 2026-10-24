using System.Collections.Generic;
using System.Linq;
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
        /// <summary>Brief 028 road finish (Vela; Aster 2026-10-22): the export's ground pieces (the road finish's ribbon, shoulder, pinstripe,
        /// lozenge and seam, the capstone's flags, kerbs, pits, drains, tiles) baked into the layer's tile batches by material (KitPlacer.Bake),
        /// not ~17k prefab instances: flat pieces that never fade draw as a few batches a tile. Each at the export's z as is: the flat pieces are
        /// modelled at z 0 (Vela's re-export, 2026-10-23, of Pave_Slab, Pave_Flags_0..3, InlayArc_R*, PaveInlay_*), so none is laid down here.</summary>
        /// <param name="batchOf">brief 055 road test: the batch layer a piece bakes into (null = the layer's own): the pieces one road finish
        /// lays and the other does not bake apart (RoadFinish turns them).</param>
        public int GroundPieces(Transform layer, List<StreetItemDto> items, System.Func<StreetItemDto, string> batchOf = null)
        {
            var none = new Dictionary<string, string>(); RoadPoolJoints.Clear();
            try
            {
                foreach (var s in items)
                {
                    if (s.Kit == null) throw new System.IO.InvalidDataException($"World: ground piece {s.Id} ({s.Kind}) has no kit ref");
                    placer.Bake(Nodes.Element(layer, s.Id), batchOf?.Invoke(s) ?? layer.name, s.Id, s.Kit, s.At, s.Z, none, s.Yaw);
                }
            }
            catch { RoadPoolJoints.Abort(); throw; } // Redline 064 #7: a throw never leaves the pool bands held for a later bake
            Debug.Log($"World: strip light pool joints closed {RoadPoolJoints.Fill()} (Glyph's 0.2.104 review #4: no wedge or doubled overlap between the chords of a curve)");
            return items.Count;
        }

        /// <summary>Brief 048 (Splice, the tp-long renderer budget): a crossing's flush parts (dots, bars, edge lines, studs, inlay arcs, the
        /// lit strip's pool) are baked into the non-fade `crossing-parts` batches by material and tile, as the ground pieces are, not one prefab
        /// instance each (~600 renderers in the worst third-person view); the signal orbs stay instances (they turn to the camera, show a state
        /// and cast). Their light is drawn as emission only (CrossingKit.LightMaterial); no shadows (Combine.Flush).</summary>
        public const string CrossingParts = "crossing-parts";
        public static bool BakedCrossingPart(StreetItemDto s) => s.Crossing != null && s.Kind is "crossing-dots" or "crossing-bar" or "crossing-edge" or "crossing-stud" or "crossing-arc" or "crossing-field";
        public int CrossingsBaked { get; private set; }

        public void Street(Transform layer, List<StreetItemDto> items)
        {
            var none = new Dictionary<string, string>(); Material kitLight = null, glowLight = null;
            foreach (var s in items)
            {
                var el = Nodes.Element(layer, s.Id);
                if (s.Kit != null && BakedCrossingPart(s))
                {
                    if (glowLight == null) { kitLight = KitImport.Library(CrossingKit.Light); glowLight = CrossingKit.LightMaterial(); }
                    placer.Bake(el, CrossingParts, s.Id, s.Kit, s.At, s.Z, none, s.Yaw, m => kitLight != null && m == kitLight ? glowLight : m); CrossingsBaked++;
                    continue;
                }
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
