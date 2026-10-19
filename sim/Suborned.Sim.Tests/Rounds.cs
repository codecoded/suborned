// Brief 022 (Pulse; Sky 2026-10-18): the living café. The engine's round is tools/routines/rounds.ts poseAt: it reproduces Pulse's reference
// vectors (sim/data/handover.rounds.json `vectors`) to 30 cm and by phase; and with the round switched off (Start.Rounds false, the migration
// gate) every scenario and recorded route plays exactly as it does with no rounds file at all.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class RoundsTests
{
    static readonly Engine.Inputs I = Repo.Inputs;
    static readonly Engine.Inputs NoRounds = Engine.FromTexts(I.Rulebook, I.PeopleRules, I.Site, I.People, I.Provisional.Length > 0 ? I.Provisional : null, I.Takedown.Length > 0 ? I.Takedown : null);

    public static IEnumerable<object[]> Kinds => new[] { "free", "held", "called", "ended" }.Select(k => new object[] { k });

    [Fact]
    public void TheMissionDataTurnsTheRoundOn()
    {
        Assert.NotNull(I.SiteData.Rounds);
        var g = Engine.New(I); Assert.True(g.RoundsOn);
        Assert.True(I.SiteData.Rounds!.Servers.ContainsKey(g.Inez.Id));
        Assert.False(new Game(I.Rules, I.SiteData, new Start { Rounds = false }, "", I.Crowd).RoundsOn);
        Assert.False(Engine.New(NoRounds).RoundsOn);
    }

    [Theory, MemberData(nameof(Kinds))]
    public void TheEngineMatchesPulsesReferencePoses(string kind)
    {
        var R = I.SiteData.Rounds!; var g = Engine.New(I);
        foreach (var (id, sv) in R.Servers)
        {
            var rows = R.Vectors[id][kind]; Assert.NotEmpty(rows);
            int o0 = sv.Origin * Game.Hz; var e0 = sv.Exc[0]; int at0 = o0 + e0.Start * Game.Hz;
            var holds = Array.Empty<(int, int, Pt)>(); (int, int, int, Pt)? call = null; int end = -1;
            if (kind == "held") holds = new[] { (at0 + 50, at0 + 50 + 420, new Pt(100, 0)) };
            if (kind == "called")
            {
                int gw = (int)Math.Round(R.GuardStopWalkCm / (double)R.PaceCmPerS * Game.Hz, MidpointRounding.AwayFromZero), c0 = at0 + (int)Math.Floor(e0.Out * Game.Hz * 0.5);
                call = (c0, c0 + gw, c0 + gw + R.StayS * Game.Hz, R.GuardStopAt);
            }
            if (kind == "ended") { var mid = sv.Exc[Math.Min(1, sv.Exc.Count - 1)]; end = o0 + mid.Start * Game.Hz + mid.Out * Game.Hz + 20; }
            foreach (var (tick, pos, _, phase) in rows)
            {
                var q = g.RoundReference(id, tick, holds, call, end);
                Assert.True(q.Phase == phase, $"{kind} @{tick}: phase {q.Phase}, want {phase}");
                Assert.True(G.Within(q.Pos, pos, 30), $"{kind} @{tick}: at {q.Pos}, want {pos} (30 cm)");
            }
        }
    }

    [Fact]
    public void WithTheRoundOffEveryScenarioPlaysAsWithNoRoundsFile()
    {
        foreach (var sc in Scenarios.All)
        {
            if (Scenarios.PlaysTheRound.Contains(sc.Id)) continue; // the round's own cases (N130 to N133) read it
            var off = Start.Parse(JNode.Parse(sc.Start.Canonical())); off.Rounds = false;
            var a = new Run(new Game(NoRounds.Rules, NoRounds.SiteData, Start.Parse(JNode.Parse(sc.Start.Canonical())), "gate", NoRounds.Crowd)); sc.Body(a); a.G.Finish();
            var b = new Run(new Game(I.Rules, I.SiteData, off, "gate", I.Crowd)); sc.Body(b); b.G.Finish();
            Assert.True(Same(a.G, b.G), $"{sc.Id}: rounds off differs from no rounds file");
        }
    }

    [Fact]
    public void WithTheRoundOffEveryRecordedRoutePlaysAsWithNoRoundsFile()
    {
        var root = Engine.FindRoot(AppContext.BaseDirectory); int n = 0;
        foreach (var f in Directory.GetFiles(System.IO.Path.Combine(root, "contracts/missions/handover/routes"), "*.json").OrderBy(x => x, StringComparer.Ordinal))
        {
            var txt = File.ReadAllText(f); if (!txt.Contains("suborned.sim.script/1")) continue;
            var sc = Script.Parse(txt); n++;
            var off = Start.Parse(JNode.Parse(sc.Start.Canonical())); off.Rounds = false;
            var a = Engine.Play(NoRounds, sc.Start, sc.Orders, sc.Until); var b = Engine.Play(I, off, sc.Orders, sc.Until);
            Assert.True(Same(a, b), $"{System.IO.Path.GetFileName(f)}: rounds off differs from no rounds file");
        }
        Assert.True(n > 10);
    }

    /// <summary>The same behaviour: every log line's tick and code, every order's answer, the outcome and the numbers.</summary>
    static bool Same(Game a, Game b) =>
        a.Log.Count == b.Log.Count && a.Log.Zip(b.Log).All(x => x.First.Tick == x.Second.Tick && x.First.Code == x.Second.Code)
        && a.Orders.Count == b.Orders.Count && a.Orders.Zip(b.Orders).All(x => x.First.ok == x.Second.ok && x.First.code == x.Second.code)
        && (a.Result, a.FailCode, a.Tick, a.Trail, a.Alert, a.Cash, a.Standing) == (b.Result, b.FailCode, b.Tick, b.Trail, b.Alert, b.Cash, b.Standing);

    [Fact]
    public void HeadingSkipsZeroLengthSegments()
    {
        var p = new Suborned.Sim.Path(new[] { new Pt(0, 0), new Pt(0, 0), new Pt(100, 0), new Pt(100, 0) });
        Assert.Equal(new Pt(100, 0), p.Heading(0));
        Assert.Equal(new Pt(100, 0), p.Heading(p.Length));
        Assert.Equal(new Pt(0, 0), new Suborned.Sim.Path(new[] { new Pt(5, 5), new Pt(5, 5) }).Heading(0));
    }
}
