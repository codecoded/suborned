using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The pause menu (Esc; HostMenu), brief 051, in the light line (Glyph's briefs/051/lightline/SPEC.md "Pause menu"; Sky 2026-10-23 "A seems
    /// ok"): the smoked sheet, 360 ref wide at 12 % of the height, flat ink 90 % (it stands over the frozen game) with a 1 ref chrome border; "PAUSED"
    /// at the left, then seven words at a 44 ref pitch with one sliding marker: Resume (focused) · Options · Controls · Restart mission · Fight range
    /// (The Handover while in the range) · a 14 ref gap · Quit to menu · Quit; the build line at the bottom left. Hover = focus; Up/Down (W/S) move it,
    /// Enter/Space take it, a click takes it on release. The veil stays clear as built (a dim counts against the HUD budget). Options and Controls open
    /// the front end's sheet over the frozen game.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement menuVeil, menuCard; Label menuRange;
        FrontEnd.LightList menuList; readonly List<System.Action> menuActs = new(); int menuFocus; bool menuWasOpen; int menuOpenFrame = -1;
        float MenuW => FrontEnd.LS.pause.wRef;
        /// <summary>The pause menu's focused word (the proof's focus checks).</summary>
        public static string MenuFocusName => live?.menuList != null && live.menuFocus >= 0 && live.menuFocus < live.menuList.Items.Count ? live.menuList.Items[live.menuFocus].Root.name : "";

        void BuildMenu()
        {
            menuVeil = E(root, null, pick: true); menuVeil.style.position = Position.Absolute; menuVeil.style.left = 0; menuVeil.style.top = 0; menuVeil.style.right = 0; menuVeil.style.bottom = 0;
            menuVeil.style.backgroundColor = Color.clear; // invisible, as the briefing and the end card have none: a dim over the whole screen is HUD to the budget
            menuVeil.RegisterCallback<PointerDownEvent>(e => e.StopPropagation()); // the street never hears a click under the menu
            var ll = FrontEnd.LS; var P = ll.pause;
            var all = new List<(string text, System.Action act)>
            {
                ("Resume", () => host.ResumeFromMenu()), ("Options", () => FrontEnd.OpenFromPause(true)), ("Controls", () => FrontEnd.OpenFromPause(false)),
                ("Restart mission", () => host.RestartFromMenu()), (host.rangeMode ? "The Handover" : "Fight range", () => host.SwitchRangeScene()), // brief 029: the range from the menu, and back
                ("Quit to menu", () => host.QuitToMenuFromMenu()), ("Quit", () => host.QuitFromMenu()),
            };
            if (!(FrontEnd.DebugOptions || host.rangeMode)) all.RemoveAt(4); // Glyph's 0.2.104 review #6: the Fight Range (a lab) only in a development player or with `-debug` (from the range, the way back stays)
            var items = all.ToArray(); int quitAt = items.Length - 2;
            float listH = items.Length * P.itemH + P.endGapRef, cardH = P.padTop + P.titleH + listH + P.buildH + P.padBottom;
            menuCard = E(root, null, 0, 0, P.wRef, cardH, pick: true); menuCard.name = "pause-card";
            menuCard.style.backgroundColor = FrontEnd.SC("ink", FrontEnd.SrgbAlpha(Mathf.Max(P.fillAlpha, 0.97f))); /* Glyph's 0.2.104 review #5: opaque, so nothing (the briefing card) shows through */
            FrontEnd.Border(menuCard, ll.sheet.borderRef, FrontEnd.SC("chrome", ll.sheet.borderAlpha), ll.sheet.radiusRef); // the light line's 1 ref chrome border (0.2.108 lost it to a swallowed comment)
            var t = new Label { pickingMode = PickingMode.Ignore }; menuCard.Add(t); FrontEnd.Typeset(t, ll.type.pauseTitle, "Paused");
            t.style.position = Position.Absolute; t.style.left = P.padX - 2; t.style.top = P.padTop - 0.22f * ll.type.pauseTitle.sizeRef;
            menuList = FrontEnd.List(menuCard, P.wRef - 2 * P.padX, P.itemH, 0f, P.insetRef, P.gutterRef); menuList.Root.style.left = P.padX; menuList.Root.style.top = P.padTop + P.titleH;
            menuActs.Clear();
            for (int i = 0; i < items.Length; i++)
            {
                int ix = i; var (text, act) = items[i];
                var w = FrontEnd.ListWord(menuList, text, ll.type.wordSmall, P.lineWRef, P.lineYRef, i == quitAt ? P.endGapRef : 0f, false); w.Root.name = "pause-" + text.ToLowerInvariant().Replace(' ', '-');
                FrontEnd.Hook(w, () => MenuFocus(ix), act); menuActs.Add(act);
                if (items[i].text is "Fight range" or "The Handover") menuRange = w.Label;
            }
            var b = new Label { pickingMode = PickingMode.Ignore }; menuCard.Add(b); FrontEnd.Typeset(b, ll.type.pauseBuild, BuildStamp.Line); b.style.opacity = ll.type.pauseBuild.alpha;
            b.style.position = Position.Absolute; b.style.left = P.padX - 2; b.style.bottom = P.padBottom; // Sky 2026-10-19: which build this is (BuildStamp)
            manifest.Add((menuCard, "image", () => "pause menu"));
            Show(menuVeil, false); Show(menuCard, false);
        }

        void MenuFocus(int i)
        {
            if (menuList == null) return; menuFocus = Mathf.Clamp(i, 0, menuList.Items.Count - 1);
            for (int k = 0; k < menuList.Items.Count; k++) { menuList.Items[k].Focused = k == menuFocus; FrontEnd.PaintWord(menuList.Items[k]); }
        }

        void MenuFrame()
        {
            if (menuCard == null) return;
            bool open = host.MenuOpen && !FrontEnd.Covers; // Options or Controls opened from it stand in its place until Back
            Show(menuVeil, host.MenuOpen); Show(menuCard, open);
            if (brief != null) brief.style.visibility = host.MenuOpen ? Visibility.Hidden : Visibility.Visible; // the briefing card never stands behind the pause sheet (review #5)
            if (host.MenuOpen && !menuWasOpen) { menuOpenFrame = Time.frameCount; MenuFocus(0); } // Resume focused on open
            menuWasOpen = host.MenuOpen;
            if (!open) return;
            if (root.IndexOf(menuCard) != root.childCount - 1) { menuVeil.BringToFront(); menuCard.BringToFront(); }
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) || size.x <= 0 ? 1920 : size.x, H = float.IsNaN(size.y) || size.y <= 0 ? 1080 : size.y;
            // Glyph 0.2.85 #7: the card scales by H / 1080 like the front end (the HUD panel's whole-number scale made it 409 ref at 2880 x 1864, spec 360)
            float k = doc != null && doc.panelSettings.scale > 0 ? doc.panelSettings.scale : 1f, s = Screen.height / 1080f / k;
            menuCard.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), 0); menuCard.style.scale = new Scale(new Vector3(s, s, 1));
            menuCard.style.left = (W - MenuW) / 2; menuCard.style.top = H * FrontEnd.LS.pause.topFrac;
            // keys: Up/Down (W/S) move the marker, Enter/Space take the word (never on the frame the menu opened; Esc is the host's: it resumes)
            var kb = Keyboard.current; if (kb == null || Time.frameCount == menuOpenFrame) return;
            int n = menuList.Items.Count;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) MenuFocus((menuFocus - 1 + n) % n);
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) MenuFocus((menuFocus + 1) % n);
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) menuActs[menuFocus]?.Invoke();
        }
        /// <summary>The proof's key step on the pause menu (Down = +1), as Up/Down do.</summary>
        public static void MenuKeyMove(int d) { if (live?.menuList == null) return; int n = live.menuList.Items.Count; live.MenuFocus((live.menuFocus + d + n) % n); }
        /// <summary>The proof's hover on a pause word (the same call its PointerEnter makes).</summary>
        public static bool MenuHover(string name) { if (live?.menuList == null) return false; int i = live.menuList.Items.FindIndex(w => w.Root.name == name); if (i < 0) return false; live.MenuFocus(i); return true; }
    }
}
