using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Perf
{
    /// <summary>The perf player's SSAO switches (Redline fix 1) need the variants of the SSAO source the play renderer does not use,
    /// which URP's prefiltering strips: the depth source (_SOURCE_DEPTH_LOW/MEDIUM/HIGH, after opaque) when the play renderer reads
    /// depth-normals, the depth-normals source (prepass) when it reads depth, as built since B6. For the perf build only, a copy of the
    /// play renderer whose SSAO uses the other source joins the pipeline asset's renderer list, so the build keeps both; the camera
    /// keeps renderer 0, so the copy never draws. Remove takes it out again and re-gathers the prefiltering a normal build writes.</summary>
    public static class PerfVariantsRenderer
    {
        public const string Folder = "Assets/Suborned/PerfBuildTemp";
        const string CopyPath = Folder + "/PerfVariants_Renderer.asset";

        /// <summary>Adds the copy; returns what the build keeps.</summary>
        public static string Add()
        {
            var asset = Asset();
            var so = new SerializedObject(asset);
            var list = so.FindProperty("m_RendererDataList");
            var play = list.GetArrayElementAtIndex(so.FindProperty("m_DefaultRendererIndex").intValue).objectReferenceValue;
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.CreateFolder("Assets/Suborned", "PerfBuildTemp");
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(play), CopyPath)) throw new InvalidOperationException($"Perf: could not copy {play.name} to {CopyPath}");
            var copy = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(CopyPath);
            int ssao = 0; string kept = "";
            foreach (var f in copy.rendererFeatures)
            {
                if (f is not ScreenSpaceAmbientOcclusion || !f.isActive) continue;
                if (AssetDatabase.GetAssetPath(f) != CopyPath) throw new InvalidOperationException("Perf: the renderer copy shares its SSAO feature with the play renderer");
                var fs = new SerializedObject(f);
                var source = fs.FindProperty("m_Settings.Source");
                bool depth = source.enumValueIndex != 0; // the play renderer reads depth-normals: keep depth (0), and the other way round
                source.enumValueIndex = depth ? 0 : 1;   // DepthSource.Depth : DepthSource.DepthNormals
                fs.FindProperty("m_Settings.AfterOpaque").boolValue = depth;
                kept = depth ? "SSAO from depth, after opaque" : "SSAO from depth-normals, prepass";
                fs.ApplyModifiedPropertiesWithoutUndo();
                ssao++;
            }
            if (ssao == 0) throw new InvalidOperationException($"Perf: {play.name} has no active SSAO feature to copy");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = copy;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return $"{asset.name} + {copy.name} ({kept})";
        }

        public static void Remove()
        {
            var asset = Asset();
            var so = new SerializedObject(asset);
            var list = so.FindProperty("m_RendererDataList");
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                var r = list.GetArrayElementAtIndex(i);
                if (r.objectReferenceValue && AssetDatabase.GetAssetPath(r.objectReferenceValue) != CopyPath) continue;
                r.objectReferenceValue = null;
                list.DeleteArrayElementAtIndex(i);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
            Regather();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        static UniversalRenderPipelineAsset Asset() =>
            QualitySettings.renderPipeline as UniversalRenderPipelineAsset ?? GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset
            ?? throw new InvalidOperationException("Perf: no URP asset for the current quality level");

        /// <summary>URP's own prefiltering pass (internal), so the asset holds what a build of the play renderer alone writes.</summary>
        static void Regather()
        {
            var t = Type.GetType("UnityEditor.Rendering.Universal.ShaderBuildPreprocessor, Unity.RenderPipelines.Universal.Editor");
            var m = t?.GetMethod("GatherShaderFeatures", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null) { Debug.LogWarning("Perf: URP's GatherShaderFeatures not found; the pipeline asset keeps the perf build's prefiltering until the next build"); return; }
            m.Invoke(null, null);
        }
    }
}
