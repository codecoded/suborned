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

        /// <summary>A §15 palette colour by key (facade kit: sign boards).</summary>
        public Color Palette(string key) => palette.TryGetValue(key ?? "", out var c) ? c : throw new InvalidDataException($"World: palette key `{key}` not in §15 palette");

        public object Entry(string key) => J.Get(table, key) ?? throw new InvalidDataException($"World: look.json has no `{key}`");

        object Surface(string surface) => J.Get(Entry("surfaces"), surface) ?? throw new InvalidDataException($"World: surface `{surface}` not in look.json");

        /// <summary>A surface's base colour (facade kit: megastructure and gate walls keep their look colour).</summary>
        public Color SurfaceColour(string surface) => Colour(Surface(surface));

        public Material Mat(string surface)
        {
            if (surface == null) throw new InvalidDataException("World: element has no surface");
            if (cache.TryGetValue(surface, out var m)) return m;
            var e = Surface(surface);
            m = new Material(lit) { name = surface, enableInstancing = true };
            m.SetColor("_BaseColor", Colour(e));
            m.SetFloat("_Smoothness", J.Get(e, "smoothness") is double s ? (float)s : 0f);
            m.SetFloat("_Metallic", J.Get(e, "metallic") is double mt ? (float)mt : 0f);
            cache[surface] = m;
            return m;
        }
    }
}
