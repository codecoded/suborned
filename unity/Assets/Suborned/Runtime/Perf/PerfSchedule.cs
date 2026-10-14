using System;
using System.Collections.Generic;

namespace Suborned.Perf
{
    /// <summary>One recording: a stop under a variant, in a pass and repeat. `repeatOf` names the segment it repeats (A...A).</summary>
    public readonly struct Segment
    {
        public readonly int id, pass, repeat, stop, variant, repeatOf;
        public Segment(int id, int pass, int repeat, int stop, int variant, int repeatOf = -1)
        { this.id = id; this.pass = pass; this.repeat = repeat; this.stop = stop; this.variant = variant; this.repeatOf = repeatOf; }
    }

    /// <summary>The run's order (Redline M4, item 0): per pass, `repeats` rounds; round r starts at stop r (rotated) and, at each
    /// stop, starts the variants at r + the stop's place (rotated), so no variant always runs first or last; with repeatFirst the
    /// pass ends on its first segment again.</summary>
    public static class PerfSchedule
    {
        public static List<Segment> Build(PerfPlan plan, string onlyPass)
        {
            var list = new List<Segment>();
            for (int p = 0; p < plan.passes.Length; p++)
            {
                var pass = plan.passes[p];
                if (onlyPass != null && Array.IndexOf(onlyPass.Split(','), pass.name) < 0) continue;
                var stops = Pick(pass.stops, plan.stops, s => s.name, "stop", pass.name);
                var variants = Pick(pass.variants, plan.variants, v => v.name, "variant", pass.name);
                int first = list.Count;
                for (int r = 0; r < Math.Max(1, pass.repeats); r++)
                    for (int k = 0; k < stops.Count; k++)
                    {
                        int stop = stops[(r + k) % stops.Count];
                        for (int j = 0; j < variants.Count; j++) list.Add(new Segment(list.Count, p, r, stop, variants[(r + k + j) % variants.Count]));
                    }
                if (pass.repeatFirst && list.Count > first) { var a = list[first]; list.Add(new Segment(list.Count, p, a.repeat, a.stop, a.variant, a.id)); }
            }
            if (list.Count == 0) throw new InvalidOperationException(onlyPass != null ? $"Perf: no pass `{onlyPass}` in the plan" : "Perf: the plan has no passes");
            return list;
        }

        static List<int> Pick<T>(string[] names, T[] all, Func<T, string> name, string what, string pass)
        {
            var picked = new List<int>();
            if (names == null || names.Length == 0) { for (int i = 0; i < all.Length; i++) picked.Add(i); return picked; }
            foreach (var n in names)
            {
                int i = Array.FindIndex(all, x => name(x) == n);
                if (i < 0) throw new InvalidOperationException($"Perf: pass `{pass}` names {what} `{n}`, which the plan does not have");
                picked.Add(i);
            }
            return picked;
        }
    }
}
