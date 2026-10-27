using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Suborned.Scratch
{
    /// <summary>Brief 073 SCRATCH (Glint, 2026-10-28): the "vintage print" finish prototype. Off by default. When on it
    /// (1) adds one global volume (priority 1000) with a Print073Grade from a preset, (2) scales RenderSettings.reflectionIntensity,
    /// (3) enqueues two passes on every Game base camera from beginCameraRendering (no renderer asset or scene edit):
    ///   cap   (BeforeRenderingPostProcessing): a soft shoulder on scene-linear HDR, so chrome and glass hot spots stop reading glossy CG
    ///          and feed less bloom;
    ///   print (AfterRenderingPostProcessing, enqueued before SignalsFeature's pass so signals still draw after it, on top): ink..paper
    ///          fade, warm cast, highlight desat, optional depth ink lines, screen-fixed halftone, paper grain and mottle, vignette.
    /// Toggle: Enable/Disable, the menu Suborned/Scratch/Print 073, or -print073 [subtle|default|strong] on the command line.</summary>
    public static class Print073
    {
        public static readonly string[] Presets = { "subtle", "default", "strong" };
        public static bool On => volumeGo != null;
        public static string Preset { get; private set; }

        static GameObject volumeGo; static VolumeProfile profile; static Material mat;
        static CapPass cap; static PrintPass print;
        static float reflection0 = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void FromCommandLine()
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-print073");
            if (i < 0) return;
            Enable(i + 1 < a.Length && Array.IndexOf(Presets, a[i + 1]) >= 0 ? a[i + 1] : "default");
        }

        public static void Enable(string preset)
        {
            Disable();
            var shader = Shader.Find("Hidden/Suborned/Scratch/Print073") ?? throw new InvalidOperationException("print073: shader missing");
            mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.hideFlags = HideFlags.HideAndDontSave;
            var g = profile.Add<Print073Grade>(true);
            float refl = Apply(g, preset);
            volumeGo = new GameObject("print073 volume (scratch)") { hideFlags = HideFlags.HideAndDontSave };
            var any = Object.FindAnyObjectByType<Volume>(); if (any) volumeGo.layer = any.gameObject.layer; // the layer the play camera's volume mask already sees
            var v = volumeGo.AddComponent<Volume>(); v.isGlobal = true; v.priority = 1000f; v.weight = 1f; v.sharedProfile = profile;
            reflection0 = RenderSettings.reflectionIntensity; RenderSettings.reflectionIntensity = reflection0 * refl;
            cap = new CapPass(mat); print = new PrintPass(mat);
            RenderPipelineManager.beginCameraRendering += Begin;
            Preset = preset;
            Debug.Log($"print073: on ({preset}); reflections x{refl:F2}");
        }

        public static void Disable()
        {
            RenderPipelineManager.beginCameraRendering -= Begin;
            if (reflection0 >= 0f) { RenderSettings.reflectionIntensity = reflection0; reflection0 = -1f; }
            if (volumeGo) Object.DestroyImmediate(volumeGo);
            if (profile) Object.DestroyImmediate(profile);
            if (mat) Object.DestroyImmediate(mat);
            volumeGo = null; profile = null; mat = null; cap = null; print = null; Preset = null;
        }

        /// <summary>The three looks. Returns the reflection-intensity scale.</summary>
        static float Apply(Print073Grade g, string preset)
        {
            void S(VolumeParameter<float> p, float v) => p.Override(v);
            S(g.strength, 1f);
            g.ink.Override(new Color(0.10f, 0.13f, 0.15f)); g.paper.Override(new Color(0.97f, 0.94f, 0.86f));
            S(g.inkLow, 0.05f); S(g.inkHigh, 0.25f); S(g.halftoneAngle, 15f);
            switch (preset)
            {
                case "subtle":
                    S(g.capMix, 0.7f); S(g.capKnee, 1.8f); S(g.capRange, 1.0f);
                    S(g.fade, 0.45f); S(g.saturation, 0.95f); S(g.highlightDesat, 0.25f); S(g.warmth, 0.03f);
                    S(g.inkLines, 0f); S(g.halftone, 0.10f); S(g.halftoneCell, 3.5f); S(g.halftoneColour, 0f);
                    S(g.grain, 0.035f); S(g.mottle, 0.03f); S(g.vignette, 0.06f);
                    return 0.7f;
                case "strong":
                    S(g.capMix, 1f); S(g.capKnee, 1.35f); S(g.capRange, 0.6f);
                    S(g.fade, 0.8f); S(g.saturation, 1.08f); S(g.highlightDesat, 0.4f); S(g.warmth, 0.06f);
                    S(g.inkLines, 0.85f); S(g.halftone, 0.38f); S(g.halftoneCell, 5f); S(g.halftoneColour, 0.25f);
                    S(g.grain, 0.065f); S(g.mottle, 0.06f); S(g.vignette, 0.14f);
                    return 0.35f;
                default:
                    S(g.capMix, 1f); S(g.capKnee, 1.5f); S(g.capRange, 0.8f);
                    S(g.fade, 0.6f); S(g.saturation, 1.0f); S(g.highlightDesat, 0.3f); S(g.warmth, 0.045f);
                    S(g.inkLines, 0.4f); S(g.halftone, 0.22f); S(g.halftoneCell, 4f); S(g.halftoneColour, 0f);
                    S(g.grain, 0.05f); S(g.mottle, 0.045f); S(g.vignette, 0.1f);
                    return 0.5f;
            }
        }

        static void Begin(ScriptableRenderContext _, Camera cam)
        {
            if (cam.cameraType != CameraType.Game || mat == null) return;
            var d = cam.GetUniversalAdditionalCameraData();
            if (d == null || d.renderType != CameraRenderType.Base || !d.renderPostProcessing) return;
            var g = VolumeManager.instance.stack?.GetComponent<Print073Grade>();
            // the stack is updated for this camera after beginCameraRendering, so read the profile's own (the only source) instead
            if (profile != null && profile.TryGet<Print073Grade>(out var own)) g = own;
            if (g == null || !g.IsActive()) return;
            cap.grade = g; print.grade = g; print.cam = cam;
            d.scriptableRenderer.EnqueuePass(cap);
            print.ConfigureInput(g.inkLines.value > 0f ? ScriptableRenderPassInput.Depth : ScriptableRenderPassInput.None);
            d.scriptableRenderer.EnqueuePass(print);
        }

        static readonly int CapId = Shader.PropertyToID("_P73Cap"), FadeId = Shader.PropertyToID("_P73Fade"), InkId = Shader.PropertyToID("_P73Ink"),
            PaperId = Shader.PropertyToID("_P73Paper"), ToneId = Shader.PropertyToID("_P73Tone"), LineId = Shader.PropertyToID("_P73Line"),
            DotId = Shader.PropertyToID("_P73Dot"), PaperFxId = Shader.PropertyToID("_P73PaperFx"), ScreenId = Shader.PropertyToID("_P73Screen"),
            ClipId = Shader.PropertyToID("_P73Clip"), DepthId = Shader.PropertyToID("_P73Depth");

        sealed class Data { public TextureHandle src, depth; public Material mat; public int pass; }

        static void Blit(RenderGraph rg, UniversalResourceData res, Material m, int pass, string name, TextureHandle depth)
        {
            var src = res.activeColorTexture; if (!src.IsValid()) return;
            var desc = rg.GetTextureDesc(src); desc.name = "_" + name.Replace(' ', '_'); desc.clearBuffer = false; desc.msaaSamples = MSAASamples.None; desc.depthBufferBits = DepthBits.None;
            var dst = rg.CreateTexture(desc);
            using (var b = rg.AddRasterRenderPass<Data>(name, out var d))
            {
                d.src = src; d.mat = m; d.pass = pass; d.depth = depth;
                b.UseTexture(src); if (depth.IsValid()) b.UseTexture(depth);
                b.SetRenderAttachment(dst, 0, AccessFlags.Write);
                b.SetRenderFunc(static (Data d, RasterGraphContext c) =>
                {
                    if (d.depth.IsValid()) d.mat.SetTexture(DepthId, d.depth);
                    Blitter.BlitTexture(c.cmd, d.src, new Vector4(1f, 1f, 0f, 0f), d.mat, d.pass);
                });
            }
            res.cameraColor = dst;
        }

        sealed class CapPass : ScriptableRenderPass
        {
            readonly Material m; public Print073Grade grade;
            public CapPass(Material m) { this.m = m; renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing; requiresIntermediateTexture = true; }
            public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData)
            {
                if (grade.capMix.value <= 0f) return;
                var res = frameData.Get<UniversalResourceData>(); if (res.isActiveTargetBackBuffer) return;
                m.SetVector(CapId, new Vector4(grade.capKnee.value, grade.capRange.value, grade.capMix.value * grade.strength.value, 0f));
                Blit(rg, res, m, 0, "Print073 cap", TextureHandle.nullHandle);
            }
        }

        sealed class PrintPass : ScriptableRenderPass
        {
            readonly Material m; public Print073Grade grade; public Camera cam;
            public PrintPass(Material m) { this.m = m; renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing; requiresIntermediateTexture = true; }
            public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData)
            {
                var res = frameData.Get<UniversalResourceData>(); if (res.isActiveTargetBackBuffer) return;
                var cd = frameData.Get<UniversalCameraData>();
                var g = grade; float k = g.strength.value;
                m.SetVector(FadeId, new Vector4(g.fade.value * k, g.saturation.value, g.highlightDesat.value * k, g.warmth.value * k));
                m.SetColor(InkId, g.ink.value); m.SetColor(PaperId, g.paper.value);
                var depth = g.inkLines.value > 0f ? res.cameraDepthTexture : TextureHandle.nullHandle;
                m.SetVector(LineId, new Vector4(depth.IsValid() ? g.inkLines.value * k : 0f, g.inkLow.value, g.inkHigh.value, cam != null && cam.orthographic ? 1f : 0f));
                m.SetVector(DotId, new Vector4(g.halftone.value * k, g.halftoneCell.value, g.halftoneAngle.value * Mathf.Deg2Rad, g.halftoneColour.value));
                m.SetVector(PaperFxId, new Vector4(g.grain.value * k, g.mottle.value * k, g.vignette.value * k, 0f));
                var td = cd.cameraTargetDescriptor; m.SetVector(ScreenId, new Vector4(td.width, td.height, td.height / 1080f, 0f));
                var c = cd.camera; m.SetVector(ClipId, new Vector4(c.nearClipPlane, c.farClipPlane, 0f, 0f));
                Blit(rg, res, m, 1, "Print073 print", depth);
            }
        }
    }
}
