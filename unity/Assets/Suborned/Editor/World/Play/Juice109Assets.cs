using System.IO;
using UnityEditor;
using UnityEngine;

namespace Suborned.World.Play
{
    /// <summary>Brief 109: the few assets the juice (Suborned.Play.Juice109) needs in the player, made at build time into Resources/Juice109 (git-ignored),
    /// beside Fx097's: a soft ring sprite (the destination's pulse) and the glow material on the Shader Graph shader tools/juice/author.ts writes
    /// (Generated/Juice109/Juice_Glow.shadergraph: map x tint x vertex colour, additive), one on the soft sprite (flares) and one on the ring, and the
    /// tyre smoke's VFX Graph (Generated/Juice109/Juice_TyreSmoke.vfx) copied in.
    /// Deterministic; only written when missing or changed. Returns how many assets it wrote.</summary>
    public static class Juice109Assets
    {
        public const string Dir = "Assets/Suborned/Resources/Juice109", Graph = "Assets/Suborned/Generated/Juice109/Juice_Glow.shadergraph";

        public static int Ensure()
        {
            int n = 0; string root = Path.Combine(Application.dataPath, "..");
            Directory.CreateDirectory(Path.Combine(root, Dir));
            var ringPath = $"{Dir}/juice-ring.png";
            if (!File.Exists(Path.Combine(root, ringPath)))
            {
                const int s = 128; var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
                for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f, r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.1f); a = a * a * (3f - 2f * a); // a soft band at 86 % of the radius
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                File.WriteAllBytes(Path.Combine(root, ringPath), t.EncodeToPNG()); Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(ringPath);
                var imp = (TextureImporter)AssetImporter.GetAtPath(ringPath);
                imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Clamp; imp.sRGBTexture = true; imp.SaveAndReimport(); n++;
            }
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Graph);
            if (!shader) { Debug.LogWarning($"Juice109Assets: no shader at {Graph} (run bun tools/juice/author.ts)"); return n; }
            var soft = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Fx097Assets.Dir}/fx097-soft.png");
            n += Mat($"{Dir}/Juice-Glow.mat", shader, soft);
            n += Mat($"{Dir}/Juice-Ring.mat", shader, AssetDatabase.LoadAssetAtPath<Texture2D>(ringPath));
            // the getaway's tyre smoke (VFX Graph): tools/juice/author.ts writes it beside the shader; the player loads it from Resources
            string vfxFrom = Path.Combine(root, "Assets/Suborned/Generated/Juice109/Juice_TyreSmoke.vfx"), vfxTo = $"{Dir}/Juice_TyreSmoke.vfx";
            if (File.Exists(vfxFrom) && (!File.Exists(Path.Combine(root, vfxTo)) || File.ReadAllText(vfxFrom) != File.ReadAllText(Path.Combine(root, vfxTo))))
            { File.Copy(vfxFrom, Path.Combine(root, vfxTo), true); AssetDatabase.ImportAsset(vfxTo, ImportAssetOptions.ForceUpdate); n++; }
            return n;
        }

        static int Mat(string path, Shader shader, Texture2D tex)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m && m.shader == shader && m.GetTexture("_BaseMap") == tex) return 0;
            bool fresh = !m; if (fresh) m = new Material(shader) { name = Path.GetFileNameWithoutExtension(path), enableInstancing = true };
            m.shader = shader; m.SetTexture("_BaseMap", tex); m.SetColor("_Tint", Color.white);
            if (fresh) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
            return 1;
        }
    }
}
