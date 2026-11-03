#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// FBX import settings + material remap onto the shared library, then one prefab variant per asset.
    /// Variants keep the link to the FBX, so re-running export_kit.py updates every placed prefab.
    internal static class DecoKitModels
    {
        static readonly HashSet<string> Walkable = new() { "Ground", "Structures", "Plaza", "Skyline", "Landmarks" };

        internal static List<(AssetRow row, GameObject prefab)> BuildAll(Dictionary<string, Material> lib)
        {
            var built = new List<(AssetRow, GameObject)>();
            foreach (var row in DecoKitData.Assets().assets)
            {
                var path = $"{Root}/{row.fbx}";
                if (!Configure(path, row, lib)) continue;
                var lod = !string.IsNullOrEmpty(row.lod1) && Configure($"{Root}/{row.lod1}", row, lib)
                    ? AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/{row.lod1}") : null;
                built.Add((row, SavePrefab(row, AssetDatabase.LoadAssetAtPath<GameObject>(path), lod)));
            }
            AssetDatabase.SaveAssets();
            return built;
        }

        static bool Configure(string path, AssetRow row, Dictionary<string, Material> lib)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter mi)
            {
                Debug.LogError($"[DecoKit] missing model {path}");
                return false;
            }
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.bakeAxisConversion = false;
            mi.importNormals = ModelImporterNormals.Import;      // bevel + weighted-normal shading from Blender
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.isReadable = false;
            mi.importBlendShapes = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importVisibility = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importAnimation = false;
            mi.generateSecondaryUV = Walkable.Contains(row.category);  // lightmap UVs where baking will matter
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            foreach (var name in row.materials)
            {
                if (lib.TryGetValue(name, out var mat))
                    mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), mat);
                else Debug.LogError($"[DecoKit] {row.name}: material {name} not in decokit_materials.json");
            }
            mi.SaveAndReimport();
            return true;
        }

        static GameObject SavePrefab(AssetRow row, GameObject model, GameObject lod1)
        {
            var path = PrefabPath(row);
            EnsureFolder(System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/'));
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                go.name = $"DK_{row.name}";
                if (lod1 != null) AddLods(go, lod1);
                AddColliders(go, row);
                DecoKitRobots.Apply(go, row.name);
                if (Walkable.Contains(row.category))
                    foreach (var t in go.GetComponentsInChildren<Transform>())
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.ContributeGI
                            | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic
                            | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                VerifyScale(go, row);
                return PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { Object.DestroyImmediate(go); }
        }

        /// Full detail until the asset is 10% of the screen tall (a 1.6 m robot ~15 m from a 55 degree camera), the
        /// decimated version to 1.2% (~130 m), then culled.
        static void AddLods(GameObject go, GameObject lod1)
        {
            var full = go.GetComponentsInChildren<Renderer>();
            var lo = (GameObject)PrefabUtility.InstantiatePrefab(lod1, go.transform);
            lo.name = lod1.name;
            go.AddComponent<LODGroup>().SetLODs(new[]
            {
                new LOD(.1f, full), new LOD(.012f, lo.GetComponentsInChildren<Renderer>()),
            });
        }

        /// Walkable kit gets exact mesh colliders; props get one box so crowds and the walker path around them.
        static void AddColliders(GameObject go, AssetRow row)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            if (Walkable.Contains(row.category))
            {
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                return;
            }
            var b = Bounds(go);
            var box = go.AddComponent<BoxCollider>();
            if (row.category == "Plants")  // trunk/pot only: a crown-sized box would wall off the plaza
            {
                var s = new Vector3(Mathf.Min(b.size.x, .6f), Mathf.Min(b.size.y, 2f), Mathf.Min(b.size.z, .6f));
                box.center = go.transform.InverseTransformPoint(new Vector3(b.center.x, b.min.y + s.y * .5f, b.center.z));
                box.size = s;
                return;
            }
            box.center = go.transform.InverseTransformPoint(b.center);
            box.size = b.size;
        }

        internal static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            return b;
        }

        /// Catches unit-scale or axis mistakes in the FBX round trip (Blender dims vs imported bounds).
        static void VerifyScale(GameObject go, AssetRow row)
        {
            if (row.size == null || row.size.Length != 3) return;
            var got = Bounds(go).size;
            var want = new Vector3(row.size[0], row.size[1], row.size[2]);
            var err = Vector3.Scale(got - want, new Vector3(1f / Mathf.Max(want.x, .01f),
                1f / Mathf.Max(want.y, .01f), 1f / Mathf.Max(want.z, .01f)));
            if (err.magnitude > .03f) Debug.LogWarning($"[DecoKit] {row.name}: imported size {got} != Blender {want}");
        }
    }
}
#endif
