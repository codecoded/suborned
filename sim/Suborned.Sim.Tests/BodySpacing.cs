// Engine 0.36.0 (brief 079, Sky on the recorded fight, route loud-fight: "Players were walking through the car at the beginning", "Rivals were
// overlapping each other"). Replays the repo's route scripts tick by tick and reads where everybody is. A test, not a scenario: it reads the
// routes (as GateC037). Reads only: the checkpoint hash at the end must match Engine.Play's (ticking one at a time moves nothing).
using System;
using System.Collections.Generic;
using System.Linq;
using IOPath = System.IO.Path; using File = System.IO.File;
using Suborned.Sim;
using Xunit;

public class BodySpacingTests
{
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);
    static Script Load(string id) => Script.Parse(File.ReadAllText(IOPath.Combine(Root, "contracts/missions/handover/routes", id + ".json")));

    /// <summary>Plays a route one tick at a time; `read` sees the game after each tick's orders.</summary>
    static Game Walk(Script s, Action<Game> read)
    {
        var g = Engine.New(Repo.Inputs, s.Start); int until = s.Until <= 0 ? Game.T(Repo.Inputs.SiteData.Over) : s.Until;
        var list = s.Orders.Select((o, k) => (o, k)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).ToList(); int next = 0;
        for (int t = 0; t <= until; t++)
        {
            if (t > 0) g.RunTo(t);
            if (g.Over && g.Tick != t) break;
            while (next < list.Count && list[next].o.Tick == t) g.Issue(list[next++].o);
            read(g);
            if (g.Over) break;
        }
        g.Finish(); return g;
    }

    /// <summary>How deep p stands inside a car's body (cm), 0 outside.</summary>
    static long Depth(Game g, Game.StandingCar c, Pt p)
    {
        long hx = c.Heading.X, hy = c.Heading.Y, n = Math.Max(1, G.Isqrt(hx * hx + hy * hy)), dx = p.X - c.At.X, dy = p.Y - c.At.Y;
        long along = Math.Abs(dx * hx + dy * hy) / n, across = Math.Abs(dx * hy - dy * hx) / n;
        return Math.Max(0, Math.Min(g.R.CarLenCm / 2 - along, g.R.CarWCm / 2 - across));
    }

    static IEnumerable<(string id, Pt at)> Fighters(Game g)
    {
        foreach (var m in g.Crew) if (m.Mode != CrewMode.Aboard && m.Mode != CrewMode.Gone && !m.Gun.Down && g.PosOf(m.Id) is Pt p) yield return (m.Id, p);
        foreach (var h in g.Hostiles) if (!h.Id.StartsWith("rival", StringComparison.Ordinal) && h.Present && !h.Down && h.Gun.St != "gone") yield return (h.Id, h.Pos(g.Tick));
    }

    [Theory]
    [InlineData("loud-fight")] [InlineData("loud")] [InlineData("fight-loud-wait-square")] [InlineData("fail-enforcer-down")]
    public void TheResponseUnitWalksRoundTheStandingCar(string id)
    {
        var worst = new List<string>();
        Walk(Load(id), g =>
        {
            foreach (var k in new[] { "security-1", "security-2" })
                if (g.GunOf(k) is Gun gn && !gn.Down && g.PosOf(k) is Pt p)
                    foreach (var c in g.StandingCars()) if (Depth(g, c, p) > 0) worst.Add($"{k} {Depth(g, c, p)} cm in {c.Id} at t{g.Tick}");
        });
        Assert.True(worst.Count == 0, $"{id}: {worst.Count} ticks, first {worst.FirstOrDefault()}");
    }

    [Fact]
    public void NoOneStandsInTheCarThatParkedOnThem()
    {
        var hits = new List<string>(); int parked = 0;
        Walk(Load("fail-vale-leaves"), g =>
        {
            var c = g.StandingCars().Where(x => x.Id == "car:rivals").ToList(); if (c.Count == 0) return; parked++;
            var op = g.OpOf(OpId.Scout); if (Depth(g, c[0], op.Pos) > 0 && op.Walk == null) hits.Add($"t{g.Tick} {op.Pos}");
        });
        Assert.True(parked > 0, "the rival car parks in this route");
        Assert.True(hits.Count <= 1, $"the Scout stands in the rival car for {hits.Count} ticks, first {hits.FirstOrDefault()}"); // the step off starts the tick after the car stops
    }

    [Theory]
    [InlineData("loud-fight")] [InlineData("loud")] [InlineData("route-a")] [InlineData("route-g")]
    public void NoTwoFightersStandInOneAnother(string id)
    {
        const int Radius = 25; // a body's half width (cm): closer than twice this, standing still, they overlap
        var still = new Dictionary<string, (Pt at, int since)>(); var bad = new List<string>();
        Walk(Load(id), g =>
        {
            var now = Fighters(g).GroupBy(x => x.id).Select(x => x.First()).ToList();
            foreach (var (who, at) in now) if (!still.TryGetValue(who, out var s) || s.at != at) still[who] = (at, g.Tick);
            for (int a = 0; a < now.Count; a++) for (int b = a + 1; b < now.Count; b++)
            {
                var (x, y) = (now[a], now[b]);
                if (G.Dist(x.at, y.at) >= 2 * Radius) continue;
                if (g.Tick - still[x.id].since >= 5 && g.Tick - still[y.id].since >= 5) bad.Add($"{x.id}+{y.id} {G.Dist(x.at, y.at)} cm at t{g.Tick} {x.at}");
            }
        });
        Assert.True(bad.Count == 0, $"{id}: {bad.Count} ticks of two standing in one spot, first {bad.FirstOrDefault()}");
    }

    [Fact]
    public void OfficersFromOneEntryComeInFile()
    {
        int together = 0, seen = 0;
        var seeded = Load("loud-fight"); seeded.Start.Seed = 3; // brief 080: seed 0's fixed opening is refused at the door (entry-seen-guard); this seed plays the fight
        Walk(seeded, g =>
        {
            var o = g.Hostiles.Where(h => h.Id.StartsWith("officer-", StringComparison.Ordinal) && h.Present && !h.Down).ToList(); if (o.Count < 2) return;
            seen++; if (G.Dist(o[0].Pos(g.Tick), o[1].Pos(g.Tick)) < g.R.BodyClearCm && g.Tick > g.FightStart + g.R.BodyFileTicks + 1) together++;
        });
        Assert.True(seen > 0); Assert.True(together < 20, $"the two officers walk as one for {together} ticks");
    }

    [Fact]
    public void ClearStopChangesNothingWhenNobodyIsNear()
    {
        // an uncrowded walk ends where it was sent
        var g = Engine.New(Repo.Inputs); var to = g.S.Stand("d:Courtyard"); g.Step();
        Assert.True(g.Issue(new Order { Tick = g.Tick, Op = "fixer", Do = "move", To = "d:Courtyard" }).ok);
        Assert.Equal(to, g.Ops[(int)OpId.Fixer].Walk!.End);
    }

    [Theory]
    [InlineData("loud-fight")] [InlineData("route-b")]
    public void TickingOneAtATimeMovesNothing(string id)
    {
        var s = Load(id); var a = Engine.Play(Repo.Inputs, s.Start, s.Orders, s.Until); var b = Walk(s, _ => { });
        Assert.Equal(Hash.Hex(a.Checkpoints[^1].hash), Hash.Hex(b.Checkpoints[^1].hash));
    }
}
