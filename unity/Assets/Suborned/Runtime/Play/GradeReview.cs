using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Play
{
    /// <summary>Glint, brief 096 grading review (Sky 2026-11-05: "our entire color grading is a little off still. Not utilising Unity to it's fullest").
    /// A review switch for a launch only, as LookSwitches: `-grade &lt;spec.json&gt;` lays one option over the play look so the shot runner can take it.
    /// Without the flag nothing runs; with it nothing is saved (the URP asset, the renderer and the kit materials are changed in memory for this launch).
    /// The spec (suborned.glint-grade/1) names only what it changes; the rest is today's look:
    ///  · a global Volume at priority 1000 (above the play grade and the lens, 100): tonemapping, ColorAdjustments, WhiteBalance, LiftGammaGain,
    ///    ShadowsMidtonesHighlights, SplitToning, ColorCurves (master, satVsSat, hueVsSat), ColorLookup (a strip PNG, sRGB off), Bloom, Vignette;
    ///  · the URP asset's colour grading mode (HDR / LDR) and LUT size; the SSAO feature (on in the ortho play view, its settings);
    ///  · the play light (OldTownPlayLook's sun intensity, colour, shadow strength; its ambient probe scaled per channel);
    ///  · kit materials by name (base colour, smoothness, environment reflections, sun specular, emission, bump, detail).
    /// It logs "GradeReview effective {...}" (the volume stack the play camera rendered with) so the review records today's values as rendered.</summary>
    public sealed class GradeReview : MonoBehaviour
    {
        [Serializable] public sealed class Mat { public string name = ""; public string baseColour = ""; public float smoothness = float.NaN, env = float.NaN, spec = float.NaN, bump = float.NaN, detail = float.NaN, emissionScale = float.NaN; public string emission = ""; }
        [Serializable] public sealed class Spec
        {
            public string id = "", tonemap = "", grading = "", filter = "", splitShadows = "", splitHighlights = "", lut = "", sunColour = "", vignetteColour = "";
            public float exposure = float.NaN, contrast = float.NaN, saturation = float.NaN, temperature = float.NaN, tint = float.NaN, hueShift = float.NaN;
            public float[] lift = { }, gamma = { }, gain = { }, shadows = { }, midtones = { }, highlights = { };
            public float shStart = float.NaN, shEnd = float.NaN, hiStart = float.NaN, hiEnd = float.NaN, splitBalance = float.NaN, lutContribution = float.NaN;
            public float[] master = { }, satVsSat = { }, hueVsSat = { }, lumVsSat = { };
            public float bloomThreshold = float.NaN, bloomIntensity = float.NaN, bloomScatter = float.NaN, bloomClamp = float.NaN, vignette = float.NaN;
            public int ssao = -1, ssaoDownsample = -1, ssaoAfterOpaque = -1, ssaoSamples = -1, ssaoBlur = -1, ssaoSource = -1;
            public float ssaoIntensity = float.NaN, ssaoRadius = float.NaN, ssaoFalloff = float.NaN, ssaoDirect = float.NaN;
            public float sunIntensity = float.NaN, sunShadow = float.NaN; public float[] ambientScale = { };
            public Mat[] mats = { };
        }

        Spec s; int frames; bool logged, pending = true;
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || GameBoot.ArgValue("-grade") is not string path) return;
            var spec = JsonUtility.FromJson<Spec>(File.ReadAllText(path));
            var go = new GameObject("grade review (" + spec.id + ")"); DontDestroyOnLoad(go);
            var r = go.AddComponent<GradeReview>(); r.s = spec; r.Install(Path.GetDirectoryName(Path.GetFullPath(path)));
        }

        static bool Has(float v) => !float.IsNaN(v);
        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : throw new InvalidDataException("GradeReview: colour " + h);
        static Vector4 V4(float[] a) => new(a[0], a[1], a[2], a.Length > 3 ? a[3] : 0f);
        static TextureCurve Curve(float[] xy, float zero, bool loop)
        {
            var k = new Keyframe[xy.Length / 2]; for (int i = 0; i < k.Length; i++) k[i] = new Keyframe(xy[2 * i], xy[2 * i + 1]);
            return new TextureCurve(k, zero, loop, new Vector2(0f, 1f));
        }

        void Install(string dir)
        {
            var log = new List<string>();
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            var v = gameObject.AddComponent<Volume>(); v.isGlobal = true; v.priority = 1000f; v.weight = 1f; v.sharedProfile = p;
            if (s.tonemap != "") { var t = p.Add<Tonemapping>(); t.mode.Override(s.tonemap == "neutral" ? TonemappingMode.Neutral : s.tonemap == "none" ? TonemappingMode.None : TonemappingMode.ACES); }
            if (Has(s.exposure) || Has(s.contrast) || Has(s.saturation) || Has(s.hueShift) || s.filter != "")
            {
                var ca = p.Add<ColorAdjustments>();
                if (Has(s.exposure)) ca.postExposure.Override(s.exposure);
                if (Has(s.contrast)) ca.contrast.Override(s.contrast);
                if (Has(s.saturation)) ca.saturation.Override(s.saturation);
                if (Has(s.hueShift)) ca.hueShift.Override(s.hueShift);
                if (s.filter != "") ca.colorFilter.Override(Hex(s.filter));
            }
            if (Has(s.temperature) || Has(s.tint)) { var wb = p.Add<WhiteBalance>(); if (Has(s.temperature)) wb.temperature.Override(s.temperature); if (Has(s.tint)) wb.tint.Override(s.tint); }
            if (s.lift.Length + s.gamma.Length + s.gain.Length > 0)
            {
                var l = p.Add<LiftGammaGain>();
                l.lift.Override(s.lift.Length > 0 ? V4(s.lift) : new Vector4(1, 1, 1, 0)); l.gamma.Override(s.gamma.Length > 0 ? V4(s.gamma) : new Vector4(1, 1, 1, 0)); l.gain.Override(s.gain.Length > 0 ? V4(s.gain) : new Vector4(1, 1, 1, 0));
            }
            if (s.shadows.Length + s.midtones.Length + s.highlights.Length > 0)
            {
                var m = p.Add<ShadowsMidtonesHighlights>();
                if (s.shadows.Length > 0) m.shadows.Override(V4(s.shadows)); if (s.midtones.Length > 0) m.midtones.Override(V4(s.midtones)); if (s.highlights.Length > 0) m.highlights.Override(V4(s.highlights));
                if (Has(s.shStart)) m.shadowsStart.Override(s.shStart); if (Has(s.shEnd)) m.shadowsEnd.Override(s.shEnd); if (Has(s.hiStart)) m.highlightsStart.Override(s.hiStart); if (Has(s.hiEnd)) m.highlightsEnd.Override(s.hiEnd);
            }
            if (s.splitShadows != "" || s.splitHighlights != "")
            {
                var st = p.Add<SplitToning>();
                if (s.splitShadows != "") st.shadows.Override(Hex(s.splitShadows)); if (s.splitHighlights != "") st.highlights.Override(Hex(s.splitHighlights)); if (Has(s.splitBalance)) st.balance.Override(s.splitBalance);
            }
            if (s.master.Length + s.satVsSat.Length + s.hueVsSat.Length + s.lumVsSat.Length > 0)
            {
                var cc = p.Add<ColorCurves>();
                if (s.master.Length > 0) cc.master.Override(Curve(s.master, 0f, false));
                if (s.satVsSat.Length > 0) cc.satVsSat.Override(Curve(s.satVsSat, .5f, false));
                if (s.hueVsSat.Length > 0) cc.hueVsSat.Override(Curve(s.hueVsSat, .5f, true));
                if (s.lumVsSat.Length > 0) cc.lumVsSat.Override(Curve(s.lumVsSat, .5f, false));
            }
            if (s.lut != "")
            {
                var file = Path.IsPathRooted(s.lut) ? s.lut : Path.Combine(dir, s.lut);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = Path.GetFileName(file) };
                tex.LoadImage(File.ReadAllBytes(file), false);
                var cl = p.Add<ColorLookup>(); cl.texture.Override(tex); cl.contribution.Override(Has(s.lutContribution) ? s.lutContribution : 1f);
                log.Add($"lut {tex.width}x{tex.height} valid {cl.ValidateLUT()}");
            }
            if (Has(s.bloomThreshold) || Has(s.bloomIntensity) || Has(s.bloomScatter) || Has(s.bloomClamp))
            {
                var b = p.Add<Bloom>();
                if (Has(s.bloomThreshold)) b.threshold.Override(s.bloomThreshold); if (Has(s.bloomIntensity)) b.intensity.Override(s.bloomIntensity);
                if (Has(s.bloomScatter)) b.scatter.Override(s.bloomScatter); if (Has(s.bloomClamp)) b.clamp.Override(s.bloomClamp);
            }
            if (Has(s.vignette)) { var vg = p.Add<Vignette>(); vg.intensity.Override(s.vignette); vg.smoothness.Override(.6f); if (s.vignetteColour != "") vg.color.Override(Hex(s.vignetteColour)); }

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                if (s.grading != "") urp.colorGradingMode = s.grading == "hdr" ? ColorGradingMode.HighDynamicRange : ColorGradingMode.LowDynamicRange;
                log.Add($"grading {urp.colorGradingMode} lut {urp.colorGradingLutSize}");
                foreach (var f in urp.rendererDataList[0].rendererFeatures)
                    if (f is ScreenSpaceAmbientOcclusion ao && s.ssao >= 0)
                    {
                        var st = typeof(ScreenSpaceAmbientOcclusion).GetField("m_Settings", Any).GetValue(ao); var t = st.GetType();
                        void Set(string n, object val) { var fi = t.GetField(n, Any); if (fi == null) { log.Add("ssao: no field " + n); return; } fi.SetValue(st, fi.FieldType.IsEnum ? Enum.ToObject(fi.FieldType, val) : Convert.ChangeType(val, fi.FieldType)); }
                        if (Has(s.ssaoIntensity)) Set("Intensity", s.ssaoIntensity); if (Has(s.ssaoRadius)) Set("Radius", s.ssaoRadius); if (Has(s.ssaoFalloff)) Set("Falloff", s.ssaoFalloff);
                        if (Has(s.ssaoDirect)) Set("DirectLightingStrength", s.ssaoDirect); if (s.ssaoDownsample >= 0) Set("Downsample", s.ssaoDownsample == 1); if (s.ssaoAfterOpaque >= 0) Set("AfterOpaque", s.ssaoAfterOpaque == 1);
                        if (s.ssaoSamples >= 0) Set("Samples", s.ssaoSamples); if (s.ssaoBlur >= 0) Set("BlurQuality", s.ssaoBlur); if (s.ssaoSource >= 0) Set("Source", s.ssaoSource);
                        ao.SetActive(s.ssao == 1); ao.Create();
                        log.Add("ssao " + string.Join(", ", t.GetFields(Any).Select(fi => fi.Name + " " + fi.GetValue(st))));
                    }
            }
            foreach (var m in s.mats) log.Add(Retune(m));
            // the kit materials load with the play scene, after this first scene: retune on every scene load, before its Start (the host reads the ring's emission when it builds a head)
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (sc, _) => { foreach (var m in s.mats) Debug.Log($"GradeReview: scene {sc.name}: {Retune(m)}"); };
            Debug.Log($"GradeReview: '{s.id}' installed ({p.components.Count} volume overrides: {string.Join(", ", p.components.Select(c => c.GetType().Name))}); {string.Join("; ", log)}");
            RenderPipelineManager.beginCameraRendering += Effective; // after OldTownPlayLook's own Begin (subscribed first): the play light as it renders
        }

        static string Retune(Mat m)
        {
            var hit = Resources.FindObjectsOfTypeAll<Material>().Where(x => x.name == m.name || x.name.StartsWith(m.name + " ")).ToArray();
            foreach (var x in hit)
            {
                string was = $"{x.name}: base #{ColorUtility.ToHtmlStringRGB(x.GetColor("_BaseColor"))} sm {x.GetFloat("_Smoothness"):0.##} env {(x.HasFloat("_EnvironmentReflections") ? x.GetFloat("_EnvironmentReflections") : -1)} spec {(x.HasFloat("_SpecularHighlights") ? x.GetFloat("_SpecularHighlights") : -1)} emit {x.GetColor("_EmissionColor")}";
                if (m.baseColour != "") { x.SetColor("_BaseColor", Hex(m.baseColour)); if (x.HasColor("_Color")) x.SetColor("_Color", Hex(m.baseColour)); }
                if (Has(m.smoothness)) x.SetFloat("_Smoothness", m.smoothness);
                if (Has(m.env)) { x.SetFloat("_EnvironmentReflections", m.env); if (m.env < .5f) x.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); else x.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); }
                if (Has(m.spec)) { x.SetFloat("_SpecularHighlights", m.spec); if (m.spec < .5f) x.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); else x.DisableKeyword("_SPECULARHIGHLIGHTS_OFF"); }
                if (Has(m.bump) && x.HasFloat("_BumpScale")) x.SetFloat("_BumpScale", m.bump);
                if (Has(m.detail) && x.HasFloat("_DetailAlbedoMapScale")) { x.SetFloat("_DetailAlbedoMapScale", m.detail); x.SetFloat("_DetailNormalMapScale", m.detail); }
                if (m.emission != "" || Has(m.emissionScale))
                {
                    var e = m.emission != "" ? Hex(m.emission).linear : x.GetColor("_EmissionColor"); if (Has(m.emissionScale)) e *= m.emissionScale;
                    x.SetColor("_EmissionColor", e); x.EnableKeyword("_EMISSION"); x.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                Debug.Log("GradeReview mat " + was);
            }
            return $"{m.name} x{hit.Length}";
        }

        void Update()
        {
            if (Time.frameCount % 60 != 1) return;
            if (s.mats.Length > 0 && pending) { pending = false; foreach (var m in s.mats) if (Retune(m).EndsWith(" x0")) pending = true; } // until each is found
            // the play light: OldTownPlayLook (assembly Suborned.OldTown, not referenced here) and TacticalShadows, every frame (the host may make cameras late)
            foreach (var b in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                var t = b.GetType();
                if (t.Name == "OldTownPlayLook")
                {
                    if (Has(s.sunIntensity)) t.GetField("sunIntensity").SetValue(b, s.sunIntensity);
                    if (Has(s.sunShadow)) t.GetField("sunShadowStrength").SetValue(b, s.sunShadow);
                    if (s.sunColour != "") t.GetField("sunColour").SetValue(b, Hex(s.sunColour));
                    if (s.ambientScale.Length == 3 && !scaled.Contains(b))
                    {
                        scaled.Add(b); var probe = (float[])t.GetField("ambientProbe").GetValue(b);
                        if (probe != null && probe.Length == 27) for (int ch = 0; ch < 3; ch++) for (int k = 0; k < 9; k++) probe[ch * 9 + k] *= s.ambientScale[ch];
                        foreach (var (f, ch) in new[] { ("ambientSky", 0), ("ambientEquator", 1), ("ambientGround", 2) }) { var c = (Color)t.GetField(f).GetValue(b); t.GetField(f).SetValue(b, new Color(c.r * s.ambientScale[0], c.g * s.ambientScale[1], c.b * s.ambientScale[2], c.a)); }
                    }
                }
                else if (b is TacticalShadows ts && s.ssao == 1) ts.orthoSsao = true;
                else if (t.Name == "AtmosphereLens" && Has(s.sunShadow) && t.GetField("data", Any)?.GetValue(b) is { } d && d.GetType().GetField("sun")?.GetValue(d) is { } sp)
                    sp.GetType().GetField("shadowStrength").SetValue(sp, s.sunShadow); // the lens sets the sun's shadow strength every render (lens.json sun.shadowStrength)
            }
        }
        readonly HashSet<MonoBehaviour> scaled = new();

        void Effective(ScriptableRenderContext _, Camera c)
        {
            if (!c.orthographic || c.cameraType != CameraType.Game || ++frames < 30 || logged && frames % 600 != 0) return;
            logged = true;
            var st = VolumeManager.instance.stack; var o = new List<string>();
            var tm = st.GetComponent<Tonemapping>(); var ca = st.GetComponent<ColorAdjustments>(); var wb = st.GetComponent<WhiteBalance>(); var lg = st.GetComponent<LiftGammaGain>();
            var sm = st.GetComponent<ShadowsMidtonesHighlights>(); var sp = st.GetComponent<SplitToning>(); var bl = st.GetComponent<Bloom>(); var cl = st.GetComponent<ColorLookup>(); var vg = st.GetComponent<Vignette>();
            o.Add($"\"tonemap\":\"{tm.mode.value}\""); o.Add($"\"exposure\":{ca.postExposure.value:0.###}"); o.Add($"\"contrast\":{ca.contrast.value:0.#}"); o.Add($"\"saturation\":{ca.saturation.value:0.#}");
            o.Add($"\"filter\":\"#{ColorUtility.ToHtmlStringRGB(ca.colorFilter.value)}\""); o.Add($"\"wb\":[{wb.temperature.value:0.#},{wb.tint.value:0.#}]"); o.Add($"\"lift\":\"{lg.lift.value}\""); o.Add($"\"gain\":\"{lg.gain.value}\"");
            o.Add($"\"smh\":\"{sm.shadows.value} {sm.midtones.value} {sm.highlights.value}\""); o.Add($"\"split\":\"#{ColorUtility.ToHtmlStringRGB(sp.shadows.value)}/#{ColorUtility.ToHtmlStringRGB(sp.highlights.value)}\"");
            o.Add($"\"bloom\":[{bl.threshold.value:0.##},{bl.intensity.value:0.##},{bl.scatter.value:0.##},{bl.clamp.value:0.#}]"); o.Add($"\"lut\":{(cl.IsActive() ? 1 : 0)}"); o.Add($"\"vignette\":{vg.intensity.value:0.##}");
            var sun = RenderSettings.sun; if (sun) o.Add($"\"sun\":[\"#{ColorUtility.ToHtmlStringRGB(sun.color)}\",{sun.intensity:0.##},{sun.shadowStrength:0.##}]");
            o.Add($"\"ambient\":\"{RenderSettings.ambientMode}\""); o.Add($"\"camera\":\"{c.name}\"");
            Debug.Log("GradeReview effective {" + string.Join(",", o) + "}");
        }
    }
}
