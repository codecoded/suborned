using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World
{
    /// <summary>The export's crossings (brief 009: no zebra, no paint, no landing slab). One marker per crossing under `crossings`
    /// (named by id). A crossing's parts are street items with kit refs (light-dot rows and brass edge lines flush in the road along
    /// the walk, kerb studs, inlay arcs, signal eyes), placed with the street (CityLayers.Street); Dress then sets what the export
    /// says of them and the piece leaves to Unity:
    ///   shadows: only an eye's orb body casts (the item's `castShadows`); its beam, halo and displays, and every flush part, never do;
    ///   state: an eye shows its crossing's state (`walk` at build): the Walk figure on, the Stop figure off (or the other way);
    ///   displays: the orb body and its figures turn together about the vertical axis (they share the piece origin; the beam and halo
    ///     are round) so a display faces the play camera (the export view's yaw): a display along the walk is edge-on to a camera
    ///     looking along the crossing (Vela, 009 A open issue 1). Both faces show the same aspect, so the nearer one is turned.
    /// The beam and halo are DecoKit kind `glow` (Materials/Glow, Shaders/CrossGlow: additive, double-sided, unlit); the lights and
    /// figures are the kit's emissive materials, the seafoam light (Light) redrawn as emission only. A refuge is drawn here: its median ring raised as an island, in its road's kerb
    /// surface, in the ground layer's kerb batch that already reaches it (Combine.Fit), so it costs no renderer of its own.</summary>
    public sealed class CrossingKit
    {
        const string Ground = "ground"; // the batch layer refuges go into, with the kerbs
        /// <summary>The kit's seafoam light (dots, bars, the eye's lens and halo ring) and the factor its linear base colour is emitted at.
        /// The kit row (DK_Cross_Light) is lit seafoam plus a pale HDR emission (r/g 0.63 against the colour's 0.36): in sun the two sum
        /// past the Neutral tonemap's shoulder, the channels flatten and the warm grade filter leaves the dots cream-white (009 C:
        /// (224, 211, 191) at the kerb shot). Drawn instead as emission only (base black, no specular or reflections), seafoam x 2.0
        /// linear: green 0.92, under the bloom threshold, reads (154, 219, 198) in the same shot; 2.5 and 3 already pale toward white.</summary>
        public const string Light = "DK_Cross_Light"; const float LightGlow = 2.0f;
        public const string Body = "DK_SignalEye", Walk = "DK_SignalEye_Walk", Stop = "DK_SignalEye_Stop", Beam = "DK_SignalEye_Beam", Halo = "DK_SignalEye_Halo";
        static readonly HashSet<string> Kinds = new() { "crossing-dots", "crossing-bar", "crossing-edge", "crossing-stud", "crossing-arc", "signal-orb" };

        readonly Nodes nodes; readonly Catalogue.CrossingSpec spec;
        public CrossingKit(Nodes nodes, Catalogue kit) { this.nodes = nodes; spec = kit.Crossings; }

        /// <summary>After the ground layer (its kerb batches take the refuges) and the street layer (its pieces are the parts).</summary>
        public void Build(Transform layer, Transform street, WorldDto w)
        {
            var roads = new Dictionary<string, RoadDto>(); foreach (var r in w.Ground.Roads) roads[r.Id] = r;
            var items = new Dictionary<string, StreetItemDto>(System.StringComparer.Ordinal); foreach (var s in w.Street) items[s.Id] = s;
            int refuges = 0, inside = 0, grew = 0, alone = 0; float most = 0f;
            foreach (var c in w.Crossings)
            {
                if (c.Line.Length != 2 || (c.Line[1] - c.Line[0]).sqrMagnitude < 1e-6f) throw new InvalidDataException($"World: {c.Id}: `line` is not two points apart");
                if (c.Control is not (null or "signal")) throw new InvalidDataException($"World: {c.Id}: control `{c.Control}` is not drawn (signal | none; a zebra no longer exists)");
                if (c.Items.Length == 0) throw new InvalidDataException($"World: {c.Id}: a crossing with no parts");
                foreach (var id in c.Items)
                    if (!items.TryGetValue(id, out var part) || part.Crossing != c.Id) throw new InvalidDataException($"World: {c.Id}: part `{id}` is not a street item naming it back");
                roads.TryGetValue("road/" + c.Road, out var road);

                var islands = new MeshKit();
                foreach (var ring in c.Refuges)
                {
                    var median = w.Ground.Medians.Find(m => Poly.Contains(m.Ring, Centre(ring))) ?? throw new InvalidDataException($"World: {c.Id}: a refuge stands on no median");
                    var grown = Poly.Inset(Poly.Clean(ring), -spec.Lip) ?? throw new InvalidDataException($"World: {c.Id}: a refuge ring is degenerate");
                    islands.Prism(grown, median.Base, median.Base + median.Height + spec.Lip);
                    refuges++;
                }
                // a crossing's own geometry is its refuges; one without is a marker only (its parts are the street layer's pieces)
                var el = c.Refuges.Count > 0 ? Nodes.Element(layer, c.Id) : Nodes.Group(layer, c.Id);
                switch (nodes.PartIn(Ground, el, "refuges", islands, road?.KerbSurface ?? spec.Surface, spec.Reach))
                {
                    case null: break;
                    case < 0f: alone++; break;
                    case 0f: inside++; break;
                    case float g: grew++; most = Mathf.Max(most, g); break;
                }
            }
            Debug.Log($"World: crossings {w.Crossings.Count} ({w.Crossings.FindAll(c => c.Control == "signal").Count} signalled): {refuges} refuges ({inside} inside a kerb batch, " +
                $"{grew} grew one by at most {most:F2} m of {spec.Reach} m, {alone} in a ground tile of their own)");
            Dress(street, w);
        }

        /// <summary>Casters, the eye's state and its displays turned to the play camera, on the pieces the street layer placed.</summary>
        void Dress(Transform street, WorldDto w)
        {
            var els = new Dictionary<string, Transform>(System.StringComparer.Ordinal);
            foreach (Transform t in street) els[t.name] = t;
            // the play camera's heading (Unity: Euler(pitch, yaw, 0) looks along (sin yaw, 0, cos yaw)); a display faces back up it
            float yaw = w.View.Yaw * Mathf.Deg2Rad;
            var toCamera = -new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            int parts = 0, eyes = 0, walk = 0, casters = 0, quiet = 0, lit = 0; float turned = 0f;
            var kitLight = KitImport.Library(Light) ?? throw new InvalidDataException($"World: kit material {Light} missing (run `bun run kit`)");
            var light = Lens(kitLight);
            var byKind = new SortedDictionary<string, int>(System.StringComparer.Ordinal);
            foreach (var s in w.Street)
            {
                if (s.Crossing == null) continue;
                if (!Kinds.Contains(s.Kind)) throw new InvalidDataException($"World: {s.Id}: crossing part kind `{s.Kind}` is not drawn");
                if (s.Kit == null) throw new InvalidDataException($"World: {s.Id}: a crossing part without its kit piece (no adopted piece of its size)");
                if (!els.TryGetValue(s.Id, out var el) || el.childCount != 1) throw new InvalidDataException($"World: {s.Id}: crossing part not placed on the street layer");
                var piece = el.GetChild(0);
                parts++; byKind[s.Kind] = (byKind.TryGetValue(s.Kind, out var n) ? n : 0) + 1;
                bool eye = s.Kind == "signal-orb";
                if (eye && s.Signal is not ("walk" or "stop")) throw new InvalidDataException($"World: {s.Id}: signal state `{s.Signal}` (walk | stop)");
                if (!eye && s.CastShadows) throw new InvalidDataException($"World: {s.Id}: a flush crossing part that casts");
                float turn = 0f;
                if (eye)
                {
                    eyes++; if (s.Signal == "walk") walk++;
                    var a = piece.rotation * Vector3.forward; a.y = 0f; // display A: the piece's front (+Z)
                    turn = Vector3.SignedAngle(a, toCamera, Vector3.up);
                    if (turn > 90f) turn -= 180f; else if (turn < -90f) turn += 180f; // the nearer face: both show the same
                    turned = Mathf.Max(turned, Mathf.Abs(turn));
                }
                bool seenBody = false, seenWalk = false, seenStop = false;
                foreach (var r in piece.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var part = KitLods.Part(r.name);
                    bool body = eye && part == Body, glow = eye && (part == Beam || part == Halo);
                    seenBody |= body; seenWalk |= part == Walk; seenStop |= part == Stop;
                    r.shadowCastingMode = body && s.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    if (r.shadowCastingMode == ShadowCastingMode.On) casters++; else quiet++;
                    if (glow) { r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off; }
                    var ms = r.sharedMaterials; bool swap = false;
                    for (int i = 0; i < ms.Length; i++) if (ms[i] == kitLight) { ms[i] = light; swap = true; }
                    if (swap) { r.sharedMaterials = ms; lit++; }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    if (!eye) continue;
                    if (part == Walk || part == Stop)
                    {
                        r.gameObject.SetActive(part == (s.Signal == "walk" ? Walk : Stop));
                        PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);
                    }
                    if (body || part == Walk || part == Stop)
                    {   // about the piece's vertical axis through its origin (the parts share it)
                        var t = r.transform; var q = Quaternion.AngleAxis(turn, Vector3.up);
                        t.SetPositionAndRotation(piece.position + q * (t.position - piece.position), q * t.rotation);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                    }
                }
                if (eye && !(seenBody && seenWalk && seenStop)) throw new InvalidDataException($"World: {s.Id}: the SignalEye piece lacks its body, Walk or Stop part");
            }
            var kinds = new List<string>(); foreach (var kv in byKind) kinds.Add($"{kv.Value} {kv.Key}");
            Debug.Log($"World: crossing parts {parts} ({string.Join(", ", kinds)}); eyes {eyes} ({walk} walk, {eyes - walk} stop), displays turned to the play camera (yaw {w.View.Yaw}°) by at most {turned:F0}°; " +
                $"renderers casting {casters} (orb bodies, every LOD), not casting {quiet}; seafoam light on {lit} renderers (emission only, x{LightGlow})");
        }

        /// <summary>The seafoam light drawn as emission only, from the kit row (its colour, shader and fade); saved beside the scene.</summary>
        static Material Lens(Material kit)
        {
            var m = new Material(kit) { name = Light + "_Glow" };
            var c = kit.GetColor("_BaseColor").linear * LightGlow;
            m.SetColor("_BaseColor", Color.black);
            m.SetColor("_EmissionColor", new Color(c.r, c.g, c.b, 1f)); m.EnableKeyword("_EMISSION");
            m.SetFloat("_Smoothness", 0f); m.SetFloat("_SpecularHighlights", 0f); m.SetFloat("_EnvironmentReflections", 0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            var path = Path.ChangeExtension(HarbourWorldBuilder.ScenePath, null) + "-crosslight.mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Vector2 Centre(Vector2[] ring)
        {
            var c = Vector2.zero;
            foreach (var p in ring) c += p;
            return c / ring.Length;
        }
    }
}
