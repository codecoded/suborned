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
        public bool HasPalette(string key) => key != null && palette.ContainsKey(key);
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
        /// its shape or motion cue, so lighting and grade never shift them. The signals don't own their colours: red, amber and violet are
        /// free for the world (Sky, 2026-10-16, which dropped the ΔE2000 >= 20 reservation). No signal visuals exist in the world build yet;
        /// when they do, they take this material and nothing lit.</summary>
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

        /// <summary>Brief 026: a textured ground surface with `seed: true` (pavement, lawn) gets a seeded UV offset and mirror per patch
        /// (MeshKit.SeedUv); returns the material's repeat in metres (1 / its _BaseMap scale, Vela's m_Scale = 1 / tile), 0 = not seeded.
        /// Never on surfaces whose patches overlap (roads, kerbs at junctions): there the world-continuous UVs hide the overlap.</summary>
        /// <summary>Brief 026: the surface a ground-layer element draws in: `ground/<surface>` when look.json has one (plots share their
        /// surface key with the buildings on them, so the textured ground sheet is keyed apart), else the surface itself.</summary>
        public bool Has(string surface) => J.Get(Entry("surfaces"), surface) != null;

        public string Ground(string surface) => surface != null && J.Get(Entry("surfaces"), "ground/" + surface) != null ? "ground/" + surface : surface;

        public float SeededPeriod(string surface)
        {
            if (J.Get(Surface(surface), "seed") is not true) return 0f;
            var m = Mat(surface); var sc = m.HasProperty("_BaseMap") ? m.GetTextureScale("_BaseMap") : Vector2.one;
            return sc.x > 0f ? 1f / sc.x : 0f;
        }

        public const string KitTextures = "Assets/Suborned/Kit/Textures";

        /// <summary>Brief 072-F: a builder colour surface takes the kit detail pair of its kind (look.json `detail` → `details`: Vela's
        /// surface_&lt;kind&gt;_detail_v1 albedo x2 and normal) in URP Lit's own detail slots with _DETAIL_MULX2: texture, not palette (the
        /// base colour is untouched), and no new material. The meshes get box-projected UVs in metres at bake (BoxUv), so the slot's scale is
        /// 1 / the pair's tile. Unknown pair or missing texture = reject.</summary>
        void Detail(Material m, string surface, string pair)
        {
            var d = J.Get(Entry("details"), pair) ?? throw new InvalidDataException($"World: surface `{surface}`: detail `{pair}` not in look.json details");
            Texture2D Tex(string key)
            {
                var name = J.S(d, key);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"{KitTextures}/{name}.png")
                    ?? throw new InvalidDataException($"World: surface `{surface}`: detail texture {name} missing in {KitTextures} (run `bun run kit`)");
            }
            float tile = J.F(d, "tile"), bump = J.F(d, "bump");
            var scale = Vector2.one / Mathf.Max(0.01f, tile);
            m.SetTexture("_DetailAlbedoMap", Tex("albedo")); m.SetTextureScale("_DetailAlbedoMap", scale);
            m.SetTexture("_DetailNormalMap", Tex("normal")); m.SetTextureScale("_DetailNormalMap", scale);
            m.SetFloat("_DetailAlbedoMapScale", 1f); m.SetFloat("_DetailNormalMapScale", bump);
            // the DecoKit cutaway shader (without _DK_CUT it is URP Lit's own passes): its detail path reads the global surface-detail switch
            // (DK_SurfaceDetail.hlsl, LookLab; Redline phase 2, 4.3), which URP Lit cannot. Same properties, same keywords; the
            // [_DETAIL_MULX2] variant is the kit rows' own, so no new variant (a tagged batch took this shader anyway, FadeTags).
            var cutaway = Shader.Find("DecoKit/Lit Cutaway") ?? throw new InvalidDataException("World: shader DecoKit/Lit Cutaway not found (Assets/Suborned/Shaders/DecoKit)");
            m.shader = cutaway;
            m.EnableKeyword("_DETAIL_MULX2");
        }

        /// <summary>A builder colour material carrying a detail pair (072-F): not a kit asset, _DETAIL_MULX2 on. Its meshes need UVs in
        /// metres and tangents (BoxUv).</summary>
        public static bool NeedsBoxUv(Material m) =>
            m && m.IsKeywordEnabled("_DETAIL_MULX2") && !UnityEditor.AssetDatabase.Contains(m);

        /// <summary>Box-projected UVs in world metres from each vertex's normal (the dominant axis picks the plane: walls run along their
        /// face and up, tops lie on plan), then tangents, so a detail pair tiles at its real size on meshes the builder drew without UVs.</summary>
        public static void BoxUv(Mesh mesh)
        {
            var v = mesh.vertices; var n = mesh.normals; var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                var a = new Vector3(Mathf.Abs(n[i].x), Mathf.Abs(n[i].y), Mathf.Abs(n[i].z));
                uv[i] = a.y >= a.x && a.y >= a.z ? new Vector2(v[i].x, v[i].z)
                      : a.x >= a.z ? new Vector2(v[i].z * Mathf.Sign(n[i].x), v[i].y)
                      : new Vector2(-v[i].x * Mathf.Sign(n[i].z), v[i].y);
            }
            mesh.uv = uv;
            mesh.RecalculateTangents();
        }

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
            else if (J.Get(e, "detail") is string pair) Detail(m, surface, pair);
            cache[surface] = m;
            return m;
        }
    }
}
