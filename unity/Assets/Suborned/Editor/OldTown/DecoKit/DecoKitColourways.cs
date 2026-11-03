#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The city's colour plan, as material swaps on the placed instances (the kit's prefabs stay neutral). Colour goes
    /// by ensemble, never building by building: a wall or a crescent is one composition, a district one hue family.
    /// Walls stay within a breath of cream; the strong colour runs along the horizontal lines (the shop fascia, the sill
    /// bands) and on the landmarks. Warm at the civic centre, cooling toward the sea, earthy toward the gardens.
    /// A scheme is a '+' list of layers:
    ///   plan     the ensembles below (frontage pieces carry DecoKitFrontage's "#run.segment"; position picks the ensemble)
    ///   soft | muted  the high street's painted buildings (HighStreet below) at two strengths; without either it stays cream
    ///   livery   landmark lattices in vermilion, signature landmarks in their own paint, the massing's accent in coral
    ///   <Landmark>.<kit material>=<RRGGBB>/<metallic>/<smoothness>   a finish study on one landmark's material
    ///   clear    fog density x 0.45; grade: +15 saturation on an unsaved copy of the grade: studies only
    /// Idempotent: a painted scene is first put back to the kit's materials, so a scheme can be re-applied.
    internal static class DecoKitColourways
    {
        readonly struct Ensemble
        {
            internal readonly string Wall, Fascia, Sills;  // sRGB; null = the kit's own colour
            internal Ensemble(string wall, string fascia, string sills) { Wall = wall; Fascia = fascia; Sills = sills; }
        }

        static readonly Ensemble Boulevard = new(null, "#C8553D", null);            // cream spine, one coral ribbon both sides
        static readonly Ensemble Circus = new("#EBDCB5", "#1F6C67", "#C8553D");     // Naples-cream ring, the spine's accents inverted
        static readonly Ensemble Harbour = new("#E2E8E0", "#1F7F7A", "#1F7F7A");    // sea-glass cream, teal
        static readonly Ensemble Gardens = new("#E8D6C4", "#B9653F", "#8FA88A");    // sand-rose, terracotta, sage
        static readonly Ensemble Southside = new("#EEE3C9", "#D9962B", "#2F7E86");  // warm white, marigold, petrol teal: the new town

        // The boulevard (the high street): its rendered streamline buildings are painted, its stone ones (the deco blocks,
        // the civic hall, the corner towers) stay limestone as anchors. Every other building stays cream, so two colours
        // never meet; the colours come in a fixed order down both walls and the coral fascia runs unbroken across them all.
        // Warm Mediterranean render with one cool note, close in value so no building jumps forward; a painted
        // building's eyebrows and canopy take a lighter shade of its colour (Lighter).
        static readonly string[] Soft = { "#E8B793", "#B7CDB4", "#DDA596", "#E2C07A" };   // apricot, celadon, terracotta-rose, ochre
        static readonly string[] Quiet = { "#EFC9A8", "#CBDCC6", "#E8BDB0", "#EDD49A" };  // the same, sun-faded
        static readonly string[] Stone = { "DecoBay_", "DecoCorner_", "DecoEnd_", "Hall", "CornerTower_" };

        static readonly string[] Walls = { "DK_Limestone_Tex", "DK_Render_White" };
        const string Fascia = "DK_Enamel_Teal", Sills = "DK_Enamel_Seafoam", Lattice = "DK_Steel_Ivory", Trim = "DK_Enamel_Ivory";
        const string Vermilion = "#B8432E", MassAccent = "#C8553D";
        const int MassAccentSlot = 4;  // DecoKitCityMass.Slots: cream, ivory, limestone, glass, teal
        static readonly string[] LiveryCats = { "Landmarks", "Skyport", "Sky" };
        // signature landmarks: (kit material, colour) over the livery (SpaceHQ's gold and chrome is in the kit itself)
        static readonly Dictionary<string, (string kit, string hex)[]> Signature = new()
        {
            ["SphereStation"] = new[] { ("DK_Hull_Ivory", "#9C2E2B") },  // deep brick red against the teal sky (pastel #E4877A read salmon)
            // the flagship's showpiece: the richest gold in the city, brighter and a little rough; its metals mirror a
            // warm vault of their own (DecoKitShowpiece), not the teal sky that turns gold olive and chrome teal
            ["SpaceHQ"] = new[] { ("DK_Gold", "#F2C14E/1/0.45") },
        };

        static readonly Regex Name = new(@"^DK_(.+?)(?: #(\d+)\.(\d+))?$"), Painted = new(@"^(DK_.+?)_[0-9A-F]{6}(?:_m\d+s\d+)?$");
        static readonly Dictionary<(Material, string), Material> variants = new();

        internal static void Apply(string scheme)
        {
            var layers = new HashSet<string>(scheme.Split('+', System.StringSplitOptions.RemoveEmptyEntries));
            var category = DecoKitData.Assets().assets.ToDictionary(a => a.name, a => a.category);
            var frontage = new List<(Transform t, int run, int seg, string piece, Ensemble e)>();
            int landmarks = 0, slots = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID))
            {
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)) continue;
                var m = Name.Match(t.name);
                if (!m.Success) continue;
                if (m.Groups[2].Success)
                    frontage.Add((t, int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), m.Groups[1].Value, Of(t.position)));
                else if (category.TryGetValue(m.Groups[1].Value, out var cat) && LiveryCats.Contains(cat))
                {
                    var own = Signature.GetValueOrDefault(m.Groups[1].Value) ?? System.Array.Empty<(string kit, string hex)>();
                    var finish = layers.Where(l => l.StartsWith(m.Groups[1].Value + ".") && l.Contains('='))
                                       .Select(l => l[(l.IndexOf('.') + 1)..].Split('=')).ToDictionary(f => f[0], f => "#" + f[1]);
                    slots += Swap(t.gameObject, n => finish.GetValueOrDefault(n) ?? (!layers.Contains("livery") ? null
                        : own.FirstOrDefault(o => o.kit == n).hex ?? (n == Lattice ? Vermilion : null)));
                    landmarks++;
                }
            }
            var paint = HighStreet(frontage, layers.Contains("soft") ? Soft : layers.Contains("muted") ? Quiet : null);
            foreach (var f in frontage)
            {
                var e = f.e;
                string wall = paint.TryGetValue((f.run, f.seg), out var hex) ? hex : e.Wall, trim = hex != null ? Lighter(hex) : null;
                slots += Swap(f.t.gameObject, n => !layers.Contains("plan") ? null
                    : Walls.Contains(n) ? wall : n == Trim ? trim : n == Fascia ? e.Fascia : n == Sills ? e.Sills : null);
            }
            slots += CityMass(layers.Contains("livery"));
            if (layers.Contains("grade")) Grade(15f);
            if (layers.Contains("clear")) RenderSettings.fogDensity *= .45f;
            AssetDatabase.SaveAssets();
            var census = frontage.GroupBy(f => f.e.Equals(Boulevard) ? "boulevard" : f.e.Equals(Circus) ? "circus" : f.e.Equals(Harbour) ? "harbour" : f.e.Equals(Southside) ? "southside" : "gardens")
                                 .Select(g => $"{g.Key} {g.Count()}");
            Debug.Log($"[DecoKit] colourway {scheme}: {frontage.Count} frontage pieces ({string.Join(", ", census)}), {paint.Count} " +
                      $"high-street buildings painted, {landmarks} landmarks, {slots} slots painted, {variants.Count} paint materials");
        }

        /// The high street's painted buildings, walking each boulevard run in order: a rendered building is painted when
        /// the one before it is not, so painted buildings always have cream or stone neighbours; stone (most of a
        /// building's pieces in a Stone class) is never painted. The palette runs on in order from wall to wall.
        static Dictionary<(int, int), string> HighStreet(List<(Transform t, int run, int seg, string piece, Ensemble e)> frontage, string[] palette)
        {
            var paint = new Dictionary<(int, int), string>();
            if (palette == null) return paint;
            int k = 0;
            foreach (var run in frontage.GroupBy(f => f.run).OrderBy(g => g.Key))
            {
                bool previous = false;
                foreach (var seg in run.GroupBy(f => f.seg).OrderBy(g => g.Key))
                {
                    bool boulevard = seg.Count(f => f.e.Equals(Boulevard)) * 2 > seg.Count();
                    bool stone = seg.Count(f => Stone.Any(f.piece.StartsWith)) * 2 > seg.Count();
                    previous = boulevard && !stone && !previous;
                    if (previous) paint[(run.Key, seg.Key)] = palette[k++ % palette.Length];
                }
            }
            return paint;
        }

        /// A painted building's eyebrows and canopy: halfway (linear) from its wall colour to the kit's ivory, so the
        /// building reads as one tonal mass (on a streamline bay the eyebrows outweigh the wall between the ribbons).
        static string Lighter(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            ColorUtility.TryParseHtmlString("#EFE8D6", out var ivory);
            return "#" + ColorUtility.ToHtmlStringRGB(Color.Lerp(c.linear, ivory.linear, .5f).gamma);
        }

        /// The ensemble a frontage piece belongs to, from its pivot (the facade's front-centre): the circus crescents
        /// (R 35), the terrace walls facing the harbour (x 46), Southside, the garden walls (x -46 and the west
        /// terrace), else the boulevard's two walls and their short returns.
        static Ensemble Of(Vector3 p)
        {
            if (Mathf.Abs(new Vector2(p.x - P.Circus.x, p.z - P.Circus.z).magnitude - P.CrescentR) < 1.5f) return Circus;
            if (p.x > P.EastBack - 6f) return Harbour;
            if (p.z < DecoKitStreetLayout.Z.x && DecoKitSouthside.Contains(p)) return Southside;
            if (p.x < P.WestBack + 6f) return Gardens;
            return Boulevard;
        }

        /// Sets each slot to what the rule names (a colour's variant of the slot's kit material, or the kit material).
        static int Swap(GameObject go, System.Func<string, string> rule)
        {
            int count = 0;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var kit = Kit(mats[i]);
                    var hex = rule(kit.name);
                    var want = hex == null ? kit : Variant(kit, hex);
                    if (hex != null) count++;
                    if (want == mats[i]) continue;
                    mats[i] = want;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
            return count;
        }

        static int CityMass(bool livery)
        {
            int count = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("CityMass_")))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = Kit(mats[i]);
                if (livery && mats.Length > MassAccentSlot) { mats[MassAccentSlot] = Variant(mats[MassAccentSlot], MassAccent); count++; }
                r.sharedMaterials = mats;
            }
            return count;
        }

        /// The kit material a (possibly painted) slot started from.
        static Material Kit(Material m)
        {
            var p = m != null ? Painted.Match(m.name) : Match.Empty;
            return p.Success ? AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/{p.Groups[1].Value}.mat") ?? m : m;
        }

        /// The kit material with a new _BaseColor. A textured base (limestone) is tinted so the texture's mean lands on
        /// the colour: the stone's variation survives, the hue is the paint's.
        static Material Variant(Material source, string spec)
        {
            if (variants.TryGetValue((source, spec), out var v)) return v;
            var part = spec.Split('/');  // "#RRGGBB" or "#RRGGBB/metallic/smoothness"
            string hex = part[0], finish = "";
            float metallic = 0f, smooth = 0f;
            if (part.Length == 3)
            {
                metallic = float.Parse(part[1], System.Globalization.CultureInfo.InvariantCulture);
                smooth = float.Parse(part[2], System.Globalization.CultureInfo.InvariantCulture);
                finish = $"_m{metallic * 100f:0}s{smooth * 100f:0}";
            }
            ColorUtility.TryParseHtmlString(hex, out var want);
            var tint = want;
            if (source.GetTexture("_BaseMap") is Texture2D tex)
            {
                Color mean = Mean(tex).linear, w = want.linear;
                tint = new Color(Mathf.Min(1f, w.r / mean.r), Mathf.Min(1f, w.g / mean.g), Mathf.Min(1f, w.b / mean.b)).gamma;
            }
            var dir = $"{GeneratedDir}/Paint";
            EnsureFolder(dir);
            var path = $"{dir}/{source.name}_{hex.TrimStart('#')}{finish}.mat";
            v = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (v == null) AssetDatabase.CreateAsset(v = new Material(source), path);
            else { v.shader = source.shader; v.CopyPropertiesFromMaterial(source); }  // the source's shader may have moved on
            tint.a = source.GetColor("_BaseColor").a;
            v.SetColor("_BaseColor", tint);  // SetColor takes sRGB, as DecoKitMaterials does
            if (finish != "") { v.SetFloat("_Metallic", metallic); v.SetFloat("_Smoothness", smooth); }
            EditorUtility.SetDirty(v);
            return variants[(source, spec)] = v;
        }

        /// Mean albedo (sRGB) of a texture, from its source file (imported textures are not readable).
        static Color Mean(Texture2D tex)
        {
            var img = new Texture2D(2, 2);
            img.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(tex)));
            var px = img.GetPixels32();
            Object.DestroyImmediate(img);
            double r = 0, g = 0, b = 0;
            for (int i = 0; i < px.Length; i += 7) { r += px[i].r; g += px[i].g; b += px[i].b; }
            double n = System.Math.Ceiling(px.Length / 7.0) * 255.0;
            return new Color((float)(r / n), (float)(g / n), (float)(b / n));
        }

        /// A saturation lift on an unsaved copy of the scene's grade (the profile asset stays as built).
        static void Grade(float lift)
        {
            var volume = Object.FindAnyObjectByType<Volume>();
            if (volume == null) return;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();  // components copied too: they are sub-assets
            foreach (var comp in volume.sharedProfile.components) profile.components.Add(Object.Instantiate(comp));
            if (profile.TryGet<ColorAdjustments>(out var c)) c.saturation.Override(c.saturation.value + lift);
            volume.sharedProfile = profile;
        }
    }
}
#endif
