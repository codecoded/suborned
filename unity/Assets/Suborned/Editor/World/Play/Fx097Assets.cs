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
            n += Vents();
            return n;
        }

        /// <summary>The steam vents: the three drain grates or manholes of the world export nearest the Records Square fountain (plan -160, -236.5) but at
        /// least 9 m from it (they are in the roads round the square), in the old town's frame (plan y + 206 on Unity z), as "x,z;x,z;x,z".</summary>
        static int Vents()
        {
            var world = Path.Combine(HarbourWorldBuilder.RepoRoot, "out/world/harbour-district.world.json"); if (!File.Exists(world)) return 0;
            var pieces = J.A(J.Get(J.Get(Json.Parse(File.ReadAllText(world)), "ground"), "pieces"));
            var c = new Vector2(-160f, -236.5f); var found = new System.Collections.Generic.List<Vector2>();
            foreach (var o in pieces) { var k = J.S(o, "kind") ?? ""; if ((k == "drain" || k == "manhole") && J.Get(o, "at") != null) { var at = J.P(o, "at"); float d = (at - c).magnitude; if (d >= 9f && d < 45f) found.Add(at); } }
            found.Sort((a, b) => (a - c).sqrMagnitude.CompareTo((b - c).sqrMagnitude));
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < found.Count && parts.Count < 3; i++) parts.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F2},{1:F2}", found[i].x, found[i].y + Suborned.OldTown.OldTownFrame.DZ));
            var text = string.Join(";", parts); var path = $"{Dir}/vents.txt"; var full = Path.Combine(Application.dataPath, "..", path);
            if (File.Exists(full) && File.ReadAllText(full) == text) return 0;
            File.WriteAllText(full, text); AssetDatabase.ImportAsset(path); return 1;
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
