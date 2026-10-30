// Engine 0.33.0, Motive's S1-GATE-C037 (briefs/059/motive/s1-rulings.json checks.keel; M059-S1.2): every committed route script (c037's,
// contracts/missions/handover/routes/*.json) played with the gate on (fight-always 1, the rulebook) and off (fight-always 0) on this engine gives
// the same outcome, end tick and final checkpoint hash, except a script whose gate-off run left with Vale before the fight's blocker was out
// (a Go the gate refuses: the leak it closes); a run with a rider out after the spring may move its hash, not its end (the late spring, M059-S1.3,
// rides the same switch). Motive's probe on 0.31.0: 41 of 41 the same. A test, not a scenario: it reads the repo's routes. Engine 0.34.0: a run
// whose final fight has a rival advance (M067-SQ2) is held only to the same run up to the fight's start.
using System;
using System.Collections.Generic;
using IOPath = System.IO.Path; using File = System.IO.File; using Directory = System.IO.Directory;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class GateC037Tests
{
    static readonly string Root = Engine.FindRoot(AppContext.BaseDirectory);
    public static IEnumerable<object[]> Routes => Directory.GetFiles(IOPath.Combine(Root, "contracts/missions/handover/routes"), "*.json")
        .Where(f => IOPath.GetFileName(f) != "routes.json").OrderBy(f => f, StringComparer.Ordinal).Select(f => new object[] { IOPath.GetFileNameWithoutExtension(f) });

    static Game Play(Engine.Inputs i, string id)
    {
        var s = Script.Parse(File.ReadAllText(IOPath.Combine(Root, "contracts/missions/handover/routes", id + ".json")));
        return Engine.Play(i, s.Start, s.Orders, s.Until);
    }
    static int Tick(Game g, string code) { foreach (var l in g.Log) if (l.Code == code) return l.Tick; return -1; }

    [Theory, MemberData(nameof(Routes))]
    public void TheGateMovesOnlyALeak(string id)
    {
        var on = Repo.Inputs; Assert.True(on.Rules.FightAlways, "the rulebook's fight-always is 1");
        var off = Scenarios.With059(on, "\"fight-always\": \\{ \"v\": 1,", "\"fight-always\": { \"v\": 0,"); Assert.False(off.Rules.FightAlways);
        var a = Play(on, id); var b = Play(off, id);
        if (id == "route-b-thin" || id == "fight-safe-unguarded") { Assert.Equal(a.FightStart, b.FightStart); return; } // brief 080: on rolled shots and the close's verdict these two part inside the fight (gate off is a retired rule); the gate still moves nothing before the fight starts
        bool sameEnd = a.Result == b.Result && a.Tick == b.Tick && a.FailCode == b.FailCode;
        bool sameHash = sameEnd && a.Checkpoints.Count == b.Checkpoints.Count && (a.Checkpoints.Count == 0 || a.Checkpoints[a.Checkpoints.Count - 1].hash == b.Checkpoints[b.Checkpoints.Count - 1].hash);
        if (sameHash) return;
        // engine 0.34.0 (Motive M067-SQ2, ai.noStall): a rival with nobody in reach advances on an operative, gate or no gate, and with the gate
        // off (0.32.0's fight: no blocker named until a rider is out, a fight that may be left unfought) the advance meets another fight, so the
        // two runs part inside the final fight (route-g-fast: on, the blocker downed and a win at 3:44.2; off, no blocker and the Enforcer downed
        // with Vale at 3:11.1). Those runs say only that the gate moves nothing before the fight: every checkpoint before it starts is the same
        bool advanced(Game g) => g.Log.Exists(l => l.Code == "ai.advance");
        if (advanced(a) || advanced(b))
        {
            int f0 = Math.Min(a.FightStart, b.FightStart);
            Assert.True(f0 < int.MaxValue / 4, $"{id}: an advance with no final fight");
            Assert.Equal(a.Checkpoints.Where(c => c.tick < f0), b.Checkpoints.Where(c => c.tick < f0));
            return;
        }
        // the same end with another hash: fight-always 1 also carries the late spring (M059-S1.3: a member who steps out after the spring fires
        // weapons.aimNew after; the blocker waits for the first on foot), which moves a run's state only where a rider stepped out after the spring
        int spring = Tick(a, "fight.ambush");
        // engine 0.35.2 (Motive rulings-0.35.1 §2): with the gate on the spring names a blocker who holds the car; a cell that never draws
        // beside the held car is no guard there, so the rivals walk to it and take Vale (fight-safe-cell-down, the never-draws script). With
        // the gate off (0.32.0's fight) no blocker is named, the car is never held, and the run idles to the close. That is the gate's hold,
        // not a leak: every checkpoint before the spring is the same
        if (a.FailCode == "rivals-reach-vale" && a.Log.Exists(l => l.Code == "rivals.for-car") && spring >= 0 && b.Result == Outcome.None && Tick(b, "fight.blocker") < 0)
        {
            Assert.Equal(a.Checkpoints.Where(c => c.tick < spring), b.Checkpoints.Where(c => c.tick < spring));
            return;
        }
        if (sameEnd) { Assert.True(spring >= 0 && a.Log.Exists(l => l.Code == "rivals.joins" || l.Code == "rivals.late"), $"{id}: the same end, another final hash, and no rider stepped out into the fight"); return; } // the riders' step-out ticks follow the car (rivals.late); their aimNew lands after the spring
        // the gate's leak: with it off the car left with Vale while the fight was on and its blocker not yet out
        int bOut = Tick(b, "blocker.out"), go = b.CarLeftTick;
        bool leak = b.Result == Outcome.Success && b.FightBranch.Length > 0 && b.FightOverTick >= go && (bOut < 0 || go < bOut);
        Assert.True(leak, $"{id}: gate on {a.Result} {a.FailCode} at {a.Tick}, off {b.Result} {b.FailCode} at {b.Tick}; off left at {go} (blocker.out {bOut}): a difference that is not the gate's leak");
    }
}
