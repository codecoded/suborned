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
        public string Id = ""; public Pt At, Home; public Path? Run; public int RunStart, Speed; public bool Present; public Gun Gun = null!; // Home: its stand (police; a blocker's lane point)
        public int Wave; // brief 029: the range's spawn it came with (a crew breaks by its own spawn)
        public bool Blocker; // brief 018 §1: it went for the road, and holds its point in the exit lane as police hold a stand
        public Pt? BreakTo; // brief 059: where a broken rival runs to (his car where it stood when the crew broke); not hashed (his walk is)
        /// <summary>Engine 0.34.0 (Motive M067-SQ1, car.held.&lt;how&gt;): why its current walk started: "post" (to its stand, spot or post: the
        /// spawn walk too), "advance" (on the car or an operative), "answer" (to fire: a step out of a line, to cover, a close), "press" (for the
        /// held car, K3 a), "blocker" (to the road), "break". Read only by the car.held log; not hashed (no rule reads it).</summary>
        public string Why = "post";
        /// <summary>Engine 0.34.1 (Aster's ruling within M067-SQ2: an advance closes on its operative): walking an advance on an operative
        /// (AdvanceOnOp). While he walks it (St advance) he takes no kite step out of a line, as on K3's walk to the car: the step would turn
        /// the advance back to cover that does not close. A foe in reach still stops him (advance → engage). Every new walk or stop clears it.
        /// Hashed only when set.</summary>
        public bool Closing;
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
            RoundsEnd("final-fight"); // brief 022
            if (loud)
            {
                int n = R.LoudOfficers;
                if (Alert >= 3) { n += R.LoudAlert; FightWhy.Add("alert-3"); }
                bool civShot = false; foreach (var kv in civGuns) if (kv.Value.Down) civShot = true; // a civilian shot (a takedown is no shot)
                if (civShot) { n += R.LoudCivilian; FightWhy.Add("civilian-shot"); }
                var entry = LoudEntryNow(out var entryName); // brief 059 S2 (fight.loud.entry): the response unit's entry, or the nearest to the car's stand
                var stands = CurStand.Police; // brief 059 K6: the stand's police stands
                for (int i = 0; i < n && i < stands.Length; i++)
                {
                    var route = RouteRoundHold(entry, S.Stand(stands[i])) ?? Walk.Route(entry, S.Stand(stands[i])) ?? new[] { entry, S.Stand(stands[i]) }; // engine 0.34.0 (Motive M067-SQ1): a walk to a post goes round the car's hold
                    var gun = NewGun($"officer-{i + 1}", PistolW, true, R.HealthOfficer); gun.Engaged = true; gun.St = "advance"; // summoned: no alert delay (Pulse §3.2)
                    var home = S.Stand(stands[i]);
                    if (Lab && i < Labr!.PoliceContest) // brief 027 retest R1 (lab; Pulse retest-fixes.md §2, Motive tuning.md §7.2): its stand pulled into its ring of the car
                    {
                        var cp = StopPoint(home, Car, gun.W!.EffCm);
                        if (cp != home) { Note("ai.kite", $"{gun.Id} comes for the car: its stand is {G.Dist(cp, Car) / 100} m from it, not {G.Dist(home, Car) / 100} m."); home = cp; route = RouteRoundHold(entry, home) ?? Walk.Route(entry, home) ?? new[] { entry, home }; }
                    }
                    Hostiles.Add(new Hostile { Id = gun.Id, At = home, Home = home, Run = new Path(route), RunStart = Tick + i * R.BodyFileTicks, Speed = secSpeed, Present = true, Gun = gun }); // engine 0.36.0 (fight.spacing.file): in file from their one entry
                }
                int lockBefore = Lockdown;
                Lockdown = Math.Min(Lockdown, Tick + T(SealS)); SealMoved(lockBefore);
                NoteSealOrigin(lockBefore, () => { var o = alertTwoOrigin ?? gunfireOrigin; return o == null ? OriginNow("loud-fight", "loud-fight", "", null, null, null, "enforcer") : Recast(o, "loud-fight"); }); // what made it loud
                Note("fight.loud", $"The final fight: {Hostiles.Count} police officers come in from {entryName}. The seal clock is running.");
            }
            else
            {
                // brief 059 K9: the crew on the map (and still to come in the car) are the hostiles; no fresh spawns. The −1s are people who left
                CrewToFight();
                if (lookoutWarned) FightWhy.Add("lookout-warned-off");
                if (CarSt == CrewCar.Away || CarSt == CrewCar.TipOut) FightWhy.Add("wheelman-away");
                FightWarned = Inez.Turned && !Inez.Down && Inez.Has(R.HelpedAct);
                if (FightWarned) FightWhy.Add("waitress-warns");
                GuardAlly = !GuardDown && GuardGun.Health == GuardGun.Max && !GuardEngaged && !SawCell(GuardP);
                if (GuardAlly) FightWhy.Add("guard-fights-them");
                rivalsAbsorbed = true; // the crew is the ambush now
                if (FightOverTick != Tick) Note("fight.safe", $"The final fight: the rival crew ({Hostiles.Count}) lies in wait for the extraction."); // engine 0.34.0: not when the crew is destroyed at the fight's start (rivals.wiped is the one line, CrewFloor)
            }
        }

        /// <summary>Brief 059 S2 (fight.loud.entry): where the loud fight's officers come from: the response unit's entry, or (nearest) whichever of
        /// it and the mission's official entrances (the world's entrances of kind official) is the shortest grid walk to the getaway car's stand.</summary>
        Pt LoudEntryNow(out string name)
        {
            var home = S.Stand(S.FightEntry); name = "the north street";
            if (R.LoudEntry != "nearest" || Range) return home;
            var car = Walk.NearestFree(Car) ?? Car; long best = WalkMm(home, car); var at = home;
            foreach (var e in S.Entrances)
            {
                if (e.kind != "official") continue;
                long d = WalkMm(e.stand, car);
                if (d < best) { best = d; at = e.stand; name = "the " + e.id.Replace('-', ' '); }
            }
            return at;
        }

        /// <summary>The guard "never saw you" (§07): he holds no memory the cell left — no face, no act seen.</summary>
        bool SawCell(Person p) => p.Memory.Count > 0;


        void FightStep()
        {
            ContactStep(); // brief 027 verdict 6.4.8: an observer for the fight-lab matrix (output only)
            if (FightBranch.Length == 0 || FightOverTick != INF) return;
            JoinResponse();
            if (FightBranch == "loud" && !blockersPicked && pickFrom.Length == 0) pickFrom = "police"; // brief 018 §1: the first officer to arrive takes the lane (picked after this tick's fire)
            if (RivalReachesVale()) { Fail("rivals-reach-vale", $"A rival reaches the car with nobody beside it and takes {ValeName} out.", RivalsAtCar); return; }
            var e = Enforcer; var car = Car;
            if (FightBranch == "safe" && FightSprung == INF && ((Vale == ValeAt.WithEnforcer && !e.Inside && G.Within(e.Pos, car, R.SafeAmbushCm)) || (R.FightAlways && Vale == ValeAt.Car))) Spring(false); // engine 0.33.0 (Motive M059-S1.3): or Vale already in the car
            bool anyUp = false; foreach (var h in Hostiles) if (!h.Down && h.Gun.St != "break" && h.Gun.St != "gone") anyUp = true;
            if (!anyUp && FightBranch == "safe" && CrewDue()) anyUp = true; // brief 059: a wheelman coming back on the car is still to come
            if (!anyUp && Hostiles.Count > 0) { FightOverTick = Tick; Note("fight.over", "The last of them is down or gone. The car can go."); return; }
            // the guard runs in against the rivals (safe); his shots are in Combat()
            if (GuardAlly && guardAllyPath != null && !GuardDown)
            {
                long mm = (long)(Tick - guardAllyStart) * guardSpeed;
                heldPos = guardAllyPath.At(mm); heldFacing = guardAllyPath.Heading(mm);
            }
        }

        /// <summary>The ambush springs: the Enforcer brings Vale within fight.safe.ambush of the car, or (brief 059: the crew lurks on the street,
        /// in plain sight) the cell's shot at one of them gives it away first.</summary>
        void Spring(bool shot)
        {
            {
                FightSprung = Tick;
                // lying in wait, the crew fires aimNew after springing, plus the warning if Inez gave it (motive-fights.md §2.2 timing); brief 059: a
                // member still in the car springs when he gets out (he comes in on his path)
                foreach (var h in Hostiles) { if (CrewOf(h.Id) is CrewMember cm && !cm.Fight) continue; h.Present = true; h.Gun.Engaged = true; h.Gun.St = "alerted"; h.Gun.Until = h.Gun.NextShot = Tick + AimNewT + (FightWarned ? T(R.SafeWarnS) : 0); }
                Note("fight.ambush", shot ? "The cell's shot gives the ambush away: the rival crew springs." : FightWarned ? $"{Cap(InezName)}'s warning comes in time: the ambush is seen before it fires." : "The rival crew springs its ambush at the getaway car.");
                if (!blockersPicked) pickFrom = "rivals"; // brief 018 §1: one goes for the road as the ambush springs (picked after this tick's fire)
                if (GuardAlly)
                {
                    var from = GuardPose()?.pos ?? S.Stand(S.GuardPost);
                    var stop = ClearStop("guard", AllyStop()); // brief 081 (fight.spacing): clear of who stands there; his post, or the cover beside it (Pulse §3.4)
                    var route = Walk.Route(Walk.NearestFree(from) ?? from, Walk.NearestFree(stop) ?? stop);
                    guardAllyPath = new Path(route ?? new[] { from, stop }); guardAllyStart = Tick; // brief 081: stop is clear of the fight's claims (fight.spacing)
                    GuardHeld = true; heldPhase = "fighting the rivals"; heldPos = from; GuardAwayUntil = INF;
                    Note("fight.guard", "The guard hears the shooting and runs in against the rivals.");
                }
            }
        }
        bool GuardAllyMoving => GuardAlly && guardAllyPath != null && (long)(Tick - guardAllyStart) * guardSpeed < guardAllyPath.Length;

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


        void MisledOnce() { misledOnce = true; }
        /// <summary>A false tip has landed this mission (brief 059: the safe fight counts the wheelman away, not this; a read for the debrief).</summary>
        public bool MisledEver => misledOnce;
        bool RivalsAbsorbed => rivalsAbsorbed;
    }
}
