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
            n += Lamps(root);
            // moment 8's tilt-shift (Juice109Tilt, -tiltshift): its Fullscreen Shader Graph's material
            var tilt = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Suborned/Generated/Juice109/Juice_TiltShift.shadergraph");
            if (tilt) { var tp = $"{Dir}/Juice-TiltShift.mat"; var tm = AssetDatabase.LoadAssetAtPath<Material>(tp); if (!tm) { AssetDatabase.CreateAsset(new Material(tilt) { name = "Juice-TiltShift" }, tp); n++; } else if (tm.shader != tilt) { tm.shader = tilt; EditorUtility.SetDirty(tm); n++; } }
            else Debug.LogWarning("Juice109Assets: no Juice_TiltShift.shadergraph (run bun tools/juice/author.ts)");
            return n;
        }

        /// <summary>The getaway sedan's two headlamps, from its own model (round 2, Sky: "the cars headlights don't align properly when it drives off"):
        /// the DK_Lamp sub-mesh's vertices in the model's frame (its front is +Z), the front half, split left and right, and on each side the upper
        /// cluster (the headlamp; the parking lamp sits below it); its centre in x and y, its frontmost z. Written as "x,y,z;x,y,z" (model metres) to
        /// Resources/Juice109/lamps-<model>.txt for Juice109Fx, which parents the flares to the drawn model at those points.</summary>
        static int Lamps(string root)
        {
            const string model = "Assets/Suborned/Kit/Models/Vehicles/DK_Sedan_Getaway.fbx";
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(model); if (!go) { Debug.LogWarning($"Juice109Assets: no {model}"); return 0; }
            var side = new[] { new System.Collections.Generic.List<Vector3>(), new System.Collections.Generic.List<Vector3>() };
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                var mr = mf.GetComponent<MeshRenderer>(); var mesh = mf.sharedMesh; if (!mr || !mesh) continue;
                var mats = mr.sharedMaterials; var verts = mesh.vertices; var toRoot = go.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                for (int sub = 0; sub < mesh.subMeshCount && sub < mats.Length; sub++)
                {
                    if (!mats[sub] || !mats[sub].name.StartsWith("DK_Lamp")) continue;
                    foreach (int vi in mesh.GetTriangles(sub)) { var p = toRoot.MultiplyPoint3x4(verts[vi]); if (p.z > 0f) side[p.x < 0f ? 0 : 1].Add(p); }
                }
            }
            var parts = new System.Collections.Generic.List<string>();
            foreach (var pts in side)
            {
                if (pts.Count == 0) { Debug.LogWarning("Juice109Assets: no DK_Lamp vertices on one side of the getaway sedan's front"); return 0; }
                float top = float.MinValue; foreach (var p in pts) top = Mathf.Max(top, p.y);
                Vector3 lo = Vector3.one * float.MaxValue, hi = Vector3.one * float.MinValue;
                foreach (var p in pts) if (p.y > top - 0.2f) { lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); } // the headlamp: within 0.2 m of the lamps' top
                parts.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F3},{1:F3},{2:F3}", (lo.x + hi.x) / 2f, (lo.y + hi.y) / 2f, hi.z));
            }
            var text = string.Join(";", parts); var path = $"{Dir}/lamps-DK_Sedan_Getaway.txt"; var full = Path.Combine(root, path);
            if (File.Exists(full) && File.ReadAllText(full) == text) return 0;
            File.WriteAllText(full, text); AssetDatabase.ImportAsset(path); Debug.Log($"Juice109Assets: the getaway sedan's headlamps at {text} (model metres)"); return 1;
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
