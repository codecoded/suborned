// Brief 098 step 1b (Sky 2026-11-07, on jams: "should be opposite - shot cars can get past, others block"): laws that hold for every seed,
// never a pinned moment.
//    AQueueBoxesTheCellsCarIn            a passing car standing on the getaway car's way out (queued, not shot) boxes it in: Go is refused car-boxed
//                                        naming it; the box ends when that car moves on
//    AWreckIsShovedAsideAsAMissionCarGoesBy  a shot car in a mission car's way is pushed aside, physically (never driven through), and the car goes on
//    AWreckBoxesNobodyInAndIsShovedAtTheGo   a shot car standing on the way out boxes nothing: Go is taken, and the wreck is pushed off the way
using System;
using System.Collections.Generic;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class TrafficJamTests
{
    static Start Cars(int seed, int cars) { var s = new Start { Seed = seed }; s.Counts[Population.Cars] = cars; return s; }
    static Game New(int seed, int cars) => Engine.New(Repo.Inputs, Cars(seed, cars));
    static Pt[] Box(TrafficView v) => CarGeo.Rect(v.At, v.Heading, v.Length / 2, v.Width / 2);
    static TrafficView View(Game g, string id) => g.TrafficNow().Single(v => v.Id == id);
    static Pt[] Getaway(Game g) => CarGeo.Rect(g.GetawayCarPos(), g.GetawayHeading(), g.R.CarLenCm / 2, g.R.CarWCm / 2);
    static void Stand(Game g, OpId who, Pt at) { var op = g.OpOf(who); op.Walk = null; op.WalkMm = 0; op.Pos = at; }
    /// <summary>The Tech, placed beside the getaway car at its first stand, boards it, so a Go can be asked.</summary>
    static void Board(Game g) { Stand(g, OpId.Tech, g.Walk.NearestFree(g.GetawayCarPos())!.Value); var a = g.Issue(new Order { Tick = g.Tick, Op = "tech", Do = "board", Target = "car" }); Assert.True(a.ok, a.code); for (int k = 0; k < Game.T(90) && !g.AnyInCar && !g.Over; k++) g.Step(); Assert.True(g.AnyInCar, "the Tech is aboard"); }
    /// <summary>Shoot passing car `id` to disabled (the Enforcer from a spot 5..9 m off it): a wreck where it stopped.</summary>
    static void Wreck(Game g, string id)
    {
        var at = View(g, id).At; var spot = g.S.Grid.Nearest(at, q => G.D2(q, at) >= 500L * 500 && G.D2(q, at) <= 900L * 900 && g.S.Sight(q, at, g.R.Eye), 1500);
        Assert.True(spot.HasValue, "a spot to shoot it from"); Stand(g, OpId.Enforcer, spot!.Value);
        g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "draw" }); for (int k = 0; k < Game.T(2); k++) { g.Step(); Stand(g, OpId.Enforcer, spot.Value); }
        g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "fire", Target = id });
        for (int k = 0; k < Game.T(30) && !g.CarDisabled(id) && !g.Over; k++) { g.Step(); Stand(g, OpId.Enforcer, spot.Value); }
        Assert.True(g.CarDisabled(id), "shot to disabled"); g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "holster" });
    }

    [Theory] [InlineData(1)] [InlineData(2)] [InlineData(4)]
    public void AQueueBoxesTheCellsCarIn(int seed)
    {
        var g = New(seed, 16); Board(g); var way = g.WayOut(g.CurStand.Id); long half = g.S.GroundOf(g.R).Half;
        // a car of the street crossing the cell's way out: someone steps out right in front of it, so it stops at once with its body across the way
        TrafficCar? c = null; Pt spot = default;
        for (int k = 0; k < Game.T(400) && c == null && !g.Over; k++)
        {
            g.Step();
            foreach (var o in g.Traffic)
            {
                if (o.Out != null || o.V < 100 || o.Mode != TrafficMode.Drive || !View(g, o.Id).InZone || !CarGeo.OnWay(Box(View(g, o.Id)), way, half)) continue;
                spot = o.Route.Path.At(Math.Min(o.Route.Path.Length, o.Front + 900)); if (g.Walk.Free(spot)) { c = o; break; }
            }
        }
        Assert.True(c != null, $"seed {seed}: a car of the street crossed the cell's way out");
        string boxer = c!.Id; bool boxed = false;
        for (int k = 0; k < Game.T(12) && !boxed && !g.Over; k++) { Stand(g, OpId.Scout, spot); g.Step(); boxed = g.GetawayBoxers().Contains(boxer); }
        Assert.True(boxed, $"{boxer} stands across the way out and boxes the car in ({g.TrafficWhy(boxer)}; at {View(g, boxer).At}, the way {string.Join(" ", way)})");
        var v = View(g, boxer); Assert.True(v.Standing && !v.Shot, "an ordinary car of the street, standing (waiting), not a wreck");
        Assert.Equal("car:getaway", v.Boxes); // the host is told whom it boxes
        var q = g.GoQuery(); Assert.Equal("car-boxed", q.code); Assert.Contains(boxer, q.ids.Split(','));
        var go = g.Issue(new Order { Tick = g.Tick, Op = "cell", Do = "go" }); Assert.False(go.ok); Assert.Equal("car-boxed", go.code); // the Go itself is refused: a jam traps the car
        Stand(g, OpId.Scout, g.Walk.NearestFree(g.GetawayCarPos())!.Value); // the one in the road steps off it
        bool cleared = false;
        for (int k = 0; k < Game.T(60) && !cleared && !g.Over; k++) { g.Step(); cleared = !g.GetawayBoxers().Contains(boxer); }
        Assert.True(cleared, $"the way clears and {boxer} moves on: it no longer boxes the car in ({g.TrafficWhy(boxer)})");
    }

    [Theory] [InlineData(2)] [InlineData(5)]
    public void AWreckIsShovedAsideAsAMissionCarGoesBy(int seed)
    {
        // a probe game (the same seed and orders) learns the getaway car's Drive; the real game puts the wreck on it once the leg is under way
        string victim = "";
        Game Setup() { var x = New(seed, 8); Board(x); victim = x.Traffic.Where(c => c.Out == null && View(x, c.Id).InZone).OrderBy(c => c.Id, StringComparer.Ordinal).First().Id; Wreck(x, victim); return x; }
        var probe = Setup(); int t0 = probe.Tick; var to = probe.S.Stands.First(s => s != probe.CurStand && probe.DriveTicks(s.Id) > Game.T(8)).Id;
        Assert.True(probe.Issue(new Order { Tick = t0, Op = "tech", Do = "drive-car", Target = to }).ok);
        var path = new List<(Pt at, Pt h)>(); for (int k = 0; k < Game.T(60) && probe.GetawayDest != null && !probe.Over; k++) { probe.Step(); path.Add((probe.GetawayCarPos(), probe.GetawayHeading())); }
        Assert.True(path.Count > Game.T(6), "the probe's Drive runs a while");
        var g = Setup(); Assert.Equal(t0, g.Tick);
        var drive = g.Issue(new Order { Tick = g.Tick, Op = "tech", Do = "drive-car", Target = to }); Assert.True(drive.ok, drive.code);
        for (int k = 0; k < 5; k++) g.Step();
        var ahead = path[g.Tick - t0 + Game.T(2)]; var w = g.TrafficCarById(victim)!; w.Moved = true; w.MovedAt = ahead.at; w.MovedH = ahead.h; // the wreck stands 2 s on along the leg (a test's placing, as Stand places an agent)
        bool shoved = false, there = false;
        for (int k = 0; k < Game.T(90) && !g.Over && !there; k++)
        {
            g.Step(); var v = View(g, victim);
            Assert.False(CarGeo.PolysMeet(Getaway(g), CarGeo.Rect(v.At, v.Heading, v.Length / 2 - 5, v.Width / 2 - 5)), $"the getaway car never drives through the wreck (at {g.Tick})");
            if (v.ShovedBy == "car:getaway") shoved = true; there = g.GetawayDest == null && g.CurStand.Id == to;
        }
        Assert.True(shoved, "the wreck in its way is pushed aside"); Assert.True(there, $"and the getaway car gets past it to {to} ({g.MissionHoldNow("car:getaway")})");
        var wv = View(g, victim); Assert.True(wv.Shot && wv.Shoved > 0 && wv.ShovedAt > 0, "the host is told: shot, pushed, how far and when");
        Assert.Contains(g.Log, l => l.Code == "traffic.shoved");
    }

    [Theory] [InlineData(3)] [InlineData(6)]
    public void AWreckBoxesNobodyInAndIsShovedAtTheGo(int seed)
    {
        var g = New(seed, 8); Board(g);
        var victim = g.Traffic.Where(c => c.Out == null && View(g, c.Id).InZone).OrderBy(c => c.Id, StringComparer.Ordinal).First().Id; Wreck(g, victim);
        var way = g.WayOut(g.CurStand.Id); Assert.True(way.Length >= 2);
        var w = g.TrafficCarById(victim)!; var mid = way[way.Length / 2]; var d = new Pt(way[way.Length / 2 + (way.Length / 2 + 1 < way.Length ? 1 : -1)].X - mid.X, way[way.Length / 2 + (way.Length / 2 + 1 < way.Length ? 1 : -1)].Y - mid.Y);
        w.Moved = true; w.MovedAt = mid; w.MovedH = d.X == 0 && d.Y == 0 ? new Pt(0, 100) : d; g.Step(); // a wreck across the way out (a test's placing)
        Assert.DoesNotContain(victim, g.GetawayBoxers()); // a wreck boxes nobody in
        var go = g.Issue(new Order { Tick = g.Tick, Op = "cell", Do = "go" }); Assert.True(go.ok, go.code); // and the Go is taken
        var v = View(g, victim); Assert.Equal("car:getaway", v.ShovedBy); Assert.True(v.Shoved > 0, "pushed off the way as the car leaves");
        Assert.True(!CarGeo.OnWay(Box(v), way, g.S.GroundOf(g.R).Half) || v.Shoved >= Repo.Inputs.Population!.Traffic.ShoveMaxCm - 5, "off the way, unless pushed as far as a wreck goes");
    }
}
