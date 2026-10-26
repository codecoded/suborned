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
        sealed class Wedge { public VisualElement Icon, Pill, Key, Cameos, Draw, Loud, Obj, Held, Strike; public Label Word, KeyText; public string ShutLine = ""; public VisualElement Fields; public string FieldsKey = "", HeldKey = null; public readonly System.Collections.Generic.List<string> HeldIds = new(); }
        VisualElement ringRoot; Paint ringPaint; readonly List<Wedge> wedges = new(); HandoverHost.RingState ringFor; VisualElement cursorMark; Paint cursorDots;

        void BuildRing()
        {
            ringRoot = E(root, null, 0, 0, 0, 0); ringRoot.style.overflow = Overflow.Visible;
            ringPaint = P(ringRoot, -HandoverHost.RingOuter - 8, -HandoverHost.RingOuter - 8, 2 * HandoverHost.RingOuter + 16, 2 * HandoverHost.RingOuter + 16, RingPaint);
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
            w.Obj = Cameo(w.Pill, 12f, 2f); w.Obj.style.marginLeft = 6; w.Obj.style.marginRight = 2; // brief 057 §2: the thing the way moves, 18 px in the chrome ring
            w.Held = E(w.Pill); w.Held.style.flexDirection = FlexDirection.Row; w.Held.style.alignItems = Align.Center; w.Held.style.marginLeft = 8; // brief 057 §4.2: a shut Go's lock and holders
            w.Fields = E(w.Pill); w.Fields.style.flexDirection = FlexDirection.Row; w.Fields.style.alignItems = Align.Center;
            w.Loud = E(w.Pill, null, w: 14, h: 14); w.Loud.style.marginLeft = 8;
            w.Key = E(w.Pill, "s-plate_light", w: float.NaN, h: 18); w.Key.style.marginLeft = 8; w.Key.style.minWidth = 22; w.KeyText = L(w.Key, "keycap"); w.KeyText.style.paddingLeft = 4; w.KeyText.style.paddingRight = 4; w.KeyText.style.unityTextAlign = TextAnchor.MiddleCenter;
            w.Strike = E(w.Pill, null, 0, 0, 0, 1.5f); w.Strike.style.position = Position.Absolute; w.Strike.style.backgroundColor = Dim; w.Strike.pickingMode = PickingMode.Ignore; Show(w.Strike, false); // Glyph N3: the shut way's word struck (a sibling over the word: a child of the Label broke its measured width and the lock sat on the word, 0.2.127)
            manifest.Add((w.Pill, "lock", () => w.ShutLine)); // a shut wedge's plate ("Listen · Already heard") is the lock pill's twin (Glyph budget §2); "" when the wedge is open: not listed
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
                if (s.Shut) { var a = c + Dir(i * w - w * 0.3f) * ((ro + ri) / 2f); var b = c + Dir(i * w + w * 0.3f) * ((ro + ri) / 2f); p.strokeColor = Cream; p.lineWidth = 1.6f; p.BeginPath(); p.MoveTo(a); p.LineTo(b); p.Stroke(); } // brief 057 §4.2: shut, struck (no colour)
                if (s.More) Dots(p, c + Dir(i * w) * ((ro + ri) / 2f), 3.2f, 2.4f, Cream);
                else if (s.Row?.I?.Order is Order so && rs.Plan?.T != null && host.Fresh(rs.Plan.T, so)) Sparkle(p, c + Dir(i * w + w * 0.28f) * (ro - 9f), 6f, Cream); // brief 043: the act that just opened
            }
            p.fillColor = new Color(Plate.r, Plate.g, Plate.b, 0.6f); p.BeginPath(); p.Arc(c, ri - 5f, Deg(0), Deg(360)); p.Fill(); // the middle: release here to keep it open
            // Glyph's 0.2.94 review #12: the wheel's chrome rim (the cameo's ring: 2 px chrome on a 1 px ink-deep line), a notch outside the wedges
            p.lineCap = LineCap.Butt; p.strokeColor = InkDeep; p.lineWidth = 4f; p.BeginPath(); p.Arc(c, ro + 3f, Deg(0), Deg(360)); p.Stroke();
            p.strokeColor = Chrome; p.lineWidth = 2f; p.BeginPath(); p.Arc(c, ro + 3f, Deg(0), Deg(360)); p.Stroke();
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
                Show(w.Obj, s.Obj.Length > 0);
                if (s.Obj.Length > 0) { var fc = w.Obj.Q("face"); if (s.Obj.StartsWith("icon:")) { fc.style.backgroundImage = StyleKeyword.None; if (fc.childCount == 0) E(fc, null, 1.5f, 1.5f, 9, 9); Icon(fc[0], s.Obj[5..], Cream); } else { if (fc.childCount > 0) Show(fc[0], false); Bg(fc, s.Obj, Color.white); } }
                { var sk = w.Obj.Q("strike"); if (sk == null && s.ObjStruck) { sk = E(w.Obj, null, -2f, 6f, 20f, 2f); sk.name = "strike"; sk.style.backgroundColor = Cream; sk.style.rotate = new Rotate(-45f); sk.pickingMode = PickingMode.Ignore; } if (sk != null) Show(sk, s.ObjStruck && s.Obj.Length > 0); } // brief 067 C5b (Glyph): Go before Vale: Vale's cameo struck (a 2 px cream diagonal)
                Show(w.Held, s.Shut); Show(w.Fields, !s.Shut || s.ShutText.Length > 0); // a shut Loop (one loop at a time) keeps one field: when the running loop ends
                w.Pill.style.opacity = 1f; w.Icon.style.opacity = s.Shut ? 0.45f : 1f; // Glyph N3 (0.2.125): a shut wedge's plate is solid plate_dark like the others (it was 75 %: the reason read at about half contrast over the chip)
                w.Word.style.color = s.Shut ? Dim : Cream; // N3: the way word struck in dim #b0cdc7; the reason stays cream
                Show(w.Strike, s.Shut); if (s.Shut) { var wl = w.Word.layout; if (!float.IsNaN(wl.width) && wl.width > 0) { w.Strike.style.left = wl.x; w.Strike.style.width = wl.width; w.Strike.style.top = wl.y + wl.height * 0.54f; } }
                w.ShutLine = s.Shut ? s.Word + (s.ShutText.Length > 0 ? " · " + s.ShutText : "") : "";
                if (s.Shut)
                {   // the lock, then one police badge a holder (brief 057 §4.2): the same holders the world marks
                    if (w.HeldKey != s.Holders) { w.HeldKey = s.Holders; w.HeldIds.Clear(); foreach (var hx in s.Holders.Split(",")) if (hx.Trim().Length > 0) w.HeldIds.Add(hx.Trim()); } var ids = w.HeldIds; /* Redline 064 #6: split once a ring, not a frame */
                    while (w.Held.childCount < 1 + Mathf.Max(1, ids.Count)) { var c0 = w.Held.childCount == 0 ? E(w.Held, null, w: 14, h: 14) : Cameo(w.Held, 12f, 2f); c0.style.marginRight = 2; }
                    Icon(w.Held[0], "lock", Cream);
                    for (int k = 1; k < w.Held.childCount; k++) { bool has = k - 1 < ids.Count; Show(w.Held[k], has); if (has) Bg(w.Held[k].Q("face"), "badges/" + (PlayText.RoleOfId(ids[k - 1]) == "rival" || ids[k - 1] == "car:rivals" ? "signal_rival" : "signal_police"), Color.white); } // engine 0.33.0: the rival car holds while its riders are still to come
                }
                var fs = s.Shut ? new List<HudModel.PField>() : HudModel.PriceLine(host, s.Row.I);
                if (s.Shut && s.ShutText.Length > 0) fs.Add(new HudModel.PField { Kind = "time", Icon = s.ShutIcon, Text = s.ShutText }); // brief 067 E: a shut way's short lock (no icon: the lock leads the pill)
                else if (s.Shut && host.RivalsComingS() is int rcs && rcs >= 0 && s.Holders.Contains("car:rivals")) fs.Add(new HudModel.PField { Kind = "time", Icon = "p_time", Text = $"{rcs} s" }); // engine 0.33.0 (Motive M059-S1.7): the countdown to the first rider's step-out
                if (s.Draws) fs.Insert(0, new HudModel.PField { Kind = "time", Icon = "", Text = $"draws {host.DrawS:0.0#} s" });
                Fields(w.Fields, fs, Cream, ref w.FieldsKey, false, small: false); // Glyph N3 (0.2.125): the reason in cream (the struck way word is the dim one)
                Show(w.Loud, s.Violent); if (s.Violent) Icon(w.Loud, "bang", Cream);
                Show(w.Key, false); // Sky: no key legends (Aster 2026-10-22: the ⇧1 / ⇧2 caps on the plates); the wedge is the click
                w.Pill.style.unityBackgroundImageTintColor = lit ? new Color(0.75f, 1f, 1f) : Color.white;
                float pw = w.Pill.resolvedStyle.width; if (float.IsNaN(pw) || pw <= 0) pw = 180;
                var at = dir * (ro + 10f);
                bool right = dir.x > 0.2f, left = dir.x < -0.2f;
                float x = right ? at.x : left ? at.x - pw : at.x - pw / 2f, y = at.y - 15f + (Mathf.Abs(dir.x) <= 0.2f ? (dir.y < 0 ? -14f : 14f) : 0f);
                w.Pill.style.left = x; w.Pill.style.top = y;
                if (s.ObjStruck && s.Row?.I?.Order?.Do == "go") { altAt = new Vector2(x + pw / 2f, y + 30f + 4f); altTop = y - 4f; altLine = s.Row.I.Does; } // Fable 067 less.go.noVale: Go before Vale's Alt line
            }
            RingAltFrame(host.Peeking ? altLine : "", altAt); altLine = "";
            ringFor = rs; ringPaint.MarkDirtyRepaint();
            RingFocusFrame();
        }

        VisualElement ringAlt; Label ringAltText; string altLine = ""; Vector2 altAt; float altTop;
        /// <summary>Fable's less.go.noVale (brief 067, Glyph C5b): with Alt, the line of the Go wedge before Vale is aboard (the ring's last, never
        /// lit way with the struck Vale cameo) on a plate_dark under its pill; "" hides it. Glyph N5 (0.2.125): the plate sizes to its text on one
        /// line (no wrap), under the wedge's plate, clear of the feed card and the ring's rim; it flips above the wedge's plate when it meets them.</summary>
        void RingAltFrame(string line, Vector2 under)
        {
            if (ringAlt == null) { if (string.IsNullOrEmpty(line)) return; ringAlt = PlateDark(ringRoot, 26f, 0.3f); ringAlt.style.position = Position.Absolute; ringAlt.style.paddingLeft = 12; ringAlt.style.paddingRight = 12; ringAltText = Words(ringAlt, 12.5f); ringAltText.style.whiteSpace = WhiteSpace.NoWrap; manifest.Add((ringAlt, "altline", () => ringAltText.text)); }
            bool on = !string.IsNullOrEmpty(line); Show(ringAlt, on); if (!on) return;
            Text(ringAltText, line); float aw = float.IsNaN(ringAlt.resolvedStyle.width) ? 220f : ringAlt.resolvedStyle.width; const float ah = 26f;
            var c = new Vector2(ringRoot.style.left.value.value, ringRoot.style.top.value.value); float W = root.layout.width > 0 ? root.layout.width : 1920f, rim = HandoverHost.RingOuter + 8f;
            var feed = FeedRects();
            bool Clear(Rect r) { foreach (var f in feed) if (Grow(f, 8f).Overlaps(r)) return false; var n = new Vector2(Mathf.Clamp(c.x, r.xMin, r.xMax), Mathf.Clamp(c.y, r.yMin, r.yMax)); return (n - c).sqrMagnitude >= rim * rim; }
            float lx = Mathf.Clamp(c.x + under.x - aw / 2f, 14f, Mathf.Max(14f, W - aw - 14f));
            Rect below = new(lx, c.y + under.y, aw, ah), above = new(lx, c.y + altTop - ah, aw, ah);
            var at = Clear(below) ? below : Clear(above) ? above : below;
            ringAlt.style.left = at.x - c.x; ringAlt.style.top = at.y - c.y; ringAlt.BringToFront();
        }
        /// <summary>The feed's cards and the story beats on screen (panel space): the Alt line keeps off them.</summary>
        List<Rect> FeedRects()
        {
            var l = new List<Rect>();
            foreach (var (b, _, _) in beatEls) if (b != null && Visible(b) && b.worldBound.width > 1) l.Add(b.worldBound);
            foreach (var cd in cards) if (cd?.Root != null && Visible(cd.Root) && cd.Root.worldBound.width > 1) l.Add(cd.Root.worldBound);
            foreach (var e in new[] { historyKey2, history, beatsBox }) if (e != null && Visible(e) && e.worldBound.width > 1) l.Add(e.worldBound); // after 0.2.126: the history drawer is the feed card the Alt line sat on
            return l;
        }
        /// <summary>The proof reads it: the ring's Alt line now ("" hidden).</summary>
        public static string RingAltShown => live?.ringAlt != null && live.ringAlt.resolvedStyle.display != DisplayStyle.None ? live.ringAltText.text : "";

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
            // a camera's pole view stands 1.6 m up its pole; the head (HostViews.CamHeadUp) is 3.25 m up on the stand-in, 4.78 m on the kit pole
            Vector2 q = default; bool on = t != null && t.gameObject.activeInHierarchy && Proj(t.position + Vector3.up * (camera ? host.Views.CamHeadUp - 1.6f : 1.2f), out q);
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
            var g = cursorMark.Q("g"); Show(g, kind is "aim" or "nowalk"); if (kind == "aim") Icon(g, "fire", Cream); else if (kind == "nowalk") Icon(g, "s_moving", Cream);
            var nw = cursorMark.Q("nowalk"); if (nw == null) { nw = P(cursorMark, 0, 0, 22, 22, (p, r) => { p.strokeColor = Cream; p.lineWidth = 2f; p.lineCap = LineCap.Round; p.BeginPath(); p.MoveTo(new Vector2(4, 18)); p.LineTo(new Vector2(18, 4)); p.Stroke(); }); nw.name = "nowalk"; } Show(nw, kind == "nowalk"); // struck footprints: no walk there
            Show(cursorDots, kind == "ring");
            cursorMark.style.backgroundColor = new Color(Plate.r, Plate.g, Plate.b, 0.85f); Round(cursorMark, 11); var cr = cursorMark.Q("chrome"); if (cr == null) { cr = P(cursorMark, 0, 0, 22, 22, (pp, r) => { pp.strokeColor = InkDeep; pp.lineWidth = 1f; pp.BeginPath(); pp.Arc(r.center, 10.5f, Deg(0), Deg(360)); pp.Stroke(); pp.strokeColor = Chrome; pp.lineWidth = 2f; pp.BeginPath(); pp.Arc(r.center, 9f, Deg(0), Deg(360)); pp.Stroke(); }); cr.name = "chrome"; } Show(cr, kind == "nowalk"); /* Glyph's 0.2.112 check: the no-walk disc in its chrome ring (2 px #cbc6c3, 1 px ink-deep outside), as a kind disc; painted, so the glyph stays centred */
        }
    }
}
