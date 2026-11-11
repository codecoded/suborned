using System;
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>The page laid out on the 1920 x 1080 grid, pure and testable (hostlogic). The grid's numbers and every fit rule are Glyph's (look4/make/l4_light.py, l4_common.py).</summary>
    public static partial class BookLayout
    {
        public const float X = 1010f, W = 700f, Top = 60f, HeroX = 640f, HeroY = 70f, HeroD = 330f, TrackX = 880f, TrackW = 840f, TrackFloor = 880f, TrackD = 44f, SignX = 1830f, WayH = 96f, CanW = 730f, Grid = 1920f, GridH = 1080f;
        /// <summary>A text width in grid px: (text, size, font).</summary>
        public delegate float Measure(string text, float size, string font);
        public struct Item { public BookSlot Slot; public int Index; public Rect At; public float Size, Cy, D; }
        public sealed class Result { public readonly List<Item> Items = new(); public float Bottom, NameSize, TrackStep, TrackSize; public string[] SubLines = Array.Empty<string>(); public readonly Dictionary<int, string[]> CanLines = new(), NeedLines = new(), CrewLines = new(), StoryLines = new(); public readonly Dictionary<int, (string[][] Lines, string[] Result, string Where)> RowText = new(); public int RowsShown; public float SubSize = 30f; public int Rows; public DebriefLines Debrief; public bool Has(BookSlot s) { foreach (var i in Items) if (i.Slot == s) return true; return false; } public IEnumerable<Item> Of(BookSlot s) { foreach (var i in Items) if (i.Slot == s) yield return i; } }

        static readonly (float size, float step, float d)[] One = { (36, 60, 46), (31, 54, 42) }, Two = { (30, 52, 40), (27, 46, 36), (24, 40, 32), (22, 37, 30) };

        /// <summary>The name takes the biggest of three sizes that fits; many facts step it down once.</summary>
        public static float NameSize(string name, int facts, Measure tw)
        {
            float top = facts > 8 ? 112f * .72f : 112f;
            foreach (var s in new[] { top, top * .72f, top * .52f }) if (tw(name, s, "Jost-Bold") <= W) return s;
            return top * .4f;
        }

        /// <summary>The facts' size: 1-4 one column, 5-12 two; the size steps down until the longest row fits its column. The cap stays 12.</summary>
        public static (int cols, int rows, float size, float step, float d) FitFacts(IList<BookFact> facts, Measure tw)
        {
            int n = Mathf.Min(facts.Count, 12), cols = n <= 4 ? 1 : 2; var table = cols == 1 ? One : Two; float cw = W / cols; var pick = table[table.Length - 1];
            foreach (var t in table)
            {
                float lim = 0f; for (int i = 0; i < n; i++) { var f = facts[i]; float nw = f.Note.Length > 0 ? tw(f.Note, t.size * .76f, "Jost-Medium") + 10f : 0f; lim = Mathf.Max(lim, tw(f.Word, t.size, "Jost-SemiBold") + nw); }
                if (lim + t.d + 26f <= cw) { pick = t; break; }
            }
            return (cols, (n + cols - 1) / cols, pick.size, pick.step, pick.d);
        }

        public static Result Place(BookPage p, Measure tw)
        {
            if (p.Step >= 0) return PlaceBrief(p, tw); // brief 108: the briefing's three pages (BookLayout.Brief)
            var r = new Result(); float y = Top;
            PlaceKicker(p, r, tw);
            if (p.HeroFace.Length > 0 || p.HeroIcon.Length > 0) r.Items.Add(new Item { Slot = BookSlot.Hero, At = new Rect(HeroX, HeroY, HeroD, HeroD) });
            if (p.Name.Length > 0 && p.NameFit != null) // brief 099: the debrief's headline takes the biggest of its own sizes that fits its width (Glyph's d_parts.head)
            {
                float s = p.NameFit[^1]; foreach (var z in p.NameFit) if (tw(p.Name, z, "Jost-Bold") <= p.NameW) { s = z; break; }
                r.NameSize = s; float cy = y + s * .58f; r.Items.Add(new Item { Slot = BookSlot.Name, Cy = cy, Size = s, At = new Rect(X, cy - s * .5f, p.NameW, s) }); y = cy + 68f;
            }
            else if (p.Name.Length > 0)
            {
                float s = r.NameSize = NameSize(p.Name, p.Facts.Count, tw);
                // the name's tag (the job word beside an agent's name, "Larthi · Scout"): at about two-fifths of the name's size; the name steps down until both fit
                if (p.NameTag.Length > 0) foreach (var k in new[] { 1f, .85f, .72f, .6f }) { s = r.NameSize * k; if (tw(p.Name, s, "Jost-Bold") + 18f + tw("· " + p.NameTag, s * .42f, "Jost-Medium") <= W) break; }
                r.NameSize = s;
                r.Items.Add(new Item { Slot = BookSlot.Name, Cy = y + s * .5f, Size = s, At = new Rect(X, y, W, s) }); y += s + 16f;
            }
            if (p.Sub.Length > 0 || p.State.Length > 0)
            {
                // the sub: one line at 30, else 26, else two lines at 26 split at the word nearest the middle
                float big = p.NameFit != null ? 27f : 30f, sz = big; if (tw(p.Sub, big, "Jost-Medium") > CanW) sz = p.NameFit != null ? 27f : 26f; r.SubSize = sz; r.SubLines = new[] { p.Sub }; // brief 099: the debrief's line is 27 on one or two lines
                if (tw(p.Sub, sz, "Jost-Medium") > CanW)
                {
                    int mid = p.Sub.Length / 2, best = -1; for (int i = 0; i < p.Sub.Length; i++) if (p.Sub[i] == ' ' && (best < 0 || Mathf.Abs(i - mid) < Mathf.Abs(best - mid))) best = i;
                    if (best > 0) r.SubLines = new[] { p.Sub[..best], p.Sub[(best + 1)..] };
                }
                float lh = sz * 1.3f; if (p.Sub.Length > 0) r.Items.Add(new Item { Slot = BookSlot.Sub, Cy = y, Size = sz, At = new Rect(X, y - sz * .6f, W, lh * r.SubLines.Length) });
                if (p.State.Length > 0) { float sx = X + (p.Sub.Length > 0 ? tw(r.SubLines[r.SubLines.Length - 1], sz, "Jost-Medium") + 18f : 0f), w = tw(p.State, 30f, "Jost-Bold") + 34f; r.Items.Add(new Item { Slot = BookSlot.State, Cy = y + lh * (r.SubLines.Length - 1), Size = 30f, At = new Rect(sx, y + lh * (r.SubLines.Length - 1) - 23f, w, 46f) }); }
                y += lh * (r.SubLines.Length - 1) + 36f;
            }
            if (p.FactsHead.Length > 0 && (p.Facts.Count > 0 || p.FactsFolded)) { r.Items.Add(new Item { Slot = BookSlot.FactsHead, Cy = y + 12f, Size = 24f, At = new Rect(X, y, W, 24f) }); PlacePurse(p, r, y, tw); y += 34f; }
            else if (PlacePurse(p, r, y, tw)) y += 34f;
            if (p.Filters.Count > 0) y = PlaceFilters(p, r, y, tw);
            if (p.Facts.Count > 0)
            {
                var (cols, rows, size, step, d) = FitFacts(p.Facts, tw); float cw = W / cols; r.Rows = rows;
                for (int i = 0; i < Mathf.Min(p.Facts.Count, 12); i++) r.Items.Add(new Item { Slot = BookSlot.Facts, Index = i, Size = size, D = d, Cy = y + (i % rows) * step + d / 2f, At = new Rect(X + (i / rows) * cw, y + (i % rows) * step, cw, d) });
                y += rows * step + 6f;
            }
            if (p.Gear.Count > 0)
            {
                var (_, _, size, step, d) = FitFacts(p.Gear, tw);
                for (int i = 0; i < Mathf.Min(p.Gear.Count, 4); i++) r.Items.Add(new Item { Slot = BookSlot.Gear, Index = i, Size = size, D = d, Cy = y + i * step + d / 2f, At = new Rect(X, y + i * step, W, d) });
                y += Mathf.Min(p.Gear.Count, 4) * step + 6f;
            }
            if (p.Weapon is BookWeapon wpn)
            {
                // the weapon's line (as a fact), then its ratings in two short columns, a word and its pips each
                int rows = (wpn.Stats.Count + 1) / 2; float h = 46f + 10f + rows * 36f;
                r.Items.Add(new Item { Slot = BookSlot.Weapon, Size = 36f, D = 46f, Cy = y + 23f, At = new Rect(X, y, CanW, h) }); y += h + 8f;
            }
            if (p.Ways.Count > 0)
            {
                float cx = X;
                for (int i = 0; i < p.Ways.Count; i++)
                {
                    var w = p.Ways[i]; float ww = 100f + Mathf.Max(tw(w.Word, 40f, "Jost-Bold"), tw(w.SubText, 25f, "Jost-Medium")) + 34f;
                    if (cx + ww > X + W && cx > X) { cx = X; y += WayH + 12f; }
                    r.Items.Add(new Item { Slot = BookSlot.Ways, Index = i, Size = 40f, At = new Rect(cx, y, ww, WayH) }); cx += ww + 14f;
                }
                y += WayH + 22f;
                y = PlaceFaces(p, r, y, tw);
            }
            if (p.Needs.Count > 0)
            {
                // the conditions, under each way's sign: a column a way (its sign's x, to the next sign), rows top down, a line wrapping to two; groups with no sign stack after
                var cols = new List<(string g, float x, float w)>(); var ws = new List<Item>(); foreach (var it in r.Items) if (it.Slot == BookSlot.Ways) ws.Add(it);
                for (int i = 0; i < ws.Count; i++) { float x0 = ws[i].At.x, x1 = i + 1 < ws.Count && ws[i + 1].At.y == ws[i].At.y ? ws[i + 1].At.x - 14f : X + CanW; cols.Add((p.Ways[ws[i].Index].Verb, x0, x1 - x0)); }
                float top = y - 10f, bottom = top; int n = 0;
                foreach (var (g, cx, cw) in cols)
                {
                    float cy = top;
                    for (int i = 0; i < p.Needs.Count; i++) { if (p.Needs[i].Group != g || g == Tail) continue; var lines = WrapAny(p.Needs[i].Words, NeedSize, "Jost-Medium", cw - 30f, tw); r.NeedLines[i] = lines; float h = NeedLineH * lines.Length + 8f; r.Items.Add(new Item { Slot = BookSlot.Needs, Index = i, Size = NeedSize, D = 22f, At = new Rect(cx, cy, cw, h) }); cy += h; n++; }
                    bottom = Mathf.Max(bottom, cy);
                }
                for (int i = 0; i < p.Needs.Count; i++) if (!r.NeedLines.ContainsKey(i) && p.Needs[i].Group != Tail) { var lines = WrapAny(p.Needs[i].Words, NeedSize, "Jost-Medium", CanW - 30f, tw); r.NeedLines[i] = lines; float h = NeedLineH * lines.Length + 8f; r.Items.Add(new Item { Slot = BookSlot.Needs, Index = i, Size = NeedSize, D = 22f, At = new Rect(X, bottom, CanW, h) }); bottom += h; }
                y = bottom + 16f;
            }
            y = PlaceShared(p, r, y, tw);
            if (p.Rows.Count > 0) y = PlaceRows(p, r, y, tw);
            if (p.Scales.Count > 0) { r.Items.Add(new Item { Slot = BookSlot.Scales, Size = 25f, Cy = y + 14f, At = new Rect(X + 2f, y, W, 30f) }); y += 42f; }
            if (p.Knows.Count > 0) { r.Items.Add(new Item { Slot = BookSlot.Knows, Size = 25f, Cy = y + 14f, At = new Rect(X + 2f, y, W, 30f) }); y += 42f; }
            if (p.Can.Count > 0)
            {
                // one row a verb, one size up to 6 so the agents' pages match; 7-8 smaller, 9 and more smallest; a line wraps to at most two
                y += 10f; int n = p.Can.Count; var (ws, ls, d) = n <= 6 ? (30f, 21.5f, 42f) : n <= 8 ? (26f, 20f, 38f) : (23f, 18f, 32f); float lw = CanW - d - 14f, gap = n <= 6 ? 14f : 10f;
                for (int i = 0; i < n; i++)
                {
                    var lines = WrapAny(p.Can[i].Line, ls, "Jost-Medium", lw, tw); r.CanLines[i] = lines; float lh = ls * 1.3f, h = ws * 1.25f + lh * lines.Length; // brief 108: WrapAny, never one line past its column
                    r.Items.Add(new Item { Slot = BookSlot.Can, Index = i, Size = ws, D = d, Cy = y + ws * .6f, At = new Rect(X, y, CanW, h) }); y += h + gap;
                }
                y += 10f;
            }
            y = PlaceDebrief(p, r, y, tw); // brief 099: the debrief's rows, numbers, fold or city, and its run (BookLayout.Debrief)
            if (p.Track.Count > 0 && r.Debrief == null) // brief 099: the debrief places its own run (or its fold, in the run's place)
            {
                // one evenly spaced line, the latest six; the label size steps down until the longest fits its share
                int n = Mathf.Min(p.Track.Count, 6), from = p.Track.Count - n; float step = r.TrackStep = TrackW / n, ty = p.Crew.Count > 0 ? y + 8f : Mathf.Max(y + 2f, TrackFloor); // brief 096: with the cell strip under it, the clock follows the page
                float size = 18f; foreach (var s in new[] { 24f, 22f, 20f, 18f }) { float lim = 0f; for (int i = from; i < p.Track.Count; i++) lim = Mathf.Max(lim, tw(p.Track[i].Word, s, "Jost-SemiBold")); if (lim <= step - 8f) { size = s; break; } }
                r.TrackSize = size;
                for (int i = 0; i < n; i++) r.Items.Add(new Item { Slot = BookSlot.Track, Index = from + i, Size = size, D = TrackD, At = new Rect(TrackX + i * step, ty, step, TrackD + 70f) });
                y = ty + TrackD + 70f;
            }
            y = PlaceCrew(p, r, y, tw); PlaceBegin(p, r); PlaceAgain(p, r, tw);
            for (int i = 0; i < Mathf.Min(p.Others.Count, 3); i++) r.Items.Add(new Item { Slot = BookSlot.Others, Index = i, D = 80f, At = new Rect(22f + i * 92f, 26f, 80f, 104f) });
            r.Bottom = y; return r;
        }

        /// <summary>Words to at most two lines: one if it fits; else at a sentence's end if both parts fit, then a comma, then the most even break, so no line ends on a lone word.</summary>
        public static string[] Wrap(string text, float size, string font, float w, Measure tw)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<string>(); if (tw(text, size, font) <= w) return new[] { text };
            int best = -1; float bestCost = float.MaxValue;
            for (int i = text.IndexOf(' '); i > 0; i = text.IndexOf(' ', i + 1))
            {
                float l = tw(text[..i], size, font), r = tw(text[(i + 1)..], size, font); if (l > w || r > w) continue;
                float cost = Mathf.Abs(l - r) * (text[i - 1] == '.' ? .05f : text[i - 1] is ',' or ';' ? .1f : 1f); if (cost < bestCost) { bestCost = cost; best = i; }
            }
            return best > 0 ? new[] { text[..best], text[(best + 1)..] } : new[] { text };
        }

        /// <summary>The section signs re-space from the list alone: centre y of sign i of n; 880 grid px share them.</summary>
        public static float SignY(int i, int n) => 110f + (i + .5f) * (880f / Mathf.Max(n, 1));
    }
}
