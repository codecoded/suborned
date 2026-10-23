using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 049 round 2 (Glyph's spec, the lab's `targetRings` export): the target rings as ground decals (HudToolkit.Ground: depth-
    /// tested, in the transparent queue after the figures, so legs stand in front of the back half). Widths are metres on the ground.
    /// Colours, all sourced: guard and police amber #f0a020, rivals violet #8a4fd0 (style contract signal_colours), a civilian cream (kit.uss),
    /// the threat red #e03a2f (the cones' red, told apart from a cone by shape and motion: a closed outline at the feet with four ticks that
    /// close in and kick, never filled), ticks cream, edges ink-deep. Not a cone: no fill, no sweep.</summary>
    public sealed partial class HudToolkit
    {
        static readonly Color RingAmber = Hex(0xf0a020), RingViolet = Hex(0x8a4fd0), RingThreat = Hex(0xe03a2f);
        const float RingR = 0.54f, RingHoverR = 0.68f, TickIn = 0.07f, TickOut = 0.22f, TickW = 0.095f, TickEdge = 0.17f, TickEdgeAlpha = 0.85f, EdgeAlpha = 0.6f; // ticks: Glyph 0.2.79 #5 (0.075 / 0.13 at 60 % thinned out on pale paving)

        /// <summary>A ring on the ground, its line and edge in metres; `dashes` &gt; 0 draws that many dashes (50 % on).</summary>
        void GroundRingM(Vector3 c, float r, Color col, float lineM, int dashes, float edgeAlpha)
        {
            if (r <= 0f || col.a <= 0.01f) return;
            if (edgeAlpha > 0f) RingQuadsN(GroundOf(new Color(InkDeep.r, InkDeep.g, InkDeep.b, edgeAlpha), true), c, r, lineM + 0.05f, dashes);
            RingQuadsN(GroundOf(col, false), c, r, lineM, dashes);
        }
        static void RingQuadsN(GroundBatch b, Vector3 c, float r, float w, int dashes)
        {
            int n = dashes > 0 ? dashes * 2 : Mathf.Clamp(Mathf.CeilToInt(r * 48f), 32, 96); float y = c.y + GroundLift, ri = Mathf.Max(0f, r - w / 2f), ro = r + w / 2f;
            for (int i = 0; i < n; i++)
            {
                if (dashes > 0 && (i & 1) == 1) continue;
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                Quad(b, new Vector3(c.x + Mathf.Cos(a0) * ri, y, c.z + Mathf.Sin(a0) * ri), new Vector3(c.x + Mathf.Cos(a0) * ro, y, c.z + Mathf.Sin(a0) * ro),
                        new Vector3(c.x + Mathf.Cos(a1) * ro, y, c.z + Mathf.Sin(a1) * ro), new Vector3(c.x + Mathf.Cos(a1) * ri, y, c.z + Mathf.Sin(a1) * ri));
            }
        }
        /// <summary>A short segment on the ground from `a` to `b`, `w` metres wide.</summary>
        static void SegQuad(GroundBatch g, Vector3 a, Vector3 b, float w)
        {
            var d = b - a; d.y = 0f; if (d.sqrMagnitude < 1e-8f) return; var f = d.normalized; var s = new Vector3(-f.z, 0f, f.x) * (w / 2f);
            float y = a.y + GroundLift + 0.002f; a.y = y; b.y = y; Quad(g, a - s, a + s, b + s, b - s);
        }

        /// <summary>This frame's target rings (HandoverHost.TargetRings), priority fire &gt; aim &gt; ours &gt; hover &gt; in &gt; long &gt; out.</summary>
        void TargetRingsDraw()
        {
            var h = host; if (h.Game == null || m.Ended) return; float now = Time.unscaledTime; var (up, right) = GroundAxes();
            foreach (var r in h.TargetRings)
            {
                var c = r.At; bool threat = r.Fire || r.Aim;
                var side = r.Side == HandoverHost.RingSide.Rival ? RingViolet : r.Side == HandoverHost.RingSide.Guard ? RingAmber : Cream;
                float rad = !threat && r.Hover ? RingHoverR : RingR, line; int dashes = 0; float alpha = 1f, edge = EdgeAlpha;
                if (threat) line = 0.12f;
                else if (r.Hover) line = 0.12f;
                else if (r.Reach == HandoverHost.RingReach.In) line = 0.10f;
                else if (r.Reach == HandoverHost.RingReach.Long) line = 0.075f;
                else { line = 0.065f; dashes = 22; alpha = 0.9f; edge = 0.42f; }
                var col = threat ? RingThreat : side; col.a = alpha;
                GroundRingM(c, rad, col, line, dashes, edge);
                // the pulse: one thin red ring leaves at each shot (scale 1 to 1.8, 300 ms, alpha 0.9 to 0)
                float since = now - r.ShotAt;
                if (r.Fire && since >= 0f && since < 0.3f) { float t = since / 0.3f; GroundRingM(c, rad * Mathf.Lerp(1f, 1.8f, t), new Color(RingThreat.r, RingThreat.g, RingThreat.b, Mathf.Lerp(0.9f, 0f, t)), 0.07f, 0, 0f); }
                if (!(threat || r.Ours)) continue;
                // the four ticks, square to the screen: still (ours), closing in (aim: 0.30 m out at alpha 0.5, at rest by 78 % of a 1 s loop with
                // ease-in, then held), kicked in 0.07 m at a shot and eased back over 30 % of the loop (fire)
                float off = 0f, ta = 1f;
                if (r.Fire) { float k = since >= 0f && since < 0.3f ? 1f - since / 0.3f : 0f; off = -0.07f * k * k; }
                else if (r.Aim) { float p = Mathf.Repeat(now, 1f) / 0.78f; if (p < 1f) { float e = 1f - p * p; off = 0.30f * e; ta = Mathf.Lerp(1f, 0.5f, e); } }
                var tick = new Color(Cream.r, Cream.g, Cream.b, ta); var tEdge = new Color(InkDeep.r, InkDeep.g, InkDeep.b, TickEdgeAlpha * ta);
                var gE = GroundOf(tEdge, true); var gT = GroundOf(tick, false);
                foreach (var d in new[] { up, right, -up, -right })
                {
                    var a = c + d * (rad + TickIn + off); var b = c + d * (rad + TickOut + off);
                    SegQuad(gE, a - d * 0.035f, b + d * 0.035f, TickEdge); SegQuad(gT, a, b, TickW);
                }
            }
        }
    }
}
