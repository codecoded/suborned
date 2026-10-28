#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The flagship's showpiece mirrors a sky of its own. Metal takes its colour from what it reflects: under the teal
    /// sky gold reads olive and chrome reads teal. A custom probe boxed round the landmark alone gives its metals a warm
    /// studio vault (cream sky, a bright horizon line, the limestone city below), so the gilt reads gold and the chrome
    /// reads chrome. Nothing else sits in the box but its own plaza stone, which is too rough to show it.
    internal static class DecoKitShowpiece
    {
        // elevation (deg), sRGB; x the sky gain. The zenith keeps a hint of the real sky's sea-green.
        static readonly (float el, Color c)[] Vault =
        {
            (-90f, Srgb(70, 54, 40)), (-20f, Srgb(150, 120, 88)), (-2f, Srgb(200, 172, 132)),
            (0f, Srgb(255, 244, 222) * 1.5f), (2f, Srgb(246, 232, 204)), (30f, Srgb(224, 214, 190)), (90f, Srgb(180, 196, 186)),
        };

        internal static void Apply(string landmark, float gain)
        {
            var go = GameObject.Find("DK_" + landmark);
            if (go == null) { Debug.LogError($"[DecoKit] showpiece {landmark} not placed"); return; }
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);

            var probe = new GameObject($"{landmark} showpiece probe").AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Custom;
            probe.customBakedTexture = Env(landmark, gain);
            probe.transform.position = b.center;
            probe.size = b.size + Vector3.one * 4f;
            probe.boxProjection = false;  // an infinitely far vault, not the box walls
            probe.blendDistance = 2f;
            probe.importance = 10;        // over the street and circus probes where the boxes meet
            probe.hdr = true;
            Debug.Log($"[DecoKit] showpiece probe {landmark}: {probe.size.x:0} x {probe.size.y:0} x {probe.size.z:0} m");
        }

        /// The vault as a latitude-longitude EXR imported as a specular-convolved cube (rough gilt blurs it right).
        static Texture Env(string landmark, float gain)
        {
            EnsureFolder(GeneratedDir + "/Probes");
            var path = $"{GeneratedDir}/Probes/DK_Showpiece_{landmark}.exr";
            const int w = 512, h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBAHalf, false, true);
            var row = new Color[w];
            for (int y = 0; y < h; y++)
            {
                var c = At((y + .5f) / h * 180f - 90f) * gain;
                for (int x = 0; x < w; x++) row[x] = c;
                tex.SetPixels(0, y, w, 1, row);
            }
            File.WriteAllBytes(path, tex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureShape = TextureImporterShape.TextureCube;
            ti.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
            var set = new TextureImporterSettings();
            ti.ReadTextureSettings(set);
            set.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
            ti.SetTextureSettings(set);
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture>(path);
        }

        static Color At(float el)
        {
            int i = 0;
            while (i < Vault.Length - 2 && el > Vault[i + 1].el) i++;
            var (e0, c0) = Vault[i];
            var (e1, c1) = Vault[i + 1];
            return Color.Lerp(c0, c1, Mathf.InverseLerp(e0, e1, el));
        }

        static Color Srgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f).linear;
    }
}
#endif
