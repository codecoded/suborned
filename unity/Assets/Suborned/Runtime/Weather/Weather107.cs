using System;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 107: the weather's look from Resources/Atmosphere/weather107.json (suborned.weather/1; Glint's starting values, his to tune):
    /// per kind the sun and ambient factors for PlayView107, the wet ground and puddles (the _SubornedWet global every DecoKit forward pass reads,
    /// DK_Wet107.hlsl), the falling rain and the lamps' streaks (Rain107) and the ground mist (MistPass107). Look only. A kind missing from the file is clear.</summary>
    public static class Weather107
    {
        [Serializable] public sealed class Kind { public string name = "clear"; public float sun = 1f, ambient = 1f, desaturate = 0f, shadow = 1f, wet, puddles, ripples, rain, streaks, mist; }
        [Serializable] public sealed class Data
        {
            public string schema; public Kind[] kinds = Array.Empty<Kind>();
            public string mistDay = "#f3e8d4", mistNight = "#22323a";
            /// <summary>Stops of exposure the play light adds at night (TimeOfDay097's eye adapting); 0 by default: the carried night grade has its own.</summary>
            public float nightExposure;
            /// <summary>The share of indirect light (the lightmaps' day bounce, the ambient, the reflections) our shaders take away at night (_SubornedNight.x, DK_Wet107.hlsl).</summary>
            public float nightIndirect;
            /// <summary>Brief 107, Glint's testbed port (Sky 2026-11-11: "this city should thrive at night"): the night's lamps, uplights and grade. The old town's play look reads the same block (OldTownPlayLook.Night107.cs).</summary>
            public NightLook night = new();
            public Kind Of(string name) { foreach (var k in kinds) if (k.name == name) return k; return Clear; }
        }
        /// <summary>The night look (weather107.json `night`). Lamps: every night lamp (the carried LightPhase's and the play look's) at x`lampScale`, range x`lampRange`, its colour leaned toward
        /// `lampColour` by `lampShare`; a lamp whose hue is in `rehueFrom`..`rehueTo` degrees (the teal table lamp's pool) takes `rehueColour`; `liveCap` (0 = as built) lamps lit at once.
        /// Uplights (`up*`): a shadowless spot at the foot of each tree whose name starts with one of `upNames` (comma separated), aimed up the trunk. Grade (a volume over the night grade):
        /// `exposure` stops, `temperature`, `tint`, `saturation`, `contrast`, `filter` (colour), `shadowsNeutral` 0..1 (how far the night grade's teal shadow tint goes back to neutral).
        /// `ambient` and `moon`: x the old town's night trilight and moon. Look only.</summary>
        [Serializable] public sealed class NightLook
        {
            public float ambient = 1f, moon = 1f, lampScale = 1f, lampRange = 1f, lampShare = 0f; public string lampColour = "#ffc27e"; public int liveCap;
            public float rehueFrom, rehueTo; public string rehueColour = "";
            public float upIntensity, upRange = 7f, upAngle = 40f; public string upColour = "#ffe6b8", upNames = "";
            public float exposure, temperature, tint, saturation, contrast, shadowsNeutral; public string filter = "#ffffff";
        }
        static readonly Kind Clear = new();
        static readonly int WetId = Shader.PropertyToID("_SubornedWet"), NightId = Shader.PropertyToID("_SubornedNight");
        static Data cached; static Kind now = Clear;

        public static Data Load()
        {
            if (cached != null) return cached;
            var t = Resources.Load<TextAsset>("Atmosphere/weather107");
            var file = GameBoot.ArgValue("-weatherJson"); // tuning without a rebuild (Glint): a loose copy of weather107.json
            try { cached = file != null && System.IO.File.Exists(file) ? JsonUtility.FromJson<Data>(System.IO.File.ReadAllText(file)) : t ? JsonUtility.FromJson<Data>(t.text) : null; if (file != null) Debug.Log($"Weather107: {(System.IO.File.Exists(file) ? "read " : "NOT FOUND, the built one used: ")}{file}"); } catch (Exception e) { Debug.LogError("Weather107: weather107.json: " + e.Message); }
            if (cached == null || cached.schema != "suborned.weather/1") { Debug.LogError("Weather107: Resources/Atmosphere/weather107.json missing or not suborned.weather/1: every weather draws as clear"); cached = new Data { schema = "suborned.weather/1" }; }
            return cached;
        }

        /// <summary>The kind drawn now.</summary>
        public static Kind Now => now;
        public static bool Night { get; private set; }

        public static void Apply(string weather, bool night, Data d)
        {
            now = d.Of(weather); Night = night;
            Rain107.Set(now.rain, now.streaks);
            MistPass107.Amount = now.mist;
            MistPass107.Colour = ColorUtility.TryParseHtmlString(night ? d.mistNight : d.mistDay, out var c) ? c : Color.white;
            if (now.wet <= 0f) Shader.SetGlobalVector(WetId, Vector4.zero);
            Shader.SetGlobalVector(NightId, night ? new Vector4(Mathf.Clamp01(d.nightIndirect), 0f, 0f, 0f) : Vector4.zero);
        }

        /// <summary>Every frame: the ripple clock (unscaled: the pause tool does not freeze the rain) and the focus for the rain box and the mist's clear ring.</summary>
        public static void Frame(HandoverHost host)
        {
            if (now.wet > 0f) Shader.SetGlobalVector(WetId, new Vector4(now.wet, now.puddles, Time.unscaledTime, now.ripples));
            var f = host ? host.FocusPoint : Vector3.zero;
            Rain107.Focus = f; MistPass107.Focus = f;
        }
    }
}
