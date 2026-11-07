// Brief 098, law L9 (Bodies; engine 0.46.0; give and take, engine 0.47.0, in BodiesGiveTests): the law's tests. Laws, none pinned to a moment: route scripts played on several seeds under the dial's
// settings, every tick read by the law's independent check (BodyCheck: every upright body as the host reads it), and the law's promises held at
// whatever moment they come up. dotnet test sim/Suborned.Sim.Tests -c Release --filter FullyQualifiedName~BodiesTests
using System;
using System.Collections.Generic;
using System.Linq;
using IOPath = System.IO.Path; using File = System.IO.File;
using Suborned.Sim;
using Xunit;

public class BodiesTests
{
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);
    static Script Route(string id) => Script.Parse(File.ReadAllText(IOPath.Combine(Root, "contracts/missions/handover/routes", id + ".json")));
    static readonly string[] Routes = { "route-a", "route-b", "route-g", "loud-fight", "careless-takedown-seen-crowd" };
    static readonly int[] Seeds = { 1, 2 };

    /// <summary>A route on a seed under counts ("unset" or "kind=n,..."), one tick at a time; `read` sees the game after each tick.</summary>
    static Game Play(string route, int seed, string counts, Action<Game> read)
    {
        var s = Route(route); s.Start.Seed = seed; s.Start.Counts.Clear();
        if (counts != "unset") foreach (var kv in counts.Split(',')) { var p = kv.Split('='); s.Start.Counts[p[0]] = int.Parse(p[1]); }
        var g = Engine.New(Repo.Inputs, s.Start); int until = s.Until > 0 ? s.Until : Game.T(Repo.Inputs.SiteData.Over);
        var orders = s.Orders.Select((o, k) => (o, k)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).Select(x => x.o).ToList(); int oi = 0;
        read(g);
        while (g.Tick < until && !g.Over) { while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]); g.Step(); read(g); }
        g.Finish(); return g;
    }

    [Theory]
    [InlineData("unset")] [InlineData("people=32")] [InlineData("people=64")] [InlineData("guards=32")] [InlineData("guards=64")] [InlineData("cars=8")]
    public void NoTwoBodiesOverlapAtAnyTick(string counts)
    {
        var bad = new List<string>(); int ticks = 0, bodies = 0, tenets = 0;
        foreach (var route in Routes) foreach (var seed in Seeds)
            Play(route, seed, counts, g =>
            {
                var up = BodyCheck.Of(g); ticks++; bodies += up.Count;
                foreach (var (a, b, cm) in BodyCheck.Closer(up, g.R.BodyMinCm))
                {
                    if (a.Tenet && b.Tenet) { tenets++; continue; } // two tenets placed within two radii: the tenet wins (counted below, none today)
                    if (BodyCheck.Squeezing(g, a, b, cm)) continue; // engine 0.47.0: two squeezing past each other, two squeeze widths apart (BodiesGiveTests holds the squeeze)
                    if (bad.Count < 5) bad.Add($"{route} #{seed} {Report.Clock(g.Tick)}: {a.Id} and {b.Id} {cm} cm");
                    else bad.Add("");
                }
            });
        Assert.True(ticks > 1000 && bodies > ticks * 10, $"watched {ticks} ticks, {bodies} body-ticks");
        Assert.True(bad.Count == 0, $"{counts}: {bad.Count} pairs inside two radii, first: {string.Join("; ", bad.Where(x => x.Length > 0))}");
        Assert.Equal(0, tenets);
    }

    [Fact]
    public void AStopHeldByOneTakesTheNextBesideItAQueueOrAnArcOrACrewSpot()
    {
        int queues = 0, crews = 0; var bad = new List<string>();
        foreach (var (route, counts) in new[] { ("careless-takedown-seen-crowd", "people=64"), ("route-a", "guards=64"), ("loud-fight", "guards=32,people=32") })
        {
            var was = new Dictionary<string, Pt>(StringComparer.Ordinal); // where each stood the tick before a look: at rest, it has not moved since
            Play(route, 1, counts, g =>
            {
                if (g.Tick % 10 == 9) { was.Clear(); foreach (var w in g.Walkers) if (g.BodyInfo(w.P.Id) is var bw && bw.HasValue) was[w.P.Id] = bw.Value.at; }
                if (g.Tick % 10 != 0) return;
                var at = new Dictionary<string, List<(string id, Pt at, Pt raw, int form, Pt face, int slot)>>(StringComparer.Ordinal);
                foreach (var w in g.Walkers)
                    if (g.BodyInfo(w.P.Id) is var bi && bi.HasValue && bi.Value.stop != null && bi.Value.cls == 2 && bi.Value.form != 0)
                    {
                        if (!at.TryGetValue(bi.Value.stop, out var l)) at[bi.Value.stop] = l = new List<(string, Pt, Pt, int, Pt, int)>();
                        l.Add((w.P.Id, bi.Value.at, bi.Value.raw, bi.Value.form, bi.Value.face, bi.Value.slot));
                    }
                foreach (var kv in at)
                {
                    var l = kv.Value.Where(x => G.Within(x.raw, kv.Value[0].raw, 1)).ToList(); if (l.Count < 2) continue; // more than one sent to the one spot
                    bool crew = l.All(x => x.form == 2);
                    if (crew) crews++; else queues++;
                    var spots = l.Select(x => x.at).Distinct().Count();
                    if (spots != l.Count) bad.Add($"{kv.Key} at {Report.Clock(g.Tick)}: {l.Count} on {spots} spots");
                    int reach = crew ? 2 * g.R.BodyCrewCm + g.R.BodyMinCm : 5 * g.R.BodyQueueCm + g.R.BodySearchCm;
                    foreach (var x in l) if (was.TryGetValue(x.id, out var xw) && xw == x.at && !g.BodyHeld(x.id) && !G.Within(x.at, x.raw, reach)) bad.Add($"{x.id} at {kv.Key} stands {G.Dist(x.at, x.raw)} cm from it ({Report.Clock(g.Tick)})"); // at rest, settled (one still on his way is behind; one held on it, engine 0.47.0, is not yet there)
                    if (crew) // armed at one post, at rest: the holder and those at crew spots bodies.crew apart, each to his sector; one with no spot free, personal space (one just come, stepping to his spot, is on his way)
                    {
                        bool still(int k) => was.TryGetValue(l[k].id, out var p) && p == l[k].at && !g.BodyHeld(l[k].id); // settled, not held on its way
                        for (int i = 0; i < l.Count; i++) for (int j = i + 1; j < l.Count; j++)
                        {
                            if (!still(i) || !still(j)) continue; bool both = l[i].slot >= 0 && l[j].slot >= 0; int d = G.Dist(l[i].at, l[j].at);
                            if (both ? d < g.R.BodyCrewCm / 2 : d < g.R.BodySpaceCm) bad.Add($"{l[i].id} and {l[j].id} at {kv.Key}: {d} cm, {(both ? "two crew spots under half a crew's spread" : "inside personal space")} ({Report.Clock(g.Tick)})");
                        }
                    }
                }
            });
        }
        Assert.True(bad.Count == 0, string.Join("; ", bad.Take(5)));
        Assert.True(queues > 0, "a stop held by one and wanted by another (a queue or an arc) came up");
        Assert.True(crews > 0, "a post held by one guard and wanted by another (a crew spot) came up");
    }

    [Theory] [InlineData(32)] [InlineData(64)]
    public void TheDialsAddedGuardsTakeSeparatePostsFirstAndStandApartWhenTheyShare(int guards)
    {
        var bad = new List<string>(); int shared = 0, pairs = 0, still = 0; var was = new Dictionary<string, Pt>(StringComparer.Ordinal);
        var g = Play("route-a", 1, $"guards={guards}", h =>
        {
            var stand = h.Walkers.Where(w => w.Added && w.P.Job == "guard" && h.BodyInfo(w.P.Id) is var bi && bi.HasValue && bi.Value.stop != null)
                .Select(w => (w.P.Id, info: h.BodyInfo(w.P.Id)!.Value)).ToList();
            if (h.Tick % 10 == 0)
                foreach (var grp in stand.GroupBy(x => x.info.stop))
                {
                    var l = grp.ToList(); if (l.Count < 2) continue; shared++;
                    for (int i = 0; i < l.Count; i++) for (int j = i + 1; j < l.Count; j++)
                    {
                        pairs++; // at rest: neither moved since the tick before (one just come, stepping to his crew spot, is on his way)
                        if (!(was.TryGetValue(l[i].Id, out var pi) && pi == l[i].info.at && was.TryGetValue(l[j].Id, out var pj) && pj == l[j].info.at) || h.BodyHeld(l[i].Id) || h.BodyHeld(l[j].Id)) continue; // settled: neither moved, neither held on his way
                        still++; int d = G.Dist(l[i].info.at, l[j].info.at);
                        if (d < h.R.BodySpaceCm) bad.Add($"{l[i].Id} and {l[j].Id} share {grp.Key}: {d} cm ({Report.Clock(h.Tick)})");
                    }
                }
            was.Clear(); foreach (var x in stand) was[x.Id] = x.info.at;
        });
        var added = g.Walkers.Where(w => w.Added && w.P.Job == "guard").ToList();
        var points = added.Select(w => g.StopsNow(w.P.Id)!.First(s => s.outside).place).ToList();
        int distinctFirst = points.Distinct().Count(), sitePoints = Population.PointsOf(g.S, Repo.Inputs.Population!).Posts.Count;
        Assert.Equal(Math.Min(added.Count, sitePoints), distinctFirst); // one guard to a point before any shares (step 0's rule, kept)
        Assert.True(bad.Count == 0, $"{bad.Count} times two guards stood inside personal space at one post: {string.Join("; ", bad.Take(3))}");
        Assert.True(still * 10 >= pairs * 8, $"guards sharing a post settle: {still} of {pairs} looks at a pair found both at rest"); // a band: neither jostles for the post
        if (guards == 64) Assert.True(shared > 0, "with more guards than points some share a post, and stand apart");
    }

    [Fact]
    public void TheDrawnRivalCrewSpreadsAtTheOfficeFrontEachToItsSector()
    {
        int seen = 0; var bad = new List<string>();
        foreach (var seed in new[] { 1, 2, 3 })
            Play("loud-fight", seed, "unset", g =>
            {
                var front = g.Crew.Where(m => !m.Fight && m.Mode == CrewMode.Posted && m.Post == "front" && !m.Gun.Down && m.Disp == null && g.MemberPose(m).HasValue).ToList(); // a man on the kite's step off his line is the fight's (fight.kite, brief 081)
                if (front.Count < 2) return; seen++;
                for (int i = 0; i < front.Count; i++) for (int j = i + 1; j < front.Count; j++)
                {
                    var a = g.MemberPose(front[i])!.Value; var b = g.MemberPose(front[j])!.Value; Pt fa = front[i].Base, fb = front[j].Base; // each man's sector (his watch beats turn him round it)
                    if (G.Dist(a.pos, b.pos) < g.R.BodyCrewCm - 60) bad.Add($"{front[i].Id} and {front[j].Id} {G.Dist(a.pos, b.pos)} cm apart");
                    long dot = (long)fa.X * fb.X + (long)fa.Y * fb.Y, la = G.Isqrt(G.D2(fa, default)), lb = G.Isqrt(G.D2(fb, default));
                    if (front[i].Post == front[j].Post && dot * 1000 > 966L * la * lb) bad.Add($"{front[i].Id} and {front[j].Id} face one way"); // within 15°
                }
            });
        Assert.True(seen > 0, "gunfire draws the crew to the Office front, two or more posted there");
        Assert.True(bad.Count == 0, string.Join("; ", bad.Distinct().Take(5)));
    }

    [Fact]
    public void TheTenetsStillPlay()
    {
        var bad = new List<string>(); bool ring = false, escort = false, carDriver = false;
        foreach (var route in new[] { "route-a", "route-b", "route-g", "loud-fight" })
            Play(route, 0, "unset", g =>
            {
                // the escort grip: Vale held is a metre off the Enforcer, never laid apart
                if (g.Vale == ValeAt.WithEnforcer && !g.Enforcer.Inside && g.ValePos() is Pt vp) { escort = true; if (vp != g.Enforcer.Pos + new Pt(100, 0)) bad.Add($"{route} {Report.Clock(g.Tick)}: Vale {vp} off the Enforcer's grip"); }
                // a carry: the carried go where the carrier goes
                foreach (var op in g.Ops) if (op.CarriedBy.HasValue && op.Pos != g.OpOf(op.CarriedBy.Value).Pos) bad.Add($"{route} {Report.Clock(g.Tick)}: {op.Id} carried off its carrier");
                // Inez and the door guard's meeting ring: he stands at it, never moved off it by the law
                if (g.GuardPose() is var gp && gp.HasValue && gp.Value.phase == "at café" && g.PersonPos(g.Inez) is Pt ip)
                {
                    ring = true; int d = G.Dist(gp.Value.pos, ip);
                    if (g.BodyLayOf(g.GuardP.Id) != default || d < g.R.BodyMinCm) bad.Add($"{route} {Report.Clock(g.Tick)}: the guard at the café {d} cm from Inez, laid {g.BodyLayOf(g.GuardP.Id)}");
                }
                // the driver on duty at his car, and the cast standing at their places, are never moved by the law
                foreach (var p in new[] { g.Inez, g.Driver, g.GuardP })
                    if (g.BodyInfo(p.Id) is var bi && bi.HasValue && bi.Value.cls == 0 && bi.Value.at != bi.Value.raw) bad.Add($"{route} {Report.Clock(g.Tick)}: tenet {p.Id} laid off its place");
                if (g.DriverPos() is Pt dp && dp == g.S.Stand(g.S.CarDriver)) carDriver = true;
            });
        Assert.True(bad.Count == 0, string.Join("; ", bad.Distinct().Take(5)));
        Assert.True(ring && escort && carDriver, $"the ring {ring}, the escort {escort}, the driver at his car {carDriver} came up");
    }

    [Fact]
    public void AnOperativesWalkWaitsForHisBodyAndEndsWhereHeWasSent()
    {
        // every walk the cell finishes ends at its end or beside it (two radii and a step, when someone holds the end), never short of it
        var bad = new List<string>(); int ended = 0;
        foreach (var route in Routes)
        {
            var walks = new Dictionary<OpId, Path?>();
            Play(route, 1, "people=64", g =>
            {
                foreach (var op in g.Ops)
                {
                    walks.TryGetValue(op.Id, out var was);
                    if (was != null && op.Walk == null && !op.Inside && !op.InCar && !op.Down && !op.CarriedBy.HasValue)
                    { ended++; if (!G.Within(op.Pos, was.End, g.R.BodyMinCm + g.R.BodySpaceCm)) bad.Add($"{route} {op.Id} {Report.Clock(g.Tick)}: walk ended {G.Dist(op.Pos, was.End)} cm from its end"); }
                    walks[op.Id] = op.Walk;
                }
            });
        }
        Assert.True(ended > 10, $"{ended} walks ended");
        Assert.True(bad.Count == 0, string.Join("; ", bad.Take(5)));
    }

    [Fact]
    public void TheSameSeedAndOrdersLayTheSameBodies()
    {
        var a = Play("route-b", 3, "people=64,cars=8", _ => { }); var b = Play("route-b", 3, "people=64,cars=8", _ => { });
        Assert.Equal(a.Checkpoints, b.Checkpoints);
        var s = Route("route-b"); s.Start.Seed = 3; s.Start.Counts["people"] = 64; s.Start.Counts["cars"] = 8;
        Assert.Equal(Hash.Hex(a.Checkpoints[^1].hash), Hash.Hex(Engine.Play(Repo.Inputs, s.Start, s.Orders, s.Until).Checkpoints[^1].hash));
    }

    [Fact]
    public void WithTheLawOffNothingIsLaid()
    {
        var text = File.ReadAllText(IOPath.Combine(Root, Engine.RulebookFile));
        var off = text.Replace("\"bodies-on\": { \"v\": 1,", "\"bodies-on\": { \"v\": 0,");
        Assert.NotEqual(text, off);
        var i = Engine.FromTexts(off, File.ReadAllText(IOPath.Combine(Root, Engine.PeopleRulesFile)), File.ReadAllText(IOPath.Combine(Root, Engine.SiteFile)), File.ReadAllText(IOPath.Combine(Root, Engine.PeopleFile)),
            File.ReadAllText(IOPath.Combine(Root, Engine.ProvisionalFile)), File.ReadAllText(IOPath.Combine(Root, Engine.TakedownFile)), File.ReadAllText(IOPath.Combine(Root, Engine.RoundsFile)));
        Assert.False(i.Rules.BodiesOn);
        var g = Engine.New(i); g.RunTo(Game.T(330));
        Assert.Empty(g.BodiesNow());
        foreach (var w in g.Walkers) Assert.Equal(default, g.BodyLayOf(w.P.Id));
    }
}
