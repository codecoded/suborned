#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using G = Suborned.OldTown.Editor.DecoKit.DecoKitGroundMesh;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Built-up blocks (Concepts/Blocks/needle_trio.png, mixed_skyline.png): from the 2.5D camera a roof is
    /// half of every building, so each Southside block's roof is a landscaped deck, programmed (DecoKitDecks) and on
    /// some carrying a tower. The deck is a terrazzo slab laid just over the bays' flat roofs (one height a block),
    /// inside their copings and rounded to the corners' 6 m quarter rounds; cut round an arcade bay, whose folded-plate
    /// roof rises through it. Lawn beds on it, the programme's pieces (roofdeck.py, highrise.py, palms, furniture), and
    /// people on the deck left free.
    internal static class DecoKitHighrise
    {
        const float Inset = .3f, Corner = 6f, Lift = .02f, Lawn = .08f, Bay = 14f;

        /// Brief 075 swap (Latch), edit E9 of the verbatim copy: the "no tower here" parameter. Null (the old builders as they were) or a
        /// test on each roof-programme piece as placed (block, piece, instance): false = it does not stand (Editor/OldTown/Swap: the towers
        /// that would bury the Handover zone from the play camera, the old rule of DecoKitDecks' header extended to the zone).
        internal static System.Func<string, string, GameObject, bool> Stands;

        /// Decks, towers and programme for every block; returns spots for people on the decks (world).
        internal static List<Vector3> Build(DecoKitPlacer p, Transform root, IEnumerable<DecoKitBlock> blocks)
        {
            var people = new List<Vector3>();
            G deck = new(), kerb = new(), lawn = new();
            foreach (var b in blocks)
            {
                var q = DecoKitDecks.Of(b.Name);
                if (q == null) { Debug.LogWarning($"[DecoKit] block {b.Name}: no roof programme (DecoKitDecks)"); continue; }
                float y = L.Walk + Top(p, b);
                var holes = Arcades(b);
                foreach (var piece in Deck(b, holes)) deck.Prism(piece.Select(t => Flat(b.At(t.x, t.y))).ToArray(), y - .25f, y + Lift);
                foreach (var (u0, v0, u1, v1) in q.Lawns)
                {
                    kerb.Prism(Quad(b, u0 - .2f, v0 - .2f, u1 + .2f, v1 + .2f), y, y + Lift + .06f);
                    lawn.Prism(Quad(b, u0, v0, u1, v1), y, y + Lift + Lawn);
                }
                var placed = new List<(Transform t, Rect foot)>();
                foreach (var it in q.Items)
                {
                    if (holes.Any(h => h.Contains(new Vector2(it.U, it.V))))
                        Debug.LogWarning($"[DecoKit] block {b.Name}: {it.Asset} at ({it.U}, {it.V}) stands in an arcade bay");
                    bool onLawn = q.Lawns.Any(l => it.U > l.u0 && it.U < l.u1 && it.V > l.v0 && it.V < l.v1);
                    var go = p.Put(it.Asset, b.At(it.U, it.V) + Vector3.up * (y + Lift + (onLawn ? Lawn : 0f)), b.Phi + it.Yaw,
                                   scale: it.Scale);
                    if (go && Stands != null && !Stands(b.Name, it.Asset, go)) { Object.DestroyImmediate(go); continue; }
                    if (go) placed.Add((go.transform, Foot(go, 2.5f)));
                }
                people.AddRange(People(b, holes, placed, y + Lift));
            }
            deck.Emit("Podium decks", G.Mat("DK_Terrazzo_Ivory"), true, root);
            kerb.Emit("Podium lawn kerbs", G.Mat("DK_Limestone_Tex"), false, root);
            lawn.Emit("Podium lawns", G.Lawn(), true, root);
            return people;
        }

        /// The bays' roof over the walk (their coping less its 0.15 m): one height a block, arcades aside.
        static float Top(DecoKitPlacer p, DecoKitBlock b)
        {
            var hs = b.Right.Concat(b.Left).Where(s => s.cls != "A3").Select(s => DecoKitFrontage.Of(p, s.cls).Height).Distinct().ToList();
            if (hs.Count > 1) Debug.LogError($"[DecoKit] block {b.Name}: mixed frontage heights {string.Join(", ", hs)} under one deck");
            return hs.Max() - .15f;
        }

        /// The arcade bays (u0, v0, u1, v1 as a Rect in u / v): the long faces' bays run 6 m each from the 8 m corner.
        static List<Rect> Arcades(DecoKitBlock b)
        {
            var holes = new List<Rect>();
            float hd = b.D * .5f;
            foreach (var (segs, side) in new[] { (b.Right, 1f), (b.Left, -1f) })
            {
                float u = -b.W * .5f + 8f;
                foreach (var (cls, n) in segs)
                {
                    if (cls == "A3") holes.Add(Rect.MinMaxRect(u, side > 0 ? hd - Bay : -hd, u + 6f * n, side > 0 ? hd : -hd + Bay));
                    u += 6f * n;
                }
            }
            return holes;
        }

        /// The deck (u / v): the inset rounded footprint in strips along u, each stopped short of any arcade it crosses.
        static IEnumerable<List<Vector2>> Deck(DecoKitBlock b, List<Rect> holes)
        {
            float hw = b.W * .5f - Inset, hd = b.D * .5f - Inset;
            var outline = Rounded(hw, hd, Corner - Inset);
            var cuts = holes.SelectMany(h => new[] { h.xMin, h.xMax }).Where(u => u > -hw && u < hw).Append(-hw).Append(hw)
                            .Distinct().OrderBy(u => u).ToList();
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float u0 = cuts[i], u1 = cuts[i + 1], um = (u0 + u1) * .5f, v0 = -hd, v1 = hd;
                foreach (var h in holes.Where(h => um > h.xMin && um < h.xMax))
                    if (h.center.y > 0f) v1 = Mathf.Min(v1, h.yMin); else v0 = Mathf.Max(v0, h.yMax);
                var piece = Clip(Clip(Clip(Clip(outline, new(u0, 0f), Vector2.right), new(u1, 0f), Vector2.left),
                                      new(0f, v0), Vector2.up), new(0f, v1), Vector2.down);
                if (piece.Count >= 3) yield return piece;
            }
        }

        /// Rounded rectangle +-hw x +-hd, corner radius r, CCW in u / v.
        static List<Vector2> Rounded(float hw, float hd, float r, int n = 6)
        {
            var pts = new List<Vector2>();
            foreach (var (cu, cv, a0) in new[] { (hw - r, hd - r, 0f), (-hw + r, hd - r, 90f), (-hw + r, -hd + r, 180f), (hw - r, -hd + r, 270f) })
                for (int k = 0; k <= n; k++)
                {
                    float a = (a0 + 90f * k / n) * Mathf.Deg2Rad;
                    pts.Add(new Vector2(cu + r * Mathf.Cos(a), cv + r * Mathf.Sin(a)));
                }
            return pts;
        }

        /// Keep the part of a convex polygon on the `keep` side of the line through `on`.
        static List<Vector2> Clip(List<Vector2> poly, Vector2 on, Vector2 keep)
        {
            var o = new List<Vector2>();
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 a = poly[i], c = poly[(i + 1) % poly.Count];
                float da = Vector2.Dot(a - on, keep), dc = Vector2.Dot(c - on, keep);
                if (da >= 0f) o.Add(a);
                if (da * dc < 0f) o.Add(a + (c - a) * (da / (da - dc)));
            }
            return o;
        }

        /// The piece's plan below `below` metres over its base, in its own frame (x / z): what people keep clear of.
        internal static Rect Foot(GameObject go, float below)
        {
            var t = go.transform;
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.sharedMesh) continue;
                var m = mf.transform.localToWorldMatrix;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var w = m.MultiplyPoint3x4(v);
                    if (w.y - t.position.y > below) continue;
                    var l = t.InverseTransformPoint(w);
                    x0 = Mathf.Min(x0, l.x); x1 = Mathf.Max(x1, l.x); z0 = Mathf.Min(z0, l.z); z1 = Mathf.Max(z1, l.z);
                }
            }
            return x0 > x1 ? Rect.zero : Rect.MinMaxRect(x0, z0, x1, z1);
        }

        /// About one person to 55 m2 of deck, on the free deck (clear of pieces, arcades, the edge), 1.2 m apart.
        static IEnumerable<Vector3> People(DecoKitBlock b, List<Rect> holes, List<(Transform t, Rect foot)> placed, float y)
        {
            var rng = new System.Random(b.Name.Aggregate(17, (h, c) => h * 31 + c));
            float hw = b.W * .5f - 1.2f, hd = b.D * .5f - 1.2f, r = Corner - 1.2f;
            var spots = new List<Vector3>();
            int tries = Mathf.RoundToInt(b.W * b.D / 55f);
            for (int k = 0; k < tries; k++)
            {
                var uv = new Vector2((float)(rng.NextDouble() * 2 - 1) * hw, (float)(rng.NextDouble() * 2 - 1) * hd);
                var c = new Vector2(Mathf.Clamp(uv.x, -hw + r, hw - r), Mathf.Clamp(uv.y, -hd + r, hd - r));
                if ((uv - c).sqrMagnitude > r * r || holes.Any(h => Grow(h, .5f).Contains(uv))) continue;
                var at = b.At(uv.x, uv.y) + Vector3.up * y;
                if (placed.Any(x => Grow(x.foot, .45f / x.t.lossyScale.x).Contains(Flat(x.t.InverseTransformPoint(at))))) continue;
                if (spots.Any(s => (s - at).sqrMagnitude < 1.2f * 1.2f)) continue;
                spots.Add(at);
            }
            return spots;
        }

        internal static Rect Grow(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);
        static Vector2 Flat(Vector3 v) => new(v.x, v.z);

        static Vector2[] Quad(DecoKitBlock b, float u0, float v0, float u1, float v1) =>
            new[] { (u0, v0), (u1, v0), (u1, v1), (u0, v1) }.Select(t => Flat(b.At(t.Item1, t.Item2))).ToArray();
    }
}
#endif
