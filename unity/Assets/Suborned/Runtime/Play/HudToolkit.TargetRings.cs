using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 049 round 2 (Glyph's spec, the lab's `targetRings` export): the target rings as ground decals (HudToolkit.Ground: depth-
    /// tested, in the transparent queue after the figures, so legs stand in front of the back half). Widths are metres on the ground.
    /// Colours, all sourced: guard and police amber #f0a020, rivals violet #8a4fd0 (style contract signal_colours), a civilian cream (kit.uss),
    /// the threat red #e03a2f (the cones' red, told apart from a cone by shape and motion: a closed outline at the feet with four ticks that
    /// close in and kick, never filled), ticks cream, edges ink-deep. Not a cone: no fill, no sweep.
    /// Brief 101 step 6 (Glyph's S mocks, briefs/101/glyph/r2/S-busy.png): the focus's ring is in his signal colour (amber police and guards, violet rivals), whole
    /// while a shooter has him in range and in three arcs for a long shot (the only hint of a long shot, with no number); a shield stands on its front while his
    /// cover counts against our shooters (filled: it counts; half: in part). The threat red stays for whoever aims or fires at the cell.</summary>
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

        /// <summary>Brief 101: a ring in a signal colour, 0.10 m, over an ink-deep edge: whole, or (a long shot) three arcs with a gap at 12, 4 and 8 o'clock as seen.</summary>
        void FocusRing(Vector3 c, Color col, bool longShot, float rad = RingR, float line = 0.10f)
        {
            if (!longShot) { GroundRingM(c, rad, col, line, 0, EdgeAlpha); return; }
            var (u, rt) = GroundAxes(); var edge = GroundOf(new Color(InkDeep.r, InkDeep.g, InkDeep.b, EdgeAlpha), true); var fill = GroundOf(col, false);
            for (int i = 0; i < 3; i++) { float a0 = i * 120f + 19f, a1 = (i + 1) * 120f - 19f; ArcQuads(edge, c, rad, line + 0.05f, a0, a1, u, rt); ArcQuads(fill, c, rad, line, a0, a1, u, rt); }
        }

        /// <summary>Brief 101: a shield on the ring's front (its lowest point on screen) while the target's cover counts against a shooter of ours (the engine's own rule:
        /// CoverRules.Protects from where each stands; filled at the full cover's quality, half filled under it), in his signal colour on an ink keyline.</summary>
        void CoverShield(HandoverHost.TargetRing r, Color col)
        {
            var g = host.Game; if (g == null) return; var gun = g.GunOf(r.Id); if (gun == null || gun.Down || string.IsNullOrEmpty(gun.Cover)) return;
            Suborned.Sim.CoverPoint cp = null; foreach (var x in g.CoverNow()) if (x.Id == gun.Cover) { cp = x; break; } if (cp == null) return;
            bool counts = false; int sec = g.Tick / Suborned.Sim.Game.Hz;
            for (int k = 0; k < 4 && !counts; k++) { var op = g.Ops[k]; if (op.Down || op.Inside || !(host.Selected[k] || host.FireNow[k].Target == r.Id)) continue; counts = Suborned.Sim.CoverRules.Protects(cp, op.Pos, sec, g.R.PointBlankCm); }
            var (up, _) = GroundAxes(); if (!counts || !Proj(r.At - up * RingR, out var q)) return;
            bool full = cp.Q >= g.R.CoverQFull; var at = q + new Vector2(0f, 3f); paints.Add(pp => HalfShield(pp, at, 7f, col, full, 1f));
            ShieldsShown += (ShieldsShown.Length > 0 ? " " : "") + $"{r.Id}:{(full ? "full" : "part")}";
        }
        /// <summary>The proof reads it: the target shields drawn this frame ("security-1:full").</summary>
        public static string ShieldsShown { get; private set; } = "";

        /// <summary>This frame's target rings (HandoverHost.TargetRings), priority fire &gt; aim &gt; ours &gt; hover &gt; in &gt; long &gt; out.</summary>
        void TargetRingsDraw()
        {
            ShieldsShown = ""; var h = host; if (h.Game == null || m.Ended) return; float now = Time.unscaledTime; var (up, right) = GroundAxes();
            foreach (var r in h.TargetRings)
            {
                var c = r.At; bool threat = r.Fire || r.Aim;
                var side = r.Side == HandoverHost.RingSide.Rival ? RingViolet : r.Side == HandoverHost.RingSide.Guard ? RingAmber : Cream;
                if (r.Focus) { if (!FightCues.Target) { FocusRing(c, side, r.Reach == HandoverHost.RingReach.Long); CoverShield(r, side); } continue; } // engine 0.35 (Glyph b-final): the focus ring, one line, no ticks; brief 101: in his signal colour
                float rad = !threat && r.Hover ? RingHoverR : RingR, line; int dashes = 0; float alpha = 1f, edge = EdgeAlpha;
                if (threat) line = 0.12f;
                else if (r.Hover) line = 0.12f;
                else if (r.Reach == HandoverHost.RingReach.In) line = 0.10f;
                else if (r.Reach == HandoverHost.RingReach.Long) line = 0.075f;
                else { line = 0.065f; dashes = 22; alpha = 0.9f; edge = 0.42f; }
                var col = threat ? RingThreat : side; col.a = alpha;
                if (!threat && r.Reach == HandoverHost.RingReach.Long && r.Side != HandoverHost.RingSide.Civilian) FocusRing(c, col, true, rad, line); else GroundRingM(c, rad, col, line, dashes, edge);
                if (r.Hover && r.Side != HandoverHost.RingSide.Civilian) CoverShield(r, side); // the aim's target: his cover's shield on the ring's front
                // the pulse: one thin red ring leaves at each shot (scale 1 to 1.8, 300 ms, alpha 0.9 to 0)
                float since = now - r.ShotAt;
                if (r.Fire && since >= 0f && since < 0.3f) { float t = since / 0.3f; GroundRingM(c, rad * Mathf.Lerp(1f, 1.8f, t), new Color(RingThreat.r, RingThreat.g, RingThreat.b, Mathf.Lerp(0.9f, 0f, t)), 0.07f, 0, 0f); }
                if (!(threat || r.Ours)) continue;
                // the four ticks, square to the screen: still (ours), closing in (aim: 0.30 m out at alpha 0.5, at rest by 78 % of a 1 s loop with
                // ease-in, then held), kicked in 0.07 m at a shot and eased back over 30 % of the loop (fire)
                float off = 0f, ta = 1f;
                if (r.Fire) { float k = since >= 0f && since < 0.3f ? 1f - since / 0.3f : 0f; off = -0.07f * k * k; }
                else if (r.Aim) { float p = Mathf.Repeat(now, 1f) / 0.78f; if (p < 1f) { float e = 1f - p * p; off = 0.30f * e; } } // Glyph's 0.2.94 review #6: the closing ticks stay at full alpha (at 0.5 they read as faint grey on grey)
                var tick = new Color(Cream.r, Cream.g, Cream.b, ta); var tEdge = new Color(InkDeep.r, InkDeep.g, InkDeep.b, threat ? 1f : TickEdgeAlpha * ta); // threat: the ink edge solid under the cream, as the hover ring's
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
