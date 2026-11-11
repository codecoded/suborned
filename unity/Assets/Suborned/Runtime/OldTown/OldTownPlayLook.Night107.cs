using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.OldTown
{
    /// <summary>Brief 107, Glint's testbed port (Sky 2026-11-11: "What's with this horrible paving colors at night", "Look at the test bed scene and lighting used for the cafe",
    /// "This city should thrive at night"). The testbed's café night is a dark base under many warm pools: a weak cool moon and sky (0.18, 0.12), exposure 1.05, warm lamps,
    /// the terrace's wall lights and the palms' uplights, the sign lit. This reads weather107.json `night` (the same block Suborned.Play.Weather107 reads; this assembly can't
    /// reference that one; `-weatherJson` a loose copy) and gives the play look's night: the trilight x`ambient`, the moon x`moon`, its lamps warmer and stronger, a shadowless
    /// uplight at the foot of each named tree, and a grade over the carried night grade (exposure, white balance, contrast, saturation, filter, the teal shadow tint taken
    /// back toward neutral). Look only.</summary>
    public sealed partial class OldTownPlayLook
    {
        [Serializable] sealed class N107
        {
            public float ambient = 1f, moon = 1f, lampScale = 1f, lampRange = 1f, lampShare = 0f; public string lampColour = "#ffc27e";
            public float upIntensity, upRange = 7f, upAngle = 40f; public string upColour = "#ffe6b8", upNames = "";
            public float exposure, temperature, tint, saturation, contrast, shadowsNeutral; public string filter = "#ffffff";
        }
        [Serializable] sealed class F107 { public N107 night = new(); }

        static N107 n107;
        readonly List<Light> w107Up = new(); bool w107UpMade;
        WhiteBalance w107Wb; ShadowsMidtonesHighlights w107Smh;

        static N107 Night107()
        {
            if (n107 != null) return n107;
            string text = null; var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-weatherJson");
            if (i >= 0 && i + 1 < a.Length && System.IO.File.Exists(a[i + 1])) text = System.IO.File.ReadAllText(a[i + 1]);
            else { var t = Resources.Load<TextAsset>("Atmosphere/weather107"); if (t) text = t.text; }
            try { n107 = text != null ? JsonUtility.FromJson<F107>(text)?.night : null; } catch (Exception e) { Debug.LogError("OldTownPlayLook: weather107.json night: " + e.Message); }
            return n107 ??= new N107();
        }

        static bool Starts(string s, string[] names) { foreach (var p in names) { var q = p.Trim(); if (q.Length > 0 && s.StartsWith(q)) return true; } return false; }
        static Color Hex(string h, Color or) => ColorUtility.TryParseHtmlString(h, out var c) ? c : or;

        /// <summary>A night lamp of the play look as the night block says (called as each is made; they are lit only at night).</summary>
        static void NightLamp107(Light l)
        {
            var n = Night107();
            l.color = Color.Lerp(l.color, Hex(n.lampColour, l.color), Mathf.Clamp01(n.lampShare));
            l.intensity *= Mathf.Max(0f, n.lampScale); l.range *= Mathf.Max(0.1f, n.lampRange);
        }

        /// <summary>The uplights at the foot of the named trees (made once, the first night), on at night only.</summary>
        void Uplights107(bool night)
        {
            var n = Night107();
            if (!w107UpMade && night && n.upIntensity > 0f && !string.IsNullOrEmpty(n.upNames))
            {
                w107UpMade = true; var names = n.upNames.Split(','); var seen = new HashSet<Vector3Int>(); var parent = new GameObject("play light uplights (brief 107)") { hideFlags = HideFlags.DontSave }.transform;
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
                {
                    if (!Starts(t.name, names) || t.parent && Starts(t.parent.name, names) || !seen.Add(Vector3Int.RoundToInt(t.position))) continue; // the tree's own root, once
                    var at = t.position; var go = new GameObject("uplight " + t.name); go.transform.SetParent(parent, false);
                    go.transform.position = at + new Vector3(0.35f, 0.2f, -0.35f); go.transform.LookAt(at + Vector3.up * 3.5f);
                    var l = go.AddComponent<Light>(); l.type = LightType.Spot; l.shadows = LightShadows.None; l.color = Hex(n.upColour, Color.white);
                    l.intensity = n.upIntensity; l.range = n.upRange; l.spotAngle = n.upAngle; l.innerSpotAngle = n.upAngle * 0.4f; l.enabled = false; w107Up.Add(l);
                }
                Debug.Log($"OldTownPlayLook: {w107Up.Count} tree uplights ({n.upNames})");
            }
            foreach (var l in w107Up) if (l) l.enabled = night;
        }

        /// <summary>The night grade on top of the carried one (and TimeOfDay097's eye adapting, `ev`): only what the block sets is overridden.</summary>
        void Grade107(bool night, float ev, Transform nightGrade)
        {
            var n = Night107();
            if (!w107Ev)
            {
                var go = new GameObject("play light exposure (brief 107)") { hideFlags = HideFlags.DontSave }; w107Ev = go.AddComponent<Volume>(); w107Ev.isGlobal = true; w107Ev.priority = 1001f;
                var prof = ScriptableObject.CreateInstance<VolumeProfile>(); prof.hideFlags = HideFlags.DontSave; w107Adj = prof.Add<ColorAdjustments>(false); w107Wb = prof.Add<WhiteBalance>(false); w107Smh = prof.Add<ShadowsMidtonesHighlights>(false); w107Ev.sharedProfile = prof;
            }
            ColorAdjustments ca = null; ShadowsMidtonesHighlights smh = null; var vol = nightGrade ? nightGrade.GetComponent<Volume>() : null; var gp = vol ? vol.sharedProfile : null;
            if (gp) { gp.TryGet(out ca); gp.TryGet(out smh); }
            float e0 = ca && ca.postExposure.overrideState ? ca.postExposure.value : 0f, c0 = ca && ca.contrast.overrideState ? ca.contrast.value : 0f, s0 = ca && ca.saturation.overrideState ? ca.saturation.value : 0f;
            bool g = night; float exp = e0 + ev + (g ? n.exposure : 0f);
            w107Adj.postExposure.overrideState = Mathf.Abs(exp - e0) > 1e-4f; w107Adj.postExposure.value = exp;
            w107Adj.contrast.overrideState = g && n.contrast != 0f; w107Adj.contrast.value = c0 + n.contrast;
            w107Adj.saturation.overrideState = g && n.saturation != 0f; w107Adj.saturation.value = s0 + n.saturation;
            var f = Hex(n.filter, Color.white); w107Adj.colorFilter.overrideState = g && f != Color.white; w107Adj.colorFilter.value = f;
            w107Wb.temperature.overrideState = g && n.temperature != 0f; w107Wb.temperature.value = n.temperature;
            w107Wb.tint.overrideState = g && n.tint != 0f; w107Wb.tint.value = n.tint;
            bool neutral = g && smh && n.shadowsNeutral > 0f; w107Smh.shadows.overrideState = neutral;
            if (neutral) w107Smh.shadows.value = Vector4.Lerp(smh.shadows.value, new Vector4(1f, 1f, 1f, 0f), Mathf.Clamp01(n.shadowsNeutral));
            w107Ev.enabled = w107Adj.postExposure.overrideState || w107Adj.contrast.overrideState || w107Adj.saturation.overrideState || w107Adj.colorFilter.overrideState || w107Wb.temperature.overrideState || w107Wb.tint.overrideState || neutral;
        }
    }
}
