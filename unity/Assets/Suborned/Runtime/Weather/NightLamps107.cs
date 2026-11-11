using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 107, Glint's testbed port (Sky 2026-11-11: "horrible paving colors at night", "this city should thrive at night"): the carried LightPhase's night lamps
    /// as weather107.json `night` says: intensity x lampScale (written into the phase's lampIntensity, which its Cap writes to the lit lamps), range x lampRange, colour leaned
    /// toward lampColour by lampShare, a lamp whose hue falls in rehueFrom..rehueTo taking rehueColour (the teal table lamp's cyan pool), liveCap. Everything is set from the
    /// lamps' values as first seen at night, so applying twice never compounds; day puts them back. Look only: no rule reads a light.</summary>
    public static class NightLamps107
    {
        static LightPhase seen; static float[] baseI; static Color[] baseC; static float[] baseR; static int baseCap;

        public static void Apply(LightPhase phase, Weather107.NightLook n, bool night)
        {
            if (!phase || n == null || phase.lamps == null) return;
            if (seen != phase || baseI == null || baseI.Length != phase.lamps.Length)
            {
                if (!night) return; // nothing changed yet
                seen = phase; int k = phase.lamps.Length; baseI = new float[k]; baseC = new Color[k]; baseR = new float[k]; baseCap = phase.liveCap;
                for (int i = 0; i < k; i++) { var l = phase.lamps[i]; baseI[i] = i < phase.lampIntensity.Length ? phase.lampIntensity[i] : l ? l.intensity : 0f; if (l) { baseC[i] = l.color; baseR[i] = l.range; } }
            }
            ColorUtility.TryParseHtmlString(n.lampColour, out var warm); var rc = warm; bool rehue = n.rehueTo > n.rehueFrom && ColorUtility.TryParseHtmlString(n.rehueColour, out rc);
            int teal = 0;
            for (int i = 0; i < phase.lamps.Length; i++)
            {
                var l = phase.lamps[i]; if (!l) continue;
                var c = baseC[i];
                if (night)
                {
                    Color.RGBToHSV(c, out var h, out var s, out _); float hd = h * 360f;
                    if (rehue && s > 0.15f && hd >= n.rehueFrom && hd <= n.rehueTo) { c = rc; teal++; } else c = Color.Lerp(c, warm, Mathf.Clamp01(n.lampShare));
                }
                l.color = c; l.range = baseR[i] * (night ? Mathf.Max(0.1f, n.lampRange) : 1f);
                if (i < phase.lampIntensity.Length) phase.lampIntensity[i] = baseI[i] * (night ? Mathf.Max(0f, n.lampScale) : 1f);
                if (l.enabled && i < phase.lampIntensity.Length) l.intensity = phase.lampIntensity[i];
            }
            phase.liveCap = night && n.liveCap > 0 ? n.liveCap : baseCap;
            if (night) Debug.Log($"NightLamps107: {phase.lamps.Length} lamps x{n.lampScale} range x{n.lampRange}, lean {n.lampShare} to {n.lampColour}, {teal} teal re-hued, live cap {phase.liveCap}");
        }
    }
}
