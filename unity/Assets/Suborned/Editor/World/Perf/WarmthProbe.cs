using System.IO;
using System.Linq;
using Suborned.Perf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Perf
{
    /// <summary>Brief 008 B6, Glint's warmth question and ruling (briefs/008/perf/pass2/warmth/glint-ruling.md): the play camera's home
    /// frame rendered in the Editor at 1920x1080 twice, the GPU Resident Drawer off for both, into
    /// briefs/008/perf/pass2/warmth/honest/: editor-new-home.{png,txt} through Capture (the honest path every Editor preview now
    /// takes: an ARGBHalf target with the asset's MSAA, then the sRGB encode), and editor-old-home.{png,txt} the way every Editor
    /// shot was made before the ruling (Camera.Render into ARGB32 with 4x MSAA: an 8-bit intermediate that clamps scene colour at
    /// 1.0 before the grade), kept here only so the pair can be compared. Each .txt carries the grade, ambient and sun it resolved
    /// and the intermediate format (PerfLook). The player side comes from the perf harness's `look` pass. Reads only: the scene is
    /// not changed or saved.</summary>
    public static class WarmthProbe
    {
        const int W = 1920, H = 1080;

        [MenuItem("Suborned/Perf/Warmth Probe (Editor home frame, old and honest)")]
        public static void Run()
        {
            var dir = Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "008", "perf", "pass2", "warmth", "honest");
            Directory.CreateDirectory(dir);
            var cam = GameObject.Find("cameras/play").GetComponent<Camera>();
            var urp = UniversalRenderPipeline.asset;
            var drawer = urp.gpuResidentDrawerMode;
            var people = GameObject.Find("people"); // the perf player strips the people layer (PerfStrip): the Editor frame must match it
            var hidden = people ? people.GetComponentsInChildren<Renderer>().Where(r => !r.forceRenderingOff).ToArray() : new Renderer[0];
            try
            {
                urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
                foreach (var r in hidden) r.forceRenderingOff = true; // not serialised: the scene stays clean
                var ts = cam.GetComponent<TacticalShadows>();
                using (ts ? ts.Use() : null)
                using (var capture = new Capture())
                {
                    capture.Grab(cam, 64, 64); // the first render after a change comes out flat
                    File.WriteAllBytes(Path.Combine(dir, "editor-new-home.png"), capture.Grab(cam, W, H).EncodeToPNG());
                    Write(dir, "editor-new-home.txt", $"Editor render of cameras/play at its built focus (home), {W}x{H}, {Capture.Describe}, GPU Resident Drawer off for the render{(people ? ", people layer hidden (the perf player strips it)" : "")}", PerfLook.Read(cam, capture.Target(W, H)));
                    var (png, rt) = Old(cam);
                    File.WriteAllBytes(Path.Combine(dir, "editor-old-home.png"), png);
                    Write(dir, "editor-old-home.txt", $"Editor render of cameras/play at its built focus (home), {W}x{H}, the path before Glint's ruling: Camera.Render into RenderTexture ARGB32 (sRGB in a linear project), MSAA 4, ReadPixels; GPU Resident Drawer off for the render{(people ? ", people layer hidden" : "")}", PerfLook.Read(cam, rt));
                    rt.Release(); Object.DestroyImmediate(rt);
                }
                Debug.Log($"Perf: warmth probe (old and honest Editor frames) written to {dir}");
            }
            finally
            {
                urp.gpuResidentDrawerMode = drawer;
                foreach (var r in hidden) if (r) r.forceRenderingOff = false;
            }
        }

        static void Write(string dir, string file, string head, string[] lines) => File.WriteAllLines(Path.Combine(dir, file), lines.Prepend(head));

        /// <summary>The pre-ruling Editor path, for the comparison only: an 8-bit target, so an 8-bit intermediate. Returns the PNG and
        /// the (still allocated) target, for PerfLook to describe.</summary>
        static (byte[] png, RenderTexture rt) Old(Camera cam)
        {
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var previous = RenderTexture.active;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            try
            {
                cam.targetTexture = rt;
                cam.ResetAspect();
                cam.Render(); cam.Render(); // the first render into a new target comes out flat
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                return (tex.EncodeToPNG(), rt);
            }
            finally
            {
                cam.targetTexture = null; cam.ResetAspect();
                RenderTexture.active = previous;
                Object.DestroyImmediate(tex);
            }
        }
    }
}
