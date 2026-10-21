using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>Brief 026 phase 1 (Glint, 2026-10-19): lighting variants shot on Latch's baseline stops without touching the scene on disk.
    /// A variant is briefs/026/glint/variants/&lt;name&gt;.json: sun, ambient multipliers, post grade, SSAO and per-surface material overrides.
    /// It is applied in memory (sun, RenderSettings, a runtime copy of the grade profile, the SSAO feature settings, materials matched by
    /// name), Baseline.Run shoots the pinned cameras into briefs/026/rounds/glint-&lt;name&gt;/ (its sheet sets each frame beside the baseline's),
    /// and everything is put back. Nothing is saved. Menu: Suborned/Render/Glint Rounds (all variants).</summary>
    public static class GlintRounds
    {
        static string Dir => Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "026", "glint", "variants");

        [MenuItem("Suborned/Render/Glint Rounds (all variants)")]
        static void Menu() { foreach (var f in Directory.GetFiles(Dir, "*.json").OrderBy(x => x)) RunVariant(Path.GetFileNameWithoutExtension(f)); }

        public static void Cli() => HarbourWorldBuilder.Batch(Menu);

        public static void RunVariant(string name)
        {
            var file = name.StartsWith("r2-") ? Path.Combine(Dir, "..", "variants-r2", name + ".json") : name.StartsWith("r1-") ? Path.Combine(Dir, "..", "variants-r1", name + ".json") : name.StartsWith("t-") ? Path.Combine(Dir, "..", "scratch", name + ".json") : Path.Combine(Dir, name + ".json");
            var spec = J.O(Json.Parse(File.ReadAllText(file)));
            if (!RenderSession.EnsureScene()) return;
            var undo = new List<Action>();
            try
            {
                Apply(spec, undo);
                Baseline.Run(Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "026", "rounds", "glint-" + name), false);
            }
            finally { for (int i = undo.Count - 1; i >= 0; i--) undo[i](); }
        }

        /// <summary>Applies a variant and shoots one close-up piece (CloseUps) into briefs/026/rounds/glint-&lt;name&gt;/.</summary>
        public static void RunClose(string name, string piece)
        {
            var file = Path.Combine(Dir, "..", "variants-r2", name + ".json");
            var spec = J.O(Json.Parse(File.ReadAllText(file)));
            if (!RenderSession.EnsureScene()) return;
            var undo = new List<Action>();
            try { Apply(spec, undo); CloseUps.Run(Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "026", "rounds", "glint-" + name), piece); }
            finally { for (int i = undo.Count - 1; i >= 0; i--) undo[i](); }
        }

        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : throw new InvalidDataException("GlintRounds: colour " + h);

        static void Apply(Dictionary<string, object> v, List<Action> undo)
        {
            var sun = RenderSettings.sun ?? throw new InvalidOperationException("GlintRounds: no sun");
            if (J.Get(v, "sun") is { } s)
            {
                float i0 = sun.intensity; var c0 = sun.color; undo.Add(() => { sun.intensity = i0; sun.color = c0; });
                if (J.Get(s, "intensity") != null) sun.intensity = J.F(s, "intensity");
                if (J.Get(s, "colour") != null) sun.color = Hex(J.S(s, "colour"));
            }
            if (J.Get(v, "ambient") is { } a)
            {
                Color sk = RenderSettings.ambientSkyColor, eq = RenderSettings.ambientEquatorColor, gr = RenderSettings.ambientGroundColor;
                undo.Add(() => { RenderSettings.ambientSkyColor = sk; RenderSettings.ambientEquatorColor = eq; RenderSettings.ambientGroundColor = gr; });
                float k(string key) => J.Get(a, key) != null ? J.F(a, key) : 1f;
                RenderSettings.ambientSkyColor = Scale(sk, k("sky"), J.Get(a, "skyTint") != null ? Hex(J.S(a, "skyTint")) : (Color?)null);
                RenderSettings.ambientEquatorColor = Scale(eq, k("equator"), null);
                RenderSettings.ambientGroundColor = Scale(gr, k("ground"), null);
            }
            if (J.Get(v, "post") is { } p) Post(p, undo);
            if (J.Get(v, "ssao") is { } ao) Ssao(ao, undo);
            if (J.Get(v, "contact") is Dictionary<string, object> contact) Contact(contact, undo);
            if (J.Get(v, "surfaces") is Dictionary<string, object> surf) Surfaces(surf, undo);
        }

        static Color Scale(Color c, float k, Color? tint)
        {
            var o = new Color(c.r * k, c.g * k, c.b * k, 1f);
            return tint is { } t ? new Color(t.r * k, t.g * k, t.b * k, 1f) : o;
        }

        static void Post(object p, List<Action> undo)
        {
            var vol = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include).First(x => x.isGlobal && x.sharedProfile && x.sharedProfile.TryGet<Tonemapping>(out _));
            var orig = vol.sharedProfile;
            // a deep copy: Instantiate(VolumeProfile) shares its component objects, so overrides would land in the saved grade asset
            var prof = ScriptableObject.CreateInstance<VolumeProfile>(); prof.name = "grade-variant"; prof.hideFlags = HideFlags.HideAndDontSave;
            foreach (var c in orig.components) { var copy = UnityEngine.Object.Instantiate(c); copy.hideFlags = HideFlags.HideAndDontSave; prof.components.Add(copy); }
            vol.sharedProfile = prof;
            undo.Add(() => { vol.sharedProfile = orig; foreach (var c in prof.components) UnityEngine.Object.DestroyImmediate(c); UnityEngine.Object.DestroyImmediate(prof); });
            if (J.Get(p, "tonemapping") != null && prof.TryGet(out Tonemapping tm))
                tm.mode.Override(J.S(p, "tonemapping") == "ACES" ? TonemappingMode.ACES : TonemappingMode.Neutral);
            if (prof.TryGet(out ColorAdjustments ca))
            {
                if (J.Get(p, "postExposure") != null) ca.postExposure.Override(J.F(p, "postExposure"));
                if (J.Get(p, "contrast") != null) ca.contrast.Override(J.F(p, "contrast"));
                if (J.Get(p, "saturation") != null) ca.saturation.Override(J.F(p, "saturation"));
            }
            if (prof.TryGet(out Bloom bl) && J.Get(p, "bloomIntensity") != null) bl.intensity.Override(J.F(p, "bloomIntensity"));
            if (prof.TryGet(out Vignette vg) && J.Get(p, "vignette") != null) vg.intensity.Override(J.F(p, "vignette"));
            if (prof.TryGet(out ShadowsMidtonesHighlights sm))
            {
                if (J.Get(p, "shadowsTint") != null) { var c = Hex(J.S(p, "shadowsTint")); sm.shadows.Override(new Vector4(c.r, c.g, c.b, 0f)); }
                if (J.Get(p, "midtonesTint") != null) { var c = Hex(J.S(p, "midtonesTint")); sm.midtones.Override(new Vector4(c.r, c.g, c.b, 0f)); }
                if (J.Get(p, "highlightsTint") != null) { var c = Hex(J.S(p, "highlightsTint")); sm.highlights.Override(new Vector4(c.r, c.g, c.b, 0f)); }
            }
        }

        static void Ssao(object ao, List<Action> undo)
        {
            var rd = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/PC_Renderer.asset");
            var f = rd.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().First();
            var so = new SerializedObject(f);
            var keys = new Dictionary<string, string> { ["intensity"] = "m_Settings.Intensity", ["radius"] = "m_Settings.Radius", ["falloff"] = "m_Settings.Falloff", ["directLighting"] = "m_Settings.DirectLightingStrength" };
            foreach (var kv in keys)
            {
                if (J.Get(ao, kv.Key) == null) continue;
                var pr = so.FindProperty(kv.Value); float old = pr.floatValue; undo.Add(() => { var s2 = new SerializedObject(f); s2.FindProperty(kv.Value).floatValue = old; s2.ApplyModifiedPropertiesWithoutUndo(); f.Create(); });
                pr.floatValue = J.F(ao, kv.Key);
            }
            foreach (var kv in new Dictionary<string, string> { ["source"] = "m_Settings.Source", ["afterOpaque"] = "m_Settings.AfterOpaque", ["method"] = "m_Settings.AOMethod", ["samples"] = "m_Settings.Samples", ["normalSamples"] = "m_Settings.NormalSamples", ["downsample"] = "m_Settings.Downsample", ["blur"] = "m_Settings.BlurQuality" })
            {
                if (J.Get(ao, kv.Key) == null) continue;
                var pr = so.FindProperty(kv.Value); int old = pr.intValue; undo.Add(() => { var s2 = new SerializedObject(f); s2.FindProperty(kv.Value).intValue = old; s2.ApplyModifiedPropertiesWithoutUndo(); f.Create(); });
                pr.intValue = (int)J.F(ao, kv.Key);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            f.Create();
        }

        /// <summary>Rebuilds the baked contact shade with these light.json `contact` values (widths included) and swaps it in for the shot.</summary>
        static void Contact(Dictionary<string, object> contact, List<Action> undo)
        {
            var old = GameObject.Find("contact") ?? throw new InvalidOperationException("GlintRounds: no `contact` root in the scene (build round 1 first)");
            var root = GameObject.Find(HarbourWorldBuilder.RootName) ?? throw new InvalidOperationException("GlintRounds: no world root");
            var look = new Look(HarbourWorldBuilder.StylePath, HarbourWorldBuilder.LookPath);
            var fld = typeof(Look).GetField("light", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ((Dictionary<string, object>)fld.GetValue(look))["contact"] = contact;
            var world = WorldReader.Load(Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "world", "harbour-district.world.json"));
            old.SetActive(false);
            ContactShade.Build(look, world, root.transform);
            var fresh = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).First(t => t.name == "contact" && t.gameObject != old && t.parent == null).gameObject;
            undo.Add(() => { if (fresh) UnityEngine.Object.DestroyImmediate(fresh); if (old) old.SetActive(true); });
        }

        static void Surfaces(Dictionary<string, object> surf, List<Action> undo)
        {
            var seen = new HashSet<Material>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                foreach (var m in r.sharedMaterials)
                {
                    if (!m || !seen.Add(m)) continue;
                    var key = m.name.Replace(" (Instance)", "");
                    if (!surf.TryGetValue(key, out var e)) continue;
                    var col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : default; float sm = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : 0, mt = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0;
                    undo.Add(() => { if (!m) return; if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col); if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", sm); if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", mt); });
                    if (J.Get(e, "value") != null && m.HasProperty("_BaseColor"))
                    {
                        float k = J.F(e, "value"); var n = new Color(col.r * k, col.g * k, col.b * k, col.a); // RGB scale = HSV value scale, unclamped (a white textured base can go above 1)
                        m.SetColor("_BaseColor", n);
                    }
                    if (J.Get(e, "props") is Dictionary<string, object> pr)
                        foreach (var kv in pr)
                        {
                            if (!m.HasProperty(kv.Key)) continue;
                            float old = m.GetFloat(kv.Key); var pk = kv.Key; undo.Add(() => { if (m) m.SetFloat(pk, old); });
                            m.SetFloat(kv.Key, (float)(double)kv.Value);
                        }
                    if (J.Get(e, "smoothness") != null && m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", J.F(e, "smoothness"));
                    if (J.Get(e, "metallic") != null && m.HasProperty("_Metallic")) m.SetFloat("_Metallic", J.F(e, "metallic"));
                }
        }
    }
}
