// Real-time shoot-outs and running (Sky 2026-10-15: approved Motive's briefs/011/motive-fights.md, all five answers as recommended).
// Every number is the rulebook's `weapons` section (and cell.runSpeed, cell.runAlert, cell.carry, alert.gunfire). The rules here:
//  · A shot's accuracy is its band's (point blank, effective, long) times the modifiers that apply (Rules.Accuracy). Hits are counted,
//    not rolled: each shooter keeps an integer accumulator on its target, starting at acc.start; each shot adds the accuracy, and at
//    1000 or more the shot hits and 1000 comes off. At 750 that is exactly three hits in four. No seed, no dice: same orders, same hits.
//  · The cost moves from the shot to the victim: +hit Trail per person hit (once), +down more when they go down; any gunfire raises
//    Alert to at least alert.gunfire; the final fight stays untraced (fight.traced). A drawn weapon seen by security raises Alert to
//    costs.drawSeenSecurity and that watcher engages; seen by people it is one witnessed incident.
//  · Running is noticed where an act would be: people remember where and when (and the face), security counts a found fault once a run.
//    Running is free once a fight is on, or the run is loud.
// Hostile behaviour beyond "shoot what you see, by priority" (advance to cover, rivals break, police hold) is Pulse's spec: not here yet.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>The going-loud card (motive-fights.md §2.6): what an order would set off, read before it is given. The engine never pauses.</summary>
    public sealed class LoudCard
    {
        public bool Loud;                 // the order would make the run loud (Alert ≥ 2 or a shot) when it isn't yet
        public string Why = "";           // draw-seen, fire, panic; or why not: already-loud, fight-on, unseen-draw, no-change
        public string Refused = "";       // the order would be refused, and why (the card shows nothing to commit)
        public int AlertTo;               // Alert after the order: 2, or 3 with civilians watching violence
        public int Civilians;             // civilians who would see it now
        public string ReportedBy = "";    // witness, seen, unseen, civilian, rivals-unseen, none
        public int PoliceInS = -1;        // the response unit's delay from now, -1 if none is called
        public int SecurityArrives = -1, Seal = -1; // ticks: the response unit on the street, the district sealed
        public int RivalsInS = -1;        // the rivals drawn by gunfire: seconds until they come (0: already here or coming)
        public bool PatrolBreaks;         // the patrol is within patrol.breakTo and breaks its route
        public string FightAfter = "";    // the final fight's branch after this order ("loud"), "" if the fight is not to come
        public int Officers, ResponseOfficers; // the loud fight's officers from the north street, and the response unit's
        public bool GuardAllyLost, WarningLost; // the safe fight's ally and Inez's warning, lost
        public int TrailPerHit, TrailPerDown, HoldTicks; // the price per person, and the card's commit hold
    }

    /// <summary>The price chip for a move (motive-fights.md §1.2): walk time, run time, who would see the run now, and whether it can run.</summary>
    public sealed class MoveQuote
    {
        public int WalkS, RunS; public string RunRefused = "";
        public readonly List<string> RunSeenBy = new List<string>(); // watchers whose cone the route crosses now
        public int RunPeople;             // people within witness range of the route now ("~": people move)
        public int AlertIfRun;            // Alert after the run if security sees it
        public bool RunFree;              // a fight is on or the run is loud: nobody counts a runner
    }

    public sealed partial class Game
    {
        public Gun GuardGun = null!, PatrolGun = null!, RivalsGun = null!;
        public Gun[] SecGuns = Array.Empty<Gun>();
        readonly Dictionary<string, Gun> civGuns = new Dictionary<string, Gun>(StringComparer.Ordinal);
        public readonly List<string> Captured = new List<string>();
        readonly HashSet<string> drawSeen = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> violenceBy = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, Incident> fireIncident = new Dictionary<string, Incident>(StringComparer.Ordinal);
        public bool CellFiredOn; // someone has fired on the cell this mission: a fight is on

        Gun NewGun(string id, Weapon? w, bool trained, int health) => new Gun(id) { W = w, Trained = trained, Health = health, Max = health };

        static bool IsOpId(string id) => id == "scout" || id == "fixer" || id == "tech" || id == "enforcer";
        static bool IsCivilianTarget(string id) => id == "waitress" || id == "driver" || id == "councillor";
        bool IsFightHostile(string id) { foreach (var h in Hostiles) if (h.Id == id) return true; return false; }
        Hostile? HostileOf(string id) { foreach (var h in Hostiles) if (h.Id == id) return h; return null; }
        int SecIndex(string id) { for (int k = 0; k < SecGuns.Length; k++) if (SecGuns[k].Id == id) return k; return -1; }
        /// <summary>A person, not the patrol unit: violence on them seen by a civilian is §05's "violence seen by a civilian".</summary>
        bool IsPersonTarget(string id) => id != "patrol" && !IsOpId(id);

        /// <summary>A fight is on (motive-fights.md §2.4 panic, §1.1 running free): someone has fired on the cell, or the final fight is on.</summary>
        public bool FightOn => CellFiredOn || FightSprung != INF || FightBranch == "loud";
        bool RunFree(Op op) => FightOn || Alert >= 2 || Gunfire > 0 || UnderFire(op);
        bool UnderFire(Op op)
        {
            string id = Sim.Op.Name(op.Id);
            if (GuardGun.Target == id || PatrolGun.Target == id || RivalsGun.Target == id) return true;
            foreach (var g in SecGuns) if (g.Target == id) return true;
            foreach (var h in Hostiles) if (h.Gun.Target == id) return true;
            return false;
        }

        public Gun? GunOf(string id)
        {
            if (IsOpId(id)) return OpOf(Sim.Op.Parse(id)).Gun;
            switch (id) { case "guard": return GuardGun; case "patrol": return PatrolGun; case "rivals": return RivalsGun; }
            if (civGuns.TryGetValue(id, out var c)) return c;
            int k = SecIndex(id); if (k >= 0) return SecGuns[k];
            return HostileOf(id)?.Gun;
        }

        /// <summary>Where a shooter or target stands now, or null when it is not out on the street to be shot at.</summary>
        public Pt? PosOf(string id)
        {
            if (IsOpId(id)) { var op = OpOf(Sim.Op.Parse(id)); return op.Inside ? (Pt?)null : op.Pos; }
            switch (id)
            {
                case "guard": return GuardDown ? null : GuardPose()?.pos;
                case "patrol": return PatrolDown ? null : PatrolPose()?.pos;
                case "rivals": return RivalsPresent ? RivalsPose()?.pos : null;
                case "waitress": case "driver": case "councillor": return PersonPos(PersonByRole(id));
            }
            int k = SecIndex(id); if (k >= 0) return SecOfficerPose(k)?.pos;
            var h = HostileOf(id); return h != null && h.Present && !h.Down ? h.Pos(Tick) : (Pt?)null;
        }

        bool Moving(string id)
        {
            if (IsOpId(id)) return OpOf(Sim.Op.Parse(id)).Walk != null;
            switch (id)
            {
                case "guard": { if (GuardAlly) return GuardAllyMoving; var g = GuardPose(); return g.HasValue && (g.Value.phase == "walking to café" || g.Value.phase == "returning to door"); }
                case "patrol": return !PatrolDown && PatrolAwayUntil <= Tick; // sweeping or chasing, it is never still
                case "rivals":
                    {
                        if (!RivalsDrawn || !RivalsPresent) return false;
                        var a = RivalsPose(); if (!a.HasValue) return false;
                        var path = RivalsDelayedUntil > RivalsDrawnTick ? rivalsRestartPath : rivalsPath;
                        return path != null && a.Value.pos != path.End;
                    }
            }
            int k = SecIndex(id);
            if (k >= 0) { long mm = (long)(Tick - SecurityArrives) * secSpeed - k * SecFileMm; return mm < Math.Max(0, securityPath.Length - k * SecFileMm); }
            return HostileOf(id)?.Moving(Tick) ?? false;
        }

        /// <summary>A target standing still beside cover (rulebook weapons.mod.cover). The cover points are Pulse's build-time data for the
        /// site (motive-fights.md §2.7); until they land, nobody stands beside cover.</summary>
        bool BesideCover(string id, Pt at) => false;

        static long Dist(Pt a, Pt b) => G.Isqrt(G.D2(a, b));
        bool Ready(Gun g) => g.Drawn && g.HolsterAt < 0 && Tick >= g.DrawReady;

        // ---------------------------------------------------------------- the tick

        void Combat()
        {
            foreach (var op in Ops) { var g = op.Gun; if (g.HolsterAt >= 0 && Tick >= g.HolsterAt) { g.Drawn = false; g.HolsterAt = -1; } }
            foreach (var op in Ops) { CellShooter(op); if (Over) return; }
            // the city: the guard, the patrol, the response unit's officers, the rival crew; then the final fight's hostiles
            if (GuardAlly) { if (FightSprung != INF) AllyShooter(); }
            else if (GuardEngaged && !GuardDown) HostileShooter("guard", GuardGun, PosOf("guard"), GuardWatching, false);
            if (Over) return;
            PatrolTracking = false;
            if (PatrolChasing && !PatrolDown && PatrolAwayUntil <= Tick) { HostileShooter("patrol", PatrolGun, PosOf("patrol"), PatrolWatching, false); PatrolTracking = PatrolGun.Target.Length > 0; }
            if (Over) return;
            SecurityTracking = false;
            for (int k = 0; k < SecGuns.Length; k++)
            {
                int kk = k; HostileShooter(SecGuns[k].Id, SecGuns[k], SecOfficerPose(k)?.pos, p => SecOfficerWatching(kk, p), false);
                SecurityTracking |= SecGuns[k].Target.Length > 0;
                if (Over) return;
            }
            RivalsTracking = false;
            if (RivalsDrawn && RivalsPresent) { HostileShooter("rivals", RivalsGun, PosOf("rivals"), RivalsWatching, false); RivalsTracking = RivalsGun.Target.Length > 0; }
            if (Over) return;
            foreach (var h in Hostiles)
            {
                if (!h.Present) continue;
                var at = h.Pos(Tick); var w = h.Gun.W!;
                HostileShooter(h.Id, h.Gun, at, p => G.Within(at, p, w.MaxCm) && Sees(at, p), true);
                if (Over) return;
            }
        }

        /// <summary>An operative shoots: its standing fire order, else what its stance allows. Not while running, carrying, inside or doing
        /// an action; a walking operative fires at the moving rate (motive-fights.md §1.1, §2.4).</summary>
        void CellShooter(Op op)
        {
            var g = op.Gun; string name = Sim.Op.Name(op.Id);
            if (g.Down || g.W == null || op.Inside || !g.Drawn) return;
            if (g.Order.Length > 0)
            {
                var tg = GunOf(g.Order);
                if (tg == null || tg.Down || !PosOf(g.Order).HasValue)
                {
                    Note("fire.done", $"{name}: {g.Order} is {(tg != null && tg.Down ? "down" : "gone")}. The fire order ends.");
                    g.Order = ""; g.Force = false; g.Target = ""; g.LostAt = -1;
                }
            }
            if (op.Busy != null || op.Running || op.Carrying.HasValue || !Ready(g)) return;
            string t = g.Order.Length > 0 ? g.Order : StanceTarget(op);
            if (t.Length == 0) { g.Target = ""; return; }
            var at = PosOf(t)!.Value; long d = Dist(op.Pos, at);
            if (d > g.W.MaxCm || !Sees(op.Pos, at))
            {
                if (t == g.Order)
                {
                    if (g.LostAt < 0) g.LostAt = Tick;
                    else if (Tick - g.LostAt >= T(R.LineLapseS)) { Note("fire.lapses", $"{name} loses the line on {t}. The fire order lapses."); g.Order = ""; g.Force = false; g.Target = ""; g.LostAt = -1; }
                }
                return;
            }
            g.LostAt = -1;
            Aim(g, t, false);
            if (Tick >= g.NextShot) Shoot(name, g, op.Pos, t, at, d, op.Walk != null, op.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer);
        }

        /// <summary>Who an operative's stance fires at (motive-fights.md §2.4): hold, nobody; return, whoever fired on it; free, the nearest
        /// combatant. A combatant is a final-fight hostile, or a guard, patrol, officer or rival crew that has fired or aims at the cell;
        /// never a civilian, a contact or Vale, so a stance can never start a loud run by itself.</summary>
        string StanceTarget(Op op)
        {
            var g = op.Gun;
            if (g.Stance == "hold") return "";
            if (g.Stance == "return") return g.ShotBy.Length > 0 && Combatant(g.ShotBy) && InLine(op.Pos, g.ShotBy, g.W!.MaxCm) ? g.ShotBy : "";
            string best = ""; long bd = long.MaxValue;
            foreach (var id in CombatantIds())
            {
                if (!Combatant(id)) continue;
                var at = PosOf(id); if (!at.HasValue) continue;
                long d = G.D2(op.Pos, at.Value);
                if (d < bd && d <= (long)g.W!.MaxCm * g.W.MaxCm && Sees(op.Pos, at.Value)) { bd = d; best = id; }
            }
            return best;
        }
        bool InLine(Pt from, string id, int maxCm) { var at = PosOf(id); return at.HasValue && G.Within(from, at.Value, maxCm) && Sees(from, at.Value); }
        IEnumerable<string> CombatantIds()
        {
            yield return "guard"; yield return "patrol";
            foreach (var g in SecGuns) yield return g.Id;
            yield return "rivals";
            foreach (var h in Hostiles) yield return h.Id;
        }
        bool Combatant(string id)
        {
            var g = GunOf(id); if (g == null || g.Down || !PosOf(id).HasValue) return false;
            if (IsFightHostile(id)) return HostileOf(id)!.Present;
            if (id == "guard" && GuardAlly) return false;
            if (IsOpId(id) || IsCivilianTarget(id)) return false;
            return IsOpId(g.Target) || g.Fired;
        }

        /// <summary>A hostile shoots (motive-fights.md §2.7 target priority): 1, whoever is shooting at it; 2, whoever holds Vale; 3, the
        /// nearest operative with a weapon out; 4, the nearest operative. Before the final fight only a foe is a target: an operative with a
        /// weapon out, who has fired, or who holds Vale. A first sight costs react; a new target, aimNew.</summary>
        void HostileShooter(string id, Gun g, Pt? posN, Func<Pt, bool> sees, bool anyOp)
        {
            if (g.Down || g.W == null || !posN.HasValue) { g.Target = ""; return; }
            var pos = posN.Value;
            string t = PickTarget(id, pos, sees, anyOp);
            if (t.Length == 0) { g.Target = ""; return; }
            Aim(g, t, true);
            if (Tick >= g.NextShot) { var at = PosOf(t)!.Value; Shoot(id, g, pos, t, at, Dist(pos, at), Moving(id), false); }
        }
        string PickTarget(string id, Pt pos, Func<Pt, bool> sees, bool anyOp)
        {
            string p1 = "", p3 = "", p4 = ""; long d1 = long.MaxValue, d3 = long.MaxValue, d4 = long.MaxValue; bool vale = false;
            foreach (var op in Ops)
            {
                if (op.Down || op.Inside) continue;
                bool foe = anyOp || op.Gun.Drawn || op.Gun.Fired || (op.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer);
                if (!foe || !sees(op.Pos)) continue;
                long d = G.D2(pos, op.Pos); string name = Sim.Op.Name(op.Id);
                if (op.Gun.Target == id && op.Gun.Fired && d < d1) { d1 = d; p1 = name; }
                if (op.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer) vale = true;
                if (op.Gun.Drawn && d < d3) { d3 = d; p3 = name; }
                if (d < d4) { d4 = d; p4 = name; }
            }
            if (anyOp && GuardAlly && !GuardDown && GuardGun.Target == id) // the guard ally shooting at this hostile
            {
                var ga = PosOf("guard"); if (ga.HasValue && sees(ga.Value)) { long d = G.D2(pos, ga.Value); if (d < d1) { d1 = d; p1 = "guard"; } }
            }
            return p1.Length > 0 ? p1 : vale ? "enforcer" : p3.Length > 0 ? p3 : p4;
        }

        /// <summary>The guard as the cell's ally in the safe fight: he shoots the nearest rival he can see.</summary>
        void AllyShooter()
        {
            var g = GuardGun; if (GuardDown) return;
            var pos = PosOf("guard"); if (!pos.HasValue) return;
            var h = NearestHostile(pos.Value, g.W!.MaxCm);
            if (h == null) { g.Target = ""; return; }
            Aim(g, h.Id, false);
            var at = h.Pos(Tick);
            if (Tick >= g.NextShot) Shoot("guard", g, pos.Value, h.Id, at, Dist(pos.Value, at), GuardAllyMoving, false);
        }

        /// <summary>Take a target: a new one resets the accumulator to acc.start and waits aimNew, or react on a hostile's first sight.</summary>
        void Aim(Gun g, string t, bool react)
        {
            if (g.Target == t) return;
            g.Target = t; g.Acc = R.AccStart;
            g.NextShot = Math.Max(g.NextShot, Tick + (react && !g.Engaged ? T(R.ReactS) : R.AimNewTicks));
        }

        // ---------------------------------------------------------------- a shot

        void Shoot(string by, Gun g, Pt from, string t, Pt at, long d, bool moving, bool vale)
        {
            var tg = GunOf(t)!;
            int acc = R.Accuracy(g.W!, d, moving, BesideCover(t, at), vale, !g.Trained);
            g.Acc += acc; bool hit = g.Acc >= 1000; if (hit) g.Acc -= 1000;
            g.NextShot = Tick + g.W!.Every; g.Fired = g.Engaged = true;
            tg.ShotBy = by;
            if (IsOpId(t)) CellFiredOn = true;
            Note(hit ? "shot.hit" : "shot.miss", $"{by} fires on {t} at {d / 100} m ({acc} ‰): {(hit ? "hit" : "miss")}.");
            bool cell = IsOpId(by), untraced = cell && IsFightHostile(t) && !R.FightTraced;
            if (cell) CellGunfire(OpOf(Sim.Op.Parse(by)), t, at, untraced);
            else if (!IsFightHostile(by) && !(by == "guard" && GuardAlly)) RaiseAlert(R.GunfireAlert, "gunfire:" + by); // any gunfire (§05)
            if (Over) return;
            if (hit && !tg.Down) Damage(t, tg, g.W.Damage, by, cell, untraced);
        }

        /// <summary>What the cell's shot sets off. Untraced in the final fight (fight.traced off, Sky 2026-10-15): the shots are counted apart
        /// and only wake the patrol. Otherwise: a shot fired, Alert to alert.gunfire, one incident per target with everyone who saw it,
        /// violence seen by civilians, the rivals drawn, the patrol woken, and the response unit called by whoever reports it.</summary>
        void CellGunfire(Op op, string t, Pt at, bool untraced)
        {
            string name = Sim.Op.Name(op.Id);
            if (untraced) { FightShots++; WakePatrol(op.Pos, op.Id); return; }
            Gunfire++;
            RaiseAlert(R.GunfireAlert, "gunfire:" + t);
            Person? victim = IsCivilianTarget(t) ? PersonByRole(t) : t == "guard" ? GuardP : null;
            // one incident per target (rulebook alert: every shot of one fight with one target is one incident); later shots add new witnesses
            if (!fireIncident.TryGetValue(t, out var inc))
            {
                incidents.Add("fire:" + t);
                inc = AddIncident(IsCivilianTarget(t) ? "violence" : "gunfire", (IsCivilianTarget(t) ? "violence:" : "gunfire:") + t, IsPersonTarget(t) ? at : op.Pos);
                fireIncident[t] = inc;
            }
            var seen = WitnessesWithPos(op.Pos, victim);
            if (IsPersonTarget(t)) foreach (var w in WitnessesWithPos(at, victim)) if (!seen.Exists(x => x.p == w.p)) seen.Add(w);
            foreach (var (p, wp) in seen)
            {
                if (inc.Witnesses.Exists(x => x.p == p)) continue;
                inc.Witnesses.Add((p, wp)); Remember(p, "saw-act", name, true);
            }
            int civSees = 0;
            if (IsPersonTarget(t))
                foreach (var c in WitnessesOf(at, victim))
                {
                    if (!c.Civilian) continue;
                    civSees++;
                    if (!violenceBy.Add(t + "/" + c.Id)) continue;
                    ViolenceSeen(t, c, name);
                    if (c == Inez && !InezWitnessedShooting) { InezWitnessedShooting = true; Note("witness.waitress", $"Inez sees the {t} shot at near her café. She is now a witness."); }
                    else if (c != Inez) Note("witness.crowd", $"{c.Name} sees the {t} shot at.");
                }
            if (IsCivilianTarget(t)) CivilianShotAt(PersonByRole(t), op);
            DrawRivals(); WakePatrol(op.Pos, op.Id);
            var (delay, text) = Reporter(t, op.Pos, civSees > 0);
            if (delay >= 0) CallSecurity(delay, text);
        }

        /// <summary>Who reports the cell's gunfire, and so how soon the response unit comes (rulebook security.response).</summary>
        (int delay, string text) Reporter(string t, Pt shooter, bool civSees)
        {
            if (IsCivilianTarget(t)) return (R.RespCivilianS, t == "waitress" ? "Inez is shot at the café. Witnesses call security." : t == "driver" ? "The official driver is shot at. Security rushes to the pickup." : "Shots at the councillor are reported.");
            if (t == "patrol") return (R.RespSeenS, "Gunfire at the patrol is reported.");
            if (t == "rivals") return CameraWatching("pickup", shooter) ? (R.RespSeenS, "The firefight with the rivals is reported.") : (R.RespRivalsUnseenS, "The firefight with the rivals is reported.");
            if (SecIndex(t) >= 0 || IsFightHostile(t)) return (-1, "");
            if (civSees) return (R.RespWitnessS, InezWitnessedShooting ? "Inez reports the shooting." : "A witness reports the shooting.");
            bool guard = t == "guard" ? GuardWatching(shooter) : false;
            if (guard) return (R.RespSeenS, "The guard calls in the shooting.");
            if (PatrolWatching(shooter)) return (R.RespSeenS, "The patrol reports gunfire.");
            if (CameraWatching("entrance", shooter)) return (R.RespSeenS, "The entrance camera reports gunfire.");
            return (R.RespUnseenS, "Shots are heard in the district.");
        }

        /// <summary>A civilian or a contact shot at (§05: violence → Alert 3 at once; rulebook traceable violence): the guard, away from his
        /// door, stops to search for the shooter.</summary>
        void CivilianShotAt(Person p, Op op)
        {
            RaiseAlert(R.ViolenceSeen, $"violence-seen:{p.Role}");
            if (incidents.Add("seen:" + p.Role)) { AddTrail(R.WitnessTrail, $"witnessed:violence:{p.Role}"); MoveStanding(-R.StandingStep, $"violence-seen:{p.Role}"); }
            var g = GuardPose();
            if (g.HasValue && g.Value.phase != "door" && !GuardHeld)
            {
                GuardHeld = true; heldPos = g.Value.pos; heldFacing = g.Value.facing; heldPhase = "searching for the shooter"; GuardAwayUntil = INF;
                if (GuardWatching(op.Pos)) GuardEngaged = true;
            }
        }

        // ---------------------------------------------------------------- a hit

        void Damage(string t, Gun tg, int dmg, string by, bool cell, bool untraced)
        {
            tg.Health = Math.Max(0, tg.Health - dmg);
            bool traced = cell && !untraced;
            if (traced && !tg.Hit) AddTrail(R.HitTrail, "hit:" + t);
            tg.Hit = true;
            bool down = tg.Health == 0;
            if (down && t != "rivals") { tg.Down = true; if (traced) AddTrail(R.DownTrail, "down:" + t); }
            if (IsOpId(t)) { OpHit(OpOf(Sim.Op.Parse(t)), down, by); return; }
            switch (t)
            {
                case "guard": GuardHit(down, by); return;
                case "patrol":
                    if (down) { patrolDownAt = PatrolPose()!.Value.pos; PatrolDown = true; PatrolChasing = false; Chain.Add("patrol-down"); Note("patrol.down", "The patrol goes down. Its distress signal is already out."); }
                    else Note("patrol.hit", $"The patrol is hit. {tg.Health} left.");
                    return;
                case "rivals":
                    if (down)
                    {
                        tg.Health = tg.Max; RivalsDelayedUntil = Tick + T(R.RivalsRetreatS); RivalsDelayMethod = "fire"; // running away isn't a drop (no Trail)
                        Chain.Add("rivals-driven-back"); Note("rivals.retreat", "The rivals retreat for now.");
                    }
                    else Note("rivals.hit", "The rival crew takes a hit.");
                    return;
                case "waitress": case "driver": case "councillor": CivilianHit(PersonByRole(t), down); return;
            }
            int k = SecIndex(t);
            if (k >= 0)
            {
                bool all = true; foreach (var s in SecGuns) if (!s.Down) all = false;
                if (all && !SecurityDown) { SecurityDown = true; Chain.Add("response-unit-down"); Note("security.down", "The response unit is down. The wider cordon is still closing."); }
                else Note(down ? "security.officer-down" : "security.hit", down ? $"{t} is down." : $"{t} is hit.");
                return;
            }
            var h = HostileOf(t);
            if (h != null)
            {
                string who = by == "enforcer" ? "The Enforcer" : by == "guard" ? "The guard" : "The " + by;
                if (down) { if (by == "enforcer") DownedByEnforcer++; else if (by == "guard") DownedByGuard++; else DownedByCell++; Note("fight.down", $"{who} drops {t}."); }
                else Note("fight.hit", $"{who} hits {t}.");
            }
        }

        void OpHit(Op op, bool down, string by)
        {
            string name = Sim.Op.Name(op.Id);
            if (FightBranch.Length > 0) FightHits++;
            Note(name + ".hit." + (IsFightHostile(by) ? "fight" : by), $"{by} hits the {name}. {op.Gun.Health} health left.");
            if (!down) return;
            if (op.Id == OpId.Enforcer) { Fail("enforcer-down", "The Enforcer is down."); return; }
            if (op.Busy != null) { Cash += op.BusyPaid; op.Busy = null; op.BusyQuote = null; op.BusyPaid = 0; } // an action cut short: the price comes back, as a refusal's does
            op.Walk = null; op.WalkMm = 0; op.Running = false; op.Gun.Order = ""; op.Gun.Target = "";
            if (op.Carrying.HasValue) { OpOf(op.Carrying.Value).CarriedBy = null; op.Carrying = null; }
            Note(name + ".down", $"The {name} is down. Carry them to the car, or they are captured.");
        }

        void GuardHit(bool down, string by)
        {
            if (down)
            {
                GuardDown = true; GuardP.Down = true; GuardEngaged = false; MissingNotice = Tick + T(R.CheckInS);
                Chain.Add("guard-shot"); Note("guard.down", "The guard falls. His missed check-in will draw more attention.");
                return;
            }
            var pose = GuardPose()!.Value;
            if (pose.phase != "door" && !GuardHeld) { GuardHeld = true; heldPos = pose.pos; heldFacing = pose.facing; heldPhase = "wounded"; GuardAwayUntil = INF; }
            bool seen = IsOpId(by) && GuardWatching(OpOf(Sim.Op.Parse(by)).Pos);
            if (seen && !GuardAlly) GuardEngaged = true;
            Note("guard.hit", seen ? "The guard is hit but fires back." : "The guard is hit. He cannot see the shooter yet.");
        }

        void CivilianHit(Person p, bool down)
        {
            if (!down) { Note("civilian.hit", $"{p.Name} ({p.Role}) is hit."); return; }
            if (p == ValeP)
            {
                Vale = ValeAt.Dead; ValeP.Down = true;
                Fail("vale-dead", "Vale is dead. The live capture objective has failed.");
                return;
            }
            if (p == Driver) carStoppedAt = CarPos();
            p.Down = true; p.Turned = false;
            Chain.Add(p.Role + "-shot");
            Note("civilian.down", $"{p.Name} ({p.Role}) is down.");
            if (p == Driver && Vale != ValeAt.Office) Fail("driver-shot", "The driver is shot during handover. Security pulls Vale away.");
        }

        // ---------------------------------------------------------------- what the city sees

        /// <summary>A drawn weapon is an act (motive-fights.md §2.3). Seen by security (the guard, the patrol, a live camera, the response unit):
        /// Alert to weapons.costs.drawSeenSecurity, the watcher engages and calls it in. Seen by people (civilians within witness range):
        /// one witnessed incident, Alert at least drawSeenPeople, and they remember the face. Once per draw each; not in the final fight.</summary>
        void DrawnSeen()
        {
            if (FightBranch.Length > 0) return; // the final fight is untraced (fight.traced)
            foreach (var op in Ops)
            {
                var g = op.Gun; if (!g.Drawn || g.Down || op.Inside) continue;
                string name = Sim.Op.Name(op.Id);
                foreach (var w in SecurityWatchers)
                    if (!drawSeen.Contains($"{w}/{name}/{g.DrawTick}") && WatcherSees(w, op.Pos)) { drawSeen.Add($"{w}/{name}/{g.DrawTick}"); SecuritySawDraw(w, op); if (Over) return; }
                string pk = $"people/{name}/{g.DrawTick}";
                if (drawSeen.Contains(pk)) continue;
                var ws = WitnessesWithPos(op.Pos, null).FindAll(x => x.p.Civilian);
                if (ws.Count == 0) continue;
                drawSeen.Add(pk);
                AddIncident("weapon-drawn", "weapon-drawn:" + name, op.Pos, ws);
                if (incidents.Add(pk)) { AddTrail(R.WitnessTrail, $"witnessed:weapon-drawn:{name}"); MoveStanding(R.Traceable["weapon-drawn"] * R.StandingStep, "witnessed:weapon-drawn"); }
                RaiseAlert(R.DrawSeenPeople, $"drawn-seen:people:{name}");
                foreach (var (p, _) in ws) Remember(p, "saw-act", name, true);
                Note("witness.weapon-drawn", $"{ws.Count} see the {name}'s weapon drawn.");
            }
        }
        static readonly string[] SecurityWatchers = { "guard", "patrol", "camera:entrance", "camera:pickup", "security" };
        bool WatcherSees(string w, Pt p) => w switch
        {
            "guard" => !GuardAlly && GuardWatching(p),
            "patrol" => PatrolWatching(p),
            "camera:entrance" => CameraWatching("entrance", p),
            "camera:pickup" => CameraWatching("pickup", p),
            _ => SecurityWatching(p),
        };
        string? SecurityWatcherOf(Pt p) { foreach (var w in SecurityWatchers) if (WatcherSees(w, p)) return w; return null; }

        void SecuritySawDraw(string w, Op op)
        {
            string name = Sim.Op.Name(op.Id);
            RaiseAlert(R.DrawSeenSecurity, $"drawn-seen:{w}:{name}");
            Note("drawn.seen", $"The {w.Replace("camera:", "")} {(w.StartsWith("camera", StringComparison.Ordinal) ? "camera shows" : "sees")} the {name}'s weapon drawn. Alert {Alert}.");
            switch (w)
            {
                case "guard":
                    if (GuardDown) return;
                    { var pose = GuardPose()!.Value; if (pose.phase != "door" && !GuardHeld) { GuardHeld = true; heldPos = pose.pos; heldFacing = pose.facing; heldPhase = "drawing on the cell"; GuardAwayUntil = INF; } }
                    GuardEngaged = true; Remember(GuardP, "saw-act", name, true);
                    CallSecurity(R.RespSeenS, "The guard calls in a drawn weapon.");
                    return;
                case "patrol":
                    if (!PatrolChasing && !PatrolDown && PatrolAwayUntil <= Tick) { chasePos = PatrolPose()!.Value.pos; PatrolChasing = true; chaseOp = op.Id; Note("patrol.breaks", "The patrol breaks its route and turns on the drawn weapon."); }
                    CallSecurity(R.RespSeenS, "The patrol reports a drawn weapon.");
                    return;
                case "camera:entrance": case "camera:pickup":
                    CallSecurity(R.RespSeenS, "The watchers on the camera report a drawn weapon.");
                    return;
            }
        }

        /// <summary>The attention a run draws, a tick at a time (motive-fights.md §1.1): people within witness range on a clear line remember
        /// where and when (and the face: memory by the operative); security that sees it counts a found fault, Alert to cell.runAlert, once a
        /// run. No Trail, no standing. Running is free once a fight is on or the run is loud.</summary>
        void RunAttention(Op op)
        {
            if (RunFree(op)) return;
            string name = Sim.Op.Name(op.Id);
            foreach (var (p, _) in WitnessesWithPos(op.Pos, null)) if (op.RunSeen.Add(p.Id)) Remember(p, "saw-runner", name, true);
            if (!GuardDown && !GuardAlly && GuardWatching(op.Pos) && op.RunSeen.Add(GuardP.Id)) Remember(GuardP, "saw-runner", name, true);
            if (op.RunAlerted) return;
            var w = SecurityWatcherOf(op.Pos); if (w == null) return;
            op.RunAlerted = true;
            RaiseAlert(R.RunAlert, $"runner-seen:{w}:{name}");
            Note("run.seen", $"The {w.Replace("camera:", "")} {(w.StartsWith("camera", StringComparison.Ordinal) ? "camera" : "")} sees the {name} running: a found fault. Alert {Alert}.".Replace("  ", " "));
        }

        // ---------------------------------------------------------------- the orders

        void StartDraw(Op op)
        {
            var g = op.Gun;
            g.Drawn = true; g.HolsterAt = -1; g.DrawReady = Tick + R.DrawTicks; g.DrawTick = Tick; g.Stance = "return"; g.Target = "";
        }

        string Draw(Op op, Order o)
        {
            var g = op.Gun;
            if (g.W == null) return $"{o.Op} carries no weapon (Arms)";
            if (g.Drawn && g.HolsterAt < 0) return "the weapon is already drawn";
            StartDraw(op);
            Note("order.draw", $"{Sim.Op.Name(op.Id)} draws ({R.DrawTicks / Hz}.{R.DrawTicks % Hz} s).");
            return "";
        }
        string Holster(Op op, Order o)
        {
            var g = op.Gun;
            if (!g.Drawn || g.HolsterAt >= 0) return "no weapon drawn";
            g.HolsterAt = Tick + R.HolsterTicks; g.Order = ""; g.Force = false; g.Target = "";
            Note("order.holster", $"{Sim.Op.Name(op.Id)} holsters.");
            return "";
        }
        string Cease(Op op, Order o)
        {
            var g = op.Gun;
            if (!g.Drawn) return "no weapon drawn";
            g.Order = ""; g.Force = false; g.Target = ""; g.LostAt = -1;
            Note("order.cease", $"{Sim.Op.Name(op.Id)} ceases fire.");
            return "";
        }
        string Stance(Op op, Order o)
        {
            if (op.Gun.W == null) return $"{o.Op} carries no weapon (Arms)";
            if (o.Stance != "hold" && o.Stance != "return" && o.Stance != "free") return $"stance '{o.Stance}' is unknown (hold, return, free)";
            op.Gun.Stance = o.Stance;
            Note("order.stance", $"{Sim.Op.Name(op.Id)}: {o.Stance}.");
            return "";
        }
        string Panic(Order o)
        {
            if (o.Op != "cell") return "panic is the cell's: op \"cell\"";
            if (!FightOn) return "no fight: panic works only once a fight is on";
            int n = 0;
            foreach (var op in Ops)
            {
                if (op.Down || op.Inside || op.Gun.W == null) continue;
                if (!op.Gun.Drawn || op.Gun.HolsterAt >= 0) StartDraw(op);
                op.Gun.Stance = "free"; n++;
            }
            if (n == 0) return "nobody armed can fire";
            Note("order.panic", $"Panic: {n} armed operatives fire at will.");
            return "";
        }

        /// <summary>Resolve a fire order's target: "hostiles" and "security" are the nearest one up with a line; others name one.</summary>
        string FireTarget(Op op, string target)
        {
            int max = op.Gun.W!.MaxCm;
            if (target == "hostiles") return NearestHostile(op.Pos, max)?.Id ?? "";
            if (target == "security")
            {
                string best = ""; long bd = long.MaxValue;
                for (int k = 0; k < SecGuns.Length; k++)
                {
                    var at = SecOfficerPose(k)?.pos; if (!at.HasValue) continue;
                    long d = G.D2(op.Pos, at.Value); if (d < bd && d <= (long)max * max && Sees(op.Pos, at.Value)) { bd = d; best = SecGuns[k].Id; }
                }
                if (best.Length == 0) for (int k = 0; k < SecGuns.Length; k++) if (SecOfficerPose(k).HasValue) return SecGuns[k].Id; // none in line: the first up, to say why
                return best;
            }
            return target;
        }

        string Fire(Op op, Order o)
        {
            var g = op.Gun;
            if (g.W == null) return $"{o.Op} carries no weapon (Arms)";
            if (op.Running) return "a running operative doesn't fire: walk or stop first";
            if (op.Carrying.HasValue) return "carrying, the operative doesn't fire";
            string t = FireTarget(op, o.Target);
            if (t.Length == 0) return o.Target == "hostiles" ? "no hostile in range with a clear line" : $"no {o.Target} to fire at";
            if (IsOpId(t)) return "the cell doesn't fire on its own";
            var tg = GunOf(t); if (tg == null) return $"target '{o.Target}' is unknown (unknown = reject)";
            if (IsCivilianTarget(t) && !o.Force) return $"{t} is a civilian or a contact: a target only when forced (Ctrl)";
            var at = PosOf(t);
            if (!at.HasValue || tg.Down) return $"no {o.Target} to fire at";
            if (!G.Within(op.Pos, at.Value, g.W.MaxCm)) return $"out of range ({g.W.MaxCm / 100} m)";
            if (!Sees(op.Pos, at.Value)) return "no clear line";
            if (!g.Drawn || g.HolsterAt >= 0) StartDraw(op);
            g.Order = t; g.Force = o.Force; g.LostAt = -1;
            Actions.Add("fire:" + o.Target);
            Note("fire." + o.Target, $"{Sim.Op.Name(op.Id)} fires on {t} until down.");
            return "";
        }

        string Carry(Op op, Order o)
        {
            if (op.Id == OpId.Enforcer && (Vale == ValeAt.WithEnforcer || Vale == ValeAt.Taking)) return "the Enforcer is carrying Vale";
            if (op.Carrying.HasValue) return "already carrying";
            if (!IsOpId(o.Target)) return "carry names a downed operative";
            var other = OpOf(Sim.Op.Parse(o.Target));
            if (other == op) return "nobody carries themself";
            if (!other.Down) return $"{o.Target} is not down";
            if (other.CarriedBy.HasValue) return $"{o.Target} is already carried";
            if (!G.Within(op.Pos, other.Pos, actReach)) return $"stand beside the {o.Target}";
            Engage(op, o, R.CarryS); op.Gun.Order = ""; op.Gun.Target = "";
            return "";
        }

        /// <summary>The mission ends with Vale in the car: a downed operative not carried to it, nor lying beside it, is captured (§06).</summary>
        void Captures()
        {
            var car = S.At(S.GetawayCar);
            foreach (var op in Ops)
            {
                if (!op.Down) continue;
                if (G.Within(op.Pos, car, R.AtReachCm)) continue;
                string name = Sim.Op.Name(op.Id);
                Captured.Add(name); Chain.Add(name + "-captured");
                Note("captured", $"The {name} is left behind and captured.");
            }
        }

        // ---------------------------------------------------------------- the cards

        /// <summary>The going-loud card (motive-fights.md §2.6; Sky 2026-10-15 Q4: commit by holding weapons.costs.loudHold, no pause): would
        /// this order make the run loud, and what follows. Changes nothing.</summary>
        public LoudCard GoingLoud(Order o)
        {
            var c = new LoudCard { TrailPerHit = R.HitTrail, TrailPerDown = R.DownTrail, HoldTicks = R.LoudHoldTicks, ResponseOfficers = R.ResponseOfficers, AlertTo = Alert };
            if (Over) { c.Refused = "the mission is over"; return c; }
            bool loudNow = Alert >= 2 || Gunfire > 0 || FightBranch == "loud";
            if (o.Do == "panic") { c.Why = "panic"; if (!FightOn) c.Refused = "no fight: panic works only once a fight is on"; else c.Why = "fight-on"; return c; }
            if (o.Do != "draw" && o.Do != "fire") { c.Why = "no-change"; return c; }
            var op = OpOf(Sim.Op.Parse(o.Op)); var g = op.Gun;
            if (g.W == null) { c.Refused = $"{o.Op} carries no weapon (Arms)"; return c; }
            if (FightOn || FightBranch.Length > 0) { c.Why = "fight-on"; return c; }
            if (loudNow) { c.Why = "already-loud"; return c; }
            string reported; int delay; int civ = 0;
            if (o.Do == "draw")
            {
                var w = SecurityWatcherOf(op.Pos);
                if (w == null) { c.Why = "unseen-draw"; c.AlertTo = WitnessesWithPos(op.Pos, null).Exists(x => x.p.Civilian) ? Math.Max(Alert, R.DrawSeenPeople) : Alert; return c; }
                c.Why = "draw-seen"; reported = w == "security" ? "none" : "seen"; delay = w == "security" ? -1 : R.RespSeenS;
                c.AlertTo = Math.Max(Alert, R.DrawSeenSecurity);
            }
            else
            {
                string t = FireTarget(op, o.Target); var at = t.Length > 0 ? PosOf(t) : null;
                if (t.Length == 0 || !at.HasValue || IsOpId(t) || GunOf(t) == null) { c.Refused = $"no {o.Target} to fire at"; return c; }
                if (IsCivilianTarget(t) && !o.Force) { c.Refused = $"{t} is a civilian or a contact: a target only when forced (Ctrl)"; return c; }
                c.Why = "fire";
                Person? victim = IsCivilianTarget(t) ? PersonByRole(t) : t == "guard" ? GuardP : null;
                if (IsPersonTarget(t)) foreach (var p in WitnessesOf(at.Value, victim)) if (p.Civilian) civ++;
                var (d, _) = Reporter(t, op.Pos, civ > 0); delay = d;
                reported = IsCivilianTarget(t) ? "civilian" : t == "rivals" ? (d == R.RespRivalsUnseenS ? "rivals-unseen" : "seen") : d < 0 ? "none" : d == R.RespWitnessS && civ > 0 ? "witness" : d == R.RespUnseenS ? "unseen" : "seen";
                c.AlertTo = Math.Max(Alert, civ > 0 || IsCivilianTarget(t) ? R.ViolenceSeen : R.GunfireAlert);
                c.RivalsInS = RivalsDrawn || RivalsArrived ? 0 : Math.Max(0, Math.Min(RivalAt, Tick + T(R.RivalsDrawnS)) - Tick) / Hz;
                var pp = PatrolPose();
                c.PatrolBreaks = !PatrolDown && PatrolAwayUntil <= Tick && !PatrolChasing && pp.HasValue && G.Within(pp.Value.pos, op.Pos, R.PatrolBreakCm);
            }
            c.Loud = true; c.Civilians = civ; c.ReportedBy = reported;
            if (delay >= 0) { c.PoliceInS = delay; c.SecurityArrives = Math.Min(SecurityArrives, Tick + T(delay)); c.Seal = c.SecurityArrives + T(R.SealS); }
            else if (SecurityArrives != INF) { c.SecurityArrives = SecurityArrives; c.Seal = Lockdown; }
            if (Start.FinalFight && FightBranch.Length == 0)
            {
                c.FightAfter = "loud";
                c.Officers = R.LoudOfficers + (c.AlertTo >= 3 ? R.LoudAlert : 0) + (o.Do == "fire" && IsCivilianTarget(FireTarget(op, o.Target)) ? R.LoudCivilian : 0);
                c.GuardAllyLost = !GuardDown && !SawCell(GuardP) && !GuardEngaged;
                c.WarningLost = Inez.Turned && !Inez.Down && Inez.Has(R.HelpedAct);
            }
            return c;
        }

        /// <summary>The price chip for a move (motive-fights.md §1.2): walk and run times, and who would see the run along the route now.</summary>
        public MoveQuote QuoteMove(string opName, Pt to)
        {
            var q = new MoveQuote(); var op = OpOf(Sim.Op.Parse(opName));
            q.RunRefused = RunRefusal(op);
            var route = S.Grid.Free(to) ? S.Grid.Route(op.Pos, to) : null;
            if (route == null) { q.RunRefused = q.RunRefused.Length > 0 ? q.RunRefused : "no walk there"; return q; }
            var path = new Path(route);
            q.WalkS = (int)((path.Length + opSpeed * Hz - 1) / (opSpeed * Hz));
            q.RunS = (int)((path.Length + R.RunSpeed * Hz - 1) / (R.RunSpeed * Hz));
            q.RunFree = RunFree(op); q.AlertIfRun = Alert;
            if (q.RunFree) return q;
            var people = new HashSet<string>(StringComparer.Ordinal);
            for (long mm = 0; ; mm += 1000) // a sample every metre of the route, at the current tick
            {
                var p = path.At(Math.Min(mm, path.Length));
                foreach (var w in SecurityWatchers) if (!q.RunSeenBy.Contains(w) && WatcherSees(w, p)) q.RunSeenBy.Add(w);
                foreach (var (pp, wp) in OnTheStreet()) if (G.Within(wp, p, witnessRange)) people.Add(pp.Id);
                if (mm >= path.Length) break;
            }
            q.RunPeople = people.Count;
            if (q.RunSeenBy.Count > 0) q.AlertIfRun = Math.Max(Alert, R.RunAlert);
            return q;
        }

        /// <summary>Why this operative can't run now, or "" (motive-fights.md §1.1; Sky 2026-10-15 Q3: no running while carrying Vale).</summary>
        string RunRefusal(Op op)
        {
            if (op.Down) return $"the {Sim.Op.Name(op.Id)} is down";
            if (op.Id == OpId.Enforcer && (Vale == ValeAt.WithEnforcer || Vale == ValeAt.Taking)) return "carrying Vale, the Enforcer walks: a man being taken walks";
            if (op.Carrying.HasValue) return "carrying a downed operative, the operative walks";
            if (op.Id == OpId.Tech && LoopOn) return "the Tech is tethered to the cabinet while the loop holds";
            if (op.Busy != null) return $"{Sim.Op.Name(op.Id)} is busy until {op.BusyEnd}";
            return "";
        }
    }
}
