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

        VisualElement vpill, vpillDisc, vpillClip, vpillFill, vpillFaces, vpillSeen, vpillStripeL, vpillStripeR; Label vpillHead, vpillDot, vpillText; Paint vpillTail, vpillGlow, vpillPip;
        string vpillKey = "", vpillLine = ""; bool vpillHarm, vpillReady;
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
        /// <summary>The proof reads it: the pill's words now ("" hidden), the head word included ("Hold · Watch the guard · 26 s").</summary>
        public static string PillWords => live != null && live.vpill != null && live.vpill.resolvedStyle.display != DisplayStyle.None ? live.vpillLine : "";

        void BuildPill()
        {
            vpill = E(marks, "s-pill_status", 0, 0, float.NaN, PillH); vpill.style.unitySliceScale = PillH / 88f; vpill.style.flexDirection = FlexDirection.Row; vpill.style.alignItems = Align.Center; vpill.pickingMode = PickingMode.Ignore;
            vpill.style.overflow = Overflow.Visible; vpill.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));
            vpill.style.paddingLeft = PillPadL; vpill.style.paddingRight = PillPadR; // 0.2.65 drew the first plain pill with no padding at all (the harm toggle set it, and a plain pill never toggled)
            // "Let go": a 2 px turquoise-light ring and a soft glow round the frame (painted, behind the content)
            vpillGlow = P(vpill, -12, -12, 0, 0, (p, r) =>
            {
                var box = new Rect(r.x + 12f, r.y + 12f, r.width - 24f, r.height - 24f); float rad = vpillHarm ? 6f : 19f;
                static Rect Grow(Rect b, float d) => new(b.x - d, b.y - d, b.width + 2f * d, b.height + 2f * d);
                for (int i = 4; i >= 1; i--) { float lw = 2.5f * i, d = 2f + lw / 2f; p.strokeColor = new Color(LookLive.r, LookLive.g, LookLive.b, ReadyGlowAlpha); p.lineWidth = lw; p.BeginPath(); RoundRect(p, Grow(box, d), rad + d); p.Stroke(); } // the glow, outside the ring
                p.strokeColor = LookLive; p.lineWidth = ReadyRingW; p.BeginPath(); RoundRect(p, Grow(box, ReadyRingW / 2f), rad + ReadyRingW / 2f); p.Stroke(); // the 2 px ring, just outside the frame
            });
            vpillGlow.style.right = -12; vpillGlow.style.bottom = -12; vpillGlow.style.width = StyleKeyword.Auto; vpillGlow.style.height = StyleKeyword.Auto;
            // the hold: a turquoise-light sweep at 40 % inside the frame's window
            vpillClip = E(vpill, null, 15, 6, float.NaN, float.NaN); vpillClip.style.right = 15; vpillClip.style.bottom = 6; Round(vpillClip, 13); vpillClip.style.overflow = Overflow.Hidden;
            vpillFill = E(vpillClip, null, 0, 0, 0, float.NaN); vpillFill.style.bottom = 0; vpillFill.style.backgroundColor = new Color(HoldFill.r, HoldFill.g, HoldFill.b, HoldSweepAlpha);
            vpillStripeL = Stripes(vpill, 10, 24); vpillStripeL.style.marginRight = 9;
            vpillDisc = ActDiscEl(vpill);
            vpillHead = Words(vpill, 15.5f, LookHead); vpillHead.style.marginLeft = DiscGap;
            vpillDot = Words(vpill, 15.5f, new Color(LookHead.r, LookHead.g, LookHead.b, HeadDotAlpha)); vpillDot.text = "·"; vpillDot.style.marginLeft = 5; vpillDot.style.marginRight = 5;
            vpillText = Words(vpill, 15.5f); vpillText.style.marginLeft = 9;
            vpillFaces = E(vpill); vpillFaces.style.flexDirection = FlexDirection.Row; vpillFaces.style.marginLeft = 10; vpillFaces.style.flexShrink = 0;
            for (int i = 0; i < 4; i++) { var f = Cameo(vpillFaces, FaceSize); f.style.marginLeft = i == 0 ? 0 : -7; faceEls.Add(f); }
            // "Watched spot": an ink-deep plate, a 1.5 px panel-line edge, the kit's warning stripes, the sawyou icon (Fable row 8)
            vpillSeen = E(vpill, null, w: float.NaN, h: 22); vpillSeen.style.flexDirection = FlexDirection.Row; vpillSeen.style.alignItems = Align.Center; vpillSeen.style.flexShrink = 0; vpillSeen.style.marginLeft = 9; vpillSeen.style.paddingRight = 9;
            vpillSeen.style.backgroundColor = InkDeep; Round(vpillSeen, 5); Border(vpillSeen, 1.5f, new Color(PanelLine.r, PanelLine.g, PanelLine.b, 0.66f)); vpillSeen.style.overflow = Overflow.Hidden;
            Stripes(vpillSeen, 7, 19); var seenIcon = E(vpillSeen, null, w: 13, h: 13); seenIcon.style.flexShrink = 0; seenIcon.style.marginLeft = 5; Icon(seenIcon, "sawyou", Cream);
            var seenWord = Words(vpillSeen, 12.5f); seenWord.text = "Watched spot"; seenWord.style.marginLeft = 5;
            vpillStripeR = Stripes(vpill, 10, 24); vpillStripeR.style.marginLeft = 9;
            // the tail: a chrome triangle with the pill's body inside it, its tip at the thing
            vpillTail = P(marks, 0, 0, TailW, TailH, (p, r) =>
            {
                p.fillColor = Chrome; p.BeginPath(); p.MoveTo(new Vector2(0, 2)); p.LineTo(new Vector2(TailW, 2)); p.LineTo(new Vector2(TailW / 2f, TailH)); p.ClosePath(); p.Fill();
                p.fillColor = LookTail; p.BeginPath(); p.MoveTo(new Vector2(3, 0)); p.LineTo(new Vector2(TailW - 3, 0)); p.LineTo(new Vector2(TailW / 2f, 7.5f)); p.ClosePath(); p.Fill();
            });
            // the lit pip of an act that just opened (brief 043): a cream sparkle on the frame's top-right chrome corner, over the frame
            vpillPip = P(vpillFaces, 0, -5, 18, 18, (p, r) => Sparkle(p, r.center, 7.5f, Cream)); vpillPip.style.left = StyleKeyword.Auto; vpillPip.style.right = -5; Show(vpillPip, false); // review 3: on the cameo's top-right (the frame's corner clipped it)
            vpillFaces.style.overflow = Overflow.Visible;
            manifest.Add((vpill, "line", () => vpillLine));
            Show(vpill, false); Show(vpillTail, false);
        }

        static readonly string[] PillHeads = { "Let go", "Right hold", "Hold" };

        /// <summary>The pill's look for one frame (both presets): the words (the head word "Hold", "Right hold", "Let go" split off in dim, "Let
        /// go" in turquoise-light), the act's disc, the faces, "Watched spot", the hold's sweep, harm's strip and stripes, the "Let go" ring.</summary>
        void PillLook(string text, string icon, bool harm, IReadOnlyList<int> faces, bool seen, float fill, string state)
        {
            bool ready = state == "ready";
            string key = text + "|" + icon + "|" + harm + "|" + seen + "|" + (faces == null ? "" : string.Join(",", faces)) + "|" + ready;
            if (key != vpillKey)
            {
                vpillKey = key; vpillLine = text; string head = "", rest = text;
                foreach (var hw in PillHeads) if (text.StartsWith(hw + " · ")) { head = hw; rest = text[(hw.Length + 3)..]; break; }
                if (head.Length == 0 && text.StartsWith("to ") && text.IndexOf(" · ", System.StringComparison.Ordinal) is int wi and > 3) { head = text[..wi]; rest = text[(wi + 3)..]; } // review 4: the walk-in's "to Inez" is a head word, dim like "Hold"
                Text(vpillHead, head); Show(vpillHead, head.Length > 0); Show(vpillDot, head.Length > 0); Text(vpillText, rest); vpillText.style.marginLeft = head.Length > 0 ? 0 : DiscGap;
                vpillHead.style.color = ready ? LookLive : LookHead;
                Show(vpillDisc, icon.Length > 0); if (icon.Length > 0) ActDiscOf(vpillDisc, icon, harm);
                int n = faces?.Count ?? 0; Show(vpillFaces, n > 0);
                for (int i = 0; i < 4; i++) { bool on = i < n; Show(faceEls[i], on); if (on) CameoOf(faceEls[i], faces[i]); }
                Show(vpillSeen, seen);
                if (harm != vpillHarm || vpill.ClassListContains("s-pill_status") == harm)
                {
                    vpillHarm = harm; Class(vpill, "s-pill_status", !harm); Class(vpill, "s-panel_strip", harm); vpill.style.unitySliceScale = harm ? 0.3f : PillH / 88f;
                    vpill.style.paddingLeft = harm ? HarmPad : PillPadL; vpill.style.paddingRight = harm ? HarmPad : PillPadR;
                    vpillClip.style.left = vpillClip.style.right = harm ? 8 : 15; Round(vpillClip, harm ? 3 : 13);
                    vpillGlow.MarkDirtyRepaint();
                }
                Show(vpillStripeL, harm); Show(vpillStripeR, harm);
                if (ready != vpillReady) { vpillReady = ready; vpillGlow.MarkDirtyRepaint(); }
            }
            Show(vpillGlow, ready);
            // the sweep; a hold let go early (charge, then no charge, never ready) drains back when hold.cancel is "drain"
            float now = Time.unscaledTime, sw = fill >= 0f ? HoldSweep(fill) : -1f;
            if (sw < 0f && sweepState == "charge" && sweepShown > 0f && Tok.hold.cancel == "drain") { drainFrom = sweepShown; drainAt = now; }
            if (sw >= 0f) drainFrom = -1f;
            float shown = sw;
            if (sw < 0f && drainFrom > 0f) { float t = (now - drainAt) / Mathf.Max(0.05f, DrainS * drainFrom); if (t < 1f) shown = drainFrom * (1f - t); else drainFrom = -1f; }
            sweepShown = sw; sweepState = sw >= 0f ? state : "";
            Show(vpillClip, shown >= 0f); if (shown >= 0f) vpillFill.style.width = Length.Percent(shown * 100f); SweepShownNow = shown;
        }

        /// <summary>Places the pill above the screen point q (centred, clamped to the screen), the tail's tip just above q.</summary>
        float PillPlace(Vector2 q, out float x)
        {
            float pw = float.IsNaN(vpill.resolvedStyle.width) ? 300f : vpill.resolvedStyle.width, W = root.layout.width > 0 ? root.layout.width : 1920f;
            x = Mathf.Clamp(q.x - pw / 2f, 14f, Mathf.Max(14f, W - pw - 14f)); float top = q.y - 10f - PillH - 11f;
            top = ClearOfSpeech(SpeechRects(), x, pw, top + PillH, PillH) - PillH; // brief 044: above the thing's speech and its leader, never over it
            vpill.style.left = x; vpill.style.top = top; vpillTail.style.left = q.x - TailW / 2f; vpillTail.style.top = top + PillH - 4f;
            vpill.BringToFront(); vpillTail.BringToFront(); return pw;
        }

        /// <summary>Per frame: the pill above the thing (centred, clamped to the screen), its tail pointing down at it.</summary>
        void PillFrame()
        {
            if (marks == null) return; if (vpill == null) BuildPill();
            if (host.controls.HoldActs) { HoldPillFrame(); HoldMarks(); DoneMarks(); world?.MarkDirtyRepaint(); return; } // brief 041: the hold preset's pill and marks (HudToolkit.Hold)
            if (squadBtn != null) HoldMarks();
            DoneMarks(); // brief 043: the gain chips and window markers in every preset
            var now = PillNow(); var cam = host.View;
            Vector2 q = default; bool on = now.HasValue && cam && host.TargetWorld(now.Value.t) is Vector3 w && Proj(w + Vector3.up * (now.Value.t.Kind is "person" or "crowd" or "hostile" or "bystander" or "patrol" or "rivals" or "security" ? 2.25f : 1.5f), out q);
            Show(vpill, on); Show(vpillTail, on); if (!on) return;
            var (t, i) = now.Value;
            PillLook(PillLine(host, i), i.Lead.Length > 0 ? i.Lead : HudModel.AtlasIcon(i.Order.Do, i.Order.Approach, ""), Harmful(i.Order), null, false, -1f, "offer");
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
