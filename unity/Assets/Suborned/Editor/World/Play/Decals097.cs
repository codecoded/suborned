using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Play
{
    /// <summary>Brief 097 (Sky 2026-11-06: "URP Decal projector sounds interesting"): URP's Decal renderer feature on the play renderer, Screen Space technique
    /// (no DBuffer prepass: the lit shaders need no new variants, and ShaderStrip097 keeps stripping _DBUFFER_MRT*), decal layers off; and the decal textures
    /// and materials the runtime (Suborned.Fx097 decals) projects, made here at build time into Resources/Fx097 (git-ignored), on URP's own decal shader:
    ///  - pock: a shot's chip mark where a miss lands on the ground (a dark scuffed centre, a few radial scratches);
    ///  - wet:  the fountain's splash halo on the paving round the basin (a soft dark ring);
    ///  - petals: a scatter of fallen coral, flamingo and butter petals under a flower bed.
    /// Idempotent; called from Look097Materials.Apply (every game build and the world build).</summary>
    public static class Decals097
    {
        const string Renderer = "Assets/Settings/PC_Renderer.asset";

        public static int Ensure()
        {
            int n = Feature();
            n += Make("pock", Pock); n += Make("wet", Wet); n += Make("petals", Petals);
            return n;
        }

        static int Feature()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Renderer); if (!data) { Debug.LogWarning($"Decals097: no {Renderer}"); return 0; }
            var f = data.rendererFeatures.OfType<DecalRendererFeature>().FirstOrDefault();
            bool made = false;
            if (!f)
            {
                f = ScriptableObject.CreateInstance<DecalRendererFeature>(); f.name = "Decals (brief 097)";
                AssetDatabase.AddObjectToAsset(f, data); data.rendererFeatures.Add(f); made = true;
                // the renderer's feature map (local ids) is rebuilt by its own validation, as the inspector's Add Renderer Feature does
                typeof(ScriptableRendererData).GetMethod("ValidateRendererFeatures", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(data, null);
            }
            var so = new SerializedObject(f);
            var tech = so.FindProperty("m_Settings.technique"); var layers = so.FindProperty("m_Settings.decalLayers"); var dist = so.FindProperty("m_Settings.maxDrawDistance");
            bool dirty = false;
            if (tech != null && tech.enumValueIndex != 2) { tech.enumValueIndex = 2; dirty = true; } // DecalTechniqueOption.ScreenSpace
            if (layers != null && layers.boolValue) { layers.boolValue = false; dirty = true; }
            if (dist != null && !Mathf.Approximately(dist.floatValue, 120f)) { dist.floatValue = 120f; dirty = true; }
            if (dirty) so.ApplyModifiedPropertiesWithoutUndo();
            if (made || dirty) { EditorUtility.SetDirty(f); EditorUtility.SetDirty(data); AssetDatabase.SaveAssets(); return 1; }
            return 0;
        }

        static int Make(string name, System.Func<int, int, Color> px)
        {
            var dir = Fx097Assets.Dir; var texPath = $"{dir}/decal-{name}.png"; var matPath = $"{dir}/Fx097-Decal-{name}.mat"; int n = 0;
            var full = Path.Combine(Application.dataPath, "..", texPath);
            if (!File.Exists(full))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                const int S = 128; var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
                for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) t.SetPixel(x, y, px(x, y));
                File.WriteAllBytes(full, t.EncodeToPNG()); Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(texPath); var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
                imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = true; imp.SaveAndReimport(); n++;
            }
            if (!AssetDatabase.LoadAssetAtPath<Material>(matPath))
            {
                var sh = Shader.Find("Shader Graphs/Decal"); if (!sh) { Debug.LogWarning("Decals097: no Shader Graphs/Decal"); return n; }
                var m = new Material(sh) { name = $"Fx097-Decal-{name}", enableInstancing = true };
                m.SetTexture("Base_Map", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath)); m.SetFloat("Normal_Blend", 0f);
                AssetDatabase.CreateAsset(m, matPath); n++;
            }
            return n;
        }

        static float H(int x, int y, int s) { unchecked { uint h = (uint)(x * 73856093 ^ y * 19349663 ^ s * 83492791); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (h & 0xffff) / 65535f; } }

        static Color Pock(int x, int y)
        {
            float dx = (x + 0.5f) / 64f - 1f, dy = (y + 0.5f) / 64f - 1f, r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
            float core = Mathf.Clamp01(1f - r / 0.32f); core = core * core;
            float spokes = Mathf.Pow(Mathf.Clamp01(Mathf.Cos(a * 7f + H(x / 9, y / 9, 1) * 2f)), 12f) * Mathf.Clamp01(1f - r / 0.85f) * 0.6f;
            float alpha = Mathf.Clamp01(core * 0.85f + spokes + H(x, y, 2) * 0.06f * Mathf.Clamp01(1f - r));
            return new Color(0.16f, 0.14f, 0.12f, alpha * 0.8f);
        }

        static Color Wet(int x, int y)
        {
            float dx = (x + 0.5f) / 64f - 1f, dy = (y + 0.5f) / 64f - 1f, r = Mathf.Sqrt(dx * dx + dy * dy);
            float ring = Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.12f, 2f)) * (0.75f + 0.5f * H(x / 6, y / 6, 3));
            return new Color(0.42f, 0.4f, 0.37f, Mathf.Clamp01(ring) * 0.38f);
        }

        static Color Petals(int x, int y)
        {
            float dx = (x + 0.5f) / 64f - 1f, dy = (y + 0.5f) / 64f - 1f, r = Mathf.Sqrt(dx * dx + dy * dy);
            int cx = x / 8, cy = y / 8; float h = H(cx, cy, 4); if (h > 0.34f || r > 0.95f) return new Color(0, 0, 0, 0);
            float px = (x % 8 - 4 + (H(cx, cy, 5) - 0.5f) * 4f) / 3f, py = (y % 8 - 4 + (H(cx, cy, 6) - 0.5f) * 4f) / 2f;
            float d = px * px + py * py; if (d > 1f) return new Color(0, 0, 0, 0);
            float k = H(cx, cy, 7); var c = k < 0.4f ? new Color(0.95f, 0.45f, 0.38f) : k < 0.75f ? new Color(0.93f, 0.4f, 0.6f) : new Color(0.98f, 0.86f, 0.45f);
            c.a = (1f - d) * 0.95f; return c;
        }
    }
}
