// Engine 0.33.0 (brief 060, Suture's 2026-10-26 cause A): no routine leg of sim/data/handover.people.json with an end inside the walk grid's
// window is a straight-line fallback. 0.32.0 drew the clerks' leg from the Office entrance to the T1 stop (block (150, 130), off the window) as a
// straight line through the Records Office: Grid.NearestFree searched only 6 m, so the off-window end had no cell and Route had no end.
using System;
using System.Collections.Generic;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class CrowdLegTests
{
    static IEnumerable<(string key, Pt a, Pt b)> Legs()
    {
        var c = Repo.Inputs.Crowd!; var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var w in c.Walkers)
            for (int i = 0; i + 1 < w.Place.Length; i++)
            {
                var a = w.Place[i]; var b = w.Place[i + 1]; if (a == "beyond" || b == "beyond") continue;
                if (seen.Add(a + "→" + b)) yield return (a + "→" + b, c.Places[a], c.Places[b]);
            }
    }

    [Fact]
    public void NoRoutineLegWithAnEndInTheWindowIsAStraightLine()
    {
        var site = Repo.Inputs.SiteData; var g = site.Grid; var straight = new List<string>(); int n = 0;
        foreach (var (key, a, b) in Legs())
        {
            if (!g.InGrid(a) && !g.InGrid(b)) continue; // both ends off the window: it never crosses the block
            n++;
            Crowd.Leg(site, a, b, out bool routed);
            if (!routed) straight.Add($"{key} ({a} → {g.NearestFreeAny(a)}, {b} → {g.NearestFreeAny(b)})");
        }
        Assert.True(n > 0, "the people file has routine legs");
        Assert.True(straight.Count == 0, $"{straight.Count} of {n} legs fall back to a straight line: {string.Join("; ", straight.Take(12))}");
    }

    [Fact]
    public void TheClerksLegToTheT1StopWalksTheGridAndLeavesAtItsCorner()
    {
        var c = Repo.Inputs.Crowd!; var site = Repo.Inputs.SiteData; var g = site.Grid;
        // engine 0.48.0 (Pulse's people rebuilt on today's plan, brief 098: the stop the clerks walk to is t1-rocket now, not t1-records): the T1 stop by its place, whatever its name
        var t1Name = c.Places.Keys.Where(k => k.StartsWith("t1-", StringComparison.Ordinal) && !k.Contains("-stand-", StringComparison.Ordinal)).OrderBy(k => k, StringComparer.Ordinal).FirstOrDefault();
        Assert.True(c.Places.ContainsKey("office-entrance") && t1Name != null, "the people file's places: the Office entrance and a T1 stop");
        var t1 = c.Places[t1Name!];
        Assert.False(g.InGrid(t1), $"{t1Name} {t1} lies off the window");
        Assert.Null(g.NearestFree(t1)); // NearestFree's 6 m search finds nothing there (the cause)
        var any = g.NearestFreeAny(t1);
        Assert.True(any.HasValue && g.Free(any.Value), "NearestFreeAny finds a free cell with no cap");
        var p = Crowd.Leg(site, c.Places["office-entrance"], t1, out bool routed);
        Assert.True(routed, "the leg is routed on the grid");
        // every stretch of it inside the window keeps to free cells: nothing walks through the Records Office
        var pts = p.Points; for (int k = 1; k + 1 < pts.Length; k++) if (g.InGrid(pts[k]) && g.InGrid(pts[k + 1])) Assert.True(g.LineFree(pts[k], pts[k + 1]), $"stretch {pts[k]} → {pts[k + 1]} is on free ground");
        Assert.Equal(any.Value, pts[pts.Length - 2]); // it leaves the window from the free cell nearest the stop
    }
}
