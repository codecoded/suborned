using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Perf
{
    /// <summary>Applies a variant's A/B switches (Redline item 0) to the running player and puts everything back for "as built":
    /// SSAO (URP's feature settings are internal, so by reflection: AfterOpaque and Source), the play camera's shadow cascades
    /// (TacticalShadows) and shadows, the architecture fade, and the shadow proxies. Each Apply sets every switch, so the order of
    /// variants never matters.</summary>
    public sealed class PerfSwitches
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly UniversalAdditionalCameraData data;
        readonly TacticalShadows shadows;
        readonly ArchitectureFade fade;
        readonly PerfProxies proxies;
        readonly ScriptableRendererFeature ssao;
        readonly object ssaoSettings;
        readonly FieldInfo afterOpaque, source;
        readonly object builtSource, depthSource, normalsSource;
        readonly bool builtAfterOpaque, builtActive, builtFade;
        readonly int builtCascades;

        /// <summary>What the player's SSAO is as built ("on, after opaque True, source Depth"), or why it cannot be switched.</summary>
        public string SsaoAsBuilt { get; }

        public PerfSwitches(Camera cam, TacticalShadows shadows, ArchitectureFade fade, PerfProxies proxies)
        {
            this.shadows = shadows; this.fade = fade; this.proxies = proxies;
            data = cam.GetUniversalAdditionalCameraData();
            builtCascades = shadows.cascades; builtFade = fade.fade;
            try
            {
                var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
                foreach (var f in urp.rendererDataList[0].rendererFeatures) if (f is ScreenSpaceAmbientOcclusion) ssao = f;
                if (!ssao) { SsaoAsBuilt = "no SSAO feature"; return; }
                ssaoSettings = typeof(ScreenSpaceAmbientOcclusion).GetField("m_Settings", Any).GetValue(ssao);
                var t = ssaoSettings.GetType();
                afterOpaque = t.GetField("AfterOpaque", Any); source = t.GetField("Source", Any);
                builtActive = ssao.isActive; builtAfterOpaque = (bool)afterOpaque.GetValue(ssaoSettings); builtSource = source.GetValue(ssaoSettings);
                depthSource = Enum.ToObject(source.FieldType, 0); // DepthSource.Depth
                normalsSource = Enum.ToObject(source.FieldType, 1); // DepthSource.DepthNormals
                SsaoAsBuilt = $"{(ssao.isActive ? "on" : "off")}, after opaque {builtAfterOpaque}, source {builtSource}";
            }
            catch (Exception e) { SsaoAsBuilt = "cannot switch: " + e.Message; ssaoSettings = null; }
        }

        public void Apply(PerfVariant v)
        {
            string mode = string.IsNullOrEmpty(v.ssao) ? "on" : v.ssao;
            if (mode != "on" && mode != "depth" && mode != "normals" && mode != "off") throw new InvalidOperationException($"Perf: variant `{v.name}` ssao `{mode}`: on, depth, normals or off");
            if (mode != "on" && ssaoSettings == null) throw new InvalidOperationException($"Perf: variant `{v.name}` switches SSAO, but {SsaoAsBuilt}");
            if (ssaoSettings != null)
            {
                ssao.SetActive(mode switch { "off" => false, "on" => builtActive, _ => true });
                afterOpaque.SetValue(ssaoSettings, mode switch { "depth" => true, "normals" => false, _ => builtAfterOpaque });
                source.SetValue(ssaoSettings, mode switch { "depth" => depthSource, "normals" => normalsSource, _ => builtSource });
            }
            shadows.cascades = v.cascades > 0 ? v.cascades : builtCascades;
            data.renderShadows = !v.shadowsOff;
            fade.fade = builtFade && !v.fadeOff;
            if (v.UsesProxies && proxies is not { Count: > 0 }) throw new InvalidOperationException($"Perf: variant `{v.name}` switches shadow proxies, but this scene has none");
            if (proxies != null) proxies.Set(v.proxies == "on" || (v.proxies != "off" && proxies.BuiltOn));
        }

        public void Restore() => Apply(new PerfVariant { name = "as built" });
    }
}
