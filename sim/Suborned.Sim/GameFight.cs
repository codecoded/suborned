// The final fight (GDD §07 [S]: always; 30–60 s; 3–5 hostiles; rulebook `fight`). It starts when the Enforcer has Vale and is
// fought at the getaway car: the car can't leave while a hostile holds it. Loud (any shot or Alert ≥ 2): police officers run in
// from the response unit's entry; the seal clock runs; no allies. Safe: the rival crew ambushes the extraction, fewer for a turned
// driver and an earlier false tip; a helped Inez's warning holds their first volley back; a guard who never saw the cell joins in.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed class Hostile
    {
        public string Id = ""; public Pt At; public Path? Run; public int RunStart, Speed, Health; public bool Down, Present;
        public Pt Pos(int tick) => Run == null ? At : Run.At((long)Math.Max(0, tick - RunStart) * Speed);
    }

    public sealed partial class Game
    {
        public string FightBranch = "";                    // "", "loud", "safe"
        public int FightStart = INF, FightSprung = INF, NextVolley = INF, FightOverTick = INF;
        public bool FightWarned, GuardAlly, GuardAllyTracking;
        public int GuardAllyNextShot, FightHits, DownedByEnforcer, DownedByGuard, FightShots;
        public readonly List<Hostile> Hostiles = new List<Hostile>();
        public readonly List<string> FightWhy = new List<string>();
        Path? guardAllyPath; int guardAllyStart;
        bool rivalsAbsorbed, misledOnce;


        /// <summary>The Enforcer has Vale: the final fight begins (§07: always).</summary>
        void StartFight()
        {
            if (!Start.FinalFight || FightBranch.Length > 0) return;
            bool loud = Gunfire > 0 || Alert >= 2;
            FightBranch = loud ? "loud" : "safe"; FightStart = Tick;
            int hits = R.Hits;
            if (loud)
            {
                int n = R.LoudOfficers;
                if (Alert >= 3) { n += R.LoudAlert; FightWhy.Add("alert-3"); }
                if (Inez.Down || Driver.Down) { n += R.LoudCivilian; FightWhy.Add("civilian-shot"); }
                var entry = S.Stand(S.FightEntry);
                var stands = S.FightPolice;
                for (int i = 0; i < n && i < stands.Length; i++)
                {
                    var route = S.Grid.Route(entry, S.Stand(stands[i])) ?? new[] { entry, S.Stand(stands[i]) };
                    Hostiles.Add(new Hostile { Id = $"officer-{i + 1}", At = S.Stand(stands[i]), Run = new Path(route), RunStart = Tick, Speed = secSpeed, Health = hits, Present = true });
                }
                Lockdown = Math.Min(Lockdown, Tick + T(R.SealS));
                Note("fight.loud", $"The final fight: {Hostiles.Count} police officers come in from the north street. The seal clock is running.");
            }
            else
            {
                int n = R.SafeHostiles;
                if (Driver.Turned && !Driver.Down) { n -= R.SafeMinus; FightWhy.Add("driver-blocks-rival-car"); }
                if (misledOnce) { n -= R.SafeMinus; FightWhy.Add("earlier-false-tip"); }
                var spots = S.FightAmbush;
                for (int i = 0; i < n && i < spots.Length; i++) Hostiles.Add(new Hostile { Id = $"rival-{i + 1}", At = S.Stand(spots[i]), Health = hits });
                FightWarned = Inez.Turned && !Inez.Down && Inez.Has(R.HelpedAct);
                if (FightWarned) FightWhy.Add("waitress-warns");
                GuardAlly = !GuardDown && GuardHealth == hits && !SawCell(GuardP);
                if (GuardAlly) FightWhy.Add("guard-fights-them");
                rivalsAbsorbed = true; // the crew is the ambush now
                Note("fight.safe", $"The final fight: the rival crew ({Hostiles.Count}) lies in wait for the extraction.");
            }
        }

        /// <summary>The guard "never saw you" (§07): he holds no memory the cell left — no face, no act seen.</summary>
        bool SawCell(Person p) => p.Memory.Count > 0;

        bool HostileSees(Hostile h, Pt p) => h.Present && !h.Down && G.Within(h.Pos(Tick), p, R.RivReachCm) && Sees(h.Pos(Tick), p);

        void FightStep()
        {
            if (FightBranch.Length == 0 || FightOverTick != INF) return;
            var e = Enforcer; var car = S.At(S.GetawayCar);
            if (FightBranch == "safe" && FightSprung == INF && Vale == ValeAt.WithEnforcer && !e.Inside && G.Within(e.Pos, car, R.SafeAmbushCm))
            {
                FightSprung = Tick;
                foreach (var h in Hostiles) h.Present = true;
                NextVolley = Tick + T(R.AimS) + (FightWarned ? T(R.SafeWarnS) : 0);
                Note("fight.ambush", FightWarned ? "Inez's warning comes in time: the ambush is seen before it fires." : "The rival crew springs its ambush at the getaway car.");
                if (GuardAlly)
                {
                    var from = GuardPose()?.pos ?? S.Stand(S.GuardPost);
                    var route = S.Grid.Route(S.Grid.NearestFree(from) ?? from, S.Stand(S.FightGuard));
                    guardAllyPath = new Path(route ?? new[] { from, S.Stand(S.FightGuard) }); guardAllyStart = Tick;
                    GuardHeld = true; heldPhase = "fighting the rivals"; heldPos = from; GuardAwayUntil = INF;
                    Note("fight.guard", "The guard hears the shooting and runs in against the rivals.");
                }
            }
            bool anyUp = false; foreach (var h in Hostiles) if (!h.Down) anyUp = true;
            if (!anyUp && Hostiles.Count > 0) { FightOverTick = Tick; Note("fight.over", "The last of them is down. The car can go."); return; }
            // the hostiles' volley
            if (!e.Inside)
            {
                bool seen = false; foreach (var h in Hostiles) if (HostileSees(h, e.Pos)) { seen = true; break; }
                if (seen && NextVolley == INF) NextVolley = Tick + T(R.AimS);
                if (seen && Tick >= NextVolley)
                {
                    NextVolley = Tick + T(R.VolleyS); FightHits++;
                    HitEnforcer("fight", FightBranch == "loud" ? "The officers' volley hits the Enforcer." : "The rivals' volley hits the Enforcer.");
                    if (Over) return;
                }
            }
            // the guard joins the fight (safe)
            if (GuardAlly && guardAllyPath != null && !GuardDown)
            {
                heldPos = guardAllyPath.At((long)(Tick - guardAllyStart) * secSpeed); heldFacing = guardAllyPath.Heading((long)(Tick - guardAllyStart) * secSpeed);
                Hostile? target = NearestHostile(heldPos, R.FireReachCm);
                if (target == null) GuardAllyTracking = false;
                else
                {
                    if (!GuardAllyTracking) { GuardAllyTracking = true; GuardAllyNextShot = Tick + T(R.AimS); }
                    if (Tick >= GuardAllyNextShot)
                    {
                        GuardAllyNextShot = Tick + T(R.GuardFireS);
                        if (--target.Health <= 0) { target.Down = true; DownedByGuard++; Note("fight.guard-hit", $"The guard drops {target.Id}."); }
                        else Note("fight.guard-hit", $"The guard hits {target.Id}.");
                    }
                }
            }
        }

        Hostile? NearestHostile(Pt from, int reach)
        {
            Hostile? best = null; long bd = long.MaxValue;
            foreach (var h in Hostiles)
            {
                if (!h.Present || h.Down) continue;
                var p = h.Pos(Tick); long d = G.D2(from, p);
                if (d <= (long)reach * reach && d < bd && Sees(from, p)) { bd = d; best = h; }
            }
            return best;
        }

        /// <summary>A hostile still holds the getaway car (rulebook fight.hold): the car can't leave.</summary>
        public bool CarHeld()
        {
            if (FightBranch.Length == 0) return false;
            var car = S.At(S.GetawayCar); int hold = R.HoldCm;
            foreach (var h in Hostiles) if (!h.Down && (h.Present || FightBranch == "safe") && G.Within(h.Pos(Tick), car, hold)) return true;
            return false;
        }

        void FireHostile(Hostile h)
        {
            if (R.FightTraced) RegisterGunfire("hostiles");
            else { FightShots++; EnforcerNextShot = Tick + T(R.ShotEveryS); WakePatrol(); } // Sky 2026-10-15 (rulebook fight.traced off): the final fight's shots are the consequence, not a new act: no Trail, no Alert, not "a shot fired" for the debrief
            if (--h.Health <= 0) { h.Down = true; DownedByEnforcer++; Note("fight.down", $"The Enforcer drops {h.Id}."); }
            else Note("fight.hit", $"The Enforcer hits {h.Id}.");
        }

        void MisledOnce() { misledOnce = true; }
        bool RivalsAbsorbed => rivalsAbsorbed;
    }
}
