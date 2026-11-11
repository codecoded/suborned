using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Play
{
    /// <summary>Brief 109: the camera's share of the juice. The tactics camera stays in charge: the host places the Unity camera as before, and at the
    /// very end of the frame this copies that pose and lens into one CinemachineCamera with no position or rotation control ("Do Nothing", no
    /// damping, so the quarter-step turns are never smoothed) and steps the CinemachineBrain by hand (ManualUpdate, unscaled time), only while a shake
    /// is live: the rest of the time the brain isn't stepped and the camera is the host's alone. The only thing
    /// Cinemachine adds is the Impulse listener's shake. The shaken pose lives for the render only: the next frame's first script puts the
    /// camera back where the host left it before anything reads it, so picking, the HUD's anchors and the host's own camera code never see a shake.
    /// Also the takedown's vignette pulse (a URP Volume of its own, weight 0 at rest).</summary>
    [DefaultExecutionOrder(32000)]
    public sealed class Juice109Cam : MonoBehaviour
    {
        // Sky judges these; one place to change them
        public const float ShakeM = 0.2f, ShakeS = 0.26f, VignettePeak = 0.38f, VignetteS = 0.6f;

        CinemachineBrain brain; CinemachineCamera vcam; CinemachineImpulseSource source; Camera cam;
        Vector3 basePos, wrotePos; Quaternion baseRot, wroteRot; bool wrote;
        Volume vol; Vignette vig; float vigT = -1f, liveUntil = -1f; int logged;
        public static int Shakes { get; private set; }

        void Bind(Camera c)
        {
            cam = c; if (!c) return;
            brain = c.GetComponent<CinemachineBrain>(); if (!brain) brain = c.gameObject.AddComponent<CinemachineBrain>();
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate; brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            brain.IgnoreTimeScale = true; brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            if (!vcam)
            {
                var go = new GameObject("Juice109 tactics vcam (Do Nothing)"); go.transform.SetParent(transform, false); // never under the camera (Unity issue: non-deterministic movement)
                vcam = go.AddComponent<CinemachineCamera>();
                var l = go.AddComponent<CinemachineImpulseListener>(); l.ChannelMask = 1; l.Gain = 1f; l.Use2DDistance = true; l.UseCameraSpace = true;
                l.ReactionSettings = new CinemachineImpulseListener.ImpulseReaction { AmplitudeGain = 1f, FrequencyGain = 1f, Duration = 1f };
                var s = new GameObject("Juice109 impulse source"); s.transform.SetParent(transform, false);
                source = s.AddComponent<CinemachineImpulseSource>();
                var d = source.ImpulseDefinition; d.ImpulseChannel = 1; d.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
                d.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump; d.ImpulseDuration = ShakeS;
            }
            Debug.Log($"Juice109Cam: brain on {c.name} (ManualUpdate, cut blends), one Do Nothing camera with an impulse listener");
        }

        /// <summary>First script of the frame: undo last frame's shake if nothing else moved the camera since.</summary>
        [DefaultExecutionOrder(-32000)] sealed class Restore : MonoBehaviour { public Juice109Cam of; void Update() { if (of) of.Unshake(); } }
        void Awake() { gameObject.AddComponent<Restore>().of = this; }

        void Unshake()
        {
            if (!wrote || !cam) return; wrote = false;
            var t = cam.transform; if (t.position == wrotePos && t.rotation == wroteRot) t.SetPositionAndRotation(basePos, baseRot);
        }

        void LateUpdate()
        {
            var h = Juice109Fx.Host; var c = h ? h.View : null;
            if (c != cam) Bind(c);
            if (!cam || !brain || !vcam) return;
            VignetteFrame();
            if (Time.unscaledTime > liveUntil) return; // nothing to add: the brain isn't stepped and the camera is the host's alone (the idle cost is nil)
            var t = cam.transform; basePos = t.position; baseRot = t.rotation;
            vcam.transform.SetPositionAndRotation(basePos, baseRot);
            vcam.Lens = LensSettings.FromCamera(cam);
            brain.ManualUpdate(Time.frameCount, Time.unscaledDeltaTime);
            wrote = t.position != basePos || t.rotation != baseRot; wrotePos = t.position; wroteRot = t.rotation;
            if (wrote && logged < 12) { logged++; Debug.Log($"Juice109Cam: shake {(t.position - basePos).magnitude:F3} m, {Quaternion.Angle(t.rotation, baseRot):F2} deg (frame {Time.frameCount})"); }
            
        }

        /// <summary>A takedown landed: a short bump (direction from the tick, so a replay shakes the same way) and the vignette pulse.</summary>
        public void Takedown(Vector3 at, int tick)
        {
            if (source)
            {
                float a = (tick * 2654435761u % 360u) * Mathf.Deg2Rad;
                source.GenerateImpulseAtPositionWithVelocity(at, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * ShakeM); // camera space (the listener's UseCameraSpace): across the screen
                Shakes++; liveUntil = Time.unscaledTime + ShakeS + 0.15f;
            }
            vigT = 0f;
        }

        void VignetteFrame()
        {
            if (vigT < 0f) return;
            if (!vol)
            {
                var go = new GameObject("Juice109 vignette pulse"); go.transform.SetParent(transform, false);
                vol = go.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 5000f; vol.weight = 0f;
                var p = ScriptableObject.CreateInstance<VolumeProfile>(); p.hideFlags = HideFlags.DontSave;
                vig = p.Add<Vignette>(true); vig.intensity.Override(VignettePeak); vig.smoothness.Override(0.45f); vig.color.Override(new Color(0.043f, 0.086f, 0.082f)); // ink-deep #0b1615
                vol.sharedProfile = p;
            }
            vigT += Time.unscaledDeltaTime; float u = vigT / VignetteS;
            vol.weight = u >= 1f ? 0f : u < 0.18f ? u / 0.18f : 1f - Mathf.SmoothStep(0f, 1f, (u - 0.18f) / 0.82f);
            if (u >= 1f) vigT = -1f;
        }
    }
}
