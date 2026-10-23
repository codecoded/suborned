// The fight range's cars, bystanders, layouts and lines in plain words (Keel, 2026-10-21; GameRangeKinds.cs, GameRange.cs RangeLines).
// Range scenarios (Scenarios.Range): each passes, replays from its start and orders, and gives the same checksums twice.
//    RK-CAR        a car: disabled at car.target.disabledAt, burning at 0, exploding car.target.burn later; officers in its ring with a
//                  line take the blast; its cover stands only while it does; a new car clears the wreck; nothing traced
//    RK-CAR-OUT    a car with police in it: disabled, the officer gets out on the far side into the car's own cover and fires on the cell;
//                  the blast catches him there
//    RK-CAR-LAB    the lab on the lane: the car's health, disable point and blast ×healthScale; a rival gets out and fights
//    RK-SLOTS      the spawn order's shapes on the dense layout: car slots by "car" and "car:<k>", held slots, unknown occupants and
//                  placements refused; the slot list the host reads; a script with `with` replays and verifies its record
//    RK-BY-FLEE    bystanders as the street's crowd: a drawn weapon in witness range startles them, they run for the far end, are safe off
//                  the floor, witness the agent they saw fire, and walk back crowd.quiet after the last shot
//    RK-BY-SHOT    anyone can be shot: a bystander only when forced (civilian-target otherwise); a hit sends them running; down they lie;
//                  stances and hostiles never pick them; nothing traced
//    RK-LANE       the lane layout takes every kind: people slots, car slots, a car with someone in it, bystanders; the lines include them
//    RK-LINES      the lines say the rules in play in plain words: follow, lock, the trigger, police answering every recent shooter, the
//                  pair's count (hits next shot, aim kept), fire on the move (walking, running) and the settle
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static Answer SpawnCar(Run r, string to, int count, string with = "") => r.G.Issue(new Order { Tick = r.G.Tick, Do = "spawn", Target = "car", To = to, Count = count, With = with });
        static RangeCarView CarV(Run r, string id) => r.G.RangeCars().Find(c => c.Id == id) ?? throw new InvalidOperationException($"no range car {id}");
        static RangeBystanderView ByV(Run r, string id) => r.G.RangeBystanders().Find(c => c.Id == id) ?? throw new InvalidOperationException($"no bystander {id}");
        static bool Logged(Run r, string code) => r.G.Log.Exists(l => l.Code == code);
        static bool NothingTraced(Run r) => r.Is(r.G.Trail == 0 && r.G.Alert == 0 && r.G.Gunfire == 0 && r.G.Standing == r.G.StandingAtStart, $"nothing in the range is traced (Trail {r.G.Trail}, Alert {r.G.Alert}, gunfire {r.G.Gunfire})");

        static void RangeKindsSet()
        {
            AddRange("RK-CAR", "today", "open", "a car: disabled at car.target.disabledAt, burning at 0, exploding car.target.burn later; officers in the ring with a line take the blast; its cover stands only while it does; a new car clears the wreck; nothing traced", r =>
            {
                var g = r.G; var R = g.R;
                r.Is(!g.CoverNow().Exists(c => c.Of.StartsWith("car/slot-", StringComparison.Ordinal)), "no car yet: no car slot's cover stands");
                r.Is(SpawnCar(r, "car", 1).ok, "a car spawned");
                var car = CarV(r, "car:range-1");
                r.Eq((car.Slot, car.At, car.State, car.Health, car.Max), (1, g.S.At("spawn:car:1"), "ok", R.HealthCar, R.HealthCar), "on car slot 1, whole");
                r.Is(g.CoverNow().Exists(c => c.Of == "car/slot-1"), "its cover stands");
                // the Enforcer fires from the side, out of the near officers' reach (they come later)
                r.To(r.SendTo("enforcer", new Pt(300, 3800)));
                r.Fire("enforcer", "car:range-1");
                r.Is(r.Until(() => CarV(r, "car:range-1").State != "ok", Game.T(30)), "the car is disabled");
                car = CarV(r, "car:range-1");
                r.Is(car.State == "disabled" && car.Health <= R.CarDisabledAt && car.Health > 0, $"disabled at {car.Health} (car.target.disabledAt {R.CarDisabledAt})");
                r.Is(Logged(r, "car.disabled") && g.CoverNow().Exists(c => c.Of == "car/slot-1"), "logged car.disabled; still cover");
                r.Is(r.Until(() => CarV(r, "car:range-1").State == "burning", Game.T(30)), "it burns at 0");
                car = CarV(r, "car:range-1"); int burnAt = g.Tick;
                r.Eq(car.BlastIn, Game.T(R.CarBurnS), "it will explode car.target.burn later");
                r.Eq(car.BlastCm, R.CarBlastCm, "the blast ring the host draws");
                // two officers inside its ring with a line: near slots 1 and 2, about 2 m from it
                Spawned(r, "police", "near", 2);
                foreach (var id in new[] { "officer-1", "officer-2" }) { var p = H(r, id).Pos(g.Tick); r.Is(G.Within(p, car.At, R.CarBlastCm) && g.S.Sight(car.At, p, R.Eye), $"{id} stands in the ring with a line"); }
                r.Is(r.Until(() => CarV(r, "car:range-1").State == "wrecked", Game.T(R.CarBurnS) + 2), "it explodes");
                r.Eq(g.Log.First(l => l.Code == "car.exploded").Tick - burnAt, Game.T(R.CarBurnS), "car.target.burn after it caught fire");
                foreach (var id in new[] { "officer-1", "officer-2" }) { var h = H(r, id); r.Is(h.Gun.Health <= h.Gun.Max - R.CarBlastDamage, $"{id} takes the blast ({h.Gun.Health} of {h.Gun.Max})"); }
                r.Is(!g.CoverNow().Exists(c => c.Of == "car/slot-1"), "its cover is gone");
                r.Is(!r.Shots().Exists(s => s.target == "car:range-1" && !s.by.Equals("enforcer")), "no hostile ever fired on the car");
                var a = g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "fire", Target = "car:range-1" });
                r.Is(!a.ok && a.code == "no-target", $"a wreck is no target (got {a.code})");
                NothingTraced(r);
                r.Is(SpawnCar(r, "car:1", 1).ok, "a new car on the wreck's slot");
                r.Eq(string.Join(",", g.RangeCars().Select(c => c.Id + ":" + c.State)), "car:range-2:ok", "the wreck is cleared; the new car stands");
                r.Is(g.CoverNow().Exists(c => c.Of == "car/slot-1"), "and its cover is back");
            });

            AddRange("RK-CAR-OUT", "today", "open", "a car with police in it: disabled, the officer gets out on the far side into the car's own cover and fires on the cell; the blast catches him there", r =>
            {
                var g = r.G; var R = g.R;
                r.Is(SpawnCar(r, "car", 1, "police").ok, "a car with police in it");
                r.Is(CarV(r, "car:range-1").Inside && CarV(r, "car:range-1").With == "police", "the host reads someone inside");
                r.Eq(g.Hostiles.Count, 0, "inside, he is nobody's target yet");
                var a = g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "fire", Target = "officer-1" }); r.Is(!a.ok && a.code == "target-unknown", "no fire order on him while he is inside");
                r.Fire("enforcer", "car:range-1");
                r.Is(r.Until(() => Logged(r, "range.car-out"), Game.T(30)), "disabled, he gets out");
                var h = H(r, "officer-1"); var car = CarV(r, "car:range-1"); var cp = g.CoverNow().Find(c => c.Id == h.Gun.Cover);
                r.Is(cp != null && cp.Of == "car/slot-1" && h.Pos(g.Tick) == cp.At, $"into the car's own cover ({h.Gun.Cover})");
                r.Is(cp != null && cp.At.X > car.At.X && CoverRules.Protects(cp, g.RangeMark, g.Tick / Game.Hz, R.PointBlankCm), "on the far side, shielded from the cell");
                r.Eq((car.Occupant, car.Inside, h.Gun.St), ("officer-1", false, "hold"), "the host reads him out; he holds there");
                int outAt = g.Tick;
                r.Is(r.Until(() => r.Shots().Exists(s => s.by == "officer-1"), Game.T(10)), "he fires on the cell");
                r.Is(r.Shots().Where(s => s.by == "officer-1").All(s => s.target is "scout" or "fixer" or "tech" or "enforcer"), "at the cell only");
                r.Is(r.Until(() => CarV(r, "car:range-1").State == "wrecked" || h.Down, Game.T(30)), "the car burns and explodes (or he falls first)");
                if (CarV(r, "car:range-1").State == "wrecked") r.Is(g.Log.Exists(l => l.Code == "car.blast-hits" && l.Text.Contains("officer-1")), "the blast catches him behind it");
                NothingTraced(r);
            });

            AddRange("RK-CAR-LAB", "lab", "lane", "the lab on the lane: a car's health, disable point and blast ×healthScale; a rival gets out and fights", r =>
            {
                var g = r.G; var R = g.R; int k = g.HealthScale;
                r.Is(SpawnCar(r, "car", 1, "rivals").ok, "a car with a rival in it, on the lane");
                var car = CarV(r, "car:range-1");
                r.Eq((car.Max, car.DisabledAt), (R.HealthCar * k, R.CarDisabledAt * k), "health and the disable point ×healthScale");
                r.Do("cell", "panic");
                r.Fire("enforcer", "car:range-1");
                r.Is(r.Until(() => Logged(r, "range.car-out"), Game.T(60)), "disabled, the rival gets out");
                var h = H(r, "rival-1");
                r.Eq(h.Wave, 1, "a crew of its own spawn");
                r.Is(r.Until(() => h.Gun.St != "alerted" || h.Down, Game.T(5)), "it waits its aim, then fights");
                r.Is(r.Until(() => r.Shots().Exists(s => s.by == "rival-1") || h.Down, Game.T(20)), "it fires (or falls first)");
                r.Is(r.Until(() => CarV(r, "car:range-1").State == "wrecked", Game.T(90)), "the car explodes");
                var hits = g.Log.Where(l => l.Code == "car.blast-hits").ToList();
                r.Is(hits.Count == 0 || g.Log.Exists(l => l.Code.EndsWith(".hit.blast", StringComparison.Ordinal) || l.Code == "fight.hit" || l.Code == "fight.down"), "anyone it caught took the blast");
                NothingTraced(r);
            });

            AddRange("RK-SLOTS", "today", "dense", "the spawn order's shapes: car slots by car and car:<k>, held slots, unknown occupants and placements refused; the host's slot list; a script with `with` replays", r =>
            {
                var g = r.G;
                var slots = g.RangeSlotList();
                r.Eq(slots.Count, Game.RangePlacements.Length * Game.RangeSlots + g.RangeCarSlots, "every people slot and every car slot");
                r.Is(g.RangeCarSlots >= 1 && slots.Where(s => s.Placement == "car").All(s => s.DistanceCm > 0 && !s.Held), $"{g.RangeCarSlots} car slots, each with its distance, all free");
                r.Is(slots.Where(s => s.Placement != "car").All(s => g.S.Grid.Free(s.At)), "the people slots stand on free ground");
                r.Eq(string.Join(",", Game.RangeKinds), "police,rivals,response,bystanders,car", "the kinds the panel lists");
                var a = SpawnCar(r, "car", 1, "robots"); r.Is(!a.ok && a.code == "target-unknown", "an unknown occupant: target-unknown");
                a = r.G.Issue(new Order { Tick = g.Tick, Do = "spawn", Target = "police", To = "near", Count = 1, With = "police" }); r.Is(!a.ok && a.code == "target-unknown", "with on a person: target-unknown");
                a = SpawnCar(r, "near", 1); r.Is(!a.ok && a.code == "no-place", "a car on a people slot: no-place");
                a = SpawnCar(r, "car:9", 1); r.Is(!a.ok && a.code == "no-place", "a car slot that isn't: no-place");
                r.Is(SpawnCar(r, "car:2", 1, "bystander").ok, "a car with a bystander on car slot 2");
                a = SpawnCar(r, "car:2", 1); r.Is(!a.ok && a.code == "no-place", "car slot 2 held: no-place");
                r.Is(SpawnCar(r, "car", g.RangeCarSlots - 1).ok, "the rest of the car slots");
                a = SpawnCar(r, "car", 1); r.Is(!a.ok && a.code == "no-place", "every car slot held: no-place");
                r.Is(g.RangeSlotList().Where(s => s.Placement == "car").All(s => s.Held), "the host reads them held");
                Spawned(r, "bystanders", "near", 2);
                r.Is(g.RangeSlotList().Count(s => s.Placement == "near" && s.Held) == 2, "two near slots held by bystanders");
                a = r.G.Issue(new Order { Tick = g.Tick, Do = "spawn", Target = "bystanders", To = "roof", Count = 1 }); r.Is(!a.ok && a.code == "no-place", "bystanders on an unknown placement: no-place");
                r.Wait(Game.T(2));
                var inputs = r.Inputs!; g.Finish();
                var text = Sim.Script.Write(g, inputs.Hash);
                r.Is(text.Contains("\"with\":\"bystander\"") && text.Contains("\"target\":\"car\""), "the script writes the car spawn and who sits in it");
                var sc = Sim.Script.Parse(text);
                r.Eq(sc.Verify(Engine.Play(inputs, sc.Start, sc.Orders, sc.Until), inputs.Hash), "", "the replay matches its record");
            });

            AddRange("RK-BY-FLEE", "today", "open", "bystanders as the street's crowd: a drawn weapon in witness range startles them, they run for the far end, are safe off the floor, witness the agent they saw fire, and walk back crowd.quiet after the last shot", r =>
            {
                var g = r.G; var R = g.R;
                Spawned(r, "bystanders", "near", 2);
                var b1 = ByV(r, "bystander-1"); var home = b1.Pos;
                r.Eq((b1.State, b1.Health, b1.Max), ("standing", R.HealthCivilian, R.HealthCivilian), "standing at its slot, a civilian's health");
                r.Is(SpawnCar(r, "car", 1).ok, "a car to fire at");
                r.Wait(Game.T(2));
                r.Eq(ByV(r, "bystander-1").State, "standing", "the cell holstered: nobody is startled");
                r.Fire("scout", "car:range-1"); // the fire order draws the Scout's weapon in their sight
                r.Is(r.Until(() => ByV(r, "bystander-1").State == "startled", Game.T(3)), "the drawn weapon within witness range startles them");
                int st = g.Tick;
                r.Is(r.Until(() => ByV(r, "bystander-1").State == "fleeing", R.CrowdStartleTicks + 2), "then they run");
                r.Eq(g.Tick - st, R.CrowdStartleTicks, "after crowd.startle ticks");
                r.Is(r.Until(() => ByV(r, "bystander-1").Moving && g.RangeLines().Exists(l => l.Target == "bystander-1"), 5), "running, with a line on them");
                var ln = g.RangeLines().Find(l => l.Shooter == "scout" && l.Target == "bystander-1");
                r.Is(ln != null && ln.Bystander && ln.TargetRunning && ln.Text.Contains("target running") && ln.Text.Contains("bystander") && !ln.Text.Contains("Ctrl"), $"the line says so: {ln?.Text}");
                r.Is(r.Until(() => ByV(r, "bystander-1").Saw.Length > 0, Game.T(5)), "they see who fires");
                r.Eq(ByV(r, "bystander-1").Saw, "scout", "a witness of the Scout");
                r.Is(r.Until(() => ByV(r, "bystander-1").State == "safe", Game.T(30)), "safe at the far end");
                var safeAt = ByV(r, "bystander-1").Pos;
                r.Is(safeAt == g.S.Stand("range:entry") || safeAt == g.S.Stand("range:retreat"), $"at a refuge ({safeAt})");
                r.Is(G.D2(safeAt, g.OpOf(OpId.Scout).Pos) > G.D2(home, g.OpOf(OpId.Scout).Pos), "farther from the shooter than they stood");
                var a = g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "fire", Target = "bystander-1", Force = true }); r.Is(!a.ok && a.code == "no-target", "safe, off the floor: no target");
                r.Do("scout", "cease"); r.Do("scout", "holster"); // as the street: a weapon still out in their sight startles them again
                int last = g.LastShotTick;
                r.Is(r.Until(() => ByV(r, "bystander-1").State == "returning", Game.T(R.CrowdQuietS) + Game.T(2)), "after the quiet they walk back");
                r.Is(g.Tick - Math.Max(last, 0) >= Game.T(R.CrowdQuietS), $"crowd.quiet after the last shot ({(g.Tick - last) / 10.0} s)");
                r.Is(r.Until(() => ByV(r, "bystander-1").State == "standing", Game.T(60)), "and are back");
                r.Eq(ByV(r, "bystander-1").Pos, home, "at their slot");
                NothingTraced(r);
            });

            AddRange("RK-BY-SHOT", "today", "open", "anyone can be shot: a bystander only when forced; a hit sends them running; down they lie; stances and hostiles never pick them; nothing traced", r =>
            {
                var g = r.G;
                Spawned(r, "bystanders", "near", 1); Spawned(r, "police", "cover", 1);
                r.Do("enforcer", "draw"); g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "stance", Stance = "free" });
                r.Wait(Game.T(4));
                r.Is(!r.Shots().Exists(s => s.target == "bystander-1"), "at will, nobody fires on the bystander");
                var a = g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "fire", Target = "bystander-1" });
                r.Is(!a.ok && a.code == "civilian-target", $"not forced: civilian-target (got {a.code})");
                r.Fire("enforcer", "bystander-1", force: true);
                r.Is(r.Until(() => r.Shots().Exists(s => s.target == "bystander-1" && s.hit), Game.T(30)), "forced, he fires and hits");
                var b = ByV(r, "bystander-1");
                r.Is(b.Down || b.State == "fleeing", $"hit, they run at once, or fall ({b.State})");
                r.Is(r.Until(() => ByV(r, "bystander-1").Down, Game.T(40)), "until they are down");
                b = ByV(r, "bystander-1");
                r.Eq((b.State, b.Health, b.Moving), ("down", 0, false), "down where they fell");
                r.Is(Logged(r, "range.bystander-down"), "logged");
                r.Is(!r.Shots().Exists(s => s.target == "bystander-1" && s.by.StartsWith("officer-", StringComparison.Ordinal)), "the police never fired on them");
                NothingTraced(r);
            });

            AddRange("RK-LANE", "today", "lane", "the lane layout takes every kind: people slots, car slots, a car with someone in it, bystanders; the lines include them", r =>
            {
                var g = r.G;
                r.Eq(g.RangeSlotList().Count, Game.RangePlacements.Length * Game.RangeSlots + g.RangeCarSlots, "every slot, people and car");
                r.Is(g.RangeCarSlots == 3, $"three car slots ({g.RangeCarSlots})");
                Spawned(r, "police", "beyond", 1); Spawned(r, "bystanders", "eff", 2); r.Is(SpawnCar(r, "car", 1, "bystander").ok, "a car with a bystander in it");
                r.Do("enforcer", "draw"); r.Wait(Game.T(2));
                var lines = g.RangeLines();
                foreach (var t in new[] { "officer-1", "bystander-1", "bystander-2", "car:range-1" }) r.Is(lines.Exists(l => l.Shooter == "enforcer" && l.Target == t), $"a line from the Enforcer to {t}");
                r.Is(lines.Exists(l => l.Shooter == "officer-1" && l.Target == "enforcer"), "and from the officer to him");
                r.Is(lines.All(l => l.Text.Length > 0), "each in words");
                r.Eq((g.RangeHostiles().Count, g.RangeBystanders().Count, g.RangeCars().Count), (1, 2, 1), "the host reads each kind");
                r.Is(g.RangeCars()[0].Inside, "the bystander still in the car");
                r.Fire("enforcer", "car:range-1");
                r.Is(r.Until(() => Logged(r, "range.car-out"), Game.T(30)), "disabled, they get out");
                r.Is(r.Until(() => ByV(r, "bystander-3").State == "fleeing" || ByV(r, "bystander-3").State == "safe", 2), "and run at once");
                NothingTraced(r);
            });

            foreach (var preset in new[] { "lab", "today" })
            AddRange(preset == "lab" ? "RK-LINES" : "RK-LINES-TODAY", preset, "lane", "the lines say the rules in play in plain words: the trigger, follow, lock, police answering every recent shooter, the pair's count, fire on the move and the settle" + (preset == "today" ? " (today: an officer may fall before a line is read; each check then reads the next one up)" : ""), r =>
            {
                var g = r.G; F11Near(r, officers: 3); bool today = !g.Lab;
                FireLine? L(string by, string t) => g.RangeLines().Find(l => l.Shooter == by && l.Target == t);
                var o3 = L("enforcer", "officer-3");
                r.Is(o3 != null && !o3.Aimed && o3.Count == (today ? g.R.AccStart : g.Labr!.AccStart) && o3.Text.Contains("new target: aims first"), $"a target not yet fired on: the count starts fresh, it aims first ({o3?.Count}: {o3?.Text})");
                r.Is(Trig(r, "enforcer", "officer-1", true).ok, "the trigger pressed on officer-1");
                r.Wait(2);
                var e = L("enforcer", "officer-1"); var s = L("scout", "officer-1");
                r.Is(e != null && e.Mode == "trigger" && e.Text.Contains("trigger held"), $"the Enforcer's line: trigger held ({e?.Text})");
                r.Is(s != null && s.Mode == "follow" && s.Lead == "enforcer" && s.Text.Contains("following the Enforcer"), $"the Scout's: following the Enforcer ({s?.Text})");
                r.Is(r.Until(() => r.Shots().Count(x => x.by == "enforcer") >= 1 && r.Shots().Count(x => x.by == "scout") >= 1, Game.T(5)), "both fire on officer-1");
                var eg = r.Op("enforcer").Gun; e = L("enforcer", eg.Target);
                if (today && e == null) r.Line(false, $"today: the Enforcer's target ({eg.Target}) still up to read its line");
                else if (r.Is(e != null, $"the Enforcer's line on its target ({eg.Target})"))
                {
                    r.Eq(e!.Count, eg.Acc, "the line's count is the pair's");
                    r.Is(e.Acc > 0 && e.Count + e.ShotsToHit * e.Acc >= 1000 && e.Count + (e.ShotsToHit - 1) * e.Acc < 1000, $"hits in {e.ShotsToHit} shot(s) at {e.Acc} ‰ from {e.Count}");
                    r.Is(e.Text.Contains(e.ShotsToHit == 1 ? "hits next shot" : $"hits in {e.ShotsToHit} shots"), $"in words ({e.Text})");
                }
                string answered = "";
                bool both = r.Until(() => { foreach (var h in g.Hostiles) if (!h.Down && L(h.Id, "enforcer") is FireLine o1 && o1.Answers && L(h.Id, "scout") is FireLine o2 && o2.Answers) { answered = h.Id; return true; } return false; }, Game.T(3));
                if (today) r.Line(both, "today: an officer lives to answer both who fired on it (§12.2)");
                else r.Is(both, "the officer answers both who fired on it (§12.2)");
                if (both) r.Is(L(answered, "enforcer")?.Text.Contains("answering your fire") == true, $"in words ({L(answered, "enforcer")?.Text})");
                // the trigger moves to another officer; the line back keeps its aim
                var first = eg.Target; var next = g.Hostiles.First(h => !h.Down && h.Id != first).Id;
                r.Is(Trig(r, "enforcer", next, true).ok, $"the trigger moves to {next}");
                r.Wait(1);
                var back = first.Length > 0 ? L("enforcer", first) : null;
                if (today) r.Line(back != null, $"today: {first} still up to read the line back");
                r.Is(back == null || (back.AimKept && back.Text.Contains("aim kept from before")), $"the line back to {first} keeps the aim ({back?.Text})");
                r.Is(Trig(r, "enforcer", "", false).ok, "released");
                r.Is(r.Until(() => !eg.TrigTap, Game.T(3)), "the press's one shot is fired (or lost)");
                var up = L("enforcer", next);
                r.Is(up == null || up.Text.Contains("trigger up"), $"trigger up, aim kept ({up?.Text})");
                // follow on, in a run of its own from the same start (nobody engaged yet): a press locks the Scout
                var lk = PlayRangeBody(r.Inputs!, g.Start, rb =>
                {
                    F11Near(rb, officers: 3);
                    rb.Is(FollowSetting(rb, true).ok, "follow on");
                    rb.Is(Trig(rb, "enforcer", "officer-1", true).ok, "pressed on officer-1");
                    rb.Wait(1);
                    var sl = rb.G.RangeLines().Find(l => l.Shooter == "scout" && l.Target == "officer-1");
                    rb.Is(sl != null && sl.Mode == "lock" && sl.Lead == "enforcer" && sl.Text.Contains("locked on by the Enforcer"), $"the Scout's line: locked on by the Enforcer ({sl?.Text})");
                });
                foreach (var c in lk.Checks) r.Is(c.ok, "follow-on run: " + c.what);
                // fire on the move, in a run of its own (police out of reach at their cover, so the Fixer stays up): it runs, stops and steadies, walks
                var mv = PlayRangeBody(r.Inputs!, g.Start, rb =>
                {
                    var gg = rb.G; rb.Do("fixer", "draw"); rb.Wait(Game.T(2)); Spawned(rb, "police", "cover", 1);
                    FireLine? F() => gg.RangeLines().Find(l => l.Shooter == "fixer" && l.Target == "officer-1");
                    var fx = rb.Op("fixer"); var to = new Pt(fx.Pos.X, fx.Pos.Y - 400); // along the cell's line, clear ground
                    rb.Is(Move(rb, "fixer", to, run: true).ok, "the Fixer runs");
                    rb.Is(rb.Until(() => fx.Running && fx.Walk != null && fx.Pos != fx.StepFrom, 3), "running");
                    var fl = F(); rb.Is(fl != null && (fl.Gait == "run" || fl.Gait == "run-away") && fl.Text.Contains("running"), $"its line says running ({fl?.Text})");
                    rb.Is(rb.Until(() => fx.Walk == null, Game.T(5)), "the run ends");
                    rb.Is(rb.Until(() => fx.SettleUntil > gg.Tick, 2), "the settle starts");
                    fl = F(); rb.Is(fl != null && fl.Steadying && fl.Text.Contains("steadying after the run"), $"its line says steadying ({fl?.Text})");
                    rb.Is(rb.Until(() => fx.SettleUntil <= gg.Tick, gg.AimNewT + 1), "for aim-new");
                    rb.Is(F()?.Steadying == false, "then it is steady");
                    rb.Is(Move(rb, "fixer", new Pt(to.X, to.Y - 300)).ok, "then walks");
                    rb.Is(rb.Until(() => fx.Walk != null && fx.Pos != fx.StepFrom, 3), "walking");
                    fl = F(); rb.Is(fl != null && fl.Gait == "walk" && fl.Text.Contains("walking") && !fl.Text.Contains("steadying"), $"its line says walking ({fl?.Text})");
                });
                foreach (var c in mv.Checks) r.Is(c.ok, "on the move: " + c.what);
                r.Is(g.RangeLines().All(l => !l.Text.Contains(" eff") && !l.Text.Contains("acc") && !l.Text.Contains("‰")), "no design terms in the words");
            });
        }
    }
}
