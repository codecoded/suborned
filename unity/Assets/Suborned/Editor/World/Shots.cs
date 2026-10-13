using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World
{
    /// <summary>Renders every views.json view of the built scene to &lt;repo&gt;/out/shots/&lt;prefix&gt;-&lt;view&gt;.png at the
    /// view's size. Per view: the URP shadow distance (restored afterwards; an export view's TacticalShadows fit wins) and fog on or off. Needs a GPU: batchmode
    /// without -nographics.
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Shots.Render [-shots &lt;prefix&gt;]</summary>
    public static class Shots
    {
        [MenuItem("Suborned/World/Render Shots")]
        public static void Render() => HarbourWorldBuilder.Batch(() =>
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new System.InvalidOperationException("World: no graphics device; run Shots without -nographics");
            var urp = UniversalRenderPipeline.asset ?? throw new System.InvalidOperationException("World: no URP asset active");
            float shadowDistance = urp.shadowDistance; bool fog = RenderSettings.fog;
            var drawerMode = urp.gpuResidentDrawerMode;
            bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
            try
            {
                // Manual Camera.Render calls after a scene reload can leave the Editor's resident batches stale.
                urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
                ShaderUtil.allowAsyncCompilation = false;
                EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
                fog = RenderSettings.fog;
                var prefix = Prefix();
                var dir = Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "shots");
                Directory.CreateDirectory(dir);
                var views = Views.Load(HarbourWorldBuilder.ViewsPath);
                bool warm = false;
                foreach (var v in views)
                {
                    var go = GameObject.Find("cameras/" + v.Name);
                    if (!go) { Debug.LogWarning($"World: no camera for view {v.Name} in {HarbourWorldBuilder.ScenePath}; skipped"); continue; }
                    var cam = go.GetComponent<Camera>();
                    urp.shadowDistance = float.IsNaN(v.ShadowDistance) ? shadowDistance : v.ShadowDistance;
                    RenderSettings.fog = v.Fog;
                    if (!warm) { Shot(cam, 64, 64, null); warm = true; } // the first render after opening a scene in batchmode comes out flat
                    using (go.TryGetComponent<TacticalShadows>(out var ts) ? ts.Use() : null) // export views: shadows fitted to the camera's depth
                    {
                        if (ts) Debug.Log($"World: view {v.Name}: tactical shadows, focus {ts.focusDepth:F1} m, distance {ts.Distance:F1} m, {ts.cascades} cascades");
                        Shot(cam, v.Width, v.Height, Path.Combine(dir, $"{prefix}-{v.Name}.png"));
                    }
                    Perf(cam);
                }
            }
            finally
            {
                urp.shadowDistance = shadowDistance; RenderSettings.fog = fog;
                urp.gpuResidentDrawerMode = drawerMode;
                ShaderUtil.allowAsyncCompilation = asyncCompilation;
            }
        });

        static string Prefix()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-shots");
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : "sample";
        }

        /// <summary>Evidence per view: the batch renderers whose bounds meet the camera frustum, and their vertices = draw calls
        /// of the opaque pass before shadows (one material per renderer). Batchmode publishes no render stats or profiler
        /// counters for Camera.Render, so this is counted, not sampled.</summary>
        static void Perf(Camera cam)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            int seen = 0, all = 0; long verts = 0, total = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>())
            {
                long n = r.GetComponent<MeshFilter>().sharedMesh.vertexCount;
                all++; total += n;
                if (!GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;
                seen++; verts += n;
            }
            Debug.Log($"World: perf {cam.name}: {seen}/{all} renderers in frustum, {verts}/{total} vertices");
            ProcKit.Perf(cam);
        }

        static void Shot(Camera cam, int w, int h, string file)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var previous = RenderTexture.active;
            cam.targetTexture = rt;
            cam.ResetAspect();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            if (file != null) File.WriteAllBytes(file, tex.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            if (file != null) Debug.Log($"World: shot {file} ({w}x{h}, {cam.name})");
        }
    }
}
