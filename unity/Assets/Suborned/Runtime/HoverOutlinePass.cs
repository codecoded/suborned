using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Suborned
{
    /// <summary>Brief 101 (Sky's idea, 2026-11-09: "outline it a little ... helps with those items close together e.g. guard and door"; Glyph's look
    /// B, briefs/101/glyph/outline/notes.md): what a left click would act on wears a thin ink line with a fine cream edge. Built the way
    /// <see cref="SilhouettePass"/> is (from code, no renderer-asset edit): the host puts the thing's renderers on <see cref="LayerA"/> (the one
    /// it fades out of, on <see cref="LayerB"/>), and each frame something is outlined this pass, after post-processing and the signals and under
    /// the HUD, (1) draws them into a mask the size of their screen box (the camera's view cropped to the box, with their own depth), and while a
    /// silhouetted person is outlined every person's depth too, then (2) draws the line from the mask over that box only: ink #17302f 1.5 px at
    /// 92 %, then cream #f7f4ec 1 px at 85 %, outside the thing's edge, both × the target's height / 1080 (the same at every zoom). Where the
    /// scene stands nearer the camera than the thing by <see cref="CoverM"/> the line is hidden; for a silhouetted person only another person
    /// hides it (the silhouette shows through cover, and the line goes round it). Nothing is enqueued while nothing is outlined. Play camera only
    /// (the host asks for its own camera, in the map view); the close shots, the review eyes and third person draw none.</summary>
    public sealed class HoverOutlinePass : ScriptableRenderPass
    {
        /// <summary>Rendering-layer bits on the outlined thing's renderers (20 is the fade, 21 and 22 the silhouettes): the one fading in, the one fading out.</summary>
        public const uint LayerA = 1u << 23, LayerB = 1u << 24;
        /// <summary>Glyph's spec at 1080p: the ink and the cream line (px) and the box's margin round the thing (px); cover counts when nearer than the thing by this
        /// (m, the silhouettes' margin: a kerb at the feet is never cover); a person is the cover when the scene there is that person within this (m).</summary>
        public const float InkPx = 1.5f, CreamPx = 1f, BoxPx = 8f, CoverM = 0.35f, PersonM = 0.1f;
        /// <summary>The kit's ink #17302f and cream #f7f4ec (art bible: the HUD's own colours), at Glyph's alphas.</summary>
        public static readonly Color Ink = new(0x17 / 255f, 0x30 / 255f, 0x2f / 255f, 0.92f), Cream = new(0xf7 / 255f, 0xf4 / 255f, 0xec / 255f, 0.85f);

        /// <summary>One frame's outline: the things' union box in the camera's viewport (0..1 from the bottom left, before the line and margin), each
        /// thing's fade, its clip height (world y: below it the thing makes no line, a mast's post) and whether it is a silhouetted person.</summary>
        public struct Request { public Rect Box; public float AlphaA, AlphaB, ClipA, ClipB; public bool SilA, SilB; }

        /// <summary>The proof reads these: the last frame the line was drawn, and its box on the target (px from the bottom left).</summary>
        public static int DrawnFrame { get; private set; } = -10;
        public static RectInt DrawnBox { get; private set; } public static Vector2Int DrawnTarget { get; private set; }
        static readonly Dictionary<Vector2Int, RectInt> boxBySize = new();
        /// <summary>The proof reads it: the last box drawn on a target of this size (a capture resizes the window for its frame: its own box, not the play size's).</summary>
        public static bool DrawnBoxFor(int w, int h, out RectInt box) => boxBySize.TryGetValue(new Vector2Int(w, h), out box);

        static HoverOutlinePass pass; static bool warned;
        Request req; readonly Material mat; readonly MaterialPropertyBlock props = new();

        HoverOutlinePass(Material mat)
        {
            this.mat = mat; renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing + 1; // after the signals (SignalsFeature, same event): the line over a cone's fill, as the HUD would be
            profilingSampler = new ProfilingSampler("Suborned hover outline"); ConfigureInput(ScriptableRenderPassInput.Depth); // the scene's depth for the cover (SSAO asks for it too)
        }

        /// <summary>Draw this frame's outline on `cam` (from beginCameraRendering, before its culling). False when the shader is not in the build.</summary>
        public static bool Enqueue(Camera cam, in Request r)
        {
            if (pass == null)
            {
                var sh = Resources.Load<Shader>("Shaders/HoverOutline");
                if (!sh || !sh.isSupported) { if (!warned) Debug.LogWarning("HoverOutlinePass: no shader (Resources/Shaders/HoverOutline): no hover outline"); warned = true; return false; }
                pass = new HoverOutlinePass(new Material(sh) { name = "hover outline", hideFlags = HideFlags.HideAndDontSave });
            }
            pass.req = r; cam.GetUniversalAdditionalCameraData()?.scriptableRenderer?.EnqueuePass(pass); return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Forget() { pass = null; warned = false; DrawnFrame = -10; boxBySize.Clear(); } // Play Mode may start without a domain reload

        static readonly List<ShaderTagId> Tags = new() { new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly"), new ShaderTagId("SRPDefaultUnlit"), new ShaderTagId("SubornedOutline") };
        static readonly int ClipId = Shader.PropertyToID("_OutlineClip"), MaskId = Shader.PropertyToID("_OutlineMask"), DepthAId = Shader.PropertyToID("_OutlineDepthA"), DepthBId = Shader.PropertyToID("_OutlineDepthB"),
            PeopleId = Shader.PropertyToID("_OutlinePeople"), SceneId = Shader.PropertyToID("_OutlineScene"), BoxId = Shader.PropertyToID("_OutlineBox"), TargetId = Shader.PropertyToID("_OutlineTarget"),
            RowsId = Shader.PropertyToID("_OutlineRows"), AlphaId = Shader.PropertyToID("_OutlineAlpha"), WidthsId = Shader.PropertyToID("_OutlineWidths"), DepthsId = Shader.PropertyToID("_OutlineDepths"),
            InkId = Shader.PropertyToID("_OutlineInk"), CreamId = Shader.PropertyToID("_OutlineCream");

        sealed class MaskData { public RendererListHandle list; public TextureHandle target; public Matrix4x4 view, proj; public Vector4 clip; }
        sealed class EdgeData
        {
            public Material mat; public MaterialPropertyBlock props; public TextureHandle color, mask, depthA, depthB, people, scene;
            public Vector4 box, alpha, widths, depths; public Matrix4x4 view, camProj; public int w, h;
        }

        public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData)
        {
            var res = frameData.Get<UniversalResourceData>(); var rd = frameData.Get<UniversalRenderingData>();
            var cd = frameData.Get<UniversalCameraData>(); var ld = frameData.Get<UniversalLightData>();
            var color = res.activeColorTexture; var scene = res.cameraDepthTexture;
            if (!color.IsValid() || !scene.IsValid() || req.AlphaA <= 0f && req.AlphaB <= 0f) return;
            var cdesc = rg.GetTextureDesc(color); int W = cdesc.width, H = cdesc.height; float s = H / 1080f;
            float ink = InkPx * s, cream = CreamPx * s, reach = ink + cream + 1f, pad = reach + BoxPx * s;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(req.Box.xMin * W - pad), 0, W), x1 = Mathf.Clamp(Mathf.CeilToInt(req.Box.xMax * W + pad), 0, W);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(req.Box.yMin * H - pad), 0, H), y1 = Mathf.Clamp(Mathf.CeilToInt(req.Box.yMax * H + pad), 0, H);
            int w = x1 - x0, h = y1 - y0; if (w < 1 || h < 1) return;
            DrawnFrame = Time.frameCount; DrawnBox = new RectInt(x0, y0, w, h); DrawnTarget = new Vector2Int(W, H); boxBySize[DrawnTarget] = DrawnBox;

            // the camera's view cropped to the box: the mask's texels are the target's pixels there
            var view = cd.GetViewMatrix(); var proj = cd.GetProjectionMatrix(); float sx = W / (float)w, sy = H / (float)h, cx = (x0 + x1) / (float)W - 1f, cy = (y0 + y1) / (float)H - 1f;
            var crop = Matrix4x4.identity; crop.m00 = sx; crop.m03 = -sx * cx; crop.m11 = sy; crop.m13 = -sy * cy; var cropProj = crop * proj;
            var clip = new Vector4(req.ClipA, req.ClipB, 0f, 0f);
            var mask = rg.CreateTexture(new TextureDesc(w, h) { name = "_SubornedOutlineMask", format = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = true, clearColor = Color.clear });
            TextureHandle Depth(string n) => rg.CreateTexture(new TextureDesc(w, h) { name = n, format = GraphicsFormat.D32_SFloat, clearBuffer = true });
            RendererListHandle List(uint layer, int passIndex)
            {
                var draw = RenderingUtils.CreateDrawingSettings(Tags, rd, cd, ld, SortingCriteria.CommonOpaque);
                draw.overrideMaterial = mat; draw.overrideMaterialPassIndex = passIndex;
                return rg.CreateRendererList(new RendererListParams(rd.cullResults, draw, new FilteringSettings(RenderQueueRange.all, ~0, layer)));
            }
            void Mask(string name, TextureHandle colorTarget, TextureHandle depth, RendererListHandle list, bool load)
            {
                using var b = rg.AddRasterRenderPass<MaskData>(name, out var d, profilingSampler);
                d.list = list; d.target = depth; d.view = view; d.proj = cropProj; d.clip = clip;
                b.UseRendererList(list); b.AllowGlobalStateModification(true);
                if (colorTarget.IsValid()) b.SetRenderAttachment(colorTarget, 0, load ? AccessFlags.ReadWrite : AccessFlags.Write);
                b.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                b.SetRenderFunc(static (MaskData d, RasterGraphContext c) =>
                {
                    bool flip = c.GetTextureUVOrigin(d.target) == TextureUVOrigin.BottomLeft;
                    RenderingUtils.SetViewAndProjectionMatrices(c.cmd, d.view, GL.GetGPUProjectionMatrix(d.proj, flip), false);
                    c.cmd.SetGlobalVector(ClipId, d.clip); c.cmd.DrawRendererList(d.list);
                });
            }
            var depthA = Depth("_SubornedOutlineDepthA"); Mask("Suborned hover outline A", mask, depthA, List(LayerA, 0), false);
            bool hasB = req.AlphaB > 0f, hasPeople = req.SilA && req.AlphaA > 0f || req.SilB && hasB; // B: a thing fading out; people: a silhouetted person outlined
            var depthB = depthA; if (hasB) { depthB = Depth("_SubornedOutlineDepthB"); Mask("Suborned hover outline B", mask, depthB, List(LayerB, 1), true); }
            var people = depthA; if (hasPeople) { people = Depth("_SubornedOutlinePeople"); Mask("Suborned hover outline people", TextureHandle.nullHandle, people, List(SilhouettePass.PersonLayer, 2), false); }

            using var e = rg.AddRasterRenderPass<EdgeData>("Suborned hover outline", out var ed, profilingSampler);
            ed.mat = mat; ed.props = props; ed.color = color; ed.mask = mask; ed.depthA = depthA; ed.depthB = depthB; ed.people = people; ed.scene = scene; ed.w = W; ed.h = H;
            ed.box = new Vector4(x0, y0, w, h); ed.alpha = new Vector4(req.AlphaA, req.AlphaB, req.SilA ? 1f : 0f, req.SilB ? 1f : 0f);
            ed.widths = new Vector4(ink, cream, reach, 0f); ed.depths = new Vector4(cd.camera.nearClipPlane, cd.camera.farClipPlane, CoverM, PersonM); ed.view = view; ed.camProj = proj;
            e.UseTexture(mask); e.UseTexture(depthA); if (hasB) e.UseTexture(depthB); if (hasPeople) e.UseTexture(people); e.UseTexture(scene);
            e.SetRenderAttachment(color, 0, AccessFlags.ReadWrite); e.AllowGlobalStateModification(true);
            e.SetRenderFunc(static (EdgeData d, RasterGraphContext c) =>
            {
                bool flipColor = c.GetTextureUVOrigin(d.color) == TextureUVOrigin.BottomLeft;
                var p = d.props; p.Clear();
                p.SetTexture(MaskId, d.mask); p.SetTexture(DepthAId, d.depthA); p.SetTexture(DepthBId, d.depthB); p.SetTexture(PeopleId, d.people); p.SetTexture(SceneId, d.scene);
                p.SetVector(BoxId, d.box); p.SetVector(TargetId, new Vector4(d.w, d.h, SystemInfo.graphicsUVStartsAtTop && flipColor ? -1f : 1f, 0f));
                p.SetVector(RowsId, new Vector4(c.GetTextureUVOrigin(d.mask) == TextureUVOrigin.TopLeft ? 1f : 0f, c.GetTextureUVOrigin(d.scene) == TextureUVOrigin.TopLeft ? 1f : 0f, 0f, 0f));
                p.SetVector(AlphaId, d.alpha); p.SetVector(WidthsId, d.widths); p.SetVector(DepthsId, d.depths);
                p.SetVector(InkId, Lin(Ink)); p.SetVector(CreamId, Lin(Cream));
                c.cmd.DrawProcedural(Matrix4x4.identity, d.mat, 3, MeshTopology.Triangles, 6, 1, p);
                RenderingUtils.SetViewAndProjectionMatrices(c.cmd, d.view, GL.GetGPUProjectionMatrix(d.camProj, flipColor), false); // the camera's own again for anything after
            });
        }

        /// <summary>A kit colour (sRGB) as the target takes it: linear in a linear project, the alpha kept.</summary>
        static Vector4 Lin(Color c) { var l = QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c; return new Vector4(l.r, l.g, l.b, c.a); }
    }
}
