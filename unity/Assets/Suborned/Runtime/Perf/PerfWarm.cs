using System;
using System.Collections.Generic;

namespace Suborned.Perf
{
    /// <summary>The warm-up's trace and its end (Redline M3): the frame p50 of every dwell (about 10 s) and of every full cycle of the
    /// stops, with the thermal state; done when two cycles in a row agree within the plateau share after minSeconds, or at
    /// maxSeconds (then `settled` is false and the report says so).</summary>
    public sealed class PerfWarm
    {
        readonly PerfWarmup spec;
        readonly double[] dwell, cycle, scratch;
        readonly List<WarmSample> trace = new();
        int nd, nc, cycles;
        double previous = double.NaN, change = double.NaN;
        bool settled;

        public PerfWarm(PerfWarmup spec, int stops, int maxFps)
        {
            this.spec = spec;
            dwell = new double[(int)(spec.dwellSeconds * maxFps) + 1];
            cycle = new double[dwell.Length * Math.Max(1, stops)];
            scratch = new double[cycle.Length];
        }

        public void Frame(double ms)
        {
            if (nd < dwell.Length) dwell[nd++] = ms;
            if (nc < cycle.Length) cycle[nc++] = ms;
        }

        public void EndDwell(float t, string stop, int thermal)
        {
            trace.Add(new WarmSample { t = t, stop = stop, frames = nd, frameP50 = Metric.Of(dwell, nd, scratch).p50, thermal = thermal });
            nd = 0;
        }

        /// <summary>Closes a full cycle of the stops; true when the warm-up is over.</summary>
        public bool EndCycle(float t, int thermal)
        {
            double p50 = Metric.Of(cycle, nc, scratch).p50;
            trace.Add(new WarmSample { t = t, stop = "cycle", frames = nc, frameP50 = p50, thermal = thermal });
            nc = 0; cycles++;
            if (!double.IsNaN(previous) && previous > 0) change = Math.Abs(p50 - previous) / previous;
            previous = p50;
            settled = t >= spec.minSeconds && cycles >= 2 && change < spec.plateau;
            return settled || t >= spec.maxSeconds;
        }

        public WarmupResult Result(float seconds) => new() { seconds = seconds, cycles = cycles, settled = settled, lastChange = Metric.Round(change), trace = trace.ToArray() };
    }
}
