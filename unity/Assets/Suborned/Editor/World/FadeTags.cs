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
    /// café, the Records Office and its towers: the facade atlas batches) and the kit pieces the export flags `fade` (parasols,
    /// cypresses, the café bay, the pavilion and the entrances: what stands over a person), never its other furniture. Never tagged: ground, transit,
    /// decks and connectors (people stand on them), street furniture, the horizon and the sea; people and signals are not
    /// built here and never carry the bit. A tagged batch in a plain URP Lit material takes a copy of it in the cutaway
    /// shader (same name and colour), since only the cutaway shaders fade; glass (transparent) is left as it is.</summary>
    public static class FadeTags
    {
        static readonly HashSet<string> Whole = new(System.StringComparer.Ordinal)
        { "buildings", "skins", "facades", "roofs", "frontages", "fabric", "megastructures", "gates", "signatures", "landmarks" };
        /// <summary>Layers that hold buildings and furniture: only their facade-atlas batches fade.</summary>
        static readonly HashSet<string> BuildingsOnly = new(System.StringComparer.Ordinal) { "civic" };

        /// <summary>Whether batches of this layer may carry the fade (all of it, or for civic its facade-atlas batches).</summary>
        public static bool Layer(string layer) => Whole.Contains(layer) || BuildingsOnly.Contains(layer);

        const string Cutaway = "DecoKit/Lit Cutaway", Lit = "Universal Render Pipeline/Lit";

        /// <summary>The markers under `layer` of the elements the export flags (MassDto.Fade), by id. An id with no marker is rejected.</summary>
        public static List<Transform> Elements(Transform layer, IEnumerable<string> ids)
        {
            var byName = new Dictionary<string, Transform>(System.StringComparer.Ordinal);
            if (layer) foreach (Transform t in layer) byName[t.name] = t; // ids hold `/`: Transform.Find would read them as paths
            var list = new List<Transform>();
            foreach (var id in ids) list.Add(byName.TryGetValue(id, out var t) ? t : throw new InvalidDataException($"World: `{id}` is flagged fade but was not built"));
            return list;
        }

        /// <summary>Tags the kit pieces under root/&lt;layer&gt;, the kit pieces of the `flagged` elements (pieces of an untagged layer the
        /// export says may ghost: a parasol, a cypress, an entrance canopy; a flagged element without a kit piece is rejected, since
        /// batched geometry cannot be tagged apart) and the batches under batches/&lt;layer&gt;. Returns (kit renderers, batch renderers,
        /// kit renderers of flagged elements) tagged; the first count holds the third.</summary>
        public static (int kit, int batches, int flagged) Apply(Transform root, Transform batches, Atlas atlas, IReadOnlyList<Transform> flagged)
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
            int ofFlagged = 0;
            foreach (var el in flagged)
            {
                var rs = el.GetComponentsInChildren<MeshRenderer>(true);
                if (rs.Length == 0) throw new InvalidDataException($"World: {el.name} is flagged fade but has no kit piece to tag");
                foreach (var r in rs)
                {
                    if ((r.renderingLayerMask & ArchitectureFade.Layer) != 0) continue;
                    r.renderingLayerMask |= ArchitectureFade.Layer;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    kit++; ofFlagged++;
                }
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
            return (kit, batched, ofFlagged);
        }
    }
}
