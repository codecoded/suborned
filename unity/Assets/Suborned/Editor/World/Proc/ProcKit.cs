using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Shared state of the procedural facades (Vela's grammar, the default for every building): detail tiers and the
    /// vertex tallies. Every facade is built at the hero tier (2): the tactical camera can stand anywhere in the district, so play
    /// zoom must look and cost the same on every block (Redline 007 C1; the old tiers fell off with distance from the export focus
    /// and a hard-coded probe street, which left SpaceHQ on the cheapest facades). Distance detail belongs to LODGroups, not to a
    /// baked distance from fixed points. Tiers 0 and 1 stay in the grammar for LOD levels.</summary>
    public static class ProcKit
    {
        /// <summary>Profile end caps (hidden against neighbours nearly everywhere; off keeps the vertex count down).</summary>
        public const bool ProfileCaps = false;
        /// <summary>The tier every facade is built at (2 = hero: neon tubes, chrome rails, canopy stripes).</summary>
        public const int Tier = 2;

        public static readonly Dictionary<string, long> Cost = new();
        public static readonly long[] VertsByTier = new long[3];
        public static readonly int[] MassesByTier = new int[3];

        public static void Tally(string key, int d, int from, int to)
        {
            if (d == 0) return;
            key = d + ":" + key; Cost[key] = (Cost.TryGetValue(key, out var v) ? v : 0) + (to - from);
        }

        /// <summary>Facade detail tier at plan point p: the same everywhere (see the class note).</summary>
        public static int Detail(Vector2 p) => Tier;

        /// <summary>After the facade kit exists: reset the tallies, decide every host's grammar and every sign.</summary>
        public static void Init(WorldDto world, FacadeKit fk)
        {
            Cost.Clear(); System.Array.Clear(VertsByTier, 0, 3); System.Array.Clear(MassesByTier, 0, 3);
            Pc.GlassSw = fk.Spec.Glass;
            ProcSkin.Init(world);
            ProcGrammar.Assign(world, fk);
        }

        /// <summary>Draw calls (renderers) and vertices in the camera's frustum for the facade layers only (buildings, frontages,
        /// roofs), which the kit prefabs the world also places do not pollute.</summary>
        public static void Perf(Camera cam)
        {
            var batches = GameObject.Find("batches"); if (!batches) return;
            var planes = GeometryUtility.CalculateFrustumPlanes(cam); int n = 0; long v = 0;
            foreach (var r in batches.GetComponentsInChildren<MeshRenderer>())
            {
                var layer = r.transform.parent.name;
                if (layer != "buildings" && layer != "frontages" && layer != "roofs") continue;
                if (!GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;
                n++; v += r.GetComponent<MeshFilter>().sharedMesh.vertexCount;
            }
            Debug.Log($"World: perf {cam.name}: facade layers {n} draw calls, {v} vertices");
        }

        /// <summary>Repetition metrics to out/review/procedural-metrics.json, and the vertex cost by tier, feature and layer.</summary>
        public static void Report(WorldDto world, FacadeKit fk)
        {
            var json = ProcGrammar.Metrics(world, fk);
            var dir = Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "review"); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "procedural-metrics.json"), json);
            Debug.Log("World: facade metrics " + json);
            Debug.Log($"World: facade vertices by tier far/mid/hero = {VertsByTier[0]}/{VertsByTier[1]}/{VertsByTier[2]} over {MassesByTier[0]}/{MassesByTier[1]}/{MassesByTier[2]} masses");
            Debug.Log("World: facade hero-tier cost " + string.Join(", ", Cost.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value}")));
            var b = GameObject.Find("batches"); if (!b) return;
            var by = new SortedDictionary<string, long>(System.StringComparer.Ordinal);
            foreach (var mf in b.GetComponentsInChildren<MeshFilter>()) { var k = mf.transform.parent.name; by[k] = (by.TryGetValue(k, out var v) ? v : 0) + mf.sharedMesh.vertexCount; }
            Debug.Log("World: vertices by layer " + string.Join(", ", by.Select(kv => $"{kv.Key}={kv.Value}")));
        }
    }
}
