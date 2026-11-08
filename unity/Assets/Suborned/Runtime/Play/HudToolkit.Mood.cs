using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Engine 0.28/0.29 on the HUD (brief 054 §10, Glyph's hud29.spec.json; next-batch item 19):
    ///  · the mood disc over a cast person (Game.MoodOf: friendly, uneasy, scared; neutral and unknown draw nothing): the kit's `disc_dark_24` on a
    ///    32 px element, the face 17 px in cream, beside the person's pip with a 3 px gap (two icons at most over a person);
    ///  · the prediction plate on the price pill (Quote.Likely / Hurt): a `plate_dark` 22 px at slice 0.37 with a dashed 15 px ring holding a
    ///    tick and the word "likely", or a half disc and no word; after Listen the kit's solid `mark_ok` / `mark_half`, no word, no dash;
    ///  · the same dashed marks 15 px at the lower right of the verb knobs, no word.
    /// Colours: cream and ink-deep only (kit.uss). It reads the host only.</summary>
    public sealed partial class HudToolkit
    {
        /// <summary>hud29 moodDisc: the element, the face inside it (0.53 of the element), the gap to the pip.</summary>
        const float MoodEl = 32f, MoodFace = 17f, MoodGap = 3f;
        /// <summary>hud29 predictionMarks: the plate's height, its slice scale, the ring, the word.</summary>
        const float PredH = 28f, PredSlice = 0.37f, PredRing = 15f, PredWord = 12.5f; // Glyph's 0.2.104 review: the kit plate draws about 6 px inside its box, so a 28 px element reads as the spec's 22 px plate round a 15 px mark

        /// <summary>The mood disc beside a pip (a child of the pip's root, so it moves with it); built once per pip.</summary>
        static VisualElement MoodDiscOf(VisualElement pipRoot)
        {
            var d = pipRoot.Q("mood"); if (d != null) return d;
            d = E(pipRoot, "e-body e-inline", 26f + MoodGap, 2f, 22f, 22f); d.name = "mood"; d.pickingMode = PickingMode.Ignore; Round(d, 11f); BorderW(d, 2f); // brief 101 round 3: a small round tab (the book's body, the cream ring)
            var f = E(d, null, 4f, 4f, 14f, 14f); f.name = "face"; f.pickingMode = PickingMode.Ignore;
            Show(d, false); return d;
        }

        /// <summary>Per pip, per frame: the mood the cell knows for the role ("" and neutral: no disc).</summary>
        readonly Dictionary<VisualElement, string> moodShown = new();
        void MoodFrame(VisualElement pipRoot, string role)
        {
            var d = MoodDiscOf(pipRoot); string mood = "";
            if (role != "councillor" && host.Game != null) try { mood = host.Game.MoodOf(role); } catch (System.Exception) { mood = ""; }
            bool on = mood is "friendly" or "uneasy" or "scared";
            Show(d, on); if (!on) { moodShown.Remove(d); return; }
            if (moodShown.TryGetValue(d, out var was) && was == mood) return; moodShown[d] = mood; // Redline 064 #6: set when it changes, not every frame
            Icon(d.Q("face"), "mood_" + mood, Cream);
        }

        /// <summary>A prediction mark painted in a box: "likely" (dashed ring, tick), "hurt" (dashed ring, half disc). The ring is 15 px across,
        /// cream 1.5 px, 3.1 px on and 2.3 off.</summary>
        static void PredGlyph(Painter2D p, Vector2 c, string mark, float size = PredRing)
        {
            float w = 1.5f * size / PredRing, r = size / 2f - w / 2f, circ = 2f * Mathf.PI * r, on = 3.1f * size / PredRing, off = 2.3f * size / PredRing;
            int n = Mathf.Max(4, Mathf.RoundToInt(circ / (on + off))); float step = 360f / n, onDeg = step * on / (on + off);
            p.strokeColor = Cream; p.lineWidth = w; p.lineCap = LineCap.Butt;
            for (int i = 0; i < n; i++) { p.BeginPath(); p.Arc(c, r, Deg(-90f + i * step), Deg(-90f + i * step + onDeg)); p.Stroke(); }
            float k = size / PredRing;
            if (mark == "likely")
            {   // an 8 px tick
                p.lineWidth = 1.6f * k; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round; p.BeginPath();
                p.MoveTo(c + new Vector2(-3.4f, 0.1f) * k); p.LineTo(c + new Vector2(-1.1f, 2.5f) * k); p.LineTo(c + new Vector2(3.5f, -2.6f) * k); p.Stroke();
            }
            else
            {   // a half disc (the left half filled, a hairline round the right)
                p.fillColor = Cream; p.BeginPath(); p.MoveTo(c + new Vector2(0f, -3.6f * k)); p.Arc(c, 3.6f * k, Deg(-90f), Deg(90f), ArcDirection.CounterClockwise); p.ClosePath(); p.Fill();
                p.lineWidth = 1f * k; p.BeginPath(); p.Arc(c, 3.6f * k, Deg(-90f), Deg(90f)); p.Stroke();
            }
        }

        VisualElement vpillPred, vpillPredKit; Paint vpillPredRing; Label vpillPredWord; string vpillPredMark = "";
        /// <summary>Proofs only: the pill's prediction mark forced ("likely", "hurt", "ok", "half"), to picture a state the run does not reach; "" off.</summary>
        public static string PredForce = "";

        /// <summary>The prediction plate on the pill, after the words and before the faces (built with the pill).</summary>
        void BuildPillPred()
        {
            vpillPred = PlateDark(vpill, PredH, PredSlice); vpillPred.style.marginLeft = 9; vpillPred.style.paddingLeft = 5; vpillPred.style.paddingRight = 6;
            vpillPredRing = P(vpillPred, 0, 0, PredRing, PredRing, (p, r) => PredGlyph(p, r.center, vpillPredMark)); vpillPredRing.style.position = Position.Relative; vpillPredRing.style.flexShrink = 0;
            vpillPredKit = E(vpillPred, null, w: PredRing, h: PredRing); vpillPredKit.style.flexShrink = 0;
            vpillPredWord = Words(vpillPred, PredWord); vpillPredWord.text = host.Text?.MoodLikely ?? "likely"; /* Fable: less.mood.likely */ vpillPredWord.style.marginLeft = 4;
            Show(vpillPred, false);
        }

        /// <summary>Per pill change: the mark ("likely", "hurt", "ok", "half", "" none).</summary>
        void PillPred(string mark)
        {
            if (vpillPred == null) return; if (PredForce.Length > 0) mark = PredForce;
            mark ??= ""; Show(vpillPred, mark.Length > 0); if (mark.Length == 0) { vpillPredMark = ""; return; }
            bool exact = mark is "ok" or "half";
            Show(vpillPredRing, !exact); Show(vpillPredKit, exact); if (exact) Bg(vpillPredKit, mark == "ok" ? "badges/mark_ok" : "badges/mark_half", Color.white);
            Show(vpillPredWord, mark == "likely"); vpillPred.style.paddingRight = mark == "likely" ? 8 : 5;
            if (vpillPredMark != mark) { vpillPredMark = mark; vpillPredRing.MarkDirtyRepaint(); }
        }

        readonly Paint[] knobPred = new Paint[4]; readonly string[] knobPredShown = { "", "", "", "" };

        /// <summary>Per frame with the plate: the dashed marks at each knob's lower right (the host's KnobPred, priced on hover change).</summary>
        void KnobPredFrame()
        {
            for (int k = 0; k < 4; k++)
            {
                if (knobs[k] == null) continue;
                if (knobPred[k] == null) { int kk = k; knobPred[k] = P(knobs[k], 48f - PredRing - 1f, 48f - PredRing - 1f, PredRing, PredRing, (p, r) => { var mk = knobPredShown[kk]; if (mk is "ok" or "half") return; PredGlyph(p, r.center, mk); }); }
                string mark = m.VerbHas[k] && k < host.KnobPred.Length ? host.KnobPred[k] ?? "" : "";
                if (mark is "ok" or "half") { knobPred[k].style.backgroundImage = new StyleBackground(Sp(mark == "ok" ? "badges/mark_ok" : "badges/mark_half")); }
                else knobPred[k].style.backgroundImage = StyleKeyword.None;
                Show(knobPred[k], mark.Length > 0);
                if (knobPredShown[k] != mark) { knobPredShown[k] = mark; knobPred[k].MarkDirtyRepaint(); }
            }
        }
    }
}
