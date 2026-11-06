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
            n += Variation();
            n += Vents();
            return n;
        }

        /// <summary>Brief 097 (Redline's audit item 4; the concept stills paid/097-look-bar): the painted variation, one shared tiling texture the cutaway
        /// shaders multiply into albedo in world space (DK_Variation097.hlsl): a soft, sun-faded wash (low-frequency value noise, two scales, a few broad
        /// streaks), grey about 1.0 (0.9 .. 1.07), 512 px, mips, deterministic. Written once to Resources/Fx097/variation.png.</summary>
        static int Variation()
        {
            var path = $"{Dir}/variation.png"; var full = Path.Combine(Application.dataPath, "..", path); if (File.Exists(full)) return 0;
            const int N = 512; var rnd = new System.Random(4097); int[] cells = { 4, 8, 16, 32 }; var lat = new float[cells.Length][];
            for (int o = 0; o < cells.Length; o++) { int k = cells[o]; lat[o] = new float[k * k]; for (int i = 0; i < k * k; i++) lat[o][i] = (float)rnd.NextDouble(); }
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float v = 0, amp = 0.55f, sum = 0;
                for (int o = 0; o < cells.Length; o++)
                {
                    int k = cells[o]; float fx = (float)x / N * k, fy = (float)y / N * k; int x0 = (int)fx, y0 = (int)fy; float tx = fx - x0, ty = fy - y0; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                    float a = lat[o][(y0 % k) * k + x0 % k], b = lat[o][(y0 % k) * k + (x0 + 1) % k], c = lat[o][((y0 + 1) % k) * k + x0 % k], d = lat[o][((y0 + 1) % k) * k + (x0 + 1) % k];
                    v += amp * Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty); sum += amp; amp *= 0.5f;
                }
                v /= sum; float streak = 0.5f + 0.5f * Mathf.Sin((x + y * 0.35f) / N * Mathf.PI * 2f * 3f + v * 4f);
                float g = Mathf.Lerp(0.9f, 1.07f, Mathf.SmoothStep(0f, 1f, v)) * Mathf.Lerp(0.985f, 1.015f, streak);
                byte b8 = (byte)Mathf.RoundToInt(Mathf.Clamp01(g / 1.1f) * 255f); px[y * N + x] = new Color32(b8, b8, b8, 255);
            }
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false); t.SetPixels32(px); t.Apply(); File.WriteAllBytes(full, t.EncodeToPNG()); Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path); var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.sRGBTexture = false; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Repeat; imp.textureCompression = TextureImporterCompression.CompressedHQ; imp.SaveAndReimport();
            return 1;
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
            for (int i = 0; i < found.Count && parts.Count < 3; i++) parts.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F2},{1:F2}", found[i].x, found[i].y + 206f));
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
