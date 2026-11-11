using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 026 round 2, brief 107: the Handover by day or by night. LOOK ONLY. Since engine 0.54.0 the light is the game's (Game.Light: rolled
    /// from the seed, changed at the briefing by the `light` order, kept by Replay), and PlayView107 draws it; the briefing's Day / Night tab gives the
    /// order (ChooseLight). SetNight is the view's override for a shot, `-night` and the proofs: it never touches the game, and it is REFUSED, with an
    /// error, in a scene that has no night (no LightPhase): a night asked for is never quietly drawn as day (Glint's finding, brief 107).
    /// The old PlayerPrefs choice is retired: the roll decides.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>Night is drawn now.</summary>
        public bool NightLook => PlayView107.Night;
        /// <summary>This scene can draw a night (the briefing offers Night only then).</summary>
        public bool CanNight => PlayView107.CanNight;
        /// <summary>The camera's focus on the ground (the lamps' cap, the rain's box, the mist's clear ring).</summary>
        public Vector3 FocusPoint => focus;

        /// <summary>The view's night or day, whatever the game says (a shot, `-night`). False, with an error, when night is asked and the scene has none.</summary>
        public bool SetNight(bool night)
        {
            if (night && !CanNight) { Debug.LogError("HostNight: night was asked for, but this scene has no night (no LightPhase): drawn as day, and the caller is told"); PlayView107.ForceNight(false); return false; }
            PlayView107.ForceNight(night); return true;
        }

        /// <summary>The briefing's Day / Night: the game's `light` order (before the clock runs), so it is saved and replayed.</summary>
        public void ChooseLight(bool night) { if (night && !CanNight) return; PlayView107.Release(); SetLook("light", night ? "night" : "day"); }
        /// <summary>The briefing's weather: the game's `weather` order (before the clock runs), saved and replayed.</summary>
        public void ChooseWeather(string kind) { PlayView107.Release(); SetLook("weather", kind); }

        /// <summary>At start: `-evening` (the day look with the dusk lights on, HarbourDistrict's LightPhase) and `-night` (night for this launch, the view only).</summary>
        void NightAtStart()
        {
            if (GameBoot.Arg("-evening"))
            {
                var lp = LightPhase.Instance ? LightPhase.Instance : FindAnyObjectByType<LightPhase>();
                if (lp) { lp.Focus = () => cam ? focus : (Vector3?)null; lp.SetEvening(); } else Debug.LogError("HostNight: -evening with no LightPhase in the scene");
                return;
            }
            if (GameBoot.Arg("-night")) SetNight(true);
        }
    }
}
