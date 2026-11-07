using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 088, the book's light page (Glyph's L, briefs/085/glyph/book/look4; Sky: "Yeah light page is nice"): the real street stopped, one cream page with one big curve over it, the face
    /// overlapping its edge, round section signs at the right, the small clock chip. Laid out on the 1920 x 1080 grid (BookLayout) and scaled as one, the page anchored right. Every slot is drawn from the
    /// page's data by BookLayout's items; nothing here knows what a page is about. Built when the book opens and taken out of the tree when it shuts, so a shut book costs the HUD nothing.
    /// The orange (the café parasols, Glyph's sample) and the paper aren't in the kit's HUD colour rule: the look is Sky's pick, flagged to Aster in the brief's report.</summary>
    public sealed partial class HudToolkit
    {
        static readonly Color BkOrange = Hex(0xe8622c), BkBody = Hex(0x0c3434), BkDeep = Hex(0x1f6c67), BkPaper = new(0xf9 / 255f, 0xf7 / 255f, 0xee / 255f, 247f / 255f), BkTurq = Hex(0x60e0ea), BkPale = Hex(0x9bcfc2), BkInk = Hex(0x17302f), BkSoft = Hex(0x4e625f);
        const string BkInkHex = "#17302f", BkSoftHex = "#4e625f", BkCreamHex = "#f7f4ec", BkDimHex = "#b0cdc7";
        const float BkPad = 3000f; // the curve's and the line's layers reach this far past the grid, for any window shape

        VisualElement bk, bkPage, bkBody; Paint bkCurve, bkLine; Label bkClock; BookPage bkFor; string bkDrawnSignsFor = ""; float bkOpenedAt; readonly Dictionary<string, Font> bkFonts = new();

        /// <summary>The proof reads these: whether the page is in the tree, how many elements the panel holds, the signs drawn, the items the last page laid out.</summary>
        public static bool BookShown => live?.bk != null;
        public static int PanelElementCount { get { if (live?.root == null) return 0; int n = 0; void W(VisualElement e) { n++; foreach (var c in e.Children()) W(c); } W(live.root); return n; } }
        public static int BookSigns { get; private set; }
        public static BookLayout.Result BookLaidOut { get; private set; }

        /// <summary>The proof and the recording: the screen point (origin bottom left) of a page's element by name ("book-sign-job", "book-way", "book-other", "book-back"), the index'th of that name; null when none.</summary>
        public static Vector2? BookElementScreen(string name, int index = 0)
        {
            if (live?.bk == null || live.root?.panel == null) return null;
            var l = new List<VisualElement>(); void W(VisualElement e) { if (e.name == name) l.Add(e); foreach (var c in e.Children()) W(c); } W(live.bk); if (index >= l.Count) return null;
            var pn = live.root.panel; Vector2 a = RuntimePanelUtils.ScreenToPanel(pn, Vector2.zero), b = RuntimePanelUtils.ScreenToPanel(pn, new Vector2(100f, 100f)); if (Mathf.Abs(b.x - a.x) < 1e-3f) return null;
            var c0 = l[index].worldBound.center; return new Vector2((c0.x - a.x) * 100f / (b.x - a.x), Screen.height - (c0.y - a.y) * 100f / (b.y - a.y));
        }

        float BkTw(string s, float size, string font)
        {
            if (string.IsNullOrEmpty(s)) return 0f;
            if (!bkFonts.TryGetValue(font, out var f)) bkFonts[font] = f = Resources.Load<Font>("Kit/fonts/" + font);
            if (!f) return s.Length * size * .52f;
            int px = Mathf.Max(8, Mathf.RoundToInt(size)); f.RequestCharactersInTexture(s, px); float w = 0f;
            foreach (var c in s) if (f.GetCharacterInfo(c, out var ci, px)) w += ci.advance; else w += px * .5f;
            return w * size / px;
        }

        /// <summary>From ConvoHud's first line, each frame: the book up has the HUD (true); shut, one compare and out.</summary>
        bool BookHud()
        {
            if (!host.BookOpen) { if (bk != null) BookTake(); return false; }
            if (bk == null) BookBuild();
            if (bkFor != host.BookPageNow && host.BookPageNow != null) BookDraw(host.BookPageNow);
            DialHud(); // brief 098: the dial's dev panel while the book is the start (HudToolkit.Dial)
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) || size.x <= 0 ? BookLayout.Grid : size.x, H = float.IsNaN(size.y) || size.y <= 0 ? BookLayout.GridH : size.y, k = Mathf.Min(W / BookLayout.Grid, H / BookLayout.GridH);
            bkPage.style.left = W - BookLayout.Grid * k; bkPage.style.top = (H - BookLayout.GridH * k) / 2f; bkPage.style.scale = new Scale(new Vector3(k, k, 1f));
            float u = Mathf.Clamp01((Time.unscaledTime - bkOpenedAt) / 0.16f), e = 1f - (1f - u) * (1f - u); bkBody.style.opacity = e; bkBody.style.translate = new Translate(90f * (1f - e), 0f);
            if (bkClock != null && host.Game != null) Text(bkClock, HandoverHost.MissionClock(host.Game.Tick));
            bkLine.MarkDirtyRepaint();
            return true;
        }

        void BookBuild()
        {
            bk = new VisualElement { pickingMode = PickingMode.Ignore }; root.Add(bk); bk.style.position = Position.Absolute; bk.style.left = 0; bk.style.top = 0; bk.style.right = 0; bk.style.bottom = 0;
            foreach (var ch in root.Children()) if (ch != bk) { ch.style.visibility = Visibility.Hidden; ch.style.opacity = 0f; } // as the conversation does: nothing of the play HUD shows or takes a click
            bkPage = new VisualElement { pickingMode = PickingMode.Ignore }; bk.Add(bkPage); bkPage.style.position = Position.Absolute; bkPage.style.width = BookLayout.Grid; bkPage.style.height = BookLayout.GridH; bkPage.style.transformOrigin = new TransformOrigin(0, 0, 0);
            bkBody = new VisualElement { pickingMode = PickingMode.Ignore }; bkPage.Add(bkBody); bkBody.style.position = Position.Absolute; bkBody.style.left = 0; bkBody.style.top = 0; bkBody.style.width = BookLayout.Grid; bkBody.style.height = BookLayout.GridH;
            bkCurve = P(bkBody, -BkPad, -BkPad, BookLayout.Grid + 2 * BkPad, BookLayout.GridH + 2 * BkPad, BookCurvePaint); bkCurve.pickingMode = PickingMode.Position;
            bkLine = P(bkPage, -BkPad, -BkPad, BookLayout.Grid + 2 * BkPad, BookLayout.GridH + 2 * BkPad, BookLinePaint);
            bkOpenedAt = Time.unscaledTime; bkFor = null; bkDrawnSignsFor = "";
        }

        void BookTake()
        {
            bk.RemoveFromHierarchy(); bk = null; bkPage = null; bkBody = null; bkClock = null; bkFor = null; bkCurve = null; bkLine = null;
            foreach (var ch in root.Children()) { ch.style.visibility = StyleKeyword.Null; ch.style.opacity = StyleKeyword.Null; }
        }

        /// <summary>The page of light: an ellipse across the grid (Glyph's: left edge 740, centre 2220, y -900 to 1980), a soft turquoise halo outside it, cream inside. Painted once.</summary>
        void BookCurvePaint(Painter2D p, Rect r)
        {
            var c = new Vector2(2220f + BkPad, 540f + BkPad); const float rx = 1480f, ry = 1440f; const int N = 180;
            void Path() { p.BeginPath(); for (int i = 0; i <= N; i++) { float a = i * Mathf.PI * 2f / N; var q = c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry); if (i == 0) p.MoveTo(q); else p.LineTo(q); } p.ClosePath(); }
            p.lineJoin = LineJoin.Round; p.lineCap = LineCap.Butt;
            for (int i = 0; i < 9; i++) { p.strokeColor = new Color(BkTurq.r, BkTurq.g, BkTurq.b, .075f); p.lineWidth = 36f - i * 4f; Path(); p.Stroke(); }
            p.fillColor = new Color(BkPaper.r, BkPaper.g, BkPaper.b, 1f); Path(); p.Fill();
        }

        /// <summary>The thin line from the face to the person, their eye and ground ring, on the frozen street: drawn only where the person stands in view; moved each frame with the camera's glide.</summary>
        void BookLinePaint(Painter2D p, Rect r)
        {
            var page = host.BookPageNow; if (page == null || bkPage == null) return;
            string key = host.BookLookView.Length > 0 ? host.BookLookView : page.Line ? page.SubjectView : null; if (string.IsNullOrEmpty(key)) return; // brief 096: a hovered way or face is joined by the line too
            var t = host.Views?.Find(key); if (!t || !t.gameObject.activeInHierarchy || !Proj(t.position + Vector3.up * 2.1f, out var head) || !Proj(t.position, out var feet)) return;
            float k = bkPage.resolvedStyle.scale.value.x; if (k <= 0f) k = 1f; float ox = bkPage.resolvedStyle.left, oy = bkPage.resolvedStyle.top;
            Vector2 G(Vector2 q) => new Vector2((q.x - ox) / k + BkPad, (q.y - oy) / k + BkPad);
            Vector2 a = G(head), b = new(BookLayout.HeroX + 20f + BkPad, 250f + BkPad), f = G(feet);
            p.strokeColor = BkTurq; p.lineWidth = 3f; p.lineCap = LineCap.Round; p.BeginPath(); p.MoveTo(a); p.LineTo(b); p.Stroke();
            p.BeginPath(); for (int i = 0; i <= 48; i++) { float an = i * Mathf.PI * 2f / 48; var q = f + new Vector2(Mathf.Cos(an) * 42f, Mathf.Sin(an) * 26f); if (i == 0) p.MoveTo(q); else p.LineTo(q); } p.ClosePath(); p.lineWidth = 3.5f; p.Stroke();
            p.fillColor = Cream; p.BeginPath(); p.Arc(a, 24f, Deg(0), Deg(360)); p.Fill(); p.lineWidth = 6f; p.BeginPath(); p.Arc(a, 21f, Deg(0), Deg(360)); p.Stroke();
            p.fillColor = BkDeep; p.BeginPath(); p.Arc(a, 8f, Deg(0), Deg(360)); p.Fill();
        }

        Label BkPut(VisualElement parent, float x, float cy, string text, float size, string font, string hex)
        {
            var l = PgText(parent, x, cy - size * .75f, font, size, hex, text); l.style.height = size * 1.5f; l.style.unityTextAlign = TextAnchor.MiddleLeft; return l;
        }
        VisualElement BkBox(VisualElement parent, Rect r, bool pick = false) { var e = E(parent, null, r.x, r.y, r.width, r.height, pick); return e; }

        /// <summary>One page: every slot from BookLayout's items, in its order.</summary>
        void BookDraw(BookPage pg)
        {
            bkFor = pg; var body = bkBody; for (int i = body.childCount - 1; i >= 0; i--) if (body[i] != bkCurve) body[i].RemoveFromHierarchy();
            var r = BookLaidOut = BookLayout.Place(pg, BkTw); var keep = new List<VisualElement>(); var dl = r.Debrief; // brief 099: what stays in front of the fold's veil
            foreach (var it in r.Items)
                switch (it.Slot)
                {
                    case BookSlot.Hero: BkHero(body, pg, it.At); break;
                    case BookSlot.Name: BkPut(body, it.At.x, it.Cy, pg.Name, it.Size, "Jost-Bold", BkInkHex); if (pg.NameTag.Length > 0) BkPut(body, it.At.x + BkTw(pg.Name, it.Size, "Jost-Bold") + 18f, it.Cy + it.Size * .2f, "· " + pg.NameTag, it.Size * .42f, "Jost-Medium", BkSoftHex); break;
                    case BookSlot.Sub: { float lh = r.SubSize * 1.3f; for (int n = 0; n < r.SubLines.Length; n++) BkPut(body, it.At.x, it.Cy + n * lh, r.SubLines[n], r.SubSize, "Jost-Medium", BkSoftHex); break; }
                    case BookSlot.State: { var e = BkBox(body, it.At); Round(e, 23f); e.style.backgroundColor = BkOrange; BkPut(e, 17f, 23f, pg.State, it.Size, "Jost-Bold", BkCreamHex); break; }
                    case BookSlot.FactsHead: BkFold(body, pg, it); break;
                    case BookSlot.Needs: BkNeed(body, pg.Needs[it.Index], it, r.NeedLines[it.Index]); break;
                    case BookSlot.Facts: BkFact(body, pg.Facts[it.Index], it); break;
                    case BookSlot.Gear: BkFact(body, pg.Gear[it.Index], it); break;
                    case BookSlot.Weapon: BkWeapon(body, pg.Weapon, it, r); break;
                    case BookSlot.Ways: BkWay(body, pg.Ways[it.Index], it.At); break;
                    case BookSlot.Can: BkCan(body, pg.Can[it.Index], it, r.CanLines[it.Index], r); break;
                    case BookSlot.Scales: BkScales(body, pg, it); break;
                    case BookSlot.Knows: BkKnows(body, pg, it); break;
                    case BookSlot.Track: if (dl != null) BkRunStop(body, pg, it, r); else BkStop(body, pg, it, r); break;
                    case BookSlot.Others: BkOther(body, pg.Others[it.Index], it.At); break;
                    case BookSlot.Kicker: BkKicker(body, pg, it); break;
                    case BookSlot.Purse: BkPurse(body, pg, it); break;
                    case BookSlot.Faces: BkFace(body, pg.Ways[it.Index / 16].Faces[it.Index % 16], it); break;
                    case BookSlot.Shared: BkShared(body, pg, it); break;
                    case BookSlot.Crew: BkCrew(body, pg.Crew[it.Index], it, r.CrewLines[it.Index]); break;
                    case BookSlot.Begin: BkBegin(body, it); break;
                    case BookSlot.Filters: BkFilter(body, pg.Filters[it.Index], it); break;
                    case BookSlot.Rows: BkRow(body, pg.Rows[it.Index], it); break;
                    case BookSlot.WayIn: BkWayIn(body, pg, it); break; // brief 099, the debrief (HudToolkit.BookDebrief, BookFold)
                    case BookSlot.Ticks: BkTick(body, pg.Ticks[it.Index], it, dl.TickWords[it.Index]); break;
                    case BookSlot.Next: BkNext(body, pg, it, dl.Next[0]); break;
                    case BookSlot.Stats: { var b = BkStat(body, pg, pg.Stats[it.Index], it, dl.Why[it.Index]); if (pg.Fold?.Id == pg.Stats[it.Index].Id) keep.Add(b); break; }
                    case BookSlot.Fold: keep.Add(BkFoldDraw(body, pg.Fold, it, dl)); break;
                    case BookSlot.City: BkCity(body, pg.City[it.Index], it, dl.CityHead[it.Index], dl.CityLine[it.Index]); break;
                    case BookSlot.Again: keep.Add(BkAgain(body, pg, it)); break;
                }
            BkSigns(body, pg); BkChrome(body, pg); if (pg.Fold != null) BkFoldVeil(body, keep);
        }

        void BkHero(VisualElement body, BookPage pg, Rect at)
        {
            var disc = BkBox(body, at); Round(disc, at.width / 2f); disc.style.backgroundColor = pg.HeroFace.Length > 0 ? BkPale : BkDeep; Border(disc, 5f, BkTurq); disc.style.overflow = Overflow.Hidden; disc.name = "book-hero";
            if (pg.HeroFace.Length > 0) Bg(disc, pg.HeroFace, Color.white);
            else { disc.style.borderTopWidth = disc.style.borderBottomWidth = disc.style.borderLeftWidth = disc.style.borderRightWidth = 0f; var g = E(disc, null, at.width * .25f, at.width * .25f, at.width * .5f, at.width * .5f); Icon(g, pg.HeroIcon, Cream); }
        }

        void BkFact(VisualElement body, BookFact f, BookLayout.Item it)
        {
            float x = it.At.x, y = it.At.y, d = it.D;
            if (f.Unknown) { var q = E(body, null, x, y, d, d); Round(q, d / 2f); Border(q, 1.8f, BkSoft); BkPut(q, d * .33f, d / 2f, "?", d * .56f, "Jost-Bold", BkSoftHex); }
            else Icon(E(body, null, x + 2f, y + 2f, d - 4f, d - 4f), f.Icon, BkInk);
            var word = f.Word + (f.Unknown && f.Note.Length == 0 ? " ?" : ""); BkPut(body, x + d + 12f, it.Cy, word, it.Size, "Jost-SemiBold", f.Unknown ? BkSoftHex : BkInkHex);
            if (f.Note.Length > 0) BkPut(body, x + d + 12f + BkTw(word, it.Size, "Jost-SemiBold") + 10f, it.Cy + 1f, f.Note, it.Size * .76f, "Jost-Medium", BkSoftHex);
        }

        /// <summary>A way: the HUD's signage plate, flat: enamel, one cream inline, a pictogram, a word, its price. Shut ways go hollow. A click opens the thing's ring; no order.</summary>
        void BkWay(VisualElement body, BookWay w, Rect at)
        {
            var e = BkBox(body, at, true); Round(e, 18f); e.name = "book-way"; var ink = w.Shut ? BkSoft : Cream; string inkHex = w.Shut ? BkSoftHex : BkCreamHex, subHex = w.Shut ? BkSoftHex : BkDimHex;
            if (w.Shut) Border(e, 2f, BkSoft); else e.style.backgroundColor = BkDeep;
            VisualElement inl = null; if (!w.Shut) { inl = E(e, null, 6f, 6f, at.width - 12f, at.height - 12f); Round(inl, 13f); Border(inl, 2f, Cream); }
            Icon(E(e, null, 26f, at.height / 2f - 28f, 56f, 56f), w.Shut ? "lock" : w.Icon, ink);
            BkPut(e, 100f, at.height / 2f - 16f, w.Word, 40f, "Jost-Bold", inkHex);
            float sx = 100f;
            foreach (var f in w.Sub) { if (f.Icon.Length > 0 && f.Icon != "p_time") { Icon(E(e, null, sx, at.height / 2f + 13f, 20f, 20f), f.Icon, ink); sx += 22f; } BkPut(e, sx, at.height / 2f + 24f, f.Text, 25f, "Jost-Medium", subHex); sx += BkTw(f.Text, 25f, "Jost-Medium") + 10f; }
            string view = w.Faces.Count > 0 ? w.Faces[0].View : w.Target?.Key ?? "";
            if (inl != null && host.BookLookView.Length > 0 && host.BookLookView == view) Border(inl, 2f, BkTurq);
            e.RegisterCallback<PointerEnterEvent>(_ => { if (inl != null) Border(inl, 2f, BkTurq); else Border(e, 2f, BkInk); host.BookHover(view); }); // brief 096: a hover turns the street to the way's place
            e.RegisterCallback<PointerLeaveEvent>(_ => { if (inl != null && host.BookLookView != view) Border(inl, 2f, Cream); else if (inl == null) Border(e, 2f, BkSoft); host.BookHover(null); });
            e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); if (host.BookStart) host.BookPin(view); else host.BookTakeWay(w); }); // the start: a click keeps the street there; in play: the way's ring
        }

        /// <summary>A verb the agent has: a pictogram, its word, its one line. Not a sign and no click: nothing here looks pressable, the ring is where you act.</summary>
        void BkCan(VisualElement body, BookCan c, BookLayout.Item it, string[] lines, BookLayout.Result r)
        {
            float d = it.D, x = it.At.x, tx = x + d + 14f, ws = it.Size, ls = ws <= 23f ? 18f : ws <= 26f ? 20f : 21.5f, y0 = it.At.y;
            Icon(E(body, null, x + 1f, y0 + 2f, d - 2f, d - 2f), c.Icon, BkInk);
            BkPut(body, tx, y0 + ws * .6f, c.Word, ws, "Jost-SemiBold", BkInkHex);
            for (int i = 0; i < lines.Length; i++) BkPut(body, tx, y0 + ws * 1.25f + ls * 1.3f * (i + .4f), lines[i], ls, "Jost-Medium", BkSoftHex);
        }

        /// <summary>The weapon block: its line, then its ratings in two short columns, each a word and a row of pips (the health dots' own look).</summary>
        void BkWeapon(VisualElement body, BookWeapon w, BookLayout.Item it, BookLayout.Result r)
        {
            BkFact(body, new BookFact { Icon = "fire", Word = w.Word, Note = w.Note }, it);
            float x = it.At.x + 2f, y0 = it.At.y + 46f + 10f, col = 300f, wordW = 0f; int rows = (w.Stats.Count + 1) / 2;
            foreach (var s in w.Stats) wordW = Mathf.Max(wordW, BkTw(s.word, 22f, "Jost-SemiBold"));
            for (int i = 0; i < w.Stats.Count; i++)
            {
                var s = w.Stats[i]; float cx = x + (i / rows) * col, cy = y0 + (i % rows) * 36f + 16f;
                BkPut(body, cx, cy, s.word, 22f, "Jost-SemiBold", BkInkHex); float px = cx + wordW + 14f;
                for (int k = 0; k < s.of; k++) { var d = E(body, null, px + k * 20f, cy - 8f, 16f, 16f); Round(d, 8f); if (k < s.pips) d.style.backgroundColor = BkDeep; else Border(d, 1.5f, BkSoft); }
            }
        }

        /// <summary>A facts head that folds: shut, it says how many and a chevron; a click opens or shuts the list.</summary>
        void BkFold(VisualElement body, BookPage pg, BookLayout.Item it)
        {
            string t = pg.FactsHead + (pg.FactsFolded ? "  " + pg.FactsCount : ""); float w = BkTw(t, it.Size, "Jost-Medium") + 34f;
            var e = E(body, null, it.At.x, it.Cy - 16f, w, 32f, true); e.name = "book-fold"; BkPut(e, 0f, 16f, t, it.Size, "Jost-Medium", BkSoftHex);
            var ch = E(e, null, w - 22f, 8f, 16f, 16f); Icon(ch, "chev_down", BkSoft); if (pg.FactsFolded) ch.style.rotate = new Rotate(-90f);
            e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BookToggleEnds(); });
        }

        /// <summary>A condition: a round mark (filled with a tick when it holds, hollow when not, "?" until learned) and its words. Not a button.</summary>
        void BkNeed(VisualElement body, BookNeed n, BookLayout.Item it, string[] lines)
        {
            float x = it.At.x, y = it.At.y, d = it.D; var m = E(body, null, x, y + 2f, d, d); Round(m, d / 2f); m.name = "book-need";
            if (n.Met) { m.style.backgroundColor = BkDeep; Icon(E(m, null, 4f, 4f, d - 8f, d - 8f), "check", Cream); }
            else { Border(m, 1.8f, BkSoft); if (n.Unknown) BkPut(m, d * .3f, d / 2f, "?", d * .6f, "Jost-Bold", BkSoftHex); }
            for (int i = 0; i < lines.Length; i++) BkPut(body, x + d + 12f, y + 13f + BookLayout.NeedLineH * i, lines[i], it.Size, "Jost-Medium", n.Met ? BkInkHex : n.Unknown ? BkSoftHex : BkInkHex);
        }

        void BkScales(VisualElement body, BookPage pg, BookLayout.Item it)
        {
            float cx = it.At.x, y = it.Cy;
            foreach (var s in pg.Scales)
            {
                BkPut(body, cx, y, s.Word, it.Size, "Jost-SemiBold", BkInkHex); cx += BkTw(s.Word, it.Size, "Jost-SemiBold") + 10f;
                int n = Mathf.Max(0, s.Max), on = Mathf.Clamp(s.Value, 0, n);
                for (int k = 0; k < n; k++) { var d = E(body, null, cx, y - 8f, 16f, 16f); Round(d, 8f); if (k < on) d.style.backgroundColor = BkDeep; else Border(d, 1.5f, BkSoft); cx += 20f; }
                cx += 22f;
            }
        }

        void BkKnows(VisualElement body, BookPage pg, BookLayout.Item it)
        {
            float cx = it.At.x;
            foreach (var w in pg.Knows)
            {
                if (w.Face.Length > 0) { var f = E(body, null, cx, it.Cy - 16f, 32f, 32f); Round(f, 16f); Bg(f, w.Face, Color.white); } else Icon(E(body, null, cx + 2f, it.Cy - 14f, 28f, 28f), w.Icon, BkInk);
                cx += 40f; BkPut(body, cx, it.Cy, w.Word, it.Size, "Jost-SemiBold", BkInkHex); cx += BkTw(w.Word, it.Size, "Jost-SemiBold") + 24f;
            }
        }

        void BkStop(VisualElement body, BookPage pg, BookLayout.Item it, BookLayout.Result r)
        {
            var s = pg.Track[it.Index]; float x = it.At.x, y = it.At.y, d = it.D; int first = pg.Track.Count - Mathf.Min(pg.Track.Count, 6);
            if (it.Index < pg.Track.Count - 1 && it.Index >= first) { var ln = E(body, null, x + d / 2f, y + d / 2f - 1.25f, r.TrackStep, 2.5f); ln.style.backgroundColor = BkSoft; ln.SendToBack(); bkCurve.SendToBack(); }
            var disc = E(body, null, x, y, d, d); Round(disc, d / 2f);
            if (s.Past) disc.style.backgroundColor = BkDeep; else { disc.style.backgroundColor = Hex(0xf7f4ec); Border(disc, 2f, BkSoft); }
            float ic = d * .6f, off = (d - ic) / 2f - (s.Past ? 0f : 2f); Icon(E(disc, null, off, off, ic, ic), s.Icon, s.Past ? Cream : BkInk);
            BkPut(body, x, y + d + 26f, s.Word, it.Size, "Jost-SemiBold", BkInkHex); BkPut(body, x, y + d + 56f, s.Time, 22f, "Jost-Medium", BkSoftHex);
        }

        void BkOther(VisualElement body, BookOther o, Rect at)
        {
            var e = BkBox(body, at, true); e.name = "book-other";
            var f = E(e, null, 0f, 0f, 80f, 80f); Round(f, 40f); f.style.backgroundColor = BkPale; Border(f, 3f, Cream); Bg(f, o.Face, Color.white);
            var pl = E(e, null, 2f, 76f, 76f, 28f); Round(pl, 7f); pl.style.backgroundColor = BkBody; var lb = BkPut(pl, 0f, 14f, o.Word, 17f, "Jost-SemiBold", BkCreamHex); lb.style.width = 76f; lb.style.unityTextAlign = TextAnchor.MiddleCenter;
            e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BookShow("cell", o.Agent); });
        }

        /// <summary>The round section signs: spaced from the list alone. The lit one is the café's orange and larger; one with no page is dim and takes no click.</summary>
        void BkSigns(VisualElement body, BookPage pg)
        {
            var list = host.BookSections; if (list == null) return; int n = list.Count; BookSigns = n;
            for (int i = 0; i < n; i++)
            {
                var s = list[i]; bool menu = s.Id == "menu", cur = s.Id == pg.Section, dim = !menu && (s.Page == null || !host.BookSectionLive(s.Id)); float d = cur ? 108f : 84f, y = BookLayout.SignY(i, n), x = BookLayout.SignX; // brief 096: Menu opens the pause menu; the Journal is dim until it has a row
                var e = E(body, null, x - d / 2f, y - d / 2f, d, d, !dim); Round(e, d / 2f); e.style.backgroundColor = cur ? BkOrange : BkBody; e.name = "book-sign-" + s.Id; if (dim) e.style.opacity = .34f;
                Icon(E(e, null, d * .25f, d * .14f, d * .5f, d * .5f), s.Icon, Cream);
                var l = BkPut(e, 0f, d * .74f, s.Word, cur ? 22f : 16f, "Jost-Bold", BkCreamHex); l.style.width = d; l.style.unityTextAlign = TextAnchor.MiddleCenter;
                if (menu) e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BookToMenu(); });
                else if (!dim) { string id = s.Id; e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BookShow(id); }); }
            }
        }

        /// <summary>The small clock chip (the kit's plate, the pause mark) and the page's back control, top right.</summary>
        void BkChrome(VisualElement body, BookPage pg)
        {
            var chip = PlateDark(body, 40f, 0.43f); chip.style.position = Position.Absolute; chip.style.right = 64f; chip.style.top = 14f; chip.style.paddingLeft = 12f; chip.style.paddingRight = 14f;
            Icon(E(chip, null, w: 18f, h: 18f), "pause", Cream); bkClock = Words(chip, 22f); bkClock.style.marginLeft = 6f; bkClock.style.unityFontDefinition = FontDefinition.FromFont(Resources.Load<Font>("Kit/fonts/Jost-Medium-Tab"));
            if (pg.Begin || pg.Again.Count > 0) return; // brief 096: the start has Begin (bottom right) where Back would be; brief 099: the debrief has its ways on
            string word = BookWordsFile.Get().Back; float bw = 30f + BkTw(word, 24f, "Jost-SemiBold");
            var back = E(body, null, 1920f - 64f - 120f - 24f - bw, 14f, bw, 40f, true); back.name = "book-back";
            var ch = E(back, null, 0f, 11f, 18f, 18f); Icon(ch, "chev_up", BkInk); ch.style.rotate = new Rotate(-90f);
            BkPut(back, 26f, 20f, word, 24f, "Jost-SemiBold", BkInkHex);
            back.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BookClose(); });
        }
    }
}
