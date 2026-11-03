using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Suborned.World.Review
{
    /// <summary>Brief 075 golden view (Latch, 2026-10-28): one fixed camera on the boulevard, the frame Sky compared against the old reel
    /// (Flyby074's flight through briefs/074/flyby/new-keys.json at t = 6.0 s), rendered offscreen in batchmode on the built HarbourDistrict
    /// scene with the runtime lens and haze installed as Flyby074 `-lens on` does. Camera from briefs/075/golden/cameras.json ("ours"):
    /// eye, look, fov 58, 1920 x 1080 at supersample 2 (rendered 3840 x 2160, lanczos down in ffmpeg as the reels were), shadow distance
    /// clamp(eye height x 8, 120, 1200). Nothing is saved; the scene is never written.
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.Golden075.Cli -tag &lt;step&gt; [-out dir] [-life t [-lifeFile json]] -logFile &lt;log&gt;
    /// -life t: Pulse's presentation life (traffic, trams, people; out/world/harbour-district.presentation.json) posed at presentation time t (LifeInstall).
    /// Writes &lt;out&gt;/&lt;step&gt;.png and &lt;out&gt;/&lt;step&gt;.json (camera, export stamp). Normally run through briefs/075/golden/golden.ts.</summary>
    public static class Golden075
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        static string Arg(string name, string fallback)
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : fallback;
        }
        static string Abs(string p) => Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(HarbourWorldBuilder.RepoRoot, p));
        static Vector3 V(object o) { var l = (System.Collections.Generic.List<object>)o; return new Vector3((float)(double)l[0], (float)(double)l[1], (float)(double)l[2]); }
        static string F(float v) => v.ToString("0.####", CultureInfo.InvariantCulture);

        public static void Cli() => HarbourWorldBuilder.Batch(() =>
        {
            var tag = Arg("-tag", null) ?? throw new ArgumentException("golden075: -tag <step> required");
            foreach (var c in tag) if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')) throw new ArgumentException($"golden075: tag '{tag}' must be letters, digits, - _ .");
            var outDir = Abs(Arg("-out", "briefs/075/golden"));
            var cams = Json.Parse(File.ReadAllText(Abs(Arg("-cameras", "briefs/075/golden/cameras.json"))));
            var ours = (System.Collections.Generic.Dictionary<string, object>)J.Get(cams, "ours");
            Vector3 eye = V(J.Get(ours, "eye")), look = V(J.Get(ours, "look"));
            float fov = J.F(ours, "fov_deg");
            int W = (int)J.F(ours, "width"), H = (int)J.F(ours, "height"), ss = (int)J.F(ours, "supersample");

            if (!RenderSession.EnsureScene()) throw new InvalidOperationException("golden075: no scene");
            var stamp = UnityEngine.Object.FindAnyObjectByType<WorldStamp>();
            string export = stamp ? stamp.exportPath : "", sha = stamp ? stamp.exportSha256 : "";
            Debug.Log($"golden075: scene {HarbourWorldBuilder.ScenePath}, export {(stamp ? export + " sha256 " + sha : "no stamp")}");

            // the runtime look, as Flyby074 -lens on: LightPhase's day keys, the lens tiers (lens.json) and the long haze (haze.json)
            var phase = UnityEngine.Object.FindAnyObjectByType<Suborned.Play.LightPhase>(FindObjectsInactive.Include);
            if (phase) typeof(Suborned.Play.LightPhase).GetMethod("OnEnable", Any)?.Invoke(phase, null);
            AtmosphereLens lens = Glint075.InstallLens(null); DistanceHaze haze = Glint075.InstallHaze();
            // brief 075 golden s3: `-life t` draws Pulse's presentation life at presentation time t (LifeInstall; never in play)
            string lifeArg = Arg("-life", null); float lifeT = float.NaN; PresentationLife life = null;
            if (lifeArg != null)
            {
                lifeT = float.Parse(lifeArg, CultureInfo.InvariantCulture);
                life = LifeInstall.Install(Arg("-lifeFile", null) is string lf ? Abs(lf) : null);
            }
            var tmp = Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "scratch", "latch", "golden075", $"{tag}-ss{ss}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(tmp)); Directory.CreateDirectory(outDir);
            var png = Path.Combine(outDir, tag + ".png");
            using (var session = new RenderSession())
            {
                var cam = RenderSession.TempCamera("golden075");
                cam.fieldOfView = fov;
                try
                {
                    new Pose { Position = eye, Rotation = Quaternion.LookRotation((look - eye).normalized, Vector3.up), Fov = fov, Near = 0.3f, Far = 12000f }.Apply(cam);
                    if (life) { life.View = cam; life.Step(lifeT); Debug.Log($"golden075: life at t {lifeT:0.##} (tau {lifeT % life.Period:0.##}): {life.Shown} shown of {life.Count}"); }
                    session.ShadowDistance = Mathf.Clamp(eye.y * 8f, 120f, 1200f);
                    session.Grab(cam, 64, 64); session.Grab(cam, 64, 64); // the first render after a load is flat; the lens forces post on from the next frame
                    File.WriteAllBytes(tmp, session.Grab(cam, W * ss, H * ss).EncodeToPNG());
                    Debug.Log($"golden075: eye {eye} look {look} fov {fov} tier {DistanceHaze.TierOf(cam)} fog {RenderSettings.fogMode} {RenderSettings.fogDensity}");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(cam.gameObject);
                    if (life) UnityEngine.Object.DestroyImmediate(life.gameObject); // the dressing trams shown again
                    if (lens) { typeof(AtmosphereLens).GetMethod("OnDisable", Any)?.Invoke(lens, null); UnityEngine.Object.DestroyImmediate(lens.gameObject); }
                    if (haze) { typeof(DistanceHaze).GetMethod("OnDisable", Any)?.Invoke(haze, null); UnityEngine.Object.DestroyImmediate(haze.gameObject); }
                }
            }
            if (ss > 1)
            {
                var psi = new ProcessStartInfo("/opt/homebrew/bin/ffmpeg", $"-hide_banner -loglevel error -y -i \"{tmp}\" -vf scale={W}:{H}:flags=lanczos \"{png}\"")
                { UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true };
                using var ff = Process.Start(psi);
                var err = ff.StandardError.ReadToEnd(); ff.WaitForExit();
                if (ff.ExitCode != 0) throw new IOException($"golden075: ffmpeg exit {ff.ExitCode}: {err}");
                File.Delete(tmp);
            }
            else { File.Copy(tmp, png, true); File.Delete(tmp); }
            File.WriteAllText(Path.Combine(outDir, tag + ".json"),
                "{\n  \"schema\": \"latch.golden075-frame/1\",\n" +
                $"  \"step\": \"{tag}\",\n  \"rendered\": \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\",\n" +
                $"  \"scene\": \"{HarbourWorldBuilder.ScenePath}\",\n  \"export\": \"{export.Replace("\\", "/")}\",\n  \"export_sha256\": \"{sha}\",\n" +
                $"  \"eye\": [{F(eye.x)}, {F(eye.y)}, {F(eye.z)}],\n  \"look\": [{F(look.x)}, {F(look.y)}, {F(look.z)}],\n" +
                $"  \"fov_deg\": {F(fov)},\n  \"size\": [{W}, {H}],\n  \"supersample\": {ss},\n  \"lens\": \"runtime lens + haze (Flyby074 -lens on)\",\n" +
                $"  \"life_t\": {(float.IsNaN(lifeT) ? "null" : F(lifeT))}\n}}\n");
            Debug.Log($"golden075: {png}");
        });
    }
}
