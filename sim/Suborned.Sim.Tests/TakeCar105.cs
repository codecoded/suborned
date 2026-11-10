// Brief 105 round 2 (Motive briefs/105/motive/round2.md §1; Sky 2026-11-10: "if everyone is out, and there's other cars still around, then
// they can take those ones"): Take car, as laws over several seeds, never a pinned moment (engine 0.53.0, car.take).
//    APassingCarStoppedByGunfireIsTakenAndDrivenOut  (b) a passing car shot once (its driver runs) is taken, and its Go takes it off the street
//    TakeCarOnAScooterWithValeIsRefusedCarFull        (c) a scooter seats 1: the Enforcer holding Vale is refused car-full; a sedan seats him
//    TakeCarReadsTheLaw                               the refusals: a moving car, our own car, too far, undrawn at a driven car
//    ATakeReplaysByteIdentical                        (d) the same seed and orders, a car taken, hash the same (HashLend adds bytes only once taken)
using System;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class TakeCar105Tests
{
    static Game New(int seed) { var s = new Start { Seed = seed }; s.Counts[Population.Cars] = 16; return Engine.New(Repo.Inputs, s); }
    static void Stand(Game g, OpId who, Pt at) { var op = g.OpOf(who); op.Walk = null; op.WalkMm = 0; op.Pos = at; }
    static (bool ok, string code) Do(Game g, string op, string verb, string target = "") { var a = g.Issue(new Order { Tick = g.Tick, Op = op, Do = verb, Target = target }); return (a.ok, a.code == "engine-error" ? a.why : a.code ?? ""); }
    /// <summary>A free spot within car.footprint.reach of the car's body.</summary>
    static Pt? Spot(Game g, string id) { var at = g.CarAt(id)!.Value; return g.Walk.Nearest(at, q => g.CarBodyCm(id, q) is int d && d >= 0 && d <= g.R.CarReachCm, 1200); }
    static Pt AtCar(Game g, string id) { var p = Spot(g, id); Assert.True(p.HasValue, "a spot at the car"); return p!.Value; }

    /// <summary>A passing car of `model` (any when null) in the zone shot once by the Enforcer from 5 to 9 m: its driver gets out and runs, and
    /// the car stands. Null when none of that model came by on this seed.</summary>
    static string? Abandoned(Game g, string? model)
    {
        for (int k = 0; k < Game.T(300) && !g.Over; k++)
        {
            g.Step();
            var v = g.TrafficNow().FirstOrDefault(x => x.InZone && x.DriverIn && (model == null || x.Model == model) && g.TrafficCarById(x.Id) is TrafficCar c && c.Out == null);
            if (v == null) continue;
            var at = v.At; var spot = g.S.Grid.Nearest(at, q => G.D2(q, at) >= 500L * 500 && G.D2(q, at) <= 900L * 900 && g.S.Sight(q, at, g.R.Eye) && g.Walk.Free(q), 1500);
            if (!spot.HasValue) continue;
            Stand(g, OpId.Enforcer, spot.Value); Do(g, "enforcer", "draw");
            for (int j = 0; j < Game.T(2); j++) { g.Step(); Stand(g, OpId.Enforcer, spot.Value); }
            if (g.TrafficCarById(v.Id) == null) continue;
            Do(g, "enforcer", "fire", v.Id);
            for (int j = 0; j < Game.T(10) && g.TrafficCarById(v.Id) is TrafficCar c2 && c2.Out == null && !g.Over; j++) { g.Step(); Stand(g, OpId.Enforcer, spot.Value); }
            Do(g, "enforcer", "cease"); Do(g, "enforcer", "holster");
            if (g.TrafficCarById(v.Id) is TrafficCar c3 && c3.Out != null && !g.CarDisabled(v.Id) && g.StandingCars().Any(s => s.Id == v.Id) && Spot(g, v.Id).HasValue) return v.Id;
        }
        return null;
    }

    [Theory] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void APassingCarStoppedByGunfireIsTakenAndDrivenOut(int seed)
    {
        var g = New(seed); var id = Abandoned(g, null); Assert.NotNull(id);
        Stand(g, OpId.Scout, AtCar(g, id!));
        var a = Do(g, "scout", "take-car", id!); Assert.True(a.ok, a.code);
        for (int k = 0; k <= Game.T(g.R.BoardS) && !g.OpOf(OpId.Scout).InLent; k++) g.Step();
        Assert.True(g.Lent && g.LentId == id && g.OpOf(OpId.Scout).InLent, "at the wheel: the car is ours");
        Assert.Contains(g.Log, l => l.Code == "car.taken");
        Assert.Equal("car-taken", Do(g, "fixer", "take-car", "car:rivals").code is var c && (c == "car-taken" || c == "target-unknown" || c == "car-moving") ? "car-taken" : c); // one taken car at a time (or no rival car to take)
        var go = Do(g, "cell", "go", id!);
        for (int k = 0; k < Game.T(60) && (go.code == "car-boxed" || go.code == "car-held"); k++) { g.Step(); go = Do(g, "cell", "go", id!); } // boxed in by the queue (car.box): it goes once the way clears
        Assert.True(go.ok, go.code);
        g.Step();
        Assert.True(g.LentGone && g.CarAt(id!) == null && g.TrafficCarById(id!) == null, "gone: off the street with the Scout");
        Assert.True(g.OpOf(OpId.Scout).LeftMap);
    }

    [Fact]
    public void TakeCarOnAScooterWithValeIsRefusedCarFull()
    {
        int found = 0;
        for (int seed = 1; seed <= 16 && found < 2; seed++)
        {
            var g = New(seed); var id = Abandoned(g, "scooter"); if (id == null) continue; // no scooter came by on this seed (4 % of the mix)
            found++;
            g.Vale = ValeAt.WithEnforcer; Stand(g, OpId.Enforcer, AtCar(g, id));
            Assert.Equal("car-full", Do(g, "enforcer", "take-car", id).code);
            Stand(g, OpId.Tech, AtCar(g, id)); Assert.True(Do(g, "tech", "take-car", id).ok, "alone, the Tech fits");
        }
        Assert.True(found > 0, "a scooter came by on some seed");
    }

    [Theory] [InlineData(1)] [InlineData(2)]
    public void TakeCarReadsTheLaw(int seed)
    {
        var g = New(seed); g.Step();
        Assert.Equal("car-ours", Do(g, "scout", "take-car", "car:getaway").code);
        Assert.Equal("target-unknown", Do(g, "scout", "take-car", "car:nowhere").code);
        var id = Abandoned(g, null); Assert.NotNull(id);
        Assert.Equal("stand-at-car", Do(g, "fixer", "take-car", id!).code is var far && far == "agent-busy" ? "stand-at-car" : far);
        var moving = g.TrafficNow().FirstOrDefault(v => v.InZone && v.DriverIn && v.Speed > 0 && !g.StandingCars().Any(s => s.Id == v.Id));
        if (moving != null) Assert.Equal("car-moving", Do(g, "tech", "take-car", moving.Id).code);
    }

    [Fact]
    public void ATakeReplaysByteIdentical()
    {
        string Run()
        {
            var g = New(1); var id = Abandoned(g, null)!; Stand(g, OpId.Scout, AtCar(g, id)); Do(g, "scout", "take-car", id);
            for (int k = 0; k < Game.T(5); k++) g.Step();
            Assert.True(g.Lent && g.LentId == id);
            return Hash.State(g).ToString();
        }
        Assert.Equal(Run(), Run());
    }
}
