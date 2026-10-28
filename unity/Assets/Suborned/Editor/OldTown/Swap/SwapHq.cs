#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.OldTown.Editor.DecoKit;
using UnityEditor;
using UnityEngine;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap, second build (Latch; Sky 2026-10-29: "SpaceHQ: ours"): our SpaceHQ_Portal (the atom crown, the drum) placed from our
    /// kit where the plan's "ours" form has it (plan (0, 244), ground x +-40, y 204..284, base 0.18, yaw as the 0.5.2 export: kit yaw pi/2, Unity
    /// yaw -90 as KitPlacer), finished in the old materials (each of our kit materials swapped for the old DK_* of the same name, or the
    /// nearest old one: our windows to the old dark glass ribbons, the crown gold to the old HQ gold), with the old showpiece probe the old
    /// HQ had (DecoKitShowpiece), so it sits in the old Boulevard vista under the old grade. The old gold HQ stands too, off (the switch).
    /// The plan's form stays "old" (station beside the HQ at (-95, 253)): the "ours" form, with the station in the drum, fails the checker
    /// (S-P1, S-S1, S-C1, L-1, L-5, SF-9, SF-12; notes.md "Swap: play look" / SpaceHQ), so the network is the plan's and the portal is the look.</summary>
    internal static class SwapHq
    {
        static readonly Dictionary<string, string> Alias = new()
        {
            ["DK_Window_Teal"] = "DK_GlassDark", ["DK_Window_Tower"] = "DK_GlassDark", ["DK_Crown_Gold"] = "DK_Gold", ["DK_Enamel_Coral17"] = "DK_Enamel_Coral",
            ["DK_Enamel_Flamingo"] = "DK_Enamel_Coral", ["DK_Enamel_Sunshine"] = "DK_Enamel_Butter", ["DK_Enamel_Turquoise"] = "DK_Enamel_Seafoam",
            ["DK_Beacon_Rose"] = "DK_Neon_Coral", ["DK_Lamp_Deco"] = "DK_Lamp",
        };

        internal static GameObject Place(OldTownSwap.Report rep, Transform parent, float skyGain)
        {
            var guid = AssetDatabase.FindAssets("DK_SpaceHQ_Portal t:Prefab", new[] { "Assets/Suborned/Kit/Prefabs" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileNameWithoutExtension(p) == "DK_SpaceHQ_Portal").OrderBy(p => p.Contains("/Built/") ? 0 : 1).FirstOrDefault();
            if (guid == null) throw new FileNotFoundException("[Swap] our DK_SpaceHQ_Portal prefab not found under Assets/Suborned/Kit/Prefabs (NP-1)");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(guid);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(OldTownSwap.Old3(new Vector2(0f, 244f), .18f), Quaternion.Euler(0f, -90f, 0f));
            go.name = "DK_SpaceHQ_Portal";
            int swapped = 0; var missing = new SortedSet<string>(); var cache = new Dictionary<string, Material>();
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (!mats[i]) continue;
                    var name = Alias.TryGetValue(mats[i].name, out var a) ? a : mats[i].name;
                    if (!cache.TryGetValue(name, out var m)) cache[name] = m = AssetDatabase.LoadAssetAtPath<Material>($"{DecoKitData.MaterialDir}/{name}.mat");
                    if (m) { mats[i] = m; swapped++; } else missing.Add(mats[i].name);
                }
                r.sharedMaterials = mats;
            }
            DecoKitShowpiece.Apply("SpaceHQ_Portal", skyGain);
            var probe = GameObject.Find("SpaceHQ_Portal showpiece probe"); if (probe) probe.transform.SetParent(go.transform, true);  // the switch takes the probe with it
            go.name = SwapSwitches.OursHq;
            var b = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
            rep.Log($"SpaceHQ ours: {Path.GetFileName(Path.GetDirectoryName(guid))}/DK_SpaceHQ_Portal at plan (0, 244), yaw -90, base 0.18; top {b.max.y:0.0} m, ground x {b.min.x:0.0}..{b.max.x:0.0}, y {b.min.z - OldTownSwap.DZ:0.0}..{b.max.z - OldTownSwap.DZ:0.0}; " +
                    $"{swapped} material slots to the old materials ({string.Join(", ", cache.Where(k => k.Value).Select(k => k.Key))})" + (missing.Count > 0 ? $"; kept ours (no old match): {string.Join(", ", missing)}" : "") + "; the old showpiece probe");
            return go;
        }
    }
}
#endif
