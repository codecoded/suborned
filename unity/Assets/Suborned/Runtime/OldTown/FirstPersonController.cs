using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.OldTown
{
    /// <summary>
    /// Small Input System first-person controller for the isolated Harbor proof.
    /// The CharacterController owns collision; decorative proof meshes stay non-colliding.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] Transform view;
        [SerializeField] float walkSpeed = 3.6f;
        [SerializeField] float sprintSpeed = 6.2f;
        [SerializeField] float acceleration = 18f;
        [SerializeField] float airControl = 0.35f;
        [SerializeField] float jumpHeight = 1.05f;
        [SerializeField] float gravity = -22f;
        [SerializeField] float lookSensitivity = 0.075f;
        [SerializeField] float maxLookAngle = 86f;
        [SerializeField] bool showControls = true;

        CharacterController _controller;
        Vector3 _spawnPosition;
        Quaternion _spawnRotation;
        Vector3 _planarVelocity;
        float _verticalVelocity;
        float _pitch;
        bool _cursorCaptured;

        public bool CursorCaptured => _cursorCaptured;
        public Vector3 SpawnPosition => _spawnPosition;

        public void Configure(Transform cameraTransform, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            view = cameraTransform;
            _spawnPosition = spawnPosition;
            _spawnRotation = spawnRotation;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            if (view != null)
            {
                var localEuler = view.localEulerAngles;
                _pitch = NormalizeAngle(localEuler.x);
            }
        }

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (view == null)
            {
                var camera = GetComponentInChildren<Camera>(true);
                if (camera != null) view = camera.transform;
            }

            if (_spawnPosition == default)
            {
                _spawnPosition = transform.position;
                _spawnRotation = transform.rotation;
            }
        }

        void OnEnable()
        {
            if (Application.isPlaying)
                SetCursorCaptured(true);
        }

        void OnDisable()
        {
            if (Application.isPlaying)
                SetCursorCaptured(false);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
                SetCursorCaptured(false);
            if (!_cursorCaptured && mouse != null && mouse.leftButton.wasPressedThisFrame)
                SetCursorCaptured(true);
            if (keyboard.hKey.wasPressedThisFrame)
                showControls = !showControls;
            if (keyboard.rKey.wasPressedThisFrame)
                ResetToSpawn();

            if (_cursorCaptured && mouse != null)
                ApplyLook(mouse.delta.ReadValue());

            var input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            var grounded = _controller.isGrounded;
            if (grounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            if (grounded && keyboard.spaceKey.wasPressedThisFrame)
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            var targetSpeed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed
                ? sprintSpeed
                : walkSpeed;
            var targetVelocity = (transform.right * input.x + transform.forward * input.y) * targetSpeed;
            var control = grounded ? 1f : airControl;
            _planarVelocity = Vector3.MoveTowards(
                _planarVelocity,
                targetVelocity,
                acceleration * control * Time.deltaTime);
            _verticalVelocity += gravity * Time.deltaTime;

            var motion = (_planarVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime;
            _controller.Move(motion);

            if (transform.position.y < -5f)
                ResetToSpawn();
        }

        public void ResetToSpawn()
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            _controller.enabled = true;
            Physics.SyncTransforms();
            _planarVelocity = Vector3.zero;
            _verticalVelocity = -2f;
            _pitch = 0f;
            if (view != null)
                view.localRotation = Quaternion.identity;
        }

        void ApplyLook(Vector2 delta)
        {
            transform.Rotate(Vector3.up, delta.x * lookSensitivity, Space.World);
            _pitch = Mathf.Clamp(_pitch - delta.y * lookSensitivity, -maxLookAngle, maxLookAngle);
            if (view != null)
                view.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        void SetCursorCaptured(bool captured)
        {
            _cursorCaptured = captured;
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }

        void OnGUI()
        {
            if (!showControls) return;

            const float width = 430f;
            const float height = 30f;
            var rect = new Rect(16f, Screen.height - height - 14f, width, height);
            var previousColor = GUI.color;
            GUI.color = new Color(0.025f, 0.04f, 0.055f, 0.82f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = new Color(0.87f, 0.92f, 0.94f, 0.92f);
            GUI.Label(
                new Rect(rect.x + 10f, rect.y + 5f, rect.width - 20f, 20f),
                "WASD move   Shift sprint   Space jump   R reset   Esc release   H hide");
            GUI.color = previousColor;
        }

        static float NormalizeAngle(float angle) => angle > 180f ? angle - 360f : angle;
    }
}
