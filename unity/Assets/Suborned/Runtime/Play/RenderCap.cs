using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Suborned.Play
{
    /// <summary>Brief 045 F7 (Redline: at Sky's native 6016 x 3384 the frame had no headroom at 60 Hz, 15.6 ms with 14.7 ms of GPU): the 3D pass is
    /// drawn at most about the reference machine's pixel count (the 15-inch MacBook Air, 2880 x 1864) and scaled up; the HUD (UI Toolkit) stays at
    /// the display's own resolution. On the Air and below the scale is 1 (nothing changes). Built player only: in the Editor the pipeline asset is a
    /// project file and is never written.</summary>
    public sealed class RenderCap : MonoBehaviour
    {
        /// <summary>The most pixels the 3D pass draws (the Air's native panel).</summary>
        public const float MaxPixels = 2880f * 1864f;
        /// <summary>The scale in use now (1 = native), for logs and checks.</summary>
        public static float Scale { get; private set; } = 1f;
        /// <summary>Brief 051: the player's render scale (Options, Display; 0.5 to 1) on top of the cap.</summary>
        public static float UserScale { get => user; set { value = Mathf.Clamp(value, 0.5f, 1f); if (Mathf.Abs(user - value) > 0.001f) { user = value; dirty = true; } } }
        static float user = 1f; static bool dirty;
        /// <summary>Brief 086 rows: a `-tplong` row's own filter (TpLongVariants), null = the rule below.</summary>
        public static UpscalingFilterSelection? FilterOverride { get => filterOverride; set { filterOverride = value; dirty = true; } }
        static UpscalingFilterSelection? filterOverride;
        /// <summary>Brief 106: the scale or filter rule changed (BuiltIns.SetStp): apply it on the next frame.</summary>
        public static void Refresh() => dirty = true;
        Vector2Int seen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            var go = new GameObject("render-cap"); DontDestroyOnLoad(go); go.AddComponent<RenderCap>();
        }

        void Update()
        {
            var now = new Vector2Int(Screen.width, Screen.height); if (now == seen && !dirty) return; seen = now; dirty = false;
            if (UniversalRenderPipeline.asset is not UniversalRenderPipelineAsset urp || now.x <= 0 || now.y <= 0) return;
            float s = Mathf.Clamp(Mathf.Sqrt(MaxPixels / ((float)now.x * now.y)), 0.5f, 1f) * user * BuiltIns.StpFactor;
            s = Mathf.Max(0.25f, Mathf.Round(s * 20f) / 20f); // steps of 0.05: a window dragged a pixel does not rescale the pass
            // brief 106: STP upscales when it is on and the pass is smaller than the screen; otherwise Automatic, the filter as built before 106
            var filter = filterOverride ?? (BuiltIns.Stp && s < 0.999f ? UpscalingFilterSelection.STP : UpscalingFilterSelection.Auto);
            if (Mathf.Abs(urp.renderScale - s) > 0.001f || urp.upscalingFilter != filter) { urp.renderScale = s; urp.upscalingFilter = filter; Debug.Log($"Play: 3D render scale {s:0.00}, filter {filter} at {now.x}x{now.y} (cap {MaxPixels / 1e6f:0.0} Mpx; HUD native)"); }
            Scale = s;
        }
    }
}
