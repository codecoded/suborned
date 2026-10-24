using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 057 §2 with Sky's note ("just make it thin"): what Reroute moves, on the ground, hairline weight, cream over ink-deep (054's
    /// language, no new colour): the patrol's sweep as a line between its two ends with a short cross-tick at each; the route it walks to the wait
    /// spot dotted (0.35 m on, 0.35 m off) with a small filled chevron every 3 m; the wait spot a dashed circle (0.9 m); on hover the patrol's own
    /// ground ring. The patrol's cameo (the kit's signal_unit badge, until Forma and Glyph make npc_patrol) over the patrol on hover and over the
    /// wait spot; an edge chip (plate_dark 22 px, the cameo and the pointer icon turned toward it) when the wait spot is off screen. Drawn while
    /// the way is hovered or lit, and while the reroute is in play.</summary>
    public sealed partial class HudToolkit
    {
        const float RrLine = 0.04f, RrUnder = 0.09f, RrUnderA = 0.6f, RrDot = 0.35f, RrChevron = 3f;
        VisualElement rrWaitCameo, rrPatrolCameo, rrEdge; Paint rrEdgeArrow; float rrEdgeAngle;
        /// <summary>The proof reads it: "hover" / "live" / "" and the route's point count.</summary>
        public static string RerouteShown { get; private set; } = "";
        /// <summary>The proof reads it: the edge chip is up (the wait spot off screen).</summary>
        public static bool RerouteEdgeShown => live?.rrEdge != null && live.rrEdge.style.display == DisplayStyle.Flex;

        /// <summary>A rect overlaps an edge cameo shown (its 24 px face and the tail's reach), grown by `pad`.</summary>
        bool EdgeCamHits(Rect r, float pad)
        {
            foreach (var ec in edgeCams)
            {
                if (ec.Root.style.display != DisplayStyle.Flex) continue;
                var cr = new Rect(ec.Root.style.left.value.value - 10f - pad, ec.Root.style.top.value.value - 10f - pad, 44f + 2f * pad, 44f + 2f * pad);
                if (cr.Overlaps(r)) return true;
            }
            return false;
        }

        void RrSeg(Vector3 a, Vector3 b, float w, float under, float alpha = 1f)
        {
            LineM(GroundOf(new Color(InkDeep.r, InkDeep.g, InkDeep.b, RrUnderA * alpha), true), a, b, under);
            LineM(GroundOf(new Color(Cream.r, Cream.g, Cream.b, alpha), false), a, b, w);
        }

        VisualElement RrCameo()
        {
            var c = Cameo(marks, 14f, 2f); c.style.position = Position.Absolute; c.pickingMode = PickingMode.Ignore; Bg(c.Q("face"), "badges/signal_unit", Color.white); Show(c, false); return c;
        }

        void RerouteDraw()
        {
            var rr = host.Reroute; if (marks == null) return;
            rrWaitCameo ??= RrCameo(); rrPatrolCameo ??= RrCameo();
            if (rrEdge == null)
            {   // Glyph's 0.2.104 review #3: cameo, 4 px, the pointer (cream, 12 px, painted turned toward the wait spot so its box stays 12 px for the
                // layout audit), the plate shrinking to fit; 9 px each side keeps both 4 px clear of the rim
                rrEdge = PlateDark(marks, 30f, 0.37f); rrEdge.style.position = Position.Absolute; rrEdge.style.paddingLeft = 10; rrEdge.style.paddingRight = 10;
                var cm = Cameo(rrEdge, 12f, 2f); Bg(cm.Q("face"), "badges/signal_unit", Color.white);
                rrEdgeArrow = P(rrEdge, 0, 0, 12, 12, (pt, r) =>
                {
                    var c = r.center; float a = rrEdgeAngle; Vector2 R(Vector2 v) => new(v.x * Mathf.Cos(a) - v.y * Mathf.Sin(a), v.x * Mathf.Sin(a) + v.y * Mathf.Cos(a));
                    pt.fillColor = Cream; pt.BeginPath(); pt.MoveTo(c + R(new Vector2(5.5f, 0f))); pt.LineTo(c + R(new Vector2(-4f, 4.5f))); pt.LineTo(c + R(new Vector2(-1.5f, 0f))); pt.LineTo(c + R(new Vector2(-4f, -4.5f))); pt.ClosePath(); pt.Fill();
                });
                rrEdgeArrow.style.position = Position.Relative; rrEdgeArrow.style.marginLeft = 4; rrEdgeArrow.style.flexShrink = 0;
                Show(rrEdge, false); manifest.Add((rrEdge, "image", () => "reroute edge chip")); /* the 2x check and the layout audit find it */
            }
            bool on = rr.On && !m.Ended && host.View != null;
            Show(rrWaitCameo, false); Show(rrPatrolCameo, false); Show(rrEdge, false);
            RerouteShown = on ? (rr.Hover ? "hover" : "live") + ":" + rr.Route.Count + (rr.FromEngine ? ":engine" : ":grid") : "";
            if (!on) return;
            // the sweep: a line between its ends, a cross-tick at each
            RrSeg(rr.A, rr.B, RrLine, RrUnder);
            var ab = rr.B - rr.A; ab.y = 0f; var n = Vector3.Cross(Vector3.up, ab.normalized) * 0.3f;
            RrSeg(rr.A - n, rr.A + n, RrLine, RrUnder); RrSeg(rr.B - n, rr.B + n, RrLine, RrUnder);
            // the route: dotted, a chevron every 3 m pointing along it
            float dash = 0f, chev = RrChevron * 0.5f;
            for (int i = 1; i < rr.Route.Count; i++)
            {
                var a = rr.Route[i - 1]; var b = rr.Route[i]; var d = b - a; d.y = 0f; float len = d.magnitude; if (len < 1e-3f) continue; var f = d / len;
                for (float t = -dash; t < len; t += 2f * RrDot) { float s0 = Mathf.Max(0f, t), s1 = Mathf.Min(len, t + RrDot); if (s1 > s0) RrSeg(Vector3.Lerp(a, b, s0 / len), Vector3.Lerp(a, b, s1 / len), RrLine, RrUnder); }
                dash = (dash + len) % (2f * RrDot);
                for (; chev < len; chev += RrChevron)
                {
                    var p = Vector3.Lerp(a, b, chev / len); var side = Vector3.Cross(Vector3.up, f) * 0.14f; var tip = p + f * 0.16f; var back = p - f * 0.1f;
                    var ink = GroundOf(new Color(InkDeep.r, InkDeep.g, InkDeep.b, RrUnderA), true); var cr = GroundOf(Cream, false); float lift = GroundLift + 0.002f;
                    Quad(ink, back + side * 1.4f + Vector3.up * GroundLift, tip + f * 0.05f + Vector3.up * GroundLift, tip + f * 0.05f + Vector3.up * GroundLift, back - side * 1.4f + Vector3.up * GroundLift);
                    Quad(cr, back + side + Vector3.up * lift, tip + Vector3.up * lift, tip + Vector3.up * lift, back - side + Vector3.up * lift); // a filled triangle (a quad with two corners at the tip)
                }
                chev -= len;
            }
            // the wait spot: a dashed circle, the cameo on it
            for (int i = 0; i < 36; i++) { float a0 = i * Mathf.PI * 2f / 36f, a1 = (i + 0.56f) * Mathf.PI * 2f / 36f; RrSeg(rr.Wait + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * 0.9f, rr.Wait + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * 0.9f, RrLine, RrUnder); }
            if (rr.Hover) GroundRing(rr.Patrol, 0.68f, Cream, 2f, false, 0.55f); // where it is now (the hover ring's size)
            bool waitSeen = Proj(rr.Wait + Vector3.up * 0.9f, out var wq) && InView(wq);
            if (waitSeen) { Show(rrWaitCameo, true); rrWaitCameo.style.left = wq.x - 9f; rrWaitCameo.style.top = wq.y - 9f; }
            else
            {   // an edge chip toward it
                var W = root.layout.width > 0 ? root.layout.width : 1920f; var H = root.layout.height > 0 ? root.layout.height : 1080f; var c = new Vector2(W / 2f, H / 2f);
                Proj(rr.Wait, out var raw); var dir = raw - c; if (dir.sqrMagnitude < 1f) dir = Vector2.right; dir.Normalize();
                float k = Mathf.Min((W / 2f - 60f) / Mathf.Max(1e-3f, Mathf.Abs(dir.x)), (H / 2f - 40f) / Mathf.Max(1e-3f, Mathf.Abs(dir.y)));
                var at = c + dir * k;
                // 0.2.109 own check: the Tech stands by the wait spot, so his off-screen cameo (24 px and its tail) sits on the same edge point:
                // step the chip inward, clear of every edge cameo up, so both read
                for (int step = 0; step < 4 && EdgeCamHits(new Rect(at.x - 26f, at.y - 14f, 56f, 30f), 6f); step++) at -= dir * 34f;
                Show(rrEdge, true); rrEdge.style.left = at.x - 26f; rrEdge.style.top = at.y - 14f;
                float ang = Mathf.Atan2(dir.y, dir.x); if (Mathf.Abs(ang - rrEdgeAngle) > 0.01f) { rrEdgeAngle = ang; rrEdgeArrow.MarkDirtyRepaint(); } // the arrow points at the wait spot (screen space, y down)
            }
            if (rr.Hover && Proj(rr.Patrol + Vector3.up * 1.9f, out var pq)) { Show(rrPatrolCameo, true); rrPatrolCameo.style.left = pq.x - 9f; rrPatrolCameo.style.top = pq.y - 9f; }
        }
    }
}
