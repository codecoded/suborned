using System;
using System.Collections;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Captures are 1920 x 1080 (brief 034, Sky: shots at the Studio's native 6016 x 3384 were 20 to 26 MB each). In the built player, every run
    /// that takes pictures (`-gameProof`, `-shots`, `-shots031`, `-shots033`) first sets the window to 1920 x 1080, windowed: the HUD's whole-number scale
    /// is then 1 (HudToolkit.CrispScale), which is the size the HUD budget and the layout audit are defined at, and every capture helper
    /// (HudToolkit.Shoot's frame and HUD layer, the proof's shots, the 031 and 033 routines, ShotRunner) captures at the screen's size, so all of them
    /// follow. `-capture-size WxH` picks another size, `-capture-native` keeps the display's own. Frame times are another matter: the 60 fps limit is set on the
    /// MacBook Air (2880 x 1864), so tools/game/cli.ts runs the proof with `-frames-size 2880x1864` (below) and labels the result. A Retina display may give a window twice the requested size in pixels; Apply measures what it got and asks again for the
    /// size that gives exactly the one wanted. Frame times taken under a run are at that run's size: proof.json says which ("screen" the pictures, "framesAt" the frames).
    /// One pass (Latch, 2026-10-21): `-frames-size WxH` keeps the window at that size for the run (the frame times: the proof at the Air's
    /// 2880 x 1864) and HudToolkit.Shoot switches it to the picture size (1920 x 1080, or `-capture-size`) around each capture, then back
    /// (ToPicture, ToRun). So one proof run gives pictures at 1080p and frames at the Air's size; game:check no longer runs it twice.</summary>
    public sealed class CaptureSize : MonoBehaviour
    {
        public static readonly Vector2Int Default = new(1920, 1080);
        /// <summary>The size pictures are taken at when it differs from the run's window (`-frames-size`), else null: capture at the screen's size.</summary>
        public static Vector2Int? Picture { get; private set; }
        /// <summary>The window between pictures (`-frames-size`), else null.</summary>
        public static Vector2Int? Run { get; private set; }
        static float ratio = 1f; // pixels per requested unit, learned on the first Apply (2 on a Retina window sized in points)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || Application.isBatchMode || GameBoot.Arg("-capture-native")) return;
            var v = GameBoot.ArgValue("-capture-size");
            bool captures = Captures;
            if (!captures && v == null) return;
            var want = Parse(v) ?? Default;
            var run = captures ? Parse(GameBoot.ArgValue("-frames-size")) : null;
            if (run is Vector2Int r && r != want) { Run = r; Picture = want; }
            var go = new GameObject("capture-size"); DontDestroyOnLoad(go);
            var at = Run ?? want;
            go.AddComponent<CaptureSize>().StartCoroutine(Apply(at.x, at.y));
        }

        /// <summary>Before a picture: the window at the picture size (nothing when there is no separate run size, or it is there already).</summary>
        public static IEnumerator ToPicture() { if (Picture is Vector2Int p) yield return Apply(p.x, p.y); }
        /// <summary>After a picture: the window back at the run's size, and a moment for the frame times to settle.</summary>
        public static IEnumerator ToRun() { if (Run is Vector2Int r) { yield return Apply(r.x, r.y); yield return new WaitForSecondsRealtime(0.2f); } }
        /// <summary>The window is at the run's size (or there is no separate one).</summary>
        public static bool AtRun => Run is not Vector2Int r || Screen.width == r.x && Screen.height == r.y;

        /// <summary>This run takes pictures (and is sized here): GameBoot's full-screen request is not made, so it cannot land after the window is
        /// sized (Latch 2026-10-21: a -shots033 run came out 6016x3384, the full screen arriving after Apply had found the window already 1920x1080).</summary>
        public static bool Captures => !GameBoot.Arg("-capture-native") && (GameBoot.Arg("-gameProof") || GameBoot.Arg("-shots") || GameBoot.Arg("-shots031") || GameBoot.Arg("-shots033") || GameBoot.ArgValue("-capture-size") != null);

        static Vector2Int? Parse(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var p = s.ToLowerInvariant().Split('x');
            return p.Length == 2 && int.TryParse(p[0], out var w) && int.TryParse(p[1], out var h) && w >= 320 && h >= 180 ? new Vector2Int(w, h) : null;
        }

        /// <summary>The window at w x h pixels (windowed), a few frames to settle. Already that size: nothing happens. Check Screen.width afterwards: a size the display cannot give is left as it came.</summary>
        public static IEnumerator Apply(int w, int h)
        {
            if (Screen.width == w && Screen.height == h && Screen.fullScreenMode == FullScreenMode.Windowed) yield break;
            int rw = Mathf.RoundToInt(w / ratio), rh = Mathf.RoundToInt(h / ratio);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                int bw = Screen.width, bh = Screen.height;
                Screen.SetResolution(rw, rh, FullScreenMode.Windowed);
                // Latch 2026-10-21: the resize lands a few frames late (the first one-pass proof read the old size, learned a 1.5 "ratio" and
                // asked for 1280x720): wait for the size to change, then settle
                float t0 = Time.realtimeSinceStartup; while (Screen.width == bw && Screen.height == bh && Time.realtimeSinceStartup - t0 < 2f) yield return null;
                for (int i = 0; i < 4; i++) yield return null;
                yield return new WaitForSecondsRealtime(0.15f);
                if (Screen.width == w && Screen.height == h) break;
                float r = rw > 0 ? (float)Screen.width / rw : 1f; // what the display made of the request: 2 (or 0.5) on a Retina window sized in points; anything else is not a ratio
                if (Mathf.Abs(r - 2f) < 0.02f || Mathf.Abs(r - 0.5f) < 0.01f) { ratio = r; rw = Mathf.RoundToInt(w / ratio); rh = Mathf.RoundToInt(h / ratio); }
                else if (Mathf.Abs(r - 1f) > 0.01f) { rw = w; rh = h; ratio = 1f; } // a stale read: ask again for the size itself
                else break;
            }
            Debug.Log($"CaptureSize: wanted {w}x{h}, the player is {Screen.width}x{Screen.height} {Screen.fullScreenMode}");
        }
    }
}
