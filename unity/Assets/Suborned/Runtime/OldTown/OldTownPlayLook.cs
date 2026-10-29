using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.OldTown
{
    /// <summary>Brief 075 swap, second build (Latch; Sky 2026-10-29, approved for the play camera only): the old-built town's play camera keeps the
    /// old materials but renders with our play look, and the presentation cameras keep the old full look. While this camera renders (and only then):
    /// - fog off (the old town's exponential haze and the old renderer's "Harbour fog" pass);
    /// - the old grade, bloom, soft focus (depth of field) volumes at weight 0 and our play grade volume at 1 (it sits at 0 for every other camera);
    /// - the sun flare (LensFlareComponentSRP) off;
    /// - the baked crowd and cars (layer CrowdLayer, culled by this camera's mask) not drawn: the sim's own people stand there;
    /// - MSAA off (the camera's allowMSAA, set once);
    /// - our play light: the sun's heading, colour, intensity and shadow strength and the ambient (trilight colours and probe) of our pipeline's
    ///   scene, which our play grade is exposed for (the old stage's sun is higher and brighter and its ambient the baked old sky: under our
    ///   grade it burns out, and its short shadows leave the square flat).
    /// Shadows: our TacticalShadows on the same camera fits the shadow distance and cascades to the play view and turns SSAO off at ortho.
    /// Everything is put back at the end of the camera's render, so no asset or setting is left changed.</summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class OldTownPlayLook : MonoBehaviour
    {
        /// <summary>The layer the swap puts the old town's baked crowd and traffic on (unnamed in the TagManager; the play camera's culling mask leaves it out).</summary>
        public const int CrowdLayer = 29;
        public Volume playVolume;
        public Volume[] presentationVolumes = new Volume[0];
        public Behaviour[] presentationOnly = new Behaviour[0];
        public string[] featuresOff = { "Harbour fog" };
        public bool fogOff = true;
        [Header("Our play light (lightSwap)")]
        public bool lightSwap;
        public Light sun;
        public Quaternion sunRotation = Quaternion.identity;
        public Color sunColour = Color.white;
        public float sunIntensity = 1f, sunShadowStrength = 1f;
        public AmbientMode ambientMode = AmbientMode.Trilight;
        public Color ambientSky, ambientEquator, ambientGround;
        public float ambientIntensity = 1f;
        [Tooltip("Our scene's ambient probe (SphericalHarmonicsL2 as 27 floats, [rgb][coefficient]); empty = Unity's own from the trilight colours.")]
        public float[] ambientProbe = new float[0];
        [Header("Perf A/B (brief 075, PerfSwitches oldLens)")]
        [Tooltip("Off as built: the play view has no soft focus or sun flare. On: the old lens comes back in the play view (the presentation volumes' depth of field, copied into a volume of its own at weight 1 while this camera renders, and the sun flares left on), so the Air A/B measures what the cut saves.")]
        public bool presentationLens;

        Camera cam;
        Quaternion sunRot0; Color sunCol0; float sunInt0, sunShadow0;
        AmbientMode amb0; Color sky0, eq0, gr0; float ambInt0; SphericalHarmonicsL2 probe0;
        bool fog; float playWeight; readonly List<float> weights = new(); readonly List<bool> enabledWas = new();
        readonly List<(ScriptableRendererFeature f, bool was)> features = new();
        bool applied;
        Volume lensVolume; float lensWeight;

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            cam.allowMSAA = false;
            cam.cullingMask &= ~(1 << CrowdLayer);
            RenderPipelineManager.beginCameraRendering += Begin;
            RenderPipelineManager.endCameraRendering += End;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Begin;
            RenderPipelineManager.endCameraRendering -= End;
            Restore();
            if (lensVolume) { var p = lensVolume.sharedProfile; if (Application.isPlaying) { Destroy(lensVolume.gameObject); Destroy(p); } else { DestroyImmediate(lensVolume.gameObject); DestroyImmediate(p); } lensVolume = null; }
        }

        void Begin(ScriptableRenderContext _, Camera c)
        {
            if (c != cam || applied) return;
            applied = true;
            fog = RenderSettings.fog; if (fogOff) RenderSettings.fog = false;
            weights.Clear(); foreach (var v in presentationVolumes) { weights.Add(v ? v.weight : 0f); if (v) v.weight = 0f; }
            if (playVolume) { playWeight = playVolume.weight; playVolume.weight = 1f; }
            enabledWas.Clear(); foreach (var b in presentationOnly) { enabledWas.Add(b && b.enabled); if (b && !presentationLens) b.enabled = false; }
            if (presentationLens && Lens() is { } lv) { lensWeight = lv.weight; lv.weight = 1f; }
            if (lightSwap)
            {
                if (sun) { sunRot0 = sun.transform.rotation; sunCol0 = sun.color; sunInt0 = sun.intensity; sunShadow0 = sun.shadowStrength; sun.transform.rotation = sunRotation; sun.color = sunColour; sun.intensity = sunIntensity; sun.shadowStrength = sunShadowStrength; }
                amb0 = RenderSettings.ambientMode; sky0 = RenderSettings.ambientSkyColor; eq0 = RenderSettings.ambientEquatorColor; gr0 = RenderSettings.ambientGroundColor; ambInt0 = RenderSettings.ambientIntensity; probe0 = RenderSettings.ambientProbe;
                RenderSettings.ambientMode = ambientMode; RenderSettings.ambientSkyColor = ambientSky; RenderSettings.ambientEquatorColor = ambientEquator; RenderSettings.ambientGroundColor = ambientGround; RenderSettings.ambientIntensity = ambientIntensity;
                if (ambientProbe != null && ambientProbe.Length == 27) { var sh = new SphericalHarmonicsL2(); for (int ch = 0; ch < 3; ch++) for (int k = 0; k < 9; k++) sh[ch, k] = ambientProbe[ch * 9 + k]; RenderSettings.ambientProbe = sh; }
            }
            features.Clear();
            if (UniversalRenderPipeline.asset is { } urp && urp.rendererDataList.Length > 0 && urp.rendererDataList[0])
                foreach (var f in urp.rendererDataList[0].rendererFeatures)
                    if (f && f.isActive && System.Array.IndexOf(featuresOff, f.name) >= 0) { features.Add((f, true)); f.SetActive(false); }
        }

        void End(ScriptableRenderContext _, Camera c) { if (c == cam) Restore(); }

        void Restore()
        {
            if (!applied) return;
            applied = false;
            RenderSettings.fog = fog;
            for (int i = 0; i < presentationVolumes.Length && i < weights.Count; i++) if (presentationVolumes[i]) presentationVolumes[i].weight = weights[i];
            if (playVolume) playVolume.weight = playWeight;
            if (lensVolume) lensVolume.weight = lensWeight;
            for (int i = 0; i < presentationOnly.Length && i < enabledWas.Count; i++) if (presentationOnly[i]) presentationOnly[i].enabled = enabledWas[i];
            foreach (var (f, was) in features) if (f) f.SetActive(was);
            features.Clear();
            if (lightSwap)
            {
                if (sun) { sun.transform.rotation = sunRot0; sun.color = sunCol0; sun.intensity = sunInt0; sun.shadowStrength = sunShadow0; }
                RenderSettings.ambientMode = amb0; RenderSettings.ambientSkyColor = sky0; RenderSettings.ambientEquatorColor = eq0; RenderSettings.ambientGroundColor = gr0; RenderSettings.ambientIntensity = ambInt0;
                RenderSettings.ambientProbe = probe0;
            }
        }

        /// <summary>presentationLens: the presentation volumes' depth of field copied once into a global volume of its own (priority above the play
        /// grade, weight 0 except while this camera renders with presentationLens on; never saved). Null when no presentation volume has one.</summary>
        Volume Lens()
        {
            if (lensVolume) return lensVolume;
            DepthOfField src = null;
            foreach (var v in presentationVolumes)
                if (v && v.sharedProfile && v.sharedProfile.TryGet<DepthOfField>(out var d) && d.active && d.mode.value != DepthOfFieldMode.Off) { src = d; break; }
            if (src == null) return null;
            var go = new GameObject("Play lens (perf A/B: the old depth of field in the play view)") { hideFlags = HideFlags.DontSave };
            lensVolume = go.AddComponent<Volume>(); lensVolume.isGlobal = true; lensVolume.priority = (playVolume ? playVolume.priority : 100f) + 1f; lensVolume.weight = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.hideFlags = HideFlags.DontSave;
            var dof = profile.Add<DepthOfField>(true);
            for (int i = 0; i < dof.parameters.Count && i < src.parameters.Count; i++) dof.parameters[i].SetValue(src.parameters[i]);
            dof.active = true;
            lensVolume.sharedProfile = profile;
            return lensVolume;
        }

        /// <summary>A SphericalHarmonicsL2 as 27 floats ([rgb][coefficient]).</summary>
        public static float[] Flatten(SphericalHarmonicsL2 sh) { var o = new float[27]; for (int c = 0; c < 3; c++) for (int k = 0; k < 9; k++) o[c * 9 + k] = sh[c, k]; return o; }
    }
}
