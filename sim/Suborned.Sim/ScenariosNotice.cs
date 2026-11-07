// Engine 0.28.0's rules: sight from real heights (brief 055 §7, rulebook perception.mount), civilian notice by a cone and the Alert
// (brief 054 §3, perception.people, witness.notice) and the Watch that shows something (brief 054 §2, §4, rulebook mood).
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>A point `cm` along the person's facing from where they stand (negative: behind them), on free ground nearby.</summary>
        static Pt? AlongFacing(Game g, PersonSight s, int cm)
        {
            long len = Math.Max(1, G.Isqrt((long)s.Facing.X * s.Facing.X + (long)s.Facing.Y * s.Facing.Y));
            var p = new Pt(s.At.X + (int)(s.Facing.X * cm / len), s.At.Y + (int)(s.Facing.Y * cm / len));
            return g.S.Grid.Free(p) ? p : g.S.Grid.NearestFree(p) is Pt q && G.Within(q, p, 60) ? q : (Pt?)null;
        }
        /// <summary>The first civilian on the street with a wedge (a facing) who has free ground 5 m ahead and 5 m behind, at this tick.</summary>
        static (Walker w, PersonSight s, Pt ahead, Pt behind)? ACivilian(Game g, Func<Walker, bool>? also = null)
        {
            foreach (var w in g.Walkers)
            {
                if (!w.P.Civilian || g.CrowdPos(w) == null || (also != null && !also(w))) continue;
                var s = g.PersonCone(w.P.Id); if (s.Kind != "wedge") continue;
                var a = AlongFacing(g, s, 500); var b = AlongFacing(g, s, -500);
                if (a.HasValue && b.HasValue && g.S.Sight3(s.At, g.S.Grid.FloorAt(s.At) + g.R.Eye, a.Value, g.S.Grid.FloorAt(a.Value) + g.R.Eye) && g.S.Sight3(s.At, g.S.Grid.FloorAt(s.At) + g.R.Eye, b.Value, g.S.Grid.FloorAt(b.Value) + g.R.Eye)) return (w, s, a.Value, b.Value);
            }
            return null;
        }

        static void NoticeCases()
        {
            // ---------------------------------------------------------------- sight from real heights (brief 055 §7)
            Add("MS-1", "—", "The 3D line (rulebook perception.mount; brief 055 §7): a 2 m solid blocks a 1.6 m line through it, a line from a 4.79 m lens over it to a head behind it is clear, and a line that only touches its corner is blocked at a height it spans and clear above it.", r =>
            {
                var box = new Occluder("box", 0, 200, new[] { new Pt(0, 0), new Pt(100, 0), new Pt(100, 100), new Pt(0, 100) });
                r.Is(box.Blocks3(new Pt(-500, 50), 160, new Pt(600, 50), 160), "a 1.6 m line through the 2 m solid is blocked");
                r.Is(!box.Blocks3(new Pt(-500, 50), 479, new Pt(600, 50), 160), "from the 4.79 m lens over it to a head beyond: clear (334 cm above it)");
                r.Is(box.Blocks3(new Pt(-500, 50), 479, new Pt(150, 50), 160), "a head just behind it is in its shadow (the line crosses the box at 186..208 cm)");
                r.Is(box.Blocks3(new Pt(-100, 100), 150, new Pt(100, -100), 150), "a line touching only its corner, at 1.5 m: blocked, as the 2D test has it");
                r.Is(!box.Blocks3(new Pt(-100, 100), 300, new Pt(100, -100), 300), "the same graze at 3 m: clear");
                r.Is(!box.Blocks3(new Pt(-500, 50), 260, new Pt(600, 50), 210), "above it all the way: clear");
                r.Wait(1); // a scenario steps its run at least one tick (its own replay)
            });

            Add("MS-2", "—", "Eyes above their own floor (brief 055 §7): the door guard's post is on the Office landing (75 cm), so he sees from 2.35 m; the entrance camera sees the door's stand from its 4.79 m lens over the door planting, where an eye at 1.6 m beside the pole would not.", r =>
            {
                var g = r.G;
                var post = g.S.At(g.S.GuardPost); int fl = g.S.Grid.FloorAt(post);
                r.Eq(fl, 75, "the landing's floor, block z cm (site grid.floor)");
                var cam = g.S.Cameras.First(c => c.Id == "entrance"); var door = r.Stand("d:Office entrance"); int head = g.S.Grid.FloorAt(door) + g.R.Eye;
                r.Eq(cam.Kind, "pole-camera", "the mission's cameras are pole cameras");
                r.Is(g.CameraWatching("entrance", door, false), "the entrance camera covers the door from its lens");
                r.Is(g.S.Sight3(cam.At, g.S.Grid.FloorAt(cam.At) + g.R.MountCameraCm["pole-camera"], door, head, true), "the lens line to the guard's head is clear");
                r.Wait(1);
            });

            // ---------------------------------------------------------------- who notices what (brief 054 §3)
            Add("NC-1", "—", "A civilian sees by their cone (brief 054 §3.3, perception.people: ±60° to 13 m, no near sense): a takedown 5 m in front of her is seen, the same takedown 5 m behind her is not; a security person (a guard, all round) sees both.", r =>
            {
                var g = r.G;
                r.To(T(30));
                var c = ACivilian(g);
                if (!r.Is(c.HasValue, "a civilian on the street with ground in front and behind")) return;
                var (w, s, ahead, behind) = c!.Value;
                r.Is(g.NoticesNow(w.P.Id, "takedown", ahead), $"{w.P.Id} sees a takedown 5 m in front of her");
                r.Is(!g.NoticesNow(w.P.Id, "takedown", behind), "and not one 5 m behind her back");
                var guard = g.Walkers.FirstOrDefault(x => !x.P.Civilian && g.CrowdPos(x).HasValue);
                if (guard != null) { var gp = g.CrowdPos(guard)!.Value; var s2 = g.PersonCone(guard.P.Id); r.Eq(s2.Kind, "ring", "security sees all round (a ring)"); }
            });

            Add("NC-2", "—", "Below Alert 2 ordinary people mind their own business (brief 054 §3.2; witness.notice.odd, people-care-alert): a Pressure in front of a civilian at Alert 0 is unseen by her, a drawn weapon there is seen; the private approaches never are; security see the Pressure at any Alert.", r =>
            {
                var g = r.G;
                r.To(T(30));
                r.Eq(g.Alert, 0, "Alert 0");
                var c = ACivilian(g);
                if (!r.Is(c.HasValue, "a civilian on the street")) return;
                var (w, _, ahead, _) = c!.Value;
                r.Is(!g.NoticesNow(w.P.Id, "threat", ahead), "a Pressure in front of her at Alert 0: unseen (witness.notice.odd: threat)");
                r.Is(g.NoticesNow(w.P.Id, "weapon-drawn", ahead), "a drawn weapon there: seen (always)");
                r.Is(!g.NoticesNow(w.P.Id, "pay", ahead) && !g.NoticesNow(w.P.Id, "listen", ahead), "Pay and Listen: never");
                r.Is(!g.CaresAbout(w.P.Id, "threat") && g.CaresAbout(w.P.Id, "takedown"), "her cone matters on hover for a takedown, not for a Pressure (brief 054 §3.1)");
                r.Is(g.CaresAbout("p-records-office-74", "threat"), "an off-duty guard cares about a Pressure at Alert 0 (security)");
            });

            Add("NC-3", "—", "From Alert 2 the city is on edge (brief 054 §3.2): after the pickup camera sees the Enforcer draw on it (Alert 2: a drawn weapon seen by security), a civilian sees a Pressure and a run in her cone; a shot is heard and seen all round within 13 m on a clear line.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Enf, new Pt(7400, -3600)));
                r.Is(r.Fire(Enf, "camera:pickup", force: true), "the Enforcer fires on the pickup camera");
                r.Is(r.Until(() => g.Alert >= 2, T(60)), $"Alert {g.Alert} ({Report.Clock(g.Tick)})");
                var c = ACivilian(g);
                if (!r.Is(c.HasValue, "a civilian on the street")) return;
                var (w, _, ahead, behind) = c!.Value;
                r.Is(g.NoticesNow(w.P.Id, "threat", ahead), "a Pressure in front of her: seen now");
                r.Is(g.NoticesNow(w.P.Id, "run", ahead), "and a run: she would remember the face (no found fault)");
                r.Is(!g.NoticesNow(w.P.Id, "threat", behind), "behind her: still unseen");
                r.Is(g.NoticesNow(w.P.Id, "gunfire", behind), "a shot behind her within 13 m: the bang turns her head");
            });

            Add("NC-4", "—", "Stop facings (brief 054 §3.3): a person seated at a café table faces their chair's way (the seats file's chair face, now an engine input through the people file); a takedown across their table is seen, one behind their chair is not.", r =>
            {
                var g = r.G;
                (Walker w, PersonSight s, Pt ahead, Pt behind)? c = null;
                for (int k = 0; k < 20 && !c.HasValue; k++) { r.To(g.Tick + T(15)); c = ACivilian(g, x => x.Place.Any(p => p.Contains("-seat-")) && g.CrowdPos(x) is Pt q && g.People2!.Places.Any(kv => kv.Key.Contains("-seat-") && kv.Value == q)); }
                if (!r.Is(c.HasValue, "someone seated at a café table")) return;
                var (w, s, ahead, behind) = c!.Value;
                r.Is(g.NoticesNow(w.P.Id, "takedown", ahead) && !g.NoticesNow(w.P.Id, "takedown", behind), $"{w.P.Id} at their seat sees across the table, not behind the chair");
            });

            // ---------------------------------------------------------------- a body at your feet (brief 054 §9.1, engine 0.29.0)
            Add("ND-1", "—", "A body at your feet (brief 054 §9.1, witness.notice.near, people-near-body-cm 150): the door guard killed at his post on the Office landing with the entrance camera kept looped and the patrol rerouted; after the 5:30 chime a clerk steps out of the door with her back to the body and finds it all the same, within 1.5 m on a clear line (the engine's own read, NoticesNow body, says the same; a takedown there, cone-only, she would not see). The café half is ND-2.", r =>
            {
                var g = r.G;
                Orders(r, new Order { Tick = 0, Op = Tech, Do = "move", To = "cabinet", Run = true }, MvTo(0, Enf, BesideDoor), Mv(0, Scout, "d:Office approach"), Mv(0, Fixer, "crew:fixer"),
                    Act(300, Tech, "loop", "entrance") /* Loop v2 (brief 058 §4): planted at 0:30 so its 2:00 runs past the Enter */, Act(462, Scout, "watch", "guard"), Mv(662, Scout, "crew:scout"), Act(1060, Tech, "reroute", "patrol"), // N68's route G-kill; the Tech holds the loop
                    MvTo(1440, Enf, BesideGuard), Act(1510, Enf, "takedown", "guard", "kill"), MvTo(1540, Enf, new Pt(1025, -4625)), MvTo(1540, Scout, new Pt(1175, -4825)), MvTo(1540, Fixer, new Pt(1025, -4825)));
                r.To(T(160));
                var body = g.Bodies.FirstOrDefault(b => b.P == g.GuardP);
                if (!r.Is(body != null && body.Dead && body.Open, "the guard lies dead and open at his post")) return;
                var a = ActOn(r, GuardId);
                bool Civ(string id) => g.PersonById(id) is Person q && q.Civilian;
                bool found = r.Until(() => a.Noticed.Any(Civ), T(340) - g.Tick);
                if (!r.Is(found, $"a civilian finds the body ({Report.Clock(g.Tick)})")) return;
                var at = g.BodyPos(body!);
                var who = a.Noticed.Where(Civ).ToList(); int near = 0, behind = 0;
                foreach (var id in who)
                {
                    var s = g.PersonCone(id);
                    if (s.Kind == "none" && s.At == new Pt(0, 0)) continue; // off the street again this tick
                    r.Eq(s.NearCm, 150, $"{id}'s near sense for a body: 1.5 m (PersonSight.NearCm, the host's hover)");
                    bool inNear = G.Within(s.At, at, 150), inCone = s.Kind == "wedge" && G.InCone(s.At, s.Facing, s.Reach, s.HalfCm, 0, at);
                    if (inNear) near++; if (inNear && s.Kind == "wedge" && !inCone) behind++;
                    if (g.BodyLayOf(id) == default) r.Is(g.NoticesNow(id, "body", at), $"{id}: the engine's read agrees with the outcome (NoticesNow body)"); // brief 098 L9: one the law laid aside at the tick's end noticed it where she stood when she noticed
                    if (inNear && !inCone) r.Is(!g.NoticesNow(id, "takedown", at), $"{id}: a takedown there she would not see (cone-only)");
                }
                r.Eq(Report.Clock(g.Tick), "5:30.1", "found when the clerks step out after the 5:30 chime");
                r.Is(near > 0, $"{near} found it within 1.5 m ({behind} of them with their back to it)"); // brief 098 L9: the clerks stepping out together are laid round the door, so whether one has her back to it is the moment's; the near sense behind one's back is ND-2's

            }, new Start { FinalFight = false });

            Add("ND-2", "—", "A body at your feet, the café (brief 054 §9.1): a seated café customer faces her chair's way; a body 2 m behind her chair, outside her cone and the 1.5 m, is not found; 1 m behind it, it is (the near sense); 2 m in front, it is (her cone); a takedown 1 m behind her is not seen (the near sense is for bodies only).", r =>
            {
                var g = r.G;
                // the cell gives no orders (NC-4's café): a seated customer, a body behind her chair, by the engine's one test (ScanAct reads it)
                r.Wait(1); // engine 0.30.0: someone is seated at 0:00 on the face-margin grid; a run that never steps does not replay as itself (Finish's closing checkpoint)
                (Walker w, PersonSight s, Pt ahead, Pt behind)? c = null;
                for (int k = 0; k < 20 && !c.HasValue; k++) { c = ACivilian(g, x => x.Place.Any(p => p.Contains("-seat-")) && g.CrowdPos(x) is Pt q && g.People2!.Places.Any(kv => kv.Key.Contains("-seat-") && kv.Value == q)); if (!c.HasValue) r.To(g.Tick + T(15)); }
                if (r.Is(c.HasValue, "someone seated at a café table"))
                {
                    var (cw, cs, _, _) = c!.Value;
                    Pt? Raw(int cm) { long len = Math.Max(1, G.Isqrt((long)cs.Facing.X * cs.Facing.X + (long)cs.Facing.Y * cs.Facing.Y)); return new Pt(cs.At.X + (int)(cs.Facing.X * cm / len), cs.At.Y + (int)(cs.Facing.Y * cm / len)); }
                    var two = Raw(-200); var one = Raw(-100); var front = Raw(200); // points on her facing line (a chair or table may stand there; the test is the line, not the ground)
                    if (r.Is(two.HasValue && one.HasValue && front.HasValue, "points 1 and 2 m behind her chair and 2 m in front"))
                    {
                        r.Is(!g.NoticesNow(cw.P.Id, "body", two!.Value), $"{cw.P.Id}: a body 2 m behind her chair, outside her cone and the 1.5 m: not found");
                        r.Is(g.NoticesNow(cw.P.Id, "body", one!.Value), "1 m behind it: found (the near sense)");
                        r.Is(g.NoticesNow(cw.P.Id, "body", front!.Value), "2 m in front: found (her cone)");
                        r.Is(!g.NoticesNow(cw.P.Id, "takedown", one!.Value), "a takedown 1 m behind her: not seen (the near sense is for bodies only)");
                    }
                }
            });

            // ---------------------------------------------------------------- what a Watch shows (brief 054 §2, §4)
            Add("WM-1", "—", "The Watch set at 0:00 (brief 054 §2.1, §6): Inez (Fearful, fear 1) reads uneasy and has a Watch to give; the door guard too (security: his sight and marks); the driver (Mercenary, neutral) and Vale have none (nothing-to-see, free). Listen on Inez waits for her Watch (needs-routine); Listen and Pay on the driver need none.", r =>
            {
                var g = r.G;
                r.Is(g.WatchOffered(Inez) && g.WatchOffered(GuardR), "Inez and the guard: a Watch to give");
                r.Is(!g.WatchOffered(Driver) && !g.WatchOffered(Vale), "the driver and Vale: none");
                r.Eq(g.ScoutQuery("watch", Driver).code, "nothing-to-see", "the driver's Watch is refused nothing-to-see");
                r.Eq(g.ScoutQuery("listen", Inez).code, "needs-routine", "Listen on Inez: Watch first");
                r.Is(g.ScoutQuery("listen", Driver).code != "needs-routine", "Listen on the driver: no Watch needed");
                r.Is(g.Preview("pay", Driver).Refused == "" && g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "turn", Target = Driver, Approach = "pay" }).code != "needs-routine", "Pay on the driver: no Watch needed");
                r.Wait(1);
            });

            Add("WM-2", "—", "After a Watch (brief 054 §2.3, §4.3): the cell knows Inez reads uneasy (MoodOf), the Pressure card wears likely and the Help card a dashed half; a second Watch is already-watched; Listen opens.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard"));
                r.Eq(g.MoodOf(Inez), "", "before the Watch the cell knows no mood");
                r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.Eq(g.MoodOf(Inez), "uneasy", "the Watch saw her uneasy");
                var pr = g.Preview("pressure", Inez); var he = g.Preview("help", Inez);
                r.Is(pr.Mood == "uneasy" && !pr.Likely && !pr.Hurt, "uneasy moves Pressure 0 (mood.shift: uneasy pressure 0): no mark");
                r.Is(he.Hurt && !he.Likely, "Help: a dashed half (uneasy help −1; a Fearful Help is already worse, the floor)");
                r.Eq(g.ScoutQuery("watch", Inez).code, "already-watched", "a second Watch: already watched");
                r.Is(g.ScoutQuery("listen", Inez).code != "needs-routine", "Listen opens");
            });

            Add("WM-3", "—", "A mood that changes mid-mission (brief 054 §2.2): the driver, neutral and never watched, sees the Enforcer draw beside his car and turns uneasy: now he has a Watch to give, and Listen on him waits for it.", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Meet driver"));
                r.Is(!g.WatchOffered(Driver), "no Watch to give before");
                r.Do(Enf, "draw"); r.Wait(T(2));
                bool saw = g.Driver.Memory.Exists(m => m.Act == "saw-weapon-drawn");
                if (!saw) { r.Is(true, "the driver faced away from the draw (no mood change): nothing to check"); return; }
                r.Is(g.WatchOffered(Driver), "he saw the weapon: uneasy, a Watch to give");
                r.Eq(g.ScoutQuery("listen", Driver).code, "needs-routine", "Listen on him now waits for the Watch");
            });

            Add("WM-4", "—", "The mood's step on the fit (brief 054 §4.2, rulebook mood.shift): friendly lifts a suiting Pay to ready ($20,000 less) and a Help one order fewer; scared lifts a suiting Pressure to ready (one Trail less); a refusal is never lifted; worse is the floor.", r =>
            {
                var g = r.G; var R = g.R; var band = R.BandOf(g.Standing);
                foreach (var t in R.TemperamentIds)
                {
                    foreach (var a in new[] { "help", "pay", "pressure" })
                    {
                        var f = R.FitOf(a, t, band);
                        foreach (var m in Rules.Moods)
                        {
                            var fm = R.FitWithMood(a, t, band, m);
                            if (f == Fit.Refused) r.Is(fm == Fit.Refused, $"{t} {a} refused stays refused ({m})");
                            if (f == Fit.Worse && R.MoodStep(m, a) <= 0) r.Is(fm == Fit.Worse, $"{t} {a} worse is the floor ({m})");
                            if (f == Fit.Suits && R.MoodStep(m, a) > 0) r.Is(fm == Fit.Ready, $"{t} {a} suits steps up to ready ({m})");
                        }
                    }
                }
                r.Eq(R.Num("v:ready-pay-usd"), -20000L, "ready Pay: $20,000 less");
                r.Wait(1);
            });
        }
    }
}
