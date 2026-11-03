// Brief 080 (engine 0.37.0): the seed and the rolled shot, as scenarios, so Unity plays them too (GDD §01: a run replays exactly from seed + orders; §06 fair shots).
//    SEED-REPLAY    same seed + orders = the same checkpoint hashes; the script a run writes carries its seed and replays against its record
//    SEED-DIFFER    different seeds give different starts and different runs from the same orders
//    SEED-NEUTRAL   no seed is seed 0: the mission's timetable, the sweep at its end, the id-only scan phases
//    SEED-FIXED     never by the seed: the mission's clock, the door guard's post, prices, the cast; fuzziness 0 draws nothing
//    SEED-READS     a host's reads draw nothing: a game read on every tick stays byte-identical to one that isn't
//    SEED-FAIR      the patrol never begins with the cell's stands in its cone, and a Watch's schedule holds to the tick
//    SHOT-SUM       the sheet's sum, the design's worked examples, the limits: no shot at 100 % or 0 %
//    SHOT-STREAK    the caps hold across seeds: no hostile lands a fourth hit in a row on one agent; no agent misses a fourth in a row at 20 % or better
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static Script? SeedFixture(string name)
        {
            using var st = typeof(Scenarios).Assembly.GetManifestResourceStream("Suborned.Sim.Fixtures." + name + ".script.json");
            if (st == null) return null;
            using var rd = new System.IO.StreamReader(st);
            var s = Suborned.Sim.Script.Parse(rd.ReadToEnd()); s.Expect = null; return s;
        }
        static Game PlaySeed(Engine.Inputs i, Script s, int seed) { var st = Start.Parse(JNode.Parse(s.Start.Canonical(), "start")); st.Seed = seed; return Engine.Play(i, st, s.Orders, s.Until); }
        static bool SameChecks(Game a, Game b) { if (a.Checkpoints.Count != b.Checkpoints.Count) return false; for (int k = 0; k < a.Checkpoints.Count; k++) if (a.Checkpoints[k] != b.Checkpoints[k]) return false; return true; }
        /// <summary>The chance a shot's log line carries, per mille.</summary>
        static int LoggedChance(string text) { int a = text.IndexOf('('), b = text.IndexOf(" ‰", StringComparison.Ordinal); return a < 0 || b < a ? -1 : int.Parse(text.Substring(a + 1, b - a - 1), CultureInfo.InvariantCulture); }

        static void SeedCases()
        {
            const string Fx = "sq-route-a-board";
            Add("SEED-REPLAY", "080", "Same seed + orders = the same checkpoint hashes; a run's written script carries the seed and replays against its record.", r =>
            {
                r.Wait(1); // the scenario's own run steps a tick, so its replay plays that tick, not the whole mission
                var i = r.Inputs!; var s = SeedFixture(Fx); if (!r.Is(s != null, "the fixture loads")) return;
                var a = PlaySeed(i, s!, 42); var b = PlaySeed(i, s!, 42);
                r.Is(a.Log.Exists(l => l.Code == "shot.hit" || l.Code == "shot.miss"), "the run has shots to roll");
                r.Is(SameChecks(a, b), "the same seed and orders: every checkpoint identical");
                r.Is(SameChecks(a, Engine.Replay(i, a)), "Engine.Replay from its own start and orders: identical");
                var text = Suborned.Sim.Script.Write(a, i.Hash); r.Is(text.Contains("\"seed\":42"), "the saved run carries its seed");
                var back = Suborned.Sim.Script.Parse(text); r.Eq(back.Start.Seed, 42, "and reads it back");
                var replay = Engine.Play(i, back.Start, back.Orders, back.Until); r.Eq(back.Verify(replay, i.Hash), "", "and replays against its record");
            });
            Add("SEED-DIFFER", "080", "Different seeds differ: the start's hash, the patrol's place, the crowd, the rivals' arrival, and the run from the same orders.", r =>
            {
                r.Wait(1); // the scenario's own run steps a tick, so its replay plays that tick, not the whole mission
                var i = r.Inputs!; var s = SeedFixture(Fx); if (!r.Is(s != null, "the fixture loads")) return;
                var starts = new HashSet<ulong>(); var ends = new HashSet<ulong>(); var arrive = new HashSet<int>(); var phases = new HashSet<int>(); var posts = new HashSet<string>(); int crowd = 0;
                var g0 = Engine.New(i);
                for (int seed = 1; seed <= 24; seed++)
                {
                    var a = PlaySeed(i, s!, seed); ends.Add(a.Checkpoints[a.Checkpoints.Count - 1].hash); starts.Add(a.StartHash);
                    var f = Engine.New(i, new Start { Seed = seed }); arrive.Add(f.RivalAt); phases.Add(f.PatrolPhase);
                    for (int k = 0; k < f.Walkers.Count; k++) if (f.Walkers[k].To[0] != g0.Walkers[k].To[0]) { crowd++; break; }
                    f.RunTo(Game.T(S_PostsRead)); posts.Add(string.Join(",", f.Crew.Take(3).Select(m => m.Spot.ToString(CultureInfo.InvariantCulture))));
                }
                r.Eq(starts.Count, 24, "24 seeds, 24 start hashes");
                r.Eq(ends.Count, 24, "24 seeds, 24 final hashes from the same orders");
                r.Is(arrive.Count > 5 && arrive.All(a => Math.Abs(a - g0.RivalAt) <= Game.T(g0.R.Seeds.RivalsArriveS)), $"the rivals arrive at {arrive.Count} different ticks, each within the band");
                r.Is(phases.Count > 12, $"the patrol begins at {phases.Count} places on its sweep");
                r.Eq(crowd, 24, "the crowd differs from the neutral start on every seed");
                r.Is(posts.Count > 2, $"the rivals take their posts {posts.Count} ways");
            });
            Add("SEED-NEUTRAL", "080", "No seed is seed 0, the neutral start: the mission's timetable, the sweep from its end A, the phases from the id alone.", r =>
            {
                r.Wait(1); // the scenario's own run steps a tick, so its replay plays that tick, not the whole mission
                var g = r.G; r.Eq(g.Seed, 0, "no seed reads 0");
                r.Eq(g.RivalAt, Game.T(g.S.RivalsArrive), "the rivals on the mission's timetable");
                r.Eq(g.PatrolPhase, 0, "the sweep from its end A");
                var c = Engine.New(r.Inputs!, new Start { Seed = 0 }); r.Eq(c.StartHash, g.StartHash, "an explicit seed 0 is the same start, hash and all");
                r.Is(!c.Start.Canonical().Contains("seed"), "and adds no bytes");
            });
            Add("SEED-FIXED", "080", "Never by the seed: Vale's clock, the guard's post and marks, prices, cast; fuzziness 0 draws nothing.", r =>
            {
                r.Wait(1); // the scenario's own run steps a tick, so its replay plays that tick, not the whole mission
                var i = r.Inputs!; var a = Engine.New(i);
                r.Eq(a.R.Seeds.FuzzOf("councillor"), 0, "Vale is always the same"); r.Eq(a.R.Seeds.FuzzOf("waitress"), 0, "Inez is always the same");
                r.Is(a.R.Seeds.FuzzOf("p-records-office-73") > 0, "a clerk has a whole band");
                for (int seed = 1; seed <= 12; seed++)
                {
                    var b = Engine.New(i, new Start { Seed = seed });
                    r.Eq((b.Cash, b.S.ValeExits, b.S.ValeAtCar, b.S.Chime, b.S.DriverLast), (a.Cash, a.S.ValeExits, a.S.ValeAtCar, a.S.Chime, a.S.DriverLast), "the purse and the mission's clock");
                    r.Eq(b.GuardPose()!.Value.pos, a.GuardPose()!.Value.pos, "the door guard at his post");
                    r.Eq(b.NextMarkOf(b.GuardP, 0), a.NextMarkOf(a.GuardP, 0), "and his next mark");
                    int staffOff = 0; for (int k = 0; k < a.Walkers.Count; k++) if (b.NowStaff(b.Walkers[k]) && (b.Walkers[k].To[0] != a.Walkers[k].To[0])) staffOff++;
                    r.Eq(staffOff, 0, "who keeps a post keeps its clock");
                }
            });
            Add("SEED-READS", "080", "A host's reads draw nothing: fire previews, fire lines, cards and schedules on every tick of a fight, and the game stays byte-identical.", r =>
            {
                r.Wait(1); // the scenario's own run steps a tick, so its replay plays that tick, not the whole mission
                var i = r.Inputs!; var s = SeedFixture(Fx); if (!r.Is(s != null, "the fixture loads")) return;
                var plain = PlaySeed(i, s!, 7);
                var st = new Start { Seed = 7 }; var g = Engine.New(i, st); var os = s!.Orders.OrderBy(o => o.Tick).ToList(); int oi = 0, reads = 0; int[] shotsBefore = new int[4];
                for (int t = 0; t <= plain.Tick && !g.Over; t++)
                {
                    if (t > 0) g.RunTo(t);
                    while (oi < os.Count && os[oi].Tick == t) { g.Issue(os[oi]); oi++; }
                    if (t < 1500) continue;
                    foreach (var op in g.Ops)
                    {
                        int n = op.Gun.ShotN;
                        foreach (var m in g.Crew) if (!m.Gun.Down) { var p = g.PreviewFire(op.Id, m.Id); reads++; _ = p.Sum; }
                        g.GoingLoud(new Order { Tick = t, Op = Op.Name(op.Id), Do = "draw" }); g.Routine("patrol", 300); g.RangeLines();
                        if (op.Gun.ShotN != n) r.Is(false, "a read moved a stream");
                    }
                }
                g.Finish();
                r.Is(reads > 100, $"{reads} previews read");
                r.Eq(Hash.Hex(g.Checkpoints[g.Checkpoints.Count - 1].hash), Hash.Hex(plain.Checkpoints[plain.Checkpoints.Count - 1].hash), "the run's final hash is the unread run's");
            });
            Add("SEED-FAIR", "080", "Every start is fair: no start has the cell's stands in the patrol's cone for the first 15 s; what a Watch of the patrol shows holds to the tick.", r =>
            {
                r.Wait(1); // the scenario's own run steps a tick, so its replay plays that tick, not the whole mission
                var i = r.Inputs!; int moved = 0;
                for (int seed = 1; seed <= 200; seed++)
                {
                    var g = Engine.New(i, new Start { Seed = seed }); if (g.PatrolPhase != 0) moved++;
                    var early = new List<string>();
                    for (int t = 1; t <= Game.T(g.R.Seeds.FairFirstS); t++)
                    {
                        g.Step();
                        foreach (var op in g.Ops) if (g.PatrolWatching(op.Pos)) early.Add($"{seed}: patrol sees {op.Id} at {t}");
                    }
                    r.Is(early.Count == 0, early.Count == 0 ? $"seed {seed}: unseen" : early[0]);
                    r.Is(!g.Log.Exists(l => l.Code.StartsWith("notice", StringComparison.Ordinal) || l.Code.StartsWith("attention", StringComparison.Ordinal) || l.Code.StartsWith("report", StringComparison.Ordinal)), $"seed {seed}: no notice, no report in the first 15 s");
                }
                r.Is(moved > 150, $"the patrol's start moved on {moved} of 200 seeds");
                var told = new Start { Seed = 5 }; told.Known["patrol"] = new List<string> { "sight", "routine" }; var w = Engine.New(i, told); var ev = w.Routine("patrol", Game.T(w.R.PatrolPeriodS))!; int seen = 0;
                foreach (var e in ev)
                {
                    if (e.What != "turn-at-a" && e.What != "turn-at-b") continue;
                    w.RunTo(e.Tick); seen++;
                    r.Eq(w.PatrolPose()!.Value.pos, w.S.Stand(e.What == "turn-at-a" ? w.S.PatrolA : w.S.PatrolB), $"{Report.Clock(e.Tick)}: at the sweep's end, as the Watch read it");
                }
                r.Is(seen >= 1, $"{seen} turns read and kept");
            });
            Add("PACE-CLOCK", "080", "The shift comes forward a minute (Sky 2026-10-30, pacing.html): Vale out 4:50, at his car 5:10, close 7:30, the driver's last turn 4:40, the rivals where they were; the guard's marks 1:00, 2:30, 4:00; talking about 30 % shorter, and the prices shown follow; Enter and Take stay at 38 s.", r =>
            {
                r.Wait(1); var g = r.G;
                r.Eq((g.S.Chime, g.S.DriverLast, g.S.ValeExits, g.S.ValeAtCar, g.S.Over, g.S.RivalsArrive), (270, 280, 290, 310, 450, 190), "the schedule");
                r.Eq(g.NextMarkOf(g.GuardP, 1), T(60), "the guard's first mark, 1:00"); r.Eq(g.NextMarkOf(g.GuardP, T(61)), T(150), "2:30"); r.Eq(g.NextMarkOf(g.GuardP, T(151)), T(240), "4:00");
                r.Eq(g.R.Num("v:listen-s") , 30, "Listen 30 s"); r.Eq(g.R.Num("v:mislead-s"), 30, "Mislead 30 s"); r.Eq(g.R.Num("v:reroute-s"), 30, "Reroute 30 s"); r.Eq(g.R.Num("v:enter-s"), 38, "Enter stays 38 s"); r.Eq(g.R.Num("v:take-s"), 38, "Take stays 38 s");
                r.To(T(291)); r.Eq(g.Vale, ValeAt.Leaving, "at 4:51 Vale is walking to his car");
                r.To(T(313) + 1); r.Is(g.Over && g.FailCode == "vale-leaves", $"by 5:13 he is driven away, no plan made ({g.FailCode})");
            });
            Add("CLOSE-VERDICT", "080", "The close is a verdict (fight.close): Vale in the Enforcer's hands, the warning a minute before, closed-in at the close, naming why; the warning once.", r =>
            {
                r.Wait(1); var g = r.G; r.Eq(g.R.CloseWarnS, 60, "the warning a minute before");
                var s = new Start { FinalFight = false, Security = 0 }; s.Turned.Add("driver"); s.CrewGone.AddRange(new[] { "rival-1", "rival-2", "rival-3", "rival-4", "rival-5" }); var h = Engine.New(r.Inputs!, s);
                h.Vale = ValeAt.WithEnforcer; h.RunTo(T(h.ClockOver) - T(60) - 1); r.Eq(h.CloseWarnedAt, -1, "not yet");
                h.Step(); r.Eq(h.CloseWarnedAt, T(h.ClockOver) - T(60), "at 6:30 Vale's absence is noticed"); r.Is(h.Log.Exists(l => l.Code == "close.warning"), "the HUD's line");
                h.RunTo(T(h.ClockOver)); r.Eq((h.Result, h.FailCode, h.Cause?.Why), (Outcome.Failure, "closed-in", "on-foot"), "closed in, Vale on foot"); r.Is(h.Cause != null && h.Cause.Culprits.Count > 0, "and it names who");
                r.Eq(h.Log.FindAll(l => l.Code == "close.warning").Count, 1, "the warning, once");
            });
            Add("BOARD-REACH", "080", "fight.spacing.cell keeps Board's reach: four agents sent to the getaway car's stand all end within reach of it, apart.", r =>
            {
                r.Wait(1); var g = r.G; string[] all = { Scout, Fixer, Tech, Enf }; int last = 0;
                foreach (var op in all) { last = Math.Max(last, r.Send(op, "d:Getaway bay")); }
                r.To(last + 1);
                foreach (var op in all) r.Is(g.AtGetaway(r.Op(op).Pos), $"{op} can Board from {r.Op(op).Pos}");
                for (int a = 0; a < 4; a++) for (int b = a + 1; b < 4; b++) r.Is(!G.Within(r.Op(all[a]).Pos, r.Op(all[b]).Pos, 20), $"{all[a]} and {all[b]} stand apart");
            });
            Add("GUARD-LEAVES", "080", "Off-shift guards leave directly (crowd.staffDirect): guard 46 is clear of the service lane within 90 s of his shift's end, at the city's walk, and waits at the far end.", r =>
            {
                r.Wait(1); var g = r.G; var w = g.Walkers.Find(x => x.P.Id == "p-records-office-46")!; int i = Array.IndexOf(w.Place, "cafe-table-0-seat-1"); if (!r.Is(i > 0, "his leg")) return;
                var end = g.People2!.Places[w.Place[i]]; int leave = w.To[i - 1];
                r.To(leave + T(90)); r.Eq(g.CrowdPos(w), end, "there within 90 s of leaving");
            }, new Start { ValeWaits = true, FinalFight = false });
            Add("SHOT-INTAKE", "080", "Focus fire is bounded (weapons.sheet.intake): across seeds of real fights, no agent takes two hits closer than the gap, so full to down takes 5.2 s or more, the Enforcer 9.1 s or more; it still kills, and cover still pays.", r =>
            {
                r.Wait(1); var i = r.Inputs!; int pairs = 0, downs = 0, gap = i.Rules.Sheet.IntakeTicks;
                foreach (var fx in new[] { Fx, "bal-loud-x4" })
                {
                    var s = SeedFixture(fx); if (!r.Is(s != null, $"{fx} loads")) return;
                    for (int seed = 1; seed <= 60; seed++)
                    {
                        var g = PlaySeed(i, s!, seed); var last = new Dictionary<string, int>();
                        foreach (var l in g.Log)
                        {
                            if (l.Code != "shot.hit") continue;
                            var w = l.Text.Split(' '); string by = w[0], t = w[3]; if (by == "scout" || by == "fixer" || by == "tech" || by == "enforcer" || !(t == "scout" || t == "fixer" || t == "tech" || t == "enforcer")) continue;
                            if (last.TryGetValue(t, out int p)) { pairs++; if (l.Tick - p < gap) r.Is(false, $"{fx} seed {seed}: {t} hit {l.Tick - p} ticks apart at {Report.Clock(l.Tick)}"); }
                            last[t] = l.Tick;
                        }
                        foreach (var op in g.Ops) if (op.Gun.Down) downs++;
                    }
                }
                r.Is(pairs > 100, $"{pairs} pairs of hits read"); r.Is(downs > 0, $"focus fire still drops agents: {downs} down");
            });
            Add("CARD-COMING", "080", "The takedown card warns of who is about to arrive: at any tick, who on their routine would notice the act within its seconds and doesn't now is on Coming, and arrives when it says, and nobody else does.", r =>
            {
                r.Wait(1); var i = r.Inputs!; int listed = 0, checks = 0;
                for (int seed = 0; seed <= 8; seed++)
                {
                    var g = Engine.New(i, new Start { Seed = seed }); var at = g.GuardPose()!.Value.pos; g.RunTo(T(120));
                    for (int t = T(120); t < T(280); t += T(7))
                    {
                        g.RunTo(t); at = g.GuardPose()!.Value.pos; int s = 6;
                        var coming = g.ComingToSee(at, s, "takedown", g.GuardP); var now = new HashSet<string>(); foreach (var w in g.Walkers) if (g.CrowdPos(w) is Pt p && g.NoticesNow(w.P.Id, "takedown", at)) now.Add(w.P.Id);
                        var later = new HashSet<string>();
                        for (int k = 0; k < T(s); k += 5) { g.RunTo(t + k + 5); foreach (var w in g.Walkers) if (!now.Contains(w.P.Id) && g.CrowdPos(w) is Pt p && g.NoticesNow(w.P.Id, "takedown", at)) later.Add(w.P.Id); }
                        checks++; listed += coming.Count;
                        var named = new HashSet<string>(coming.Select(c => c.p.Id));
                        if (!later.SetEquals(named)) r.Is(false, $"seed {seed} at {Report.Clock(t)}: the card names [{string.Join(",", named)}], who came [{string.Join(",", later)}]");
                    }
                }
                r.Is(checks > 100 && listed > 0, $"{checks} cards read, {listed} arrivals foretold");
            });
            Add("SHOT-SUM", "080", "The sheet's sum: the design's worked examples; no shot is ever at 100 % or 0 %.", r =>
            {
                r.Wait(1); // the scenario's own run steps a tick, so its replay plays that tick, not the whole mission
                var g = r.G; var k = g.R.Sheet; int P(string who, Weapon w, int m, Gait gait, int cov, bool running = false, bool pin = false) => k.Sum(k.AimOf(who), g.R, w, m * 100, gait, cov, running, pin).Pct;
                r.Eq(P("guard", g.R.Pistol, 15, Gait.Stand, 1000, running: true), 40, "guard at 15 m, Enforcer running: 50 − 10");
                r.Eq(P("rival-1", g.R.Pistol, 10, Gait.Stand, g.R.ModCover), 20, "rival at 10 m, agent still in full cover: 65 − 45");
                r.Eq(P("rival-1", g.R.Pistol, 10, Gait.Stand, g.R.ModCoverPartial), 40, "in partial cover: 65 − 25");
                r.Eq(P("police-1", g.R.Pistol, 12, Gait.Walk, 1000), 35, "officer walking at 12 m, agent in the open: 60 − 25");
                r.Eq(P("enforcer", g.R.Carbine, 18, Gait.Stand, 1000), 75, "Enforcer at 18 m, rival in the open");
                r.Eq(P("rival-1", g.R.Pistol, 4, Gait.Stand, 1000), 95, "rival at 4 m, Scout in the open: 65 + 30");
                r.Eq(P("enforcer", g.R.Carbine, 4, Gait.Stand, 1000), 95, "kept to the top limit: 75 + 30 would be 105");
                r.Eq(P("civilian", g.R.Pistol, 15, Gait.RunAway, g.R.ModCover), 5, "kept to the bottom limit: 35 − 45 − 55 is far below");
                foreach (var who in SheetRules.Classes) foreach (var gait in new[] { Gait.Stand, Gait.Walk, Gait.Run, Gait.RunAway }) foreach (var cov in new[] { 1000, g.R.ModCover, g.R.ModCoverPartial }) for (int m = 0; m <= 16; m++) foreach (var pin in new[] { false, true })
                {
                    int v = P(who == "officer" ? "police-1" : who == "robot" ? "patrol" : who == "rival" ? "rival-1" : who == "civilian" ? "p-x" : who, g.R.Pistol, m, gait, cov, false, pin);
                    if (v < k.Min || v > k.Max) r.Is(false, $"{who} {gait} {cov} {m} m: {v} %");
                }
                r.Eq((k.AimOf("enforcer"), k.AimOf("rival-3"), k.AimOf("scout"), k.AimOf("security-1"), k.AimOf("officer-2"), k.AimOf("fixer"), k.AimOf("tech"), k.AimOf("guard"), k.AimOf("robot-1"), k.AimOf("p-records-office-73")), (75, 65, 60, 60, 60, 55, 50, 50, 45, 35), "the ladder Sky approved");
                r.Eq((g.GuardGun.Max, g.R.HealthCivilian, g.R.HealthRival, g.R.Pistol.DamageAt(0), g.R.Carbine.DamageAt(0)), (3, 3, 3, 1, 2), "guards and civilians 3 HP, rivals 3; pistols 1, the carbine 2");
                r.Is(k.GuardSpeed < g.R.SecuritySpeed, "guards move slow");
            });
            Add("SHOT-STREAK", "080", "Over many seeds of a real fight: every chance logged is inside the limits; no hostile lands a fourth hit in a row on one agent; no agent misses a fourth in a row at 20 % or better; the Enforcer's warning goes out before he can drop.", r =>
            {
                r.Wait(1); // the scenario's own run steps a tick, so its replay plays that tick, not the whole mission
                var i = r.Inputs!; int shots = 0, warned = 0, capped = 0, leaned = 0;
                foreach (var fx in new[] { Fx, "bal-loud-x4" })
                {
                    var s = SeedFixture(fx); if (!r.Is(s != null, $"{fx} loads")) return;
                    for (int seed = 1; seed <= 60; seed++)
                    {
                        var g = PlaySeed(i, s!, seed);
                        var run = new Dictionary<string, int>(); var lastChance = new Dictionary<string, int>();
                        foreach (var l in g.Log)
                        {
                            if (l.Code == "enforcer.warning")
                            {
                                warned++;
                                r.Is(g.Log.FindAll(x => x.Code == "enforcer.warning").Count == 1, "the warning, once");
                                int down = g.Log.FindIndex(x => x.Code == "enforcer.down" || x.Code == "fail.enforcer-down");
                                if (down >= 0) r.Is(g.Log.IndexOf(l) < down, "before he drops");
                            }
                            if (l.Code != "shot.hit" && l.Code != "shot.miss") continue;
                            shots++; int c = LoggedChance(l.Text);
                            if (c < g.R.Sheet.Min * 10 || c > g.R.Sheet.Max * 10) r.Is(false, $"{fx} seed {seed}: {l.Text}");
                            var w = l.Text.Split(' '); string by = w[0], t = w[3], key = by + ">" + t; bool cell = by == "scout" || by == "fixer" || by == "tech" || by == "enforcer", onCell = !cell && (t == "scout" || t == "fixer" || t == "tech" || t == "enforcer");
                            run.TryGetValue(key, out int n); bool hit = l.Code == "shot.hit";
                            if (onCell)
                            {
                                if (n >= g.R.Sheet.StreakHits) { capped++; if (hit) r.Is(false, $"{fx} seed {seed}: {by} landed a hit after {n} in a row on {t} at {Report.Clock(l.Tick)}"); run[key] = 0; }
                                else run[key] = hit ? n + 1 : 0;
                            }
                            else if (cell)
                            {
                                if (n >= g.R.Sheet.StreakMisses && c >= g.R.Sheet.StreakFloor * 10) { leaned++; if (!hit) r.Is(false, $"{fx} seed {seed}: {by} missed after {n} in a row on {t} at {c} ‰"); run[key] = 0; }
                                else run[key] = hit ? 0 : n + 1;
                            }
                        }
                    }
                }
                r.Is(shots > 1000, $"{shots} shots read");
                r.Is(capped > 0, $"the hits cap was reached {capped} times"); r.Is(leaned > 0, $"the misses cap was reached {leaned} times"); r.Is(warned > 0, $"the Enforcer's warning went out in {warned} runs");
            });
        }
        const int S_PostsRead = 300; // by 5:00 the posted rivals have their spots on every seed's timetable
    }
}
