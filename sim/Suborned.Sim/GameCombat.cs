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
        public int TrailPerShooting;      // +T once per shooting, hit or miss (Motive addendum Q1)
        public bool DownedCaptured;       // the loss line: anyone downed is captured unless carried (Motive addendum Q3)
    }

    /// <summary>The price chip for a move (motive-fights.md §1.2): walk time, run time, who would see the run now, and whether it can run.</summary>
    public sealed class MoveQuote
    {
        public int WalkS, RunS; public string RunRefused = "";
        public readonly List<string> RunSeenBy = new List<string>(); // watchers whose cone the route crosses now
        public int RunPeople;             // people within witness range of the route now ("~": people move)
        public int AlertIfRun;            // Alert after the run if security sees it
        public bool RunFree;              // a fight is on or the run is loud: nobody counts a runner
        /// <summary>What the chip shows for "seen by" along the route now (rulebook sight.unknown; Sky 2026-10-16): known watchers' true
        /// answer, "?" for an unknown watcher that could see it. RunSeenBy above is the engine's truth for every watcher: the HUD shows Seen.</summary>
        public SeenQuote Seen = new SeenQuote();
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
        Incident? lastDrawIncident;

        Gun NewGun(string id, Weapon? w, bool trained, int health) => new Gun(id) { W = w, Trained = trained, Health = health, Max = health };

        static bool IsOpId(string id) => id == "scout" || id == "fixer" || id == "tech" || id == "enforcer";
        /// <summary>The simulated person a target id names, if they are a civilian (people.rules: not the city's security), cast or crowd, by
        /// role or id; null for an operative, the guard, the patrol, the rivals, an officer or a robot. One rule for every person (brief 013,
        /// Sky 2026-10-16: no named target list): a civilian is shot at only when forced (the Ctrl case).</summary>
        Person? CivilianOf(string id) => IsOpId(id) ? null : PersonById(id) is Person p && p.Civilian ? p : null;
        bool IsCivilian(string id) => CivilianOf(id) != null;
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
            if (UnitOf(id) is Unit u) return u.Gun;
            var h = HostileOf(id); if (h != null) return h.Gun;
            if (CivilianOf(id) is Person p && WalkerOf(p) != null) { var g = NewGun(p.Id, null, false, R.HealthCivilian); civGuns[p.Id] = g; extraCivGuns.Add(p.Id); return g; } // a person of the crowd, the first time a shot names them
            return null;
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
            if (UnitOf(id) is Unit u) return UnitPose(u)?.pos;
            var h = HostileOf(id); if (h != null) return h.Present && !h.Down ? h.Pos(Tick) : (Pt?)null;
            return CivilianOf(id) is Person p ? PosOfPerson(p) : null;
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
            if (UnitOf(id) is Unit u) return u.St == "walk" || u.St == "back";
            return HostileOf(id)?.Moving(Tick) ?? false;
        }

        /// <summary>The cover modifier on a shot at `t` standing at `at` from a shooter at `from` (Pulse §2.3, rulebook ai.cover): standing
        /// still beside a cover point that shields it, past point blank: weapons.mod.cover for full cover, ai.cover.modPartial for partial.</summary>
        int CoverMod(string t, Pt at, Pt from)
        {
            if (S.Cover.Count == 0 || Moving(t)) return 1000;
            return CoverRules.Modifier(S.Cover, at, from, Tick / Hz, R.CoverSnapCm, R.PointBlankCm, R.CoverQFull, R.CoverQPartial, R.ModCover, R.ModCoverPartial);
        }

        static long Dist(Pt a, Pt b) => G.Isqrt(G.D2(a, b));
        bool Ready(Gun g) => g.Drawn && g.HolsterAt < 0 && Tick >= g.DrawReady;

        // ---------------------------------------------------------------- the tick

        /// <summary>One tick of fighting (Pulse §1): the cell, then the guard as an ally, then security (by id: the response unit, the
        /// officers, an engaging guard, the patrol), then the rivals (by id). Every body decides from the start-of-tick state; the shots are
        /// applied together at the end (accumulators at the decision, then damage, then down).</summary>
        void Combat()
        {
            pending.Clear();
            foreach (var op in Ops) { var g = op.Gun; if (g.HolsterAt >= 0 && Tick >= g.HolsterAt) { g.Drawn = false; g.HolsterAt = -1; } }
            foreach (var op in Ops) CellShooter(op);
            if (GuardAlly && FightSprung != INF) AllyBody();
            var security = new List<string>();
            for (int k = 0; k < SecGuns.Length; k++) if (!IsFightHostile(SecGuns[k].Id)) security.Add(SecGuns[k].Id);
            foreach (var h in Hostiles) if (!h.Id.StartsWith("rival-", StringComparison.Ordinal)) security.Add(h.Id);
            if (!GuardAlly) security.Add("guard");
            security.Add("patrol");
            security.Sort(StringComparer.Ordinal);
            foreach (var id in security) CityBody(id);
            UnitsCombat(); // brief 013: the response's robots and police
            var rivals = new List<string> { "rivals" };
            foreach (var h in Hostiles) if (h.Id.StartsWith("rival-", StringComparison.Ordinal)) rivals.Add(h.Id);
            rivals.Sort(StringComparer.Ordinal);
            foreach (var id in rivals) CityBody(id);
            PatrolTracking = PatrolGun.Target.Length > 0 && !IsFightHostile("patrol");
            SecurityTracking = false; for (int k = 0; k < SecGuns.Length; k++) SecurityTracking |= SecGuns[k].Target.Length > 0;
            RivalsTracking = RivalsGun.Target.Length > 0;
            Resolve();
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
            if (op.Busy != null || op.Running || op.Carrying.HasValue || op.CarryBody.Length > 0 || !Ready(g)) return;
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
            foreach (var u in Units) if (!u.Legacy && !u.Patrol) yield return u.Id;
        }
        bool Combatant(string id)
        {
            var g = GunOf(id); if (g == null || g.Down || !PosOf(id).HasValue) return false;
            if (IsFightHostile(id)) { var h = HostileOf(id)!; return h.Present && h.Gun.St != "break" && h.Gun.St != "gone"; }
            if (id == "guard" && GuardAlly) return false;
            if (IsOpId(id) || IsCivilian(id)) return false;
            return IsOpId(g.Target) || g.Fired;
        }

        /// <summary>Take a target: a new one resets the accumulator to acc.start and waits aimNew, or react on a hostile's first sight.</summary>
        void Aim(Gun g, string t, bool react)
        {
            if (g.Target == t) return;
            g.Target = t; g.Acc = R.AccStart;
            g.NextShot = Math.Max(g.NextShot, Tick + (react && !g.Engaged ? T(R.ReactS) : R.AimNewTicks));
        }

        // ---------------------------------------------------------------- a shot

        sealed class PendingShot { public string By = "", T = ""; public Gun G = null!; public Pt At, From; public long D; public int Acc; public bool Hit; }
        readonly List<PendingShot> pending = new List<PendingShot>();

        /// <summary>A body fires: the accumulator moves now (it is the shooter's), the shot lands with the others at the end of the tick.</summary>
        void Shoot(string by, Gun g, Pt from, string t, Pt at, long d, bool moving, bool vale)
        {
            int acc = R.Accuracy(g.W!, d, moving, CoverMod(t, at, from), vale, !g.Trained);
            g.Acc += acc; bool hit = g.Acc >= 1000; if (hit) g.Acc -= 1000;
            g.NextShot = Tick + g.W!.Every; g.Fired = g.Engaged = true;
            pending.Add(new PendingShot { By = by, T = t, G = g, At = at, From = from, D = d, Acc = acc, Hit = hit });
        }

        void Resolve()
        {
            foreach (var s in pending)
            {
                if (Over) return;
                var tg = GunOf(s.T)!;
                tg.ShotBy = s.By; tg.ShotByTick = Tick;
                shotsThisTick.Add(s.From); LastShotTick = Tick; lastShooter = s.From; // the crowd hears it (Pulse §4)
                if (IsOpId(s.T)) CellFiredOn = true;
                Note(s.Hit ? "shot.hit" : "shot.miss", $"{s.By} fires on {s.T} at {s.D / 100} m ({s.Acc} ‰): {(s.Hit ? "hit" : "miss")}.");
                bool cell = IsOpId(s.By), untraced = cell && IsFightHostile(s.T) && !R.FightTraced;
                if (cell) CellGunfire(OpOf(Sim.Op.Parse(s.By)), s.T, s.At, untraced);
                else if (!IsFightHostile(s.By) && !(s.By == "guard" && GuardAlly)) RaiseAlert(R.GunfireAlert, "gunfire:" + s.By); // any gunfire (§05)
                if (Over) return;
                if (s.Hit && !tg.Down) Damage(s.T, tg, s.G.W!.Damage, s.By, cell, untraced);
            }
            pending.Clear();
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
            Person? victim = CivilianOf(t) ?? (t == "guard" ? GuardP : null);
            // one incident per target (rulebook alert: every shot of one fight with one target is one incident); later shots add new witnesses
            if (!fireIncident.TryGetValue(t, out var inc))
            {
                incidents.Add("fire:" + t);
                AddTrail(R.GunfireTrail, "gunfire:" + t); // a shooting is a case: once, at its first shot, hit or miss (Motive addendum Q1)
                inc = AddIncident(IsCivilian(t) ? "violence" : "gunfire", (IsCivilian(t) ? "violence:" : "gunfire:") + t, IsPersonTarget(t) ? at : op.Pos);
                fireIncident[t] = inc;
            }
            var seen = WitnessesWithPos(op.Pos, victim);
            if (IsPersonTarget(t)) foreach (var w in WitnessesWithPos(at, victim)) if (!seen.Exists(x => x.p == w.p)) seen.Add(w);
            foreach (var (p, wp) in seen)
            {
                if (inc.Witnesses.Exists(x => x.p == p)) continue;
                inc.Witnesses.Add((p, wp)); Remember(p, "saw-gunfire", name, true); // Pulse §4: a witness of a shot, before any startle
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
            if (CivilianOf(t) is Person shotAt) CivilianShotAt(shotAt, op);
            DrawRivals(); WakePatrol(op.Pos, op.Id);
            var (delay, text) = Reporter(t, op.Pos, civSees > 0);
            if (delay >= 0) CallSecurity(delay, text, IsPersonTarget(t) ? at : op.Pos);
        }

        /// <summary>Who reports the cell's gunfire, and so how soon the response unit comes (rulebook security.response).</summary>
        (int delay, string text) Reporter(string t, Pt shooter, bool civSees)
        {
            if (IsCivilian(t)) return (R.RespCivilianS, t == "waitress" ? "Inez is shot at the café. Witnesses call security." : t == "driver" ? "The official driver is shot at. Security rushes to the pickup." : t == "councillor" ? "Shots at the councillor are reported." : "A shooting in the street is reported.");
            if (t == "patrol") return (R.RespSeenS, "Gunfire at the patrol is reported.");
            if (t == "rivals") return CameraWatching("pickup", shooter) ? (R.RespSeenS, "The firefight with the rivals is reported.") : (R.RespRivalsUnseenS, "The firefight with the rivals is reported.");
            if (SecIndex(t) >= 0 || IsFightHostile(t) || UnitOf(t) != null) return (-1, "");
            if (civSees) return (R.RespWitnessS, InezWitnessedShooting ? "Inez reports the shooting." : "A witness reports the shooting.");
            bool guard = t == "guard" ? GuardWatching(shooter) : false;
            if (guard) return (R.RespSeenS, "The guard calls in the shooting.");
            if (PatrolWatching(shooter)) return (R.RespSeenS, "The patrol reports gunfire.");
            if (CameraWatching("entrance", shooter)) return (R.RespSeenS, "The entrance camera reports gunfire.");
            foreach (var c in S.Cameras) if (c.World && CameraWatching(c.Id, shooter)) return (R.RespSeenS, "A street camera reports gunfire.");
            return (R.RespUnseenS, "Shots are heard in the district.");
        }

        /// <summary>A civilian or a contact shot at (§05: violence → Alert 3 at once; rulebook traceable violence): the guard, away from his
        /// door, stops to search for the shooter.</summary>
        void CivilianShotAt(Person p, Op op)
        {
            string who = Who(p);
            RaiseAlert(R.ViolenceSeen, $"violence-seen:{who}");
            if (incidents.Add("seen:" + who)) { AddTrail(R.WitnessTrail, $"witnessed:violence:{who}"); MoveStanding(-R.StandingStep, $"violence-seen:{who}"); }
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
            if (UnitOf(t) is Unit hu) { Note(down ? "response.down" : "response.hit", down ? $"{t} is down." : $"{t} is hit."); if (down) hu.St = "gone"; return; }
            if (CivilianOf(t) is Person cp) { CivilianHit(cp, down); return; }
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
                if (down)
                {
                    if (by == "enforcer") DownedByEnforcer++; else if (by == "guard") DownedByGuard++; else DownedByCell++;
                    Note("fight.down", $"{who} drops {t}."); ReleaseCover(tg); Stop(h);
                    if (IsRival(t)) RivalDown();
                }
                else Note("fight.hit", $"{who} hits {t}.");
            }
        }

        void OpHit(Op op, bool down, string by)
        {
            string name = Sim.Op.Name(op.Id);
            if (FightBranch.Length > 0) FightHits++;
            Note(name + ".hit." + (IsFightHostile(by) ? "fight" : by), $"{by} hits the {name}. {op.Gun.Health} health left.");
            if (!down) return;
            if (op.Id == OpId.Enforcer)
            {
                if (FightBranch == "safe" && Vale == ValeAt.WithEnforcer) Fail("rivals-reach-vale", "The Enforcer is down and the rival crew takes Vale off him."); // the rivals' goal (Motive addendum Q7)
                else Fail("enforcer-down", "The Enforcer is down.");
                return;
            }
            if (op.Busy != null) { Cash += op.BusyPaid; op.Busy = null; op.BusyQuote = null; op.BusyPaid = 0; } // an action cut short: the price comes back, as a refusal's does
            op.Walk = null; op.WalkMm = 0; op.Running = false; op.Gun.Order = ""; op.Gun.Target = "";
            if (op.Carrying.HasValue) { OpOf(op.Carrying.Value).CarriedBy = null; op.Carrying = null; }
            if (op.CarryBody.Length > 0) PutDown(op, name + " down");
            Note(name + ".down", $"The {name} is down. Carry them to the car, or they are captured.");
        }

        void GuardHit(bool down, string by)
        {
            if (down)
            {
                GuardDown = true; GuardP.Down = true; GuardEngaged = false; // his next check-in mark misses (brief 013: response.checkIn)
                bool rivals = IsFightHostile(by); // downed by the rivals in the safe fight: the post is still empty, but no act of the cell (Motive addendum Q6)
                Chain.Add(rivals ? "guard-down-by-rivals" : "guard-shot"); Note("guard.down", rivals ? "The rivals drop the guard. His missed check-in will still be noticed." : "The guard falls. His missed check-in will draw more attention.");
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
            if (WalkerOf(p) is Walker cw) { cw.St = ""; cw.Run = null; cw.Pace = 0; }
            Chain.Add(Who(p) + "-shot");
            Note("civilian.down", $"{p.Name} ({Who(p)}) is down.");
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
                foreach (var w in new List<string>(SecurityWatchers))
                    if (!drawSeen.Contains($"{w}/{name}/{g.DrawTick}") && WatcherSees(w, op.Pos)) { drawSeen.Add($"{w}/{name}/{g.DrawTick}"); SecuritySawDraw(w, op); if (Over) return; }
                string pk = $"people/{name}/{g.DrawTick}";
                if (drawSeen.Contains(pk)) continue;
                var ws = WitnessesWithPos(op.Pos, null).FindAll(x => x.p.Civilian);
                if (ws.Count == 0) continue;
                drawSeen.Add(pk);
                // drawings at one place and time are one incident (Motive addendum Q1): within weapons.costs.drawWindow and witness range of the last
                var same = lastDrawIncident != null && Tick - lastDrawIncident.Tick < T(R.DrawWindowS) && G.Within(lastDrawIncident.At, op.Pos, witnessRange);
                if (same) { foreach (var w in ws) if (!lastDrawIncident!.Witnesses.Exists(x => x.p == w.p)) lastDrawIncident.Witnesses.Add(w); }
                else
                {
                    lastDrawIncident = AddIncident("weapon-drawn", "weapon-drawn:" + name, op.Pos, ws);
                    AddTrail(R.WitnessTrail, $"witnessed:weapon-drawn:{name}"); MoveStanding(R.Traceable["weapon-drawn"] * R.StandingStep, "witnessed:weapon-drawn");
                }
                RaiseAlert(R.DrawSeenPeople, $"drawn-seen:people:{name}");
                foreach (var (p, _) in ws) Remember(p, "saw-weapon-drawn", name, true);
                Note("witness.weapon-drawn", $"{ws.Count} see the {name}'s weapon drawn.");
            }
        }
        static readonly string[] SecurityWatchersFixed = { "guard", "patrol", "camera:entrance", "camera:pickup", "security" };
        /// <summary>The city's eyes that call in what they see: the fixed five, then the response's robots and police (brief 013) by id.</summary>
        IEnumerable<string> SecurityWatchers { get { foreach (var w in SecurityWatchersFixed) yield return w; foreach (var c in S.Cameras) if (c.World) yield return "camera:" + c.Id; foreach (var u in Units) if (!u.Legacy && !u.Patrol && u.OnStreet(Tick) && !u.Gun.Down) yield return u.Id; } }
        bool WatcherSees(string w, Pt p) => w switch
        {
            "guard" => !GuardAlly && GuardWatching(p),
            "patrol" => PatrolWatching(p),
            "camera:entrance" => CameraWatching("entrance", p),
            "camera:pickup" => CameraWatching("pickup", p),
            "security" => SecurityWatching(p),
            _ => UnitOf(w) is Unit u ? UnitSees(u, p) : w.StartsWith("camera:", StringComparison.Ordinal) && CameraWatching(w.Substring(7), p),
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
                    GuardEngaged = true; Remember(GuardP, "saw-weapon-drawn", name, true);
                    CallSecurity(R.RespSeenS, "The guard calls in a drawn weapon.");
                    return;
                case "patrol":
                    if (!PatrolChasing && !PatrolDown && PatrolAwayUntil <= Tick) { chasePos = PatrolPose()!.Value.pos; PatrolChasing = true; chaseOp = op.Id; patrolOrigin = op.Pos; patrolReturning = false; PatrolGun.NoFoeSince = -1; Note("patrol.breaks", "The patrol breaks its route and turns on the drawn weapon."); }
                    CallSecurity(R.RespSeenS, "The patrol reports a drawn weapon.");
                    return;
                case "camera:entrance": case "camera:pickup":
                    CallSecurity(R.RespSeenS, "The watchers on the camera report a drawn weapon.");
                    return;
                default:
                    if (w.StartsWith("camera:", StringComparison.Ordinal)) CallSecurity(R.RespSeenS, "The watchers on the camera report a drawn weapon."); // a city camera (the mast)
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
            if (g.W == null) return No("no-weapon", $"{o.Op} carries no weapon (Arms)");
            if (g.Drawn && g.HolsterAt < 0) return No("already-drawn", "the weapon is already drawn");
            StartDraw(op);
            Note("order.draw", $"{Sim.Op.Name(op.Id)} draws ({R.DrawTicks / Hz}.{R.DrawTicks % Hz} s).");
            return "";
        }
        string Holster(Op op, Order o)
        {
            var g = op.Gun;
            if (!g.Drawn || g.HolsterAt >= 0) return No("not-drawn", "no weapon drawn");
            g.HolsterAt = Tick + R.HolsterTicks; g.Order = ""; g.Force = false; g.Target = "";
            Note("order.holster", $"{Sim.Op.Name(op.Id)} holsters.");
            return "";
        }
        string Cease(Op op, Order o)
        {
            var g = op.Gun;
            if (!g.Drawn) return No("not-drawn", "no weapon drawn");
            g.Order = ""; g.Force = false; g.Target = ""; g.LostAt = -1;
            Note("order.cease", $"{Sim.Op.Name(op.Id)} ceases fire.");
            return "";
        }
        string Stance(Op op, Order o)
        {
            if (op.Gun.W == null) return No("no-weapon", $"{o.Op} carries no weapon (Arms)");
            if (o.Stance != "hold" && o.Stance != "return" && o.Stance != "free") return No("stance-unknown", $"stance '{o.Stance}' is unknown (hold, return, free)");
            op.Gun.Stance = o.Stance;
            Note("order.stance", $"{Sim.Op.Name(op.Id)}: {o.Stance}.");
            return "";
        }
        string Panic(Order o)
        {
            if (o.Op != "cell") return No("panic-not-cell", "panic is the cell's: op \"cell\"");
            if (!FightOn) return No("panic-no-fight", "no fight: panic works only once a fight is on");
            int n = 0;
            foreach (var op in Ops)
            {
                if (op.Down || op.Inside || op.Gun.W == null) continue;
                if (!op.Gun.Drawn || op.Gun.HolsterAt >= 0) StartDraw(op);
                op.Gun.Stance = "free"; n++;
            }
            if (n == 0) return No("panic-nobody", "nobody armed can fire");
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
            if (g.W == null) return No("no-weapon", $"{o.Op} carries no weapon (Arms)");
            if (op.Running) return No("fire-running", "a running operative doesn't fire: walk or stop first");
            if (op.Carrying.HasValue || op.CarryBody.Length > 0) return No("fire-carrying", "carrying, the operative doesn't fire");
            string t = FireTarget(op, o.Target);
            if (t.Length == 0) return No("no-target", o.Target == "hostiles" ? "no hostile in range with a clear line" : $"no {o.Target} to fire at");
            if (IsOpId(t)) return No("fire-on-cell", "the cell doesn't fire on its own");
            var tg = GunOf(t); if (tg == null) return No("target-unknown", $"target '{o.Target}' is unknown (unknown = reject)");
            if (IsCivilian(t) && !o.Force) return No("civilian-target", $"{t} is a civilian or a contact: a target only when forced (Ctrl)");
            var at = PosOf(t);
            if (!at.HasValue || tg.Down) return No("no-target", $"no {o.Target} to fire at");
            if (!G.Within(op.Pos, at.Value, g.W.MaxCm)) return No("out-of-range", $"out of range ({g.W.MaxCm / 100} m)", m: g.W.MaxCm / 100);
            if (!Sees(op.Pos, at.Value)) return No("no-clear-line", "no clear line");
            if (!g.Drawn || g.HolsterAt >= 0) StartDraw(op);
            g.Order = t; g.Force = o.Force; g.LostAt = -1;
            Actions.Add("fire:" + o.Target);
            Note("fire." + o.Target, $"{Sim.Op.Name(op.Id)} fires on {t} until down.");
            return "";
        }

        string Carry(Op op, Order o)
        {
            if (op.Id == OpId.Enforcer && (Vale == ValeAt.WithEnforcer || Vale == ValeAt.Taking)) return No("carry-vale", "the Enforcer is carrying Vale");
            if (op.Carrying.HasValue || op.CarryBody.Length > 0) return No("already-carrying", "already carrying");
            if (!IsOpId(o.Target)) return PersonById(o.Target) != null ? CarryBody(op, o) : No("carry-names-downed", "carry names a downed operative or a body");
            var other = OpOf(Sim.Op.Parse(o.Target));
            if (other == op) return No("carry-self", "nobody carries themself");
            if (!other.Down) return No("not-down", $"{o.Target} is not down");
            if (other.CarriedBy.HasValue) return No("already-carried", $"{o.Target} is already carried");
            if (!G.Within(op.Pos, other.Pos, actReach)) return No("stand-beside-op", $"stand beside the {o.Target}", m: actReach / 100);
            Engage(op, o, R.CarryS); op.Gun.Order = ""; op.Gun.Target = "";
            return "";
        }

        /// <summary>The mission ends with Vale in the car: a downed operative not carried to it, nor lying beside it, is captured (§06).</summary>
        void Captures()
        {
            foreach (var name in WouldBeCaptured())
            {
                Captured.Add(name); Chain.Add(name + "-captured");
                Note("captured", $"The {name} is left behind and captured.");
            }
        }

        /// <summary>The operatives an extract now would leave behind: every downed one not within the getaway car's reach (cell.atReach, the
        /// same reach as Extract), carried or not; a carried one stands where its carrier stands. For the HUD's warning (Motive's H11).</summary>
        public List<string> WouldBeCaptured()
        {
            var car = S.At(S.GetawayCar); var l = new List<string>();
            foreach (var op in Ops) if (op.Down && !G.Within(op.Pos, car, R.AtReachCm)) l.Add(Sim.Op.Name(op.Id));
            return l;
        }

        // ---------------------------------------------------------------- the cards

        /// <summary>The going-loud card (motive-fights.md §2.6; Sky 2026-10-15 Q4: commit by holding weapons.costs.loudHold, no pause): would
        /// this order make the run loud, and what follows. Changes nothing.</summary>
        public LoudCard GoingLoud(Order o)
        {
            var c = new LoudCard { TrailPerHit = R.HitTrail, TrailPerDown = R.DownTrail, TrailPerShooting = R.GunfireTrail, HoldTicks = R.LoudHoldTicks, ResponseOfficers = R.ResponseOfficers, AlertTo = Alert };
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
                if (IsCivilian(t) && !o.Force) { c.Refused = $"{t} is a civilian or a contact: a target only when forced (Ctrl)"; return c; }
                c.Why = "fire";
                Person? victim = CivilianOf(t) ?? (t == "guard" ? GuardP : null);
                if (IsPersonTarget(t)) foreach (var p in WitnessesOf(at.Value, victim)) if (p.Civilian) civ++;
                var (d, _) = Reporter(t, op.Pos, civ > 0); delay = d;
                reported = IsCivilian(t) ? "civilian" : t == "rivals" ? (d == R.RespRivalsUnseenS ? "rivals-unseen" : "seen") : d < 0 ? "none" : d == R.RespWitnessS && civ > 0 ? "witness" : d == R.RespUnseenS ? "unseen" : "seen";
                c.AlertTo = Math.Max(Alert, civ > 0 || IsCivilian(t) ? R.ViolenceSeen : R.GunfireAlert);
                c.RivalsInS = RivalsDrawn || RivalsArrived ? 0 : Math.Max(0, Math.Min(RivalAt, Tick + T(R.RivalsDrawnS)) - Tick) / Hz;
                var pp = PatrolPose();
                c.PatrolBreaks = !PatrolDown && PatrolAwayUntil <= Tick && !PatrolChasing && pp.HasValue && G.Within(pp.Value.pos, op.Pos, R.PatrolBreakCm);
            }
            c.Loud = true; c.Civilians = civ; c.ReportedBy = reported; c.DownedCaptured = true;
            if (delay >= 0) { int arr = GunfireArrival(delay); c.PoliceInS = (arr - Tick) / Hz; c.SecurityArrives = Math.Min(SecurityArrives, arr); c.Seal = c.SecurityArrives + T(SealS); }
            else if (SecurityArrives != INF) { c.SecurityArrives = SecurityArrives; c.Seal = Lockdown; }
            if (Start.FinalFight && FightBranch.Length == 0)
            {
                c.FightAfter = "loud";
                c.Officers = R.LoudOfficers + (c.AlertTo >= 3 ? R.LoudAlert : 0) + (o.Do == "fire" && IsCivilian(FireTarget(op, o.Target)) ? R.LoudCivilian : 0);
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
            { var at = new List<Pt>(); for (long mm = 0; ; mm += 1000) { at.Add(path.At(Math.Min(mm, path.Length))); if (mm >= path.Length) break; } q.Seen = SeenAt(at); } // the same samples as below
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
            if (op.Down) return No("agent-down", $"the {Sim.Op.Name(op.Id)} is down");
            if (op.Id == OpId.Enforcer && (Vale == ValeAt.WithEnforcer || Vale == ValeAt.Taking)) return No("run-carrying-vale", "carrying Vale, the Enforcer walks: a man being taken walks");
            if (op.Carrying.HasValue || op.CarryBody.Length > 0) return No("run-carrying-downed", "carrying, the operative walks");
            if (op.Id == OpId.Tech && LoopOn) return No("run-tethered", "the Tech is tethered to the cabinet while the loop holds");
            if (op.Busy != null) return No("agent-busy", $"{Sim.Op.Name(op.Id)} is busy until {op.BusyEnd}", tick: op.BusyEnd);
            return "";
        }
    }
}
