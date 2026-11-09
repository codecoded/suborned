using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 101, take control (Sky 2026-11-09: "Button + eye"; Glyph's spec, briefs/101/glyph/tc/notes.md and board.png): the ground view gets a click, so T is no
    /// longer the only way in. One slot beside a face, right of her pips and level with her face (screen px at 1080: x 100, 30 px):
    ///  · on the map, a round tab (the built RoundTab) with the take_control pictogram, beside the agent T would take (HandoverHost.TakeWho) and beside any face the pointer
    ///    is on; none while she is down, inside or in a car. Hovered: its ring turquoise-light, and "Take control · T" as a street label, the key in the soft ink. A click takes
    ///    control of that face's agent (HandoverHost.TakeControlOf: TakeInSquad, then DriveEnter, as T);
    ///  · in third person the slot is a capsule (it has a word): the map pictogram and "Back to map". Hovered, its inline turquoise-light and the T key beside it in the soft
    ///    ink, never Esc (Sky 2026-11-09: "esc is always pause menu"). A click is BackToMap: the 0.5 s glide back, her order still running;
    ///  · over the driven agent, where the selection chevron was, one keyed eye (GDD §10, who sees you): shut in ink, unseen; open in the cones' red, seen; "?" in ink, a
    ///    watcher the cell hasn't learned could see her (HandoverHost.SightEye: the ring's seen read where she stands, twice a second). No chevron over her while driven.
    /// It reads the host only: nothing the engine records changes.</summary>
    public sealed partial class HudToolkit
    {
        /// <summary>The slot's left edge, the tab's size and the gap to its words (screen px at 1080): 7 px right of the pips (x 86 to 93), centred on the face.</summary>
        const float TakeX = 100f, TakeD = 30f, TakeGap = 8f, SightEyeD = 24f;
        /// <summary>Seconds a face's tab stays up after the pointer leaves the face, so the pointer can cross the pips to it.</summary>
        const float TakeLingerS = 0.6f;
        sealed class TakeTab { public VisualElement Root, Tab, Body; public KeyText Word, Key; }
        readonly TakeTab[] takeTabs = new TakeTab[4];
        readonly float[] takeSeenAt = { -9f, -9f, -9f, -9f };
        Capsule backCap; VisualElement backRow; KeyText backKey; int backFace = -1; bool backHot;
        /// <summary>The face whose tab the pointer is on (-1 none).</summary>
        int takeHot = -1;
        KeyIcon sightEye; int sightEyeFrame = -1;

        /// <summary>The proof reads them: the faces wearing a tab now ("scout fixer"), the hovered tab's words, the capsule's key on hover, the eye over the driven agent
        /// ("scout:unk"); "" for none.</summary>
        public static string TakeShown { get; private set; } = "";
        public static string TakeWordShown => live?.takeHot is int k && k >= 0 && live.takeTabs[k] is TakeTab t && Visible(t.Word.Root) ? $"{t.Word.Words} {t.Key.Words}" : "";
        public static string BackKeyShown => live?.backKey is KeyText bk && Visible(bk.Root) ? bk.Words : "";
        public static string SightEyeShown { get; private set; } = "";
        /// <summary>The proof reads them: where face i's tab, or the capsule, is on screen (pixels, origin bottom left), null when hidden.</summary>
        public static Vector2? TakeTabScreen(int i) => live != null && i >= 0 && i < 4 && live.takeTabs[i] is TakeTab t ? ScreenOf(t.Tab) : null;
        public static Vector2? BackCapsuleScreen => live?.backCap is Capsule c ? ScreenOf(c.Root) : null;
        /// <summary>The proof reads it: face i's photo in the column on screen (pixels, origin bottom left), where a pointer shows that face's tab; null when hidden.</summary>
        public static Vector2? TakeFaceScreen(int i) => live != null && i >= 0 && i < 4 && live.colWins[i] is Window w ? ScreenOf(w.Portrait) : null;
        static Vector2? ScreenOf(VisualElement b)
        {
            if (b == null || !Visible(b)) return null;
            var c = b.worldBound.center; var sz = b.panel.visualTree.layout.size; return new Vector2(c.x / sz.x * Screen.width, Screen.height - c.y / sz.y * Screen.height);
        }

        /// <summary>The tab or the capsule beside face i is under the pointer: the face's own label row gives way to its words.</summary>
        bool TakeHot(int i) => takeHot == i || backHot && backFace == i;
        /// <summary>Where face i's label row starts (screen px at 1080): after the tab's slot, or after the capsule while it stands beside her.</summary>
        float TakeRowX(int i)
        {
            float w = backCap != null && backFace == i && Visible(backCap.Root) ? backCap.Root.resolvedStyle.width : float.NaN;
            return TakeX + (float.IsNaN(w) || w <= 0f ? TakeD : w) + TakeGap;
        }

        void TakeBuild()
        {
            for (int i = 0; i < 4; i++)
            {
                var t = takeTabs[i] = new TakeTab(); int k = i;
                var r = t.Root = StackRow(root, TakeD); r.style.position = Position.Absolute; r.style.justifyContent = Justify.FlexStart; r.style.left = TakeX; r.style.top = FaceScreen(i).y - TakeD / 2f;
                t.Tab = RoundTab(r, TakeD, "take_control", pick: true); t.Body = t.Tab.Q("body");
                t.Word = KText(r, 15f); t.Word.Root.style.marginLeft = TakeGap; t.Key = KText(r, 15f, "k-bold", "k-soft"); t.Key.Root.style.marginLeft = 7f;
                t.Tab.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.TakeControlOf(k); e.StopPropagation(); } });
                t.Tab.RegisterCallback<PointerEnterEvent>(_ => takeHot = k); t.Tab.RegisterCallback<PointerLeaveEvent>(_ => { if (takeHot == k) takeHot = -1; });
                manifest.Add((r, "image", () => "take control tab")); // the tab, and its words while hovered: a button and its label, as the Squad tab's word was
                Show(r, false);
            }
            backRow = StackRow(root, TakeD); backRow.style.position = Position.Absolute; backRow.style.justifyContent = Justify.FlexStart; backRow.style.left = TakeX;
            var c = backCap = NewCapsule(backRow, TakeD, false, () => host.BackToMap());
            c.Icon.style.width = c.Icon.style.height = TakeD * 0.52f; Icon(c.Icon, "map", Cream); // Glyph: the tab's pictogram size, the word Jost Bold 14, 9 px in and 13 px out
            c.Word.style.fontSize = 14f; c.Word.style.marginLeft = 6f; c.Root.style.paddingRight = 13f;
            if (c.Root.Q(className: "e-shadow") is VisualElement sh) { sh.style.top = 2f; sh.style.bottom = -2f; } // the tab's shadow, 2 px down: one slot, one family
            backKey = KText(backRow, 14f, "k-bold", "k-soft"); backKey.Root.style.marginLeft = TakeGap;
            c.Root.RegisterCallback<PointerEnterEvent>(_ => backHot = true); c.Root.RegisterCallback<PointerLeaveEvent>(_ => backHot = false);
            manifest.Add((backRow, "image", () => "back to map capsule"));
            Show(backRow, false);
        }

        /// <summary>Per frame (HoldMarks, after the Squad tab; `on`: the hold HUD is up): the tabs on the map, the capsule in third person, the eye's leftovers.</summary>
        void TakeControlFrame(bool on)
        {
            if (takeTabs[0] == null) TakeBuild();
            var h = host; float now = Time.unscaledTime; bool left = on && m.Dock == RosterDock.Left && ModalGate.AllowsClicks(h.Modal) && !h.ConvoOn; // never under a modal or the conversation (T is not heard there either)
            bool map = left && !h.Following; int who = map ? h.TakeWho : -1; string shown = "";
            for (int i = 0; i < 4; i++)
            {
                var t = takeTabs[i]; if (rosterHover == i || takeHot == i) takeSeenAt[i] = now;
                bool show = map && h.CanTake(i) && (i == who || now - takeSeenAt[i] < TakeLingerS);
                if (!show) { if (takeHot == i) takeHot = -1; if (t.Root.style.display != DisplayStyle.None) RingColour(t.Body, null); Show(t.Root, false); continue; } // hidden under the pointer: no hover is left lit
                Show(t.Root, true); shown += (shown.Length > 0 ? " " : "") + Op.Name((OpId)i);
                bool hot = takeHot == i; Show(t.Word.Root, hot); Show(t.Key.Root, hot);
                if (hot) { t.Word.Set(h.TakeControlWord); t.Key.Set("· " + h.controls.Label(PlayAction.FollowView)); }
            }
            TakeShown = shown;
            // third person: the capsule beside the followed agent's face (no click through the glide back)
            int bf = left && h.Following && !h.FollowExiting ? h.FollowAgent : -1;
            if (bf < 0 || bf > 3) { if (backFace >= 0) RingColour(backCap.Inline, null); backFace = -1; backHot = false; Show(backRow, false); }
            else
            {
                if (bf != backFace) { backFace = bf; backRow.style.top = FaceScreen(bf).y - TakeD / 2f; }
                Show(backRow, true); Text(backCap.Word, h.BackToMapWord);
                Show(backKey.Root, backHot); if (backHot) backKey.Set(h.controls.Label(PlayAction.FollowView));
            }
            if (sightEye != null && sightEyeFrame != Time.frameCount) { Show(sightEye.Root, false); SightEyeShown = ""; } // StreetLess drew no eye this frame
        }

        /// <summary>Per frame in StreetLess (brief 083 note 5, "Needs to be more clear which character you're controlling"): over each selected agent's head, in both views, a
        /// turquoise-light chevron (the HUD's live accent, not a fire, cone or guard signal; an ink-deep edge carries it over pale paving and in shade), with the thick ground
        /// ring (HostViews.CellRings) and the face's ring. Brief 101: over the driven agent the eye stands there instead.</summary>
        void SelectionMarks()
        {
            SelectedCarets = 0; int driven = host.DriveAgent; string eye = "";
            for (int i = 0; i < 4; i++)
            {
                if (!(host.Selected[i] || i == driven) || !m.Marks[i].World.HasValue || !Proj(m.Marks[i].World.Value + Vector3.up * 2.02f, out var caretAt)) continue;
                if (i == driven) { eye = SightEyeAt(i, caretAt); continue; }
                SelectedCarets++; var cc = caretAt;
                ActPaint(pp => { void Tri(float g, Color c) { pp.fillColor = c; pp.BeginPath(); pp.MoveTo(cc + new Vector2(-11f - g, -8f - g * 0.6f)); pp.LineTo(cc + new Vector2(11f + g, -8f - g * 0.6f)); pp.LineTo(cc + new Vector2(0f, 7f + g)); pp.ClosePath(); pp.Fill(); } Tri(2f, InkDeep); Tri(0f, TurqLight); });
            }
            SightEyeShown = eye; if (eye.Length > 0) sightEyeFrame = Time.frameCount; else if (sightEye != null) Show(sightEye.Root, false);
        }

        /// <summary>The eye over agent k, centred at `at` (panel px), in the act marks' layer (the follow view keeps it): "scout:unk", "" when the host has no read.</summary>
        string SightEyeAt(int k, Vector2 at)
        {
            string s = host.SightEye(k); if (s.Length == 0) return "";
            if (sightEye == null) { ActMarkRootEnsure(); sightEye = KIcon(actMarkRoot, "", SightEyeD); sightEye.Root.style.position = Position.Absolute; manifest.Add((sightEye.Root, "image", () => "sight eye")); }
            sightEye.Set(this, "eye_" + s); sightEye.Face.style.unityBackgroundImageTintColor = s == "open" ? Red : BkInk;
            Show(sightEye.Root, true); sightEye.Root.style.left = at.x - SightEyeD / 2f; sightEye.Root.style.top = at.y - SightEyeD / 2f;
            return $"{Op.Name((OpId)k)}:{s}";
        }
    }
}
