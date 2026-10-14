using System;
using UnityEngine;

namespace Suborned.Perf
{
    /// <summary>The run's JSON (JsonUtility shape): the machine and build it ran on (with the preflight's load check at both ends),
    /// the warm-up trace, every recorded segment, and the summary per pass, stop and variant over the repeats. Per-frame rows go to
    /// the two CSV files named here (framesCsv, timingsCsv: column `seg` is the segment id).</summary>
    [Serializable]
    public sealed class PerfReport
    {
        public string harness = "PerfHarness (brief 008 B6, Redline M1-M8 and item 0)", content, date, error;
        public string unityVersion, build, quality, pipeline, gpuResidentDrawer, commandLine, ssaoAsBuilt;
        public int screenWidth, screenHeight, renderWidth, renderHeight, memoryMB, proxyRenderers, proxyTriangles;
        public string fullScreenMode, device, gpu, graphicsApi, cpu, os, missingCounters;
        public bool frameTimingEnabled;
        public string fingerprint, exportSha256, sceneExportSha256, source;
        public float pitch, yaw;
        public PerfMachine machineStart, machineEnd;
        public WarmupResult warmup = new();
        public PerfPass[] passes = Array.Empty<PerfPass>();
        public PerfVariant[] variants = Array.Empty<PerfVariant>();
        public SegmentResult[] segments = Array.Empty<SegmentResult>();
        public SummaryRow[] summary = Array.Empty<SummaryRow>();
        /// <summary>Stops by frame p50, heaviest first: the first uncapped pass, its first variant (Redline M6: frame time, not GPU ms).</summary>
        public string[] ranking = Array.Empty<string>();
        public AaCheck[] repeatChecks = Array.Empty<AaCheck>();
        public string framesCsv, timingsCsv;
        /// <summary>Every render counter the player offers, at the first recorded frame (PerfCounters).</summary>
        public string[] renderCounters = Array.Empty<string>();
    }

    /// <summary>The machine's load and power at one end of the run (Redline M2). busy = any warning; with `-perfStrict` a busy
    /// machine refuses the run.</summary>
    [Serializable]
    public sealed class PerfMachine
    {
        public string when, therm, batt;
        /// <summary>NSProcessInfo.thermalState: 0 nominal, 1 fair, 2 serious, 3 critical, -1 unread.</summary>
        public int thermal = -1;
        public bool lowPower, onAC, busy;
        public string[] warnings = Array.Empty<string>(), top = Array.Empty<string>();
    }

    [Serializable]
    public sealed class WarmupResult
    {
        public float seconds;
        public int cycles;
        /// <summary>Whether two full cycles in a row agreed within the plan's plateau before maxSeconds.</summary>
        public bool settled;
        public double lastChange;
        public WarmSample[] trace = Array.Empty<WarmSample>();
    }

    /// <summary>One dwell of the warm-up (or, with stop "cycle", one full cycle of the stops).</summary>
    [Serializable]
    public sealed class WarmSample { public float t; public string stop; public double frameP50; public int frames, thermal; }

    [Serializable]
    public sealed class SegmentResult
    {
        public int id, repeat, repeatOf = -1, frames, unfocusedFrames, thermal = -1, nanPixels = -1;
        public string pass, stop, variant, shot;
        public Vector3 focus;
        public float standOff, seconds;
        public double fps;
        public Metric frameMs, cpuFrameMs, cpuMainMs, cpuRenderMs, gpuMs, presentWaitMs;
        /// <summary>Share of frames longer than the pass's missMs (capped: missed vblanks), and the longest frame.</summary>
        public double missedShare, longestStallMs;
        /// <summary>Render counters, p50 only (they read the previous profiler frame; now and then one holds two frames).</summary>
        public double setPassP50, trianglesP50, verticesP50, shadowCastersP50, gcAllocP50;
        public DrawerStats drawer;
        /// <summary>How long the segment settled (until QuietSeconds without a hitch and its probes were in), its hitches and the worst.</summary>
        public double settleSeconds, settleWorstMs;
        public int settleHitches;
    }

    /// <summary>The GPU Resident Drawer's own culling counters for one frame (camera view, and the shadow splits), read in the
    /// settle phase (Redline M5: the player's Draw Calls and Batches counters read 0 under the drawer).</summary>
    [Serializable]
    public sealed class DrawerStats
    {
        public string error;
        public long cameraInstances, cameraTriangles, cameraDraws, shadowInstances, shadowTriangles, shadowDraws;
        public int shadowSplits;
        public string splits;
    }

    /// <summary>A pass, stop and variant over its repeats: the median of the segments' values, and the range of their frame p50.</summary>
    [Serializable]
    public sealed class SummaryRow
    {
        public string pass, stop, variant;
        /// <summary>Repeats folded in, and those left out because the player lost focus for over 1% of their frames (macOS may then
        /// throttle or stop presenting); when every repeat lost focus they are all used and unfocusedUsed says so.</summary>
        public int repeats, unfocusedLeftOut, unfocusedUsed;
        public double frameP50, frameP50Min, frameP50Max, frameP95, frameP99, missedShare, longestStallMs;
        public double gpuP50, cpuMainP50, cpuMainP95, trianglesP50, setPassP50, shadowCastersP50;
        /// <summary>Frame p50 against the pass's first variant at the same stop, as a share (-0.2 = 20% faster); NaN-free: 0 for the first.</summary>
        public double vsFirst;
        public long drawerCameraTriangles, drawerShadowTriangles;
        public int nanPixels = -1;
    }

    /// <summary>The A...A check (Redline M3): the pass's first segment recorded again at its end.</summary>
    [Serializable]
    public sealed class AaCheck { public string pass, stop, variant; public double firstP50, lastP50, change; public bool within3Percent; }

    /// <summary>Nearest-rank percentiles over the values present (NaN = absent); -1 = no value.</summary>
    [Serializable]
    public sealed class Metric
    {
        public int n;
        public double p50, p95, p99, mean, max;

        public static Metric Of(double[] values, int count, double[] scratch)
        {
            int n = 0; double sum = 0;
            for (int i = 0; i < count; i++) if (!double.IsNaN(values[i])) { scratch[n++] = values[i]; sum += values[i]; }
            Array.Sort(scratch, 0, n);
            double At(double p) => n == 0 ? double.NaN : scratch[Math.Clamp((int)Math.Ceiling(p * n) - 1, 0, n - 1)];
            return new Metric { n = n, p50 = Round(At(0.50)), p95 = Round(At(0.95)), p99 = Round(At(0.99)), mean = Round(n == 0 ? double.NaN : sum / n), max = Round(At(1.0)) };
        }

        public static double Round(double x) => double.IsNaN(x) ? -1 : Math.Round(x, 3);
    }
}
