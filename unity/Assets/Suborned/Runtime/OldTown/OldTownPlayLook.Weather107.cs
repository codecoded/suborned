using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.OldTown
{
    /// <summary>Brief 107 (Glint's fix: one driver owns the play light). Suborned.Play.PlayView107 (assembly Suborned.Runtime, which this assembly doesn't
    /// reference, nor it this one) sends the message "PlayLight107" with [night 0|1, sun x, ambient x, desaturate 0..1, shadow x, night exposure in stops]. This writes the play
    /// look's own light fields from it: by day the look's built light with the weather's factors; at night TimeOfDay097's night at NightHour (the moon,
    /// the dark trilight and its probe), its exposure, and warm shadowless lamps at the old town's lamp heads (the nearest LiveCap to the camera's ground
    /// point, none within 2.5 m of a lamp under the carried night root). The play grade is off at night when the carried night grade is there. While
    /// TimeOfDay097's dev clock runs it owns the light and this writes nothing. Look only.</summary>
    public sealed partial class OldTownPlayLook
    {
        public const float NightHour = 21.5f;
        const string NightRoot = "Night (our LightPhase, brief 107)";
        const int LiveCap = 48; const float LiveRadius = 70f;
        static readonly string[] LampNames = { "Streetlamp", "SphereLamp", "GlobeLamp", "BackDoorLamp", "CafeLamp" };

        bool w107Bound, w107Night; TimeOfDay097.Base w107Day; float[] w107Probe; float w107AmbInt; float w107Next;
        Volume w107Ev; ColorAdjustments w107Adj; readonly List<Light> w107Lamps = new();
        readonly List<float> baseLampPower = new();
        readonly List<(float d, Light l)> byDistance = new();
        readonly HashSet<Light> liveLamps = new();
        float lampShare;

        void PlayLight107(float[] p)
        {
            if (p == null || p.Length < 5 || !lightSwap || TimeOfDay097.Running) return;
            if (!w107Bound)
            {
                w107Bound = true; w107Day = TimeOfDay097.BaseFrom(sunRotation, sunColour, sunIntensity, ambientSky, ambientEquator, ambientGround);
                w107Probe = ambientProbe != null ? (float[])ambientProbe.Clone() : new float[0]; w107AmbInt = ambientIntensity; Lamps107();
            }
            bool night = p[0] > 0.5f; float sunK = p[1], ambK = p[2], desat = p[3]; w107Night = night;
            lampShare = night ? 1f : 0f;
            var s = night ? TimeOfDay097.Evaluate(NightHour, w107Day, true) : default;
            Color sun = night ? s.sun : w107Day.sun, sky = night ? s.sky : w107Day.sky, eq = night ? s.eq : w107Day.eq, gr = night ? s.ground : w107Day.ground;
            sun = Color.Lerp(sun, new Color(sun.grayscale, sun.grayscale, sun.grayscale), desat);
            if (!night) { var cool = new Color(0.94f, 0.98f, 1.06f); sky = Color.Lerp(sky, sky * cool, desat) * ambK; eq = Color.Lerp(eq, eq * cool, desat) * ambK; gr *= ambK; }
            if (night) { var n = Night107(); sky *= n.ambient; eq *= n.ambient; gr *= n.ambient; } // Glint's testbed port: a darker base under the pools
            sunRotation = night ? s.rot : w107Day.rot; sunColour = sun; sunIntensity = (night ? s.intensity * Night107().moon : w107Day.intensity) * sunK; shadowScale = p[4];
            ambientSky = sky; ambientEquator = eq; ambientGround = gr; ambientIntensity = w107AmbInt;
            bool plain = !night && Mathf.Approximately(sunK, 1f) && Mathf.Approximately(ambK, 1f) && desat <= 0f;
            ambientProbe = plain ? w107Probe : TimeOfDay097.Probe(sky, eq, gr);
            if (held) PlayAmbient();
            var nightRoot = GameObject.Find(NightRoot);
            var nightGrade = nightRoot ? nightRoot.transform.Find("grade-night") : null;
            if (playVolume) playVolume.enabled = !(night && nightGrade);
            Grade107(night, night ? (p.Length > 5 ? p[5] : s.ev) : 0f, nightGrade); // weather107.json nightExposure (0: the carried night grade has its own) and its night block's grade
            Uplights107(night);
            Cap107();
        }

        // In free roam the same clock as the HUD moves the sun, moon and shadow direction.
        // The original briefing and photo overrides still apply their authored light states.
        void StreetSun(float[] p)
        {
            if (!w107Bound || TimeOfDay097.Running) return;
            var s = TimeOfDay097.Evaluate(p[0], w107Day);
            float mix = p[4];
            var n = Night107();
            sunRotation = s.rot;
            sunColour = Color.Lerp(s.sun, new Color(s.sun.grayscale, s.sun.grayscale, s.sun.grayscale), p[3]);
            sunIntensity = s.intensity * p[1] * (s.moon ? n.moon : 1f);
            float fill = Mathf.Lerp(p[2], n.ambient, mix);
            var cool = Color.Lerp(Color.white, new Color(.94f,.98f,1.06f), p[3] * (1f - mix));
            ambientSky = s.sky * cool * fill; ambientEquator = s.eq * cool * fill; ambientGround = s.ground * fill;
            ambientProbe = TimeOfDay097.Probe(ambientSky, ambientEquator, ambientGround);
            if (held) PlayAmbient();
            if (!Mathf.Approximately(lampShare, mix)) { lampShare = mix; Cap107(); }
        }

        void Lamps107()
        {
            var taken = new List<Vector3>(); var root = GameObject.Find(NightRoot);
            if (root) foreach (var l in root.GetComponentsInChildren<Light>(true)) taken.Add(l.transform.position);
            var seen = new HashSet<Vector3Int>(); var parent = new GameObject("play light lamps (brief 107)") { hideFlags = HideFlags.DontSave }.transform;
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                string n = r.name; int k = Array.FindIndex(LampNames, x => n.Contains(x)); if (k < 0) continue;
                var bb = r.bounds; bool table = LampNames[k] == "CafeLamp"; var head = new Vector3(bb.center.x, bb.max.y - (table ? 0.15f : 0.35f), bb.center.z);
                if (!seen.Add(Vector3Int.RoundToInt(head))) continue;
                bool near = false; foreach (var t in taken) if ((t - head).sqrMagnitude < 2.5f * 2.5f) { near = true; break; }
                if (near) continue;
                var go = new GameObject("lamp"); go.transform.SetParent(parent, false); go.transform.position = head;
                var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.74f, 0.46f); l.range = table ? 3.5f : 9f; l.shadows = LightShadows.None; l.enabled = false;
                l.intensity = table ? 1.2f : 6f; NightLamp107(l); w107Lamps.Add(l); baseLampPower.Add(l.intensity);
            }
        }

        /// <summary>At night the nearest LiveCap lamps to the camera's ground point are lit, the rest off (LightPhase's own rule).</summary>
        void Cap107()
        {
            w107Next = Time.unscaledTime + 0.25f;
            Vector3 f = cam ? cam.transform.position : Vector3.zero;
            if (cam)
            {
                var fw = cam.transform.forward;
                float reach = fw.y < -.001f ? Mathf.Max(0f, -f.y / fw.y) : 30f;
                f += fw * (cam.orthographic ? reach : Mathf.Min(reach, 30f));
            }
            byDistance.Clear(); liveLamps.Clear();
            if (lampShare > .005f)
            {
                foreach (var l in w107Lamps) { if (!l) continue; var d = l.transform.position - f; d.y = 0; if (d.sqrMagnitude <= LiveRadius * LiveRadius) byDistance.Add((d.sqrMagnitude, l)); }
                byDistance.Sort((a, b) => a.d.CompareTo(b.d));
                for (int i = 0; i < byDistance.Count && i < LiveCap; i++) liveLamps.Add(byDistance[i].l);
            }
            for (int i = 0; i < w107Lamps.Count; i++)
            {
                var lamp = w107Lamps[i]; if (!lamp) continue;
                lamp.enabled = liveLamps.Contains(lamp);
                if (lamp.enabled) lamp.intensity = baseLampPower[i] * lampShare;
            }
        }

        void LateUpdate() { if (w107Bound && lampShare > .005f && Time.unscaledTime >= w107Next) Cap107(); }
    }
}
