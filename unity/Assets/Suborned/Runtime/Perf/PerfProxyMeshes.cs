using System;
using UnityEngine;

namespace Suborned.Perf
{
    /// <summary>The LOD1 meshes the shadow proxies cast with (PerfProxies), keyed by each LOD group's sibling-index path under the
    /// world root. Written by Suborned.World.Perf.PerfProxyAsset into a Resources folder for the perf player's build only and deleted
    /// after it: in a player, kit facades are static-batched, so their own LOD1 meshes are gone by the time the scene runs.</summary>
    public sealed class PerfProxyMeshes : ScriptableObject
    {
        public const string ResourceName = "PerfProxyMeshes";
        public string[] groups = Array.Empty<string>();
        public Mesh[] meshes = Array.Empty<Mesh>();
    }
}
