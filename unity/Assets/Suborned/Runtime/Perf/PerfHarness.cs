using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.Perf
{
    /// <summary>Frame-time harness (brief 008 B6; Redline's review out/review/008-b6-perf.html, M1-M8 and item 0). In a development
    /// player started with `-perf plan.json [-perfOut result.json] [-perfShots dir] [-perfPass name] [-perfStrict]` and, to pin one
    /// variant for the whole run, `[-perfSsao on|depth|normals|off] [-perfCascades n] [-perfShadows on|off] [-perfFade on|off|tile|reach]
    /// [-perfProxies on|off]` (`-perfNoWarm` skips the warm-up, for a counts-only pass), it drives the scene's play camera (the real rig: rotation, orthographic zoom, TacticalShadows fitted
    /// to the stand-off, ArchitectureFade about the focus). Preflight: the machine's load and power go into the report and a busy
    /// machine is flagged (refused with -perfStrict). Warm-up cycles the stops unrecorded until the frame time plateaus (PerfWarm).
    /// Then every segment of the schedule (PerfSchedule: passes, rotated repeats, interleaved variants) settles (thermal state;
    /// once per stop and variant the drawer's per-view counts, the NaN guard and a screenshot) and records every frame
    /// (PerfRecorder, rows to CSV by PerfFrames); pan stops move the focus along their path while they record. The report is
    /// written and the player quits (exit 1 with `error` set on failure). Nothing is saved in a scene: it spawns itself after the
    /// scene loads, only with `-perf` and only in a development player.</summary>
    public sealed class PerfHarness : MonoBehaviour
    {
        /// <summary>Recording buffers hold the longest recording at this many frames a second.</summary>
        const int MaxFps = 1000;
        /// <summary>Probe only when the pass settles this long (drawer counts at 0.4-0.7 s, NaN at 1.1 s, shot at 1.3 s after the last hitch).</summary>
        const float ProbeSettle = 1.5f;
        /// <summary>A settle frame longer than this is a hitch (a variant's first use compiles its pipeline states on Metal: seconds):
        /// settle lasts until QuietSeconds pass without one and the probes are done, so no compile lands in a recording; at most MaxSettle.</summary>
        const float HitchMs = 100f, QuietSeconds = 1f, MaxSettle = 30f;

        enum Phase { Warm, Settle, Record, Done }

        PerfPlan plan;
        List<Segment> schedule;
        string output, shots;
        readonly PerfReport report = new();
        readonly List<SegmentResult> results = new();
        readonly HashSet<(int, int)> probed = new();
        PerfRecorder recorder;
        PerfCounters counters;
        PerfSwitches switches;
        PerfNaN nan;
        PerfDrawer drawer;
        PerfFrames csv;
        PerfWarm warm;
        PerfPass pacing;
        Camera cam;
        TacticalShadows shadows;
        ArchitectureFade fade;
        Phase phase = Phase.Done;
        int seg, warmStop, unfocused, thermal, nanPixels, drawerFrame, hitches;
        float runStart, phaseStart, lastHitch, worstHitch, settled, lastUpdate;
        bool probe, drawerOn, drawerRetried, shot, thermalRead;
        DrawerStats drawerStats;
        double[] scratch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var args = Environment.GetCommandLineArgs();
            var planPath = Arg(args, "-perf");
            if (planPath == null || !Debug.isDebugBuild) return; // development players (and the Editor) only
            var harness = new GameObject("perf-harness").AddComponent<PerfHarness>();
            harness.shots = Arg(args, "-perfShots");
            harness.Init(args, planPath, Arg(args, "-perfOut") ?? Path.ChangeExtension(planPath, ".result.json"));
        }

        static string Arg(string[] args, string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

        void Init(string[] args, string planPath, string outPath)
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
                report.pitch = plan.pitch; report.yaw = plan.yaw;
                if (!stamp || stamp.exportSha256 != plan.exportSha256)
                    throw new InvalidDataException($"Perf: plan resolved on export {plan.exportSha256}, this player's scene is {report.sceneExportSha256}");
                if (CliVariant(args) is { } cli) { plan.variants = new[] { cli }; foreach (var p in plan.passes) p.variants = Array.Empty<string>(); }
                if (plan.variants.Length == 0) plan.variants = new[] { new PerfVariant { name = "as built" } };
                schedule = PerfSchedule.Build(plan, Arg(args, "-perfPass"));
                report.passes = plan.passes; report.variants = plan.variants;
                report.machineStart = PerfHost.Machine("start");
                if (report.machineStart.busy && Array.IndexOf(args, "-perfStrict") >= 0)
                    throw new InvalidOperationException("Perf: the machine is busy (-perfStrict): " + string.Join("; ", report.machineStart.warnings));
                if (report.machineStart.busy) Debug.LogWarning("Perf: MACHINE BUSY, this run is not a reference measure: " + string.Join("; ", report.machineStart.warnings));
                var proxies = new PerfProxies(); // the scene's baked shadow proxies (ShadowProxy), as built on or off
                report.proxyRenderers = proxies.Count; report.proxyTriangles = proxies.Triangles; report.proxiesAsBuilt = proxies.BuiltOn;
                switches = new PerfSwitches(cam, shadows, fade, proxies);
                report.ssaoAsBuilt = switches.SsaoAsBuilt;
                nan = new PerfNaN(cam, plan.width, plan.height);
                drawer = new PerfDrawer();
                float longest = 0f;
                foreach (var s in schedule) longest = Mathf.Max(longest, Duration(s));
                recorder = new PerfRecorder(Mathf.CeilToInt(longest * MaxFps) + 1);
                scratch = new double[Mathf.CeilToInt(longest * MaxFps) + 1];
                report.missingCounters = recorder.Missing();
                counters = new PerfCounters();
                csv = new PerfFrames(output);
                report.framesCsv = Path.GetFileName(csv.FramesPath); report.timingsCsv = Path.GetFileName(csv.TimingsPath);
                Application.targetFrameRate = -1;
                Application.runInBackground = true;
                Pace(plan.passes[schedule[0].pass]);
                runStart = Time.realtimeSinceStartup;
                if (Array.IndexOf(args, "-perfNoWarm") >= 0) plan.warmup.maxSeconds = 0f; // counts only (no timing read): no warm-up
                if (plan.warmup.maxSeconds > 0f)
                {
                    warm = new PerfWarm(plan.warmup, plan.stops.Length, MaxFps);
                    switches.Restore();
                    Park(plan.stops[0]);
                    phase = Phase.Warm; phaseStart = runStart;
                }
                else Open(0);
                Debug.Log($"Perf: {schedule.Count} segments, warm-up up to {plan.warmup.maxSeconds}s -> {output}");
            }
            catch (Exception e) { Fail(e); }
        }

        /// <summary>A variant from the command-line switches, or null when none is given.</summary>
        static PerfVariant CliVariant(string[] args)
        {
            string ssao = Arg(args, "-perfSsao"), casc = Arg(args, "-perfCascades"), sh = Arg(args, "-perfShadows"), fd = Arg(args, "-perfFade"), px = Arg(args, "-perfProxies");
            if (ssao == null && casc == null && sh == null && fd == null && px == null) return null;
            var v = new PerfVariant { ssao = ssao ?? "", cascades = casc != null ? int.Parse(casc) : 0, shadowsOff = sh == "off", fadeOff = fd == "off", fade = fd is "tile" or "reach" ? fd : "", proxies = px ?? "" };
            v.name = "cli: " + v.Describe(); v.why = "pinned by the command line";
            return v;
        }

        void Update()
        {
            if (phase == Phase.Done) return;
            try
            {
                float now = Time.realtimeSinceStartup, ms = (now - lastUpdate) * 1000f; // wall clock: vSync's deltaTime hides long frames
                lastUpdate = now;
                switch (phase)
                {
                    case Phase.Warm: Warm(now); break;
                    case Phase.Settle:
                        recorder.Tick();
                        if (ms > HitchMs) { hitches++; lastHitch = now; worstHitch = Mathf.Max(worstHitch, ms); }
                        Probes(now - lastHitch);
                        float e = now - phaseStart;
                        bool quiet = e >= plan.passes[schedule[seg].pass].settleSeconds && now - lastHitch >= QuietSeconds && ProbesDone;
                        if (!quiet && e < MaxSettle) break;
                        if (report.screenWidth == 0) Describe();
                        settled = e;
                        recorder.Begin(); unfocused = 0; phase = Phase.Record; phaseStart = now;
                        break;
                    case Phase.Record:
                        recorder.Sample();
                        if (!Application.isFocused) unfocused++;
                        var stop = plan.stops[schedule[seg].stop];
                        float t = now - phaseStart;
                        if (stop.IsPath) Along(stop, t * stop.speed);
                        if (t >= Duration(schedule[seg])) End(t);
                        break;
                }
            }
            catch (Exception e) { Fail(e); }
        }

        void Warm(float now)
        {
            recorder.Tick();
            warm.Frame(Time.unscaledDeltaTime * 1000.0);
            if (now - phaseStart < plan.warmup.dwellSeconds) return;
            int th = PerfHost.Thermal();
            warm.EndDwell(now - runStart, plan.stops[warmStop % plan.stops.Length].name, th);
            warmStop++;
            if (warmStop % plan.stops.Length == 0 && warm.EndCycle(now - runStart, th))
            {
                report.warmup = warm.Result(now - runStart);
                Open(0);
                return;
            }
            Park(plan.stops[warmStop % plan.stops.Length]);
            phaseStart = Time.realtimeSinceStartup;
        }

        void Open(int index)
        {
            seg = index;
            var s = schedule[index];
            var pass = plan.passes[s.pass];
            Pace(pass);
            switches.Apply(plan.variants[s.variant]);
            Park(plan.stops[s.stop]);
            phase = Phase.Settle; phaseStart = lastHitch = Time.realtimeSinceStartup; hitches = 0; worstHitch = 0f;
            thermal = -1; thermalRead = false; nanPixels = -1; drawerStats = null; drawerOn = false; drawerRetried = false; shot = false;
            probe = pass.settleSeconds >= ProbeSettle && s.repeatOf < 0 && probed.Add((s.stop, s.variant));
        }

        /// <summary>Settle-phase reads, in order so that none lands in another's frame: thermal state, drawer counts, NaN, shot.</summary>
        void Probes(float e)
        {
            if (!thermalRead) { thermal = PerfHost.Thermal(); thermalRead = true; return; }
            if (report.screenWidth == 0) { Describe(); return; } // once, in a settle frame (M7), the window has its size by now
            if (!probe) return;
            if (e >= 0.4f && !drawerOn) { drawer.Begin(); drawerOn = true; drawerFrame = Time.frameCount; }
            else if (e >= 0.7f && drawerOn && drawerStats == null && Time.frameCount >= drawerFrame + 3)
            {
                drawerStats = drawer.Read();
                if (drawerStats.cameraInstances == 0 && drawerStats.error == null && !drawerRetried) { drawerRetried = true; drawerStats = null; drawerOn = false; } // the stats' first frame after they are switched on is empty
            }
            else if (e >= 1.1f && drawerStats != null && nanPixels < 0) nanPixels = nan.Count();
            else if (e >= 1.3f && nanPixels >= 0 && !shot && shots != null && plan.passes[schedule[seg].pass].shots) Shoot();
        }

        /// <summary>The settle-phase reads this segment needs are in: thermal state, the window's description, and (probed) NaN and shot.</summary>
        bool ProbesDone => thermalRead && report.screenWidth != 0 &&
            (!probe || nanPixels >= 0 && (shot || shots == null || !plan.passes[schedule[seg].pass].shots));

        void Describe() { PerfHost.Describe(report); report.renderCounters = counters.Read(); }

        void Shoot()
        {
            shot = true;
            Directory.CreateDirectory(shots);
            var s = schedule[seg];
            if (report.look.Length == 0) report.look = PerfLook.Read(cam);
            ScreenCapture.CaptureScreenshot(Path.Combine(shots, ShotName(s)));
        }

        string ShotName(Segment s) => $"{s.id:000}-{plan.stops[s.stop].name}-{Safe(plan.variants[s.variant].name)}.png";

        static string Safe(string s) { foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s.Replace(' ', '_').Replace(':', '_').Replace(',', '_'); }

        /// <summary>vSync and the window for a pass (Redline M1); unchanged passes change nothing.</summary>
        void Pace(PerfPass pass)
        {
            if (pacing == pass) return;
            pacing = pass;
            QualitySettings.vSyncCount = pass.vSync;
            Application.targetFrameRate = -1;
            var mode = Enum.TryParse<FullScreenMode>(pass.window, out var m) ? m : FullScreenMode.FullScreenWindow;
            if (plan.width > 0 && (Screen.fullScreenMode != mode || Screen.width != plan.width || Screen.height != plan.height)) Screen.SetResolution(plan.width, plan.height, mode);
        }

        float Duration(Segment s)
        {
            var stop = plan.stops[s.stop];
            return stop.IsPath && stop.speed > 0f ? stop.path[^1].along / stop.speed : plan.passes[s.pass].recordSeconds;
        }

        void Park(PerfStop s) { if (s.IsPath) Along(s, 0f); else Place(s.focus, s.standOff, s.zoom); }

        /// <summary>The focus `d` metres along a pan's path (samples carry their stand-off, resolved in the Editor).</summary>
        void Along(PerfStop s, float d)
        {
            var p = s.path;
            int i = 0;
            while (i + 2 < p.Length && p[i + 1].along <= d) i++;
            float span = Mathf.Max(p[i + 1].along - p[i].along, 1e-4f), k = Mathf.Clamp01((d - p[i].along) / span);
            Place(Vector3.Lerp(p[i].focus, p[i + 1].focus, k), Mathf.Lerp(p[i].standOff, p[i + 1].standOff, k), s.zoom);
        }

        void Place(Vector3 focus, float standOff, float zoom)
        {
            var rot = Quaternion.Euler(plan.pitch, plan.yaw, 0f);
            cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * standOff, rot);
            cam.orthographicSize = zoom;
            cam.farClipPlane = standOff + plan.farMargin;
            shadows.focusDepth = standOff;
            fade.focus = focus;
        }

        void End(float seconds)
        {
            var s = schedule[seg];
            var r = Result(s, seconds);
            results.Add(r);
            csv.Write(s.id, recorder);
            if (seg + 1 < schedule.Count) Open(seg + 1); else Finish();
        }

        SegmentResult Result(Segment s, float seconds)
        {
            var stop = plan.stops[s.stop]; var pass = plan.passes[s.pass];
            int n = recorder.Frames;
            var frame = recorder.Frame(1);
            int missed = 0; double longest = 0;
            for (int i = 0; i < n; i++) { if (frame[i] > pass.missMs) missed++; longest = Math.Max(longest, frame[i]); }
            double P50(int column) => Metric.Of(recorder.Frame(column), n, scratch).p50;
            Metric T(int column) => Metric.Of(recorder.Timing(column), recorder.Timings, scratch);
            return new SegmentResult
            {
                id = s.id, repeat = s.repeat, repeatOf = s.repeatOf, pass = pass.name, stop = stop.name, variant = plan.variants[s.variant].name,
                focus = stop.IsPath ? stop.path[0].focus : stop.focus, standOff = stop.IsPath ? stop.path[0].standOff : stop.standOff,
                seconds = seconds, frames = n, fps = Metric.Round(n / Math.Max(seconds, 1e-3)), unfocusedFrames = unfocused, thermal = thermal, nanPixels = nanPixels,
                shot = shot ? ShotName(s) : null, drawer = drawerStats,
                settleSeconds = Metric.Round(settled), settleHitches = hitches, settleWorstMs = Metric.Round(worstHitch),
                frameMs = Metric.Of(frame, n, scratch), missedShare = Metric.Round(n > 0 ? (double)missed / n : double.NaN), longestStallMs = Metric.Round(longest),
                cpuFrameMs = T(1), cpuMainMs = T(2), cpuRenderMs = T(3), gpuMs = T(4), presentWaitMs = T(5),
                setPassP50 = P50(2), trianglesP50 = P50(3), verticesP50 = P50(4), shadowCastersP50 = P50(5), gcAllocP50 = P50(6),
            };
        }

        void Finish()
        {
            report.machineEnd = PerfHost.Machine("end");
            report.segments = results.ToArray();
            PerfSummary.Fill(report);
            Write(0);
        }

        void Fail(Exception e)
        {
            Debug.LogException(e);
            report.error = e.ToString();
            report.segments = results.ToArray();
            Write(1);
        }

        void Write(int exitCode)
        {
            phase = Phase.Done;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
                Debug.Log($"Perf: wrote {output}");
            }
            catch (Exception e) { Debug.LogException(e); exitCode = 1; }
            recorder?.Dispose(); csv?.Dispose(); nan?.Dispose();
            Application.Quit(exitCode);
        }
    }
}
