using System.IO;
using System.Linq;
using Suborned.Play;
using UnityEditor;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Kit pieces with turning parts (contracts/world/kit.json `parts` named `_Orbit`; Splice: AtomFountain_Testbed's Orbit0..2 spin about the vertical
    /// axis through the globe). Every placed instance's part objects, at every LOD (they keep the part's name), get a Spin about the piece's
    /// own vertical axis; each part of a piece its own rate, alternating direction. kit.json gives no rate: 10, 16 and 22 degrees a second
    /// until it does. Not static, so batching never freezes them.</summary>
    public static class KitParts
    {
        static readonly float[] Rates = { 10f, -16f, 22f };
        public static float Rate(int k) => Rates[k % Rates.Length];
        /// <summary>Orbits KitPlacer.Bake stood as their own renderers this build.</summary>
        public static int Baked;

        public static int Spin(Transform root)
        {
            int baked = Baked; Baked = 0;
            var kit = Json.Parse(File.ReadAllText(Path.Combine(HarbourWorldBuilder.RepoRoot, "contracts", "world", "kit.json")));
            int n = 0;
            foreach (var a in J.A(kit, "assets"))
            {
                if (J.Get(a, "parts") == null || J.S(a, "status") != "adopted") continue;
                var parts = J.A(a, "parts").Select(p => (string)p).ToList(); var id = J.S(a, "id");
                foreach (var inst in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "DK_" + id && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)))
                    foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                    {
                        int k = parts.IndexOf(KitLods.Part(t.name)); if (k < 0 || !KitLods.Part(t.name).Contains("_Orbit") || t.GetComponent<Spin>()) continue; // `parts` are any separately drawn parts (robot heads, signal eyes); only orbits turn (kit.json has no spin key yet)
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                        var s = t.gameObject.AddComponent<Spin>(); s.axisPoint = inst.position; s.degreesPerSecond = Rates[k % Rates.Length]; n++;
                    }
            }
            return n + baked;
        }
    }
}
