using System;
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 088, the book's page as data: named slots in a fixed order, no positions. Glyph's rule (briefs/085/glyph/book/look3/notes.md, look4/notes.md):
    /// empty slots take no room, and the layout never knows what the content is. `Gear` and `Scales` carry our own agents' kit and stats (Sky, 2026-11-02), so wounds,
    /// gear and what a person gives need no redesign: more rows, the same code.</summary>
    public enum BookSlot { Hero, Name, Sub, State, FactsHead, Facts, Gear, Weapon, Ways, Needs, Scales, Knows, Can, Track, Others }

    public sealed class BookFact { public string Icon = "", Word = "", Note = ""; public bool Unknown; }
    public sealed class BookField { public string Icon = "", Text = ""; }
    /// <summary>A way on a page: only where the mission bounds them (the job's ways in). It gives no order: a click opens the ring on `Target`, so the price shows first.</summary>
    public sealed class BookWay
    {
        public string Icon = "", Word = "", Verb = ""; public bool Shut; public HandoverHost.Target Target;
        public readonly List<BookField> Sub = new();
        public string SubText { get { var p = new List<string>(); foreach (var f in Sub) p.Add(f.Text); return string.Join(" · ", p); } }
    }
    /// <summary>What someone can do in general: a verb and its one line. No target, price, time or lock, and no click: the ring is the only place to act. The rows depend only on who it is.</summary>
    public sealed class BookCan { public string Id = "", Icon = "", Word = "", Line = ""; }
    /// <summary>A weapon at a glance (Sky on 0.2.226: "too specific"): its word, drawn or not, and a few ratings as pips, each the engine's value against a scale held as
    /// data (book.cell.weapon.stats). The fire chip gives a real shot's exact chance; the book only compares. Any weapon fits by data.</summary>
    public sealed class BookWeapon
    {
        public string Id = "", Word = "", Note = "";
        public readonly List<(string id, string word, int pips, int of, float value)> Stats = new();
    }
    /// <summary>A condition a way in needs (book.job.needs, data): its words, the way it belongs to, and its state now: Met, not yet, or Unknown ("?") while the cell hasn't learned what it rests on.
    /// It names no method: any act that makes it true ticks it.</summary>
    public sealed class BookNeed { public string Id = "", Group = "", Words = ""; public bool Met, Unknown; }
    public sealed class BookScale { public string Word = ""; public int Value, Max; }
    public sealed class BookKnow { public string Icon = "", Face = "", Word = ""; }
    public sealed class BookStop { public string Icon = "", Word = "", Time = ""; public bool Past; }
    public sealed class BookOther { public string Face = "", Word = ""; public int Agent = -1; }

    public sealed class BookPage
    {
        public string Section = "", HeroFace = "", HeroIcon = "", Name = "", Sub = "", State = "", FactsHead = "";
        public readonly List<BookFact> Facts = new(), Gear = new();
        public readonly List<BookWay> Ways = new();
        public readonly List<BookCan> Can = new();
        public BookWeapon Weapon;
        public readonly List<BookNeed> Needs = new();
        /// <summary>The facts fold under their head: shut, the head alone says how many (FactsCount) and opens them.</summary>
        public bool FactsFolded; public int FactsCount;
        public readonly List<BookScale> Scales = new();
        public readonly List<BookKnow> Knows = new();
        public readonly List<BookStop> Track = new();
        public readonly List<BookOther> Others = new();
        /// <summary>What the frozen street shows beside the page, its world point; Line: the thin line from the face to that person (people, never a place they aren't).</summary>
        public Vector3? Subject; public bool Line; public float? SubjectYaw; public string SubjectView = "";
    }

    /// <summary>One section sign: an entry in the list is all a section takes (HandoverHost.BookSections); no Page: a dim sign with no page.</summary>
    public sealed class BookSection
    {
        public readonly string Id, Icon, Word; public readonly Func<HandoverHost, BookPage> Page;
        public BookSection(string id, string icon, string word, Func<HandoverHost, BookPage> page = null) { Id = id; Icon = icon; Word = word; Page = page; }
    }

    /// <summary>The page laid out on the 1920 x 1080 grid, pure and testable (hostlogic). The grid's numbers and every fit rule are Glyph's (look4/make/l4_light.py, l4_common.py).</summary>
    public static class BookLayout
    {
        public const float X = 1010f, W = 700f, Top = 60f, HeroX = 640f, HeroY = 70f, HeroD = 330f, TrackX = 880f, TrackW = 840f, TrackFloor = 880f, TrackD = 44f, SignX = 1830f, WayH = 96f, CanW = 730f, Grid = 1920f, GridH = 1080f;
        /// <summary>A text width in grid px: (text, size, font).</summary>
        public delegate float Measure(string text, float size, string font);
        public struct Item { public BookSlot Slot; public int Index; public Rect At; public float Size, Cy, D; }
        public sealed class Result { public readonly List<Item> Items = new(); public float Bottom, NameSize, TrackStep, TrackSize; public string[] SubLines = Array.Empty<string>(); public readonly Dictionary<int, string[]> CanLines = new(), NeedLines = new(); public float SubSize = 30f; public int Rows; public bool Has(BookSlot s) { foreach (var i in Items) if (i.Slot == s) return true; return false; } public IEnumerable<Item> Of(BookSlot s) { foreach (var i in Items) if (i.Slot == s) yield return i; } }

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
            var r = new Result(); float y = Top;
            if (p.HeroFace.Length > 0 || p.HeroIcon.Length > 0) r.Items.Add(new Item { Slot = BookSlot.Hero, At = new Rect(HeroX, HeroY, HeroD, HeroD) });
            if (p.Name.Length > 0)
            {
                float s = r.NameSize = NameSize(p.Name, p.Facts.Count, tw);
                r.Items.Add(new Item { Slot = BookSlot.Name, Cy = y + s * .5f, Size = s, At = new Rect(X, y, W, s) }); y += s + 16f;
            }
            if (p.Sub.Length > 0 || p.State.Length > 0)
            {
                // the sub: one line at 30, else 26, else two lines at 26 split at the word nearest the middle
                float sz = 30f; if (tw(p.Sub, 30f, "Jost-Medium") > CanW) sz = 26f; r.SubSize = sz; r.SubLines = new[] { p.Sub };
                if (tw(p.Sub, sz, "Jost-Medium") > CanW)
                {
                    int mid = p.Sub.Length / 2, best = -1; for (int i = 0; i < p.Sub.Length; i++) if (p.Sub[i] == ' ' && (best < 0 || Mathf.Abs(i - mid) < Mathf.Abs(best - mid))) best = i;
                    if (best > 0) r.SubLines = new[] { p.Sub[..best], p.Sub[(best + 1)..] };
                }
                float lh = sz * 1.3f; if (p.Sub.Length > 0) r.Items.Add(new Item { Slot = BookSlot.Sub, Cy = y, Size = sz, At = new Rect(X, y - sz * .6f, W, lh * r.SubLines.Length) });
                if (p.State.Length > 0) { float sx = X + (p.Sub.Length > 0 ? tw(r.SubLines[r.SubLines.Length - 1], sz, "Jost-Medium") + 18f : 0f), w = tw(p.State, 30f, "Jost-Bold") + 34f; r.Items.Add(new Item { Slot = BookSlot.State, Cy = y + lh * (r.SubLines.Length - 1), Size = 30f, At = new Rect(sx, y + lh * (r.SubLines.Length - 1) - 23f, w, 46f) }); }
                y += lh * (r.SubLines.Length - 1) + 36f;
            }
            if (p.FactsHead.Length > 0 && (p.Facts.Count > 0 || p.FactsFolded)) { r.Items.Add(new Item { Slot = BookSlot.FactsHead, Cy = y + 12f, Size = 24f, At = new Rect(X, y, W, 24f) }); y += 34f; }
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
                    for (int i = 0; i < p.Needs.Count; i++) { if (p.Needs[i].Group != g) continue; var lines = Wrap(p.Needs[i].Words, 21f, "Jost-Medium", cw - 34f, tw); r.NeedLines[i] = lines; float h = 27f * lines.Length + 8f; r.Items.Add(new Item { Slot = BookSlot.Needs, Index = i, Size = 21f, D = 22f, At = new Rect(cx, cy, cw, h) }); cy += h; n++; }
                    bottom = Mathf.Max(bottom, cy);
                }
                for (int i = 0; i < p.Needs.Count; i++) if (!r.NeedLines.ContainsKey(i)) { var lines = Wrap(p.Needs[i].Words, 21f, "Jost-Medium", CanW - 34f, tw); r.NeedLines[i] = lines; float h = 27f * lines.Length + 8f; r.Items.Add(new Item { Slot = BookSlot.Needs, Index = i, Size = 21f, D = 22f, At = new Rect(X, bottom, CanW, h) }); bottom += h; }
                y = bottom + 16f;
            }
            if (p.Scales.Count > 0) { r.Items.Add(new Item { Slot = BookSlot.Scales, Size = 25f, Cy = y + 14f, At = new Rect(X + 2f, y, W, 30f) }); y += 42f; }
            if (p.Knows.Count > 0) { r.Items.Add(new Item { Slot = BookSlot.Knows, Size = 25f, Cy = y + 14f, At = new Rect(X + 2f, y, W, 30f) }); y += 42f; }
            if (p.Can.Count > 0)
            {
                // one row a verb, one size up to 6 so the agents' pages match; 7-8 smaller, 9 and more smallest; a line wraps to at most two
                y += 10f; int n = p.Can.Count; var (ws, ls, d) = n <= 6 ? (30f, 21.5f, 42f) : n <= 8 ? (26f, 20f, 38f) : (23f, 18f, 32f); float lw = CanW - d - 14f, gap = n <= 6 ? 14f : 10f;
                for (int i = 0; i < n; i++)
                {
                    var lines = Wrap(p.Can[i].Line, ls, "Jost-Medium", lw, tw); r.CanLines[i] = lines; float lh = ls * 1.3f, h = ws * 1.25f + lh * lines.Length;
                    r.Items.Add(new Item { Slot = BookSlot.Can, Index = i, Size = ws, D = d, Cy = y + ws * .6f, At = new Rect(X, y, CanW, h) }); y += h + gap;
                }
                y += 10f;
            }
            if (p.Track.Count > 0)
            {
                // one evenly spaced line, the latest six; the label size steps down until the longest fits its share
                int n = Mathf.Min(p.Track.Count, 6), from = p.Track.Count - n; float step = r.TrackStep = TrackW / n, ty = Mathf.Max(y + 2f, TrackFloor);
                float size = 18f; foreach (var s in new[] { 24f, 22f, 20f, 18f }) { float lim = 0f; for (int i = from; i < p.Track.Count; i++) lim = Mathf.Max(lim, tw(p.Track[i].Word, s, "Jost-SemiBold")); if (lim <= step - 8f) { size = s; break; } }
                r.TrackSize = size;
                for (int i = 0; i < n; i++) r.Items.Add(new Item { Slot = BookSlot.Track, Index = from + i, Size = size, D = TrackD, At = new Rect(TrackX + i * step, ty, step, TrackD + 70f) });
                y = ty + TrackD + 70f;
            }
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
