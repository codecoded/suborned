using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned
{
    /// <summary>Distance haze per camera tier (Glint, brief 048). LOOK ONLY: no sim, sight or replay reads it. Tiers (TierOf): Play = the orthographic
    /// play camera (zoom 14 at 38 degrees) and every overlay camera (signals, fire lines): FOG OFF, always, so the play area stays crisp and the signals
    /// draw as built (URP's vertex fog reads clip depth, which an orthographic projection breaks); Follow = a perspective camera whose far plane is at most
    /// `followFarMax` (third person, conversation and fail cutaways): the haze profile `follow` of Resources/Atmosphere/haze.json (linear: clear to
    /// 90 m, the haze colour at the 350 m far clip); Long = a perspective camera with a longer far plane (flybys, free fly): the old city's gentle
    /// exponential haze (`long`). While a perspective camera renders this swaps the haze in, and restores the scene's fog after (the scene's own fog
    /// stays what light.json set: linear 1.1 to 7.5 km, a thin horizon-ring fog).
    /// The fog COLOUR is haze.json `colour` (#ecd9b4, sand-cream) by day, the same value AtmosphereLens gives the sky horizon, so sky and city share
    /// one haze; at night the scene's own fog colour (night.haze, LightPhase) is kept. Turn the whole atmosphere off with AtmosphereSettings.Enabled
    /// (the scene's fog stays as built). Installs itself once the first scene has loaded; nothing to wire in a scene.</summary>
    public sealed class DistanceHaze : MonoBehaviour
    {
        public const string ResourcePath = "Atmosphere/haze";

        public enum Tier { Play, Follow, Long }

        /// <summary>mode: linear (start, end) | exp2 (farClip, reach) | exp (density).</summary>
        [Serializable] public sealed class Profile { public string mode; public float start; public float end; public float farClip; public float reach; public float density;
            /// <summary>Optional (Glint 2026-10-23, long tier): this profile's own haze colour (#rrggbb; empty = haze.json colour) and an HDR gain on it, a LINEAR multiplier (0 or 1 = none; 1 to 3). The old city's fog was pushed past 1 so the tonemap bleaches it toward cream and bloom catches it; the sky horizon takes the same value.</summary>
            public string colour; public float gain; }
        [Serializable] public sealed class Data { public string schema; public string colour; public Profile follow; public Profile @long; public float followFarMax; public bool orthographic; public bool overlays; }

        public static DistanceHaze Instance { get; private set; }
        /// <summary>The far clip (m) the follow haze is tuned to: a third-person camera set to this cuts where the haze is already complete (HostFollow.followFar must equal it).</summary>
        public static float FarClip => Instance ? Instance.followEnd : 0f;
        /// <summary>The haze colour (haze.json `colour`), the one value for fog and sky horizon by day.</summary>
        public static Color Colour => Instance ? Instance.colour : new Color(0.925f, 0.851f, 0.706f);

        /// <summary>The haze of a tier as a LINEAR colour (may be above 1: the long tier's gain): the fog colour and the sky horizon use this one value.</summary>
        public static Color LinearHaze(Tier t)
        {
            var c = Colour; float g = 1f;
            if (Instance && t == Tier.Long) { if (Instance.longColourSet) c = Instance.longColour; g = Instance.data.@long.gain > 1f ? Instance.data.@long.gain : 1f; }
            if (QualitySettings.activeColorSpace == ColorSpace.Linear) c = c.linear;
            return new Color(c.r * g, c.g * g, c.b * g, 1f);
        }
        static Color ToFog(Color lin) => QualitySettings.activeColorSpace == ColorSpace.Linear ? lin.gamma : lin;

        /// <summary>Which tier a camera is in. Orthographic and overlay cameras are Play whatever their far plane (the play camera's is 1683 m).</summary>
        public static Tier TierOf(Camera c)
        {
            if (c.orthographic) return Tier.Play;
            if (c.TryGetComponent<UniversalAdditionalCameraData>(out var ud) && ud.renderType == CameraRenderType.Overlay) return Tier.Play;
            float max = Instance ? Instance.data.followFarMax : 900f;
            return c.farClipPlane <= max ? Tier.Follow : Tier.Long;
        }

        /// <summary>True from the fall of night until day (LightPhase): the scene's own fog colour stays then.</summary>
        public static bool Night => Suborned.Play.LightPhase.Instance && Suborned.Play.LightPhase.Instance.Night;

        Data data;
        Color colour, longColour; bool longColourSet;
        float followEnd;
        float followDensity; // exp2 only

        struct Saved { public bool fog; public FogMode mode; public float density, start, end; public Color colour; }
        Saved saved; bool has;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Instance) return;
            var text = Resources.Load<TextAsset>(ResourcePath);
            if (!text) { Debug.LogError($"DistanceHaze: Resources/{ResourcePath}.json not found; the scene's fog stays as built"); return; }
            Data d;
            try { d = JsonUtility.FromJson<Data>(text.text); } catch (Exception e) { Debug.LogError($"DistanceHaze: haze.json: {e.Message}"); return; }
            string why = Check(d, out var col);
            if (why != null) { Debug.LogError($"DistanceHaze: haze.json rejected ({why}); the scene's fog stays as built"); return; }
            var go = new GameObject("distance haze");
            go.SetActive(false); // OnEnable runs on AddComponent: fill in the data first, then switch on
            DontDestroyOnLoad(go);
            var h = go.AddComponent<DistanceHaze>();
            h.data = d; h.colour = col;
            h.longColourSet = !string.IsNullOrEmpty(d.@long.colour) && ColorUtility.TryParseHtmlString(d.@long.colour, out h.longColour);
            h.followEnd = d.follow.mode == "linear" ? d.follow.end : d.follow.farClip;
            if (d.follow.mode == "exp2") h.followDensity = Mathf.Sqrt(-Mathf.Log(1f - d.follow.reach)) / d.follow.farClip;
            go.SetActive(true);
        }

        /// <summary>Why a haze.json is rejected, or null. Unknown schema or mode, or an out-of-range value, rejects the file.</summary>
        static string Check(Data d, out Color col)
        {
            col = default;
            if (d == null) return "empty";
            if (d.schema != "suborned.haze/2") return "schema suborned.haze/2 expected";
            if (!ColorUtility.TryParseHtmlString(d.colour, out col)) return "colour is not #rrggbb";
            if (d.followFarMax <= 0f) return "followFarMax > 0";
            string p = Check(d.follow, "follow"); if (p != null) return p;
            p = Check(d.@long, "long"); if (p != null) return p;
            if (!string.IsNullOrEmpty(d.@long.colour) && !ColorUtility.TryParseHtmlString(d.@long.colour, out _)) return "long.colour is not #rrggbb";
            if (d.@long.gain != 0f && (d.@long.gain < 1f || d.@long.gain > 3f)) return "long.gain 0 (none) or 1..3";
            return null;
        }

        static string Check(Profile p, string name)
        {
            if (p == null) return name + " missing";
            switch (p.mode)
            {
                case "linear": return p.start >= 0f && p.end > p.start + 1f ? null : name + ": linear needs 0 <= start < end";
                case "exp2": return p.farClip >= 50f && p.reach > 0f && p.reach < 1f ? null : name + ": exp2 needs farClip >= 50 and 0 < reach < 1";
                case "exp": return p.density > 0f && p.density < 0.05f ? null : name + ": exp needs 0 < density < 0.05";
                default: return name + ": mode linear | exp2 | exp";
            }
        }

        void OnEnable()
        {
            if (data == null) return; // added by hand without data: does nothing
            Instance = this;
            RenderPipelineManager.beginCameraRendering += Begin;
            RenderPipelineManager.endCameraRendering += End;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Begin;
            RenderPipelineManager.endCameraRendering -= End;
            if (has) Restore();
            if (Instance == this) Instance = null;
        }

        void Begin(ScriptableRenderContext _, Camera c)
        {
            if (c.cameraType != CameraType.Game) return; // the Scene view, previews and probe bakes keep the scene's fog
            if (!has) { saved = new Saved { fog = RenderSettings.fog, mode = RenderSettings.fogMode, density = RenderSettings.fogDensity, start = RenderSettings.fogStartDistance, end = RenderSettings.fogEndDistance, colour = RenderSettings.fogColor }; has = true; }
            else Restore(false); // a stacked camera starts from the scene's fog, not the base camera's
            bool overlay = c.TryGetComponent<UniversalAdditionalCameraData>(out var ud) && ud.renderType == CameraRenderType.Overlay;
            if ((c.orthographic && !data.orthographic) || (overlay && !data.overlays)) { RenderSettings.fog = false; return; }
            if (c.orthographic || !AtmosphereSettings.Enabled) return; // atmosphere off: the scene's fog, as built
            var p = c.farClipPlane <= data.followFarMax ? data.follow : data.@long;
            RenderSettings.fog = true;
            switch (p.mode)
            {
                case "linear": RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = p.start; RenderSettings.fogEndDistance = p.end; break;
                case "exp2": RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = followDensity; break;
                case "exp": RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = p.density; break;
            }
            if (!Night) RenderSettings.fogColor = ToFog(LinearHaze(c.farClipPlane <= data.followFarMax ? Tier.Follow : Tier.Long)); // by day, the haze colour (the long tier's own, with its HDR gain); at night the scene's own (night.haze)
        }

        void End(ScriptableRenderContext _, Camera c)
        {
            if (has) Restore(true);
        }

        void Restore(bool done)
        {
            RenderSettings.fog = saved.fog; RenderSettings.fogMode = saved.mode; RenderSettings.fogDensity = saved.density;
            RenderSettings.fogStartDistance = saved.start; RenderSettings.fogEndDistance = saved.end; RenderSettings.fogColor = saved.colour;
            if (done) has = false;
        }

        void Restore() => Restore(true);
    }
}
