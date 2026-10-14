using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Suborned.Perf
{
    /// <summary>The guard against the NaN glow (brief 008 B6): renders the play camera once into a float target with post-processing
    /// off and counts the pixels that are NaN or infinite. Bloom spreads one such pixel into a glow with black holes, and URP's
    /// Stop NaNs would only hide it, so a stop that renders any is reported, not filtered. Runs in the settle phase only.</summary>
    public sealed class PerfNaN : IDisposable
    {
        readonly Camera cam;
        readonly UniversalAdditionalCameraData data;
        readonly RenderTexture rt;
        readonly Texture2D tex;

        public PerfNaN(Camera cam, int width, int height)
        {
            this.cam = cam;
            data = cam.GetUniversalAdditionalCameraData();
            rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat);
            tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        }

        public int Count()
        {
            bool post = data.renderPostProcessing;
            var target = cam.targetTexture;
            try
            {
                data.renderPostProcessing = false;
                cam.targetTexture = rt;
                cam.Render();
            }
            finally { cam.targetTexture = target; data.renderPostProcessing = post; }
            var prev = RenderTexture.active; RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            RenderTexture.active = prev;
            var px = tex.GetPixelData<float>(0);
            int bad = 0;
            for (int p = 0; p < px.Length; p += 4)
                if (!float.IsFinite(px[p]) || !float.IsFinite(px[p + 1]) || !float.IsFinite(px[p + 2])) bad++;
            return bad;
        }

        public void Dispose()
        {
            rt.Release();
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(tex);
        }
    }
}
