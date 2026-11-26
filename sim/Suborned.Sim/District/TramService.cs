using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    // Left and right running rails, the actual Crown turning loop and dwell times at the
    // platforms. Cars share a timetable, so a player can leave and come back to the same service.
    internal sealed class TramService
    {
        readonly Path line;
        readonly long[] marks;
        readonly int[] starts;
        readonly int period;
        const int Pace = 730, Dwell = 90;
        public readonly int Count;

        public TramService(DistrictMap map)
        {
            var trunk = map.Trams.First(s => s.Id.EndsWith("/trunk", StringComparison.Ordinal)).Path;
            var loop = map.Trams.First(s => s.Id.EndsWith("/loop", StringComparison.Ordinal)).Path;
            var tracks = new List<Pt>();
            var source = trunk.Points;
            Pt Rail(int k, int side)
            {
                var heading = source[Math.Min(source.Length - 1, k + 1)] - source[Math.Max(0, k - 1)];
                return CarGeo.Ahead(source[k], new Pt(-heading.Y, heading.X), side * 180);
            }
            int last = source.Length - 1;
            int side = G.D2(Rail(last, 1), loop.Points[0]) < G.D2(Rail(last, -1), loop.Points[0]) ? 1 : -1;
            for (int k = 0; k <= last; k++) tracks.Add(Rail(k, side));
            tracks.AddRange(loop.Points);
            for (int k = last; k >= 0; k--) tracks.Add(Rail(k, -side));
            tracks.Add(tracks[0]);
            line = new Path(tracks.ToArray());
            marks = map.Ground.Platforms.Select(s => line.NearestMm(s.Centre, 0, line.Length)).Append(0L).Distinct().OrderBy(d => d).Append(line.Length).ToArray();
            starts = new int[marks.Length];
            for (int i = 1; i < marks.Length; i++) starts[i] = starts[i - 1] + Dwell + (int)((marks[i] - marks[i - 1] + Pace - 1) / Pace);
            period = starts[starts.Length - 1];
            Count = Math.Max(2, (int)(line.Length / 230000));
        }

        public DistrictTraffic.Pose At(int car, int tick)
        {
            int time = (int)(((long)tick + period * (long)car / Count + 17) % period);
            int i = Array.BinarySearch(starts, time);
            if (i < 0) i = ~i - 1;
            int elapsed = time - starts[i] - Dwell;
            long mm = marks[i] + Math.Max(0, elapsed) * Pace;
            return new DistrictTraffic.Pose(line.At(mm), line.Heading(mm), elapsed > 0);
        }
    }
}
