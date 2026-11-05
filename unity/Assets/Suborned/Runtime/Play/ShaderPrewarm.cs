using System;
using System.Collections;
using System.IO;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Play
{
    /// <summary>Brief 096 (Sky on 0.2.252, the Air: "yeah that 5 seconds jerk is bad"; Redline's render audit item 5, prewarm): the first switch to
    /// third person froze the game for seconds. Cause: the play view draws with one shadow cascade (option B), so its lit shaders take the
    /// _MAIN_LIGHT_SHADOWS variants; third person's perspective camera has four cascades, so its first frame needed the _MAIN_LIGHT_SHADOWS_CASCADE
    /// variant of every material in view, and Unity and Metal built them all in that one frame (6.1 s on a cold Metal cache on the Studio; the cache
    /// is per build, so every new build starts cold). briefs/096/latch/prewarm.md has the timings.
    /// The standard Unity 6 route: a GraphicsStateCollection of the states third person, the drive, a conversation cutaway and the map's quarter turns
    /// add to the opening play view (recorded in a development player with HitchTrace096 `-gscTrace`, the play view's own taken off by
    /// tools/game/prewarm-delta.ts), shipped as Resources/Prewarm/play-metal.graphicsstate and warmed a few states a frame once the street is
    /// loaded: the loading bar's last stage ("Plan") waits for it up to BarCapS, the rest goes on behind the menu.
    /// A collection recorded for another graphics API or platform is skipped. `-noPrewarm` turns it off (the A/B).</summary>
    public static class ShaderPrewarm
    {
        public const string Resource = "Prewarm/play-metal";
        /// <summary>States warmed per frame. A warm-up holds the frame it runs in (Metal builds the pipeline states there; one collection warms
        /// one state after another, and parts warmed side by side were no faster), so it goes a few states a frame: the loading bar, the menu
        /// and the Job page keep moving. `-prewarmStep n` sets it; `-prewarmAll` warms everything in one go (one long frame), for comparison.</summary>
        public const int Step = 2;
        static GraphicsStateCollection gsc; static ResourceRequest req; static JobHandle job; static bool begun, ended, jobOn, all; static int step = Step;
        static double t0, tJob, lastFrame, gapMax; static int frames;

        /// <summary>The warm-up has finished, was skipped or is not running at all: nothing to wait for.</summary>
        public static bool Done => !begun || ended;
        /// <summary>0..1 through the collection's states (1 when done or skipped).</summary>
        public static float Progress => Done ? 1f : gsc != null && gsc.totalGraphicsStateCount > 0 ? Mathf.Clamp01((float)gsc.completedWarmupCount / gsc.totalGraphicsStateCount) : 0f;
        /// <summary>Seconds from Begin to done (-1 while running or never begun).</summary>
        public static double Seconds { get; private set; } = -1;
        /// <summary>The longest frame while it warmed (ms), and the frames it took.</summary>
        public static double GapMaxMs => gapMax * 1000.0;
        public static int Frames => frames;

        /// <summary>Start loading and warming the collection (once per process; the front end's load, or the straight load a proof takes).</summary>
        public static void Begin()
        {
            if (begun || Application.isEditor || Application.isBatchMode) return;
            begun = true; t0 = Time.realtimeSinceStartupAsDouble;
            if (GameBoot.Arg("-noPrewarm") || trace != null) { End(trace != null ? "off while tracing (-gscTrace)" : "off (-noPrewarm)"); return; }
            all = GameBoot.Arg("-prewarmAll"); if (int.TryParse(GameBoot.ArgValue("-prewarmStep"), out var n) && n > 0) step = n;
            req = Resources.LoadAsync<GraphicsStateCollection>(Resource);
            req.completed += _ => Load();
        }

        static void Load()
        {
            gsc = req.asset as GraphicsStateCollection; req = null;
            if (gsc == null) { End($"no collection at Resources/{Resource}"); return; }
            if (gsc.graphicsDeviceType != SystemInfo.graphicsDeviceType || gsc.runtimePlatform != Application.platform) { End($"skipped: recorded for {gsc.graphicsDeviceType} {gsc.runtimePlatform}, this is {SystemInfo.graphicsDeviceType} {Application.platform}"); gsc = null; return; }
            tJob = lastFrame = Time.realtimeSinceStartupAsDouble;
            Debug.Log($"ShaderPrewarm: {gsc.variantCount} variants, {gsc.totalGraphicsStateCount} states (quality {gsc.qualityLevelName}); loaded in {tJob - t0:0.00} s, warming {(all ? "all at once" : $"{step} a frame")}");
            Application.onBeforeRender += Tick; // the first states start once the street is loaded (Tick)
        }

        /// <summary>Per frame, before it renders: the frame's length counted, and the next few states started once the last are done.</summary>
        static void Tick()
        {
            double now = Time.realtimeSinceStartupAsDouble; gapMax = Math.Max(gapMax, now - lastFrame); lastFrame = now; frames++;
            if (!StreetReady()) return;
            if (jobOn) { if (!job.IsCompleted) return; job.Complete(); jobOn = false; }
            if (gsc.isWarmedUp || gsc.completedWarmupCount >= gsc.totalGraphicsStateCount)
            {
                Application.onBeforeRender -= Tick;
                End($"warmed {gsc.completedWarmupCount}/{gsc.totalGraphicsStateCount} states in {now - tJob:0.00} s over {frames} frames, longest {GapMaxMs:0} ms (load {tJob - t0:0.00} s)");
                return;
            }
            job = all ? gsc.WarmUp(default) : gsc.WarmUpProgressively(step, default); jobOn = true;
        }

        /// <summary>A warm-up started while the street loads asynchronously (the front end's LoadSceneAsync) never finished, and neither did the load
        /// (Studio, 2026-11-06: both waited on each other): the first states start the frame after the street scene is loaded and active.</summary>
        static bool StreetReady()
        {
            if (readyAt > 0) return Time.frameCount >= readyAt;
            var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (sc.name == FrontEnd.GameScene && sc.isLoaded) readyAt = Time.frameCount + 1;
            return false;
        }
        static int readyAt;

        static void End(string why) { ended = true; Seconds = Time.realtimeSinceStartupAsDouble - t0; Debug.Log($"ShaderPrewarm: {why}; {Seconds:0.00} s from begin"); }

        /// <summary>The longest the loading bar waits for it. A cold Metal cache (a fresh install, and every new build: the cache does not carry
        /// over between builds) takes about 17 s on the Studio; a warm one about 0.2 s. What is left after the cap warms on behind the menu and the
        /// Job page, where a frame may stall now and then (the longest about 1 s), until it is done.</summary>
        public const float BarCapS = 6f;

        /// <summary>Waits for the warm-up (the loading bar's last stage), reporting progress, at most `maxS`; the rest goes on behind the menu.</summary>
        public static IEnumerator Wait(Action<float> progress, float maxS = BarCapS)
        {
            Begin(); float tw = Time.realtimeSinceStartup;
            while (!Done && Time.realtimeSinceStartup - tw < maxS) { progress?.Invoke(Progress); yield return null; }
            if (!Done) Debug.Log($"ShaderPrewarm: {Progress:P0} warm after the bar's {maxS:0} s; the rest warms behind the menu");
            Debug.Log($"ShaderPrewarm: the loading bar waited {Time.realtimeSinceStartup - tw:0.00} s for it");
            progress?.Invoke(1f);
        }

        // ---------------------------------------------------------------- recording (`-gscTrace file`, a probe's player only)

        static GraphicsStateCollection trace; static string tracePath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void TraceBoot()
        {
            if (Application.isEditor || GameBoot.ArgValue("-gscTrace") is not string f) return;
            tracePath = Path.GetFullPath(f); trace = new GraphicsStateCollection();
            Debug.Log($"ShaderPrewarm: tracing {(trace.BeginTrace() ? "on" : "FAILED to start")} → {tracePath}");
        }

        /// <summary>Ends the recording and writes it (HitchTrace096 at the end of its run). False when nothing was tracing.</summary>
        public static bool TraceSave()
        {
            if (trace == null) return false;
            trace.EndTrace(); Directory.CreateDirectory(Path.GetDirectoryName(tracePath));
            if (File.Exists(tracePath)) { var was = new GraphicsStateCollection(tracePath); Debug.Log($"ShaderPrewarm: adding to the recording already there ({was.variantCount} variants, {was.totalGraphicsStateCount} states)"); trace.Append(was); }
            bool ok = trace.SaveToFile(tracePath);
            Debug.Log($"ShaderPrewarm: trace {(ok ? "saved" : "NOT saved")}: {trace.variantCount} variants, {trace.totalGraphicsStateCount} states ({trace.graphicsDeviceType}, {trace.runtimePlatform}, quality {trace.qualityLevelName}) → {tracePath}");
            return ok;
        }
    }
}
