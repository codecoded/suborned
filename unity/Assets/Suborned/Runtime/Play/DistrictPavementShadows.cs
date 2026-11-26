using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Play
{
    public sealed class DistrictPavementShadows : MonoBehaviour
    {
        public MeshRenderer[] surfaces;

        void Awake()
        {
            // A same-player A/B restores only the original casting pass.
            if (!GameBoot.Arg("-fullPavementShadows")) return;
            foreach (var surface in surfaces)
                if (surface) surface.shadowCastingMode = ShadowCastingMode.On;
        }
    }
}
