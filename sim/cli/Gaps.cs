// suborned-sim gaps [file] [--json] (brief 098, engine 0.49.0, Keel): the zone-wide gap check (WalkGaps.cs) on the site's walk: every gap of
// sim/data/handover.gaps.json crossed both ways, or why not. Exit 1 if any is not. --json: one line per gap.
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;

static class GapsCli
{
    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        string file = args.Length > 1 && !args[1].StartsWith("--", StringComparison.Ordinal) ? args[1] : System.IO.Path.Combine(root, WalkGaps.File);
        var grid = inputs.SiteData.Grid; var gaps = WalkGaps.Read(File.ReadAllText(file), grid);
        var res = WalkGaps.Check(grid, gaps); int bad = res.Count(r => !r.There || !r.Back);
        if (Array.IndexOf(args, "--json") > 0)
        {
            var w = new JWriter().Obj().Prop("engine", Engine.Version).Prop("gaps", res.Count).Prop("walled", bad).OpenArr("rows");
            foreach (var r in res) w.Obj().Prop("a", r.G.A).Prop("b", r.G.B).Prop("gap", r.G.Cm).Prop("there", r.There).Prop("back", r.Back).Prop("why", r.Why).Prop("lenThere", r.LenThere).Prop("lenBack", r.LenBack).EndObj();
            Console.WriteLine(w.EndArr().EndObj().ToString());
        }
        foreach (var r in res.Where(r => !r.There || !r.Back).OrderByDescending(r => r.G.Cm)) Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"WALLED {r.G.Cm,4} cm  {r.G.A} | {r.G.B}  at ({r.G.M.X}, {r.G.M.Y}): {r.Why}"));
        var widest = res.Where(r => !r.There || !r.Back).Select(r => r.G.Cm).DefaultIfEmpty(0).Max();
        Console.Error.WriteLine($"{res.Count - bad}/{res.Count} gaps crossed both ways (nodes {grid.FineNodes}, edges {grid.FineEdges}); widest walled {widest} cm");
        return bad == 0 ? 0 : 1;
    }
}
