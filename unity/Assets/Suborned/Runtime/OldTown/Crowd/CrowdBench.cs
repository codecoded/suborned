using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Suborned.OldTown.Crowd
{
    /// <summary>The crowd test player's benchmark: launched with -crowdBench &lt;out.json&gt;, it runs every config (agents,
    /// run share, 2.5D zoom or first person on the square, heading, shadows) Rounds times interleaved - a loaded machine drifts, so each config's figure is
    /// the median of its rounds - each for Warm s then Measure s of frame times, with the GPU and main-thread times from
    /// FrameTimingManager (the player has frame timing stats), writes them and quits. Without the flag it does nothing,
    /// so the same scene stays the interactive test.</summary>
    public sealed class CrowdBench : MonoBehaviour
    {
        const float Warm = 1.5f, Measure = 3f;
        const int Rounds = 3;

        [SerializeField] VatCrowd crowd;
        [SerializeField] CrowdTestCamera rig;
        [SerializeField] CrowdTestViews views;

        [Serializable]
        struct Result
        {
            public string name;
            public int agents, drawn, frames;
            public float trisM, fps, msMean, msP50, msP95, msMax, gpuMs, mainMs;
        }

        [Serializable]
        struct Report
        {
            public string device, os;
            public int width, height;
            public List<Result> results;
        }

        static readonly (string name, int agents, float run, float size, float yaw, bool shadows)[] Configs =
        {
            ("empty", 0, .1f, 14f, 45f, true), ("tactical_empty", 0, .1f, 48f, 45f, true), ("play_1k", 1000, .1f, 14f, 45f, true), ("play_3k", 3000, .1f, 14f, 45f, true),
            ("play_6k", 6000, .1f, 14f, 45f, true), ("play_12k", 12000, .1f, 14f, 45f, true),
            ("play_3k_noshadow", 3000, .1f, 14f, 45f, false), ("play_3k_run60", 3000, .6f, 14f, 45f, true),
            ("tactical_3k", 3000, .1f, 48f, 45f, true), ("tactical_12k", 12000, .1f, 48f, 45f, true),
            ("tactical_12k_noshadow", 12000, .1f, 48f, 45f, false), ("play_12k_far", 12000, .1f, 14f, 45f, true),
            ("street_empty", 0, .1f, 0f, 45f, true), ("street_3k", 3000, .1f, 0f, 45f, true), ("street_12k", 12000, .1f, 0f, 45f, true),
            ("street_12k_r", 12000, .1f, 0f, 225f, true),  // size 0: first person on the square, facing yaw
        };

        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-crowdBench");
            if (at < 0 || at + 1 >= args.Length) yield break;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var report = new Report
            {
                device = SystemInfo.graphicsDeviceName, os = SystemInfo.operatingSystem, width = Screen.width, height = Screen.height,
                results = new List<Result>(),
            };
            var timings = new FrameTiming[1];
            var rounds = Configs.Select(_ => new List<Result>()).ToArray();
            for (int round = 0; round < Rounds; round++)
                for (int k = 0; k < Configs.Length; k++)
                {
                    var c = Configs[k];
                    crowd.Agents = c.agents;
                    crowd.RunShare = c.run;
                    crowd.Shadows = c.shadows;
                    crowd.FarSize = c.name.EndsWith("_far") ? 0f : 20f;  // _far: the far LOD at play zoom too
                    if (c.size > 0f)
                    {
                        if (views != null && views.FirstPerson) views.Set(false);
                        rig.Set(new Vector3(0f, 0f, -125f), c.size, c.yaw);
                    }
                    else if (views != null) views.Set(true, new Vector3(-8f, 0f, -137f), c.yaw);
                    for (float t = 0f; t < Warm; t += Time.unscaledDeltaTime) yield return null;
                    var ms = new List<float>();
                    float gpu = 0f, main = 0f;
                    int gpuN = 0;
                    for (float t = 0f; t < Measure; t += Time.unscaledDeltaTime)
                    {
                        yield return null;
                        ms.Add(Time.unscaledDeltaTime * 1000f);
                        FrameTimingManager.CaptureFrameTimings();
                        if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0)
                            (gpu, main, gpuN) = (gpu + (float)timings[0].gpuFrameTime, main + (float)timings[0].cpuMainThreadFrameTime, gpuN + 1);
                    }
                    if (round == 0 && c.name == "play_3k")  // what the player sees, HUD included: <out>.png
                    {
                        ScreenCapture.CaptureScreenshot(Path.ChangeExtension(args[at + 1], ".png"));
                        yield return null;
                    }
                    ms.Sort();
                    var r = new Result
                    {
                        name = c.name, agents = c.agents, drawn = crowd.Drawn, trisM = crowd.DrawnTris / 1e6f, frames = ms.Count,
                        msMean = ms.Average(), msP50 = ms[ms.Count / 2], msP95 = ms[(int)(ms.Count * .95f)], msMax = ms[^1],
                        gpuMs = gpuN > 0 ? gpu / gpuN : -1f, mainMs = gpuN > 0 ? main / gpuN : -1f,
                    };
                    r.fps = 1000f / r.msMean;
                    rounds[k].Add(r);
                    Debug.Log($"[CrowdBench] r{round} {r.name}: {r.fps:0} fps, p95 {r.msP95:0.0} ms, gpu {r.gpuMs:0.0} ms, main {r.mainMs:0.0} ms, drawn {r.drawn}");
                }
            foreach (var runs in rounds)  // per config, the round with the median frame time
                report.results.Add(runs.OrderBy(r => r.msMean).ElementAt(runs.Count / 2));
            File.WriteAllText(args[at + 1], JsonUtility.ToJson(report, true));
            Application.Quit();
        }
    }
}
