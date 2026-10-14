using UnityEngine;

namespace Suborned.Perf
{
    /// <summary>The shadow-proxies switch (Redline fix 2): the scene's baked proxies (Suborned.ShadowProxy, built on kit facades and
    /// street trees by the world builder) on, so each piece casts from its LOD1 mesh and no LOD casts its own; or off, so every LOD
    /// casts as before the proxies (the scene as it was).</summary>
    public sealed class PerfProxies
    {
        readonly ShadowProxy[] all;
        bool on;

        public int Count => all.Length;
        public int Triangles { get; }
        /// <summary>Whether the scene as built has its proxies on.</summary>
        public bool BuiltOn { get; }

        public PerfProxies()
        {
            all = Object.FindObjectsByType<ShadowProxy>(FindObjectsInactive.Include);
            foreach (var p in all)
            {
                if (p.proxy && p.proxy.TryGetComponent<MeshFilter>(out var f) && f.sharedMesh)
                    for (int s = 0; s < f.sharedMesh.subMeshCount; s++) Triangles += (int)(f.sharedMesh.GetIndexCount(s) / 3);
                if (p.On) BuiltOn = true;
            }
            on = BuiltOn;
        }

        public void Set(bool value)
        {
            if (value == on) return;
            on = value;
            foreach (var p in all) if (p) p.Use(on);
        }
    }
}
