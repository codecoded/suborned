using System.Collections.Generic;
using UnityEngine;

namespace Suborned.OldTown.Crowd
{
    /// <summary>Where the crowd may walk, without a NavMesh: the placed people (the street's RFM citizens stand on its
    /// pavements, squares and decks) as nodes, each linked to its nearest neighbours within reach on the same level
    /// (|dy| under step) with a clear line at hip height, so links don't run through walls, kiosks or benches.</summary>
    public sealed class VatWalkGraph
    {
        const float Hip = .9f, Merge = 1.2f;

        public readonly List<Vector3> nodes = new();
        public readonly List<int[]> links = new();
        public int Links { get; private set; }

        public static VatWalkGraph Build(IEnumerable<Vector3> points, float reach, float step, int maxLinks, int blockers)
        {
            var g = new VatWalkGraph();
            var cells = new Dictionary<Vector2Int, List<int>>();
            Vector2Int Cell(Vector3 p) => new(Mathf.FloorToInt(p.x / reach), Mathf.FloorToInt(p.z / reach));
            IEnumerable<int> Near(Vector3 p)
            {
                var c = Cell(p);
                for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    if (cells.TryGetValue(new Vector2Int(c.x + x, c.y + z), out var list))
                        foreach (int i in list) yield return i;
            }
            foreach (var p in points)  // a pair or a queue stands closer than a stride: one node for them
            {
                bool dup = false;
                foreach (int i in Near(p))
                    if ((g.nodes[i] - p).sqrMagnitude < Merge * Merge) { dup = true; break; }
                if (dup) continue;
                var c = Cell(p);
                if (!cells.TryGetValue(c, out var list)) cells[c] = list = new List<int>();
                list.Add(g.nodes.Count);
                g.nodes.Add(p);
            }
            var sets = new HashSet<int>[g.nodes.Count];
            for (int i = 0; i < sets.Length; i++) sets[i] = new HashSet<int>();
            var near = new List<(float d, int j)>();
            for (int i = 0; i < g.nodes.Count; i++)
            {
                var a = g.nodes[i];
                near.Clear();
                foreach (int j in Near(a))
                {
                    var d = g.nodes[j] - a;
                    if (j == i || Mathf.Abs(d.y) > step || d.sqrMagnitude > reach * reach) continue;
                    near.Add((d.sqrMagnitude, j));
                }
                near.Sort((x, y) => x.d.CompareTo(y.d));
                int kept = 0;
                foreach (var (_, j) in near)
                {
                    if (kept >= maxLinks) break;
                    if (blockers != 0 && Physics.Linecast(a + Vector3.up * Hip, g.nodes[j] + Vector3.up * Hip, blockers,
                                                           QueryTriggerInteraction.Ignore)) continue;
                    sets[i].Add(j);
                    sets[j].Add(i);
                    kept++;
                }
            }
            foreach (var s in sets)
            {
                var arr = new int[s.Count];
                s.CopyTo(arr);
                g.links.Add(arr);
                g.Links += arr.Length;
            }
            g.Links /= 2;
            return g;
        }

        /// <summary>The next node from `at`, arriving from `from`: any neighbour but the way back, unless it's a dead end.</summary>
        public int Next(int at, int from, System.Random rng)
        {
            var l = links[at];
            if (l.Length == 0) return at;
            if (l.Length == 1) return l[0];
            int k = rng.Next(l.Length - 1), n = l[k];
            return n == from ? l[l.Length - 1] : n;
        }
    }
}
