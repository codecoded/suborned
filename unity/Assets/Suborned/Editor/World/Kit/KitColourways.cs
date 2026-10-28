using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Brief 074 (Sky 2026-10-28, "Do building colors"): the building colourways on the kit facades. The export names each facade piece's
    /// colourway (`facades[].colourway`, its building's facade.json wall family); Vela's mapping names, per colourway, the kit material each base slot
    /// is drawn in (same shader and keywords, only the base colour differs, so no SetPass is added). A placed piece or baked column instance has its
    /// renderers' slots swapped (recorded as prefab overrides, as KitSlots does); the column meshes are baked once per layout, never per colourway.
    /// `base`, `civic` and a missing colourway draw the piece as it is. A colourway the mapping lacks, or a material not in the Kit folder = reject.
    /// Base(m) maps a tinted material back to its base one (the shadow proxies keep the base set: the caster pass ignores colour).</summary>
    public static class KitColourways
    {
        static Dictionary<string, Dictionary<string, Material>> map;
        static readonly Dictionary<Material, Material> toBase = new();
        static readonly HashSet<string> keep = new(System.StringComparer.Ordinal) { "base", "civic" };
        public static string Source;
        public static int Swapped, Renderers;
        public static readonly SortedDictionary<string, int> Placed = new(System.StringComparer.Ordinal);

        public static bool Loaded => map != null;
        public static IEnumerable<string> Colourways => map != null ? map.Keys : System.Array.Empty<string>();

        public static void Reset() { map = null; toBase.Clear(); keep.Clear(); keep.Add("base"); keep.Add("civic"); Source = null; Swapped = Renderers = 0; Placed.Clear(); }

        /// <summary>Reads Vela's mapping from the kit catalogue (contracts/world/kit.json `colourways`, schema suborned.colourways/1:
        /// `materials[base material][family]` = the kit material, `keep` = the colourways drawn as they are); none = every piece as it is.</summary>
        public static void Load(string catalogue)
        {
            Reset();
            var root = File.Exists(catalogue) ? Json.Parse(File.ReadAllText(catalogue)) : null;
            if (root == null || J.Get(root, "colourways") is not Dictionary<string, object> cw) { Debug.Log($"World: colourways: none in {catalogue}; the kit facades keep their base materials"); return; }
            if (J.S(cw, "schema") != "suborned.colourways/1") throw new InvalidDataException($"World: {catalogue} colourways schema `{J.S(cw, "schema")}` (want suborned.colourways/1)");
            keep.Clear(); if (J.Get(cw, "keep") is List<object> ks) foreach (var k in ks) keep.Add((string)k);
            map = new Dictionary<string, Dictionary<string, Material>>(System.StringComparer.Ordinal);
            foreach (var b in J.O(J.Get(cw, "materials")))
            {
                var from = AssetDatabase.LoadAssetAtPath<Material>($"{Look.KitMaterials}/{b.Key}.mat")
                    ?? throw new FileNotFoundException($"World: colourways: base material {b.Key} missing in {Look.KitMaterials} (run `bun run kit`)");
                foreach (var f in J.O(b.Value))
                {
                    var to = f.Value as string ?? throw new InvalidDataException($"World: colourways: materials.{b.Key}.{f.Key} is not a material name");
                    var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Look.KitMaterials}/{to}.mat")
                        ?? throw new FileNotFoundException($"World: colourway {f.Key}: kit material {to} missing in {Look.KitMaterials} (run `bun run kit`)");
                    if (mat.shader != from.shader) throw new InvalidDataException($"World: colourway {f.Key}: {to} uses {mat.shader.name}, its base {b.Key} {from.shader.name} (a colourway may change only the base colour)");
                    if (!SameKeywords(mat, from)) throw new InvalidDataException($"World: colourway {f.Key}: {to} keywords [{string.Join(",", mat.shaderKeywords)}] differ from {b.Key}'s [{string.Join(",", from.shaderKeywords)}] (a new keyword set is a new SetPass)");
                    if (!map.TryGetValue(f.Key, out var slots)) map[f.Key] = slots = new Dictionary<string, Material>(System.StringComparer.Ordinal);
                    slots[b.Key] = mat; toBase[mat] = from;
                }
            }
            Source = $"{Path.GetFileName(catalogue)} colourways";
            Debug.Log($"World: colourways: {map.Count} ({string.Join(" ", map.Keys)}) from {catalogue}, {toBase.Count} tinted materials; kept as they are: {string.Join(", ", keep)}");
        }

        static bool SameKeywords(Material a, Material b)
        {
            var x = new HashSet<string>(a.shaderKeywords); var y = new HashSet<string>(b.shaderKeywords);
            return x.SetEquals(y);
        }

        static bool Plain(string cw) => cw == null || keep.Contains(cw);

        /// <summary>The material a slot draws in for a colourway (its own when the colourway leaves it alone).</summary>
        public static Material For(Material own, string colourway)
        {
            if (!own || Plain(colourway) || map == null) return own;
            if (!map.TryGetValue(colourway, out var slots)) throw new InvalidDataException($"World: colourway `{colourway}` is not in the mapping {Source}");
            return slots.TryGetValue(own.name, out var to) ? to : own;
        }

        /// <summary>The base material of a tinted one (itself otherwise).</summary>
        public static Material Base(Material m) => m && toBase.TryGetValue(m, out var b) ? b : m;

        /// <summary>Swaps the colourway's slots on every mesh renderer of a placed instance (all LOD levels), recorded as prefab overrides.</summary>
        public static void Apply(Transform piece, string colourway)
        {
            if (Plain(colourway) || map == null) return;
            if (!map.ContainsKey(colourway)) throw new InvalidDataException($"World: colourway `{colourway}` is not in the mapping {Source}");
            bool any = false;
            foreach (var r in piece.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++) { var to = For(mats[i], colourway); if (to != mats[i]) { mats[i] = to; changed = true; Swapped++; } }
                if (!changed) continue;
                r.sharedMaterials = mats; Renderers++; any = true;
                if (PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            if (any) Placed[colourway] = (Placed.TryGetValue(colourway, out var n) ? n : 0) + 1;
        }

        public static string Summary()
        {
            if (map == null) return "World: colourways: none (base materials)";
            return $"World: colourways from {Source}: {Renderers} renderers, {Swapped} slots swapped; instances per colourway {string.Join(", ", Placed)}";
        }
    }
}
