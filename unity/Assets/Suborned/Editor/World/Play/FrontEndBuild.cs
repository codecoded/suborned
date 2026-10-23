using System.IO;
using Suborned.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Suborned.World.Play
{
    /// <summary>Brief 051 (the front end) in the build: the boot scene (Assets/Scenes/FrontEnd.unity: one camera clearing to ink-deep; FrontEnd.Boot
    /// does the rest), first in GameBuild's scene list; the missions' briefs (contracts/missions/&lt;mission&gt;/brief.json) copied into
    /// StreamingAssets/missions for the build only; and the import of Resources/Loading's picture (sRGB, no mips, bilinear, uncompressed, max 4096,
    /// as the loading spec asks).</summary>
    public static class FrontEndBuild
    {
        public const string ScenePath = "Assets/Scenes/FrontEnd.unity";
        const string Missions = "Assets/StreamingAssets/missions";

        /// <summary>Writes the boot scene when it is missing (it never changes: a camera and nothing else).</summary>
        public static void EnsureScene()
        {
            if (File.Exists(Path.Combine(Application.dataPath, "..", ScenePath))) return;
            var keep = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var go = new GameObject("front-end camera");
            var cam = go.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0x0b / 255f, 0x16 / 255f, 0x15 / 255f); cam.cullingMask = 0; cam.depth = -100;
            SceneManager.MoveGameObjectToScene(go, scene);
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", "Assets", "Scenes"));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException($"FrontEnd: could not save {ScenePath}");
            EditorSceneManager.CloseScene(scene, true);
            Debug.Log($"FrontEnd: boot scene written ({ScenePath}); active scene kept: {keep}");
        }

        /// <summary>contracts/missions/*/brief.json into StreamingAssets/missions/&lt;mission&gt;/brief.json (this build only, as the camera presets).</summary>
        public static void CopyBriefs()
        {
            var src = Path.Combine(HarbourWorldBuilder.RepoRoot, "contracts", "missions"); if (!Directory.Exists(src)) return;
            var dst = Path.Combine(Application.dataPath, "..", Missions); int n = 0;
            foreach (var dir in Directory.GetDirectories(src))
            {
                var f = Path.Combine(dir, "brief.json"); if (!File.Exists(f)) continue;
                var to = Path.Combine(dst, Path.GetFileName(dir)); Directory.CreateDirectory(to); File.Copy(f, Path.Combine(to, "brief.json"), true); n++;
            }
            if (n > 0) AssetDatabase.ImportAsset(Missions, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"FrontEnd: {n} mission brief(s) into {Missions}");
        }
        public static void RemoveBriefs() { if (Directory.Exists(Path.Combine(Application.dataPath, "..", Missions))) AssetDatabase.DeleteAsset(Missions); }
    }

    /// <summary>The loading picture's import (any png under Resources/Loading).</summary>
    public sealed class LoadingPictureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Loading/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.mipmapEnabled = false; ti.filterMode = FilterMode.Bilinear; ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 4096; ti.npotScale = TextureImporterNPOTScale.None; ti.alphaSource = TextureImporterAlphaSource.None;
        }
    }
}
