using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>Roof cut-away for the play camera (a study option, not the default view). Every building taller than
    /// Rig.CutHeight is moved to Rig.TallLayer and gets a twin under the "cutaway" layer root on Rig.CutLayer: walls of
    /// Style.CutWall thickness from its base to base + Rig.CutHeight, no roof, and a floor at its base. Cameras hide one
    /// set or the other (Rig.CutAway), so the overview and the default play view are unchanged.</summary>
    public sealed class CutAway
    {
        readonly List<(string name, Vector2[] poly, float lo, float hi, string material, GameObject full)> tall = new();

        /// <summary>Records a building drawn as Prism(poly, lo, hi); ignored when it is no taller than the cut.</summary>
        public void Add(string name, Vector2[] poly, float lo, float hi, string material, GameObject full)
        {
            if (full && hi - lo > Rig.CutHeight) tall.Add((name, poly, lo, hi, material, full));
        }

        public void Build(Transform root)
        {
            foreach (var (name, raw, lo, _, material, full) in tall)
            {
                full.layer = Rig.TallLayer;
                var outer = Poly.Clean(raw);
                var inner = Poly.Inset(outer, Style.CutWall);
                if (inner == null) { Debug.LogWarning($"Graybox: cutaway {name} too small for its walls"); continue; }
                var walls = Nodes.Part(root, name, new MeshKit().Ring(outer, inner, lo, lo + Rig.CutHeight), material);
                var floor = Nodes.Part(walls.transform, "floor", new MeshKit().Flat(inner, lo + Style.CutFloorLift), "cut/floor");
                walls.layer = floor.layer = Rig.CutLayer;
            }
        }
    }
}
