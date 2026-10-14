using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Suborned.Perf
{
    /// <summary>The per-frame rows of every recording (Redline M4), as two CSV files beside the report: `.frames.csv` (one row per
    /// Update: time since the recording began, wall frame ms, render counters) and `.timings.csv` (one row per frame timing:
    /// frame start since the recording's first, CPU, main, render thread, GPU and present wait ms). Column `seg` is the segment id
    /// in the report. Written between recordings, never during one.</summary>
    public sealed class PerfFrames : IDisposable
    {
        readonly StreamWriter frames, timings;
        readonly StringBuilder line = new(256);

        public string FramesPath { get; }
        public string TimingsPath { get; }

        public PerfFrames(string reportPath)
        {
            var stem = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath)) ?? ".", Path.GetFileNameWithoutExtension(reportPath));
            FramesPath = stem + ".frames.csv"; TimingsPath = stem + ".timings.csv";
            frames = new StreamWriter(FramesPath, false, new UTF8Encoding(false));
            timings = new StreamWriter(TimingsPath, false, new UTF8Encoding(false));
            frames.WriteLine("seg," + string.Join(",", PerfRecorder.FrameNames));
            timings.WriteLine("seg," + string.Join(",", PerfRecorder.TimingNames));
        }

        public void Write(int seg, PerfRecorder r)
        {
            Rows(frames, seg, r.Frames, PerfRecorder.FrameNames.Length, r.Frame);
            Rows(timings, seg, r.Timings, PerfRecorder.TimingNames.Length, r.Timing);
        }

        void Rows(StreamWriter w, int seg, int count, int columns, Func<int, double[]> column)
        {
            for (int i = 0; i < count; i++)
            {
                line.Clear().Append(seg);
                for (int c = 0; c < columns; c++)
                {
                    double v = column(c)[i];
                    line.Append(',');
                    if (!double.IsNaN(v)) line.Append(Math.Round(v, 3).ToString(CultureInfo.InvariantCulture));
                }
                w.WriteLine(line);
            }
            w.Flush();
        }

        public void Dispose() { frames.Dispose(); timings.Dispose(); }
    }
}
