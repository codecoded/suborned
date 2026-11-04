using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 096, Glyph's key-only fixes on the HUD (2-hud-book.png ②③, 2d, 2e): the pill's pause glyph becomes a button (a hover ring; lit turquoise while the
    /// clock is stopped; a click pauses and resumes), and two turn buttons sit on the scope's rim (the kit's turn_view icon on disc_dark_24; Q and E). The host does what
    /// the keys do (HostKeys096). The fire line's Stop is in HudToolkit.Fire.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement pauseBtn, turnLeft, turnRight;

        void BuildKeys096()
        {
            // the pause glyph inside a round button where it stood in the pill
            pauseBtn = E(null, null, w: 24, h: 24, pick: true); Round(pauseBtn, 12f); pauseBtn.style.marginLeft = 8; pauseBtn.style.flexShrink = 0; pauseBtn.name = "pause-button";
            pill.Insert(pill.IndexOf(pauseIcon), pauseBtn); pauseBtn.Add(pauseIcon); pauseIcon.style.position = Position.Absolute; pauseIcon.style.left = 4.5f; pauseIcon.style.top = 4.5f; pauseIcon.style.marginLeft = 0;
            pauseBtn.RegisterCallback<PointerEnterEvent>(_ => Border(pauseBtn, 1.5f, TurqLight)); pauseBtn.RegisterCallback<PointerLeaveEvent>(_ => Border(pauseBtn, 0f, TurqLight));
            pauseBtn.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.PauseClick(); e.StopPropagation(); } });
            // the turn buttons on the scope's top corners, half over its rim
            for (int i = 0; i < 2; i++)
            {
                int q = i == 0 ? -1 : 1; var b = E(scope, "s-disc_dark_24", i == 0 ? -10f : ScopeW - 20f, -12f, 30f, 30f, pick: true); b.name = i == 0 ? "turn-left" : "turn-right";
                var ic = E(b, null, 7f, 7f, 16f, 16f); Icon(ic, "turn_view", Cream); if (i == 1) ic.style.scale = new Scale(new Vector3(-1f, 1f, 1f));
                b.RegisterCallback<PointerEnterEvent>(_ => b.style.unityBackgroundImageTintColor = new Color(0.75f, 1f, 1f)); b.RegisterCallback<PointerLeaveEvent>(_ => b.style.unityBackgroundImageTintColor = Color.white);
                b.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.TurnClick(q); e.StopPropagation(); } });
                if (i == 0) turnLeft = b; else turnRight = b;
                manifest.Add((b, "image", () => "turn button"));
            }
            scope.style.overflow = Overflow.Visible;
        }

        /// <summary>Per frame with the pill: the button shows while the run is on; the glyph turquoise while the clock is stopped.</summary>
        void Keys096Frame(bool ended)
        {
            Show(pauseBtn, !ended); Show(pauseIcon, !ended); pauseIcon.style.unityBackgroundImageTintColor = host.Paused || host.Frozen ? TurqLight : Cream;
        }
    }
}
