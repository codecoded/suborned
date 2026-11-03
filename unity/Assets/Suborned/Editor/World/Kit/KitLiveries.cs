using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Brief 075-C (rule LV-1): the old reel's landmark livery (vermilion lattices, the brick-red sphere hull, the gold SpaceHQ) on placed kit pieces.
    /// The export lists every piece that takes livery (`liveries.placed`: host = the export object holding the kit ref, ref = its key path in it, `kit`,
    /// `kits[i]`, `crown`, `gateway`, or `use` for the piece Unity places for a megastructure by its `landmark:` tag; slots = base material → tinted material).
    /// Vela's tinted materials come from the kit catalogue (contracts/world/kit.json `landmarks`, schema suborned.livery/1); as with the colourways, a tinted
    /// material must share its base's shader and keywords (only the base colour differs: no new SetPass). Each placement site that can carry a livery calls
    /// Apply with its host and ref; Finish rejects a row that no site placed, a row placed twice, or a piece that does not match the row.
    /// Unknown material, unknown key, or a row against the catalogue's rule = reject.</summary>
    public static class KitLiveries
    {
        static Dictionary<string, (string piece, Dictionary<string, Material> slots)> rows; // "host|ref" → row
        static readonly HashSet<string> applied = new(System.StringComparer.Ordinal);
        public static int Renderers, Swapped;
        public static string Source;

        public static void Reset() { rows = null; applied.Clear(); Renderers = Swapped = 0; Source = null; }

        static string Key(string host, string refPath) => host + "|" + refPath;

        /// <summary>Reads the catalogue's tinted materials and the export's placed rows; no `liveries` in the export = every piece as it is.</summary>
        public static void Load(string catalogue, object liveries)
        {
            Reset();
            if (liveries == null) { Debug.Log("World: liveries: none in the export; landmarks keep their kit materials"); return; }
            var root = Json.Parse(File.ReadAllText(catalogue));
            if (J.Get(root, "landmarks") is not Dictionary<string, object> lm) throw new InvalidDataException($"World: the export has liveries but {catalogue} has no `landmarks`");
            if (J.S(lm, "schema") != "suborned.livery/1") throw new InvalidDataException($"World: {catalogue} landmarks schema `{J.S(lm, "schema")}` (want suborned.livery/1)");
            var byName = new Dictionary<string, (string colour, Material from, Material to)>(System.StringComparer.Ordinal); // tinted name → its base
            foreach (var k in J.O(J.Get(lm, "keys")))
            {
                string bn = J.S(k.Value, "base"), tn = J.S(k.Value, "material");
                var from = AssetDatabase.LoadAssetAtPath<Material>($"{Look.KitMaterials}/{bn}.mat") ?? throw new FileNotFoundException($"World: livery {k.Key}: base material {bn} missing in {Look.KitMaterials} (run `bun run kit`)");
                var to = AssetDatabase.LoadAssetAtPath<Material>($"{Look.KitMaterials}/{tn}.mat") ?? throw new FileNotFoundException($"World: livery {k.Key}: material {tn} missing in {Look.KitMaterials} (run `bun run kit`)");
                if (to.shader != from.shader) throw new InvalidDataException($"World: livery {k.Key}: {tn} uses {to.shader.name}, its base {bn} {from.shader.name} (a livery may change only the base colour)");
                if (!new HashSet<string>(to.shaderKeywords).SetEquals(from.shaderKeywords)) throw new InvalidDataException($"World: livery {k.Key}: {tn} keywords differ from {bn}'s (a new keyword set is a new SetPass)");
                byName[tn] = (k.Key, from, to);
                KitColourways.AddBase(to, from); // shadow proxies and the colourways-off render map it back like a colourway
            }
            var pieces = J.Get(lm, "pieces") as Dictionary<string, object>;
            rows = new Dictionary<string, (string, Dictionary<string, Material>)>(System.StringComparer.Ordinal);
            foreach (var p in J.A(liveries, "placed"))
            {
                string host = J.S(p, "host"), rf = J.S(p, "ref"), piece = J.S(p, "piece");
                var slots = new Dictionary<string, Material>(System.StringComparer.Ordinal);
                foreach (var s in J.O(J.Get(p, "slots")))
                {
                    var tn = s.Value as string;
                    if (tn == null || !byName.TryGetValue(tn, out var m)) throw new InvalidDataException($"World: livery {host} {rf}: slot {s.Key} → `{tn}` is not a kit.json landmarks material");
                    if (m.from.name != s.Key) throw new InvalidDataException($"World: livery {host} {rf}: slot {s.Key} → {tn}, whose base is {m.from.name}");
                    // the export's row must match the catalogue's rule for the piece (kit.json landmarks.pieces[piece][base] = colour)
                    if (pieces == null || J.Get(pieces, piece) is not Dictionary<string, object> rule || J.S(rule, s.Key) != m.colour)
                        throw new InvalidDataException($"World: livery {host} {rf}: {piece} slot {s.Key} → {m.colour}, but kit.json landmarks.pieces says `{(pieces != null && J.Get(pieces, piece) is Dictionary<string, object> r2 ? J.S(r2, s.Key) : null)}`");
                    slots[s.Key] = m.to;
                }
                if (!rows.TryAdd(Key(host, rf), (piece, slots))) throw new InvalidDataException($"World: livery {host} {rf} listed twice");
            }
            Source = Path.GetFileName(catalogue) + " landmarks";
            Debug.Log($"World: liveries: {rows.Count} placed pieces, {byName.Count} tinted materials ({string.Join(", ", byName.Keys)}) from {catalogue}");
        }

        /// <summary>Swaps a placed instance's slots if the export gives its host and ref a livery (all LOD levels; recorded as prefab overrides).</summary>
        public static Transform Apply(Transform piece, string host, string refPath, string kitId)
        {
            if (rows == null || !piece) return piece;
            var key = Key(host, refPath);
            if (!rows.TryGetValue(key, out var row)) return piece;
            if (row.piece != kitId) throw new InvalidDataException($"World: livery {host} {refPath} is for {row.piece}, the builder placed {kitId} there");
            if (!applied.Add(key)) throw new InvalidDataException($"World: livery {host} {refPath} placed twice");
            int n = 0;
            foreach (var r in piece.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] && row.slots.TryGetValue(mats[i].name, out var to)) { mats[i] = to; changed = true; n++; }
                if (!changed) continue;
                r.sharedMaterials = mats; Renderers++;
                if (PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            if (n == 0) throw new InvalidDataException($"World: livery {host} {refPath}: {kitId} has none of the slots {string.Join(", ", row.slots.Keys)}");
            Swapped += n;
            return piece;
        }

        /// <summary>Every row placed exactly once, else reject.</summary>
        public static string Finish()
        {
            if (rows == null) return "World: liveries: none";
            var missing = new List<string>();
            foreach (var k in rows.Keys) if (!applied.Contains(k)) missing.Add(k.Replace('|', ' '));
            if (missing.Count > 0) throw new InvalidDataException($"World: liveries listed in the export but placed by no site: {string.Join(", ", missing)}");
            return $"World: liveries from {Source}: {rows.Count} pieces, {Renderers} renderers, {Swapped} slots swapped";
        }
    }
}
