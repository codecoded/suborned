// The final fight (GDD §07 [S]: always; 3–5 hostiles; rulebook `fight`). It starts when the Enforcer has Vale and is fought at the
// getaway car: the car can't leave while a hostile holds it. Loud (any shot or Alert ≥ 2): police officers run in from the response
// unit's entry; the seal clock runs; no allies. Safe: the rival crew ambushes the extraction, fewer for a turned driver and an earlier
// false tip; a helped Inez's warning holds their first shots back; a guard who never saw the cell joins in. Every hostile shoots at
// its own weapon's cadence (GameCombat.cs; Sky 2026-10-15, briefs/011/motive-fights.md: the volley is retired).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed class Hostile
    {
        public string Id = ""; public Pt At, Home; public Path? Run; public int RunStart, Speed; public bool Present; public Gun Gun = null!; // Home: its stand (police)
        public bool Down => Gun.Down; public int Health => Gun.Health;
        public Pt Pos(int tick) => Run == null ? At : Run.At((long)Math.Max(0, tick - RunStart) * Speed);
        public bool Moving(int tick) => Run != null && (long)Math.Max(0, tick - RunStart) * Speed < Run.Length;
    }

    public sealed partial class Game
    {
        public string FightBranch = "";                    // "", "loud", "safe"
        public int FightStart = INF, FightSprung = INF, FightOverTick = INF;
        public bool FightWarned, GuardAlly;
        public int FightHits, DownedByEnforcer, DownedByGuard, DownedByCell, FightShots;
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
            FightJoinsFinal(); // fight.public: gunfire still open is the final fight
            if (loud)
            {
                int n = R.LoudOfficers;
                if (Alert >= 3) { n += R.LoudAlert; FightWhy.Add("alert-3"); }
                bool civShot = false; foreach (var kv in civGuns) if (kv.Value.Down) civShot = true; // a civilian shot (a takedown is no shot)
                if (civShot) { n += R.LoudCivilian; FightWhy.Add("civilian-shot"); }
                var entry = S.Stand(S.FightEntry);
                var stands = S.FightPolice;
                for (int i = 0; i < n && i < stands.Length; i++)
                {
                    var route = S.Grid.Route(entry, S.Stand(stands[i])) ?? new[] { entry, S.Stand(stands[i]) };
                    var gun = NewGun($"officer-{i + 1}", R.Pistol, true, R.HealthOfficer); gun.Engaged = true; gun.St = "advance"; // summoned: no alert delay (Pulse §3.2)
                    Hostiles.Add(new Hostile { Id = gun.Id, At = S.Stand(stands[i]), Home = S.Stand(stands[i]), Run = new Path(route), RunStart = Tick, Speed = secSpeed, Present = true, Gun = gun });
                }
                Lockdown = Math.Min(Lockdown, Tick + T(SealS));
                Note("fight.loud", $"The final fight: {Hostiles.Count} police officers come in from the north street. The seal clock is running.");
            }
            else
            {
                int n = R.SafeHostiles;
                if (Driver.Turned && !Driver.Down) { n -= R.SafeMinus; FightWhy.Add("driver-blocks-rival-car"); }
                if (misledOnce) { n -= R.SafeMinus; FightWhy.Add("earlier-false-tip"); }
                var spots = S.FightAmbush;
                for (int i = 0; i < n && i < spots.Length; i++)
                {
                    var gun = NewGun($"rival-{i + 1}", R.Pistol, false, R.HealthRival); gun.St = "routine"; // lurking (Pulse §3.1)
                    Hostiles.Add(new Hostile { Id = gun.Id, At = S.Stand(spots[i]), Home = S.Stand(spots[i]), Speed = rivSpeed, Gun = gun });
                }
                FightWarned = Inez.Turned && !Inez.Down && Inez.Has(R.HelpedAct);
                if (FightWarned) FightWhy.Add("waitress-warns");
                GuardAlly = !GuardDown && GuardGun.Health == GuardGun.Max && !GuardEngaged && !SawCell(GuardP);
                if (GuardAlly) FightWhy.Add("guard-fights-them");
                rivalsAbsorbed = true; // the crew is the ambush now
                Note("fight.safe", $"The final fight: the rival crew ({Hostiles.Count}) lies in wait for the extraction.");
            }
        }

        /// <summary>The guard "never saw you" (§07): he holds no memory the cell left — no face, no act seen.</summary>
        bool SawCell(Person p) => p.Memory.Count > 0;


        void FightStep()
        {
            if (FightBranch.Length == 0 || FightOverTick != INF) return;
            JoinResponse();
            var e = Enforcer; var car = S.At(S.GetawayCar);
            if (FightBranch == "safe" && FightSprung == INF && Vale == ValeAt.WithEnforcer && !e.Inside && G.Within(e.Pos, car, R.SafeAmbushCm))
            {
                FightSprung = Tick;
                // lying in wait, the crew fires aimNew after springing, plus the warning if Inez gave it (motive-fights.md §2.2 timing)
                foreach (var h in Hostiles) { h.Present = true; h.Gun.Engaged = true; h.Gun.St = "alerted"; h.Gun.Until = h.Gun.NextShot = Tick + R.AimNewTicks + (FightWarned ? T(R.SafeWarnS) : 0); }
                Note("fight.ambush", FightWarned ? "Inez's warning comes in time: the ambush is seen before it fires." : "The rival crew springs its ambush at the getaway car.");
                if (GuardAlly)
                {
                    var from = GuardPose()?.pos ?? S.Stand(S.GuardPost);
                    var stop = AllyStop(); // his post, or the cover beside it (Pulse §3.4)
                    var route = S.Grid.Route(S.Grid.NearestFree(from) ?? from, S.Grid.NearestFree(stop) ?? stop);
                    guardAllyPath = new Path(route ?? new[] { from, stop }); guardAllyStart = Tick;
                    GuardHeld = true; heldPhase = "fighting the rivals"; heldPos = from; GuardAwayUntil = INF;
                    Note("fight.guard", "The guard hears the shooting and runs in against the rivals.");
                }
            }
            bool anyUp = false; foreach (var h in Hostiles) if (!h.Down && h.Gun.St != "break" && h.Gun.St != "gone") anyUp = true;
            if (!anyUp && Hostiles.Count > 0) { FightOverTick = Tick; Note("fight.over", "The last of them is down or gone. The car can go."); return; }
            // the guard runs in against the rivals (safe); his shots are in Combat()
            if (GuardAlly && guardAllyPath != null && !GuardDown)
            {
                long mm = (long)(Tick - guardAllyStart) * secSpeed;
                heldPos = guardAllyPath.At(mm); heldFacing = guardAllyPath.Heading(mm);
            }
        }
        bool GuardAllyMoving => GuardAlly && guardAllyPath != null && (long)(Tick - guardAllyStart) * secSpeed < guardAllyPath.Length;

        Hostile? NearestHostile(Pt from, int reach)
        {
            Hostile? best = null; long bd = long.MaxValue;
            foreach (var h in Hostiles)
            {
                if (!h.Present || h.Down || h.Gun.St == "break" || h.Gun.St == "gone") continue;
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
            foreach (var h in Hostiles) if (!h.Down && h.Gun.St != "break" && h.Gun.St != "gone" && (h.Present || FightBranch == "safe") && G.Within(h.Pos(Tick), car, hold)) return true; // a broken rival no longer holds it (Pulse §2.1)
            return false;
        }

        void MisledOnce() { misledOnce = true; }
        bool RivalsAbsorbed => rivalsAbsorbed;
    }
}
