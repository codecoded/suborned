using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 101 round 3, piece 3 (Glyph's rule 3, briefs/101/glyph/r3/window.png and quiet.png; Sky: "yes"): time that runs is the tab's own cream ring.
    ///  · the window marker (moved here from HudToolkit.Done): what a window switched off (the guard away, a camera blind or looped, the rivals' next move, Popovic's
    ///    car) wears its pictogram on a 26 px timed tab whose cream ring drains with the window, the time beside it as a street label (Jost Bold 17: "55 s", m:ss past
    ///    a minute); its last 10 s orange on ink, ring and figure, as the seal. Held open by an agent (a loop the Tech holds), the ring stays whole and the holder's
    ///    face stands for the time. It retires the plate_dark marker and its draining underline;
    ///  · the attention clock (moved here from HudToolkit.Hold): over an agent standing in a watcher's sight, the open eye on a 24 px timed tab whose ring fills
    ///    toward the report, the stage word beside it (Jost Bold 14.5); from "wary" the ring and the word turn orange on ink. It retires the plate_dark clock and the
    ///    ring arc beside it.
    /// A window is a key event (brief 101: time shows only for those); an attention clock shows a stage, never seconds. It reads the host only: replays are untouched.</summary>
    public sealed partial class HudToolkit
    {
        readonly List<TimedEl> winEls = new(), clockEls = new();
        public static int WindowMarksShown { get; private set; }
        public static string WindowWords { get; private set; } = "";
        const float WinD = 26f, AttnD = 24f;

        /// <summary>Glyph 058 §3, the window's count as the proofs read it: m:ss when the window runs a minute or more, plain seconds under that.</summary>
        public static string WindowCount(int left, int total) => left < 0 ? "" : total >= 60 ? $"{left / 60}:{left % 60:00}" : left.ToString();
        /// <summary>Glyph r3 (rule 3), the time as the marker shows it: "55 s", m:ss past a minute; "" while the window has no end the cell can read.</summary>
        public static string WindowTime(int left) => left < 0 ? "" : left >= 60 ? $"{left / 60}:{left % 60:00}" : $"{left} s";

        /// <summary>The window markers, per frame before the done chips (a chip on the same thing stands over its marker: `markTop`).</summary>
        void WindowMarkers(bool on, List<Rect> bubbles, Dictionary<string, float> markTop)
        {
            var h = host; int wu = 0; var wwords = new List<string>();
            if (on)
                foreach (var w in h.WindowMarks)
                {
                    if (CueNoPlate(w.Kind)) continue; /* brief 084: no plate over anything that moves */
                    var at = h.AnchorWorld(w.Key); if (!at.HasValue) continue;
                    bool person = w.Key is "guard" or "patrol"; if (!Proj(at.Value + Vector3.up * (person ? 2.45f : w.Key.StartsWith("camera/") ? 0.75f : 1.7f), out var q)) continue;
                    while (winEls.Count <= wu) { var nk = NewTimed(marks, WinD, 17f, 6f); winEls.Add(nk); manifest.Add((nk.Root, "marker", () => nk.Word.Root.style.display == DisplayStyle.None ? "" : nk.Word.Words)); }
                    var k = winEls[wu++]; Show(k.Root, true);
                    bool face = w.Face >= 0, timed = w.Left >= 0 && w.Total > 0, hot = !face && w.Left >= 0 && w.Left <= 10;
                    string icon = w.Kind == "disable" ? "p_alert" : w.Kind == "rivals-next" ? "nextevent" : w.Kind == "vale-car" ? "car" : "eye_shut"; // Glyph 058 §3: the desk's clock wears the siren; Glyph 059: the rivals' next move, Popovic's car
                    SetTimed(k, icon, face || !timed ? 1f : Mathf.Clamp01(w.Left / (float)w.Total), hot, w.Label.Length > 0 ? w.Label : WindowTime(w.Left), face ? w.Face : -1);
                    float ww = k.Root.resolvedStyle.width; if (float.IsNaN(ww) || ww < 1f) ww = 80f;
                    float wx = q.x - ww / 2f, wb = ClearOfSpeech(bubbles, wx, ww, q.y - 18f, WinD); // centred on the thing, above its speech, never on it
                    k.Root.style.left = wx; k.Root.style.top = wb - WinD; markTop[w.Key] = wb - WinD;
                    wwords.Add($"{w.Kind}:{w.Thing}:{(face ? "face" : WindowCount(w.Left, w.Total))}");
                }
            for (int i = wu; i < winEls.Count; i++) Show(winEls[i].Root, false);
            WindowMarksShown = wu; WindowWords = string.Join(" ", wwords);
        }

        /// <summary>The attention clocks over the agents standing in a watcher's sight (the engine's Attention), in the hold marks' pass; `cu` counts those shown.</summary>
        void AttentionClocks(Game g, ref int cu)
        {
            var h = host; var (notice, wary, report) = g.AttentionMarks;
            // Glyph N3 (0.2.137): in a fight under one input the attention clock is a cut kind: the live portrait and the attention callout say it (GDD §10)
            for (int k = 0; k < 4 && !(h.OneInput && g.FightOn); k++)
            {
                var op = g.Ops[k]; if (op.Inside || op.Down || op.InCar || op.Walk != null) continue; string nm = Op.Name((OpId)k); int ticks = 0;
                foreach (var w in g.WatcherIds()) { var (st, who) = g.Attention(w); if (who == nm) ticks = Mathf.Max(ticks, g.AttentionTicks(w, nm)); }
                if (ticks < Game.Hz / 2 || h.Views?.Find("op/" + nm) is not Transform tr || !tr.gameObject.activeSelf || !Proj(tr.position + Vector3.up * 2.7f, out var aq)) continue;
                float s = ticks / (float)Game.Hz; string word = s >= report ? "reported" : s >= wary ? "wary" : s >= notice ? "noticed" : "seen";
                while (clockEls.Count <= cu) { var nk = NewTimed(marks, AttnD, 14.5f, 6f); clockEls.Add(nk); manifest.Add((nk.Root, "marker", () => nk.Word.Words)); } // Glyph N3 (0.2.137): a world marker, one word, in the manifest (G2: every street mark)
                var c = clockEls[cu++]; Show(c.Root, true);
                SetTimed(c, "eye_open", report > 0 ? Mathf.Clamp01(s / report) : 0f, word is "wary" or "reported", word);
                c.Root.style.left = aq.x - 52f; c.Root.style.top = aq.y - AttnD / 2f; // the tab where the old clock's arc stood, its word over the old plate (Glyph r3: StepAside may move it)
            }
        }
    }
}
