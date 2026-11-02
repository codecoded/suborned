// Engine 0.33.0 (brief 059, Sky's S1 = no; Motive briefs/059/motive/s1-rulings.json checks.keel and balance-rulings.json checks.keel; GDD v0.25.0
// §06 the getaway ("the fight always comes"), §07 final fight; rulebook fight.alwaysRule, rivals.peopleRule). The done-check's scenarios:
//    S1-GATE-EARLY-YARD   Vale aboard at the service drive before the rival car enters: Go is refused car-held [car:rivals] until the first
//                         rider steps out at rival-tail; he is the blocker and holds the car wherever he is; Go once he is down
//    S1-GATE-SWITCH-OFF   the same start with fight-always 0: Go at Vale-aboard, the fight never springs (the leak, as 0.32.0)
//    S1-GATE-LOUD-YARD    route B taken at Alert 2: Go is refused while the police blocker is up, wherever he stands (M059-5.4's race closed)
//    S1-SPRING-LATE       the spring with nobody on foot: the blocker is picked at the first step-out (rival-1, ties by id); each arrival
//                         fires weapons.aimNew after he steps out
//    S1-FLOOR-TIP         the tip, the warned lookout, P1 gone: the tipped car comes back at once, the wheelman out at rival-tail ~19.8 s on
//    S1-FLOOR-LOOKOUT     no tip, the warned lookout, P1 and P2 gone: the wheelman by car and the lookout walking back, count 3
//    S1-FLOOR-NO-SPAWN    P1-P3 gone, tip, warn: the wheelman and the lookout come back, count 2; no hostile id beyond rival-5
//    S1-FLOOR-WAKE        P3 knocked out more than 2:00 before the fight: he wakes, rejoins his post; no recall
//    S1-FLOOR-ZERO        rivals-floor 0 with S1-FLOOR-NO-SPAWN's start: no recall; not destroyed (the wheelman and lookout live)
//    S1-CREW-WIPED        all five gone: the fight is over at its start, the gate is open, the debrief's why reads crew-destroyed
//    S1-SEE-TAKEDOWN      rival-1 (ambush-3) knocked out in rival-2's cone (ambush-1, 2.3 m): rival-2 fires, gunfire, Alert >= 2: loud
//    (S1-GATE-C037 is the xunit test Suborned.Sim.Tests/GateC037.cs: it reads the committed route scripts.)
//    BAL-JOIN-POS         M059-B1: at the safe fight's start every crew body stands where its member stood (0 cm), never at (0,0)
//    BAL-YARD-AT-WILL     M059-B7 restated (one input): route B at the yard, F at the spring holds the carry (rb-ff, rbb-ff win); F only
//                         at the Board on board and go loses (rivals-reach-vale, rivals.for-car)
//    BAL-YARD-FOCUS       M059-B4, B7: the same with the Scout and Fixer locking rival-1 then rival-2 from the spring: wins; and with both
//                         locks 3.0 s late
//    BAL-YARD-NEVER-DRAWN engine 0.35.2 (Motive rulings-0.35.1 §2): rb-ff and rbb-ff never drawn: the rivals take Vale from the held car
//    BAL-YARD-POST        M059-B9: the yard's blocker (svc-ambush-1's man) walks ~9.16 m to (86, -5) within the carry and Board
//    BAL-LOUD-BREAK       M059-B5: loud, the four drawn on foot: no break at 1 or 2 down, the break at the third
//    BAL-NOPRESS          M059-B3: after that break no member presses for the Enforcer who walks Vale out
//    BAL-LANE             M059-B2: a rival blocker fired on from beyond his effective reach stays on his lane
// "Gone" members are Start.CrewGone's (test only: killed before the mission, no body); the takedown scenarios use real orders.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static Engine.Inputs AlwaysOff(Engine.Inputs i) => With059(i, "\"fight-always\": \\{ \"v\": 1,", "\"fight-always\": { \"v\": 0,");
        static Engine.Inputs FloorZero(Engine.Inputs i) => With059(i, "\"rivals-floor\": \\{ \"v\": 3,", "\"rivals-floor\": { \"v\": 0,");
        static Start Gone(Start s, params string[] ids) { s.CrewGone.AddRange(ids); return s; }
        static string Sec1(double s) => s.ToString("0.0", CultureInfo.InvariantCulture);
        static int LogTick(Game g, string code, int from = 0) { foreach (var l in g.Log) if (l.Code == code && l.Tick >= from) return l.Tick; return -1; }
        static int LogTick(Game g, string code, Func<string, bool> text, int from = 0) { foreach (var l in g.Log) if (l.Code == code && l.Tick >= from && text(l.Text)) return l.Tick; return -1; }
        /// <summary>The rival bodies of the final fight now, in id order.</summary>
        static List<string> RivalBodies(Game g) => g.Hostiles.Where(h => h.Id.StartsWith("rival-", StringComparison.Ordinal)).Select(h => h.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();

        /// <summary>Vale aboard the getaway car at the service drive before the rival car enters (A-pressure's shape: the Scout drives the car round
        /// first; the Tech loops and reroutes, the Fixer calls a turned Inez, the Enforcer enters and comes out at the service lane). The Fixer
        /// and the Tech then wait in the yard. Start: Inez turned.</summary>
        static void YardEarly(Run r)
        {
            var g = r.G;
            int at = Max(r.Send(Tech, "cabinet"), r.Send(Enf, "d:Office entrance"), r.SendTo(Scout, g.GetawaySeat));
            r.To(at);
            r.Do(Scout, "board", "car"); r.Done(Scout); r.Do(Scout, "drive-car", "service-drive"); int parked = r.Op(Scout).BusyEnd;
            r.Do(Tech, "loop", "entrance"); r.Done(Tech);
            r.Do(Tech, "reroute", "patrol");
            r.Do(Fixer, "call", Inez);
            r.Until(() => !g.GuardAtDoor, T(120));
            r.Do(Enf, "enter"); r.Done(Enf);
            r.To(Math.Max(g.Tick, parked));
            r.Is(g.CurStand.Id == "service-drive", "the getaway car stands on the service drive");
            r.To(r.SendTo(Enf, g.GetawaySeat)); r.Do(Enf, "board", "car"); r.Done(Enf); if (!g.Over) g.Step();
            r.SendTo(Fixer, g.S.Grid.NearestFree(new Pt(7300, -1100)) ?? g.GetawaySeat); r.SendTo(Tech, g.S.Grid.NearestFree(new Pt(7300, -1900)) ?? g.GetawaySeat);
        }

        /// <summary>Route B's yard on the 059 engine to the Take (P59-B): the driver turned (the lookout warned off), the tip at the parked car,
        /// Vale driven round, the Tech drives the getaway car round behind him, the Enforcer takes him in the car. Returns the Take's landing tick.
        /// scoutTo/fixerTo: where the Scout and the Fixer wait in the yard (Motive's probe plans: (73, -19) and (73, -11)) from 5:55.</summary>
        static int YardB(Run r, bool tip = true)
        {
            var g = r.G;
            Open059(r);
            r.Until(() => Mem(r, 4).Mode == CrewMode.Gone || g.Over, T(330));
            if (tip) Tip059(r);
            r.To(Math.Max(g.Tick, T(355)));
            r.SendTo(Scout, g.S.Grid.NearestFree(new Pt(7300, -1900))!.Value); r.SendTo(Fixer, g.S.Grid.NearestFree(new Pt(7300, -1100))!.Value);
            r.To(T(370) + T(g.R.ValeInS));
            r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick, T(60));
            int there = DriveRound(r);
            r.Do(Enf, "take"); int takeEnd = r.Op(Enf).BusyEnd;
            r.To(Math.Max(there, takeEnd));
            return takeEnd;
        }

        /// <summary>A takedown on a member of the crew by real orders: the Enforcer walks to free ground within reach of him, outside his facing
        /// cone (so he is not aware) and, quiet, outside every crewmate's cone with its near ring (so nobody sees it), else inside `seenBy`'s;
        /// waits (up to `limit`) for the beats to give such a spot, and for no civilian or watcher to see it there (quiet only). True when ordered.</summary>
        static bool CrewTakedownAt(Run r, string id, string how, int limit, string seenBy = "")
        {
            var g = r.G; var m = g.CrewOf(id)!; int end = g.Tick + limit; var why = new SortedDictionary<string, int>(StringComparer.Ordinal); void Miss(string w) { why[w] = why.TryGetValue(w, out var c) ? c + 1 : 1; }
            bool sees(CrewMember x, Pt p, bool nearRing) { var ps = g.MemberPose(x); return ps.HasValue && G.InCone(ps.Value.pos, ps.Value.facing, g.R.RivReachCm, g.R.RivHalfCm, nearRing ? g.R.NearCm : 0, p) && g.S.Sight(ps.Value.pos, p, g.R.Eye); }
            bool ok(Pt p)
            {
                var mp = g.MemberPose(m); if (!mp.HasValue || !G.Within(mp.Value.pos, p, g.R.TakedownReachCm - 20) || !g.S.Grid.Free(p)) { Miss("reach"); return false; }
                if (sees(m, p, false)) { Miss("he sees"); return false; }
                foreach (var x in g.Crew)
                {
                    if (x == m || !x.OnFoot || x.Gun.Down || x.Fight) continue;
                    bool s = sees(x, p, true) && G.Within(g.MemberPose(x)!.Value.pos, p, x.Gun.W!.MaxCm);
                    if (x.Id == seenBy ? !s : s) { Miss(x.Id + (s ? " sees" : " does not see")); return false; }
                }
                if (seenBy.Length == 0) { var q = g.QuoteTakedown(id, how); if (q.People.Count > 0) { Miss("people " + string.Join("+", q.People.Select(x => x.id))); return false; } var ws = q.Seen.SeenBy.Concat(q.Seen.MaybeSeenBy).Where(w => !w.StartsWith("rival-", StringComparison.Ordinal)).ToList(); if (ws.Count > 0) { Miss("watchers " + string.Join("+", ws)); return false; } } // the crew's own eyes are read above (a rival reports nothing)
                return true;
            }
            while (!g.Over && g.Tick < end)
            {
                var mp = g.MemberPose(m); if (!mp.HasValue) return false;
                var e = r.Op(Enf);
                if (G.Within(e.Pos, mp.Value.pos, g.R.TakedownReachCm - 20) && e.Walk == null && ok(e.Pos))
                    return r.Takedown(id, how);
                if (e.Walk == null)
                {
                    Pt? best = null; long bd = long.MaxValue;
                    for (int k = 0; k < 16; k++)
                    {
                        double a = Math.PI * 2 * k / 16;
                        var q = g.S.Grid.NearestFree(mp.Value.pos + new Pt((int)(Math.Cos(a) * 110), (int)(Math.Sin(a) * 110)));
                        if (!q.HasValue || !ok(q.Value) || g.S.Grid.Route(e.Pos, q.Value) == null) continue;
                        long d = G.D2(e.Pos, q.Value); if (d < bd) { bd = d; best = q; }
                    }
                    if (best.HasValue) g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", Point = best.Value });
                    else if (!G.Within(e.Pos, mp.Value.pos, 600)) { var near = g.S.Grid.NearestFree(mp.Value.pos + new Pt(0, 400)); if (near.HasValue) g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", Point = near.Value }); }
                }
                g.Step();
            }
            return r.Is(false, $"a spot to take {id} down ({how}{(seenBy.Length > 0 ? ", in " + seenBy + "'s sight" : ", unseen")}) came within {limit / 10} s (why not: {string.Join("; ", why.OrderByDescending(kv => kv.Value).Take(6).Select(kv => kv.Key + " ×" + kv.Value))})");
        }

        /// <summary>The cell's locks (Motive M059-B7; his probe plans' focus fire): each of `ops` fires on the first of `ids` still up, retrying
        /// every 0.5 s until taken; the next when it is down. One tick of it; the caller steps.</summary>
        static void Locks(Run r, string[] ops, string[] ids)
        {
            var g = r.G;
            if (g.Tick % 5 != 0) return;
            string t = ids.FirstOrDefault(id => g.GunOf(id) is Gun gn && !gn.Down && g.Hostiles.Exists(h => h.Id == id && h.Present && h.Gun.St != "break" && h.Gun.St != "gone")) ?? "";
            if (t.Length == 0) return;
            foreach (var op in ops) { var o = r.Op(op); if (o.Down || o.InCar || o.Inside || o.Busy != null || o.Gun.Target == t) continue; g.Issue(new Order { Tick = g.Tick, Op = op, Do = "fire", Target = t }); }
        }

        static void Always059()
        {
            var inez = new Start(); inez.Turned.Add(Inez);

            Add("S1-GATE-EARLY-YARD", "059", "Motive M059-S1.2, S1.3 (Sky's S1 = no): the getaway car at the service drive and Vale aboard before the rival car enters. Go is refused car-held naming car:rivals (with the first step-out's tick once it rides) until the first rider steps out at rival-tail; he is the blocker, and the refusal names him wherever he stands until he is down; then Go is taken, after blocker.out.", r =>
            {
                var g = r.G;
                YardEarly(r);
                if (!r.Is(g.Vale == ValeAt.Car && g.Tick < g.CrewEntry, $"Vale aboard at {Report.Clock(g.Tick)}, before the rival car enters ({Report.Clock(g.CrewEntry)})")) return;
                var a = g.Issue(Go(g));
                r.Eq((a.ok, a.code, a.ids), (false, "car-held", "car:rivals"), "Go is refused: car:rivals holds it before anyone is on the map (the gate)");
                r.Eq(g.GoQuery().ids, "car:rivals", "the Go card reads the same");
                r.Until(() => g.CarSt != CrewCar.Off, T(120));
                r.Wait(1);
                var b = g.Issue(Go(g)); int said = b.tick;
                r.Is(!b.ok && b.ids == "car:rivals" && b.tick > g.Tick, $"while the car rides in, car-held's tick is its first step-out ({(b.tick >= 0 ? Report.Clock(b.tick) : "none")}; {b.why})");
                int joined = -1; string firstOut = "";
                r.Until(() => { if (joined < 0) { var f = g.Crew.FirstOrDefault(m => m.Fight); if (f != null) { joined = g.Tick; firstOut = f.Id; } } return g.Blockers.Count > 0 || g.Over; }, T(60));
                r.Is(joined >= 0 && joined == g.Tick && g.Crew.Count(m => m.Fight) == 1, $"the blocker is picked the tick the first rider steps out ({Report.Clock(g.Tick)}; out {firstOut} at {(joined >= 0 ? Report.Clock(joined) : "-")})");
                r.Eq((g.Blockers.FirstOrDefault(), firstOut), ("rival-1", "rival-1"), "the first out is rival-1 (ties by id) and he is the blocker");
                r.Eq(joined, said, "he steps out at the tick car-held gave");
                r.Is(G.Within(g.Hostiles.Find(h => h.Id == "rival-1")!.Pos(g.Tick), g.S.CarStand(g.S.RivalCarTail), 500), "he stepped out at rival-tail, the stand's rival stop");
                var bl = g.Hostiles.Find(h => h.Blocker)!;
                bool far = false; int fromT = g.Tick;
                while (!g.Over && !bl.Down && g.Tick < fromT + T(60))
                {
                    if (g.Tick % 5 == 0)
                    {
                        var c = g.Issue(Go(g));
                        if (c.ok) { r.Is(false, $"Go taken at {Report.Clock(g.Tick)} with the blocker up"); return; }
                        if (!c.ids.Split(',').Contains(bl.Id)) { r.Is(false, $"the refusal names the blocker ({c.ids})"); return; }
                        if (!G.Within(bl.Pos(g.Tick), g.GetawayCarPos(), g.R.HoldCm) && !g.S.InExitLane(bl.Pos(g.Tick), g.CurStand)) far = true;
                    }
                    Locks(r, new[] { Fixer, Tech }, new[] { bl.Id });
                    g.Step();
                }
                r.Is(far, "he holds the car from beyond the hold and outside the lane too (wherever he stands, M059-S1.2(1))");
                r.Is(bl.Down, $"the Fixer and the Tech down the blocker ({Report.Clock(g.Tick)}; {g.Result} {g.FailCode})");
                r.Until(() => LogTick(g, "blocker.out") >= 0 || g.Over, T(5));
                int outAt = LogTick(g, "blocker.out");
                r.Until(() => g.GoQuery().ok || g.Over, T(30));
                var d = g.Issue(Go(g));
                r.Is(d.ok && g.Result == Outcome.Success && outAt >= 0 && g.Tick >= outAt, $"then Go is taken at {Report.Clock(g.Tick)}, at or after blocker.out ({(outAt >= 0 ? Report.Clock(outAt) : "-")}): {d.why}");
            }, inez);

            Add("S1-GATE-SWITCH-OFF", "059", "Motive M059-S1.1: fight-always 0 gives back engine 0.32.0's pick. The S1-GATE-EARLY-YARD start: Go is taken at Vale-aboard, before the crew enters, and the ambush never springs (the leak).", r =>
            {
                if (!r.Is(r.Inputs != null, "inputs")) return;
                var alt = Alt(r, AlwaysOff(r.Inputs!));
                YardEarly(alt); foreach (var c in alt.Checks) r.Is(c.ok, "[fight-always 0] " + c.what);
                var g = alt.G;
                r.Is(!g.R.FightAlways, "played on the rulebook with fight-always 0");
                var a = g.Issue(Go(g));
                r.Is(a.ok && g.Result == Outcome.Success && g.Tick < g.CrewEntry, $"Go is taken at Vale-aboard ({Report.Clock(g.Tick)}, the crew enters {Report.Clock(g.CrewEntry)}): {a.why}");
                r.Is(g.FightBranch == "safe" && g.RivalPoses().members.Count == 0 && r.Shots().Count == 0, "the safe fight started, and the cell left with nobody of the crew on the map and no shot fired: unfought");
            }, inez);

            Add("S1-GATE-LOUD-YARD", "059", "Motive M059-S1.2 (M059-5.4's race): route B taken at Alert 2 (the pickup camera shot as Vale's car parks), the in-car Take, the carry, Board. Go is refused car-held while the police blocker is up, wherever he stands: the officers' 7.5 s against the cell's 6.5 s is no race now.", r =>
            {
                var g = r.G;
                Open059(r); Tip059(r);
                r.To(Math.Max(g.Tick, T(370) + T(g.R.ValeInS))); r.Until(() => g.PickupService || g.Over, T(60));
                r.Do(Fixer, "draw"); r.Wait(g.DrawT + 1); r.Fire(Fixer, "camera:pickup"); r.Until(() => g.CameraDark("pickup") || g.Over, T(20));
                r.Is(g.Gunfire > 0 && g.Alert >= 2, $"a shot: Alert {g.Alert}, the run is loud");
                r.Do(Fixer, "holster");
                if (!r.Is(!g.Over && g.PickupService, $"Vale's car parks round the back ({g.Result} {g.FailCode})")) return;
                int there = DriveRound(r);
                r.Do(Enf, "take"); r.To(Math.Max(there, r.Op(Enf).BusyEnd));
                if (!r.Is(g.FightBranch == "loud" && g.CurStand.Id == "service-drive", $"the loud fight at the service drive ({g.FightBranch} {g.Result} {g.FailCode})")) return;
                r.To(Math.Max(g.Tick, r.SendTo(Enf, g.GetawaySeat))); if (!g.Over) { r.Do(Enf, "board", "car"); r.Done(Enf); g.Step(); }
                if (!r.Is(g.Vale == ValeAt.Car && !g.Over, $"Vale aboard ({Report.Clock(g.Tick)}; {g.Result} {g.FailCode})")) return;
                r.Is(g.Blockers.Count == 1 && g.Blockers[0].StartsWith("officer-", StringComparison.Ordinal), $"the loud blocker is an officer ({string.Join(",", g.Blockers)})");
                var bl = g.Hostiles.Find(h => h.Blocker);
                int held = 0, far = 0;
                for (int k = 0; k < 10 && !g.Over && bl != null && !bl.Down; k++)
                {
                    var a = g.Issue(Go(g)); if (a.ok) break;
                    if (a.ids.Split(',').Contains(bl.Id)) held++;
                    if (!G.Within(bl.Pos(g.Tick), g.GetawayCarPos(), g.R.HoldCm) && !g.S.InExitLane(bl.Pos(g.Tick), g.CurStand)) far++;
                    r.Wait(T(1));
                }
                r.Is(held > 0 && (bl == null || bl.Down || held == 10 || g.Over), $"Go is refused naming the blocker {bl?.Id} each second while he is up ({held} of 10 s)");
                r.Line(far > 0, $"seconds he held it from beyond the hold and outside the lane: {far} (the gate holds wherever he is; with him in the lane by Board, the race is gone either way)");
            });

            var early = new Start(); early.Turned.Add(Inez);
            Add("S1-SPRING-LATE", "059", "Motive M059-S1.3: the cell has Vale before the crew enters; the Enforcer brings him within fight-ambush-m of the car at the square with nobody of the crew on foot: the ambush springs and no blocker is picked. At the first step-out (rival-1 at rival-drop, ties by id) he is picked among those on foot, and each later arrival fires weapons.aimNew after he steps out.", r =>
            {
                var g = r.G;
                int at = Max(r.Send(Tech, "cabinet"), r.Send(Enf, "d:Office entrance"));
                r.To(at);
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.Do(Tech, "reroute", "patrol");
                r.Do(Fixer, "call", Inez);
                r.Until(() => !g.GuardAtDoor, T(120));
                r.Do(Enf, "enter"); r.Done(Enf);
                r.SendTo(Enf, g.GetawaySeat);
                r.Until(() => g.FightSprung != Never || g.Over, T(120));
                r.Is(g.FightSprung != Never && g.Tick < g.CrewEntry && g.RivalPoses().members.Count == 0, $"the ambush springs at {Report.Clock(g.Tick)} with nobody of the crew on the map");
                g.Step();
                r.Eq(g.Blockers.Count, 0, "no blocker is picked at the spring (nobody on foot)");
                r.To(r.SendTo(Enf, g.GetawaySeat)); r.Do(Enf, "board", "car"); r.Done(Enf);
                var joins = new Dictionary<string, int>(StringComparer.Ordinal); int firstPick = -1;
                int end = g.Tick + T(120);
                while (!g.Over && g.Tick < end && joins.Count < 5)
                {
                    g.Step();
                    foreach (var m in g.Crew) if (m.Fight && !joins.ContainsKey(m.Id)) { joins[m.Id] = g.Tick; r.Is(m.Gun.NextShot >= g.Tick + g.AimNewT, $"{m.Id} steps out at {Report.Clock(g.Tick)}: his first shot no sooner than aimNew later (next shot {Report.Clock(m.Gun.NextShot)})"); }
                    if (firstPick < 0 && g.Blockers.Count > 0) firstPick = g.Tick;
                }
                var first = joins.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).FirstOrDefault();
                r.Is(first.Key == "rival-1" && firstPick == first.Value && g.Blockers.FirstOrDefault() == "rival-1", $"the blocker is picked at the first step-out: {string.Join(",", g.Blockers)} at {(firstPick >= 0 ? Report.Clock(firstPick) : "-")} (first out {first.Key} {(first.Value > 0 ? Report.Clock(first.Value) : "-")})");
                foreach (var s in r.Shots()) if (joins.TryGetValue(s.by, out var j) && s.tick < j + g.AimNewT) { r.Is(false, $"{s.by} fired at {Report.Clock(s.tick)}, sooner than aimNew after he stepped out"); break; }
            }, early);

            Add("S1-FLOOR-TIP", "059", "Motive M059-S1.4 (rivals-floor 3): route B, the tip and the warned lookout, P1 gone (Start.CrewGone): at the safe fight's start two posted are up, so the tipped car cuts its trip short at once and rides to rival-tail; the wheelman steps out there about 19.8 s later (the entry to rival-tail, then rivals.out). Count 3; the lookout is not recalled.", r =>
            {
                var g = r.G;
                YardB(r);
                if (!r.Is(g.FightBranch == "safe" && g.CurStand.Id == "service-drive", $"the safe fight at the service drive ({g.FightBranch} {g.Result} {g.FailCode})")) return;
                int start = g.FightStart;
                r.Is(g.Log.Exists(l => l.Code == "rivals.tip-leaves") && g.Log.Count(l => l.Code == "rivals.recalled" && l.Tick == start) == 1, "the car was away on the tip at the fight's start, and one away member is recalled (rivals.recalled), not both");
                r.Is(g.FightWhy.Contains("rivals-recalled"), "fight why: rivals-recalled");
                // the earliest at the stand first (ties by id): the car from the entry to rival-tail, then rivals.out; the lookout's walk from his exit to his spot
                var st = g.CurStand; long carT = g.CarProfileTicks(g.S.RivalCarEntry, st.RivalStop) + T(g.R.Crew.OutS);
                var spot4 = g.S.Stand(st.Ambush[3]); var wr = g.S.Grid.Route(g.S.Grid.NearestFree(g.S.Stand(g.S.RivalLookoutExit))!.Value, g.S.Grid.NearestFree(spot4)!.Value); long lkT = wr == null ? long.MaxValue : new Path(wr).Length / g.R.RivalsSpeed;
                string first = lkT < carT ? "rival-4" : "rival-5";
                r.Line(true, $"the wheelman by car {Sec1(carT / 10.0)} s (entry to rival-tail + rivals.out; Motive 19.8 s), the lookout's walk from rival-lookout-exit to his spot {st.Ambush[3]} {Sec1(lkT / 10.0)} s (Motive: 21.7 s to rival-tail)");
                r.Eq(string.Join(",", RivalBodies(g)), first == "rival-4" ? "rival-2,rival-3,rival-4" : "rival-2,rival-3,rival-5", $"count 3: the two posted and the earliest away, {first}");
                if (first == "rival-5")
                {
                    r.Is(g.CarSt == CrewCar.ToStand && g.Crew[3].Mode == CrewMode.Gone, "the tipped car rides for the stand at once; the lookout stays gone");
                    r.Until(() => g.Crew[4].Fight || g.Over, T(60));
                    r.Is(g.Crew[4].Fight && Math.Abs(g.Tick - start - carT) <= 2, $"the wheelman steps out {Sec1((g.Tick - start) / 10.0)} s after the fight's start, at rival-tail");
                }
                else r.Is(g.Crew[3].Fight && g.Crew[3].H!.Run != null && G.Within(g.Crew[3].H!.Run!.Points[0], g.S.Stand(g.S.RivalLookoutExit), 200) && g.CarSt == CrewCar.Away, "the lookout walks back from rival-lookout-exit; the car stays away on its tip");
            }, Gone(new Start(), "rival-1"));

            Add("S1-FLOOR-LOOKOUT", "059", "Motive M059-S1.4: route A at the square, no tip, the driver turned before the mission (his word sends the lookout off), P1 and P2 gone: at the fight's start the wheelman rides in by car (F6, the car parked) and the lookout walks back from rival-lookout-exit. Count 3.", r =>
            {
                var g = r.G;
                RouteA(r);
                if (!r.Is(g.FightBranch == "safe", $"the safe fight ({g.FightBranch} {g.Result} {g.FailCode} at {Report.Clock(g.Tick)})")) return;
                int start = g.FightStart;
                r.Is(g.Crew[3].Fight && g.Crew[3].Mode == CrewMode.Walking && LogTick(g, "rivals.recalled", tx => tx.Contains("lookout")) == start, "the lookout walks back for the fight (rivals.recalled)");
                r.Is(!g.Crew[4].Fight && g.Crew[4].Mode == CrewMode.Aboard && g.CarSt == CrewCar.ToStand, "the wheelman rides in by car for the stand (F6, Parked)");
                r.Eq(string.Join(",", RivalBodies(g)), "rival-3,rival-4,rival-5", "count 3: the posted P3, the lookout, the wheelman's body still aboard");
                r.Is(G.Within(g.Crew[3].H!.Run?.Points[0] ?? default, g.S.Stand(g.S.RivalLookoutExit), 200), "the lookout's walk starts at rival-lookout-exit, where he left the map");
            }, Gone(Turned(Driver), "rival-1", "rival-2"));

            Add("S1-FLOOR-NO-SPAWN", "059", "Motive M059-S1.4: route B, tip and warn, P1-P3 gone: the wheelman (by car) and the lookout (on foot) come back, count 2; no hostile beyond rival-1..rival-5 exists and the fight has no fresh body.", r =>
            {
                var g = r.G;
                YardB(r);
                if (!r.Is(g.FightBranch == "safe", $"the safe fight ({g.FightBranch} {g.Result} {g.FailCode})")) return;
                int start = g.FightStart;
                r.Eq(g.Log.Count(l => l.Code == "rivals.recalled" && l.Tick == start), 2, "both away members are recalled at the start (the car, the lookout)");
                r.Is(g.Crew[3].Fight && g.CarSt == CrewCar.ToStand, "the lookout walks back; the car rides in");
                r.Until(() => g.Crew[4].Fight || g.Over, T(60));
                var ids = g.Hostiles.Select(h => h.Id).ToList();
                r.Is(ids.All(id => id == "rival-4" || id == "rival-5") && ids.Count == 2, $"count 2, and the fight's bodies are rival-4 and rival-5 only ({string.Join(",", ids)})");
                r.Is(g.Crew.Take(3).All(m => m.Gun.Down && !m.Fight), "the three gone stay gone: nothing spawns");
            }, Gone(new Start(), "rival-1", "rival-2", "rival-3"));

            Add("S1-FLOOR-ZERO", "059", "Motive M059-S1.4, S1.5: rivals-floor 0 with S1-FLOOR-NO-SPAWN's start: no recall. With fight-always 1 the crew is destroyed only when no member is able; the wheelman (on the tip) and the lookout (warned off) live, so it is not, and the fight is not over.", r =>
            {
                if (!r.Is(r.Inputs != null, "inputs")) return;
                var alt = Alt(r, FloorZero(r.Inputs!), Gone(new Start(), "rival-1", "rival-2", "rival-3"));
                YardB(alt); foreach (var c in alt.Checks) r.Is(c.ok, "[rivals-floor 0] " + c.what);
                var g = alt.G;
                if (!r.Is(g.FightBranch == "safe", $"the safe fight ({g.FightBranch} {g.Result} {g.FailCode})")) return;
                r.Is(g.R.RivalsFloor == 0 && LogTick(g, "rivals.recalled") < 0, "no recall with rivals-floor 0");
                r.Is(!g.FightWhy.Contains("crew-destroyed") && g.FightOverTick == Never && LogTick(g, "rivals.wiped") < 0, "not destroyed: the wheelman and the lookout are alive");
                r.Is(g.Hostiles.Count(h => h.Id.StartsWith("rival-", StringComparison.Ordinal)) == 0, "no body in the fight");
            }, Gone(new Start(), "rival-1", "rival-2", "rival-3"));

            Add("S1-CREW-WIPED", "059", "Motive M059-S1.5: all five gone (Start.CrewGone; the car never comes, nobody drives it). The cell has Vale early at the square: the safe fight is over at its start (rivals.wiped), the gate is open, Go is taken, and the debrief's fight why reads crew-destroyed.", r =>
            {
                var g = r.G;
                int at = Max(r.Send(Tech, "cabinet"), r.Send(Enf, "d:Office entrance"));
                r.To(at);
                r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.Do(Tech, "reroute", "patrol");
                r.Do(Fixer, "call", Inez);
                r.Until(() => !g.GuardAtDoor, T(120));
                r.Do(Enf, "enter"); r.Done(Enf);
                r.Is(g.FightBranch == "safe" && g.FightOverTick == g.FightStart && LogTick(g, "rivals.wiped") == g.FightStart, $"the safe fight is over at its start ({Report.Clock(g.FightStart)}): the crew is destroyed");
                r.Is(g.FightWhy.Contains("crew-destroyed") && g.Chain.Contains("rivals-destroyed"), "fight why crew-destroyed");
                r.Extract();
                r.Is(g.Result == Outcome.Success, $"the gate is open: Go is taken ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)})");
                var d = Debrief.Of(g);
                r.Is(d.FightWhy.Contains("crew-destroyed"), "the debrief carries crew-destroyed");
            }, Gone(Turned(Inez), "rival-1", "rival-2", "rival-3", "rival-4", "rival-5"));

            Add("S1-SEE-TAKEDOWN", "059", "Motive M059-S1.6: rival-1 at ambush-3 knocked out where rival-2 at ambush-1 (2.3 m off) sees it (his cone with its near ring, a clear line, his pistol's max ring), but rival-1 does not see the Enforcer coming. rival-2 counts the Enforcer a foe (ai.intent crewmate) and fires: gunfire, Alert at least 2, so the final fight would be loud.", r =>
            {
                var g = r.G;
                r.Until(() => Mem(r, 1).Mode == CrewMode.Posted && Mem(r, 2).Mode == CrewMode.Posted, T(240));
                r.Is(g.S.Stands[0].Ambush[Mem(r, 1).Spot] == "ambush-3" && g.S.Stands[0].Ambush[Mem(r, 2).Spot] == "ambush-1", "rival-1 at ambush-3, rival-2 at ambush-1");
                if (!CrewTakedownAt(r, "rival-1", "knockout", T(120), seenBy: "rival-2")) return;
                int td = g.Tick;
                r.Until(() => LogTick(g, "rivals.see-takedown") >= 0 || g.Over, T(8));
                r.Is(LogTick(g, "rivals.see-takedown") >= td && Mem(r, 2).Avenge.Contains("enforcer"), "rival-2 sees it (rivals.see-takedown) and counts the Enforcer a foe");
                r.Until(() => r.Shots().Exists(s => s.by == "rival-2") || g.Over, T(15));
                r.Is(r.Shots().Exists(s => s.by == "rival-2" && s.target == "enforcer"), "rival-2 fires on the Enforcer");
                r.Is(g.FireCauses("rival-2", OpId.Enforcer).Contains("crewmate"), "his cause: crewmate");
                r.Is(g.Alert >= 2, $"gunfire before the final fight: Alert {g.Alert}, so the final fight is loud (StartFight: a shot or Alert 2)");
            });

            // S1-FLOOR-WAKE plays on the rulebook with wake-s 30 (so the takedown at 3:2x is woken by the fight route A gives): what it tests is that a
            // member up again by the fight's start counts for the floor; with 120 s the cell would have to enter after he wakes and before Vale leaves
            void Wake(Run r)
            {
                var g = r.G;
                r.To(Max(r.Send(Scout, "d:Courtyard"), r.Send(Tech, "cabinet")));
                r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez);
                r.Send(Fixer, "d:Courtyard"); r.SendTo(Enf, g.S.Grid.NearestFree(new Pt(-575, -2775))!.Value); // 10 m north of ambush-2, off the square's way
                r.Done(Scout); r.SendTo(Scout, TipSpot(r)); HelpUntilTurned(r, Inez);
                r.Until(() => Mem(r, 3).Mode == CrewMode.Posted || g.Over, T(120));
                if (!CrewTakedownAt(r, "rival-3", "knockout", T(60))) return;
                r.Done(Enf);
                int down = g.Tick;
                r.Is(Mem(r, 3).Gun.Down && g.BodyById("rival-3") != null && !g.Acts.Last().Kill, $"rival-3 is out at {Report.Clock(down)} (a body, brief 013)");
                r.Send(Enf, "d:Office entrance"); // away from him before he wakes: he would shoot the man who took him down on sight (M059-3.4)
                Tip059(r);
                ToSweepAtDoor(r); // engine 0.34.0 (brief 067 A): the yard sweep never sees the door, so no wait
                r.Do(Tech, "loop", "entrance"); r.Do(Fixer, "call", Inez);
                ToEntryOpening(r, walkDone: true);
                r.Do(Enf, "enter"); r.Done(Enf);
                r.Is(LogTick(g, "rivals.see-takedown") < 0, "no crewmate saw the takedown");
                if (!r.Is(g.FightBranch == "safe", $"the safe fight ({g.FightBranch} {g.Result} {g.FailCode} at {Report.Clock(g.Tick)}, Alert {g.Alert})")) return;
                int wake = LogTick(g, "rivals.wakes");
                r.Is(wake >= down + T(g.R.WakeS) - 1 && wake < g.FightStart, $"he woke at {(wake >= 0 ? Report.Clock(wake) : "-")}, wake-s ({g.R.WakeS} s) after the takedown and before the fight ({Report.Clock(g.FightStart)})");
                r.Is(Mem(r, 3).Avenge.Contains("enforcer"), "the Enforcer is his foe (M059-3.4)");
                r.Is(RivalBodies(g).Contains("rival-1") && RivalBodies(g).Contains("rival-2") && RivalBodies(g).Contains("rival-3"), $"the posted three are the fight's bodies ({string.Join(",", RivalBodies(g))})");
                r.Is(LogTick(g, "rivals.recalled") < 0 && g.Crew[3].Mode == CrewMode.Gone && g.CarSt == CrewCar.Away, "no recall is made: the lookout stays gone and the car away (the floor is met)");
            }
            Add("S1-FLOOR-WAKE", "059", "Motive M059-S1.4, M059-3.4: route A at the square with the driver turned before the mission (the lookout warned off) and the tip given; rival-3 knocked out unseen at his post (ambush-2), wake-s before the fight. He wakes, gets up where he fell and posts again, the Enforcer his foe; at the fight's start the posted three are up, so no recall is made. Played on the rulebook with wake-s 30 (the scenario's timing; the rule reads the value).", r =>
            {
                if (!r.Is(r.Inputs != null, "inputs")) return;
                var alt = Alt(r, With059(r.Inputs!, "\"wake-s\": \\{ \"v\": 120,", "\"wake-s\": { \"v\": 30,"));
                Wake(alt); foreach (var c in alt.Checks) r.Is(c.ok, "[wake-s 30] " + c.what);
            }, Turned(Driver));

            // the crew as people for the body orders (Motive M059-3.1, 3.4): a knocked-out member tied, carried, stashed; freed by a crewmate
            void KoRival3(Run r)
            {
                var g = r.G;
                r.SendTo(Enf, g.S.Grid.NearestFree(new Pt(-575, -2775))!.Value);
                r.Until(() => Mem(r, 3).Mode == CrewMode.Posted || g.Over, T(260));
                if (CrewTakedownAt(r, "rival-3", "knockout", T(90))) r.Done(Enf);
            }
            Add("CREW-BODY", "059", "Motive M059-3.1, 3.4 (engine 0.33.0, the crew as people): rival-3 knocked out unseen at ambush-2; the Enforcer ties him (restrain), carries him to the café's kitchen door and stashes him. Tied and hidden he stays out past wake-s; the host's RivalPoses no longer lists him; the takedown is a brief-013 act and body.", r =>
            {
                var g = r.G;
                KoRival3(r);
                var b = g.BodyById("rival-3");
                if (!r.Is(b != null && Mem(r, 3).Gun.Down && !b!.Dead, $"rival-3 is a body, knocked out ({Report.Clock(g.Tick)})")) return;
                int down = g.Tick;
                r.Is(g.Acts.Exists(a => a.Victim == "rival-3" && a.How == "knockout"), "one takedown act on him (brief 013)");
                r.Is(r.Body(Enf, "restrain", "rival-3"), "the Enforcer ties him"); r.Done(Enf);
                r.Is(b!.Restrained, "he is tied");
                r.Is(r.Body(Enf, "carry", "rival-3"), "and lifts him"); r.Done(Enf);
                var st = g.S.Stashes.Find(x => x.Id == "stash/cafe-kitchen-door/door")!;
                r.To(Math.Max(g.Tick, r.SendTo(Enf, st.Stand)));
                r.Is(r.Body(Enf, "stash", st.Id), "and stashes him at the café's kitchen door"); r.Done(Enf);
                r.Is(b.Stash == st.Id && !g.RivalPoses().members.Exists(x => x.id == "rival-3"), "hidden: RivalPoses does not list him");
                r.To(down + T(g.R.WakeS) + T(15));
                r.Is(Mem(r, 3).Gun.Down && g.BodyById("rival-3") != null && LogTick(g, "rivals.wakes") < 0, "tied in the stash he stays out past wake-s");
                var q = g.QuoteTakedown("rival-1", "kill");
                r.Is(q.Armed && q.IfNeverFound == "" && q.TrailIfMissing == 0, "the card on a rival: armed, never missed, nothing if never found (M059-3.2)");
            });
            Add("CREW-FREE", "059", "Motive M059-3.4: rival-3 knocked out unseen and tied; the Enforcer carries him to rival-1's post and lays him there: rival-1, within rivals.free of him, unties him in free-s (rivals.frees); at wake-s rival-3 comes round, the Enforcer his foe, and goes back to his post. With the crowd off (Start.Crowd false), so no passer-by finds him first and unties him (a finder does, as for anyone).", r =>
            {
                var g = r.G;
                KoRival3(r);
                var b = g.BodyById("rival-3");
                if (!r.Is(b != null, $"rival-3 is a body ({Report.Clock(g.Tick)})")) return;
                int down = g.Tick;
                r.Body(Enf, "restrain", "rival-3"); r.Done(Enf);
                r.Body(Enf, "carry", "rival-3"); r.Done(Enf);
                var p1 = g.MemberPose(Mem(r, 1))!.Value.pos;
                var at = g.S.Grid.NearestFree(p1 + new Pt(0, 100)) ?? p1;
                r.To(Math.Max(g.Tick, r.SendTo(Enf, at)));
                r.Body(Enf, "drop"); r.Done(Enf);
                r.Is(G.Within(g.BodyPos(b!), p1, g.R.Crew.FreeCm), $"he lies {G.Dist(g.BodyPos(b!), p1)} cm from rival-1 (rivals.free {g.R.Crew.FreeCm} cm)");
                r.Until(() => !b!.Restrained || g.Over, T(10));
                r.Is(!b!.Restrained && LogTick(g, "rivals.frees") >= 0, "rival-1 unties him (rivals.frees)");
                r.Until(() => !Mem(r, 3).Gun.Down || g.Over, Math.Max(1, down + T(g.R.WakeS) + 5 - g.Tick));
                r.Is(!Mem(r, 3).Gun.Down && LogTick(g, "rivals.wakes") >= down + T(g.R.WakeS) - 1 && Mem(r, 3).Avenge.Contains("enforcer"), $"he comes round at {Report.Clock(g.Tick)}, the Enforcer his foe");
                r.Is(Mem(r, 3).Goal == "spot" || Mem(r, 3).Post == "spot", "and goes back to his post");
            }, new Start { Crowd = false });

            Add("BAL-JOIN-POS", "059", "Motive M059-B1: route A to the safe fight's start: every member on foot becomes a fight body exactly where his routine put him that tick (0 cm), never at (0,0) (engine 0.32.0 read the new body's place before he had one).", r =>
            {
                var g = r.G;
                var want = new Dictionary<string, Pt>(StringComparer.Ordinal);
                RouteAUntilFight(r, want);
                if (!r.Is(g.FightBranch == "safe", $"the safe fight starts ({g.FightBranch} {Report.Clock(g.Tick)})")) return;
                int n = 0;
                foreach (var m in g.Crew)
                {
                    if (!want.TryGetValue(m.Id, out var p) || m.H == null || !m.H.Present) continue;
                    n++;
                    var at = m.H.Pos(g.Tick);
                    r.Is(at == p && at != new Pt(0, 0), $"{m.Id}'s body starts where he stood: {at} (want {p}, {G.Dist(at, p)} cm)");
                }
                r.Is(n >= 3, $"the posted three at least ({n} on foot)");
            });

            // route B at the yard: Motive's rb-ff (fight it out) and rbb-ff (board and go), composed on this engine (tools/sim/fixtures059.ts); their
            // locks are the cell's fire orders: dropped, the play is at will (the cell's panic only); 3.0 s later, a slower player. Engine 0.34.0
            // (brief 067 A): the fixtures are Motive's rb-rr and rbb-rr, the same plays with the Tech's Reroute at 5:00 (the patrol sweeps the yard)
            static List<Order> NoLocks(List<Order> os) => os.Where(o => o.Do != "fire").ToList();
            static List<Order> LateLocks(List<Order> os) => os.Select(o => { if (o.Do == "fire") o.Tick += T(3); return o; }).OrderBy(o => o.Tick).ToList();
            // brief 039 (one input, Motive rulings-0.35.1 §3): at will is F. The edit drops the fixture's locks (its fire orders) and the Scout and
            // the Fixer press F (draw) once: at the spring (the run's own tick, FightSprung) or at the Board (the Enforcer's board order's tick)
            static List<Order> FAt(List<Order> os, int tick)
            {
                var keep = os.Where(o => o.Do != "fire").ToList();
                int at = keep.FindLastIndex(o => o.Tick <= tick) + 1;
                keep.InsertRange(at, new[] { new Order { Tick = tick, Op = Scout, Do = "draw" }, new Order { Tick = tick, Op = Fixer, Do = "draw" } });
                return keep;
            }
            static int BoardTick(List<Order> os) => os.First(o => o.Op == Enf && o.Do == "board").Tick;
            void AtWill(Run r, string fx, bool atBoard)
            {
                var g = r.G; int drew = -1;
                Action? atSpring = atBoard ? null : () =>
                {
                    if (drew >= 0 || g.FightSprung > g.Tick) return;
                    drew = g.Tick;
                    foreach (var op in new[] { Scout, Fixer }) r.Is(g.Issue(new Order { Tick = g.Tick, Op = op, Do = "draw" }).ok, $"{fx}: the {op} presses F at the spring ({Report.Clock(g.Tick)})");
                };
                if (!PlayFixture(r, fx, atBoard ? os => { drew = BoardTick(os); return FAt(os, drew); } : NoLocks, atSpring)) return;
                r.Is(drew >= 0, $"{fx}: the Scout and the Fixer press F {(atBoard ? "at the Board" : "at the spring")}");
            }
            Add("BAL-YARD-AT-WILL", "059", "Motive M059-B7 restated (rulings-0.35.1 §3, one input): route B at the yard (svc-ambush-1 at (77.35, -8.0)), Motive's rb-ff and rbb-ff with their locks dropped, the Scout and the Fixer pressing F once. F at the spring: both hold the carry and win, no focus needed (Motive 7:32.2 and 7:35.4). F only at the Board on board and go (rbb-ff): the rivals walk to the held car with all aboard and take Vale (rivals-reach-vale with rivals.for-car; Motive 7:52.7). Drawing late is still punished.", r =>
            {
                var g = r.G;
                r.Is(g.S.Stand("svc-ambush-1") == new Pt(7735, -800), "svc-ambush-1 at (77.35, -8.0) (Vantage, M059-B4)");
                AtWill(r, "bal-rb-ff", false);
                r.Line(g.Result == Outcome.Success, $"bal-rb-ff, F at the spring, wins ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)}; Motive 7:32.2)");
                if (r.Inputs == null) return;
                var b = Alt(r, r.Inputs); AtWill(b, "bal-rbb-ff", false);
                foreach (var c in b.Checks) r.Is(c.ok, "[rbb-ff F-spring] " + c.what);
                r.Line(b.G.Result == Outcome.Success, $"bal-rbb-ff, F at the spring, wins ({b.G.Result} {b.G.FailCode} at {Report.Clock(b.G.Tick)}; Motive 7:35.4)");
                var c2 = Alt(r, r.Inputs); AtWill(c2, "bal-rbb-ff", true);
                foreach (var c in c2.Checks) r.Is(c.ok, "[rbb-ff F-board] " + c.what);
                var gb = c2.G; int forCar = LogTick(gb, "rivals.for-car");
                r.Is(gb.FailCode == "rivals-reach-vale" && forCar >= 0, $"bal-rbb-ff, F only at the Board, loses: all aboard a held car, the rivals walk to it and take Vale ({gb.Result} {gb.FailCode} at {Report.Clock(gb.Tick)}; rivals.for-car {(forCar >= 0 ? Report.Clock(forCar) : "-")})");
                r.Line(gb.Tick == T(472) + 7, $"bal-rbb-ff, F at the Board: rivals-reach-vale at {Report.Clock(gb.Tick)} (Motive 7:52.7; rivals.for-car 7:43.8)");
            });
            void Focus(Run r, string fx, bool late)
            {
                var g = r.G;
                // late: the plan's Go was composed for the locks on time, so it may come while the car is still held; the player then Goes
                // once it is free (Motive's sweep counts a late lock that keeps Vale aboard and the Enforcer up as holding)
                if (!PlayFixture(r, fx, late ? LateLocks : null, late ? (Action)(() => { if (g.Vale == ValeAt.Car && LogTick(g, "blocker.out") >= 0 && g.Tick % 5 == 0 && g.GoQuery().ok) g.Issue(Go(g)); }) : null)) return;
                if (late && fx == "bal-rbb-ff") // board and go composed for locks on time: the Scout boards and the Fixer falls before the blocker is out
                {
                    // engine 0.34.0 (Motive M067-K3 a, hiding in a held car loses): nobody stands outside the held car, so the rivals walk to it and
                    // take Vale. Restated on 0.35.1 (Motive rulings-0.35.1 §3: the floor and the pin off the cell): the Enforcer stays up; the loss is K3 a
                    r.Is(g.FailCode == "rivals-reach-vale" && g.Log.Exists(l => l.Code == "rivals.for-car") && !r.Op(Enf).Down, $"{fx} with its locks 3.0 s late loses: nobody outside the held car, the rivals walk to it and take Vale, the Enforcer up (M067-K3 a; {g.Result} {g.FailCode} at {Report.Clock(g.Tick)}; Vale {g.Vale}, the Enforcer {(r.Op(Enf).Down ? "down" : "up")})");
                    r.Line(g.Tick == T(464) + 4, $"{fx} late: rivals-reach-vale at {Report.Clock(g.Tick)} (Motive 7:44.4)");
                }
                else r.Line(g.Result == Outcome.Success, $"brief 039 drift (B, cover and pin; Motive re-composes): {fx}{(late ? " with its locks 3.0 s late" : "")} wins ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)}, {g.Trail} T, Alert {g.Alert}, Standing {g.Standing.ToString(CultureInfo.InvariantCulture)})");
                r.Line(g.Trail == 4 && g.Alert == 0 && g.Standing == 3, $"{fx}: T {g.Trail}, Alert {g.Alert}, Standing {g.Standing.ToString(CultureInfo.InvariantCulture)} (Motive: 4 T with the Reroute, A 0, Standing +3; rb-rr 7:32.2, rbb-rr 7:36.1: briefs/067/motive-square.md §5.2, vantage.md A)");
            }
            Add("BAL-YARD-FOCUS", "059", "Motive M059-B4, B7: route B at the yard with svc-ambush-1 at (77.35, -8.0); from the spring the Scout and the Fixer lock rival-1 then rival-2 (focus fire): Motive's rb-ff (fight it out) and rbb-ff (board and go) both win (probe 7:32.2 and 7:36.0, 3 T, A 0, Standing +3). Engine 0.34.0 (brief 067 A: the patrol sweeps the service yard): as Motive's rb-rr and rbb-rr, with the Tech's Reroute at 5:00 (away 5:45 to 7:40) and the Enforcer at the service lane from 5:50: 4 T.", r =>
            {
                Focus(r, "bal-rb-ff", false);
                if (r.Inputs == null) return;
                var b = Alt(r, r.Inputs); Focus(b, "bal-rbb-ff", false); foreach (var c in b.Checks) r.Is(c.ok, "[rbb-ff] " + c.what);
            });
            Add("BAL-YARD-FOCUS-LATE", "059", "Motive M059-B4: BAL-YARD-FOCUS with every lock 3.0 s late (a slower player): rb-ff still wins (the player Goes once the car is free); rbb-ff loses (since 0.34.0, restated on 0.35.1 by Motive rulings-0.35.1 §3): nobody stands outside the held car, so the rivals walk to it and take Vale with the Enforcer up (rivals-reach-vale, rivals.for-car; Motive 7:44.4; M067-K3 a).", r =>
            {
                Focus(r, "bal-rb-ff", true);
                if (r.Inputs == null) return;
                var b = Alt(r, r.Inputs); Focus(b, "bal-rbb-ff", true); foreach (var c in b.Checks) r.Is(c.ok, "[rbb-ff] " + c.what);
            });

            // engine 0.35.2 (Motive rulings-0.35.1 §2 and §3's last row, Sky's yes 2026-10-27): the yard plays with their locks dropped and no F at
            // all. 0.35.1: none at 8:30 (the stall: an undrawn cell by a held car with Vale aboard was a guard, never shot, never pressed)
            void NeverDrawn(Run r, string fx)
            {
                var g = r.G;
                if (!PlayFixture(r, fx, NoLocks)) return;
                r.Is(!g.Log.Exists(l => l.Code == "order.draw"), $"{fx}: nobody of the cell draws");
                int forCar = LogTick(g, "rivals.for-car");
                r.Is(g.FailCode == "rivals-reach-vale" && forCar >= 0, $"{fx}, never drawn, loses: the rivals walk to the held car past the undrawn cell and take Vale ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)}; rivals.for-car {(forCar >= 0 ? Report.Clock(forCar) : "-")})");
                r.Is(g.Tick < T(g.ClockOver), $"{fx}: an outcome before the close, no stall ({Report.Clock(g.Tick)})");
            }
            Add("BAL-YARD-NEVER-DRAWN", "059", "Engine 0.35.2 (Motive rulings-0.35.1 §2, Sky's yes 2026-10-27: only a drawn agent guards a held car): route B at the yard, Motive's rb-ff and rbb-ff with their locks dropped and nobody pressing F. An undrawn agent standing by is no guard: the rivals walk to the held car with Vale aboard and take him (rivals-reach-vale with rivals.for-car). 0.35.1: none at 8:30, the run idled to the close.", r =>
            {
                NeverDrawn(r, "bal-rb-ff");
                if (r.Inputs == null) return;
                var b = Alt(r, r.Inputs); NeverDrawn(b, "bal-rbb-ff"); foreach (var c in b.Checks) r.Is(c.ok, "[rbb-ff] " + c.what);
                r.Line(true, $"bal-rb-ff never drawn: {r.G.FailCode} at {Report.Clock(r.G.Tick)}; bal-rbb-ff: {b.G.FailCode} at {Report.Clock(b.G.Tick)}");
            });

            Add("BAL-YARD-POST", "059", "Motive M059-B9 (the build as it stands): in rb-ff's run the yard's blocker is svc-ambush-1's man; he walks about 9.16 m to the open post (86, -5) and is there within the carry and Board from Vale's car (the Enforcer's carry at the cell's pace plus Board carrying, read from the rules and the run).", r =>
            {
                var g = r.G;
                int spring = -1, take = -1; Pt from = default; long walk = 0; Hostile? bl = null; int atPost = -1;
                if (!PlayFixture(r, "bal-rb-ff", null, () =>
                {
                    if (take < 0 && g.Vale == ValeAt.WithEnforcer) { take = g.Tick; from = r.Op(Enf).Pos; }
                    if (spring < 0 && g.Blockers.Count > 0) { spring = g.FightSprung; bl = g.Hostiles.Find(h => h.Blocker); walk = bl?.Run?.Length ?? 0; }
                    if (bl != null && atPost < 0 && !bl.Moving(g.Tick)) atPost = g.Tick;
                })) return;
                if (!r.Is(bl != null && spring >= 0, "a blocker is picked at the spring")) return;
                var m = g.CrewOf(bl!.Id)!;
                r.Eq(g.S.Stands[1].Ambush[m.Spot], "svc-ambush-1", $"the blocker ({bl.Id}) is the man at svc-ambush-1");
                r.Is(G.Within(bl.Home, new Pt(8600, -500), 100), $"his post is the lane's open point (86, -5): {bl.Home}");
                r.Line(Math.Abs(walk - 9160) <= 300, $"his walk is {Sec1(walk / 1000.0)} m (Vantage measured 9.16 m)");
                long carry = G.Dist(from, g.S.Stand(g.S.Stands[1].Car));
                int limit = (int)((carry * Game.Hz + g.R.CellSpeed - 1) / g.R.CellSpeed) + T(g.R.BoardS + g.R.CarryS);
                r.Is(atPost >= 0 && atPost - spring <= limit, $"he is at his post {Sec1((atPost - spring) / 10.0)} s after the spring, within the carry and Board ({Sec1(limit / 10.0)} s: {Sec1(carry / 100.0)} m at {g.R.CellSpeed} cm/s + Board carrying {g.R.BoardS + g.R.CarryS} s)");
                r.Line(atPost - spring <= T(6) + 5, $"and within Motive's 6.5 s (the brief's 3.5 m carry): {Sec1((atPost - spring) / 10.0)} s");
            });

            // the loud fight on the drawn crew: Motive's loud-x4 (briefs/059/motive/balance/probe-plans/loud-x4.plan.json) composed on this engine
            // (tools/sim/fixtures059.ts → Fixtures/bal-loud-x4.script.json): the guard shot at 2:31 draws the crew to the Office front with four on
            // foot; the Enforcer comes out with Vale at the service lane while the Scout and Fixer lock rival-1..3; he walks Vale out on rivals.retreat
            Add("BAL-LOUD-BREAK", "059", "Motive M059-B5 (kept: GDD §06, more than half down): Motive's loud-x4, composed on this engine: the guard shot at 2:31 draws the crew to the Office front with four on foot (the wheelman aboard); the cell downs three of them one by one: no break at the first or the second down, the break (rivals.retreat) at the third; the run wins (Motive: 5:20.1, 27 T, Alert 2, Standing -1).", r =>
            {
                var g = r.G;
                if (!PlayFixture(r, "bal-loud-x4")) return;
                var downs = g.Log.Where(l => l.Code == "rivals.down").Select(l => l.Tick).ToList(); int brk = LogTick(g, "rivals.retreat");
                int stop = LogTick(g, "rivals.drawn-stop");
                r.Is(stop >= 0 && g.Log.Count(l => l.Code == "rivals.joins" || l.Code == "rivals.late") == 0, $"the gunfire drew the crew to the Office front ({(stop >= 0 ? Report.Clock(stop) : "-")})");
                r.Line(downs.Count >= 3, $"brief 039 drift (B, cover and pin; Motive re-composes): three of the drawn crew go down ({downs.Count}; {g.Result} {g.FailCode} at {Report.Clock(g.Tick)})");
                if (downs.Count < 3) return;
                r.Is(brk > downs[1] && brk == downs[2], $"no break at {Report.Clock(downs[0])} (one down) or {Report.Clock(downs[1])} (two of four), the break at the third ({Report.Clock(downs[2])}; rivals.retreat {(brk >= 0 ? Report.Clock(brk) : "-")})");
                r.Is(g.Result == Outcome.Success, $"loud-x4 wins on this engine ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)}, {g.Trail} T, Alert {g.Alert}, Standing {g.Standing.ToString(CultureInfo.InvariantCulture)})");
            });
            Add("BAL-NOPRESS", "059", "Motive M059-B3: in loud-x4's run the drawn crew presses for the Enforcer once he is out with Vale (ai.kite 'presses'), and once it has broken (rivals.retreat) no member presses again: they make for their car.", r =>
            {
                var g = r.G;
                if (!PlayFixture(r, "bal-loud-x4")) return;
                int brk = LogTick(g, "rivals.retreat");
                if (!r.Line(brk >= 0, $"brief 039 drift (B, cover and pin; Motive re-composes): the crew breaks ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)}): the press rule is read only once it does")) { r.Is(true, "loud-x4 played"); return; }
                r.Is(g.Log.Exists(l => l.Tick < brk && l.Code == "ai.kite" && l.Text.Contains("presses")), "before the break the crew presses for the Enforcer holding Vale (the press is live)");
                r.Is(!g.Log.Exists(l => l.Tick >= brk && l.Code == "ai.kite" && l.Text.Contains("presses")), "after the break no member presses");
                r.Is(g.Log.Exists(l => l.Tick > brk && l.Code == "board.in" && l.Text.Contains(g.ValeName)), "the Enforcer walks Vale out after it and boards (the log names him from the cast, brief 089)");
            });

            Add("BAL-LANE", "059", "Motive M059-B2 (GDD §06: the blocker keeps its lane and answers only from it): route A; the Enforcer holds Vale at the service lane, far from the car, while the Scout's shot from the pistol's long band springs the ambush (brief 059: a shot at a lurker gives it away). The rival blocker walks to his lane post; the Fixer then fires on him from the long band, beyond his effective reach: he takes no kite step out of the line and stays on the lane.", r =>
            {
                var g = r.G; var w = g.R.Pistol;
                RouteA(r, reroute: true);
                if (!r.Is(g.FightBranch == "safe" && g.Vale == ValeAt.WithEnforcer, $"the Enforcer has Vale at the service lane ({g.FightBranch} {Report.Clock(g.Tick)})")) return;
                Pt? Band(Pt at, Pt from)
                {
                    Pt? best = null; long bd = long.MaxValue;
                    for (int d = w.EffCm + 40; d <= w.MaxCm - 40; d += 20)
                        for (int k = 0; k < 72; k++)
                        {
                            double a = Math.PI * 2 * k / 72; var q = at + new Pt((int)(Math.Cos(a) * d), (int)(Math.Sin(a) * d));
                            if (!g.S.Grid.Free(q) || G.Within(q, at, w.EffCm + 20) || !G.Within(q, at, w.MaxCm - 20) || !g.S.Sight(q, at, g.R.Eye)) continue;
                            long dd = G.D2(q, from); if (dd < bd && g.S.Grid.Route(from, q) != null) { bd = dd; best = q; }
                        }
                    return best;
                }
                var lurker = g.Hostiles.Where(h => h.Id.StartsWith("rival-", StringComparison.Ordinal) && h.Present).OrderBy(h => h.Id, StringComparer.Ordinal).FirstOrDefault();
                if (!r.Is(lurker != null, "a rival lies in wait at his spot")) return;
                var s1 = Band(lurker!.Pos(g.Tick), r.Op(Scout).Pos);
                if (!r.Is(s1.HasValue, "a spot for the Scout's shot")) return;
                r.To(Math.Max(g.Tick, r.SendTo(Scout, s1!.Value)));
                r.Do(Scout, "draw"); r.Wait(g.DrawT + 1); r.Fire(Scout, lurker.Id);
                r.Until(() => g.FightSprung != Never || g.Over, T(15));
                r.Is(LogTick(g, "fight.ambush", tx => tx.Contains("shot")) >= 0, "the shot springs the ambush");
                r.Until(() => g.Blockers.Count > 0 || g.Over, T(5));
                var bl = g.Hostiles.Find(h => h.Blocker);
                if (!r.Is(bl != null && bl.Id.StartsWith("rival-", StringComparison.Ordinal), $"a rival blocker ({bl?.Id})")) return;
                g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "move", Point = g.S.Stand("crew:scout") }); // the Scout steps back
                r.Until(() => !bl!.Moving(g.Tick) || bl.Down || g.Over, T(30));
                var post = bl!.Pos(g.Tick);
                r.Is(g.S.InExitLane(post, g.CurStand), $"he stands on his lane post {post}");
                var spot = Band(post, r.Op(Fixer).Pos);
                if (!r.Is(spot.HasValue, "a spot beyond his effective reach with a line to him")) return;
                r.To(Math.Max(g.Tick, r.SendTo(Fixer, spot!.Value)));
                if (!r.Is(!g.Over && !r.Op(Fixer).Down && !bl.Down, $"the Fixer at {spot}, {G.Dist(r.Op(Fixer).Pos, bl.Pos(g.Tick)) / 100} m from him ({g.Result} {g.FailCode})")) return;
                int from = g.Tick; r.Do(Fixer, "draw"); r.Wait(g.DrawT + 1);
                r.Fire(Fixer, bl.Id);
                int stop = g.Tick + T(12); bool off = false;
                while (!g.Over && g.Tick < stop && !bl.Down) { if (!g.S.InExitLane(bl.Pos(g.Tick), g.CurStand)) off = true; g.Step(); }
                r.Is(r.Shots().Exists(x => x.by == Fixer && x.target == bl.Id && x.tick >= from), "the Fixer fires on him");
                r.Is(!g.Log.Exists(l => l.Tick >= from && l.Code == "ai.kite" && l.Text.StartsWith(bl.Id + " ", StringComparison.Ordinal) && l.Text.Contains("out of the line")), $"{bl.Id} takes no kite step out of the line");
                r.Is(!off, $"he stays on the lane ({bl.Pos(g.Tick)})");
            });
        }

        /// <summary>Plays an embedded fixture script (tools/sim/fixtures059.ts) on the scenario's run, each order at its tick, to its end. False
        /// (with a check line) when it is missing or does not play here.</summary>
        static bool PlayFixture(Run r, string name, Func<List<Order>, List<Order>>? edit = null, Action? perTick = null)
        {
            Script? s = null; string err = "";
            try
            {
                using var st = typeof(Scenarios).Assembly.GetManifestResourceStream("Suborned.Sim.Fixtures." + name + ".script.json");
                if (st != null) { using var rd = new System.IO.StreamReader(st); s = Suborned.Sim.Script.Parse(rd.ReadToEnd()); } else err = "not embedded";
            }
            catch (Exception e) { err = e.GetType().Name + ": " + e.Message; }
            if (!r.Is(s != null, $"the fixture {name} loads{(err.Length > 0 ? ": " + err : "")} (re-compose: bun tools/sim/fixtures059.ts)")) return false;
            var g = r.G; int refused = 0;
            var orders = edit != null ? edit(s!.Orders.ToList()) : s!.Orders;
            void To(int t) { if (perTick == null) { g.RunTo(t); return; } while (!g.Over && g.Tick < t) { g.Step(); perTick(); } }
            foreach (var o in orders)
            {
                To(o.Tick); if (g.Over) break;
                if (g.OneInputOn && (o.Do == "panic" || o.Do == "stance" || o.Do == "free" || o.Do == "cease")) // brief 039: a fixture composed before one input
                {
                    if (o.Do != "panic") continue; // weapons free is always on: a stance or free order says nothing more (a cease is no fixture's)
                    foreach (var op in g.Ops) // panic is F on every armed agent who can draw (one input)
                        if (!op.Down && !op.Inside && !op.InCar && op.Gun.W != null && !(op.Gun.Drawn && op.Gun.HolsterAt < 0) && !g.Issue(new Order { Tick = g.Tick, Op = Sim.Op.Name(op.Id), Do = "draw" }).ok) refused++;
                    continue;
                }
                if (!g.Issue(o).ok) refused++;
            }
            if (!g.Over) To(s.Until > 0 ? s.Until : T(g.ClockOver));
            r.Line(refused == 0, $"{name}: every order taken where it was composed ({refused} refused; else re-compose: bun tools/sim/fixtures059.ts)");
            return true;
        }

        /// <summary>Route A to the safe fight's start, recording each member's place for the tick about to be played before every step (BAL-JOIN-POS).</summary>
        static void RouteAUntilFight(Run r, Dictionary<string, Pt> want)
        {
            var g = r.G;
            r.To(Max(r.Send(Scout, "d:Courtyard"), r.Send(Tech, "cabinet")));
            r.Do(Scout, "watch", Inez); r.Done(Scout);
            r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez);
            r.Send(Fixer, "d:Courtyard"); r.SendTo(Enf, new Pt(4900, -1700));
            r.Done(Scout); HelpUntilTurned(r, Inez);
            ToSweepAtDoor(r, T(228)); // engine 0.34.0 (brief 067 A): bounded; the yard sweep never sees the door, so it calls at 3:48
            r.Do(Tech, "loop", "entrance");
            r.Do(Fixer, "call", Inez);
            r.To(r.Send(Enf, "d:Office entrance"));
            ToEntryOpening(r); // engine 0.34.0: bounded by the run's end
            r.Do(Enf, "enter");
            while (!g.Over && g.FightBranch.Length == 0)
            {
                want.Clear(); foreach (var m in g.Crew) if (m.OnFoot && !m.Gun.Down && !m.Fight) want[m.Id] = g.MemberPos(m, g.Tick + 1);
                g.Step();
            }
        }
    }
}
