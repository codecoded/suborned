using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 109, the HUD's share of the juice (only while Juice109.Live): the ring pops open (scale 0.8 to 1 with a small overshoot, UI
    /// Toolkit's own USS transitions), and the money counts to its new value over 0.45 s instead of jumping, with a small bump on the figure; Trail
    /// bumps when it moves. The figures it shows are the engine's own, only eased; with the juice off the clock bar sets the text as before.</summary>
    public sealed partial class HudToolkit
    {
        bool juiceRingWasOn; float juiceCashShown = float.NaN, juiceCashFrom, juiceCashT0; long juiceCashTo; string juiceTrail;
        static readonly List<StylePropertyName> JuiceProps = new() { new StylePropertyName("scale"), new StylePropertyName("opacity") };

        static void JuiceEase(VisualElement e, float seconds, EasingMode ease)
        {
            e.style.transitionProperty = JuiceProps; e.style.transitionDuration = new List<TimeValue> { new(seconds, TimeUnit.Second) };
            e.style.transitionTimingFunction = new List<EasingFunction> { new(ease) };
        }

        /// <summary>RingFrame: the frame the ring opens, it starts small and faint and eases out to its size.</summary>
        void JuiceRing(bool on)
        {
            bool was = juiceRingWasOn; juiceRingWasOn = on;
            if (!Juice109.Live || !on || was) return;
            ringRoot.style.transitionDuration = new List<TimeValue> { new(0f, TimeUnit.Second) };
            ringRoot.style.scale = new Scale(Vector2.one * 0.8f); ringRoot.style.opacity = 0.25f;
            ringRoot.schedule.Execute(() => { JuiceEase(ringRoot, 0.14f, EasingMode.EaseOutBack); ringRoot.style.scale = new Scale(Vector2.one); ringRoot.style.opacity = 1f; }).StartingIn(16);
        }

        /// <summary>ClockFrame: the money's words while it counts (the engine's figure, eased), or `words` as they are.</summary>
        string JuiceCash(string words)
        {
            var g = host.Game; if (!Juice109.Live || g == null) return words;
            long now = g.Cash; float t = Time.unscaledTime;
            if (float.IsNaN(juiceCashShown)) { juiceCashShown = juiceCashFrom = juiceCashTo = now; juiceCashT0 = t - 1f; }
            if (now != juiceCashTo) { juiceCashFrom = juiceCashShown; juiceCashTo = now; juiceCashT0 = t; JuiceBump(cashK.Root); }
            float u = Mathf.Clamp01((t - juiceCashT0) / 0.45f); u = 1f - (1f - u) * (1f - u) * (1f - u);
            juiceCashShown = Mathf.Lerp(juiceCashFrom, juiceCashTo, u);
            return u >= 1f ? words : $"${(long)Mathf.Round(juiceCashShown):N0}";
        }

        /// <summary>ClockFrame: Trail bumps the frame it changes.</summary>
        void JuiceTrail(string trail)
        {
            if (!Juice109.Live) return;
            if (juiceTrail != null && trail != juiceTrail) JuiceBump(trailK.Root);
            juiceTrail = trail;
        }

        static void JuiceBump(VisualElement e)
        {
            JuiceEase(e, 0.11f, EasingMode.EaseOut); e.style.scale = new Scale(Vector2.one * 1.22f);
            e.schedule.Execute(() => { JuiceEase(e, 0.24f, EasingMode.EaseInOut); e.style.scale = new Scale(Vector2.one); }).StartingIn(110);
        }
    }
}
