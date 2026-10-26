// Engine 0.35.0 scenarios: brief 039 B, cover and pin (Sky picked B on 2026-10-26; GDD v0.28.0 §06; rulebook weapons.oneInput, focus,
// takeCover, pin, moves; GameCoverPin.cs). Ids avoid the S prefix (the 28). On the range (its today preset is B):
//    B-INPUT       one input: cease, stance, panic and free are unknown verbs; F draws; weapons free is on whatever the start says; pre039 takes them
//    B-PIN         a round pins its target for 2 s (hit or miss), renewed by each round; a pinned shooter fires at half; pinCell decides the cell
//    B-STICKY      the focus waits out of reach (no out-of-range end, no lapse) while the agent fights on its own, then takes him back after aimNew
//    B-SNAP        a drawn agent's move in a fight snaps to the kneel point of cover within 10 m; he kneels there, ducks when shot at
//    B-COVER-FIRE  Covering fire pins everyone within 3 m of its target for its 4 s, never hits; a pinned rival holds; the move cools 20 s
//    B-MARK        Mark: the marked hostile's cover doesn't count against the cell for 10 s (the line's chance), the Scout's alone, 30 m
//    B-STEAM       Steam: no line within 4 m of an open heat-main grille for 8 s; no grille in reach, steam-no-grille
//    B-TALK        Talk down: never police, not before one of the crew is down, not on an unhurt rival; then he breaks and counts to the break
// On the Handover:
//    B-CARRY-COVER the Enforcer walking Vale fires no fire order, but his Covering fire is taken (Sky's call b): it pins, never hits
//    B-TALK-BLOCKER the Fixer talks the blocker down (Sky's call c): he breaks off the road, the car is free without a kill
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static Answer Move039(Run r, string op, string verb, string target) => r.G.Issue(new Order { Tick = r.G.Tick, Op = op, Do = verb, Target = target });
        static int ShotsCovering(Run r, string t) => r.G.Log.Count(l => l.Code == "shot.covering" && l.Text.StartsWith("enforcer lays covering fire on " + t + " ", StringComparison.Ordinal));

        static void FightBRange()
        {
            AddRange("B-INPUT", "today", "open", "brief 039 one input: cease, stance, panic and free are refused unknown-verb; F draws; weapons free is on whatever the start says; the fight before B (pre039) takes all four", r =>
            {
                var g = r.G;
                r.Is(g.OneInputOn && g.WeaponsFree, "one input plays, weapons free on");
                r.Do(Scout, "draw");
                foreach (var o in new[] { new Order { Op = Scout, Do = "stance", Stance = "hold" }, new Order { Op = Scout, Do = "cease" }, new Order { Op = "cell", Do = "panic" }, new Order { Op = "cell", Do = "free", On = false } })
                { o.Tick = g.Tick; var a = g.Issue(o); r.Eq(a.code, "unknown-verb", $"{o.Do} is retired"); }
                r.Is(g.WeaponsFree, "weapons free is still on");
                var off = new Start { WeaponsFree = false }; var one = Alt(r, r.Inputs!, off);
                r.Is(one.G.WeaponsFree, "a start's weaponsFree false is not read: weapons free is on");
                var old = Alt(r, r.Inputs!, new Start { Pre039 = true, WeaponsFree = false }); var og = old.G;
                r.Is(!og.OneInputOn && !og.WeaponsFree, "the fight before B reads the start's weapons free");
                og.Issue(new Order { Tick = og.Tick, Op = Scout, Do = "draw" });
                r.Is(og.Issue(new Order { Tick = og.Tick, Op = Scout, Do = "stance", Stance = "hold" }).ok && og.Issue(new Order { Tick = og.Tick, Op = Scout, Do = "cease" }).ok && og.Issue(new Order { Tick = og.Tick, Op = "cell", Do = "free", On = true }).ok, "and takes stance, cease and free");
                r.Wait(1);
            });

            AddRange("B-PIN", "today", "open", "brief 039 pinned: a round, hit or miss, pins its target for 2 s from the tick it lands, renewed by each round; a pinned shooter fires at weapons.pin.mod; with pin.cell the cell is pinned by hostile fire too, and a start's pinCell false spares it", r =>
            {
                var g = r.G; var R = g.R;
                r.Is(g.PinCellOn, "pin.cell on: the cell is pinned too (Motive's probe; Motive sets it)");
                PinSetup(r); var o1 = H(r, "officer-1"); var sc = r.Op(Scout);
                // the officer fires first, unpinned; the Scout's answer pins him
                r.Is(r.Until(() => r.Shots().Exists(s => s.by == "officer-1"), Game.T(10)), "officer-1 fires on the drawn Scout");
                var first = r.Shots().First(s => s.by == "officer-1");
                r.Eq(first.target, Scout, "at the drawn Scout, the nearest drawn agent in his effective ring");
                long d = G.Isqrt(G.D2(o1.Pos(g.Tick), sc.Pos));
                int clean = R.Accuracy(o1.Gun.W!, d, Gait.Stand, 1000, false, false, false);
                r.Eq(LoggedAcc(r, first.tick, "officer-1", first.target), clean, $"his first shot, nobody yet shot at him: the clean chance at {d / 100.0:0.0} m");
                r.Eq(g.PinLeft(Scout), first.tick + Game.T(R.PinS) + 1 - g.Tick, "the Scout is pinned from the tick the round landed (2 s, this tick counted)");
                r.Eq(g.PostureOf(Scout).Kind, "pinned-open", "pinned in the open: she cowers");
                r.Is(r.Until(() => r.Shots().Exists(s => s.by == Scout), Game.T(10)), "the Scout fights on her own: she answers");
                var mine = r.Shots().First(s => s.by == Scout);
                if (mine.tick - first.tick <= Game.T(R.PinS)) r.Eq(LoggedAcc(r, mine.tick, Scout, "officer-1"), R.Accuracy(sc.Gun.W!, G.Isqrt(G.D2(sc.Pos, o1.Pos(mine.tick))), Gait.Stand, 1000, false, true, false) * R.PinMod / 1000, "pinned herself, her answer goes at half (untrained)");
                r.Eq(o1.Gun.ShotByTick, mine.tick, "the round landed on officer-1 (Gun.ShotByTick)");
                r.Eq(g.PinLeft("officer-1"), Game.T(R.PinS) + 1 - (g.Tick - mine.tick), "officer-1 is pinned for 2 s from it");
                r.Is(g.Pins().Exists(p => p.id == "officer-1") && g.Pins().Exists(p => p.id == Scout), "the host reads both pins");
                r.Is(r.Until(() => r.Shots().Count(s => s.by == "officer-1") >= 2, Game.T(5)), "officer-1 fires again");
                var second = r.Shots().Where(s => s.by == "officer-1").ElementAt(1);
                bool pinnedThen = r.Shots().Any(s => s.by == Scout && s.tick < second.tick && second.tick - s.tick <= Game.T(R.PinS)); // his shot is decided before the tick's rounds land
                if (r.Is(pinnedThen, "the officer's second shot comes while he is pinned"))
                {
                    long d2 = G.Isqrt(G.D2(o1.Pos(second.tick), sc.Pos));
                    r.Eq(LoggedAcc(r, second.tick, "officer-1", second.target), R.Accuracy(o1.Gun.W!, d2, Gait.Stand, 1000, false, false, false) * R.PinMod / 1000, "pinned, he fires at half (weapons.pin.mod)");
                }
                // renewal: each round restarts the 2 s
                int a = r.Shots().Count(s => s.by == Scout);
                r.Is(r.Until(() => r.Shots().Count(s => s.by == Scout) > a, Game.T(5)), "the Scout's next round");
                var last = r.Shots().Last(s => s.by == Scout);
                r.Is(g.PinLeft("officer-1") == Game.T(R.PinS) + 1 - (g.Tick - last.tick), "the next round restarts his pin");
                // the cell spared: a start's pinCell false
                var b = Alt(r, r.Inputs!, new Start { PinCell = false }); var bg = b.G;
                PinSetup(b);
                b.Until(() => b.Shots().Exists(s => s.by == "officer-1"), Game.T(10));
                r.Is(!bg.PinCellOn && bg.PinLeft(Scout) == 0 && bg.PostureOf(Scout).Kind == "stand", "pinCell false: the Scout shot at is not pinned, she stands");
                r.Is(b.Until(() => bg.PinLeft("officer-1") > 0, Game.T(10)), "and still pins officer-1 with her answer");
            });

            AddRange("B-STICKY", "today", "open", "brief 039 sticky focus: a focus whose target leaves reach waits (no fire.out-of-range, no lapse) while the agent fights on its own, then takes him back with aimNew; a holster ends it; the fight before B ends it out of range", r => StickyCase(r, false));
            AddRange("B-STICKY-OLD", "pre039", "open", "the same orders in the fight before B: the order ends out of range (fire.out-of-range)", r => StickyCase(r, true));

            AddRange("B-SNAP", "today", "open", "brief 039 readable cover: a drawn agent's move in a fight ending within 1.5 m of a free cover point within 10 m of him snaps to its kneel point (the move-snap test reads it); holstered, or a point over 10 m off, it doesn't; there she kneels, and ducks when shot at", r =>
            {
                var g = r.G; var R = g.R; var sc = r.Op(Scout); int sec = g.Tick / Game.Hz;
                r.Eq((g.CoverSnapOn, g.CoverSnapCm, g.CoverMarksCm), (true, R.TakeCoverSnapCm, R.TakeCoverReachCm), "the snap is on out of the lab: 1.5 m, cover within 10 m");
                Spawned(r, "police", "eff", 1); var op1 = H(r, "officer-1").Pos(g.Tick);
                // a cover point that shields against officer-1 and lies in his effective ring (he will fire on her there)
                var cpN = g.S.Cover.Where(c => c.Present(sec) && c.Q >= R.CoverQPartial && g.S.Grid.Free(c.At) && CoverRules.Protects(c, op1, sec, R.PointBlankCm) && G.Within(c.At, op1, H(r, "officer-1").Gun.W!.EffCm - 200) && G.Dist(c.At, op1) > R.PointBlankCm + 200)
                    .OrderBy(c => G.D2(c.At, sc.Pos)).ThenBy(c => c.Id, StringComparer.Ordinal).FirstOrDefault();
                if (!r.Is(cpN != null, "a cover point that shields against officer-1, in his reach")) return;
                var cp = cpN!;
                // she walks to 4 m from it first (a point of the grid), holstered
                Pt start = sc.Pos;
                foreach (var (dx, dy) in new[] { (-400, 0), (0, -400), (0, 400), (400, 0), (-300, -300), (-300, 300) }) { var q = new Pt(cp.At.X + dx, cp.At.Y + dy); if (g.S.Grid.Free(q) && g.S.Grid.Route(sc.Pos, q) != null) { start = q; break; } }
                r.To(r.SendTo(Scout, start));
                Pt? Click() { foreach (var (dx, dy) in new[] { (100, 0), (0, 100), (-100, 0), (0, -100), (70, 70), (-70, 70), (70, -70), (-70, -70) }) { var q = new Pt(cp.At.X + dx, cp.At.Y + dy); if (g.S.Grid.Free(q) && g.S.Grid.LineFree(q, cp.At) && g.PreviewSnap(OpId.Scout, q)?.Id == cp.Id) return q; } return null; }
                r.Is(Click() == null && !g.PreviewMoveSnap(OpId.Scout, cp.At).Snaps, "holstered: no snap");
                r.Do(Scout, "draw"); r.Wait(Game.T(1) + 1);
                var click = Click();
                if (!r.Is(click.HasValue, $"drawn, a click 1 m off {cp.Id} snaps to it")) return;
                var ms = g.PreviewMoveSnap(OpId.Scout, click!.Value);
                r.Is(ms.Snaps && ms.Cover == cp.Id && ms.EndAt == cp.At && ms.Faces == cp.Faces, "the move-snap test: it snaps, to that point, its kneel point and the way it shields");
                var farC = g.S.Cover.Where(c => c.Present(sec) && c.Q >= R.CoverQPartial && !G.Within(c.At, sc.Pos, R.TakeCoverReachCm + 200) && g.S.Grid.Free(c.At)).OrderBy(c => G.D2(c.At, sc.Pos)).ThenBy(c => c.Id, StringComparer.Ordinal).FirstOrDefault();
                if (farC != null) r.Is(g.PreviewSnap(OpId.Scout, farC.At) == null, $"a cover point over 10 m from her ({farC.Id}, {G.Dist(farC.At, sc.Pos) / 100} m) is no snap");
                r.SendTo(Scout, click.Value);
                r.Is(sc.Walk != null && sc.Walk.End == cp.At && g.CoverOf(OpId.Scout) == cp.Id, $"the move ends on the kneel point and holds it ({sc.Walk?.End} {cp.At} {g.CoverOf(OpId.Scout)}, from {sc.Pos})");
                r.Until(() => sc.Walk == null, Game.T(10));
                r.Is(g.CoverOf(OpId.Scout) == cp.Id && g.PostureOf(Scout).Cover == cp.Id && g.PostureOf(Scout).KneelAt == cp.At, "she holds it, at its kneel point");
                r.Is(r.Until(() => g.PostureOf(Scout).Kind == "kneel" || g.PostureOf(Scout).Kind == "pinned-cover", Game.T(2)), $"in cover she kneels, or ducks if shot at ({g.PostureOf(Scout).Kind})");
                r.Is(r.Until(() => g.PostureOf(Scout).Kind == "pinned-cover", Game.T(10)), "shot at in cover, she ducks: pinned-cover");
                r.Is(g.PostureOf(Scout).Shields, "her cover shields her from the officer who shot at her");
            });

            AddRange("B-COVER-FIRE", "today", "open", "brief 039 Covering fire: for 4 s the Enforcer fires a round every 0.5 s at a hostile in his carbine's reach that never hits, and everyone within 3 m of it is pinned until it ends; a pinned rival holds (no advance); the next is refused move-cooling until 20 s after", r =>
            {
                var g = r.G; var R = g.R; var e = r.Op(Enf);
                r.Do(Enf, "draw"); r.Wait(Game.T(2));
                Spawned(r, "rivals", "beyond", 2); var r1 = H(r, "rival-1"); var r2 = H(r, "rival-2"); int t0 = g.Tick;
                var p1 = r1.Pos(g.Tick); var p2 = r2.Pos(g.Tick);
                r.Is(!G.Within(p1, p2, R.CoverFireRCm) && G.Dist(p1, e.Pos) <= e.Gun.W!.MaxCm, $"rival-2 is {G.Dist(p1, p2) / 100.0:0.0} m from rival-1 (beyond 3 m); rival-1 {G.Dist(p1, e.Pos) / 100.0:0.0} m from the Enforcer (in the carbine's reach)");
                var a = Move039(r, Enf, "covering-fire", "rival-1");
                r.Is(a.ok, $"covering fire on rival-1 taken{(a.ok ? "" : ": " + a.code)}");
                var cf = g.CoveringFireOf(); r.Is(cf.Target == "rival-1" && cf.Until == t0 + Game.T(R.CoverFireS) && g.MoveOf(OpId.Enforcer).Running, "it runs 4 s (the host reads it)");
                int bad = 0; bool r2moved = false;
                while (!g.Over && g.Tick < cf.Until - 1)
                {
                    g.Step();
                    if (r1.Gun.St == "advance" || g.PinLeft("rival-1") <= 0) bad++;
                    if (r2.Gun.St == "advance" || r2.Moving(g.Tick)) r2moved = true;
                }
                r.Eq(bad, 0, "rival-1, within 3 m of its target, stays pinned the whole 4 s and never advances (a step out of the line is still his)");
                r.Is(r2moved && g.PinLeft("rival-2") == 0, "rival-2, 3.5 m off, is not pinned and advances");
                r.Eq(ShotsCovering(r, "rival-1"), Game.T(R.CoverFireS) / R.CoverFireEveryTicks, "a round every 0.5 s, 8 in its 4 s (shot.covering)");
                r.Eq(r1.Gun.Health, r1.Gun.Max, "none hits");
                var no = Move039(r, Enf, "covering-fire", "rival-1");
                r.Is(!no.ok && no.code == "move-cooling" && no.tick == t0 + Game.T(R.MoveReadyS), $"again at once: move-cooling, ready at {Report.Clock(t0 + Game.T(R.MoveReadyS))} (got {no.code} {no.tick})");
                r.Eq(g.PreviewMove(new Order { Tick = g.Tick, Op = Enf, Do = "covering-fire", Target = "rival-1" }).code, "move-cooling", "the move disc reads cooling");
                r.Is(r.Until(() => g.CoveringFireOf().Target.Length == 0, Game.T(3)), "it ends at its time");
                r.Do(Enf, "holster"); // else he fights on his own and his rounds keep rival-1 pinned
                r.Is(r.Until(() => r1.Gun.St != "alerted" || r1.Down, Game.T(6)), $"once his pin runs out, rival-1 advances ({r1.Gun.St})");
                r.Eq(Move039(r, Scout, "covering-fire", "rival-2").code, "only-enforcer", "the Enforcer's move alone");
            });

            AddRange("B-MARK", "today", "open", "brief 039 Mark: the Scout marks a hostile she sees within 30 m; for 10 s his cover doesn't count against the cell (the Enforcer's line on him reads the open chance), then it does again; the Scout's alone", r =>
            {
                var g = r.G; var R = g.R;
                r.Do(Enf, "draw"); r.Do(Scout, "draw"); r.Wait(Game.T(2));
                Spawned(r, "police", "cover", 4); // cover slots 3 and 4: the car boxes (full cover)
                var tid = g.Hostiles.Where(h => g.S.Cover.Find(c => c.At == h.Pos(g.Tick)) is CoverPoint c && c.Q >= R.CoverQFull).Select(h => h.Id).OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault();
                if (!r.Is(tid != null, "an officer in full cover")) return;
                FireLine? Line() => LineOf(r, Enf, tid!);
                var before = Line();
                if (!r.Is(before != null && before.Line && before.Cover == "full", $"the Enforcer's line on {tid}: full cover ({before?.Cover})")) return;
                r.Eq(Move039(r, Fixer, "mark", tid!).code, "only-scout", "Mark is the Scout's");
                var a = Move039(r, Scout, "mark", tid!);
                if (!r.Is(a.ok, $"the Scout marks {tid}{(a.ok ? "" : ": " + a.code)}")) return;
                var mk = g.MarkOf(); r.Is(mk.Target == tid && mk.Left == Game.T(R.MarkS), "the host reads the mark, 10 s");
                var after = Line()!;
                r.Eq(after.Acc, Math.Min(1000, before!.Acc * 1000 / R.ModCover), "marked, his cover doesn't count: the chance ×1000/mod-cover");
                r.Wait(Game.T(R.MarkS));
                var back = Line();
                r.Is(g.MarkOf().Target.Length == 0 && back != null && back.Acc < after.Acc, "after 10 s the mark is gone and his cover counts again");
                var far = Spawn(r, "response", "near", 1); // a response officer walks in from the far end, 40 m off
                if (far.ok) r.Eq(Move039(r, Scout, "mark", "response-1").code, "move-cooling", "the Scout's move cools 20 s");
            });

            AddRange("B-STEAM", "today", "open", "brief 039 Steam: the Tech opens a heat-main grille within 24 m (a start's test grilles: the site names none yet); for 8 s no line passes within 4 m of it, then it clears; out of reach, out-of-range; no grille in reach, steam-no-grille; an id that is no grille, no-target", r =>
            {
                var g = r.G; var R = g.R;
                r.Do(Enf, "draw"); r.Wait(Game.T(2));
                Spawned(r, "police", "beyond", 1); var o1 = H(r, "officer-1");
                var line = LineOf(r, Enf, "officer-1");
                if (!r.Is(line != null && line.Line, "the Enforcer has a line on officer-1")) return;
                r.Eq(Move039(r, Tech, "steam", "nope").code, "no-target", "an id that is no grille: no-target");
                r.Eq(Move039(r, Tech, "steam", "grille-far").code, "out-of-range", "the far grille, out of the Tech's 24 m");
                var a = Move039(r, Tech, "steam", "grille-mid");
                if (!r.Is(a.ok, $"the Tech opens grille-mid{(a.ok ? "" : ": " + a.code)}")) return;
                r.Is(g.Steams().Count == 1 && g.Steams()[0].Left == Game.T(R.SteamS) && g.HeatMains().Count == 2, "the host reads the steam, 8 s");
                r.Wait(1);
                r.Is(!LineOf(r, Enf, "officer-1")!.Line, "no line through the steam");
                r.Wait(Game.T(R.SteamS));
                r.Is(LineOf(r, Enf, "officer-1")!.Line && g.Steams().Count == 0, "after 8 s the line is back");
                var none = Alt(r, r.Inputs!, RangePreset("today"));
                r.Eq(Move039(none, Tech, "steam", "").code, "steam-no-grille", "a run with no heat-main grille: steam-no-grille");
                r.Eq(Move039(r, Fixer, "steam", "grille-mid").code, "only-tech", "the Tech's alone");
            }).Start.Grilles.AddRange(new[] { ("grille-mid", new Pt(1200, 2050)), ("grille-far", new Pt(4000, 2000)) });

            AddRange("B-TALK", "today", "open", "brief 039 Talk down: never police (talk-police), not before one of the crew is down (talk-none-down), not on an unhurt rival (talk-not-hurt), within 12 m; then the rival breaks for the crew's car, counts to the crew's break as one down, and the Fixer shouts 2 s in the open", r =>
            {
                var g = r.G; var R = g.R; var fx = r.Op(Fixer);
                Spawned(r, "police", "cover", 1); // 21 m off, out of his reach: he fires on nobody
                r.Eq(Move039(r, Fixer, "talk-down", "officer-1").code, "talk-police", "never the police");
                Spawned(r, "rivals", "near", 3);
                r.Eq(Move039(r, Scout, "talk-down", "rival-1").code, "only-fixer", "Talk down is the Fixer's");
                r.Eq(Move039(r, Fixer, "talk-down", "rival-2").code, "talk-none-down", "none of the crew is down yet");
                r.Do(Enf, "draw"); r.Do(Scout, "draw"); r.Do(Tech, "draw");
                var rv1 = H(r, "rival-1"); var rv2 = H(r, "rival-2"); var rv3 = H(r, "rival-3");
                g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "fire", Target = "rival-1", Focus = true });
                g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "fire", Target = "rival-1", Focus = true });
                g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "fire", Target = "rival-2", Focus = true });
                r.Until(() => rv1.Down || rv2.Gun.Health < rv2.Gun.Max || fx.Down, Game.T(20));
                if (!rv1.Down) r.Until(() => rv1.Down || fx.Down, Game.T(20));
                if (!r.Is(rv1.Down && !fx.Down, $"rival-1 is down ({rv1.Gun.Health}), the Fixer stands")) return;
                if (rv3.Gun.Health == rv3.Gun.Max && !rv3.Down && rv3.Gun.St != "break") r.Eq(Move039(r, Fixer, "talk-down", "rival-3").code, "talk-not-hurt", "rival-3 isn't hurt");
                foreach (var op in new[] { Scout, Enf, Tech }) if (!r.Op(op).Down) r.Do(op, "holster"); // the rest is the Fixer's
                r.Until(() => rv2.Gun.Health < rv2.Gun.Max || rv2.Down, Game.T(10));
                if (!r.Is(!rv2.Down && rv2.Gun.Health < rv2.Gun.Max && rv2.Gun.St != "break", $"rival-2 is hurt and up ({rv2.Gun.Health}/{rv2.Gun.Max}, {rv2.Gun.St})")) return;
                if (!G.Within(fx.Pos, rv2.Pos(g.Tick), R.TalkReachCm)) { r.Eq(Move039(r, Fixer, "talk-down", "rival-2").code, "out-of-range", "beyond 12 m: out-of-range"); r.SendTo(Fixer, g.S.Grid.NearestFree(rv2.Pos(g.Tick) + new Pt(-900, 0)) ?? fx.Pos); r.Until(() => fx.Walk == null, Game.T(15)); }
                var ans = Move039(r, Fixer, "talk-down", "rival-2");
                if (!r.Is(ans.ok, $"the Fixer talks rival-2 down{(ans.ok ? "" : ": " + ans.code + " " + ans.why)}")) return;
                r.Is(rv2.Gun.St == "break" && rv2.Gun.Target.Length == 0, "he lowers his gun and breaks");
                r.Is(fx.Busy != null && fx.Busy.Do == "talk-down" && fx.BusyEnd == g.Tick + Game.T(R.TalkShoutS) && fx.Walk == null, "the Fixer shouts 2 s, standing");
                r.Is(rv3.Down || rv3.Gun.St == "break" || rv3.Gun.St == "gone", $"one down and one talked down of three: more than half, the crew breaks (rival-3 {rv3.Gun.St})");
                r.Is(g.Log.Exists(l => l.Code == "move.talk-down") && g.Log.Exists(l => l.Code == "ai.break"), "logged move.talk-down, then ai.break");
                r.Eq(Move039(r, Fixer, "talk-down", "rival-3").code, "agent-busy", "busy shouting");
            });
        }

        /// <summary>B-PIN's set-up: the Scout walks within 12 m of the eff slot's first stand and draws; one officer spawns there.</summary>
        static void PinSetup(Run r)
        {
            var g = r.G; var at = g.S.Grid.NearestFree(new Pt(600, 2000)) ?? g.S.Stand("crew:scout");
            r.To(r.SendTo(Scout, at)); r.Do(Scout, "draw"); r.Wait(Game.T(2));
            Spawned(r, "police", "eff", 1);
        }

        static void StickyCase(Run r, bool old)
        {
            var g = r.G; var sc = r.Op(Scout);
            r.Do(Scout, "draw"); r.Wait(Game.T(2));
            Spawned(r, "police", "eff", 1); // officer-1, 15 m off: in her pistol's 16 m
            var a = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "fire", Target = "officer-1", Focus = true });
            if (!r.Is(a.ok, $"the Scout's focus on officer-1{(a.ok ? "" : ": " + a.why)}")) return;
            r.Is(r.Until(() => r.Shots().Exists(s => s.by == Scout && s.target == "officer-1"), Game.T(4)), "she fires on him");
            var away = g.S.Grid.NearestFree(new Pt(300, 900)) ?? sc.Pos;
            var o1 = H(r, "officer-1").Pos(g.Tick); int max = sc.Gun.W!.MaxCm;
            if (!r.Is(G.Dist(away, o1) > max, $"a walk away takes officer-1 out of her reach ({G.Dist(away, o1) / 100.0:0.0} m)")) return;
            int at = r.SendTo(Scout, away); r.Until(() => sc.Walk == null || sc.Down, at - g.Tick + 5);
            Spawned(r, "police", "near", 1); // officer-2 by her (near slot 1): someone to fight on her own
            r.Wait(Game.T(3) + 5); // past the line lapse (3 s) since she left his reach
            if (old)
            {
                r.Is(sc.Gun.Order.Length == 0 && g.Log.Exists(l => l.Code == "fire.out-of-range"), "before B, out of range ends the order (fire.out-of-range)");
                return;
            }
            r.Eq(sc.Gun.Order, "officer-1", "the focus still stands");
            r.Is(!g.Log.Exists(l => l.Code == "fire.out-of-range" || l.Code == "fire.lapses"), "no out-of-range end, no lapse");
            r.Eq(g.Log.Count(l => l.Code == "fire.focus-waits"), 1, "fire.focus-waits once");
            r.Eq(g.FocusOf(OpId.Scout).State, "out-of-reach", "the host reads why it waits");
            r.Is(r.Shots().Exists(s => s.by == Scout && s.target == "officer-2") || sc.Down, "meanwhile she fights on her own (officer-2)");
            if (sc.Down) return;
            r.SendTo(Scout, g.S.Stand("crew:scout"));
            int inReach = -1, end = g.Tick + Game.T(15);
            while (!g.Over && g.Tick < end && !sc.Down) { g.Step(); if (inReach < 0 && G.Within(sc.Pos, o1, max)) inReach = g.Tick; if (inReach >= 0 && r.Shots().Exists(s => s.by == Scout && s.target == "officer-1" && s.tick > inReach)) break; }
            var first = r.Shots().FirstOrDefault(s => s.by == Scout && s.target == "officer-1" && s.tick > inReach);
            if (!r.Is(inReach >= 0 && first.by != null, "back in reach, she takes him back")) return;
            r.Is(first.tick - inReach >= g.AimNewT, $"after aimNew to aim ({first.tick - inReach} ticks)");
            r.Do(Scout, "holster"); r.Eq(sc.Gun.Order, "", "a holster ends the focus");
        }

        static void FightBCases()
        {
            Add("B-CARRY-COVER", "039", "Sky's call (b), brief 039: route A into the safe fight; the Enforcer walking Vale fires no fire order (fire-carrying-vale), but his Covering fire is taken: for 4 s, one-handed, every rival within 3 m of its target is pinned, no round hits, and he walks on", r =>
            {
                var g = r.G; var R = g.R;
                RouteA(r, reroute: true);
                if (!r.Is(g.FightBranch == "safe" && g.Vale == ValeAt.WithEnforcer, $"the Enforcer has Vale ({g.FightBranch} {g.Vale})")) return;
                Support(r); ToAmbush(r);
                if (!r.Is(g.FightSprung != Never, "the ambush springs")) return;
                var e = r.Op(Enf);
                Hostile? T() => g.Hostiles.Where(h => h.Present && !h.Down && h.Gun.St != "break" && h.Gun.St != "gone" && g.PreviewMove(new Order { Tick = g.Tick, Op = Enf, Do = "covering-fire", Target = h.Id }).ok).OrderBy(h => G.D2(h.Pos(g.Tick), e.Pos)).ThenBy(h => h.Id, StringComparer.Ordinal).FirstOrDefault();
                r.Until(() => T() != null || g.Over, Game.T(10));
                var t = T(); if (!r.Is(t != null, "a rival in his carbine's reach on a line")) return;
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "fire", Target = t!.Id }).code, "fire-carrying-vale", "walking Vale, no fire order");
                bool walking = e.Walk != null;
                var a = Move039(r, Enf, "covering-fire", t.Id);
                if (!r.Is(a.ok, $"his Covering fire on {t.Id} is taken, Vale on his shoulder{(a.ok ? "" : ": " + a.code)}")) return;
                r.Is(walking == (e.Walk != null), "it doesn't stop his walk");
                int until = g.CoveringFireOf().Until; bool pinned = false;
                while (!g.Over && g.Tick < until - 1) { g.Step(); if (g.PinLeft(t.Id) > 0 && g.Vale == ValeAt.WithEnforcer) pinned = true; }
                r.Is(pinned, $"{t.Id} is pinned while he carries");
                r.Is(ShotsCovering(r, t.Id) > 0 && g.Log.Exists(l => l.Code == "move.covering-fire" && l.Text.Contains("holding Vale")), "the rounds go out (shot.covering), one-handed, holding Vale");
                r.Is(!g.Log.Exists(l => l.Code == "shot.hit" && l.Text.StartsWith("enforcer fires on ", StringComparison.Ordinal) && l.Tick >= g.FightSprung), "no hit by the Enforcer in the carry");
            });

            Add("B-TALK-BLOCKER", "039", "Sky's call (c), brief 039: route A into the safe fight; the Scout and the Tech drop a rival, the Fixer clips the blocker, then talks him down from 12 m: he breaks off the road (blocker.out), the car is free without a kill, and the Fixer stands 2 s in the open", r =>
            {
                var g = r.G; var R = g.R;
                RouteA(r, reroute: true);
                if (!r.Is(g.FightBranch == "safe" && g.Vale == ValeAt.WithEnforcer, $"the Enforcer has Vale ({g.FightBranch} {g.Vale})")) return;
                Support(r); ToAmbush(r);
                if (!r.Is(g.FightSprung != Never, "the ambush springs")) return;
                r.SendTo(Enf, r.Stand("d:Getaway bay"));
                var fx = r.Op(Fixer); bool talked = false; int talkAt = -1; Hostile? bl = null; int end = g.Tick + Game.T(60);
                while (!g.Over && g.Tick < end && !talked)
                {
                    bl ??= g.Hostiles.Find(h => h.Blocker);
                    if (bl != null && (bl.Down || bl.Gun.St == "break" || bl.Gun.St == "gone")) break;
                    var up = g.Hostiles.Where(h => h.Present && !h.Down && h.Gun.St != "break" && h.Gun.St != "gone" && !h.Blocker).OrderBy(h => h.Id, StringComparer.Ordinal).ToList();
                    foreach (var op in new[] { Scout, Tech })
                    {
                        var o = r.Op(op); if (o.Down || o.InCar || o.Busy != null || o.Gun.W == null) continue;
                        if (up.Count > 0 && o.Gun.Order != up[0].Id) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "fire", Target = up[0].Id, Focus = true });
                        if (up.Count == 0 && o.Gun.Drawn && o.Gun.HolsterAt < 0) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "holster" });
                    }
                    if (bl != null && !fx.Down && fx.Busy == null)
                    {
                        bool hurt = bl.Gun.Health < bl.Gun.Max, downed = g.Hostiles.Exists(h => h.Id.StartsWith("rival-", StringComparison.Ordinal) && h.Down);
                        if (!hurt && fx.Gun.Order != bl.Id) g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "fire", Target = bl.Id });
                        if (hurt && fx.Gun.Drawn && fx.Gun.HolsterAt < 0) g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "holster" });
                        if (hurt && downed)
                        {
                            var bp = bl.Pos(g.Tick);
                            if (!G.Within(fx.Pos, bp, R.TalkReachCm - 100) && fx.Walk == null) r.SendTo(Fixer, g.S.Grid.NearestFree(bp + new Pt((fx.Pos.X - bp.X) * 1000 / Math.Max(1, (int)G.Dist(fx.Pos, bp)) * (R.TalkReachCm - 300) / 1000, (fx.Pos.Y - bp.Y) * 1000 / Math.Max(1, (int)G.Dist(fx.Pos, bp)) * (R.TalkReachCm - 300) / 1000)) ?? fx.Pos);
                            else if (g.PreviewMove(new Order { Tick = g.Tick, Op = Fixer, Do = "talk-down", Target = bl.Id }).ok)
                            {
                                var a = Move039(r, Fixer, "talk-down", bl.Id); talked = a.ok; talkAt = g.Tick;
                                r.Is(a.ok, $"the Fixer talks the blocker {bl.Id} down at {Report.Clock(g.Tick)}{(a.ok ? "" : ": " + a.code)}");
                            }
                        }
                    }
                    if (!talked) g.Step();
                }
                if (!r.Is(talked && bl != null, $"the blocker hurt and one of the crew down, the Fixer talks him down ({(bl == null ? "no blocker" : bl.Id + " " + bl.Gun.St + " " + bl.Gun.Health + "/" + bl.Gun.Max)}, {g.Result} {g.FailCode} at {Report.Clock(g.Tick)})")) return;
                r.Is(bl!.Gun.St == "break" && !bl.Down, "he breaks, alive");
                r.Is(fx.Busy != null && fx.BusyEnd == talkAt + Game.T(R.TalkShoutS), "the Fixer shouts 2 s, busy in the open");
                g.Step();
                r.Is(g.Log.Exists(l => l.Code == "blocker.out" && l.Tick >= talkAt), "blocker.out: nobody blocks the lane now");
                r.Is(!g.CarHolders().Contains(bl.Id), "he holds the car no more");
                r.Is(!g.Log.Exists(l => l.Code == "fight.down" && l.Text.EndsWith(" drops " + bl.Id + ".", StringComparison.Ordinal)), "without a kill");
            });
        }
    }
}
