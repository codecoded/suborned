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

        VisualElement vpill, vpillIcon; Label vpillText; Paint vpillTail; string vpillKey = "";
        static readonly Color PillInk = new(0x10 / 255f, 0x20 / 255f, 0x1f / 255f);
        /// <summary>The act's kind colour (data.js KINDS): look blue, talk green, tech teal, take cream, harm pink (harm also has square corners).</summary>
        public static (Color c, bool harm) KindOf(Suborned.Sim.Order o)
        {
            static Color H(int rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
            switch (o?.Do ?? "")
            {
                case "watch": case "listen": case "read": return (H(0x4aa8ff), false);
                case "turn": case "call": case "ask": case "pay": case "mislead": return (H(0x4cc35f), false);
                case "loop": case "reroute": case "open": case "enter": case "act": return (H(0x1fd1b5), false);
                case "takedown": case "fire": return (H(0xff4d7d), true);
                default: return (H(0xf1ead8), false);
            }
        }

        /// <summary>The words on the pill (data.js priceLine, time Total): the act and its target, the whole time (walk and act), the cash, the Trail, the Alert.</summary>
        public static string PillLine(HandoverHost h, HandoverHost.Intent i)
        {
            var parts = new List<string> { i.Label.Length > 0 ? i.Label : i.Order?.Do ?? "" };
            foreach (var f in HudModel.PriceLine(h, i))
                switch (f.Kind)
                {
                    case "time": case "walk": if (f.Text.Length > 0) parts.Add(i.Order?.Do == "fire" ? "down in " + f.Text : f.Text); break;
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
        /// <summary>The proof reads it: the pill's words now ("" hidden).</summary>
        public static string PillWords => live != null && live.vpill != null && live.vpill.resolvedStyle.display != DisplayStyle.None ? live.vpillText.text : "";

        void BuildPill()
        {
            vpill = E(marks, null, 0, 0, float.NaN, 34); vpill.style.flexDirection = FlexDirection.Row; vpill.style.alignItems = Align.Center; vpill.pickingMode = PickingMode.Ignore;
            vpill.style.paddingLeft = 11; vpill.style.paddingRight = 15; Border(vpill, 2f, Color.white);
            vpillIcon = E(vpill, null, w: 20, h: 20); vpillIcon.style.marginRight = 8; vpillIcon.style.flexShrink = 0;
            vpillText = L(vpill, "chip-value"); vpillText.style.fontSize = 16; vpillText.style.unityFontStyleAndWeight = FontStyle.Bold; vpillText.style.color = PillInk; vpillText.style.whiteSpace = WhiteSpace.NoWrap;
            vpillTail = P(marks, 0, 0, 14, 9, (p, r) => { p.fillColor = Color.white; p.BeginPath(); p.MoveTo(new Vector2(0, 0)); p.LineTo(new Vector2(14, 0)); p.LineTo(new Vector2(7, 9)); p.ClosePath(); p.Fill(); });
            manifest.Add((vpill, "line", () => vpillText.text));
            Show(vpill, false); Show(vpillTail, false);
        }

        /// <summary>Per frame: the pill above the thing (centred, clamped to the screen), its tail pointing down at it.</summary>
        void PillFrame()
        {
            if (marks == null) return; if (vpill == null) BuildPill();
            var now = PillNow(); var cam = host.View;
            Vector2 q = default; bool on = now.HasValue && cam && host.TargetWorld(now.Value.t) is Vector3 w && Proj(w + Vector3.up * (now.Value.t.Kind is "person" or "crowd" or "hostile" or "bystander" or "patrol" or "rivals" or "security" ? 2.25f : 1.5f), out q);
            Show(vpill, on); Show(vpillTail, on); if (!on) return;
            var (t, i) = now.Value; var (c, harm) = KindOf(i.Order);
            string key = (t.Kind + t.Key + i.Label + i.Usd + i.Time + i.Trail + i.Alert + i.WalkS);
            if (key != vpillKey) { vpillKey = key; Text(vpillText, PillLine(host, i)); Art(vpillIcon, "icons/singles/" + (i.Lead.Length > 0 ? i.Lead : HudModel.AtlasIcon(i.Order.Do, i.Order.Approach, "")), PillInk); }
            vpill.style.backgroundColor = c; Round(vpill, harm ? 6f : 17f);
            float pw = float.IsNaN(vpill.resolvedStyle.width) ? 300f : vpill.resolvedStyle.width, W = root.layout.width > 0 ? root.layout.width : 1920f;
            float x = Mathf.Clamp(q.x - pw / 2f, 14f, Mathf.Max(14f, W - pw - 14f));
            vpill.style.left = x; vpill.style.top = q.y - 10f - 34f - 9f; vpillTail.style.left = q.x - 7f; vpillTail.style.top = q.y - 10f - 9f - 1f;
            vpill.BringToFront(); vpillTail.BringToFront();
        }

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
            PillFrame(); if (host.controls.DoubleClick) { Show(actBadge, false); return; } // brief 041: no ground ring, no disc, no name: the pill alone
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
            bool on = have && (glyph.Length > 0 || more) && !ActMarkOff && !host.controls.DoubleClick; // brief 041: the verb pill (PillFrame) instead of the disc
            Show(actBadge, on); if (!on) return;
            actBadge.style.left = front.x; actBadge.style.top = front.y;
            Show(actDisc, glyph.Length > 0); Icon(actGlyph, glyph, Cream); // a harmful likely way (never lit): the dots alone
            Show(actMore, more); Show(actWalk, walk); Show(actDraw, draws);
            actDraw.style.left = -ActDisc / 2 - 20 - (walk ? 21 : 0);
            actBadge.BringToFront();
        }
    }
}
