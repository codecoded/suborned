using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Brief 075-F1 (rules BB-1..4): the ad each placed board shows. Every billboard face bakes the same canonical cell ("mars"), so a board
    /// always takes a key: the export's pick (`posters.placed`: host = the element whose kit ref it is, piece, key, back, slots = base → variant) or,
    /// with no pick, kit.json `posters.defaults[piece]` (the ad the piece showed in the old town). Sheet pieces (AdColumn, SandwichBoard, Marquee_Cinema)
    /// take sheet A (keep the base) or B. Vela's variants (kit.json `posters`, schema suborned.posters/1) share the base's shader and keywords: only the
    /// texture and its offset differ (no new SetPass). As KitLiveries: shared materials only, every LOD, recorded as prefab overrides; shadow proxies
    /// and the colourways-off map see the base. KitPlacer.Place reports every instance (host = its element's name); Finish rejects a pick that no
    /// placed board took. Unknown material, piece or key, or a pick against the catalogue = reject.</summary>
    public static class KitPosters
    {
        sealed class Pick { public string Piece, Key; public Dictionary<string, Material> Slots; }
        static Dictionary<string, Pick> picks;                                   // host → pick
        static Dictionary<string, string> adPieces, sheetPieces, defaults;       // piece → base slot · piece → default key
        static Dictionary<string, Dictionary<string, Material>> adMats;          // key → base name → variant
        static Dictionary<string, Material> sheetB;                               // base name → sheet-B variant
        static readonly Dictionary<Material, Material> variantBase = new();
        static readonly HashSet<string> applied = new(System.StringComparer.Ordinal);
        public static int Picked, Defaulted, SheetA, Renderers, Swapped;
        public static readonly SortedDictionary<string, int> Shown = new(System.StringComparer.Ordinal); // ad or sheet → boards
        public static string Source;

        public static void Reset()
        {
            picks = null; adPieces = sheetPieces = defaults = null; adMats = null; sheetB = null; variantBase.Clear(); applied.Clear(); Shown.Clear();
            Picked = Defaulted = SheetA = Renderers = Swapped = 0; Source = null;
        }

        static Material Variant(string tag, string baseName, string name)
        {
            var from = AssetDatabase.LoadAssetAtPath<Material>($"{Look.KitMaterials}/{baseName}.mat") ?? throw new FileNotFoundException($"World: posters {tag}: base material {baseName} missing in {Look.KitMaterials} (run `bun run kit`)");
            var to = AssetDatabase.LoadAssetAtPath<Material>($"{Look.KitMaterials}/{name}.mat") ?? throw new FileNotFoundException($"World: posters {tag}: material {name} missing in {Look.KitMaterials} (run `bun run kit`)");
            if (to.shader != from.shader) throw new InvalidDataException($"World: posters {tag}: {name} uses {to.shader.name}, its base {baseName} {from.shader.name} (a poster may change only the texture and offset)");
            if (!new HashSet<string>(to.shaderKeywords).SetEquals(from.shaderKeywords)) throw new InvalidDataException($"World: posters {tag}: {name} keywords differ from {baseName}'s (a new keyword set is a new SetPass)");
            variantBase[to] = from;
            return to;
        }

        static Dictionary<string, string> Strings(object o, string what)
        {
            if (o is not Dictionary<string, object> d) throw new InvalidDataException($"World: kit.json posters has no `{what}`");
            var r = new Dictionary<string, string>(System.StringComparer.Ordinal);
            foreach (var kv in d) r[kv.Key] = kv.Value as string ?? throw new InvalidDataException($"World: kit.json posters.{what}.{kv.Key} is not a string");
            return r;
        }

        /// <summary>Reads the catalogue's variants and the export's picks. Called before any piece is placed. With no `posters` in the catalogue
        /// the builder places boards as they are; the export's picks then reject.</summary>
        public static void Load(string catalogue, object posters)
        {
            Reset();
            var root = Json.Parse(File.ReadAllText(catalogue));
            if (J.Get(root, "posters") is not Dictionary<string, object> pc)
            {
                if (posters != null) throw new InvalidDataException($"World: the export has posters but {catalogue} has no `posters`");
                Debug.Log("World: posters: none in the catalogue; boards keep their kit materials"); return;
            }
            if (J.S(pc, "schema") != "suborned.posters/1") throw new InvalidDataException($"World: {catalogue} posters schema `{J.S(pc, "schema")}` (want suborned.posters/1)");
            adPieces = Strings(J.Get(pc, "adPieces"), "adPieces"); sheetPieces = Strings(J.Get(pc, "sheetPieces"), "sheetPieces"); defaults = Strings(J.Get(pc, "defaults"), "defaults");
            var backFace = Strings(J.Get(pc, "backFace"), "backFace");
            adMats = new Dictionary<string, Dictionary<string, Material>>(System.StringComparer.Ordinal);
            foreach (var a in J.O(J.Get(pc, "ads") ?? throw new InvalidDataException("World: kit.json posters has no `ads`")))
            {
                var m = new Dictionary<string, Material>(System.StringComparer.Ordinal);
                foreach (var kv in Strings(J.Get(a.Value, "materials"), $"ads.{a.Key}.materials")) m[kv.Key] = Variant(a.Key, kv.Key, kv.Value);
                adMats[a.Key] = m;
            }
            sheetB = new Dictionary<string, Material>(System.StringComparer.Ordinal);
            if (J.Get(J.Get(pc, "sheets"), "B") is Dictionary<string, object> sb)
                foreach (var kv in Strings(J.Get(sb, "materials"), "sheets.B.materials")) sheetB[kv.Key] = Variant("sheet B", kv.Key, kv.Value);
            foreach (var d in defaults)
            {
                if (!adPieces.TryGetValue(d.Key, out var bs)) throw new InvalidDataException($"World: kit.json posters.defaults names {d.Key}, not an adPiece");
                if (!adMats.TryGetValue(d.Value, out var dm) || !dm.ContainsKey(bs)) throw new InvalidDataException($"World: kit.json posters.defaults {d.Key} → `{d.Value}`: no {bs} variant for that ad");
            }
            foreach (var p in adPieces.Keys) if (!defaults.ContainsKey(p)) throw new InvalidDataException($"World: kit.json posters.defaults has no ad for {p} (every board must show a key)");
            Source = Path.GetFileName(catalogue) + " posters";
            if (posters == null) { Debug.Log($"World: posters: none in the export; every board takes its default ({Source})"); return; }

            picks = new Dictionary<string, Pick>(System.StringComparer.Ordinal);
            foreach (var p in J.A(posters, "placed"))
            {
                string host = J.S(p, "host"), piece = J.S(p, "piece"), key = J.S(p, "key"), back = J.S(p, "back");
                var want = new Dictionary<string, Material>(System.StringComparer.Ordinal);
                if (adPieces.TryGetValue(piece, out var bs))
                {
                    if (!adMats.TryGetValue(key ?? "", out var am) || !am.TryGetValue(bs, out var v)) throw new InvalidDataException($"World: poster {host}: {piece} key `{key}` is not an ad of kit.json posters.ads with a {bs} variant");
                    want[bs] = v;
                    if (piece.StartsWith("Billboard_Pylon_", System.StringComparison.Ordinal) && back != backFace[key]) throw new InvalidDataException($"World: poster {host}: pylon back `{back}`, the catalogue's back face of {key} is {backFace[key]}");
                }
                else if (sheetPieces.TryGetValue(piece, out bs))
                {
                    if (key == "B") want[bs] = sheetB.TryGetValue(bs, out var v) ? v : throw new InvalidDataException($"World: poster {host}: {piece} sheet B, but kit.json posters.sheets.B has no {bs} variant");
                    else if (key != "A") throw new InvalidDataException($"World: poster {host}: {piece} sheet `{key}` (want A or B)");
                }
                else throw new InvalidDataException($"World: poster {host}: {piece} is neither a kit.json posters adPiece nor a sheetPiece");
                // the export's slots must be exactly the catalogue's: base → the key's variant
                var slots = J.Get(p, "slots") as Dictionary<string, object> ?? new Dictionary<string, object>();
                if (slots.Count != want.Count) throw new InvalidDataException($"World: poster {host}: {piece} {key}: slots {{{string.Join(", ", slots.Keys)}}}, the catalogue gives {{{string.Join(", ", want.Keys)}}}");
                foreach (var s in slots)
                    if (!want.TryGetValue(s.Key, out var v) || v.name != s.Value as string) throw new InvalidDataException($"World: poster {host}: {piece} {key}: slot {s.Key} → `{s.Value}`, the catalogue says `{(want.TryGetValue(s.Key, out var w2) ? w2.name : "no such slot")}`");
                if (!picks.TryAdd(host, new Pick { Piece = piece, Key = key, Slots = want })) throw new InvalidDataException($"World: poster {host} listed twice");
            }
            Debug.Log($"World: posters: {picks.Count} picks, {adMats.Count} ads, {variantBase.Count} variant materials from {catalogue}");
        }

        static bool Known(string kitId) => adPieces != null && (adPieces.ContainsKey(kitId) || sheetPieces.ContainsKey(kitId));

        /// <summary>A piece baked into batches cannot take a per-instance swap: reject a board (or a picked host) going that way.</summary>
        public static void Baked(string host, string kitId)
        {
            if (adPieces == null) return;
            if (adPieces.ContainsKey(kitId) || (picks != null && picks.ContainsKey(host)))
                throw new InvalidDataException($"World: poster {host}: {kitId} is baked into batches, where no ad can be swapped (place it as an instance)");
        }

        /// <summary>Called by KitPlacer.Place for every instance: a board takes its pick or its default; a sheet piece its sheet.</summary>
        public static void Placed(Transform piece, string host, string kitId)
        {
            if (!piece) return;
            Pick pick = null;
            if (picks != null && picks.TryGetValue(host, out pick))
            {
                if (pick.Piece != kitId) throw new InvalidDataException($"World: poster {host} is for {pick.Piece}, the builder placed {kitId} there");
                if (!applied.Add(host)) throw new InvalidDataException($"World: poster {host}: two {kitId} placed on one host");
            }
            if (!Known(kitId)) return;
            Dictionary<string, Material> slots; string shown;
            if (pick != null) { slots = pick.Slots; shown = pick.Key; Picked++; }
            else if (adPieces.TryGetValue(kitId, out var bs)) { shown = defaults[kitId]; slots = new Dictionary<string, Material> { [bs] = adMats[shown][bs] }; Defaulted++; }
            else { shown = "A"; slots = null; } // a sheet piece with no pick keeps sheet A
            Shown[shown] = (Shown.TryGetValue(shown, out var c) ? c : 0) + 1;
            if (slots == null || slots.Count == 0) { SheetA++; return; }
            int n = 0;
            foreach (var r in piece.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] && slots.TryGetValue(mats[i].name, out var to)) { mats[i] = to; changed = true; n++; }
                if (!changed) continue;
                r.sharedMaterials = mats; Renderers++;
                if (PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            if (n == 0) throw new InvalidDataException($"World: poster {host}: {kitId} has none of the slots {string.Join(", ", slots.Keys)}");
            Swapped += n;
        }

        /// <summary>Every pick taken exactly once, else reject; then map each variant to its base for the shadow proxies and the colourways-off render
        /// (after KitColourways.Load, which clears that map).</summary>
        public static string Finish()
        {
            if (adPieces == null) return "World: posters: none";
            if (picks != null)
            {
                var missing = new List<string>();
                foreach (var k in picks.Keys) if (!applied.Contains(k)) missing.Add($"{k} ({picks[k].Piece})");
                if (missing.Count > 0) throw new InvalidDataException($"World: posters picked for boards the builder did not place: {string.Join(", ", missing)}");
            }
            foreach (var kv in variantBase) KitColourways.AddBase(kv.Key, kv.Value);
            var shown = new List<string>(); foreach (var kv in Shown) shown.Add($"{kv.Key} {kv.Value}");
            return $"World: posters from {Source}: {Picked} picked, {Defaulted} by default, {SheetA} sheet A as built; {Renderers} renderers, {Swapped} slots swapped; shown: {string.Join(", ", shown)}";
        }
    }
}
