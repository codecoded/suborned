using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Suborned;
using Suborned.World;

// Glint (008 light-honest): live, in-memory light.json candidates on the open scene, rendered through Capture (the honest path).
// Nothing is saved: every change is restored in the finally block. Mirrors Rig.Sun, Rig fog and Atmosphere.Post/Apply exactly.
public static class GlintHonest
{
    sealed class Saved
    {
        public Color sunColour; public float sunIntensity; public Quaternion sunRot;
        public Color amb, eq, gr; public Color fog; public Material sky;
        public VolumeProfile profile; public Volume vol; public AmbientMode mode;
    }

    static Dictionary<string, Color> palette;
    static Color Pal(string key) => palette.TryGetValue(key ?? "", out var c) ? c : throw new InvalidDataException("palette key " + key);
    static Color Col(object entry)
    {
        var c = Pal(J.S(entry, "palette"));
        Color.RGBToHSV(c, out var h, out var s, out var v);
        return Color.HSVToRGB(h, s, Mathf.Clamp01(v * J.F(entry, "value")));
    }
    static Vector4 Vec(object s, string ck, string wk) { var c = Pal(J.S(s, ck)); return new Vector4(c.r, c.g, c.b, J.F(s, wk)); }

    static VolumeProfile Post(object post)
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = "grade-candidate";
        var tm = profile.Add<Tonemapping>(true);
        tm.mode.Override(J.S(post, "tonemapping") == "ACES" ? TonemappingMode.ACES : TonemappingMode.Neutral);
        if (J.Get(post, "bloom") is { } b)
        {
            var bl = profile.Add<Bloom>(true);
            bl.threshold.Override(J.F(b, "threshold")); bl.intensity.Override(J.F(b, "intensity"));
            bl.scatter.Override(J.F(b, "scatter")); bl.tint.Override(Pal(J.S(b, "tint")));
        }
        if (J.Get(post, "colour") is { } c)
        {
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(J.F(c, "postExposure")); ca.contrast.Override(J.F(c, "contrast"));
            ca.saturation.Override(J.F(c, "saturation")); ca.colorFilter.Override(Pal(J.S(c, "filter")));
        }
        if (J.Get(post, "whiteBalance") is { } wb)
        {
            var w = profile.Add<WhiteBalance>(true);
            w.temperature.Override(J.F(wb, "temperature")); w.tint.Override(J.F(wb, "tint"));
        }
        if (J.Get(post, "shadowsMidtonesHighlights") is { } s)
        {
            var sm = profile.Add<ShadowsMidtonesHighlights>(true);
            sm.shadows.Override(Vec(s, "shadows", "shadowsWeight"));
            sm.midtones.Override(Vec(s, "midtones", "midtonesWeight"));
            sm.highlights.Override(Vec(s, "highlights", "highlightsWeight"));
        }
        if (J.Get(post, "vignette") is { } v)
        {
            var vg = profile.Add<Vignette>(true);
            vg.intensity.Override(J.F(v, "intensity")); vg.smoothness.Override(J.F(v, "smoothness")); vg.color.Override(Pal(J.S(v, "colour")));
        }
        return profile;
    }

    static Saved Apply(string cand)
    {
        var sun = RenderSettings.sun;
        var vol = GameObject.Find("lighting/grade").GetComponent<Volume>();
        var sv = new Saved { sunColour = sun.color, sunIntensity = sun.intensity, sunRot = sun.transform.rotation, amb = RenderSettings.ambientSkyColor, eq = RenderSettings.ambientEquatorColor,
            gr = RenderSettings.ambientGroundColor, fog = RenderSettings.fogColor, sky = RenderSettings.skybox, profile = vol.sharedProfile, vol = vol, mode = RenderSettings.ambientMode };
        if (string.IsNullOrEmpty(cand)) return sv;
        var root = Json.Parse(File.ReadAllText(cand));
        palette = new Dictionary<string, Color>();
        foreach (var kv in J.O(J.Get(root, "palette"))) { ColorUtility.TryParseHtmlString(J.S(kv.Value, "hex"), out var c); palette[kv.Key] = c; }
        var e = J.Get(root, "sun");
        sun.color = Col(e); sun.intensity = J.F(e, "intensity"); sun.transform.rotation = Quaternion.Euler(J.F(e, "pitch"), J.F(e, "yaw"), 0f);
        var amb = J.Get(root, "ambient");
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Col(J.Get(amb, "sky")); RenderSettings.ambientEquatorColor = Col(J.Get(amb, "equator")); RenderSettings.ambientGroundColor = Col(J.Get(amb, "ground"));
        RenderSettings.fogColor = Col(J.Get(root, "fog"));
        if (J.Get(root, "skyGradient") is { } sky)
        {
            var m = new Material(Shader.Find("Suborned/SkyGradient")) { name = "sky-candidate" };
            m.SetColor("_Zenith", Pal(J.S(sky, "zenith"))); m.SetColor("_Mid", Pal(J.S(sky, "mid"))); m.SetColor("_Horizon", Pal(J.S(sky, "horizon")));
            m.SetColor("_Below", Pal(J.S(sky, "below"))); m.SetColor("_SunColour", Pal(J.S(sky, "sunGlow")));
            var toSun = -sun.transform.forward; m.SetVector("_SunDir", new Vector4(toSun.x, toSun.y, toSun.z, 0));
            m.SetFloat("_MidHeight", J.F(sky, "midHeight")); m.SetFloat("_Curve", J.F(sky, "curve"));
            m.SetVector("_Glow", new Vector4(J.F(sky, "glowWidth"), J.F(sky, "glowStrength"), J.F(sky, "disc"), 0));
            RenderSettings.skybox = m;
        }
        vol.sharedProfile = Post(J.Get(root, "post"));
        return sv;
    }

    static void Restore(Saved s)
    {
        var sun = RenderSettings.sun;
        sun.color = s.sunColour; sun.intensity = s.sunIntensity; sun.transform.rotation = s.sunRot;
        RenderSettings.ambientMode = s.mode; RenderSettings.ambientSkyColor = s.amb; RenderSettings.ambientEquatorColor = s.eq; RenderSettings.ambientGroundColor = s.gr;
        RenderSettings.fogColor = s.fog; RenderSettings.skybox = s.sky; s.vol.sharedProfile = s.profile;
    }


    /// Unlit signal swatches (cones, guard, rivals) rendered through the play camera and the candidate's grade: what the grade does to a signal that draws unlit but is not yet outside the grade.
    public static string Signals(string[] cands, int w, int h)
    {
        var sb = new StringBuilder();
        var urp = UniversalRenderPipeline.asset; var drawer = urp.gpuResidentDrawerMode; var sd = urp.shadowDistance; bool fog = RenderSettings.fog;
        var cam = GameObject.Find("cameras/play").GetComponent<Camera>();
        var hex = new[] { ("cones", "#e03a2f"), ("guard", "#f0a020"), ("rivals", "#8a4fd0") };
        foreach (var c in cands)
        {
            bool none = c.StartsWith("="); var tag = none ? c.Substring(1) : Path.GetFileNameWithoutExtension(c);
            var saved = Apply(none ? "" : c); var quads = new List<GameObject>();
            try
            {
                urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled; RenderSettings.fog = false;
                var f = cam.transform.forward; var t = -cam.transform.position.y / f.y; var focus = cam.transform.position + f * t;
                var right = cam.transform.right; right.y = 0; right.Normalize(); var fh = cam.transform.forward; fh.y = 0; fh.Normalize(); focus -= fh * 17f;
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                for (int i = 0; i < hex.Length; i++)
                {
                    ColorUtility.TryParseHtmlString(hex[i].Item2, out var col);
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "glint-swatch-" + hex[i].Item1; quads.Add(q);
                    UnityEngine.Object.DestroyImmediate(q.GetComponent<Collider>());
                    q.transform.position = focus + right * (i - 1) * 6f + Vector3.up * 0.3f; q.transform.rotation = Quaternion.Euler(90, 0, 0); q.transform.localScale = new Vector3(4, 4, 1);
                    var m = new Material(shader); m.SetColor("_BaseColor", col); q.GetComponent<MeshRenderer>().sharedMaterial = m;
                    q.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                using var cap = new Capture();
                var rt = cap.Target(w, h); var own = cam.targetTexture; cam.targetTexture = rt; cam.ResetAspect();
                var vp = quads.Select(q => cam.WorldToViewportPoint(q.transform.position)).ToArray(); cam.targetTexture = own; cam.ResetAspect();
                cap.Grab(cam, 64, 64);
                var ts = cam.GetComponent<TacticalShadows>();
                using (ts ? ts.Use() : null)
                {
                    var tex = cap.Grab(cam, w, h); File.WriteAllBytes("/Users/sky/Projects/suborned/out/glint-honest/frames/sig-" + tag + ".png", tex.EncodeToPNG());
                    for (int i = 0; i < hex.Length; i++)
                    {
                        int px = Mathf.RoundToInt(vp[i].x * w), py = Mathf.RoundToInt(vp[i].y * h);
                        var p = tex.GetPixel(px, py);
                        sb.AppendLine($"{tag} {hex[i].Item1} {hex[i].Item2} -> ({Mathf.RoundToInt(p.r * 255)},{Mathf.RoundToInt(p.g * 255)},{Mathf.RoundToInt(p.b * 255)}) at {px},{py}");
                    }
                }
            }
            finally { foreach (var q in quads) UnityEngine.Object.DestroyImmediate(q); urp.gpuResidentDrawerMode = drawer; RenderSettings.fog = fog; Restore(saved); }
        }
        return sb.ToString();
    }

    /// Redline L7 in memory: the crossing light's material with and without receiving shadows, the kerb frame (lights in shadow) both ways; the asset is restored and not saved.
    public static string L7(string outDir, int w, int h)
    {
        var path = "Assets/Scenes/HarbourDistrict-crosslight.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        float rs = mat.GetFloat("_ReceiveShadows"); bool kw = mat.IsKeywordEnabled("_RECEIVE_SHADOWS_OFF");
        var urp = UniversalRenderPipeline.asset; var drawer = urp.gpuResidentDrawerMode; var sd = urp.shadowDistance; bool fog = RenderSettings.fog;
        var cam = GameObject.Find("cameras/009-kerb").GetComponent<Camera>();
        int users = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include).Count(r => r.sharedMaterials.Contains(mat));
        try
        {
            urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled; RenderSettings.fog = true;
            using var cap = new Capture(); var ts = cam.GetComponent<TacticalShadows>();
            Directory.CreateDirectory(outDir);
            foreach (var on in new[] { true, false })
            {
                mat.SetFloat("_ReceiveShadows", on ? 1f : 0f); if (on) mat.DisableKeyword("_RECEIVE_SHADOWS_OFF"); else mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
                cap.Grab(cam, 64, 64);
                using (ts ? ts.Use() : null) File.WriteAllBytes(Path.Combine(outDir, $"l7-{(on ? "receive" : "norecv")}.png"), cap.Grab(cam, w, h).EncodeToPNG());
            }
        }
        finally { mat.SetFloat("_ReceiveShadows", rs); if (kw) mat.EnableKeyword("_RECEIVE_SHADOWS_OFF"); else mat.DisableKeyword("_RECEIVE_SHADOWS_OFF"); urp.gpuResidentDrawerMode = drawer; RenderSettings.fog = fog; }
        return $"crosslight renderers in scene: {users}; asset _ReceiveShadows {rs}, keyword _RECEIVE_SHADOWS_OFF {kw}";
    }

    /// Several candidates, one call: each is applied, rendered to <outDir>/<candidate file name>-<view>[-nopeople].png, restored.
    public static string Sweep(string[] cands, string outDir, string[] views, bool showPeople, int w, int h)
    {
        var sb = new StringBuilder();
        foreach (var c in cands) { bool none = c.StartsWith("="); var tag = none ? c.Substring(1) : Path.GetFileNameWithoutExtension(c); Run(none ? "" : c, outDir, tag, views, showPeople, w, h); sb.Append(tag).Append(" "); }
        return sb.ToString();
    }

    /// views: existing camera names under cameras/, or "aerial:px,py,pz,tx,ty,tz,fov[,fog]" (world metres, Unity axes).
    public static string Run(string cand, string outDir, string tag, string[] views, bool showPeople, int w, int h)
    {
        var urp = UniversalRenderPipeline.asset;
        var drawer = urp.gpuResidentDrawerMode; var shadowDistance = urp.shadowDistance; bool fog = RenderSettings.fog;
        var people = GameObject.Find("people");
        var hidden = people && !showPeople ? people.GetComponentsInChildren<Renderer>().Where(r => !r.forceRenderingOff).ToArray() : new Renderer[0];
        var saved = Apply(cand);
        var log = new StringBuilder();
        Directory.CreateDirectory(outDir);
        try
        {
            urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
            foreach (var r in hidden) r.forceRenderingOff = true;
            var vs = Views.Load(HarbourWorldBuilder.ViewsPath);
            using var cap = new Capture();
            bool warm = false;
            foreach (var spec in views)
            {
                var label = spec.Contains("@") ? spec.Substring(0, spec.IndexOf('@')) : null; var name = label != null ? spec.Substring(spec.IndexOf('@') + 1) : spec;
                Camera cam; GameObject temp = null; bool useFog; float sd;
                if (name.StartsWith("aerial:"))
                {
                    var a = name.Substring(7).Split(',').Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                    var src = GameObject.Find("cameras/docks");
                    temp = UnityEngine.Object.Instantiate(src); temp.name = "glint-aerial";
                    cam = temp.GetComponent<Camera>(); cam.enabled = false;
                    temp.transform.position = new Vector3(a[0], a[1], a[2]);
                    temp.transform.rotation = Quaternion.LookRotation(new Vector3(a[3], a[4], a[5]) - temp.transform.position, Vector3.up);
                    cam.fieldOfView = a[6]; cam.nearClipPlane = 1f; cam.farClipPlane = 12000f;
                    useFog = a.Length > 7 && a[7] > 0; sd = 1500f;
                }
                else
                {
                    var go = GameObject.Find("cameras/" + name); cam = go.GetComponent<Camera>();
                    var v = vs.First(x => x.Name == name); useFog = v.Fog; sd = float.IsNaN(v.ShadowDistance) ? shadowDistance : v.ShadowDistance;
                }
                urp.shadowDistance = sd; RenderSettings.fog = useFog;
                if (!warm) { cap.Grab(cam, 64, 64); warm = true; }
                var ts = cam.GetComponent<TacticalShadows>();
                using (ts ? ts.Use() : null)
                {
                    cap.Grab(cam, 64, 64);
                    var file = Path.Combine(outDir, $"{tag}-{label ?? name.Split(':')[0]}{(showPeople ? "" : "-nopeople")}.png");
                    File.WriteAllBytes(file, cap.Grab(cam, w, h).EncodeToPNG());
                    log.AppendLine(file);
                }
                if (temp) UnityEngine.Object.DestroyImmediate(temp);
            }
        }
        finally
        {
            urp.gpuResidentDrawerMode = drawer; urp.shadowDistance = shadowDistance; RenderSettings.fog = fog;
            foreach (var r in hidden) if (r) r.forceRenderingOff = false;
            Restore(saved);
        }
        return log.ToString();
    }
}
