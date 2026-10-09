using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>Evidence: counts per layer against the export, renderer bounds, and a deterministic hierarchy dump of the
    /// saved scene (paths, transforms, mesh sizes and content hashes, materials) for build-to-build comparison.
    /// Batchmode: -executeMethod Suborned.Graybox.Inspect.Dump [-dump &lt;path&gt;] (default out/unity/hierarchy.txt).</summary>
    public static class Inspect
    {
        public static Bounds Bounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Layer path → number of objects, following the export's keys (handover splits into its parts).</summary>
        public static SortedDictionary<string, int> Layers(Transform root)
        {
            var d = new SortedDictionary<string, int>(System.StringComparer.Ordinal);
            foreach (Transform layer in root)
            {
                if (layer.name == "handover") foreach (Transform part in layer) d["handover/" + part.name] = part.childCount;
                else d[layer.name] = layer.childCount;
            }
            return d;
        }

        public static string Counts(Transform root)
        {
            var sb = new StringBuilder();
            foreach (var kv in Layers(root)) sb.Append(kv.Key).Append(' ').Append(kv.Value).Append(", ");
            return sb.ToString().TrimEnd(',', ' ');
        }

        /// <summary>Logs an error for any layer whose object count differs from the export's array length, then fails the
        /// build (batchmode exits 1).</summary>
        public static void Verify(Transform root, object world)
        {
            int bad = 0;
            foreach (var kv in Layers(root))
            {
                var parts = kv.Key.Split('/');
                object node = world;
                foreach (var p in parts) node = J.Get(node, p);
                if (kv.Key == "handover/towers") node = Towers(world);
                // ground, circus, water and the crossing polygon have no 1:1 array of objects
                if (node is not List<object> list || (list.Count > 0 && list[0] is not Dictionary<string, object>)) continue;
                if (list.Count == kv.Value) continue;
                bad++;
                Debug.LogError($"Graybox: layer {kv.Key} has {kv.Value} objects, export has {list.Count}");
            }
            if (bad > 0) throw new InvalidDataException($"Graybox: {bad} layer counts differ from the export");
            Debug.Log("Graybox: every layer count matches the export");
        }

        /// <summary>Tower anchors across all Handover buildings (they are nested per building in the export).</summary>
        static List<object> Towers(object world)
        {
            var all = new List<object>();
            foreach (var b in J.A(J.Get(world, "handover"), "buildings"))
                if (J.Get(b, "towers") is List<object> t) all.AddRange(t);
            return all;
        }

        [MenuItem("Suborned/Graybox/Dump Hierarchy")]
        public static void Dump() => WorldBuilder.Batch(() =>
        {
            if (!WorldBuilder.MayReplaceScene()) return;
            var path = DumpPath();
            var scene = EditorSceneManager.OpenScene(WorldBuilder.ScenePath, OpenSceneMode.Single);
            var sb = new StringBuilder();
            int meshes = 0, missing = 0;
            foreach (var go in scene.GetRootGameObjects()) Walk(go.transform, "", sb, ref meshes, ref missing);
            var root = GameObject.Find(WorldBuilder.RootName);
            sb.Insert(0, $"layers: {Counts(root.transform)}\nmeshes: {meshes}, missing: {missing}\n");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"Graybox: dumped {path} ({meshes} meshes, {missing} missing)\n{Counts(root.transform)}");
            if (missing > 0) Debug.LogError($"Graybox: {missing} mesh filters lost their mesh on reload");
        });

        static string DumpPath() => WorldBuilder.Arg("-dump", Path.Combine(WorldBuilder.RepoRoot, "out", "unity", "hierarchy.txt"));

        static void Walk(Transform t, string prefix, StringBuilder sb, ref int meshes, ref int missing)
        {
            string path = prefix + "/" + t.name;
            sb.Append(path).Append(' ').Append(V(t.position)).Append(' ').Append(V(t.eulerAngles));
            var mf = t.GetComponent<MeshFilter>();
            if (mf)
            {
                var m = mf.sharedMesh;
                if (m) { meshes++; sb.Append(System.FormattableString.Invariant($" mesh v{m.vertexCount} t{m.triangles.Length / 3} #{Hash(m):x16} min{V(m.bounds.min)} max{V(m.bounds.max)}")); }
                else { missing++; sb.Append(" mesh MISSING"); }
                var mr = t.GetComponent<MeshRenderer>();
                sb.Append(" mat ").Append(mr && mr.sharedMaterial ? mr.sharedMaterial.name : "MISSING");
            }
            var cam = t.GetComponent<Camera>();
            if (cam) sb.Append(" camera ortho=").Append(cam.orthographic).Append(" size=").Append(N(cam.orthographicSize, "F2"))
                .Append(" enabled=").Append(cam.enabled);
            sb.Append('\n');
            foreach (Transform c in t) Walk(c, path, sb, ref meshes, ref missing);
        }

        static string N(float x, string format = "F3") => x.ToString(format, CultureInfo.InvariantCulture);
        static string V(Vector3 v) => "(" + N(v.x) + "," + N(v.y) + "," + N(v.z) + ")";

        static ulong Hash(Mesh m)
        {
            ulong h = 14695981039346656037UL;
            void Mix(int x) { unchecked { for (int k = 0; k < 4; k++) { h ^= (byte)(x >> (8 * k)); h *= 1099511628211UL; } } }
            foreach (var v in m.vertices) { Mix(System.BitConverter.SingleToInt32Bits(v.x)); Mix(System.BitConverter.SingleToInt32Bits(v.y)); Mix(System.BitConverter.SingleToInt32Bits(v.z)); }
            foreach (var i in m.triangles) Mix(i);
            return h;
        }
    }
}
