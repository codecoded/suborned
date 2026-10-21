using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World
{
    /// <summary>The reflection environment the facade glass and chrome read where no probe reaches: a generated cube saved beside the scene
    /// (&lt;scene&gt;-reflection.asset by day, &lt;scene&gt;-reflection-night.asset at night), so scenes built side by side never swap or orphan
    /// each other's cube. Glint, 026 chrome fix (light.json `reflection.street`): a street canyon, dark at the horizon, mid-grey walls,
    /// bright only high up, so chrome shows form instead of one flat pale tone.</summary>
    public static class ReflectionCube
    {
        /// <summary>The day cube becomes the default reflection at light.json `reflection.intensity`.</summary>
        public static void Build(Look look)
        {
            var sunDir = RenderSettings.sun ? -RenderSettings.sun.transform.forward : new Vector3(0.4f, 0.7f, 0.3f).normalized;
            var rf = look.Light("reflection");
            var cube = Make(look, look.Light("skyGradient"), rf, sunDir, Path.ChangeExtension(HarbourWorldBuilder.ScenePath, null) + "-reflection.asset");
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cube;
            RenderSettings.reflectionIntensity = rf != null && J.Get(rf, "intensity") != null ? J.F(rf, "intensity") : 1f;
        }

        /// <summary>The cube for a phase's sky and reflection blocks (night: light.json `night.skyGradient`, `night.reflection`), saved at `path`.</summary>
        public static Cubemap Make(Look look, object sky, object rf, Vector3 sunDir, string path)
        {
            const int N = 64;
            var cube = new Cubemap(N, TextureFormat.RGBA32, true) { name = Path.GetFileNameWithoutExtension(path) };
            if (sky == null) throw new InvalidDataException("World: light.json has no skyGradient for the reflection cube");
            Color zenith = look.Palette(J.S(sky, "zenith")), haze = look.Palette(J.S(sky, "horizon")), sun = look.Palette(J.S(sky, "sunGlow"));
            var st = rf != null ? J.Get(rf, "street") : null;
            // the old gradient's ground, for a light.json without `street`
            Color ground = rf != null ? look.Palette(J.S(rf, "ground")) : haze;
            Color wallBase = default, wall = default, gH = default, gN = default; float baseH = 0, band = 0, soft = 1, depth = 1, mix = 1;
            // Glint round 3: a sky-window streak, replacing the sky colour there (elevation from..to with soft edges, within ±halfWidth of the sun's azimuth)
            var sk = st != null ? J.Get(st, "streak") : null; Color streak = default; float sFrom = 0, sTo = 0, sSoft = 1, sHalf = 180, sStr = 0; bool towardSun = true;
            if (sk != null)
            {
                streak = look.Palette(J.S(sk, "colour")); sFrom = J.F(sk, "elevationFrom"); sTo = J.F(sk, "elevationTo"); sSoft = Mathf.Max(0.01f, J.F(sk, "soft"));
                sHalf = J.F(sk, "azimuthHalfWidth"); sStr = J.F(sk, "strength"); towardSun = !(J.Get(sk, "towardSun") is bool tb) || tb;
            }
            float sunAz = Mathf.Atan2(sunDir.x, sunDir.z) * Mathf.Rad2Deg;
            if (st != null)
            {
                wallBase = look.Palette(J.S(st, "wallBase")); wall = look.Palette(J.S(st, "wall"));
                gH = look.Palette(J.S(st, "groundHorizon")); gN = look.Palette(J.S(st, "groundNadir"));
                baseH = J.F(st, "baseHeight"); band = J.F(st, "bandHeight"); soft = Mathf.Max(0.01f, J.F(st, "bandSoft"));
                depth = Mathf.Max(0.01f, J.F(st, "groundDepth")); mix = J.F(st, "skyMix");
            }
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
                        Color c;
                        if (st == null) c = d.y >= 0 ? Color.Lerp(haze, zenith, Mathf.Pow(d.y, 0.55f)) : Color.Lerp(haze, ground, Mathf.Clamp01(-d.y * 3f));
                        else
                        {
                            float e = Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) * Mathf.Rad2Deg; // elevation, degrees
                            if (e < 0f) c = Color.Lerp(gH, gN, Mathf.Clamp01(-e / depth));
                            else
                            {
                                var skyC = Color.Lerp(haze, zenith, mix * Mathf.Pow(Mathf.Max(0f, d.y), 0.55f));
                                var wallC = Color.Lerp(wallBase, wall, Mathf.Clamp01(e / Mathf.Max(0.01f, baseH)));
                                c = Color.Lerp(wallC, skyC, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((e - band) / soft)));
                                if (sk != null)
                                {
                                    float we = Mathf.Clamp01((e - (sFrom - sSoft)) / sSoft) * Mathf.Clamp01(((sTo + sSoft) - e) / sSoft);
                                    float az = Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, sunAz));
                                    float wa = towardSun ? Mathf.Clamp01(((sHalf + sSoft) - az) / sSoft) : 1f;
                                    c = Color.Lerp(c, streak, Mathf.SmoothStep(0f, 1f, we) * Mathf.SmoothStep(0f, 1f, wa) * sStr);
                                }
                            }
                        }
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
            return cube;
        }
    }
}
