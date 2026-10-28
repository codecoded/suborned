#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Import settings for the kit's generated textures and HDRIs (idempotent: only reimports on change).
    internal static class DecoKitTextures
    {
        internal static Texture2D Albedo(string file, bool alpha, int maxSize = 2048) =>
            Configure($"{TextureDir}/{file}", TextureImporterType.Default, srgb: true, alpha, alpha, maxSize);

        internal static Texture2D Normal(string file, int maxSize = 2048) =>
            Configure($"{TextureDir}/{file}", TextureImporterType.NormalMap, srgb: false, false, false, maxSize);

        /// pave_sheet.py mask: linear, R metallic / G occlusion / A smoothness (alpha kept, not transparency).
        internal static Texture2D Mask(string file, int maxSize = 4096) =>
            Configure($"{TextureDir}/{file}", TextureImporterType.Default, srgb: false, true, false, maxSize);

        /// Painted matte / sky gradient: full 3K width, clamped. Gradients stay uncompressed (BC7 bands them).
        internal static Texture2D Backdrop(string file, bool compressed = true)
        {
            var path = $"{TextureDir}/{file}";
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null) throw new System.IO.FileNotFoundException($"[DecoKit] missing backdrop {path}");
            var compression = compressed ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            if (ti.maxTextureSize != 4096 || ti.wrapMode != TextureWrapMode.Clamp || ti.textureCompression != compression)
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
                ti.maxTextureSize = 4096;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.textureCompression = compression;
                ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// Lat-long EXR for Skybox/Panoramic; clamp V so the poles do not bleed.
        internal static Texture2D Hdri(string name)
        {
            var path = $"{HdriDir}/{name}.exr";
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null) throw new System.IO.FileNotFoundException($"[DecoKit] missing HDRI {path}");
            bool dirty = ti.textureShape != TextureImporterShape.Texture2D || ti.wrapModeV != TextureWrapMode.Clamp
                         || ti.textureCompression != TextureImporterCompression.Uncompressed || ti.maxTextureSize != 8192;
            if (dirty)
            {
                ti.textureShape = TextureImporterShape.Texture2D;
                ti.wrapModeU = TextureWrapMode.Repeat;
                ti.wrapModeV = TextureWrapMode.Clamp;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.maxTextureSize = 8192;
                ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Texture2D Configure(string path, TextureImporterType type, bool srgb, bool alpha, bool transparent,
                                   int maxSize)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null) throw new System.IO.FileNotFoundException($"[DecoKit] missing texture {path}");
            var alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            bool dirty = ti.textureType != type || ti.sRGBTexture != srgb || ti.alphaIsTransparency != transparent
                         || ti.alphaSource != alphaSource || ti.maxTextureSize != maxSize
                         || ti.anisoLevel != 8 || ti.textureCompression != TextureImporterCompression.CompressedHQ;
            if (dirty)
            {
                ti.textureType = type;
                ti.sRGBTexture = srgb;
                ti.alphaSource = alphaSource;
                ti.alphaIsTransparency = transparent;
                ti.mipmapEnabled = true;
                ti.mipMapsPreserveCoverage = transparent;  // keeps cutout density at distance
                ti.alphaTestReferenceValue = .5f;
                ti.anisoLevel = 8;                    // ground tiles are seen at grazing angles
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.filterMode = FilterMode.Trilinear;
                ti.maxTextureSize = maxSize;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
#endif
