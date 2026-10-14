using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.Perf
{
    /// <summary>Frame-time harness (brief 008 B6; brief 007 Redline action 1): in a development player started with
    /// `-perf plan.json [-perfOut result.json] [-perfShots dir]`, drives the scene's play camera (the real rig: its rotation, orthographic zoom,
    /// TacticalShadows fitted to the stand-off, ArchitectureFade about the focus) through the plan's stops. Warm-up loops the stops
    /// unrecorded (a fanless machine reaches its steady clock only after minutes); then each stop settles and records every frame
    /// (PerfRecorder), and the report is written and the player quits (exit 1 with `error` set on failure). vSync off, no frame
    /// cap, runs in the background. Nothing is saved in a scene: it spawns itself after the scene loads, only with `-perf` and only
    /// in a development player (a release player ignores the argument).</summary>
    public sealed class PerfHarness : MonoBehaviour
    {
        /// <summary>Recording buffers hold recordSeconds at this many frames a second.</summary>
        const int MaxFps = 1000;

        enum Phase { Warm, Settle, Record, Done }

        PerfPlan plan;
        string output, shots;
        bool shot;
        readonly PerfReport report = new();
        readonly List<StopResult> results = new();
        readonly List<PowerSample> power = new();
        PerfRecorder recorder;
        PerfCounters counters;
        Camera cam;
        TacticalShadows shadows;
        ArchitectureFade fade;
        Phase phase = Phase.Done;
        int stop;
        float runStart, phaseStart;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var args = Environment.GetCommandLineArgs();
            var planPath = Arg(args, "-perf");
            if (planPath == null || !Debug.isDebugBuild) return; // development players (and the Editor) only
            var harness = new GameObject("perf-harness").AddComponent<PerfHarness>();
            harness.shots = Arg(args, "-perfShots");
            harness.Init(planPath, Arg(args, "-perfOut") ?? Path.ChangeExtension(planPath, ".result.json"));
        }

        static string Arg(string[] args, string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

        void Init(string planPath, string outPath)
        {
            output = outPath;
            try
            {
                plan = JsonUtility.FromJson<PerfPlan>(File.ReadAllText(planPath));
                if (plan?.stops == null || plan.stops.Length == 0) throw new InvalidDataException($"Perf: no stops in {planPath}");
                cam = Camera.main ? Camera.main : throw new InvalidOperationException("Perf: no main camera (cameras/play)");
                if (!cam.TryGetComponent(out shadows)) throw new InvalidOperationException("Perf: the play camera has no TacticalShadows");
                if (!cam.TryGetComponent(out fade)) throw new InvalidOperationException("Perf: the play camera has no ArchitectureFade");
                var stamp = FindAnyObjectByType<WorldStamp>();
                report.content = plan.content; report.fingerprint = plan.fingerprint; report.exportSha256 = plan.exportSha256; report.source = plan.source;
                report.sceneExportSha256 = stamp ? stamp.exportSha256 : "no WorldStamp";
                report.warmupSeconds = plan.warmupSeconds; report.dwellSeconds = plan.dwellSeconds; report.settleSeconds = plan.settleSeconds; report.recordSeconds = plan.recordSeconds;
                report.pitch = plan.pitch; report.yaw = plan.yaw;
                if (stamp && stamp.exportSha256 != plan.exportSha256)
                    throw new InvalidDataException($"Perf: plan resolved on export {plan.exportSha256}, this player's scene is {stamp.exportSha256}");
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                Application.runInBackground = true;
                if (plan.width > 0) Screen.SetResolution(plan.width, plan.height, FullScreenMode.Windowed);
                recorder = new PerfRecorder(Mathf.CeilToInt(plan.recordSeconds * MaxFps) + 1);
                report.missingCounters = recorder.Missing();
                counters = new PerfCounters();
                power.Add(PerfHost.Power("start"));
                runStart = Time.realtimeSinceStartup;
                Go(0, plan.warmupSeconds > 0 ? Phase.Warm : Phase.Settle);
                Debug.Log($"Perf: {plan.stops.Length} stops, warm-up {plan.warmupSeconds}s, record {plan.recordSeconds}s each -> {output}");
            }
            catch (Exception e) { Fail(e); }
        }

        void Update()
        {
            if (phase == Phase.Done) return;
            try
            {
                float now = Time.realtimeSinceStartup;
                switch (phase)
                {
                    case Phase.Warm:
                        PerfRecorder.Tick();
                        if (now - runStart >= plan.warmupSeconds) { power.Add(PerfHost.Power("after warm-up")); Go(0, Phase.Settle); }
                        else if (now - phaseStart >= plan.dwellSeconds) Go((stop + 1) % plan.stops.Length, Phase.Warm);
                        break;
                    case Phase.Settle:
                        PerfRecorder.Tick();
                        if (shots != null && !shot && now - phaseStart >= plan.settleSeconds * 0.5f) Shoot(); // what the stop frames, never while recording
                        if (now - phaseStart < plan.settleSeconds) break;
                        if (report.screenWidth == 0) { PerfHost.Describe(report); report.renderCounters = counters.Read(); } // the window has its size by now
                        recorder.Begin(); phase = Phase.Record; phaseStart = now;
                        break;
                    case Phase.Record:
                        recorder.Sample();
                        if (now - phaseStart < plan.recordSeconds) break;
                        results.Add(Result(plan.stops[stop], now - phaseStart));
                        if (stop + 1 < plan.stops.Length) Go(stop + 1, Phase.Settle); else Finish();
                        break;
                }
            }
            catch (Exception e) { Fail(e); }
        }

        void Shoot()
        {
            shot = true;
            Directory.CreateDirectory(shots);
            ScreenCapture.CaptureScreenshot(Path.Combine(shots, $"{stop:00}-{plan.stops[stop].name}.png"));
        }

        void Go(int index, Phase next)
        {
            stop = index; phase = next; phaseStart = Time.realtimeSinceStartup; shot = false;
            var s = plan.stops[index];
            var rot = Quaternion.Euler(plan.pitch, plan.yaw, 0f);
            cam.transform.SetPositionAndRotation(s.focus - rot * Vector3.forward * s.standOff, rot);
            cam.orthographicSize = s.zoom;
            cam.farClipPlane = s.standOff + plan.farMargin;
            shadows.focusDepth = s.standOff;
            fade.focus = s.focus;
        }

        StopResult Result(PerfStop s, float seconds)
        {
            var metrics = new Metric[PerfRecorder.Names.Length];
            for (int i = 0; i < metrics.Length; i++) metrics[i] = Metric.Of(PerfRecorder.Names[i], recorder.Column(i), recorder.Count);
            return new StopResult { name = s.name, why = s.why, focus = s.focus, zoom = s.zoom, standOff = s.standOff, seconds = seconds, frames = recorder.Count, fps = recorder.Count / seconds, metrics = metrics };
        }

        void Finish()
        {
            power.Add(PerfHost.Power("end"));
            report.stops = results.ToArray();
            report.heaviestGpu = Heaviest("gpuMs", m => m.p95);
            report.heaviestCpu = Heaviest("cpuMainMs", m => m.p95);
            report.heaviestTriangles = Heaviest("triangles", m => m.p50);
            Write(0);
        }

        string Heaviest(string metric, Func<Metric, double> by)
        {
            string name = null; double best = double.MinValue;
            foreach (var s in results) if (s.Get(metric) is { } m && by(m) > best) { best = by(m); name = s.name; }
            return name;
        }

        void Fail(Exception e)
        {
            Debug.LogException(e);
            report.error = e.ToString();
            report.stops = results.ToArray();
            Write(1);
        }

        void Write(int exitCode)
        {
            phase = Phase.Done;
            report.power = power.ToArray();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
                Debug.Log($"Perf: wrote {output}");
            }
            catch (Exception e) { Debug.LogException(e); exitCode = 1; }
            recorder?.Dispose();
            Application.Quit(exitCode);
        }
    }
}
