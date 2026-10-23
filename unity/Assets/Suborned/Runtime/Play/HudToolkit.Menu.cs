using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The pause menu (Esc; HostMenu), brief 051's rework (Glyph's spec briefs/051/layout/SPEC.md "The pause menu rework"; Sky: "which needs
    /// a lot of work"): the kit's modal card, 360 ref wide at 12 % of the height, "Paused", and seven cream buttons: Resume · Options · Controls ·
    /// Restart mission · Fight range (The Handover while in the range) · Quit to menu · Quit; the build line under them. The twelve playground toggles
    /// it used to step now live, labelled, on the Options pages (Gameplay and Camera), each with its saved value (settings.json). The veil stays clear
    /// as built (a dim counts against the HUD budget). Options and Controls open the front end's panel over the frozen game.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement menuVeil, menuCard, menuResumeRing; Label menuRange;
        const float MenuW = 360, MenuBtnW = 292, MenuBtnH = 46, MenuGap = 10;

        void BuildMenu()
        {
            menuVeil = E(root, null, pick: true); menuVeil.style.position = Position.Absolute; menuVeil.style.left = 0; menuVeil.style.top = 0; menuVeil.style.right = 0; menuVeil.style.bottom = 0;
            menuVeil.style.backgroundColor = Color.clear; // invisible, as the briefing and the end card have none: a dim over the whole screen is HUD to the budget
            menuVeil.RegisterCallback<PointerDownEvent>(e => e.StopPropagation()); // the street never hears a click under the menu
            menuCard = E(root, "s-panel_dark_modal", 0, 0, MenuW, float.NaN, pick: true);
            menuCard.style.paddingLeft = 34; menuCard.style.paddingRight = 34; menuCard.style.paddingTop = 28; menuCard.style.paddingBottom = 26; menuCard.style.alignItems = Align.Center;
            var fd = FrontEnd.Shared; // Glyph 0.2.85 #7: the front end's type: "PAUSED" in panelTitle, the buttons in button
            var t = new Label { pickingMode = PickingMode.Ignore }; menuCard.Add(t); FrontEnd.Typeset(t, fd.type.panelTitle, "Paused"); t.style.marginBottom = 20 - MenuGap;
            Label Btn(string text, System.Action act)
            {
                var b = E(menuCard, "s-btn_text_normal btn_text", w: MenuBtnW, h: MenuBtnH, pick: true); b.style.marginTop = MenuGap; b.style.flexDirection = FlexDirection.Row; b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
                b.style.unitySliceScale = MenuBtnH / 80f; // Glyph 0.2.85 #3: the plate's tips follow its height (kept across the hover class)
                b.RegisterCallback<PointerEnterEvent>(_ => { b.RemoveFromClassList("s-btn_text_normal"); b.AddToClassList("s-btn_text_hover"); });
                b.RegisterCallback<PointerLeaveEvent>(_ => { b.RemoveFromClassList("s-btn_text_hover"); b.AddToClassList("s-btn_text_normal"); });
                b.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { act(); e.StopPropagation(); } });
                var l = new Label { pickingMode = PickingMode.Ignore }; b.Add(l); FrontEnd.Typeset(l, fd.type.button, text); b.name = "pause-" + text.ToLowerInvariant().Replace(' ', '-'); return l;
            }
            var resume = Btn("Resume", () => host.ResumeFromMenu()).parent;
            // the default focus ring on Resume (Glyph 0.2.85 #8: the HUD's "ready" ring, its radius on the chamfer's line)
            menuResumeRing = E(resume); menuResumeRing.style.position = Position.Absolute; menuResumeRing.style.left = -4; menuResumeRing.style.top = -4; menuResumeRing.style.right = -4; menuResumeRing.style.bottom = -4;
            { var lc = FrontEnd.Hex(fd.colors.live); float rw = fd.button.focus.ringRef, rr = MenuBtnH * 0.3f + 4;
              menuResumeRing.style.borderTopWidth = menuResumeRing.style.borderBottomWidth = menuResumeRing.style.borderLeftWidth = menuResumeRing.style.borderRightWidth = rw;
              menuResumeRing.style.borderTopColor = menuResumeRing.style.borderBottomColor = menuResumeRing.style.borderLeftColor = menuResumeRing.style.borderRightColor = lc;
              menuResumeRing.style.borderTopLeftRadius = menuResumeRing.style.borderTopRightRadius = menuResumeRing.style.borderBottomLeftRadius = menuResumeRing.style.borderBottomRightRadius = rr;
              menuResumeRing.style.backgroundColor = new Color(lc.r, lc.g, lc.b, fd.button.focus.glowAlpha * 0.25f); }
            Btn("Options", () => FrontEnd.OpenFromPause(true));
            Btn("Controls", () => FrontEnd.OpenFromPause(false));
            Btn("Restart mission", () => host.RestartFromMenu());
            menuRange = Btn(host.rangeMode ? "The Handover" : "Fight range", () => host.SwitchRangeScene()); // brief 029: the range from the menu, and back
            Btn("Quit to menu", () => host.QuitToMenuFromMenu());
            Btn("Quit", () => host.QuitFromMenu());
            BuildLine(menuCard, corner: false); // Sky 2026-10-19: which build this is (BuildStamp)
            manifest.Add((menuCard, "image", () => "pause menu"));
            Show(menuVeil, false); Show(menuCard, false);
        }

        void MenuFrame()
        {
            if (menuCard == null) return;
            bool open = host.MenuOpen && !FrontEnd.Covers; // Options or Controls opened from it stand in its place until Back
            Show(menuVeil, host.MenuOpen); Show(menuCard, open);
            if (!open) return;
            if (root.IndexOf(menuCard) != root.childCount - 1) { menuVeil.BringToFront(); menuCard.BringToFront(); }
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) || size.x <= 0 ? 1920 : size.x, H = float.IsNaN(size.y) || size.y <= 0 ? 1080 : size.y;
            // Glyph 0.2.85 #7: the card scales by H / 1080 like the front end (the HUD panel's whole-number scale made it 409 ref at 2880 x 1864, spec 360)
            float k = doc != null && doc.panelSettings.scale > 0 ? doc.panelSettings.scale : 1f, s = Screen.height / 1080f / k;
            menuCard.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), 0); menuCard.style.scale = new Scale(new Vector3(s, s, 1));
            menuCard.style.left = (W - MenuW) / 2; menuCard.style.top = H * 0.12f;
        }
    }
}
