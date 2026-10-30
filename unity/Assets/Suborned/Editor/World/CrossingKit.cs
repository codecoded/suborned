using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World
{
    /// <summary>The export's crossings (brief 009: no zebra, no paint, no landing slab; brief 061 pick A). One marker per crossing under `crossings`
    /// (named by id). A crossing's parts are street items with kit refs (the light-dot rows flush in the road along the walk, light bars, the
    /// signal heads: Vela's SignalLens; the brass edge lines, kerb studs, inlay arcs, lit strip and SignalEye are retired, brief 061), placed with
    /// the street (CityLayers.Street); Dress then sets what the export says of them and the piece leaves to Unity:
    ///   shadows: only a head's body casts (the item's `castShadows`); its rim, displays, beam, halo and clock ring, and every flush part, never do;
    ///   the clock ring: the HaloRing piece at the head's `signal.halo`, on the head's own yaw (Vela's data: same pivot and yaw), under the head;
    ///   state: SignalHead shows its crossing's cycle at the mission clock (brief 061); the heads keep the export's `faces` yaw (never turned).
    /// The beam and halo are DecoKit kind `glow` (Materials/Glow, Shaders/CrossGlow: additive, double-sided, unlit); the rim, displays and ring
    /// are the kit's emissive materials driven per state; the dot rows' seafoam light (Light) is redrawn as emission only where baked, and gated
    /// per module (Shaders/SignalDots) where live. A refuge is drawn here: its median ring raised as an island, in its road's kerb
    /// surface, in the ground layer's kerb batch that already reaches it (Combine.Fit), so it costs no renderer of its own.</summary>
    public sealed class CrossingKit
    {
        /// <summary>The signal head's atlas (figure and numerals) and clock ring are white with the glyphs in alpha, cut at 0.5 (DK_Signal_Atlas,
        /// DK_Signal_Ring `_Cutoff`). Imported with Alpha Source None, the build drew every face as a solid square and every ring full (0.2.120's
        /// eye-level frames). The kit's own metas now carry the alpha (alphaUsage 1, alphaIsTransparency; Splice's catalogue, 0.2.123), so this is a
        /// guard: on every world build it checks the two importers and reimports only one that has lost its alpha (an older meta copied back by
        /// hand or by a kit pass), saying so in the log. Expect 0. Returns how many it reimported.</summary>
        public static int EnsureSignalTextures()
        {
            int n = 0;
            foreach (var path in new[] { "Assets/Suborned/Kit/Textures/signal_atlas_v1.png", "Assets/Suborned/Kit/Textures/signal_ring_v2.png" })
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) throw new InvalidDataException($"World: signal heads: {path} missing (the SignalLens and HaloRing pieces need it)");
                if (ti.alphaSource == TextureImporterAlphaSource.FromInput && ti.alphaIsTransparency) continue;
                ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.alphaIsTransparency = true; ti.SaveAndReimport(); n++;
                Debug.LogWarning($"World: signal heads: {path} reimported with its alpha (its meta had lost it: alphaSource {ti.alphaSource}; the kit's meta should carry alphaUsage 1)");
            }
            return n;
        }

        const string Ground = "ground"; // the batch layer refuges go into, with the kerbs
        /// <summary>The kit's seafoam light (dots, bars, the eye's lens and halo ring) and the factor its linear base colour is emitted at.
        /// The kit row (DK_Cross_Light) is lit seafoam plus a pale HDR emission (r/g 0.63 against the colour's 0.36): in sun the two sum
        /// past the Neutral tonemap's shoulder, the channels flatten and the warm grade filter leaves the dots cream-white (009 C:
        /// (224, 211, 191) at the kerb shot). Drawn instead as emission only (base black, no specular or reflections), seafoam x 2.0
        /// linear: green 0.92, under the bloom threshold, reads (154, 219, 198) in the same shot; 2.5 and 3 already pale toward white.</summary>
        public const string Light = "DK_Cross_Light"; const float LightGlow = 2.0f;
        /// <summary>Brief 061 pick A, Vela's SignalLens (crossing_lens.py; kit.json `parts`, checked 2026-10-25 against the FBX: DK_SignalLens and its
        /// _Rim, _Figure, _Tens, _Units, _Beam, _Halo, the LOD file's DK_SignalLens_L1_* read the same through KitLods.Part): the body, the emissive rim,
        /// the two-face figure and digit quads, the glows. The clock ring is its own kit piece (HaloRing, mesh DK_HaloRing) placed under the head here.
        /// Wired to SignalHead by part name.</summary>
        public const string LensBody = "DK_SignalLens", LensRim = "DK_SignalLens_Rim", LensFigure = "DK_SignalLens_Figure", LensTens = "DK_SignalLens_Tens", LensUnits = "DK_SignalLens_Units",
            LensBeam = "DK_SignalLens_Beam", LensHalo = "DK_SignalLens_Halo", RingPiece = "HaloRing", Ring = "DK_HaloRing";
        /// <summary>Brief 061: the dot rows of a signalled crossing are left unbaked and gated per module by SignalHead (_LitFrom/_LitTo on u, the
        /// SignalDots shader), on since Splice adopted Vela's re-cut LightDotRow (u = metres along the module), 2026-10-25. Each live module is its
        /// own instance (194 on the 16 signalled crossings of today's export; the build logs the renderers); `-bakeddots` bakes them as before, for
        /// Redline's perf064 row to compare.</summary>
        public static readonly bool LiveDots = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bakeddots") < 0;
        /// <summary>The crossings with a cycle (set by the builder before the street layer, so a live crossing's dot rows are not baked).</summary>
        public static readonly HashSet<string> Signalled = new(System.StringComparer.Ordinal);
        static readonly HashSet<string> Kinds = new() { "crossing-dots", "crossing-bar", "signal-orb" }; // brief 061: edge lines, studs, arcs and the lit strip are retired (unknown = reject)

        readonly Nodes nodes; readonly Catalogue.CrossingSpec spec; readonly KitPlacer placer;
        public CrossingKit(Nodes nodes, Catalogue kit, KitPlacer placer) { this.nodes = nodes; spec = kit.Crossings; this.placer = placer; }

        /// <summary>After the ground layer (its kerb batches take the refuges) and the street layer (its pieces are the parts).</summary>
        public void Build(Transform layer, Transform street, WorldDto w)
        {
            var roads = new Dictionary<string, RoadDto>(); foreach (var r in w.Ground.Roads) roads[r.Id] = r;
            var items = new Dictionary<string, StreetItemDto>(System.StringComparer.Ordinal); foreach (var s in w.Street) items[s.Id] = s;
            int refuges = 0, inside = 0, grew = 0, alone = 0, onIslands = 0; float most = 0f;
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
                    var at = Centre(ring);
                    var median = w.Ground.Medians.Find(m => Poly.Contains(m.Ring, at));
                    if (median == null)
                    {   // brief 075: a tram street's stop crossing runs kerb to kerb across both boarding islands (transit.stops) or a kerbed island (ground.islands):
                        // the island is the refuge and is drawn already (its own mass and kerb ribbon), so nothing is raised here
                        if (IslandUnder(w, at) == null) throw new InvalidDataException($"World: {c.Id}: a refuge at ({at.x:F2}, {at.y:F2}) stands on no median, boarding island or kerbed island");
                        onIslands++; refuges++; continue;
                    }
                    var grown = Poly.Inset(Poly.Clean(ring), -spec.Lip) ?? throw new InvalidDataException($"World: {c.Id}: a refuge ring is degenerate");
                    islands.Prism(grown, median.Base, median.Base + median.Height + spec.Lip);
                    refuges++;
                }
                // a crossing's own geometry is its refuges; one without is a marker only (its parts are the street layer's pieces)
                var el = !islands.Empty ? Nodes.Element(layer, c.Id) : Nodes.Group(layer, c.Id);
                switch (nodes.PartIn(Ground, el, "refuges", islands, road?.KerbSurface ?? spec.Surface, spec.Reach))
                {
                    case null: break;
                    case < 0f: alone++; break;
                    case 0f: inside++; break;
                    case float g: grew++; most = Mathf.Max(most, g); break;
                }
            }
            Debug.Log($"World: crossings {w.Crossings.Count} ({w.Crossings.FindAll(c => c.Control == "signal").Count} signalled): {refuges} refuges ({onIslands} on a boarding or kerbed island, drawn with it; {inside} inside a kerb batch, " +
                $"{grew} grew one by at most {most:F2} m of {spec.Reach} m, {alone} in a ground tile of their own)");
            Dress(street, w);
        }

        /// <summary>Casters, the clock ring and SignalHead on the pieces the street layer placed. The heads keep the export's `faces` yaw (brief 061: no
        /// turn to the play camera; baked toward yaw 30 they read as billboards at 120 and 300 and in third person).</summary>
        void Dress(Transform street, WorldDto w)
        {
            var els = new Dictionary<string, Transform>(System.StringComparer.Ordinal);
            foreach (Transform t in street) els[t.name] = t;
            int parts = 0, eyes = 0, walk = 0, casters = 0, quiet = 0, lit = 0, rings = 0;
            var kitLight = KitImport.Library(Light) ?? throw new InvalidDataException($"World: kit material {Light} missing (run `bun run kit`)");
            var light = LightMaterial(); int baked = 0;
            var byKind = new SortedDictionary<string, int>(System.StringComparer.Ordinal);
            var crossings = new Dictionary<string, CrossingDto>(System.StringComparer.Ordinal); foreach (var c in w.Crossings) crossings[c.Id] = c;
            var dotsBy = new Dictionary<string, List<(Renderer r, StreetItemDto s)>>(System.StringComparer.Ordinal); int heads = 0, cycled = 0, liveModules = 0;
            if (LiveDots) foreach (var s in w.Street) if (s.Crossing != null && s.Kind == "crossing-dots" && !CityLayers.BakedCrossingPart(s) && els.TryGetValue(s.Id, out var de) && de.childCount == 1)
                    { if (!dotsBy.TryGetValue(s.Crossing, out var dl)) dotsBy[s.Crossing] = dl = new(); foreach (var r in de.GetChild(0).GetComponentsInChildren<MeshRenderer>(true)) dl.Add((r, s)); liveModules++; }
            // the head that drives a crossing's dot rows: its first head in the export (the `a` end when it stands; a head left out for want of ground leaves the next)
            var driver = new Dictionary<string, string>(System.StringComparer.Ordinal); foreach (var s in w.Street) if (s.Crossing != null && eyeKind(s)) driver.TryAdd(s.Crossing, s.Id);
            foreach (var s in w.Street)
            {
                if (s.Crossing == null) continue;
                if (!Kinds.Contains(s.Kind)) throw new InvalidDataException($"World: {s.Id}: crossing part kind `{s.Kind}` is not drawn");
                if (s.Kit == null) throw new InvalidDataException($"World: {s.Id}: a crossing part without its kit piece (no adopted piece of its size)");
                if (!eyeKind(s) && CityLayers.BakedCrossingPart(s)) { if (s.CastShadows) throw new InvalidDataException($"World: {s.Id}: a flush crossing part that casts"); baked++; byKind[s.Kind] = (byKind.TryGetValue(s.Kind, out var nb) ? nb : 0) + 1; parts++; continue; } // brief 048: baked into crossing-parts (CityLayers.Street), its light swapped at bake time
                if (!els.TryGetValue(s.Id, out var el) || el.childCount != 1) throw new InvalidDataException($"World: {s.Id}: crossing part not placed on the street layer");
                var piece = el.GetChild(0);
                parts++; byKind[s.Kind] = (byKind.TryGetValue(s.Kind, out var n) ? n : 0) + 1;
                bool eye = s.Kind == "signal-orb";
                if (eye && s.Signal is not ("walk" or "stop")) throw new InvalidDataException($"World: {s.Id}: signal state `{s.Signal}` (walk | stop)");
                if (!eye && s.CastShadows) throw new InvalidDataException($"World: {s.Id}: a flush crossing part that casts");
                if (eye) { eyes++; if (s.Signal == "walk") walk++; PlaceRing(piece, s); rings++; }
                bool seenBody = false, seenRim = false, seenRing = false;
                foreach (var r in piece.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var part = KitLods.Part(r.name);
                    // glow: the head's beam and its halo disc
                    bool body = eye && part == LensBody, glow = eye && (part == LensBeam || part == LensHalo);
                    seenBody |= body; seenRim |= part == LensRim; seenRing |= part == Ring;
                    r.shadowCastingMode = body && s.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    if (r.shadowCastingMode == ShadowCastingMode.On) casters++; else quiet++;
                    if (glow) { r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off; }
                    var ms = r.sharedMaterials; bool swap = false;
                    for (int i = 0; i < ms.Length; i++) if (ms[i] == kitLight) { ms[i] = light; swap = true; }
                    if (swap) { r.sharedMaterials = ms; lit++; }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                }
                if (eye && !(seenBody && seenRim && seenRing)) throw new InvalidDataException($"World: {s.Id}: the signal head lacks its body ({LensBody}: {seenBody}), its rim ({LensRim}: {seenRim}) or its clock ring ({Ring}: {seenRing})");
                if (eye) { Head(piece, s, crossings.TryGetValue(s.Crossing, out var cx) ? cx : null, driver.TryGetValue(s.Crossing, out var dr) && dr == s.Id ? dotsBy : null, kitLight, light); heads++; if (crossings.TryGetValue(s.Crossing, out var cy) && cy.Cycle != null) cycled++; }
            }
            var kinds = new List<string>(); foreach (var kv in byKind) kinds.Add($"{kv.Value} {kv.Key}");
            int dotRenderers = 0, dotDraws = 0; foreach (var dl in dotsBy.Values) foreach (var (r, _) in dl) { dotRenderers++; dotDraws += r.sharedMaterials.Length; }
            Debug.Log($"World: signal heads {heads} with SignalHead and {rings} clock rings (HaloRing at signal.halo), {cycled} on their crossing's cycle (brief 061; the rest show the export's state); " +
                (LiveDots ? $"live dot rows: {liveModules} modules on {dotsBy.Count} signalled crossings, {dotRenderers} renderers (every LOD; {dotDraws} material slots), gated per module by SignalHead (perf064)" : "dot rows baked, not gated (-bakeddots)"));
            Debug.Log($"World: crossing parts {parts} ({string.Join(", ", kinds)}); heads {eyes} ({walk} walk, {eyes - walk} stop at build), displays on the export's faces (not turned); " +
                $"renderers casting {casters} (head bodies, every LOD), not casting {quiet}; seafoam light on {lit} renderers (emission only, x{LightGlow}); {baked} flush parts baked into {CityLayers.CrossingParts}");
        }

        /// <summary>Brief 061: the clock ring (HaloRing) under its head, at the head's `signal.halo` on the ground and on the head's own yaw (Vela's
        /// data: "the SignalLens's own transform: same pivot and yaw"), at its built size (radii 0.47 to 0.60, the export's halo clearance).
        /// A child of the head piece, so SignalHead finds it with the head's parts; not a placed piece of its own (the street's kit count stays the export's).</summary>
        void PlaceRing(Transform piece, StreetItemDto s)
        {
            if (!s.HasHalo) throw new InvalidDataException($"World: {s.Id}: a signal head without its `signal.halo`");
            if ((s.HaloAt - s.At).sqrMagnitude > 1e-4f || Mathf.Abs(s.HaloZ - s.Z) > 0.01f) throw new InvalidDataException($"World: {s.Id}: halo at ({s.HaloAt.x}, {s.HaloAt.y}, {s.HaloZ}) is not the head's pivot ({s.At.x}, {s.At.y}, {s.Z})");
            for (int i = piece.childCount - 1; i >= 0; i--) if (piece.GetChild(i).name.StartsWith(Ring, System.StringComparison.Ordinal)) throw new InvalidDataException($"World: {s.Id}: clock ring placed twice");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(placer.Prefab(RingPiece), piece);
            var t = go.transform; var ls = piece.lossyScale;
            t.SetPositionAndRotation(new Vector3(s.HaloAt.x, s.HaloZ, s.HaloAt.y), piece.rotation);
            t.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off; }
        }

        /// <summary>Brief 061: the head's SignalHead, its cycle from the export (absent: the export's state stands) and its parts by name. The first
        /// head of a crossing drives the crossing's dot rows when they are live (`dotsBy` given only to it).</summary>
        static void Head(Transform piece, StreetItemDto s, CrossingDto c, Dictionary<string, List<(Renderer r, StreetItemDto s)>> dotsBy, Material kitLight, Material glowLight)
        {
            var root = piece.gameObject; if (!root.TryGetComponent<Suborned.Play.SignalHead>(out var h)) h = root.AddComponent<Suborned.Play.SignalHead>(); // never `GetComponent() ??`: the Editor's fake null is not C# null
            h.Crossing = s.Crossing ?? ""; h.BuildState = s.Signal ?? "walk";
            h.HasCycle = c?.Cycle != null && c.Cycle.Walk + c.Cycle.Clearing + c.Cycle.Stop > 0;
            if (h.HasCycle) { h.Walk = c.Cycle.Walk; h.Clearing = c.Cycle.Clearing; h.Stop = c.Cycle.Stop; h.Arrive = c.Cycle.Arrive; h.Offset = c.Cycle.Offset; }
            h.Rim = h.Figure = h.Tens = h.Units = h.Ring = h.Beam = h.Halo = null;
            foreach (var r in piece.GetComponentsInChildren<MeshRenderer>(true))
            {
                var part = KitLods.Part(r.name); // the first LOD found of each part
                switch (part)
                {
                    case LensRim: if (!h.Rim) h.Rim = r; break;
                    case LensFigure: if (!h.Figure) h.Figure = r; break;
                    case LensTens: if (!h.Tens) h.Tens = r; break;
                    case LensUnits: if (!h.Units) h.Units = r; break;
                    case LensBeam: if (!h.Beam) h.Beam = r; break;
                    case LensHalo: if (!h.Halo) h.Halo = r; break;
                    case Ring: if (!h.Ring) h.Ring = r; break;
                }
            }
            var dots = new List<Renderer>(); var from = new List<float>(); var len = new List<float>();
            if (c != null && dotsBy != null && dotsBy.TryGetValue(c.Id, out var dl) && c.Line.Length == 2)
            {
                var a = c.Line[0]; var ab = c.Line[1] - a; float L = ab.magnitude; var dir = L > 1e-4f ? ab / L : Vector2.right; h.CrossLength = L;
                var dotsMat = DotsMaterial();
                foreach (var (r, ds) in dl)
                {
                    float centre = Vector2.Dot(ds.At - a, dir), m = Mathf.Max(0.01f, ds.Size.x); // a module's centre along the crossing, its length; u runs from its -X end
                    // only the dots' light slot is gated; the charcoal seat keeps its own enamel (the gate's dark dot is that seat's colour)
                    var ms = r.sharedMaterials; bool any = false; for (int i = 0; i < ms.Length; i++) if (ms[i] == kitLight || ms[i] == glowLight || ms[i] == dotsMat) { ms[i] = dotsMat; any = true; }
                    if (!any) throw new InvalidDataException($"World: {ds.Id}: a live dot row without its light slot ({Light})");
                    r.sharedMaterials = ms; PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    // u runs along the piece's +X: a module laid the other way round mirrors about the crossing's middle (SignalHead's lit window is symmetric about it)
                    float yaw = ds.Kit != null && !float.IsNaN(ds.Kit.Yaw) ? ds.Kit.Yaw : ds.Yaw; bool reversed = Vector2.Dot(new Vector2(Mathf.Cos(yaw), Mathf.Sin(yaw)), dir) < 0f;
                    float f0 = centre - m * 0.5f;
                    dots.Add(r); from.Add(reversed ? L - f0 - m : f0); len.Add(m);
                }
            }
            h.Dots = dots.ToArray(); h.DotFrom = from.ToArray(); h.DotLength = len.ToArray();
            EditorUtility.SetDirty(h); PrefabUtility.RecordPrefabInstancePropertyModifications(h);
        }

        static Material dotsMat;
        /// <summary>The dot rows' gated emission material (Shaders/SignalDots), made once a build and saved beside the scene.</summary>
        static Material DotsMaterial()
        {
            if (dotsMat) return dotsMat;
            var sh = Shader.Find("Suborned/Signal Dots") ?? throw new InvalidDataException("World: shader Suborned/Signal Dots missing (Shaders/SignalDots.shader)");
            dotsMat = new Material(sh) { name = "DK_Cross_Dots_Gated" };
            var path = Path.ChangeExtension(HarbourWorldBuilder.ScenePath, null) + "-crossdots.mat";
            AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(dotsMat, path);
            return dotsMat;
        }

        static bool eyeKind(StreetItemDto s) => s.Kind == "signal-orb";
        static Material lightMat;
        /// <summary>The emission-only seafoam light, made once a build (the street layer's bake asks for it first, the orbs' dress after).</summary>
        public static Material LightMaterial()
        {
            if (lightMat) return lightMat;
            var kit = KitImport.Library(Light) ?? throw new InvalidDataException($"World: kit material {Light} missing (run `bun run kit`)");
            return lightMat = Lens(kit);
        }

        /// <summary>The seafoam light on the crossings with no signal, from the kit row's colour; saved beside the scene. Brief 079: Signal Dots, not URP Lit (the row's lens and its
        /// spill share one slot, and Lit would draw the spill as an opaque ring on the road); the shader's defaults, wide gate, always lit.</summary>
        static Material Lens(Material kit)
        {
            var sh = Shader.Find("Suborned/Signal Dots") ?? throw new InvalidDataException("World: shader Suborned/Signal Dots missing (Shaders/SignalDots.shader)");
            var c = kit.GetColor("_BaseColor").linear * LightGlow;
            var m = new Material(sh) { name = Light + "_Glow" };
            m.SetColor("_EmissionColor", new Color(c.r, c.g, c.b, 1f));
            var path = Path.ChangeExtension(HarbourWorldBuilder.ScenePath, null) + "-crosslight.mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Brief 075: the boarding island (transit.stops) or kerbed island (ground.islands) under a plan point, else null.</summary>
        public static MassDto IslandUnder(WorldDto w, Vector2 p) =>
            w.Transit.Stops.Find(m => Poly.Contains(m.Ring, p)) ?? w.Ground.Islands.Find(m => Poly.Contains(m.Ring, p));

        static Vector2 Centre(Vector2[] ring)
        {
            var c = Vector2.zero;
            foreach (var p in ring) c += p;
            return c / ring.Length;
        }
    }
}
