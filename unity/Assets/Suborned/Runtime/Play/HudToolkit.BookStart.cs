using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 096, the start's pieces on the book's light page (Glyph's 1-start-job.png and 1b): the mission's title in spaced capitals over the name, the purse
    /// and Heat beside the folded ways to fail, the faces under each way (a hover turns the street to them, a click keeps it there), the shared end, the cell strip
    /// and Begin. Drawn from BookLayout's items like every other slot; the colours are the book's own (HudToolkit.Book).</summary>
    public sealed partial class HudToolkit
    {
        void BkKicker(VisualElement body, BookPage pg, BookLayout.Item it)
        {
            var l = PgText(body, it.At.x, it.Cy - it.Size * .75f, "Jost-SemiBold", it.Size, pg.KickerHot ? "#e8622c" : "#1f6c67", pg.Kicker, 0.26f, true); l.style.height = it.Size * 1.5f; l.style.unityTextAlign = TextAnchor.MiddleLeft; // brief 099: a lost run's kicker in the orange
            BkLook(body, it); // brief 107: Day / Night and the weather on the start page (HudToolkit.Look107.cs)
        }

        void BkPurse(VisualElement body, BookPage pg, BookLayout.Item it)
        {
            float x = it.At.x, cy = it.Cy;
            if (pg.Purse.Length > 0) { Icon(E(body, null, x, cy - 14f, 28f, 28f), "p_cash", BkInk); x += 34f; BkPut(body, x, cy, pg.Purse, 24f, "Jost-SemiBold", BkInkHex); x += BkTw(pg.Purse, 24f, "Jost-SemiBold") + 32f; }
            if (pg.Heat.Length > 0)
            {
                BkPut(body, x, cy, pg.HeatWord, 24f, "Jost-Medium", BkSoftHex); x += BkTw(pg.HeatWord, 24f, "Jost-Medium") + 8f;
                Icon(E(body, null, x, cy - 13f, 26f, 26f), "eye_unk", BkInk); x += 30f; BkPut(body, x, cy, pg.Heat, 24f, "Jost-SemiBold", BkInkHex);
            }
        }

        /// <summary>A face under a way: the cameo (or badge) in the turquoise ring, the name; a hover turns the street to them, a click keeps it there.</summary>
        void BkFace(VisualElement body, BookFace f, BookLayout.Item it)
        {
            var e = BkBox(body, it.At, true); e.name = "book-face"; float d = it.D; bool on = host.BookLookView == f.View;
            var disc = E(e, null, 0f, 0f, d, d); Round(disc, d / 2f); disc.style.backgroundColor = BkPale; Border(disc, 2.5f, on ? BkDeep : BkTurq); disc.style.overflow = Overflow.Hidden;
            if (f.Badge) Bg(E(disc, null, 4f, 4f, d - 8f, d - 8f), f.Face, Color.white); else Bg(disc, f.Face, Color.white);
            BkPut(e, d + 10f, d / 2f, f.Word, it.Size, "Jost-SemiBold", on ? "#1f6c67" : BkInkHex);
            e.RegisterCallback<PointerEnterEvent>(_ => host.BookHover(f.View)); e.RegisterCallback<PointerLeaveEvent>(_ => host.BookHover(null));
            e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BookPin(f.View); });
        }

        /// <summary>The line where both ways end: the car on a deep-teal disc, then the words.</summary>
        void BkShared(VisualElement body, BookPage pg, BookLayout.Item it)
        {
            float d = it.D; var disc = E(body, null, it.At.x, it.Cy - d / 2f, d, d); Round(disc, d / 2f); disc.style.backgroundColor = BkDeep;
            Icon(E(disc, null, d * .2f, d * .2f, d * .6f, d * .6f), "car", Cream);
            BkPut(body, it.At.x + d + 14f, it.Cy, pg.Shared, it.Size, "Jost-SemiBold", BkInkHex);
        }

        /// <summary>One of the cell: face in the ring, name, job word, one line; a click opens their Cell page.</summary>
        void BkCrew(VisualElement body, BookCrew c, BookLayout.Item it, string[] lines)
        {
            var e = BkBox(body, it.At, true); e.name = "book-crew"; float d = it.D;
            var disc = E(e, null, 0f, 0f, d, d); Round(disc, d / 2f); disc.style.backgroundColor = BkPale; Border(disc, 2.5f, BkTurq); disc.style.overflow = Overflow.Hidden; Bg(disc, c.Face, Color.white);
            BkPut(e, d + 12f, 21f, c.Name, it.Size, "Jost-Bold", BkInkHex); BkPut(e, d + 12f, 45f, c.Job, 17f, "Jost-Medium", BkSoftHex);
            for (int i = 0; i < lines.Length; i++) BkPut(e, 0f, d + 20f + i * 21f, lines[i], 17f, "Jost-Medium", BkInkHex);
            int k = c.Agent; e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BookShow("cell", k); });
        }

        /// <summary>Begin: the book's orange, where Back would be; the start sequence (HostBook.Start).</summary>
        void BkBegin(VisualElement body, BookLayout.Item it)
        {
            var e = BkBox(body, it.At, true); e.name = "book-begin"; Round(e, it.At.height / 2f); e.style.backgroundColor = BkOrange;
            string word = BookWordsFile.Get().Begin; float tw = BkTw(word, it.Size, "Jost-Bold"), x = (it.At.width - tw - 26f) / 2f;
            BkPut(e, x, it.At.height / 2f, word, it.Size, "Jost-Bold", BkCreamHex);
            var ch = E(e, null, x + tw + 10f, it.At.height / 2f - 9f, 18f, 18f); Icon(ch, "chev_up", Cream); ch.style.rotate = new Rotate(90f);
            e.RegisterCallback<PointerEnterEvent>(_ => e.style.scale = new Scale(new Vector3(1.04f, 1.04f, 1f))); e.RegisterCallback<PointerLeaveEvent>(_ => e.style.scale = new Scale(Vector3.one));
            e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.BriefingBegin(); });
        }
    }
}
