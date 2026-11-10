// Brief 105 round 2 (engine 0.53.0, car.take; Latch): the host's Take car locks are the engine's refusals. The run is r105's (briefs/105/latch/k3-shot.script.json,
// Keel's K3-TAKE cut before the shooting), played on in the engine to the getaway car shot out; every agent's Take car on every car standing is then asked of both:
// the ring's lock (HostTakeCar.TakeCarLock) and the engine's answer to the order (a refused order changes nothing). Distance is a walk on the ring, so where the
// engine says stand-at-car first only the codes after it are compared.
using System.IO;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;

public class TakeCar105
{
    static Game ShotOut(HandoverHost h)
    {
        var sc = Script.Parse(File.ReadAllText(System.IO.Path.Combine(Root.Path, "briefs/105/latch/k3-shot.script.json")));
        var g = Engine.New(h.PlayInputs, sc.Start);
        foreach (var o in sc.Orders.OrderBy(x => x.Tick)) { g.RunTo(o.Tick); if (g.Over) break; g.Issue(o); }
        for (int i = 0; i < 1200 && !g.Over && !g.Log.Exists(l => l.Code == "car.shot-out"); i++) g.Step();
        Assert.That(g.Log.Exists(l => l.Code == "car.shot-out") && !g.Over, $"the getaway car shot out ({g.Result} {g.FailCode})");
        return g;
    }

    [Test] public void TheRingsTakeCarLockIsTheEnginesRefusal()
    {
        var h = Host.Fresh(); var g = ShotOut(h); Host.Set(h, "Game", g);
        int asked = 0, compared = 0;
        foreach (var car in g.StandingCars().Select(c => c.Id).Where(id => id != "car:getaway").ToList())
            for (int k = 0; k < 4; k++)
            {
                var op = g.Ops[k]; var was = op.Pos; var at = g.CarAt(car); if (op.Down || op.InCar || !at.HasValue) continue;
                var door = g.Walk.Nearest(at.Value, q => g.CarBodyCm(car, q) is int d && d >= 0 && d <= g.R.CarReachCm, 800); if (door.HasValue) op.Pos = door.Value; // at its door: the codes after the reach are compared
                var lk = (Answer)Host.Call(h, "TakeCarLock", (OpId)k, car); asked++;
                if (lk.ok) { op.Pos = was; continue; } // the engine would take it: issuing it would change the run
                var a = g.Issue(new Order { Tick = g.Tick, Op = Op.Name((OpId)k), Do = "take-car", Target = car });
                Assert.That(a.ok, Is.False, $"{Op.Name((OpId)k)} take-car {car}: the host locks it {lk.code}, the engine took it");
                op.Pos = was; if (a.code == "stand-at-car") continue; compared++;
                Assert.That(lk.code, Is.EqualTo(a.code), $"{Op.Name((OpId)k)} take-car {car}: host {lk.code} ({lk.why}), engine {a.code} ({a.why})");
            }
        Assert.That(compared, Is.GreaterThan(0), "no Take car refusal compared with the engine");
        var getaway = (Answer)Host.Call(h, "BoardLock", OpId.Scout); Assert.That(getaway.code, Is.EqualTo("car-disabled"), "Board on the shot-out car");
        Assert.That(h.GoLock().code, Is.EqualTo("car-disabled"), "Go on the shot-out car");
        System.Console.WriteLine($"take-car locks: {asked} asked, {compared} compared with the engine at {Report.Clock(g.Tick)}");
    }
}
