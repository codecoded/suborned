// Brief 098, engine 0.49.0 (Sky 2026-11-08: "Gap between planter and fountain should be walkable, don't be one of those game engines where you can't
// get past even though visibly it's clear you have a lot of space either side"): the zone-wide gap check on the engine's own walk (WalkGaps.cs), and
// the walk raster's law where Sky saw it: the fountain's four planter gaps walked both ways.
using System.IO;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class GapsTests
{
    static readonly string Root = Engine.FindRoot(System.AppContext.BaseDirectory);
    static Grid W => Repo.Inputs.SiteData.Grid;

    [Fact]
    public void EveryGapABodyFitsThroughIsCrossedBothWays()
    {
        var gaps = WalkGaps.Read(File.ReadAllText(System.IO.Path.Combine(Root, WalkGaps.File)), W);
        Assert.True(gaps.Count > 100, $"the zone's gaps: {gaps.Count}");
        var bad = WalkGaps.Check(W, gaps).Where(r => !r.There || !r.Back).Select(r => $"{r.G.Cm} cm {r.G.A} | {r.G.B} at {r.G.M}: {r.Why}").ToList();
        Assert.True(bad.Count == 0, $"{bad.Count} invisible walls: " + string.Join("; ", bad.Take(10)));
    }

    [Fact]
    public void TheFountainsPlanterGapsAreWalkedAcrossAndAlong()
    {
        var gaps = WalkGaps.Read(File.ReadAllText(System.IO.Path.Combine(Root, WalkGaps.File)), W).Where(g => (g.A + g.B).Contains("atom-fountain") && (g.A + g.B).Contains("fountain-garden")).ToList();
        Assert.True(gaps.Select(g => (g.A + g.B).Replace("civic/atom-fountain", "")).Distinct().Count() >= 4, "the fountain's four planters each stand a gap from its basin");
        foreach (var g in gaps) { Assert.True(g.Cm >= 180, $"{g.A} | {g.B}: {g.Cm} cm"); Assert.True(W.Free(g.M), $"the gap's middle {g.M} is free ground"); }
    }

    [Fact]
    public void TheWalkKeepsTheRulebooksClearanceAndTheRasterIsCutToIt()
    {
        Assert.True(W.HasFine); Assert.Equal(Repo.Inputs.Rules.WalkClearCm, W.Body); Assert.Equal(35, W.Body);
        Assert.Equal(5, W.FCell); Assert.Equal(W.Nx * 10, W.FNx);
    }
}
