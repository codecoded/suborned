using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned
{
    /// <summary>Settings for the old-city atmosphere (Glint, brief 048). A settings/debug toggle, NOT a game control. LOOK ONLY.
    /// Sky colour: cobalt (the scene's own sky, today's default) or the old city's sea-green teal, so Sky can compare in game:
    /// Shift+F9 flips it (remembered in PlayerPrefs), or start with `-sky teal` / `-sky cobalt`. Atmosphere on or off (everything this component and
    /// DistanceHaze do; off = the scene as built): Shift+F10, or `-atmosphere off`; not remembered. Scripts may set the properties.</summary>
    public static class AtmosphereSettings
    {
        const string SkyPref = "suborned.sky.teal";
        static bool? teal, enabled;

        /// <summary>The sky palette: false cobalt (default), true the old teal.</summary>
        public static bool SkyTeal
        {
            get => teal ??= Arg("-sky") is { } s ? s == "teal" : PlayerPrefs.GetInt(SkyPref, 0) == 1;
            set { if (teal == value) return; teal = value; PlayerPrefs.SetInt(SkyPref, value ? 1 : 0); PlayerPrefs.Save(); Debug.Log($"atmosphere: sky {(value ? "teal (old city)" : "cobalt")}"); }
        }

        /// <summary>The whole atmosphere (haze, lens, sky horizon, grade): false shows the scene as built.</summary>
        public static bool Enabled
        {
            get => enabled ??= Arg("-atmosphere") != "off";
            set { if (enabled == value) return; enabled = value; Debug.Log($"atmosphere: {(value ? "on" : "off (scene as built)")}"); }
        }

        /// <summary>One line for a debug panel.</summary>
        public static string Status => $"atmosphere {(Enabled ? "on" : "off")}, sky {(SkyTeal ? "teal" : "cobalt")} (Shift+F9 sky, Shift+F10 on/off)";

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1].ToLowerInvariant();
            return null;
        }
    }

    /// <summary>The old city's atmosphere, by camera tier (DistanceHaze.TierOf), from Resources/Atmosphere/lens.json (brief 048; look only, nothing a rule,
    /// sight or replay reads). No scene edits: it installs itself after the first scene loads, builds its own global Volumes, sun flare and sky inputs,
    /// and switches them per camera in beginCameraRendering (the hook DistanceHaze uses; URP fires it before it updates the volume stack, so a Volume
    /// weight set here applies to that same camera).
    ///   Play (orthographic play camera, overlays): the GRADE only. Saturation +16 percent over the scene grade's own and a small warm shadow lift
    ///     (a Volume of ColorAdjustments + LiftGammaGain, both folded into the grading LUT: no per-pixel cost). No haze, no blur, no flare; signals
    ///     draw on their overlay camera outside the grade, as built.
    ///   Follow (third person, cutaways): warm bloom (threshold 0.86), the sky's horizon haze band with the sun aureole and the haze colour of
    ///     haze.json, the sun flare at 0.6; fog by DistanceHaze (linear 90 to 350 m).
    ///   Long (flyby, free fly): the old look whole: bloom 0.8 / 0.6 / 0.85, Gaussian far blur 300 to 1800 m, the flare at 1.0, the exponential haze.
    /// Night (LightPhase.Night) and AtmosphereSettings.Enabled == false: nothing here applies. The sky inputs are shader globals read by
    /// Suborned/SkyGradient (zero = the sky as built); the far band (a baked strip of the horizon cards) is read from Resources when it exists.</summary>
    public sealed class AtmosphereLens : MonoBehaviour
    {
        public const string ResourcePath = "Atmosphere/lens";

        // ---- data (suborned.lens/1) ----
        [Serializable] public sealed class Bloomp { public float threshold, intensity, scatter; public string tint; }
        [Serializable] public sealed class Dofp { public bool enabled; public float start, end, maxRadius; public bool highQuality; }
        [Serializable] public sealed class Skyp { public float hazeHeight, aureole, aureolePower, hazeLift, broad, broadPower; }
        [Serializable] public sealed class Palp { public string zenith, mid; public float hazeHeight; } // hazeHeight: the teal sky's own band (Glint 2026-10-23); 0 = sky.hazeHeight
        [Serializable] public sealed class FlareEl
        {
            public string type; public float position, scale, intensity, rotation, fallOff, roundness, spread; public string tint;
            public int sides, count; public string distribution;
        }
        [Serializable] public sealed class Flarep { public float follow, @long, occlusionRadius; public int samples; public string starburst; public FlareEl[] elements; }
        [Serializable] public sealed class Gradep { public float saturationPercent, liftOffset, liftWarmth; public string liftTint; public float fallbackSaturation; public Greensp greens; }
        /// <summary>Greens protection (Glint 2026-10-23): a hue-vs-hue shift of hueDeg on hues fromDeg..toDeg, full between fullFromDeg and fullToDeg, eased to 0 at the ends; hueDeg 0 = none.</summary>
        [Serializable] public sealed class Greensp { public float hueDeg, fromDeg, fullFromDeg, fullToDeg, toDeg; }
        [Serializable] public sealed class Bandp { public string resource; public float minDeg, maxDeg, gain, opacity; }
        [Serializable] public sealed class Lens
        {
            public string schema; public Skyp sky; public Palp teal; public Bloomp bloomFollow, bloomLong; public Dofp dofLong;
            public Flarep flare; public Gradep gradePlay; public Bandp farBand;
        }

        public static AtmosphereLens Instance { get; private set; }

        Lens data;
        Color tealZenith, tealMid;
        Volume vPlay, vFollow, vLong;
        ColorAdjustments playSat; LiftGammaGain playLift; ColorCurves playCurves, followCurves, longCurves; DepthOfField longDof;
        /// <summary>The long tier's far blur on or off (TpLongRun times the long view both ways, Glint 2026-10-23); the data's dofLong.enabled at start.</summary>
        public static bool LongDof { get => Instance && Instance.longDof && Instance.longDof.active; set { if (Instance && Instance.longDof) Instance.longDof.active = value; } }
        float baseSaturation = -999f;
        LensFlareComponentSRP flare; LensFlareDataSRP flareData; Texture flareStarburst;
        Texture2D farBand; bool farBandTried;
        readonly HashSet<Camera> forcedPost = new();

        static readonly int idHaze = Shader.PropertyToID("_AtmHaze"), idZenith = Shader.PropertyToID("_AtmSkyZenith"), idMid = Shader.PropertyToID("_AtmSkyMid"),
            idShape = Shader.PropertyToID("_AtmSkyShape"), idExtra = Shader.PropertyToID("_AtmSkyExtra"), idBand = Shader.PropertyToID("_AtmFarBand"), idBandRange = Shader.PropertyToID("_AtmFarBandRange");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Instance) return;
            var text = Resources.Load<TextAsset>(ResourcePath);
            if (!text) { Debug.LogError($"AtmosphereLens: Resources/{ResourcePath}.json not found; no lens, grade or sky change"); return; }
            Lens d;
            try { d = JsonUtility.FromJson<Lens>(text.text); } catch (Exception e) { Debug.LogError($"AtmosphereLens: lens.json: {e.Message}"); return; }
            string why = Check(d);
            if (why != null) { Debug.LogError($"AtmosphereLens: lens.json rejected ({why}); no lens, grade or sky change"); return; }
            var go = new GameObject("atmosphere lens");
            go.SetActive(false); // OnEnable runs on AddComponent: fill in the data first, then switch on
            DontDestroyOnLoad(go);
            var a = go.AddComponent<AtmosphereLens>();
            a.data = d;
            a.Build();
            go.SetActive(true);
        }

        static string Check(Lens d)
        {
            if (d == null) return "empty";
            if (d.schema != "suborned.lens/1") return "schema suborned.lens/1 expected";
            if (d.sky == null || d.sky.hazeHeight <= 0f || d.sky.hazeHeight > 1f) return "sky.hazeHeight in (0, 1]";
            if (d.teal == null || !ColorUtility.TryParseHtmlString(d.teal.zenith, out _) || !ColorUtility.TryParseHtmlString(d.teal.mid, out _)) return "teal.zenith / teal.mid are not #rrggbb";
            foreach (var b in new[] { d.bloomFollow, d.bloomLong })
                if (b == null || b.threshold < 0f || b.intensity < 0f || b.scatter < 0f || b.scatter > 1f || !ColorUtility.TryParseHtmlString(b.tint, out _)) return "bloom: threshold >= 0, intensity >= 0, scatter 0..1, tint #rrggbb";
            if (d.dofLong == null || (d.dofLong.enabled && (d.dofLong.start < 0f || d.dofLong.end <= d.dofLong.start || d.dofLong.maxRadius <= 0f || d.dofLong.maxRadius > 5f))) return "dofLong: 0 <= start < end, 0 < maxRadius <= 5";
            if (d.flare == null || d.flare.follow < 0f || d.flare.@long < 0f || d.flare.samples < 1 || d.flare.samples > 128 || d.flare.elements == null) return "flare: follow >= 0, long >= 0, 1 <= samples <= 128, elements";
            foreach (var e in d.flare.elements)
                if (e == null || (e.type != "image" && e.type != "circle" && e.type != "polygon") || e.scale <= 0f || e.intensity < 0f || !ColorUtility.TryParseHtmlString(e.tint, out _)) return "flare element: type image | circle | polygon, scale > 0, intensity >= 0, tint #rrggbb";
            if (d.gradePlay == null || d.gradePlay.saturationPercent < -50f || d.gradePlay.saturationPercent > 60f || d.gradePlay.liftOffset < 0f || d.gradePlay.liftOffset > 0.1f
                || d.gradePlay.liftWarmth < 0f || d.gradePlay.liftWarmth > 1f || !ColorUtility.TryParseHtmlString(d.gradePlay.liftTint, out _)) return "gradePlay: saturationPercent -50..60, liftOffset 0..0.1, liftWarmth 0..1, liftTint #rrggbb";
            if (d.teal.hazeHeight < 0f || d.teal.hazeHeight > 1f) return "teal.hazeHeight in [0, 1] (0 = sky.hazeHeight)";
            var gg = d.gradePlay.greens;
            if (gg != null && Mathf.Abs(gg.hueDeg) > 0.01f && (Mathf.Abs(gg.hueDeg) > 30f || !(0f <= gg.fromDeg && gg.fromDeg < gg.fullFromDeg && gg.fullFromDeg <= gg.fullToDeg && gg.fullToDeg < gg.toDeg && gg.toDeg <= 360f)))
                return "gradePlay.greens: |hueDeg| <= 30, 0 <= fromDeg < fullFromDeg <= fullToDeg < toDeg <= 360";
            if (d.farBand == null || string.IsNullOrEmpty(d.farBand.resource) || d.farBand.maxDeg <= d.farBand.minDeg) return "farBand: resource, minDeg < maxDeg";
            return null;
        }

        static Color Hex(string s) { ColorUtility.TryParseHtmlString(s, out var c); return c; }
        /// <summary>A shader global is raw: material colours are converted to linear by Unity, so a colour given as a vector is converted here.</summary>
        static Color Lin(Color c) => QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;

        void Build()
        {
            tealZenith = Hex(data.teal.zenith); tealMid = Hex(data.teal.mid);
            // three global Volumes above the scene grade (priority 100), all at weight 0 until a camera of their tier renders. Layer Default: every camera in the
            // scene has volumeLayerMask Default (checked in HarbourDistrict.unity: 23 of 23).
            vPlay = NewVolume("lens/play", out var pp);
            playSat = pp.Add<ColorAdjustments>(false); playLift = pp.Add<LiftGammaGain>(false); playCurves = pp.Add<ColorCurves>(false);
            vFollow = NewVolume("lens/follow", out var pf);
            Bloom(pf, data.bloomFollow); followCurves = pf.Add<ColorCurves>(false); // Glint 2026-10-23 (optional): the greens protection on the follow and long tiers too
            vLong = NewVolume("lens/long", out var pl);
            Bloom(pl, data.bloomLong); longCurves = pl.Add<ColorCurves>(false);
            if (data.dofLong.enabled)
            {
                var dof = longDof = pl.Add<DepthOfField>(false);
                dof.mode.Override(DepthOfFieldMode.Gaussian);
                dof.gaussianStart.Override(data.dofLong.start); dof.gaussianEnd.Override(data.dofLong.end);
                dof.gaussianMaxRadius.Override(data.dofLong.maxRadius); dof.highQualitySampling.Override(data.dofLong.highQuality);
            }
            ApplyPlayGrade();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => { baseSaturation = -999f; };
        }

        Volume NewVolume(string name, out VolumeProfile profile)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var v = go.AddComponent<Volume>();
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = name;
            v.isGlobal = true; v.priority = 100f; v.weight = 0f; v.sharedProfile = profile;
            return v;
        }

        static void Bloom(VolumeProfile p, Bloomp b)
        {
            var bl = p.Add<Bloom>(false);
            bl.threshold.Override(b.threshold); bl.intensity.Override(b.intensity); bl.scatter.Override(b.scatter); bl.tint.Override(Hex(b.tint));
        }

        /// <summary>The play grade: saturation = the scene grade's own x (1 + saturationPercent / 100), and a warm shadow lift (linear, before the
        /// tonemap: c x (1 - liftOffset) + liftOffset, so white stays white, plus the warm hue of liftTint at `liftWarmth`). The scene grade's own
        /// saturation is read from the day grade volume when it can be found.</summary>
        void ApplyPlayGrade()
        {
            var g = data.gradePlay;
            float s0 = BaseSaturation();
            float mult = (1f + s0 / 100f) * (1f + g.saturationPercent / 100f);
            playSat.saturation.Override(Mathf.Clamp((mult - 1f) * 100f, -100f, 100f));
            var tint = Color.Lerp(Color.white, Hex(g.liftTint), g.liftWarmth);
            playLift.lift.Override(new Vector4(tint.r, tint.g, tint.b, g.liftOffset));
            playLift.gain.Override(new Vector4(1f, 1f, 1f, -g.liftOffset)); // white stays white: c x (1 - L) + L raises the shadows and leaves the highlights
            // greens protection (Glint 2026-10-23): the play grade's saturation and lift had turned palm and lawn greens toward olive; a hue-vs-hue
            // curve nudges them back toward blue-green. URP's curve: x = input hue 0..1, y = 0.5 + shift (turns); in the grading LUT, no per-pixel cost
            var gr = g.greens;
            if (gr != null && Mathf.Abs(gr.hueDeg) > 0.01f)
            {
                float y = 0.5f + gr.hueDeg / 360f;
                var keys = new[] { new Keyframe(gr.fromDeg / 360f, 0.5f, 0f, 0f), new Keyframe(gr.fullFromDeg / 360f, y, 0f, 0f), new Keyframe(gr.fullToDeg / 360f, y, 0f, 0f), new Keyframe(gr.toDeg / 360f, 0.5f, 0f, 0f) };
                foreach (var cc in new[] { playCurves, followCurves, longCurves }) if (cc) cc.hueVsHue.Override(new TextureCurve(keys, 0.5f, true, new Vector2(0f, 1f)));
            }
            baseSaturation = s0;
        }

        float BaseSaturation()
        {
            var ph = Suborned.Play.LightPhase.Instance;
            var vol = ph ? ph.day.grade : null;
            if (!vol)
                foreach (var v in FindObjectsByType<Volume>())
                    if (v && v.isGlobal && v.sharedProfile && v.transform.parent != transform && v.sharedProfile.TryGet<ColorAdjustments>(out _)) { vol = v; break; }
            if (vol && vol.sharedProfile && vol.sharedProfile.TryGet<ColorAdjustments>(out var ca) && ca.saturation.overrideState) return ca.saturation.value;
            return data.gradePlay.fallbackSaturation;
        }

        void OnEnable()
        {
            if (data == null) return;
            Instance = this;
            RenderPipelineManager.beginCameraRendering += Begin;
            RenderPipelineManager.endCameraRendering += End;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Begin;
            RenderPipelineManager.endCameraRendering -= End;
            Reset(true);
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !(kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)) return;
            if (kb.f9Key.wasPressedThisFrame) AtmosphereSettings.SkyTeal = !AtmosphereSettings.SkyTeal;
            if (kb.f10Key.wasPressedThisFrame) AtmosphereSettings.Enabled = !AtmosphereSettings.Enabled;
        }

        void Begin(ScriptableRenderContext _, Camera c)
        {
            if (c.cameraType != CameraType.Game) return;
            var tier = DistanceHaze.TierOf(c);
            bool on = AtmosphereSettings.Enabled && !DistanceHaze.Night;
            bool hasUd = c.TryGetComponent<UniversalAdditionalCameraData>(out var ud);
            bool overlay = hasUd && ud.renderType == CameraRenderType.Overlay;
            if (on && tier == DistanceHaze.Tier.Long && !overlay && forcedPost.Add(c))
            {   // a flyby or free-fly camera made without post-processing gets it (from the next frame; the old look is the lens): the camera's own HDR stays
                if (!hasUd) { ud = c.gameObject.AddComponent<UniversalAdditionalCameraData>(); hasUd = true; }
                ud.renderPostProcessing = true; c.allowHDR = true;
            }
            bool post = on && hasUd && ud.renderPostProcessing && !overlay;
            if (baseSaturation < -990f && on) ApplyPlayGrade();
            vPlay.weight = post && tier == DistanceHaze.Tier.Play ? 1f : 0f;
            vFollow.weight = post && tier == DistanceHaze.Tier.Follow ? 1f : 0f;
            vLong.weight = post && tier == DistanceHaze.Tier.Long ? 1f : 0f;
            float fl = !post ? 0f : tier == DistanceHaze.Tier.Follow ? data.flare.follow : tier == DistanceHaze.Tier.Long ? data.flare.@long : 0f;
            if (fl > 0f) EnsureFlare();
            if (flare) flare.intensity = fl;
            Sky(on && tier != DistanceHaze.Tier.Play && !overlay);
        }

        void End(ScriptableRenderContext _, Camera c)
        {
            if (c.cameraType != CameraType.Game) return;
            Reset(false);
        }

        /// <summary>Volumes and flare back to neutral, so a camera that is not ours (the Scene view, a probe) renders as built. Both are read while the
        /// camera records, which is before this runs. The sky globals are set explicitly by every game camera's Begin (the last in a stack is the
        /// signals overlay: zero), so they are only cleared when the component goes.</summary>
        void Reset(bool sky)
        {
            if (vPlay) vPlay.weight = 0f; if (vFollow) vFollow.weight = 0f; if (vLong) vLong.weight = 0f;
            if (flare) flare.intensity = 0f;
            if (sky) Sky(false);
        }

        // ---- sky: shader globals read by Suborned/SkyGradient (all zero = the sky as built) ----
        void Sky(bool on)
        {
            if (!on)
            {
                Shader.SetGlobalVector(idHaze, Vector4.zero); Shader.SetGlobalVector(idZenith, Vector4.zero); Shader.SetGlobalVector(idMid, Vector4.zero);
                Shader.SetGlobalVector(idShape, Vector4.zero); Shader.SetGlobalVector(idExtra, Vector4.zero); Shader.SetGlobalVector(idBandRange, Vector4.zero);
                return;
            }
            var s = data.sky;
            var h = Lin(DistanceHaze.Colour); // the sky's horizon and below-horizon colour: the fog's colour
            Shader.SetGlobalVector(idHaze, new Vector4(h.r, h.g, h.b, 1f));
            if (AtmosphereSettings.SkyTeal)
            {
                var z = Lin(tealZenith); var m = Lin(tealMid);
                Shader.SetGlobalVector(idZenith, new Vector4(z.r, z.g, z.b, 1f));
                Shader.SetGlobalVector(idMid, new Vector4(m.r, m.g, m.b, 1f));
            }
            else { Shader.SetGlobalVector(idZenith, Vector4.zero); Shader.SetGlobalVector(idMid, Vector4.zero); } // cobalt: the scene sky material's own
            float hh = AtmosphereSettings.SkyTeal && data.teal.hazeHeight > 0f ? data.teal.hazeHeight : s.hazeHeight; // the teal sky's gentler band into cream; cobalt keeps sky.hazeHeight
            Shader.SetGlobalVector(idShape, new Vector4(hh, s.aureole, s.aureolePower, s.hazeLift));
            Shader.SetGlobalVector(idExtra, new Vector4(s.broad, s.broadPower, 0f, 0f));
            FarBand();
        }

        /// <summary>The baked far band (the horizon cards and ring rendered once into an equirect strip at build time) when Resources holds it; none yet means nothing is drawn.</summary>
        void FarBand()
        {
            var b = data.farBand;
            if (!farBandTried) { farBandTried = true; farBand = Resources.Load<Texture2D>(b.resource); if (!farBand) Debug.Log($"AtmosphereLens: no far band at Resources/{b.resource} yet (the build step is Latch's); the sky shows haze only past the far clip"); }
            if (!farBand) { Shader.SetGlobalVector(idBandRange, Vector4.zero); return; }
            Shader.SetGlobalTexture(idBand, farBand);
            Shader.SetGlobalVector(idBandRange, new Vector4(b.minDeg * Mathf.Deg2Rad, b.maxDeg * Mathf.Deg2Rad, b.gain, b.opacity));
        }

        // ---- sun flare: the old DK_SunFlare, built from data on the scene's sun ----
        void EnsureFlare()
        {
            if (flare) return;
            var sun = RenderSettings.sun;
            if (!sun) foreach (var l in FindObjectsByType<Light>()) if (l && l.type == LightType.Directional && l.enabled) { sun = l; break; }
            if (!sun) return;
            if (!flareData) flareData = BuildFlare();
            flare = sun.TryGetComponent<LensFlareComponentSRP>(out var have) ? have : sun.gameObject.AddComponent<LensFlareComponentSRP>();
            flare.lensFlareData = flareData; flare.intensity = 0f; flare.scale = 1f;
            flare.attenuationByLightShape = false;
            flare.useOcclusion = true; flare.occlusionRadius = data.flare.occlusionRadius; flare.sampleCount = (uint)data.flare.samples; // towers cut the flare as they pass the sun
            flare.allowOffScreen = false;
        }

        LensFlareDataSRP BuildFlare()
        {
            if (!flareStarburst && !string.IsNullOrEmpty(data.flare.starburst)) flareStarburst = Resources.Load<Texture2D>(data.flare.starburst);
            var d = ScriptableObject.CreateInstance<LensFlareDataSRP>(); d.name = "atmosphere sun flare";
            var list = new List<LensFlareDataElementSRP>();
            foreach (var e in data.flare.elements)
            {
                if (e.type == "image" && !flareStarburst) continue; // no starburst texture: the glow and ghosts still draw
                var el = new LensFlareDataElementSRP
                {
                    flareType = e.type == "image" ? SRPLensFlareType.Image : e.type == "polygon" ? SRPLensFlareType.Polygon : SRPLensFlareType.Circle,
                    position = e.position, uniformScale = e.scale, localIntensity = e.intensity, tint = Hex(e.tint),
                    blendMode = SRPLensFlareBlendMode.Additive, modulateByLightColor = true,
                };
                if (e.type == "image") { el.lensFlareTexture = flareStarburst; el.rotation = e.rotation; }
                else el.fallOff = e.fallOff;
                if (e.type == "polygon")
                {
                    el.sideCount = e.sides; el.sdfRoundness = e.roundness;
                    if (e.count > 1) { el.allowMultipleElement = true; el.count = e.count; el.distribution = SRPLensFlareDistribution.Uniform; el.lengthSpread = e.spread; }
                }
                list.Add(el);
            }
            d.elements = list.ToArray();
            return d;
        }
    }
}
