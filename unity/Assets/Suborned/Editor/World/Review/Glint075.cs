using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Suborned.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World.Review
{
    /// <summary>Brief 075-A (Glint, 2026-10-28): the old reel's look ported, shot offscreen in batchmode on the built HarbourDistrict scene (no
    /// Editor window, no Game view, no desktop capture). Variant `today` = the scene as built with the runtime look installed from the lens.json
    /// and haze.json in Resources (AtmosphereLens, DistanceHaze, LightPhase), as the player shows it. Variant `port` = light.json's day keys applied
    /// in memory first through the builder's own code (Rig's sun, Atmosphere's ambient, SpaceSky and grade Build, Look.Ocean for the water), so the
    /// frames are what Latch's rebuild will give; nothing is saved and the scene is never written.
    ///   -mode play   : office front and café square (cell-open) at yaw 30 and 210, play zoom 14 / pitch 38, day and night
    ///   -mode flyby  : Flyby074's flight (keys json) at the grid times, 1920 x 1080, the long tier (lens and haze) on
    ///   -mode heads  : the crossing heads nearest the office front held in walk / clearing / stop at the four yaws, 3840 x 2160 (Signals072's measure)
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.Glint075.Cli -variant today|port -mode play|flyby|heads
    ///         -out &lt;dir&gt; [-keys briefs/074/flyby/new-keys.json] [-stills 6,20,33,50,61.5] -logFile &lt;log&gt;</summary>
    public static class Glint075
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static void Cli() => HarbourWorldBuilder.Batch(Run);

        static string Arg(string name, string fallback)
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : fallback;
        }
        static string Abs(string p) => Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(HarbourWorldBuilder.RepoRoot, p));

        static void Run()
        {
            string variant = Arg("-variant", "today"), mode = Arg("-mode", "play");
            var outDir = Abs(Arg("-out", $"out/scratch/glint/075/{variant}"));
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            var playGo = GameObject.Find("cameras/play") ?? throw new InvalidOperationException("glint075: no cameras/play");
            var cam = playGo.GetComponent<Camera>(); var shadows = playGo.GetComponent<TacticalShadows>(); var fade = playGo.GetComponent<ArchitectureFade>();
            var host = UnityEngine.Object.FindAnyObjectByType<HandoverHost>(FindObjectsInactive.Include);
            float standOff = host ? host.standOff : Baseline072.DefaultStandOff;
            Vector2 cellOpen = Baseline072.CellOpenFallback;
            try { if (host != null) { var w = host.frame.World(HandoverHost.OpeningFocus); if (!float.IsNaN(w.x) && (w.x != 0 || w.z != 0)) cellOpen = new Vector2(w.x, w.z); } } catch { }
            var people = GameObject.Find("people"); if (people) people.SetActive(false);
            if (shadows) { shadows.enabled = false; shadows.enabled = true; }
            if (fade) { fade.enabled = false; fade.enabled = true; }
            foreach (var oc in Camera.allCameras) if (oc != cam && oc.targetTexture == null) oc.enabled = false;

            var phase = UnityEngine.Object.FindAnyObjectByType<LightPhase>(FindObjectsInactive.Include) ?? throw new InvalidOperationException("glint075: no LightPhase");
            typeof(LightPhase).GetMethod("OnEnable", Any).Invoke(phase, null);
            var report = new StringBuilder(); Action restore = null;
            if (variant == "port") Port(phase, report);
            var lens = InstallLens(Arg("-lens", null));
            var haze = InstallHaze();
            Vector3 focusNow = new(Baseline072.OfficeFront.x, 0f, Baseline072.OfficeFront.y); phase.Focus = () => focusNow;
            var sun = RenderSettings.sun;
            report.Append($"variant {variant}: sun #{ColorUtility.ToHtmlStringRGB(sun.color)} x{sun.intensity:F3} euler {sun.transform.eulerAngles}; skybox {(RenderSettings.skybox ? RenderSettings.skybox.shader.name : "none")}; ambient sky #{ColorUtility.ToHtmlStringRGB(RenderSettings.ambientSkyColor)} eq #{ColorUtility.ToHtmlStringRGB(RenderSettings.ambientEquatorColor)} gr #{ColorUtility.ToHtmlStringRGB(RenderSettings.ambientGroundColor)}\n");
            try
            {
                using var session = new RenderSession();
                // banding probe (075-A): the URP asset's HDR buffer precision and grading LUT size, in memory only (restored below)
                var urp = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
                var prec0 = urp.hdrColorBufferPrecision; int lut0 = urp.colorGradingLutSize;
                restore = () => { urp.hdrColorBufferPrecision = prec0; urp.colorGradingLutSize = lut0; };
                if (Arg("-precision", null) == "64") urp.hdrColorBufferPrecision = UnityEngine.Rendering.Universal.HDRColorBufferPrecision._64Bits;
                if (Arg("-lut", null) is { } lut) urp.colorGradingLutSize = int.Parse(lut);
                report.Append($"urp: hdr precision {urp.hdrColorBufferPrecision}, grading {urp.colorGradingMode} lut {urp.colorGradingLutSize}\n");
                Baseline072.Pose(cam, shadows, fade, Baseline072.OfficeFront, 30f, standOff); session.Grab(cam, 64, 64); // the first render after a load comes out flat
                if (mode == "play")
                {
                    var views = new List<(string id, Vector2 f, float yaw)> { ("office-front", Baseline072.OfficeFront, 30f), ("office-front", Baseline072.OfficeFront, 210f), ("cafe-square", cellOpen, 30f), ("cafe-square", cellOpen, 210f) };
                    // brief 075-F1 (Latch): -spots "id:x,y,yaw;..." adds play-zoom views (plan x, y = world x, z); -posters off puts every board back on its
                    // base material in memory (the canonical "mars" cell everywhere: the scene as it was before the per-instance swap), never saved
                    if (Arg("-spots", null) is { } spots)
                    {
                        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-spotsonly") >= 0) views.Clear();
                        foreach (var sp in spots.Split(';', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var kv = sp.Split(':'); var n = kv[1].Split(',').Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToArray();
                            views.Add((kv[0], new Vector2(n[0], n[1]), n[2]));
                        }
                    }
                    if (Arg("-posters", "on") == "off") report.Append($"posters off: {PostersOff()} slots back on their base\n");
                    foreach (var night in Array.IndexOf(Environment.GetCommandLineArgs(), "-nonight") >= 0 ? new[] { false } : new[] { false, true })
                    {
                        phase.Set(night);
                        foreach (var v in views)
                        {
                            focusNow = new Vector3(v.f.x, 0f, v.f.y);
                            if (night) typeof(LightPhase).GetMethod("Cap", Any).Invoke(phase, new object[] { true });
                            Baseline072.Pose(cam, shadows, fade, v.f, v.yaw, standOff);
                            session.Grab(cam, 64, 64);
                            File.WriteAllBytes(Path.Combine(outDir, $"{variant}-{(night ? "night" : "day")}-{v.id}-y{v.yaw:000}.png"), session.Grab(cam, 1920, 1080).EncodeToPNG());
                        }
                    }
                    phase.Set(false);
                }
                else if (mode == "flyby") Flyby(session, outDir, variant, report);
                else if (mode == "golden") { UnityEngine.Object.DestroyImmediate(lens.gameObject); lens = Golden(session, cam, shadows, fade, standOff, cellOpen, outDir, report, v => focusNow = v); }
                else if (mode == "heads") Heads(session, cam, shadows, fade, standOff, outDir, report);
                else throw new ArgumentException("glint075: -mode play|flyby|heads|golden");
            }
            finally
            {
                restore?.Invoke();
                phase.Set(false);
                typeof(AtmosphereLens).GetMethod("OnDisable", Any)?.Invoke(lens, null);
                UnityEngine.Object.DestroyImmediate(lens.gameObject);
                if (haze) { typeof(DistanceHaze).GetMethod("OnDisable", Any)?.Invoke(haze, null); UnityEngine.Object.DestroyImmediate(haze.gameObject); }
            }
            File.WriteAllText(Path.Combine(outDir, $"glint075-{variant}-{mode}.txt"), report.ToString());
            Debug.Log($"glint075: {variant} {mode} → {outDir}\n{report}");
        }

        /// <summary>Brief 075-F1: every DK_Billboard_Ads(_Lit)__key slot back on its base, in memory (the canonical cell on every board).</summary>
        static int PostersOff()
        {
            var bases = new Dictionary<string, Material>(StringComparer.Ordinal); int n = 0;
            foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (!mats[i] || !mats[i].name.StartsWith("DK_Billboard_Ads", StringComparison.Ordinal)) continue;
                    int cut = mats[i].name.IndexOf("__", StringComparison.Ordinal); if (cut < 0) continue;
                    var bn = mats[i].name.Substring(0, cut);
                    if (!bases.TryGetValue(bn, out var b)) bases[bn] = b = AssetDatabase.LoadAssetAtPath<Material>($"{Look.KitMaterials}/{bn}.mat");
                    if (!b) continue; mats[i] = b; changed = true; n++;
                }
                if (changed) r.sharedMaterials = mats;
            }
            return n;
        }

        /// <summary>light.json's day keys onto the open scene, through the builder's code; the day State LightPhase restores is updated too.</summary>
        static void Port(LightPhase phase, StringBuilder report)
        {
            var look = new Look(HarbourWorldBuilder.StylePath, HarbourWorldBuilder.LookPath);
            var sun = phase.sun ? phase.sun : RenderSettings.sun;
            var e = look.Entry("sun");
            sun.color = look.Colour(e); sun.intensity = J.F(e, "intensity");
            sun.transform.rotation = Quaternion.Euler(J.F(e, "pitch"), J.F(e, "yaw"), 0f);
            var amb = look.Light("ambient");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = look.Colour(J.Get(amb, "sky"));
            RenderSettings.ambientEquatorColor = look.Colour(J.Get(amb, "equator"));
            RenderSettings.ambientGroundColor = look.Colour(J.Get(amb, "ground"));
            if (look.Light("spaceSky") is { } space) RenderSettings.skybox = Atmosphere.SpaceSky(space, -sun.transform.forward);
            DynamicGI.UpdateEnvironment();
            var grade = Atmosphere.Build(look, look.Light("post"), "grade (075 port, unsaved)");
            if (phase.day.grade) phase.day.grade.sharedProfile = grade;
            // the day State LightPhase.Set(false) restores
            var st = phase.day; var t = st.GetType();
            void SetF(string n, object v) { var f = t.GetField(n, Any); if (f != null) f.SetValue(st, v); }
            SetF("sunColour", sun.color); SetF("sunIntensity", sun.intensity); SetF("sunRotation", sun.transform.rotation);
            SetF("ambientSky", RenderSettings.ambientSkyColor); SetF("ambientEquator", RenderSettings.ambientEquatorColor); SetF("ambientGround", RenderSettings.ambientGroundColor);
            SetF("skybox", RenderSettings.skybox);
            int swapped = 0;
            if (look.Light("ocean") is { } ocean)
            {
                Material made = null;
                foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
                {
                    var mats = r.sharedMaterials; bool any = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = mats[i]; if (!m || m.name != "water" || m.shader.name != "Suborned/Water") continue;
                        if (!made)
                        {
                            made = Look.Ocean(ocean, m.GetColor("_Haze"), look.Light("spaceSky"));
                            made.SetVector("_FadeCentre", m.GetVector("_FadeCentre")); made.SetFloat("_FadeFrom", m.GetFloat("_FadeFrom")); made.SetFloat("_FadeTo", m.GetFloat("_FadeTo"));
                        }
                        mats[i] = made; any = true;
                    }
                    if (any) { r.sharedMaterials = mats; swapped++; }
                }
            }
            report.Append($"port: state fields {string.Join(",", t.GetFields(Any).Select(f => f.Name))}; water renderers swapped {swapped}\n");
        }

        /// <summary>AtmosphereLens as Glint072.InstallLens does, from `path` (a variant lens.json) or Resources/Atmosphere/lens.json.</summary>
        internal static AtmosphereLens InstallLens(string path)
        {
            var text = path != null ? File.ReadAllText(Abs(path)) : (Resources.Load<TextAsset>(AtmosphereLens.ResourcePath) ?? throw new InvalidOperationException("glint075: no lens.json")).text;
            var d = JsonUtility.FromJson<AtmosphereLens.Lens>(text);
            var why = (string)typeof(AtmosphereLens).GetMethod("Check", Any).Invoke(null, new object[] { d });
            if (why != null) throw new InvalidDataException("glint075: lens rejected: " + why);
            typeof(AtmosphereLens).GetProperty("EveningData", Any).SetValue(null, d.evening);
            typeof(AtmosphereLens).GetProperty("NightData", Any).SetValue(null, d.night);
            var go = new GameObject("atmosphere lens (glint075)"); go.SetActive(false);
            var a = go.AddComponent<AtmosphereLens>();
            typeof(AtmosphereLens).GetField("data", Any).SetValue(a, d);
            typeof(AtmosphereLens).GetMethod("Build", Any).Invoke(a, null);
            go.SetActive(true);
            typeof(AtmosphereLens).GetMethod("OnEnable", Any).Invoke(a, null);
            return a;
        }

        /// <summary>DistanceHaze as its Install does (edit mode: no DontDestroyOnLoad), from Resources/Atmosphere/haze.json.</summary>
        internal static DistanceHaze InstallHaze()
        {
            var text = Resources.Load<TextAsset>("Atmosphere/haze") ?? throw new InvalidOperationException("glint075: no haze.json");
            var d = JsonUtility.FromJson<DistanceHaze.Data>(text.text);
            var args = new object[] { d, null };
            var why = (string)typeof(DistanceHaze).GetMethod("Check", Any, null, new[] { typeof(DistanceHaze.Data), typeof(Color).MakeByRefType() }, null).Invoke(null, args);
            if (why != null) throw new InvalidDataException("glint075: haze.json rejected: " + why);
            var go = new GameObject("distance haze (glint075)"); go.SetActive(false);
            var h = go.AddComponent<DistanceHaze>();
            void S(string n, object v) => typeof(DistanceHaze).GetField(n, Any).SetValue(h, v);
            S("data", d); S("colour", (Color)args[1]);
            if (!string.IsNullOrEmpty(d.@long.colour) && ColorUtility.TryParseHtmlString(d.@long.colour, out var lc)) { S("longColour", lc); S("longColourSet", true); }
            S("followEnd", d.follow.mode == "linear" ? d.follow.end : d.follow.farClip);
            go.SetActive(true);
            typeof(DistanceHaze).GetMethod("OnEnable", Any).Invoke(h, null);
            return h;
        }

        struct K { public float t; public Vector3 eye, look; }

        static void Flyby(RenderSession session, string outDir, string variant, StringBuilder report)
        {
            var o = Json.Parse(File.ReadAllText(Abs(Arg("-keys", "briefs/074/flyby/new-keys.json"))));
            var keys = new List<K>();
            foreach (var k in J.A(o, "keys"))
            {
                var e = J.A(k, "eye"); var l = J.A(k, "look");
                keys.Add(new K { t = J.F(k, "t"), eye = new Vector3((float)(double)e[0], (float)(double)e[1], (float)(double)e[2]), look = new Vector3((float)(double)l[0], (float)(double)l[1], (float)(double)l[2]) });
            }
            if (Arg("-sun", null) is { } sa) // a demonstration sun (pitch,yaw), e.g. the old reel's sun ahead of the camera for the sea glitter; never the game's
            {
                var pv = sa.Split(','); var sun = RenderSettings.sun;
                sun.transform.rotation = Quaternion.Euler(float.Parse(pv[0], CultureInfo.InvariantCulture), float.Parse(pv[1], CultureInfo.InvariantCulture), 0f);
                if (RenderSettings.skybox && RenderSettings.skybox.HasProperty("_SunDir")) { var to = -sun.transform.forward; RenderSettings.skybox.SetVector("_SunDir", new Vector4(to.x, to.y, to.z, 0)); }
                variant += "-sun" + pv[0] + "_" + pv[1];
                report.Append($"demonstration sun {sa}\n");
            }
            var cam = RenderSession.TempCamera("flyby075");
            cam.fieldOfView = J.Get(o, "fov") is double fv ? (float)fv : 58f;
            try
            {
                foreach (var s in Arg("-stills", "6,20,33,50,61.5").Split(','))
                {
                    float t = float.Parse(s, CultureInfo.InvariantCulture);
                    var eye = Spline(keys, t, i => keys[i].eye); var dir = Spline(keys, t, i => (keys[i].look - keys[i].eye).normalized);
                    new Pose { Position = eye, Rotation = Quaternion.LookRotation(dir, Vector3.up), Fov = cam.fieldOfView, Near = 0.3f, Far = 12000f }.Apply(cam);
                    session.ShadowDistance = Mathf.Clamp(eye.y * 8f, 120f, 1200f);
                    session.Grab(cam, 64, 64); session.Grab(cam, 64, 64); // two warm-up frames: the lens forces post on from the next frame
                    File.WriteAllBytes(Path.Combine(outDir, $"{variant}-t{s}.png"), session.Grab(cam, 1920, 1080).EncodeToPNG());
                    report.Append($"flyby t{s}: eye {eye} tier {DistanceHaze.TierOf(cam)} fog {RenderSettings.fogMode} {RenderSettings.fogDensity}\n");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(cam.gameObject); }
        }


        /// <summary>Brief 075 golden view (Glint, 2026-10-28): the golden frame (Flyby074's flight at -t, default 6 s: the boulevard frame Sky compared)
        /// under each variant of -variants (a json array), in one session. A variant: name; lens (a lens.json path, default Resources); ambient (scale on
        /// the day trilight) and ambientSky / ambientEquator / ambientGround (#rrggbb, before the scale); sunYaw / sunPitch / sunIntensity; ssao
        /// { intensity, radius, direct, afterOpaque, source depth|normals } on the renderer's feature in memory; play (true: also the four day play
        /// views). Every variant starts from the ported base; nothing is saved.</summary>
        static AtmosphereLens Golden(RenderSession session, Camera playCam, TacticalShadows shadows, ArchitectureFade fade, float standOff, Vector2 cellOpen,
                                     string outDir, StringBuilder report, Action<Vector3> focus)
        {
            var o = Json.Parse(File.ReadAllText(Abs(Arg("-keys", "briefs/074/flyby/new-keys.json"))));
            var keys = new List<K>();
            foreach (var k in J.A(o, "keys"))
            {
                var e = J.A(k, "eye"); var l = J.A(k, "look");
                keys.Add(new K { t = J.F(k, "t"), eye = new Vector3((float)(double)e[0], (float)(double)e[1], (float)(double)e[2]), look = new Vector3((float)(double)l[0], (float)(double)l[1], (float)(double)l[2]) });
            }
            float t = float.Parse(Arg("-t", "6"), CultureInfo.InvariantCulture);
            var variants = J.A(Json.Parse(File.ReadAllText(Abs(Arg("-variants", "briefs/075/golden/glint/variants.json")))));
            var sun = RenderSettings.sun;
            Quaternion rot0 = sun.transform.rotation; float int0 = sun.intensity;
            Color as0 = RenderSettings.ambientSkyColor, ae0 = RenderSettings.ambientEquatorColor, ag0 = RenderSettings.ambientGroundColor;
            var feat = TacticalShadows.SsaoFeature;
            object st = feat ? typeof(UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion).GetField("m_Settings", Any).GetValue(feat) : null;
            FieldInfo SF(string n) => st?.GetType().GetField(n, Any);
            var ssao0 = st == null ? null : new object[] { SF("Intensity").GetValue(st), SF("Radius").GetValue(st), SF("DirectLightingStrength").GetValue(st), SF("AfterOpaque").GetValue(st), SF("Source").GetValue(st) };
            void SsaoSet(object i, object r, object d, object a, object src) { if (st == null) return; SF("Intensity").SetValue(st, i); SF("Radius").SetValue(st, r); SF("DirectLightingStrength").SetValue(st, d); SF("AfterOpaque").SetValue(st, a); SF("Source").SetValue(st, src); }
            var cam = RenderSession.TempCamera("golden075");
            cam.fieldOfView = J.Get(o, "fov") is double fv ? (float)fv : 58f;
            AtmosphereLens lens = null;
            static Color Hx(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
            try
            {
                foreach (var v in variants)
                {
                    string name = J.S(v, "name");
                    // base
                    sun.transform.rotation = rot0; sun.intensity = int0;
                    RenderSettings.ambientSkyColor = as0; RenderSettings.ambientEquatorColor = ae0; RenderSettings.ambientGroundColor = ag0;
                    if (ssao0 != null) SsaoSet(ssao0[0], ssao0[1], ssao0[2], ssao0[3], ssao0[4]);
                    // variant
                    var eu = rot0.eulerAngles;
                    float pitch = J.Get(v, "sunPitch") is double sp ? (float)sp : eu.x, yaw = J.Get(v, "sunYaw") is double sy ? (float)sy : eu.y;
                    sun.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                    if (J.Get(v, "sunIntensity") is double si) sun.intensity = (float)si;
                    float sc = J.Get(v, "ambient") is double asc ? (float)asc : 1f;
                    Color A(string key, Color c0) => (J.S(v, key) is { } h ? Hx(h) : c0) * sc;
                    RenderSettings.ambientSkyColor = A("ambientSky", as0); RenderSettings.ambientEquatorColor = A("ambientEquator", ae0); RenderSettings.ambientGroundColor = A("ambientGround", ag0);
                    if (RenderSettings.skybox && RenderSettings.skybox.HasProperty("_SunDir")) { var to = -sun.transform.forward; RenderSettings.skybox.SetVector("_SunDir", new Vector4(to.x, to.y, to.z, 0)); }
                    DynamicGI.UpdateEnvironment();
                    if (J.Get(v, "ssao") is Dictionary<string, object> so && st != null)
                    {
                        var srcT = SF("Source").FieldType;
                        SsaoSet(J.Get(so, "intensity") is double i ? (float)i : ssao0[0], J.Get(so, "radius") is double r ? (float)r : ssao0[1], J.Get(so, "direct") is double d ? (float)d : ssao0[2],
                            J.Get(so, "afterOpaque") is bool ao ? ao : ssao0[3], J.S(so, "source") is { } src ? Enum.ToObject(srcT, src == "normals" ? 1 : 0) : ssao0[4]);
                    }
                    var ph = UnityEngine.Object.FindAnyObjectByType<LightPhase>(FindObjectsInactive.Include);
                    if (ph != null)
                    {   // the day State LightPhase.Set(false) restores
                        var dst = ph.day; var dt = dst.GetType();
                        void SetF(string n, object val) { var f = dt.GetField(n, Any); if (f != null) f.SetValue(dst, val); }
                        SetF("sunIntensity", sun.intensity); SetF("sunRotation", sun.transform.rotation);
                        SetF("ambientSky", RenderSettings.ambientSkyColor); SetF("ambientEquator", RenderSettings.ambientEquatorColor); SetF("ambientGround", RenderSettings.ambientGroundColor);
                    }
                    if (lens) { typeof(AtmosphereLens).GetMethod("OnDisable", Any)?.Invoke(lens, null); UnityEngine.Object.DestroyImmediate(lens.gameObject); }
                    lens = InstallLens(J.S(v, "lens"));
                    var eye = Spline(keys, t, i => keys[i].eye); var dir = Spline(keys, t, i => (keys[i].look - keys[i].eye).normalized);
                    new Pose { Position = eye, Rotation = Quaternion.LookRotation(dir, Vector3.up), Fov = cam.fieldOfView, Near = 0.3f, Far = 12000f }.Apply(cam);
                    session.ShadowDistance = Mathf.Clamp(eye.y * 8f, 120f, 1200f);
                    cam.enabled = true;
                    session.Grab(cam, 64, 64); session.Grab(cam, 64, 64);
                    File.WriteAllBytes(Path.Combine(outDir, $"{name}-golden.png"), session.Grab(cam, 1920, 1080).EncodeToPNG());
                    cam.enabled = false;
                    session.ShadowDistance = 0f;
                    report.Append($"golden {name}: t {t} eye {eye} sun {sun.transform.eulerAngles} x{sun.intensity:F2}; ambient #{ColorUtility.ToHtmlStringRGB(RenderSettings.ambientSkyColor)} #{ColorUtility.ToHtmlStringRGB(RenderSettings.ambientEquatorColor)} #{ColorUtility.ToHtmlStringRGB(RenderSettings.ambientGroundColor)}; ssao {(st == null ? "none" : $"{SF("Intensity").GetValue(st)} r{SF("Radius").GetValue(st)} d{SF("DirectLightingStrength").GetValue(st)} after {SF("AfterOpaque").GetValue(st)} {SF("Source").GetValue(st)}")}; lens {J.S(v, "lens") ?? "Resources"}\n");
                    if (J.B(v, "play"))
                        foreach (var (id, f, yawV) in new[] { ("office-front", Baseline072.OfficeFront, 30f), ("office-front", Baseline072.OfficeFront, 210f), ("cafe-square", cellOpen, 30f), ("cafe-square", cellOpen, 210f) })
                        {
                            focus(new Vector3(f.x, 0f, f.y));
                            Baseline072.Pose(playCam, shadows, fade, f, yawV, standOff);
                            session.Grab(playCam, 64, 64);
                            File.WriteAllBytes(Path.Combine(outDir, $"{name}-day-{id}-y{yawV:000}.png"), session.Grab(playCam, 1920, 1080).EncodeToPNG());
                        }
                }
            }
            finally
            {
                if (ssao0 != null) SsaoSet(ssao0[0], ssao0[1], ssao0[2], ssao0[3], ssao0[4]);
                sun.transform.rotation = rot0; sun.intensity = int0;
                UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }
            return lens ? lens : InstallLens(null);
        }

        /// <summary>Flyby074's Hermite (non-uniform Catmull-Rom, still at both ends).</summary>
        static Vector3 Spline(List<K> keys, float t, Func<int, Vector3> p)
        {
            int last = keys.Count - 1, i = 0;
            while (i < last - 1 && t >= keys[i + 1].t) i++;
            float t0 = keys[i].t, t1 = keys[i + 1].t, dt = t1 - t0, u = Mathf.Clamp01((t - t0) / dt);
            Vector3 V(int k) => k == 0 || k == last ? Vector3.zero : (p(k + 1) - p(k - 1)) / (keys[k + 1].t - keys[k - 1].t);
            float u2 = u * u, u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * p(i) + (u3 - 2f * u2 + u) * dt * V(i) + (-2f * u3 + 3f * u2) * p(i + 1) + (u3 - u2) * dt * V(i + 1);
        }

        /// <summary>Signals072's heads measure on the (ported) scene: 3 heads x walk / clearing / stop x 4 yaws at 3840 x 2160, rects in heads.json.</summary>
        static void Heads(RenderSession session, Camera cam, TacticalShadows shadows, ArchitectureFade fade, float standOff, string outDir, StringBuilder report)
        {
            const int W = 3840, H = 2160;
            var heads = UnityEngine.Object.FindObjectsByType<SignalHead>(FindObjectsInactive.Exclude).Where(h => h.Figure)
                .OrderBy(h => (new Vector2(h.transform.position.x, h.transform.position.z) - Baseline072.OfficeFront).sqrMagnitude).Take(3).ToList();
            var all = UnityEngine.Object.FindObjectsByType<SignalHead>(FindObjectsInactive.Exclude);
            var step = typeof(SignalHead).GetMethod("Step", Any);
            var holdWas = SignalHead.Hold;
            var rows = new List<string>();
            static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
            try
            {
                foreach (var state in new[] { "walk", "clearing", "stop" })
                {
                    SignalHead.Hold = state;
                    foreach (var h in all) { Set(h, "first", true); Set(h, "wroteHead", false); Set(h, "wroteDots", false); step.Invoke(h, null); }
                    for (int i = 0; i < heads.Count; i++)
                    {
                        var h = heads[i]; var p = h.transform.position;
                        foreach (var yaw in Baseline072.Yaws)
                        {
                            Baseline072.Pose(cam, shadows, fade, new Vector2(p.x, p.z), yaw, standOff);
                            session.Grab(cam, 64, 64);
                            var name = $"h{i + 1}-{state}-y{yaw:000}.png";
                            File.WriteAllBytes(Path.Combine(outDir, name), session.Grab(cam, W, H).EncodeToPNG());
                            var r = (ValueTuple<int, int, int, int>)typeof(Signals072).GetMethod("Rect", Any).Invoke(null, new object[] { cam, h.Figure.bounds });
                            rows.Add($"    {{ \"shot\": \"{name}\", \"head\": \"{h.name}\", \"crossing\": \"{h.Crossing}\", \"state\": \"{state}\", \"yaw\": {yaw:0}, \"at\": [{F(p.x)}, {F(p.y)}, {F(p.z)}], \"facing\": {F(h.transform.eulerAngles.y)}, \"rect\": [{r.Item1}, {r.Item2}, {r.Item3}, {r.Item4}] }}");
                        }
                    }
                }
            }
            finally { SignalHead.Hold = holdWas; }
            File.WriteAllText(Path.Combine(outDir, "heads.json"), "{\n  \"schema\": \"latch.072g-heads/1\",\n  \"picture\": \"3840x2160\",\n  \"export\": \"scene as built\",\n  \"note\": \"glint075 " + Arg("-variant", "today") + "\",\n  \"shots\": [\n" + string.Join(",\n", rows) + "\n  ]\n}\n");
            report.Append($"heads: {heads.Count} x 3 states x {Baseline072.Yaws.Length} yaws\n");
        }

        static void Set(object o, string n, object v) => o.GetType().GetField(n, Any)?.SetValue(o, v);
    }
}
