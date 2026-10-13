using UnityEngine;

namespace Suborned.World
{
    /// <summary>Scene objects: static groups, element markers (one per export element, named by id) and their parts. Part
    /// geometry (MeshKit, world-space vertices) goes to Combine, combined per layer, material and tile; markers carry no
    /// renderer.</summary>
    public sealed class Nodes
    {
        readonly Look look;
        public Nodes(Look look) => this.look = look;

        public static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name) { isStatic = true };
            if (parent) go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>Marker for one export element; Verify fails the build if it ends up without geometry.</summary>
        public static Transform Element(Transform parent, string id)
        {
            var t = Group(parent, id);
            Combine.Element(t);
            return t;
        }

        /// <summary>Adds a part to element `el` in surface's material; warns on polygon repairs.</summary>
        public void Part(Transform el, string name, MeshKit kit, string surface) => Part(el, name, kit, look.Mat(surface));

        public void Part(Transform el, string name, MeshKit kit, Material mat)
        {
            if (kit.Repairs > 0) Debug.LogWarning($"World: {el.name}/{name}: {kit.Repairs} polygon repairs");
            if (kit.Fallbacks > 0) Debug.LogWarning($"World: {el.name}/{name}: {kit.Fallbacks} ear-clipping fallbacks");
            if (kit.Empty) return;
            Combine.Add(el, LayerOf(el), kit, mat);
        }

        /// <summary>Adds a part to element `el` in the batch of `layer` and this surface that already reaches it (Combine.Fit:
        /// paint goes into kerbs drawn there anyway). Returns the metres that batch grew; -1 when none was within `reach` and
        /// the part took its own tile's batch; null when the part is empty.</summary>
        public float? PartIn(string layer, Transform el, string name, MeshKit kit, string surface, float reach)
        {
            if (kit.Repairs > 0) Debug.LogWarning($"World: {el.name}/{name}: {kit.Repairs} polygon repairs");
            if (kit.Empty) return null;
            return Combine.Fit(el, layer, kit, look.Mat(surface), reach);
        }

        /// <summary>One-part element.</summary>
        public Transform Solid(Transform layer, string id, MeshKit kit, string surface)
        {
            var el = Element(layer, id);
            Part(el, id, kit, surface);
            return el;
        }

        /// <summary>The layer an object belongs to: the ancestor directly under the build root.</summary>
        static string LayerOf(Transform t)
        {
            while (t.parent && t.parent.parent) t = t.parent;
            return t.name;
        }
    }
}
