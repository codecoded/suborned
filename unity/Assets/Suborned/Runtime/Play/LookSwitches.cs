using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Play
{
    /// <summary>Diagnostic look switches for a player run (Splice's road-seam order, Glyph 0.2.115 #5: "shoot the view with SSAO off first, to learn
    /// what reads the seam"): `-nossao` turns the renderer's ScreenSpaceAmbientOcclusion feature off, `-noshadows` turns shadows off on every camera.
    /// For a launch only (a player's renderer data is not saved); never in the Editor; nothing in play changes without the flag.</summary>
    public static class LookSwitches
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor) return;
            if (GameBoot.Arg("-nossao"))
            {
                bool done = false;
                if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
                    foreach (var f in urp.rendererDataList[0].rendererFeatures) if (f is ScreenSpaceAmbientOcclusion) { f.SetActive(false); done = true; }
                Debug.Log(done ? "LookSwitches: SSAO off for this launch (-nossao)" : "LookSwitches: -nossao: no SSAO feature found");
            }
            if (GameBoot.Arg("-noshadows"))
            {
                foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { var d = c.GetUniversalAdditionalCameraData(); if (d != null) d.renderShadows = false; }
                Camera.onPreCull += c => { var d = c.GetUniversalAdditionalCameraData(); if (d != null && d.renderShadows) d.renderShadows = false; }; // cameras made later (the shot runner's, the road test's)
                Debug.Log("LookSwitches: shadows off for this launch (-noshadows)");
            }
        }
    }
}
