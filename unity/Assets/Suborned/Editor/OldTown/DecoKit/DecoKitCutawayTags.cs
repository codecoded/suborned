#if UNITY_EDITOR
using System.Linq;
using Suborned.OldTown.Sky;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Marks what the 2.5D cutaway (DK_Cutaway.hlsl) may cut: architecture, i.e. anything standing over 10 m on a real
    /// footprint (bays, towers, landmarks) and anything up on a roof (decks, lawns, plant, pads, billboards, which would
    /// float once the building under them is cut). Street furniture, trees, monuments and people stay; so does
    /// whatever moves on a sky path (the rail, its trains, the aircar lanes, the rockets).
    internal static class DecoKitCutawayTags
    {
        internal const uint Layer = 1u << 20;  // renderingLayerMask bit; DK_CUT_LAYER in DK_Cutaway.hlsl
        const float Tall = 10f, Footprint = 40f, Raised = 8f;

        internal static void Apply()
        {
            int tagged = 0, all = 0;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!r.sharedMaterials.Any(m => m && m.shader && m.shader.name.EndsWith("Cutaway"))) continue;
                    all++;
                    if (r.GetComponentInParent<SkyPath>(true) || r.GetComponentInParent<SkyTraffic>(true) ||
                        r.GetComponentInParent<RocketFlight>(true)) continue;
                    var b = r.bounds;
                    if (b.min.y < Raised && (b.size.y < Tall || b.size.x * b.size.z < Footprint)) continue;
                    r.renderingLayerMask |= Layer;
                    tagged++;
                }
            Debug.Log($"[DecoKit] cutaway: {tagged} of {all} renderers may be cut");
        }
    }
}
#endif
