// Brief 098 (engine 0.50.0; Aster's ruling on the 0.49.0 report): fighters hold cover (rulebook fight.cover), the law's tests, none pinned to a
// moment: route scripts with fights in them, on seeds, under the dial's settings, each promise held whenever it comes up.
//  · no cycles: no fighter goes back to cover he left again and again (an advance, a hide, a step out of the line, an advance again); quiet-g
//    seed 1 on 0.49.0 had rival-1 between cv-035 and cv-036 six times, 50 of 50 quiet-g seeds a return, 38 a fighter with three or more;
//  · a move out of a shooter's line commits him: on its walk he is never turned to another step out of a line while the place he walks to
//    still shields him from where that shooter stands now;
//  · cover is somewhere to fight from: the range's scenarios CH-REACH-NONE and CH-REACH-CAR (ScenariosCoverHold.cs).
//   dotnet test sim/Suborned.Sim.Tests -c Release --filter FullyQualifiedName~CoverHoldTests
using System;
using System.Collections.Generic;
using System.Linq;
using IOPath = System.IO.Path; using File = System.IO.File;
using Suborned.Sim;
using Xunit;

public class CoverHoldTests
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
    // the safe fight (the rival crew) and the loud one (police officers, the response unit), on the routes that reach them
    static readonly string[] Fights = { "route-g", "route-g-board", "route-g-kill", "route-a-board", "fight-safe-unguarded", "loud-fight", "fight-loud-wait-square", "fight-loud-wait-yard" };
    static readonly int[] Seeds = { 1, 2, 3, 4, 5, 6 };

    /// <summary>Each fighter's cover points in the order he took them (the log's ai.cover), and how often he went back to one he had left.</summary>
    static Dictionary<string, int> Returns(Game g)
    {
        var seq = new Dictionary<string, (string last, HashSet<string> seen, int n)>(StringComparer.Ordinal);
        foreach (var l in g.Log)
        {
            if (l.Code != "ai.cover") continue;
            int k = l.Text.IndexOf(" takes cover at ", StringComparison.Ordinal); if (k <= 0) continue;
            string who = l.Text.Substring(0, k), rest = l.Text.Substring(k + 16), cv = rest.Substring(0, rest.IndexOf(' '));
            (string last, HashSet<string> seen, int n) s = seq.TryGetValue(who, out var x) ? x : ("", new HashSet<string>(StringComparer.Ordinal), 0);
            if (cv != s.last && s.seen.Contains(cv)) s.n++;
            s.seen.Add(cv); s.last = cv; seq[who] = s;
        }
        return seq.ToDictionary(kv => kv.Key, kv => kv.Value.n, StringComparer.Ordinal);
    }

    [Fact]
    public void NoFighterGoesBackAndForthBetweenCover()
    {
        int runs = 0, withReturn = 0, takes = 0; var bad = new List<string>();
        foreach (var counts in new[] { "unset", "people=64" })
            foreach (var route in Fights)
                foreach (var seed in Seeds)
                {
                    var g = Play(route, seed, counts, _ => { }); runs++;
                    takes += g.Log.Count(l => l.Code == "ai.cover");
                    var r = Returns(g); if (r.Values.Any(n => n > 0)) withReturn++;
                    foreach (var kv in r) if (kv.Value >= 3) bad.Add($"{route} #{seed} {counts}: {kv.Key} went back to cover he had left {kv.Value} times");
                }
        Assert.True(takes > 100, $"fighters took cover {takes} times"); // the law is looked for where it comes up
        Assert.True(bad.Count == 0, string.Join("; ", bad.Take(5))); // a cycle goes back again and again; once or twice is a fighter flanked and moving on
        Assert.True(withReturn * 3 < runs, $"a fighter went back to cover he had left in {withReturn} of {runs} runs"); // a band: fewer than one run in three (engine 0.50.0: 19 of 96, each once, an advance that met a foe and took cover to fight from; 0.49.0's quiet-g: every run, most of them three times or more)
    }

    [Fact]
    public void AStepOutOfALineIsNeverTurnedOnItsWalk()
    {
        int commits = 0; var bad = new List<string>();
        foreach (var route in Fights)
            foreach (var seed in Seeds)
            {
                var was = new Dictionary<string, (string by, Pt at, string cover, string why, Pt? end)>(StringComparer.Ordinal);
                Play(route, seed, "unset", g =>
                {
                    int sec = g.Tick / Game.Hz;
                    foreach (var h in g.Hostiles)
                    {
                        if (!h.Present || h.Down) { was.Remove(h.Id); continue; }
                        var c = h.Gun; Pt? end = h.Moving(g.Tick) ? h.Run!.End : (Pt?)null;
                        // walking his move out of a line last tick, and this tick a new move out of a line (committed again) to somewhere else: only
                        // if the place he walked to no longer shields him from the shooter he moves for now (flanked, or a shooter it doesn't shield)
                        if (was.TryGetValue(h.Id, out var w) && w.by.Length > 0 && w.cover.Length > 0 && w.end == w.at && c.Commit.Length > 0 && end.HasValue && end != w.at)
                        {
                            var op = g.Ops.FirstOrDefault(o => Op.Name(o.Id) == c.Commit); var cp = g.S.Cover.Find(p => p.Id == w.cover);
                            if (op != null && cp != null && !op.Down && !op.Inside && CoverRules.Protects(cp, op.Pos, sec, g.R.PointBlankCm))
                                bad.Add($"{route} #{seed} {Report.Clock(g.Tick)}: {h.Id}, walking to {w.cover} out of the {w.by}'s line, was turned to another step out of the {c.Commit}'s while it still shielded him");
                        }
                        if (c.Commit.Length > 0 && (!was.TryGetValue(h.Id, out var w0) || w0.at != c.CommitAt)) commits++;
                        was[h.Id] = (c.Commit, c.CommitAt, c.CommitCover, h.Why, end);
                    }
                });
            }
        Assert.True(commits > 20, $"fighters committed to a move out of a line {commits} times");
        Assert.True(bad.Count == 0, string.Join("; ", bad.Distinct().Take(5)));
    }
}
