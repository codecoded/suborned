#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitFrontage;
using G = Suborned.OldTown.Editor.DecoKit.DecoKitGroundMesh;
using H = Suborned.OldTown.Editor.DecoKit.DecoKitHighrise;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using T = Suborned.OldTown.Editor.DecoKit.DecoKitRoofThemes;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The boulevard district's roofs (Concepts/Blocks): from the 2.5D camera a roof is half of every
    /// building, so every roof on the district's straight walls is programmed. Each building (a run's segment, less its
    /// rounded end bays; the civic hall and arcades aside) gives a 14 m deep roof strip at its own height; the strip
    /// splits at bay joints into plots of 12-30 m, and each plot takes a theme (DecoKitRoofThemes) unlike the one before
    /// it, on a terrazzo deck laid just over the roof (plant stands on the bare roof). Five crowned towers stand across
    /// the blocks' two halves, on a limestone plinth where one half is lower; plots under them, or already carrying a pad
    /// or a billboard (DecoKitDistrictDressing), only get planters.
    internal static class DecoKitRoofline
    {
        const float Depth = 14f, Inset = .3f, Lift = .02f, Lawn = .08f, LawnEdge = 1.4f;

        internal readonly struct Roof
        {
            internal readonly Vector3 Start, Dir, N;
            internal readonly float A0, A1, Top, Front;  // along the wall from Start; roof over the walk; deck setback from the facade

            internal Roof(Vector3 start, Vector3 dir, Vector3 n, float a0, float a1, float top, float front)
            {
                Start = start; Dir = dir; N = n; A0 = a0; A1 = a1; Top = top; Front = front;
            }

            /// a along the wall, w in from the facade.
            internal Vector3 At(float a, float w) => Start + Dir * a - N * w;

            internal Rect Area(float a0, float a1, float w0, float w1)
            {
                Vector3 p = At(a0, w0), q = At(a1, w1);
                return Rect.MinMaxRect(Mathf.Min(p.x, q.x), Mathf.Min(p.z, q.z), Mathf.Max(p.x, q.x), Mathf.Max(p.z, q.z));
            }
        }

        // across the blocks' halves (faces the boulevard); clear of the lanes, the rail, the pads and the billboards
        static readonly (string asset, Vector3 at, float yaw, float scale)[] Towers =
        {
            ("DiscTower", new(32f, 0f, 38f), -90f, .9f),         // Sunset Way's block: S6 front, P7 back (plinth 26.5 -> 29)
            ("Tower_Starburst", new(32f, 0f, 96f), -90f, .85f),  // north of Market Street: S9W front, D10 back (plinth 36.7 -> 40)
            ("Tower_Atom", new(-32f, 0f, 146f), 90f, .9f),       // the west wall short of the circus: S9 both halves
            ("Tower_Lantern", new(-32f, 0f, 278f), 90f, 1f),     // north-west: S9 both halves
            ("Tower_Terrace", new(32f, 0f, 324f), -90f, 1f),     // north-east, north of the pad at 302: S9 front, S6 back (plinth 26.5 -> 36.7)
        };

        /// The roofs of a run laid by DecoKitFrontage.Run with the same arguments.
        internal static IEnumerable<Roof> Roofs(DecoKitPlacer p, Vector3 start, Vector3 dir, Vector3 n, (string cls, int n)[] segs,
                                               Cap first, Cap last)
        {
            foreach (var bays in Bays(p, segs, first, last).Where(b => !b.endS && !b.endN).GroupBy(b => b.seg))
            {
                string cls = segs[bays.Key].cls;
                if (cls == "H" || cls == "A3") continue;
                yield return new Roof(start, dir, n, bays.Min(b => b.at), bays.Max(b => b.at) + 6f, Of(p, cls).Height - .15f,
                                      cls == "D10" ? 1.3f : Inset);  // the Deco bay's piers climb past its parapet
            }
        }

        internal static void Build(DecoKitPlacer p, List<Roof> roofs)
        {
            var taken = p.Placed().Where(x => x.pos.y > 8f).Select(x => x.pos).ToList();  // pads, billboards (and wall signs, told apart by height)
            G deck = new(), kerb = new(), lawn = new(), plinth = new();
            var feet = Stand(p, roofs, plinth);
            var tally = new Dictionary<string, int>();
            foreach (var roof in roofs) Programme(p, roof, taken, feet, deck, kerb, lawn, tally);
            deck.Emit("Roof decks", G.Mat("DK_Terrazzo_Ivory"));
            kerb.Emit("Roof lawn kerbs", G.Mat("DK_Limestone_Tex"), false);
            lawn.Emit("Roof lawns", G.Lawn());
            plinth.Emit("Tower plinths", G.Mat("DK_Limestone_Tex"));
            Debug.Log($"[DecoKit] roofline: {roofs.Count} roofs, {feet.Count} towers, plots " +
                      string.Join(", ", tally.OrderBy(k => k.Key).Select(k => $"{k.Key} {k.Value}")));
        }

        /// Each tower stands on the higher roof under its foot, on a plinth from the lower; returns the feet (world x / z).
        static List<Rect> Stand(DecoKitPlacer p, List<Roof> roofs, G plinth)
        {
            var feet = new List<Rect>();
            foreach (var (asset, at, yaw, scale) in Towers)
            {
                var go = p.Put(asset, at, yaw, scale: scale);
                if (!go) continue;
                var foot = World(go.transform, H.Foot(go, 2.5f));
                var under = roofs.Where(r => r.Area(r.A0, r.A1, 0f, Depth).Overlaps(foot)).Select(r => r.Top).ToList();
                if (under.Count == 0) { Debug.LogError($"[DecoKit] {asset} at {at}: no roof under it"); continue; }
                float top = under.Max(), low = under.Min();
                go.transform.position = at + Vector3.up * (L.Walk + top + Lift);
                if (low < top - .05f) plinth.Block(H.Grow(foot, .3f), L.Walk + low, L.Walk + top + Lift);
                feet.Add(foot);
            }
            return feet;
        }

        /// A local plan rect (x / z) as world x / z (the towers stand square to the grid).
        static Rect World(Transform t, Rect r)
        {
            Vector3 a = t.TransformPoint(new Vector3(r.xMin, 0f, r.yMin)), b = t.TransformPoint(new Vector3(r.xMax, 0f, r.yMax));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
        }

        static void Programme(DecoKitPlacer p, Roof roof, List<Vector3> all, List<Rect> feet, G deck, G kerb, G lawn,
                              Dictionary<string, int> tally)
        {
            var rng = new System.Random(Mathf.RoundToInt(roof.Start.x * 73f + roof.Start.z * 131f + roof.A0 * 7f));
            float y = L.Walk + roof.Top, w0 = roof.Front, w1 = Depth - Inset, d = w1 - w0, wm = (w0 + w1) * .5f;
            var taken = all.Where(t => Mathf.Abs(t.y - y) < 1.5f).Select(t => new Vector2(t.x, t.z)).ToList();  // standing on this roof
            var cuts = AroundFeet(roof, Cuts(roof.A0, roof.A1, rng), feet);
            var themes = new T.Theme[cuts.Count - 1];
            for (int i = 0; i < themes.Length; i++)
            {
                var area = roof.Area(cuts[i], cuts[i + 1], w0, w1);
                themes[i] = feet.Any(f => H.Grow(f, 1.5f).Overlaps(area)) ? T.Beneath
                    : taken.Any(t => H.Grow(area, 7f).Contains(t)) ? T.Planters
                    : Pick(cuts[i + 1] - cuts[i] - 2f * Inset, d, i > 0 ? themes[i - 1] : null, rng);
                tally[themes[i].Name] = tally.TryGetValue(themes[i].Name, out int c) ? c + 1 : 1;
            }
            for (int i = 0; i < themes.Length; i++)
            {
                var th = themes[i];
                float am = (cuts[i] + cuts[i + 1]) * .5f, l = cuts[i + 1] - cuts[i] - 2f * Inset;
                if (th.Deck)  // one deck along neighbouring decked plots; set in from the roof's ends and from bare plots
                    deck.Block(roof.Area(cuts[i] + (i == 0 || !themes[i - 1].Deck ? Inset : 0f),
                                         cuts[i + 1] - (i == themes.Length - 1 || !themes[i + 1].Deck ? Inset : 0f), w0, w1),
                               y - .25f, y + Lift);
                float lu = l * .5f - LawnEdge, lv = d * .5f - LawnEdge;
                if (th.Lawn)
                {
                    kerb.Block(roof.Area(am - lu - .2f, am + lu + .2f, wm - lv - .2f, wm + lv + .2f), y, y + Lift + .06f);
                    lawn.Block(roof.Area(am - lu, am + lu, wm - lv, wm + lv), y, y + Lift + Lawn);
                }
                foreach (var it in th.Fill(l, d, rng))
                {
                    var at = roof.At(am + it.U, wm - it.V);  // +v toward the street
                    var flat = new Vector2(at.x, at.z);
                    if (th == T.Planters && (feet.Any(f => H.Grow(f, 1f).Contains(flat)) || taken.Any(t => (t - flat).sqrMagnitude < 81f)))
                        continue;
                    bool onLawn = th.Lawn && Mathf.Abs(it.U) < lu && Mathf.Abs(it.V) < lv;
                    p.Put(it.Asset, at + Vector3.up * (y + Lift + (onLawn ? Lawn : 0f)), P.Yaw(roof.N) + it.Yaw);
                }
            }
        }

        /// Plot boundaries along a roof: bay joints 2-5 bays apart, at least two bays left for the last plot.
        static List<float> Cuts(float a0, float a1, System.Random rng)
        {
            var cuts = new List<float> { a0 };
            for (float at = a0; a1 - at > 30f;)
            {
                at += 6f * rng.Next(2, Mathf.Min(5, Mathf.RoundToInt((a1 - at) / 6f) - 2) + 1);
                cuts.Add(at);
            }
            cuts.Add(a1);
            return cuts;
        }

        /// Also cut at the bay joints either side of a tower's foot, so the tower takes only the bays it stands on.
        static List<float> AroundFeet(Roof roof, List<float> cuts, List<Rect> feet)
        {
            var area = roof.Area(roof.A0, roof.A1, 0f, Depth);
            foreach (var f in feet.Where(f => f.Overlaps(area)))
            {
                var along = new[] { f.min, f.max, new Vector2(f.xMin, f.yMax), new Vector2(f.xMax, f.yMin) }
                    .Select(c => Vector3.Dot(new Vector3(c.x, 0f, c.y) - roof.Start, roof.Dir)).ToList();
                float a0 = roof.A0 + 6f * Mathf.Floor((along.Min() - 1.5f - roof.A0) / 6f), a1 = roof.A0 + 6f * Mathf.Ceil((along.Max() + 1.5f - roof.A0) / 6f);
                cuts.Add(Mathf.Clamp(a0, roof.A0, roof.A1));
                cuts.Add(Mathf.Clamp(a1, roof.A0, roof.A1));
            }
            return cuts.Select(c => Mathf.Round(c * 10f) / 10f).Distinct().OrderBy(c => c).ToList();
        }

        /// A theme that fits the plot (weighted), unlike the plot before it where anything else fits.
        static T.Theme Pick(float l, float d, T.Theme prev, System.Random rng)
        {
            var fit = T.All.Where(t => l >= t.MinL && d >= t.MinD).ToList();
            if (fit.Count > 1) fit.Remove(prev);
            if (fit.Count == 0) return T.Planters;
            float x = (float)rng.NextDouble() * fit.Sum(t => t.Weight);
            foreach (var t in fit)
                if ((x -= t.Weight) <= 0f) return t;
            return fit[^1];
        }
    }
}
#endif
