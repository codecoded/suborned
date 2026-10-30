using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 056 F: which drawn people take a silhouette (Suborned.SilhouettePass). Every person's figure carries the person bit (the
    /// stencil mark, so nobody silhouettes over another person); the people who matter carry the silhouette bit too: the cell, hostiles (the
    /// fight's, the response's units, the security guns, the patrol, the rivals) and the key people (Inez, the guard, the driver, Vale), as
    /// the lab drew them (Sky 2026-10-25: silhouettes stay as now). Set once, when a figure or a robot model is put in its view; view only.</summary>
    public sealed partial class HostViews
    {
        /// <summary>The views whose people take a silhouette.</summary>
        public static bool SilhouetteKey(string key) => key.StartsWith("op/") || key.StartsWith("hostile/") || key.StartsWith("unit/") || IsCrewId(key) || key.StartsWith("security/")
            || key is "patrol" or "waitress" or "guard" or "driver" or "councillor";

        /// <summary>Glint 056 (2026-10-26) test: every shown, active person view that takes a silhouette, with its position (HostTurn.UncutSilhouettePeople).</summary>
        public System.Collections.Generic.IEnumerable<(string key, Vector3 at)> SilhouettePeople()
        {
            foreach (var kv in things) { var (t, _, _, shown) = kv.Value; if (shown && t && t.gameObject.activeInHierarchy && SilhouetteKey(kv.Key) && !(lifted.ContainsKey(kv.Key) && FallenGoesQuiet(kv.Key))) yield return (kv.Key, t.position); }
        }

        /// <summary>Brief 079: who keeps no cut and no silhouette once fallen: the opposition (hostiles, rivals, officers, the patrol). The cell keep
        /// theirs, and a body in the engine's Bodies has its own view (body/...), which never took a silhouette.</summary>
        static bool FallenGoesQuiet(string key) => !key.StartsWith("op/") && key is not ("waitress" or "guard" or "driver" or "councillor");

        readonly System.Collections.Generic.Dictionary<string, Transform> silhouetteOff = new();
        readonly System.Collections.Generic.List<string> silhouetteOn = new();

        /// <summary>Brief 079: a hostile lying down is not drawn again through the roof that has closed over him; up again, he is. Per frame over the
        /// fallen only (a handful), and the renderers walked only when one falls or gets up.</summary>
        void FallenSilhouettes()
        {
            foreach (var kv in lifted)
            {
                if (kv.Value > 0.5f || !SilhouetteKey(kv.Key) || !FallenGoesQuiet(kv.Key) || !things.TryGetValue(kv.Key, out var th) || !th.t) continue;
                var model = figs.TryGetValue(kv.Key, out var fig) && fig.model ? fig.model : th.t; // a change of clothes brings a new model with the bit on
                if (silhouetteOff.TryGetValue(kv.Key, out var was) && was == model) continue;
                silhouetteOff[kv.Key] = model;
                foreach (var r in model.GetComponentsInChildren<Renderer>(true)) r.renderingLayerMask &= ~SilhouettePass.SilhouetteLayer;
            }
            if (silhouetteOff.Count == 0) return;
            silhouetteOn.Clear();
            foreach (var kv in silhouetteOff) if (!lifted.ContainsKey(kv.Key)) silhouetteOn.Add(kv.Key);
            foreach (var key in silhouetteOn)
            {
                if (silhouetteOff[key]) foreach (var r in silhouetteOff[key].GetComponentsInChildren<Renderer>(true)) r.renderingLayerMask |= SilhouettePass.SilhouetteLayer;
                silhouetteOff.Remove(key);
            }
        }

        /// <summary>Marks every renderer of a person's model: the person bit, and the silhouette bit for the views that take one.</summary>
        static void MarkPerson(string key, GameObject model)
        {
            if (model == null || key.StartsWith("car/")) return;
            uint bits = SilhouettePass.PersonLayer | (SilhouetteKey(key) ? SilhouettePass.SilhouetteLayer : 0u);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true)) r.renderingLayerMask |= bits;
        }
    }
}
