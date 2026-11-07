// suborned-sim dial (brief 098 step 0): the dial from the command line, for Latch's dev panel and Waymark's sweeps.
//   dial census                                        today's zone by kind: the people file's count, the floor (the cast) and the most, as JSON
//   dial sweep [plans | scripts | dirs] [--seeds n] [--seed-from k] [--settings "s;s;..."] [--no-replay] [--jobs n] [--out f]
//       every item on every seed under every setting, in one process on every core: the Handover's outcomes per setting, as information (no gate).
//       Items default to contracts/missions/handover/adaptive (Waymark's adaptive plans: they play what they see, so a changed world moves their orders).
//       A setting is "unset" (the start sets no counts), "today" (counts at today's numbers), or kind=n pairs ("guards=32,people=228"); x0.5, x2, x4
//       scale today's. Default: unset;today;x0.5;x2;x4. Exit 1 if any run errs or fails its replay; the outcomes never fail it.
//   dial perf [script] [--settings ...] [--runs n]      the tick's cost per setting on one thread: ms a tick, sim-seconds a wall-second, the start's cost
//   dial traffic [script] [--counts cars=8] [--seed n]  brief 098 step 1: one script under the dial's cars, every tick watched (Traffic.cs)
//   (a setting may name cars too: "cars=16" is today's guards and people with 16 cars; the sweep reports the mean cars on the zone's roads)
//   play <script> --counts guards=20,people=200         one script under those counts (its record is dropped); batch items take `#counts=guards:20,people:200`
//       and `batch ... --counts guards=20` gives every item those counts.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;

static class DialCli
{
    /// <summary>"guards=20,people=200" (or ':' for '='): kind → count, checked against the dial's kinds.</summary>
    public static SortedDictionary<string, int> Parse(string spec)
    {
        var d = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var part in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = part.Split(new[] { '=', ':' }, 2);
            if (kv.Length != 2 || Array.IndexOf(Population.Kinds, kv[0]) < 0) throw new ArgumentException($"counts: '{part}' is not kind=n (kinds: {string.Join(", ", Population.Kinds)})");
            int n = int.Parse(kv[1], NumberStyles.None, CultureInfo.InvariantCulture); if (n > Population.CountCap) throw new ArgumentException($"counts: {part} is over {Population.CountCap}");
            d[kv[0]] = n;
        }
        return d;
    }
    public static string Tag(IReadOnlyDictionary<string, int> c) => string.Join(",", c.Select(kv => kv.Key + ":" + kv.Value.ToString(CultureInfo.InvariantCulture)));
    /// <summary>Takes `#counts=...` out of an item; null when it has none.</summary>
    public static SortedDictionary<string, int>? Strip(ref string what)
    {
        int k = what.IndexOf("#counts=", StringComparison.Ordinal); if (k < 0) return null;
        int e = what.IndexOf('#', k + 1); string spec = e < 0 ? what.Substring(k + 8) : what.Substring(k + 8, e - k - 8);
        what = what.Remove(k, (e < 0 ? what.Length : e) - k);
        return Parse(spec);
    }
    public static void Apply(Start s, IReadOnlyDictionary<string, int> c) { s.Counts.Clear(); foreach (var kv in c) s.Counts[kv.Key] = kv.Value; }
    /// <summary>batch --counts spec: every item plays those counts.</summary>
    public static List<string> WithCounts(List<string> items, string[] args)
    {
        int k = Array.IndexOf(args, "--counts"); if (k < 0) return items;
        if (k + 1 >= args.Length) throw new ArgumentException("--counts which? (guards=20,people=200)");
        var tag = "#counts=" + Tag(Parse(args[k + 1]));
        return items.ConvertAll(x => x + tag);
    }

    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        string sub = args.Length > 1 ? args[1] : "";
        switch (sub)
        {
            case "census": Console.WriteLine(CensusJson(Population.Today(inputs))); return 0;
            case "sweep": return Sweep(inputs, root, args);
            case "perf": return Perf(inputs, root, args);
            case "traffic": return TrafficCli.Run(inputs, root, args); // brief 098 step 1: a script under the dial's cars, every tick watched (Traffic.cs)
            default: Console.Error.WriteLine("usage: dial census | dial sweep [items] [--seeds n] [--settings s;s] | dial perf [script] [--settings s;s] [--runs n] | dial traffic [script] [--counts cars=8] [--seed n]"); return 2;
        }
    }

    static string CensusJson(List<KindCensus> l)
    {
        var w = new JWriter().Obj().Prop("engine", Engine.Version).OpenArr("kinds");
        foreach (var c in l) w.Obj().Prop("kind", c.Kind).Prop("today", c.Today).Prop("floor", c.Floor).Prop("max", c.Max).EndObj();
        return w.EndArr().EndObj().ToString();
    }

    /// <summary>The settings a sweep or a perf plays: (name, counts or null for unset).</summary>
    static List<(string name, SortedDictionary<string, int>? counts)> Settings(Engine.Inputs inputs, string[] args, string fallback)
    {
        int k = Array.IndexOf(args, "--settings"); var spec = k > 0 && k + 1 < args.Length ? args[k + 1] : fallback;
        var today = Population.Today(inputs); var l = new List<(string, SortedDictionary<string, int>?)>();
        foreach (var s in spec.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (s == "unset") { l.Add((s, null)); continue; }
            if (s == "today" || s.StartsWith("x", StringComparison.Ordinal))
            {
                decimal f = s == "today" ? 1m : decimal.Parse(s.Substring(1), CultureInfo.InvariantCulture);
                var d = new SortedDictionary<string, int>(StringComparer.Ordinal); foreach (var c in today) d[c.Kind] = (int)Math.Round(c.Today * f, MidpointRounding.AwayFromZero);
                l.Add((s, d)); continue;
            }
            l.Add((s, Parse(s)));
        }
        return l;
    }

    static int Arg(string[] args, string name, int dflt) { int k = Array.IndexOf(args, name); return k > 0 && k + 1 < args.Length ? int.Parse(args[k + 1], CultureInfo.InvariantCulture) : dflt; }

    sealed class Row { public int Runs, Wins, Errors, ReplayBad, EndSum, AlertSum, TrailSum, Street, StreetN; public long CarSum, CarN, MissionWait; public readonly SortedDictionary<string, int> Fails = new SortedDictionary<string, int>(StringComparer.Ordinal), PlanWins = new SortedDictionary<string, int>(StringComparer.Ordinal), PlanRuns = new SortedDictionary<string, int>(StringComparer.Ordinal); public string Counts = ""; public readonly Jams J = new Jams(); }
    /// <summary>Brief 098 step 1b: what traffic did to the mission's cars across a setting's runs: the spells a mission car was held (by car and by
    /// what held it), the longest, the spells still held when the game ended; the spells a passing car boxed a mission car in, and those ticks with
    /// someone aboard; Go refused car-boxed by a passing car; closed-in with a passing car in the way.</summary>
    sealed class Jams
    {
        public int HoldRuns, Holds, HoldTicks, Longest, Over10s, OpenAtEnd, BoxRuns, Boxes, BoxTicks, BoxAboard, GoBoxed, GoBoxedRuns, ClosedIn, Shoves; public string LongestWhat = "";
        public readonly SortedDictionary<string, int> ByWhy = new SortedDictionary<string, int>(StringComparer.Ordinal), WhyTicks = new SortedDictionary<string, int>(StringComparer.Ordinal), ByCar = new SortedDictionary<string, int>(StringComparer.Ordinal), BoxCar = new SortedDictionary<string, int>(StringComparer.Ordinal);
        public static Jams Of(Game g, Adaptive.Extra? x, string plan) { var j = new Jams(); j.Add(g, x, plan); return j; }
        public void Merge(Jams o)
        {
            HoldRuns += o.HoldRuns; Holds += o.Holds; HoldTicks += o.HoldTicks; Over10s += o.Over10s; OpenAtEnd += o.OpenAtEnd; BoxRuns += o.BoxRuns; Boxes += o.Boxes; BoxTicks += o.BoxTicks; BoxAboard += o.BoxAboard;
            GoBoxed += o.GoBoxed; GoBoxedRuns += o.GoBoxedRuns; ClosedIn += o.ClosedIn; Shoves += o.Shoves; if (o.Longest > Longest) { Longest = o.Longest; LongestWhat = o.LongestWhat; }
            foreach (var (a, b) in new[] { (ByWhy, o.ByWhy), (WhyTicks, o.WhyTicks), (ByCar, o.ByCar), (BoxCar, o.BoxCar) }) foreach (var kv in b) a[kv.Key] = a.GetValueOrDefault(kv.Key) + kv.Value;
        }
        void Add(Game g, Adaptive.Extra? x, string plan)
        {
            if (g.MissionHolds.Count > 0) HoldRuns++;
            foreach (var h in g.MissionHolds)
            {
                Holds++; HoldTicks += h.Ticks; if (h.Ticks >= Game.T(10)) Over10s++; if (h.Open) OpenAtEnd++;
                ByWhy[h.Why] = ByWhy.GetValueOrDefault(h.Why) + 1; WhyTicks[h.Why] = WhyTicks.GetValueOrDefault(h.Why) + h.Ticks; ByCar[h.Car] = ByCar.GetValueOrDefault(h.Car) + 1;
                if (h.Ticks > Longest) { Longest = h.Ticks; LongestWhat = $"{h.Car} {h.Why} by {h.By} from {Report.Clock(h.From)} ({plan} #{g.Seed})"; }
                if (h.Ticks >= Game.T(10) && Environment.GetEnvironmentVariable("SUBORNED_SIM_HOLDS") == "1") Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  hold {h.Ticks / 10.0,6:0.0} s {h.Car} {h.Why} by {h.By} from {Report.Clock(h.From)}{(h.Open ? " (to the end)" : "")}: {plan} #{g.Seed} cars {g.Census.Find(c => c.Kind == Population.Cars)?.Count}")); // the long ones, for the law's evidence
            }
            if (g.MissionBoxes.Count > 0) BoxRuns++;
            foreach (var b in g.MissionBoxes) { Boxes++; BoxTicks += b.Ticks; BoxAboard += b.Aboard; BoxCar[b.Car] = BoxCar.GetValueOrDefault(b.Car) + 1; }
            int gb = x != null ? x.Refusals.GetValueOrDefault("go:car-boxed:traffic") : 0; GoBoxed += gb; if (gb > 0) GoBoxedRuns++;
            if (g.FailCode == "closed-in" && g.FailText.Contains("car:t-", StringComparison.Ordinal)) ClosedIn++;
            foreach (var l in g.Log) if (l.Code == "traffic.shoved") Shoves++;
        }
        public void Write(JWriter w, int runs)
        {
            w.Open("jams").Prop("runsHeld", HoldRuns).Prop("holds", Holds).Prop("holdS10", HoldTicks).Prop("longestS10", Longest).Prop("longest", LongestWhat).Prop("holds10sOrMore", Over10s).Prop("heldAtEnd", OpenAtEnd)
                .Prop("runsBoxed", BoxRuns).Prop("boxSpells", Boxes).Prop("boxedS10", BoxTicks).Prop("boxedAboardS10", BoxAboard).Prop("goBoxedByTraffic", GoBoxed).Prop("runsGoBoxedByTraffic", GoBoxedRuns).Prop("closedInByTraffic", ClosedIn).Prop("wrecksShoved", Shoves);
            w.Open("holdsByWhy"); foreach (var kv in ByWhy) w.Prop(kv.Key, kv.Value); w.EndObj(); w.Open("holdS10ByWhy"); foreach (var kv in WhyTicks) w.Prop(kv.Key, kv.Value); w.EndObj();
            w.Open("holdsByCar"); foreach (var kv in ByCar) w.Prop(kv.Key, kv.Value); w.EndObj(); w.Open("boxSpellsByCar"); foreach (var kv in BoxCar) w.Prop(kv.Key, kv.Value); w.EndObj(); w.EndObj();
        }
        public string Line(int runs) => runs == 0 || (Holds == 0 && Boxes == 0) ? "" : string.Create(CultureInfo.InvariantCulture,
            $"    held: {HoldRuns}/{runs} runs, {Holds} spells, {HoldTicks / 10.0 / runs:0.0} s a run, longest {Longest / 10.0:0} s ({LongestWhat}), {Over10s} of 10 s or more, {OpenAtEnd} still held at the end; by what: {string.Join(", ", ByWhy.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value))}\n    boxed in by traffic: {BoxRuns}/{runs} runs, {Boxes} spells, {BoxTicks / 10.0 / runs:0.0} s a run ({BoxAboard / 10.0 / runs:0.0} s with someone aboard); Go refused by traffic {GoBoxed} times in {GoBoxedRuns} runs; closed-in by traffic {ClosedIn}; wrecks shoved {Shoves}");
    }

    static int Sweep(Engine.Inputs inputs, string root, string[] args)
    {
        var items = new List<string>();
        for (int i = 2; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--no-replay") continue;
            if (a.StartsWith("--", StringComparison.Ordinal)) { i++; continue; }
            if (Directory.Exists(a)) { var fs = Directory.GetFiles(a, "*.json"); Array.Sort(fs, StringComparer.Ordinal); items.AddRange(fs); } else items.Add(a);
        }
        if (items.Count == 0) { var fs = Directory.GetFiles(System.IO.Path.Combine(root, "contracts/missions/handover/adaptive"), "*.adaptive.json"); Array.Sort(fs, StringComparer.Ordinal); items.AddRange(fs); }
        int seeds = Arg(args, "--seeds", 20), from = Arg(args, "--seed-from", 1); bool replay = Array.IndexOf(args, "--no-replay") < 0;
        var settings = Settings(inputs, args, "unset;today;x0.5;x2;x4");
        var jobs = new List<(int set, string item, int seed)>();
        for (int s = 0; s < settings.Count; s++) foreach (var it in items) for (int sd = from; sd < from + seeds; sd++) jobs.Add((s, it, sd));
        _ = Scenarios.All.Count;
        var sw = Stopwatch.StartNew();
        var res = Par.Map(jobs.Count, BatchCli.Jobs(args), j =>
        {
            var (s, it, sd) = jobs[j]; var c = settings[s].counts;
            var p = BatchCli.PlayOne(inputs, root, it + "#seed=" + sd.ToString(CultureInfo.InvariantCulture) + (c != null ? "#counts=" + Tag(c) : ""), replay);
            var g = p.G; int street = 0, n = 0;
            for (int t = 0; t <= Game.T(inputs.SiteData.Over); t += Game.T(30)) { foreach (var w in g.Walkers) if (g.CrowdPos(w, t).HasValue) street++; n++; } // on the street by routine, every 30 s
            return (ok: p.Same, res: g.Result, fail: g.FailCode, tick: g.Tick, alert: g.Alert, trail: g.Trail, street, n, census: g.DialOn ? string.Join(",", g.Census.Select(x => x.Kind + ":" + x.Count.ToString(CultureInfo.InvariantCulture))) : "",
                cars: g.TrafficZoneSum, carN: g.TrafficZoneTicks, wait: g.MissionWaits.Values.Sum(), jams: Jams.Of(g, p.Extra, System.IO.Path.GetFileName(it).Replace(".adaptive.json", ""))); // brief 098 step 1: the cars on the zone's roads (summed per tick) and the ticks the mission's cars waited for them; step 1b: its jams
        });
        var rows = new Row[settings.Count]; for (int s = 0; s < rows.Length; s++) rows[s] = new Row();
        int bad = 0;
        for (int j = 0; j < jobs.Count; j++)
        {
            var r = rows[jobs[j].set]; var plan = System.IO.Path.GetFileName(jobs[j].item).Replace(".adaptive.json", "").Replace(".json", ""); r.Runs++;
            r.PlanRuns[plan] = r.PlanRuns.GetValueOrDefault(plan) + 1;
            if (res[j].error != null) { r.Errors++; bad++; var key = "error: " + res[j].error!.Message; r.Fails[key] = r.Fails.GetValueOrDefault(key) + 1; continue; }
            var x = res[j].value;
            if (!x.ok) { r.ReplayBad++; bad++; }
            if (x.res == Outcome.Success) { r.Wins++; r.PlanWins[plan] = r.PlanWins.GetValueOrDefault(plan) + 1; } else { var key = x.fail.Length > 0 ? x.fail : x.res.ToString().ToLowerInvariant(); r.Fails[key] = r.Fails.GetValueOrDefault(key) + 1; }
            r.EndSum += x.tick; r.AlertSum += x.alert; r.TrailSum += x.trail; r.Street += x.street; r.StreetN += x.n; if (x.census.Length > 0) r.Counts = x.census; r.CarSum += x.cars; r.CarN += x.carN; r.MissionWait += x.wait;
            r.J.Merge(x.jams);
        }
        var w = new JWriter().Obj().Prop("engine", Engine.Version).Prop("seeds", seeds).Prop("seedFrom", from).Prop("replay", replay).Prop("items", items.Count).Prop("wallS", (long)sw.Elapsed.TotalSeconds).OpenArr("settings");
        Console.Error.WriteLine($"dial sweep: {jobs.Count} runs ({settings.Count} settings x {items.Count} items x {seeds} seeds) in {sw.Elapsed.TotalSeconds:0.0} s; {bad} errors or replay differences");
        Console.Error.WriteLine("setting      counts                         runs  wins   win%  street  cars  end   alert trail  fails (most first)");
        for (int s = 0; s < settings.Count; s++)
        {
            var r = rows[s]; int ok = Math.Max(1, r.Runs - r.Errors);
            w.Obj().Prop("setting", settings[s].name).Prop("counts", r.Counts).Prop("runs", r.Runs).Prop("wins", r.Wins).Prop("errors", r.Errors).Prop("replayDiffers", r.ReplayBad)
                .Prop("streetMean10", r.StreetN > 0 ? r.Street * 10L / r.StreetN : 0).Prop("carsMean10", r.CarN > 0 ? r.CarSum * 10L / r.CarN : 0).Prop("missionWaitMeanS10", r.MissionWait * 10L / ok / Game.Hz).Prop("endMeanS", r.EndSum / ok / Game.Hz).Prop("alertMean10", r.AlertSum * 10L / ok).Prop("trailMean10", r.TrailSum * 10L / ok);
            w.Open("fails"); foreach (var kv in r.Fails.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal)) w.Prop(kv.Key, kv.Value); w.EndObj();
            w.Open("planWins"); foreach (var kv in r.PlanRuns) w.Prop(kv.Key, r.PlanWins.GetValueOrDefault(kv.Key)); w.EndObj(); r.J.Write(w, r.Runs); w.EndObj();
            var fails = string.Join(", ", r.Fails.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(4).Select(kv => kv.Key + " " + kv.Value));
            Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{settings[s].name,-12} {(r.Counts.Length > 0 ? r.Counts : "(the people file)"),-30} {r.Runs,4} {r.Wins,5} {100.0 * r.Wins / Math.Max(1, r.Runs),6:0.0} {(r.StreetN > 0 ? (double)r.Street / r.StreetN : 0),6:0.0} {(r.CarN > 0 ? (double)r.CarSum / r.CarN : 0),5:0.0} {r.EndSum / ok / Game.Hz,5} {(double)r.AlertSum / ok,5:0.0} {(double)r.TrailSum / ok,5:0.0}  {fails}"));
            var jl = r.J.Line(ok); if (jl.Length > 0) Console.Error.WriteLine(jl);
        }
        var json = w.EndArr().EndObj().ToString();
        int oi = Array.IndexOf(args, "--out"); if (oi > 0) File.WriteAllText(args[oi + 1], json + "\n"); else Console.WriteLine(json);
        return bad == 0 ? 0 : 1;
    }

    static int Perf(Engine.Inputs inputs, string root, string[] args)
    {
        string script = args.Length > 2 && !args[2].StartsWith("--", StringComparison.Ordinal) ? args[2] : "contracts/missions/handover/routes/route-a.json";
        var sc = Script.Parse(File.ReadAllText(File.Exists(script) ? script : System.IO.Path.Combine(root, script)));
        int runs = Arg(args, "--runs", 10);
        var w = new JWriter().Obj().Prop("engine", Engine.Version).Prop("script", System.IO.Path.GetFileName(script)).Prop("runs", runs).OpenArr("settings");
        Console.Error.WriteLine("setting      walkers  start ms   ms a tick   p99 tick ms   sim-s per wall-s");
        var sets = Settings(inputs, args, "today;x2;x4");
        foreach (var (_, counts) in sets) for (int r = 0; r < 3; r++) { sc.Start.Counts.Clear(); if (counts != null) Apply(sc.Start, counts); Engine.Play(inputs, sc.Start, sc.Orders, sc.Until); } // warm: the JIT, and the shared legs (built once, on a setting's first game)
        foreach (var (name, counts) in sets)
        {
            var st = sc.Start; st.Counts.Clear(); if (counts != null) Apply(st, counts);
            double startMs = 0, stepMs = 0; long ticks = 0; var tickTimes = new List<double>(); int walkers = 0;
            for (int r = 0; r < runs; r++)
            {
                var t0 = Stopwatch.GetTimestamp(); var g = Engine.New(inputs, st); startMs += Stopwatch.GetElapsedTime(t0).TotalMilliseconds; walkers = g.Walkers.Count;
                var orders = sc.Orders.OrderBy(o => o.Tick).ToList(); int oi = 0, until = sc.Until > 0 ? sc.Until : Game.T(inputs.SiteData.Over);
                while (g.Tick < until && !g.Over)
                {
                    while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]);
                    var t1 = Stopwatch.GetTimestamp(); g.Step(); var ms = Stopwatch.GetElapsedTime(t1).TotalMilliseconds; stepMs += ms; ticks++; tickTimes.Add(ms);
                }
            }
            tickTimes.Sort(); double p99 = tickTimes.Count > 0 ? tickTimes[(int)(tickTimes.Count * 0.99)] : 0, per = ticks > 0 ? stepMs / ticks : 0;
            Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{name,-12} {walkers,7} {startMs / runs,9:0.0} {per,11:0.000} {p99,13:0.000} {(per > 0 ? 100.0 / per : 0),18:0}"));
            w.Obj().Prop("setting", name).Prop("walkers", walkers).Key("startMs").Raw((startMs / runs).ToString("0.0", CultureInfo.InvariantCulture)).Key("msPerTick").Raw(per.ToString("0.0000", CultureInfo.InvariantCulture)).Key("p99TickMs").Raw(p99.ToString("0.000", CultureInfo.InvariantCulture)).EndObj();
        }
        Console.WriteLine(w.EndArr().EndObj().ToString());
        return 0;
    }
}
