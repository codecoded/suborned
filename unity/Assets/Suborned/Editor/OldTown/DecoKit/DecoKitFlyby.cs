#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.Linq;
using Suborned.OldTown.Robots;
using Suborned.OldTown.Sky;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// A camera flight through the saved street, rendered offline frame by frame (the study grade, haze and lens) with
    /// the aircars, trains, rockets and service robots stepped per frame, piped raw into ffmpeg: no frame files on disk.
    ///   -dkOut flyby -dkFrom 0 -dkTo 60 -dkFps 30 -dkW 1920 -dkH 1080 -dkSS 2
    ///   -dkStoryboard 5   stills every 5 s of dkFrom..dkTo instead (framing check) plus a clearance sweep of the whole path
    internal static class DecoKitFlyby
    {
        const float Clearance = 1.5f;

        // time (s), eye, look-at: boulevard -> circus -> parade -> skyport -> out to sea, looking back at the skyline
        static readonly (float t, Vector3 eye, Vector3 look)[] Keys =
        {
            (0f, new(2f, 6f, -60f), new(0f, 8f, 60f)),
            (8f, new(2f, 9f, 60f), new(0f, 10f, 180f)),
            (16f, new(3f, 12f, 150f), new(0f, 14f, 206f)),
            (21f, new(12f, 15f, 185f), new(5f, 18f, 215f)),     // past the orrery on its east side...
            (25f, new(22f, 17f, 210f), new(60f, 18f, 232f)),    // ...banking right as it slides out of frame
            (29f, new(42f, 20f, 208f), new(170f, 40f, 206f)),   // down the parade to the skyport
            (38f, new(96f, 32f, 204f), new(170f, 55f, 206f)),
            (44f, new(128f, 62f, 160f), new(170f, 60f, 206f)),  // ~60 m off the tower: the discs stay in frame
            (50f, new(190f, 70f, 120f), new(150f, 55f, 206f)),  // round the tower's sea side...
            (56f, new(250f, 60f, 150f), new(60f, 45f, 215f)),   // ...turning back to the city: from above the far
            (62f, new(300f, 55f, 190f), new(0f, 50f, 215f)),    // massing reads as bare boxes, across the water as skyline
        };

        internal static float Duration => Keys[^1].t;
        static RobotIdle[] robots = { };

        internal static void Render()
        {
            var cam = DecoKitStreetDemo.OpenSaved();
            DecoKitHarbour.Ocean();
            cam.enabled = true;
            cam.farClipPlane = 2500f;
            float fps = DecoKitStage.Arg("dkFps", 30f), board = DecoKitStage.Arg("dkStoryboard", 0f);
            float from = DecoKitStage.Arg("dkFrom", 0f), to = Mathf.Min(DecoKitStage.Arg("dkTo", Duration), Duration);
            int w = (int)DecoKitStage.Arg("dkW", 1920f), h = (int)DecoKitStage.Arg("dkH", 1080f), ss = (int)DecoKitStage.Arg("dkSS", 2f);
            string name = DecoKitStage.Arg("dkOut", "flyby");
            var traffic = Object.FindObjectsByType<SkyTraffic>(FindObjectsSortMode.None);
            var rockets = Object.FindObjectsByType<RocketFlight>(FindObjectsSortMode.None);
            robots = Object.FindObjectsByType<RobotIdle>(FindObjectsSortMode.None);
            Object.DestroyImmediate(DecoKitStage.Render(cam, w, h, ss));  // a freshly opened scene's first frame blows out

            if (board > 0f)
            {
                Sweep();
                for (float t = from; t <= to + 1e-3f; t += board)
                {
                    Pose(cam, traffic, rockets, t);
                    DecoKitStage.WithShadows(Shadow(cam), () =>
                        DecoKitStage.Capture(cam, w, h, Path.Combine(CaptureDir, $"{name}_t{t:00}.png"), ss));
                }
                Debug.Log($"[DecoKit] flyby storyboard: every {board} s of {Duration} s");
                return;
            }

            var file = Path.Combine(CaptureDir, name + ".mp4");
            using var ffmpeg = Encoder(file, w, h, fps);
            var stdin = ffmpeg.StandardInput.BaseStream;
            var clock = Stopwatch.StartNew();
            int frames = Mathf.RoundToInt((to - from) * fps);
            for (int f = 0; f < frames; f++)
            {
                Pose(cam, traffic, rockets, from + f / fps);
                DecoKitStage.WithShadows(Shadow(cam), () =>
                {
                    var tex = DecoKitStage.Render(cam, w, h, ss);
                    var raw = tex.GetRawTextureData();  // RGB24, bottom row first (ffmpeg flips)
                    stdin.Write(raw, 0, raw.Length);
                    Object.DestroyImmediate(tex);
                });
                if (f % 150 == 149) Debug.Log($"[DecoKit] flyby {f + 1}/{frames}, {clock.Elapsed.TotalSeconds / (f + 1):0.00} s a frame");
            }
            stdin.Close();
            ffmpeg.WaitForExit();
            Debug.Log($"[DecoKit] flyby {file}: {frames} frames in {clock.Elapsed.TotalMinutes:0.0} min, ffmpeg exit {ffmpeg.ExitCode}");
        }

        static void Pose(Camera cam, SkyTraffic[] traffic, RocketFlight[] rockets, float t)
        {
            // the view direction is splined, not the look-at point: a point swept past the camera whips the view round
            var eye = Spline(t, k => Keys[k].eye);
            cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(Spline(t, k => (Keys[k].look - Keys[k].eye).normalized), Vector3.up));
            cam.fieldOfView = 58f;
            foreach (var v in traffic) v.PoseAt(t);
            foreach (var r in rockets) r.PoseAt(t);
            foreach (var r in robots) r.PoseAt(t);
        }

        // crisp near the street, the whole skyline once the camera climbs
        static float Shadow(Camera cam) => Mathf.Clamp(cam.transform.position.y * 8f, 120f, 1200f);

        /// Hermite through the keys with finite-difference velocities (non-uniform Catmull-Rom); still at both ends.
        static Vector3 Spline(float t, System.Func<int, Vector3> p)
        {
            int last = Keys.Length - 1, i = 0;
            while (i < last - 1 && t >= Keys[i + 1].t) i++;
            float t0 = Keys[i].t, t1 = Keys[i + 1].t, dt = t1 - t0, u = Mathf.Clamp01((t - t0) / dt);
            Vector3 V(int k) => k == 0 || k == last ? Vector3.zero : (p(k + 1) - p(k - 1)) / (Keys[k + 1].t - Keys[k - 1].t);
            float u2 = u * u, u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * p(i) + (u3 - 2f * u2 + u) * dt * V(i)
                 + (-2f * u3 + 3f * u2) * p(i + 1) + (u3 - u2) * dt * V(i + 1);
        }

        /// Every 0.1 s of the path: nothing within Clearance of the eye (logs what it would clip).
        static void Sweep()
        {
            Physics.SyncTransforms();
            var hits = Enumerable.Range(0, Mathf.CeilToInt(Duration * 10f) + 1).Select(k => k * .1f)
                .SelectMany(t => Physics.OverlapSphere(Spline(t, k => Keys[k].eye), Clearance).Select(c => (t, c)))
                .GroupBy(x => x.c.name).Select(g => $"{g.Key} at {g.First().t:0.0}-{g.Last().t:0.0} s").ToArray();
            Debug.Log(hits.Length == 0 ? "[DecoKit] flyby path clear" : "[DecoKit] flyby path clips: " + string.Join("; ", hits));
        }

        internal static Process Encoder(string file, int w, int h, float fps)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? ".");  // Scratch/Captures/DecoKit may not exist yet
            var info = new ProcessStartInfo("/opt/homebrew/bin/ffmpeg",
                $"-y -loglevel error -f rawvideo -pix_fmt rgb24 -s {w}x{h} -r {fps} -i - -vf vflip " +
                $"-c:v libx264 -preset medium -crf 17 -pix_fmt yuv420p -movflags +faststart \"{file}\"")
            {
                UseShellExecute = false, RedirectStandardInput = true, CreateNoWindow = true,
            };
            return Process.Start(info);
        }
    }
}
#endif
