// Brief 080 (engine 0.37.0): the seeded start and rolled shots over the real route scripts, and brief 081's viewer findings as rules that hold on any seed.
// The route scripts live in contracts/, so these play in `dotnet test`, not in Unity; ScenariosSeed.cs has the ones Unity plays too.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class SeedTests
{
    static readonly string Routes = System.IO.Path.Combine(Engine.FindRoot(AppContext.BaseDirectory), "contracts", "missions", "handover", "routes");
    static Script Route(string name, int seed) { var s = Script.Parse(File.ReadAllText(System.IO.Path.Combine(Routes, name + ".json"))); s.Expect = null; s.Start.Seed = seed; return s; }
    static Game Play(string name, int seed) { var s = Route(name, seed); return Engine.Play(Repo.Inputs, s.Start, s.Orders, s.Until); }

    /// <summary>Plays a route tick by tick and hands each tick to `each`; reads only.</summary>
    static Game Walk(string name, int seed, Action<Game> each)
    {
        var s = Route(name, seed); var g = Engine.New(Repo.Inputs, s.Start); int until = s.Until <= 0 ? Game.T(Repo.Inputs.SiteData.Over) : s.Until;
        var os = s.Orders.Select((o, k) => (k, o)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).Select(x => x.o).ToList(); int oi = 0;
        for (int t = 0; t <= until && !g.Over; t++)
        {
            if (t > 0) g.RunTo(t);
            while (oi < os.Count && os[oi].Tick == t) { g.Issue(os[oi]); oi++; }
            each(g);
        }
        return g;
    }

    [Theory]
    [InlineData("route-a")] [InlineData("route-b")] [InlineData("route-g")] [InlineData("loud-fight")]
    public void SameSeedSameHashDifferentSeedDifferentHash(string route)
    {
        var a = Play(route, 11); var b = Play(route, 11); var c = Play(route, 12);
        Assert.Equal(a.Checkpoints, b.Checkpoints);
        Assert.NotEqual(a.Checkpoints[^1].hash, c.Checkpoints[^1].hash);
        Assert.Equal(a.Checkpoints, Engine.Replay(Repo.Inputs, a).Checkpoints);
    }

    [Fact]
    public void AHostReadDoesNotMoveAStream()
    {
        var plain = Play("route-a", 21); int reads = 0;
        var read = Walk("route-a", 21, g =>
        {
            foreach (var op in g.Ops)
            {
                if (op.Gun.W == null) continue;
                foreach (var m in g.Crew) { _ = g.PreviewFire(op.Id, m.Id); reads++; }
                _ = g.RangeLines(); _ = g.GoingLoud(new Order { Tick = g.Tick, Op = Op.Name(op.Id), Do = "draw" }); _ = g.Routine("patrol", 100);
            }
        });
        read.Finish();
        Assert.True(reads > 1000);
        Assert.Equal(Hash.Hex(plain.Checkpoints[^1].hash), Hash.Hex(read.Checkpoints[^1].hash));
    }

    [Fact]
    public void OneMoreShotByOneShooterMovesNoOtherStream()
    {
        // the draw is a pure function of (seed, shooter, stage, index): the same index draws the same number whoever else has fired
        uint k = Streams.Fnv1a32("9/scout/shot");
        var m = new Streams.Mulberry("9/scout/shot");
        for (int n = 1; n <= 50; n++) Assert.Equal(m.Next(), Streams.Nth(k, n));
        Assert.NotEqual(Streams.Nth(Streams.Fnv1a32("9/scout/shot"), 1), Streams.Nth(Streams.Fnv1a32("9/fixer/shot"), 1));
    }

    [Fact]
    public void ASeedOutOfRangeAndAnUnknownSheetAreRejected()
    {
        Assert.Throws<FormatException>(() => Script.Parse("{\"schema\":\"suborned.sim.script/1\",\"mission\":\"handover\",\"start\":{\"seed\":-1},\"orders\":[]}"));
        Assert.Throws<FormatException>(() => Script.Parse("{\"schema\":\"suborned.sim.script/1\",\"mission\":\"handover\",\"start\":{\"seed\":2147483648},\"orders\":[]}"));
        var root = Engine.FindRoot(AppContext.BaseDirectory); var book = File.ReadAllText(System.IO.Path.Combine(root, "contracts", "rules", "rulebook.json"));
        string Load(string text) { try { new Rules(text, File.ReadAllText(System.IO.Path.Combine(root, "contracts", "people", "people.rules.json"))); return ""; } catch (FormatException e) { return e.Message; } }
        Assert.Equal("", Load(book));
        Assert.Contains("limit", Load(book.Replace("\"sheet-max\": { \"v\": 95", "\"sheet-max\": { \"v\": 100")));
        Assert.Contains("nope", Load(book.Replace("\"warnHealth\": \"v:sheet-warn-health\",", "\"nope\": 1, \"warnHealth\": \"v:sheet-warn-health\",")));
    }

    /// <summary>Brief 081, finding 1: a body never jumps. Everyone who fights moves at a body's pace, on every seed.</summary>
    [Theory]
    [InlineData("loud-fight", 3)] [InlineData("loud-fight", 7)] [InlineData("loud", 0)] [InlineData("route-b", 5)] [InlineData("route-g", 9)]
    public void NoFighterJumps(string route, int seed)
    {
        var last = new Dictionary<string, Pt>(); var jumps = new List<string>();
        Walk(route, seed, g =>
        {
            foreach (var m in g.Crew)
            {
                if (m.Gun.Down || m.Mode == CrewMode.Aboard || m.Mode == CrewMode.Boarding) { last.Remove(m.Id); continue; }
                var p = g.PosOf(m.Id); if (!p.HasValue) { last.Remove(m.Id); continue; }
                if (last.TryGetValue(m.Id, out var q) && G.Dist(p.Value, q) > 150) jumps.Add($"{m.Id} {G.Dist(p.Value, q)} cm at {Report.Clock(g.Tick)} ({m.Mode} {m.Goal})");
                last[m.Id] = p.Value;
            }
        });
        Assert.True(jumps.Count == 0, string.Join("; ", jumps.Take(4)));
    }

    /// <summary>Brief 081, finding 2: officers sent to one place don't stack; two of the response stand apart once they have stopped.</summary>
    [Theory]
    [InlineData("loud", 0)] [InlineData("loud-fight", 3)] [InlineData("fail-enforcer-down", 0)] [InlineData("route-g", 0)]
    public void TheResponseDoesNotStack(string route, int seed)
    {
        var together = new Dictionary<string, int>(); int worst = 0; string who = "";
        Walk(route, seed, g =>
        {
            var at = new List<(string id, Pt p)>();
            foreach (var u in g.Units) if (!u.Gun.Down && g.PosOf(u.Id) is Pt up && u.OnStreet(g.Tick)) at.Add((u.Id, up));
            foreach (var h in g.Hostiles) if (!h.Down && h.Present && !at.Exists(x => x.id == h.Id)) at.Add((h.Id, h.Pos(g.Tick)));
            if (!g.GuardDown && g.GuardPose() is var gp && gp.HasValue) at.Add(("guard", gp.Value.pos));
            for (int a = 0; a < at.Count; a++) for (int b = a + 1; b < at.Count; b++)
            {
                var key = at[a].id + "+" + at[b].id;
                if (G.Within(at[a].p, at[b].p, 20)) { together.TryGetValue(key, out int n); together[key] = ++n; if (n > worst) { worst = n; who = key; } } else together[key] = 0;
            }
        });
        Assert.True(worst <= Game.T(3), $"{who} stood in one place for {worst} ticks");
    }

    /// <summary>Brief 081, finding 4: a walk ends beside a person, not in them. The cell's own moves too (fight.spacing.cell).</summary>
    [Theory]
    [InlineData("route-b")] [InlineData("fail-rivals-reach-vale")] [InlineData("route-a")]
    public void AnAgentSentToAPersonStopsBesideThem(string route)
    {
        int worst = 0; string who = ""; var run = new Dictionary<string, int>();
        Walk(route, 0, g =>
        {
            foreach (var op in g.Ops)
            {
                if (op.Inside || op.InCar || op.Walk != null) continue;
                foreach (var (id, at) in new (string, Pt?)[] { ("driver", g.DriverAboard ? null : g.DriverPos()), ("waitress", g.PosOf("waitress")), ("guard", g.GuardDown ? null : g.GuardPose()?.pos) })
                {
                    var key = Op.Name(op.Id) + "+" + id; run.TryGetValue(key, out int n);
                    n = at.HasValue && G.Within(op.Pos, at.Value, 45) ? n + 1 : 0; run[key] = n;
                    if (n > worst) { worst = n; who = key; }
                }
            }
        });
        Assert.True(worst <= Game.T(2), $"{who} stood in one place for {worst} ticks");
    }

    [Fact]
    public void TheNeutralStartIsTheMissionsTimetable()
    {
        var g = Engine.New(Repo.Inputs); var seeded = Engine.New(Repo.Inputs, new Start { Seed = 1 });
        Assert.Equal(Game.T(g.S.RivalsArrive), g.RivalAt);
        Assert.Equal(0, g.PatrolPhase);
        Assert.NotEqual(g.StartHash, seeded.StartHash);
    }
}
