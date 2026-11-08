using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 096, Glyph's key-only fixes on the HUD (2-hud-book.png ②③, 2d, 2e): the pause glyph is a button (built with the clock since brief 101, HudToolkit.Clock;
    /// lit turquoise while the clock is stopped; a click pauses and resumes), and two turn buttons sit on the scope's rim (brief 101 step 5: the street tab, 32 px, the turn_view icon; Q and E). The host does what
    /// the keys do (HostKeys096). The fire line's Stop is in HudToolkit.Fire.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement pauseBtn, turnLeft, turnRight;
        /// <summary>The scope's tabs on its rim (brief 101 step 5: 32 px, the mock's).</summary>
        const float ScopeTab = 32f;

        void BuildKeys096()
        {
            // the pause button is the clock's (brief 101: HudToolkit.Clock's round button before the time)
            // the turn buttons on the lens's rim, up left and up right (the mock's -145 and -35 degrees)
            for (int i = 0; i < 2; i++)
            {
                int q = i == 0 ? -1 : 1; var b = Tab(scope, ScopeTab, "turn_view", out var ic); b.name = i == 0 ? "turn-left" : "turn-right"; Pin(b, ScopeTabAt(ScopeTabAngles[i == 0 ? 0 : 2], ScopeTab));
                if (i == 1) ic.style.scale = new Scale(new Vector3(-1f, 1f, 1f));
                b.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.TurnClick(q); e.StopPropagation(); } });
                if (i == 0) turnLeft = b; else turnRight = b;
                manifest.Add((b, "image", () => "turn button"));
            }
            scope.style.overflow = Overflow.Visible;
            BuildConesButton(); // the cones round: the eye button between them (HudToolkit.Sight)
        }

        /// <summary>Per frame with the clock: the button shows while the run is on; the glyph turquoise while the clock is stopped.</summary>
        void Keys096Frame(bool ended)
        {
            Show(pauseBtn, !ended); Show(pauseIcon, !ended); pauseIcon.style.unityBackgroundImageTintColor = host.Paused || host.Frozen ? TurqLight : Cream;
            SightFrame(ended); // the cones round: the eye button's state and the seen counts (HudToolkit.Sight)
        }
    }
}
