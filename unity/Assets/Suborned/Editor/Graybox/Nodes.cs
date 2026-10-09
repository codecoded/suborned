using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>Scene object creation for the graybox: empty groups and mesh parts at the origin (world-space vertices).</summary>
    public static class Nodes
    {
        public static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.isStatic = true;
            if (parent) go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>Mesh part named `name` under parent. Skipped (null) when the kit produced no triangles.</summary>
        public static GameObject Part(Transform parent, string name, MeshKit kit, string material)
        {
            if (kit.Repairs > 0) Debug.LogWarning($"Graybox: {parent.name}/{name}: {kit.Repairs} polygon repairs (repeated points or spurs; the export promises simple polygons)");
            if (kit.Fallbacks > 0) Debug.LogWarning($"Graybox: {parent.name}/{name}: {kit.Fallbacks} ear-clipping fallbacks");
            if (kit.Empty) return null;
            var go = new GameObject(name);
            go.isStatic = true;
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = kit.Build(parent.name + "/" + name);
            go.AddComponent<MeshRenderer>().sharedMaterial = Style.Mat(material);
            return go;
        }
    }
}
