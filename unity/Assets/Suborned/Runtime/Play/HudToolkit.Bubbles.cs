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
    /// Round 4 (Glyph, briefs/101/glyph/r4/notes.md; Sky 2026-11-09: "Sure", and option B for long lines, "two rows"):
    ///  · the pool: the host's lines newest first, one a speaker, up to BubbleMax (2), each at its speaker on screen, else beside its docked face (a line said off
    ///    screen, Bubble.Inset; SpeechPlate places it after the callouts). A third gives way, the oldest, wherever it is drawn; a docked line that gives way takes its
    ///    face with it (Callouts). A line with neither shows nowhere but the Journal. Speech never shows under the Go hold;
    ///  · the wrap: one row while it fits 460 px; else two rows, broken after a comma or a full stop when that row runs at most 40 px longer than the most balanced
    ///    break, else at the balanced break; a line two rows of 460 can't hold widens its bubble, never a third row. 47 px tall, the face centred on it, the second
    ///    row growing upward from where the build puts the bubble.</summary>
    public sealed partial class HudToolkit
    {
        readonly List<Rect> bubbleRects = new();
        readonly List<(VisualElement tail, Rect box, Vector2 head)> bubbleLeads = new();
        /// <summary>The bubble's height and its face (the mock's).</summary>
        const float BubbleH = 28f, BubbleFace = 22f;
        /// <summary>Option B (Glyph r4 §3): a one-row bubble's widest at 1080, a two-row bubble's height (the font's 18.6 px a row, corners 14 px), how much longer than
        /// the balanced break a phrase break may run, and the docked line's padding (no face of its own: 12 px each side).</summary>
        const float BubbleWrapW = 460f, BubbleH2 = 47f, PhraseSlack = 40f, BubblePadDocked = 12f, BubbleType = 15.5f;
        /// <summary>Text room between a bubble's ends: with the face (3 + 22 + 7 on the left, 12 on the right) and without it (12 and 12).</summary>
        static float BubbleChrome(bool face) => face ? 3f + BubbleFace + 7f + 12f : 2f * BubblePadDocked;

        /// <summary>Per pool element: its second row, and the rows last measured for its line (each break measured once, when the text changes).</summary>
        sealed class BubbleRowsEl { public Label Second; public string Text, One = "", Two = ""; public bool Face; public float W; }
        readonly List<BubbleRowsEl> bubbleRowEls = new();
        /// <summary>The pool's docked lines this frame and the element each is dressed in (SpeechPlate shows and places it after the callouts).</summary>
        readonly List<(HandoverHost.Bubble b, int el, Vector2 size)> dockedSay = new();

        /// <summary>A speaker's face in a bubble: the cell's cameo, the cast's painted face, else the speaker's badge (police, rivals, guard, a passer-by).</summary>
        static (string art, bool badge) BubbleArt(string speaker) => Cell.Contains(speaker) ? ($"portraits/cameo_{speaker}_normal", false) : HasFace(speaker) ? (FaceOf(speaker), false) : (SpeakerBadge(speaker), true);

        /// <summary>The callouts draw this frame (Callouts' own test): a docked line counts in the pool only when its face can show.</summary>
        bool CalloutsLive() => !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None && host.Game != null && !host.Following;

        void Bubbles(Camera cam)
        {
            dockedSay.Clear();
            if (host.Loud?.Left != null) { foreach (var (br, _, bt) in bubbleEls) { Show(br, false); Show(bt, false); } return; } // Glyph r1: no speech under the Go hold (it clipped "Heads down.")
            float now = Time.unscaledTime; int max = host.Text?.BubbleMax ?? 2; bool docks = CalloutsLive();
            var atSpeaker = new List<(HandoverHost.Bubble b, Vector2 at, Vector2 head)>(); var docked = new List<HandoverHost.Bubble>(); var said = new HashSet<string>(); bubbleLeads.Clear();
            for (int i = host.Bubbles.Count - 1; i >= 0 && atSpeaker.Count + docked.Count < max; i--)
            {
                var b = host.Bubbles[i]; if (now - b.Born > b.Life || said.Contains(b.Speaker)) continue; /* brief 085: a street line is held to read; one bubble a speaker: the newest */
                if (host.BubbleWorld(b) is Vector3 w && Proj(w, out var p) && InPlay(p) && Proj(w - Vector3.up * 0.45f, out var hv) && InPlay(hv)) { said.Add(b.Speaker); atSpeaker.Add((b, p, hv)); continue; }
                if (b.Inset && docks && host.Callouts.Exists(c => c.Speech == b)) { said.Add(b.Speaker); docked.Add(b); } // round 4: said off screen, it counts beside its docked face; else the Journal alone (Glyph N7: none behind the roster)
            }
            while (bubbleEls.Count < max)
            {
                // placed by left/top (in the flow, a second bubble sat a bubble's height off where it was put); the shadow and the body stand behind the face and the line
                var r = E(marks); r.style.position = Position.Absolute; r.style.height = BubbleH; r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.paddingLeft = 3; r.style.paddingRight = 12; r.style.overflow = Overflow.Visible;
                var sh = E(r, "e-shadow"); sh.style.position = Position.Absolute; sh.style.left = 0; sh.style.right = 0; sh.style.top = 2; sh.style.bottom = -2; Round(sh, BubbleH / 2f); sh.style.opacity = 0.55f;
                var body = E(r, "e-page"); body.style.position = Position.Absolute; body.style.left = 0; body.style.right = 0; body.style.top = 0; body.style.bottom = 0; Round(body, BubbleH / 2f);
                var face = E(r, null, w: BubbleFace, h: BubbleFace); face.name = "who"; Round(face, BubbleFace / 2f); face.style.flexShrink = 0; face.style.overflow = Overflow.Hidden;
                E(face, null, 0, 0, BubbleFace, BubbleFace).name = "art";
                var col = E(r); col.name = "rows"; col.style.flexDirection = FlexDirection.Column; col.style.alignItems = Align.FlexStart; col.style.flexShrink = 0; col.style.marginLeft = 7; // option B: the rows, left-aligned
                Label Row() { var t = L(col, "card-body"); t.style.color = BkInk; t.style.fontSize = BubbleType; t.style.whiteSpace = WhiteSpace.NoWrap; return t; }
                var one = Row(); var two = Row(); Show(two, false);
                var tail = E(r, null, 0, 0, 1, 1); tail.style.top = Length.Percent(100); tail.style.visibility = Visibility.Hidden; // the stem's foot on the bubble's bottom (BubbleTails moves it): no shape of its own
                int bi = bubbleEls.Count; bubbleEls.Add((r, one, tail)); bubbleRowEls.Add(new BubbleRowsEl { Second = two });
                manifest.Add((r, "speech", () => BubbleLine(bi)));
            }
            bubbleRects.Clear();
            for (int i = 0; i < bubbleEls.Count; i++)
            {
                var (r, t, tail) = bubbleEls[i]; Show(tail, false);
                if (i >= atSpeaker.Count)
                {   // a docked line's element (round 4): dressed here, shown and placed by SpeechPlate once the callouts have their places; else hidden
                    Show(r, false); int di = i - atSpeaker.Count; if (di >= docked.Count || m.Ended) continue;
                    dockedSay.Add((docked[di], i, Dress(i, docked[di], false))); continue;
                }
                Show(r, !m.Ended); if (m.Ended) continue; Show(tail, true);
                var (b, p, head) = atSpeaker[i]; var size = Dress(i, b, true); float w = size.x, h = size.y;
                int ri = Array.IndexOf(CellOrder, b.Speaker); bool ringed = ri >= 0 && m.Rings[ri].Show; // techwatch fix 3: over an activity ring the stem clears it by 8 px more
                float y = p.y - h - 34 - (ringed ? 16 : 0), x = BubbleX(p.x - w / 2, w, y); // review 12: 8 more px over the ring; Glyph r5 #5: inside the screen; option B: a second row grows upward
                const float BubbleGap = 14; // at least 10 px between stacked bubbles as drawn, so each reads, and is counted, as its own; the newest nearest the speaker
                for (int guard = 0; guard < 4; guard++) { bool hit = false; foreach (var pr in bubbleRects) if (pr.Overlaps(new Rect(x - BubbleGap / 2, y - BubbleGap / 2, w + BubbleGap, h + BubbleGap))) { y = pr.y - h - BubbleGap; hit = true; } if (!hit) break; }
                if (y < BubbleEdge) { y = BubbleEdge; foreach (var pr in bubbleRects) if (pr.Overlaps(new Rect(x - BubbleGap / 2, y - BubbleGap / 2, w + BubbleGap, h + BubbleGap))) y = Mathf.Max(y, pr.yMax + BubbleGap); } // round 4 must 5: 14 px inside the top too, the newer below
                y = Mathf.Min(y, Mathf.Max(BubbleEdge, ScreenH() - h - BubbleEdge));
                bubbleRects.Add(new Rect(x, y, w, h));
                r.style.left = x; r.style.top = y; bubbleLeads.Add((tail, new Rect(x, y, w, h), head));
                float age = Time.unscaledTime - b.Born; r.style.opacity = tail.style.opacity = Mathf.Clamp01((b.Life - age) / 0.3f);
            }
        }

        /// <summary>A pool element dressed for a line: its face (or none on a docked line, whose docked face is the speaker), its one or two rows, its height; its size
        /// (from the measured rows, so a new line is placed right in its first frame; the laid-out size until the label can measure).</summary>
        Vector2 Dress(int i, HandoverHost.Bubble b, bool face)
        {
            var (r, t, _) = bubbleEls[i]; var re = bubbleRowEls[i]; var who = r.Q("who"); var col = r.Q("rows");
            Show(who, face); r.style.paddingLeft = face ? 3f : BubblePadDocked; col.style.marginLeft = face ? 7f : 0f;
            if (face)
            {
                var (art, badge) = BubbleArt(b.Speaker); who.style.backgroundColor = badge ? BkInk : Color.clear;
                var ae = who.Q("art"); float inset = badge ? 1f : 0f; ae.style.left = ae.style.top = inset; ae.style.width = ae.style.height = BubbleFace - 2f * inset; Bg(ae, art, Color.white);
            }
            if (re.Text != b.Text || re.Face != face || re.W <= 0f) { re.Text = b.Text; re.Face = face; (re.One, re.Two, re.W) = BubbleRows(t, b.Text, BubbleWrapW - BubbleChrome(face)); }
            Text(t, re.One); Text(re.Second, re.Two); Show(re.Second, re.Two.Length > 0);
            float h = re.Two.Length > 0 ? BubbleH2 : BubbleH; r.style.height = h; // the corners stay 14 px (BubbleH / 2): a two-row bubble is a rounded box
            float lw = r.resolvedStyle.width; return new Vector2(re.W > 0f ? re.W + BubbleChrome(face) : float.IsNaN(lw) || lw < 1f ? 160f : lw, h);
        }

        /// <summary>The words a pool element says now (its rows joined): the HUD budget's manifest text.</summary>
        string BubbleLine(int i) { var re = bubbleRowEls[i]; return re.Two.Length > 0 ? re.One + " " + re.Two : bubbleEls[i].text.text; }

        /// <summary>Option B (Glyph r4 §3): the line in one row when it fits `room` px of text, else in two: after a comma or a full stop when that row runs at most 40 px
        /// longer than the most balanced break, else at the break that makes the longer row shortest. Each break's rows are measured once, in the label's own font,
        /// when the text changes. (row one, row two or "", the widest row's width; 0 while the label can't measure yet).</summary>
        static (string one, string two, float w) BubbleRows(Label t, string text, float room)
        {
            if (t.panel == null || Mathf.Abs(t.resolvedStyle.fontSize - BubbleType) > 0.1f) return (text, "", 0f); // not styled yet (a new element's first frame): measured next frame
            float M(string s) { var v = t.MeasureTextSize(s, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined); return float.IsNaN(v.x) ? 0f : v.x; }
            float full = M(text); var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (full <= 0f || full <= room || words.Length < 2) return (text, "", full);
            int best = -1, phrase = -1; float bestW = float.MaxValue, phraseW = float.MaxValue;
            for (int k = 1; k < words.Length; k++)
            {
                float w = Mathf.Max(M(string.Join(" ", words, 0, k)), M(string.Join(" ", words, k, words.Length - k)));
                if (w < bestW) { bestW = w; best = k; }
                char end = words[k - 1][^1]; if ((end == ',' || end == '.') && w < phraseW) { phraseW = w; phrase = k; }
            }
            int at = phrase > 0 && phraseW <= bestW + PhraseSlack ? phrase : best;
            return (string.Join(" ", words, 0, at), string.Join(" ", words, at, words.Length - at), at == phrase ? phraseW : bestW);
        }

        /// <summary>The panel's height (screen px at 1080).</summary>
        float ScreenH() { var lay = root.panel.visualTree.layout; return float.IsNaN(lay.height) || lay.height <= 0 ? 1080f : lay.height; }

        /// <summary>Glyph r5 #5 (0.2.290: the Fixer's line ran to x 2120 of 1920, over a face at the edge): a bubble's left edge, 14 px inside the screen as the slip's, and
        /// clear of the roster's band (its faces, pips and take-control tab) beside it; BubbleTails slides the stem to the speaker. The edge faces keep 10 px off every
        /// bubble from their side (Edges: they slide along the edge), so a line stays nearest its speaker.</summary>
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
