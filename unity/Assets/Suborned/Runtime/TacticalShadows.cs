using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned
{
    /// <summary>Main-light shadows for the tactical camera (GDD §10: orthographic, play zoom 14 at 38°; brief 007 round 3).
    /// The camera stands back far enough to clear every roof in its frame column, so the street lies `focusDepth` m along the
    /// view (about 85 m at the civic square, 420 m under SpaceHQ). One global shadow distance either stops short of the street
    /// or spreads one cascade over hundreds of metres of empty air. While this camera renders, the URP asset's shadow distance
    /// and cascades are fitted to it: the last cascade covers the street band [focusDepth - nearBand, focusDepth + farBand]
    /// (pavement, people, and roofs up to about nearBand·sin(pitch) m), the others split the air above it where tall roofs in
    /// frame sit. Everything is restored after the camera renders, so other cameras keep the asset's values and the asset
    /// is never dirtied. A camera rig that pans or zooms sets focusDepth (and the bands) when it moves.</summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class TacticalShadows : MonoBehaviour
    {
        [Tooltip("Distance (m) along the view from the camera to the focus on the street datum.")]
        public float focusDepth = 100f;
        [Tooltip("The last cascade starts this many metres in front of the focus.")]
        public float nearBand = 60f;
        [Tooltip("Shadows reach this many metres past the focus (the frame's far ground edge, lower levels).")]
        public float farBand = 40f;
        [Tooltip("Cascade count (1-4).")]
        public int cascades = 4;
        [Tooltip("Shares of the air in front of the street band at which cascades 1 and 2 end (4 cascades).")]
        public Vector2 airSplits = new(0.3f, 0.65f);
        [Tooltip("Metres over which shadows fade out at the shadow distance. URP fades by distance from the camera from (1 - border) of the " +
            "shadow distance, so a share would fade the street itself at 400 m; this keeps the fade past the frame's far ground edge.")]
        public float fade = 8f;
        [Header("Perspective (the third-person follow view; brief 048, Glint's sign-off)")]
        [Tooltip("Shadow distance (m) for a perspective camera: 100 to 120 m (80 showed a hard line; 120 if far-tower shadows are lost).")]
        public float perspectiveDistance = 110f;
        [Tooltip("Where the first of two cascades ends (m): about 20 to 25 m, the near street sharp.")]
        public float perspectiveSplit = 22f;
        [Tooltip("Share of the shadow distance the last cascade fades over (about 30 %).")]
        public float perspectiveFade = 0.3f;
        [Header("SSAO (brief 064 A3 item 1)")]
        [Tooltip("SSAO while this camera renders orthographic (the play view). Off: the camera stands about 160 m back and the street lies 142 to " +
            "178 m along its view, past the SSAO falloff (100 m), so URP's ShouldSkipAO gives no occlusion there and the pass only costs. The " +
            "perspective views (ground, follow) keep the renderer's SSAO. The perf harness turns it back on for its `ssao` A/B (PerfSwitches).")]
        public bool orthoSsao = false;

        Camera cam;
        State saved;
        SsaoScope ssaoOff;

        /// <summary>Shadow distance this camera renders with.</summary>
        public float Distance => focusDepth + farBand;

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += Begin;
            RenderPipelineManager.endCameraRendering += End;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Begin;
            RenderPipelineManager.endCameraRendering -= End;
            saved?.Restore();
            saved = null;
            ssaoOff?.Dispose(); ssaoOff = null;
        }

        void Begin(ScriptableRenderContext _, Camera c)
        {
            if (c != cam || UniversalRenderPipeline.asset is not { } urp) return;
            saved = new State(urp);
            Apply(urp);
            ssaoOff?.Dispose(); ssaoOff = SsaoScope.For(cam, orthoSsao);
        }

        void End(ScriptableRenderContext _, Camera c)
        {
            if (c != cam) return;
            saved?.Restore();
            saved = null;
            ssaoOff?.Dispose(); ssaoOff = null;
        }

        /// <summary>Applies this camera's shadow fit to the asset until the returned scope is disposed (for renders that do not
        /// raise the pipeline's camera events, or before the component has been enabled).</summary>
        public IDisposable Use()
        {
            if (UniversalRenderPipeline.asset is not { } urp) return null;
            var s = new State(urp) { ssao = SsaoScope.For(cam ? cam : GetComponent<Camera>(), orthoSsao) };
            Apply(urp);
            return s;
        }

        /// <summary>The renderer's SSAO feature (PC_Renderer), or null.</summary>
        public static ScriptableRendererFeature SsaoFeature
        {
            get
            {
                if (UniversalRenderPipeline.asset is not { } urp) return null;
                if (ssaoCache.asset == urp && ssaoCache.feature) return ssaoCache.feature;
                ScriptableRendererFeature found = null;
                var list = urp.rendererDataList;
                if (list.Length > 0 && list[0]) foreach (var f in list[0].rendererFeatures) if (f is ScreenSpaceAmbientOcclusion) { found = f; break; }
                ssaoCache = (urp, found);
                return found;
            }
        }
        static (UniversalRenderPipelineAsset asset, ScriptableRendererFeature feature) ssaoCache;

        /// <summary>Brief 064 A3 item 1: SSAO off while an orthographic camera renders, unless asked for; Dispose puts the feature back as it
        /// was (a launch's -nossao, the perf switch's own setting).</summary>
        sealed class SsaoScope : IDisposable
        {
            ScriptableRendererFeature f; readonly bool was;
            SsaoScope(ScriptableRendererFeature f) { this.f = f; was = f.isActive; f.SetActive(false); }
            public static SsaoScope For(Camera c, bool keep) => c && c.orthographic && !keep && SsaoFeature is { isActive: true } f ? new SsaoScope(f) : null;
            public void Dispose() { if (f) f.SetActive(was); f = null; }
        }

        void Apply(UniversalRenderPipelineAsset urp)
        {
            if (cam && !cam.orthographic) { FitPerspective(urp, perspectiveDistance, perspectiveSplit, perspectiveFade); return; } // brief 048: the follow view's own fit, not the asset's 260 m single cascade
            Fit(urp, focusDepth, nearBand, farBand, cascades, airSplits, fade);
        }

        /// <summary>Brief 048 (Redline's plan item 2, Glint's conditions): a perspective camera's shadows over `distance` m in two cascades, the
        /// first ending at `split` m, the last `fadeShare` of the distance faded (casters 3,446 to about 1,000 in tp-worst).</summary>
        public static void FitPerspective(UniversalRenderPipelineAsset urp, float distance, float split, float fadeShare)
        {
            float dist = Mathf.Max(distance, 1f);
            urp.shadowDistance = dist; urp.shadowCascadeCount = 2;
            urp.cascade2Split = Mathf.Clamp(split / dist, 0.05f, 0.95f);
            urp.cascadeBorder = Mathf.Clamp(fadeShare, 0.0002f, 1f);
        }

        /// <summary>Fits the asset's shadow distance and cascades to a camera whose street focus lies focusDepth m along its view
        /// (see the class note). Callers that render without this component (the flyby's play stop) use it with this camera's bands
        /// and restore the asset themselves (Save).</summary>
        public static void Fit(UniversalRenderPipelineAsset urp, float focusDepth, float nearBand, float farBand, int cascades, Vector2 airSplits, float fade)
        {
            float dist = Mathf.Max(focusDepth + farBand, 1f);
            float last = Mathf.Clamp((focusDepth - nearBand) / dist, 0.05f, 0.95f); // where the street band's cascade starts
            int n = Mathf.Clamp(cascades, 1, 4);
            urp.shadowDistance = dist;
            urp.shadowCascadeCount = n;
            if (n == 2) urp.cascade2Split = last;
            else if (n == 3) urp.cascade3Split = new Vector2(last * airSplits.y, last);
            else if (n == 4) urp.cascade4Split = new Vector3(last * airSplits.x, last * airSplits.y, last);
            urp.cascadeBorder = Mathf.Clamp(fade / dist, 0.0002f, 1f); // URP: fade from (1 - border) · distance to distance
        }

        /// <summary>A snapshot of the asset's shadow distance and cascades; Dispose restores it.</summary>
        public static IDisposable Save(UniversalRenderPipelineAsset urp) => new State(urp);

        sealed class State : IDisposable
        {
            readonly UniversalRenderPipelineAsset urp;
            readonly float distance, split2, border;
            readonly int count;
            readonly Vector2 split3;
            readonly Vector3 split4;
            public IDisposable ssao;

            public State(UniversalRenderPipelineAsset a)
            {
                urp = a; distance = a.shadowDistance; count = a.shadowCascadeCount;
                split2 = a.cascade2Split; split3 = a.cascade3Split; split4 = a.cascade4Split; border = a.cascadeBorder;
            }

            public void Restore()
            {
                if (!urp) return;
                urp.shadowDistance = distance; urp.shadowCascadeCount = count;
                urp.cascade2Split = split2; urp.cascade3Split = split3; urp.cascade4Split = split4; urp.cascadeBorder = border;
            }

            public void Dispose() { Restore(); ssao?.Dispose(); ssao = null; }
        }
    }
}
