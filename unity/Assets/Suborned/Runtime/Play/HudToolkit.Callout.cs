using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 101 round 3, piece 7 (Glyph's callout, briefs/101/glyph/r3/quiet.png; Sky: "yes"; moved here from HudToolkit.Attention, whose placement stands): the
    /// watcher's close-up in the built cream ring over a soft shadow (a camera: its eye in the cones' red on the book's body), the watched agent's face at its lower right in
    /// the cream ring with 1-3 orange ticks round it for the stage (noticed, wary, reporting), "+n" as a street label when more wait, and a fine cream stem down to the
    /// watcher's head (HudToolkit.Attention). Docked at the screen's edge, the ink chevron points at the watcher. It retires the chrome cameo, the stage rings and the notch.</summary>
    public sealed partial class HudToolkit
    {
        sealed class Medal { public VisualElement Root, Disc, Img, Kit, KitIcon, Badge; public KeyText PlusK; public bool Speech; public Paint Ring; public HandoverHost.Callout For; public Vector2 At; public bool Placed, Docked; public int Spot = -1; public Vector2 Toward; public int Stage; }
        readonly Medal[] medals = new Medal[4];
        const float MedalD = 72f, DockD = 56f, MedalLift = 70f, ScreenPad = 12f, MedalAgent = 29f;

        Medal NewMedal(int k)
        {
            var md = new Medal();
            md.Root = E(marks, null, 0, 0, MedalD, MedalD, pick: true); md.Root.name = "callout" + k; md.Root.style.overflow = Overflow.Visible;
            md.Root.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && md.For != null) { host.CalloutClick(md.For); e.StopPropagation(); } });
            var sh = Disc(md.Root, MedalD, "e-shadow"); Pin(sh, new Vector2(0f, 3f)); sh.style.opacity = 0.75f;
            md.Disc = E(md.Root, null, 0, 0, MedalD, MedalD); Round(md.Disc, MedalD / 2); md.Disc.style.overflow = Overflow.Hidden; md.Disc.style.backgroundColor = BkPale;
            md.Img = E(md.Disc, null, 0, 0, MedalD, MedalD);
            md.Kit = E(md.Root, "e-body", 0, 0, MedalD, MedalD); Round(md.Kit, MedalD / 2); md.KitIcon = E(md.Kit, null, MedalD * 0.2f, MedalD * 0.2f, MedalD * 0.6f, MedalD * 0.6f); Show(md.Kit, false); // a camera: its eye on the book's body
            var ring = E(md.Root, "e-inline", 0, 0, MedalD, MedalD); Round(ring, MedalD / 2); BorderW(ring, 2.5f);
            md.Ring = P(md.Root, -8, -8, MedalD + 16, MedalD + 16, (p, r) => MedalRing(p, md));
            md.Badge = SFace(md.Root, MedalAgent, 2f); Pin(md.Badge, new Vector2(MedalD - MedalAgent + 4f, MedalD - MedalAgent + 2f));
            md.PlusK = KText(md.Root, 13f); md.PlusK.Root.style.position = Position.Absolute; md.PlusK.Root.style.left = MedalD - 12f; md.PlusK.Root.style.top = -10f; Show(md.PlusK.Root, false);
            manifest.Add((md.Root, "image", () => "attention callout"));
            Show(md.Root, false); return md;
        }

        /// <summary>The stage ticks round the agent's face (1 noticed, 2 wary, 3 reporting: Glyph's orange on an ink keyline, 26° each from 20° past the top), and the
        /// dock's ink chevron toward the watcher.</summary>
        void MedalRing(Painter2D p, Medal md)
        {
            var bc = new Vector2(8f + MedalD - MedalAgent + 4f + MedalAgent / 2f, 8f + MedalD - MedalAgent + 2f + MedalAgent / 2f); float br = MedalAgent / 2f + 3.4f;
            p.lineCap = LineCap.Butt;
            for (int i = 0; i < md.Stage && !md.Speech && md.Badge.style.display != DisplayStyle.None; i++)
            {
                float a0 = 20f + i * 34f - 90f, a1 = a0 + 26f;
                p.strokeColor = BkInk; p.lineWidth = 4.6f; p.BeginPath(); p.Arc(bc, br, Deg(a0), Deg(a1)); p.Stroke();
                p.strokeColor = BkOrange; p.lineWidth = 2.6f; p.BeginPath(); p.Arc(bc, br, Deg(a0), Deg(a1)); p.Stroke();
            }
            if (md.Docked && md.Toward.sqrMagnitude > 0.01f) EdgeChevron(p, new Vector2(8f + MedalD / 2f, 8f + MedalD / 2f), MedalD, md.Toward);
        }
    }
}
