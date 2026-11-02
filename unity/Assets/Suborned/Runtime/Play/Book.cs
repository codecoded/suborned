using System;
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 088, the book's page as data: named slots in a fixed order, no positions. Glyph's rule (briefs/085/glyph/book/look3/notes.md, look4/notes.md):
    /// empty slots take no room, and the layout never knows what the content is. `Gear` and `Scales` carry our own agents' kit and stats (Sky, 2026-11-02), so wounds,
    /// gear and what a person gives need no redesign: more rows, the same code.</summary>
    public enum BookSlot { Hero, Name, Sub, State, FactsHead, Facts, Gear, Ways, Scales, Knows, Track, Others }

    public sealed class BookFact { public string Icon = "", Word = "", Note = ""; public bool Unknown; }
    public sealed class BookField { public string Icon = "", Text = ""; }
    /// <summary>A way on a page. It gives no order: a click opens the ring on `Target` for `By` (-1: the selection as it is), so the price shows first.
    /// Its price and lock are the ring's own words (HandoverHost.BookWayFrom).</summary>
    public sealed class BookWay
    {
        public string Icon = "", Word = "", Verb = "", Named = ""; public int By = -1; public bool Shut; public string LockCode = ""; public HandoverHost.Target Target;
        public readonly List<BookField> Sub = new();
        public string SubText { get { var p = new List<string>(); if (Named.Length > 0) p.Add(Named); foreach (var f in Sub) p.Add(f.Text); return string.Join(" · ", p); } }
    }
    public sealed class BookScale { public string Word = ""; public int Value, Max; }
    public sealed class BookKnow { public string Icon = "", Face = "", Word = ""; }
    public sealed class BookStop { public string Icon = "", Word = "", Time = ""; public bool Past; }
    public sealed class BookOther { public string Face = "", Word = ""; public int Agent = -1; }

    public sealed class BookPage
    {
        public string Section = "", HeroFace = "", HeroIcon = "", Name = "", Sub = "", State = "", FactsHead = "";
        public readonly List<BookFact> Facts = new(), Gear = new();
        public readonly List<BookWay> Ways = new();
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
        public const float X = 1010f, W = 700f, Top = 60f, HeroX = 640f, HeroY = 70f, HeroD = 330f, TrackX = 880f, TrackW = 840f, TrackFloor = 880f, TrackD = 44f, SignX = 1830f, WayH = 96f, Grid = 1920f, GridH = 1080f;
        /// <summary>A text width in grid px: (text, size, font).</summary>
        public delegate float Measure(string text, float size, string font);
        public struct Item { public BookSlot Slot; public int Index; public Rect At; public float Size, Cy, D; }
        public sealed class Result { public readonly List<Item> Items = new(); public float Bottom, NameSize, TrackStep, TrackSize; public string[] SubLines = Array.Empty<string>(); public float SubSize = 30f; public int Rows; public bool Has(BookSlot s) { foreach (var i in Items) if (i.Slot == s) return true; return false; } public IEnumerable<Item> Of(BookSlot s) { foreach (var i in Items) if (i.Slot == s) yield return i; } }

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
                float sz = 30f; if (tw(p.Sub, 30f, "Jost-Medium") > W) sz = 26f; r.SubSize = sz; r.SubLines = new[] { p.Sub };
                if (tw(p.Sub, sz, "Jost-Medium") > W)
                {
                    int mid = p.Sub.Length / 2, best = -1; for (int i = 0; i < p.Sub.Length; i++) if (p.Sub[i] == ' ' && (best < 0 || Mathf.Abs(i - mid) < Mathf.Abs(best - mid))) best = i;
                    if (best > 0) r.SubLines = new[] { p.Sub[..best], p.Sub[(best + 1)..] };
                }
                float lh = sz * 1.3f; if (p.Sub.Length > 0) r.Items.Add(new Item { Slot = BookSlot.Sub, Cy = y, Size = sz, At = new Rect(X, y - sz * .6f, W, lh * r.SubLines.Length) });
                if (p.State.Length > 0) { float sx = X + (p.Sub.Length > 0 ? tw(r.SubLines[r.SubLines.Length - 1], sz, "Jost-Medium") + 18f : 0f), w = tw(p.State, 30f, "Jost-Bold") + 34f; r.Items.Add(new Item { Slot = BookSlot.State, Cy = y + lh * (r.SubLines.Length - 1), Size = 30f, At = new Rect(sx, y + lh * (r.SubLines.Length - 1) - 23f, w, 46f) }); }
                y += lh * (r.SubLines.Length - 1) + 36f;
            }
            if (p.Facts.Count > 0 && p.FactsHead.Length > 0) { r.Items.Add(new Item { Slot = BookSlot.FactsHead, Cy = y + 12f, Size = 24f, At = new Rect(X, y, W, 24f) }); y += 34f; }
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
            if (p.Ways.Count > 0)
            {
                float cx = X;
                for (int i = 0; i < p.Ways.Count; i++)
                {
                    var w = p.Ways[i]; float ww = 100f + Mathf.Max(tw(w.Word, 40f, "Jost-Bold"), tw(w.SubText, 25f, "Jost-Medium") + 22f * w.Sub.Count) + 34f;
                    if (cx + ww > X + W && cx > X) { cx = X; y += WayH + 12f; }
                    r.Items.Add(new Item { Slot = BookSlot.Ways, Index = i, Size = 40f, At = new Rect(cx, y, ww, WayH) }); cx += ww + 14f;
                }
                y += WayH + 22f;
            }
            if (p.Scales.Count > 0) { r.Items.Add(new Item { Slot = BookSlot.Scales, Size = 25f, Cy = y + 14f, At = new Rect(X + 2f, y, W, 30f) }); y += 42f; }
            if (p.Knows.Count > 0) { r.Items.Add(new Item { Slot = BookSlot.Knows, Size = 25f, Cy = y + 14f, At = new Rect(X + 2f, y, W, 30f) }); y += 42f; }
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

        /// <summary>The section signs re-space from the list alone: centre y of sign i of n; 880 grid px share them.</summary>
        public static float SignY(int i, int n) => 110f + (i + .5f) * (880f / Mathf.Max(n, 1));
    }
}
