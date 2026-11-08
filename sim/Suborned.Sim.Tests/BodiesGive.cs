// Brief 098, law L9's give and take (engine 0.47.0; Sky 2026-11-07, on 0.46.0's last resort: "Do all but the jumping back to spot should be better
// to dealt with"): the law's tests. Laws, none pinned to a moment: route scripts played on seeds under the dial's settings, every tick read by the
// law's independent check (BodyCheck), and each promise held whenever it comes up: nobody jumps; a person of the city in the way makes room and
// steps back; two people of the city face to face squeeze past, turned sideways, two squeeze widths apart; one still stuck changes plans; the
// tenets are never moved by any of these, and the cell and the armed only make room, for their own side (engine 0.48.0: Aster, brief 098, after
// a police officer stood 81.6 s behind a response robot); engine 0.49.0, an idle operative off the place the player put him makes room for anyone
// (BodiesIdle.cs).
//   dotnet test sim/Suborned.Sim.Tests -c Release --filter FullyQualifiedName~BodiesGiveTests
using System;
using System.Collections.Generic;
using System.Linq;
using IOPath = System.IO.Path; using File = System.IO.File;
using Suborned.Sim;
using Xunit;

public class BodiesGiveTests
{
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);
    static Script Route(string id) => Script.Parse(File.ReadAllText(IOPath.Combine(Root, "contracts/missions/handover/routes", id + ".json")));
    static readonly string[] Routes = { "route-a", "route-b", "route-g", "loud-fight", "careless-takedown-seen-crowd" };

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
    /// <summary>A person of the city the give and take may move: upright, laid soft and unarmed (not a tenet, not the cell, not the armed).</summary>
    static bool City(Game g, in BodyCheck.Upright u) => !u.Tenet && (u.Kind == "crowd" || u.Kind == "dial-person") && !g.BodyKeepsPlace(u.Id);
    /// <summary>Engine 0.49.0: a person of the city sat in the chair it took instead (a change of plans to another chair: seated, it is laid as one
    /// seated is, and the squeeze it came out of ends once the two stand apart): still the city's, not a tenet giving way.</summary>
    static bool SatInstead(Game g, in BodyCheck.Upright u) => (u.Kind == "crowd" || u.Kind == "dial-person") && g.BodyMove(u.Id) is BodyMoveRead m && m.Replanned && m.AltChair != null && g.BodyIsTenet(u.Id);
    /// <summary>The side an upright body is on, as the law tells them apart (engine 0.48.0, GameBodyGive.SideOf): 1 the cell; 2 security (the
    /// patrol, the city's and the dial's guards, officers, police and robots, the fight's police); 3 the rival crew; 0 none (the city unarmed, the cast).</summary>
    static int Side(Game g, in BodyCheck.Upright u) => u.Kind == "patrol" ? 2 : u.Tenet ? 0 : u.Kind switch // the patrol is security when it asks; a tenet, it never makes room itself
    {
        "cell" => 1, "officer" or "police" or "robot" or "dial-guard" => 2, "rival" => 3,
        "fight" => u.Id.StartsWith("rival-", StringComparison.Ordinal) ? 3 : 2,
        "crowd" => g.BodyKeepsPlace(u.Id) ? 2 : 0, // a staff guard of the city
        _ => 0,
    };
    /// <summary>Making room for `forId` is the law's: a person of the city for anyone, one armed for one of its own side (never an enemy).</summary>
    static bool RoomByLaw(Game g, List<BodyCheck.Upright> up, in BodyCheck.Upright u, string forId) => City(g, u) || (u.Kind == "cell" && g.BodyIdle(u.Id)) || (Side(g, u) != 0 && up.Find(x => x.Id == forId) is var a && (a.Id == null || Side(g, a) == Side(g, u))); // one who has just left the street: the room ends on the next tick (GiveStart)

    [Theory]
    [InlineData("unset")] [InlineData("people=64")] [InlineData("guards=64")] [InlineData("guards=64,people=64")] [InlineData("cars=8")]
    public void NobodyJumps(string counts)
    {
        long moves = 0; int jumps = 0; var first = new List<string>();
        foreach (var route in Routes) foreach (var seed in new[] { 1, 2 })
        {
            var mv = new BodyCheck.Moves();
            Play(route, seed, counts, g => mv.Look(g, BodyCheck.Of(g)));
            moves += mv.Looks; jumps += mv.Jumps;
            foreach (var j in mv.First) if (first.Count < 4) first.Add($"{route} #{seed} {Report.Clock(j.tick)}: {j.id} ({j.kind}) {j.cm} cm, its pace {j.pace} cm a tick");
        }
        Assert.True(moves > 100_000, $"{moves} tick-to-tick moves watched");
        Assert.True(jumps == 0, $"{counts}: {jumps} moves over a body's own pace: {string.Join("; ", first)}");
    }

    [Fact]
    public void ThePersonOfTheCityInTheWayMakesRoomAndStepsBack()
    {
        int orders = 0; var bad = new List<string>(); var since = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (route, counts) in new[] { ("careless-takedown-seen-crowd", "people=64"), ("route-a", "people=64"), ("loud-fight", "people=32") })
            Play(route, 1, counts, g =>
            {
                var up = BodyCheck.Of(g); var now = new HashSet<string>(StringComparer.Ordinal);
                foreach (var u in up)
                {
                    if (g.BodyMove(u.Id) is not BodyMoveRead m || m.RoomFor == null) continue;
                    now.Add(u.Id); if (!since.ContainsKey(u.Id)) { since[u.Id] = g.Tick; orders++; }
                    if (!RoomByLaw(g, up, u, m.RoomFor)) bad.Add($"{route} {Report.Clock(g.Tick)}: {u.Id} ({u.Kind}) made room for {m.RoomFor}, but it keeps its place for them");
                    if (g.Tick - since[u.Id] > Game.T(6) + 1) bad.Add($"{route} {Report.Clock(g.Tick)}: {u.Id} has stood aside for {m.RoomFor} over 6 s");
                    if (up.Exists(x => x.Id == m.RoomFor) && up.First(x => x.Id == m.RoomFor) is var a && G.D2(a.At, u.At) < (long)g.R.BodyMinCm * g.R.BodyMinCm && !BodyCheck.Squeezing(g, a, u, G.Dist(a.At, u.At))) bad.Add($"{route} {Report.Clock(g.Tick)}: {u.Id} made room for {m.RoomFor} inside two radii of it"); // closer than two radii (BodyCheck.Closer's measure; two radii apart is allowed: engine 0.47.1 met it exactly, route-a at 6:34.3)
                }
                foreach (var id in since.Keys.Where(k => !now.Contains(k)).ToList()) since.Remove(id); // stepped back (or left the street): the next ask is a new one
            });
        Assert.True(orders > 20, $"people stood aside {orders} times");
        Assert.True(bad.Count == 0, string.Join("; ", bad.Distinct().Take(5)));
    }

    [Fact]
    public void TwoPeopleOfTheCityFaceToFaceSqueezePastTurnedSideways()
    {
        int squeezes = 0, ticks = 0; var bad = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (route, counts) in new[] { ("careless-takedown-seen-crowd", "people=64"), ("route-a", "people=64"), ("route-b", "guards=64,people=64") })
            foreach (var seed in new[] { 1, 2 })
                Play(route, seed, counts, g =>
                {
                    var up = BodyCheck.Of(g);
                    foreach (var u in up)
                    {
                        var sq = g.BodySqueeze(u.Id); if (!sq.HasValue) continue;
                        var (with, facing) = sq.Value; ticks++;
                        if (seen.Add(string.CompareOrdinal(u.Id, with) < 0 ? $"{route}#{seed}:{u.Id}+{with}" : $"{route}#{seed}:{with}+{u.Id}")) squeezes++; // each pair once
                        if (!City(g, u) && !SatInstead(g, u)) bad.Add($"{route} {Report.Clock(g.Tick)}: {u.Id} ({u.Kind}) squeezes, but it keeps its place");
                        var back = g.BodySqueeze(with); if (!back.HasValue || back.Value.with != u.Id) bad.Add($"{route} {Report.Clock(g.Tick)}: {u.Id} squeezes past {with}, who does not squeeze past it");
                        if (up.Find(x => x.Id == with) is var o && o.Id != null && G.Dist(o.At, u.At) < 2 * g.R.BodySqueezeCm) bad.Add($"{route} {Report.Clock(g.Tick)}: {u.Id} and {with} {G.Dist(o.At, u.At)} cm apart, inside two squeeze widths");
                        if (facing == default || (g.FacingNow(u.Id) is Pt f && f != facing)) bad.Add($"{route} {Report.Clock(g.Tick)}: {u.Id} squeezing faces {g.FacingNow(u.Id)}, not sideways {facing}");
                    }
                });
        Assert.True(squeezes > 5, $"{squeezes} squeezes ({ticks} body-ticks)");
        Assert.True(bad.Count == 0, string.Join("; ", bad.Distinct().Take(5)));
    }

    [Fact]
    public void APersonOfTheCityStillStuckChangesPlans()
    {
        int replans = 0; var bad = new List<string>(); var by = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var (route, counts) in new[] { ("loud-fight", "guards=64,people=64"), ("route-b", "guards=64,people=64"), ("careless-takedown-seen-crowd", "people=64") })
            foreach (var seed in new[] { 1, 2, 3 })
            {
                var was = new HashSet<string>(StringComparer.Ordinal);
                var g = Play(route, seed, counts, h =>
                {
                    var now = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var u in BodyCheck.Of(h))
                    {
                        if (h.BodyMove(u.Id) is not BodyMoveRead m || !m.Replanned) continue; now.Add(u.Id);
                        if (!City(h, u) && !SatInstead(h, u)) bad.Add($"{route} #{seed} {Report.Clock(h.Tick)}: {u.Id} ({u.Kind}) changed plans, but it keeps its place");
                        if (m.AltChair != null && h.People.Exists(p => p.Id == u.Id)) bad.Add($"{route} #{seed}: one of the cast took another chair");
                    }
                    was = now;
                });
                replans += g.BodyReplans; foreach (var kv in g.BodyReplansBy) by[kv.Key] = by.GetValueOrDefault(kv.Key) + kv.Value;
            }
        Assert.True(replans > 0, "somebody changed plans");
        Assert.True(bad.Count == 0, string.Join("; ", bad.Distinct().Take(5)));
    }

    [Theory]
    [InlineData("people=64")] [InlineData("guards=64,people=64")]
    public void TheTenetsAreNeverMovedAndTheArmedMakeRoomOnlyForTheirOwnSide(string counts)
    {
        var bad = new List<string>(); int looked = 0, own = 0;
        foreach (var route in Routes)
            Play(route, 1, counts, g =>
            {
                var up = BodyCheck.Of(g);
                foreach (var u in up)
                {
                    if (City(g, u) || SatInstead(g, u)) continue; looked++;
                    if (g.BodyMove(u.Id) is not BodyMoveRead m) continue;
                    if (u.Tenet || m.SqueezeWith != null || m.Replanned) bad.Add($"{route} {Report.Clock(g.Tick)}: {u.Id} ({u.Kind}) {(m.SqueezeWith != null ? "squeezes past " + m.SqueezeWith : m.RoomFor != null ? "made room for " + m.RoomFor : "changed plans")}");
                    else if (m.RoomFor != null) { if (RoomByLaw(g, up, u, m.RoomFor)) own++; else bad.Add($"{route} {Report.Clock(g.Tick)}: {u.Id} ({u.Kind}) made room for {m.RoomFor}, not one of its own side"); }
                }
            });
        Assert.True(looked > 10_000, $"{looked} looks at tenets, the cell and the armed");
        Assert.True(!counts.Contains("guards") || own > 0, $"the armed made room for their own side {own} body-ticks (the dial's guards share posts)");
        Assert.True(bad.Count == 0, string.Join("; ", bad.Distinct().Take(5)));
    }
}
