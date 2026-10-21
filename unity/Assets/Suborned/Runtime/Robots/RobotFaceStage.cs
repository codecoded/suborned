using System;
using UnityEngine;

namespace Suborned.Robots
{
    /// <summary>
    /// Brief 031 (Vela's robot_face_display, adopted as the PatrolRobot): the patrol robot's attention face, M_RobotFace on its
    /// DK_PatrolRobot_Head_Face screen, drawn by DK_RobotFaceStage.shader from the 16 x 10 stage atlas (robot_face_stage_v1.png, six slices:
    /// calm, noticed, wary, reporting, calm blink half, calm blink shut). Per renderer (a property block): _Stage 0..3, _Gaze -1/0/+1 (the
    /// eyes jump toward the agent, stages 1 and 2) and _Blink 0/1/2 (stage 0 only: the old RobotFace schedule, 40 ms half, 70 ms bar, 40 ms
    /// half, every 2.5 to 5.5 s, seeded by the host key). Reporting flashes in the shader (2 Hz). View only: nothing here is read by the sim.
    /// </summary>
    public sealed class RobotFaceStage : MonoBehaviour
    {
        [SerializeField] string seed = "";
        [SerializeField] Vector2 blinkEvery = new(2.5f, 5.5f);

        const int Blinks = 24; const float Half = .04f, Shut = .07f;
        static readonly int StageId = Shader.PropertyToID("_Stage"), GazeId = Shader.PropertyToID("_Gaze"), BlinkId = Shader.PropertyToID("_Blink");

        [NonSerialized] Renderer[] screens; [NonSerialized] MaterialPropertyBlock block; [NonSerialized] float[] blinkAt; [NonSerialized] float blinkLoop;
        [NonSerialized] int stage, gaze;

        /// <summary>The screens this face drives (renderers whose material is the stage face).</summary>
        public int Screens { get { if (screens == null) Build(); return screens.Length; } }
        public string Seed { get => seed; set { seed = value ?? ""; blinkAt = null; } }

        /// <summary>The watcher's displayed stage (0 calm .. 3 reporting) and the eyes' jump toward the agent (-1, 0, +1).</summary>
        public void Stage(int s, int toward) { stage = Mathf.Clamp(s, 0, 3); gaze = Math.Sign(toward); }
        public int StageNow => stage;

        void Update() => PoseAt(Time.time);

        public void PoseAt(float seconds)
        {
            if (screens == null || blinkAt == null) Build();
            if (screens.Length == 0) return;
            float lid = stage == 0 ? Lid(seconds) : 0f;
            for (int i = 0; i < screens.Length; i++)
            {
                var s = screens[i]; if (!s) continue;
                s.GetPropertyBlock(block);
                block.SetFloat(StageId, stage); block.SetFloat(GazeId, stage is 1 or 2 ? gaze : 0); block.SetFloat(BlinkId, lid);
                s.SetPropertyBlock(block);
            }
        }

        float Lid(float t)
        {
            t = Mathf.Repeat(t, blinkLoop);
            int i = 0;
            while (i < Blinks - 1 && blinkAt[i + 1] <= t) i++;
            float dt = t - blinkAt[i];
            return dt < Half ? 1f : dt < Half + Shut ? 2f : dt < Half + Shut + Half ? 1f : 0f;
        }

        void Build()
        {
            screens = Array.FindAll(GetComponentsInChildren<Renderer>(true), r => r.sharedMaterial && r.sharedMaterial.HasProperty(StageId));
            block ??= new MaterialPropertyBlock();
            var rng = new System.Random(RobotFace.SeedOf(seed, transform.position, 3, 41.3f, 23.9f, 4567));
            float R(Vector2 v) => v.x + (v.y - v.x) * (float)rng.NextDouble();
            blinkAt = new float[Blinks]; blinkAt[0] = -1f;
            for (int i = 1; i < Blinks; i++) blinkAt[i] = blinkAt[i - 1] + R(blinkEvery);
            blinkLoop = blinkAt[^1] + R(blinkEvery);
        }
    }
}
