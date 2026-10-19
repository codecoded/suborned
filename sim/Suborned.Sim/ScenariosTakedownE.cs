// Brief 013 E (Keel, 2026-10-16; Aster's small items after Waymark's c010): one scenario a change, on the real block and the promoted cast.
//  N79 a tie out of speaking range is told only from a phone: the teller walks there, and a takedown on the way stops it (the driver route, early)
//  N80 the same walk not stopped: the tie is told by phone when the call is made, not at the sight
//  N81 face to face is unchanged (the driver route, late: the driver tells Vale at once); N82 one hop: a told person never retells
//  N83 a fleeing teller phones only from the refuge she reaches (re-cast 2026-10-19: clerks 56 and 58, the 5:30 group)
//  N84 a cancelled Enter with the public door held comes out by the service lane; N85 both held: he waits inside, then out by a free door
//  N87 a search that opens a stash and is sent on to the body it found (no engine error: the stash comes off the list first)
//  N86 the HUD's read-only accessors (Glyph's ask 1): held-door stand-down, stash room, servicing round, carry-path sight; no hash moves
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>Waymark's driver-route opening (out/waymark013 d-part2/d-part3): the Tech loops the pickup camera, the Enforcer waits
        /// beside the official entrance on the camera's blind side, the Scout watches the driver.
        /// Retimed 2026-10-19 (the door on the west face; c022): the cabinet is on the forecourt now, the Tech's run there lands at 0:17.1
        /// (was 0:13.6); the Scout walks to the driver as c022's route B does and watches at 0:57.6 within 6 m (the way past the forecourt is
        /// longer; was to the rivals' park, watching at 0:56.0), then steps back to where she stood.</summary>
        static Order[] DriverOpening() => new[] {
            new Order { Tick = 0, Op = Tech, Do = "move", To = "cabinet", Run = true }, MvTo(0, Enf, new Pt(7625, -2880)), Mv(0, Scout, "d:Meet driver"), Mv(0, Fixer, "crew:fixer"),
            Act(166, Tech, "loop", "pickup"), Act(576, Scout, "watch", Driver), MvTo(776, Scout, new Pt(8250, -3100)) };
        const string Guard29 = "p-records-office-29", Guard74 = "p-records-office-74", Guard65 = "p-records-office-65", Clerk39 = "p-records-office-39";
        static bool ToldBy(Person q, Person by) => q.Memory.Exists(m => m.Act == "told" && m.By == by.Id);

        static void TakedownE()
        {
            Add("N79", "—", "Telling a tie needs a phone (Aster 2026-10-16, brief 013 E1; GDD §01: calls are voice, no networks). Waymark's early driver route: guard 29 killed beside the car at 2:31; the driver sees it and, a Mercenary at Alert 0, keeps quiet. His workmate (clerk 39) is indoors and nobody of his is within speaking range, so he must reach a phone first: the same walk as a report (the official entrance's desk), and the same stop. The Enforcer kills him at 2:37, before the call: nobody is told.", r =>
            {
                var g = r.G;
                Orders(r, DriverOpening().Concat(new[] { Act(1510, Enf, "takedown", Guard29, "kill") }).ToArray());
                var a = ActOn(r, Guard29);
                r.Is(a.Noticed.Contains(g.Driver.Id) && !g.Reps.Exists(x => x.P == g.Driver && x.Kind != "tell"), "the driver saw it and reports nothing");
                var tell = g.Reps.SingleOrDefault(x => x.P == g.Driver && x.Kind == "tell");
                r.Is(tell != null && tell.Phase == "walk" && tell.LandAt == T(162), $"he goes to the official entrance's phone: react, the walk, the call at 2:42 ({(tell == null ? "none" : Report.Clock(tell.LandAt))})");
                var c39 = g.PersonById(Clerk39)!;
                r.Is(!ToldBy(c39, g.Driver) && !g.Log.Exists(l => l.Code == "witness.told"), "nobody is told at the sight");
                Orders(r, MvTo(1540, Enf, new Pt(7690, -3370)), Act(1570, Enf, "takedown", Driver, "kill"));
                r.Is(tell != null && tell.Phase == "stopped", "taken down before the call: the tell is stopped, as a report is");
                r.To(T(180));
                r.Is(!ToldBy(c39, g.Driver) && !g.Log.Exists(l => l.Code == "witness.told"), "clerk 39 is never told");
                r.Is(!g.Acts.Any(x => x.Told.Count > 0), "no act was told to anyone");
            });

            Add("N80", "—", "The same walk, not stopped (brief 013 E1): the driver sees guard 29 killed at 2:31, walks to the official entrance's desk and calls his workmate clerk 39 at 2:42: the told entry lands with the call, not at the sight, and the log says by phone. Clerk 39, a Mercenary, keeps it to himself.", r =>
            {
                var g = r.G;
                Orders(r, DriverOpening().Concat(new[] { Act(1510, Enf, "takedown", Guard29, "kill") }).ToArray());
                var tell = g.Reps.Single(x => x.P == g.Driver && x.Kind == "tell");
                var c39 = g.PersonById(Clerk39)!;
                r.To(tell.LandAt - 1);
                r.Is(!ToldBy(c39, g.Driver), "not told the tick before the call");
                r.To(tell.LandAt);
                r.Is(ToldBy(c39, g.Driver) && tell.Phase == "landed", "told at the call (2:42)");
                r.Is(g.Log.Exists(l => l.Code == "witness.told" && l.Tick == tell.LandAt && l.Text.Contains("by phone")), "by phone");
                r.Is(ActOn(r, Guard29).Told.Contains(Clerk39), "the act holds whom it was told to");
                r.Eq(g.Alert, 0, "a tell is no report: no Alert");
                r.Is(!g.Reps.Exists(x => x.P == c39), "clerk 39 (a Mercenary) does nothing with it");
            });

            Add("N81", "—", "Face to face is unchanged (brief 013 E1): Waymark's late driver route; guard 74 knocked out at 5:55 beside the car. The driver sees it, keeps quiet, and tells Vale at once: Vale is on the street, walking to his car within speaking range (no phone, no walk).", r =>
            {
                var g = r.G;
                Orders(r, DriverOpening().Concat(new[] { Act(1900, Scout, "mislead", "rivals"), Mv(2340, Scout, "crew:scout"), Act(3550, Enf, "takedown", Guard74, "knockout") }).ToArray());
                r.Is(ToldBy(g.ValeP, g.Driver), "Vale is told at the sight");
                r.Is(g.Log.Exists(l => l.Code == "witness.told" && l.Tick == T(355) && l.Text.Contains("councillor") && !l.Text.Contains("by phone")), "face to face, not by phone");
                r.Is(!g.Reps.Exists(x => x.P == g.Driver), "no walk to a phone");
            });

            Add("N82", "—", "One hop (Pulse §3.4; rulebook memory: a told entry is never retold): the late driver route on to 6:30. Vale sees the driver knocked out at 6:04 and tells guard 65 face to face; 65, Loyal, walks to report it and it lands at 6:27. At his phone he calls nobody: what he was told is not his to tell. (Before brief 013 E1 a told reporter told again from the phone.)", r =>
            {
                var g = r.G;
                Orders(r, DriverOpening().Concat(new[] { Act(1900, Scout, "mislead", "rivals"), Mv(2340, Scout, "crew:scout"), Act(3550, Enf, "takedown", Guard74, "knockout"),
                    MvTo(3610, Enf, new Pt(7690, -3370)), Act(3640, Enf, "takedown", Driver, "knockout") }).ToArray());
                var a = ActOn(r, Driver);
                var g65 = g.PersonById(Guard65)!;
                r.Is(a.Told.Contains(Guard65) && ToldBy(g65, g.ValeP), "Vale tells guard 65 of the driver's takedown");
                var rep = g.Reps.FirstOrDefault(x => x.P == g65 && x.Act == a.Id && x.Kind == "down");
                r.Is(rep != null, "guard 65 (Loyal) goes to report what he was told");
                if (rep == null) return;
                r.To(rep.LandAt);
                r.Is(rep.Phase == "landed", $"his report lands ({Report.Clock(rep.LandAt)})");
                r.Is(!g.People.Concat(g.Walkers.Select(w => w.P)).Any(p => ToldBy(p, g65)), "and he tells nobody: one hop");
            });

            // Re-cast 2026-10-19 (Keel, brief 023): Pulse's leave stagger (0..8 min after the chime for an inside post) keeps clerk 32 inside
            // past the close and sends 42 out alone at 6:30, so the pair no longer walks. The intent is the same with the 5:30 group bound for
            // Rocket West: clerk 56 (Fearful) walks beside clerk 58, a stranger to her; her one tie, workmate 54, is inside the Office until after
            // the close, so nobody of hers is within speaking range and she can tell only from a phone.
            Add("N83", "—", "A fleeing teller (brief 013 E1; Pulse §3.4: 'tells if they reach a phone'): clerks 56 (Fearful) and 58 walk the forecourt with the 5:30 group bound for Rocket West; the Enforcer knocks out 58 beside her. She flees (startled, then a run to a refuge). Nobody of hers is within speaking range: her one tie, workmate 54, is inside the Office until after the close, so she tells only if the refuge she reaches is a staffed door with its desk phone: no walk to a phone beyond her flight.", r =>
            {
                var g = r.G;
                var v = WalkerById(r, "p-records-office-58"); var w = WalkerById(r, "p-records-office-56");
                r.To(T(340)); r.To(MeetFromBehind(r, v, T(380)));
                r.Is(r.Until(() => g.CrowdPos(v) is Pt p && G.Within(p, r.Op(Enf).Pos, g.R.TakedownReachCm), T(40)), $"clerk 58 walks into reach ({Report.Clock(g.Tick)})");
                r.Takedown("p-records-office-58", "knockout");
                var a = ActOn(r, "p-records-office-58");
                r.Is(a.Noticed.Contains(w.P.Id) && w.St == "startled", "clerk 56 sees it and is startled (Fearful: flee)");
                var tell = g.Reps.SingleOrDefault(x => x.P == w.P && x.Kind == "tell");
                r.Is(tell != null && tell.Phase == "flee" && tell.LandAt == Game.Never, "her tell waits on her flight (no walk of its own)");
                r.Is(!g.Log.Exists(l => l.Code == "witness.told" && l.Text.StartsWith("p-records-office-56 ", StringComparison.Ordinal)), "nobody is told at the sight");
                if (tell == null) return;
                r.Is(r.Until(() => tell.Phase != "flee", T(120)), $"her flight ends ({Report.Clock(g.Tick)}, at {w.Refuge})");
                bool door = g.S.Refuges.Exists(x => x.id == w.Refuge && x.kind == "door");
                if (door)
                {
                    r.Is(tell.Phase == "call" && tell.LandAt == g.Tick + T(g.R.CallS), $"{w.Refuge} is a staffed door: she calls from its desk");
                    r.To(tell.LandAt);
                    r.Is(g.Log.Exists(l => l.Code == "witness.told" && l.Tick == g.Tick && l.Text.StartsWith("p-records-office-56 ", StringComparison.Ordinal) && l.Text.Contains("by phone")), "a tie is told by phone");
                }
                else
                {
                    r.Is(tell.Phase == "stopped" && g.Log.Exists(l => l.Code == "witness.tell-stopped" && l.Text.Contains("no phone")), $"{w.Refuge} has no phone: nobody is told");
                    r.To(g.Tick + T(30));
                    r.Is(!a.Told.Contains("p-records-office-54"), "and her workmate is never told");
                }
            });

            Add("N84", "—", "A cancelled Enter comes out by a door no unit holds (Aster 2026-10-16, brief 013 E3; rulebook response: no exit through a held door). Waymark's orders (2026-10-19, the west-face door): the guard knocked out unseen at his post at 4:00.1 and left; guard 73 finds him and radios it (4:57.1, landing 5:02.1); the Enforcer goes in at the landing, before the scene unit arrives. The unit holds the public door from 5:12.1. The player cancels the Enter: the Enforcer comes out at the service lane, not through the held public door.", r =>
            {
                var g = r.G;
                Orders(r, Mv(0, Tech, "cabinet", run: true), MvTo(0, Enf, BesideDoor), Act(166, Tech, "loop", "entrance"), Act(1900, Tech, "reroute", "patrol"),
                    MvTo(2300, Enf, new Pt(6520, -4110)), Act(2401, Enf, "takedown", "guard", "knockout"), Mv(2461, Enf, "d:Office entrance"));
                var a = ActOn(r, GuardId);
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.P != null && x.Kind == "down" && x.Phase == "radio"), T(200)), $"a passer-by finds him and radios it ({Report.Clock(g.Tick)})");
                var rep = g.Reps.FirstOrDefault(x => x.Act == a.Id && x.P != null && x.Kind == "down" && x.Phase == "radio");
                if (rep == null) return; // the check above has failed: a clear failure, not a LINQ throw (the suite's whole-scenario tests read every run)
                r.Eq((rep.P!.Id, Report.Clock(rep.LandAt)), ("p-records-office-73", "5:02.1"), "guard 73 radios it, landing at 5:02.1 (Waymark 2026-10-19)");
                r.To(rep.LandAt);
                r.Is(!g.DoorHeld(g.EnterDoor), "the unit is not there yet");
                Orders(r, Act(3021, Enf, "enter"));
                r.Is(r.Until(() => g.DoorHeld(g.EnterDoor), T(30)), $"the unit arrives and holds the public door ({Report.Clock(g.Tick)})");
                r.Is(!g.DoorHeld(g.ExitDoor), "the service door is not held");
                r.To(Math.Max(g.Tick, 3125)); // Waymark: the cancel at or after 5:12.5
                r.Is(r.Op(Enf).Inside && g.ActivityOf(OpId.Enforcer).CancelText.Contains("service lane"), "the cancel line says where he would come out");
                r.Do(Enf, "cancel");
                r.Is(!r.Op(Enf).Inside && G.Within(r.Op(Enf).Pos, r.Stand(g.S.ValeService), 100), "out at the service lane");
                r.Is(g.Log.Exists(l => l.Code == "cancel.enter-out" && l.Text.Contains("service lane")), "the log says so");
                r.Eq(g.Vale, ValeAt.Office, "without Vale");
            });

            Add("N85", "—", "Both doors held, he waits (brief 013 E3; rulebook response: no exit through a held door). This block can't reach it in play (Keel 2026-10-19, briefs/023/keel-n85-question.md): the public door and the service door are on opposite faces, so it takes two scenes, and the quiet yard is found only by the response unit (6:24 at best) while any Enter begun before the public door is held (5:12.1) ends by 6:20. So the start places a held scene at each door (test only, Start.holds; Aster 2026-10-19, option 1). N84's orders take the guard down and go in at 5:02.1; from 5:03 both doors are held (the service door to 5:40, the public door to 6:00). The cancelled Enter waits inside without Vale (the wait itself cannot be cancelled), and comes out the tick a door is free, by that door, when the HUD said.", r =>
            {
                var g = r.G;
                Orders(r, Mv(0, Tech, "cabinet", run: true), MvTo(0, Enf, BesideDoor), Act(166, Tech, "loop", "entrance"), Act(1900, Tech, "reroute", "patrol"),
                    MvTo(2300, Enf, new Pt(6520, -4110)), Act(2401, Enf, "takedown", "guard", "knockout"), Mv(2461, Enf, "d:Office entrance"));
                r.To(3021);
                r.Is(!g.DoorHeld(g.EnterDoor) && !g.DoorHeld(g.ExitDoor), "no door held yet");
                Orders(r, Act(3021, Enf, "enter"));
                r.To(T(303));
                r.Is(g.DoorHeld(g.EnterDoor) && g.DoorHeld(g.ExitDoor), "both doors held (the start's scenes)");
                r.Is(r.Op(Enf).Inside, "the Enforcer is inside");
                r.Do(Enf, "cancel");
                r.Is(r.Op(Enf).Inside && g.Log.Exists(l => l.Code == "cancel.enter-waits"), "both held: he waits inside");
                r.Eq(g.Vale, ValeAt.Office, "without Vale");
                var act = g.ActivityOf(OpId.Enforcer);
                r.Is(act.Verb == "enter" && !act.CanCancel, "the wait shows as inside the Office and cannot be cancelled");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "cancel" }).code, "nothing-to-cancel", "a second cancel is refused");
                int front = g.HeldUntil(g.EnterDoor), back = g.HeldUntil(g.ExitDoor), until = Math.Min(front, back);
                r.Eq((Report.Clock(back), front >= T(360)), ("5:40.0", true), "the HUD's stand-downs: the service door at 5:40, the public door not before 6:00");
                r.Is(r.Until(() => !r.Op(Enf).Inside, T(400)), $"he comes out once a door is free ({Report.Clock(g.Tick)})");
                r.Is(G.Within(r.Op(Enf).Pos, r.Stand(g.S.ValeService), 100) && g.Log.Exists(l => l.Code == "cancel.enter-out"), "by the free door (the service door)");
                r.Is(Math.Abs(g.Tick - until) <= T(2), $"when the HUD said ({Report.Clock(until)})");
            }, new Start { Holds = { ("office-entrance", 303, 360), ("office-service-door", 303, 340) } });

            Add("N86", "—", "The HUD's read-only accessors (Glyph's ask 1; Aster 2026-10-16): HeldUntil (a held door's stand-down, -1 when not held), StashRoom (bodies and room), NextRound (a container's next round, unknown to the cell), CarryPathSight (who sees which stretch of a carry, SeenQuote's test a second at a time). Calling them changes nothing: the state hash is the same before and after.", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                r.Takedown("guard", "knockout"); r.Done(Enf);
                ulong h0 = Hash.State(g);
                r.Eq(g.HeldUntil(g.EnterDoor), -1, "no door held");
                r.Eq(g.StashRoom(DumpsterId), (0, 1), "the Dumpster: empty, holds 1");
                r.Eq(g.StashRoom(ServiceDoorId), (0, g.R.StashDoorHolds), "the service door: empty, holds 3");
                r.Is(g.StashRoom("nowhere") == (-1, -1), "an unknown stash: (-1, -1)"); // no tuple in the text: its minus sign is the culture's (Redline E7)
                var (round, known) = g.NextRound(DumpsterId);
                int period = T(g.R.ServicePeriodS), phase = (int)(Streams.Fnv1a32(DumpsterId) % (uint)period);
                r.Is(round >= g.Tick && round < g.Tick + period && (round - phase) % period == 0 && !known, $"the Dumpster's next round {Report.Clock(round)}, unknown to the cell (\"?\")");
                r.Eq(g.NextRound(ServiceDoorId).tick, -1, "a door has no round");
                var dry = g.CarryPathSight(OpId.Enforcer, ServiceDoor);
                r.Is(dry.Refused == "" && dry.S > 0 && dry.PaceCmS == g.R.CellSpeed && dry.Stretches.Count >= 1 && dry.Stretches[0].FromS == 0 && dry.Stretches.Last().ToS == dry.S, $"a carry to the service door: {dry.S} s at {dry.PaceCmS} cm/s in {dry.Stretches.Count} stretch(es)");
                r.Is(dry.Stretches.Zip(dry.Stretches.Skip(1), (x, y) => y.FromS == x.ToS + 1).All(b => b), "the stretches follow each other, a second at a time");
                var slow = g.CarryPathSight(OpId.Scout, ServiceDoor);
                r.Is(slow.PaceCmS == g.R.CarryUntrainedSpeed && slow.S > dry.S, $"the Scout carries slower ({slow.S} s)");
                var guardCam = "camera:entrance";
                r.Is(!dry.SeenS.ContainsKey(guardCam), "the looped entrance camera sees none of it");
                r.Is(Hash.State(g) == h0, "nothing changed: the state hash is the same");
                r.Do(Tech, "cancel"); // the loop goes: the entrance camera is live again
                var live = g.CarryPathSight(OpId.Enforcer, ServiceDoor);
                var k = g.Known(guardCam)!;
                r.Is(k.Sight ? live.SeenS.ContainsKey(guardCam) && live.Seen == "yes" : live.MaybeS.ContainsKey(guardCam), $"live, the entrance camera {(k.Sight ? "sees" : "could see (?)")} the first stretch ({string.Join(", ", live.SeenS.Select(x => x.Key + " " + x.Value + " s"))}{string.Join(", ", live.MaybeS.Select(x => x.Key + " ? " + x.Value + " s"))})");
                r.Is(live.Stretches.Exists(x => x.Seen == "no") || live.Seen != "no", "seen and unseen stretches are told apart");
            });

            Add("N87", "—", "A searching officer sent on by what he finds (Keel 2026-10-16, found trying driver-route variants for Waymark; re-pinned to Waymark's orders 2026-10-19, the service door on the east face): the driver and guard 29 killed at 2:31 and 2:35 and put through the service door, guard 74 knocked out in front of the outgoing shift at 5:40, Vale taken at 6:10. At 6:56.1 police-3, searching the lane, opens the service door: the driver's body lands its own incident, and the desk sends police-3 on to it. Before, the search then took the opened stash off a list the reassignment had emptied (ArgumentOutOfRange, engine-error); now the stash comes off the list before it is opened, and the officer walks to the new scene.", r =>
            {
                var g = r.G;
                Orders(r, DriverOpening().Concat(new[] {
                    MvTo(1400, Enf, new Pt(7690, -3370)), Act(1510, Enf, "takedown", Driver, "kill"), MvTo(1540, Enf, new Pt(7625, -2880)), Act(1557, Enf, "takedown", Guard29, "kill"),
                    Act(1617, Enf, "carry", Driver), MvTo(1647, Enf, ServiceDoor), Act(1815, Enf, "stash", ServiceDoorId), Act(1900, Scout, "mislead", "rivals"), Act(1900, Tech, "reroute", "patrol"),
                    new Order { Tick = 1915, Op = Enf, Do = "move", Point = new Pt(7625, -2880), Run = true }, Act(1973, Enf, "carry", Guard29), MvTo(2003, Enf, ServiceDoor), Act(2119, Enf, "stash", ServiceDoorId),
                    MvTo(2219, Enf, new Pt(7625, -2880)), Mv(2340, Scout, "crew:scout"), Act(3400, Enf, "takedown", Guard74, "knockout"), MvTo(3460, Enf, new Pt(7700, -3100)), Act(3700, Enf, "take"),
                    MvTo(4080, Enf, new Pt(2400, -6000)) }).ToArray()); // brief 018 (c018): no panic: without the final fight no fight is on
                const int opens = 4161; // 6:56.1 (Waymark 2026-10-19; was 7:58.5, tick 4785, before the east-face door)
                r.To(opens);
                var open = g.Log.FindIndex(l => l.Code == "stash.opened" && l.Tick == opens && l.Text.Contains("driver"));
                r.Is(open >= 0, "at 6:56.1 an officer opens the service door and finds the driver");
                r.Is(g.Log.Exists(l => l.Code == "alarm.reassigns" && l.Tick == opens), "and is sent on to the scene it found");
                r.Is(g.FailCode != "engine-error", $"no engine error{(g.FailCode == "engine-error" ? ": " + g.FailText : "")}");
                r.To(T(500));
                r.Is(g.FailCode != "engine-error", "the run goes on");
            }, NoFight()); // brief 018 (c018): walking Vale the Enforcer no longer fires, so the loud final fight downs him at 7:35, before the 7:58.5 search; without the final fight the fixture is the same path
        }
    }
}
