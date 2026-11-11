using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Play
{
    /// <summary>Brief 109 moment 8, the miniature look trial, for Sky to judge: its own launch switch, `-tiltshift on|off` (off by default, whatever
    /// -juice says), with `-tiltBand` (the sharp band's half height, 0..0.5 of the screen, default 0.16) and `-tiltBlur` (px at 1080 at the top and
    /// bottom edges, default 7) to tune it without a rebuild. A screen-space band blur (Shader Graph Generated/Juice109/Juice_TiltShift.shadergraph,
    /// a URP Fullscreen graph with a Custom Function) added for this launch as a Full Screen Pass after post-processing on the play renderer, so the
    /// HUD (UI Toolkit, drawn over the camera) stays sharp. Screen space, not depth of field: the play camera is orthographic, so depth reads
    /// little of near and far. A look only; for a launch only (the renderer asset is not saved).</summary>
    public static class Juice109Tilt
    {
        public static readonly bool On = (GameBoot.ArgValue("-tiltshift") ?? "off").ToLowerInvariant() is "on" or "1" or "true";
        public static string Status { get; private set; } = "off";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!On) return;
            var src = Resources.Load<Material>("Juice109/Juice-TiltShift"); if (!src) { Status = "no material"; Debug.LogWarning("Juice109Tilt: no Resources/Juice109/Juice-TiltShift"); return; }
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urp) { Status = "no URP"; return; }
            var mat = new Material(src) { name = "Juice109 tilt-shift (this launch)" };
            if (float.TryParse(GameBoot.ArgValue("-tiltBand"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var band)) mat.SetFloat("_TiltBand", Mathf.Clamp(band, 0f, 0.5f));
            if (float.TryParse(GameBoot.ArgValue("-tiltBlur"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var blur)) mat.SetFloat("_TiltBlur", Mathf.Clamp(blur, 0f, 24f));
            int n = 0;
            foreach (var data in urp.rendererDataList)
            {
                if (!data) continue;
                var f = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>(); f.name = "Juice109 tilt-shift";
                f.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing; f.fetchColorBuffer = true; f.passMaterial = mat; f.passIndex = 0;
                data.rendererFeatures.Add(f); data.SetDirty(); n++;
            }
            Status = $"on: band {mat.GetFloat("_TiltBand"):0.00}, blur {mat.GetFloat("_TiltBlur"):0.0} px on {n} renderers";
            Debug.Log("Juice109Tilt: " + Status);
        }
    }
}
