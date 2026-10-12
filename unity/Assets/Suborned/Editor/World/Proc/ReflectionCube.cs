using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World
{
    /// <summary>The reflection environment the facade glass and chrome read: a generated cube saved beside the scene
    /// (&lt;scene&gt;-reflection.asset), so scenes built side by side never swap or orphan each other's cube.</summary>
    public static class ReflectionCube
    {
        /// <summary>A sky / haze / warm-paving gradient with a sun glow as the default reflection, so glass and chrome have
        /// something to mirror (the scene has no skybox).</summary>
        public static void Build()
        {
            const int N = 64;
            var path = Path.ChangeExtension(HarbourWorldBuilder.ScenePath, null) + "-reflection.asset";
            var cube = new Cubemap(N, TextureFormat.RGBA32, true) { name = Path.GetFileNameWithoutExtension(path) };
            var sunDir = RenderSettings.sun ? -RenderSettings.sun.transform.forward : new Vector3(0.4f, 0.7f, 0.3f).normalized;
            Color zenith = new Color(0.30f, 0.52f, 0.72f), haze = new Color(0.93f, 0.90f, 0.82f), ground = new Color(0.70f, 0.64f, 0.55f), sun = new Color(1f, 0.93f, 0.78f);
            for (int f = 0; f < 6; f++)
            {
                var px = new Color[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                        Vector3 d = (CubemapFace)f switch
                        {
                            CubemapFace.PositiveX => new Vector3(1, v, -u), CubemapFace.NegativeX => new Vector3(-1, v, u),
                            CubemapFace.PositiveY => new Vector3(u, 1, -v), CubemapFace.NegativeY => new Vector3(u, -1, v),
                            CubemapFace.PositiveZ => new Vector3(u, v, 1), _ => new Vector3(-u, v, -1),
                        };
                        d.Normalize();
                        Color c = d.y >= 0 ? Color.Lerp(haze, zenith, Mathf.Pow(d.y, 0.55f)) : Color.Lerp(haze, ground, Mathf.Clamp01(-d.y * 3f));
                        float g = Mathf.Max(0f, Vector3.Dot(d, sunDir));
                        c = Color.Lerp(c, sun, Mathf.Pow(g, 24f) * 0.9f) + sun * Mathf.Pow(g, 600f) * 3f;
                        px[y * N + x] = c;
                    }
                cube.SetPixels(px, (CubemapFace)f);
            }
            cube.Apply(true);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Path.Combine(Application.dataPath, "..", path))));
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(cube, path);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cube;
            RenderSettings.reflectionIntensity = 1f;
        }
    }
}
