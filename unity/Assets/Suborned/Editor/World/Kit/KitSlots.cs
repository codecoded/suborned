using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>A mass's material slots (export `Mass.materials`: slot, a material name on the piece, → kit material): that mass's
    /// piece draws the slot in the named material; every other use of the piece keeps its own. A slot the piece does not have,
    /// or a material not in the Kit folder, is rejected.</summary>
    public static class KitSlots
    {
        static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{Look.KitMaterials}/{name}.mat")
            ?? throw new FileNotFoundException($"World: kit material {name} missing in {Look.KitMaterials} (run `bun run kit`)");

        /// <summary>The material a slot draws in: its own, or the one `slots` names for it (noted in `used`).</summary>
        public static Material For(Material own, IReadOnlyDictionary<string, string> slots, HashSet<string> used)
        {
            if (slots == null || !own || !slots.TryGetValue(own.name, out var to)) return own;
            used.Add(own.name);
            return Load(to);
        }

        public static void AllUsed(string id, IReadOnlyDictionary<string, string> slots, HashSet<string> used)
        {
            if (slots == null) return;
            foreach (var slot in slots.Keys) if (!used.Contains(slot)) throw new InvalidDataException($"World: {id}: its piece has no material slot `{slot}`");
        }

        /// <summary>Swaps the slots on a placed instance (recorded as prefab overrides, or they are lost on save).</summary>
        public static void Swap(Transform piece, string id, IReadOnlyDictionary<string, string> slots)
        {
            if (slots == null) return;
            var used = new HashSet<string>();
            foreach (var r in piece.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++) { var to = For(mats[i], slots, used); if (to != mats[i]) { mats[i] = to; changed = true; } }
                if (!changed) continue;
                r.sharedMaterials = mats;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            AllUsed(id, slots, used);
        }
    }
}
