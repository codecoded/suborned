using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Glint, brief 096 (the grading review found the red cone ring shifting with the grade): GDD §10 / §15 say the signals (cones red, guard amber,
    /// rivals violet) draw unlit and emissive, outside the post grade. SignalsFeature already draws its layer after post, but only the fight lab put
    /// anything on it: in the Handover the cones, the guard's and the rivals' rings and lamps were on the Default layer, so the play grade, bloom and the
    /// new LUT reached them. This moves every renderer under the host whose material is a signal material (Suborned/SignalUnlit, or the host's unlit and fan templates it falls back to) in a signal colour (the host's cone, guard
    /// or rival colour, by hue) onto SignalsFeature.Layer, adds the layer to the game cameras' culling masks, and asks for the pass each frame one is
    /// drawn. Ink and shade that share the shader (the figures' under-shadow) are not signal colours and stay in the scene. Look only; nothing reads it.</summary>
    public sealed class SignalsOutsideGrade : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (!FindAnyObjectByType<SignalsOutsideGrade>()) { var go = new GameObject("signals outside the grade"); DontDestroyOnLoad(go); go.AddComponent<SignalsOutsideGrade>(); } }

        readonly List<Renderer> rs = new(); readonly HashSet<Material> yes = new(), no = new();
        HandoverHost host; Shader signal; int moved;
        const int Every = 15; const float HueTol = 0.045f, MinSat = 0.45f;

        void Update()
        {
            if (moved > 0) SignalsFeature.Want();
            if (Time.frameCount % Every != 0) return;
            if (!host) { host = FindAnyObjectByType<HandoverHost>(); yes.Clear(); no.Clear(); if (!host) return; }
            if (!signal) signal = Shader.Find("Suborned/SignalUnlit"); // may be absent in a player: HandoverHost.SignalMat then copies its unlit templates
            int mask = 1 << SignalsFeature.Layer;
            foreach (var c in Camera.allCameras) if (c.cameraType == CameraType.Game && c.targetTexture == null && (c.cullingMask & 1) != 0) c.cullingMask |= mask;
            host.GetComponentsInChildren(true, rs); int n = 0;
            foreach (var r in rs)
            {
                if (r.gameObject.layer == SignalsFeature.Layer) { n++; continue; }
                if (r.gameObject.layer != 0 || r is not (MeshRenderer or LineRenderer or SkinnedMeshRenderer)) continue;
                var m = r.sharedMaterial; if (!m || !IsSignal(m)) continue;
                r.gameObject.layer = SignalsFeature.Layer; n++;
            }
            if (n != moved) Debug.Log($"SignalsOutsideGrade: {n} signal renderers drawn after post (layer {SignalsFeature.Layer}); {rs.Count} under the host, {yes.Count} signal materials, {no.Count} others");
            moved = n;
        }

        bool IsSignal(Material m)
        {
            if (yes.Contains(m)) return true; if (no.Contains(m)) return false;
            var sh = m.shader;
            bool kind = sh && (sh == signal || host.unlitTemplate && sh == host.unlitTemplate.shader || host.fanTemplate && sh == host.fanTemplate.shader);
            bool s = kind && m.HasProperty("_BaseColor") && Near(m.GetColor("_BaseColor"));
            (s ? yes : no).Add(m); return s;
        }

        bool Near(Color c)
        {
            Color.RGBToHSV(c, out float h, out float sat, out float v);
            if (sat < MinSat || v < 0.3f) return false;
            foreach (var k in new[] { host.coneColour, host.guardColour, host.rivalColour })
            {
                Color.RGBToHSV(k, out float kh, out _, out _);
                float d = Mathf.Abs(h - kh); if (Mathf.Min(d, 1f - d) < HueTol) return true;
            }
            return false;
        }
    }
}
