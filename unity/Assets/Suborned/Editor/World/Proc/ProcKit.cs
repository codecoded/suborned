using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Shared state of the procedural facades (Vela's grammar, the default for every building): detail tiers and the
    /// vertex tallies. Detail falls off with distance from the nearest hero point (the export's play focus and Harbour Lane, the
    /// probe street) so the district stays cheap.</summary>
    public static class ProcKit
    {
        /// <summary>Profile end caps (hidden against neighbours nearly everywhere; off keeps the vertex count down).</summary>
        public const bool ProfileCaps = false;
        public const float Near = 80f, Mid = 260f;
        static readonly Vector2 HarbourLane = new(120f, -330f);
        static Vector2[] heroes = { HarbourLane };

        public static readonly Dictionary<string, long> Cost = new();
        public static readonly long[] VertsByTier = new long[3];
        public static readonly int[] MassesByTier = new int[3];

        public static void Tally(string key, int d, int from, int to)
        {
            if (d == 0) return;
            key = d + ":" + key; Cost[key] = (Cost.TryGetValue(key, out var v) ? v : 0) + (to - from);
        }

        public static int Detail(Vector2 p)
        {
            float d = float.MaxValue; foreach (var h in heroes) d = Mathf.Min(d, (p - h).magnitude);
            return d < Near ? 2 : d < Mid ? 1 : 0;
        }

        /// <summary>After the facade kit exists: reset the tallies, set the hero points, decide every host's grammar and every sign.</summary>
        public static void Init(WorldDto world, FacadeKit fk)
        {
            Cost.Clear(); System.Array.Clear(VertsByTier, 0, 3); System.Array.Clear(MassesByTier, 0, 3);
            heroes = world.View != null ? new[] { world.View.Focus, HarbourLane } : new[] { HarbourLane };
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
