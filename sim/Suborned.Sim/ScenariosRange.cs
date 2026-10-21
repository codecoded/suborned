// Brief 029, the fight range's scenarios (Keel): the range set, apart from the Handover's (Scenarios.All, the hashes of every Handover run,
// never gains one). Each runs on a range site (Engine.LoadRange: layout open or dense) under a brief 027 preset (today: lab off; lab: the
// lab.json switches). The suite runs each like any scenario: its checks pass, the same orders give the same checksums twice, and the
// replay from its start state and order log matches.
//    R01 spawns: each side onto its placement's slots, in order; held slots refuse a spawn that does not fit; unknown sides, placements and
//        Handover verbs are refused; a fire order names only a spawned hostile
//    R02 firing, today: police at the effective ring answer a drawn agent and the cell answers back; nothing is traced (Trail, Alert,
//        Standing unchanged) and the range never fails, even with the Enforcer down
//    R03 cover changes the hit chance, today: the line officer → Enforcer reads none in the open and full beside the box, the chance halves
//        (weapons.mod.cover), and the engine's next shot lands at exactly the chance the line read
//    R04 the same under the lab: one pip less beside cover, and the shot at the line's chance
//    R05 the response unit walks in from the far end to its slots and stops to engage the first agent inside its effective ring
//    R06 rivals: they wait aimNew, engage, and break for the far corner when more than half of their own spawn is down; range.clear
//    R07 the done-check under the lab: three police behind cover at 20 m, an agent fires, they answer (the kite) and fire back, an agent
//        moves into cover and the hit chance on it drops, all inside a minute
//    R08 today's police hold: behind cover at 20 m, beyond their 15 m reach, they never fire while the cell stays out of it
//    R09 the dense layout: every slot of every placement takes a hostile, each at its distance from the mark
//    R10 replay: the run written as a script (mission range, the spawn's count) parses, replays and verifies its record
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static List<Scenario>? range;
        /// <summary>The fight range's scenarios (brief 029). Run each on Engine.LoadRange(root, s.Layout).</summary>
        public static List<Scenario> Range { get { if (range == null) { range = new List<Scenario>(); RangeSet(); } return range; } }
        public static Scenario? FindRange(string id) { foreach (var s in Range) if (string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase)) return s; return null; }
        /// <summary>A range scenario on its own layout's inputs.</summary>
        public static Run RunRange(string root, Scenario s) => Run(Engine.LoadRange(root, s.Layout), s);

        /// <summary>The brief 027 presets as a run's start (briefs/027/presets/{today,lab}.json `start`): today = the lab off; lab = its switches.</summary>
        public static Start RangePreset(string name)
        {
            if (name == "today") return new Start();
            if (name != "lab") throw new FormatException($"preset '{name}' (today, lab)");
            var s = new Start { FightLabOn = true, LabHealthScale = 10, LabPips = true, LabStances = "three", LabThen = "free", LabClose = true, LabCharge = true, LabField = false, LabWarn = true, LabFireMoving = true, LabCoverSnap = true, LabKite = true };
            s.LabLoadout[0] = "sidearm"; s.LabLoadout[1] = "sidearm"; s.LabLoadout[2] = "sidearm"; s.LabLoadout[3] = "carbine";
            return s;
        }

        static Scenario AddRange(string id, string preset, string layout, string intent, Action<Run> body)
        {
            var s = new Scenario { Id = id, Proto = "brief 029", Intent = intent, Body = body, Start = RangePreset(preset), Layout = layout };
            range!.Add(s); return s;
        }

        static Answer Spawn(Run r, string side, string at, int count) => r.G.Issue(new Order { Tick = r.G.Tick, Do = "spawn", Target = side, To = at, Count = count });
        static bool Spawned(Run r, string side, string at, int count)
        {
            var a = Spawn(r, side, at, count);
            return r.Is(a.ok, $"spawn {count} {side} at {at} taken at {Report.Clock(r.G.Tick)}{(a.ok ? "" : ": " + a.why)}");
        }
        static void Hold(Run r, string op) { var a = r.G.Issue(new Order { Tick = r.G.Tick, Op = op, Do = "stance", Stance = "hold" }); r.Is(a.ok, $"{op} stance hold taken{(a.ok ? "" : ": " + a.why)}"); }
        static Hostile H(Run r, string id) => r.G.Hostiles.Find(h => h.Id == id) ?? throw new InvalidOperationException($"no hostile {id}");
        static FireLine? LineOf(Run r, string by, string t) => r.G.RangeLines().Find(l => l.Shooter == by && l.Target == t);
        /// <summary>The ‰ the log gives a shot (tick, by → t), from "… at n m (acc ‰): hit".</summary>
        static int LoggedAcc(Run r, int tick, string by, string t)
        {
            foreach (var x in r.G.Log)
            {
                if (x.Tick != tick || (x.Code != "shot.hit" && x.Code != "shot.miss") || !x.Text.StartsWith(by + " fires on " + t + " at ", StringComparison.Ordinal)) continue;
                int a = x.Text.IndexOf('(') + 1, b = x.Text.IndexOf(" ‰", StringComparison.Ordinal);
                return int.Parse(x.Text.Substring(a, b - a), CultureInfo.InvariantCulture);
            }
            return -1;
        }

        static void RangeSet()
        {
            AddRange("R01", "today", "open", "spawns: every side onto its slots in order; held slots, unknown sides, placements and verbs refused", r =>
            {
                var g = r.G;
                r.Eq(g.Hostiles.Count, 0, "the range starts with no hostile");
                r.Eq(g.FightOn, true, "the range is a fight from its first tick");
                Spawned(r, "police", "cover", 3);
                r.Eq(string.Join(",", g.Hostiles.Select(h => h.Id)), "officer-1,officer-2,officer-3", "three officers, numbered in order");
                for (int k = 1; k <= 3; k++)
                {
                    var h = H(r, $"officer-{k}"); var slot = g.RangeSlot("cover", k);
                    r.Eq(h.Pos(g.Tick), slot, $"officer-{k} stands on cover slot {k}");
                    var cp = g.S.Cover.Find(c => c.At == slot)!;
                    r.Eq(h.Gun.Cover, cp.Id, $"officer-{k} holds the cover point its slot stands on");
                    r.Eq(h.Gun.St, "hold", $"officer-{k} holds its stand");
                    r.Is(G.Within(slot, g.RangeMark, 2600) && !G.Within(slot, g.RangeMark, 1600), $"cover slot {k} is 16-26 m from the mark");
                }
                var a = Spawn(r, "police", "cover", 4);
                r.Is(!a.ok && a.code == "no-place", $"four more at cover: only three slots are free, refused no-place (got {a.code})");
                Spawned(r, "police", "cover", 3);
                r.Eq(g.Hostiles.Count, 6, "the three that fit");
                a = Spawn(r, "police", "cover", 1); r.Is(!a.ok && a.code == "no-place", "every cover slot held: refused no-place");
                Spawned(r, "rivals", "near", 2);
                r.Eq(H(r, "rival-1").Gun.St, "alerted", "a rival waits its aim first (as the ambush springs)");
                r.Eq(H(r, "rival-1").Wave, 3, "the third spawn is wave 3");
                Spawned(r, "response", "eff", 2);
                var resp = H(r, "response-1");
                r.Eq(resp.Pos(g.Tick), g.S.Stand("range:entry"), "the response unit starts at the far end");
                r.Eq(resp.Home, g.RangeSlot("eff", 1), "and holds the eff slot as its stand");
                r.Is(resp.Moving(g.Tick), "and walks to it");
                a = Spawn(r, "robots", "near", 1); r.Is(!a.ok && a.code == "target-unknown", "an unknown side: target-unknown");
                a = Spawn(r, "police", "roof", 1); r.Is(!a.ok && a.code == "no-place", "an unknown placement: no-place");
                a = g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "watch", Target = "guard" }); r.Is(!a.ok && a.code == "unknown-verb", "a Handover verb: unknown-verb");
                a = g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "fire", Target = "guard" }); r.Is(!a.ok && a.code == "target-unknown", "the parked guard is no target: target-unknown");
                a = g.Issue(new Order { Tick = g.Tick, Op = "cell", Do = "go" }); r.Is(!a.ok && a.code == "unknown-verb", "no car to go in: unknown-verb");
                r.Wait(1); // a run that ends at tick 0 replays to the script's default close: step once
                r.Is(g.RangeHostiles().Count == 10 && g.RangeHostiles().All(x => x.Side.Length > 0), "the host reads all ten, each with its side");
            });

            AddRange("R02", "today", "open", "firing under today's rules: police answer a drawn agent inside their reach; nothing is traced; the range never fails", r =>
            {
                var g = r.G;
                Spawned(r, "police", "eff", 2);
                r.Do("enforcer", "draw");
                r.Is(r.Until(() => r.Shots().Exists(s => s.by.StartsWith("officer-", StringComparison.Ordinal)), Game.T(10)), "an officer fires on the cell within 10 s");
                var first = r.Shots().Find(s => s.by.StartsWith("officer-", StringComparison.Ordinal));
                r.Eq(first.target, "enforcer", "at the drawn agent (class 3: the nearest with a weapon drawn)");
                r.Fire("enforcer", "officer-1");
                r.Is(r.Until(() => H(r, "officer-1").Down, Game.T(40)), "the Enforcer drops officer-1");
                r.Fire("enforcer", "officer-2");
                r.Until(() => H(r, "officer-2").Down || g.Enforcer.Down, Game.T(40));
                r.Is(r.Shots().Exists(s => s.by == "enforcer"), "the cell fired");
                r.Eq((g.Trail, g.Alert, g.Gunfire), (0, 0, 0), "nothing in the range is traced: Trail 0, Alert 0, no counted gunfire");
                r.Eq(g.Standing, g.StandingAtStart, "Standing does not move");
                r.Eq(g.Result, Outcome.None, "the range runs on");
                // more than the Enforcer can take: six police at point blank, the cell holstered
                Spawned(r, "police", "near", 6);
                r.Until(() => g.Enforcer.Down, Game.T(120));
                r.Is(g.Enforcer.Down, "the Enforcer goes down");
                r.Eq(g.Result, Outcome.None, "and the range does not fail: the Enforcer is any agent here");
            });

            AddRange("R03", "today", "open", "cover changes the hit chance (today): none in the open, full beside the box, the chance halved, the next shot at the line's chance", r => CoverChanges(r, false));
            AddRange("R04", "lab", "open", "cover changes the hit chance (lab): one pip less beside cover, the next shot at the line's chance", r => CoverChanges(r, true));

            AddRange("R05", "today", "open", "the response unit walks in from the far end and stops to engage the first agent in its reach", r =>
            {
                var g = r.G;
                Spawned(r, "response", "near", 2);
                r.Do("scout", "draw");
                var h = H(r, "response-1");
                r.Is(r.Until(() => h.Gun.St == "engage", Game.T(60)), "response-1 engages");
                var at = h.Pos(g.Tick); long near = g.Ops.Min(o => G.Isqrt(G.D2(o.Pos, at)));
                r.Is(near <= h.Gun.W!.EffCm && near > h.Gun.W.EffCm - 100, $"it stopped on its effective ring of the nearest agent ({near / 100.0:0.00} m), not at its slot");
                r.Is(!G.Within(at, h.Home, 300), "short of its slot");
                r.Is(!h.Moving(g.Tick), "and stands there to fire");
                r.Is(r.Until(() => r.Shots().Exists(s => s.by == "response-1"), Game.T(10)), "and fires");
            });

            AddRange("R06", "today", "open", "rivals wait their aim, engage, and break for the far corner when more than half their spawn is down", r =>
            {
                var g = r.G;
                Spawned(r, "rivals", "eff", 3);
                r.Do("cell", "panic");
                r.Is(r.Until(() => H(r, "rival-1").Gun.St != "alerted", Game.T(5)), "a rival leaves its wait");
                r.Is(r.Until(() => g.Hostiles.Count(h => h.Down) >= 2, Game.T(90)), "the cell drops two of the three");
                var up = g.Hostiles.Find(h => !h.Down);
                if (!r.Is(up != null, "one is still up")) return;
                r.Eq(up!.Gun.St, "break", "the third breaks");
                r.Is(r.Until(() => up.Gun.St == "gone", Game.T(60)), "and is gone");
                r.Is(G.Within(up.Pos(g.Tick), g.S.Stand(g.S.RivalsPark), 300) || !up.Present, "at the far corner (range:retreat)");
                g.Step();
                r.Is(g.Log.Exists(l => l.Code == "range.clear"), "the range notes it is clear");
                // a second crew is its own: it does not break on the first crew's dead
                Spawned(r, "rivals", "beyond", 3);
                r.Is(g.Hostiles.Where(h => h.Wave == 2).All(h => h.Gun.St == "alerted"), "the second crew comes in whole, not broken by the first's");
                r.Wait(Game.T(3));
                r.Is(g.Hostiles.Where(h => h.Wave == 2).All(h => h.Gun.St != "break"), "and fights: none of it breaks with none of it down");
            });

            AddRange("R07", "lab", "open", "the done-check (lab): three police behind cover at 20 m answer fire; an agent in cover is harder to hit; under a minute", r =>
            {
                var g = r.G; int t0 = g.Tick;
                Spawned(r, "police", "cover", 3);
                // the scout's sidearm reaches 16 m: the fire order walks him into his 15 m ring of officer-1 (fire.close), inside theirs
                r.Do("scout", "fire", "officer-1");
                r.Is(g.OpOf(OpId.Scout).CloseWalk, "the scout walks into range");
                r.Is(r.Until(() => r.Shots().Exists(s => s.by.StartsWith("officer-", StringComparison.Ordinal)), Game.T(30)), "the police fire back within 30 s");
                r.Is(r.Until(() => r.Shots().Exists(s => s.by == "scout"), Game.T(10)), "the scout fires");
                string shooter = ""; FireLine? open = null;
                r.Until(() => { foreach (var l in g.RangeLines()) if (l.Target == "scout" && l.InReach && l.Band != "long" && l.Cover == "none") /* in its effective ring: in reach at long range is police answering fire (§7.10) */ { shooter = l.Shooter; open = l; return true; } return false; }, Game.T(20));
                r.Is(open != null, $"an officer has the scout in reach in the open ({shooter})");
                if (open == null) return;
                var sp = g.Hostiles.Find(h => h.Id == shooter)!.Pos(g.Tick);
                var me = g.OpOf(OpId.Scout).Pos; int sec = g.Tick / Game.Hz;
                int reach = g.Hostiles.Find(x => x.Id == shooter)!.Gun.W!.EffCm;
                var cover = g.CoverNow().Where(c => c.Q >= g.R.CoverQPartial && CoverRules.Protects(c, sp, sec, g.R.PointBlankCm) && G.Within(c.At, me, 800) && G.Within(c.At, sp, reach) && g.S.Sight(c.At, sp, g.R.Eye)
                    && !g.Hostiles.Exists(x => x.Gun.Cover == c.Id)).OrderBy(c => G.D2(c.At, me)).ThenBy(c => c.Id, StringComparer.Ordinal).FirstOrDefault();
                r.Is(cover != null, "a cover point within 8 m of the scout shields him from that officer, inside its reach on a clear line");
                if (cover == null) return;
                Hold(r, "scout");
                int arrive = r.SendTo("scout", cover.At); r.To(arrive + 1);
                var inCover = LineOf(r, shooter, "scout");
                r.Is(inCover != null && inCover.Cover != "none" && inCover.CoverId == cover.Id, $"beside {cover.Id} the officer's line on the scout reads cover ({inCover?.Cover})");
                r.Is(inCover != null && inCover.InReach && inCover.Acc > 0 && inCover.Acc < open.Acc, $"still in its reach, at a lower chance: {open.Acc} → {inCover?.Acc} ‰ ({open.Pips} → {inCover?.Pips} pips, {open.DistanceCm / 100} → {inCover?.DistanceCm / 100} m)");
                r.Is(g.Tick - t0 <= Game.T(60), $"all inside a minute ({Report.Clock(g.Tick - t0)})");
            });

            AddRange("R08", "today", "open", "police in cover answer fire from beyond their max ring (Motive tuning.md §7.10, Sky's range report): three officers in cover at 20 m under the Enforcer's carbine hold at most 3 s, then close a bound to the shielding cover nearest their stop point, one bound per 3 s at most, and fire on the cell within 10 s of his first shot", r =>
            {
                var g = r.G;
                Spawned(r, "police", "cover", 3);
                r.Fire("enforcer", "officer-1"); // the carbine reaches 30 m: at 20 m it fires in its effective band, beyond the officers' 16 m max
                if (!r.Is(r.Until(() => r.Shots().Exists(s => s.by == "enforcer"), Game.T(10)), "the Enforcer fires at 20 m")) return;
                int first = r.Shots().First(s => s.by == "enforcer").tick;
                // §7.10 asks for fire on the Enforcer; closing to their stop point of him (13.5 m) brings the cell's others, drawn and nearer, into
                // reach first, and an officer not shot at takes the nearest drawn foe (Pulse §2.2 class 3): the check is fire on the cell
                r.Is(r.Until(() => r.Shots().Exists(s => s.by.StartsWith("officer-", StringComparison.Ordinal) && s.tick - first <= Game.T(10)), Game.T(10)), "the police fire on the cell within 10 s of his first shot");
                var closes = g.Log.Where(l => l.Code == "ai.kite" && l.Text.Contains("it closes")).ToList();
                r.Is(closes.Count > 0, "they close on him");
                if (closes.Count == 0) return;
                r.Is(closes.Min(l => l.Tick) - first <= g.R.KiteCoverHold + g.R.KiteReact + 1, $"holding cover no more than 3 s (first bound {closes.Min(l => l.Tick) - first} ticks after his first shot)");
                bool paced = true;
                foreach (var grp in closes.GroupBy(l => l.Text.Substring(0, l.Text.IndexOf(' '))))
                { var ts = grp.Select(l => l.Tick).OrderBy(t => t).ToList(); for (int k = 1; k < ts.Count; k++) paced &= ts[k] - ts[k - 1] >= g.R.KiteCoverHold; }
                r.Is(paced, "never more than one bound per 3 s each");
                // tuning.md §7.11.2: once the Enforcer is inside an officer's max ring with a line and has fired on it in the last 3 s, its next shot is at him
                var due = new Dictionary<string, int>(StringComparer.Ordinal); int checkedN = 0, wrong = 0; var seen = r.Shots().Count;
                for (int t = 0; t < Game.T(40) && !g.Over; t++)
                {
                    var e = g.OpOf(OpId.Enforcer);
                    foreach (var h in g.Hostiles)
                    {
                        if (h.Down || h.Gun.W == null || due.ContainsKey(h.Id)) continue;
                        var hp = h.Pos(g.Tick);
                        if (!e.Down && h.Gun.ShotBy == "enforcer" && h.Gun.ShotByTick >= 0 && g.Tick - h.Gun.ShotByTick <= Game.T(g.R.LineLapseS) && G.Within(hp, e.Pos, h.Gun.W.MaxCm) && g.S.Sight(hp, e.Pos, g.R.Eye)) due[h.Id] = g.Tick;
                    }
                    g.Step();
                    var shots = r.Shots();
                    for (int k = seen; k < shots.Count; k++) { var sh = shots[k]; if (due.TryGetValue(sh.by, out var at) && at >= 0) { checkedN++; if (sh.target != "enforcer") wrong++; due[sh.by] = -1; } }
                    seen = shots.Count;
                }
                r.Is(wrong == 0, $"an officer the Enforcer shoots from inside its max ring turns to him: its next shot is at him ({checkedN} checked, {wrong} not)");
            });

            AddRange("R12", "today", "open", "return fire in the band (Motive tuning.md §7.10 rule 1): an officer in cover shot at 15.5 m, inside its 16 m max but beyond its 15 m effective ring, fires back within kite.react plus its aim, and doesn't move", r =>
            {
                var g = r.G;
                Spawned(r, "police", "cover", 1);
                var off = g.Hostiles.First(h => h.Id == "officer-1"); var op = off.Pos(g.Tick);
                // ground 15.5 m from it with a line, the nearest the Scout can walk to without coming inside its 15 m ring on the way
                bool Outside(Pt p) { var rt = g.S.Grid.Route(r.Op("scout").Pos, p); if (rt == null) return false; var path = new Path(rt); for (long mm = 0; mm <= path.Length; mm += 200) if (G.Within(path.At(mm), op, 1510)) return false; return true; }
                var spot = FreeWhere(r, r.Op("scout").Pos, p => !G.Within(p, op, 1530) && G.Within(p, op, 1570) && g.S.Sight(p, op, g.R.Eye) && Outside(p), "scout", 6000);
                if (!r.Is(spot.HasValue, "ground 15.5 m from officer-1 with a line")) return;
                r.To(r.SendTo("scout", spot!.Value));
                Hold(r, "scout");
                r.Fire("scout", "officer-1");
                if (!r.Is(r.Until(() => r.Shots().Exists(s => s.by == "scout"), Game.T(10)), "the Scout fires at 15.5 m")) return;
                int first = r.Shots().First(s => s.by == "scout").tick;
                bool answered = r.Until(() => r.Shots().Exists(s => s.by == "officer-1" && s.target == "scout"), Game.T(5));
                r.Is(answered, "officer-1 fires back on the Scout");
                if (!answered) return;
                int back = r.Shots().First(s => s.by == "officer-1" && s.target == "scout").tick;
                r.Is(back - first <= g.R.KiteReact + g.AimNewT + 1, $"within kite.react plus its aim ({back - first} ticks)");
                r.Eq(off.Pos(g.Tick), op, "and stays in its cover");
            });

            AddRange("R11", "today", "open", "police in the open answer fire from beyond their reach (the kite response, today since tuning.md §7.9, Sky 2026-10-20): two officers beyond 15 m, shot at by the Enforcer, close to their ring within kite.react and fire on the cell", r =>
            {
                var g = r.G;
                Spawned(r, "police", "beyond", 2);
                r.Fire("enforcer", "officer-1");
                r.Is(r.Until(() => r.Shots().Exists(s => s.by == "enforcer"), Game.T(10)), "the Enforcer fires from beyond their reach");
                int first = r.Shots().First(s => s.by == "enforcer").tick;
                r.Is(r.Until(() => g.Log.Exists(l => l.Code == "ai.kite" && l.Text.StartsWith("officer-", StringComparison.Ordinal)), Game.T(5)), "they answer (ai.kite)");
                var k = g.Log.First(l => l.Code == "ai.kite" && l.Text.StartsWith("officer-", StringComparison.Ordinal));
                r.Is(k.Tick - first <= g.R.KiteReact, $"within kite.react of the first shot ({k.Tick - first} ticks)");
                r.Is(r.Until(() => r.Shots().Exists(s => s.by.StartsWith("officer-", StringComparison.Ordinal)), Game.T(20)), "and fire on the cell");
            });

            AddRange("R09", "today", "dense", "the dense layout: every slot of every placement takes a hostile, each at its distance from the mark", r =>
            {
                var g = r.G; var mark = g.RangeMark;
                var band = new Dictionary<string, (int lo, int hi)> { ["near"] = (700, 900), ["eff"] = (1400, 1600), ["beyond"] = (1700, 1900), ["cover"] = (1600, 2600) };
                foreach (var p in Game.RangePlacements)
                {
                    Spawned(r, "police", p, 6);
                    for (int k = 1; k <= 6; k++) { var s = g.RangeSlot(p, k); long d = G.Isqrt(G.D2(s, mark)); r.Is(d >= band[p].lo && d <= band[p].hi, $"{p} slot {k} stands {d / 100.0:0.00} m from the mark"); }
                }
                r.Eq(g.Hostiles.Count, 24, "24 police on the dense floor");
                r.Is(g.CoverNow().Count > 54, $"the dense layout has more cover than the open ({g.CoverNow().Count} points standing)");
                r.Wait(Game.T(5));
                r.Eq(g.Result, Outcome.None, "it runs");
            });

            FireMoveRange(); // engine 0.15.0 (tuning.md §8): fire on the move
            TriggerRange(); // engine 0.17.0 (tuning.md §11): the trigger, locks and follow fire
            RangeKindsSet(); // cars, bystanders, the lane, the lines in words (ScenariosRangeKinds.cs)
            AddRange("R10", "lab", "open", "replay: the run as a script (mission range, the spawn's count) replays and verifies its record", r =>
            {
                var g = r.G;
                Spawned(r, "rivals", "beyond", 2); Spawned(r, "police", "near", 1);
                r.Do("cell", "panic");
                r.Wait(Game.T(30));
                var inputs = r.Inputs!;
                g.Finish();
                var text = Sim.Script.Write(g, inputs.Hash);
                r.Is(text.Contains("\"mission\":\"range\"") && text.Contains("\"count\":2"), "the script names the range and the spawn's count");
                var sc = Sim.Script.Parse(text);
                var again = Engine.Play(inputs, sc.Start, sc.Orders, sc.Until);
                r.Eq(sc.Verify(again, inputs.Hash), "", "the replay matches its record (inputs, engine, start, every checkpoint, outcome)");
            });
        }

        /// <summary>R03, R04: one officer at the effective ring, the Enforcer drawn (its target); the line's chance in the open, then beside the
        /// car box's cover point that shields him from it; each checked against the ‰ the engine's next shot lands at.</summary>
        static void CoverChanges(Run r, bool lab)
        {
            var g = r.G;
            Spawned(r, "police", "eff", 1);
            r.Do("enforcer", "draw"); Hold(r, "enforcer"); // he holds his fire: officer-1 stays up to be measured
            var h = H(r, "officer-1"); var hp = h.Pos(g.Tick);
            r.Is(r.Until(() => h.Gun.Target == "enforcer", Game.T(10)), "officer-1 aims at the drawn Enforcer");
            var open = LineOf(r, "officer-1", "enforcer")!;
            r.Eq(open.Cover, "none", "in the open: no cover");
            r.Is(open.InReach && open.Aimed, "the line is in reach and aimed");
            r.Is(r.Until(() => LoggedAcc(r, g.Tick, "officer-1", "enforcer") >= 0, Game.T(10)), "officer-1 fires");
            r.Eq(LoggedAcc(r, g.Tick, "officer-1", "enforcer"), open.Acc, "the shot lands at the line's chance in the open");
            var cp = g.S.Cover.Where(c => c.Of == "range/car-box-1" && CoverRules.Protects(c, hp, g.Tick / Game.Hz, g.R.PointBlankCm)).OrderBy(c => G.D2(c.At, g.Enforcer.Pos)).First();
            r.Is(cp.Q >= g.R.CoverQFull, $"the box's point {cp.Id} is full cover (q {cp.Q})");
            int arrive = r.SendTo("enforcer", cp.At); r.To(arrive + 1); // drawn, the lab's snap lands on the clicked point itself (nearest, it shields)
            r.Eq(g.Enforcer.Pos, cp.At, "the Enforcer stands on the point");
            var walking = g.Tick;
            var cov = LineOf(r, "officer-1", "enforcer")!;
            r.Eq(cov.Cover, "full", "beside the box: full cover against officer-1");
            r.Eq(cov.CoverId, cp.Id, "from that point");
            if (lab) r.Eq(cov.Pips, open.Pips - 1, "one pip less");
            else r.Eq(cov.Acc, open.Acc * g.R.ModCover / 1000, "the chance times weapons.mod.cover");
            r.Is(cov.Acc < open.Acc, $"harder to hit: {open.Acc} → {cov.Acc} ‰");
            r.Is(r.Until(() => LoggedAcc(r, g.Tick, "officer-1", "enforcer") >= 0 && g.Tick > walking, Game.T(10)), "officer-1 fires again");
            r.Eq(LoggedAcc(r, g.Tick, "officer-1", "enforcer"), cov.Acc, "the shot lands at the line's chance in cover");
            var back = LineOf(r, "enforcer", "officer-1")!;
            r.Is(back.Line && back.Cover == "none", "the Enforcer's own line on officer-1: clear, officer-1 in the open");
        }
    }
}
