// suborned-sim dial bodies (brief 098, law L9 Bodies): scripts and adaptive plans played under the dial's settings with every tick watched by the
// law's independent check (BodyCheck: every upright body as the host reads it), for the law's evidence.
//   dial bodies [items] [--seeds n] [--seed-from k] [--settings "s;s"] [--jobs n] [--show n] [--out f]
//     items: scripts or *.adaptive.json plans (default: contracts/missions/handover/adaptive); settings as dial sweep's (default
//     "unset;people=32;people=64;guards=32;guards=64;cars=8;x4"). Per setting: runs, ticks watched; jumps (a body's tick-to-tick move over its own
//     pace, BodyCheck.Moves; with its own place jumping too, or the law's); tick-pairs inside two radii (a body not a tenet in it, not a counted
//     squeeze: the law's failures; tenet with tenet: the tenets' own; squeezes, which must stand two squeeze widths apart); how long bodies are held
//     (spells of ticks got nowhere: p50, p95, max) and held where they stood; the give and take (made room, squeezed, changed plans, by what); a
//     body leaving the street far from its own place; the farthest from its own place; outcomes; engine 0.47.1, the law's own speed: drawn moves
//     faster than a walk (the city's) and than its own place moved that tick, by the law's why, and moves straight back while its own place did
//     not turn (information, no gate). SUBORNED_JUMP_LOG=f appends each jump over 1 m.
//     Exit 1 if any run errs, any body jumps or any pair with a body not a tenet stands inside two radii outside a counted squeeze.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;

static class BodiesCli
{
    sealed class Res
    {
        public int Ticks, Bad, Tenet, Sqz, SqzMin = int.MaxValue, Min = int.MaxValue, LawOverlap, LawTenet, Bodies, Held, CellHeld, Far, Far5, Jumps, JumpsOwn, WorstOver, Sidesteps, Squeezes, Replans, Leaves, LeavesFar, LeaveMax; public long Moves;
        public string MinWhat = "", Outcome = "", Fail = "", FarWhat = "", LeaveWhat = "";
        public readonly List<string> First = new List<string>(), TenetFirst = new List<string>(), JumpFirst = new List<string>();
        public readonly SortedDictionary<string, int> JumpKinds = new SortedDictionary<string, int>(StringComparer.Ordinal), ReplansBy = new SortedDictionary<string, int>(StringComparer.Ordinal), LeaveKinds = new SortedDictionary<string, int>(StringComparer.Ordinal);
        public readonly List<int> Spells = new List<int>(), SpellsCity = new List<int>(), SpellsCell = new List<int>(), SpellsArmed = new List<int>(); public int LongSpell; public string LongWhat = ""; public readonly List<(int cm, string what)> Big = new List<(int, string)>();
        /// <summary>Engine 0.47.1, the law's own speed: drawn moves faster than a walk and than its own place moved that tick (by class, by the law's
        /// why), the worst; moves straight back the way it moved the tick before while its own place did not turn (and those faster than a walk).</summary>
        public int Fast, FastCity, FastCell, FastArmed, FastWorst, Back, BackFast; public string FastWhat = ""; public readonly SortedDictionary<string, int> FastBy = new SortedDictionary<string, int>(StringComparer.Ordinal);
        /// <summary>Drawn as a run by the host (over RunDrawnCmT a tick) while its own place moved no faster: a run the law made (by class, by why).</summary>
        public int Run, RunCity, RunCell, RunArmed; public readonly SortedDictionary<string, int> RunBy = new SortedDictionary<string, int>(StringComparer.Ordinal);
    }
    /// <summary>The host draws a figure running above 2.0 m/s (unity HostViews.Figures.cs, the walk and run clips' switch): 20 cm a tick.</summary>
    const int RunDrawnCmT = 20;
    /// <summary>Two moves more than 150° apart.</summary>
    static bool Opposed(Pt a, Pt b) { long dot = (long)a.X * b.X + (long)a.Y * b.Y, la = G.Isqrt(G.D2(a, default)), lb = G.Isqrt(G.D2(b, default)); return dot * 1000 < -866L * la * lb; }

    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        var items = new List<string>();
        for (int i = 2; i < args.Length; i++) { var a = args[i]; if (a.StartsWith("--", StringComparison.Ordinal)) { i++; continue; } if (Directory.Exists(a)) { var fs = Directory.GetFiles(a, "*.json"); Array.Sort(fs, StringComparer.Ordinal); items.AddRange(fs); } else items.Add(File.Exists(a) ? a : System.IO.Path.Combine(root, a)); }
        if (items.Count == 0) { var fs = Directory.GetFiles(System.IO.Path.Combine(root, "contracts/missions/handover/adaptive"), "*.adaptive.json"); Array.Sort(fs, StringComparer.Ordinal); items.AddRange(fs); }
        int seeds = Arg(args, "--seeds", 3), from = Arg(args, "--seed-from", 1), show = Arg(args, "--show", 3);
        var sets = DialCli.Settings(inputs, args, "unset;people=32;people=64;guards=32;guards=64;cars=8;x4"); // the sweep's settings (x4: four times today's guards and people)
        var jobs = new List<(int set, string item, int seed)>(); for (int s = 0; s < sets.Count; s++) foreach (var it in items) for (int sd = from; sd < from + seeds; sd++) jobs.Add((s, it, sd));
        var sw = Stopwatch.StartNew(); int min = inputs.Rules.BodyMinCm;
        var res = Par.Map(jobs.Count, BatchCli.Jobs(args), j => Watch(inputs, root, jobs[j].item, jobs[j].seed, sets[jobs[j].set].counts, min, show));
        int bad = 0; var w = new JWriter().Obj().Prop("engine", Engine.Version).Prop("minCm", min).Prop("squeezeCm", inputs.Rules.BodySqueezeCm).Prop("seeds", seeds).Prop("items", items.Count).OpenArr("settings");
        Console.Error.WriteLine($"dial bodies: {jobs.Count} runs ({sets.Count} settings x {items.Count} items x {seeds} seeds) in {sw.Elapsed.TotalSeconds:0.0} s; two radii = {min} cm, two squeezes = {2 * inputs.Rules.BodySqueezeCm} cm");
        for (int s = 0; s < sets.Count; s++)
        {
            var t = new Res(); int runs = 0, errs = 0, wins = 0; long bodies = 0, held = 0, cellHeld = 0, moves = 0; var firsts = new List<string>(); var tenets = new List<string>(); var jfirst = new List<string>(); var fails = new SortedDictionary<string, int>(StringComparer.Ordinal); var big = new List<(int cm, string what)>(); var spells = new List<int>(); var byCls = new[] { new List<int>(), new List<int>(), new List<int>() };
            for (int j = 0; j < jobs.Count; j++)
            {
                if (jobs[j].set != s) continue; runs++; string tag = $"{Name(jobs[j].item)} #{jobs[j].seed}";
                if (res[j].error != null) { errs++; bad++; firsts.Add($"{tag}: error {res[j].error!.Message}"); continue; }
                var r = res[j].value!;
                t.Ticks += r.Ticks; t.Bad += r.Bad; t.Tenet += r.Tenet; t.Sqz += r.Sqz; t.SqzMin = Math.Min(t.SqzMin, r.SqzMin); t.LawOverlap += r.LawOverlap; t.LawTenet += r.LawTenet; t.Far5 += r.Far5; t.Jumps += r.Jumps; t.JumpsOwn += r.JumpsOwn; t.WorstOver = Math.Max(t.WorstOver, r.WorstOver);
                t.Sidesteps += r.Sidesteps; t.Squeezes += r.Squeezes; t.Replans += r.Replans; t.Leaves += r.Leaves; t.LeavesFar += r.LeavesFar; bodies += r.Bodies; held += r.Held; cellHeld += r.CellHeld; moves += r.Moves; spells.AddRange(r.Spells); byCls[0].AddRange(r.SpellsCity); byCls[1].AddRange(r.SpellsCell); byCls[2].AddRange(r.SpellsArmed);
                foreach (var kv in r.JumpKinds) t.JumpKinds[kv.Key] = t.JumpKinds.GetValueOrDefault(kv.Key) + kv.Value; foreach (var kv in r.ReplansBy) t.ReplansBy[kv.Key] = t.ReplansBy.GetValueOrDefault(kv.Key) + kv.Value; foreach (var kv in r.LeaveKinds) t.LeaveKinds[kv.Key] = t.LeaveKinds.GetValueOrDefault(kv.Key) + kv.Value;
                t.Run += r.Run; t.RunCity += r.RunCity; t.RunCell += r.RunCell; t.RunArmed += r.RunArmed; foreach (var kv in r.RunBy) t.RunBy[kv.Key] = t.RunBy.GetValueOrDefault(kv.Key) + kv.Value;
                t.Fast += r.Fast; t.FastCity += r.FastCity; t.FastCell += r.FastCell; t.FastArmed += r.FastArmed; t.Back += r.Back; t.BackFast += r.BackFast; foreach (var kv in r.FastBy) t.FastBy[kv.Key] = t.FastBy.GetValueOrDefault(kv.Key) + kv.Value; if (r.FastWorst > t.FastWorst) { t.FastWorst = r.FastWorst; t.FastWhat = $"{r.FastWhat} ({tag})"; }
                foreach (var f in r.JumpFirst) if (jfirst.Count < show * 4) jfirst.Add($"{tag}: {f}"); foreach (var bj in r.Big) big.Add((bj.cm, $"{tag}: {bj.what}"));
                if (r.Far > t.Far) { t.Far = r.Far; t.FarWhat = $"{r.FarWhat} ({tag})"; }
                if (r.LongSpell > t.LongSpell) { t.LongSpell = r.LongSpell; t.LongWhat = $"{r.LongWhat} ({tag})"; }
                if (r.LeaveMax > t.LeaveMax) { t.LeaveMax = r.LeaveMax; t.LeaveWhat = $"{r.LeaveWhat} ({tag})"; }
                if (r.Outcome == "success") wins++; else fails[r.Fail.Length > 0 ? r.Fail : r.Outcome] = fails.GetValueOrDefault(r.Fail.Length > 0 ? r.Fail : r.Outcome) + 1;
                if (r.Min < t.Min) { t.Min = r.Min; t.MinWhat = $"{r.MinWhat} ({tag})"; }
                if (r.Bad > 0 || r.Jumps > 0) { bad++; foreach (var f in r.First) if (firsts.Count < show * 3) firsts.Add($"{tag}: {f}"); }
                foreach (var f in r.TenetFirst) if (tenets.Count < show * 2) tenets.Add($"{tag}: {f}");
            }
            spells.Sort(); double Pct(int p) => spells.Count == 0 ? 0 : spells[Math.Min(spells.Count - 1, (int)((long)spells.Count * p / 100))] / (double)Game.Hz;
            int over5 = spells.Count(x => x >= Game.T(5)), over10 = spells.Count(x => x >= Game.T(10)), smax = spells.Count == 0 ? 0 : spells[^1];
            string ClsLine(List<int> l) { l.Sort(); if (l.Count == 0) return "none"; double P(int q) => l[Math.Min(l.Count - 1, (int)((long)l.Count * q / 100))] / (double)Game.Hz; return string.Create(CultureInfo.InvariantCulture, $"{l.Count} spells, p50 {P(50):0.0} s, p95 {P(95):0.0} s, max {l[^1] / (double)Game.Hz:0.0} s, {l.Count(x => x >= Game.T(5))} of 5 s or more"); }
            Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{sets[s].name,-14} runs {runs,4}  wins {wins,4}  ticks {t.Ticks,8}  bodies/tick {(double)bodies / Math.Max(1, t.Ticks),6:0.0}  errors {errs}\n" +
                $"               jumps {t.Jumps} of {moves} moves ({t.JumpsOwn} with its own place jumping too), worst {t.WorstOver} cm over its pace\n" +
                $"               inside two radii: {t.Bad} tick-pairs (tenet with tenet {t.Tenet}); squeezes {t.Sqz} tick-pairs, closest {(t.SqzMin == int.MaxValue ? "-" : t.SqzMin + " cm")}; law's own overlaps {t.LawOverlap}, tenets {t.LawTenet}; closest pair {(t.Min == int.MaxValue ? "-" : t.Min + " cm")} {t.MinWhat}\n" +
                $"               held spells {spells.Count}: p50 {Pct(50):0.0} s, p95 {Pct(95):0.0} s, max {smax / (double)Game.Hz:0.0} s ({t.LongWhat}); {over5} of 5 s or more, {over10} of 10 s or more; held where it stood {(double)held / Math.Max(1, t.Ticks):0.000} a tick (cell {(double)cellHeld / Math.Max(1, runs) / Game.Hz:0.00} s a run)\n" +
                $"               held by class: the city's people {ClsLine(byCls[0])}; the cell {ClsLine(byCls[1])}; the armed and the cast {ClsLine(byCls[2])}\n" +
                $"               made room {t.Sidesteps} ({(double)t.Sidesteps / Math.Max(1, runs):0.0} a run), squeezed {t.Squeezes} ({(double)t.Squeezes / Math.Max(1, runs):0.0}), changed plans {t.Replans} ({(double)t.Replans / Math.Max(1, runs):0.0}: {string.Join(", ", t.ReplansBy.Select(kv => kv.Key + " " + kv.Value))})\n" +
                $"               faster than a walk and its own step: {t.Fast} moves (the city's {t.FastCity}, the cell {t.FastCell}, the armed and the cast {t.FastArmed}), worst {t.FastWorst} cm a tick over ({t.FastWhat}); by the law's why: {string.Join(", ", t.FastBy.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(8).Select(kv => kv.Key + " " + kv.Value))}; straight back {t.Back} ({t.BackFast} both ways faster than a walk)\n" +
                $"               drawn as a run (over {RunDrawnCmT} cm a tick) by the law, its own place slower: {t.Run} moves (the city's {t.RunCity}, the cell {t.RunCell}, the armed and the cast {t.RunArmed}); by the law's why: {string.Join(", ", t.RunBy.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(8).Select(kv => kv.Key + " " + kv.Value))}\n" +
                $"               left the street (not over the sheet's edge) {t.Leaves} times, {t.LeavesFar} more than two radii, a step and its pace from its own place ({string.Join(", ", t.LeaveKinds.Select(kv => kv.Key + " " + kv.Value))}) (farthest {t.LeaveMax} cm {t.LeaveWhat}); farthest from its own place {t.Far / 100.0:0.0} m {t.FarWhat}; ticks with someone 5 m behind {t.Far5}"));
            if (t.Jumps > 0) Console.Error.WriteLine("               jumps by kind: " + string.Join(", ", t.JumpKinds.Select(kv => kv.Key + " " + kv.Value)));
            foreach (var f in firsts.Take(show * 3)) Console.Error.WriteLine("    " + f);
            foreach (var f in tenets) Console.Error.WriteLine("    " + f);
            foreach (var f in jfirst) Console.Error.WriteLine("    jump " + f);
            w.Obj().Prop("setting", sets[s].name).Prop("runs", runs).Prop("wins", wins).Prop("ticks", t.Ticks).Prop("bodyTicks", bodies).Prop("errors", errs)
                .Prop("moves", moves).Prop("jumps", t.Jumps).Prop("jumpsOwnPlace", t.JumpsOwn).Prop("jumpWorstOverCm", t.WorstOver)
                .Prop("insideTwoRadii", t.Bad).Prop("tenetPairs", t.Tenet).Prop("squeezePairTicks", t.Sqz).Prop("squeezeClosestCm", t.SqzMin == int.MaxValue ? -1 : t.SqzMin).Prop("lawOverlaps", t.LawOverlap).Prop("lawTenetTicks", t.LawTenet).Prop("closestCm", t.Min == int.MaxValue ? -1 : t.Min).Prop("closest", t.MinWhat)
                .Prop("heldSpells", spells.Count).Prop("heldP50S10", (int)Math.Round(Pct(50) * 10)).Prop("heldP95S10", (int)Math.Round(Pct(95) * 10)).Prop("heldMaxS10", smax * 10 / Game.Hz).Prop("held5sOrMore", over5).Prop("held10sOrMore", over10).Prop("heldLongest", t.LongWhat).Prop("heldCity", ClsLine(byCls[0])).Prop("heldCell", ClsLine(byCls[1])).Prop("heldArmedAndCast", ClsLine(byCls[2])).Prop("heldBodyTicks", held).Prop("cellHeldTicks", cellHeld)
                .Prop("madeRoom", t.Sidesteps).Prop("squeezes", t.Squeezes).Prop("changedPlans", t.Replans).Prop("leftTheStreet", t.Leaves).Prop("leftFarFromOwnPlace", t.LeavesFar).Prop("leftFarthestCm", t.LeaveMax).Prop("leftFarthest", t.LeaveWhat)
                .Prop("farthestCm", t.Far).Prop("farthest", t.FarWhat).Prop("ticks5mBehind", t.Far5)
                .Prop("lawFast", t.Fast).Prop("lawFastCity", t.FastCity).Prop("lawFastCell", t.FastCell).Prop("lawFastArmed", t.FastArmed).Prop("lawFastWorstOverCm", t.FastWorst).Prop("lawFastWorst", t.FastWhat).Prop("straightBack", t.Back).Prop("straightBackFast", t.BackFast)
                .Prop("lawRun", t.Run).Prop("lawRunCity", t.RunCity).Prop("lawRunCell", t.RunCell).Prop("lawRunArmed", t.RunArmed);
            w.Open("lawRunBy"); foreach (var kv in t.RunBy.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal)) w.Prop(kv.Key, kv.Value); w.EndObj();
            w.Open("lawFastBy"); foreach (var kv in t.FastBy.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal)) w.Prop(kv.Key, kv.Value); w.EndObj();
            w.Open("leftFarByKind"); foreach (var kv in t.LeaveKinds) w.Prop(kv.Key, kv.Value); w.EndObj();
            w.Open("changedPlansBy"); foreach (var kv in t.ReplansBy) w.Prop(kv.Key, kv.Value); w.EndObj();
            w.Open("fails"); foreach (var kv in fails) w.Prop(kv.Key, kv.Value); w.EndObj();
            w.Open("jumpsByKind"); foreach (var kv in t.JumpKinds) w.Prop(kv.Key, kv.Value); w.EndObj();
            big.Sort((a, b) => b.cm.CompareTo(a.cm)); w.OpenArr("jumpBiggest"); foreach (var f in big.Take(show * 6)) w.Str(f.what); w.EndArr();
            w.OpenArr("first"); foreach (var f in firsts.Take(show * 3)) w.Str(f); w.EndArr().EndObj();
        }
        var json = w.EndArr().EndObj().ToString(); int oi = Array.IndexOf(args, "--out"); if (oi > 0) File.WriteAllText(args[oi + 1], json + "\n"); else Console.WriteLine(json);
        return bad == 0 ? 0 : 1;
    }
    static readonly object JumpLock = new object();
    /// <summary>Whether `at` stands within `cm` of the sheet's edge (a body leaving there walks off the sheet, the street going on beyond it).</summary>
    static bool AtEdge(Game g, Pt at, int cm) { var gr = g.S.Grid; return !gr.InGrid(at + new Pt(cm, 0)) || !gr.InGrid(at + new Pt(-cm, 0)) || !gr.InGrid(at + new Pt(0, cm)) || !gr.InGrid(at + new Pt(0, -cm)); }
    /// <summary>Whether a body that left the upright set fell (shot, taken down, knocked out: it lies where it fell) rather than left the street.</summary>
    static bool Fell(Game g, string id)
    {
        foreach (var w in g.Walkers) if (w.P.Id == id) return w.P.Down;
        foreach (var p in g.People) if (p.Id == id) return p.Down;
        foreach (var m in g.Crew) if (m.Id == id) return m.Gun.Down || m.P.Down;
        foreach (var h in g.Hostiles) if (h.Id == id) return h.Down;
        foreach (var u in g.Units) if (u.Id == id) return u.Gun.Down;
        foreach (var op in g.Ops) if (Op.Name(op.Id) == id) return op.Down || op.CarriedBy.HasValue;
        return id == "patrol" && g.PatrolDown;
    }
    static string Name(string p) => System.IO.Path.GetFileName(p).Replace(".adaptive.json", "").Replace(".json", "");
    static int Arg(string[] args, string name, int dflt) { int k = Array.IndexOf(args, name); return k > 0 && k + 1 < args.Length ? int.Parse(args[k + 1], CultureInfo.InvariantCulture) : dflt; }

    /// <summary>One item on one seed under counts, every tick watched.</summary>
    static Res Watch(Engine.Inputs inputs, string root, string item, int seed, SortedDictionary<string, int>? counts, int min, int show)
    {
        Script sc;
        if (item.EndsWith(".adaptive.json", StringComparison.Ordinal))
        {
            var pl = Adaptive.Play(inputs, Adaptive.Load(item, root), seed, out _, counts);
            sc = Script.Parse(new JWriter().Obj().Prop("schema", "suborned.sim.script/1").Prop("mission", "handover").Key("start").Raw(pl.Start.Canonical()).OpenArr("orders").EndArr().EndObj().ToString());
            foreach (var (o, _, _, _) in pl.Orders) sc.Orders.Add(o); sc.Until = pl.Tick;
        }
        else { sc = Script.Parse(File.ReadAllText(item)); sc.Start.Seed = seed; sc.Start.Counts.Clear(); if (counts != null) DialCli.Apply(sc.Start, counts); }
        var g = Engine.New(inputs, sc.Start); var r = new Res(); var mv = new BodyCheck.Moves();
        if (Environment.GetEnvironmentVariable("SUBORNED_SCRIPT_OUT") is string sdir) { var w0 = new JWriter().Obj().Prop("schema", "suborned.sim.script/1").Prop("mission", "handover").Key("start").Raw(sc.Start.Canonical()).OpenArr("orders"); foreach (var o in sc.Orders) o.Write(w0); File.WriteAllText(System.IO.Path.Combine(sdir, $"{Name(item)}-{seed}.json"), w0.EndArr().Prop("until", sc.Until).EndObj().ToString()); } // a probe's run as a script, for `play`
        var raw0 = new Dictionary<string, Pt>(StringComparer.Ordinal); var raw1 = new Dictionary<string, Pt>(StringComparer.Ordinal); int seenJ = 0;
        var stuck = new Dictionary<string, int>(StringComparer.Ordinal); var cls = new Dictionary<string, char>(StringComparer.Ordinal); var stuckNow = new Dictionary<string, int>(StringComparer.Ordinal);
        var upLast = new Dictionary<string, (Pt at, Pt raw, string kind)>(StringComparer.Ordinal); var upNow = new Dictionary<string, (Pt at, Pt raw, string kind)>(StringComparer.Ordinal);
        string? jlog = Environment.GetEnvironmentVariable("SUBORNED_JUMP_LOG"), hlog = Environment.GetEnvironmentVariable("SUBORNED_HELD_LOG"), llog = Environment.GetEnvironmentVariable("SUBORNED_LEAVE_LOG");
        var orders = sc.Orders.Select((o, k) => (o, k)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).Select(x => x.o).ToList(); int oi = 0, until = sc.Until > 0 ? sc.Until : Game.T(inputs.SiteData.Over);
        var atLast = new Dictionary<string, Pt>(StringComparer.Ordinal); var atNow = new Dictionary<string, Pt>(StringComparer.Ordinal); int walkT = Math.Max(1, g.R.PeopleWalk / Game.Hz); // a walk: the city's, cm a tick
        var mvLast = new Dictionary<string, (Pt drawn, Pt own, int tick)>(StringComparer.Ordinal); // each body's last move of 5 cm or more: drawn, its own place's, when
        void look()
        {
            var up = BodyCheck.Of(g); r.Ticks++; r.Bodies += up.Count; bool any = false, ten = false, sq = false;
            (raw0, raw1) = (raw1, raw0); g.BodyRawsInto(raw1); mv.Look(g, up, int.MaxValue);
            atNow.Clear(); // engine 0.47.1, the law's own speed (Latch at people=128: one about to squeeze slid at the city's run, once straight back)
            foreach (var u in up)
            {
                atNow[u.Id] = u.At; if (!atLast.TryGetValue(u.Id, out var p0) || !raw0.TryGetValue(u.Id, out var o0) || !raw1.TryGetValue(u.Id, out var o1)) continue; // new on the street, or not laid: no move
                Pt dm = u.At - p0, om = o1 - o0; int d = G.Dist(u.At, p0), own = G.Dist(o1, o0), lim = Math.Max(walkT, own) + BodyCheck.RoundCm;
                char k = u.Kind == "cell" ? 'c' : (u.Kind == "crowd" || u.Kind == "dial-person") && !u.Tenet && !g.BodyKeepsPlace(u.Id) ? 'p' : 'a';
                if (d > RunDrawnCmT && own <= RunDrawnCmT) { r.Run++; if (k == 'p') r.RunCity++; else if (k == 'c') r.RunCell++; else r.RunArmed++; var rw = g.BodyWhy(u.Id); r.RunBy[rw] = r.RunBy.GetValueOrDefault(rw) + 1; } // the host draws it running; its own rules walk it (or stand it)
                if (d > lim) { r.Fast++; if (k == 'p') r.FastCity++; else if (k == 'c') r.FastCell++; else r.FastArmed++; var why = g.BodyWhy(u.Id); r.FastBy[why] = r.FastBy.GetValueOrDefault(why) + 1; if (d - lim > r.FastWorst) { r.FastWorst = d - lim; r.FastWhat = $"{Report.Clock(g.Tick)} {u.Id} ({u.Kind}) {d} cm a tick, its own place {own}: {g.BodyTrace(u.Id)}"; } }
                if (d < 5) continue;
                if (mvLast.TryGetValue(u.Id, out var m0) && g.Tick - m0.tick <= 3 && Opposed(m0.drawn, dm) && (long)m0.own.X * om.X + (long)m0.own.Y * om.Y >= 0) { r.Back++; if (d > lim && G.Dist(m0.drawn, default) > Math.Max(walkT, G.Dist(m0.own, default)) + BodyCheck.RoundCm) r.BackFast++; } // straight back the way it last moved (a wait or two between included), its own place not turning
                mvLast[u.Id] = (dm, om, g.Tick);
            }
            (atLast, atNow) = (atNow, atLast);
            for (; seenJ < mv.First.Count; seenJ++) // each jump: its own place's move too (its own rules jumped it, or the law did)
            {
                var jx = mv.First[seenJ]; Pt ra = default, rb = default; bool own = raw0.TryGetValue(jx.id, out ra) && raw1.TryGetValue(jx.id, out rb) && G.Dist(ra, rb) > jx.pace + BodyCheck.RoundCm;
                if (own) r.JumpsOwn++; string bucket = jx.cm <= 2 * jx.pace ? "<2x" : jx.cm <= 100 ? "<1m" : jx.cm <= 300 ? "<3m" : ">3m", jk = jx.kind + (own ? "/own" : "/law") + bucket; r.JumpKinds[jk] = r.JumpKinds.GetValueOrDefault(jk) + 1;
                var what = $"{Report.Clock(jx.tick)} {jx.id} ({jx.kind}) {jx.cm} cm (pace {jx.pace}) {jx.from} -> {jx.to}{(own ? $" its own place {G.Dist(ra, rb)} cm" : " the law")}: {g.BodyTrace(jx.id)}";
                if (r.JumpFirst.Count < show) r.JumpFirst.Add(what);
                if (jx.cm > 100) { r.Big.Add((jx.cm, what)); if (r.Big.Count > 64) { r.Big.Sort((a, b) => b.cm.CompareTo(a.cm)); r.Big.RemoveRange(32, r.Big.Count - 32); } if (jlog != null) lock (JumpLock) File.AppendAllText(jlog, $"{System.IO.Path.GetFileName(item)}\t{seed}\t{jx.tick}\t{jx.id}\t{jx.kind}\t{jx.cm}\t{(own ? "own" : "law")}\t{jx.from.X},{jx.from.Y}\t{jx.to.X},{jx.to.Y}\t{g.BodyTrace(jx.id)}\n"); }
            }
            if (mv.First.Count > 4096) { mv.First.Clear(); seenJ = 0; }
            r.Jumps = mv.Jumps; r.Moves = mv.Looks; r.WorstOver = mv.WorstOverCm;
            foreach (var (a, b, cm) in BodyCheck.Closer(up, min))
            {
                if (a.Tenet && b.Tenet) { ten = true; if (r.TenetFirst.Count < show && !r.TenetFirst.Exists(x => x.Contains(a.Id + " (") && x.Contains(b.Id + " ("))) r.TenetFirst.Add($"{Report.Clock(g.Tick)} tenets {a.Id} ({a.Kind}) and {b.Id} ({b.Kind}) {cm} cm at {a.At}"); continue; }
                if (BodyCheck.Squeezing(g, a, b, cm)) { sq = true; r.SqzMin = Math.Min(r.SqzMin, cm); continue; } // a counted squeeze, two squeeze widths apart
                any = true; if (r.First.Count < show) r.First.Add($"{Report.Clock(g.Tick)} {a.Id} ({a.Kind}) and {b.Id} ({b.Kind}) {cm} cm at {a.At}: {g.BodyTrace(a.Id)} | {g.BodyTrace(b.Id)}");
            }
            if (any) r.Bad++; if (ten) r.Tenet++; if (sq) r.Sqz++;
            var st = g.BodyStrain(); r.Held += st.held; r.CellHeld += st.cellHeld; if (st.farCm > 500) r.Far5++; if (st.farCm > r.Far) { r.Far = st.farCm; r.FarWhat = $"{st.farId} at {Report.Clock(g.Tick)}"; }
            foreach (var (a, b, cm) in BodyCheck.Closer(up, 400)) { if (cm < r.Min && !(a.Tenet && b.Tenet) && !BodyCheck.Squeezing(g, a, b, cm)) { r.Min = cm; r.MinWhat = $"{a.Id}+{b.Id} at {Report.Clock(g.Tick)}"; } break; }
            // held: spells of ticks got nowhere (the law's count; a spell ends when it gets on, changes plans or leaves the street)
            foreach (var u in up) cls[u.Id] = u.Kind == "cell" ? 'c' : (u.Kind == "crowd" || u.Kind == "dial-person") && !u.Tenet && !g.BodyKeepsPlace(u.Id) ? 'p' : 'a'; // the city's people, the cell, the armed and the cast
            g.BodyStuckInto(stuckNow); foreach (var kv in stuck) if (!stuckNow.TryGetValue(kv.Key, out var n) || n < kv.Value) { r.Spells.Add(kv.Value); (cls.GetValueOrDefault(kv.Key, 'a') == 'p' ? r.SpellsCity : cls.GetValueOrDefault(kv.Key, 'a') == 'c' ? r.SpellsCell : r.SpellsArmed).Add(kv.Value); if (kv.Value > r.LongSpell) { r.LongSpell = kv.Value; r.LongWhat = $"{kv.Key} to {Report.Clock(g.Tick)}"; } }
            if (hlog != null) foreach (var kv in stuckNow) if (kv.Value == Game.T(5)) lock (JumpLock) File.AppendAllText(hlog, $"{System.IO.Path.GetFileName(item)}\t{seed}\t{g.Tick}\t{kv.Key}\t{g.BodyTrace(kv.Key)}\n"); // a hold reaching 5 s, as it stands then
            (stuck, stuckNow) = (stuckNow, stuck);
            // leaving the street: how far from its own place it stood when it went
            upNow.Clear(); foreach (var u in up) upNow[u.Id] = (u.At, raw1.TryGetValue(u.Id, out var rw) ? rw : u.At, u.Kind);
            foreach (var kv in upLast) if (!upNow.ContainsKey(kv.Key) && !Fell(g, kv.Key) && !AtEdge(g, kv.Value.at, BodyCheck.PaceCmT(g, kv.Value.kind) + 10)) { r.Leaves++; int d = G.Dist(kv.Value.at, kv.Value.raw); if (d > g.R.BodyMinCm + g.R.BodySpaceCm + BodyCheck.PaceCmT(g, kv.Value.kind)) { r.LeavesFar++; r.LeaveKinds[kv.Value.kind] = r.LeaveKinds.GetValueOrDefault(kv.Value.kind) + 1; if (llog != null) lock (JumpLock) File.AppendAllText(llog, $"{System.IO.Path.GetFileName(item)}\t{seed}\t{g.Tick}\t{kv.Key}\t{kv.Value.kind}\t{d}\tat {kv.Value.at} own {kv.Value.raw} on the sheet {g.S.Grid.InGrid(kv.Value.raw)}\n"); } if (d > r.LeaveMax) { r.LeaveMax = d; r.LeaveWhat = $"{kv.Key} ({kv.Value.kind}) at {Report.Clock(g.Tick)}"; } }
            (upLast, upNow) = (upNow, upLast);
        }
        look();
        string[]? trace = Environment.GetEnvironmentVariable("SUBORNED_BODY_TRACE")?.Split(','); // id,id,...,fromTick,toTick
        while (g.Tick < until && !g.Over)
        {
            while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]);
            g.Step(); look();
            if (trace != null && trace.Length >= 3 && g.Tick >= int.Parse(trace[^2], CultureInfo.InvariantCulture) && g.Tick <= int.Parse(trace[^1], CultureInfo.InvariantCulture))
                foreach (var id in trace[..^2])
                {
                    Console.Error.WriteLine($"  {Report.Clock(g.Tick)} {g.BodyTrace(id)}");
                    if (Environment.GetEnvironmentVariable("SUBORNED_BODY_NEAR") is string nr && g.BodiesNow().Find(x => x.id == id) is var me && me.id != null)
                        foreach (var x in g.BodiesNow()) if (x.id != id && G.Within(x.at, me.at, int.Parse(nr, CultureInfo.InvariantCulture))) Console.Error.WriteLine($"      near {x.id} at {x.at} ({G.Dist(x.at, me.at)} cm) cls {x.cls} moving {x.moving}: {g.BodyTrace(x.id)}");
                }
        }
        while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]);
        g.Finish(); r.Outcome = g.Result.ToString().ToLowerInvariant(); r.Fail = g.FailCode;
        if (r.Outcome == "none" && Environment.GetEnvironmentVariable("SUBORNED_NONE_LOG") is string nlog) lock (JumpLock) File.AppendAllText(nlog, $"{Name(item)}\t{seed}\t{g.Tick}\tuntil {until}\tover {g.Over}\n"); // a run with no outcome
        r.LawOverlap = (int)g.BodyOverlapTicks; r.LawTenet = g.BodyTenetTicks; // the law's own counts: laid still inside two radii (BodyRepair's leftovers), two tenets inside two radii
        foreach (var kv in stuck) { r.Spells.Add(kv.Value); (cls.GetValueOrDefault(kv.Key, 'a') == 'p' ? r.SpellsCity : cls.GetValueOrDefault(kv.Key, 'a') == 'c' ? r.SpellsCell : r.SpellsArmed).Add(kv.Value); if (kv.Value > r.LongSpell) { r.LongSpell = kv.Value; r.LongWhat = $"{kv.Key} to the end, {g.BodyTrace(kv.Key)}"; } }
        r.Sidesteps = g.BodySidesteps; r.Squeezes = g.BodySqueezes; r.Replans = g.BodyReplans; foreach (var kv in g.BodyReplansBy) r.ReplansBy[kv.Key] = kv.Value;
        return r;
    }
}
