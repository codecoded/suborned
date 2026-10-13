using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>The review tool's perspective camera (Walk and Fly share it): made at runtime like the builder's own cameras (the
    /// play camera's clear, post-processing on), never saved in the scene. While it renders, the URP asset's shadow distance is
    /// the current mode's and its cascades the asset's own, as Shots renders the perspective views; the asset is put back after
    /// every render, as TacticalShadows does, so it is never left changed.</summary>
    public sealed class WalkEye : IDisposable
    {
        public readonly Camera Camera;
        /// <summary>Shadow distance (m) for the next render; the current mode sets it.</summary>
        public float ShadowDistance = 400f;
        IDisposable saved;

        public WalkEye(Camera play)
        {
            var go = new GameObject("walk-review-eye");
            Camera = go.AddComponent<Camera>();
            Camera.clearFlags = play.clearFlags; Camera.backgroundColor = play.backgroundColor; Camera.allowMSAA = play.allowMSAA;
            Atmosphere.Camera(Camera);
            Camera.enabled = false;
            RenderPipelineManager.beginCameraRendering += Begin;
            RenderPipelineManager.endCameraRendering += End;
        }

        void Begin(ScriptableRenderContext _, Camera c)
        {
            if (c != Camera || UniversalRenderPipeline.asset is not { } urp) return;
            saved = TacticalShadows.Save(urp);
            urp.shadowDistance = ShadowDistance;
        }

        void End(ScriptableRenderContext _, Camera c)
        {
            if (c != Camera) return;
            saved?.Dispose(); saved = null;
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= Begin;
            RenderPipelineManager.endCameraRendering -= End;
            saved?.Dispose(); saved = null;
        }
    }
}
