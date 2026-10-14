using System;
using UnityEngine;

namespace Suborned.Perf
{
    /// <summary>The run's JSON (JsonUtility shape): the machine and build it ran on, the power state at the run's edges, and per stop
    /// the percentiles of every sampled metric.</summary>
    [Serializable]
    public sealed class PerfReport
    {
        public string harness = "PerfHarness (brief 008 B6)", content, date, error;
        public string unityVersion, build, quality, pipeline, gpuResidentDrawer, commandLine;
        public int screenWidth, screenHeight, renderWidth, renderHeight, vSyncCount, targetFrameRate, memoryMB;
        public string fullScreenMode, device, gpu, graphicsApi, cpu, os, missingCounters;
        public bool frameTimingEnabled;
        public string fingerprint, exportSha256, sceneExportSha256, source;
        public float warmupSeconds, dwellSeconds, settleSeconds, recordSeconds, pitch, yaw;
        public PowerSample[] power = Array.Empty<PowerSample>();
        /// <summary>Every render counter the player offers, at the first recorded frame (PerfCounters).</summary>
        public string[] renderCounters = Array.Empty<string>();
        public StopResult[] stops = Array.Empty<StopResult>();
        /// <summary>Stop names with the highest p95 GPU ms, p95 CPU main-thread ms and p50 triangles.</summary>
        public string heaviestGpu, heaviestCpu, heaviestTriangles;
    }

    [Serializable]
    public sealed class PowerSample { public string when, therm, batt; }

    [Serializable]
    public sealed class StopResult
    {
        public string name, why;
        public Vector3 focus;
        public float zoom, standOff, seconds, fps;
        public int frames;
        public Metric[] metrics;

        public Metric Get(string metric) { foreach (var m in metrics) if (m.name == metric) return m; return null; }
    }

    /// <summary>Nearest-rank percentiles over the frames that have the value (frame timings repeat-free, so n may be below frames);
    /// -1 = no value. Counters read the previous profiler frame: now and then one holds two rendered frames (twice the count), so
    /// p50 is their figure.</summary>
    [Serializable]
    public sealed class Metric
    {
        public string name;
        public int n;
        public double p50, p95, p99, mean, max;

        public static Metric Of(string name, double[] values, int count)
        {
            var v = new double[count]; int n = 0; double sum = 0;
            for (int i = 0; i < count; i++) if (!double.IsNaN(values[i])) { v[n++] = values[i]; sum += values[i]; }
            Array.Sort(v, 0, n);
            double At(double p) => n == 0 ? double.NaN : v[Math.Clamp((int)Math.Ceiling(p * n) - 1, 0, n - 1)];
            return new Metric { name = name, n = n, p50 = Round(At(0.50)), p95 = Round(At(0.95)), p99 = Round(At(0.99)), mean = Round(n == 0 ? double.NaN : sum / n), max = Round(At(1.0)) };
        }

        static double Round(double x) => double.IsNaN(x) ? -1 : Math.Round(x, 3);
    }
}
