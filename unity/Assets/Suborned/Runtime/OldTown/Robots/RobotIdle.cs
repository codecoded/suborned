using UnityEngine;

namespace Suborned.OldTown.Robots
{
    /// <summary>
    /// A service robot's idle glances on the kit's movable head (DK_*_Head and the DK_*_Head_Face screen on it, which
    /// the FBX carries as siblings about the robot's origin; near and far versions alike): the
    /// head turns like a servo (a quick move that overshoots ~5% and settles, then holds), and the display's eyes
    /// (RobotFace) jump toward the new heading `lead` seconds before the head moves, re-centring as it arrives, the way
    /// eyes lead a head turn. Alarmed (Alarm) the head snaps onto the suspect in 200 ms, overshooting ~10%, and
    /// tracks them while the face goes red and angry; Calm eases it back to its glances. Idle is a pure function of
    /// time on a looping schedule seeded by where the robot stands, so no two move in step and an offline flyby can
    /// pose it frame by frame (PoseAt).
    /// </summary>
    public sealed class RobotIdle : MonoBehaviour
    {
        [SerializeField] RobotFace face;
        [SerializeField] float range = 45f;                  // widest glance either side of straight ahead, deg
        [SerializeField] float turn = .45f;                  // seconds a glance takes
        [SerializeField] Vector2 hold = new(1.2f, 3.5f);     // seconds a glance holds
        [SerializeField, Range(0f, 1f)] float home = .35f;   // chance a glance comes back to straight ahead
        [SerializeField] float lead = .15f;                  // seconds the eyes move before the head
        [SerializeField] int gaze = 2;                       // cells the eyes jump at a full-range glance

        const int Glances = 24;
        const float Overshoot = 1.1f, Snap = .2f, SnapOvershoot = 1.7f, Settle = .6f, StareMax = 120f;

        Transform[] heads;
        Quaternion[] headRest;
        float[] glanceAt, glanceYaw;
        float glanceLoop, alarmAt = float.PositiveInfinity, calmAt = float.PositiveInfinity;
        Transform suspect;

        public void Configure(RobotFace display, float glanceRange, float turnSeconds, Vector2 holdSeconds, float homeChance)
        {
            face = display; range = glanceRange; turn = turnSeconds; hold = holdSeconds; home = homeChance;
            heads = null;
        }

        void Update() => PoseAt(Time.time);

        /// Turn on `target` from `seconds` on this robot's clock (Time.time in play): head and face both.
        public void Alarm(Transform target, float seconds)
        {
            suspect = target; alarmAt = seconds; calmAt = float.PositiveInfinity;
            if (face) face.Alarm(seconds);
        }

        public void Calm(float seconds)
        {
            calmAt = seconds;
            if (face) face.Calm(seconds);
        }

        /// The idle head yaw at `seconds` (captures pick a moment when it is looking away).
        public float YawAt(float seconds)
        {
            if (heads == null) Build();
            return Yaw(seconds);
        }

        /// Pose at `seconds` on this robot's schedule; t = 0 is the rest pose (facing ahead).
        public void PoseAt(float seconds)
        {
            if (heads == null) Build();
            float yaw = Yaw(seconds), ahead = glanceYaw[Glance(seconds + lead)];
            // a turn to the robot's right (+yaw) moves its eyes toward the viewer's left (-u on the screen)
            int eyes = Mathf.RoundToInt(Mathf.Clamp((yaw - ahead) / range, -1f, 1f) * gaze);
            if (suspect && seconds >= alarmAt)
            {
                float stare = Stare(), u = Mathf.Clamp01((seconds - alarmAt) / Snap) - 1f;
                yaw = seconds < calmAt
                    ? Mathf.LerpUnclamped(Yaw(alarmAt), stare, 1f + (SnapOvershoot + 1f) * u * u * u + SnapOvershoot * u * u)
                    : Mathf.Lerp(stare, yaw, Mathf.SmoothStep(0f, 1f, (seconds - calmAt) / Settle));
                if (seconds < calmAt) eyes = 0;
            }
            var q = Quaternion.AngleAxis(yaw, Vector3.up);
            for (int i = 0; i < heads.Length; i++) heads[i].localRotation = q * headRest[i];
            if (face) face.PoseAt(seconds, eyes);
        }

        /// Head yaw that faces the suspect (heads turn about the robot's own origin), within reach of the neck.
        float Stare()
        {
            var d = transform.InverseTransformPoint(suspect.position);
            return Mathf.Clamp(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -StareMax, StareMax);
        }

        int Glance(float t)
        {
            t = Mathf.Repeat(t, glanceLoop);
            int i = 0;
            while (i < Glances - 1 && glanceAt[i + 1] <= t) i++;
            return i;
        }

        float Yaw(float t)
        {
            int i = Glance(t);
            float from = glanceYaw[(i + Glances - 1) % Glances];
            float u = Mathf.Clamp01((Mathf.Repeat(t, glanceLoop) - glanceAt[i]) / turn) - 1f;
            return Mathf.LerpUnclamped(from, glanceYaw[i], 1f + (Overshoot + 1f) * u * u * u + Overshoot * u * u);  // ease out, back
        }

        void Build()
        {
            var parts = GetComponentsInChildren<Transform>(true);
            heads = System.Array.FindAll(parts, p => p.name.EndsWith("_Head") || p.name.Contains("_Head_"));  // and its face
            headRest = System.Array.ConvertAll(heads, h => h.localRotation);

            var p0 = transform.position;
            var rng = new System.Random(Mathf.RoundToInt(p0.x * 73.1f) ^ Mathf.RoundToInt(p0.z * 19.7f) * 7919);
            float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
            glanceAt = new float[Glances];
            glanceYaw = new float[Glances];  // the first and last glances are home, so t = 0 is the rest pose
            for (int i = 1; i < Glances; i++)
            {
                glanceAt[i] = glanceAt[i - 1] + turn + R(hold.x, hold.y);
                if (i < Glances - 1)
                    glanceYaw[i] = rng.NextDouble() < home ? 0f : R(.35f, 1f) * range * (rng.NextDouble() < .5 ? -1f : 1f);
            }
            glanceLoop = glanceAt[^1] + turn + R(hold.x, hold.y);
        }
    }
}
