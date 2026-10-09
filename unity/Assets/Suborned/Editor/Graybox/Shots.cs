using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>Renders the scene's cameras to PNG under &lt;repo&gt;/out/shots/ (needs a GPU: batchmode without -nographics).
    /// overview.png (2048², whole district) · play-handover.png (1920×1080, play camera as saved) ·
    /// play-handover-wide.png (play camera at orthographic size 40, not saved, so the whole block is in one frame).
    /// Every shot log line names the plan hash stamped in the scene.
    /// Batchmode: -executeMethod Suborned.Graybox.Shots.Render</summary>
    public static class Shots
    {
        public const float WideSize = 40f;

        [MenuItem("Suborned/Graybox/Render Shots")]
        public static void Render() => WorldBuilder.Batch(() =>
        {
            if (!WorldBuilder.MayReplaceScene()) return;
            // Async compilation draws not-yet-compiled shaders with a placeholder, which spoiled the first shot.
            ShaderUtil.allowAsyncCompilation = false;
            EditorSceneManager.OpenScene(WorldBuilder.ScenePath, OpenSceneMode.Single);
            var dir = Path.Combine(WorldBuilder.RepoRoot, "out", "shots");
            Directory.CreateDirectory(dir);
            var plan = WorldBuilder.ScenePlanHash();
            var overview = Find("cameras/overview");
            var play = Find("cameras/play");
            // The first Camera.Render after opening the scene in batchmode comes out as one flat colour (last draw
            // fills the frame); a throwaway warm-up render makes every later one correct.
            Shot(play, 64, 64, null);
            Shot(overview, 2048, 2048, Path.Combine(dir, "overview.png"), plan);
            Shot(play, 1920, 1080, Path.Combine(dir, "play-handover.png"), plan);
            float size = play.orthographicSize;
            play.orthographicSize = WideSize;
            Shot(play, 1920, 1080, Path.Combine(dir, "play-handover-wide.png"), plan);
            play.orthographicSize = size;
        });

        internal static Camera Find(string path)
        {
            var go = GameObject.Find(path);
            if (!go) throw new FileNotFoundException($"Graybox: camera {path} not in {WorldBuilder.ScenePath}");
            return go.GetComponent<Camera>();
        }

        internal static void Shot(Camera cam, int w, int h, string file, string plan = null)
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
            if (file != null) Debug.Log($"Graybox: shot {file} ({w}x{h}, {cam.name}, plan {plan})");
        }
    }
}
