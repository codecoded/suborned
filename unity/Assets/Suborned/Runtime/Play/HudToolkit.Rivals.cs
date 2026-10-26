using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 059 in the kit HUD (Glyph 059 §1-§3 and the shut stand, 2026-10-26; boards 01-04): the rivals' marks, the getaway car's stands and
    /// Vale's car round the back, as ground decals (HudToolkit.Ground: depth-tested, so a building hides what is behind it) with a few kit plates.
    /// Colours, all sourced: violet #8a4fd0 (style contract signal_colours.rivals) on the rivals' ground marks only, cream and ink-deep (kit.uss),
    /// turquoise-light for the live stand's hover (the hold's live accent). No sentence: icons, times and the verb pill.
    ///  · approach: violet 2.2 px over an ink-deep 4.8 px at 60 %, diamond ticks every 2.4 m, trimmed from the nose, the stop an `arrive` icon on
    ///    disc_dark_24; an edge pointer chip (plate_dark, signal_rival in the cameo ring, the pointer turned toward it) while it drives off screen;
    ///  · walks: dotted violet 1.8 px (2.5 on / 5 off) to each post; posts: the rival ring as built (solid 2.4 px at 0.55 m, dashed 3.5/3.5 1.8 px
    ///    82 % at 0.95 m) over the underlay;
    ///  · after a Watch: the spring ring (dashed violet 7/6 1.8 px 78 %) at the rulebook's radius round the cell's stand, on hover or Alt; the follow
    ///    line (dotted violet 2 px, filled chevrons every 3 m) from their car to Vale's car;
    ///  · stands: a dashed cream rounded rectangle 4.6 x 1.9 m (radius 0.5 m) 2.2 px over ink-deep, a chevron at the nose, a disc_dark_24 with
    ///    `car`; hovered: solid turquoise-light and the route preview (cream dashed 7/6 2.4 px, chevrons every 3 m); after Vale's car: 50 %,
    ///    dashes 3/5 (crawling 6 px/s while his car drives round), a plate of nextevent and Vale's cameo; struck (drive-held): 45 %, no disc;
    ///  · Vale's car (board 03): the cream dashed route with chevrons, the times plate (car 6:13, pointer 6:25), the hand-off ring (cream 0.8 m,
    ///    2.6 px) at the rear door with a disc_dark_24 carrying `take`.</summary>
    public sealed partial class HudToolkit
    {
        static readonly Color RivalViolet = Hex(0x8a4fd0);
        VisualElement rvStop, rvEdge, rvPlate; Paint rvEdgeArrow; float rvEdgeAngle;
        readonly List<(VisualElement root, VisualElement disc)> standDiscs = new(); readonly List<VisualElement> standPlates = new();
        VisualElement valePlate, valeTake; Label valeFrom, valeTo;
        /// <summary>The proof reads them: what the rival marks drew this frame, and the stands'.</summary>
        public static string RivalsShown { get; private set; } = "";
        public static string StandsShown { get; private set; } = "";
        public static string ValeRouteShown { get; private set; } = "";

        /// <summary>A polyline on the ground, dashed by `onPx`/`offPx` HUD pixels (0 off: solid), `wPx` wide, over an ink-deep underlay `underPx` at `underA`.</summary>
        void GroundDashes(List<Vector3> pts, Color col, float wPx, float onPx, float offPx, float underPx, float underA, float phasePx = 0f)
        {
            if (pts == null || pts.Count < 2) return; float m = GroundPxM;
            float on = onPx * m, off = offPx * m, w = wPx * m, uw = underPx * m;
            var ink = underA > 0f ? GroundOf(new Color(InkDeep.r, InkDeep.g, InkDeep.b, underA), true) : null; var c = GroundOf(col, false);
            float period = on + off, d = off > 0f ? -(phasePx * m % period) : 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                var a = pts[i - 1]; var b = pts[i]; var ab = b - a; ab.y = 0f; float len = ab.magnitude; if (len < 1e-4f) continue;
                if (off <= 0f) { if (ink != null) SegQuad(ink, a, b, uw); SegQuad(c, a, b, w); continue; }
                for (float t = d; t < len; t += period) { float s0 = Mathf.Max(0f, t), s1 = Mathf.Min(len, t + on); if (s1 <= s0) continue; var p0 = Vector3.Lerp(a, b, s0 / len); var p1 = Vector3.Lerp(a, b, s1 / len); if (ink != null) SegQuad(ink, p0, p1, uw); SegQuad(c, p0, p1, w); }
                d = ((d - len) % period + period) % period; if (d > 0f) d -= period;
            }
        }

        /// <summary>Marks every `stepM` metres along a polyline (from half a step in): its point and direction.</summary>
        static IEnumerable<(Vector3 p, Vector3 f)> Every(List<Vector3> pts, float stepM)
        {
            float next = stepM * 0.5f, run = 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                var a = pts[i - 1]; var b = pts[i]; var ab = b - a; ab.y = 0f; float len = ab.magnitude; if (len < 1e-4f) continue; var f = ab / len;
                while (next <= run + len) { yield return (Vector3.Lerp(a, b, (next - run) / len), f); next += stepM; }
                run += len;
            }
        }

        void DiamondAt(Vector3 p, Vector3 f, float halfPx, Color col)
        {
            float m = GroundPxM, h = halfPx * m; var s = Vector3.Cross(Vector3.up, f); float y = GroundLift + 0.003f;
            var ink = GroundOf(InkDeep, true); var c = GroundOf(col, false); float hi = h + 1.2f * m;
            Quad(ink, p + f * hi + Vector3.up * y, p + s * hi + Vector3.up * y, p - f * hi + Vector3.up * y, p - s * hi + Vector3.up * y);
            Quad(c, p + f * h + Vector3.up * (y + 0.001f), p + s * h + Vector3.up * (y + 0.001f), p - f * h + Vector3.up * (y + 0.001f), p - s * h + Vector3.up * (y + 0.001f));
        }

        void ChevronAt(Vector3 p, Vector3 f, float sizePx, Color col)
        {
            float m = GroundPxM, s0 = sizePx * m; var s = Vector3.Cross(Vector3.up, f) * (s0 * 0.5f); var tip = p + f * (s0 * 0.55f); var back = p - f * (s0 * 0.45f); float y = GroundLift + 0.003f;
            Quad(GroundOf(new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.6f), true), back + s * 1.35f + Vector3.up * y, tip + f * m + Vector3.up * y, tip + f * m + Vector3.up * y, back - s * 1.35f + Vector3.up * y);
            Quad(GroundOf(col, false), back + s + Vector3.up * (y + 0.001f), tip + Vector3.up * (y + 0.001f), tip + Vector3.up * (y + 0.001f), back - s + Vector3.up * (y + 0.001f));
        }

        /// <summary>A circle on the ground as a closed polyline (n points), dashed in HUD pixels.</summary>
        void GroundCircleDash(Vector3 c, float r, Color col, float wPx, float onPx, float offPx, float underPx, float underA)
        {
            int n = Mathf.Clamp(Mathf.CeilToInt(r * 12f), 32, 256); var pts = new List<Vector3>(n + 1);
            for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2f / n; pts.Add(c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r)); }
            GroundDashes(pts, col, wPx, onPx, offPx, underPx, underA);
        }

        /// <summary>A rounded rectangle on the ground (a car's footprint), centre `c`, nose `f`, as a closed polyline.</summary>
        static List<Vector3> Footprint(Vector3 c, Vector3 f, float len = 4.6f, float wid = 1.9f, float rad = 0.5f)
        {
            f.y = 0f; f = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward; var s = Vector3.Cross(Vector3.up, f);
            float hl = len / 2f - rad, hw = wid / 2f - rad; var pts = new List<Vector3>();
            (float cx, float cy, float a0)[] corners = { (hl, hw, 0f), (-hl, hw, 90f), (-hl, -hw, 180f), (hl, -hw, 270f) };
            foreach (var (cx, cy, a0) in corners)
                for (int k = 0; k <= 6; k++) { float a = (a0 + k * 15f) * Mathf.Deg2Rad; pts.Add(c + f * (cx + Mathf.Cos(a) * rad) + s * (cy + Mathf.Sin(a) * rad)); }
            pts.Add(pts[0]); return pts;
        }

        static Color A(Color c, float a) => new(c.r, c.g, c.b, c.a * a);

        void RivalsDraw()
        {
            var h = host; var rv = h.Rivals; RivalsShown = ""; if (marks == null) return;
            if (rvStop == null)
            {
                rvStop = E(marks, "s-disc_dark_24", 0, 0, 22, 22); rvStop.pickingMode = PickingMode.Ignore; var sg = E(rvStop, null, 4.5f, 4.5f, 13, 13); sg.name = "g"; Show(rvStop, false); manifest.Add((rvStop, "image", () => "rival stop"));
                rvEdge = PlateDark(marks, 22f, 0.3f); rvEdge.style.position = Position.Absolute; rvEdge.style.width = 52; rvEdge.style.paddingLeft = 6; rvEdge.style.paddingRight = 6; rvEdge.style.justifyContent = Justify.SpaceBetween;
                var cm = Cameo(rvEdge, 12f, 2f); Bg(cm.Q("face"), "badges/signal_rival", Color.white);
                rvEdgeArrow = P(rvEdge, 0, 0, 14, 14, (pt, r) =>
                {
                    var c = r.center; float a = rvEdgeAngle; Vector2 R(Vector2 v) => new(v.x * Mathf.Cos(a) - v.y * Mathf.Sin(a), v.x * Mathf.Sin(a) + v.y * Mathf.Cos(a));
                    pt.fillColor = Cream; pt.BeginPath(); pt.MoveTo(c + R(new Vector2(6f, 0f))); pt.LineTo(c + R(new Vector2(-4.5f, 5f))); pt.LineTo(c + R(new Vector2(-1.5f, 0f))); pt.LineTo(c + R(new Vector2(-4.5f, -5f))); pt.ClosePath(); pt.Fill();
                });
                rvEdgeArrow.style.position = Position.Relative; rvEdgeArrow.style.flexShrink = 0;
                Show(rvEdge, false); manifest.Add((rvEdge, "image", () => "rival car edge chip"));
            }
            Show(rvStop, false); Show(rvEdge, false);
            if (!rv.On || m.Ended || h.View == null) return;
            var bits = new List<string>();
            // the approach and its stop
            if (rv.Moving && rv.Approach.Count >= 2)
            {
                GroundDashes(rv.Approach, RivalViolet, 2.2f, 0f, 0f, 4.8f, 0.6f);
                foreach (var (p, f) in Every(rv.Approach, 2.4f)) DiamondAt(p, f, 3.4f, RivalViolet);
                if (Proj(rv.Stop, out var sq) && InView(sq)) { Show(rvStop, true); rvStop.style.left = sq.x - 11; rvStop.style.top = sq.y - 11; Icon(rvStop.Q("g"), "arrive", Cream); }
                bits.Add($"approach:{rv.Approach.Count}");
                if (rv.Edge)
                {
                    var W = root.layout.width > 0 ? root.layout.width : 1920f; var H = root.layout.height > 0 ? root.layout.height : 1080f; var c = new Vector2(W / 2f, H / 2f);
                    var cam = h.View; var sp = cam.WorldToScreenPoint(rv.Car); var raw = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(sp.x, Screen.height - sp.y)); if (sp.z < 0) raw = c - (raw - c);
                    var dir = raw - c; if (dir.sqrMagnitude < 1f) dir = Vector2.right; dir.Normalize();
                    float k = Mathf.Min((W / 2f - 50f) / Mathf.Max(1e-3f, Mathf.Abs(dir.x)), (H / 2f - 30f) / Mathf.Max(1e-3f, Mathf.Abs(dir.y))); var at = c + dir * k;
                    for (int step = 0; step < 4 && EdgeCamHits(new Rect(at.x - 26f, at.y - 11f, 52f, 22f), 6f); step++) at -= dir * 30f;
                    Show(rvEdge, true); rvEdge.style.left = at.x - 26f; rvEdge.style.top = at.y - 11f;
                    float ang = Mathf.Atan2(dir.y, dir.x); if (Mathf.Abs(ang - rvEdgeAngle) > 0.01f) { rvEdgeAngle = ang; rvEdgeArrow.MarkDirtyRepaint(); }
                    bits.Add("edge");
                }
            }
            // the walks and the posts
            foreach (var (walk, post, ring) in rv.Walks)
            {
                GroundDashes(walk, RivalViolet, 1.8f, 2.5f, 5f, 3.8f, 0.6f);
                if (ring) PostRing(post);
            }
            foreach (var p in rv.Posts) PostRing(p);
            if (rv.Walks.Count > 0) bits.Add($"walks:{rv.Walks.Count}"); if (rv.Posts.Count > 0) bits.Add($"posts:{rv.Posts.Count}");
            // after a Watch
            if (rv.Spring) { GroundCircleDash(rv.SpringAt, rv.SpringR, A(RivalViolet, 0.78f), 1.8f, 7f, 6f, 3.8f, 0.5f); bits.Add($"spring:{rv.SpringR:0}"); }
            if (rv.Follow)
            {
                var line = new List<Vector3> { rv.Car, rv.FollowTo };
                GroundDashes(line, RivalViolet, 2f, 2.5f, 5f, 4f, 0.6f);
                foreach (var (p, f) in Every(line, 3f)) ChevronAt(p, f, 5.5f, RivalViolet);
                bits.Add("follow");
            }
            if (rv.Watched) bits.Add("watched");
            RivalsShown = string.Join(" ", bits);
        }

        /// <summary>The rival ring as built (Glyph 059 §1): solid 2.4 px at 0.55 m, dashed (3.5 on / 3.5 off, 1.8 px, 82 %) at 0.95 m, over the underlay.</summary>
        void PostRing(Vector3 c)
        {
            GroundCircleDash(c, 0.55f, RivalViolet, 2.4f, 0f, 0f, 4.4f, 0.6f);
            GroundCircleDash(c, 0.95f, A(RivalViolet, 0.82f), 1.8f, 3.5f, 3.5f, 3.6f, 0.5f);
        }

        /// <summary>Brief 067 C5a (Glyph): the stands' footprint alpha at rest.</summary>
        public static float StandRestAlpha = 0.55f;

        void DriveDraw()
        {
            var h = host; var d = h.Drive; StandsShown = ""; ValeRouteShown = ""; if (marks == null) return;
            int used = 0, plates = 0; float now = Time.unscaledTime; var bits = new List<string>();
            if (d.On && !m.Ended && h.View != null)
                foreach (var gh in d.Ghosts)
                {
                    float alpha = gh.Struck ? 0.45f : gh.Shut ? (gh.Hover ? 0.7f : 0.5f) : 1f;
                    float liveK = gh.Hover ? 1f : d.LiveK; alpha *= Mathf.Lerp(StandRestAlpha, 1f, liveK); // brief 067 C5a (Glyph): at rest (anyone aboard, nothing live) the footprint at 55 %, no disc, no route; live eases in over 0.25 s
                    float open = now - gh.OpenedAt; bool opening = open >= 0f && open < 0.85f;
                    float scale = opening && open > 0.25f ? 1f + 0.04f * Mathf.Sin(Mathf.Clamp01((open - 0.25f) / 0.6f) * Mathf.PI) : 1f;
                    var fp = Footprint(gh.At, gh.Fwd, 4.6f * scale, 1.9f * scale);
                    var col = gh.Hover && !gh.Shut && !gh.Struck ? TurqLight : Cream;
                    if (gh.Hover && !gh.Struck) GroundDashes(fp, A(col, alpha), 2.2f, 0f, 0f, 4.2f, 0.6f);                       // hovered: solid
                    else if (gh.Shut) GroundDashes(fp, A(col, alpha), 2.2f, 3f, 5f, 4.2f, 0.6f * alpha, gh.Crawl ? now * 6f : 0f); // after Vale's car: short dashes, crawling while his car drives round
                    else GroundDashes(fp, A(col, alpha), 2.2f, 7f, 6f, 4.2f, 0.6f * alpha);
                    ChevronAt(gh.At + gh.Fwd * 2.05f * scale, gh.Fwd, 7f, A(col, alpha));
                    if (gh.Route.Count >= 2) { GroundDashes(gh.Route, LookGround, 2.4f, 7f, 6f, 4.4f, 0.6f); foreach (var (p, f) in Every(gh.Route, 3f)) ChevronAt(p, f, 6f, LookGround); }
                    // the disc at the centre (none under the car), the shut plate above the far edge
                    if (!gh.Here && !gh.Struck && liveK > 0.01f && Proj(gh.At, out var q) && InView(q))
                    {
                        while (standDiscs.Count <= used) { var r = E(marks, "s-disc_dark_24", 0, 0, 24, 24); r.pickingMode = PickingMode.Ignore; var gl = E(r, null, 5, 5, 14, 14); gl.name = "g"; standDiscs.Add((r, gl)); manifest.Add((r, "image", () => "stand disc")); }
                        var (dr, dg) = standDiscs[used++]; Show(dr, true); dr.style.left = q.x - 12; dr.style.top = q.y - 12; dr.style.opacity = (gh.Shut ? 0.5f : 1f) * liveK; Icon(dg, "car", Cream);
                    }
                    if (gh.Shut || opening && gh.OpenedAt > 0f)
                    {
                        float fade = gh.Shut ? 1f : Mathf.Clamp01(1f - open / 0.25f);
                        if (fade > 0f && Proj(gh.At + gh.Fwd * -2.4f + Vector3.up * 0.4f, out var pq) && InView(pq))
                        {
                            while (standPlates.Count <= plates)
                            {
                                var pl = PlateDark(marks, 26f, 0.3f); pl.style.position = Position.Absolute; pl.style.width = 60; pl.style.paddingLeft = 9; pl.style.paddingRight = 7; pl.style.justifyContent = Justify.SpaceBetween;
                                var ni = E(pl, null, w: 14, h: 14); ni.name = "g"; ni.style.flexShrink = 0; var cm = Cameo(pl, 12f, 2f); cm.name = "vale"; Bg(cm.Q("face"), "badges/npc_councillor", Color.white);
                                standPlates.Add(pl); manifest.Add((pl, "image", () => "stand after Vale's car"));
                            }
                            var p2 = standPlates[plates++]; Show(p2, true); Icon(p2.Q("g"), "nextevent", Cream); p2.style.left = pq.x - 30; p2.style.top = pq.y - 26 - 14; p2.style.opacity = fade;
                        }
                    }
                    bits.Add(gh.Id + (gh.Here ? ":here" : gh.Shut ? ":after-vale" : gh.Struck ? ":struck" : "") + (gh.Hover ? ":hover" : "") + (gh.Route.Count >= 2 ? ":route" : "") + (liveK < 0.5f ? ":rest" : ""));
                }
            for (int i = used; i < standDiscs.Count; i++) Show(standDiscs[i].root, false);
            for (int i = plates; i < standPlates.Count; i++) Show(standPlates[i], false);
            StandsShown = string.Join(" ", bits);
            ValeRouteDraw();
        }

        void ValeRouteDraw()
        {
            var v = host.ValeRoute;
            if (valePlate == null)
            {
                valePlate = PlateDark(marks, 28f, 0.3f); valePlate.style.position = Position.Absolute; valePlate.style.paddingLeft = 9; valePlate.style.paddingRight = 10;
                var ci = E(valePlate, null, w: 14, h: 14); ci.name = "car"; ci.style.flexShrink = 0; valeFrom = Words(valePlate, 13.5f); valeFrom.style.marginLeft = 5;
                var pi = E(valePlate, null, w: 12, h: 12); pi.name = "pointer"; pi.style.marginLeft = 5; pi.style.flexShrink = 0; valeTo = Words(valePlate, 13.5f); valeTo.style.marginLeft = 5;
                Show(valePlate, false); manifest.Add((valePlate, "image", () => "vale car times"));
                valeTake = E(marks, "s-disc_dark_24", 0, 0, 22, 22); valeTake.pickingMode = PickingMode.Ignore; var tg = E(valeTake, null, 4.5f, 4.5f, 13, 13); tg.name = "g"; Show(valeTake, false); manifest.Add((valeTake, "image", () => "hand-off take"));
            }
            Show(valePlate, false); Show(valeTake, false);
            if (m.Ended || host.View == null || !(v.On || v.HandOff)) return;
            var bits = new List<string>();
            if (v.On && v.Route.Count >= 2)
            {
                GroundDashes(v.Route, LookGround, 2.4f, 7f, 6f, 4.4f, 0.6f);
                foreach (var (p, f) in Every(v.Route, 3f)) ChevronAt(p, f, 6f, LookGround);
                bits.Add("route");
                if (v.Plate && Proj(v.PlateAt + Vector3.up * 0.3f, out var pq) && InView(pq))
                {
                    Show(valePlate, true); Icon(valePlate.Q("car"), "car", Cream); Icon(valePlate.Q("pointer"), "pointer", Cream); Text(valeFrom, v.From); Text(valeTo, v.To);
                    float w = float.IsNaN(valePlate.resolvedStyle.width) ? 110f : valePlate.resolvedStyle.width; valePlate.style.left = pq.x - w / 2f; valePlate.style.top = pq.y - 14f;
                    bits.Add($"times:{v.From}>{v.To}");
                }
            }
            if (v.HandOff)
            {
                GroundCircleDash(v.HandOffAt, 0.8f, LookGround, 2.6f, 0f, 0f, 4.6f, 0.6f);
                if (Proj(v.TakeDisc, out var tq) && InView(tq)) { Show(valeTake, true); valeTake.style.left = tq.x - 11; valeTake.style.top = tq.y - 11; Icon(valeTake.Q("g"), "take", Cream); }
                bits.Add(v.Parked ? (v.AtMouth ? "handoff:mouth" : "handoff:bay") : "handoff:preview");
            }
            ValeRouteShown = string.Join(" ", bits);
        }
    }
}
