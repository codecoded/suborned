// Brief 013 E (Keel, 2026-10-16; Aster's small items after Waymark's c010): one scenario a change, on the real block and the promoted cast.
//  N79 a tie out of speaking range is told only from a phone: the teller walks there, and a takedown on the way stops it (the driver route, early)
//  N80 the same walk not stopped: the tie is told by phone when the call is made, not at the sight
//  N81 face to face is unchanged (the driver route, late: the driver tells Vale at once); N82 one hop: a told person never retells
//  N83 a fleeing teller phones only from the refuge she reaches
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
        /// beside the official entrance on the camera's blind side, the Scout watches the driver.</summary>
        static Order[] DriverOpening() => new[] {
            new Order { Tick = 0, Op = Tech, Do = "move", To = "cabinet", Run = true }, MvTo(0, Enf, new Pt(7625, -2880)), MvTo(0, Scout, new Pt(8250, -3100)), Mv(0, Fixer, "crew:fixer"),
            Act(136, Tech, "loop", "pickup"), Act(560, Scout, "watch", Driver), MvTo(760, Scout, new Pt(8250, -3100)) }; // c014 start (brief 014): the Scout is within 6 m of the driver at 0:56.0, not 0:51.8
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

            Add("N83", "—", "A fleeing teller (brief 013 E1; Pulse §3.4: 'tells if they reach a phone'): clerks 32 (Fearful) and 42 walk the square together after the chime; the Enforcer knocks out 42 beside her. She flees (startled, then a run to a refuge). The one tie who was within speaking range is the one on the ground; her family is indoors, so she tells them only if the refuge she reaches is a staffed door with its desk phone: no walk to a phone beyond her flight.", r =>
            {
                var g = r.G;
                var w42 = WalkerById(r, "p-records-office-42"); var w = WalkerById(r, "p-records-office-32");
                r.To(T(340)); r.To(MeetFromBehind(r, w42, T(370)));
                r.Is(r.Until(() => g.CrowdPos(w42) is Pt p && G.Within(p, r.Op(Enf).Pos, g.R.TakedownReachCm), T(40)), $"clerk 42 walks into reach ({Report.Clock(g.Tick)})");
                r.Takedown("p-records-office-42", "knockout");
                var a = ActOn(r, "p-records-office-42");
                r.Is(a.Noticed.Contains(w.P.Id) && w.St == "startled", "clerk 32 sees it and is startled (Fearful: flee)");
                var tell = g.Reps.SingleOrDefault(x => x.P == w.P && x.Kind == "tell");
                r.Is(tell != null && tell.Phase == "flee" && tell.LandAt == Game.Never, "her tell waits on her flight (no walk of its own)");
                r.Is(!g.Log.Exists(l => l.Code == "witness.told" && l.Text.StartsWith("p-records-office-32 ", StringComparison.Ordinal)), "nobody is told at the sight");
                if (tell == null) return;
                r.Is(r.Until(() => tell.Phase != "flee", T(120)), $"her flight ends ({Report.Clock(g.Tick)}, at {w.Refuge})");
                bool door = g.S.Refuges.Exists(x => x.id == w.Refuge && x.kind == "door");
                if (door)
                {
                    r.Is(tell.Phase == "call" && tell.LandAt == g.Tick + T(g.R.CallS), $"{w.Refuge} is a staffed door: she calls from its desk");
                    r.To(tell.LandAt);
                    r.Is(g.Log.Exists(l => l.Code == "witness.told" && l.Tick == g.Tick && l.Text.StartsWith("p-records-office-32 ", StringComparison.Ordinal) && l.Text.Contains("by phone")), "a tie is told by phone");
                }
                else
                {
                    r.Is(tell.Phase == "stopped" && g.Log.Exists(l => l.Code == "witness.tell-stopped" && l.Text.Contains("no phone")), $"{w.Refuge} has no phone: nobody is told");
                    r.To(g.Tick + T(30));
                    r.Is(!a.Told.Contains("p-cafe-13"), "and her family is never told");
                }
            });

            Add("N84", "—", "A cancelled Enter comes out by a door no unit holds (Aster 2026-10-16, brief 013 E3; rulebook response: no exit through a held door). The guard knocked out unseen at his post and left; guard 74 finds him and radios it; the Enforcer goes in at the landing, before the scene unit arrives. The unit arrives from the Office's own post and holds the public door. The player cancels the Enter: the Enforcer comes out at the service lane, not through the held public door.", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                r.Takedown("guard", "knockout"); r.Done(Enf);
                var a = ActOn(r, GuardId);
                r.To(r.SendTo(Enf, r.Stand(g.S.GuardPost)));
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.P != null && x.Kind == "down" && x.Phase == "radio"), T(200)), $"a passer-by finds him and radios it ({Report.Clock(g.Tick)})");
                var rep = g.Reps.First(x => x.Act == a.Id && x.P != null && x.Kind == "down" && x.Phase == "radio");
                r.To(rep.LandAt);
                if (g.PatrolAwayUntil <= g.Tick || g.PatrolWatching(g.S.At(g.S.GuardPost))) { r.Do(Tech, "reroute", "patrol"); r.Done(Tech); }
                r.Is(!g.DoorHeld(g.EnterDoor), "the unit is not there yet");
                r.Do(Enf, "enter");
                r.Is(r.Until(() => g.DoorHeld(g.EnterDoor), T(30)), $"the unit arrives and holds the public door ({Report.Clock(g.Tick)})");
                r.Is(!g.DoorHeld(g.ExitDoor), "the service door is not held");
                r.Is(r.Op(Enf).Inside && g.ActivityOf(OpId.Enforcer).CancelText.Contains("service lane"), "the cancel line says where he would come out");
                r.Do(Enf, "cancel");
                r.Is(!r.Op(Enf).Inside && G.Within(r.Op(Enf).Pos, r.Stand(g.S.ValeService), 100), "out at the service lane");
                r.Is(g.Log.Exists(l => l.Code == "cancel.enter-out" && l.Text.Contains("service lane")), "the log says so");
                r.Eq(g.Vale, ValeAt.Office, "without Vale");
            });

            Add("N85", "—", "Both doors held, he waits (brief 013 E3): the guard knocked out unseen and laid where a scene would hold both Office doors (in sight of the public door and the service door, 11 m and 10 m); guard 74 walking the lane finds him and radios it; the Enforcer goes in at the landing. The unit holds both doors; the cancelled Enter waits inside without Vale (the wait itself cannot be cancelled), and comes out the tick a door is free.", r =>
            {
                var g = r.G;
                var spot = new Pt(5300, -4100);
                r.Is(g.DoorsInSightOf(spot).Contains(g.EnterDoor) && g.DoorsInSightOf(spot).Contains(g.ExitDoor), "a body found here holds both Office doors");
                GuardQuiet(r);
                r.Takedown("guard", "knockout"); r.Done(Enf);
                r.Body(Enf, "carry", "guard"); r.Done(Enf);
                r.To(r.SendTo(Enf, spot)); r.Body(Enf, "drop"); r.Done(Enf);
                var a = ActOn(r, GuardId);
                r.To(r.SendTo(Enf, r.Stand(g.S.GuardPost)));
                r.Is(r.Until(() => g.Reps.Exists(x => x.Act == a.Id && x.P != null && x.Kind == "down" && x.Phase == "radio"), T(200)), $"a passer-by finds him and radios it ({Report.Clock(g.Tick)})");
                var rep = g.Reps.First(x => x.Act == a.Id && x.P != null && x.Kind == "down" && x.Phase == "radio");
                r.To(rep.LandAt);
                if (g.PatrolAwayUntil <= g.Tick || g.PatrolWatching(g.S.At(g.S.GuardPost))) { r.Do(Tech, "reroute", "patrol"); r.Done(Tech); }
                r.Do(Enf, "enter");
                r.Is(r.Until(() => g.DoorHeld(g.EnterDoor) && g.DoorHeld(g.ExitDoor), T(30)), $"the unit holds both doors ({Report.Clock(g.Tick)})");
                r.Is(r.Op(Enf).Inside, "the Enforcer is inside");
                r.Do(Enf, "cancel");
                r.Is(r.Op(Enf).Inside && g.Log.Exists(l => l.Code == "cancel.enter-waits"), "both held: he waits inside");
                r.Eq(g.Vale, ValeAt.Office, "without Vale");
                var act = g.ActivityOf(OpId.Enforcer);
                r.Is(act.Verb == "enter" && !act.CanCancel, "the wait shows as inside the Office and cannot be cancelled");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "cancel" }).code, "nothing-to-cancel", "a second cancel is refused");
                int until = g.HeldUntil(g.EnterDoor);
                r.Is(until > g.Tick, $"the HUD's stand-down for the public door: {Report.Clock(until)}");
                r.Is(r.Until(() => !r.Op(Enf).Inside, T(400)), $"he comes out once a door is free ({Report.Clock(g.Tick)})");
                bool front = !g.DoorHeld(g.EnterDoor);
                r.Is(G.Within(r.Op(Enf).Pos, front ? r.Stand(g.S.GuardPost) : r.Stand(g.S.ValeService), 100) && g.Log.Exists(l => l.Code == "cancel.enter-out"), $"by the free door ({(front ? "public" : "service")})");
                r.Is(Math.Abs(g.Tick - until) <= T(2), $"when the HUD said ({Report.Clock(until)})");
            });

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

            Add("N87", "—", "A searching officer sent on by what he finds (Keel 2026-10-16, found trying driver-route variants for Waymark): guard 29 and the driver killed at 2:31 and put through the service door, guard 74 knocked out in front of the outgoing shift at 5:40 (Alert 3), Vale taken at 6:10. At 7:58.5 police-5, searching the lane, opens the service door: the driver's body lands its own incident, and the desk sends police-5 on to it. Before, the search then took the opened stash off a list the reassignment had emptied (ArgumentOutOfRange, engine-error); now the stash comes off the list before it is opened, and the officer walks to the new scene.", r =>
            {
                var g = r.G;
                Orders(r, DriverOpening().Concat(new[] {
                    Act(1510, Enf, "takedown", Guard29, "kill"), MvTo(1540, Enf, new Pt(7690, -3370)), Act(1570, Enf, "takedown", Driver, "kill"),
                    Act(1630, Enf, "carry", Driver), MvTo(1660, Enf, ServiceDoor), Act(1775, Enf, "stash", ServiceDoorId),
                    new Order { Tick = 1875, Op = Enf, Do = "move", Point = new Pt(7625, -2880), Run = true }, Act(1900, Scout, "mislead", "rivals"), Act(1900, Tech, "reroute", "patrol"),
                    Act(1921, Enf, "carry", Guard29), MvTo(1951, Enf, ServiceDoor), Act(2043, Enf, "stash", ServiceDoorId), MvTo(2143, Enf, new Pt(6278, -4175)), Mv(2340, Scout, "crew:scout"),
                    MvTo(2400, Enf, new Pt(7625, -2880)), Act(3400, Enf, "takedown", Guard74, "knockout"), MvTo(3610, Enf, new Pt(7700, -3100)), Act(3700, Enf, "take"),
                    MvTo(4080, Enf, new Pt(2400, -6000)) }).ToArray()); // brief 018 (c018): no panic: without the final fight no fight is on
                r.To(T(478) + 5);
                var open = g.Log.FindIndex(l => l.Code == "stash.opened" && l.Tick == T(478) + 5 && l.Text.Contains("driver"));
                r.Is(open >= 0, "at 7:58.5 an officer opens the service door and finds the driver");
                r.Is(g.Log.Exists(l => l.Code == "alarm.reassigns" && l.Tick == T(478) + 5), "and is sent on to the scene it found");
                r.Is(g.FailCode != "engine-error", $"no engine error{(g.FailCode == "engine-error" ? ": " + g.FailText : "")}");
                r.To(T(500));
                r.Is(g.FailCode != "engine-error", "the run goes on");
            }, NoFight()); // brief 018 (c018): walking Vale the Enforcer no longer fires, so the loud final fight downs him at 7:35, before the 7:58.5 search; without the final fight the fixture is the same path
        }
    }
}
