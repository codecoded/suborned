using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 013 E in the kit view (Glyph's spec briefs/013/glyph-takedown-hud.md, the 011 kit's sprites and Glyph's 11 new glyphs):
    /// the takedown's way pills under the armed knob, the card's "Before you act" panel, and the marks in the street, drawn over the world
    /// (and over the building fade, as the HUD is): one marker per body (round with a wake ring for a knockout, square for a kill, a second
    /// small disc when tied, a dashed ring once found), stash plates (while carrying, on hover, with Alt; hollow with a dashed leader when
    /// behind a building), a held door's lock, ring and tape (a hollow dashed lock as the card's preview), a report in transit (one disc, a
    /// ring to its landing, a dotted line to the call box), the response's unit badges (merged within 2.5 m) with arrival chips at the
    /// screen's edge, the response pill under the status pill, and the mast heads' number plates. Nothing new is red, amber or violet.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement before, marks, respPill, respList; Label respLine; readonly List<(VisualElement row, VisualElement icon, Label text)> beforeRows = new();
        readonly List<(VisualElement row, VisualElement icon, Label label, Label text)> respRows = new();

        sealed class Mark { public VisualElement Root, Plate, Glyph, Second, Tape, Leader; public Label Text; public KeyText Name; public bool Tight, Hot; public float Lead = -1f, Frac = -1f; public Paint Clock; public readonly List<VisualElement> Ring = new(), Outer = new(), Dots = new(), Pips = new(); }
        readonly List<Mark> bodyMarks = new(), stashMarks = new(), doorMarks = new(), reportMarks = new(), unitMarks = new(), edgeMarks = new(), headMarks = new();

        // ---------------------------------------------------------------- build

        void BuildBefore()
        {
            before = E(null, "s-panel_card"); before.style.width = 368; before.style.marginBottom = 6; before.style.paddingBottom = 8;
            var hb = E(before); hb.style.height = 28; hb.style.flexDirection = FlexDirection.Row; hb.style.alignItems = Align.Center; hb.style.paddingLeft = 10;
            Icon(E(hb, null, w: 16, h: 16), "watch", Cream); var t = L(hb, "card-title"); t.text = "Before you act"; t.style.marginLeft = 6;
            var body = E(before); body.style.paddingLeft = 14; body.style.paddingRight = 14; body.style.paddingTop = 6;
            for (int i = 0; i < 6; i++)
            {
                var r = E(body); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.FlexStart; r.style.marginTop = 3;
                var ic = E(r, null, w: 13, h: 13); ic.style.marginRight = 6; ic.style.marginTop = 1; ic.style.flexShrink = 0;
                var tx = L(r, "card-body"); tx.style.whiteSpace = WhiteSpace.Normal; tx.style.flexShrink = 1; beforeRows.Add((r, ic, tx));
            }
            panelCards.Insert(0, before); Show(before, false);
        }

        void BeforeFrame()
        {
            bool on = m.PanelBefore.Count > 0; Show(before, on); if (!on) return;
            for (int i = 0; i < beforeRows.Count; i++) { var (r, ic, tx) = beforeRows[i]; Show(r, i < m.PanelBefore.Count); if (i < m.PanelBefore.Count) { Icon(ic, m.PanelBefore[i].icon, Ink); Text(tx, m.PanelBefore[i].text); } }
        }

        void BuildTakedown()
        {
            marks = E(root); marks.style.position = Position.Absolute; marks.style.left = 0; marks.style.top = 0; marks.style.right = 0; marks.style.bottom = 0; marks.SendToBack(); // under the panels, over the world
            marks.style.alignItems = Align.FlexStart; // marks size to their content (brief 014: bubbles, pops, name tags)
            // the way pills under the armed Takedown knob retire (brief 101 round 4): the verb row names the way, and its tab again flips it (HudToolkit.VerbRow)
            // the response pill (Glyph §8): under the status pill, collapsed like Intel; R or a click opens the list
            respPill = E(root, "s-panel_strip", pick: true); respPill.style.position = Position.Absolute; respPill.style.right = 14; respPill.style.width = 318; respPill.style.height = 34;
            respPill.style.flexDirection = FlexDirection.Row; respPill.style.alignItems = Align.Center; respPill.style.paddingLeft = 22;
            respPill.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.ResponseOpen = !host.ResponseOpen; e.StopPropagation(); } });
            Icon(E(respPill, null, w: 15, h: 15), "units", Cream); respLine = L(respPill, "panel-sub", ellipsis: true); respLine.style.marginLeft = 8; respLine.style.flexShrink = 1;
            respList = E(root, "s-panel_dark", pick: true); respList.style.position = Position.Absolute; respList.style.right = 14; respList.style.width = 318; respList.style.paddingTop = 12; respList.style.paddingBottom = 12; respList.style.paddingLeft = 22; respList.style.paddingRight = 18;
            for (int i = 0; i < 4; i++)
            {
                var r = E(respList); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.height = 26;
                var ic = E(r, null, w: 14, h: 14); var lb = L(r, "intel-name"); lb.style.marginLeft = 8; lb.style.width = 90; var tx = L(r, "panel-sub"); tx.style.flexShrink = 1;
                respRows.Add((r, ic, lb, tx));
            }
            Show(respPill, false); Show(respList, false);
        }

        readonly List<Rect> stashPlaced = new();
        static VisualElement Dot(VisualElement parent, float x, float y, float d, Color c) { var e = E(parent, null, x - d / 2, y - d / 2, d, d); Round(e, d / 2); e.style.backgroundColor = c; return e; }

        Mark NewMark(List<Mark> pool, string plate, float w, float h, bool square = false)
        {
            var k = new Mark { Root = E(marks, null, 0, 0, w, h) };
            k.Plate = E(k.Root, plate, 0, 0, w, h); if (square) Round(k.Plate, 5);
            k.Glyph = E(k.Plate, null, (w - 16) / 2, (h - 16) / 2, 16, 16);
            pool.Add(k); return k;
        }

        static void Place(VisualElement e, Vector2 p, float w, float h) { e.style.left = p.x - w / 2; e.style.top = p.y - h / 2; }

        bool Proj(Vector3 world, out Vector2 p)
        {
            p = default; var cam = host.View; if (!cam) return false;
            var sp = cam.WorldToScreenPoint(world); if (sp.z <= 0) return false;
            p = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(sp.x, Screen.height - sp.y)); return true;
        }

        void Ring(Mark k, int n, float cx, float cy, float r, float d) { for (int i = 0; i < n; i++) { float a = Mathf.PI / 2 - i * Mathf.PI * 2 / n; k.Ring.Add(Dot(k.Root, cx + r * Mathf.Cos(a), cy - r * Mathf.Sin(a), d, TurqLight)); } }
        void Outer(Mark k, int n, float cx, float cy, float r, float d) { for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; k.Outer.Add(Dot(k.Root, cx + r * Mathf.Cos(a), cy - r * Mathf.Sin(a), d, Cream)); } }
        /// <summary>Glyph r5 #7 (rule 3: time that runs is the tab's own ring): a knocked-out body's tab rings its wake, the book's ink filling in cream toward waking, orange in
        /// its last 10 s (it retires the 16 turquoise wake dots, a second clock form); a dead body keeps the plain cream ring.</summary>
        void WakeRing(Mark u, HandoverHost.BodyMark b)
        {
            bool timed = !b.Dead; if ((u.Clock.style.display != DisplayStyle.None) != timed) { BorderW(u.Plate, timed ? 0f : 2f); Show(u.Clock, timed); }
            if (!timed) return;
            float left = (1f - b.Wake) * (host.Game?.R.WakeS ?? 0); bool hot = b.Wake > 0f && left <= 10f;
            if (Mathf.Abs(b.Wake - u.Frac) > 0.002f || hot != u.Hot) { u.Frac = b.Wake; u.Hot = hot; u.Clock.MarkDirtyRepaint(); }
        }

        static void Fill(Mark k, float frac, bool on) { int lit = Mathf.RoundToInt(frac * k.Ring.Count); for (int i = 0; i < k.Ring.Count; i++) { Show(k.Ring[i], on); k.Ring[i].style.backgroundColor = i < lit ? TurqLight : new Color(1, 1, 1, 0.22f); } }

        // ---------------------------------------------------------------- per frame

        void TakedownFrame()
        {
            // bodies: a round tab, its own ring the wake clock (knockout); a square plate (kill); tied: a small second disc; found: a dashed outer ring
            int bi = 0;
            foreach (var b in m.Bodies)
            {
                if (!Proj(b.World, out var p)) continue;
                if (bodyMarks.Count <= bi) { var k = NewMark(bodyMarks, "e-body e-inline", 44, 44); Round(k.Plate, 13f); BorderW(k.Plate, 2f); k.Plate.style.left = k.Plate.style.top = 9; k.Plate.style.width = k.Plate.style.height = 26; k.Glyph.style.left = k.Glyph.style.top = 6.25f; k.Glyph.style.width = k.Glyph.style.height = 13.5f; var bsh = Disc(k.Root, 26f, "e-shadow"); Pin(bsh, new Vector2(9f, 11f)); bsh.style.opacity = 0.6f; bsh.SendToBack(); var kc = k; k.Clock = P(k.Plate, 0, 0, 26, 26, (pp, rc) => TimedRing(pp, rc, kc.Frac, kc.Hot)); Show(k.Clock, false); Outer(k, 12, 22, 22, 20, 3f); k.Second = E(k.Root, "e-body e-inline", 30, 0, 16, 16); Round(k.Second, 8f); BorderW(k.Second, 1.6f); Icon(E(k.Second, null, 3.5f, 3.5f, 9, 9), "restrain", Cream); /* brief 101 round 3 (Glyph's rule 2): the body a round tab; Glyph r5 #7: its own ring is the wake clock (WakeRing) */ manifest.Add((k.Root, "image", () => k.Tight ? null : "body")); manifest.Add((k.Plate, "image", () => k.Tight ? "body" : null)); } // Glyph N5 (0.2.137): a bare kill is its 30 x 30 plate; a knockout (its wake ring), a found or tied body the whole 44 px root
                var u = bodyMarks[bi++]; Show(u.Root, true); Place(u.Root, p, 44, 44);
                // brief 101 round 3: knocked out or dead, the same round tab; its pictogram says which (body_out, body_dead)
                Icon(u.Glyph, b.Dead ? "body_dead" : "body_out", Cream);
                WakeRing(u, b);
                foreach (var d in u.Outer) Show(d, b.Found);
                Show(u.Second, b.Tied); u.Tight = b.Dead && !b.Found && !b.Tied;
                u.Root.style.opacity = b.Hidden ? 0.75f : 1f;
            }
            for (int i = bi; i < bodyMarks.Count; i++) Show(bodyMarks[i].Root, false);
            // brief 101 round 3 (Glyph's rule 1): a stash's name is a street label under its point (its pictogram, the name, a pip a body: ink full, cream empty), a fine stem up
            // to the point, so it never climbs into the clock stack; a later one drops below an earlier; behind a building it fades
            int si = 0; stashPlaced.Clear();
            foreach (var st in m.Stashes)
            {
                if (!Proj(st.World, out var p)) continue;
                if (stashMarks.Count <= si)
                {
                    var (row, ic, nmK) = KeyRow(marks, 16f, 15f); row.style.position = Position.Absolute; ic.Set(this, "stash"); ic.Look("ki-ink", "ki-key");
                    var k = new Mark { Root = row, Plate = row, Name = nmK }; stashMarks.Add(k); var kk = k; manifest.Add((row, "marker", () => kk.Name.Root.style.display == DisplayStyle.None ? "" : kk.Name.Words)); // a stash's name is a street mark of a word or two (G2: every street mark in the manifest)
                    k.Leader = P(row, 6f, -4f, 8f, 8f, (pp, rc) => // the stem from the label's top up to its point, the point a cream dot in an ink ring (drawn here: the street's paints reset after this pass)
                    {
                        float L = kk.Lead; if (L < 2f) return; var top = new Vector2(4f, 4f); var foot = new Vector2(4f, 4f + L);
                        pp.lineCap = LineCap.Round; pp.strokeColor = new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.51f); pp.lineWidth = 4f; pp.BeginPath(); pp.MoveTo(top); pp.LineTo(foot); pp.Stroke();
                        pp.strokeColor = StPage; pp.lineWidth = 2.2f; pp.BeginPath(); pp.MoveTo(top); pp.LineTo(foot); pp.Stroke();
                        pp.fillColor = BkInk; pp.BeginPath(); pp.Arc(top, 3.8f, Deg(0), Deg(360)); pp.Fill(); pp.fillColor = StPage; pp.BeginPath(); pp.Arc(top, 2.6f, Deg(0), Deg(360)); pp.Fill();
                    });
                    for (int j = 0; j < 3; j++) { var pp = E(row, "e-inline", w: 8, h: 8); Round(pp, 4f); BorderW(pp, 1.4f); pp.style.marginLeft = j == 0 ? 5 : 2; pp.style.flexShrink = 0; k.Pips.Add(pp); }
                }
                var u = stashMarks[si++]; Show(u.Root, true); u.Name.Set(st.Name);
                float pw = u.Root.resolvedStyle.width; if (float.IsNaN(pw) || pw < 1) pw = 130; const float SH = 22f;
                var at = new Rect(p.x - 10f, p.y + 16f, pw, SH); // the stem lands on the pictogram
                for (int g2 = 0; g2 < 4; g2++) { bool hit = false; foreach (var q in stashPlaced) if (q.Overlaps(new Rect(at.x - 4, at.y - 4, at.width + 8, at.height + 8))) { at.y = q.yMax + 8f; hit = true; } if (!hit) break; }
                stashPlaced.Add(at); u.Root.style.left = at.x; u.Root.style.top = at.y;
                for (int j = 0; j < u.Pips.Count; j++) { Show(u.Pips[j], j < st.Holds); if (j < st.Holds) { bool full = j < st.Used; u.Pips[j].style.backgroundColor = full ? BkInk : StPage; RingColour(u.Pips[j], full ? (Color?)null : BkInk); } }
                float al = st.Hover ? 1f : st.Behind ? 0.62f : 0.92f; u.Root.style.opacity = al;
                float lead = at.y - p.y; if (Mathf.Abs(lead - u.Lead) > 0.5f) { u.Lead = lead; u.Leader.style.top = -lead - 4f; u.Leader.style.height = lead + 8f; u.Leader.MarkDirtyRepaint(); }
            }
            for (int i = si; i < stashMarks.Count; i++) Show(stashMarks[i].Root, false);
            stashPlaced.Clear();
            // held doors: the lock disc, a ring counting to the stand-down (full until the host can read it), tape at the foot; Alert 3: "locked"
            int di = 0;
            foreach (var d in m.Doors)
            {
                if (!Proj(d.World, out var p)) continue;
                if (doorMarks.Count <= di)
                {
                    var k = NewMark(doorMarks, "s-disc_dark_30", 46, 46); k.Plate.style.left = 8; k.Plate.style.top = 8; k.Plate.style.width = 30; k.Plate.style.height = 30; k.Glyph.style.left = 7; k.Glyph.style.top = 7;
                    Ring(k, 18, 23, 23, 19, 3.2f); Outer(k, 14, 23, 23, 19, 3f);
                    var tp = E(k.Root, "s-plate_dark", -20, 48, 86, 18); k.Text = L(tp, "verb-name"); k.Text.style.flexGrow = 1; k.Text.style.unityTextAlign = TextAnchor.MiddleCenter; k.Second = tp;
                    k.Tape = E(marks, "s-stripes_warn", 0, 0, 54, 9);
                }
                var u = doorMarks[di++]; Show(u.Root, true); Place(u.Root, p, 46, 46);
                Icon(u.Glyph, d.Preview ? "door_held" : "lock", Cream);
                u.Plate.style.opacity = d.Preview ? 0.45f : 1f;
                Fill(u, d.Frac, !d.Preview && !d.Locked); foreach (var o in u.Outer) Show(o, d.Preview);
                Text(u.Text, d.Text);
                Vector2 foot = default; bool tape = !d.Preview && Proj(d.World + Vector3.down * 2.7f, out foot); Show(u.Tape, tape); if (tape) Place(u.Tape, foot, 54, 9);
            }
            for (int i = di; i < doorMarks.Count; i++) { Show(doorMarks[i].Root, false); Show(doorMarks[i].Tape, false); }
            // a report in transit: one disc (handset, radio, eye), a ring to the landing, a dotted line to the call box
            int ri = 0;
            foreach (var r in m.Reports)
            {
                if (!Proj(r.World, out var p)) continue;
                if (reportMarks.Count <= ri) { var k = NewMark(reportMarks, "s-disc_dark_24", 40, 40); k.Plate.style.left = 8; k.Plate.style.top = 8; k.Plate.style.width = 24; k.Plate.style.height = 24; k.Glyph.style.left = 4; k.Glyph.style.top = 4; Ring(k, 14, 20, 20, 15, 3f); for (int j = 0; j < 24; j++) k.Dots.Add(Dot(marks, 0, 0, 4, Cream)); }
                var u = reportMarks[ri++]; Show(u.Root, true); Place(u.Root, p, 40, 40);
                Icon(u.Glyph, r.Icon, Cream); Fill(u, r.Frac, r.LandsS >= 0);
                int n = 0;
                if (r.To.HasValue)
                {
                    var from = r.World + Vector3.down * 2.25f; var to = r.To.Value; float len = Vector3.Distance(from, to); int steps = Mathf.Min(u.Dots.Count, Mathf.Max(1, Mathf.FloorToInt(len / 1.2f)));
                    for (int j = 1; j <= steps; j++) if (Proj(Vector3.Lerp(from, to, j / (float)steps), out var q)) { Show(u.Dots[n], true); u.Dots[n].style.left = q.x - 2; u.Dots[n].style.top = q.y - 2; n++; }
                }
                for (int j = n; j < u.Dots.Count; j++) Show(u.Dots[j], false);
            }
            for (int i = ri; i < reportMarks.Count; i++) { Show(reportMarks[i].Root, false); foreach (var d in reportMarks[i].Dots) Show(d, false); }
            // the response's units: robots the guard shield, officers the police chevron, merged with a count
            int ui = 0;
            foreach (var b in m.Units)
            {
                if (!Proj(b.World, out var p)) continue;
                if (unitMarks.Count <= ui) { var k = new Mark { Root = E(marks, null, 0, 0, 52, 22) }; k.Plate = E(k.Root, null, 0, 0, 22, 22); var tp = E(k.Root, "s-plate_light", 22, 3, float.NaN, 16); k.Text = L(tp, "keycap"); k.Text.style.paddingLeft = 4; k.Text.style.paddingRight = 4; k.Second = tp; unitMarks.Add(k); }
                var u = unitMarks[ui++]; Show(u.Root, true); u.Root.style.left = p.x - 11; u.Root.style.top = p.y - 11;
                Bg(u.Plate, b.Robot ? "badges/signal_guard" : "badges/signal_police"); Show(u.Second, b.Count > 1); Text(u.Text, $"x{b.Count}");
            }
            for (int i = ui; i < unitMarks.Count; i++) Show(unitMarks[i].Root, false);
            // arrival chips at the screen's edge
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) || size.x <= 0 ? 1920 : size.x, H = float.IsNaN(size.y) || size.y <= 0 ? 1080 : size.y;
            int ei = 0;
            foreach (var a in m.Arrivals)
            {
                if (!Proj(a.World, out var p)) continue;
                if (edgeMarks.Count <= ei) { var k = new Mark { Root = E(marks, "s-plate_dark", 0, 0, float.NaN, 26) }; k.Root.style.flexDirection = FlexDirection.Row; k.Root.style.alignItems = Align.Center; k.Root.style.paddingLeft = 8; k.Root.style.paddingRight = 10; k.Glyph = E(k.Root, null, w: 15, h: 15); Icon(k.Glyph, "units", Cream); k.Text = L(k.Root, "verb-name"); k.Text.style.marginLeft = 6; edgeMarks.Add(k); }
                var u = edgeMarks[ei++]; Show(u.Root, true); Text(u.Text, a.Text);
                u.Root.style.left = Mathf.Clamp(p.x - 50, 130, W - 160); /* clear of the roster column */ u.Root.style.top = Mathf.Clamp(p.y - 13, 70, H - 120);
            }
            for (int i = ei; i < edgeMarks.Count; i++) Show(edgeMarks[i].Root, false);
            // the mast heads' number plates
            int hi = 0;
            foreach (var w in m.MastHeads)
            {
                if (!Proj(w, out var p)) continue;
                if (headMarks.Count <= hi) { var k = new Mark { Root = E(marks, "s-plate_light", 0, 0, 18, 18) }; k.Text = L(k.Root, "numplate"); k.Text.style.flexGrow = 1; k.Text.style.unityTextAlign = TextAnchor.MiddleCenter; headMarks.Add(k); }
                var u = headMarks[hi]; Show(u.Root, true); Place(u.Root, p, 18, 18); Text(u.Text, (hi + 1).ToString()); hi++;
            }
            for (int i = hi; i < headMarks.Count; i++) Show(headMarks[i].Root, false);
            // the response pill and its list, under Intel
            bool resp = m.Response.Length > 0; Show(respPill, resp); Show(respList, resp && m.ResponseOpen);
            if (resp)
            {
                float top = intel.resolvedStyle.top + (float.IsNaN(intel.resolvedStyle.height) ? 34 : intel.resolvedStyle.height) + 6;
                respPill.style.top = top; respList.style.top = top + 40; Text(respLine, m.Response);
                for (int i = 0; i < respRows.Count; i++) { var (r, ic, lb, tx) = respRows[i]; bool has = i < m.ResponseRows.Count; Show(r, has); if (has) { Icon(ic, m.ResponseRows[i].icon, TurqLight); Text(lb, m.ResponseRows[i].label); Text(tx, m.ResponseRows[i].text); } }
            }
        }
    }
}
