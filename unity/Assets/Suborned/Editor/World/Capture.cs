using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Suborned.World
{
    /// <summary>Honest Editor frames (Glint's ruling, briefs/008/perf/pass2/warmth/glint-ruling.md, 2026-10-15). URP builds a
    /// camera's intermediate colour buffer from its target texture's format, so a Camera.Render into ARGB32 grades an 8-bit
    /// buffer: scene colour clamps at 1.0 before post-exposure, bloom, tonemap and grade, and sunlit stone loses its red. The
    /// player draws to the screen through an HDR float buffer (the asset's hdrColorBufferPrecision). Here the camera renders into
    /// ARGBHalf, MSAA as the URP asset's (the screen path's, not the old 4x), so the grade sees what the player's does; URP writes
    /// linear values into an RT target (no sRGB conversion), so a Graphics.Blit into an sRGB ARGB32 target does the backbuffer's
    /// encode in hardware, and the pixels are read from that. Never read the half target directly. Every Editor preview render
    /// goes through here: Shots (views.json, the 009-* crossing views), RenderSession.Grab (flyby, Glint stills, props audit,
    /// People Shots), WalkRig notes, WarmthProbe; PerfProbe takes its target for timing.</summary>
    public sealed class Capture : IDisposable
    {
        /// <summary>One line for evidence files: how the frame was made.</summary>
        public static string Describe => $"Camera.Render into ARGBHalf (MSAA {Msaa}, the URP asset's), Graphics.Blit into ARGB32 sRGB, ReadPixels (Capture: Glint's honest path)";

        /// <summary>The screen path's MSAA: the active URP asset's sample count (1 today).</summary>
        public static int Msaa => UniversalRenderPipeline.asset is { } a ? Mathf.Max(1, a.msaaSampleCount) : 1;

        RenderTexture hdr, ldr;
        Texture2D tex;

        /// <summary>The HDR render target at w x h (owned by this capture): for callers that render without reading back (timing).</summary>
        public RenderTexture Target(int w, int h)
        {
            if (hdr != null && hdr.width == w && hdr.height == h && hdr.antiAliasing == Msaa) return hdr;
            Release();
            hdr = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { antiAliasing = Msaa, name = "capture-hdr" };
            ldr = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "capture-srgb" };
            tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            return hdr;
        }

        /// <summary>Renders cam at w x h and returns a readable texture owned by this capture (valid until the next call or Dispose).
        /// The camera's own target and RenderTexture.active are restored.</summary>
        public Texture2D Grab(Camera cam, int w, int h)
        {
            var target = Target(w, h);
            var previous = RenderTexture.active; var own = cam.targetTexture;
            try
            {
                cam.targetTexture = target;
                cam.ResetAspect();
                cam.Render();
                Graphics.Blit(target, ldr); // linear half -> sRGB 8-bit: the hardware encode the backbuffer gets
                RenderTexture.active = ldr;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
            }
            finally
            {
                cam.targetTexture = own;
                cam.ResetAspect();
                RenderTexture.active = previous;
            }
            return tex;
        }

        /// <summary>One frame as PNG bytes.</summary>
        public static byte[] Png(Camera cam, int w, int h)
        {
            using var c = new Capture();
            return c.Grab(cam, w, h).EncodeToPNG();
        }

        void Release()
        {
            if (hdr != null) { hdr.Release(); UnityEngine.Object.DestroyImmediate(hdr); hdr = null; }
            if (ldr != null) { ldr.Release(); UnityEngine.Object.DestroyImmediate(ldr); ldr = null; }
            if (tex != null) { UnityEngine.Object.DestroyImmediate(tex); tex = null; }
        }

        public void Dispose() => Release();
    }
}
