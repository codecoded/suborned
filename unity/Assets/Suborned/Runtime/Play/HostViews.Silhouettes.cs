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
        public static bool SilhouetteKey(string key) => key.StartsWith("op/") || key.StartsWith("hostile/") || key.StartsWith("unit/") || key.StartsWith("rivals/") || key.StartsWith("security/")
            || key is "patrol" or "waitress" or "guard" or "driver" or "councillor";

        /// <summary>Marks every renderer of a person's model: the person bit, and the silhouette bit for the views that take one.</summary>
        static void MarkPerson(string key, GameObject model)
        {
            if (model == null || key.StartsWith("car/")) return;
            uint bits = SilhouettePass.PersonLayer | (SilhouetteKey(key) ? SilhouettePass.SilhouetteLayer : 0u);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true)) r.renderingLayerMask |= bits;
        }
    }
}
