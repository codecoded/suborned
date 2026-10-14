using System.IO;
using Suborned.Perf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Perf
{
    /// <summary>Brief 008 B6, Glint's warmth question (the player reads warmer than the Editor, in sun and shade): the play camera's
    /// home frame rendered in the Editor at 1920x1080 the way the review shots are (Shots: Camera.Render into an ARGB32 target with
    /// 4x MSAA, the GPU Resident Drawer off), and the grade, ambient and sun it resolved (PerfLook), into
    /// briefs/008/perf/pass2/warmth/editor-home.{png,txt}. The player side comes from the perf harness's `look` pass. Reads only:
    /// the scene is not changed or saved.</summary>
    public static class WarmthProbe
    {
        [MenuItem("Suborned/Perf/Warmth Probe (Editor home frame)")]
        public static void Run()
        {
            var dir = Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "008", "perf", "pass2", "warmth");
            Directory.CreateDirectory(dir);
            var cam = GameObject.Find("cameras/play").GetComponent<Camera>();
            var urp = UniversalRenderPipeline.asset;
            var drawer = urp.gpuResidentDrawerMode;
            try
            {
                urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
                var ts = cam.GetComponent<TacticalShadows>();
                using (ts ? ts.Use() : null)
                {
                    Shot(cam, 64, 64, null); // the first render after a change comes out flat
                    Shot(cam, 1920, 1080, Path.Combine(dir, "editor-home.png"));
                }
                var lines = PerfLook.Read(cam);
                File.WriteAllLines(Path.Combine(dir, "editor-home.txt"), System.Linq.Enumerable.Prepend(lines,
                    "Editor render of cameras/play at its built focus (home), 1920x1080, Camera.Render into RenderTexture ARGB32 (sRGB in a linear project), MSAA 4, GPU Resident Drawer off for the render (as Suborned/World/Render Shots)"));
                Debug.Log($"Perf: warmth probe written to {dir}");
            }
            finally { urp.gpuResidentDrawerMode = drawer; }
        }

        static void Shot(Camera cam, int w, int h, string file)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var previous = RenderTexture.active;
            cam.targetTexture = rt;
            cam.ResetAspect();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            if (file != null) File.WriteAllBytes(file, tex.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
