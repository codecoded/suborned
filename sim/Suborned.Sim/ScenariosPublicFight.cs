// A public fight (Sky 2026-10-16, via Aster: "it should be per fight, and depends who you're fighting - a rival gang that has menace
// level authority is a +1"; Motive, briefs/014/motive-standing.md, a public fight; rulebook fight.public). One scenario a row of the
// outcomes table, on the real block and the promoted cast, with the crowd in unless a scenario says otherwise.
//  N95  feared rivals driven off: route A's safe final fight, the rivals break: one fight, +1 seen-fight-rivals when it ends; with
//       Inez's word the district ends at +4, Hero (Motive's route A row)
//  N96  a rival fight not won: the Enforcer fires one shot on the feared rival crew as it arrives, then the cell withdraws: 0
//  N97  firing first on rivals nobody fears: N96's orders with neutral rivals: −1 seen-fight-first
//  N98  a fight with security: the patrol shot down from the walk north of the Office at 4:30: −1 seen-fight-security, once, 30 s after the last shot
//  N99  a mixed fight: the patrol shot down, then the rival crew its gunfire draws, driven off inside 30 s: one fight, −1 (authority outranks)
//  N100 a fight nobody heard: N98's orders with no crowd and no cast within 30 m: no move, steady
//  N101 two fights 30 s apart: N99's orders with a wait before the rivals (the fight's end, people back): two fights, −1 and +1 (N99 is the closer twin: one)
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The walk north of the Office, 26 m from the public door on its west face; the Enforcer fires at 4:30 (ShotAt), when the
        /// patrol's sweep is 23 m off and a person of the city crossing from the square is within 30 m. Re-sited 2026-10-19 (Keel, brief 023:
        /// the door on the west face, the sweep along the forecourt): was a corner of the square at (2000, -1000) firing on arrival at 0:30, but
        /// the sweep is 60 m from the square now and nobody of the city is near the forecourt before 3:30.</summary>
        static readonly Pt ShotCorner = new Pt(4300, -2600);
        const int ShotAt = 2700;

        static string Moves(Game g) => string.Join(", ", FightMoves(g).Select(m => $"{m.delta.ToString(System.Globalization.CultureInfo.InvariantCulture)} {m.cause} {m.who}"));
        static List<(int tick, int delta, string cause, string who)> FightMoves(Game g) => g.StandingMoves.FindAll(m => m.cause.StartsWith("seen-fight-", StringComparison.Ordinal));

        /// <summary>The civilians within the public reach of an operative now: the cast by role, then (crowd) the city's people.</summary>
        static int HeardBy(Game g, string op, bool crowd)
        {
            int n = 0; var at = g.OpOf(Sim.Op.Parse(op)).Pos;
            foreach (var p in g.People) { var q = g.PersonPos(p); if (p.Civilian && q.HasValue && G.Within(q.Value, at, g.R.FightPublicCm)) n++; }
            if (crowd) foreach (var w in g.Walkers) { var q = g.CrowdPos(w); if (w.P.Civilian && q.HasValue && G.Within(q.Value, at, g.R.FightPublicCm)) n++; }
            return n;
        }

        /// <summary>The Enforcer shoots the patrol robot down from the walk north of the Office at 4:30; returns the tick it went down.</summary>
        static int PatrolShotDown(Run r)
        {
            var g = r.G;
            r.To(r.SendTo(Enf, ShotCorner)); r.To(Math.Max(g.Tick, ShotAt));
            r.Fire(Enf, "patrol");
            r.Is(r.Until(() => g.PatrolDown, T(60)), $"the patrol is down at {Report.Clock(g.Tick)}");
            return g.Tick;
        }

        /// <summary>The rival crew arrives (3:10) by its car; the Enforcer, waiting beside it out of their way, fires on them as they come.</summary>
        static void FireOnArrivingRivals(Run r, bool once)
        {
            var g = r.G;
            r.To(r.SendTo(Enf, Beside(r, g.S.Stand(g.S.RivalsPark)) ?? g.S.Stand(g.S.RivalsPark)));
            r.Do(Enf, "draw");
            r.Is(r.Until(() => g.RivalsPresent, T(g.S.RivalsArrive) + T(5) - g.Tick), $"the rival crew is here at {Report.Clock(g.Tick)}");
            r.Fire(Enf, "rivals");
            r.Is(r.Until(() => r.Shots().Exists(x => x.by == Enf && x.target == "rivals"), T(10)), "the Enforcer fires on the crew");
            if (once) { r.Do(Enf, "cease"); return; }
            r.Is(r.Until(() => g.RivalsDelayMethod == "fire", T(20)), $"the crew is driven back at {Report.Clock(g.Tick)}");
        }

        /// <summary>The patrol down, the Enforcer walks within reach of the rival crew its gunfire drew to the Office front and fires on them
        /// until they are driven back.</summary>
        static void FireOnDrawnRivals(Run r, bool waitForPeople = false)
        {
            var g = r.G;
            r.Is(r.Until(() => g.RivalsDrawn && g.RivalsPresent, T(30)), $"the gunfire draws the rival crew ({Report.Clock(g.Tick)})");
            r.To(r.SendTo(Enf, g.S.Grid.NearestFree(NearFront) ?? NearFront));
            if (waitForPeople) r.Is(r.Until(() => HeardBy(g, Enf, true) > 0, T(120)), $"people back on the street within reach at {Report.Clock(g.Tick)}");
            r.Fire(Enf, "rivals");
            r.Is(r.Until(() => g.RivalsDelayMethod == "fire", T(30)), $"the crew is driven back at {Report.Clock(g.Tick)}");
        }
        /// <summary>Between the shot corner and the Office front, 16 m from where the drawn rivals make for (d:Office approach). Re-sited
        /// 2026-10-19 with the door (brief 023; was (3600, -2000)).</summary>
        static readonly Pt NearFront = new Pt(4800, -4200);

        static void PublicFightCases()
        {
            Add("N95", "—", "Feared rivals driven off (Sky 2026-10-16: 'a rival gang that has menace level authority is a +1'; rulebook fight.public): route A, quiet, then the safe final fight; the Handover's rivals are feared (mission rivals.public menace) and the cell breaks them. The final fight is one fight, public (the square is within 30 m of the car): Standing +1 once, when it ends, cause seen-fight-rivals. Between missions Inez's word adds +1: the district ends at +4, the Hero band (Motive's route A row).", r =>
            {
                var g = r.G;
                r.Eq(g.RivalsPublic, "menace", "the Handover's rivals are feared");
                RouteA(r, reroute: true);
                r.Eq((g.FightBranch, g.Fights.Count, g.StandingMoves.Count), ("safe", 0, 0), "a quiet route: no fight and no move before the final fight");
                Support(r); ToAmbush(r); // brief 018: walking Vale the Enforcer doesn't fire; the three by the car fight
                r.FightItOutAll(120, Scout, Fixer, Tech); // brief 018 §10: break the crew (the blocker falls first; a free car alone is board and go, N113)
                r.Extract();
                r.Eq(g.Result, Outcome.Success, "route A wins through the ambush");
                var bo = g.Log.FindAll(l => l.Code == "blocker.out"); var cf = g.Log.FindAll(l => l.Code == "car.free"); // moved here from N113 (2026-10-19)
                r.Is(bo.Count == 1 && cf.Count >= 1 && cf[0].Tick >= bo[0].Tick, $"logged by code: blocker.out, then car.free ({string.Join(", ", bo.Concat(cf).Select(l => l.Code + "@" + Report.Clock(l.Tick)))})");
                r.Eq(g.Fights.Count, 1, "one fight: the final fight");
                var f = g.Fights[0];
                r.Is(f.Final && f.Public, "the final fight, and the public heard it");
                r.Eq((f.OnRivals, f.OnSecurity, f.Outcome, f.Delta, f.Cause), (true, false, "rivals", 1, "seen-fight-rivals"), "the cell drove off a feared crew: +1");
                r.Eq(f.End, g.FightOverTick, "it moves when the fight ends");
                var m = FightMoves(g);
                r.Eq(m.Count, 1, "once per fight, however many shots");
                r.Eq((m[0].tick, m[0].delta, m[0].cause, m[0].who), (g.FightOverTick, 1, "seen-fight-rivals", "rivals"), "+1 at the fight's end");
                var l = OneLine(r, g.BetweenMissions(g.Investigate()));
                r.Eq((l.From, l.End, l.Word, l.Drift, l.To, l.Band), (2, 3, 1, 0, 4, "hero"), "2 + 1 the fight + 1 Inez's word = +4, Hero");
                r.Eq(l.Cause, "seen-fight-rivals", "the line's cause: the fight (the earliest of the two equal moves)");
            });

            Add("N137", "—", "Fire ends with the final fight (rulebook fight.over, Motive 2026-10-19): N95's route and ambush, but once a rival breaks the cell gives an explicit fire order on him (fire until down). When the final fight is over every fire order still on a final-fight hostile ends (fire.done, 'the fight is over'), a new one is refused fight-over, and 35 s later (past crowd.quiet) no shot has opened a second fight: one fight, one Standing move, +1 seen-fight-rivals.", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true);
                Support(r); ToAmbush(r);
                string[] cell = { Enf, Scout, Fixer, Tech };
                Func<string> broken = () => { foreach (var h in g.Hostiles) if (h.Present && !h.Down && h.Gun.St == "break") return h.Id; return ""; };
                r.Engage(() => { var l = new List<string>(); foreach (var h in g.Hostiles) if (h.Present && !h.Down) l.Add(h.Id); return l; },
                         () => broken().Length > 0 || g.FightOverTick != Never, 120, new[] { Scout, Fixer, Tech }, true);
                string runner = broken();
                r.Is(runner.Length > 0 && g.FightOverTick == Never, $"a rival breaks while the fight is on ({(runner.Length > 0 ? runner : "none")})");
                string by = "";
                foreach (var name in new[] { Scout, Fixer, Tech })
                {
                    var o = r.Op(name); if (o.Down || o.Busy != null || o.Gun.W == null || runner.Length == 0) continue;
                    if (g.Issue(new Order { Tick = g.Tick, Op = name, Do = "fire", Target = runner }).ok) { by = name; break; }
                }
                r.Is(by.Length > 0, $"an explicit fire order on the breaking rival {runner} is taken while the fight is on ({(by.Length > 0 ? by : "nobody had a line")})");
                r.FightItOutAll(120, Scout, Fixer, Tech);
                r.Is(g.FightOverTick != Never, "the final fight is over");
                foreach (var name in cell)
                {
                    var o = r.Op(name);
                    r.Is(o.Down || o.Gun.Order.Length == 0 || !g.Hostiles.Exists(h => h.Id == o.Gun.Order), $"{name}: no fire order left on a final-fight hostile ({o.Gun.Order})");
                }
                var still = g.Hostiles.Find(h => h.Present && !h.Down);
                if (still != null && by.Length > 0 && !r.Op(by).Down)
                {
                    var a = g.Issue(new Order { Tick = g.Tick, Op = by, Do = "fire", Target = still.Id });
                    r.Eq(a.code, "fight-over", $"a new fire order on {still.Id} ({still.Gun.St}) is refused");
                }
                int over = g.FightOverTick;
                r.Wait(T(35));
                r.Is(!g.Log.Exists(l => (l.Code == "shot.hit" || l.Code == "shot.miss") && l.Tick > over && g.Hostiles.Exists(h => l.Text.Contains(" fires on " + h.Id + " "))), "no cell shot at a final-fight hostile after the fight is over");
                r.Eq(g.Fights.Count, 1, "one fight: the final fight, no straggler fight");
                var m = FightMoves(g);
                r.Eq(m.Count, 1, "one Standing move from the fight");
                if (m.Count > 0) r.Eq((m[0].delta, m[0].cause), (1, "seen-fight-rivals"), "+1, the feared crew driven off");
                r.Extract();
                r.Eq(g.Result, Outcome.Success, "route A still wins");
            });

            Add("N96", "—", "A rival fight not won (rulebook fight.public): the Enforcer fires on the feared rival crew as it arrives, then the cell withdraws with the crew unbroken. The fight ends with the mission: public, but no outcome holds (not security; feared, so firing first is no fault; not driven off): Standing does not move.", r =>
            {
                var g = r.G;
                FireOnArrivingRivals(r, once: true);
                r.Wait(T(3));
                r.Do(Enf, "withdraw");
                r.Eq(g.Result, Outcome.Withdrawn, "the cell withdraws");
                r.G.Finish();
                r.Eq(g.Fights.Count, 1, "one fight, ended with the mission");
                var f = g.Fights[0];
                r.Is(f.Public, $"public: {HeardBy(g, Enf, true)} civilians within {g.R.FightPublicCm / 100} m of the Enforcer");
                r.Eq((f.OnRivals, f.OnSecurity, f.RivalsFirst, f.Outcome, f.Delta), (true, false, "cell", "none", 0), "the cell fired first on feared rivals and did not break them: 0");
                r.Is(g.RivalsDelayMethod != "fire", "the crew was not driven back");
                r.Eq(FightMoves(g).Count, 0, "no fight move");
            }, new Start { FinalFight = false });

            Add("N97", "—", "Firing first on rivals nobody fears (rulebook fight.public; a scenario fixes rivals.public neutral): N96's orders exactly. The cell fired the first shot between it and the crew: Standing −1 when the fight ends, cause seen-fight-first.", r =>
            {
                var g = r.G;
                r.Eq(g.RivalsPublic, "neutral", "these rivals are not feared");
                FireOnArrivingRivals(r, once: true);
                r.Wait(T(3));
                r.Do(Enf, "withdraw");
                r.G.Finish();
                r.Eq(g.Fights.Count, 1, "one fight");
                var f = g.Fights[0];
                r.Eq((f.Public, f.RivalsFirst, f.Outcome, f.Delta, f.Cause), (true, "cell", "first", -1, "seen-fight-first"), "the cell fired first on neutral rivals: −1");
                var m = FightMoves(g);
                r.Eq((m.Count, m[0].tick, m[0].who), (1, f.End, "rivals"), "once, when the fight ends (here, with the mission)");
            }, new Start { FinalFight = false, RivalsPublic = "neutral" });

            Add("N98", "—", "A fight with security (rulebook fight.public): the Enforcer shoots the patrol robot down from the walk north of the Office at 4:30, people of the city within 30 m. Standing −1 once, not at the first shot but when the fight ends, 30 s (crowd.quiet) after the last shot by or at the cell; cause seen-fight-security, naming the patrol. Trail and Alert are the shots' own, as before; the district ends at +1.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Enf, ShotCorner)); r.To(Math.Max(g.Tick, ShotAt));
                r.Is(HeardBy(g, Enf, true) > 0, $"civilians within {g.R.FightPublicCm / 100} m of the Enforcer ({HeardBy(g, Enf, true)})");
                r.Fire(Enf, "patrol");
                r.Is(r.Until(() => g.PatrolDown, T(60)), $"the patrol is down at {Report.Clock(g.Tick)}");
                r.Eq(g.StandingMoves.Count, 0, "nothing moves while the fight is on");
                r.Is(r.Until(() => g.Fights.Count > 0, T(60)), $"the fight ends at {Report.Clock(g.Tick)}");
                var f = g.Fights[0];
                r.Eq(f.End - f.Last, g.R.FightQuietTicks, "30 s after the last shot");
                r.Eq((f.Public, f.OnSecurity, f.FirstSecurity, f.Outcome, f.Delta), (true, true, "patrol", "security", -1), "a fight with authority: −1");
                var m = FightMoves(g);
                r.Eq((m.Count, m[0].tick, m[0].cause, m[0].who), (1, f.End, "seen-fight-security", "patrol"), "once, when it ends");
                r.Eq(g.Standing, g.R.StandingStart - 1, "+2 → +1");
                r.Eq((g.Trail, g.Alert), (g.R.GunfireTrail + g.R.HitTrail + g.R.DownTrail, g.R.GunfireAlert), "Trail and Alert are the shots' own: a shooting, a hit, a down; Alert 2");
                r.Wait(T(5));
                var l = OneLine(r, g.BetweenMissions(g.Investigate()));
                r.Eq((l.From, l.End, l.Word, l.Drift, l.To, l.Cause, l.Who), (2, 1, 0, 0, 1, "seen-fight-security", "patrol"), "the district ends at +1, the fight its cause");
            }, new Start { FinalFight = false });

            // N99, N101: the patrol shot down, then the rival crew its gunfire draws, driven off: inside 30 s one fight, after 30 s two.
            void PatrolThenRivals(Run r, bool apart)
            {
                var g = r.G;
                PatrolShotDown(r);
                if (apart) r.Is(r.Until(() => g.Fights.Count == 1, T(60)), $"the patrol's fight ends at {Report.Clock(g.Tick)}");
                FireOnDrawnRivals(r, apart);
                int first = r.Shots().Find(x => (x.by == Enf && x.target == "rivals") || (x.by == "rivals" && x.target == Enf)).tick;
                int gap = first - r.Shots().FindLast(x => x.tick < first && (x.by == Enf || x.target == Enf)).tick;
                r.Is(apart ? gap >= g.R.FightQuietTicks : gap < g.R.FightQuietTicks, $"the rivals' fight starts {gap / 10} s after the last shot at the patrol");
                r.Is(r.Until(() => g.OpenFight == null, T(60)), $"the last fight ends at {Report.Clock(g.Tick)}");
            }
            Add("N99", "—", "A mixed fight (rulebook fight.public: authority outranks): the Enforcer shoots the patrol down from the walk north of the Office, then walks on the rival crew its gunfire draws and drives it off, never 30 s without a shot. One fight, on security and on rivals (broken): −1, seen-fight-security, once; the win over the rivals counts for nothing.", r =>
            {
                var g = r.G;
                PatrolThenRivals(r, false);
                r.Eq(g.Fights.Count, 1, "one fight: no 30 s gap");
                var f = g.Fights[0];
                r.Eq((f.Public, f.OnSecurity, f.OnRivals, f.Outcome, f.Delta, f.Cause), (true, true, true, "security", -1, "seen-fight-security"), "on security and rivals: authority outranks, −1");
                r.Is(f.CrewBroken >= 0, "though the rivals broke");
                r.Eq(Moves(g), "-1 seen-fight-security patrol", "one move, naming the patrol");
            }, new Start { FinalFight = false });

            Add("N100", "—", "A fight nobody heard (rulebook fight.public): N98's orders in a world with no crowd (Start.Crowd off); the cast's civilians (Inez, the driver, Vale inside) are all beyond 30 m of every shot. The fight ends unheard: Standing does not move, and the district stands steady at +2. Trail and Alert are N98's.", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Enf, ShotCorner)); r.To(Math.Max(g.Tick, ShotAt));
                r.Eq(HeardBy(g, Enf, false), 0, "no civilian within 30 m of the Enforcer");
                r.Fire(Enf, "patrol");
                r.Is(r.Until(() => g.PatrolDown, T(60)), $"the patrol is down at {Report.Clock(g.Tick)}");
                r.Is(r.Until(() => g.Fights.Count > 0, T(60)), $"the fight ends at {Report.Clock(g.Tick)}");
                var f = g.Fights[0];
                r.Eq((f.Public, f.OnSecurity, f.Outcome, f.Delta), (false, true, "unheard", 0), "nobody heard it: no outcome");
                r.Eq((g.Standing, g.StandingMoves.Count), (g.R.StandingStart, 0), "Standing does not move");
                r.Eq((g.Trail, g.Alert), (g.R.GunfireTrail + g.R.HitTrail + g.R.DownTrail, g.R.GunfireAlert), "Trail and Alert as in N98");
                var l = OneLine(r, g.BetweenMissions(g.Investigate()));
                r.Eq((l.To, l.Cause), (2, "steady"), "steady at +2");
            }, new Start { FinalFight = false, Crowd = false });

            Add("N101", "—", "Two fights 30 s apart give two moves (rulebook fight.public: a fight ends crowd.quiet after its last shot): N99's orders, but the Enforcer waits for the patrol's fight to end and for people to come back within 30 s of him (the first shots scattered them) before he fires on the rivals; at security level 0 (lax), so the response does not join in. Two public fights: −1 for the patrol, then +1 for driving off the feared crew: two moves, the district back at +2. N99 is the closer twin: one fight, one move.", r =>
            {
                var g = r.G;
                PatrolThenRivals(r, true);
                r.Eq(string.Join(", ", g.Fights.Select(f => $"{f.Public} {f.Outcome}")), "True security, True rivals", "two fights: the patrol's, then the rivals' (won)");
                r.Eq(Moves(g), "-1 seen-fight-security patrol, 1 seen-fight-rivals rivals", "two moves");
                r.Eq(g.Standing, g.R.StandingStart, "+2 → +1 → +2");
            }, new Start { FinalFight = false, Security = 0 });
        }
    }
}
