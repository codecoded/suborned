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

        void PlayLight107(float[] p)
        {
            if (p == null || p.Length < 5 || !lightSwap || TimeOfDay097.Running) return;
            if (!w107Bound)
            {
                w107Bound = true; w107Day = TimeOfDay097.BaseFrom(sunRotation, sunColour, sunIntensity, ambientSky, ambientEquator, ambientGround);
                w107Probe = ambientProbe != null ? (float[])ambientProbe.Clone() : new float[0]; w107AmbInt = ambientIntensity; Lamps107();
            }
            bool night = p[0] > 0.5f; float sunK = p[1], ambK = p[2], desat = p[3]; w107Night = night;
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
                l.intensity = table ? 1.2f : 6f; NightLamp107(l); w107Lamps.Add(l);
            }
        }

        /// <summary>At night the nearest LiveCap lamps to the camera's ground point are lit, the rest off (LightPhase's own rule).</summary>
        void Cap107()
        {
            w107Next = Time.unscaledTime + 0.25f;
            Vector3 f = cam ? cam.transform.position : Vector3.zero;
            if (cam) { var fw = cam.transform.forward; if (Mathf.Abs(fw.y) > 1e-3f) f = cam.transform.position + fw * (-cam.transform.position.y / fw.y); }
            var order = new List<(float d, Light l)>();
            foreach (var l in w107Lamps) { if (!l) continue; var d = l.transform.position - f; d.y = 0; if (w107Night && d.sqrMagnitude <= LiveRadius * LiveRadius) order.Add((d.sqrMagnitude, l)); }
            order.Sort((a, b) => a.d.CompareTo(b.d));
            var live = new HashSet<Light>(); for (int i = 0; i < order.Count && i < LiveCap; i++) live.Add(order[i].l);
            foreach (var l in w107Lamps) if (l) l.enabled = live.Contains(l);
        }

        void LateUpdate() { if (w107Bound && w107Night && Time.unscaledTime >= w107Next) Cap107(); }
    }
}
