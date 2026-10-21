using System;
using UnityEngine;

namespace Suborned.Robots
{
    /// <summary>
    /// A service robot's idle glances on the kit's movable head (DK_*_Head and the DK_*_Head_Face screen on it, siblings about the
    /// robot's origin): the head turns like a servo (a quick move that overshoots and settles, then holds), and the display's eyes
    /// (RobotFace) jump toward the new heading `lead` seconds before the head moves. Alarmed (Alarm) the head snaps onto the suspect in
    /// 200 ms, overshooting, and tracks them while the face goes red and angry; Calm eases it back to its glances.
    ///
    /// Ported from the old project's Scripts/Robots/RobotIdle.cs (brief 026; same script GUID). View only: a pure function of time on a
    /// looping schedule seeded by the face's id (RobotFace.Seed; else position), so no two move in step, a capture can pose one frame by
    /// frame (PoseAt), and nothing reaches the sim. Personality is data (robots.json, RobotRigs).
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

        // runtime caches: never serialized (a domain reload would bring them back as empty arrays, not null)
        [NonSerialized] Transform[] heads;
        [NonSerialized] Quaternion[] headRest;
        [NonSerialized] float[] glanceAt, glanceYaw;
        [NonSerialized] float glanceLoop, alarmAt = float.PositiveInfinity, calmAt = float.PositiveInfinity;
        [NonSerialized] Transform suspect;
        [NonSerialized] Transform attend; [NonSerialized] float attendAt = float.PositiveInfinity, attendEnd = float.PositiveInfinity;

        public RobotFace Face => face;
        public int Heads { get { if (heads == null) Build(); return heads.Length; } }

        public void Configure(RobotFace display, float glanceRange, float turnSeconds, Vector2 holdSeconds, float homeChance, float leadSeconds, int gazeCells)
        {
            face = display; range = glanceRange; turn = turnSeconds; hold = holdSeconds; home = homeChance; lead = leadSeconds; gaze = gazeCells;
            heads = null;
        }

        void Update() => PoseAt(Time.time);

        /// <summary>Turn on `target` from `seconds` on this robot's clock (Time.time in play): head and face both. A null target keeps
        /// the head on its glances; the face still turns red.</summary>
        public void Alarm(Transform target, float seconds)
        {
            suspect = target; alarmAt = seconds; calmAt = float.PositiveInfinity;
            if (face) face.Alarm(seconds);
        }

        public void Track(Transform target) => suspect = target;

        /// <summary>Brief 031 (a watcher's attention, wary): the head snaps onto `target` and tracks it, the face untouched (its stage is
        /// RobotFace.Stage); null lets it go back to its glances. An alarm (Alarm) outranks it.</summary>
        public void Attend(Transform target, float seconds)
        {
            if (target == attend && (target == null || float.IsPositiveInfinity(attendEnd))) return;
            if (target != null) { attend = target; attendAt = seconds; attendEnd = float.PositiveInfinity; }
            else if (attend != null && float.IsPositiveInfinity(attendEnd)) attendEnd = seconds;
        }

        public void Calm(float seconds)
        {
            calmAt = seconds;
            if (face) face.Calm(seconds);
        }

        /// <summary>The idle head yaw at `seconds` (captures pick a moment when it is looking away).</summary>
        public float YawAt(float seconds)
        {
            if (heads == null || glanceAt == null || glanceAt.Length == 0) Build();
            return Yaw(seconds);
        }

        /// <summary>Pose at `seconds` on this robot's schedule; t = 0 is the rest pose (facing ahead).</summary>
        public void PoseAt(float seconds)
        {
            if (heads == null || glanceAt == null || glanceAt.Length == 0) Build();
            float yaw = Yaw(seconds), ahead = glanceYaw[Glance(seconds + lead)];
            // a turn to the robot's right (+yaw) moves its eyes toward the viewer's left (-u on the screen)
            int eyes = Mathf.RoundToInt(Mathf.Clamp((yaw - ahead) / range, -1f, 1f) * gaze);
            if (seconds >= alarmAt && (suspect || seconds >= calmAt))
            {
                float stare = suspect ? Stare(suspect) : Yaw(alarmAt), u = Mathf.Clamp01((seconds - alarmAt) / Snap) - 1f;
                yaw = seconds < calmAt
                    ? Mathf.LerpUnclamped(Yaw(alarmAt), stare, 1f + (SnapOvershoot + 1f) * u * u * u + SnapOvershoot * u * u)
                    : Mathf.Lerp(stare, yaw, Mathf.SmoothStep(0f, 1f, (seconds - calmAt) / Settle));
                if (seconds < calmAt) eyes = 0;
            }
            else if (seconds >= attendAt && (attend || seconds < attendEnd + Settle))
            {   // brief 031: wary, the head on the agent (the same snap as the alarm's), then back to the glances once let go
                float stare = attend ? Stare(attend) : Yaw(attendAt), u = Mathf.Clamp01((seconds - attendAt) / Snap) - 1f;
                yaw = attend && seconds < attendEnd
                    ? Mathf.LerpUnclamped(Yaw(attendAt), stare, 1f + (Overshoot + 1f) * u * u * u + Overshoot * u * u)
                    : Mathf.Lerp(stare, yaw, Mathf.SmoothStep(0f, 1f, (seconds - attendEnd) / Settle));
                if (seconds >= attendEnd + Settle) { attend = null; attendAt = float.PositiveInfinity; }
            }
            var q = Quaternion.AngleAxis(yaw, Vector3.up);
            for (int i = 0; i < heads.Length; i++) if (heads[i]) heads[i].localRotation = q * headRest[i];
            if (face) face.PoseAt(seconds, eyes);
        }

        /// <summary>Head yaw that faces the suspect (heads turn about the robot's own origin), within reach of the neck.</summary>
        float Stare(Transform who)
        {
            var d = transform.InverseTransformPoint(who.position);
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
            // the head and its face screen; never a LOD child of another piece, and never the `__LOD` holders themselves
            heads = System.Array.FindAll(parts, p => p != transform && (p.name.EndsWith("_Head") || p.name.Contains("_Head_")));
            headRest = System.Array.ConvertAll(heads, h => h.localRotation);

            var rng = new System.Random(RobotFace.SeedOf(face ? face.Seed : "", transform.position, 2, 73.1f, 19.7f, 7919));
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

        /// <summary>Re-reads the schedule (after the face's seed is set).</summary>
        public void Reseed() => heads = null;
    }
}
