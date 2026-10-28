#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The painted-sky atmosphere shared by the walkable demos: lat-long gradient sampled from the skyline painting
    /// (skyline_matte.py), warm trilight ambient, horizon-matched linear fog, baked box-projected probes.
    internal static class DecoKitAtmosphere
    {
        /// Trilight ambient (sRGB): shadows on cream stone carry warm bounce that sky-only ambient (no GI in these
        /// scenes) renders as deep blue.
        internal readonly struct Ambient
        {
            internal readonly Color Sky, Equator, Ground;
            internal Ambient(Color sky, Color equator, Color ground) { Sky = sky; Equator = equator; Ground = ground; }
        }

        internal static readonly Ambient PlazaAmbient = new(new Color(.42f, .5f, .62f), new Color(.46f, .45f, .43f),
                                                            new Color(.5f, .43f, .34f));

        /// name: DK_Sky_<name>.mat; gradient: lat-long file under Textures; gain: sky exposure (same as a skyline
        /// card's, so its faded edges meet the same colours); fill: ambient multiplier.
        internal static void PaintedSky(string name, string gradient, Ambient ambient, float gain, float fill,
                                        float fogStart, float fogEnd)
        {
            EnsureFolder(GeneratedDir);
            var path = $"{GeneratedDir}/DK_Sky_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var pano = Shader.Find("Skybox/Panoramic");
            if (mat == null) AssetDatabase.CreateAsset(mat = new Material(pano), path);
            else mat.shader = pano;
            mat.SetTexture("_MainTex", DecoKitTextures.Backdrop(gradient, compressed: false));
            mat.SetFloat("_Mapping", 1f);    // latitude-longitude
            mat.SetFloat("_ImageType", 0f);  // 360
            mat.SetColor("_Tint", new Color(.5f, .5f, .5f));  // neutral (x colour-space double)
            mat.SetFloat("_Exposure", gain);
            EditorUtility.SetDirty(mat);
            RenderSettings.skybox = mat;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Linear(ambient.Sky * fill);
            RenderSettings.ambientEquatorColor = Linear(ambient.Equator * fill);
            RenderSettings.ambientGroundColor = Linear(ambient.Ground * fill);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 256;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Linear(Horizon(gradient).linear * gain);
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
        }

        /// The gradient texel just above the horizon (file read directly: the imported texture is not readable).
        static Color Horizon(string gradient)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes($"{TextureDir}/{gradient}"));
            var c = tex.GetPixel(0, tex.height / 2 + 1);
            Object.DestroyImmediate(tex);
            return c;
        }

        static Color Linear(Color c) => c.gamma;  // RenderSettings colours are sRGB-encoded

        internal static void Probe(string name, Vector3 pos, Vector3 size)
        {
            var probe = new GameObject(name).AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.transform.position = pos;
            probe.size = size;
            probe.resolution = 256;  // URP's Forward+ atlas (8192 sq, buddy-allocated) holds ~12 probes at 512; the street has 17
            probe.hdr = true;
            probe.boxProjection = true;
            probe.blendDistance = 4f;
        }
    }
}
#endif
