using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Suborned.OldTown;

namespace Suborned
{
    /// <summary>Brief 097 time-of-day test (Sky 2026-11-07: "Test time of day and show me"). A clock that drives the play light through a day: the sun's
    /// path, colour and strength, the trilight sky (and its ambient probe), a little exposure as it gets dark, the reflection probes' strength, and, in a
    /// scene baked with Adaptive Probe Volumes lighting scenarios (Editor TimeOfDay097Bake), the blend between the baked bounce of the nearest two times.
    /// It writes OldTownPlayLook's fields (the play camera's light swap reads them at every render), so nothing fights it.
    /// Off unless asked: -tod H (a fixed hour, 0..24), or -todFrom H [-todSpeed S] (a running clock, S hours per game second, default 0.75; it holds at H
    /// until a capture clip starts, Time.captureFramerate, so a clip shows the same hours every run). In a scene without the scenarios (today's
    /// lightmaps) it still moves the light: the baked bounce then stays where the bake's sun put it, which is the point of the comparison.
    /// The day here: the play look's own sun is a morning sun (Glint's south-east light), so its elevation sets the hour it stands for (Base.hour).</summary>
    public sealed class TimeOfDay097 : MonoBehaviour
    {
        /// <summary>The baked scenarios and the hour each was baked at (Editor TimeOfDay097Bake bakes these, in this order).</summary>
        public static readonly (string name, float hour)[] Keys = { ("morning", 8f), ("noon", 12.5f), ("evening", 17.6f), ("night", 20.6f) };
        const float NoonElevation = 62f, Lat = 15f; // degrees: the sun's height at noon; degrees of azimuth per hour

        /// <summary>The play look's own light, which the day is built around.</summary>
        public struct Base { public Quaternion rot; public Color sun, sky, eq, ground; public float intensity, hour, azimuth; }

        /// <summary>The light at one hour.</summary>
        public struct State { public Quaternion rot; public Color sun, sky, eq, ground; public float intensity, ev, reflect, elevation; public bool moon; }

        public static Base BaseFrom(Quaternion sunRot, Color sun, float intensity, Color sky, Color eq, Color ground)
        {
            var toSun = -(sunRot * Vector3.forward);
            float el = Mathf.Asin(Mathf.Clamp(toSun.y, -1f, 1f)) * Mathf.Rad2Deg, az = Mathf.Atan2(toSun.x, toSun.z) * Mathf.Rad2Deg;
            float hour = 6f + 12f / Mathf.PI * Mathf.Asin(Mathf.Clamp(el / NoonElevation, -1f, 1f)); // the morning side: Glint's sun is from the south-east
            return new Base { rot = sunRot, sun = sun, intensity = intensity, sky = sky, eq = eq, ground = ground, hour = hour, azimuth = az };
        }

        static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        static Color Mul(Color c, float r, float g, float b) => new Color(c.r * r, c.g * g, c.b * b, 1f);

        public static State Evaluate(float hour, in Base b)
        {
            float el = NoonElevation * Mathf.Sin(Mathf.PI * (hour - 6f) / 12f); // 0 at 6 and 18, the noon height at 12, below the horizon at night
            float az = b.azimuth + Lat * (hour - b.hour);
            var s = new State { elevation = el };
            float er = el * Mathf.Deg2Rad, ar = az * Mathf.Deg2Rad;
            var toSun = new Vector3(Mathf.Sin(ar) * Mathf.Cos(er), Mathf.Sin(er), Mathf.Cos(ar) * Mathf.Cos(er));
            // the sun: warm and weak near the horizon, the play look's own colour and strength from about its own height up
            float baseEl = NoonElevation * Mathf.Sin(Mathf.PI * (b.hour - 6f) / 12f);
            var low = new Color(1f, 0.55f, 0.32f); var mid = new Color(1f, 0.76f, 0.56f);
            var col = el < 12f ? Color.Lerp(low, mid, Smooth(1f, 12f, el)) : Color.Lerp(mid, b.sun, Smooth(12f, Mathf.Max(baseEl, 24f), el));
            float strength = b.intensity * Mathf.Clamp(Mathf.Sin(Mathf.Max(el, 0f) * Mathf.Deg2Rad) / Mathf.Max(Mathf.Sin(baseEl * Mathf.Deg2Rad), 0.2f), 0f, 1f); // never brighter than the play look's own sun
            strength *= Smooth(-1.5f, 6f, el);
            // the moon once the sun is well down: a cool weak light from high in the opposite sky (its own shadows)
            float moon = Smooth(-5f, -12f, el);
            if (strength < 0.02f * b.intensity && moon > 0f)
            {
                float mr = (az + 160f) * Mathf.Deg2Rad, me = 38f * Mathf.Deg2Rad;
                toSun = new Vector3(Mathf.Sin(mr) * Mathf.Cos(me), Mathf.Sin(me), Mathf.Cos(mr) * Mathf.Cos(me));
                col = new Color(0.7f, 0.76f, 1f); strength = b.intensity * 0.11f * moon; s.moon = true;
            }
            s.rot = Quaternion.LookRotation(-toSun, Vector3.up); s.sun = col; s.intensity = strength;
            // the sky (trilight): the play look's own by day; warm at the horizon round sunset; deep blue, then dark after it
            float day = Smooth(4f, 20f, el), gold = Smooth(-3f, 4f, el) * (1f - day), dusk = Smooth(-12f, -2f, el) * (1f - Smooth(-3f, 4f, el));
            float night = 1f - Smooth(-14f, -6f, el);
            Color sky = b.sky * day + Mul(b.sky, 0.82f, 0.82f, 0.95f) * gold + Mul(b.sky, 0.34f, 0.32f, 0.72f) * dusk; // violet-blue overhead
            Color eq = b.eq * day + Mul(b.eq, 1.18f, 0.92f, 0.78f) * gold + Mul(b.eq, 0.82f, 0.48f, 0.5f) * dusk; // a warm pink horizon
            Color gr = b.ground * day + Mul(b.ground, 1.05f, 0.92f, 0.82f) * gold + Mul(b.ground, 0.32f, 0.26f, 0.32f) * dusk;
            float rest = Mathf.Max(0f, 1f - day - gold - dusk); // deep dusk to night
            sky += Mul(b.sky, Mathf.Lerp(0.17f, 0.07f, night), Mathf.Lerp(0.15f, 0.08f, night), Mathf.Lerp(0.42f, 0.16f, night)) * rest;
            eq += Mul(b.eq, Mathf.Lerp(0.26f, 0.07f, night), Mathf.Lerp(0.16f, 0.06f, night), Mathf.Lerp(0.3f, 0.11f, night)) * rest;
            gr += Mul(b.ground, Mathf.Lerp(0.12f, 0.04f, night), Mathf.Lerp(0.12f, 0.04f, night), Mathf.Lerp(0.16f, 0.06f, night)) * rest;
            s.sky = sky; s.eq = eq; s.ground = gr;
            s.ev = Mathf.Lerp(0f, 1.0f, Smooth(6f, -14f, el)); // the eye adapting as it gets dark (post exposure)
            s.reflect = Mathf.Clamp(Luma(sky) / Mathf.Max(Luma(b.sky), 1e-3f), 0.12f, 1f);
            return s;
        }

        static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        /// <summary>The two baked scenarios round an hour and the blend between them (wrapping through midnight).</summary>
        public static (string a, string b, float t) Scenario(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            for (int i = 0; i < Keys.Length; i++)
            {
                var k0 = Keys[i]; var k1 = Keys[(i + 1) % Keys.Length];
                float h1 = k1.hour > k0.hour ? k1.hour : k1.hour + 24f, h = hour >= k0.hour ? hour : hour + 24f;
                if (h >= k0.hour && h <= h1) return (k0.name, k1.name, Mathf.Clamp01((h - k0.hour) / (h1 - k0.hour)));
            }
            return (Keys[0].name, Keys[0].name, 0f);
        }

        /// <summary>A trilight's ambient probe: the equator all round, the sky added from above and the ground from below.</summary>
        public static float[] Probe(Color sky, Color eq, Color ground)
        {
            var sh = new SphericalHarmonicsL2(); sh.AddAmbientLight(eq);
            sh.AddDirectionalLight(Vector3.up, new Color(Mathf.Max(0, sky.r - eq.r), Mathf.Max(0, sky.g - eq.g), Mathf.Max(0, sky.b - eq.b)), 1.6f);
            sh.AddDirectionalLight(Vector3.down, new Color(Mathf.Max(0, ground.r - eq.r * 0.5f), Mathf.Max(0, ground.g - eq.g * 0.5f), Mathf.Max(0, ground.b - eq.b * 0.5f)), 0.6f);
            var f = new float[27]; for (int ch = 0; ch < 3; ch++) for (int k = 0; k < 9; k++) f[ch * 9 + k] = sh[ch, k]; return f;
        }

        // ---- the runtime clock ----
        static float fixedHour = -1f, fromHour = -1f, speed = 0.75f;
        OldTownPlayLook look; Base b; bool bound; float clock, nextLog; bool started; string scenarioA;
        Volume evVolume; ColorAdjustments ev; readonly List<(ReflectionProbe p, float i)> probes = new();
        readonly List<(Light l, float i)> lamps = new(); static readonly string[] LampNames = { "Streetlamp", "SphereLamp", "GlobeLamp", "BackDoorLamp", "CafeLamp" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var a = Environment.GetCommandLineArgs();
            fixedHour = Arg(a, "-tod", -1f); fromHour = Arg(a, "-todFrom", -1f); speed = Arg(a, "-todSpeed", 0.75f);
            if (fixedHour < 0f && fromHour < 0f) return;
            var go = new GameObject("TimeOfDay097") { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave };
            DontDestroyOnLoad(go); go.AddComponent<TimeOfDay097>();
        }

        static float Arg(string[] a, string name, float d) { int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length && float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : d; }

        void OnEnable() { SceneManager.sceneLoaded += Rebind; clock = fixedHour >= 0f ? fixedHour : fromHour; }
        void OnDisable() { SceneManager.sceneLoaded -= Rebind; }
        void Rebind(Scene _, LoadSceneMode __) { bound = false; look = null; probes.Clear(); lamps.Clear(); }

        void Bind()
        {
            look = FindAnyObjectByType<OldTownPlayLook>(); if (!look || !look.lightSwap) return;
            b = BaseFrom(look.sunRotation, look.sunColour, look.sunIntensity, look.ambientSky, look.ambientEquator, look.ambientGround);
            foreach (var p in FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude)) probes.Add((p, p.intensity));
            Lamps();
            if (!evVolume)
            {
                var go = new GameObject("TimeOfDay097 exposure"); DontDestroyOnLoad(go); evVolume = go.AddComponent<Volume>(); evVolume.isGlobal = true; evVolume.priority = 1001f;
                var prof = ScriptableObject.CreateInstance<VolumeProfile>(); ev = prof.Add<ColorAdjustments>(false); ev.postExposure.Override(0f); evVolume.sharedProfile = prof;
            }
            var prv = ProbeReferenceVolume.instance; var set = prv.currentBakingSet;
            Debug.Log($"TimeOfDay097: bound; pipeline {GraphicsSettings.currentRenderPipeline?.name}; the play sun stands for {b.hour:0.0} h (azimuth {b.azimuth:0}); probe volumes {(set ? $"on, scenarios {string.Join("/", set.lightingScenarios)}" : "not registered yet")}; " +
                      (fixedHour >= 0f ? $"fixed at {fixedHour:0.00} h" : $"from {fromHour:0.00} h at {speed} h per game second"));
            bound = true;
        }

        /// <summary>A warm shadowless point light at the head of every lamp kit piece in the scene (one per lamp, found by the piece's name; the
        /// café's table lamps smaller), off by day. Forward+ takes dozens of small lights; no shadows.</summary>
        void Lamps()
        {
            var seen = new HashSet<Vector3Int>(); var root = new GameObject("TimeOfDay097 lamps"); root.transform.SetParent(transform, false);
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                string n = r.name; int k = Array.FindIndex(LampNames, x => n.Contains(x)); if (k < 0) continue;
                var bb = r.bounds; bool table = LampNames[k] == "CafeLamp"; var head = new Vector3(bb.center.x, bb.max.y - (table ? 0.15f : 0.35f), bb.center.z);
                if (!seen.Add(Vector3Int.RoundToInt(head))) continue;
                var go = new GameObject("lamp"); go.transform.SetParent(root.transform, false); go.transform.position = head;
                var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.74f, 0.46f); l.range = table ? 3.5f : 9f; l.shadows = LightShadows.None; l.enabled = false;
                float i = table ? 1.2f : 6f; l.intensity = 0f; lamps.Add((l, i));
            }
            Debug.Log($"TimeOfDay097: {lamps.Count} lamp lights");
        }

        void Update()
        {
            if (!bound) { Bind(); if (!bound) return; }
            if (fixedHour < 0f)
            {
                if (!started) started = Time.captureFramerate > 0 || !Array.Exists(Environment.GetCommandLineArgs(), x => x == "-shots");
                if (started) clock += speed * Time.deltaTime;
            }
            float h = Mathf.Repeat(clock, 24f);
            var s = Evaluate(h, b);
            look.sunRotation = s.rot; look.sunColour = s.sun; look.sunIntensity = s.intensity;
            look.ambientSky = s.sky; look.ambientEquator = s.eq; look.ambientGround = s.ground; look.ambientProbe = Probe(s.sky, s.eq, s.ground);
            if (ev) ev.postExposure.Override(s.ev);
            foreach (var (p, i) in probes) if (p) p.intensity = i * s.reflect;
            float lit = Smooth(1f, -6f, s.elevation); // the street lamps after sunset
            foreach (var (l, i) in lamps) if (l) { l.intensity = i * lit; l.enabled = lit > 0.01f; }
            var prv = ProbeReferenceVolume.instance;
            if (prv.currentBakingSet != null && prv.currentBakingSet.lightingScenarios.Count > 1)
            {
                var (a, bName, t) = Scenario(h);
                if (a != scenarioA) { prv.lightingScenario = a; scenarioA = a; }
                prv.BlendLightingScenario(bName, t);
            }
            if (Time.time >= nextLog)
            {
                nextLog = Time.time + 1f; var bs = prv.currentBakingSet;
                Debug.Log($"TimeOfDay097: {h:00.00} h, sun {s.elevation:0}°{(s.moon ? " (moon)" : "")} x{s.intensity:0.00}, ev +{s.ev:0.00}, probe volumes {(bs ? $"{prv.lightingScenario} -> {prv.otherScenario} {prv.scenarioBlendingFactor:0.00}" : "off")}, frame {Time.frameCount}");
            }
        }
    }
}
