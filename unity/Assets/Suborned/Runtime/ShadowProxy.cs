using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned
{
    /// <summary>A shadow proxy (brief 008 B6, Redline fix 2): a shadows-only renderer with a kit piece's LOD1 mesh, standing in for the
    /// piece's LOD renderers in every shadow cascade. At play zoom the LODs never switch (lod-play-zoom.html), so without it a facade
    /// casts its LOD0 into every cascade; with it the shadow costs LOD1 triangles whatever LOD the camera shows. Built by the world
    /// builder (ShadowProxies) on kit facades and street trees; `casters` are the piece's LOD renderers and `was` their casting mode
    /// before the proxy took over, so Use(false) puts the piece back as it was (the perf harness's proxies switch).</summary>
    public sealed class ShadowProxy : MonoBehaviour
    {
        public MeshRenderer proxy;
        public Renderer[] casters;
        public ShadowCastingMode[] was;

        public bool On => proxy && proxy.enabled;

        public void Use(bool on)
        {
            if (proxy) proxy.enabled = on;
            for (int i = 0; i < casters.Length; i++)
                if (casters[i]) casters[i].shadowCastingMode = on ? ShadowCastingMode.Off : was[i];
        }
    }
}
