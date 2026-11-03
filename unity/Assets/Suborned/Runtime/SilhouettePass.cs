using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Suborned
{
    /// <summary>Brief 056 F (Sky 2026-10-25: "the soft circle cut with silhouettes, as the one rule for every yaw"): people who matter are
    /// never lost behind a wall. The port note asked for a URP Render Objects feature on the people layer, depth Greater, drawn before the
    /// people's own pass; this does the same from code with no renderer-asset edit and without taking the people out of the opaque pass
    /// (which would take them out of the depth prepass and SSAO): after the opaques, (1) every person (rendering-layer bit
    /// <see cref="PersonLayer"/>) marks a stencil bit where it is the visible surface, then (2) the people who matter (bit
    /// <see cref="SilhouetteLayer"/>: the cell, hostiles, key people, set by the host on their figures) draw a cream rim and a flat fill, depth
    /// Greater, wherever no person is visible: so other people never take a silhouette, as in the lab. Enqueued by ArchitectureFade for its own
    /// camera only (the orthographic play camera), so the close shots, the review eyes and third person draw none.</summary>
    public sealed class SilhouettePass : ScriptableRenderPass
    {
        /// <summary>Rendering-layer bit on every person's renderers (the stencil mark).</summary>
        public const uint PersonLayer = 1u << 21;
        /// <summary>Rendering-layer bit on the renderers of the people who take a silhouette.</summary>
        public const uint SilhouetteLayer = 1u << 22;

        static readonly List<ShaderTagId> Tags = new() { new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly"), new ShaderTagId("SRPDefaultUnlit") };
        static readonly int FillId = Shader.PropertyToID("_Fill"), EdgeId = Shader.PropertyToID("_Edge"), EdgeMId = Shader.PropertyToID("_EdgeM"), MarginId = Shader.PropertyToID("_MarginM");
        readonly Material mat;

        public SilhouettePass(Material mat) { this.mat = mat; renderPassEvent = RenderPassEvent.AfterRenderingOpaques; profilingSampler = new ProfilingSampler("Suborned silhouettes"); }

        /// <summary>The look, per render (Glint tunes it on ArchitectureFade).</summary>
        public void Look(Color fill, Color edge, float edgeM, float marginM) { mat.SetColor(FillId, fill); mat.SetColor(EdgeId, edge); mat.SetFloat(EdgeMId, edgeM); mat.SetFloat(MarginId, marginM); }

        /// <summary>The override material, or null when the shader is not in the build (Resources/Shaders/Silhouette).</summary>
        public static Material MakeMaterial()
        {
            var sh = Resources.Load<Shader>("Shaders/Silhouette");
            return sh && sh.isSupported ? new Material(sh) { name = "silhouettes", hideFlags = HideFlags.HideAndDontSave } : null;
        }

        sealed class PassData { public RendererListHandle mark, edge, fill; }

        public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData)
        {
            var res = frameData.Get<UniversalResourceData>(); var rd = frameData.Get<UniversalRenderingData>();
            var cd = frameData.Get<UniversalCameraData>(); var ld = frameData.Get<UniversalLightData>();
            using var b = rg.AddRasterRenderPass<PassData>("Suborned silhouettes", out var pd, profilingSampler);
            RendererListHandle List(uint layer, int pass)
            {
                var draw = RenderingUtils.CreateDrawingSettings(Tags, rd, cd, ld, SortingCriteria.CommonOpaque);
                draw.overrideMaterial = mat; draw.overrideMaterialPassIndex = pass;
                var filter = new FilteringSettings(RenderQueueRange.opaque, ~0, layer);
                return rg.CreateRendererList(new RendererListParams(rd.cullResults, draw, filter));
            }
            pd.mark = List(PersonLayer, 0); pd.edge = List(SilhouetteLayer, 1); pd.fill = List(SilhouetteLayer, 2);
            b.UseRendererList(pd.mark); b.UseRendererList(pd.edge); b.UseRendererList(pd.fill);
            b.SetRenderAttachment(res.activeColorTexture, 0, AccessFlags.Write);
            b.SetRenderAttachmentDepth(res.activeDepthTexture, AccessFlags.ReadWrite); // depth read; the stencil mark is written
            b.SetRenderFunc(static (PassData d, RasterGraphContext c) => { c.cmd.DrawRendererList(d.mark); c.cmd.DrawRendererList(d.edge); c.cmd.DrawRendererList(d.fill); });
        }
    }
}
