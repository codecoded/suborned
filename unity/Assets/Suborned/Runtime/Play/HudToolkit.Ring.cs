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
            }
            p.fillColor = new Color(Plate.r, Plate.g, Plate.b, 0.6f); p.BeginPath(); p.Arc(c, ri - 5f, Deg(0), Deg(360)); p.Fill(); // the middle: release here to keep it open
        }

        static Vector2 Dir(float deg) { float a = deg * Mathf.Deg2Rad; return new Vector2(Mathf.Sin(a), -Mathf.Cos(a)); } // 0 at 12 o'clock, clockwise, y down

        void RingFrame()
        {
            var rs = host.Ring; bool on = rs != null && !m.Ended; Show(ringRoot, on);
            if (!on) { ringFor = null; return; }
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
                Show(w.Key, s.Key >= 0); if (s.Key >= 0) Text(w.KeyText, host.VerbKey(s.Key));
                w.Pill.style.unityBackgroundImageTintColor = lit ? new Color(0.75f, 1f, 1f) : Color.white;
                float pw = w.Pill.resolvedStyle.width; if (float.IsNaN(pw) || pw <= 0) pw = 180;
                var at = dir * (ro + 10f);
                bool right = dir.x > 0.2f, left = dir.x < -0.2f;
                float x = right ? at.x : left ? at.x - pw : at.x - pw / 2f, y = at.y - 15f + (Mathf.Abs(dir.x) <= 0.2f ? (dir.y < 0 ? -14f : 14f) : 0f);
                w.Pill.style.left = x; w.Pill.style.top = y;
            }
            ringFor = rs; ringPaint.MarkDirtyRepaint();
        }

        /// <summary>The cursor's mark beside the pointer: a crosshair where a press fires, three dots where other ways are one hold away.</summary>
        void CursorFrame()
        {
            var kind = m.Ended ? "" : host.CursorKind; Show(cursorMark, kind.Length > 0); if (kind.Length == 0) return;
            var p = RuntimePanelUtils.ScreenToPanel(root.panel, m.Mouse);
            cursorMark.style.left = p.x + 12; cursorMark.style.top = p.y + 12;
            var g = cursorMark.Q("g"); Show(g, kind == "aim"); if (kind == "aim") Icon(g, "fire", Cream);
            Show(cursorDots, kind == "ring");
            cursorMark.style.backgroundColor = new Color(Plate.r, Plate.g, Plate.b, 0.85f); Round(cursorMark, 11);
        }
    }
}
