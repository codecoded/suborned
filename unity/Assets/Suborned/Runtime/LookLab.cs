using System.Linq;
using UnityEngine;

namespace Suborned
{
    /// <summary>Brief 072 (Redline phase 2, F1 and 072-F): two look switches the perf harness (PerfSwitches, TpLongRun) and the camera lab share.
    /// Neither changes a material or adds a shader variant.
    ///  - Module shadows: the every-face modules cast from their L2 proxy (as built) or from their own LODs (L0 at play zoom), so Glint can judge
    ///    the proxy against full shadows on a sun-facing wall.
    ///  - Surface detail (072-F): the global float _SubornedSurfaceDetailOff (0, the default when nothing sets it, = detail as built; 1 = off).
    ///    FacadeLit skips its surface-detail array sample and the DecoKit cutaway shaders (the kit rows and the builder Look rows with a detail
    ///    pair) skip URP's detail mask, albedo and normal samples, each behind a uniform branch: the off side saves the fetches and the ALU,
    ///    not only the bandwidth. Keyword-free: the textured materials keep their _DETAIL_MULX2 variant either way.</summary>
    public static class LookLab
    {
        public static readonly int SurfaceDetailOffId = Shader.PropertyToID("_SubornedSurfaceDetailOff");

        public static bool SurfaceDetail => Shader.GetGlobalFloat(SurfaceDetailOffId) < 0.5f;

        public static void SetSurfaceDetail(bool on) => Shader.SetGlobalFloat(SurfaceDetailOffId, on ? 0f : 1f);

        static ShadowProxy[] modules; static bool[] modulesBuilt;

        static void FindModules()
        {
            if (modules != null && modules.All(p => p)) return;
            modules = Object.FindObjectsByType<ShadowProxy>(FindObjectsInactive.Include).Where(p => p.module).ToArray();
            modulesBuilt = modules.Select(p => p.On).ToArray();
        }

        /// <summary>The module proxies in the open scene (0 = a scene without every-face modules).</summary>
        public static int ModuleCount { get { FindModules(); return modules.Length; } }

        /// <summary>Whether the modules cast from their own LODs (no module proxy on).</summary>
        public static bool ModuleShadowsFromLods { get { FindModules(); return modules.Length > 0 && modules.All(p => !p.On); } }

        /// <summary>fromLods: the modules cast from their own LODs (L0 at play zoom); false: from their L2 proxies. Null = as built.</summary>
        public static void SetModuleShadows(bool? fromLods)
        {
            FindModules();
            for (int i = 0; i < modules.Length; i++) if (modules[i]) modules[i].Use(fromLods == null ? modulesBuilt[i] : !fromLods.Value);
        }
    }
}
