using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Facades for megastructure parts and gate towers through FacadeKit: walls keep their look.json surface colour
    /// (the structure's identity), bays and storeys come from each part's own size and height, style from its surface
    /// (shed, stair core, terrace, civic stone, gallery bands) or its shape (slender towers: slot windows). A structure's
    /// parts share one host (colours, frontages); collinear walls of stacked parts are drawn once. Prims keep their look
    /// materials. A gateway arch stands on its quay; a megastructure with a generated stand-in (StandIns) is that piece.</summary>
    public sealed class MegaKit
    {
        readonly Nodes nodes; readonly FacadeKit fk; readonly WorldDto world;

        public MegaKit(Nodes nodes, FacadeKit fk, WorldDto world) { this.nodes = nodes; this.fk = fk; this.world = world; }

        /// <summary>Swatches the walls need, registered before the atlas is baked.</summary>
        public static IEnumerable<string> Surfaces(WorldDto w) =>
            w.Megastructures.SelectMany(m => m.Parts).Concat(w.Gates).Select(p => p.Surface).Distinct();

        public static string Swatch(string surface) => "look:" + surface;

        public void Megastructures(Transform layer, List<MegaDto> megas, KitPlacer placer, StandIns standIns)
        {
            foreach (var m in megas)
            {
                var el = Nodes.Element(layer, m.Id);
                var h = HostOf(m.Id);
                if (m.Gateway != null) placer.Place(el, layer.name, m.Gateway, m.Gateway.At, QuayTop(m.Gateway.At), 0f);
                if (standIns.Covers(m)) { standIns.Mega(layer, el, m); continue; }
                var peers = m.Parts.Select(p => new FacadeKit.Peer(p.Id, Poly.Clean(p.Ring), p.Base, p.Base + p.Height)).ToList();
                for (int i = 0; i < m.Parts.Count; i++)
                {
                    var p = m.Parts[i];
                    fk.Facade(el, p, peers[i].Ring, h, StyleOf(p, peers[i].Ring), Swatch(p.Surface), peers);
                }
                foreach (var p in Coplanar.Boxes(m.Prims)) nodes.Part(el, p.Id, PrimKit.Add(new MeshKit(), p), p.Surface);
            }
        }

        /// <summary>Top of the quay under p (the gateway stands on it); the datum where none.</summary>
        float QuayTop(Vector2 p)
        {
            foreach (var q in world.Ground.Quays) if (Poly.Contains(q.Ring, p)) return q.Base + q.Height;
            return 0f;
        }

        public void Gates(Transform layer, List<MassDto> gates)
        {
            foreach (var g in gates)
            {
                var ring = Poly.Clean(g.Ring);
                fk.Facade(Nodes.Element(layer, g.Id), g, ring, fk.Resolve(g.Id), StyleOf(g, ring), Swatch(g.Surface), null);
            }
        }

        /// <summary>The structure's host: area = the most common area of the frontages on any of its parts; those frontages
        /// mark its shop bays.</summary>
        Host HostOf(string id)
        {
            var fronts = world.Frontages.Where(f => f.Host == id || f.Host.StartsWith(id + "/")).ToList();
            var area = fronts.GroupBy(f => f.Area).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, System.StringComparer.Ordinal).FirstOrDefault()?.Key;
            var h = fk.Get(id, area);
            foreach (var f in fronts) if (!h.Fronts.Contains(f)) h.Fronts.Add(f);
            return fk.Resolve(id);
        }

        /// <summary>Slender (footprint under 8 m, at least 4x as tall): pylon; else by surface.</summary>
        static string StyleOf(MassDto m, Vector2[] ring)
        {
            Vector2 lo = ring[0], hi = ring[0];
            foreach (var q in ring) { lo = Vector2.Min(lo, q); hi = Vector2.Max(hi, q); }
            float span = Mathf.Max(hi.x - lo.x, hi.y - lo.y);
            if (span < 8f && m.Height >= 4f * span) return "pylon";
            return m.Surface switch
            {
                "industrial" or "port" => "shed",
                "stair" => "core",
                "residential" => "punched",
                "limestone" or "civic" => "grand",
                "tower" => "band", // no curtain wall (grammar §3)
                _ => "band",
            };
        }
    }
}
