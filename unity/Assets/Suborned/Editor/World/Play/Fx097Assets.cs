using System.IO;
using UnityEditor;
using UnityEngine;

namespace Suborned.World.Play
{
    /// <summary>Brief 097: the few assets the runtime VFX (Suborned.Fx097) needs in the player, made at build time into Resources/Fx097 (git-ignored):
    /// a soft round sprite (a smooth radial falloff, 64 px with mips) and two URP particle materials on it, alpha-blended (spray, petals) and
    /// soft-additive (mist, glints, dust in the sun). Deterministic; only written when missing. Returns how many assets it wrote.</summary>
    public static class Fx097Assets
    {
        public const string Dir = "Assets/Suborned/Resources/Fx097";

        public static int Ensure()
        {
            int n = 0;
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", Dir));
            var texPath = $"{Dir}/fx097-soft.png";
            if (!File.Exists(Path.Combine(Application.dataPath, "..", texPath)))
            {
                const int s = 64; var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
                for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f, r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - r); a = a * a * (3f - 2f * a);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                File.WriteAllBytes(Path.Combine(Application.dataPath, "..", texPath), t.EncodeToPNG()); Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(texPath);
                var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
                imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Clamp; imp.sRGBTexture = true; imp.SaveAndReimport(); n++;
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            n += Mat($"{Dir}/Fx097-Blend.mat", tex, additive: false);
            n += Mat($"{Dir}/Fx097-Add.mat", tex, additive: true);
            if (!AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/Fx097-Spark.mat"))
            { n += Mat($"{Dir}/Fx097-Spark.mat", tex, additive: true); var sp = AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/Fx097-Spark.mat"); sp.SetColor("_BaseColor", new Color(3.2f, 2.6f, 1.9f, 1f)); EditorUtility.SetDirty(sp); }
            if (!AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/Fx097-Bird.mat") && Shader.Find("Suborned/Bird") is Shader bird)
            { AssetDatabase.CreateAsset(new Material(bird) { name = "Fx097-Bird", enableInstancing = true }, $"{Dir}/Fx097-Bird.mat"); n++; }
            return n;
        }

        static int Mat(string path, Texture2D tex, bool additive)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path)) return 0;
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = new Material(sh) { name = Path.GetFileNameWithoutExtension(path) };
            m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f); // transparent; alpha or additive
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f); m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); if (additive) m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, path); return 1;
        }
    }
}
