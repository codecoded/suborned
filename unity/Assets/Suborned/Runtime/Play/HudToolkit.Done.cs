using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 043, the done moment in the kit HUD (testbed/controls/gains.js, the playground's Settings → Done moment "Gains"):
    ///  · the gain chip over the thing that changed, without hover, 4.2 s: the doer's face with the outcome's mark (✓ done, ½ not yet, ✗ refused),
    ///    one icon-and-word line per fact (at most three), and the unlock row (an arrow, then the verbs that just opened as small dark pills, a
    ///    way shut by a refusal struck through); it rises 10 px as it appears and lifts clear of the pill when they share a thing;
    ///  · the window marker on what a window switched off: an icon and the seconds left, or the Tech's face while he holds a loop; it blinks in
    ///    its last 10 s (the closing pop is the host's Pop);
    ///  · the cancel tag over the pill ("Cancels Listen · 12 s lost") and beside the cursor resting on open ground;
    ///  · the lit pip on the pill and the ring wedge of an act that just opened.
    /// It reads the host only: replays are untouched.</summary>
    public sealed partial class HudToolkit
    {
        sealed class ChipEl { public VisualElement Root, Face, Mark, Lines, Unl; public readonly List<(VisualElement row, VisualElement icon, Label word)> Rows = new(); public readonly List<(VisualElement pill, VisualElement icon, Label word, VisualElement strike)> Pills = new(); public int Id = -1; }
        readonly List<ChipEl> chipEls = new();
        readonly List<(VisualElement root, VisualElement icon, Label num, VisualElement face, VisualElement under)> winEls = new();
        VisualElement cancelTag; Label cancelTagText; Vector2 restAt; float restSince;
        /// <summary>The proof reads it: the cancel tag's words now ("" hidden), and where it is (over the pill, or on the ground by the cursor).</summary>
        public static string CancelTagState { get; private set; } = "";
        public static bool PipShown { get; private set; }
        public static int ChipsShown { get; private set; }
        public static int WindowMarksShown { get; private set; }
        public static string ChipWords { get; private set; } = "";
        public static string WindowWords { get; private set; } = "";

        /// <summary>Brief 044 A: the window marker is plate_dark at 38 px (slice 0.43, the verb pill's height), the unlock pills plate_dark at 30 px (slice 0.3): the
        /// heights that keep their glyphs 4 px clear of the chrome rim at that scale (the layout audit's rule), with a pixel to spare for the
        /// layout's rounding.</summary>
        const float WinH = 38f, WinScale = 0.43f, UnlH = 30f, UnlScale = 0.3f, ChipFace = 32f;

        ChipEl NewChip()
        {
            // brief 044 A: the Intel card's panel_dark (slice 0.5), the doer's face in the chrome ring with the kit's outcome badge, the lines in
            // cream, a brass hairline, then the turquoise-light arrow and the verbs that opened as small plate_dark pills (a shut way struck through)
            var c = new ChipEl();
            c.Root = E(marks, "s-panel_dark", 0, 0, float.NaN, float.NaN); c.Root.style.flexDirection = FlexDirection.Row; c.Root.style.alignItems = Align.FlexStart; c.Root.pickingMode = PickingMode.Ignore;
            c.Root.style.paddingLeft = 17; c.Root.style.paddingRight = 17; c.Root.style.paddingTop = 13; c.Root.style.paddingBottom = 12;
            c.Face = Cameo(c.Root, ChipFace); c.Face.style.overflow = Overflow.Visible; c.Face.style.marginTop = -2;
            c.Mark = E(c.Face, null, float.NaN, float.NaN, 19, 19); c.Mark.style.position = Position.Absolute; c.Mark.style.right = -6; c.Mark.style.bottom = -5;
            c.Lines = E(c.Root); c.Lines.style.marginLeft = 11; c.Lines.style.flexShrink = 0; c.Lines.style.paddingRight = 2;
            for (int i = 0; i < 3; i++)
            {
                var row = E(c.Lines); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.height = 22;
                var ic = E(row, null, w: 16, h: 16); ic.style.flexShrink = 0; var w = Words(row, 15f); w.style.marginLeft = 8;
                c.Rows.Add((row, ic, w));
            }
            c.Unl = E(c.Lines); c.Unl.style.flexDirection = FlexDirection.Row; c.Unl.style.alignItems = Align.Center; c.Unl.style.marginTop = 4; c.Unl.style.paddingTop = 6; c.Unl.style.borderTopWidth = 1; c.Unl.style.borderTopColor = LookHair;
            var arrow = E(c.Unl, null, w: 12, h: 12); arrow.style.flexShrink = 0; arrow.style.rotate = new Rotate(90f); arrow.name = "arrow";
            for (int i = 0; i < 3; i++)
            {
                var p = PlateDark(c.Unl, UnlH, UnlScale); p.style.marginLeft = 5; p.style.paddingLeft = 9; p.style.paddingRight = 10;
                var ic = E(p, null, w: 12, h: 12); ic.style.flexShrink = 0; var w = Words(p, 12.5f); w.style.marginLeft = 5;
                var st = E(p, null, 6, UnlH / 2f - 0.75f, float.NaN, 1.5f); st.style.right = 6; st.style.backgroundColor = Cream; // the struck way
                c.Pills.Add((p, ic, w, st));
            }
            chipEls.Add(c); return c;
        }

        /// <summary>Per frame after the pill (the commander view and third person alike; none while the end card or the briefing is up).</summary>
        void DoneMarks()
        {
            var h = host; var g = h.Game; float now = Time.unscaledTime; bool on = g != null && !m.Ended && !m.BriefingOpen && h.View != null && h.Modal == ModalGate.Modal.None;
            int used = 0; var words = new List<string>(); var stackTop = new Dictionary<string, float>();
            Rect pill = PillRect(); var bubbles = SpeechRects(); var markTop = new Dictionary<string, float>();
            // the window markers
            int wu = 0; var wwords = new List<string>();
            if (on)
                foreach (var w in h.WindowMarks)
                {
                    var at = h.AnchorWorld(w.Key); if (!at.HasValue) continue;
                    bool person = w.Key is "guard" or "patrol"; if (!Proj(at.Value + Vector3.up * (person ? 2.45f : w.Key.StartsWith("camera/") ? 0.75f : 1.7f), out var q)) continue;
                    while (winEls.Count <= wu)
                    {
                        // brief 044 A: plate_dark, the eye and the seconds (tabular), or the Tech's face while he holds a loop; a 2.5 px underline drains
                        // with the time, turquoise-light, amber (the HUD's "running out") in the last 10 s, when the plate blinks as built
                        var r = PlateDark(marks, WinH, WinScale); r.style.position = Position.Absolute; r.style.paddingLeft = 11; r.style.paddingRight = 12;
                        var ic = E(r, null, w: 15, h: 15); ic.style.flexShrink = 0; var nl = Words(r, 14.5f); nl.style.marginLeft = 6; nl.style.marginBottom = 1;
                        var f = Cameo(r, 16f, 1.5f); f.style.marginLeft = 6;
                        var track = E(r, null, 11, WinH - 9f, float.NaN, 2.5f); track.style.right = 11; Round(track, 1.25f); track.style.backgroundColor = new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.6f); track.style.overflow = Overflow.Hidden;
                        var u = E(track, null, 0, 0, 0, 2.5f); Round(u, 1.25f);
                        winEls.Add((r, ic, nl, f, u));
                    }
                    var (wr, wi, wn, wf, wu2) = winEls[wu++]; Show(wr, true);
                    Icon(wi, "eye_shut", Cream); bool face = w.Face >= 0; Show(wf, face); Show(wn, !face);
                    if (face) CameoOf(wf, w.Face); else Text(wn, w.Left.ToString());
                    bool low = !face && w.Left >= 0 && w.Left <= 10; wr.style.opacity = low ? 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(now * Mathf.PI * 4f)) : 1f;
                    bool timed = w.Left >= 0 && w.Total > 0; Show(wu2.parent, timed);
                    if (timed) { wu2.style.width = Length.Percent(Mathf.Clamp01(w.Left / (float)w.Total) * 100f); wu2.style.backgroundColor = low ? LookSoon : LookLive; }
                    float ww = float.IsNaN(wr.resolvedStyle.width) ? 60f : wr.resolvedStyle.width, wx = q.x - ww / 2f, wb = ClearOfSpeech(bubbles, wx, ww, q.y - 12f, WinH); // above the thing's speech, never on it
                    wr.style.left = wx; wr.style.top = wb - WinH; markTop[w.Key] = wb - WinH;
                    wwords.Add($"{w.Kind}:{w.Thing}:{(face ? "face" : w.Left.ToString())}");
                }
            for (int i = wu; i < winEls.Count; i++) Show(winEls[i].root, false);
            WindowMarksShown = wu; WindowWords = string.Join(" ", wwords);
            if (on)
                foreach (var ch in h.DoneChips)
                {
                    float age = now - ch.Born; if (age > HandoverHost.DoneChipS) continue;
                    var at = h.AnchorWorld(ch.AnchorKey) ?? ch.At; if (!at.HasValue) continue; ch.At = at;
                    bool person = ch.AnchorKey is "waitress" or "driver" or "guard" or "councillor" || ch.AnchorKey.StartsWith("op/") || ch.AnchorKey == "patrol";
                    if (!Proj(at.Value + Vector3.up * (person ? 2.3f : ch.AnchorKey.StartsWith("camera/") ? 0.7f : 1.6f), out var q)) continue;
                    var c = used < chipEls.Count ? chipEls[used] : NewChip(); used++; Show(c.Root, true);
                    if (c.Id != ch.Id)
                    {
                        c.Id = ch.Id; CameoOf(c.Face, ch.Agent);
                        Bg(c.Mark, ch.Tone == "ok" ? "badges/mark_ok" : ch.Tone == "soft" ? "badges/mark_half" : "badges/mark_no", Color.white); // the tone only picks the kit's badge
                        for (int i = 0; i < 3; i++)
                        {
                            bool has = i < ch.Lines.Count; Show(c.Rows[i].row, has); if (!has) continue;
                            Icon(c.Rows[i].icon, ch.Lines[i].Icon, Cream); Text(c.Rows[i].word, ch.Lines[i].Word);
                        }
                        Show(c.Unl, ch.Unlocks.Count > 0); Icon(c.Unl.Q("arrow"), "chev_up", LookLive);
                        for (int i = 0; i < 3; i++)
                        {
                            bool has = i < ch.Unlocks.Count; Show(c.Pills[i].pill, has); if (!has) continue; var u = ch.Unlocks[i];
                            Icon(c.Pills[i].icon, u.Icon, Cream); Text(c.Pills[i].word, u.Word); Show(c.Pills[i].strike, u.Shut); c.Pills[i].pill.style.opacity = u.Shut ? 0.65f : 1f;
                        }
                    }
                    float cw = float.IsNaN(c.Root.resolvedStyle.width) ? 180f : c.Root.resolvedStyle.width, chh = float.IsNaN(c.Root.resolvedStyle.height) ? 60f : c.Root.resolvedStyle.height;
                    float rise = (1f - Ease(Mathf.Min(1f, age / 0.25f))) * 10f;
                    // a second chip on the same thing stacks above the first by the first's own height (0.2.66 at 2x: a short turn chip sat inside the tall listen chip)
                    float x = q.x - cw / 2f, bottom = (stackTop.TryGetValue(ch.AnchorKey, out var st0) ? st0 - 6f : markTop.TryGetValue(ch.AnchorKey, out var mt) ? mt - 8f : q.y - 10f) - rise; // over its window's marker
                    bottom = ClearOfSpeech(bubbles, x, cw, bottom, chh);
                    // lift clear of the pill when they share the thing (the playground's hover lift)
                    if (pill.width > 0 && x < pill.xMax && x + cw > pill.xMin && bottom > pill.yMin - 8f && bottom - chh < pill.yMax) bottom = pill.yMin - 8f;
                    float W = root.layout.width > 0 ? root.layout.width : 1920f; x = Mathf.Clamp(x, 14f, Mathf.Max(14f, W - cw - 14f));
                    c.Root.style.left = x; c.Root.style.top = bottom - chh; stackTop[ch.AnchorKey] = bottom - chh + rise; c.Root.style.opacity = Mathf.Min(1f, Mathf.Min(age / 0.12f, (HandoverHost.DoneChipS - age) / 0.5f)); c.Root.BringToFront();
                    var ws = new List<string>(); foreach (var l in ch.Lines) ws.Add(l.Word); foreach (var u in ch.Unlocks) ws.Add((u.Shut ? "~" : "→") + u.Word); words.Add(string.Join("|", ws));
                }
            for (int i = used; i < chipEls.Count; i++) { Show(chipEls[i].Root, false); chipEls[i].Id = -1; }
            ChipsShown = used; ChipWords = string.Join(" / ", words);
        }

        static float Ease(float t) => 1f - (1f - t) * (1f - t);

        /// <summary>The speech bubbles on screen now (panel space): speech is placed first; a chip or a window marker stacks above it.</summary>
        List<Rect> SpeechRects()
        {
            // each bubble's box and its leader down to the speaker's head (Bubbles fills bubbleLeads before the overlays are placed): an overlay
            // on the speaker stacks above the speech, never over it or across its leader
            var l = new List<Rect>();
            foreach (var (tail, box, head) in bubbleLeads)
            {
                if (tail == null || tail.parent == null || tail.parent.resolvedStyle.display == DisplayStyle.None || tail.parent.resolvedStyle.opacity < 0.05f) continue;
                l.Add(Rect.MinMaxRect(box.xMin, box.yMin, box.xMax, Mathf.Max(box.yMax + 6f, head.y)));
            }
            return l;
        }
        /// <summary>The bottom for a box (x, width, height) at `bottom`, raised until it clears every speech bubble by 10 px.</summary>
        static float ClearOfSpeech(List<Rect> bubbles, float x, float w, float bottom, float hh)
        {
            for (int guard = 0; guard < 4; guard++)
            {
                bool hit = false; var box = new Rect(x - 4f, bottom - hh - 4f, w + 8f, hh + 8f);
                foreach (var b in bubbles) if (b.Overlaps(box)) { bottom = b.yMin - 10f; hit = true; box = new Rect(x - 4f, bottom - hh - 4f, w + 8f, hh + 8f); } // review 5: 8 px between as drawn (the bubble's rim eats 2)
                if (!hit) break;
            }
            return bottom;
        }

        /// <summary>The pill's rect in panel space now (zero width when it is hidden).</summary>
        Rect PillRect()
        {
            if (vpill == null || vpill.resolvedStyle.display == DisplayStyle.None) return default;
            float x = vpill.style.left.value.value, y = vpill.style.top.value.value, w = vpill.resolvedStyle.width, hh = vpill.resolvedStyle.height;
            return float.IsNaN(w) || float.IsNaN(hh) ? default : new Rect(x, y, w, hh);
        }

        /// <summary>The cancel tag (brief 043 §3.6): over the pill when the order it offers or charges would drop an act; on open ground beside a
        /// cursor resting 0.3 s, for the selected agents (a click there walks them, dropping their acts). Words: Fable's less.done.cancel.</summary>
        /// <summary>Sky, brief 046: the cancel tag is off for now ("confusing, remove it for now and I'll think of something as I play through");
        /// the cancel pop and the "cancelled" plate stay.</summary>
        public static readonly bool CancelTagOn = false;

        void CancelTagFrame(string state, IReadOnlyList<int> doers, bool fire)
        {
            var h = host; float now = Time.unscaledTime;
            if (!CancelTagOn) { if (cancelTag != null) Show(cancelTag, false); CancelTagState = ""; return; }
            if (cancelTag == null)
            {
                // brief 044 A: an ink-deep plate (94 %), a dashed 1.5 px cream edge (60 %), the kit's warning stripes at the left, ✕ and the words
                cancelTag = E(marks, null, 0, 0, float.NaN, 26); cancelTag.style.flexDirection = FlexDirection.Row; cancelTag.style.alignItems = Align.Center; cancelTag.style.paddingRight = 11; cancelTag.pickingMode = PickingMode.Ignore;
                cancelTag.style.backgroundColor = new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.94f); Round(cancelTag, 6); cancelTag.style.overflow = Overflow.Hidden;
                var st = Stripes(cancelTag, 9, 26); Round(st, 0);
                var x = E(cancelTag, null, w: 12, h: 12); x.style.flexShrink = 0; x.style.marginLeft = 7; Icon(x, "close_x", Cream);
                manifest.Add((cancelTag, "line", () => cancelTagText.text)); // a one-line label (the pill's warning), never running text
                cancelTagText = Words(cancelTag, 13.5f); cancelTagText.style.marginLeft = 7;
                var edge = P(cancelTag, 0, 0, 0, 0, (p, r) => DashedRect(p, r, 6f, new Color(Cream.r, Cream.g, Cream.b, 0.6f), 1.5f)); edge.style.right = 0; edge.style.bottom = 0; edge.style.width = StyleKeyword.Auto; edge.style.height = StyleKeyword.Auto;
            }
            string tag = null; Vector2 at = default; bool ground = false;
            if (state is "offer" or "charge" or "ready" && !fire && doers != null && doers.Count > 0) { tag = h.CancelTag(doers); var pr = PillRect(); at = new Vector2(pr.center.x, pr.yMin - 8f - 26f); }
            if ((m.Mouse - restAt).sqrMagnitude > 9f) { restAt = m.Mouse; restSince = now; }
            if (tag == null && state.Length == 0 && h.Hover == null && h.Panel == null && h.Ring == null && !m.Ended && now - restSince >= 0.3f && h.controls.HoldActs && !h.Driving && h.Picked < 0)
            {
                var sel = new List<int>(); for (int k = 0; k < 4; k++) if (h.Selected[k]) sel.Add(k);
                var p = RuntimePanelUtils.ScreenToPanel(root.panel, m.Mouse); float W = root.layout.width, H = root.layout.height;
                if (p.x >= 0f && p.y >= 0f && p.x <= W - 120f && p.y <= H - 60f && h.Hover == null) { tag = h.CancelTag(sel); at = new Vector2(p.x + 16f + 60f, p.y + 18f); ground = true; } // the cursor in the window, on open ground
            }
            bool show = tag != null && !m.Ended && h.Modal == ModalGate.Modal.None; Show(cancelTag, show); CancelTagState = show ? (ground ? "ground:" : "pill:") + tag : "";
            if (!show) return;
            Text(cancelTagText, tag); float tw = float.IsNaN(cancelTag.resolvedStyle.width) ? 160f : cancelTag.resolvedStyle.width;
            cancelTag.style.left = at.x - (ground ? 60f : tw / 2f); cancelTag.style.top = at.y; cancelTag.BringToFront(); cancelTag.Q<Paint>()?.MarkDirtyRepaint();
        }

        /// <summary>A four-point sparkle: the lit pip of an act that just opened (no text).</summary>
        static void Sparkle(Painter2D p, Vector2 c, float r, Color col)
        {
            p.fillColor = col; p.BeginPath(); p.MoveTo(c + new Vector2(0, -r)); p.LineTo(c + new Vector2(r * 0.28f, -r * 0.28f)); p.LineTo(c + new Vector2(r, 0)); p.LineTo(c + new Vector2(r * 0.28f, r * 0.28f));
            p.LineTo(c + new Vector2(0, r)); p.LineTo(c + new Vector2(-r * 0.28f, r * 0.28f)); p.LineTo(c + new Vector2(-r, 0)); p.LineTo(c + new Vector2(-r * 0.28f, -r * 0.28f)); p.ClosePath(); p.Fill();
            p.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.6f); p.lineWidth = 1f; p.Stroke();
        }
    }
}
