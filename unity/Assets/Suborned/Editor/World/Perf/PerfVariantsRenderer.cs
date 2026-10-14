using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Perf
{
    /// <summary>The perf player's ssao-depth switch (Redline fix 1) needs SSAO's depth-source variants (_SOURCE_DEPTH_LOW/MEDIUM/HIGH),
    /// which URP's prefiltering strips from a build whose renderers only use DepthNormals. For the perf build only, a copy of the play
    /// renderer whose SSAO reads depth (after opaque) joins the pipeline asset's renderer list, so the build keeps them; the camera
    /// keeps renderer 0, so the copy never draws. Remove takes it out again and re-gathers the prefiltering a normal build writes.</summary>
    public static class PerfVariantsRenderer
    {
        const string CopyPath = PerfProxyAsset.Folder + "/PerfVariants_Renderer.asset";

        /// <summary>Adds the copy; returns what the build keeps.</summary>
        public static string Add()
        {
            var asset = Asset();
            var so = new SerializedObject(asset);
            var list = so.FindProperty("m_RendererDataList");
            var play = list.GetArrayElementAtIndex(so.FindProperty("m_DefaultRendererIndex").intValue).objectReferenceValue;
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(play), CopyPath)) throw new InvalidOperationException($"Perf: could not copy {play.name} to {CopyPath}");
            var copy = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(CopyPath);
            int ssao = 0;
            foreach (var f in copy.rendererFeatures)
            {
                if (f is not ScreenSpaceAmbientOcclusion || !f.isActive) continue;
                if (AssetDatabase.GetAssetPath(f) != CopyPath) throw new InvalidOperationException("Perf: the renderer copy shares its SSAO feature with the play renderer");
                var fs = new SerializedObject(f);
                fs.FindProperty("m_Settings.Source").enumValueIndex = 0; // DepthSource.Depth
                fs.FindProperty("m_Settings.AfterOpaque").boolValue = true;
                fs.ApplyModifiedPropertiesWithoutUndo();
                ssao++;
            }
            if (ssao == 0) throw new InvalidOperationException($"Perf: {play.name} has no active SSAO feature to copy");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = copy;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return $"{asset.name} + {copy.name} (SSAO from depth, after opaque)";
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
