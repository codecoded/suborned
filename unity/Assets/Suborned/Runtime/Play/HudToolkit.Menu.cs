using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The pause menu (Esc; HostMenu): the kit's modal panel and text buttons, centred over a clear veil that takes every click. Three
    /// buttons, labels only (no keycaps: Sky), a one-word title. Built with the less-reading HUD, drawn above everything while it is up.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement menuVeil, menuCard;
        const float MenuW = 300;

        void BuildMenu()
        {
            menuVeil = E(root, null, pick: true); menuVeil.style.position = Position.Absolute; menuVeil.style.left = 0; menuVeil.style.top = 0; menuVeil.style.right = 0; menuVeil.style.bottom = 0;
            menuVeil.style.backgroundColor = Color.clear; // invisible, as the briefing and the end card have none: a dim over the whole screen is HUD to the budget
            menuVeil.RegisterCallback<PointerDownEvent>(e => e.StopPropagation()); // the street never hears a click under the menu
            menuCard = E(root, "s-panel_dark_modal", 0, 0, MenuW, float.NaN, pick: true);
            menuCard.style.paddingLeft = 30; menuCard.style.paddingRight = 30; menuCard.style.paddingTop = 24; menuCard.style.paddingBottom = 30; menuCard.style.alignItems = Align.Center;
            var t = L(menuCard, "briefing-title"); t.text = "Paused";
            void Btn(string text, System.Action act)
            {
                var b = E(menuCard, "s-btn_text_normal btn_text", w: 220, h: 40, pick: true); b.style.marginTop = 12; b.style.flexDirection = FlexDirection.Row; b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
                b.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { act(); e.StopPropagation(); } });
                var l = L(b, "button-label"); l.text = text;
            }
            Btn("Resume", () => host.ResumeFromMenu());
            Btn("Restart mission", () => host.RestartFromMenu());
            Btn("Quit", () => host.QuitFromMenu());
            manifest.Add((menuCard, "image", () => "pause menu"));
            Show(menuVeil, false); Show(menuCard, false);
        }

        void MenuFrame()
        {
            if (menuCard == null) return;
            bool open = host.MenuOpen; Show(menuVeil, open); Show(menuCard, open);
            if (!open) return;
            if (root.IndexOf(menuCard) != root.childCount - 1) { menuVeil.BringToFront(); menuCard.BringToFront(); }
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) || size.x <= 0 ? 1920 : size.x, H = float.IsNaN(size.y) || size.y <= 0 ? 1080 : size.y;
            menuCard.style.left = (W - MenuW) / 2; menuCard.style.top = H * 0.3f;
        }
    }
}
