// suborned-sim check [ids…] [--no-replay] [--timeout s]: every scenario (or the named ones) played in this process on every core, each one's
// failed checks and whether it replays bit for bit from its start state and orders (the xunit ScenarioTests' Passes and ReplaysFromStartStateAndOrders,
// in seconds instead of minutes). One line a scenario, in list order: "<id>: ok", or "<id>: FAIL <check>; <check>", "<id>: DIFFERS", "<id>: THREW …".
// Engine 0.34.0: every game is bounded (Scenarios.StepLimit: a scenario waiting for what never comes THREW RunawayException) and every play is
// watched (--timeout, Watchdog.cs); the last line names the scenario that used the most Step calls and the slowest.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Suborned.Sim;

static class CheckCli
{
    public static int Run(Engine.Inputs inputs, string[] args)
    {
        bool replay = Array.IndexOf(args, "--no-replay") < 0;
        var ids = new List<string>(); for (int a = 1; a < args.Length; a++) { if (args[a] == "--timeout") { a++; continue; } if (!args[a].StartsWith("--", StringComparison.Ordinal)) ids.Add(args[a]); }
        var want = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
        var list = Scenarios.All.Where(s => want.Count == 0 || want.Contains(s.Id)).ToList();
        var lines = new string[list.Count]; var steps = new int[list.Count]; var secs = new double[list.Count];
        using var dog = new Watchdog(Watchdog.Seconds(args));
        Parallel.For(0, list.Count, k =>
        {
            var sc = list[k]; var sw = System.Diagnostics.Stopwatch.StartNew(); dog.Start(k, sc.Id);
            try
            {
                var run = Scenarios.Run(inputs, sc); steps[k] = run.G.StepCalls;
                var failed = run.Checks.Where(c => !c.ok).Select(c => c.what).ToList();
                string rep = "";
                if (replay) { var again = Scenarios.Bounded(() => Engine.Replay(inputs, run.G)); if (!again.Checkpoints.SequenceEqual(run.G.Checkpoints) || again.Result != run.G.Result) rep = " DIFFERS"; }
                if (run.Checks.Count == 0) failed.Add("no checks");
                lines[k] = failed.Count == 0 && rep.Length == 0 ? $"{sc.Id}: ok" : $"{sc.Id}: FAIL{rep} {string.Join("; ", failed)}";
            }
            catch (Exception e) { lines[k] = $"{sc.Id}: THREW {e.GetType().Name}: {e.Message}"; }
            finally { dog.Stop(k); secs[k] = sw.Elapsed.TotalSeconds; }
        });
        int bad = 0; foreach (var l in lines) { Console.WriteLine(l); if (!l.EndsWith(": ok", StringComparison.Ordinal)) bad++; }
        int most = 0, slow = 0; for (int k = 1; k < list.Count; k++) { if (steps[k] > steps[most]) most = k; if (secs[k] > secs[slow]) slow = k; }
        Console.WriteLine(list.Count == 0 ? "0/0 ok" : string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{list.Count - bad}/{list.Count} ok · most Step calls {steps[most]} ({list[most].Id}; limit {Scenarios.StepLimit}) · slowest {secs[slow]:0.0} s ({list[slow].Id})"));
        return bad == 0 ? 0 : 1;
    }
}
