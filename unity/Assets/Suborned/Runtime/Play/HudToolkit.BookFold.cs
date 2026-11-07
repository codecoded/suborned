using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 099, a number's explainer as the book's fold (Glyph's A-front-detail.png, d_parts.fold_trail): a slightly darker sheet with a hairline and a tail up to the
    /// number that opened it; its head and what it means; one bar of every point (the hot share in the orange); each thing that moved it, biggest first, with the faces of who
    /// did it, the amount and when; the rule under them. The rest of the page steps back while it is open; a click on the page outside it shuts it. The sheet and the hairline are
    /// the book's own ink laid thin over its paper, not new colours.</summary>
    public sealed partial class HudToolkit
    {
        static readonly Color BkFoldSheet = new(0x17 / 255f, 0x30 / 255f, 0x2f / 255f, 0.045f), BkShadow = new(0f, 0f, 0f, 0.09f);

        VisualElement BkFoldDraw(VisualElement body, BookFoldView f, BookLayout.Item it, BookLayout.DebriefLines dl)
        {
            var at = it.At; float tail = it.Cy, pip = it.D; int rows = dl.FoldRows; bool below = dl.FoldBelow;
            var shade = E(body, null, at.x + 3f, at.y + 5f, at.width, at.height); Round(shade, 22f); shade.style.backgroundColor = BkShadow;
            var e = BkBox(body, at, true); e.name = "debrief-fold"; Round(e, 22f); e.style.backgroundColor = new Color(BkPaper.r, BkPaper.g, BkPaper.b, 1f);
            var sheet = E(e, null, 0f, 0f, at.width, at.height); Round(sheet, 22f); sheet.style.backgroundColor = BkFoldSheet; Border(sheet, 1.5f, BkRule);
            var tip = P(e, tail - at.x - 17f, below ? -16f : at.height - 2f, 34f, 18f, (p, r) => // the tail toward the number (up, or down when the fold opens over the band): the sheet's colours, its hairline on the slanted sides
            {
                float Y(float v) => below ? v : 18f - v;
                p.fillColor = new Color(BkPaper.r * (1f - BkFoldSheet.a) + BkFoldSheet.r * BkFoldSheet.a, BkPaper.g * (1f - BkFoldSheet.a) + BkFoldSheet.g * BkFoldSheet.a, BkPaper.b * (1f - BkFoldSheet.a) + BkFoldSheet.b * BkFoldSheet.a, 1f);
                p.BeginPath(); p.MoveTo(new Vector2(1f, Y(17.5f))); p.LineTo(new Vector2(17f, Y(1.5f))); p.LineTo(new Vector2(33f, Y(17.5f))); p.ClosePath(); p.Fill();
                p.strokeColor = BkRule; p.lineWidth = 1.5f; p.BeginPath(); p.MoveTo(new Vector2(1f, Y(17f))); p.LineTo(new Vector2(17f, Y(1f))); p.LineTo(new Vector2(33f, Y(17f))); p.Stroke();
            });
            tip.pickingMode = PickingMode.Ignore;
            float x = 30f, y = 36f; BkPut(e, x, y, f.Title, 34f, "Jost-Bold", BkInkHex); if (f.Head.Length > 0) BkPut(e, x + 12f + BkTw(f.Title, 34f, "Jost-Bold"), y + 2f, f.Head, 21f, "Jost-Medium", BkSoftHex);
            if (f.Means.Length > 0) BkPut(e, x, y + 32f, f.Means, 19f, "Jost-Medium", BkSoftHex);
            y += 32f + 20f;
            if (pip > 0f)
            {
                int total = 0; foreach (var (n, _) in f.Pips) total += n; float ps = Mathf.Min(16f, (at.width - 60f - 8f * f.Pips.Count) / Mathf.Max(total, 1) - 4f), cx = x;
                foreach (var (n, hot) in f.Pips) { for (int k = 0; k < n; k++) { var d = E(e, null, cx, y + 4f, ps, ps); Round(d, ps / 2f); d.style.backgroundColor = hot ? BkOrange : BkDeep; cx += ps + 4f; } cx += 8f; }
                y += pip;
            }
            y += 22f;
            for (int i = 0; i < rows; i++)
            {
                var row = dl.FoldRest && i == rows - 1 ? new BookFoldRow { Icon = "p_trail", Value = dl.FoldRestValue, Words = f.Rest } : f.Rows[dl.FoldShown[i]]; float cy = y + i * BookLayout.FoldRowH; // the last row sums what did not fit
                if (row.Faces.Count > 0) for (int k = 0; k < Mathf.Min(row.Faces.Count, 2); k++) { var fc = E(e, null, x - 2f + k * 26f, cy - 18f, 36f, 36f); Round(fc, 18f); fc.style.backgroundColor = BkPale; fc.style.overflow = Overflow.Hidden; Border(fc, 2f, BkDeep); Bg(fc, row.Faces[k], Color.white); }
                else { var dc = E(e, null, x - 2f, cy - 18f, 36f, 36f); Round(dc, 18f); dc.style.backgroundColor = BkDeep; Icon(E(dc, null, 7f, 7f, 22f, 22f), row.Icon.Length > 0 ? row.Icon : "p_trail", Cream); }
                BkPut(e, 150f - BkTw(row.Value, 26f, "Jost-Bold"), cy, row.Value, 26f, "Jost-Bold", row.Hot ? "#e8622c" : BkInkHex);
                BkPut(e, 166f, cy, row.Words, 22f, "Jost-SemiBold", BkInkHex);
                if (row.When.Length > 0) BkPut(e, 178f + BkTw(row.Words, 22f, "Jost-SemiBold"), cy + 1f, row.When, 19f, "Jost-Medium", BkSoftHex);
            }
            if (f.Law.Length > 0) { float ly = y + rows * BookLayout.FoldRowH - 17f; var hr = E(e, null, x, ly, at.width - 60f, 1.5f); hr.style.backgroundColor = BkRule; BkPut(e, x, ly + 20f, f.Law, 18.5f, "Jost-Medium", BkSoftHex); }
            e.RegisterCallback<ClickEvent>(ev => ev.StopPropagation()); // a click inside the fold keeps it open
            return e;
        }

        /// <summary>While a fold is open the rest of the page steps back (the page's own paper laid over it, the face's disc with it); `keep` comes back to the front. A click on it shuts the fold.</summary>
        void BkFoldVeil(VisualElement body, List<VisualElement> keep)
        {
            var veil = P(body, -BkPad, -BkPad, BookLayout.Grid + 2 * BkPad, BookLayout.GridH + 2 * BkPad, (p, r) =>
            {
                var c = new Vector2(2220f + BkPad, 540f + BkPad); p.fillColor = new Color(BkPaper.r, BkPaper.g, BkPaper.b, .66f);
                p.BeginPath(); for (int i = 0; i <= 180; i++) { float a = i * Mathf.PI * 2f / 180; var q = c + new Vector2(Mathf.Cos(a) * 1480f, Mathf.Sin(a) * 1440f); if (i == 0) p.MoveTo(q); else p.LineTo(q); } p.ClosePath(); p.Fill();
                p.BeginPath(); p.Arc(new Vector2(BookLayout.HeroX + BookLayout.HeroD / 2f + BkPad, BookLayout.HeroY + BookLayout.HeroD / 2f + BkPad), BookLayout.HeroD / 2f + 3f, Deg(0), Deg(360)); p.Fill();
            });
            veil.name = "debrief-veil"; veil.pickingMode = PickingMode.Position; veil.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); host.DebriefFold(null); });
            foreach (var k in keep) k?.BringToFront();
        }
    }
}
