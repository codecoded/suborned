using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 109 round 4: the cars drawn now, for the juice's lights (Juice109Lights): each car's view key, its body (the model HostViews.Cars turns
    /// to the heading; its front is the kit's +Z) and the model it was made from (its lamps are read from that model at build time). Read only.</summary>
    public sealed partial class HostViews
    {
        public IEnumerable<(string key, Transform body, string model)> CarBodies()
        {
            foreach (var kv in carModels)
            {
                if (!kv.Key.StartsWith("car/") || !kv.Value || !kv.Value.gameObject.activeInHierarchy || !things.TryGetValue(kv.Key, out var th) || !th.shown) continue;
                yield return (kv.Key, kv.Value, modelSource.TryGetValue(kv.Key, out var m) ? m : "");
            }
        }
    }
}
