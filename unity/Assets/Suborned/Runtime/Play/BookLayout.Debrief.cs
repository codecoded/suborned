using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 099, the debrief's pieces on the book's 1920 x 1080 grid (Sky's pick, concept A: briefs/099/glyph/A-front-*.png; the numbers are Glyph's renderer's,
    /// briefs/099/glyph/make/d_parts.py and d_base.py): the way taken and its start-page rows answered, a fail's next time, the five numbers, a number's fold, what the city
    /// does now, the run along the foot and the ways on. Pure, as the rest of BookLayout: an empty slot takes no room, and nothing here knows what a row says.</summary>
    public static partial class BookLayout
    {
        /// <summary>The debrief's band (the numbers, the fold, the city, the run) and its text column's right edge; the numbers' highest top; the run's highest line; the floor every piece
        /// but the run stays above (the ways on); the run's columns keep RunGap from every way on instead, each at its own depth (the new-or-same choice stands higher than the rest).</summary>
        public const float DX = 880f, DX1 = 1745f, StatTop = 470f, RunTop = 790f, RunD = 44f, AgainFloor = 952f, FoldFloor = 960f, FoldRowH = 36f, RunGap = 4f;
        public static readonly float[] StatSizes = { 48f, 41f, 35.5f };
        /// <summary>The run's label sizes: on two lines where a label wants it (Glyph's three), then on one line, down to the page's smallest words (the chips' 17); the fewest stops before the run gives way.</summary>
        static readonly float[] RunTwo = { 21f, 19.5f, 18f }, RunOne = { 21f, 19.5f, 18f, 17f }; const int RunMin = 3;

        /// <summary>The page's own extras in the result: a number's value size and its reason's lines, a city row's lines, a run label's lines, the fold's rows shown and its side.</summary>
        public sealed class DebriefLines { public readonly Dictionary<int, string[]> Why = new(), CityHead = new(), CityLine = new(), Run = new(), Next = new(); public readonly Dictionary<int, float> TickWords = new(); public int FoldRows, TicksShown; public readonly List<int> FoldShown = new(); public bool FoldBelow = true, FoldRest; public string FoldRestValue = ""; public float RunSize = 21f, TickSize = 22f, BandH = 162f; }

        /// <summary>The way taken and its rows, a fail's next time, the numbers, then the fold (when one is open) or the city and the run. A page that would reach the ways on gives
        /// way in order: the spare rows (after the one that lost it) from the last, then the rows closer and smaller, then the run (its labels on one line, smaller or on fewer
        /// stops, then no run). It is the words' size that decides, never today's words: any label in its slot fits (debrief.schema.ts). Returns the bottom.</summary>
        static float PlaceDebrief(BookPage p, Result r, float y, Measure tw)
        {
            if (p.WayWord.Length == 0 && p.Ticks.Count == 0 && p.Stats.Count == 0) return y;
            int mark = r.Items.Count, spare = 0; for (int i = p.Ticks.Count - 1; i >= 0 && p.Ticks[i].Spare; i--) spare++;
            var ways = Ways(p, tw); var runs = RunShapes(p, tw); float bottom = y;
            for (int pass = 0, last = spare + runs.Count; pass <= last; pass++)
            {
                r.Items.RemoveRange(mark, r.Items.Count - mark);
                bottom = PlaceDebriefOnce(p, r, y, tw, p.Ticks.Count - Mathf.Min(pass, spare), pass > spare, runs[Mathf.Max(0, pass - spare - 1)], ways, out bool fits);
                if (fits) break;
            }
            return bottom;
        }

        static float PlaceDebriefOnce(BookPage p, Result r, float y, Measure tw, int shown, bool tight, RunFit run, List<Item> ways, out bool fits)
        {
            var d = r.Debrief = new DebriefLines(); float x0 = X, w = DX1 - X, top = y; d.TicksShown = shown;
            if (p.WayWord.Length > 0) { float cy = y + 32f; r.Items.Add(new Item { Slot = BookSlot.WayIn, Cy = cy, Size = 28f, D = 34f, At = new Rect(x0, cy - 17f, w, 34f) }); y = cy + 44f; }
            // the rows: 40 apart up to five, closer and a size smaller past that (the front door's seven still leave the numbers and the run their room), closer still when tight
            bool many = shown > 5; float step = tight ? 28f : many ? 33f : 40f; d.TickSize = tight ? 19f : many ? 20f : 22f;
            for (int i = 0; i < shown; i++)
            {
                // the note keeps its size, right-aligned; the words step down until they clear it (Glyph's d_parts.conds)
                var t = p.Ticks[i]; float nw = tw(t.Note, d.TickSize - 2f, t.Lost ? "Jost-SemiBold" : "Jost-Medium"), ww = w - 44f - nw - 18f, ws = d.TickSize - 3.5f;
                foreach (var z in new[] { d.TickSize, d.TickSize - 2f, d.TickSize - 3.5f }) if (tw(t.Words, z, "Jost-SemiBold") <= ww) { ws = z; break; }
                d.TickWords[i] = ws; r.Items.Add(new Item { Slot = BookSlot.Ticks, Index = i, Cy = y + i * step, Size = d.TickSize, D = tight ? 24f : many ? 26f : 28f, At = new Rect(x0, y + i * step - step / 2f, w, step) });
            }
            if (shown > 0) y += (shown - 1) * step + 20f;
            float ty;
            if (p.NextLine.Length > 0)
            {
                float cy = y + 22f; var lines = Clip(WrapAny(p.NextLine, 25f, "Jost-SemiBold", w, tw), 2, 25f, "Jost-SemiBold", w, tw); d.Next[0] = lines;
                float chips = cy + 34f + lines.Length * 31.25f + 8f, h = p.NextWays.Count > 0 ? 44f : 0f;
                r.Items.Add(new Item { Slot = BookSlot.Next, Cy = cy, Size = 25f, At = new Rect(x0, cy - 12f, w, chips + h - (cy - 12f)), D = chips });
                ty = Mathf.Max(chips + h + 34f, StatTop);
            }
            else ty = Mathf.Max(y + 26f, StatTop);
            ty = PlaceStats(p, r, ty, tw);
            float bottom = ty + d.BandH;
            // the city's three columns, then the run, its columns measured as drawn; the city gives way when the run would not clear the ways on under it (and comes back when there is
            // no run to make room for). Measured with a fold open too, so opening one moves nothing above it
            float runY = Mathf.Max(RunTop, ty + d.BandH + 43f);
            if (p.City.Count > 0)
            {
                float cy0 = ty + d.BandH + 44f, cityBottom = cy0 + 70f, withCity = Mathf.Max(RunTop, cityBottom + 14f);
                if (cityBottom <= AgainFloor && (run == null || Clear(run, withCity, ways)))
                {
                    float cw = (DX1 - DX) / 3f;
                    for (int i = 0; i < Mathf.Min(p.City.Count, 3); i++)
                    {
                        var head = Clip(WrapAny(p.City[i].Head, 21f, "Jost-SemiBold", cw - 50f, tw), 2, 21f, "Jost-SemiBold", cw - 50f, tw);
                        var line = Clip(WrapAny(p.City[i].Line, 17.5f, "Jost-Medium", cw - 50f, tw), head.Length > 1 ? 1 : 2, 17.5f, "Jost-Medium", cw - 50f, tw);
                        d.CityHead[i] = head; d.CityLine[i] = line;
                        r.Items.Add(new Item { Slot = BookSlot.City, Index = i, Cy = cy0, Size = 21f, At = new Rect(DX + i * cw, cy0 - 14f, cw, 86f) });
                    }
                    runY = withCity; bottom = cityBottom;
                }
            }
            fits = bottom <= AgainFloor;
            if (run != null) { if (Clear(run, runY, ways)) bottom = PlaceRun(r, runY, run); else fits = false; }
            if (p.Fold != null) { r.Items.RemoveAll(i => i.Slot is BookSlot.City or BookSlot.Track); return PlaceFold(p, r, ty, top, tw); } // the fold takes the city's and the run's place
            return bottom;
        }

        /// <summary>At most `max` lines, the last cut to its width with an ellipsis when the rest would not fit (a line never runs into its neighbour).</summary>
        static string[] Clip(string[] lines, int max, float size, string font, float w, Measure tw)
        {
            if (lines.Length <= max) return lines;
            var o = new string[max]; System.Array.Copy(lines, o, max); string last = string.Join(" ", lines, max - 1, lines.Length - max + 1);
            while (last.Length > 1 && tw(last + "…", size, font) > w) { int sp = last.LastIndexOf(' '); last = sp > 0 ? last[..sp] : last[..^1]; }
            o[max - 1] = last.TrimEnd(',', ' ', '·') + "…"; return o;
        }

        /// <summary>The five numbers in one band: each as wide as its words want, the spare shared out; the value steps down until it fits, the reason wraps to three lines.</summary>
        static float PlaceStats(BookPage p, Result r, float ty, Measure tw)
        {
            int n = p.Stats.Count; if (n == 0) return ty; var d = r.Debrief;
            var want = new float[n]; float sum = 0f;
            for (int i = 0; i < n; i++) { var s = p.Stats[i]; want[i] = Mathf.Max(150f, Mathf.Max(34f + tw(s.Label, 19f, "Jost-SemiBold") + 34f, Mathf.Max(Mathf.Min(tw(s.Value, StatSizes[1], "Jost-Bold") + 24f, 260f), Mathf.Min(tw(s.Why, 17.5f, "Jost-Medium") / 2f + 40f, 230f)))); sum += want[i]; } // a long reason wraps rather than squeeze the rest // the value at the middle size, the reason on two lines (Glyph's widths share the band so)
            float spare = Mathf.Max(0f, (DX1 - DX) - sum) / n, k = sum > DX1 - DX ? (DX1 - DX) / sum : 1f, x = DX; int most = 1;
            for (int i = 0; i < n; i++)
            {
                var s = p.Stats[i]; float bw = want[i] * k + spare, vs = StatSizes[^1];
                foreach (var z in StatSizes) if (tw(s.Value, z, "Jost-Bold") <= bw - 32f) { vs = z; break; } // clear of the open number's frame
                var why = Clip(WrapAny(s.Why, 17.5f, "Jost-Medium", bw - 22f, tw), 3, 17.5f, "Jost-Medium", bw - 22f, tw); d.Why[i] = why; most = Mathf.Max(most, why.Length);
                r.Items.Add(new Item { Slot = BookSlot.Stats, Index = i, Cy = ty, Size = vs, At = new Rect(x - 12f, ty - 14f, bw - 6f, 176f) }); x += bw;
            }
            d.BandH = 112f + most * 17.5f * 1.25f + 6f; // the band as deep as its longest reason (the fold sits close under it)
            for (int i = 0; i < r.Items.Count; i++) if (r.Items[i].Slot == BookSlot.Stats) { var it = r.Items[i]; it.At = new Rect(it.At.x, it.At.y, it.At.width, d.BandH + 14f); r.Items[i] = it; }
            return ty;
        }

        /// <summary>The open number's fold: under the band when its rows fit there, else over it (the tail down to the number) when more of them fit above, never over the head;
        /// as many rows as reach the floor or the head, the biggest first.</summary>
        static float PlaceFold(BookPage p, Result r, float ty, float yTop, Measure tw)
        {
            var f = p.Fold; float pip = f.Pips.Count > 0 ? 30f : 0f, head = 96f + pip, foot = f.Law.Length > 0 ? 34f : 8f, below = ty + r.Debrief.BandH + 16f, aboveBottom = ty - 30f;
            float roomBelow = FoldFloor - below - head - foot, roomAbove = aboveBottom - yTop - head - foot; // -1: not even its head fits there
            int fitBelow = roomBelow < 0f ? -1 : Mathf.FloorToInt(roomBelow / FoldRowH), fitAbove = roomAbove < 0f ? -1 : Mathf.FloorToInt(roomAbove / FoldRowH);
            bool down = fitBelow >= 0 && (fitBelow >= f.Rows.Count || fitBelow >= fitAbove) || fitAbove < 0; int rows = Mathf.Clamp(f.Rows.Count, 0, Mathf.Max(0, down ? fitBelow : fitAbove));
            r.Debrief.FoldRows = rows; r.Debrief.FoldBelow = down;
            // the rows shown, in their order: every kept row, then the others from the first, as many as fit
            var shown = r.Debrief.FoldShown; int keep = 0; foreach (var x in f.Rows) if (x.Keep) keep++;
            for (int i = 0, others = Mathf.Max(0, rows - keep); i < f.Rows.Count && shown.Count < rows; i++) if (f.Rows[i].Keep || others-- > 0) shown.Add(i);
            if (rows > 0 && rows < f.Rows.Count && f.Rest.Length > 0) { int sum = 0; for (int i = rows - 1; i < f.Rows.Count; i++) sum += SignedValue(f.Rows[i].Value); r.Debrief.FoldRest = true; r.Debrief.FoldRestValue = sum > 0 ? "+" + sum : sum < 0 ? "\u2212" + (-sum) : "0"; } // the last row sums the rest
            float h = head + rows * FoldRowH + foot, at = down ? below : aboveBottom - h, tail = DX + 60f;
            foreach (var it in r.Items) if (it.Slot == BookSlot.Stats && p.Stats[it.Index].Id == f.Id) tail = it.At.center.x;
            r.Items.Add(new Item { Slot = BookSlot.Fold, Cy = tail, Size = 22f, D = pip, At = new Rect(DX, at, DX1 - DX, h) });
            return down ? at + h : ty + r.Debrief.BandH;
        }

        /// <summary>A row's signed value ("+18", "\u22123", "2"); 0 for anything else.</summary>
        public static int SignedValue(string v) { v = (v ?? "").Replace("+", "").Replace("\u2212", "-"); return int.TryParse(v, out int n) ? n : 0; }

        /// <summary>The run as it fits its columns: the stops shown (indices into the track, in order), the step, the label size and each label's lines.</summary>
        sealed class RunFit { public readonly List<int> Shown = new(); public readonly Dictionary<int, string[]> Lines = new(); public float Step, Size; public float H(int i) => RunD + 60f + (Lines[i].Length - 1) * Size * 1.05f; }

        /// <summary>The run's shapes, the best first: the most stops whose labels fit their columns (on two lines where a label wants it, else on one at a smaller size), then, when
        /// that one is deeper, the most stops that fit on one line; then no run (null). Fewer stops never make a run shallower, so no other shape could clear what these do not.</summary>
        static List<RunFit> RunShapes(BookPage p, Measure tw)
        {
            var l = new List<RunFit>(); int n = Mathf.Min(p.Track.Count, p.TrackMax);
            for (int k = n; k > 0 && k >= Mathf.Min(n, RunMin) && l.Count == 0; k--) if ((FitRun(p, k, true, tw) ?? FitRun(p, k, false, tw)) is RunFit f) l.Add(f);
            if (l.Count > 0 && l[0].Shown.Exists(i => l[0].Lines[i].Length > 1))
                for (int k = n; k >= Mathf.Min(n, RunMin); k--) if (FitRun(p, k, false, tw) is RunFit one) { l.Add(one); break; }
            l.Add(null); return l;
        }

        /// <summary>The run on its n stops that give way last (BookStop.Rank; the end always stays), evenly spaced, at the biggest size whose every label fits its column, on one
        /// line or, with `two`, on two where a label wants it; null when no size fits.</summary>
        static RunFit FitRun(BookPage p, int n, bool two, Measure tw)
        {
            var f = new RunFit(); var order = new List<int>(); for (int i = 0; i < p.Track.Count; i++) order.Add(i);
            order.Sort((a, b) => p.Track[a].Rank != p.Track[b].Rank ? p.Track[a].Rank.CompareTo(p.Track[b].Rank) : b.CompareTo(a)); // equal ranks keep the latest (the book's clock's rule)
            f.Shown.AddRange(order.GetRange(0, Mathf.Min(n, order.Count))); f.Shown.Sort(); f.Step = (DX1 - DX) / Mathf.Max(1, f.Shown.Count);
            foreach (var s in two ? RunTwo : RunOne)
            {
                bool ok = true; f.Lines.Clear();
                foreach (int i in f.Shown) { var ls = two ? RunWords(p.Track[i].Word, s, f.Step, tw) : new[] { p.Track[i].Word }; foreach (var part in ls) ok &= tw(part, s, "Jost-SemiBold") <= f.Step - 14f; f.Lines[i] = ls; }
                if (ok) { f.Size = s; return f; }
            }
            return null;
        }

        /// <summary>Every column of the run at y, as deep as its own label, RunGap clear of every way on (the new-or-same choice stands over the last columns) and on the grid.</summary>
        static bool Clear(RunFit f, float y, List<Item> ways)
        {
            for (int k = 0; k < f.Shown.Count; k++)
            {
                float h = f.H(f.Shown[k]); if (y + h > (ways.Count == 0 ? AgainFloor : GridH - 20f)) return false;
                var c = new Rect(DX + k * f.Step, y, f.Step, h + RunGap);
                foreach (var w in ways) if (c.Overlaps(w.At)) return false;
            }
            return true;
        }

        /// <summary>The run along the foot as fitted: the book's track, its stops evenly spaced, each label on its lines.</summary>
        static float PlaceRun(Result r, float y, RunFit f)
        {
            var d = r.Debrief; r.TrackStep = f.Step; r.TrackSize = d.RunSize = f.Size; float low = y;
            for (int k = 0; k < f.Shown.Count; k++) { int i = f.Shown[k]; d.Run[i] = f.Lines[i]; low = Mathf.Max(low, y + f.H(i)); r.Items.Add(new Item { Slot = BookSlot.Track, Index = i, Size = f.Size, D = RunD, At = new Rect(DX + k * f.Step, y, f.Step, f.H(i)) }); }
            return low;
        }
        /// <summary>A run label on one line when it fits its share, else on two, broken at the space that leaves the longer line shortest.</summary>
        static string[] RunWords(string word, float size, float step, Measure tw)
        {
            if (tw(word, size, "Jost-SemiBold") <= step - 14f || word.IndexOf(' ') < 0) return new[] { word };
            int best = -1; float bw = float.MaxValue; for (int i = word.IndexOf(' '); i > 0; i = word.IndexOf(' ', i + 1)) { float w = Mathf.Max(tw(word[..i], size, "Jost-SemiBold"), tw(word[(i + 1)..], size, "Jost-SemiBold")); if (w < bw) { bw = w; best = i; } }
            return new[] { word[..best], word[(best + 1)..] };
        }

        /// <summary>The ways on, bottom right: Try again (the book's orange pill), its new-or-same choice over it, then the outlined ways to its left. Index -1 is the choice.</summary>
        static void PlaceAgain(BookPage p, Result r, Measure tw) => r.Items.AddRange(Ways(p, tw));
        /// <summary>Where the ways on stand (PlaceAgain), for the page above them to keep clear of.</summary>
        static List<Item> Ways(BookPage p, Measure tw)
        {
            var l = new List<Item>(); if (p.Again.Count == 0) return l; const float x1 = 1875f, y = 1000f; float gx = x1;
            for (int i = 0; i < p.Again.Count; i++)
            {
                var a = p.Again[i]; if (!a.Main) continue; float w = tw(a.Word, 34f, "Jost-Bold") + 78f; gx = x1 - w;
                l.Add(new Item { Slot = BookSlot.Again, Index = i, Size = 34f, At = new Rect(gx, y, w, 62f) });
            }
            if (p.NewWord.Length > 0 && p.SameWord.Length > 0) { float sw = tw(p.NewWord, 17f, "Jost-SemiBold") + tw(p.SameWord, 17f, "Jost-SemiBold") + 64f; l.Add(new Item { Slot = BookSlot.Again, Index = -1, Size = 17f, At = new Rect(x1 - sw, y - 40f, sw, 30f) }); }
            gx -= 18f;
            for (int i = p.Again.Count - 1; i >= 0; i--)
            {
                var a = p.Again[i]; if (a.Main) continue; float w = tw(a.Word, 24f, "Jost-SemiBold") + 80f; gx -= w;
                l.Add(new Item { Slot = BookSlot.Again, Index = i, Size = 24f, At = new Rect(gx, y + 5f, w, 52f) }); gx -= 12f;
            }
            return l;
        }
    }
}
