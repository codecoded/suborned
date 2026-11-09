using UnityEngine;
using UnityEngine.UIElements;
using OpId = Suborned.Sim.OpId;

namespace Suborned.Play
{
    /// <summary>Brief 101 round 4, item 10 (Glyph, briefs/101/glyph/r4/notes.md §2 and board.png "The row's states"; Sky 2026-11-09: "Sure"): one verb row for the Action
    /// plate and the strip alike. A built round tab (30 px, the pictogram at 0.52) for each verb the selected agent has, 14 px after the take-control tab and 6 px apart,
    /// level with the selected face; a missing verb is left out and the rest close up.
    ///  · a click arms it (HandoverHost.Pick; a second click on Knock out flips it to Kill, as its key does); giving the order disarms it, as today; Esc plays no part;
    ///  · hovered: the built hover ring, and 6 px under the row its name in a street label (Jost Bold 13.5, ink), then " · " and its key in the soft ink ("Fire · ⇧2");
    ///  · armed: the book's deep teal body and the turquoise-light ring at 2.2 px, its name under it ("Knock out", or "Kill"); a hovered tab's name stands in for it;
    ///  · shut (Fire while carrying Popovic): the tab at 55 %, its pictogram kept;
    ///  · the Fixer's turn ways over a hovered person wear their outcome (HandoverHost.KnobPred) at the tab's lower right on the cream disc, as the built face carries
    ///    its badge: the kit badge once the temperament is known (mark_ok, mark_half), the dashed mark while the mood only suggests it (the pill's own rule);
    ///  · the Enforcer's Enter tab wears the dashed turquoise ring while the door is open to him and nothing is armed there (the old knob's ring, Glyph 016 r2 D).
    /// It shows always with the Action plate on (the pause menu's setting), else for 2.5 s after Tab or Shift as the strip did, and takes clicks while it shows. No hit
    /// chance and no time on it; keys show on hover only. At the cursor the armed verb is its pictogram keyed in ink (18 px, 14 px right of the tip and 16 under it): no
    /// disc, since a disc means a click. It retires the verb plate (its knobs and ⇧ numerals), its fold tab, stub and preference, the armed plate, the strip's plate and
    /// tiles, the takedown's way pills and the cursor's disc. The manifest has one image element, "verb row": the tabs and their words, as the take-control tab is.</summary>
    public sealed partial class HudToolkit
    {
        const float VerbTabD = 30f, VerbTabGap = 6f, VerbRowAfter = 14f, VerbWordGap = 6f, VerbWordSize = 13.5f, VerbWordH = 20f, CursorVerbD = 18f;
        sealed class VerbTab { public VisualElement Tab, Body, Badge, BadgeArt; public Paint Dashed, Ring; public string Mark = ""; public bool RingOn; }
        VisualElement verbRow, verbWords; readonly VerbTab[] verbTabs = new VerbTab[5]; KeyText verbName, verbKey; KeyIcon cursorVerb;
        int verbHot = -1; float verbRowRight;
        /// <summary>The row shows now: with the Action plate on, always; else for 2.5 s after Tab or Shift (the strip's rule). Left dock only, never under a modal or a scene.</summary>
        bool VerbRowOn { get; set; }
        /// <summary>The proof reads it: the row's tabs now ("enter fire *takedown watch": * armed, ~ shut), and the words under it; "" when it is hidden.</summary>
        public static string VerbRowShown { get; private set; } = "";
        public static string VerbRowWords => live?.verbWords is VisualElement w && Visible(w) ? (live.verbName.Words + " " + (Visible(live.verbKey.Root) ? live.verbKey.Words : "")).Trim() : "";
        /// <summary>The proof reads it: the tab of plate slot `slot` on screen (pixels, origin bottom left), null when hidden.</summary>
        public static Vector2? VerbTabScreen(int slot) => live != null && slot >= 0 && slot < live.verbTabs.Length && live.verbTabs[slot] is VerbTab t ? ScreenOf(t.Tab) : null;

        void VerbRowBuild()
        {
            verbRow = E(root, null, pick: false); verbRow.style.position = Position.Absolute; verbRow.style.height = VerbTabD; verbRow.style.overflow = Overflow.Visible;
            for (int i = 0; i < verbTabs.Length; i++)
            {
                int k = i; var v = verbTabs[i] = new VerbTab();
                var t = v.Tab = RoundTab(verbRow, VerbTabD, ""); t.style.position = Position.Absolute; t.style.top = 0; t.pickingMode = PickingMode.Position; v.Body = t.Q("body");
                t.RegisterCallback<PointerDownEvent>(e => { if (e.button != 0) return; e.StopPropagation(); host.Pick(k); }); // arms it (the takedown's second click flips the way); never Esc
                t.RegisterCallback<PointerEnterEvent>(_ => verbHot = k); t.RegisterCallback<PointerLeaveEvent>(_ => { if (verbHot == k) verbHot = -1; });
                float bd = Mathf.Round(VerbTabD * 0.42f); // the outcome on the cream disc at the lower right (the built face's badge slot)
                v.Badge = E(t, "e-page", VerbTabD - bd + 2f - 1.2f, VerbTabD - bd + 2f - 1.2f, bd + 2.4f, bd + 2.4f); Round(v.Badge, bd / 2f + 1.2f);
                v.BadgeArt = E(v.Badge, null, 1.2f, 1.2f, bd, bd); var vv = v; v.Dashed = P(v.Badge, 1.2f, 1.2f, bd, bd, (p, r) => { if (vv.Mark is "likely" or "hurt") PredGlyph(p, r.center, vv.Mark, bd); }); Show(v.Badge, false);
                v.Ring = P(t, -6f, -6f, VerbTabD + 12f, VerbTabD + 12f, (p, r) => { if (vv.RingOn) Dashed(p, r.center, VerbTabD / 2f + 3f, TurqLight, 1.6f, 12); }); // the Enter hint
                Show(t, false);
            }
            verbWords = StackRow(verbRow, VerbWordH); verbWords.style.position = Position.Absolute; verbWords.style.justifyContent = Justify.FlexStart; // inside the row's box (Glyph r4 budget rule 5)
            verbName = KText(verbWords, VerbWordSize); verbKey = KText(verbWords, VerbWordSize, "k-bold", "k-soft"); verbKey.Root.style.marginLeft = 4f;
            manifest.Add((verbRow, "image", () => "verb row")); // the tabs, and inside its box their words while hovered or armed
            manifest.Add((verbWords, "hover", () => Visible(verbKey.Root) ? (verbName.Words + " " + verbKey.Words).Trim() : verbName.Words)); // round 4 (Splice): the row's words as a hover ("Fire · ⇧2", "Knock out")
            cursorVerb = KIcon(root, "", CursorVerbD); cursorVerb.Root.style.position = Position.Absolute; manifest.Add((cursorVerb.Root, "image", () => "armed verb at the cursor"));
            Show(verbRow, false); Show(verbWords, false); Show(cursorVerb.Root, false);
        }

        /// <summary>A verb's name for the row's words: the takedown's way (Fable's words: armed, the way it gives; else the knockout a click arms), else the verb's label.</summary>
        string VerbWord(int k)
        {
            if (k == m.TakedownKnob && host.Text != null && host.Text.Actions.TryGetValue("takedown", out var td))
            {
                string way = m.TakedownArmed ? m.Way : "knockout";
                return td.Ways.TryGetValue(way, out var w) ? w.label : way == "kill" ? "Kill" : "Knock out";
            }
            return m.VerbName[k] ?? "";
        }

        /// <summary>Per frame (LiveFrame): the row beside the selected face, its states, its words, and the armed verb at the cursor.</summary>
        void VerbRowFrame()
        {
            if (verbRow == null) VerbRowBuild();
            float now = Time.unscaledTime; var g = host.Game; int a = m.Agent;
            bool strip = now - host.VerbStripAt < HandoverHost.VerbStripS;
            VerbRowOn = g != null && m.Dock == RosterDock.Left && !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None && !host.ConvoOn && a >= 0 && a < 4 && m.Slots[a].Show && (PlateOn || strip);
            Show(verbRow, VerbRowOn); if (!VerbRowOn) { verbHot = -1; Show(verbWords, false); VerbRowShown = ""; verbRowRight = 0f; }
            if (VerbRowOn)
            {
                float y = FaceScreen(a).y - VerbTabD / 2f, x0 = TakeX + TakeD + VerbRowAfter;
                verbRow.style.opacity = PlateOn ? 1f : Mathf.Clamp01((HandoverHost.VerbStripS - (now - host.VerbStripAt)) / 0.3f); // the strip's fade in its last 0.3 s
                int n = 0, lit = -1; string shown = ""; var at0 = new float[verbTabs.Length]; var on = new bool[verbTabs.Length]; // this frame's (resolvedStyle lags a frame)
                for (int k = 0; k < verbTabs.Length; k++)
                {
                    var v = verbTabs[k]; bool has = m.VerbHas[k], shut = m.VerbRefused[k];
                    if (!has && !shut) { Show(v.Tab, false); if (verbHot == k) verbHot = -1; continue; }
                    Show(v.Tab, true); on[k] = true; at0[k] = n * (VerbTabD + VerbTabGap); n++;
                    bool armed = has && host.Picked == k; if (armed) lit = k;
                    TabIcon(v.Tab, k == m.TakedownKnob && armed ? ArmedIcon() : m.VerbIcon[k]);
                    Class(v.Body, "e-deep", armed); Class(v.Body, "e-body", !armed); BorderW(v.Body, armed ? 2.2f : 2f); RingColour(v.Body, armed || verbHot == k ? TurqLight : null);
                    v.Tab.style.opacity = shut && !has ? 0.55f : 1f;
                    string mark = has && k < host.KnobPred.Length ? host.KnobPred[k] ?? "" : ""; Show(v.Badge, mark.Length > 0);
                    if (mark != v.Mark) { v.Mark = mark; if (mark is "ok" or "half") { Show(v.BadgeArt, true); Bg(v.BadgeArt, mark == "ok" ? "badges/mark_ok" : "badges/mark_half", Color.white); } else Show(v.BadgeArt, false); v.Dashed.MarkDirtyRepaint(); }
                    bool ring = k == 0 && knobOn; if (ring != v.RingOn) { v.RingOn = ring; v.Ring.MarkDirtyRepaint(); }
                    shown += (shown.Length > 0 ? " " : "") + (armed ? "*" : shut && !has ? "~" : "") + m.VerbIcon[k];
                }
                float tabsW = Mathf.Max(0, n * (VerbTabD + VerbTabGap) - VerbTabGap); verbRowRight = x0 + tabsW; VerbRowShown = shown;
                // the words, 6 px under the row: the hovered tab's name and its key in the soft ink; else the armed verb's name; centred under its tab, inside the row's box
                int at = verbHot >= 0 && verbHot < verbTabs.Length && on[verbHot] ? verbHot : lit; bool words = at >= 0;
                float left = x0, right = x0 + tabsW, wx = 0f; Show(verbWords, words);
                if (words)
                {
                    verbName.Set(VerbWord(at)); bool key = at == verbHot && (m.VerbKey[at] ?? "").Length > 0; Show(verbKey.Root, key); if (key) verbKey.Set("· " + m.VerbKey[at]);
                    float ww = verbWords.resolvedStyle.width; if (float.IsNaN(ww) || ww < 1f) ww = 80f;
                    wx = Mathf.Max(TakeX, x0 + at0[at] + VerbTabD / 2f - ww / 2f); left = Mathf.Min(left, wx); right = Mathf.Max(right, wx + ww);
                }
                verbRow.style.left = left; verbRow.style.top = y; verbRow.style.width = right - left; verbRow.style.height = words ? VerbTabD + VerbWordGap + VerbWordH : VerbTabD; verbRow.BringToFront();
                for (int k = 0; k < verbTabs.Length; k++) if (on[k]) verbTabs[k].Tab.style.left = x0 - left + at0[k];
                if (words) { verbWords.style.left = wx - left; verbWords.style.top = VerbTabD + VerbWordGap - 3f; } // the label's box sits ~3 px above its type
            }
            // the armed verb at the cursor: its pictogram keyed in ink (with the plate off; Fire's crosshair wins where a press fires; target mode's cross)
            var mp = RuntimePanelUtils.ScreenToPanel(root.panel, m.Mouse);
            bool liveCur = g != null && !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None && host.Ring == null && !host.ConvoOn && host.Panel == null && !host.PointerHeld; // Glyph's N4
            bool cross = !PlateOn && liveCur && host.Frozen && host.TargetFreeze;
            bool badge = !PlateOn && liveCur && !cross && host.Picked >= 0 && host.Picked < 5 && m.VerbHas[host.Picked] && host.CursorKind != "aim";
            Show(cursorVerb.Root, badge);
            if (badge) { cursorVerb.Set(this, ArmedIcon()); cursorVerb.Root.style.left = mp.x + 14f; cursorVerb.Root.style.top = mp.y + 16f; cursorVerb.Root.style.opacity = host.PlateKeysDim ? 0.45f : 1f; cursorVerb.Root.BringToFront(); Show(cursorMark, false); }
        }

        /// <summary>The busy chip of the selected agent stands after the verb row while it shows, else beside its portrait.</summary>
        float SelectedChipX(float plateX) => VerbRowOn ? Mathf.Max(plateX, verbRowRight + 6f) : plateX;
    }
}
