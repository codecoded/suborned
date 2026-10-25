// suborned-sim check [ids…] [--no-replay]: every scenario (or the named ones) played in this process on every core, each one's failed checks
// and whether it replays bit for bit from its start state and orders (the xunit ScenarioTests' Passes and ReplaysFromStartStateAndOrders, in
// seconds instead of minutes). One line a scenario, in list order: "<id>: ok", or "<id>: FAIL <check>; <check>", "<id>: DIFFERS", "<id>: THREW …".
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
        var want = new HashSet<string>(args.Skip(1).Where(a => !a.StartsWith("--", StringComparison.Ordinal)), StringComparer.OrdinalIgnoreCase);
        var list = Scenarios.All.Where(s => want.Count == 0 || want.Contains(s.Id)).ToList();
        var lines = new string[list.Count];
        Parallel.For(0, list.Count, k =>
        {
            var sc = list[k];
            try
            {
                var run = Scenarios.Run(inputs, sc);
                var failed = run.Checks.Where(c => !c.ok).Select(c => c.what).ToList();
                string rep = "";
                if (replay) { var again = Engine.Replay(inputs, run.G); if (!again.Checkpoints.SequenceEqual(run.G.Checkpoints) || again.Result != run.G.Result) rep = " DIFFERS"; }
                if (run.Checks.Count == 0) failed.Add("no checks");
                lines[k] = failed.Count == 0 && rep.Length == 0 ? $"{sc.Id}: ok" : $"{sc.Id}: FAIL{rep} {string.Join("; ", failed)}";
            }
            catch (Exception e) { lines[k] = $"{sc.Id}: THREW {e.GetType().Name}: {e.Message}"; }
        });
        int bad = 0; foreach (var l in lines) { Console.WriteLine(l); if (!l.EndsWith(": ok", StringComparison.Ordinal)) bad++; }
        Console.WriteLine($"{list.Count - bad}/{list.Count} ok");
        return bad == 0 ? 0 : 1;
    }
}
