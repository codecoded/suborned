using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Suborned
{
    /// <summary>Brief 064 A3 item 7 (Latch, 2026-10-28): the signal layer drawn in the camera's own frame, after post, in place of the
    /// stacked overlay camera HostFightLab used to add (one camera's culling, one colour and depth load/store fewer). The layer
    /// (<see cref="Layer"/>) is left out of PC_Renderer's opaque and transparent filters, so the scene passes never draw it; this pass draws it
    /// after post-processing, so the grade, bloom and SMAA never touch a signal (GDD §10, §15: signals unlit and emissive, outside the grade),
    /// against a depth buffer of its own, cleared, as the overlay camera's was (URP's overlay default, clear depth: signals on top of the
    /// world; a ghost's ZWrite still sorts its own parts). The pass runs only for a camera whose culling mask holds the layer, and only on a
    /// frame someone asked for it (<see cref="Want"/>): when nothing is on the layer the frame keeps post writing straight to the backbuffer.</summary>
    public sealed class SignalsFeature : ScriptableRendererFeature
    {
        /// <summary>The signal layer (HandoverHost.SignalLayer).</summary>
        public const int Layer = 8;
        static int wanted = -10;

        /// <summary>Draw the signal layer this frame (and the next, so a request made after the frame's first camera still lands).</summary>
        public static void Want() => wanted = Time.frameCount;
        static bool Wanted => Time.frameCount - wanted <= 1;

        SignalsPass pass;

        public override void Create() => pass = new SignalsPass();

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cam = renderingData.cameraData.camera;
            if (!Wanted || (cam.cullingMask & (1 << Layer)) == 0 || renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection) return;
            renderer.EnqueuePass(pass);
        }

        sealed class SignalsPass : ScriptableRenderPass
        {
            static readonly List<ShaderTagId> Tags = new() { new ShaderTagId("SRPDefaultUnlit"), new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly") };

            public SignalsPass() { renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing; profilingSampler = new ProfilingSampler("Suborned signals"); }

            sealed class PassData { public RendererListHandle opaque, transparent; }

            public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData)
            {
                var res = frameData.Get<UniversalResourceData>(); var rd = frameData.Get<UniversalRenderingData>();
                var cd = frameData.Get<UniversalCameraData>(); var ld = frameData.Get<UniversalLightData>();
                var color = res.activeColorTexture; if (!color.IsValid()) return;
                var cdesc = rg.GetTextureDesc(color);
                var ddesc = new TextureDesc(cdesc.width, cdesc.height)
                {   // the overlay camera's cleared depth: transient, cleared on first use, discarded after (memoryless on a tile GPU)
                    name = "_SubornedSignalsDepth", format = GraphicsFormat.D32_SFloat_S8_UInt, msaaSamples = cdesc.msaaSamples,
                    clearBuffer = true, clearColor = Color.black, discardBuffer = true
                };
                var depth = rg.CreateTexture(ddesc);
                using var b = rg.AddRasterRenderPass<PassData>("Suborned signals", out var pd, profilingSampler);
                RendererListHandle List(RenderQueueRange range, SortingCriteria sort)
                {
                    var draw = RenderingUtils.CreateDrawingSettings(Tags, rd, cd, ld, sort);
                    return rg.CreateRendererList(new RendererListParams(rd.cullResults, draw, new FilteringSettings(range, 1 << Layer)));
                }
                pd.opaque = List(RenderQueueRange.opaque, SortingCriteria.CommonOpaque);
                pd.transparent = List(RenderQueueRange.transparent, SortingCriteria.CommonTransparent);
                b.UseRendererList(pd.opaque); b.UseRendererList(pd.transparent);
                b.SetRenderAttachment(color, 0, AccessFlags.Write);
                b.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                b.SetRenderFunc(static (PassData d, RasterGraphContext c) => { c.cmd.DrawRendererList(d.opaque); c.cmd.DrawRendererList(d.transparent); });
            }
        }
    }
}
