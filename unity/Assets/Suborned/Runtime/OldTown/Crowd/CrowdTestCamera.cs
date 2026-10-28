using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.OldTown.Crowd
{
    /// <summary>The crowd test's view: the game's 2.5D camera (orthographic, 38 deg pitch, 45 deg yaw, ortho size 8
    /// close / 14 play / 48 tactical, DecoKitIso) with WASD pan, mouse wheel or +/- zoom (1/2/3/4 jump to closest 4 / close 8 / play 14 / tactical 48), Q/E turning in 90 deg steps
    /// and Shift for speed. The camera sits Back m up its ray with a negative near plane (towers nearer the lens still
    /// draw, DKCutaway thins them), so the URP asset's shadow distance is stretched to reach the frame while it runs; fog thins to DecoKitIso's .35.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CrowdTestCamera : MonoBehaviour
    {
        public const float Back = 150f, Pitch = 38f;
        public const float Fog = .35f;  // DecoKitIso: the lens looks down through less air than the street eye

        [SerializeField] Vector3 focus = new(0f, 0f, -125f);
        [SerializeField] float size = 14f, yaw = 45f;
        [SerializeField] Vector2 zoomRange = new(2f, 60f);
        [SerializeField] float panPerSize = 1.4f;  // m/s per metre of ortho size

        Camera view;
        float yawTarget, shadowDistance = -1f, fogDensity = -1f;

        public float Size => size;
        public float Yaw => yaw;
        public Vector3 Focus => focus;

        void OnEnable()
        {
            yawTarget = yaw;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp) shadowDistance = urp.shadowDistance;
            fogDensity = RenderSettings.fogDensity;
            RenderSettings.fogDensity = fogDensity * Fog;
            Apply();
        }

        void OnDisable()
        {
            if (fogDensity >= 0f) RenderSettings.fogDensity = fogDensity;
            if (shadowDistance > 0f && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
                urp.shadowDistance = shadowDistance;
        }

        void Update()
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            float dt = Time.unscaledDeltaTime;
            if (k != null)
            {
                var input = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
                var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                var fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                float speed = size * panPerSize * (k.leftShiftKey.isPressed ? 3f : 1f);
                focus += (right * input.x + fwd * input.y) * (speed * dt);
                if (k.qKey.wasPressedThisFrame) yawTarget -= 90f;
                if (k.eKey.wasPressedThisFrame) yawTarget += 90f;
                if (k.equalsKey.isPressed || k.numpadPlusKey.isPressed) size *= 1f - dt;
                if (k.minusKey.isPressed || k.numpadMinusKey.isPressed) size *= 1f + dt;
                if (k.digit1Key.wasPressedThisFrame) size = 4f;   // closest: a figure ~240 px tall at 1080p
                if (k.digit2Key.wasPressedThisFrame) size = 8f;   // close
                if (k.digit3Key.wasPressedThisFrame) size = 14f;  // play
                if (k.digit4Key.wasPressedThisFrame) size = 48f;  // tactical
            }
            if (m != null) size *= Mathf.Pow(.9f, m.scroll.ReadValue().y);  // a wheel notch is 1, a trackpad a fraction
            size = Mathf.Clamp(size, zoomRange.x, zoomRange.y);
            yaw = Mathf.MoveTowardsAngle(yaw, yawTarget, 360f * dt);
            Apply();
        }

        public void Set(Vector3 at, float orthoSize, float heading)
        {
            focus = at;
            size = orthoSize;
            yaw = yawTarget = heading;
            Apply();
        }

        void Apply()
        {
            if (view == null)
            {
                view = GetComponent<Camera>();
                view.orthographic = true;
                view.nearClipPlane = -Back * 2f;  // towers nearer the lens than Back still draw
                view.farClipPlane = Back + 700f;
            }
            var rot = Quaternion.Euler(Pitch, yaw, 0f);
            transform.SetPositionAndRotation(focus - rot * Vector3.forward * Back, rot);
            view.orthographicSize = size;
            // shadows must reach from the lens (Back) through the frame's depth (DecoKitIso's captures); play mode only,
            // OnDisable puts the shared asset back
            if (Application.isPlaying && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
                urp.shadowDistance = Back + size * 4f;
        }
    }
}
