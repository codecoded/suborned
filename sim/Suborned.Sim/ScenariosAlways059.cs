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


            var early = new Start(); early.Turned.Add(Inez);





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
                r.Until(() => Mem(r, 1).Mode == CrewMode.Posted && Mem(r, 2).Mode == CrewMode.Posted && Mem(r, 3).Mode == CrewMode.Posted, T(240));
                // engine 0.41.0 (brief 096): they step out at the block point, so who takes which spot moved; the pair is whoever stands at ambush-3 and ambush-1 (2.3 m apart)
                var at3 = g.Crew.Take(3).FirstOrDefault(m => g.S.Stands[0].Ambush[m.Spot] == "ambush-3"); var at1 = g.Crew.Take(3).FirstOrDefault(m => g.S.Stands[0].Ambush[m.Spot] == "ambush-1");
                if (!r.Is(at3 != null && at1 != null, $"two of the three at ambush-3 and ambush-1 ({at3?.Id}, {at1?.Id})")) return;
                if (!CrewTakedownAt(r, at3!.Id, "knockout", T(120), seenBy: at1!.Id)) return;
                int td = g.Tick;
                r.Until(() => LogTick(g, "rivals.see-takedown") >= 0 || g.Over, T(8));
                r.Is(LogTick(g, "rivals.see-takedown") >= td && at1.Avenge.Contains("enforcer"), $"{at1.Id} sees it (rivals.see-takedown) and counts the Enforcer a foe");
                r.Until(() => r.Shots().Exists(s => s.by == at1.Id) || g.Over, T(15));
                r.Is(r.Shots().Exists(s => s.by == at1.Id && s.target == "enforcer"), $"{at1.Id} fires on the Enforcer");
                r.Is(g.FireCauses(at1.Id, OpId.Enforcer).Contains("crewmate"), "his cause: crewmate");
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
                r.Send(Enf, "d:Office entrance"); // away from him before he wakes: he would shoot the man who took him down on sight (M059-3.4); engine 0.41.0: no tip (retired)
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
                r.Is(LogTick(g, "rivals.recalled") < 0, "no recall is made (the floor is met)");
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
            static List<Order> LateLocks(List<Order> os) => os.Select(o => { if (o.Do == "fire") o.Tick += T(3); return o; }).OrderBy(o => o.Tick).ToList();
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



            // the loud fight on the drawn crew: Motive's loud-x4 (briefs/059/motive/balance/probe-plans/loud-x4.plan.json) composed on this engine
            // (tools/sim/fixtures059.ts → Fixtures/bal-loud-x4.script.json): the guard shot at 2:31 draws the crew to the Office front with four on
            // foot; the Enforcer comes out with Vale at the service lane while the Scout and Fixer lock rival-1..3; he walks Vale out on rivals.retreat
            Add("BAL-LOUD-BREAK", "059", "Motive M059-B5 (kept: GDD §06, more than half down): Motive's loud-x4, composed on this engine: the guard shot at 2:31 draws the crew to the Office front with four on foot (the wheelman aboard); the cell downs three of them one by one: no break at the first or the second down, the break (rivals.retreat) at the third; the run wins (Motive: 5:20.1, 27 T, Alert 2, Standing -1).", r =>
            {
                var g = r.G;
                if (!PlayFixture(r, "bal-loud-x4")) return;
                var downs = g.Log.Where(l => l.Code == "rivals.down").Select(l => l.Tick).ToList(); int brk = LogTick(g, "rivals.retreat");
                int stop = LogTick(g, "rivals.drawn"); // engine 0.41.0 (brief 096): the crew on foot go to the front; the car keeps its block (RIV-21)
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
