#if UNITY_EDITOR
using System.IO;
using System.Linq;
using Suborned.OldTown.Robots;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;
using Object = UnityEngine.Object;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Close-ups of each service robot on the saved street, heads and faces stepped frame by frame (RobotIdle.PoseAt),
    /// one after another into SourceArt/DecoKit/<dkOut>.mp4. Each window opens a few seconds before that robot's first
    /// change of expression, so every robot shows a mood as well as glances and blinks, then ends on the alarm: at
    /// the moment it looks furthest away, it clocks the camera, red-eyed.
    ///   -dkOut robot_reel -dkSecs 12 -dkFps 24 -dkW 960 -dkH 720 -dkSS 2
    ///   -dkStoryboard 0.5   stills every 0.5 s instead, to SourceArt/DecoKit/<dkOut>/<robot>_<t>.png
    internal static class DecoKitRobotReel
    {
        // robot, camera distance in front of the face and rise above it (m): over the porter's trolley and the nanny's
        // pram, and looking down on the waiter so the low sun behind it stays out of frame
        static readonly (string asset, float dist, float rise)[] Cast =
        {
            ("RobotButler", 1.1f, .12f), ("RobotPorter", 1.35f, .3f), ("RobotWaiter", 1.05f, .32f),
            ("RobotNanny", 1.25f, .35f), ("RobotShopkeeper", 1.15f, .12f),
        };

        internal static void Render()
        {
            var cam = DecoKitStreetDemo.OpenSaved();
            cam.enabled = true;
            cam.fieldOfView = 34f;
            float fps = DecoKitStage.Arg("dkFps", 24f), secs = DecoKitStage.Arg("dkSecs", 12f), board = DecoKitStage.Arg("dkStoryboard", 0f);
            int w = (int)DecoKitStage.Arg("dkW", 960f), h = (int)DecoKitStage.Arg("dkH", 720f), ss = (int)DecoKitStage.Arg("dkSS", 2f);
            string name = DecoKitStage.Arg("dkOut", "robot_reel");
            var idles = Object.FindObjectsByType<RobotIdle>(FindObjectsSortMode.None);
            foreach (var r in idles) r.PoseAt(0f);
            Object.DestroyImmediate(DecoKitStage.Render(cam, w, h, ss));  // a freshly opened scene's first frame blows out

            using var ffmpeg = board > 0f ? null : DecoKitFlyby.Encoder(Path.Combine(CaptureDir, name + ".mp4"), w, h, fps);
            int frames = 0;
            foreach (var (asset, dist, rise) in Cast)
            {
                var robot = idles.Where(r => r.name.StartsWith($"DK_{asset}")).OrderBy(r => r.transform.position.z).FirstOrDefault();
                var face = robot ? robot.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name.EndsWith("_Face") && !r.name.Contains("_Far")) : null;
                if (face == null)
                {
                    Debug.LogWarning($"[DecoKit] robot reel: no {asset} with a face on the street");
                    continue;
                }
                var c = face.bounds.center;
                var fwd = Vector3.ProjectOnPlane(robot.transform.forward, Vector3.up).normalized;
                cam.transform.SetPositionAndRotation(c + fwd * dist + Vector3.up * rise, Quaternion.LookRotation(c - Vector3.up * .12f - (c + fwd * dist + Vector3.up * rise)));
                float t0 = Mathf.Max(0f, robot.GetComponent<RobotFace>().FirstMoodAt - secs * .4f);
                float alarm = Enumerable.Range(0, 85).Select(k => t0 + secs * (.62f + .0025f * k))  // after the mood, 2 s+ of stare
                    .OrderByDescending(t => Mathf.Abs(robot.YawAt(t))).First();
                robot.Alarm(cam.transform, alarm);
                float step = board > 0f ? board : 1f / fps;
                for (float t = 0f; t < secs - 1e-4f; t += step)
                {
                    robot.PoseAt(t0 + t);
                    DecoKitStage.WithShadows(25f, () =>
                    {
                        if (ffmpeg == null)
                        {
                            DecoKitStage.Capture(cam, w, h, Path.Combine(CaptureDir, name, $"{asset}_{t:00.00}.png"), ss);
                            return;
                        }
                        var tex = DecoKitStage.Render(cam, w, h, ss);
                        var raw = tex.GetRawTextureData();  // RGB24, bottom row first (ffmpeg flips)
                        ffmpeg.StandardInput.BaseStream.Write(raw, 0, raw.Length);
                        Object.DestroyImmediate(tex);
                    });
                    frames++;
                }
                robot.Calm(float.NegativeInfinity);  // calm since forever: back to plain idle
                robot.PoseAt(0f);
                Debug.Log($"[DecoKit] robot reel: {asset} from t = {t0:0.0} s, alarmed at {alarm - t0:0.00} s");
            }
            if (ffmpeg == null) return;
            ffmpeg.StandardInput.Close();
            ffmpeg.WaitForExit();
            Debug.Log($"[DecoKit] robot reel {name}.mp4: {frames} frames, ffmpeg exit {ffmpeg.ExitCode}");
        }
    }
}
#endif
