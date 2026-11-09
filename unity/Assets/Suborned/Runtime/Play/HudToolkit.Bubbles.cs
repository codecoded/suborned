using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Camera = UnityEngine.Camera;

namespace Suborned.Play
{
    /// <summary>Brief 101 step 6, speech in the street (Glyph's S mocks, briefs/101/glyph/r2/S-*.png; moved here from HudToolkit.Less): a cream bubble at the speaker,
    /// 28 px tall and fully round at its ends over a soft shadow, the speaker's face at its left (22 px: the cell's and the cast's painted faces; police, rivals, the
    /// guard and passers-by by their badge on an ink disc), the line in Barlow Medium 15.5 in the book's ink, and a fine cream stem down to the speaker's head.
    /// It retires the plate_light bubble with its initial dot and its tail. One line, never wrapped; the newest nearest its speaker; one bubble a speaker; none for a
    /// speaker off screen or behind the roster (the Journal keeps the line); speech never shows under the Go hold.</summary>
    public sealed partial class HudToolkit
    {
        readonly List<Rect> bubbleRects = new();
        readonly List<(VisualElement tail, Rect box, Vector2 head)> bubbleLeads = new();
        /// <summary>The bubble's height and its face (the mock's).</summary>
        const float BubbleH = 28f, BubbleFace = 22f;

        /// <summary>A speaker's face in a bubble: the cell's cameo, the cast's painted face, else the speaker's badge (police, rivals, guard, a passer-by).</summary>
        static (string art, bool badge) BubbleArt(string speaker) => Cell.Contains(speaker) ? ($"portraits/cameo_{speaker}_normal", false) : HasFace(speaker) ? (FaceOf(speaker), false) : (SpeakerBadge(speaker), true);

        void Bubbles(Camera cam)
        {
            if (host.Loud?.Left != null) { foreach (var (br, _, bt) in bubbleEls) { Show(br, false); Show(bt, false); } return; } // Glyph r1: no speech under the Go hold (it clipped "Heads down.")
            float now = Time.unscaledTime; int max = host.Text?.BubbleMax ?? 2;
            var live = new List<(HandoverHost.Bubble b, Vector2 at, Vector2 head)>(); bubbleLeads.Clear();
            for (int i = host.Bubbles.Count - 1; i >= 0 && live.Count < max; i--)
            {
                var b = host.Bubbles[i]; if (now - b.Born > b.Life) continue; /* brief 085: a street line is held to read */
                if (host.BubbleWorld(b) is not Vector3 w || !Proj(w, out var p)) continue;
                if (!InPlay(p) || !Proj(w - Vector3.up * 0.45f, out var hv) || !InPlay(hv)) continue; // Glyph N7 (0.2.125): a speaker off screen has no bubble (one at x -198 drew behind the roster); the feed carries the line
                bool dup = false; foreach (var x in live) if (x.b.Speaker == b.Speaker) dup = true; if (dup) continue; // one bubble a speaker: the newest
                live.Add((b, p, Proj(w - Vector3.up * 0.45f, out var hq) ? hq : p));
            }
            while (bubbleEls.Count < max)
            {
                // placed by left/top (in the flow, a second bubble sat a bubble's height off where it was put); the shadow and the body stand behind the face and the line
                var r = E(marks); r.style.position = Position.Absolute; r.style.height = BubbleH; r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.paddingLeft = 3; r.style.paddingRight = 12; r.style.overflow = Overflow.Visible;
                var sh = E(r, "e-shadow"); sh.style.position = Position.Absolute; sh.style.left = 0; sh.style.right = 0; sh.style.top = 2; sh.style.bottom = -2; Round(sh, BubbleH / 2f); sh.style.opacity = 0.55f;
                var body = E(r, "e-page"); body.style.position = Position.Absolute; body.style.left = 0; body.style.right = 0; body.style.top = 0; body.style.bottom = 0; Round(body, BubbleH / 2f);
                var face = E(r, null, w: BubbleFace, h: BubbleFace); face.name = "who"; Round(face, BubbleFace / 2f); face.style.flexShrink = 0; face.style.overflow = Overflow.Hidden;
                E(face, null, 0, 0, BubbleFace, BubbleFace).name = "art";
                var t = L(r, "card-body"); t.style.color = BkInk; t.style.fontSize = 15.5f; t.style.marginLeft = 7; t.style.whiteSpace = WhiteSpace.NoWrap;
                var tail = E(r, null, 0, 0, 1, 1); tail.style.top = Length.Percent(100); tail.style.visibility = Visibility.Hidden; // the stem's foot on the bubble's bottom (BubbleTails moves it): no shape of its own
                int bi = bubbleEls.Count; bubbleEls.Add((r, t, tail)); manifest.Add((r, "speech", () => bubbleEls[bi].text.text));
            }
            bubbleRects.Clear();
            for (int i = 0; i < bubbleEls.Count; i++)
            {
                var (r, t, tail) = bubbleEls[i]; bool has = i < live.Count && !m.Ended; Show(r, has); Show(tail, has); if (!has) continue;
                var (b, p, head) = live[i]; Text(t, b.Text);
                var (art, badge) = BubbleArt(b.Speaker); var who = r.Q("who"); who.style.backgroundColor = badge ? BkInk : Color.clear;
                var ae = who.Q("art"); float inset = badge ? 1f : 0f; ae.style.left = ae.style.top = inset; ae.style.width = ae.style.height = BubbleFace - 2f * inset; Bg(ae, art, Color.white);
                float w = r.resolvedStyle.width, h = r.resolvedStyle.height; if (float.IsNaN(w)) w = 160; if (float.IsNaN(h)) h = BubbleH;
                int ri = Array.IndexOf(CellOrder, b.Speaker); bool ringed = ri >= 0 && m.Rings[ri].Show; // techwatch fix 3: over an activity ring the stem clears it by 8 px more
                float y = p.y - h - 34 - (ringed ? 16 : 0), x = BubbleX(p.x - w / 2, w, y); // review 12: 8 more px over the ring; Glyph r5 #5: inside the screen
                const float BubbleGap = 14; // at least 10 px between stacked bubbles as drawn, so each reads, and is counted, as its own; the newest nearest the speaker
                for (int guard = 0; guard < 4; guard++) { bool hit = false; foreach (var pr in bubbleRects) if (pr.Overlaps(new Rect(x - BubbleGap / 2, y - BubbleGap / 2, w + BubbleGap, h + BubbleGap))) { y = pr.y - h - BubbleGap; hit = true; } if (!hit) break; }
                bubbleRects.Add(new Rect(x, y, w, h));
                r.style.left = x; r.style.top = y; bubbleLeads.Add((tail, new Rect(x, y, w, h), head));
                float age = Time.unscaledTime - b.Born; r.style.opacity = tail.style.opacity = Mathf.Clamp01((b.Life - age) / 0.3f);
            }
        }

        /// <summary>Glyph r5 #5 (0.2.290: the Fixer's line ran to x 2120 of 1920, over a face at the edge): a bubble's left edge, 14 px inside the screen as the slip's, and
        /// clear of the roster's band (its faces, pips and take-control tab) beside it; BubbleTails slides the stem to the speaker.</summary>
        float BubbleX(float x, float w, float y)
        {
            var lay = root.panel.visualTree.layout; float W = float.IsNaN(lay.width) || lay.width <= 0 ? 1920f : lay.width;
            bool band = column != null && column.resolvedStyle.display != DisplayStyle.None && column.worldBound.width > 0 && y < (squadBtn != null && squadBtn.resolvedStyle.display != DisplayStyle.None ? squadBtn.worldBound.yMax : column.worldBound.yMax) + 40f;
            float lo = band ? TakeX + TakeD + 12f : 14f; return Mathf.Clamp(x, lo, Mathf.Max(lo, W - w - 14f));
        }

        /// <summary>Glyph 022, after the pips are placed: each bubble's stem drops from its bottom to its speaker's head (the mock's fine cream line, 2.2 px), and when a pip
        /// or a street mark sits under it, its foot moves along the bubble's bottom clear of it.</summary>
        void BubbleTails()
        {
            var icons = new List<Rect>(OtherMarks()); foreach (var (pr, _, _) in pipEls) if (pr.resolvedStyle.display != DisplayStyle.None) icons.Add(new Rect(pr.style.left.value.value, pr.style.top.value.value, 26, 26));
            foreach (var (tail, box, head) in bubbleLeads)
            {
                if (tail.resolvedStyle.display == DisplayStyle.None) continue;
                float tx = Mathf.Clamp(head.x, box.xMin + 14, box.xMax - 14);
                for (int guard = 0; guard < 3; guard++)
                {
                    bool hit = false; var lane = Rect.MinMaxRect(tx - 6, box.yMax, tx + 6, head.y);
                    foreach (var ic in icons) if (ic.Overlaps(lane)) { float right = ic.xMax + 8, left = ic.xMin - 8; tx = right <= box.xMax - 14 && (Mathf.Abs(right - head.x) <= Mathf.Abs(left - head.x) || left < box.xMin + 14) ? right : Mathf.Max(box.xMin + 14, left); hit = true; break; }
                    if (!hit) break;
                }
                tail.style.left = tx - box.xMin;
                var a = new Vector2(tx, box.yMax); var hd = head; float al = tail.resolvedStyle.opacity; for (var pe = tail.parent; pe != null && pe != marks; pe = pe.parent) al *= pe.resolvedStyle.opacity; // Glyph's 0.2.94 review #12: the bubble's own fade too
                if (hd.y - a.y > 6f && al > 0.02f) paints.Add(pp => { pp.lineCap = LineCap.Round; pp.strokeColor = new Color(StPage.r, StPage.g, StPage.b, al); pp.lineWidth = 2.2f; pp.BeginPath(); pp.MoveTo(a); pp.LineTo(hd); pp.Stroke(); });
            }
        }
    }
}
