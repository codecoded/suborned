using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Suborned.World.Review
{
    /// <summary>Brief 075 remap (Latch, 2026-10-28): a top-down orthographic overhead of the whole district from the built HarbourDistrict
    /// scene, offscreen in batchmode, framed on the plan sheet's extent (plan.sheet x [-460, 380], y [-560, 300], Datum's SBN-PLN-001 and
    /// briefs/075/remap/sheet.html's viewBox) so it lays over the sheet: north (plan +y) up, east (+x) right, -ppm pixels a metre (default 2).
    /// The day look (LightPhase's day keys), no lens or haze (a map, not a view), fog off for the render, shadows to 1200 m. Nothing is saved.
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.Overhead075.Cli -out &lt;png&gt;
    ///         [-x0 -460 -x1 380 -y0 -560 -y1 300] [-ppm 2] [-ss 2] -logFile &lt;log&gt;</summary>
    public static class Overhead075
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        static string Arg(string name, string fallback)
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : fallback;
        }
        static float F(string name, float fallback) => float.TryParse(Arg(name, null), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;
        static string Abs(string p) => Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(HarbourWorldBuilder.RepoRoot, p));

        public static void Cli() => HarbourWorldBuilder.Batch(() =>
        {
            var png = Abs(Arg("-out", "out/scratch/latch/075remap/overhead.png"));
            float x0 = F("-x0", -460f), x1 = F("-x1", 380f), y0 = F("-y0", -560f), y1 = F("-y1", 300f), ppm = F("-ppm", 2f);
            int ss = Mathf.Max(1, (int)F("-ss", 2f));
            int W = Mathf.RoundToInt((x1 - x0) * ppm), H = Mathf.RoundToInt((y1 - y0) * ppm);
            if (!RenderSession.EnsureScene()) throw new InvalidOperationException("overhead075: no scene");
            var stamp = UnityEngine.Object.FindAnyObjectByType<WorldStamp>();
            Debug.Log($"overhead075: scene {HarbourWorldBuilder.ScenePath}, export {(stamp ? stamp.exportPath + " sha256 " + stamp.exportSha256 : "no stamp")}");
            var phase = UnityEngine.Object.FindAnyObjectByType<Suborned.Play.LightPhase>(FindObjectsInactive.Include);
            if (phase) typeof(Suborned.Play.LightPhase).GetMethod("OnEnable", Any)?.Invoke(phase, null);
            foreach (var oc in Camera.allCameras) if (oc.targetTexture == null) oc.enabled = false;
            bool fog0 = RenderSettings.fog; RenderSettings.fog = false;
            var tmp = Path.ChangeExtension(png, null) + $"-ss{ss}.png";
            Directory.CreateDirectory(Path.GetDirectoryName(png));
            using (var session = new RenderSession())
            {
                var cam = RenderSession.TempCamera("overhead075");
                try
                {
                    cam.orthographic = true; cam.orthographicSize = (y1 - y0) * 0.5f;
                    cam.transform.SetPositionAndRotation(new Vector3((x0 + x1) * 0.5f, 800f, (y0 + y1) * 0.5f), Quaternion.Euler(90f, 0f, 0f)); // looking down, +z (plan north) up
                    cam.nearClipPlane = 1f; cam.farClipPlane = 1600f;
                    RenderSettings.fog = false;
                    session.ShadowDistance = 1200f;
                    session.Grab(cam, 64, 64); session.Grab(cam, 64, 64); // the first render after a load comes out flat
                    File.WriteAllBytes(tmp, session.Grab(cam, W * ss, H * ss).EncodeToPNG());
                }
                finally { UnityEngine.Object.DestroyImmediate(cam.gameObject); RenderSettings.fog = fog0; }
            }
            if (ss > 1)
            {
                var psi = new ProcessStartInfo("/opt/homebrew/bin/ffmpeg", $"-hide_banner -loglevel error -y -i \"{tmp}\" -vf scale={W}:{H}:flags=lanczos \"{png}\"")
                { UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true };
                using var ff = Process.Start(psi);
                var err = ff.StandardError.ReadToEnd(); ff.WaitForExit();
                if (ff.ExitCode != 0) throw new IOException($"overhead075: ffmpeg exit {ff.ExitCode}: {err}");
                File.Delete(tmp);
            }
            else { File.Copy(tmp, png, true); File.Delete(tmp); }
            File.WriteAllText(Path.ChangeExtension(png, ".json"),
                "{\n  \"schema\": \"latch.overhead075/1\",\n" +
                $"  \"rendered\": \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\",\n  \"export\": \"{(stamp ? stamp.exportPath.Replace("\\", "/") : "")}\",\n  \"export_sha256\": \"{(stamp ? stamp.exportSha256 : "")}\",\n" +
                $"  \"plan_extent\": {{ \"x\": [{x0.ToString(CultureInfo.InvariantCulture)}, {x1.ToString(CultureInfo.InvariantCulture)}], \"y\": [{y0.ToString(CultureInfo.InvariantCulture)}, {y1.ToString(CultureInfo.InvariantCulture)}] }},\n" +
                $"  \"px_per_m\": {ppm.ToString(CultureInfo.InvariantCulture)},\n  \"size\": [{W}, {H}],\n  \"supersample\": {ss},\n" +
                "  \"projection\": \"orthographic, straight down; image up = plan +y (north), right = plan +x; pixel (0, 0) = plan (x0, y1)\"\n}\n");
            Debug.Log($"overhead075: {png} {W}x{H} ({ppm} px/m, ss {ss})");
        });
    }
}
