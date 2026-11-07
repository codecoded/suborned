// Vantage's probe for brief 098's site round, kept here as the evidence's source (it is not built from here): one more file in a scratch copy of sim/cli (out/scratch/vantage/probe), with `case "regions": return RegionsCli.Run(inputs, root, args);` added to Dial.cs: `vantage-probe dial regions` plays the adaptive plans under the dial's
// settings, as `dial bodies` does, and bins the law's give and take by place. A region is a box in the block frame (cm). Per region and setting:
// held spells (a spell is binned where the body stood when it ended: count, p50, p95, max, 5 s or more, 10 s or more), held body-ticks, the
// bodies that passed, squeeze tick-pairs, making room (each start) and changes of plans (each start). Outcomes per setting as in the sweep.
//   dial regions [items] [--seeds n] [--seed-from k] [--settings "s;s"] [--regions "name:x0,x1,y0,y1;..."] [--jobs n] [--out f]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;

static class RegionsCli
{
    sealed class Reg { public string Name = ""; public int X0, X1, Y0, Y1; public bool In(Pt p) => p.X >= X0 && p.X <= X1 && p.Y >= Y0 && p.Y <= Y1; }
    sealed class Acc
    {
        public readonly List<int> Spells = new List<int>(); public long HeldTicks, BodyTicks; public int Sqz, Room, Replan; public readonly HashSet<string> Ids = new HashSet<string>(StringComparer.Ordinal);
        public readonly List<(int len, string what)> Long = new List<(int, string)>();
        public readonly Dictionary<long, long> Heat = new Dictionary<long, long>();
    }
    sealed class Res { public Acc[] A = Array.Empty<Acc>(); public string Outcome = "", Fail = ""; }

    static List<Reg> Regions(string[] args)
    {
        int k = Array.IndexOf(args, "--regions");
        string spec = k > 0 ? args[k + 1] : "office-door:7500,8050,-3820,-2380;cafe-front:-1070,-500,-4000,-1800;cafe-alley:-650,-500,-3700,-3300";
        var l = new List<Reg>();
        foreach (var part in spec.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var nv = part.Split(':'); var n = nv[1].Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            l.Add(new Reg { Name = nv[0], X0 = n[0], X1 = n[1], Y0 = n[2], Y1 = n[3] });
        }
        return l;
    }
    static int Arg(string[] args, string name, int dflt) { int k = Array.IndexOf(args, name); return k > 0 && k + 1 < args.Length ? int.Parse(args[k + 1], CultureInfo.InvariantCulture) : dflt; }
    static string Name(string p) => System.IO.Path.GetFileName(p).Replace(".adaptive.json", "").Replace(".json", "");

    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        var items = new List<string>();
        for (int i = 2; i < args.Length; i++) { var a = args[i]; if (a.StartsWith("--", StringComparison.Ordinal)) { i++; continue; } if (Directory.Exists(a)) { var fs = Directory.GetFiles(a, "*.json"); Array.Sort(fs, StringComparer.Ordinal); items.AddRange(fs); } else items.Add(File.Exists(a) ? a : System.IO.Path.Combine(root, a)); }
        if (items.Count == 0) { var fs = Directory.GetFiles(System.IO.Path.Combine(root, "contracts/missions/handover/adaptive"), "*.adaptive.json"); Array.Sort(fs, StringComparer.Ordinal); items.AddRange(fs); }
        int seeds = Arg(args, "--seeds", 3), from = Arg(args, "--seed-from", 1);
        var sets = DialCli.Settings(inputs, args, "unset;people=64;x4"); var regs = Regions(args);
        var jobs = new List<(int set, string item, int seed)>(); for (int s = 0; s < sets.Count; s++) foreach (var it in items) for (int sd = from; sd < from + seeds; sd++) jobs.Add((s, it, sd));
        var res = Par.Map(jobs.Count, BatchCli.Jobs(args), j => Watch(inputs, root, jobs[j].item, jobs[j].seed, sets[jobs[j].set].counts, regs, $"{Name(jobs[j].item)} #{jobs[j].seed}"));
        var w = new JWriter().Obj().Prop("engine", Engine.Version).Prop("seeds", seeds).Prop("items", items.Count).OpenArr("settings"); int errs = 0;
        for (int s = 0; s < sets.Count; s++)
        {
            int runs = 0, wins = 0; var fails = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var tot = regs.Select(_ => new Acc()).ToArray();
            for (int j = 0; j < jobs.Count; j++)
            {
                if (jobs[j].set != s) continue; runs++;
                if (res[j].error != null) { errs++; Console.Error.WriteLine($"{Name(jobs[j].item)} #{jobs[j].seed}: {res[j].error!.Message}"); continue; }
                var r = res[j].value!; if (Environment.GetEnvironmentVariable("VANTAGE_RUNS") != null) Console.Error.WriteLine($"RUN {sets[s].name} {Name(jobs[j].item)} #{jobs[j].seed} {r.Outcome} {r.Fail}"); if (r.Outcome == "success") wins++; else { var f = r.Fail.Length > 0 ? r.Fail : r.Outcome; fails[f] = fails.GetValueOrDefault(f) + 1; }
                for (int k = 0; k < regs.Count; k++) { var a = r.A[k]; var t = tot[k]; t.Spells.AddRange(a.Spells); t.HeldTicks += a.HeldTicks; t.BodyTicks += a.BodyTicks; t.Sqz += a.Sqz; t.Room += a.Room; t.Replan += a.Replan; foreach (var id in a.Ids) t.Ids.Add(id + "@" + j); t.Long.AddRange(a.Long); foreach (var kv in a.Heat) t.Heat[kv.Key] = t.Heat.GetValueOrDefault(kv.Key) + kv.Value; }
            }
            Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{sets[s].name,-12} runs {runs} wins {wins} ({100.0 * wins / Math.Max(1, runs):0.0} %)  fails {string.Join(", ", fails.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value))}"));
            w.Obj().Prop("setting", sets[s].name).Prop("runs", runs).Prop("wins", wins); w.Open("fails"); foreach (var kv in fails) w.Prop(kv.Key, kv.Value); w.EndObj(); w.OpenArr("regions");
            for (int k = 0; k < regs.Count; k++)
            {
                var t = tot[k]; t.Spells.Sort(); double P(int q) => t.Spells.Count == 0 ? 0 : t.Spells[Math.Min(t.Spells.Count - 1, (int)((long)t.Spells.Count * q / 100))] / (double)Game.Hz;
                int o5 = t.Spells.Count(x => x >= Game.T(5)), o10 = t.Spells.Count(x => x >= Game.T(10)), mx = t.Spells.Count == 0 ? 0 : t.Spells[^1];
                t.Long.Sort((a, b) => b.len.CompareTo(a.len));
                Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"    {regs[k].Name,-12} bodies {t.Ids.Count / (double)Math.Max(1, runs),6:0.0} a run, held {t.HeldTicks / (double)Game.Hz / Math.Max(1, runs),6:0.0} body-s a run ({100.0 * t.HeldTicks / Math.Max(1, t.BodyTicks):0.00} % of body-ticks); spells {t.Spells.Count}: p50 {P(50):0.0} s, p95 {P(95):0.0} s, max {mx / (double)Game.Hz:0.0} s, {o5} of 5 s+, {o10} of 10 s+; squeeze tick-pairs {t.Sqz}, made room {t.Room}, changed plans {t.Replan}"));
                foreach (var l in t.Long.Take(3)) Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"        {l.len / (double)Game.Hz:0.0} s {l.what}"));
                w.Obj().Prop("region", regs[k].Name).Prop("box", $"{regs[k].X0},{regs[k].X1},{regs[k].Y0},{regs[k].Y1}").Prop("bodiesARunX10", (int)Math.Round(10.0 * t.Ids.Count / Math.Max(1, runs))).Prop("heldBodyTicks", t.HeldTicks).Prop("bodyTicks", t.BodyTicks)
                    .Prop("spells", t.Spells.Count).Prop("p50S10", (int)Math.Round(P(50) * 10)).Prop("p95S10", (int)Math.Round(P(95) * 10)).Prop("maxS10", mx * 10 / Game.Hz).Prop("held5sOrMore", o5).Prop("held10sOrMore", o10)
                    .Prop("squeezePairTicks", t.Sqz).Prop("madeRoom", t.Room).Prop("changedPlans", t.Replan);
                if (Environment.GetEnvironmentVariable("VANTAGE_HEAT") is string hdir) { using var hf = new StreamWriter(System.IO.Path.Combine(hdir, $"heat-{sets[s].name}-{regs[k].Name}.tsv")); foreach (var kv in t.Heat) hf.WriteLine($"{(int)(kv.Key >> 32)}\t{(int)(uint)kv.Key}\t{kv.Value}"); }
                w.OpenArr("longest"); foreach (var l in t.Long.Take(8)) w.Str(string.Create(CultureInfo.InvariantCulture, $"{l.len / (double)Game.Hz:0.0} s {l.what}")); w.EndArr().EndObj();
            }
            w.EndArr().EndObj();
        }
        var json = w.EndArr().EndObj().ToString(); int oi = Array.IndexOf(args, "--out"); if (oi > 0) File.WriteAllText(args[oi + 1], json + "\n"); else Console.WriteLine(json);
        return errs == 0 ? 0 : 1;
    }

    static Res Watch(Engine.Inputs inputs, string root, string item, int seed, SortedDictionary<string, int>? counts, List<Reg> regs, string tag)
    {
        Script sc;
        if (item.EndsWith(".adaptive.json", StringComparison.Ordinal))
        {
            var pl = Adaptive.Play(inputs, Adaptive.Load(item, root), seed, out _, counts);
            sc = Script.Parse(new JWriter().Obj().Prop("schema", "suborned.sim.script/1").Prop("mission", "handover").Key("start").Raw(pl.Start.Canonical()).OpenArr("orders").EndArr().EndObj().ToString());
            foreach (var (o, _, _, _) in pl.Orders) sc.Orders.Add(o); sc.Until = pl.Tick;
        }
        else { sc = Script.Parse(File.ReadAllText(item)); sc.Start.Seed = seed; sc.Start.Counts.Clear(); if (counts != null) DialCli.Apply(sc.Start, counts); }
        var g = Engine.New(inputs, sc.Start); var r = new Res { A = regs.Select(_ => new Acc()).ToArray() };
        var orders = sc.Orders.Select((o, k) => (o, k)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).Select(x => x.o).ToList(); int oi = 0, until = sc.Until > 0 ? sc.Until : Game.T(inputs.SiteData.Over);
        var stuck = new Dictionary<string, int>(StringComparer.Ordinal); var stuckNow = new Dictionary<string, int>(StringComparer.Ordinal);
        var pos = new Dictionary<string, Pt>(StringComparer.Ordinal); var room = new HashSet<string>(StringComparer.Ordinal); var roomNow = new HashSet<string>(StringComparer.Ordinal);
        var alt = new HashSet<string>(StringComparer.Ordinal); var altNow = new HashSet<string>(StringComparer.Ordinal); int min = g.R.BodyMinCm;
        int RegOf(Pt p) { for (int k = 0; k < regs.Count; k++) if (regs[k].In(p)) return k; return -1; }
        void look()
        {
            var up = BodyCheck.Of(g);
            foreach (var u in up) { pos[u.Id] = u.At; int k = RegOf(u.At); if (k >= 0) { r.A[k].BodyTicks++; r.A[k].Ids.Add(u.Id); } }
            g.BodyStuckInto(stuckNow);
            foreach (var kv in stuckNow) if (pos.TryGetValue(kv.Key, out var p) && RegOf(p) is int k && k >= 0) { r.A[k].HeldTicks++; long key = ((long)Math.Floor(p.X / 50.0) << 32) ^ (uint)(int)Math.Floor(p.Y / 50.0); r.A[k].Heat[key] = r.A[k].Heat.GetValueOrDefault(key) + 1; }
            foreach (var kv in stuck) if (!stuckNow.TryGetValue(kv.Key, out var n) || n < kv.Value) { if (pos.TryGetValue(kv.Key, out var p) && RegOf(p) is int k && k >= 0) { r.A[k].Spells.Add(kv.Value); if (kv.Value >= Game.T(5)) r.A[k].Long.Add((kv.Value, $"{kv.Key} at {p} to {Report.Clock(g.Tick)} ({tag})")); } }
            (stuck, stuckNow) = (stuckNow, stuck);
            foreach (var (a, b, cm) in BodyCheck.Closer(up, min)) if (BodyCheck.Squeezing(g, a, b, cm) && RegOf(a.At) is int k && k >= 0) r.A[k].Sqz++;
            roomNow.Clear(); altNow.Clear();
            foreach (var u in up)
            {
                var m = g.BodyMove(u.Id); if (m == null) continue; int k = RegOf(u.At);
                if (m.Value.RoomFor != null) { roomNow.Add(u.Id); if (!room.Contains(u.Id) && k >= 0) r.A[k].Room++; }
                if (m.Value.Replanned) { altNow.Add(u.Id); if (!alt.Contains(u.Id) && k >= 0) r.A[k].Replan++; }
            }
            (room, roomNow) = (roomNow, room); (alt, altNow) = (altNow, alt);
        }
        look();
        while (g.Tick < until && !g.Over) { while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]); g.Step(); look(); }
        while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]);
        g.Finish(); r.Outcome = g.Result.ToString().ToLowerInvariant(); r.Fail = g.FailCode;
        foreach (var kv in stuck) if (pos.TryGetValue(kv.Key, out var p) && RegOf(p) is int k && k >= 0) { r.A[k].Spells.Add(kv.Value); if (kv.Value >= Game.T(5)) r.A[k].Long.Add((kv.Value, $"{kv.Key} at {p} to the end ({tag})")); }
        return r;
    }
}
