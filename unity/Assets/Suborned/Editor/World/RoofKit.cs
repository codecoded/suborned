using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Roof items in accent colours (trim and awnings from the building's enamel pair, chrome legs and masts, fresh
    /// greens, pool water), painted from the facade atlas. Boxes of one roof that share a face plane are separated
    /// first (Coplanar.Boxes). A roof's kit piece (hover pads) stands on the slab; a tower's roof goes with its generated piece.</summary>
    public sealed class RoofKit
    {
        static readonly HashSet<string> Shiny = new() { "chrome", "pool" };
        readonly Nodes nodes; readonly FacadeKit fk; readonly FacadeSpec spec; readonly QuadKit sf;

        public RoofKit(Nodes nodes, FacadeKit fk) { this.nodes = nodes; this.fk = fk; spec = fk.Spec; sf = fk.Quads; }

        public void Roofs(Transform layer, List<RoofDto> roofs, KitPlacer placer, StandIns standIns)
        {
            foreach (var r in roofs)
            {
                if (standIns.Covers(r)) { standIns.Roof(layer, r); continue; }
                var el = Nodes.Element(layer, r.Id);
                if (r.Kit != null) placer.Place(el, layer.name, r.Kit, r.Kit.At, r.Z, r.Yaw);
                var h = fk.Resolve(r.Building); var d = new Dice(r.Id, 5);
                string accent = d.Int(2) == 0 ? h.A : h.B;
                foreach (var p in Coplanar.Boxes(r.Parts))
                {
                    var sw = spec.RoofItems.TryGetValue(p.Surface, out var s) ? s : "paper";
                    sw = sw switch { "@A" => accent, "@B" => accent == h.A ? h.B : h.A, "@leaf" => spec.Greens[d.Int(spec.Greens.Length)], _ => sw };
                    var kit = PrimKit.Add(new MeshKit(), p);
                    sf.Paint(kit, 0, sw);
                    nodes.Part(el, p.Id, kit, Shiny.Contains(sw) ? spec.Atlas.Glossy : spec.Atlas.Matte);
                }
            }
        }
    }
}
