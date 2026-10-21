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

        public static LightPhase Instance { get; private set; }
        public bool Night { get; private set; }
        /// <summary>Where the lamp cap measures from (the play camera's focus on the ground); null: the main camera's ground point.</summary>
        public Func<Vector3?> Focus;
        public int LiveLamps { get; private set; }

        void OnEnable() { Instance = this; }
        void OnDisable() { if (Instance == this) Instance = null; }

        /// <summary>Day or night, now. Same mission, same replay: only the look changes.</summary>
        public void Set(bool toNight)
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
        }

        float nextCap;
        void Update() { if (Night && Time.unscaledTime >= nextCap) Cap(false); }

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
