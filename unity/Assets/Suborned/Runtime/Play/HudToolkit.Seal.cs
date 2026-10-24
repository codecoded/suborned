using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 057 §4.1 (Sky 2026-10-24: "I failed mission for shots heard but that was a long time ago"): the seal clock, a window marker
    /// hung under the top-right clock pill (its right edge at x 1795 at 1080p, clear of the pill): `plate_dark` 38 px, the kit's `lock` icon, the
    /// seconds left as m:ss (the clock bar's own format), a 2.5 px draining underline in turquoise-light over the seal's span, amber and blinking
    /// in the last 10 s (the window marker's rule). Hovered, a kit disc (`disc_dark_24`, 30 px, chrome ring) left of it holds what started the
    /// seal, as one icon. On first showing it pops once. Colours: the kit's own (accent.live, accent.warn, cream). It reads the host only.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement sealEl, sealIcon, sealTrack, sealFill, sealCause, sealCauseIcon; Label sealText; bool sealHover; float sealShownAt = -9f;
        /// <summary>The proof reads it: the marker's words now ("" hidden).</summary>
        public static string SealWords { get; private set; } = "";
        /// <summary>Proofs: the cause disc as if hovered.</summary>
        public static bool SealHoverForce;
        /// <summary>Proofs: the end card's lines as the HUD has them now.</summary>
        public static IReadOnlyList<string> EndLinesNow => live?.m?.EndLines;

        void BuildSeal()
        {
            sealEl = PlateDark(root, WinH, WinScale); sealEl.style.position = Position.Absolute; sealEl.style.right = 1920f - 1795f; sealEl.style.top = 14f + 44f + 8f; sealEl.style.paddingLeft = 11; sealEl.style.paddingRight = 12; sealEl.pickingMode = PickingMode.Position;
            sealIcon = E(sealEl, null, w: 17, h: 17); sealIcon.style.flexShrink = 0; Icon(sealIcon, "lock", Cream);
            sealText = Words(sealEl, 15.5f); sealText.style.marginLeft = 6; sealText.style.marginBottom = 1; sealText.style.unityFontStyleAndWeight = FontStyle.Bold;
            sealTrack = E(sealEl, null, 11, WinH - 9f, float.NaN, 2.5f); sealTrack.style.right = 11; Round(sealTrack, 1.25f); sealTrack.style.backgroundColor = new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.6f); sealTrack.style.overflow = Overflow.Hidden;
            sealFill = E(sealTrack, null, 0, 0, 0, 2.5f); Round(sealFill, 1.25f);
            sealCause = E(root, null, 0, 0, 30, 30); sealCause.style.position = Position.Absolute; Bg(sealCause, "markers/disc_dark_24", Color.white); sealCause.pickingMode = PickingMode.Ignore;
            sealCauseIcon = E(sealCause, null, 7, 7, 16, 16);
            sealEl.RegisterCallback<PointerEnterEvent>(_ => sealHover = true); sealEl.RegisterCallback<PointerLeaveEvent>(_ => sealHover = false);
            manifest.Add((sealEl, "line", () => SealWords)); manifest.Add((sealCause, "image", () => "seal cause"));
            Show(sealEl, false); Show(sealCause, false);
        }

        void SealFrame()
        {
            if (root == null) return; if (sealEl == null) BuildSeal();
            var h = host; bool on = h.SealOn && !m.Ended && !m.BriefingOpen;
            Show(sealEl, on); if (!on) { Show(sealCause, false); SealWords = ""; sealShownAt = -9f; return; }
            float now = Time.unscaledTime; if (sealShownAt < 0f) sealShownAt = now;
            int left = h.SealLeftS, span = h.SealSpanS; string t = $"{left / 60}:{left % 60:00}"; Text(sealText, t); SealWords = t;
            bool low = left <= 10; float blink = low ? 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(now * Mathf.PI * 4f)) : 1f;
            sealEl.style.opacity = blink; sealText.style.color = low ? LookSoon : LookText;
            sealFill.style.width = Length.Percent(Mathf.Clamp01(span > 0 ? left / (float)span : 0f) * 100f); sealFill.style.backgroundColor = low ? LookSoon : LookLive;
            float pop = now - sealShownAt; float sc = pop < 0.6f ? 1f + 0.18f * Mathf.Sin(pop / 0.6f * Mathf.PI) : 1f; sealEl.style.scale = new Scale(new Vector2(sc, sc)); // the one pop when it is set
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
