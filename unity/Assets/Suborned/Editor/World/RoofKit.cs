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

        /// <summary>roofs[].kits[] pieces placed this build (brief 075 remap).</summary>
        public static int Pieces, Floored;
        /// <summary>The roof pieces' nearest L1 switch: the crown class's (kit.json lodClasses.crown, 45 m), past the play-zoom guard's 36 m.</summary>
        const float RoofL0 = 45f;

        public void Roofs(Transform layer, List<RoofDto> roofs, KitPlacer placer, StandIns standIns)
        {
            Pieces = Floored = 0;
            foreach (var r in roofs)
            {
                if (standIns.Covers(r)) { standIns.Roof(layer, r); continue; }
                var el = Nodes.Element(layer, r.Id);
                if (r.Kit != null) KitLiveries.Apply(placer.Place(el, layer.name, r.Kit, r.Kit.At, r.Z, r.Yaw), r.Id, "kit", r.Kit.Id);
                if (r.Crown != null) KitLiveries.Apply(placer.Place(el, layer.name, r.Crown, r.Crown.At, r.Z, r.Yaw), r.Id, "crown", r.Crown.Id); // grammar §6: every tower's crown
                // brief 075 remap (Splice's roofline.ts RL-3): the old roof themes' pieces (lido, tennis, garden, cafe, glass, machine, sun), each at its own
                // At, pivot height Z and yaw; prefab instances like every placed piece, so URP's GPU Resident Drawer draws the repeats (CafeSet_Tulip,
                // Deckchair) instanced
                for (int i = 0; i < r.Kits.Count; i++)
                {
                    var k = r.Kits[i];
                    if (float.IsNaN(k.Z)) throw new System.IO.InvalidDataException($"World: {r.Id}: kits[{i}] ({k.Id}) has no z");
                    var t = placer.Place(el, layer.name, k, k.At, k.Z, r.Yaw);
                    Floored += KitLods.Floor(t, RoofL0); // on a 3-storey roof the play camera sees it close: L0 to the crown class's 45 m (the play-zoom guard)
                    KitLiveries.Apply(t, r.Id, $"kits[{i}]", k.Id);
                    Pieces++;
                }
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
