using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 096, the book's new pieces on the same 1920 x 1080 grid (Glyph's mocks briefs/096/glyph: 1-start-job, 3c-journal; the numbers are read off them):
    /// the kicker over the name, the purse and Heat right of the folded ways to fail, the faces under each way, the shared end and its conditions, the cell strip
    /// under the clock, Begin, the Journal's filters and rows. Pure, as the rest of BookLayout: empty slots take no room.</summary>
    public static partial class BookLayout
    {
        /// <summary>The group of a condition every way shares after the first shared one (it sits under the shared line, not under a way).</summary>
        public const string Tail = "*";
        public const float NeedSize = 20f, NeedLineH = 26f, FaceD = 46f, CrewX = 880f, CrewStep = 200f, CrewW = 186f, CrewD = 64f, RowX = 960f, RowW = 800f, RowH = 54f, LineH = 38f, RowsBottom = 1050f;
        public static readonly Rect BeginAt = new(1685f, 987f, 190f, 68f);

        static void PlaceKicker(BookPage p, Result r, Measure tw)
        {
            if (p.Kicker.Length > 0) r.Items.Add(new Item { Slot = BookSlot.Kicker, Cy = 52f, Size = 19f, At = new Rect(X, 40f, W, 24f) });
        }

        /// <summary>The purse and Heat on the fold's row, right-aligned to the page's text column; true when placed.</summary>
        static bool PlacePurse(BookPage p, Result r, float y, Measure tw)
        {
            if (p.Purse.Length == 0 && p.Heat.Length == 0) return false;
            float w = 0f;
            if (p.Purse.Length > 0) w += 34f + tw(p.Purse, 24f, "Jost-SemiBold") + 32f;
            if (p.Heat.Length > 0) w += tw(p.HeatWord, 24f, "Jost-Medium") + 8f + 30f + tw(p.Heat, 24f, "Jost-SemiBold");
            r.Items.Add(new Item { Slot = BookSlot.Purse, Cy = y + 12f, Size = 24f, At = new Rect(X + CanW - w, y, w, 24f) });
            return true;
        }

        /// <summary>The Journal's filter chips, one row.</summary>
        static float PlaceFilters(BookPage p, Result r, float y, Measure tw)
        {
            float x = X - 10f;
            for (int i = 0; i < p.Filters.Count; i++) { float w = tw(p.Filters[i].Word, 19f, "Jost-SemiBold") + 36f; r.Items.Add(new Item { Slot = BookSlot.Filters, Index = i, Size = 19f, At = new Rect(x, y, w, 36f) }); x += w + 10f; }
            return y + 56f;
        }

        /// <summary>Under each way sign, its faces in a row (a cameo, then the name); Index = way * 16 + face.</summary>
        static float PlaceFaces(BookPage p, Result r, float y, Measure tw)
        {
            bool any = false;
            foreach (var it in new System.Collections.Generic.List<Item>(r.Of(BookSlot.Ways))) // a copy: the faces join the same list
            {
                var w = p.Ways[it.Index]; float x = it.At.x;
                for (int k = 0; k < Mathf.Min(w.Faces.Count, 15); k++)
                {
                    float fw = FaceD + 10f + tw(w.Faces[k].Word, 21f, "Jost-SemiBold");
                    r.Items.Add(new Item { Slot = BookSlot.Faces, Index = it.Index * 16 + k, Size = 21f, D = FaceD, Cy = y - 8f + FaceD / 2f, At = new Rect(x, y - 8f, fw, FaceD) }); x += fw + 16f; any = true;
                }
            }
            return any ? y + FaceD + 6f : y;
        }

        /// <summary>The line where both ways end, then the conditions they share in two columns (top down, the left first).</summary>
        static float PlaceShared(BookPage p, Result r, float y, Measure tw)
        {
            if (p.Shared.Length == 0) return y;
            float sz = 24f; while (sz > 18f && tw(p.Shared, sz, "Jost-SemiBold") > CanW + 20f - 52f) sz -= 1f; // clear of the rail: the line steps down until it fits the column
            r.Items.Add(new Item { Slot = BookSlot.Shared, Cy = y + 20f, Size = sz, D = 38f, At = new Rect(X - 10f, y, CanW + 20f, 40f) }); y += 52f;
            var tail = new System.Collections.Generic.List<int>(); for (int i = 0; i < p.Needs.Count; i++) if (p.Needs[i].Group == Tail) tail.Add(i);
            if (tail.Count == 0) return y + 4f;
            int half = (tail.Count + 1) / 2; float cw = (CanW + 20f) / 2f, top = y, bottom = y;
            for (int c = 0; c < 2; c++)
            {
                float cy = top;
                for (int k = c * half; k < Mathf.Min(tail.Count, (c + 1) * half); k++)
                {
                    int i = tail[k]; var lines = Wrap(p.Needs[i].Words, NeedSize, "Jost-Medium", cw - 42f, tw); r.NeedLines[i] = lines; float h = NeedLineH * lines.Length + 8f;
                    r.Items.Add(new Item { Slot = BookSlot.Needs, Index = i, Size = NeedSize, D = 22f, At = new Rect(X - 10f + c * cw, cy, cw, h) }); cy += h;
                }
                bottom = Mathf.Max(bottom, cy);
            }
            return bottom + 10f;
        }

        /// <summary>The Journal: a row each from RowsFrom, as many as fit; an open row takes its lines and result under it.</summary>
        static float PlaceRows(BookPage p, Result r, float y, Measure tw)
        {
            int shown = 0;
            for (int i = Mathf.Clamp(p.RowsFrom, 0, p.Rows.Count - 1); i < p.Rows.Count; i++)
            {
                var row = p.Rows[i]; float h = RowH + (row.Open ? 16f + row.Lines.Count * LineH + (row.Result.Length > 0 ? 48f : 0f) + 12f : 0f);
                if (y + h > RowsBottom && shown > 0) break;
                r.Items.Add(new Item { Slot = BookSlot.Rows, Index = i, Size = 23f, Cy = y + RowH / 2f, At = new Rect(RowX, y, RowW, h) }); y += h; shown++;
            }
            r.RowsShown = shown; return y;
        }

        /// <summary>The cell strip under the clock: four columns of face, name, job word and a line wrapped to two.</summary>
        static float PlaceCrew(BookPage p, Result r, float y, Measure tw)
        {
            if (p.Crew.Count == 0) return y;
            y += 22f; // the clock's hairline sits halfway
            int most = 1; for (int i = 0; i < Mathf.Min(p.Crew.Count, 4); i++) { r.CrewLines[i] = WrapAny(p.Crew[i].Line, 17f, "Jost-Medium", CrewW, tw); most = Mathf.Max(most, r.CrewLines[i].Length); }
            float h = CrewD + 16f + 21f * most;
            for (int i = 0; i < Mathf.Min(p.Crew.Count, 4); i++) r.Items.Add(new Item { Slot = BookSlot.Crew, Index = i, Size = 22f, D = CrewD, At = new Rect(CrewX + i * CrewStep, y, CrewW, h) });
            return y + h;
        }

        /// <summary>Wrap's two balanced lines when they fit; else greedy lines, as many as it takes (a long line never runs into its neighbour).</summary>
        static string[] WrapAny(string text, float size, string font, float w, Measure tw)
        {
            var two = Wrap(text, size, font, w, tw); bool fits = true; foreach (var l in two) fits &= tw(l, size, font) <= w; if (fits) return two;
            var lines = new System.Collections.Generic.List<string>(); var cur = "";
            foreach (var word in text.Split(' ')) { var t = cur.Length == 0 ? word : cur + " " + word; if (cur.Length > 0 && tw(t, size, font) > w) { lines.Add(cur); cur = word; } else cur = t; }
            if (cur.Length > 0) lines.Add(cur); return lines.ToArray();
        }

        static void PlaceBegin(BookPage p, Result r) { if (p.Begin) r.Items.Add(new Item { Slot = BookSlot.Begin, Size = 34f, At = BeginAt }); }
    }
}
