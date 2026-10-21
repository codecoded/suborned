using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The hover marker says what a right click does (Sky 2026-10-21, over Inez: "I thought this marker would change to show what action
    /// right click would cause?"). A 26 px dark disc at the front edge of the target's ground ring carries the glyph of the way a right click
    /// lights in the ring (ContextRank's pick, the hover chip's own way; the armed action when one is picked; Fire on a hostile), so the ring
    /// under the target and its action read as one mark, at play zoom and in third person. Beside the disc, satellites without words: "⋯" when
    /// the ring has other ways, footprints when the agent walks there first, the holstered mark when Fire draws first. Nothing to do there
    /// (another agent's act, a passer-by): the plain ring, no disc. In the follow view the ring is drawn in the view's own perspective under
    /// the target (the hover, or the driven agent's focus), 0.6 m round.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement actBadge, actDisc, actGlyph, actWalk, actDraw; Paint actMore;
        readonly List<Vector2> followHoverRing = new();
        public const float ActDisc = 26f;
        /// <summary>Shots only (ShotRunner's `mark: false`): the marker is not drawn, the plain hover ring as before (a before shot).</summary>
        public static bool ActMarkOff;

        void BuildActMark()
        {
            actBadge = E(marks, null, 0, 0, 0, 0); actBadge.style.overflow = Overflow.Visible; actBadge.pickingMode = PickingMode.Ignore;
            actDisc = E(actBadge, "s-disc_dark_24", -ActDisc / 2, -ActDisc / 2, ActDisc, ActDisc);
            actGlyph = E(actDisc, null, 5, 5, ActDisc - 10, ActDisc - 10);
            actMore = P(actBadge, ActDisc / 2 + 2, -6, 20, 12, (p, r) => Dots(p, r.center, 2.4f, 1.9f, Cream));
            actWalk = E(actBadge, "s-disc_dark_24", -ActDisc / 2 - 20, -9, 18, 18); Icon(E(actWalk, null, 3, 3, 12, 12), "s_moving", Cream);
            actDraw = E(actBadge, null, -ActDisc / 2 - 20, -9, 18, 18); Round(actDraw, 9); Border(actDraw, 1.4f, Cream); actDraw.style.backgroundColor = new Color(Plate.r, Plate.g, Plate.b, 0.9f); Icon(E(actDraw, null, 3.5f, 3.5f, 11, 11), "fire", Cream); // the holstered-gun mark (the ring wedge's)
            manifest.Add((actBadge, "image", () => "hover marker: what a right click does"));
            Show(actBadge, false);
        }

        /// <summary>Per frame, after the follow view's pass: the disc at the ring's front edge (the ring's lowest point on screen).</summary>
        void ActMarkFrame()
        {
            if (marks == null) return;
            if (actBadge == null) BuildActMark();
            followHoverRing.Clear();
            var cam = host.View; var hr = m.HoverRing;
            if (!cam || !hr.HasValue || m.Ended) { Show(actBadge, false); return; }
            Vector2 front; bool have;
            if (host.Following)
            {   // the view's own perspective: a 0.6 m ring on the ground under the target; the front is its lowest point on screen
                have = true; front = default; float best = float.MinValue;
                for (int i = 0; i <= 40; i++)
                {
                    float a = i * Mathf.PI * 2f / 40f;
                    if (!Proj(hr.Value + new Vector3(Mathf.Cos(a) * 0.6f, 0.03f, Mathf.Sin(a) * 0.6f), out var q)) { followHoverRing.Clear(); have = false; break; }
                    followHoverRing.Add(q); if (q.y > best) { best = q.y; front = q; }
                }
            }
            else
            {   // the overhead hover ring (StreetLess's Ground: 64 px across at play zoom, ry = 0.62 rx): its front is straight below its centre
                have = Proj(hr.Value, out var q); float px = cam.orthographic ? 14f / Mathf.Max(0.01f, cam.orthographicSize) : 1f;
                front = q + new Vector2(0f, 64f * px / 2f * 0.62f);
            }
            var (glyph, more, walk, draws) = host.HoverAct;
            bool on = have && (glyph.Length > 0 || more) && !ActMarkOff;
            Show(actBadge, on); if (!on) return;
            actBadge.style.left = front.x; actBadge.style.top = front.y;
            Show(actDisc, glyph.Length > 0); Icon(actGlyph, glyph, Cream); // a harmful likely way (never lit): the dots alone
            Show(actMore, more); Show(actWalk, walk); Show(actDraw, draws);
            actDraw.style.left = -ActDisc / 2 - 20 - (walk ? 21 : 0);
            actBadge.BringToFront();
        }
    }
}
