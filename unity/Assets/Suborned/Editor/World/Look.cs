using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Materials and light colours from the §15 palette (contracts/style/suborned.style.json) through the builder's
    /// look table (look.json: surface → palette key, value, smoothness, metallic). URP Lit; unknown surface = reject.
    /// Materials are cached per build and saved inside the scene with the meshes.</summary>
    public sealed class Look
    {
        readonly Dictionary<string, Color> palette = new();
        readonly object table;
        readonly object light;
        readonly Dictionary<string, Material> cache = new();
        readonly Shader lit;

        public Look(string stylePath, string lookPath)
        {
            var style = Json.Parse(File.ReadAllText(stylePath));
            foreach (var kv in J.O(J.Get(style, "palette")))
            {
                var hex = J.S(kv.Value, "hex");
                if (!ColorUtility.TryParseHtmlString(hex, out var c)) throw new InvalidDataException($"World: palette {kv.Key} hex {hex}");
                palette[kv.Key] = c;
            }
            lookPath = LookArg() ?? lookPath;
            table = Json.Parse(File.ReadAllText(lookPath));
            // §15 keys the style contract lacks; the contract wins where it has the key.
            if (J.Get(table, "palette") is Dictionary<string, object> extra)
                foreach (var kv in extra)
                {
                    if (kv.Value is not Dictionary<string, object> || palette.ContainsKey(kv.Key)) continue;
                    var hex = J.S(kv.Value, "hex");
                    if (!ColorUtility.TryParseHtmlString(hex, out var c)) throw new InvalidDataException($"World: look palette {kv.Key} hex {hex}");
                    palette[kv.Key] = c;
                }
            // Lighting data (light.json, beside look.json): its keys and palette entries win over look.json's.
            var lightPath = HarbourWorldBuilder.LightPath;
            if (File.Exists(lightPath))
            {
                light = Json.Parse(File.ReadAllText(lightPath));
                foreach (var kv in J.O(J.Get(light, "palette")))
                {
                    var hex = J.S(kv.Value, "hex");
                    if (!ColorUtility.TryParseHtmlString(hex, out var c)) throw new InvalidDataException($"World: light palette {kv.Key} hex {hex}");
                    palette[kv.Key] = c;
                }
            }
            lit = Shader.Find("Universal Render Pipeline/Lit") ?? throw new InvalidDataException("World: URP Lit shader not found");
        }

        /// <summary>Optional `-look &lt;path&gt;` batch argument (relative to the repo root): a variant look table instead of look.json.</summary>
        static string LookArg()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-look");
            return i >= 0 && i + 1 < args.Length ? Path.GetFullPath(Path.Combine(HarbourWorldBuilder.RepoRoot, args[i + 1])) : null;
        }

        /// <summary>Palette colour for a look entry: palette hex scaled in HSV value.</summary>
        public Color Colour(object entry)
        {
            var key = J.S(entry, "palette");
            if (key == null || !palette.TryGetValue(key, out var c)) throw new InvalidDataException($"World: palette key `{key}` not in §15 palette");
            Color.RGBToHSV(c, out var h, out var s, out var v);
            return Color.HSVToRGB(h, s, Mathf.Clamp01(v * J.F(entry, "value")));
        }

        /// <summary>A light.json entry or null (sky gradient, post, reflection).</summary>
        public object Light(string key) => light != null ? J.Get(light, key) : null;

        /// <summary>A §15 palette colour by key (facade kit: sign boards).</summary>
        public Color Palette(string key) => palette.TryGetValue(key ?? "", out var c) ? c : throw new InvalidDataException($"World: palette key `{key}` not in §15 palette");

        public object Entry(string key) => (light != null ? J.Get(light, key) : null) ?? J.Get(table, key) ?? throw new InvalidDataException($"World: look.json has no `{key}`");

        object Surface(string surface) => J.Get(Entry("surfaces"), surface) ?? throw new InvalidDataException($"World: surface `{surface}` not in look.json");

        /// <summary>A surface's base colour (facade kit: megastructure and gate walls keep their look colour).</summary>
        public Color SurfaceColour(string surface) => Colour(Surface(surface));

        /// <summary>URP Lit, transparent alpha blend: the surface's colour at `alpha` coverage, highlights kept (glass).</summary>
        static void Transparent(Material m, float alpha)
        {
            var c = m.GetColor("_BaseColor"); c.a = alpha; m.SetColor("_BaseColor", c);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_AlphaClip", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false); m.SetShaderPassEnabled("ShadowCaster", true);
        }

        /// <summary>Signal rule (Sky, 2026-10-13, brief 007 Decisions): cones, guard and rival markers draw unlit and emissive, each with
        /// its shape or motion cue, so lighting and grade never shift them; the ΔE2000 >= 20 gate is measured on the unlit palette against
        /// the signal colours (style contract `signal_colours`). No signal visuals exist in the world build yet; when they do, they take
        /// this material and nothing lit.</summary>
        public static Material UnlitSignal(string name, Color colour)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? throw new InvalidDataException("World: URP Unlit shader not found")) { name = "signal/" + name };
            m.SetColor("_BaseColor", colour);
            return m;
        }

        /// <summary>The district water and the horizon sea (one material, so they never seam): Suborned/Water with the look's `water`
        /// colour and smoothness, a grazing colour of the water mixed 55% toward the fog (haze) colour, and the haze it fades into.</summary>
        Material Water(object e)
        {
            var shader = Shader.Find("Suborned/Water") ?? throw new InvalidDataException("World: shader Suborned/Water not found (Assets/Suborned/Shaders)");
            var c = Colour(e); var haze = Colour(Entry("fog"));
            var m = new Material(shader) { name = "water", enableInstancing = true };
            m.SetColor("_BaseColor", c);
            m.SetColor("_Horizon", Color.Lerp(c, haze, 0.55f));
            m.SetColor("_Haze", haze);
            m.SetFloat("_Smoothness", J.Get(e, "smoothness") is double s ? (float)s : 0.6f);
            m.SetFloat("_FadeFrom", 1e7f); m.SetFloat("_FadeTo", 2e7f); // no fade until the horizon sets it
            return m;
        }

        /// <summary>The sea fades into `haze` from `from` to `to` metres about `centre` (plan); the district water shares the material.</summary>
        public void SeaFade(Vector2 centre, float from, float to, Color haze)
        {
            var m = Mat("water");
            m.SetVector("_FadeCentre", new Vector4(centre.x, centre.y, 0, 0));
            m.SetFloat("_FadeFrom", from); m.SetFloat("_FadeTo", to);
            m.SetColor("_Haze", haze);
        }

        public const string KitMaterials = "Assets/Suborned/Kit/Materials";

        /// <summary>Metres per repeat of a surface's texture laid on plan (look.json `tile`, with `material`); 0 = the surface has none.</summary>
        public float Tile(string surface) => J.Get(Surface(surface), "tile") is double t ? (float)t : 0f;

        public Material Mat(string surface)
        {
            if (surface == null) throw new InvalidDataException("World: element has no surface");
            if (cache.TryGetValue(surface, out var m)) return m;
            var e = Surface(surface);
            if (surface == "water") return cache[surface] = Water(e);
            if (J.Get(e, "material") is string piece) // a kit material as it is (textured paving); surfaces naming one share its batches
                return cache[surface] = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>($"{KitMaterials}/{piece}.mat")
                    ?? throw new InvalidDataException($"World: surface `{surface}`: kit material {piece} missing in {KitMaterials} (run `bun run kit`)");
            m = new Material(lit) { name = surface, enableInstancing = true };
            m.SetColor("_BaseColor", Colour(e));
            m.SetFloat("_Smoothness", J.Get(e, "smoothness") is double s ? (float)s : 0f);
            m.SetFloat("_Metallic", J.Get(e, "metallic") is double mt ? (float)mt : 0f);
            if (J.Get(e, "alpha") is double al) Transparent(m, (float)al); // glazing (the Parade gallery canopies)
            cache[surface] = m;
            return m;
        }
    }
}
