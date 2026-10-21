using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 026 round 2: the Handover by day or by night, chosen on the briefing. LOOK ONLY (Glint's light.json `night`; LightPhase):
    /// the same mission, the same start, the same orders and the same replay; nothing is saved in the session. The choice is kept between
    /// runs (PlayerPrefs). The night lamps count from the play camera's focus.</summary>
    public sealed partial class HandoverHost
    {
        const string NightPref = "suborned.handover.night";
        public bool NightLook { get; private set; }

        public void SetNight(bool night)
        {
            NightLook = night; PlayerPrefs.SetInt(NightPref, night ? 1 : 0);
            var lp = LightPhase.Instance ? LightPhase.Instance : FindAnyObjectByType<LightPhase>();
            if (!lp) return;
            lp.Focus = () => cam ? focus : (Vector3?)null;
            lp.Set(night);
        }

        /// <summary>At start: the phase the player chose last time (day when never chosen);
        /// `-night` on the command line (the app's proof at night): night for this launch, the stored choice untouched.</summary>
        void NightAtStart()
        {
            if (GameBoot.Arg("-night")) { int keep = PlayerPrefs.GetInt(NightPref, 0); SetNight(true); PlayerPrefs.SetInt(NightPref, keep); return; }
            if (PlayerPrefs.GetInt(NightPref, 0) == 1) SetNight(true); else NightLook = false;
        }
    }
}
