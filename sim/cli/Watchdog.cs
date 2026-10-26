// Engine 0.34.0 (brief 067, Aster: "no hang anywhere, a timeout on every play"): the CLI's plays (check, batch) each run under a wall-clock
// watch. The engine's loops all end and a harness bounds the Step calls (Scenarios.StepLimit), so this is the last line: a play still running
// after the timeout ends the process with exit code 3 and its name on stderr, never a batch stuck at 100 % of a core. A .NET thread can't be
// stopped from outside, so the process goes. --timeout <s> (default 300; 0 = off) on check and batch.
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

sealed class Watchdog : IDisposable
{
    public const int DefaultS = 300;
    readonly ConcurrentDictionary<int, (string what, long started)> live = new ConcurrentDictionary<int, (string, long)>();
    readonly Timer? timer; readonly long limitTicks; readonly int timeoutS;
    public Watchdog(int timeoutS)
    {
        this.timeoutS = timeoutS; limitTicks = (long)timeoutS * Stopwatch.Frequency;
        if (timeoutS > 0) timer = new Timer(_ => Look(), null, 1000, 1000);
    }
    /// <summary>The play in slot i (an item index) starts now.</summary>
    public void Start(int i, string what) { if (timer != null) live[i] = (what, Stopwatch.GetTimestamp()); }
    public void Stop(int i) => live.TryRemove(i, out _);
    void Look()
    {
        long now = Stopwatch.GetTimestamp();
        foreach (var kv in live)
        {
            if (now - kv.Value.started <= limitTicks) continue;
            Console.Out.Flush();
            Console.Error.WriteLine($"TIMEOUT: {kv.Value.what} is still playing after {timeoutS} s of wall time (check --timeout); the process ends here (exit 3)");
            Console.Error.Flush();
            Environment.Exit(3);
        }
    }
    public void Dispose() => timer?.Dispose();
    /// <summary>--timeout s from the args (default 300; 0 = off).</summary>
    public static int Seconds(string[] args)
    {
        int k = Array.IndexOf(args, "--timeout");
        return k > 0 && k + 1 < args.Length ? int.Parse(args[k + 1], CultureInfo.InvariantCulture) : DefaultS;
    }
}
