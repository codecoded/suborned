// Brief 098, Sky's calls on walking and listening (2026-11-08: "Sit them down if they're listening, they're just regular people"; engine 0.49.0):
// the laws' tests, none pinned to a moment: route scripts on seeds under the dial's settings, each promise held whenever it comes up.
//  · an idle operative (no order, off the place the player put him) makes room as a person of the city does, and steps back; never while he acts,
//    walks, listens or sits, never at the place the player put him (Aster's reading, for Sky to confirm);
//  · a listening Scout with a chair sits in it, a body the law never moves; with none, he stands by a wall and never across a passage.
//   dotnet test sim/Suborned.Sim.Tests -c Release --filter FullyQualifiedName~BodiesIdleTests
using System;
using System.Collections.Generic;
using System.Linq;
using IOPath = System.IO.Path; using File = System.IO.File;
using Suborned.Sim;
using Xunit;

public class BodiesIdleTests
{
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);
    static Game Play(string route, int seed, string counts, Action<Game> read)
    {
        var s = Script.Parse(File.ReadAllText(IOPath.Combine(Root, "contracts/missions/handover/routes", route + ".json"))); s.Start.Seed = seed; s.Start.Counts.Clear();
        if (counts != "unset") foreach (var kv in counts.Split(',')) { var p = kv.Split('='); s.Start.Counts[p[0]] = int.Parse(p[1]); }
        var g = Engine.New(Repo.Inputs, s.Start); int until = s.Until > 0 ? s.Until : Game.T(Repo.Inputs.SiteData.Over);
        var orders = s.Orders.Select((o, k) => (o, k)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).Select(x => x.o).ToList(); int oi = 0;
        read(g);
        while (g.Tick < until && !g.Over) { while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]); g.Step(); read(g); }
        g.Finish(); return g;
    }
    static readonly string[] Cell = { "scout", "fixer", "tech", "enforcer" };

    [Fact]
    public void AnIdleOperativeMakesRoomAndStepsBackAsAnyoneDoes()
    {
        int asks = 0; var bad = new List<string>(); var since = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (route, counts) in new[] { ("route-a", "people=64"), ("route-a", "x4"), ("route-g", "people=64"), ("careless-takedown-seen-crowd", "people=64") })
            foreach (var seed in new[] { 1, 2, 3 })
                Play(route, seed, counts == "x4" ? "people=64,guards=32,cars=8" : counts, g =>
                {
                    foreach (var id in Cell)
                    {
                        var op = g.Ops[Array.IndexOf(Cell, id)];
                        if (g.BodyMove(id) is not BodyMoveRead m || m.RoomFor == null) { since.Remove(route + seed + id); continue; }
                        bool own = Array.IndexOf(Cell, m.RoomFor) >= 0; // the cell for the cell: engine 0.48.0's own side
                        if (!since.ContainsKey(route + seed + id)) { since[route + seed + id] = g.Tick; if (!own) asks++; }
                        if (own) continue;
                        if (!g.BodyIdle(id)) bad.Add($"{route} #{seed} {Report.Clock(g.Tick)}: {id} made room for {m.RoomFor} but is not idle");
                        if (op.Busy != null || op.Act != null || op.Walk != null || g.SeatedAt(id).HasValue || op.Gun.Drawn) bad.Add($"{route} #{seed} {Report.Clock(g.Tick)}: {id} made room for {m.RoomFor} while at work, walking, seated or drawn");
                        if (op.PlacedAt is Pt pa && G.Within(op.Body.Raw, pa, g.R.BodyMinCm + g.R.BodySpaceCm)) bad.Add($"{route} #{seed} {Report.Clock(g.Tick)}: {id} made room at the place the player put him");
                        if (g.Tick - since[route + seed + id] > Game.T(6) + 1) bad.Add($"{route} #{seed} {Report.Clock(g.Tick)}: {id} stood aside for {m.RoomFor} over 6 s");
                    }
                });
        Assert.True(asks > 0, $"idle operatives made room {asks} times");
        Assert.True(bad.Count == 0, string.Join("; ", bad.Distinct().Take(5)));
    }

    [Fact]
    public void AListeningScoutSitsInHerChairOrStandsByAWallOutOfTheWay()
    {
        int seated = 0, ground = 0; var bad = new List<string>(); var seen = new HashSet<string>(); var chose = new HashSet<string>(); // engine 0.55.0 (cell.listen.follow): he settles after the start, so each Listen's arrival at its spot counts once
        foreach (var route in new[] { "route-a", "route-a-pressure", "careless-takedown-seen-crowd" })
            foreach (var seed in new[] { 1, 2, 3, 4, 5, 6 })
                Play(route, seed, "unset", g =>
                {
                    var op = g.Ops[0]; var n = g.ListenSpotNow("scout"); if (op.Busy?.Do != "listen" || !n.HasValue) return;
                    var (at, chair, _, _) = n.Value; string key = $"{route}#{seed}#{op.Busy.Tick}#{at}";
                    if (chair.Length == 0 && chose.Add(key) && g.ListenChairFor("scout", op.Busy.Target) is string free) bad.Add($"{route} #{seed} {Report.Clock(g.Tick)}: chose a wall with {free} free (chair first)"); // engine 0.56.0: a wall only when no chair is free, read on the tick the spot first shows
                    if (op.Walk != null) return;
                    if (chair.Length > 0)
                    {
                        if (op.Pos != at) return; // not in yet
                        if (seen.Add(key)) seated++;
                        if (g.SeatedAt("scout")?.chair != chair) bad.Add($"{route} #{seed} {Report.Clock(g.Tick)}: listening from {chair} but not seated in it");
                        if (!g.BodyIsTenet("scout")) bad.Add($"{route} #{seed} {Report.Clock(g.Tick)}: seated, but a body the law may move");
                    }
                    else
                    {
                        if (!G.Within(op.Pos, at, g.R.ListenAtSpotCm)) return;
                        if (seen.Add(key)) ground++;
                        if (!g.BesideSolid(at)) bad.Add($"{route} #{seed}: listening from {at}, not by a wall");
                        if (g.S.Grid.Cuts(at, g.R.BodyMinCm, g.R.ListenPassCm)) bad.Add($"{route} #{seed}: listening from {at}, across a passage");
                    }
                });
        Assert.True(seated + ground > 0, $"the Scout settled {seated} times seated, {ground} by a wall"); // engine 0.56.0 (Sky 2026-11-12: "the scout (she) prefers a chair"): a wall only when no chair is free (above); on these routes the café's chairs in Inez's ring are taken (LS-CHAIR waits for one)
        Assert.True(bad.Count == 0, string.Join("; ", bad.Distinct().Take(5)));
    }

    /// <summary>A 4 m square walk grid whose free raster cells are those `free(x cm)` says, along every row.</summary>
    static Grid Synthetic(Func<int, bool> free)
    {
        var rows = new System.Text.StringBuilder(); var fine = new System.Text.StringBuilder();
        for (int i = 0; i < 8; i++) rows.Append(i > 0 ? "," : "").Append('"').Append(new string(free(i * 50 + 25) ? '.' : '#', 8)).Append('"');
        for (int i = 0; i < 80; i++) fine.Append(i > 0 ? "," : "").Append(free(i * 5 + 2) ? "[80]" : "[0,80]");
        return new Grid(JNode.Parse($"{{\"x0\":0,\"y0\":0,\"cell\":50,\"nx\":8,\"ny\":8,\"body\":35,\"rows\":[{rows}],\"fine\":{{\"cell\":5,\"nx\":80,\"ny\":80,\"rows\":[{fine}]}}}}"));
    }

    [Fact]
    public void ABodyStandingInAOneBodyCorridorCutsItAndOneByAWallInTheOpenDoesNot()
    {
        int two = Repo.Inputs.Rules.BodyMinCm, pass = Repo.Inputs.Rules.ListenPassCm;
        var corridor = Synthetic(x => x >= 85 && x <= 125); // a walker's centre has 40 cm between two long walls (1.1 m apart)
        Assert.True(corridor.Cuts(new Pt(105, 200), two, pass), "in the corridor: nobody gets past");
        Assert.True(corridor.Cuts(new Pt(87, 200), two, pass), "by one of its walls: still nobody gets past");
        var open = Synthetic(x => x >= 85); // one wall, open ground beyond
        Assert.False(open.Cuts(new Pt(87, 200), two, pass), "by the wall in the open: the way past is on his open side");
        Assert.False(open.Cuts(new Pt(250, 200), two, pass), "in the open: people walk round him");
    }
}
