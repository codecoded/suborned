using System;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace Suborned.Perf
{
    /// <summary>Per-frame rows for one recording, into buffers allocated once (nothing is allocated while recording, so the GC counter
    /// reads the world, not the harness). Two tables (Redline M4): frame rows, one per Update (wall frame time, the render counters of
    /// the previous profiler frame), and timing rows, one per frame FrameTimingManager reports (CPU, render thread, GPU and present
    /// wait for that frame, keyed by its frameStartTimestamp; they arrive a few frames late, so every new one is taken, none twice).
    /// The first `Skip` rows of each table are the harness's own frame and are dropped (M7). On Metal the GPU time is the span of
    /// the frame's command buffers, which overlap on a tile GPU, so it can exceed the wall frame time: an upper bound; frameMs is
    /// what the player shows: the wall-clock interval between Updates, not Time.unscaledDeltaTime, which under vSync reports the
    /// refresh interval (16.667 ms) even when frames take 17-50 ms (seen in an occluded window).</summary>
    public sealed class PerfRecorder : IDisposable
    {
        public const int Skip = 3;
        // These are separate native counters in the Unity 6.6 Metal player. SetPass counts
        // shader changes and the legacy aggregate batch counter is unavailable in this build.
        public static readonly (string column, string marker)[] DrawPaths = {
            ("standard", "Standard Draw Calls Count"), ("instanced", "Standard Instanced Draw Calls Count"),
            ("indirect", "Standard Indirect Draw Calls Count"), ("srpBatcher", "SRP Batcher Draw Calls Count"),
            ("brg", "BRG Draw Calls Count"), ("brgIndirect", "BRG Indirect Draw Calls Count"),
            ("nullGeometry", "Null Geometry Draw Calls Count"), ("nullGeometryIndirect", "Null Geometry Indirect Draw Calls Count")
        };
        public static readonly string[] FrameNames = new[] { "tMs", "frameMs", "setPassCalls", "triangles", "vertices", "shadowCasters", "gcAllocBytes" }
            .Concat(DrawPaths.Select(p => p.column + "Draws")).ToArray();
        public static readonly string[] TimingNames = { "startMs", "cpuFrameMs", "cpuMainMs", "cpuRenderMs", "gpuMs", "presentWaitMs" };
        static readonly (ProfilerCategory cat, string name)[] Counters = new (ProfilerCategory, string)[]
        {
            (ProfilerCategory.Render, "SetPass Calls Count"), (ProfilerCategory.Render, "Triangles Count"),
            (ProfilerCategory.Render, "Vertices Count"), (ProfilerCategory.Render, "Shadow Casters Count"),
            (ProfilerCategory.Memory, "GC Allocated In Frame"),
        }.Concat(DrawPaths.Select(p => (ProfilerCategory.Render, p.marker))).ToArray();

        readonly ProfilerRecorder[] recorders = new ProfilerRecorder[Counters.Length];
        readonly double[][] frames, timings;
        readonly FrameTiming[] latest = new FrameTiming[8];
        readonly double tickMs = 1000.0 / FrameTimingManager.GetCpuTimerFrequency();
        ulong lastStart, firstStart;
        double begun, last;
        int seen, timed;

        public int Frames { get; private set; }
        public int Timings { get; private set; }
        public double[] Frame(int column) => frames[column];
        public long MedianDraws(int path)
        {
            var values = frames[FrameNames.Length - DrawPaths.Length + path].Take(Frames)
                .Where(double.IsFinite).OrderBy(x => x).ToArray();
            return values.Length == 0 ? -1 : (long)values[values.Length / 2];
        }
        public double[] Timing(int column) => timings[column];

        public PerfRecorder(int capacity)
        {
            frames = new double[FrameNames.Length][];
            timings = new double[TimingNames.Length][];
            for (int i = 0; i < frames.Length; i++) frames[i] = new double[capacity];
            for (int i = 0; i < timings.Length; i++) timings[i] = new double[capacity];
            for (int i = 0; i < Counters.Length; i++) recorders[i] = ProfilerRecorder.StartNew(Counters[i].cat, Counters[i].name);
        }

        /// <summary>Which counters this player does not provide (an empty string when all are there).</summary>
        public string Missing()
        {
            var s = "";
            for (int i = 0; i < Counters.Length; i++) if (!recorders[i].Valid) s += (s.Length > 0 ? ", " : "") + Counters[i].name;
            return s;
        }

        public void Begin()
        {
            Frames = Timings = seen = timed = 0; firstStart = 0;
            begun = last = Time.realtimeSinceStartupAsDouble;
            Tick(); // timings from before the recording are not its own
        }

        /// <summary>Once per frame, from Update, while the stop records.</summary>
        public void Sample()
        {
            double now = Time.realtimeSinceStartupAsDouble, interval = (now - last) * 1000.0;
            last = now;
            if (seen++ >= Skip && Frames < frames[0].Length)
            {
                int n = Frames++;
                frames[0][n] = (now - begun) * 1000.0;
                frames[1][n] = interval;
                for (int i = 0; i < recorders.Length; i++) frames[2 + i][n] = recorders[i].Valid ? recorders[i].LastValue : double.NaN;
            }
            FrameTimingManager.CaptureFrameTimings();
            uint got = FrameTimingManager.GetLatestTimings((uint)latest.Length, latest);
            while (true) // every new timing, oldest first (whatever order the manager returns them in)
            {
                int k = -1;
                for (int j = 0; j < got; j++)
                    if (latest[j].frameStartTimestamp > lastStart && (k < 0 || latest[j].frameStartTimestamp < latest[k].frameStartTimestamp)) k = j;
                if (k < 0) break;
                var t = latest[k];
                lastStart = t.frameStartTimestamp;
                if (timed++ < Skip || Timings >= timings[0].Length) continue;
                if (firstStart == 0) firstStart = t.frameStartTimestamp;
                int n = Timings++;
                timings[0][n] = (t.frameStartTimestamp - firstStart) * tickMs;
                timings[1][n] = t.cpuFrameTime; timings[2][n] = t.cpuMainThreadFrameTime; timings[3][n] = t.cpuRenderThreadFrameTime;
                timings[4][n] = t.gpuFrameTime; timings[5][n] = t.cpuMainThreadPresentWaitTime;
            }
        }

        /// <summary>Keeps FrameTimingManager's history moving between recordings (settle, warm-up), and marks what it has seen.</summary>
        public void Tick()
        {
            FrameTimingManager.CaptureFrameTimings();
            uint got = FrameTimingManager.GetLatestTimings((uint)latest.Length, latest);
            for (int j = 0; j < got; j++) if (latest[j].frameStartTimestamp > lastStart) lastStart = latest[j].frameStartTimestamp;
        }

        public void Dispose() { foreach (var r in recorders) r.Dispose(); }
    }
}
