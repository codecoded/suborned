using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World
{
    /// <summary>Kit import, second step (brief 007 round 3): every adopted piece's placed prefab under Prefabs/Built/, written from its
    /// source prefab on every build. Colliders are stripped (the sim never queries Unity physics). A piece with distance versions gets
    /// one LODGroup: LOD0 = the source's renderers, LOD1 and LOD2 = children `__LOD1`, `__LOD2` carrying the catalogue's `lods[i].file`
    /// meshes, switching at `lods[i].screen` (else the piece's `lodCategory` switch point), the last level culled (brief 048: below 1 % of the screen under 8 m, 0.4 % above). Every LOD
    /// material is the library material of its slot's name (Kit/Materials); an unmapped slot = reject, and a LOD slot the LOD0 does not
    /// list (13 older pieces carry DK_Beacon_Rose on their LODs only) is mapped the same way and counted. A multi-part piece (the SignalLens)
    /// puts each level's parts under one `__LOD<i>` child, each named as the LOD0 part it stands for.</summary>
    public static class KitLods
    {
        const string Prefix = "__LOD";

        public static int RobotFaces, Groups, Levels, Colliders, ConcaveMesh, ConvexMesh, Boxes, OtherColliders, ExtraSlots, OldLodGroups, MissingPrefabs, MissingScripts;
        public static readonly List<string> ExtraSlotPieces = new();

        /// <summary>t is part of LOD0 (not under a `__LOD` child of the piece's root).</summary>
        public static bool IsLod0(Transform t, Transform root)
        {
            for (var p = t; p && p != root; p = p.parent) if (p.name.StartsWith(Prefix)) return false;
            return true;
        }

        /// <summary>The LOD0 renderers of a placed piece (or the prefab itself).</summary>
        public static IEnumerable<MeshRenderer> Lod0(GameObject go) =>
            go.GetComponentsInChildren<MeshRenderer>(true).Where(r => IsLod0(r.transform, go.transform));

        public static void Build(KitPlacer kit)
        {
            CullSmallCount = CullOnly = LevelsBelowCull = Classed = 0; RobotFaces = Groups = Levels = Colliders = ConcaveMesh = ConvexMesh = Boxes = OtherColliders = ExtraSlots = OldLodGroups = MissingPrefabs = MissingScripts = 0; ExtraSlotPieces.Clear();
            Folder(KitPlacer.BuiltRoot.TrimEnd('/'));
            var written = new Dictionary<string, string>(System.StringComparer.Ordinal); // built path → id
            foreach (var id in kit.Prefabs.Keys.OrderBy(k => k, System.StringComparer.Ordinal))
            {
                var path = kit.Prefabs[id];
                if (written.TryGetValue(path, out var other))
                {
                    if (kit.Sources[other] != kit.Sources[id] || !SameLods(kit.Lods[other], kit.Lods[id]))
                        throw new InvalidDataException($"World: kit `{id}` and `{other}` would both build {path} from different sources");
                    continue;
                }
                written[path] = id;
                var go = Object.Instantiate(kit.Source(id));
                go.name = Path.GetFileNameWithoutExtension(path);
                try
                {
                    Strip(go);
                    Lod(id, kit.Categories.TryGetValue(id, out var cat) ? cat : "", go, kit.Lods[id], kit.LodClass.TryGetValue(id, out var lc) ? lc.distances : null);
                    if (RobotKit.Rig(go, id)) RobotFaces++; // brief 026: a service robot's face and head, from robots.json (view only)
                    if (!PrefabUtility.SaveAsPrefabAsset(go, path)) throw new IOException($"World: could not save {path}");
                }
                finally { Object.DestroyImmediate(go); }
            }
            // stale built prefabs (a piece no longer adopted) go
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { KitPlacer.BuiltRoot.TrimEnd('/') }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!written.ContainsKey(p)) AssetDatabase.DeleteAsset(p);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"World: kit built {written.Count} prefabs: {Groups} LODGroups ({Levels} LOD levels beyond LOD0; a cull level on each: {CullSmallCount} small pieces below {CullSmall:P0} of the screen or half their commander-view height, the rest below {CullLarge:P1}; {CullOnly} cull-only, {LevelsBelowCull} levels below their cull dropped); colliders stripped {Colliders} " +
                $"(concave mesh {ConcaveMesh}, convex mesh {ConvexMesh}, box {Boxes}, other {OtherColliders}); LOD slots the LOD0 does not list {ExtraSlots} " +
                $"({string.Join(", ", ExtraSlotPieces)}), all mapped; " +
                $"{Classed} switched by distance class (kit.json lodClasses, follow view {FollowFov}°); removed from the sources: {OldLodGroups} old LODGroups, {MissingPrefabs} missing nested prefabs, {MissingScripts} missing scripts; robot faces rigged {RobotFaces}");
        }

        static bool SameLods(List<(string file, float screen)> a, List<(string file, float screen)> b) => a.Count == b.Count && a.Zip(b, (x, y) => x == y).All(v => v);

        static void Strip(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
            {
                Colliders++;
                switch (c)
                {
                    case MeshCollider mc when !mc.convex: ConcaveMesh++; break;
                    case MeshCollider: ConvexMesh++; break;
                    case BoxCollider: Boxes++; break;
                    default: OtherColliders++; break;
                }
                Object.DestroyImmediate(c);
            }
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            // the old project's own LOD wiring goes (the catalogue's LODs replace it), with its leftovers: a nested prefab `bun run kit`
            // does not copy (DK_RobotButler_Far) and scripts this project does not have
            foreach (var lg in go.GetComponentsInChildren<LODGroup>(true)) { OldLodGroups++; Object.DestroyImmediate(lg); }
            foreach (var t in go.GetComponentsInChildren<Transform>(true).Reverse().ToList())
            {
                if (!t) continue;
                if (t != go.transform && (PrefabUtility.IsPrefabAssetMissing(t.gameObject) || t.name.Contains("(Missing Prefab")))
                { MissingPrefabs++; Object.DestroyImmediate(t.gameObject); continue; }
                MissingScripts += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
        }

        public static int DroppedParts, CullSmallCount, CullOnly, LevelsBelowCull;
        /// <summary>Brief 048: the cull level (screen-relative height) for a piece under CullSmallM metres, and for a larger one.
        /// CommanderHalfMax is the widest play zoom (HostCameraLab.zoomMax, the orthographic half-height): no cull may come within half
        /// of the smallest height a piece has there, so the commander view never loses a piece.</summary>
        const float CullSmallM = 8f, CullSmall = 0.01f, CullLarge = 0.004f, CommanderHalfMax = 18.5f;
        /// <summary>Brief 072 (Redline's plan 3.1): the follow view's vertical FOV the distance classes are set against, and the play-zoom guard:
        /// no L1 switch nearer than GuardM, so the orthographic play camera (relative height = size / (2 · ortho size), at most zoom 18.5,
        /// equal to perspective at 18.5 / tan 27.5° = 35.5 m) always draws L0.</summary>
        public const float FollowFov = 55f, GuardM = 36f;
        static float K => 2f * Mathf.Tan(FollowFov * 0.5f * Mathf.Deg2Rad); // 1.041
        /// <summary>The screen-relative height at which a group of size s (m) switches at distance d (m), lodBias 1.</summary>
        public static float ScreenAt(float s, float d) => s / (K * d);
        /// <summary>The distance at which a group of size s switches at screen-relative height h.</summary>
        public static float DistanceAt(float s, float h) => s / (K * h);
        public static int Classed;

        static void Lod(string id, string category, GameObject go, List<(string file, float screen)> lods, float[] dist = null)
        {
            var r0 = Lod0(go).ToArray();
            if (r0.Length == 0) { if (lods.Count == 0) return; throw new InvalidDataException($"World: kit `{id}` has no renderers for LOD0"); }
            var bounds = new Bounds(go.transform.position, Vector3.zero); foreach (var r in r0) bounds.Encapsulate(r.bounds);
            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            bool small = size < CullSmallM;
            if (dist != null)
            {   // brief 072: switch by distance from the piece's own unscaled size (the LODGroup's size is its bounds' largest side), not the catalogue's screens
                if (dist.Length < lods.Count) throw new InvalidDataException($"World: kit `{id}` has {lods.Count} LODs and its class {dist.Length} distances");
                lods = lods.Select((l, i) => (l.file, ScreenAt(size, dist[i]))).ToList();
                Classed++;
            }
            // brief 048 (Redline's plan item 3): a small piece with no distance versions (planters, pots, robots, bike racks, kiosks' parts)
            // gets a cull-only group too; ground pieces never (a culled tile is a hole), nor a large piece (it would cull past the clip anyway)
            if (lods.Count == 0 && (!small || category == "Ground")) return;
            float cull = Mathf.Min(small ? CullSmall : CullLarge, 0.5f * size / (2f * CommanderHalfMax));
            // the class's cull distance (330 m for modules): a class is always [L1, L2, cull], so the cull is distances[2] whatever number of LODs the
            // piece has (Splice, 072: distances[lods.Count] read a one-LOD crown's L2 400 m as its cull); 0 = the default cull above
            if (dist is { Length: >= 3 } && dist[2] > 0f) cull = ScreenAt(size, dist[2]);
            // a level that would only switch in below the cull never shows: it goes (the street category's L2 at 0.003 had pulled the cull
            // down to 0.15 %, about 500 m for a bollard, so small pieces still drew to the 350 m clip)
            var kept = lods.Where(l => l.screen > cull).ToList(); LevelsBelowCull += lods.Count - kept.Count; lods = kept;
            if (lods.Count == 0) CullOnly++;
            // a multi-part piece (brief 061: the SignalLens's body, rim, figure, digits, beam and halo are sibling objects Unity drives and
            // lights apart) matches each LOD file renderer to its LOD0 part by name (its `_L1` tag and Blender's `.001` suffix dropped)
            bool parts = r0.Length > 1;
            var byPart = new Dictionary<string, MeshRenderer>(System.StringComparer.Ordinal);
            foreach (var r in r0) if (!byPart.TryAdd(Part(r.name), r)) throw new InvalidDataException($"World: kit `{id}` has two LOD0 parts named {Part(r.name)}");
            var levels = new List<LOD>();
            Renderer[] previous = r0; float prevScreen = 1f;
            for (int i = 0; i < lods.Count; i++)
            {
                var (file, screen) = lods[i];
                if (!(screen > 0f && screen < prevScreen)) throw new InvalidDataException($"World: kit `{id}` LOD{i + 1} switch {screen} is not below LOD{i}'s {prevScreen}");
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                if (!asset)
                {
                    AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceSynchronousImport);
                    asset = AssetDatabase.LoadAssetAtPath<GameObject>(file) ?? throw new FileNotFoundException($"World: kit `{id}` LOD{i + 1} missing at {file} (run `bun run kit`)");
                }
                var lr = asset.GetComponentsInChildren<MeshRenderer>(true);
                // a multi-part piece's far LOD may drop parts (the restored porter's L1 keeps 3 of 6): each one left still maps to its LOD0 part by
                // name below, so nothing stands in the wrong place; more renderers than LOD0, or a single-part piece that splits, still fails
                if (lr.Length > r0.Length || (!parts && lr.Length != r0.Length)) throw new InvalidDataException($"World: kit `{id}` LOD{i + 1} {file} has {lr.Length} renderers, LOD0 {r0.Length}");
                if (lr.Length < r0.Length) { Debug.Log($"World: kit `{id}` LOD{i + 1} keeps {lr.Length} of {r0.Length} parts"); DroppedParts++; }
                Transform holder = go.transform;
                if (parts) { holder = new GameObject(Prefix + (i + 1)).transform; holder.SetParent(go.transform, false); }
                var level = new List<Renderer>();
                foreach (var l in lr)
                {
                    var src = parts ? (byPart.TryGetValue(Part(l.name), out var s0) ? s0 : throw new InvalidDataException($"World: kit `{id}` LOD{i + 1} part {l.name} has no LOD0 part"))
                                    : r0[0];
                    level.Add(Level(id, i, file, go, holder, parts ? Part(l.name) : Prefix + (i + 1), src, l));
                }
                levels.Add(new LOD(screen, previous));
                previous = level.ToArray(); prevScreen = screen;
                Levels++;
            }
            // brief 048 (Redline's plan item 3, measured 6,081 to 3,882 renderers drawn in tp-worst, 14 % faster): the last level culls instead of
            // drawing to the horizon. A piece under 8 m (crossings, bollards, benches, signs) culls below 1 % of the screen's height (a 0.8 m
            // bollard at about 80 m in third person, a 2 m bench at about 190 m), a larger one below 0.4 %; the commander view is untouched (above).
            levels.Add(new LOD(cull, previous));
            var lg = go.AddComponent<LODGroup>();
            lg.fadeMode = LODFadeMode.None;
            lg.SetLODs(levels.ToArray());
            lg.RecalculateBounds();
            Groups++; if (small) CullSmallCount++;
        }

        /// <summary>Brief 072 build check (Redline's plan 3.1): the L1 switch distance of every placed LODGroup under the world root, from its world
        /// size (LODGroup.size × the largest lossy scale, as Unity measures it). Under the layers that hold buildings (facades, civic,
        /// signatures, landmarks, megastructures, frontages) and the roof crowns (roofs) one nearer than GuardM fails the build: the play camera would
        /// draw it below L0. Elsewhere (street props, trees) it is counted and logged as a warning. Returns (groups checked, nearest L1 distance, failures).</summary>
        public static (int groups, float nearest, string nearestName, int failed, int otherBelow) PlayZoomGuard(Transform root)
        {
            // `roofs` (Redline phase 2 F2): the roof crowns have their own `crown` class (L1 at 45 m) since 072, so a crown below L0 at play zoom fails too
            var strict = new HashSet<string>(System.StringComparer.Ordinal) { "facades", "civic", "signatures", "landmarks", "megastructures", "frontages", "roofs" };
            int n = 0, failed = 0, other = 0; float nearest = float.PositiveInfinity; string nearestName = "";
            var bad = new List<string>(); var below = new SortedDictionary<string, (int n, float d)>(System.StringComparer.Ordinal);
            foreach (var g in root.GetComponentsInChildren<LODGroup>(true))
            {
                var lods = g.GetLODs(); if (lods.Length < 2) continue; // a cull-only group never draws below L0
                var t = g.transform; while (t.parent && t.parent != root) t = t.parent;
                if (t.name == "horizon") continue; // the sky bodies draw L0 always; the band is 1–8 km out
                var s = g.transform.lossyScale; float size = g.size * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                // Unity switches at size · lodBias / (K · d), so the distance grows with the bias (Redline phase 2 F6: it divided; same at bias 1)
                float d = DistanceAt(size, lods[0].screenRelativeTransitionHeight / QualitySettings.lodBias);
                n++;
                bool isStrict = strict.Contains(t.name);
                if (isStrict && d < nearest) { nearest = d; nearestName = g.name; }
                if (d >= GuardM) continue;
                if (isStrict) { failed++; if (bad.Count < 12) bad.Add($"{t.name}/{g.name} L1 at {d:F1} m"); }
                else { other++; var k = $"{t.name}/{g.name}"; below[k] = below.TryGetValue(k, out var e) ? (e.n + 1, Mathf.Min(e.d, d)) : (1, d); }
            }
            // pieces outside the strict layers (street props, trees) that the play camera draws below L0: reported, not failed
            if (below.Count > 0) Debug.LogWarning($"World: play-zoom guard: {other} groups outside the facade layers switch to L1 nearer than {GuardM} m (drawn below L0 at the widest play zoom): " +
                string.Join("; ", below.Select(kv => $"{kv.Key} x{kv.Value.n} from {kv.Value.d:F1} m")));
            if (failed > 0) throw new InvalidDataException($"World: play-zoom guard: {failed} LOD groups switch to L1 nearer than {GuardM} m (the play camera would draw them below L0): {string.Join("; ", bad)}");
            return (n, nearest, nearestName, failed, other);
        }

        /// <summary>A part's name without its LOD tag or Blender's duplicate suffix (`DK_SignalLens_L1_Beam`, `DK_SignalLens_Beam.001` → `DK_SignalLens_Beam`).</summary>
        public static string Part(string name) => System.Text.RegularExpressions.Regex.Replace(System.Text.RegularExpressions.Regex.Replace(name, @"\.\d{3}$", ""), @"_L\d+(?=_|$)", "");

        /// <summary>One LOD renderer: the LOD file's mesh `l` standing where its LOD0 part `src` stands, with `src`'s render settings.</summary>
        static MeshRenderer Level(string id, int i, string file, GameObject go, Transform holder, string name, MeshRenderer src, MeshRenderer l)
        {
            var srcMesh = src.GetComponent<MeshFilter>().sharedMesh;
            var srcNames = new HashSet<string>(src.sharedMaterials.Where(m => m).Select(m => m.name), System.StringComparer.Ordinal);
            var mesh = l.GetComponent<MeshFilter>().sharedMesh ?? throw new InvalidDataException($"World: kit `{id}` LOD{i + 1} {file} has no mesh");
            var mats = new Material[l.sharedMaterials.Length];
            for (int k = 0; k < mats.Length; k++)
            {
                var mn = l.sharedMaterials[k] ? l.sharedMaterials[k].name : null;
                var lib = mn != null ? KitImport.Library(mn) : null;
                if (!lib) throw new InvalidDataException($"World: kit `{id}` LOD{i + 1} slot {k} material `{mn}` not in {KitPlacer.KitRoot}Materials (unmapped)");
                if (!lib.shader || lib.shader.name == "Hidden/InternalErrorShader") throw new InvalidDataException($"World: kit `{id}` LOD{i + 1} material {mn} has a broken shader");
                if (!srcNames.Contains(mn)) { ExtraSlots++; if (!ExtraSlotPieces.Contains(id)) ExtraSlotPieces.Add(id); }
                mats[k] = lib;
            }
            if (mesh.subMeshCount != mats.Length) throw new InvalidDataException($"World: kit `{id}` LOD{i + 1} has {mesh.subMeshCount} submeshes and {mats.Length} materials");
            // a LOD must stand where LOD0 stands (same frame, same export)
            var d0 = srcMesh.bounds; var d1 = mesh.bounds;
            float tol = Mathf.Max(0.05f, d0.size.magnitude * 0.08f);
            if ((d0.center - d1.center).magnitude > tol || (d0.size - d1.size).magnitude > tol * 2f)
                Debug.LogWarning($"World: kit `{id}` LOD{i + 1} bounds {d1.center}/{d1.size} differ from LOD0 {d0.center}/{d0.size}");
            // the LOD child sits where the LOD0 mesh node sits relative to the root
            var rel = holder.worldToLocalMatrix * src.transform.localToWorldMatrix;
            var child = new GameObject(name);
            child.transform.SetParent(holder, false);
            child.transform.localPosition = rel.GetColumn(3);
            child.transform.localRotation = rel.rotation;
            child.transform.localScale = rel.lossyScale;
            GameObjectUtility.SetStaticEditorFlags(child, GameObjectUtility.GetStaticEditorFlags(src.gameObject));
            child.layer = src.gameObject.layer;
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = child.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = src.shadowCastingMode; mr.receiveShadows = src.receiveShadows;
            mr.lightProbeUsage = src.lightProbeUsage; mr.reflectionProbeUsage = src.reflectionProbeUsage;
            mr.motionVectorGenerationMode = src.motionVectorGenerationMode; mr.allowOcclusionWhenDynamic = src.allowOcclusionWhenDynamic;
            return mr;
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
