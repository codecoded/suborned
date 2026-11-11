using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 109, the HUD's share of the juice (only while Juice109.Live), all UI Toolkit's own USS transitions: the ring pops open (scale 0.8
    /// to 1 with a small overshoot); the money counts to its new value over 0.45 s instead of jumping, with a small bump on the figure; Trail bumps
    /// when it moves; a takedown's done chip (round 2, Sky: "I do like our mini bobble head action PIPs") lands with its bobble-head cameo popping
    /// and a wobble, then its facts ("Out · Wakes 120 s") settling in one by one; landed off screen (no chip), the agent's rail portrait bumps.
    /// Alert rising pops the lamps it lights (moment 5). The figures and words are the engine's own, only eased; with the juice off the HUD is as before.</summary>
    public sealed partial class HudToolkit
    {
        bool juiceRingWasOn; float juiceCashShown = float.NaN, juiceCashFrom, juiceCashT0; long juiceCashTo; string juiceTrail;
        static readonly List<StylePropertyName> JuiceProps = new() { new StylePropertyName("scale"), new StylePropertyName("opacity"), new StylePropertyName("rotate"), new StylePropertyName("translate") };
        readonly HashSet<int> juiceChips = new();

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

        static void JuiceSet(VisualElement e, float scale, float deg, float opacity, float dy)
        { e.style.scale = new Scale(Vector2.one * scale); e.style.rotate = new Rotate(new Angle(deg, AngleUnit.Degree)); e.style.opacity = opacity; e.style.translate = new Translate(0, dy); }

        /// <summary>ChipFill: a takedown's chip lands with a bobble-head pop (the cameo from 0.35 to 1.28 with a tilt, then settling to 1 with an
        /// overshoot, about 0.4 s), then its facts settle in one by one (70 ms apart). Any other chip on a reused element is put back at rest.</summary>
        void JuiceChip(ChipEl c, HandoverHost.DoneChip ch)
        {
            if (!Juice109.Live) return;
            JuiceEase(c.Face, 0f, EasingMode.Linear); JuiceSet(c.Face, 1f, 0f, 1f, 0f);
            foreach (var r in c.Rows) { JuiceEase(r.row, 0f, EasingMode.Linear); JuiceSet(r.row, 1f, 0f, 1f, 0f); }
            if (ch.Do is not ("takedown" or "turn")) return; // round 3: a turn landing's chip ("Inez turned") pops as a takedown's does
            juiceChips.Add(ch.Id); var face = c.Face;
            JuiceSet(face, 0.35f, -14f, 1f, 0f);
            foreach (var r in c.Rows) JuiceSet(r.row, 1f, 0f, 0f, 8f);
            face.schedule.Execute(() => { JuiceEase(face, 0.13f, EasingMode.EaseOut); JuiceSet(face, 1.28f, 8f, 1f, 0f); }).StartingIn(16);
            face.schedule.Execute(() => { JuiceEase(face, 0.26f, EasingMode.EaseOutBack); JuiceSet(face, 1f, 0f, 1f, 0f); }).StartingIn(150);
            for (int i = 0; i < c.Rows.Count; i++) { var row = c.Rows[i].row; row.schedule.Execute(() => { JuiceEase(row, 0.2f, EasingMode.EaseOut); JuiceSet(row, 1f, 0f, 1f, 0f); }).StartingIn(170 + 70 * i); }
        }

        /// <summary>DoneMarks, after the chips are placed: a takedown that drew no chip (it landed off screen, or above the frame) bumps the doer's rail
        /// portrait instead, the place that always shows the agents.</summary>
        void JuiceRail()
        {
            var g = host.Game; if (!Juice109.Live || g == null) return; float now = Time.unscaledTime;
            foreach (var ch in host.DoneChips)
            {
                if (ch.Do is not ("takedown" or "turn") || juiceChips.Contains(ch.Id) || now - ch.Born < 0.05f) continue;
                juiceChips.Add(ch.Id);
                if (ch.Agent < 0 || ch.Agent >= colWins.Length || colWins[ch.Agent]?.Clip is not VisualElement f) continue;
                JuiceEase(f, 0.12f, EasingMode.EaseOut); JuiceSet(f, 1.24f, -7f, 1f, 0f);
                f.schedule.Execute(() => { JuiceEase(f, 0.28f, EasingMode.EaseOutBack); JuiceSet(f, 1f, 0f, 1f, 0f); }).StartingIn(130);
            }
        }

        int juiceAlert = -1; readonly VisualElement[] juiceLampFlash = new VisualElement[3];
        static readonly List<StylePropertyName> JuiceScaleProp = new() { new StylePropertyName("scale") }, JuiceOpacityProp = new() { new StylePropertyName("opacity") };
        /// <summary>ClockFrame (moment 5, Sky: on the HUD's Alert pips, never a screen tint): each lamp a rise lights pops (1.9 and back with an overshoot)
        /// under a cream flash that fades (a disc over the lamp: ClockFrame keeps setting the lamp's own colour). A fall only dims, as before.</summary>
        void JuiceAlert(int a)
        {
            int was = juiceAlert; juiceAlert = a; if (!Juice109.Live || was < 0 || a <= was) return;
            for (int i = Mathf.Max(0, was); i < Mathf.Min(a, lamps.Length); i++)
            {
                var l = lamps[i]; if (l == null) continue; int k = i - was;
                var f = juiceLampFlash[i];
                if (f == null) { f = juiceLampFlash[i] = new VisualElement { pickingMode = PickingMode.Ignore }; f.style.position = Position.Absolute; f.style.left = f.style.top = f.style.right = f.style.bottom = 0; Round(f, 8.1f); f.style.backgroundColor = Cream; f.style.opacity = 0f; l.Add(f); }
                l.style.transitionProperty = JuiceScaleProp; l.style.transitionDuration = new List<TimeValue> { new(0f, TimeUnit.Second) }; l.style.scale = new Scale(Vector2.one * 1.9f);
                f.style.transitionProperty = JuiceOpacityProp; f.style.transitionDuration = new List<TimeValue> { new(0f, TimeUnit.Second) }; f.style.opacity = 1f;
                var ff = f;
                l.schedule.Execute(() =>
                {
                    l.style.transitionDuration = new List<TimeValue> { new(0.32f, TimeUnit.Second) }; l.style.transitionTimingFunction = new List<EasingFunction> { new(EasingMode.EaseOutBack) }; l.style.scale = new Scale(Vector2.one);
                    ff.style.transitionDuration = new List<TimeValue> { new(0.45f, TimeUnit.Second) }; ff.style.transitionTimingFunction = new List<EasingFunction> { new(EasingMode.EaseOut) }; ff.style.opacity = 0f;
                }).StartingIn(40 + 90 * k);
            }
        }
    }
}
