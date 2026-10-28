#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using G = Suborned.OldTown.Editor.DecoKit.DecoKitGroundMesh;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Road markings read off the kerbs: the roads are implicit (one asphalt deck; the blocks' kerbs edge it), so each
    /// straight kerb stone, facing the road along its +Z, is paired with the nearest stone facing it across the road.
    /// A pair 3-30 m apart gives the carriageway's centre and width every 2 m. Two-lane widths get a dashed centre line
    /// (2 m on, 4 m off); where a paired run ends (a junction mouth, a square, a stop island) it gets a zebra 3 m in and
    /// coral tactile pads on both walks (the boulevard_crossing study). Paint is geometry: crisp at every zoom.
    internal static class DecoKitRoadMarkings
    {
        const float MinW = 3f, MaxW = 30f, Lateral = 1.2f, Step = 2f;
        const float DashMin = 7f, DashMax = 17f;  // kerb-to-kerb widths with two running lanes
        const float Kerb = .675f;                  // kerb stone + gutter courses, road side of the pivot
        const float Y = .004f, Walk = .152f;       // paint over the deck (-0.002); pads on the walk (0.15)
        const float RunMin = 10f;                  // shortest run that earns a crossing
        static readonly Regex Stone = new(@"^DK_Kerb_Granite_\d$");

        readonly struct Span
        {
            internal readonly Vector2 c, t, r;  // centre, road direction (canonical sign), across (from kerb A to B)
            internal readonly float w;          // kerb pivot to kerb pivot
            internal Span(Vector2 c, Vector2 t, Vector2 r, float w) { this.c = c; this.t = t; this.r = r; this.w = w; }
        }

        internal static void Build(Transform parent)
        {
            var kerbs = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(k => Stone.IsMatch(k.name))  // the straight stones' instances (the placer's groups drop the DK_)
                .Select(k => (p: V2(k.position), r: V2(k.forward).normalized)).ToArray();
            var spans = Pair(kerbs);
            G paint = new(), pads = new();
            int dashes = 0, zebras = 0;
            foreach (var s in spans)
            {
                // clear of a run's end cells (the zebra's 3 m +- 1.5)
                if (s.w < DashMin || s.w > DashMax || !Has(spans, s, s.c + s.t * 6f) || !Has(spans, s, s.c - s.t * 6f)) continue;
                if (Mathf.FloorToInt(Vector2.Dot(s.c, s.t) / Step) % 3 != 0) continue;
                paint.Quad(s.c, s.t, Step * .5f, .06f, Y);
                dashes++;
            }
            foreach (var s in spans)
                foreach (var outward in new[] { s.t, -s.t })
                {
                    if (Has(spans, s, s.c + outward * Step) || !Run(spans, s, -outward)) continue;
                    Zebra(paint, pads, s, s.c - outward * 3f);
                    zebras++;
                }
            Debug.Log($"[DecoKit] road markings: {kerbs.Length} kerbs, {spans.Count} spans, {dashes} dashes, {zebras} zebras");
            if (dashes + zebras == 0) { Debug.LogWarning("[DecoKit] road markings: no kerb pairs"); return; }
            paint.Emit("Road markings", G.Plain("DK_Road_Paint", "#E8E2D2", .3f), false, parent);
            pads.Emit("Crossing tactile pads", G.Plain("DK_Tactile_Coral", "#CF7A63", .25f), false, parent);
        }

        /// One span per kerb stone on one side of each road (the side whose stones face +x, or +z along x).
        static List<Span> Pair((Vector2 p, Vector2 r)[] kerbs)
        {
            var spans = new List<Span>();
            foreach (var (p, r) in kerbs)
            {
                if (r.x < -.01f || Mathf.Abs(r.x) <= .01f && r.y < 0f) continue;
                float best = float.MaxValue;
                Vector2 bestP = default;
                foreach (var (q, s) in kerbs)
                {
                    if (Vector2.Dot(r, s) > -.94f) continue;
                    var d = q - p;
                    float along = Vector2.Dot(d, r);
                    if (along < .5f || along >= best || (d - r * along).magnitude > Lateral) continue;
                    best = along;
                    bestP = q;
                }
                if (best < MinW || best > MaxW) continue;  // the nearest facing stone decides: an island hides the far kerb
                var t = new Vector2(r.y, -r.x);
                if (t.x < -.01f || Mathf.Abs(t.x) <= .01f && t.y < 0f) t = -t;
                spans.Add(new Span((p + bestP) * .5f, t, r, best));
            }
            return spans;
        }

        /// A zebra across the carriageway at c (stripes 0.5 m on / 0.5 m off, 3 m deep), pads on both walks.
        static void Zebra(G paint, G pads, Span s, Vector2 c)
        {
            float road = s.w - 2f * Kerb;
            int n = Mathf.FloorToInt(road);
            for (int i = 0; i < n; i++) paint.Quad(c + s.r * (i + .5f - n * .5f), s.t, 1.5f, .25f, Y);
            foreach (var side in new[] { -1f, 1f })
                pads.Quad(c + s.r * side * (s.w * .5f + .9f), s.t, 1.5f, .6f, Walk);
        }

        /// A span on the same road (same direction and centre line) at `at`.
        static bool Has(List<Span> spans, Span s, Vector2 at) =>
            spans.Any(o => Same(o, s) && Mathf.Abs(Vector2.Dot(o.c - at, s.t)) < Step * .6f && Mathf.Abs(Vector2.Dot(o.c - at, s.r)) < 1f);

        /// The run behind s (toward `inward`) is at least RunMin long.
        static bool Run(List<Span> spans, Span s, Vector2 inward)
        {
            for (float a = Step; a < RunMin; a += Step)
                if (!Has(spans, s, s.c + inward * a)) return false;
            return true;
        }

        static bool Same(Span a, Span b) => Mathf.Abs(Vector2.Dot(a.t, b.t)) > .98f && Mathf.Abs(Vector2.Dot(a.c - b.c, a.r)) < 1f;
        static Vector2 V2(Vector3 v) => new(v.x, v.z);
    }
}
#endif
