// Engine 0.33.0 (brief 059, Sky's S1 = no; Motive briefs/059/motive/s1-rulings.json and rulings.json M059-3.1, 3.4; rulebook fight.alwaysRule,
// rivals.peopleRule). The rival crew as people for the takedown and the body orders, and the rules that make the final fight always come:
//  · Takedown (knockout or kill), carry, drop, stash and restrain take a member on foot (not aboard, not a final-fight body) through the
//    brief-013 pipeline: his person (CrewMember.P, job rival) is the body's, so civilians who see the act or find the body report it and the
//    Trail is charged on a confirmed report, as for anyone. He is no witness, reports nothing, gives no statement, misses no check-in and is
//    never missing (he is not in Game.People). He is armed: aware of the Enforcer by his own facing cone (perception.rivals, no all-round near
//    ring), the takedown fails and he fires on him. A crewmate who sees the act (his cone, near ring included, a clear line, inside his
//    weapon's max ring) fires on the taker (ai.intent foe crewmate). Knocked out, he wakes after wake-s, staggers stagger-s and rejoins his
//    crew (his post; the car if the crew has gone; the safe fight as a joiner), keeping the Enforcer as a foe. Tied, he is out until freed: a
//    crewmate on foot within rivals.free of him unties him in free-s (as a finder does).
//  · The floor (rivals-floor): at the safe fight's start, away members come back until the count of the able and coming reaches it.
//  · Crew destroyed: with fight.safe.always and nobody able, the safe fight is over at its start.
// Every rule here is a function of ticks and orders; the state is hashed with the crew (HashCrew) and the bodies (HashTakedown).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>The crew member a person is (CrewMember.P), or null.</summary>
        CrewMember? CrewOfPerson(Person? p) => p != null && p.Job == Person.RivalJob ? CrewOf(p.Id) : null;

        /// <summary>A member not yet in the fight who will be (the safe fight): in the rival car on the map (the wheelman at the block steps out at
        /// the spring), or coming in it; not one away with it.</summary>
        bool CrewComing(CrewMember m)
        {
            if (!Up(m) || m.Fight || m.Mode != CrewMode.Aboard) return false;
            if (CarSt == CrewCar.Away || CarSt == CrewCar.Leaving || CarSt == CrewCar.Gone) return false;
            return CarSt != CrewCar.Stopped || m.OutAt < INF;
        }
        /// <summary>A member taken down by a knockout and not tied: he wakes and comes back (Motive M059-3.4).</summary>
        bool CrewWaking(CrewMember m) => m.Gun.Down && BodyOf(m.P) is Body b && !ActOf(b).Kill && !b.Restrained;

        /// <summary>The takedown's awareness for a member: his own facing cone and a clear line (no all-round near ring: a man posted with his
        /// back turned can be taken), or he already counts the Enforcer a foe. Armed: his pistol.</summary>
        (bool aware, bool armed) CrewAwareness(CrewMember m)
        {
            var e = Enforcer; var pose = MemberPose(m);
            bool aware = m.Avenge.Contains(Sim.Op.Name(e.Id)) || (pose.HasValue && G.InCone(pose.Value.pos, pose.Value.facing, R.RivReachCm, R.RivHalfCm, 0, e.Pos) && Sees(pose.Value.pos, e.Pos));
            return (aware, m.Gun.W != null);
        }

        /// <summary>The takedown's order lands on a member: he is down from now (seized), out of his walk or post; a crewmate who sees it draws;
        /// before the final fight, the crew may break (more than half on the map down, Motive M059-3.5).</summary>
        void CrewSeized(CrewMember m, Pt at)
        {
            m.Gun.Down = true; m.DownAt = at; m.Disp = null; m.Until = INF; ReleaseCover(m.Gun); LoseTarget(m.Gun);
            CrewSeesTakedown(m);
            if (FightBranch.Length == 0) CrewBreakCheck();
        }
        /// <summary>Motive M059-3.5: more than half of the crew on the map down (shot or taken down) before the final fight: it breaks.</summary>
        void CrewBreakCheck()
        {
            int on = 0, dn = 0;
            foreach (var x in Crew) { if (x.Gun.Down && x.Mode == CrewMode.Gone) continue; if (x.Gun.Down) { on++; dn++; continue; } if (x.Mode == CrewMode.Gone || x.Mode == CrewMode.Aboard) continue; on++; } // a member never on the map (Start.CrewGone) is not counted
            if (dn * 2 > on && !CrewBrokenPre) CrewBreaks();
        }

        /// <summary>Motive M059-S1.6: each tick of a takedown on a member, a crewmate on foot who sees the taker (his cone with its near ring, a
        /// clear line, inside his weapon's max ring) counts him a foe and fires (ai.intent foe crewmate; the shot is gunfire, so the run goes loud).</summary>
        void CrewSeesTakedown(CrewMember victim)
        {
            var e = Enforcer; string taker = Sim.Op.Name(e.Id);
            foreach (var m in Crew)
            {
                if (m == victim || !Up(m) || m.Fight || !m.OnFoot || m.Gun.W == null || m.Avenge.Contains(taker)) continue;
                var pose = MemberPose(m); if (!pose.HasValue) continue;
                if (!G.Within(pose.Value.pos, e.Pos, m.Gun.W.MaxCm)) continue;
                if (!G.InCone(pose.Value.pos, pose.Value.facing, R.RivReachCm, R.RivHalfCm, near, e.Pos) || !Sees(pose.Value.pos, e.Pos)) continue;
                m.Avenge.Add(taker);
                Note("rivals.see-takedown", $"{m.Id} sees {victim.Id} taken down and draws on the {taker}.");
            }
        }

        /// <summary>Motive M059-3.4: a knocked-out member wakes (BodiesStep's Wake, or the response brings him round): no report, no statement. He
        /// keeps the Enforcer as a foe, staggers stagger-s and rejoins his crew: the safe fight as a joiner while it runs; else his post (the
        /// posted three their spot, the lookout his post unless warned off), the front when the crew is drawn, the car when it is parked or
        /// stopped with the crew gone back to it, off the map when the car is.</summary>
        void CrewWakes(CrewMember m, Pt at, bool brought)
        {
            m.Gun.Down = false; m.Disp = null; m.Until = INF; m.Place = at; m.Avenge.Add(Sim.Op.Name(OpId.Enforcer));
            Note("rivals.wakes", brought ? $"{m.Id} of the rival crew is brought round. He says nothing." : $"{m.Id} of the rival crew comes round and gets up.");
            int stagger = T(R.StaggerS);
            if (FightBranch == "safe" && FightOverTick == INF)
            {
                JoinFightOnFoot(m, at, $"{m.Id} comes round and joins the fight.");
                var h = m.H!; if (h.Run != null) h.RunStart = Tick + stagger; h.Gun.NextShot = Math.Max(h.Gun.NextShot, Tick + stagger + AimNewT);
                return;
            }
            var car = RivalCarPose();
            if (RivalsDrawn && !CrewBrokenPre) StartWalk(m, at, FrontPlace(m), "front", rivSpeed);
            else if (CrewBrokenPre || m == Wheelman)
            {
                if (car.HasValue && (CarSt == CrewCar.Blocking || CarSt == CrewCar.Stopped)) StartWalk(m, at, StepOutAt(), "aboard", rivSpeed);
                else StartWalk(m, at, S.Stand(S.RivalLookoutExit), "leave", rivSpeed);
            }
            else if (m == Lookout) StartWalk(m, at, S.Stand(S.RivalLookout), "lookout", rivSpeed);
            else { var st = CrewStand; if (m.SpotStand != st.Id || m.Spot < 0) AssignSpot(m, st, at); StartWalk(m, at, S.Stand(st.Ambush[m.Spot]), "spot", rivSpeed); }
            if (m.Walk != null) m.WalkStart = Tick + stagger;
        }

        /// <summary>Motive M059-3.4: a tied member is out until freed; a crewmate on foot and up within rivals.free of him unties him in free-s.</summary>
        void CrewFrees(Body b, CrewMember victim)
        {
            var at = BodyPos(b);
            foreach (var m in Crew)
            {
                if (m == victim || !Up(m) || !m.OnFoot || m.Fight) continue;
                if (!G.Within(MemberPos(m, Tick), at, Cr.FreeCm)) continue;
                b.FreeAt = Tick + T(R.FreeS);
                Note("rivals.frees", $"{m.Id} finds {victim.Id} tied and unties him.");
                return;
            }
        }

        // ---------------------------------------------------------------- the floor and crew destroyed (fight.alwaysRule (3), (4))

        /// <summary>Motive M059-S1.4 (rivals-floor) at the safe fight's start: count the members up and coming (bodies up, riders coming; not one
        /// still knocked out, who joins only when he wakes: S1-FLOOR-WAKE); while fewer than the floor, the car away after a pre-fight break with
        /// members aboard cuts its trip short and comes back to the stand's block point (engine 0.41.0: the riders step out there for the fight).
        /// Nothing spawns: a killed, tied or paid member stays gone; a misled man is on foot and already a body. Then, with fight.safe.always and
        /// nobody able, the fight is over: the crew is destroyed.</summary>
        void CrewFloor(Stand st)
        {
            int coming = CrewAbleCount(false);
            if (R.RivalsFloor > 0 && coming < R.RivalsFloor && (CarSt == CrewCar.Away || CarSt == CrewCar.Leaving) && !carForGood && !crewCarless)
            {
                int aboard = Crew.FindAll(x => Up(x) && x.Mode == CrewMode.Aboard).Count;
                if (aboard > 0)
                {
                    var from = RivalCarPose()?.pos; LegToBlock(CrewCar.Returning, st, from);
                    crewNextAt = INF; crewNext = ""; RivalsDelayedUntil = Tick;
                    Note("rivals.recalled", "The rival car cuts its trip short and comes back for the fight.");
                    if (!FightWhy.Contains("rivals-recalled")) FightWhy.Add("rivals-recalled");
                }
            }
            if (R.FightAlways && CrewDestroyed()) // M059-S1.5: every one of the five killed, tied or paid, none knocked out to wake
            {
                FightOverTick = Tick; FightWhy.Add("crew-destroyed"); Chain.Add("rivals-destroyed");
                Note("rivals.wiped", "Nobody of the rival crew is left to fight: the crew is destroyed. The car can go.");
            }
        }
        /// <summary>Motive M059-S1.5: no member of the crew is able: each is down (shot down or killed) or tied, none knocked out to wake. A
        /// member alive and away (the wheelman on a tip, the lookout warned off) is able.</summary>
        bool CrewDestroyed() => Crew.TrueForAll(m => (m.Gun.Down && !CrewWaking(m)) || m.Paid); // engine 0.41.0: a paid man is gone for good too
        /// <summary>The members up and coming at the safe fight's start: a body up (on the street or still in the car), a rider coming, the
        /// lookout boarding; with waking, a knocked-out member not tied too.</summary>
        int CrewAbleCount(bool waking)
        {
            int n = 0;
            foreach (var m in Crew)
            {
                if (m.H != null && Hostiles.Contains(m.H) && Up(m.H)) { n++; continue; }
                if (CrewComing(m) || (waking && CrewWaking(m))) n++;
            }
            return n;
        }
    }
}
