using System;
using Unity.Profiling;
using UnityEngine;

namespace Suborned.Perf
{
    /// <summary>Per-frame samples for one stop, into buffers allocated once (nothing is allocated while recording, so the GC counter
    /// reads the world, not the harness). Frame timings come from FrameTimingManager (always on in a development player; the GPU
    /// time lags the frame by a few frames, so a timing already seen is not counted twice), render and memory counters from
    /// ProfilerRecorder (the previous frame's value). On Metal the GPU time is the span of the frame's command buffers, which overlap
    /// on a tile GPU, so it can exceed the wall frame time (frameMs): read it as an upper bound; frameMs is what the player shows.</summary>
    public sealed class PerfRecorder : IDisposable
    {
        public static readonly string[] Names =
        {
            "frameMs", "cpuFrameMs", "cpuMainMs", "cpuRenderMs", "gpuMs", "presentWaitMs",
            "drawCalls", "setPassCalls", "batches", "triangles", "vertices", "gcAllocBytes",
        };
        const int Timed = 6; // Names[1..5] come from FrameTimingManager; Names[Timed..] from the counters
        static readonly (ProfilerCategory cat, string name)[] Counters =
        {
            (ProfilerCategory.Render, "Draw Calls Count"), (ProfilerCategory.Render, "SetPass Calls Count"),
            (ProfilerCategory.Render, "Batches Count"), (ProfilerCategory.Render, "Triangles Count"),
            (ProfilerCategory.Render, "Vertices Count"), (ProfilerCategory.Memory, "GC Allocated In Frame"),
        };

        readonly ProfilerRecorder[] recorders = new ProfilerRecorder[Counters.Length];
        readonly double[][] data;
        readonly FrameTiming[] timing = new FrameTiming[1];
        ulong lastFrame;

        public int Count { get; private set; }
        public double[] Column(int i) => data[i];

        public PerfRecorder(int capacity)
        {
            data = new double[Names.Length][];
            for (int i = 0; i < data.Length; i++) data[i] = new double[capacity];
            for (int i = 0; i < Counters.Length; i++) recorders[i] = ProfilerRecorder.StartNew(Counters[i].cat, Counters[i].name);
        }

        /// <summary>Which counters this player does not provide (an empty string when all are there).</summary>
        public string Missing()
        {
            var s = "";
            for (int i = 0; i < Counters.Length; i++) if (!recorders[i].Valid) s += (s.Length > 0 ? ", " : "") + Counters[i].name;
            return s;
        }

        public void Begin() => Count = 0;

        /// <summary>Once per frame, from Update, while the stop records.</summary>
        public void Sample()
        {
            FrameTimingManager.CaptureFrameTimings();
            if (Count >= data[0].Length) return;
            int n = Count++;
            data[0][n] = Time.unscaledDeltaTime * 1000.0;
            bool fresh = FrameTimingManager.GetLatestTimings(1, timing) > 0 && timing[0].frameStartTimestamp != lastFrame;
            if (fresh) lastFrame = timing[0].frameStartTimestamp;
            var t = timing[0];
            data[1][n] = fresh ? t.cpuFrameTime : double.NaN;
            data[2][n] = fresh ? t.cpuMainThreadFrameTime : double.NaN;
            data[3][n] = fresh ? t.cpuRenderThreadFrameTime : double.NaN;
            data[4][n] = fresh ? t.gpuFrameTime : double.NaN;
            data[5][n] = fresh ? t.cpuMainThreadPresentWaitTime : double.NaN;
            for (int i = 0; i < recorders.Length; i++) data[Timed + i][n] = recorders[i].Valid ? recorders[i].LastValue : double.NaN;
        }

        /// <summary>Keeps FrameTimingManager's history moving between recordings (settle, warm-up).</summary>
        public static void Tick() => FrameTimingManager.CaptureFrameTimings();

        public void Dispose() { foreach (var r in recorders) r.Dispose(); }
    }
}
