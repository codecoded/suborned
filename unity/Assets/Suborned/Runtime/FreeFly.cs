using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR || SUBORNED_FREEFLY
namespace Suborned
{
    /// <summary>Review tool, not a game system: in Play mode on the generated HarbourDistrict scene, a free-fly camera is ready at
    /// the play camera, OFF (opt-in, Redline 007 C4: it used to draw a second full-city view over the play camera by default).
    /// Tab turns it on, which turns the play camera off, so only one camera renders; Tab again hands back. Hold right mouse to
    /// look; WASD move, Q/E down/up, Shift x5, scroll changes the base speed. Nothing is added to the scene file. Editor only,
    /// or a player built with the SUBORNED_FREEFLY define (never in profiling builds by default).</summary>
    public sealed class FreeFly : MonoBehaviour
    {
        float yaw, pitch, speed = 20f;
        Camera cam, main;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            if (SceneManager.GetActiveScene().name != "HarbourDistrict" || FindAnyObjectByType<FreeFly>()) return;
            var main = Camera.main;
            var go = new GameObject("free-fly");
            var cam = go.AddComponent<Camera>();
            if (main)
            {
                cam.clearFlags = main.clearFlags; cam.backgroundColor = main.backgroundColor;
                // an orthographic play camera stands far back: start nearer the ground, looking the same way
                var fwd = main.transform.forward;
                var start = main.orthographic ? main.transform.position + fwd * (main.transform.position.y / Mathf.Max(0.2f, -fwd.y) - main.orthographicSize * 4f) : main.transform.position;
                go.transform.SetPositionAndRotation(start, main.transform.rotation);
            }
            cam.fieldOfView = 60f; cam.nearClipPlane = 0.2f; cam.farClipPlane = 5000f; cam.depth = 50;
            cam.enabled = false;
            go.AddComponent<FreeFly>().main = main;
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            var e = transform.eulerAngles; yaw = e.y; pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        void Update()
        {
            var kb = Keyboard.current; var mouse = Mouse.current;
            if (kb == null) return;
            if (kb.tabKey.wasPressedThisFrame)
            {
                cam.enabled = !cam.enabled;
                if (main) main.enabled = !cam.enabled;
            }
            if (!cam.enabled) return;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    var d = mouse.delta.ReadValue() * 0.15f;
                    yaw += d.x; pitch = Mathf.Clamp(pitch - d.y, -89f, 89f);
                    transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0f) speed = Mathf.Clamp(speed * (scroll > 0 ? 1.2f : 1f / 1.2f), 1f, 500f);
            }
            var move = Vector3.zero;
            if (kb.wKey.isPressed) move += Vector3.forward;
            if (kb.sKey.isPressed) move += Vector3.back;
            if (kb.dKey.isPressed) move += Vector3.right;
            if (kb.aKey.isPressed) move += Vector3.left;
            if (kb.eKey.isPressed) move += Vector3.up;
            if (kb.qKey.isPressed) move += Vector3.down;
            float s = speed * (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed ? 5f : 1f);
            var world = transform.TransformDirection(new Vector3(move.x, 0f, move.z)) + Vector3.up * move.y;
            transform.position += world * (s * Time.unscaledDeltaTime);
        }
    }
}
#endif
