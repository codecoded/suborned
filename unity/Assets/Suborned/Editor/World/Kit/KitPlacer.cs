using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Places DecoKit prefabs by the export's kit refs. Prefab paths come from contracts/world/kit.json (the `prefab` file of
    /// each adopted piece, under Assets/Suborned/Kit/); an unknown id or a missing prefab = reject. Pieces stay prefab instances, so
    /// the scene references the kit's meshes and materials instead of copying them, and URP's GPU Resident Drawer (PC_RPAsset,
    /// Forward+, BRG variants kept) draws repeats instanced. Each placed piece is counted per layer for Inspect.</summary>
    public sealed class KitPlacer
    {
        public const string KitRoot = "Assets/Suborned/Kit/";
        /// <summary>The prefabs the builder places (KitLods writes them from the sources at kit import: colliders stripped, LODGroup from the catalogue).</summary>
        public const string BuiltRoot = KitRoot + "Prefabs/Built/";
        readonly Dictionary<string, string> prefabs = new(), sources = new(), generated = new();
        readonly Dictionary<string, List<(string file, float screen)>> lods = new();
        readonly List<(string, string)> uses = new();
        readonly Dictionary<string, GameObject> loaded = new();
        readonly Dictionary<string, int> placed = new();
        readonly Dictionary<string, int> tris = new();

        public KitPlacer(string catalogue)
        {
            var root = Json.Parse(File.ReadAllText(catalogue));
            var categories = new Dictionary<string, (float l1, float l2)>();
            if (J.Get(root, "lodCategories") is Dictionary<string, object> cats)
                foreach (var kv in cats) categories[kv.Key] = (J.F(kv.Value, "l1"), J.F(kv.Value, "l2"));
            foreach (var a in J.A(root, "assets"))
            {
                if (J.S(a, "status") != "adopted") continue;
                var id = J.S(a, "id");
                foreach (var f in J.A(a, "files"))
                    if (J.S(f, "role") == "prefab") sources[id] = KitRoot + J.S(f, "path");
                    else if (J.S(a, "source") == "generated" && J.S(f, "role") == "fbx")
                    {   // the generator ships the FBX alone: KitImport.Generated builds its prefab here
                        var fbx = KitRoot + J.S(f, "path");
                        generated[id] = fbx; sources[id] = KitRoot + "Prefabs/Generated/" + Path.GetFileNameWithoutExtension(fbx) + ".prefab";
                    }
                if (!sources.TryGetValue(id, out var src)) throw new InvalidDataException($"World: kit `{id}` is adopted but has neither a prefab nor a generated fbx");
                prefabs[id] = BuiltRoot + Path.GetFileName(src);
                // LOD levels: lods[i].screen, else the category's switch point (l1 for LOD1, l2 for LOD2)
                var l = new List<(string, float)>();
                var cat = J.S(a, "lodCategory");
                if (J.Get(a, "lods") is List<object> ls)
                    for (int i = 0; i < ls.Count; i++)
                    {
                        float screen = J.Get(ls[i], "screen") is double d ? (float)d
                            : cat != null && categories.TryGetValue(cat, out var c) ? (i == 0 ? c.l1 : c.l2)
                            : throw new InvalidDataException($"World: kit `{id}` LOD{i + 1} has no screen and no lodCategory switch");
                        l.Add((KitRoot + J.S(ls[i], "file"), screen));
                    }
                lods[id] = l;
                if (J.Get(a, "tags") is { } tags && J.Get(tags, "use") is List<object> use) foreach (var u in use) uses.Add(((string)u, id));
            }
        }

        /// <summary>id → the prefab the builder places (under Prefabs/Built/).</summary>
        public IReadOnlyDictionary<string, string> Prefabs => prefabs;
        /// <summary>id → the source prefab (`bun run kit`'s copy, or the one KitImport.Generated builds from a generated FBX).</summary>
        public IReadOnlyDictionary<string, string> Sources => sources;
        /// <summary>id → LOD1, LOD2 files (asset paths) with their screen-relative switch heights (LOD0 = the source).</summary>
        public IReadOnlyDictionary<string, List<(string file, float screen)>> Lods => lods;
        /// <summary>Generated pieces: id → FBX asset path.</summary>
        public IReadOnlyDictionary<string, string> Generated => generated;
        /// <summary>Every adopted piece's `tags.use` entries (e.g. `site:harbour-hotel`, `landmark:skyport-tower`), in catalogue order.</summary>
        public IReadOnlyList<(string use, string id)> Uses => uses;

        /// <summary>Pieces placed under each layer root.</summary>
        public int Placed(string layer) => placed.TryGetValue(layer, out var n) ? n : 0;

        /// <summary>The placed prefab (Built/). Source(id) = the source prefab.</summary>
        public GameObject Prefab(string id) => Load(id, prefabs);
        public GameObject Source(string id) => Load(id, sources);

        GameObject Load(string id, Dictionary<string, string> paths)
        {
            if (paths == prefabs && loaded.TryGetValue(id, out var go)) return go;
            if (!paths.TryGetValue(id, out var path)) throw new InvalidDataException($"World: kit `{id}` is not adopted in contracts/world/kit.json");
            go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!go && File.Exists(Path.Combine(HarbourWorldBuilder.RepoRoot, "unity", path)))
            {   // copied by `bun run kit` while the Editor was not refreshing: import it now
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            if (!go) throw new FileNotFoundException($"World: kit `{id}` prefab missing at {path} (run `bun run kit`; Built/ is written by KitLods at kit import)");
            if (paths == prefabs) loaded[id] = go;
            return go;
        }

        readonly Dictionary<string, Bounds> local = new();

        /// <summary>The prefab's mesh bounds in its own frame (scale 1, unrotated, pivot at the origin).</summary>
        public Bounds LocalBounds(string id)
        {
            if (local.TryGetValue(id, out var have)) return have;
            var root = Prefab(id).transform; Bounds? b = null;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.sharedMesh || !KitLods.IsLod0(mf.transform, root)) continue;
                var m = root.worldToLocalMatrix * mf.transform.localToWorldMatrix; var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var p = m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (b is { } h) { h.Encapsulate(p); b = h; } else b = new Bounds(p, Vector3.zero);
                }
            }
            return local[id] = b ?? throw new InvalidDataException($"World: kit `{id}` has no meshes");
        }

        /// <summary>Unity rotation of a ref at plan yaw θ (+X turns to (cos θ, sin θ)).</summary>
        public static Quaternion Rotation(float yaw) => Quaternion.Euler(0f, -yaw * Mathf.Rad2Deg, 0f);

        /// <summary>The pivot (plan) that puts the piece's footprint centre (its scaled, turned mesh bounds centre) on `centre`: facade
        /// pieces are modelled with the pivot on the facade plane, the export gives their footprint centre.</summary>
        public Vector2 PivotForCentre(KitRefDto r, Vector2 centre, float elementYaw = 0f)
        {
            var c = LocalBounds(r.Id).center; c.y = 0f;
            var o = Rotation(float.IsNaN(r.Yaw) ? elementYaw : r.Yaw) * Vector3.Scale(c, r.Scale);
            return centre - new Vector2(o.x, o.z);
        }

        /// <summary>Pivot height that puts the piece's lowest point on `z` (pieces modelled about their centre, e.g. the SkyBridge).</summary>
        public float PivotForLowest(KitRefDto r, float z) => z - LocalBounds(r.Id).min.y * r.Scale.y;

        /// <summary>Top of the piece (its highest point) when its pivot stands at z.</summary>
        public float Top(KitRefDto r, float z) => z + LocalBounds(r.Id).max.y * r.Scale.y;

        /// <summary>A piece at scale 1 by id (stand-ins and generated pieces carry no export ref).</summary>
        public Transform Place(Transform el, string layer, string id, Vector2 at, float z, float yaw) =>
            Place(el, layer, new KitRefDto { Id = id, Scale = Vector3.one, Yaw = yaw }, at, z, yaw);

        /// <summary>Instance of r under element el: pivot (footprint centre, ground) at plan `at`, height z; plan yaw θ turns +X to
        /// (cos θ, sin θ), i.e. Unity yaw -θ; scale = (across, up, depth).</summary>
        public Transform Place(Transform el, string layer, KitRefDto r, Vector2 at, float z, float elementYaw)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(r.Id), el);
            var t = go.transform;
            float yaw = float.IsNaN(r.Yaw) ? elementYaw : r.Yaw;
            t.SetPositionAndRotation(new Vector3(at.x, z, at.y), Quaternion.Euler(0f, -yaw * Mathf.Rad2Deg, 0f));
            t.localScale = r.Scale;
            Bounds? b = null; int n = 0;
            foreach (var mr in KitLods.Lod0(go))
            {
                b = b is { } have ? Union(have, mr.bounds) : mr.bounds;
                var mesh = mr.GetComponent<MeshFilter>().sharedMesh;
                for (int i = 0; i < mesh.subMeshCount; i++) n += (int)mesh.GetIndexCount(i) / 3;
            }
            if (b == null) throw new InvalidDataException($"World: kit `{r.Id}` has no renderers");
            Combine.Kit(el, b.Value, n);
            placed[layer] = Placed(layer) + 1;
            tris[layer] = (tris.TryGetValue(layer, out var k) ? k : 0) + n;
            return t;
        }

        /// <summary>The piece r baked into `layer`'s tile batches by material instead of standing as an instance (its LOD0, placed as
        /// Place places it): many small pieces that never fade (bed tufts, paving bands, bollards) then draw as a few batches.
        /// slots: this piece's material slots drawn in another kit material (KitSlots).</summary>
        public void Bake(Transform el, string layer, string id, KitRefDto r, Vector2 at, float z, IReadOnlyDictionary<string, string> slots)
        {
            var root = Prefab(r.Id).transform; var used = new HashSet<string>(); int n = 0;
            var trs = Matrix4x4.TRS(new Vector3(at.x, z, at.y), Quaternion.Euler(0f, -(float.IsNaN(r.Yaw) ? 0f : r.Yaw) * Mathf.Rad2Deg, 0f), r.Scale) * root.worldToLocalMatrix;
            foreach (var mr in KitLods.Lod0(root.gameObject))
            {
                var mesh = mr.GetComponent<MeshFilter>().sharedMesh; var mats = mr.sharedMaterials;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var kit = new MeshKit().Mesh(mesh, s, trs * mr.transform.localToWorldMatrix);
                    if (kit.Empty) continue;
                    Combine.Baked(el, layer, kit, KitSlots.For(mats[Mathf.Min(s, mats.Length - 1)], slots, used)); n += kit.TriangleCount;
                }
            }
            if (n == 0) throw new InvalidDataException($"World: kit `{r.Id}` has no meshes");
            KitSlots.AllUsed(id, slots, used);
            placed[layer] = Placed(layer) + 1;
            tris[layer] = (tris.TryGetValue(layer, out var k) ? k : 0) + n;
        }

        /// <summary>Blade and wall signs (`signKit`) on the frontage markers ShopKit built: the piece at its At, lowest point at its Base.</summary>
        public void Signs(Transform layer, List<FrontageDto> frontages)
        {
            var byId = new Dictionary<string, Transform>(System.StringComparer.Ordinal);
            foreach (Transform c in layer) byId[c.name] = c;
            foreach (var f in frontages)
                if (f.SignKit != null)
                    Place(byId.TryGetValue(f.Id, out var el) ? el : throw new InvalidDataException($"World: frontage {f.Id} not built"),
                        layer.name, f.SignKit, f.SignKit.At, f.SignKit.Base, f.Yaw);
        }

        public string Summary()
        {
            var sb = new System.Text.StringBuilder("World: kit placed");
            foreach (var kv in placed) sb.Append($" {kv.Key} {kv.Value} ({tris[kv.Key]} tris)");
            return sb.ToString();
        }

        static Bounds Union(Bounds a, Bounds b) { a.Encapsulate(b); return a; }
    }
}
