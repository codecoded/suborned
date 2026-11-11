using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Play
{
    /// <summary>Brief 109 moment 8, the miniature look (Sky: "cool but yeah, need to play with it. Think it might be too much for this game"): off by default,
    /// a switch and a strength slider in the pause menu's Display settings (FrontEnd.Juice109; Settings.display.miniature, miniatureStrength), read live.
    /// `-tiltshift on|off` overrides the setting for a launch (captures), with `-tiltBand`/`-tiltBlur` for exact values. A screen-space band blur (Shader Graph
    /// Generated/Juice109/Juice_TiltShift.shadergraph: a URP Fullscreen graph with a Custom Function), installed once as a Full Screen Pass after post-processing
    /// on the play renderer and switched on or off, so the HUD (UI Toolkit, over the camera) stays sharp. Screen space, not depth of field: the play camera is
    /// orthographic. A look only; the renderer asset is not saved.</summary>
    public static class Juice109Tilt
    {
        static readonly string Forced = (GameBoot.ArgValue("-tiltshift") ?? "").ToLowerInvariant();
        public static bool On { get; private set; }
        public static string Status { get; private set; } = "off";
        static Material mat; static FullScreenPassRendererFeature[] features = new FullScreenPassRendererFeature[0];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var src = Resources.Load<Material>("Juice109/Juice-TiltShift"); if (!src) { Status = "no material"; Debug.LogWarning("Juice109Tilt: no Resources/Juice109/Juice-TiltShift"); return; }
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urp) { Status = "no URP"; return; }
            mat = new Material(src) { name = "Juice109 tilt-shift (this launch)" };
            var list = new System.Collections.Generic.List<FullScreenPassRendererFeature>();
            foreach (var data in urp.rendererDataList)
            {
                if (!data) continue;
                var f = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>(); f.name = "Juice109 tilt-shift";
                f.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing; f.fetchColorBuffer = true; f.passMaterial = mat; f.passIndex = 0; f.SetActive(false);
                data.rendererFeatures.Add(f); data.SetDirty(); list.Add(f);
            }
            features = list.ToArray();
            Settings.Changed += Apply; Apply();
        }

        /// <summary>The setting (or the launch's flag) onto the pass: on or off, and the strength as the sharp band and the blur.</summary>
        static void Apply()
        {
            if (!mat) return; var d = Settings.Current.display;
            bool on = Forced is "on" or "1" or "true" || Forced is not ("off" or "0" or "false") && d.miniature;
            float s = Mathf.Clamp01(d.miniatureStrength / 100f);
            float band = Mathf.Lerp(0.32f, 0.06f, s), blur = Mathf.Lerp(2f, 14f, s);
            if (Forced.Length > 0 && float.TryParse(GameBoot.ArgValue("-tiltBand"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var b)) band = Mathf.Clamp(b, 0f, 0.5f);
            if (Forced.Length > 0 && float.TryParse(GameBoot.ArgValue("-tiltBlur"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var u)) blur = Mathf.Clamp(u, 0f, 24f);
            mat.SetFloat("_TiltBand", band); mat.SetFloat("_TiltBlur", blur);
            foreach (var f in features) if (f && f.isActive != on) f.SetActive(on);
            if (on != On || Status == "off" && on) Debug.Log($"Juice109Tilt: {(on ? "on" : "off")} (band {band:0.00}, blur {blur:0.0} px{(Forced.Length > 0 ? ", -tiltshift " + Forced : ", the pause menu's setting")})");
            On = on; Status = on ? $"on: band {band:0.00}, blur {blur:0.0} px" : "off";
        }
    }
}
