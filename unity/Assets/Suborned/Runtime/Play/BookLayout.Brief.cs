using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 108, the briefing as three calm pages on the Job page (Glyph's mocks briefs/108/glyph/pages, the numbers read off them; Sky 2026-11-11: "Yes, much better
    /// and clearer"): 0 the mission (the face, the title, a few lines of story), 1 the place (the page's edge further right so the street has room; a row a place, the ways in
    /// among them with a chip that opens their steps), 2 look out for (a row a danger), and on every page the pager (Back, the three dots, Next; Begin on the last at the start).
    /// Pure, as the rest of BookLayout; nothing here knows what the words say.</summary>
    public static partial class BookLayout
    {
        public const float BriefCurve = 740f, PlaceCurve = 1100f, PagerY = 956f, PagerH = 76f, NextW = 190f, BeginW = 270f, BriefRight = 1745f, BriefRowGap = 10f, BriefDisc = 70f, StepSize = 21f;
        /// <summary>Each page's text column's left edge, and its pager's.</summary>
        public static readonly float[] BriefX = { 1120f, 1210f, 930f }, PagerX = { 930f, 1206f, 926f };
        public static readonly float[] TitleSize = { 70f, 54f, 66f };

        /// <summary>A brief row's height: the disc and its padding; a way's chip under its line adds one more line.</summary>
        public static float BriefRowH(BookPlace pl) => BriefDisc + 24f + (pl.Chip.Length > 0 ? 42f : 0f);

        static Result PlaceBrief(BookPage p, Measure tw)
        {
            var r = new Result(); int st = Mathf.Clamp(p.Step, 0, 2); float x = BriefX[st], w = BriefRight - x, y;
            float ky = st == 0 ? 212f : 120f, ks = st == 1 ? 20f : 22f;
            if (p.Kicker.Length > 0) r.Items.Add(new Item { Slot = BookSlot.Kicker, Cy = ky, Size = ks, At = new Rect(x, ky - 12f, w, 24f) });
            if (st == 0 && (p.HeroFace.Length > 0 || p.HeroIcon.Length > 0)) r.Items.Add(new Item { Slot = BookSlot.Hero, At = new Rect(670f, 180f, 420f, 420f) });
            float ns = TitleSize[st]; while (ns > 34f && tw(p.Name, ns, "Jost-Bold") > w) ns -= 2f; r.NameSize = ns;
            float ny = ky + 20f + ns * .55f; if (p.Name.Length > 0) r.Items.Add(new Item { Slot = BookSlot.Name, Cy = ny, Size = ns, At = new Rect(x, ny - ns * .5f, w, ns) });
            y = st == 0 ? 360f : 262f;
            if (st == 0)
                for (int i = 0; i < p.Story.Count; i++)
                {
                    const float ss = 34f, lh = ss * 1.32f; var lines = WrapAny(p.Story[i], ss, i == 0 ? "Jost-SemiBold" : "Jost-Medium", 620f, tw); r.StoryLines[i] = lines;
                    r.Items.Add(new Item { Slot = BookSlot.Story, Index = i, Size = ss, At = new Rect(x + 2f, y, 620f, lh * lines.Length) }); y += lh * lines.Length + 22f;
                }
            if (st == 1)
            {
                // the places; a way's steps open in the list's place: its row, then its conditions (Needs of its group) under it
                for (int i = 0; i < p.Places.Count; i++)
                {
                    var pl = p.Places[i]; if (p.WayOpen.Length > 0 && pl.Way != p.WayOpen) continue;
                    float h = BriefRowH(pl); r.Items.Add(new Item { Slot = BookSlot.Place, Index = i, Size = 31f, D = BriefDisc, At = new Rect(x - 18f, y, w + 18f, h) }); y += h + BriefRowGap;
                }
                if (p.WayOpen.Length > 0)
                {
                    y += 8f; float nx = x + 90f, nw = BriefRight - nx;
                    for (int i = 0; i < p.Needs.Count; i++)
                    {
                        if (p.Needs[i].Group != p.WayOpen) continue;
                        var lines = Wrap(p.Needs[i].Words, StepSize, "Jost-Medium", nw - 34f, tw); r.NeedLines[i] = lines; float h = NeedLineH * lines.Length + 10f;
                        r.Items.Add(new Item { Slot = BookSlot.Needs, Index = i, Size = StepSize, D = 22f, At = new Rect(nx, y, nw, h) }); y += h;
                    }
                }
            }
            if (st == 2)
                for (int i = 0; i < p.Dangers.Count; i++) { float h = BriefDisc + 26f; r.Items.Add(new Item { Slot = BookSlot.Danger, Index = i, Size = 31f, D = 72f, At = new Rect(x - 18f, y, w + 18f, h) }); y += h + BriefRowGap; }
            // the pager on the page's floor; Begin in Next's place on the last page of the start
            bool begin = p.Begin && st == 2; float px = PagerX[st];
            r.Items.Add(new Item { Slot = BookSlot.Pager, Index = st, Size = 34f, At = new Rect(px, PagerY, BriefRight - px - (begin ? BeginW : 0f), PagerH) });
            if (begin) r.Items.Add(new Item { Slot = BookSlot.Begin, Size = 46f, At = new Rect(BriefRight - BeginW, PagerY - 8f, BeginW, 92f) });
            r.Bottom = y; return r;
        }
    }
}
