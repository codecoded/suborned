using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Play
{
    /// <summary>Brief 026 round 2 (Sky: a night version of the Handover; Glint, light.json `night`): the scene's two light states, day and
    /// night, written by the builder (Suborned.World.NightPhase) from light.json, and the switch between them. LOOK ONLY: nothing the sim,
    /// sight, schedules or a replay reads changes; the signals draw on their own camera outside the grade, as by day. Day is the state the
    /// scene is saved in, so a scene that never calls Set renders exactly as before.
    /// Night lamps (light.json night.lamps): lights beside real fixtures, no shadows, no cookies; at most `liveCap` lit at once, the nearest
    /// to the camera focus within `liveRadius`, the rest off; every lamp off by day.</summary>
    public sealed class LightPhase : MonoBehaviour
    {
        [Serializable]
        public sealed class State
        {
            public Color sunColour; public float sunIntensity; public Quaternion sunRotation;
            public Color ambientSky, ambientEquator, ambientGround;
            public Color fogColour; public float fogStart, fogEnd, fogDensity;
            public Material skybox;
            public Cubemap reflection; public float reflectionIntensity;
            public float probeIntensity = -1f; // night: every probe at this; day: each probe's own (probeIntensities)
            public Volume grade;
        }

        public State day = new(), night = new();
        public Light sun;
        public ReflectionProbe[] probes = Array.Empty<ReflectionProbe>();
        public float[] probeIntensities = Array.Empty<float>();
        [Tooltip("night.lamps: every spawned light, its night intensity")] public Light[] lamps = Array.Empty<Light>();
        public float[] lampIntensity = Array.Empty<float>();
        public int liveCap = 32; public float liveRadius = 60f;
        float[] lampBase; // the lamps' built intensities, kept by NightFloor when a group is scaled

        /// <summary>The phase of the day the look is in (Glint, brief 048). Day and Night are full looks (the two States above, built into the scene). Evening is the day State
        /// leaned toward the evening values of Resources/Atmosphere/lens.json `evening` by Dusk (0 day .. 1 evening, eased): a low warm sun with long shadows, a warmer ambient, softer reflections
        /// (here); the haze, the sky and the grade follow Dusk in DistanceHaze and AtmosphereLens; the dusk lights (the road's strip light, the lit windows) in RoadLight. Nothing of it is in the scene,
        /// so a scene that never calls SetEvening renders exactly as before. Look only.</summary>
        public enum Phase { Day, Evening, Night }

        public static LightPhase Instance { get; private set; }
        public bool Night { get; private set; }
        public Phase Current { get; private set; } = Phase.Day;
        /// <summary>How far into the evening the look is, 0 (day) to 1 (evening), eased toward the phase at lens.json evening.easeSeconds. Night snaps it to 0 (night is its own full State).</summary>
        public float Dusk { get; private set; }
        float duskApplied = -1f; bool duskGi;
        /// <summary>Where the lamp cap measures from (the play camera's focus on the ground); null: the main camera's ground point.</summary>
        public Func<Vector3?> Focus;
        public int LiveLamps { get; private set; }

        void OnEnable() { Instance = this; }
        void OnDisable() { if (Instance == this) Instance = null; }

        /// <summary>Day or night, now. Same mission, same replay: only the look changes.</summary>
        public void Set(bool toNight) { if (toNight) Dusk = 0f; Apply(toNight); Current = toNight ? Phase.Night : Phase.Day; } // RoadLight reads Current; day eases the dusk out

        /// <summary>Evening: the low warm sun, the dusk haze, sky and grade (eased in over lens.json evening.easeSeconds) and the dusk lights (the road's strip light, the lit windows). The street lamps stay off: they are night lamps.</summary>
        public void SetEvening() { Apply(false); Current = Phase.Evening; }

        void Apply(bool toNight)
        {
            Night = toNight; var s = toNight ? night : day;
            if (sun) { sun.color = s.sunColour; sun.intensity = s.sunIntensity; sun.transform.rotation = s.sunRotation; }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = s.ambientSky; RenderSettings.ambientEquatorColor = s.ambientEquator; RenderSettings.ambientGroundColor = s.ambientGround;
            RenderSettings.fogColor = s.fogColour; RenderSettings.fogStartDistance = s.fogStart; RenderSettings.fogEndDistance = s.fogEnd; RenderSettings.fogDensity = s.fogDensity;
            if (s.skybox) RenderSettings.skybox = s.skybox;
            if (s.reflection) { RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom; RenderSettings.customReflectionTexture = s.reflection; }
            RenderSettings.reflectionIntensity = s.reflectionIntensity;
            for (int i = 0; i < probes.Length; i++) if (probes[i]) probes[i].intensity = toNight && s.probeIntensity >= 0f ? s.probeIntensity : i < probeIntensities.Length ? probeIntensities[i] : 1f;
            if (day.grade) day.grade.enabled = !toNight;
            if (night.grade) night.grade.enabled = toNight;
            DynamicGI.UpdateEnvironment();
            if (!toNight) { foreach (var l in lamps) if (l) l.enabled = false; LiveLamps = 0; }
            else Cap(true);
            if (toNight) NightFloor();
            duskApplied = -1f; ApplyDusk(); // Apply wrote the day State: lean it back to where the dusk is (no pop when day is chosen from the evening)
        }

        /// <summary>The night floor (lens.json `night`, Glint 2026-10-24: night walls crushed to #020100): the ambient the scene's night State was built with is replaced by a higher teal-indigo one, at runtime, so no scene rebuild; the moon's shadow strength and the grade's lift are AtmosphereLens's. No lights.</summary>
        void NightFloor()
        {
            var n = AtmosphereLens.NightData; if (n == null) return;
            RenderSettings.ambientSkyColor = AtmosphereLens.Scaled(n.ambient.sky.hex, n.ambient.sky.value);
            RenderSettings.ambientEquatorColor = AtmosphereLens.Scaled(n.ambient.equator.hex, n.ambient.equator.value);
            RenderSettings.ambientGroundColor = AtmosphereLens.Scaled(n.ambient.ground.hex, n.ambient.ground.value);
            if (sun && n.moonIntensity > 0f) { sun.intensity = n.moonIntensity; if (!string.IsNullOrEmpty(n.moonColour)) sun.color = AtmosphereLens.Colour(n.moonColour); }
            if (n.lampScales != null && n.lampScales.Length > 0)
            {   // lamp groups at a share of the intensity they were built with (Cap writes lampIntensity, so the array is what changes; the first look is kept)
                lampBase ??= (float[])lampIntensity.Clone();
                for (int i = 0; i < lamps.Length && i < lampIntensity.Length; i++)
                {
                    float s = 1f; if (lamps[i]) foreach (var ls in n.lampScales) if (lamps[i].name.StartsWith(ls.prefix)) { s = ls.scale; break; }
                    lampIntensity[i] = lampBase[i] * s;
                }
            }
            if (n.violetToHueDeg > 0f)
                foreach (var l in lamps)
                {   // a violet pool beside a rival's ring reads as the ring's colour: lamps of that hue go to a flamingo, same saturation and value
                    if (!l) continue; Color.RGBToHSV(l.color, out var h, out var s, out var v); float hd = h * 360f;
                    if (s > 0.15f && hd >= n.violetFromDeg && hd <= n.violetToDeg) l.color = Color.HSVToRGB(n.violetToHueDeg / 360f, s, v);
                }
        }

        /// <summary>The sun, the ambient and the reflections at the evening's Dusk (lens.json `evening`), from the day State; nothing when Dusk is 0 or the lens has no evening.</summary>
        void ApplyDusk()
        {
            var e = AtmosphereLens.EveningData;
            if (e == null || Night || Mathf.Approximately(Dusk, duskApplied)) return;
            float k = Dusk; duskApplied = k; if (k <= 0f && duskGi == false) return; // never left the day State: nothing was written, nothing to undo
            duskGi = true;
            var s = day;
            if (sun)
            {
                sun.color = Color.Lerp(s.sunColour, AtmosphereLens.Colour(e.sun.colour), k); sun.intensity = Mathf.Lerp(s.sunIntensity, e.sun.intensity, k);
                sun.transform.rotation = Quaternion.Slerp(s.sunRotation, Quaternion.Euler(e.sun.pitch, e.sun.yaw, 0f), k);
            }
            RenderSettings.ambientSkyColor = Color.Lerp(s.ambientSky, AtmosphereLens.Scaled(e.ambient.sky.hex, e.ambient.sky.value), k);
            RenderSettings.ambientEquatorColor = Color.Lerp(s.ambientEquator, AtmosphereLens.Scaled(e.ambient.equator.hex, e.ambient.equator.value), k);
            RenderSettings.ambientGroundColor = Color.Lerp(s.ambientGround, AtmosphereLens.Scaled(e.ambient.ground.hex, e.ambient.ground.value), k);
            RenderSettings.reflectionIntensity = Mathf.Lerp(s.reflectionIntensity, e.reflection.intensity, k);
            float ps = Mathf.Lerp(1f, e.reflection.probeScale, k);
            for (int i = 0; i < probes.Length; i++) if (probes[i]) probes[i].intensity = (i < probeIntensities.Length ? probeIntensities[i] : 1f) * ps;
            if (k <= 0f || k >= 1f) { DynamicGI.UpdateEnvironment(); if (k <= 0f) duskGi = false; } // the ambient probe is recomputed at the ends of the ease, not on every step
        }


        float nextCap;
        void Update()
        {
            if (Night && Time.unscaledTime >= nextCap) Cap(false);
            var e = AtmosphereLens.EveningData; float want = Current == Phase.Evening && e != null ? 1f : 0f;
            if (!Mathf.Approximately(Dusk, want))
            {   // ease on unscaled time (the pause tool does not freeze the dusk), 95 percent in easeSeconds
                float a = e == null || e.easeSeconds <= 0f ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.05f, e.easeSeconds / 3f));
                Dusk = Mathf.Lerp(Dusk, want, a); if (Mathf.Abs(Dusk - want) < 0.002f) Dusk = want;
                ApplyDusk();
            }
        }

        readonly List<(float d2, int i)> order = new();
        /// <summary>The nearest `liveCap` lamps within `liveRadius` of the focus are lit at their night intensity; every other lamp is off.</summary>
        void Cap(bool now)
        {
            nextCap = Time.unscaledTime + 0.2f;
            Vector3? f = Focus?.Invoke();
            if (!f.HasValue)
            {
                var cam = Camera.main; if (!cam) return;
                var ray = new Ray(cam.transform.position, cam.transform.forward);
                f = Mathf.Abs(ray.direction.y) > 1e-3f ? ray.origin + ray.direction * (-ray.origin.y / ray.direction.y) : ray.origin;
            }
            order.Clear(); float r2 = liveRadius * liveRadius;
            for (int i = 0; i < lamps.Length; i++) { if (!lamps[i]) continue; var p = lamps[i].transform.position; float d2 = (p.x - f.Value.x) * (p.x - f.Value.x) + (p.z - f.Value.z) * (p.z - f.Value.z); if (d2 <= r2) order.Add((d2, i)); }
            order.Sort((a, b) => a.d2 != b.d2 ? a.d2.CompareTo(b.d2) : a.i.CompareTo(b.i));
            var live = new HashSet<int>(); for (int k = 0; k < order.Count && k < liveCap; k++) live.Add(order[k].i);
            for (int i = 0; i < lamps.Length; i++)
            {
                var l = lamps[i]; if (!l) continue; bool on = live.Contains(i);
                if (on) l.intensity = i < lampIntensity.Length ? lampIntensity[i] : l.intensity;
                if (l.enabled != on) l.enabled = on;
            }
            LiveLamps = live.Count;
        }
    }
}
