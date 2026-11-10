using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 099, the debrief's pieces on the book's light page (Sky's pick, concept A: briefs/099/glyph/A-front-*.png): the way taken over its start-page rows
    /// answered, a fail's next time, the five numbers (each a click that opens its fold, HudToolkit.BookFold), what the city does now, the run along the foot with the
    /// cell's acts as their faces, and the ways on. Drawn from BookLayout's items like every other slot, in the book's own colours (HudToolkit.Book): the orange is the
    /// one that lost it and the main way on; nothing here gives an order.</summary>
    public sealed partial class HudToolkit
    {
        const string BkOrangeHex = "#e8622c", BkDeepHex = "#1f6c67";

        void BkWayIn(VisualElement body, BookPage pg, BookLayout.Item it)
        {
            float x = it.At.x, cy = it.Cy, d = it.D; var disc = E(body, null, x, cy - d / 2f, d, d); Round(disc, d / 2f); disc.style.backgroundColor = BkDeep;
            Icon(E(disc, null, d * .18f, d * .18f, d * .64f, d * .64f), pg.WayIcon, Cream);
            BkPut(body, x + d + 12f, cy, pg.WayWord, it.Size, "Jost-Bold", BkInkHex);
            if (pg.WayNote.Length > 0) BkPut(body, x + d + 18f + BkTw(pg.WayWord, it.Size, "Jost-Bold"), cy + 2f, pg.WayNote, 20f, "Jost-Medium", BkSoftHex);
        }

        /// <summary>A start-page row answered: a filled tick (it came true), the orange cross (the one that lost it), a hollow ring (never), "?" (never learned); its words, and how and when right-aligned.</summary>
        void BkTick(VisualElement body, BookTick t, BookLayout.Item it, float wordSize)
        {
            float x = it.At.x + 2f, cy = it.Cy, d = it.D; var m = E(body, null, x, cy - d / 2f, d, d); Round(m, d / 2f); m.name = "debrief-tick";
            if (t.Met) { m.style.backgroundColor = BkDeep; Icon(E(m, null, d * .2f, d * .2f, d * .6f, d * .6f), "check", Cream); }
            else if (t.Lost) { m.style.backgroundColor = BkOrange; Icon(E(m, null, d * .22f, d * .22f, d * .56f, d * .56f), "close_x", Cream); }
            else { Border(m, 2f, BkSoft); if (t.Unknown) BkMid(m, d, 2f, "?", d * .6f, "Jost-Bold", BkSoftHex); }
            BkPut(body, x + 40f, cy, t.Words, wordSize, "Jost-SemiBold", t.Met || t.Lost ? BkInkHex : BkSoftHex);
            if (t.Note.Length == 0) return;
            float ns = it.Size - 2f; string font = t.Lost ? "Jost-SemiBold" : "Jost-Medium";
            BkPut(body, BookLayout.DX1 - BkTw(t.Note, ns, font), cy, t.Note, ns, font, t.Lost ? BkOrangeHex : BkSoftHex);
        }

        /// <summary>A fail's next time: the small caps in the orange, the line, then the cell's own ways on the culprit as outlined chips (a face, the way, who). Words, not buttons.</summary>
        void BkNext(VisualElement body, BookPage pg, BookLayout.Item it, string[] lines)
        {
            float x = it.At.x, cy = it.Cy;
            var caps = PgText(body, x, cy - 17f * .75f, "Jost-SemiBold", 17f, BkOrangeHex, pg.NextHead, 0.3f, true); caps.style.height = 17f * 1.5f; caps.style.unityTextAlign = TextAnchor.MiddleLeft;
            for (int i = 0; i < lines.Length; i++) BkPut(body, x, cy + 34f + i * 31.25f, lines[i], 25f, "Jost-SemiBold", BkInkHex);
            float cx = x, top = it.D;
            foreach (var c in pg.NextWays)
            {
                float w = 56f + BkTw(c.Word, 21f, "Jost-SemiBold") + 22f + BkTw(c.Who, 17f, "Jost-Medium") + 18f; var chip = E(body, null, cx, top, w, 44f); Round(chip, 22f); Border(chip, 1.8f, BkDeep); chip.name = "debrief-chip";
                var f = E(chip, null, 4f, 3.2f, 34f, 34f); Round(f, 17f); f.style.backgroundColor = BkPale; f.style.overflow = Overflow.Hidden; if (c.Face.Length > 0) Bg(f, c.Face, Color.white);
                Icon(E(chip, null, 44f, 10f, 22f, 22f), c.Icon, BkDeep); BkPut(chip, 72f, 20f, c.Word, 21f, "Jost-SemiBold", BkDeepHex);
                BkPut(chip, 80f + BkTw(c.Word, 21f, "Jost-SemiBold"), 21f, c.Who, 17f, "Jost-Medium", BkSoftHex); cx += w + 12f;
            }
        }

        /// <summary>A number: its icon and label with the fold's chevron, the value, its reason under it. The whole box is the click that opens or shuts its fold.</summary>
        VisualElement BkStat(VisualElement body, BookPage pg, BookStat s, BookLayout.Item it, string[] why)
        {
            bool open = pg.Fold != null && pg.Fold.Id == s.Id; var box = BkBox(body, it.At, true); box.name = "debrief-stat"; Round(box, 18f);
            if (open) { box.style.backgroundColor = new Color(BkPaper.r, BkPaper.g, BkPaper.b, 1f); Border(box, 2.5f, BkDeep); }
            float x = 12f, ty = 14f; var tint = s.Hot ? BkOrange : BkDeep;
            Icon(E(box, null, x, ty - 15f, 26f, 26f), s.Icon, tint); BkPut(box, x + 34f, ty + 10f, s.Label, 19f, "Jost-SemiBold", BkSoftHex);
            var ch = E(box, null, x + 40f + BkTw(s.Label, 19f, "Jost-SemiBold"), ty + 2f, 16f, 16f); Icon(ch, "chev_down", open ? BkDeep : BkSoft); ch.style.rotate = new Rotate(open ? 0f : -90f);
            BkPut(box, x, ty + 64f, s.Value, it.Size, "Jost-Bold", s.Hot ? BkOrangeHex : BkInkHex);
            for (int i = 0; i < why.Length; i++) BkPut(box, x, ty + 112f + i * 17.5f * 1.25f, why[i], 17.5f, "Jost-Medium", BkSoftHex);
            box.RegisterCallback<PointerEnterEvent>(_ => { if (!open) Border(box, 1.5f, BkSoft); }); box.RegisterCallback<PointerLeaveEvent>(_ => { if (!open) Border(box, 0f, BkSoft); });
            string id = s.Id; box.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.DebriefFold(id); });
            return box;
        }

        /// <summary>What the city does now: an icon, the head on up to two lines, its line under it.</summary>
        void BkCity(VisualElement body, BookCity c, BookLayout.Item it, string[] head, string[] line)
        {
            float x = it.At.x, cy = it.Cy; Icon(E(body, null, x, cy - 13f, 26f, 26f), c.Icon, BkDeep);
            for (int i = 0; i < head.Length; i++) BkPut(body, x + 36f, cy + i * 21f * 1.15f, head[i], 21f, "Jost-SemiBold", BkInkHex);
            float y = cy + head.Length * 21f * 1.15f + 2f; for (int i = 0; i < line.Length; i++) BkPut(body, x + 36f, y + i * 17.5f * 1.25f, line[i], 17.5f, "Jost-Medium", BkSoftHex);
        }

        /// <summary>A beat of the run: the cell's act as its agent's face, the one that lost it in the orange, the end and our acts on the deep disc, the city's on a hollow one; its word and time under it.</summary>
        void BkRunStop(VisualElement body, BookPage pg, BookLayout.Item it, BookLayout.Result r)
        {
            var s = pg.Track[it.Index]; float x = it.At.x, y = it.At.y, d = it.D; var lines = r.Debrief.Run.TryGetValue(it.Index, out var l) ? l : new[] { s.Word };
            if (it.Index < pg.Track.Count - 1) { var ln = E(body, null, x + d / 2f, y + d / 2f - 1.25f, r.TrackStep, 2.5f); ln.style.backgroundColor = BkSoft; ln.SendToBack(); bkCurve.SendToBack(); }
            var disc = E(body, null, x, y, d, d); Round(disc, d / 2f); disc.name = "debrief-beat";
            if (s.Face.Length > 0) { disc.style.backgroundColor = BkPale; disc.style.overflow = Overflow.Hidden; Bg(disc, s.Face, Color.white); Border(disc, 3f, s.Hot ? BkOrange : BkDeep); }
            else
            {
                if (s.Hot) disc.style.backgroundColor = BkOrange; else if (s.Past) disc.style.backgroundColor = BkDeep; else { disc.style.backgroundColor = Hex(0xf7f4ec); Border(disc, 2f, BkSoft); }
                float ic = d * .6f, off = (d - ic) / 2f; Icon(E(disc, null, off, off, ic, ic), s.Icon, s.Hot || s.Past ? Cream : BkInk);
            }
            float sz = r.Debrief.RunSize; for (int i = 0; i < lines.Length; i++) BkPut(body, x, y + d + 22f + i * sz * 1.05f, lines[i], sz, "Jost-SemiBold", s.Hot ? BkOrangeHex : BkInkHex);
            BkPut(body, x, y + d + 48f + (lines.Length - 1) * sz * 1.05f, s.Time, 19f, "Jost-Medium", BkSoftHex);
        }

        /// <summary>The ways on: Try again (the orange pill), the new-or-same choice over it (Index -1), and the outlined ways. Each is a click; none is key-only.</summary>
        VisualElement BkAgain(VisualElement body, BookPage pg, BookLayout.Item it)
        {
            var e = BkBox(body, it.At, true); float h = it.At.height;
            if (it.Index < 0)
            {
                e.name = "debrief-same"; Round(e, h / 2f); e.style.backgroundColor = new Color(BkPaper.r, BkPaper.g, BkPaper.b, 1f); Border(e, 1.5f, BkRule);
                float a = BkTw(pg.NewWord, 17f, "Jost-SemiBold") + 32f;
                var lit = E(e, null, pg.SameOn ? a : 2f, 2f, pg.SameOn ? it.At.width - a - 2f : a, h - 4f); Round(lit, (h - 4f) / 2f); lit.style.backgroundColor = BkDeep;
                BkPut(e, 16f, h / 2f - 1f, pg.NewWord, 17f, "Jost-SemiBold", pg.SameOn ? BkSoftHex : BkCreamHex); BkPut(e, a + 14f, h / 2f - 1f, pg.SameWord, 17f, "Jost-SemiBold", pg.SameOn ? BkCreamHex : BkSoftHex);
                e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.DebriefSame(ev.localPosition.x >= a); });
                return e;
            }
            var w = pg.Again[it.Index]; e.name = "debrief-way-" + w.Id; Round(e, h / 2f);
            if (w.Main)
            {
                e.style.backgroundColor = BkOrange; BkPut(e, 30f, h / 2f - 1f, w.Word, it.Size, "Jost-Bold", BkCreamHex);
                var ch = E(e, null, it.At.width - 44f, h / 2f - 10f, 20f, 20f); Icon(ch, "chev_up", Cream); ch.style.rotate = new Rotate(90f);
            }
            else
            {
                e.style.backgroundColor = new Color(BkPaper.r, BkPaper.g, BkPaper.b, 1f); Border(e, 2.2f, BkDeep);
                Icon(E(e, null, 22f, h / 2f - 15f, 30f, 30f), w.Icon, BkDeep); BkPut(e, 62f, h / 2f - 1f, w.Word, it.Size, "Jost-SemiBold", BkDeepHex);
            }
            e.RegisterCallback<PointerEnterEvent>(_ => e.style.scale = new Scale(new Vector3(1.03f, 1.03f, 1f))); e.RegisterCallback<PointerLeaveEvent>(_ => e.style.scale = new Scale(Vector3.one));
            string id = w.Id; e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.DebriefWay(id); });
            return e;
        }
    }
}
