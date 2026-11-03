using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Brief 056 item 3 (Splice): the size budget for the kit's and the people's textures, applied at import by a postprocessor, so the .meta files the
    /// generators write (tools/decokit/ground_tex.meta_text, the pave sheets, tools/figures) and the sha256 in contracts/world/kit.json stay as generated and the
    /// copies under Kit/ and People/ (git-ignored) are never hand-edited.
    ///  · KitCap: no kit texture is imported above 2048 px (the five 4096 px sheets: pavilion_atlas_v1 and its normal, pave_civic_v1 and its normal and mask;
    ///    a 4096 sheet with mips is 21.3 MB in the build, a 2048 one 5.3 MB). Formats are left as generated: normals are already BC5 and the rest BC7, both one
    ///    byte a texel, so a "cheaper" format would only be a lossier one (BC1 drops the mask's alpha).
    ///  · Streaming mipmaps on every kit and figure texture of 2048 px or more that has mips (they take effect through QualitySettings.streamingMipmapsActive, PC level).
    /// Presentation only: nothing the sim or a replay reads. Revert: delete this file (or set KitCap to 4096 and Stream to false) and let Unity reimport.</summary>
    public sealed class TextureBudget : AssetPostprocessor
    {
        public const int KitCap = 2048;
        public const bool Stream = true;
        /// <summary>Bump to make Unity reimport the textures this class touches.</summary>
        public override uint GetVersion() => 1;

        static readonly MethodInfo SourceSize = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);

        void OnPreprocessTexture()
        {
            var path = assetPath.Replace('\\', '/');
            bool kit = path.StartsWith("Assets/Suborned/Kit/Textures/"), people = path.StartsWith("Assets/Suborned/People/Figures/");
            if (!kit && !people) return;
            var ti = (TextureImporter)assetImporter;
            if (ti.textureShape != TextureImporterShape.Texture2D || ti.textureType == TextureImporterType.Sprite) return;
            if (kit)
            {
                if (ti.maxTextureSize > KitCap) ti.maxTextureSize = KitCap;
                var ps = ti.GetPlatformTextureSettings("Standalone"); // the generators' BC7 / BC5 override carries its own size
                if (ps.overridden && ps.maxTextureSize > KitCap) { ps.maxTextureSize = KitCap; ti.SetPlatformTextureSettings(ps); }
            }
            if (Stream && ti.mipmapEnabled && Longest(ti) >= 2048) ti.streamingMipmaps = true;
        }

        /// <summary>The longer side the texture imports at: its source size, capped by the importer's max size.</summary>
        static int Longest(TextureImporter ti)
        {
            int w = ti.maxTextureSize, h = ti.maxTextureSize;
            if (SourceSize != null) { var a = new object[] { 0, 0 }; SourceSize.Invoke(ti, a); w = (int)a[0]; h = (int)a[1]; }
            return Mathf.Min(Mathf.Max(w, h), ti.maxTextureSize);
        }
    }
}
