using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 101 round 3 (Glyph, briefs/101/glyph/r3; Sky: "yes"): the street kit's last pieces, in the kit built for steps 4 to 6 (HudToolkit.Street,
    /// Kit/street.uss), so the old plates, chrome and screws can go. Glyph's five rules: words in the street are street labels; what you click is a round tab, or a
    /// capsule if it has a word; time that runs is the tab's own cream ring (it drains for a window and fills for someone's attention, orange on ink in its last
    /// 10 s, as the seal); every face wears the built cream ring; the bottom right holds only the news and the Book.
    ///  · the round tab: the built pause button's form: the book's body, a 2 px cream ring, the pictogram in cream at half its size, a soft shadow 2 px down;
    ///  · the timed tab: the tab whose ring is painted as a clock (0 at the top, clockwise), its time or stage word beside it as a street label;
    ///  · the capsule: the news card's fix chip: the book's body, a 1.4 px cream inline (turquoise-light on hover), a face and/or a cream pictogram, Jost Bold
    ///    12.5 in the page's cream; a shut way struck through;
    ///  · the face: a cream ring over a soft shadow, the outcome's kit badge at its lower right on a cream disc;
    ///  · the stem: the bubbles' fine cream line on an ink keyline, its foot a cream dot in an ink ring.
    /// Every colour is the street kit's or the book's (HudToolkit.Book): nothing new. Plain elements and labels; the timed ring repaints only when it moves.</summary>
    public sealed partial class HudToolkit
    {
        // ---------------------------------------------------------------- rule 2: a round tab

        /// <summary>A round tab `d` px: its shadow, its body (the child "body", whose border is the cream ring) and its pictogram (the child "g") at `k` of `d`.
        /// `pick`: it takes the pointer, and its ring lights turquoise-light while hovered.</summary>
        VisualElement RoundTab(VisualElement parent, float d, string icon, bool pick = false, float k = 0.52f)
        {
            var t = E(parent, null, w: d, h: d, pick: pick); t.style.flexShrink = 0; t.style.alignItems = Align.Center; t.style.justifyContent = Justify.Center; t.style.overflow = Overflow.Visible;
            var sh = Disc(t, d, "e-shadow"); Pin(sh, new Vector2(0f, 2f)); sh.style.opacity = 0.6f;
            var body = Disc(t, d, "e-body"); Pin(body, Vector2.zero); body.AddToClassList("e-inline"); BorderW(body, 2f); body.name = "body";
            var g = E(t, null, w: d * k, h: d * k); g.name = "g"; g.style.flexShrink = 0; if (!string.IsNullOrEmpty(icon)) Icon(g, icon, Cream);
            if (pick) { t.RegisterCallback<PointerEnterEvent>(_ => RingColour(body, TurqLight)); t.RegisterCallback<PointerLeaveEvent>(_ => RingColour(body, null)); }
            return t;
        }

        /// <summary>A round tab's pictogram (its "g"), in `tint` (cream unless a signal says otherwise).</summary>
        void TabIcon(VisualElement tab, string icon, Color? tint = null) => Icon(tab.Q("g"), icon, tint ?? Cream);

        /// <summary>Glyph's edge tab (rule 2): the ink chevron 3 px off a round tab, pointing off screen. `dir` in panel space (y down); the tab is `d` px.</summary>
        static void EdgeChevron(Painter2D p, Vector2 tabCentre, float d, Vector2 dir)
        {
            if (dir.sqrMagnitude < 1e-4f) return; dir.Normalize(); var side = new Vector2(-dir.y, dir.x);
            var b = tabCentre + dir * (d / 2f + 3f); var tip = b + dir * 10f;
            p.fillColor = BkInk; p.BeginPath(); p.MoveTo(b + side * 8f); p.LineTo(tip); p.LineTo(b - side * 8f); p.ClosePath(); p.Fill();
        }

        /// <summary>Glyph's edge tab (rule 2): something off screen that isn't a face (a reroute, the rival car, a lock): a round tab with its pictogram or badge at the
        /// screen's edge, the ink chevron pointing off toward it.</summary>
        sealed class EdgeTabEl { public VisualElement Root; public Paint Chev; public float Angle = float.NaN; }
        EdgeTabEl NewEdgeTab(string icon, string badge = null)
        {
            var e = new EdgeTabEl(); var r = e.Root = E(marks, null, 0, 0, 26, 26); r.style.overflow = Overflow.Visible;
            var t = RoundTab(r, 26f, badge == null ? icon : ""); t.style.position = Position.Absolute; t.style.left = 0; t.style.top = 0;
            if (badge != null) Bg(E(t, null, 4.7f, 4.7f, 16.6f, 16.6f), badge, Color.white); // the badge at 0.64 of the tab
            var ee = e; e.Chev = P(r, -20, -20, 66, 66, (p, rc) => { if (!float.IsNaN(ee.Angle)) EdgeChevron(p, new Vector2(33f, 33f), 26f, new Vector2(Mathf.Cos(ee.Angle), Mathf.Sin(ee.Angle))); });
            return e;
        }
        /// <summary>An edge tab centred at `c` (panel px), its chevron at `angle` (radians, screen space, y down).</summary>
        static void EdgeTabAt(EdgeTabEl e, Vector2 c, float angle)
        {
            e.Root.style.left = c.x - 13f; e.Root.style.top = c.y - 13f;
            if (float.IsNaN(e.Angle) || Mathf.Abs(angle - e.Angle) > 0.01f) { e.Angle = angle; e.Chev.MarkDirtyRepaint(); }
        }

        // ---------------------------------------------------------------- rule 3: the timed tab

        /// <summary>A timed tab and what stands beside it: the time or the stage word (a street label), or the holder's face (a loop the Tech holds).</summary>
        sealed class TimedEl { public VisualElement Root, Tab, Glyph, Holder; public Paint Ring; public KeyText Word; public float Frac = -1f; public bool Hot, KeyHot, Styled; public int HolderAgent = -1; }

        /// <summary>A timed tab `d` px with its word (`size` px, `gap` px off the tab), in `parent` (absolute: place its Root by left and top).</summary>
        TimedEl NewTimed(VisualElement parent, float d, float size, float gap)
        {
            var k = new TimedEl();
            var r = k.Root = StackRow(parent, d); r.style.position = Position.Absolute; r.style.justifyContent = Justify.FlexStart;
            var t = k.Tab = E(r, null, w: d, h: d); t.style.flexShrink = 0; t.style.alignItems = Align.Center; t.style.justifyContent = Justify.Center; t.style.overflow = Overflow.Visible;
            var sh = Disc(t, d, "e-shadow"); Pin(sh, new Vector2(0f, 2f)); sh.style.opacity = 0.6f;
            var body = Disc(t, d, "e-body"); Pin(body, Vector2.zero);
            k.Glyph = E(t, null, w: d * 0.56f, h: d * 0.56f); k.Glyph.style.flexShrink = 0;
            var kk = k; k.Ring = P(t, 0, 0, d, d, (p, rc) => TimedRing(p, rc, kk.Frac, kk.Hot));
            k.Word = KText(r, size); k.Word.Root.style.marginLeft = gap;
            k.Holder = SFace(r, 20f, 2f); k.Holder.style.marginLeft = 4f; Show(k.Holder, false);
            return k;
        }

        /// <summary>The tab's own ring as a clock: the whole ring in the book's ink, then `frac` of it from the top, clockwise, in the page's cream (orange while hot).</summary>
        static void TimedRing(Painter2D p, Rect r, float frac, bool hot)
        {
            var c = r.center; float R = Mathf.Min(r.width, r.height) / 2f - 1.3f;
            p.lineCap = LineCap.Butt; p.strokeColor = BkInk; p.lineWidth = 2.6f; p.BeginPath(); p.Arc(c, R, Deg(0), Deg(360)); p.Stroke();
            if (frac <= 0.001f) return;
            p.strokeColor = hot ? BkOrange : StPage; p.BeginPath(); p.Arc(c, R, Deg(-90), Deg(-90 + 360f * Mathf.Min(1f, frac))); p.Stroke();
        }

        /// <summary>A timed tab now: its pictogram, its ring's share and heat, and beside it the word (orange on an ink keyline while hot) or the holder's face.</summary>
        void SetTimed(TimedEl k, string icon, float frac, bool hot, string word, int holder = -1)
        {
            Icon(k.Glyph, icon, Cream);
            if (Mathf.Abs(frac - k.Frac) > 0.002f || hot != k.Hot) { k.Frac = frac; k.Hot = hot; k.Ring.MarkDirtyRepaint(); }
            bool face = holder >= 0; Show(k.Holder, face); Show(k.Word.Root, !face && word.Length > 0);
            if (face) { if (k.HolderAgent != holder) { k.HolderAgent = holder; CameoOf(k.Holder, holder); } return; }
            k.Word.Set(word);
            if (hot != k.KeyHot || !k.Styled) { k.KeyHot = hot; k.Styled = true; k.Word.Look(hot ? "k-hot" : "k-ink", hot ? "k-key-ink" : "k-key"); KeyWidth(k.Word, hot ? KeyHotW : KeyW); }
        }

        /// <summary>A street label's keyline width (Glyph: 2.6 px for ink type, 2.2 for orange on ink): its key copies moved out by `kw`, the shadow under them.</summary>
        static void KeyWidth(KeyText t, float kw)
        {
            int n = 0;
            foreach (var c in t.Copies)
            {
                if (c.ClassListContains("k-shadow")) { c.style.translate = new Translate(0f, kw + KeyDrop); continue; }
                if (n < KeyDirs.Length) { var d = KeyDirs[n++] * kw; c.style.translate = new Translate(d.x, d.y); }
            }
        }

        // ---------------------------------------------------------------- rule 2: the capsule

        /// <summary>A capsule: a way you can click that has a word. Root (the body and its inline), a face, a pictogram, the word, the strike.</summary>
        sealed class Capsule { public VisualElement Root, Inline, Face, Icon, Strike; public Label Word; public bool Live; }

        /// <summary>A capsule `h` px tall (24, or 22 on the slip) in `parent`'s flow; `face`: room for the doer's face (18 px in a 1.2 px cream ring). It takes the
        /// pointer when `click` is given; hovered, its inline lights turquoise-light.</summary>
        Capsule NewCapsule(VisualElement parent, float h = 24f, bool face = false, Action click = null)
        {
            var c = new Capsule();
            var r = c.Root = E(parent, null, h: h, pick: click != null); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.flexShrink = 0; r.style.overflow = Overflow.Visible;
            r.style.paddingLeft = face ? 6f : 9f; r.style.paddingRight = 10f;
            VisualElement Layer(string cls, float inset, float dy)
            {
                var e = E(r, cls); e.style.position = Position.Absolute; e.style.left = inset; e.style.right = inset; e.style.top = inset + dy; e.style.bottom = inset - dy; Round(e, h / 2f - inset); return e;
            }
            var sh = Layer("e-shadow", 0f, 1.5f); sh.style.opacity = 0.6f; Layer("e-body", 0f, 0f); c.Inline = Layer("e-inline", 0.7f, 0f); BorderW(c.Inline, 1.4f);
            if (face) { c.Face = E(r, null, w: 18, h: 18); c.Face.style.flexShrink = 0; c.Face.style.marginLeft = -2f; Round(c.Face, 9f); BorderW(c.Face, 1.2f); c.Face.AddToClassList("e-inline"); c.Face.style.backgroundColor = BkPale; var f = E(c.Face, null, 0, 0, 15.6f, 15.6f); f.name = "face"; Round(f, 7.8f); }
            c.Icon = E(r, null, w: 13, h: 13); c.Icon.style.flexShrink = 0; if (face) c.Icon.style.marginLeft = 3f;
            c.Word = KLabel(r, "k-bold", "k-page", 12.5f); c.Word.style.marginLeft = 5f;
            c.Strike = E(r, "e-page"); c.Strike.style.position = Position.Absolute; c.Strike.style.left = 6; c.Strike.style.right = 6; c.Strike.style.top = h / 2f; c.Strike.style.height = 1.6f; Show(c.Strike, false);
            if (click != null)
            {
                var cc = c; r.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { click(); e.StopPropagation(); } });
                r.RegisterCallback<PointerEnterEvent>(_ => RingColour(cc.Inline, TurqLight)); r.RegisterCallback<PointerLeaveEvent>(_ => RingColour(cc.Inline, cc.Live ? TurqLight : (Color?)null));
            }
            return c;
        }

        /// <summary>A capsule now: its pictogram, its word, its doer's face (-1 none), struck when the way is shut; `live` keeps the inline turquoise (an agent's fix chip).</summary>
        void SetCapsule(Capsule c, string icon, string word, int agent = -1, bool shut = false, bool live = false)
        {
            bool ic = !string.IsNullOrEmpty(icon); if (ic) Icon(c.Icon, icon, Cream); else Show(c.Icon, false);
            Text(c.Word, word ?? ""); Show(c.Word, !string.IsNullOrEmpty(word)); c.Word.style.marginLeft = ic || c.Face != null ? 5f : 0f;
            c.Root.style.paddingRight = string.IsNullOrEmpty(word) ? 8f : 10f;
            if (c.Face != null) { Show(c.Face, agent >= 0); if (agent >= 0) CameoOf(c.Face, agent); }
            Show(c.Strike, shut); c.Root.style.opacity = shut ? 0.75f : 1f;
            c.Live = live; RingColour(c.Inline, live ? TurqLight : (Color?)null);
        }

        // ---------------------------------------------------------------- rule 4: the face

        /// <summary>The built face, `d` px: a soft shadow 2 px down, the photo clipped round on the book's pale (the child "face": CameoOf and Bg fill it), the cream
        /// ring `rw` over its edge, and the badge slot (the child "badge": the outcome's kit badge on a cream disc at the lower right, hidden until set).</summary>
        VisualElement SFace(VisualElement parent, float d, float rw = 2.5f)
        {
            var w = E(parent, null, w: d, h: d); w.style.flexShrink = 0; w.style.overflow = Overflow.Visible;
            var sh = Disc(w, d, "e-shadow"); Pin(sh, new Vector2(0f, 2f)); sh.style.opacity = 0.6f;
            var f = E(w, null, 0, 0, d, d); f.name = "face"; Round(f, d / 2f); f.style.backgroundColor = BkPale;
            var ring = E(w, "e-inline", 0, 0, d, d); Round(ring, d / 2f); BorderW(ring, rw);
            float bd = Mathf.Round(d * 0.42f); var bdisc = E(w, "e-page", d - bd + 2f - 1.2f, d - bd + 2f - 1.2f, bd + 2.4f, bd + 2.4f); bdisc.name = "badge"; Round(bdisc, bd / 2f + 1.2f);
            var b = E(bdisc, null, 1.2f, 1.2f, bd, bd); b.name = "badge-art"; Show(bdisc, false);
            return w;
        }

        /// <summary>A face's badge (badges/mark_ok, mark_half, mark_no ...), "" for none.</summary>
        void FaceBadge(VisualElement face, string badge)
        {
            var b = face.Q("badge"); if (b == null) return; bool on = !string.IsNullOrEmpty(badge); Show(b, on); if (on) Bg(b.Q("badge-art"), badge, Color.white);
        }

        /// <summary>A face's photo: an agent's cameo (k >= 0), else the cast's painted face or the role's badge (FaceOf).</summary>
        void FaceArt(VisualElement face, int agent, string role = null)
        {
            if (agent >= 0) CameoOf(face, agent); else if (!string.IsNullOrEmpty(role)) Bg(face.Q("face"), FaceOf(role), Color.white);
        }

        // ---------------------------------------------------------------- the stem

        /// <summary>The fine cream stem from a piece down to what it belongs to (the bubbles' stem, on an ink keyline), its foot a cream dot in an ink ring; into the
        /// street's paints (behind the HUD's pieces), at `alpha`.</summary>
        void Stem(Vector2 top, Vector2 foot, float alpha = 1f, bool dot = true)
        {
            if (alpha <= 0.02f || (foot - top).sqrMagnitude < 4f) return;
            paints.Add(pp =>
            {
                pp.lineCap = LineCap.Round; pp.strokeColor = new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.51f * alpha); pp.lineWidth = 4f; pp.BeginPath(); pp.MoveTo(top); pp.LineTo(foot); pp.Stroke();
                pp.strokeColor = new Color(StPage.r, StPage.g, StPage.b, alpha); pp.lineWidth = 2.2f; pp.BeginPath(); pp.MoveTo(top); pp.LineTo(foot); pp.Stroke();
                if (!dot) return;
                pp.fillColor = new Color(BkInk.r, BkInk.g, BkInk.b, alpha); pp.BeginPath(); pp.Arc(foot, 3.8f, Deg(0), Deg(360)); pp.Fill();
                pp.fillColor = new Color(StPage.r, StPage.g, StPage.b, alpha); pp.BeginPath(); pp.Arc(foot, 2.6f, Deg(0), Deg(360)); pp.Fill();
            });
        }

        // ---------------------------------------------------------------- rule 1: a street label with its pictogram

        /// <summary>A street label row in `parent`'s flow: a keyed pictogram (`isz` px) and its words (`size` px, Jost Bold) `gap` px after it.</summary>
        (VisualElement row, KeyIcon icon, KeyText words) KeyRow(VisualElement parent, float isz, float size, float gap = 6f, string face = "k-bold")
        {
            var r = StackRow(parent, Mathf.Max(isz, size + 6f)); r.style.justifyContent = Justify.FlexStart;
            var ic = KIcon(r, "", isz, kw: 2f); var w = KText(r, size, face); w.Root.style.marginLeft = gap;
            return (r, ic, w);
        }
    }
}
