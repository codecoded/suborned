using System.IO;
using Suborned.Play;
using UnityEditor;
using UnityEngine;

namespace Suborned.World.Play
{
    /// <summary>Brief 078: the car damage rig as assets (Assets/Suborned/Generated/Fx): the four textures CarFxArt draws in code, the six URP
    /// particle materials over them (saved, so their shader variants ship in a build), and CarFx.prefab, the rig CarFxRig.Create builds. Never
    /// edited by hand: rebuild it from code. PlayInstall puts the prefab on every host it installs (HandoverHost.carFx).
    /// Menu: Suborned/Play/Build Car FX · batch: -executeMethod Suborned.World.Play.CarFxBuild.BuildBatch</summary>
    public static class CarFxBuild
    {
        public const string Dir = "Assets/Suborned/Generated/Fx", PrefabPath = Dir + "/CarFx.prefab";

        [MenuItem("Suborned/Play/Build Car FX")]
        static void Menu() => Build();

        public static void BuildBatch() { Build(); if (Application.isBatchMode) EditorApplication.Exit(0); }

        /// <summary>The prefab, built when it is not there yet.</summary>
        public static GameObject Ensure() => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) ?? Build();

        public static GameObject Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Suborned/Generated")) AssetDatabase.CreateFolder("Assets/Suborned", "Generated");
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Suborned/Generated", "Fx");
            var tex = CarFxArt.MakeTextures();
            tex.puff = Save(tex.puff); tex.flame = Save(tex.flame); tex.glow = Save(tex.glow); tex.scorch = Save(tex.scorch);
            var m = CarFxArt.MakeMaterials(tex) ?? throw new InvalidDataException($"CarFx: shader {CarFxArt.UnlitShader} or {CarFxArt.LitShader} missing");
            m.smoke = Save(m.smoke); m.fireball = Save(m.fireball); m.fire = Save(m.fire); m.glow = Save(m.glow); m.scorch = Save(m.scorch); m.debris = Save(m.debris);
            var rig = CarFxRig.Create(m);
            var prefab = PrefabUtility.SaveAsPrefabAsset(rig.gameObject, PrefabPath);
            Object.DestroyImmediate(rig.gameObject);
            AssetDatabase.SaveAssets();
            int max = 0; foreach (var p in prefab.GetComponentsInChildren<ParticleSystem>(true)) max += p.main.maxParticles;
            Debug.Log($"CarFx: built {PrefabPath}: {prefab.GetComponentsInChildren<ParticleSystem>(true).Length} systems (max {max} particles a rig), {prefab.GetComponentsInChildren<Light>(true).Length} lights, textures and materials in {Dir}");
            return prefab;
        }

        static T Save<T>(T asset) where T : Object
        {
            var path = $"{Dir}/{asset.name}.{(asset is Material ? "mat" : "asset")}";
            var have = AssetDatabase.LoadAssetAtPath<T>(path);
            if (have)
            {
                if (have is Material hm && asset is Material am) { hm.shader = am.shader; hm.CopyPropertiesFromMaterial(am); hm.shaderKeywords = am.shaderKeywords; hm.renderQueue = am.renderQueue; hm.SetOverrideTag("RenderType", am.GetTag("RenderType", false)); for (int i = 0; i < am.passCount; i++) hm.SetShaderPassEnabled(am.GetPassName(i), am.GetShaderPassEnabled(am.GetPassName(i))); EditorUtility.SetDirty(hm); return have; }
                if (have is Texture2D ht && asset is Texture2D at) { EditorUtility.CopySerialized(at, ht); EditorUtility.SetDirty(ht); return have; }
            }
            AssetDatabase.CreateAsset(asset, path); return asset;
        }
    }
}
