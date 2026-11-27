using System;
using System.Linq;
using UnityEngine;

namespace Suborned.Play
{
    internal static class DistrictVehicleLod
    {
        public static void AddKitLevels(GameObject root, GameObject model, GameObject middle, GameObject far, bool small = false)
        {
            if (!middle || !far)
                return;
            var midModel = UnityEngine.Object.Instantiate(middle, root.transform, false);
            var farModel = UnityEngine.Object.Instantiate(far, root.transform, false);
            var group = root.AddComponent<LODGroup>();
            group.SetLODs(new[] { new LOD(small ? .14f : .18f, model.GetComponentsInChildren<Renderer>()),
                new LOD(small ? .035f : .06f, midModel.GetComponentsInChildren<Renderer>()),
                new LOD(small ? .004f : .006f, farModel.GetComponentsInChildren<Renderer>()) });
            group.RecalculateBounds();
        }

        public static void AddSedan(GameObject root, GameObject model)
        {
            if (model.GetComponentInChildren<LODGroup>())
                return;
            var near = model.GetComponentsInChildren<MeshRenderer>();
            if (near.Length == 0)
                return;
            var bounds = near[0].bounds;
            foreach (var renderer in near)
                bounds.Encapsulate(renderer.bounds);
            var largest = near.OrderByDescending(r => r.bounds.size.sqrMagnitude).First();
            // At distance the painted body and raised cabin keep the original sedan's proportions
            // and colours. Fine chrome, lamps and wheels stay on the near kit model.
            var paint = largest.sharedMaterials.FirstOrDefault(m => m && m.name.IndexOf("body", StringComparison.OrdinalIgnoreCase) >= 0) ?? largest.sharedMaterial;
            var glass = near.SelectMany(r => r.sharedMaterials).FirstOrDefault(m => m && m.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0) ?? paint;
            var centre = root.transform.InverseTransformPoint(bounds.center);
            var size = bounds.size;
            MeshRenderer Shape(string name, Vector3 at, Vector3 scale, Material material)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = name;
                box.transform.SetParent(root.transform, false);
                box.transform.localPosition = at;
                box.transform.localScale = scale;
                var collider = box.GetComponent<Collider>();
                collider.enabled = false;
                UnityEngine.Object.Destroy(collider);
                var renderer = box.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                return renderer;
            }

            var body = Shape("distant body", centre - Vector3.up * size.y * .13f, new Vector3(size.x, size.y * .45f, size.z), paint);
            var cabin = Shape("distant cabin", centre + Vector3.up * size.y * .17f, new Vector3(size.x * .82f, size.y * .36f, size.z * .52f), glass);
            var group = root.AddComponent<LODGroup>();
            group.SetLODs(new[] { new LOD(.045f, near), new LOD(.005f, new Renderer[] { body, cabin }) });
            group.RecalculateBounds();
        }
    }
}
