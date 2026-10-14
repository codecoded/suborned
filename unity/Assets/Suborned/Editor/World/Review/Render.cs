using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>Manual Camera.Render for review output (flyby frames, audit thumbnails), the way Shots does it: the GPU Resident
    /// Drawer off (manual renders after a scene reload can leave its batches stale) and synchronous shader compilation, both
    /// restored on Dispose, together with the URP shadow distance a caller may change per frame.</summary>
    public sealed class RenderSession : IDisposable
    {
        readonly UniversalRenderPipelineAsset urp;
        readonly GPUResidentDrawerMode drawer;
        readonly float shadowDistance;
        readonly bool asyncCompilation;
        readonly IDisposable cascades;
        readonly TacticalShadows play;
        readonly Capture capture = new();

        public RenderSession()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("World: no graphics device; run without -nographics");
            urp = UniversalRenderPipeline.asset ?? throw new InvalidOperationException("World: no URP asset active");
            drawer = urp.gpuResidentDrawerMode; shadowDistance = urp.shadowDistance; asyncCompilation = ShaderUtil.allowAsyncCompilation;
            urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
            ShaderUtil.allowAsyncCompilation = false;
            cascades = TacticalShadows.Save(urp);
            play = GameObject.Find("cameras/play") is { } p && p.TryGetComponent<TacticalShadows>(out var ts) ? ts : null;
        }

        /// <summary>A pose's shadows: fitted to its street focus like the play camera (its bands) when it has one, else the plain
        /// distance with the asset's own cascades.</summary>
        public void Shadows(Pose pose)
        {
            cascades.Dispose(); // back to the asset's own cascades
            if (!float.IsNaN(pose.ShadowFocus) && play) TacticalShadows.Fit(urp, pose.ShadowFocus, play.nearBand, play.farBand, play.cascades, play.airSplits, play.fade);
            else ShadowDistance = pose.ShadowDistance;
        }

        public float ShadowDistance { set => urp.shadowDistance = value > 0 ? value : shadowDistance; }

        /// <summary>Renders cam at w x h through Capture (Glint's honest path: an HDR target with the asset's MSAA, then the sRGB
        /// encode) and returns a readable texture owned by the session (valid until the next call).</summary>
        public Texture2D Grab(Camera cam, int w, int h) => capture.Grab(cam, w, h);

        public void Dispose()
        {
            cascades.Dispose();
            urp.gpuResidentDrawerMode = drawer; urp.shadowDistance = shadowDistance;
            ShaderUtil.allowAsyncCompilation = asyncCompilation;
            capture.Dispose();
        }

        /// <summary>A throwaway camera (not saved with the scene) with the play camera's clear colour.</summary>
        public static Camera TempCamera(string name)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = RenderSettings.skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            var play = GameObject.Find("cameras/play");
            cam.backgroundColor = play && play.TryGetComponent<Camera>(out var p) ? p.backgroundColor : new Color(0.75f, 0.85f, 0.92f);
            Atmosphere.Camera(cam);
            return cam;
        }

        /// <summary>Opens the generated scene unless it is already the active one (asks to save the current one first in the Editor).</summary>
        public static bool EnsureScene()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.path == HarbourWorldBuilder.ScenePath && GameObject.Find(HarbourWorldBuilder.RootName)) return true;
            if (!System.IO.File.Exists(System.IO.Path.Combine(HarbourWorldBuilder.RepoRoot, "unity", HarbourWorldBuilder.ScenePath)))
                throw new System.IO.FileNotFoundException($"World: {HarbourWorldBuilder.ScenePath} not built yet (Suborned/World/Build Harbour District (Export))");
            if (!HarbourWorldBuilder.MayReplaceScene()) return false;
            EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            return true;
        }

        public static string ExportPath => HarbourWorldBuilder.Arg("-world", System.IO.Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "world", "harbour-district.world.json"));
    }
}
