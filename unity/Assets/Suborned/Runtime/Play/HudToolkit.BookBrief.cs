using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 108, the briefing's three pages drawn (Glyph's mocks briefs/108/glyph/pages): the story's lines, a row a place or a danger (an icon on the book's dark disc,
    /// a face in the turquoise ring, or the badge the player will see over it in the street), a way's chip, the pager (Back, three dots, Next), and the label pinned in the street
    /// on the place the page shows. Every piece is a click (Sky: no key-only features); the colours are the book's own (HudToolkit.Book).</summary>
    public sealed partial class HudToolkit
    {
        VisualElement bkPin; Label bkPinWord; string bkPinFor = ""; float bkCurveNow = BookLayout.BriefCurve;
        static readonly Color BkRowOn = new(0x60 / 255f, 0xe0 / 255f, 0xea / 255f, .14f), BkRowHover = new(0x17 / 255f, 0x30 / 255f, 0x2f / 255f, .05f);

        void BkStory(VisualElement body, BookLayout.Item it, string[] lines)
        {
            float lh = it.Size * 1.32f;
            for (int n = 0; n < lines.Length; n++) BkPut(body, it.At.x, it.At.y + lh * (n + .5f), lines[n], it.Size, it.Index == 0 ? "Jost-SemiBold" : "Jost-Medium", BkInkHex);
        }

        /// <summary>A place or a danger: its picture, word and line, a way's chip or a time; a hover turns the street to it, a click keeps it there.</summary>
        void BkBriefRow(VisualElement body, BookPlace pl, BookLayout.Item it, bool danger)
        {
            var at = it.At; var e = BkBox(body, at, true); e.name = danger ? "book-danger" : "book-place"; Round(e, 22f);
            void Look(bool hover) { bool on = pl.On || host.BookLookView.Length > 0 && host.BookLookView == pl.View; e.style.backgroundColor = on ? BkRowOn : hover ? BkRowHover : new Color(0, 0, 0, 0); Border(e, 2f, on ? BkTurq : new Color(0, 0, 0, 0)); }
            Look(false);
            float d = it.D, dy = 12f, dx = 18f;
            if (pl.Badge) { var bc = E(e, null, dx, dy, d, d); Round(bc, d / 2f); bc.style.overflow = Overflow.Hidden; Bg(E(bc, null, -1f, -1f, d + 2f, d + 2f), pl.Face, Color.white); } // a round clip: a badge's square corners don't show on the lit row
            else if (pl.Face.Length > 0) { var f = E(e, null, dx, dy, d, d); Round(f, d / 2f); f.style.backgroundColor = BkPale; Border(f, 3f, BkTurq); f.style.overflow = Overflow.Hidden; Bg(f, pl.Face, Color.white); }
            else { var c = E(e, null, dx, dy, d, d); Round(c, d / 2f); c.style.backgroundColor = BkBody; Border(c, 3f, danger ? DlRim : BkTurq); float s = d * .48f; Icon(E(c, null, (d - 6f - s) / 2f, (d - 6f - s) / 2f, s, s), pl.Icon, Cream); }
            float tx = dx + d + 18f, cy = dy + 20f;
            BkPut(e, tx, cy, pl.Word, it.Size, "Jost-Bold", BkInkHex);
            if (pl.When.Length > 0) // a time chip after the word (the rivals' arrival)
            {
                float wx = tx + BkTw(pl.Word, it.Size, "Jost-Bold") + 14f, ww = 30f + BkTw(pl.When, 20f, "Jost-SemiBold") + 12f; var c = E(e, null, wx, cy - 16f, ww, 32f); Round(c, 16f); c.style.backgroundColor = BkBody;
                Icon(E(c, null, 9f, 8f, 16f, 16f), "nextevent", Cream); BkPut(c, 29f, 16f, pl.When, 20f, "Jost-SemiBold", BkCreamHex);
            }
            BkPut(e, tx, cy + 38f, pl.Line, 23f, "Jost-Medium", BkSoftHex);
            if (pl.Chip.Length > 0)
            {
                bool open = host.BookPageNow?.WayOpen == pl.Way; float cw = 10f + 18f + 8f + BkTw(pl.Chip, 19f, "Jost-SemiBold") + 8f + 12f + 12f;
                var c = E(e, null, tx, cy + 64f, cw, 34f, true); c.name = "book-chip"; Round(c, 17f); c.style.backgroundColor = BkDeep;
                Icon(E(c, null, 10f, 8f, 18f, 18f), pl.Way == "pickup" ? "car" : "enter", Cream); BkPut(c, 36f, 17f, pl.Chip, 19f, "Jost-SemiBold", BkCreamHex);
                Icon(E(c, null, cw - 24f, 11f, 12f, 12f), open ? "chev_up" : "chev_down", Cream);
                c.RegisterCallback<PointerEnterEvent>(_ => Border(c, 2f, BkTurq)); c.RegisterCallback<PointerLeaveEvent>(_ => Border(c, 0f, BkTurq));
                string way = pl.Way, view = pl.View; c.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BookWayToggle(way, view); });
            }
            string v = pl.View;
            e.RegisterCallback<PointerEnterEvent>(_ => { Look(true); if (v.Length > 0) host.BookBriefHover(v); });
            e.RegisterCallback<PointerLeaveEvent>(_ => { Look(false); if (v.Length > 0) host.BookBriefHover(null); });
            e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); if (pl.Way.Length > 0 && host.BookPageNow?.WayOpen == pl.Way) host.BookWayToggle(pl.Way, v); else host.BookBriefPin(v); });
        }

        /// <summary>Back, the three dots (each a click to its page), Next; the mission page's quiet Skip to Begin after a first play.</summary>
        void BkPager(VisualElement body, BookPage pg, BookLayout.Item it)
        {
            float x0 = it.At.x, x1 = it.At.xMax, y = it.At.y, h = it.At.height; int st = pg.Step; bool next = st < 2;
            float backW = 0f;
            if (st > 0)
            {
                backW = 12f + 16f + 8f + BkTw(pg.BackWord, 24f, "Jost-SemiBold") + 18f; var b = E(body, null, x0, y + h / 2f - 24f, backW, 48f, true); b.name = "book-pager-back"; Round(b, 24f); Border(b, 2f, BkDeep);
                var ch = E(b, null, 12f, 14f, 16f, 16f); Icon(ch, "chev_up", BkDeep); ch.style.rotate = new Rotate(-90f); BkPut(b, 36f, 22f, pg.BackWord, 24f, "Jost-SemiBold", "#1f6c67");
                b.RegisterCallback<PointerEnterEvent>(_ => b.style.backgroundColor = BkRowHover); b.RegisterCallback<PointerLeaveEvent>(_ => b.style.backgroundColor = new Color(0, 0, 0, 0));
                b.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BookStepBy(-1); });
            }
            else if (pg.Skip)
            {
                float sw = BkTw(pg.SkipWord, 21f, "Jost-SemiBold"); backW = sw + 26f; var s = E(body, null, x0, y + h / 2f - 20f, backW, 40f, true); s.name = "book-skip";
                BkPut(s, 0f, 20f, pg.SkipWord, 21f, "Jost-SemiBold", BkSoftHex); var u = E(s, null, 0f, 33f, sw, 1.5f); u.style.backgroundColor = new Color(BkSoft.r, BkSoft.g, BkSoft.b, .45f);
                var ch = E(s, null, sw + 8f, 14f, 13f, 13f); Icon(ch, "chev_up", BkSoft); ch.style.rotate = new Rotate(90f);
                s.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BookSkip(); });
            }
            float right = next ? x1 - BookLayout.NextW : x1, dw = 3f * 38f + 2f * 26f, cx = (x0 + 112f + right) / 2f - dw / 2f, dy = y + h / 2f - 19f;
            for (int i = 0; i < 3; i++)
            {
                if (i > 0) { var bar = E(body, null, cx + i * 64f - 26f, dy + 18f, 26f, 2f); bar.style.backgroundColor = new Color(BkDeep.r, BkDeep.g, BkDeep.b, .35f); }
                bool on = i == st, done = i < st; var dt = E(body, null, cx + i * 64f, dy, 38f, 38f, true); dt.name = "book-dot-" + i; Round(dt, 19f);
                dt.style.backgroundColor = on ? BkDeep : done ? BkPale : new Color(BkPaper.r, BkPaper.g, BkPaper.b, 1f); Border(dt, 2f, done ? BkPale : BkDeep);
                var l = BkPut(dt, 0f, 17f, (i + 1).ToString(), 18f, "Jost-Bold", on ? BkCreamHex : BkInkHex); l.style.width = 34f; l.style.unityTextAlign = TextAnchor.MiddleCenter;
                int k = i; dt.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BookStepTo(k); });
            }
            if (!next) return;
            var n = E(body, null, x1 - BookLayout.NextW, y, BookLayout.NextW, h, true); n.name = "book-next"; Round(n, h / 2f); n.style.backgroundColor = BkDeep;
            var inl = E(n, null, 5f, 5f, BookLayout.NextW - 10f, h - 10f); Round(inl, h / 2f - 5f); Border(inl, 2f, new Color(Cream.r, Cream.g, Cream.b, .55f));
            float tw = BkTw(pg.NextWord, it.Size, "Jost-Bold"), tx = (BookLayout.NextW - tw - 28f) / 2f; BkPut(n, tx, h / 2f, pg.NextWord, it.Size, "Jost-Bold", BkCreamHex);
            var nc = E(n, null, tx + tw + 10f, h / 2f - 9f, 18f, 18f); Icon(nc, "chev_up", Cream); nc.style.rotate = new Rotate(90f);
            n.RegisterCallback<PointerEnterEvent>(_ => Border(inl, 2f, BkTurq)); n.RegisterCallback<PointerLeaveEvent>(_ => Border(inl, 2f, new Color(Cream.r, Cream.g, Cream.b, .55f)));
            n.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BookStepBy(1); });
        }

        /// <summary>From BookHud, each frame: the page's edge slides to its page's (the place page gives the street room), and the label pinned over the place the street shows.</summary>
        void BkBriefFrame()
        {
            var pg = host.BookPageNow; float want = pg?.CurveLeft ?? BookLayout.BriefCurve;
            if (!Mathf.Approximately(bkCurveNow, want)) { bkCurveNow = Mathf.MoveTowards(bkCurveNow, want, Time.unscaledDeltaTime * 1400f); bkCurve?.MarkDirtyRepaint(); }
            if (bkPage == null) return;
            if (bkPin == null) BkPinBuild();
            string view = host.BookLookView, word = ""; if (pg != null && pg.Step >= 1 && view.Length > 0) { foreach (var p in pg.Places) if (p.View == view) word = p.Word; foreach (var p in pg.Dangers) if (p.View == view) word = p.Word; }
            var t = word.Length > 0 ? host.BookView(view) : null; bool show = false;
            if (t && Proj(t.position + Vector3.up * PinUp(view), out var q))
            {
                float k = bkPage.resolvedStyle.scale.value.x; if (k <= 0f) k = 1f; float gx = (q.x - bkPage.resolvedStyle.left) / k, gy = (q.y - bkPage.resolvedStyle.top) / k;
                if (word != bkPinFor) { bkPinFor = word; Text(bkPinWord, word); }
                float w = BkTw(word, 26f, "Jost-Bold") + 36f;
                show = gx > 60f + w / 2f && gx < bkCurveNow - 60f - w / 2f && gy > 130f && gy < 940f;
                if (show) { bkPin.style.left = gx - w / 2f; bkPin.style.top = gy - 64f; bkPin.style.width = w; }
            }
            Show(bkPin, show);
        }

        /// <summary>How far above a view's own point its label sits (metres): over a head, a car's roof, the door's marker or a camera's mast.</summary>
        static float PinUp(string view) => view.StartsWith("camera/") ? 6.4f : view.StartsWith("car/") ? 1.8f : view == "door" ? 1.8f : 2.3f;

        void BkPinBuild()
        {
            bkPin = E(bkPage, null, 0f, 0f, 120f, 50f); bkPin.name = "book-pin"; Round(bkPin, 12f); bkPin.style.backgroundColor = BkBody; Border(bkPin, 2f, DlRim);
            bkPinWord = BkPut(bkPin, 16f, 23f, "", 26f, "Jost-Bold", BkCreamHex); bkPinFor = "";
            var tl = E(bkPin, null, 0f, 41f, 16f, 16f); tl.name = "book-pin-tail"; tl.style.backgroundColor = BkBody; tl.style.rotate = new Rotate(45f);
            tl.style.borderBottomWidth = tl.style.borderRightWidth = 2f; tl.style.borderBottomColor = tl.style.borderRightColor = DlRim;
            bkPin.RegisterCallback<GeometryChangedEvent>(_ => tl.style.left = bkPin.resolvedStyle.width / 2f - 10f);
            Show(bkPin, false);
        }
    }
}
