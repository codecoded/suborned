#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The image-to-3D citizens in engine: every GLB in -dkLineup (default Assets/Suborned/OldTownArt/Citizens3D/bakeoff) stood
    /// in rows on Orbit Square's paving, facing the game camera, the RFM crowd cleared round them, and shot through
    /// DecoKitIso at play zoom (14), close zoom (8) and a detail zoom (3). Raw sources are scaled to 1.75 m; LODs
    /// (<stem>@<tris>.glb, citizen_lod.py) keep their baked height. Rows of -dkRow run along the camera's right in
    /// file-name order, one per Space m, front row first, so the tiles label left to right. Nothing is saved.
    ///   -dkLineup <assets dir>   -dkRow 12   -dkOut <prefix>
    internal static class DecoKitCitizenLineup
    {
        const float Yaw = 45f, Space = .9f, RowGap = 2.2f, Clear = 9f, Height = 1.75f;
        static readonly Vector3 Centre = new(-13.7f, 0f, -142.2f);  // open paving in the iso "close" shot

        internal static void Shoot()
        {
            DecoKitStreetDemo.OpenSaved().enabled = false;
            DecoKitHarbour.Ocean();
            string dir = DecoKitStage.Arg("dkLineup", "Assets/Suborned/OldTownArt/Citizens3D/bakeoff");
            var files = Directory.GetFiles(dir, "*.glb").Select(f => f.Replace('\\', '/')).OrderBy(f => f).ToArray();
            foreach (var r in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                                    .Where(t => t.name.StartsWith("RFM_People") || t.name.StartsWith("RFM_City"))
                                    .Where(t => (t.position - Centre).sqrMagnitude < Clear * Clear))
                r.gameObject.SetActive(false);
            var turn0 = Quaternion.Euler(0f, Yaw, 0f);
            var (right, away) = (turn0 * Vector3.right, turn0 * Vector3.forward);
            int perRow = Mathf.Max(1, (int)DecoKitStage.Arg("dkRow", 12f)), rows = (files.Length + perRow - 1) / perRow;
            var lineup = new GameObject("DK citizen lineup").transform;
            for (int i = 0; i < files.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(files[i]);
                if (asset == null) { Debug.LogWarning($"[DecoKit] lineup: cannot load {files[i]}"); continue; }
                var go = (GameObject)Object.Instantiate(asset, lineup);
                // glTF fronts face +Z; the raw Tripo mesh faces -X (citizen_compare.py FACING). Turned on top of the
                // root's own node rotation (Hunyuan stands its Z-up mesh with one)
                float turn = Path.GetFileNameWithoutExtension(files[i]).EndsWith("__tripo") ? 90f : 0f;
                go.transform.rotation = Quaternion.Euler(0f, Yaw + 180f + turn, 0f) * go.transform.rotation;
                if (!files[i].Contains("@")) go.transform.localScale *= Height / Mathf.Max(DecoKitModels.Bounds(go).size.y, .01f);
                int col = i % perRow, rowN = Mathf.Min(perRow, files.Length - i / perRow * perRow);
                var at = Centre + right * ((col - (rowN - 1) * .5f) * Space) + away * ((i / perRow - (rows - 1) * .5f) * RowGap);
                var b = DecoKitModels.Bounds(go);
                go.transform.position += at - new Vector3(b.center.x, b.min.y, b.center.z);
                int tris = go.GetComponentsInChildren<MeshFilter>().Where(m => m.sharedMesh)
                             .Sum(m => Enumerable.Range(0, m.sharedMesh.subMeshCount).Sum(k => (int)m.sharedMesh.GetIndexCount(k) / 3));
                Debug.Log($"[DecoKit] lineup {i} {Path.GetFileNameWithoutExtension(files[i])}: {tris} tris");
            }
            DecoKitIso.Shoot(DecoKitStage.Arg("dkOut", "lineup"), new[]
            {
                ("play", Centre, 14f, Yaw), ("close", Centre, 8f, Yaw), ("detail", Centre + Vector3.up * .9f, 3f, Yaw),
            });
        }
    }
}
#endif
