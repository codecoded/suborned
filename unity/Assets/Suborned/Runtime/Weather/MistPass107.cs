using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Suborned.Play
{
    /// <summary>Brief 107 (Glint: "Haze at the play camera works only as low ground mist and a height haze, not as distance fog"): one full-screen pass of
    /// our own on the Render Graph (Resources/Shaders/Mist107). It reads the scene depth, finds each pixel's world position (the orthographic play camera
    /// too) and lays mist by its height over the plan's level below it (street 0, deck 9.43, concourse 24) and by drifting world-space noise, plus a faint
    /// height haze; under 15 % above knee height (Glint: the 80 px people keep their feet), thinned to nothing within a few metres of the focus. It runs
    /// before transparents, so the rings, the outlines and the signals (after post) all draw over it. Amount 0 (as built, every clear day) enqueues nothing.
    /// Enqueued from beginCameraRendering on the main camera only (play and follow views); presentation cameras keep their own haze (DistanceHaze).</summary>
    public sealed class MistPass107 : ScriptableRenderPass
    {
        public static float Amount; public static Color Colour = Color.white; public static Vector3 Focus;
        static MistPass107 pass; static bool hooked, warned;
        static readonly int ParamsId = Shader.PropertyToID("_MistParams"), ColourId = Shader.PropertyToID("_MistColour"), FocusId = Shader.PropertyToID("_MistFocus");
        readonly Material mat;

        MistPass107(Material m) { mat = m; renderPassEvent = RenderPassEvent.BeforeRenderingTransparents; profilingSampler = new ProfilingSampler("Suborned mist 107"); ConfigureInput(ScriptableRenderPassInput.Depth); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook() { if (hooked) return; hooked = true; RenderPipelineManager.beginCameraRendering += Begin; }

        static void Begin(ScriptableRenderContext _, Camera cam)
        {
            if (Amount <= 0.001f || cam != Camera.main || cam.cameraType != CameraType.Game) return;
            if (pass == null)
            {
                var sh = Resources.Load<Shader>("Shaders/Mist107");
                if (!sh || !sh.isSupported) { if (!warned) Debug.LogError("MistPass107: no shader (Resources/Shaders/Mist107): no ground mist"); warned = true; return; }
                pass = new MistPass107(new Material(sh) { name = "mist 107", hideFlags = HideFlags.HideAndDontSave });
            }
            cam.GetUniversalAdditionalCameraData()?.scriptableRenderer?.EnqueuePass(pass);
        }

        sealed class Data { public Material mat; public TextureHandle depth; public Vector4 p, c, f; }

        public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData)
        {
            var res = frameData.Get<UniversalResourceData>();
            if (!res.cameraDepthTexture.IsValid() || !res.activeColorTexture.IsValid()) return;
            using var b = rg.AddRasterRenderPass<Data>("Suborned mist 107", out var d, profilingSampler);
            d.mat = mat; d.depth = res.cameraDepthTexture;
            var col = QualitySettings.activeColorSpace == ColorSpace.Linear ? Colour.linear : Colour;
            d.p = new Vector4(Amount, Time.unscaledTime, 0.6f, 0.15f); d.c = new Vector4(col.r, col.g, col.b, 1f); d.f = new Vector4(Focus.x, Focus.y, Focus.z, 0f);
            b.UseTexture(d.depth); b.SetRenderAttachment(res.activeColorTexture, 0, AccessFlags.ReadWrite); b.AllowGlobalStateModification(true);
            b.SetRenderFunc(static (Data d, RasterGraphContext c) =>
            {
                c.cmd.SetGlobalTexture("_MistDepth", d.depth);
                c.cmd.SetGlobalVector(ParamsId, d.p); c.cmd.SetGlobalVector(ColourId, d.c); c.cmd.SetGlobalVector(FocusId, d.f);
                c.cmd.DrawProcedural(Matrix4x4.identity, d.mat, 0, MeshTopology.Triangles, 3, 1);
            });
        }
    }
}
