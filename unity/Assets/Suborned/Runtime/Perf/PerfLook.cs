using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Perf
{
    /// <summary>What the play camera's picture is graded with, as this process resolves it (brief 008 B6, Glint's warmth question:
    /// the player reads warmer than the Editor): the volume stack the camera's volume layer and position blend to (tonemapping,
    /// colour adjustments, white balance, colour curves and lift/gamma/gain, split toning, shadows/midtones/highlights, bloom),
    /// the camera's post and HDR flags, the colour space, and the ambient light. The same code runs in the Editor
    /// (Suborned.World.Perf.WarmthProbe) and the player, so the two lists compare line for line.</summary>
    public static class PerfLook
    {
        public static string[] Read(Camera cam)
        {
            var lines = new List<string>();
            var ci = CultureInfo.InvariantCulture;
            string F(float v) => v.ToString("0.###", ci);
            string C(Color c) => $"({F(c.r)}, {F(c.g)}, {F(c.b)}, {F(c.a)})";
            lines.Add($"context = {(Application.isEditor ? (Application.isPlaying ? "editor play mode" : "editor") : "player")}, {SystemInfo.graphicsDeviceType}");
            lines.Add($"colorSpace = {QualitySettings.activeColorSpace}");
            lines.Add($"pipeline = {(GraphicsSettings.currentRenderPipeline ? GraphicsSettings.currentRenderPipeline.name : "none")}, quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            var urp = UniversalRenderPipeline.asset;
            if (urp) lines.Add($"urp hdr = {urp.supportsHDR}, colorGradingMode {urp.colorGradingMode}, lutSize {urp.colorGradingLutSize}, renderScale {F(urp.renderScale)}");
            var data = cam.GetUniversalAdditionalCameraData();
            lines.Add($"camera {cam.name}: allowHDR {cam.allowHDR}, renderPostProcessing {data.renderPostProcessing}, antialiasing {data.antialiasing}, volumeLayerMask {data.volumeLayerMask.value}, volumeTrigger {(data.volumeTrigger ? data.volumeTrigger.name : "camera")}, stopNaN {data.stopNaN}, dithering {data.dithering}");
            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include))
                lines.Add($"volume {v.name}: enabled {v.isActiveAndEnabled}, global {v.isGlobal}, weight {F(v.weight)}, priority {F(v.priority)}, profile {(v.sharedProfile ? v.sharedProfile.name : "none")}, layer {v.gameObject.layer}");
            var stack = data.volumeStack ?? VolumeManager.instance.stack;
            if (stack == null) lines.Add("volume stack = none (the camera has not rendered yet)");
            else
            {
                var t = stack.GetComponent<Tonemapping>();
                if (t) lines.Add($"tonemapping = {t.mode.value} (override {t.mode.overrideState}, active {t.IsActive()})");
                var ca = stack.GetComponent<ColorAdjustments>();
                if (ca) lines.Add($"colorAdjustments: postExposure {F(ca.postExposure.value)}, contrast {F(ca.contrast.value)}, colorFilter {C(ca.colorFilter.value)}, hueShift {F(ca.hueShift.value)}, saturation {F(ca.saturation.value)} (active {ca.IsActive()})");
                var wb = stack.GetComponent<WhiteBalance>();
                if (wb) lines.Add($"whiteBalance: temperature {F(wb.temperature.value)}, tint {F(wb.tint.value)} (active {wb.IsActive()})");
                var lgg = stack.GetComponent<LiftGammaGain>();
                if (lgg) lines.Add($"liftGammaGain: lift {lgg.lift.value}, gamma {lgg.gamma.value}, gain {lgg.gain.value} (active {lgg.IsActive()})");
                var smh = stack.GetComponent<ShadowsMidtonesHighlights>();
                if (smh) lines.Add($"shadowsMidtonesHighlights: shadows {smh.shadows.value}, midtones {smh.midtones.value}, highlights {smh.highlights.value} (active {smh.IsActive()})");
                var st = stack.GetComponent<SplitToning>();
                if (st) lines.Add($"splitToning: shadows {C(st.shadows.value)}, highlights {C(st.highlights.value)}, balance {F(st.balance.value)} (active {st.IsActive()})");
                var cm = stack.GetComponent<ChannelMixer>();
                if (cm) lines.Add($"channelMixer: active {cm.IsActive()}");
                var cc = stack.GetComponent<ColorCurves>();
                if (cc) lines.Add($"colorCurves: active {cc.IsActive()}");
                var ccl = stack.GetComponent<ColorLookup>();
                if (ccl) lines.Add($"colorLookup: active {ccl.IsActive()}, contribution {F(ccl.contribution.value)}");
                var b = stack.GetComponent<Bloom>();
                if (b) lines.Add($"bloom: intensity {F(b.intensity.value)}, threshold {F(b.threshold.value)}, tint {C(b.tint.value)}, scatter {F(b.scatter.value)} (active {b.IsActive()})");
                var vg = stack.GetComponent<Vignette>();
                if (vg) lines.Add($"vignette: intensity {F(vg.intensity.value)}, color {C(vg.color.value)} (active {vg.IsActive()})");
            }
            lines.Add($"ambient: mode {RenderSettings.ambientMode}, intensity {F(RenderSettings.ambientIntensity)}, sky {C(RenderSettings.ambientSkyColor)}, equator {C(RenderSettings.ambientEquatorColor)}, ground {C(RenderSettings.ambientGroundColor)}, flat {C(RenderSettings.ambientLight)}");
            var sh = RenderSettings.ambientProbe; lines.Add($"ambient probe L0 rgb = ({F(sh[0, 0])}, {F(sh[1, 0])}, {F(sh[2, 0])})");
            lines.Add($"reflection: mode {RenderSettings.defaultReflectionMode}, intensity {F(RenderSettings.reflectionIntensity)}, custom {(RenderSettings.customReflectionTexture ? RenderSettings.customReflectionTexture.name : "none")}");
            lines.Add($"fog: {RenderSettings.fog}, color {C(RenderSettings.fogColor)}");
            var sun = RenderSettings.sun;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                if (l.type == LightType.Directional) lines.Add($"directional {l.name}{(l == sun ? " (sun)" : "")}: color {C(l.color)}, intensity {F(l.intensity)}, useColorTemperature {l.useColorTemperature} {F(l.colorTemperature)} K, euler {l.transform.eulerAngles}");
            lines.Add($"lightsUseLinearIntensity {GraphicsSettings.lightsUseLinearIntensity}, lightsUseColorTemperature {GraphicsSettings.lightsUseColorTemperature}");
            return lines.ToArray();
        }
    }
}
