// Engine 0.35.0 scenarios: brief 039 B, cover and pin (Sky picked B on 2026-10-26; GDD v0.28.0 §06; rulebook weapons.oneInput, focus,
// takeCover, pin, moves; GameCoverPin.cs). Ids avoid the S prefix (the 28). On the range (its today preset is B):
//    B-INPUT       one input: cease, stance, panic and free are unknown verbs; F draws; weapons free is on whatever the start says; pre039 takes them
//    B-PIN         a round pins its target for 2 s (hit or miss), renewed by each round; a pinned shooter fires at half; the rulebook spares the cell (0.35.1), a start's pinCell pins it
//    B-STICKY      the focus waits out of reach (no out-of-range end, no lapse) while the agent fights on its own, then takes him back after aimNew
//    B-SNAP        a drawn agent's move in a fight snaps to the kneel point of cover within 10 m; he kneels there, and still kneels when shot at
//                  (the cell isn't pinned, engine 0.35.1)
//    B-MOVES-OFF   engine 0.35.1 (Sky 2026-10-26): Mark, Talk down, Steam and Covering fire are out of play: unknown-verb, the host's reads empty
// On the Handover:
//    B-MOVES-OFF-SAFE route A's safe fight: no Covering fire with Vale, no Talk down on the blocker, no Mark, no Steam (unknown-verb)
// Retired at 0.35.1 with the moves: B-COVER-FIRE, B-MARK, B-STEAM, B-TALK, B-CARRY-COVER, B-TALK-BLOCKER (git show HEAD:sim/Suborned.Sim/ScenariosFightB.cs).
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

            AddRange("B-PIN", "today", "open", "brief 039 pinned: a round, hit or miss, pins its target for 2 s from the tick it lands, renewed by each round; a pinned shooter fires at weapons.pin.mod; with pin.cell (a start's pinCell true here) the cell is pinned by hostile fire too; the rulebook's pin.cell false (engine 0.35.1, Motive's ruling 7) spares it", r =>
            {
                var g = r.G; var R = g.R;
                r.Is(g.PinCellOn && !R.PinCell, "the start's pinCell true: the cell is pinned too (the rulebook's pin.cell is false, Motive's ruling 7)");
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
                // the cell spared: the rulebook's pin.cell false (engine 0.35.1)
                var b = Alt(r, r.Inputs!, RangePreset("today")); var bg = b.G;
                PinSetup(b);
                b.Until(() => b.Shots().Exists(s => s.by == "officer-1"), Game.T(10));
                r.Is(!bg.PinCellOn && bg.PinLeft(Scout) == 0 && bg.PostureOf(Scout).Kind == "stand", "pin.cell false: the Scout shot at is not pinned, she stands");
                r.Is(b.Until(() => bg.PinLeft("officer-1") > 0, Game.T(10)), "and still pins officer-1 with her answer");
            }).Start.PinCell = true; // engine 0.35.1: the rulebook spares the cell; this case pins it by the start to keep the half-rate checks

            AddRange("B-STICKY", "today", "open", "brief 039 sticky focus: a focus whose target leaves reach waits (no fire.out-of-range, no lapse) while the agent fights on its own, then takes him back with aimNew; a holster ends it; the fight before B ends it out of range", r => StickyCase(r, false));
            AddRange("B-STICKY-OLD", "pre039", "open", "the same orders in the fight before B: the order ends out of range (fire.out-of-range)", r => StickyCase(r, true));

            AddRange("B-SNAP", "today", "open", "brief 039 readable cover: a drawn agent's move in a fight ending within 1.5 m of a free cover point within 10 m of him snaps to its kneel point (the move-snap test reads it); holstered, or a point over 10 m off, it doesn't; there she kneels, and still kneels when shot at (the cell isn't pinned, engine 0.35.1)", r =>
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
                r.Is(r.Until(() => sc.Gun.ShotByTick >= 0, Game.T(10)), "the officer shoots at her in cover");
                r.Eq(g.PostureOf(Scout).Kind, "kneel", "shot at in cover she kneels on: the cell isn't pinned (pin.cell false, engine 0.35.1; with pin.cell she ducks, pinned-cover)");
                r.Is(g.PostureOf(Scout).Shields, "her cover shields her from the officer who shot at her");
            });

            AddRange("B-MOVES-OFF", "today", "open", "engine 0.35.1 (Sky 2026-10-26, via Main): the four moves are out of play (weapons.moves.on false): Mark, Talk down, Steam and Covering fire are refused unknown-verb by their own agents, a start's test grilles or not, and the host's move reads stay empty", r =>
            {
                var g = r.G; var R = g.R;
                r.Is(!R.MovesOn, "weapons.moves.on false");
                Spawned(r, "rivals", "near", 3);
                foreach (var op in new[] { Enf, Scout, Tech, Fixer }) r.Do(op, "draw");
                r.Until(() => g.Hostiles.Exists(h => h.Down), Game.T(20));
                foreach (var (op, verb, t) in new[] { (Scout, "mark", "rival-1"), (Fixer, "talk-down", "rival-2"), (Tech, "steam", "grille-mid"), (Enf, "covering-fire", "rival-3"), (Fixer, "mark", "rival-1") })
                {
                    r.Eq(Move039(r, op, verb, t).code, "unknown-verb", $"{op} {verb}: unknown-verb");
                    r.Eq(g.PreviewMove(new Order { Tick = g.Tick, Op = op, Do = verb, Target = t }).code, "unknown-verb", $"{op} {verb} previewed: unknown-verb");
                }
                foreach (var id in new[] { OpId.Scout, OpId.Fixer, OpId.Tech, OpId.Enforcer }) r.Eq(g.MoveOf(id).Verb, "", $"{id}: no move to show");
                r.Is(g.MarkOf().Target.Length == 0 && g.Steams().Count == 0 && g.CoveringFireOf().Target.Length == 0, "no mark, no steam, no covering fire");
                r.Is(!g.Log.Exists(l => l.Code.StartsWith("move.", StringComparison.Ordinal)), "no move logged");
                r.Wait(1);
            }).Start.Grilles.Add(("grille-mid", new Pt(1200, 2050)));
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
            Add("B-MOVES-OFF-SAFE", "039", "engine 0.35.1 (Sky 2026-10-26, via Main): route A into the safe fight; with the moves out of play the Enforcer walking Vale gets no Covering fire and the Fixer can't talk the blocker down (unknown-verb): the car comes free by the fight", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true);
                if (!r.Is(g.FightBranch == "safe" && g.Vale == ValeAt.WithEnforcer, $"the Enforcer has Vale ({g.FightBranch} {g.Vale})")) return;
                Support(r); ToAmbush(r);
                if (!r.Is(g.FightSprung != Never, "the ambush springs")) return;
                var bl = g.Hostiles.Find(h => h.Blocker);
                var t = bl?.Id ?? g.Hostiles.First(h => h.Present && !h.Down).Id;
                r.Eq(Move039(r, Enf, "covering-fire", t).code, "unknown-verb", "walking Vale, no Covering fire: unknown-verb");
                r.Eq(Move039(r, Fixer, "talk-down", t).code, "unknown-verb", "no Talk down on the blocker: unknown-verb");
                r.Eq(Move039(r, Scout, "mark", t).code, "unknown-verb", "no Mark: unknown-verb");
                r.Eq(Move039(r, Tech, "steam", "heat-main-square").code, "unknown-verb", "no Steam: unknown-verb");
                r.Is(!g.Log.Exists(l => l.Code.StartsWith("move.", StringComparison.Ordinal)), "no move logged");
                r.Wait(1);
            });
        }
    }
}
