// The debrief (§08 [B]) as data: the top-down test's nextCity, with ids instead of sentences (Fable writes the words, Glyph lays
// them out). Public security from the final Alert and any shot; the investigation from the Trail and named causes; contacts and
// assets; the route summary; the interventions beyond the route.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed class Debrief
    {
        public string PublicSecurity = "", InvestigationState = "", InvestigationCause = "";
        public int Contacts; public bool Loud;
        public string FightBranch = ""; public int FightHostiles, FightDownedByEnforcer, FightDownedByGuard, FightDownedByCell, FightHits, FightShots;
        public readonly List<string> Captured = new List<string>(); public readonly List<(string op, int health, int max)> Cell = new List<(string, int, int)>();
        public Investigation Inv = new Investigation(); public Between Next = new Between(); public int Sightings; public readonly List<string> FightWhy = new List<string>();
        public readonly List<string> Assets = new List<string>(), Route = new List<string>(), Extra = new List<string>(), Chain = new List<string>();
        /// <summary>brief 018 §3: everyone not in the car when it left, with fate and why.</summary>
        public readonly List<(string op, string fate, string why)> LeftBehind = new List<(string, string, string)>();
        /// <summary>What ended a failed (or withdrawn) run, for the fail camera (FailCause.cs); null on success, and then not written.</summary>
        public FailCause? Cause;

        public static Debrief Of(Game g)
        {
            var d = new Debrief { Loud = g.Gunfire > 0, Cause = g.Cause };
            d.PublicSecurity = g.Alert >= 3 ? "high" : d.Loud ? "raised" : "calm";
            bool inezExposed = g.GuardReachedCafe;
            d.Inv = g.Investigate(); d.Next = g.BetweenMissions(d.Inv); d.Sightings = g.Sightings.Count;
            // §08: little → early → active by Trail (0 / 1–2 / 3+); a named cause takes precedence: a civilian shot, a witness of violence, the
            // Fixer's face given up to the investigators (rulebook investigation), the staged distraction; then the count
            (d.InvestigationState, d.InvestigationCause) =
                g.GunOf("waitress")!.Down ? ("active", "waitress-shot") :
                g.GunOf("driver")!.Down ? ("active", "driver-shot") :
                g.InezWitnessedShooting ? ("active", "waitress-witnessed-shooting") :
                g.InezMethod == "pressure" ? ("active", "waitress-can-identify-fixer") :
                d.Inv.NamedCause.Length > 0 ? ("active", d.Inv.NamedCause) :
                inezExposed ? ("active", "staged-distraction") :
                g.Trail >= 3 ? ("active", "several-interventions") :
                g.Trail > 0 ? ("early", "few-leads") : ("little", "");
            if (g.Inez.Turned && !g.Inez.Down) d.Contacts++;
            if (g.Driver.Turned && !g.Driver.Down) d.Contacts++;
            if (g.Inez.Turned && !g.Inez.Down)
                d.Assets.Add(g.InezWitnessedShooting ? "waitress-witnessed-shooting" : g.InezMethod == "pressure" ? "waitress-may-identify-fixer" : inezExposed ? "waitress-may-be-questioned" : "waitress-introduces-cafe");
            if (g.Driver.Turned && !g.Driver.Down) d.Assets.Add("driver-alters-pickups");
            d.Chain.AddRange(g.Chain);
            d.FightBranch = g.FightBranch; d.FightHostiles = g.Hostiles.Count; d.FightDownedByEnforcer = g.DownedByEnforcer; d.FightDownedByGuard = g.DownedByGuard; d.FightDownedByCell = g.DownedByCell; d.FightHits = g.FightHits; d.FightShots = g.FightShots; d.FightWhy.AddRange(g.FightWhy);
            d.Captured.AddRange(g.Captured); d.LeftBehind.AddRange(g.LeftBehindAtGo); foreach (var op in g.Ops) d.Cell.Add((Sim.Op.Name(op.Id), op.Gun.Health, op.Gun.Max)); // wounds carry over (§08)

            var relevant = new HashSet<string>(StringComparer.Ordinal);
            if (g.RouteKind == "front")
            {
                relevant.Add("loop");
                if (g.RouteGuard == "shot") relevant.Add("fire:guard");
                else if (g.RouteGuard == "waitress") { relevant.Add("turn." + (g.InezMethod.Length > 0 ? g.InezMethod : "help") + ":waitress"); relevant.Add("call:waitress"); }
                if (g.RouteBot == "rerouted") relevant.Add("reroute");
                d.Route.Add(g.RouteGuard == "shot" ? (g.InezWitnessedShooting ? "guard-shot-beside-waitress" : "guard-shot-away-from-waitress")
                    : g.RouteGuard == "knocked-out" ? "guard-knocked-out" : g.RouteGuard == "killed" ? "guard-killed" // a takedown (brief 013 E4), not a shooting
                    : "waitress-pulls-guard-away");
                d.Route.Add("entrance-camera-looped");
                d.Route.Add(g.RouteBot == "rerouted" ? "patrol-diverted" : "entry-timed-between-sweeps");
                d.Route.Add("vale-taken-through-office"); d.Route.Add("vale-reaches-getaway-car");
            }
            else if (g.RouteKind == "service" || g.RouteKind == "pickup")
            {
                if (g.RouteKind == "service") relevant.Add("turn.help:driver"); else relevant.Add("call:driver");
                if (g.RouteRival == "mislead") relevant.Add("mislead"); else if (g.RouteRival == "fire") relevant.Add("fire:rivals");
                d.Route.Add(g.RouteKind == "service" ? "driver-moves-vale-to-service-lane" : "pickup-camera-blocked");
                if (g.RouteRival.Length > 0) d.Route.Add(g.RouteRival == "fire" ? "rivals-driven-back" : "rivals-follow-false-tip");
                d.Route.Add("vale-intercepted-outside"); d.Route.Add("vale-reaches-getaway-car");
            }
            // interventions that left a trace beyond the route (the top-down test's traceLabels), counted in the order first taken
            var traced = new[] { "turn.ask:waitress", "turn.pay:waitress", "turn.help:waitress", "turn.pressure:waitress", "call:waitress", "turn.help:driver", "turn.pay:driver",
                "call:driver", "loop", "reroute", "mislead", "fire:guard", "fire:waitress", "fire:driver", "fire:rivals" };
            var order = new List<string>(); var count = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var a in g.Actions)
            {
                if (Array.IndexOf(traced, a) < 0 || relevant.Contains(a)) continue;
                if (!count.ContainsKey(a)) { count[a] = 0; order.Add(a); }
                count[a]++;
            }
            foreach (var a in order) d.Extra.Add($"{count[a]} {a}");
            return d;
        }

        public void Write(JWriter w)
        {
            w.Obj().Prop("publicSecurity", PublicSecurity).Open("investigation").Prop("state", InvestigationState).Prop("cause", InvestigationCause).EndObj()
                .Prop("contacts", Contacts).Prop("loud", Loud);
            w.OpenArr("assets"); foreach (var a in Assets) w.Str(a); w.EndArr();
            w.OpenArr("route"); foreach (var a in Route) w.Str(a); w.EndArr();
            w.OpenArr("extraInterventions"); foreach (var a in Extra) w.Str(a); w.EndArr();
            w.OpenArr("chain"); foreach (var a in Chain) w.Str(a); w.EndArr();
            w.Open("investigators").Prop("count", Inv.Investigators).Prop("hops", Inv.Hops).Prop("sightings", Sightings).OpenArr("findings");
            foreach (var x in Inv.Findings)
            {
                w.Obj().Prop("hop", x.Hop).Prop("who", x.Who).Prop("outcome", x.Outcome).Prop("lead", x.Lead);
                w.OpenArr("gives"); foreach (var f in x.Facts) w.Str(f); w.EndArr();
                if (x.Cause.Length > 0) w.Prop("because", x.Cause);
                w.EndObj();
            }
            w.EndArr();
            if (Inv.Hunted.Count > 0) { w.OpenArr("hunted"); foreach (var x in Inv.Hunted) w.Str(x); w.EndArr(); } // brief 018: written only when set
            w.EndObj();
            w.Open("nextMission").Prop("ambush", Next.Ambush);
            if (Next.Cases.Count > 0 || Next.CampaignTrail != 0) // brief 013: written only when a takedown left something, so older debriefs read as before
                w.Prop("campaignTrail", Next.CampaignTrail).Prop("campaignStanding", Next.CampaignStanding).Prop("openCases", Next.OpenCases).Prop("nextLevel", Next.NextLevel); w.OpenArr("ambushBy"); foreach (var a in Next.AmbushBy) w.Str(a); w.EndArr();
            w.OpenArr("cases");
            foreach (var c in Next.Cases)
                w.Obj().Prop("who", c.who).Prop("kind", c.kind).Prop("place", c.place).Prop("missions", c.missions).EndObj();
            w.EndArr();
            w.OpenArr("areaWatch");
            foreach (var a in Next.AreaWatch)
                w.Obj().Prop("place", a.place).Prop("guards", a.guards).Prop("cameras", a.cameras).Prop("missions", a.missions).EndObj();
            w.EndArr();
            w.OpenArr("standing"); // brief 014: one line per district (rulebook debrief.lines.standing) and the Standing the next mission starts at
            foreach (var l in Next.Standing)
            {
                w.Obj().Prop("district", l.District).Prop("from", l.From).Prop("to", l.To).Prop("band", l.Band).Prop("cause", l.Cause).Prop("who", l.Who)
                 .Prop("mission", l.End - l.From).Prop("found", l.Found).Prop("word", l.Word).Prop("wordUncapped", l.WordUncapped).Prop("drift", l.Drift).OpenArr("moves");
                foreach (var (d, c, who) in l.Moves) w.Obj().Prop("delta", d).Prop("cause", c).Prop("who", who).EndObj();
                w.EndArr().EndObj();
            }
            w.EndArr();
            if (Next.Captives.Count > 0 || Next.CarSeenBy.Length > 0) // brief 018 §3: written only when the car left someone or was seen
            {
                if (Next.CarSeenBy.Length > 0) w.Prop("carSeenBy", Next.CarSeenBy);
                w.OpenArr("captives");
                foreach (var c in Next.Captives)
                {
                    w.Obj().Prop("who", c.Op).Prop("why", c.Why).Prop("heldAt", c.HeldAt).Prop("holds", c.Holds).Prop("talksAt", c.TalksAt).Prop("rescueUntil", c.RescueUntil)
                     .Prop("bravery", c.Sc[0]).Prop("resentment", c.Sc[1]).Prop("fear", c.Sc[2]).Prop("loyalty", c.Sc[3]).Prop("left", c.Left).Prop("right", c.Right).OpenArr("gives");
                    foreach (var f in c.Facts) w.Str(f);
                    w.EndArr().EndObj();
                }
                w.EndArr();
            }
            w.OpenArr("lines"); foreach (var l in Next.Lines) w.Obj().Prop("line", l.Line).Prop("who", l.Who).Prop("detail", l.Detail).EndObj(); w.EndArr();
            w.OpenArr("contacts");
            foreach (var c in Next.Contacts)
                w.Obj().Prop("who", c.Role.Length > 0 ? c.Role : c.Id).Prop("name", c.Name).Prop("need", c.Need).Prop("needKnown", c.KNeed)
                 .Prop("bravery", c.Sc[0]).Prop("resentment", c.Sc[1]).Prop("fear", c.Sc[2]).Prop("loyalty", c.Sc[3]).EndObj();
            w.EndArr().EndObj();
            if (FightBranch.Length > 0)
            {
                w.Open("finalFight").Prop("branch", FightBranch).Prop("hostiles", FightHostiles).Prop("downedByEnforcer", FightDownedByEnforcer).Prop("downedByGuard", FightDownedByGuard).Prop("downedByCell", FightDownedByCell).Prop("hitsTaken", FightHits).Prop("shots", FightShots);
                w.OpenArr("why"); foreach (var a in FightWhy) w.Str(a); w.EndArr(); w.EndObj();
            }
            w.OpenArr("cell"); foreach (var (op, hp, max) in Cell) w.Obj().Prop("op", op).Prop("health", hp).Prop("max", max).EndObj(); w.EndArr();
            w.OpenArr("captured"); foreach (var c in Captured) w.Str(c); w.EndArr();
            if (LeftBehind.Count > 0) { w.OpenArr("leftBehind"); foreach (var (op, fate, why) in LeftBehind) w.Obj().Prop("who", op).Prop("fate", fate).Prop("why", why).EndObj(); w.EndArr(); } // brief 018
            if (Cause != null) { w.Key("cause"); Cause.Write(w); } // the fail camera's culprit: only a failed or withdrawn run has one
            w.EndObj();
        }
    }

    /// <summary>Loading and playing: the engine's inputs from the repo, a script played to its end, a replay from its orders.</summary>
    public static class Engine
    {
        public const string RulebookFile = "contracts/rules/rulebook.json", PeopleRulesFile = "contracts/people/people.rules.json", SiteFile = "sim/data/handover.site.json", PeopleFile = "sim/data/handover.people.json",
            ProvisionalFile = "sim/data/handover.provisional.json", // brief 013: the security level until the plan and mission carry it
            TakedownFile = "sim/data/handover.takedown.json",       // brief 013: stashes, report points, docks and posts (Splice, tools/sim/takedown.ts)
            RoundsFile = "sim/data/handover.rounds.json";           // brief 022: the server's baked round (Pulse, tools/routines/rounds.ts)

        public sealed class Inputs
        {
            public string Rulebook = "", PeopleRules = "", Site = "", People = "", Provisional = "", Takedown = "", Rounds = ""; public Rules Rules = null!; public Site SiteData = null!; public Crowd? Crowd;
            /// <summary>The inputs' hash over their canonical text (no BOM, LF line ends: Redline E4), so a Windows checkout replays a Mac record.</summary>
            public ulong Hash { get { unchecked { var h = Sim.Hash.Of(Rulebook) ^ (Sim.Hash.Of(PeopleRules) * 3) ^ (Sim.Hash.Of(Site) * 7) ^ (Sim.Hash.Of(People) * 11); if (Provisional.Length > 0) h ^= Sim.Hash.Of(Provisional) * 13; if (Takedown.Length > 0) h ^= Sim.Hash.Of(Takedown) * 17; if (Rounds.Length > 0) h ^= Sim.Hash.Of(Rounds) * 19; return h; } } } // a host without brief 013's files hashes as before, and its record says so
        }
        static Inputs? cache; static string? cacheRoot;
        public static Inputs Load(string root)
        {
            if (cache != null && cacheRoot == root) return cache;
            string rd(string f) => System.IO.File.ReadAllText(System.IO.Path.Combine(root, f));
            string? opt(string f) => System.IO.File.Exists(System.IO.Path.Combine(root, f)) ? rd(f) : null;
            var i = FromTexts(rd(RulebookFile), rd(PeopleRulesFile), rd(SiteFile), rd(PeopleFile), opt(ProvisionalFile), opt(TakedownFile), opt(RoundsFile));
            cache = i; cacheRoot = root; return i;
        }
        /// <summary>The same inputs from their texts, for a host that reads files its own way (Unity: TextAssets or StreamingAssets).</summary>
        /// <summary>The engine's version for run records: bump it when a change moves any replay. The assembly's MVID is recorded beside it.</summary>
        public const string Version = "suborned.sim/0.26.0"; // 0.26.0: the squad fire order agent by agent (Sky playing 0.2.65: "I had the whole squad selected but some of my agents weren't firing at the target e.g. the Scout"; briefs/041/next-batch.md): read-only Game.FireQuery(op, target, force, focus, out walk), the answer Issue would give a fire order now (taken, or its refusal code, words and numbers, in Check then Fire's order: mission-over, agent-down, enforcer-inside, agent-busy, in-car, no-weapon, fire-carrying, fire-carrying-vale, no-target, fire-on-cell, target-unknown, civilian-target, out-of-range / no-clear-line with no walk in) and whether it would walk in first; for the host to give a squad fire order to every selected agent who takes it and say why on the plate of each who does not; scenarios QF-HOST, QF-ALL, QF-LINE, QF-CANT, QF-FOCUS (range) and QF-BUSY (Handover) keep it equal to Issue; no rule, state, inputs or run hash moves. 0.25.0: why a hostile fires on an operative (Sky playing 0.2.56: "Enforcer was shot by the door guard - I couldn't tell if that's because weapon was out, carrying Vale or coz they shot police etc?"): read-only Game.FireWhyOf(op) lists each hostile aiming at the operative or that fired on it within weapons.lineLapse (FireWhy: By, Causes now, First (the causes when it first took him), Since, Aiming, Firing, Main), Game.FireCauses(by, op) and Game.FireWhyFirst(by, op); the causes are what the targeting reads (Provoked; final every "final"; the fight range "range") in the fixed order Game.FireCauseOrder, the strongest first: range, final, fired, vale, violence, drawn, face, seen; the log code ai.why once a pair, on the tick a hostile first takes an operative as its target: hashed with the log, so only runs where a hostile takes an operative as a target move, from that tick; no rule, state or inputs hash moves; and brief 045 (Redline, late lag): F3 Game.ClosingHash(), the closing checkpoint Finish would take now (an open fight closed, hashed and put back) without a replay or a change to the live game, one Hash.State (Hash.State reads each order's text as it was issued, Game.OrderText: the same bytes, written once); F5 a Watch or Listen query out of reach (LearnQuery, engage false) asks only whether the walk in exists (one route, CanWalkIn) instead of the intercept; neither moves a hash. 0.24.0: the window marker's one read (brief 043 §6, the done moment): Game.Windows() lists the cell's open windows in a fixed order, each OpenWindow (Kind, Thing, Until: the tick it closes as the engine has it now, -1 while held): call guard (Call Inez, GuardAwayUntil; not while he is held or down), call pickup (Call driver, from car.blocks to BlockedUntil, or driverLast if the turned driver takes the car first), reroute patrol (PatrolAwayUntil), mislead rivals (RivalsDelayedUntil, a false tip only), loop <camera> (LoopOn + LoopCam, -1); read-only, no hash moves; and the log code patrol.returns on the tick the rerouted patrol's false orders run out (Tick == PatrolAwayUntil, the patrol not down), the window's closing code as guard.back, car.clears and loop.ends are: hashed with the log, so only runs where a rerouted patrol comes back before the end move. 0.23.0: the walk in (Sky 2026-10-22 playing: "I can't listen to Inez a lot of the time with the Scout - I think we shouldn't block actions whilst Inez is moving about"; rulebook cell.approach, approach-repath-s 1): a person act (Watch on a person, Listen, Ask, Pay, Help, Pressure) given out of reach, or a person's Watch with no clear line, is taken instead of refused too-far / no-clear-line, once its other needs hold (checked as if in reach; the driver's last chance counts the walk); the operative walks the grid at a walk toward where the person will be (a server's round read ahead), re-aimed every second and when the walk ends; the act starts on the first tick in place and is checked again then as an order given there (refused then: dropped, Game.Dropped, log order.dropped, nothing paid); a server is held from the start as before; any other order but stance, draw, holster, cease (and the cell settings) ends the walk in (order.walk-in-ends), a cancel stops it (cancel.move); no route: no-walk; a driven operative is never walked (too-far as before); Op.Act and Op.ActNext hashed only when set; host reads WalkInS, ActivityOf (phase walking, verb the act's), Dropped; the rulebook moves every inputs hash, and runs that gave a person act out of reach now walk in. 0.22.0: only security notices a run (Sky 2026-10-22: "running only in front of camera, patrol bots and guards get you noticed, anyone else fine"; police count as security, Sky via Aster "yep"; both presets; rulebook cell.rule): a run is remembered (saw-runner, place, time and the face) only by security staff on the street (the door guard, a job in witness.staff: guard, patrol-officer, watcher, investigator), no longer by anyone else in witness range; the found fault (runAlert, once a run) comes from a live camera, the guard by his cone, the patrol, the response's robots and police as before, and now also from a security-staff person on the street other than the door guard, not turned, who sees the run by the door guard's sight rule on his rounds (the facing cone with the near sense, a clear line, people blocking; at a stop the near sense only; Sky via Aster: "a guard is a guard"); ledger runner-seen:<person id>:<agent>, log run.seen "The <job> <name> sees the <agent> running"; MoveQuote.RunSeenBy lists such a person by id when the route passes them now); a takedown target hears a running Enforcer within witness range only if staff (sight by the facing cone and a wary guard stand); MoveQuote.RunPeople counts staff only; rivals never noticed a run and still don't; the rulebook moves every inputs hash. 0.21.0: the lasting fire order on the move (Sky 2026-10-22 playing 0.2.48, brief 041: "Keep them firing until out of range I would say, same for running"; both presets): a walk or a run keeps a standing fire order and it fires on the move at the gait's chance, as 0.15.0; once the order has had its target in reach (the weapon's max ring) and line (Gun.Reached, hashed only while an order stands and has it), the target beyond the max ring ends it at once (fire.out-of-range: no walk back in, no lapse), walking, running, driven or standing; a new act (any order that engages the agent) ends it (fire.cancelled) instead of pausing it through the act; until the order has had reach and line, the brief-030 walk-in and the lineLapse lapse stand; the rulebook rule text moves every inputs hash. 0.20.0: the final fight per agent, PROVISIONAL until Sky plays it (Motive tuning.md §15.2; Sky 2026-10-22 "I think so - sounds right"; both presets; rulebook ai.intent.final agent, was every): in the final fight a hostile fires on an operative only for that operative's own causes, the lists it reads before the fight (police and robots drawn, fired, vale, violence or its own known face; rivals drawn, vale, seen), so a holstered agent never seen acting draws no fire; final every keeps 0.19.0 exactly; the fight range is always every; the rival crew's guard ally is unchanged; tuning.md §15.1's five intent values and the press are confirmed (no longer proposed, no number changes); no new host field; the rulebook moves every inputs hash. 0.19.0: weapons free and hostile intent per agent (Motive tuning.md §14 with Sky's §14 note; Pulse briefs/027/pulse/hostile-initiative.md with Sky's answers 2026-10-21; both presets; rulebook weapons.free, ai.intent): weapons free is a logged cell input ({do: free, op: cell, on}; Start.weaponsFree, else weapons.free.start on; refusal free-not-cell); in a fight a drawn agent at return or free with no fire order, trigger or follow picks its attacker, else the nearest combatant in its max ring on a line, and keeps it (fire.free); precedence fire order (lock, focus), trigger, follow, weapons free, and under weapons free in a fight follow takes only a non-combatant target; cease stands an agent down until shot at (Gun.StandDown); a fight is on from a shot by or at the cell while a combatant aims at an operative in its max ring on a line, and off weapons.free.settle (line-lapse-s 3 s) after the last contact or shot (fire.free-on, fire.free-off); a focus (fire with focus: true) is a lock that never walks in (fire.focus); before the final fight a hostile fires on an agent only for that agent's seen act or own known face (ai.intent: police drawn, fired, vale, violence, the face from Alert 2; robots the same, the face at any Alert at wanted or infamous Heat; rivals calm drawn or vale, alerted also what they saw it do), never for another agent's acts or the Alert alone; a face is known when security saw the draw or violence, warned the agent, or a witness's report of violence or a draw landed; police warn a drawn weapon with no target at Alert 0-1 before any shot (the lab's warning, its numbers: 2 s, Alert 1, +1 T heeded, one a watcher); from Alert 3 at security level 1 and up police close in on a sighting by fight.kite's close-in; the guard and the patrol engage a known face they see; the rival crew watches the street from its arrival (its cone and the host's shape) and, drawn, presses for the Vale holder (KiteDisp.Press); host reads UnderFireOf (who, since, last, aiming), StandDownOf, FreeFightOn, FreeFightSince, KnownFaces, RivalsSawAct, RivalsStreetWatching, FireState mode weapons-free; faces, rivalsSaw, today's warnings, the fight's ticks and a stand-down are hashed only when set; the rulebook moves every inputs hash. 0.18.0: Motive tuning.md §13 (both presets): the count is the pair's in today too (§13.1, closing the tap-switch reset in the §11 trigger: taking a target resumes the count the shooter left on it, a target not yet fired at starts at acc.start; Gun.Mem in today, hashed only where the state does not already say it: not the target in hand at its count, not a target shot down); a holding agent never follows and is never locked by a press (§13.2); the lab's stances are three (fight.lab.stances, §13.4: a lab draw sets return); the rulebook moves every inputs hash. 0.17.0: the trigger, locks and follow fire (Sky on firing 2026-10-21, brief 024; Motive tuning.md §11; both presets; rulebook weapons.trigger, weapons.follow, follow-m 8): a logged trigger input for the driven agent (op, on, target, tick; refusal not-driven) fires on cadence while pressed and one shot per press at the first ready tick (a buffer of one), lost on a change of target, the target gone, or no reach or line when ready; a release keeps the target, the aim and the count (no tap reset); a press ends the agent's fire order, a fire order, cease, holster, the agent down or control ending ends the trigger; while it has a trigger target the driven agent's stance does not fire; a lock is a lasting fire order (engaged while its target is up); drawn agents within follow-m of the driven agent, not locked, not returning fire, not busy, carrying, inside or in the car, follow its target from where they stand (never walked, never drawn), paying aimNew: follow setting off (Start.followLock, the follow order, op cell) while the trigger is held, the aim kept on release; on, a press or a change of target locks each follower with reach and line (a fire order with its walk-in spent; fire.lock); Gun.Trig, TrigTap, TrigForce, TrigT, FollowBy and Game.FollowLock hashed only when set; host reads FollowersOf, LockOf, FireStateOf (mode, target, firing, hollow, shot, next shot, cadence, lead), FollowRingCm; the rulebook moves every inputs hash. 0.16.0: the settle and cars (Sky 2026-10-21, approved directions; Motive tuning.md §10.2, §10.7; both presets): a shooter whose run ends (a click run, or a driven run that moved) waits aimNew before its next shot, keeping its target, order and count (weapons.settle; Op.RanLast, hashed only on a run ended since the last fight tick); cars are targets ("car:getaway", "car:official", "car:rivals"; rulebook car.target: health, disabledAt, burn, blast.m, blast.damage, ×healthScale in the lab but burn and the ring): disabled at disabledAt (stops, occupants out; the getaway car fails getaway-lost), burning at 0, exploding burn s later (everyone in the ring with a line takes the blast, the cell included; people it hits cost the cell hit and down Trail, operatives none; seen by a civilian, Alert 3 and a Menace step once a car; its cover points go); a car shot is a shooting with no hit or down Trail and no Standing; GoingLoud.Fails getaway-lost, and vale-dead for the official car with Vale at it; Game.CarAt, CarOf, CarDisabled, CarBurning, CoverStanding; the range has layout lane (90 m, F8-BREAK); the rulebook moves every inputs hash. 0.15.0: fire on the move and anyone can be shot (Sky 2026-10-21; Motive tuning.md §8, §9; both presets): a fire order stops a walker and never a runner, a driven agent keeps its drive, a move given while a fire order stands keeps it and spends its walk-in (Op.CloseSpent; the order lapses weapons.lineLapse after the move ends out of reach); stances fire on the move; a shot reads the shooter's gait (walk mod.moving, run mod.running, a run away within weapons.runAwayCone mod.runningAway; the lab −1, −runPips floor 0, 0 pips = pip0), a running target past point blank takes mod.targetRunning in cover's place, and inside point blank cover counts only with weapons.pointBlankCover (false: Sky 2026-10-21, as the lab); hostiles fire as they walk in today (ai.cover.stepFires true; false is §8.3's fallback, not adopted); fire-running and fight-over are no longer given (a fire order given in the final fight still ends at fight.over; one given after it is taken and traced); a knocked-out body is a target and a hit kills; a person shot down outside the final fight lies dead (a brief-013 body, act How "shot", landed with the shooting); cameras are targets ("camera:<id>", weapons.health.camera 1; dark when shot out); GoingLoud.Fails and Hold: a shot that would fail the mission always takes the hold; FirePreview.Gait, TargetRunning, HasEnd, EndAt, AccEnd, PipsEnd, ShotsToDownEnd; the rulebook moves every inputs hash. 0.14.1: tuning.md §7.11.1: rule 4 is the blocker only (the lab's contest officer answers as any police body does); lab-only behaviour, today's hashes unchanged apart from the inputs hash (rule text). 0.14.0: police in cover answer fire (Motive tuning.md §7.10, both presets): shot at within weapons.lineLapse by an operative they see, police (officers, the response unit, police units; not robots, the guard or the patrol) fire back to their max ring; shielded and shot from beyond it they hold at most fight.kite.coverHold (3 s), then close to the shielding cover nearest their stop point (again each 3 s while shots land); the blocker and the lab's car holders answer by the max ring only; the rulebook moves every inputs hash. 0.13.0: the kite response and the 16 m sidearm in today (Sky 2026-10-20, tuning.md §7.9; rulebook fight.kite, pistol-max-m 16): armed hostiles shot at from beyond their reach answer as the lab's fight.lab.kite does (the R2 budget, the R3 police units, the no-cover close-in, the N7 re-plan), hashed in today too; the walk into range (brief 030) is hashed in today; the lab keeps both; the rulebook moves every inputs hash. 0.12.0: the contextual act's engine side (brief 030, Sky 2026-10-20; rulebook weapons.close, weapons.autoHolster): a fire order out of reach or line walks into the weapon's effective ring in every mode (was the lab's fire.close only), holds while it walks, and a driven agent is not walked (PreviewFireWalkIn previews the walk-in for one); a weapon drawn by a fire order or panic (Gun.DrawnBy fire, panic; a draw order: key) holsters autoHolster s after its fight ends (fight.public's end), unless a fire order stands, a stance order was given since, or a hostile up within its max reach has a line to the agent (log holster.auto); the rulebook moves every inputs hash. 0.11.0: everyone watches, watchers notice loiterers (brief 031, Sky 2026-10-20; rulebook sight, attention): any agent Watches (cell.watch, renamed from scout.watch; its log code is the agent's, <agent>.watch.watched) people, cameras, the patrol and containers, 20 s, from its own reach (sight.watch.reach: Scout 30, Tech 24, Fixer 12, Enforcer 3 m) on a clear line (walls and people); the patrol's routine counts the union of watched time; Listen and Mislead stay the Scout's; every security watcher keeps an attention clock per operative (still in sight +1, any act holds, moving or out of sight -1, floor at wary once wary), with noticed, wary (a wary guard is aware at a takedown) and report (a found fault: Alert at least 1 through the report path, no Trail, a sighting, the dial's dispatch) at the Heat band's marks; the start state carries heat (else the site's mission start.heat, else 0); machines (the chasing patrol, the response's robots) lose the all-round near sense, and the chasing patrol faces the operative it chases (was always the Enforcer); host reads Attention, AttentionStage, HeatBand, AttentionMarks, R.WatchReachCm; the rulebook moves every inputs hash and the clock moves runs whose agents stand still in sight. 0.10.0: the fight lab, EXPERIMENTAL (brief 027 playground, Sky 2026-10-20; rulebook fight.lab): read only when a start turns it on (Start.fightLab, hashed only then; off, every run plays and hashes as 0.9.0 apart from the inputs hash): health ×healthScale, the loadout and lab weapons (damage, cadence, rings, the riot gun's near band, the rifle never fires walking), four pips, draw and aimNew, stances two, fire.then free, fire.close (walk into range), charge, the volley order (target mode) and the field, the police warning, hostiles fire walking, panic any time; read-only previews PreviewFire and PreviewVolley, ChargeOf, FieldOf, Combatants, Warnings; refusal code no-charge; the rulebook moves every inputs hash. 0.9.0: the driven agent, EXPERIMENTAL (brief 025 playground, Sky 2026-10-19; rulebook cell.drive): a host moves one operative freely and hands the engine a drive sample a tick (an order: to, run, face; release), checked against the pace budget (cell.speed or runSpeed, burst, slack), the walk grid one axis at a time (it slides along walls, never crosses one) and a gap to people; the agent is then where it stands for every rule; a run is one span from Shift down to Shift up (runAlert once a span); pushing a busy agent cancels its act at the cancel cost (onBusy), release stands it (onRelease); a playground start may set pace, onBusy, onRelease, gap (Start.drive); Start.holds (test only) places a held scene at a door (N85); the official car faces along its bay at block-spot (site car.blockFacing, read by CarHeading only); the rulebook and the site move every inputs hash. 0.8.2: a person walks a routine leg 0..12 % faster than its timing, by a hash of their id, and waits at its end (rulebook crowd.pace, brief 023, Pulse: people who leave a stop on the same tick no longer walk as one block); every moving car keeps to the site's drive grid (rulebook car.drive, brief 023: carriageways, bays and drives, never a pavement): the turned driver reaches the service bay (mission point service-bay, parked facing car.serviceFacing) by the road and the drive, and the block move stops at block-spot's nearest drivable cell; a car drives each drive-grid cell at its surface's pace (rulebook car.pace, Motive 2026-10-19: road 5 m/s, drive or bay 2 m/s; the site marks cells r and y): the turned driver is in the service bay about 13 s after driverLast and waits there for Vale (the block move keeps blockMoveS); a car keeps off the kerb (Grid.CarRoute: a carriageway's lane, the middle of a drive or bay; the site's drive.lane); fire orders on final-fight hostiles end when the final fight is over and new ones are refused fight-over (rulebook fight.over, Motive 2026-10-19); the rulebook and the site move every inputs hash. 0.8.1: the summoned partner stands at her side (brief 022 §7.1, Pulse): his stop is the first of the baked rounds guard.stops with nobody within rounds.clear (partners-clear-cm) at the call, else the farthest from anyone; the rulebook and the rounds file move every inputs hash. 0.8.0: the living café (brief 022, Sky 2026-10-18): a server works her round from the baked sim/data/handover.rounds.json (held while spoken to, called home by the summons, ended by Alert 2, a shot, a body found or the final fight); the summoned lover stops rounds.close short of her post, facing her; Path.Heading skips zero-length segments (moves no run); the rounds file moves every inputs hash. 0.7.0: the getaway (brief 018, Sky 2026-10-18): the hold is 8 m or the exit lane (site getaway.exit) with one blocker per fight, Board and Go replace Extract, left behind at Go (captured or walks out), seen leaving (+1 T, the car hunted), the custody projection; the rulebook, the people rules and the site move every inputs hash. 0.6.3: open needs phase 1 (brief 017): the people file is suborned.sim.people/2, every person carries a need record (kind, label, size, holder), read strictly and never acted on; behaviour unchanged, only the inputs hash moves. 0.6.2: a public fight moves Standing once, by whom the cell fought (fight.public, Sky 2026-10-16; retires 0.6.1's gunfire.heard). 0.6.1: a public gunfight costs Standing (gunfire.heard, Sky 2026-10-16). 0.6.0: brief 014 (private turning; Standing starts at +2, the word and the drift between missions). 0.5.0: brief 013 (takedowns, bodies, reports, the response dial, check-in marks)
        public static string Mvid => typeof(Engine).Assembly.ManifestModule.ModuleVersionId.ToString("D");

        public static Inputs FromTexts(string rulebook, string peopleRules, string site, string? people = null, string? provisional = null, string? takedown = null, string? rounds = null)
        {
            rounds = rounds == null ? null : JNode.Canon(rounds);
            rulebook = JNode.Canon(rulebook); peopleRules = JNode.Canon(peopleRules); site = JNode.Canon(site); people = people == null ? null : JNode.Canon(people);
            provisional = provisional == null ? null : JNode.Canon(provisional); takedown = takedown == null ? null : JNode.Canon(takedown);
            var i = new Inputs { Rulebook = rulebook, PeopleRules = peopleRules, Site = site, People = people ?? "", Provisional = provisional ?? "", Takedown = takedown ?? "", Rounds = rounds ?? "" };
            i.Rules = new Rules(rulebook, peopleRules); i.SiteData = new Site(site);
            if (provisional != null) i.SiteData.AddProvisional(provisional);
            if (takedown != null) i.SiteData.AddTakedownWorld(takedown);
            if (rounds != null) i.SiteData.Rounds = RoundsData.Parse(rounds, i.Rules, i.SiteData); // brief 022: the mission's data turns the round on
            if (people != null)
            {
                var cast = new List<string>(); foreach (var c in i.SiteData.Cast) cast.Add(c.Id);
                i.Crowd = new Crowd(people, i.Rules, cast);
                i.Crowd.Warm(i.SiteData); // every routine leg's walk, once, at load: no tick pays for it
            }
            return i;
        }
        /// <summary>The repository root: the nearest directory above `from` holding contracts/rules/rulebook.json.</summary>
        public static string FindRoot(string from)
        {
            var d = new System.IO.DirectoryInfo(from);
            while (d != null) { if (System.IO.File.Exists(System.IO.Path.Combine(d.FullName, RulebookFile))) return d.FullName; d = d.Parent; }
            throw new System.IO.FileNotFoundException($"no {RulebookFile} above {from}");
        }

        /// <summary>Brief 029: the fight range's sites (tools/sim/range.ts), by layout.</summary>
        public static readonly IReadOnlyDictionary<string, string> RangeSiteFiles = new SortedDictionary<string, string>(StringComparer.Ordinal) { ["open"] = "sim/data/range.site.json", ["dense"] = "sim/data/range-dense.site.json", ["lane"] = "sim/data/range-lane.site.json" }; // lane: tuning.md §10.4, the open layout on a 90 m floor (F8-BREAK)
        static readonly Dictionary<string, Inputs> rangeCache = new Dictionary<string, Inputs>(StringComparer.Ordinal);
        /// <summary>Brief 029: the inputs of a fight range run: the rulebook, the people rules and the range's site (layout open or dense); no
        /// people file, no provisional, takedown or rounds file (the range has no crowd, no response dial, no round).</summary>
        public static Inputs LoadRange(string root, string layout = "open")
        {
            if (!RangeSiteFiles.TryGetValue(layout, out var file)) throw new FormatException($"range layout '{layout}' is not one of {string.Join(", ", RangeSiteFiles.Keys)}");
            var key = root + "|" + layout;
            lock (rangeCache)
            {
                if (rangeCache.TryGetValue(key, out var c)) return c;
                string rd(string f) => System.IO.File.ReadAllText(System.IO.Path.Combine(root, f));
                var i = FromTexts(rd(RulebookFile), rd(PeopleRulesFile), rd(file));
                if (!i.SiteData.IsRange) throw new FormatException($"{file} is not a range site (mission range)");
                rangeCache[key] = i; return i;
            }
        }

        public static Game New(Inputs i, Start? start = null) => new Game(i.Rules, i.SiteData, start, i.Hash.ToString(System.Globalization.CultureInfo.InvariantCulture), i.Crowd);

        /// <summary>Play a start state and its orders (sorted by tick, stable) to `until` or the outcome.</summary>
        public static Game Play(Inputs i, Start start, IReadOnlyList<Order> orders, int until)
        {
            var g = New(i, start);
            if (until <= 0) until = Game.T(i.SiteData.Over); // a script without `until` plays to the close mission.json gives
            var list = new List<(int idx, Order o)>(); for (int k = 0; k < orders.Count; k++) list.Add((k, orders[k]));
            list.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.idx.CompareTo(b.idx));
            foreach (var (_, o) in list)
            {
                if (o.Tick > until) break; // orders past `until` are not played (Redline I2)
                g.RunTo(o.Tick);
                if (g.Over && g.Tick != o.Tick) break; // the mission ended before this order's tick: nothing later was given
                if (g.Tick == o.Tick) g.Issue(o);      // at the tick it ended, the played run took (and refused) it: so does the replay
            }
            g.RunTo(until); g.Finish();
            return g;
        }
        /// <summary>Replay a finished game from its own start state and order log.</summary>
        public static Game Replay(Inputs i, Game played)
        {
            var orders = new List<Order>(); foreach (var (o, _, _, _) in played.Orders) orders.Add(o);
            return Play(i, played.Start, orders, played.Tick);
        }
    }
}
