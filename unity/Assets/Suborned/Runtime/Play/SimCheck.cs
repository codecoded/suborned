using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Suborned.Sim;
using UnityEngine;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>§11: "Unity must pass the same 28 scenarios before it's trusted." Plays every scenario of the loaded DLL (Scenarios.All: the 28 and
    /// the N-cases) in this runtime, replays each from its own start state and order log, and compares every checkpoint hash with the
    /// headless CLI's run of the same DLL on the same inputs (headless.json). Used by the Editor menu and test (Mono) and by a player launched
    /// with `-sim-check &lt;out.json&gt;` (Mono or IL2CPP), which writes the rows and quits.</summary>
    public static class SimCheck
    {
        public sealed class Row
        {
            public string Id = "", Outcome = "", Fail = "", StartHash = "", FinalHash = "", HeadlessFinal = "", Why = "";
            public int Tick, Checkpoints, Checks, ChecksFailed; public bool ReplayIdentical, Match; public double Ms;
        }

        public static List<Row> RunAll(Engine.Inputs inputs, JNode headless) => RunSet(Scenarios.All, _ => inputs, headless["scenarios"]);

        /// <summary>Brief 029: the fight range's set (R01..R10) on its own inputs (SimBundle.LoadRange by each scenario's layout), against
        /// headless.json `range`. Empty when the bundle carries no range.</summary>
        public static List<Row> RunRange(JNode headless) => headless.Opt("range") is JNode r && r.Items.Count > 0 ? RunSet(Scenarios.Range, sc => SimBundle.LoadRange(sc.Layout), r) : new List<Row>();

        static List<Row> RunSet(IEnumerable<Scenario> set, Func<Scenario, Engine.Inputs> inputsOf, JNode headlessRows)
        {
            var want = new Dictionary<string, JNode>(StringComparer.Ordinal);
            foreach (var s in headlessRows.Items) want[s["id"].Str()] = s;
            var rows = new List<Row>();
            foreach (var sc in set)
            {
                var row = new Row { Id = sc.Id };
                var sw = Stopwatch.StartNew();
                try
                {
                    var inputs = inputsOf(sc);
                    var run = Scenarios.Run(inputs, sc);
                    var g = run.G;
                    var replay = Engine.Replay(inputs, g);
                    sw.Stop();
                    row.Outcome = g.Result.ToString().ToLowerInvariant(); row.Fail = g.FailCode; row.Tick = g.Tick;
                    row.StartHash = Hash.Hex(g.StartHash); row.FinalHash = Hash.Hex(g.Checkpoints[g.Checkpoints.Count - 1].hash);
                    row.Checkpoints = g.Checkpoints.Count; row.Checks = run.Checks.Count; row.ChecksFailed = run.Checks.FindAll(c => !c.ok).Count;
                    row.ReplayIdentical = Same(g.Checkpoints, replay.Checkpoints);
                    if (!want.TryGetValue(sc.Id, out var h)) row.Why = "not in headless.json";
                    else
                    {
                        row.HeadlessFinal = h["finalHash"].Str();
                        var cps = h["checkpoints"].Items;
                        bool same = cps.Count == g.Checkpoints.Count && h["startHash"].Str() == row.StartHash && h["outcome"].Str() == row.Outcome;
                        for (int k = 0; same && k < cps.Count; k++)
                            same = cps[k]["tick"].Int() == g.Checkpoints[k].tick && cps[k]["hash"].Str() == Hash.Hex(g.Checkpoints[k].hash);
                        row.Match = same;
                        if (!same) row.Why = $"headless {h["outcome"].Str()} {row.HeadlessFinal[..12]} ({cps.Count} checkpoints), here {row.Outcome} {row.FinalHash[..12]} ({row.Checkpoints})";
                    }
                }
                catch (Exception e) { row.Why = $"{e.GetType().Name}: {e.Message}"; }
                row.Ms = sw.Elapsed.TotalMilliseconds;
                rows.Add(row);
            }
            return rows;
        }

        public static bool Passed(List<Row> rows) => rows.TrueForAll(r => r.Match && r.ReplayIdentical && r.ChecksFailed == 0 && r.Why.Length == 0);

        static bool Same(List<(int tick, ulong hash)> a, List<(int tick, ulong hash)> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }

        public static string Json(List<Row> rows, SimBundle.Loaded bundle, string where)
        {
            var w = new JWriter().Obj().Prop("schema", "suborned.sim.unity-check/1").Prop("runtime", SimBundle.Runtime).Prop("where", where)
                .Prop("unity", Application.unityVersion).Prop("platform", Application.platform.ToString())
                .Prop("commit", bundle.Commit).Prop("dllSha256", bundle.DllSha).Prop("passed", Passed(rows))
                .Prop("matching", rows.FindAll(r => r.Match).Count).Prop("scenarios", rows.Count);
            w.OpenArr("rows");
            foreach (var r in rows)
                w.Obj().Prop("id", r.Id).Prop("outcome", r.Outcome).Prop("fail", r.Fail).Prop("tick", r.Tick).Prop("startHash", r.StartHash)
                    .Prop("finalHash", r.FinalHash).Prop("headlessFinal", r.HeadlessFinal).Prop("checkpoints", r.Checkpoints).Prop("checks", r.Checks)
                    .Prop("checksFailed", r.ChecksFailed).Prop("replayIdentical", r.ReplayIdentical).Prop("matchesHeadless", r.Match)
                    .Prop("ms", (long)Math.Round(r.Ms)).Prop("why", r.Why).EndObj();
            return w.EndArr().EndObj().ToString();
        }

        /// <summary>A player launched with -sim-check &lt;out.json&gt; plays the scenarios headless and quits (exit 0 when all match).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void FromCommandLine()
        {
            if (Application.isEditor) return;
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-sim-check"); if (i < 0) i = Array.IndexOf(args, "-runScenarios"); // the game build's name for it
            if (i < 0 || i + 1 >= args.Length) return;
            int code = 1;
            try
            {
                var b = SimBundle.Load();
                var rows = RunAll(b.Inputs, SimBundle.Headless());
                File.WriteAllText(args[i + 1], Json(rows, b, "player") + "\n");
                code = Passed(rows) ? 0 : 1;
            }
            catch (Exception e) { File.WriteAllText(args[i + 1], $"{{\"error\": \"{e.GetType().Name}: {e.Message.Replace("\"", "'")}\"}}\n"); }
            Application.Quit(code);
        }
    }
}
