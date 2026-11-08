// Kept here as the evidence's source (brief 098, engine 0.50.0; not built from here: a scratch copy of sim/cli, with `case "replans"` in Dial.cs). Keel's probe: route scripts on seeds under counts, the changes of plans each run shows.
//   dial replans <dir> --seeds n --counts guards=64,people=64
using System; using System.Globalization; using System.IO; using System.Linq; using Suborned.Sim;
static class ReplansCli
{
    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        string Arg(string k, string d) { int i = Array.IndexOf(args, k); return i > 0 && i + 1 < args.Length ? args[i + 1] : d; }
        var files = Directory.GetFiles(args[2], "*.json").Where(f => !f.EndsWith("routes.json", StringComparison.Ordinal)).OrderBy(f => f, StringComparer.Ordinal).ToList();
        int seeds = int.Parse(Arg("--seeds", "3"), CultureInfo.InvariantCulture); var cs = Arg("--counts", "");
        var jobs = files.SelectMany(f => Enumerable.Range(1, seeds).Select(s => (f, s))).ToList();
        var res = Par.Map(jobs.Count, BatchCli.Jobs(args), j =>
        {
            var (f, sd) = jobs[j]; var sc = Script.Parse(File.ReadAllText(f)); sc.Start.Seed = sd; sc.Start.Counts.Clear(); if (cs.Length > 0) DialCli.Apply(sc.Start, DialCli.Parse(cs));
            var g = Engine.Play(inputs, sc.Start, sc.Orders, sc.Until); return (g.BodyReplans, string.Join(",", g.BodyReplansBy.Select(kv => kv.Key + ":" + kv.Value)));
        });
        for (int j = 0; j < jobs.Count; j++) if (res[j].error == null && res[j].value.BodyReplans > 0) Console.WriteLine($"{System.IO.Path.GetFileNameWithoutExtension(jobs[j].f)} #{jobs[j].s} {res[j].value.BodyReplans} {res[j].value.Item2}");
        return 0;
    }
}
