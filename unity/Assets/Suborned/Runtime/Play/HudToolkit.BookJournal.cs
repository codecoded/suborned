using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 096, the Journal's pieces (Glyph's 3c-journal.png): the filter chips and the rows (time, icon on a disc, what happened, → where, the unread dot);
    /// crucial rows take the book's orange disc; an open row shows its lines with the speaker's cameo, then its result. A click on a row opens it and turns the street.</summary>
    public sealed partial class HudToolkit
    {
        static readonly Color BkRule = new(0x17 / 255f, 0x30 / 255f, 0x2f / 255f, 0.12f), BkOpenBg = new(0x9b / 255f, 0xcf / 255f, 0xc2 / 255f, 0.22f);

        void BkFilter(VisualElement body, BookFilter f, BookLayout.Item it)
        {
            var e = BkBox(body, it.At, true); e.name = "book-filter"; Round(e, it.At.height / 2f);
            if (f.On) e.style.backgroundColor = BkDeep; else Border(e, 1.5f, BkSoft);
            BkPut(e, 18f, it.At.height / 2f, f.Word, it.Size, "Jost-SemiBold", f.On ? BkCreamHex : BkInkHex);
            string id = f.Id; e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.JournalFilterSet(id); });
        }

        void BkRow(VisualElement body, BookRow row, BookLayout.Item it)
        {
            var e = BkBox(body, it.At, true); e.name = "book-row"; float h = BookLayout.RowH, cy = h / 2f;
            if (row.Open) { var bg = E(e, null, 0f, h - 4f, it.At.width, it.At.height - h); Round(bg, 14f); bg.style.backgroundColor = BkOpenBg; }
            BkPut(e, 12f, cy, row.Time, 21f, "Jost-Medium", BkSoftHex);
            const float d = 38f; var disc = E(e, null, 70f, cy - d / 2f, d, d); Round(disc, d / 2f); disc.style.backgroundColor = row.Crucial ? BkOrange : BkDeep;
            Icon(E(disc, null, 8f, 8f, d - 16f, d - 16f), row.Icon.Length > 0 ? row.Icon : "dot", Cream);
            float x = 124f; BkPut(e, x, cy, row.Title, it.Size, "Jost-SemiBold", row.Open ? "#1f6c67" : BkInkHex); x += BkTw(row.Title, it.Size, "Jost-SemiBold") + 16f;
            if (row.Where.Length > 0 && x < it.At.width - 60f) BkPut(e, x, cy + 1f, "→ " + row.Where, 19f, "Jost-Medium", BkSoftHex);
            if (row.Unread) { var dot = E(e, null, it.At.width - 18f, cy - 5f, 10f, 10f); Round(dot, 5f); dot.style.backgroundColor = BkTurq; }
            var rule = E(e, null, 0f, h - 1f, it.At.width, 1f); rule.style.backgroundColor = BkRule;
            if (row.Open)
            {
                float y = h + 12f;
                foreach (var (face, text) in row.Lines)
                {
                    if (face.Length > 0) { var f = E(e, null, 86f, y + 4f, 30f, 30f); Round(f, 15f); f.style.backgroundColor = BkPale; Border(f, 1.5f, BkSoft); f.style.overflow = Overflow.Hidden; Bg(f, face, Color.white); }
                    BkPut(e, 130f, y + BookLayout.LineH / 2f, text, 20f, "Jost-Medium", BkInkHex); y += BookLayout.LineH;
                }
                if (row.Result.Length > 0) { var hr = E(e, null, 86f, y + 6f, it.At.width - 110f, 1f); hr.style.backgroundColor = BkRule; BkPut(e, 86f, y + 30f, row.Result, 20f, "Jost-SemiBold", BkInkHex); }
            }
            int id = row.Id; e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); if (id > 0) host.JournalRow(id); });
        }
    }
}
