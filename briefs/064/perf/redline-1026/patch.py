# Redline scratch only: instrument a COPY of the engine's Game.StepInner with per-call timers and add a `tickprof` CLI command.
import re, sys, pathlib
root = pathlib.Path(sys.argv[1])
g = root / "sim/Suborned.Sim/Game.cs"
s = g.read_text()
a = s.index("void StepInner()"); b = s.index("public void RunTo", a)
body = s[a:b]
# 1) `if (!Over) Name();` -> timed
body2, n1 = re.subn(r"if \(!Over\) (\w+)\(\);", lambda m: f'if (!Over) {{ var __t = System.Diagnostics.Stopwatch.GetTimestamp(); {m.group(1)}(); Prof.Add("{m.group(1)}", __t); }}', body)
# 2) timelines line
def tl(m):
    calls = re.findall(r"(\w+)\(\);", m.group(1))
    return "if (!Range) { " + " ".join(f'{{ var __t = System.Diagnostics.Stopwatch.GetTimestamp(); {c}(); Prof.Add("{c}", __t); }}' for c in calls) + " }"
body3, n2 = re.subn(r"if \(!Range\) \{ ((?:\w+\(\); )+)\}", tl, body2, count=1)
# 3) the ops loop: time the first foreach (op step) as a block
i = body3.index("foreach (var op in Ops)")
body3 = body3[:i] + 'var __ops = System.Diagnostics.Stopwatch.GetTimestamp(); ' + body3[i:]
j = body3.index("foreach (var op in Ops) if (op.CarriedBy.HasValue)")
body3 = body3[:j] + 'Prof.Add("OpsStep", __ops); ' + body3[j:]
# 4) FightEnds + checkpoint hash
body3 = body3.replace("FightEnds();", '{ var __t = System.Diagnostics.Stopwatch.GetTimestamp(); FightEnds(); Prof.Add("FightEnds", __t); }', 1)
body3 = body3.replace("if (Tick % 100 == 0 || Over) Checkpoints.Add((Tick, Hash.State(this)));", '{ var __t = System.Diagnostics.Stopwatch.GetTimestamp(); if (Tick % 100 == 0 || Over) Checkpoints.Add((Tick, Hash.State(this))); Prof.Add("Checkpoint", __t); }', 1)
s = s[:a] + body3 + s[b:]
# whole step
s = s.replace("try { StepInner(); }", 'try { var __s = System.Diagnostics.Stopwatch.GetTimestamp(); StepInner(); Prof.EndTick(Tick, __s); }', 1)
g.write_text(s)
(root / "sim/Suborned.Sim/ZProf.cs").write_text('''namespace Suborned.Sim {
public static class Prof {
  public static bool On;
  public static readonly System.Collections.Generic.Dictionary<string, long> Cur = new(), Sum = new(), Max = new();
  public static readonly System.Collections.Generic.List<(int tick, long total, string top, long topT)> Ticks = new();
  public static void Add(string k, long t0) { if (!On) return; long d = System.Diagnostics.Stopwatch.GetTimestamp() - t0; Cur[k] = (Cur.TryGetValue(k, out var v) ? v : 0) + d; }
  public static void EndTick(int tick, long t0) { if (!On) return; long d = System.Diagnostics.Stopwatch.GetTimestamp() - t0; string top = ""; long tt = 0;
    foreach (var kv in Cur) { Sum[kv.Key] = (Sum.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value; if (!Max.TryGetValue(kv.Key, out var m) || kv.Value > m) Max[kv.Key] = kv.Value; if (kv.Value > tt) { tt = kv.Value; top = kv.Key; } }
    Cur.Clear(); Ticks.Add((tick, d, top, tt)); }
}}
''')
p = root / "sim/cli/Program.cs"; ps = p.read_text()
ps = ps.replace('case "perf":', 'case "tickprof": return TickProf.Run(inputs, root, args);\n                case "perf":', 1)
p.write_text(ps)
(root / "sim/cli/ZTickProf.cs").write_text('''using System; using System.Linq; using System.Globalization; using Suborned.Sim;
static class TickProf {
  public static int Run(Engine.Inputs inputs, string root, string[] args) {
    string what = args[1]; int reps = int.Parse(args[2], CultureInfo.InvariantCulture);
    BatchCli.PlayOne(inputs, root, what); BatchCli.PlayOne(inputs, root, what); // warm (JIT)
    Prof.Sum.Clear(); Prof.Max.Clear(); Prof.Ticks.Clear(); Prof.On = true;
    var sw = System.Diagnostics.Stopwatch.StartNew(); int endTick = 0; string res = "";
    for (int i = 0; i < reps; i++) { var p = BatchCli.PlayOne(inputs, root, what); endTick = p.G.Tick; res = p.G.Result + " " + p.G.FailCode; }
    sw.Stop(); Prof.On = false;
    double us = 1e6 / System.Diagnostics.Stopwatch.Frequency;
    var t = Prof.Ticks.Select(x => x.total * us).OrderBy(x => x).ToArray();
    double P(double q) => t[Math.Min(t.Length - 1, (int)Math.Ceiling(q * t.Length) - 1)];
    Console.WriteLine(FormattableString.Invariant($"{what} x{reps}: end tick {endTick} ({res}); wall {sw.Elapsed.TotalMilliseconds / reps:0.0} ms a play (incl. load/verify); Step: {t.Length} ticks, mean {t.Average():0.0} us, p50 {P(0.5):0.0}, p99 {P(0.99):0.0}, p999 {P(0.999):0.0}, max {t[^1]:0.0} us; step sum {t.Sum() / 1000 / reps:0.00} ms a play"));
    foreach (var kv in Prof.Sum.OrderByDescending(k => k.Value).Take(14)) Console.WriteLine(FormattableString.Invariant($"  {kv.Key,-18} {kv.Value * us / 1000 / reps,8:0.000} ms a play  {kv.Value * us / t.Length,8:0.00} us a tick  max {Prof.Max[kv.Key] * us,8:0.0} us"));
    // per-tick median over reps of the worst ticks
    var byTick = Prof.Ticks.GroupBy(x => x.tick).Select(gr => (tick: gr.Key, med: gr.Select(x => x.total * us).OrderBy(x => x).ElementAt(gr.Count() / 2), top: gr.GroupBy(x => x.top).OrderByDescending(z => z.Count()).First().Key)).OrderByDescending(x => x.med).Take(8);
    Console.WriteLine("  worst ticks (median over reps): " + string.Join(", ", byTick.Select(x => FormattableString.Invariant($"{x.tick}:{x.med:0}us({x.top})"))));
    return 0;
  }
}
''')
print("patched", root, n1, n2)
