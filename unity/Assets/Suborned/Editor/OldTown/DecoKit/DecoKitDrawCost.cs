#if UNITY_EDITOR
using System.Linq;
using UnityEngine;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// What a camera would draw before occlusion culling (UnityStats reads 0 for offscreen batch renders): renderers
    /// and submeshes in the frustum, banded by screen height (the LODGroup metric) so size culling can be judged.
    internal static class DecoKitDrawCost
    {
        static readonly float[] Bands = { .005f, .02f, .1f };

        /// Submeshes of `seen` still visible after the camera's own culling (frustum + baked occlusion), via a CullingGroup
        /// on their bounding spheres (conservative), evaluated by one real render.
        internal static int Visible(Camera cam, MeshRenderer[] seen, bool top = false)
        {
            using var group = new CullingGroup { targetCamera = cam };
            var spheres = seen.Select(r => new BoundingSphere(r.bounds.center, r.bounds.extents.magnitude)).ToArray();
            group.SetBoundingSpheres(spheres);
            group.SetBoundingSphereCount(spheres.Length);
            Object.DestroyImmediate(DecoKitStage.Render(cam, 384, 256, 1));
            var shown = seen.Where((r, i) => group.IsVisible(i)).ToArray();
            if (top)
                foreach (var g in shown.GroupBy(Source).Select(g => (g.Key, n: g.Sum(r => r.sharedMaterials.Length), c: g.Count()))
                                       .OrderByDescending(g => g.n).Take(16))
                    Debug.Log($"[DecoKit]   {g.Key}: {g.n} submeshes in {g.c} renderers");
            return shown.Sum(r => r.sharedMaterials.Length);
        }

        /// The placed kit piece a renderer belongs to (outermost prefab instance), else its scene root.
        static string Source(Renderer r) =>
            UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject)?.name ?? r.transform.root.name;

        internal static MeshRenderer[] Log(Camera cam, string label)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            var seen = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                             .Where(r => r.enabled && r.gameObject.activeInHierarchy && GeometryUtility.TestPlanesAABB(planes, r.bounds))
                             .ToArray();
            var band = new int[Bands.Length + 1];
            float k = .5f / Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad);
            var eye = cam.transform.position;
            foreach (var r in seen)
            {
                var b = r.bounds;
                float h = Mathf.Max(b.size.x, b.size.y, b.size.z) * k / Mathf.Max(Vector3.Distance(eye, b.center), .01f);
                int i = 0;
                while (i < Bands.Length && h >= Bands[i]) i++;
                band[i] += r.sharedMaterials.Length;
            }
            Debug.Log($"[DecoKit] cost {label}: {seen.Length} renderers, {band.Sum()} submeshes, " +
                      $"{seen.SelectMany(r => r.sharedMaterials).Distinct().Count()} materials; submeshes by screen height " +
                      $"<0.5% {band[0]}, 0.5-2% {band[1]}, 2-10% {band[2]}, >10% {band[3]}");
            return seen;
        }
    }
}
#endif
