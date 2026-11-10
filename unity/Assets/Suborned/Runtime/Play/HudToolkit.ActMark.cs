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

        // ---------------------------------------------------------------- brief 041: the verb pill (Sky's pick, testbed/controls/markers.js 'pill')

        VisualElement vpill, vpillDisc, vpillFaces, vpillSeen, vpillAfter; bool pillAfterNow; Label vpillHead, vpillDot, vpillText; Paint vpillTail, vpillPip; // the street label's parts (HudToolkit.Pill)
        string vpillKey = "", vpillLine = ""; bool vpillReady;
        readonly List<VisualElement> faceEls = new();
        /// <summary>The pill's height and its tail (brief 044 A: `pill_status` 38 px at slice 0.43; harm `panel_strip` at 0.3).</summary>
        const float PillH = 38f, TailW = 18f, TailH = 13f;
        /// <summary>Glyph's 0.2.65 review 3: the disc and the cameo sit inside the body, clear of the end caps. `pill_status` at slice 0.43 has its
        /// inner chrome line 17 px in from each end; the drawn disc (DiscSize's box less its 12.5 % margin) and the cameo stand 3 px inside it.
        /// The harm strip (`panel_strip` at 0.3) has its screws about 9 px in: the stripe blocks start 8 px past them (review 2).</summary>
        static float PillPadL => 20f - (DiscSize - DiscVisible) / 2f;
        const float PillPadR = 20f, HarmPad = 22f;
        /// <summary>9 px from the drawn disc to the first word (the disc's box carries its own transparent margin).</summary>
        static float DiscGap => 9f - (DiscSize - DiscVisible) / 2f;
        /// <summary>The hold's "cancel" token (hold.cancel): "drain" runs the sweep back to empty over DrainS × its fill when a hold is let go
        /// early; "snap" drops it at once.</summary>
        const float DrainS = 0.3f;
        float sweepShown = -1f, drainFrom = -1f, drainAt; string sweepState = "";
        /// <summary>The proof reads it: the sweep's drawn fraction now (-1 none), the hold's fill or its drain.</summary>
        public static float SweepShownNow { get; private set; } = -1f;

        /// <summary>Harm reads by shape, never by colour (brief 044 §6): a takedown or fire is drawn on the chamfered strip with stripes and the
        /// light disc.</summary>
        public static bool Harmful(Suborned.Sim.Order o) => o?.Do is "takedown" or "fire";

        /// <summary>The words on the pill (data.js priceLine): the act and its target, the cash, the Trail, the Alert. Brief 101 (Sky 2026-11-08): no
        /// hit chance and no time; a shot says what the street will do (its Trail, and Alert ≥2: it goes loud).</summary>
        public static string PillLine(HandoverHost h, HandoverHost.Intent i)
        {
            var parts = new List<string> { i.Label.Length > 0 ? i.Label : i.Order?.Do ?? "" };
            foreach (var f in HudModel.PriceLine(h, i))
                switch (f.Kind)
                {
                    case "usd": if (f.Text.Length > 0) parts.Add("$" + f.Text); break;
                    case "trail": if (f.Text.Length > 0) parts.Add(f.Text + " Trail" + (f.Found ? " if found" : "")); break;
                    case "alert": if (f.Text.Length > 0) parts.Add("Alert " + f.Text); break;
                }
            return string.Join(" · ", parts);
        }

        /// <summary>What the pill is on now: the hovered thing's act, else the single-clicked thing's; null when nothing to say (the Double click
        /// scheme only; a ring, the panel, the going-loud card or the aim's chip up: none).</summary>
        (HandoverHost.Target t, HandoverHost.Intent i)? PillNow()
        {
            if (!host.controls.DoubleClick || m.Ended || host.Ring != null || host.Panel != null || host.Loud != null || host.AimNow != null || host.Modal != ModalGate.Modal.None) return null;
            if (host.Hover != null && host.HoverIntent is { Actionable: true } hi && host.Hover.Kind != "op") return (host.Hover, hi);
            if (host.SelTarget != null && host.SelIntent is { Actionable: true } si) return (host.SelTarget, si);
            return null;
        }
        /// <summary>The proof reads it: the pill's words now ("" hidden), the head word included ("Hold · Watch the guard").</summary>
        public static string PillWords => live != null && live.vpill != null && live.vpill.resolvedStyle.display != DisplayStyle.None ? live.vpillLine : "";

        /// <summary>Per frame: the pill above the thing (centred, clamped to the screen), its tail pointing down at it.</summary>
        void PillFrame()
        {
            if (marks == null) return; if (vpill == null) BuildPill();
            if (host.controls.HoldActs) { HoldPillFrame(); HoldMarks(); DoneMarks(); world?.MarkDirtyRepaint(); return; } // brief 041: the hold preset's pill and marks (HudToolkit.Hold)
            if (squadBtn != null) HoldMarks();
            DoneMarks(); // brief 043: the gain chips and window markers in every preset
            var now = PillNow(); var cam = host.View;
            Vector2 q = default; bool on = now.HasValue && cam && host.TargetWorld(now.Value.t) is Vector3 w && Proj(w + Vector3.up * (now.Value.t.Kind is "person" or "crowd" or "hostile" or "bystander" or "patrol" or "rivals" or "security" ? 2.25f : 1.5f), out q);
            Show(vpill, on); Show(vpillTail, on); if (!on) { Show(loudRow, false); return; }
            var (t, i) = now.Value;
            pillPredNow = i.PredMark; pillFire = null; PillLook(PillLine(host, i), i.Lead.Length > 0 ? i.Lead : HudModel.AtlasIcon(i.Order.Do, i.Order.Approach, ""), Harmful(i.Order), null, false, -1f, "offer");
            vpill.style.opacity = 1f; vpillTail.style.opacity = 1f; vpill.style.scale = new Scale(Vector3.one);
            PillPlace(q, out _);
        }

        void BuildActMark()
        {
            actBadge = E(marks, null, 0, 0, 0, 0); actBadge.style.overflow = Overflow.Visible; actBadge.pickingMode = PickingMode.Ignore;
            actDisc = E(actBadge, "s-disc_dark_24", -ActDisc / 2, -ActDisc / 2, ActDisc, ActDisc);
            actGlyph = E(actDisc, null, 5, 5, ActDisc - 10, ActDisc - 10);
            actMore = P(actBadge, ActDisc / 2 + 2, -6, 20, 12, (p, r) => Dots(p, r.center, 2.4f, 1.9f, Cream));
            actWalk = E(actBadge, "s-disc_dark_24", -ActDisc / 2 - 20, -9, 18, 18); Icon(E(actWalk, null, 3, 3, 12, 12), "s_moving", Cream);
            actDraw = E(actBadge, null, -ActDisc / 2 - 20, -9, 18, 18); Round(actDraw, 9); Border(actDraw, 1.4f, Cream); actDraw.style.backgroundColor = new Color(Plate.r, Plate.g, Plate.b, 0.9f); Icon(Middle(actDraw, 11, 11), "fire", Cream); // the holstered-gun mark (the ring wedge's)
            manifest.Add((actBadge, "image", () => "hover marker: what a right click does"));
            Show(actBadge, false);
        }

        /// <summary>Per frame, after the follow view's pass: the disc at the ring's front edge (the ring's lowest point on screen).</summary>
        void ActMarkFrame()
        {
            if (marks == null) return;
            if (actBadge == null) BuildActMark();
            followHoverRing.Clear();
            PillFrame(); if (host.controls.Modern) { Show(actBadge, false); return; } // brief 041: no ground ring, no disc, no name: the pill alone
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
                    if (i == 40) GroundRing(hr.Value, 0.6f, Cream, 2f, false, 0.5f); // brief 046: the ring itself on the ground (the points place the disc)
                }
            }
            else
            {   // the overhead hover ring (StreetLess's Ground: 64 px across at play zoom, ry = 0.62 rx): its front is straight below its centre
                have = Proj(hr.Value, out var q); float px = cam.orthographic ? 14f / Mathf.Max(0.01f, cam.orthographicSize) : 1f;
                front = q + new Vector2(0f, 64f * px / 2f * 0.62f);
            }
            var (glyph, more, walk, draws) = host.HoverAct;
            bool on = have && (glyph.Length > 0 || more) && !ActMarkOff && !host.controls.Modern; // brief 041: the verb pill (PillFrame) instead of the disc
            Show(actBadge, on); if (!on) return;
            actBadge.style.left = front.x; actBadge.style.top = front.y;
            Show(actDisc, glyph.Length > 0); Icon(actGlyph, glyph, Cream); // a harmful likely way (never lit): the dots alone
            Show(actMore, more); Show(actWalk, walk); Show(actDraw, draws);
            actDraw.style.left = -ActDisc / 2 - 20 - (walk ? 21 : 0);
            actBadge.BringToFront();
        }
    }
}
