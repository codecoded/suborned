using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The live seal deadline, top centre in both views: named countdown, reason and the next step from mission text.
    /// Sky's 0.2.232 play (2026-11-04) missed the old 38 px lock-and-time badge. This stays opaque throughout the deadline,
    /// changes to amber at 30 s and keeps the draining underline and cause disc. It reads the engine's deadline only.</summary>
    public sealed partial class HudToolkit
    {
        int sealLeftShown = -1;
        VisualElement sealEl, sealIcon, sealTrack, sealFill, sealCause, sealCauseIcon; Label sealText, sealAction, sealReason; bool sealHover; float sealShownAt = -9f;
        /// <summary>The proof reads it: the marker's words now ("" hidden).</summary>
        public static string SealWords { get; private set; } = "";
        /// <summary>Proofs: the cause disc as if hovered.</summary>
        public static bool SealHoverForce;
        /// <summary>The proof checks the actual panel bounds and actionable instruction, not just its backing string.</summary>
        public static Rect SealBounds => live?.sealEl?.worldBound ?? default;
        public static string SealActionWords { get; private set; } = "";
        public static bool SealVisible => live?.sealEl != null && live.sealEl.resolvedStyle.display != DisplayStyle.None && live.sealEl.resolvedStyle.opacity > 0.9f && SealBounds.width > 1 && SealBounds.height > 1;
        /// <summary>Proofs: the end card's lines as the HUD has them now.</summary>
        public static IReadOnlyList<string> EndLinesNow => live?.m?.EndLines;

        void BuildSeal()
        {
            sealEl = PlateDark(root, 104f, WinScale); sealEl.style.position = Position.Absolute; sealEl.style.top = 66f; sealEl.style.width = 420f; sealEl.style.flexDirection = FlexDirection.Column; sealEl.style.alignItems = Align.FlexStart; sealEl.style.paddingLeft = 18; sealEl.style.paddingRight = 18; sealEl.style.paddingTop = 10; sealEl.pickingMode = PickingMode.Position;
            var heading = E(sealEl); heading.style.flexDirection = FlexDirection.Row; heading.style.alignItems = Align.Center;
            sealIcon = E(heading, null, w: 19, h: 19); sealIcon.style.flexShrink = 0; Icon(sealIcon, "lock", Cream);
            sealText = Words(heading, 23f); sealText.style.marginLeft = 8; sealText.style.unityFontStyleAndWeight = FontStyle.Bold;
            sealReason = Words(sealEl, 14f); sealAction = Words(sealEl, 17f); sealAction.style.marginTop = 2;
            sealTrack = E(sealEl, null, 18, 94f, float.NaN, 2.5f); sealTrack.style.right = 18; Round(sealTrack, 1.25f); sealTrack.style.backgroundColor = new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.6f); sealTrack.style.overflow = Overflow.Hidden;
            sealFill = E(sealTrack, null, 0, 0, 0, 2.5f); Round(sealFill, 1.25f);
            sealCause = E(root, null, 0, 0, 30, 30); sealCause.style.position = Position.Absolute; Bg(sealCause, "markers/disc_dark_24", Color.white); sealCause.pickingMode = PickingMode.Ignore;
            sealCauseIcon = E(sealCause, null, 7, 7, 16, 16);
            sealEl.RegisterCallback<PointerEnterEvent>(_ => sealHover = true); sealEl.RegisterCallback<PointerLeaveEvent>(_ => sealHover = false);
            manifest.Add((sealEl, "card", () => SealWords + " | " + SealActionWords)); manifest.Add((sealCause, "image", () => "seal cause"));
            Show(sealEl, false); Show(sealCause, false);
        }

        void SealFrame()
        {
            if (root == null) return; if (sealEl == null) BuildSeal();
            var h = host; bool on = h.SealOn && !m.Ended && !m.BriefingOpen;
            Show(sealEl, on); if (!on) { Show(sealCause, false); SealWords = SealActionWords = ""; sealShownAt = -9f; sealLeftShown = -1; return; }
            float now = Time.unscaledTime; if (sealShownAt < 0f) sealShownAt = now;
            int left = h.SealLeftS, span = h.SealSpanS; if (left != sealLeftShown) { sealLeftShown = left; SealWords = h.Text.SealHeading(left); Text(sealText, SealWords); }
            SealActionWords = h.Text.SealLive[h.ValeAboard ? h.Game.CarHolders().Count > 0 ? "held" : "ready" : "empty"]; Text(sealAction, SealActionWords); Text(sealReason, h.Text.SealLive["cause"]);
            float width = root.layout.width > 0 ? root.layout.width : 1920f; sealEl.style.left = Mathf.Max(14f, (width - 420f) / 2f);
            bool low = left <= 30;
            sealEl.style.opacity = 1f; sealText.style.color = low ? LookSoon : LookText;
            sealFill.style.width = Length.Percent(Mathf.Clamp01(span > 0 ? left / (float)span : 0f) * 100f); sealFill.style.backgroundColor = low ? LookSoon : LookLive;
            float pop = now - sealShownAt; float sc = pop < 0.6f ? 1f + 0.18f * Mathf.Sin(pop / 0.6f * Mathf.PI) : 1f; sealEl.style.scale = new Scale(new Vector2(sc, sc)); // the one pop when it is set
            if (h.Modal == ModalGate.Modal.None) sealEl.BringToFront(); // a live deadline cannot be buried beneath ordinary HUD pieces
            bool hv = sealHover || SealHoverForce; Show(sealCause, hv);
            if (hv)
            {
                var ic = h.SealCauseIcon();
                if (ic.StartsWith("badge:")) { Bg(sealCauseIcon, "badges/" + ic[6..], Color.white); } else Icon(sealCauseIcon, ic, Cream);
                var r = sealEl.worldBound; sealCause.style.left = r.xMin - 30f - 6f; sealCause.style.top = r.center.y - 15f;
            }
        }
    }
}
