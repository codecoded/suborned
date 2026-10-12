using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>DecoKit FBX import settings (as the-suborned's DecoKitModels): scale 1 with the file's units, no axis bake, Blender's
    /// normals, Mikk tangents, no compression, no cameras, lights or animation; materials remapped by name onto
    /// Assets/Suborned/Kit/Materials (the copied library). Applied to every model `bun run kit` copies under Kit/Models.
    /// Check() is the build gate: every adopted prefab binds every slot to a material whose shader compiled.</summary>
    public sealed class KitImport : AssetPostprocessor
    {
        const string Models = KitPlacer.KitRoot + "Models/";
        const string Materials = KitPlacer.KitRoot + "Materials/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Models) || assetImporter is not ModelImporter mi) return;
            Configure(mi);
        }

        static void Configure(ModelImporter mi)
        {
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.bakeAxisConversion = false;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.isReadable = false;
            mi.importBlendShapes = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importVisibility = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importAnimation = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }

        /// <summary>Remap each embedded material to the library material of the same name (once the model has imported, so its
        /// material names are known). Returns the number of slots that found no library material.</summary>
        static int Remap(ModelImporter mi, string path)
        {
            int missing = 0; bool changed = false;
            foreach (var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
            {
                var lib = AssetDatabase.LoadAssetAtPath<Material>(Materials + m.name + ".mat");
                if (!lib) { missing++; Debug.LogError($"World: kit {path}: material `{m.name}` not in {Materials}"); continue; }
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name);
                if (mi.GetExternalObjectMap().TryGetValue(id, out var have) && have == lib) continue;
                mi.AddRemap(id, lib); changed = true;
            }
            if (changed) mi.SaveAndReimport();
            return missing;
        }

        /// <summary>Menu / batchmode: re-applies the settings and the remap to every kit model, then checks.
        ///   Unity -batchmode -nographics -quit -projectPath unity -executeMethod Suborned.World.KitImport.Apply</summary>
        [MenuItem("Suborned/World/Import Kit")]
        public static void Apply() => HarbourWorldBuilder.Batch(() =>
        {
            int models = 0, missing = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Models.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not ModelImporter mi) continue;
                Configure(mi); mi.SaveAndReimport();
                missing += Remap(mi, path); models++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"World: kit import {models} models, {missing} unmapped materials");
            if (missing > 0) throw new InvalidDataException("World: kit materials unmapped");
            Check(new KitPlacer(Path.Combine(HarbourWorldBuilder.RepoRoot, "contracts", "world", "kit.json")));
        });

        /// <summary>Generated pieces (FBX only): import settings and the material remap on the model, then a prefab of the model under
        /// Kit/Prefabs/Generated/, built once (it references the model, so a re-copied FBX shows through). Unmapped material = reject.</summary>
        public static void Generated(KitPlacer kit)
        {
            int built = 0;
            foreach (var (id, fbx) in kit.Generated.OrderBy(kv => kv.Key, System.StringComparer.Ordinal).Select(kv => (kv.Key, kv.Value)))
            {
                if (AssetImporter.GetAtPath(fbx) is not ModelImporter mi)
                {
                    AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceSynchronousImport);
                    mi = AssetImporter.GetAtPath(fbx) as ModelImporter ?? throw new FileNotFoundException($"World: kit `{id}` model missing at {fbx} (run `bun run kit`)");
                }
                if (!Configured(mi)) { Configure(mi); mi.SaveAndReimport(); }
                if (Remap(mi, fbx) > 0) throw new InvalidDataException($"World: kit `{id}` materials unmapped");
                var path = kit.Prefabs[id];
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) continue;
                Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
                try { if (!PrefabUtility.SaveAsPrefabAsset(go, path)) throw new IOException($"World: could not save {path}"); }
                finally { Object.DestroyImmediate(go); }
                built++;
            }
            Debug.Log($"World: kit generated {kit.Generated.Count} pieces, {built} prefabs built");
        }

        static bool Configured(ModelImporter mi) =>
            mi.globalScale == 1f && mi.useFileScale && !mi.bakeAxisConversion && mi.importNormals == ModelImporterNormals.Import &&
            mi.materialImportMode == ModelImporterMaterialImportMode.ImportViaMaterialDescription && mi.materialLocation == ModelImporterMaterialLocation.InPrefab &&
            !mi.importCameras && !mi.importLights && !mi.importAnimation;

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Every adopted prefab: renderers present, no empty slot, every shader found and supported (no pink).</summary>
        public static void Check(KitPlacer kit)
        {
            int bad = 0, mats = 0;
            foreach (var id in kit.Prefabs.Keys.OrderBy(k => k, System.StringComparer.Ordinal))
            {
                var go = kit.Prefab(id);
                var rs = go.GetComponentsInChildren<MeshRenderer>();
                if (rs.Length == 0) { bad++; Debug.LogError($"World: kit {id}: no renderers"); }
                foreach (var r in rs)
                    foreach (var m in r.sharedMaterials)
                    {
                        mats++;
                        if (!m) { bad++; Debug.LogError($"World: kit {id}/{r.name}: empty material slot"); continue; }
                        var s = m.shader;
                        bool gpu = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null; // -nographics: no device to ask
                        if (!s || s.name == "Hidden/InternalErrorShader" || (gpu && !s.isSupported) || ShaderUtil.ShaderHasError(s))
                        { bad++; Debug.LogError($"World: kit {id}/{r.name}: material {m.name} shader `{(s ? s.name : "none")}` missing or broken"); }
                    }
            }
            Debug.Log($"World: kit check {kit.Prefabs.Count} prefabs, {mats} material slots, {bad} problems");
            if (bad > 0) throw new InvalidDataException("World: kit check failed");
        }
    }
}
