using UnityEngine;

namespace Suborned.Perf
{
    /// <summary>The shadow-proxies switch (Redline fix 2): the scene's baked proxies (Suborned.ShadowProxy, built on kit facades and
    /// street trees by the world builder) on, so each piece casts from its LOD1 mesh and no LOD casts its own; or off, so every LOD
    /// casts as before the proxies (the scene as it was). Brief 072-E: the every-face modules' proxies (ShadowProxy.module, L2) switch
    /// apart (SetModules: moduleShadows proxy | lod0).</summary>
    public sealed class PerfProxies
    {
        readonly ShadowProxy[] all, mods;
        bool on, modsOn;

        public int Count => all.Length;
        public int ModuleCount => mods.Length;
        public int Triangles { get; }
        public int ModuleTriangles { get; }
        /// <summary>Whether the scene as built has its (kit facade and tree) proxies on.</summary>
        public bool BuiltOn { get; }
        /// <summary>Whether the scene as built has its module proxies on.</summary>
        public bool ModulesBuiltOn { get; }

        public PerfProxies()
        {
            var every = Object.FindObjectsByType<ShadowProxy>(FindObjectsInactive.Include);
            var a = new System.Collections.Generic.List<ShadowProxy>(); var m = new System.Collections.Generic.List<ShadowProxy>();
            foreach (var p in every)
            {
                int t = 0;
                if (p.proxy && p.proxy.TryGetComponent<MeshFilter>(out var f) && f.sharedMesh)
                    for (int s = 0; s < f.sharedMesh.subMeshCount; s++) t += (int)(f.sharedMesh.GetIndexCount(s) / 3);
                if (p.module) { m.Add(p); ModuleTriangles += t; if (p.On) ModulesBuiltOn = true; }
                else { a.Add(p); Triangles += t; if (p.On) BuiltOn = true; }
            }
            all = a.ToArray(); mods = m.ToArray();
            on = BuiltOn; modsOn = ModulesBuiltOn;
        }

        public void Set(bool value)
        {
            if (value == on) return;
            on = value;
            foreach (var p in all) if (p) p.Use(on);
        }

        public void SetModules(bool value)
        {
            if (value == modsOn) return;
            modsOn = value;
            foreach (var p in mods) if (p) p.Use(modsOn);
        }
    }
}
