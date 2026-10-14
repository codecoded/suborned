using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;

namespace Suborned.Perf
{
    /// <summary>Every count-type render counter this player offers, read once (the first recorded frame of the run): which counters
    /// carry the draw work is player- and drawer-dependent (with the GPU Resident Drawer, "Draw Calls Count" and "Batches Count" can
    /// read 0 in a player), so the report lists them all rather than trusting one name.</summary>
    public sealed class PerfCounters
    {
        readonly List<(string name, ProfilerRecorder rec)> all = new();

        public PerfCounters()
        {
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (var h in handles)
            {
                var d = ProfilerRecorderHandle.GetDescription(h);
                if (d.UnitType != ProfilerMarkerDataUnit.Count || d.Category.Name != ProfilerCategory.Render.Name) continue;
                var rec = new ProfilerRecorder(h, 1, ProfilerRecorderOptions.Default);
                if (!rec.IsRunning) rec.Start();
                all.Add((d.Name, rec));
            }
        }

        /// <summary>"name = last frame's value" for each counter, sorted by name; the recorders are released.</summary>
        public string[] Read()
        {
            var lines = new List<string>(all.Count);
            foreach (var (name, rec) in all) { lines.Add($"{name} = {(rec.Valid ? rec.LastValue : -1)}"); rec.Dispose(); }
            all.Clear();
            lines.Sort(System.StringComparer.Ordinal);
            return lines.ToArray();
        }
    }
}
