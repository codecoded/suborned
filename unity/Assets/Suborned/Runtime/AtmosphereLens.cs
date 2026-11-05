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
    public sealed partial class AtmosphereLens : MonoBehaviour
    {
        public const string ResourcePath = "Atmosphere/lens";

        // ---- data (suborned.lens/1) ----
        [Serializable] public sealed class Bloomp { public float threshold, intensity, scatter, clamp; public string tint; }
        [Serializable] public sealed class Dofp { public bool enabled; public float start, end, maxRadius; public bool highQuality; }
        [Serializable] public sealed class Skyp { public float hazeHeight, hazeHeightLong, aureole, aureolePower, hazeLift, broad, broadPower, peach; } // peach (Glint 2026-10-24, optional, 0 = none): a warm peach cast on the blue-to-haze blend, so the band between sky and horizon does not pass through grey
        [Serializable] public sealed class Palp { public string zenith, mid; public float hazeHeight; } // hazeHeight: the teal sky's own band (Glint 2026-10-23); 0 = sky.hazeHeight
        [Serializable] public sealed class FlareEl
        {
            public string type; public float position, scale, intensity, rotation, fallOff, roundness, spread; public string tint;
            public int sides, count; public string distribution;
        }
        [Serializable] public sealed class Flarep { public float follow, @long, occlusionRadius; public int samples; public string starburst; public FlareEl[] elements; }
        [Serializable] public sealed class Gradep { public float saturationPercent, liftOffset, liftWarmth; public string liftTint; public float fallbackSaturation; public Greensp greens; public float exposureDelta; }
        /// <summary>Greens protection (Glint 2026-10-23): a hue-vs-hue shift of hueDeg on hues fromDeg..toDeg, full between fullFromDeg and fullToDeg, eased to 0 at the ends; hueDeg 0 = none.</summary>
        [Serializable] public sealed class Greensp { public float hueDeg, fromDeg, fullFromDeg, fullToDeg, toDeg; }
        /// <summary>Glint 2026-10-24 (Glyph's review of 0.2.99): the sun's SHADOW STRENGTH by day (1 = a shadow takes the whole sun out; below 1 the sun still fills the shade in its own warm colour, so shaded cream stays cream and shaded enamel stays powder blue; the sunlit side does not change). 0 or absent = the scene's own.</summary>
        [Serializable] public sealed class Sunp { public float shadowStrength; }
        /// <summary>Glint 2026-10-24: the long tier's exposure as a DELTA in EV on the scene grade's own post exposure (0 = none): the plaza and the pale roofs blow out in the flyby.</summary>
        [Serializable] public sealed class Longp { public float exposureDelta, saturationDelta, contrastDelta; } // saturation / contrast deltas: brief 075 golden view (Glint 2026-10-28), on the scene grade's own; 0 = none
        [Serializable] public sealed class Bandp { public string resource; public float minDeg, maxDeg, gain, opacity; }
        // ---- the evening phase (Glint 2026-10-24; LightPhase.SetEvening). All optional: a lens.json with no `evening` leaves SetEvening as it was (the day look with the dusk lights) ----
        /// <summary>The evening sun: a low warm sun with long shadows. pitch/yaw are the light's own Euler angles (as light.json sun: pitch 38 yaw 330 by day); shadowStrength is the Light's, replacing sun.shadowStrength.</summary>
        [Serializable] public sealed class EveSun { public string colour; public float intensity, pitch, yaw, shadowStrength; }
        /// <summary>An ambient colour as the builder makes it: the hex with its HSV value scaled (light.json ambient.sky.value).</summary>
        [Serializable] public sealed class EveAmb { public string hex; public float value; }
        [Serializable] public sealed class EveAmbient { public EveAmb sky, equator, ground; }
        [Serializable] public sealed class EveReflection { public float intensity, probeScale; }
        /// <summary>The sky at dusk, perspective tiers: zenith and mid colours, the sun glow colour, the haze band and the aureole.</summary>
        [Serializable] public sealed class EveSky { public string zenith, mid, sunGlow; public float hazeHeight, aureole, aureolePower, hazeLift, peach; }
        /// <summary>The grade between day and night, ADDED to the tier's own grade by k (0 = day, 1 = evening). exposure EV and contrast/saturation are deltas on the scene grade; filter, shadows, highlights are multipliers on the scene's own; temperature, tint on the scene's 0; vignette is the absolute intensity.</summary>
        [Serializable] public sealed class EveGrade { public float exposure, contrast, saturation, temperature, tint, vignette; public string filter, shadows, highlights; }
        [Serializable] public sealed class Evening { public float easeSeconds; public EveSun sun; public EveAmbient ambient; public EveReflection reflection; public EveSky sky; public EveGrade grade; }
        /// <summary>The night floor (Glint 2026-10-24, Glyph's 0.2.113 check: night walls crush to #020100). Look only, applied at runtime on the night State the scene was saved with: a higher teal-indigo ambient (LightPhase), the moon's shadow strength below 1 so the moon fills the shade, and a lift in the night grade (offset in linear before the tonemap, tinted). No lights.</summary>
        [Serializable] public sealed class NightFloor
        {
            public float shadowStrength, liftOffset, liftTintShare; public string liftTint; public EveAmbient ambient;
            /// <summary>Glyph's 0.2.115 check: the moon's own intensity and colour at night (0 / empty = the scene's), a stop up so ground, furniture and roofs separate from each other.</summary>
            public float moonIntensity; public string moonColour;
            /// <summary>A night lamp whose hue is between fromDeg and toDeg (the lilac table lamp: violet is the rivals' signal colour) is turned to toHueDeg (a flamingo), keeping its saturation and value; 0 = off.</summary>
            public float violetFromDeg, violetToDeg, violetToHueDeg;
            /// <summary>Glyph's 0.2.116 check: a night lamp group (by its object's name prefix, e.g. "interior" for the café's three pendants) at a share of its built intensity. The pendants are inside the café: seen through the cutaway one shows as a hot spot on the checker floor with no fixture over it.</summary>
            public LampScale[] lampScales;
        }
        [Serializable] public sealed class LampScale { public string prefix; public float scale; }
        [Serializable] public sealed class Lens
        {
            public string schema; public Skyp sky; public Palp teal; public Bloomp bloomFollow, bloomLong, bloomPlay; public Dofp dofLong;
            public Flarep flare; public Gradep gradePlay; public Bandp farBand; public Sunp sun; public Longp gradeLong; public Evening evening; public NightFloor night;
        }

        /// <summary>The evening phase's data (lens.json `evening`), or null when the file has none or was rejected: LightPhase reads it for the sun, the ambient and the reflections.</summary>
        public static Evening EveningData { get; private set; }
        /// <summary>The night floor's data (lens.json `night`), or null: LightPhase reads the ambient from it.</summary>
        public static NightFloor NightData { get; private set; }
        /// <summary>A hex colour with its HSV value scaled, as the builder makes the ambient colours (Look.Palette).</summary>
        public static Color Scaled(string hex, float value) { var c = HexC(hex); Color.RGBToHSV(c, out var h, out var s, out var v); return Color.HSVToRGB(h, s, Mathf.Clamp01(v * value)); }

        public static AtmosphereLens Instance { get; private set; }

        Lens data;
        Color tealZenith, tealMid;
        Volume vPlay, vFollow, vLong;
        ColorAdjustments playSat, longAdj; LiftGammaGain playLift; ColorCurves playCurves, followCurves, longCurves; DepthOfField longDof;
        /// <summary>The long tier's far blur on or off (TpLongRun times the long view both ways, Glint 2026-10-23); the data's dofLong.enabled at start.</summary>
        public static bool LongDof { get => Instance && Instance.longDof && Instance.longDof.active; set { if (Instance && Instance.longDof) Instance.longDof.active = value; } }
        float baseSaturation = -999f;
        /// <summary>The evening grade's parts on one tier's volume (a tier volume holds the tier's own grade; the evening is pushed onto it by k, so a tier is never stacked on another).</summary>
        sealed class Rig { public ColorAdjustments adj; public WhiteBalance wb; public ShadowsMidtonesHighlights smh; public Vignette vig; }
        Rig rigPlay, rigFollow, rigLong;
        Volume vNight; LiftGammaGain nightLift;
        float duskK = -1f, playSatValue, longExposureDelta, longSatDelta, longConDelta;
        Light sunLight; float sunStrength0 = -1f; int sunSearched;
        LensFlareComponentSRP flare; LensFlareDataSRP flareData; Texture flareStarburst;
        Texture2D farBand; bool farBandTried;
        readonly HashSet<Camera> forcedPost = new();

        static readonly int idNight = Shader.PropertyToID("_AtmNight");
        static readonly int idHaze = Shader.PropertyToID("_AtmHaze"), idZenith = Shader.PropertyToID("_AtmSkyZenith"), idMid = Shader.PropertyToID("_AtmSkyMid"),
            idShape = Shader.PropertyToID("_AtmSkyShape"), idExtra = Shader.PropertyToID("_AtmSkyExtra"), idBand = Shader.PropertyToID("_AtmFarBand"), idBandRange = Shader.PropertyToID("_AtmFarBandRange"),
            idSunDir = Shader.PropertyToID("_AtmSunDir"), idSunCol = Shader.PropertyToID("_AtmSunCol");
        Color dayZenith, dayMid, daySunGlow; bool skyRead;

        /// <summary>The scene sky material's own zenith, mid and sun glow (linear), read once from the day skybox (the evening blends from them).</summary>
        void SceneSky()
        {
            if (skyRead) return;
            var m = RenderSettings.skybox; if (!m || !m.HasProperty("_Zenith") || !m.HasProperty("_Mid")) return;
            var glow = m.HasProperty("_SunColour") ? "_SunColour" : m.HasProperty("_SunColor") ? "_SunColor" : null; // _SunColor: the ported old sky, DecoKit/Space Age Sky (brief 075-A)
            if (glow == null) return;
            dayZenith = Lin(m.GetColor("_Zenith")); dayMid = Lin(m.GetColor("_Mid")); daySunGlow = Lin(m.GetColor(glow)); skyRead = true;
        }

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
            EveningData = d.evening; NightData = d.night;
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
            if (d.sky.hazeHeightLong < 0f || d.sky.hazeHeightLong > 1f) return "sky.hazeHeightLong in [0, 1] (0 = sky.hazeHeight)";
            if (d.teal == null || !ColorUtility.TryParseHtmlString(d.teal.zenith, out _) || !ColorUtility.TryParseHtmlString(d.teal.mid, out _)) return "teal.zenith / teal.mid are not #rrggbb";
            if (HasBloom(d.bloomPlay) && (d.bloomPlay.threshold < 0f || d.bloomPlay.scatter < 0f || d.bloomPlay.scatter > 1f || !ColorUtility.TryParseHtmlString(d.bloomPlay.tint, out _))) return "bloomPlay: threshold >= 0, intensity > 0, scatter 0..1, tint #rrggbb (or absent: the scene grade's bloom)";
            if (d.gradePlay != null && Mathf.Abs(d.gradePlay.exposureDelta) > 1.5f) return "gradePlay.exposureDelta within +-1.5 EV";
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
            if (d.sky.peach < 0f || d.sky.peach > 2f) return "sky.peach in [0, 2] (0 = none)";
            if (d.sun != null && d.sun.shadowStrength != 0f && (d.sun.shadowStrength < 0.2f || d.sun.shadowStrength > 1f)) return "sun.shadowStrength 0 (the scene's own) or 0.2..1";
            if (d.gradeLong != null && (Mathf.Abs(d.gradeLong.exposureDelta) > 1.5f || Mathf.Abs(d.gradeLong.saturationDelta) > 60f || Mathf.Abs(d.gradeLong.contrastDelta) > 40f)) return "gradeLong: exposureDelta within +-1.5 EV, saturationDelta within 60, contrastDelta within 40";
            var n = d.night;
            if (n != null)
            {
                if (n.shadowStrength < 0.2f || n.shadowStrength > 1f || n.liftOffset < 0f || n.liftOffset > 0.1f || n.liftTintShare < 0f || n.liftTintShare > 1f || !ColorUtility.TryParseHtmlString(n.liftTint, out _)) return "night: shadowStrength 0.2..1, liftOffset 0..0.1, liftTintShare 0..1, liftTint #rrggbb";
                if (n.moonIntensity < 0f || n.moonIntensity > 3f || (!string.IsNullOrEmpty(n.moonColour) && !ColorUtility.TryParseHtmlString(n.moonColour, out _))) return "night: moonIntensity 0..3 (0 = the scene's), moonColour #rrggbb";
                if (n.violetToHueDeg != 0f && !(0f <= n.violetFromDeg && n.violetFromDeg < n.violetToDeg && n.violetToDeg <= 360f && n.violetToHueDeg >= 0f && n.violetToHueDeg <= 360f)) return "night: 0 <= violetFromDeg < violetToDeg <= 360, violetToHueDeg 0..360";
                if (n.lampScales != null) foreach (var ls in n.lampScales) if (ls == null || string.IsNullOrEmpty(ls.prefix) || ls.scale < 0f || ls.scale > 2f) return "night.lampScales: prefix, scale 0..2";
                if (n.ambient == null || n.ambient.sky == null || n.ambient.equator == null || n.ambient.ground == null) return "night.ambient: sky, equator, ground";
                foreach (var a in new[] { n.ambient.sky, n.ambient.equator, n.ambient.ground }) if (!ColorUtility.TryParseHtmlString(a.hex, out _) || a.value < 0f || a.value > 1f) return "night.ambient: hex #rrggbb, value 0..1";
            }
            return CheckEvening(d.evening);
        }

        static string CheckEvening(Evening e)
        {
            if (e == null) return null; // optional
            bool Ok(string h) => ColorUtility.TryParseHtmlString(h, out _);
            if (e.easeSeconds < 0f || e.easeSeconds > 10f) return "evening.easeSeconds 0..10";
            if (e.sun == null || !Ok(e.sun.colour) || e.sun.intensity < 0f || e.sun.intensity > 8f || e.sun.pitch < 3f || e.sun.pitch > 80f || e.sun.yaw < 0f || e.sun.yaw > 360f || e.sun.shadowStrength < 0.2f || e.sun.shadowStrength > 1f)
                return "evening.sun: colour #rrggbb, intensity 0..8, pitch 3..80, yaw 0..360, shadowStrength 0.2..1";
            if (e.ambient == null || e.ambient.sky == null || e.ambient.equator == null || e.ambient.ground == null) return "evening.ambient: sky, equator, ground";
            foreach (var a in new[] { e.ambient.sky, e.ambient.equator, e.ambient.ground }) if (!Ok(a.hex) || a.value < 0f || a.value > 2f) return "evening.ambient: hex #rrggbb, value 0..2";
            if (e.reflection == null || e.reflection.intensity < 0f || e.reflection.intensity > 2f || e.reflection.probeScale < 0f || e.reflection.probeScale > 2f) return "evening.reflection: intensity 0..2, probeScale 0..2";
            var s = e.sky;
            if (s == null || !Ok(s.zenith) || !Ok(s.mid) || !Ok(s.sunGlow) || s.hazeHeight <= 0f || s.hazeHeight > 1f || s.aureole < 0f || s.aureolePower < 1f || s.hazeLift < 0f || s.peach < 0f || s.peach > 2f)
                return "evening.sky: zenith, mid, sunGlow #rrggbb, hazeHeight (0, 1], aureole >= 0, aureolePower >= 1, hazeLift >= 0, peach 0..2";
            var g = e.grade;
            if (g == null || !Ok(g.filter) || !Ok(g.shadows) || !Ok(g.highlights) || Mathf.Abs(g.exposure) > 1.5f || Mathf.Abs(g.contrast) > 40f || Mathf.Abs(g.saturation) > 40f || Mathf.Abs(g.temperature) > 40f || Mathf.Abs(g.tint) > 40f || g.vignette < 0f || g.vignette > 0.6f)
                return "evening.grade: filter, shadows, highlights #rrggbb, exposure within 1.5 EV, contrast/saturation/temperature/tint within 40, vignette 0..0.6";
            return null;
        }

        static Color Hex(string s) { ColorUtility.TryParseHtmlString(s, out var c); return c; }
        /// <summary>Redline 064 #6: the evening's colours parsed once per string, not every frame at dusk and night.</summary>
        static readonly System.Collections.Generic.Dictionary<string, Color> hexSeen = new();
        /// <summary>A #rrggbb colour parsed once per string (the evening code asks every frame it eases).</summary>
        public static Color Colour(string s) => HexC(s);
        static Color HexC(string s) { if (s == null) return default; if (!hexSeen.TryGetValue(s, out var c)) hexSeen[s] = c = Hex(s); return c; }
        /// <summary>A shader global is raw: material colours are converted to linear by Unity, so a colour given as a vector is converted here.</summary>
        static Color Lin(Color c) => QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;

        void Build()
        {
            tealZenith = Hex(data.teal.zenith); tealMid = Hex(data.teal.mid);
            // three global Volumes above the scene grade (priority 100), all at weight 0 until a camera of their tier renders. Layer Default: every camera in the
            // scene has volumeLayerMask Default (checked in HarbourDistrict.unity: 23 of 23).
            vPlay = NewVolume("lens/play", out var pp);
            playSat = pp.Add<ColorAdjustments>(false); playLift = pp.Add<LiftGammaGain>(false); playCurves = pp.Add<ColorCurves>(false);
            if (HasBloom(data.bloomPlay)) Bloom(pp, data.bloomPlay); // brief 075-A: the play view's own bloom (the old reel's whole bloom veils a frame of lit stone)
            vFollow = NewVolume("lens/follow", out var pf);
            Bloom(pf, data.bloomFollow); followCurves = pf.Add<ColorCurves>(false); // Glint 2026-10-23 (optional): the greens protection on the follow and long tiers too
            vLong = NewVolume("lens/long", out var pl);
            Bloom(pl, data.bloomLong); longCurves = pl.Add<ColorCurves>(false);
            longExposureDelta = data.gradeLong != null ? data.gradeLong.exposureDelta : 0f;
            longSatDelta = data.gradeLong != null ? data.gradeLong.saturationDelta : 0f; longConDelta = data.gradeLong != null ? data.gradeLong.contrastDelta : 0f;
            if (LongGrade) longAdj = pl.Add<ColorAdjustments>(false);
            if (data.evening != null)
            {   // the evening phase's grade parts, on each tier's own volume (inert until LightPhase.Dusk > 0: no override is set)
                rigPlay = MakeRig(pp, playSat); rigFollow = MakeRig(pf, null); rigLong = MakeRig(pl, longAdj); longAdj = rigLong.adj;
            }
            if (data.dofLong.enabled)
            {
                var dof = longDof = pl.Add<DepthOfField>(false);
                dof.mode.Override(DepthOfFieldMode.Gaussian);
                dof.gaussianStart.Override(data.dofLong.start); dof.gaussianEnd.Override(data.dofLong.end);
                dof.gaussianMaxRadius.Override(data.dofLong.maxRadius); dof.highQualitySampling.Override(data.dofLong.highQuality);
            }
            if (data.night != null)
            {   // the night floor: one lift on every non-overlay post camera while it is night (weight set per camera)
                vNight = NewVolume("lens/night", out var pn); nightLift = pn.Add<LiftGammaGain>(false);
                var tn = Color.Lerp(Color.white, Hex(data.night.liftTint), data.night.liftTintShare); float lo = data.night.liftOffset;
                nightLift.lift.Override(new Vector4(tn.r, tn.g, tn.b, lo)); nightLift.gain.Override(new Vector4(1f, 1f, 1f, -lo));
            }
            ApplyPlayGrade();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => { baseSaturation = -999f; };
        }

        static Rig MakeRig(VolumeProfile p, ColorAdjustments adj) => new Rig
        {
            adj = adj ? adj : p.Add<ColorAdjustments>(false), wb = p.Add<WhiteBalance>(false), smh = p.Add<ShadowsMidtonesHighlights>(false), vig = p.Add<Vignette>(false),
        };

        Volume NewVolume(string name, out VolumeProfile profile)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var v = go.AddComponent<Volume>();
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = name;
            v.isGlobal = true; v.priority = 100f; v.weight = 0f; v.sharedProfile = profile;
            return v;
        }

        /// <summary>A bloom block that is there: JsonUtility fills an absent one with zeros.</summary>
        static bool HasBloom(Bloomp b) => b != null && b.intensity > 0f && !string.IsNullOrEmpty(b.tint);

        static void Bloom(VolumeProfile p, Bloomp b)
        {
            var bl = p.Add<Bloom>(false);
            bl.threshold.Override(b.threshold); bl.intensity.Override(b.intensity); bl.scatter.Override(b.scatter); bl.tint.Override(Hex(b.tint));
            if (b.clamp > 0f) bl.clamp.Override(b.clamp); // brief 079: the most one pixel can give the bloom (0 or absent: the scene grade's, unclamped)
        }

        /// <summary>The play grade: saturation = the scene grade's own x (1 + saturationPercent / 100), and a warm shadow lift (linear, before the
        /// tonemap: c x (1 - liftOffset) + liftOffset, so white stays white, plus the warm hue of liftTint at `liftWarmth`). The scene grade's own
        /// saturation is read from the day grade volume when it can be found.</summary>
        void ApplyPlayGrade()
        {
            var g = data.gradePlay;
            float s0 = BaseSaturation();
            float mult = (1f + s0 / 100f) * (1f + g.saturationPercent / 100f);
            playSatValue = Mathf.Clamp((mult - 1f) * 100f, -100f, 100f);
            playSat.saturation.Override(playSatValue);
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
            if (longAdj && Mathf.Abs(longExposureDelta) > 0.001f) longAdj.postExposure.Override(BaseExposure() + longExposureDelta);
            if (longAdj && Mathf.Abs(longSatDelta) > 0.001f) longAdj.saturation.Override(Mathf.Clamp(BaseSaturation() + longSatDelta, -100f, 100f));
            if (longAdj && Mathf.Abs(longConDelta) > 0.001f) longAdj.contrast.Override(Mathf.Clamp(BaseContrast() + longConDelta, -100f, 100f));
            if (Mathf.Abs(g.exposureDelta) > 0.001f) playSat.postExposure.Override(BaseExposure() + g.exposureDelta); else playSat.postExposure.overrideState = false; // brief 075-A: the play view's own exposure step // only the exposure is overridden; contrast, saturation and filter stay the scene grade's
            baseSaturation = s0;
            duskK = -1f; ApplyPlay096(); // the evening grade is pushed again on the next Update (a scene load re-reads the scene grade); brief 096: Sky's A+D+B play grade (AtmosphereLens.Play096.cs)
        }

        bool LongGrade => Mathf.Abs(longExposureDelta) > 0.001f || Mathf.Abs(longSatDelta) > 0.001f || Mathf.Abs(longConDelta) > 0.001f;

        float BaseContrast() { var sa = SceneAdj(); return sa != null && sa.contrast.overrideState ? sa.contrast.value : 6f; }

        float BaseExposure()
        {
            var ph = Suborned.Play.LightPhase.Instance;
            var vol = ph ? ph.day.grade : null;
            if (vol && vol.sharedProfile && vol.sharedProfile.TryGet<ColorAdjustments>(out var ca) && ca.postExposure.overrideState) return ca.postExposure.value;
            return 0.15f; // light.json post.colour.postExposure, when the day grade cannot be found
        }

        /// <summary>The sun's shadow strength (lens.json sun.shadowStrength) by day on the perspective and play cameras, the scene's own when the atmosphere is off or at night.</summary>
        void SunShade(bool on)
        {
            if (!sunLight)
            {
                if (Time.frameCount - sunSearched < 120 && sunSearched > 0) return; // a scene without a sun (a menu): look again every two seconds, not every camera
                sunSearched = Time.frameCount;
                sunLight = RenderSettings.sun;
                if (!sunLight) foreach (var l in FindObjectsByType<Light>()) if (l && l.type == LightType.Directional && l.enabled) { sunLight = l; break; }
                if (!sunLight) return;
                sunStrength0 = sunLight.shadowStrength;
            }
            float want = on && data.sun != null && data.sun.shadowStrength > 0f ? data.sun.shadowStrength : sunStrength0;
            if (!on && DistanceHaze.Night && data.night != null && AtmosphereSettings.Enabled) want = data.night.shadowStrength; // the moon fills the shade (the night floor)
            float k = on ? Dusk : 0f; // evening (Glint 2026-10-24): the low sun's own shadow strength, eased in with LightPhase.Dusk
            if (k > 0f && data.evening != null) want = Mathf.Lerp(want, data.evening.sun.shadowStrength, k);
            if (!Mathf.Approximately(sunLight.shadowStrength, want)) sunLight.shadowStrength = want; Shader.SetGlobalFloat(LensSunShadowId, want); // brief 096: published for OldTownPlayLook, which otherwise forced its baked 1.0 over it
        }

        ColorAdjustments SceneAdj()
        {
            var ph = Suborned.Play.LightPhase.Instance; var vol = ph ? ph.day.grade : null;
            return vol && vol.sharedProfile && vol.sharedProfile.TryGet<ColorAdjustments>(out var ca) ? ca : null;
        }

        /// <summary>The evening grade at k (0 day .. 1 evening) on the three tiers' volumes: exposure, contrast, saturation, the colour filter, white balance, the shadows' and highlights' tint and the vignette, each as the SCENE GRADE's
        /// own value plus the evening's change by k. At k 0 every override that is not the tier's own day grade is released, so day frames do not move. Look only, no per-pixel cost (the grade is the LUT).</summary>
        void PushEvening(float k)
        {
            var g = data.evening.grade; bool on = k > 0.0005f;
            var sa = SceneAdj(); float c0 = sa != null && sa.contrast.overrideState ? sa.contrast.value : 6f;
            Color f0 = sa != null && sa.colorFilter.overrideState ? sa.colorFilter.value : Color.white;
            var ph = Suborned.Play.LightPhase.Instance; var vol = ph ? ph.day.grade : null; var prof = vol ? vol.sharedProfile : null;
            Vector4 s0 = new Vector4(1f, 1f, 1f, 0f), h0 = s0; float v0 = 0.1f;
            if (prof && prof.TryGet<ShadowsMidtonesHighlights>(out var sm)) { if (sm.shadows.overrideState) s0 = sm.shadows.value; if (sm.highlights.overrideState) h0 = sm.highlights.value; }
            if (prof && prof.TryGet<Vignette>(out var vg) && vg.intensity.overrideState) v0 = vg.intensity.value;
            Color fil = Color.Lerp(Color.white, HexC(g.filter), k), shT = Color.Lerp(Color.white, HexC(g.shadows), k), hiT = Color.Lerp(Color.white, HexC(g.highlights), k);
            float expBase = BaseExposure(), satBase = BaseSaturation();
            Rig[] rigs = { rigPlay, rigFollow, rigLong };
            for (int i = 0; i < 3; i++)
            {
                var r = rigs[i]; if (r == null) continue;
                bool play = i == 0, lng = i == 2;
                // exposure: the long tier's own delta stays; play and follow have none by day
                if (lng && Mathf.Abs(longExposureDelta) > 0.001f) r.adj.postExposure.Override(expBase + longExposureDelta + g.exposure * k);
                else if (play && Mathf.Abs(data.gradePlay.exposureDelta) > 0.001f) r.adj.postExposure.Override(expBase + data.gradePlay.exposureDelta + g.exposure * k); // the play view's own step stays at k 0
                else if (on) r.adj.postExposure.Override(expBase + g.exposure * k); else r.adj.postExposure.overrideState = false;
                if (play) r.adj.saturation.Override(playSatValue + g.saturation * k);                      // the play grade's own saturation is always overridden
                else if (lng && Mathf.Abs(longSatDelta) > 0.001f) r.adj.saturation.Override(satBase + longSatDelta + g.saturation * k); // brief 075 golden: the long tier's own step stays at k 0
                else if (on) r.adj.saturation.Override(satBase + g.saturation * k); else r.adj.saturation.overrideState = false;
                if (lng && Mathf.Abs(longConDelta) > 0.001f) r.adj.contrast.Override(c0 + longConDelta + g.contrast * k);
                else if (on) r.adj.contrast.Override(c0 + g.contrast * k); else r.adj.contrast.overrideState = false;
                if (on) r.adj.colorFilter.Override(new Color(f0.r * fil.r, f0.g * fil.g, f0.b * fil.b, 1f)); else r.adj.colorFilter.overrideState = false;
                if (on) { r.wb.temperature.Override(g.temperature * k); r.wb.tint.Override(g.tint * k); } else { r.wb.temperature.overrideState = false; r.wb.tint.overrideState = false; }
                if (on)
                {
                    r.smh.shadows.Override(new Vector4(s0.x * shT.r, s0.y * shT.g, s0.z * shT.b, s0.w));
                    r.smh.highlights.Override(new Vector4(h0.x * hiT.r, h0.y * hiT.g, h0.z * hiT.b, h0.w));
                    r.vig.intensity.Override(Mathf.Lerp(v0, g.vignette, k));
                }
                else { r.smh.shadows.overrideState = false; r.smh.highlights.overrideState = false; r.vig.intensity.overrideState = false; }
            }
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

        /// <summary>How far into the evening the look is (LightPhase.Dusk, 0 = day), 0 when the lens has no evening or the atmosphere is off.</summary>
        float Dusk { get { var ph = Suborned.Play.LightPhase.Instance; return data.evening != null && ph && AtmosphereSettings.Enabled ? ph.Dusk : 0f; } }

        void Update()
        {
            if (data.evening != null) { float k = Dusk; if (!Mathf.Approximately(k, duskK)) { duskK = k; PushEvening(k); } }
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
            // brief 075 banding (Glint, 2026-10-28; Sky calls the stepped sky a defect): the 8-bit encode of the slow sky and shading gradients steps
            // one level every 4 to 18 px with no dither (075-A kept it on purpose; that is reversed). URP's final-pass dither (blue noise, ±0.5 of an
            // 8-bit level) on every base camera that runs post, day, evening and night, lens on or off. Signals draw after post (SignalsFeature), so
            // they are untouched. Cost: one noise tap and an add in the final blit.
            if (hasUd && !overlay && ud.renderPostProcessing && !ud.dithering) ud.dithering = true;
            if (baseSaturation < -990f && on) ApplyPlayGrade();
            if (!overlay) SunShade(on);
            if (vNight) vNight.weight = hasUd && ud.renderPostProcessing && !overlay && DistanceHaze.Night && AtmosphereSettings.Enabled ? 1f : 0f;
            vPlay.weight = post && tier == DistanceHaze.Tier.Play ? 1f : 0f;
            vFollow.weight = post && tier == DistanceHaze.Tier.Follow ? 1f : 0f;
            vLong.weight = post && tier == DistanceHaze.Tier.Long ? 1f : 0f;
            float fl = !post ? 0f : tier == DistanceHaze.Tier.Follow ? data.flare.follow : tier == DistanceHaze.Tier.Long ? data.flare.@long : 0f;
            if (fl > 0f) EnsureFlare();
            if (flare) { flare.intensity = fl; if (flare.enabled != fl > 0f) flare.enabled = fl > 0f; } // brief 064 A3 item 5: a registered flare with occlusion makes URP force the depth texture (CheckPostProcessForDepth), so it is off wherever it does not draw (the play view)
            Sky(on && tier != DistanceHaze.Tier.Play && !overlay, tier);
            Shader.SetGlobalFloat(idNight, DistanceHaze.Night ? 1f : 0f); // brief 075-A: the ported sea (Suborned/DK Ocean) mirrors the day sky's air only by day
        }

        void End(ScriptableRenderContext _, Camera c)
        {
            if (c.cameraType != CameraType.Game) return;
            Reset(false);
        }

        /// <summary>Volumes and flare back to neutral, so a camera that is not ours (the Scene view, a probe) renders as built. Both are read while the
        /// camera records, which is before this runs. The sky globals are set explicitly by every game camera's Begin, so they are only cleared
        /// when the component goes. The flare stays enabled across follow and long frames and is disabled by the next Begin that does not draw it.</summary>
        void Reset(bool sky)
        {
            if (vNight) vNight.weight = 0f;
            if (vPlay) vPlay.weight = 0f; if (vFollow) vFollow.weight = 0f; if (vLong) vLong.weight = 0f;
            if (flare) flare.intensity = 0f;
            if (sky) { Sky(false, DistanceHaze.Tier.Play); SunShade(false); }
        }

        // ---- sky: shader globals read by Suborned/SkyGradient (all zero = the sky as built) ----
        void Sky(bool on, DistanceHaze.Tier tier)
        {
            if (!on)
            {
                Shader.SetGlobalVector(idHaze, Vector4.zero); Shader.SetGlobalVector(idZenith, Vector4.zero); Shader.SetGlobalVector(idMid, Vector4.zero);
                Shader.SetGlobalVector(idShape, Vector4.zero); Shader.SetGlobalVector(idExtra, Vector4.zero); Shader.SetGlobalVector(idBandRange, Vector4.zero);
                Shader.SetGlobalVector(idSunDir, Vector4.zero); Shader.SetGlobalVector(idSunCol, Vector4.zero);
                return;
            }
            var s = data.sky;
            float k = Dusk; var ev = k > 0f ? data.evening : null; // evening (Glint 2026-10-24): the sky leans toward its dusk values by k; at k 0 none of it is set, the sky is as it was
            var h = DistanceHaze.LinearHaze(tier); // the sky's horizon and below-horizon colour: the fog's colour (already linear; the long tier's HDR gain included)
            Shader.SetGlobalVector(idHaze, new Vector4(h.r, h.g, h.b, 1f));
            if (ev != null) SceneSky();
            if (AtmosphereSettings.SkyTeal)
            {
                var z = Lin(tealZenith); var m = Lin(tealMid);
                if (ev != null) { z = Color.Lerp(z, Lin(HexC(ev.sky.zenith)), k); m = Color.Lerp(m, Lin(HexC(ev.sky.mid)), k); }
                Shader.SetGlobalVector(idZenith, new Vector4(z.r, z.g, z.b, 1f));
                Shader.SetGlobalVector(idMid, new Vector4(m.r, m.g, m.b, 1f));
            }
            else if (ev != null && skyRead)
            {   // cobalt by day is the scene sky material's own; at dusk it is blended (in linear) toward the evening zenith and mid
                var z = Color.Lerp(dayZenith, Lin(HexC(ev.sky.zenith)), k); var m = Color.Lerp(dayMid, Lin(HexC(ev.sky.mid)), k);
                Shader.SetGlobalVector(idZenith, new Vector4(z.r, z.g, z.b, 1f));
                Shader.SetGlobalVector(idMid, new Vector4(m.r, m.g, m.b, 1f));
            }
            else { Shader.SetGlobalVector(idZenith, Vector4.zero); Shader.SetGlobalVector(idMid, Vector4.zero); } // cobalt: the scene sky material's own
            float hh = AtmosphereSettings.SkyTeal && data.teal.hazeHeight > 0f ? data.teal.hazeHeight : s.hazeHeight; // the teal sky's gentler band into cream; cobalt keeps sky.hazeHeight
            if (!AtmosphereSettings.SkyTeal && tier == DistanceHaze.Tier.Long && s.hazeHeightLong > 0f) hh = s.hazeHeightLong; // the long tier's own, lower band: the sky stays blue overhead
            float aur = s.aureole, aurP = s.aureolePower, lift = s.hazeLift, peach = s.peach;
            if (ev != null)
            {
                hh = Mathf.Lerp(hh, ev.sky.hazeHeight, k); aur = Mathf.Lerp(aur, ev.sky.aureole, k); aurP = Mathf.Lerp(aurP, ev.sky.aureolePower, k); lift = Mathf.Lerp(lift, ev.sky.hazeLift, k); peach = Mathf.Lerp(peach, ev.sky.peach, k);
                var ph = Suborned.Play.LightPhase.Instance; var sun = ph ? ph.sun : null;
                if (sun)
                {   // the sky's glare follows the real sun: its direction and a glow colour that leans from the scene's to the dusk orange
                    var to = -sun.transform.forward; var gc = Color.Lerp(skyRead ? daySunGlow : Lin(HexC(ev.sky.sunGlow)), Lin(HexC(ev.sky.sunGlow)), k);
                    Shader.SetGlobalVector(idSunDir, new Vector4(to.x, to.y, to.z, 1f)); Shader.SetGlobalVector(idSunCol, new Vector4(gc.r, gc.g, gc.b, 1f));
                }
            }
            else { Shader.SetGlobalVector(idSunDir, Vector4.zero); Shader.SetGlobalVector(idSunCol, Vector4.zero); }
            Shader.SetGlobalVector(idShape, new Vector4(hh, aur, aurP, lift));
            Shader.SetGlobalVector(idExtra, new Vector4(s.broad, s.broadPower, peach, 0f));
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
