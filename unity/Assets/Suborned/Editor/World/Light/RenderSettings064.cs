using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Suborned.World
{
    /// <summary>Brief 064 A3 items 5 and 7 (Latch, 2026-10-28), set on the render assets by code so the setting has a source:
    ///  5. PC_RPAsset `m_RequireDepthTexture` 0: the depth texture only when something asks for it (SSAO and DoF request it themselves; the sun
    ///     flare, which would force it, is disabled wherever it does not draw: AtmosphereLens).
    ///  7. PC_Renderer: the signal layer (SignalsFeature.Layer) out of the prepass, opaque and transparent filters, and SignalsFeature added,
    ///     which draws that layer after post in the camera's own frame (in place of HostFightLab's stacked overlay camera).
    /// Idempotent; prints what it found and what it changed.
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.RenderSettings064.Apply -logFile &lt;log&gt;</summary>
    public static class RenderSettings064
    {
        const string AssetPath = "Assets/Settings/PC_RPAsset.asset", RendererPath = "Assets/Settings/PC_Renderer.asset";

        [MenuItem("Suborned/Render/Apply 064 A3 settings (depth by request, signals pass)")]
        public static void Apply() => HarbourWorldBuilder.Batch(Run);

        static void Run()
        {
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath) ?? throw new System.InvalidOperationException($"064 A3: no {AssetPath}");
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath) ?? throw new System.InvalidOperationException($"064 A3: no {RendererPath}");
            var log = new List<string>();
            if (urp.supportsCameraDepthTexture) { urp.supportsCameraDepthTexture = false; EditorUtility.SetDirty(urp); log.Add("PC_RPAsset requireDepthTexture 1 → 0"); }
            else log.Add("PC_RPAsset requireDepthTexture already 0");
            int bit = 1 << SignalsFeature.Layer;
            LayerMask Off(LayerMask m, string name) { if ((m.value & bit) == 0) { log.Add($"PC_Renderer {name} already leaves layer {SignalsFeature.Layer} out"); return m; } log.Add($"PC_Renderer {name} {m.value} → {m.value & ~bit} (layer {SignalsFeature.Layer} out)"); return m.value & ~bit; }
            data.prepassLayerMask = Off(data.prepassLayerMask, "prepassLayerMask");
            data.opaqueLayerMask = Off(data.opaqueLayerMask, "opaqueLayerMask");
            data.transparentLayerMask = Off(data.transparentLayerMask, "transparentLayerMask");
            if (!data.rendererFeatures.Any(f => f is SignalsFeature))
            {
                var f = ScriptableObject.CreateInstance<SignalsFeature>(); f.name = nameof(SignalsFeature); f.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(f, data);
                data.rendererFeatures.Add(f);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out _, out long id);
                var map = (List<long>)typeof(ScriptableRendererData).GetField("m_RendererFeatureMap", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
                map.Add(id);
                log.Add($"PC_Renderer: SignalsFeature added (local id {id}); features now {string.Join(", ", data.rendererFeatures.Select(x => x ? x.name : "null"))}");
            }
            else log.Add("PC_Renderer: SignalsFeature already present");
            data.SetDirty(); EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("064 A3 render settings:\n  " + string.Join("\n  ", log));
        }
    }
}
