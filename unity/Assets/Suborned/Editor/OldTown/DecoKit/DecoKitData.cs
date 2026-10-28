#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Paths and the JSON contracts written by Tools/Blender/decokit/export_kit.py.
    internal static class DecoKitData
    {
        internal const string Root = "Assets/Suborned/OldTownArt/DecoKit";
        internal const string TextureDir = Root + "/Textures";
        internal const string HdriDir = Root + "/HDRI";
        internal const string MaterialDir = Root + "/Materials";
        internal const string PrefabDir = Root + "/Prefabs";
        internal const string GeneratedDir = Root + "/Generated";
        internal const string ScenesDir = "Assets/Scenes";
        // Shots, sweeps and test renders: Scratch/ is gitignored and outside Assets (AGENTS.md "Output rules").
        internal static readonly string CaptureDir = Path.GetFullPath("Scratch/Captures/DecoKit");
        // Kept outputs: the live Unity icons (films and stills go to CaptureDir).
        internal static readonly string ArtDir = Path.GetFullPath("SourceArt/DecoKit");

        [Serializable]
        internal class MaterialRow
        {
            public string name, kind, texture, normal, mask;
            public float[] color;
            public float smoothness, metallic, coat, coatSmoothness, alpha, emission, tileMetres;
        }

        [Serializable]
        internal class MaterialTable { public MaterialRow[] materials; }

        [Serializable]
        internal class AssetRow
        {
            public string name, category, fbx, lod1;  // lod1: decimated distance version, if exported
            public int tris, lod1_tris;
            public float[] size;
            public string[] materials;
        }

        [Serializable]
        internal class Manifest { public AssetRow[] assets; }

        internal static MaterialTable Materials() => Load<MaterialTable>("decokit_materials.json");
        internal static Manifest Assets() => Load<Manifest>("decokit_manifest.json");

        internal static string PrefabPath(AssetRow a) => $"{PrefabDir}/{a.category}/DK_{a.name}.prefab";

        static T Load<T>(string file)
        {
            var path = Path.Combine(Root, file);
            if (!File.Exists(path)) throw new FileNotFoundException($"[DecoKit] run export_kit.py first: {path}");
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }

        internal static void EnsureFolder(string assetPath)
        {
            var parts = assetPath.Split('/');
            var cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!UnityEditor.AssetDatabase.IsValidFolder(next)) UnityEditor.AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
