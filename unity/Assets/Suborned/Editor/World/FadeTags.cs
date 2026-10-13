using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Marks, at build time, what the play camera's architecture fade may thin (GDD §15: "tagged architecture ghosts to
    /// 0.2"; Suborned.ArchitectureFade): every renderer of a tagged layer gets rendering-layer bit ArchitectureFade.Layer.
    /// Tagged: buildings and what is fixed to them (skins, kit facades, roofs, frontages, awnings and banners), megastructures,
    /// gate blocks, the signature towers and structures, landmarks; of the civic layer only the masses built as buildings (the
    /// café, the Records Office and its towers: the facade atlas batches), never its furniture. Never tagged: ground, transit,
    /// decks and connectors (people stand on them), street furniture, the horizon and the sea; people and signals are not
    /// built here and never carry the bit. A tagged batch in a plain URP Lit material takes a copy of it in the cutaway
    /// shader (same name and colour), since only the cutaway shaders fade; glass (transparent) is left as it is.</summary>
    public static class FadeTags
    {
        static readonly HashSet<string> Whole = new(System.StringComparer.Ordinal)
        { "buildings", "skins", "facades", "roofs", "frontages", "fabric", "megastructures", "gates", "signatures", "landmarks" };
        /// <summary>Layers that hold buildings and furniture: only their facade-atlas batches fade.</summary>
        static readonly HashSet<string> BuildingsOnly = new(System.StringComparer.Ordinal) { "civic" };

        const string Cutaway = "DecoKit/Lit Cutaway", Lit = "Universal Render Pipeline/Lit";

        /// <summary>Tags the kit pieces under root/&lt;layer&gt; and the batches under batches/&lt;layer&gt;. Returns (kit renderers, batch renderers) tagged.</summary>
        public static (int kit, int batches) Apply(Transform root, Transform batches, Atlas atlas)
        {
            var cutaway = Shader.Find(Cutaway) ?? throw new InvalidDataException($"World: shader {Cutaway} not found (Assets/Suborned/Shaders/DecoKit)");
            var twins = new Dictionary<Material, Material>();
            int kit = 0, batched = 0;
            foreach (var layer in Whole)
                if (root.Find(layer) is { } t)
                    foreach (var r in t.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        r.renderingLayerMask |= ArchitectureFade.Layer;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(r); // kit pieces are prefab instances: an unrecorded change is lost on save
                        kit++;
                    }
            foreach (Transform layer in batches)
            {
                bool whole = Whole.Contains(layer.name);
                if (!whole && !BuildingsOnly.Contains(layer.name)) continue;
                foreach (var r in layer.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var m = r.sharedMaterial;
                    if (!whole && m != atlas.Matte && m != atlas.Glossy) continue;
                    if (m.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent) continue;
                    if (m.shader.name == Lit)
                    {
                        if (!twins.TryGetValue(m, out var twin)) twins[m] = twin = new Material(m) { shader = cutaway, name = m.name };
                        r.sharedMaterial = twin;
                    }
                    r.renderingLayerMask |= ArchitectureFade.Layer;
                    batched++;
                }
            }
            return (kit, batched);
        }
    }
}
