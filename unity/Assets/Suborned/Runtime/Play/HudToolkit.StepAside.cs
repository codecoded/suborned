using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Glyph r123 F2 (batch 0.2.123 pre-flight, the loud frame at 2x: the gun disc over the speech bubble's first word, the attention
    /// clock half hidden behind the "5 s" fire pill, the stance pop 3 px off the bubble): one pass at the end of the HUD's frame, after every
    /// overlay is placed. Bubbles win: they never move and draw above the body discs (the fire layer). The fire pill steps up clear of a bubble
    /// or a pop. Plates step aside: an attention clock moves above or below what it would cover (its arc goes with it), at most 40 px, else it
    /// hides for the frame; a holder's name plate hides while a fire pill is within 60 px (its no-entry disc and the lock's count stay). View only.</summary>
    public sealed partial class HudToolkit
    {
        const float AsideGap = 6f, HolderPlateClear = 60f, ClockReach = 40f, RingChipClear = 60f, PopReach = 90f;
        /// <summary>The proof reads them: pops hidden this run because they could not clear (Glyph N2), chips hidden under an open ring (N4).</summary>
        public static int PopsHid, ChipsHidByRing;
        /// <summary>The car's seat-pip pill this frame (zero width: not drawn).</summary>
        Rect seatPill;

        /// <summary>An element's box in the marks' space (left/top as set this frame; the size as last laid out).</summary>
        static Rect BoxOf(VisualElement e, float fw = 120f, float fh = 30f)
        {
            float w = e.resolvedStyle.width, h = e.resolvedStyle.height; if (float.IsNaN(w) || w <= 0f) w = fw; if (float.IsNaN(h) || h <= 0f) h = fh;
            return new Rect(e.style.left.value.value, e.style.top.value.value, w, h);
        }
        static bool Shown(VisualElement e) => e != null && e.style.display != DisplayStyle.None && e.resolvedStyle.opacity > 0.05f;
        static Rect Grow(Rect r, float d) => new(r.x - d, r.y - d, r.width + 2f * d, r.height + 2f * d);
        static float Gap(Rect a, Rect b) { float dx = Mathf.Max(0f, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax)), dy = Mathf.Max(0f, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax)); return Mathf.Sqrt(dx * dx + dy * dy); }

        /// <summary>A box moved up (then, if that leaves the screen's top band, down) until it clears every rect by `gap`; null when it cannot.</summary>
        static Rect? ClearOf(Rect r, List<Rect> avoid, float gap, float minY, float maxY)
        {
            bool Free(Rect x) { foreach (var o in avoid) if (Grow(o, gap).Overlaps(x)) return false; return true; }
            if (Free(r)) return r;
            // candidates: just above or below each thing it meets; the nearest free one wins
            Rect? best = null; float bestD = float.MaxValue;
            foreach (var o in avoid)
            {
                foreach (float y in new[] { o.yMin - gap - r.height, o.yMax + gap })
                {
                    if (y < minY || y + r.height > maxY) continue; var c = new Rect(r.x, y, r.width, r.height);
                    if (!Free(c)) continue; float d = Mathf.Abs(y - r.y); if (d < bestD) { bestD = d; best = c; }
                }
            }
            return best;
        }

        void StepAside(float H)
        {
            if (marks == null) return;
            var bubbles = new List<Rect>(); var over = new List<VisualElement>();
            foreach (var (br, _, _) in bubbleEls) if (Shown(br)) { bubbles.Add(BoxOf(br, 160f, 26f)); over.Add(br); }
            // Glyph N4 (0.2.125): while a ring is open, the done chips near it hide (the feed carries them; they come back on close inside their 4.2 s)
            var ringPlates = new List<Rect>();
            if (host.Ring != null && ringRoot != null && ringRoot.style.display != DisplayStyle.None)
            {
                var rc = new Vector2(ringRoot.style.left.value.value, ringRoot.style.top.value.value); float reach = HandoverHost.RingOuter + RingChipClear;
                foreach (var w in wedges) if (Shown(w.Pill)) { var b = BoxOf(w.Pill, 180f, 30f); ringPlates.Add(new Rect(rc.x + b.x, rc.y + b.y, b.width, b.height)); }
                if (ringAlt != null && Shown(ringAlt)) { var b = BoxOf(ringAlt, 220f, 26f); ringPlates.Add(new Rect(rc.x + b.x, rc.y + b.y, b.width, b.height)); }
                ringPlates.Add(new Rect(rc.x - HandoverHost.RingOuter - 4f, rc.y - HandoverHost.RingOuter - 4f, 2f * HandoverHost.RingOuter + 8f, 2f * HandoverHost.RingOuter + 8f)); // the wheel itself (pops keep off it)
                foreach (var c in chipEls)
                {
                    if (!Shown(c.Root)) continue; var cb = BoxOf(c.Root, 180f, 60f);
                    var n = new Vector2(Mathf.Clamp(rc.x, cb.xMin, cb.xMax), Mathf.Clamp(rc.y, cb.yMin, cb.yMax)); bool hit = (n - rc).sqrMagnitude < reach * reach;
                    foreach (var pr in ringPlates) if (Grow(pr, AsideGap).Overlaps(cb)) hit = true;
                    if (hit) { Show(c.Root, false); ChipsHidByRing++; }
                }
            }
            // Glyph N2 (0.2.125): the pops are movers: each clears every bubble, chip, pill and ring plate by 16 px (up or down, at most PopReach), else it
            // hides for the frame; a pop that repeats the hovered lock pill's reason keeps its icon and drops its words
            var popAvoid = new List<Rect>(bubbles); popAvoid.AddRange(ringPlates);
            foreach (var c in chipEls) if (Shown(c.Root)) popAvoid.Add(BoxOf(c.Root, 180f, 60f));
            var pr0 = PillRect(); if (pr0.width > 0f) popAvoid.Add(Rect.MinMaxRect(pr0.xMin, pr0.yMin - (stackTab != null && Shown(stackTab) ? 10f : 0f), pr0.xMax, pr0.yMax + TailH - 4f));
            if (lockAlt != null && Shown(lockAlt)) popAvoid.Add(BoxOf(lockAlt, 300f, 26f));
            string lockSaid = PillState == "lock" && vpillLock != null && vpillLock.style.display != DisplayStyle.None ? vpillLock.text : "";
            var pops = new List<Rect>();
            foreach (var (pr, _, pt) in popEls)
            {
                if (!Shown(pr)) continue;
                if (lockSaid.Length > 0 && pt.style.display != DisplayStyle.None && pt.text == lockSaid) Show(pt, false);
                var pb = BoxOf(pr, 120f, ChipH); var av = new List<Rect>(popAvoid); av.AddRange(pops);
                var mv = ClearOf(pb, av, PopSpeechGap, 8f, H - 60f);
                if (mv is Rect m2 && Mathf.Abs(m2.y - pb.y) <= PopReach) { if (Mathf.Abs(m2.y - pb.y) > 0.01f) pr.style.top = m2.y; pops.Add(m2); over.Add(pr); }
                else { Show(pr, false); PopsHid++; }
            }
            // the fire pill (a decision) steps up clear of the speech and the pops; it never covers a bubble
            Rect pill = default; bool pillOn = fireLine != null && fireLine.style.display != DisplayStyle.None;
            if (pillOn)
            {
                pill = BoxOf(fireLine, 160f, 38f); var avoid = new List<Rect>(bubbles); avoid.AddRange(pops);
                if (ClearOf(pill, avoid, 8f, 112f, H - 230f) is Rect moved) { pill = moved; fireLine.style.top = moved.y; }
            }
            // a holder's name plate hides near a fire pill
            for (int i = 0; i < holderUsed && i < holderEls.Count; i++)
            {
                var np = holderEls[i].Q("np"); if (np == null || np.style.display == DisplayStyle.None) continue;
                var hb = BoxOf(holderEls[i], 26f, 26f); float nw = np.resolvedStyle.width; if (float.IsNaN(nw)) nw = 90f;
                var nb = new Rect(hb.x + 27f, hb.y + 2f, nw, 22f); bool near = pillOn && Gap(nb, pill) < HolderPlateClear;
                if (!near) foreach (var b in bubbles) if (Grow(b, AsideGap).Overlaps(nb)) near = true;
                if (near) Show(np, false);
            }
            // attention clocks: above or below what they would cover, else hidden this frame
            var hard = new List<Rect>(bubbles); hard.AddRange(pops); if (pillOn) hard.Add(pill);
            if (heldMark != null && heldMark.style.display != DisplayStyle.None) hard.Add(BoxOf(heldMark, 26f, 26f));
            for (int i = 0; i < holderUsed && i < holderEls.Count; i++) if (holderEls[i].style.display != DisplayStyle.None) hard.Add(BoxOf(holderEls[i], 26f, 26f));
            hard.AddRange(fireDiscs); // the gun discs over the firing agents (FireMarks)
            if (seatPill.width > 0f) hard.Add(seatPill); // the car's seat pips (StepMarks)
            foreach (var md in medals) if (md != null && md.Root.style.display != DisplayStyle.None) hard.Add(Grow(BoxOf(md.Root, 72f, 72f), 4f)); // the attention callouts (their ring 8 px round the disc, half of it clear)
            for (int i = 0; i < clockEls.Count; i++)
            {
                var (cr, _) = clockEls[i]; if (cr.style.display == DisplayStyle.None) continue;
                var cb = BoxOf(cr, 74f, ClockH); var arcAt = i < clockArcAt.Count ? clockArcAt[i] : null;
                var withArc = Rect.MinMaxRect(cb.xMin - 22f, cb.yMin, cb.xMax, cb.yMax); // the arc 11 px left of the plate, 8 px round
                var moved = ClearOf(withArc, hard, AsideGap, 112f, H - 120f);
                if (moved is Rect mv && Mathf.Abs(mv.y - withArc.y) > ClockReach) moved = null; // too far from its agent to read as his: the callout says it
                if (moved is Rect m2) { float dy = m2.y - withArc.y; if (Mathf.Abs(dy) > 0.01f) { cr.style.top = cb.y + dy; if (arcAt != null) arcAt[0].y += dy; } hard.Add(Rect.MinMaxRect(m2.xMin, m2.yMin, m2.xMax, m2.yMax)); }
                else { Show(cr, false); if (arcAt != null) arcAt[0] = new Vector2(float.NaN, float.NaN); }
            }
            // bubbles and pops above the fire layer's discs (the gun disc sat on the bubble's first word); the fire layer above every other mark
            if (fireLayer != null && fireLayer.parent == marks)
            {
                int fi = marks.IndexOf(fireLayer); bool ok = fi == marks.childCount - 1 - over.Count;
                if (ok) foreach (var e in over) if (e.parent != marks || marks.IndexOf(e) < fi) ok = false;
                if (!ok) { fireLayer.BringToFront(); foreach (var e in over) if (e.parent == marks) e.BringToFront(); }
            }
        }
    }
}
