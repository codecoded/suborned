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
    /// meshes, switching at `lods[i].screen` (else the piece's `lodCategory` switch point), the last level never culled. Every LOD
    /// material is the library material of its slot's name (Kit/Materials); an unmapped slot = reject, and a LOD slot the LOD0 does not
    /// list (13 older pieces carry DK_Beacon_Rose on their LODs only) is mapped the same way and counted. A multi-part piece (the SignalEye)
    /// puts each level's parts under one `__LOD<i>` child, each named as the LOD0 part it stands for.</summary>
    public static class KitLods
    {
        const string Prefix = "__LOD";

        public static int Groups, Levels, Colliders, ConcaveMesh, ConvexMesh, Boxes, OtherColliders, ExtraSlots, OldLodGroups, MissingPrefabs, MissingScripts;
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
            Groups = Levels = Colliders = ConcaveMesh = ConvexMesh = Boxes = OtherColliders = ExtraSlots = OldLodGroups = MissingPrefabs = MissingScripts = 0; ExtraSlotPieces.Clear();
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
                    Lod(id, go, kit.Lods[id]);
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
            Debug.Log($"World: kit built {written.Count} prefabs: {Groups} LODGroups ({Levels} LOD levels beyond LOD0, no cull level); colliders stripped {Colliders} " +
                $"(concave mesh {ConcaveMesh}, convex mesh {ConvexMesh}, box {Boxes}, other {OtherColliders}); LOD slots the LOD0 does not list {ExtraSlots} " +
                $"({string.Join(", ", ExtraSlotPieces)}), all mapped; " +
                $"removed from the sources: {OldLodGroups} old LODGroups, {MissingPrefabs} missing nested prefabs, {MissingScripts} missing scripts");
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

        static void Lod(string id, GameObject go, List<(string file, float screen)> lods)
        {
            if (lods.Count == 0) return;
            var r0 = Lod0(go).ToArray();
            if (r0.Length == 0) throw new InvalidDataException($"World: kit `{id}` has no renderers for LOD0");
            // a multi-part piece (brief 009: the SignalEye's body, Walk, Stop, Beam and Halo are sibling objects Unity switches and
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
                if (lr.Length != r0.Length) throw new InvalidDataException($"World: kit `{id}` LOD{i + 1} {file} has {lr.Length} renderers, LOD0 {r0.Length}");
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
            levels.Add(new LOD(0f, previous)); // the last level is never culled
            var lg = go.AddComponent<LODGroup>();
            lg.fadeMode = LODFadeMode.None;
            lg.SetLODs(levels.ToArray());
            lg.RecalculateBounds();
            Groups++;
        }

        /// <summary>A part's name without its LOD tag or Blender's duplicate suffix (`DK_SignalEye_L1_Beam`, `DK_SignalEye_Beam.001` → `DK_SignalEye_Beam`).</summary>
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
