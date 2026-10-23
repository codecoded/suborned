using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 030 (Glyph's mockup briefs/030/radial.html): the ring a held right click (or held E, or Shift + right) opens. Up to five
    /// wedges and the full panel's ⋯, the likely way at 12 o'clock and lit; clockwise in rank, so one kind of target always has one shape.
    /// Each wedge: its icon, the selected agents' cameos when a group can give it, and one pill: one word, then the price in the panel's order.
    /// A violent way has a dashed outline and the loud mark (a shape cue, not a colour); a holstered Fire carries the holstered-gun mark and
    /// "draws 0.6 s". The host reads the pointer against the same geometry (HandoverHost.RingSegAt); nothing here takes a click. Also the
    /// cursor's mark (a crosshair where a press fires, three dots where a hold opens a ring) and the chip's ⋯.</summary>
    public sealed partial class HudToolkit
    {
        sealed class Wedge { public VisualElement Icon, Pill, Key, Cameos, Draw, Loud; public Label Word, KeyText; public VisualElement Fields; public string FieldsKey = ""; }
        VisualElement ringRoot; Paint ringPaint; readonly List<Wedge> wedges = new(); HandoverHost.RingState ringFor; VisualElement cursorMark; Paint cursorDots;

        void BuildRing()
        {
            ringRoot = E(root, null, 0, 0, 0, 0); ringRoot.style.overflow = Overflow.Visible;
            ringPaint = P(ringRoot, -HandoverHost.RingOuter - 4, -HandoverHost.RingOuter - 4, 2 * HandoverHost.RingOuter + 8, 2 * HandoverHost.RingOuter + 8, RingPaint);
            Show(ringRoot, false);
            cursorMark = E(root, null, 0, 0, 22, 22); var g = E(cursorMark, null, 3, 3, 16, 16); g.name = "g";
            cursorDots = P(cursorMark, 0, 0, 22, 22, (p, r) => Dots(p, r.center, 3.2f, 2.2f, Cream)); Show(cursorMark, false);
            manifest.Add((ringRoot, "ring", () => ringFor == null ? "" : string.Join(" · ", ringFor.Segs.ConvertAll(s => s.More ? "⋯" : s.Word))));
        }

        static void Dots(Painter2D p, Vector2 c, float gap, float rad, Color col)
        {
            p.fillColor = col;
            for (int i = -1; i <= 1; i++) { p.BeginPath(); p.Arc(c + new Vector2(i * (gap + rad * 1.4f), 0), rad, Deg(0), Deg(360)); p.Fill(); }
        }

        Wedge NewWedge()
        {
            var w = new Wedge();
            w.Icon = E(ringRoot, null, 0, 0, 22, 22);
            w.Cameos = E(ringRoot, null, 0, 0, float.NaN, 14); w.Cameos.style.flexDirection = FlexDirection.Row;
            w.Pill = E(ringRoot, "s-plate_dark", 0, 0, float.NaN, 30); w.Pill.style.flexDirection = FlexDirection.Row; w.Pill.style.alignItems = Align.Center; w.Pill.style.paddingLeft = 10; w.Pill.style.paddingRight = 12;
            w.Draw = E(w.Pill, null, w: 18, h: 18); Round(w.Draw, 9); Border(w.Draw, 1.4f, Cream); w.Draw.style.marginRight = 5; Icon(E(w.Draw, null, 2.5f, 2.5f, 11, 11), "fire", Cream);
            w.Word = L(w.Pill, "verb-name"); w.Word.style.fontSize = 13.5f; w.Word.style.color = Cream;
            w.Fields = E(w.Pill); w.Fields.style.flexDirection = FlexDirection.Row; w.Fields.style.alignItems = Align.Center;
            w.Loud = E(w.Pill, null, w: 14, h: 14); w.Loud.style.marginLeft = 8;
            w.Key = E(w.Pill, "s-plate_light", w: float.NaN, h: 18); w.Key.style.marginLeft = 8; w.Key.style.minWidth = 22; w.KeyText = L(w.Key, "keycap"); w.KeyText.style.paddingLeft = 4; w.KeyText.style.paddingRight = 4; w.KeyText.style.unityTextAlign = TextAnchor.MiddleCenter;
            wedges.Add(w); return w;
        }

        /// <summary>The wedges: teal enamel, the lit one lifted with a turquoise rim; violent ways dashed.</summary>
        void RingPaint(Painter2D p, Rect r)
        {
            var rs = host.Ring; if (rs == null) return;
            var c = r.center; int n = rs.Segs.Count; float w = 360f / n, gap = n > 1 ? 2.2f : 0f;
            float ro = HandoverHost.RingOuter, ri = HandoverHost.RingInner;
            for (int i = 0; i < n; i++)
            {
                var s = rs.Segs[i]; bool lit = i == (rs.Hover >= 0 ? rs.Hover : rs.Lit);
                float a0 = -90f + i * w - w / 2f + gap, a1 = -90f + i * w + w / 2f - gap;
                p.BeginPath(); p.Arc(c, ro, Deg(a0), Deg(a1)); p.Arc(c, ri, Deg(a1), Deg(a0), ArcDirection.CounterClockwise); p.ClosePath();
                p.fillColor = lit ? DeepTeal : Plate; p.Fill();
                if (rs.HoldSeg == i) // brief 024 rule 2, brief 038 §4.3: a harmful wedge fills from the middle out over its 0.5 s hold, and takes when full
                {
                    float hr = ri + (ro - ri) * host.RingHoldProgress;
                    p.BeginPath(); p.Arc(c, hr, Deg(a0), Deg(a1)); p.Arc(c, ri, Deg(a1), Deg(a0), ArcDirection.CounterClockwise); p.ClosePath();
                    p.fillColor = new Color(TurqLight.r, TurqLight.g, TurqLight.b, 0.75f); p.Fill();
                }
                if (s.Violent) // dashed outline: the loud shape cue
                {
                    p.strokeColor = lit ? TurqLight : Cream; p.lineWidth = 1.6f; p.lineCap = LineCap.Butt;
                    int dashes = Mathf.Max(3, Mathf.RoundToInt((a1 - a0) / 9f)); float step = (a1 - a0) / dashes;
                    for (int d = 0; d < dashes; d++) { p.BeginPath(); p.Arc(c, ro - 1f, Deg(a0 + d * step), Deg(a0 + d * step + step * 0.55f)); p.Stroke(); }
                }
                else if (lit) { p.strokeColor = TurqLight; p.lineWidth = 2f; p.BeginPath(); p.Arc(c, ro - 1f, Deg(a0), Deg(a1)); p.Stroke(); }
                if (s.More) Dots(p, c + Dir(i * w) * ((ro + ri) / 2f), 3.2f, 2.4f, Cream);
                else if (s.Row?.I?.Order is Order so && rs.Plan?.T != null && host.Fresh(rs.Plan.T, so)) Sparkle(p, c + Dir(i * w + w * 0.28f) * (ro - 9f), 6f, Cream); // brief 043: the act that just opened
            }
            p.fillColor = new Color(Plate.r, Plate.g, Plate.b, 0.6f); p.BeginPath(); p.Arc(c, ri - 5f, Deg(0), Deg(360)); p.Fill(); // the middle: release here to keep it open
        }

        static Vector2 Dir(float deg) { float a = deg * Mathf.Deg2Rad; return new Vector2(Mathf.Sin(a), -Mathf.Cos(a)); } // 0 at 12 o'clock, clockwise, y down

        void RingFrame()
        {
            var rs = host.Ring; bool on = rs != null && !m.Ended; Show(ringRoot, on);
            if (!on) { ringFor = null; if (ringFocusEl != null) Show(ringFocusEl, false); RingFocusName = ""; return; }
            var c = RuntimePanelUtils.ScreenToPanel(root.panel, rs.Gui);
            ringRoot.style.left = c.x; ringRoot.style.top = c.y; ringRoot.BringToFront();
            int n = rs.Segs.Count; float step = 360f / n; float ro = HandoverHost.RingOuter, ri = HandoverHost.RingInner;
            while (wedges.Count < n) NewWedge();
            bool group = false; { int sel = 0; for (int k = 0; k < 4; k++) if (host.Selected[k]) sel++; group = sel > 1; }
            for (int i = 0; i < wedges.Count; i++)
            {
                var w = wedges[i]; bool show = i < n; Show(w.Icon, show); Show(w.Pill, show); Show(w.Cameos, show); if (!show) continue;
                var s = rs.Segs[i]; var dir = Dir(i * step); var mid = dir * ((ro + ri) / 2f); bool lit = i == (rs.Hover >= 0 ? rs.Hover : rs.Lit);
                if (s.More) { Show(w.Icon, false); Show(w.Pill, false); Show(w.Cameos, false); continue; }
                Icon(w.Icon, s.Icon, lit ? TurqLight : Cream); w.Icon.style.left = mid.x - 11; w.Icon.style.top = mid.y - 11 - (group ? 6 : 0);
                // the givers: a cameo each, when a group is selected (a way only another selected agent can give shows that agent)
                bool cam = group || (s.By.Count > 0 && s.By[0] != host.Agent);
                Show(w.Cameos, cam);
                if (cam)
                {
                    while (w.Cameos.childCount < s.By.Count) { var cm = E(w.Cameos, null, w: 14, h: 14); Round(cm, 7); cm.style.marginLeft = 1; cm.style.marginRight = 1; }
                    for (int k = 0; k < w.Cameos.childCount; k++) { var cm = w.Cameos[k]; Show(cm, k < s.By.Count); if (k < s.By.Count) Bg(cm, $"portraits/cameo_{Op.Name(s.By[k])}_normal", Color.white); }
                    w.Cameos.style.left = mid.x - 8 * s.By.Count; w.Cameos.style.top = mid.y + 7;
                }
                // the pill: one word, then the price (the panel's order), outside the wedge on its side
                Text(w.Word, s.Word); Show(w.Draw, s.Draws);
                var fs = HudModel.PriceLine(host, s.Row.I);
                if (s.Draws) fs.Insert(0, new HudModel.PField { Kind = "time", Icon = "", Text = $"draws {host.DrawS:0.0#} s" });
                Fields(w.Fields, fs, Cream, ref w.FieldsKey, false, small: false);
                Show(w.Loud, s.Violent); if (s.Violent) Icon(w.Loud, "bang", Cream);
                Show(w.Key, false); // Sky: no key legends (Aster 2026-10-22: the ⇧1 / ⇧2 caps on the plates); the wedge is the click
                w.Pill.style.unityBackgroundImageTintColor = lit ? new Color(0.75f, 1f, 1f) : Color.white;
                float pw = w.Pill.resolvedStyle.width; if (float.IsNaN(pw) || pw <= 0) pw = 180;
                var at = dir * (ro + 10f);
                bool right = dir.x > 0.2f, left = dir.x < -0.2f;
                float x = right ? at.x : left ? at.x - pw : at.x - pw / 2f, y = at.y - 15f + (Mathf.Abs(dir.x) <= 0.2f ? (dir.y < 0 ? -14f : 14f) : 0f);
                w.Pill.style.left = x; w.Pill.style.top = y;
            }
            ringFor = rs; ringPaint.MarkDirtyRepaint();
            RingFocusFrame();
        }

        VisualElement ringFocusEl;
        /// <summary>The thing a hovered wedge names, shown in the world (Sky 2026-10-22: "looping what?"): a pulsing turquoise-light ring round
        /// the camera's head (or the person), and its cone when the cell knows its sight (HostSight). Drawn under the ring.</summary>
        void RingFocusFrame()
        {
            if (ringFocusEl == null)
            {
                ringFocusEl = E(root, null, 0, 0, 44, 44); Round(ringFocusEl, 22); Border(ringFocusEl, 2.6f, LookLive); ringFocusEl.style.backgroundColor = new Color(LookLive.r, LookLive.g, LookLive.b, 0.12f);
                var halo = E(ringFocusEl, null, -3.6f, -3.6f, 47.2f, 47.2f); Round(halo, 23.6f); Border(halo, 1f, new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.7f)); // reads on pale stone
                manifest.Add((ringFocusEl, "image", () => "ring focus"));
            }
            var f = host.RingFocus; Transform t = f.HasValue && host.Views != null ? host.Views.Find(f.Value.view) : null;
            bool camera = f.HasValue && f.Value.view.StartsWith("camera/");
            // a camera's pole view stands 1.6 m up its 3.2 m pole; the head (HostViews.CamHeadFor) is 3.25 m up, the base on the ground
            Vector2 q = default; bool on = t != null && t.gameObject.activeInHierarchy && Proj(t.position + Vector3.up * (camera ? 1.65f : 1.2f), out q);
            Show(ringFocusEl, on); RingFocusName = on ? f.Value.view : "";
            if (!on) return;
            GroundRing(t.position - Vector3.up * (camera ? 1.6f : 0f), 0.8f, LookLive, 2.6f, false, 0.6f); // and on the ground at its foot, behind legs (brief 046)
            float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f);
            ringFocusEl.style.left = q.x - 22; ringFocusEl.style.top = q.y - 22; ringFocusEl.style.scale = new Scale(new Vector3(pulse, pulse, 1f));
            if (ringRoot.parent == ringFocusEl.parent && ringFocusEl.parent.IndexOf(ringFocusEl) > ringFocusEl.parent.IndexOf(ringRoot)) ringFocusEl.PlaceBehind(ringRoot);
        }
        /// <summary>The proof reads it: the view key the ring's hovered wedge shows in the world ("" none).</summary>
        public static string RingFocusName { get; private set; } = "";

        /// <summary>The cursor's mark beside the pointer: a crosshair where a press fires, three dots where other ways are one hold away.</summary>
        void CursorFrame()
        {
            var kind = m.Ended ? "" : host.CursorKind; Show(cursorMark, kind.Length > 0); if (kind.Length == 0) return;
            var p = RuntimePanelUtils.ScreenToPanel(root.panel, m.Mouse);
            float cx = p.x + 12, cy = p.y + 12;
            // Glyph 0.2.79 #4: over a target the mark stood on its ground ring and body (a red wedge showed past it): it goes beside the target,
            // clear of the ring's outer edge and its ticks (hover ring 0.68 m + line + ticks 0.28 m: 1.05 m on the ground), at mid-figure height
            if (kind == "aim" && host.Hover != null && host.TargetWorld(host.Hover) is Vector3 tw)
            {
                var (_, gr) = GroundAxes();
                if (Proj(tw + gr * 1.05f, out var edge) && Proj(tw + Vector3.up * 0.9f, out var mid) && Proj(tw, out var foot))
                {
                    var pe = edge; var pm = mid; float half = 11f;
                    cx = Mathf.Max(cx, pe.x + 6f); cy = pm.y - half;
                    if (Mathf.Abs(p.x - foot.x) > 120f || Mathf.Abs(p.y - foot.y) > 160f) { cx = p.x + 12; cy = p.y + 12; } // the pointer is not on this target: as before
                }
            }
            cursorMark.style.left = cx; cursorMark.style.top = cy;
            var g = cursorMark.Q("g"); Show(g, kind == "aim"); if (kind == "aim") Icon(g, "fire", Cream);
            Show(cursorDots, kind == "ring");
            cursorMark.style.backgroundColor = new Color(Plate.r, Plate.g, Plate.b, 0.85f); Round(cursorMark, 11);
        }
    }
}
