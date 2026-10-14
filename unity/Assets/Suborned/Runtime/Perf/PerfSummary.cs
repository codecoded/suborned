using System;
using System.Collections.Generic;

namespace Suborned.Perf
{
    /// <summary>Folds the run's segments into the report's summary: one row per pass, stop and variant (medians over the repeats, the
    /// range of frame p50), each variant against the pass's first at the same stop, the stops ranked by frame p50 (Redline M6), and
    /// the A...A checks (M3). Repeats that lost focus are left out of a row when it has focused ones.</summary>
    public static class PerfSummary
    {
        /// <summary>The player held focus for 99% of the segment's frames or more.</summary>
        static bool Focused(SegmentResult s) => s.unfocusedFrames * 100 <= s.frames;

        public static void Fill(PerfReport report)
        {
            var groups = new Dictionary<(string, string, string), List<SegmentResult>>();
            var order = new List<(string pass, string stop, string variant)>();
            var byId = new Dictionary<int, SegmentResult>();
            foreach (var s in report.segments)
            {
                byId[s.id] = s;
                if (s.repeatOf >= 0) continue;
                var key = (s.pass, s.stop, s.variant);
                if (!groups.TryGetValue(key, out var list)) { groups[key] = list = new List<SegmentResult>(); order.Add(key); }
                list.Add(s);
            }
            var rows = new List<SummaryRow>();
            var first = new Dictionary<(string, string), double>();
            var firstVariant = new Dictionary<string, string>();
            foreach (var key in order)
            {
                var all = groups[key];
                var list = all.FindAll(Focused);
                if (list.Count == 0) list = all;
                var row = new SummaryRow
                {
                    pass = key.pass, stop = key.stop, variant = key.variant, repeats = list.Count,
                    unfocusedLeftOut = all.Count - list.Count, unfocusedUsed = list.FindAll(s => !Focused(s)).Count,
                    frameP50 = Median(list, s => s.frameMs.p50), frameP50Min = Min(list, s => s.frameMs.p50), frameP50Max = Max(list, s => s.frameMs.p50),
                    frameP95 = Median(list, s => s.frameMs.p95), frameP99 = Median(list, s => s.frameMs.p99),
                    missedShare = Median(list, s => s.missedShare), longestStallMs = Max(list, s => s.longestStallMs),
                    gpuP50 = Median(list, s => s.gpuMs.p50), cpuMainP50 = Median(list, s => s.cpuMainMs.p50), cpuMainP95 = Median(list, s => s.cpuMainMs.p95),
                    trianglesP50 = Median(list, s => s.trianglesP50), setPassP50 = Median(list, s => s.setPassP50), shadowCastersP50 = Median(list, s => s.shadowCastersP50),
                };
                foreach (var s in list)
                {
                    if (s.nanPixels >= 0) row.nanPixels = Math.Max(row.nanPixels, s.nanPixels);
                    if (s.drawer != null && string.IsNullOrEmpty(s.drawer.error) && row.drawerCameraTriangles == 0) { row.drawerCameraTriangles = s.drawer.cameraTriangles; row.drawerShadowTriangles = s.drawer.shadowTriangles; }
                }
                if (!firstVariant.ContainsKey(key.pass)) firstVariant[key.pass] = PassFirstVariant(report, key.pass) ?? key.variant;
                if (key.variant == firstVariant[key.pass]) first[(key.pass, key.stop)] = row.frameP50;
                rows.Add(row);
            }
            foreach (var row in rows)
                row.vsFirst = first.TryGetValue((row.pass, row.stop), out var b) && b > 0 ? Math.Round(row.frameP50 / b - 1.0, 4) : 0;
            report.summary = rows.ToArray();

            var ranked = new List<SummaryRow>();
            var uncapped = Array.Find(report.passes, p => p.vSync == 0 && Array.Exists(report.segments, s => s.pass == p.name));
            if (uncapped != null)
                foreach (var r in rows) if (r.pass == uncapped.name && r.variant == firstVariant.GetValueOrDefault(r.pass)) ranked.Add(r);
            ranked.Sort((a, b) => b.frameP50.CompareTo(a.frameP50));
            report.ranking = ranked.ConvertAll(r => $"{r.stop}: frame p50 {r.frameP50} ms, p95 {r.frameP95} ms ({r.pass}, {r.variant})").ToArray();

            var checks = new List<AaCheck>();
            foreach (var s in report.segments)
            {
                if (s.repeatOf < 0 || !byId.TryGetValue(s.repeatOf, out var a)) continue;
                double change = a.frameMs.p50 > 0 ? Math.Round(s.frameMs.p50 / a.frameMs.p50 - 1.0, 4) : double.NaN;
                checks.Add(new AaCheck { pass = s.pass, stop = s.stop, variant = s.variant, firstP50 = a.frameMs.p50, lastP50 = s.frameMs.p50, change = change, within3Percent = Math.Abs(change) <= 0.03 });
            }
            report.repeatChecks = checks.ToArray();
        }

        static string PassFirstVariant(PerfReport report, string pass)
        {
            var p = Array.Find(report.passes, x => x.name == pass);
            if (p == null) return null;
            if (p.variants is { Length: > 0 }) return p.variants[0];
            return report.variants.Length > 0 ? report.variants[0].name : null;
        }

        static double Median(List<SegmentResult> list, Func<SegmentResult, double> f)
        {
            var v = new List<double>();
            foreach (var s in list) { double x = f(s); if (x >= 0) v.Add(x); }
            if (v.Count == 0) return -1;
            v.Sort();
            return v.Count % 2 == 1 ? v[v.Count / 2] : Math.Round((v[v.Count / 2 - 1] + v[v.Count / 2]) / 2, 3);
        }

        static double Min(List<SegmentResult> list, Func<SegmentResult, double> f) { double m = double.MaxValue; foreach (var s in list) m = Math.Min(m, f(s)); return list.Count > 0 ? m : -1; }
        static double Max(List<SegmentResult> list, Func<SegmentResult, double> f) { double m = double.MinValue; foreach (var s in list) m = Math.Max(m, f(s)); return list.Count > 0 ? m : -1; }
    }
}
