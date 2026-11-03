using System;
using Suborned.Play;
using UnityEngine;

namespace Suborned
{
    /// <summary>Glint's signal light data (Resources/Atmosphere/signals.json, suborned.signallight/1; briefs 056 F, 061, 063): the colours that must
    /// read the same under day, evening and night although the play grade differs: the crossing heads' state colours and levels, the silhouettes'
    /// fill and rim, the capsule cut's rim. LOOK ONLY: no rule, sight or replay reads it. The phase is LightPhase.Current; evening is eased by
    /// LightPhase.Dusk (0 day .. 1 evening): the colour lerps in linear and the level between `day` and `evening` by Dusk; night switches whole
    /// (as RoadLight does). Every colour in the file is the pre-grade sRGB hex. Read once; a missing or broken file keeps the built-in day values.</summary>
    public static class SignalLight
    {
        public const string ResourcePath = "Atmosphere/signals";

        [Serializable] public sealed class Lit { public string colour = ""; public float level = 1f; }
        [Serializable] public sealed class State { public string target = "", sameAs = ""; public Lit day, evening, night; }
        [Serializable] public sealed class States { public State walk, clearing, stop, arriving; }
        [Serializable] public sealed class Crossing { public States states; }
        [Serializable] public sealed class Phased { public string day = "", evening = "", night = ""; }
        [Serializable] public sealed class Silhouette { public Phased fill, edge; public float edgeM = 0.045f, marginM = 0.35f; }
        [Serializable] public sealed class Capsule { public float rim = 0.33f, rimLit = 2.5f; public string rimColor = "#F7F4EC"; }
        [Serializable] public sealed class Data { public string schema = ""; public Crossing crossing; public Silhouette silhouette; public Capsule capsule; }

        static Data data; static bool tried;

        /// <summary>The data, or null when the file is missing or not suborned.signallight/1 (logged once).</summary>
        public static Data D
        {
            get
            {
                if (tried) return data; tried = true;
                var text = Resources.Load<TextAsset>(ResourcePath);
                if (text == null) { Debug.LogWarning($"SignalLight: no Resources/{ResourcePath}.json: built-in values"); return null; }
                try { data = JsonUtility.FromJson<Data>(text.text); } catch (Exception e) { Debug.LogError($"SignalLight: signals.json: {e.Message}"); data = null; return null; }
                if (data == null || data.schema != "suborned.signallight/1") { Debug.LogError($"SignalLight: signals.json schema '{data?.schema}' is not suborned.signallight/1 (unknown = reject): built-in values"); data = null; }
                return data;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Forget() { data = null; tried = false; }

        /// <summary>An sRGB hex as a Color (sRGB), or the fallback.</summary>
        public static Color Hex(string hex, Color fallback) => !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;

        /// <summary>The phase now: night (whole), else Dusk (0 day .. 1 evening).</summary>
        public static (bool night, float dusk) Phase()
        {
            var lp = LightPhase.Instance; if (!lp) return (false, 0f);
            return (lp.Current == LightPhase.Phase.Night, lp.Current == LightPhase.Phase.Evening ? Mathf.Max(lp.Dusk, 0f) : lp.Dusk);
        }

        /// <summary>A phased sRGB colour (day, evening, night) now, lerped in linear by Dusk, returned as sRGB (a material Color takes sRGB and
        /// converts it to linear in a linear project; Glint: "set the material colour as the sRGB hex").</summary>
        public static Color PhasedColour(Phased p, Color fallback)
        {
            if (p == null) return fallback;
            var (night, dusk) = Phase(); Color day = Hex(p.day, fallback);
            if (night) return Hex(p.night, day);
            if (dusk <= 0f) return day;
            return Color.Lerp(day.linear, Hex(p.evening, day).linear, Mathf.Clamp01(dusk)).gamma;
        }

        /// <summary>The silhouettes' fill and rim now (Glint brief 056 F §3), and the rim's width and the cover margin (m).</summary>
        public static (Color fill, Color edge, float edgeM, float marginM) SilhouetteNow(Color fillDefault, Color edgeDefault, float edgeMDefault, float marginDefault)
        {
            var s = D?.silhouette; if (s == null) return (fillDefault, edgeDefault, edgeMDefault, marginDefault);
            return (PhasedColour(s.fill, fillDefault), PhasedColour(s.edge, edgeDefault), s.edgeM > 0f ? s.edgeM : edgeMDefault, s.marginM > 0f ? s.marginM : marginDefault);
        }

        /// <summary>A crossing head state's emission now (brief 061, Vela's lens data; Glint's trim): linear(colour) x level, HDR, for
        /// _EmissionColor by MaterialPropertyBlock (never the .mat default, which unity_mats.py writes gamma-style). `arriving` takes stop's
        /// values (sameAs). False when the data has no such state.</summary>
        public static bool HeadEmission(string state, out Color linearHdr, out Color srgb, out float level)
        {
            linearHdr = Color.black; srgb = Color.black; level = 0f;
            var st = StateOf(state); if (st == null || st.day == null) return false;
            var (night, dusk) = Phase();
            Lit a = st.day, b = st.evening ?? st.day;
            Color ca = Hex(a.colour, Color.white), cb = Hex(b.colour, ca);
            Color lin; float lv;
            if (night && st.night != null) { lin = Hex(st.night.colour, ca).linear; lv = st.night.level; }
            else { float t = Mathf.Clamp01(dusk); lin = Color.Lerp(ca.linear, cb.linear, t); lv = Mathf.Lerp(a.level, b.level, t); }
            srgb = lin.gamma; level = lv; linearHdr = new Color(lin.r * lv, lin.g * lv, lin.b * lv, 1f);
            return true;
        }

        static State StateOf(string state)
        {
            var ss = D?.crossing?.states; if (ss == null) return null;
            State s = state switch { "walk" => ss.walk, "clearing" => ss.clearing, "stop" => ss.stop, "arriving" => ss.arriving, _ => null };
            if (s != null && !string.IsNullOrEmpty(s.sameAs)) s = StateOf(s.sameAs);
            else if (s == null && state == "arriving") s = ss.stop;
            return s;
        }

        /// <summary>The capsule cut's rim (Glint brief 056 F §1): its strength, its cream (sRGB) and rimLit.</summary>
        public static (float rim, Color colour, float rimLit) CapsuleRim(float rimDefault, Color colourDefault, float litDefault)
        {
            var c = D?.capsule; if (c == null) return (rimDefault, colourDefault, litDefault);
            return (c.rim > 0f ? c.rim : rimDefault, Hex(c.rimColor, colourDefault), c.rimLit > 0f ? c.rimLit : litDefault);
        }
    }
}
